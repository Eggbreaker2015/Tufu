"""X2 (design 3.5): DpsData/DefenseData of every snapshot recomputed from the exported items, slots and stats.

``CalculateDpsData``/``CalculateDefenseData`` (``model.simcalc``) read ``GetForecastStatValue`` (Value under the
interpolation modifiers, :func:`..forecast_value`), the items with their ``Evolved`` flag (``items_ext.csv``), the
unlocked weapon/armor slots (``snapshots_ext.csv``) and the StatModifier coefficient at the balance timing (the
day d). Which modifiers the stats carry depends on when the caches were last computed:

* day snapshots and their early copies (the forced snapshot of a slow day): at the day change, before the modifier
  moves (``RpmFirst``), so K2/K3(d-1);
* other early snapshots: at the start of their slow tick (``CalculateSimulationData``), so K2/K3(d). A fight in the
  same tick changes Values and items after that, and the export does not carry the tick's starting state: once a
  policy fights (C7) these comparisons need it (a handoff); on a run without clears they are exact.

Bit for bit (not R2's approximation: slots and evolved flags are exported). ``k2_lag_discriminating`` counts the
day snapshots whose caches differ between K2(d-1) and K2(d): when it is 0 (no flat damage/defence stat yet, as on
an empty policy) the lag itself is not tested by this run, which the result says.
"""

from __future__ import annotations

from pathlib import Path

from ..model import simcalc as sc
from ..model.enums import WeaponType
from ..model.items import ItemState
from . import Check, CheckResult, Env, RunData, Snap, forecast_value, num, rel_diff, same

COLUMNS = ("list", "index", "time_chunk_index", "case", "k_day", "kind", "weapon_type", "csharp", "oracle", "rel_diff")


def forecast_stats(snapshot, env: Env, k_day: int) -> dict[int, float]:
    k2, k3 = env.k(k_day)
    return {st.key: forecast_value(st.key, st.value, k2, k3) for st in snapshot.stats}


def item_entries(s: Snap, env: Env, c: Check | None = None) -> list:
    out = []
    for it, flag in zip(s.snapshot.items, s.evolved()):
        if flag is None:
            flag = it.level != it.copies
            if c is not None:
                c.count("evolved_fallback_level_ne_copies")
        out.append((env.ctx.items[it.key], ItemState.from_snapshot(it.level, it.copies, bool(flag))))
    return out


def recompute(s: Snap, env: Env, k_day: int, c: Check | None = None) -> tuple[list, list]:
    """(DpsData, DefenseData) of snapshot ``s`` with the stats at K(k_day) and the coefficient at its day."""
    weapons, armors = s.slots
    if weapons is None or armors is None:
        raise ValueError(f"{s.list}[{s.index}]: snapshots_ext has no slot counts")
    stats = forecast_stats(s.snapshot, env, k_day)
    items = item_entries(s, env, c)
    return (sc.calculate_dps_data(env.ctx, stats, items, int(weapons), s.tc),
            sc.calculate_defense_data(env.ctx, stats, items, int(armors), s.tc))


def forced_keys(run: RunData) -> set:
    """(P bits, T bits, TC) of the forced day snapshots: their early copies carry the day change's caches."""
    return {(float(s.snapshot.playtime), float(s.snapshot.total_playtime), s.tc) for s in run.full}


def _compare(c: Check, run: RunData, s: Snap, case: str, k_day: int, calc, stored, kind: str) -> bool:
    ok_all = [w for w, _ in calc] == [w for w, _ in stored]
    if not ok_all:
        c.item(False, list=s.list, index=s.index, time_chunk_index=s.tc, case=case, k_day=k_day, kind=kind,
               weapon_type="order", csharp=str([w for w, _ in stored]), oracle=str([w for w, _ in calc]), rel_diff="")
        return False
    for pos, ((w, o), (_, v)) in enumerate(zip(calc, stored)):
        ok = c.same(run, s, (kind, pos), v, o)
        ok_all &= ok
        c.item(ok, list=s.list, index=s.index, time_chunk_index=s.tc, case=case, k_day=k_day, kind=kind,
               weapon_type=WeaponType(w).name, csharp=num(v), oracle=num(o), rel_diff=num(rel_diff(v, o)))
    return ok_all


def check_x2(run: RunData, env: Env, out_dir: str | Path | None = None) -> CheckResult:
    c = Check("X2", "DpsData/DefenseData from items, Evolved, slots and stats (bit for bit)", COLUMNS)
    forced = forced_keys(run)
    clears = sum(int(r["clears"]) for r in run.trace.daily) if run.trace else None
    for s in run.full + run.early:
        is_forced = s.list == "TimeChunkSnapshots" or (float(s.snapshot.playtime), float(s.snapshot.total_playtime), s.tc) in forced
        case = "day_change" if is_forced else "slow_tick"
        k_day = s.tc - 1 if is_forced else s.tc
        if s.slots[0] is None:
            c.count("no_slot_record")
            continue
        dps, defense = recompute(s, env, k_day, c)
        ok = _compare(c, run, s, case, k_day, dps, list(s.snapshot.dps_data), "DpsData")
        ok &= _compare(c, run, s, case, k_day, defense, list(s.snapshot.defense_data), "DefenseData")
        c.count(f"snapshots_{case}")
        if is_forced and s.list == "TimeChunkSnapshots" and s.tc > 0:
            lag0 = recompute(s, env, s.tc)
            if [v for _, v in lag0[0]] != [v for _, v in dps] or [v for _, v in lag0[1]] != [v for _, v in defense]:
                c.count("k2_lag_discriminating")
    if not c.counts.get("k2_lag_discriminating"):
        c.vacuous_part("k2_lag", "DpsData/DefenseData are the same under K2(d-1) and K2(d) on every day of this run "
                                 "(no flat damage/defence stat): the RpmFirst lag of the caches is not tested here")
    if clears == 0:
        c.vacuous_part("rewards_in_tick", "no clears: the slow-tick case never follows a reward or a drop")
    if c.counts.get("no_slot_record"):
        c.vacuous_part("unrecorded_snapshots", f"{c.counts['no_slot_record']} snapshot(s) without an engine record")
    return c.result(out_dir)
