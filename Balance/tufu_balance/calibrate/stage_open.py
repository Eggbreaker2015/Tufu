"""Reproducible stage-1 calibration (design 5.3), with no writes to game assets.

The candidate list and split rules are closed before measurement. Failed design
criteria remain failed in the report; a regression baseline is not acceptance.
"""
from __future__ import annotations

import argparse
from collections import defaultdict
import dataclasses
import json
import math
import os
from pathlib import Path
import statistics
import time

import numpy as np

from . import driver, fit, metrics as M, params as P, report, sensitivity as S, targets as T
from ..model import data, items, simcalc as sc, spawners as sp
from ..model.enums import StatType, BalanceInterpolationType as K
from ..unity import engine, fasttick as ft, inventory, override as ov, request as req, simio, spawner_map

BALANCE = Path(__file__).resolve().parents[2]
THETAS = (.5, .75, .9, 1.1)
DPS_SCALES = (.95, .98, .99, 1.01, 1.02, 1.05)
SLOTS = ((4, 8), (3, 8), (4, 7))
P18 = (("StatsThenItems", "SlotBeforeRpm"), ("ItemsThenStats", "SlotBeforeRpm"),
       ("StatsThenItems", "SlotAfterRpm"), ("ItemsThenStats", "SlotAfterRpm"))


def make_job(db, name, changes=None, *, recompute_items=False, econ="stored", day=405, rows=False, rpm_validation=False):
    override = ov.default_override(db, from_day=0 if recompute_items or rpm_validation else 4,
                                   replay_items="recompute" if recompute_items else "snapshot",
                                   econ_caches=econ, slots=None if recompute_items else ov.slots_p12(db),
                                   unlock="snapshot" if rpm_validation else "derived",
                                   rpm_after_override="skip" if rpm_validation else "recompute")
    return ft.fast_job(name, override=override, params=changes, day=day,
                       max_wall_seconds=600.0, spawner_days=rows, outputs={"inventory": True},
                       probe=engine.engine_probe(null_policy=True) if rpm_validation else None)


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
        *(make_job(db, f"p07-{order}", {"DpsCacheProvenance": order}, rpm_validation=True)
          for order in ("RpmFirst", "ModifierFirst", "StaleTickCache")),
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
    timing = {"wall_s": time.monotonic() - start,
              "jobs": len(jobs), "cached": sum(r.status == "cached" for r in results),
              "submitted": drv.submitted, "code_rev": drv.backend.identity().code_rev}
    timing_path = path.with_suffix(".timing.json")
    attempt = 1
    while timing_path.exists():
        attempt += 1
        timing_path = path.with_suffix(f".attempt-{attempt}.timing.json")
    # Exclusive creation also protects the first attempt from concurrent/retry writers.
    with timing_path.open("x") as f:
        f.write(json.dumps(timing, indent=2, sort_keys=True) + "\n")
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
    return float(statistics.mean(values)) if values else None


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
        if not math.isclose(tour, d.tour, rel_tol=2e-6, abs_tol=1e-6):
            raise ValueError(f"Python/C5 NN path differs on day {d.day}: {tour} / {d.tour}")
        out[d.day] = (3 * tour / d.speed / 60, d.fight_seconds / 60)
    return out


def active_fit(db, paths, shipped):
    days = fast_days(paths["kill-start"])
    f = {d.day: d.farmed_seconds / 60 for d in days}
    nf = tour_features(db, paths["tour"])
    nn_relative = max(abs(nf[d.day][0] - 3*d.tour/d.speed/60) / max(1, abs(nf[d.day][0]))
                      for d in fast_days(paths["tour"]))
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
    tour_blocks = [[(y[i], *x["NnTour"][i])] for i in train]
    tour_ci = []
    for column in (0, 1):
        sample = fit.block_bootstrap(tour_blocks,
                    lambda xs, col=column: fit.linear_least_squares([r[1:] for r in xs], [r[0] for r in xs]).x[col])
        tour_ci.append([sample.lo, sample.hi])
    changes = {"ActiveMinutesModel": chosen, "Kappa": coefficients["FarmedTime"][0],
               "TourA": coefficients["NnTour"][0], "TourB": coefficients["NnTour"][1], "Tours": 3}
    return changes, {"chosen": chosen, "models": models, "aic": aic_rows,
                     "kappa_bootstrap90": [boot.lo, boot.hi], "tour_bootstrap90": tour_ci, "nn_feature_max_relative_vs_csharp": nn_relative, "train_n": len(train), "holdout_n": len(hold)}, residuals


