"""model.timeline: island table, rel -> OT, evolution schedule, interpolation knots, amulet (G01-G06)."""

from __future__ import annotations

import inspect
import math
import struct
from types import SimpleNamespace

import numpy as np
import pytest

from tufu_balance.golden import at_most, check, exact, same_set
from tufu_balance.model import timeline as tl
from tufu_balance.model.f32 import f32

SUITE = "timeline"
SCENE = "Scenes/Main.unity"
# G03: spawners that neither rel -> OT nor OT -> rel reproduces (IslandTimeline.md 2.3), by file_id.
G03_EXCEPTIONS = {
    58941: "Spawner_elite_HP", 60557: "second_biome_door", 56670: "Spawner_1 (10)", 59878: "Spawner_1 (11)",
    55497: "Spawner_1 (12)", 58145: "Spawner_1 (5)", 56447: "tree_1 (16)", 56574: "tree_1 (21)",
    55816: "tree_1 (22)", 59596: "tree_1 (23)", 55751: "tree_1 (26)", 59922: "tree_1 (27)",
    57815: "Spawner_1_epic_buck", 57718: "Spawner_elite_regen", 55927: "Spawner_superboss_legendary_pierce",
    58041: "Spawner_Crystal",
}


def _sid(scene: str, file_id: int) -> str:
    return f"{scene}:{file_id}"


def _array(db, field: str) -> list:
    return [r["value"] for r in db.execute(
        "SELECT value FROM config_arrays WHERE class = 'CombatBalanceConfig' AND field_path = ? ORDER BY idx",
        (field,))]


@pytest.fixture(scope="module")
def data(db):
    itd = [f32(v) for v in _array(db, "IslandTargetDays")]
    spawners = [dict(r) for r in db.execute("SELECT * FROM spawners ORDER BY scene, file_id")]
    tool = {r["LocationNumber"]: dict(r) for r in db.execute(
        'SELECT * FROM s_SpawnerRelativeOptimumTimeTool__Islands ORDER BY "Islands#idx"')}
    fill = [dict(r) for r in db.execute('SELECT * FROM "c_CombatBalanceConfig__EvolutionIslandFillRuleRows" '
                                        'ORDER BY "EvolutionIslandFillRuleRows#idx"')]
    timing = [dict(r) for r in db.execute('SELECT * FROM "c_CombatBalanceConfig__EvolutionIslandTimingRows" '
                                          'ORDER BY "EvolutionIslandTimingRows#idx"')]
    interps = [dict(r) for r in db.execute('SELECT * FROM "c_CombatBalanceConfig__Interpolations.Interpolations" '
                                           'ORDER BY "Interpolations.Interpolations#idx"')]
    amulet = dict(db.execute("SELECT * FROM c_AmuletConfig").fetchone())
    raw = bytes.fromhex(amulet["LevelTargetDays"])
    level_target_days = list(struct.unpack(f"<{len(raw) // 4}i", raw))
    return SimpleNamespace(itd=itd, spawners=spawners, tool=tool, fill=fill, timing=timing, interps=interps,
                           amulet=amulet, level_target_days=level_target_days, db=db)


def _apply(s, tool, **kw):
    t = tool[s["LocationNumber"]]
    return tl.optimum_time_from_relative(s["RelativeOptimumTime"], t["MinOptimumTime"], t["MaxOptimumTime"],
                                         bool(s["IsDays"]), **kw)


# ---------------------------------------------------------------------------------------------
# Golden: G01-G06


def test_g01_island_table_tail(data):
    itd = data.itd
    hits = [i for i in range(tl.TAIL_FIRST_ISLAND, len(itd) + 1) if tl.island_target_days_closed_form(i) == itd[i - 1]]
    first = min(i for i in range(1, len(itd) + 1)
                if all(tl.island_target_days_closed_form(j) == itd[j - 1] for j in range(i, len(itd) + 1)))
    regenerated = tl.generate_island_target_days(itd[:tl.TAIL_FIRST_ISLAND - 1], len(itd))
    check(SUITE, {
        "G01.n_islands": exact(len(itd), source="G01", original=99),
        "G01.tail_f32_exact": exact(len(hits), source="G01", better="higher", original=84,
                                    note="islands 16..99: D(i) = (5/6)(i+1)(i+2) - 105, float32"),
        "G01.tail_first_island": exact(first, source="G01", original=16,
                                       note="island 15 is hand-set (130 vs 121.67 by the closed form)"),
        "G01.regenerated_table_f32_exact": exact(sum(a == b for a, b in zip(regenerated, itd)), source="G01",
                                                 better="higher", original=99,
                                                 note="15 hand values + closed-form tail"),
    })


