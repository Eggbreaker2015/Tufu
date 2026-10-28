"""Snapshot lookups, caching and aliasing fixes (design 3.6 item 2), stat composition, G19 CurrentGpm."""

from __future__ import annotations

import copy
import math
import random
import timeit
from dataclasses import replace

import pytest

from tufu_balance.golden import check, exact
from tufu_balance.model import data as MD
from tufu_balance.model.f32 import f32
from tufu_balance.model.snapshots import (DIRECT_INTERPOLATION_STATS, MAIN_INTERPOLATION_STATS, SimulationData,
                                          SimulationItem, SimulationSnapshot, compose_stat, composition_mismatches,
                                          early_current_gpm, interpolate_snapshot)


@pytest.fixture(scope="module")
def sd(db):
    return MD.load_simulation_data(db)


@pytest.fixture(scope="module")
def interp(db):
    return MD.load_interpolations(db)


def _stage4_get_by_time_chunk_index(time_chunk_index, src):
    """SimulationData.get_by_time_chunk_index as of 7da12da (re-sorts and scans on every call)."""
    x = f32(time_chunk_index)
    ordered = sorted(src, key=lambda s: s.time_chunk_index)
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


def _days():
    rng = random.Random(405)
    return [float(d) for d in range(0, 406)] + [float(f32(rng.uniform(0.0, 405.0))) for _ in range(100)]


def test_get_by_time_chunk_index_same_as_stage4(sd):
    days = _days() + [-3.0, 0.5, 404.5, 405.0, 450.0, math.inf, -math.inf, math.nan]
    shuffled = list(sd.time_chunk_snapshots)
    random.Random(7).shuffle(shuffled)
    dup = list(sd.time_chunk_snapshots[:20]) + [replace(sd.time_chunk_snapshots[10], source_index=999)] + \
        list(sd.time_chunk_snapshots[20:])
    for src in (None, shuffled, dup, sd.time_chunk_snapshots[5:9]):
        ref_src = sd.time_chunk_snapshots if src is None else src
        for d in days:
            got = sd.get_by_time_chunk_index(d, src)
            want = _stage4_get_by_time_chunk_index(d, ref_src)
            assert got == want, d
            if want.source_index is not None:
                assert got is want  # stored snapshots are returned, not copies
    assert len(days) >= 506


def test_ordered_cache_sees_list_edits(sd):
    data = SimulationData(list(sd.snapshots), list(sd.time_chunk_snapshots))
    a = data.get_by_time_chunk_index(10)
    data.time_chunk_snapshots[10] = replace(data.time_chunk_snapshots[10], location=77)
    assert data.get_by_time_chunk_index(10).location == 77 != a.location
    data.time_chunk_snapshots.append(replace(data.time_chunk_snapshots[-1], time_chunk_index=500, source_index=None))
    assert data.get_by_time_chunk_index(480).time_chunk_index in (405, 500)
    assert data.get_by_time_chunk_index(600).time_chunk_index == 500


def test_interpolated_snapshot_has_own_items(sd):
    left, right = sd.time_chunk_snapshots[12], sd.time_chunk_snapshots[13]
    s = interpolate_snapshot(left, right, f32(0.5), f32(14))
    assert s.items == left.items and s.items is not left.items
    n = len(left.items)
    s.items.append(SimulationItem("x", 1, 1))
    assert len(left.items) == n


def test_evolved_defaults_to_none_and_is_ignored_by_equality():
    a = SimulationItem("knife", 1, 1)
    assert a.evolved is None
    b = SimulationItem("knife", 1, 1, evolved=True)
    assert a == b and hash(a) == hash(b)
    assert SimulationItem("knife", 1, 2) != b


def test_stat_lookup_cache(sd):
    s = copy.deepcopy(sd.time_chunk_snapshots[20])
    for st in s.stats:
        assert s.stat(st.key) is st
        assert s.stat(int(st.key)) is st
    i = 3
    s.stats[i] = replace(s.stats[i], value=-1.0)
    assert s.stat(s.stats[i].key).value == -1.0
    s.stats = [st for st in s.stats if st.key != 1]
    with pytest.raises(KeyError):
        s.stat(1)
    dup = SimulationSnapshot(f32(0), f32(0), 1, 0, stats=[replace(s.stats[0], value=1.0), replace(s.stats[0], value=2.0)])
    assert dup.stat(s.stats[0].key).value == 1.0  # First()


