"""Per-spawner economics of SimulationSystem: GPM replay, DpsData, eligibility (unit P8).

Design 3.3 G20, G21, R2, R2b; 7.3 "P8-oracle-simcalc". Golden suite ``simcalc``.
"""

from __future__ import annotations

import hashlib
import inspect
import json
import logging
import math
import re
import warnings
from dataclasses import fields, replace
from pathlib import Path

import numpy as np
import pytest

from tufu_balance.golden import at_least, at_most, check, exact, same_set
from tufu_balance.model import ab
from tufu_balance.model import data as model_data
from tufu_balance.model import items as model_items
from tufu_balance.model import simcalc as sc
from tufu_balance.model import spawners as sp
from tufu_balance.model.enums import StatType, WeaponType
from tufu_balance.model.items import ItemState
from tufu_balance.model.registry import REGISTRY, Provenance

log = logging.getLogger(__name__)
REPO = Path(__file__).resolve().parents[2]
SIMULATION_SYSTEM_CS = (REPO / "Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems"
                        / "SimulationSystem.cs")


@pytest.fixture(scope="module")
def ctx(db):
    with warnings.catch_warnings():
        warnings.simplefilter("ignore", ab.KeyDataFallbackWarning)
        return sc.load_simcalc_context(db)


@pytest.fixture(scope="module")
def sim_data(db):
    return model_data.load_simulation_data(db)


@pytest.fixture(scope="module")
def days(sim_data):
    return sim_data.time_chunk_snapshots


@pytest.fixture(scope="module")
def by_name(ctx):
    out = {}
    for s in ctx.spawners:
        out.setdefault(s.d.go_name, []).append(s)
    return out


def _score(replay: sc.GpmReplay, days) -> tuple[int, int, list, int, float]:
    hits = cells = bitwise = 0
    misses = []
    max_rel = 0.0
    for (tc, values), snap in zip(replay.replayed, days):
        assert tc == snap.time_chunk_index
        for st in snap.stats:
            cells += 1
            stored, replayed = st.current_gpm, values[st.key]
            if sc.gpm_matches(stored, replayed):
                hits += 1
            else:
                misses.append([tc, StatType(st.key).name])
            if stored == replayed:
                bitwise += 1
            else:
                max_rel = max(max_rel, abs(stored - replayed) / max(abs(stored), abs(replayed)))
    return hits, cells, misses, bitwise, max_rel


# ---------------------------------------------------------------- G20

def test_g20_gpm_replay(ctx, days, load_fixture):
    fixture = load_fixture("rpm_replay")
    variants = {(v["k2k3_day"], v["k4k5_day"]): v for v in fixture["variants"]}
    runs = {
        ("d-1", "d"): sc.replay_day_gpm(ctx, days, order=sc.RPM_FIRST),
        ("d", "d"): sc.replay_day_gpm(ctx, days, order=sc.MODIFIER_FIRST),
        ("d-1", "d-1"): sc.replay_day_gpm(ctx, days, order=sc.RPM_FIRST, k4k5_offset=1),
        ("d", "d-1"): sc.replay_day_gpm(ctx, days, order=sc.MODIFIER_FIRST, k4k5_offset=1),
    }
    scores = {k: _score(r, days) for k, r in runs.items()}
    for key, (hits, cells, misses, _, _) in scores.items():  # every variant reproduces the survey's cells
        want = variants[key]
        assert (hits, cells) == (want["match"], want["cells"]), key
        assert misses == [[m["TC"], m["stat"]] for m in want["misses"]], key
    # The port against the survey's float64 replay (it used 0.1 for (double)0.1f and stepwise K4/K5).
    survey = {r["TC"]: r["replayed"] for r in fixture["rpm_first_replayed"]}
    vs_survey = 0.0
    for tc, values in runs[("d-1", "d")].replayed:
        for key, v in values.items():
            s = survey[tc][StatType(key).name]
            if v != s:
                vs_survey = max(vs_survey, abs(v - s) / max(abs(v), abs(s)))
    assert vs_survey < 1e-7
    hits, cells, misses, bitwise, max_rel = scores[("d-1", "d")]
    check("simcalc", {
        "G20.rpm_first.hits": exact(hits, source="G20", better="higher", original=1380),
        "G20.rpm_first.cells": exact(cells, source="G20", original=1380),
        "G20.rpm_first.misses": same_set(misses, source="G20", better="subset", original=[]),
        "G20.rpm_first.bitwise_equal": exact(bitwise, source="G20", better="higher",
                                             note="cells whose replay equals the stored CurrentGpm bit for bit"),
        "G20.rpm_first.max_rel_err": at_most(max_rel, source="G20", note="max |stored - replayed| / max(|.|)"),
        "G20.rpm_first.max_rel_vs_survey": at_most(vs_survey, source="G20",
                                                   note="against rpm_replay.json rpm_first_replayed"),
        "G20.ablation_k2k3_d.hits": exact(scores[("d", "d")][0], source="G20", original=1376),
        "G20.ablation_k2k3_d.misses": same_set(scores[("d", "d")][2], source="G20", original=[
            [1, "Health"], [30, "CritDamagePercent"], [94, "Health"], [94, "StabDamageFlat"]]),
        "G20.ablation_k4k5_lag.hits": exact(scores[("d-1", "d-1")][0], source="G20", original=1047),
        "G20.ablation_k4k5_lag_only.hits": exact(scores[("d", "d-1")][0], source="G20",
                                                 note="K2/K3 at d, K4/K5 at d-1 (fixture: 1046)"),
    })


