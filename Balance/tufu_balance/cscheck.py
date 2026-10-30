"""Offline C# check (design 1.6): runs Balance/tools/cscheck (.NET 8 + Roslyn) and reports its JSON.

Compiles this worktree's Assembly-CSharp and Assembly-CSharp-Editor with the references, defines and
language options of the .rsp files Unity left in the run worktree's Library, compiles Assembly-CSharp
again without UNITY_EDITOR, checks the signature lock against tools/cscheck/signature_6af011f.json and
the banned APIs in rebuild code. Needs no Unity and takes no lock.

Usage (from Balance/):
    .venv/bin/python -m tufu_balance.cscheck                      # everything
    .venv/bin/python -m tufu_balance.cscheck --signature --bans   # only the selected steps
    .venv/bin/python -m tufu_balance.cscheck --generate-signature # rewrite signature_6af011f.json from 6af011f
    .venv/bin/python -m tufu_balance.cscheck --update-baseline    # accept current warnings / no-editor errors
Steps: --compile (both assemblies, with UNITY_EDITOR), --no-editor-define, --signature, --bans.
Exit code 0 ok, 1 violations, 2 tool error (missing .rsp/DLLs, Packages differ, build failure).
"""

import argparse
import fcntl
import json
import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

BALANCE = Path(__file__).resolve().parents[1]
WORKTREE = BALANCE.parent
TOOL_DIR = BALANCE / "tools" / "cscheck"
TOOL_DLL = TOOL_DIR / "bin" / "Release" / "net8.0" / "cscheck.dll"
TOOL_SOURCES = ("cscheck.csproj", "Program.cs", "packages.lock.json")
SIGNATURE = TOOL_DIR / "signature_6af011f.json"
BASELINE = TOOL_DIR / "baseline.json"
SIGNATURE_REV = "6af011f"
DEFAULT_RUN_WORKTREE = Path("/Users/joyfort/MyProjects/Tufu-stage5-run")
STEPS = ("compile", "no_editor", "signature", "bans")


class CsCheckError(RuntimeError):
    """The tool could not run (exit code 2); the message says why."""


def run_worktree():
    return Path(os.environ.get("TUFU_RUN_WORKTREE", DEFAULT_RUN_WORKTREE))


def dotnet():
    exe = os.environ.get("TUFU_DOTNET") or shutil.which("dotnet") or "/opt/homebrew/bin/dotnet"
    if not Path(exe).exists():
        raise CsCheckError(f"dotnet not found ({exe}); install the .NET 8 SDK or set TUFU_DOTNET")
    return exe


def ensure_built():
    """Build the tool when the DLL is missing or older than its sources (locked restore)."""
    def stale():
        if not TOOL_DLL.exists():
            return True
        built = TOOL_DLL.stat().st_mtime
        return any((TOOL_DIR / name).stat().st_mtime > built for name in TOOL_SOURCES)

    if not stale():
        return TOOL_DLL
    (TOOL_DIR / "obj").mkdir(exist_ok=True)
    with open(TOOL_DIR / "obj" / ".build.lock", "w") as lock:
        fcntl.flock(lock, fcntl.LOCK_EX)
        if stale():
            proc = subprocess.run(
                [dotnet(), "build", str(TOOL_DIR / "cscheck.csproj"), "-c", "Release", "--nologo", "-v", "q",
                 "-p:RestoreLockedMode=true"],
                capture_output=True, text=True)
            if proc.returncode != 0:
                raise CsCheckError("dotnet build failed:\n" + proc.stdout[-4000:] + proc.stderr[-2000:])
            TOOL_DLL.touch()
    return TOOL_DLL


def _invoke(args):
    dll = ensure_built()
    with tempfile.TemporaryDirectory(prefix="cscheck-") as tmp:
        out = Path(tmp) / "result.json"
        proc = subprocess.run([dotnet(), str(dll), *args, "--json", str(out)], capture_output=True, text=True)
        result = json.loads(out.read_text(encoding="utf-8")) if out.exists() else None
    if proc.returncode == 2 or result is None:
        message = (result or {}).get("tool_error") or proc.stderr.strip() or f"exit code {proc.returncode}"
        raise CsCheckError(message)
    result["exit_code"] = proc.returncode
    return result


