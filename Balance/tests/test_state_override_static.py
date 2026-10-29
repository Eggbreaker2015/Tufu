"""Replay/checkpoint override without Unity (design 1.4.2, 4.6; unit C4): request fields on both sides, Python
validation, defaults, the oracle of what a day's override sets, and the engine's call order in source."""

from __future__ import annotations

import copy
import re

import pytest

from tufu_balance.model.data import load_simulation_data
from tufu_balance.unity import override as ov, request as req

SIM = ov.BALANCE.parent / "Recovery" / "StudyProject" / "Assets" / "Scripts" / "Assembly-CSharp" / "CodeBase" / "Simulation"
CS_OVERRIDE = SIM / "Harness" / "SimulationRunRequest.Override.cs"
CS_REPLAY = SIM / "Systems" / "SimulationSystem.Replay.cs"
CS_SNAPSHOT = SIM / "Systems" / "SimulationSystem.Snapshot.cs"


def _class_fields(text: str, name: str) -> set[str]:
    body = re.search(r"partial class " + name + r"\s*\{(.*?)\n\t\t\}", text, re.S).group(1)
    return set(re.findall(r'\[JsonProperty\("([a-z_]+)"', body))


def test_csharp_fields_and_values_match_python():
    text = CS_OVERRIDE.read_text(encoding="utf-8")
    assert re.search(r'\[JsonProperty\("override", NullValueHandling = NullValueHandling.Ignore\)\] public OverrideOptions Override;', text)
    assert _class_fields(text, "OverrideOptions") == set(ov.OVERRIDE_FIELDS)
    assert _class_fields(text, "SlotCounts") == {"weapon", "armor"}
    for cs_name, values in (("OverrideModes", ov.OVERRIDE_MODES), ("EconCachesValues", ov.ECON_CACHES),
                            ("ReplayItemsValues", ov.REPLAY_ITEMS), ("UnlockValues", ov.UNLOCK),
                            ("TotalValueValues", ov.TOTAL_VALUE), ("RpmAfterOverrideValues", ov.RPM_AFTER_OVERRIDE)):
        found = re.search(cs_name + r" = \{(.*?)\};", text).group(1)
        assert tuple(re.findall(r'"(\w+)"', found)) == values, cs_name
    defaults = dict(re.findall(r'public string (\w+) = "(\w+)";', text))
    assert defaults == {"EconCaches": "stored", "ReplayItems": "snapshot", "Unlock": "derived",
                        "RpmAfterOverride": "recompute"}
    assert "public string TotalValue;" in text
    assert 'TotalValue ?? (mode == "checkpoint" ? "compose" : "snapshot")' in text
    assert ov.override_options() == {"from_day": None, "econ_caches": "stored", "replay_items": "snapshot",
                                     "unlock": "derived", "total_value": None, "rpm_after_override": "recompute",
                                     "items_evolved": None, "slots": None}
    assert [ov.resolved_total_value(m, ov.override_options()) for m in ov.OVERRIDE_MODES] == ["snapshot", "compose"]
    assert ov.resolved_total_value("checkpoint", ov.override_options(total_value="compose")) == "compose"


def test_checkpoint_rejects_the_total_value_correction():
    """The correction modifiers last until the next override or equipment change; in checkpoint that is the closed
    loop, where K2/K3 move on (and a K patch would be pulled back to the shipped TotalValue)."""
    text = CS_OVERRIDE.read_text(encoding="utf-8")
    assert 'o.TotalValue == "snapshot" && job.Mode == "checkpoint"' in text
    ok = {"job_id": "j", "mode": "checkpoint", "override": ov.override_options(total_value="compose")}
    ov.check_override(ok)
    ov.check_override({**ok, "override": ov.override_options()})
    with pytest.raises(req.RequestError, match="replay only"):
        ov.check_override({**ok, "override": ov.override_options(total_value="snapshot")})
    ov.check_override({**ok, "mode": "replay", "override": ov.override_options(total_value="snapshot")})


def _job(**override):
    o = ov.override_options()
    o.update(override)
    return {"job_id": "j", "mode": "replay", "override": o}


@pytest.mark.parametrize("bad, message", [
    ({"from_day": -1}, "from_day"),
    ({"from_day": True}, "from_day"),
    ({"econ_caches": "cached"}, "econ_caches"),
    ({"replay_items": "none"}, "replay_items"),
    ({"unlock": "always"}, "unlock"),
    ({"total_value": "interpolated"}, "total_value"),
    ({"rpm_after_override": "never"}, "rpm_after_override"),
    ({"rpm_after_override": None}, "must not be null"),
    ({"items_evolved": {"x": {}}}, "TimeChunkIndex"),
    ({"items_evolved": {"04": {}}}, "TimeChunkIndex"),
    ({"items_evolved": {"4": []}}, "must be an object"),
    ({"items_evolved": {"4": {"knife": 1}}}, "true, false or null"),
    ({"slots": {"4": {"weapon": 2}}}, "armor is required"),
    ({"slots": {"4": {"weapon": -1, "armor": 0}}}, ">= 0"),
    ({"slots": {"4": {"weapon": 1, "armor": 0, "vip": 1}}}, "unknown field"),
    ({"slots": {"4": {"weapon": 1.0, "armor": 0}}}, "must be int"),
    ({"extra": 1}, "unknown field"),
])
def test_python_validation_rejects(bad, message):
    with pytest.raises(req.RequestError, match=message):
        ov.check_override(_job(**bad))