def test_g20_variant_b_runs(ctx, days):
    """AB variant B changes the fast RPM parameters (15, 5); the replay must still run (policy extrapolation)."""
    b = ctx.with_variant("B")
    assert b.fast_rpm_config() == (15.0, 5.0) and ctx.fast_rpm_config() == (10.0, 1.0)
    replay = sc.replay_day_gpm(b, days[:6])
    assert all(v >= 0.0 for _, values in replay.replayed for v in values.values())


# ---------------------------------------------------------------- G21

def test_g21_initial_player(db, ctx, days):
    stats = sc.initial_player_state_stats(db, ctx)
    items = [(ctx.items["knife"], ItemState())]  # new save: knife level 1, 1 copy, weapon_1 only
    dps = sc.calculate_dps_data(ctx, stats, items, 1, 0)
    defense = sc.calculate_defense_data(ctx, stats, items, 0, 0)
    assert dps == days[0].dps_data  # bit for bit against the shipped TC0 snapshot
    assert defense == days[0].defense_data
    by_type = {WeaponType(wt).name: v for wt, v in dps}
    assert round(by_type["Cutting"], 4) == 9.2308
    assert {round(v, 5) for k, v in by_type.items() if k != "Cutting"} == {0.92308}
    check("simcalc", {
        "G21.dps": exact(by_type, source="G21", note="initial player: base stats + knife 1/1, one weapon slot"),
        "G21.defense_all_zero": exact(all(v == 0.0 for _, v in defense), source="G21", original=True),
    })


# ---------------------------------------------------------------- R2, R2b

@pytest.fixture(scope="module")
def evolved(ctx, days):
    inferred = model_items.infer_evolved_snapshots(days, ctx.items, ctx.item_tables)
    undetermined = [(tc, k, r.reason) for tc, per in inferred.items() for k, r in per.items() if r.evolved is None]
    for row in undetermined:
        log.warning("R2: evolved state undetermined, treated as unevolved: %s", row)
    return {tc: {k: r.evolved for k, r in per.items()} for tc, per in inferred.items()}, len(undetermined)


def _r2_numbers(results: list[sc.DpsRecompute]) -> dict:
    def summary(attr: str) -> dict:
        errs = [getattr(r, attr) for r in results]
        worst = max(range(len(results)), key=lambda i: errs[i])
        return {"exact": sum(e == 0.0 for e in errs), "max": errs[worst], "max_tc": results[worst].time_chunk_index,
                "tc100_max": max(getattr(r, attr) for r in results if r.time_chunk_index >= 100), "last": errs[-1]}
    return {"dps": summary("dps_max_rel_err"), "defense": summary("defense_max_rel_err")}


def test_r2_fitted_slots(ctx, days, evolved, load_fixture):
    fixture = load_fixture("stage4_slot_schedule")
    assert fixture["kind"] == "fitted"
    slots = {c["TimeChunkIndex"]: (c["weapon_slots"], c["armor_slots"]) for c in fixture["chunks"]}
    ev, undetermined = evolved
    results = [sc.recompute_dps_defense(ctx, s, ev[s.time_chunk_index], *slots[s.time_chunk_index]) for s in days]
    n = _r2_numbers(results)
    # Pin the K3 timing: K3(d-1) fits the stored data better, so the one-sided bounds alone would not notice.
    lagged = [sc.recompute_dps_defense(ctx, s, ev[s.time_chunk_index], *slots[s.time_chunk_index], k3_offset=1)
              for s in days]
    per_snapshot = [[r.time_chunk_index, r.dps_max_rel_err, r.defense_max_rel_err] for r in results]
    assert per_snapshot != [[r.time_chunk_index, r.dps_max_rel_err, r.defense_max_rel_err] for r in lagged]
    check("simcalc", {
        "R2.undetermined_evolved": exact(undetermined, source="R2", original=0),
        "R2.dps.exact": at_least(n["dps"]["exact"], source="R2", original=1),
        "R2.dps.max": at_most(n["dps"]["max"], source="R2", original=0.2283),
        "R2.dps.max_tc": exact(n["dps"]["max_tc"], source="R2", original=19),
        "R2.dps.tc100_max": at_most(n["dps"]["tc100_max"], source="R2", original=0.01214),
        # Design 3.3 prints 0.00132; stage 4 measured 0.001323 (stage4_slot_schedule, TC405), which this reproduces.
        "R2.dps.last": at_most(n["dps"]["last"], source="R2", original=0.001323,
                               note="design 3.3 rounds the stage-4 value 0.001323 to 0.00132"),
        "R2.defense.exact": at_least(n["defense"]["exact"], source="R2", original=9),
        "R2.defense.max": at_most(n["defense"]["max"], source="R2", original=0.8786),
        "R2.defense.max_tc": exact(n["defense"]["max_tc"], source="R2", original=9),
        "R2.defense.tc100_max": at_most(n["defense"]["tc100_max"], source="R2", original=0.00971),
        "R2.defense.last": at_most(n["defense"]["last"], source="R2", original=0.00115),
        "R2.per_snapshot": exact(per_snapshot, source="R2", note="[TC, DPS max rel err, Defense max rel err] at K3(d)"),
        "R2.ablation_k3_d_minus_1": exact(_r2_numbers(lagged), source="R2",
                                          note="same with K3(d-1): fits better on every snapshot (fact for Z1)"),
    })