def test_g02_gate_ot_equals_island_table(data):
    itd = data.itd
    gated = [s for s in data.spawners if (s["Gates#count"] or 0) > 0]
    from_gates = tl.island_target_days_from_gates(
        (s["LocationNumber"], s["OptimumTime"], bool(s["IsDays"])) for s in gated)
    hits = [L for L, days in from_gates.items() if days == itd[L - 1]]
    # the gate defining each island's value sits at the top of the window (rel = 1)
    top = {}
    for s in gated:
        days = f32(s["OptimumTime"]) if s["IsDays"] else f32(s["OptimumTime"]) / f32(60.0)
        if s["LocationNumber"] not in top or days > top[s["LocationNumber"]][0]:
            top[s["LocationNumber"]] = (days, s)
    rel1 = sum(1 for _, s in top.values() if s["RelativeOptimumTime"] == 1.0)
    check(SUITE, {
        "G02.gated_islands": exact(sorted(from_gates), source="G02", original=list(range(1, 17))),
        "G02.gate_ot_eq_itd": exact(len(hits), source="G02", better="higher", original=16,
                                    note="largest gate OT per island (minutes / 60 on islands 1..3) == ITD[L-1]"),
        "G02.gate_rel_eq_1": exact(rel1, source="G02", original=16, note="IslandTimeline.md 1.1"),
    })


def test_g03_relative_to_optimum_time(data):
    sp, tool, itd = data.spawners, data.tool, data.itd
    misses = {}
    for rounding in tl.ROUNDINGS:
        for precision in tl.PRECISIONS:
            misses[rounding, precision] = [s for s in sp if _apply(s, tool, rounding=rounding, precision=precision)
                                           != f32(s["OptimumTime"])]
    exceptions = [_sid(s["scene"], s["file_id"]) for s in misses["half_even", "f32"]]
    # exact .5 ties of the float32 product x*k; "discriminating" ties are where half-even and half-up differ
    ties = disc = disc_even = 0
    exc_ids = {s["file_id"] for s in misses["half_even", "f32"]}
    for s in sp:
        t = tool[s["LocationNumber"]]
        x = float(f32(f32(t["MinOptimumTime"]) + f32(s["RelativeOptimumTime"])
                      * (f32(t["MaxOptimumTime"]) - f32(t["MinOptimumTime"]))) * f32(10.0 if s["IsDays"] else 1.0))
        if x - math.floor(x) == 0.5:
            ties += 1
            if math.floor(x) % 2 == 0:
                disc += 1
                if s["file_id"] not in exc_ids:
                    disc_even += 1
    window_hits = sum(1 for L, t in tool.items() if tl.relative_window(itd, L) == (
        f32(t["MinOptimumTime"]), f32(t["MaxOptimumTime"]), bool(t["IsDays"])))
    fill_hits = sum(1 for s in sp if tl.relative_from_optimum_time(
        s["OptimumTime"], tool[s["LocationNumber"]]["MinOptimumTime"], tool[s["LocationNumber"]]["MaxOptimumTime"])
        == f32(s["RelativeOptimumTime"]))
    n = len(sp)
    check(SUITE, {
        "G03.n_spawners": exact(n, source="G03", original=717),
        "G03.rel_to_ot_hits": exact(n - len(misses["half_even", "f32"]), source="G03", better="higher", original=701,
                                    note="float32, Mathf.Round (ties to even), step 0.1 day / 1 minute"),
        "G03.exceptions": same_set(exceptions, source="G03", better="subset",
                                   original=[_sid(SCENE, f) for f in G03_EXCEPTIONS],
                                   note="(scene, file_id) of the 16 spawners off the rule"),
        "G03.ablation_half_up": exact(n - len(misses["half_up", "f32"]), source="G03", original=682,
                                      note="ablation: Round half up instead of half to even"),
        "G03.ablation_f64_half_even": exact(n - len(misses["half_even", "f64"]), source="G03",
                                            note="ablation: double arithmetic on the float fields (stage 4 used "
                                                 "decimal-text doubles and got 701; with float inputs ties vanish)"),
        "G03.ablation_f64_half_up": exact(n - len(misses["half_up", "f64"]), source="G03",
                                          note="ablation: double arithmetic, Round half up"),
        "G03.ties_f32": exact(ties, source="G03", original=38, note="IslandTimeline.md 2.2 / revision 3"),
        "G03.ties_discriminating": exact(disc, source="G03", original=27),
        "G03.ties_discriminating_outside_exceptions_even": exact(
            disc_even, source="G03", better="higher", original=19,
            note="every discriminating tie outside the exception set resolves to even"),
        "G03.window_from_itd_eq_tool": exact(window_hits, source="G03", better="higher", original=17,
                                             note="islands 4..17 [ITD[L-2], ITD[L-1]] (14/14) + minute islands "
                                                  "1..3 (Max = 60 ITD[L-1], Min 0/35/120)"),
        "G03.fill_ot_to_rel_hits": exact(fill_hits, source="G03",
                                         note="reverse rule, exact float32 Round(x*100)/100; stage 4 counted 656 "
                                              "with a +-0.005 tolerance that accepts the 3 exact .x25 ties"),
    })


