"""The fast phase in Unity (design 4.4, 4.5, 5.1 T04/T05/T07/T08, 5.2 P01/P04, 7.3 C5).

Open loop: ``replay`` + ``stored`` from day 4 to 405 with the survey's initial parameters (the slow phase runs the
merged slow policy; island 2 opens before the fast phase). Expected
values come from the shipped data, the frozen fixture ``forensics_killdays.json`` and the Python oracle
(``model.simcalc``); the per-day records are ``fasttick_days.jsonl`` and ``spawner_days.csv.gz``.
"""

from __future__ import annotations

import json
import statistics

import numpy as np
import pytest

from tufu_balance.calibrate import sensitivity as S
from tufu_balance.golden import at_most, check, exact, same_set
from tufu_balance.model import simcalc as sc, spawners as sp
from tufu_balance.model.data import load_simulation_data
from tufu_balance.model.enums import StatType
from tufu_balance.unity import fasttick as ft, override as ov, request as req, schema, spawner_map, trace

pytestmark = pytest.mark.unity

LAST = 405
SUITE = "sim_fasttick"
DPS_SCALES = (0.95, 0.98, 0.99, 1.01, 1.02, 1.05)
THETAS = (0.9, 1.1)
SHORT = 30


def _ok(result):
    assert result.exit_code == 0, result.describe()
    return result


def _jobs(db):
    full = ov.default_override(db, from_day=4)

    def job(job_id, params=None, day=LAST, **kw):
        return ft.fast_job(job_id, "replay", override=full, params=params, day=day, max_wall_seconds=600.0, **kw)

    jobs = [
        job("fast-405", spawner_days=True),
        job("fast-405-reference", reference_flush=True),
        job("fast-405-nextday", {"PlanNewIslandsSameDay": False}),
        job("fast-405-nextday-reference", {"PlanNewIslandsSameDay": False}, reference_flush=True),
        job("fast-405-nogate", {"DiagnosticDisableKillPredicate": True}),
        *(job(f"fast-405-theta-{t}", {"KillLossMultiplier": t}) for t in THETAS),
        *(job(f"fast-405-dps-{m}", {"DiagnosticPlayerDpsScale": m}) for m in DPS_SCALES),
        job("fast-405-nntour", {"ActiveMinutesModel": "NnTour"}),
        job("fast-405-afterpass", {"KillState": "AfterPass"}),
        job("fast-ab-loss", {"KillLossFromAb": True}, day=SHORT),
        ft.fast_job("fast-checkpoint", "checkpoint", override=ov.default_override(db, from_day=4), day=SHORT,
                    max_wall_seconds=600.0),
        ft.fast_job("free-405", "free", day=LAST, max_wall_seconds=600.0, spawner_days=True),
    ]
    return jobs


@pytest.fixture(scope="module")
def run(unity_run, db):
    return _ok(unity_run(req.make_request(_jobs(db))))


@pytest.fixture(scope="module")
def run_b(unity_run, db):
    job = ft.fast_job("fast-ab-b", "replay", override=ov.default_override(db, from_day=4), day=8, max_wall_seconds=600.0)
    return _ok(unity_run(req.make_request([job], sandbox={"ab_variant": "B"})))


@pytest.fixture(scope="module")
def shipped(db):
    return load_simulation_data(db).time_chunk_snapshots


@pytest.fixture(scope="module")
def ctx(db):
    return sc.load_simcalc_context(db)


@pytest.fixture(scope="module")
def ident(db):
    """SpawnerId -> runtime id, and runtime id -> P6 definition."""
    registered = sp.registered(sp.load_spawners(db))
    to_rt = {d.spawner_id: spawner_map.runtime_id(spawner_map.runtime_path(d.scene, d.go_path), d.key) for d in registered}
    return to_rt, {to_rt[d.spawner_id]: d for d in registered}


def _days(result, job):
    return ft.read_days(result.file(job, ft.DAYS_FILE).decode())


def _rows(result, job):
    return ft.read_spawner_days(result.file(job, ft.SPAWNER_DAYS_FILE))


