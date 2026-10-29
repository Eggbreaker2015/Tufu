"""C9: the config patch applied in Play against Python's PatchReport (design 6.2, 7.3 C9).

One launch runs every case of ``tests/patch_cases`` as an ``empty`` job: the positive cases with
``PatchReport.expanded`` (C# runs no generator), the error cases with their raw overrides and
``patch_on_error = record``, a patch with nodes C# does not support, and one short ``free`` run with a patch
around a running simulation. A second launch checks that a patch error fails the job with exit 5.
"""

from __future__ import annotations

import json
import struct
from pathlib import Path

import pytest
import yaml

from tufu_balance.model import config as C
from tufu_balance.model import patch as P
from tufu_balance.model import pipeline as PL
from tufu_balance.unity import engine, launcher, patcher, request as req, schema

pytestmark = pytest.mark.unity

CASES = Path(__file__).resolve().parents[1] / "patch_cases"
NULL = engine.engine_probe(null_policy=True)
UNSUPPORTED = [
    {"node": "SpawnerRelativeOptimumTimeTool.Islands[].IsDays", "where": {"LocationNumber": 1}, "set": True},
    {"node": "CombatBalanceConfig.IslandTargetDays", "index": 0, "add": 1},
    {"node": "EnemySpawnerConfig.StatRewards[].Value", "where": {}, "mul": 2},
]
FREE_CASE = "05_where_mul_spawners"
AB_CASES = ("12_ab_remove_override_key", "13_ab_remove_legacy_key")


def _cases(sub: str = "") -> list[Path]:
    return sorted((CASES / sub).glob("*.yaml"))


def _expected(path: Path) -> dict:
    return json.loads(path.with_name(path.name.replace(".yaml", ".expected.json")).read_text(encoding="utf-8"))


@pytest.fixture(scope="module")
def cfg(db_path):
    return C.load_game_config(db_path)


@pytest.fixture(scope="module")
def spawner_map(db_path):
    return patcher.spawner_map(db_path)


@pytest.fixture(scope="module")
def reports(cfg):
    return {p.stem: P.apply(cfg, P.load_patch(p)) for p in _cases()}


@pytest.fixture(scope="module")
def parity_run(unity_run, reports, spawner_map):
    jobs = [patcher.patch_job(f"case-{stem}", report.expanded, spawner_map=spawner_map)
            for stem, (_, report) in reports.items()]
    for path in _cases("errors"):
        raw = yaml.safe_load(path.read_text(encoding="utf-8"))["overrides"]
        jobs.append(patcher.patch_job(f"err-{path.stem}", raw, spawner_map=spawner_map, on_error="record"))
    jobs.append(patcher.patch_job("unsupported", UNSUPPORTED, spawner_map=spawner_map, on_error="record"))
    free = engine.engine_job("free-patched", day=2, max_wall_seconds=300.0, probe=NULL)
    jobs.append(patcher.with_patch(free, reports[FREE_CASE][1].expanded, spawner_map=spawner_map))
    result = unity_run(req.make_request(jobs))
    assert result.exit_code == 0, result.describe()
    return result


@pytest.fixture(scope="module")
def fail_run(unity_run, spawner_map):
    raw = yaml.safe_load((CASES / "errors" / "two_ops.yaml").read_text(encoding="utf-8"))["overrides"]
    return unity_run(req.make_request([patcher.patch_job("patch-fails", raw, spawner_map=spawner_map)]))


def _applied(result, job_id):
    return patcher.load_applied(result.job_dir(job_id))


def _bits(x: float) -> bytes:
    return struct.pack("<d", x)


# ---------------------------------------------------------------- leaf parity

@pytest.mark.parametrize("stem", [p.stem for p in _cases()])
def test_case_leaves_identical_to_the_patch_report(parity_run, reports, stem):
    _, report = reports[stem]
    applied = _applied(parity_run, f"case-{stem}")
    assert applied["error"] is None, applied["error"]
    result = patcher.compare(report, applied)
    assert result["identical"], patcher.describe_mismatch(result)
    assert result["python"] == len(report.csharp_leaves()) and result["csharp"] == result["python"]
    assert applied["verified_leaves"] >= result["csharp"]
    assert applied["overrides"] == len(report.expanded) == len(applied["ops"])
    assert applied["spawners"]["unmapped_selected"] == []