def test_g04_evolution_schedule(data):
    itd = data.itd
    island_hits = day_hits = cells = 0
    max_err = 0.0
    for fr, tr in zip(data.fill, data.timing, strict=True):
        assert fr["Rarity"] == tr["Rarity"] and fr["Name"] == tr["Name"]
        stored_islands = [f32(tr[f"E{k}"]) for k in range(1, tl.EVOLUTION_LEVELS + 1)]
        island_hits += sum(a == b for a, b in zip(tl.evolution_islands(fr["StartValue"], fr["StepPerEvolution"]),
                                                  stored_islands, strict=True))
        kind = "Armor" if fr["Name"].startswith("Armor") else ""
        stored_days = [f32(v) for v in _array(data.db, f"{fr['Rarity__name']}{kind}EvolutionTargetTimings")]
        days = tl.evolution_target_timings(stored_islands, itd)
        day_hits += sum(a == b for a, b in zip(days, stored_days, strict=True))
        max_err = max(max_err, *(abs(float(a) - float(b)) for a, b in zip(days, stored_days)))
        cells += len(stored_days)
    check(SUITE, {
        "G04.cells": exact(cells, source="G04", original=150),
        "G04.fill_rule_island_exact": exact(island_hits, source="G04", better="higher", original=150,
                                            note="E_k = Start + (k-1) Step, float32"),
        "G04.target_timings_f32_exact": exact(day_hits, source="G04", better="higher", original=150,
                                              note="ConvertIslandTimingToDays(E_k) == *EvolutionTargetTimings"),
    })
    assert max_err == 0.0


def test_g05_interpolation_knots(data):
    itd = data.itd
    knots = f32_hits = close = text_hits = curve_text_hits = 0
    max_err = 0.0
    for it in data.interps:
        islands = [float(x) for x in it["IslandPoints"].split(";")]
        stored_text = [x.strip() for x in it["TimingPoints"].split(";")]
        points = tl.interpolation_timing_points(islands, itd)
        for got, text in zip(points, stored_text, strict=True):
            err = abs(float(got) - float(f32(float(text))))
            knots += 1
            f32_hits += got == f32(float(text))
            close += err <= 5e-5
            text_hits += tl.format_points([got]) == text
            max_err = max(max_err, err)
        curve_text_hits += tl.format_points(points) == it["TimingPoints"]
    check(SUITE, {
        "G05.knots": exact(knots, source="G05", original=49),
        "G05.curves": exact(len(data.interps), source="G05", original=19),
        "G05.knots_le_5e-5": exact(close, source="G05", better="higher", original=49),
        "G05.knots_f32_exact": exact(f32_hits, source="G05", better="higher", original=45,
                                     note="the stored strings keep 7 significant digits"),
        "G05.max_abs_err_days": at_most(max_err, source="G05", original=3.0517578125e-05),
        "G05.knots_text_7_significant_digits": exact(
            text_hits, source="G05", better="higher",
            note="TimingPoints text == float32 result printed with 7 significant digits"),
        "G05.curves_text_exact": exact(curve_text_hits, source="G05", better="higher",
                                       note="whole TimingPoints string == '; '.join(7 significant digits)"),
    })


