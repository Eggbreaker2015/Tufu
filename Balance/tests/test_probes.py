"""Differential probes without Unity (unit C2): the committed input grids replay from the seed, and every
comparison of ``tufu_balance.unity.probes`` reports planted faults in outputs built from the oracles.
The Unity side is tests/unity/test_differential.py."""

from __future__ import annotations

import copy
import json

import pytest

from tufu_balance.model import data as MD
from tufu_balance.model import items as IT
from tufu_balance.model.defense import (apply_damage_defense, get_battle_regen, solve_attack_damage_for_incoming,
                                        weapon_type_to_defence)
from tufu_balance.model.interpolation import Rounding
from tufu_balance.model.stats import calculate_specific_value, clamp_specific_value
from tufu_balance.model.timeaxis import trimmed_time_chunk_days
from tufu_balance.unity import probes as P
from tufu_balance.unity import request as req


def test_inputs_replay_from_the_seed(db):
    """The committed grids are exactly what the generator gives for this database and seed."""
    data = P.dumps_inputs(P.generate_inputs(db))
    assert data == P.INPUT_FILE.read_bytes(), "regenerate with python -m tufu_balance.unity.probes --write"
    inputs = json.loads(data)
    assert inputs["seed"] == P.SEED
    assert len(P.floats(inputs["dp3"]["t"])) == P.DP3_INTEGERS + P.DP3_FRACTIONAL + 7
    job = P.probe_job("probes")
    req.validate(req.make_request([job], out_dir="/x"))
    assert job["probes"]["input"] == "Balance/tests/data/probes_in.json"


def _fake_snapshot(s, sd):
    e = P.encode_snapshot(s)
    stats = "".join(P._int_hex(k) + "".join(P.dhex(v) for v in rest) for k, *rest in e["stats"])
    pairs = lambda xs: "".join(P._int_hex(k) + P.dhex(v) for k, v in xs)  # noqa: E731
    return {**e, "stats": stats, "dps": pairs(e["dps"]), "def": pairs(e["def"])}


def _fake_outputs(db, inputs, rounding):
    """What a C# side that computes exactly like the oracles would write (edit + play merged)."""
    sd, interp = MD.load_simulation_data(db), MD.load_interpolations(db)
    x = P.doubles(inputs["dp1"]["x"])
    dp1 = {"specific": {str(t): "".join(P.dhex(calculate_specific_value(t, v)) for v in x) for t in inputs["dp1"]["types"]},
           "clamped": {str(t): "".join(P.dhex(clamp_specific_value(t, calculate_specific_value(t, v))) for v in x)
                       for t in inputs["dp1"]["types"]}}
    i2 = inputs["dp2"]
    a, d = P.doubles(i2["apply"]["attack"]), P.doubles(i2["apply"]["defense"])
    s = [P.doubles(i2["solve"][k]) for k in ("target", "defense", "percent")]
    r = [P.doubles(i2["regen"][k]) for k in ("health_regen", "lifesteal_flat", "difficulty")]
    dp2 = {"apply": "".join(P.dhex(apply_damage_defense(*p)) for p in zip(a, d)),
           "solve": "".join(P.dhex(solve_attack_damage_for_incoming(*p)) for p in zip(*s)),
           "regen": "".join(P.dhex(get_battle_regen(*p)) for p in zip(*r)),
           "defence": [[wt] + [P._err(lambda p=p: int(weapon_type_to_defence(wt, p))) for p in (False, True)]
                       for wt in i2["weapon_types"]]}
    t = P.floats(inputs["dp3"]["t"])
    dp3 = {"curves": [{"name": c.name, "type": c.type, "use": c.use,
                       "values": "".join(P.fhex(c.evaluate_rounded(v, rounding)) for v in t)} for c in interp.interpolations],
           "coef": {str(k): "".join(P.fhex(interp.total_coefficient(v, k, rounding=rounding)) for v in t)
                    for k in inputs["dp3"]["types"]},
           "coef_rarity": {f"{k}:{q}": "".join(P.fhex(interp.total_coefficient(v, k, q, rounding=rounding)) for v in t)
                           for k in inputs["dp3"]["types"] for q in inputs["dp3"]["rarities"]}}

    def snap(o, lst, tag, x):
        f = _fake_snapshot(o, sd)
        src, items_src = P._expected_source(o, x, lst, tag) if tag else (f"s:{o.source_index}",) * 2
        return {**f, "src": src, "items_src": items_src}

    i4 = inputs["dp4"]
    dp4 = {"tc": [snap(sd.get_by_time_chunk_index(x), sd.time_chunk_snapshots, "t", x) for x in P.floats(i4["tc"])],
           "tc_early": [snap(sd.get_by_time_chunk_index(x, sd.snapshots), sd.snapshots, "s", x) for x in P.floats(i4["tc_early"])],
           "playtime": [snap(sd.get_by_playtime(x), sd.snapshots, None, x) for x in P.floats(i4["playtime"])]}
    items, tables = IT.load_item_defs(db), IT.load_item_tables(db)
    dp7 = {"columns": ["damage", "armor", "dps", "levels_to_evolve"], "missing_keys": [], "rows": [
        [v if isinstance(v, str) else (str(v) if i == 3 else P.dhex(v)) for i, v in
         enumerate(P._oracle_item(items[k], IT.ItemState(bool(e), lv, c), tables))]
        for k, lv, c, e in inputs["dp7"]["samples"]]}
    return {"dp1": dp1, "dp2": dp2, "dp3": dp3, "dp4": dp4, "dp7": dp7}