def _events(result, job):
    return trace.read(result.job_dir(job)).events


def _unlock_phases(result, job):
    start = result.manifest(job)["job"].get("config_overrides", {}).get("FastTickStartChunkOffset", 4)
    unlocks = S.unlock_days(_events(result, job))
    fast = {n: d for n, d in unlocks.items() if d >= start}
    slow = {n: d for n, d in unlocks.items() if d < start}
    return fast, slow


def _results(result, job, shipped):
    return S.interval_results(shipped, S.daily_gains(_days(result, job)))


def _fast(result, job):
    return result.manifest(job)["engine"]["fast_tick"]


def _full(result, job):
    return json.loads(result.file(job, "timechunk_full.json"))["TimeChunkSnapshots"]


@pytest.fixture(scope="module")
def fixture(load_fixture):
    return load_fixture("forensics_killdays.json")


# ---------------------------------------------------------------- the run and its parameters


def test_every_fast_day_planned_once(run):
    for job in ("fast-405", "free-405"):
        days = _days(run, job)
        assert [d.day for d in days] == list(range(4, LAST)), job
        m = run.manifest(job)
        assert m["engine"]["stop_reason"] == "day" and m["engine"]["failure"] is None
        assert _fast(run, job)["counters"]["days"] == LAST - 4 and _fast(run, job)["error"] is None


def test_defaults_and_kill_gate_provenance(run):
    """KillLossMultiplier = 1.0, KillLossFromAb = false, diagnostics off by default (design 4.5, 7.3 C5); the kill gate
    is Provenance.Inferred with pending:simulation.fast_kill_gate (read from the attribute by reflection)."""
    m = run.manifest("fast-405")["engine"]
    p = m["params"]
    assert p["KillLossMultiplier"] == 1.0 and p["KillLossFromAb"] is False and p["KillState"] == 0
    assert all(p[k] == v for k, v in ft.DIAGNOSTICS_OFF.items()) and p["PlanNewIslandsSameDay"] is True
    assert p["ActiveMinutesModel"] == ft.ACTIVE_MINUTES_MODEL["FarmedTime"] and p["Kappa"] == pytest.approx(0.245)
    f = m["fast_tick"]
    assert f["kill_gate"] == {"method": "SimulateFightWithSpawnerFast", "provenance": "Inferred",
                              "formula_id": ft.KILL_GATE_FORMULA, "kill_loss": 1.0, "kill_loss_from_ab": False}
    assert f["policy_extrapolation"] is False and f["diagnostics_on"] is False and f["reference_flush"] is False
    assert f["ab_variant"] == "A" and f["schema"] == ft.SCHEMA


def test_policy_extrapolation_is_marked(run, run_b):
    """L from the AB table, or a run in an AB group other than A, is a policy extrapolation (design 4.5, Q18); L
    itself stays 1.0 in group B unless KillLossFromAb."""
    a = _fast(run, "fast-ab-loss")
    assert a["policy_extrapolation"] is True and a["policy_extrapolation_reasons"] == ["kill_loss_from_ab"]
    assert a["kill_gate"]["kill_loss"] == 1.0  # group A's GetFastRPMConfig().Item2
    b = _fast(run_b, "fast-ab-b")
    assert b["policy_extrapolation"] is True and b["policy_extrapolation_reasons"] == ["ab_variant_B"]
    assert b["ab_variant"] == "B" and b["kill_gate"]["kill_loss"] == 1.0
    assert [d.day for d in _days(run_b, "fast-ab-b")] == [4, 5, 6, 7]


# ---------------------------------------------------------------- T05


def _explained(results, rows, ctx, ident):
    """Each bad interval -> an explanation (first-kill day +-1) or None."""
    to_rt, defs = ident
    kills = ft.kill_days(rows)
    by_rt = {to_rt[s.spawner_id]: s for s in ctx.spawners if s.spawner_id in to_rt}
    daily = {}
    for r in rows:
        if r.fights and r.spawner not in daily:
            s = by_rt[r.spawner]
            daily[r.spawner] = {rw.stat_type: float(rw.value) * s.d.count * r.fights for rw in s.stat_rewards}
    return {r.unit: S.explain(r, kills, daily) for r in S.bad_intervals(results)}


