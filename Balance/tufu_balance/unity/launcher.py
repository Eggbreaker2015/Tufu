"""Launch Unity for batch simulation runs (design 2.1-2.5, 2.10; unit C0b).

Around every run:

* refuse the main checkout, refuse a dirty run worktree, refuse when another Unity has the project open
  (case-insensitive ``pgrep -fi``: the Hub writes ``-projectpath``), take the run worktree lock (queue,
  never pre-empt);
* check the protected DLLs with the rules of ``Recovery/Open-StudyProject.ps1``;
* hash and back up the real save directory and the real PlayerPrefs plist, hash ProjectSettings.asset
  and the protected assets; after the Unity process (and its import workers) exited, hash them again
  and check ``git status --porcelain -uall``. A changed real save/plist is restored from the backup only
  when no other Unity process was seen during the run (another editor may legitimately write it);
  either way the run fails with exit code 2;
* watch the heartbeat: no first beat within the startup timeout, or no progress within the stall
  timeout, kills Unity (exit code 3);
* sample the Unity process's sockets (``lsof``) and scan the log for SDK output;
* remove the sandbox PlayerPrefs domain and save directory afterwards.

Exit codes: 0 ok, 1 exception, 2 guard, 3 timeout, 4 not ready, 5 bad request, 6 self-check, 7 dev
worktree changed, 8 ready check.
"""

from __future__ import annotations

import dataclasses
import datetime
import fcntl
import hashlib
import json
import os
import re
import shutil
import signal
import socket
import sqlite3
import subprocess
import threading
import time
from pathlib import Path
from typing import Any, Callable, Mapping

from . import request as req
from . import runner

EXIT_OK, EXIT_EXCEPTION, EXIT_GUARD, EXIT_TIMEOUT, EXIT_NOT_READY, EXIT_BAD_REQUEST = 0, 1, 2, 3, 4, 5
EXIT_SELF_CHECK, EXIT_DEV_CHANGED, EXIT_READY_CHECK = 6, 7, 8
EXIT_NAMES = {0: "ok", 1: "exception", 2: "guard", 3: "timeout", 4: "not ready", 5: "bad request", 6: "self-check",
              7: "dev worktree changed", 8: "ready check"}

DEFAULT_UNITY = Path("/Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/MacOS/Unity")
FIXED_ARGS = ("-disable-assembly-updater", "Unity.Mathematics.dll", "Unity.Burst.dll")
ENTRY_CLASS = "Codebase.Simulation.EditorTools.SimulationBatch"
OUT_ROOT = runner.BALANCE / "out" / "sim"
HEARTBEAT = Path("Temp/TufuSimulation/heartbeat.json")
QUEUE = Path("Temp/TufuSimulation/queue")

# Copied from Recovery/Open-StudyProject.ps1 ($taskLibraries); tests/test_unity_launcher.py checks they agree.
DLL_RULES = {
    "Unity.Mathematics.dll": {"expected": "3a1be62e9bc5b692432d47ca9af877db407157dcf520c131b499e5f5f45e7dd5",
                              "damaged": "c812927a2ae6bc684c11e7266ff074931dee022a6d317d62961ae83cdae7af48"},
    "Unity.Burst.dll": {"expected": "bb77bc0cf4e7f5850602d5922d5946d30a6b443b554a565399f3e041e990377d",
                        "damaged": "97432dc1a7b3e25f4b7fde9d4ba22714008eca5aced87b3381724bb828fbae61"},
}

# Log lines a third-party SDK or the game backend would print. A scene object named "RemoteConfig" logs
# a harmless DontDestroyOnLoad warning in every run, so the bare word is not a pattern.
SDK_LOG_PATTERN = re.compile(r"AppMetrica|\bAdjust(Sdk|Initializer|Config)?\b|Firebase|AppLovin|MaxSdk|Facebook\.Unity|Tapjoy|"
                             r"BackendApi|Multicast\.Backend|UnityServices|RemoteConfigService", re.I)


class LauncherError(RuntimeError):
    """The launcher refused to start (nothing was run); ``code`` is the exit code to report."""

    def __init__(self, message: str, code: int = EXIT_GUARD):
        super().__init__(message)
        self.code = code


@dataclasses.dataclass
class Options:
    unity: Path = dataclasses.field(default_factory=lambda: Path(os.environ.get("TUFU_UNITY", DEFAULT_UNITY)))
    nographics: bool = False
    startup_timeout_s: float = 600.0   # process start -> first heartbeat (covers script compilation)
    stall_timeout_s: float = 120.0     # heartbeat progress unchanged
    overall_timeout_s: float | None = None  # default: derived from the request
    lock_wait_s: float = 3600.0
    net_sample_s: float = 2.0
    allow_main_checkout: bool = False
    worker_exit_wait_s: float = 60.0


@dataclasses.dataclass
class ProtectedPaths:
    """Real (non-sandbox) locations a run must never change."""
    save_dir: Path
    plist: Path
    domain: str | None  # PlayerPrefs domain read through cfprefsd; None skips the live check (tests)

    @classmethod
    def for_project(cls, project: Path) -> "ProtectedPaths":
        company, product = player_identity(project)
        home = Path.home()
        return cls(save_dir=home / "Library" / "Application Support" / company / product.replace(":", "_").replace("/", "_"),
                   plist=home / "Library" / "Preferences" / f"unity.{company}.{product}.plist",
                   domain=f"unity.{company}.{product}")


