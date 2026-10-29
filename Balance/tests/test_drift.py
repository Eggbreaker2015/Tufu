"""Base-coordinate line matching, git worktree coverage, and the real P0 signature hook."""

import csv
import json
import shutil

import pytest

from tufu_balance import cscheck, drift as D


def git(repo, *args):
    return D.git(repo, *args).strip()


def commit(repo, message):
    git(repo, "add", ".")
    git(repo, "-c", "user.name=Drift test", "-c", "user.email=drift@example.invalid", "commit", "-qm", message)
    return git(repo, "rev-parse", "HEAD")


@pytest.fixture
def history(tmp_path):
    repo = tmp_path / "repo"
    repo.mkdir()
    git(repo, "init", "-q")
    path = repo / D.CODEBASE / "Unit/Stat.cs"
    path.parent.mkdir(parents=True)
    path.write_text("".join(f"line {i}\n" for i in range(1, 31)))
    base = commit(repo, "base")
    lines = path.read_text().splitlines(keepends=True)
    lines[4] = "changed five\n"
    lines[19:21] = []
    path.write_text("header one\nheader two\n" + "".join(lines))
    head = commit(repo, "change lines 5 and 20-21, insert header")
    return repo, path, base, head


def catalog():
    def f(name, *refs):
        return {"id": "stats." + name, "status": "reconstructed", "subsystem": "stats", "code_refs": list(refs)}
    return {"formulas": [f("five", "Unit/Stat.cs:5"), f("twenty", "../Unit/Stat.cs:20-21"),
                         f("elsewhere", "Unit/Stat.cs:10-15"), f("multi", "Unit/Stat.cs:2,4-5,25"),
                         f("whole", "CodeBase/Unit/Stat.cs"), f("missing", "Unit/Missing.cs:4"),
                         f("escape", "Unit/../../Outside.cs:1"), f("malformed", "Unit/Stat.cs:9-2")]}


def test_base_coordinates_file_and_range_matches(history):
    repo, path, base, head = history
    changes = D.changed_lines(repo, base, head)
    assert changes[path.relative_to(repo).as_posix()] == [(0, 0), (5, 1), (20, 2)]
    files = set(git(repo, "ls-tree", "-r", "--name-only", base).splitlines())
    hits, unresolved, normalized = D.match_formulas(catalog()["formulas"], files, changes)
    assert {h["id"] for h in hits} == {"stats.five", "stats.twenty", "stats.elsewhere", "stats.multi", "stats.whole"}
    assert {h["id"] for h in hits if h["line_hit"]} == {"stats.five", "stats.twenty", "stats.multi"}
    assert {u["id"] for u in unresolved} == {"stats.missing", "stats.escape", "stats.malformed"}
    assert len(normalized) == 1 and normalized[0]["path"].endswith("/Unit/Stat.cs")
    assert next(h for h in hits if h["id"] == "stats.whole")["refs"][0]["match"] == "file_unbounded"


def test_git_color_always_preserves_line_hits(history):
    repo, path, base, head = history
    git(repo, "config", "color.ui", "always")
    changes = D.changed_lines(repo, base, head)
    assert changes[path.relative_to(repo).as_posix()] == [(0, 0), (5, 1), (20, 2)]
    files = set(git(repo, "ls-tree", "-r", "--name-only", base).splitlines())
    hits, _, _ = D.match_formulas(catalog()["formulas"], files, changes)
    assert {h["id"] for h in hits if h["line_hit"]} == {"stats.five", "stats.twenty", "stats.multi"}


@pytest.mark.parametrize("worktree", [False, True])
def test_modified_existing_file_without_hunks_is_an_error(history, worktree):
    repo, path, _, base = history
    git(repo, "config", "core.filemode", "true")
    path.chmod(0o755)
    head = "worktree" if worktree else commit(repo, "mode-only change")
    with pytest.raises(ValueError, match="modified C# file has no diff hunks"):
        D.changed_lines(repo, base, head)


def test_empty_new_and_deleted_files_do_not_require_hunks(history):
    repo, path, _, _ = history
    empty = path.parent / "Empty.cs"
    empty.touch()
    base = commit(repo, "empty file")
    empty.unlink()
    added = path.parent / "NewEmpty.cs"
    added.touch()
    head = commit(repo, "replace empty file")
    changes = D.changed_lines(repo, base, head)
    assert changes == {empty.relative_to(repo).as_posix(): [], added.relative_to(repo).as_posix(): []}


