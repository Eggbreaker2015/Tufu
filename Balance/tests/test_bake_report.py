"""C8 without Unity: request fields, the C#/Python constants, the plan checker and every refusal of
``bake --apply`` before an editor starts (design 7.3 C8). The Unity side is tests/unity/test_bake_tool.py."""

from __future__ import annotations

import copy
import hashlib
import json
import math
import re
import struct
import subprocess
import sys
from pathlib import Path

import pytest

from tufu_balance.unity import bake_report as BR, launcher, probes as P, request as req, runner

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
    plan["rows"][5]["new_health_bits"] = _hex(struct.unpack(">d", bytes.fromhex(plan["rows"][5]["new_health_bits"]))[0] + 1)
    plan["rows"][7]["new_damage_bits"] = None
    res = BR.check_plan(plan, report, db)
    assert {d["id"]: d["fields"] for d in res["differ"]} == {
        plan["rows"][1]["id"]: ["old_health"], plan["rows"][2]["id"]: ["action"], plan["rows"][4]["id"]: ["reasons"],
        plan["rows"][5]["id"]: ["new_health"], plan["rows"][7]["id"]: ["new_damage"]}


def test_check_plan_reason_order_and_duplicates(db):
    """Reasons in the fixed order (not sorted), and a row id listed twice in the plan is a duplicate."""
    from tufu_balance.unity.spawner_map import load_identity_map
    identity = load_identity_map(db)
    plan, report = _plan_and_report(db, limit=10)
    vip = db.execute("SELECT * FROM spawners WHERE IsVip = 1 AND IsEvent = 0 AND HandBalance = 0 AND IsDummy = 0 "
                     "ORDER BY scene, file_id LIMIT 1").fetchone()
    vid = identity.by_spawner_id[(str(vip["scene"]), int(vip["file_id"]))].runtime_id
    oh, od = float(vip["stat.Health"]), float(vip["stat.Damage"])
    report["spawners"].append({"id": vid, "registered": True, "report": {"NewHealth": _hex(float("nan")), "NewDamage": _hex(od)}})
    plan["rows"].append({"id": vid, "action": "skip", "reasons": ["vip", "non_finite"], "old_health_bits": _hex(oh),
                         "old_damage_bits": _hex(od), "new_health_bits": _hex(float("nan")), "new_damage_bits": _hex(od)})
    dup = copy.deepcopy(plan["rows"][1])
    assert dup["reasons"] == [] and dup["action"] in ("write", "unchanged")
    first_action = dup["action"]
    for r in (plan["rows"][1], dup):
        r.update(action="skip", reasons=["duplicate_id"])
    plan["rows"].append(dup)
    plan["counts"] = {"write": sum(r["action"] == "write" for r in plan["rows"]),
                      "unchanged": sum(r["action"] == "unchanged" for r in plan["rows"]),
                      "skip": sum(r["action"] == "skip" for r in plan["rows"]),
                      "by_reason": {r: sum(r in x["reasons"] for x in plan["rows"]) for r in BR.REASONS}}
    res = BR.check_plan(plan, report, db)
    assert not res["differ"] and all(res["counts_equal"].values()), res
    assert res["counts"]["by_reason"]["duplicate_id"] == 2 and res["counts"]["by_reason"]["non_finite"] == 1
    plan["rows"][-2]["reasons"] = ["non_finite", "vip"]
    plan["rows"][-1].update(action=first_action, reasons=[])
    res = BR.check_plan(plan, report, db)
    assert {d["id"]: d["fields"] for d in res["differ"]} == {vid: ["reasons"], dup["id"]: ["action", "reasons"]}


# ---------------------------------------------------------------- X4 and DP5 comparators on planted faults

def _encode_stats(stats) -> str:
    return "".join(P._int_hex(st.key) + "".join(P.dhex(v) for v in (st.value, st.stats_from_features_flat,
                   st.percent_multiplier, st.total_flat_value, st.total_value, st.current_gpm)) for st in stats)