@dataclasses.dataclass
class RunResult:
    run_dir: Path
    exit_code: int
    request: dict
    report: dict
    cached: bool = False

    def job_dir(self, job_id: str) -> Path:
        return self.run_dir / job_id

    def manifest(self, job_id: str) -> dict:
        return json.loads((self.job_dir(job_id) / "manifest.json").read_text())

    def file(self, job_id: str, name: str) -> bytes:
        return (self.job_dir(job_id) / name).read_bytes()

    def log_tail(self, lines: int = 200) -> str:
        log = self.run_dir / "unity.log"
        if not log.exists():
            return "(no unity.log)"
        return "\n".join(log.read_text(errors="replace").splitlines()[-lines:])

    def describe(self) -> str:
        """Failure context for tests: exit code, launcher findings, sandbox report, log tail."""
        parts = [f"run {self.run_dir}: exit {self.exit_code} ({EXIT_NAMES.get(self.exit_code, '?')})",
                 "launcher: " + json.dumps({k: self.report.get(k) for k in ("reason", "unity_exit", "guard_problems")},
                                           ensure_ascii=False)]
        for job in self.request.get("jobs", []):
            path = self.job_dir(job["job_id"]) / "manifest.json"
            if path.exists():
                m = json.loads(path.read_text())
                parts.append(f"{job['job_id']}: exit {m.get('exit_code')} error {m.get('error')!r}")
                parts.append("sandbox: " + json.dumps(m.get("sandbox"), ensure_ascii=False))
        parts.append("---- unity.log (tail) ----\n" + self.log_tail())
        return "\n".join(parts)


# ---------------------------------------------------------------- small helpers

def now_iso() -> str:
    return datetime.datetime.now(datetime.timezone.utc).isoformat(timespec="seconds")


def pid_alive(pid: int | None) -> bool:
    if not pid:
        return False
    try:
        os.kill(pid, 0)
    except ProcessLookupError:
        return False
    except PermissionError:
        return True
    # An exited child of this process stays a zombie until reaped; do not reap it here (Popen.poll
    # would then report exit code 0), just look at its state.
    stat = subprocess.run(["ps", "-o", "stat=", "-p", str(pid)], capture_output=True, text=True).stdout.strip()
    return bool(stat) and not stat.startswith("Z")


def player_identity(project: Path) -> tuple[str, str]:
    """companyName/productName from ProjectSettings.asset (single-quoted YAML scalars unescaped)."""
    text = (Path(project) / "ProjectSettings" / "ProjectSettings.asset").read_text(encoding="utf-8")

    def field(name):
        m = re.search(rf"^\s*{name}: (.*)$", text, re.M)
        if not m:
            raise LauncherError(f"{name} missing from ProjectSettings.asset")
        value = m.group(1).strip()
        if value.startswith("'") and value.endswith("'"):
            value = value[1:-1].replace("''", "'")
        elif value.startswith('"') and value.endswith('"'):
            value = json.loads(value)
        return value

    return field("companyName"), field("productName")


def _ere_escape(text: str) -> str:
    return re.sub(r"([.\[\]{}()*+?^$|\\])", r"\\\1", text)


def unity_processes(project: Path) -> list[tuple[int, str]]:
    """Processes whose command line opens ``project`` (any case, ``-projectPath`` or ``-projectpath``)."""
    proc = subprocess.run(["pgrep", "-fil", _ere_escape(str(Path(project)).rstrip("/"))], capture_output=True, text=True)
    found = []
    for line in proc.stdout.splitlines():
        pid_text, _, cmd = line.strip().partition(" ")
        if not pid_text.isdigit() or int(pid_text) == os.getpid():
            continue
        if re.search(r"-projectpath", cmd, re.I):
            found.append((int(pid_text), cmd))
    return found


def check_dlls(project: Path, repair: bool = True) -> dict[str, str]:
    """Apply the .ps1 rules: expected -> ok; known damaged -> back up and replace from
    Recovery/Tools/OfficialPlugins; anything else -> refuse."""
    plugins = Path(project) / "Assets" / "Plugins"
    status = {}
    for name, rule in DLL_RULES.items():
        target = plugins / name
        digest = runner.sha256_file(target)
        if digest == rule["expected"]:
            status[name] = "expected"
            continue
        if digest != rule["damaged"] or not repair:
            raise LauncherError(f"unexpected identity of {target} (sha256 {digest}); inspect before replacing")
        recovery = Path(project).parent
        candidate = recovery / "Tools" / "OfficialPlugins" / name
        if runner.sha256_file(candidate) != rule["expected"]:
            raise LauncherError(f"invalid official adapted candidate {candidate}")
        backup = recovery / "Backups" / "ProtectedLaunch" / datetime.datetime.now().strftime("%Y%m%d-%H%M%S-%f")
        backup.mkdir(parents=True, exist_ok=True)
        shutil.copy2(target, backup / name)
        shutil.copy2(candidate, target)
        status[name] = "repaired"
    return status


# ---------------------------------------------------------------- lock

class UnityLock:
    """Lock on one Unity project, a JSON file ``{pid, owner, ...}`` in ``<worktree>/Balance/out``.
    A holder whose process is gone is stale and is taken over. Contention waits (queue), never pre-empts.
    Serve mode hands the lock to the Unity process (``set_holder``) so it outlives the CLI call."""

    def __init__(self, worktree: Path):
        self.path = Path(worktree) / "Balance" / "out" / "unity.lock"
        self.held = False

    def _guard(self):
        self.path.parent.mkdir(parents=True, exist_ok=True)
        f = open(self.path.with_suffix(".guard"), "w")
        fcntl.flock(f, fcntl.LOCK_EX)
        return f

    def read(self) -> dict | None:
        try:
            return json.loads(self.path.read_text())
        except (FileNotFoundError, json.JSONDecodeError):
            return None

    def try_acquire(self, owner: str, **info) -> bool:
        with self._guard():
            current = self.read()
            if current and pid_alive(current.get("pid")) and current.get("pid") != os.getpid():
                return False
            data = {"pid": os.getpid(), "owner": owner, "host": socket.gethostname(), "since": now_iso(), **info}
            self.path.write_text(json.dumps(data, indent=1) + "\n")
            self.held = True
            return True

    def acquire(self, owner: str, wait_s: float = 3600.0, **info) -> None:
        deadline = time.monotonic() + wait_s
        while not self.try_acquire(owner, **info):
            if time.monotonic() > deadline:
                raise LauncherError(f"Unity lock {self.path} held by {self.read()}", EXIT_TIMEOUT)
            time.sleep(1.0)

    def set_holder(self, pid: int, **info) -> None:
        with self._guard():
            data = self.read() or {}
            data.update({"pid": pid, **info})
            self.path.write_text(json.dumps(data, indent=1) + "\n")

    def release(self) -> None:
        with self._guard():
            current = self.read()
            if current is None:
                return
            if current.get("pid") == os.getpid() or not pid_alive(current.get("pid")):
                self.path.unlink(missing_ok=True)
        self.held = False


