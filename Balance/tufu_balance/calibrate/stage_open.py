"""Reproducible stage-1 calibration (design 5.3), with no writes to game assets.

The candidate list and split rules are closed before measurement. Failed design
criteria remain failed in the report; a regression baseline is not acceptance.
"""
from __future__ import annotations

import argparse
from collections import defaultdict
import csv
import dataclasses
import json
import math
import os
from pathlib import Path
import statistics
import time

import numpy as np

from . import driver, fit, metrics as M, params as P, report, sensitivity as S, targets as T
from ..model import data, simcalc as sc, spawners as sp
from ..model.enums import StatType, BalanceInterpolationType as K
from ..unity import fasttick as ft, inventory, override as ov, request as req, simio, spawner_map

BALANCE = Path(__file__).resolve().parents[2]
THETAS = (.5, .75, .9, 1.1)
DPS_SCALES = (.95, .98, .99, 1.01, 1.02, 1.05)
SLOTS = ((4, 8), (3, 8), (4, 7))
P18 = (("StatsThenItems", "SlotBeforeRpm"), ("ItemsThenStats", "SlotBeforeRpm"),
       ("StatsThenItems", "SlotAfterRpm"), ("ItemsThenStats", "SlotAfterRpm"))


def make_job(db, name, changes=None, *, recompute_items=False, econ="stored", day=405, rows=False):
    override = ov.default_override(db, from_day=0 if recompute_items else 4,
                                   replay_items="recompute" if recompute_items else "snapshot",
                                   econ_caches=econ, slots=None if recompute_items else ov.slots_p12(db))
    return ft.fast_job(name, override=override, params=changes, day=day,
                       max_wall_seconds=600.0, spawner_days=rows, outputs={"inventory": True})


def initial_jobs(db):
    """A fixed matrix, including diagnostics which never enter parameter selection."""
    return [
        make_job(db, "kill-start", rows=True),
        make_job(db, "kill-after", {"KillState": "AfterPass"}),
        make_job(db, "kill-none", {"DiagnosticDisableKillPredicate": True}),
        *(make_job(db, f"theta-{v}", {"KillLossMultiplier": v}) for v in THETAS),
        *(make_job(db, f"dps-{v}", {"DiagnosticPlayerDpsScale": v}) for v in DPS_SCALES),
        make_job(db, "tour", {"ActiveMinutesModel": "NnTour"}, rows=True),
        *(make_job(db, f"copies-{emission}-{mapping}", {"EmissionModel": emission, "PreferredMapping": mapping},
                   recompute_items=True, rows=emission == "Chain" and mapping == "Swapped")
          for emission in ("Chain", "E1") for mapping in ("Swapped", "Direct")),
        *(make_job(db, f"evolution-{trigger}", {"EvolutionTrigger": trigger}, recompute_items=True)
          for trigger in ("Timed", "Greedy", "TimedLag")),
        *(make_job(db, f"p07-{order}", {"DpsCacheProvenance": order})
          for order in ("ModifierFirst", "StaleTickCache")),
        *(make_job(db, f"p18-{i}", {"DayChangeInventoryOrder": a, "SlotRpmOrder": b},
                   recompute_items=True, day=12) for i, (a, b) in enumerate(P18)),
        *(make_job(db, f"slots-{a}-{b}", {"WeaponSlotUnlockLocations": [a, b]}, recompute_items=True)
          for a, b in SLOTS),
    ]


def write_json(path, value):
    Path(path).write_text(json.dumps(value, indent=2, sort_keys=True, ensure_ascii=False, allow_nan=False) + "\n")


def load_results(path):
    rows = json.loads(Path(path).read_text())
    return {r["job_id"]: Path(r["job_dir"]) for r in rows}


