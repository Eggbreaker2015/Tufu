"""P9: derive_static on the shipped config (golden suite ``pipeline``) and the static diff pipeline (design 6.3)."""

from __future__ import annotations

import json
import sqlite3
import struct
import time
from pathlib import Path

import numpy as np
import pytest

from tufu_balance.golden import check, exact, same_set
from tufu_balance.model import ab as AB
from tufu_balance.model import bake as B
from tufu_balance.model import config as C
from tufu_balance.model import data as MD
from tufu_balance.model import evolution_gen as EG
from tufu_balance.model import items as IT
from tufu_balance.model import patch as P
from tufu_balance.model import pipeline as PL
from tufu_balance.model import snapshots as S
from tufu_balance.model import spawners as SP
from tufu_balance.model import timeline as TL
from tufu_balance.model.f32 import f32

SUITE = "pipeline"
GOLDEN = Path(__file__).resolve().parent / "golden"
CASES = Path(__file__).resolve().parent / "patch_cases"

# Design 3.3 (today's numbers on the shipped data).
DESIGN = {
    "G01.tail_f32_exact": 84, "G02.gate_ot_eq_itd": 16, "G03.rel_to_ot_hits": 701,
    "G04.fill_rule_island_exact": 150, "G04.target_timings_f32_exact": 150,
    "G05.knots": 49, "G05.knots_le_5e-5": 49, "G05.knots_f32_exact": 45,
    "G06.amulet_level_target_days_eq_island_start": 7,
    "G07.weapon_value": 225, "G07.weapon_copy_value": 225, "G07.armor_value": 225, "G07.armor_copy_value": 225,
    "G08.half_even": 450,
    "G09.filled_rows": 265, "G09.filled_h_eq_last_value": 265, "G09.filled_copy_value_frozen": 265,
    "G09.armor_sync_cells": 75, "G09.armor_sync_value": 75, "G09.armor_sync_copy_value": 75,
    "G09.armor_sync_heuristic": 75, "G09.armor_sync_shards": 75, "G09.armor_sync_time_chunk": 48,
    "G09.armor_sync_stat_value": 48, "G09.universal_rows": 150, "G09.universal_placeholders": 150,
    "G09.reference_shards_rows": 570, "G09.reference_shards_eq_table": 570,
    "G10.value": 15, "G10.copies_to_evolve": 15, "G10.copy_value": 15, "G10.copied_rows": 120, "G10.shards_rows": 30,
    "G10.on_nice_grid": 11,
    "G11.k2": 109, "G11.k3": 109, "G11.cells": 981, "G11.sff": 981, "G11.pm": 981, "G11.tf": 981, "G11.tv": 981,
    "G11.other_cells": 2289, "G11.other": 2289,
    "G12.rows_at_snapshot_days": 16, "G12.rel_lt_1e-6": 16, "G12.bracket_checked": 162, "G12.bracket_inside": 157,
    "G12.bracket_outside_in_other_type": 5, "G12.groups": 178, "G12.groups_single_valued": 178,
    "G13.minute_n": 108, "G13.minute_hp_exact": 79, "G13.minute_damage_exact": 104, "G13.minute_both_exact": 79,
    "G13.ot_0_120_n": 78, "G13.ot_0_120_hp_exact": 76, "G13.bakeinfo_health_eq_pred": 13,
    "G13.bakeinfo_damage_eq_pred": 43,
}
# Frozen by the domain units (their original baselines), where design 3.3 gives no number.
FROM_SUITES = {
    "G01.regenerated_table_f32_exact": ("timeline", "G01.regenerated_table_f32_exact"),
    "G03.window_from_itd_eq_tool": ("timeline", "G03.window_from_itd_eq_tool"),
    "G05.curves_text_exact": ("timeline", "G05.curves_text_exact"),
    "G18.n": ("items", "G18.n"),
    "spawners.registered": ("spawners", "ready.registered"),
    "spawners.uses_ab": ("spawners", "G17.ab_override"),
}
SETS = {"G03.exceptions": ("timeline", "G03.exceptions"), "G13.minute_hp_exact_set": ("bake", "G13.minute_hp_exact_set")}


def _original(suite: str, key: str):
    return json.loads((GOLDEN / "original" / f"{suite}.json").read_text(encoding="utf-8"))["expectations"][key]["value"]


@pytest.fixture(scope="module")
def cfg(db_path):
    return C.load_game_config(db_path)


@pytest.fixture(scope="module")
def shipped(cfg):
    return PL.derive_static(cfg)


