"""The rebuilt engine in Unity (design 4.4, 4.5, 7.3 C3b): snapshot 0, the empty-policy clock for days 0-30,
the day-change order RpmFirst against the oracle under every rounding, write points, bosses, stop, disabled
modules, per-tick cost, and the Job-phase heartbeat.

Every engine job runs with ``engine_probe.null_policy`` (FindBestSpawner not called, minimal FastTick), so
these checks keep meaning the engine alone after C5/C7 fill the policy modules.
"""

from __future__ import annotations

import csv
import io
import json
import math
import struct
from pathlib import Path
from unittest import mock

import numpy as np
import pytest

from tufu_balance.golden import check, exact, same_set
from tufu_balance.model import interpolation as ip, simcalc as sc, snapshots as ss, spawners as sp, timeaxis as ta
from tufu_balance.model.data import load_interpolations, load_simulation_data
from tufu_balance.model.f32 import f32
from tufu_balance.model.items import ItemState
from tufu_balance.unity import engine, launcher, request as req, schema, spawner_map

pytestmark = pytest.mark.unity

NULL = engine.engine_probe(null_policy=True)
DAYS = 30
STOP_DAY = 6
ROUNDINGS = tuple(ip.ROUNDINGS)
# CutDamageFlat and CutDefenceFlat: main interpolation stats, so DpsData/DefenseData depend on K2.
FLAT_GRANTS = {8: 10.0, 16: 10.0}
STAT_FIELDS = (("StatsFromFeaturesFlat", "stats_from_features_flat"), ("PercentMultiplier", "percent_multiplier"),
               ("TotalFlatValue", "total_flat_value"), ("TotalValue", "total_value"))


def _ok(result):
    assert result.exit_code == 0, result.describe()
    return result


def _jobs():
    return [
        engine.engine_job("engine-30", day=DAYS, max_wall_seconds=300.0, probe=NULL),
        engine.engine_job("stop-6", day=STOP_DAY, max_wall_seconds=300.0, probe=NULL),
        engine.engine_job("modifier-first", day=DAYS, max_wall_seconds=300.0, probe=NULL,
                          params={"DpsCacheProvenance": engine.DPS_CACHE_PROVENANCE["ModifierFirst"]}),
        engine.engine_job("stale-tick-cache", day=DAYS, max_wall_seconds=300.0, probe=NULL,
                          params={"DpsCacheProvenance": engine.DPS_CACHE_PROVENANCE["StaleTickCache"]}),
        engine.engine_job("write-points", max_wall_seconds=300.0, probe=engine.engine_probe(write_points=True)),
        engine.engine_job("all-islands", day=DAYS, max_wall_seconds=300.0,
                          probe=engine.engine_probe(null_policy=True, unlock_through=17)),
        engine.engine_job("flat-stats", day=DAYS, max_wall_seconds=300.0,
                          probe=engine.engine_probe(null_policy=True, grant_flat=FLAT_GRANTS)),
    ]


@pytest.fixture(scope="module")
def engine_run(unity_run):
    return _ok(unity_run(req.make_request(_jobs())))


@pytest.fixture(scope="module")
def variant_run(unity_run):
    """stop-6 and the write-point probe again at another SimulationSpeed and without the harness's ready-cooldown
    zeroing (Q07)."""
    request = req.make_request([engine.engine_job("stop-6", day=STOP_DAY, max_wall_seconds=300.0, probe=NULL),
                                engine.engine_job("write-points", max_wall_seconds=300.0,
                                                  probe=engine.engine_probe(write_points=True))],
                               unity={"simulation_speed": 3}, sandbox={"zero_ready_cooldowns": False})
    return _ok(unity_run(request))


def _json(result, job, name):
    return json.loads(result.file(job, name))


def _full(result, job):
    return _json(result, job, "timechunk_full.json")["TimeChunkSnapshots"]


def _early(result, job):
    return _json(result, job, "simulation_data.json")["Snapshots"]


def _bits(x: float) -> bytes:
    return struct.pack("<d", x)


def _snapshot(s: dict) -> ss.SimulationSnapshot:
    return ss.SimulationSnapshot(
        f32(s["Playtime"]), f32(s["TotalPlaytime"]), s["Location"], s["TimeChunkIndex"],
        [ss.SimulationStat(st["Key"], st["Value"], st["StatsFromFeaturesFlat"], st["PercentMultiplier"],
                           st["TotalFlatValue"], st["TotalValue"], st["CurrentGpm"]) for st in s["Stats"]],
        [ss.SimulationItem(it["Key"], it["Level"], it["Copies"]) for it in s["Items"]],
        [(x["WeaponType"], x["Value"]) for x in s["DpsData"]], [(x["WeaponType"], x["Value"]) for x in s["DefenseData"]])


@pytest.fixture(scope="module")
def ctx(db):
    return sc.load_simcalc_context(db)


def _with_rounding(rounding):
    """The oracle's default rounding swapped for one run (ctx.coefficient and K4/K5 read the default)."""
    return mock.patch.object(ip, "default_rounding", lambda: rounding)


# ---------------------------------------------------------------- snapshot 0

