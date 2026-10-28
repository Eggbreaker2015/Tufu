"""Per-job result cache of the calibration driver (design 1.4.2 run_key, 5.3, 5.4; unit P10).

The launcher caches whole requests by ``run_key`` (``out/sim/runs/<run_key>/``). The driver batches jobs
that were never run together before, so it caches per job: ``job_key`` is the launcher's ``run_key`` of a
request holding that job alone (same request template, code version, Unity version, protected DLLs),
which jobs are independent of the batch they ran in (``reenter_play``).

* a single-job run of the launcher is found directly: ``out/sim/runs/<job_key>/launcher.json`` with exit 0;
* a job from a batch is indexed in ``out/sim/jobcache/<job_key>.json``, pointing at its directory in the
  batch run and recording the sha256 of its ``manifest.json``.

A hit needs the job's manifest exit code 0 and an unchanged manifest, in a run whose launcher found no
guard problem. A later job's failure, crash or timeout does not invalidate it: the editor runs the jobs
in order, and writes a job's ``manifest.json`` (with its exit code) only after that job's outputs, so a
manifest with exit 0 marks a finished job. Guard problems (real save, run worktree, protected files)
belong to the whole run and cannot be attributed to one job.
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
        if _exit_code(run_dir / "launcher.json") == 0:
            req_path = run_dir / "request.json"
            try:
                jobs = json.loads(req_path.read_text())["jobs"]
            except (OSError, ValueError, KeyError):
                jobs = []
            if len(jobs) == 1:
                jid = jobs[0]["job_id"]
                if _exit_code(run_dir / jid / "manifest.json") == 0:
                    return CacheEntry(key, run_dir / jid, run_dir, jid)
        record = self.index / f"{key}.json"
        try:
            rec = json.loads(record.read_text())
        except (OSError, ValueError):
            return None
        job_dir, run = Path(rec["job_dir"]), Path(rec["run_dir"])
        manifest = job_dir / "manifest.json"
        if run_ok(run) and _exit_code(manifest) == 0 and _sha(manifest) == rec.get("manifest_sha256"):
            return CacheEntry(key, job_dir, run, rec["job_id"])
        return None

    def store(self, key: str, job_dir: Path, run_dir: Path, job_id: str, **info) -> CacheEntry:
        """Index a finished job of a batch run (exit 0 jobs of runs without guard problems only)."""
        job_dir, run_dir = Path(job_dir), Path(run_dir)
        if not run_ok(run_dir) or _exit_code(job_dir / "manifest.json") != 0:
            raise ValueError(f"not caching {job_dir}: the run had guard problems or the job did not exit 0")
        self.index.mkdir(parents=True, exist_ok=True)
        rec = {"job_key": key, "job_id": job_id, "job_dir": str(job_dir), "run_dir": str(run_dir),
               "manifest_sha256": _sha(job_dir / "manifest.json"), **info}
        tmp = self.index / f".{key}.tmp"
        tmp.write_text(json.dumps(rec, indent=1, sort_keys=True) + "\n")
        tmp.replace(self.index / f"{key}.json")
        return CacheEntry(key, job_dir, run_dir, job_id)