def test_g06_amulet_level_target_days(data):
    days = tl.amulet_level_target_days(data.itd)
    hits = sum(a == f32(b) for a, b in zip(days, data.level_target_days, strict=True))
    check(SUITE, {
        "G06.amulet_level_target_days_eq_island_start": exact(
            hits, source="G06", better="higher", original=7, note="islands 8, 9, 10, 12, 14, 16, 18"),
    })
    # UnlockLocationNumber 8 starts on day 20 (level 1); FeatureUnlockDay 16 is off the island grid.
    assert tl.island_start_day(data.itd, data.amulet["UnlockLocationNumber"]) == f32(data.level_target_days[0])
    assert tl.days_to_island_timing(data.amulet["FeatureUnlockDay"], data.itd) == pytest.approx(7 + 1 / 3)


def test_matches_stage4_fixtures(data, load_fixture):
    """The port reproduces the stage-4 numbers frozen by W0."""
    probe = load_fixture("bake_probe_counts")
    stage4 = load_fixture("stage4_summaries")["summaries"]["timeline"]
    assert probe["island_target_days_tail"]["n"] == 84 == probe["island_target_days_tail"]["f32_exact"]
    assert (probe["interpolation_knots"]["n"], probe["interpolation_knots"]["f32_exact"]) == (49, 45)
    assert probe["evolution_target_timings"]["f32_exact"] == 150
    assert {e["file_id"] for e in stage4["spawner_window"]["exceptions"]} == set(G03_EXCEPTIONS)
    rounding = stage4["spawner_window"]["rounding_2x2"]
    assert (rounding["f32_even"], rounding["f32_up"]) == (701, 682)
    ours = {s["file_id"] for s in data.spawners if _apply(s, data.tool) != f32(s["OptimumTime"])}
    assert ours == set(G03_EXCEPTIONS)
    assert [float(v) for v in tl.amulet_level_target_days(data.itd)] == \
        [float(v) for v in stage4["other_island_axis"]["amulet_LevelTargetDays"]]


# ---------------------------------------------------------------------------------------------
# Unit tests


@pytest.fixture(scope="module")
def itd(data):
    return data.itd


def test_convert_island_timing_to_days_values(itd):
    assert tl.convert_island_timing_to_days(1.0, itd) == 0.0
    assert tl.convert_island_timing_to_days(9.0, itd) == f32(26.0)  # start of island 9 = end of island 8
    assert tl.convert_island_timing_to_days(7.5, itd) == f32(17.0)  # Weapon Common E1
    assert tl.convert_island_timing_to_days(18.5, itd) == f32(195.83334)  # Armor Legendary E1
    assert tl.convert_island_timing_to_days(99.5, itd) == (f32(itd[97]) + f32(0.5) * (itd[98] - itd[97]))
    # end of the last island: the C# returns ITD[98] without reading ITD[99]
    assert tl.convert_island_timing_to_days(100.0, itd) == itd[98]
    for k in range(1, 100):
        assert tl.convert_island_timing_to_days(k, itd) == tl.island_start_day(itd, k)
    assert isinstance(tl.convert_island_timing_to_days(3.25, itd), np.float32)


@pytest.mark.parametrize("s", [0.0, 0.999, -1.0, -0.5, 100.5, 101.0, 250.0, math.nan, math.inf, -math.inf])
def test_convert_island_timing_to_days_out_of_range(itd, s):
    with pytest.raises(ValueError):
        tl.convert_island_timing_to_days(s, itd)


@pytest.mark.parametrize("island", [0, -1, 101, 1.0, True])
def test_island_bounds(itd, island):
    with pytest.raises(ValueError):
        tl.island_start_day(itd, island)
    with pytest.raises(ValueError):
        tl.relative_window(itd, island)


def test_relative_window_bounds(itd):
    assert tl.relative_window(itd, 1) == (0.0, 30.0, False)
    assert tl.relative_window(itd, 99) == (itd[97], itd[98], True)
    with pytest.raises(ValueError):
        tl.relative_window(itd, 100)
    with pytest.raises(ValueError):
        tl.island_target_days_closed_form(0)
    with pytest.raises(ValueError):
        tl.generate_island_target_days(itd, 98)


