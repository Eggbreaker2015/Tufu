"""P9: GameConfig fields, patch semantics and the shared cases in tests/patch_cases (design 6.1-6.2, 7.3)."""

from __future__ import annotations

import json
import re
import time
from pathlib import Path

import pytest
import yaml

from tufu_balance.field_types import FieldTypes
from tufu_balance.model import config as C
from tufu_balance.model import patch as P
from tufu_balance.model import pipeline as PL

CASES = Path(__file__).resolve().parent / "patch_cases"
SCRIPTS = Path(__file__).resolve().parents[2] / "Recovery" / "StudyProject" / "Assets" / "Scripts" / "Assembly-CSharp"


@pytest.fixture(scope="module")
def cfg(db_path):
    return C.load_game_config(db_path)


def _canon(leaves) -> list:
    return sorted(leaves, key=lambda x: (x["node"], json.dumps(x["row"], sort_keys=True),
                                         -1 if x["index"] is None else x["index"]))


# ---------------------------------------------------------------------------------------------
# Fields


def test_every_patchable_field_has_a_dictionary_node(cfg, db):
    nodes = [f.node for f in C.patchable_fields()]
    assert len(nodes) == len(set(nodes)) == len(C.FIELDS)
    dictionary = {r[0] for r in db.execute("SELECT node FROM data_dictionary")}
    missing = [n for n in nodes if n not in dictionary]
    assert not missing, missing
    for f in C.patchable_fields():
        assert f.type in C.SCALAR_TYPES and (f.array is None or f.array in C.ARRAY_ENCODINGS), f
        assert (f.type == "enum") == (f.enum is not None), f
        assert (f.array == "key_data") == (f.values_column is not None), f
        assert C.field_for(f.node) is f
    for spec in C.TABLES:
        cols = {r[1] for r in db.execute(f'PRAGMA table_info("{spec.sql}")')}
        assert set(spec.key) | {f.column for f in spec.fields} | set(spec.selectors) <= cols, spec.sql
        for f in spec.fields:
            assert f.node == f"{spec.prefix}.{spec.name_of(f.column)}"
    assert len(nodes) == 143


_CLASS = {
    "c_CombatBalanceConfig": "CombatBalanceConfig", "c_CombatBalanceConfig__EvolutionDamageValues": "EvolutionCombatValue",
    "c_CombatBalanceConfig__EvolutionArmorValues": "EvolutionCombatValue",
    "c_CombatBalanceConfig__EvolutionDamageShardsRequirements": "EvolutionShardsRequirement",
    "c_CombatBalanceConfig__EvolutionArmorShardsRequirements": "EvolutionShardsRequirement",
    "c_CombatBalanceConfig__MythicalEvolutionValues": "MythicalEvolutionValue",
    "c_CombatBalanceConfig__EvolutionIslandFillRuleRows": "EvolutionIslandFillRuleRow",
    "c_CombatBalanceConfig__EvolutionIslandTimingRows": "EvolutionIslandTimingRow",
    "c_CombatBalanceConfig__Interpolations.Interpolations": "BalanceInterpolation",
    "spawners": "EnemySpawnerConfig", "c_AmuletConfig": "AmuletConfig",
    "s_SpawnerRelativeOptimumTimeTool__Islands": "IslandOptimumTimeRange", "c_ItemConfig": "ItemConfig",
    "c_ArmorConfig__Armors": "Armor", "c_LocationsConfig__Locations": "LocationConfig",
    "c_SimulationConfig": "SimulationConfig", "c_ABTestDataConfig__spawnerCooldowns.variants": "SpawnerCooldownsData",
    "c_ABTestDataConfig__enemyHpDamageMult.variants": "EnemyHpDamageMultData",
    "c_ABTestDataConfig__fastRPMParams.variants": "FastRPMParamsData",
    "c_ABTestDataConfig__commonRewardMultipliers.variants": "CommonRewardMultipliersData",
}
_DICT_VALUE = {"LocationOverrides": "float", "IntLocationOverrides": "int"}


def _csharp_type(ft: FieldTypes, f: C.FieldSpec) -> str:
    """Declared C# type of a field, walking nested serializable classes (``data.x.y``)."""
    if f.sql == "spawners" and f.column.startswith("stat."):
        return ft.fields["StatBase"]["BaseValue"]
    cls = _CLASS[f.sql]
    parts = f.column.removeprefix("data.").split(".")
    for part in parts[:-1]:
        cls = ft.fields[cls][part]
    if parts[-1] in ("keyData", "valueData"):
        value = _DICT_VALUE[cls]
        return "List<int>" if parts[-1] == "keyData" else f"List<{value}>"
    return ft.fields[cls][parts[-1]]


