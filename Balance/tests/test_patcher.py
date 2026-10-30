"""C9 without Unity: the patch request fields, the spawner identity side file, the leaf comparison and the
patch semantics C9 needed from model.patch (typed where, non-finite ints, enum values)."""

from __future__ import annotations

import json
import math
from dataclasses import replace

import pytest

from tufu_balance.model import config as C
from tufu_balance.model import patch as P
from tufu_balance.unity import patcher, request as req
from tufu_balance.unity.spawner_map import load_identity_map

ELITE = {"go_name": "Spawner_elite_HP"}
TOWER = {"Key": "BossTower_c296e152609ae5849bb7d52b03df895a"}


@pytest.fixture(scope="module")
def cfg(db_path):
    return C.load_game_config(db_path)


def _apply(cfg, *overrides):
    return P.apply(cfg, P.parse_patch({"name": "t", "overrides": list(overrides)}))


def _code(cfg, *overrides):
    with pytest.raises(P.PatchError) as e:
        _apply(cfg, *overrides)
    return e.value.code


# ---------------------------------------------------------------- request fields

def test_patch_fields_validate():
    job = patcher.patch_job("p", [{"node": "CombatBalanceConfig.IslandTargetDays", "index": 0, "add": 1}],
                            spawner_map={"path": "/x/map.json", "sha256": "a" * 64}, on_error="record")
    req.validate(req.make_request([job], out_dir="/tmp/x"))
    assert job["mode"] == "empty" and job["patch_on_error"] == "record"
    bad = [
        ({**job, "patch_on_error": "ignore"}, "patch_on_error"),
        ({**req.make_job("f"), "patch": [], "patch_on_error": "record"}, "needs mode empty"),
        ({**job, "patch_spawner_map": {"path": "/x", "sha256": "A" * 64}}, "sha256"),
        ({**job, "patch_spawner_map": {"path": "/x"}}, "sha256"),
        ({**job, "patch": [1]}, "list of objects"),
        ({**job, "patch": {"node": "x"}}, "must be list"),
        ({k: v for k, v in job.items() if k != "patch"}, "need a patch"),
    ]
    for j, why in bad:
        with pytest.raises(req.RequestError, match=why):
            req.validate(req.make_request([j], out_dir="/tmp/x"))


def test_jobs_without_a_patch_keep_their_shape():
    job = req.make_job("plain")
    assert not {"patch", "patch_spawner_map", "patch_on_error"} & set(job)
    req.validate(req.make_request([job], out_dir="/tmp/x"))


def test_spawner_map_is_content_addressed(db, db_path, tmp_path):
    a = patcher.spawner_map(db_path, tmp_path)
    b = patcher.spawner_map(db_path, tmp_path)
    assert a == b and a["path"].endswith(f"spawner_map.{a['sha256'][:16]}.json")
    data = json.loads(open(a["path"], encoding="utf-8").read())
    identity = load_identity_map(db)
    assert data["schema"] == patcher.MAP_SCHEMA and len(data["spawners"]) == len(identity) == 717
    runtime = {f"{e['path']}|{e['key']}" for e in data["spawners"]}
    assert len(runtime) == 717
    e = identity.entries[0]
    assert {"scene": e.scene, "file_id": e.file_id, "path": e.path, "key": e.key} in data["spawners"]


# ---------------------------------------------------------------- leaf comparison

def test_same_value_is_strict():
    assert patcher.same_value(1.0, 1.0) and patcher.same_value(math.nan, math.nan)
    assert not patcher.same_value(0.0, -0.0) and not patcher.same_value(1, 1.0) and not patcher.same_value(True, 1)
    assert not patcher.same_value(1.0, math.nextafter(1.0, 2.0)) and patcher.same_value(None, None)


def test_compare_finds_every_kind_of_difference(cfg):
    _, report = P.apply(cfg, P.load_patch(patcher.Path(__file__).parent / "patch_cases" / "17_passthrough_unmodeled_node.yaml"))
    _, report2 = P.apply(cfg, P.load_patch(patcher.Path(__file__).parent / "patch_cases" / "03_mul_array_slice.yaml"))
    leaves = [x.to_json() for x in report2.csharp_leaves()]
    assert len(leaves) == 3
    assert patcher.compare(report2, {"leaves": leaves})["identical"]
    nudged = [dict(leaves[0], new=math.nextafter(leaves[0]["new"], 0.0))] + leaves[1:]
    r = patcher.compare(report2, {"leaves": nudged})
    assert not r["identical"] and len(r["different"]) == 1
    r = patcher.compare(report2, {"leaves": leaves[1:] + [dict(leaves[0], index=50)]})
    assert len(r["missing"]) == 1 and len(r["extra"]) == 1
    aggro = {"node": "EnemySpawnerConfig.AggroRange", "row": {"scene": "s", "file_id": 1}, "index": None, "old": 1.0, "new": 5.0}
    r = patcher.compare(report, {"leaves": [aggro]})
    assert r["identical"] and r["passthrough_leaves"] == [aggro]


