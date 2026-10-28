"""Spawner definitions, AB tables, charges and cooldowns, readiness table and per-island bosses (unit P6).

Design 3.3 G17/G17b, 7.3 "P6-oracle-spawners-ab", P17/B11, Q27. Golden suite ``spawners``.
"""

from __future__ import annotations

import collections
import csv
import hashlib
import warnings
from pathlib import Path

import pytest

from tufu_balance.golden import check, exact, same_set
from tufu_balance.model import ab
from tufu_balance.model import spawners as sp
from tufu_balance.model.enums import Rarity
from tufu_balance.model.f32 import f32
from tufu_balance.model.registry import REGISTRY

DATA = Path(__file__).resolve().parent / "data"
# Unity readiness dump of C0a (run fix1_6288800 on commit 6288800, Main scene, new sandbox save, AB
# spawner_cooldowns_v3 = A): one row per registered spawner, ChargesTotal and BaseCooldown read live.
C0A_READY = DATA / "c0a_spawners_ready.csv"
C0A_READY_SHA256 = "8f8120bd3a545259ae53b8a87ee7de9598c15c89a8b05323a30094683ab1bfd9"


@pytest.fixture(scope="module")
def ab_config(db):
    with warnings.catch_warnings():
        warnings.simplefilter("ignore", ab.KeyDataFallbackWarning)
        config = ab.load_ab_config(db)
        for variant in ("A", "B"):
            config.spawner_cooldowns(variant), config.enemy_hp_damage_mult(variant), config.fast_rpm_params(variant)
        return config


@pytest.fixture(scope="module")
def spawners(db):
    return sp.load_spawners(db)


@pytest.fixture(scope="module")
def meta(db):
    return sp.load_meta_levels(db)


@pytest.fixture(scope="module")
def cooldowns(ab_config):
    return ab_config.spawner_cooldowns("A")


@pytest.fixture(scope="module")
def ready(spawners, ab_config, meta):
    return sp.expected_ready_spawners("A", spawners=spawners, ab_config=ab_config, meta=meta)


def _sid(s) -> list:
    return [s.scene, s.file_id]


# ---------------------------------------------------------------- G17, G17b

def test_g17_counts(spawners):
    by_slot = sorted([s.slot_reward, s.go_name, s.location, list(s.gate_next_locations)]
                     for s in spawners if s.slot_reward)
    check("spawners", {
        "G17.rows": exact(len(spawners), source="G17", original=717),
        "G17.ab_override": exact(sum(map(sp.uses_spawner_cooldowns_ab, spawners)), source="G17", original=569),
        "G17.dafc": exact(sum(s.dafc for s in spawners), source="G17", original=25),
        "G17.elite": exact(sum(s.is_elite for s in spawners), source="G17", original=33),
        "G17.event": exact(sum(s.is_event for s in spawners), source="G17", original=6),
        "G17.vip": exact(sum(s.is_vip for s in spawners), source="G17", original=10),
        "G17.static": exact(sum(s.is_static for s in spawners), source="G17", original=362),
        "G17.is_days": exact(sum(s.is_days for s in spawners), source="G17", original=609),
        "G17.gate_rows": exact(sum(bool(s.gate_next_locations) for s in spawners), source="G17", original=17),
        "G17.slot_rewards": same_set(by_slot, source="G17", note="slot, go_name, island, gate NextLocationNumbers"),
        "G17.count_zero": same_set([[s.go_name, s.location] for s in spawners if s.count == 0], source="G17",
                                   original=[["BossTower", 15]]),
    })
    slots = {s.slot_reward: s for s in spawners if s.slot_reward}
    assert set(slots) == {"weapon_2", "helmet_1", "shield_1"}
    assert slots["weapon_2"].location == 1 and slots["weapon_2"].gate_next_locations == (2,)
    assert slots["helmet_1"].go_name == "dragon" and slots["helmet_1"].location == 4
    assert slots["shield_1"].location == 5 and 6 in slots["shield_1"].gate_next_locations


def test_g17b_item_spawner_charges(spawners, cooldowns, meta):
    items = [s for s in spawners if s.item_reward]
    by_rarity = collections.Counter((s.item_rarity.name, sp.charges_total(s, cooldowns, meta)) for s in items)
    check("spawners", {
        "G17b.item_spawners": exact(len(items), source="G17b", original=127),
        "G17b.item_charges_by_rarity": same_set(
            [[r, c, n] for (r, c), n in by_rarity.items()], source="G17b",
            note="[rarity, ChargesTotal, spawners]; the 0 is one Common spawner with ChargesBeforeBigRespawn 0",
            original=[["Common", 8, 39], ["Common", 0, 1], ["Uncommon", 6, 30], ["Rare", 4, 24],
                      ["Epic", 2, 21], ["Legendary", 1, 12]]),
    })