# ---------------------------------------------------------------- protected state

def _defaults_export(domain: str | None) -> bytes | None:
    if domain is None:
        return None
    proc = subprocess.run(["/usr/bin/defaults", "export", domain, "-"], capture_output=True)
    return proc.stdout if proc.returncode == 0 else None


def snapshot(paths: ProtectedPaths, project: Path) -> dict:
    save = {}
    if paths.save_dir.is_dir():
        for f in sorted(paths.save_dir.rglob("*")):
            if f.is_file() and not f.is_symlink():
                save[str(f.relative_to(paths.save_dir))] = runner.sha256_file(f)
    live = _defaults_export(paths.domain)
    assets = {}
    for rel in ("Assets/MonoBehaviour/Simulation Config.asset", "Assets/MonoBehaviour/Simulation Data.asset",
                "Assets/MonoBehaviour/Combat Balance Config.asset", "Assets/MonoBehaviour/Player Config.asset",
                "Assets/MonoBehaviour/ABTest Data Config.asset", "Assets/MonoBehaviour/Locations Config.asset",
                "Assets/Simulation/Simulation Rebuild Params.asset", "ProjectSettings/ProjectSettings.asset",
                "ProjectSettings/EditorSettings.asset", "ProjectSettings/TimeManager.asset"):
        assets[rel] = runner.sha256_file(Path(project) / rel)
    return {"save_dir": save, "plist_file": runner.sha256_file(paths.plist),
            "plist_live": hashlib.sha256(live).hexdigest() if live is not None else None, "project": assets}


# The editor keeps its own analytics session under the real persistentDataPath from start-up (before the
# sandbox rename) until it exits; while it is alive these files come and go.
EDITOR_ANALYTICS = re.compile(r"^Unity/[^/]+/Editor/Analytics/")


def diff_snapshots(before: Mapping, after: Mapping, *, editor_alive: bool = False) -> list[str]:
    """Differences that matter. Pass ``editor_alive`` for a check while Unity still runs (serve mode)."""
    problems = []
    for name in sorted(set(before["save_dir"]) | set(after["save_dir"])):
        if editor_alive and EDITOR_ANALYTICS.match(name):
            continue
        if before["save_dir"].get(name) != after["save_dir"].get(name):
            problems.append(f"real save file changed: {name}")
    for key in ("plist_file", "plist_live"):
        if before[key] != after[key]:
            problems.append(f"real PlayerPrefs changed ({key})")
    for name in sorted(set(before["project"]) | set(after["project"])):
        if before["project"].get(name) != after["project"].get(name):
            problems.append(f"project file changed on disk: {name}")
    return problems


def sandbox_proven(run_dir: Path, identity: str, pid: int, real_save_dir: Path) -> bool:
    """This editor process (``pid``) reported that persistentDataPath and the PlayerPrefs probe followed the
    sandbox rename away from ``real_save_dir``."""
    try:
        report = json.loads((Path(run_dir) / "editor_report.json").read_text())
        sb = report["sandbox"]
    except (OSError, ValueError, KeyError, TypeError):
        return False
    data, real = str(sb.get("sandbox_persistent_data_path") or ""), str(sb.get("real_persistent_data_path") or "")
    return (report.get("pid") == pid and real == str(real_save_dir)
            and sb.get("prefs_probe_in_sandbox_domain") is True and sb.get("prefs_probe_in_real_domain") is False
            and bool(data) and not data.startswith(real) and f"/{req.SANDBOX_COMPANY}/" in data
            and sb.get("sandbox_product") == identity)


def main_checkout_editors() -> set[int]:
    """Unity editors the user has open on the main checkout (their import workers excluded)."""
    project = runner.MAIN_CHECKOUT / runner.PROJECT_SUBDIR
    return {pid for pid, cmdline in unity_processes(project) if "AssetImportWorker" not in cmdline}


def _is_editor_analytics(problem: str) -> bool:
    prefix = "real save file changed: "
    return problem.startswith(prefix) and bool(EDITOR_ANALYTICS.match(problem[len(prefix):]))


def backup_real(paths: ProtectedPaths, dest: Path) -> None:
    dest.mkdir(parents=True, exist_ok=True)
    if paths.save_dir.is_dir():
        shutil.copytree(paths.save_dir, dest / "save_dir", symlinks=True, dirs_exist_ok=True)
    if paths.plist.exists():
        shutil.copy2(paths.plist, dest / "prefs.plist")
    live = _defaults_export(paths.domain)
    if live is not None:
        (dest / "prefs.live.plist").write_bytes(live)


def restore_real(paths: ProtectedPaths, src: Path, *, save: bool = True, prefs: bool = True) -> list[str]:
    """Put the real save directory and/or PlayerPrefs back from ``backup_real``'s copy."""
    done = []
    if save and (src / "save_dir").is_dir():
        if paths.save_dir.exists():
            shutil.rmtree(paths.save_dir)
        shutil.copytree(src / "save_dir", paths.save_dir, symlinks=True)
        done.append("save_dir")
    live = src / "prefs.live.plist"
    if prefs and paths.domain and live.exists():
        subprocess.run(["/usr/bin/defaults", "import", paths.domain, str(live)], check=True)
        done.append("prefs (defaults import)")
    return done


