"""Differential probes DP1-DP7: the game's C# formulas against the Python oracles (design 3.4, 7.3 C2).

One ``empty`` job with ``probes`` runs DP1-DP5 in edit mode before Play and DP3, DP6, DP7 in Play
(``tufu_balance.unity.probes``). The data anchors are asserted on the C# output: DP3 -> G11's K2/K3 on
the 109 snapshots, DP4 -> G19's day sequence, DP5 -> G13's 79 exact spawners, DP6 -> G17b, DP7 -> G18.
Golden suite ``differential``.
The input replay and the comparisons' own fault-injection test need no Unity: tests/test_probes.py.
"""

from __future__ import annotations

import json
import math

import pytest

from tufu_balance.golden import check, exact, same_set
from tufu_balance.model import bake as B
from tufu_balance.model import data as MD
from tufu_balance.model.interpolation import Rounding
from tufu_balance.model.timeaxis import trimmed_time_chunk_days
from tufu_balance.unity import probes as P
from tufu_balance.unity import request as req

SUITE = "differential"


# ---------------------------------------------------------------- with Unity

@pytest.fixture(scope="module")
def probe_run(unity_run):
    result = unity_run(req.make_request([P.probe_job("probes")]))
    assert result.exit_code == 0, result.describe()
    return result


@pytest.fixture(scope="module")
def outputs(probe_run):
    job = probe_run.job_dir("probes")
    manifest = probe_run.manifest("probes")
    edit = json.loads((job / P.EDIT_FILE).read_text())
    play = json.loads((job / P.RUNTIME_FILE).read_text())
    return {"edit": edit, "play": play, "manifest": manifest, "inputs": P.load_inputs()}


@pytest.fixture(scope="module")
def dp3(outputs, db):
    return P.compare_dp3(outputs["edit"]["dp3"], outputs["inputs"]["dp3"], MD.load_interpolations(db))


@pytest.fixture(scope="module")
def editor_rounding(dp3) -> Rounding:
    (variant,) = dp3["variants_bit_equal"]
    return Rounding(variant)


@pytest.mark.unity
def test_probe_files_and_manifest(outputs):
    m, edit, play = outputs["manifest"], outputs["edit"], outputs["play"]
    sha = P.input_ref()[1]
    assert edit["schema"] == play["schema"] == P.SCHEMA
    assert edit["mode"] == "edit" and play["mode"] == "play"
    assert edit["input_sha256"] == play["input_sha256"] == m["probes"]["input_sha256"] == sha
    assert m["probes"]["runtime"]["probes"] == list(P.PLAY_PROBES)
    assert m["exit_code"] == 0 and m["stop_reason"] == "max_frames"


@pytest.mark.unity
def test_dp1_specific_value_curves(outputs):
    r = P.compare_dp1(outputs["edit"]["dp1"], outputs["inputs"]["dp1"])
    assert r["mismatches"] == []
    check(SUITE, {
        "DP1.values": exact(r["values"], source="DP1", note="36 StatTypes x (200 log points + 17 specials)"),
        "DP1.mismatches": exact(r["values"] - min(r["specific_equal"], r["clamped_equal"]), source="DP1", original=0),
    })


@pytest.mark.unity
def test_dp2_defence_inverse_regen(outputs):
    r = P.compare_dp2(outputs["edit"]["dp2"], outputs["inputs"]["dp2"])
    assert r["mismatches"] == []
    check(SUITE, {
        f"DP2.{k}": exact([r[k]["n"], r[k]["n"] - r[k]["equal"]], source="DP2", note="[values, mismatches]",
                          original=[r[k]["n"], 0])
        for k in ("apply", "solve", "regen", "defence")
    })


@pytest.mark.unity
def test_dp3_editor_rounding(outputs, dp3, editor_rounding):
    """The editor's Mono equals exactly one of the four roundings, in edit mode and in Play."""
    assert outputs["play"]["dp3"] == outputs["edit"]["dp3"]
    results = {
        "DP3.editor_rounding": exact(editor_rounding.value, source="DP3",
                                     note="the only rounding bit-equal to the editor on every curve and coefficient"),
        "DP3.play_equals_edit": exact(True, source="DP3", original=True),
        "DP3.points": exact([dp3["points"], dp3["main_points"], dp3["curves"]], source="DP3",
                            note="t values (801 integers + 1000 fractional + 7 specials), main points, curves"),
    }
    for r, per in dp3["by_rounding"].items():
        results[f"DP3.ulp_hist.{r}"] = exact({"curves": per["curve_hist"], "coef": per["coef_hist"],
                                              "specials_equal": per["special_equal"]}, source="DP3",
                                             note="editor minus oracle in binary32 ulps over the 1801 main points")
    check(SUITE, results)


