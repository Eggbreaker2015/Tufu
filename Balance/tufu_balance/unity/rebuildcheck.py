"""Runs the UnityEngine-free rebuild sources under .NET (``Balance/tools/rebuildcheck``; unit C3c).

``SimulationRandom.cs`` and ``SimulationRebuildParams*.cs`` (not the asset file) compile into a small console
tool, so the C# random streams, canonical JSON, ``params_hash`` and the parameter parsing are tested against
the Python side without Unity. The editor's Mono runs the same code; Unity runs (tests/unity/test_params.py)
confirm the hashes there.
"""

from __future__ import annotations

import fcntl
import json
import subprocess
from pathlib import Path
from typing import Any, Mapping, Sequence

from .. import cscheck

TOOL_DIR = cscheck.BALANCE / "tools" / "rebuildcheck"
TOOL_DLL = TOOL_DIR / "bin" / "Release" / "net8.0" / "rebuildcheck.dll"
REBUILD = cscheck.WORKTREE / "Recovery" / "StudyProject" / "Assets" / "Scripts" / "Assembly-CSharp" / "CodeBase" / "Simulation" / "Rebuild"


def available() -> bool:
    try:
        cscheck.dotnet()
    except cscheck.CsCheckError:
        return False
    return True


def sources() -> list[Path]:
    linked = [REBUILD / "SimulationRandom.cs"] + sorted(p for p in REBUILD.glob("SimulationRebuildParams*.cs")
                                                        if p.name != "SimulationRebuildParamsAsset.cs")
    return [TOOL_DIR / "rebuildcheck.csproj", TOOL_DIR / "Program.cs", *linked]


def ensure_built() -> Path:
    def stale():
        return not TOOL_DLL.exists() or any(p.stat().st_mtime > TOOL_DLL.stat().st_mtime for p in sources())

    if stale():
        (TOOL_DIR / "obj").mkdir(exist_ok=True)
        with open(TOOL_DIR / "obj" / ".build.lock", "w") as lock:
            fcntl.flock(lock, fcntl.LOCK_EX)
            if stale():
                proc = subprocess.run([cscheck.dotnet(), "build", str(TOOL_DIR / "rebuildcheck.csproj"), "-c", "Release",
                                       "--nologo", "-v", "q"], capture_output=True, text=True)
                if proc.returncode != 0:
                    raise RuntimeError("rebuildcheck build failed:\n" + proc.stdout[-4000:] + proc.stderr[-2000:])
                TOOL_DLL.touch()
    return TOOL_DLL


def run(commands: Sequence[Mapping[str, Any]]) -> list[dict]:
    return run_text(json.dumps(list(commands), allow_nan=True))


def run_text(commands_json: str) -> list[dict]:
    """As :func:`run`, with the commands as JSON text (to send number spellings Python would normalise)."""
    proc = subprocess.run([cscheck.dotnet(), str(ensure_built())], input=commands_json, capture_output=True, text=True)
    if proc.returncode != 0:
        raise RuntimeError("rebuildcheck failed:\n" + proc.stderr[-4000:])
    return json.loads(proc.stdout)
