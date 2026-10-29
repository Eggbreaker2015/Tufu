"""Determinism of the rebuilt simulator (design 2.6, 7.3 C3d).

* ``SimulationSpeed`` 1, 9, 500 and ``frame_budget_ms`` auto-pacing give the same digests for days 0-6 (and the
  same output files: only the request echo in ``manifest.json`` differs).
* Two launches of the same request at different real times give byte-identical outputs (every file except the
  ones Runbook 10 lists as carrying times or process ids); a third launch with ``TimeSkipSeconds`` preset
  non-zero differs only where the manifest echoes the request and reads the preset back.
* Two identical jobs in one editor process (``reenter_play``) give identical digests and files.

The jobs run the free policy (no ``engine_probe``), so the checks cover whatever policy the tree holds (C5-C7
rerun this file); one ``null_policy`` job anchors the digest chain in the golden suite ``sim_determinism``.
``simulation_speed`` and ``frame_budget_ms`` are not part of ``run_key`` (design 1.4.2): each speed variant
gets its own job id, so it is a run of its own instead of a cache hit.
"""

from __future__ import annotations

import copy
import datetime
import difflib
import json
import math
import shutil
from pathlib import Path

import pytest

from tufu_balance.golden import check, exact, same_set
from tufu_balance.unity import engine, launcher, request as req, schema, simio

pytestmark = pytest.mark.unity

STOP_DAY = 6
DAYS = list(range(STOP_DAY + 1))
OUTPUTS = {"digest_text": True}
NULL = engine.engine_probe(null_policy=True)
TIME_SKIP_SECONDS = 259200.0
# Speed variants: (job id, unity overrides). The harness always paces (design 2.4: a frame over the budget scales
# the speed down, one under half the budget doubles it, up to 16x the request), and frame_budget_ms must be > 0.
# A budget of 1e-6 ms holds speed 1 at one tick per frame; speed 9 keeps the default budget, so it runs 9 ticks
# on its first frames and then doubles; speed 500 runs up to 500 ticks in each frame. The paced one starts at 8 ticks
# per frame with a 2 ms budget, so the frame times drive the speed up and down while the job runs.
SPEEDS = {
    "speed-1": {"simulation_speed": 1, "frame_budget_ms": 1e-6},
    "speed-9": {"simulation_speed": 9},
    "speed-500": {"simulation_speed": 500},
    "speed-paced": {"simulation_speed": 8, "frame_budget_ms": 2.0},
}
# Files that carry times, process ids or the editor's log (Runbook 10), the launcher's backups, and the database
# tests build from a job directory (simio.ingest): never compared byte for byte.
RUN_VOLATILE = {"launcher.json", "editor_report.json", "sandbox.json", "unity.log"}
JOB_VOLATILE = {"timings.json", "unity.log", simio.SIM_DB}
# Manifest keys that echo the request or identify the job rather than describe the simulation.
ECHO_KEYS = ("run_key", "request", "job", "job_id", "job_index")


def _job(job_id: str, *, probe: dict | None = None) -> dict:
    return engine.engine_job(job_id, day=STOP_DAY, max_wall_seconds=300.0, probe=probe, outputs=OUTPUTS)


def _base_request(sandbox: dict | None = None) -> dict:
    return req.make_request([_job("det"), _job("det-again"), _job("det-null", probe=NULL)], sandbox=sandbox)


def _ok(result: launcher.RunResult) -> launcher.RunResult:
    assert result.exit_code == 0, result.describe()
    return result


def _copy(result: launcher.RunResult, dest: Path) -> launcher.RunResult:
    shutil.copytree(result.run_dir, dest, ignore=shutil.ignore_patterns("_real_backup"))
    return launcher.RunResult(dest, result.exit_code, result.request, result.report, result.cached)


