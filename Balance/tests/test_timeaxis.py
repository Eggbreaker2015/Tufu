"""Time axes (design 4.3): ported conversions, G14, G19 trimming, G22 session rule, G23 early-list makeup."""

from __future__ import annotations

import math

import numpy as np
import pytest

from tufu_balance.golden import at_most, check, exact, same_set
from tufu_balance.model import data as MD
from tufu_balance.model import timeaxis as TA
from tufu_balance.model.f32 import f32


@pytest.fixture(scope="module")
def sd(db):
    return MD.load_simulation_data(db)


@pytest.fixture(scope="module")
def cfg(db):
    return TA.load_time_axis_config(db)


def test_constants_and_shipped_config(cfg):
    assert TA.TIME_STEP_MIN == TA.SNAPSHOT_INTERVAL_MIN == 5.0
    assert cfg == TA.DEFAULT_CONFIG
    assert cfg.time_step_minutes == cfg.snapshot_interval_minutes == 5.0
    assert (cfg.session_minutes, cfg.offline_minutes) == (10.0, 230.0)
    assert TA.SPAWNER_CHARGES_CHUNK_MINUTES == 1440.0 and TA.TIME_CHUNK_DURATION_MINUTES == 30.0
    assert TA.SESSIONS_PER_DAY * (TA.SESSION_MINUTES + TA.OFFLINE_MINUTES) == 1440.0


def test_balance_timing_minutes():
    g = TA.get_balance_timing_minutes
    assert g(0, 1439.99) == 0.0 and g(0, 1440) == 1.0 and g(50, 583_000.0) == 404.0
    assert isinstance(g(0, 2000), np.float32)
    # float32 T: 1440*405 - 0.01 is not representable and rounds onto the day boundary
    assert g(0, 583_199.99) == 405.0 and f32(583_199.99) == f32(583_200.0)
    off = TA.TimeAxisConfig(use_spawner_charges=False)
    assert g(90, 999_999, off) == f32(1.5) and g(91, 0, off) == f32(f32(91) / f32(60))
    assert TA.time_chunk_index(5768.132) == 4 and TA.time_chunk_index(5759.9) == 3


def test_optimum_timing_cap_and_bake_day():
    assert TA.get_spawner_optimum_timing_for_current_mode(240, False) == f32(8.0)
    assert TA.get_spawner_optimum_timing_for_current_mode(7, True) == f32(7.0)
    assert TA.get_spawner_optimum_timing_for_current_mode(240, False, TA.TimeAxisConfig(use_spawner_charges=False)) == 240
    cap = TA.optimum_time_over_cap
    assert cap(7, True, 1000.0, 6) and not cap(7, True, 0.0, 7)
    assert cap(240, False, 239.9, 50) and not cap(240, False, 240.0, 0)
    assert not cap(7, True, 0.0, 0, TA.TimeAxisConfig(is_cap_optimum_time=False))
    assert TA.bake_day(240, False) == f32(4.0) and TA.bake_day(7.5, True) == f32(7.5)
    assert TA.bake_day(110, False) == f32(f32(110) / f32(60))
    assert TA.seconds_to_minutes(90.0) == 1.5
    assert [TA.writes_early_snapshot(d) for d in range(6)] == [True, True, True, True, False, False]


def test_g14_day_index_and_offline_multiples(sd, cfg):
    day = sum(TA.time_chunk_index(s.total_playtime, cfg) == s.time_chunk_index for s in sd.time_chunk_snapshots)
    early = sum(TA.time_chunk_index(s.total_playtime, cfg) == s.time_chunk_index for s in sd.snapshots)
    res = [TA.offline_residual(s.playtime, s.total_playtime) for s in sd.snapshots]
    within = sum(r <= 0.005 for r in res)
    assert (day, early, within) == (46, 63, 63) and max(res) <= 0.0033
    check("core", {
        "G14.day_list_tc_floor": exact(day, source="G14", original=46),
        "G14.early_list_tc_floor": exact(early, source="G14", original=63),
        "G14.early_offline_multiple": exact(within, source="G14", note="|T-P-230k| <= 0.005 min", original=63),
        "G14.early_offline_max_residual": at_most(max(res), source="G14", note="design: 0.0032", original=0.005),
    })


