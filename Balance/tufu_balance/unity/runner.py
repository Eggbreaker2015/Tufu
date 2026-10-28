"""Run worktree, code version and run_key (design 1.4.2, 2.2 item 3, 2.10; unit C0b).

Unity only opens the dedicated run worktree, checked out (detached) at a commit with no uncommitted
or untracked files. ``run_key`` hashes the result-relevant request, the commit, the tree hashes of
``Recovery/StudyProject/{Assets,ProjectSettings,Packages}``, the Unity version and the protected DLL
hashes; ``--dev`` runs hash the dirty state of the development worktree instead.
"""

from __future__ import annotations

import hashlib
import os
import plistlib
import subprocess
from pathlib import Path
from typing import Any, Mapping

from . import request as req

BALANCE = Path(__file__).resolve().parents[2]
WORKTREE = BALANCE.parent
DEFAULT_RUN_WORKTREE = Path("/Users/joyfort/MyProjects/Tufu-stage5-run")
MAIN_CHECKOUT = Path("/Users/joyfort/MyProjects/Tufu")
PROJECT_SUBDIR = Path("Recovery/StudyProject")
TREE_DIRS = ("Assets", "ProjectSettings", "Packages")
PROTECTED_DLLS = ("Unity.Mathematics.dll", "Unity.Burst.dll")


class WorktreeError(RuntimeError):
    pass


def run_worktree() -> Path:
    return Path(os.environ.get("TUFU_RUN_WORKTREE", DEFAULT_RUN_WORKTREE))


def project_dir(worktree: Path) -> Path:
    return Path(worktree) / PROJECT_SUBDIR


def git(worktree: Path, *args: str, check: bool = True) -> str:
    proc = subprocess.run(["git", "-C", str(worktree), *args], capture_output=True, text=True)
    if check and proc.returncode != 0:
        raise WorktreeError(f"git {' '.join(args)} failed in {worktree}: {proc.stderr.strip()}")
    return proc.stdout


def porcelain(worktree: Path) -> str:
    """``git status --porcelain -uall``: empty means clean (ignored files such as Library/Temp do not count)."""
    return git(worktree, "status", "--porcelain", "-uall")


def require_clean(worktree: Path, what: str = "run worktree") -> None:
    status = porcelain(worktree)
    if status.strip():
        lines = status.strip().splitlines()
        raise WorktreeError(f"{what} {worktree} is not clean ({len(lines)} entries):\n" + "\n".join(lines[:20]))


def resolve(worktree: Path, rev: str) -> str:
    return git(worktree, "rev-parse", "--verify", f"{rev}^{{commit}}").strip()


def head(worktree: Path) -> str:
    return resolve(worktree, "HEAD")


def checkout(run_tree: Path, rev: str) -> str:
    """Detach the run worktree at ``rev`` (a commit of any branch: the object store is shared)."""
    require_clean(run_tree)
    sha = resolve(run_tree, rev)
    if head(run_tree) != sha:
        git(run_tree, "checkout", "--quiet", "--detach", sha)
    require_clean(run_tree)
    if head(run_tree) != sha:
        raise WorktreeError(f"run worktree did not move to {sha}")
    return sha


def tree_hashes(worktree: Path, rev: str) -> dict[str, str]:
    return {name: git(worktree, "rev-parse", f"{rev}:{PROJECT_SUBDIR / name}").strip() for name in TREE_DIRS}


def sha256_file(path: Path) -> str | None:
    path = Path(path)
    if not path.is_file():
        return None
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def dll_hashes(project: Path) -> dict[str, str | None]:
    return {name: sha256_file(Path(project) / "Assets" / "Plugins" / name) for name in PROTECTED_DLLS}


def dev_state(worktree: Path) -> dict[str, Any]:
    """Dirty state of a development worktree for ``--dev`` runs: porcelain plus content hashes of
    every changed or untracked file under Recovery/StudyProject."""
    status = porcelain(worktree)
    files = {}
    for line in status.splitlines():
        path = line[3:]
        if " -> " in path:
            path = path.split(" -> ", 1)[1]
        path = path.strip('"')
        if path.startswith(str(PROJECT_SUBDIR) + "/"):
            files[path] = sha256_file(Path(worktree) / path)
    return {"head": head(worktree), "porcelain": status, "files": dict(sorted(files.items()))}


def unity_version(unity_exe: Path) -> str:
    """Full editor version string from the app bundle's Info.plist (e.g. ``6000.6.3f1 (45d8eee7de74)``)."""
    app = Path(unity_exe)
    while app.suffix != ".app" and app != app.parent:
        app = app.parent
    info = app / "Contents" / "Info.plist"
    if not info.exists():
        return f"unknown:{unity_exe}"
    with open(info, "rb") as f:
        data = plistlib.load(f)
    version, build = data.get("CFBundleVersion"), data.get("UnityBuildNumber")
    return f"{version} ({build})" if build else str(version)


def run_key(request: Mapping[str, Any], *, code_rev: str, trees: Mapping[str, str], unity: str,
            dlls: Mapping[str, str | None], dev: Mapping[str, Any] | None = None) -> str:
    material = {
        "request": req.result_relevant(request),
        "code_rev": code_rev,
        "trees": dict(trees),
        "unity": unity,
        "dlls": dict(dlls),
        "dev": dev,
    }
    return hashlib.sha256(req.canonical_json(material).encode()).hexdigest()
