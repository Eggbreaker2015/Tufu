"""Simulation snapshots and their lookup/interpolation.

Ports of ``Simulation/SimulationData.cs`` at commit 6af011f:

* :meth:`SimulationData.get_by_playtime` -- lines 13-16: nearest snapshot by
  ``Math.Abs(snapshot.Playtime - playTime)`` in binary32; ``OrderBy`` is stable so a tie
  returns the earlier list entry; no interpolation.
* :meth:`SimulationData.get_by_time_chunk_index` -- lines 23-39: clamp to the first/last
  stored chunk, otherwise linear interpolation between the bracketing stored chunks.
* :func:`interpolate_snapshot` -- lines 41-86: every stat field, DpsData and DefenseData are
  ``a + (b - a) * (double)t``; Location/TimeChunkIndex switch at ``t >= 0.5``; Items from the left.
* :meth:`SimulationData.get_stat_value_at_time_chunk` -- lines 18-21 (returns ``.Value``).

Stage 5 (unit P2) adds the stat-column composition of a snapshot (G11, data-determined):
``SFF = K2(d)``, ``PM = K3(d)``, ``TF = Value * (1 + K2)``, ``TV = TF * K3`` for the direct stats
(Health, HealthRegen, LifestealFlat) and ``TV = TF`` for the other main stats; every other stat has
``SFF = 0``, ``PM = 1``, ``TF = TV = Value``. ``d`` is the snapshot's TimeChunkIndex and K2/K3 are
``GetTotalCoefficient`` types 2/3 under the interpolation rounding (default: ``interpolation_default.json``).

Lookups are cached: ``stat()`` keeps a key index, ``get_by_time_chunk_index`` keeps the sorted list
per source list (checked by element identity, so replacing a snapshot in the list is seen).

Data types: ``SimulationSnapshot.cs`` (Playtime/TotalPlaytime float, Location/TimeChunkIndex int),
``SimulationStatData.cs`` (all double), ``SimulationDpsData.cs`` (WeaponType, double Value).
All three are C# structs, so ``FirstOrDefault`` on a missing key yields Value 0.
"""

from __future__ import annotations

import bisect
import math
import operator
from dataclasses import dataclass, field, replace

import numpy as np

from .enums import StatType
from .f32 import f32
from .registry import Provenance, implements

# SimulationSystem.cs:187-191 (MainInterpolationStats, DirectInterpolationStats).
MAIN_INTERPOLATION_STATS = frozenset({
    StatType.StabDamageFlat, StatType.CutDamageFlat, StatType.BuckshotDamageFlat, StatType.StabDefenceFlat,
    StatType.CutDefenceFlat, StatType.BuckshotDefenceFlat, StatType.Health, StatType.HealthRegen,
    StatType.LifestealFlat})
DIRECT_INTERPOLATION_STATS = frozenset({StatType.Health, StatType.HealthRegen, StatType.LifestealFlat})


@dataclass(frozen=True)
class SimulationStat:
    key: int
    value: float = 0.0
    stats_from_features_flat: float = 0.0
    percent_multiplier: float = 0.0
    total_flat_value: float = 0.0
    total_value: float = 0.0
    current_gpm: float = 0.0


@dataclass(frozen=True)
class SimulationItem:
    key: str
    level: int
    copies: int
    # Not stored in Simulation Data (design 3.6 item 7): None = unknown. Ignored by ==/hash.
    evolved: bool | None = field(default=None, compare=False)


@dataclass
class SimulationSnapshot:
    playtime: np.float32
    total_playtime: np.float32
    location: int
    time_chunk_index: int
    stats: list[SimulationStat] = field(default_factory=list)
    items: list[SimulationItem] = field(default_factory=list)
    dps_data: list[tuple[int, float]] = field(default_factory=list)
    defense_data: list[tuple[int, float]] = field(default_factory=list)
    source_index: int | None = None  # list index in the asset (None for interpolated)

    def stat(self, key: int) -> SimulationStat:
        """``Stats.First(stat => stat.Key == key)`` (throws if missing)."""
        key = int(key)
        stats = self.stats
        cache = self.__dict__.get("_stat_index")
        if cache is None or cache[0] is not stats or cache[1] != len(stats):
            index: dict[int, int] = {}
            for i, st in enumerate(stats):
                index.setdefault(st.key, i)
            cache = (stats, len(stats), index)
            self.__dict__["_stat_index"] = cache
        i = cache[2].get(key)
        if i is not None:
            st = stats[i]
            if st.key == key:
                return st
        for st in stats:  # index stale (element replaced in place) or key missing
            if st.key == key:
                self.__dict__.pop("_stat_index", None)
                return st
        raise KeyError(f"stat {key} missing from snapshot")

    def total(self, key: int) -> float:
        return self.stat(key).total_value

    def dps(self, weapon_type: int, default: float | None = None) -> float:
        """``DpsData.First(...)`` (default None => throw) or ``FirstOrDefault`` (default 0.0)."""
        for wt, v in self.dps_data:
            if wt == int(weapon_type):
                return v
        if default is None:
            raise KeyError(f"DpsData {weapon_type} missing")
        return default

    def defense(self, weapon_type: int) -> float:
        """``DefenseData.First(...)``."""
        for wt, v in self.defense_data:
            if wt == int(weapon_type):
                return v
        raise KeyError(f"DefenseData {weapon_type} missing")


