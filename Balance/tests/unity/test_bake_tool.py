"""C# bake report, X4 and the guarded write-back in Unity (design 1.4.3, 3.5 X4, 7.3 C8).

* ``bake-run``: an engine run (null policy, flat damage/defence grants so DefenseData is not 0) to day 405 ends with
  the original ``BuildSpawnerBakeReport`` over every scene spawner on this run's snapshots; ``model.bake`` on the
  same snapshots gives the same NewHealth/NewDamage bit for bit (X4).
* ``bake-shipped``: an empty job that runs DP5 in edit mode before Play and the bake export on the shipped data in
  Play; the two reports are identical field for field, and X4 holds on the shipped data too.
* ``bake --plan`` on bake-run's report touches no asset and agrees with an independent recomputation from the
  extracted scene; the tool's self-test exercises the write-back on an unsaved ``NewScene`` scene. The real
  Main.unity is never written.
"""

from __future__ import annotations

import hashlib
import json
import sqlite3

import pytest

from tufu_balance.golden import check, exact, same_set
from tufu_balance.unity import bake_report as BR, engine, probes, request as req, simio

pytestmark = pytest.mark.unity

NULL_FLAT = engine.engine_probe(null_policy=True, grant_flat={6: 10.0, 8: 10.0, 10: 10.0, 12: 10.0,
                                                              14: 10.0, 16: 10.0, 18: 10.0, 20: 10.0})
RUN_DAY = 405


def _jobs():
    run = BR.bake_outputs(engine.engine_job("bake-run", day=RUN_DAY, max_wall_seconds=600.0, probe=NULL_FLAT))
    shipped = BR.bake_outputs(probes.probe_job("bake-shipped", edit=["DP5"], runtime=[]), "shipped")
    return [run, shipped]


def _ok(result):
    assert result.exit_code == 0, result.describe()
    return result


@pytest.fixture(scope="module")
def bake_run(unity_run):
    return _ok(unity_run(req.make_request(_jobs())))


@pytest.fixture(scope="module")
def plan_result(bake_run, unity_opts):
    result = BR.plan(bake_run.job_dir("bake-run"), opts=unity_opts, reuse=True)
    assert result.exit_code == 0, json.dumps(result.report, indent=1, default=str)[:4000]
    return result


@pytest.fixture(scope="module")
def selftest_result(unity_rev, unity_opts):
    result = BR.selftest(rev=unity_rev, opts=unity_opts, reuse=True)
    assert result.exit_code == 0, json.dumps(result.tool or result.report, indent=1, default=str)[:4000]
    return result


def _short(res: dict) -> dict:
    return {k: (v[:10] if isinstance(v, list) else v) for k, v in res.items()}


def test_run_report_is_the_original_bake_on_this_runs_snapshots(bake_run, db):
    job = bake_run.job_dir("bake-run")
    block = bake_run.manifest("bake-run")["bake"]
    assert block["snapshots"] == "run" and block["swap"]["restored"] is True
    assert block["run_snapshots"] > 0 and block["run_time_chunk_snapshots"] == RUN_DAY + 1
    report = BR.read_report(job)
    res = BR.compare_x4(job, db, report=report)
    assert not res["mismatches"] and not res["unknown"] and not res["missing"], _short(res)
    assert res["new_bits_equal"] == res["spawners"] - len(res["errors_both"])
    assert res["snapshot_source_equal"] == res["spawners"] - len(res["errors_both"])
    assert res["registered"] == block["registered"] == 716 and block["registered_not_in_scenes"] == 0
    # The day list holds every day, so a day-mode spawner reads a stored day only when its bake day is not an
    # integer (t = 1 interpolation otherwise, as on the shipped list: DP4).
    shipped = BR.read_report(bake_run.job_dir("bake-shipped"))
    differ = sum(a["report"]["NewHealth"] != b["report"]["NewHealth"] for a, b in zip(report["spawners"], shipped["spawners"])
                 if "report" in a and "report" in b)
    check("bake_cs", {
        "run.x4.spawners": exact(res["spawners"], source="X4", note="scene EnemySpawner components, inactive included"),
        "run.x4.new_bits_equal": exact(res["new_bits_equal"], source="X4", better="higher"),
        "run.x4.mismatches": same_set([m["id"] for m in res["mismatches"]], source="X4", better="subset"),
        "run.x4.errors_both": same_set(res["errors_both"], source="X4", better="subset"),
        "run.x4.non_finite": same_set(res["non_finite"], source="X4", note="BossTower: Count 0"),
        "run.x4.intermediate_bits_equal": exact(res["intermediate_bits_equal"], source="X4", better="higher"),
        "run.x4.intermediate_values": exact(res["intermediate_values"], source="X4"),
        "run.x4.new_health_raw_explained_by_mono": exact(res["new_health_raw_explained_by_mono"], source="X4",
                                                         note="(double)(1f - usedResist): DifferentialProbes 3"),
        "run.x4.minute_n": exact(res["minute_n"], source="X4"),
        "run.x4.new_values_sha256": exact(res["new_values_sha256"], source="X4",
                                          note="null policy, flat grants, day 405; changes with the engine"),
        "run.new_health_differs_from_shipped": exact(differ, source="X4", note="the bake read this run's snapshots"),
    })
    assert differ > 0


