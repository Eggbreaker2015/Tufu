"""Replay and checkpoint state override in Unity (design 4.6, 7.3 C4).

Every job runs under the empty policy (``engine_probe.null_policy``: no FindBestSpawner, C3b's minimal FastTick),
so nothing but the override changes the state and each check isolates it. Expected values come from the Python
oracle (``model.snapshots`` for GetByTimeChunkIndex, ``model.simcalc`` for DpsData, P4/P6 for evolved flags and
bosses); ``override_days.jsonl`` holds the state the engine has right after each override.
"""

from __future__ import annotations

import json
import math
import struct

import pytest

from tufu_balance.golden import check, exact
from tufu_balance.model import simcalc as sc, snapshots as ss, spawners as sp
from tufu_balance.model.data import load_interpolations, load_simulation_data
from tufu_balance.model.items import ItemState
from tufu_balance.unity import engine, override as ov, request as req, schema, spawner_map

pytestmark = pytest.mark.unity

NULL = engine.engine_probe(null_policy=True)
LAST = 405
SHORT = 30


def _ok(result):
    assert result.exit_code == 0, result.describe()
    return result


@pytest.fixture(scope="module")
def shipped(db):
    return load_simulation_data(db)


@pytest.fixture(scope="module")
def interp(db):
    return load_interpolations(db)


@pytest.fixture(scope="module")
def ctx(db):
    return sc.load_simcalc_context(db)


@pytest.fixture(scope="module")
def evolved(db):
    return ov.items_evolved_default(db)


@pytest.fixture(scope="module")
def slots():
    return ov.slots_fixture()


def _jobs(db):
    full = ov.default_override(db, from_day=4)
    return [
        ov.override_job("replay-405", "replay", full, probe=NULL, day=LAST, max_wall_seconds=300.0),
        ov.override_job("checkpoint-405", "checkpoint", ov.default_override(db), probe=NULL, day=LAST, max_wall_seconds=300.0),
        ov.override_job("replay-recomputed", "replay", {**full, "econ_caches": "recomputed"}, probe=NULL, day=SHORT,
                        max_wall_seconds=300.0),
        ov.override_job("replay-items-recompute", "replay", {**full, "replay_items": "recompute"}, probe=NULL, day=SHORT,
                        max_wall_seconds=300.0),
        ov.override_job("replay-unlock-snapshot", "replay", {**full, "unlock": "snapshot", "slots": None}, probe=NULL,
                        day=SHORT, max_wall_seconds=300.0),
        ov.override_job("replay-slow", "replay", {**full, "from_day": None}, probe=NULL, day=6, max_wall_seconds=300.0),
        ov.override_job("free-ignores-override", "free", full, probe=NULL, day=6, max_wall_seconds=300.0),
    ]


@pytest.fixture(scope="module")
def run(unity_run, db):
    return _ok(unity_run(req.make_request(_jobs(db))))


def _days(result, job):
    return ov.read_days(result.file(job, ov.DAYS_FILE).decode())


def _full(result, job):
    return json.loads(result.file(job, "timechunk_full.json"))["TimeChunkSnapshots"]


def _early(result, job):
    return json.loads(result.file(job, "simulation_data.json"))["Snapshots"]


def _bits(x: float) -> bytes:
    return struct.pack("<d", x)


def _same(a, b) -> bool:
    return [_bits(x) for x in a] == [_bits(x) for x in b]


def _pairs_same(a, b) -> bool:
    return [w for w, _ in a] == [w for w, _ in b] and _same([v for _, v in a], [v for _, v in b])


def _ulps(a: float, b: float) -> float:
    return 0.0 if a == b else abs(a - b) / math.ulp(b)


def _items(rec):
    """(Key, snapshot Level = effective level, Copies, Evolved), and the raw ItemModel.Level is 1 until evolved."""
    assert all(lv == (el if ev else 1) for _, lv, _, ev, el in rec.items), rec.items
    return [(k, el, c, ev) for k, _, c, ev, el in rec.items]


def _snapshot_items(s):
    return [(it["Key"], it["Level"], it["Copies"]) for it in s["Items"]]