# ---------------------------------------------------------------------------------------------
# Golden: derive_static(shipped config) reproduces G01-G13


def test_derive_static_reproduces_g01_g13(shipped):
    c = shipped.checks
    results = {k: exact(c[k], source=k.split(".")[0], original=v) for k, v in DESIGN.items()}
    results.update({k: exact(c[k], source=k.split(".")[0], original=_original(*src), note=f"{src[0]} suite: {src[1]}")
                    for k, src in FROM_SUITES.items()})
    results.update({k: same_set(c[k], source=k.split(".")[0], original=_original(*src), note=f"{src[0]} suite: {src[1]}")
                    for k, src in SETS.items()})
    check(SUITE, results)
    assert c["evolution_values.frozen"] == {k: 225 for k in ("weapon_value", "weapon_copy_value", "armor_value",
                                                            "armor_copy_value")}
    assert c["evolution_values.armor_unsynced_cells"] == 0
    assert shipped.warnings == []


def test_g07_through_replay_mode(cfg):
    r = PL.derive_static(cfg, evolution_tables="replay_stored_inputs")
    got = r.checks["evolution_values.replay_stored_inputs"]
    check(SUITE, {"G07.replay_table": exact(got, source="G07", original={
        "weapon_value": 225, "weapon_copy_value": 225, "armor_value": 225, "armor_copy_value": 225},
        note="evolution_values table regenerated with replay_stored_inputs == stored rows")})
    assert all(r2["value"] == r2["stored_value"] and r2["copy_value"] == r2["stored_copy_value"]
               and r2["percent"] == r2["stored_percent"] for r2 in r.tables["evolution_values"])


def test_fitted_h_mode_reports_unsynced_armor(cfg):
    r = PL.derive_static(cfg, evolution_tables="fitted_h")
    synced = PL.derive_static(cfg, evolution_tables="fitted_h", armor_sync="Stabbing")
    assert [w["code"] for w in r.warnings] == ["armor_sync_broken"]
    assert synced.warnings == [] and synced.checks["evolution_values.armor_unsynced_cells"] == 0
    check(SUITE, {
        "fitted_h.regenerated_eq_stored": exact(r.checks["evolution_values.fitted_h"], source="R4",
                                                note="whole tables, chained recursion (P5 fitted_h)"),
        "fitted_h.armor_unsynced_cells": exact(r.checks["evolution_values.armor_unsynced_cells"], source="G09",
                                               note="(rarity, level) armor cells whose types differ; of 75"),
        "fitted_h.leader_stabbing_regenerated_eq_stored": exact(
            synced.checks["evolution_values.fitted_h"], source="R4", note="armor synced to the Stabbing rows"),
    })


# ---------------------------------------------------------------------------------------------
# Empty patch == the domain functions called directly


def _rewritten(cfg: C.GameConfig) -> C.GameConfig:
    """Same values, every table a new object: the in-memory view rewrites all of them from the leaves."""
    out, report = P.apply(cfg, P.parse_patch({"name": "empty"}))
    assert report.leaves == [] and out.changed_tables() == []
    return out.with_tables({s: C.ConfigTable(t.spec, tuple(dict(r) for r in t.rows)) for s, t in out.tables.items()})


def _f(x):
    return None if x is None else float(x)


def _sid(scene, file_id) -> str:
    return f"{scene}:{file_id}"


