"""C8 without Unity: request fields, the C#/Python constants, the plan checker and every refusal of
``bake --apply`` before an editor starts (design 7.3 C8). The Unity side is tests/unity/test_bake_tool.py."""

from __future__ import annotations

import hashlib
import json
import re
import struct
import subprocess
from pathlib import Path

import pytest

from tufu_balance.unity import bake_report as BR, request as req, runner

CB = runner.WORKTREE / "Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Harness"
ED = runner.WORKTREE / "Recovery/StudyProject/Assets/Editor/Simulation"


def _hex(v: float) -> str:
    return struct.pack(">d", v).hex()


# ---------------------------------------------------------------- request

def _job(**kw):
    job = req.make_job("j", day=3)
    job.update(kw)
    return job


@pytest.mark.parametrize("job", [
    _job(outputs={"bake_report": True}),
    _job(outputs={"bake_report": True}, bake={"snapshots": "shipped"}),
    {**req.empty_job("e"), "outputs": {"bake_report": True}, "bake": {"snapshots": "shipped"}},
    _job(outputs={"bake_report": False}),
])
def test_valid_bake_requests(job):
    req.validate(req.make_request([job], out_dir="/x"))


@pytest.mark.parametrize("job, message", [
    (_job(bake={"snapshots": "run"}), "outputs.bake_report is not true"),
    (_job(outputs={"bake_report": True}, bake={"snapshots": "disk"}), "snapshots must be one of"),
    (_job(outputs={"bake_report": True}, bake={"snapshots": "run", "x": 1}), "unknown field"),
    ({**req.empty_job("e"), "outputs": {"bake_report": True}}, 'needs a job that runs the simulation'),
    (_job(outputs={"bake_report": True}, bake=[]), "must be dict"),
])
def test_invalid_bake_requests(job, message):
    with pytest.raises(req.RequestError, match=message):
        req.validate(req.make_request([job], out_dir="/x"))


def test_csharp_constants_agree():
    tool = (ED / "SimulationBakeTool.cs").read_text()
    export = (CB / "SimulationBakeExport.cs").read_text()
    reasons = re.search(r"string\[\] Reasons =\s*\{(.*?)\};", tool, re.S).group(1)
    assert tuple(re.findall(r'"(\w+)"', reasons)) == BR.REASONS
    env = re.search(r"RefusedEnvironment = \{(.*?)\};", tool, re.S).group(1)
    assert tuple(re.findall(r'"(\w+)"', env)) == BR.REFUSED_ENV
    header = "".join(re.findall(r'"([^"]*)"', re.search(r"CsvHeader = (.*?);", export, re.S).group(1)))
    assert header.split(",") == [n for n, _ in BR.CSV_COLUMNS]
    for name, value in (("PlanSchema", BR.PLAN_SCHEMA), ("ApplySchema", BR.APPLY_SCHEMA),
                        ("SelfTestSchema", BR.SELFTEST_SCHEMA), ("MainScenePath", BR.MAIN_SCENE),
                        ("ApplyManifestFile", BR.APPLY_MANIFEST), ("CallerCli", BR.CALLER_CLI)):
        assert f'{name} = "{value}"' in tool
    assert f'Schema = "{BR.REPORT_SCHEMA}"' in export
    assert f'JsonFile = "{BR.JSON_FILE}", CsvFile = "{BR.CSV_FILE}"' in export


def test_skip_reasons_on_the_extracted_scene(db):
    counts = {r: 0 for r in BR.REASONS}
    for row in db.execute("SELECT * FROM spawners"):
        for r in BR.expected_reasons(row, 1.0, 1.0, in_report=True, report_error=False, duplicate=False):
            counts[r] += 1
    assert counts == {**{r: 0 for r in BR.REASONS}, "event": 6, "vip": 10, "boss_tower": 1, "dummy": 1, "hand_balance": 2}
    boss = db.execute("SELECT * FROM spawners WHERE go_name = 'BossTower'").fetchone()
    assert BR.expected_reasons(boss, float("inf"), 20.0, in_report=True, report_error=False, duplicate=False) == \
        ["boss_tower", "non_finite"]


# ---------------------------------------------------------------- plan checker

def _plan_and_report(db, limit=40):
    from tufu_balance.unity.spawner_map import load_identity_map
    identity = load_identity_map(db)
    rows, spawners = [], []
    for i, r in enumerate(db.execute("SELECT * FROM spawners ORDER BY scene, file_id LIMIT ?", (limit,))):
        e = identity.by_spawner_id[(str(r["scene"]), int(r["file_id"]))]
        oh, od = float(r["stat.Health"]), float(r["stat.Damage"])
        nh, nd = (oh, od) if i % 3 == 0 else (oh + 1, od * 2)
        reasons = BR.expected_reasons(r, nh, nd, in_report=True, report_error=False, duplicate=False)
        action = "skip" if reasons else "unchanged" if (nh, nd) == (oh, od) else "write"
        spawners.append({"id": e.runtime_id, "registered": e.registers, "report": {"NewHealth": _hex(nh), "NewDamage": _hex(nd)}})
        rows.append({"id": e.runtime_id, "action": action, "reasons": reasons, "old_health_bits": _hex(oh),
                     "old_damage_bits": _hex(od), "new_health_bits": _hex(nh), "new_damage_bits": _hex(nd)})
    by_reason = {r: sum(r in x["reasons"] for x in rows) for r in BR.REASONS}
    counts = {a: sum(x["action"] == a for x in rows) for a in ("write", "unchanged", "skip")}
    plan = {"schema": BR.PLAN_SCHEMA, "rows": rows, "counts": {**counts, "by_reason": by_reason}}
    return plan, {"schema": BR.REPORT_SCHEMA, "snapshots": "run", "spawners": spawners}