def test_snapshot_zero_equals_the_shipped_snapshot(engine_run, db):
    shipped = load_simulation_data(db)
    full, early = _full(engine_run, "engine-30"), _early(engine_run, "engine-30")
    day0, early0 = full[0], early[0]
    ref = shipped.time_chunk_snapshots[0]
    assert day0["TimeChunkIndex"] == 0 and day0["Playtime"] == 0.0 and day0["TotalPlaytime"] == 0.0
    assert [st["Key"] for st in day0["Stats"]] == [st.key for st in ref.stats]
    assert [st["Value"] for st in day0["Stats"]] == [st.value for st in ref.stats]
    assert len(day0["Stats"]) == 30  # Unit.Stats of a new player (the stat lookup has 36 slots)
    assert [st["Value"] for st in early0["Stats"]] == [st["Value"] for st in day0["Stats"]]
    assert [(it["Key"], it["Level"], it["Copies"]) for it in day0["Items"]] == [("knife", 1, 1)]
    dps = {x["WeaponType"]: x["Value"] for x in day0["DpsData"]}
    assert math.isclose(dps[1], 9.2308, rel_tol=1e-4) and all(math.isclose(v, 0.92308, rel_tol=1e-4) for w, v in dps.items() if w != 1)
    assert all(x["Value"] == 0.0 for x in day0["DefenseData"])
    health = next(st for st in day0["Stats"] if st["Key"] == 1)
    assert math.isclose(health["CurrentGpm"], 2.8507, rel_tol=1e-4)
    assert all(st["CurrentGpm"] == 0.0 for st in early0["Stats"])
    # Bit for bit: DpsData, DefenseData and every day-list CurrentGpm of the shipped snapshot 0 (G20 at TC0, G21).
    assert [_bits(x["Value"]) for x in day0["DpsData"]] == [_bits(v) for _, v in ref.dps_data]
    assert [_bits(x["Value"]) for x in day0["DefenseData"]] == [_bits(v) for _, v in ref.defense_data]
    assert [_bits(st["CurrentGpm"]) for st in day0["Stats"]] == [_bits(st.current_gpm) for st in ref.stats]
    check("sim_engine", {
        "snapshot0.health_gpm": exact(health["CurrentGpm"], source="design 7.3 C3b", original=2.8507132548166316,
                                      note="shipped TC0 Health CurrentGpm, bit for bit"),
        "snapshot0.dps_cut": exact(dps[1], source="G21", original=9.23076956935184),
        "snapshot0.stats": exact(len(day0["Stats"]), source="design 7.3 C3b", original=30,
                                 note="design says 36 (the stat lookup size); Unit.Stats has 30"),
    })


# ---------------------------------------------------------------- the empty-policy clock (days 0-30)

def _reference_clock(cfg: ta.TimeAxisConfig, days: int, starts=(0.0, 20.0), intervals=(1.0, 5.0), fast_step=120.0):
    """Design 4.4/4.5 under an empty policy, in float32: idle steps up to the next due early snapshot (at most
    TimeStep), sessions ending at P >= P_offline + 10 (G22), 230-minute offlines, forced day snapshots, from
    day 4 one 120-minute offline step at a time. Returns the early and day lists as (P, T, TC) bit tuples."""
    F = np.float32
    P = T = F(0.0)
    d, last, end = 0, F(0.0), F(cfg.life_cycle_in_minutes[0])
    early, day = [(P, T, 0)], [(P, T, 0)]

    def interval(p):
        return F([v for s, v in zip(starts, intervals) if p >= s][-1])

    def day_index(t):
        return int(np.floor(F(t / F(cfg.spawner_charges_chunk_minutes))))

    while d < days:
        if cfg.use_fast_tick and d >= cfg.fast_tick_start_chunk_offset:
            while day_index(T) == d:
                T = F(T + F(fast_step))
        else:
            step = F(cfg.time_step_seconds / F(60.0))
            until = F(F(last + interval(P)) - P)
            if F(0.0) < until < step:
                step = until
            P, T = F(P + step), F(T + step)
            if P >= F(last + interval(P)):
                early.append((P, T, d))
                last = P
            if P >= end:
                T = F(T + F(cfg.life_cycle_in_minutes[1]))
                end = F(P + F(cfg.life_cycle_in_minutes[0]))
        c = day_index(T)
        if c != d:
            d = c
            day.append((P, T, d))
            if ta.writes_early_snapshot(d, cfg):
                early.append((P, T, d))
                last = P
    return early, day


def _ptt(rows):
    return [(f32(r["Playtime"]), f32(r["TotalPlaytime"]), r["TimeChunkIndex"]) for r in rows]


def test_empty_policy_clock_matches_the_reference(engine_run, db):
    cfg = ta.load_time_axis_config(db)
    early_ref, day_ref = _reference_clock(cfg, DAYS)
    early, full = _early(engine_run, "engine-30"), _full(engine_run, "engine-30")
    assert _ptt(full) == day_ref
    assert _ptt(early) == early_ref