def _direct(con) -> dict[str, dict]:
    """Every derive_static table, projected to its derived cells, straight from a database with the domain
    functions (no GameConfig): the independent side of the empty-patch and propagation tests."""
    out: dict[str, dict] = {}
    itd = [float(x) for x in MD.load_island_target_days(con)]
    regenerated = TL.generate_island_target_days(itd[:TL.TAIL_FIRST_ISLAND - 1], len(itd))
    spawners = SP.load_spawners(con)
    sp_rows = {(r["scene"], r["file_id"]): dict(r) for r in con.execute("SELECT * FROM spawners")}
    gates = TL.island_target_days_from_gates((s.location, sp_rows[s.spawner_id]["OptimumTime"], s.is_days)
                                             for s in spawners if s.gate_next_locations)
    out["island_target_days"] = {(i,): (itd[i - 1], float(TL.island_target_days_closed_form(i)) if i >= 16 else None,
                                        float(regenerated[i - 1]), _f(gates.get(i)), float(TL.island_start_day(itd, i)))
                                 for i in range(1, len(itd) + 1)}
    tool = {r["LocationNumber"]: dict(r) for r in con.execute("SELECT * FROM s_SpawnerRelativeOptimumTimeTool__Islands")}
    out["relative_windows"] = {}
    for L, w in tool.items():
        lo, hi, days = TL.relative_window(itd, L)
        out["relative_windows"][(L,)] = (float(lo), float(hi), bool(days), _f(f32(w["MinOptimumTime"])),
                                         _f(f32(w["MaxOptimumTime"])), bool(w["IsDays"]))
    out["optimum_time"] = {}
    for (scene, file_id), r in sp_rows.items():
        w = tool.get(r["LocationNumber"])
        rule = None if w is None else float(TL.optimum_time_from_relative(
            r["RelativeOptimumTime"], w["MinOptimumTime"], w["MaxOptimumTime"], bool(r["IsDays"])))
        out["optimum_time"][(_sid(scene, file_id),)] = (float(f32(r["OptimumTime"])), rule)
    out["evolution_islands"] = {}
    for r in con.execute('SELECT * FROM "c_CombatBalanceConfig__EvolutionIslandFillRuleRows"'):
        for k, e in enumerate(TL.evolution_islands(r["StartValue"], r["StepPerEvolution"]), start=1):
            out["evolution_islands"][(r["Rarity"], r["Name"], k)] = float(e)
    cbc = con.execute("SELECT * FROM c_CombatBalanceConfig").fetchone()
    out["evolution_target_days"] = {}
    for r in con.execute('SELECT * FROM "c_CombatBalanceConfig__EvolutionIslandTimingRows"'):
        kind = "armor" if r["Name"].startswith("Armor") else "weapon"
        array = f"{r['Rarity__name']}{'Armor' if kind == 'armor' else ''}EvolutionTargetTimings"
        stored = json.loads(cbc[array])
        days = TL.evolution_target_timings([r[f"E{k}"] for k in range(1, 16)], itd)
        for k, d in enumerate(days, start=1):
            out["evolution_target_days"][(kind, r["Rarity"], k)] = (float(f32(stored[k - 1])), float(d))
    out["interpolation_points"] = {}
    for r in con.execute('SELECT * FROM "c_CombatBalanceConfig__Interpolations.Interpolations"'):
        islands = [float(x) for x in r["IslandPoints"].split(";")]
        out["interpolation_points"][(r["Interpolations.Interpolations#idx"],)] = (
            r["TimingPoints"], TL.format_points(TL.interpolation_timing_points(islands, itd)))
    raw = bytes.fromhex(con.execute("SELECT LevelTargetDays FROM c_AmuletConfig").fetchone()[0])
    level_days = struct.unpack(f"<{len(raw) // 4}i", raw)
    derived = TL.amulet_level_target_days(itd)
    out["amulet_level_days"] = {(k,): (float(v), float(derived[k - 1]) if k <= len(derived) else None)
                                for k, v in enumerate(level_days, start=1)}
    evo = EG.load_evolution_config(con)
    out["evolution_values"] = {(x.kind, x.damage_type, x.rarity, x.evolution_level):
                               (float(x.value), float(x.copy_value), x.reference_stat_percent) for x in evo.rows}
    sd = MD.load_simulation_data(con)
    interp = MD.load_interpolations(con)
    out["snapshot_composition"] = {}
    for name, lst in (("time_chunk_snapshots", sd.time_chunk_snapshots), ("snapshots", sd.snapshots)):
        for i, snap in enumerate(lst):
            out["snapshot_composition"][(name, i)] = (
                *S.composition_coefficients(interp, snap.time_chunk_index), len(S.composition_mismatches(snap, interp)))
    ctx = MD.load_bake_context(con, simulation_data=sd)
    armors = MD.load_armor_configs(con)
    out["bake"] = {}
    for r in MD.load_spawner_rows(con):
        rep = B.bake_spawner(ctx, MD.spawner_input_from_row(r, armors))
        out["bake"][(_sid(r["scene"], r["file_id"]),)] = (rep.recalc_optimum_time, rep.day, rep.armor_type,
                                                          rep.ability_dps, rep.new_health, rep.new_damage)
    ready = SP.expected_ready_spawners(con=con)
    out["spawner_runtime"] = {(_sid(*k),): (r.charges_total, r.base_cooldown, r.charges_limit) for k, r in ready.items()}
    out["bosses"] = {(n,): (_sid(b.scene, b.file_id) if b else None,)
                     for n, b in SP.bosses_by_location(spawners).items()}
    jumps = IT.evolve_jumps(IT.g18_items(IT.load_item_defs(con)), IT.load_item_tables(con))
    out["item_evolve_jumps"] = {(j.key, j.evolution_level): (j.before, j.after, j.ratio) for j in jumps}
    return out


