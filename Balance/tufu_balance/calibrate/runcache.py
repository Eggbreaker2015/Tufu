"""Per-job result cache of the calibration driver (design 1.4.2 run_key, 5.3, 5.4; unit P10).

The launcher caches whole requests by ``run_key`` (``out/sim/runs/<run_key>/``). The driver batches jobs
that were never run together before, so it caches per job: ``job_key`` is the launcher's ``run_key`` of a
request holding that job alone (same request template, code version, Unity version, protected DLLs),
which jobs are independent of the batch they ran in (``reenter_play``).

* a single-job run of the launcher is found directly: ``out/sim/runs/<job_key>/launcher.json`` with exit 0;
* a job from a batch is indexed in ``out/sim/jobcache/<job_key>.json``, pointing at its directory in the
  batch run and recording the sha256 of its ``manifest.json``.

A hit needs a valid job (:func:`job_verdict`) with an unchanged manifest, in a run whose launcher found no
guard problem. A job is valid when its ``manifest.json`` says exit 0, the editor's asset guard after it
passed (``asset_guard.json``, written after the manifest), and the editor's own result for it, when the
run has one (``editor_report.json`` ``job_results`` in run mode, ``launcher.json`` ``result.jobs`` in
serve mode), says exit 0: the editor turns a job whose guard failed into exit 2 after its manifest was
written. A later job's failure, crash or timeout does not invalidate it: the editor runs the jobs in
order and checks a job completely before starting the next. Guard problems of the launcher (real save,
run worktree, protected files) belong to the whole run and cannot be attributed to one job.
Computing a key never touches the run worktree (no checkout): the tree hashes come from the object
store, which every worktree shares.
"""

from __future__ import annotations

import copy
import dataclasses
import hashlib
import json
from pathlib import Path
from typing import Any, Callable, Mapping

from ..unity import launcher, request as req, runner

DEFAULT_RUNS = launcher.OUT_ROOT
INDEX_DIR = "jobcache"


@dataclasses.dataclass(frozen=True)
class CodeIdentity:
    """What besides the request decides a run's outputs (design 1.4.2)."""

    code_rev: str
    trees: Mapping[str, str]
    unity: str
    dlls: Mapping[str, str | None]

    @classmethod
    def of(cls, rev: str | None = None, *, worktree: Path | None = None, run_tree: Path | None = None,
           opts: launcher.Options | None = None) -> "CodeIdentity":
        worktree = Path(worktree or runner.WORKTREE)
        run_tree = Path(run_tree or runner.run_worktree())
        opts = opts or launcher.Options()
        sha = runner.resolve(worktree, rev or "HEAD")
        return cls(code_rev=sha, trees=runner.tree_hashes(worktree, sha), unity=runner.unity_version(opts.unity),
                   dlls=runner.dll_hashes(runner.project_dir(run_tree)))


def resolve_sandbox(request: Mapping[str, Any]) -> dict:
    """``expected_ready_spawners = "auto"`` becomes the content-addressed table name, as the launcher does
    before computing ``run_key``."""
    r = copy.deepcopy(dict(request))
    sandbox = r.get("sandbox", {})
    if sandbox.get("expected_ready_spawners") == req.EXPECTED_READY_AUTO:
        sandbox["expected_ready_spawners"] = launcher.expected_ready_table(sandbox.get("ab_variant", "A"))[0]
    return r


def single_job_request(job: Mapping[str, Any], template: Mapping[str, Any]) -> dict:
    """The request a job would be run in alone: the template's unity/sandbox with this one job."""
    return req.make_request([dict(job)], unity=template.get("unity"), sandbox=template.get("sandbox"))


def job_key(job: Mapping[str, Any], template: Mapping[str, Any], identity: CodeIdentity,
            resolve: Callable[[Mapping[str, Any]], dict] = resolve_sandbox) -> str:
    r = resolve(single_job_request(job, template))
    return runner.run_key(r, code_rev=identity.code_rev, trees=identity.trees, unity=identity.unity,
                          dlls=identity.dlls, dev=None)


def _sha(path: Path) -> str | None:
    try:
        return hashlib.sha256(path.read_bytes()).hexdigest()
    except OSError:
        return None


def _exit_code(path: Path) -> int | None:
    try:
        return json.loads(path.read_text()).get("exit_code")
    except (OSError, ValueError, AttributeError):
        return None


def _json(path: Path) -> Any:
    try:
        return json.loads(Path(path).read_text())
    except (OSError, ValueError):
        return None