def dps_scores(series, targets, lo=1, hi=24):
    values = []
    for ref in targets.shipped.tc:
        if lo <= ref.tc <= hi:
            snap = series.day(ref.tc)
            for k, v in ref.dps.items():
                if v:
                    values.append(abs(snap.dps[k] / v - 1))
    return mean(values)



def evolution_score(cells, tc_days):
    """Mean arrival-position error on observed targets; missing simulated arrivals are lower bounds."""
    def score(group):
        return mean(abs(c.residual) if c.residual is not None else len(tc_days)-M._position(tc_days, c.ref)
                    for c in cells if c.group == group and c.ref is not None)
    return score("train"), score("holdout")

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
    for name in (sc.RPM_FIRST, sc.MODIFIER_FIRST):
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
        residuals[name] = [{"a": r.a, "b": r.b, "stat": StatType(k).name,
                            "sim": r.pred[k], "ref": v, "residual": r.pred[k]-v,
                            "rel": abs(r.pred[k]-v)/max(1, abs(v)),
                            "split": "train" if r.b <= 104 else "holdout"}
                           for r in rs for k, v in r.obs.items()]
    rank = ranking("KillState", scores, post_hoc=False, segment="T05: TC4-103 / TC104-404; T08 diagnostic",
                   note="Holdout is non-discriminating; 1.5x is a sanity check, not confirmation")
    hypotheses += rank
    chosen_kill = "StartOfDay" if rank[0]["hypothesis"] == "kill-start" else "AfterPass"
    changes, active, active_rows = active_fit(db, paths, shipped)
    changes["KillState"] = chosen_kill
    changes["DpsCacheProvenance"] = "RpmFirst"
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
        # A simulated-only arrival has no reference time: retain it in the CSV as censored.
        # A reference arrival that the run has not reached has a lower-bound error beyond day 405.
        detail.setdefault("evolution_censored", {})[trigger] = [c.key for c in cells if c.ref is None]
        evo_scores[trigger] = evolution_score(cells, targets.shipped.tc_days)
    evo_rank = ranking("EvolutionTrigger", evo_scores, post_hoc=False, segment="T13 C/U/R fit; E/L holdout")
    hypotheses += evo_rank
    changes["EvolutionTrigger"] = evo_rank[0]["hypothesis"]
    p18_scores = {str(i): (dps_scores(series[f"p18-{i}"], targets, 4, 7),
                            dps_scores(series[f"p18-{i}"], targets, 8, 11)) for i in range(4)}
    for i in range(4):
        residuals[f"p18-{i}"] = [{"tc": ref.tc, "stat": k, "sim": series[f"p18-{i}"].day(ref.tc).dps[k],
                                   "ref": v, "rel": abs(series[f"p18-{i}"].day(ref.tc).dps[k]/v-1),
                                   "split": "train" if ref.tc <= 7 else "holdout"}
                                  for ref in targets.shipped.tc if 4 <= ref.tc <= 11 for k, v in ref.dps.items() if v]
    p18_rank = ranking("P18", p18_scores, post_hoc=False, segment="DPS only: TC4-7 fit / TC8-11 holdout; never T05",
                       note="DayChangeInventoryOrder tied / not identified; default kept. SlotRpmOrder selected")
    hypotheses += p18_rank
    # An exact tie cannot identify the inventory order. Keep the declared default explicitly.
    best = p18_rank[0]["score_train"]
    tied = [int(r["hypothesis"]) for r in p18_rank if r["score_train"] == best]
    default_order = "StatsThenItems"
    winner = next((i for i in tied if P18[i][0] == default_order), tied[0])
    changes["DayChangeInventoryOrder"], changes["SlotRpmOrder"] = P18[winner]
    scores, slot_rows = p12_scores(db, targets)
    slot_rank = ranking("P12", scores, post_hoc=False, segment="R2b stored inputs: TC1-24 fit; TC>=25 holdout (not fitted fixture)",
                        note="Holdout is non-discriminating: all candidates have the same slots by TC25; sanity check only")
    hypotheses += slot_rank
    changes["WeaponSlotUnlockLocations"] = list(map(int, slot_rank[0]["hypothesis"].split(",")))
    residuals["p12"] = slot_rows
    detail["p12_actual_recompute_items"] = {n: {"train": dps_scores(s, targets), "holdout": dps_scores(s, targets, 25, 405)}
                                              for n, s in series.items() if n.startswith("slots-")}
    detail["p18"] = {str(i): {"train": p18_scores[str(i)][0], "holdout": p18_scores[str(i)][1],
                              "all": dps_scores(series[f"p18-{i}"], targets, 4, 11),
                              "day5": dps_scores(series[f"p18-{i}"], targets, 5, 5)} for i in range(4)}
    detail["Q25"] = "unresolved" if all(v["day5"] > 2e-5 for v in detail["p18"].values()) else "candidate matches day5; investigate independently"
    detail["non_discriminating_holdout"] = [family for family in ("KillState", "P12")
        if len({r["score_holdout"] for r in hypotheses if r["family"] == family}) == 1]
    detail["p18_inventory_order"] = ("tie: not identified; default kept" if len({P18[i][0] for i in tied}) > 1
                                      else "identified on training segment")
    detail["rpm_oracle"] = rpm_oracle(db, shipped)
    detail["holdout"] = {r["family"]: {"train": r["score_train"], "holdout": r["score_holdout"],
                                     "pass": r["score_holdout"] <= 1.5*r["score_train"]}
                         for r in hypotheses if r["rank"] == 1 and not r["post_hoc"] and r["score_holdout"] is not None}
    return changes, hypotheses, detail, residuals



