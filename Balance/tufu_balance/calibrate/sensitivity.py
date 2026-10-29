"""Fast-phase targets on a run's per-day records, and the sensitivity probes (design 5.1 T04/T05/T07/T08, 5.2 P01/P04;
unit C5).

The open loop (``replay`` + ``stored``) resets the stats every day, so T05 cannot come from forced-snapshot
differences: the increment of an interval ``[a, b)`` of shipped day snapshots is the sum of the stat rewards the
fast flushes applied on days ``a .. b-1`` (``fasttick_days.jsonl``: ``FastDay.stats``), against ``Value(b) - Value(a)``
of the shipped data. An interval is exact when every one of the 15 stats is within ``max(0.5, 2e-5 |obs|)`` (the
survey's test, ``forensics_killdays.json`` ``rule``).

* :func:`interval_results`, :func:`bad_intervals`: T05 per interval; :func:`explain` tries a first-kill day +-1 of
  one spawner, or of all spawners first cleared on one day (one island's batch).
* :func:`closed_loop_exact_min`: the closed-loop T05 bound from the DPS sensitivity curve (design 5.1: exact >=
  41 - the most bad intervals of any DPS scale within ``[1 - e, 1 + e]``).
* :func:`fit_kappa`, :func:`t04`: active minutes (design 5.3: ``kappa = sum A F / sum F^2`` on the training intervals).
* :func:`unlock_days`, :func:`t07`, :func:`t08`: island unlock days and the hard spawners' first-kill days.

Nothing here simulates the bot: the policy exists only in C# (design 1.5 Python 6).
"""

from __future__ import annotations

import math
import statistics
from dataclasses import dataclass, field
from pathlib import Path
from typing import Iterable, Mapping, Sequence

import yaml

from ..model.enums import StatType
from . import targets as T

TOLERANCES = Path(__file__).with_name("tolerances.yaml")
FAST_FROM_TC = 4
HOLDOUT_TC = 104
STAT_KEYS = tuple(int(StatType[name]) for name in T.STATS)
ABS_TOL, REL_TOL = 0.5, 2e-5
# T08 (design 5.1): (go_name, LocationNumber) -> first-kill day interval (lo, hi], or a single day (d - 1, d]; the
# island-8 elite health only needs >= 27.
T08 = {
    ("Spawner_elite health", 4): (6, 7),
    ("dragon", 4): (7, 8),
    ("Spawner_elite_regen", 5): (9, 10),
    ("Spawner_1 (19)", 5): (9, 10),
    ("ice pike blocker (16)", 10): (37, 41),
    ("ice pike blocker (17)", 10): (37, 41),
    ("ice pike blocker (18)", 10): (37, 41),
    ("ice pike blocker (19)", 10): (37, 41),
    ("Spawner_17_death", 17): (187, 206),
    ("Spawner_1_elite health", 8): (26, math.inf),
}


def tolerance(obs: float) -> float:
    return max(ABS_TOL, REL_TOL * abs(obs))


@dataclass(frozen=True)
class IntervalResult:
    a: int
    b: int
    obs: dict[int, float]  # shipped Value(b) - Value(a) per stat
    pred: dict[int, float]  # sum of the run's flushed rewards over days a .. b-1
    missing_days: tuple[int, ...] = ()  # days of the interval the run has no record of

    @property
    def residual(self) -> dict[int, float]:
        return {k: self.obs[k] - self.pred.get(k, 0.0) for k in self.obs}

    @property
    def failing(self) -> dict[int, float]:
        return {k: r for k, r in self.residual.items() if abs(r) > tolerance(self.obs[k])}

    @property
    def exact(self) -> bool:
        return not self.missing_days and not self.failing

    @property
    def daily(self) -> bool:
        return self.b - self.a == 1

    @property
    def unit(self) -> str:
        return f"{self.a:03d}->{self.b:03d}"