def test_field_types_match_csharp_declarations():
    if not SCRIPTS.exists():
        pytest.skip(f"no C# sources at {SCRIPTS}")

    class NoEnums:
        by_name: dict = {}

    ft = FieldTypes(SCRIPTS, NoEnums())
    want = {"float": "float", "double": "double", "int": "int", "bool": "bool", "string": "string"}
    bad = []
    for f in C.patchable_fields():
        cs = _csharp_type(ft, f)
        if f.is_array:
            ok = cs in (f"{f.type}[]", f"List<{f.type}>")
        elif f.type == "enum":
            ok = cs == f.enum
        elif f.type == "ref":
            ok = cs in ("ArmorConfig", "ItemConfig")
        else:
            ok = cs == want[f.type]
        if not ok:
            bad.append((f.node, f.type, f.array, cs))
    assert not bad, bad


def test_modeled_fields_are_read_by_a_loader():
    """``modeled`` fields are the ones the oracle loaders read (spot checks of both directions)."""
    modeled = {f.node for f in C.patchable_fields() if f.modeled}
    for node in ("CombatBalanceConfig.IslandTargetDays", "EnemySpawnerConfig.AverageFightTime",
                 "EnemySpawnerConfig.Stats[Health]", "ItemConfig.FlatDamage",
                 "ABTestDataConfig.spawnerCooldowns.variants[].data.elite.charges.defaultValue",
                 "SimulationConfig.EarlySpawnerOptimumTimeOffsetMultiplier"):
        assert node in modeled, node
    for node in ("EnemySpawnerConfig.PriorityMultiplier", "ABTestDataConfig.fastRPMParams.variants[].data.fightTimeSeconds",
                 "SimulationConfig.TimeChunkDurationMinutes"):
        assert node not in modeled, node


# ---------------------------------------------------------------------------------------------
# Loading and the in-memory view


@pytest.mark.perf
def test_load_game_config_time(db_path):
    C.load_game_config(db_path)
    t = time.perf_counter()
    C.load_game_config(db_path)
    assert time.perf_counter() - t <= 0.5


def test_overlay_round_trip_is_lossless(cfg, db):
    """Rewriting every config table (and its mirrors) from the decoded rows reproduces the source."""
    con = cfg.connection(rewrite_all=True)
    for spec in C.TABLES:
        for t in (spec.sql,):
            a = sorted(map(tuple, db.execute(f'SELECT * FROM "{t}"')), key=repr)
            b = sorted(map(tuple, con.execute(f'SELECT * FROM "{t}"')), key=repr)
            assert a == b, t
    for t in ("config_arrays", "items", "s_EnemySpawner__config.Stats"):
        a = sorted(map(tuple, db.execute(f'SELECT * FROM "{t}"')), key=repr)
        b = sorted(map(tuple, con.execute(f'SELECT * FROM "{t}"')), key=repr)
        assert a == b, t


def test_decode_encode_round_trip(cfg):
    for spec in C.TABLES:
        for row in cfg.tables[spec.sql].rows:
            for f in spec.fields:
                v = f.decode(row)
                assert f.decode({**row, f.column: f.encode(v)}) == v, (f.node, spec.row_id(row))


def test_source_is_read_only(cfg):
    with pytest.raises(Exception):
        cfg.source.execute("CREATE TABLE p9_probe (x)")


# ---------------------------------------------------------------------------------------------
# Shared cases


def _case_files(sub: str = "") -> list[Path]:
    return sorted((CASES / sub).glob("*.yaml")) if (CASES / sub).exists() else []


def test_case_inventory():
    semantic = _case_files()
    assert len(semantic) >= 12
    for p in semantic + _case_files("errors"):
        assert p.with_name(p.name.replace(".yaml", ".expected.json")).exists(), p
    ops = {k for p in semantic for o in (yaml.safe_load(p.read_text()).get("overrides") or []) for k in P.OPS if k in o}
    assert ops == set(P.OPS)


