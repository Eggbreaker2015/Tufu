"""Fast phase (unit C5) checks that need no Unity: parameter defaults and enums, request field and outputs, the
kill gate's annotation, the reference flush's shape, and the sensitivity helpers on synthetic data."""

from __future__ import annotations

import gzip
import json
import math
import re
import struct
from dataclasses import dataclass

import pytest

from tufu_balance.calibrate import params as P, sensitivity as S
from tufu_balance.model import registry
from tufu_balance.model.enums import StatType
from tufu_balance.unity import fasttick as ft, request as req, schema

SIM = ft.PARAMS_SOURCE.parents[1]
FAST_TICK = SIM / "Systems" / "SimulationSystem.FastTick.cs"
RECORDER = SIM / "Harness" / "SimulationFastTickRecorder.cs"
REQUEST = SIM / "Harness" / "SimulationRunRequest.FastTick.cs"


def test_defaults_equal_the_code_and_keep_the_regression_configuration():
    """Design 4.5, 7.3 C5: KillLossMultiplier = 1.0 (group A), KillLossFromAb = false, every diagnostic switch off,
    same-day planning on, FarmedTime with kappa 0.245."""
    defaults = ft.fasttick_defaults()
    cs = P.read_csharp_params()
    code = P.csharp_defaults(cs)
    assert defaults == {k: code[k] for k in defaults}
    assert set(defaults) == {k for k, path in _field_files(cs).items() if path.endswith("SimulationRebuildParams.FastTick.cs")}
    assert defaults["KillLossMultiplier"] == 1.0 and defaults["KillLossFromAb"] is False
    assert {k: defaults[k] for k in ft.DIAGNOSTICS_OFF} == ft.DIAGNOSTICS_OFF
    assert defaults["PlanNewIslandsSameDay"] is True and defaults["KillState"] == ft.KILL_STATE["StartOfDay"]
    assert defaults["ActiveMinutesModel"] == ft.ACTIVE_MINUTES_MODEL["FarmedTime"] and defaults["Kappa"] == 0.245
    resolved = P.resolve(["defaults"])
    assert {k: resolved.params[k] for k in defaults} == defaults


def _field_files(cs):
    out = {}
    for rel in cs.files:
        text = (SIM / rel).read_text(encoding="utf-8")
        for name in cs.fields:
            if re.search(rf"^\t\tpublic [\w\[\]]+ {name} = ", text, re.M):
                out[name] = rel
    return out


def test_enums_match_csharp():
    cs = P.read_csharp_params()
    assert cs.enums["KillState"] == ft.KILL_STATE and cs.enums["ActiveMinutesModel"] == ft.ACTIVE_MINUTES_MODEL
    assert P.resolve(["defaults"], {"KillState": "AfterPass"}).params["KillState"] == 1


def test_request_field_and_outputs():
    text = REQUEST.read_text(encoding="utf-8")
    assert re.findall(r'\[JsonProperty\("fast_tick_probe"', text)
    probe = re.search(r"class FastTickProbe\s*\{(.*?)\n\t\t\}", text, re.S).group(1)
    assert set(re.findall(r'\[JsonProperty\("([a-z_]+)"\)\]', probe)) == set(ft.PROBE_FIELDS)
    job = ft.fast_job("x", "replay", override=None, reference_flush=True, spawner_days=True, day=10)
    request = req.make_request([job], out_dir="/tmp/x")
    req.validate(request)
    assert job["fast_tick_probe"] == {"reference_flush": True} and job["outputs"] == {"spawner_days": True}
    assert job["params"] == P.resolve(["defaults"]).params and job["params_hash"] == req.params_hash(job["params"])
    with pytest.raises(req.RequestError):
        req.validate(req.make_request([dict(job, fast_tick_probe={"reference_flush": 1})], out_dir="/tmp/x"))
    assert schema.OUTPUTS["fasttick_days"] == "C5" and schema.OUTPUTS["spawner_days"] == "C5"
    rec = RECORDER.read_text(encoding="utf-8")
    for const, value in (("DaysFile", ft.DAYS_FILE), ("SpawnerDaysFile", ft.SPAWNER_DAYS_FILE), ("Schema", ft.SCHEMA),
                         ("SpawnerDaysHeader", ",".join(ft.SPAWNER_DAYS_COLUMNS))):
        assert f'public const string {const} = "{value}";' in rec


def test_kill_gate_annotation():
    """Design 4.1, 4.5: the eligibility is Provenance.Inferred with formula id pending:simulation.fast_kill_gate; the
    planning and active minutes are calibrated, accumulation and flush inferred."""
    found = {a.member: (a.provenance, a.formula_id) for a in registry.scan_csharp() if a.file.endswith(FAST_TICK.name)}
    assert found["SimulateFightWithSpawnerFast"] == ("Inferred", ft.KILL_GATE_FORMULA)
    assert found["FastTick"][0] == "Calibrated"
    assert found["AccumulateFastTickSpawnerRewards"][0] == found["FlushFastTickChunkRewards"][0] == "Inferred"


