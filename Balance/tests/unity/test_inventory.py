"""C6 inventory: real replay rewards, closed/checkpoint evolution, and stored-input G07.

Count structure and mapping are post-hoc. P18 uses only TC4-11 DPS; P12 is
scored on TC1-24 with TC>=25 reported separately, never against fitted slots.
"""
from __future__ import annotations

from collections import defaultdict
import csv
import io
import json
import math
import statistics

import numpy as np
import pytest

from tufu_balance.calibrate import targets as T, metrics as M
from tufu_balance.golden import check, exact, at_most, at_least
from tufu_balance.model import data, items, evolution_gen as eg, simcalc as sc, spawners as sp
from tufu_balance.model.enums import BalanceInterpolationType as K
from tufu_balance.unity import fasttick as ft, inventory, override as ov, request as req, simio, spawner_map, trace

pytestmark = pytest.mark.unity
SUITE = "sim_inventory"


def jobs(db):
    base = ov.default_override(db, from_day=0, replay_items="recompute", slots=None)
    def job(name, params=None, day=405, mode="replay", override=base, **kw):
        return ft.fast_job(name, mode, override=override if mode != "free" else None,
                           params=params, day=day, max_wall_seconds=600.0,
                           outputs={"inventory": True}, **kw)
    return [
        job("chain-swapped", spawner_days=True),
        job("chain-direct", {"PreferredMapping": "Direct"}),
        job("e1-swapped", {"EmissionModel": "E1"}),
        job("e1-direct", {"EmissionModel": "E1", "PreferredMapping": "Direct"}),
        job("reference", reference_flush=True),
        job("p18-stats-before", day=12),
        job("p18-items-before", {"DayChangeInventoryOrder": "ItemsThenStats"}, day=12),
        job("p18-stats-after", {"SlotRpmOrder": "SlotAfterRpm"}, day=12),
        job("p18-items-after", {"DayChangeInventoryOrder": "ItemsThenStats", "SlotRpmOrder": "SlotAfterRpm"}, day=12),
        job("inventory-free", mode="free"),
        job("inventory-checkpoint", mode="checkpoint", override=ov.default_override(db, from_day=4)),
        job("inventory-regenerate", {"AutoBalanceMode": "Regenerate"}, day=30),
        job("inventory-seeded", {"RollMode": "Seeded"}, day=30),
    ]


@pytest.fixture(scope="module")
def run(unity_run, db):
    result = unity_run(req.make_request(jobs(db)))
    assert result.exit_code == 0, result.describe()
    return result


def inv(run, job="chain-swapped"):
    return json.loads(run.file(job, "inventory.json"))


@pytest.fixture(scope="module")
def series(run):
    out = {}
    for job in run.request["jobs"]:
        name = job["job_id"]
        with T.connect_ro(simio.ingest(run.job_dir(name))) as db:
            out[name] = T.load_series(db)
    return out


@pytest.fixture(scope="module")
def targets(db):
    return T.build(db, slot_schedule=T.load_slot_schedule())


_frozen = {}


@pytest.fixture(autouse=True)
def record_golden():
    _frozen.clear()
    yield
    if _frozen:
        check(SUITE, dict(_frozen))


def freeze(key, value, source, **kw):
    _frozen[key] = exact(value, source=source, **kw)


def test_jobs_and_asset_guards(run):
    for job in run.request["jobs"]:
        name = job["job_id"]
        m = run.manifest(name)
        assert m["exit_code"] == 0 and m["engine"]["stop_reason"] == "day", name
        guard = json.loads(run.file(name, "asset_guard.json"))["after_job"]
        assert guard["ok"] and not guard["changed_on_disk"]
        assert "Combat Balance Config" not in guard["changed_in_memory"], (name, guard)
        d = inv(run, name)
        assert d["restored"], name
        assert d["balance_before_sha256"] == d["balance_restored_sha256"], name
        if name != "inventory-regenerate":
            assert d["balance_before_sha256"] == d["balance_during_sha256"], name
        else:
            assert d["balance_before_sha256"] != d["balance_during_sha256"], name
        if name != "inventory-regenerate":
            assert d["frozen_unchanged"], name
        else:
            assert d["mode"] == 1 and not d["frozen_unchanged"]


