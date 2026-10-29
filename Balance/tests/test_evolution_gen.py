"""P5: evolution value generator and the Reference* columns (design 3.3 G07-G10, G12, R3-R5; 7.3)."""

from __future__ import annotations

import math
import statistics
from fractions import Fraction

import numpy as np
import pytest

from tufu_balance.golden import at_least, at_most, check, exact, same_set
from tufu_balance.model import data as MD
from tufu_balance.model import evolution_gen as G
from tufu_balance.model import items as I
from tufu_balance.model.registry import REGISTRY

SUITE = "evolution_gen"


@pytest.fixture(scope="module")
def cfg(db):
    return G.load_evolution_config(db)


@pytest.fixture(scope="module")
def days(db):
    return MD.load_simulation_data(db).time_chunk_snapshots


@pytest.fixture(scope="module")
def states(cfg, days):
    inferred = I.infer_evolved_snapshots(days, cfg.items, cfg.tables)
    return G.item_states(days, inferred)


@pytest.fixture(scope="module")
def stage4(load_fixture):
    return load_fixture("stage4_summaries")["summaries"]["growth"]


def _label(key) -> str:
    kind, t, r, lv = key
    return f"{kind}|{G.WeaponType(t).name}|{G.RARITY_NAMES[r]}|E{lv}"


# --------------------------------------------------------------------------------------- registry, config


def test_formulas_registered():
    ids = {"balance.evolution_value_autobalance", "balance.copy_value_rule", "balance.mythical_evolution_curve",
           "balance.evolution_shards_capture", "balance.evolution_nice_rounding",
           "balance.evolution_reference_stat_percent", "balance.evolution_fill_remaining",
           "balance.evolution_replay_stored_inputs", "balance.evolution_data_identities",
           "balance.evolution_reference_stat_value", "balance.evolution_reference_time_chunk",
           "balance.evolution_heuristic_fit", "balance.evolution_fitted_h"}
    for fid in ids:
        assert any(i.module == G.__name__ for i in REGISTRY.implementers(fid)), fid


def test_config_shape(cfg):
    assert len(cfg.rows) == 720 and cfg.levers.max_evolution_level == 15
    assert cfg.levers == G.EvolutionLevers(1.0, 0.5, 2.0, 1.3, 15, True)
    assert len(list(G.simulated_rows(cfg))) == 185  # 95 weapon + 90 armor
    assert cfg.start("weapon", G.WeaponType.Stabbing, G.Rarity.Common) == (0.0, 8.0)
    assert cfg.start("armor", G.WeaponType.Cutting, G.Rarity.Rare) == (27000.0, 350.0)
    assert cfg.weapon_cooldown(G.WeaponType.Buckshot, G.Rarity.Epic) == float(np.float32(2.3))


# --------------------------------------------------------------------------------------- nice()


def test_nice_grid_and_ties():
    assert [G.nice(x) for x in (3.74, 3.76, 4.2, 4.3, 7.8, 8.4, 9.6, 0.0, -3.0)] == \
        [3.7, 3.8, 4.0, 4.5, 8.0, 8.0, 10.0, 0.0, 0.0]
    assert G.nice(105825.7) == 110000.0 and G.nice(73.8) == 75.0 and G.nice(3225.0) == 3200.0 and G.nice(1700.0) == 1700.0
    # Ties go to even multiples of the step; "up" is half away from zero.
    assert G.nice(12_500_000) == 12_000_000 and G.nice(12_500_000, tie="up") == 13_000_000
    assert G.nice(85_000_000) == 80_000_000 and G.nice(42_500_000) == 40_000_000
    assert G.nice(225_000) == 220_000 and G.nice(235_000) == 240_000
    assert G.nice_is_tie(225_000) and not G.nice_is_tie(225_001)
    # Exact decade boundaries and binary values close to them.
    assert G.nice(1000.0) == 1000.0 and G.nice(math.nextafter(1000.0, 0)) == 1000.0
    assert G.nice_step(math.nextafter(4.0, 0)) == Fraction(1, 10) and G.nice_step(4.0) == Fraction(1, 2)
    assert G.nice(12_500_000, tie="down") == 12_000_000 and G.nice(13_500_000, tie="down") == 13_000_000
    assert G.nice(13_500_000) == 14_000_000
    with pytest.raises(ValueError):
        G.nice(1.0, tie="odd")


