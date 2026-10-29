"""Kill sequences of the slow phase from the shipped early snapshots (design 4.5 "目标选择", 5.2 P13; unit C7).

The early list (``Simulation Data.Snapshots``, days 0-3) records stat Values and item copies every 1 or 5
active minutes. Between two consecutive snapshots the Value increments are sums of whole clears' rewards
(``Value * Count`` per clear, design 4.5), and item copies grow by one per clear of an item spawner (slow
phase: ``Chain(1) = 1``). :func:`decompose` turns each increment into kill counts per **reward category**
(spawners with the same rewards cannot be told apart) with a small integer program: exact where possible,
otherwise the smallest residual in units of one clear, then the fewest kills. Only spawners the bot filter
could admit count (island open at either end, ``OptimumTime <= P`` at the end, no event/VIP/dummy), each at
most once per cooldown and per its daily charges within the interval.

:func:`clear_times` compares the active seconds of each interval with the clear time of the time models of
design 4.5, evaluated per kill on the state of the interval's first snapshot (``simcalc.spawner_data``):
``farmedTime`` (``TimeModel.FarmedTime``) and its parts (fight, the constant ``70/speed`` move, the regen wait).
Per category it gives the mean active seconds per clear next to the mean ``farmedTime`` (the per-spawner
comparison of design 7.3 C7); :func:`fit_parts` fits ``dP = a*fight + b*move + c*regen`` (non-negative) over
the intervals with kills. Intervals that end exactly on the snapshot cadence end with an idle step, so their
time is an upper bound; the per-category means use the others when they have any.

Diagnostics for K2 (closed-loop targets T03/T16 and the ``TimeModel`` choice); the golden suite ``sim_slow``
keeps the residuals and the per-category comparison as a ratchet (``tests/test_killseq.py``).
"""

from __future__ import annotations

import math
from collections import defaultdict
from dataclasses import dataclass, field
from typing import Mapping, Sequence

import numpy as np

from ..model import simcalc as sc, spawners as sp
from ..model.snapshots import SimulationSnapshot

FAST_FROM_DAY = 4  # the early list ends with the slow phase (FastTickStartChunkOffset)
CADENCE_MINUTES = (1.0, 5.0)  # P06: idle steps end exactly on these
RESIDUAL_WEIGHT = 1000.0  # one clear of residual outweighs any number of kills


@dataclass(frozen=True)
class Category:
    """Spawners whose clears change the snapshot the same way: stat rewards ``Value * Count`` and the item."""

    key: str
    rewards: tuple[tuple[int, float], ...]  # (StatType, Value * Count), sorted
    item: str | None
    members: tuple[sp.SpawnerId, ...]


@dataclass
class Interval:
    index: int  # snapshots[index - 1] -> snapshots[index]
    day: int
    location: int
    p0: float
    p1: float
    t0: float
    t1: float
    delta: dict[int, float]  # stat Value increments
    item_delta: dict[str, int]  # item copy increments (negative ones dropped)
    kills: dict[str, int] = field(default_factory=dict)  # category key -> clears
    residual: dict[str, float] = field(default_factory=dict)  # "stat:<id>" / "item:<key>" -> unexplained, in clears
    exact: bool = True
    idle_end: bool = False  # the interval ends on the cadence: its last step was idle
    farmed: float = 0.0  # seconds, sum over kills of the category's mean farmedTime
    fight: float = 0.0
    move: float = 0.0
    regen: float = 0.0

    @property
    def active_seconds(self) -> float:
        return (self.p1 - self.p0) * 60.0

    @property
    def total_kills(self) -> int:
        return sum(self.kills.values())


def _category_key(rewards: Sequence[tuple[int, float]], item: str | None) -> str:
    parts = [f"{t}={v:g}" for t, v in rewards]
    return ",".join(parts) + (f"|item={item}" if item else "")


def _bot_candidate(s: sc.SimSpawner) -> bool:
    d = s.d
    return not (d.is_event or d.is_vip or d.is_dummy) and (bool(s.stat_rewards) or s.item_key is not None)