class _Totals:
    """TotalValue of the overridden state: the engine's Stat.FastValue (game order K3*(1+K2)*Value) against the
    snapshot composition (Value*(1+K2))*K3 at the day's K (ulps) and against the interpolated snapshot's own
    TotalValue (ulps on shipped days, relative error on interpolated days)."""

    def __init__(self, interp, shipped_days):
        self.interp, self.shipped_days = interp, shipped_days
        self.compose_ulps = 0.0
        self.compose_cells = self.compose_off = 0
        self.shipped_ulps = 0.0
        self.interpolated_rel = 0.0
        self.k_mismatch_days = []

    def add(self, rec, snapshot):
        k2, k3 = ss.composition_coefficients(self.interp, rec.day)
        if _bits(k2) != _bits(rec.k2) or _bits(k3) != _bits(rec.k3):
            self.k_mismatch_days.append(rec.day)
        for key, (value, fast, _) in rec.stats.items():
            want = ss.compose_stat(key, value, k2, k3).total_value
            u = _ulps(fast, want)
            self.compose_cells += 1
            self.compose_off += u != 0.0
            self.compose_ulps = max(self.compose_ulps, u)
            stored = snapshot.stat(key).total_value
            if rec.day in self.shipped_days:
                self.shipped_ulps = max(self.shipped_ulps, _ulps(fast, stored))
            elif stored != 0.0:
                self.interpolated_rel = max(self.interpolated_rel, abs(fast - stored) / abs(stored))


def _check_state(rec, e, slots):
    """Values, caches, items, slots of one day against the oracle; returns the list of what differs."""
    bad = []
    if set(rec.stats) != {st.key for st in e.snapshot.stats}:
        bad.append("stat keys")
        return bad
    if not _same([rec.stats[st.key][0] for st in e.snapshot.stats], [st.value for st in e.snapshot.stats]):
        bad.append("values")
    for name, got in (("dps", rec.dps), ("dps_cache", rec.dps_cache)):
        if not _pairs_same(got, e.snapshot.dps_data):
            bad.append(name)
    for name, got in (("defense", rec.defense), ("defense_cache", rec.defense_cache)):
        if not _pairs_same(got, e.snapshot.defense_data):
            bad.append(name)
    if _items(rec) != e.items:
        bad.append("items")
    if slots is not None:
        want = slots[str(e.items_time_chunk)]
        if (rec.raw["weapon_slots"], rec.raw["armor_slots"]) != (want["weapon"], want["armor"]):
            bad.append("slots")
    if rec.current_health != rec.stats[1][2]:
        bad.append("current_health")
    return bad


def _recomputed_dps(ctx, interp, values, items, weapons, armors, k_day, timing):
    """CalculateDpsData/CalculateDefenseData from Value composed at K2/K3(k_day), items and slots, timing ``timing``."""
    k2, k3 = ss.composition_coefficients(interp, k_day)
    stats = {key: ss.compose_stat(key, v, k2, k3).total_value for key, v in values.items()}
    entries = [(ctx.items[k], ItemState.from_snapshot(lv, c, ev)) for k, lv, c, ev in items]
    return (sc.calculate_dps_data(ctx, stats, entries, weapons, timing),
            sc.calculate_defense_data(ctx, stats, entries, armors, timing))


# ---------------------------------------------------------------- replay + stored, days 4-405

