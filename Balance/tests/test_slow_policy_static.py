"""Slow-phase policy checks without Unity (unit C7): parameter defaults and enums against the C# partial, P10's
resolve()/check_against_csharp on the new fields, the request field and events, and the source facts the Unity
tests rely on (the decomposed time form is farmedTime's own expression; clears only through RebuildApplyClear)."""

from __future__ import annotations

import re

import pytest

from tufu_balance.calibrate import params as P
from tufu_balance.model import simcalc as sc
from tufu_balance.model.data import load_simulation_data
from tufu_balance.unity import engine, request as req, schema, slow_policy as spol

SYSTEMS = P.SIMULATION_SCRIPTS / "Systems"
HARNESS = P.SIMULATION_SCRIPTS / "Harness"


def _slow_fields(cs):
    text = spol.PARAMS_SOURCE.read_text(encoding="utf-8")
    body = text[text.index("public partial class SimulationRebuildParams"):]
    return [m.group(2) for m in re.finditer(r"^\t\tpublic ([\w\[\]]+) (\w+) = ", body, re.M)]


def test_slow_policy_json_equals_the_code_defaults():
    cs = P.read_csharp_params()
    fields = _slow_fields(cs)
    defaults = spol.slow_policy_defaults()
    assert list(defaults) == fields
    assert defaults == {k: v for k, v in P.csharp_defaults(cs).items() if k in fields}
    assert defaults["TimeModel"] == spol.TIME_MODEL["FarmedTime"]
    assert defaults["SlowLethalityGate"] == spol.SLOW_LETHALITY_GATE["SameAsFast"]
    assert defaults["DeathPolicy"] == spol.DEATH_POLICY["SkipSpawner"]
    assert defaults["PriorityFamily"] == spol.PRIORITY_FAMILY["Dynamic"] and defaults["SmartPath"] is True
    assert defaults["CMove"] == defaults["CRegen"] == 1.0  # P14: fixed at 1


def test_enums_match_csharp():
    cs = P.read_csharp_params()
    for name, members in spol.ENUMS.items():
        assert cs.enums[name] == members, name
        assert name not in cs.ambiguous


def test_resolve_and_check_against_csharp():
    resolved = P.resolve(["defaults"], {"TimeModel": "FarmedTimeMoveByDistance", "SlowLethalityGate": "None",
                                        "SmartPath": False, "CMove": 2})
    p = resolved.params
    assert p["TimeModel"] == 1 and p["SlowLethalityGate"] == 2 and p["SmartPath"] is False and p["CMove"] == 2.0
    assert P.check_against_csharp(p, P.read_csharp_params()) == []
    with pytest.raises(P.ParamsError):
        P.resolve(["defaults"], {"TimeModel": "MoveModel"})
    with pytest.raises(P.ParamsError):
        P.resolve(["defaults"], {"SmartPath": 1})
    with pytest.raises(P.ParamsError):
        P.resolve(["defaults"], {"SlowLethalityGate": 7})


def test_request_field_and_events():
    text = (HARNESS / "SimulationRunRequest.SlowPolicy.cs").read_text(encoding="utf-8")
    assert '[JsonProperty("slow_policy_probe", NullValueHandling = NullValueHandling.Ignore)]' in text
    probe = re.search(r"class SlowPolicyProbe\s*\{(.*?)\n\t\t\}", text, re.S).group(1)
    assert set(re.findall(r'\[JsonProperty\("([a-z_]+)"\)\]', probe)) == set(spol.PROBE_FIELDS)
    job = spol.slow_job("x", probe={"selection": True}, params=P.resolve(["defaults"]).params)
    req.validate(req.make_request([job], out_dir="/tmp/x"))
    with pytest.raises(req.RequestError):
        spol.slow_job("x", probe={"selection": True, "bogus": 1})
    for kind in ("slow_fight", "slow_gate", "stat_benefit", "slow_policy_probe"):
        assert kind in schema.EVENT_TYPES
    fight = (SYSTEMS / "SimulationSystem.Fight.cs").read_text(encoding="utf-8")
    start = fight.index('RebuildTraceEvent("slow_fight", ') + len('RebuildTraceEvent("slow_fight", ')
    sent = set(re.findall(r'\("([a-z_]+)", ', fight[start:]))
    assert sent == set(spol.SLOW_FIGHT_FIELDS)


def test_decomposed_form_is_farmed_times_expression():
    """CMove = CRegen = 1 is bit-equal to FarmedTime because the decomposed form keeps farmedTime's operation order
    (CalculateSpawnerData: regenTime + (moveTime + rpm.Item3 * fightTime))."""
    recovered = (SYSTEMS / "SimulationSystem.cs").read_text(encoding="utf-8")
    assert "data.farmedTime = regenTime + (moveTime + rpm.Item3 * data.fightTime);" in recovered
    assert "data.avMoveTime = moveTime;" in recovered and "data.avRegenTime = regenTime;" in recovered
    fight = (SYSTEMS / "SimulationSystem.Fight.cs").read_text(encoding="utf-8")
    assert "(double)RebuildParams.CRegen * data.avRegenTime + (cMove * m + rpm.Item3 * data.fightTime)" in fight


def test_policy_sources_use_engine_hooks_only():
    """The policy files call RebuildApplyClear only in SimulateFightWithSpawner and keep no state fields."""
    fight = (SYSTEMS / "SimulationSystem.Fight.cs").read_text(encoding="utf-8")
    targeting = (SYSTEMS / "SimulationSystem.Targeting.cs").read_text(encoding="utf-8")
    assert "RebuildApplyClear" not in targeting
    body = fight[fight.index("private void SimulateFightWithSpawner("):fight.index("private double CalcDifficultSpawner(")]
    assert fight.count("RebuildApplyClear(") == body.count("RebuildApplyClear(") == 1
    assert engine.engine_defaults().keys().isdisjoint(spol.slow_policy_defaults())


def test_island3_gate_at_the_shipped_tc3_state(db, load_fixture):
    """Design 4.5: at the shipped TC3 state (P8's port of CalculateSpawnerData) the island-3 gate passes the
    fightTime < 300 filter with loss/H about 4.16 (fixture l3_gate_lethality), so SameAsFast (L = 1) and
    CurrentHealth (at full health) refuse it and only None would pick it; on day 5 it is no longer lethal."""
    ctx = sc.load_simcalc_context(db)
    shipped = load_simulation_data(db)
    gate = next(s for s in ctx.spawners if s.d.key.startswith("Spawner_boss_cut_rare"))
    probe = next(p for p in load_fixture("forensics_killdays")["l3_gate_lethality"]["probes"] if p["key_prefix"] == "Spawner_boss_cut_rare")
    for row in probe["days"]:
        if row["day"] not in (3, 5):
            continue
        state = sc.snapshot_state(shipped.get_by_time_chunk_index(row["day"]), ctx)
        data = sc.spawner_data(ctx, state, gate)
        h = state.stat(1)
        assert sc.keeps_fight_time(data)
        if row["day"] == 3:  # day 5 differs with the oracle's K2/K3 lag (0.31 against 0.21), still not lethal
            assert data.loss / h == pytest.approx(row["lossfrac"], rel=0.01)
        same_as_fast = 1.0 * h < data.loss
        current_health = data.loss >= h
        assert same_as_fast == current_health == (row["day"] == 3)