def _encode_input(s) -> dict:
    """The C# ``inputs`` encoding of one snapshot (SimulationBakeExport: hex stats and pairs)."""
    return {"p": P.fhex(s.playtime), "tp": P.fhex(s.total_playtime), "loc": int(s.location), "tci": int(s.time_chunk_index),
            "stats": _encode_stats(s.stats), "items": [[it.key, it.level, it.copies] for it in s.items],
            "dps": "".join(P._int_hex(w) + P.dhex(v) for w, v in s.dps_data),
            "def": "".join(P._int_hex(w) + P.dhex(v) for w, v in s.defense_data)}


@pytest.fixture(scope="module")
def oracle_report(db):
    """A shipped report written from the oracle's own outputs (every field compare_x4 reads), with the tag of
    each spawner and whether it is day mode."""
    from tufu_balance.model import bake as B, data as MD
    from tufu_balance.model.enums import StatType
    from tufu_balance.model.f32 import f32
    from tufu_balance.unity.spawner_map import load_identity_map
    ctx = MD.load_bake_context(db)
    armors = MD.load_armor_configs(db)
    rows = {(str(r["scene"]), int(r["file_id"])): r for r in MD.load_spawner_rows(db)}
    spawners, meta = [], {}
    for e in load_identity_map(db).entries:
        sp = MD.spawner_input_from_row(rows[e.spawner_id], armors)
        row = {"id": e.runtime_id, "registered": e.registers}
        meta[e.runtime_id] = {"tag": f"{e.spawner_id[0]}:{e.spawner_id[1]}", "is_days": sp.is_days}
        try:
            o = B.bake_spawner(ctx, sp)
        except B.BakeError as err:
            spawners.append({**row, "error": str(err)})
            continue
        rep = {field: (P.fhex if kind == "f" else P.dhex)(getattr(o, attr)) for attr, (field, kind) in P.DP5_FIELDS.items()}
        rep.update({field: int(getattr(o, attr)) for attr, field in P.DP5_INTS.items()})
        snap = ctx.snapshot_for_bake(f32(o.recalc_optimum_time), sp.is_days)
        rep.update(NewHealth=P.dhex(o.new_health), NewDamage=P.dhex(o.new_damage),
                   PlayerAbilityDpsMultiplier=P.dhex(o.ability_dps + 1.0), PlayerAblityEhpMultiplier=P.dhex(o.ability_ehp + 1.0),
                   DefenseFromSnapshot=P.dhex(o.defense), PlayerBlockChanceRaw=P.dhex(snap.total(StatType.BlockChance)),
                   PlayerBlockAmountRaw=P.dhex(snap.total(StatType.BlockAmount)),
                   SnapshotSource=None if o.snapshot_source_index is None else
                   f"{'t' if sp.is_days else 's'}:{o.snapshot_source_index}")
        spawners.append({**row, "report": rep})
    return {"schema": BR.REPORT_SCHEMA, "snapshots": "shipped", "spawners": spawners}, meta


def _bump(hex_text: str, ulps: int = 1) -> str:
    n = 16 if len(hex_text) == 16 else 8
    return format(int(hex_text, 16) + ulps, f"0{n}x")