def daily_gains(days: Iterable) -> dict[int, dict[int, float]]:
    """Day -> {StatType: reward applied by that day's flush} (``FastDay`` records)."""
    out: dict[int, dict[int, float]] = {}
    for d in days:
        if d.day in out:
            raise ValueError(f"day {d.day} recorded twice")
        out[d.day] = dict(d.stats)
    return out


def interval_results(shipped_days: Sequence, gains: Mapping[int, Mapping[int, float]], *, first_tc: int = FAST_FROM_TC,
                     stats: Sequence[int] = STAT_KEYS) -> list[IntervalResult]:
    """T05 per interval of consecutive shipped day snapshots from ``first_tc`` (``model.snapshots`` snapshots)."""
    out = []
    snaps = [s for s in shipped_days if s.time_chunk_index >= first_tc]
    for a, b in zip(snaps, snaps[1:]):
        av = {st.key: st.value for st in a.stats}
        bv = {st.key: st.value for st in b.stats}
        days = range(a.time_chunk_index, b.time_chunk_index)
        pred = {k: sum(gains.get(d, {}).get(k, 0.0) for d in days) for k in stats}
        out.append(IntervalResult(a.time_chunk_index, b.time_chunk_index, {k: bv[k] - av[k] for k in stats}, pred,
                                  tuple(d for d in days if d not in gains)))
    return out


def bad_intervals(results: Iterable[IntervalResult]) -> list[IntervalResult]:
    return [r for r in results if not r.exact]


def bad_spans(results: Iterable[IntervalResult]) -> list[list[int]]:
    return [[r.a, r.b] for r in bad_intervals(results)]


@dataclass(frozen=True)
class Explanation:
    day: int  # first-kill day in the run
    shift: int  # +1: the data cleared one day later (one day less), -1: one day earlier (one day more)
    spawners: tuple[str, ...]


def explain(result: IntervalResult, kill_days: Mapping[str, int], daily_reward: Mapping[str, Mapping[int, float]]
            ) -> Explanation | None:
    """A residual explained by a first-kill day +-1 (design 5.1 T05): the residual equals plus one day of reward of a
    spawner (or of all spawners first cleared on the same day) whose first kill one day earlier would fall in the
    interval, or minus one day of a spawner first cleared inside it, on every stat within the interval tolerance.
    ``daily_reward``: spawner -> {StatType: reward of one full day}."""
    residual = result.residual
    by_day: dict[int, list[str]] = {}
    for s, d in kill_days.items():
        by_day.setdefault(d, []).append(s)
    candidates: list[tuple[int, tuple[str, ...]]] = []
    for d in range(result.a, result.b + 1):
        group = tuple(sorted(by_day.get(d, ())))
        candidates += [(d, (s,)) for s in group]
        if len(group) > 1:
            candidates.append((d, group))
    for day, group in candidates:
        gain = {k: sum(daily_reward.get(s, {}).get(k, 0.0) for s in group) for k in residual}
        if not any(gain.values()):
            continue
        for shift, sign, ok_day in ((-1, 1.0, result.a < day <= result.b), (1, -1.0, result.a <= day < result.b)):
            if ok_day and all(abs(residual[k] - sign * gain[k]) <= tolerance(result.obs[k]) for k in residual):
                return Explanation(day, shift, group)
    return None


def closed_loop_exact_min(curve: Mapping[float, int], e: float, intervals: int = 41) -> int:
    """Design 5.1 T05 closed loop: ``intervals - max(bad intervals at a DPS scale in [1 - e, 1 + e])``."""
    inside = [bad for scale, bad in curve.items() if 1.0 - e - 1e-12 <= scale <= 1.0 + e + 1e-12]
    if not inside:
        raise ValueError(f"no DPS scale of the curve lies within [1 - {e}, 1 + {e}]")
    return intervals - max(inside)


# ---------------------------------------------------------------- T04


@dataclass(frozen=True)
class T04Cell:
    a: int
    b: int
    sim: float  # active minutes per day
    ref: float

    @property
    def rel(self) -> float:
        return abs(self.sim / self.ref - 1.0)

    @property
    def group(self) -> str:
        return "tc_4_10" if self.a < 11 else "tc_ge_11"


