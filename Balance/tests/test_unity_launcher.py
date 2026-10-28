"""Launcher, run worktree and run_key (unit C0b). No Unity: a fake editor script plays its part.

The fake editor (written to a temp dir) reads -projectPath/-logFile/-tufuSimRequest like the real one
and behaves according to FAKE_UNITY_MODE: ok, stall, nobeat, exit:N, touch_dev, touch_real, dirty. With
-tufuSimServe it plays the resident editor: writes sandbox.json, serves the queue (each request's jobs get
a manifest with exit FAKE_SERVE_JOB_EXIT, default FAKE_SERVE_EXIT, the request's exit; touch_real/touch_project
fire while a request runs) until the stop file.
"""

from __future__ import annotations

import json
import os
import re
import subprocess
import sys
import textwrap
import time
from pathlib import Path

import pytest

from tufu_balance.unity import launcher, request as req, runner

REPO = runner.WORKTREE

FAKE_UNITY = textwrap.dedent('''\
    import json, os, sys, time
    from pathlib import Path
    args = sys.argv[1:]
    def arg(name):
        return args[args.index(name) + 1] if name in args else None
    project = Path(arg("-projectPath"))
    Path(arg("-logFile")).write_text("fake unity\\n[TufuSim] job x (1/1): entering Play from Boot\\n")
    beat = project / "Temp/TufuSimulation/heartbeat.json"
    beat.parent.mkdir(parents=True, exist_ok=True)
    def write(progress):
        beat.write_text(json.dumps({"pid": os.getpid(), "phase": "play:Job", "progress": progress}))
    mode = os.environ.get("FAKE_UNITY_MODE", "ok")
    modes = set(mode.split("+"))
    if arg("-tufuSimServe"):
        cfg = json.loads(Path(arg("-tufuSimServe")).read_text())
        queue, report_dir = Path(cfg["queue_dir"]), Path(cfg["report_dir"])
        if os.environ.get("FAKE_EDITOR_SANDBOX"):
            pid = int(os.environ.get("FAKE_EDITOR_PID") or os.getpid())
            (report_dir / "sandbox.json").write_text(json.dumps(
                {"schema": "tufu.sim.sandbox/1", "entry": "serve", "pid": pid,
                 "sandbox": json.loads(os.environ["FAKE_EDITOR_SANDBOX"])}))
        served, n, deadline = [], 0, time.time() + 60
        while time.time() < deadline and not (queue / "stop").exists():
            n += 1
            beat.write_text(json.dumps({"pid": os.getpid(), "phase": "edit:idle", "progress": str(n)}))
            inbox = sorted((queue / "inbox").glob("*.json"))
            if not inbox:
                time.sleep(0.1)
                continue
            running = queue / "running" / inbox[0].name
            inbox[0].rename(running)
            r = json.loads(running.read_text())
            code = int(os.environ.get("FAKE_SERVE_EXIT", "0"))
            job_code = int(os.environ.get("FAKE_SERVE_JOB_EXIT", code))
            jobs = []
            for job in r["jobs"]:
                d = Path(r["out_dir"]) / job["job_id"]
                d.mkdir(parents=True, exist_ok=True)
                (d / "manifest.json").write_text(json.dumps({"job_id": job["job_id"], "exit_code": job_code}))
                jobs.append({"job_id": job["job_id"], "exit_code": job_code})
            if "touch_real" in modes:
                Path(os.environ["FAKE_REAL_SAVE"]).write_text("changed")
            if "touch_project" in modes:
                (project / "ProjectSettings" / "ProjectSettings.asset").write_text("changed")
            result = {"request": running.name, "exit_code": code, "jobs": jobs}
            tmp = queue / "done" / ".result.tmp"
            tmp.write_text(json.dumps(result))
            tmp.rename(queue / "done" / (running.stem + ".result.json"))
            running.rename(queue / "done" / running.name)
            served.append(result)
        (report_dir / "serve_report.json").write_text(json.dumps({"exit_code": 0, "served": served}))
        sys.exit(0)
    if mode == "nobeat":
        time.sleep(60)
    write("0")
    if mode == "stall":
        time.sleep(60)
    for i in range(3):
        time.sleep(0.2)
        write(str(i + 1))
    if mode == "touch_dev":
        (project / "Assets" / "touched.txt").write_text("x")
    if os.environ.get("FAKE_EDITOR_SANDBOX"):
        report = Path(arg("-tufuSimRequest")).parent / "editor_report.json"
        pid = int(os.environ.get("FAKE_EDITOR_PID") or os.getpid())
        report.write_text(json.dumps({"pid": pid, "sandbox": json.loads(os.environ["FAKE_EDITOR_SANDBOX"])}))
    if "touch_real" in modes:
        Path(os.environ["FAKE_REAL_SAVE"]).write_text("changed")
    if "touch_project" in modes:
        (project / "ProjectSettings" / "ProjectSettings.asset").write_text("changed")
    if mode == "analytics":
        events = Path(os.environ["FAKE_REAL_SAVE"]).parent / "Unity" / "cloud-id" / "Editor" / "Analytics" / "ArchivedEvents" / "1"
        events.mkdir(parents=True)
        (events / "c").write_text("session")
    if mode == "dirty":
        (project / "Assets" / "untracked.cs").write_text("// new")
    sys.exit(next((int(m.split(":")[1]) for m in modes if m.startswith("exit:")), 0))
''')