@pytest.mark.parametrize("start,count,expected", [(9, 0, False), (10, 0, True), (19, 0, True),
                                                    (20, 0, False), (9, 1, False), (9, 2, True),
                                                    (20, 1, True), (21, 1, False)])
def test_insertion_and_deletion_boundaries(start, count, expected):
    assert D.overlaps([(10, 20)], [(start, count)]) == expected


def test_worktree_includes_committed_staged_unstaged_deleted_and_untracked(history):
    repo, path, base, head = history
    staged = path.parent / "Staged.cs"
    staged.write_text("new file\n")
    git(repo, "add", str(staged))
    path.write_text(path.read_text().replace("line 12", "unstaged twelve"))
    untracked = path.parent / "Untracked.cs"
    untracked.write_text("untracked\n")
    changes = D.changed_lines(repo, base, "worktree")
    key = path.relative_to(repo).as_posix()
    assert (12, 1) in changes[key] and (5, 1) in changes[key]
    assert staged.relative_to(repo).as_posix() in changes
    assert untracked.relative_to(repo).as_posix() in changes
    path.unlink()
    assert D.changed_lines(repo, base, "worktree")[key] == [(1, 30)]


def test_rename_retains_base_reference_hit(history):
    repo, path, base, _ = history
    path.rename(path.with_name("Renamed.cs"))
    head = commit(repo, "rename")
    changes = D.changed_lines(repo, base, head)
    assert changes[path.relative_to(repo).as_posix()] == [(1, 30)]


def test_reports_and_strict_exit(history, tmp_path, monkeypatch):
    repo, _, base, head = history
    root = repo / "Balance/catalog"
    root.mkdir(parents=True)
    (root / "formulas.json").write_text(json.dumps(catalog()))
    monkeypatch.setattr(D, "enrich", lambda *a: None)
    calls = []
    def signature(source, out):
        calls.append(source)
        return {"exit_code": 0, "output": "signature 0 violations", "result": {}}
    monkeypatch.setattr(D, "signature_check", signature)
    out = tmp_path / "report"
    args = ["--repo", str(repo), "--base", base, "--head", head, "--out", str(out)]
    assert D.main(args) == 0
    assert D.main(args + ["--strict"]) == 1
    r = json.loads((out / "report.json").read_text())
    assert r["counts"]["file_hits"] == 5 and r["counts"]["line_hits"] == 3
    assert len(list(csv.DictReader((out / "drift.csv").open()))) == 5
    text = (out / "report.md").read_text()
    assert "stats.missing" in text and "DP1" in text and "signature 0 violations" in text
    assert len(calls) == 2
    monkeypatch.setattr(D, "signature_check", lambda *a: {"exit_code": 1, "output": "violation", "result": {}})
    assert D.main(args) == 1


def test_invalid_revision_returns_tool_error(history, tmp_path):
    repo, _, base, _ = history
    assert D.main(["--repo", str(repo), "--base", base, "--head", "missing-ref", "--out", str(tmp_path / "out")]) == 2


def test_signature_cli_detects_member_change(tmp_path):
    """Run P0's actual --signature command, not a simulated verdict."""
    try:
        cscheck.dotnet()
    except cscheck.CsCheckError:
        pytest.skip("requires the P0 dotnet tool")
    root = tmp_path / "changed"
    for rel in (D.SCRIPTS, D.PACKAGES):
        shutil.copytree(D.REPO / rel, root / rel)
    path = root / D.CODEBASE / "Simulation/Systems/SimulationSystem.cs"
    text = path.read_text()
    assert "partial class SimulationSystem" in text
    pos = text.index("{", text.index("partial class SimulationSystem"))
    path.write_text(text[:pos + 1] + "\npublic void P13SignatureViolation() {}\n" + text[pos + 1:])
    r = D.signature_check(root, tmp_path / "signature.json")
    assert r["exit_code"] == 1, r["output"]
    violations = r["result"]["signature"]["violations"]
    assert any("P13SignatureViolation" in v["member"] for v in violations)


def test_historical_sources_are_from_selected_ref(history, tmp_path):
    repo, path, base, head = history
    D.snapshot_sources(repo, base, tmp_path / "base")
    D.snapshot_sources(repo, head, tmp_path / "head")
    rel = path.relative_to(repo)
    assert (tmp_path / "base" / rel).read_text().startswith("line 1\n")
    assert (tmp_path / "head" / rel).read_text().startswith("header one\n")