def _flip(hexes: str, index: int, width: int) -> str:
    """Flip the lowest bit of value ``index`` in a concatenated hex grid."""
    v = int(hexes[index * width:(index + 1) * width], 16) ^ 1
    return hexes[:index * width] + format(v, f"0{width}x") + hexes[(index + 1) * width:]


@pytest.mark.slow
def test_comparisons_report_each_planted_fault(db):
    inputs = P.load_inputs()
    sd, interp = MD.load_simulation_data(db), MD.load_interpolations(db)
    fake = _fake_outputs(db, inputs, Rounding.DOUBLE_TAIL)
    assert P.compare_dp1(fake["dp1"], inputs["dp1"])["mismatches"] == []
    assert P.compare_dp2(fake["dp2"], inputs["dp2"])["mismatches"] == []
    r3 = P.compare_dp3(fake["dp3"], inputs["dp3"], interp)
    assert r3["variants_bit_equal"] == ["double_tail"]
    assert r3["by_rounding"]["fma"]["curve_hist"] != {"0": r3["main_points"] * r3["curves"]}
    assert P.compare_dp4(fake["dp4"], inputs["dp4"], sd)["mismatch_count"] == 0
    assert P.compare_dp7(fake["dp7"], inputs["dp7"], db)["mismatch_count"] == 0
    assert P.g11_on_csharp(sd, P.k2k3_from_csharp(fake["dp3"], inputs["dp3"]))["k2"] == 109
    g19 = P.g19_on_csharp(fake["dp4"], inputs["dp4"], sd)
    assert g19["days"] == trimmed_time_chunk_days(405) and g19["locations_equal"]

    bad = copy.deepcopy(fake)
    bad["dp1"]["clamped"]["9"] = _flip(bad["dp1"]["clamped"]["9"], 17, 16)
    bad["dp2"]["solve"] = _flip(bad["dp2"]["solve"], 5, 16)
    bad["dp2"]["defence"][2][2] = 99
    bad["dp3"]["curves"][4]["values"] = _flip(bad["dp3"]["curves"][4]["values"], 333, 8)
    bad["dp4"]["tc"][700]["tp"] = _flip(bad["dp4"]["tc"][700]["tp"], 0, 8)
    bad["dp4"]["playtime"][3]["src"] = "s:999"
    bad["dp7"]["rows"][40][2] = _flip(bad["dp7"]["rows"][40][2], 0, 16)
    r1 = P.compare_dp1(bad["dp1"], inputs["dp1"])
    assert (r1["values"] - r1["clamped_equal"], r1["values"] - r1["specific_equal"]) == (1, 0)
    r2 = P.compare_dp2(bad["dp2"], inputs["dp2"])
    assert [m["probe"] for m in r2["mismatches"]] == ["solve", "defence"]
    r3 = P.compare_dp3(bad["dp3"], inputs["dp3"], interp)
    assert r3["variants_bit_equal"] == [] and r3["by_rounding"]["double_tail"]["curve_equal"] == r3["main_points"] * r3["curves"] - 1
    r4 = P.compare_dp4(bad["dp4"], inputs["dp4"], sd)
    assert [(m["lookup"], m["fields"]) for m in r4["mismatches"]] == [("tc", ["tp"]), ("playtime", ["src"])]
    r7 = P.compare_dp7(bad["dp7"], inputs["dp7"], db)
    assert [(m["sample"], m["field"], m["explained_by_mono"]) for m in r7["mismatches"]] == [
        (inputs["dp7"]["samples"][40], "dps", False)]


def test_oracle_default_rounding_is_the_editors():
    """interpolation_default.json carries the rounding DP3 measured in the editor (design 3.6 item 1, Q32)."""
    from tufu_balance.golden import DEFAULT_STORE
    from tufu_balance.model import interpolation as I

    measured = DEFAULT_STORE.load("current", "differential")["DP3.editor_rounding"].value
    assert I.default_rounding() is Rounding(measured) is Rounding.DOUBLE_ALL