def test_valid_override_in_a_request():
    job = ov.override_job("r", "replay", ov.override_options(from_day=4, items_evolved={"3": {"knife": None}},
                                                             slots={"3": {"weapon": 2, "armor": 1}}),
                          probe={"null_policy": True, "unlock_through": 0, "write_points": False}, day=10)
    request = req.make_request([job], out_dir="/tmp/x")
    req.validate(request)
    plain = req.make_request([req.make_job("f")], out_dir="/tmp/x")
    req.validate(plain)  # override stays optional
    broken = copy.deepcopy(request)
    broken["jobs"][0]["override"]["unlock"] = "x"
    with pytest.raises(req.RequestError):
        req.validate(broken)


def test_defaults_cover_every_shipped_day_snapshot(db):
    shipped = load_simulation_data(db)
    tcs = [str(s.time_chunk_index) for s in shipped.time_chunk_snapshots]
    evolved = ov.items_evolved_default(db)
    assert sorted(evolved, key=int) == tcs
    assert all(set(per) == {it.key for it in s.items} for per, s in zip((evolved[t] for t in tcs), shipped.time_chunk_snapshots))
    slots = ov.slots_fixture()
    assert sorted(slots, key=int) == tcs
    p12 = ov.slots_p12(db)
    assert sorted(p12, key=int) == tcs and p12["0"] == {"weapon": 1, "armor": 0}
    full = ov.default_override(db, from_day=4)
    assert full["items_evolved"] == evolved and full["slots"] == slots and full["from_day"] == 4


def test_expected_day_takes_the_item_side_of_get_by_time_chunk_index(db):
    shipped = load_simulation_data(db)
    by_tc = {s.time_chunk_index: s for s in shipped.time_chunk_snapshots}
    # an interpolated day: items of the left snapshot; a shipped day other than the first and last: t = 1, the
    # Values of that day, the items of the one before (GetByTimeChunkIndex never interpolates items)
    e = ov.expected_day(shipped, "replay", 12, None)
    assert e.items_time_chunk == 11 and e.snapshot.time_chunk_index == 13 and e.snapshot.location == by_tc[13].location
    e = ov.expected_day(shipped, "replay", 13, None)
    assert e.items_time_chunk == 11 and [st.value for st in e.snapshot.stats] == [st.value for st in by_tc[13].stats]
    assert ov.expected_day(shipped, "replay", 0, None).items_time_chunk == 0
    assert ov.expected_day(shipped, "replay", 405, None).items_time_chunk == 405
    assert ov.expected_day(shipped, "replay", 406, None).items_time_chunk == 405
    # checkpoint: shipped days only, the snapshot itself
    assert ov.expected_day(shipped, "checkpoint", 12, None) is None
    e = ov.expected_day(shipped, "checkpoint", 19, ov.items_evolved_default(db))
    assert e.items_time_chunk == 19 and e.snapshot is by_tc[19] and ("knife", 1, 86, True) in e.items
    # without items_evolved: evolved iff Level != Copies (the C# fallback)
    assert ("knife", 1, 86, True) in ov.expected_day(shipped, "checkpoint", 19, None).items
    # a request flag overrules Level != Copies; unevolved items hold their Copies as the effective level
    flipped = ov.expected_day(shipped, "checkpoint", 19, {"19": {"knife": False}}).items
    assert ("knife", 86, 86, False) in flipped
    assert ("knife", 117, 117, True) in ov.expected_day(shipped, "checkpoint", 17, {"17": {"knife": True}}).items


def test_engine_marks_the_day_applied_before_it_reads_the_stored_caches():
    text = CS_REPLAY.read_text(encoding="utf-8")
    body = text[text.index("partial void RebuildApplyDayOverride(int day)"):text.index("partial void RebuildApplyReplayCaches()")]
    order = [body.index(s) for s in ("state.Resolve(day)", "RebuildReplaySetSlots", "RebuildReplaySetItems(",
                                     "RebuildReplaySetStats(", "RebuildReplaySetTotalValues(", "state.Applied(s)", "CalculateSimulationData();",
                                     "RebuildApplyReplayCaches();", "if (state.RecomputeRpm)", "CalculateRPMValueTimeChunk(type)",
                                     "state.Record(")]
    assert order == sorted(order)


def test_snapshot_items_carry_the_effective_level():
    """Design 4.5: snapshot Items are (Key, effective level, Copies); Level == Copies until evolved (shipped data)."""
    text = CS_SNAPSHOT.read_text(encoding="utf-8")
    assert "new SimulationItemData { Key = item.Config.Key, Level = item.EffectiveLevel, Copies = item.Copies }" in text


def test_unlock_snapshot_opens_forward_gates_only():
    """Back-gates (islands 11-13 into lower islands) open only when their spawner is cleared (C3b), never by the
    snapshot unlock: the loop skips spawners not on an already open island."""
    text = CS_REPLAY.read_text(encoding="utf-8")
    body = text[text.index("private void RebuildReplayUnlock("):text.index("private bool RebuildReplaySetSlots(")]
    assert "def.LocationNumber >= n ||" in body