def t04(shipped_days: Sequence, per_day_minutes: Mapping[int, float], *, first_tc: int = FAST_FROM_TC) -> list[T04Cell]:
    """Active minutes per day of each shipped interval ``[a, b)`` from ``first_tc``: the run's mean of
    ``per_day_minutes`` over the interval against the shipped ``(P(b) - P(a)) / (b - a)``."""
    snaps = [s for s in shipped_days if s.time_chunk_index >= first_tc]
    out = []
    for a, b in zip(snaps, snaps[1:]):
        n = b.time_chunk_index - a.time_chunk_index
        days = range(a.time_chunk_index, b.time_chunk_index)
        if any(d not in per_day_minutes for d in days):
            continue
        out.append(T04Cell(a.time_chunk_index, b.time_chunk_index, sum(per_day_minutes[d] for d in days) / n,
                           (b.playtime - a.playtime) / n))
    return out


def t04_summary(cells: Iterable[T04Cell]) -> dict[str, float]:
    late = [c.rel for c in cells if c.group == "tc_ge_11"]
    return {"median": statistics.median(late), "max": max(late), "n": len(late)}


def fit_kappa(shipped_days: Sequence, farmed_seconds: Mapping[int, float], *, lo: int = 11, hi: int = HOLDOUT_TC) -> float:
    """Design 5.3: ``kappa = sum A F / sum F^2`` over the shipped intervals with ``lo <= a < hi``; A = shipped active
    minutes per day, F = the run's mean ``sum(clears * farmedTime) / 60`` per day."""
    f_min = {d: s / 60.0 for d, s in farmed_seconds.items()}
    cells = [c for c in t04(shipped_days, f_min) if lo <= c.a < hi]
    return sum(c.ref * c.sim for c in cells) / sum(c.sim * c.sim for c in cells)


# ---------------------------------------------------------------- T07, T08


def unlock_days(events: Iterable[Mapping]) -> dict[int, int]:
    """Island -> day of its ``unlock`` event (events.jsonl)."""
    out: dict[int, int] = {}
    for e in events:
        if e["type"] == "unlock":
            if int(e["location"]) in out:
                raise ValueError(f"island {e['location']} unlocked twice")
            out[int(e["location"])] = int(e["day"])
    return out


def t07_ranges(path: Path = TOLERANCES) -> dict[int, tuple[int, int]]:
    cells = yaml.safe_load(path.read_text(encoding="utf-8"))["targets"]["T07"]["cells"]
    out = {int(k): tuple(v) for k, v in cells["island_4"]["ranges"].items()}
    out.update({int(k): tuple(v) for k, v in cells["islands_5_17"]["ranges"].items()})
    return out


def t07(unlocks: Mapping[int, int], ranges: Mapping[int, tuple[int, int]] | None = None, slack: int = 1
        ) -> dict[int, dict]:
    """Island -> {day, range, distance (days outside the range), ok (within ``slack``)}."""
    ranges = t07_ranges() if ranges is None else ranges
    out = {}
    for island, (lo, hi) in sorted(ranges.items()):
        day = unlocks.get(island)
        dist = None if day is None else max(0, lo - day, day - hi)
        out[island] = {"day": day, "range": [lo, hi], "distance": dist, "ok": dist is not None and dist <= slack}
    return out


def t08(first_kills: Mapping[tuple[str, int], int], slack: int = 1) -> dict[str, dict]:
    """``first_kills``: (go_name, island) -> first-kill day of the run. Within ``(lo, hi]`` or at most ``slack`` days
    outside it."""
    out = {}
    for (name, island), (lo, hi) in T08.items():
        day = first_kills.get((name, island))
        dist = None if day is None else max(0, lo + 1 - day, day - hi)
        out[f"{name}@{island}"] = {"day": day, "range": [lo, None if math.isinf(hi) else hi], "distance": dist,
                                   "ok": dist is not None and dist <= slack}
    return out