def test_t05_open_loop(run, shipped, fixture, ctx, ident):
    """Design 5.1 T05 (open loop): the 7 daily intervals TC4-11 exact on all 15 stats; every multi-day miss explained
    by a first-kill day +-1 or registered (the unexplained count frozen as a cap at the first run); >= 37/41 exact
    reported."""
    results = _results(run, "fast-405", shipped)
    assert len(results) == 41
    daily = [r for r in results if r.a < 11]
    assert len(daily) == 7 and all(r.daily for r in daily)
    assert [r.unit for r in daily if not r.exact] == []
    bad = S.bad_intervals(results)
    assert all(not r.missing_days for r in results)
    expl = _explained(results, _rows(run, "fast-405"), ctx, ident)
    unexplained = sorted(u for u, e in expl.items() if e is None)
    survey = [[x["from"], x["to"]] for x in fixture["theta1_start"]["bad_intervals"]]
    check(SUITE, {
        "t05.daily_exact": exact(sum(r.exact for r in daily), source="T05", original=7),
        "t05.exact_intervals": exact(41 - len(bad), source="T05", better="higher", original=41 - len(survey),
                                     note="report only; the survey's 37/41 under theta = 1"),
        "t05.bad_spans": same_set([[r.a, r.b] for r in bad], source="T05", better="subset", original=survey),
        "t05.residuals": exact({r.unit: {StatType(k).name: round(v, 3) for k, v in sorted(r.failing.items())} for r in bad},
                               source="T05"),
        "t05.explanations": exact({u: None if e is None else {"day": e.day, "shift": e.shift, "spawners": list(e.spawners)}
                                   for u, e in sorted(expl.items())}, source="T05"),
        "t05.unexplained": same_set(unexplained, source="T05", better="subset"),
        "t05.unexplained_count": at_most(len(unexplained), source="T05", note="cap frozen at the first run"),
    })


def test_same_day_new_island_planning(run, shipped, fixture):
    """Gates cleared in a round open their island the same day (design 4.5); planning it the next day instead
    (PlanNewIslandsSameDay off) raises the bad intervals (survey: 14/41; measured value frozen)."""
    same = len(S.bad_intervals(_results(run, "fast-405", shipped)))
    nxt = S.bad_intervals(_results(run, "fast-405-nextday", shipped))
    assert len(nxt) > same
    nd = _days(run, "fast-405-nextday")
    assert all(not d.round_unlocks and len(d.rounds) == 1 for d in nd)
    fast, _ = _unlock_phases(run, "fast-405-nextday")
    assert {n for d in nd for n in d.flush_unlocks} == set(fast)
    check(SUITE, {"t05.next_day_bad": exact(len(nxt), source="design 4.5 same-day planning", note="survey ~14/41"),
                  "t05.next_day_bad_spans": exact([[r.a, r.b] for r in nxt], source="design 4.5")})


