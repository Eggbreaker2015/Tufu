"""Batch request: Python validation and parity with the C# core fields (design 1.4.2; unit C0b).
The Python -> C# -> manifest -> Python round trip runs in Unity: tests/unity/test_sandbox.py."""

from __future__ import annotations

import json
import math
import re

import pytest

from tufu_balance.unity import request as req, runner

CS_REQUEST = runner.project_dir(runner.WORKTREE) / "Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Harness/SimulationRunRequest.cs"


def full_request() -> dict:
    return req.roundtrip_example()


def test_full_request_validates_and_round_trips_through_json():
    r = full_request()
    req.validate(r)
    assert json.loads(req.dumps(r)) == r


@pytest.mark.parametrize("path", [(), ("unity",), ("sandbox",), ("jobs", 0), ("jobs", 0, "stop"), ("jobs", 0, "test")])
def test_unknown_fields_are_rejected(path):
    r = full_request()
    obj = r
    for p in path:
        obj = obj[p]
    obj["bogus_field"] = 1
    with pytest.raises(req.RequestError, match="unknown field"):
        req.validate(r)


@pytest.mark.parametrize("mutate, message", [
    (lambda r: r.update(schema="tufu.sim.request/2"), "schema"),
    (lambda r: r.update(out_dir="relative/dir"), "absolute"),
    (lambda r: r["jobs"][0].update(mode="fast"), "mode"),
    (lambda r: r["jobs"][0].update(job_id="../x"), "job_id"),
    (lambda r: r["jobs"].append(dict(r["jobs"][0])), "duplicate"),
    (lambda r: r["jobs"][0]["config_overrides"].update(IsRunning=True), "run switch"),
    (lambda r: r["sandbox"].update(identity="Multicast Games"), "identity"),
    (lambda r: r["sandbox"]["prefs_seed"].update(flag=True), "prefs_seed"),
    (lambda r: r["sandbox"]["prefs_seed"].update(x=math.nan), "finite"),
    (lambda r: r["unity"].update(reset_mode="in_session"), "reenter_play"),
    (lambda r: r["unity"].update(simulation_speed=None), "null"),
    (lambda r: r["jobs"][0]["stop"].update(day="405"), "stop.day"),
    (lambda r: r.update(jobs=[]), "empty"),
])
def test_invalid_requests(mutate, message):
    r = full_request()
    mutate(r)
    with pytest.raises(req.RequestError, match=message):
        req.validate(r)


def test_params_hash_and_canonical_form():
    a = {"b": 1, "a": [1.5, None]}
    assert req.params_hash(a) == req.params_hash(json.loads(json.dumps(a, sort_keys=False)))
    assert req.params_hash(a) != req.params_hash({"b": 1, "a": [1.5]})
    job = req.make_job("j", params=a)
    assert job["params_hash"] == req.params_hash(a)


def test_presets_validate():
    for name in req.PRESETS:
        r = req.preset(name)
        r["out_dir"] = "/tmp/x"
        req.validate(r)


def _cs_fields() -> dict[str, set[str]]:
    """JsonProperty names per nested class of SimulationRunRequest.cs."""
    text = CS_REQUEST.read_text()
    fields: dict[str, set[str]] = {}
    current = ""
    for line in text.splitlines():
        m = re.search(r"partial class (\w+)", line)
        if m:
            current = {"SimulationRunRequest": "", "UnityOptions": "unity", "SandboxOptions": "sandbox", "Job": "job",
                       "StopOptions": "stop", "HarnessTest": "test"}[m.group(1)]
        for name in re.findall(r'\[JsonProperty\("([a-z_]+)"', line):
            fields.setdefault(current, set()).add(name)
    return fields


def test_python_core_fields_match_csharp():
    cs = _cs_fields()
    py = {k: set(v) for k, v in req.CORE_FIELDS.items()}
    assert cs == py
    assert re.search(r'SchemaId = "' + re.escape(req.SCHEMA) + '"', CS_REQUEST.read_text())
    whitelist = re.search(r"ConfigOverrideWhitelist =\s*\{(.*?)\};", CS_REQUEST.read_text(), re.S).group(1)
    assert tuple(re.findall(r'"(\w+)"', whitelist)) == req.CONFIG_OVERRIDE_WHITELIST