PROJECTION = {
    "island_target_days": ("stored", "closed_form", "regenerated", "from_gates", "start_day"),
    "relative_windows": ("min", "max", "is_days", "tool_min", "tool_max", "tool_is_days"),
    "optimum_time": ("stored", "rule"), "evolution_islands": ("fill_rule",),
    "evolution_target_days": ("stored", "derived"), "interpolation_points": ("stored", "derived"),
    "amulet_level_days": ("stored", "derived"), "evolution_values": ("stored_value", "stored_copy_value", "stored_percent"),
    "snapshot_composition": ("k2", "k3", "mismatches"),
    "bake": ("recalc_optimum_time", "day", "armor_type", "ability_dps", "new_health", "new_damage"),
    "spawner_runtime": ("charges_total", "base_cooldown", "charges_limit"), "bosses": ("spawner",),
    "item_evolve_jumps": ("before", "after", "ratio"),
}


def _project(res: PL.StaticResult) -> dict[str, dict]:
    out = {}
    for name, fields in PROJECTION.items():
        rows = res.rows_by_key(name)
        out[name] = {k: (r[fields[0]] if len(fields) == 1 and name == "evolution_islands" else
                         tuple(r[f] for f in fields)) for k, r in rows.items()}
    return out


def test_empty_patch_equals_direct_domain_calls(cfg, db):
    rt = _rewritten(cfg)
    assert len(rt.changed_tables()) == len(C.TABLES)
    got = PL.derive_static(rt)
    assert set(got.tables) == set(PROJECTION) == set(PL.KEYS)
    direct = _direct(db)
    projected = _project(got)
    for name in PROJECTION:
        assert projected[name] == direct[name], name
    # the rewritten view and the untouched source give the same tables and checks
    plain = PL.derive_static(cfg)
    assert got.tables == plain.tables and got.checks == plain.checks


# ---------------------------------------------------------------------------------------------
# Patched inputs reach every derived table (independent side: an in-memory DB copy edited with SQL)


def _mem(db) -> sqlite3.Connection:
    mem = sqlite3.connect(":memory:")
    db.backup(mem)
    mem.row_factory = sqlite3.Row
    return mem


def _edit_json(con, table: str, column: str, where: str, fn) -> None:
    for rowid, cell in con.execute(f'SELECT rowid, "{column}" FROM "{table}" WHERE {where}').fetchall():
        con.execute(f'UPDATE "{table}" SET "{column}" = ? WHERE rowid = ?', (json.dumps(fn(json.loads(cell))), rowid))


def _sql_itd(con):
    con.execute("UPDATE config_arrays SET value = value + 2 WHERE field_path = 'IslandTargetDays' AND idx = 9")
    _edit_json(con, "c_CombatBalanceConfig", "IslandTargetDays", "1", lambda a: [*a[:9], a[9] + 2, *a[10:]])


def _sql_amulet(con):
    raw = bytes.fromhex(con.execute("SELECT LevelTargetDays FROM c_AmuletConfig").fetchone()[0])
    v = list(struct.unpack(f"<{len(raw) // 4}i", raw))
    v[0] += 1
    con.execute("UPDATE c_AmuletConfig SET LevelTargetDays = ?", (struct.pack(f"<{len(v)}i", *v).hex(),))


def _first_boss(db) -> tuple[str, int]:
    b = SP.bosses_by_location(SP.load_spawners(db))[5]
    return b.scene, b.file_id


def _first_gate(db) -> tuple[str, int]:
    g = next(s for s in SP.load_spawners(db) if s.gate_next_locations and s.location == 6)
    return g.scene, g.file_id


def _g18_item(db) -> str:
    return sorted(IT.g18_items(IT.load_item_defs(db)), key=lambda i: i.key)[0].key