def _stage4_nice(x: float) -> float:
    """Stage 4's float64 implementation (analysis/item_growth.py)."""
    e = 10.0 ** math.floor(math.log10(x))
    st = 0.1 if x / e < 4 else (0.5 if x / e < 8 else 1.0)
    return float(round(x / (st * e)) * (st * e))


def _grid_round(x: float, steps) -> float:
    """Round half-even on an alternative grid: ``steps`` = (step below 4, [4, 8), from 8) as mantissa units."""
    xf = Fraction(x)
    e = G._decade(xf)
    m = xf / Fraction(10) ** e
    st = steps[0] if m < 4 else steps[1] if m < 8 else steps[2]
    step = Fraction(st) * Fraction(10) ** e
    return float(round(xf / step) * step)


def test_nice_rule_proven_on_data(cfg):
    """The grid and the tie rule against the 450 generated rows: every stored Value and CopyValue lies on the
    grid; rounding acts on most targets; only half-to-even reproduces the 4 ties; other grids fail;
    stage 4's float64 nice() and binary32 arithmetic give the same rows."""
    gen = G.replay_stored_inputs(cfg)
    rows = [cfg.row(*k) for k in gen]
    on_grid_v = sum(G.is_nice(x.value) for x in rows)
    on_grid_cv = sum(G.is_nice(x.copy_value) for x in rows)
    acted = [k for k, g in gen.items() if G.nice(g.target) != g.target]
    ties = sorted(_label(k) for k, g in gen.items() if G.nice_is_tie(g.target))
    tie_rows = [k for k in gen if G.nice_is_tie(gen[k].target)]
    tie_hits = {tie: sum(float(cfg.row(*k).value) == G.nice(gen[k].target, tie=tie) for k in tie_rows)
                for tie in G.TIES}
    # A tie has to be exact in either arithmetic, or the tie rule would not be what decides it.
    b32 = G.replay_stored_inputs(cfg, binary32=True)
    assert all(b32[k].target == gen[k].target for k in gen if G.nice_is_tie(gen[k].target))
    assert G.compare_generated(cfg, b32) == G.compare_generated(cfg, gen)
    assert all(_stage4_nice(g.target) == G.nice(g.target) for g in gen.values())
    grids = {"two_digits": (Fraction(1, 10),) * 3, "half_steps_from_4_only": (Fraction(1, 10), Fraction(1, 2), Fraction(1, 2)),
             "unit_steps_from_4": (Fraction(1, 10), Fraction(1), Fraction(1)),
             "half_steps_everywhere": (Fraction(1, 2),) * 3}
    grid_hits = {name: sum(float(cfg.row(*k).value) == _grid_round(g.target, st) for k, g in gen.items())
                 for name, st in grids.items()}
    assert max(grid_hits.values()) < 450
    check(SUITE, {
        "nice.values_on_grid": exact(on_grid_v, source="G07", note="450 generated rows"),
        "nice.copy_values_on_grid": exact(on_grid_cv, source="G07"),
        "nice.targets_moved_by_rounding": exact(len(acted), source="G07",
                                                note="rows where nice(target) != target: the rounding is tested there"),
        "nice.ties": same_set(ties, source="G07",
                              note="midpoint targets; ItemGrowth.md 3.1 lists the 4 where half-up differs"),
        "nice.tie_rule_hits": exact(tie_hits, source="G07", note="only half-to-even gets every midpoint"),
        "nice.alternative_grids_value_hits": exact(grid_hits, source="G07", note="ablation: other grids fail"),
    })