def _git(cwd, *args):
    subprocess.run(["git", "-C", str(cwd), *args], check=True, capture_output=True)


@pytest.fixture
def fake_tree(tmp_path, monkeypatch):
    """A git worktree shaped like the repo (Recovery/StudyProject with a Library) plus a fake Unity."""
    tree = tmp_path / "tree"
    project = tree / "Recovery" / "StudyProject"
    for sub in ("Assets/Plugins", "ProjectSettings", "Packages", "Library"):
        (project / sub).mkdir(parents=True)
    (tree / ".gitignore").write_text("/Balance/out/\n/Recovery/StudyProject/Library/\n/Recovery/StudyProject/Temp/\n")
    (project / "Assets" / "a.cs").write_text("// a\n")
    (project / "ProjectSettings" / "ProjectSettings.asset").write_text(
        "PlayerSettings:\n  companyName: Fake Co\n  productName: 'Fake: ''game'''\n")
    (project / "Packages" / "manifest.json").write_text("{}\n")
    for name in runner.PROTECTED_DLLS:
        (project / "Assets" / "Plugins" / name).write_bytes(name.encode())
    _git(tree, "init", "-q")
    _git(tree, "-c", "user.name=t", "-c", "user.email=t@t", "add", "-A")
    _git(tree, "-c", "user.name=t", "-c", "user.email=t@t", "commit", "-qm", "init")
    # An editor the user has open on the main checkout would make the launcher skip restoring a changed real
    # save (another editor may legitimately write it); the fake runs must not depend on that.
    real_processes = launcher.unity_processes
    main = (runner.MAIN_CHECKOUT / runner.PROJECT_SUBDIR).resolve()
    monkeypatch.setattr(launcher, "unity_processes",
                        lambda project: [] if Path(project).resolve() == main else real_processes(project))
    monkeypatch.setattr(launcher, "DLL_RULES", {
        name: {"expected": runner.sha256_file(project / "Assets" / "Plugins" / name), "damaged": "0" * 64}
        for name in runner.PROTECTED_DLLS})
    unity = tmp_path / "FakeUnity"
    unity.write_text(f"#!{sys.executable}\n" + FAKE_UNITY)
    unity.chmod(0o755)
    real = tmp_path / "real"
    (real / "save").mkdir(parents=True)
    (real / "save" / "game_data.bin").write_text("original")
    paths = launcher.ProtectedPaths(save_dir=real / "save", plist=real / "fake.plist", domain=None)
    opts = launcher.Options(unity=unity, startup_timeout_s=5, stall_timeout_s=2, net_sample_s=0.5, worker_exit_wait_s=1,
                            lock_wait_s=5)
    return {"tree": tree, "project": project, "paths": paths, "opts": opts, "out": tmp_path / "out", "real": real}


def _run(ft, mode, monkeypatch, **kw):
    monkeypatch.setenv("FAKE_UNITY_MODE", mode)
    monkeypatch.setenv("FAKE_REAL_SAVE", str(ft["real"] / "save" / "game_data.bin"))
    request = req.make_request([req.empty_job("fake")], sandbox={"identity": "TufuSimFakeTest"})
    return launcher.run(request, rev="HEAD", opts=ft["opts"], out_root=ft["out"], worktree=ft["tree"],
                        run_tree=ft["tree"], paths=ft["paths"], log=lambda *_: None, **kw)