def test_all_count_combinations_and_carry(run):
    for name in ("chain-swapped", "chain-direct", "e1-swapped", "e1-direct"):
        totals = inv(run, name)["totals"]
        assert totals
        assert all(0 <= r["expectation"] - r["copies"] < 1 for r in totals)
        freeze(name + ".carry_max", max(r["expectation"] - r["copies"] for r in totals), "4.7")
    assert inv(run, "chain-swapped")["totals"] != inv(run, "e1-swapped")["totals"]
    assert inv(run, "chain-swapped")["totals"] != inv(run, "chain-direct")["totals"]


def test_random_streams_and_slow_clear_contract(run, db):
    from tufu_balance.unity.simrandom import SimulationRandom
    interpolation = data.load_interpolations(db)
    defs = items.load_item_defs(db)
    for name in ("chain-swapped", "inventory-seeded"):
        rng = SimulationRandom(1 if name.endswith("seeded") else 0, 1)
        slow = []
        for r in inv(run, name)["rewards"]:
            assert rng.roll(r["key"], r["expectation"]) == r["copies"]
            if r["phase"] == "slow":
                kind = K.ItemLegendaryEmission if defs[r["key"]].rarity == 4 else K.ItemEmission
                k = float(interpolation.get_total_coefficient(r["day"], kind))
                # K7 is zero throughout the slow phase; one clear is always n=1.
                assert r["expectation"] == float(np.float32(1 + k))
                slow.append(r)
        assert slow


def test_fast_rewards_match_the_independent_count_formula(run, db):
    defs = items.load_item_defs(db)
    interp = data.load_interpolations(db)
    locations = {r["Number"]: (r["PreferredWeaponType"], r["PreferredArmorType"]) for r in db.execute('SELECT * FROM c_LocationsConfig__Locations')}
    by_asset = {i.asset_name: i for i in defs.values()}
    by_id = {spawner_map.runtime_id(spawner_map.runtime_path(s.scene, s.go_path), s.key): s for s in sp.registered(sp.load_spawners(db))}
    days = {d.day: d for d in ft.read_days(run.file("chain-swapped", ft.DAYS_FILE).decode())}
    rows = ft.read_spawner_days(run.file("chain-swapped", ft.SPAWNER_DAYS_FILE))
    expectations = defaultdict(float)
    per_rarity = defaultdict(list)
    for r in rows:
        s = by_id[r.spawner]
        if not r.fights or not s.item_reward:
            continue
        item = by_asset[s.item_reward]
        chain = float(interp.get_total_coefficient(r.day, K.ItemChainProbability))
        n = r.fights * r.count
        base = n if chain == 1 else (1 - chain**n) / (1 - chain)
        k0 = float(interp.get_total_coefficient(r.day, K.ItemLegendaryEmission if item.rarity == 4 else K.ItemEmission))
        k7 = float(interp.get_total_coefficient(r.day, K.ItemEmissionByType))
        current = days[r.day].rounds[r.round - 1].location
        preferred = item.damage_type == locations[current][0 if item.is_armor else 1]
        expectations[(r.day, item.key)] += base * float(np.float32(1 + k0 + (k7 if preferred else 0)))
        if 4 <= r.day < 11 and r.round == 1:
            per_rarity[(r.day, item.rarity)].append(base * float(np.float32(1 + k0)))
    actual = {(r["day"], r["key"]): r["expectation"] for r in inv(run)["rewards"] if r["phase"] == "fast"}
    assert expectations.keys() == actual.keys()
    assert all(math.isclose(v, actual[k], rel_tol=2e-7, abs_tol=1e-10) for k, v in expectations.items())
    limits = {0: (5, 6), 1: (4.4, 4.7), 2: (3.4, 3.6), 3: (1.9, 2), 4: (1, 1)}
    assert per_rarity
    measured = {}
    for (day, rarity), values in per_rarity.items():
        # Some spawners first become killable after part of a day's charges were used in the slow phase.
        value = statistics.median(values)
        lo, hi = limits[rarity]
        assert lo * .95 <= value <= hi * 1.05, (day, rarity, values)
        measured[f"{day}|{rarity}"] = value
    freeze("chain.daily_per_spawner", measured, "T12 post-hoc, TC4-11 by day and rarity")