@pytest.fixture(scope="module")
def first(unity_run, tmp_path_factory):
    """The base request (speed 500): a cached run of this commit or a fresh one, copied away because the
    repeat below reruns the same run_key into the same directory."""
    return _copy(_ok(unity_run(_base_request())), tmp_path_factory.mktemp("determinism_first") / "run")


@pytest.fixture(scope="module")
def repeat(first, unity_run):
    """The same request again, always a fresh launch (later real time than ``first``)."""
    return _ok(unity_run(_base_request(), force=True))


@pytest.fixture(scope="module")
def time_skip(repeat, unity_run):
    """The base request with ``TimeSkipSeconds`` preset non-zero in the sandbox PlayerPrefs."""
    return _ok(unity_run(_base_request({"prefs_seed": {"TimeSkipSeconds": TIME_SKIP_SECONDS}})))


@pytest.fixture(scope="module")
def speeds(time_skip, unity_run):
    return {job_id: _ok(unity_run(req.make_request([_job(job_id)], unity=unity)))
            for job_id, unity in SPEEDS.items()}


# ---------------------------------------------------------------- helpers

def _files(root: Path, volatile: set[str]) -> dict[str, bytes]:
    out = {}
    for path in sorted(root.rglob("*")):
        rel = path.relative_to(root)
        if path.is_dir() or rel.parts[0] == "_real_backup" or path.name in volatile:
            continue
        out[str(rel)] = path.read_bytes()
    return out


def _run_files(result: launcher.RunResult) -> dict[str, bytes]:
    return _files(result.run_dir, RUN_VOLATILE | JOB_VOLATILE)


def _job_files(result: launcher.RunResult, job_id: str) -> dict[str, bytes]:
    """The files of one job, for comparison with a job of another id: asset_guard.json names its job."""
    files = _files(result.job_dir(job_id), JOB_VOLATILE)
    if "asset_guard.json" in files:
        guard = json.loads(files["asset_guard.json"])
        assert guard.pop("job_id") == job_id
        files["asset_guard.json"] = json.dumps(guard, sort_keys=True).encode()
    return files


def _digests(result: launcher.RunResult, job_id: str) -> dict[int, str]:
    rows = schema.read_csv("digests.csv", result.file(job_id, "digests.csv").decode())
    return {int(r["day"]): r["sha256"] for r in rows}


def _digest_text(result: launcher.RunResult, job_id: str, day: int) -> list[str]:
    path = result.job_dir(job_id) / "digests.txt"
    if not path.exists():
        return []
    section, out = None, []
    for line in path.read_text().splitlines():
        if line.startswith("# day "):
            section = int(line.split()[2])
            continue
        if section == day:
            out.append(line)
    return out


def _explain_digest(a: launcher.RunResult, job_a: str, b: launcher.RunResult, job_b: str, day: int) -> str:
    diff = difflib.unified_diff(_digest_text(a, job_a, day), _digest_text(b, job_b, day), lineterm="", n=0)
    return f"day {day} digest text {job_a} -> {job_b}:\n" + "\n".join(list(diff)[:40])


def _mismatched_days(a: launcher.RunResult, job_a: str, b: launcher.RunResult, job_b: str) -> list[int]:
    da, db = _digests(a, job_a), _digests(b, job_b)
    return sorted(d for d in set(da) | set(db) if da.get(d) != db.get(d))


def _manifest_core(result: launcher.RunResult, job_id: str) -> dict:
    m = copy.deepcopy(result.manifest(job_id))
    for key in ECHO_KEYS:
        m.pop(key, None)
    return m


def _differing(a: dict[str, bytes], b: dict[str, bytes]) -> list[str]:
    return sorted(k for k in set(a) | set(b) if a.get(k) != b.get(k))


def _started(result: launcher.RunResult) -> datetime.datetime:
    return datetime.datetime.fromisoformat(json.loads((result.run_dir / "launcher.json").read_text())["started"])


