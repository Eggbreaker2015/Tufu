"""Spawner Health/Damage bake (unit P7): G13, G15, G16, R1, BossTower, BakeError, injection.

Design 3.3 (G13, G15, G16, R1), 3.6 item 3, 7.3 "P7-oracle-bake". Golden suite ``bake``.
Counts are taken under the default interpolation rounding (``interpolation_default.json``) unless a
test names one; G13 also runs all four roundings.
"""

from __future__ import annotations

import dataclasses
import hashlib
import math
import re
import sqlite3
import statistics
import time
from collections import defaultdict

import numpy as np
import pytest

from tufu_balance.golden import at_least, check, exact, same_set
from tufu_balance.model import bake as B
from tufu_balance.model import data as MD
from tufu_balance.model import interpolation as I
from tufu_balance.model import snapshots as S
from tufu_balance.model.interpolation import ROUNDINGS, Rounding

TOL1, TOL5 = 0.01, 0.05


# ----------------------------------------------------------------------------- fixtures
@pytest.fixture(scope="module")
def rows_inputs(db):
    armors = MD.load_armor_configs(db)
    return [(r, MD.spawner_input_from_row(r, armors)) for r in MD.load_spawner_rows(db)]


@pytest.fixture(scope="module")
def contexts(db):
    return {None: MD.load_bake_context(db), **{r.value: MD.load_bake_context(db, rounding=r) for r in ROUNDINGS}}


@pytest.fixture(scope="module")
def baked(contexts, rows_inputs):
    """{rounding (None = default): [(row, input, report), ...]} in ``load_spawner_rows`` order."""
    return {k: [(r, sp, B.bake_spawner(ctx, sp)) for r, sp in rows_inputs] for k, ctx in contexts.items()}


def _sid(sp) -> str:
    return f"{sp.spawner_id[0]}:{sp.spawner_id[1]}"


def _fingerprint(ids) -> str:
    return hashlib.sha256("\n".join(sorted(ids)).encode()).hexdigest()


def _ratio(scene: float, pred: float) -> float:
    """analysis/bake_check.py ``ratio``: Inf/Inf counts as 1, other non-finite cases are NaN."""
    if math.isinf(scene) and math.isinf(pred):
        return 1.0
    if pred == 0 or math.isnan(pred) or math.isinf(pred) or math.isinf(scene):
        return math.nan
    return scene / pred


def _within(scene: float, pred: float, tol: float) -> bool:
    r = _ratio(scene, pred)
    return math.isfinite(r) and abs(r - 1) <= tol


def _flag(r, name) -> bool:
    return bool(int(r[name] or 0))


def _is_event(r) -> bool:
    return _flag(r, "IsEvent") or str(r["go_name"]).lower().startswith("event")


# ----------------------------------------------------------------------------- G13
def _g13(rows) -> dict:
    mm = [(r, sp, rep) for r, sp, rep in rows if not sp.is_days]
    hp = [x for x in mm if x[1].health_base == x[2].new_health]
    bi = [x for x in mm if x[0]["bake.Health"] is not None]
    # OT band 0-120 minutes; event, dummy and HandBalance spawners are hand-set or overridden at runtime
    band = [x for x in mm if float(x[1].optimum_time) <= 120.0001
            and not (_is_event(x[0]) or _flag(x[0], "IsDummy") or _flag(x[0], "HandBalance"))]
    return {
        "n": len(mm),
        "hp_exact": len(hp),
        "damage_exact": sum(sp.damage_base == rep.new_damage for _, sp, rep in mm),
        "both_exact": sum(sp.health_base == rep.new_health and sp.damage_base == rep.new_damage for _, sp, rep in mm),
        "hp_exact_ids": sorted(_sid(sp) for _, sp, _ in hp),
        "hp_exact_keys": sorted(sp.key + "|" + sp.name for _, sp, _ in hp),
        "ot_0_120_n": len(band),
        "ot_0_120_hp_exact": sum(sp.health_base == rep.new_health for _, sp, rep in band),
        "bakeinfo_n": len(bi),
        "bakeinfo_health_eq_pred": sum(float(r["bake.Health"]) == rep.new_health for r, _, rep in bi),
        "bakeinfo_damage_eq_pred": sum(float(r["bake.Damage"]) == rep.new_damage for r, _, rep in bi),
    }


