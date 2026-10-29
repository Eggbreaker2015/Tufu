"""X5 (design 3.5): each day's eligible set of the C# run against ``model.simcalc`` on the same state.

``daily.csv`` carries, per day, the bot-filter set of the day's first planning round (``CalculateSpawnerData(false)``
right after ``CalculateSimulationData``; C3c's ``RebuildTraceEligible``) as a count and a signature of the sorted
runtime ids; days without a planning round have ``-1`` (the minimal FastTick: no planning on a null policy).

The oracle state is the day's start, i.e. its forced snapshot: Values under K2/K3(d) (the modifier moved at the day
change), DpsData/DefenseData recomputed from items and slots at K(d) (the planning's ``CalculateSimulationData``),
the snapshot's Location, P and current health. Bot filter: charges were cleared and cooldowns reset at the day
change, except the ``+inf`` of a DAFC spawner cleared on an earlier day (``first_kill`` events). The comparison is
``simcalc.calculate_spawner_data(forGPMBot = false)`` (alive, cooldown, charges, OT cap, unlocked island,
``fightTime < 300``); ``eligible_spawners`` (the fast kill gate on top) is counted alongside for C5.

Handoff (C5): if the fast phase plans after flushing the previous day's accumulated rewards, the planning state is
the flushed one, not the forced snapshot; the digest text (``digest_text`` output) then has to supply it.
"""

from __future__ import annotations

from pathlib import Path

from ..model import simcalc as sc
from ..unity import trace as tr
from . import Check, CheckResult, Env, RunData, forecast_value
from .dps import item_entries
from .rpm import count_health, health_coverage

COLUMNS = ("day", "phase", "csharp_count", "oracle_count", "csharp_signature", "oracle_signature",
           "oracle_killable_count", "cleared_dafc")


def cleared_dafc_before(run: RunData, env: Env, day: int, dafc: set) -> frozenset:
    out = set()
    for e in (run.trace.events if run.trace else []):
        if e["type"] == "first_kill" and int(e["day"]) < day:
            sid = env.spawner_id(e["spawner"])
            if sid in dafc:
                out.add(sid)
    return frozenset(out)


def planning_state(env: Env, s) -> sc.PlayerState:
    k2, k3 = env.k(s.tc)
    snap = s.snapshot
    stats = {st.key: forecast_value(st.key, st.value, k2, k3) for st in snap.stats}
    weapons, armors = s.slots
    items = item_entries(s, env)
    return sc.PlayerState(
        stats=stats, dps_data=dict(sc.calculate_dps_data(env.ctx, stats, items, int(weapons), s.tc)),
        defense_data=dict(sc.calculate_defense_data(env.ctx, stats, items, int(armors), s.tc)),
        unlocked_location=int(snap.location), day=s.tc, current_health=s.current_health,
        current_playtime=float(snap.playtime), item_keys=frozenset(it.key for it in snap.items))


def check_x5(run: RunData, env: Env, out_dir: str | Path | None = None) -> CheckResult:
    c = Check("X5", "daily eligible sets against simcalc on the day's state", COLUMNS)
    if run.trace is None or not run.trace.daily:
        c.vacuous_part("daily", "the job wrote no daily.csv")
        return c.result(out_dir)
    if run.override_active:
        c.vacuous_part("override", "C4 applies its state after the forced snapshot and replaces planning caches; "
                                     "the forced snapshot is not the planning state")
        return c.result(out_dir)
    by_day = {s.tc: s for s in run.full}
    dafc = {s.spawner_id for s in env.ctx.spawners if s.d.dafc}
    fast_from = env.ctx.time_axis.fast_tick_start_chunk_offset if env.ctx.time_axis.use_fast_tick else None
    for row in run.trace.daily:
        d = int(row["day"])
        phase = "fast" if fast_from is not None and d >= fast_from else "slow"
        if int(row["eligible_count"]) < 0:
            c.count(f"no_planning_{phase}")
            continue
        s = by_day.get(d)
        if s is None or s.slots[0] is None:
            c.count("no_state")
            continue
        cleared = cleared_dafc_before(run, env, d, dafc)
        state = planning_state(env, s)
        count_health(c, state)
        data = sc.calculate_spawner_data(env.ctx, state, for_gpm_bot=False, bot=sc.BotFilter(cleared_dafc=cleared))
        ids = [env.runtime_id(sid) for sid in data]
        killable = sc.eligible_spawners(env.ctx, state, sc.BotFilter(cleared_dafc=cleared))
        sig = tr.set_signature(ids)
        c.item(len(ids) == int(row["eligible_count"]) and sig == (row["eligible_signature"] or ""), day=d, phase=phase,
               csharp_count=row["eligible_count"], oracle_count=len(ids), csharp_signature=row["eligible_signature"] or "",
               oracle_signature=sig, oracle_killable_count=len(killable), cleared_dafc=len(cleared))
        c.count(f"days_{phase}")
        c.count("dafc_cleared_excluded", sum(1 for sid in cleared if sid not in data))
    health_coverage(c)
    for phase in ("slow", "fast"):
        n = c.counts.get(f"no_planning_{phase}", 0)
        if n:
            c.vacuous_part(f"{phase}_days_without_planning",
                           f"{n} {phase} day(s) without a planning round (eligible_count = -1: "
                           f"{'the minimal FastTick of the null policy' if phase == 'fast' else 'no slow tick ran'})")
    if not c.counts.get("days_fast"):
        c.vacuous_part("fast_phase", "no fast day has an eligible set: the fast phase's set (C5) is not tested by this run")
    return c.result(out_dir)
