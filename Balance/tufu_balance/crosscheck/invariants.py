"""X6 (design 3.5): trajectory invariants of a run.

* ``values``: Value = base (``Player Config``) + test grants (``engine_probe.grant_flat``) + the
  rewards of the clears so far, equipment modifiers excluded (the snapshot Value already leaves them out). Without
  clears every snapshot is checked. With clears, day snapshots are checked against closed charge usage.
  Slow days replay each cleared slow_fight event in order (including en-route fights), checking the counts
  against the digest. Fast days make one float32 reward write per stat at the flush. Each write rounds both
  the reward and the merged modifier to float32, as StatBase.AddModifier does. BaseValue is double and
  __probeGrant is a separate modifier: neither belongs in the merged Devoured accumulator. Values are
  bit-exact on both phases; forced day snapshots must have no pending fast rewards.
* ``charges``: no usage (``charges``, ``charges_closed`` in the digests) above ``GetSpawnerChargesLimit``.
* ``clock``: P and T never decrease along each list and ``daily.csv``; the day list's T strictly increases.
* ``time_chunk``: TC = floor((float)(T / SpawnerChargesChunkMinutes)) on every snapshot (the editor's double
  quotient stored to float, as ``RebuildDayIndex``).
* ``day_list``: the untrimmed day list has every day 0..N once, in order; the exported list is
  ``GetTrimmedTimeChunkSnapshots``' days (``timeaxis.trimmed_time_chunk_days``) and ``snapshots_ext``'s
  ``exported_index`` numbers exactly those rows.
* ``dafc``: a DisableAfterFirstClear spawner is cleared at most once per run (closed usage <= one clear in total),
  its cooldown stays ``+inf`` afterwards, and later days' eligible sets leave it out (X5 compares those sets with
  the oracle, whose bot filter excludes it).
* ``unlocks``: each island unlocks once (``unlock`` events strictly increasing, no ``repeat_unlocks``).

A part with nothing to check (no clears, no digest text, no DAFC clear, no unlock) is listed as vacuous.
"""

from __future__ import annotations

from collections import Counter
import math
import struct
from pathlib import Path

import numpy as np

from ..model.f32 import f32
from ..model.timeaxis import trimmed_time_chunk_days
from . import Check, CheckResult, Env, RunData, num, rel_diff, same, stat_name

COLUMNS = ("invariant", "where", "csharp", "oracle", "detail")
VALUE_REL_TOL = 1e-6
INF_BITS = "7f800000"


def _float_bits(text: str) -> float:
    return struct.unpack(">f", bytes.fromhex(text))[0]


def base_values(env: Env) -> dict[int, float]:
    return {int(r[0]): float(r[1]) for r in env.con.execute('SELECT Type, BaseValue FROM c_PlayerConfig__Stats ORDER BY "Stats#idx"')}


def grants(run: RunData) -> dict[int, float]:
    probe = (run.manifest.get("job") or {}).get("engine_probe") or {}
    return {int(k): float(f32(v)) for k, v in (probe.get("grant_flat") or {}).items()}


def total_clears(run: RunData) -> int:
    counters = (run.manifest.get("engine") or {}).get("counters") or {}
    daily = sum(int(r["clears"]) for r in run.trace.daily) if run.trace else 0
    return max(int(counters.get("clears", 0)), daily)


def closed_rewards(env: Env, fields: dict[str, str]) -> tuple[int | None, dict[int, float], dict[str, int]]:
    """(closed day, reward per stat, clears per spawner id) from a digest's ``charges_closed`` lines."""
    day = fields.get("charges_closed.day")
    by_id = {s.spawner_id: s for s in env.ctx.spawners}
    rewards: dict[int, float] = {}
    clears: dict[str, int] = {}
    for key, value in fields.items():
        if not key.startswith("charges_closed.") or key == "charges_closed.day":
            continue
        rid = key[len("charges_closed."):]
        s = by_id.get(env.spawner_id(rid))
        if s is None:
            raise ValueError(f"digest: unknown spawner {rid}")
        n = math.ceil(int(value) / s.d.count)
        clears[rid] = n
        for r in s.stat_rewards:
            rewards[r.stat_type] = rewards.get(r.stat_type, 0.0) + float(r.value) * s.d.count * n
    return (None if day is None else int(day)), rewards, clears