def test_r2b_closed_form_slots(db, ctx, days, evolved, load_fixture):
    ev, _ = evolved
    unlocks = sc.slot_unlock_locations(sp.load_spawners(db))
    assert unlocks == {"weapon_2": 2, "weapon_3": 4, "weapon_4": 8, "helmet_1": 5, "shield_1": 6}
    results = [sc.recompute_dps_defense(ctx, s, ev[s.time_chunk_index], *sc.slots_p12(s.location, unlocks))
               for s in days]
    fitted = {c["TimeChunkIndex"]: (c["weapon_slots"], c["armor_slots"])
              for c in load_fixture("stage4_slot_schedule")["chunks"]}
    r2 = {s.time_chunk_index: sc.recompute_dps_defense(ctx, s, ev[s.time_chunk_index], *fitted[s.time_chunk_index])
          for s in days}
    vs_r2 = [[r.time_chunk_index, r.dps_max_rel_err - r2[r.time_chunk_index].dps_max_rel_err,
              r.defense_max_rel_err - r2[r.time_chunk_index].defense_max_rel_err] for r in results]
    n = _r2_numbers(results)
    check("simcalc", {
        "R2b.slots": exact([[r.time_chunk_index, r.weapon_slots, r.armor_slots] for r in results], source="R2b",
                           note="[TC, weapon slots, armor slots] from the snapshot Location (P12)"),
        "R2b.dps.exact": at_least(n["dps"]["exact"], source="R2b"),
        "R2b.dps.max": at_most(n["dps"]["max"], source="R2b"),
        "R2b.dps.max_tc": exact(n["dps"]["max_tc"], source="R2b"),
        "R2b.dps.tc100_max": at_most(n["dps"]["tc100_max"], source="R2b"),
        "R2b.dps.last": at_most(n["dps"]["last"], source="R2b"),
        "R2b.defense.exact": at_least(n["defense"]["exact"], source="R2b"),
        "R2b.defense.max": at_most(n["defense"]["max"], source="R2b"),
        "R2b.defense.max_tc": exact(n["defense"]["max_tc"], source="R2b"),
        "R2b.defense.tc100_max": at_most(n["defense"]["tc100_max"], source="R2b"),
        "R2b.defense.last": at_most(n["defense"]["last"], source="R2b"),
        "R2b.minus_R2": exact(vs_r2, source="R2b", note="[TC, DPS max err R2b - R2, Defense max err R2b - R2]"),
    })


# ---------------------------------------------------------------- eligibility and kill days

def _day_state(ctx, sim_data):
    def state(d: int) -> sc.PlayerState:
        snap = sim_data.get_by_time_chunk_index(d)
        return sc.snapshot_state(snap, ctx, order=sc.MODIFIER_FIRST, day=d, unlocked_location=99)
    return state


def test_first_kill_days_vs_forensics(ctx, sim_data, load_fixture):
    fixture = load_fixture("forensics_killdays")
    by_id = {(s.d.key, s.d.file_id): s for s in ctx.spawners}
    rows = fixture["day_spawners"]
    targets = [by_id[(r["Key"], r["file_id"])] for r in rows]
    kill = sc.first_kill_days(ctx, _day_state(ctx, sim_data), targets)
    diff = [[r["go_name"], r["file_id"], r["kill_day"], kill[by_id[(r["Key"], r["file_id"])].spawner_id]]
            for r in rows if kill[by_id[(r["Key"], r["file_id"])].spawner_id] != r["kill_day"]]
    gates = {int(n): g for n, g in fixture["gates"].items()}
    gate_days = {n: kill[next(s for s in targets if s.d.key == g["Key"]).spawner_id] for n, g in gates.items()}
    assert gate_days == {n: g["kill_day"] for n, g in gates.items()}
    check("simcalc", {
        "killdays.spawners": exact(len(targets), source="7.3 P8", note="day-mode spawners of the fixture"),
        "killdays.diff_vs_fixture": same_set(diff, source="7.3 P8", better="subset",
                                             note="[go_name, file_id, fixture kill day, eligible_spawners kill day]"),
        "killdays.never_before_405": exact(sum(1 for v in kill.values() if v >= 405), source="7.3 P8"),
        "killdays.gate_days": exact({str(n): d for n, d in sorted(gate_days.items())}, source="7.3 P8",
                                    note="first kill day of each island's latest-OT day gate"),
    })


