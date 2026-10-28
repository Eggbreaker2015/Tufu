"""Session fixtures for tests that read Unity run outputs (design 2.8; unit C0b).

Tests here are marked ``unity`` and only run with ``pytest --unity`` (root conftest). A run is found by
its run_key under ``Balance/out/sim/runs/``; when there is no successful run for the key the fixture
starts Unity in the run worktree at the commit under test (``$TUFU_UNITY_REV``, default this
worktree's HEAD: uncommitted changes are not tested, a warning says so). Failing tests print the last
200 lines of ``unity.log`` and the manifest's sandbox report through ``RunResult.describe()``.
"""

from __future__ import annotations

import os
import shutil
import warnings

import pytest

from tufu_balance.unity import launcher, request as req, runner


@pytest.fixture(scope="session")
def unity_rev() -> str:
    rev = os.environ.get("TUFU_UNITY_REV") or "HEAD"
    sha = runner.resolve(runner.WORKTREE, rev)
    dirty = [l for l in runner.porcelain(runner.WORKTREE).splitlines() if "Recovery/StudyProject/" in l]
    if dirty and rev == "HEAD":
        warnings.warn(f"{len(dirty)} uncommitted Unity project change(s) are not part of the tested commit {sha[:10]}")
    return sha


@pytest.fixture(scope="session")
def unity_opts() -> launcher.Options:
    return launcher.Options(nographics=os.environ.get("TUFU_UNITY_NOGRAPHICS") == "1")


@pytest.fixture(scope="session")
def unity_run(unity_rev, unity_opts):
    """``unity_run(request, force=False, validate=True, method="Run", opts=None)`` -> RunResult.
    Results are cached per run_key on disk and per request in the session."""
    cache: dict[str, launcher.RunResult] = {}

    def run(request, *, force=False, validate=True, method="Run", opts=None):
        key = req.canonical_json([request, method, validate, bool(opts)])
        if key in cache and not force:
            return cache[key]
        result = launcher.run(request, rev=unity_rev, force=force, validate=validate, method=method,
                              opts=opts or unity_opts, log=print)
        if not force:
            cache[key] = result
        return result

    return run


@pytest.fixture(scope="session")
def selftest_runs(unity_run, tmp_path_factory):
    """Three fresh selftests at different real times (design 7.3 C0b: three in a row). Each run's
    directory is copied away before the next one replaces it."""
    copies = []
    for i in range(3):
        result = unity_run(req.preset("selftest"), force=True, method="SelfTest")
        dest = tmp_path_factory.mktemp(f"selftest{i + 1}") / "run"
        shutil.copytree(result.run_dir, dest)
        copies.append(launcher.RunResult(dest, result.exit_code, result.request, result.report))
    return copies


@pytest.fixture(scope="session")
def selftest_run(selftest_runs):
    return selftest_runs[-1]
