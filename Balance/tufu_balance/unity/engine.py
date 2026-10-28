"""Engine-side request fields, manifest key and parameter defaults (design 4.4, 4.5, 4.8; unit C3b).

* ``job.engine_probe`` (C# ``SimulationRunRequest.Engine.cs``): test-only switches -- ``null_policy`` (no
  ``FindBestSpawner``, minimal FastTick), ``unlock_through`` (open islands 2..N before the run),
  ``write_points`` (the write-point scenario, then stop). Null/absent in real runs.
* manifest ``engine`` (``SimulationSystem.RebuildManifest``): effective params, stop, counters, per-day and
  per-offline logs, bosses per island, the Provenance enum values.
* ``sim/params/defaults/engine.json``: the core fields of ``SimulationRebuildParams`` with the code defaults.
"""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from . import request as req, schema

BALANCE = Path(__file__).resolve().parents[2]
ENGINE_DEFAULTS = BALANCE / "sim" / "params" / "defaults" / "engine.json"
PARAMS_SOURCE = (BALANCE.parent / "Recovery" / "StudyProject" / "Assets" / "Scripts" / "Assembly-CSharp" / "CodeBase"
                 / "Simulation" / "Rebuild" / "SimulationRebuildParams.cs")

PROBE_FIELDS = {"null_policy": (bool,), "unlock_through": (int,), "write_points": (bool,)}

req.register_fields("job", {"engine_probe": (dict, None)})
req.register_fields("engine_probe", PROBE_FIELDS)
schema.register_manifest_keys({"engine": "object"})

# SimulationRebuildParams enums (C# Rebuild/SimulationRebuildParams.cs); JSON carries the integers.
DPS_CACHE_PROVENANCE = {"RpmFirst": 0, "ModifierFirst": 1, "StaleTickCache": 2}
ROLL_MODE = {"Carry": 0, "Seeded": 1}
AUTO_BALANCE_MODE = {"Frozen": 0, "Regenerate": 1}
# SimulationRebuildAttribute.Provenance: the manifest and rebuild_coverage.json write it as an integer.
PROVENANCE = {"Inferred": 0, "Calibrated": 1, "Null": 2, "DebugOnly": 3, "Native": 4}


def engine_defaults() -> dict[str, Any]:
    return json.loads(ENGINE_DEFAULTS.read_text(encoding="utf-8"))


def engine_probe(*, null_policy: bool = False, unlock_through: int = 0, write_points: bool = False) -> dict:
    return {"null_policy": null_policy, "unlock_through": unlock_through, "write_points": write_points}


def engine_job(job_id: str, *, probe: dict | None = None, **kw) -> dict:
    """``request.make_job`` plus ``engine_probe`` (validated like the core objects)."""
    job = req.make_job(job_id, **kw)
    if probe is not None:
        req._check_object(probe, "engine_probe", f"{job_id}.engine_probe")
        job["engine_probe"] = dict(probe)
    return job