@implements("simulation.snapshot_interpolation_chunk", provenance=Provenance.PORT,
            code_ref="CodeBase/Simulation/SimulationData.cs:41-86")
def interpolate_snapshot(left: SimulationSnapshot, right: SimulationSnapshot, t, time_chunk_index) -> SimulationSnapshot:
    """``SimulationData.InterpolateSnapshot`` (SimulationData.cs:41-86).

    C# shares ``left.Items`` by reference; here the list is copied so that editing the result never
    edits the stored snapshot (the items themselves are immutable).
    """
    t = f32(t)
    td = float(t)

    def lerp(a: float, b: float) -> float:
        return a + (b - a) * td

    stats = []
    for a in left.stats:
        b = right.stat(a.key)
        stats.append(SimulationStat(
            key=a.key,
            value=lerp(a.value, b.value),
            stats_from_features_flat=lerp(a.stats_from_features_flat, b.stats_from_features_flat),
            percent_multiplier=lerp(a.percent_multiplier, b.percent_multiplier),
            total_flat_value=lerp(a.total_flat_value, b.total_flat_value),
            total_value=lerp(a.total_value, b.total_value),
            current_gpm=lerp(a.current_gpm, b.current_gpm),
        ))
    dps = [(wt, lerp(v, right.dps(wt))) for wt, v in left.dps_data]
    defense = [(wt, lerp(v, right.defense(wt))) for wt, v in left.defense_data]
    upper = t >= f32(0.5)
    return SimulationSnapshot(
        playtime=f32(time_chunk_index),
        total_playtime=f32(left.total_playtime + f32(f32(right.total_playtime - left.total_playtime) * t)),
        location=right.location if upper else left.location,
        time_chunk_index=right.time_chunk_index if upper else left.time_chunk_index,
        stats=stats,
        items=list(left.items),
        dps_data=dps,
        defense_data=defense,
        source_index=None,
    )


@dataclass
class SimulationData:
    snapshots: list[SimulationSnapshot]
    time_chunk_snapshots: list[SimulationSnapshot]

    @implements("simulation.snapshot_lookup_playtime", provenance=Provenance.PORT,
                code_ref="CodeBase/Simulation/SimulationData.cs:13-16")
    def get_by_playtime(self, play_time) -> SimulationSnapshot:
        """``SimulationData.GetByPlayTime`` (SimulationData.cs:13-16)."""
        pt = f32(play_time)
        best = None
        best_d = None
        for s in self.snapshots:  # stable: first minimum wins
            d = abs(f32(s.playtime - pt))
            if best is None or d < best_d:
                best, best_d = s, d
        return best

    @implements("simulation.snapshot_interpolation_chunk", provenance=Provenance.PORT,
                code_ref="CodeBase/Simulation/SimulationData.cs:23-39")
    def get_by_time_chunk_index(self, time_chunk_index, snapshots: list[SimulationSnapshot] | None = None) -> SimulationSnapshot:
        """``SimulationData.GetByTimeChunkIndex`` (SimulationData.cs:23-39).

        The C# loop returns the first ``i`` with ``left <= x <= right``; on the sorted list that is
        ``bisect_left(x)`` once ``first < x < last`` (equal TimeChunkIndex runs keep the C# choice).
        """
        x = f32(time_chunk_index)
        src = snapshots if snapshots is not None else self.time_chunk_snapshots
        ordered, keys = self._ordered(src)
        if x <= f32(keys[0]):
            return ordered[0]
        if not (x < f32(keys[-1])):
            return ordered[-1]
        i = bisect.bisect_left(keys, float(x))
        left, right = ordered[i - 1], ordered[i]
        t = f32(f32(x - f32(left.time_chunk_index)) / f32(right.time_chunk_index - left.time_chunk_index))
        return interpolate_snapshot(left, right, t, x)

    def _ordered(self, src: list[SimulationSnapshot]) -> tuple[list[SimulationSnapshot], list[int]]:
        """``OrderBy(TimeChunkIndex)`` (stable), cached per source list and checked by element identity."""
        cache = self.__dict__.setdefault("_ordered_cache", {})
        hit = cache.get(id(src))
        if hit is not None:
            ref, members, ordered, keys = hit
            if ref is src and len(members) == len(src) and all(map(operator.is_, members, src)):
                return ordered, keys
        if not src:
            raise IndexError("no time chunk snapshots")  # C# ordered[0] throws
        ordered = sorted(src, key=lambda s: s.time_chunk_index)
        keys = [s.time_chunk_index for s in ordered]
        if len(cache) > 16:
            cache.clear()
        cache[id(src)] = (src, tuple(src), ordered, keys)
        return ordered, keys

    def get_stat_value_at_time_chunk(self, time_chunk_index, stat_type: int, snapshots=None) -> float:
        """``SimulationData.GetStatValueAtTimeChunk`` (SimulationData.cs:18-21): ``.Value``, not TotalValue."""
        return self.get_by_time_chunk_index(time_chunk_index, snapshots).stat(stat_type).value

    def with_time_chunk_snapshots(self, snaps: list[SimulationSnapshot]) -> "SimulationData":
        return replace(self, time_chunk_snapshots=list(snaps))