# ---------------------------------------------------------------- constants and refusals

def test_dll_constants_match_ps1():
    text = (REPO / "Recovery" / "Open-StudyProject.ps1").read_text()
    rules = {m[0]: {"expected": m[1].lower(), "damaged": m[2].lower()}
             for m in re.findall(r"Name='([^']+)';Expected='([0-9A-F]+)';Damaged='([0-9A-F]+)'", text)}
    assert rules == launcher.DLL_RULES


def test_dll_hashes_of_this_worktree_are_expected():
    project = runner.project_dir(REPO)
    assert launcher.check_dlls(project, repair=False) == {name: "expected" for name in launcher.DLL_RULES}


def test_refuses_main_checkout():
    with pytest.raises(launcher.LauncherError, match="main checkout"):
        launcher.run(req.preset("selftest"), dev=True, worktree=runner.MAIN_CHECKOUT)
    with pytest.raises(launcher.LauncherError, match="main checkout"):
        launcher._refuse_main(runner.MAIN_CHECKOUT / runner.PROJECT_SUBDIR, launcher.Options())
    launcher._refuse_main(runner.MAIN_CHECKOUT / runner.PROJECT_SUBDIR, launcher.Options(allow_main_checkout=True))


def test_refuses_dirty_run_worktree(fake_tree, monkeypatch):
    (fake_tree["project"] / "Assets" / "b.cs").write_text("// untracked\n")
    with pytest.raises(runner.WorktreeError, match="not clean"):
        _run(fake_tree, "ok", monkeypatch)
    assert not (fake_tree["tree"] / "Balance" / "out" / "unity.lock").exists()


def test_player_identity_of_the_project():
    assert launcher.player_identity(runner.project_dir(REPO)) == ("Multicast Games", "Butcher hero: hook'n'chew RPG")
    paths = launcher.ProtectedPaths.for_project(runner.project_dir(REPO))
    assert paths.save_dir.name == "Butcher hero_ hook'n'chew RPG"
    assert paths.plist.name == "unity.Multicast Games.Butcher hero: hook'n'chew RPG.plist"


def test_no_network_api_in_game_code():
    """Runbook 4.3, second criterion: the game and editor code have no network API."""
    pattern = re.compile(r"UnityWebRequest|HttpClient|WebClient|System\.Net\b|TcpClient|\bSocket\b|new WWW\b")
    hits = []
    for root in ("Assets/Scripts", "Assets/Editor"):
        for f in (runner.project_dir(REPO) / root).rglob("*.cs"):
            for i, line in enumerate(f.read_text(errors="replace").splitlines(), 1):
                if pattern.search(line):
                    hits.append(f"{f}:{i}: {line.strip()}")
    assert hits == []


# ---------------------------------------------------------------- pgrep, lock, sandbox cleanup

def test_finds_unity_with_case_insensitive_projectpath(tmp_path):
    project = tmp_path / "Some Tree" / "Recovery" / "StudyProject"
    proc = subprocess.Popen([sys.executable, "-c", "import time; time.sleep(30)", "-projectpath", str(project)])
    try:
        deadline = time.time() + 5
        found = []
        while time.time() < deadline and not found:
            found = launcher.unity_processes(Path(str(project).upper()))
            time.sleep(0.1)
        assert [pid for pid, _ in found] == [proc.pid]
        assert launcher.unity_processes(tmp_path / "Other" / "Recovery" / "StudyProject") == []
    finally:
        proc.kill()
        proc.wait()


def test_lock_waits_and_takes_over_stale(tmp_path):
    lock = launcher.UnityLock(tmp_path)
    assert lock.try_acquire("a")
    # Another process cannot take it while we hold it.
    code = ("import sys; from pathlib import Path; from tufu_balance.unity.launcher import UnityLock; "
            f"sys.exit(0 if UnityLock(Path({str(tmp_path)!r})).try_acquire('b') else 3)")
    assert subprocess.run([sys.executable, "-c", code], cwd=runner.BALANCE).returncode == 3
    lock.release()
    assert not lock.path.exists()
    # A dead holder is stale.
    dead = subprocess.Popen([sys.executable, "-c", "pass"])
    dead.wait()
    lock.path.write_text(json.dumps({"pid": dead.pid, "owner": "x"}))
    assert launcher.UnityLock(tmp_path).try_acquire("c")
    with pytest.raises(launcher.LauncherError):
        lock2 = launcher.UnityLock(tmp_path)
        lock2.path.write_text(json.dumps({"pid": 1, "owner": "launchd"}))  # pid 1 is always alive
        lock2.acquire("d", wait_s=0.5)


