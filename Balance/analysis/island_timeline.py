"""Stage 4 / track "timeline": work back the island timeline generation rules.

Reads only Balance/out/balance.sqlite and writes machine-readable results to
Balance/out/analysis/timeline/.  Deterministic; re-run with

    cd Balance && .venv/bin/python analysis/island_timeline.py

Sections (keys in summary.json):
  units            time units used by the data / simulation (verified from data)
  target_days      generating rule of CombatBalanceConfig.IslandTargetDays
  spawner_window   spawner OptimumTime vs island window (RelativeOptimumTime mapping)
  evolution        EvolutionIslandFillRuleRows -> TimingRows -> *EvolutionTargetTimings
  other_island_axis  interpolation IslandPoints, amulet LevelTargetDays on the island axis
  simulation       simulated bot island arrival vs IslandTargetDays
"""
from __future__ import annotations

import csv
import json
import math
import sqlite3
import struct
from collections import Counter, defaultdict
from fractions import Fraction
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
DB = ROOT / "out" / "balance.sqlite"
OUT = ROOT / "out" / "analysis" / "timeline"

f32 = np.float32


def q(con, sql, *args):
    cur = con.execute(sql, args)
    cols = [d[0] for d in cur.description]
    return [dict(zip(cols, r)) for r in cur.fetchall()]


def arr(con, cls, field):
    rows = con.execute(
        "select value from config_arrays where class=? and field_path=? order by idx", (cls, field)
    ).fetchall()
    return [r[0] for r in rows]


def r6(x):
    return None if x is None else float(round(float(x), 6))


def write_csv(name, rows, cols):
    with open(OUT / name, "w", newline="", encoding="utf-8") as fh:
        w = csv.DictWriter(fh, fieldnames=cols, extrasaction="ignore")
        w.writeheader()
        for r in rows:
            w.writerow({k: (r6(v) if isinstance(v, (float, np.floating)) else v) for k, v in r.items()})


# ---------------------------------------------------------------------------
# recovered runtime formula (balance.island_timing_to_days, native 0x157ad44)
# ---------------------------------------------------------------------------
def convert_island_timing_to_days(s, itd):
    s = f32(s)
    n = int(math.floor(s))
    prev = f32(0.0) if n == 1 else f32(itd[n - 2])
    frac = f32(s - f32(n))
    if frac == 0:
        return prev
    return f32(prev + frac * f32(f32(itd[n - 1]) - prev))


def island_position(day, itd):
    """Inverse of ConvertIslandTimingToDays on the stored table: island timing s for a day value."""
    if day <= 0:
        return 1.0
    ends = [0.0] + list(itd)  # ends[k] = start of island k+1
    for k in range(1, len(ends)):
        if day < ends[k]:
            lo, hi = ends[k - 1], ends[k]
            return k + (day - lo) / (hi - lo)
    return float(len(itd) + 1)


# ---------------------------------------------------------------------------
def units_section(con, sim_cfg, snaps, chunks):
    life = arr(con, "SimulationConfig", "LifeCycleInMinutes")
    cycle = sum(life)
    res = {
        "LifeCycleInMinutes": life,
        "cycle_minutes": cycle,
        "cycles_per_1440": 1440 / cycle,
        "active_minutes_per_1440_if_always_active": 1440 / cycle * life[0],
        "SpawnerChargesChunkMinutes": sim_cfg["SpawnerChargesChunkMinutes"],
        "TimeChunkDurationMinutes": sim_cfg["TimeChunkDurationMinutes"],
        "UseSpawnerCharges": sim_cfg["UseSpawnerCharges"],
        "IsCapOptimumTime": sim_cfg["IsCapOptimumTime"],
        "UseFastTick": sim_cfg["UseFastTick"],
        "FastTickStartChunkOffset": sim_cfg["FastTickStartChunkOffset"],
    }
    # TimeChunkIndex == floor(TotalPlaytime / SpawnerChargesChunkMinutes) ?
    allrows = snaps + chunks
    chunk_ok = sum(
        1 for r in allrows if r["TimeChunkIndex"] == math.floor(r["TotalPlaytime"] / sim_cfg["SpawnerChargesChunkMinutes"])
    )
    chunk_ok30 = sum(
        1 for r in allrows if r["TimeChunkIndex"] == math.floor(r["TotalPlaytime"] / sim_cfg["TimeChunkDurationMinutes"])
    )
    chunk_okplay30 = sum(1 for r in allrows if r["TimeChunkIndex"] == math.floor(r["Playtime"] / 30.0))
    res["snapshot_rows"] = len(allrows)
    res["TimeChunkIndex_eq_floor_Total_div_1440"] = chunk_ok
    res["TimeChunkIndex_eq_floor_Total_div_30"] = chunk_ok30
    res["TimeChunkIndex_eq_floor_Playtime_div_30"] = chunk_okplay30
    # AFK (Total - Playtime) increments in the dense Snapshots list
    afk = [r["TotalPlaytime"] - r["Playtime"] for r in snaps]
    jumps = [round(b - a, 3) for a, b in zip(afk, afk[1:]) if abs(b - a) > 0.01]
    res["afk_jumps_in_Snapshots"] = dict(Counter(jumps))
    # active minutes per real day (TimeChunkSnapshots)
    per_day = []
    for a, b in zip(chunks, chunks[1:]):
        dd = b["TimeChunkIndex"] - a["TimeChunkIndex"]
        if dd > 0:
            per_day.append(
                {
                    "from_chunk": a["TimeChunkIndex"],
                    "to_chunk": b["TimeChunkIndex"],
                    "active_min_per_day": (b["Playtime"] - a["Playtime"]) / dd,
                }
            )
    res["active_min_per_day_first4"] = [r6(x["active_min_per_day"]) for x in per_day[:4]]
    res["active_min_per_day_min_after4"] = r6(min(x["active_min_per_day"] for x in per_day[4:]))
    res["active_min_per_day_max_after4"] = r6(max(x["active_min_per_day"] for x in per_day[4:]))
    last = chunks[-1]
    res["final_chunk"] = last["TimeChunkIndex"]
    res["final_playtime_min"] = r6(last["Playtime"])
    res["avg_active_min_per_day_overall"] = r6(last["Playtime"] / last["TimeChunkIndex"])
    return res, per_day