@pytest.mark.unity
def test_dp3_anchor_k2_k3_on_the_csharp_values(outputs, db):
    sd = MD.load_simulation_data(db)
    g = P.g11_on_csharp(sd, P.k2k3_from_csharp(outputs["edit"]["dp3"], outputs["inputs"]["dp3"]))
    assert g["n"] == 109
    check(SUITE, {
        "DP3.anchor.k2_snapshots": exact(g["k2"], source="DP3/G11", better="higher", original=109),
        "DP3.anchor.k3_snapshots": exact(g["k3"], source="DP3/G11", better="higher", original=109),
        "DP3.anchor.failing_snapshots": same_set(g["failing_snapshots"], source="DP3/G11", better="subset", original=[]),
    })


@pytest.mark.unity
def test_dp4_snapshot_lookups(outputs, db):
    """Equal except TotalPlaytime of interpolated snapshots, which the editor rounds once (Mono)."""
    sd = MD.load_simulation_data(db)
    r = P.compare_dp4(outputs["edit"]["dp4"], outputs["inputs"]["dp4"], sd)
    unexplained = [m for m in r["mismatches"] if m["fields"] != ["tp"]]
    assert unexplained == []
    assert r["tp_explained_by_mono"] == r["mismatch_count"]
    check(SUITE, {
        "DP4.lookups": exact({k: r[k] for k in ("tc", "tc_early", "playtime")}, source="DP4",
                             note="n and bit-equal results per lookup"),
        "DP4.mismatches": exact(r["mismatch_count"], source="DP4", better="lower",
                                note="all TotalPlaytime of interpolated snapshots (Mono rounds a + (b - a) * t once)"),
        "DP4.tp_ulp_hist": exact(r["tp_ulp_hist"], source="DP4", note="editor minus oracle, binary32 ulps (Q03)"),
        "DP4.tp_explained_by_mono": exact(r["tp_explained_by_mono"], source="DP4"),
    })


@pytest.mark.unity
def test_dp4_anchor_g19_day_sequence(outputs, db):
    sd = MD.load_simulation_data(db)
    g = P.g19_on_csharp(outputs["edit"]["dp4"], outputs["inputs"]["dp4"], sd)
    assert g["days"] == [s.time_chunk_index for s in sd.time_chunk_snapshots] == trimmed_time_chunk_days(405)
    assert g["locations_equal"]
    check(SUITE, {
        "DP4.anchor.g19_days": exact(g["days"] == trimmed_time_chunk_days(405), source="DP4/G19", original=True),
        "DP4.anchor.g19_count": exact(len(g["days"]), source="DP4/G19", original=46),
    })


@pytest.fixture(scope="module")
def dp5(outputs, db, editor_rounding):
    """Compared under the rounding DP3 measured (the editor's K4/K5)."""
    return P.compare_dp5(outputs["edit"]["dp5"], db, rounding=editor_rounding)


@pytest.mark.unity
def test_dp5_bake_report(dp5, db):
    r = dp5
    assert r["unknown"] == [] and r["missing"] == [] and r["errors_both"] == []
    assert r["csharp_spawners"] == 717 and r["registered"] == 716 and r["unregistered"] == ["Scenes/Main.unity:57694"]
    assert r["new_bits_equal"] == 717
    # every other difference is NewHealthRaw, where the editor never rounds (double)(1f - usedResist)
    assert all(m["new_ok"] and m["fields"] == ["NewHealthRaw"] for m in r["mismatches"])
    assert r["new_health_raw_explained_by_mono"] == len(r["mismatches"])
    assert [b["name"] for b in r["boss_tower"]] == ["BossTower"]
    boss = r["boss_tower"][0]
    assert math.isinf(boss["csharp_new_health"]) and math.isinf(boss["oracle_new_health"]) and boss["count"] == 0
    check(SUITE, {
        "DP5.spawners": exact([r["csharp_spawners"], r["registered"]], source="DP5", original=[717, 716],
                              note="[components in Main, registered at runtime]"),
        "DP5.unregistered": same_set(r["unregistered"], source="DP5", note="compared too, listed apart"),
        "DP5.new_health_damage_bit_equal": exact(r["new_bits_equal"], source="DP5", better="higher", original=717),
        "DP5.intermediates": exact([r["intermediate_values"], r["intermediate_bits_equal"], r["intermediate_rel_ok"]],
                                   source="DP5", note="[values, bit-equal, within 1e-12 relative]"),
        "DP5.new_health_raw_mono": exact([r["new_health_raw_explained_by_mono"], r["new_health_raw_rel_diff_max"]],
                                         source="DP5", note="NewHealthRaw off by the float rounding of 1 - resist (Q03)"),
        "DP5.snapshot_source_equal": exact(r["snapshot_source_equal"], source="DP5", original=717),
        "DP5.boss_tower": exact([{k: (repr(v) if isinstance(v, float) else v) for k, v in b.items()} for b in r["boss_tower"]],
                                source="DP5", note="Count 0: NewHealthRaw and NewHealth are +Inf on both sides"),
    })


