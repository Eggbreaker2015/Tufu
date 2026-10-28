"""Island timeline: the island table and everything placed on the island axis.

Generation rules worked back from the shipped data in stage 4 (``Docs/Balance/Generation/IslandTimeline.md``,
``Balance/analysis/island_timeline.py``); the editor methods that produced the data are empty in the
reconstruction. One runtime formula, ``ConvertIslandTimingToDays``, is a port of native code.

* ``CombatBalanceConfig.IslandTargetDays`` (``ITD``): ``ITD[i-1]`` is the day island ``i`` ends
  (= island ``i+1`` starts). Islands 1..16 are hand values equal to the gate spawners' OptimumTime
  (minute gates on islands 1..3 at 60 active minutes per day); from island 16 on the table follows
  ``D(i) = (5/6)(i+1)(i+2) - 105`` (G01, G02).
* Spawner OptimumTime from RelativeOptimumTime within the island window
  (``SpawnerRelativeOptimumTimeTool.ApplyOptimumTimesFromRelative``, empty): 701/717 (G03).
* Evolution island rows and ``*EvolutionTargetTimings`` (G04), interpolation ``TimingPoints``
  values and strings (G05), amulet ``LevelTargetDays`` (G06), all through
  :func:`convert_island_timing_to_days`.

Float arithmetic follows the C# ``float`` declarations step by step (design 1.5, Python rule 3).
Pure functions; inputs are plain values (the tests load them from ``balance.sqlite``).
"""

from __future__ import annotations

import math
from fractions import Fraction
from typing import Iterable, Sequence

import numpy as np

from .f32 import f32, fadd, fdiv, fmul, fsub, round_half_even
from .registry import Provenance, implements

# Island 16 is the last gated island and the first one on the closed-form tail (D(15) = 121.67 != 130).
TAIL_FIRST_ISLAND = 16
ISLAND_COUNT = 99
# Minute islands 1..3: SpawnerRelativeOptimumTimeTool.Islands[L].MinOptimumTime is a hand value
# (0, 35, 120 minutes), not 60 * ITD[L-2] (0, 30, 110); MaxOptimumTime = 60 * ITD[L-1].
MINUTE_ISLANDS = (1, 2, 3)
MINUTE_WINDOW_MIN = {1: 0.0, 2: 35.0, 3: 120.0}
MINUTES_PER_DESIGN_DAY = 60.0
EVOLUTION_LEVELS = 15  # CombatBalanceConfig.EvolutionTimingLevelsCount
# AmuletConfig.LevelTargetDays = start days of these islands (no rule found for the choice).
AMULET_TARGET_ISLANDS = (8, 9, 10, 12, 14, 16, 18)

ROUNDINGS = ("half_even", "half_up")
PRECISIONS = ("f32", "f64")


def _f32_from_fraction(x: Fraction) -> np.float32:
    """Correctly rounded binary32 of a rational (ties to even), without double rounding."""
    c = np.float32(float(x))
    best, best_err = c, abs(Fraction(float(c)) - x)
    for n in (np.nextafter(c, np.float32(-np.inf)), np.nextafter(c, np.float32(np.inf))):
        err = abs(Fraction(float(n)) - x)
        if err < best_err or (err == best_err and int(n.view(np.uint32)) % 2 == 0):
            best, best_err = n, err
    return np.float32(best)


def _check_island(itd: Sequence, island: int, *, allow_end: bool = False) -> int:
    if isinstance(island, bool) or not isinstance(island, (int, np.integer)):
        raise ValueError(f"island number must be an int, got {island!r}")
    hi = len(itd) + (1 if allow_end else 0)
    if not 1 <= island <= hi:
        raise ValueError(f"island {island} out of range 1..{hi} (IslandTargetDays has {len(itd)} entries)")
    return int(island)


# ---------------------------------------------------------------------------------------------
# Island table


@implements("balance.island_target_days_curve", provenance=Provenance.RULE)
def island_target_days_closed_form(island: int) -> np.float32:
    """Tail of ``IslandTargetDays``: ``D(i) = (5/6)(i+1)(i+2) - 105`` as float32.

    Exact (float32) on islands 16..99 of the shipped table (G01: 84/84); increment ``(5/3)(i+1)``
    days = ``100(i+1)`` active minutes. Not valid before island 16.
    """
    if isinstance(island, bool) or not isinstance(island, (int, np.integer)) or island < 1:
        raise ValueError(f"island number must be an int >= 1, got {island!r}")
    return _f32_from_fraction(Fraction(5, 6) * (island + 1) * (island + 2) - 105)


@implements("balance.island_target_days_curve", provenance=Provenance.RULE)
def generate_island_target_days(head: Sequence, count: int = ISLAND_COUNT) -> list[np.float32]:
    """``IslandTargetDays``: the hand values ``head`` (islands 1..len(head)), then the closed-form tail.

    The shipped table is ``head`` = the first 15 (or 16) stored values; island 16 is on both.
    """
    if count < len(head):
        raise ValueError(f"count {count} < {len(head)} hand values")
    return [f32(v) for v in head] + [island_target_days_closed_form(i) for i in range(len(head) + 1, count + 1)]