# ---------------------------------------------------------------------------
def chunk30_section(con, itd, tool):
    """Where TimeChunkDurationMinutes=30 / SpawnerOptimumTimeDaysConverter (/30, *30) could touch data."""
    rng = {t["LocationNumber"]: t for t in tool}
    quest = q(con, 'select IsDays, count(*) n from "c_QuestConfig__Chains__Steps" group by IsDays order by IsDays')
    l3, l4 = rng[3], rng[4]
    return {
        "consumers": {
            "SimulationSystem.GetSpawnerOptimumTimingForCurrentMode (0x13ed83c)": "only feeds autoGamblingOptimumTime",
            "BossSpawnService.BakeStatsForFloor": "boss tower / quest mob stats; boss tower timing not tested here",
            "QuestMobSpawnerService": "ConvertDaysToOptimum (*30) then BakeStatsForFloor (/30) for IsDays steps; ConvertOptimumToDays only for IsDays=0 steps",
        },
        "quest_steps_by_IsDays": {str(r["IsDays"]): r["n"] for r in quest},
        "AutoGambling": q(con, "select AutoGambling from c_SimulationConfig")[0]["AutoGambling"],
        # continuity of the L3 -> L4 boundary under the two day lengths
        "L3_tool_minutes": [l3["MinOptimumTime"], l3["MaxOptimumTime"]],
        "L4_tool_days": [l4["MinOptimumTime"], l4["MaxOptimumTime"]],
        "L4_in_minutes_at_60": [l4["MinOptimumTime"] * 60, l4["MaxOptimumTime"] * 60],
        "L4_in_minutes_at_30": [l4["MinOptimumTime"] * 30, l4["MaxOptimumTime"] * 30],
        "L4_start_continuous_with_L3_end_at_60": abs(l4["MinOptimumTime"] * 60 - l3["MaxOptimumTime"]) < 1e-4,
        "L4_start_continuous_with_L3_end_at_30": abs(l4["MinOptimumTime"] * 30 - l3["MaxOptimumTime"]) < 1e-4,
    }


# ---------------------------------------------------------------------------
def target_days_section(con, itd, spawners):
    n = len(itd)
    d1 = [itd[i + 1] - itd[i] for i in range(n - 1)]
    d2 = [d1[i + 1] - d1[i] for i in range(len(d1) - 1)]

    # closed form for the tail: D(i) = (5/6)(i+1)(i+2) - 105  (i = island number, D(i) = itd[i-1])
    def closed(i):
        return Fraction(5, 6) * (i + 1) * (i + 2) - 105

    rows = []
    for i in range(1, n + 1):
        c = closed(i)
        c32 = float(f32(float(c)))
        rows.append(
            {
                "island": i,
                "IslandTargetDays": itd[i - 1],
                "start_day": 0.0 if i == 1 else itd[i - 2],
                "window_days": itd[i - 1] - (0.0 if i == 1 else itd[i - 2]),
                "end_minutes_at_60": itd[i - 1] * 60.0,
                "closed_form": float(c),
                "closed_form_f32": c32,
                "abs_err": abs(itd[i - 1] - float(c)),
                "f32_exact": f32(itd[i - 1]) == f32(c32),
            }
        )
    # smallest i0 such that the closed form holds (f32-exact) for every i >= i0
    i0 = None
    for i in range(n, 0, -1):
        if rows[i - 1]["f32_exact"]:
            i0 = i
        else:
            break
    tail = rows[i0 - 1 :]
    # increments in the tail: Delta(i) = D(i) - D(i-1) = (5/3)(i+1)
    inc_rule_ok = all(
        abs((itd[i - 1] - itd[i - 2]) - 5.0 / 3.0 * (i + 1)) < 1e-3 for i in range(i0 + 1, n + 1)
    )
    # head pattern in minutes (60 min/day): 30, 110, 240, 420 -> second difference 50
    head_min = [round(itd[i] * 60, 4) for i in range(6)]
    head_d1 = [round(head_min[i + 1] - head_min[i], 4) for i in range(5)]
    head_d2 = [round(head_d1[i + 1] - head_d1[i], 4) for i in range(4)]

    # gate spawners: max OT per island among spawners that own gates
    gate = {}
    for s in spawners:
        if (s["Gates#count"] or 0) > 0:
            L = s["LocationNumber"]
            ot_days = s["OptimumTime"] if s["IsDays"] else s["OptimumTime"] / 60.0
            if L not in gate or ot_days > gate[L]["ot_days"]:
                gate[L] = {"name": s["go_name"], "file_id": s["file_id"], "ot_days": ot_days, "IsDays": s["IsDays"],
                           "OT": s["OptimumTime"], "rel": s["RelativeOptimumTime"]}
    gate_rows = []
    maxL = max(s["LocationNumber"] for s in spawners)
    for L in range(1, maxL + 1):
        g = gate.get(L)
        isl = [s for s in spawners if s["LocationNumber"] == L and s["IsDays"] == (1 if L >= 4 else 0)]
        max_ot = max(s["OptimumTime"] for s in isl)
        max_days = max_ot if L >= 4 else max_ot / 60.0
        gate_rows.append(
            {
                "island": L,
                "IslandTargetDays": itd[L - 1],
                "gate_spawner": g["name"] if g else None,
                "gate_file_id": g["file_id"] if g else None,
                "gate_OT_days": g["ot_days"] if g else None,
                "gate_IsDays": g["IsDays"] if g else None,
                "gate_rel": g["rel"] if g else None,
                "gate_match": (abs(g["ot_days"] - itd[L - 1]) < 1e-4) if g else None,
                "max_OT_days_on_island": max_days,
                "max_match": abs(max_days - itd[L - 1]) < 1e-4,
            }
        )
    gate_matches = sum(1 for r in gate_rows if r["gate_match"])
    gate_count = sum(1 for r in gate_rows if r["gate_spawner"])
    res = {
        "count": n,
        "values": [r6(x) for x in itd],
        "first_diff": [r6(x) for x in d1],
        "second_diff": [r6(x) for x in d2],
        "closed_form": "D(i) = (5/6)*(i+1)*(i+2) - 105   (i>=i0); equivalently D(i)=D(i-1)+(5/3)*(i+1)",
        "closed_form_first_island_f32_exact_onwards": i0,
        "closed_form_islands_covered": len(tail),
        "closed_form_max_abs_err": r6(max(r["abs_err"] for r in tail)),
        "closed_form_check_island_before_i0": {
            "island": i0 - 1,
            "stored": itd[i0 - 2],
            "closed_form": r6(float(closed(i0 - 1))),
        },
        "tail_increment_rule_ok": inc_rule_ok,
        "tail_increment_minutes_rule": "Delta(i) * 60 = 100*(i+1) minutes",
        "hand_part_islands": f"1..{i0 - 1}",
        "hand_increments_days": [r6(rows[i]["window_days"]) for i in range(i0)],
        "head_minutes": head_min,
        "head_minutes_d1": head_d1,
        "head_minutes_d2": head_d2,
        "gate_islands_with_gate_spawner": gate_count,
        "gate_OT_equals_IslandTargetDays": gate_matches,
        "max_OT_equals_IslandTargetDays_islands": sum(1 for r in gate_rows if r["max_match"]),
        "max_island_in_scene": maxL,
        "gate_rel_eq_1": sum(1 for r in gate_rows if r["gate_rel"] is not None and abs(r["gate_rel"] - 1.0) < 1e-9),
        "gate_minute_islands": [r["island"] for r in gate_rows if r["gate_IsDays"] == 0],
        "gate_day_islands": [r["island"] for r in gate_rows if r["gate_IsDays"] == 1],
        "gate_day_OT": [r["gate_OT_days"] for r in gate_rows if r["gate_IsDays"] == 1],
        "gate_day_OT_all_integer": all(float(r["gate_OT_days"]).is_integer() for r in gate_rows if r["gate_IsDays"] == 1),
        "other_gates": [{"island": s["LocationNumber"], "name": s["go_name"], "OT": s["OptimumTime"],
                         "rel": s["RelativeOptimumTime"]} for s in spawners
                        if (s["Gates#count"] or 0) > 0 and gate[s["LocationNumber"]]["file_id"] != s["file_id"]],
    }
    return res, rows, gate_rows