# name -> (overrides (filled from the shipped DB), SQL edit of the copy, tables that must move)
PROPAGATION = {
    "itd": (lambda db: [{"node": "CombatBalanceConfig.IslandTargetDays", "index": 9, "add": 2}], _sql_itd,
            {"island_target_days", "relative_windows", "evolution_target_days", "interpolation_points", "bake"}),
    "interpolation_use": (
        lambda db: [{"node": "CombatBalanceConfig.Interpolations.Interpolations[].Use", "where": {"Type": t},
                     "set": False} for t in (2, 4)],
        lambda con: con.execute('UPDATE "c_CombatBalanceConfig__Interpolations.Interpolations" SET Use = 0 '
                                "WHERE Type IN (2, 4)"),
        {"snapshot_composition", "bake"}),
    "preferred_weapon": (
        lambda db: [{"node": "LocationsConfig.Locations[].PreferredWeaponType", "where": {"Number": n}, "set": "Cutting"}
                    for n in range(1, 18)],
        lambda con: con.execute("UPDATE c_LocationsConfig__Locations SET PreferredWeaponType = 1, "
                                "PreferredWeaponType__name = 'Cutting' WHERE Number BETWEEN 1 AND 17"),
        {"bake"}),
    "armor_resist": (
        lambda db: [{"node": "ArmorConfig.Armors[].Resist", "where": {}, "mul": 0.5}],
        lambda con: con.execute("UPDATE c_ArmorConfig__Armors SET Resist = Resist * 0.5"),
        {"bake"}),
    "boss": (
        lambda db: [{"node": "EnemySpawnerConfig.LocationNumber",
                     "where": dict(zip(("scene", "file_id"), _first_boss(db))), "set": 6}],
        lambda con: con.execute("UPDATE spawners SET LocationNumber = 6 WHERE scene = ? AND file_id = ?",
                                _first_boss(con)),
        {"bosses", "island_target_days", "optimum_time", "bake"}),
    "spawner_cooldown": (
        lambda db: [{"node": "EnemySpawnerConfig.Cooldown", "where": {}, "add": 1}],
        lambda con: con.execute("UPDATE spawners SET Cooldown = Cooldown + 1"),
        {"spawner_runtime"}),
    "item_rarity": (
        lambda db: [{"node": "ItemConfig.Rarity", "where": {"Key": _g18_item(db)}, "set": "Legendary"}],
        lambda con: [con.execute(f"UPDATE {t} SET Rarity = 4, Rarity__name = 'Legendary' WHERE Key = ?",
                                 (_g18_item(con),)) for t in ("c_ItemConfig", "items")],
        {"item_evolve_jumps"}),
    "amulet": (lambda db: [{"node": "AmuletConfig.LevelTargetDays", "index": 0, "add": 1}], _sql_amulet,
               {"amulet_level_days"}),
    "fill_rule": (
        lambda db: [{"node": "CombatBalanceConfig.EvolutionIslandFillRuleRows[].StartValue", "index": 0, "add": 1}],
        lambda con: con.execute('UPDATE "c_CombatBalanceConfig__EvolutionIslandFillRuleRows" SET StartValue = '
                                'StartValue + 1 WHERE "EvolutionIslandFillRuleRows#idx" = 0'),
        {"evolution_islands"}),
    "tool_window": (
        lambda db: [{"node": "SpawnerRelativeOptimumTimeTool.Islands[].MaxOptimumTime", "where": {"LocationNumber": 5},
                     "add": 10}],
        lambda con: con.execute("UPDATE s_SpawnerRelativeOptimumTimeTool__Islands SET MaxOptimumTime = "
                                "MaxOptimumTime + 10 WHERE LocationNumber = 5"),
        {"relative_windows", "optimum_time"}),
    "gate_ot": (
        lambda db: [{"node": "EnemySpawnerConfig.OptimumTime", "where": dict(zip(("scene", "file_id"), _first_gate(db))),
                     "add": 1}],
        lambda con: con.execute("UPDATE spawners SET OptimumTime = OptimumTime + 1 WHERE scene = ? AND file_id = ?",
                                _first_gate(con)),
        {"island_target_days", "optimum_time", "bake"}),
    "evolution_value": (
        lambda db: [{"node": "CombatBalanceConfig.EvolutionDamageValues[].Value", "index": 3, "mul": 2}],
        lambda con: con.execute('UPDATE "c_CombatBalanceConfig__EvolutionDamageValues" SET Value = Value * 2 '
                                'WHERE "EvolutionDamageValues#idx" = 3'),
        {"evolution_values", "item_evolve_jumps"}),
}


@pytest.fixture(scope="module")
def shipped_projection(shipped):
    return _project(shipped)


@pytest.mark.parametrize("name", sorted(PROPAGATION))
def test_patched_inputs_reach_derived_tables(cfg, db, shipped_projection, name):
    """Each derived table reads the patched config, not the base: the pipeline's patched side equals the domain
    functions on a DB copy edited with SQL (mirrors included), and the expected tables move."""
    overrides, edit, moves = PROPAGATION[name]
    res = PL.run(cfg, P.parse_patch({"name": name, "overrides": overrides(db)}))
    mem = _mem(db)
    edit(mem)
    direct = _direct(mem)
    got = _project(res.patched)
    for table in PROJECTION:
        assert got[table] == direct[table], table
    moved = {t for t in PROJECTION if got[t] != shipped_projection[t]}
    assert moved == moves
    assert set(res.diff) >= moves