def test_exported_snapshot_text_against_the_bake_inputs(bake_run):
    """C1's simulation_data.json/timechunk_full.json against the exact bits the bake read (a diagnostic: the
    editor's Mono checks its "R" text with its own parser, see BakeTool.md)."""
    job = bake_run.job_dir("bake-run")
    res = BR.export_text_check(job, BR.read_report(job))
    check("bake_cs", {
        "run.export_text.values": exact(res["values"], source="C1"),
        "run.export_text.differ": exact(res["differ"], source="C1", better="lower"),
        "run.export_text.ulps": exact(res["ulps"], source="C1"),
    })


def test_shipped_report_ingests_as_sim_bake_report(bake_run):
    job = bake_run.job_dir("bake-shipped")
    db_path = simio.ingest(job, job / "sim_bake_test.sqlite")
    try:
        con = sqlite3.connect(f"file:{db_path}?mode=ro", uri=True)
        rows = con.execute("SELECT id, new_health, new_damage, error FROM sim_bake_report ORDER BY \"index\"").fetchall()
        con.close()
    finally:
        db_path.unlink()
    report = BR.read_report(job)
    assert len(rows) == len(report["spawners"])
    for (rid, nh, nd, err), c in zip(rows, report["spawners"]):
        assert rid == c["id"]
        if "report" in c:
            assert probes._same(float(nh), probes.doubles(c["report"]["NewHealth"])[0])
            assert probes._same(float(nd), probes.doubles(c["report"]["NewDamage"])[0])
        else:
            assert err == c["error"]


def test_shipped_report_equals_dp5_and_the_oracle(bake_run, db):
    job = bake_run.job_dir("bake-shipped")
    report = BR.read_report(job)
    dp5 = json.loads((job / probes.EDIT_FILE).read_text())["dp5"]
    cmp = BR.compare_dp5(report, dp5)
    res = BR.compare_x4(job, db, report=report)
    assert not res["mismatches"] and res["new_bits_equal"] == res["spawners"], _short(res)
    check("bake_cs", {
        "shipped.dp5.identical": exact(cmp["identical"], source="DP5", better="higher",
                                       note="Play-mode export on the shipped data vs edit-mode DP5, every field"),
        "shipped.dp5.differ": same_set([d["id"] for d in cmp["differ"]], source="DP5", better="subset"),
        "shipped.dp5.fields_compared": exact(cmp["fields_compared"], source="DP5"),
        "shipped.x4.new_bits_equal": exact(res["new_bits_equal"], source="X4", better="higher"),
        "shipped.x4.hp_exact_minute": same_set(res["hp_exact_minute"], source="G13", better="superset",
                                               note="G13 anchor on the C# report: 79 minute-mode spawners"),
        "shipped.x4.non_finite": same_set(res["non_finite"], source="DP5", note="BossTower"),
    })
    assert cmp["identical"] == cmp["spawners"] == len(dp5["spawners"]), cmp["differ"][:10]
    assert len(res["hp_exact_minute"]) == 79


def test_plan_touches_nothing_and_matches_the_scene(plan_result, bake_run, db):
    tool, launch = plan_result.tool, plan_result.report
    assert tool["entered_play"] is False and tool["scene_dirty"] is False
    assert tool["scene_sha256_before"] == tool["scene_sha256_after"] == launch["scene_sha256_before"] == launch["scene_sha256_after"]
    assert not launch["guard_problems"] and launch["run_worktree_restored"]
    raw = plan_result.plan.read_bytes()
    assert plan_result.token == hashlib.sha256(raw).hexdigest() == tool["plan_sha256"]
    plan, _ = BR.read_plan(plan_result.plan)
    report = BR.read_report(bake_run.job_dir("bake-run"))
    assert plan["code_rev"] == bake_run.manifest("bake-run")["code_rev"] and plan["scene"] == BR.MAIN_SCENE
    assert plan["report"]["sha256"] == hashlib.sha256((bake_run.job_dir("bake-run") / BR.JSON_FILE).read_bytes()).hexdigest()
    res = BR.check_plan(plan, report, db)
    assert not res["differ"] and not res["unknown"] and all(res["counts_equal"].values()), res
    assert not plan["unmatched_report_rows"]
    counts = plan["counts"]
    check("bake_cs", {
        "plan.rows": exact(counts["spawners"], source="C8"),
        "plan.write": exact(counts["write"], source="C8"),
        "plan.unchanged": exact(counts["unchanged"], source="C8"),
        "plan.skip": exact(counts["skip"], source="C8"),
        **{f"plan.reason.{k}": exact(v, source="C8") for k, v in counts["by_reason"].items()},
    })
    assert counts["by_reason"]["event"] == 6 and counts["by_reason"]["vip"] == 10
    assert counts["by_reason"]["boss_tower"] == counts["by_reason"]["dummy"] == 1


def test_tool_self_test_on_an_unsaved_scene(selftest_result):
    tool = selftest_result.tool
    failed = [k for k, v in tool["checks"].items() if not (v if isinstance(v, bool) else v["ok"])]
    assert tool["ok"] is True and not failed, failed
    assert tool["main_scene_sha256_before"] == selftest_result.report["scene_sha256_after"]
    assert not selftest_result.report["guard_problems"]
    check("bake_cs", {
        "selftest.checks": same_set(sorted(tool["checks"]), source="C8", better="superset"),
        "selftest.failed": same_set(failed, source="C8", better="subset"),
        "selftest.plan_counts": exact(tool["plan_counts"], source="C8"),
    })
