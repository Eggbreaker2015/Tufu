"""Calibration driver: Python runs C# jobs in batches (design 5.3, 5.4; unit P10).

``Driver.run(jobs)``:

1. computes each job's ``job_key`` (:mod:`.runcache`) and returns cached results without starting Unity;
2. sends the misses in batches of at most ``batch_size`` (default 12) jobs, to the resident editor when one
   is serving (``launcher.serve_submit``) or as one batch launch each (``launcher.run``);
3. a job that failed is retried once, alone; jobs that did not run because an earlier job of their batch
   failed (the editor ends a request at its first failing job) go into the next batch without counting
   as an attempt;
4. indexes every job that finished with exit 0 in the cache.

Results come back in input order. The backends are small objects with ``identity()`` and ``submit()``,
so tests drive the logic without Unity.
"""

from __future__ import annotations

import dataclasses
import json
from pathlib import Path
from typing import Any, Callable, Mapping, Protocol, Sequence

from ..unity import launcher, request as req, runner
from .runcache import CodeIdentity, RunCache, job_key, run_ok, single_job_request

DEFAULT_BATCH = 12


class Backend(Protocol):
    name: str

    def identity(self) -> CodeIdentity: ...

    def submit(self, request: dict) -> launcher.RunResult: ...


class BatchBackend:
    """One Unity launch per batch (``launcher.run``), at ``rev`` (default: this worktree's HEAD)."""

    name = "batch"

    def __init__(self, rev: str | None = None, *, opts: launcher.Options | None = None, worktree: Path | None = None,
                 run_tree: Path | None = None, out_root: Path | None = None, paths=None, log=print):
        self.opts = opts or launcher.Options()
        self.worktree = Path(worktree or runner.WORKTREE)
        self.run_tree = Path(run_tree or runner.run_worktree())
        self.rev = runner.resolve(self.worktree, rev or "HEAD")
        self.out_root, self.paths, self.log = out_root, paths, log

    def identity(self) -> CodeIdentity:
        return CodeIdentity.of(self.rev, worktree=self.worktree, run_tree=self.run_tree, opts=self.opts)

    def submit(self, request: dict) -> launcher.RunResult:
        return launcher.run(request, rev=self.rev, opts=self.opts, worktree=self.worktree, run_tree=self.run_tree,
                            out_root=self.out_root, paths=self.paths, log=self.log)


class ServeBackend:
    """The resident editor (``serve start`` must have been run): jobs go through its queue."""

    name = "serve"

    def __init__(self, *, opts: launcher.Options | None = None, worktree: Path | None = None,
                 run_tree: Path | None = None, out_root: Path | None = None, log=print):
        self.opts = opts or launcher.Options()
        self.worktree = Path(worktree or runner.WORKTREE)
        self.run_tree = Path(run_tree or runner.run_worktree())
        self.out_root, self.log = out_root, log
        status = launcher.serve_status(run_tree=self.run_tree)
        if not status.get("running"):
            raise launcher.LauncherError("no resident editor is serving")
        self.code_rev = status["code_rev"]

    def identity(self) -> CodeIdentity:
        return CodeIdentity.of(self.code_rev, worktree=self.worktree, run_tree=self.run_tree, opts=self.opts)

    def submit(self, request: dict) -> launcher.RunResult:
        return launcher.serve_submit(request, run_tree=self.run_tree, worktree=self.worktree, opts=self.opts,
                                     out_root=self.out_root, log=self.log)


def default_backend(*, rev: str | None = None, opts: launcher.Options | None = None, worktree: Path | None = None,
                    run_tree: Path | None = None, out_root: Path | None = None, paths=None, log=print) -> Backend:
    """The resident editor when one is serving, else batch launches at ``rev``."""
    if launcher.serve_status(run_tree=run_tree).get("running"):
        return ServeBackend(opts=opts, worktree=worktree, run_tree=run_tree, out_root=out_root, log=log)
    return BatchBackend(rev, opts=opts, worktree=worktree, run_tree=run_tree, out_root=out_root, paths=paths, log=log)


@dataclasses.dataclass
class JobResult:
    job_id: str
    job_key: str
    status: str  # cached | ok | failed
    job_dir: Path | None
    attempts: int
    backend: str
    run_dirs: list[Path] = dataclasses.field(default_factory=list)
    exit_code: int | None = None
    error: str | None = None

    @property
    def ok(self) -> bool:
        return self.status in ("cached", "ok")

    @property
    def sim_db(self) -> Path | None:
        return None if self.job_dir is None else self.job_dir / "sim.sqlite"