def test_connection_mirrors_follow_the_patch(cfg):
    """The domain loaders read mirrors: config_arrays (CombatBalanceConfig arrays) and items (ItemConfig)."""
    out, _ = P.apply(cfg, P.parse_patch({"name": "m", "overrides": [
        {"node": "CombatBalanceConfig.IslandTargetDays", "index": 9, "add": 2},
        {"node": "ItemConfig.Rarity", "where": {"Key": cfg.tables["c_ItemConfig"].rows[0]["Key"]}, "set": "Epic"}]}))
    con = out.connection()
    (v,) = con.execute("SELECT value FROM config_arrays WHERE field_path = 'IslandTargetDays' AND idx = 9").fetchone()
    assert v == cfg.value(PL.ITD)[9] + 2
    key = cfg.tables["c_ItemConfig"].rows[0]["Key"]
    assert {r[0] for r in con.execute("SELECT Rarity FROM items WHERE Key = ?", (key,))} == {3}
    assert MD.load_island_target_days(con)[9] == cfg.value(PL.ITD)[9] + 2


# ---------------------------------------------------------------------------------------------
# Pipeline (static half of design 6.3)


def test_diff_of_empty_patch_is_empty(cfg, shipped):
    res = PL.run(cfg, P.parse_patch({"name": "empty"}))
    assert res.diff == {} and res.warnings == [] and res.report.leaves == []
    assert PL.diff_static(shipped, shipped) == {}


def test_island10_later_example(cfg):
    """Design 6.1's example: island 10 two days later, elites of island 10 fight 20% longer."""
    patch = P.parse_patch({"name": "island10_later", "overrides": [
        {"node": "CombatBalanceConfig.IslandTargetDays", "index": 9, "add": 2.0},
        {"node": "EnemySpawnerConfig.AverageFightTime", "where": {"LocationNumber": 10, "IsEliteBoss": 1}, "mul": 1.2}],
        "regenerate": {"island_target_days_tail": False, "optimum_time_from_relative": True,
                       "evolution_target_timings": True, "interpolation_timing_points": True,
                       "evolution_tables": "frozen"}})
    res = PL.run(cfg, patch)
    assert res.patched.checks["G03.rel_to_ot_hits"] == 701 and res.patched.checks["G04.target_timings_f32_exact"] == 150
    assert res.patched.checks["G05.curves_text_exact"] == 19 and res.patched.checks["G01.tail_f32_exact"] == 84
    by_location = {r["spawner"]: r["location"] for r in res.base.tables["optimum_time"]}
    moved = {by_location[c["key"][0]] for c in res.diff["optimum_time"] if c["field"] == "stored"}
    assert moved == {10, 11}
    assert {c["key"][0] for c in res.diff["relative_windows"]} == {10, 11}
    assert {c["key"][0] for c in res.diff["island_target_days"]} == {10, 11}  # stored ITD[9], start day of 11
    fight = [x for x in res.report.leaves if x.node == "EnemySpawnerConfig.AverageFightTime"]
    assert len(fight) == 4
    for x in fight:
        sid = f"{x.row['scene']}:{x.row['file_id']}"
        assert any(c["key"] == [sid] for c in res.diff["bake"])
    assert "evolution_target_days" in res.diff and "interpolation_points" in res.diff
    assert not res.diff.get("evolution_values") and not res.diff.get("spawner_runtime")


def test_single_spawner_fight_time_changes_only_its_bake(cfg):
    """P12's example B on the static side: one spawner's AverageFightTime x1.2 changes only that bake row."""
    patch = P.parse_patch({"name": "one", "overrides": [
        {"node": "EnemySpawnerConfig.AverageFightTime", "where": {"go_name": "Spawner_elite_HP"}, "mul": 1.2}]})
    res = PL.run(cfg, patch)
    (leaf,) = res.report.leaves
    sid = f"{leaf.row['scene']}:{leaf.row['file_id']}"
    assert set(res.diff) == {"bake"} and {c["key"][0] for c in res.diff["bake"]} == {sid}