def test_compare_x4_reports_each_planted_fault(db, oracle_report):
    report, meta = oracle_report
    clean = BR.compare_x4(None, db, report=report)
    assert not clean["mismatches"] and not clean["unknown"] and not clean["missing"], clean["mismatches"][:5]
    n_ok = clean["new_bits_equal"]
    assert n_ok == clean["spawners"] - len(clean["errors_both"]) and clean["report_only_values"] == 3 * n_ok
    assert clean["report_only_bits_equal"] == clean["report_only_values"]
    assert clean["snapshot_source_equal"] == n_ok

    bad = copy.deepcopy(report)
    day = [c for c in bad["spawners"] if "report" in c and meta[c["id"]]["is_days"]]
    minute = [c for c in bad["spawners"] if "report" in c and not meta[c["id"]]["is_days"] and c["report"]["SnapshotSource"]]
    ok_rows = [c for c in bad["spawners"] if "report" in c and c is not day[0] and c is not minute[0]
               and all(P.doubles(c["report"][f])[0] not in (0.0, 1.0) and math.isfinite(P.doubles(c["report"][f])[0])
                       for f in ("NewHealth", "PlayerDps", "PlayerAblityEhpMultiplier"))]
    plants = {}

    def plant(c, fields, **change):
        c["report"].update(change)
        plants[meta[c["id"]]["tag"]] = fields

    plant(ok_rows[0], (False, []), NewHealth=_bump(ok_rows[0]["report"]["NewHealth"]))
    plant(ok_rows[1], (False, []), NewDamage=_bump(ok_rows[1]["report"]["NewDamage"], -1))
    plant(ok_rows[2], (True, ["PlayerDps"]), PlayerDps=P.dhex(P.doubles(ok_rows[2]["report"]["PlayerDps"])[0] * (1 + 1e-9)))
    plant(ok_rows[3], (True, ["ArmorType"]), ArmorType=ok_rows[3]["report"]["ArmorType"] + 1)
    plant(ok_rows[4], (True, ["PlayerBlockChanceRaw"]), PlayerBlockChanceRaw=_bump(ok_rows[4]["report"]["PlayerBlockChanceRaw"]))
    plant(ok_rows[5], (True, ["DefenseFromSnapshot"]), DefenseFromSnapshot=_bump(ok_rows[5]["report"]["DefenseFromSnapshot"]))
    plant(ok_rows[6], (True, ["PlayerBlockAmountRaw"]), PlayerBlockAmountRaw=_bump(ok_rows[6]["report"]["PlayerBlockAmountRaw"]))
    plant(ok_rows[7], (True, ["PlayerAblityEhpMultiplier"]),
          PlayerAblityEhpMultiplier=P.dhex(P.doubles(ok_rows[7]["report"]["PlayerAblityEhpMultiplier"])[0] * 1.5))
    src = day[0]["report"]["SnapshotSource"]
    plant(day[0], (True, []), SnapshotSource="t:3" if src is None else None)
    s_index = int(minute[0]["report"]["SnapshotSource"][2:])
    plant(minute[0], (True, []), SnapshotSource=f"s:{s_index + 1}")
    errored = ok_rows[8]
    errored.pop("report")
    errored["error"] = "InvalidOperationException: Sequence contains no matching element"
    res = BR.compare_x4(None, db, report=bad)
    got = {m["id"]: (m["new_ok"], m["fields"]) for m in res["mismatches"] if "new_ok" in m}
    assert got == plants
    assert [m for m in res["mismatches"] if "new_ok" not in m] == \
        [{"id": meta[errored["id"]]["tag"], "csharp": errored["error"], "oracle": "ok"}]
    assert res["new_bits_equal"] == n_ok - 3 and res["snapshot_source_equal"] == n_ok - 3
    assert res["report_only_bits_equal"] == clean["report_only_bits_equal"] - 3 - 3


def test_compare_dp5_reports_each_planted_fault(oracle_report):
    report, _ = oracle_report
    only = ("DefenseFromSnapshot", "PlayerBlockChanceRaw", "PlayerBlockAmountRaw", "SnapshotSource")
    dp5 = {"spawners": [{**c, "report": {k: v for k, v in c["report"].items() if k not in only}} if "report" in c else c
                        for c in copy.deepcopy(report["spawners"])]}
    clean = BR.compare_dp5(report, dp5)
    assert clean["identical"] == clean["spawners"] == len(dp5["spawners"]) and not clean["differ"], clean["differ"][:5]
    fields = len(dp5["spawners"][0]["report"]) if "report" in dp5["spawners"][0] else None
    assert clean["fields_compared"] == fields * sum("report" in c for c in report["spawners"])
    ok = [c for c in dp5["spawners"] if "report" in c]
    ok[0]["report"]["NewDamageRaw"] = _bump(ok[0]["report"]["NewDamageRaw"])
    ok[1]["report"]["TimeChunkIndex"] = ok[1]["report"]["TimeChunkIndex"] + 1
    ok[2].pop("report")
    ok[2]["error"] = "x"
    gone = dp5["spawners"].pop(dp5["spawners"].index(ok[3]))
    res = BR.compare_dp5(report, dp5)
    assert {d["id"]: d["fields"] for d in res["differ"]} == {
        ok[0]["id"]: ["NewDamageRaw"], ok[1]["id"]: ["TimeChunkIndex"], ok[2]["id"]: ["error"]}
    assert res["only_report"] == [gone["id"]] and res["identical"] == clean["identical"] - 4