def test_t11_and_t12_posthoc_residuals(run, series, targets, db):
    for name in ("chain-swapped", "chain-direct", "e1-swapped", "e1-direct"):
        cells = M.t12(series[name], targets)
        freeze(name + ".t12_cells", {c.key: [c.sim, c.ref, c.rel] for c in cells}, "T12 post-hoc; integer copies by rarity")
    cells = M.t12(series["chain-swapped"], targets)
    phase = [c for c in cells if c.group != "daily" and c.ref > 0]
    _frozen.update({
        "t12.weapon_phase_max": at_most(max(c.rel for c in phase if "weapon" in c.key), source="T12 post-hoc", original=.05),
        "t12.armor_phase_max": at_most(max(c.rel for c in phase if "armor" in c.key), source="T12 post-hoc", original=.07),
        "t12.late_phase_max": at_most(max(c.rel for c in phase if c.group == "phase_154_405"), source="T12 post-hoc", original=.02),
    })
    appearances = M.t11(series["chain-swapped"], targets)
    assert all(c.residual is not None and 0 <= c.residual <= 1 for c in appearances)
    freeze("t11.appearances", {c.key: [c.sim, c.ref] for c in appearances}, "T11")
    interp = data.load_interpolations(db)
    selected = {}
    for name in ("chain-swapped", "chain-direct", "e1-swapped", "e1-direct"):
        rows = inv(run, name)["rewards"]
        selected[name] = {
            "mapping_days": sorted({r["day"] for r in rows if float(interp.get_total_coefficient(r["day"], K.ItemEmissionByType)) > 0}),
            "coefficient_days": sorted({r["day"] for r in rows if float(interp.get_total_coefficient(r["day"], K.ItemEmission)) > .1}),
        }
    assert all(d > 0 for d in selected["chain-swapped"]["mapping_days"])
    freeze("discriminating_days", selected, "5.5 K7>0 for mapping; K0>0.1 for coefficient form")


def test_t13_free_checkpoint_and_headroom(run, series, targets):
    for name in ("inventory-free", "inventory-checkpoint"):
        cells = M.t13(series[name], targets)
        hits = sum(c.residual is not None and abs(c.residual) <= 1 for c in cells)
        _frozen[name + ".t13_fraction"] = at_least(hits / len(cells), source="T13", original=.9)
        freeze(name + ".t13_cells", {c.key: [c.sim, c.ref, c.residual] for c in cells}, "T13 train C/U/R; holdout E/L")
        headroom = inv(run, name)["headroom"]
        assert headroom and all(r["headroom"] == r["copies"] - r["shards"] for r in headroom)
        freeze(name + ".headroom", headroom, "T13 copies/shards at first processing on/after target")


def test_t17_slots_export_and_fixture_contradiction(series, targets):
    for name in ("inventory-free", "inventory-checkpoint"):
        slots = series[name].slots
        assert all(slots[d][0] == 2 for d in range(1, 6))
        assert all(w == 3 for d, (w, a) in slots.items() if 6 <= d <= 21)
        assert all(w == 4 for d, (w, a) in slots.items() if d >= 24)
        assert all(a == 0 for d, (w, a) in slots.items() if d < 9)
    s = series["chain-swapped"]
    assert s.slots is not None
    assert s.slots[6][0] == 3 and s.slots[24][0] == 4
    assert all(a == 0 for d, (w, a) in s.slots.items() if d < 9)
    freeze("t17.actual_vs_fitted", {str(d): [list(s.slots[d]), list(v)] for d, v in targets.slot_schedule.items()}, "T17 comparison only")
    freeze("t17.fitted_armor_before_9", sorted({a for d, (w, a) in targets.slot_schedule.items() if d < 9}), "T17 fitted fixture contradicts design zero")