@pytest.mark.parametrize("stem", [p.stem for p in _cases()])
def test_case_ops_match_rows_and_leaves(parity_run, reports, stem):
    """Every override selects the rows Python selects; generated sets touch one row each; a passthrough node
    selects as many rows as Python's check on the dictionary's source table."""
    _, report = reports[stem]
    ops = _applied(parity_run, f"case-{stem}")["ops"]
    passthrough = {o.node for o in report.passthrough}
    written = [o for o in report.ops if o.override is not None]
    hits = iter(w.detail["rows"] for w in report.warnings if w.code == "not_python_patchable")
    n_overrides = len(report.expanded) - sum(len(o.leaves) for o in report.ops if o.override is None
                                             if P.csharp_node(o.node))
    plain = iter(written)
    for op in ops[:n_overrides]:
        if op["node"] in passthrough:
            assert op["rows"] == next(hits), op
            continue
        py = next(plain)
        assert (op["node"], op["rows"], op["leaves"]) == (py.node, py.rows, len(py.leaves)), op
    assert all(op["op"] == "set" and op["rows"] == 1 for op in ops[n_overrides:])


def test_passthrough_leaves_are_listed_apart(parity_run, reports):
    _, report = reports["17_passthrough_unmodeled_node"]
    result = patcher.compare(report, _applied(parity_run, "case-17_passthrough_unmodeled_node"))
    leaves = result["passthrough_leaves"]
    rows = next(w.detail["rows"] for w in report.warnings if w.code == "not_python_patchable")
    assert leaves and len(leaves) <= rows
    assert {x["node"] for x in leaves} == {"EnemySpawnerConfig.AggroRange"}
    assert all(isinstance(x["old"], float) and x["new"] == 5.0 and set(x["row"]) == {"scene", "file_id"} for x in leaves)


def test_float32_where_and_enum_names_select_the_same_rows(parity_run, reports):
    """Case 22: a binary32 where value and an enum member name on a field without a __name column."""
    _, report = reports["22_where_float32"]
    applied = _applied(parity_run, "case-22_where_float32")
    assert applied["ops"][0]["rows"] == report.ops[0].rows == len(_expected(CASES / "22_where_float32.yaml")["leaves"])


# ---------------------------------------------------------------- errors

@pytest.mark.parametrize("path", _cases("errors"), ids=lambda p: p.stem)
def test_error_case_rejected_with_the_same_code(parity_run, path):
    applied = _applied(parity_run, f"err-{path.stem}")
    assert applied["error"] is not None, applied
    assert applied["error"]["code"] == _expected(path)["error"], applied["error"]
    assert applied["leaves"] == []
    assert applied["restore"]["when"] == "error" and applied["restore"]["ok"], applied["restore"]


def test_unsupported_nodes_are_listed(parity_run):
    error = _applied(parity_run, "unsupported")["error"]
    assert error["code"] == "unknown_node"
    assert error["nodes"] == ["SpawnerRelativeOptimumTimeTool.Islands[].IsDays", "EnemySpawnerConfig.StatRewards[].Value"]
    assert all(n in error["message"] for n in error["nodes"]) and error["override"] == 0
    m = parity_run.manifest("unsupported")
    assert m["exit_code"] == 0 and m["patch"]["error"] == "unknown_node"


def test_patch_error_fails_the_job_with_exit_5(fail_run):
    m = fail_run.manifest("patch-fails")
    assert m["exit_code"] == launcher.EXIT_BAD_REQUEST and m["error"].startswith("patch: [bad_op]"), m["error"]
    assert m["patch"]["error"] == "bad_op" and "stop_reason" not in m
    applied = _applied(fail_run, "patch-fails")
    assert applied["error"]["code"] == "bad_op" and applied["restore"]["ok"]
    if fail_run.exit_code != launcher.EXIT_BAD_REQUEST:
        # A failed run while the user's editor rewrites the real save is reported as a guard failure (Runbook 13).
        problems = fail_run.report.get("guard_problems") or []
        assert fail_run.exit_code == launcher.EXIT_GUARD and fail_run.report.get("main_checkout_editors"), fail_run.describe()
        assert problems and all(p.startswith("real ") for p in problems), fail_run.describe()


