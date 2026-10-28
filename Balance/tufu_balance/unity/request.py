"""Batch requests for the Unity harness (design 1.4.2; unit C0b).

A request is plain JSON read by ``SimulationRunRequest`` (C#, Newtonsoft, unknown members are errors).
This module owns the core fields, their defaults, Python-side validation, presets, the canonical
form and ``params_hash``. Units that add fields (C4 ``override``, C8 bake, C9 ``patch``) register
them with :func:`register_fields` from their own module.
"""

from __future__ import annotations

import copy
import hashlib
import json
import math
import re
from typing import Any, Mapping

SCHEMA = "tufu.sim.request/1"
SANDBOX_COMPANY = "TufuSimSandbox"
MODES = ("free", "checkpoint", "replay", "empty")
EXPECTED_ALIVE_ENABLED = 699  # Runbook 6.1: frozen AnyoneAlive && IsEnabled count on a new save

CONFIG_OVERRIDE_WHITELIST = (
    "UseSpawnerCharges", "SpawnerChargesChunkMinutes", "TimeChunkDurationMinutes", "CrystalSimulation", "GraphSimulation",
    "AutoGambling", "AutoBalanceItems", "AutoBalanceAmulet", "IsLog", "IsSmartPath", "IsDynamicResourcePriority",
    "IsCapOptimumTime", "TrimSimulationData", "TimeStep", "SnapshotInterval", "UseFastTick", "FastTickStartChunkOffset",
    "EarlySpawnerOptimumTimeOffsetStartLocation", "EarlySpawnerOptimumTimeOffsetRelativePart",
    "EarlySpawnerOptimumTimeOffsetMultiplier",
)

# Fields that change only throughput or failure timing, never results: excluded from run_key (design 1.4.2).
SPEED_ONLY = {"unity": ("simulation_speed", "frame_budget_ms", "heartbeat_interval_s", "ready_timeout_s")}

# object path -> {field: python type(s)}; None in the tuple allows null.
_FIELDS: dict[str, dict[str, tuple]] = {
    "": {"schema": (str,), "run_key": (str, None), "code_rev": (str, None), "out_dir": (str,), "unity": (dict,),
         "sandbox": (dict,), "jobs": (list,)},
    "unity": {"simulation_speed": (int,), "frame_budget_ms": (int, float), "time_scale_zero": (bool,),
              "disable_ui": (bool,), "reset_mode": (str,), "ready_timeout_s": (int, float),
              "heartbeat_interval_s": (int, float)},
    "sandbox": {"ab_variant": (str,), "pets": (str,), "clock_epoch_utc": (str,), "identity": (str,),
                "prefs_seed": (dict,), "expected_ready_spawners": (str, None), "expected_alive_enabled": (int, None),
                "skip_integrations": (bool,), "zero_ready_cooldowns": (bool,)},
    "job": {"job_id": (str,), "mode": (str,), "stop": (dict,), "config_overrides": (dict,), "params": (dict, None),
            "params_hash": (str, None), "outputs": (dict,), "test": (dict, None)},
    "stop": {"day": (int, None), "location": (int, None), "max_ticks": (int, None), "max_wall_seconds": (int, float, None),
             "max_frames": (int, None)},
    "test": {"mutate_player_config": (bool,), "skip_reimport": (bool,), "busy_seconds": (int, float),
             "busy_ticks": (bool,), "throw_exception": (bool,)},
}
REQUIRED = {"": ("schema", "out_dir", "jobs"), "job": ("job_id",)}
CORE_FIELDS = {obj: frozenset(fields) for obj, fields in _FIELDS.items()}  # C0b's set, before other units register


class RequestError(ValueError):
    """The request would be rejected by the C# side (exit code 5)."""


def register_fields(obj: str, fields: Mapping[str, tuple]) -> None:
    """Add fields of a later unit (only adds; changing a field's meaning bumps SCHEMA)."""
    known = _FIELDS.setdefault(obj, {})
    for name, types in fields.items():
        if name in known and known[name] != tuple(types):
            raise ValueError(f"field {obj}.{name} already registered with other types")
        known[name] = tuple(types)


def default_unity() -> dict:
    return {"simulation_speed": 500, "frame_budget_ms": 1000.0, "time_scale_zero": True, "disable_ui": True,
            "reset_mode": "reenter_play", "ready_timeout_s": 300.0, "heartbeat_interval_s": 5.0}


