"""Calibration harness (design 5, 7.3 P10): targets, metrics, tolerances, parameter layers, fitting tools,
reports, driver and run cache. No Unity: the shipped Simulation Data stands in for a run (C1's ingest of
its YAML), a fake backend and a fake editor stand in for Unity."""

from __future__ import annotations

import ast
import copy
import hashlib
import inspect
import json
import math
import os
import shutil
import sqlite3
import subprocess
import sys
import textwrap
from collections import Counter
from dataclasses import replace
from pathlib import Path

import numpy as np
import pytest

from tufu_balance.calibrate import driver as D, fit, metrics as M, params as P, report as R, runcache as RC, targets as T
from tufu_balance.golden import check, exact
from tufu_balance.unity import launcher, request as req, runner, simio

BALANCE = Path(__file__).resolve().parents[1]
COMPUTED = ("T01", "T02", "T03", "T04", "T05", "T06", "T07", "T09", "T10", "T11", "T12", "T13", "T14", "T15", "T16", "T17",
            "T18")


@pytest.fixture(scope="module")
def tg(db):
    return T.build(db, slot_schedule=T.load_slot_schedule())


@pytest.fixture(scope="module")
def shipped_db(tg, tmp_path_factory):
    job = M.write_shipped_job(tmp_path_factory.mktemp("shipped") / "shipped")
    return simio.ingest(job)


@pytest.fixture(scope="module")
def tolerances():
    return M.load_tolerances()


def _series(path: Path) -> T.Series:
    con = T.connect_ro(path)
    try:
        return T.load_series(con)
    finally:
        con.close()


# ---------------------------------------------------------------- targets

def test_targets_rebuilt_from_the_database_equal_the_fixture(tg, load_fixture):
    fixture = {k: v for k, v in load_fixture("forensics_targets").items() if k != "meta"}
    built = json.loads(json.dumps(tg.forensics()))
    assert sorted(built) == sorted(fixture)
    for key in fixture:
        assert built[key] == fixture[key], key
    # value by value, including float bits (JSON round trip of a double is exact)
    for a, b in zip(built["tc"], fixture["tc"]):
        for k in T.STATS:
            assert struct_bits(a["Value"][k]) == struct_bits(b["Value"][k])
            assert struct_bits(a["Gpm"][k]) == struct_bits(b["Gpm"][k])
    assert (len(built["tc"]), len(built["early"]), len(built["active_min_per_day"])) == (46, 63, 45)


def struct_bits(x: float) -> str:
    return float(x).hex()


def test_targets_do_not_read_the_fixture(db, monkeypatch):
    """No code path of the target build opens the frozen fixture (only the docs mention it)."""
    import builtins
    import pathlib

    opened = []
    real_open, real_read = builtins.open, pathlib.Path.read_text

    def spy_open(file, *a, **kw):
        opened.append(str(file))
        return real_open(file, *a, **kw)

    def spy_read(self, *a, **kw):
        opened.append(str(self))
        return real_read(self, *a, **kw)

    monkeypatch.setattr(builtins, "open", spy_open)
    monkeypatch.setattr(pathlib.Path, "read_text", spy_read)
    T.forensics_view(T.build(db).shipped)
    assert not [f for f in opened if "fixtures" in f]
    for module in (T, M):
        code = [n.value for n in ast.walk(ast.parse(inspect.getsource(module)))
                if isinstance(n, ast.Constant) and isinstance(n.value, str) and "\n" not in n.value]
        assert not [c for c in code if "forensics_targets" in c]


def test_config_inputs_of_the_targets(tg):
    knife = tg.items["knife"]
    assert (knife.slot, knife.rarity, knife.is_armor) == ("Weapon", "Common", False)
    assert len(knife.target_timings) >= 10 and len(knife.shards) == len(knife.target_timings)
    assert tg.one_clear_reward["Health"][1] <= tg.one_clear_reward["Health"][17]
    assert sorted(tg.slot_schedule)[:3] == [0, 1, 2] and len(tg.slot_schedule) == 46


# ---------------------------------------------------------------- metrics

def test_shipped_against_shipped_has_zero_residuals(tg, shipped_db, tolerances):
    sim = _series(shipped_db)
    cells = M.compute(sim, tg)
    by_target = Counter(c.target for c in cells)
    assert set(by_target) == set(COMPUTED)
    # the shipped Simulation Data has no slot counts: T17 has no values for it (not synthesized)
    assert sim.slots is None and {c.residual for c in cells if c.target == "T17"} == {None}
    nonzero = [c for c in cells if c.residual != 0 and c.target != "T17"]
    assert nonzero == []
    for mode in M.MODES:
        ev = M.evaluate(cells, tolerances, mode)
        assert ev.failing_cells() == []
        assert {c.ok for c in ev.cells if c.target == "T17"} == {None} and ev.targets["T17"] == "unavailable"
        assert {t: s for t, s in ev.targets.items()
                if t in COMPUTED and s not in ("pass", "report", "n/a", "pending", "unavailable")} == {}
    closed = M.evaluate(cells, tolerances, "closed").targets
    assert [t for t in COMPUTED if closed[t] == "pass"] == [t for t in COMPUTED
                                                            if t not in ("T11", "T17")]
    assert (closed["T05"], closed["T07"], closed["T11"], closed["T17"]) == ("pass", "pass", "report",
                                                                              "unavailable")
    check("calibrate", {
        "T01.days": exact(by_target["T01"], source="T01", original=46),
        "T05.cells": exact(by_target["T05"], source="T05", note="41 intervals from TC4 x 15 stats", original=615),
        "T09.cells": exact(by_target["T09"], source="T09/G20", original=1380),
        "T10.cells": exact(by_target["T10"], source="T10/G19", note="1800 values + 60 NaN", original=1860),
        "T18.cells": exact(by_target["T18"], source="T18/G11", note="981 main + 2289 other", original=3270),
        "cells.total": exact(sum(c.target != "T15" for c in cells), source="design 7.3 P10", note="cells of T01-T07, T09-T14, T16-T18"),
    })


def test_health_times_1_01_fails_only_health_cells(tg, shipped_db, tolerances, tmp_path):
    db = tmp_path / "sim.sqlite"
    shutil.copy(shipped_db, db)
    con = sqlite3.connect(db)
    for table in ("c_SimulationData__TimeChunkSnapshots__Stats", "c_SimulationData__Snapshots__Stats"):
        con.execute(f'UPDATE "{table}" SET Value = Value * 1.01 WHERE Key__name = \'Health\'')
    con.commit()
    con.close()
    expected = {"open": {"T05", "T10", "T18"}, "closed": {"T05", "T06", "T10", "T18"}}
    cells_failing = {"open": {"T05", "T10", "T18"}, "closed": {"T05", "T06", "T10", "T18"}}
    for mode in M.MODES:
        ev = M.evaluate_run(db, tg, mode, tolerances)
        failing = ev.failing_cells()
        assert failing and {c.stat for c in failing} == {"Health"}
        assert {t for t, s in ev.targets.items() if s == "fail"} == expected[mode]
        assert {c.target for c in failing} == cells_failing[mode]
    open_ = M.evaluate_run(db, tg, "open", tolerances)
    assert open_.targets["T06"] == "n/a" and {c.ok for c in open_.cells if c.target in ("T06", "T16")} == {None}
    closed = M.evaluate_run(db, tg, "closed", tolerances)
    assert closed.targets["T16"] == "pass"  # TC1-4 Health: 1% is inside the slow-phase 5%
    assert closed.targets["T09"] == "pass" and closed.targets["T04"] == "pass"


def test_metrics_see_a_later_unlock_and_fewer_active_minutes(tg, shipped_db, tolerances):
    sim = _series(shipped_db)
    late = copy.deepcopy(sim)
    for s in late.tc:
        if s.tc in (37, 41):  # island 10 first seen at TC37 in the shipped run, now at TC46
            s.location = min(s.location, 9)
        if s.tc >= 206:
            s.playtime *= 0.9
    ev = M.evaluate(M.compute(late, tg, ["T04", "T07"]), tolerances, "closed")
    bad = {(c.target, c.key) for c in ev.failing_cells()}
    assert ("T07", "island 10") in bad
    assert ev.targets["T07"] == "fail" and ev.targets["T04"] == "fail"
    assert all(c.tc >= 187 for c in ev.failing_cells() if c.target == "T04")


def test_unlock_interval_uses_the_untrimmed_day_list(tg):
    full = copy.deepcopy(tg.shipped)
    days = []
    loc = 1
    for d in range(0, 40):
        s = copy.deepcopy(full.tc[0])
        s.tc, loc = d, 10 if d >= 35 else 9
        s.location = loc
        days.append(s)
    full.full = days
    assert M.unlock_interval_days(full, 10) == [34, 34]
    assert M.unlock_interval_days(tg.shipped, 10) == [33, 36]
    assert M.unlock_interval_days(tg.shipped, 1) == [0, 0]  # already there at the first snapshot
    assert M.unlock_interval_days(full, 11) is None


def test_tolerances_name_every_group(tg, shipped_db, tolerances):
    cells = M.compute(_series(shipped_db), tg)
    cells += M.t12_expected({}, tg)  # side-trace metric is unavailable from sim.sqlite alone
    groups: dict[str, set] = {}
    for c in cells:
        groups.setdefault(c.target, set()).add(c.group)
    for tid, spec in tolerances["targets"].items():
        named = set()
        for key in ("cells", "cells_open", "cells_closed", "open", "closed"):
            if isinstance(spec.get(key), dict):
                named |= set(spec[key])
        if tid in COMPUTED:
            assert named <= groups[tid], (tid, named - groups[tid])
            missing = groups[tid] - named
            assert not missing, (tid, missing)
        else:
            assert tid not in groups


# ---------------------------------------------------------------- the tolerance table is design 5.1's

ALL = {"all_ok": True}
# Written from design 5.1's table (not read from tolerances.yaml): every rule of every computed target.
DESIGN_5_1 = {
    "T01": {"cells": {"days": {"exact": True}}, "open": "n/a", "closed": {"days": ALL}},
    "T02": {"cells": {"tc_floor": {"exact": True}, "offline_multiple": {"exact": True},
                      "session_rule": {"exact": True}},
            "open": "n/a",
            "closed": {"tc_floor": ALL, "offline_multiple": ALL, "session_rule": ALL, "p_over_t": "report"}},
    "T03": {"cells": {"rows": {"abs_max": 3}, "last_row_tc": {"exact": True},
                      "fast_days_only_in_day_list": {"exact": True}, "cadence": {"exact": True},
                      "forced_day_snapshot": {"exact": True}},
            "open": "n/a",
            "closed": {"rows": ALL, "last_row_tc": ALL, "last_row_t": "report", "fast_days_only_in_day_list": ALL,
                       "cadence": ALL, "forced_day_snapshot": ALL}},
    "T04": {"cells": {"tc_ge_11": {"rel_max": 0.08}, "day_0_3": {"rel_max": 0.03}, "p_final": {"rel_max": 0.03}},
            "open": {"tc_ge_11": {"all_ok": True, "median_rel_max": 0.02},
                     "tc_4_10": {"report": True, "note": "known failure of both models"},
                     "day_0_3": "report", "p_final": "report"},
            "closed": {"tc_ge_11": ALL, "tc_4_10": {"report": True, "note": "known failure of both models"},
                       "day_0_3": ALL, "p_final": ALL}},
    "T05": {"cells": {"daily": {"abs_or_rel": [0.5, 2.0e-5]}, "multi_day": {"abs_or_rel": [0.5, 2.0e-5]}},
            "open": {"daily": ALL, "multi_day": {"max_failing_units": None,
                                                 "note": "unexplained intervals registered; count frozen at the first run"}},
            "closed": {"daily": ALL, "multi_day": {"max_failing_units": 5, "note": "first calibrated free run 36/41 exact"}}},
    "T06": {"cells": {"tc_5_23": {"rel_or_bound": 0.02}, "tc_ge_24": {"rel_max": 0.005}},
            "open": "n/a",
            "closed": {"tc_1_4": {"report": True, "note": "same target as T16"}, "tc_5_23": ALL,
                       "tc_ge_24": {"frac_ok_min": 0.95, "rel_max_all": 0.02}}},
    "T07": {"cells": {"by_playtime": {"ranges": {2: [31.2, 37.4], 3: [110.2, 115.3]}, "slack": 0,
                                      "day_ranges": {2: [0, 0], 3: [1, 1]}, "slack_days": 1},
                      "island_4": {"ranges": {4: [5, 5]}, "slack": 1},
                      "islands_5_17": {"ranges": {5: [8, 8], 6: [11, 12], 7: [15, 16], 8: [21, 23], 9: [27, 29],
                                                  10: [34, 36], 11: [44, 45], 12: [57, 62], 13: [71, 76],
                                                  14: [90, 93], 15: [110, 114], 16: [130, 139], 17: [150, 153]},
                                       "slack": 1}},
            "open": {"by_playtime": "report",
                     "island_4": {"report": True, "note": "a result only once derived from the island-3 gate"},
                     "islands_5_17": ALL},
            "closed": {"by_playtime": ALL, "island_4": ALL,
                       "islands_5_17": {"all_ok": True, "min_in_range": 11}}},
    "T09": {"cells_open": {"nonzero": {"g20": 1.0e-4}, "zero": {"g20": 1.0e-4}},
            "cells_closed": {"nonzero": {"rel_max": 0.01}},
            "open": {"nonzero": ALL, "zero": ALL}, "closed": {"nonzero": {"frac_ok_min": 0.9}, "zero": "report"}},
    "T10": {"cells": {"gpm": {"exact": True}, "nan": {"exact": True}},
            "open": {"gpm": ALL, "nan": ALL}, "closed": {"gpm": ALL, "nan": ALL}},
    "T11": {"cells": {"first_appearance": {"window": [-1, 1]}},
            "open": {"first_appearance": {"report": True}}, "closed": {"first_appearance": {"report": True}}},
    "T12": {"cells": {"daily": {"rel_max": 0.05}, "daily_expected": {"bound": True}, "phase_4_19": {"rel_max": 0.05}, "phase_19_57": {"rel_max": 0.05},
                      "phase_57_104": {"rel_max": 0.05}, "phase_104_154": {"rel_max": 0.07},
                      "phase_154_405": {"rel_max": 0.07}},
            "open": {g: ALL for g in ("daily_expected", "phase_4_19", "phase_19_57", "phase_57_104", "phase_104_154",
                                      "phase_154_405") } | {"daily": {"report": True, "note": "integer-noise diagnostic; see Runbook 24.7 and calib_open loosening"}},
            "closed": {g: ALL for g in ("daily_expected", "phase_4_19", "phase_19_57", "phase_57_104", "phase_104_154",
                                        "phase_154_405")} | {"daily": {"report": True, "note": "integer-noise diagnostic; see Runbook 24.7 and calib_open loosening"}}},
    "T13": {"cells": {"train": {"window": [-1, 1]}, "holdout": {"window": [-1, 1]}},
            "open": {"train": {"frac_ok_min": 0.9}, "holdout": {"frac_ok_min": 0.9}},
            "closed": {"train": {"frac_ok_min": 0.9}, "holdout": {"frac_ok_min": 0.9}}},
    "T14": {"cells": {"level": {"exact": True}, "copies": {"rel_max": 0.05}},
            "open": "n/a", "closed": {"level": {"frac_ok_min": 0.9}, "copies": {"frac_ok_min": 0.8}}},
    "T15": {"cells": {"mid_dps": {"rel_max": 0.15}, "mid_defense": {"rel_max": 0.15},
                        "late_dps": {"rel_max": 0.1706405195322352}, "late_defense": {"rel_max": 0.009840993197501419}},
            "open": "report", "closed": {"early_dps": "report", "early_defense": "report", "mid_dps": ALL,
                                            "mid_defense": ALL, "late_dps": ALL, "late_defense": ALL}},
    "T16": {"cells": {"main": {"rel_max": 0.05}, "small": {"bound": True}},
            "open": "n/a", "closed": {"main": ALL, "small": ALL}},
    "T17": {"cells": {"weapon": {"exact": True}, "armor_before_9": {"exact": True}},
            "open": {"weapon": {"report": True, "note": "the reference is fitted"}, "armor_before_9": ALL},
            "closed": {"weapon": {"report": True, "note": "the reference is fitted"}, "armor_before_9": ALL}},
    "T18": {"cells": {"composition": {"exact": True}},
            "open": {"composition": ALL}, "closed": {"composition": ALL}},
}