def test_pipeline_warns_on_exception_inputs(cfg):
    shipped = PL.derive_static(cfg)
    exc = shipped.checks["G03.exceptions"][0]
    scene, file_id = exc.rsplit(":", 1)
    hand = next(r["spawner"] for r in shipped.tables["bake"] if r["hand_balance"])
    hs, hf = hand.rsplit(":", 1)
    patch = P.parse_patch({"name": "x", "overrides": [
        {"node": "EnemySpawnerConfig.RelativeOptimumTime", "where": {"scene": scene, "file_id": int(file_id)}, "add": 0.01},
        {"node": "EnemySpawnerConfig.Difficulty", "where": {"scene": hs, "file_id": int(hf)}, "mul": 2}]})
    res = PL.run(cfg, patch)
    codes = {w["code"]: w for w in res.warnings}
    assert exc in codes["ot_exception_input_changed"]["spawners"]
    assert hand in codes["hand_balance_input_changed"]["spawners"]


def test_run_is_deterministic(cfg):
    patch = P.load_patch(CASES / "16_regenerate_island17.yaml")
    a, b = PL.run(cfg, patch), PL.run(cfg, patch)
    dump = lambda r: json.dumps({"diff": r.diff, "report": r.report.to_json(), "warnings": r.warnings},  # noqa: E731
                                sort_keys=True, default=repr)
    assert dump(a) == dump(b)


def test_ab_variant_option(cfg):
    a = PL.derive_static(cfg)
    b = PL.derive_static(cfg, variant="B")
    assert AB.DEFAULT_VARIANT == "A"
    assert a.checks["spawners.registered"] == b.checks["spawners.registered"] == 716


def test_derive_static_rejects_bad_options(cfg):
    with pytest.raises(ValueError):
        PL.derive_static(cfg, evolution_tables="regenerate")
    with pytest.raises(ValueError):
        PL.derive_static(cfg, armor_sync="Universal")


@pytest.mark.perf
def test_derive_static_time(db_path):
    cfg = C.load_game_config(db_path)
    t = time.perf_counter()
    PL.derive_static(cfg)
    assert time.perf_counter() - t <= 1.0


def test_checks_are_plain_python(shipped):
    json.dumps(shipped.checks)
    assert not any(isinstance(v, np.generic) for v in shipped.checks.values())


# ---------------------------------------------------------------------------------------------
# Review fixes: robustness, checks diff, exception warnings, fields without a static effect


def test_rules_without_a_value_become_warnings(cfg):
    """A patch that applies cleanly but leaves a rule without a value (evolution islands past island 99, a
    too-short IslandTargetDays) gives derive_error and None cells instead of raising."""
    patch = P.parse_patch({"name": "x", "overrides": [
        {"node": f"CombatBalanceConfig.EvolutionIslandTimingRows[].E{k}", "where": {}, "mul": 2} for k in range(11, 16)]})
    res = PL.run(cfg, patch)
    (w,) = [w for w in res.warnings if w["code"] == "derive_error"]
    bad = {tuple(e["key"]) for e in w["errors"] if e["table"] == "evolution_target_days"}
    rows = res.patched.rows_by_key("evolution_target_days")
    assert bad and all(rows[(kind, rarity, k)]["derived"] is None for kind, rarity in bad for k in range(1, 16))
    short = P.parse_patch({"name": "s", "overrides": [
        {"node": PL.ITD, "set": list(cfg.value(PL.ITD)[:10])}]})
    res = PL.run(cfg, short)
    errors = next(w for w in res.warnings if w["code"] == "derive_error")["errors"]
    assert {e["table"] for e in errors} >= {"relative_windows", "evolution_target_days"}
    assert len(res.patched.tables["island_target_days"]) == 10


def test_checks_diff(cfg):
    """Fields only the identity checks see (Mythical values: G10) show in checks_diff, with not_modeled_static."""
    res = PL.run(cfg, P.parse_patch({"name": "m", "overrides": [
        {"node": "CombatBalanceConfig.MythicalEvolutionValues[].Value", "index": 0, "mul": 1.5}]}))
    assert res.diff == {} and res.checks_diff["G10.value"] == {"base": 15, "patched": 14}
    assert {w["code"] for w in res.warnings} == {"not_modeled_static"}
    assert PL.diff_checks(res.base, res.base) == {}


def test_no_static_effect_warning(cfg):
    res = PL.run(cfg, P.load_patch(CASES / "18_not_modeled_static.yaml"))
    assert res.diff == {} and res.checks_diff == {}
    assert [w["code"] for w in res.warnings] == ["not_modeled_static", "no_static_effect"]