def test_empty_policy_sessions_days_and_gating(engine_run, db):
    cfg = ta.load_time_axis_config(db)
    early, full = _early(engine_run, "engine-30"), _full(engine_run, "engine-30")
    m = engine_run.manifest("engine-30")["engine"]
    # TC = floor(T / 1440) everywhere (G14); slow-phase T - P is a whole number of 230-minute offlines (G22).
    for s in early + full:
        assert s["TimeChunkIndex"] == ta.time_chunk_index(s["TotalPlaytime"], cfg)
    for s in early:
        assert ta.offline_residual(s["Playtime"], s["TotalPlaytime"]) == 0.0
    lc = ta.check_lifecycle([(s["Playtime"], s["TotalPlaytime"]) for s in early],
                            ta.offline_pins([(s["TimeChunkIndex"], s["Playtime"], s["TotalPlaytime"]) for s in full], cfg))
    assert lc.chain_feasible and lc.violations == ()
    # Each offline k at the first tick with P >= P_{k-1} + 10; the next session ends 10 minutes after it.
    previous_end = cfg.life_cycle_in_minutes[0]
    for k, p, t, end in m["offline"]:
        assert previous_end <= p < previous_end + cfg.time_step_minutes and end == p + cfg.life_cycle_in_minutes[0]
        previous_end = end
    assert len(m["offline"]) == 4 * ta.SESSIONS_PER_DAY == m["life_cycle_index"]
    # Early cadence: 1 minute below P = 20, then 5 (P06), between non-forced rows of the same day.
    steps = [(a["Playtime"], round(b["Playtime"] - a["Playtime"], 6)) for a, b in zip(early, early[1:])
             if b["Playtime"] != a["Playtime"]]
    assert {d for p, d in steps if p < 19} == {1.0} and {d for p, d in steps if p >= 20} == {5.0}
    # Early-list gating (G23): forced snapshots of days 0-3 in both lists, none from day 4 on; the early copy
    # of a forced snapshot after an offline has dP = 0, so its CurrentGpm is NaN.
    assert [s["TimeChunkIndex"] for s in full] == list(range(DAYS + 1))
    assert max(s["TimeChunkIndex"] for s in early) == cfg.fast_tick_start_chunk_offset - 1
    forced_early = [i for i, s in enumerate(early) if i > 0 and s["Playtime"] == early[i - 1]["Playtime"]]
    assert [early[i]["TimeChunkIndex"] for i in forced_early] == [1, 2, 3]
    assert all(math.isnan(st["CurrentGpm"]) for i in forced_early for st in early[i]["Stats"])
    day4 = full[cfg.fast_tick_start_chunk_offset]
    assert not any(s["TotalPlaytime"] == day4["TotalPlaytime"] for s in early)
    # FastTick from day 4: P frozen, T in 120-minute steps, one day change per FastTick.
    fast = [s for s in full if s["TimeChunkIndex"] >= cfg.fast_tick_start_chunk_offset]
    assert len({s["Playtime"] for s in fast}) == 1
    assert {round(b["TotalPlaytime"] - a["TotalPlaytime"], 6) for a, b in zip(fast, fast[1:])} == {1440.0}
    days = {row[0]: row for row in m["days"]}
    for d in range(cfg.fast_tick_start_chunk_offset, DAYS):
        _, is_fast, ticks, afk_steps, offline, *_ = days[d]
        assert (is_fast, ticks, afk_steps, offline) == (1, 1, 12, 1440.0)
    assert m["counters"]["day_changes"] == DAYS and m["counters"]["fast_ticks"] == DAYS - cfg.fast_tick_start_chunk_offset
    # Trimmed export (G19): the rule's days up to 30.
    trimmed = _json(engine_run, "engine-30", "simulation_data.json")["TimeChunkSnapshots"]
    assert [s["TimeChunkIndex"] for s in trimmed] == ta.trimmed_time_chunk_days(DAYS)
    assert engine_run.manifest("engine-30")["export"]["time_chunk_trim"] == "GetTrimmedTimeChunkSnapshots"
    check("sim_engine", {
        "clock.early_snapshots": exact(len(early), source="design 4.5"),
        "clock.day_snapshots": exact(len(full), source="design 7.3 C3b", original=DAYS + 1),
        "clock.offlines_slow_phase": exact(len(m["offline"]), source="G22", original=24),
        "clock.trimmed_days": exact([s["TimeChunkIndex"] for s in trimmed], source="G19"),
        "clock.final_p_t": exact([full[-1]["Playtime"], full[-1]["TotalPlaytime"]], source="design 4.4"),
    })


# ---------------------------------------------------------------- day change against the oracle

def _x1_mismatches(snapshots, interp, rounding):
    out = []
    for s in snapshots:
        k2, k3 = ss.composition_coefficients(interp, s["TimeChunkIndex"], rounding)
        for st in s["Stats"]:
            e = ss.compose_stat(st["Key"], st["Value"], k2, k3)
            out += [(s["TimeChunkIndex"], st["Key"], f) for f, ef in STAT_FIELDS if _bits(st[f]) != _bits(getattr(e, ef))]
    return out


def _dps_mismatches(ctx, snapshots, interp, rounding, k2_lag, timing_lag, slots):
    """DpsData/DefenseData of day snapshots recomputed from Value, items and slots with K2/K3(d - k2_lag) on the
    stats and the StatModifier coefficient at d - timing_lag (X2)."""
    bad = []
    with _with_rounding(rounding):
        for i, s in enumerate(snapshots):
            d = s["TimeChunkIndex"]
            k2, k3 = ss.composition_coefficients(interp, max(d - k2_lag, 0), rounding)
            stats = {st["Key"]: ss.compose_stat(st["Key"], st["Value"], k2, k3).total_value for st in s["Stats"]}
            items = [(ctx.items[it["Key"]], ItemState.from_snapshot(it["Level"], it["Copies"], False)) for it in s["Items"]]
            weapons, armors = slots[i]
            dps = sc.calculate_dps_data(ctx, stats, items, weapons, max(d - timing_lag, 0))
            defense = sc.calculate_defense_data(ctx, stats, items, armors, max(d - timing_lag, 0))
            if [_bits(v) for _, v in dps] != [_bits(x["Value"]) for x in s["DpsData"]] or \
                    [_bits(v) for _, v in defense] != [_bits(x["Value"]) for x in s["DefenseData"]]:
                bad.append(d)
    return bad


