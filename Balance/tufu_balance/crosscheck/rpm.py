"""X3, X7 and DP8 (design 3.4, 3.5): RPM and the per-spawner economics against ``model.simcalc``.

* X3: the day list's CurrentGpm recomputed along the run: at each day change ``CalculateRPMValueTimeChunk``
  (``forGPMBot = true``) on the day's state, the oracle's own running maximum from ``OnAwake``'s zeros.
* X7: the day-change order. Each day snapshot's CurrentGpm against the same computation started from the previous
  day snapshot's CurrentGpm (the engine's own value), under ``RpmFirst`` (K2/K3 of day d-1 on the stats, K4/K5 and
  the DpsData coefficient at d); ``ModifierFirst`` (K(d)) must differ somewhere, else the order is not tested.
  X2 covers the DpsData half of X7.
* DP8: on replay/checkpoint jobs with the ``crosscheck_dp8`` output, the C# ``spawnerData`` of each overridden
  state (``SimulationSystem.Crosscheck.cs``) field by field against ``simcalc.calculate_spawner_data`` on the state
  built independently from the shipped snapshot: at K(d) as the override leaves it, and at RpmFirst. The anchor is
  the C# RpmFirst RPM (a running maximum over the overridden days) against the shipped CurrentGpm under G20's cell
  test: 1380/1380 on the 46 shipped day snapshots.

The day-change state of a day snapshot d: stats ``GetForecastStatValue`` (:func:`..forecast_value`) of its Values
under K2/K3(d-1), its own DpsData/DefenseData (computed at that day change), its Location as the unlocked island,
day d, its recorded current health. On replay/checkpoint jobs the override recomputes the RPM after the forced
snapshot (Runbook 18.2), so X3/X7 do not apply there and say so.
"""

from __future__ import annotations

import math
from pathlib import Path
from typing import Mapping

from ..model import simcalc as sc
from ..model import spawners as sp
from ..model.enums import StatType
from ..model.data import load_simulation_data
from ..unity import override as ov, probes
from . import OUTPUT_DP8, REL_TOL, Check, CheckResult, Env, RunData, Snap, forecast_value, num, rel_diff, same, stat_name

COLUMNS = ("time_chunk_index", "stat", "stat_name", "csharp", "oracle", "rel_diff", "bit_equal")
FIELDS = ("health_delta", "fight_time", "player_dps", "enemy_dps", "reward_value", "player_rps", "farmed_time",
          "av_move_time", "av_regen_time")  # SpawnerData's nine doubles, in the C# export's order


def day_state(env: Env, s: Snap, k_day: int) -> sc.PlayerState:
    k2, k3 = env.k(k_day)
    snap = s.snapshot
    return sc.PlayerState(
        stats={st.key: forecast_value(st.key, st.value, k2, k3) for st in snap.stats}, dps_data=dict(snap.dps_data),
        defense_data=dict(snap.defense_data), unlocked_location=int(snap.location), day=s.tc,
        current_health=s.current_health, current_playtime=float(snap.playtime),
        item_keys=frozenset(it.key for it in snap.items))


def _rpm(env: Env, state: sc.PlayerState, previous: Mapping[int, float]) -> dict[int, float]:
    out, _, _ = sc.calculate_rpm_value_time_chunk(env.ctx, state, rpm=previous)
    return out


def _with(previous: Mapping[int, float], snap) -> dict[int, float]:
    out = dict(previous)
    out.update({st.key: st.current_gpm for st in snap.stats})
    return out


def _no_override(run: RunData, c: Check) -> bool:
    if run.override_active:
        c.vacuous_part("override", f"{run.mode} job: the override recomputes RPM after the forced snapshot "
                                   "(Runbook 18.2), the day-change path does not apply")
        return False
    if not run.full:
        c.vacuous_part("day_list", "no day snapshots")
        return False
    return True


def _cell(c: Check, d: int, key: int, got: float, want: float, table: str | None = None) -> None:
    rd = rel_diff(got, want)
    c.item(rd <= REL_TOL, table, time_chunk_index=d, stat=key, stat_name=stat_name(key), csharp=num(got),
           oracle=num(want), rel_diff=num(rd), bit_equal=int(same(got, want)))
    if same(got, want):
        c.count("bit_equal")