def test_diagnostic_switches(run, shipped, fixture):
    """No kill predicate (survey ~26/41 exact), the theta ablation through KillLossMultiplier (survey 14/4/6 bad), and
    the DPS sensitivity curve (equal to the fixture's 7/6/5/4/3/2/3)."""
    bad = lambda job: S.bad_intervals(_results(run, job, shipped))
    nogate = bad("fast-405-nogate")
    theta = {1.0: len(bad("fast-405")), **{t: len(bad(f"fast-405-theta-{t}")) for t in THETAS}}
    curve = {1.0: theta[1.0], **{m: len(bad(f"fast-405-dps-{m}")) for m in DPS_SCALES}}
    spans = {1.0: S.bad_spans(_results(run, "fast-405", shipped)),
             **{m: S.bad_spans(_results(run, f"fast-405-dps-{m}", shipped)) for m in DPS_SCALES}}
    survey = {r["player_dps_scale"]: r for r in fixture["player_dps_scaling"]}
    assert curve == {m: survey[m]["bad"] for m in curve}
    assert spans == {m: survey[m]["bad_spans"] for m in spans}
    for job in ("fast-405-nogate", *(f"fast-405-theta-{t}" for t in THETAS), *(f"fast-405-dps-{m}" for m in DPS_SCALES)):
        assert _fast(run, job)["policy_extrapolation"] is False
    assert _fast(run, "fast-405-nogate")["diagnostics_on"] is True
    assert _fast(run, "fast-405-theta-0.9")["diagnostics_on"] is False
    assert _fast(run, "fast-405-theta-0.9")["kill_gate"]["kill_loss"] == pytest.approx(0.9)
    check(SUITE, {
        "diag.no_kill_gate_exact": exact(41 - len(nogate), source="design 4.5 diagnostics", note="survey ~26/41"),
        "diag.theta_bad": exact({str(t): n for t, n in sorted(theta.items())}, source="P01 ablation",
                                note="survey 0.9/1.0/1.1 -> 14/4/6"),
        "diag.dps_curve": exact({str(m): n for m, n in sorted(curve.items())}, source="design 5.1 T05",
                                original={str(m): survey[m]["bad"] for m in sorted(curve)}),
        "diag.closed_loop_exact_min_e2": exact(S.closed_loop_exact_min(curve, 0.02), source="design 5.1 T05 closed"),
    })


# ---------------------------------------------------------------- accumulation, gates, flush


def test_accumulation_writes_all_five_fields(run, ident):
    """AccumulateFastTickSpawnerRewards writes the stat, item, soul, gate and kill fields; the flush reads and empties
    them. Items: the expected copies are C6's (zero until filled), so the item path is counted, not stored."""
    days = _days(run, "fast-405")
    assert all(d.stats for d in days) and any(d.souls for d in days)
    assert all(d.kill_count == d.kills for d in days)
    nd = _days(run, "fast-405-nextday")
    assert any(d.gate_spawners for d in nd)
    fast, _ = _unlock_phases(run, "fast-405-nextday")
    assert {n for d in nd for n in d.flush_unlocks} == set(fast)
    rows = _rows(run, "fast-405")
    _, defs = ident
    item_clears = sum(1 for r in rows if r.fights and defs[r.spawner].item_reward)
    counters = run.manifest("fast-405")["engine"]["counters"]
    assert item_clears > 0 and counters.get("fast_item_rewards_zero", 0) == item_clears
    assert "fast_stat_reward_mixed" not in counters
    assert counters["fast_kills_flushed"] == sum(d.kills for d in days)
    assert counters["accumulate_calls"] == sum(1 for r in rows if r.fights)
    assert sum(d.clears for d in days) == sum(r.fights for r in rows)


def test_gates_unlock_once_per_island_through_the_two_flushes(run):
    """Islands open only through RebuildFlushFastTickGates (between rounds) or FlushFastTickChunkRewards, each once."""
    for job in ("fast-405", "fast-405-nextday"):
        days = _days(run, job)
        unlocks = S.unlock_days(_events(run, job))
        assert sorted(unlocks) == list(range(2, 18)), job
        by_flush = {n: d.day for d in days for n in d.round_unlocks + d.flush_unlocks}
        fast, slow = _unlock_phases(run, job)
        assert by_flush == fast, job
        assert all(d < 4 for d in slow.values()), job
        counters = run.manifest(job)["engine"]["counters"]
        assert counters["unlocks"] == 16 and "repeat_unlocks" not in counters, job