def test_replay_once_per_day_with_the_interpolated_state(run, shipped, interp, evolved, slots):
    recs = _days(run, "replay-405")
    m = run.manifest("replay-405")
    schema.validate_manifest(m)
    o = m["override"]
    assert [r.day for r in recs] == list(range(4, LAST + 1)), "exactly one override per day 4..405"
    assert all(r.raw["time_chunk_index"] == r.day and r.raw["modifier_day"] == r.day for r in recs)
    assert o["applied"] == len(recs) and o["first_day"] == 4 and o["last_day"] == LAST
    assert o["calls"] == LAST + 1  # day 0 and every day change; days 0-3 are before from_day
    assert m["engine"]["counters"]["day_overrides"] == len(recs) and m["engine"]["counters"]["day_changes"] == LAST
    assert "repeat_day_calls" not in o["counters"] and "items_evolved_fallback" not in o["counters"]
    assert (o["econ_caches"], o["replay_items"], o["unlock"], o["items_evolved"], o["slots"]) == \
        ("stored", "snapshot", "derived", "request", "request")
    shipped_days = {s.time_chunk_index for s in shipped.time_chunk_snapshots}
    totals = _Totals(interp, shipped_days)
    bad = {}
    for r in recs:
        e = ov.expected_day(shipped, "replay", r.day, evolved)
        diff = _check_state(r, e, slots)
        if r.raw["location"] != 1 or r.raw["snapshot_time_chunk"] != e.snapshot.time_chunk_index \
                or r.raw["items_time_chunk"] != e.items_time_chunk:
            diff.append("bracket")
        if diff:
            bad[r.day] = diff
        totals.add(r, e.snapshot)
    print(f"TotalValue: engine vs composition max {totals.compose_ulps} ulp ({totals.compose_off}/{totals.compose_cells} "
          f"cells off); vs shipped TotalValue on shipped days max {totals.shipped_ulps} ulp; vs interpolated TotalValue "
          f"max rel {totals.interpolated_rel:.3g}")
    assert bad == {}
    assert totals.k_mismatch_days == []
    assert totals.compose_ulps <= 1.0 and totals.shipped_ulps <= 1.0
    check("sim_override", {
        "replay405.days": exact(len(recs), source="design 7.3 C4", original=402, note="one override per day 4..405"),
        "replay405.state_mismatch_days": exact(len(bad), source="design 7.3 C4", original=0,
                                               note="Values, DpsData/DefenseData and their caches, items, slots bit for bit"),
        "replay405.total_value_compose_max_ulp": exact(totals.compose_ulps, source="design 7.3 C4",
                                                       note="Stat.FastValue vs (Value*(1+K2))*K3 at K(d), double_all K"),
        "replay405.total_value_compose_cells_off": exact(totals.compose_off, source="design 7.3 C4"),
        "replay405.total_value_shipped_max_ulp": exact(totals.shipped_ulps, source="design 7.3 C4",
                                                       note="vs the shipped TotalValue on shipped day-snapshot days"),
        "replay405.total_value_interpolated_max_rel": exact(totals.interpolated_rel, source="design 4.6",
                                                            note="TotalValue is interpolated linearly, K2/K3 are not"),
        "replay405.counters": exact(o["counters"], source="design 7.3 C4"),
    })


def test_replay_state_carries_to_the_next_forced_snapshot(run):
    """Empty policy: day d+1's forced snapshot (taken before its override) shows day d's override: the Values and
    items (effective level) of the record, and the K(d+1) modifiers."""
    recs = {r.day: r for r in _days(run, "replay-405")}
    full = {s["TimeChunkIndex"]: s for s in _full(run, "replay-405")}
    bad = []
    for d in range(4, LAST):
        s, r = full[d + 1], recs[d]
        if not _same([st["Value"] for st in s["Stats"]], [r.stats[st["Key"]][0] for st in s["Stats"]]) \
                or _snapshot_items(s) != [(k, lv, c) for k, lv, c, _ in _items(r)]:
            bad.append(d)
    assert bad == []


def test_replay_405_runs_in_under_two_minutes(run):
    t = json.loads(run.file("replay-405", "timings.json"))
    print("replay-405 job wall", t["job_wall_s"], "s; fast ticks", t["fast_tick_ms"])
    assert t["job_wall_s"] < 120.0


# ---------------------------------------------------------------- checkpoint

def test_checkpoint_resets_at_shipped_day_snapshots(run, shipped, interp, evolved, slots):
    recs = _days(run, "checkpoint-405")
    m = run.manifest("checkpoint-405")
    tcs = [s.time_chunk_index for s in shipped.time_chunk_snapshots]
    assert [r.day for r in recs] == tcs
    assert m["override"]["applied"] == len(tcs) and m["override"]["calls"] == LAST + 1
    totals = _Totals(interp, set(tcs))
    bad = {}
    for r in recs:
        e = ov.expected_day(shipped, "checkpoint", r.day, evolved)
        diff = _check_state(r, e, slots)
        if r.raw["items_time_chunk"] != r.day or r.raw["left"] != -1:
            diff.append("source")
        if diff:
            bad[r.day] = diff
        totals.add(r, e.snapshot)
    assert bad == {}
    assert totals.compose_ulps <= 1.0 and totals.shipped_ulps <= 1.0
    check("sim_override", {
        "checkpoint405.days": exact(len(recs), source="design 7.3 C4", original=46),
        "checkpoint405.state_mismatch_days": exact(len(bad), source="design 7.3 C4", original=0),
        "checkpoint405.total_value_shipped_max_ulp": exact(totals.shipped_ulps, source="design 7.3 C4"),
    })


