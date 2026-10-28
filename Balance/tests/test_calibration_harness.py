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
from pathlib import Path

import numpy as np
import pytest

from tufu_balance.calibrate import driver as D, fit, metrics as M, params as P, report as R, runcache as RC, targets as T
from tufu_balance.golden import check, exact
from tufu_balance.unity import launcher, request as req, runner, simio

BALANCE = Path(__file__).resolve().parents[1]
COMPUTED = ("T01", "T02", "T03", "T04", "T05", "T06", "T07", "T09", "T10", "T11", "T12", "T13", "T14", "T16", "T17",
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
    assert [t for t in COMPUTED if closed[t] == "pass"] == [t for t in COMPUTED if t not in ("T05", "T11", "T17")]
    check("calibrate", {
        "T01.days": exact(by_target["T01"], source="T01", original=46),
        "T05.cells": exact(by_target["T05"], source="T05", note="41 intervals from TC4 x 15 stats", original=615),
        "T09.cells": exact(by_target["T09"], source="T09/G20", original=1380),
        "T10.cells": exact(by_target["T10"], source="T10/G19", note="1800 values + 60 NaN", original=1860),
        "T18.cells": exact(by_target["T18"], source="T18/G11", note="981 main + 2289 other", original=3270),
        "cells.total": exact(len(cells), source="design 7.3 P10", note="cells of T01-T07, T09-T14, T16-T18"),
    })


def test_health_times_1_01_fails_only_health_cells(tg, shipped_db, tolerances, tmp_path):
    db = tmp_path / "sim.sqlite"
    shutil.copy(shipped_db, db)
    con = sqlite3.connect(db)
    for table in ("c_SimulationData__TimeChunkSnapshots__Stats", "c_SimulationData__Snapshots__Stats"):
        con.execute(f'UPDATE "{table}" SET Value = Value * 1.01 WHERE Key__name = \'Health\'')
    con.commit()
    con.close()
    expected = {"open": {"T05", "T10", "T18"}, "closed": {"T06", "T10", "T18"}}
    cells_failing = {"open": {"T05", "T10", "T18"}, "closed": {"T05", "T06", "T10", "T18"}}  # closed T05: pending
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


def test_tolerances_name_every_group(tg, shipped_db, tolerances):
    cells = M.compute(_series(shipped_db), tg)
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
    ([_cell(None), _cell(None)], {"all_ok": True}, "unavailable"),
])
def test_group_rules_at_their_boundaries(cells, rule, status):
    assert M._group_status("TX", "g", cells, rule).status == status


def test_mode_without_a_rule_gives_cells_no_verdict(tolerances):
    cells = [M.Cell("T06", "tc_ge_24", "024|Health", 2.0, 1.0, 1.0, 1.0, stat="Health", tc=24),
             M.Cell("T17", "weapon", "001|weapon", None, 2, None, tc=1)]
    for mode in M.MODES:
        ev = M.evaluate(cells, tolerances, mode)
        assert ev.cells[1].ok is None and ev.targets["T17"] == "unavailable"
        assert ev.cells[0].ok is (None if mode == "open" else False)
        assert ev.failing_cells() == ([] if mode == "open" else [ev.cells[0]])


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
        assert {c.group for c in ev.failing_cells() if c.target == "T12"} == {"phase_19_57", "phase_57_154"}
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
    """Writes what the launcher and the editor would: request.json, launcher.json and one manifest per job,
    in order, stopping at the first failing job (the editor ends a request there). ``crash``: Unity dies
    (killed on a stall) while that job runs, before its manifest; ``guard``: the next N runs have a guard
    problem."""

    name = "fake"

    def __init__(self, root: Path, fail: dict[str, int] | None = None, refuse: int = 0,
                 crash: dict[str, int] | None = None, guard: int = 0):
        self.root, self.fail, self.refuse = root, dict(fail or {}), refuse
        self.crash, self.guard = dict(crash or {}), guard
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
        run_dir.mkdir(parents=True, exist_ok=True)
        (run_dir / "request.json").write_text(req.dumps(request))
        code, reason = 0, None
        for job in request["jobs"]:
            jid = job["job_id"]
            (run_dir / jid).mkdir(exist_ok=True)
            if self.crash.get(jid, 0) > 0:
                self.crash[jid] -= 1
                code, reason = launcher.EXIT_TIMEOUT, "heartbeat progress stalled for 120 s at 1"
                break
            code = 1 if self.fail.get(jid, 0) > 0 else 0
            if code:
                self.fail[jid] -= 1
            (run_dir / jid / "manifest.json").write_text(json.dumps({"job_id": jid, "exit_code": code,
                                                                     "params_hash": job.get("params_hash")}))
            if code:
                break
        problems = []
        if self.guard:
            self.guard -= 1
            problems, code = ["real save file changed: game_data.bin"], launcher.EXIT_GUARD
        report = {"exit_code": code, "guard_problems": problems, "reason": reason}
        (run_dir / "launcher.json").write_text(json.dumps(report))
        return launcher.RunResult(run_dir, code, request, report)


def _jobs(n, prefix="j"):
    return [req.make_job(f"{prefix}{i}", "replay", day=11, params={"Kappa": 0.2 + i / 100}) for i in range(n)]


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
    jobs[1] = req.make_job("j1", "replay", day=11, params={"Kappa": 0.3})
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
