"""Stage 4 / track "bake": re-run the recovered spawner bake on the shipped Simulation Data.

For every EnemySpawner in Main.unity this script recomputes Health and Damage with
``tufu_balance.model.bake`` (a faithful port of SimulationConfig.BuildSpawnerBakeReport at
commit 6af011f), compares them with the scene values (``stat.Health`` / ``stat.Damage``) and with
the stored ``SpawnerBakeInfos`` (``bake.*``), and runs a set of hypothesis tests on the mismatches.

Run from ``Balance/``::

    .venv/bin/python analysis/bake_check.py

Outputs (``Balance/out/analysis/bake/``):

* ``spawners.csv``       one row per spawner (prediction, scene, ratios, flags, implied times).
* ``groups.csv``         spawners sharing (mode, bake time, chosen armor type): scene/model HP ratio.
* ``summary.json``       every number quoted in Docs/Balance/Generation/BakeCheck.md.

Deterministic: reads only ``out/balance.sqlite``; no randomness; rows sorted by key.
"""

from __future__ import annotations

import csv
import dataclasses
import json
import math
import statistics
import sys
from collections import Counter, defaultdict
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from tufu_balance.model import bake as bake_mod  # noqa: E402
from tufu_balance.model import data  # noqa: E402
from tufu_balance.model import interpolation as interp_mod  # noqa: E402
from tufu_balance.model import snapshots as snap_mod  # noqa: E402
from tufu_balance.model.enums import WeaponType  # noqa: E402
from tufu_balance.model.f32 import f32  # noqa: E402

OUT = ROOT / "out" / "analysis" / "bake"
TOL1, TOL5 = 0.01, 0.05


# ----------------------------------------------------------------------------- helpers

def ratio(scene: float, pred: float) -> float:
    if math.isinf(scene) and math.isinf(pred):
        return 1.0
    if pred == 0 or math.isnan(pred) or math.isinf(pred) or math.isinf(scene):
        return math.nan
    return scene / pred


def match_class(scene: float, pred: float) -> str:
    if scene == pred:
        return "exact"
    r = ratio(scene, pred)
    if math.isnan(r):
        return "other"
    if abs(r - 1) <= TOL1:
        return "within1"
    if abs(r - 1) <= TOL5:
        return "within5"
    return "other"


def counts(classes: list[str]) -> dict:
    c = Counter(classes)
    n = len(classes)
    exact = c["exact"]
    w1 = exact + c["within1"]
    w5 = w1 + c["within5"]
    return {"n": n, "exact": exact, "within1": w1, "within5": w5, "beyond5": n - w5}


def pct(values, q):
    v = [x for x in values if x is not None and math.isfinite(x)]
    return float(np.percentile(v, q)) if v else None


def ratio_stats(values) -> dict:
    v = [x for x in values if x is not None and math.isfinite(x)]
    if not v:
        return {"median": None, "p10": None, "p90": None, "min": None, "max": None}
    return {"median": float(statistics.median(v)), "p10": pct(v, 10), "p90": pct(v, 90),
            "min": float(min(v)), "max": float(max(v))}


def sanitize(o):
    """JSON-safe copy: non-finite floats become strings, numpy scalars become Python numbers."""
    if isinstance(o, dict):
        return {str(k): sanitize(v) for k, v in o.items()}
    if isinstance(o, (list, tuple)):
        return [sanitize(v) for v in o]
    if isinstance(o, (np.floating, float)):
        o = float(o)
        return o if math.isfinite(o) else fmt(o)
    if isinstance(o, np.integer):
        return int(o)
    return o


def category(r) -> str:
    name = str(r["go_name"])
    low = name.lower()
    if int(r["IsEvent"] or 0) or low.startswith("event"):
        return "event"
    if int(r["IsDummy"] or 0):
        return "dummy"
    if "door" in low or "blocker" in low:
        return "blocker"
    if low.startswith("bosstower"):
        return "boss_tower"
    if low.startswith("tree"):
        return "tree"
    if int(r["IsVip"] or 0):
        return "vip"
    if "boss" in low or "death" in low:
        return "boss"
    if "elite" in low:
        return "elite"
    if "knight" in low or "dragon" in low or "hard" in low:
        return "knight_dragon_hard"
    return "regular"


def fmt(x):
    if x is None:
        return ""
    if isinstance(x, float):
        if math.isnan(x):
            return "nan"
        if math.isinf(x):
            return "inf" if x > 0 else "-inf"
        return repr(x)
    return str(x)


# ----------------------------------------------------------------------------- main bake run

def run_bake(ctx, spawners):
    out = []
    for r, sp in spawners:
        rep = bake_mod.bake_spawner(ctx, sp)
        out.append(rep)
    return out