def selected_jobs(db, changes):
    return [make_job(db, "selected-kill", changes, rows=True),
            make_job(db, "selected-items", changes, recompute_items=True, rows=True),
            make_job(db, "selected-recomputed", changes, econ="recomputed", rows=True)]


def t05_evidence(db, directory, shipped):
    results = stat_intervals(directory, shipped)
    rows = ft.read_spawner_days((directory / ft.SPAWNER_DAYS_FILE).read_bytes())
    ctx = sc.load_simcalc_context(db)
    by_rt = {spawner_map.runtime_id(spawner_map.runtime_path(s.d.scene, s.d.go_path), s.d.key): s
             for s in ctx.spawners}
    rewards = {}
    for r in rows:
        if r.fights and r.spawner not in rewards:
            s = by_rt[r.spawner]
            rewards[r.spawner] = {rw.stat_type: float(rw.value)*s.d.count*r.fights for rw in s.stat_rewards}
    explanations = {r.unit: S.explain(r, ft.kill_days(rows), rewards) for r in S.bad_intervals(results)}
    first = {}
    for key, day in ft.kill_days(rows).items():
        spawner = by_rt[key].d
        first.setdefault((spawner.go_name, spawner.location), day)
    return {"exact": sum(r.exact for r in results), "daily_exact": sum(r.exact for r in results if r.daily),
            "bad_spans": S.bad_spans(results),
            "unexplained": [u for u, e in explanations.items() if e is None],
            "explanations": {u: dataclasses.asdict(e) if e else None for u, e in explanations.items()},
            "first_kills": S.t08(first),
            "threshold_equal_rows": sum(r.loss == r.max_health for r in rows),
            "rows": len(rows)}