def check_x3(run: RunData, env: Env, out_dir: str | Path | None = None) -> CheckResult:
    c = Check("X3", "day-list CurrentGpm recomputed along the run (forGPMBot, running maximum)", COLUMNS)
    if _no_override(run, c):
        rpm = sc.initial_rpm()
        for s in run.full:
            rpm = _rpm(env, day_state(env, s, s.tc - 1), rpm)
            for st in s.snapshot.stats:
                _cell(c, s.tc, st.key, st.current_gpm, rpm.get(st.key, 0.0))
        c.counts["days"] = len(run.full)
        c.counts["nonzero_cells"] = sum(1 for s in run.full for st in s.snapshot.stats if st.current_gpm != 0.0)
    return c.result(out_dir)


def check_x7(run: RunData, env: Env, out_dir: str | Path | None = None) -> CheckResult:
    c = Check("X7", "day-change order: CurrentGpm under K2/K3(d-1) from the previous day's RPM", COLUMNS)
    if _no_override(run, c):
        previous = sc.initial_rpm()
        modifier_first_off = 0
        for s in run.full:
            want = _rpm(env, day_state(env, s, s.tc - 1), previous)
            alt = _rpm(env, day_state(env, s, s.tc), previous)
            for st in s.snapshot.stats:
                _cell(c, s.tc, st.key, st.current_gpm, want.get(st.key, 0.0))
                modifier_first_off += not same(st.current_gpm, alt.get(st.key, 0.0))
            previous = _with(previous, s.snapshot)
        c.counts["modifier_first_cells_off"] = modifier_first_off
        if modifier_first_off == 0:
            c.vacuous_part("order", "ModifierFirst gives the same CurrentGpm on every day: the order is not tested")
    return c.result(out_dir)


# ---------------------------------------------------------------- DP8

DP8_COLUMNS = {
    "DP8": ("day", "state", "spawner", "scene", "file_id", "csharp_index", "oracle_index", "present", "max_rel_diff",
            "worst_field", "fields_bit_equal"),
    "DP8_rpm": ("day", "stat", "stat_name", "csharp", "oracle", "rel_diff", "bit_equal", "shipped_gpm",
                "g20_match", "g20_replay_bit_equal"),
}


def decode_spawners(rows) -> list[tuple[str, list[float]]]:
    return [(rid, probes.doubles(hexes)) for rid, hexes in rows]


def _oracle_fields(x: sc.SpawnerData) -> list[float]:
    return [x.health_delta, x.fight_time, x.player_dps, x.enemy_dps, x.reward_value, x.player_rps, x.farmed_time,
            x.av_move_time, x.av_regen_time]


def _shipped_state(env: Env, snap, day: int, k_day: int) -> sc.PlayerState:
    """The override's state of ``day`` from the shipped snapshot, stats under K(k_day). The override sets the current
    health to the Health of K(d) (full); the RpmFirst state keeps it while Health moves to K(d-1)."""
    k2, k3 = env.k(k_day)
    health = next((forecast_value(st.key, st.value, *env.k(day)) for st in snap.stats if st.key == int(StatType.Health)), None)
    return sc.PlayerState(
        stats={st.key: forecast_value(st.key, st.value, k2, k3) for st in snap.stats}, dps_data=dict(snap.dps_data),
        defense_data=dict(snap.defense_data), unlocked_location=int(snap.location), day=int(day),
        current_health=health, current_playtime=float(snap.playtime), item_keys=frozenset(it.key for it in snap.items))


def _compare_spawners(c: Check, env: Env, day: int, state_name: str, csharp, oracle: Mapping) -> None:
    order = {sid: i for i, sid in enumerate(oracle)}
    seen = set()
    for i, (rid, values) in enumerate(csharp):
        sid = env.spawner_id(rid)
        seen.add(sid)
        x = oracle.get(sid)
        if x is None:
            c.item(False, "DP8", day=day, state=state_name, spawner=rid, scene=sid and sid[0], file_id=sid and sid[1],
                   csharp_index=i, oracle_index="", present="csharp_only", max_rel_diff="", worst_field="",
                   fields_bit_equal="")
            continue
        o = _oracle_fields(x)
        diffs = [rel_diff(a, b) for a, b in zip(values, o)]
        worst = max(range(len(diffs)), key=lambda j: diffs[j])
        bit_equal = sum(same(a, b) for a, b in zip(values, o))
        c.count("fields")
        c.count("fields_bit_equal", bit_equal)
        c.counts["fields_max_rel_diff"] = max(c.counts.get("fields_max_rel_diff", 0.0), diffs[worst])
        c.item(diffs[worst] <= REL_TOL and len(values) == len(FIELDS), "DP8", day=day, state=state_name, spawner=rid,
               scene=sid[0], file_id=sid[1], csharp_index=i, oracle_index=order[sid], present="both",
               max_rel_diff=num(diffs[worst]), worst_field=FIELDS[worst], fields_bit_equal=bit_equal)
        c.count("order_differs", int(i != order[sid]))
    for sid in oracle:
        if sid not in seen:
            c.item(False, "DP8", day=day, state=state_name, spawner=env.runtime_id(sid), scene=sid[0], file_id=sid[1],
                   csharp_index="", oracle_index=order[sid], present="oracle_only", max_rel_diff="", worst_field="",
                   fields_bit_equal="")


