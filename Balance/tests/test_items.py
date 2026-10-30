"""P4: item damage / armor, evolution transition, G18 / G18b, ``infer_evolved`` (design 3.3, 7.3)."""

from __future__ import annotations

import hashlib
import json

import pytest

from tufu_balance.golden import check, exact, same_set
from tufu_balance.model import data as MD
from tufu_balance.model import items as I
from tufu_balance.model.registry import REGISTRY


@pytest.fixture(scope="module")
def cfg(db):
    return I.load_item_defs(db), I.load_item_tables(db)


@pytest.fixture(scope="module")
def inferred(db, cfg):
    items, tables = cfg
    return I.infer_evolved_snapshots(MD.load_simulation_data(db).time_chunk_snapshots, items, tables)


def _sha(obj) -> str:
    return hashlib.sha256(json.dumps(obj, sort_keys=True, separators=(",", ":")).encode()).hexdigest()


# --------------------------------------------------------------------------------------- formulas


def test_formulas_registered():
    ids = {"items.weapon_damage_pre_evolution", "items.weapon_damage_evolved", "items.armor_pre_evolution",
           "items.armor_evolved", "items.effective_level", "items.evolution_level_clamp", "items.evolution_cost",
           "items.evolve_transition", "items.weapon_dps"}
    for fid in ids:
        assert any(i.module == I.__name__ for i in REGISTRY.implementers(fid)), fid


def test_unevolved_and_evolved_values(cfg):
    items, tables = cfg
    knife = items["knife"]  # Cutting Common: growth 12, base 0
    st = I.ItemState(False, 1, 7)
    assert st.effective_level == 7 and st.to_snapshot() == (7, 7)
    assert I.damage(knife, st, tables) == 84.0
    assert I.dps(knife, st, tables) == 84.0 / float(knife.attack_cooldown)
    ev = I.ItemState(True, 9, 665)
    row = tables.evolution_row(False, knife.damage_type, knife.rarity, 9)
    assert I.damage(knife, ev, tables) == float(row.value + row.copy_value * I.f32(665) + knife.flat_damage)
    assert I.ItemState.from_snapshot(9, 665, True) == ev
    with pytest.raises(ValueError):
        I.ItemState.from_snapshot(3, 5, False)
    # Level above MaxEvolutionLevel reads the last row (GetExistingEvolutionLevel clamps above only).
    assert I.damage(knife, I.ItemState(True, 40, 0), tables) == I.damage(knife, I.ItemState(True, 15, 0), tables)


def test_divine_lookups_throw(cfg):
    items, tables = cfg
    div = next(it for it in items.values() if it.key.endswith("_div"))
    I.item_value(div, I.ItemState(False, 1, 3), tables)  # pre-evolution needs no table row
    with pytest.raises(I.MissingTableRow):
        I.item_value(div, I.ItemState(True, 1, 0), tables)
    with pytest.raises(I.MissingTableRow):
        I.levels_to_evolve(div, I.ItemState(False, 1, 3), tables)


def test_evolve_transition(cfg):
    """``items.evolve_transition``: Copies' = Copies - LevelsToEvolve; first evolution sets Evolved and keeps
    Level 1, later ones increment Level; the cost is Shards[min(NextEvolutionLevel, Max)]."""
    items, tables = cfg
    for key in ("knife", "iron_block", "graveeye_visor", "runekeeper_face"):
        it = items[key]
        s = [None] + [tables.shards_requirement(it.is_armor, it.rarity, e) for e in range(1, 16)]
        st = I.ItemState(False, 1, s[1] + 5)
        assert I.levels_to_evolve(it, st, tables) == s[1] and I.can_evolve(it, st, tables)
        st = I.evolve(it, st, tables)
        assert st == I.ItemState(True, 1, 5)
        for e in range(2, 16):
            st = I.ItemState(True, st.level, st.copies + s[e])
            assert I.levels_to_evolve(it, st, tables) == s[e]
            st = I.evolve(it, st, tables)
            assert st == I.ItemState(True, e, 5)
        st = I.ItemState(True, 15, s[15])
        assert I.evolve(it, st, tables) == I.ItemState(True, 16, 0)  # cost clamps to row 15
        with pytest.raises(I.CannotEvolve):
            I.evolve(it, I.ItemState(False, 1, s[1] - 1), tables)


# --------------------------------------------------------------------------------------- G18 / G18b


def test_g18_evolve_jump_ratios(cfg):
    items, tables = cfg
    jumps = I.evolve_jumps(I.g18_items(items), tables)
    st = I.jump_stats(jumps)
    ladder = [[j.key, j.evolution_level, j.shards, j.before, j.after] for j in jumps]
    check("items", {
        "G18.n": exact(st["n"], source="G18", original=675,
                       note="design 3.3 says 690: counted on pet data (player_claw); 675 on the pet-free data (W0)"),
        "G18.min_3dp": exact(round(st["min"], 3), source="G18", original=1.022),
        "G18.median_3dp": exact(round(st["median"], 3), source="G18", original=1.235),
        "G18.max": exact(st["max"], source="G18", original=8.4),
        "G18.argmax": exact(st["argmax"], source="G18",
                            original={"key": "paddle", "evolution_level": 1, "before": 2500.0, "after": 21000.0}),
        "G18.min": exact(st["min"], source="G18"),
        "G18.median": exact(st["median"], source="G18"),
        "G18.mean": exact(st["mean"], source="G18", note="stage-4 summary 1.41853 (6 dp)"),
        "G18.ladder_sha256": exact(_sha(ladder), source="G18",
                                   note="sha256 of [key, E, shards, before, after] for all 675 jumps"),
    })