def cleanup_sandbox(identity: str) -> list[str]:
    """Remove the sandbox PlayerPrefs domain and data directory; only paths under TufuSimSandbox."""
    if not re.fullmatch(r"TufuSim[A-Za-z0-9_-]{0,48}", identity):
        raise LauncherError(f"bad sandbox identity {identity!r}")
    company = req.SANDBOX_COMPANY
    domain = f"unity.{company}.{identity}"
    removed = []
    if subprocess.run(["/usr/bin/defaults", "delete", domain], capture_output=True).returncode == 0:
        removed.append(f"defaults domain {domain}")
    prefs = Path.home() / "Library" / "Preferences"
    plist = prefs / f"{domain}.plist"
    if plist.parent == prefs and plist.name.startswith(f"unity.{company}.") and plist.exists():
        plist.unlink()
        removed.append(str(plist))
    root = Path.home() / "Library" / "Application Support" / company
    data = root / identity
    if data.parent == root and data.is_dir():
        shutil.rmtree(data)
        removed.append(str(data))
    if root.is_dir() and not any(root.iterdir()):
        root.rmdir()
    return removed


# ---------------------------------------------------------------- network sampling and log scan

def system_proxy() -> str | None:
    proc = subprocess.run(["scutil", "--proxy"], capture_output=True, text=True)
    values = dict(re.findall(r"^\s*(\w+) : (.*)$", proc.stdout, re.M))
    if values.get("HTTPSEnable") == "1":
        return f"{values.get('HTTPSProxy')}:{values.get('HTTPSPort')}"
    return None


def _is_loopback(endpoint: str) -> bool:
    host = endpoint.rsplit(":", 1)[0].strip("[]")
    return host in ("localhost", "::1") or host.startswith("127.") or host == "*"


class NetSampler(threading.Thread):
    """Samples ``lsof -i`` of the Unity process; each sample is tagged with the heartbeat phase."""

    def __init__(self, pid: int, heartbeat: Path, interval: float):
        super().__init__(daemon=True)
        self.pid, self.heartbeat, self.interval = pid, heartbeat, interval
        self.samples: list[tuple[str, list[str]]] = []
        self.stop = threading.Event()

    def run(self):
        while not self.stop.is_set() and pid_alive(self.pid):
            proc = subprocess.run(["lsof", "-nP", "-a", "-p", str(self.pid), "-i", "-F", "n"], capture_output=True, text=True)
            remotes = sorted({line[1:].split("->", 1)[1] for line in proc.stdout.splitlines()
                              if line.startswith("n") and "->" in line})
            self.samples.append((_phase(self.heartbeat), remotes))
            self.stop.wait(self.interval)

    def summary(self) -> dict:
        proxy = system_proxy()
        edit, play = set(), set()
        for phase, remotes in self.samples:
            (play if phase.startswith("play") else edit).update(remotes)
        non_loop = lambda s: sorted(r for r in s if not _is_loopback(r))
        return {
            "samples": len(self.samples),
            "system_proxy": proxy,
            "remote_endpoints_edit": sorted(edit),
            "remote_endpoints_play": sorted(play),
            "non_loopback_edit": non_loop(edit),
            "non_loopback_play": non_loop(play),
            "new_non_loopback_in_play": sorted(set(non_loop(play)) - set(non_loop(edit))),
            "note": "through a system proxy every connection is 127.0.0.1:<port>; hosts are not visible here",
        }


def _phase(heartbeat: Path) -> str:
    try:
        return json.loads(heartbeat.read_text()).get("phase", "")
    except (OSError, ValueError):
        return ""


def scan_log(log: Path) -> dict:
    """SDK-looking lines after the first Play entry (the editor's own startup is excluded)."""
    if not log.exists():
        return {"play_entries": 0, "sdk_lines": []}
    lines = log.read_text(errors="replace").splitlines()
    starts = [i for i, l in enumerate(lines) if "[TufuSim] job " in l and "entering Play" in l]
    hits = []
    if starts:
        for line in lines[starts[0]:]:
            if line.startswith("[TufuSim]") or "[ABTest]" in line:
                continue
            if SDK_LOG_PATTERN.search(line):
                hits.append(line[:300])
    errors = [l[:300] for l in lines if re.match(r"^(Error|Exception|NullReferenceException|.*Exception:)", l)]
    return {"play_entries": len(starts), "sdk_lines": hits[:50], "error_like_lines": len(errors)}


# ---------------------------------------------------------------- process control

def _kill(proc: subprocess.Popen) -> None:
    try:
        os.killpg(proc.pid, signal.SIGTERM)
    except ProcessLookupError:
        return
    try:
        proc.wait(20)
    except subprocess.TimeoutExpired:
        try:
            os.killpg(proc.pid, signal.SIGKILL)
        except ProcessLookupError:
            pass
        proc.wait(10)


def wait_for_project_free(project: Path, wait_s: float) -> list[tuple[int, str]]:
    deadline = time.monotonic() + wait_s
    while True:
        left = unity_processes(project)
        if not left or time.monotonic() > deadline:
            return left
        time.sleep(0.5)


def unity_command(opts: Options, project: Path, method: str, log: Path, extra: list[str]) -> list[str]:
    cmd = [str(opts.unity), "-batchmode", "-projectPath", str(project), *FIXED_ARGS, "-logFile", str(log),
           "-executeMethod", f"{ENTRY_CLASS}.{method}", *extra]
    if opts.nographics:
        cmd.append("-nographics")
    return cmd