def test_g17b_ab_tables(ab_config):
    a = ab_config.spawner_cooldowns("A")
    non_elite = {loc: float(v) for loc, v in a.non_elite.cooldown_minutes.overrides.items()}
    elite = {loc: float(v) for loc, v in a.elite.cooldown_minutes.overrides.items()}
    check("spawners", {
        "G17b.non_elite_cooldown_minutes": exact(
            [non_elite.get(n) for n in range(1, 14)], source="G17b", original=list(range(4, 29, 2))),
        "G17b.elite_cooldown_minutes": exact([elite.get(n) for n in range(1, 5)], source="G17b",
                                             original=[60, 90, 120, 150]),
        "G17b.cooldown_defaults": exact([float(a.non_elite.cooldown_minutes.default),
                                         float(a.elite.cooldown_minutes.default)], source="G17b",
                                        note="minutes beyond the overrides (islands 14+ non-elite, 5+ elite)"),
        "G17b.charges_per_unit": exact([a.non_elite.charges.get(1), a.elite.charges.get(1)], source="G17b",
                                       note="AB charges per unit: non-elite, elite (no overrides)"),
        "G17b.fast_rpm_A": exact(list(ab.fast_rpm_config(ab_config.fast_rpm_params("A"))), source="G17b",
                                 original=[10, 1]),
        "G17b.fast_rpm_B": exact(list(ab.fast_rpm_config(ab_config.fast_rpm_params("B"))), source="G17b",
                                 original=[15, 5]),
    })
    assert set(non_elite) == set(range(1, 14)) and set(elite) == {1, 2, 3, 4}
    assert float(ab.spawner_cooldown_seconds(a, False, 1)) == 240.0
    assert float(ab.spawner_cooldown_seconds(a, False, 14)) == 1800.0
    assert float(ab.spawner_cooldown_seconds(a, True, 4)) == 9000.0
    assert float(ab.spawner_cooldown_seconds(a, True, 5)) == 10800.0


def test_ab_variant_assignment(ab_config):
    # Tests with rules take the requested variant; a variant without data uses the C# defaults.
    assert ab_config.assigned_variant(ab.SPAWNER_COOLDOWNS, "B") == "B"
    assert ab_config.spawner_cooldowns("B") == ab.DEFAULT_SPAWNER_COOLDOWNS == ab_config.spawner_cooldowns("A")
    assert ab_config.assigned_variant("level_pass", "A") is None  # no rules: never applied
    assert ab_config.fast_rpm_params({ab.FAST_RPM_PARAMS: None}) == ab.DEFAULT_FAST_RPM_PARAMS
    b = ab_config.enemy_hp_damage_mult("B")
    assert [float(ab.enemy_hp_damage_multiplier(b, n)) for n in range(1, 6)] == \
        [float(f32(x)) for x in (1.1, 1.2, 1.3, 1.4, 1.5)]
    a = ab_config.enemy_hp_damage_mult("A")
    assert {float(ab.enemy_hp_damage_multiplier(a, n)) for n in range(1, 18)} == {1.0}


# ---------------------------------------------------------------- charges limit and cooldown rules

def test_charges_limit_rules(spawners, cooldowns, meta):
    classes = collections.Counter()
    bad = []
    for s in spawners:
        limit = sp.charges_limit(s, cooldowns, meta)
        if s.dafc:
            cls, want = "dafc", s.count
        elif sp.uses_spawner_cooldowns_ab(s):
            cls, want = ("ab_elite", 2 * s.count) if s.is_elite else ("ab_non_elite", 3 * s.count)
        elif s.item_reward:
            cls, want = "item", s.charges_before_big_respawn
        elif s.is_event:
            cls, want = "event", 0
        else:
            cls, want = "other", s.charges_before_big_respawn
        classes[cls] += 1
        if limit != want:
            bad.append((s.go_path, cls, limit, want))
    assert bad == []
    check("spawners", {
        "limit.classes": exact(dict(sorted(classes.items())), source="7.3 P6",
                               note="AB non-elite 3*Count, AB elite 2*Count, DAFC Count, item ChargesBeforeBigRespawn"),
    })