def _gpm_mismatches(ctx, snapshots, rounding, order):
    with _with_rounding(rounding):
        replay = sc.replay_day_gpm(ctx, [_snapshot(s) for s in snapshots], order=order)
    bad, worst = [], 0.0
    for s, (_, values) in zip(snapshots, replay.replayed):
        for st in s["Stats"]:
            a, e = st["CurrentGpm"], values[st["Key"]]
            if _bits(a) != _bits(e):
                bad.append((s["TimeChunkIndex"], st["Key"]))
                worst = max(worst, abs(a - e) / max(abs(a), 1e-300))
    return bad, worst


def _slots(result, job):
    rows = csv.DictReader(io.StringIO(result.file(job, "snapshots_ext.csv").decode()))
    return [(int(r["unlocked_weapon_slots"]), int(r["unlocked_armor_slots"])) for r in rows if r["list"] == "TimeChunkSnapshots"]


def test_day_change_order_rpm_first_bitwise(engine_run, db, ctx):
    """X1 (columns from Value and K(d)), X2/X7 (DpsData and CurrentGpm under K2/K3(d-1), K4/K5 and the DpsData
    coefficient at d), per rounding; RPM never decreases."""
    interp = load_interpolations(db)
    full, early = _full(engine_run, "engine-30"), _early(engine_run, "engine-30")
    slots = _slots(engine_run, "engine-30")
    x1 = {r.value: len(_x1_mismatches(full + early, interp, r)) for r in ROUNDINGS}
    cells = sum(len(s["Stats"]) * len(STAT_FIELDS) for s in full + early)
    dps = {r.value: _dps_mismatches(ctx, full, interp, r, 1, 0, slots) for r in ROUNDINGS}
    gpm = {r.value: _gpm_mismatches(ctx, full, r, sc.RPM_FIRST) for r in ROUNDINGS}
    gpm_modifier_first = {r.value: _gpm_mismatches(ctx, full, r, sc.MODIFIER_FIRST)[0] for r in ROUNDINGS}
    matching = sorted(r for r in x1 if x1[r] == 0 and not dps[r] and not gpm[r][0])
    print(f"X1 mismatches of {cells} cells: {x1}; DpsData days off: {dps}; "
          f"Gpm cells off: { {r: len(v[0]) for r, v in gpm.items()} }; ModifierFirst Gpm: {gpm_modifier_first}")
    assert matching, "no rounding reproduces the C# columns, DpsData and Gpm bit for bit"
    assert all(not v for v in dps.values()) and all(not v[0] for v in gpm.values())
    assert all(gpm_modifier_first.values()), "ModifierFirst must differ from the engine's order somewhere"
    for st_index in range(len(full[0]["Stats"])):
        series = [s["Stats"][st_index]["CurrentGpm"] for s in full]
        assert all(b >= a for a, b in zip(series, series[1:])), full[0]["Stats"][st_index]["Key"]
    check("sim_engine", {
        "x1.cells": exact(cells, source="X1"),
        "x1.mismatches_by_rounding": exact(x1, source="X1, DP3", note="C# columns against compose_stat under K(d)"),
        "x1.matching_roundings": same_set(matching, source="DP3", note="roundings under which X1, X2 and X7 are bit-exact"),
        "x7.gpm_cells": exact(sum(len(s["Stats"]) for s in full), source="X7"),
        "x7.modifier_first_mismatches": exact(gpm_modifier_first, source="G20 ablation"),
    })


def test_p07_ablations_are_selectable(engine_run, db, ctx):
    """ModifierFirst reproduces the oracle's ModifierFirst replay; StaleTickCache keeps the last tick's DpsData
    (coefficient at d-1 in the slow phase, frozen at the last slow tick afterwards); both differ from RpmFirst."""
    interp = load_interpolations(db)
    base = _full(engine_run, "engine-30")
    mf, stale = _full(engine_run, "modifier-first"), _full(engine_run, "stale-tick-cache")
    assert engine_run.manifest("modifier-first")["engine"]["params"]["DpsCacheProvenance"] == 1
    assert engine_run.manifest("stale-tick-cache")["engine"]["params"]["DpsCacheProvenance"] == 2
    r = ip.Rounding(sorted(r.value for r in ROUNDINGS if not _x1_mismatches(base, interp, r))[0])
    assert not _gpm_mismatches(ctx, mf, r, sc.MODIFIER_FIRST)[0]
    assert _gpm_mismatches(ctx, mf, r, sc.RPM_FIRST)[0]
    assert [s["Stats"] for s in mf] != [s["Stats"] for s in base]
    slots = _slots(engine_run, "stale-tick-cache")
    offset = ta.FAST_TICK_START_CHUNK_OFFSET
    assert not _dps_mismatches(ctx, stale[1:offset], interp, r, 1, 1, slots[1:offset])
    assert all(s["DpsData"] == stale[offset]["DpsData"] for s in stale[offset:])
    assert [s["DpsData"] for s in stale] != [s["DpsData"] for s in base]
    assert [s["Stats"] for s in stale] != [s["Stats"] for s in base]


