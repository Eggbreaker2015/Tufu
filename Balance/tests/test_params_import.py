"""``params import`` around the editor (design 4.8, 1.5 C# rule 6, 7.3 C3c), with a fake editor: the parameter
asset is the one project file the import may change; the result is checked independently, copied to the
caller's worktree and the run worktree is restored. A refusal, any other change or a wrong asset fails."""

from __future__ import annotations

import json
import subprocess
import sys
import textwrap
from pathlib import Path

import pytest

from tufu_balance.calibrate import params as P
from tufu_balance.unity import launcher, paramsio, runner

BALANCE = Path(__file__).resolve().parents[1]

FAKE = textwrap.dedent('''\
    import json, os, sys
    from pathlib import Path
    sys.path.insert(0, os.environ["FAKE_BALANCE"])
    from tufu_balance.unity import paramsio
    args = sys.argv[1:]
    arg = lambda n: args[args.index(n) + 1] if n in args else None
    project = Path(arg("-projectPath"))
    Path(arg("-logFile")).write_text("fake unity\\n")
    assert arg("-executeMethod") == paramsio.IMPORTER
    doc = json.loads(Path(arg("-tufuSimParams")).read_text())
    mode = os.environ.get("FAKE_IMPORT_MODE", "ok")
    report = {"schema": paramsio.IMPORT_REPORT_SCHEMA, "ok": False, "entered_play": mode == "refuse"}
    code = 0
    if mode == "refuse":
        report["error"] = "refused: this editor process has entered Play mode"
        code = 2
    else:
        params = dict(doc["params"])
        if mode == "wrong_value":
            params["Seed"] += 1
        asset = project / paramsio.ASSET_REL
        asset.write_text(paramsio.asset_text(params), encoding="utf-8")
        h = doc["params_hash"]
        report.update(ok=True, params_hash=h, request_hash=h, effective_hash=h, read_back_hash=h,
                      file_sha256=paramsio.runner.sha256_file(asset))
        if mode == "touch_other":
            (project / "ProjectSettings" / "ProjectSettings.asset").write_text("changed")
    Path(arg("-tufuSimParamsReport")).write_text(json.dumps(report))
    sys.exit(code)
''')


def _git(cwd, *args):
    subprocess.run(["git", "-C", str(cwd), *args], check=True, capture_output=True)


@pytest.fixture
def tree(tmp_path, monkeypatch):
    tree = tmp_path / "tree"
    project = tree / "Recovery" / "StudyProject"
    for sub in ("Assets/Plugins", "Assets/Simulation", "ProjectSettings", "Packages", "Library"):
        (project / sub).mkdir(parents=True)
    (tree / ".gitignore").write_text("/Balance/out/\n/Recovery/StudyProject/Library/\n")
    (project / "ProjectSettings" / "ProjectSettings.asset").write_text("PlayerSettings:\n  companyName: Fake\n  productName: Fake\n")
    (project / "Packages" / "manifest.json").write_text("{}\n")
    (project / paramsio.ASSET_REL).write_text(paramsio.asset_text(P.resolve(["defaults"]).params), encoding="utf-8")
    for name in runner.PROTECTED_DLLS:
        (project / "Assets" / "Plugins" / name).write_bytes(name.encode())
    _git(tree, "init", "-q")
    _git(tree, "-c", "user.name=t", "-c", "user.email=t@t", "add", "-A")
    _git(tree, "-c", "user.name=t", "-c", "user.email=t@t", "commit", "-qm", "init")
    main = (runner.MAIN_CHECKOUT / runner.PROJECT_SUBDIR).resolve()
    real_processes = launcher.unity_processes
    monkeypatch.setattr(launcher, "unity_processes",
                        lambda p: [] if Path(p).resolve() == main else real_processes(p))
    monkeypatch.setattr(launcher, "DLL_RULES", {
        name: {"expected": runner.sha256_file(project / "Assets" / "Plugins" / name), "damaged": "0" * 64}
        for name in runner.PROTECTED_DLLS})
    unity = tmp_path / "FakeUnity"
    unity.write_text(f"#!{sys.executable}\n" + FAKE)
    unity.chmod(0o755)
    monkeypatch.setenv("FAKE_BALANCE", str(BALANCE))
    real = tmp_path / "real"
    (real / "save").mkdir(parents=True)
    (real / "save" / "game_data.bin").write_text("original")
    return {"tree": tree, "project": project, "tmp": tmp_path,
            "paths": launcher.ProtectedPaths(save_dir=real / "save", plist=real / "fake.plist", domain=None),
            "opts": launcher.Options(unity=unity, worker_exit_wait_s=1, lock_wait_s=5)}


OVERRIDES = {"FastAfkStepMinutes": 90.0, "Seed": 7, "RollMode": 1}


def _import(t, mode, monkeypatch, overrides=OVERRIDES):
    monkeypatch.setenv("FAKE_IMPORT_MODE", mode)
    return paramsio.import_params([], overrides, rev="HEAD", dest=t["tmp"] / "dest" / "params.asset", opts=t["opts"],
                                  worktree=t["tree"], run_tree=t["tree"], paths=t["paths"], out_root=t["tmp"] / "out",
                                  timeout_s=60, log=lambda *_: None)


def test_import_writes_checks_copies_and_restores(tree, monkeypatch):
    result = _import(tree, "ok", monkeypatch)
    assert result.exit_code == 0, result.report
    assert all(result.report["checks"].values())
    resolved = P.resolve(["defaults"], OVERRIDES)
    assert paramsio.read_asset(result.dest) == paramsio.effective(resolved.params)
    assert result.report["params_hash"] == resolved.params_hash
    assert not runner.porcelain(tree["tree"]).strip() and result.report["restored_run_worktree_asset"]
    assert json.loads((result.out_dir / "params.json").read_text())["params_hash"] == resolved.params_hash
    assert (result.out_dir / "import.json").exists()


@pytest.mark.parametrize("mode,code,problem", [
    ("refuse", 2, None),
    ("touch_other", 2, "project file changed on disk: ProjectSettings/ProjectSettings.asset"),
    ("wrong_value", 1, "import checks failed"),
])
def test_import_failures(tree, monkeypatch, mode, code, problem):
    result = _import(tree, mode, monkeypatch)
    assert result.exit_code == code, result.report
    assert result.dest is None and not (tree["tmp"] / "dest" / "params.asset").exists()
    if problem:
        assert any(problem in p for p in result.report["guard_problems"]), result.report["guard_problems"]
    # The parameter asset is always back at the committed text; other changes stay for inspection.
    assert result.report["restored_run_worktree_asset"]


def test_dest_inside_the_run_worktree_is_refused(tree, monkeypatch):
    with pytest.raises(launcher.LauncherError):
        paramsio.import_params([], OVERRIDES, rev="HEAD", dest=tree["project"] / paramsio.ASSET_REL, opts=tree["opts"],
                               worktree=tree["tree"], run_tree=tree["tree"], paths=tree["paths"], out_root=tree["tmp"] / "out",
                               log=lambda *_: None)


def test_unknown_layer_field_is_refused_before_unity(tree, monkeypatch):
    with pytest.raises(P.ParamsError):
        _import(tree, "ok", monkeypatch, overrides={"Bogus": 1})
