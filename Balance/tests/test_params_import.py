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
    modes = os.environ.get("FAKE_IMPORT_MODE", "ok").split("+")
    report = {"schema": paramsio.IMPORT_REPORT_SCHEMA, "ok": False, "entered_play": "refuse" in modes or "played" in modes}
    code = 0
    if "real_save" in modes:
        Path(os.environ["FAKE_REAL_SAVE"]).write_text(f"rewritten by an editor {os.getpid()}")
    if "refuse" in modes:
        report["error"] = "refused: this editor process has entered Play mode"
        code = 2
    elif "reject" in modes:
        report["error"] = "params: Could not find member 'Bogus'"
        code = 5
    else:
        params = dict(doc["params"])
        if "wrong_value" in modes:
            params["Seed"] += 1
        asset = project / paramsio.ASSET_REL
        asset.write_text(paramsio.asset_text(params), encoding="utf-8")
        h = doc["params_hash"]
        loader = {"source": "asset", "asset_found": True, "error": None, "hash": h, "result_hash": h}
        report.update(ok=True, params_hash=h, request_hash=h, effective_hash=h, read_back_hash=h,
                      file_sha256=paramsio.runner.sha256_file(asset), read_back_job=dict(loader),
                      read_back_interactive=dict(loader))
        if "touch_other" in modes:
            (project / "ProjectSettings" / "ProjectSettings.asset").write_text("changed")
        if "new_file" in modes:
            (project / "Assets" / "Simulation" / "Extra.asset").write_text("new")
        if "bad_sha" in modes:
            report["file_sha256"] = "0" * 64
        if "loader_defaults" in modes:
            report["read_back_job"]["result_hash"] = "1" * 64
        if "interactive_defaults" in modes:
            report["read_back_interactive"]["source"] = "code_defaults"
        if "stage_asset" in modes:
            import subprocess
            subprocess.run(["git", "-C", str(project), "add", "--", str(asset)], check=True)
        if "fail_after_write" in modes:
            report.update(ok=False, error="read back: differs")
            code = 1
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
    monkeypatch.setenv("FAKE_REAL_SAVE", str(real / "save" / "game_data.bin"))
    monkeypatch.setattr(launcher, "main_checkout_editors", lambda: set())
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
    ("new_file", 2, "run worktree changed beyond the parameter asset: ?? Recovery/StudyProject/Assets/Simulation/Extra.asset"),
    ("wrong_value", 1, "import checks failed: asset_values, asset_hash"),
    ("bad_sha", 1, "import checks failed: asset_file_sha256"),
    ("played", 1, "import checks failed: importer_report"),
    ("loader_defaults", 1, "import checks failed: importer_read_back_job"),
    ("interactive_defaults", 1, "import checks failed: importer_read_back_interactive"),
    ("fail_after_write", 2, "the parameter asset changed although the import failed"),
    ("stage_asset", 2, "run worktree changed beyond the parameter asset: M  Recovery/StudyProject/Assets/Simulation/"),
    ("reject", 5, None),
    ("real_save", 2, "real save file changed"),
    ("real_save+played", 2, "real save file changed"),
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


def test_real_save_changes_while_the_users_editor_runs_are_attributed(tree, monkeypatch):
    """The importer never enters Play, so a real save rewritten while an editor on the main checkout ran for the
    whole import is that editor's (Runbook 13, 17); not when the importer says it played or failed."""
    monkeypatch.setattr(launcher, "main_checkout_editors", lambda: {4242})
    result = _import(tree, "ok+real_save", monkeypatch)
    assert result.exit_code == 0, result.report
    assert result.report["real_changed_by_main_editor"] and result.report["main_checkout_editors"] == [4242]
    assert result.dest is not None and result.report["guard_problems"] == []
    for mode in ("real_save+played", "real_save+fail_after_write", "real_save+reject"):
        result = _import(tree, mode, monkeypatch)
        assert result.exit_code == 2 and any(p.startswith("real save") for p in result.report["guard_problems"]), mode
        assert result.dest is None and "real_changed_by_main_editor" not in result.report


def test_non_float32_values_are_refused_before_unity(tree, monkeypatch):
    with pytest.raises(paramsio.ParamsIoError, match="FastAfkStepMinutes"):
        _import(tree, "ok", monkeypatch, overrides={"FastAfkStepMinutes": 120.00000001})
    assert not (tree["tmp"] / "out").exists()
    result = paramsio.import_params([], {"FastAfkStepMinutes": 120.00000001}, rev="HEAD", dest=tree["tmp"] / "dest" / "p.asset",
                                    opts=tree["opts"], worktree=tree["tree"], run_tree=tree["tree"], paths=tree["paths"],
                                    out_root=tree["tmp"] / "out", timeout_s=60, float32=True, log=lambda *_: None)
    assert result.exit_code == 0, result.report
    assert paramsio.read_asset(result.dest)["FastAfkStepMinutes"] == 120.0
    rounded = paramsio.round_float32(paramsio.resolve_layers([], {"FastAfkStepMinutes": 120.00000001}))
    assert result.report["params_hash"] == rounded.params_hash == P.resolve(["defaults"]).params_hash