def default_sandbox() -> dict:
    return {"ab_variant": "A", "pets": "absent_or_disabled", "clock_epoch_utc": "2024-01-01T00:00:00Z",
            "identity": SANDBOX_COMPANY, "prefs_seed": {}, "expected_ready_spawners": None,
            "expected_alive_enabled": EXPECTED_ALIVE_ENABLED, "skip_integrations": True, "zero_ready_cooldowns": True}


def make_job(job_id: str, mode: str = "free", *, day: int | None = 405, location: int | None = None,
             max_ticks: int | None = 5_000_000, max_wall_seconds: float | None = 900.0, max_frames: int | None = None,
             config_overrides: Mapping[str, Any] | None = None, params: Mapping[str, Any] | None = None,
             outputs: Mapping[str, bool] | None = None, test: Mapping[str, Any] | None = None) -> dict:
    job = {
        "job_id": job_id,
        "mode": mode,
        "stop": {"day": day, "location": location, "max_ticks": max_ticks, "max_wall_seconds": max_wall_seconds,
                 "max_frames": max_frames},
        "config_overrides": dict(config_overrides or {}),
        "params": None if params is None else dict(params),
        "params_hash": None if params is None else params_hash(params),
        "outputs": dict(outputs or {}),
        "test": None,
    }
    if test is not None:
        full = {"mutate_player_config": False, "skip_reimport": False, "busy_seconds": 0.0, "busy_ticks": False,
                "throw_exception": False}
        full.update(test)
        job["test"] = full
    return job


def empty_job(job_id: str = "empty", frames: int = 30, **kw) -> dict:
    """Harness lifecycle only: readiness, remedy, checks, frozen frames, export; never sets IsRunning."""
    return make_job(job_id, "empty", day=None, max_ticks=None, max_wall_seconds=60.0, max_frames=frames, **kw)


def make_request(jobs: list[dict], *, unity: Mapping[str, Any] | None = None, sandbox: Mapping[str, Any] | None = None,
                 out_dir: str = "", code_rev: str | None = None) -> dict:
    u = default_unity()
    u.update(unity or {})
    s = default_sandbox()
    s.update(sandbox or {})
    return {"schema": SCHEMA, "run_key": None, "code_rev": code_rev, "out_dir": out_dir, "unity": u, "sandbox": s,
            "jobs": [copy.deepcopy(j) for j in jobs]}


PRESETS = {
    # selftest: start, readiness, remedy, one empty job, exit (design 7.3 C0b)
    "selftest": lambda: make_request([empty_job("selftest")]),
    "empty": lambda: make_request([empty_job("empty")]),
    # simulation presets; meaningful once C3b fills the engine
    "smoke-11": lambda: make_request([make_job("smoke-11", day=11, max_wall_seconds=300.0)]),
    "golden-405": lambda: make_request([make_job("golden-405", day=405)]),
    "slow-5": lambda: make_request([make_job("slow-5", day=5, max_wall_seconds=300.0,
                                             config_overrides={"UseFastTick": False})]),
}


def preset(name: str) -> dict:
    try:
        return PRESETS[name]()
    except KeyError:
        raise RequestError(f"unknown preset {name!r}; known: {', '.join(sorted(PRESETS))}") from None


def _check_object(obj: Mapping[str, Any], kind: str, where: str) -> None:
    if not isinstance(obj, Mapping):
        raise RequestError(f"{where} must be an object")
    fields = _FIELDS[kind]
    for name in obj:
        if name not in fields:
            raise RequestError(f"{where}.{name}: unknown field")
    for name in REQUIRED.get(kind, ()):
        if obj.get(name) is None:
            raise RequestError(f"{where}.{name} is required")
    for name, value in obj.items():
        types = fields[name]
        if value is None:
            if None not in types:
                raise RequestError(f"{where}.{name} must not be null")
            continue
        allowed = tuple(t for t in types if t is not None)
        if isinstance(value, bool) and bool not in allowed:
            raise RequestError(f"{where}.{name} must be {'/'.join(t.__name__ for t in allowed)}, got bool")
        if not isinstance(value, allowed):
            raise RequestError(f"{where}.{name} must be {'/'.join(t.__name__ for t in allowed)}, got {type(value).__name__}")