def test_g13_minute_bake(baked, load_fixture):
    g = _g13(baked[None])
    fx = load_fixture("bake_probe_counts")
    assert g["hp_exact_keys"] == fx["minute_mode_hp_exact_set"]["keys"]
    assert {k: g[k] for k in ("n", "hp_exact", "damage_exact", "both_exact")} == fx["bake_minute_mode"]
    assert (g["bakeinfo_n"], g["bakeinfo_health_eq_pred"], g["bakeinfo_damage_eq_pred"]) == tuple(
        fx["bake_infos_minute_mode"][k] for k in ("n", "health_eq_pred", "damage_eq_pred"))
    check("bake", {
        "G13.minute_n": exact(g["n"], source="G13", original=108),
        "G13.minute_hp_exact": exact(g["hp_exact"], source="G13", better="higher", original=79),
        "G13.minute_damage_exact": exact(g["damage_exact"], source="G13", better="higher", original=104),
        "G13.minute_both_exact": exact(g["both_exact"], source="G13", better="higher", original=79),
        "G13.minute_hp_exact_set": same_set(g["hp_exact_ids"], source="G13", better="superset",
                                            note="SpawnerId scene:file_id; equals fixture bake_probe_counts keys"),
        "G13.ot_0_120_n": exact(g["ot_0_120_n"], source="G13", original=78,
                                note="minute spawners, OT <= 120, without event/dummy/HandBalance"),
        "G13.ot_0_120_hp_exact": exact(g["ot_0_120_hp_exact"], source="G13", better="higher", original=76),
        "G13.bakeinfo_health_eq_pred": exact(g["bakeinfo_health_eq_pred"], source="G13", better="higher", original=13),
        "G13.bakeinfo_damage_eq_pred": exact(g["bakeinfo_damage_eq_pred"], source="G13", better="higher", original=43),
    })


def test_g13_roundings(baked, load_fixture):
    """Counts and the exact set under each rounding; stepwise and fma must agree (fma-switch ablation)."""
    fx = load_fixture("bake_probe_counts")
    per = {r.value: _g13(baked[r.value]) for r in ROUNDINGS}
    keys = ("hp_exact", "damage_exact", "both_exact", "ot_0_120_hp_exact", "bakeinfo_health_eq_pred",
            "bakeinfo_damage_eq_pred")
    for name in ("stepwise", "fma"):
        assert per[name]["hp_exact_keys"] == fx["minute_mode_hp_exact_set"]["keys"], name
        assert per[name]["hp_exact"] == fx["bake_minute_mode"]["hp_exact"], name
    assert {k: per["stepwise"][k] for k in keys} == {k: per["fma"][k] for k in keys}
    assert per["stepwise"]["hp_exact_ids"] == per["fma"]["hp_exact_ids"]
    assert _g13(baked[I.default_rounding().value]) == _g13(baked[None])
    original = {"hp_exact": 79, "damage_exact": 104, "both_exact": 79, "ot_0_120_hp_exact": 76,
                "bakeinfo_health_eq_pred": 13, "bakeinfo_damage_eq_pred": 43}
    results = {}
    for name, g in per.items():
        counts = {k: g[k] for k in keys}
        counts["hp_exact_set_sha256"] = _fingerprint(g["hp_exact_ids"])
        if name in ("stepwise", "fma"):
            results[f"G13.rounding.{name}"] = exact(counts, source="G13", original={
                **original, "hp_exact_set_sha256": _fingerprint(per["stepwise"]["hp_exact_ids"])},
                note="stepwise and fma equal (design: known)")
        else:
            results[f"G13.rounding.{name}"] = exact(counts, source="G13", note="first measured by P7")
    check("bake", results)