def trace_cells(directory, targets):
    """P10 cells, with T04/T05 measured from rewards instead of overwritten snapshots."""
    shipped = [s for s in targets.shipped.tc if s.tc >= 4]
    days = fast_days(directory)
    lookup = {d.day: d for d in days}
    cells = []
    for a, b in zip(shipped, shipped[1:]):
        ds = range(a.tc, b.tc)
        for key in T.STATS:
            ref = b.value(key)-a.value(key)
            pred = sum(lookup[d].stats.get(int(StatType[key]), 0) for d in ds)
            cells.append(M.Cell("T05", "daily" if b.tc-a.tc == 1 else "multi_day",
                                f"{a.tc:03d}->{b.tc:03d}|{key}", pred, ref, pred-ref,
                                abs(pred-ref)/max(1, abs(ref)), stat=key, tc=b.tc,
                                unit=f"{a.tc:03d}->{b.tc:03d}", split="report",
                                note="sum of actual fast rewards; replay resets Value daily"))
        pred = sum(lookup[d].active_minutes for d in ds)/(b.tc-a.tc)
        ref = (b.playtime-a.playtime)/(b.tc-a.tc)
        cells.append(M.Cell("T04", "tc_4_10" if a.tc < 11 else "tc_ge_11",
                            f"{a.tc:03d}->{b.tc:03d}", pred, ref, pred-ref, abs(pred/ref-1), tc=b.tc,
                            split="train" if b.tc <= 104 else "holdout",
                            note="sum of actual fast active minutes; excludes replay P resets"))
    return cells


def events(directory):
    return [json.loads(line) for line in (directory / "events.jsonl").read_text().splitlines()]


def diagnostic_rows(paths, shipped):
    rows = []
    curve, unlocks = {}, {}
    for name in ("kill-start", "kill-none", *(f"theta-{t}" for t in THETAS), *(f"dps-{v}" for v in DPS_SCALES)):
        bad = len(S.bad_intervals(stat_intervals(paths[name], shipped)))
        t07 = S.t07(S.unlock_days(events(paths[name])))
        inrange = sum(v["distance"] == 0 for k, v in t07.items() if k >= 5)
        rows += [{"probe": name, "metric": "T05_bad_intervals", "value": bad, "note": "post-hoc diagnostic, not fitted"},
                 {"probe": name, "metric": "T07_in_range_5_17", "value": inrange}]
        if name.startswith("theta-") or name in ("kill-start", "kill-none"):
            theta = 1.0 if name == "kill-start" else ("Infinity" if name == "kill-none" else float(name[6:]))
            rows.append({"probe": "theta", "scale": theta, "metric": "T05_bad_intervals", "value": bad,
                         "note": "fixed L=1; ablation only"})
        if name.startswith("dps-") or name == "kill-start":
            scale = 1.0 if name == "kill-start" else float(name[4:])
            curve[scale] = bad
            unlocks[scale] = {k: v["distance"] == 0 for k, v in t07.items() if k >= 5}
            rows += [{"probe": "DPS", "scale": scale, "metric": f"island_{k}_day", "value": v["day"]}
                     for k, v in t07.items()]
    base = unlocks[1.0]
    flips = {str(v): sum(base[k] != values[k] for k in base) for v, values in unlocks.items()}
    return rows, {"dps_bad": {str(k): v for k, v in curve.items()}, "t07_flips": flips,
                   "closed_t05_min_e002": S.closed_loop_exact_min(curve, .02)}


