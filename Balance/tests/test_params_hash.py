"""Run parameters without Unity (design 4.8, 7.3 C3c).

* The C# canonical JSON (``SimulationCanonicalJson``, run under .NET by tools/rebuildcheck) is Python's
  ``request.canonical_json`` byte for byte, so the C# ``params_hash`` of a request equals P10's ``params_hash``.
* The effective hash of the parsed parameters equals it too for the resolved defaults; a float that is not a
  float32 value changes the effective hash exactly as ``paramsio.effective`` predicts.
* Unknown fields are errors, partial objects report their missing fields, complete objects are required by the
  importer path.
* The committed parameter asset holds the defaults; only the importer calls asset-writing editor APIs.
"""

from __future__ import annotations

import json
import math
import random
import re
import struct

import numpy as np
import pytest

from tufu_balance.calibrate import params as P
from tufu_balance.unity import engine, paramsio, rebuildcheck, request as req

needs_dotnet = pytest.mark.skipif(not rebuildcheck.available(), reason="needs the .NET 8 SDK")

REPO = rebuildcheck.cscheck.WORKTREE
ASSETS = REPO / "Recovery" / "StudyProject" / "Assets"
SIM = ASSETS / "Scripts" / "Assembly-CSharp" / "CodeBase" / "Simulation"
EDITOR = ASSETS / "Editor" / "Simulation"


def _double(bits: int) -> float:
    return struct.unpack("<d", struct.pack("<Q", bits))[0]


def _float(bits: int) -> float:
    return struct.unpack("<f", struct.pack("<I", bits))[0]


def _doubles(n=6000):
    rng = random.Random(1)
    special = [0.0, 1.0, 0.1, 0.3, 1 / 3, 1e16, 1e15, 9999999999999998.0, 1.5e16, 1e-4, 1e-5, 0.00012345, 123456789012345680.0,
               5e-324, 2.2250738585072014e-308, 2.225073858507201e-308, 1.7976931348623157e308, 2.0 ** 52, 2.0 ** 53 + 2,
               4.35, 0.07, 100.0, 1e22, 1e23, 5e-7, 9007199254740993.0]
    out = special + [-x for x in special if x]
    while len(out) < n:
        x = _double(rng.getrandbits(64))
        if math.isfinite(x):
            out.append(x)
    for e in range(-323, 309):  # every decade, both sides of the power
        p = float(f"1e{e}")
        out += [p, math.nextafter(p, 0), math.nextafter(p, math.inf)]
    return out


@needs_dotnet
def test_double_text_is_python_repr():
    xs = _doubles()
    [res] = rebuildcheck.run([{"op": "format", "doubles": [format(struct.unpack("<Q", struct.pack("<d", x))[0], "016x") for x in xs],
                               "floats": []}])
    wrong = [(x, got, repr(x)) for x, got in zip(xs, res["doubles"]) if got != repr(x)]
    assert wrong == []


@needs_dotnet
def test_float_text_is_the_shortest_float32_as_python_writes_it():
    rng = random.Random(2)
    bits = [rng.getrandbits(32) for _ in range(6000)] + [0x3DCCCCCD, 0x42F00000, 0x00000001, 0x007FFFFF, 0x00800000,
                                                          0x7F7FFFFF, 0x3F800000, 0x80000000, 0x5A0E1BCA]
    bits = [b for b in bits if math.isfinite(_float(b))]
    [res] = rebuildcheck.run([{"op": "format", "doubles": [], "floats": [format(b, "08x") for b in bits]}])
    expected = [repr(float(paramsio.float32_text(_float(b)))) for b in bits]
    assert [x for x in zip(bits, res["floats"], expected) if x[1] != x[2]] == []


def _random_json(rng, depth=0):
    kind = rng.randrange(8 if depth < 3 else 5)
    if kind == 0:
        return rng.randint(-10 ** 12, 10 ** 12)
    if kind == 1:
        x = _double(rng.getrandbits(64))
        return x if math.isfinite(x) and x != 0 else rng.random()
    if kind == 2:
        return "".join(rng.choice(["a", "Z", "é", "\u0001", "\t", "\"", "\\", "/", " ", "😀", " ", "\x7f"])
                       for _ in range(rng.randrange(6)))
    if kind == 3:
        return rng.choice([True, False, None])
    if kind == 4:
        return rng.choice([0.5, 120.0, 1e-05, 1e16, -2.5])
    if kind == 5:
        return [_random_json(rng, depth + 1) for _ in range(rng.randrange(4))]
    keys = ["b", "a", "B", "é", "😀", "￿", "aa", "", "Seed"]
    return {rng.choice(keys): _random_json(rng, depth + 1) for _ in range(rng.randrange(5))}


