"""P9: derive_static on the shipped config (golden suite ``pipeline``) and the static diff pipeline (design 6.3)."""

from __future__ import annotations

import json
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


def test_empty_patch_equals_direct_domain_calls(cfg, db):
    rt = _rewritten(cfg)
    assert len(rt.changed_tables()) == len(C.TABLES)
    got = PL.derive_static(rt)
    t = {name: got.rows_by_key(name) for name in got.tables}

    itd = MD.load_island_target_days(db)
    assert [t["island_target_days"][(i,)]["stored"] for i in range(1, 100)] == [float(x) for x in itd]
    assert [t["island_target_days"][(i,)]["closed_form"] for i in range(16, 100)] == \
        [float(TL.island_target_days_closed_form(i)) for i in range(16, 100)]

    tool = {r["LocationNumber"]: r for r in db.execute('SELECT * FROM s_SpawnerRelativeOptimumTimeTool__Islands')}
    for r in db.execute("SELECT * FROM spawners"):
        w = tool[r["LocationNumber"]]
        rule = TL.optimum_time_from_relative(r["RelativeOptimumTime"], w["MinOptimumTime"], w["MaxOptimumTime"],
                                             bool(r["IsDays"]))
        assert t["optimum_time"][(f"{r['scene']}:{r['file_id']}",)]["rule"] == float(rule)

    fill = [dict(r) for r in db.execute('SELECT * FROM "c_CombatBalanceConfig__EvolutionIslandFillRuleRows"')]
    for r in fill:
        for k, e in enumerate(TL.evolution_islands(r["StartValue"], r["StepPerEvolution"]), start=1):
            assert t["evolution_islands"][(r["Rarity"], r["Name"], k)]["fill_rule"] == float(e)
    for r in db.execute('SELECT * FROM "c_CombatBalanceConfig__EvolutionIslandTimingRows"'):
        kind = "armor" if r["Name"].startswith("Armor") else "weapon"
        days = TL.evolution_target_timings([r[f"E{k}"] for k in range(1, 16)], itd)
        assert [t["evolution_target_days"][(kind, r["Rarity"], k)]["derived"] for k in range(1, 16)] == \
            [float(x) for x in days]
    for r in db.execute('SELECT * FROM "c_CombatBalanceConfig__Interpolations.Interpolations"'):
        islands = [float(x) for x in r["IslandPoints"].split(";")]
        assert t["interpolation_points"][(r["Interpolations.Interpolations#idx"],)]["derived"] == \
            TL.format_points(TL.interpolation_timing_points(islands, itd))
    assert [t["amulet_level_days"][(k,)]["derived"] for k in range(1, 8)] == \
        [float(x) for x in TL.amulet_level_target_days(itd)]

    evo = EG.load_evolution_config(db)
    for row in evo.rows:
        g = t["evolution_values"][(row.kind, row.damage_type, row.rarity, row.evolution_level)]
        assert (g["value"], g["copy_value"], g["percent"]) == (float(row.value), float(row.copy_value),
                                                              row.reference_stat_percent)

    sd = MD.load_simulation_data(db)
    interp = MD.load_interpolations(db)
    for name, lst in (("time_chunk_snapshots", sd.time_chunk_snapshots), ("snapshots", sd.snapshots)):
        for i, s in enumerate(lst):
            assert t["snapshot_composition"][(name, i)]["mismatches"] == len(S.composition_mismatches(s, interp))

    ctx = MD.load_bake_context(db)
    armors = MD.load_armor_configs(db)
    for r in MD.load_spawner_rows(db):
        sp = MD.spawner_input_from_row(r, armors)
        rep = B.bake_spawner(ctx, sp)
        g = t["bake"][(f"{r['scene']}:{r['file_id']}",)]
        assert (g["recalc_optimum_time"], g["day"], g["new_health"], g["new_damage"], g["ability_dps"]) == \
            (rep.recalc_optimum_time, rep.day, rep.new_health, rep.new_damage, rep.ability_dps)

    ready = SP.expected_ready_spawners(con=db)
    assert {k: (v["charges_total"], v["base_cooldown"], v["charges_limit"]) for k, v in t["spawner_runtime"].items()} == \
        {(f"{s}:{f}",): (r.charges_total, r.base_cooldown, r.charges_limit) for (s, f), r in ready.items()}
    bosses = SP.bosses_by_location(SP.load_spawners(db))
    assert {k[0]: v["spawner"] for k, v in t["bosses"].items()} == \
        {n: (f"{b.scene}:{b.file_id}" if b else None) for n, b in bosses.items()}

    items = IT.load_item_defs(db)
    jumps = IT.evolve_jumps(IT.g18_items(items), IT.load_item_tables(db))
    assert {(j.key, j.evolution_level): j.ratio for j in jumps} == \
        {k: v["ratio"] for k, v in t["item_evolve_jumps"].items()}

    # the rewritten view and the untouched source give the same tables and checks
    plain = PL.derive_static(cfg)
    assert got.tables == plain.tables and got.checks == plain.checks


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