def test_day_change_k2_lag_with_flat_stats(engine_run, db, ctx):
    """X2 with K2 in play: flat Cut damage and defence granted before day 0, so DpsData/DefenseData change with
    K2. They match K2/K3(d-1) and not K2/K3(d); the day list's CurrentGpm still replays RpmFirst."""
    interp = load_interpolations(db)
    full, slots = _full(engine_run, "flat-stats"), _slots(engine_run, "flat-stats")
    base0 = {st["Key"]: st["Value"] for st in _full(engine_run, "engine-30")[0]["Stats"]}
    for s in (full[0], full[-1]):
        values = {st["Key"]: st["Value"] for st in s["Stats"]}
        assert all(values[k] == base0[k] + v for k, v in FLAT_GRANTS.items())
    assert all(x["Value"] > 0.0 for s in full for x in s["DefenseData"] if x["WeaponType"] == 1)
    x1 = {r.value: len(_x1_mismatches(full, interp, r)) for r in ROUNDINGS}
    matching = [r for r in ROUNDINGS if x1[r.value] == 0]
    lag1 = {r.value: _dps_mismatches(ctx, full, interp, r, 1, 0, slots) for r in ROUNDINGS}
    lag0 = {r.value: _dps_mismatches(ctx, full, interp, r, 0, 0, slots) for r in ROUNDINGS}
    gpm = {r.value: len(_gpm_mismatches(ctx, full, r, sc.RPM_FIRST)[0]) for r in ROUNDINGS}
    print(f"flat-stats X1: {x1}; DpsData days off, lag 1: {lag1}; lag 0: {lag0}; RpmFirst Gpm cells off: {gpm}")
    assert matching
    for r in matching:
        assert not lag1[r.value] and gpm[r.value] == 0, r
    assert all(lag0.values()), "with flat stats DpsData must tell K2(d-1) from K2(d)"
    check("sim_engine", {
        "flat_stats.x1_mismatches_by_rounding": exact(x1, source="X1"),
        "flat_stats.lag1_days_off": exact({k: len(v) for k, v in lag1.items()}, source="X2"),
        "flat_stats.lag0_days_off": exact({k: len(v) for k, v in lag0.items()}, source="X2"),
        "flat_stats.gpm_cells_off": exact(gpm, source="X7"),
    })


# ---------------------------------------------------------------- write points, bosses, stop

def _deltas(steps):
    """Counter increments of each probe step (the manifest records the running totals; snapshot counters, which
    snapshot 0 already moved before the first step, are left out)."""
    out, previous = {}, {}
    for step in steps:
        out[step["name"]] = {k: v - previous.get(k, 0) for k, v in step["counters"].items()
                             if v != previous.get(k, 0) and not k.startswith("snapshots_")}
        previous = step["counters"]
    return out


def _cooldown(text: str) -> float:
    return float(text)