def test_cleanup_sandbox_only_touches_sandbox_names():
    for bad in ("Multicast Games", "TufuSim/../x", "../TufuSimX", ""):
        with pytest.raises(launcher.LauncherError):
            launcher.cleanup_sandbox(bad)
    assert launcher.cleanup_sandbox("TufuSimNeverUsedByAnyRun") == []


def test_scan_log(tmp_path):
    log = tmp_path / "unity.log"
    log.write_text("Firebase before play is ignored\n[TufuSim] job a (1/1): entering Play from Boot\n"
                   "RemoteConfig: DontDestroyOnLoad only works for root GameObjects\n[AppMetrica] send event\n")
    scan = launcher.scan_log(log)
    assert scan["play_entries"] == 1
    assert scan["sdk_lines"] == ["[AppMetrica] send event"]


# ---------------------------------------------------------------- run_key

def test_run_key_sensitivity(fake_tree):
    tree, project = fake_tree["tree"], fake_tree["project"]
    request = req.preset("selftest")
    unity, dlls = "6000.6.3f1 (45d8eee7de74)", runner.dll_hashes(project)

    def key(rev="HEAD", **kw):
        sha = runner.resolve(tree, rev)
        args = dict(code_rev=sha, trees=runner.tree_hashes(tree, sha), unity=unity, dlls=dlls)
        args.update(kw)
        return runner.run_key(kw.pop("request", request), **{k: v for k, v in args.items() if k != "request"})

    base = key()
    assert key() == base
    # speed-only fields and out_dir do not count
    fast = json.loads(json.dumps(request))
    fast["unity"].update(simulation_speed=9, frame_budget_ms=50.0, heartbeat_interval_s=1.0, ready_timeout_s=10.0)
    fast["out_dir"] = "/elsewhere"
    assert key(request=fast) == base
    slower = json.loads(json.dumps(request))
    slower["jobs"][0]["stop"]["max_frames"] = 31
    assert key(request=slower) != base
    assert key(unity="6000.6.4f1 (x)") != base
    assert key(dlls={**dlls, "Unity.Burst.dll": "f" * 64}) != base
    for rel in ("ProjectSettings/ProjectSettings.asset", "Packages/manifest.json", "Assets/a.cs"):
        (project / rel).write_text((project / rel).read_text() + "# change\n")
        _git(tree, "-c", "user.name=t", "-c", "user.email=t@t", "commit", "-qam", rel)
        changed = key()
        assert changed != base, rel
        base = changed
    # --dev: the dirty state is part of the key, including new untracked files
    dev0 = runner.dev_state(tree)
    (project / "Assets" / "new.cs").write_text("// new\n")
    dev1 = runner.dev_state(tree)
    assert dev1 != dev0 and "Recovery/StudyProject/Assets/new.cs" in dev1["files"]
    assert key(dev=dev1) != key(dev=dev0)
    (project / "Assets" / "new.cs").write_text("// edited\n")
    assert runner.dev_state(tree) != dev1


def test_unity_version_string():
    if not launcher.DEFAULT_UNITY.exists():
        pytest.skip("Unity not installed")
    assert re.fullmatch(r"6000\.6\.3f1 \([0-9a-f]+\)", runner.unity_version(launcher.DEFAULT_UNITY))


# ---------------------------------------------------------------- launcher exit codes with the fake editor

def test_fake_run_ok_and_cache(fake_tree, monkeypatch):
    result = _run(fake_tree, "ok", monkeypatch)
    assert result.exit_code == 0, result.report
    assert result.report["guard_problems"] == []
    assert json.loads((result.run_dir / "request.json").read_text())["run_key"] == result.run_dir.name
    assert not (fake_tree["tree"] / "Balance" / "out" / "unity.lock").exists()
    again = _run(fake_tree, "exit:1", monkeypatch)  # cached: the fake is not started
    assert again.cached and again.exit_code == 0