# ---------------------------------------------------------------- restore

def _patched_jobs(result):
    return [j["job_id"] for j in result.request["jobs"] if j.get("patch") is not None]


def test_config_restored_after_every_job(parity_run):
    """The digest of every patched object after the restore equals the one before the patch, the asset guard
    passes, and the guard sees no other changed asset than a job that patched nothing."""
    baseline = json.loads(parity_run.file("err-unknown_node", "asset_guard.json"))["after_job"]
    assert baseline["ok"]
    for job_id in _patched_jobs(parity_run):
        applied = _applied(parity_run, job_id)
        restore = applied["restore"]
        assert restore and restore["ok"] and not restore["errors"], (job_id, restore)
        assert restore["when"] == ("error" if applied["error"] else "play_mode_exit"), job_id
        for owner in restore["owners"]:
            assert owner["after"] == owner["before"], (job_id, owner)
        guard = json.loads(parity_run.file(job_id, "asset_guard.json"))["after_job"]
        assert guard["ok"], (job_id, guard)
        if job_id != "free-patched":
            assert guard["changed_in_memory"] == baseline["changed_in_memory"], (job_id, guard)


def test_patched_objects_were_changed_while_the_job_ran(parity_run, reports):
    for stem, (_, report) in reports.items():
        applied = _applied(parity_run, f"case-{stem}")
        changed = [o for o in applied["restore"]["owners"] if o["patched"] != o["before"]]
        assert bool(changed) == bool(applied["leaves"]), stem


def test_free_run_with_a_patch(parity_run, reports):
    m = parity_run.manifest("free-patched")
    assert m["exit_code"] == 0 and m["stop_reason"] == "simulation_stopped", m.get("error")
    assert m["patch"] == {"file": "applied_patch.json", "overrides": len(reports[FREE_CASE][1].expanded),
                          "leaves": len(reports[FREE_CASE][1].csharp_leaves()), "error": None, "ab_roundtrip": False}
    assert patcher.compare(reports[FREE_CASE][1], _applied(parity_run, "free-patched"))["identical"]
    schema.validate_manifest(m)


# ---------------------------------------------------------------- AB data round trip

def _value_by_location(effective: dict, py, is_int: bool):
    overrides = {int(k): (int(v) if is_int else _bits(float(v))) for k, v in effective["overridesByLocation"].items()}
    want = {int(k): (int(v) if is_int else _bits(float(v))) for k, v in py.overrides.items()}
    default = int(effective["defaultValue"]) if is_int else _bits(float(effective["defaultValue"]))
    return (default, overrides) == ((int(py.default) if is_int else _bits(float(py.default))), want)


@pytest.mark.parametrize("stem", AB_CASES)
def test_ab_round_trip_gives_the_class_default_for_removed_keys(parity_run, reports, stem):
    """The patched asset goes through BuildDataJson -> ApplyRemote: the effective spawner cooldowns equal
    model.ab on the patched config (removed keys fall back to the class default, not to defaultValue)."""
    out, _ = reports[stem]
    roundtrip = _applied(parity_run, f"case-{stem}")["ab_roundtrip"]
    test = roundtrip["tests"]["spawnerCooldowns"]
    assert test["version_after"] > test["version_before"]
    eff, py = test["effective"], PL.ab_config(out).spawner_cooldowns("A")
    for name, group in (("nonElite", py.non_elite), ("elite", py.elite)):
        assert _value_by_location(eff[name]["cooldownMinutes"], group.cooldown_minutes, False), (name, eff[name])
        assert _value_by_location(eff[name]["charges"], group.charges, True), (name, eff[name])
    if stem.startswith("12"):
        assert float(eff["nonElite"]["cooldownMinutes"]["overridesByLocation"]["13"]) == 28.0
    else:
        assert float(eff["elite"]["cooldownMinutes"]["overridesByLocation"]["4"]) == 150.0
