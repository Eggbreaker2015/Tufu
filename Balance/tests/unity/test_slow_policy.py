"""The slow-phase policy in Unity (design 4.5 "目标选择"/"战斗", 5.2 P13-P15, 7.3 C7): first clears of a new save,
selection (gate, ties, null), the time models, health, the lethality gate around the island-3 gate, deaths,
SmartPath, CalculateStatBenefit's cadence, and the per-tick cost.

Every job is a free run of days 0-3 (stop after day 4's forced snapshot; FastTick is still C3b's minimal one) with
the resolved default parameters (``calibrate.params.resolve``) plus one override. Per-fight facts come from the
``slow_fight`` events (``tufu_balance.unity.slow_policy``), checked against the recovered formulas and the
database, never against values of the code under test.
"""

from __future__ import annotations

import json
import math
from collections import Counter

import pytest

from tufu_balance.calibrate import params as P
from tufu_balance.golden import check, exact
from tufu_balance.model import simcalc as sc
from tufu_balance.model.f32 import f32
from tufu_balance.unity import engine, request as req, slow_policy as spol, spawner_map, trace

pytestmark = pytest.mark.unity

DAY = 4  # stop after day 4's forced snapshot: the whole slow phase (days 0-3)
ISLAND3_GATE_LOSS_PER_H = 4.163  # forensics_killdays.json l3_gate_lethality, shipped TC3 state (design 4.5)
VARIANTS = {
    "slow-default": {},
    "slow-decomposed": {"TimeModel": spol.TIME_MODEL["Decomposed"]},
    "slow-bydist": {"TimeModel": spol.TIME_MODEL["FarmedTimeMoveByDistance"]},
    "slow-static": {"PriorityFamily": spol.PRIORITY_FAMILY["Static"]},
    "slow-current-health": {"SlowLethalityGate": spol.SLOW_LETHALITY_GATE["CurrentHealth"]},
    "slow-none": {"SlowLethalityGate": spol.SLOW_LETHALITY_GATE["None"]},
    "slow-none-retry": {"SlowLethalityGate": spol.SLOW_LETHALITY_GATE["None"], "DeathPolicy": spol.DEATH_POLICY["Retry"]},
    "slow-nosmart": {"SmartPath": False},
    "slow-marginal": {"StatBenefitModel": spol.STAT_BENEFIT_MODEL["MarginalKillTime"]},
}
NULL = engine.engine_probe(null_policy=True)
ALL = 17


def _params(overrides=None):
    return P.resolve(["defaults"], overrides or {}).params


def _jobs():
    jobs = [spol.slow_job(job_id, day=DAY, max_wall_seconds=600.0, params=_params(over),
                          probe={"selection": True} if job_id == "slow-default" else None)
            for job_id, over in VARIANTS.items()]
    jobs += [
        spol.slow_job("perf-null", day=DAY, max_wall_seconds=600.0, params=_params(), engine_probe=NULL),
        spol.slow_job("perf-null-all", day=DAY, max_wall_seconds=600.0, params=_params(),
                      engine_probe=engine.engine_probe(null_policy=True, unlock_through=ALL)),
        spol.slow_job("perf-policy-all", day=DAY, max_wall_seconds=600.0, params=_params(),
                      engine_probe=engine.engine_probe(unlock_through=ALL)),
    ]
    return jobs


@pytest.fixture(scope="module")
def run(unity_run):
    result = unity_run(req.make_request(_jobs()))
    assert result.exit_code == 0, result.describe()
    return result


GATES = {"gate-same-as-fast": "SameAsFast", "gate-current-health": "CurrentHealth", "gate-none": "None"}


# CutDamageFlat granted before day 0 (engine_probe.grant_flat): without C6's ProcessInventory the closed loop keeps
# the starting knife only, and the island-3 gate's fightTime stays above 300 s (dropped by the bot filter); the
# shipped TC3 state has 1143 Cut DpsData from seven weapons. The grant gives a comparable Cut DPS.
GATE_GRANT = {8: 1200.0}


def _gate_jobs():
    """Islands 2-3 open from the start (engine_probe.unlock_through, no slot rewards): currentBoss is the island-3
    gate (OT 240), which passes the bot filter once P >= 240, late on day 3, as in the shipped run (TC3)."""
    return [spol.slow_job(job_id, day=DAY, max_wall_seconds=600.0,
                          params=_params({"SlowLethalityGate": spol.SLOW_LETHALITY_GATE[gate]}),
                          engine_probe=engine.engine_probe(unlock_through=3, grant_flat=GATE_GRANT))
            for job_id, gate in GATES.items()]


