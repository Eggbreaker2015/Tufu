"""Headless selftest, determinism of the harness outputs, serve mode (design 7.3 C0b)."""

from __future__ import annotations

import csv
import io
import json
import time
from pathlib import Path

import pytest

from tufu_balance.golden import check, check_file, exact, same_set
from tufu_balance.unity import launcher, request as req

pytestmark = pytest.mark.unity

BYTE_IDENTICAL = ("manifest.json", "ready_spawners.csv", "asset_guard.json")


def test_selftest_three_times_in_a_row(selftest_runs):
    for result in selftest_runs:
        assert result.exit_code == 0, result.describe()
        m = result.manifest("selftest")
        assert m["exit_code"] == 0 and m["stop_reason"] == "max_frames"
        assert m["ready"]["alive_enabled"] == req.EXPECTED_ALIVE_ENABLED
        guard = json.loads(result.file("selftest", "asset_guard.json"))["after_job"]
        assert guard["ok"] and guard["not_restored"] == [] and guard["changed_on_disk"] == []
        editor = json.loads((result.run_dir / "editor_report.json").read_text())
        assert editor["exit_code"] == 0 and editor["domain_and_scene_reload_on"]
        timings = json.loads(result.file("selftest", "timings.json"))
        print(f"E {editor['E_cold_start_s']:.2f} s, B {timings['B_play_to_ready_s']:.2f} s, "
              f"X {timings['X_exit_play_s']:.2f} s, process {result.report['process_wall_s']:.1f} s")


def test_outputs_byte_identical_at_different_real_times(selftest_runs):
    """Same request, same commit, three real times: every non-timing output is byte-identical."""
    first = selftest_runs[0]
    for other in selftest_runs[1:]:
        for name in BYTE_IDENTICAL:
            assert other.file("selftest", name) == first.file("selftest", name), name


def test_registration_order_and_ab_stable(selftest_runs):
    manifests = [r.manifest("selftest") for r in selftest_runs]
    assert len({m["ready"]["registration_order_sha256"] for m in manifests}) == 1
    assert len({json.dumps(m["ab"], sort_keys=True) for m in manifests}) == 1


def test_selftest_golden(selftest_run):
    """Freeze the harness numbers (suite ``harness``); C3a's selftest must reproduce them."""
    m = selftest_run.manifest("selftest")
    rows = list(csv.DictReader(io.StringIO(selftest_run.file("selftest", "ready_spawners.csv").decode())))
    exceptions = [f"{r['key']}|{'event' if r['is_event'] == '1' else 'vip' if r['is_vip'] == '1' else 'dummy' if r['is_dummy'] == '1' else 'other'}"
                  for r in rows if not (r["anyone_alive"] == "1" and r["is_enabled"] == "1")]
    check("harness", {
        "ready.alive_enabled": exact(m["ready"]["alive_enabled"], source="Runbook 6.1", original=699),
        "ready.registered": exact(m["ready"]["registered"], source="Runbook 6.1", original=716),
        "ready.scene_spawner_components": exact(m["ready"]["scene_spawner_components"], source="Runbook 6.1", original=717),
        "ready.exceptions": same_set(exceptions, source="Runbook 6.1", better="subset"),
        "ready.remedy_complete_bootstrap": exact(m["ready"]["remedy_complete_bootstrap"], source="Runbook 6.2", original=0),
        "ready.registration_order_sha256": exact(m["ready"]["registration_order_sha256"], source="C0b"),
        "ready.cooldowns_zeroed": exact(m["ready_cooldowns_zeroed"], source="C0b"),
        "ab.tests": same_set([f"{k}={v}" for k, v in m["ab"]["tests"].items()], source="C0b"),
        "ab.fastRPMParams": exact(m["ab"]["effective"]["fastRPMParams"], source="design 7.3 C0b"),
        "ab.spawnerCooldowns": exact(m["ab"]["effective"]["spawnerCooldowns"], source="design 7.3 C0b"),
        "state.job_start_sha256": exact(m["state_sha256_job_start"], source="C0b"),
        "state.job_end_sha256": exact(m["state_sha256_job_end"], source="C0b"),
    })
    exc = {e.split("|")[1] for e in exceptions}
    assert len(exceptions) == 17 and exc <= {"event", "vip", "dummy", "other"}
    assert sum(e.endswith("|event") for e in exceptions) == 5
    assert sum(e.endswith("|vip") for e in exceptions) == 10


def test_selftest_golden_ready_table(selftest_run):
    """The ready table byte for byte (C3a must keep it; later units compare against it)."""
    check_file("harness", "ready_spawners.csv", selftest_run.file("selftest", "ready_spawners.csv"))


def test_serve_mode_reenter_play_two_identical_jobs(unity_rev, unity_opts, selftest_run):
    """Resident editor, jobs in reenter_play: two identical jobs give the same digests (and the same
    as a cold selftest)."""
    state = launcher.serve_start(rev=unity_rev, idle_timeout_s=600, opts=unity_opts)
    try:
        assert launcher.serve_status()["running"]
        result = launcher.serve_submit(req.make_request([req.empty_job("serve-a"), req.empty_job("serve-b")]), opts=unity_opts)
        # A request for another commit is rejected by the resident editor (exit 5).
        queue = Path(state["queue_dir"])
        other = dict(req.preset("selftest"), code_rev="0" * 40, out_dir=str(result.run_dir / "rejected"))
        (queue / "inbox" / "zz-other-rev.json").write_text(req.dumps(other))
        done = queue / "done" / "zz-other-rev.result.json"
        deadline = time.monotonic() + 60
        while not done.exists() and time.monotonic() < deadline:
            time.sleep(0.25)
        rejected = json.loads(done.read_text())
    finally:
        out = launcher.serve_stop(opts=unity_opts)
    assert result.exit_code == 0, result.describe()
    a, b = result.manifest("serve-a"), result.manifest("serve-b")
    for key in ("state_sha256_job_start", "state_sha256_job_end"):
        assert a[key] == b[key] == selftest_run.manifest("selftest")[key]
    assert a["ready"] == b["ready"]
    assert result.file("serve-a", "ready_spawners.csv") == result.file("serve-b", "ready_spawners.csv") \
        == selftest_run.file("selftest", "ready_spawners.csv")
    assert rejected["exit_code"] == 5 and "code_rev" in rejected["jobs"][0]["error"]
    assert out["editor_exit_code"] == 0 and out["guard_problems"] == [] and out["served"] == 2
    assert state["code_rev"] == unity_rev