def test_nice_ties_match_stage4(cfg, stage4):
    gen = G.replay_stored_inputs(cfg)
    ties = sorted([k[0], G.WeaponType(k[1]).name, G.RARITY_NAMES[k[2]], k[3], int(cfg.row(*k).value)]
                  for k, g in gen.items() if G.nice_is_tie(g.target) and G.nice(g.target, tie="up") != G.nice(g.target))
    assert ties == sorted(stage4["generator"]["nice_rounding_ties"])


# --------------------------------------------------------------------------------------- G07


def _flat(c: dict) -> dict:
    return {k: v for k, v in c.items() if not k.endswith("_rows")}


def test_g07_replay_stored_inputs(cfg):
    one = lambda **o: _flat(G.compare_generated(cfg, G.replay_stored_inputs(cfg, previous="stored", **o)))  # noqa: E731
    chain = lambda **o: _flat(G.compare_generated(cfg, G.replay_stored_inputs(cfg, **o)))  # noqa: E731
    full = chain()
    full_one = one()
    no_prev, half_up, no_min = one(armor_prev=False), one(tie="up"), one(min_constraints=False)
    rows = G.compare_generated(cfg, G.replay_stored_inputs(cfg))
    assert rows["weapon_rows"] == rows["armor_rows"] == 225
    check(SUITE, {
        "G07.weapon_value": exact(full["weapon_value"], source="G07", original=225),
        "G07.weapon_copy_value": exact(full["weapon_copy_value"], source="G07", original=225),
        "G07.armor_value": exact(full["armor_value"], source="G07", original=225),
        "G07.armor_copy_value": exact(full["armor_copy_value"], source="G07", original=225),
        "G07.one_step": exact(full_one, source="G07", original={"weapon_value": 225, "weapon_copy_value": 225,
                                                                  "armor_value": 225, "armor_copy_value": 225},
                              note="each level fed the stored previous row (stage 4's replay)"),
        "G07.binary32_arithmetic": exact(chain(binary32=True), source="G07",
                                         note="floors and copy share in float instead of double: same rows"),
        "G07.ablation_no_armor_prev.armor_value": exact(no_prev["armor_value"], source="G07", original=111),
        "G07.ablation_half_up.weapon_value": exact(half_up["weapon_value"], source="G07", original=221),
        "G07.ablation_no_min.weapon_value": exact(no_min["weapon_value"], source="G07", original=80),
        "G07.ablation_no_min.armor_value": exact(no_min["armor_value"], source="G07", original=63),
        "G07.ablations_one_step": exact({"no_armor_prev": no_prev, "half_up": half_up, "no_min": no_min},
                                        source="G07"),
        "G07.ablations_chained": exact({"no_armor_prev": chain(armor_prev=False), "half_up": chain(tie="up"),
                                        "no_min": chain(min_constraints=False)}, source="G07",
                                       note="errors propagate through the recursion"),
    })


def test_g07_bindings(cfg):
    """Which term set Value and CopyValue (ItemGrowth.md 3.1 tables)."""
    gen = G.replay_stored_inputs(cfg)
    vb, cb = {}, {}
    for k, g in gen.items():
        tag = f"{k[0]}|{'filled' if cfg.row(*k).is_filled else 'simulated'}"
        vb[f"{tag}|{g.binding}"] = vb.get(f"{tag}|{g.binding}", 0) + 1
        cb[f"{tag}|{g.copy_binding}"] = cb.get(f"{tag}|{g.copy_binding}", 0) + 1
    # ItemGrowth.md: weapons 79 H / 16 shards (simulated), 130 shards (filled); armor 48 / 42 / 0 and 0 / 18 / 117.
    assert vb == {"weapon|simulated|heuristic": 79, "weapon|simulated|shards_min": 16, "weapon|filled|shards_min": 130,
                  "armor|simulated|heuristic": 48, "armor|simulated|shards_min": 42,
                  "armor|filled|shards_min": 18, "armor|filled|armor_prev": 117}
    check(SUITE, {"G07.value_bindings": exact(dict(sorted(vb.items())), source="G07"),
                  "G07.copy_value_bindings": exact(dict(sorted(cb.items())), source="G07")})