@pytest.fixture(scope="module")
def gate_run(unity_run):
    result = unity_run(req.make_request(_gate_jobs()))
    assert result.exit_code == 0, result.describe()
    return result


@pytest.fixture(scope="module")
def gate_traces(gate_run):
    return {job["job_id"]: trace.read(gate_run.job_dir(job["job_id"])) for job in _gate_jobs()}


@pytest.fixture(scope="module")
def traces(run):
    return {job["job_id"]: trace.read(run.job_dir(job["job_id"])) for job in _jobs()}


@pytest.fixture(scope="module")
def ctx(db):
    return sc.load_simcalc_context(db)


@pytest.fixture(scope="module")
def ident(db):
    return spawner_map.load_identity_map(db)


@pytest.fixture(scope="module")
def by_runtime(ctx, ident):
    by_id = {s.spawner_id: s for s in ctx.spawners}
    return {e.runtime_id: by_id.get(e.spawner_id) for e in ident.entries}


def _events(traces, job, kind):
    return [e for e in traces[job].events if e["type"] == kind]


def _counters(run, job):
    return run.manifest(job)["engine"]["counters"]


def _slow_phase(fights):
    return [f for f in fights if f["day"] < DAY]


def _reward(s: sc.SimSpawner, stat: int) -> float:
    return sum(float(r.value) * s.d.count for r in s.stat_rewards if r.stat_type == stat)


# ---------------------------------------------------------------- a new save's first clears

@pytest.mark.parametrize("job", ["slow-default", "slow-bydist"])
def test_first_clears_are_the_ot0_spawners_once_each(traces, by_runtime, ctx, job):
    """Design 7.3 C7: from a new save every OT = 0 spawner is killed once (Health +15, Cut +3) before anything
    else. The active minutes this takes go to the golden suite; the design's "within 1.17 minutes" (the shipped
    first snapshot) is not reachable by the recovered time forms (killseq: sum of farmedTime 232 s, of
    rpm.Item3 * fightTime alone 79.8 s, against 69.9 s; Runbook 19)."""
    ot0 = {s.spawner_id for s in ctx.spawners if s.d.location == 1 and s.d.optimum_time == 0.0
           and not (s.d.is_event or s.d.is_vip or s.d.is_dummy)}
    assert len(ot0) == 6
    cleared = [f for f in _events(traces, job, "slow_fight") if f["cleared"]]
    first = [by_runtime[f["spawner"]] for f in cleared[:6]]
    assert {s.spawner_id for s in first} == ot0 and len({s.spawner_id for s in first}) == 6
    assert sum(_reward(s, 1) for s in first) == 15.0 and sum(_reward(s, 8) for s in first) == 3.0
    assert not any(f["died"] for f in _events(traces, job, "slow_fight")[:6])
    minutes = float(f32(cleared[5]["p_after"]))
    check("sim_slow", {f"first_clears.{job}.minutes": exact(round(minutes, 4), source="design 7.3 C7",
                                                            note="shipped: 1.1656 (first early snapshot)")})


# ---------------------------------------------------------------- selection

def test_selection_ties_null_and_order(traces):
    """FindBestSpawner's selection on the live spawnerData of the first tick: equal priorities give the first
    in sortedSpawners order, a tie of the first and the last gives the first, nothing allowed or nothing eligible
    gives null; spawnerData is in sortedSpawners order and is left as it was."""
    [p] = _events(traces, "slow-default", "slow_policy_probe")
    eligible = p["eligible"].split("\n")
    assert len(eligible) == 6 and p["sorted_eligible"] == p["eligible"]
    assert p["tie"] == eligible[0] and p["tie_first_last"] == eligible[0]
    assert p["none_allowed_is_null"] and p["empty_is_null"] and p["restored"]
    first = _events(traces, "slow-default", "slow_fight")[0]
    assert p["best"] in eligible and first["kind"] in ("target", "en_route")


@pytest.mark.parametrize("job", list(VARIANTS))
def test_idle_only_when_nothing_is_allowed(run, job):
    """A slow tick idles exactly when FindBestSpawner returns null (nothing eligible, or nothing allowed)."""
    c = _counters(run, job)
    nulls = c.get("slow_pick_none_eligible", 0) + c.get("slow_pick_none_allowed", 0)
    assert c.get("idle_steps", 0) == nulls > 0