@implements("balance.island_target_days_curve", provenance=Provenance.DATA)
def island_target_days_from_gates(gates: Iterable[tuple[int, float, bool]]) -> dict[int, np.float32]:
    """``FillIslandTargetDaysByGateSpawners`` (empty): ``ITD[L-1]`` = largest gate OptimumTime on island L.

    ``gates``: ``(LocationNumber, OptimumTime, IsDays)`` of every spawner with ``Gates`` on it.
    Minute gates (islands 1..3) count 60 active minutes per day. Identity on islands 1..16
    (G02: 16/16, every gate's RelativeOptimumTime is 1); the direction (gates -> table) is a guess
    from the method name.
    """
    out: dict[int, np.float32] = {}
    for location, ot, is_days in gates:
        days = f32(ot) if is_days else fdiv(ot, MINUTES_PER_DESIGN_DAY)
        if location not in out or days > out[location]:
            out[int(location)] = days
    return dict(sorted(out.items()))


@implements("balance.island_timing_to_days", provenance=Provenance.DATA)
def island_start_day(itd: Sequence, island: int) -> np.float32:
    """Day island ``island`` starts: 0 for island 1, else ``ITD[island-2]``; ``len(itd)+1`` is allowed
    (the end of the last island). Equals ``ConvertIslandTimingToDays(island)``."""
    island = _check_island(itd, island, allow_end=True)
    return f32(0.0) if island == 1 else f32(itd[island - 2])


@implements("balance.island_timing_to_days", provenance=Provenance.PORT,
            code_ref="CodeBase/Simulation/Forecast/ForecastSnapshots.cs:69", va="0x157ad44")
def convert_island_timing_to_days(island_timing: float, itd: Sequence) -> np.float32:
    """``ConvertIslandTimingToDays``: fractional island number -> days on the ITD axis (float32).

    ``n = FloorToInt(s); prev = n == 1 ? 0 : ITD[n-2]; f = s - n;
    f == 0 ? prev : prev + f * (ITD[n-1] - prev)``.

    Raises ``ValueError`` where the C# indexes out of range: ``n < 1``, ``n > len(itd)``, and NaN/inf.
    ``s == len(itd) + 1`` exactly (end of the last island) is accepted: the C# returns ``ITD[-1]``
    there without reading ``ITD[n-1]``.
    """
    s = f32(island_timing)
    if not np.isfinite(s):
        raise ValueError(f"island timing must be finite, got {island_timing!r}")
    n = int(math.floor(float(s)))
    frac = fsub(s, f32(n))
    if n < 1 or n > len(itd) + 1 or (n == len(itd) + 1 and frac != 0):
        raise ValueError(f"island timing {float(s)!r} out of range [1, {len(itd) + 1}] "
                         f"(island {n}; IslandTargetDays has {len(itd)} entries)")
    prev = f32(0.0) if n == 1 else f32(itd[n - 2])
    if frac == 0:
        return prev
    return fadd(prev, fmul(frac, fsub(itd[n - 1], prev)))


@implements("pending:timeline.days_to_island_timing", provenance=Provenance.INFERRED)
def days_to_island_timing(day: float, itd: Sequence) -> float:
    """Inverse of :func:`convert_island_timing_to_days` (double): island ``k`` covers
    ``[start(k), ITD[k-1])``; days ``<= 0`` map to 1, days past the table to ``len(itd) + 1``."""
    day = float(day)
    if math.isnan(day):
        raise ValueError("day must not be NaN")
    if day <= 0:
        return 1.0
    lo = 0.0
    for k, end in enumerate(itd, start=1):
        end = float(end)
        if day < end:
            return k + (day - lo) / (end - lo)
        lo = end
    return float(len(itd) + 1)


# ---------------------------------------------------------------------------------------------
# Spawner OptimumTime within the island window (SpawnerRelativeOptimumTimeTool)


@implements("spawners.relative_optimum_time", provenance=Provenance.DATA)
def relative_window(itd: Sequence, island: int) -> tuple[np.float32, np.float32, bool]:
    """``SpawnerRelativeOptimumTimeTool.Islands[L]`` ``(Min, Max, IsDays)`` from the island table.

    Islands 4+: ``[ITD[L-2], ITD[L-1]]`` days (14/14 on the scene). Islands 1..3: minutes,
    ``Max = 60 * ITD[L-1]`` and ``Min`` = the hand values 0, 35, 120 (3/3).
    """
    island = _check_island(itd, island)
    if island in MINUTE_ISLANDS:
        return f32(MINUTE_WINDOW_MIN[island]), fmul(MINUTES_PER_DESIGN_DAY, itd[island - 1]), False
    return island_start_day(itd, island), f32(itd[island - 1]), True