def test_g18_matches_stage4_summary(cfg, load_fixture):
    items, tables = cfg
    jumps = I.evolve_jumps(I.g18_items(items), tables)
    st = I.jump_stats(jumps)
    s4 = load_fixture("stage4_summaries")["summaries"]["growth"]["runtime"]["evolve_jump_ratio_at_threshold"]
    assert s4["n"] == st["n"]
    for k in ("min", "median", "max"):
        assert s4[k] == round(st[k], 6), k
    # stage 4 averaged the ratios after rounding each to 6 decimals
    assert s4["mean"] == round(sum(round(j.ratio, 6) for j in jumps) / len(jumps), 6)


def test_g18b_table_coverage(cfg):
    items, tables = cfg
    cov = I.table_coverage(items, tables)
    missing = cov["missing_value_rows"]
    check("items", {
        "G18b.configs_without_rows": exact(len(missing), source="G18b", original=39),
        "G18b.all_without_rows_are_div": exact(all(k.endswith("_div") and items[k].rarity == I.Rarity.Divine
                                                   for k in missing), source="G18b", original=True),
        "G18b.shard_rows_missing_same_set": exact(cov["missing_shard_rows"] == missing, source="G18b"),
        "G18b.configs_without_rows_set": same_set(missing, source="G18b"),
        "G18b.magic_items": exact(len(cov["magic_items"]), source="G18b", original=0),
    })


# --------------------------------------------------------------------------------------- snapshot values


def test_values_equal_fixture(cfg, load_fixture):
    items, tables = cfg
    fx = load_fixture("item_growth_values")
    n, bad = 0, []
    for snap in fx["snapshots"]:
        for it in snap["items"]:
            item = items[it["Key"]]
            assert item.kind == it["kind"]
            st = I.ItemState.from_snapshot(it["Level"], it["Copies"], it["evolved"])
            v = I.item_value(item, st, tables)
            n += 1
            if v != it["value"]:
                bad.append((snap["TimeChunkIndex"], it["Key"], v, it["value"]))
    assert not bad, bad[:10]
    check("items", {
        "fixture.item_values_compared": exact(n, source="P4", note="46 day snapshots, item_growth_values.json"),
        "fixture.item_values_mismatch": exact(len(bad), source="P4", original=0),
    })


def test_infer_evolved_on_day_snapshots(inferred, load_fixture):
    assert len(inferred) == 46
    counts = I.inference_counts(inferred)
    fx = load_fixture("item_growth_values")
    heuristic = {(s["TimeChunkIndex"], it["Key"]): it["evolved"] for s in fx["snapshots"] for it in s["items"]}
    assert set(heuristic) == {(t, k) for t, per in inferred.items() for k in per}
    disagree = sorted(f"TC{t}:{k}" for (t, k), ev in heuristic.items() if inferred[t][k].evolved is not ev)
    undetermined = sorted(f"TC{t}:{k}:{r.reason}" for t, per in inferred.items() for k, r in per.items()
                          if r.evolved is None)
    check("items", {
        "infer_evolved.evolved": exact(counts["evolved"], source="P4"),
        "infer_evolved.unevolved": exact(counts["unevolved"], source="P4"),
        "infer_evolved.undetermined": exact(counts["undetermined"], source="P4", better="lower"),
        "infer_evolved.undetermined_set": same_set(undetermined, source="P4", better="subset"),
        "infer_evolved.disagree_level_ne_copies": same_set(
            disagree, source="P4", note="against the stage-4 heuristic (evolved from the first Level != Copies on)"),
    })


def test_infer_evolved_cases(cfg):
    items, tables = cfg
    knife = items["knife"]  # weapon Common: shards 100, 390, 800, ...

    def ev(obs):
        return [(r.evolved, r.reason) for r in I.infer_evolved(knife, tables, obs)]

    # Level != Copies decides on its own; the run before it is unevolved because levels never drop.
    assert ev([(7, 7), (117, 117), (1, 86)]) == [(False, "trajectory"), (False, "trajectory"),
                                                  (True, "level_ne_copies")]
    # Copies fell from 60 to 1 with Level == Copies: only an evolution explains it (Level 1, 1 copy left).
    assert ev([(60, 60), (1, 1), (1, 50)]) == [(False, "trajectory"), (True, "trajectory"),
                                                (True, "level_ne_copies")]
    # (2, 2) then (2, 40): unevolved 2 copies, or evolution 2 with 2 copies left -- both fit.
    assert [e for e, _ in ev([(2, 2), (2, 40)])] == [None, True]
    assert I.infer_evolved(knife, tables, [(2, 2), (2, 40)])[0].reason == "ambiguous"
    # 600 unevolved copies cannot end at evolution 1 with 5 left (100 + 5 < 600): no history fits.
    assert {r.reason for r in I.infer_evolved(knife, tables, [(600, 600), (1, 5)])} == {"inconsistent"}
    assert I.infer_evolved(knife, tables, []) == []


def test_items_evolved_override(cfg, inferred):
    items, tables = cfg
    ov = I.items_evolved_override(inferred)
    assert list(ov) == [str(t) for t in sorted(inferred)]
    assert json.loads(json.dumps(ov)) == ov
    assert all(isinstance(v, bool) for per in ov.values() for v in per.values())
    assert ov["0"] == {"knife": False} and ov["405"]["knife"] is True
    assert sum(v for per in ov.values() for v in per.values()) == I.inference_counts(inferred)["evolved"]
    amb = {5: {"knife": I.EvolvedInference(None, I.AMBIGUOUS, "x")}}
    assert I.items_evolved_override(amb) == {"5": {"knife": False}}
    assert I.items_evolved_override(amb, undetermined="null") == {"5": {"knife": None}}
    with pytest.raises(ValueError):
        I.items_evolved_override(amb, undetermined="error")
