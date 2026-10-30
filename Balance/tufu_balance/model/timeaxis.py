"""Time axes of the simulation (design 4.3).

Only the active minutes ``P`` (``config.CurrentPlaytime``) and the total minutes ``T``
(``config.CurrentTotalPlaytime``, P plus offline minutes) are clock state, both binary32. Everything
else is derived from them or constant:

* day (time chunk) ``d = floor(T / SpawnerChargesChunkMinutes)`` with 1440 minutes per chunk;
* the 30-minute ``TimeChunkDurationMinutes`` only feeds ``GetSpawnerOptimumTimingForCurrentMode``
  (auto-gambling), never the clock;
* the bake day: ``OT`` for day-mode spawners, ``OT / 60`` for minute-mode spawners;
* fight/move/regen times are double seconds, divided by 60 when added to the clock.

Ports at commit 6af011f (``CodeBase/Simulation/Systems/SimulationSystem.cs``):

* :func:`get_balance_timing_minutes` -- lines 515-521, native VA 0x13ebe60 (``Mathf.Floor``, a float);
* :func:`get_spawner_optimum_timing_for_current_mode` -- lines 533-539, VA 0x13ed83c;
* :func:`optimum_time_over_cap` -- the ``IsCapOptimumTime`` filter of ``CalculateSpawnerData``,
  lines 647-651 (VA 0x13e6074);
* :func:`bake_day` -- ``SimulationConfig.cs:118`` (``day = isDays ? t : t / 60f``).

Rules read off the shipped data (empty methods in the recovered code): the trimmed day list
(:func:`trimmed_time_chunk_days`, G19), the offline count of an early snapshot (G14) and the slow-phase
session rule ``period_end = P_offline + 10`` (:func:`check_lifecycle`, G22).
"""

from __future__ import annotations

import math
from dataclasses import dataclass, field
from typing import Iterable, Mapping, Sequence

import numpy as np

from .f32 import f32
from .registry import Provenance, implements

# Shipped Simulation Config values (c_SimulationConfig); TimeAxisConfig carries them per run.
SPAWNER_CHARGES_CHUNK_MINUTES = 1440.0  # minutes per day
TIME_CHUNK_DURATION_MINUTES = 30.0  # GetSpawnerOptimumTimingForCurrentMode only
SECONDS_PER_MINUTE = 60.0
BAKE_MINUTES_PER_DAY = 60.0  # minute-mode bake: 60 active minutes = 1 island-table day
TIME_STEP_SECONDS = 300  # SimulationConfig.TimeStep: cap of one idle step
SNAPSHOT_INTERVAL_SECONDS = 300  # SimulationConfig.SnapshotInterval
TIME_STEP_MIN = TIME_STEP_SECONDS / SECONDS_PER_MINUTE
SNAPSHOT_INTERVAL_MIN = SNAPSHOT_INTERVAL_SECONDS / SECONDS_PER_MINUTE
LIFE_CYCLE_IN_MINUTES = (10.0, 230.0)  # [session length, offline minutes]
SESSION_MINUTES, OFFLINE_MINUTES = LIFE_CYCLE_IN_MINUTES
FAST_TICK_START_CHUNK_OFFSET = 4
FAST_OFFLINE_STEP_MINUTES = 120.0  # data fact: fast-phase offline advances in 120-minute steps
TRIM_GROWTH = f32(1.1)  # GetTrimmedTimeChunkSnapshots: i_{n+1} = max(i_n + 1, ceil(1.1f * i_n))
SESSIONS_PER_DAY = 6  # 6 x (10 active + 230 offline) ~ 1440