def watch(proc: subprocess.Popen, heartbeat: Path, opts: Options, overall_s: float,
          on_beat: Callable[[dict], None] | None = None) -> tuple[int, str | None, dict]:
    """Waits for the process; returns (exit code, timeout reason or None, watch stats)."""
    start = time.monotonic()
    last_progress, last_change, beats = None, None, 0
    while True:
        code = proc.poll()
        if code is not None:
            return code, None, {"beats": beats, "wall_s": time.monotonic() - start}
        now = time.monotonic()
        try:
            beat = json.loads(heartbeat.read_text())
        except (OSError, ValueError):
            beat = None
        if beat and beat.get("pid") == proc.pid:
            if beat.get("progress") != last_progress:
                last_progress, last_change = beat.get("progress"), now
                beats += 1
                if on_beat:
                    on_beat(beat)
        reason = None
        if last_change is None and now - start > opts.startup_timeout_s:
            reason = f"no heartbeat within {opts.startup_timeout_s:g} s"
        elif last_change is not None and now - last_change > opts.stall_timeout_s:
            reason = f"heartbeat progress stalled for {opts.stall_timeout_s:g} s at {last_progress}"
        elif now - start > overall_s:
            reason = f"overall timeout {overall_s:g} s"
        if reason:
            _kill(proc)
            return EXIT_TIMEOUT, reason, {"beats": beats, "wall_s": time.monotonic() - start}
        time.sleep(0.25)


# ---------------------------------------------------------------- run

def _refuse_main(project: Path, opts: Options) -> None:
    main = (runner.MAIN_CHECKOUT / runner.PROJECT_SUBDIR).resolve()
    if Path(project).resolve() == main and not opts.allow_main_checkout:
        raise LauncherError(f"refusing to run Unity on the main checkout {main} (pass --allow-main-checkout)")


def _overall(request: Mapping, opts: Options) -> float:
    if opts.overall_timeout_s:
        return opts.overall_timeout_s
    total = 300.0
    for job in request["jobs"]:
        total += request["unity"].get("ready_timeout_s", 300) + (job["stop"].get("max_wall_seconds") or 3600) + 120
    return total


def balance_db() -> Path:
    return Path(os.environ.get("TUFU_BALANCE_DB") or runner.BALANCE / "out" / "balance.sqlite")


_expected_tables: dict[tuple, tuple[str, str]] = {}


def expected_ready_table(variant: str, db: Path | None = None) -> tuple[str, str]:
    """(file name, CSV text) of P6's readiness table for an AB variant, keyed by the C# spawner identity."""
    from .spawner_map import expected_ready_csv, expected_ready_file_name

    db = Path(db or balance_db())
    if not db.exists():
        raise LauncherError(f"sandbox.expected_ready_spawners = {req.EXPECTED_READY_AUTO!r} needs the extracted database "
                            f"({db}); set TUFU_BALANCE_DB or pass an explicit table or null", EXIT_BAD_REQUEST)
    cache_key = (str(db), db.stat().st_mtime_ns, variant)
    if cache_key not in _expected_tables:
        con = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
        try:
            text = expected_ready_csv(con, variant)
        finally:
            con.close()
        _expected_tables[cache_key] = (expected_ready_file_name(text), text)
    return _expected_tables[cache_key]


def write_side_inputs(r: Mapping, run_dir: Path) -> None:
    """Files the request names relative to out_dir (the auto readiness table)."""
    name = r["sandbox"].get("expected_ready_spawners")
    if name and name.startswith("ready_spawners.expected.") and not Path(name).is_absolute():
        resolved = expected_ready_table(r["sandbox"].get("ab_variant", "A"))
        if resolved[0] != name:
            raise LauncherError(f"readiness table changed since the request was prepared ({name} -> {resolved[0]})")
        (Path(run_dir) / name).write_text(resolved[1], encoding="utf-8")


def prepare_request(request: Mapping, *, project: Path, worktree: Path, run_tree: Path, rev: str | None, dev: bool,
                    opts: Options, out_root: Path, checkout: bool = True) -> tuple[dict, dict]:
    """Fills code_rev, run_key and out_dir, and resolves sandbox.expected_ready_spawners = "auto" to the
    content-addressed name of P6's table (so the run_key covers it). Returns (request, key material)."""
    r = json.loads(json.dumps(request))
    if r.get("sandbox", {}).get("expected_ready_spawners") == req.EXPECTED_READY_AUTO:
        r["sandbox"]["expected_ready_spawners"] = expected_ready_table(r["sandbox"].get("ab_variant", "A"))[0]
    if dev:
        state = runner.dev_state(worktree)
        code_rev = state["head"] + "+dev"
        trees = runner.tree_hashes(worktree, state["head"])
    else:
        state = None
        code_rev = runner.checkout(run_tree, rev or runner.head(worktree)) if checkout else runner.resolve(run_tree, rev or "HEAD")
        trees = runner.tree_hashes(run_tree, code_rev)
    unity = runner.unity_version(opts.unity)
    dlls = runner.dll_hashes(project)
    key = runner.run_key(r, code_rev=code_rev, trees=trees, unity=unity, dlls=dlls, dev=state)
    r["code_rev"] = code_rev
    r["run_key"] = key
    r["out_dir"] = str(Path(out_root) / "runs" / key)
    return r, {"code_rev": code_rev, "trees": trees, "unity_version": unity, "dlls": dlls, "dev": state}


