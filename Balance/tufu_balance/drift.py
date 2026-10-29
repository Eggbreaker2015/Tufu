"""Report formula references touched by a git diff, in base-revision line coordinates."""

from __future__ import annotations

import argparse
from collections import Counter
import csv
import io
import json
from pathlib import Path
import posixpath
import re
import subprocess
import sys
import tarfile
import tempfile
import warnings

REPO = Path(__file__).resolve().parents[2]
CODEBASE = "Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase"
SCRIPTS = "Recovery/StudyProject/Assets/Scripts"
PACKAGES = "Recovery/StudyProject/Packages"
REF = re.compile(r"^(.+\.cs)(?::(\d+(?:-\d+)?(?:,\d+(?:-\d+)?)*))?$")
HUNK = re.compile(r"^@@ -(\d+)(?:,(\d+))? \+\d+(?:,\d+)? @@", re.M)

# Conservative subsystem recommendations, not a claim of per-formula test coverage.
SUITES = {
    "stats": "DP1 DP3 X1 X2 X3 X7 G11",
    "combat": "DP2 DP8 X2 X3 X5 X7 G20 G21",
    "spawners": "DP5 DP6 DP8 X4 X5 X6 G13 G15 G16 G17 G17b",
    "items": "DP7 DP8 X2 X3 G07 G08 G09 G10 G12 G18 G18b G21",
    "simulation": "DP3 DP4 DP5 DP8 X1 X2 X3 X4 X5 X6 X7 G11 G14 G19 G20 G22 G23",
    "powerups": "DP1 DP2 X1 X2",
    "meta": "DP1 X1",
}


def git(repo: Path, *args: str, binary: bool = False):
    p = subprocess.run(["git", "--no-pager", "-C", str(repo), *args],
                       capture_output=True, text=not binary)
    if p.returncode:
        err = p.stderr.decode(errors="replace") if binary else p.stderr
        raise ValueError(err.strip())
    return p.stdout


def normalize_ref(ref: str, files: set[str]):
    """Resolve catalog paths only against the base tree; repair the eight stray ../ prefixes."""
    match = REF.fullmatch(ref)
    if not match:
        raise ValueError("invalid code reference syntax")
    path, spans = match.groups()
    while path.startswith("../"):
        path = path[3:]
    if path.startswith("CodeBase/"):
        path = path[len("CodeBase/"):]
    if not path.startswith(CODEBASE + "/"):
        path = CODEBASE + "/" + path
    path = posixpath.normpath(path)
    if not path.startswith(CODEBASE + "/") or path not in files:
        raise ValueError("file absent from base CodeBase tree")
    ranges = []
    for span in spans.split(",") if spans else ():
        lo, _, hi = span.partition("-")
        start, end = int(lo), int(hi or lo)
        if start < 1 or end < start:
            raise ValueError("invalid line range")
        ranges.append((start, end))
    return path, ranges


def overlaps(ranges, hunks):
    """Insertions after old line n belong between n and n+1, including a range's interior only."""
    for start, count in hunks:
        for lo, hi in ranges:
            if (lo <= start < hi) if count == 0 else (lo <= start + count - 1 and start <= hi):
                return True
    return False


def changed_lines(repo: Path, base: str, head: str):
    args = ["diff", "--no-color", "--no-ext-diff", "--no-textconv", "--no-renames"]
    revisions = [base] if head == "worktree" else [base, head]
    names = git(repo, *args, "--name-only", "-z", *revisions, "--", CODEBASE).split("\0")
    out = {}
    for path in names:
        if not path.endswith(".cs"):
            continue
        diff = git(repo, *args, "-U0", *revisions, "--", path)
        out[path] = [(int(m[1]), int(m[2] or 1)) for m in HUNK.finditer(diff)]
        if not out[path]:
            in_base = bool(git(repo, "ls-tree", "--name-only", base, "--", path).strip())
            in_head = (repo / path).exists() if head == "worktree" else bool(
                git(repo, "ls-tree", "--name-only", head, "--", path).strip())
            if in_base and in_head:
                raise ValueError(f"modified C# file has no diff hunks: {path}")
    if head == "worktree":
        # New untracked files have no old coordinates, but still belong in the file inventory.
        for path in git(repo, "ls-files", "--others", "--exclude-standard", "-z", "--", CODEBASE).split("\0"):
            if path.endswith(".cs"):
                out.setdefault(path, [])
    return out