def check(worktree=None, run=None, steps=STEPS, signature=SIGNATURE, baseline=BASELINE,
          update_baseline=False, list_warnings=False):
    """Run the selected steps on `worktree` (default: this worktree) and return the result dict.

    result["ok"] is the verdict; result["failures"] lists the reasons. Raises CsCheckError when the tool
    cannot run at all.
    """
    unknown = set(steps) - set(STEPS)
    if unknown:
        raise ValueError(f"unknown steps {sorted(unknown)}")
    args = ["check", "--worktree", str(worktree or WORKTREE), "--run-worktree", str(run or run_worktree()),
            "--signature-file", str(signature), "--baseline-file", str(baseline)]
    flags = {"compile": "--compile", "no_editor": "--no-editor-define", "signature": "--signature", "bans": "--bans"}
    args += [flags[s] for s in steps]
    if update_baseline:
        args.append("--update-baseline")
    if list_warnings:
        args.append("--list-warnings")
    return _invoke(args)


def generate_signature(out=SIGNATURE, rev=SIGNATURE_REV, repo=None, run=None):
    """Write the signature lock of `rev` (read straight from git objects) to `out`."""
    dll = ensure_built()
    proc = subprocess.run(
        [dotnet(), str(dll), "signature", "--git-rev", rev, "--repo", str(repo or WORKTREE),
         "--run-worktree", str(run or run_worktree()), "--out", str(out)],
        capture_output=True, text=True)
    if proc.returncode != 0:
        raise CsCheckError(proc.stderr.strip() or f"exit code {proc.returncode}")
    return Path(out)


def sources(worktree=None, run=None):
    """Predefined-assembly membership of a worktree, compared with the .rsp source lists."""
    return _invoke(["sources", "--worktree", str(worktree or WORKTREE), "--run-worktree", str(run or run_worktree())])


def summary(result):
    lines = []
    for asm, entry in result.get("sources", {}).items():
        extra = ""
        if entry.get("added") or entry.get("removed"):
            extra = f" (vs .rsp: +{len(entry['added'])} -{len(entry['removed'])})"
        lines.append(f"sources   {asm}: {entry['count']} files{extra}")
    for asm, entry in result.get("compile", {}).items():
        lines.append(f"compile   {asm}: {entry['errors']} errors, {entry['warnings']} warnings")
    if "no_editor" in result:
        ne = result["no_editor"]
        lines.append(f"no-editor Assembly-CSharp: {ne['errors']} errors"
                     + (f", {len(ne['new_errors'])} not in baseline" if "new_errors" in ne else ""))
    if "signature" in result:
        lines.append(f"signature {len(result['signature']['violations'])} violations")
    if "bans" in result:
        lines.append(f"bans      {len(result['bans']['violations'])} uses in "
                     f"{len(result['bans']['scope']['files'])} files + {len(result['bans']['scope']['members'])} members")
    lines += [f"FAIL      {f}" for f in result.get("failures", [])]
    lines.append(f"{'OK' if result['ok'] else 'FAILED'} in {result['timings']['total_ms'] / 1000:.1f} s")
    return "\n".join(lines)


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--compile", action="store_true", help="compile both assemblies with UNITY_EDITOR")
    parser.add_argument("--no-editor-define", action="store_true", help="compile Assembly-CSharp without UNITY_EDITOR")
    parser.add_argument("--signature", action="store_true", help="signature lock against 6af011f")
    parser.add_argument("--bans", action="store_true", help="banned APIs in rebuild code")
    parser.add_argument("--worktree", type=Path, help="worktree to check (default: this one)")
    parser.add_argument("--run-worktree", type=Path, help=f"worktree holding Unity's Library (default {DEFAULT_RUN_WORKTREE}, env TUFU_RUN_WORKTREE)")
    parser.add_argument("--json", type=Path, help="also write the full result JSON here")
    parser.add_argument("--update-baseline", action="store_true", help="rewrite baseline.json from this run")
    parser.add_argument("--generate-signature", action="store_true", help=f"rewrite {SIGNATURE.name} from {SIGNATURE_REV}")
    args = parser.parse_args()
    try:
        if args.generate_signature:
            print(generate_signature(run=args.run_worktree))
            return 0
        steps = [s for s, on in zip(STEPS, (args.compile, args.no_editor_define, args.signature, args.bans)) if on]
        if args.update_baseline:
            steps = ["compile", "no_editor"]
        result = check(args.worktree, args.run_worktree, steps or STEPS, update_baseline=args.update_baseline)
    except CsCheckError as e:
        print(f"cscheck: {e}", file=sys.stderr)
        return 2
    if args.json:
        args.json.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(summary(result))
    return 0 if result["ok"] else 1


if __name__ == "__main__":
    sys.exit(main())