def test_checkpoint_continues_closed_loop(run, shipped, ctx, interp, evolved, slots):
    """Between checkpoints the engine runs on: the next forced snapshot keeps the reset Values and items, and its
    DpsData/DefenseData are recomputed by the recovered CalculateDpsData (RpmFirst: K2/K3(d) on the stats,
    timing d+1), no longer the stored ones."""
    full = {s["TimeChunkIndex"]: s for s in _full(run, "checkpoint-405")}
    tcs = [s.time_chunk_index for s in shipped.time_chunk_snapshots]
    bad, recomputed_differs = [], 0
    for s in shipped.time_chunk_snapshots:
        d = s.time_chunk_index
        if d == LAST:
            continue
        nxt = full[d + 1]
        values = {st.key: st.value for st in s.stats}
        items = ov.expected_day(shipped, "checkpoint", d, evolved).items
        if not _same([st["Value"] for st in nxt["Stats"]], [values[st["Key"]] for st in nxt["Stats"]]) \
                or _snapshot_items(nxt) != [(k, lv, c) for k, lv, c, _ in items]:
            bad.append((d, "state"))
        w = slots[str(d)]
        dps, defense = _recomputed_dps(ctx, interp, values, items, w["weapon"], w["armor"], d, d + 1)
        if not _pairs_same([(x["WeaponType"], x["Value"]) for x in nxt["DpsData"]], dps) \
                or not _pairs_same([(x["WeaponType"], x["Value"]) for x in nxt["DefenseData"]], defense):
            bad.append((d, "dps"))
        recomputed_differs += not _pairs_same(dps, s.dps_data)
    # days between checkpoints are not overridden: the state stays the last checkpoint's
    for a, b in zip(tcs, tcs[1:]):
        for d in range(a + 2, b + 1):
            if not _same([st["Value"] for st in full[d]["Stats"]], [st["Value"] for st in full[a + 1]["Stats"]]):
                bad.append((d, "between"))
    assert bad == []
    assert recomputed_differs > 0
    check("sim_override", {
        "checkpoint405.recomputed_dps_differs_from_stored": exact(recomputed_differs, source="design 4.6",
                                                                   note="shipped days whose closed-loop DpsData (next day) "
                                                                        "differs from the stored one"),
    })


# ---------------------------------------------------------------- options

def test_econ_caches_recomputed(run, shipped, ctx, interp, evolved):
    recs = _days(run, "replay-recomputed")
    assert run.manifest("replay-recomputed")["override"]["econ_caches"] == "recomputed"
    assert [r.day for r in recs] == list(range(4, SHORT + 1))
    bad, differs = [], 0
    for r in recs:
        e = ov.expected_day(shipped, "replay", r.day, evolved)
        values = {k: v for k, (v, _, _) in r.stats.items()}
        dps, defense = _recomputed_dps(ctx, interp, values, _items(r), r.raw["weapon_slots"], r.raw["armor_slots"], r.day, r.day)
        if not (_pairs_same(r.dps, dps) and _pairs_same(r.dps_cache, dps) and _pairs_same(r.defense, defense)
                and _pairs_same(r.defense_cache, defense)):
            bad.append(r.day)
        if r.raw["stored_caches"] is not False:
            bad.append((r.day, "flag"))
        differs += not _pairs_same(r.dps, e.snapshot.dps_data)
    assert bad == [] and differs > 0
    assert "cache_applications" not in run.manifest("replay-recomputed")["override"]["counters"]
    check("sim_override", {
        "recomputed.days_dps_differs_from_stored": exact(differs, source="design 4.6",
                                                         note="R2's recompute error on days 4-30, fixture slots"),
    })


def test_replay_items_recompute_sets_items_once(run, shipped, evolved):
    recs = _days(run, "replay-items-recompute")
    assert run.manifest("replay-items-recompute")["override"]["replay_items"] == "recompute"
    first = ov.expected_day(shipped, "replay", 4, evolved)
    assert [r.raw["items_set"] for r in recs] == [True] + [False] * (len(recs) - 1)
    assert all(_items(r) == first.items for r in recs)  # empty policy: nothing drops
    later = ov.expected_day(shipped, "replay", SHORT, evolved)
    assert later.items != first.items
    assert all(_same([r.stats[st.key][0] for st in ov.expected_day(shipped, "replay", r.day, evolved).snapshot.stats],
                     [st.value for st in ov.expected_day(shipped, "replay", r.day, evolved).snapshot.stats]) for r in recs)