def validate(request: Mapping[str, Any]) -> None:
    """Mirror of SimulationRunRequest.Validate (plus unknown-field checks Newtonsoft does in C#)."""
    _check_object(request, "", "request")
    if request["schema"] != SCHEMA:
        raise RequestError(f"schema must be {SCHEMA}")
    if not str(request["out_dir"]).startswith("/"):
        raise RequestError("out_dir must be an absolute path")
    unity = request.get("unity", {})
    sandbox = request.get("sandbox", {})
    _check_object(unity, "unity", "unity")
    _check_object(sandbox, "sandbox", "sandbox")
    if unity.get("reset_mode", "reenter_play") != "reenter_play":
        raise RequestError("reset_mode: only reenter_play is supported")
    if not re.fullmatch(r"TufuSim[A-Za-z0-9_-]{0,48}", sandbox.get("identity", SANDBOX_COMPANY)):
        raise RequestError("sandbox.identity must match ^TufuSim[A-Za-z0-9_-]*$")
    for key, value in sandbox.get("prefs_seed", {}).items():
        if isinstance(value, bool) or not isinstance(value, (int, float, str)):
            raise RequestError(f"sandbox.prefs_seed.{key} must be an integer, float or string")
        if isinstance(value, float) and not math.isfinite(value):
            raise RequestError(f"sandbox.prefs_seed.{key} must be finite")
    jobs = request["jobs"]
    if not jobs:
        raise RequestError("jobs must not be empty")
    seen = set()
    for i, job in enumerate(jobs):
        where = f"jobs[{i}]"
        _check_object(job, "job", where)
        if not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9_.-]{0,63}", job["job_id"]):
            raise RequestError(f"{where}.job_id must match ^[A-Za-z0-9][A-Za-z0-9_.-]*$")
        if job["job_id"] in seen:
            raise RequestError(f"duplicate job_id {job['job_id']}")
        seen.add(job["job_id"])
        if job.get("mode", "free") not in MODES:
            raise RequestError(f"{where}.mode must be one of {MODES}")
        _check_object(job.get("stop", {}), "stop", where + ".stop")
        for key in job.get("config_overrides", {}):
            if key not in CONFIG_OVERRIDE_WHITELIST:
                raise RequestError(f"{where}.config_overrides.{key} is not a run switch")
        if job.get("test") is not None:
            _check_object(job["test"], "test", where + ".test")


def canonical_json(value: Any) -> str:
    return json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=False, allow_nan=True)


def params_hash(params: Mapping[str, Any]) -> str:
    return hashlib.sha256(canonical_json(params).encode()).hexdigest()


def result_relevant(request: Mapping[str, Any]) -> dict:
    """The request without run_key, out_dir and speed-only fields: the part that determines outputs."""
    r = copy.deepcopy(dict(request))
    r.pop("run_key", None)
    r.pop("out_dir", None)
    r.pop("code_rev", None)  # enters run_key separately, with the tree hashes
    for obj, names in SPEED_ONLY.items():
        for name in names:
            r.get(obj, {}).pop(name, None)
    return r


def dumps(request: Mapping[str, Any]) -> str:
    return json.dumps(request, indent=1, ensure_ascii=False, allow_nan=True) + "\n"


def roundtrip_example(out_dir: str = "/tmp/tufu-roundtrip", code_rev: str | None = "abc") -> dict:
    """A request with every core field set, including dictionaries, nulls and polymorphic values
    (int, float, string): the reference for the Python -> C# -> manifest round trip."""
    job = make_job("roundtrip", "empty", day=None, location=None, max_ticks=None, max_wall_seconds=60.0, max_frames=12,
                   config_overrides={"UseFastTick": True, "FastTickStartChunkOffset": 4, "SpawnerChargesChunkMinutes": 7.5},
                   params={"SchemaVersion": 1, "RollMode": 0, "Seed": 7, "Nested": {"a": [1, 2.5, "x", None]}},
                   outputs={"daily": True, "events": False},
                   test={"busy_seconds": 0.0})
    return make_request([job], out_dir=out_dir, code_rev=code_rev, unity={"simulation_speed": 37, "frame_budget_ms": 250.5},
                        sandbox={"prefs_seed": {"TimeSkipSeconds": 259200.0, "ShowSnapshotSelectorOnce": 1,
                                                "tufu_probe_text": "sandbox-text"},
                                 "expected_ready_spawners": None, "identity": SANDBOX_COMPANY})