def test_l3_gate_probes(ctx, sim_data, load_fixture):
    """The fixture's per-day fight of the island-3 gate and the first dragon (design 4.5, SlowLethalityGate)."""
    by_key = {s.d.key: s for s in ctx.spawners}
    worst = 0.0
    for probe in load_fixture("forensics_killdays")["l3_gate_lethality"]["probes"]:
        s = by_key[probe["Key"]]
        for row in probe["days"]:
            d = row["day"]
            state = sc.snapshot_state(sim_data.get_by_time_chunk_index(d), ctx, order=sc.MODIFIER_FIRST, day=d)
            x = sc.spawner_data(ctx, state, s)
            for got, want in ((x.fight_time, row["fight_time"]), (x.loss, row["loss"]), (state.stat(StatType.Health), row["H"])):
                if got != want:
                    worst = max(worst, abs(got - want) / max(abs(got), abs(want)))
    assert worst < 1e-7
    check("simcalc", {"l3_gate.max_rel_vs_fixture": at_most(worst, source="4.5",
                                                            note="fight_time, loss, H against l3_gate_lethality")})


def test_eligible_excludes_cleared_dafc(ctx, sim_data, by_name):
    dragon = next(s for s in by_name["dragon"] if s.d.location == 4)
    assert dragon.d.dafc
    state = sc.snapshot_state(sim_data.get_by_time_chunk_index(8), ctx, order=sc.MODIFIER_FIRST, day=8)
    assert state.unlocked_location >= 4
    fresh = sc.eligible_spawners(ctx, state)
    assert dragon.spawner_id in fresh
    cleared = sc.BotFilter(cleared_dafc=frozenset({dragon.spawner_id}))
    after = sc.eligible_spawners(ctx, state, cleared)
    assert dragon.spawner_id not in after and set(after) == set(fresh) - {dragon.spawner_id}
    assert float(cleared.cooldown(dragon.spawner_id)) == math.inf
    # The GPM path never sees DAFC spawners; a used-up spawner or one on cooldown is not eligible either.
    assert all(not x.spawner.d.dafc for x in sc.calculate_spawner_data(ctx, state).values())
    other = next(sid for sid in fresh if sid != dragon.spawner_id)
    limit = ctx.charges_limit(next(s for s in ctx.spawners if s.spawner_id == other))
    assert other not in sc.eligible_spawners(ctx, state, sc.BotFilter(charges_used={other: limit}))
    assert other in sc.eligible_spawners(ctx, state, sc.BotFilter(charges_used={other: limit - 1}))
    assert other not in sc.eligible_spawners(ctx, state, sc.BotFilter(cooldowns={other: 1.0}))
    # Locked islands and the OT cap.
    locked = sc.snapshot_state(sim_data.get_by_time_chunk_index(8), ctx, order=sc.MODIFIER_FIRST, day=8,
                               unlocked_location=3)
    assert dragon.spawner_id not in sc.eligible_spawners(ctx, locked)
    early = sc.snapshot_state(sim_data.get_by_time_chunk_index(8), ctx, order=sc.MODIFIER_FIRST, day=6)
    assert float(dragon.d.optimum_time) > 6 and dragon.spawner_id not in sc.eligible_spawners(ctx, early)


def test_boss_tower_dropped(ctx, sim_data, by_name):
    tower = by_name["BossTower"][0]
    assert math.isinf(tower.base_stat(StatType.Health)) and tower.d.count == 0 and not tower.d.dafc
    state = sc.snapshot_state(sim_data.time_chunk_snapshots[-1], ctx, unlocked_location=99)
    x = sc.spawner_data(ctx, state, tower)
    assert math.isnan(x.fight_time) and not sc.keeps_fight_time(x)
    assert tower.spawner_id not in sc.calculate_spawner_data(ctx, state)
    assert tower.spawner_id not in sc.eligible_spawners(ctx, state, sc.BotFilter(alive=frozenset({tower.spawner_id})))