def test_tolerances_are_the_design_table(tolerances):
    """Every rule of tolerances.yaml equals design 5.1 as transcribed above (types included: 1 is not 1.0
    only where it matters, True is not 1)."""
    rules = ("cells", "cells_open", "cells_closed", "open", "closed")

    def typed(x):
        if isinstance(x, dict):
            return {k: typed(v) for k, v in x.items()}
        if isinstance(x, list):
            return [typed(v) for v in x]
        return (type(x).__name__ if isinstance(x, bool) else "num", x)

    for tid, expected in DESIGN_5_1.items():
        got = {k: v for k, v in tolerances["targets"][tid].items() if k in rules}
        assert typed(got) == typed(expected), tid
    assert set(tolerances["targets"]) - set(DESIGN_5_1) == {"T08", "T19", "T20"}
    assert tolerances["holdout_from_tc"] == 104


# ---------------------------------------------------------------- criteria and group rules at their boundaries

def _cell(residual, ref=100.0, rel=None, bound=None, unit=None, key="k", sim=None, ok=None):
    if sim is None and residual is not None and not (isinstance(residual, float) and math.isnan(residual)):
        sim = ref + residual
    return M.Cell("TX", "g", key, sim, ref, residual, rel, bound=bound, unit=unit, ok=ok)


@pytest.mark.parametrize("crit, cell, ok", [
    ({"exact": True}, _cell(0), True),
    ({"exact": True}, _cell(1e-12), False),
    ({"abs_max": 3}, _cell(-3), True),
    ({"abs_max": 3}, _cell(3.0001), False),
    ({"rel_max": 0.05}, _cell(5, rel=0.05), True),
    ({"rel_max": 0.05}, _cell(5.01, rel=0.0501), False),
    ({"rel_max": 0.05}, _cell(0, rel=None), False),
    # T05: absolute 0.5 OR relative 2e-5
    ({"abs_or_rel": [0.5, 2.0e-5]}, _cell(0.5, ref=1.0, rel=0.5), True),
    ({"abs_or_rel": [0.5, 2.0e-5]}, _cell(-0.5, ref=1.0, rel=0.5), True),
    ({"abs_or_rel": [0.5, 2.0e-5]}, _cell(10.0, ref=5.0e5, rel=2.0e-5), True),
    ({"abs_or_rel": [0.5, 2.0e-5]}, _cell(10.5, ref=5.0e5, rel=2.1e-5), False),
    ({"abs_or_rel": [0.5, 2.0e-5]}, _cell(0.51, ref=1.0, rel=0.51), False),
    # T06: |sim - ref| <= max(rel * |ref|, the one-clear bound)
    ({"rel_or_bound": 0.02}, _cell(5.0, ref=100.0, bound=5.0), True),
    ({"rel_or_bound": 0.02}, _cell(5.01, ref=100.0, bound=5.0), False),
    ({"rel_or_bound": 0.02}, _cell(-20.0, ref=1000.0, bound=5.0), True),
    ({"rel_or_bound": 0.02}, _cell(20.01, ref=1000.0, bound=5.0), False),
    ({"rel_or_bound": 0.02}, _cell(2.0, ref=100.0), True),
    # T16 small stats: within one clear of the most rewarding reachable spawner
    ({"bound": True}, _cell(80.0, bound=80.0), True),
    ({"bound": True}, _cell(-80.0, bound=80.0), True),
    ({"bound": True}, _cell(80.5, bound=80.0), False),
    ({"bound": True}, _cell(1.0, bound=None), False),
    # T11/T13: in day snapshots
    ({"window": [-1, 1]}, _cell(-1), True),
    ({"window": [-1, 1]}, _cell(1), True),
    ({"window": [-1, 1]}, _cell(-2), False),
    ({"window": [-1, 1]}, _cell(2), False),
    # T09 open: G20's cell test, |sim - ref| <= 1e-4 * max(1, |ref|)
    ({"g20": 1.0e-4}, _cell(0.9e-4, ref=0.5, rel=1.8e-4), True),
    ({"g20": 1.0e-4}, _cell(1.1e-4, ref=0.5, rel=2.2e-4), False),
    ({"g20": 1.0e-4}, _cell(0.5e-4, ref=0.0, rel=math.inf), True),
    ({"g20": 1.0e-4}, _cell(2.0e-4, ref=0.0, rel=math.inf), False),
    ({"g20": 1.0e-4}, _cell(0.15, ref=2000.0, rel=7.5e-5), True),
    ({"g20": 1.0e-4}, _cell(0.25, ref=2000.0, rel=1.25e-4), False),
    # no value, NaN
    ({"abs_max": 3}, _cell(None), False),
    ({"abs_max": 3}, _cell(math.nan), False),
    ({}, _cell(1e9), None),
])
def test_cell_criteria_at_their_boundaries(crit, cell, ok):
    assert M._cell_ok(cell, crit) is ok


@pytest.mark.parametrize("sim, ok", [([34, 36], True), ([37, 37], True), ([38, 38], False), ([33, 33], True),
                                     ([32, 32], False), (None, False)])
def test_t07_range_and_slack(sim, ok):
    cell = M.Cell("T07", "islands_5_17", "island 10", sim, [33, 36], 0 if sim else None)
    assert M._cell_ok(cell, {"ranges": {10: [34, 36]}, "slack": 1}) is ok


def _group(oks, rels=None, units=None, residual=0.0):
    rels = rels or [0.0] * len(oks)
    units = units or [None] * len(oks)
    return [_cell(residual, rel=r, unit=u, key=f"k{i}", ok=o) for i, (o, r, u) in enumerate(zip(oks, rels, units))]


@pytest.mark.parametrize("cells, rule, status", [
    (_group([True, True]), {"all_ok": True}, "pass"),
    (_group([True, False]), {"all_ok": True}, "fail"),
    (_group([True] * 9 + [False]), {"frac_ok_min": 0.9}, "pass"),
    (_group([True] * 8 + [False] * 2), {"frac_ok_min": 0.9}, "fail"),
    (_group([True] * 3, rels=[0.01, 0.02, 0.5]), {"median_rel_max": 0.02}, "pass"),
    (_group([True] * 3, rels=[0.01, 0.03, 0.5]), {"median_rel_max": 0.02}, "fail"),
    (_group([True, True], rels=[0.0, 0.02]), {"rel_max_all": 0.02}, "pass"),
    (_group([True, True], rels=[0.0, 0.0201]), {"rel_max_all": 0.02}, "fail"),
    (_group([True, True], rels=[0.0, None]), {"rel_max_all": 0.02}, "fail"),
    (_group([True] * 9 + [False], rels=[0.01] * 9 + [0.03]), {"frac_ok_min": 0.95, "rel_max_all": 0.05}, "fail"),
    (_group([True] * 19 + [False], rels=[0.01] * 19 + [0.03]), {"frac_ok_min": 0.95, "rel_max_all": 0.02}, "fail"),
    (_group([True] * 19 + [False], rels=[0.01] * 19 + [0.02]), {"frac_ok_min": 0.95, "rel_max_all": 0.02}, "pass"),
    # T05 multi-day intervals: a unit (interval) fails when any of its cells fails
    (_group([True, False, False, True, True], units=["a", "a", "b", "c", "c"]), {"max_failing_units": 2}, "pass"),
    (_group([True, False, False, True, False], units=["a", "a", "b", "c", "c"]), {"max_failing_units": 2}, "fail"),
    (_group([True, False, False, True, True], units=["a", "a", "b", "c", "c"]), {"max_failing_units": 1}, "fail"),
    (_group([False], units=["a"]), {"max_failing_units": None}, "report"),
    (_group([False]), "n/a", "n/a"),
    (_group([False]), "report", "report"),
    (_group([False]), {"report": True, "note": "x"}, "report"),
    (_group([False]), {"pending": "C5"}, "pending"),
    # a column the run does not export at all: no verdict; a state the run never reached: a failure
    ([replace(_cell(None), available=False)] * 2, {"all_ok": True}, "unavailable"),
    ([_cell(None), _cell(None)], {"all_ok": True}, "pass"),   # cells' ok set by evaluate (None here)
    ([_cell(None, ok=False), _cell(None, ok=False)], {"all_ok": True}, "fail"),
    (_group([True, True]), {"all_ok": True, "pending_clause": "K3"}, "pending"),
    (_group([None, True, True], units=["a", "a", "b"]), {"max_failing_units": 0}, "pass"),  # no verdict: not failing
    (_group([True] * 19 + [None]), {"frac_ok_min": 0.95}, "pass"),
    (_group([True] * 18 + [None] * 2), {"frac_ok_min": 0.95}, "fail"),  # a cell without verdict is not ok
    (_group([True, False]), {"all_ok": True, "pending_clause": "K3"}, "fail"),
])
def test_group_rules_at_their_boundaries(cells, rule, status):
    assert M._group_status("TX", "g", cells, rule).status == status


def test_mode_without_a_rule_gives_cells_no_verdict(tolerances):
    cells = [M.Cell("T06", "tc_ge_24", "024|Health", 2.0, 1.0, 1.0, 1.0, stat="Health", tc=24),
             M.Cell("T17", "weapon", "001|weapon", None, 2, None, tc=1, available=False)]
    for mode in M.MODES:
        ev = M.evaluate(cells, tolerances, mode)
        assert ev.cells[1].ok is None and ev.targets["T17"] == "unavailable"
        assert ev.cells[0].ok is (None if mode == "open" else False)
        assert ev.failing_cells() == ([] if mode == "open" else [ev.cells[0]])


def test_evaluate_rule_lookup_and_target_status():
    tol = {"targets": {"TX": {"cells": {"g": {"exact": True}, "h": {"exact": True}},
                              "cells_closed": {"g": {"abs_max": 5}},
                              "open": {"g": ALL}, "closed": {"g": ALL}}}}
    cells = [M.Cell("TX", "g", "a", 3.0, 0.0, 3.0), M.Cell("TX", "h", "b", 1.0, 0.0, 1.0)]
    closed, open_ = M.evaluate(cells, tol, "closed"), M.evaluate(cells, tol, "open")
    assert [c.ok for c in closed.cells] == [True, False] and [c.ok for c in open_.cells] == [False, False]
    # a group a mode does not name is reported, with verdicts listed
    assert {g.group: g.status for g in closed.groups} == {"g": "pass", "h": "report"}
    assert (closed.targets["TX"], open_.targets["TX"]) == ("pass", "fail")
    # a target with a passing group and a group the run does not export is unavailable, not pass
    cells.append(M.Cell("TX", "h", "c", None, 1.0, None, available=False))
    tol["targets"]["TX"]["closed"]["h"] = ALL
    cells[1] = replace(cells[1], available=False, sim=None, residual=None)
    ev = M.evaluate(cells, tol, "closed")
    assert {g.group: g.status for g in ev.groups} == {"g": "pass", "h": "unavailable"}
    assert ev.targets["TX"] == "unavailable" and [c.ok for c in ev.cells] == [True, None, None]


def test_group_and_split_boundaries(tg, shipped_db, tolerances):
    cells = {(c.target, c.key): c for c in M.compute(_series(shipped_db), tg)}
    t04 = {"003->004": "day_0_3", "004->005": "tc_4_10", "010->011": "tc_4_10", "011->013": "tc_ge_11"}
    assert {k: cells[("T04", k)].group for k in t04} == t04
    t05 = {"010->011|Health": "daily", "011->013|Health": "multi_day"}
    assert {k: cells[("T05", k)].group for k in t05} == t05 and cells[("T05", "011->013|Health")].unit == "011->013"
    t06 = {"001|Health": "tc_1_4", "004|Health": "tc_1_4", "005|Health": "tc_5_23", "021|Health": "tc_5_23",
           "024|Health": "tc_ge_24"}
    assert {k: cells[("T06", k)].group for k in t06} == t06
    assert (cells[("T16", "002|Health")].group, cells[("T16", "002|BuckshotDamageFlat")].group) == ("main", "small")
    assert (M._split(103), M._split(104)) == ("train", "holdout") and tolerances["holdout_from_tc"] == M.HOLDOUT_TC
    assert cells[("T06", "094|Health")].split == "train" and cells[("T06", "104|Health")].split == "holdout"


def test_one_clear_bounds_of_hand_checked_spawners(tg, shipped_db):
    """Largest Value x Count among registered non-event, non-VIP, non-dummy spawners on islands <= L (checked
    by hand against the spawner list: the VIP spawners of island 3 would give 4800 Health, 360 Buckshot)."""
    bound, src = tg.one_clear_reward, tg.one_clear_source
    assert bound["Health"][1] == bound["Health"][3] == 500.0 and src["Health"][3].startswith("Spawner_elite_HP_")
    assert bound["Health"][4] == 2500.0 and src["Health"][4].startswith("Spawner_elite health_")
    assert bound["BuckshotDamageFlat"][1] == bound["BuckshotDamageFlat"][2] == 0.0
    assert bound["BuckshotDamageFlat"][3] == 80.0 and src["BuckshotDamageFlat"][3].startswith("Spawner_elite smash_")
    assert bound["HealthRegen"][1] == 2.0 and src["HealthRegen"][1].startswith("Spawner_1 (2)_")  # 0.5 x 4
    cells = {(c.target, c.key): c for c in M.compute(_series(shipped_db), tg, ["T16"])}
    assert [cells[("T16", f"00{d}|Health")].bound for d in (2, 3, 4)] == [500.0] * 3
    assert [cells[("T16", f"00{d}|BuckshotDamageFlat")].bound for d in (2, 3, 4)] == [80.0] * 3