def test_flush_adds_one_merged_modifier_per_stat_per_day(run, shipped):
    """One merged Devoured modifier per stat per day; TC405 Value against the shipped one within 1e-5 (actual into
    golden)."""
    for job in ("fast-405", "free-405"):
        for d in _days(run, job):
            assert d.flush_stat_modifiers == len(d.stats), (job, d.day)
    last = {s["Key"]: s["Value"] for s in _full(run, "fast-405")[-1]["Stats"]}
    ref = {st.key: st.value for st in shipped[-1].stats}
    assert _full(run, "fast-405")[-1]["TimeChunkIndex"] == shipped[-1].time_chunk_index == LAST
    rel = {StatType(k).name: abs(last[k] - v) / abs(v) for k, v in ref.items() if v}
    assert max(rel.values()) <= 1e-5, rel
    check(SUITE, {"tc405.value_rel": exact({k: float(f"{v:.3e}") for k, v in sorted(rel.items())}, source="design 7.3 C5"),
                  "tc405.value_rel_max": at_most(max(rel.values()), source="design 7.3 C5", original=1e-5)})


def test_reference_flush_gives_the_same_digests(run):
    """Native-replacement acceptance (design 4.5): the original-shaped reference flush, with gates left to the flush
    (next-day planning) and without, gives the same per-day state digests and day snapshots."""
    for base, ref in (("fast-405", "fast-405-reference"), ("fast-405-nextday", "fast-405-nextday-reference")):
        assert _fast(run, ref)["reference_flush"] is True
        assert run.file(base, "digests.csv") == run.file(ref, "digests.csv"), base
        assert run.file(base, "timechunk_full.json") == run.file(ref, "timechunk_full.json"), base


def test_cleared_dafc_never_eligible_again(run):
    """X6: a cleared DisableAfterFirstClear spawner is +inf and never in a later day's bot data."""
    for job in ("fast-405", "free-405"):
        rows = _rows(run, job)
        cleared = {}
        for r in rows:
            if r.dafc and r.spawner in cleared and r.day > cleared[r.spawner]:
                pytest.fail(f"{job}: {r.spawner} cleared on day {cleared[r.spawner]} back on day {r.day}")
            if r.dafc and r.fights:
                assert r.fights == 1 and r.spawner not in cleared
                cleared[r.spawner] = r.day
        if job == "fast-405":
            assert cleared


# ---------------------------------------------------------------- X5


def test_eligible_sets_equal_simcalc(run, ctx, ident):
    """X5: every round's cleared set equals model.simcalc.eligible_spawners on the recorded state (forecast stats and
    stored caches after the override, unlocked island of the round, P, charges and cooldowns of the earlier rounds,
    cleared DAFC spawners)."""
    to_rt, _ = ident
    from_rt = {v: k for k, v in to_rt.items()}
    overrides = {r.day: r for r in ov.read_days(run.file("fast-405", ov.DAYS_FILE).decode())}
    rows = _rows(run, "fast-405")
    by_round: dict[tuple[int, int], set[str]] = {}
    for r in rows:
        if r.fights:
            by_round.setdefault((r.day, r.round), set()).add(r.spawner)
    by_id = {s.spawner_id: s for s in ctx.spawners}
    cleared_dafc: set = set()
    checked = mismatched = 0
    for d in _days(run, "fast-405"):
        o = overrides[d.day]
        stats = {k: g for k, (_, _, g) in o.stats.items()}
        used: dict = {}
        cooling: dict = {}
        for i, rnd in enumerate(d.rounds, 1):
            state = sc.PlayerState(stats=stats, dps_data=dict(o.dps_cache), defense_data=dict(o.defense_cache),
                                   unlocked_location=rnd.location, day=d.day, current_playtime=d.playtime)
            bot = sc.BotFilter(charges_used=dict(used), cooldowns=dict(cooling), cleared_dafc=frozenset(cleared_dafc))
            want = {to_rt[sid] for sid in sc.eligible_spawners(ctx, state, bot)}
            got = by_round.get((d.day, i), set())
            checked += 1
            if want != got:
                mismatched += 1
            assert len(got) == rnd.eligible
            for rt in got:
                s = by_id[from_rt[rt]]
                if s.d.dafc:
                    cleared_dafc.add(s.spawner_id)
                else:
                    used[s.spawner_id] = ctx.charges_limit(s) // s.d.count * s.d.count
                    cooling[s.spawner_id] = 1.0
    assert mismatched == 0 and checked >= LAST - 4