def test_rpm_survivable_is_the_recovered_predicate(ctx, sim_data):
    # Verbatim: the recovered C# line and the port.
    text = SIMULATION_SYSTEM_CS.read_text(encoding="utf-8")
    assert len(re.findall(r"if \(parameters\.Item2 \* health < loss\) continue;", text)) == 1
    assert "return not (max_health_loss_multiplier * health < loss)" in inspect.getsource(sc.rpm_survivable)
    assert sc.rpm_survivable(10.0, 10.0, 1.0)  # loss == L*H still counts (non-strict)
    assert not sc.rpm_survivable(math.nextafter(10.0, 11.0), 10.0, 1.0)
    assert sc.rpm_survivable(49.0, 10.0, 5.0) and not sc.rpm_survivable(51.0, 10.0, 5.0)
    assert sc.rpm_survivable(math.nan, 10.0, 1.0)  # C#: NaN < x is false, so the spawner is not skipped
    # Inside the RPM: a spawner whose loss is exactly H contributes with health multiplier 1.
    state = sc.snapshot_state(sim_data.time_chunk_snapshots[10], ctx)
    data = sc.calculate_spawner_data(ctx, state)
    sid, x = next((sid, x) for sid, x in data.items() if x.spawner.stat_rewards)
    stat = x.spawner.stat_rewards[0].stat_type
    health = state.stat(StatType.Health)
    alone = {sid: x}
    x.health_delta = -health
    at_h = sc.rpm_value_time_chunk(ctx, state, alone, stat, None)
    x.health_delta = -math.nextafter(health, math.inf)
    above = sc.rpm_value_time_chunk(ctx, state, alone, stat, None)
    x.health_delta = 0.0
    full = sc.rpm_value_time_chunk(ctx, state, alone, stat, None)
    assert at_h == full > 0.0 and above == 0.0
    assert sc.fast_killable(sc.SpawnerData(x.spawner, health_delta=-health), health)


def test_soul_rpm(ctx, days):
    state = sc.snapshot_state(days[-1], ctx)
    rpm, souls, data = sc.calculate_rpm_value_time_chunk(ctx, state)
    keys = {x.spawner.soul_reward for x in data.values() if x.spawner.soul_reward}
    assert keys < set(souls) == set(ctx.soul_keys) and all(souls[k] >= sc.SOUL_RPM_FLOOR for k in keys)
    again = sc.calculate_soul_rpm_values(ctx, sc.snapshot_state(days[0], ctx), {}, souls)
    assert again == souls  # monotonic: an empty spawnerData keeps every stored value
    assert set(rpm) == {int(t) for t in StatType}


def test_cs_max_min_nan():
    assert math.isnan(sc.cs_max(math.nan, 1.0)) and math.isnan(sc.cs_max(1.0, math.nan))
    assert math.isnan(sc.cs_min(math.nan, 1.0)) and sc.cs_max(0.5, 2.0) == 2.0 and sc.cs_min(0.5, 2.0) == 0.5


def test_ab_rpm_multiplier(ctx):
    data = ctx.ab_config.common_reward_multipliers("A")
    assert {float(ab.rpm_stat_value_multiplier(data, t)) for t in StatType} == {1.0}
    custom = ab.CommonRewardMultipliersData(2.0, 3.0, 4.0)
    assert [float(ab.rpm_stat_value_multiplier(custom, t)) for t in (1, 2, 22, 6, 8, 12, 14, 16, 20, 0)] == \
        [4.0, 4.0, 4.0, 2.0, 2.0, 2.0, 3.0, 3.0, 3.0, 1.0]


def test_ported_functions_carry_va():
    ported = [i for i in REGISTRY.implementations if i.module.endswith((".simcalc", ".ab"))]
    names = {i.qualname for i in ported}
    assert {"combat_caches", "calculate_dps_data", "calculate_defense_data", "best_armor_against_dps",
            "dps_against_spawner", "spawner_dps", "battle_regen", "resource_value", "spawner_reward_value",
            "spawner_data", "calculate_spawner_data", "rpm_survivable", "rpm_value_time_chunk",
            "calculate_rpm_value_time_chunk", "calculate_soul_rpm_values", "rpm_config", "initial_rpm",
            "initial_soul_rpm", "player_ability_dps_multiplier", "player_ability_ehp_multiplier",
            "simulation_enemy_multiplier", "SimCalcContext.location_multiplier", "rpm_stat_value_multiplier"} <= names
    soul_vas = {i.va for i in ported if i.qualname == "calculate_soul_rpm_values"}
    assert {"0x13e6dd0", "0x13e8424", "0x13e86b8", "0x14889a4"} <= soul_vas
    # ABTestService.cs is not recovered from native code and has no VA comments: its ports cannot carry one.
    no_va_source = "CodeBase/ABTests/ABTestService.cs"
    ab_service = REPO / "Recovery/StudyProject/Assets/Scripts/Assembly-CSharp" / no_va_source
    assert "VA" not in ab_service.read_text(encoding="utf-8")
    for impl in ported:
        if impl.provenance != Provenance.PORT.value:
            continue
        assert impl.code_ref, impl
        if impl.code_ref.startswith(no_va_source):
            assert impl.va is None, impl
        else:
            assert impl.va and re.fullmatch(r"0x[0-9a-f]+", impl.va), impl


# ---------------------------------------------------------------- synthetic checks of the ported formulas

def _fake(base: sc.SimSpawner, file_id: int, *, rewards=(), soul=None, item_key=None, **d_fields) -> sc.SimSpawner:
    """A DAFC copy of ``base`` (charges limit = Count) with its own SpawnerId."""
    d = replace(base.d, file_id=file_id, dafc=True, **d_fields)
    return replace(base, d=d, stat_rewards=tuple(sc.StatReward(int(t), np.float32(v)) for t, v in rewards),
                   soul_reward=soul, item_key=item_key)