def test_generate_level_examples(cfg):
    lev = cfg.levers
    # Cutting Uncommon E2: H 105825.7 is below the shards floor; CopyValue from H is 73.8, floored at 120.
    g = G.generate_level(32000.0, 120.0, 105825.7, 500, False, lev)
    assert (g.value, g.copy_value, g.binding, g.copy_binding) == (150000.0, 120.0, "shards_min", "floor")
    assert g.copy_raw == pytest.approx(73.8257)
    # Armor Epic E1 from the pre-evolution item: CopyValue 0.5 * (967646 - 310000) / 190 -> 1700.
    g = G.generate_level(310000.0, 500.0, 967646.2, 190, True, lev)
    assert g.copy_value == 1700.0
    with pytest.raises(ValueError):
        G.generate_level(1.0, 1.0, 1.0, 0, False, lev)
    with pytest.raises(ValueError):
        G.generate_series("weapon", (0.0, 8.0), [None], [100], lev)


# --------------------------------------------------------------------------------------- G08


def test_g08_reference_stat_percent(cfg, stage4):
    rows = list(G.replay_stored_inputs(cfg))
    rows = [cfg.row(*k) for k in rows]
    even = sum(G.reference_stat_percent(x.value, x.reference_stat_value) == x.reference_stat_percent for x in rows)
    up = sum(G.reference_stat_percent(x.value, x.reference_stat_value, tie="up") == x.reference_stat_percent
             for x in rows)
    ties = sorted(_label(x.key) for x in rows
                  if (100.0 * float(x.value) / float(x.reference_stat_value)) % 1 == 0.5)
    assert len(ties) == len(stage4["generator"]["ReferenceStatPercent"]["exact_.5_ties"])
    check(SUITE, {
        "G08.half_even": exact(even, source="G08", original=450),
        "G08.ablation_half_up": exact(up, source="G08", original=439),
        "G08.ties": same_set(ties, source="G08", note="rows with 100*Value/sv exactly .5 (ItemGrowth.md: 18)"),
    })


# --------------------------------------------------------------------------------------- G09, G10


def test_g09_data_identities(cfg):
    d = G.data_identities(cfg)
    s = d["armor_sync"]
    check(SUITE, {
        "G09.filled_rows": exact(d["filled_rows"], source="G09", original=265),
        "G09.filled_h_eq_last_value": exact(d["filled_h_eq_last_value"], source="G09", original=265),
        "G09.filled_copy_value_frozen": exact(d["filled_copy_value_frozen"], source="G09", original=265),
        "G09.armor_sync_cells": exact(d["armor_sync_cells"], source="G09", original=75),
        "G09.armor_sync_value": exact(s["value"], source="G09", original=75),
        "G09.armor_sync_copy_value": exact(s["copy_value"], source="G09", original=75),
        "G09.armor_sync_heuristic": exact(s["reference_heuristic_value"], source="G09", original=75),
        "G09.armor_sync_shards": exact(s["reference_shards_requirement"], source="G09", original=75),
        "G09.armor_sync_time_chunk": exact(s["reference_time_chunk_index"], source="G09", original=48),
        "G09.armor_sync_stat_value": exact(s["reference_stat_value"], source="G09", original=48),
        "G09.universal_rows": exact(d["universal_rows"], source="G09", original=150),
        "G09.universal_placeholders": exact(d["universal_placeholders"], source="G09", original=150,
                                            note="Value 1, CopyValue 0, every Reference* column 0"),
        "G09.reference_shards_rows": exact(d["reference_shards_rows"], source="G09", original=570),
        "G09.reference_shards_eq_table": exact(d["reference_shards_eq_table"], source="G09", original=570),
    })