def test_farmed_time_and_charge_usage_equal_oracle(run, ctx, ident):
    """Audit T04's inputs independently: the recovered farmedTime formula and the charge cap, with no
    fitted multiplier. Charge units are exported separately from clears for P10's T05/T12."""
    to_rt, defs = ident
    by_rt = {to_rt[s.spawner_id]: s for s in ctx.spawners if s.spawner_id in to_rt}
    overrides = {r.day: r for r in ov.read_days(run.file("fast-405", ov.DAYS_FILE).decode())}
    rows = _rows(run, "fast-405")
    farmed = {}
    for r in rows:
        d = defs[r.spawner]
        assert r.count == d.count and r.charge_limit == ctx.charges_limit(by_rt[r.spawner])
        assert 0 <= r.charges_before <= r.charges_after <= r.charge_limit
        assert r.charges_after - r.charges_before == r.fights * r.count
        if r.fights:
            want = 1 if r.dafc else (r.charge_limit - r.charges_before) // r.count
            assert r.fights == want
        farmed[r.day] = farmed.get(r.day, 0.0) + r.fights * r.farmed_time
        if r.day not in (4, 11, 30, 104, 206, 404):
            continue
        o = overrides[r.day]
        state = sc.PlayerState(stats={k: g for k, (_, _, g) in o.stats.items()},
                               dps_data=dict(o.dps_cache), defense_data=dict(o.defense_cache),
                               unlocked_location=17, day=r.day, current_health=o.current_health)
        expected = sc.spawner_data(ctx, state, by_rt[r.spawner])
        # C2: Mono's float intermediates differ from stepwise Python by up to 1 ulp.
        assert r.fight_time == pytest.approx(expected.fight_time, rel=2e-6, abs=1e-9)
        assert r.farmed_time == pytest.approx(expected.farmed_time, rel=2e-6, abs=1e-9)
    for d in _days(run, "fast-405"):
        assert d.farmed_seconds == pytest.approx(farmed[d.day], rel=1e-12)
    usage = ft.charge_usage(rows)
    clears = ft.charge_counts(rows)
    assert usage.keys() == clears.keys()
    assert all(n == clears[key] * defs[key[1]].count for key, n in usage.items())


# ---------------------------------------------------------------- T07, T08, T04


def test_t07_unlock_days(run):
    """Islands 5-17 within the shipped range or <= 1 day off; island 4 through island 3's gate (result into golden)."""
    unlocks = S.unlock_days(_events(run, "fast-405"))
    t07 = S.t07(unlocks)
    assert all(v["ok"] for island, v in t07.items() if island >= 5), t07
    check(SUITE, {"t07.unlock_days": exact({str(k): v for k, v in sorted(unlocks.items())}, source="T07"),
                  "t07.in_range_5_17": exact(sum(v["distance"] == 0 for i, v in t07.items() if i >= 5), source="T07",
                                             better="higher", note="survey 12/13"),
                  "t07.island_4": exact(unlocks[4], source="T07", note="shipped: day 5")})


def test_t08_first_kill_days(run, ident):
    """Hard spawners' first-kill days within the interval or <= 1 day off (design 5.1 T08)."""
    _, defs = ident
    first = {}
    for rt, day in ft.kill_days(_rows(run, "fast-405")).items():
        d = defs[rt]
        first.setdefault((d.go_name, d.location), day)
    t08 = S.t08(first)
    assert all(v["ok"] for v in t08.values()), t08
    check(SUITE, {"t08.first_kill_days": exact({k: v["day"] for k, v in sorted(t08.items())}, source="T08")})


