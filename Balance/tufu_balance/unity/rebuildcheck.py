"""Runs the UnityEngine-free rebuild sources outside Unity (``Balance/tools/rebuildcheck``; unit C3c).

``SimulationRandom.cs`` and ``SimulationRebuildParams*.cs`` (not the asset file) compile into a small console
tool, so the C# random streams, canonical JSON, ``params_hash`` and the parameter parsing are tested against
the Python side without Unity. Two runtimes: ``"dotnet"`` (.NET 8) and ``"mono"``, the MonoBleedingEdge of the
editor the launcher uses, whose double parser rounds some literals 1 ulp off (C3c review). Unity runs
(tests/unity/test_params.py) confirm the hashes in the editor itself.
"""

from __future__ import annotations

import fcntl
import json
import os
import shutil
import subprocess
from pathlib import Path
from typing import Any, Mapping, Sequence

from .. import cscheck

TOOL_DIR = cscheck.BALANCE / "tools" / "rebuildcheck"
TOOL_DLL = TOOL_DIR / "bin" / "Release" / "net8.0" / "rebuildcheck.dll"
REBUILD = cscheck.WORKTREE / "Recovery" / "StudyProject" / "Assets" / "Scripts" / "Assembly-CSharp" / "CodeBase" / "Simulation" / "Rebuild"


MONO_EXE = TOOL_DIR / "bin" / "mono" / "rebuildcheck.exe"
RUNTIMES = ("dotnet", "mono")


def available(runtime: str = "dotnet") -> bool:
    """The runtime (and for .NET the SDK) is installed and the tool's sources are present (tests skip otherwise)."""
    if runtime == "mono":
        if mono_home() is None:
            return False
    else:
        try:
            cscheck.dotnet()
        except cscheck.CsCheckError:
            return False
    return all(p.exists() for p in sources())


def mono_home() -> Path | None:
    """MonoBleedingEdge of the editor the launcher runs (TUFU_UNITY, else launcher.DEFAULT_UNITY)."""
    from .launcher import DEFAULT_UNITY

    unity = Path(os.environ.get("TUFU_UNITY", DEFAULT_UNITY))
    home = unity.parents[1] / "Resources" / "Scripting" / "MonoBleedingEdge"
    ok = all((home / p).exists() for p in ("bin/mono", "lib/mono/4.5/csc.exe", "lib/mono/4.5/Facades/netstandard.dll"))
    return home if ok else None


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


def ensure_built_mono() -> tuple[Path, Path]:
    """The editor's mono binary (default ``net_4_x-macos`` class libraries, the same Mono build as the editor's
    scripting runtime) and the tool compiled by that Mono's C# compiler."""
    home = mono_home()
    if home is None:
        raise RuntimeError("the editor's MonoBleedingEdge was not found")
    mono = home / "bin" / "mono"

    def stale():
        return not MONO_EXE.exists() or any(p.stat().st_mtime > MONO_EXE.stat().st_mtime for p in sources())

    if stale():
        (TOOL_DIR / "obj").mkdir(exist_ok=True)
        with open(TOOL_DIR / "obj" / ".build-mono.lock", "w") as lock:
            fcntl.flock(lock, fcntl.LOCK_EX)
            if stale():
                MONO_EXE.parent.mkdir(parents=True, exist_ok=True)
                newtonsoft = cscheck.WORKTREE / "Recovery" / "StudyProject" / "Assets" / "Plugins" / "Newtonsoft.Json.dll"
                shutil.copy2(newtonsoft, MONO_EXE.parent / "Newtonsoft.Json.dll")
                cs = [str(p) for p in sources() if p.suffix == ".cs"]
                proc = subprocess.run([str(home / "bin" / "mono"), str(home / "lib" / "mono" / "4.5" / "csc.exe"), "-nologo",
                                       "-langversion:9.0", "-nowarn:CS1702", "-target:exe", f"-out:{MONO_EXE}",
                                       f"-r:{MONO_EXE.parent / 'Newtonsoft.Json.dll'}", "-r:System.Numerics.dll",
                                       f"-r:{home / 'lib' / 'mono' / '4.5' / 'Facades' / 'netstandard.dll'}", *cs],
                                      capture_output=True, text=True)
                if proc.returncode != 0:
                    raise RuntimeError("rebuildcheck (mono) build failed:\n" + proc.stdout[-4000:] + proc.stderr[-2000:])
                MONO_EXE.touch()
    return mono, MONO_EXE


def runtime_banner(runtime: str) -> str:
    """The runtime's own version line (recorded in test output)."""
    if runtime == "mono":
        mono, _ = ensure_built_mono()
        return subprocess.run([str(mono), "--version"], capture_output=True, text=True).stdout.splitlines()[0]
    return "dotnet " + subprocess.run([cscheck.dotnet(), "--version"], capture_output=True, text=True).stdout.strip()


def run(commands: Sequence[Mapping[str, Any]], runtime: str = "dotnet") -> list[dict]:
    return run_text(json.dumps(list(commands), allow_nan=True), runtime)


def run_text(commands_json: str, runtime: str = "dotnet") -> list[dict]:
    """As :func:`run`, with the commands as JSON text (to send number spellings Python would normalise)."""
    if runtime == "mono":
        mono, exe = ensure_built_mono()
        argv = [str(mono), "--debug", str(exe)]
    elif runtime == "dotnet":
        argv = [cscheck.dotnet(), str(ensure_built())]
    else:
        raise ValueError(f"unknown runtime {runtime!r}")
    proc = subprocess.run(argv, input=commands_json.encode("utf-8"), capture_output=True)
    if proc.returncode != 0:
        raise RuntimeError(f"rebuildcheck ({runtime}) failed:\n" + proc.stderr.decode("utf-8", "replace")[-4000:])
    return json.loads(proc.stdout.decode("utf-8"))