class _Multipliers:
    """An ABTestConfig whose common reward multipliers are replaced."""

    def __init__(self, config, data):
        self._config, self._data = config, data

    def common_reward_multipliers(self, variant):
        return self._data

    def __getattr__(self, name):
        return getattr(self._config, name)


@pytest.fixture(scope="module")
def base_spawner(ctx):
    return next(s for s in ctx.spawners if not s.d.is_static and not s.d.is_event and s.armors)


def test_rpm_value_time_chunk_terms(ctx, sim_data, base_spawner):
    state = sc.snapshot_state(sim_data.get_by_time_chunk_index(10), ctx)
    h = state.stat(StatType.Health)
    health = int(StatType.Health)

    def one(c, s, fight_time=5.0, loss=0.0, previous=None, stat=health):
        x = sc.SpawnerData(s, fight_time=fight_time, health_delta=-loss)
        return sc.rpm_value_time_chunk(c, state, {s.spawner_id: x}, stat, previous)

    s3 = _fake(base_spawner, -1, rewards=[(health, 1000.0)], location=3, count=3, is_elite=False)
    full = (10.0 / (10.0 + 5.0)) * 1000.0 * 3.0 * 1.0 * 1.0 / 60.0  # A: fightTimeSeconds 10, charges = Count 3
    assert one(ctx, s3) == full
    # Running max against the stored value.
    assert one(ctx, s3, previous=full * 2) == full * 2 and one(ctx, s3, previous=full / 2) == full
    # x2 for non-elite spawners on islands 1-2 only.
    assert one(ctx, _fake(base_spawner, -2, rewards=[(health, 1000.0)], location=1, count=3, is_elite=False)) == full * 2
    assert one(ctx, _fake(base_spawner, -3, rewards=[(health, 1000.0)], location=2, count=3, is_elite=False)) == full * 2
    assert one(ctx, _fake(base_spawner, -4, rewards=[(health, 1000.0)], location=1, count=3, is_elite=True)) == full
    # No candidate rewards the stat: the stored value is returned unchanged (early return).
    assert one(ctx, s3, stat=StatType.MovementSpeed, previous=1.25) == 1.25
    # Floor 0.5 for a positive total, applied before the AB stat-class multiplier.
    tiny = _fake(base_spawner, -5, rewards=[(health, 0.001)], location=3, count=1, is_elite=False)
    assert one(ctx, tiny) == 0.5
    scaled = replace(ctx, ab_config=_Multipliers(ctx.ab_config, ab.CommonRewardMultipliersData(2.0, 3.0, 2.5)),
                     _charges={}, _ab_mult={})
    assert one(scaled, tiny) == 0.5 * 2.5 and one(scaled, s3) == full * 2.5
    # Variant B (fightTimeSeconds 15, maxHealthLossMultiplier 5): H < loss <= 5H counts with sqrt(H / loss).
    b = ctx.with_variant("B")
    full_b = (15.0 / (15.0 + 5.0)) * 1000.0 * 3.0 * 1.0 * 1.0 / 60.0
    assert one(b, s3) == full_b
    assert one(b, s3, loss=4 * h) == full_b * 0.5
    loss = 5.0 * h
    assert one(b, s3, loss=loss) == (15.0 / (15.0 + 5.0)) * 1000.0 * 3.0 * 1.0 * math.sqrt(h / loss) / 60.0
    assert one(b, s3, loss=math.nextafter(loss, math.inf)) == 0.0  # skipped: no contribution, no floor
    assert one(ctx, s3, loss=4 * h) == 0.0  # A: L = 1


def test_spawner_reward_value_terms(base_spawner):
    state = sc.PlayerState(stats={}, dps_data={}, defense_data={}, unlocked_location=1, day=0,
                           item_keys=frozenset({"knife"}))
    s = _fake(base_spawner, -20, rewards=[(1, 3.0), (6, 5.0)], count=3, item_reward=None)
    assert sc.spawner_reward_value(s, state) == (1.0 * 3.0 + 1.0 * 5.0) * 3.0  # initial statsBenefit 1f
    benefit = {1: 0.1, 6: 2.0}
    want = (0.0 + float(np.float32(0.1)) * 3.0 + 2.0 * 5.0) * 3.0  # benefit is a float widened to double
    assert sc.spawner_reward_value(s, state, stats_benefit=benefit) == want != (0.1 * 3.0 + 10.0) * 3.0
    assert sc.spawner_reward_value(s, state, stats_benefit={6: 2.0}) == 2.0 * 5.0 * 3.0  # no entry: skipped
    assert sc.spawner_reward_value(s, state, stat_type=6) == 5.0 and sc.spawner_reward_value(s, state, stat_type=2) == 0.0
    assert sc.spawner_reward_value(s, state, current_boss=s.spawner_id) == 1000000.0  # not times Count
    assert sc.resource_value(s) == 3.0 and sc.resource_value(_fake(base_spawner, -21)) == 0.0
    item = _fake(base_spawner, -22, rewards=[(1, 3.0)], count=2, item_reward="cut common", item_key="knife")
    assert sc.spawner_reward_value(item, state) == 10000.0 * 2.0
    assert sc.spawner_reward_value(item, state, item_benefit={"knife": 0.7}) == (3.0 + float(np.float32(0.7))) * 2.0
    assert sc.spawner_reward_value(item, state, item_benefit={"pike": 0.7}) == 10000.0 * 2.0
    not_owned = replace(state, item_keys=frozenset())
    assert sc.spawner_reward_value(item, not_owned, item_benefit={"knife": 0.7}) == 10000.0 * 2.0