def test_p18_all_four_combinations_dps_only(run, series, targets):
    scores = {}
    for name in ("p18-stats-before", "p18-items-before", "p18-stats-after", "p18-items-after"):
        s = series[name]
        residuals = [abs(s.day(d).dps[k] - ref.dps[k]) / abs(ref.dps[k])
                     for ref in targets.shipped.tc if 4 <= (d := ref.tc) <= 11 for k in ref.dps if ref.dps[k]]
        scores[name] = {"cells": len(residuals), "mean_rel": statistics.mean(residuals), "max_rel": max(residuals)}
    freeze("p18.dps_tc4_11", scores, "P18 training TC4-11 DPS only; Q25 remains diagnostic")
    assert series["p18-stats-before"].day(6).dps != series["p18-stats-after"].day(6).dps


def test_p12_fit_from_dps_and_report_holdout(db):
    ctx = sc.load_simcalc_context(db)
    shipped = data.load_simulation_data(db).time_chunk_snapshots
    evolved = ov.items_evolved_default(db)
    gates = sc.slot_unlock_locations(sp.load_spawners(db))
    scores = {}
    for a, b in ((3, 8), (4, 8), (4, 7)):
        splits = defaultdict(list)
        for s in shipped:
            w, armor = sc.slots_p12(s.location, gates)
            w = 1 + int(w >= 2) + int(s.location >= a) + int(s.location >= b)
            result = sc.recompute_dps_defense(ctx, s, evolved[str(s.time_chunk_index)], w, armor)
            values = [abs(v - dict(s.dps_data)[k]) / abs(dict(s.dps_data)[k]) for k, v in result.dps if dict(s.dps_data).get(k)]
            if s.time_chunk_index >= 1:
                splits["train" if s.time_chunk_index <= 24 else "holdout"].extend(values)
        scores[f"{a},{b}"] = {k: {"cells": len(v), "mean_rel": statistics.mean(v), "max_rel": max(v)} for k, v in splits.items()}
    freeze("p12.dps_candidates", scores, "P12 choose TC1-24 only; report TC>=25 independently")
    assert min(scores, key=lambda k: scores[k]["train"]["mean_rel"]) == "4,8"


def test_reference_flush_positive_rewards(run):
    assert inv(run)["rewards"] and inv(run, "reference")["rewards"]
    for name in ("simulation_data.json", "timechunk_full.json", "snapshots_ext.csv", "items_ext.csv", "digests.csv"):
        assert run.file("chain-swapped", name) == run.file("reference", name), name


def test_generator_matches_p5_and_shipped_225_each(run, db):
    cfg = eg.load_evolution_config(db)
    generated = inv(run)["generator"]["rows"]
    counts = inventory.validate_generator_rows(generated, cfg)
    freeze("g07.rows", dict(counts), "G07 C# fed stored H and shards vs P5", original={"armor":225,"weapon":225})


def test_unobserved_rarities_chain_curve_and_x6(run, db):
    from tufu_balance.model.data import load_island_target_days
    probe = inv(run)["generator"]
    ranges = {0: (5, 6), 1: (4.4, 4.7), 2: (3.4, 3.6), 3: (1.9, 2), 4: (1, 1)}
    assert len(probe["chain_examples"]) == 35
    for r in probe["chain_examples"]:
        lo, hi = ranges[r["rarity"]]
        assert lo * .95 <= r["chain"] <= hi * 1.05, r
    freeze("chain.all_rarities", probe["chain_examples"], "T12 measured runtime chain including early unobserved rarities")
    x6 = probe["x6"]
    assert x6["before"] == x6["with_equipment"] and x6["total_before"] != x6["total_after"]
    itd = load_island_target_days(db)
    for island, day in probe["island_days"]:
        n = math.floor(island)
        fraction = float(np.float32(island - n))
        previous = 0.0 if n == 1 else float(itd[n - 2])
        expected = np.float32(previous if fraction == 0 else previous + fraction * (float(itd[n - 1]) - previous))
        assert day == float(expected), (island, day, float(expected))