def test_g10_mythical(cfg):
    m = G.data_identities(cfg)["mythical"]
    assert m["rows"] == 15
    check(SUITE, {
        "G10.value": exact(m["value"], source="G10", original=15),
        "G10.copies_to_evolve": exact(m["copies_to_evolve"], source="G10", original=15),
        "G10.copy_value": exact(m["copy_value"], source="G10", original=15),
        "G10.copied_rows": exact(m["copied_rows_equal"], source="G10", original=120),
        "G10.shards_rows": exact(m["shards_rows_equal"], source="G10", original=30),
        "G10.on_nice_grid": exact(m["on_nice_grid"], source="G10", original=11),
        "G10.off_grid": exact(m["off_grid"], source="G10", note="[L, Value, nice(Value)]: L = 11..14"),
        "G10.copy_value_eq_share_of_increment": exact(m["copy_value_eq_share_of_increment"], source="G10",
                                                      note="consistency only (ItemGrowth.md 3.4)"),
    })


# --------------------------------------------------------------------------------------- G12


def test_g12_reference_stat_value(cfg, days):
    rc = G.reference_stat_checks(cfg, days)
    at = rc["at_snapshot_days"]
    check(SUITE, {
        "G12.rows_at_snapshot_days": exact(len(at), source="G12", original=16),
        "G12.rel_lt_1e-6": exact(sum(x["rel"] < 1e-6 for x in at), source="G12", original=16),
        "G12.binary32_equal": exact(sum(x["binary32_equal"] for x in at), source="G12",
                                    note="(float)TotalValue == ReferenceStatValue bit for bit"),
        "G12.bracket_checked": exact(rc["bracket_checked"], source="G12", original=162),
        "G12.bracket_inside": exact(rc["bracket_inside"], source="G12", original=157),
        "G12.bracket_outside_in_other_type": exact(rc["bracket_outside_in_other_type"], source="G12", original=5),
        "G12.bracket_outside": same_set([[x["kind"], x["type"], x["tc"], x["fits_bracket_of"]]
                                         for x in rc["bracket_outside"]], source="G12"),
        "G12.groups": exact(rc["groups"], source="G12", original=178),
        "G12.groups_single_valued": exact(rc["groups_single_valued"], source="G12", original=178),
    })


# --------------------------------------------------------------------------------------- R3


def test_r3_shards_capture(cfg, days, states):
    sc = G.shards_capture_checks(cfg, days, states)
    w, a, b = sc["weapon"], sc["armor"], sc["best"]
    check(SUITE, {
        "R3.weapon_levels": exact(w["with_candidate"], source="R3", original=35),
        "R3.weapon_within_5pct": at_least(w["within_5pct"], source="R3", original=28),
        "R3.weapon_within_15pct": at_least(w["within_15pct"], source="R3", original=34),
        "R3.armor_levels": exact(a["with_candidate"], source="R3", original=30),
        "R3.armor_within_5pct": at_least(a["within_5pct"], source="R3", original=23),
        "R3.armor_within_15pct": at_least(a["within_15pct"], source="R3", original=29),
        "R3.best_levels": exact(b["with_candidate"], source="R3", original=65),
        "R3.best_within_5pct": at_least(b["within_5pct"], source="R3", original=59),
        "R3.best_nice_equal": at_least(b["nice_equal"], source="R3", note="ItemGrowth.md 5: 52"),
        "R3.plateau_levels": exact(sc["plateau_levels"], source="R3", original=85),
        "R3.plateau_equal": exact(sc["plateau_equal"], source="R3", original=85),
    })