def run(request: Mapping | None = None, *, preset: str | None = None, rev: str | None = None, dev: bool = False,
        force: bool = False, archive_previous: bool = False, validate: bool = True, method: str = "Run",
        opts: Options | None = None, out_root: Path | None = None, worktree: Path | None = None,
        run_tree: Path | None = None, paths: ProtectedPaths | None = None, log=print) -> RunResult:
    """Run a request (or preset) in the run worktree at ``rev`` (default: this worktree's HEAD)."""
    opts = opts or Options()
    worktree = Path(worktree or runner.WORKTREE)
    run_tree = Path(run_tree or runner.run_worktree())
    out_root = Path(out_root or OUT_ROOT)
    if request is None:
        request = req.preset(preset or "selftest")
    target = worktree if dev else run_tree
    project = runner.project_dir(target)
    _refuse_main(project, opts)
    if validate:
        try:
            req.validate({**request, "out_dir": "/placeholder"} if not request.get("out_dir") else request)
        except req.RequestError as e:
            raise LauncherError(f"invalid request: {e}", EXIT_BAD_REQUEST) from e
    if not (project / "Library").is_dir():
        raise LauncherError(f"{project} has no Library; Unity runs only in the run worktree (or a dev worktree with a Library)")
    lock = UnityLock(target)
    lock.acquire(method.lower(), opts.lock_wait_s)
    try:
        others = unity_processes(project)
        if others:
            raise LauncherError(f"Unity already has {project} open: {others}")
        if not dev:
            runner.require_clean(run_tree)
        r, material = prepare_request(request, project=project, worktree=worktree, run_tree=run_tree, rev=rev, dev=dev,
                                      opts=opts, out_root=out_root)
        dll_status = check_dlls(project)
        run_dir = Path(r["out_dir"])
        cached = _cached(run_dir)
        if cached is not None and not force and not dev:
            log(f"cached: {run_dir}")
            return RunResult(run_dir, cached["exit_code"], r, cached, cached=True)
        if run_dir.exists():
            if archive_previous:
                n = 1
                while run_dir.with_name(f"{run_dir.name}.prev-{n}").exists():
                    n += 1
                run_dir.rename(run_dir.with_name(f"{run_dir.name}.prev-{n}"))
            else:
                shutil.rmtree(run_dir)
        run_dir.mkdir(parents=True)
        (run_dir / "request.json").write_text(req.dumps(r))
        write_side_inputs(r, run_dir)
        return _launch_and_check(r, run_dir, project=project, worktree=worktree, dev=dev, method=method, opts=opts,
                                 material={**material, "dll_status": dll_status}, paths=paths, log=log)
    finally:
        lock.release()


def _cached(run_dir: Path) -> dict | None:
    try:
        report = json.loads((run_dir / "launcher.json").read_text())
    except (OSError, ValueError):
        return None
    return report if report.get("exit_code") == 0 else None


def _launch_and_check(r: dict, run_dir: Path, *, project: Path, worktree: Path, dev: bool, method: str, opts: Options,
                      material: dict, paths: ProtectedPaths | None, log) -> RunResult:
    paths = paths or ProtectedPaths.for_project(project)
    identity = r["sandbox"]["identity"]
    report: dict[str, Any] = {"schema": "tufu.sim.launcher/1", "method": method, "project": str(project), "dev": dev,
                              "started": now_iso(), **material}
    report["sandbox_residue_removed"] = cleanup_sandbox(identity)
    main_editors_before = main_checkout_editors()
    before = snapshot(paths, project)
    backup = run_dir / "_real_backup"
    backup_real(paths, backup)
    others_seen = []
    heartbeat = project / HEARTBEAT
    heartbeat.unlink(missing_ok=True)
    cmd = unity_command(opts, project, method, run_dir / "unity.log", ["-tufuSimRequest", str(run_dir / "request.json")])
    report["command"] = cmd
    log(f"unity {method}: {run_dir.name[:12]}… ({', '.join(j['job_id'] for j in r['jobs'])})")
    t0 = time.monotonic()
    proc = subprocess.Popen(cmd, stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                            start_new_session=True)
    sampler = NetSampler(proc.pid, heartbeat, opts.net_sample_s)
    sampler.start()

    def on_beat(beat):
        for pid, cmdline in unity_processes(project):
            if pid != proc.pid and "AssetImportWorker" not in cmdline and pid not in [p for p, _ in others_seen]:
                others_seen.append((pid, cmdline))

    unity_exit, reason, stats = watch(proc, heartbeat, opts, _overall(r, opts), on_beat)
    sampler.stop.set()
    report["unity_exit"] = unity_exit
    report["reason"] = reason
    report["watch"] = stats
    report["process_wall_s"] = time.monotonic() - t0
    report["workers_left"] = wait_for_project_free(project, opts.worker_exit_wait_s)

    after = snapshot(paths, project)
    problems = diff_snapshots(before, after)
    # A killed editor leaves its analytics session in the real persistentDataPath (it writes it at start-up,
    # before the sandbox rename, and removes it on a clean exit): restore it, but it is not a game write.
    analytics = [p for p in problems if _is_editor_analytics(p)]
    problems = [p for p in problems if not _is_editor_analytics(p)]
    report["editor_analytics_leftovers"] = analytics
    restored = []
    real_changed = [p for p in problems if p.startswith("real ")] + analytics
    main_editors = sorted(main_editors_before & main_checkout_editors())
    report["main_checkout_editors"] = main_editors
    if real_changed:
        foreign = others_seen or [p for p in unity_processes(runner.MAIN_CHECKOUT / runner.PROJECT_SUBDIR)]
        if foreign:
            report["restore_skipped"] = f"other Unity processes were running: {foreign}"
        else:
            restored = restore_real(paths, backup, save=any("save file" in p for p in real_changed),
                                    prefs=any("PlayerPrefs" in p for p in real_changed))
    report["restored"] = restored
    exit_code = unity_exit if unity_exit in EXIT_NAMES else EXIT_EXCEPTION
    if dev:
        if runner.dev_state(worktree) != material["dev"]:
            problems.append("dev worktree changed during the run")
            exit_code = EXIT_DEV_CHANGED
    else:
        status = runner.porcelain(Path(project).parents[1])
        if status.strip():
            problems.append("run worktree not clean after the run: " + " | ".join(status.splitlines()[:10]))
    # The user's own editor on the main checkout writes the same real save and PlayerPrefs while it plays.
    # Those changes are attributed to it, not to this run, only when that editor ran for the whole run, this
    # editor exited cleanly with nothing else wrong, and it proved that its own writes went to the sandbox.
    # They are recorded and never restored (another editor is running); the backup is kept.
    real_problems = [p for p in problems if p.startswith("real ")]
    if (real_problems and main_editors and unity_exit == 0 and not others_seen
            and len(real_problems) == len(problems) and sandbox_proven(run_dir, identity, proc.pid, paths.save_dir)):
        report["real_changed_by_main_editor"] = real_problems
        problems = []
    guard_problems = [p for p in problems if "dev worktree" not in p]
    if guard_problems:
        exit_code = EXIT_GUARD
    report["guard_problems"] = problems
    report["sandbox_removed"] = cleanup_sandbox(identity)
    report["net"] = sampler.summary()
    report["log_scan"] = scan_log(run_dir / "unity.log")
    report["others_seen"] = others_seen
    report["exit_code"] = exit_code
    report["finished"] = now_iso()
    if not real_changed or restored:
        shutil.rmtree(backup, ignore_errors=True)
    for job in r["jobs"]:
        job_dir = run_dir / job["job_id"]
        if job_dir.is_dir() and (run_dir / "unity.log").exists():
            shutil.copy2(run_dir / "unity.log", job_dir / "unity.log")
    (run_dir / "launcher.json").write_text(json.dumps(report, indent=1, ensure_ascii=False, default=str) + "\n")
    log(f"unity {method}: exit {exit_code} ({EXIT_NAMES.get(exit_code, '?')})" + (f", {reason}" if reason else "")
        + (f", {'; '.join(problems[:3])}" if problems else "") + f" in {report['process_wall_s']:.1f} s")
    return RunResult(run_dir, exit_code, r, report)