def check_dp8(run: RunData, env: Env, out_dir: str | Path | None = None) -> CheckResult:
    c = Check("DP8", "spawnerData of the shipped day-snapshot states and the RpmFirst RPM anchor", DP8_COLUMNS)
    records = [r for r in run.override_days if "crosscheck_dp8" in r]
    if not records:
        why = (f"the job has no {OUTPUT_DP8} records" if run.override_active
               else "not a replay/checkpoint job: DP8 runs on the override's states")
        c.vacuous_part("states", why)
        return c.result(out_dir)
    shipped = load_simulation_data(env.con)
    evolved = ov.items_evolved_default(env.con)
    defs = sp.load_spawners(env.con)
    replay = dict(sc.replay_day_gpm(env.ctx, shipped.time_chunk_snapshots, order=sc.RPM_FIRST).replayed)
    chain = sc.initial_rpm()
    shipped_days = {s.time_chunk_index for s in shipped.time_chunk_snapshots}
    g20 = cells = 0
    for r in records:
        x = r["crosscheck_dp8"]
        d = int(x["day"])
        e = ov.expected_day(shipped, run.mode, d, evolved)
        snap = e.snapshot
        if int(r["location"]) != int(snap.location):
            c.count("location_differs")  # the override must run with unlock = snapshot (Runbook 18.6)
        boss = sp.boss_for_location(int(snap.location), defs)
        kw = {"current_boss": boss.spawner_id if boss is not None else None}
        if r.get("current_boss") != (env.runtime_id(boss.spawner_id) if boss is not None else None):
            c.count("current_boss_differs")
        gpm_state = _shipped_state(env, snap, d, d)
        _compare_spawners(c, env, d, "gpm", decode_spawners(x["gpm"]), sc.calculate_spawner_data(env.ctx, gpm_state, True, **kw))
        rf = x["rpm_first"]
        rf_state = _shipped_state(env, snap, d, d - 1)
        cs_stats = {int(k): probes.doubles(h)[0] for k, h in rf["stats"]}
        c.count("rpm_first_stats_bit_equal", sum(same(cs_stats.get(k, 0.0), v) for k, v in rf_state.stats.items()))
        _compare_spawners(c, env, d, "rpm_first", decode_spawners(rf["spawners"]),
                          sc.calculate_spawner_data(env.ctx, rf_state, True, **kw))
        chain = _rpm(env, rf_state, chain)
        cs_rpm = {int(k): probes.doubles(h)[0] for k, h in rf["rpm"]}
        stored = {st.key: st.current_gpm for st in snap.stats} if d in shipped_days else {}
        for key in sorted(chain):
            got, want = cs_rpm.get(key, 0.0), chain[key]
            rd = rel_diff(got, want)
            row = dict(day=d, stat=key, stat_name=stat_name(key), csharp=num(got), oracle=num(want), rel_diff=num(rd),
                       bit_equal=int(same(got, want)), shipped_gpm="", g20_match="", g20_replay_bit_equal="")
            if key in stored:
                hit = sc.gpm_matches(stored[key], got)
                cells += 1
                g20 += hit
                row.update(shipped_gpm=num(stored[key]), g20_match=int(hit),
                           g20_replay_bit_equal=int(same(got, replay.get(d, {}).get(key, math.nan))))
                c.count("g20_replay_bit_equal", row["g20_replay_bit_equal"])
            c.item(rd <= REL_TOL and row["g20_match"] != 0, "DP8_rpm", **row)
    c.counts.update(states=len(records), shipped_states=sum(int(r["crosscheck_dp8"]["day"]) in shipped_days for r in records),
                    g20_cells=cells, g20_matches=g20)
    return c.result(out_dir)