def test_soul_rpm_terms(ctx, sim_data, base_spawner):
    state = sc.snapshot_state(sim_data.get_by_time_chunk_index(10), ctx)
    h = state.stat(StatType.Health)
    c = replace(ctx, soul_drop_bonus=0.5, souls_rarity_amount={"k1": 0.25}, _charges={}, _ab_mult={})
    rows = [  # (soul key, Count = charges, fight time, loss)
        ("k1", 2, 5.0, 0.0), ("k1", 1, 20.0, h), ("k1", 4, 1.0, math.nextafter(h, math.inf)),
        ("k2", 1, 1e6, 0.0), ("  ", 1, 1.0, 0.0), (None, 1, 1.0, 0.0), ("k3", 1, 1.0, 2 * h)]
    data = {}
    for n, (key, count, fight_time, loss) in enumerate(rows):
        s = _fake(base_spawner, -40 - n, soul=key, count=count)
        data[s.spawner_id] = sc.SpawnerData(s, fight_time=fight_time, health_delta=-loss)
    out = sc.calculate_soul_rpm_values(c, state, data, {"k1": 0.0, "other": 7.0})
    k1 = ((1.0 + 0.5) + 0.25) * ((0.0 + (10.0 / (5.0 + 10.0)) * 2.0 + (10.0 / (20.0 + 10.0)) * 1.0) / 60.0)
    assert out == {"k1": k1, "other": 7.0, "k2": 0.03, "k3": 0.0}
    assert list(out) == ["k1", "other", "k2", "k3"]
    assert sc.calculate_soul_rpm_values(c, state, data, {"k1": 100.0})["k1"] == 100.0  # running max
    assert sc.initial_soul_rpm(ctx) == {k: 0.0 for k in ("common", "large", "elite", "huge", "great", "divine",
                                                         "chaotic")}


def test_fight_time_and_spawner_dps_inputs(ctx, sim_data):
    state = sc.snapshot_state(sim_data.get_by_time_chunk_index(104), ctx, unlocked_location=99)
    b = ctx.with_variant("B")
    caches = sc.combat_caches(ctx, state)
    for s in ctx.spawners:  # B's per-island enemy HP multiplier scales the fight time, not the player's DPS
        d = s.d
        if d.is_static or d.is_event or d.location not in (1, 3) or not s.armors:
            continue
        xa, xb = sc.spawner_data(ctx, state, s, caches), sc.spawner_data(b, state, s, caches)
        m = float(b.location_multiplier(d.location))
        assert m != 1.0 and xb.player_dps == xa.player_dps
        assert math.isclose(xb.fight_time / xa.fight_time, m, rel_tol=1e-15)
    # Enemy damage meets the defense of the location's PreferredWeaponType, not the spawner's own type
    # (TC13 is where that choice changes the most spawner DPS values).
    state = sc.snapshot_state(sim_data.get_by_time_chunk_index(13), ctx, unlocked_location=99)
    caches = sc.combat_caches(ctx, state)
    picked = 0
    for s in ctx.spawners:
        if s.d.is_static or s.d.is_event or not s.armors:
            continue
        preferred = ctx.preferred_weapon_type[s.d.location]
        same = replace(caches, defense_cache={wt: caches.defense_cache[preferred] for wt in caches.defense_cache})
        own = replace(caches, defense_cache={wt: caches.defense_cache[s.weapon_type] for wt in caches.defense_cache})
        if sc.spawner_dps(ctx, s, same) != sc.spawner_dps(ctx, s, own):
            assert sc.spawner_dps(ctx, s, caches) == sc.spawner_dps(ctx, s, same)
            picked += 1
    assert picked == 6


def test_dummy_not_alive_on_new_save(ctx, sim_data):
    dummy = next(s for s in ctx.spawners if s.d.go_name == "Spawner_Dummy")
    state = sc.snapshot_state(sim_data.time_chunk_snapshots[-1], ctx, order=sc.MODIFIER_FIRST, unlocked_location=99)
    assert dummy.spawner_id not in sc.calculate_spawner_data(ctx, state, for_gpm_bot=False)
    assert dummy.spawner_id not in sc.eligible_spawners(ctx, state)
    assert dummy.spawner_id in sc.calculate_spawner_data(ctx, state)  # forGPMBot = true skips no dead spawner
    alive = sc.BotFilter(alive=frozenset({dummy.spawner_id}))
    assert set(sc.calculate_spawner_data(ctx, state, for_gpm_bot=False, bot=alive)) == {dummy.spawner_id}