@pytest.mark.parametrize("path", _case_files(), ids=lambda p: p.stem)
def test_patch_case(cfg, path):
    expected = json.loads(path.with_name(path.name.replace(".yaml", ".expected.json")).read_text(encoding="utf-8"))
    patch = P.load_patch(path)
    out, report = P.apply(cfg, patch)
    got = report.to_json()
    assert _canon(got["leaves"]) == _canon(expected["leaves"])
    ops = [{"source": o["source"], "node": o["node"], "rows": o["rows"], "leaves": len(o["leaves"])} for o in got["ops"]]
    want = [{k: v for k, v in o.items() if k != "first"} for o in expected["ops"]]
    plain = [o for o in ops if not o["source"].startswith("regenerate.")]
    assert plain == [o for o in want if not o["source"].startswith("regenerate.")]
    key = lambda o: (o["source"], o["node"])  # noqa: E731
    assert sorted((o for o in ops if o not in plain), key=key) == sorted(
        (o for o in want if o["source"].startswith("regenerate.")), key=key)
    for o, w in zip(got["ops"], expected["ops"]):
        if "first" in w:
            assert {"old": o["leaves"][0]["old"], "new": o["leaves"][0]["new"]} == w["first"]
    assert sorted({w["code"] for w in got["warnings"]}) == sorted(expected["warnings"])
    assert [o["node"] for o in got["passthrough"]] == expected["passthrough"]
    # every net leaf is readable on the patched config
    for leaf in report.leaves:
        if leaf.new is not None:
            assert out.value(leaf.node, leaf.row, leaf.index) == leaf.new


@pytest.mark.parametrize("path", _case_files("errors"), ids=lambda p: p.stem)
def test_error_case(cfg, path):
    expected = json.loads(path.with_name(path.name.replace(".yaml", ".expected.json")).read_text(encoding="utf-8"))
    with pytest.raises(P.PatchError) as e:
        P.apply(cfg, P.load_patch(path))
    assert e.value.code == expected["error"], str(e.value)


@pytest.mark.parametrize("path", _case_files(), ids=lambda p: p.stem)
def test_expanded_patch_reproduces_the_leaves(cfg, path):
    """What C# receives (overrides as written + generated explicit sets, no generators) yields the same leaves."""
    out, report = P.apply(cfg, P.load_patch(path))
    again, report2 = P.apply(cfg, P.parse_patch({"name": "expanded", "overrides": report.expanded}))
    assert [x.to_json() for x in report2.leaves] == [x.to_json() for x in report.leaves]
    generated = [x for o in report.ops if o.override is None for x in o.leaves]
    sets = report.expanded[len(report.expanded) - len(generated):]
    assert all(set(s) <= {"node", "index", "where", "set"} and "set" in s for s in sets)


def test_report_is_deterministic_json(cfg):
    patch = P.load_patch(CASES / "16_regenerate_island17.yaml")
    a = json.dumps(P.apply(cfg, patch)[1].to_json(), sort_keys=True)
    b = json.dumps(P.apply(cfg, patch)[1].to_json(), sort_keys=True)
    assert a == b


# ---------------------------------------------------------------------------------------------
# Semantics beyond the shared cases


def test_empty_patch_and_noop_generators(cfg):
    """Every generator reproduces the shipped config: no sets, no warnings (the 16 OT exceptions are kept)."""
    regen = {k: True for k in P.BOOL_STEPS} | {"evolution_tables": "replay_stored_inputs"}
    out, report = P.apply(cfg, P.parse_patch({"name": "noop", "regenerate": regen}))
    assert report.leaves == [] and report.warnings == [] and report.expanded == []
    assert out.changed_tables() == []
    out, report = P.apply(cfg, P.parse_patch({"name": "empty"}))
    assert report.to_json()["leaves"] == [] and out.changed_tables() == []


def test_where_all_rows_and_open_slice(cfg):
    p = P.parse_patch({"name": "x", "overrides": [
        {"node": "CombatBalanceConfig.EvolutionIslandFillRuleRows[].StepPerEvolution", "where": {}, "mul": 1},
        {"node": "CombatBalanceConfig.IslandTargetDays", "index": "97:", "add": 1}]})
    _, report = P.apply(cfg, p)
    assert report.ops[0].rows == 10 and report.ops[0].leaves and all(x.old == x.new for x in report.ops[0].leaves)
    assert [x.index for x in report.leaves] == [97, 98]


def test_enum_set_by_name_updates_name_column(cfg):
    p = P.parse_patch({"name": "x", "overrides": [
        {"node": "LocationsConfig.Locations[].PreferredWeaponType", "where": {"Number": 4}, "set": "Buckshot"}]})
    out, report = P.apply(cfg, p)
    (leaf,) = report.leaves
    assert leaf.new == 3
    row = out.tables["c_LocationsConfig__Locations"].rows[report.leaves[0].row["idx"]]
    assert row["PreferredWeaponType__name"] == "Buckshot"
    assert PL.bake_context(out).preferred_weapon_type[4] == 3