# ---------------------------------------------------------------- semantics shared with C9

def test_where_compares_float_fields_as_binary32(cfg):
    stored = [r for r in cfg.tables[C.SPAWNERS].rows if r["OptimumTime"] == 4.4]
    assert stored
    _, report = _apply(cfg, {"node": "EnemySpawnerConfig.AverageFightTime", "where": {"OptimumTime": 4.400000095367432},
                             "mul": 2})
    assert report.ops[0].rows == len(stored)
    _, report = _apply(cfg, {"node": "EnemySpawnerConfig.AverageFightTime", "where": {"OptimumTime": 4.4}, "mul": 2})
    assert report.ops[0].rows == len(stored)
    # a double field compares in double
    assert _code(cfg, {"node": "EnemySpawnerConfig.Stats[Health]", "where": {"Stats[Health]": 1e-300}, "mul": 2}) == "no_match"


def test_where_string_on_float_never_matches_and_enum_names_match(cfg):
    assert _code(cfg, {"node": "EnemySpawnerConfig.AverageFightTime", "where": {"AverageFightTime": "10"}, "mul": 2}) == "no_match"
    by_int = _apply(cfg, {"node": "EnemySpawnerConfig.Count", "where": {"WeaponType": 3}, "add": 0})[1]
    by_name = _apply(cfg, {"node": "EnemySpawnerConfig.Count", "where": {"WeaponType": "Buckshot"}, "add": 0})[1]
    assert by_int.ops[0].rows == by_name.ops[0].rows == 96
    assert _code(cfg, {"node": "EnemySpawnerConfig.Count", "where": {"WeaponType": "NoSuchType"}, "add": 0}) == "no_match"
    # strings on int and bool fields keep comparing the Python str() of the stored integer
    assert _apply(cfg, {"node": "EnemySpawnerConfig.Count", "where": {"LocationNumber": "2"}, "add": 0})[1].ops[0].rows \
        == _apply(cfg, {"node": "EnemySpawnerConfig.Count", "where": {"LocationNumber": 2}, "add": 0})[1].ops[0].rows


@pytest.mark.parametrize("value", [math.nan, math.inf, -math.inf, 1.5])
def test_int_fields_reject_non_integral_values(cfg, value):
    assert _code(cfg, {"node": "EnemySpawnerConfig.Count", "where": TOWER, "set": value}) == "bad_type"


def test_int_arithmetic_overflow_is_bad_type(cfg):
    many = {"go_name": "Spawner_1 (8)"}  # Count 7: 7 * 1e308 is inf
    assert _code(cfg, {"node": "EnemySpawnerConfig.Count", "where": many, "mul": 1e308}) == "bad_type"
    assert _code(cfg, {"node": "EnemySpawnerConfig.Count", "where": TOWER, "add": 2 ** 31}) == "bad_type"


@pytest.mark.parametrize("value", [True, False, 1.5, math.nan, None, 99, "Cutting "])
def test_enum_sets_reject_bools_fractions_and_non_members(cfg, value):
    node = {"node": "LocationsConfig.Locations[].PreferredWeaponType", "where": {"Number": 4}}
    assert _code(cfg, {**node, "set": value}) == "bad_type"


def test_enum_sets_accept_names_and_integral_numbers(cfg):
    node = {"node": "LocationsConfig.Locations[].PreferredWeaponType", "where": {"Number": 4}}
    news = {_apply(cfg, {**node, "set": v})[1].ops[0].leaves[0].new for v in ("Buckshot", 3, 3.0)}
    assert news == {3}


def test_override_dataclass_round_trip():
    ov = P.Override("CombatBalanceConfig.IslandTargetDays", "add", 1, index="0:2")
    assert replace(ov, source="base:x").to_json() == {"node": ov.node, "index": "0:2", "add": 1}
