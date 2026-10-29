"""Cumulative plans, real child exit propagation, and baseline preservation."""

import json
import subprocess
import sys
from types import SimpleNamespace

import pytest

from tufu_balance import refresh as R


def test_levels_are_cumulative_and_complete(tmp_path):
    plans = [[s.name for s in R.plan(n, tmp_path)] for n in range(3)]
    assert plans[0] == ["extract", "graph", "render_catalog", "pytest-static", "cscheck", "drift",
                        "golden-guard", "golden-current-differences"]
    assert set(plans[0]) <= set(plans[1]) <= set(plans[2])
    assert {"unity-probe-edit-play", "smoke-11", "ingest-smoke-11", "crosscheck-smoke-11", "baseline-smoke-11"} <= set(plans[1])
    assert {"golden-405", "bake-report", "empty-patch", "pytest-unity-all", "baseline-golden-405"} <= set(plans[2])
    for n in range(3):
        commands = [s.command for s in R.plan(n, tmp_path, rev="abc123")]
        assert all("accept" not in c and "--apply" not in c for c in commands)
        assert all("abc123" in s.command for s in R.plan(n, tmp_path, rev="abc123")
                   if "--step" in s.command)


@pytest.mark.parametrize("size,workers", [(1, 6), (17, 6), (893, 6)])
def test_static_shards_assign_every_collected_node_once(size, workers):
    nodes = [f"tests/test_sample.py::test_{n}" for n in range(size)]
    shards = R.shard_nodes(nodes, workers)
    flattened = [n for shard in shards for n in shard]
    assert len(flattened) == len(set(flattened)) == size
    assert set(flattened) == set(nodes)
    assert all(shards)


def test_static_shards_keep_module_fixtures_together():
    nodes = [f"tests/{module}.py::test_{i}" for module in ("test_cscheck", "test_unity_launcher", "test_params_import")
             for i in range(6)]
    shards = R.shard_nodes(nodes, 6)
    assert len([s for s in shards if "test_cscheck" in s[0]]) == 1
    assert len([s for s in shards if "test_unity_launcher" in s[0]]) == 2
    assert len([s for s in shards if "test_params_import" in s[0]]) == 2


def test_dry_run_lists_every_step_without_writes(tmp_path, monkeypatch, capsys):
    monkeypatch.setattr(R, "BALANCE", tmp_path)
    for n in range(3):
        dest = tmp_path / "out" / f"dry{n}"
        assert R.main(["--level", str(n), "--dry-run", "--out", str(dest)]) == 0
        text = capsys.readouterr().out
        assert all(s.name + ":" in text for s in R.plan(n, dest))
        assert not dest.exists()


@pytest.mark.parametrize("failing", range(4))
def test_any_failing_child_propagates_and_later_steps_are_not_run(tmp_path, failing):
    baseline = tmp_path / "current"
    baseline.mkdir()
    (baseline / "test.json").write_text('{"keep": true}\n')
    before = R.fingerprint(baseline)
    steps = [R.Step(f"s{i}", (sys.executable, "-c", f"print('step {i}'); raise SystemExit({17 if i == failing else 0})"))
             for i in range(4)]
    result = R.execute(steps, tmp_path / "run", current=baseline)
    assert result["exit_code"] == 17 and len(result["steps"]) == failing + 1
    assert result["current_unchanged"] and R.fingerprint(baseline) == before
    assert (tmp_path / "run/summary.md").exists()
    assert "step " in (tmp_path / "run" / f"s{failing}.log").read_text()


def test_launch_error_is_nonzero_and_recorded(tmp_path):
    step = R.Step("unavailable", ("/does/not/exist",))
    result = R.execute([step], tmp_path / "run", current=tmp_path / "current")
    assert result["exit_code"] == 2 and result["steps"][0]["exit_code"] == 2


def test_current_mutation_is_detected_even_when_child_succeeds(tmp_path):
    current = tmp_path / "current"
    current.mkdir()
    def mutate(*a, **kw):
        (current / "unauthorized.json").write_text("changed")
        return subprocess.CompletedProcess(a, 0, "", "")
    result = R.execute([R.Step("bad", ("irrelevant",))], tmp_path / "run", run=mutate, current=current)
    assert result["exit_code"] == 1 and not result["current_unchanged"]