@dataclass(frozen=True)
class TimeAxisConfig:
    use_spawner_charges: bool = True
    spawner_charges_chunk_minutes: float = SPAWNER_CHARGES_CHUNK_MINUTES
    time_chunk_duration_minutes: float = TIME_CHUNK_DURATION_MINUTES
    life_cycle_in_minutes: tuple[float, ...] = LIFE_CYCLE_IN_MINUTES
    time_step_seconds: int = TIME_STEP_SECONDS
    snapshot_interval_seconds: int = SNAPSHOT_INTERVAL_SECONDS
    use_fast_tick: bool = True
    fast_tick_start_chunk_offset: int = FAST_TICK_START_CHUNK_OFFSET
    is_cap_optimum_time: bool = True

    @property
    def time_step_minutes(self) -> float:
        return self.time_step_seconds / SECONDS_PER_MINUTE

    @property
    def snapshot_interval_minutes(self) -> float:
        return self.snapshot_interval_seconds / SECONDS_PER_MINUTE

    @property
    def session_minutes(self) -> float:
        """``lifeCyclePeriodEnd`` offset: ``LifeCycleInMinutes[0]``, or 0 when the array is empty."""
        return float(self.life_cycle_in_minutes[0]) if self.life_cycle_in_minutes else 0.0

    @property
    def offline_minutes(self) -> float:
        return float(self.life_cycle_in_minutes[1]) if len(self.life_cycle_in_minutes) > 1 else 0.0


DEFAULT_CONFIG = TimeAxisConfig()


def load_time_axis_config(con) -> TimeAxisConfig:
    """Read the time settings of ``Simulation Config`` from ``balance.sqlite`` (read-only connection)."""
    import json

    r = con.execute("SELECT * FROM c_SimulationConfig").fetchone()
    life = tuple(float(f32(x)) for x in json.loads(r["LifeCycleInMinutes"] or "[]"))
    return TimeAxisConfig(
        use_spawner_charges=bool(int(r["UseSpawnerCharges"])),
        spawner_charges_chunk_minutes=float(f32(r["SpawnerChargesChunkMinutes"])),
        time_chunk_duration_minutes=float(f32(r["TimeChunkDurationMinutes"])),
        life_cycle_in_minutes=life,
        time_step_seconds=int(r["TimeStep"]),
        snapshot_interval_seconds=int(r["SnapshotInterval"]),
        use_fast_tick=bool(int(r["UseFastTick"])),
        fast_tick_start_chunk_offset=int(r["FastTickStartChunkOffset"]),
        is_cap_optimum_time=bool(int(r["IsCapOptimumTime"])),
    )


# ----------------------------------------------------------------------------- ported
@implements("simulation.balance_timing", provenance=Provenance.PORT,
            code_ref="CodeBase/Simulation/Systems/SimulationSystem.cs:515-521", va="0x13ebe60")
def get_balance_timing_minutes(current_playtime, current_total_playtime,
                               cfg: TimeAxisConfig = DEFAULT_CONFIG) -> np.float32:
    """``GetBalanceTimingMinutes``: ``Mathf.Floor(T / chunk)`` (a float, FRINTM) or ``P / 60f``."""
    if cfg.use_spawner_charges:
        return f32(np.floor(f32(f32(current_total_playtime) / f32(cfg.spawner_charges_chunk_minutes))))
    return f32(f32(current_playtime) / f32(60.0))


def time_chunk_index(current_total_playtime, cfg: TimeAxisConfig = DEFAULT_CONFIG) -> int:
    """Day index ``(int)floor(T / SpawnerChargesChunkMinutes)`` as ``UpdateSpawnerChargesChunk`` keeps it (G14)."""
    return int(np.floor(f32(f32(current_total_playtime) / f32(cfg.spawner_charges_chunk_minutes))))


@implements("simulation.spawner_optimum_timing_mode", provenance=Provenance.PORT,
            code_ref="CodeBase/Simulation/Systems/SimulationSystem.cs:533-539", va="0x13ed83c")
def get_spawner_optimum_timing_for_current_mode(optimum_time, is_days: bool,
                                                cfg: TimeAxisConfig = DEFAULT_CONFIG) -> np.float32:
    """``GetSpawnerOptimumTimingForCurrentMode``: minute-mode OT in 30-minute chunks when charges are on."""
    if cfg.use_spawner_charges and not is_days:
        return f32(f32(optimum_time) / f32(cfg.time_chunk_duration_minutes))
    return f32(optimum_time)