def _values(c: Check, run: RunData, env: Env) -> None:
    base = base_values(env)
    extra = grants(run)
    for s in run.full:
        for key, value in run.digests.get(s.tc, {}).items():
            if key.startswith("fast.stat."):
                pending = _float_bits(value.split(",")[3])
                c.item(pending == 0.0, invariant="values", where=f"day {s.tc} {key}",
                       csharp=num(pending), oracle="0", detail="forced snapshot follows fast reward flush")
    clears = total_clears(run)
    if clears == 0:
        for s in run.early + run.full:
            for st in s.snapshot.stats:
                want = base.get(st.key, 0.0) + extra.get(st.key, 0.0)
                c.item(same(st.value, want), invariant="values", where=f"{s.list}[{s.index}] tc {s.tc} {stat_name(st.key)}",
                       csharp=num(st.value), oracle=num(want), detail="no clears: base + grants")
        c.counts["values_exact_cells"] = sum(len(s.snapshot.stats) for s in run.early + run.full)
        c.vacuous_part("values_rewards", "no clears in this run: Value = base + grants exactly; the reward sum is not tested")
        return
    if not run.digests:
        c.vacuous_part("values", f"{clears} clears but no digest text (output digest_text): rewards cannot be reconstructed")
        return
    cumulative: dict[int, float] = {}
    by_id = {env.runtime_id(sp.spawner_id): sp for sp in env.ctx.spawners}
    job = run.manifest.get("job") or {}
    switches = job.get("config_overrides") or {}
    fast_start = switches.get("FastTickStartChunkOffset", env.ctx.time_axis.fast_tick_start_chunk_offset)
    use_fast = switches.get("UseFastTick", env.ctx.time_axis.use_fast_tick)
    events = run.trace.events if run.trace else []
    for s in run.full:
        d = s.tc
        fields = run.digests.get(d)
        if fields is None:
            c.count("values_days_without_digest")
            continue
        closed_day, rewards, day_clears = closed_rewards(env, fields)
        if d > 0 and closed_day != d - 1:
            c.item(False, invariant="values", where=f"day {d}", csharp=str(closed_day), oracle=str(d - 1),
                   detail="charges_closed.day is not the previous day")
            continue
        slow = closed_day is not None and (not use_fast or closed_day < fast_start)
        if slow:
            fights = [e for e in events if e["type"] == "slow_fight" and e["day"] == closed_day and e["cleared"]]
            if not fights and day_clears:
                c.vacuous_part("values", "slow-phase clears need slow_fight events to replay float32 writes; "
                                        "later accumulators cannot be reconstructed exactly")
                return
            observed = dict(Counter(e["spawner"] for e in fights))
            c.item(observed == day_clears, invariant="values", where=f"day {d} slow writes",
                   csharp=str(observed), oracle=str(day_clears), detail="cleared events match closed charges")
            writes = [(r.stat_type, float(r.value) * by_id[e["spawner"]].d.count)
                      for e in fights for r in by_id[e["spawner"]].stat_rewards]
        else:
            writes = list(rewards.items())
        for k, v in writes:
            cumulative[k] = float(f32(cumulative.get(k, 0.0) + float(f32(v))))
        for st in s.snapshot.stats:
            got = st.value
            want = base.get(st.key, 0.0) + (extra.get(st.key, 0.0) + cumulative.get(st.key, 0.0))
            c.item(same(got, want), invariant="values", where=f"day {d} {stat_name(st.key)}", csharp=num(got),
                   oracle=num(want), detail="bit-exact float32 modifier writes; double base + grants")
            c.count("values_exact_cells")


def _charges(c: Check, run: RunData, env: Env) -> None:
    if not run.digests:
        c.vacuous_part("charges", "no digest text (output digest_text): charge usage is not exported otherwise")
        return
    by_id = {s.spawner_id: s for s in env.ctx.spawners}
    n = 0
    for d, fields in sorted(run.digests.items()):
        for key, value in fields.items():
            if not (key.startswith("charges.") or key.startswith("charges_closed.")) or key == "charges_closed.day":
                continue
            rid = key.split(".", 1)[1]
            s = by_id.get(env.spawner_id(rid))
            limit = env.ctx.charges_limit(s) if s is not None else None
            n += 1
            c.item(limit is not None and int(value) <= limit, invariant="charges", where=f"day {d} {key}",
                   csharp=value, oracle=str(limit), detail="usage <= GetSpawnerChargesLimit")
    if n == 0:
        c.vacuous_part("charges", "no charges used on any day")


def _clock(c: Check, run: RunData) -> None:
    for lst in (run.early, run.full):
        for a, b in zip(lst, lst[1:]):
            pa, pb = float(a.snapshot.playtime), float(b.snapshot.playtime)
            ta, tb = float(a.snapshot.total_playtime), float(b.snapshot.total_playtime)
            strict = b.list == "TimeChunkSnapshots"
            ok = pb >= pa and (tb > ta if strict else tb >= ta)
            c.item(ok, invariant="clock", where=f"{b.list}[{a.index}->{b.index}]", csharp=f"P {num(pb)} T {num(tb)}",
                   oracle=f"P >= {num(pa)}, T {'>' if strict else '>='} {num(ta)}", detail="")
    rows = run.trace.daily if run.trace else []
    for a, b in zip(rows, rows[1:]):
        ok = float(b["playtime"]) >= float(a["playtime"]) and float(b["total_playtime"]) >= float(a["total_playtime"])
        c.item(ok, invariant="clock", where=f"daily day {a['day']}->{b['day']}", csharp=f"{b['playtime']},{b['total_playtime']}",
               oracle=f">= {a['playtime']},{a['total_playtime']}", detail="")