def test_summary_includes_current_baseline_difference(tmp_path):
    result = R.execute([R.Step("golden-current-differences", (sys.executable, "-c", "print('baseline difference: example')"))],
                       tmp_path / "run", current=tmp_path / "current")
    assert result["exit_code"] == 0
    assert "baseline difference: example" in (tmp_path / "run/summary.md").read_text()
    assert json.loads((tmp_path / "run/summary.json").read_text())["current_unchanged"]


@pytest.mark.parametrize("exit_code", [0, 19])
def test_refresh_uses_private_extracted_db_and_pins_unity_rev(tmp_path, monkeypatch, exit_code):
    monkeypatch.setattr(R, "BALANCE", tmp_path)
    seen = {}
    def execute(steps, out, *, env):
        seen.update(env)
        return {"exit_code": exit_code}
    monkeypatch.setattr(R, "execute", execute)
    dest = tmp_path / "out/private"
    assert R.main(["--out", str(dest), "--rev", "abc123"]) == exit_code
    assert seen["TUFU_BALANCE_DB"] == str(dest / "extracted/balance.sqlite")
    assert seen["TUFU_UNITY_REV"] == "abc123"
    assert seen["TMPDIR"] == str(dest / "tmp")


def test_outside_out_is_rejected():
    with pytest.raises(SystemExit):
        R.main(["--dry-run", "--out", str(R.BALANCE / "tests")])


def test_csv_baseline_checks_bytes_and_missing_outputs(tmp_path, monkeypatch):
    monkeypatch.setattr(R, "BALANCE", tmp_path)
    root = tmp_path / "tests/golden/current/sim_run/smoke-11"
    root.mkdir(parents=True)
    (root / "daily.csv").write_text("day,value\n0,1\n")
    job = tmp_path / "job"
    job.mkdir()
    monkeypatch.setattr(R, "job_dir", lambda *a: job)
    assert R.baseline(tmp_path, "smoke-11") == 1
    (job / "daily.csv").write_bytes((root / "daily.csv").read_bytes())
    assert R.baseline(tmp_path, "smoke-11") == 0
    (job / "daily.csv").write_text("day,value\n0,2\n")
    assert R.baseline(tmp_path, "smoke-11") == 1


@pytest.mark.parametrize("exit_code", [0, 1])
def test_smoke_crosscheck_excludes_checkpoint_only_dp8(tmp_path, monkeypatch, exit_code):
    from tufu_balance.crosscheck import __main__ as X
    seen = []
    monkeypatch.setattr(X, "main", lambda args: seen.extend(args) or exit_code)
    monkeypatch.setattr(R, "job_dir", lambda *a: tmp_path)
    assert R.run_step("crosscheck-smoke-11", tmp_path, "HEAD") == exit_code
    assert seen[2:] == ["X1", "X2", "X3", "X4", "X5", "X6", "X7"]


def test_main_static_step_propagates_first_failing_shard(tmp_path, monkeypatch):
    monkeypatch.setattr(R, "BALANCE", tmp_path)
    out = tmp_path / "out/static"
    out.mkdir(parents=True)
    def run(command, **kwargs):
        if "--collect-only" in command:
            return subprocess.CompletedProcess(command, 0, "tests/test_a.py::test_a\ntests/test_b.py::test_b\n", "")
        index = int(command[command.index("--step") + 1].removeprefix("pytest-static-worker-"))
        return subprocess.CompletedProcess(command, [23, 31][index], "shard failed", "")
    monkeypatch.setattr(R.subprocess, "run", run)
    assert R.main(["--step", "pytest-static", "--out", str(out)]) == 23
    assert len(json.loads((out / "static-shards.json").read_text())["shards"]) == 2
    assert all((out / f"pytest-static-{i}.log").exists() for i in range(2))


def test_main_worker_step_propagates_pytest_failure(tmp_path, monkeypatch):
    monkeypatch.setattr(R, "BALANCE", tmp_path)
    out = tmp_path / "out/worker"
    out.mkdir(parents=True)
    (out / "static-shards.json").write_text(json.dumps({"run_id": "test-session", "shards": [["tests/test_a.py::test_a"]]}))
    seen = []
    def pytest_main(args, **kwargs):
        seen.extend(args)
        assert len(kwargs["plugins"]) == 1
        return 5
    monkeypatch.setattr(pytest, "main", pytest_main)
    assert R.main(["--step", "pytest-static-worker-0", "--out", str(out)]) == 5
    assert seen[-1] == "tests/test_a.py::test_a"