@pytest.mark.parametrize("mode, code, reason", [
    ("stall", launcher.EXIT_TIMEOUT, "stalled"),
    ("nobeat", launcher.EXIT_TIMEOUT, "no heartbeat"),
    ("exit:4", 4, None),
    ("exit:8", 8, None),
])
def test_fake_run_exit_codes(fake_tree, monkeypatch, mode, code, reason):
    result = _run(fake_tree, mode, monkeypatch)
    assert result.exit_code == code
    if reason:
        assert reason in result.report["reason"]


def test_fake_run_real_save_changed_is_restored_and_fails(fake_tree, monkeypatch):
    result = _run(fake_tree, "touch_real", monkeypatch)
    assert result.exit_code == launcher.EXIT_GUARD
    assert any("real save file changed" in p for p in result.report["guard_problems"])
    assert result.report["restored"] == ["save_dir"]
    assert (fake_tree["real"] / "save" / "game_data.bin").read_text() == "original"


SANDBOX_DATA = "/Users/u/Library/Application Support/TufuSimSandbox/TufuSimFakeTest"


def _proven_sandbox(real_save_dir):
    return {"real_persistent_data_path": str(real_save_dir), "sandbox_persistent_data_path": SANDBOX_DATA,
            "sandbox_product": "TufuSimFakeTest", "prefs_probe_in_sandbox_domain": True,
            "prefs_probe_in_real_domain": False}


@pytest.mark.parametrize("change", [
    None,
    {"prefs_probe_in_real_domain": True},
    {"prefs_probe_in_real_domain": None},
    {"prefs_probe_in_sandbox_domain": False},
    {"prefs_probe_in_sandbox_domain": "true"},
    {"sandbox_persistent_data_path": "/real/save"},
    {"sandbox_persistent_data_path": "/real/save/TufuSimSandbox/x"},
    {"sandbox_persistent_data_path": "/Users/u/Library/Application Support/Other/TufuSimFakeTest"},
    {"sandbox_product": "TufuSimOther"},
    {"real_persistent_data_path": "/real/other"},
    {"real_persistent_data_path": None},
    "other pid",
    "no report",
])
def test_sandbox_proven_needs_every_sign(tmp_path, change):
    if change != "no report":
        sandbox = {**_proven_sandbox("/real/save"), **(change if isinstance(change, dict) else {})}
        pid = 99 if change == "other pid" else 42
        (tmp_path / "editor_report.json").write_text(json.dumps({"pid": pid, "sandbox": sandbox}))
    assert launcher.sandbox_proven(tmp_path, "TufuSimFakeTest", 42, Path("/real/save")) is (change is None)


def _main_editor(monkeypatch, present=lambda look: True):
    """Pretend the user's editor has the main checkout open; ``present(n)`` says whether the n-th look sees it.
    Its import worker is always listed and must not count as the editor."""
    main = (runner.MAIN_CHECKOUT / runner.PROJECT_SUBDIR).resolve()
    other = launcher.unity_processes
    looks = []

    def fake(project):
        if Path(project).resolve() != main:
            return other(project)
        looks.append(1)
        worker = [(434343, f"Unity -batchMode -projectPath {main} -name AssetImportWorker0")]
        return ([(424242, f"Unity -projectPath {main}")] if present(len(looks)) else []) + worker
    monkeypatch.setattr(launcher, "unity_processes", fake)


def _attempt(fake_tree, monkeypatch, mode, sandbox=True):
    if sandbox:
        monkeypatch.setenv("FAKE_EDITOR_SANDBOX", json.dumps(_proven_sandbox(fake_tree["paths"].save_dir)))
    return _run(fake_tree, mode, monkeypatch)


def test_fake_run_real_save_written_by_main_editor_is_attributed(fake_tree, monkeypatch):
    """The user playing in the main checkout's editor writes the real save: recorded, kept, not exit 2."""
    _main_editor(monkeypatch)
    result = _attempt(fake_tree, monkeypatch, "touch_real")
    assert result.exit_code == 0, result.report
    assert result.report["real_changed_by_main_editor"] == ["real save file changed: game_data.bin"]
    assert result.report["guard_problems"] == []
    assert result.report["main_checkout_editors"] == [424242]
    assert result.report["restored"] == [] and "restore_skipped" in result.report
    assert (fake_tree["real"] / "save" / "game_data.bin").read_text() == "changed"
    assert (result.run_dir / "_real_backup" / "save_dir" / "game_data.bin").read_text() == "original"