# ----------------------------------------------------------------------------- stat composition (G11)
@dataclass(frozen=True)
class CompositionMismatch:
    key: int
    field: str  # stats_from_features_flat | percent_multiplier | total_flat_value | total_value
    stored: float
    expected: float


def composition_coefficients(interpolations, time_chunk_index, rounding=None) -> tuple[float, float]:
    """(K2, K3) at day ``d = (float)TimeChunkIndex`` as doubles widened from binary32."""
    d = f32(time_chunk_index)
    return (float(interpolations.total_coefficient(d, 2, rounding=rounding)),
            float(interpolations.total_coefficient(d, 3, rounding=rounding)))


@implements("simulation.snapshot_stat_composition", provenance=Provenance.DATA,
            code_ref="CodeBase/Simulation/Systems/SimulationSystem.cs:183-191")
def compose_stat(key: int, value: float, k2: float, k3: float, current_gpm: float = 0.0) -> SimulationStat:
    """Stat columns of a snapshot from ``Value`` and K2/K3 (double arithmetic, K widened from float)."""
    key = int(key)
    value = float(value)
    if key in MAIN_INTERPOLATION_STATS:
        k2 = float(k2)
        k3 = float(k3)
        tf = value * (1.0 + k2)
        tv = tf * k3 if key in DIRECT_INTERPOLATION_STATS else tf
        return SimulationStat(key, value, k2, k3, tf, tv, current_gpm)
    return SimulationStat(key, value, 0.0, 1.0, value, value, current_gpm)


def composition_mismatches(snapshot: SimulationSnapshot, interpolations, rounding=None) -> list[CompositionMismatch]:
    """Columns of ``snapshot`` that differ (exact float compare) from :func:`compose_stat` at its day."""
    k2, k3 = composition_coefficients(interpolations, snapshot.time_chunk_index, rounding)
    out = []
    for st in snapshot.stats:
        exp = compose_stat(st.key, st.value, k2, k3, st.current_gpm)
        for name in ("stats_from_features_flat", "percent_multiplier", "total_flat_value", "total_value"):
            a, b = getattr(st, name), getattr(exp, name)
            if not (a == b or (math.isnan(a) and math.isnan(b))):
                out.append(CompositionMismatch(st.key, name, a, b))
    return out


# ----------------------------------------------------------------------------- early-list CurrentGpm (G19)
@implements("simulation.snapshot_current_gpm", provenance=Provenance.DATA)
def early_current_gpm(prev: SimulationSnapshot, cur: SimulationSnapshot, key: int) -> float:
    """Early-list ``CurrentGpm`` = ΔValue / ΔPlaytime against the previous snapshot; NaN when ΔPlaytime = 0.

    Data fact (G19, relative error <= 1e-5): ``MakeSnapshot`` is empty in the recovered code.
    """
    dp = float(cur.playtime) - float(prev.playtime)
    if dp == 0.0:
        return math.nan
    return (cur.stat(key).value - prev.stat(key).value) / dp