def test_k4_k5_follow_rounding(contexts, rows_inputs):
    """The bake's ability coefficients come from total_coefficient under ctx.rounding."""
    differ = set()
    for _, sp in rows_inputs:
        reps = {r.value: B.bake_spawner(contexts[r.value], sp) for r in (Rounding.STEPWISE, Rounding.FMA)}
        for r, rep in reps.items():
            ctx = contexts[r]
            day = np.float32(rep.day)
            assert rep.ability_dps == float(ctx.interpolations.total_coefficient(day, 4, rounding=r))
            assert rep.ability_ehp == float(ctx.interpolations.total_coefficient(day, 5, rounding=r))
        st, fm = reps["stepwise"], reps["fma"]
        assert st.ability_dps == float(contexts["stepwise"].interpolations.get_interpolation_coefficient(st.day, 4))
        if (st.ability_dps, st.ability_ehp) != (fm.ability_dps, fm.ability_ehp):
            differ.add(_sid(sp))
            assert sp.is_days
    check("bake", {"G13.k4k5_differ_stepwise_fma": exact(len(differ), source="G13",
                                                         note="spawners whose K4 or K5 differ; counts still equal")})


# ----------------------------------------------------------------------------- G15
def test_g15_early_offset(contexts, baked):
    ctx = contexts[None]
    rows = baked[None]
    eligible = [x for x in rows if x[1].location_number >= ctx.early_offset_start_location
                and x[1].relative_optimum_time < ctx.early_offset_relative_part]
    shifted = [x for x in rows if float(x[2].recalc_optimum_time) != float(x[1].optimum_time)]
    assert [_sid(x[1]) for x in eligible] == [_sid(x[1]) for x in shifted]
    assert all(x[1].is_days for x in shifted)

    def w5(use_shift):
        h = d = 0
        for _, sp, rep in shifted:
            t = rep.recalc_optimum_time if use_shift else sp.optimum_time
            rr = B.build_spawner_bake_report(ctx, sp, sp.optimum_time, t, sp.is_days)
            h += _within(sp.health_base, rr.new_health, TOL5)
            d += _within(sp.damage_base, rr.new_damage, TOL5)
        return h, d

    (hw, dw), (ho, do) = w5(True), w5(False)
    check("bake", {
        "G15.affected": exact(len(shifted), source="G15", original=182, note="L >= 11 and rel < 0.5, all day-mode"),
        "G15.affected_set_sha256": exact(_fingerprint(_sid(x[1]) for x in shifted), source="G15"),
        "G15.with_shift_hp_w5": exact(hw, source="G15", better="higher", original=147),
        "G15.without_shift_hp_w5": exact(ho, source="G15", original=13),
        "G15.with_shift_damage_w5": exact(dw, source="G15", better="higher", original=177),
        "G15.without_shift_damage_w5": exact(do, source="G15", original=16),
    })


def test_recalc_optimum_time_matches_double_formula(contexts, rows_inputs):
    """Same as the fixture's ``recalc_optimum_time_model_vs_f64_differ_gt_1e-3`` (0)."""
    ctx = contexts[None]
    itd = [float(v) for v in ctx.island_target_days]
    for _, sp in rows_inputs:
        L, rel, ot = sp.location_number, float(sp.relative_optimum_time), float(sp.optimum_time)
        want = ot
        if L >= 11 and rel < 0.5:
            want = ot - (itd[L - 1] - itd[L - 2]) * (0.5 - rel) * (1 if sp.is_days else 60)
        assert abs(float(B.recalc_optimum_time(sp, ctx)) - want) <= 1e-3, sp.name