@pytest.mark.parametrize("mode, sandbox, present, also", [
    ("touch_real", False, lambda n: True, None),                       # no sandbox proof
    ("touch_real", True, lambda n: n > 1, None),                       # editor appeared after the start
    ("touch_real", True, lambda n: n == 1, None),                      # editor gone by the end
    ("touch_real+exit:1", True, lambda n: True, None),                 # this editor did not exit cleanly
    ("touch_real+touch_project", True, lambda n: True, "project file changed on disk"),
])
def test_fake_run_real_save_change_without_attribution_fails(fake_tree, monkeypatch, mode, sandbox, present, also):
    """Any missing condition: exit 2 with the real change listed, the backup kept, and nothing restored while
    another editor may be writing."""
    _main_editor(monkeypatch, present)
    result = _attempt(fake_tree, monkeypatch, mode, sandbox)
    assert result.exit_code == launcher.EXIT_GUARD
    assert "real_changed_by_main_editor" not in result.report
    assert "real save file changed: game_data.bin" in result.report["guard_problems"]
    if also:
        assert any(also in p for p in result.report["guard_problems"])
    assert result.report["restored"] == [] and "restore_skipped" in result.report
    assert (result.run_dir / "_real_backup" / "save_dir" / "game_data.bin").read_text() == "original"


def test_fake_run_editor_analytics_leftovers_are_restored_not_fatal(fake_tree, monkeypatch):
    """A killed editor leaves its analytics session under the real persistentDataPath: restored, not exit 2."""
    result = _run(fake_tree, "analytics", monkeypatch)
    assert result.exit_code == 0, result.report
    assert result.report["editor_analytics_leftovers"] == [
        "real save file changed: Unity/cloud-id/Editor/Analytics/ArchivedEvents/1/c"]
    assert result.report["restored"] == ["save_dir"]
    assert not (fake_tree["real"] / "save" / "Unity").exists()


def test_fake_run_dirty_run_worktree_fails(fake_tree, monkeypatch):
    result = _run(fake_tree, "dirty", monkeypatch)
    assert result.exit_code == launcher.EXIT_GUARD
    assert any("not clean" in p for p in result.report["guard_problems"])


def test_fake_dev_run_changed_worktree_exits_7(fake_tree, monkeypatch):
    monkeypatch.setenv("FAKE_UNITY_MODE", "touch_dev")
    request = req.make_request([req.empty_job("fake")], sandbox={"identity": "TufuSimFakeTest"})
    result = launcher.run(request, dev=True, opts=fake_tree["opts"], out_root=fake_tree["out"], worktree=fake_tree["tree"],
                          run_tree=fake_tree["tree"], paths=fake_tree["paths"], log=lambda *_: None)
    assert result.exit_code == launcher.EXIT_DEV_CHANGED
    assert result.request["code_rev"].endswith("+dev")


def test_fake_run_writes_the_auto_readiness_table(fake_tree, monkeypatch):
    """sandbox.expected_ready_spawners = "auto" (default): P6's table, content-addressed, next to request.json (C1)."""
    if not launcher.balance_db().exists():
        pytest.skip("no extracted database")
    result = _run(fake_tree, "ok", monkeypatch)
    name = result.request["sandbox"]["expected_ready_spawners"]
    assert json.loads((result.run_dir / "request.json").read_text())["sandbox"]["expected_ready_spawners"] == name
    assert (name, (result.run_dir / name).read_text()) == launcher.expected_ready_table("A")


def test_invalid_request_is_refused_before_launch(fake_tree, monkeypatch):
    request = req.make_request([req.empty_job("fake")])
    request["bogus"] = 1
    with pytest.raises(launcher.LauncherError) as e:
        launcher.run(request, rev="HEAD", opts=fake_tree["opts"], out_root=fake_tree["out"], worktree=fake_tree["tree"],
                     run_tree=fake_tree["tree"], paths=fake_tree["paths"], log=lambda *_: None)
    assert e.value.code == launcher.EXIT_BAD_REQUEST


# ---------------------------------------------------------------- resident mode with the fake editor