@implements("simulation.optimum_time_cap", provenance=Provenance.PORT,
            code_ref="CodeBase/Simulation/Systems/SimulationSystem.cs:647-651", va="0x13e6074")
def optimum_time_over_cap(optimum_time, is_days: bool, current_playtime, day: int,
                          cfg: TimeAxisConfig = DEFAULT_CONFIG) -> bool:
    """``IsCapOptimumTime`` filter: True = the bot skips the spawner (OT > d for day mode, OT > P otherwise)."""
    if not cfg.is_cap_optimum_time:
        return False
    if cfg.use_spawner_charges and is_days:
        return bool(f32(optimum_time) > f32(int(day)))
    return bool(f32(optimum_time) > f32(current_playtime))


@implements("simulation.bake_day", provenance=Provenance.PORT,
            code_ref="CodeBase/Simulation/SimulationConfig.cs:118")
def bake_day(recalc_optimum_time, is_days: bool) -> np.float32:
    """``BuildSpawnerBakeReport``: ``day = isDays ? t : t / 60f``."""
    t = f32(recalc_optimum_time)
    return t if is_days else f32(t / f32(BAKE_MINUTES_PER_DAY))


def seconds_to_minutes(seconds: float) -> float:
    """Double seconds (fight, move, regen) to double minutes; the caller rounds to float when adding to P/T."""
    return float(seconds) / SECONDS_PER_MINUTE


def writes_early_snapshot(day: int, cfg: TimeAxisConfig = DEFAULT_CONFIG) -> bool:
    """Whether a forced day snapshot also goes to the early list: ``!(UseFastTick && d >= offset)`` (design 4.4)."""
    return not (cfg.use_fast_tick and int(day) >= cfg.fast_tick_start_chunk_offset)


# ----------------------------------------------------------------------------- data rules
@implements("simulation.snapshot_trim", provenance=Provenance.RULE,
            code_ref="CodeBase/Simulation/SimulationConfig.cs:98-101")
def trimmed_time_chunk_days(last_day: int, keep_last: bool = False) -> list[int]:
    """Days kept by ``GetTrimmedTimeChunkSnapshots``: 0, then ``max(i + 1, ceil(1.1f * i))`` up to ``last_day``.

    ``keep_last`` appends ``last_day`` when the rule skips it (whether the original does is unknown).
    """
    days = [0]
    while True:
        i = days[-1]
        nxt = max(i + 1, int(math.ceil(float(f32(f32(i) * TRIM_GROWTH)))))
        if nxt > last_day:
            break
        days.append(nxt)
    if keep_last and days[-1] != last_day and last_day > 0:
        days.append(int(last_day))
    return days


def offline_count(playtime, total_playtime, offline_minutes: float = OFFLINE_MINUTES) -> int:
    """Slow-phase offline periods behind a snapshot: ``round((T - P) / 230)`` (G14)."""
    return int(round((float(total_playtime) - float(playtime)) / offline_minutes))


def offline_residual(playtime, total_playtime, offline_minutes: float = OFFLINE_MINUTES) -> float:
    """Distance in minutes of ``T - P`` from the nearest multiple of 230 (float32 noise, <= 0.0032 in the data)."""
    gap = float(total_playtime) - float(playtime)
    return abs(gap - offline_minutes * round(gap / offline_minutes))


@dataclass(frozen=True)
class LifecycleCheck:
    """Result of :func:`check_lifecycle`."""

    rule: str
    rows: int
    feasible_rows: int
    violations: tuple[int, ...]  # row indices
    late: tuple[int, ...]  # an earlier row had passed the next offline's threshold
    early: tuple[int, ...]  # the row's offline happened below its threshold
    chain_feasible: bool  # the offline moments P_k admit a joint assignment
    intervals: Mapping[int, tuple[float, float]] = field(default_factory=dict)  # k -> [lo, hi] of P_k


RULE_FROM_OFFLINE = "period_end = P_offline + session"  # lifeCyclePeriodEnd = P + LifeCycleInMinutes[0]
RULE_FIXED_GRID = "period_end += session"  # ablation: the k-th offline at the first tick with P >= 10k


