"""Run parameters without Unity (design 4.8, 7.3 C3c).

* The C# canonical JSON (``SimulationCanonicalJson``, run by tools/rebuildcheck under .NET and under the editor's
  Mono) is Python's ``request.canonical_json`` byte for byte, so the C# ``params_hash`` of a request equals P10's
  ``params_hash``. Request params are read with the exact parser (``SimulationCanonicalJson.Parse``), not with the
  runtime's double parser, which in Mono rounds some literals 1 ulp off.
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
RUNTIMES = [pytest.param(rt, marks=pytest.mark.skipif(not rebuildcheck.available(rt), reason=f"needs {rt}"))
            for rt in rebuildcheck.RUNTIMES]
each_runtime = pytest.mark.parametrize("runtime", RUNTIMES)

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


@each_runtime
def test_double_text_is_python_repr(runtime):
    xs = _doubles()
    [res] = rebuildcheck.run([{"op": "format", "doubles": [format(struct.unpack("<Q", struct.pack("<d", x))[0], "016x") for x in xs],
                               "floats": []}], runtime)
    wrong = [(x, got, repr(x)) for x, got in zip(xs, res["doubles"]) if got != repr(x)]
    assert wrong == []


@each_runtime
def test_float_text_is_the_shortest_float32_as_python_writes_it(runtime):
    rng = random.Random(2)
    bits = [rng.getrandbits(32) for _ in range(6000)] + [0x3DCCCCCD, 0x42F00000, 0x00000001, 0x007FFFFF, 0x00800000,
                                                          0x7F7FFFFF, 0x3F800000, 0x80000000, 0x5A0E1BCA]
    bits = [b for b in bits if math.isfinite(_float(b))]
    [res] = rebuildcheck.run([{"op": "format", "doubles": [], "floats": [format(b, "08x") for b in bits]}], runtime)
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


@each_runtime
def test_canonical_json_and_hash_match_python(runtime):
    rng = random.Random(3)
    values = [_random_json(rng) for _ in range(300)]
    values += [P.resolve(["defaults"]).params, req.roundtrip_example()["jobs"][0]["params"]]
    results = rebuildcheck.run([{"op": "canonical", "value": v} for v in values], runtime)
    for v, r in zip(values, results):
        assert r["text"] == req.canonical_json(v)
        assert r["hash"] == req.params_hash(v)
    # Stable: member order and number spelling (1.0 vs 1.00, 1e1 vs 10.0) do not change the hash.
    defaults = P.resolve(["defaults"]).params
    shuffled = dict(reversed(list(defaults.items())))
    texts = [json.dumps(shuffled), json.dumps(defaults).replace("120.0", "1.2e2").replace("20.0", "20.000")]
    hashes = rebuildcheck.run_text("[" + ",".join('{"op": "canonical", "value": %s}' % t for t in texts) + "]", runtime)
    assert {h["hash"] for h in hashes} == {P.resolve(["defaults"]).params_hash}


def _powers_of_two():
    """Binade boundaries (the lower neighbour is half as far) of doubles and float32s, both signs, with their
    neighbours."""
    doubles, floats = [], []
    for e in range(-1074, 1024):
        p = math.ldexp(1.0, e)
        doubles += [p, -p, math.nextafter(p, 0), math.nextafter(p, math.inf)]
    for e in range(-149, 128):
        b = struct.unpack("<I", struct.pack("<f", math.ldexp(1.0, e)))[0]
        floats += [b, b | 0x80000000, b - 1, b + 1]
    return doubles, [b for b in floats if 0 < (b & 0x7FFFFFFF) < 0x7F800000]


@each_runtime
def test_powers_of_two_and_signed_zero_format_as_python(runtime):
    doubles, floats = _powers_of_two()
    doubles += [0.0, -0.0]
    floats += [0x00000000, 0x80000000]
    [res] = rebuildcheck.run([{"op": "format", "doubles": [format(struct.unpack("<Q", struct.pack("<d", x))[0], "016x") for x in doubles],
                               "floats": [format(b, "08x") for b in floats]}], runtime)
    assert [(x, g) for x, g in zip(doubles, res["doubles"]) if g != repr(x)] == []
    expected = [repr(float(paramsio.float32_text(_float(b)))) for b in floats]
    assert [(hex(b), g, e) for b, g, e in zip(floats, res["floats"], expected) if g != e] == []
    assert res["doubles"][-2:] == ["0.0", "-0.0"] and res["floats"][-2:] == ["0.0", "-0.0"]


@each_runtime
def test_control_characters_are_escaped_as_python_does(runtime):
    texts = ["\b", "\f", "\r", "\n", "\t", "\x00", "\x1f", "\x7f", "\u2028", "a\bb\fc\r\nd", '"\\/']
    values = [{t: t} for t in texts] + [texts]
    results = rebuildcheck.run([{"op": "canonical", "value": v} for v in values], runtime)
    assert [r["text"] for r in results] == [req.canonical_json(v) for v in values]


# Literals Mono's double parser (through Newtonsoft) reads 1 ulp high (C3c review, Unity run 4d65b148).
MONO_ULP_LITERALS = ["1.534053662242575", "181.0483539212821"]


def _bits(x: float) -> str:
    return format(struct.unpack("<Q", struct.pack("<d", x))[0], "016x")


def _number_texts():
    """Number spellings: shortest reprs of typical and random doubles, long and padded decimals, exact halfway
    points between neighbouring doubles (round half even), subnormals, the overflow edge, signed zeros."""
    rng = random.Random(5)
    texts = list(MONO_ULP_LITERALS)
    for _ in range(12000):
        texts.append(repr(10 ** rng.uniform(-6, 6.3) * rng.choice([1, -1])))
    for _ in range(3000):
        x = _double(rng.getrandbits(64))
        if math.isfinite(x):
            texts.append(repr(x))
    for _ in range(2000):
        digits = "".join(rng.choice("0123456789") for _ in range(rng.randrange(1, 26)))
        texts.append(f"{digits.lstrip('0') or '0'}e{rng.randrange(-340, 310)}")
        texts.append(f"0.{'0' * rng.randrange(4)}{digits}")
    for _ in range(1000):
        from decimal import Decimal
        x = abs(_double(rng.getrandbits(64))) or 1.0
        if math.isfinite(x) and math.isfinite(math.nextafter(x, math.inf)):
            mid = (Decimal(x) + Decimal(math.nextafter(x, math.inf))) / 2
            texts.append(format(mid, "f") if abs(mid.adjusted()) < 30 else format(mid, "e"))
    texts += ["0.0", "-0.0", "0e0", "-0e-5", "1e-400", "-1e-400", "1e400", "4.9406564584124654e-324",
              "2.4703282292062327e-324", "2.4703282292062328e-324", "2.2250738585072011e-308", "2.2250738585072012e-308",
              "1.7976931348623157e308", "1.7976931348623158e308", "1.7976931348623159e308", "9007199254740993.0",
              "9007199254740993", "-9223372036854775808", "9223372036854775808", "1E+2", "1.0E-2", "100000000000000000000000.5"]
    return texts


@each_runtime
def test_request_numbers_are_read_as_python_reads_them(runtime):
    """SimulationCanonicalJson.Parse (the request's params) gives Python's json.loads value for every spelling,
    under .NET and under the editor's Mono."""
    texts = _number_texts()
    [res] = rebuildcheck.run([{"op": "parse", "texts": texts}], runtime)
    expected = [req.canonical_json(json.loads(t)) for t in texts]
    assert [(t, g, e) for t, g, e in zip(texts, res["texts"], expected) if g != e] == []


@each_runtime
def test_the_mono_runtime_is_the_one_with_the_inexact_parser(runtime):
    """The Mono check is meaningful: there, the runtime's own parse (Newtonsoft) of the review's literals is off;
    under .NET it is exact."""
    [res] = rebuildcheck.run([{"op": "newtonsoft", "texts": MONO_ULP_LITERALS}], runtime)
    exact = [_bits(float(t)) for t in MONO_ULP_LITERALS]
    if runtime == "mono":
        assert all(g != e for g, e in zip(res["bits"], exact)), rebuildcheck.runtime_banner(runtime)
    else:
        assert res["bits"] == exact


def test_parser_rejects_what_python_rejects():
    bad = ["1.", ".5", "01", "+1", "1e", "-", "'a'", r'"\x"', r'"\u12"', r'"a\u12g"', '"\x01"', "nul", "[1,]",
           '{"a" 1}', '{"a":1,}', "[1 2]", "1 2", "", "[", '{"a"', "tru", "-NaN", "infinity"]
    for t in bad:
        with pytest.raises(ValueError):
            json.loads(t)
    for runtime in [p.values[0] for p in RUNTIMES if rebuildcheck.available(p.values[0])]:
        [res] = rebuildcheck.run([{"op": "parse", "texts": bad + ["NaN", "-Infinity", ' {"a" : [ ] }\n', r'"\u00e9\/"']}], runtime)
        assert [(t, g) for t, g in zip(bad, res["texts"]) if not g.startswith("error: JSON")] == []
        assert res["texts"][len(bad):] == ["NaN", "-Infinity", '{"a":[]}', '"é/"']


def _request_text(jobs):
    return json.dumps(req.make_request([engine.engine_job(f"j{i}", day=1, **kw) for i, kw in enumerate(jobs)], out_dir="/x"))


@each_runtime
def test_loader_checks_params_hash_against_the_exact_params(runtime):
    """SimulationRebuildParams.FromRequest on SimulationRunRequest.Load's exact params: the review's literals keep
    P10's hash in Mono too, -0.0 keeps its sign, a wrong params_hash and params_hash without params fail."""
    odd = P.resolve(["defaults"], {"FastAfkStepMinutes": float(MONO_ULP_LITERALS[1]),
                                   "EarlySnapshotIntervals": [float(MONO_ULP_LITERALS[0]), 5.0]})
    neg = {**P.resolve(["defaults"]).params, "FastAfkStepMinutes": -0.0}
    request = json.loads(_request_text([{"params": odd.params}, {"params": odd.params}, {"params": neg}, {}, {}]))
    request["jobs"][1]["params_hash"] = "0" * 64
    request["jobs"][4]["params_hash"] = odd.params_hash
    [res] = rebuildcheck.run([{"op": "request", "text": json.dumps(request)}], runtime)
    ok, wrong, negative, defaults, orphan = res["jobs"]
    assert ok["error"] is None and ok["report"]["request_hash"] == odd.params_hash and ok["report"]["complete"]
    assert ok["effective"] == req.canonical_json(paramsio.effective(odd.params))
    assert wrong["error"] == f"params_hash {'0' * 64} is not the canonical hash {odd.params_hash} of params"
    assert wrong["effective"] is None
    assert negative["report"]["request_hash"] == req.params_hash(neg) and "-0.0" in req.canonical_json(neg)
    assert negative["error"] is None and negative["effective"] == req.canonical_json(paramsio.effective(neg))
    assert '"FastAfkStepMinutes":-0.0' in negative["effective"]
    assert defaults["error"] is None and defaults["report"]["source"] == "code_defaults"
    assert orphan["error"] == "params_hash is given without params"
    [bad] = rebuildcheck.run([{"op": "request", "text": '{"jobs": [{"params": [1]}]}'}], runtime)
    assert "params must be an object" in bad["read_error"]


@each_runtime
def test_effective_hash_equals_p10_params_hash_for_the_defaults(runtime):
    resolved = P.resolve(["defaults"])
    [r] = rebuildcheck.run([{"op": "params", "json": resolved.params, "complete": True}], runtime)
    assert r["ok"] and r["missing"] == [] and r["valid"] is None
    assert r["effective"] == resolved.canonical()
    assert r["hash"] == r["clone_hash"] == resolved.params_hash == paramsio.effective_hash(resolved.params)
    assert r["fields"] == sorted(P.read_csharp_params().fields) == sorted(resolved.params)
    assert set(engine.engine_defaults()) <= set(resolved.params)


@each_runtime
def test_overrides_take_effect_and_non_float32_values_are_visible(runtime):
    resolved = P.resolve(["defaults"], {"FastAfkStepMinutes": 90.0, "Seed": 7, "RollMode": 1,
                                        "EarlySnapshotIntervals": [2.0, 5.0]})
    odd = P.resolve(["defaults"], {"FastAfkStepMinutes": 0.1})
    r, s = rebuildcheck.run([{"op": "params", "json": x.params, "complete": True} for x in (resolved, odd)], runtime)
    effective = json.loads(r["effective"])
    assert effective["FastAfkStepMinutes"] == 90.0 and effective["Seed"] == 7 and effective["RollMode"] == 1
    assert effective["EarlySnapshotIntervals"] == [2.0, 5.0] and r["hash"] == resolved.params_hash
    # 0.1 is not a float32: C# holds 0.1f, whose shortest text is 0.1 again, so the effective object is unchanged
    # in text; a value whose float32 has a different shortest text changes the hash as paramsio predicts.
    assert s["hash"] == paramsio.effective_hash(odd.params) == odd.params_hash
    near = P.resolve(["defaults"], {"FastAfkStepMinutes": 120.00000001})
    [t] = rebuildcheck.run([{"op": "params", "json": near.params, "complete": True}], runtime)
    assert t["hash"] == paramsio.effective_hash(near.params) != near.params_hash
    assert json.loads(t["effective"])["FastAfkStepMinutes"] == 120.0


@each_runtime
def test_unknown_missing_and_invalid_fields(runtime):
    defaults = P.resolve(["defaults"]).params
    unknown, missing, partial, invalid = rebuildcheck.run([
        {"op": "params", "json": {**defaults, "Bogus": 1}, "complete": False},
        {"op": "params", "json": {k: v for k, v in defaults.items() if k != "Seed"}, "complete": True},
        {"op": "params", "json": {"DpsCacheProvenance": 1}, "complete": False},
        {"op": "params", "json": {**defaults, "RollMode": 5}, "complete": True},
    ], runtime)
    assert not unknown["ok"] and "Bogus" in unknown["error"]
    assert not missing["ok"] and "Seed" in missing["error"]
    assert partial["ok"] and set(partial["missing"]) == set(defaults) - {"DpsCacheProvenance"}
    assert invalid["ok"] and "RollMode" in invalid["valid"]


@each_runtime
def test_field_names_and_json_types_are_strict(runtime):
    """Newtonsoft alone matches names ignoring case and converts strings to numbers and enums; FromJson takes
    exact names and JSON types that fit the field only."""
    d = P.resolve(["defaults"]).params
    no_seed = {k: v for k, v in d.items() if k != "Seed"}
    bad = {
        "lower_extra": {**d, "seed": 3},
        "lower_extra_float": {**d, "fastAfkStepMinutes": 7},
        "lower_instead": {**no_seed, "seed": 3},
        "upper": {**no_seed, "SEED": 3},
        "string_int": {**d, "Seed": "7"},
        "string_enum": {**d, "RollMode": "Seeded"},
        "string_float": {**d, "FastAfkStepMinutes": "120"},
        "float_int": {**d, "Seed": 7.0},
        "float_enum": {**d, "RollMode": 1.0},
        "bool_int": {**d, "Seed": True},
        "null_int": {**d, "Seed": None},
        "bool_float": {**d, "FastAfkStepMinutes": True},
        "string_array": {**d, "EarlySnapshotIntervals": "1,5"},
        "string_item": {**d, "EarlySnapshotIntervals": [1.0, "5"]},
        "null_array": {**d, "EarlySnapshotIntervals": None},
        "object": {**d, "Seed": {"value": 1}},
    }
    results = rebuildcheck.run([{"op": "params", "json": v, "complete": c} for v in bad.values() for c in (False, True)], runtime)
    for (name, _), r in zip([(n, c) for n in bad for c in (False, True)], results):
        assert not r["ok"], (name, r)
    by_name = dict(zip(bad, results[::2]))
    assert "'seed'" in by_name["lower_extra"]["error"] and "case-sensitive: Seed" in by_name["lower_extra"]["error"]
    assert "'SEED'" in by_name["upper"]["error"]
    assert "params.RollMode" in by_name["string_enum"]["error"] and "integer" in by_name["string_enum"]["error"]
    assert "params.EarlySnapshotIntervals" in by_name["string_item"]["error"]
    # Integers are numbers for float fields; JSON order and exact duplicates as Python reads them.
    ok, = rebuildcheck.run_text('[{"op": "params", "complete": true, "json": %s}]'
                                % json.dumps({**d, "FastAfkStepMinutes": 120}), runtime)
    assert ok["ok"] and ok["hash"] == P.resolve(["defaults"]).params_hash


@each_runtime
def test_validate_rejects_each_bad_value(runtime):
    d = P.resolve(["defaults"]).params
    cases = {
        "SchemaVersion": {"SchemaVersion": 2},
        "DpsCacheProvenance": {"DpsCacheProvenance": 3},
        "RollMode": {"RollMode": 2},
        "AutoBalanceMode": {"AutoBalanceMode": 2},
        "EarlySnapshotIntervalStarts must start": {"EarlySnapshotIntervalStarts": [1.0, 20.0]},
        "EarlySnapshotIntervalStarts must start ": {"EarlySnapshotIntervalStarts": [0.0], "EarlySnapshotIntervals": [1.0, 5.0]},
        "EarlySnapshotIntervalStarts must start  ": {"EarlySnapshotIntervalStarts": [], "EarlySnapshotIntervals": []},
        "EarlySnapshotIntervalStarts must increase": {"EarlySnapshotIntervalStarts": [0.0, 0.0]},
        "EarlySnapshotIntervalStarts must increase ": {"EarlySnapshotIntervalStarts": [0.0, 20.0, 10.0],
                                                       "EarlySnapshotIntervals": [1.0, 5.0, 5.0]},
        "EarlySnapshotIntervals must be > 0": {"EarlySnapshotIntervals": [1.0, 0.0]},
        "EarlySnapshotIntervals must be > 0 ": {"EarlySnapshotIntervals": [-1.0, 5.0]},
        "FastAfkStepMinutes must be > 0": {"FastAfkStepMinutes": 0.0},
        "FastAfkStepMinutes must be > 0 ": {"FastAfkStepMinutes": -120.0},
        "FastAfkStepMinutes must be finite": {"FastAfkStepMinutes": 1e39},  # parses to float +inf
        "FastAfkStepMinutes must be finite ": {"FastAfkStepMinutes": math.inf},
        "FastAfkStepMinutes must be finite  ": {"FastAfkStepMinutes": math.nan},
        "EarlySnapshotIntervals must be finite": {"EarlySnapshotIntervals": [1.0, math.inf]},
    }
    results = rebuildcheck.run([{"op": "params", "json": {**d, **v}, "complete": True} for v in cases.values()], runtime)
    for expect, r in zip(cases, results):
        assert r["ok"] and r["valid"] and expect.strip() in r["valid"], (expect, r)
    # Every enum member is valid, including the ablation DpsCacheProvenance 2, and written as its integer.
    members = [{"DpsCacheProvenance": 2}, {"DpsCacheProvenance": 1}, {"RollMode": 1}, {"AutoBalanceMode": 1}]
    results = rebuildcheck.run([{"op": "params", "json": {**d, **v}, "complete": True} for v in members], runtime)
    for v, r in zip(members, results):
        assert r["ok"] and r["valid"] is None and r["hash"] == P.resolve(["defaults"], v).params_hash, (v, r)
        assert json.loads(r["effective"])[next(iter(v))] == next(iter(v.values()))


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


def test_rebuildcheck_sources_are_tracked():
    """The tool builds from tracked files only (the root .gitignore ignores *.csproj: it is force-added)."""
    for path in rebuildcheck.sources():
        rel = path.resolve().relative_to(REPO.resolve())
        proc = rebuildcheck.subprocess.run(["git", "-C", str(REPO), "ls-files", "--error-unmatch", str(rel)],
                                           capture_output=True, text=True)
        assert proc.returncode == 0, f"{rel} is not tracked: {proc.stderr.strip()}"


def test_request_load_reads_params_exactly():
    """SimulationRunRequest.Load hands every job's params through the exact reader, and the loader uses them."""
    text = (SIM / "Harness" / "SimulationRunRequest.cs").read_text(encoding="utf-8")
    load = text[text.index("public static SimulationRunRequest Load("):text.index("public string Validate()")]
    assert "request.ReadExactParams(text);" in load and "return request;" in load
    loader = (SIM / "Rebuild" / "SimulationRebuildParamsAsset.cs").read_text(encoding="utf-8")
    assert "job.ParamsReadError" in loader and "SimulationRebuildParams.FromRequest(job.Params, job.ParamsHash" in loader


def test_digest_is_taken_after_the_day_override():
    """Design 2.6/4.6: day d's digest covers the state after the forced snapshot and the day override, on day 0
    and at every day change. (No C3c job runs an override; C4 adds the behavioural check, Runbook 17.4.)"""
    text = (SIM / "Systems" / "SimulationSystem.Run.cs").read_text(encoding="utf-8")
    code = [ln.strip() for ln in re.sub(r"//[^\n]*", "", text).splitlines() if ln.strip()]
    starts = [i for i, ln in enumerate(code) if ln == "RebuildTraceDayStart();"]
    assert len(starts) == 2
    for i in starts:
        assert re.fullmatch(r"RebuildApplyDayOverride\((0|day)\);", code[i - 1]) and code[i - 2] == "MakeSnapshot(true);", code[i - 2:i + 1]


def test_job_params_are_checked_against_the_class():
    d = P.resolve(["defaults"]).params
    paramsio.job("ok", params={"Seed": 3})
    paramsio.job("ok2", params=d)
    for bad in ({"seed": 3}, {"Seed": "3"}, {"Seed": 3.0}, {"Bogus": 1}, {"RollMode": "Seeded"}):
        with pytest.raises(req.RequestError):
            paramsio.job("bad", params=bad)


def test_request_validate_mirrors_the_loader_checks():
    """request.validate refuses what LoadBatch fails with exit 5 (jobs the engine loads) and leaves empty jobs'
    params alone, as C# does (C0b's round-trip example carries a free-form object)."""
    d = P.resolve(["defaults"]).params

    def check(**fields):
        req.validate(req.make_request([{**engine.engine_job("j", day=1), **fields}], out_dir="/x"))

    check(params=d, params_hash=req.params_hash(d))
    check(params={"Seed": 3}, params_hash=None)
    check(params=None, params_hash=None, params_source="asset")
    req.validate(req.roundtrip_example())
    bad = [dict(params=d, params_hash="0" * 64),
           dict(params=None, params_hash=req.params_hash(d)),
           dict(params=d, params_hash=None, params_source="asset"),
           dict(params=None, params_hash=None, params_source="Asset"),
           *(dict(params=p, params_hash=req.params_hash(p))
             for p in ({"seed": 3}, {"Seed": "3"}, {"Seed": 3.0}, {"Bogus": 1}, {"RollMode": "Seeded"},
                       {**d, "EarlySnapshotIntervals": None}))]
    for fields in bad:
        with pytest.raises(req.RequestError):
            check(**fields)


def test_asset_reader_parses_bool_fields_as_unity_writes_them(tmp_path):
    """No bool field exists yet (C5 plans KillLossFromAb); Unity writes bools as 0 or 1."""
    cs = P.read_csharp_params()
    with_bool = P.CsParams(fields={**cs.fields, "Flag": "bool"}, defaults={**cs.defaults, "Flag": "false"}, enums=cs.enums)
    text = paramsio.asset_text(P.resolve(["defaults"]).params, cs)
    for raw, value in (("0", False), ("1", True)):
        (tmp_path / "a.asset").write_text(text.replace("    Seed: 1\n", f"    Seed: 1\n    Flag: {raw}\n"))
        assert paramsio.read_asset(tmp_path / "a.asset", with_bool)["Flag"] is value
    for raw in ("2", "true", "yes", "''"):
        (tmp_path / "b.asset").write_text(text.replace("    Seed: 1\n", f"    Seed: 1\n    Flag: {raw}\n"))
        with pytest.raises(paramsio.ParamsIoError):
            paramsio.read_asset(tmp_path / "b.asset", with_bool)