@pytest.fixture
def serve_tree(fake_tree, monkeypatch):
    """fake_tree plus a stop at teardown, so a failing test never leaves a fake resident editor behind."""
    monkeypatch.setenv("FAKE_REAL_SAVE", str(fake_tree["real"] / "save" / "game_data.bin"))
    yield fake_tree
    if launcher._serve_state_path(fake_tree["tree"]).exists():
        launcher.serve_stop(run_tree=fake_tree["tree"], opts=fake_tree["opts"], wait_s=10, log=lambda *_: None)


def _serve_start(ft, monkeypatch, mode="ok", sandbox=True, **env):
    monkeypatch.setenv("FAKE_UNITY_MODE", mode)
    if sandbox:
        monkeypatch.setenv("FAKE_EDITOR_SANDBOX", json.dumps(_proven_sandbox(ft["paths"].save_dir)))
    for k, v in env.items():
        monkeypatch.setenv(k, v)
    return launcher.serve_start(rev="HEAD", identity="TufuSimFakeTest", opts=ft["opts"], worktree=ft["tree"],
                                run_tree=ft["tree"], out_root=ft["out"], paths=ft["paths"], ready_wait_s=20,
                                log=lambda *_: None)


def _serve_submit(ft):
    request = req.make_request([req.empty_job("fake")], sandbox={"identity": "TufuSimFakeTest"})
    return launcher.serve_submit(request, run_tree=ft["tree"], worktree=ft["tree"], opts=ft["opts"],
                                 out_root=ft["out"], timeout_s=30, log=lambda *_: None)


def _serve_stop(ft):
    return launcher.serve_stop(run_tree=ft["tree"], opts=ft["opts"], wait_s=20, log=lambda *_: None)


def test_fake_serve_clean(serve_tree, monkeypatch):
    state = _serve_start(serve_tree, monkeypatch)
    assert state["main_editors"] == [] and state["paths"]["save_dir"] == str(serve_tree["paths"].save_dir)
    result = _serve_submit(serve_tree)
    assert result.exit_code == 0 and result.report["guard_problems"] == []
    assert json.loads((result.run_dir / "fake" / "manifest.json").read_text())["exit_code"] == 0
    out = _serve_stop(serve_tree)
    assert out["guard_problems"] == [] and out["editor_exit_code"] == 0 and out["served"] == 1
    assert not (Path(state["serve_dir"]) / "_real_backup").exists()


def test_fake_serve_real_save_written_by_main_editor_is_attributed(serve_tree, monkeypatch):
    """The user plays in the main checkout's editor while the resident editor serves: every submit and the
    stop record the real-save change under that editor, never restore it, keep the backup, exit 0."""
    _main_editor(monkeypatch)
    state = _serve_start(serve_tree, monkeypatch, "touch_real")
    assert state["main_editors"] == [424242]
    assert json.loads((Path(state["serve_dir"]) / "sandbox.json").read_text())["pid"] == state["pid"]
    for _ in range(2):
        result = _serve_submit(serve_tree)
        assert result.exit_code == 0, result.report
        assert result.report["real_changed_by_main_editor"] == ["real save file changed: game_data.bin"]
        assert result.report["guard_problems"] == [] and result.report["main_checkout_editors"] == [424242]
    out = _serve_stop(serve_tree)
    assert out["guard_problems"] == [] and out["restored"] == []
    assert out["real_changed_by_main_editor"] == ["real save file changed: game_data.bin"]
    assert (serve_tree["real"] / "save" / "game_data.bin").read_text() == "changed"
    assert (Path(state["serve_dir"]) / "_real_backup" / "save_dir" / "game_data.bin").read_text() == "original"