def test_one_clear_skips_event_vip_dummy_and_empty_spawners(db, monkeypatch):
    from types import SimpleNamespace as NS

    from tufu_balance.model import simcalc
    from tufu_balance.model.enums import StatType

    def spawner(key, value, count=1, location=1, **flags):
        d = NS(key=key, count=count, location=location,
               **{"is_event": False, "is_vip": False, "is_dummy": False, **flags})
        return NS(d=d, stat_rewards=[NS(stat_type=int(StatType.Health), value=value)])

    monkeypatch.setattr(simcalc, "load_sim_spawners", lambda con: [
        spawner("plain", 10.0, count=3), spawner("event", 1000.0, is_event=True), spawner("vip", 2000.0, is_vip=True),
        spawner("dummy", 3000.0, is_dummy=True), spawner("empty", 4000.0, count=0),
        spawner("later", 50.0, location=5)])
    src: dict = {}
    best = T.load_one_clear_rewards(db, src)
    assert [best["Health"][loc] for loc in (1, 4, 5, 17)] == [30.0, 30.0, 50.0, 50.0]
    assert (src["Health"][1], src["Health"][5]) == ("plain", "later") and best["MovementSpeed"][17] == 0.0


def test_armor_items_use_the_armor_evolution_timings(tg, db):
    cbc = dict(db.execute("SELECT * FROM c_CombatBalanceConfig").fetchone())
    armor = next(i for i in tg.items.values() if i.is_armor and i.rarity == "Common")
    weapon = next(i for i in tg.items.values() if not i.is_armor and i.rarity == "Common")
    assert armor.target_timings == tuple(json.loads(cbc["CommonArmorEvolutionTargetTimings"]))
    assert weapon.target_timings == tuple(json.loads(cbc["CommonEvolutionTargetTimings"]))
    assert armor.target_timings != weapon.target_timings


@pytest.mark.parametrize("extra, ok", [(80.0, True), (81.0, False)])
def test_t16_small_stat_within_one_clear(tg, shipped_db, tolerances, extra, ok):
    sim = copy.deepcopy(_series(shipped_db))
    sim.day(3).stats["BuckshotDamageFlat"]["Value"] += extra
    ev = M.evaluate(M.compute(sim, tg, ["T16"]), tolerances, "closed")
    assert ev.targets["T16"] == ("pass" if ok else "fail")
    assert {c.key for c in ev.failing_cells()} == (set() if ok else {"003|BuckshotDamageFlat"})


def test_t09_open_uses_g20s_criterion(tg, shipped_db, tolerances):
    sim = copy.deepcopy(_series(shipped_db))
    small = next((s, k) for s in sim.tc for k, f in s.stats.items() if 0.5 <= abs(f["CurrentGpm"]) < 1.0)
    zero = next((s, k) for s in sim.tc for k, f in s.stats.items() if f["CurrentGpm"] == 0.0)
    small[0].stats[small[1]]["CurrentGpm"] += 0.9e-4  # relative 1e-4 of |ref| < 1 would fail
    zero[0].stats[zero[1]]["CurrentGpm"] = 0.5e-4
    assert M.evaluate(M.compute(sim, tg, ["T09"]), tolerances, "open").targets["T09"] == "pass"
    zero[0].stats[zero[1]]["CurrentGpm"] = 2.0e-4
    small[0].stats[small[1]]["CurrentGpm"] += 0.2e-4  # now 1.1e-4 away
    ev = M.evaluate(M.compute(sim, tg, ["T09"]), tolerances, "open")
    assert ev.targets["T09"] == "fail"
    assert sorted(c.key for c in ev.failing_cells()) == sorted(f"{s.tc:03d}|{k}" for s, k in (small, zero))


# ---------------------------------------------------------------- T03 cadence on a synthetic early list

def _early(tg, playtimes, params=None):
    template = tg.shipped.early[0]
    rows = []
    for i, p in enumerate(playtimes):
        s = copy.deepcopy(template)
        s.index, s.tc, s.playtime, s.total_playtime = i, 0, p, p
        rows.append(s)
    return T.Series(tc=[], early=rows, params=params)


@pytest.mark.parametrize("playtimes, params, bad", [
    ([0.0, 1.0, 2.0, 3.0], None, []),
    ([0.0, 1.0, 1.9, 3.0], None, ["Snapshots[02]"]),            # too soon: 0.9 < 1
    ([0.0, 1.0, 4.0, 5.0], None, []),                            # 3.0 = interval 1 + 2 min of one action
    ([0.0, 1.0, 4.5, 5.5], None, ["Snapshots[02]"]),            # too late
    ([0.0, 19.5, 25.0, 30.0], None, ["Snapshots[01]"]),         # 19.5 late; P = 25 > 20: interval 5
    ([19.0, 20.0, 21.0], None, ["Snapshots[02]"]),              # from P = 20 exactly the interval is 5
    ([0.0, 1.0, 2.0, 3.0], {"EarlySnapshotIntervalStarts": [0.0], "EarlySnapshotIntervals": [2.0]},
     ["Snapshots[01]", "Snapshots[02]", "Snapshots[03]"]),       # the run's own P06
])
def test_t03_cadence_both_ways_with_the_runs_intervals(tg, playtimes, params, bad):
    cells = [c for c in M.t03(_early(tg, playtimes, params), tg) if c.group == "cadence"]
    assert [c.key for c in cells if c.residual != 0] == bad


def test_position_counts_a_day_between_snapshots_at_the_next_one():
    days = [0, 1, 2, 13, 15]
    assert [M._position(days, tc) for tc in (0, 2, 3, 12, 13, 14, 16)] == [0, 2, 3, 3, 3, 4, 5]


@pytest.mark.parametrize("offline, ok", [(230.004, True), (460.0, True), (230.02, False), (100.0, False)])
def test_t02_offline_time_is_a_multiple_of_230(tg, offline, ok):
    series = _early(tg, [0.0, 50.0])
    series.early[1].total_playtime = 50.0 + offline
    cell = next(c for c in M.t02(series, tg) if c.key == "Snapshots[01]" and c.group == "offline_multiple")
    assert (cell.residual == 0) is ok


def test_t02_offline_and_t10_dp_read_playtimes_as_float32(tg):
    series = _early(tg, [0.0, 0.0])
    series.early[1].total_playtime = 230.0 * 4348 + 0.03  # float32: 1000040.0 (ulp 0.0625), a multiple of 230
    cell = next(c for c in M.t02(series, tg) if c.key == "Snapshots[01]" and c.group == "offline_multiple")
    assert cell.residual == 0
    series = _early(tg, [50.0, 50.000000001])  # equal as float32: dP = 0, NaN expected
    for f in series.early[1].stats.values():
        f["CurrentGpm"] = math.nan
    cells = M.t10(series, tg)
    assert cells and {(c.group, c.residual) for c in cells} == {("nan", 0)}


@pytest.mark.parametrize("factor, ok", [(0.95, True), (1.05, True), (0.94, False)])
def test_t14_copies_relative_to_the_reference(tg, shipped_db, tolerances, factor, ok):
    sim = copy.deepcopy(_series(shipped_db))
    day = sim.day(405)
    item = next(x for x in day.items if x["Copies"] % 100 == 0 and x["Copies"] > 0 and x["Level"] != x["Copies"])
    ref = item["Copies"]
    item["Copies"] = int(round(ref * factor))
    cells = [c for c in M.evaluate(M.compute(sim, tg, ["T14"]), tolerances, "closed").cells if c.residual != 0]
    assert [(c.group, c.key, c.ref, c.ok) for c in cells] == [("copies", f"405|{item['Key']}", ref, ok)]


# ---------------------------------------------------------------- items: copy accounting, T11-T14 under perturbation

def test_acquired_counts_the_shards_paid_for_evolution():
    info = T.ItemInfo("x", "Common", "Weapon", False, (100, 390, 800), (17.0, 30.0, 45.0))
    assert M._acquired(info, False, 0, 7) == 7
    assert M._acquired(info, True, 1, 7) == 107
    assert M._acquired(info, True, 2, 7) == 7 + 100 + 390


def test_gains_across_an_evolution(tg):
    a, b = copy.deepcopy(tg.shipped.tc[4]), copy.deepcopy(tg.shipped.tc[5])
    a.items = [{"Key": "knife", "Level": 95, "Copies": 95}]
    b.items = [{"Key": "knife", "Level": 1, "Copies": 7}]  # evolved: paid 100 shards, 12 copies gained
    series = T.Series(tc=[a, b], early=[])
    assert M._gains(series, tg, a.tc, b.tc) == {("weapon", "Common"): 12}
    assert tg.items["knife"].shards[0] == 100


def _scaled_copies(series, from_tc, factor, keep_reading=True):
    out = copy.deepcopy(series)
    for s in out.tc:
        if s.tc < from_tc:
            continue
        for x in s.items:
            unevolved = x["Level"] == x["Copies"]
            x["Copies"] = int(round(x["Copies"] * factor))
            if keep_reading and unevolved:
                x["Level"] = x["Copies"]
    return out


def test_fewer_copies_fail_t12_and_t14(tg, shipped_db, tolerances):
    sim = _scaled_copies(_series(shipped_db), 57, 0.9)
    cells = M.compute(sim, tg, ["T12", "T13", "T14"])
    for mode in M.MODES:
        ev = M.evaluate(cells, tolerances, mode)
        assert ev.targets["T12"] == "fail" and ev.targets["T13"] == "pass"
        # 154-405: both ends scaled, and the shards paid for evolution (not scaled) dominate the gain
        assert {c.group for c in ev.failing_cells() if c.target == "T12"} == {"phase_19_57", "phase_57_104",
                                                                               "phase_104_154"}
    closed = M.evaluate(cells, tolerances, "closed")
    assert closed.targets["T14"] == "fail"
    assert all(c.tc >= 57 for c in closed.failing_cells() if c.target == "T14")


def test_heuristic_evolution_level_stays_inside_the_tables(tg, shipped_db):
    """Copies changed without the run's evolved flags: a Level that cannot be an evolution level is rejected
    (no invented (item, level) cells)."""
    sim = _scaled_copies(_series(shipped_db), 154, 0.9, keep_reading=False)
    cells = M.compute(sim, tg, ["T12", "T13"])
    t13 = [c for c in cells if c.target == "T13"]
    assert all(int(c.key.rsplit("|L", 1)[1]) <= len(tg.items[c.key.split("|")[0]].shards) for c in t13)
    shipped = {c.key for c in M.compute(_series(shipped_db), tg, ["T13"])}
    assert {c.key for c in t13} == shipped
    assert any("readings rejected" in c.note for c in cells if c.target == "T12" and c.tc >= 154)


@pytest.mark.parametrize("drop, residual", [((7,), 1), ((7, 8), 2)])
def test_a_rarity_appearing_later_moves_t11(tg, shipped_db, tolerances, drop, residual):
    sim = copy.deepcopy(_series(shipped_db))
    epic = {k for k, i in tg.items.items() if (i.slot, i.rarity) == ("Weapon", "Epic")}
    for s in sim.tc:
        if s.tc in drop:
            s.items = [x for x in s.items if x["Key"] not in epic]
    ev = M.evaluate(M.compute(sim, tg, ["T11"]), tolerances, "closed")
    cells = {c.key: c for c in ev.cells}
    assert (cells["Weapon|Epic"].sim, cells["Weapon|Epic"].ref) == (7 + len(drop), 7)
    assert cells["Weapon|Epic"].residual == residual and cells["Weapon|Epic"].ok is (residual <= 1)
    assert {k for k, c in cells.items() if c.residual != 0} == {"Weapon|Epic"}


@pytest.mark.parametrize("late, residual", [((19,), 1), ((19, 21), 2)])
def test_a_later_evolution_moves_t13(tg, shipped_db, tolerances, late, residual):
    sim = copy.deepcopy(_series(shipped_db))
    for s in sim.tc:
        if s.tc in late:
            for x in s.items:
                if x["Key"] == "knife":
                    x["Copies"] = x["Level"] = x["Copies"] + 100  # not evolved yet: the 100 shards still held
    ev = M.evaluate(M.compute(sim, tg, ["T13"]), tolerances, "closed")
    cell = next(c for c in ev.cells if c.key == "knife|L01")
    assert (cell.ref, cell.residual, cell.ok) == (19, residual, residual <= 1)
    assert [c.key for c in ev.cells if c.residual != 0] == ["knife|L01"]


def test_t17_compares_armor_with_the_design_not_the_fitted_fixture(tg, shipped_db, tolerances, tmp_path):
    assert tg.slot_schedule[0] == (1, 1)  # the fitted fixture: armor 1 before any armor item (unidentifiable)
    written = _series(simio.ingest(M.write_shipped_job(tmp_path / "slots", slot_schedule=tg.slot_schedule)))
    assert written.slots == tg.slot_schedule  # verbatim, nothing synthesized
    sim = copy.deepcopy(_series(shipped_db))
    sim.slots = {tc: (w, 0 if tc < 9 else a) for tc, (w, a) in tg.slot_schedule.items()}
    ev = M.evaluate(M.compute(sim, tg, ["T17"]), tolerances, "closed")
    assert ev.targets["T17"] == "pass" and {c.ref for c in ev.cells if c.group == "armor_before_9"} == {0}
    sim.slots[5] = (sim.slots[5][0], 1)
    ev = M.evaluate(M.compute(sim, tg, ["T17"]), tolerances, "closed")
    assert ev.targets["T17"] == "fail" and [c.key for c in ev.failing_cells()] == ["005|armor"]


# ---------------------------------------------------------------- a state the run never reached fails

def test_a_run_that_never_evolves_fails_t13(tg, shipped_db, tolerances):
    sim = copy.deepcopy(_series(shipped_db))
    for s in sim.tc:
        for x in s.items:
            x["Level"] = x["Copies"]  # the survey reading: never evolved
    for mode in M.MODES:
        ev = M.evaluate(M.compute(sim, tg, ["T13"]), tolerances, mode)
        assert ev.targets["T13"] == "fail"
        assert ev.cells and all(c.sim is None and c.residual is None and c.ok is False for c in ev.cells)