def match_formulas(formulas, files, changes):
    hits, unresolved, normalized = [], [], []
    for formula in formulas:
        touched = []
        for raw in formula.get("code_refs", []):
            try:
                path, ranges = normalize_ref(raw, files)
            except ValueError as e:
                unresolved.append({"id": formula["id"], "ref": raw, "reason": str(e)})
                continue
            if raw.startswith("../"):
                normalized.append({"id": formula["id"], "ref": raw, "path": path})
            if path in changes:
                touched.append({"ref": raw, "path": path, "ranges": ranges, "hunks": changes[path],
                                "match": "line" if ranges and overlaps(ranges, changes[path]) else
                                         "file_only" if ranges else "file_unbounded"})
        if touched:
            hits.append({"id": formula["id"], "status": formula.get("status", ""),
                         "subsystem": formula.get("subsystem", ""), "refs": touched,
                         "line_hit": any(r["match"] == "line" for r in touched),
                         "tests": SUITES.get(formula.get("subsystem"), "DP1-DP8 X1-X7"),
                         "python": [], "csharp": []})
    return hits, unresolved, normalized


def enrich(hits, source: Path, catalog):
    from .model import registry as R
    with warnings.catch_warnings():
        warnings.simplefilter("ignore", R.PendingFormulaWarning)
        R.import_model_modules()
    aliases = catalog.get("aliases", {})
    annotations = R.scan_csharp([source / SCRIPTS], repo=source)
    for hit in hits:
        hit["python"] = sorted({f"{i.module}.{i.qualname} ({i.provenance})"
                                for i in R.REGISTRY.implementers(hit["id"])})
        hit["csharp"] = sorted({f"{a.type_name}.{a.member} ({a.provenance}; {a.file}:{a.line})"
                                for a in annotations if aliases.get(a.formula_id, a.formula_id) == hit["id"]})


def signature_check(source: Path, out: Path):
    """Use P0's CLI, including its exit status and machine-readable violations."""
    p = subprocess.run([sys.executable, "-m", "tufu_balance.cscheck", "--signature", "--worktree", str(source),
                        "--json", str(out)], capture_output=True, text=True, cwd=REPO / "Balance")
    return {"exit_code": p.returncode, "output": p.stdout + p.stderr,
            "result": json.loads(out.read_text()) if out.exists() else None}


def snapshot_sources(repo: Path, rev: str, dest: Path):
    """Materialize only C# inputs from git objects, without checking out another branch."""
    paths = git(repo, "ls-tree", "--name-only", rev, "--", SCRIPTS, PACKAGES).splitlines()
    if not paths:
        return
    data = git(repo, "archive", rev, "--", *paths, binary=True)
    with tarfile.open(fileobj=io.BytesIO(data)) as archive:
        archive.extractall(dest, filter="data")