def test_r3_matches_stage4(cfg, days, states, stage4):
    """Same numbers as stage 4, whose evolved flags came from the Level != Copies heuristic (P4's
    ``infer_evolved`` agrees on every candidate)."""
    sc = G.shards_capture_checks(cfg, days, states)
    t = stage4["shards"]["capture_single_reference_tests"]
    for kind in G.KINDS:
        s4 = t[f"{kind}|any_type|max_copies"]["abs_rel_err"]
        # Stage 4 read the YAML decimal text as double, the model reads the binary32 fields: 6th decimal may differ.
        assert abs(sc[kind]["median_abs"] - s4["median"]) <= 1.5e-6 and abs(sc[kind]["max_abs"] - s4["max"]) <= 1.5e-6
    s4 = stage4["shards"]["capture_hypothesis"]
    assert (sc["best"]["within_5pct"], sc["best"]["within_15pct"], sc["best"]["nice_equal"]) == \
        (s4["|rel_err|<=5%"], s4["|rel_err|<=15%"], s4["nice(interp)==shards"])


# --------------------------------------------------------------------------------------- R4


def _r6(x: float) -> float:
    return round(x, 6)


def test_r4_heuristic_fit(cfg, stage4):
    """Fit quality. The design rounds the stage-4 numbers to 2 significant digits (0.0012, 0.18%, 0.43%,
    [0.803, 0.809]); the originals here are the stage-4 values at 6 decimals (fixture stage4_summaries)."""
    fit = G.fit_heuristic(cfg)
    res = G.heuristic_residuals(cfg, fit)
    env = [r["eps"] for r in res if r["envelope"]]
    late = [r["eps"] for r in res if not r["envelope"] and r["tc"] >= 200]
    s4 = stage4["reference"]["weapon_heuristic_fit"]
    for t, k in fit.type_coefficient.items():
        assert abs(k - s4["k_t(geo-mean of min ratio / M_r)"][G.WeaponType(t).name]) <= 1.5e-6
    on_time = [float(x.reference_heuristic_value) / float(x.reference_stat_value) for x in G.simulated_rows(cfg)
               if x.kind == "armor" and float(cfg.target_timing("armor", x.rarity, x.evolution_level)) >= 150
               and G.evolved_on_time(x, cfg.target_timing("armor", x.rarity, x.evolution_level))]
    assert abs(fit.armor_coefficient - stage4["reference"]["armor_hv_over_sv_on_time_rows"]["stats_T_ge_150"]["median"]) <= 1.5e-6
    check(SUITE, {
        "R4.envelope_rows": exact(len(env), source="R4", original=15),
        "R4.envelope_eps_max": at_most(_r6(max(env)), source="R4", original=0.00121, note="design: +-0.0012"),
        "R4.envelope_eps_min": at_least(_r6(min(env)), source="R4", original=-0.00097, note="design: +-0.0012"),
        "R4.late_out_of_sample_n": exact(len(late), source="R4", original=17),
        "R4.late_out_of_sample_eps_median": at_most(_r6(statistics.median(late)), source="R4", original=0.001823,
                                                    note="design: 0.18%"),
        "R4.late_out_of_sample_eps_max": at_most(_r6(max(late)), source="R4", original=0.004316,
                                                 note="design: 0.43%"),
        "R4.armor_on_time_t150_n": exact(len(on_time), source="R4", original=20),
        "R4.armor_on_time_t150_min": at_least(_r6(min(on_time)), source="R4", original=0.802894,
                                              note="design: 0.803"),
        "R4.armor_on_time_t150_max": at_most(_r6(max(on_time)), source="R4", original=0.808902,
                                             note="design: 0.809"),
        "R4.fit": exact({"k_t": {G.WeaponType(t).name: k for t, k in fit.type_coefficient.items()},
                         "M_r": list(fit.rarity_multiplier), "k_armor": fit.armor_coefficient}, source="R4"),
    })


def _value_hits(cfg, gen, kind, lo=None, hi=None):
    rows = [x for x in G.simulated_rows(cfg) if x.kind == kind
            and (lo is None or x.reference_time_chunk_index >= lo) and (hi is None or x.reference_time_chunk_index < hi)]
    return sum(bool(gen[x.key].stored[0] == x.value) for x in rows), len(rows)