def _check_days(result: launcher.RunResult, job_id: str) -> None:
    m = result.manifest(job_id)
    assert m["stop_reason"] == "simulation_stopped" and m["engine"]["stop_reason"] == "day", (job_id, m["stop_reason"])
    assert sorted(_digests(result, job_id)) == DAYS, job_id


# ---------------------------------------------------------------- speed

def test_digests_do_not_depend_on_the_simulation_speed(first, speeds):
    """Speeds 1, 9, 500 and auto-pacing: same digests for days 0-6, same files, same manifest apart from the echo."""
    ref = speeds["speed-500"]
    for job_id, result in speeds.items():
        _check_days(result, job_id)
    timings = {job_id: json.loads(r.file(job_id, "timings.json")) for job_id, r in speeds.items()}
    assert timings["speed-1"]["job_frames"] > timings["speed-9"]["job_frames"] > timings["speed-500"]["job_frames"]
    paced = timings["speed-paced"]
    # Pacing really was active: the harness changed the speed from the frame times during the job.
    assert paced["simulation_speed_min"] < paced["simulation_speed_max"], paced
    for t in ("speed-1", "speed-500"):
        assert timings[t]["simulation_speed_min"] == timings[t]["simulation_speed_max"] == SPEEDS[t]["simulation_speed"], \
            (t, timings[t])
    assert timings["speed-9"]["simulation_speed_min"] == 9
    ticks = speeds["speed-500"].manifest("speed-500")["counts"]["ticks"]
    assert timings["speed-500"]["job_frames"] == math.ceil(ticks / SPEEDS["speed-500"]["simulation_speed"])
    assert timings["speed-1"]["job_frames"] >= speeds["speed-1"].manifest("speed-1")["counts"]["ticks"]
    mismatched, files_off = [], []
    for job_id, result in speeds.items():
        days = _mismatched_days(ref, "speed-500", result, job_id)
        mismatched += [f"{job_id}:{d}" for d in days]
        assert not days, _explain_digest(ref, "speed-500", result, job_id, days[0])
        off = _differing(_job_files(ref, "speed-500"), _job_files(result, job_id))
        # The manifest differs only by the request echo (speed fields, run_key, job id).
        if "manifest.json" in off:
            off.remove("manifest.json")
            assert _manifest_core(result, job_id) == _manifest_core(ref, "speed-500"), job_id
        files_off += [f"{job_id}:{name}" for name in off]
    # The free job of the base request (speed 500, three jobs in one launch) has the same digests too.
    assert _mismatched_days(ref, "speed-500", first, "det") == []
    assert not files_off, files_off
    check("sim_determinism", {
        "speeds.mismatched_days": same_set(mismatched, source="design 2.6, 7.3 C3d", better="subset", original=[]),
        "speeds.differing_files": same_set(files_off, source="design 2.6, 7.3 C3d", better="subset", original=[],
                                           note="job files apart from timings.json/unity.log; manifest without the echo"),
    })


# ---------------------------------------------------------------- launches

def test_two_launches_are_byte_identical(first, repeat, tmp_path):
    """The same request launched twice, at different real times: every output file is the same byte for byte."""
    assert repeat.cached is False
    assert _started(repeat) > _started(first)
    a, b = _run_files(first), _run_files(repeat)
    assert any(name.endswith("simulation_data.json") for name in a) and "request.json" in a
    off = _differing(a, b)
    for name in off:
        job_id = name.split("/")[0]
        if name.endswith("digests.csv"):
            days = _mismatched_days(first, job_id, repeat, job_id)
            assert not days, _explain_digest(first, job_id, repeat, job_id, days[0])
    assert not off, off
    for job_id in ("det", "det-again", "det-null"):
        _check_days(repeat, job_id)
    # The database C1's ingest builds from the files is the same as well.
    for job_id in ("det", "det-null"):
        dbs = [simio.ingest(r.job_dir(job_id), tmp_path / f"{i}.{job_id}.sqlite") for i, r in enumerate((first, repeat))]
        assert dbs[0].read_bytes() == dbs[1].read_bytes(), job_id
    check("sim_determinism", {
        "launches.differing_files": same_set(off, source="design 2.6, 7.3 C3d", better="subset", original=[]),
        "null_policy.digests": exact([_digests(first, "det-null")[d] for d in DAYS], source="design 2.6",
                                     note="null-policy digest chain of days 0-6 (cross-session anchor)"),
    })