# ----------------------------------------------------------------------------- G16
def _group_test(rows, is_days: bool) -> dict:
    """analysis/bake_check.py H1: spawners sharing (mode, bake time, chosen armor type) see one player
    state, so scene HP / raw HP is one factor; integer HP limits it to [(v-0.5)/raw, (v+0.5)/raw]."""
    g = defaultdict(list)
    for r, sp, rep in rows:
        if sp.is_days != is_days or sp.count <= 0 or _flag(r, "HandBalance") or _is_event(r) or _flag(r, "IsDummy"):
            continue
        v, raw = sp.health_base, rep.new_health_raw
        if not math.isfinite(v) or raw <= 0 or not math.isfinite(raw):
            continue
        g[(round(rep.recalc_optimum_time, 4), rep.armor_type)].append(
            ((v - 0.5) / raw, (v + 0.5) / raw, (float(sp.average_fight_time), sp.count, rep.armor_resist_used)))
    multi = nontrivial = multi_ok = nontrivial_ok = 0
    for members in g.values():
        if len(members) < 2:
            continue
        ok = max(m[0] for m in members) <= min(m[1] for m in members) * (1 + 1e-12)
        multi += 1
        multi_ok += ok
        if len({m[2] for m in members}) > 1:
            nontrivial += 1
            nontrivial_ok += ok
    return {"multi": multi, "multi_consistent": multi_ok, "nontrivial": nontrivial,
            "nontrivial_consistent": nontrivial_ok}


def test_g16_group_consistency(baked, load_fixture):
    days, minutes = _group_test(baked[None], True), _group_test(baked[None], False)
    s4 = load_fixture("stage4_summaries")["summaries"]["bake"]["H1_group_consistency"]
    for mode, name in ((True, "scene_days"), (False, "scene_minutes")):
        st = _group_test(baked["stepwise"], mode)
        assert st == {"multi": s4[name]["n_groups_multi"], "multi_consistent": s4[name]["multi_consistent_within_rounding"],
                      "nontrivial": s4[name]["n_groups_nontrivial"],
                      "nontrivial_consistent": s4[name]["nontrivial_consistent_within_rounding"]}
    check("bake", {
        "G16.days_nontrivial": exact([days["nontrivial_consistent"], days["nontrivial"]], source="G16", original=[90, 92]),
        "G16.days_multi": exact([days["multi_consistent"], days["multi"]], source="G16", original=[125, 127]),
        "G16.minutes_nontrivial": exact([minutes["nontrivial_consistent"], minutes["nontrivial"]], source="G16",
                                        original=[13, 13]),
        "G16.minutes_multi": exact([minutes["multi_consistent"], minutes["multi"]], source="G16", original=[18, 18]),
    })


# ----------------------------------------------------------------------------- R1
def _r1(rows) -> dict:
    dm = [x for x in rows if x[1].is_days]
    full = {"n": len(dm),
            "hp_w1": sum(_within(sp.health_base, rep.new_health, TOL1) for _, sp, rep in dm),
            "hp_w5": sum(_within(sp.health_base, rep.new_health, TOL5) for _, sp, rep in dm),
            "damage_w1": sum(_within(sp.damage_base, rep.new_damage, TOL1) for _, sp, rep in dm),
            "damage_w5": sum(_within(sp.damage_base, rep.new_damage, TOL5) for _, sp, rep in dm)}
    hp_set = [x for x in dm if not (str(x[0]["go_name"]).startswith("BossTower") or x[0]["go_name"] == "Spawner_Dummy")]
    dmg_set = [x for x in hp_set if x[2].defense != 0]  # player DefenseData of the preferred type at bake time
    return {"full": full, "hp_set": hp_set, "dmg_set": dmg_set,
            "hp_w1": sum(_within(sp.health_base, rep.new_health, TOL1) for _, sp, rep in hp_set),
            "hp_w5": sum(_within(sp.health_base, rep.new_health, TOL5) for _, sp, rep in hp_set),
            "damage_w1": sum(_within(sp.damage_base, rep.new_damage, TOL1) for _, sp, rep in dmg_set),
            "damage_w5": sum(_within(sp.damage_base, rep.new_damage, TOL5) for _, sp, rep in dmg_set)}