def selftest(**kw) -> RunResult:
    return run(req.preset("selftest"), method="SelfTest", **kw)


# ---------------------------------------------------------------- interactive open

def open_editor(*, rev: str | None = None, dev: bool = False, opts: Options | None = None, worktree: Path | None = None,
                run_tree: Path | None = None) -> int:
    """Open the project interactively with the right arguments (replaces the untracked .command)."""
    opts = opts or Options()
    worktree = Path(worktree or runner.WORKTREE)
    run_tree = Path(run_tree or runner.run_worktree())
    target = worktree if dev else run_tree
    project = runner.project_dir(target)
    _refuse_main(project, opts)
    lock = UnityLock(target)
    if not lock.try_acquire("open"):
        raise LauncherError(f"Unity lock held by {lock.read()}")
    try:
        if unity_processes(project):
            raise LauncherError(f"Unity already has {project} open")
        if not dev and rev:
            runner.checkout(run_tree, rev)
        check_dlls(project)
        cmd = [str(opts.unity), "-projectPath", str(project), *FIXED_ARGS]
        proc = subprocess.Popen(cmd, stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                                start_new_session=True)
        lock.set_holder(proc.pid, owner="open")
        return proc.pid
    except BaseException:
        lock.release()
        raise


# ---------------------------------------------------------------- resident mode (design 2.5)

def _serve_state_path(run_tree: Path) -> Path:
    return Path(run_tree) / "Balance" / "out" / "serve" / "state.json"


def serve_start(*, rev: str | None = None, idle_timeout_s: float = 1800.0, identity: str = req.SANDBOX_COMPANY,
                opts: Options | None = None, worktree: Path | None = None, run_tree: Path | None = None,
                ready_wait_s: float = 600.0, log=print) -> dict:
    opts = opts or Options()
    worktree = Path(worktree or runner.WORKTREE)
    run_tree = Path(run_tree or runner.run_worktree())
    project = runner.project_dir(run_tree)
    _refuse_main(project, opts)
    lock = UnityLock(run_tree)
    lock.acquire("serve", opts.lock_wait_s)
    try:
        if unity_processes(project):
            raise LauncherError(f"Unity already has {project} open")
        runner.require_clean(run_tree)
        code_rev = runner.checkout(run_tree, rev or runner.head(worktree))
        check_dlls(project)
        paths = ProtectedPaths.for_project(project)
        cleanup_sandbox(identity)
        stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
        serve_dir = OUT_ROOT / "serve" / stamp
        serve_dir.mkdir(parents=True)
        before = snapshot(paths, project)
        backup_real(paths, serve_dir / "_real_backup")
        queue = project / QUEUE
        if queue.exists():
            shutil.rmtree(queue)
        for sub in ("inbox", "running", "done"):
            (queue / sub).mkdir(parents=True)
        config = {"queue_dir": str(queue), "report_dir": str(serve_dir), "code_rev": code_rev,
                  "idle_timeout_s": idle_timeout_s, "heartbeat_interval_s": 5.0, "identity": identity}
        (serve_dir / "serve.json").write_text(json.dumps(config, indent=1) + "\n")
        heartbeat = project / HEARTBEAT
        heartbeat.unlink(missing_ok=True)
        cmd = unity_command(opts, project, "Serve", serve_dir / "unity.log", ["-tufuSimServe", str(serve_dir / "serve.json")])
        proc = subprocess.Popen(cmd, stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                                start_new_session=True)
        lock.set_holder(proc.pid, owner="serve")
        state = {"pid": proc.pid, "code_rev": code_rev, "queue_dir": str(queue), "serve_dir": str(serve_dir),
                 "project": str(project), "identity": identity, "before": before, "started": now_iso(),
                 "dll_status": "expected", "command": cmd}
        _serve_state_path(run_tree).parent.mkdir(parents=True, exist_ok=True)
        _serve_state_path(run_tree).write_text(json.dumps(state, indent=1) + "\n")
        deadline = time.monotonic() + ready_wait_s
        while _phase(heartbeat) != "edit:idle":
            if proc.poll() is not None or time.monotonic() > deadline:
                log("serve: editor did not reach idle; stopping")
                serve_stop(run_tree=run_tree, opts=opts, log=log)
                raise LauncherError("resident editor failed to start", EXIT_NOT_READY)
            time.sleep(0.5)
        log(f"serve: pid {proc.pid} idle at {code_rev[:10]}")
        return state
    except BaseException:
        if not _serve_state_path(run_tree).exists():
            lock.release()
        raise