@needs_dotnet
def test_canonical_json_and_hash_match_python():
    rng = random.Random(3)
    values = [_random_json(rng) for _ in range(300)]
    values += [P.resolve(["defaults"]).params, req.roundtrip_example()["jobs"][0]["params"]]
    results = rebuildcheck.run([{"op": "canonical", "value": v} for v in values])
    for v, r in zip(values, results):
        assert r["text"] == req.canonical_json(v)
        assert r["hash"] == req.params_hash(v)
    # Stable: member order and number spelling (1.0 vs 1.00, 1e1 vs 10.0) do not change the hash.
    defaults = P.resolve(["defaults"]).params
    shuffled = dict(reversed(list(defaults.items())))
    texts = [json.dumps(shuffled), json.dumps(defaults).replace("120.0", "1.2e2").replace("20.0", "20.000")]
    hashes = rebuildcheck.run_text("[" + ",".join('{"op": "canonical", "value": %s}' % t for t in texts) + "]")
    assert {h["hash"] for h in hashes} == {P.resolve(["defaults"]).params_hash}


@needs_dotnet
def test_negative_zero_loses_its_sign_in_newtonsoft():
    """Known difference (Runbook 17): Newtonsoft reads -0.0 as 0.0, so such a request fails its params_hash
    check (exit 5) instead of running with a different hash."""
    [r] = rebuildcheck.run([{"op": "canonical", "value": {"x": -0.0}}])
    assert r["text"] == '{"x":0.0}' and req.canonical_json({"x": -0.0}) == '{"x":-0.0}'


@needs_dotnet
def test_effective_hash_equals_p10_params_hash_for_the_defaults():
    resolved = P.resolve(["defaults"])
    [r] = rebuildcheck.run([{"op": "params", "json": resolved.params, "complete": True}])
    assert r["ok"] and r["missing"] == [] and r["valid"] is None
    assert r["effective"] == resolved.canonical()
    assert r["hash"] == r["clone_hash"] == resolved.params_hash == paramsio.effective_hash(resolved.params)
    assert r["fields"] == sorted(P.read_csharp_params().fields) == sorted(engine.engine_defaults())


@needs_dotnet
def test_overrides_take_effect_and_non_float32_values_are_visible():
    resolved = P.resolve(["defaults"], {"FastAfkStepMinutes": 90.0, "Seed": 7, "RollMode": 1,
                                        "EarlySnapshotIntervals": [2.0, 5.0]})
    odd = P.resolve(["defaults"], {"FastAfkStepMinutes": 0.1})
    r, s = rebuildcheck.run([{"op": "params", "json": x.params, "complete": True} for x in (resolved, odd)])
    effective = json.loads(r["effective"])
    assert effective["FastAfkStepMinutes"] == 90.0 and effective["Seed"] == 7 and effective["RollMode"] == 1
    assert effective["EarlySnapshotIntervals"] == [2.0, 5.0] and r["hash"] == resolved.params_hash
    # 0.1 is not a float32: C# holds 0.1f, whose shortest text is 0.1 again, so the effective object is unchanged
    # in text; a value whose float32 has a different shortest text changes the hash as paramsio predicts.
    assert s["hash"] == paramsio.effective_hash(odd.params) == odd.params_hash
    near = P.resolve(["defaults"], {"FastAfkStepMinutes": 120.00000001})
    [t] = rebuildcheck.run([{"op": "params", "json": near.params, "complete": True}])
    assert t["hash"] == paramsio.effective_hash(near.params) != near.params_hash
    assert json.loads(t["effective"])["FastAfkStepMinutes"] == 120.0


@needs_dotnet
def test_unknown_missing_and_invalid_fields():
    defaults = P.resolve(["defaults"]).params
    unknown, missing, partial, invalid = rebuildcheck.run([
        {"op": "params", "json": {**defaults, "Bogus": 1}, "complete": False},
        {"op": "params", "json": {k: v for k, v in defaults.items() if k != "Seed"}, "complete": True},
        {"op": "params", "json": {"DpsCacheProvenance": 1}, "complete": False},
        {"op": "params", "json": {**defaults, "RollMode": 5}, "complete": True},
    ])
    assert not unknown["ok"] and "Bogus" in unknown["error"]
    assert not missing["ok"] and "Seed" in missing["error"]
    assert partial["ok"] and set(partial["missing"]) == set(defaults) - {"DpsCacheProvenance"}
    assert invalid["ok"] and "RollMode" in invalid["valid"]