def test_r1_day_bake(baked, load_fixture):
    r = _r1(baked[None])
    # Full-set counts are report-only; under stepwise they must equal the stage-4 by_mode numbers.
    s4 = load_fixture("stage4_summaries")["summaries"]["bake"]["by_mode"]["days"]
    st = _r1(baked["stepwise"])["full"]
    assert st == {"n": s4["health"]["n"], "hp_w1": s4["health"]["within1"], "hp_w5": s4["health"]["within5"],
                  "damage_w1": s4["damage"]["within1"], "damage_w5": s4["damage"]["within5"]}
    print(f"\nR1 full set (report only, default rounding): {r['full']}")
    # Every exact day-mode Damage lies where the player's defence is 0, i.e. outside the Damage subset.
    assert not any(sp.damage_base == rep.new_damage for _, sp, rep in r["dmg_set"])
    check("bake", {
        "R1.n_days": exact(r["full"]["n"], source="R1", original=609),
        "R1.hp_set": exact({"n": len(r["hp_set"]), "sha256": _fingerprint(_sid(x[1]) for x in r["hp_set"])},
                           source="R1", note="by_mode days without BossTower and Spawner_Dummy"),
        "R1.damage_set": exact({"n": len(r["dmg_set"]), "sha256": _fingerprint(_sid(x[1]) for x in r["dmg_set"])},
                               source="R1", note="HP set without spawners whose bake DefenseData is 0"),
        "R1.hp_w1": at_least(r["hp_w1"], source="R1"),
        "R1.hp_w5": at_least(r["hp_w5"], source="R1"),
        "R1.damage_w1": at_least(r["damage_w1"], source="R1"),
        "R1.damage_w5": at_least(r["damage_w5"], source="R1"),
    })


def test_boss_tower_inf(baked, contexts):
    boss = [(r, sp, rep) for r, sp, rep in baked[None] if str(r["go_name"]).startswith("BossTower")]
    assert len(boss) == 1
    _, sp, rep = boss[0]
    assert sp.count == 0 and sp.is_days and math.isinf(sp.health_base) and sp.health_base > 0
    # (playerDps * AverageFightTime) / (double)0 = +Inf; Math.Round(+Inf) = +Inf
    assert math.isinf(rep.new_health_raw) and rep.new_health_raw > 0
    assert rep.new_health == sp.health_base
    assert rep.enemies_count == 0.5 and math.isfinite(rep.new_damage)
    assert _ratio(sp.health_base, rep.new_health) == 1.0  # why the full-set HP count includes it
    other = next(sp for _, sp, _ in baked[None] if sp.is_days and sp.count > 0)
    assert math.isinf(B.bake_spawner(contexts[None], dataclasses.replace(other, count=0)).new_health)


# ----------------------------------------------------------------------------- BakeError
def _one(rows_inputs, *, is_days=True, min_location=1):
    return next(sp for _, sp in rows_inputs if sp.is_days == is_days and sp.location_number >= min_location)


def test_bake_error_no_armor(contexts, rows_inputs):
    sp = dataclasses.replace(_one(rows_inputs), armors=[])
    with pytest.raises(B.BakeError, match=re.escape(sp.name)) as e:
        B.bake_spawner(contexts[None], sp)
    assert "armor" in str(e.value) and e.value.spawner_id == sp.spawner_id