def test_unlock_snapshot_opens_islands_slots_and_boss(run, db, shipped, evolved):
    recs = _days(run, "replay-unlock-snapshot")
    m = run.manifest("replay-unlock-snapshot")
    assert m["override"]["unlock"] == "snapshot" and m["override"]["slots"] == "unchanged"
    defs = sp.load_spawners(db)
    registered = sp.registered(defs)
    ident = {d.spawner_id: spawner_map.runtime_id(spawner_map.runtime_path(d.scene, d.go_path), d.key) for d in registered}
    rewards = {k: loc for k, loc in sc.slot_unlock_locations(defs).items() if k not in sc.P12_WEAPON_SLOT_LOCATIONS}
    bad = []
    for r in recs:
        loc = ov.expected_day(shipped, "replay", r.day, evolved).snapshot.location
        boss = sp.boss_for_location(loc, registered)
        weapons = 1 + sum(1 for k, n in rewards.items() if k.startswith("weapon") and loc >= n)
        armors = sum(1 for k, n in rewards.items() if not k.startswith("weapon") and loc >= n)
        got = (r.raw["location"], r.raw["current_boss"], r.raw["weapon_slots"], r.raw["armor_slots"])
        if got != (loc, None if boss is None else ident[boss.spawner_id], weapons, armors):
            bad.append((r.day, got))
    assert bad == []
    final = recs[-1].raw["location"]
    assert final > 4
    counters = m["override"]["counters"]
    assert counters["unlocks"] == final - 1 and m["engine"]["counters"]["unlocks"] == final - 1
    assert "repeat_unlocks" not in m["engine"]["counters"]
    check("sim_override", {
        "unlock_snapshot.final_location": exact(final, source="design 4.6", note="snapshot Location at day 30"),
        "unlock_snapshot.counters": exact(counters, source="design 4.6"),
    })


def test_stored_caches_hold_through_slow_ticks(run, shipped, evolved):
    """Replay from day 0: day 0's override follows snapshot 0; in the slow phase every Tick's CalculateSimulationData
    is followed by RebuildApplyReplayCaches, so each early (non-forced) snapshot carries the day's stored DpsData."""
    recs = _days(run, "replay-slow")
    m = run.manifest("replay-slow")
    assert [r.day for r in recs] == list(range(0, 7))
    assert m["override"]["counters"]["cache_applications"] > 0
    forced = {(s["Playtime"], s["TotalPlaytime"]) for s in _full(run, "replay-slow")}
    early = [s for s in _early(run, "replay-slow") if (s["Playtime"], s["TotalPlaytime"]) not in forced]
    assert len(early) > 50
    bad = []
    for s in early:
        e = ov.expected_day(shipped, "replay", s["TimeChunkIndex"], evolved)
        if not _pairs_same([(x["WeaponType"], x["Value"]) for x in s["DpsData"]], e.snapshot.dps_data):
            bad.append(s["TimeChunkIndex"])
    assert bad == []


def test_free_job_ignores_the_override(run):
    m = run.manifest("free-ignores-override")
    assert m["override"] == {"schema": ov.SCHEMA, "mode": "free", "active": False}
    assert not (run.job_dir("free-ignores-override") / ov.DAYS_FILE).exists()
    assert "day_overrides" not in m["engine"]["counters"]


def test_options_are_echoed_in_the_manifest(run):
    for job in ("replay-405", "checkpoint-405", "replay-recomputed", "replay-items-recompute", "replay-unlock-snapshot"):
        m = run.manifest(job)
        given = m["job"]["override"]
        o = m["override"]
        assert (o["mode"], o["from_day"], o["econ_caches"], o["replay_items"], o["unlock"]) == \
            (m["mode"], given["from_day"] or 0, given["econ_caches"], given["replay_items"], given["unlock"])
        assert o["file"] == ov.DAYS_FILE and o["error"] is None and o["shipped_time_chunks"] == 46


def test_slots_beyond_the_player_fail_the_job(unity_run, db):
    o = ov.override_options(from_day=4, slots={"3": {"weapon": 9, "armor": 0}})
    result = unity_run(req.make_request([ov.override_job("too-many-slots", "replay", o, probe=NULL, day=6,
                                                         max_wall_seconds=120.0)]))
    if result.exit_code == 2 and (result.report or {}).get("main_checkout_editors"):
        pytest.skip("not run: the user's editor rewrote the real save during a run meant to fail (launcher exit 2)")
    assert result.exit_code == 5, result.describe()
    m = result.manifest("too-many-slots")
    assert "exceed" in m["error"] and m["engine"]["stop_reason"] == "failed"