def test_a_run_stuck_on_island_4_fails_t07_closed(tg, shipped_db, tolerances):
    sim = copy.deepcopy(_series(shipped_db))
    for s in sim.tc + sim.early:
        s.location = min(s.location, 4)
    closed = M.evaluate(M.compute(sim, tg, ["T07"]), tolerances, "closed")
    assert closed.targets["T07"] == "fail"
    assert sorted(c.key for c in closed.failing_cells()) == [f"island {i:02d}" for i in range(5, 18)]
    assert M.evaluate(M.compute(sim, tg, ["T07"]), tolerances, "open").targets["T07"] == "fail"


def test_a_run_without_items_fails_t13_and_t14(tg, shipped_db, tolerances):
    sim = copy.deepcopy(_series(shipped_db))
    for s in sim.tc + sim.early:
        s.items = []
    closed = M.evaluate(M.compute(sim, tg, ["T11", "T12", "T13", "T14"]), tolerances, "closed")
    assert {t: closed.targets[t] for t in ("T12", "T13", "T14")} == {"T12": "fail", "T13": "fail", "T14": "fail"}
    assert closed.targets["T11"] == "report"
    assert all(c.ok is False for c in closed.cells if c.target in ("T11", "T13", "T14"))


def test_a_missing_day_snapshot_fails_its_cells(tg, shipped_db, tolerances):
    sim = copy.deepcopy(_series(shipped_db))
    sim.tc = [s for s in sim.tc if s.tc != 30]
    closed = M.evaluate(M.compute(sim, tg, ["T06", "T09"]), tolerances, "closed")
    assert {c.key.split("|")[0] for c in closed.failing_cells()} == {"030"}
    assert closed.targets["T06"] == "fail"


# ---------------------------------------------------------------- metric details at their boundaries

def test_t10_relative_threshold_and_nan_rule(tg, shipped_db, tolerances):
    base = _series(shipped_db)
    j, k = next((j, k) for j in range(1, len(base.early)) for k, f in base.early[j].stats.items()
                if base.early[j].playtime != base.early[j - 1].playtime and abs(f["CurrentGpm"]) > 10)
    a, b = base.early[j - 1], base.early[j]
    dp = float(M.f32(b.playtime)) - float(M.f32(a.playtime))
    g = (b.stats[k]["Value"] - a.stats[k]["Value"]) / dp
    for factor, ok in ((0.5e-5, True), (-0.9e-5, True), (1.5e-5, False), (-2e-5, False)):
        sim = copy.deepcopy(base)
        sim.early[j].stats[k]["CurrentGpm"] = g + factor * abs(g)
        ev = M.evaluate(M.compute(sim, tg, ["T10"]), tolerances, "closed")
        assert [c.key for c in ev.failing_cells()] == ([] if ok else [f"{b.index:02d}|{k}"]), factor
    # NaN exactly where dP = 0: a number there fails
    sim = copy.deepcopy(base)
    i, kn = next((i, k) for i in range(1, len(sim.early)) for k in sim.early[i].stats
                 if sim.early[i].playtime == sim.early[i - 1].playtime)
    sim.early[i].stats[kn]["CurrentGpm"] = 0.0
    ev = M.evaluate(M.compute(sim, tg, ["T10"]), tolerances, "closed")
    assert [(c.group, c.key) for c in ev.failing_cells()] == [("nan", f"{sim.early[i].index:02d}|{kn}")]


@pytest.mark.parametrize("column", ["StatsFromFeaturesFlat", "PercentMultiplier", "TotalFlatValue", "TotalValue"])
def test_t18_checks_each_feature_column(tg, shipped_db, tolerances, column):
    sim = copy.deepcopy(_series(shipped_db))
    sim.day(57).stats["Health"][column] *= 1.000001
    ev = M.evaluate(M.compute(sim, tg, ["T18"]), tolerances, "closed")
    day = next(i for i, s in enumerate(sim.tc) if s.tc == 57)
    assert [c.key for c in ev.failing_cells()] == [f"TimeChunkSnapshots[{day:02d}]|Health"]
    assert ev.failing_cells()[0].residual == 1


def test_t06_tc_5_23_bound_is_the_snapshots_own_island(tg, shipped_db, tolerances):
    base = _series(shipped_db)
    bound = tg.one_clear_reward["Health"]
    day = next(s for s in base.tc if 5 <= s.tc < 24 and bound[s.location] > 0.02 * s.value("Health")
               and bound[s.location] < bound[17])
    b = bound[day.location]
    for extra, ok in ((b, True), (b + 1.0, False)):
        sim = copy.deepcopy(base)
        sim.day(day.tc).stats["Health"]["Value"] += extra
        ev = M.evaluate(M.compute(sim, tg, ["T06"]), tolerances, "closed")
        cell = next(c for c in ev.cells if c.key == f"{day.tc:03d}|Health")
        assert (cell.group, cell.bound, cell.ok) == ("tc_5_23", b, ok)
        assert ev.targets["T06"] == ("pass" if ok else "fail")