@pytest.mark.parametrize("is_days", [True, False])
def test_bake_error_missing_dps_and_defense(contexts, rows_inputs, is_days):
    ctx = contexts[None]
    sp = _one(rows_inputs, is_days=is_days)
    t = B.recalc_optimum_time(sp, ctx)
    snap = ctx.snapshot_for_bake(t, sp.is_days)
    # Scores use FirstOrDefault (0), so the first armor row is chosen and DpsData.First throws.
    armor_types = {a.type for a in sp.armors}
    no_dps = dataclasses.replace(snap, dps_data=[(w, v) for w, v in snap.dps_data if w not in armor_types])
    with pytest.raises(B.BakeError, match=re.escape(sp.name) + ".*DpsData") as e:
        B.build_spawner_bake_report(ctx, sp, sp.optimum_time, t, sp.is_days, snapshot=no_dps)
    assert isinstance(e.value, ValueError)
    with pytest.raises(B.BakeError, match="DpsData"):
        B.build_spawner_bake_report(ctx, sp, sp.optimum_time, t, sp.is_days,
                                    snapshot=dataclasses.replace(snap, dps_data=[]))
    with pytest.raises(B.BakeError, match="DefenseData"):
        B.build_spawner_bake_report(ctx, sp, sp.optimum_time, t, sp.is_days,
                                    snapshot=dataclasses.replace(snap, defense_data=[]))
    with pytest.raises(B.BakeError, match="stat"):
        B.build_spawner_bake_report(ctx, sp, sp.optimum_time, t, sp.is_days,
                                    snapshot=dataclasses.replace(snap, stats=[]))


def test_bake_error_itd_out_of_range(contexts, rows_inputs):
    ctx = contexts[None]
    sp = next(sp for _, sp in rows_inputs if sp.relative_optimum_time < ctx.early_offset_relative_part)
    n = len(ctx.island_target_days)
    for loc in (n + 1, 150):
        bad = dataclasses.replace(sp, location_number=loc)
        with pytest.raises(B.BakeError, match=re.escape(sp.name) + ".*IslandTargetDays"):
            B.recalc_optimum_time(bad, ctx)
    # L = 1 reads ITD[-1] in C# (throws) once StartLocation <= 1
    low = dataclasses.replace(ctx, early_offset_start_location=1)
    with pytest.raises(B.BakeError, match="IslandTargetDays"):
        B.recalc_optimum_time(dataclasses.replace(sp, location_number=1), low)
    assert B.recalc_optimum_time(dataclasses.replace(sp, location_number=n), ctx) <= sp.optimum_time
    # out of range only matters on the early-offset branch
    late = dataclasses.replace(sp, location_number=150, relative_optimum_time=np.float32(0.9))
    assert B.recalc_optimum_time(late, ctx) == sp.optimum_time


def test_bake_error_unknown_location(contexts, rows_inputs):
    sp = dataclasses.replace(_one(rows_inputs, is_days=False), location_number=77)
    with pytest.raises(B.BakeError, match="LocationsConfig"):
        B.build_spawner_bake_report(contexts[None], sp, sp.optimum_time, sp.optimum_time, sp.is_days)


# ----------------------------------------------------------------------------- injection, read-only
def _make_sim_db(db_path, path, scale_dps: float = 1.0):
    """A stand-in for a run's ``sim.sqlite``: only the ``c_SimulationData__*`` tables."""
    con = sqlite3.connect(path)
    con.execute("ATTACH DATABASE ? AS src", (f"file:{db_path}?mode=ro",))
    names = [r[0] for r in con.execute("SELECT name FROM src.sqlite_master WHERE type='table' "
                                       "AND name LIKE 'c_SimulationData__%'")]
    for name in names:
        con.execute(f'CREATE TABLE main."{name}" AS SELECT * FROM src."{name}"')
        if name.endswith("__DpsData"):
            con.execute(f'UPDATE main."{name}" SET Value = Value * ?', (scale_dps,))
    con.commit()
    con.execute("DETACH DATABASE src")
    con.close()
    return names