def _job_exit(run_dir: Path, job_id: str) -> int | None:
    try:
        return json.loads((Path(run_dir) / job_id / "manifest.json").read_text()).get("exit_code")
    except (OSError, ValueError, AttributeError):
        return None


class Driver:
    def __init__(self, backend: Backend, *, cache: RunCache | None = None, template: Mapping[str, Any] | None = None,
                 batch_size: int = DEFAULT_BATCH, resolve: Callable | None = None, log=print):
        self.backend = backend
        self.cache = cache or RunCache()
        self.template = dict(template or req.make_request([req.empty_job("template")]))
        self.batch_size = max(1, int(batch_size))
        self.resolve = resolve
        self.log = log
        self.submitted: list[list[str]] = []  # job ids of every submitted request, in order

    def _request(self, jobs: Sequence[Mapping[str, Any]]) -> dict:
        if len(jobs) == 1:
            return single_job_request(jobs[0], self.template)
        return req.make_request([dict(j) for j in jobs], unity=self.template.get("unity"),
                                sandbox=self.template.get("sandbox"))

    def keys(self, jobs: Sequence[Mapping[str, Any]]) -> list[str]:
        identity = self.backend.identity()
        kw = {"resolve": self.resolve} if self.resolve else {}
        return [job_key(j, self.template, identity, **kw) for j in jobs]

    def run(self, jobs: Sequence[Mapping[str, Any]]) -> list[JobResult]:
        ids = [j["job_id"] for j in jobs]
        if len(set(ids)) != len(ids):
            raise ValueError("job ids must be unique")
        keys = self.keys(jobs)
        results: dict[str, JobResult] = {}
        pending = []
        for job, key in zip(jobs, keys):
            hit = self.cache.lookup(key)
            if hit is not None:
                results[job["job_id"]] = JobResult(job["job_id"], key, "cached", hit.job_dir, 0, "cache",
                                                   [hit.run_dir], 0)
            else:
                pending.append(job)
        if results:
            self.log(f"driver: {len(results)} cached, {len(pending)} to run")
        key_of = dict(zip(ids, keys))
        attempts = {j["job_id"]: 0 for j in pending}
        runs: dict[str, list[Path]] = {j["job_id"]: [] for j in pending}
        retry: list[dict] = []
        queue = list(pending)
        while queue or retry:
            if retry:
                batch, retry = [retry[0]], retry[1:]
            else:
                batch, queue = queue[:self.batch_size], queue[self.batch_size:]
            self.submitted.append([j["job_id"] for j in batch])
            try:
                result = self.backend.submit(self._request(batch))
                run_dir, launcher_exit, error = result.run_dir, result.exit_code, None
            except launcher.LauncherError as e:
                run_dir, launcher_exit, error = None, getattr(e, "code", launcher.EXIT_EXCEPTION), str(e)
            ran_any = False
            not_run = []
            for job in batch:
                jid = job["job_id"]
                code = None if run_dir is None else _job_exit(run_dir, jid)
                if run_dir is not None:
                    runs[jid].append(run_dir)
                if code is None and len(batch) > 1 and run_dir is not None:
                    not_run.append(job)  # the request ended before this job
                    continue
                ran_any = True
                attempts[jid] += 1
                if code == 0 and run_dir is not None and run_ok(run_dir):
                    entry = self.cache.store(key_of[jid], run_dir / jid, run_dir, jid, backend=self.backend.name)
                    results[jid] = JobResult(jid, key_of[jid], "ok", entry.job_dir, attempts[jid], self.backend.name,
                                             runs[jid], 0)
                elif attempts[jid] < 2:
                    retry.append(job)
                else:
                    results[jid] = JobResult(jid, key_of[jid], "failed", None if run_dir is None else run_dir / jid,
                                             attempts[jid], self.backend.name, runs[jid],
                                             code if code is not None else launcher_exit,
                                             error or f"job exit {code}, launcher exit {launcher_exit}")
            if not ran_any:  # nothing in the batch produced a result: count it as a failure of every job
                for job in not_run:
                    jid = job["job_id"]
                    attempts[jid] += 1
                    retry.append(job)
                not_run = []
            queue = not_run + queue
        return [results[i] for i in ids]