def test_meta_levels_apply():
    base = dict(scene="s", file_id=1, go_path="p", go_name="n", key="k", active=True, enabled=True, location=3,
                count=2, cooldown=f32(600), charges_before_big_respawn=5, is_event=False, is_vip=False,
                is_dummy=False, is_elite=False, is_static=False, is_days=True, dafc=False, item_reward=None,
                item_rarity=None, slot_reward=None)
    cd = ab.DEFAULT_SPAWNER_COOLDOWNS
    plain = sp.SpawnerDef(**base)
    assert float(sp.base_cooldown(plain, cd)) == 480.0 and sp.charges_total(plain, cd) == 6
    assert float(sp.base_cooldown(plain, cd, sp.MetaLevels(map_spawner_timer_reduce={3: 100.5}))) == 379.5
    assert float(sp.base_cooldown(plain, cd, sp.MetaLevels(map_spawner_timer_reduce=1e6))) == 0.0
    item = sp.SpawnerDef(**{**base, "item_reward": "x", "item_rarity": Rarity.Rare, "cooldown": f32(1000)})
    assert not sp.uses_spawner_cooldowns_ab(item)
    meta = sp.MetaLevels(items_rarity_charges={3: 1.9}, items_rarity_timer={3: 3.0})
    assert sp.charges_total(item, cd, meta) == 6 and sp.charges_base(item, cd, meta) == 5
    assert sp.base_cooldown(item, cd, meta) == f32(1000 / 3.0) and sp.small_cooldown_seconds(item, cd, meta) == 334
    short = sp.SpawnerDef(**{**base, "cooldown": f32(5)})
    assert not sp.uses_spawner_cooldowns_ab(short) and sp.charges_total(short, cd) == 5
    event = sp.SpawnerDef(**{**base, "is_event": True})
    assert sp.charges_total(event, cd) == 0 and float(sp.base_cooldown(event, cd)) == 600.0


# ---------------------------------------------------------------- readiness table

def test_expected_ready_spawners(spawners, ab_config, meta, ready):
    reasons = collections.Counter(sp.no_unit_reason(s) for s in spawners if s.active)
    exceptions = [[*_sid(s), s.key, sp.no_unit_reason(s)] for s in spawners if s.active and not sp.alive_on_new_save(s)]
    unregistered = [[*_sid(s), s.key] for s in spawners if not s.active]
    text = sp.ready_spawners_csv(ready)
    assert all(r.go_path and r.file_id for r in ready.values())
    assert len({(r.go_path, r.key) for r in ready.values()}) == len(ready)  # path + Key identifies a runtime spawner
    assert sp.expected_ready_spawners("B", spawners=spawners, ab_config=ab_config, meta=meta) == ready
    rows = list(csv.DictReader(text.splitlines()))
    assert [(r["scene"], int(r["file_id"])) for r in rows] == sorted(ready)
    assert all(float(r["base_cooldown"]) == ready[(r["scene"], int(r["file_id"]))].base_cooldown for r in rows)
    check("spawners", {
        "ready.registered": exact(len(ready), source="Runbook 6.1", original=716),
        "ready.alive": exact(sum(r.alive for r in ready.values()), source="Runbook 6.1", original=699),
        "ready.no_unit_reasons": exact({k: v for k, v in sorted((k, v) for k, v in reasons.items() if k)}, source="Runbook 6.1",
                                       original={"count0": 1, "dummy": 1, "event": 5, "vip": 10}),
        "ready.exceptions": same_set(exceptions, source="Runbook 6.1", note="[scene, file_id, Key, reason]"),
        "ready.unregistered": same_set(unregistered, source="Runbook 6.1",
                                       note="the inactive duplicate Eventspawner_Boss"),
        "ready.charges_total_hist": exact({str(k): v for k, v in sorted(collections.Counter(
            r.charges_total for r in ready.values()).items())}, source="DP6"),
        "ready.base_cooldown_hist": exact({repr(k): v for k, v in sorted(collections.Counter(
            r.base_cooldown for r in ready.values()).items())}, source="DP6"),
        "ready.csv_sha256": exact(hashlib.sha256(text.encode()).hexdigest(), source="DP6",
                                  note="expected_ready_spawners('A') as written into the request"),
    })


def test_ready_matches_c0a_runtime(ready):
    """The oracle against what Unity reported at readiness (C0a): every registered spawner equal."""
    data = C0A_READY.read_bytes()
    assert hashlib.sha256(data).hexdigest() == C0A_READY_SHA256
    rows = list(csv.DictReader(data.decode().splitlines()))
    by_key = {r.key: r for r in ready.values()}
    assert len(by_key) == len(ready)  # registered keys are unique
    mismatch = []
    for row in rows:
        e = by_key.get(row["key"])
        alive = row["anyone_alive"] == "True" and row["is_enabled"] == "True"
        if e is None or int(row["charges_total"]) != e.charges_total or float(row["base_cooldown"]) != e.base_cooldown \
                or alive != e.alive:
            mismatch.append(row["key"])
    assert mismatch == [] and len(rows) == len(ready) and {r["key"] for r in rows} == set(by_key)
    check("spawners", {"ready.c0a_runtime_equal": exact(len(rows) - len(mismatch), source="DP6", original=716,
                                                        note="C0a run fix1_6288800, ChargesTotal/BaseCooldown/alive")})