# ---------------------------------------------------------------- report integrity

def _report_dir(tmp_path, monkeypatch, report: dict, rows: int = 0):
    job = tmp_path / "job"
    job.mkdir()
    (job / BR.JSON_FILE).write_text(json.dumps(report))
    (job / BR.CSV_FILE).write_text(",".join(n for n, _ in BR.CSV_COLUMNS) + "\n")
    files = {name: {"sha256": hashlib.sha256((job / name).read_bytes()).hexdigest(), "bytes": len((job / name).read_bytes())}
             for name in (BR.JSON_FILE, BR.CSV_FILE)}
    files[BR.CSV_FILE]["rows"] = rows
    manifest = {"bake": {"schema": BR.REPORT_SCHEMA, "snapshots": report.get("snapshots"), "files": files}}
    monkeypatch.setattr(BR, "read_manifest", lambda d: manifest)
    return job, manifest


def test_read_report_checks_the_files_against_the_manifest(tmp_path, monkeypatch):
    report = {"schema": BR.REPORT_SCHEMA, "snapshots": "shipped", "spawners": []}
    job, manifest = _report_dir(tmp_path, monkeypatch, report)
    assert BR.read_report(job) == report
    raw = (job / BR.JSON_FILE).read_bytes()
    (job / BR.JSON_FILE).write_bytes(raw.replace(b"shipped", b"shippex"))  # same length, other bytes
    with pytest.raises(BR.BakeToolError, match="sha256/bytes differ"):
        BR.read_report(job)
    (job / BR.JSON_FILE).write_bytes(raw)
    manifest["bake"]["files"][BR.CSV_FILE]["rows"] = 1
    with pytest.raises(BR.BakeToolError, match="row count differs"):
        BR.read_report(job)
    manifest["bake"]["files"][BR.CSV_FILE]["rows"] = 0
    manifest["bake"]["snapshots"] = "run"
    with pytest.raises(BR.BakeToolError, match="snapshots differ"):
        BR.read_report(job)