def test_python_effective_model():
    assert paramsio.float32_text(0.1) == "0.1" and paramsio.float32_text(120) == "120.0"
    eff = paramsio.effective({"FastAfkStepMinutes": 120.00000001, "Seed": 3, "EarlySnapshotIntervals": [0.1, 1e-45]})
    assert eff == {"EarlySnapshotIntervals": [0.1, 1e-45], "FastAfkStepMinutes": 120.0, "Seed": 3}
    assert np.float32(1e-45) == np.float32(1.401298464324817e-45)


# ---------------------------------------------------------------- the asset

def test_committed_asset_holds_the_defaults():
    defaults = P.resolve(["defaults"]).params
    path = paramsio.asset_path()
    assert paramsio.read_asset(path) == paramsio.effective(defaults) == P.csharp_defaults(P.read_csharp_params())
    assert path.read_text(encoding="utf-8") == paramsio.asset_text(defaults)
    script_meta = (SIM / "Rebuild" / "SimulationRebuildParamsAsset.cs.meta").read_text()
    guid = re.search(r"^guid: (\w+)$", script_meta, re.M).group(1)
    assert f"m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}" in path.read_text()
    meta = (path.parent / (path.name + ".meta")).read_text()
    assert "NativeFormatImporter:" in meta and "mainObjectFileID: 11400000" in meta
    assert "Resources" not in path.parts


def test_asset_reader_takes_unity_float_spellings(tmp_path):
    cs = P.read_csharp_params()
    text = paramsio.asset_text(P.resolve(["defaults"]).params).replace("FastAfkStepMinutes: 120", "FastAfkStepMinutes: 1e-05")
    text = text.replace("    - 20\n", "    - 20.5\n")
    (tmp_path / "a.asset").write_text(text)
    values = paramsio.read_asset(tmp_path / "a.asset", cs)
    assert values["FastAfkStepMinutes"] == 1e-05 and values["EarlySnapshotIntervalStarts"] == [0.0, 20.5]
    (tmp_path / "b.asset").write_text(text.replace("    Seed: 1\n", "    Seed: 1\n    Extra: 2\n"))
    with pytest.raises(paramsio.ParamsIoError):
        paramsio.read_asset(tmp_path / "b.asset", cs)


ASSET_WRITES = re.compile(r"\b(EditorUtility\.SetDirty|AssetDatabase\.(SaveAssets|SaveAssetIfDirty|CreateAsset|ForceReserializeAssets"
                          r"|CopyAsset|MoveAsset|DeleteAsset)|PrefabUtility\.Save|EditorSceneManager\.Save)\b")


def test_only_the_importer_writes_assets():
    """Design 1.5 C# rule 6: the guarded entries are the parameter importer (C3c) and C8's bake write-back;
    nothing else under the simulation code calls an asset-writing editor API."""
    allowed = {"SimulationParamsImporter.cs", "SimulationBakeTool.cs"}
    found = {}
    for root in (SIM, EDITOR):
        for path in root.rglob("*.cs"):
            text = re.sub(r"//[^\n]*", "", path.read_text(encoding="utf-8"))
            if ASSET_WRITES.search(text):
                found[path.name] = sorted({m.group(0) for m in ASSET_WRITES.finditer(text)})
    assert set(found) <= allowed, found
    assert "SimulationParamsImporter.cs" in found


def test_importer_refuses_after_play_and_outside_batch():
    text = (EDITOR / "SimulationParamsImporter.cs").read_text(encoding="utf-8")
    assert "[InitializeOnLoad]" in text
    assert re.search(r"ExitingEditMode\) SessionState\.SetBool\(EnteredPlayKey, true\)", text)
    refusal = text[text.index("public static string Refusal()"):text.index("public static void Import()")]
    assert "EnteredPlayKey" in refusal and "isPlayingOrWillChangePlaymode" in refusal and "isBatchMode" in refusal
    run = text[text.index("private static int Run("):]
    assert run.index("Refusal()") < run.index("SaveAssetIfDirty")


def test_rebuild_sources_used_by_dotnet_stay_unity_free():
    for path in rebuildcheck.sources()[2:]:
        text = re.sub(r"//[^\n]*", "", path.read_text(encoding="utf-8"))
        assert "UnityEngine" not in text and "UnityEditor" not in text, path.name


def test_request_helpers():
    j = paramsio.job("a", source="asset", day=2)
    assert j["params_source"] == "asset" and j["params"] is None
    req.validate(req.make_request([j], out_dir="/x"))
    with pytest.raises(req.RequestError):
        paramsio.job("b", source="asset", params={"Seed": 1})
    with pytest.raises(req.RequestError):
        req.validate(req.make_request([{**j, "params_source": 3}], out_dir="/x"))