@pytest.mark.parametrize("job", ["slow-default", "slow-bydist", "slow-nosmart", "slow-current-health"])
def test_the_gate_is_chosen_whenever_it_is_allowed(run, traces, job):
    """currentBoss's reward value 1e6: whenever the gate passes the bot filter and the lethality gate, it is the
    pick (counted every tick; until then the gate refuses it). The unlock days are closed-loop results for K2
    (shipped: island 2 on day 0, island 3 on day 1)."""
    c = _counters(run, job)
    assert c.get("slow_boss_allowed_not_chosen", 0) == 0
    assert c.get("slow_boss_chosen", 0) >= 1 and c.get("slow_boss_refused", 0) > 0
    unlocks = {e["location"]: e["day"] for e in _events(traces, job, "unlock")}
    assert 2 in unlocks
    check("sim_slow", {f"unlock_days.{job}": exact({str(k): v for k, v in sorted(unlocks.items())}, source="design 7.3 C7",
                                                    note="shipped: island 2 on day 0, island 3 on day 1")})


def test_static_and_dynamic_agree_under_constant_benefits(run):
    """PriorityFamily is switchable; with statsBenefit constant 1 both families rank alike: same run."""
    for name in ("simulation_data.json", "timechunk_full.json", "digests.csv"):
        assert run.file("slow-static", name) == run.file("slow-default", name), name


# ---------------------------------------------------------------- time models

def _p_t_step(f):
    """P and T are float32 in the events (shortest round-trip decimals): widen the float32 value, add in double,
    round once."""
    dt = f["seconds"] / 60.0
    return (f32(float(f32(f["p_before"])) + dt) == f32(f["p_after"])
            and f32(float(f32(f["t_before"])) + dt) == f32(f["t_after"]))


def test_farmed_time_advances_p_and_t_by_farmed_time(traces):
    """TimeModel.FarmedTime: a target fight lasts farmedTime, an en-route fight farmedTime - avMoveTime;
    P and T += seconds / 60 in float32 (one rounding from double, as the editor's Mono evaluates it)."""
    fights = [f for f in _slow_phase(_events(traces, "slow-default", "slow_fight")) if not f["died"]]
    assert len(fights) > 100
    for f in fights:
        expected = f["farmed_time"] if f["kind"] == "target" else f["farmed_time"] - f["av_move_time"]
        assert f["seconds"] == expected, f
        assert _p_t_step(f), f
    assert any(f["p_after"] != round(f["p_after"]) for f in fights)  # non-integer P: the float32 rounding is exercised


def test_move_by_distance(traces):
    """FarmedTimeMoveByDistance: dt = farmedTime - avMoveTime + GetMoveTime(currentPosition); the move is the
    distance from the previous fight's spawner (the origin at the start and after a death) at MovementSpeed
    (avMoveTime = 70 / speed)."""
    fights = _slow_phase(_events(traces, "slow-bydist", "slow_fight"))
    prev = None
    checked = 0
    for f in fights:
        if f["kind"] == "target" and not f["died"]:
            assert f["seconds"] == f["farmed_time"] - f["av_move_time"] + f["distance_time"], f
            assert f["move_time"] == f["distance_time"]
            dist = math.hypot(f["to_x"] - f["from_x"], f["to_z"] - f["from_z"])
            assert math.isclose(f["distance_time"], dist * f["av_move_time"] / 70.0, rel_tol=1e-3, abs_tol=0.05), f
            checked += 1
        assert _p_t_step(f), f
        if prev is not None and not prev["died"] and f["kind"] == "target" and prev["kind"] == "target":
            assert (f["from_x"], f["from_z"]) == (prev["to_x"], prev["to_z"])
        if prev is not None and prev["died"]:
            assert (f["from_x"], f["from_z"]) == (0.0, 0.0)
        prev = f
    assert checked > 100
    assert fights[0]["from_x"] == 0.0 and fights[0]["from_z"] == 0.0


def test_decomposed_with_unit_factors_is_farmed_time_bit_for_bit(run):
    """CMove = CRegen = 1: CRegen*avRegenTime + (CMove*avMoveTime + rpm.Item3*fightTime) is farmedTime's own
    expression, so the whole run is identical."""
    for name in ("simulation_data.json", "timechunk_full.json", "digests.csv", "daily.csv"):
        assert run.file("slow-decomposed", name) == run.file("slow-default", name), name
    assert _counters(run, "slow-decomposed") == _counters(run, "slow-default")


# ---------------------------------------------------------------- health