@pytest.mark.parametrize("mode, sandbox, present, env, also", [
    ("touch_real", False, lambda n: True, {}, None),                              # no sandbox proof
    ("touch_real", True, lambda n: True, {"FAKE_EDITOR_PID": "99"}, None),         # proof from another pid
    ("touch_real", True, lambda n: n > 1, {}, None),                              # editor appeared after start
    ("touch_real", True, lambda n: n == 1, {}, None),                             # editor gone by the submit
    ("touch_real", True, lambda n: True, {"FAKE_SERVE_EXIT": "1"}, None),         # the job did not finish cleanly
    ("touch_real", True, lambda n: True, {"FAKE_SERVE_JOB_EXIT": "1"}, None),     # request exit 0, a job exit 1
    ("touch_real+touch_project", True, lambda n: True, {}, "project file changed on disk"),
])
def test_fake_serve_real_save_change_without_attribution_fails(serve_tree, monkeypatch, mode, sandbox, present, env,
                                                               also):
    _main_editor(monkeypatch, present)
    state = _serve_start(serve_tree, monkeypatch, mode, sandbox, **env)
    result = _serve_submit(serve_tree)
    assert result.exit_code == launcher.EXIT_GUARD
    assert "real_changed_by_main_editor" not in result.report
    assert "real save file changed: game_data.bin" in result.report["guard_problems"]
    if also:
        assert any(also in p for p in result.report["guard_problems"])
    out = _serve_stop(serve_tree)
    assert "real_changed_by_main_editor" not in out and out["guard_problems"]
    if not out["restored"]:
        assert (Path(state["serve_dir"]) / "_real_backup" / "save_dir" / "game_data.bin").read_text() == "original"


def test_fake_serve_other_unity_on_the_run_worktree_blocks_attribution(serve_tree, monkeypatch):
    """Another Unity with the run worktree open could have written the real save: no attribution."""
    _main_editor(monkeypatch)
    state = _serve_start(serve_tree, monkeypatch, "touch_real")
    intruder = [(565656, f"Unity -projectPath {state['project']}")]
    monkeypatch.setattr(launcher, "_other_unity", lambda project, pid: list(intruder))
    result = _serve_submit(serve_tree)
    assert result.exit_code == launcher.EXIT_GUARD and "real_changed_by_main_editor" not in result.report
    assert result.report["others_seen"] == intruder
    assert result.report["guard_problems"] == ["real save file changed: game_data.bin"]
    free = launcher.wait_for_project_free
    monkeypatch.setattr(launcher, "wait_for_project_free", lambda project, wait_s: free(project, wait_s) + intruder)
    out = _serve_stop(serve_tree)
    assert "real_changed_by_main_editor" not in out and out["guard_problems"]
    assert (Path(state["serve_dir"]) / "_real_backup" / "save_dir" / "game_data.bin").read_text() == "original"


# ---------------------------------------------------------------- sandbox hooks in game code

HARNESS = "Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Harness/"


def _hook_lines():
    project = runner.project_dir(REPO)
    hooks = []
    for f in sorted((project / "Assets" / "Scripts").rglob("*.cs")):
        rel = f.relative_to(project).as_posix()
        if rel.startswith(HARNESS):
            continue
        for i, line in enumerate(f.read_text(errors="replace").splitlines(), 1):
            if "SimulationSandbox" in line:
                hooks.append((rel, i, line.strip()))
    return hooks


def test_sandbox_hooks_are_registered_in_the_runbook():
    """Design 1.5 C# 11: every hook is listed with file and line in the Runbook."""
    runbook = (REPO / "Docs" / "Balance" / "Simulator" / "Runbook.md").read_text()
    hooks = _hook_lines()
    assert {Path(rel).name for rel, _, _ in hooks} == {"TimeService.cs", "ABTestModel.cs", "UserModel.cs", "MainSceneInitializer.cs"}
    missing = [f"{Path(rel).name}:{line}" for rel, line, _ in hooks if f"{Path(rel).name}:{line}" not in runbook]
    assert missing == []


def test_sandbox_hooks_fall_back_to_the_original_code():
    """Each hook either branches on Active with the original expression as the other arm, or guards
    a skip with !Active; activation itself is compiled only into the editor."""
    for rel, line, text in _hook_lines():
        assert "SimulationSandbox.Active" in text, (rel, line)
        assert ("SimulationSandbox.Active ?" in text or "SimulationSandbox.Active ||" in text
                or "if (Codebase.Simulation.Harness.SimulationSandbox.Active) return" in text
                or "SimulationSandbox.Active &&" in text), (rel, line, text)
    sandbox = (runner.project_dir(REPO) / HARNESS / "SimulationSandbox.cs").read_text()
    editor_only = sandbox[sandbox.index("#if UNITY_EDITOR"):sandbox.index("#endif")]
    assert sandbox.count("RuntimeInitializeOnLoadMethod") == editor_only.count("RuntimeInitializeOnLoadMethod") == 2
    assert sandbox.count("active = true") == editor_only.count("active = true") == 1