# ---------------------------------------------------------------------------
def spawner_window_section(con, itd, spawners, tool):
    rng = {t["LocationNumber"]: (t["MinOptimumTime"], t["MaxOptimumTime"], t["IsDays"], t["MinSpawner"], t["MaxSpawner"])
           for t in tool}
    by_fid = {s["file_id"]: s for s in spawners}
    # tool ranges vs IslandTargetDays
    tool_rows = []
    for L in sorted(rng):
        mn, mx, isd, mns, mxs = rng[L]
        start = 0.0 if L == 1 else itd[L - 2]
        end = itd[L - 1]
        f = 1.0 if isd else 60.0
        mins = by_fid.get(int(mns.split(":")[1])) if mns else None
        maxs = by_fid.get(int(mxs.split(":")[1])) if mxs else None
        tool_rows.append(
            {
                "island": L,
                "IsDays": isd,
                "Min": mn,
                "Max": mx,
                "unit": "days" if isd else "minutes",
                "start_ITD_scaled": start * f,
                "end_ITD_scaled": end * f,
                "min_eq_start": abs(mn - start * f) < 1e-3,
                "max_eq_end": abs(mx - end * f) < 1e-3,
                "MinSpawner": mins["go_name"] if mins else None,
                "MinSpawner_OT": mins["OptimumTime"] if mins else None,
                "MaxSpawner": maxs["go_name"] if maxs else None,
                "MaxSpawner_OT": maxs["OptimumTime"] if maxs else None,
                "MinSpawner_OT_eq_Min": (mins is not None and abs(mins["OptimumTime"] - mn) < 1e-3),
                "MaxSpawner_OT_eq_Max": (maxs is not None and abs(maxs["OptimumTime"] - mx) < 1e-3),
            }
        )

    rows = []
    for s in spawners:
        L = s["LocationNumber"]
        mn, mx, isd_tool, _, _ = rng[L]
        ot = s["OptimumTime"]
        rel = s["RelativeOptimumTime"]
        isd = s["IsDays"]
        step = 0.1 if isd else 1.0
        W = mx - mn
        raw = float(f32(f32(mn) + f32(rel) * f32(W)))
        fwd_err = ot - raw
        fwd_ok = abs(fwd_err) <= step / 2 + 1e-4
        # ApplyOptimumTimesFromRelative (fitted, float32): Mathf.Round(x * k) / k with k = 10 (days) or 1 (minutes);
        # Mathf.Round rounds half to even, the product x*k is a float32.
        k = f32(10.0) if isd else f32(1.0)
        prod32 = f32(f32(raw) * k)
        fwd_rounded = float(np.round(prod32)) / float(k)
        # control (kept for continuity with the first report): float32 sum, x*k in double, half-up
        fwd_rounded_halfup = math.floor(raw * float(k) + 0.5) / float(k)
        # clean 2x2: precision {float32, double} x rounding {half-even, half-up}
        prod64 = (float(mn) + float(rel) * float(W)) * float(k)
        variants = {
            "f32_even": float(np.round(prod32)) / float(k),
            "f32_up": math.floor(float(prod32) + 0.5) / float(k),
            "f64_even": float(np.round(prod64)) / float(k),
            "f64_up": math.floor(prod64 + 0.5) / float(k),
        }
        tie32 = float(prod32) - math.floor(float(prod32)) == 0.5
        tie_discriminating = tie32 and (math.floor(float(prod32)) % 2 == 0)  # half-even != half-up
        rel_raw = (ot - mn) / W
        rev_ok = abs(rel - rel_raw) <= 0.005 + 1e-6
        unit_mismatch = isd != isd_tool
        # simulation/ITD form (day units): start(L)+rel*(ITD[L-1]-start(L)), start(1)=0
        start = 0.0 if L == 1 else itd[L - 2]
        itd_form_days = start + rel * (itd[L - 1] - start)
        ot_days = ot if isd else ot / 60.0
        bake_ot = s["bake.OptimumTime"]
        # GetReCalcOptimumTime (SimulationConfig.cs:219-231), shipped params 11 / 0.5 / 1
        recalc = ot
        if L >= 11 and rel < 0.5:
            delta = (itd[L - 1] - itd[L - 2]) * (0.5 - rel) * 1.0
            if not isd:
                delta *= 60.0
            recalc = ot - delta
        # Apply (rel -> OT, rounded to the unit step) is the generating direction; Fill (OT -> rel, 2 dp)
        # is checked as well.  Classes: both / apply_only / fill_only / neither.
        if fwd_ok and rev_ok:
            cls = "both"
        elif fwd_ok:
            cls = "apply_only"
        elif rev_ok:
            cls = "fill_only"
        else:
            cls = "neither"
        fwd_round_eq = abs(ot - fwd_rounded) < 1e-4
        rows.append(
            {
                "file_id": s["file_id"],
                "go_name": s["go_name"],
                "island": L,
                "IsDays": isd,
                "unit_mismatch_with_tool": unit_mismatch,
                "OptimumTime": ot,
                "RelativeOptimumTime": rel,
                "tool_Min": mn,
                "tool_Max": mx,
                "OT_from_rel_raw": raw,
                "OT_from_rel_rounded": fwd_rounded,
                "fwd_err": fwd_err,
                "fwd_ok": fwd_ok,
                "fwd_round_eq": fwd_round_eq,
                "fwd_round_eq_halfup_double": abs(ot - fwd_rounded_halfup) < 1e-4,
                **{f"eq_{name}": abs(ot - v) < 1e-4 for name, v in variants.items()},
                "tie_f32": tie32,
                "tie_discriminating": tie_discriminating,
                "rel_from_OT": rel_raw,
                "rev_ok": rev_ok,
                "class": cls,
                "OT_days": ot_days,
                "ITD_form_days": itd_form_days,
                "rel_outside_0_1": rel < -1e-9 or rel > 1 + 1e-9,
                "bake_OptimumTime": bake_ot,
                "bake_eq_OT": bake_ot is not None and abs(bake_ot - ot) < 1e-4,
                "bake_eq_OT_from_rel": bake_ot is not None and abs(bake_ot - raw) <= step / 2 + 1e-4,
                "recalc_bake_time": recalc,
                "recalc_shift": ot - recalc,
                "Gates": s["Gates#count"],
                "IsEvent": s["IsEvent"],
                "IsVip": s["IsVip"],
                "IsEliteBoss": s["IsEliteBoss"],
            }
        )
    cnt = Counter(r["class"] for r in rows)
    exc = [r for r in rows if r["class"] == "neither"]
    apply_only = [r for r in rows if r["class"] == "apply_only"]
    outside = [r for r in rows if r["rel_outside_0_1"]]
    groups = defaultdict(list)
    for r in outside:
        groups[(r["island"], r["go_name"].split(" (")[0])].append(r)
    outside_groups = [
        {"island": k[0], "name": k[1], "n": len(v), "rel": sorted({x["RelativeOptimumTime"] for x in v}),
         "OT": sorted({x["OptimumTime"] for x in v}), "classes": dict(Counter(x["class"] for x in v))}
        for k, v in sorted(groups.items())
    ]
    early_by_island = []
    for L in range(11, max(rng) + 1):
        e = [r for r in rows if r["island"] == L and r["RelativeOptimumTime"] < 0.5]
        if e:
            early_by_island.append({"island": L, "n_shifted": len(e), "n_island": sum(1 for r in rows if r["island"] == L),
                                    "max_shift_days": r6(max(r["recalc_shift"] for r in e)),
                                    "window_days": r6(itd[L - 1] - itd[L - 2])})
    # distribution of rel per island
    dist = []
    for L in sorted(rng):
        rs = sorted(r["RelativeOptimumTime"] for r in rows if r["island"] == L)
        a = np.array(rs)
        dist.append(
            {
                "island": L,
                "n": len(rs),
                "rel_min": float(a.min()),
                "rel_p25": float(np.percentile(a, 25)),
                "rel_median": float(np.median(a)),
                "rel_p75": float(np.percentile(a, 75)),
                "rel_max": float(a.max()),
                "n_rel_0": int((a == 0).sum()),
                "n_rel_1": int((a == 1).sum()),
                "n_outside_0_1": int(((a < 0) | (a > 1)).sum()),
                "mean_rel_in_0_1": float(a[(a >= 0) & (a <= 1)].mean()),
            }
        )
    # L1-3 alternative: window start = 60*IslandTargetDays[L-2] instead of the tool Min (35 / 120)
    alt13 = 0
    n13 = 0
    for r in rows:
        if r["island"] <= 3 and r["IsDays"] == 0:
            n13 += 1
            L = r["island"]
            a = 0.0 if L == 1 else 60.0 * itd[L - 2]
            b = 60.0 * itd[L - 1]
            x = f32(f32(a) + f32(r["RelativeOptimumTime"]) * f32(b - a))
            if abs(float(np.round(x)) - r["OptimumTime"]) < 1e-4:
                alt13 += 1
    tool13 = sum(1 for r in rows if r["island"] <= 3 and r["IsDays"] == 0 and r["fwd_round_eq"])
    # whole-population stats on the ITD (day) form for L>=4 IsDays spawners
    d_rows = [r for r in rows if r["island"] >= 4 and r["IsDays"] == 1]
    itd_ok = sum(1 for r in d_rows if abs(r["OptimumTime"] - r["ITD_form_days"]) <= 0.05 + 1e-4)
    # bake info vs OT
    bake_present = [r for r in rows if r["bake_OptimumTime"] is not None]
    bake_neq = [r for r in bake_present if not r["bake_eq_OT"]]
    bake_neq_from_rel = [r for r in bake_neq if r["bake_eq_OT_from_rel"]]
    # early offset statistics (L>=11)
    early = [r for r in rows if r["island"] >= 11 and r["RelativeOptimumTime"] < 0.5]
    early_apply = [r for r in early if r["fwd_round_eq"]]
    # rounding: 2x2 precision x tie rule, and the exact .5 ties of the float32 product x*k
    rounding_2x2 = {name: sum(1 for r in rows if r[f"eq_{name}"]) for name in ("f32_even", "f32_up", "f64_even", "f64_up")}
    ties = [r for r in rows if r["tie_f32"]]
    disc = [r for r in ties if r["tie_discriminating"]]
    tie_info = {
        "ties_f32_total": len(ties),
        "ties_by_island_class": {f"L{k[0]}:{k[1]}": v for k, v in sorted(Counter((r["island"], r["class"]) for r in ties).items())},
        "discriminating_total": len(disc),
        "discriminating_by_island_class": {f"L{k[0]}:{k[1]}": v for k, v in sorted(Counter((r["island"], r["class"]) for r in disc).items())},
        "discriminating_match_even": sum(1 for r in disc if r["eq_f32_even"]),
        "discriminating_match_up": sum(1 for r in disc if r["eq_f32_up"]),
        "discriminating_match_neither": sum(1 for r in disc if not r["eq_f32_even"] and not r["eq_f32_up"]),
        "discriminating_not_neither_class": sum(1 for r in disc if r["class"] != "neither"),
        "discriminating_not_neither_match_even": sum(1 for r in disc if r["class"] != "neither" and r["eq_f32_even"]),
        "discriminating_both_class": sum(1 for r in disc if r["class"] == "both"),
        "discriminating_both_class_match_even": sum(1 for r in disc if r["class"] == "both" and r["eq_f32_even"]),
        "nondiscriminating_total": len(ties) - len(disc),
    }
    res = {
        "n_spawners": len(rows),
        "tool_ranges": tool_rows,
        "tool_L4plus_min_max_eq_ITD": sum(1 for t in tool_rows if t["island"] >= 4 and t["min_eq_start"] and t["max_eq_end"]),
        "tool_L4plus_count": sum(1 for t in tool_rows if t["island"] >= 4),
        "tool_L1_3_max_eq_60xITD": sum(1 for t in tool_rows if t["island"] <= 3 and t["max_eq_end"]),
        "tool_L1_3_min_eq_60xITDprev": sum(1 for t in tool_rows if t["island"] <= 3 and t["min_eq_start"]),
        "class_counts": dict(cnt),
        "fwd_ok": sum(1 for r in rows if r["fwd_ok"]),
        "rev_ok": sum(1 for r in rows if r["rev_ok"]),
        "both_ok": cnt.get("both", 0),
        "fwd_exact_equal": sum(1 for r in rows if abs(r["fwd_err"]) < 1e-4),
        "fwd_round_eq": sum(1 for r in rows if r["fwd_round_eq"]),
        "fwd_round_eq_halfup_double": sum(1 for r in rows if r["fwd_round_eq_halfup_double"]),
        "fwd_ok_but_not_round_eq": [
            {k: r[k] for k in ("go_name", "island", "OptimumTime", "RelativeOptimumTime", "OT_from_rel_raw", "OT_from_rel_rounded")}
            for r in rows if r["fwd_ok"] and not r["fwd_round_eq"]],
        "apply_only_n": len(apply_only),
        "apply_only_by_island": dict(Counter(r["island"] for r in apply_only)),
        "apply_only_max_abs_rel_dev": r6(max(abs(r["RelativeOptimumTime"] - r["rel_from_OT"]) for r in apply_only)) if apply_only else 0,
        "rel_outside_groups": outside_groups,
        "early_offset_by_island": early_by_island,
        "L1_3_minute_spawners": n13,
        "L1_3_apply_with_tool_range": tool13,
        "L1_3_apply_with_60xITD_range": alt13,
        "itd_form_L4plus_n": len(d_rows),
        "itd_form_L4plus_ok_0p05": itd_ok,
        "rel_outside_0_1": sum(1 for r in rows if r["rel_outside_0_1"]),
        "bake_present": len(bake_present),
        "bake_eq_OT": len(bake_present) - len(bake_neq),
        "bake_neq_OT": len(bake_neq),
        "bake_neq_but_eq_OT_from_rel": len(bake_neq_from_rel),
        "bake_neq_list": [{k: r[k] for k in ("go_name", "island", "OptimumTime", "RelativeOptimumTime", "OT_from_rel_raw", "bake_OptimumTime", "bake_eq_OT_from_rel")} for r in bake_neq],
        "rounding_2x2": rounding_2x2,
        "rounding_ties": tie_info,
        "early_offset_status": "code port of SimulationConfig.GetReCalcOptimumTime; not checked against data (SpawnerBakeInfos has no ReCalc field)",
        "early_offset_spawners": len(early),
        "early_offset_spawners_where_apply_rule_holds": len(early_apply),
        "early_offset_max_shift_days": r6(max(r["recalc_shift"] for r in early)) if early else 0,
        "early_offset_effective_rel": "bakeTime = start + (2*rel - 0.5)*W  for rel < 0.5, L >= 11, only where OT == Apply(rel)",
        "rel_distribution": dist,
        "exceptions": [
            {k: r[k] for k in ("file_id", "go_name", "island", "IsDays", "OptimumTime", "RelativeOptimumTime",
                               "OT_from_rel_raw", "rel_from_OT", "class", "bake_OptimumTime", "Gates", "IsEvent",
                               "IsVip", "IsEliteBoss")}
            for r in sorted(exc, key=lambda r: (r["island"], r["class"], r["go_name"]))
        ],
    }
    return res, rows


