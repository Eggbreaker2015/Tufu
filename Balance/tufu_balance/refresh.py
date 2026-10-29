"""Run cumulative L0/L1/L2 checks without accepting or writing golden baselines."""

from __future__ import annotations

import argparse
from collections import defaultdict
from concurrent.futures import ThreadPoolExecutor
from dataclasses import dataclass
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import shlex
import subprocess
import sys
import time
import uuid

REPO = Path(__file__).resolve().parents[2]
BALANCE = REPO / "Balance"


@dataclass(frozen=True)
class Step:
    name: str
    command: tuple[str, ...]


def plan(level: int, out: Path, *, rev="HEAD", python=sys.executable):
    """Return every concrete command in execution order (dry-run does not create output)."""
    def module(name, *args):
        return (python, "-m", name, *map(str, args))

    def internal(name):
        return Step(name, module("tufu_balance.refresh", "--step", name, "--out", out, "--rev", rev))

    steps = [internal("extract"), internal("graph"), internal("render_catalog"), internal("pytest-static"),
             Step("cscheck", module("tufu_balance.cscheck", "--worktree", REPO)),
             Step("drift", module("tufu_balance.drift", "--head", "worktree", "--out", out / "drift")),
             Step("golden-guard", module("tufu_balance.golden", "guard"))]
    if level >= 1:
        steps += [Step("unity-probe-edit-play", module("pytest", "-q", "--unity", "-m", "unity",
                                                      "tests/unity/test_differential.py", "--basetemp", out / "pytest-probe")),
                  internal("smoke-11"), internal("ingest-smoke-11"), internal("crosscheck-smoke-11"),
                  internal("baseline-smoke-11")]
    if level >= 2:
        steps += [internal("golden-405"), internal("ingest-golden-405"), internal("crosscheck-golden-405"),
                  internal("bake-report"), internal("empty-patch"), internal("baseline-golden-405"),
                  Step("pytest-unity-all", module("pytest", "-q", "--unity", "-m", "unity",
                                                 "--basetemp", out / "pytest-unity"))]
    steps += [Step("golden-current-differences", module("tufu_balance.golden", "status"))]
    return steps


def shard_nodes(nodes, workers):
    """Keep compile fixtures together; split long formula and fake-editor modules."""
    modules = defaultdict(list)
    for node in nodes:
        modules[node.split("::", 1)[0]].append(node)
    batches = []
    for module, tests in modules.items():
        count = min(2, workers) if Path(module).stem in (
            "test_unity_launcher", "test_params_import", "test_params_hash", "test_pipeline") else 1
        batches.extend(tests[i::count] for i in range(count) if tests[i::count])
    # Start external-process waits and the second extraction early instead of leaving a long tail.
    early = ("test_pipeline", "test_unity_launcher", "test_params_import", "test_params_hash",
             "test_extract_keydata", "test_cscheck")
    def priority(tests):
        module = Path(tests[0].split("::", 1)[0]).stem
        return (early.index(module) if module in early else len(early), -len(tests), tests[0])
    return sorted(batches, key=priority)


def pytest_worker(out, index):
    import pytest
    spec = json.loads((out / "static-shards.json").read_text())

    class GoldenSession:
        @pytest.hookimpl(trylast=True)
        def pytest_configure(self, config):
            # The P1 store locks merges; all shards must share the same measurement session.
            os.environ["TUFU_GOLDEN_RUN"] = spec["run_id"]

    return pytest.main(["-q", "-m", "not unity", "--durations=5", "--basetemp", str(out / f"pytest-static-{index}"),
                        "--junitxml", str(out / f"pytest-static-{index}.xml"), *spec["shards"][index]],
                       plugins=[GoldenSession()])


def static_checks(out, workers=None):
    """Collect the full non-Unity suite, then run independent pytest processes without extra dependencies."""
    collect = subprocess.run([sys.executable, "-m", "pytest", "--collect-only", "-q", "-m", "not unity"],
                             cwd=BALANCE, capture_output=True, text=True)
    (out / "pytest-collection.log").write_text(collect.stdout + collect.stderr)
    if collect.returncode:
        print(collect.stdout + collect.stderr)
        return collect.returncode
    nodes = [line for line in collect.stdout.splitlines() if line.startswith("tests/") and "::" in line]
    if not nodes or len(nodes) != len(set(nodes)):
        raise ValueError("pytest collection did not yield a unique, nonempty test inventory")
    workers = workers or min(6, os.cpu_count() or 1)
    shards = shard_nodes(nodes, workers)
    (out / "static-shards.json").write_text(json.dumps({"run_id": uuid.uuid4().hex, "shards": shards}, indent=2))

    def worker(index):
        p = subprocess.run([sys.executable, "-m", "tufu_balance.refresh", "--step", f"pytest-static-worker-{index}",
                            "--out", str(out)], cwd=BALANCE, capture_output=True, text=True)
        (out / f"pytest-static-{index}.log").write_text(p.stdout + p.stderr)
        print(f"shard {index}: {len(shards[index])} collected tests, exit {p.returncode}\n" + p.stdout[-1600:] + p.stderr[-1600:])
        return p.returncode

    with ThreadPoolExecutor(max_workers=workers) as pool:
        codes = list(pool.map(worker, range(len(shards))))
    print(f"{len(nodes)} collected tests in {len(shards)} shards; every node assigned exactly once")
    return next((code for code in codes if code), 0)