def test_main_preset_step_propagates_launcher_failure(tmp_path, monkeypatch):
    from tufu_balance.unity import launcher, request
    monkeypatch.setattr(R, "BALANCE", tmp_path)
    out = tmp_path / "out/preset"
    out.mkdir(parents=True)
    req = {"jobs": []}
    monkeypatch.setattr(request, "preset", lambda name: req)
    seen = []
    def launch(value, *, rev):
        seen.append((value, rev))
        return SimpleNamespace(exit_code=7, run_dir=tmp_path / "run", describe=lambda: "launcher failed")
    monkeypatch.setattr(launcher, "run", launch)
    assert R.main(["--step", "smoke-11", "--out", str(out), "--rev", "abc123"]) == 7
    assert seen == [(req, "abc123")]
    assert json.loads((out / "smoke-11.json").read_text())["exit_code"] == 7


def test_main_step_value_error_is_nonzero(tmp_path, monkeypatch, capsys):
    monkeypatch.setattr(R, "BALANCE", tmp_path)
    assert R.main(["--step", "unknown", "--out", str(tmp_path / "out/error")]) == 1
    assert "unknown refresh step: unknown" in capsys.readouterr().err


def test_summary_records_load_next_to_step_and_total_timings(tmp_path, monkeypatch):
    loads = iter([(1., 2., 3.), (4., 5., 6.), (7., 8., 9.), (10., 11., 12.)])
    monkeypatch.setattr(R.os, "getloadavg", lambda: next(loads))
    monkeypatch.setattr(R.os, "cpu_count", lambda: 10)
    R.execute([R.Step("check", ("fake",))], tmp_path / "run", current=tmp_path / "current",
              run=lambda *a, **kw: subprocess.CompletedProcess(a, 0, "", ""))
    summary = json.loads((tmp_path / "run/summary.json").read_text())
    assert summary["load_start"] == {"load_average": [1., 2., 3.], "cpu_count": 10}
    assert summary["load_end"] == {"load_average": [10., 11., 12.], "cpu_count": 10}
    assert summary["steps"][0]["load_start"]["load_average"] == [4., 5., 6.]
    assert summary["steps"][0]["load_end"]["load_average"] == [7., 8., 9.]
    assert summary["seconds"] >= summary["steps"][0]["seconds"] >= 0


@pytest.mark.parametrize("available,smudge_code", [(False, 0), (True, 0), (True, 1)])
def test_extract_requires_local_lfs_object_and_keeps_smudge_offline(tmp_path, monkeypatch, available, smudge_code):
    from tufu_balance import extract as E
    monkeypatch.setattr(R, "REPO", tmp_path)
    source = tmp_path / "Assets"
    source.mkdir()
    rel = "Assets/Main.unity"
    oid = "a" * 64
    pointer = f"version https://git-lfs.github.com/spec/v1\noid sha256:{oid}\nsize 5\n".encode()
    (source / "Main.unity").write_bytes(pointer)
    out = tmp_path / "out/refresh"
    out.mkdir(parents=True)
    monkeypatch.setattr(E, "ASSETS", source)
    monkeypatch.setattr(R.sys, "argv", [])
    extracted = []
    monkeypatch.setattr(E, "main", lambda: extracted.append((E.ASSETS / "Main.unity").read_bytes()))
    commands = []
    def run(command, **kwargs):
        commands.append(command)
        if "ls-files" in command:
            assert f"--include={rel}" in command
            return subprocess.CompletedProcess(command, 0, json.dumps({"files": [
                {"name": rel, "oid": oid, "downloaded": available}]}), "")
        assert command[-2:] == ["lfs", "smudge"]
        assert f"lfs.url={(out / 'offline-lfs').as_uri()}" in command
        assert "lfs.remote.autodetect=false" in command
        assert kwargs["input"] == pointer
        return subprocess.CompletedProcess(command, smudge_code, b"scene" if not smudge_code else pointer, b"offline failure")
    monkeypatch.setattr(R.subprocess, "run", run)
    if not available:
        with pytest.raises(ValueError, match="object missing locally.*refresh never downloads"):
            R.extract(out)
        assert len(commands) == 1 and not extracted
    elif smudge_code:
        with pytest.raises(ValueError, match="LFS scene not available.*offline failure"):
            R.extract(out)
        assert not extracted
    else:
        R.extract(out)
        assert extracted == [b"scene"]