def test_bake_context_reads_the_exact_inputs_and_checks_their_counts(db):
    from tufu_balance.model import data as MD
    sd = MD.load_simulation_data(db)
    snaps, days = sd.snapshots[:6], sd.time_chunk_snapshots[:5]
    report = {"snapshots": "run", "run_snapshots": len(snaps), "run_time_chunk_snapshots": len(days),
              "inputs": {"Snapshots": [_encode_input(s) for s in snaps],
                         "TimeChunkSnapshots": [_encode_input(s) for s in days]}}
    ctx = BR.bake_context(None, db, report)
    assert ctx.config_time_chunk_snapshots is ctx.simulation_data.time_chunk_snapshots
    for got, want in ((ctx.simulation_data.snapshots, snaps), (ctx.config_time_chunk_snapshots, days)):
        assert [_encode_input(s) for s in got] == [_encode_input(s) for s in want]
    for key in ("run_snapshots", "run_time_chunk_snapshots"):
        with pytest.raises(BR.BakeToolError, match="snapshot counts differ"):
            BR.bake_context(None, db, {**report, key: report[key] + 1})
    with pytest.raises(BR.BakeToolError, match="no inputs"):
        BR.bake_context(None, db, {**report, "inputs": None})


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
    repo = scene.parents[4]
    ok = dict(dest=scene, caller="cli", environ={}, run_tree=tmp_path / "run")
    assert BR.apply_refusal(plan_path, token, **ok) is None
    refusals = {
        "CI": (BR.apply_refusal(plan_path, token, **{**ok, "environ": {"CI": "true"}}), "CI is set"),
        "TUFU_REFRESH": (BR.apply_refusal(plan_path, token, **{**ok, "environ": {"TUFU_REFRESH": "1"}}), "TUFU_REFRESH is set"),
        "GITHUB_ACTIONS": (BR.apply_refusal(plan_path, token, **{**ok, "environ": {"GITHUB_ACTIONS": "true"}}),
                           "GITHUB_ACTIONS is set"),
        "refresh caller": (BR.apply_refusal(plan_path, token, **{**ok, "caller": "refresh"}), "caller 'refresh'"),
        "no caller": (BR.apply_refusal(plan_path, token, **{**ok, "caller": None}), "caller None"),
        "reversed token": (BR.apply_refusal(plan_path, token[::-1], **ok), "is not the plan's sha256"),
        "token differs at the end": (BR.apply_refusal(plan_path, token[:-1] + ("0" if token[-1] != "0" else "1"), **ok),
                                     "is not the plan's sha256"),
        "no token": (BR.apply_refusal(plan_path, None, **ok), "is not the plan's sha256"),
        "dest in the run worktree": (BR.apply_refusal(plan_path, token, **{**ok, "run_tree": repo}), "inside the run worktree"),
    }
    original = scene.read_bytes()
    scene.write_bytes(original + b"# edit\n")
    refusals["modified scene"] = (BR.apply_refusal(plan_path, token, **ok), "not clean in git")
    _git(repo, "add", "-A")
    scene.write_bytes(original)  # staged change, worktree bytes equal to the planned file
    assert hashlib.sha256(scene.read_bytes()).hexdigest() == json.loads(plan_path.read_text())["scene_sha256"]
    refusals["staged change, planned bytes"] = (BR.apply_refusal(plan_path, token, **ok), "not clean in git")
    _git(repo, "reset", "-q")
    assert BR.apply_refusal(plan_path, token, **ok) is None
    untracked = scene.with_name("Other.unity")
    untracked.write_bytes(original)
    refusals["untracked scene"] = (BR.apply_refusal(plan_path, token, **{**ok, "dest": untracked}), "not clean in git")
    untracked.unlink()
    ignored = scene.with_name("Ignored.unity")
    ignored.write_bytes(original)
    (repo / ".git" / "info" / "exclude").write_text("Ignored.unity\n")
    assert not _git(repo, "status", "--porcelain", "-uall").strip()
    refusals["ignored untracked scene"] = (BR.apply_refusal(plan_path, token, **{**ok, "dest": ignored}), "not tracked in git")
    pointer = tmp_path / "pointer.unity"
    pointer.write_bytes(BR.LFS_POINTER + b"v1\noid sha256:00\nsize 1\n")
    refusals["lfs pointer"] = (BR.apply_refusal(plan_path, token, **{**ok, "dest": pointer}), "is an LFS pointer")
    refusals["missing dest"] = (BR.apply_refusal(plan_path, token, **{**ok, "dest": tmp_path / "none.unity"}), "does not exist")
    for name, change, why in (("other scene file", {"scene_sha256": "0" * 64}, "not the planned scene file"),
                              ("writes nothing", {"counts": {"write": 0}}, "the plan writes nothing"),
                              ("other scene", {"scene": "Assets/Scenes/Boot.unity"}, "not a Main.unity plan"),
                              ("no code_rev", {"code_rev": ""}, "not a Main.unity plan")):
        doc = {**json.loads(plan_path.read_text()), **change}
        other = tmp_path / f"{name}.json"
        other.write_text(json.dumps(doc))
        refusals[name] = (BR.apply_refusal(other, hashlib.sha256(other.read_bytes()).hexdigest(), **ok), why)
    wrong = {name: r for name, (r, why) in refusals.items() if not (r and r.startswith("refused: ") and why in r)}
    assert not wrong, wrong