@pytest.mark.parametrize("job", ["slow-default", "slow-bydist", "slow-none"])
def test_health_changes(traces, by_runtime, job):
    """CurrentHealth += healthDelta, then ApplyRegen over the fight: min(H, CH + regen * seconds); a clear's flat
    Health reward adds to it (RebuildApplyClear); a death refills to H (ProcessDeath)."""
    n = 0
    for f in _slow_phase(_events(traces, job, "slow_fight")):
        if f["died"]:
            assert f["health_before"] + f["health_delta"] <= 0.0
            assert f["health_after"] >= f["health_after_fight"]
            continue
        expected = min(f["max_health"], (f["health_before"] + f["health_delta"]) + f["regen"] * f["seconds"])
        assert f["health_after_fight"] == expected, f
        s = by_runtime[f["spawner"]]
        reward = float(f32(_reward(s, 1))) if f["cleared"] else 0.0
        assert f["health_after"] == f["health_after_fight"] + reward, f
        n += 1
    assert n > 100


# ---------------------------------------------------------------- lethality gate and deaths

def _island3_gate(events):
    return [e for e in events if e["location"] == 3]


def test_lethality_gate_at_the_island3_gate(gate_run, gate_traces):
    """On day 3 the island-3 gate (OT 240) passes the bot filter but is lethal (shipped TC3 state: loss/H 4.16):
    SameAsFast and CurrentHealth never pick it, None does (and the player dies); the recorded predicates are the
    design's. The state is the closed loop's with islands 2-3 open and GATE_GRANT (the shipped TC3 state needs C4's
    replay and C6's items; tests/test_slow_policy_static.py checks the shipped state with the oracle)."""
    first = {}
    for job, gate in GATES.items():
        gates = _island3_gate(_events(gate_traces, job, "slow_gate"))
        assert gates, f"{job}: the island-3 gate never passed the bot filter in the slow phase"
        for g in gates:
            assert g["refused_same_as_fast"] == (g["kill_loss_multiplier"] * g["max_health"] < g["loss"])
            assert g["refused_current_health"] == (g["loss"] >= g["current_health"])
            assert g["day"] == 3 and float(f32(g["playtime"])) >= 240.0
        g = first[job] = gates[0]
        assert g["loss"] / g["max_health"] > 1.0 and g["refused_same_as_fast"] and g["refused_current_health"]
        chosen = any(x["chosen"] for x in gates)
        assert chosen == (gate == "None"), job
        if gate != "None":
            assert _counters(gate_run, job).get("slow_boss_chosen", 0) == 0
    gate_deaths = [f for f in _events(gate_traces, "gate-none", "slow_fight") if f["died"] and f["boss"] and f["location"] == 3]
    assert gate_deaths and _counters(gate_run, "gate-none")["respawns"] >= len(gate_deaths)
    for job in GATES:
        assert 4 not in {e["location"] for e in _events(gate_traces, job, "unlock")}
    g = first["gate-same-as-fast"]
    check("sim_slow", {
        "island3_gate.first_loss_per_h": exact(round(g["loss"] / g["max_health"], 3), source="design 4.5",
                                               note=f"closed loop, islands 2-3 open; shipped TC3 state {ISLAND3_GATE_LOSS_PER_H}"),
        "island3_gate.first_p": exact(round(float(f32(g["playtime"])), 3), source="design 4.5"),
        "island3_gate.none_gate_deaths": exact(len(gate_deaths), source="design 4.5 P15"),
    })


@pytest.mark.parametrize("job,skip", [("slow-none", True), ("slow-none-retry", False)])
def test_deaths_count_respawns_by_death_policy(run, traces, job, skip):
    """ProcessDeath: countRespawn (counter respawns) per death event, full health, back to the origin; SkipSpawner
    skips the spawner for the rest of the session, Retry does not; a lost fight is no clear."""
    deaths = [f for f in _events(traces, job, "slow_fight") if f["died"]]
    assert deaths
    assert _counters(run, job).get("respawns", 0) == len(deaths) == len(_events(traces, job, "death"))
    assert not any(f["cleared"] for f in deaths)
    per_session = Counter((f["session"], f["spawner"]) for f in deaths)
    if skip:
        assert max(per_session.values()) == 1
    check("sim_slow", {f"deaths.{job}": exact(len(deaths), source="design 4.5 P15")})


def test_death_policy_changes_the_run(run):
    """None lets the bot pick lethal fights; SameAsFast still loses fights it starts below full health (its
    predicate reads maximum health, CurrentHealth's reads the current one)."""
    assert run.file("slow-none", "digests.csv") != run.file("slow-none-retry", "digests.csv")
    deaths = {job: _counters(run, job).get("respawns", 0) for job in ("slow-default", "slow-current-health", "slow-none")}
    assert deaths["slow-none"] > deaths["slow-default"] >= deaths["slow-current-health"]
    check("sim_slow", {"deaths.by_gate": exact(deaths, source="design 4.5 P15")})


# ---------------------------------------------------------------- SmartPath