def test_ref_and_bool_and_patch_file_errors(cfg, tmp_path):
    armor = sorted(P._ref_names(cfg, C.FIELDS["EnemySpawnerConfig.ArmorConfig"]))[0]
    out, report = P.apply(cfg, P.parse_patch({"name": "x", "overrides": [
        {"node": "EnemySpawnerConfig.ArmorConfig", "where": {"go_name": "Spawner_elite_HP"}, "set": armor},
        {"node": "EnemySpawnerConfig.ItemReward", "where": {"go_name": "Spawner_elite_HP"}, "set": None}]}))
    assert out.value("EnemySpawnerConfig.ArmorConfig", report.ops[0].leaves[0].row) == armor
    for bad, code in (({"name": "x", "nope": 1}, "bad_patch"), ({"regenerate": {"bake": "yes"}}, "bad_patch"),
                      ({"regenerate": {"evolution_tables": "regenerate"}}, "bad_patch"),
                      ({"overrides": [{"node": "CombatBalanceConfig.IslandTargetDays", "index": 0, "set": True}]},
                       "bad_type"),
                      ({"overrides": [{"node": "EnemySpawnerConfig.HandBalance", "where": {"go_name": "Spawner_elite_HP"},
                                       "set": 2}]}, "bad_type"),
                      ({"overrides": [{"node": "CombatBalanceConfig.IslandTargetDays", "add": [1]}]}, "bad_type"),
                      ({"overrides": [{"node": "EnemySpawnerConfig.AverageFightTime", "index": 0, "mul": 2}]},
                       "bad_index"),
                      ({"overrides": [{"node": "CombatBalanceConfig.CopyValueTargetShare", "where": {"x": 1}, "set": 1}]},
                       "bad_selector")):
        with pytest.raises(P.PatchError) as e:
            P.apply(cfg, P.parse_patch(bad))
        assert e.value.code == code, (bad, str(e.value))
    a, b = tmp_path / "a.yaml", tmp_path / "b.yaml"
    a.write_text("name: a\nbase: b.yaml\n")
    b.write_text("name: b\nbase: a.yaml\n")
    with pytest.raises(P.PatchError, match="cycle"):
        P.load_patch(a)


def test_warnings_list_reading_formulas(cfg):
    _, report = P.apply(cfg, P.load_patch(CASES / "17_passthrough_unmodeled_node.yaml"))
    (w,) = report.warnings
    assert w.detail["formulas"], w
    _, report = P.apply(cfg, P.load_patch(CASES / "18_not_modeled_static.yaml"))
    assert any("priority" in f for f in report.warnings[0].detail["formulas"]), report.warnings[0].detail


def test_exception_spawners_keep_their_ot(cfg):
    base = PL.derive_static(cfg)
    exceptions = set(base.checks["G03.exceptions"])
    out, report = P.apply(cfg, P.load_patch(CASES / "19_regenerate_exception_kept.yaml"))
    (w,) = report.warnings
    kept = {f"{x['row']['scene']}:{x['row']['file_id']}" for x in w.detail["leaves"]}
    assert w.code == "exception_kept" and kept and kept <= exceptions
    changed = {f"{x.row['scene']}:{x.row['file_id']}" for x in report.leaves if x.node == "EnemySpawnerConfig.OptimumTime"}
    assert not changed & exceptions
    after = PL.derive_static(out, exceptions_from=cfg)
    assert set(after.checks["G03.exceptions"]) == exceptions  # kept values stay off the rule, nothing else
    flagged = {r["spawner"] for r in after.tables["optimum_time"] if r["exception_in_base"]}
    assert flagged == exceptions


# ---------------------------------------------------------------------------------------------
# Known traps


def _cooldown(static: PL.StaticResult, spawner: str) -> float:
    return static.rows_by_key("spawner_runtime")[(spawner,)]["base_cooldown"]