def test_apply_refuses_before_any_editor_starts(scene_repo, monkeypatch, tmp_path):
    scene, plan_path, token = scene_repo

    def no_editor(*a, **kw):
        raise AssertionError("an editor was started")

    monkeypatch.setattr(BR, "_entry", no_editor)
    run_tree = tmp_path / "run"
    with pytest.raises(BR.BakeToolError, match="refused: caller None"):
        BR.apply(plan_path, token, dest=scene, run_tree=run_tree)  # no default caller
    with pytest.raises(BR.BakeToolError, match="refused: token"):
        BR.apply(plan_path, "0" * 64, dest=scene, caller="cli", run_tree=run_tree)
    monkeypatch.setenv("CI", "1")
    with pytest.raises(BR.BakeToolError, match="refused: CI"):
        BR.apply(plan_path, token, dest=scene, caller="cli", run_tree=run_tree)
    monkeypatch.delenv("CI")
    with pytest.raises(BR.BakeToolError, match="caller 'refresh'"):
        BR.apply(plan_path, token, dest=scene, caller="refresh", run_tree=run_tree)
    with pytest.raises(BR.BakeToolError, match="inside the run worktree"):
        BR.apply(plan_path, token, dest=scene, caller="cli", run_tree=scene.parents[4])
    # This worktree's Main.unity (an LFS pointer in unit worktrees, else another file than the plan's).
    with pytest.raises(BR.BakeToolError, match="refused"):
        BR.apply(plan_path, token, caller="cli", run_tree=run_tree)


def test_cli_apply_passes_the_cli_caller(scene_repo, monkeypatch, tmp_path):
    scene, plan_path, token = scene_repo
    seen = {}

    def fake_apply(plan_file, tok, **kw):
        seen.update(kw, plan_file=plan_file, token=tok)
        raise BR.BakeToolError("refused: stop here")

    monkeypatch.setattr(BR, "apply", fake_apply)
    from tufu_balance.unity.__main__ import main
    assert main(["bake", "--apply", "--token", token, "--plan-file", str(plan_path), "--dest", str(scene)]) != 0
    assert seen["caller"] == BR.CALLER_CLI and str(seen["plan_file"]) == str(plan_path)


# ---------------------------------------------------------------- the apply flow with a stand-in editor

FAKE_EDITOR = """#!{python}
import hashlib, json, os, pathlib, sys
a = sys.argv[1:]
arg = lambda n: a[a.index(n) + 1] if n in a else None
project, out = pathlib.Path(arg("-projectPath")), pathlib.Path(arg("-tufuSimBakeOut"))
out.mkdir(parents=True, exist_ok=True)
scene = project / "Assets/Scenes/Main.unity"
m = {{"schema": "tufu.sim.bake-apply/1", "entered_play": False, "caller": arg("-tufuSimBakeCaller"),
      "token": arg("-tufuSimBakeToken"), "plan": arg("-tufuSimBakePlan")}}
if m["caller"] != "cli":
    m.update(ok=False, exit_code=2, error="refused: caller")
else:
    scene.write_bytes(scene.read_bytes() + b"--- written by the stand-in editor\\n")
    if os.environ.get("FAKE_BAKE_EXTRA"):
        (project / "Assets/Scenes/Main").mkdir(exist_ok=True)
        (project / "Assets/Scenes/Main/LightingData.asset").write_text("x")
        (project / "Assets/Other.asset").write_text("changed")
    m.update(ok=True, exit_code=0, scene_sha256_after=hashlib.sha256(scene.read_bytes()).hexdigest())
(out / "bake_apply_manifest.json").write_text(json.dumps(m))
sys.exit(m["exit_code"])
"""