def fingerprint(root: Path):
    return {p.relative_to(root).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
            for p in sorted(root.rglob("*")) if p.is_file()}


def execute(steps, out: Path, *, env=None, run=subprocess.run, current=None):
    """Fail fast, retain command logs and baseline-difference output, check CURRENT after each step."""
    out.mkdir(parents=True, exist_ok=True)
    current = Path(current or BALANCE / "tests/golden/current")
    before = fingerprint(current)
    results = []
    started = time.monotonic()
    for step in steps:
        print(f"refresh: {step.name}", flush=True)
        t = time.monotonic()
        try:
            p = run(step.command, cwd=BALANCE, env=env, capture_output=True, text=True)
            code, output = p.returncode, p.stdout + p.stderr
        except (OSError, subprocess.SubprocessError) as e:
            code, output = 2, str(e)
        if fingerprint(current) != before:
            code = 1
            output += "\nFAIL: tests/golden/current changed during this step\n"
        (out / f"{step.name}.log").write_text(output, encoding="utf-8")
        result = {"name": step.name, "command": list(step.command), "exit_code": code,
                  "seconds": time.monotonic() - t}
        results.append(result)
        print(f"  exit {code}, {result['seconds']:.2f}s", flush=True)
        if code:
            print(output[-6000:], flush=True)
            break
    summary = {"steps": results, "planned_steps": len(steps), "seconds": time.monotonic() - started,
               "exit_code": next((r["exit_code"] for r in results if r["exit_code"]), 0),
               "current_unchanged": fingerprint(current) == before}
    (out / "summary.json").write_text(json.dumps(summary, indent=2) + "\n")
    lines = ["# 刷新结果", "", f"总耗时 {summary['seconds']:.2f} 秒；退出码 {summary['exit_code']}；"
             f"CURRENT 未变：{summary['current_unchanged']}。", "",
             "| 步骤 | 退出码 | 秒 | 日志 |", "|---|---|---|---|"]
    lines += [f"| {r['name']} | {r['exit_code']} | {r['seconds']:.2f} | [{r['name']}.log]({r['name']}.log) |"
              for r in results]
    if len(results) < len(steps):
        lines += ["", "未执行：" + "、".join(s.name for s in steps[len(results):]) + "。"]
    lines += ["", "## 当前基线差异", ""]
    log = out / "golden-current-differences.log"
    lines += ["```text", log.read_text() if log.exists() else "前序步骤失败，未比较。", "```"]
    for name in ("smoke-11", "golden-405"):
        path = out / f"baseline-{name}.json"
        if path.exists():
            lines += ["", f"### {name}", "", "```json", path.read_text(), "```"]
    (out / "summary.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    return summary


def extract(out: Path):
    """Extract into this refresh only; resolve LFS scenes into a private asset overlay."""
    from . import extract as E
    source = E.ASSETS
    overlay = out / "assets"
    overlay.mkdir()
    for entry in source.rglob("*"):
        target = overlay / entry.relative_to(source)
        if entry.is_dir():
            target.mkdir(exist_ok=True)
            continue
        if entry.is_file() and entry.suffix == ".unity" and entry.read_bytes().startswith(b"version https://git-lfs.github.com/spec/"):
            p = subprocess.run(["git", "lfs", "smudge"], input=entry.read_bytes(), cwd=REPO,
                               capture_output=True, env={**os.environ, "GIT_LFS_SKIP_SMUDGE": "0"})
            if p.returncode or p.stdout.startswith(b"version https://git-lfs.github.com/spec/"):
                raise ValueError(f"LFS scene not available: {entry.name}; {p.stderr.decode(errors='replace')}")
            target.write_bytes(p.stdout)
        else:
            target.symlink_to(entry)
    E.ASSETS = overlay
    sys.argv = ["extract", "--out", str(out / "extracted")]
    E.main()


def generated_docs(out: Path, step: str):
    if step == "graph":
        from . import graph as G
        G.DATABASE = out / "extracted/balance.sqlite"
        G.GRAPH_OUT = out / "graph.json"
        G.DOC = out / "docs/DependencyGraph.md"
        G.DOC.parent.mkdir(parents=True, exist_ok=True)
        graph = G.load()
        G.GRAPH_OUT.write_text(json.dumps(graph.to_json(), ensure_ascii=False, indent=1))
        G.render_doc(graph)
        print(f"{len(graph.nodes)} nodes; {len(graph.problems)} problems")
        if graph.problems:
            raise ValueError("\n".join(graph.problems))
    else:
        from . import render_catalog as R
        R.DOCS = out / "docs"
        R.main()


def run_preset(out, name, rev):
    from .unity import launcher, request
    req = request.preset(name)
    if name == "golden-405":
        from .unity.bake_report import bake_outputs
        for job in req["jobs"]:
            bake_outputs(job)
    result = launcher.run(req, rev=rev)
    print(result.describe())
    (out / f"{name}.json").write_text(json.dumps({"run_dir": str(result.run_dir), "exit_code": result.exit_code}))
    return result.exit_code


def job_dir(out, name):
    record = json.loads((out / f"{name}.json").read_text())
    return Path(record["run_dir"]) / name


def baseline(out, name):
    root = BALANCE / "tests/golden/current/sim_run"
    per_preset = any((root / preset).is_dir() for preset in ("smoke-11", "golden-405"))
    source = root / name if per_preset else root
    # Design 3.2's flat layout contains the full 405-day run, not an 11-day baseline.
    files = sorted(source.rglob("*.csv")) if source.exists() and (per_preset or name == "golden-405") else []
    differences = []
    for f in files:
        rel = f.relative_to(source)
        measured = job_dir(out, name) / rel
        if not measured.exists() or f.read_bytes() != measured.read_bytes():
            differences.append(rel.as_posix())
    record = {"csv_baseline_files": len(files), "differences": differences,
              "note": "逐字节比较" if files else "K3 尚未提供 sim_run CSV 基线；已有套件由 pytest/golden status 严格比较。"}
    (out / f"baseline-{name}.json").write_text(json.dumps(record, ensure_ascii=False, indent=2))
    print(json.dumps(record, ensure_ascii=False))
    return int(bool(differences))


def empty_patch(out, rev):
    from .unity import launcher, patcher, request
    jobs = [request.make_job("empty-baseline", day=405),
            patcher.with_patch(request.make_job("empty-patched", day=405), [])]
    result = launcher.run(request.make_request(jobs), rev=rev)
    if result.exit_code:
        print(result.describe())
        return result.exit_code
    files = ["simulation_data.json", "timechunk_full.json", "daily.csv", "digests.csv", "items_ext.csv", "snapshots_ext.csv"]
    differences = [f for f in files if result.file("empty-baseline", f) != result.file("empty-patched", f)]
    record = {"run_dir": str(result.run_dir), "files": files, "differences": differences}
    (out / "empty-patch.json").write_text(json.dumps(record, indent=2))
    print(json.dumps(record))
    return int(bool(differences))


def run_step(step, out, rev):
    if step == "extract":
        extract(out)
    elif step in ("graph", "render_catalog"):
        generated_docs(out, step)
    elif step == "pytest-static":
        return static_checks(out)
    elif step.startswith("pytest-static-worker-"):
        return pytest_worker(out, int(step.removeprefix("pytest-static-worker-")))
    elif step in ("smoke-11", "golden-405"):
        return run_preset(out, step, rev)
    elif step.startswith("ingest-"):
        from .unity.simio import ingest
        print(ingest(job_dir(out, step.removeprefix("ingest-"))))
    elif step.startswith("crosscheck-"):
        from .crosscheck.__main__ import main as crosscheck
        return crosscheck([str(job_dir(out, step.removeprefix("crosscheck-"))), "--checks",
                           "X1", "X2", "X3", "X4", "X5", "X6", "X7"])
    elif step.startswith("baseline-"):
        return baseline(out, step.removeprefix("baseline-"))
    elif step == "bake-report":
        from .unity.__main__ import main as unity
        return unity(["bake", str(job_dir(out, "golden-405"))])
    elif step == "empty-patch":
        return empty_patch(out, rev)
    else:
        raise ValueError(f"unknown refresh step: {step}")
    return 0


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--level", type=int, choices=(0, 1, 2), default=0)
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--out", type=Path)
    ap.add_argument("--rev", default="HEAD", help="committed revision for launcher runs")
    ap.add_argument("--step", help=argparse.SUPPRESS)
    a = ap.parse_args(argv)
    stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S.%fZ")
    out = (a.out or BALANCE / "out/refresh" / stamp).resolve()
    if not out.is_relative_to(BALANCE / "out"):
        ap.error("--out must be inside this worktree's Balance/out")
    if a.step:
        try:
            return run_step(a.step, out, a.rev)
        except (ValueError, OSError, RuntimeError, subprocess.SubprocessError) as e:
            print(f"refresh {a.step}: {e}", file=sys.stderr)
            return getattr(e, "code", 1)
    steps = plan(a.level, out, rev=a.rev)
    if a.dry_run:
        for s in steps:
            print(f"{s.name}: {shlex.join(s.command)}")
        return 0
    if out.exists():
        ap.error("--out already exists; use a new refresh directory")
    env = dict(os.environ)
    # A newly extracted private database is the input to every subsequent check.
    env["TUFU_BALANCE_DB"] = str(out / "extracted/balance.sqlite")
    env["TUFU_EXTRACT_ASSETS"] = str(out / "assets")
    env["TUFU_UNITY_REV"] = a.rev
    (out / "tmp").mkdir(parents=True)
    env["TMPDIR"] = str(out / "tmp")
    result = execute(steps, out, env=env)
    print(out / "summary.md")
    return result["exit_code"]


if __name__ == "__main__":
    sys.exit(main())