# ---------------------------------------------------------------- bosses (P17, B11)

def test_boss_for_location(spawners):
    bosses = sp.bosses_by_location(spawners)
    listing = {str(n): (None if b is None else [b.go_name, b.scene, b.file_id]) for n, b in bosses.items()}
    assert bosses[4].go_name == "dragon" and bosses[4].dafc and bosses[4].gate_next_locations == (5, 5)
    assert bosses[17] is None
    assert all(b is not None and b.location == n for n, b in bosses.items() if n < 17)
    # Gates that lead somewhere other than the next island (back-gates and the arena).
    other = sorted([s.go_name, s.location, nxt] for s in spawners for nxt in s.gate_next_locations
                   if nxt != s.location + 1)
    island4_other = [s for s in spawners if s.location == 4 and s.gate_next_locations and s is not bosses[4]]
    assert [s.go_name for s in island4_other] == ["Spawner_boss (3)"] and island4_other[0].gate_next_locations == (1,)
    check("spawners", {
        "P17.boss_by_location": exact(listing, source="P17/B11", note="[go_name, scene, file_id] per island 1-17"),
        "P17.other_gates": same_set(other, source="P17/B11",
                                    note="[go_name, island, NextLocationNumber] for gates not leading to island + 1"),
    })


# ---------------------------------------------------------------- keyData (Q27)

def test_decode_key_data():
    assert ab.decode_key_data(None, 0) == [] and ab.decode_key_data("", 3) == []
    assert ab.decode_key_data("0100000002000000030000000400000005000000060000000700000008000000090000000a0000000b0000000c0000000d000000", 13) \
        == list(range(1, 14))
    assert ab.decode_key_data("01000000020000000300000004000000", 4) == [1, 2, 3, 4]
    assert ab.decode_key_data("ffffffff07000000", 2) == [-1, 7]
    assert ab.decode_value_data("[1.1, 2]") == [1.1, 2] and ab.decode_value_data("0300000005000000") == [3, 5]
    as_float = float("01000000020000000300000004000000")
    ab.reset_key_data_warning()
    with warnings.catch_warnings(record=True) as caught:
        warnings.simplefilter("always")
        assert ab.decode_key_data(as_float, 4, "col1") == [1, 2, 3, 4]
        assert ab.decode_key_data(as_float, 4, "col2") == [1, 2, 3, 4]
        assert ab.decode_key_data(float("0100000002000000"), 2, "col3") == [1, 2]
    fallback = [w for w in caught if issubclass(w.category, ab.KeyDataFallbackWarning)]
    assert len(fallback) == 1 and "col1" in str(fallback[0].message)
    with pytest.raises(ValueError):
        ab.decode_key_data(as_float, 3)  # not the float of keys 1..3
    with pytest.raises(ValueError):
        ab.decode_key_data(1.0e30, 4)
    ab.reset_key_data_warning()


def test_keydata_in_database(db):
    """Old extractions hold two keyData cells as floats; fixed ones as hex text. Both decode the same."""
    columns = {
        ("c_ABTestDataConfig__spawnerCooldowns.variants", "data.elite.cooldownMinutes.overridesByLocation.keyData"),
        ("c_ABTestDataConfig__enemyHpDamageMult.variants", "data.damageMultiplier.overridesByLocation.keyData"),
    }
    numeric = sum(1 for table, col in columns
                  if db.execute(f'SELECT count(*) FROM "{table}" WHERE typeof("{col}") = \'real\'').fetchone()[0])
    assert numeric in (0, 2)
    ab.reset_key_data_warning()
    with warnings.catch_warnings(record=True) as caught:
        warnings.simplefilter("always")
        first, second = ab.load_ab_config(db), ab.load_ab_config(db)
        values = [first.spawner_cooldowns("A"), second.enemy_hp_damage_mult("B")]
    fallback = [w for w in caught if issubclass(w.category, ab.KeyDataFallbackWarning)]
    assert len(fallback) == (1 if numeric else 0)
    assert sorted(values[0].elite.cooldown_minutes.overrides) == [1, 2, 3, 4]
    assert sorted(values[1].damage_multiplier.overrides) == [1, 2, 3, 4]
    ab.reset_key_data_warning()


def test_registered_formulas():
    ids = {i.canonical_id for i in REGISTRY.implementations if i.module.endswith((".spawners", ".ab"))}
    assert {"spawners.ab_spawner_override_predicate", "spawners.base_cooldown", "spawners.effective_charges",
            "spawners.ab_cooldown_table", "spawners.ab_charges_table", "simulation.spawner_charges_limit",
            "simulation.rpm_stat_class_ab_multiplier"} <= ids
