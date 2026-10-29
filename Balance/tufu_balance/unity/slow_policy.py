"""Slow-phase policy request field, events and parameter enums (design 4.5, 5.2 P13-P15; unit C7).

* ``job.slow_policy_probe`` (C# ``SimulationRunRequest.SlowPolicy.cs``): ``{"selection": true}`` runs the
  selection checks at the run's first ``FindBestSpawner`` (equal priorities, nothing allowed, nothing eligible)
  and records event ``slow_policy_probe``; tests only, null in real runs.
  It also records ``slow_selection`` candidate costs at every tick with an eligible boss.
* events (``events.jsonl``): ``slow_fight`` per slow fight (en-route fights of SmartPath included),
  ``slow_gate`` the first boss decision of each session, ``slow_target_refused`` when the en-route fights left
  the target refused by the gate (the tick ends before it), ``stat_benefit`` per ``CalculateStatBenefit``
  evaluation, ``slow_policy_probe``.
* ``sim/params/defaults/slow_policy.json``: the fields of ``SimulationRebuildParams.SlowPolicy.cs``.
"""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from . import engine, request as req, schema

BALANCE = Path(__file__).resolve().parents[2]
SLOW_POLICY_DEFAULTS = BALANCE / "sim" / "params" / "defaults" / "slow_policy.json"
PARAMS_SOURCE = engine.PARAMS_SOURCE.parent / "SimulationRebuildParams.SlowPolicy.cs"

PROBE_FIELDS = {"selection": (bool,)}

req.register_fields("job", {"slow_policy_probe": (dict, None)})
req.register_fields("slow_policy_probe", PROBE_FIELDS)

SLOW_FIGHT_FIELDS = {
    "spawner": "str", "location": "int", "kind": "str", "session": "int", "boss": "bool",
    "p_before": "float", "t_before": "float", "p_after": "float", "t_after": "float", "seconds": "float",
    "fight_time": "float", "farmed_time": "float", "av_move_time": "float", "av_regen_time": "float",
    "move_time": "float", "distance_time": "float", "health_delta": "float", "max_health": "float",
    "health_before": "float", "health_after_fight": "float", "health_after": "float", "regen": "float",
    "died": "bool", "cleared": "bool", "priority": "float?", "en_route": "int",
    "from_x": "float", "from_z": "float", "to_x": "float", "to_z": "float",
}
schema.register_event("slow_fight", SLOW_FIGHT_FIELDS)
schema.register_event("slow_gate", {
    "spawner": "str", "location": "int", "session": "int", "loss": "float", "max_health": "float",
    "current_health": "float", "fight_time": "float", "kill_loss_multiplier": "float",
    "refused_same_as_fast": "bool", "refused_current_health": "bool", "skipped": "bool", "chosen": "bool"})
schema.register_event("slow_selection", {
    "boss": "str", "best": "str", "max_health": "float", "current_health": "float",
    "kill_loss_multiplier": "float", "candidates": "str"})
schema.register_event("stat_benefit", {"session": "int", "model": "int", "candidates": "int", "benefits": "str"})
schema.register_event("slow_policy_probe", {
    "eligible": "str", "sorted_eligible": "str", "tie": "str", "tie_first_last": "str",
    "none_allowed_is_null": "bool", "empty_is_null": "bool", "restored": "bool", "best": "str",
    "boss_stat_keeps_gate": "bool"})
schema.register_event("slow_target_refused", {
    "spawner": "str", "session": "int", "loss": "float", "max_health": "float", "current_health": "float",
    "en_route": "int", "from_x": "float", "from_z": "float", "to_x": "float", "to_z": "float"})

# SimulationRebuildParams.SlowPolicy.cs enums; JSON carries the integers.
PRIORITY_FAMILY = {"Static": 0, "Dynamic": 1}
TIME_MODEL = {"FarmedTime": 0, "FarmedTimeMoveByDistance": 1, "Decomposed": 2, "DecomposedMoveByDistance": 3}
SLOW_LETHALITY_GATE = {"SameAsFast": 0, "CurrentHealth": 1, "None": 2}
DEATH_POLICY = {"SkipSpawner": 0, "Retry": 1, "ClearThenRespawn": 2}
STAT_BENEFIT_MODEL = {"Constant": 0, "MarginalKillTime": 1}
ENUMS = {"PriorityFamily": PRIORITY_FAMILY, "TimeModel": TIME_MODEL, "SlowLethalityGate": SLOW_LETHALITY_GATE,
         "DeathPolicy": DEATH_POLICY, "StatBenefitModel": STAT_BENEFIT_MODEL}


def slow_policy_defaults() -> dict[str, Any]:
    return json.loads(SLOW_POLICY_DEFAULTS.read_text(encoding="utf-8"))


def slow_job(job_id: str, *, probe: dict | None = None, engine_probe: dict | None = None, **kw) -> dict:
    """``engine.engine_job`` plus ``slow_policy_probe`` (validated like the core objects)."""
    job = engine.engine_job(job_id, probe=engine_probe, **kw)
    if probe is not None:
        req._check_object(probe, "slow_policy_probe", f"{job_id}.slow_policy_probe")
        job["slow_policy_probe"] = dict(probe)
    return job


def slow_fights(events: list[dict]) -> list[dict]:
    return [e for e in events if e["type"] == "slow_fight"]