@pytest.mark.unity
def test_dp5_anchor_g13_exact_set(dp5, db):
    """G13's 79 minute-mode spawners whose stored Health equals the bake, on the C# report."""
    ctx = MD.load_bake_context(db, rounding=Rounding.STEPWISE)
    armors = MD.load_armor_configs(db)
    oracle = sorted(f"{sp.spawner_id[0]}:{sp.spawner_id[1]}" for sp in
                    (MD.spawner_input_from_row(r, armors) for r in MD.load_spawner_rows(db))
                    if not sp.is_days and sp.health_base == B.bake_spawner(ctx, sp).new_health)
    assert dp5["hp_exact_minute"] == oracle
    check(SUITE, {
        "DP5.anchor.g13_minute_n": exact(dp5["minute_n"], source="DP5/G13", original=108),
        "DP5.anchor.g13_hp_exact": exact(len(dp5["hp_exact_minute"]), source="DP5/G13", better="higher", original=79),
        "DP5.anchor.g13_hp_exact_set": same_set(dp5["hp_exact_minute"], source="DP5/G13", better="superset",
                                                note="SpawnerId scene:file_id; equals the oracle's G13 set"),
    })


@pytest.mark.unity
def test_dp6_spawner_runtime_values(outputs, db):
    r = P.compare_dp6(outputs["play"]["dp6"], db)
    assert r["unknown"] == [] and r["missing"] == [] and r["duplicates"] == [] and r["mismatches"] == []
    check(SUITE, {
        "DP6.registered_equal": exact([r["rows"], r["equal"]], source="DP6", original=[716, 716],
                                      note="ChargesTotal, BaseCooldown (bits), GetSpawnerChargesLimit, AB predicate"),
    })


@pytest.mark.unity
def test_dp6_anchor_g17b(outputs):
    g = P.g17b_on_csharp(outputs["play"]["dp6"])
    assert g["spawner_minutes_agree"]
    check(SUITE, {
        "DP6.anchor.item_spawners": exact(g["item_spawners"], source="DP6/G17b", original=127),
        "DP6.anchor.item_charges_by_rarity": same_set(
            g["item_charges_by_rarity"], source="DP6/G17b",
            original=[["Common", 8, 39], ["Common", 0, 1], ["Uncommon", 6, 30], ["Rare", 4, 24],
                      ["Epic", 2, 21], ["Legendary", 1, 12]]),
        "DP6.anchor.non_elite_cooldown_minutes": exact(g["non_elite_cooldown_minutes"], source="DP6/G17b",
                                                       original=list(range(4, 29, 2))),
        "DP6.anchor.elite_cooldown_minutes": exact(g["elite_cooldown_minutes"], source="DP6/G17b",
                                                   original=[60, 90, 120, 150]),
        "DP6.anchor.cooldown_defaults": exact(g["cooldown_defaults"], source="DP6/G17b"),
        "DP6.anchor.charges_per_unit": exact(g["charges_per_unit"], source="DP6/G17b"),
        "DP6.anchor.fast_rpm_A": exact(g["fast_rpm"], source="DP6/G17b", original=[10, 1]),
    })


@pytest.mark.unity
def test_dp7_item_values(outputs, db):
    """Equal except where the editor keeps CalcDamage/CalcArmor's float sum unrounded (Mono)."""
    r = P.compare_dp7(outputs["play"]["dp7"], outputs["inputs"]["dp7"], db)
    assert r["missing_keys"] == []
    assert r["explained_by_mono"] == r["mismatch_count"]
    check(SUITE, {
        "DP7.values": exact([r["samples"], r["values"], r["equal"], r["errors_both"]], source="DP7",
                            note="[samples, values, equal, equal as both-throw]"),
        "DP7.mismatches": exact(r["mismatch_count"], source="DP7", better="lower",
                                note="all explained by the editor's unrounded float sum (Q03)"),
        "DP7.mismatch_columns": exact(r["mismatch_columns"], source="DP7"),
        "DP7.ulp_hist": exact(r["ulp_hist"], source="DP7", note="editor minus oracle in binary32 ulps of the oracle"),
    })


@pytest.mark.unity
def test_dp7_anchor_g18(outputs, db):
    g = P.g18_on_csharp(outputs["play"]["dp7"], outputs["inputs"]["dp7"], db)
    check(SUITE, {
        "DP7.anchor.g18_n": exact(g["n"], source="DP7/G18", original=675,
                                  note="design 3.4 says 690 (pet data); 675 on the pet-free data, as G18"),
        "DP7.anchor.g18_min_3dp": exact(g["min_3dp"], source="DP7/G18", original=1.022),
        "DP7.anchor.g18_median_3dp": exact(g["median_3dp"], source="DP7/G18", original=1.235),
        "DP7.anchor.g18_max": exact(g["max"], source="DP7/G18", original=8.4),
        "DP7.anchor.g18_argmax": exact(g["argmax"], source="DP7/G18",
                                       original={"key": "paddle", "evolution_level": 1, "before": 2500.0, "after": 21000.0}),
        "DP7.anchor.g18_ladder_sha256": exact(g["ladder_sha256"], source="DP7/G18",
                                              note="[key, E, shards, before, after] with the C# values"),
    })