def measure(jobs, drv, path):
    start = time.monotonic()
    results = drv.run(jobs)
    rows = []
    for result in results:
        row = dataclasses.asdict(result)
        row["job_dir"] = str(result.job_dir) if result.job_dir else None
        row["run_dirs"] = list(map(str, result.run_dirs))
        rows.append(row)
    write_json(path, rows)
    write_json(path.with_suffix(".timing.json"), {"wall_s": time.monotonic() - start,
               "jobs": len(jobs), "cached": sum(r.status == "cached" for r in results),
               "submitted": drv.submitted, "code_rev": drv.backend.identity().code_rev})
    if not all(r.ok for r in results):
        raise RuntimeError(f"failed jobs: {[r.job_id for r in results if not r.ok]}; see {path}")
    return {r.job_id: r.job_dir for r in results}


def read_series(directory):
    db = directory / "sim.sqlite"
    if not db.exists():
        simio.ingest(directory)
    with T.connect_ro(db) as con:
        return T.load_series(con)


def fast_days(directory):
    return ft.read_days((directory / ft.DAYS_FILE).read_text())


def inventory_data(directory):
    return json.loads((directory / "inventory.json").read_text())


def stat_intervals(directory, shipped):
    return S.interval_results(shipped, S.daily_gains(fast_days(directory)))


def mean(values):
    values = list(values)
    return statistics.mean(values) if values else None


def ranking(family, scores, *, post_hoc, segment, note=""):
    """Use the P10 stable grid ranker. Only training scores choose discrete rules."""
    ordered = fit.grid([{"name": n} for n in scores], score=lambda c: scores[c["name"]][0])
    return [{"stage": "open", "family": family, "rank": i, "hypothesis": r.candidate["name"],
             "score_train": scores[r.candidate["name"]][0], "score_holdout": scores[r.candidate["name"]][1],
             "post_hoc": post_hoc, "distinguishing_segment": segment, "note": note}
            for i, r in enumerate(ordered.ranking, 1)]


def nn_tour(points):
    """C5's open nearest-neighbour path, starting at the origin (no return leg)."""
    left = list(points)
    x = z = length = 0.0
    while left:
        i = min(range(len(left)), key=lambda j: (left[j][0] - x)**2 + (left[j][1] - z)**2)
        nx, nz = left.pop(i)
        length += math.hypot(nx - x, nz - z)
        x, z = nx, nz
    return length


def tour_features(db, directory):
    """Compute the two NnTour regressors from C5's cleared set and extracted coordinates."""
    defs = sp.registered(sp.load_spawners(db))
    coords = {(r["scene"], r["file_id"]): (r["world_x"], r["world_z"])
              for r in db.execute('SELECT scene,file_id,world_x,world_z FROM spawners')}
    by_rt = {spawner_map.runtime_id(spawner_map.runtime_path(s.scene, s.go_path), s.key): coords[s.spawner_id]
             for s in defs}
    by_day = defaultdict(dict)
    for r in ft.read_spawner_days((directory / ft.SPAWNER_DAYS_FILE).read_bytes()):
        if r.fights and not r.dafc:
            by_day[r.day].setdefault(r.spawner, by_rt[r.spawner])
    out = {}
    for d in fast_days(directory):
        tour = nn_tour(by_day[d.day].values())
        if not math.isclose(tour, d.tour, rel_tol=1e-9, abs_tol=1e-6):
            raise ValueError(f"Python/C5 NN path differs on day {d.day}: {tour} / {d.tour}")
        out[d.day] = (3 * tour / d.speed / 60, d.fight_seconds / 60)
    return out