def test_time_skip_preset_and_real_time_change_nothing(first, time_skip):
    """``TimeSkipSeconds`` = 259200 in the sandbox prefs, launched at another real time: the files are those of
    the base launch; the manifest differs only where it echoes the request and reads the preset back."""
    assert _started(time_skip) != _started(first)
    for job_id in ("det", "det-again", "det-null"):
        s = time_skip.manifest(job_id)["sandbox"]
        assert s["prefs_time_skip_seconds"] == TIME_SKIP_SECONDS and s["prefs_seed_seen"] == {"TimeSkipSeconds": TIME_SKIP_SECONDS}
        assert s["time_service_cheat_offset_s"] == 0 and s["time_service_now_utc"].startswith("2024-01-01T00:00:00")
    a, b = _run_files(first), _run_files(time_skip)
    off = _differing(a, b)
    expected_echo = {"request.json"} | {f"{j}/manifest.json" for j in ("det", "det-again", "det-null")}
    for name in sorted(set(off) & expected_echo):
        if name == "request.json":
            ra, rb = json.loads(a[name]), json.loads(b[name])
            ra["sandbox"].pop("prefs_seed"), rb["sandbox"].pop("prefs_seed")
            ra.pop("run_key"), rb.pop("run_key"), ra.pop("out_dir"), rb.pop("out_dir")
            assert ra == rb
            continue
        job_id = name.split("/")[0]
        ma, mb = _manifest_core(first, job_id), _manifest_core(time_skip, job_id)
        for key in ("prefs_time_skip_seconds", "prefs_seed_seen"):
            ma["sandbox"].pop(key), mb["sandbox"].pop(key)
        assert ma == mb, job_id
    rest = [name for name in off if name not in expected_echo]
    for name in rest:
        if name.endswith("digests.csv"):
            job_id = name.split("/")[0]
            days = _mismatched_days(first, job_id, time_skip, job_id)
            assert not days, _explain_digest(first, job_id, time_skip, job_id, days[0])
    assert not rest, rest
    check("sim_determinism", {
        "time_skip.differing_files": same_set(rest, source="design 2.3, 2.6, 7.3 C3d", better="subset", original=[],
                                              note="besides request.json and the manifests' request echo"),
    })


# ---------------------------------------------------------------- one editor process

def test_two_identical_jobs_in_one_editor_process(first):
    """``det`` and ``det-again`` run one after the other in the same editor (reenter_play): same digests, same
    files, same manifest apart from the job id and index."""
    assert first.request["unity"]["reset_mode"] == "reenter_play"
    assert first.manifest("det")["job_index"] != first.manifest("det-again")["job_index"]
    days = _mismatched_days(first, "det", first, "det-again")
    assert not days, _explain_digest(first, "det", first, "det-again", days[0])
    off = _differing(_job_files(first, "det"), _job_files(first, "det-again"))
    if "manifest.json" in off:
        off.remove("manifest.json")
        assert _manifest_core(first, "det") == _manifest_core(first, "det-again")
    assert not off, off
    m = first.manifest("det-again")
    assert m["state_sha256_job_start"] == first.manifest("det")["state_sha256_job_start"]
    check("sim_determinism", {
        "same_process.mismatched_days": same_set([f"det-again:{d}" for d in days], source="design 2.5, 2.6, 7.3 C3d",
                                                 better="subset", original=[]),
        "same_process.differing_files": same_set(off, source="design 2.5, 2.6, 7.3 C3d", better="subset", original=[]),
    })