def test_smart_path_clears_en_route_spawners(run, traces, by_runtime):
    """SmartPath: eligible spawners within SmartPathRadius of the segment to the target are cleared on the way
    (countNeighbFight, counter slow_en_route_clears); switched off, there are none."""
    radius = P.resolve(["defaults"]).params["SmartPathRadius"]
    on = [f for f in _events(traces, "slow-default", "slow_fight") if f["kind"] == "en_route"]
    assert on and _counters(run, "slow-default")["slow_en_route_clears"] == sum(f["cleared"] for f in on)
    fights = _events(traces, "slow-default", "slow_fight")
    for i, f in enumerate(fights):
        if f["kind"] != "en_route":
            continue
        target = next(t for t in fights[i:] if t["kind"] == "target")
        ax, az, bx, bz = target["from_x"], target["from_z"], target["to_x"], target["to_z"]
        px, pz = f["to_x"], f["to_z"]
        L2 = (bx - ax) ** 2 + (bz - az) ** 2
        t = 0.0 if L2 == 0 else max(0.0, min(1.0, ((px - ax) * (bx - ax) + (pz - az) * (bz - az)) / L2))
        assert math.hypot(px - (ax + t * (bx - ax)), pz - (az + t * (bz - az))) <= radius + 0.05, f
    off = _counters(run, "slow-nosmart")
    assert not [f for f in _events(traces, "slow-nosmart", "slow_fight") if f["kind"] == "en_route"]
    assert off.get("slow_en_route_clears", 0) == 0 and off.get("smart_path_scans", 0) == 0
    check("sim_slow", {"smart_path.en_route_clears": exact(len(on), source="design 4.5")})


# ---------------------------------------------------------------- CalculateStatBenefit

@pytest.mark.parametrize("job", ["slow-default", "slow-marginal"])
def test_stat_benefit_once_per_session_or_day(run, traces, job):
    """CalculateStatBenefit evaluates once per (session, day) the slow phase plans in, never per tick."""
    evals = _events(traces, job, "stat_benefit")
    keys = [(e["session"], e["day"]) for e in evals]
    assert len(keys) == len(set(keys)) == _counters(run, job)["stat_benefit_evals"]
    planned = {(f["session"], f["day"]) for f in _slow_phase(_events(traces, job, "slow_fight"))}
    assert planned <= set(keys)
    sessions = {k[0] for k in keys}
    assert len(keys) <= len(sessions) + DAY
    assert _counters(run, job)["slow_ticks"] > 5 * len(keys)
    if job == "slow-marginal":
        assert any(e["benefits"] for e in evals) and all(e["candidates"] > 0 for e in evals if e["benefits"])
    else:
        assert not any(e["benefits"] for e in evals)


# ---------------------------------------------------------------- write point and cost

@pytest.mark.parametrize("job", list(VARIANTS))
def test_clears_go_through_the_write_point(run, traces, job):
    """Every clear of the run is a cleared slow fight (FastTick is minimal here), counted once by RebuildApplyClear."""
    fights = _events(traces, job, "slow_fight")
    c = _counters(run, job)
    assert c.get("clears", 0) == sum(f["cleared"] for f in fights)
    assert c.get("slow_fights", 0) + c.get("slow_deaths", 0) == len(fights)


def test_summary_counters(run):
    rows = {}
    for job in VARIANTS:
        c = _counters(run, job)
        rows[job] = {k: c.get(k, 0) for k in ("slow_ticks", "idle_steps", "slow_fights", "slow_deaths", "clears",
                                              "slow_en_route_clears", "unlocks", "stat_benefit_evals")}
    check("sim_slow", {"counters": exact(rows, source="design 7.3 C7")})


@pytest.mark.perf
def test_full_policy_tick_within_budget(run):
    """Runbook 16.3: with the full policy a slow tick (median) stays within 3x the empty policy at the same unlock
    state and within 15 ms."""
    t = {job: json.loads(run.file(job, "timings.json"))["slow_tick_ms"]
         for job in ("slow-default", "perf-null", "perf-policy-all", "perf-null-all", "slow-marginal")}
    print(json.dumps(t))
    for policy, empty in (("slow-default", "perf-null"), ("perf-policy-all", "perf-null-all"), ("slow-marginal", "perf-null")):
        assert t[policy]["p50"] <= max(3.0 * t[empty]["p50"], 0.5) and t[policy]["p50"] <= 15.0, (policy, t)


def test_params_resolve_against_the_csharp_class():
    """The resolved defaults are the complete C# object; every job's params passed the request checks."""
    cs = P.read_csharp_params()
    for over in VARIANTS.values():
        assert P.check_against_csharp(_params(over), cs) == []