def active_fit(db, paths, shipped):
    days = fast_days(paths["kill-start"])
    f = {d.day: d.farmed_seconds / 60 for d in days}
    nf = tour_features(db, paths["tour"])
    cells = S.t04(shipped, f)
    x = {"FarmedTime": [[c.sim] for c in cells],
         "NnTour": [[sum(nf[d][i] for d in range(c.a, c.b)) / (c.b-c.a) for i in (0, 1)] for c in cells]}
    train = [i for i, c in enumerate(cells) if 11 <= c.a and c.b <= 104]
    hold = [i for i, c in enumerate(cells) if c.a >= 104]
    y = [c.ref for c in cells]
    coefficients = {
        "FarmedTime": [fit.ratio_fit([y[i] for i in train], [x["FarmedTime"][i][0] for i in train])],
        "NnTour": list(fit.linear_least_squares([x["NnTour"][i] for i in train], [y[i] for i in train]).x),
    }
    models, residuals, aics = {}, [], {}
    for name, coeff in coefficients.items():
        predictions = np.asarray(x[name]) @ coeff
        rels = [abs(p / c.ref - 1) for p, c in zip(predictions, cells)]
        rss = sum((predictions[i] - y[i])**2 for i in hold)
        aics[name] = (float(rss), len(hold), len(coeff))
        models[name] = {"coefficients": list(map(float, coeff)),
                        "train": mean(rels[i] for i in train), "holdout": mean(rels[i] for i in hold),
                        "median": statistics.median(rels[i] for i in train + hold),
                        "max": max(rels[i] for i in train + hold),
                        "holdout_rss": float(rss), "holdout_n": len(hold)}
        for c, pred, rel in zip(cells, predictions, rels):
            residuals.append({"model": name, "a": c.a, "b": c.b, "sim": float(pred), "ref": c.ref,
                              "rel": float(rel), "split": "known_failure" if c.a < 11 else
                              ("train" if c.b <= 104 else "holdout")})
    aic_rows = [dataclasses.asdict(r) for r in fit.compare_aic(aics)]
    chosen = aic_rows[0]["name"]
    # The C# fields are float32; record exactly the values the runtime will consume.
    coefficients = {k: [float(np.float32(v)) for v in vals] for k, vals in coefficients.items()}
    blocks = [[(y[i], x["FarmedTime"][i][0])] for i in train]
    boot = fit.block_bootstrap(blocks, lambda xs: fit.ratio_fit([a for a, _ in xs], [f for _, f in xs]))
    changes = {"ActiveMinutesModel": chosen, "Kappa": coefficients["FarmedTime"][0],
               "TourA": coefficients["NnTour"][0], "TourB": coefficients["NnTour"][1], "Tours": 3}
    return changes, {"chosen": chosen, "models": models, "aic": aic_rows,
                     "kappa_bootstrap90": [boot.lo, boot.hi], "train_n": len(train), "holdout_n": len(hold)}, residuals


def dps_scores(series, targets, lo=1, hi=24):
    values = []
    for ref in targets.shipped.tc:
        if lo <= ref.tc <= hi:
            snap = series.day(ref.tc)
            for k, v in ref.dps.items():
                if v:
                    values.append(abs(snap.dps[k] / v - 1))
    return mean(values)


def p12_scores(db, targets):
    """R2b on identical stored items, disentangling the slot rule from copy errors."""
    ctx = sc.load_simcalc_context(db)
    snaps = data.load_simulation_data(db).time_chunk_snapshots
    evolved = ov.items_evolved_default(db)
    gates = sc.slot_unlock_locations(sp.load_spawners(db))
    scores, rows = {}, []
    for a, b in SLOTS:
        parts = defaultdict(list)
        for snap in snaps:
            if snap.time_chunk_index < 1:
                continue
            w, armor = sc.slots_p12(snap.location, gates)
            w = 1 + int(w >= 2) + int(snap.location >= a) + int(snap.location >= b)
            result = sc.recompute_dps_defense(ctx, snap, evolved[str(snap.time_chunk_index)], w, armor)
            split = "train" if snap.time_chunk_index <= 24 else "holdout"
            for k, v in result.dps:
                ref = dict(snap.dps_data).get(k)
                if ref:
                    rel = abs(v / ref - 1)
                    parts[split].append(rel)
                    rows.append({"candidate": f"{a},{b}", "tc": snap.time_chunk_index, "stat": k,
                                 "sim": v, "ref": ref, "rel": rel, "split": split})
        scores[f"{a},{b}"] = (mean(parts["train"]), mean(parts["holdout"]))
    return scores, rows