def _sha(path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def test_load_bake_context_injects_simulation_data(db, db_path, tmp_path, baked, rows_inputs):
    sd = MD.load_simulation_data(db)
    ctx = MD.load_bake_context(db, simulation_data=sd)
    assert ctx.simulation_data is sd
    base = [rep.new_health_raw for _, _, rep in baked[None]]
    assert [B.bake_spawner(ctx, sp).new_health_raw for _, sp in rows_inputs] == base

    same = tmp_path / "same.sqlite"
    names = _make_sim_db(db_path, same)
    assert {"c_SimulationData__Snapshots", "c_SimulationData__TimeChunkSnapshots"} <= set(names)
    assert "c_SimulationConfig" not in names
    from_sim_db = MD.load_bake_context(db, simulation_data=same)
    # early-list CurrentGpm holds NaN (G19), so compare the bake rather than the objects
    assert [B.bake_spawner(from_sim_db, sp).new_health_raw for _, sp in rows_inputs] == base
    assert len(from_sim_db.simulation_data.snapshots) == len(sd.snapshots)

    doubled = tmp_path / "sim.sqlite"
    _make_sim_db(db_path, doubled, scale_dps=2.0)
    before = _sha(doubled)
    by_path = MD.load_bake_context(db, simulation_data=doubled)
    sim_con = MD.connect(doubled)
    by_con = MD.load_bake_context(db, simulation_data=sim_con)
    sim_con.close()
    by_obj = MD.load_bake_context(db, simulation_data=by_path.simulation_data)
    assert _sha(doubled) == before
    for ctx2 in (by_path, by_con, by_obj):
        got = [B.bake_spawner(ctx2, sp).new_health_raw for _, sp, _ in baked[None]]
        # player DPS enters the Health need linearly, and doubling is exact in binary64
        assert all(g == 2 * b or (math.isinf(g) and math.isinf(b)) for g, b in zip(got, base))
    with pytest.raises(TypeError):
        MD.load_bake_context(db, simulation_data=42)
    with pytest.raises(FileNotFoundError):
        MD.load_bake_context(db, simulation_data=tmp_path / "missing.sqlite")


def test_database_is_read_only(db_path):
    before = _sha(db_path)
    con = MD.connect(db_path)
    try:
        with pytest.raises(sqlite3.OperationalError, match="readonly"):
            con.execute("CREATE TABLE p7_probe (a)")
        ctx = MD.load_bake_context(con)
        armors = MD.load_armor_configs(con)
        for r in MD.load_spawner_rows(con)[:50]:
            B.bake_spawner(ctx, MD.spawner_input_from_row(r, armors))
        assert con.total_changes == 0
    finally:
        con.close()
    assert _sha(db_path) == before


def test_spawner_ids_unique(rows_inputs):
    ids = [sp.spawner_id for _, sp in rows_inputs]
    assert len(ids) == len(set(ids)) == 717
    assert len({sp.key for _, sp in rows_inputs}) == 716


# ----------------------------------------------------------------------------- H9 hook, cost
def test_h9_double_hook(contexts, rows_inputs, load_fixture, monkeypatch):
    """analysis/bake_check.py H9 swaps ``f32`` of bake/interpolation/snapshots for np.float64."""
    ctx = contexts["stepwise"]
    single = [B.bake_spawner(ctx, sp) for _, sp in rows_inputs]
    for mod in (B, I, S):
        monkeypatch.setattr(mod, "f32", np.float64)
    assert isinstance(B._ability_coefficient(ctx, 100.3, 4), np.float64)
    ch = cd = eh = ed = 0
    for (_, sp), rep in zip(rows_inputs, single):
        rr = B.bake_spawner(ctx, sp)
        ch += not (rr.new_health == rep.new_health or (math.isinf(rr.new_health) and math.isinf(rep.new_health)))
        cd += rr.new_damage != rep.new_damage
        eh += rr.new_health == sp.health_base
        ed += rr.new_damage == sp.damage_base
    fx = load_fixture("stage4_summaries")["summaries"]["bake"]["H9_precision"]
    assert {"changed_health": ch, "changed_damage": cd, "double_health_exact": eh, "double_damage_exact": ed} == fx


def test_bake_cost(contexts, rows_inputs):
    ctx = contexts[None]
    for _, sp in rows_inputs:
        B.bake_spawner(ctx, sp)
    times = []
    for _ in range(3):
        t0 = time.perf_counter()
        for _, sp in rows_inputs:
            B.bake_spawner(ctx, sp)
        times.append(time.perf_counter() - t0)
    print(f"\nbake of {len(rows_inputs)} spawners: {statistics.median(times) * 1000:.1f} ms")
    assert min(times) < 1.0