def test_t09_closed_counts_nonzero_cells(tg, shipped_db, tolerances):
    base = _series(shipped_db)
    nonzero = [(s.tc, k) for s in base.tc for k, f in s.stats.items() if f["CurrentGpm"] != 0]
    one = copy.deepcopy(base)
    one.day(nonzero[0][0]).stats[nonzero[0][1]]["CurrentGpm"] *= 1.02
    closed = M.evaluate(M.compute(one, tg, ["T09"]), tolerances, "closed")
    assert [(c.group, c.key) for c in closed.failing_cells()] == [("nonzero", f"{nonzero[0][0]:03d}|{nonzero[0][1]}")]
    assert closed.targets["T09"] == "pass"  # >= 90% of the nonzero cells within 1%
    assert M.evaluate(M.compute(one, tg, ["T09"]), tolerances, "open").targets["T09"] == "fail"
    many = copy.deepcopy(base)
    for tc, k in nonzero[: len(nonzero) // 10 + 1]:
        many.day(tc).stats[k]["CurrentGpm"] *= 1.02
    assert M.evaluate(M.compute(many, tg, ["T09"]), tolerances, "closed").targets["T09"] == "fail"


def test_t14_level_must_be_exact(tg, shipped_db, tolerances):
    sim = copy.deepcopy(_series(shipped_db))
    item = sim.day(170).items[0]
    item["Level"] += 1
    closed = M.evaluate(M.compute(sim, tg, ["T14"]), tolerances, "closed")
    assert [(c.group, c.key, c.residual) for c in closed.failing_cells()] == [("level", f"170|{item['Key']}", 1)]


@pytest.mark.parametrize("factor, ok", [(1.04, True), (0.96, True), (1.06, False), (0.94, False)])
def test_t16_movement_speed_is_a_main_stat(tg, shipped_db, tolerances, factor, ok):
    sim = copy.deepcopy(_series(shipped_db))
    sim.day(3).stats["MovementSpeed"]["Value"] *= factor
    ev = M.evaluate(M.compute(sim, tg, ["T16"]), tolerances, "closed")
    cell = next(c for c in ev.cells if c.key == "003|MovementSpeed")
    assert (cell.group, cell.ok) == ("main", ok) and ev.targets["T16"] == ("pass" if ok else "fail")


def test_split_labels_of_t12_and_t13(tg, shipped_db):
    cells = M.compute(_series(shipped_db), tg, ["T12", "T13"])
    t12 = {c.group: c.split for c in cells if c.target == "T12"}
    assert t12 == {"daily": "train", "phase_4_19": "train", "phase_19_57": "train", "phase_57_104": "train",
                   "phase_104_154": "holdout", "phase_154_405": "holdout"}
    t13 = {(tg.items[c.key.split("|")[0]].rarity, c.group, c.split) for c in cells if c.target == "T13"}
    assert {r: (g, sp) for r, g, sp in t13} == {"Common": ("train", "train"), "Uncommon": ("train", "train"),
                                               "Rare": ("train", "train"), "Epic": ("holdout", "holdout"),
                                               "Legendary": ("holdout", "holdout")}
    assert len(t13) == 5


@pytest.mark.parametrize("tc, late", [(3, 0), (4, 1)])
def test_t03_fast_days_only_in_the_day_list(tg, tc, late):
    series = _early(tg, [0.0, 1.0, 2.0])
    series.early[2].tc = tc
    cell = next(c for c in M.t03(series, tg) if c.group == "fast_days_only_in_day_list")
    assert cell.residual == late


@pytest.mark.parametrize("sim, tc, ok", [
    ([30.0, 36.0], 0, True),        # in the P range
    ([40.0, 45.0], 0, True),        # outside the P range, same day
    ([70.0, 75.0], 1, True),        # one day later
    ([130.0, 135.0], 2, False),     # two days later
    (None, None, False),            # never unlocked
])
def test_t07_islands_2_3_within_a_day(tolerances, sim, tc, ok):
    crit = tolerances["targets"]["T07"]["cells"]["by_playtime"]
    cell = M.Cell("T07", "by_playtime", "island 02", sim, [31.23, 37.35], None if sim is None else 0, tc=tc)
    assert M._cell_ok(cell, crit) is ok


@pytest.mark.parametrize("before_p, before_tc, ok", [(45.0, 0, True), (64.0, 1, True), (125.0, 2, False)])
def test_t07_island_2_unlocked_later_in_a_run(tg, shipped_db, tolerances, before_p, before_tc, ok):
    sim = copy.deepcopy(_series(shipped_db))
    for s in sim.early:
        if s.playtime < before_p:
            s.location = 1
    cell = next(c for c in M.evaluate(M.compute(sim, tg, ["T07"]), tolerances, "closed").cells
                if c.key == "island 02")
    assert cell.residual > 0 and cell.tc == before_tc and cell.ok is ok


def test_t07_islands_2_3_unlock_days_of_the_shipped_run(tg, tolerances):
    """The day ranges of islands 2 and 3 are the days of the design's P ranges in the shipped early list."""
    days = tolerances["targets"]["T07"]["cells"]["by_playtime"]["day_ranges"]
    for island in (2, 3):
        assert days[island] == [M.unlock_day_early(tg.shipped, island)] * 2
        lo, hi = tolerances["targets"]["T07"]["cells"]["by_playtime"]["ranges"][island]
        assert all(s.tc == days[island][0] for s in tg.shipped.early if lo - 1 <= s.playtime <= hi + 1)


def test_evolution_reading_prefers_the_runs_record(tg):
    info = tg.items["knife"]
    series = T.Series(tc=[], early=[], items_evolved={(30, "knife"): (True, 3), (31, "knife"): (False, None),
                                                      (32, "knife"): (True, None)})
    assert M._evolution(series, 30, {"Key": "knife", "Level": 1, "Copies": 1}, info) == (True, 3, "run")
    assert M._evolution(series, 31, {"Key": "knife", "Level": 2, "Copies": 7}, info) == (False, 0, "run")
    assert M._evolution(series, 32, {"Key": "knife", "Level": 2, "Copies": 7}, info) == (True, 2, "run")
    assert M._evolution(series, 33, {"Key": "knife", "Level": 2, "Copies": 7}, info) == (True, 2, "heuristic")


def _add_export_file(job: Path, name: str, rows: list[list], manifest_extra: dict | None = None) -> None:
    from tufu_balance.unity import schema

    header = [n for n, _ in schema.CSV_SCHEMAS[name]]
    text = "\n".join([",".join(header)] + [",".join("" if v is None else str(v) for v in r) for r in rows]) + "\n"
    (job / name).write_text(text)
    manifest = json.loads((job / "manifest.json").read_text())
    manifest["export"]["files"][name] = {"sha256": hashlib.sha256(text.encode()).hexdigest(),
                                         "bytes": len(text.encode()), "rows": len(rows)}
    manifest.update(manifest_extra or {})
    (job / "manifest.json").write_text(json.dumps(manifest, indent=1, sort_keys=True) + "\n")


def test_series_reads_the_runs_params_and_day_list_evolution_records(tg, tmp_path):
    job = M.write_shipped_job(tmp_path / "job", slot_schedule={})
    data = json.loads((job / "simulation_data.json").read_text())
    day_index = next(i for i, s in enumerate(data["TimeChunkSnapshots"]) if s["TimeChunkIndex"] == 2)
    early_index = next(i for i, s in enumerate(data["Snapshots"]) if s["TimeChunkIndex"] == 2)
    p06 = {"EarlySnapshotIntervalStarts": [0.0, 30.0], "EarlySnapshotIntervals": [2.0, 6.0]}
    _add_export_file(job, "items_ext.csv", [
        ["TimeChunkSnapshots", day_index, 0, "knife", 1, 7, 1, 1],
        ["Snapshots", early_index, 0, "knife", 2, 7, 0, None],   # the early list's record does not count
        ["TimeChunkSnapshots", day_index, 1, "axe", 3, 3, None, None],
    ], {"job": {"job_id": "job", "params": p06}})
    # snapshots_ext.csv must map the early row too (list, index -> TC)
    ext = (job / "snapshots_ext.csv").read_text().splitlines()
    row = data["Snapshots"][early_index]
    ext.append(",".join(str(v) for v in ["Snapshots", early_index, early_index, 2, repr(float(row["Playtime"])),
                                         repr(float(row["TotalPlaytime"])), int(row["Location"]), "", "", ""]))
    _add_export_file(job, "snapshots_ext.csv", [line.split(",") for line in ext[1:]])
    series = _series(simio.ingest(job))
    assert series.params == p06 and M.early_intervals(series) == ((0.0, 30.0), (2.0, 6.0))
    assert series.slots is None  # snapshots_ext rows without slot values: the run has no slots
    assert series.items_evolved == {(2, "knife"): (True, 1)}


# ---------------------------------------------------------------- parameter layers

def _layers(root: Path, engine: dict, fasttick: dict, **calib) -> Path:
    (root / "defaults").mkdir(parents=True)
    (root / "defaults" / "engine.json").write_text(json.dumps(engine))
    (root / "defaults" / "fasttick.json").write_text(json.dumps(fasttick))
    for name, values in calib.items():
        (root / f"{name}.json").write_text(json.dumps(values))
    return root


ENGINE = {"_comment": "C3b", "SchemaVersion": 1, "RollMode": 0, "Seed": 1, "FastAfkStepMinutes": 120.0,
          "EarlySnapshotIntervals": [1.0, 5.0]}
FASTTICK = {"KillState": 0, "Kappa": 0.245, "KillLossFromAb": False}


def test_param_layers_merge_deterministically(tmp_path):
    root = _layers(tmp_path / "a", ENGINE, FASTTICK, open={"Kappa": 0.25, "KillState": 1},
                   calibrated={"Kappa": 0.2468})
    r = P.resolve(["defaults", "open", "calibrated"], {"Seed": 7}, root=root, csharp=None)
    assert list(r.params) == sorted(r.params)
    assert r.params == {"EarlySnapshotIntervals": [1.0, 5.0], "FastAfkStepMinutes": 120.0, "Kappa": 0.2468,
                        "KillLossFromAb": False, "KillState": 1, "RollMode": 0, "Seed": 7, "SchemaVersion": 1}
    assert r.source["Kappa"] == "calibrated" and r.source["KillState"] == "open" and r.source["Seed"] == "overrides"
    assert r.source["RollMode"] == "defaults" and r.layers == ("defaults", "open", "calibrated", "overrides")
    canonical = json.dumps(r.params, sort_keys=True, separators=(",", ":"), ensure_ascii=False)
    assert r.params_hash == hashlib.sha256(canonical.encode()).hexdigest() == req.params_hash(r.params)
    # the same values spelled differently (key order, 120 for the float 120.0, 7.0 for the int 7) hash the same
    engine2 = dict(reversed(list(ENGINE.items())))
    root2 = _layers(tmp_path / "b", engine2, dict(reversed(list(FASTTICK.items()))),
                    open={"KillState": 1, "Kappa": 0.25, "FastAfkStepMinutes": 120}, calibrated={"Kappa": 0.2468})
    assert P.resolve(["defaults", "open", "calibrated"], {"Seed": 7.0}, root=root2,
                     csharp=None).params_hash == r.params_hash
    assert P.resolve(["defaults", "open", "calibrated"], {"Seed": 8}, root=root, csharp=None).params_hash != r.params_hash
    assert P.resolve(["defaults"], root=root, csharp=None).params_hash == P.resolve(["defaults"], root=root,
                                                                                    csharp=None).params_hash


@pytest.mark.parametrize("layer, message", [
    ({"Kapa": 0.3}, "unknown parameter"),
    ({"KillState": "AfterPass"}, "must be an integer"),
    ({"Kappa": "x"}, "must be a number"),
    ({"KillLossFromAb": 1}, "does not match"),
    ({"EarlySnapshotIntervals": 5.0}, "must be an array"),
])
def test_param_layers_reject_bad_values(tmp_path, layer, message):
    root = _layers(tmp_path, ENGINE, FASTTICK, open=layer)
    with pytest.raises(P.ParamsError, match=message):
        P.resolve(["defaults", "open"], root=root, csharp=None)


def test_a_field_belongs_to_one_module(tmp_path):
    root = _layers(tmp_path, ENGINE, {**FASTTICK, "Seed": 2})
    with pytest.raises(P.ParamsError, match="defined by both"):
        P.resolve(["defaults"], root=root, csharp=None)


CS_PARAMS = textwrap.dedent('''\
    using System;
    namespace Codebase.Simulation.Rebuild
    {
    	public enum RollMode { Carry, Seeded }
    	public enum KillState { StartOfDay = 0, AfterPass = 1 }
    	[Serializable]
    	public partial class SimulationRebuildParams
    	{
    		public int SchemaVersion = 1;
    		public RollMode RollMode = RollMode.Carry;   // comment
    		public int Seed = 1;
    		public float FastAfkStepMinutes = 120f;
    		public float[] EarlySnapshotIntervals = { 1f, 5f };
    		public static int NotAField = 3;
    		public int Ignored() { return 0; }
    	}
    }
''')
CS_FASTTICK = textwrap.dedent('''\
    namespace Codebase.Simulation.Rebuild
    {
    	public partial class SimulationRebuildParams
    	{
    		public KillState KillState = KillState.StartOfDay;
    		public float Kappa = 0.245f;
    		public bool KillLossFromAb = false;
    	}
    }
''')


def test_enum_mapping_and_fields_checked_against_csharp(tmp_path):
    src = tmp_path / "Simulation" / "Rebuild"
    src.mkdir(parents=True)
    (src / "SimulationRebuildParams.cs").write_text(CS_PARAMS)
    (src / "SimulationRebuildParams.FastTick.cs").write_text(CS_FASTTICK)
    cs = P.read_csharp_params(tmp_path / "Simulation")
    assert cs.fields == {"SchemaVersion": "int", "RollMode": "RollMode", "Seed": "int", "FastAfkStepMinutes": "float",
                         "EarlySnapshotIntervals": "float[]", "KillState": "KillState", "Kappa": "float",
                         "KillLossFromAb": "bool"}
    assert cs.enums["KillState"] == {"StartOfDay": 0, "AfterPass": 1} and cs.enums["RollMode"] == {"Carry": 0, "Seeded": 1}
    defaults = P.csharp_defaults(cs)
    assert defaults == {**{k: v for k, v in ENGINE.items() if not k.startswith("_")}, **FASTTICK}
    root = _layers(tmp_path / "layers", ENGINE, FASTTICK, open={"KillState": "AfterPass", "RollMode": "Seeded"})
    r = P.resolve(["defaults", "open"], root=root, csharp=cs)
    assert (r.params["KillState"], r.params["RollMode"]) == (1, 1)
    assert P.check_against_csharp(r.params, cs) == []
    assert P.enum_names(cs, r.params) == {"KillState": "AfterPass", "RollMode": "Seeded"}
    with pytest.raises(P.ParamsError, match="not a member"):
        P.resolve(["defaults", {"KillState": "Never"}], root=root, csharp=cs)
    bad = {**r.params, "KillState": 2, "Extra": 1, "Seed": 1.5}
    del bad["Kappa"]
    problems = P.check_against_csharp(bad, cs)
    assert any(p.startswith("KillState: 2 is not a member") for p in problems)
    assert any(p.startswith("Extra: no such field") for p in problems)
    assert any(p.startswith("Seed: 1.5 does not fit int") for p in problems)
    assert any(p.startswith("Kappa: missing") for p in problems)


def _cs(tmp_path) -> P.CsParams:
    src = tmp_path / "Simulation" / "Rebuild"
    src.mkdir(parents=True, exist_ok=True)
    (src / "SimulationRebuildParams.cs").write_text(CS_PARAMS)
    (src / "SimulationRebuildParams.FastTick.cs").write_text(CS_FASTTICK)
    return P.read_csharp_params(tmp_path / "Simulation")


def test_params_hash_does_not_depend_on_the_csharp_declarations(tmp_path, monkeypatch):
    cs = _cs(tmp_path)
    root = _layers(tmp_path / "a", ENGINE, FASTTICK, open={"FastAfkStepMinutes": 120, "Kappa": 0.25, "Seed": 3.0})
    without, with_cs = (P.resolve(["defaults", "open"], root=root, csharp=c) for c in (None, cs))
    assert without.params == with_cs.params and without.params_hash == with_cs.params_hash
    assert with_cs.params["FastAfkStepMinutes"] == 120.0 and isinstance(with_cs.params["Seed"], int)
    # a float field spelled as an integer in the defaults (or the reverse) is refused once the C# class is known
    root2 = _layers(tmp_path / "b", {**ENGINE, "FastAfkStepMinutes": 120}, FASTTICK, open={"FastAfkStepMinutes": 0.5})
    with pytest.raises(P.ParamsError, match="write 120 as 120.0"):
        P.resolve(["defaults"], root=root2, csharp=cs)
    with pytest.raises(P.ParamsError, match="must be an integer .*decimal point"):
        P.resolve(["defaults", "open"], root=root2, csharp=None)
    root3 = _layers(tmp_path / "c", {**ENGINE, "Seed": 1.0}, FASTTICK)
    with pytest.raises(P.ParamsError, match="write 1.0 as an integer"):
        P.resolve(["defaults"], root=root3, csharp=cs)
    # by default resolve reads this worktree's C# class
    monkeypatch.setattr(P, "read_csharp_params", lambda *a, **kw: cs)
    with pytest.raises(P.ParamsError, match="write 120 as 120.0"):
        P.resolve(["defaults"], root=root2)
    assert P.resolve(["defaults", "open"], root=root).params_hash == with_cs.params_hash


def test_the_first_layer_must_be_the_defaults(tmp_path):
    root = _layers(tmp_path, ENGINE, FASTTICK, open={"Kappa": 0.5})
    for layers in (["open"], [{"Kappa": 0.5}], ["open", "defaults"]):
        with pytest.raises(P.ParamsError, match="first layer must be the defaults"):
            P.resolve(layers, root=root, csharp=None)


def test_parameter_enums_are_found_in_the_whole_assembly(tmp_path, monkeypatch):
    assembly = tmp_path / "Assembly-CSharp"
    sim = assembly / "CodeBase" / "Simulation"
    (sim / "Rebuild").mkdir(parents=True)
    (sim / "Rebuild" / "SimulationRebuildParams.cs").write_text(textwrap.dedent('''\
        namespace Codebase.Simulation.Rebuild
        {
        	public partial class SimulationRebuildParams
        	{
        		public AutoBalanceMode Balance = AutoBalanceMode.Days;
        		public Clash Clash = Clash.A;
        	}
        }
    '''))
    (assembly / "CodeBase" / "Balance").mkdir(parents=True)
    (assembly / "CodeBase" / "Balance" / "Modes.cs").write_text(
        "namespace Codebase.Balance { public enum AutoBalanceMode { Off, Minutes, Days } public enum Clash { A, B } }")
    (assembly / "Other").mkdir()
    (assembly / "Other" / "Clash.cs").write_text("namespace Other { public enum Clash { B, A } }")
    monkeypatch.setattr(P, "ASSEMBLY_SCRIPTS", assembly)
    monkeypatch.setattr(P, "SIMULATION_SCRIPTS", sim)
    cs = P.read_csharp_params(sim)
    assert cs.enums["AutoBalanceMode"] == {"Off": 0, "Minutes": 1, "Days": 2} and cs.ambiguous == {"Clash"}
    assert P.check_against_csharp({"Balance": 2}, cs, complete=False) == []
    assert P.check_against_csharp({"Clash": 0}, cs, complete=False) == [
        "Clash: enum Clash is declared with different members in several places"]
    assert "AutoBalanceMode" not in P.read_csharp_params(sim, enum_roots=[]).enums


def test_resolve_checks_the_merged_object_against_csharp(tmp_path):
    cs = _cs(tmp_path)
    root = _layers(tmp_path / "layers", ENGINE, FASTTICK)
    ok = P.resolve(["defaults", {"KillState": 1}], root=root, csharp=cs)
    assert ok.params["KillState"] == 1
    for layer, message in (({"KillState": 7}, "7 is not a member of KillState"),
                           ({"RollMode": -1}, "-1 is not a member of RollMode"),
                           ({"Seed": True}, "does not match the default")):
        with pytest.raises(P.ParamsError, match=message):
            P.resolve(["defaults", layer], root=root, csharp=cs)
    # without the declarations only the merge rules apply
    assert P.resolve(["defaults", {"KillState": 7}], root=root, csharp=None).params["KillState"] == 7
    # the defaults themselves: a field the class lacks, a missing field, a bool for an int, a bad enum value
    for engine, message in (({**ENGINE, "Extra": 1}, "Extra: no such field"),
                            ({k: v for k, v in ENGINE.items() if k != "Seed"}, "Seed: missing"),
                            ({**ENGINE, "Seed": True}, "True does not fit int"),
                            ({**ENGINE, "RollMode": 5}, "5 is not a member of RollMode")):
        bad = _layers(tmp_path / f"bad{len(list(tmp_path.iterdir()))}", engine, FASTTICK)
        with pytest.raises(P.ParamsError, match=message):
            P.resolve(["defaults"], root=bad, csharp=cs)


def test_csharp_fields_with_attributes_and_strict_default_types(tmp_path):
    src = tmp_path / "Simulation"
    src.mkdir()
    (src / "SimulationRebuildParams.Extra.cs").write_text(textwrap.dedent('''\
        namespace Codebase.Simulation.Rebuild
        {
        	public partial class SimulationRebuildParams
        	{
        		[UnityEngine.Tooltip("x")] public float Hidden = 1f;
        		[JsonProperty("steps")]
        		[Range(0, 9)] public int Steps = 3;
        		public double Ratio = 2;
        		public readonly int NotSerialized = 1;
        		public bool Flag;
        		public string Name;
        	}
        }
    '''))
    cs = P.read_csharp_params(src, enum_roots=[])
    assert cs.fields == {"Hidden": "float", "Steps": "int", "Ratio": "double", "Flag": "bool", "Name": "string"}
    defaults = P.csharp_defaults(cs)
    expected = {"Hidden": 1.0, "Steps": 3, "Ratio": 2.0, "Flag": False, "Name": None}
    assert defaults == expected
    assert {k: type(v) for k, v in defaults.items()} == {k: type(v) for k, v in expected.items()}
    full = _cs(tmp_path / "full")
    assert {k: type(v) for k, v in P.csharp_defaults(full).items()} == {
        "SchemaVersion": int, "RollMode": int, "Seed": int, "FastAfkStepMinutes": float, "EarlySnapshotIntervals": list,
        "KillState": int, "Kappa": float, "KillLossFromAb": bool}
    assert [type(x) for x in P.csharp_defaults(full)["EarlySnapshotIntervals"]] == [float, float]


def test_parameter_enums_declared_under_simulation_win(tmp_path):
    assembly = tmp_path / "Assembly-CSharp"
    sim = assembly / "CodeBase" / "Simulation"
    sim.mkdir(parents=True)
    (sim / "SimulationRebuildParams.cs").write_text(
        "namespace Codebase.Simulation { public enum Mode { A, B, C } "
        "public partial class SimulationRebuildParams { public Mode Mode = Mode.C; } }")
    (assembly / "Other").mkdir()
    (assembly / "Other" / "Mode.cs").write_text("namespace Other { public enum Mode { C, B } }")
    cs = P.read_csharp_params(sim, enum_roots=[assembly])
    assert cs.enums["Mode"] == {"A": 0, "B": 1, "C": 2} and "Mode" not in cs.ambiguous
    assert P.csharp_defaults(cs) == {"Mode": 2}


def test_csharp_parameter_class_of_this_worktree():
    """Once C3b/C3c/C5-C7 add SimulationRebuildParams, the default layers must match its declarations."""
    cs = P.read_csharp_params()
    if cs is None:
        pytest.skip("SimulationRebuildParams is not in CodeBase/Simulation yet (C3b)")
    assert P.check_against_csharp(P.csharp_defaults(cs), cs) == []
    if (P.PARAMS_ROOT / "defaults").is_dir():
        resolved = P.resolve(["defaults"], csharp=cs)
        assert P.check_against_csharp(resolved.params, cs) == []
        assert resolved.params == P.csharp_defaults(cs)


# ---------------------------------------------------------------- fitting tools

def test_grid_ranks_and_keeps_the_runner_up():
    seen = []

    def score_batch(batch):
        seen.append(len(batch))
        return [abs(c["a"] - 2) + (0.5 if c["b"] == "x" else 0.0) for c in batch]

    res = fit.grid({"a": [0, 1, 2, 3], "b": ["x", "y"]}, score_batch=score_batch, batch_size=3,
                   keep=lambda c: not (c["a"] == 3 and c["b"] == "y"))
    assert seen == [3, 3, 1]
    assert res.best.candidate == {"a": 2, "b": "y"} and res.best.score == 0.0
    assert res.second.candidate == {"a": 2, "b": "x"} and res.second.score == 0.5
    assert [r.score for r in res.ranking] == sorted(r.score for r in res.ranking)
    tie = fit.grid([{"k": 1}, {"k": 2}], score=lambda c: 1.0)
    assert [r.candidate["k"] for r in tie.ranking] == [1, 2]
    nan = fit.grid([{"k": 1}, {"k": 2}], score=lambda c: math.nan if c["k"] == 1 else 3.0)
    assert nan.best.candidate == {"k": 2}


def test_grid_second_place_on_a_tie_follows_enumeration_order():
    res = fit.grid({"a": [1, 3]}, score=lambda c: abs(c["a"] - 2))
    assert (res.best.candidate, res.second.candidate) == ({"a": 1}, {"a": 3})


def test_minimize_scalar():
    r = fit.minimize_scalar(lambda x: (x - 0.245) ** 2 + 1.0, (0.0, 1.0), xatol=1e-8)
    assert r.success and abs(r.x - 0.245) < 1e-6 and abs(r.fun - 1.0) < 1e-10 and r.evaluations < 60


def test_batched_simplex_minimises_in_batches():
    calls = []

    def f_batch(points):
        calls.append(len(points))
        return [(1 - p[0]) ** 2 + 100 * (p[1] - p[0] ** 2) ** 2 for p in points]

    r = fit.batched_simplex(f_batch, [-1.2, 1.0], 0.5, max_iter=2000, xatol=1e-8, fatol=1e-12)
    assert r.converged and np.allclose(r.x, [1.0, 1.0], atol=1e-4) and r.fun < 1e-8
    assert calls[0] == 3 and set(calls[1:]) <= {4, 2}
    assert r.batches == len(calls) and r.evaluations == sum(calls) and r.batches < r.evaluations
    again = fit.batched_simplex(lambda pts: [(1 - p[0]) ** 2 + 100 * (p[1] - p[0] ** 2) ** 2 for p in pts],
                                [-1.2, 1.0], 0.5, max_iter=2000, xatol=1e-8, fatol=1e-12)
    assert np.array_equal(again.x, r.x) and again.history == r.history
    boxed = fit.batched_simplex(lambda pts: [(p[0] - 3) ** 2 + (p[1] + 1) ** 2 for p in pts], [0.0, 0.0], 0.5,
                                bounds=[(0, 2), (0, 2)], max_iter=500)
    assert np.allclose(boxed.x, [2.0, 0.0], atol=1e-3)


@pytest.mark.parametrize("table, step, best", [
    ({2.0: -1.0, 3.0: -2.0}, "expand", 3.0),
    ({2.0: -1.0, 3.0: 0.0}, "reflect", 2.0),
    ({2.0: 5.0, 1.5: 4.0}, "outside", 1.0),
    ({2.0: 5.0, 1.5: 6.0, 0.5: 7.0}, "shrink", 1.0),   # outside point worse than the reflection
    ({2.0: 20.0, 0.5: 5.0}, "inside", 1.0),
    ({2.0: 20.0, 0.5: 11.0}, "shrink", 1.0),           # inside point not better than the worst
])
def test_simplex_step_rules(table, step, best):
    """One iteration on [0, 1] with f(1) = 0 (best), f(0) = 10 (worst): centroid 1, reflection 2, expansion 3,
    outside contraction 1.5, inside contraction 0.5 (coefficients 1, 2, 1/2, 1/2)."""
    values = {0.0: 10.0, 1.0: 0.0, **table}
    nm = fit.BatchedNelderMead([0.0], 1.0)
    r = nm.run(lambda pts: [values.get(float(p[0]), 100.0) for p in pts], max_iter=1, xatol=0.0, fatol=0.0)
    assert r.history[0][0] == step and r.x[0] == best
    assert sorted(float(p[0]) for p in nm.simplex) == sorted({"expand": [1.0, 3.0], "reflect": [1.0, 2.0],
                                                              "outside": [1.0, 1.5], "inside": [0.5, 1.0],
                                                              "shrink": [0.5, 1.0]}[step])


def test_least_squares():
    rng = np.random.default_rng(3)
    f = rng.uniform(10, 100, 50)
    assert fit.ratio_fit(0.245 * f, f) == pytest.approx(0.245, rel=1e-14)
    with pytest.raises(ValueError):
        fit.ratio_fit([1.0], [0.0])
    x = np.column_stack([np.ones(30), np.arange(30.0)])
    lin = fit.linear_least_squares(x, 2.0 + 0.5 * np.arange(30.0))
    assert np.allclose(lin.x, [2.0, 0.5]) and lin.rss < 1e-20 and (lin.n, lin.k) == (30, 2)
    d = np.linspace(1, 20, 40)
    y = 1.018 * d ** 0.78
    nl = fit.nonlinear_least_squares(lambda p: p[0] * d ** p[1] - y, [1.0, 1.0], bounds=([0, 0], [5, 2]))
    assert np.allclose(nl.x, [1.018, 0.78], atol=1e-6)


def test_block_bootstrap():
    rng = np.random.default_rng(0)
    blocks = {tc: list(0.245 * f + rng.normal(0, 1, 5)) for tc, f in zip(range(10), rng.uniform(50, 60, 10))}
    a = fit.block_bootstrap(blocks, lambda xs: float(np.mean(xs)), n_boot=500, seed=11)
    b = fit.block_bootstrap(blocks, lambda xs: float(np.mean(xs)), n_boot=500, seed=11)
    assert np.array_equal(a.samples, b.samples) and (a.lo, a.hi) == (b.lo, b.hi)
    assert a.lo <= a.estimate <= a.hi and a.blocks == 10 and a.level == 0.9
    # replicate i takes the blocks default_rng(seed).integers(0, n, (n_boot, n))[i] (default seed 0)
    code = lambda xs: float(sum(x * 10 ** k for k, x in enumerate(xs)))
    for seed, kw in ((0, {}), (5, {"seed": 5})):
        picks = np.random.default_rng(seed).integers(0, 4, size=(3, 4))
        got = fit.block_bootstrap([[i] for i in range(4)], code, n_boot=3, **kw).samples
        assert list(got) == [code(list(row)) for row in picks]
    c = fit.block_bootstrap(blocks, lambda xs: float(np.mean(xs)), n_boot=500, seed=12)
    assert not np.array_equal(a.samples, c.samples)
    # blocks are resampled whole: identical blocks give a zero-width interval even with spread inside a block
    same = fit.block_bootstrap([[1.0, 3.0]] * 6, lambda xs: float(np.mean(xs)), n_boot=200)
    assert same.width == 0.0 and same.estimate == 2.0
    # a block that differs moves the replicates: the interval is not degenerate
    mixed = fit.block_bootstrap([[1.0], [1.0], [1.0], [9.0]], lambda xs: float(np.mean(xs)), n_boot=400)
    assert mixed.width > 0
    with pytest.raises(ValueError):
        fit.block_bootstrap([[], []], len)
    # a mapping is read in key order, not insertion order
    shuffled = {tc: blocks[tc] for tc in reversed(list(blocks))}
    assert np.array_equal(fit.block_bootstrap(shuffled, lambda xs: float(np.mean(xs[:7])), n_boot=50, seed=1).samples,
                          fit.block_bootstrap(blocks, lambda xs: float(np.mean(xs[:7])), n_boot=50, seed=1).samples)


def test_aic():
    assert fit.aic(10.0, 20, 2) == pytest.approx(20 * math.log(0.5) + 4)
    assert fit.aic_from_residuals([1.0, -1.0, 2.0], 1) == pytest.approx(3 * math.log(2.0) + 2)
    rows = fit.compare_aic({"NnTour": (8.0, 50, 2), "FarmedTime": (9.0, 50, 1)})
    assert [r.name for r in rows] == sorted(["NnTour", "FarmedTime"], key=lambda n: fit.aic(*{
        "NnTour": (8.0, 50, 2), "FarmedTime": (9.0, 50, 1)}[n]))
    assert rows[0].delta == 0.0 and sum(r.weight for r in rows) == pytest.approx(1.0)
    assert rows[1].delta == pytest.approx(rows[1].aic - rows[0].aic)
    with pytest.raises(ValueError):
        fit.compare_aic({"a": (1.0, 10, 1), "b": (1.0, 11, 1)})


# ---------------------------------------------------------------- reports

REPORT_SCRIPT = textwrap.dedent('''\
    import sys, warnings
    warnings.simplefilter("ignore")
    from pathlib import Path
    from tufu_balance.calibrate import metrics as M, report as R, targets as T, params as P
    from tufu_balance.unity import simio
    out, layers = Path(sys.argv[1]), Path(sys.argv[2])
    con = T.connect_ro(sys.argv[3])
    tg = T.build(con, slot_schedule=T.load_slot_schedule())
    db = simio.ingest(M.write_shipped_job(out / "job" / "shipped"))
    tol = M.load_tolerances()
    evs = [M.evaluate_run(db, tg, mode, tol) for mode in ("open", "closed")]
    resolved = P.resolve(["defaults", "open"], root=layers, csharp=None)
    hyp = [{"stage": "open", "family": "KillState", "rank": 2, "hypothesis": "AfterPass", "score_train": 6.0},
           {"stage": "open", "family": "KillState", "rank": 1, "hypothesis": "StartOfDay", "score_train": 4.0,
            "post_hoc": True}]
    print(R.write(out / "reports", "harness", evaluations=evs, params=resolved, hypotheses=hyp,
                  sensitivity=[{"probe": "dps", "scale": 1.01, "metric": "T05.bad", "value": 3}],
                  inputs={"sim_db_sha256": R.sha256_file(db)}))
''')


def test_reports_are_byte_identical_across_runs(tmp_path, db_path):
    layers = _layers(tmp_path / "layers", ENGINE, FASTTICK, open={"Kappa": 0.25})
    dirs = []
    for seed in ("1", "2"):
        out = tmp_path / f"run{seed}"
        env = {**os.environ, "PYTHONHASHSEED": seed}
        proc = subprocess.run([sys.executable, "-c", REPORT_SCRIPT, str(out), str(layers), str(db_path)],
                              cwd=BALANCE, env=env, capture_output=True, text=True)
        assert proc.returncode == 0, proc.stderr[-2000:]
        dirs.append(Path(proc.stdout.strip()))
    a, b = dirs
    assert a.name == b.name and a.parent.name == "harness"
    names = sorted(p.name for p in a.iterdir())
    assert names == sorted(["targets.csv", "residuals.csv", "params.csv", "hypotheses.csv", "sensitivity.csv",
                            "evolution_margin.csv", "manifest.json"])
    for name in names:
        assert (a / name).read_bytes() == (b / name).read_bytes(), name
    manifest = json.loads((a / "manifest.json").read_text())
    assert manifest["run_id"] == a.name and manifest["stage"] == "harness"
    assert all(manifest["files"][n] == R.sha256_file(a / n) for n in names if n != "manifest.json")
    residuals = (a / "residuals.csv").read_text().splitlines()
    assert residuals[0] == ",".join(R.RESIDUAL_COLUMNS) and len(residuals) > 20000
    hyp = (a / "hypotheses.csv").read_text().splitlines()
    assert hyp[1].split(",")[2:4] == ["1", "StartOfDay"]
    assert (a / "evolution_margin.csv").read_text() == ",".join(R.EVOLUTION_MARGIN_COLUMNS) + "\n"


def test_report_formatting():
    assert [R.fmt(x) for x in (None, True, 0.1, math.nan, -math.inf, 3, [1, 2.5], {"b": 1, "a": None})] == [
        "", "1", "0.1", "NaN", "-Infinity", "3", "[1,2.5]", '{"a":null,"b":1}']
    with pytest.raises(ValueError):
        R.csv_text(("a",), [{"b": 1}])


# ---------------------------------------------------------------- driver and cache (fake backend)

IDENTITY = RC.CodeIdentity(code_rev="c0ffee", trees={"Assets": "t1", "ProjectSettings": "t2", "Packages": "t3"},
                           unity="6000.6.3f1 (x)", dlls={"Unity.Mathematics.dll": "d1", "Unity.Burst.dll": "d2"})
TEMPLATE = req.make_request([req.empty_job("template")], sandbox={"expected_ready_spawners": None})


class FakeBackend:
    """Writes what the launcher and the editor would, in the editor's order: per job ``manifest.json`` (job
    finished), then ``asset_guard.json`` (the editor's guard after the job), then its result; the request
    stops at the first failing job. Run mode puts the editor's results in ``editor_report.json``
    (``job_results``), serve mode in ``launcher.json`` (``result.jobs``). ``crash``: Unity dies (killed on
    a stall) while that job runs, before its manifest; ``asset_guard``: the job's manifest says 0 but the
    asset guard after it fails, so the editor makes it exit 2 and ends the request (the launcher's own
    guard sees nothing); ``guard``: the next N runs have a launcher guard problem."""

    name = "fake"

    def __init__(self, root: Path, fail: dict[str, int] | None = None, refuse: int = 0,
                 crash: dict[str, int] | None = None, guard: int = 0, asset_guard: dict[str, int] | None = None,
                 serve: bool = False):
        self.root, self.fail, self.refuse = root, dict(fail or {}), refuse
        self.crash, self.guard, self.asset_guard = dict(crash or {}), guard, dict(asset_guard or {})
        self.serve = serve
        self.requests: list[list[str]] = []

    def identity(self):
        return IDENTITY

    def submit(self, request):
        self.requests.append([j["job_id"] for j in request["jobs"]])
        if self.refuse:
            self.refuse -= 1
            raise launcher.LauncherError("lock timeout", launcher.EXIT_TIMEOUT)
        key = runner.run_key(request, code_rev=IDENTITY.code_rev, trees=IDENTITY.trees, unity=IDENTITY.unity,
                             dlls=IDENTITY.dlls)
        run_dir = self.root / "runs" / key
        if run_dir.exists():
            shutil.rmtree(run_dir)
        run_dir.mkdir(parents=True)
        (run_dir / "request.json").write_text(req.dumps(request))
        code, reason, results, crashed = 0, None, [], False
        for job in request["jobs"]:
            jid = job["job_id"]
            (run_dir / jid).mkdir(exist_ok=True)
            if self.crash.get(jid, 0) > 0:
                self.crash[jid] -= 1
                code, reason, crashed = launcher.EXIT_TIMEOUT, "heartbeat progress stalled for 120 s at 1", True
                break
            code = 1 if self.fail.get(jid, 0) > 0 else 0
            if code:
                self.fail[jid] -= 1
            (run_dir / jid / "manifest.json").write_text(json.dumps({"job_id": jid, "exit_code": code,
                                                                     "params_hash": job.get("params_hash")}))
            guard_ok = True
            if self.asset_guard.get(jid, 0) > 0:
                self.asset_guard[jid] -= 1
                guard_ok = False
                code = code or launcher.EXIT_GUARD
            (run_dir / jid / "asset_guard.json").write_text(json.dumps({"job_id": jid,
                                                                        "after_job": {"ok": guard_ok}}))
            results.append({"job_id": jid, "exit_code": code, "asset_guard_ok": guard_ok})
            if code:
                break
        if not crashed and not self.serve:
            (run_dir / "editor_report.json").write_text(json.dumps({"exit_code": code, "job_results": results}))
        problems = []
        if self.guard:
            self.guard -= 1
            problems, code = ["real save file changed: game_data.bin"], launcher.EXIT_GUARD
        report = {"exit_code": code, "guard_problems": problems, "reason": reason}
        if self.serve:
            report["result"] = {"exit_code": code, "jobs": results}
        (run_dir / "launcher.json").write_text(json.dumps(report))
        return launcher.RunResult(run_dir, code, request, report)


def _jobs(n, prefix="j"):
    return [req.make_job(f"{prefix}{i}", "replay", day=11, params={"FastAfkStepMinutes": 60.0 + i}) for i in range(n)]


def test_driver_cache_hit_does_not_start_unity(tmp_path):
    backend = FakeBackend(tmp_path)
    drv = D.Driver(backend, cache=RC.RunCache(tmp_path), template=TEMPLATE, log=lambda *_: None)
    first = drv.run(_jobs(3))
    assert [r.status for r in first] == ["ok"] * 3 and backend.requests == [["j0", "j1", "j2"]]
    again = D.Driver(backend, cache=RC.RunCache(tmp_path), template=TEMPLATE, log=lambda *_: None).run(_jobs(3))
    assert [r.status for r in again] == ["cached"] * 3 and backend.requests == [["j0", "j1", "j2"]]
    assert [r.job_dir for r in again] == [r.job_dir for r in first]
    # one changed parameter is a miss for that job only
    jobs = _jobs(3)
    jobs[1] = req.make_job("j1", "replay", day=11, params={"FastAfkStepMinutes": 90.0})
    mixed = D.Driver(backend, cache=RC.RunCache(tmp_path), template=TEMPLATE, log=lambda *_: None).run(jobs)
    assert [r.status for r in mixed] == ["cached", "ok", "cached"] and backend.requests[-1] == ["j1"]
    # a changed manifest invalidates the index entry
    (first[0].job_dir / "manifest.json").write_text(json.dumps({"exit_code": 0, "edited": True}))
    assert RC.RunCache(tmp_path).lookup(first[0].job_key) is None


def test_job_key_is_the_launchers_run_key_of_the_job_alone(tmp_path):
    job = _jobs(1)[0]
    alone = RC.single_job_request(job, TEMPLATE)
    assert RC.job_key(job, TEMPLATE, IDENTITY) == runner.run_key(
        alone, code_rev=IDENTITY.code_rev, trees=IDENTITY.trees, unity=IDENTITY.unity, dlls=IDENTITY.dlls)
    other = RC.CodeIdentity(**{**IDENTITY.__dict__, "code_rev": "beef"})
    assert RC.job_key(job, TEMPLATE, other) != RC.job_key(job, TEMPLATE, IDENTITY)
    slow = copy.deepcopy(TEMPLATE)
    slow["unity"]["simulation_speed"] = 9  # speed-only: same key
    assert RC.single_job_request(job, slow)["unity"] == slow["unity"]  # a job run alone keeps the template's
    assert RC.job_key(job, slow, IDENTITY) == RC.job_key(job, TEMPLATE, IDENTITY)
    # a single-job run of the launcher is a hit without an index entry
    FakeBackend(tmp_path).submit(alone)
    assert RC.RunCache(tmp_path).lookup(RC.job_key(job, TEMPLATE, IDENTITY)).job_id == job["job_id"]


def test_driver_batches_jobs(tmp_path):
    backend = FakeBackend(tmp_path)
    drv = D.Driver(backend, cache=RC.RunCache(tmp_path), template=TEMPLATE, batch_size=12, log=lambda *_: None)
    out = drv.run(_jobs(30))
    assert [len(b) for b in backend.requests] == [12, 12, 6] and all(r.ok for r in out)
    assert [r.job_id for r in out] == [f"j{i}" for i in range(30)] and drv.submitted == backend.requests


def test_driver_retries_a_failed_job_once_alone(tmp_path):
    backend = FakeBackend(tmp_path, fail={"j2": 1})
    out = D.Driver(backend, cache=RC.RunCache(tmp_path), template=TEMPLATE, log=lambda *_: None).run(_jobs(5))
    # j0, j1 ran, j2 failed (the request ended there), j2 again alone, then j3, j4 (not run yet, no attempt)
    assert backend.requests == [["j0", "j1", "j2", "j3", "j4"], ["j2"], ["j3", "j4"]]
    assert [r.status for r in out] == ["ok"] * 5
    assert [r.attempts for r in out] == [1, 1, 2, 1, 1]
    assert out[2].run_dirs[-1].name == out[2].job_key  # the retry is the job alone: its run_key is the job key
    backend2 = FakeBackend(tmp_path / "b", fail={"j1": 5})
    out2 = D.Driver(backend2, cache=RC.RunCache(tmp_path / "b"), template=TEMPLATE, log=lambda *_: None).run(_jobs(3))
    assert backend2.requests == [["j0", "j1", "j2"], ["j1"], ["j2"]]
    assert [r.status for r in out2] == ["ok", "failed", "ok"] and out2[1].attempts == 2
    assert RC.RunCache(tmp_path / "b").lookup(out2[1].job_key) is None


@pytest.mark.parametrize("crash, requests, attempts", [
    ("j2", [["j0", "j1", "j2", "j3", "j4", "j5"], ["j2"], ["j3", "j4", "j5"]], [1, 1, 2, 1, 1, 1]),
    ("j0", [["j0", "j1", "j2", "j3", "j4", "j5"], ["j0"], ["j1", "j2", "j3", "j4", "j5"]], [2, 1, 1, 1, 1, 1]),
])
def test_driver_after_a_crash_keeps_finished_jobs_and_retries_only_the_crashed_one(tmp_path, crash, requests,
                                                                                   attempts):
    backend = FakeBackend(tmp_path, crash={crash: 1})
    out = D.Driver(backend, cache=RC.RunCache(tmp_path), template=TEMPLATE, log=lambda *_: None).run(_jobs(6))
    assert backend.requests == requests
    assert [r.status for r in out] == ["ok"] * 6 and [r.attempts for r in out] == attempts
    first = out[0].run_dirs[0]
    assert json.loads((first / "launcher.json").read_text())["reason"]
    finished = [r for r in out if r.run_dirs[0] == first and r.attempts == 1 and r.job_dir.parent == first]
    assert [r.job_id for r in finished] == [f"j{i}" for i in range(int(crash[1:]))]  # cached from the crashed run
    assert all(RC.RunCache(tmp_path).lookup(r.job_key) is not None for r in out)


def test_driver_crash_twice_fails_that_job_only(tmp_path):
    backend = FakeBackend(tmp_path, crash={"j1": 5})
    out = D.Driver(backend, cache=RC.RunCache(tmp_path), template=TEMPLATE, log=lambda *_: None).run(_jobs(3))
    assert backend.requests == [["j0", "j1", "j2"], ["j1"], ["j2"]]
    assert [(r.status, r.attempts) for r in out] == [("ok", 1), ("failed", 2), ("ok", 1)]
    assert out[1].exit_code == launcher.EXIT_TIMEOUT


@pytest.mark.parametrize("guard, statuses", [(1, [("ok", 2)] * 3), (2, [("failed", 2)] * 3)])
def test_driver_discards_a_batch_with_a_guard_problem(tmp_path, guard, statuses):
    backend = FakeBackend(tmp_path, guard=guard)
    out = D.Driver(backend, cache=RC.RunCache(tmp_path), template=TEMPLATE, log=lambda *_: None).run(_jobs(3))
    assert backend.requests == [["j0", "j1", "j2"], ["j0", "j1", "j2"]]
    assert [(r.status, r.attempts) for r in out] == statuses
    run_dir = out[0].run_dirs[-1]  # the same request twice: the launcher reuses its run directory
    assert RC.run_ok(run_dir) is (guard == 1)
    assert all((RC.RunCache(tmp_path).lookup(r.job_key) is None) is (guard == 2) for r in out)
    if guard == 2:
        with pytest.raises(ValueError):
            RC.RunCache(tmp_path).store("k", run_dir / "j0", run_dir, "j0")


def test_driver_retries_each_job_of_a_refused_batch_alone(tmp_path):
    backend = FakeBackend(tmp_path, refuse=1)
    out = D.Driver(backend, cache=RC.RunCache(tmp_path), template=TEMPLATE, log=lambda *_: None).run(_jobs(3))
    assert backend.requests == [["j0", "j1", "j2"], ["j0"], ["j1"], ["j2"]]
    assert [(r.status, r.attempts) for r in out] == [("ok", 2)] * 3


def test_driver_requeued_jobs_go_first(tmp_path):
    backend = FakeBackend(tmp_path, fail={"j1": 1})
    out = D.Driver(backend, cache=RC.RunCache(tmp_path), template=TEMPLATE, batch_size=3,
                   log=lambda *_: None).run(_jobs(6))
    assert backend.requests == [["j0", "j1", "j2"], ["j1"], ["j2", "j3", "j4"], ["j5"]]
    assert all(r.ok for r in out)


@pytest.mark.parametrize("serve", [False, True])
def test_driver_retries_a_job_whose_asset_guard_failed_after_its_manifest(tmp_path, serve):
    """The editor writes manifest exit 0, then its asset guard fails: the job exits 2 and the request ends,
    while the launcher's own guard (real save, worktree) sees nothing. That job is the culprit, not the
    next one, and it is never cached."""
    backend = FakeBackend(tmp_path, asset_guard={"j1": 1}, serve=serve)
    first_run = []
    drv = D.Driver(backend, cache=RC.RunCache(tmp_path), template=TEMPLATE, log=lambda *_: None)
    real_submit = backend.submit

    def submit(request):
        result = real_submit(request)
        first_run.append(result.run_dir)
        return result

    backend.submit = submit
    out = drv.run(_jobs(3))
    assert backend.requests == [["j0", "j1", "j2"], ["j1"], ["j2"]]
    assert [(r.status, r.attempts) for r in out] == [("ok", 1), ("ok", 2), ("ok", 1)]
    run0 = first_run[0]
    assert json.loads((run0 / "j1" / "manifest.json").read_text())["exit_code"] == 0
    assert json.loads((run0 / "launcher.json").read_text())["guard_problems"] == []
    assert RC.job_verdict(run0, "j1") == (True, launcher.EXIT_GUARD) and RC.job_verdict(run0, "j2") == (False, None)
    assert out[1].job_dir.parent != run0 and out[1].run_dirs[-1].name == out[1].job_key
    with pytest.raises(ValueError):
        RC.RunCache(tmp_path).store("k", run0 / "j1", run0, "j1")
    # twice: that job fails, the others are done
    backend2 = FakeBackend(tmp_path / "b", asset_guard={"j1": 2}, serve=serve)
    out2 = D.Driver(backend2, cache=RC.RunCache(tmp_path / "b"), template=TEMPLATE, log=lambda *_: None).run(_jobs(3))
    assert [(r.status, r.attempts, r.exit_code) for r in out2] == [("ok", 1, 0), ("failed", 2, 2), ("ok", 1, 0)]
    assert RC.RunCache(tmp_path / "b").lookup(out2[1].job_key) is None


def _single_run(tmp_path, *, launcher_exit=0, job_exit=0, guard_ok=True, editor_exit=0, problems=(), keys=True):
    job = _jobs(1)[0]
    key = RC.job_key(job, TEMPLATE, IDENTITY)
    run_dir = tmp_path / "runs" / key
    (run_dir / job["job_id"]).mkdir(parents=True)
    (run_dir / "request.json").write_text(req.dumps(RC.single_job_request(job, TEMPLATE)))
    (run_dir / job["job_id"] / "manifest.json").write_text(json.dumps({"exit_code": job_exit}))
    if guard_ok is not None:
        (run_dir / job["job_id"] / "asset_guard.json").write_text(json.dumps({"after_job": {"ok": guard_ok}}))
    (run_dir / "editor_report.json").write_text(json.dumps({"job_results": [{"job_id": job["job_id"],
                                                                             "exit_code": editor_exit}]}))
    report = {"exit_code": launcher_exit}
    if keys:
        report["guard_problems"] = list(problems)
    (run_dir / "launcher.json").write_text(json.dumps(report))
    return key, run_dir, job["job_id"]


@pytest.mark.parametrize("files, verdict", [
    ({}, (False, None)),
    ({"manifest": 0}, (True, None)),                                   # died before the asset guard
    ({"manifest": 0, "guard": True}, (True, 0)),
    ({"manifest": 0, "guard": False}, (True, launcher.EXIT_GUARD)),    # died before its result was recorded
    ({"manifest": 1, "guard": True}, (True, 1)),
    ({"manifest": 0, "guard": True, "editor": 2}, (True, 2)),
    ({"manifest": 0, "guard": True, "served": 3}, (True, 3)),
    ({"manifest": 0, "guard": True, "served": 0, "editor": 2}, (True, 0)),  # serve mode: the served result
    ({"editor": 5}, (True, 5)),                                        # refused before anything ran
])
def test_job_verdict(tmp_path, files, verdict):
    job = tmp_path / "j0"
    job.mkdir()
    if "manifest" in files:
        (job / "manifest.json").write_text(json.dumps({"exit_code": files["manifest"]}))
    if "guard" in files:
        (job / "asset_guard.json").write_text(json.dumps({"after_job": {"ok": files["guard"]}}))
    if "editor" in files:
        (tmp_path / "editor_report.json").write_text(json.dumps({"job_results": [
            {"job_id": "j0", "exit_code": files["editor"]}]}))
    if "served" in files:
        (tmp_path / "launcher.json").write_text(json.dumps({"guard_problems": [], "result": {"jobs": [
            {"job_id": "j0", "exit_code": files["served"]}]}}))
    assert RC.job_verdict(tmp_path, "j0") == verdict


@pytest.mark.parametrize("kw, hit", [
    ({}, True),
    ({"launcher_exit": 2, "problems": ["real save file changed"]}, False),
    ({"launcher_exit": 2}, False),                  # a failed single-job run is not a hit
    ({"job_exit": 1}, False),
    ({"guard_ok": False}, False),
    ({"guard_ok": None}, False),                    # Unity died before the asset guard ran
    ({"editor_exit": 2}, False),
    ({"keys": False}, False),                       # a launcher.json without guard_problems is not clean
])
def test_run_cache_direct_hit_needs_a_clean_valid_run(tmp_path, kw, hit):
    key, run_dir, jid = _single_run(tmp_path, **kw)
    found = RC.RunCache(tmp_path).lookup(key)
    assert (found is not None) is hit
    if hit:
        assert (found.job_dir, found.job_id) == (run_dir / jid, jid)
    assert RC.run_ok(run_dir) is (kw.get("keys", True) and not kw.get("problems"))


@pytest.mark.parametrize("kw", [{"job_exit": 1}, {"guard_ok": False}, {"editor_exit": 2}, {"keys": False},
                                {"problems": ["x"]}])
def test_run_cache_index_refuses_and_drops_invalid_jobs(tmp_path, kw):
    key, run_dir, jid = _single_run(tmp_path / "a")
    cache = RC.RunCache(tmp_path / "b")
    cache.store(key, run_dir / jid, run_dir, jid)
    assert cache.lookup(key).job_dir == run_dir / jid
    _, bad_dir, _ = _single_run(tmp_path / "c", **kw)
    with pytest.raises(ValueError):
        cache.store(key, bad_dir / jid, bad_dir, jid)
    if "job_exit" in kw:
        return
    # the indexed run turns invalid with the same manifest bytes (guard, editor or launcher record changed)
    for name in ("asset_guard.json",):
        if (bad_dir / jid / name).exists():
            shutil.copy(bad_dir / jid / name, run_dir / jid / name)
    for name in ("editor_report.json", "launcher.json"):
        shutil.copy(bad_dir / name, run_dir / name)
    assert cache.lookup(key) is None


def test_default_backend_prefers_the_resident_editor(monkeypatch):
    monkeypatch.setattr(launcher, "serve_status", lambda run_tree=None: {"running": True, "code_rev": "abc"})
    submitted = []
    monkeypatch.setattr(launcher, "serve_submit", lambda request, **kw: submitted.append(request) or "served")
    backend = D.default_backend()
    assert isinstance(backend, D.ServeBackend) and backend.code_rev == "abc"
    assert backend.submit({"jobs": []}) == "served" and submitted == [{"jobs": []}]
    monkeypatch.setattr(launcher, "serve_status", lambda run_tree=None: {"running": False})
    batch = D.default_backend()
    assert isinstance(batch, D.BatchBackend) and batch.rev == runner.head(runner.WORKTREE)


# ---------------------------------------------------------------- driver over the real launcher (fake editor)

FAKE_EDITOR = textwrap.dedent('''\
    import json, os, sys, time
    from pathlib import Path
    args = sys.argv[1:]
    arg = lambda name: args[args.index(name) + 1] if name in args else None
    project = Path(arg("-projectPath"))
    Path(arg("-logFile")).write_text("fake\\n")
    beat = project / "Temp/TufuSimulation/heartbeat.json"
    beat.parent.mkdir(parents=True, exist_ok=True)
    r = json.loads(Path(arg("-tufuSimRequest")).read_text())
    log = Path(os.environ["FAKE_LAUNCH_LOG"])
    with log.open("a") as f:
        f.write(" ".join(j["job_id"] for j in r["jobs"]) + "\\n")
    code = 0
    for i, job in enumerate(r["jobs"]):
        beat.write_text(json.dumps({"pid": os.getpid(), "phase": "play:Job", "progress": str(i)}))
        time.sleep(0.2)
        marker = log.parent / ("failed-" + job["job_id"])
        code = 1 if job["job_id"] in os.environ.get("FAKE_FAIL_ONCE", "").split() and not marker.exists() else 0
        if code:
            marker.write_text("x")
        d = Path(r["out_dir"]) / job["job_id"]
        d.mkdir(parents=True, exist_ok=True)
        (d / "manifest.json").write_text(json.dumps({"job_id": job["job_id"], "exit_code": code}))
        (d / "asset_guard.json").write_text(json.dumps({"job_id": job["job_id"], "after_job": {"ok": True}}))
        if code:
            break
    sys.exit(code)
''')


def _git(cwd, *args):
    subprocess.run(["git", "-C", str(cwd), *args], check=True, capture_output=True)


@pytest.fixture
def fake_run_tree(tmp_path, monkeypatch):
    tree = tmp_path / "tree"
    project = tree / "Recovery" / "StudyProject"
    for sub in ("Assets/Plugins", "ProjectSettings", "Packages", "Library"):
        (project / sub).mkdir(parents=True)
    (tree / ".gitignore").write_text("/Balance/out/\n/Recovery/StudyProject/Library/\n/Recovery/StudyProject/Temp/\n")
    (project / "Assets" / "a.cs").write_text("// a\n")
    (project / "ProjectSettings" / "ProjectSettings.asset").write_text("PlayerSettings:\n  companyName: Fake\n  productName: Fake\n")
    (project / "Packages" / "manifest.json").write_text("{}\n")
    for name in runner.PROTECTED_DLLS:
        (project / "Assets" / "Plugins" / name).write_bytes(name.encode())
    _git(tree, "init", "-q")
    _git(tree, "-c", "user.name=t", "-c", "user.email=t@t", "add", "-A")
    _git(tree, "-c", "user.name=t", "-c", "user.email=t@t", "commit", "-qm", "init")
    real_processes = launcher.unity_processes
    main = (runner.MAIN_CHECKOUT / runner.PROJECT_SUBDIR).resolve()
    monkeypatch.setattr(launcher, "unity_processes",
                        lambda p: [] if Path(p).resolve() == main else real_processes(p))
    monkeypatch.setattr(launcher, "DLL_RULES", {
        name: {"expected": runner.sha256_file(project / "Assets" / "Plugins" / name), "damaged": "0" * 64}
        for name in runner.PROTECTED_DLLS})
    unity = tmp_path / "FakeUnity"
    unity.write_text(f"#!{sys.executable}\n" + FAKE_EDITOR)
    unity.chmod(0o755)
    (tmp_path / "real" / "save").mkdir(parents=True)
    monkeypatch.setenv("FAKE_LAUNCH_LOG", str(tmp_path / "launches.log"))
    opts = launcher.Options(unity=unity, startup_timeout_s=10, stall_timeout_s=5, net_sample_s=0.5,
                            worker_exit_wait_s=1, lock_wait_s=5)
    paths = launcher.ProtectedPaths(save_dir=tmp_path / "real" / "save", plist=tmp_path / "real" / "x.plist", domain=None)
    backend = D.BatchBackend(opts=opts, worktree=tree, run_tree=tree, out_root=tmp_path / "out", paths=paths,
                             log=lambda *_: None)
    return {"backend": backend, "out": tmp_path / "out", "log": tmp_path / "launches.log"}


def test_job_key_with_the_auto_readiness_table_is_the_launchers_run_key(fake_run_tree):
    """sandbox.expected_ready_spawners = "auto" (the production default): the key resolves the table name the
    way the launcher does before its run_key."""
    if not launcher.balance_db().exists():
        pytest.skip("no extracted database")
    ft = fake_run_tree
    template = req.make_request([req.empty_job("t")], sandbox={"identity": "TufuSimFakeTest"})
    assert template["sandbox"]["expected_ready_spawners"] == req.EXPECTED_READY_AUTO
    job = _jobs(1)[0]
    out = D.Driver(ft["backend"], cache=RC.RunCache(ft["out"]), template=template, log=lambda *_: None).run([job])
    assert out[0].status == "ok" and out[0].run_dirs[-1].name == out[0].job_key
    request = json.loads((out[0].run_dirs[-1] / "request.json").read_text())
    assert request["sandbox"]["expected_ready_spawners"] == launcher.expected_ready_table("A")[0]
    assert RC.job_key(job, template, ft["backend"].identity(), resolve=lambda r: r) != out[0].job_key


def test_driver_over_the_launcher_with_a_fake_editor(fake_run_tree, monkeypatch):
    ft = fake_run_tree
    monkeypatch.setenv("FAKE_FAIL_ONCE", "j1")
    template = req.make_request([req.empty_job("t")], sandbox={"identity": "TufuSimFakeTest",
                                                              "expected_ready_spawners": None})
    jobs = _jobs(3)
    out = D.Driver(ft["backend"], cache=RC.RunCache(ft["out"]), template=template, log=lambda *_: None).run(jobs)
    assert [r.status for r in out] == ["ok"] * 3 and [r.attempts for r in out] == [1, 2, 1]
    assert ft["log"].read_text().splitlines() == ["j0 j1 j2", "j1", "j2"]
    assert out[1].run_dirs[-1].name == out[1].job_key  # the launcher's run_key of the job alone
    again = D.Driver(ft["backend"], cache=RC.RunCache(ft["out"]), template=template, log=lambda *_: None).run(jobs)
    assert [r.status for r in again] == ["cached"] * 3
    assert ft["log"].read_text().splitlines() == ["j0 j1 j2", "j1", "j2"]  # Unity was not started again


def test_t03_cadence_counts_the_simulated_cells(tg, tolerances):
    # A longer run still scores every cadence cell, with the same denominator.
    sim = _early(tg, list(range(20)) + list(range(20, 260, 5)))
    ev = M.evaluate(M.t03(sim, tg), tolerances, 'closed')
    cells = [c for c in ev.cells if c.group == 'cadence']
    group = next(g for g in ev.groups if g.target == 'T03' and g.group == 'cadence')
    assert len(sim.early) > len(tg.shipped.early)
    assert group.n == len(cells)
    assert group.n_ok == sum(c.ok is True for c in cells)
    assert group.n_ok <= group.n


def test_full_ratchets_enforce_the_measured_bounds(tg, shipped_db, tolerances):
    cells = M.compute(_series(shipped_db), tg, ['T05', 'T07', 'T15'])
    base = M.evaluate(cells, tolerances, 'closed')
    assert all(base.status(t) == 'pass' for t in ('T05', 'T07', 'T15'))
    changed = []
    t05_units = sorted({c.unit for c in cells if c.target == 'T05' and c.group == 'multi_day'})[:6]
    for c in cells:
        if c.target == 'T05' and c.unit in t05_units and c.stat == 'Health':
            c = replace(c, residual=1e20, rel=1.0)
        if c.target == 'T07' and c.key in ('island 05', 'island 06', 'island 07'):
            limits = tolerances['targets']['T07']['cells']['islands_5_17']['ranges'][int(c.key[-2:])]
            c = replace(c, sim=[limits[1] + 1, limits[1] + 1])
        if c.target == 'T15' and c.group == 'late_dps' and c.tc == 140 and c.stat == 'Cutting':
            c = replace(c, residual=1.0, rel=0.18)
        changed.append(c)
    ev = M.evaluate(changed, tolerances, 'closed')
    assert all(ev.status(t) == 'fail' for t in ('T05', 'T07', 'T15'))


def test_t05_unexplained_ratchet(tolerances):
    from tufu_balance.calibrate.stage_full import check_t05_explanations
    assert check_t05_explanations({"unexplained": ["a", "b", "c"]}, tolerances)
    assert not check_t05_explanations({"unexplained": ["a", "b", "c", "d"]}, tolerances)
    with pytest.raises(ValueError):
        check_t05_explanations({"unexplained": ["a", "a"]}, tolerances)


def test_full_ratchet_values_match_measured_golden(tolerances):
    import json
    from pathlib import Path
    gold = json.loads((Path(__file__).parent / 'golden/original/sim_regression.json').read_text())['expectations']
    specs = tolerances['targets']
    assert specs['T05']['closed']['multi_day']['max_failing_units'] == 41 - gold['ratchet.T05.exact']['value']
    assert specs['T05']['max_unexplained'] == gold['ratchet.T05.unexplained']['value']
    assert specs['T07']['closed']['islands_5_17']['min_in_range'] == gold['ratchet.T07.in_range']['value']
    for group in ('late_dps', 'late_defense'):
        assert specs['T15']['cells'][group]['rel_max'] == gold['ratchet.T15.' + group]['value']


def test_full_ratchet_exact_boundaries(tg, shipped_db, tolerances):
    import math
    cells = M.compute(_series(shipped_db), tg, ['T05', 'T07', 'T15'])
    units = sorted({c.unit for c in cells if c.target == 'T05' and c.group == 'multi_day'})[:5]
    changed = []
    for c in cells:
        if c.target == 'T05' and c.unit in units and c.stat == 'Health':
            c = replace(c, residual=1e20, rel=1.0)
        if c.target == 'T07' and c.key in ('island 05', 'island 06'):
            limits = tolerances['targets']['T07']['cells']['islands_5_17']['ranges'][int(c.key[-2:])]
            c = replace(c, sim=[limits[1]+1, limits[1]+1])
        changed.append(c)
    ev = M.evaluate(changed, tolerances, 'closed')
    assert next(g for g in ev.groups if (g.target,g.group)==('T05','multi_day')).status == 'pass'
    assert next(g for g in ev.groups if (g.target,g.group)==('T07','islands_5_17')).status == 'pass'
    for group in ('late_dps','late_defense'):
        limit = tolerances['targets']['T15']['cells'][group]['rel_max']
        target = next(c for c in cells if c.target == 'T15' and c.group == group)
        test = [replace(c, rel=math.nextafter(limit, math.inf)) if c is target else c for c in cells]
        ev = M.evaluate(test, tolerances, 'closed')
        assert next(g for g in ev.groups if (g.target,g.group)==('T15',group)).status == 'fail'


def test_full_archive_rebuilds_from_cached_jobs(tmp_path, db):
    import hashlib, json
    from pathlib import Path
    from tufu_balance.calibrate.stage_full import archive
    from tufu_balance.calibrate.stage_open import BALANCE, load_results
    workspace = BALANCE/'out/calibration/full/first-run-e8054e8'
    if not (workspace/'full_jobs.json').exists():
        pytest.skip('historical cached runs are not available')
    paths = load_results(workspace/'full_jobs.json')
    if not all(p.exists() for p in paths.values()):
        pytest.skip('historical cached job directories are not available')
    out = archive(db, paths, workspace, tmp_path)
    manifest = json.loads((out/'manifest.json').read_text())
    for name, digest in manifest['files'].items():
        assert hashlib.sha256((out/name).read_bytes()).hexdigest() == digest
    assert manifest['status']['free']['T19'] == 'fail'
    assert manifest['status']['free']['T20'] == 'pass'
    assert set(manifest['modes']) == {'free', 'checkpoint', 'seed-1', 'seed-2'}
    existing = BALANCE/'out/calibration/full/calibrated-round4/manifest.json'
    if existing.exists():
        assert manifest['files'] == json.loads(existing.read_text())['files']