# ---------------------------------------------------------------- frozen per-spawner tables

SPAWNER_DATA_FIELDS = [f.name for f in fields(sc.SpawnerData) if f.name != "spawner"]


def _digest(rows: list) -> str:
    return hashlib.sha256(json.dumps(rows, separators=(",", ":")).encode()).hexdigest()


def _spawner_table(data: dict) -> dict:
    rows = [[sid[0], sid[1], *(getattr(x, f) for f in SPAWNER_DATA_FIELDS)] for sid, x in sorted(data.items())]
    sums = {f: math.fsum(getattr(x, f) for x in data.values()) for f in SPAWNER_DATA_FIELDS}
    return {"n": len(rows), "sums": sums, "sha256": _digest(rows)}


def test_spawner_data_tables(ctx, sim_data):
    b = ctx.with_variant("B")
    tables = {}
    for tc in (13, 104, 405):
        state = sc.snapshot_state(sim_data.get_by_time_chunk_index(tc), ctx)
        tables[f"A.tc{tc}"] = _spawner_table(sc.calculate_spawner_data(ctx, state))
        tables[f"B.tc{tc}"] = _spawner_table(sc.calculate_spawner_data(b, state))
    state = sc.snapshot_state(sim_data.get_by_time_chunk_index(104), ctx, order=sc.MODIFIER_FIRST, day=104)
    full = sc.calculate_spawner_data(ctx, state)
    missing = state.stat(StatType.Health) * 0.6
    low = sc.calculate_spawner_data(ctx, replace(state, current_health=state.stat(StatType.Health) - missing))
    # A fight that would gain health is capped at the missing health instead of 0.
    assert all(low[sid].health_delta >= x.health_delta for sid, x in full.items())
    assert any(low[sid].health_delta == missing and x.health_delta == 0.0 for sid, x in full.items())
    tables["A.tc104.low_health"] = _spawner_table(low)
    tables["A.tc104.bot"] = _spawner_table(sc.calculate_spawner_data(ctx, state, for_gpm_bot=False))
    check("simcalc", {f"spawner_data.{k}": exact(v, source="DP8", note="per-spawner SpawnerData (all fields): "
                                                 "count, per-field fsum, sha256 of the sorted rows")
                      for k, v in tables.items()})


def test_reward_value_table(ctx, sim_data):
    snap = sim_data.time_chunk_snapshots[-1]
    state = sc.snapshot_state(snap, ctx, unlocked_location=99)
    item_benefit = {k: 0.25 * (n + 1) for n, k in enumerate(sorted(state.item_keys))}
    stats_benefit = {int(t): 1.0 / (int(t) + 2) for t in StatType}
    boss = ctx.spawners[-1].spawner_id
    rows = [[s.d.scene, s.d.file_id, sc.spawner_reward_value(s, state),
             sc.spawner_reward_value(s, state, stats_benefit=stats_benefit, item_benefit=item_benefit,
                                     current_boss=boss), sc.resource_value(s)] for s in ctx.spawners]
    assert any(r[3] != r[2] for r in rows) and any(r[3] == 1000000.0 for r in rows)
    check("simcalc", {"reward_value.table": exact(
        {"n": len(rows), "sum_default": math.fsum(r[2] for r in rows), "sum_benefit": math.fsum(r[3] for r in rows),
         "sum_resource": math.fsum(r[4] for r in rows), "sha256": _digest(rows)}, source="3.1",
        note="CalculateSpawnerRewardValue (default and custom benefits, a current boss) and GetResourceValue "
             "of every registered spawner at TC405")})


def test_soul_rpm_values_frozen(ctx, sim_data):
    out = {}
    for variant in ("A", "B"):
        c = ctx.with_variant(variant)
        for tc in (10, 104, 405):
            state = sc.snapshot_state(sim_data.get_by_time_chunk_index(tc), c)
            _, souls, _ = sc.calculate_rpm_value_time_chunk(c, state)
            out[f"{variant}.tc{tc}"] = souls
    assert out["A.tc405"]["divine"] == 0.0 and out["A.tc405"]["common"] > sc.SOUL_RPM_FLOOR
    check("simcalc", {"soul_rpm.values": exact(out, source="3.1", note="SoulRPMCalculatedValue from OnAwake values")})


def test_g20_variant_b_frozen(ctx, days):
    replay = sc.replay_day_gpm(ctx.with_variant("B"), days)
    a = sc.replay_day_gpm(ctx, days)
    rows = [[tc, k, v] for tc, values in replay.replayed for k, v in sorted(values.items())]
    assert rows != [[tc, k, v] for tc, values in a.replayed for k, v in sorted(values.items())]
    check("simcalc", {"G20.variant_b": exact(
        {"cells": len(rows), "per_tc_fsum": [[tc, math.fsum(values.values())] for tc, values in replay.replayed],
         "sha256": _digest(rows)}, source="G20", note="RpmFirst replay under AB variant B (no shipped reference)")})