# ---------------------------------------------------------------------------
def evolution_section(con, itd):
    fill = q(con, 'select * from "c_CombatBalanceConfig__EvolutionIslandFillRuleRows" order by "EvolutionIslandFillRuleRows#idx"')
    timing = q(con, 'select * from "c_CombatBalanceConfig__EvolutionIslandTimingRows" order by "EvolutionIslandTimingRows#idx"')
    rar = ["Common", "Uncommon", "Rare", "Epic", "Legendary"]
    out_rows = []
    max_island_err = 0.0
    max_day_err = 0.0
    n_cells = 0
    row_summ = []
    for fr, tr in zip(fill, timing):
        kind = "Armor" if fr["Name"].startswith("Armor") else "Weapon"
        r = fr["Rarity__name"]
        field = f"{r}{'Armor' if kind == 'Armor' else ''}EvolutionTargetTimings"
        tgt = arr(con, "CombatBalanceConfig", field)
        within = 0
        for k in range(1, 16):
            e_rule = fr["StartValue"] + (k - 1) * fr["StepPerEvolution"]
            e_row = tr[f"E{k}"]
            day = float(convert_island_timing_to_days(e_row, itd))
            ierr = abs(e_rule - e_row)
            derr = abs(day - tgt[k - 1])
            max_island_err = max(max_island_err, ierr)
            max_day_err = max(max_day_err, derr)
            n_cells += 1
            if e_row <= 18.0:
                within += 1
            out_rows.append(
                {
                    "kind": kind,
                    "rarity": r,
                    "k": k,
                    "E_island_rule": e_rule,
                    "E_island_row": e_row,
                    "target_days_formula": day,
                    "target_days_stored": tgt[k - 1],
                    "abs_err_days": derr,
                    "target_minutes_at_60": tgt[k - 1] * 60,
                    "inside_built_islands(<=18.0)": e_row <= 18.0,
                }
            )
        row_summ.append(
            {
                "kind": kind,
                "rarity": r,
                "StartValue": fr["StartValue"],
                "StepPerEvolution": fr["StepPerEvolution"],
                "E1_days": tgt[0],
                "E15_island": tr["E15"],
                "E15_days": tgt[14],
                "evolutions_within_islands_1_17": within,
            }
        )
    # pattern of start / step vs rarity
    steps = [fr["StepPerEvolution"] for fr in fill[:5]]
    lin_step = [1.75 + 0.25 * i for i in range(5)]
    w_start = [fr["StartValue"] for fr in fill[:5]]
    a_start = [fr["StartValue"] for fr in fill[5:]]
    res = {
        "cells": n_cells,
        "fill_rule_max_island_err": max_island_err,
        "convert_max_abs_err_days": r6(max_day_err),
        "rows": row_summ,
        "steps": steps,
        "steps_linear_1p75_plus_0p25r": lin_step,
        "steps_deviation_from_linear": [r6(a - b) for a, b in zip(steps, lin_step)],
        "steps_same_for_armor": [fr["StepPerEvolution"] for fr in fill[5:]] == steps,
        "weapon_start": w_start,
        "armor_start": a_start,
        "armor_minus_weapon_start": [r6(a - b) for a, b in zip(a_start, w_start)],
        "weapon_start_d1": [r6(w_start[i + 1] - w_start[i]) for i in range(4)],
        "armor_start_d1": [r6(a_start[i + 1] - a_start[i]) for i in range(4)],
    }
    return res, out_rows