def test_lookup_costs(sd):
    sd.get_by_time_chunk_index(100.0)
    n = 2000
    lookup = min(timeit.repeat(lambda: sd.get_by_time_chunk_index(100.0), number=n, repeat=3)) / n
    stored = min(timeit.repeat(lambda: sd.get_by_time_chunk_index(500.0), number=n, repeat=3)) / n
    snap = sd.time_chunk_snapshots[30]
    stat = min(timeit.repeat(lambda: snap.stat(22), number=n, repeat=3)) / n
    # Stage 4: ~49 us per lookup (re-sorted every call); interpolating 30 stats dominates now.
    assert stored <= 5e-6, stored
    assert lookup <= 40e-6, lookup
    assert stat <= 1e-6, stat


# ----------------------------------------------------------------------------- composition (G11 cells)
def test_compose_stat_rules():
    for key in range(36):
        st = compose_stat(key, 10.0, 0.5, 2.0, current_gpm=3.0)
        assert (st.key, st.value, st.current_gpm) == (key, 10.0, 3.0)
        if key in MAIN_INTERPOLATION_STATS:
            assert (st.stats_from_features_flat, st.percent_multiplier, st.total_flat_value) == (0.5, 2.0, 15.0)
            assert st.total_value == (30.0 if key in DIRECT_INTERPOLATION_STATS else 15.0)
        else:
            assert (st.stats_from_features_flat, st.percent_multiplier, st.total_flat_value, st.total_value) == \
                (0.0, 1.0, 10.0, 10.0)
    assert len(MAIN_INTERPOLATION_STATS) == 9 and DIRECT_INTERPOLATION_STATS == {1, 2, 22}


def test_composition_on_shipped_snapshots(sd, interp):
    snaps = sd.time_chunk_snapshots + sd.snapshots
    default = sum(len(composition_mismatches(s, interp)) for s in snaps)
    by_rounding = {r: sum(len(composition_mismatches(s, interp, r)) for s in snaps)
                   for r in ("stepwise", "fma", "double_tail", "double_all")}
    # stepwise: 981*4 - (972+945+972+960) = 75 mismatching main cells, other stats always exact.
    assert by_rounding == {"stepwise": 75, "fma": 0, "double_tail": 0, "double_all": 0}
    cells = sum(len(s.stats) for s in snaps)
    check("core", {
        "G11.default.mismatching_columns": exact(default, source="G11", original=0),
        "G11.stepwise.mismatching_columns": exact(by_rounding["stepwise"], source="G11", original=75),
        "G11.stat_cells": exact(cells, source="G11", note="981 main + 2289 other", original=3270),
    })


# ----------------------------------------------------------------------------- G19 CurrentGpm
def test_g19_current_gpm(sd):
    early = sd.snapshots
    ok = n = nan = 0
    for a, b in zip(early, early[1:]):
        for st in b.stats:
            g = early_current_gpm(a, b, st.key)
            if math.isnan(g):
                nan += math.isnan(st.current_gpm)
                continue
            n += 1
            ok += abs(st.current_gpm - g) <= 1e-5 * max(1.0, abs(g))
    days = sd.time_chunk_snapshots
    keys = [st.key for st in days[0].stats]
    mono = sum(days[i + 1].stat(k).current_gpm >= days[i].stat(k).current_gpm
               for i in range(len(days) - 1) for k in keys)
    values = sum(len(s.stats) for s in days)
    assert (ok, n, nan) == (1800, 1800, 60)
    assert mono == (len(days) - 1) * len(keys) and values == 1380
    check("core", {
        "G19.early_gpm_matches": exact(ok, source="G19", note="relative error <= 1e-5", original=1800),
        "G19.early_gpm_rows": exact(n, source="G19", original=1800),
        "G19.early_gpm_nan": exact(nan, source="G19", note="dP = 0 rows", original=60),
        "G19.day_gpm_values": exact(values, source="G19", original=1380),
        "G19.day_gpm_monotone_steps": exact(mono, source="G19", note="of 45 x 30 day-to-day steps", original=1350),
    })