@pytest.fixture
def apply_setup(tmp_path, monkeypatch):
    """A run worktree (git repo with Main.unity and Library/), a destination clone, a plan and a stand-in editor."""
    run_tree = tmp_path / "run"
    project = run_tree / "Recovery/StudyProject"
    (project / "Assets/Scenes").mkdir(parents=True)
    (project / "Library").mkdir()
    (project / "Assets/Scenes/Main.unity").write_text("%YAML 1.1\n--- !u!1 &1\nGameObject: {}\n")
    (project / "Assets/Other.asset").write_text("other\n")
    (run_tree / ".gitignore").write_text("/Balance/out/\n/Recovery/StudyProject/Library/\n")
    cfg = ["-c", "user.name=t", "-c", "user.email=t@t", "-c", "commit.gpgsign=false"]
    _git(tmp_path, "init", "-q", str(run_tree))
    _git(run_tree, "add", "-A")
    _git(run_tree, *cfg, "commit", "-qm", "scene")
    _git(tmp_path, "clone", "-q", str(run_tree), str(tmp_path / "dest"))
    dest = tmp_path / "dest/Recovery/StudyProject/Assets/Scenes/Main.unity"
    plan = {"schema": BR.PLAN_SCHEMA, "scene": BR.MAIN_SCENE, "scene_sha256": hashlib.sha256(dest.read_bytes()).hexdigest(),
            "code_rev": _git(run_tree, "rev-parse", "HEAD").strip(), "counts": {"write": 1}, "rows": []}
    plan_path = tmp_path / "bake_plan.json"
    plan_path.write_text(json.dumps(plan, indent=1) + "\n")
    editor = tmp_path / "fake_unity"
    editor.write_text(FAKE_EDITOR.format(python=sys.executable))
    editor.chmod(0o755)
    monkeypatch.setattr(launcher, "check_dlls", lambda project: {})
    for name in BR.REFUSED_ENV:
        monkeypatch.delenv(name, raising=False)
    opts = launcher.Options(unity=editor, lock_wait_s=10.0, worker_exit_wait_s=2.0)
    paths = launcher.ProtectedPaths(save_dir=tmp_path / "save", plist=tmp_path / "prefs.plist", domain=None)
    kw = dict(caller="cli", out_dir=tmp_path / "out", opts=opts, worktree=tmp_path / "dest", run_tree=run_tree,
              paths=paths, timeout_s=60.0, log=lambda *a: None)
    return run_tree, dest, plan_path, hashlib.sha256(plan_path.read_bytes()).hexdigest(), kw


def test_apply_flow_copies_the_written_scene_and_restores_the_run_worktree(apply_setup):
    run_tree, dest, plan_path, token, kw = apply_setup
    before = dest.read_bytes()
    result = BR.apply(plan_path, token, dest=dest, **kw)
    assert result.exit_code == 0, result.report
    written = dest.read_bytes()
    assert written == before + b"--- written by the stand-in editor\n"
    assert result.tool["caller"] == "cli" and result.tool["token"] == token
    assert result.report["after"]["sha256"] == hashlib.sha256(written).hexdigest() == result.tool["scene_sha256_after"]
    assert result.report["run_worktree_restored"] and not _git(run_tree, "status", "--porcelain", "-uall").strip()
    assert (run_tree / "Recovery/StudyProject/Assets/Scenes/Main.unity").read_bytes() == before
    assert result.report["run_worktree_changes_kept"] == [] and not result.report["guard_problems"]


def test_apply_flow_refuses_other_changes_and_still_restores(apply_setup, monkeypatch):
    """An editor that changes more than Main.unity: nothing reaches the destination, the run worktree is restored
    and the extra files are kept as evidence."""
    run_tree, dest, plan_path, token, kw = apply_setup
    monkeypatch.setenv("FAKE_BAKE_EXTRA", "1")
    before = dest.read_bytes()
    result = BR.apply(plan_path, token, dest=dest, **kw)
    assert result.exit_code == launcher.EXIT_GUARD
    assert any(p.startswith("run worktree changed") for p in result.report["guard_problems"])
    assert dest.read_bytes() == before and "after" not in result.report
    assert result.report["run_worktree_restored"] and not _git(run_tree, "status", "--porcelain", "-uall").strip()
    kept = result.report["run_worktree_changes_kept"]
    assert kept == ["Recovery/StudyProject/Assets/Other.asset", "Recovery/StudyProject/Assets/Scenes/Main/LightingData.asset"]
    assert (kw["out_dir"] / "run_worktree_changes" / kept[0]).read_text() == "changed"


def test_cli_apply_needs_a_plan(capsys):
    from tufu_balance.unity.__main__ import main
    assert main(["bake", "--apply", "--token", "0" * 64]) == 5
    assert "needs --plan-file or JOB_DIR" in capsys.readouterr().err