# ---------------------------------------------------------------------------
def other_axis_section(con, itd):
    interp = q(con, 'select * from "c_CombatBalanceConfig__Interpolations.Interpolations"')
    knots = []
    max_err = 0.0
    for it in interp:
        ip = [float(x) for x in it["IslandPoints"].split(";")]
        tp = [float(x) for x in it["TimingPoints"].split(";")]
        for a, b in zip(ip, tp):
            c = float(convert_island_timing_to_days(a, itd))
            max_err = max(max_err, abs(c - b))
            knots.append({"curve": it["Name"], "type": it["Type__name"], "island": a, "timing": b, "converted": c})
    am = q(con, "select * from c_AmuletConfig")[0]
    raw = bytes.fromhex(am["LevelTargetDays"])
    ltd = list(struct.unpack("<%di" % (len(raw) // 4), raw))
    am_rows = []
    for lv, d in enumerate(ltd, 1):
        s = island_position(d, itd)
        am_rows.append({"level": lv, "LevelTargetDays": d, "island_timing": r6(s),
                        "is_island_start": abs(s - round(s)) < 1e-6, "island_start_of": round(s) if abs(s - round(s)) < 1e-6 else None})
    fud = am["FeatureUnlockDay"]
    res = {
        "interpolation_knots": len(knots),
        "interpolation_knot_islands": sorted({k["island"] for k in knots}),
        "interpolation_max_abs_err_days": r6(max_err),
        "amulet_UnlockLocationNumber": am["UnlockLocationNumber"],
        "amulet_LevelTargetDays": ltd,
        "amulet_levels": am_rows,
        "amulet_FeatureUnlockDay": fud,
        "amulet_FeatureUnlockDay_island_timing": r6(island_position(fud, itd)),
        "amulet_unlock_island_start_day": itd[am["UnlockLocationNumber"] - 2],
    }
    return res, knots


# ---------------------------------------------------------------------------
def simulation_section(itd, snaps, chunks, maxL, gate_rows):
    # unit of the gate that unlocks island L = unit of the gate on island L-1
    gate_isdays = {g["island"]: g["gate_IsDays"] for g in gate_rows}
    # arrival bracket: last snapshot with Location < L, first snapshot with Location >= L
    def brackets(series, key):
        out = {}
        for L in range(2, maxL + 1):
            first = next((r for r in series if r["Location"] >= L), None)
            if first is None:
                continue
            before = [r for r in series if r["Location"] < L and r[key] <= first[key]]
            last_before = before[-1] if before else None
            out[L] = (last_before, first)
        return out

    rows = []
    bc = brackets(chunks, "TimeChunkIndex")
    bs = brackets(snaps, "Playtime")
    for L in range(2, maxL + 1):
        target_day = itd[L - 2]  # start of island L = end of island L-1
        r = {"island": L, "target_start_day": target_day, "target_start_minutes_at_60": target_day * 60}
        if L in bc:
            lb, fi = bc[L]
            r.update(
                {
                    "chunk_lo": lb["TimeChunkIndex"] if lb else None,
                    "chunk_hi": fi["TimeChunkIndex"],
                    "total_lo_days": lb["TotalPlaytime"] / 1440 if lb else None,
                    "total_hi_days": fi["TotalPlaytime"] / 1440,
                    "play_lo_min": lb["Playtime"] if lb else None,
                    "play_hi_min": fi["Playtime"],
                }
            )
        if L in bs:
            lb, fi = bs[L]
            r.update({"fine_play_lo_min": lb["Playtime"] if lb else None, "fine_play_hi_min": fi["Playtime"],
                      "fine_total_lo_min": lb["TotalPlaytime"] if lb else None, "fine_total_hi_min": fi["TotalPlaytime"]})
        # deviation in real days (TotalPlaytime/1440) vs target day
        if "total_hi_days" in r:
            lo = r["total_lo_days"] if r["total_lo_days"] is not None else 0.0
            hi = r["total_hi_days"]
            if "fine_total_hi_min" in r:
                lo = max(lo, (r["fine_total_lo_min"] or 0) / 1440)
                hi = min(hi, r["fine_total_hi_min"] / 1440)
            r["arrival_lo_day"] = lo
            r["arrival_hi_day"] = hi
            r["dev_lo_days"] = lo - target_day
            r["dev_hi_days"] = hi - target_day
            r["target_in_bracket"] = lo - 1e-9 <= target_day <= hi + 1e-9
            r["dev_lo_frac_of_window"] = (lo - target_day) / (itd[L - 2] - (0 if L == 2 else itd[L - 3]))
            r["dev_hi_frac_of_window"] = (hi - target_day) / (itd[L - 2] - (0 if L == 2 else itd[L - 3]))
            # same with active minutes (60 active min = 1 design day for minute spawners)
            plo = r.get("fine_play_lo_min", r.get("play_lo_min")) or 0.0
            phi = r.get("fine_play_hi_min", r.get("play_hi_min"))
            r["arrival_lo_active_hours"] = plo / 60
            r["arrival_hi_active_hours"] = phi / 60
            r["arrival_lo_active_min"] = plo
            r["arrival_hi_active_min"] = phi
            r["target_in_bracket_real_day"] = r["target_in_bracket"]
            # compare on the axis the cap uses for the unlocking gate:
            #   minute gate (IsDays=0): OptimumTime > CurrentPlaytime  -> active minutes
            #   day gate    (IsDays=1): OptimumTime > timeChunkIndex   -> real days
            minute_gate = gate_isdays.get(L - 1) == 0
            r["gate_axis"] = "active_minutes" if minute_gate else "real_days"
            if minute_gate:
                tmin = target_day * 60
                r["dev_lo_active_min"] = plo - tmin
                r["dev_hi_active_min"] = phi - tmin
                r["target_in_bracket"] = plo - 1e-6 <= tmin <= phi + 1e-6
                r["late"] = plo > tmin + 1e-4
                r["early"] = phi < tmin - 1e-4
            else:
                r["late"] = lo > target_day + 1e-9
                r["early"] = hi < target_day - 1e-9
        rows.append(r)

    # island position lag at every TimeChunkSnapshot
    lag_rows = []
    for c in chunks:
        day = c["TotalPlaytime"] / 1440
        s = island_position(day, itd)
        lag_rows.append(
            {
                "TimeChunkIndex": c["TimeChunkIndex"],
                "day": day,
                "Playtime": c["Playtime"],
                "bot_Location": c["Location"],
                "target_island_timing": s,
                "target_island": math.floor(s),
                "lag_islands": math.floor(s) - c["Location"],
            }
        )
    within = [r for r in lag_rows if r["target_island"] <= maxL]

    # constant-lag models on the real-day axis, islands 4..maxL: arrival in (T+a, T+b]
    day_rows = [r for r in rows if r["island"] >= 4 and "arrival_lo_day" in r]

    def consistent(a, b, rs):
        return [r["island"] for r in rs if r["dev_lo_days"] < b and r["dev_hi_days"] > a]

    dg = [r for r in day_rows if r["gate_axis"] == "real_days"]
    const_lag = {
        "islands_tested": [r["island"] for r in day_rows],
        "model_T+1_T+2_consistent": consistent(1.0, 2.0, day_rows),
        "model_T_T+1_consistent": consistent(0.0, 1.0, day_rows),
        "common_lag_interval_days_L4plus": [r6(max(r["dev_lo_days"] for r in day_rows)), r6(min(r["dev_hi_days"] for r in day_rows))],
        "common_lag_interval_days_day_gated": [r6(max(r["dev_lo_days"] for r in dg)), r6(min(r["dev_hi_days"] for r in dg))],
        "day_gated_islands": [r["island"] for r in dg],
        "bracket_width_days": {r["island"]: r6(r["arrival_hi_day"] - r["arrival_lo_day"]) for r in day_rows},
    }
    minute_rows = [r for r in rows if r.get("gate_axis") == "active_minutes"]
    res = {
        "chunk_snapshots": len(chunks),
        "dense_snapshots": len(snaps),
        "islands_compared": len([r for r in rows if "arrival_hi_day" in r]),
        "target_in_bracket": sum(1 for r in rows if r.get("target_in_bracket")),
        "target_in_bracket_rule": "islands unlocked by a minute gate (L-1 gate IsDays=0) compared in active minutes, others in real days",
        "target_in_bracket_real_day_only": sum(1 for r in rows if r.get("target_in_bracket_real_day")),
        "late_islands": [r["island"] for r in rows if r.get("late")],
        "early_islands": [r["island"] for r in rows if r.get("early")],
        "minute_gated_islands": [r["island"] for r in minute_rows],
        "minute_gated_lag_after_target_active_min": {r["island"]: [r6(r["dev_lo_active_min"]), r6(r["dev_hi_active_min"])] for r in minute_rows},
        "constant_lag": const_lag,
        "lag_counts_within_content": dict(Counter(r["lag_islands"] for r in within)),
        "lag_nonzero_within_content": [(r["TimeChunkIndex"], r["bot_Location"], r["target_island"]) for r in within if r["lag_islands"] != 0],
        "content_end_day": itd[maxL - 1],
        "bot_last_island": chunks[-1]["Location"],
        "bot_last_day": chunks[-1]["TimeChunkIndex"],
        "arrival": [{k: (r6(v) if isinstance(v, float) else v) for k, v in r.items()} for r in rows],
    }
    return res, rows, lag_rows


# ---------------------------------------------------------------------------
def main():
    OUT.mkdir(parents=True, exist_ok=True)
    con = sqlite3.connect(DB)
    itd = arr(con, "CombatBalanceConfig", "IslandTargetDays")
    sim_cfg = q(con, "select * from c_SimulationConfig")[0]
    snaps = q(con, 'select * from c_SimulationData__Snapshots order by "Snapshots#idx"')
    chunks = q(con, 'select * from c_SimulationData__TimeChunkSnapshots order by "TimeChunkSnapshots#idx"')
    spawners = q(con, "select * from spawners order by LocationNumber, file_id")
    tool = q(con, 'select * from s_SpawnerRelativeOptimumTimeTool__Islands order by "Islands#idx"')

    summary = {}
    summary["units"], per_day = units_section(con, sim_cfg, snaps, chunks)
    summary["target_days"], td_rows, gate_rows = target_days_section(con, itd, spawners)
    summary["units"]["chunk30"] = chunk30_section(con, itd, tool)
    summary["spawner_window"], sp_rows = spawner_window_section(con, itd, spawners, tool)
    summary["evolution"], ev_rows = evolution_section(con, itd)
    summary["other_island_axis"], knots = other_axis_section(con, itd)
    summary["simulation"], arr_rows, lag_rows = simulation_section(itd, snaps, chunks, summary["target_days"]["max_island_in_scene"], gate_rows)

    # one row per island 1..17: schedule, gate, spawners, simulated arrival
    arr_by = {r["island"]: r for r in arr_rows}
    dist_by = {d["island"]: d for d in summary["spawner_window"]["rel_distribution"]}
    isl_rows = []
    for g in gate_rows:
        L = g["island"]
        a = arr_by.get(L, {})
        d = dist_by[L]
        isl_rows.append({
            "island": L,
            "start_day": 0.0 if L == 1 else itd[L - 2],
            "end_day": itd[L - 1],
            "window_days": itd[L - 1] - (0.0 if L == 1 else itd[L - 2]),
            "gate_spawner": g["gate_spawner"],
            "gate_OT_days": g["gate_OT_days"],
            "n_spawners": d["n"],
            "rel_median": d["rel_median"],
            "n_rel_outside_0_1": d["n_outside_0_1"],
            "n_exceptions": sum(1 for r in sp_rows if r["island"] == L and r["class"] == "neither"),
            "arrival_lo_day": a.get("arrival_lo_day"),
            "arrival_hi_day": a.get("arrival_hi_day"),
            "arrival_lo_active_min": a.get("fine_play_lo_min", a.get("play_lo_min")),
            "arrival_hi_active_min": a.get("fine_play_hi_min", a.get("play_hi_min")),
            "gate_axis": a.get("gate_axis"),
            "target_in_bracket": a.get("target_in_bracket"),
            "late": a.get("late"),
        })
    write_csv("island_table.csv", isl_rows, list(isl_rows[0].keys()))
    for r in isl_rows:
        print("[island]", {k: (round(v, 3) if isinstance(v, float) else v) for k, v in r.items()})

    with open(OUT / "summary.json", "w", encoding="utf-8") as fh:
        json.dump(summary, fh, ensure_ascii=False, indent=1, default=lambda o: o.item() if hasattr(o, "item") else str(o))
    write_csv("island_target_days.csv", td_rows, list(td_rows[0].keys()))
    write_csv("gate_spawners.csv", gate_rows, list(gate_rows[0].keys()))
    write_csv("spawner_window.csv", sp_rows, list(sp_rows[0].keys()))
    write_csv("evolution_timings.csv", ev_rows, list(ev_rows[0].keys()))
    write_csv("interpolation_knots.csv", knots, list(knots[0].keys()))
    keys = []
    for r in arr_rows:
        for k in r:
            if k not in keys:
                keys.append(k)
    write_csv("sim_island_arrival.csv", arr_rows, keys)
    write_csv("sim_island_lag.csv", lag_rows, list(lag_rows[0].keys()))
    write_csv("sim_active_minutes_per_day.csv", per_day, list(per_day[0].keys()))

    # console digest
    td = summary["target_days"]
    sw = summary["spawner_window"]
    ev = summary["evolution"]
    sm = summary["simulation"]
    print("[units]", json.dumps({k: summary["units"][k] for k in (
        "TimeChunkIndex_eq_floor_Total_div_1440", "TimeChunkIndex_eq_floor_Total_div_30", "snapshot_rows",
        "afk_jumps_in_Snapshots", "active_min_per_day_first4", "active_min_per_day_min_after4",
        "active_min_per_day_max_after4", "avg_active_min_per_day_overall")}))
    print("[units] chunk30", json.dumps(summary["units"]["chunk30"]))
    print("[target_days] closed form exact from island", td["closed_form_first_island_f32_exact_onwards"],
          "covering", td["closed_form_islands_covered"], "islands, max err", td["closed_form_max_abs_err"],
          "| before:", td["closed_form_check_island_before_i0"])
    print("[target_days] hand increments", td["hand_increments_days"])
    print("[target_days] head minutes", td["head_minutes"], "d1", td["head_minutes_d1"], "d2", td["head_minutes_d2"])
    print("[target_days] gate OT == ITD:", td["gate_OT_equals_IslandTargetDays"], "/", td["gate_islands_with_gate_spawner"],
          "; max OT == ITD on", td["max_OT_equals_IslandTargetDays_islands"], "/", td["max_island_in_scene"], "islands")
    print("[spawner_window]", json.dumps({k: sw[k] for k in (
        "n_spawners", "class_counts", "fwd_ok", "rev_ok", "both_ok", "fwd_exact_equal", "fwd_round_eq", "fwd_round_eq_halfup_double",
        "fwd_ok_but_not_round_eq", "apply_only_n", "apply_only_by_island", "apply_only_max_abs_rel_dev", "itd_form_L4plus_n",
        "itd_form_L4plus_ok_0p05", "L1_3_minute_spawners", "L1_3_apply_with_tool_range", "L1_3_apply_with_60xITD_range", "rel_outside_0_1", "bake_present", "bake_eq_OT", "bake_neq_OT",
        "bake_neq_but_eq_OT_from_rel", "bake_neq_list", "tool_L4plus_min_max_eq_ITD", "tool_L1_3_max_eq_60xITD",
        "tool_L1_3_min_eq_60xITDprev", "early_offset_spawners", "early_offset_max_shift_days")}))
    for e in sw["exceptions"]:
        print("   exc", e)
    for g in sw["rel_outside_groups"]:
        print("   rel outside [0,1]", g)
    for g in sw["early_offset_by_island"]:
        print("   early offset", g)
    print("[evolution] cells", ev["cells"], "fill max err", ev["fill_rule_max_island_err"], "convert max err days",
          ev["convert_max_abs_err_days"], "steps dev", ev["steps_deviation_from_linear"],
          "armor-weapon start", ev["armor_minus_weapon_start"])
    for r in ev["rows"]:
        print("   ", r)
    oa = summary["other_island_axis"]
    print("[axis] interp knots", oa["interpolation_knots"], "max err", oa["interpolation_max_abs_err_days"],
          "amulet", [(r["level"], r["LevelTargetDays"], r["island_timing"]) for r in oa["amulet_levels"]],
          "FeatureUnlockDay", oa["amulet_FeatureUnlockDay"], "->", oa["amulet_FeatureUnlockDay_island_timing"])
    print("[spawner_window] rounding 2x2", sw["rounding_2x2"], "ties", json.dumps(sw["rounding_ties"]),
          "early offset where apply holds", sw["early_offset_spawners_where_apply_rule_holds"])
    print("[target_days] gate rel==1:", td["gate_rel_eq_1"], "minute gates", td["gate_minute_islands"], "day gate OTs",
          td["gate_day_OT"], "all integer", td["gate_day_OT_all_integer"], "other gates", td["other_gates"])
    print("[simulation] constant lag", json.dumps(sm["constant_lag"]), "minute-gated lag (active min)",
          sm["minute_gated_lag_after_target_active_min"], "real-day-only in bracket", sm["target_in_bracket_real_day_only"])
    print("[simulation] compared", sm["islands_compared"], "target in bracket", sm["target_in_bracket"],
          "late", sm["late_islands"], "early", sm["early_islands"], "lag counts", sm["lag_counts_within_content"])
    print("   lag!=0 rows", [(r["TimeChunkIndex"], r["bot_Location"], r["target_island"]) for r in lag_rows if r["lag_islands"] != 0])
    print("   active min/day", [(r["from_chunk"], r["to_chunk"], round(r["active_min_per_day"], 2)) for r in per_day])
    for r in sm["arrival"]:
        print("   ", {k: r.get(k) for k in ("island", "target_start_day", "arrival_lo_day", "arrival_hi_day",
                                             "dev_lo_days", "dev_hi_days", "target_in_bracket",
                                             "arrival_lo_active_hours", "arrival_hi_active_hours")})
    print("wrote", OUT)


if __name__ == "__main__":
    main()