def check(repo: Path = REPO, *, base="6af011f", head="worktree", catalog=None, out=None):
    repo = Path(repo).resolve()
    base_sha = git(repo, "rev-parse", "--verify", base + "^{commit}").strip()
    head_sha = git(repo, "rev-parse", "--verify", ("HEAD" if head == "worktree" else head) + "^{commit}").strip()
    out = Path(out or REPO / "Balance/out/drift" / ("worktree" if head == "worktree" else head_sha[:12]))
    out.mkdir(parents=True, exist_ok=True)
    catalog = catalog or json.loads((repo / "Balance/catalog/formulas.json").read_text())
    files = set(git(repo, "ls-tree", "-r", "--name-only", base_sha, "--", CODEBASE).splitlines())
    changes = changed_lines(repo, base_sha, "worktree" if head == "worktree" else head_sha)
    hits, unresolved, normalized = match_formulas(catalog["formulas"], files, changes)
    with tempfile.TemporaryDirectory(prefix="sources-", dir=out) as tmp:
        source = repo if head == "worktree" else Path(tmp)
        if head != "worktree":
            snapshot_sources(repo, head_sha, source)
        enrich(hits, source, catalog)
        signature = signature_check(source, out / "signature.json")
    result = {"base": base_sha, "head": head, "head_rev": head_sha, "changed_files": changes,
              "hits": hits, "unresolved": unresolved, "normalized": normalized, "signature": signature,
              "counts": {"file_hits": len(hits), "line_hits": sum(h["line_hit"] for h in hits),
                         "file_only": sum(not h["line_hit"] for h in hits), "unresolved": len(unresolved),
                         "normalized": len(normalized),
                         "by_status": dict(Counter(h["status"] for h in hits)),
                         "by_subsystem": dict(Counter(h["subsystem"] for h in hits))}}
    write_report(result, out)
    return result


def write_report(result, out):
    (out / "report.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n")
    with (out / "drift.csv").open("w", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(["id", "status", "subsystem", "match", "references", "python", "csharp", "tests"])
        for h in result["hits"]:
            writer.writerow([h["id"], h["status"], h["subsystem"], "line" if h["line_hit"] else "file_only",
                             json.dumps(h["refs"], ensure_ascii=False), "; ".join(h["python"]),
                             "; ".join(h["csharp"]), h["tests"]])
    lines = ["# 代码漂移检查", "", f"基准：`{result['base']}`；目标：`{result['head']}` (`{result['head_rev']}`)。",
             "", f"文件级 {result['counts']['file_hits']} 条；行段级 {result['counts']['line_hits']} 条。",
             "行号属于基准版本。纯插入在相邻旧行之间；无行号引用只计文件级。测试建议按子系统保守展开，不代表逐公式覆盖。",
             "", "| 公式 | 状态 | 子系统 | 命中 | Python | C# | 重跑建议 |", "|---|---|---|---|---|---|---|"]
    for h in result["hits"]:
        cells = [h["id"], h["status"], h["subsystem"], "行段" if h["line_hit"] else "同文件",
                 "; ".join(h["python"]) or "未登记", "; ".join(h["csharp"]) or "未登记", h["tests"]]
        lines.append("| " + " | ".join(c.replace("|", "\\|").replace("\n", " ") for c in cells) + " |")
    lines += ["", "## 规范化引用", ""]
    lines += [f"- `{r['id']}`: `{r['ref']}` → `{r['path']}`" for r in result["normalized"]] or ["无。"]
    lines += ["", "## 无法解析的引用", ""]
    lines += [f"- `{r['id']}`: `{r['ref']}`：{r['reason']}" for r in result["unresolved"]] or ["无。"]
    lines += ["", "## 签名锁（cscheck --signature）", "", "```text", result["signature"]["output"].strip(), "```", ""]
    (out / "report.md").write_text("\n".join(lines), encoding="utf-8")


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--base", default="6af011f")
    ap.add_argument("--head", default="HEAD")
    ap.add_argument("--strict", action="store_true")
    ap.add_argument("--repo", type=Path, default=REPO)
    ap.add_argument("--out", type=Path)
    a = ap.parse_args(argv)
    try:
        result = check(a.repo, base=a.base, head=a.head, out=a.out)
    except (ValueError, OSError, subprocess.SubprocessError) as e:
        print(f"drift: {e}", file=sys.stderr)
        return 2
    print(json.dumps(result["counts"], ensure_ascii=False))
    print(result["signature"]["output"].strip())
    return result["signature"]["exit_code"] or int(a.strict and bool(result["hits"]))


if __name__ == "__main__":
    sys.exit(main())
