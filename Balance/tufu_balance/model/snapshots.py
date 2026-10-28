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

Data types: ``SimulationSnapshot.cs`` (Playtime/TotalPlaytime float, Location/TimeChunkIndex int),
``SimulationStatData.cs`` (all double), ``SimulationDpsData.cs`` (WeaponType, double Value).
All three are C# structs, so ``FirstOrDefault`` on a missing key yields Value 0.
"""

from __future__ import annotations

from dataclasses import dataclass, field, replace

import numpy as np

from .f32 import f32


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
        for s in self.stats:
            if s.key == int(key):
                return s
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


def interpolate_snapshot(left: SimulationSnapshot, right: SimulationSnapshot, t, time_chunk_index) -> SimulationSnapshot:
    """``SimulationData.InterpolateSnapshot`` (SimulationData.cs:41-86)."""
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
        items=left.items,
        dps_data=dps,
        defense_data=defense,
        source_index=None,
    )


@dataclass
class SimulationData:
    snapshots: list[SimulationSnapshot]
    time_chunk_snapshots: list[SimulationSnapshot]

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

    def get_by_time_chunk_index(self, time_chunk_index, snapshots: list[SimulationSnapshot] | None = None) -> SimulationSnapshot:
        """``SimulationData.GetByTimeChunkIndex`` (SimulationData.cs:23-39)."""
        x = f32(time_chunk_index)
        src = snapshots if snapshots is not None else self.time_chunk_snapshots
        ordered = sorted(src, key=lambda s: s.time_chunk_index)  # stable like OrderBy
        if x <= f32(ordered[0].time_chunk_index):
            return ordered[0]
        if not (x < f32(ordered[-1].time_chunk_index)):
            return ordered[-1]
        for i in range(1, len(ordered)):
            left, right = ordered[i - 1], ordered[i]
            if x < f32(left.time_chunk_index) or x > f32(right.time_chunk_index):
                continue
            t = f32(f32(x - f32(left.time_chunk_index)) / f32(right.time_chunk_index - left.time_chunk_index))
            return interpolate_snapshot(left, right, t, x)
        return ordered[-1]

    def get_stat_value_at_time_chunk(self, time_chunk_index, stat_type: int, snapshots=None) -> float:
        """``SimulationData.GetStatValueAtTimeChunk`` (SimulationData.cs:18-21): ``.Value``, not TotalValue."""
        return self.get_by_time_chunk_index(time_chunk_index, snapshots).stat(stat_type).value

    def with_time_chunk_snapshots(self, snaps: list[SimulationSnapshot]) -> "SimulationData":
        return replace(self, time_chunk_snapshots=list(snaps))