def test_write_points(engine_run, db):
    m = engine_run.manifest("write-points")
    assert m["stop_reason"] == "simulation_stopped" and m["engine"]["stop_reason"] == "write_point_probe"
    probe = m["engine"]["probe"]
    steps = {s["name"]: s for s in probe["steps"]}
    delta = _deltas(probe["steps"])

    def usage(step):
        return steps[step]["before"]["usage"], steps[step]["after"]["usage"]

    def added(step, clears=1):
        """Each StatReward Value x Count x clears, as the float the modifier carries."""
        st = steps[step]
        return {str(r["stat"]): float(f32(r["value"] * st["count"] * clears)) for r in st["rewards"]}

    # Immediate (Count > 1): charges += Count per clear, cooldown = BaseCooldown, rewards Value x Count, Health heals.
    imm = steps["immediate"]
    count = imm["count"]
    assert count > 1
    assert usage("immediate") == (0, count) and usage("immediate_again") == (count, 2 * count)
    assert imm["after"]["cooldown"] == imm["after"]["base_cooldown"] != "0"
    assert delta["immediate"] == {"clears": 1, "kills": count}
    for stat, value in added("immediate").items():
        assert imm["after"]["stats"][stat] == imm["before"]["stats"][stat] + value
    assert imm["after"]["current_health"] == imm["before"]["current_health"] + added("immediate")["1"]
    again = steps["immediate_again"]
    assert again["after"]["stats"]["1"] == again["before"]["stats"]["1"] + added("immediate")["1"]  # merged modifier
    # Accumulate (Count > 1): charges and cooldown at once, no reward until the fast flush.
    acc = steps["accumulate"]
    assert acc["count"] > 1 and usage("accumulate") == (0, 2 * acc["count"])
    assert acc["after"]["cooldown"] == acc["after"]["base_cooldown"]
    assert acc["after"]["stats"] == acc["before"]["stats"] and acc["after"]["current_health"] == acc["before"]["current_health"]
    assert delta["accumulate"] == {"accumulate_calls": 1, "clears": 2, "kills": 2 * acc["count"]}
    # Charge cap: clears stop once used >= limit (the bot filter's rule), the last one may overshoot the cap.
    cap = steps["charge_cap"]
    allowed = -(-(cap["before"]["limit"] - cap["before"]["usage"]) // cap["count"])
    assert 0 < allowed < 1000 and cap["after"]["usage"] == cap["after"]["limit"]
    assert delta["charge_cap"] == {"accumulate_calls": 1, "clears": allowed, "kills": allowed * cap["count"],
                                   "charge_clamped": 1000 - allowed}
    # DAFC (with a Health reward): three clears asked, one applied; +inf, untouched by ResetCooldowns and offline.
    dafc = steps["dafc_clear"]
    assert dafc["rewards"] and delta["dafc_clear"] == {"dafc_clamped": 1, "clears": 1, "kills": dafc["count"]}
    for stat, value in added("dafc_clear").items():
        assert dafc["after"]["stats"][stat] == dafc["before"]["stats"][stat] + value
    assert dafc["after"]["usage"] == min(dafc["after"]["limit"], dafc["count"])
    assert dafc["after"]["cooldown"] == "Infinity"
    assert steps["reset_cooldowns"]["after"]["cooldown"] == "Infinity"
    off = steps["offline"]
    assert off["after"]["cooldown"] == "Infinity" and off["after"]["total_playtime"] == off["before"]["total_playtime"] + 230.0
    assert steps["reset_cooldowns_plain"]["after"]["cooldown"] == "0"
    # Gate: the island-1 boss opens island 2 once, its slot reward weapon_2, and currentBoss moves on.
    gate = steps["gate_clear"]
    assert (gate["before"]["unlocked_location"], gate["after"]["unlocked_location"]) == (1, 2)
    assert gate["after"]["current_boss"] == m["engine"]["bosses"]["2"] and delta["gate_clear"]["unlocks"] == 1
    assert probe["slots"]["weapon_2"] is True and delta["gate_clear"]["slot_unlocks"] == 1
    assert delta["gate_clear_again"] == {"clears": 1, "kills": 1}  # the gate is open: nothing to unlock again
    assert steps["gate_clear_again"]["after"]["unlocked_location"] == 2
    assert delta["unlock_again"] == {"repeat_unlocks": 1}  # a direct call to an open island counts and does nothing


def _rewards_by_spawner(db):
    """SoulReward / CurrencyReward of every registered spawner, keyed by runtime id (from the DB, not the engine)."""
    rows = {(r[0], int(r[1])): tuple(r)[2:] for r in db.execute(
        "SELECT scene, file_id, SoulReward, CurrencyReward, CurrencyRewardAmount FROM spawners")}
    return {spawner_map.runtime_id(spawner_map.runtime_path(d.scene, d.go_path), d.key): (d, *rows[d.spawner_id])
            for d in sp.registered(sp.load_spawners(db))}


def test_write_points_record_souls_and_currencies(engine_run, db):
    """Immediate clears record SoulReward x Count x clears and CurrencyRewardAmount x clears (design 4.5); the
    accumulate path records nothing until C5's flush."""
    probe = engine_run.manifest("write-points")["engine"]["probe"]
    delta = _deltas(probe["steps"])
    info = _rewards_by_spawner(db)
    souls_prev, money_prev, checked = {}, {}, 0
    for step in probe["steps"]:
        d, soul, currency, amount = info[step["spawner"]]
        clears = delta[step["name"]].get("clears", 0)
        immediate = clears and "accumulate_calls" not in delta[step["name"]]
        expect_souls, expect_money = dict(souls_prev), dict(money_prev)
        if immediate and soul and soul.strip():
            expect_souls[soul] = expect_souls.get(soul, 0.0) + clears * d.count
            checked += 1
        if immediate and currency and currency.strip():
            expect_money[currency] = expect_money.get(currency, 0.0) + clears * amount
        assert step["souls"] == expect_souls, step["name"]
        assert step["currencies"] == expect_money, step["name"]
        souls_prev, money_prev = step["souls"], step["currencies"]
    assert checked >= 3


def test_fast_tick_gate_flush(engine_run, db):
    """RebuildFlushFastTickGates consumes the gate set between two planning rounds: the gate opens its island,
    currentBoss moves on, the slot reward opens, the set is emptied, and no stat or clear changes (design 4.5)."""
    m = engine_run.manifest("write-points")
    steps = {s["name"]: s for s in m["engine"]["probe"]["steps"]}
    delta = _deltas(m["engine"]["probe"]["steps"])
    fl = steps["flush_fast_gates"]
    d = _rewards_by_spawner(db)[fl["spawner"]][0]
    before = fl["before"]["unlocked_location"]
    assert fl["spawner"] == m["engine"]["bosses"][str(before)]
    target = max(n for n in d.gate_next_locations)
    assert target == before + 1 and fl["after"]["unlocked_location"] == target
    assert fl["after"]["current_boss"] == m["engine"]["bosses"][str(target)]
    expected = {"unlocks": 1}
    if d.slot_reward:
        expected["slot_unlocks"] = 1
    assert delta["flush_fast_gates"] == expected
    assert fl["after"]["stats"] == fl["before"]["stats"] and fl["after"]["current_health"] == fl["before"]["current_health"]
    assert fl["after"]["watched"] == fl["before"]["watched"]
    assert fl["fast_tick_chunk_gate_spawners"] == 0
    assert all(s["fast_tick_chunk_gate_spawners"] == 0 for s in m["engine"]["probe"]["steps"])


def test_offline_minute_and_day_change(engine_run):
    """A minute of offline runs a finite cooldown down by 60 s and regenerates Regen x 60 (capped); the day change
    clears every charge usage and cooldown, except a cleared DAFC's +inf."""
    steps = {s["name"]: s for s in engine_run.manifest("write-points")["engine"]["probe"]["steps"]}
    pre = steps["clear_before_day_change"]["after"]
    assert pre["cooldown"] == pre["base_cooldown"] and _cooldown(pre["cooldown"]) > 60.0
    afk = steps["afk_one_minute"]
    b, a = afk["before"], afk["after"]
    assert b["current_health"] == 1.0 and b["regen"] > 0.0
    assert _cooldown(a["cooldown"]) == float(f32(_cooldown(b["cooldown"]) - 60.0))
    assert a["current_health"] == min(b["max_health"], 1.0 + b["regen"] * 60.0) < b["max_health"]
    assert a["total_playtime"] == b["total_playtime"] + 1.0 and a["playtime"] == b["playtime"]
    assert (b["day_index"], a["day_index"], a["time_chunk_index"]) == (0, 1, 0)
    day = steps["day_change"]
    before, after = day["before"]["watched"], day["after"]["watched"]
    assert day["after"]["time_chunk_index"] == 1
    assert all(s["usage"] > 0 for s in before.values()) and all(s["usage"] == 0 for s in after.values())
    finite = [k for k, s in before.items() if s["cooldown"] != "Infinity"]
    assert any(_cooldown(before[k]["cooldown"]) > 0.0 for k in finite)
    assert all(after[k]["cooldown"] == "0" for k in finite)
    assert [after[k]["cooldown"] for k in before if k not in finite] == ["Infinity"]


def test_early_snapshot_gpm_is_dvalue_over_dp(engine_run):
    """Early-list CurrentGpm = (V1 - V0) / (P1 - P0) in double, with a nonzero dValue (the probe's rewards)."""
    early = _early(engine_run, "write-points")
    assert [s["Playtime"] for s in early] == [0.0, 1.0, 1.0]
    v0 = {st["Key"]: st["Value"] for st in early[0]["Stats"]}
    dp = float(f32(early[1]["Playtime"])) - float(f32(early[0]["Playtime"]))
    rows = early[1]["Stats"]
    assert [_bits(st["CurrentGpm"]) for st in rows] == [_bits((st["Value"] - v0[st["Key"]]) / dp) for st in rows]
    assert any(st["CurrentGpm"] > 0.0 for st in rows)
    assert all(math.isnan(st["CurrentGpm"]) for st in early[2]["Stats"])  # the day-1 forced copy: dP = 0


def test_back_gates_are_noops_not_repeat_unlocks(engine_run, db):
    """X6: every back-gate (a gate to an island at or below its own) cleared with its target open is a no-op;
    gates to closed islands still unlock. Expectations from P6's spawner data, not from the engine."""
    probe = engine_run.manifest("write-points")["engine"]["probe"]
    steps = {s["name"]: s for s in probe["steps"]}
    delta = _deltas(probe["steps"])
    defs = sp.registered(sp.load_spawners(db))
    ident = {spawner_map.runtime_id(spawner_map.runtime_path(d.scene, d.go_path), d.key): d for d in defs}
    expected = [d for d in defs if any(n <= d.location for n in d.gate_next_locations)]
    back = [s for s in probe["steps"] if s["name"].startswith("back_gate:")]
    assert sorted(s["spawner"] for s in back) == sorted(i for i, d in ident.items() if d in expected) and len(back) == 4
    last = steps["unlock_for_back_gates"]["after"]["unlocked_location"]
    assert last == max(d.location for d in expected)
    noops = unlocks = 0
    for s in back:
        for n in dict.fromkeys(ident[s["spawner"]].gate_next_locations):
            if n <= last:
                noops += 1
            else:
                unlocks, last = unlocks + 1, n
    assert sum(delta[s["name"]].get("back_gate_noops", 0) for s in back) == noops >= len(back)
    assert sum(delta[s["name"]].get("unlocks", 0) for s in back) == unlocks
    assert not any("repeat_unlocks" in delta[s["name"]] for s in back)
    assert back[-1]["after"]["unlocked_location"] == last
    # In runs nothing re-unlocks either; structurally guaranteed (gates open once, open islands are no-ops), so
    # the run-level check only becomes informative once C5/C7 policies clear gates.
    for job in ("engine-30", "all-islands"):
        assert "repeat_unlocks" not in engine_run.manifest(job)["engine"]["counters"]
    assert engine_run.manifest("all-islands")["engine"]["counters"]["unlocks"] == 16
    check("sim_engine", {
        "write_points.back_gate_noops": exact(noops, source="X6", note="back-gates of islands 4, 11, 12, 13"),
    })


def test_engine_zeroes_ready_cooldowns_without_the_harness(engine_run, variant_run):
    """Q07: with the harness's zeroing off, PrepareSimulationRun zeroes every non-event, non-VIP cooldown itself."""
    on = engine_run.manifest("write-points")
    off = variant_run.manifest("write-points")
    assert off["ready_cooldowns_zeroed"] == 0 and on["ready_cooldowns_zeroed"] > 0
    assert on["engine"]["probe"]["nonzero_cooldowns_at_start"] == 0 == off["engine"]["probe"]["nonzero_cooldowns_at_start"]
    zeroed = off["engine"]["probe"]["prepare_cooldowns_zeroed"]
    assert zeroed > 0
    check("sim_engine", {
        "q07.engine_zeroed_without_harness": exact(zeroed, source="Q07"),
        "q07.engine_zeroed_after_harness": exact(on["engine"]["probe"]["prepare_cooldowns_zeroed"], source="Q07"),
    })


def test_regen_refills_to_the_previous_days_health(engine_run):
    """Day list: the offline time of day d-1 regenerates to that day's Health, so current_health(d) equals
    Health TotalValue(d-1) (the day change raises the maximum after the regen)."""
    side = [r for r in schema.read_csv("snapshots_ext.csv", engine_run.file("engine-30", "snapshots_ext.csv").decode())
            if r["list"] == "TimeChunkSnapshots"]
    full = _full(engine_run, "engine-30")
    health = [next(st["TotalValue"] for st in s["Stats"] if st["Key"] == 1) for s in full]
    assert side[0]["current_health"] == health[0]
    for d in range(1, len(full)):
        assert math.isclose(side[d]["current_health"], health[d - 1], rel_tol=1e-12), d
    assert len({r["current_health"] for r in side}) > DAYS // 2


def test_find_boss_matches_p6(engine_run, db):
    defs = sp.load_spawners(db)
    ident = {d.spawner_id: spawner_map.runtime_id(spawner_map.runtime_path(d.scene, d.go_path), d.key)
             for d in sp.registered(defs)}
    bosses = engine_run.manifest("engine-30")["engine"]["bosses"]
    for n in range(1, 18):
        p6 = sp.boss_for_location(n, sp.registered(defs))
        assert bosses[str(n)] == (None if p6 is None else ident[p6.spawner_id]), n
    assert bosses["17"] is None and bosses["4"].split("|")[0].endswith("/dragon")
    assert "boss_ambiguous" not in engine_run.manifest("engine-30")["engine"]["counters"]


def test_stop_at_day_is_exact(engine_run, variant_run):
    """stop.day 6: the run ends right after day 6's forced snapshot, at SimulationSpeed 500 within one frame; the
    outputs are the prefix of the 30-day run and identical at speed 3 without zero_ready_cooldowns."""
    m = engine_run.manifest("stop-6")
    full, early = _full(engine_run, "stop-6"), _early(engine_run, "stop-6")
    assert m["stop_reason"] == "simulation_stopped" and m["engine"]["stop_reason"] == "day"
    assert full[-1]["TimeChunkIndex"] == STOP_DAY == m["engine"]["time_chunk_index"]
    last = full[-1]
    assert m["engine"]["playtime_bits"] == struct.unpack("<i", struct.pack("<f", last["Playtime"]))[0]
    assert m["engine"]["total_playtime_bits"] == struct.unpack("<i", struct.pack("<f", last["TotalPlaytime"]))[0]
    long_full, long_early = _full(engine_run, "engine-30"), _early(engine_run, "engine-30")
    assert full == long_full[:STOP_DAY + 1] and early == long_early[:len(early)]
    assert m["counts"]["ticks"] == sum(row[2] for row in engine_run.manifest("engine-30")["engine"]["days"][:STOP_DAY])
    v = variant_run.manifest("stop-6")
    assert v["ready_cooldowns_zeroed"] == 0 and engine_run.manifest("stop-6")["ready_cooldowns_zeroed"] > 0
    for name in ("simulation_data.json", "timechunk_full.json", "snapshots_ext.csv", "items_ext.csv"):
        assert variant_run.file("stop-6", name) == engine_run.file("stop-6", name), name
    assert v["engine"] == m["engine"]


@pytest.mark.parametrize("switch", ["AutoGambling", "CrystalSimulation", "AutoBalanceAmulet"])
def test_disabled_modules_fail_the_job(unity_run, switch):
    request = req.make_request([engine.engine_job("unsupported", day=2, max_wall_seconds=120.0, probe=NULL,
                                                  config_overrides={switch: True})])
    result = unity_run(request)
    assert result.exit_code == 5, result.describe()
    m = result.manifest("unsupported")
    assert "not supported" in m["error"] and switch in m["error"]
    assert m["engine"]["failure"] == m["error"] and m["engine"]["stop_reason"] == "failed"


# ---------------------------------------------------------------- manifest, params, harness

def test_manifest_engine_block(engine_run):
    m = engine_run.manifest("engine-30")
    schema.validate_manifest(m)
    assert m["engine"]["provenance_enum"] == engine.PROVENANCE
    assert m["engine"]["params"] == engine.engine_defaults()
    assert m["engine"]["null_policy"] is True and m["engine"]["failure"] is None
    assert m["engine"]["modifier_day"] == DAYS
    export = m["export"]
    assert export["problems"] == [] and export["recorded_snapshot_ext"] == export["snapshots"] + export["time_chunk_snapshots_full"]
    side = schema.read_csv("snapshots_ext.csv", engine_run.file("engine-30", "snapshots_ext.csv").decode())
    assert len(side) == export["recorded_snapshot_ext"]
    assert all(r["unlocked_weapon_slots"] == 1 and r["unlocked_armor_slots"] == 0 and r["current_health"] is not None for r in side)
    items = schema.read_csv("items_ext.csv", engine_run.file("engine-30", "items_ext.csv").decode())
    assert {(r["key"], r["level"], r["copies"], r["evolved"], r["evolution_level"]) for r in items} == {("knife", 1, 1, False, 0)}
    files = export["files"]
    check("sim_engine", {
        "engine30.simulation_data_sha256": exact(files["simulation_data.json"]["sha256"], source="design 7.3 C3b",
                                                 note="empty policy, days 0-30, trimmed export"),
        "engine30.timechunk_full_sha256": exact(files["timechunk_full.json"]["sha256"], source="design 7.3 C3b"),
        "engine30.counters": exact(m["engine"]["counters"], source="design 7.3 C3b"),
    })


def test_heartbeat_follows_ticks_in_the_job_phase(unity_run):
    """A job whose frames run but which never ticks is a stall (C0b counted frames in the Job phase too)."""
    request = req.make_request([req.make_job("frames-only", "empty", day=None, max_ticks=None, max_wall_seconds=120.0,
                                             max_frames=10_000_000)], unity={"heartbeat_interval_s": 1.0})
    result = unity_run(request, opts=launcher.Options(stall_timeout_s=8.0))
    assert result.exit_code == 3, result.describe()
    assert "stalled" in result.report["reason"]


@pytest.mark.perf
def test_per_tick_cost_within_budget(engine_run):
    """Design 2.7: empty policy, slow tick <= 5 ms, FastTick <= 50 ms (medians: single ticks of a busy machine
    spike), also with every island unlocked; mean and p95 go to the Runbook."""
    rows = {}
    for job in ("engine-30", "all-islands"):
        t = json.loads(engine_run.file(job, "timings.json"))
        rows[job] = (t["slow_tick_ms"], t["fast_tick_ms"])
        print(job, json.dumps(rows[job]))
        assert t["slow_tick_ms"]["n"] > 0 and t["fast_tick_ms"]["n"] > 0
        assert t["slow_tick_ms"]["p50"] <= 5.0 and t["fast_tick_ms"]["p50"] <= 50.0