@implements("spawners.relative_optimum_time", provenance=Provenance.FIT)
def optimum_time_from_relative(rel: float, window_min: float, window_max: float, is_days: bool, *,
                               rounding: str = "half_even", precision: str = "f32") -> np.float32:
    """``ApplyOptimumTimesFromRelative`` (empty): ``OT = Round((Min + rel*(Max-Min)) * k) / k``.

    ``k`` = 10 for day spawners (0.1 day), 1 for minute spawners; the step follows the spawner's
    ``IsDays``, the window the tool's island range. Default: float32 and ``Mathf.Round`` (ties to
    even), 701/717 (G03). Ablations: ties half up (the ``rounding`` argument) 682/717; ``precision="f64"`` (double
    arithmetic on the float fields) 676/717 with either tie rule, so the data favours float32.
    """
    if rounding not in ROUNDINGS:
        raise ValueError(f"rounding must be one of {ROUNDINGS}, got {rounding!r}")
    if precision not in PRECISIONS:
        raise ValueError(f"precision must be one of {PRECISIONS}, got {precision!r}")
    k = 10.0 if is_days else 1.0
    if precision == "f32":
        x = fmul(fadd(window_min, fmul(rel, fsub(window_max, window_min))), k)
    else:
        lo = float(f32(window_min))
        x = (lo + float(f32(rel)) * (float(f32(window_max)) - lo)) * k
    x = float(x)
    r = round_half_even(x) if rounding == "half_even" else math.floor(x + 0.5)
    return fdiv(r, k) if precision == "f32" else f32(r / k)


@implements("spawners.relative_optimum_time", provenance=Provenance.FIT)
def relative_from_optimum_time(ot: float, window_min: float, window_max: float) -> np.float32:
    """``FillRelativeOptimumTimes`` (empty): ``rel = Round((OT - Min) / (Max - Min) * 100) / 100``.

    The reverse rule covers fewer spawners (656/717) than :func:`optimum_time_from_relative`,
    which is why rel is taken to be the input.
    """
    return fdiv(round_half_even(float(fmul(fdiv(fsub(ot, window_min), fsub(window_max, window_min)), 100.0))),
                100.0)


# ---------------------------------------------------------------------------------------------
# Evolution schedule


@implements("balance.evolution_island_fill_rule", provenance=Provenance.RULE)
def evolution_islands(start: float, step: float, levels: int = EVOLUTION_LEVELS) -> list[np.float32]:
    """``FillIslandTimingsByStartStep`` (empty): ``E_k = Start + (k-1) * Step``, k = 1..15 (G04: 150/150)."""
    return [fadd(start, fmul(k - 1, step)) for k in range(1, levels + 1)]


@implements("balance.evolution_target_timings_from_islands", provenance=Provenance.RULE)
def evolution_target_timings(islands: Iterable[float], itd: Sequence) -> list[np.float32]:
    """``ApplyIslandTimingsToDayTimings`` (empty): ``<Rarity>[Armor]EvolutionTargetTimings[k-1] =
    ConvertIslandTimingToDays(E_k)`` (G04: float32-exact 150/150)."""
    return [convert_island_timing_to_days(s, itd) for s in islands]


# ---------------------------------------------------------------------------------------------
# Other configs on the island axis


@implements("balance.interpolation_points_from_islands", provenance=Provenance.RULE)
def interpolation_timing_points(island_points: Iterable[float], itd: Sequence) -> list[np.float32]:
    """``BalanceInterpolation.ApplyIslandTimings`` (empty): ``TimingPoints_j =
    ConvertIslandTimingToDays(IslandPoints_j)``. The stored strings hold 7 significant digits, so
    4 of 49 knots differ from the float32 result by up to 3.05e-5 (G05)."""
    return [convert_island_timing_to_days(s, itd) for s in island_points]


@implements("balance.interpolation_points_from_islands", provenance=Provenance.RULE)
def format_points(points: Iterable[float]) -> str:
    """``BalanceInterpolation.FormatPoint`` (empty) joined with ``"; "``: 7 significant digits, which
    reproduces all 19 shipped ``TimingPoints`` strings from :func:`interpolation_timing_points`.
    Python ``%.7g``; it would differ from .NET ``"G7"`` only in exponent notation (|x| < 1e-4 or >= 1e7)."""
    return "; ".join(f"{float(p):.7g}" for p in points)


@implements("powerups.amulet_level_target_days", provenance=Provenance.DATA)
def amulet_level_target_days(itd: Sequence, islands: Sequence[int] = AMULET_TARGET_ISLANDS) -> list[np.float32]:
    """``AmuletConfig.LevelTargetDays`` = start days of islands 8, 9, 10, 12, 14, 16, 18 (G06: 7/7)."""
    return [island_start_day(itd, island) for island in islands]