def _time_chunk(c: Check, run: RunData, env: Env) -> None:
    chunk = float(env.ctx.time_axis.spawner_charges_chunk_minutes)
    for s in run.early + run.full:
        want = int(np.floor(f32(float(s.snapshot.total_playtime) / chunk)))
        c.item(s.tc == want, invariant="time_chunk", where=f"{s.list}[{s.index}]", csharp=str(s.tc), oracle=str(want),
               detail=f"T {num(s.snapshot.total_playtime)}")


def _day_list(c: Check, run: RunData) -> None:
    tcs = [s.tc for s in run.full]
    c.item(tcs == list(range(len(tcs))), invariant="day_list", where="timechunk_full", csharp=f"{len(tcs)} days",
           oracle=f"0..{len(tcs) - 1}", detail="every day once, in order")
    if not tcs:
        return
    want = trimmed_time_chunk_days(tcs[-1])
    c.item(run.exported_days == want, invariant="day_list", where="simulation_data.TimeChunkSnapshots",
           csharp=str(run.exported_days), oracle=str(want), detail="GetTrimmedTimeChunkSnapshots")
    exported = [s.ext.get("exported_index") if s.ext else None for s in run.full]
    keep = set(want)
    expected = []
    n = 0
    for s in run.full:
        expected.append(n if s.tc in keep else None)
        n += s.tc in keep
    c.item(exported == expected, invariant="day_list", where="snapshots_ext.exported_index", csharp=str(exported[:12]),
           oracle=str(expected[:12]), detail="exported_index numbers the kept days")


def _dafc(c: Check, run: RunData, env: Env) -> None:
    dafc = {s.spawner_id: s for s in env.ctx.spawners if s.d.dafc}
    events = run.trace.events if run.trace else []
    kills = {}
    for e in events:
        if e["type"] == "first_kill":
            sid = env.spawner_id(e["spawner"])
            if sid in dafc:
                kills.setdefault(sid, []).append(int(e["day"]))
    for sid, days in sorted(kills.items()):
        c.item(len(days) == 1, invariant="dafc", where=env.runtime_id(sid), csharp=str(days), oracle="one first_kill",
               detail="")
    if not kills:
        c.vacuous_part("dafc", "no DisableAfterFirstClear spawner was cleared in this run")
        return
    if not run.digests:
        c.vacuous_part("dafc_digest", "no digest text: clears per DAFC spawner and its cooldown are not checked")
        return
    total: dict[str, int] = {}
    for d, fields in sorted(run.digests.items()):
        _, _, clears = closed_rewards(env, fields)
        for rid, n in clears.items():
            if env.spawner_id(rid) in dafc:
                total[rid] = total.get(rid, 0) + n
        for sid, days in kills.items():
            if d > days[0]:
                rid = env.runtime_id(sid)
                got = fields.get(f"cooldown.{rid}", "")
                c.item(got == INF_BITS, invariant="dafc", where=f"day {d} cooldown {rid}", csharp=got, oracle=INF_BITS,
                       detail="cleared DAFC stays at +inf")
    for rid, n in sorted(total.items()):
        c.item(n <= 1, invariant="dafc", where=rid, csharp=str(n), oracle="<= 1", detail="clears over the run")


def _unlocks(c: Check, run: RunData) -> None:
    counters = (run.manifest.get("engine") or {}).get("counters") or {}
    c.item(int(counters.get("repeat_unlocks", 0)) == 0, invariant="unlocks", where="manifest.engine.counters",
           csharp=str(counters.get("repeat_unlocks", 0)), oracle="0", detail="repeat_unlocks")
    locations = [int(e["location"]) for e in (run.trace.events if run.trace else []) if e["type"] == "unlock"]
    if not locations:
        c.vacuous_part("unlocks", "no island was unlocked in this run")
        return
    c.item(all(b > a for a, b in zip(locations, locations[1:])) and len(set(locations)) == len(locations),
           invariant="unlocks", where="events.jsonl", csharp=str(locations), oracle="strictly increasing, once each",
           detail="")
    c.item(len(locations) == int(counters.get("unlocks", 0)), invariant="unlocks", where="manifest.engine.counters",
           csharp=str(counters.get("unlocks", 0)), oracle=str(len(locations)), detail="unlock events = counter")


def check_x6(run: RunData, env: Env, out_dir: str | Path | None = None) -> CheckResult:
    c = Check("X6", "trajectory invariants", COLUMNS)
    if run.override_active:
        c.vacuous_part("values", f"{run.mode} job: the override sets Values from the shipped snapshots")
    else:
        _values(c, run, env)
    _charges(c, run, env)
    _clock(c, run)
    _time_chunk(c, run, env)
    _day_list(c, run)
    _dafc(c, run, env)
    _unlocks(c, run)
    for inv in ("values", "charges", "clock", "time_chunk", "day_list", "dafc", "unlocks"):
        c.counts[f"items_{inv}"] = sum(1 for r in c.rows["X6"] if r["invariant"] == inv)
    return c.result(out_dir)