def serve_status(*, run_tree: Path | None = None) -> dict:
    run_tree = Path(run_tree or runner.run_worktree())
    path = _serve_state_path(run_tree)
    if not path.exists():
        return {"running": False}
    state = json.loads(path.read_text())
    heartbeat = Path(state["project"]) / HEARTBEAT
    try:
        beat = json.loads(heartbeat.read_text())
    except (OSError, ValueError):
        beat = None
    return {"running": pid_alive(state["pid"]), "pid": state["pid"], "code_rev": state["code_rev"], "heartbeat": beat,
            "queue_dir": state["queue_dir"]}


def serve_submit(request: Mapping, *, run_tree: Path | None = None, worktree: Path | None = None,
                 opts: Options | None = None, out_root: Path | None = None, timeout_s: float | None = None,
                 log=print) -> RunResult:
    """Queue a request with the resident editor and wait for its result."""
    opts = opts or Options()
    run_tree = Path(run_tree or runner.run_worktree())
    worktree = Path(worktree or runner.WORKTREE)
    out_root = Path(out_root or OUT_ROOT)
    state = json.loads(_serve_state_path(run_tree).read_text())
    if not pid_alive(state["pid"]):
        raise LauncherError("resident editor is not running")
    project = Path(state["project"])
    req.validate({**request, "out_dir": "/placeholder"} if not request.get("out_dir") else request)
    r, material = prepare_request(request, project=project, worktree=worktree, run_tree=run_tree,
                                  rev=state["code_rev"], dev=False, opts=opts, out_root=out_root, checkout=False)
    if runner.head(run_tree) != state["code_rev"]:
        raise LauncherError("run worktree moved away from the resident editor's commit")
    run_dir = Path(r["out_dir"])
    if run_dir.exists():
        shutil.rmtree(run_dir)
    run_dir.mkdir(parents=True)
    (run_dir / "request.json").write_text(req.dumps(r))
    write_side_inputs(r, run_dir)
    queue = Path(state["queue_dir"])
    stem = f"{int(time.time() * 1000)}-{r['run_key'][:12]}"
    tmp = queue / f".{stem}.tmp"
    tmp.write_text(req.dumps(r))
    tmp.rename(queue / "inbox" / f"{stem}.json")
    result_path = queue / "done" / f"{stem}.result.json"
    deadline = time.monotonic() + (timeout_s or _overall(r, opts))
    t0 = time.monotonic()
    while not result_path.exists():
        if not pid_alive(state["pid"]):
            raise LauncherError("resident editor exited while the request was queued")
        if time.monotonic() > deadline:
            raise LauncherError("timed out waiting for the resident editor", EXIT_TIMEOUT)
        time.sleep(0.25)
    result = json.loads(result_path.read_text())
    problems = diff_snapshots(state["before"], snapshot(ProtectedPaths.for_project(project), project), editor_alive=True)
    report = {"schema": "tufu.sim.launcher/1", "method": "Serve", "result": result, "wall_s": time.monotonic() - t0,
              **material, "guard_problems": problems, "unity_exit": result["exit_code"],
              "exit_code": EXIT_GUARD if problems else result["exit_code"]}
    log_src = Path(state["serve_dir"]) / "unity.log"
    if log_src.exists():
        shutil.copy2(log_src, run_dir / "unity.log")
    (run_dir / "launcher.json").write_text(json.dumps(report, indent=1, default=str) + "\n")
    log(f"serve: {stem} exit {report['exit_code']} in {report['wall_s']:.1f} s")
    return RunResult(run_dir, report["exit_code"], r, report)


def serve_stop(*, run_tree: Path | None = None, opts: Options | None = None, wait_s: float = 300.0, log=print) -> dict:
    opts = opts or Options()
    run_tree = Path(run_tree or runner.run_worktree())
    path = _serve_state_path(run_tree)
    if not path.exists():
        return {"running": False}
    state = json.loads(path.read_text())
    project = Path(state["project"])
    queue = Path(state["queue_dir"])
    if pid_alive(state["pid"]):
        (queue / "stop").write_text("stop\n")
        deadline = time.monotonic() + wait_s
        while pid_alive(state["pid"]) and time.monotonic() < deadline:
            time.sleep(0.5)
        if pid_alive(state["pid"]):
            os.kill(state["pid"], signal.SIGTERM)
            time.sleep(5)
            if pid_alive(state["pid"]):
                os.kill(state["pid"], signal.SIGKILL)
    left = wait_for_project_free(project, opts.worker_exit_wait_s)
    paths = ProtectedPaths.for_project(project)
    problems = diff_snapshots(state["before"], snapshot(paths, project))
    analytics = [p for p in problems if _is_editor_analytics(p)]
    problems = [p for p in problems if not _is_editor_analytics(p)]
    status = runner.porcelain(run_tree)
    if status.strip():
        problems.append("run worktree not clean after serve: " + " | ".join(status.splitlines()[:10]))
    restored = []
    if any(p.startswith("real ") for p in problems + analytics) and not unity_processes(runner.MAIN_CHECKOUT / runner.PROJECT_SUBDIR):
        restored = restore_real(paths, Path(state["serve_dir"]) / "_real_backup", save=any("save file" in p for p in problems + analytics),
                                prefs=any("PlayerPrefs" in p for p in problems))
    report_path = Path(state["serve_dir"]) / "serve_report.json"
    serve_report = json.loads(report_path.read_text()) if report_path.exists() else {}
    removed = cleanup_sandbox(state["identity"])
    shutil.rmtree(queue, ignore_errors=True)
    if not problems:
        shutil.rmtree(Path(state["serve_dir"]) / "_real_backup", ignore_errors=True)
    path.unlink()
    lock = UnityLock(run_tree)
    lock.release()
    out = {"stopped": True, "editor_exit_code": serve_report.get("exit_code"), "guard_problems": problems,
           "editor_analytics_leftovers": analytics,
           "restored": restored, "workers_left": left, "sandbox_removed": removed, "serve_dir": state["serve_dir"],
           "served": len(serve_report.get("served", []))}
    log(f"serve: stopped (editor exit {out['editor_exit_code']}){'; ' + '; '.join(problems) if problems else ''}")
    return out