def categories(ctx: sc.SimCalcContext, members: Sequence[sc.SimSpawner]) -> dict[str, Category]:
    groups: dict[str, list] = defaultdict(list)
    sig: dict[str, tuple] = {}
    for s in members:
        totals: dict[int, float] = defaultdict(float)
        for r in s.stat_rewards:
            totals[r.stat_type] += float(r.value) * s.d.count
        rewards = tuple(sorted((t, v) for t, v in totals.items() if v != 0.0))
        key = _category_key(rewards, s.item_key)
        groups[key].append(s.spawner_id)
        sig[key] = (rewards, s.item_key)
    return {k: Category(k, sig[k][0], sig[k][1], tuple(v)) for k, v in sorted(groups.items())}


def _max_clears(ctx: sc.SimCalcContext, s: sc.SimSpawner, seconds: float) -> int:
    """Clears of one spawner possible in `seconds` of total time: one now, one per cooldown after, at most a
    day's charges (``ceil(limit / Count)``)."""
    cooldown = float(sp.base_cooldown(s.d, ctx.cooldowns, ctx.meta))
    by_cooldown = 1 + (int(seconds // cooldown) if cooldown > 0 else 10**6)
    limit = ctx.charges_limit(s)
    per_day = max(1, -(-limit // max(1, s.d.count))) if limit > 0 else 1
    if s.d.dafc:
        per_day = 1
    days = 1 + int(seconds // (ctx.time_axis.spawner_charges_chunk_minutes * 60.0))
    return min(by_cooldown, per_day * days)


def _solve(delta: Mapping[int, float], item_delta: Mapping[str, int], cats: Sequence[Category],
           bounds: Sequence[int]) -> tuple[list[int], dict[str, float], bool]:
    """Integer kill counts per category (see the module docstring); residual in clears of the smallest reward."""
    from scipy.optimize import Bounds, LinearConstraint, milp

    stats = sorted(set(delta) | {t for c in cats for t, _ in c.rewards})
    items = sorted(set(item_delta) | {c.item for c in cats if c.item})
    rows = [("stat", t) for t in stats] + [("item", k) for k in items]
    if not cats:
        res = {f"{kind}:{k}": float(delta.get(k, 0.0) if kind == "stat" else item_delta.get(k, 0)) for kind, k in rows}
        res = {k: v for k, v in res.items() if v}
        return [], res, not res
    unit = {}
    for kind, k in rows:
        vals = [abs(v) for c in cats for t, v in c.rewards if t == k] if kind == "stat" else [1.0]
        unit[(kind, k)] = min(vals) if vals else 1.0
    n, m = len(cats), len(rows)
    # x = [n_c (n), s_plus (m), s_minus (m)]
    a = np.zeros((m, n + 2 * m))
    b = np.zeros(m)
    for i, (kind, k) in enumerate(rows):
        for j, c in enumerate(cats):
            if kind == "stat":
                a[i, j] = sum(v for t, v in c.rewards if t == k) / unit[(kind, k)]
            else:
                a[i, j] = 1.0 if c.item == k else 0.0
        a[i, n + i] = 1.0
        a[i, n + m + i] = -1.0
        b[i] = (delta.get(k, 0.0) if kind == "stat" else item_delta.get(k, 0)) / unit[(kind, k)]
    cost = np.concatenate([1.0 + 1e-6 * np.arange(n), np.full(2 * m, RESIDUAL_WEIGHT)])
    integrality = np.concatenate([np.ones(n), np.zeros(2 * m)])
    lb = np.zeros(n + 2 * m)
    ub = np.concatenate([np.asarray(bounds, dtype=float), np.full(2 * m, np.inf)])
    out = milp(cost, integrality=integrality, bounds=Bounds(lb, ub), constraints=LinearConstraint(a, b, b))
    if not out.success:
        raise RuntimeError(f"killseq: integer program failed: {out.message}")
    x = out.x
    kills = [int(round(v)) for v in x[:n]]
    residual = {}
    for i, (kind, k) in enumerate(rows):
        r = float(x[n + i] - x[n + m + i])
        if abs(r) > 1e-6:
            residual[f"{kind}:{k}"] = round(r, 6)
    return kills, residual, not residual


def eligible(ctx: sc.SimCalcContext, s0: SimulationSnapshot, s1: SimulationSnapshot) -> list[sc.SimSpawner]:
    """Spawners the bot filter could admit at some point of the interval (islands 1-3 are minute-capped)."""
    location = max(int(s0.location), int(s1.location))
    p1 = float(s1.playtime)
    out = []
    for s in ctx.spawners:
        d = s.d
        if not _bot_candidate(s) or d.location > location or d.is_days:
            continue
        if d.optimum_time > p1:
            continue
        out.append(s)
    return out


def decompose(ctx: sc.SimCalcContext, early: Sequence[SimulationSnapshot]) -> list[Interval]:
    """One :class:`Interval` per consecutive pair of early snapshots of the slow phase with any change in P."""
    out = []
    for i in range(1, len(early)):
        s0, s1 = early[i - 1], early[i]
        if int(s1.time_chunk_index) >= FAST_FROM_DAY:
            break
        p0, p1 = float(s0.playtime), float(s1.playtime)
        if p1 == p0:
            continue  # the early copy of a forced day snapshot
        v0 = {st.key: st.value for st in s0.stats}
        delta = {st.key: st.value - v0.get(st.key, 0.0) for st in s1.stats}
        delta = {k: round(v, 6) for k, v in delta.items() if abs(v) > 1e-9}
        c0 = {it.key: it.copies for it in s0.items}
        item_delta = {it.key: it.copies - c0.get(it.key, 0) for it in s1.items}
        item_delta = {k: v for k, v in item_delta.items() if v > 0}
        iv = Interval(i, int(s0.time_chunk_index), max(int(s0.location), int(s1.location)), p0, p1,
                      float(s0.total_playtime), float(s1.total_playtime), delta, item_delta)
        dp = p1 - p0
        iv.idle_end = any(abs(dp - c) < 1e-4 for c in CADENCE_MINUTES)
        members = eligible(ctx, s0, s1)
        cats = list(categories(ctx, members).values())
        by_id = {s.spawner_id: s for s in members}
        seconds = (iv.t1 - iv.t0) * 60.0
        bounds = [sum(_max_clears(ctx, by_id[sid], seconds) for sid in c.members) for c in cats]
        kills, residual, exact = _solve(delta, item_delta, cats, bounds)
        iv.kills = {c.key: k for c, k in zip(cats, kills) if k}
        iv.residual, iv.exact = residual, exact
        out.append(iv)
    return out


@dataclass
class CategoryTime:
    key: str
    members: tuple[sp.SpawnerId, ...]
    kills: int
    intervals: int
    clear_seconds: float  # mean active seconds per clear (intervals without a final idle step when there are any)
    farmed_seconds: float  # mean farmedTime per clear
    fight_seconds: float
    move_seconds: float
    regen_seconds: float
    upper_bound: bool  # only intervals ending with an idle step: clear_seconds is an upper bound

    @property
    def ratio(self) -> float:
        return self.clear_seconds / self.farmed_seconds if self.farmed_seconds > 0 else math.nan


def _state(ctx: sc.SimCalcContext, snapshot: SimulationSnapshot) -> sc.PlayerState:
    # Inside a day the modifiers carry K2/K3(d): no lag for early snapshots.
    return sc.snapshot_state(snapshot, ctx, order=sc.MODIFIER_FIRST)


def clear_times(ctx: sc.SimCalcContext, early: Sequence[SimulationSnapshot], intervals: Sequence[Interval]
                ) -> dict[str, CategoryTime]:
    """Fills each interval's model sums (farmed, fight, move, regen) and returns the per-category comparison."""
    by_id = {s.spawner_id: s for s in ctx.spawners}
    acc: dict[str, dict] = {}
    for iv in intervals:
        if not iv.kills:
            continue
        state = _state(ctx, early[iv.index - 1])
        caches = sc.combat_caches(ctx, state)
        k_total = iv.total_kills
        per_kill = iv.active_seconds / k_total
        cats = categories(ctx, [by_id[sid] for sid in _members_of(ctx, early, iv)])
        for key, n in iv.kills.items():
            c = cats[key]
            datas = [sc.spawner_data(ctx, state, by_id[sid], caches) for sid in c.members]
            mean = lambda f: sum(f(d) for d in datas) / len(datas)  # noqa: E731
            farmed, fight = mean(lambda d: d.farmed_time), mean(lambda d: d.fight_time)
            move, regen = mean(lambda d: d.av_move_time), mean(lambda d: d.av_regen_time)
            iv.farmed += n * farmed
            iv.fight += n * fight
            iv.move += n * move
            iv.regen += n * regen
            a = acc.setdefault(key, {"members": c.members, "kills": 0, "intervals": 0, "farmed": 0.0, "fight": 0.0,
                                     "move": 0.0, "regen": 0.0, "time": [0.0, 0], "time_idle": [0.0, 0]})
            a["kills"] += n
            a["intervals"] += 1
            for name, v in (("farmed", farmed), ("fight", fight), ("move", move), ("regen", regen)):
                a[name] += n * v
            slot = a["time_idle"] if iv.idle_end else a["time"]
            slot[0] += n * per_kill
            slot[1] += n
    out = {}
    for key, a in sorted(acc.items()):
        k = a["kills"]
        timed = a["time"] if a["time"][1] else a["time_idle"]
        out[key] = CategoryTime(key, a["members"], k, a["intervals"], timed[0] / timed[1], a["farmed"] / k,
                                a["fight"] / k, a["move"] / k, a["regen"] / k, upper_bound=not a["time"][1])
    return out


def _members_of(ctx: sc.SimCalcContext, early: Sequence[SimulationSnapshot], iv: Interval) -> list[sp.SpawnerId]:
    return [s.spawner_id for s in eligible(ctx, early[iv.index - 1], early[iv.index])]


@dataclass(frozen=True)
class PartsFit:
    fight: float
    move: float
    regen: float
    rms_seconds: float
    intervals: int
    kappa: float  # sum of active seconds over sum of farmedTime


def fit_parts(intervals: Sequence[Interval], *, include_idle: bool = False) -> PartsFit:
    """Non-negative least squares ``active seconds = a*fight + b*move + c*regen`` over intervals with kills
    (by default only those whose last step was a fight); ``kappa`` is the one-parameter ratio to farmedTime."""
    from scipy.optimize import nnls

    rows = [iv for iv in intervals if iv.kills and iv.exact and (include_idle or not iv.idle_end)]
    if not rows:
        return PartsFit(math.nan, math.nan, math.nan, math.nan, 0, math.nan)
    a = np.array([[iv.fight, iv.move, iv.regen] for iv in rows])
    y = np.array([iv.active_seconds for iv in rows])
    coef, _ = nnls(a, y)
    rms = float(np.sqrt(np.mean((a @ coef - y) ** 2)))
    kappa = float(y.sum() / sum(iv.farmed for iv in rows))
    return PartsFit(float(coef[0]), float(coef[1]), float(coef[2]), rms, len(rows), kappa)


def analyse(con) -> tuple[list[Interval], dict[str, CategoryTime], PartsFit]:
    """The shipped data's decomposition, per-category clear times and the parts fit."""
    from ..model.data import load_simulation_data

    ctx = sc.load_simcalc_context(con)
    early = load_simulation_data(con).snapshots
    intervals = decompose(ctx, early)
    times = clear_times(ctx, early, intervals)
    return intervals, times, fit_parts(intervals)


def main(argv: Sequence[str] | None = None) -> int:
    import argparse
    import os

    from ..model.data import connect

    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--db", default=os.environ.get("TUFU_BALANCE_DB"))
    args = ap.parse_args(argv)
    intervals, times, fit = analyse(connect(args.db))
    for iv in intervals:
        res = "" if iv.exact else f" residual {iv.residual}"
        print(f"{iv.index:2d} d{iv.day} L{iv.location} P {iv.p0:8.3f}->{iv.p1:8.3f} ({iv.active_seconds:6.1f} s"
              f"{' idle' if iv.idle_end else ''}) kills {iv.total_kills:2d} farmed {iv.farmed:7.1f} s"
              f" fight {iv.fight:6.1f} s {dict(sorted(iv.kills.items()))}{res}")
    print("category: kills, mean clear s, mean farmedTime s, ratio")
    for key, t in times.items():
        print(f"  {key:40s} {t.kills:3d} {t.clear_seconds:8.1f}{'+' if t.upper_bound else ' '} {t.farmed_seconds:8.1f} "
              f"{t.ratio:6.3f}")
    print(f"fit over {fit.intervals} intervals: dP = {fit.fight:.4f}*fight + {fit.move:.4f}*move + {fit.regen:.4f}*regen"
          f" (rms {fit.rms_seconds:.1f} s); kappa {fit.kappa:.4f}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