@implements("simulation.lifecycle_time_model", provenance=Provenance.RULE,
            code_ref="CodeBase/Simulation/Systems/SimulationSystem.cs:264-265,556-570")
def check_lifecycle(rows: Sequence[tuple[float, float]], pins: Mapping[int, float] | None = None,
                    rule: str = RULE_FROM_OFFLINE, session: float = SESSION_MINUTES,
                    offline: float = OFFLINE_MINUTES) -> LifecycleCheck:
    """Check the slow-phase session rule against the early snapshot list (G22).

    ``rows`` are the early snapshots ``(P, T)`` in list order. A snapshot is taken at the end of a tick
    before ``CheckLifeCycle``, so the k-th offline happens at some ``P_k`` between the last row with
    k-1 offlines and the first row with k (``pins`` fix ``P_k`` exactly, e.g. from the forced day
    snapshots). Thresholds: ``P_k >= P_{k-1} + session`` with ``P_0 = 0`` (:data:`RULE_FROM_OFFLINE`) or
    ``P_k >= session * k`` (:data:`RULE_FIXED_GRID`).

    A row with count k is *late* when an earlier row already had ``P >=`` the largest feasible
    threshold of offline k+1 (it should have fired), *early* when its own P is below the smallest
    feasible threshold of offline k.
    """
    pins = dict(pins or {})
    ps = [float(p) for p, _ in rows]
    ks = [offline_count(p, t, offline) for p, t in rows]
    top = max(ks + list(pins))
    last_with: dict[int, float] = {}
    first_with: dict[int, float] = {}
    for p, k in zip(ps, ks):
        last_with[k] = max(last_with.get(k, -math.inf), p)
        first_with[k] = min(first_with.get(k, math.inf), p)
    lo = {0: 0.0}
    hi = {0: 0.0}
    for k in range(1, top + 1):
        a = last_with.get(k - 1, -math.inf)
        b = first_with.get(k, math.inf)
        if k in pins:
            a, b = max(a, pins[k]), min(b, pins[k])
        lo[k], hi[k] = a, b
    for k in range(1, top + 1):  # forward: P_k >= threshold(P_{k-1})
        lo[k] = max(lo[k], lo[k - 1] + session if rule == RULE_FROM_OFFLINE else session * k)
    for k in range(top, 0, -1):  # backward: P_{k-1} <= P_k - session
        if rule == RULE_FROM_OFFLINE:
            hi[k - 1] = min(hi[k - 1], hi[k] - session)
    chain = all(lo[k] <= hi[k] for k in range(top + 1))

    def thr_max(k: int) -> float:
        return hi[k - 1] + session if rule == RULE_FROM_OFFLINE else session * k

    def thr_min(k: int) -> float:
        return lo[k - 1] + session if rule == RULE_FROM_OFFLINE else session * k

    late, early = [], []
    for j, (p, k) in enumerate(zip(ps, ks)):
        if k + 1 <= top and any(thr_max(k + 1) <= q < p for q in ps[:j]):
            late.append(j)
        if k >= 1 and p < thr_min(k):
            early.append(j)
    bad = tuple(sorted(set(late) | set(early)))
    return LifecycleCheck(rule=rule, rows=len(rows), feasible_rows=len(rows) - len(bad), violations=bad,
                          late=tuple(late), early=tuple(early), chain_feasible=chain,
                          intervals={k: (lo[k], hi[k]) for k in range(top + 1)})


def offline_pins(day_snapshots: Iterable[tuple[int, float, float]], cfg: TimeAxisConfig = DEFAULT_CONFIG
                 ) -> dict[int, float]:
    """``{k: P}`` from forced day snapshots ``(d, P, T)`` of the slow phase: the day changes inside the
    k-th offline, and the forced snapshot of day ``d`` is taken in the same tick at ``P = P_k``."""
    out = {}
    for d, p, t in day_snapshots:
        if 1 <= d <= cfg.fast_tick_start_chunk_offset:
            out[offline_count(p, t, cfg.offline_minutes)] = float(p)
    return out