def test_exception_warnings_follow_recomputed_values(cfg, shipped):
    """A global input (all curves Use=false) moves the recomputed bake of exception spawners although none of
    their own leaves changes: each kind of exception warns, with the moved cells as the reason."""
    res = PL.run(cfg, P.parse_patch({"name": "u", "overrides": [
        {"node": "CombatBalanceConfig.Interpolations.Interpolations[].Use", "where": {}, "set": False}]}))
    moved = {c["key"][0] for c in res.diff["bake"] if c["field"] in PL.BAKE_OUTPUTS}
    kinds = PL.exception_spawners(shipped)
    assert kinds["hand_balance"] == {r["spawner"] for r in shipped.tables["bake"] if r["hand_balance"]}
    assert kinds["ot_exception"] == set(shipped.checks["G03.exceptions"])
    assert len(kinds["bake_exception"]) == DESIGN["G13.minute_n"] - DESIGN["G13.minute_hp_exact"] - 1  # 1 HandBalance
    warned = {w["code"]: w["spawners"] for w in res.warnings if w["code"].endswith("_input_changed")}
    for kind, ids in kinds.items():
        hit = ids & moved
        assert set(warned.get(f"{kind}_input_changed", {})) == hit, kind
        assert all(r.startswith("bake.") for s in hit for r in warned[f"{kind}_input_changed"][s])
    assert warned["hand_balance_input_changed"] and warned["ot_exception_input_changed"]


def test_run_flags_exceptions_against_the_base_config(cfg, shipped):
    """run() derives the patched side with exceptions_from=base: a spawner the patch moves off its rule is an
    exception now but not in the base."""
    regular = next(r for r in shipped.tables["optimum_time"] if not r["exception"] and r["rule"] is not None)
    scene, file_id = regular["spawner"].rsplit(":", 1)
    res = PL.run(cfg, P.parse_patch({"name": "x", "overrides": [
        {"node": "EnemySpawnerConfig.OptimumTime", "where": {"scene": scene, "file_id": int(file_id)}, "add": 1}]}))
    rows = res.patched.rows_by_key("optimum_time")
    assert rows[(regular["spawner"],)]["exception"] and not rows[(regular["spawner"],)]["exception_in_base"]
    base_exc = set(shipped.checks["G03.exceptions"])
    assert {k[0] for k, r in rows.items() if r["exception_in_base"]} == base_exc


def _perturbation(cfg: C.GameConfig, f: C.FieldSpec) -> dict | None:
    t = cfg.tables[f.sql]
    sel = {} if t.spec.singleton else {"where": {}}
    if f.node in P.POINT_LISTS:
        r = next(r for r in t.rows if int(r["Type"]) in C.STATIC_INTERPOLATION_TYPES)
        parts = str(r[f.column]).split(";")
        parts[-1] = repr(float(parts[-1]) + 1)
        return {"node": f.node, "index": r["Interpolations.Interpolations#idx"], "set": ";".join(parts)}
    if f.is_array and not any(f.decode(r) for r in t.rows):
        return None  # empty in the shipped config (e.g. the AB charges overrides)
    first = f.decode(t.rows[0])
    if f.type in ("float", "double", "int"):
        return {"node": f.node, **sel, "add": 1}
    if f.type == "bool":
        return {"node": f.node, **sel, "set": not first}
    if f.type == "enum":
        return {"node": f.node, **sel, "set": next(int(m) for m in C.ENUMS[f.enum] if int(m) not in (0, first))}
    if f.type == "ref":
        return {"node": f.node, **sel, "set": next(n for n in sorted(P._ref_names(cfg, f)) if n != first)}
    return {"node": f.node, **sel, "set": "probe"}


@pytest.mark.slow
def test_every_modeled_field_moves_a_table_or_warns(cfg, shipped):
    """Design 6.1: a field derive_static does not react to warns. Every patchable field, perturbed on all rows
    (frozen evolution tables): a table or check moves, or the patch warns not_modeled_static."""
    silent, unmodeled_moving = [], []
    for f in C.patchable_fields():
        ov = _perturbation(cfg, f)
        if ov is None:
            continue
        out, report = P.apply(cfg, P.parse_patch({"name": "p", "overrides": [ov]}))
        assert report.leaves, f.node
        patched = PL.derive_static(out, exceptions_from=cfg)
        tables = PL.diff_static(shipped, patched)
        warned = any(w.code == "not_modeled_static" for w in report.warnings)
        if not tables and not PL.diff_checks(shipped, patched) and not warned:
            silent.append(f.node)
        if tables and all(not f.modeled_for(r) for r in cfg.tables[f.sql].rows):  # checks may still move
            unmodeled_moving.append(f.node)
    assert silent == [] and unmodeled_moving == []