def _method(text, name):
    start = re.search(rf"\n\t\t(?:private|internal) [\w<>\[\], ]+ {name}\(", text).start()
    end = text.index("\n\t\t}\n", start)
    return text[start:end]


def test_reference_flush_is_editor_only_and_calls_no_rebuild_method():
    """Design 4.5 native-replacement acceptance: the reference flush reads the accumulated fields and calls no Rebuild*
    method; it and its switch are compiled only into the editor."""
    text = FAST_TICK.read_text(encoding="utf-8")
    body = _method(text, "RebuildReferenceFlush").split("\n", 2)[2]
    assert re.findall(r"\bRebuild\w*\s*\(", body) == []
    for field in ("fastTickChunkStatRewards", "fastTickChunkItemRewards", "fastTickChunkSoulRewards",
                  "fastTickChunkGateRewardSpawners", "fastTickChunkKillCount"):
        assert field in body
    editor = re.findall(r"#if UNITY_EDITOR\n(.*?)#endif", text, re.S)
    assert len(editor) == 2 and "RebuildReferenceFlush()" in editor[0] and "private void RebuildReferenceFlush()" in editor[1]
    assert text.count("RebuildReferenceFlush") == 2
    accumulate = _method(text, "AccumulateFastTickSpawnerRewards")
    for field in ("fastTickChunkStatRewards", "fastTickChunkItemRewards", "fastTickChunkSoulRewards",
                  "fastTickChunkGateRewardSpawners", "fastTickChunkKillCount"):
        assert field in accumulate


# ---------------------------------------------------------------- sensitivity on synthetic data


@dataclass
class _Stat:
    key: int
    value: float


@dataclass
class _Snap:
    time_chunk_index: int
    stats: list
    playtime: float = 0.0


H, R = int(StatType.Health), int(StatType.HealthRegen)


def _snaps(values, playtimes=None):
    return [_Snap(tc, [_Stat(H, h), _Stat(R, r)], (playtimes or {}).get(tc, 0.0)) for tc, (h, r) in values]


def test_interval_results_and_explanations():
    snaps = _snaps([(4, (0, 0)), (5, (100, 1)), (7, (300, 3)), (9, (450, 4))])
    gains = {4: {H: 100, R: 1}, 5: {H: 100, R: 1}, 6: {H: 100, R: 1}, 7: {H: 100, R: 1}, 8: {H: 100, R: 1}}
    res = S.interval_results(snaps, gains, stats=(H, R))
    assert [r.exact for r in res] == [True, True, False]
    bad = S.bad_intervals(res)[0]
    assert bad.failing == {H: -50.0, R: -1.0} and bad.unit == "007->009"
    # a spawner first cleared on day 8 giving 50/1 a day: the data cleared it a day later
    expl = S.explain(bad, {"s": 8, "t": 5}, {"s": {H: 50, R: 1}, "t": {H: 50, R: 1}})
    assert expl == S.Explanation(8, 1, ("s",))
    assert S.explain(bad, {"s": 8}, {"s": {H: 40, R: 1}}) is None
    # one day more: residual +R for a first kill at b
    more = S.interval_results(_snaps([(7, (0, 0)), (9, (250, 0))]), {7: {H: 100}, 8: {H: 100}}, stats=(H, R))[0]
    assert S.explain(more, {"u": 9}, {"u": {H: 50}}) == S.Explanation(9, -1, ("u",))
    missing = S.interval_results(snaps[:2], {}, stats=(H,))[0]
    assert missing.missing_days == (4,) and not missing.exact
    assert S.tolerance(1e6) == 20.0 and S.tolerance(10) == 0.5


def test_closed_loop_bound_from_the_curve():
    curve = {0.95: 7, 0.98: 6, 0.99: 5, 1.0: 4, 1.01: 3, 1.02: 2, 1.05: 3}
    assert S.closed_loop_exact_min(curve, 0.02) == 35 and S.closed_loop_exact_min(curve, 0.0) == 37
    assert S.closed_loop_exact_min(curve, 0.05) == 34
    with pytest.raises(ValueError):
        S.closed_loop_exact_min({0.9: 1}, 0.01)