def rpm_oracle(db, shipped):
    ctx = sc.load_simcalc_context(db)
    results = {}
    for name in (sc.RPM_FIRST, sc.MODIFIER_FIRST, sc.STALE_TICK_CACHE):
        replay = dict(sc.replay_day_gpm(ctx, shipped, order=name).replayed)
        misses = [[s.time_chunk_index, StatType(st.key).name] for s in shipped for st in s.stats
                  if not sc.gpm_matches(st.current_gpm, replay[s.time_chunk_index][st.key])]
        results[name] = {"cells": sum(len(s.stats) for s in shipped), "hits": sum(len(s.stats) for s in shipped)-len(misses),
                         "misses": misses}
    return results


def choose(db, paths, targets):
    shipped = data.load_simulation_data(db).time_chunk_snapshots
    hypotheses, detail, residuals = [], {}, {}
    scores = {}
    for name in ("kill-start", "kill-after"):
        rs = stat_intervals(paths[name], shipped)
        # Dimensionless MAE, separately per interval; no absolute reward-size weighting.
        vals = [(r, mean(abs(v) / max(1, abs(r.obs[k])) for k, v in r.residual.items())) for r in rs]
        scores[name] = (mean(v for r, v in vals if r.b <= 104), mean(v for r, v in vals if r.a >= 104))
    rank = ranking("KillState", scores, post_hoc=False, segment="T05: TC4-103 / TC104-404; T08 diagnostic")
    hypotheses += rank
    chosen_kill = "StartOfDay" if rank[0]["hypothesis"] == "kill-start" else "AfterPass"
    changes, active, active_rows = active_fit(db, paths, shipped)
    changes["KillState"] = chosen_kill
    detail["active"] = active
    residuals["active"] = active_rows
    for i, aic in enumerate(active["aic"], 1):
        name = aic["name"]
        hypotheses.append({"stage": "open", "family": "ActiveMinutesModel", "rank": i, "hypothesis": name,
                           "score_train": active["models"][name]["train"],
                           "score_holdout": active["models"][name]["holdout"], "post_hoc": False,
                           "distinguishing_segment": "T04 TC11-103 fit; TC104-404 AIC selection (not independent external validation)",
                           "note": f"holdout AIC={aic['aic']}; coefficients={active['models'][name]['coefficients']}"})
    series = {n: read_series(p) for n, p in paths.items() if n.startswith(("copies-", "evolution-", "p18-", "slots-"))}
    interp = data.load_interpolations(db)
    count_scores, mapping_scores = {}, {}
    for name in (n for n in series if n.startswith("copies-")):
        cells = M.t12(series[name], targets)
        residuals[name] = [dataclasses.asdict(c) for c in cells]
        # Chain vs E1: daily intervals already distinguish them. Later coefficient form is separate.
        count_scores[name] = (mean(c.rel for c in cells if c.group == "daily" and c.ref),
                              mean(c.rel for c in cells if c.group.startswith("phase_") and c.ref))
        eligible = {f"phase_{a}_{b}" for a, b in M.T12_PHASES
                    if all(float(interp.get_total_coefficient(d, K.ItemEmissionByType)) > 0 for d in range(a, b))}
        mapping_scores[name] = (mean(c.rel for c in cells if c.group in eligible and c.ref), None)
    count_rank = ranking("EmissionModel", {n: v for n, v in count_scores.items() if n.endswith("Swapped")},
                         post_hoc=True, segment="Chain vs E1: TC4-11, independently by rarity")
    hypotheses += count_rank
    emission = count_rank[0]["hypothesis"].split("-")[1]
    mapping_rank = ranking("PreferredMapping", {n: v for n, v in mapping_scores.items() if f"-{emission}-" in n},
                           post_hoc=True, segment="Only full phases with K7>0 (TC57-405)")
    hypotheses += mapping_rank
    mapping = mapping_rank[0]["hypothesis"].split("-")[2]
    changes.update(EmissionModel=emission, PreferredMapping=mapping)
    evo_scores = {}
    for trigger in ("Timed", "Greedy", "TimedLag"):
        name = f"evolution-{trigger}"
        cells = M.t13(series[name], targets)
        residuals[name] = [dataclasses.asdict(c) for c in cells]
        # Missing levels count as beyond the last possible interval, never disappear from the score.
        score = lambda group: mean(abs(c.residual) if c.residual is not None else len(targets.shipped.tc)
                                   for c in cells if c.group == group)
        evo_scores[trigger] = (score("train"), score("holdout"))
    evo_rank = ranking("EvolutionTrigger", evo_scores, post_hoc=False, segment="T13 C/U/R fit; E/L holdout")
    hypotheses += evo_rank
    changes["EvolutionTrigger"] = evo_rank[0]["hypothesis"]
    p18_scores = {str(i): (dps_scores(series[f"p18-{i}"], targets, 4, 7),
                            dps_scores(series[f"p18-{i}"], targets, 8, 11)) for i in range(4)}
    p18_rank = ranking("P18", p18_scores, post_hoc=False, segment="DPS only: TC4-7 fit / TC8-11 holdout; never T05")
    hypotheses += p18_rank
    changes["DayChangeInventoryOrder"], changes["SlotRpmOrder"] = P18[int(p18_rank[0]["hypothesis"])]
    scores, slot_rows = p12_scores(db, targets)
    slot_rank = ranking("P12", scores, post_hoc=False, segment="R2b stored inputs: TC1-24 fit; TC>=25 holdout (not fitted fixture)")
    hypotheses += slot_rank
    changes["WeaponSlotUnlockLocations"] = list(map(int, slot_rank[0]["hypothesis"].split(",")))
    residuals["p12"] = slot_rows
    detail["p12_actual_recompute_items"] = {n: {"train": dps_scores(s, targets), "holdout": dps_scores(s, targets, 25, 405)}
                                              for n, s in series.items() if n.startswith("slots-")}
    detail["p18"] = {str(i): {"train": p18_scores[str(i)][0], "holdout": p18_scores[str(i)][1],
                              "all": dps_scores(series[f"p18-{i}"], targets, 4, 11),
                              "day5": dps_scores(series[f"p18-{i}"], targets, 5, 5)} for i in range(4)}
    detail["Q25"] = "unresolved" if all(v["day5"] > 2e-5 for v in detail["p18"].values()) else "candidate matches day5; investigate independently"
    detail["rpm_oracle"] = rpm_oracle(db, shipped)
    detail["holdout"] = {r["family"]: {"train": r["score_train"], "holdout": r["score_holdout"],
                                     "pass": r["score_holdout"] <= 1.5*r["score_train"]}
                         for r in hypotheses if r["rank"] == 1 and not r["post_hoc"] and r["score_holdout"] is not None}
    return changes, hypotheses, detail, residuals


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--rev")
    ap.add_argument("--out", type=Path, default=report.DEFAULT_ROOT)
    ap.add_argument("--workspace", type=Path, default=BALANCE / "out/calibration/open-work")
    ap.add_argument("--analyze-only", action="store_true")
    ap.add_argument("--write-layer", action="store_true")
    args = ap.parse_args(argv)
    args.workspace.mkdir(parents=True, exist_ok=True)
    started = time.monotonic()
    with T.connect_ro(os.environ.get("TUFU_BALANCE_DB", BALANCE / "out/balance.sqlite")) as db:
        targets = T.build(db, slot_schedule=T.load_slot_schedule())
        drv = driver.Driver(driver.default_backend(rev=args.rev), batch_size=12)
        path = args.workspace / "initial.json"
        paths = load_results(path) if args.analyze_only else measure(initial_jobs(db), drv, path)
        changes, hypotheses, detail, residuals = choose(db, paths, targets)
        write_json(args.workspace / "choice.json", changes)
        write_json(args.workspace / "detail.json", detail)
        for name, rows in residuals.items():
            if rows:
                (args.workspace / (name + ".csv")).write_text(report.csv_text(tuple(rows[0]), rows))
        (args.workspace / "hypotheses.csv").write_text(report.csv_text(report.HYPOTHESIS_COLUMNS, hypotheses))
        if args.write_layer:
            write_json(P.PARAMS_ROOT / "open.json", {"_stage": "K1; empirical choices, see Runbook section 24", **changes})
        print(json.dumps({"choice": changes, "detail": detail, "wall_s": time.monotonic()-started}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
