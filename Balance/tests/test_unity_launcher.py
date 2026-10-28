"""Launcher, run worktree and run_key (unit C0b). No Unity: a fake editor script plays its part.

The fake editor (written to a temp dir) reads -projectPath/-logFile/-tufuSimRequest like the real one
and behaves according to FAKE_UNITY_MODE: ok, stall, nobeat, exit:N, touch_dev, touch_real, dirty.
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
    if mode == "touch_real":
        Path(os.environ["FAKE_REAL_SAVE"]).write_text("changed")
    if mode == "analytics":
        events = Path(os.environ["FAKE_REAL_SAVE"]).parent / "Unity" / "cloud-id" / "Editor" / "Analytics" / "ArchivedEvents" / "1"
        events.mkdir(parents=True)
        (events / "c").write_text("session")
    if mode == "dirty":
        (project / "Assets" / "untracked.cs").write_text("// new")
    sys.exit(int(mode.split(":")[1]) if mode.startswith("exit:") else 0)
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


def test_invalid_request_is_refused_before_launch(fake_tree, monkeypatch):
    request = req.make_request([req.empty_job("fake")])
    request["bogus"] = 1
    with pytest.raises(launcher.LauncherError) as e:
        launcher.run(request, rev="HEAD", opts=fake_tree["opts"], out_root=fake_tree["out"], worktree=fake_tree["tree"],
                     run_tree=fake_tree["tree"], paths=fake_tree["paths"], log=lambda *_: None)
    assert e.value.code == launcher.EXIT_BAD_REQUEST


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
