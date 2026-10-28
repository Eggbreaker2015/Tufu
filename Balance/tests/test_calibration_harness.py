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
    job = M.write_shipped_job(tmp_path_factory.mktemp("shipped") / "shipped", slot_schedule=tg.slot_schedule)
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
    nonzero = [c for c in cells if c.residual != 0]
    assert nonzero == []
    for mode in M.MODES:
        ev = M.evaluate(cells, tolerances, mode)
        assert ev.failing_cells() == []
        assert {t: s for t, s in ev.targets.items() if t in COMPUTED and s not in ("pass", "report", "n/a", "pending")} == {}
    closed = M.evaluate(cells, tolerances, "closed").targets
    assert [t for t in COMPUTED if closed[t] == "pass"] == [t for t in COMPUTED if t not in ("T05", "T11")]
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
    for mode in M.MODES:
        ev = M.evaluate_run(db, tg, mode, tolerances)
        failing = ev.failing_cells()
        assert failing and {c.stat for c in failing} == {"Health"}
        assert {t for t, s in ev.targets.items() if s == "fail"} == expected[mode]
        assert {c.target for c in failing} == {"T05", "T06", "T10", "T18"}
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
    r = P.resolve(["defaults", "open", "calibrated"], {"Seed": 7}, root=root)
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
    assert P.resolve(["defaults", "open", "calibrated"], {"Seed": 7.0}, root=root2).params_hash == r.params_hash
    assert P.resolve(["defaults", "open", "calibrated"], {"Seed": 8}, root=root).params_hash != r.params_hash
    assert P.resolve(["defaults"], root=root).params_hash == P.resolve(["defaults"], root=root).params_hash


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
        P.resolve(["defaults", "open"], root=root)


def test_a_field_belongs_to_one_module(tmp_path):
    root = _layers(tmp_path, ENGINE, {**FASTTICK, "Seed": 2})
    with pytest.raises(P.ParamsError, match="defined by both"):
        P.resolve(["defaults"], root=root)


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
    db = simio.ingest(M.write_shipped_job(out / "job" / "shipped", slot_schedule=tg.slot_schedule))
    tol = M.load_tolerances()
    evs = [M.evaluate_run(db, tg, mode, tol) for mode in ("open", "closed")]
    resolved = P.resolve(["defaults", "open"], root=layers)
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
    in order, stopping at the first failing job (the editor ends a request there)."""

    name = "fake"

    def __init__(self, root: Path, fail: dict[str, int] | None = None, refuse: int = 0):
        self.root, self.fail, self.refuse = root, dict(fail or {}), refuse
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
        code = 0
        for job in request["jobs"]:
            jid = job["job_id"]
            code = 1 if self.fail.get(jid, 0) > 0 else 0
            if code:
                self.fail[jid] -= 1
            (run_dir / jid).mkdir(exist_ok=True)
            (run_dir / jid / "manifest.json").write_text(json.dumps({"job_id": jid, "exit_code": code,
                                                                     "params_hash": job.get("params_hash")}))
            if code:
                break
        report = {"exit_code": code, "guard_problems": [], "reason": None}
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