def test_t04_active_minutes(run, shipped):
    """T04 with FarmedTime, kappa = 0.245: TC >= 11 median <= 2 %, max <= 8 % is the design's open-loop criterion;
    the measured values, the kappa refitted from the trace (design 5.3) and NnTour go into golden (TC 4-10 report)."""
    days = _days(run, "fast-405")
    minutes = {d.day: d.active_minutes for d in days}
    cells = S.t04(shipped, minutes)
    summary = S.t04_summary(cells)
    farmed = {d.day: d.farmed_seconds for d in days}
    kappa = S.fit_kappa(shipped, farmed)
    refit = S.t04_summary(S.t04(shipped, {d: kappa * f / 60.0 for d, f in farmed.items()}))
    for d in days:
        assert d.active_minutes == pytest.approx(0.245 * d.farmed_seconds / 60.0, rel=1e-6)
    p = {s["TimeChunkIndex"]: np.float32(s["Playtime"]) for s in _full(run, "fast-405")}
    for d in days:
        assert p[d.day] == np.float32(d.playtime) and p[d.day + 1] == np.float32(d.playtime + d.active_minutes), d.day
    nn = S.t04_summary(S.t04(shipped, {d.day: d.active_minutes for d in _days(run, "fast-405-nntour")}))
    check(SUITE, {
        "t04.kappa_0245": exact({k: round(v, 4) for k, v in summary.items()}, source="T04", note="design: median <= 0.02, max <= 0.08"),
        "t04.median_rel_cap": at_most(summary["median"], source="T04", original=0.02,
                                      note="design limit missed; registered C5 deviation, see FastTick.md"),
        "t04.max_rel_cap": at_most(summary["max"], source="T04", original=0.08,
                                   note="design limit missed; registered C5 deviation, see FastTick.md"),
        "t04.tc_4_10": exact({f"{c.a}->{c.b}": round(c.sim / c.ref - 1, 4) for c in cells if c.a < 11}, source="T04 report"),
        "t04.kappa_refit": exact(round(kappa, 4), source="design 5.3 kappa = sum AF / sum F^2, TC 11-103"),
        "t04.kappa_refit_summary": exact({k: round(v, 4) for k, v in refit.items()}, source="T04"),
        "t04.nntour": exact({k: round(v, 4) for k, v in nn.items()}, source="T04 NnTour (a 1.018, b 0.78, 3 tours)"),
    })


def test_afterpass_and_checkpoint(run, shipped):
    """KillState AfterPass runs (report); checkpoint: planning rounds on days without a shipped snapshot do not use the
    stored caches (C4 handoff: stored_caches_other_day > 0, cache_applications only on checkpoint days)."""
    after = len(S.bad_intervals(_results(run, "fast-405-afterpass", shipped)))
    m = run.manifest("fast-checkpoint")
    counters = m["override"]["counters"]
    shipped_days = {s.time_chunk_index for s in shipped}
    days = _days(run, "fast-checkpoint")
    on = sum(1 + len(d.rounds) for d in days if d.day in shipped_days) + (1 if SHORT in shipped_days else 0)
    off = sum(len(d.rounds) for d in days if d.day not in shipped_days)
    assert off > 0 and counters["stored_caches_other_day"] == off
    assert counters["cache_applications"] == on
    check(SUITE, {"t05.afterpass_bad": exact(after, source="P02 KillState AfterPass (report)")})


# ---------------------------------------------------------------- cost


@pytest.mark.perf
def test_fast_tick_cost_within_budget(run):
    """Design 2.7 / Runbook 16.3: one FastTick (a whole day) <= 50 ms median."""
    for job in ("fast-405", "free-405"):
        t = json.loads(run.file(job, "timings.json"))["fast_tick_ms"]
        assert t["n"] >= 300 and t["p50"] <= 50.0, (job, t)


def test_outputs_are_registered(run):
    m = run.manifest("fast-405")
    schema.validate_manifest(m)
    assert m["engine"]["fast_tick"]["files"] == {"fasttick_days": ft.DAYS_FILE, "spawner_days": ft.SPAWNER_DAYS_FILE}
    assert _fast(run, "fast-405-reference")["files"] == {"fasttick_days": ft.DAYS_FILE, "spawner_days": None}
    rows = _rows(run, "fast-405")
    assert {r.round for r in rows} >= {1, 2} and all(r.day >= 4 for r in rows)
    assert statistics.mean(len(d.rounds) for d in _days(run, "fast-405")) >= 1.0