def test_comparisons_use_only_discriminating_days_and_late_plateau(run, series, targets, db):
    interp = data.load_interpolations(db)
    phases = M.T12_PHASES
    scores = {}
    for name in ("chain-swapped", "chain-direct", "e1-swapped", "e1-direct"):
        eligible = {(a, b) for a, b in phases if all(float(interp.get_total_coefficient(d, K.ItemEmissionByType)) > 0 for d in range(a, b))}
        cells = [c for c in M.t12(series[name], targets) if c.group != "daily" and tuple(map(int, c.key.split('|')[0].split('->'))) in eligible and c.ref > 0]
        scores[name] = {kind: sum(abs(c.sim - c.ref) for c in cells if kind in c.key) / sum(c.ref for c in cells if kind in c.key)
                        for kind in ("weapon", "armor")}
    freeze("mapping.k7_positive_only", scores, "5.5 post-hoc mapping comparison; only full phases with K7>0")

    defs = items.load_item_defs(db)
    by_asset = {i.asset_name: i for i in defs.values()}
    by_id = {spawner_map.runtime_id(spawner_map.runtime_path(s.scene, s.go_path), s.key): s for s in sp.registered(sp.load_spawners(db))}
    locs = {r["Number"]: (r["PreferredWeaponType"], r["PreferredArmorType"]) for r in db.execute('SELECT * FROM c_LocationsConfig__Locations')}
    days = {d.day: d for d in ft.read_days(run.file("chain-swapped", ft.DAYS_FILE).decode())}
    no_drop, full = defaultdict(float), defaultdict(float)
    plateau = defaultdict(lambda: [0., 0])
    for r in ft.read_spawner_days(run.file("chain-swapped", ft.SPAWNER_DAYS_FILE)):
        s = by_id[r.spawner]
        if not r.fights or not s.item_reward:
            continue
        it = by_asset[s.item_reward]
        n = r.count * r.fights
        probability = float(interp.get_total_coefficient(r.day, K.ItemChainProbability))
        chain = n if probability == 1 else (1 - probability**n) / (1 - probability)
        coefficient = float(interp.get_total_coefficient(r.day, K.ItemLegendaryEmission if it.rarity == 4 else K.ItemEmission))
        k7 = float(interp.get_total_coefficient(r.day, K.ItemEmissionByType))
        preferred = it.damage_type == locs[days[r.day].rounds[r.round - 1].location][0 if it.is_armor else 1]
        key = (r.day, it.kind, it.rarity)
        full[key] += chain * float(np.float32(1 + coefficient + (k7 if preferred else 0)))
        no_drop[key] += chain * float(np.float32(1 + (k7 if preferred else 0)))
        if r.day >= 206:
            group = (it.rarity == 4, preferred)
            plateau[group][0] += chain * float(np.float32(1 + coefficient + (k7 if preferred else 0)))
            plateau[group][1] += n
    coefficient_scores = {}
    rarity_names = ("Common", "Uncommon", "Rare", "Epic", "Legendary")
    for a, b in phases:
        if not all(float(interp.get_total_coefficient(d, K.ItemEmission)) > .1 for d in range(a, b)):
            continue
        refs = M._gains(targets.shipped, targets, a, b)
        for key, ref in refs.items():
            if not ref:
                continue
            kind, rarity = key
            r = rarity_names.index(rarity)
            expected = [sum(src.get((d, kind, r), 0) for d in range(a, b)) for src in (full, no_drop)]
            coefficient_scores[f"{a}->{b}|{kind}|{rarity}"] = {"with_drop_rel": abs(expected[0] - ref) / ref,
                                                                  "without_drop_rel": abs(expected[1] - ref) / ref}
    assert coefficient_scores
    freeze("coefficient.k0_above_point1_only", coefficient_scores, "5.5 full vs no-K0/K6; only phases with K0>0.1")
    measured = {f"{legendary}|{preferred}": count / charges for (legendary, preferred), (count, charges) in plateau.items()}
    assert len(measured) == 4
    expected = {(False, False): 2.5, (False, True): 4.3, (True, False): 2.8, (True, True): 4.6}
    errors = [abs(count / charges - expected[group]) / expected[group] for group, (count, charges) in plateau.items()]
    freeze("t12.late_per_charge", measured, "T12 post-hoc TC206-404, nonpreferred/preferred by legendary")
    _frozen["t12.late_plateau_rel"] = at_most(max(errors), source="T12 post-hoc 2.50/4.30 and 2.80/4.60", original=.02)