def editor_job_results(run_dir: Path) -> dict[str, int | None]:
    """The editor's per-job exit codes: ``result.jobs`` of a served request (``launcher.json``), else
    ``job_results`` of ``editor_report.json`` (run mode; absent when Unity died)."""
    run_dir = Path(run_dir)
    rep = _json(run_dir / "launcher.json")
    jobs = (rep.get("result") or {}).get("jobs") if isinstance(rep, dict) else None
    if jobs is None:
        editor = _json(run_dir / "editor_report.json")
        jobs = editor.get("job_results") if isinstance(editor, dict) else None
    return {j["job_id"]: j.get("exit_code") for j in jobs or [] if isinstance(j, dict) and "job_id" in j}


def job_verdict(run_dir: Path, job_id: str) -> tuple[bool, int | None]:
    """(ran, exit code) of one job of a run. ``ran`` is False when the job left nothing (the request ended
    before it). The code is 0 only for a valid job: manifest exit 0, asset guard passed, and no non-zero
    editor result. A job that ran but whose code cannot be read (Unity died while it ran, or between its
    manifest and its asset guard) gets None."""
    job_dir = Path(run_dir) / job_id
    manifest = _exit_code(job_dir / "manifest.json")
    guard = _json(job_dir / "asset_guard.json")
    guard_ok = (guard.get("after_job") or {}).get("ok") if isinstance(guard, dict) else None
    editor = editor_job_results(run_dir)
    ran = (job_dir / "manifest.json").exists() or guard is not None or job_id in editor
    if not ran:
        return False, None
    code = editor.get(job_id)
    if code not in (None, 0):
        return True, code
    if manifest not in (None, 0):
        return True, manifest
    if guard_ok is False:
        return True, launcher.EXIT_GUARD
    if manifest is None or guard_ok is not True:
        return True, None
    return True, 0


def job_ok(run_dir: Path, job_id: str) -> bool:
    return job_verdict(run_dir, job_id) == (True, 0)


def run_ok(run_dir: Path) -> bool:
    """The launcher checked the run and found no guard problem (a timeout or a crash of a later job is
    not one: the jobs that finished before it are valid)."""
    try:
        rep = json.loads((Path(run_dir) / "launcher.json").read_text())
    except (OSError, ValueError):
        return False
    return isinstance(rep, dict) and "guard_problems" in rep and not rep.get("guard_problems")


@dataclasses.dataclass
class CacheEntry:
    job_key: str
    job_dir: Path
    run_dir: Path
    job_id: str


class RunCache:
    def __init__(self, root: str | Path = DEFAULT_RUNS):
        self.root = Path(root)

    @property
    def index(self) -> Path:
        return self.root / INDEX_DIR

    def lookup(self, key: str, job_id: str | None = None) -> CacheEntry | None:
        run_dir = self.root / "runs" / key
        if _exit_code(run_dir / "launcher.json") == 0 and run_ok(run_dir):
            req_path = run_dir / "request.json"
            try:
                jobs = json.loads(req_path.read_text())["jobs"]
            except (OSError, ValueError, KeyError):
                jobs = []
            if len(jobs) == 1:
                jid = jobs[0]["job_id"]
                if job_ok(run_dir, jid):
                    return CacheEntry(key, run_dir / jid, run_dir, jid)
        record = self.index / f"{key}.json"
        try:
            rec = json.loads(record.read_text())
        except (OSError, ValueError):
            return None
        job_dir, run = Path(rec["job_dir"]), Path(rec["run_dir"])
        manifest = job_dir / "manifest.json"
        if run_ok(run) and job_ok(run, rec["job_id"]) and job_dir == run / rec["job_id"] \
                and _sha(manifest) == rec.get("manifest_sha256"):
            return CacheEntry(key, job_dir, run, rec["job_id"])
        return None

    def store(self, key: str, job_dir: Path, run_dir: Path, job_id: str, **info) -> CacheEntry:
        """Index a finished job of a batch run (valid jobs of runs without guard problems only)."""
        job_dir, run_dir = Path(job_dir), Path(run_dir)
        if job_dir != run_dir / job_id or not run_ok(run_dir) or not job_ok(run_dir, job_id):
            raise ValueError(f"not caching {job_dir}: the run had guard problems or the job is not valid "
                             f"{job_verdict(run_dir, job_id)}")
        self.index.mkdir(parents=True, exist_ok=True)
        rec = {"job_key": key, "job_id": job_id, "job_dir": str(job_dir), "run_dir": str(run_dir),
               "manifest_sha256": _sha(job_dir / "manifest.json"), **info}
        tmp = self.index / f".{key}.tmp"
        tmp.write_text(json.dumps(rec, indent=1, sort_keys=True) + "\n")
        tmp.replace(self.index / f"{key}.json")
        return CacheEntry(key, job_dir, run_dir, job_id)