def test_r4_fitted_h_reproduces_value(cfg):
    """``fitted_h``: the generator with the fitted H, each level fed the stored previous row (the design's
    numbers), and the chained recursion (whole regenerated table, reported)."""
    one = G.fitted_h(cfg, previous="stored")
    chained = G.fitted_h(cfg, previous="generated")
    w, a = _value_hits(cfg, one, "weapon"), _value_hits(cfg, one, "armor")
    w200, w50, w0 = (_value_hits(cfg, one, "weapon", 200), _value_hits(cfg, one, "weapon", 50, 200),
                     _value_hits(cfg, one, "weapon", None, 50))
    a150 = _value_hits(cfg, one, "armor", 150)
    assert (w[1], w200[1], w50[1], w0[1], a[1], a150[1]) == (95, 32, 45, 18, 90, 51)
    check(SUITE, {
        "R4.weapon_value": at_least(w[0], source="R4", original=72),
        "R4.weapon_value_tc200": at_least(w200[0], source="R4", original=32),
        "R4.weapon_value_tc50_199": at_least(w50[0], source="R4", original=34),
        "R4.weapon_value_tc_lt50": at_least(w0[0], source="R4", original=6),
        "R4.armor_value": at_least(a[0], source="R4", original=43),
        "R4.armor_value_tc150": at_least(a150[0], source="R4", original=24),
        "R4.one_step_all_rows": at_least(sum(_flat(G.compare_generated(cfg, one)).values()), source="R4",
                                         note="Value + CopyValue hits over all 450 rows, filled ones included"),
        "R4.chained": exact(_flat(G.compare_generated(cfg, chained)), source="R4",
                            note="whole regenerated table (evolution_tables mode fitted_h)"),
    })


# --------------------------------------------------------------------------------------- R5


def test_r5_on_time(cfg):
    c = G.on_time_counts(cfg)
    want = {"weapon|Stabbing": (35, 29), "weapon|Cutting": (30, 9), "weapon|Buckshot": (30, 6),
            "armor|Stabbing": (30, 15), "armor|Cutting": (30, 10), "armor|Buckshot": (30, 6)}
    keys = {}
    for k, (n, on) in want.items():
        keys[f"R5.{k}.rows"] = exact(c[k]["rows"], source="R5", original=n)
        keys[f"R5.{k}.on_time"] = exact(c[k]["on_time"], source="R5", original=on)
    keys["R5.early"] = exact(sum(v["early"] for v in c.values()), source="R5", original=0,
                             note="tc is never before ceil(T)")
    check(SUITE, keys)


# --------------------------------------------------------------------------------------- whole tables


def test_evolution_tables_modes(cfg):
    frozen = G.evolution_tables(cfg)
    assert frozen == list(cfg.rows)
    replay = G.evolution_tables(cfg, "replay_stored_inputs")
    assert [x.key for x in replay] == [x.key for x in frozen]
    assert replay == frozen  # Value, CopyValue, ReferenceStatPercent and Mythical all regenerated exactly
    fitted = G.evolution_tables(cfg, "fitted_h")
    same = sum(a == b for a, b in zip(fitted, frozen))
    assert 0 < same < len(frozen)
    with pytest.raises(ValueError):
        G.evolution_tables(cfg, "regenerate")
    check(SUITE, {"tables.fitted_h_rows_equal": exact(same, source="R4", note="of 720 rows, chained")})


def test_reference_stat_type():
    assert G.reference_stat_type("weapon", G.WeaponType.Buckshot) == G.StatType.BuckshotDamageFlat
    assert G.reference_stat_type("armor", G.WeaponType.Stabbing) == G.StatType.StabDefenceFlat
    with pytest.raises(KeyError):
        G.reference_stat_type("weapon", G.WeaponType.Universal)