def test_g19_trimmed_day_list(sd):
    days = [s.time_chunk_index for s in sd.time_chunk_snapshots]
    rule = TA.trimmed_time_chunk_days(days[-1])
    assert rule == days
    assert TA.trimmed_time_chunk_days(445)[-2:] == [368, 405]
    assert TA.trimmed_time_chunk_days(446)[-2:] == [405, 446]
    assert TA.trimmed_time_chunk_days(420, keep_last=True)[-2:] == [405, 420]
    assert TA.trimmed_time_chunk_days(0) == [0]
    check("core", {
        "G19.trimmed_days_equal": exact(rule == days, source="G19", original=True),
        "G19.trimmed_days_count": exact(len(days), source="G19", original=46),
    })


def _early_rows(sd):
    return [(float(s.playtime), float(s.total_playtime)) for s in sd.snapshots]


def _pins(sd, cfg):
    return TA.offline_pins([(s.time_chunk_index, float(s.playtime), float(s.total_playtime))
                            for s in sd.time_chunk_snapshots], cfg)


def test_g22_session_rule(sd, cfg):
    rows = _early_rows(sd)
    pins = _pins(sd, cfg)
    assert {k: round(p, 4) for k, p in pins.items()} == {6: 63.2775, 12: 124.3761, 18: 185.9833, 24: 248.1339}
    good = TA.check_lifecycle(rows, pins, TA.RULE_FROM_OFFLINE, cfg.session_minutes, cfg.offline_minutes)
    bad = TA.check_lifecycle(rows, pins, TA.RULE_FIXED_GRID, cfg.session_minutes, cfg.offline_minutes)
    assert good.rows == 63 and good.feasible_rows == 63 and good.chain_feasible
    # The k-th offline lies between the last row with k-1 offlines and the first row with k.
    intervals = sum(lo <= hi for k, (lo, hi) in good.intervals.items() if k >= 1)
    assert intervals == 24
    assert bad.violations == (49, 52, 58, 60, 62) and bad.late == bad.violations
    check("core", {
        "G22.rule_feasible_rows": exact(good.feasible_rows, source="G22", original=63),
        "G22.rule_chain_feasible": exact(good.chain_feasible, source="G22", original=True),
        "G22.offline_intervals_nonempty": exact(intervals, source="G22", original=24),
        "G22.pinned_offlines": exact({str(k): round(p, 4) for k, p in sorted(pins.items())}, source="G22",
                                     original={"6": 63.2775, "12": 124.3761, "18": 185.9833, "24": 248.1339}),
        "G22.ablation_violating_rows": same_set(bad.violations, source="G22", note="period_end += 10",
                                                original=[49, 52, 58, 60, 62]),
    })


def test_g23_early_list_makeup(sd, cfg):
    early, days = sd.snapshots, sd.time_chunk_snapshots
    last = early[-1]
    last_k = TA.offline_count(last.playtime, last.total_playtime)
    pt = {(float(s.playtime), float(s.total_playtime)): i for i, s in enumerate(early)}
    forced_in_both = [pt[(float(d.playtime), float(d.total_playtime))] for d in days[1:]
                      if (float(d.playtime), float(d.total_playtime)) in pt]
    tc4 = next(d for d in days if d.time_chunk_index == 4)
    nan_rows = [i for i, s in enumerate(early) if math.isnan(s.stats[0].current_gpm)]
    gated = [d.time_chunk_index for d in days[1:] if TA.writes_early_snapshot(d.time_chunk_index, cfg)]
    assert len(early) == 63 and last.time_chunk_index == 3 and last_k == 23
    assert round(float(last.playtime), 4) == 248.1339 and round(float(last.total_playtime), 3) == 5538.132
    assert round(float(tc4.total_playtime), 3) == 5768.132 and float(tc4.playtime) == float(last.playtime)
    assert forced_in_both == [25, 37, 50] and gated == [1, 2, 3]
    assert nan_rows == [25, 50] and all(early[i - 1].playtime == early[i].playtime for i in nan_rows)
    check("core", {
        "G23.early_rows": exact(len(early), source="G23", original=63),
        "G23.last_row": exact([len(early) - 1, last.time_chunk_index, round(float(last.playtime), 4),
                               round(float(last.total_playtime), 3), last_k], source="G23",
                              note="index, TC, P, T, offlines before it", original=[62, 3, 248.1339, 5538.132, 23]),
        "G23.tc4_forced_only_in_day_list": exact((float(tc4.playtime), float(tc4.total_playtime)) not in pt,
                                                 source="G23", original=True),
        "G23.forced_rows_in_both_lists": exact(forced_in_both, source="G23", original=[25, 37, 50]),
        "G23.nan_gpm_rows": exact(nan_rows, source="G23", note="copies right after the day-crossing offline",
                                  original=[25, 50]),
    })
