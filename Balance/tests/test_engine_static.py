"""Engine (unit C3b) checks that need no Unity: parameter defaults, enum values the JSON carries as integers,
request fields, and the export trimming rule against G19's sequence."""

from __future__ import annotations

import re

from tufu_balance import csharp_enums
from tufu_balance.model import timeaxis
from tufu_balance.unity import engine, request as req, schema

SCRIPTS = engine.PARAMS_SOURCE.parents[3]  # .../Assets/Scripts/Assembly-CSharp
REBUILD_DIR = engine.PARAMS_SOURCE.parent
HARNESS_DIR = REBUILD_DIR.parent / "Harness"
SIMULATION_CONFIG = REBUILD_DIR.parent / "SimulationConfig.cs"


def _enums(directory) -> dict[str, dict[str, int]]:
    out: dict[str, dict[str, int]] = {}
    for enum, namespace, member, value, _ in csharp_enums.parse_enums(directory):
        assert namespace == "Codebase.Simulation.Rebuild"
        out.setdefault(enum, {})[member] = value
    return out


def test_provenance_enum_values_are_pinned():
    """The manifest and coverage files write Provenance as an integer: the values never change (handoff to C3b)."""
    assert _enums(REBUILD_DIR)["Provenance"] == engine.PROVENANCE == {
        "Inferred": 0, "Calibrated": 1, "Null": 2, "DebugOnly": 3, "Native": 4}


def test_params_enums_match_csharp():
    enums = _enums(REBUILD_DIR)
    assert enums["DpsCacheProvenance"] == engine.DPS_CACHE_PROVENANCE
    assert enums["RollMode"] == engine.ROLL_MODE
    assert enums["AutoBalanceMode"] == engine.AUTO_BALANCE_MODE


def _csharp_defaults() -> dict:
    text = engine.PARAMS_SOURCE.read_text(encoding="utf-8")
    body = re.search(r"public partial class SimulationRebuildParams\s*\{(.*?)(?:internal|public) static SimulationRebuildParams FromJson", text, re.S).group(1)
    enums = _enums(REBUILD_DIR)
    out = {}
    for type_, name, value in re.findall(r"^\t\tpublic ([\w\[\]]+) (\w+) = (.+?);", body, re.M):
        if type_.endswith("[]"):
            out[name] = [float(v.strip().rstrip("f")) for v in value.strip("{} ").split(",")]
        elif type_ == "float":
            out[name] = float(value.rstrip("f"))
        elif type_ == "int":
            out[name] = int(value)
        else:
            enum, member = value.split(".")
            assert enum == type_
            out[name] = enums[enum][member]
    return out


def test_engine_json_equals_the_code_defaults():
    """Balance/sim/params/defaults/engine.json carries exactly the core fields with the C# defaults."""
    defaults = engine.engine_defaults()
    assert defaults == _csharp_defaults()
    assert list(defaults) == list(_csharp_defaults())
    assert defaults["DpsCacheProvenance"] == engine.DPS_CACHE_PROVENANCE["RpmFirst"]
    assert defaults["FastAfkStepMinutes"] == timeaxis.FAST_OFFLINE_STEP_MINUTES


def test_engine_request_fields_match_csharp():
    text = (HARNESS_DIR / "SimulationRunRequest.Engine.cs").read_text(encoding="utf-8")
    assert re.findall(r'\[JsonProperty\("engine_probe"', text)
    probe = re.search(r"class EngineProbe\s*\{(.*?)\n\t\t\}", text, re.S).group(1)
    assert set(re.findall(r'\[JsonProperty\("([a-z_]+)"\)\]', probe)) == set(engine.PROBE_FIELDS)
    job = engine.engine_job("x", probe=engine.engine_probe(null_policy=True))
    r = req.make_request([job], out_dir="/tmp/x")
    req.validate(r)
    assert "engine" in schema.MANIFEST_KEYS


def test_trim_rule_in_csharp_matches_g19():
    """SimulationConfig.GetTrimmedTimeChunkSnapshots: max(i + 1, ceil(1.1f * i)) in float32 (the oracle's rule)."""
    text = SIMULATION_CONFIG.read_text(encoding="utf-8")
    body = re.search(r"private List<SimulationSnapshot> GetTrimmedTimeChunkSnapshots\(\)\s*\{(.*?)\n\t\t\}", text, re.S).group(1)
    assert "Math.Max(day + 1, (int)Mathf.Ceil((float)(1.1f * day)))" in body
    assert timeaxis.trimmed_time_chunk_days(405)[-3:] == [334, 368, 405]
    assert len(timeaxis.trimmed_time_chunk_days(405)) == 46