def test_check_plan_reports_each_planted_fault(db):
    plan, report = _plan_and_report(db)
    res = BR.check_plan(plan, report, db)
    assert not res["differ"] and all(res["counts_equal"].values()), res
    plan["rows"][1]["old_health_bits"] = _hex(-1.0)
    plan["rows"][2]["action"] = "unchanged"
    plan["rows"][4]["reasons"] = ["hand_balance"]
    res = BR.check_plan(plan, report, db)
    assert {d["id"]: d["fields"] for d in res["differ"]} == {
        plan["rows"][1]["id"]: ["old_health"], plan["rows"][2]["id"]: ["action"], plan["rows"][4]["id"]: ["reasons"]}


# ---------------------------------------------------------------- apply refusals

def _git(cwd: Path, *args: str) -> str:
    return subprocess.run(["git", "-C", str(cwd), *args], check=True, capture_output=True, text=True).stdout


@pytest.fixture
def scene_repo(tmp_path):
    repo = tmp_path / "repo"
    scene = repo / "Recovery/StudyProject/Assets/Scenes/Main.unity"
    scene.parent.mkdir(parents=True)
    scene.write_text("%YAML 1.1\n--- !u!1 &1\nGameObject: {}\n")
    _git(repo.parent, "init", "-q", str(repo))
    _git(repo, "-c", "user.name=t", "-c", "user.email=t@t", "add", ".")
    _git(repo, "-c", "user.name=t", "-c", "user.email=t@t", "commit", "-qm", "scene")
    plan = {"schema": BR.PLAN_SCHEMA, "scene": BR.MAIN_SCENE, "scene_sha256": hashlib.sha256(scene.read_bytes()).hexdigest(),
            "code_rev": _git(repo, "rev-parse", "HEAD").strip(), "counts": {"write": 1}, "rows": []}
    plan_path = tmp_path / "bake_plan.json"
    plan_path.write_text(json.dumps(plan, indent=1) + "\n")
    return scene, plan_path, hashlib.sha256(plan_path.read_bytes()).hexdigest()


def test_apply_refusals(scene_repo, tmp_path):
    scene, plan_path, token = scene_repo
    ok = dict(dest=scene, caller="cli", environ={})
    assert BR.apply_refusal(plan_path, token, **ok) is None
    refusals = {
        "CI": BR.apply_refusal(plan_path, token, **{**ok, "environ": {"CI": "true"}}),
        "TUFU_REFRESH": BR.apply_refusal(plan_path, token, **{**ok, "environ": {"TUFU_REFRESH": "1"}}),
        "GITHUB_ACTIONS": BR.apply_refusal(plan_path, token, **{**ok, "environ": {"GITHUB_ACTIONS": "true"}}),
        "refresh caller": BR.apply_refusal(plan_path, token, **{**ok, "caller": "refresh"}),
        "wrong token": BR.apply_refusal(plan_path, token[::-1], **ok),
        "no token": BR.apply_refusal(plan_path, None, **ok),
    }
    scene.write_text(scene.read_text() + "# edit\n")
    refusals["dirty scene"] = BR.apply_refusal(plan_path, token, **ok)
    _git(scene.parents[4], "checkout", "-q", "--", ".")
    untracked = scene.with_name("Other.unity")
    untracked.write_bytes(scene.read_bytes())
    refusals["untracked scene"] = BR.apply_refusal(plan_path, token, **{**ok, "dest": untracked})
    pointer = tmp_path / "pointer.unity"
    pointer.write_bytes(BR.LFS_POINTER + b"v1\noid sha256:00\nsize 1\n")
    refusals["lfs pointer"] = BR.apply_refusal(plan_path, token, **{**ok, "dest": pointer})
    for name, change in (("other scene file", {"scene_sha256": "0" * 64}), ("writes nothing", {"counts": {"write": 0}}),
                         ("other scene", {"scene": "Assets/Scenes/Boot.unity"})):
        doc = {**json.loads(plan_path.read_text()), **change}
        other = tmp_path / f"{name}.json"
        other.write_text(json.dumps(doc))
        refusals[name] = BR.apply_refusal(other, hashlib.sha256(other.read_bytes()).hexdigest(), **ok)
    assert all(r and r.startswith("refused") for r in refusals.values()), refusals


def test_apply_refuses_before_any_editor_starts(scene_repo, monkeypatch):
    scene, plan_path, token = scene_repo

    def no_editor(*a, **kw):
        raise AssertionError("an editor was started")

    monkeypatch.setattr(BR, "_entry", no_editor)
    with pytest.raises(BR.BakeToolError, match="refused: token"):
        BR.apply(plan_path, "0" * 64, dest=scene)
    monkeypatch.setenv("CI", "1")
    with pytest.raises(BR.BakeToolError, match="refused: CI"):
        BR.apply(plan_path, token, dest=scene)
    monkeypatch.delenv("CI")
    with pytest.raises(BR.BakeToolError, match="caller"):
        BR.apply(plan_path, token, dest=scene, caller="refresh")
    # This worktree's Main.unity (an LFS pointer in unit worktrees, else another file than the plan's).
    with pytest.raises(BR.BakeToolError, match="refused"):
        BR.apply(plan_path, token)


def test_cli_apply_needs_a_plan(capsys):
    from tufu_balance.unity.__main__ import main
    assert main(["bake", "--apply", "--token", "0" * 64]) == 5
    assert "needs --plan-file or JOB_DIR" in capsys.readouterr().err