def coefficient_ablation(db, directory, targets):
    defs = items.load_item_defs(db)
    by_asset = {i.asset_name: i for i in defs.values()}
    by_rt = {spawner_map.runtime_id(spawner_map.runtime_path(s.scene, s.go_path), s.key): s
             for s in sp.registered(sp.load_spawners(db))}
    locs = {r["Number"]: (r["PreferredWeaponType"], r["PreferredArmorType"])
            for r in db.execute('SELECT * FROM c_LocationsConfig__Locations')}
    days = {d.day: d for d in fast_days(directory)}
    interp = data.load_interpolations(db)
    full, no_drop, plateau = defaultdict(float), defaultdict(float), defaultdict(lambda: [0., 0])
    for r in ft.read_spawner_days((directory / ft.SPAWNER_DAYS_FILE).read_bytes()):
        spawner = by_rt[r.spawner]
        if not r.fights or not spawner.item_reward:
            continue
        it = by_asset[spawner.item_reward]
        n = r.fights*r.count
        p = float(interp.get_total_coefficient(r.day, K.ItemChainProbability))
        chain = n if p == 1 else (1-p**n)/(1-p)
        coefficient = float(interp.get_total_coefficient(r.day, K.ItemLegendaryEmission if it.rarity == 4 else K.ItemEmission))
        bonus = float(interp.get_total_coefficient(r.day, K.ItemEmissionByType))
        location = days[r.day].rounds[r.round-1].location
        preferred = it.damage_type == locs[location][0 if it.is_armor else 1]
        key = (r.day, it.kind, it.rarity)
        amount = chain*float(np.float32(1+coefficient+(bonus if preferred else 0)))
        full[key] += amount
        no_drop[key] += chain*float(np.float32(1+(bonus if preferred else 0)))
        if r.day >= 206:
            group = (it.rarity == 4, preferred)
            plateau[group][0] += amount
            plateau[group][1] += n
    rows = []
    rarities = ("Common", "Uncommon", "Rare", "Epic", "Legendary")
    for a, b in M.T12_PHASES:
        if not all(float(interp.get_total_coefficient(d, K.ItemEmission)) > .1 for d in range(a, b)):
            continue
        for (kind, rarity), ref in M._gains(targets.shipped, targets, a, b).items():
            if not ref:
                continue
            for label, source in (("full", full), ("no_K0_K6", no_drop)):
                pred = sum(source.get((d, kind, rarities.index(rarity)), 0) for d in range(a, b))
                rows.append({"candidate": label, "a": a, "b": b, "kind": kind, "rarity": rarity,
                             "sim": pred, "ref": ref, "rel": abs(pred/ref-1)})
    refs = {(False, False): 2.5, (False, True): 4.3, (True, False): 2.8, (True, True): 4.6}
    platform = {f"legendary={l},preferred={p}": {"sim": val/charges, "ref": refs[(l,p)],
                                               "rel": abs(val/charges/refs[(l,p)]-1), "charges": charges}
                for (l, p), (val, charges) in plateau.items()}
    return rows, platform


def mace_eligibility(db, directory):
    """Compare C5's first item-only Epic spawner with the unchanged original bot filter."""
    import csv
    ctx = sc.load_simcalc_context(db)
    shipped = data.load_simulation_data(db)
    target = next(x for x in ctx.spawners if x.d.go_name == "Spawner_1_epic_buck" and x.d.location == 4)
    rt = spawner_map.runtime_id(spawner_map.runtime_path(target.d.scene, target.d.go_path), target.d.key)
    rows = ft.read_spawner_days((directory / ft.SPAWNER_DAYS_FILE).read_bytes())
    actual = {r.day: r for r in rows if r.spawner == rt and r.round == 1}
    out = []
    for day in range(9, 14):
        snap = ov.expected_day(shipped, "replay", day, None).snapshot
        state = sc.snapshot_state(snap, ctx, order=sc.MODIFIER_FIRST, day=day)
        bot = sc.calculate_spawner_data(ctx, state, for_gpm_bot=False)
        eligible = sc.eligible_spawners(ctx, state)
        row = {"day": day, "snapshot_location": state.unlocked_location,
               "oracle_bot": target.spawner_id in bot, "oracle_eligible": target.spawner_id in eligible,
               "csharp": dataclasses.asdict(actual[day]) if day in actual else None}
        if target.spawner_id in bot:
            x = bot[target.spawner_id]
            row.update(oracle_fight_seconds=x.fight_time, oracle_loss=x.loss,
                       oracle_health=state.stat(StatType.Health))
        out.append(row)
    with (directory / "ready_spawners.csv").open() as f:
        ready = next(r for r in csv.DictReader(f) if r["key"] == target.d.key)
    return {"spawner": [target.d.scene, target.d.file_id], "runtime_id": rt,
            "OT": target.d.optimum_time, "is_days": target.d.is_days, "count": target.d.count,
            "charge_limit": ctx.charges_limit(target), "ready": ready, "days": out,
            "island4_unlock": S.unlock_days(events(directory)).get(4),
            "override_unlock": json.loads((directory / "manifest.json").read_text())["job"]["override"]["unlock"],
            "handoff": "K3: original C# and oracle agree days 10-12 eligible; shipped first mace TC15 implies day13. Three days x2 copies = entire +6. No C5 defect or permissible theta fit demonstrated."}