def implied_time(ctx, sp, attr, target, lo=0.0, hi=405.0, steps=40):
    """Day-mode bake time t' (no early shift) at which the shipped data reproduce ``target``.

    Uses the same bake path (GetByTimeChunkIndex + ability curves at day=t'). Bisection assumes the
    raw value is non-decreasing in t' (checked on the bracket ends only)."""
    def f(t):
        return getattr(bake_mod.build_spawner_bake_report(ctx, sp, t, t, True), attr)
    if not math.isfinite(target):
        return math.nan
    if f(lo) >= target:
        return lo
    if f(hi) < target:
        return math.inf
    for _ in range(steps):
        m = (lo + hi) / 2
        if f(m) < target:
            lo = m
        else:
            hi = m
    return (lo + hi) / 2


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    con = data.connect()
    ctx = data.load_bake_context(con)
    armors = data.load_armor_configs(con)
    rows = data.load_spawner_rows(con)
    spawners = [(r, data.spawner_input_from_row(r, armors)) for r in rows]
    reports = run_bake(ctx, spawners)
    sd = ctx.simulation_data
    stored_chunks = sorted({s.time_chunk_index for s in sd.time_chunk_snapshots})
    summary: dict = {}

    # ---- data facts used by the report
    tcs = sd.time_chunk_snapshots
    summary["data_facts"] = {
        "n_spawners": len(spawners),
        "n_minute_mode": sum(1 for _, sp in spawners if not sp.is_days),
        "n_day_mode": sum(1 for _, sp in spawners if sp.is_days),
        "n_with_bake_info": sum(1 for r, _ in spawners if r["bake.Health"] is not None),
        "n_bake_infos": con.execute("SELECT COUNT(*) FROM c_SimulationConfig__SpawnerBakeInfos").fetchone()[0],
        "n_playtime_snapshots": len(sd.snapshots),
        "playtime_snapshots_max_minutes": float(max(s.playtime for s in sd.snapshots)),
        "n_time_chunk_snapshots": len(tcs),
        "stored_chunk_indices": stored_chunks,
        "chunk_index_equals_floor_totalplaytime_over_1440": sum(
            1 for s in tcs if int(math.floor(float(s.total_playtime) / 1440.0)) == s.time_chunk_index),
        "playtime_snapshots_chunk_equals_floor_total_over_1440": sum(
            1 for s in sd.snapshots if int(math.floor(float(s.total_playtime) / 1440.0)) == s.time_chunk_index),
        "playtime_minutes_at_chunk": {str(s.time_chunk_index): float(s.playtime) for s in tcs
                                       if s.time_chunk_index in (1, 4, 10, 21, 51, 104, 206, 405)},
        "early_offset": {"start_location": ctx.early_offset_start_location,
                         "relative_part": float(ctx.early_offset_relative_part),
                         "multiplier": float(ctx.early_offset_multiplier)},
        "config_snapshot_lists_empty": data.config_snapshot_lists_empty(con),
        "duplicate_spawner_keys": [list(x) for x in con.execute(
            "SELECT Key, COUNT(*) FROM spawners GROUP BY Key HAVING COUNT(*) > 1 ORDER BY Key").fetchall()],
        "duplicate_bake_info_keys": [list(x) for x in con.execute(
            "SELECT Key, COUNT(*) FROM c_SimulationConfig__SpawnerBakeInfos GROUP BY Key HAVING COUNT(*) > 1 "
            "ORDER BY Key").fetchall()],
        "AutoBalanceItems": int(con.execute("SELECT AutoBalanceItems FROM c_SimulationConfig").fetchone()[0]),
        "n_items": con.execute("SELECT COUNT(*) FROM items").fetchone()[0],
        "items_dps_multiplier_not_1_by_damage_type": {
            WeaponType(int(t)).name: sorted(float(v) for v in vs.split(","))
            for t, vs in con.execute("SELECT DamageType, group_concat(SimulationDpsWeaponMultiplier) FROM items "
                                     "WHERE SimulationDpsWeaponMultiplier <> 1 GROUP BY DamageType ORDER BY DamageType")},
        "SpawnerChargesChunkMinutes": float(con.execute("SELECT SpawnerChargesChunkMinutes FROM c_SimulationConfig").fetchone()[0]),
        "TimeChunkDurationMinutes": float(con.execute("SELECT TimeChunkDurationMinutes FROM c_SimulationConfig").fetchone()[0]),
        "ability_curves": [{"name": it.name, "type": it.type, "timing": it.timing_points, "coef": it.coef_points}
                           for it in ctx.interpolations.interpolations if it.type in (4, 5) and it.use],
        "ability_dps_at_day": {str(d): float(ctx.interpolations.get_interpolation_coefficient(f32(d), 4))
                               for d in (4, 20, 50, 90, 150, 180)},
        "ability_ehp_at_day": {str(d): float(ctx.interpolations.get_interpolation_coefficient(f32(d), 5))
                               for d in (4, 20, 50, 90, 150, 180)},
    }

    # ---- per-spawner rows
    recs = []
    for (r, sp), rep in zip(spawners, reports):
        hp_s, d_s = sp.health_base, sp.damage_base
        rec = {
            "key": sp.key, "name": sp.name, "location": sp.location_number, "category": category(r),
            "is_days": int(sp.is_days), "optimum_time": float(sp.optimum_time),
            "relative_optimum_time": float(sp.relative_optimum_time),
            "recalc_time": rep.recalc_optimum_time, "early_shift": float(sp.optimum_time) - rep.recalc_optimum_time,
            "bake_day": rep.day,
            "snapshot": (f"P{rep.snapshot_source_index}" if not sp.is_days else
                         (f"C{rep.snapshot_source_index}" if rep.snapshot_source_index is not None
                          else f"interp@{rep.recalc_optimum_time:g}")),
            "at_stored_chunk": int(sp.is_days and rep.recalc_optimum_time in stored_chunks),
            "armor_config": r["ArmorConfig"], "chosen_armor_type": WeaponType(rep.armor_type).name,
            "resist_used": rep.armor_resist_used, "preferred_weapon_type": WeaponType(rep.preferred_weapon_type).name,
            "count": sp.count, "average_fight_time": float(sp.average_fight_time),
            "difficulty": float(sp.difficulty), "attack_cooldown": sp.attack_cooldown_base,
            "pred_health_raw": rep.new_health_raw, "pred_health": rep.new_health, "scene_health": hp_s,
            "health_ratio": ratio(hp_s, rep.new_health), "health_match": match_class(hp_s, rep.new_health),
            "pred_damage_raw": rep.new_damage_raw, "pred_damage": rep.new_damage, "scene_damage": d_s,
            "damage_ratio": ratio(d_s, rep.new_damage), "damage_match": match_class(d_s, rep.new_damage),
            "bake_OptimumTime": None if r["bake.OptimumTime"] is None else float(r["bake.OptimumTime"]),
            "bake_Health": None if r["bake.Health"] is None else float(r["bake.Health"]),
            "bake_Damage": None if r["bake.Damage"] is None else float(r["bake.Damage"]),
            "bake_PrevHealth": None if r["bake.PrevHealth"] is None else float(r["bake.PrevHealth"]),
            "bake_PrevDamage": None if r["bake.PrevDamage"] is None else float(r["bake.PrevDamage"]),
            "HandBalance": int(r["HandBalance"] or 0), "IsEvent": int(r["IsEvent"] or 0),
            "IsVip": int(r["IsVip"] or 0), "IsDummy": int(r["IsDummy"] or 0),
            "IsEliteBoss": int(r["IsEliteBoss"] or 0), "IsSpecial": int(r["IsSpecial"] or 0),
            "IsBossFightPart": int(r["IsBossFightPart"] or 0), "PriorityMultiplier": float(r["PriorityMultiplier"] or 0),
        }
        recs.append((rec, r, sp, rep))

    # ---- implied times (day-mode, L>=2, real spawners)
    for rec, r, sp, rep in recs:
        rec["implied_t_health"] = None
        rec["implied_t_damage"] = None
        if sp.is_days and sp.count > 0 and not rec["HandBalance"] and sp.location_number >= 2:
            rec["implied_t_health"] = implied_time(ctx, sp, "new_health_raw", sp.health_base)
            if sp.damage_base > 10:
                rec["implied_t_damage"] = implied_time(ctx, sp, "new_damage_raw", sp.damage_base)

    # ---- explanation code per spawner
    for rec, r, sp, rep in recs:
        hm, dm = rec["health_match"], rec["damage_match"]
        if hm == "exact" and dm == "exact":
            code = "exact"
        elif rec["HandBalance"]:
            code = "hand_balance"
        elif rec["category"] == "event":
            code = "event_runtime_override"
        elif rec["IsDummy"]:
            code = "dummy_hand_set"
        elif not sp.is_days and dm == "exact":
            code = "minute_dps_divergence"
        elif not sp.is_days:
            code = "minute_other"
        elif math.isfinite(sp.health_base) and sp.health_base >= 10000 and sp.health_base % 10000 == 0:
            code = "day_round_hand_tuned"
        else:
            code = "day_source_b"
        rec["explanation"] = code

    # ---- write spawners.csv
    recs.sort(key=lambda x: (x[0]["location"], x[0]["is_days"], x[0]["optimum_time"], x[0]["key"], x[0]["name"]))
    cols = list(recs[0][0].keys())
    with open(OUT / "spawners.csv", "w", newline="", encoding="utf-8") as fh:
        w = csv.writer(fh)
        w.writerow(cols)
        for rec, *_ in recs:
            w.writerow([fmt(rec[c]) for c in cols])

    R = [x[0] for x in recs]

    # ---- summaries
    def summarize(sel):
        sel = list(sel)
        return {
            "health": counts([x["health_match"] for x in sel]),
            "damage": counts([x["damage_match"] for x in sel]),
            "both_exact": sum(1 for x in sel if x["health_match"] == "exact" and x["damage_match"] == "exact"),
            "health_ratio": ratio_stats([x["health_ratio"] for x in sel]),
            "damage_ratio": ratio_stats([x["damage_ratio"] for x in sel]),
        }

    summary["overall"] = summarize(R)
    summary["by_mode"] = {"minutes": summarize(x for x in R if not x["is_days"]),
                          "days": summarize(x for x in R if x["is_days"])}
    summary["by_location"] = {f"{L:02d}": summarize(x for x in R if x["location"] == L)
                              for L in sorted({x["location"] for x in R})}
    summary["by_category"] = {c: summarize(x for x in R if x["category"] == c)
                              for c in sorted({x["category"] for x in R})}
    summary["by_location_mode"] = {f"{L:02d}_{'days' if d else 'minutes'}": summarize(
        x for x in R if x["location"] == L and x["is_days"] == d)
        for L, d in sorted({(x["location"], x["is_days"]) for x in R})}
    summary["explanations"] = dict(sorted(Counter(x["explanation"] for x in R).items()))
    summary["flags"] = {
        f: {"n": sum(1 for x in R if x[f]),
            "health_exact": sum(1 for x in R if x[f] and x["health_match"] == "exact"),
            "damage_exact": sum(1 for x in R if x[f] and x["damage_match"] == "exact")}
        for f in ("HandBalance", "IsEvent", "IsVip", "IsDummy", "IsEliteBoss", "IsSpecial", "IsBossFightPart")}
    summary["difficulty_fallback"] = {
        "n_difficulty_le_0": sum(1 for x in R if x["difficulty"] <= 0),
        "keys": [x["name"] for x in R if x["difficulty"] <= 0]}
    summary["named_exceptions"] = [
        {k: x[k] for k in ("name", "location", "is_days", "optimum_time", "scene_health", "pred_health",
                           "scene_damage", "pred_damage", "bake_Health", "bake_Damage", "bake_PrevHealth",
                           "HandBalance", "explanation")}
        for x in R if x["explanation"] in ("hand_balance", "event_runtime_override", "dummy_hand_set", "minute_other",
                                           "day_round_hand_tuned")
        or x["count"] <= 0]
    summary["minute_dps_divergence"] = sorted(
        [{"name": x["name"], "location": x["location"], "optimum_time": x["optimum_time"],
          "health_ratio": x["health_ratio"], "chosen_armor_type": x["chosen_armor_type"]}
         for x in R if x["explanation"] == "minute_dps_divergence"], key=lambda d: (d["optimum_time"], d["name"]))
    dm_ = [x for x in R if x["is_days"] and x["explanation"] in ("day_source_b", "exact")]
    summary["day_mode_by_bake_day"] = {
        f"{d:03d}": {"health": counts([x["health_match"] for x in dm_ if int(x["recalc_time"]) == d]),
                     "damage": counts([x["damage_match"] for x in dm_ if int(x["recalc_time"]) == d])}
        for d in sorted({int(x["recalc_time"]) for x in dm_})}
    summary["day_mode_by_bake_band"] = {
        f"[{lo},{hi})": {"health": counts([x["health_match"] for x in dm_ if lo <= x["recalc_time"] < hi]),
                         "damage": counts([x["damage_match"] for x in dm_ if lo <= x["recalc_time"] < hi]),
                         "health_ratio": ratio_stats([x["health_ratio"] for x in dm_ if lo <= x["recalc_time"] < hi]),
                         "damage_ratio": ratio_stats([x["damage_ratio"] for x in dm_ if lo <= x["recalc_time"] < hi])}
        for lo, hi in ((4, 5), (5, 7), (7, 20), (20, 60), (60, 120), (120, 181))}
    mins = [x for x in R if not x["is_days"] and x["explanation"] not in ("event_runtime_override", "dummy_hand_set", "hand_balance")]
    summary["minute_mode_exact_health_max_ot"] = max(x["optimum_time"] for x in mins if x["health_match"] == "exact")
    summary["minute_mode_first_health_mismatch_ot"] = min(x["optimum_time"] for x in mins if x["health_match"] != "exact")
    summary["minute_mode_health_exact_by_ot_band"] = {
        band: counts([x["health_match"] for x in mins if lo <= x["optimum_time"] < hi])
        for band, lo, hi in (("0-120", 0, 120.0001), ("121-240", 120.0001, 1e9))}

    # ---- H1: structural consistency of the scene HP (group test)
    def group_test(value_of, recs_sel, label):
        """Spawners sharing (mode, bake time, chosen armor type) see the same player state, so
        scene_HP / raw_HP must be one common factor. HP is an integer, so each member constrains the
        factor to [(v-0.5)/raw, (v+0.5)/raw]; a group is consistent if these intervals intersect."""
        g = defaultdict(list)
        for rec, r, sp, rep in recs_sel:
            v = value_of(rec, r, sp, rep)
            if v is None or not math.isfinite(v) or rep.new_health_raw <= 0 or not math.isfinite(rep.new_health_raw):
                continue
            raw = rep.new_health_raw
            g[(rec["is_days"], round(rep.recalc_optimum_time, 4), rec["chosen_armor_type"])].append(
                (v / raw, (v - 0.5) / raw, (v + 0.5) / raw, rec["name"], rec["location"],
                 (float(sp.average_fight_time), sp.count, float(rep.armor_resist_used))))
        out_rows = []
        n_cons = 0
        spreads = []
        for k, v in sorted(g.items()):
            xs = [a[0] for a in v]
            lo = max(a[1] for a in v)
            hi = min(a[2] for a in v)
            consistent = lo <= hi * (1 + 1e-12)
            med = statistics.median(xs)
            breakers = []
            if not consistent:
                # members whose interval misses the factor shared by the largest number of members
                best = max((sum(1 for b in v if b[1] <= a[0] <= b[2]), a[0]) for a in v)[1]
                breakers = [f"{a[3]}(L{a[4]})" for a in v if not (a[1] <= best <= a[2])]
            if len(v) > 1:
                n_cons += consistent
                spreads.append(max(xs) / min(xs) - 1)
            n_inputs = len({a[5] for a in v})
            out_rows.append({"source": label, "mode": "days" if k[0] else "minutes", "bake_time": k[1],
                             "armor_type": k[2], "n": len(v), "n_distinct_inputs": n_inputs,
                             "islands": ";".join(str(L) for L in sorted({a[4] for a in v})),
                             "ratio_median": med,
                             "ratio_min": min(xs), "ratio_max": max(xs), "spread": max(xs) / min(xs) - 1,
                             "consistent_within_rounding": int(consistent),
                             "inconsistent_members": ";".join(breakers)})
        multi = [o for o in out_rows if o["n"] > 1]
        # a group whose members all have the same (AverageFightTime, Count, resist_used) passes trivially:
        # identical inputs give identical raw HP. Only groups with >= 2 distinct input tuples test the formula.
        nontriv = [o for o in multi if o["n_distinct_inputs"] > 1]
        return {
            "n_groups": len(g), "n_groups_multi": len(multi),
            "n_singleton_groups": len(out_rows) - len(multi),
            "n_spawners_in_multi": sum(o["n"] for o in multi),
            "multi_consistent_within_rounding": n_cons,
            "n_groups_nontrivial": len(nontriv),
            "n_spawners_in_nontrivial": sum(o["n"] for o in nontriv),
            "n_independent_constraints": sum(o["n_distinct_inputs"] - 1 for o in nontriv),
            "nontrivial_consistent_within_rounding": sum(o["consistent_within_rounding"] for o in nontriv),
            "n_groups_multi_island": sum(1 for o in multi if ";" in o["islands"]),
            "multi_island_groups": [{k: o[k] for k in ("bake_time", "armor_type", "n", "islands",
                                                      "consistent_within_rounding")}
                                    for o in multi if ";" in o["islands"]],
            "multi_spread_median": float(statistics.median(spreads)) if spreads else None,
            "outliers": [{k: o[k] for k in ("mode", "bake_time", "armor_type", "n", "inconsistent_members")}
                         for o in multi if not o["consistent_within_rounding"]],
        }, out_rows

    real = [x for x in recs if x[2].count > 0 and not x[0]["HandBalance"] and x[0]["category"] not in ("event", "dummy")]
    h1_scene, grows = group_test(lambda rec, r, sp, rep: sp.health_base, [x for x in real if x[0]["is_days"]], "scene_days")
    h1_scene_min, grows_min = group_test(lambda rec, r, sp, rep: sp.health_base, [x for x in real if not x[0]["is_days"]], "scene_minutes")

    # bake infos (use their own OptimumTime)
    def bakeinfo_recs(field):
        out = []
        for rec, r, sp, rep in real:
            if r["bake.Health"] is None:
                continue
            sp2 = dataclasses.replace(sp, optimum_time=f32(float(r["bake.OptimumTime"])))
            rep2 = bake_mod.bake_spawner(ctx, sp2)
            out.append((dict(rec, chosen_armor_type=WeaponType(rep2.armor_type).name), r, sp2, rep2))
        return out
    bi = bakeinfo_recs("bake.Health")
    h1_bake, _ = group_test(lambda rec, r, sp, rep: float(r["bake.Health"]), bi, "bakeinfo_Health")
    h1_prev, _ = group_test(lambda rec, r, sp, rep: float(r["bake.PrevHealth"]), bi, "bakeinfo_PrevHealth")
    summary["H1_group_consistency"] = {"scene_days": h1_scene, "scene_minutes": h1_scene_min,
                                       "bakeinfo_Health": h1_bake, "bakeinfo_PrevHealth": h1_prev}
    gcols = list(grows[0].keys())
    with open(OUT / "groups.csv", "w", newline="", encoding="utf-8") as fh:
        w = csv.writer(fh)
        w.writerow(gcols)
        for g in grows_min + grows:
            w.writerow([fmt(g[c]) for c in gcols])

    # cross-island continuity of the (time, weapon type) factor: if each island had been baked from a
    # different player state, groups of the same type at nearly the same time but on different islands would
    # disagree more than groups on the same island.
    # Groups (incl. singletons, hand-tuned excluded) of the same chosen type whose bake times differ by <= w days.
    gd = [g for g in grows if math.isfinite(g["ratio_median"])]
    h1b = {}
    for w in (0.25, 0.5, 1.0):
        cont = {"same_island": [], "cross_island": []}
        pair_by_islands = defaultdict(list)
        for i in range(len(gd)):
            for j in range(i + 1, len(gd)):
                a, b = gd[i], gd[j]
                if a["armor_type"] != b["armor_type"] or abs(a["bake_time"] - b["bake_time"]) > w:
                    continue
                ia, ib = set(a["islands"].split(";")), set(b["islands"].split(";"))
                d = abs(math.log(a["ratio_median"] / b["ratio_median"]))
                if ia & ib:
                    cont["same_island"].append(d)
                else:
                    cont["cross_island"].append(d)
                    pair_by_islands["/".join(sorted(ia | ib, key=int))].append(d)
        h1b[f"window_{w:g}d"] = {
            **{k: {"n_pairs": len(v), "median_abs_log_diff": float(statistics.median(v)) if v else None,
                   "p90_abs_log_diff": pct(v, 90)} for k, v in cont.items()},
            "cross_island_by_islands": {k: {"n_pairs": len(v), "median_abs_log_diff": float(statistics.median(v))}
                                        for k, v in sorted(pair_by_islands.items())}}
    summary["H1b_cross_island_continuity"] = h1b

    # implied scene/shipped player-power ratio per (minute-mode time, chosen weapon type)
    summary["minute_groups_power_ratio"] = [
        {"ot_minutes": g["bake_time"], "armor_type": g["armor_type"], "n": g["n"], "ratio_median": g["ratio_median"],
         "consistent_within_rounding": g["consistent_within_rounding"]}
        for g in grows_min if g["bake_time"] >= 120]
    summary["implied_scene_over_shipped_power_by_island_days"] = {}
    for L in range(4, 18):
        xs = [x["health_ratio"] for x in R if x["is_days"] and x["location"] == L and math.isfinite(x["health_ratio"])
              and x["explanation"] == "day_source_b"]
        summary["implied_scene_over_shipped_power_by_island_days"][f"{L:02d}"] = ratio_stats(xs)

    # Damage is rounded to multiples of 10, so an exact Damage only pins the defensive need to +-5 (relative 5/scene).
    def band(sel):
        b = [5.0 / x["scene_damage"] for x in sel if x["scene_damage"] > 0]
        return {"n": len(b), "min": min(b) if b else None, "max": max(b) if b else None,
                "median": float(statistics.median(b)) if b else None}
    mdd = [x for x in R if x["explanation"] == "minute_dps_divergence"]
    d47 = [x for x in R if x["is_days"] and 4 <= x["recalc_time"] < 7 and x["explanation"] in ("day_source_b", "exact")]
    summary["damage_rounding_band"] = {
        "minute_dps_divergence": band(mdd),
        "days_4_7_all": band(d47),
        "days_4_7_damage_exact": band([x for x in d47 if x["damage_match"] == "exact"]),
    }
    m120 = [x for x in R if not x["is_days"] and x["optimum_time"] > 120
            and x["explanation"] in ("exact", "minute_dps_divergence", "minute_other")]
    summary["minute_after_120_by_armor_type"] = {
        t: {"n": sum(1 for x in m120 if x["chosen_armor_type"] == t),
            "health_exact": sum(1 for x in m120 if x["chosen_armor_type"] == t and x["health_match"] == "exact"),
            "damage_exact": sum(1 for x in m120 if x["chosen_armor_type"] == t and x["damage_match"] == "exact"),
            "ot": sorted(x["optimum_time"] for x in m120 if x["chosen_armor_type"] == t)}
        for t in sorted({x["chosen_armor_type"] for x in m120})}
    summary["days_4_7_by_armor_type"] = {
        t: {"n": sum(1 for x in d47 if x["chosen_armor_type"] == t),
            "damage_exact": sum(1 for x in d47 if x["chosen_armor_type"] == t and x["damage_match"] == "exact"),
            "health_ratio": ratio_stats([x["health_ratio"] for x in d47 if x["chosen_armor_type"] == t]),
            "members": [f'{x["name"]}(L{x["location"]},{x["recalc_time"]:g}d,HP x{x["health_ratio"]:.4f},'
                        f'D {x["damage_match"]})' for x in d47 if x["chosen_armor_type"] == t]}
        for t in sorted({x["chosen_armor_type"] for x in d47})}

    # ---- H2: stored-chunk spawners (trimming cannot be the cause)
    sc = [x for x in R if x["at_stored_chunk"] and x["explanation"] != "hand_balance"]
    summary["H2_stored_chunk"] = {"n": len(sc), "health": counts([x["health_match"] for x in sc]),
                                  "damage": counts([x["damage_match"] for x in sc]),
                                  "health_ratio": ratio_stats([x["health_ratio"] for x in sc])}

    # ---- H3: global factor / per-island factor / time shift / time scale (day-mode, normal spawners)
    dayn = [x for x in recs if x[0]["is_days"] and x[0]["explanation"] == "day_source_b" and x[2].count > 0
            and x[0]["location"] >= 4]
    def within(vals, tol):
        return sum(1 for v in vals if math.isfinite(v) and abs(v - 1) <= tol)
    hr = [x[0]["health_ratio"] for x in dayn]
    dr = [x[0]["damage_ratio"] for x in dayn]
    kH = float(statistics.median([v for v in hr if math.isfinite(v)]))
    kD = float(statistics.median([v for v in dr if math.isfinite(v)]))

    def best_k(vals, lo, hi, step=0.0005):
        """Factor k on a grid maximising (#within 1%, #within 5%), ties to |k-1| smallest; k=1 is on the grid,
        so the optimum can never be worse than the baseline."""
        grid = [round(lo + step * i, 6) for i in range(int(round((hi - lo) / step)) + 1)]
        k = max(grid, key=lambda k: (within([v / k for v in vals], TOL1), within([v / k for v in vals], TOL5), -abs(k - 1)))
        return k, within([v / k for v in vals], TOL1), within([v / k for v in vals], TOL5)

    gkH, gh1, gh5 = best_k(hr, 0.95, 1.05)
    gkD, gd1, gd5 = best_k(dr, 0.95, 1.05)
    h3 = {"n": len(dayn),
          "baseline": {"health_w1": within(hr, TOL1), "health_w5": within(hr, TOL5),
                       "damage_w1": within(dr, TOL1), "damage_w5": within(dr, TOL5)},
          "global_factor_median": {"k_health": kH, "k_damage": kD,
                                   "health_w1": within([v / kH for v in hr], TOL1), "health_w5": within([v / kH for v in hr], TOL5),
                                   "damage_w1": within([v / kD for v in dr], TOL1), "damage_w5": within([v / kD for v in dr], TOL5)},
          "global_factor": {"grid": "0.95..1.05 step 0.0005, maximise within-1% count",
                            "k_health": gkH, "k_damage": gkD,
                            "health_w1": gh1, "health_w5": gh5, "damage_w1": gd1, "damage_w5": gd5}}
    # per-island factor (14 params each), optimised on a grid like the global one
    ph1 = ph5 = pd1 = pd5 = 0
    pk = {}
    for L in sorted({x[0]["location"] for x in dayn}):
        sel = [x[0] for x in dayn if x[0]["location"] == L]
        kh, a1, a5 = best_k([x["health_ratio"] for x in sel], 0.70, 1.40)
        kd, b1, b5 = best_k([x["damage_ratio"] for x in sel], 0.70, 1.40)
        ph1, ph5, pd1, pd5 = ph1 + a1, ph5 + a5, pd1 + b1, pd5 + b5
        pk[f"{L:02d}"] = {"n": len(sel), "k_health": kh, "k_damage": kd}
    h3["per_island_factor"] = {"grid": "0.70..1.40 step 0.0005 per island, maximise within-1% count",
                               "n_params_each": len({x[0]["location"] for x in dayn}),
                               "health_w1": ph1, "health_w5": ph5, "damage_w1": pd1, "damage_w5": pd5,
                               "factors": pk}

    def eval_shift(fn):
        h1 = h5 = d1 = d5 = he = de = 0
        for rec, r, sp, rep in dayn:
            t = fn(rep.recalc_optimum_time)
            rr = bake_mod.build_spawner_bake_report(ctx, sp, sp.optimum_time, f32(t), True)
            a, b = ratio(sp.health_base, rr.new_health), ratio(sp.damage_base, rr.new_damage)
            he += sp.health_base == rr.new_health
            de += sp.damage_base == rr.new_damage
            h1 += math.isfinite(a) and abs(a - 1) <= TOL1
            h5 += math.isfinite(a) and abs(a - 1) <= TOL5
            d1 += math.isfinite(b) and abs(b - 1) <= TOL1
            d5 += math.isfinite(b) and abs(b - 1) <= TOL5
        return {"health_exact": he, "health_w1": h1, "health_w5": h5, "damage_exact": de, "damage_w1": d1, "damage_w5": d5}

    shifts = [round(-2.0 + 0.1 * i, 1) for i in range(41)]
    shift_res = {s: eval_shift(lambda t, s=s: float(t) + s) for s in shifts}
    best_shift = max(shifts, key=lambda s: (shift_res[s]["health_w1"] + shift_res[s]["damage_w1"], -abs(s)))
    scales = [round(0.90 + 0.01 * i, 2) for i in range(21)]
    scale_res = {s: eval_shift(lambda t, s=s: float(t) * s) for s in scales}
    best_scale = max(scales, key=lambda s: (scale_res[s]["health_w1"] + scale_res[s]["damage_w1"], -abs(s - 1)))
    h3["time_shift_grid"] = {"grid": "-2.0..2.0 step 0.1 days", "best": best_shift, "best_result": shift_res[best_shift],
                             "zero_result": shift_res[0.0],
                             "all": {str(k): v for k, v in shift_res.items()}}
    h3["time_scale_grid"] = {"grid": "0.90..1.10 step 0.01", "best": best_scale, "best_result": scale_res[best_scale]}
    summary["H3_global_fits_days"] = h3

    # ---- H4: implied-time warp test (does one time shift explain both HP and damage of a spawner?)
    pairs = [(x["implied_t_health"] - x["recalc_time"], x["implied_t_damage"] - x["recalc_time"], x["location"])
             for x in R if x["implied_t_health"] is not None and x["implied_t_damage"] is not None
             and math.isfinite(x["implied_t_health"]) and math.isfinite(x["implied_t_damage"])
             and x["explanation"] == "day_source_b"]
    dH = np.array([p[0] for p in pairs])
    dD = np.array([p[1] for p in pairs])
    per_island = {}
    for L in sorted({p[2] for p in pairs}):
        m = np.array([p[2] == L for p in pairs])
        per_island[f"{L:02d}"] = {"n": int(m.sum()), "median_dt_health": float(np.median(dH[m])),
                                  "median_dt_damage": float(np.median(dD[m])),
                                  "corr": float(np.corrcoef(dH[m], dD[m])[0, 1]) if m.sum() > 2 else None}
    summary["H4_implied_time"] = {
        "n": len(pairs), "corr_dt_health_dt_damage": float(np.corrcoef(dH, dD)[0, 1]),
        "median_abs_dt_health": float(np.median(np.abs(dH))), "median_abs_dt_damage": float(np.median(np.abs(dD))),
        "p90_abs_dt_health": float(np.percentile(np.abs(dH), 90)), "p90_abs_dt_damage": float(np.percentile(np.abs(dD), 90)),
        "max_abs_dt_health": float(np.max(np.abs(dH))), "max_abs_dt_damage": float(np.max(np.abs(dD))),
        "median_abs_dt_health_minus_damage": float(np.median(np.abs(dH - dD))),
        "per_island": per_island,
        "n_islands_with_corr": sum(1 for v in per_island.values() if v["corr"] is not None),
        "n_islands_abs_corr_gt_0_5": sum(1 for v in per_island.values() if v["corr"] is not None and abs(v["corr"]) > 0.5),
        "n_islands_corr_gt_0_5": sum(1 for v in per_island.values() if v["corr"] is not None and v["corr"] > 0.5),
        "n_islands_corr_lt_minus_0_5": sum(1 for v in per_island.values() if v["corr"] is not None and v["corr"] < -0.5),
    }

    # ---- H5: SpawnerBakeInfos vs scene / prediction
    withb = [x for x in R if x["bake_Health"] is not None]
    def eq(a, b):
        return a is not None and b is not None and a == b
    h5 = {"n": len(withb)}
    for mode, sel in (("minutes", [x for x in withb if not x["is_days"]]), ("days", [x for x in withb if x["is_days"]])):
        h5[mode] = {
            "n": len(sel),
            "bake_OptimumTime_eq_scene_OT": sum(1 for x in sel if abs(x["bake_OptimumTime"] - x["optimum_time"]) < 1e-4),
            "bake_Health_eq_scene": sum(1 for x in sel if eq(x["bake_Health"], x["scene_health"])),
            "bake_Damage_eq_scene": sum(1 for x in sel if eq(x["bake_Damage"], x["scene_damage"])),
            "bake_Health_eq_pred": sum(1 for x in sel if eq(x["bake_Health"], x["pred_health"])),
            "bake_Damage_eq_pred": sum(1 for x in sel if eq(x["bake_Damage"], x["pred_damage"])),
            "PrevHealth_eq_scene": sum(1 for x in sel if eq(x["bake_PrevHealth"], x["scene_health"])),
            "PrevDamage_eq_scene": sum(1 for x in sel if eq(x["bake_PrevDamage"], x["scene_damage"])),
            "PrevHealth_eq_bake_Health": sum(1 for x in sel if eq(x["bake_PrevHealth"], x["bake_Health"])),
        }
    h5["scene_over_bakeinfo_health_by_island"] = {
        f"{L:02d}": ratio_stats([x["scene_health"] / x["bake_Health"] for x in withb
                                 if x["location"] == L and x["bake_Health"] and math.isfinite(x["scene_health"])
                                 and x["explanation"] not in ("event_runtime_override", "dummy_hand_set", "hand_balance")])
        for L in sorted({x["location"] for x in withb})}
    otc = []
    for rec, r, sp, rep in recs:
        if rec["bake_OptimumTime"] is None or abs(rec["bake_OptimumTime"] - rec["optimum_time"]) < 1e-4:
            continue
        sp_log = dataclasses.replace(sp, optimum_time=f32(rec["bake_OptimumTime"]))
        rl = bake_mod.bake_spawner(ctx, sp_log)
        otc.append({"name": rec["name"], "location": rec["location"], "is_days": rec["is_days"],
                    "optimum_time": rec["optimum_time"], "bake_OptimumTime": rec["bake_OptimumTime"],
                    "health_match": rec["health_match"], "damage_match": rec["damage_match"],
                    "log_ot_health_eq_scene": rl.new_health == sp.health_base,
                    "log_ot_damage_eq_scene": rl.new_damage == sp.damage_base,
                    "log_ot_health_eq_bakeinfo": rl.new_health == rec["bake_Health"]})
    h5["ot_changed_since_bakeinfo"] = sorted(otc, key=lambda d: (d["location"], d["name"]))
    h5["ot_changed_summary"] = {
        "n": len(otc),
        "both_exact_with_current_ot": sum(1 for d in otc if d["health_match"] == "exact" and d["damage_match"] == "exact"),
        "both_exact_with_log_ot": sum(1 for d in otc if d["log_ot_health_eq_scene"] and d["log_ot_damage_eq_scene"]),
        "minute_L1_L2": sum(1 for d in otc if not d["is_days"] and d["location"] <= 2),
        "minute_L1_L2_both_exact_with_current_ot": sum(
            1 for d in otc if not d["is_days"] and d["location"] <= 2
            and d["health_match"] == "exact" and d["damage_match"] == "exact")}
    h5["n_spawners_joined"] = len(withb)
    h5["n_bake_info_rows"] = summary["data_facts"]["n_bake_infos"]
    summary["H5_bakeinfos"] = h5

    # ---- H6: rounding rules
    hp_vals = [x["scene_health"] for x in R if math.isfinite(x["scene_health"])]
    ties_h = [x for x in R if math.isfinite(x["pred_health_raw"]) and x["pred_health_raw"] % 1 == 0.5]
    ties_d = [x for x in R if math.isfinite(x["pred_damage_raw"]) and (max(10.0, x["pred_damage_raw"]) / 10.0) % 1 == 0.5]
    summary["H6_rounding"] = {
        "scene_health_integer": sum(1 for v in hp_vals if v == int(v)), "scene_health_finite": len(hp_vals),
        "scene_health_inf": [x["name"] for x in R if math.isinf(x["scene_health"])],
        "scene_damage_multiple_of_10": sum(1 for x in R if x["scene_damage"] % 10 == 0),
        "scene_damage_not_multiple_of_10": [(x["name"], x["scene_damage"]) for x in R if x["scene_damage"] % 10 != 0],
        "scene_damage_eq_10": sum(1 for x in R if x["scene_damage"] == 10),
        "pred_damage_raw_below_10": sum(1 for x in R if x["pred_damage_raw"] < 10),
        "pred_damage_floor_and_scene_10": sum(1 for x in R if x["pred_damage_raw"] < 10 and x["scene_damage"] == 10),
        "health_half_ties": [(x["name"], x["pred_health_raw"], x["scene_health"]) for x in ties_h],
        "damage_half_ties": [(x["name"], x["pred_damage_raw"], x["scene_damage"]) for x in ties_d],
    }

    # ---- H11: for non-exact minute-mode spawners, which stored snapshots (any of the 63 + 46) reproduce them?
    alls = [(f"P{x.source_index}", x) for x in sd.snapshots] + [(f"C{x.source_index}", x) for x in sd.time_chunk_snapshots]
    h11 = []
    for rec, r, sp, rep in recs:
        if sp.is_days or rec["explanation"] not in ("minute_dps_divergence", "minute_other"):
            continue
        hh, dd, both = [], [], []
        for nm, snp in alls:
            rr = bake_mod.build_spawner_bake_report(ctx, sp, sp.optimum_time, rep.recalc_optimum_time, False, snapshot=snp)
            h_ok, d_ok = rr.new_health == sp.health_base, rr.new_damage == sp.damage_base
            if h_ok:
                hh.append(nm)
            if d_ok:
                dd.append(nm)
            if h_ok and d_ok:
                both.append(nm)
        h11.append({"name": rec["name"], "location": rec["location"], "optimum_time": rec["optimum_time"],
                    "model_snapshot": rec["snapshot"], "explanation": rec["explanation"],
                    "snapshots_matching_health": hh, "snapshots_matching_damage": dd, "snapshots_matching_both": both})
    summary["H11_minute_snapshot_search"] = {
        "n_snapshots_searched": len(alls), "n": len(h11),
        "n_with_any_health_match": sum(1 for x in h11 if x["snapshots_matching_health"]),
        "n_with_both_match": sum(1 for x in h11 if x["snapshots_matching_both"]),
        "rows": h11}

    # ---- H6b: rounding ties that discriminate ToEven from AwayFromZero (Damage = Round(raw/10)*10)
    disc = []
    for x in R:
        q = max(10.0, x["pred_damage_raw"]) / 10.0 if math.isfinite(x["pred_damage_raw"]) else math.nan
        if math.isfinite(q) and q % 1 == 0.5 and int(q) % 2 == 0:
            disc.append({"name": x["name"], "raw": x["pred_damage_raw"], "to_even": math.floor(q) * 10.0,
                         "away": math.ceil(q) * 10.0, "scene": x["scene_damage"]})
    summary["H6_rounding"]["damage_ties_discriminating"] = disc
    summary["H6_rounding"]["damage_ties_discriminating_scene_eq_to_even"] = sum(1 for d in disc if d["scene"] == d["to_even"])
    summary["H6_rounding"]["damage_ties_discriminating_scene_eq_away"] = sum(1 for d in disc if d["scene"] == d["away"])

    # ---- H10: round scene values (hand-tuning signature)
    big_h = [x for x in R if math.isfinite(x["scene_health"]) and x["scene_health"] >= 10000]
    big_d = [x for x in R if x["scene_damage"] >= 10000]
    summary["H10_round_values"] = {
        "health_ge_1e4": len(big_h),
        "health_multiple_of_1e4": [(x["name"], x["location"], x["scene_health"], x["pred_health"]) for x in big_h
                                   if x["scene_health"] % 10000 == 0],
        "health_expected_by_chance": len(big_h) / 10000.0,
        "damage_ge_1e4": len(big_d),
        "damage_multiple_of_1e4": [(x["name"], x["location"], x["scene_damage"], x["pred_damage"]) for x in big_d
                                   if x["scene_damage"] % 10000 == 0],
        "damage_expected_by_chance": len(big_d) / 1000.0,
    }

    # ---- H7: early-spawner offset on/off (L>=11, rel<0.5)
    shifted = [x for x in recs if x[0]["early_shift"] != 0 and x[0]["explanation"] in ("day_source_b", "exact")]
    def mode_eval(use_shift):
        lr_h, lr_d, h5_, d5_, h1_, d1_ = [], [], 0, 0, 0, 0
        for rec, r, sp, rep in shifted:
            t = rep.recalc_optimum_time if use_shift else float(sp.optimum_time)
            rr = bake_mod.build_spawner_bake_report(ctx, sp, sp.optimum_time, f32(t), sp.is_days)
            a, b = ratio(sp.health_base, rr.new_health), ratio(sp.damage_base, rr.new_damage)
            lr_h.append(abs(math.log(a)))
            lr_d.append(abs(math.log(b)))
            h1_ += abs(a - 1) <= TOL1
            h5_ += abs(a - 1) <= TOL5
            d1_ += abs(b - 1) <= TOL1
            d5_ += abs(b - 1) <= TOL5
        return {"median_abs_log_ratio_health": float(statistics.median(lr_h)),
                "median_abs_log_ratio_damage": float(statistics.median(lr_d)),
                "health_w1": h1_, "health_w5": h5_, "damage_w1": d1_, "damage_w5": d5_}
    summary["H7_early_offset"] = {"n": len(shifted), "with_shift": mode_eval(True), "without_shift": mode_eval(False),
                                  "max_shift_days": max(x[0]["early_shift"] for x in shifted)}

    # ---- H8: PreferredWeaponType of the location vs alternatives (damage exact counts)
    # minute-mode (playtime snapshots, whose DefenseData are all zero) and early day-mode (bake time < 5 days,
    # where scene damage still matches), the only sets where the defence type could be discriminated.
    def damage_exact_with(def_type_of, select):
        n_ex = 0
        tot = 0
        for rec, r, sp, rep in recs:
            if not select(rec, sp, rep) or rec["explanation"] in ("event_runtime_override", "dummy_hand_set", "hand_balance"):
                continue
            tot += 1
            alt = dict(ctx.preferred_weapon_type)
            alt[sp.location_number] = def_type_of(r, sp)
            ctx2 = dataclasses.replace(ctx, preferred_weapon_type=alt)
            rr = bake_mod.bake_spawner(ctx2, sp)
            n_ex += rr.new_damage == sp.damage_base
        return {"n": tot, "damage_exact": n_ex}
    sel_min = lambda rec, sp, rep: not sp.is_days
    sel_day = lambda rec, sp, rep: sp.is_days and rep.recalc_optimum_time < 5 and sp.location_number >= 2
    h8 = {}
    for lab, sel in (("minutes", sel_min), ("days_lt5", sel_day)):
        h8[lab] = {"location_preferred_weapon": damage_exact_with(lambda r, sp: ctx.preferred_weapon_type[sp.location_number], sel),
                   "spawner_WeaponType": damage_exact_with(lambda r, sp: int(r["WeaponType"]), sel)}
        for wt in WeaponType:
            h8[lab][f"fixed_{wt.name}"] = damage_exact_with(lambda r, sp, wt=wt: int(wt), sel)
    h8["playtime_snapshots_with_nonzero_defense"] = sum(1 for s in sd.snapshots if any(v != 0 for _, v in s.defense_data))
    h8["first_time_chunk_with_nonzero_defense"] = min(
        (s.time_chunk_index for s in sd.time_chunk_snapshots if any(v != 0 for _, v in s.defense_data)), default=None)
    summary["H8_defense_type"] = h8

    # ---- H9: numeric precision sensitivity (float32 emulation vs all-double)
    orig = (bake_mod.f32, interp_mod.f32, snap_mod.f32)
    try:
        bake_mod.f32 = interp_mod.f32 = snap_mod.f32 = np.float64
        changed_h = changed_d = 0
        exact_h = exact_d = 0
        for (r, sp), rep in zip(spawners, reports):
            rr = bake_mod.bake_spawner(ctx, sp)
            changed_h += not (rr.new_health == rep.new_health or (math.isinf(rr.new_health) and math.isinf(rep.new_health)))
            changed_d += rr.new_damage != rep.new_damage
            exact_h += rr.new_health == sp.health_base
            exact_d += rr.new_damage == sp.damage_base
    finally:
        bake_mod.f32, interp_mod.f32, snap_mod.f32 = orig
    summary["H9_precision"] = {"changed_health": changed_h, "changed_damage": changed_d,
                               "double_health_exact": exact_h, "double_damage_exact": exact_d}

    # ---- H12: can a single changed bake input explain the minute_other spawners?
    # HP depends on (OT, AverageFightTime, Count, armor); Damage on (OT, AverageFightTime, Count, Difficulty,
    # AttackCooldown). Only OT/AFT/Count act on both, so only they can explain HP and Damage with one change.
    def frange(lo, hi, step):
        return [round(lo + step * i, 6) for i in range(int(round((hi - lo) / step)) + 1)]
    scans = {
        "AverageFightTime": (frange(1.0, 200.0, 0.5), lambda sp, v: dataclasses.replace(sp, average_fight_time=f32(v))),
        "Count": (list(range(1, 12)), lambda sp, v: dataclasses.replace(sp, count=int(v))),
        "Difficulty": (frange(0.1, 5.0, 0.05), lambda sp, v: dataclasses.replace(sp, difficulty=f32(v))),
        "OptimumTime_minutes": (frange(0.0, 250.0, 0.5), lambda sp, v: dataclasses.replace(sp, optimum_time=f32(v))),
        "AttackCooldown": (frange(0.1, 5.0, 0.05), lambda sp, v: dataclasses.replace(sp, attack_cooldown_base=float(v))),
    }
    h12 = []
    for rec, r, sp, rep in recs:
        if rec["explanation"] != "minute_other":
            continue
        row = {"name": rec["name"], "location": rec["location"], "optimum_time": rec["optimum_time"],
               "average_fight_time": rec["average_fight_time"], "count": rec["count"], "difficulty": rec["difficulty"],
               "attack_cooldown": rec["attack_cooldown"],
               "scene_health": rec["scene_health"], "pred_health": rec["pred_health"],
               "scene_damage": rec["scene_damage"], "pred_damage": rec["pred_damage"],
               "aft_implied_by_health": float(sp.average_fight_time) * sp.health_base / rep.new_health_raw,
               "scans": {}}
        for nm, (grid, mk) in scans.items():
            hh, dd, both = [], [], []
            for v in grid:
                rr = bake_mod.bake_spawner(ctx, mk(sp, v))
                h_ok, d_ok = rr.new_health == sp.health_base, rr.new_damage == sp.damage_base
                if h_ok:
                    hh.append(v)
                if d_ok:
                    dd.append(v)
                if h_ok and d_ok:
                    both.append(v)
            row["scans"][nm] = {"n_grid": len(grid), "health_match_values": hh, "damage_match_values": dd,
                                "both_match_values": both}
        h12.append(row)
    summary["H12_single_input_scan"] = {
        "grids": {"AverageFightTime": "1..200 step 0.5", "Count": "1..11", "Difficulty": "0.1..5 step 0.05",
                  "OptimumTime_minutes": "0..250 step 0.5", "AttackCooldown": "0.1..5 step 0.05"},
        "n": len(h12),
        "n_explained_by_one_input": sum(1 for x in h12 if any(s["both_match_values"] for s in x["scans"].values())),
        "rows": h12}

    # ---- write summary
    with open(OUT / "summary.json", "w", encoding="utf-8") as fh:
        json.dump(sanitize(summary), fh, indent=1, sort_keys=True, allow_nan=False)
    # short console digest
    print(json.dumps(sanitize({"overall": summary["overall"], "by_mode": {k: {"health": v["health"], "damage": v["damage"]}
                                                                for k, v in summary["by_mode"].items()},
                      "explanations": summary["explanations"]}), indent=1))
    print(f"wrote {OUT/'spawners.csv'}, {OUT/'groups.csv'}, {OUT/'summary.json'}")


if __name__ == "__main__":
    main()