def test_closed_form_tail():
    assert tl.island_target_days_closed_form(16) == 150.0
    assert tl.island_target_days_closed_form(17) == 180.0
    assert tl.island_target_days_closed_form(18) == f32(211.66667)
    assert tl.island_target_days_closed_form(99) == f32(8311.667)
    for i in range(17, 100):  # increment (5/3)(i+1) days = 100(i+1) active minutes
        d = tl.island_target_days_closed_form(i) - np.float64(tl.island_target_days_closed_form(i - 1))
        assert d == pytest.approx(5 / 3 * (i + 1), abs=1e-3)


def test_f32_from_fraction_rounds_once():
    from fractions import Fraction
    # 1 + 2^-24 + 2^-60 rounds up in binary32, but double rounding (to 1 + 2^-24, then a tie to even) gives 1.0
    x = Fraction(1) + Fraction(1, 2 ** 24) + Fraction(1, 2 ** 60)
    assert np.float32(float(x)) == np.float32(1.0)
    assert tl._f32_from_fraction(x) == np.nextafter(np.float32(1.0), np.float32(2.0))
    assert tl._f32_from_fraction(Fraction(1) + Fraction(1, 2 ** 24)) == np.float32(1.0)  # exact tie -> even


def test_optimum_time_from_relative_ties():
    # island 12: 56 + 0.67 * 15 = 66.05 -> x*10 = 660.5 in float32: half-even keeps 66.0 (stored 66.0)
    assert tl.optimum_time_from_relative(0.67, 56.0, 71.0, True) == f32(66.0)
    assert tl.optimum_time_from_relative(0.67, 56.0, 71.0, True, rounding="half_up") == f32(66.1)
    assert tl.optimum_time_from_relative(0.67, 56.0, 71.0, True, precision="f64") == f32(66.1)
    assert tl.optimum_time_from_relative(1.0, 120.0, 240.0, False) == 240.0
    assert tl.optimum_time_from_relative(-0.56, 120.0, 240.0, False) == 53.0  # Spawner_vip_3: rel < 0
    with pytest.raises(ValueError):
        tl.optimum_time_from_relative(0.5, 0, 1, True, rounding="bankers")
    with pytest.raises(ValueError):
        tl.optimum_time_from_relative(0.5, 0, 1, True, precision="f16")
    assert tl.relative_from_optimum_time(66.0, 56.0, 71.0) == f32(0.67)


def test_gates_and_islands():
    gates = [(2, 110.0, False), (2, 90.0, False), (4, 7.0, True), (4, 6.2, True)]
    assert tl.island_target_days_from_gates(gates) == {2: f32(110.0) / f32(60.0), 4: f32(7.0)}
    assert tl.evolution_islands(7.5, 1.75)[:3] == [f32(7.5), f32(9.25), f32(11.0)]
    assert tl.evolution_islands(7.5, 1.75)[-1] == f32(32.0)


def test_days_to_island_timing_round_trip(itd):
    rng = np.random.default_rng(5)
    for s in rng.uniform(1.0, 99.99, 300):
        s = float(f32(s))
        assert tl.days_to_island_timing(float(tl.convert_island_timing_to_days(s, itd)), itd) == \
            pytest.approx(s, abs=1e-4)
    assert tl.days_to_island_timing(-3, itd) == 1.0
    assert tl.days_to_island_timing(1e6, itd) == 100.0
    with pytest.raises(ValueError):
        tl.days_to_island_timing(math.nan, itd)


def test_every_function_registers_a_formula():
    public = [obj for name, obj in vars(tl).items()
              if inspect.isfunction(obj) and obj.__module__ == tl.__name__ and not name.startswith("_")]
    assert len(public) >= 12
    missing = [f.__name__ for f in public if not getattr(f, "__implements__", ())]
    assert not missing, f"functions without @implements: {missing}"
    ids = {impl.canonical_id for f in public for impl in f.__implements__}
    assert {"balance.island_timing_to_days", "balance.island_target_days_curve", "spawners.relative_optimum_time",
            "balance.evolution_island_fill_rule", "balance.evolution_target_timings_from_islands",
            "balance.interpolation_points_from_islands", "powerups.amulet_level_target_days"} <= ids
    port = tl.convert_island_timing_to_days.__implements__[0]
    assert (port.provenance, port.va) == ("port", "0x157ad44") and port.code_ref.startswith("CodeBase/")