def test_t04_and_kappa():
    snaps = _snaps([(10, (0, 0)), (11, (0, 0)), (13, (0, 0)), (104, (0, 0)), (105, (0, 0))],
                   {10: 0.0, 11: 20.0, 13: 80.0, 104: 3000.0, 105: 3030.0})
    farmed = {d: 60.0 * 100.0 for d in range(10, 105)}
    k = S.fit_kappa(snaps, farmed)
    refs = (60.0 / 2, 2920.0 / 91)  # intervals 11->13 and 13->104 (training: 11 <= a < 104)
    assert k == pytest.approx(sum(r * 100 for r in refs) / (2 * 100 * 100), rel=1e-12)
    cells = S.t04(snaps, {d: 30.0 for d in range(10, 105)})
    assert [c.group for c in cells] == ["tc_4_10", "tc_ge_11", "tc_ge_11", "tc_ge_11"]
    summary = S.t04_summary(cells)
    assert summary["n"] == 3 and summary["max"] == pytest.approx(1 - 30 / (2920 / 91)) and summary["median"] == 0.0


def test_unlocks_t07_t08():
    events = [{"type": "unlock", "location": 4, "day": 5}, {"type": "first_kill", "spawner": "x", "location": 4},
              {"type": "unlock", "location": 5, "day": 9}]
    unlocks = S.unlock_days(events)
    assert unlocks == {4: 5, 5: 9}
    t07 = S.t07(unlocks)
    assert t07[4] == {"day": 5, "range": [5, 5], "distance": 0, "ok": True}
    assert t07[5]["distance"] == 1 and t07[5]["ok"] and not t07[6]["ok"]
    with pytest.raises(ValueError):
        S.unlock_days(events + [{"type": "unlock", "location": 4, "day": 6}])
    t08 = S.t08({("dragon", 4): 9, ("Spawner_1_elite health", 8): 26, ("Spawner_17_death", 17): 190})
    assert t08["dragon@4"]["ok"] and t08["dragon@4"]["distance"] == 1
    assert t08["Spawner_1_elite health@8"] == {"day": 26, "range": [26, None], "distance": 1, "ok": True}
    assert t08["Spawner_17_death@17"]["distance"] == 0 and t08["Spawner_elite health@4"]["day"] is None


def test_readers_round_trip():
    d = lambda x: struct.pack(">d", x).hex()
    f = lambda x: struct.pack(">f", x).hex()
    line = {"day": 4, "playtime": f(244.0), "location_start": 1, "location_end": 3, "kill_loss": d(1.0),
            "rounds": [{"location": 1, "bot": 10, "eligible": 9, "signature": "s", "clears": 20, "kills": 30,
                        "fight_seconds": d(1.5), "farmed_seconds": d(9.0), "unlocked": 2}],
            "round_unlocks": [2], "clears": 20, "kills": 30, "kill_gate_rejections": 1, "fight_seconds": d(1.5),
            "farmed_seconds": d(9.0), "tour": d(0.0), "speed": d(5.0), "active_minutes": f(0.03675),
            "stats": {"1": d(1365.0)}, "items": {}, "souls": {"common": d(12.0)}, "kill_count": 30, "gate_spawners": 0,
            "flush_unlocks": [], "flush_stat_modifiers": 1}
    [day] = ft.read_days(json.dumps(line) + "\n")
    assert day.day == 4 and day.stats == {1: 1365.0} and day.rounds[0].farmed_seconds == 9.0 and day.round_unlocks == [2]
    header = ",".join(ft.SPAWNER_DAYS_COLUMNS) + "\n"
    data = gzip.compress((header + "4,1,Main:a|k,1,0,1,3,1.5,9,0,100,4,12,0,12\n").encode()) + \
        gzip.compress(b"5,1,Main:a|k,1,0,1,3,1.5,9,50,100,4,12,0,12\n5,2,Main:b|j,2,1,0,0,2,9,300,100,1,1,0,0\n")
    rows = ft.read_spawner_days(data)
    assert [r.day for r in rows] == [4, 5, 5] and rows[2].dafc and not rows[2].killable
    assert rows[1].loss_over_h == 0.5 and ft.kill_days(rows) == {"Main:a|k": 4}
    assert ft.charge_counts(rows) == {(4, "Main:a|k"): 3, (5, "Main:a|k"): 3}
    assert ft.charge_usage(rows) == {(4, "Main:a|k"): 12, (5, "Main:a|k"): 12}
    legacy = ft.read_spawner_days(gzip.compress((",".join(ft.SPAWNER_DAYS_COLUMNS[:11]) +
                                                "\n4,1,Main:a|k,1,0,1,3,1.5,9,0,100\n").encode()))
    assert ft.charge_counts(legacy) == {(4, "Main:a|k"): 3}
    with pytest.raises(schema.SchemaError, match="explicit charge columns"):
        ft.charge_usage(legacy)
    with pytest.raises(schema.SchemaError):
        ft.read_spawner_days(gzip.compress(b"day\n"))
    assert math.isinf(S.T08[("Spawner_1_elite health", 8)][1])