def test_ab_removed_override_key_falls_back_to_class_default(cfg):
    """P8's ab.py merges the asset dictionary over the C# class-default overrides: dropping a key from the
    asset gives the class default (28 / 150 minutes), not defaultValue (30 / 180) and not the old asset value."""
    defs = PL.spawner_defs(cfg)
    ab = PL.ab_config(cfg)
    non_elite = next(s for s in defs if s.location == 13 and not s.is_elite and s.active
                     and PL.SP.uses_spawner_cooldowns_ab(s))
    elite = next(s for s in defs if s.location == 4 and s.is_elite and s.active and PL.SP.uses_spawner_cooldowns_ab(s))
    ids = {k: f"{s.scene}:{s.file_id}" for k, s in (("non_elite", non_elite), ("elite", elite))}
    base = PL.derive_static(cfg)
    assert (_cooldown(base, ids["non_elite"]), _cooldown(base, ids["elite"])) == (28 * 60.0, 150 * 60.0)
    assert ab.spawner_cooldowns().non_elite.cooldown_minutes.default == 30
    case = P.load_patch(CASES / "12_ab_remove_override_key.yaml")
    first = P.Patch("only_first", overrides=case.overrides[:1])
    assert _cooldown(PL.derive_static(P.apply(cfg, first)[0]), ids["non_elite"]) == 40 * 60.0
    removed = PL.derive_static(P.apply(cfg, case)[0])
    assert _cooldown(removed, ids["non_elite"]) == 28 * 60.0
    removed = PL.derive_static(P.apply(cfg, P.load_patch(CASES / "13_ab_remove_legacy_key.yaml"))[0])
    assert _cooldown(removed, ids["elite"]) == 150 * 60.0
    # changing the variant's defaultValue moves only islands without an override (class default or asset)
    out, _ = P.apply(cfg, P.parse_patch({"name": "x", "overrides": [
        {"node": "ABTestDataConfig.spawnerCooldowns.variants[].data.nonElite.cooldownMinutes.defaultValue",
         "where": {"variant": "A"}, "set": 31}]}))
    d = PL.diff_static(base, PL.derive_static(out)).get("spawner_runtime", [])
    islands = {s.spawner_id: s.location for s in defs}
    moved = {islands[(k.split(":")[0], int(k.split(":")[1]))] for c in d for k in c["key"]}
    assert moved and min(moved) > 13


def test_fitted_h_armor_sync_reported_or_applied(cfg):
    """P5's fitted_h generates armor per type, which breaks the SyncShieldValuesByLeader identity (G09)."""
    assert P.evolution_config(cfg).levers.sync_shield_values_by_leader
    _, report = P.apply(cfg, P.parse_patch({"name": "f", "regenerate": {"evolution_tables": "fitted_h"}}))
    codes = [w.code for w in report.warnings]
    assert codes == ["armor_sync_broken"]
    cells = report.warnings[0].detail["cells"]
    out, _ = P.apply(cfg, P.parse_patch({"name": "f", "regenerate": {"evolution_tables": "fitted_h"}}))
    reported = PL.derive_static(out).checks
    assert reported["evolution_values.armor_unsynced_cells"] == len(cells) > 0
    assert 75 - len(cells) <= min(reported["G09.armor_sync_value"], reported["G09.armor_sync_copy_value"]) < 75
    synced, report = P.apply(cfg, P.parse_patch({"name": "s", "regenerate": {"evolution_tables": "fitted_h",
                                                                             "evolution_armor_sync": "Stabbing"}}))
    assert not report.warnings
    checks = PL.derive_static(synced).checks
    assert (checks["G09.armor_sync_value"], checks["G09.armor_sync_copy_value"]) == (75, 75)
    assert checks["G08.half_even"] == 450  # percent follows each row's own stat value
    assert report.leaves and {x.node.rsplit(".", 1)[-1] for x in report.leaves} <= {"Value", "CopyValue",
                                                                                     "ReferenceStatPercent"}
    # replay keeps the identity: no warning
    _, report = P.apply(cfg, P.parse_patch({"name": "r", "regenerate": {"evolution_tables": "replay_stored_inputs"}}))
    assert not report.warnings and not report.leaves


def test_spawner_stat_mirror_follows_patch(cfg):
    out, report = P.apply(cfg, P.load_patch(CASES / "10_stat_field.yaml"))
    (leaf,) = report.leaves
    con = out.connection()
    (v,) = con.execute('SELECT BaseValue FROM "s_EnemySpawner__config.Stats" WHERE scene = ? AND file_id = ? AND Type = 1',
                       (leaf.row["scene"], leaf.row["file_id"])).fetchone()
    assert v == leaf.new == 8612.0
    assert re.match(r"^\[.*\]$", out.connection().execute(
        "SELECT IslandTargetDays FROM c_CombatBalanceConfig").fetchone()[0])