def archive(db, paths, selected, changes, hypotheses, detail, residuals, out_root, workspace):
    targets = T.build(db, slot_schedule=T.load_slot_schedule())
    shipped = data.load_simulation_data(db).time_chunk_snapshots
    kill, item, bridge = (selected[n] for n in ("selected-kill", "selected-items", "selected-recomputed"))
    series = read_series(item)
    cells = [c for c in M.compute(series, targets) if c.target not in ("T04", "T05", "T09")]
    expected = {d.day: d.items for d in fast_days(item)}
    cells += M.t12_expected(expected, targets)
    cells += M.t09(read_series(paths["p07-RpmFirst"]), targets)
    cells += trace_cells(kill, targets)
    for ref in targets.shipped.tc:
        snap = series.day(ref.tc)
        for kind in ("dps", "defense"):
            for key, value in getattr(ref, kind).items():
                pred = getattr(snap, kind).get(key, 0)
                cells.append(M.Cell("T15", kind, f"{ref.tc:03d}|{kind}|{key}", pred, value, pred-value,
                                    abs(pred/value-1) if value else (0.0 if pred == 0 else math.inf),
                                    stat=key, tc=ref.tc, split="train" if 1 <= ref.tc <= 24 else "holdout",
                                    note="actual recompute-items path; P12 R2b residuals in p12.csv"))
    # Raw exported snapshot evaluation is kept independently; never disguise it as G20.
    ev = M.evaluate(cells, M.load_tolerances(), "open")
    detail["raw_export_status"] = ev.targets
    detail["t12_daily"] = {
        "integer_5pct": [dataclasses.asdict(c) for c in ev.cells if c.target == "T12" and c.group == "daily"],
        "pre_roll": [dataclasses.asdict(c) for c in ev.cells if c.target == "T12" and c.group == "daily_expected"],
        "contract": "pre-roll expected copies: 5% + <1 integer copy per active item stream; phase limits unchanged",
        "evidence": "review: identical 18/29 integer failures across Chain mappings, Greedy, slots-3-8 and selected-items; U expectation 14.06-14.72 vs shipped 15,14,15,12,12,14,14; no theta fitting",
    }
    detail["t12_daily"]["hypothesis_integer_checks"] = {}
    for name, directory in ((n, paths[n]) for n in ("copies-Chain-Swapped", "copies-Chain-Direct",
                                                   "evolution-Greedy", "slots-3-8")):
        daily = [c for c in M.t12(read_series(directory), targets) if c.group == "daily" and c.ref]
        detail["t12_daily"]["hypothesis_integer_checks"][name] = {
            "cells": len(daily), "failures": sum(c.rel > .05 for c in daily),
            "sum_absolute_residual": sum(abs(c.residual) for c in daily)}
    detail["t12_daily"]["uncommon_weapon_example"] = [
        {"tc": c.tc, "expected": c.sim, "shipped": c.ref, "allowance": c.bound}
        for c in ev.cells if c.target == "T12" and c.group == "daily_expected" and c.key.endswith("|weapon|Uncommon")]
    detail["metric_basis"] = {"T04_T05": "selected-kill fast rewards and active minutes; never overwritten snapshot deltas",
                               "T07_T08": "selected-kill unlock / first-kill traces",
                               "T09_export": "p07-RpmFirst raw export; day0, snapshot items, stored caches, snapshot unlock, post-override RPM skip",
                               "T09_G20": "independent Python oracle on shipped states (not exported run)",
                               "T12_T13_T17": "selected-items (from day 0, recompute items, real slot rule)"}
    t04_cells = [c for c in cells if c.target == "T04" and c.group == "tc_ge_11"]
    detail["t04_confirmed"] = {"median": statistics.median(c.rel for c in t04_cells),
                               "max": max(c.rel for c in t04_cells), "n": len(t04_cells)}
    t09_cells = [c for c in cells if c.target == "T09"]
    detail["t09_exported"] = {"hits": sum(sc.gpm_matches(c.ref, c.sim) for c in t09_cells), "cells": len(t09_cells)}
    # K1 round-2 review: G20's cell test is not bit equality.
    import struct
    bits = lambda x: struct.unpack(">Q", struct.pack(">d", x))[0]
    detail["t09_precision"] = {
        "cell_test": "G20 gpm_matches: absolute/relative 1e-4 cell test",
        "bit_exact": sum(bits(c.ref) == bits(c.sim) for c in t09_cells),
        "max_ulp": max(abs(bits(c.ref)-bits(c.sim)) for c in t09_cells),
        "non_tc_contract": "0 changed is by construction: sampled-TC maximum only",
        "review_counterfactual": {"all_days_non_tc_raised_cells": 5336, "g20_hits": 1380},
        "counterfactual_source": "review_K1_r2.json; independent all-406-days replay maximum",
    }
    detail["t12_daily"]["allowance_basis"] = (
        "simulator Carry rounding (C3c), not a statistical model of the shipped generator; "
        "weakly discriminating (absorbs day-10 mace +2); unchanged phase checks bind")
    rpm_series = read_series(paths["p07-RpmFirst"])
    observed = set(targets.shipped.tc_days)
    interpolated = [s for s in rpm_series.full if s.tc not in observed]
    detail["t09_interpolated_max"] = {
        "days": len(interpolated), "cells": sum(len(s.stats) for s in interpolated),
        "changed_cells": sum(s.stats[k]["CurrentGpm"] != rpm_series.day(s.tc-1).stats[k]["CurrentGpm"]
                             for s in interpolated for k in s.stats)}
    t05 = t05_evidence(db, kill, shipped)
    detail["t05"] = t05
    detail["mace_eligibility"] = mace_eligibility(db, kill)
    detail["mace_eligibility"]["first_item_day_by_probe"] = {
        name: next((d.day for d in fast_days(directory) if d.items.get("mace", 0) > 0), None)
        for name, directory in paths.items() if name.startswith(("kill-", "theta-", "dps-"))}
    diag_rows, detail["sensitivity"] = diagnostic_rows(paths, shipped)
    detail["t07"] = S.t07(S.unlock_days(events(kill)))
    detail["p07"] = {}
    for name, directory in (("RpmFirst", paths["p07-RpmFirst"]),
                            *((o, paths[f"p07-{o}"]) for o in ("ModifierFirst", "StaleTickCache"))):
        cs = M.t09(read_series(directory), targets)
        detail["p07"][name] = {"hits": sum(sc.gpm_matches(c.ref, c.sim) for c in cs), "cells": len(cs),
                              "mean_rel": mean(c.rel for c in cs if c.ref)}
        residuals[f"p07-{name}"] = [dataclasses.asdict(c) for c in cs]
    coefficient_rows, platform = coefficient_ablation(db, paths["copies-Chain-Swapped"], targets)
    residuals["coefficient"] = coefficient_rows
    detail["late_platform"] = platform
    coefficient_scores = {name: (mean(r["rel"] for r in coefficient_rows if r["candidate"] == name), None)
                          for name in ("full", "no_K0_K6")}
    hypotheses += ranking("EmissionCoefficient", coefficient_scores, post_hoc=True,
                          segment="Full phases with K0>0.1; same measured clear set; diagnostic no-K0/K6")
    headroom = inventory_data(item)["headroom"]
    margins = [{"item": r["key"], "level": r["level"], "target_day": r["target"], "reached_tc": r["day"],
                "copies_margin": r["headroom"], "shards_margin": r["copies"]-r["shards"],
                "note": "first processing on/after target, not necessarily at target"} for r in headroom]
    bridge_cells = trace_cells(bridge, targets)
    stored_cells = trace_cells(kill, targets)
    detail["bridge"] = {"stored_bad": len(S.bad_intervals(stat_intervals(kill, shipped))),
                        "recomputed_bad": len(S.bad_intervals(stat_intervals(bridge, shipped))),
                        "stored_t04_mean_rel": mean(c.rel for c in stored_cells if c.target == "T04" and c.group == "tc_ge_11"),
                        "recomputed_t04_mean_rel": mean(c.rel for c in bridge_cells if c.target == "T04" and c.group == "tc_ge_11")}
    residuals["bridge"] = [dict(dataclasses.asdict(c), basis="recomputed") for c in bridge_cells]
    residuals["bridge"] += [dict(dataclasses.asdict(c), basis="stored") for c in stored_cells]
    detail["design_failures"] = [f"{g.target}.{g.group}: {g.detail}" for g in ev.groups if g.status == "fail"]
    detail["design_failures"] += [f"T08.{k}: {v}" for k, v in t05["first_kills"].items() if not v["ok"]]
    detail["design_failures"] += [f"T12.platform.{k}: rel={v['rel']}" for k, v in platform.items() if v["rel"] > .02]
    detail["design_failures"] += [f"holdout {k} exceeds 1.5x" for k, v in detail["holdout"].items() if not v["pass"]]
    detail["t05_unexplained_cap"] = len(t05["unexplained"])
    detail["jobs"] = {}
    for name, path in sorted({**paths, **selected}.items()):
        m = json.loads((path / "manifest.json").read_text())
        guard = json.loads((path / "asset_guard.json").read_text())["after_job"]
        if m["exit_code"] != 0 or not guard["ok"]:
            raise ValueError(f"invalid measurement: {path}")
        detail["jobs"][name] = {"directory": str(path), "code_rev": m.get("code_rev"),
                               "manifest_sha256": report.sha256_file(path / "manifest.json"),
                               "asset_guard_sha256": report.sha256_file(path / "asset_guard.json"),
                               "timings": json.loads((path / "timings.json").read_text())}
    resolved = P.resolve(["defaults"], changes)
    out = report.write(out_root, "open", evaluations=[ev], params=resolved, hypotheses=hypotheses,
                       sensitivity=diag_rows, evolution_margin=margins,
                       inputs={"database_sha256": report.sha256_file(os.environ.get("TUFU_BALANCE_DB", BALANCE / "out/balance.sqlite")),
                               "tolerances_sha256": report.sha256_file(M.TOLERANCES),
                               "job_manifests": {k: v["manifest_sha256"] for k, v in detail["jobs"].items()}})
    for name, rows in residuals.items():
        if rows:
            (out / (name + ".csv")).write_text(report.csv_text(tuple(rows[0]), rows))
    write_json(out / "evidence.json", detail)
    write_json(out / "open.json", changes)
    for name in ("initial.json", "selected.json", *(p.name for p in sorted(workspace.glob("*.timing.json"))),
                 *(p.name for p in sorted(workspace.glob("*.audit.json"))),
                 *(p.name for p in sorted(workspace.glob("*.diagnosis.txt")))):
        source = workspace / name
        if source.exists():
            (out / name).write_bytes(source.read_bytes())
    # Additional evidence is content-addressed alongside the P10 report.
    manifest = json.loads((out / "manifest.json").read_text())
    manifest["evidence_files"] = {p.name: report.sha256_file(p) for p in sorted(out.iterdir())
                                  if p.name != "manifest.json" and p.name not in manifest["files"]}
    write_json(out / "manifest.json", manifest)
    write_json(workspace / "latest.json", {"report": str(out), "params": changes})
    return out, detail

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
        if args.rev:
            from ..unity import runner
            if drv.backend.identity().code_rev != runner.resolve(runner.WORKTREE, args.rev):
                raise RuntimeError("resident editor revision differs from --rev; stop/restart it through the launcher")
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
        selected_path = args.workspace / "selected.json"
        selected = load_results(selected_path) if args.analyze_only else measure(selected_jobs(db, changes), drv, selected_path)
        out, detail = archive(db, paths, selected, changes, hypotheses, detail, residuals, args.out, args.workspace)
        print(json.dumps({"report": str(out), "choice": changes, "holdout": detail["holdout"],
                          "failures": detail["design_failures"], "wall_s": time.monotonic()-started}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
