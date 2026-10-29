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
from tufu_balance.model.enums import ItemSlotType, StatType
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


@pytest.fixture(scope="module")
def spawners(db):
    """P6's registered spawners and their runtime ids (``currentBoss`` in the records)."""
    registered = sp.registered(sp.load_spawners(db))
    ident = {d.spawner_id: spawner_map.runtime_id(spawner_map.runtime_path(d.scene, d.go_path), d.key) for d in registered}
    return registered, ident


GRANT = {int(StatType.Health): 500.0, int(StatType.StabDamageFlat): 3.0}
# day 4 takes the items of TC 3, day 5 of TC 4, day 6 of TC 5; day 7 (TC 6) has no entry and keeps its slots
SLOTS_DOWN = {"3": {"weapon": 5, "armor": 2}, "4": {"weapon": 2, "armor": 1}, "5": {"weapon": 1, "armor": 0}}


def _jobs(db):
    full = ov.default_override(db, from_day=4)
    grant = engine.engine_probe(null_policy=True, grant_flat=GRANT)
    return [
        ov.override_job("replay-405", "replay", full, probe=NULL, day=LAST, max_wall_seconds=300.0),
        ov.override_job("replay-405-compose", "replay", {**full, "total_value": "compose"}, probe=NULL, day=LAST,
                        max_wall_seconds=300.0),
        ov.override_job("checkpoint-405", "checkpoint", ov.default_override(db), probe=NULL, day=LAST, max_wall_seconds=300.0),
        ov.override_job("replay-recomputed", "replay", {**full, "econ_caches": "recomputed"}, probe=NULL, day=SHORT,
                        max_wall_seconds=300.0),
        ov.override_job("replay-items-recompute", "replay", {**full, "replay_items": "recompute"}, probe=NULL, day=SHORT,
                        max_wall_seconds=300.0),
        ov.override_job("replay-unlock-snapshot", "replay", {**full, "unlock": "snapshot", "slots": None}, probe=NULL,
                        day=SHORT, max_wall_seconds=300.0),
        ov.override_job("replay-slow", "replay", {**full, "from_day": None}, probe=NULL, day=6, max_wall_seconds=300.0),
        ov.override_job("replay-grant", "replay", {**full, "from_day": None}, probe=grant, day=2, max_wall_seconds=300.0),
        ov.override_job("checkpoint-from-20", "checkpoint", ov.default_override(db, from_day=20), probe=grant, day=SHORT,
                        max_wall_seconds=300.0),
        ov.override_job("replay-slots-down", "replay", {**full, "slots": SLOTS_DOWN}, probe=NULL, day=7,
                        max_wall_seconds=300.0),
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


def _fast_value(key: int, value: float, k2: float, k3: float) -> float:
    """Stat.FastValue with only the K2/K3 modifiers on it, in StatBase.CalculateValue's order:
    ((1 * K3) * (1 + K2)) * (BaseValue + 0)."""
    multiplicative = 1.0 * k3 if key in ss.DIRECT_INTERPOLATION_STATS else 1.0
    additive = 1.0 + k2 if key in ss.MAIN_INTERPOLATION_STATS else 1.0
    return 1.0 * multiplicative * additive * (value + 0.0)


class _Totals:
    """Stat.FastValue after the override against the snapshot's own TotalValue (ulps; relative error on
    interpolated days) and against Value composed at the day's K, (Value*(1+K2))*K3 (ulps)."""

    def __init__(self, interp, shipped_days):
        self.interp, self.shipped_days = interp, shipped_days
        self.cells = 0
        self.compose_ulps = 0.0
        self.compose_off = 0
        self.snapshot_ulps = 0.0
        self.snapshot_off = 0
        self.shipped_ulps = 0.0
        self.interpolated_rel = 0.0
        self.k_mismatch_days = []

    def add(self, rec, snapshot):
        k2, k3 = ss.composition_coefficients(self.interp, rec.day)
        if _bits(k2) != _bits(rec.k2) or _bits(k3) != _bits(rec.k3):
            self.k_mismatch_days.append(rec.day)
        for key, (value, fast, _) in rec.stats.items():
            self.cells += 1
            u = _ulps(fast, ss.compose_stat(key, value, k2, k3).total_value)
            self.compose_off += u != 0.0
            self.compose_ulps = max(self.compose_ulps, u)
            stored = snapshot.stat(key).total_value
            u = _ulps(fast, stored)
            self.snapshot_off += u != 0.0
            self.snapshot_ulps = max(self.snapshot_ulps, u)
            if rec.day in self.shipped_days:
                self.shipped_ulps = max(self.shipped_ulps, u)
            elif stored != 0.0:
                self.interpolated_rel = max(self.interpolated_rel, abs(fast - stored) / abs(stored))

    def __str__(self):
        return (f"vs snapshot TotalValue max {self.snapshot_ulps} ulp ({self.snapshot_off}/{self.cells} cells off; "
                f"shipped days max {self.shipped_ulps} ulp; interpolated days max rel {self.interpolated_rel:.3g}); "
                f"vs (Value*(1+K2))*K3 at K(d) max {self.compose_ulps} ulp ({self.compose_off} cells off)")


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


def _forecast(rec):
    """GetForecastStatValue per stat as the engine had it after the override (checked against the oracle by
    :class:`_Totals`)."""
    return {k: g for k, (_, _, g) in rec.stats.items()}


def _recomputed_dps(ctx, stats, items, weapons, armors, timing):
    """CalculateDpsData/CalculateDefenseData from stats, items and slots at ``timing``."""
    entries = [(ctx.items[k], ItemState.from_snapshot(lv, c, ev)) for k, lv, c, ev in items]
    return (sc.calculate_dps_data(ctx, stats, entries, weapons, timing),
            sc.calculate_defense_data(ctx, stats, entries, armors, timing))


def _rpm_check(ctx, recs):
    """RPMCalculatedValue after each override against ``model.simcalc`` on the recorded state (forecast stats, the
    DpsData/DefenseData caches, location, day). RPM keeps its maximum, so a record is never below the fresh value
    and is non-decreasing; it equals the fresh value bit for bit when the day's value is the largest so far.
    Returns (violations, cells equal, cells)."""
    bad, equal, cells = [], 0, 0
    prev = None
    for r in recs:
        state = sc.PlayerState(stats=_forecast(r), dps_data=dict(r.dps_cache), defense_data=dict(r.defense_cache),
                               unlocked_location=r.raw["location"], day=r.day,
                               item_keys=frozenset(k for k, *_ in r.items))
        fresh, _, _ = sc.calculate_rpm_value_time_chunk(ctx, state)
        for k, v in fresh.items():
            got = r.rpm.get(k)
            if got is None:
                if v != 0.0:
                    bad.append((r.day, k, "missing"))
                continue
            cells += 1
            if got < v:
                bad.append((r.day, k, "below"))
            elif _bits(got) == _bits(v):
                equal += 1
        if prev is not None and any(r.rpm[k] < v for k, v in prev.rpm.items() if k in r.rpm):
            bad.append((r.day, "decreasing"))
        prev = r
    return bad, equal, cells


def _values(snapshot_json):
    return {st["Key"]: st["Value"] for st in snapshot_json["Stats"]}


# ---------------------------------------------------------------- replay + stored, days 4-405

def test_replay_once_per_day_with_the_interpolated_state(run, shipped, interp, ctx, evolved, slots, spawners):
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
    assert (o["econ_caches"], o["replay_items"], o["unlock"], o["total_value"], o["items_evolved"], o["slots"]) == \
        ("stored", "snapshot", "derived", "snapshot", "request", "request")
    registered, ident = spawners
    boss = ident[sp.boss_for_location(1, registered).spawner_id]
    shipped_days = {s.time_chunk_index for s in shipped.time_chunk_snapshots}
    totals = _Totals(interp, shipped_days)
    bad = {}
    for r in recs:
        e = ov.expected_day(shipped, "replay", r.day, evolved)
        diff = _check_state(r, e, slots)
        if r.raw["location"] != 1 or r.raw["snapshot_time_chunk"] != e.snapshot.time_chunk_index \
                or r.raw["items_time_chunk"] != e.items_time_chunk:
            diff.append("bracket")
        if r.raw["current_boss"] != boss:
            diff.append("current_boss")
        if diff:
            bad[r.day] = diff
        totals.add(r, e.snapshot)
    print("TotalValue (total_value = snapshot):", totals)
    rpm_bad, rpm_equal, rpm_cells = _rpm_check(ctx, recs)
    print(f"RPM: {rpm_equal}/{rpm_cells} cells equal to the fresh simcalc value, {len(rpm_bad)} violations")
    assert bad == {}
    assert totals.k_mismatch_days == []
    assert totals.snapshot_ulps <= 1.0 and totals.shipped_ulps == 0.0
    assert "total_value_uncorrected" not in o["counters"]
    assert rpm_bad == [] and rpm_equal > 0
    check("sim_override", {
        "replay405.days": exact(len(recs), source="design 7.3 C4", original=402, note="one override per day 4..405"),
        "replay405.state_mismatch_days": exact(len(bad), source="design 7.3 C4", original=0,
                                               note="Values, DpsData/DefenseData and their caches, items, slots, "
                                                    "currentBoss bit for bit"),
        "replay405.total_value_snapshot_max_ulp": exact(totals.snapshot_ulps, source="design 7.3 C4",
                                                        note="total_value = snapshot: Stat.FastValue vs the "
                                                             "interpolated snapshot's TotalValue"),
        "replay405.total_value_snapshot_cells_off": exact(totals.snapshot_off, source="design 7.3 C4"),
        "replay405.rpm_cells_equal_fresh": exact(rpm_equal, source="design 4.6",
                                                 note="RPMCalculatedValue after the override == model.simcalc on "
                                                      "the recorded state (others are kept maxima)"),
        "replay405.counters": exact(o["counters"], source="design 7.3 C4"),
    })


def test_replay_compose_total_value(run, shipped, interp, evolved, slots):
    """total_value = compose: no correction, Stat.FastValue is Value composed with K2/K3(d); on interpolated days it
    differs from the interpolated TotalValue (linear in the day, K2/K3 are not)."""
    recs = _days(run, "replay-405-compose")
    o = run.manifest("replay-405-compose")["override"]
    assert o["total_value"] == "compose" and [r.day for r in recs] == list(range(4, LAST + 1))
    assert not any(k.startswith("total_value_") for k in o["counters"])
    shipped_days = {s.time_chunk_index for s in shipped.time_chunk_snapshots}
    totals = _Totals(interp, shipped_days)
    bad = []
    for r in recs:
        e = ov.expected_day(shipped, "replay", r.day, evolved)
        if _check_state(r, e, slots):
            bad.append(r.day)
        totals.add(r, e.snapshot)
        if not _same([fast for _, fast, _ in r.stats.values()],
                     [_fast_value(k, v, r.k2, r.k3) for k, (v, _, _) in r.stats.items()]):
            bad.append((r.day, "game order"))
    print("TotalValue (total_value = compose):", totals)
    assert bad == [] and totals.k_mismatch_days == []
    assert totals.compose_ulps <= 1.0 and totals.shipped_ulps == 0.0
    check("sim_override", {
        "replay405.total_value_compose_max_ulp": exact(totals.compose_ulps, source="design 7.3 C4",
                                                       note="replay-405-compose: Stat.FastValue vs (Value*(1+K2))*K3 "
                                                            "at K(d), double_all K"),
        "replay405.total_value_compose_cells_off": exact(totals.compose_off, source="design 7.3 C4",
                                                         note="replay-405-compose"),
        "replay405.total_value_shipped_max_ulp": exact(totals.shipped_ulps, source="design 7.3 C4",
                                                       note="replay-405-compose, shipped day-snapshot days"),
        "replay405.total_value_interpolated_max_rel": exact(totals.interpolated_rel, source="design 4.6",
                                                            note="replay-405-compose: TotalValue is interpolated "
                                                                 "linearly, K2/K3 are not"),
    })


def test_replay_state_carries_to_the_next_forced_snapshot(run, shipped, ctx, evolved):
    """Empty policy: day d+1's forced snapshot (taken before its override) shows day d's override: the Values and
    items (effective level) of the record. Its DpsData/DefenseData are what the day change recomputes from the
    record's forecast stats (K2/K3(d) and the correction, RpmFirst), items and slots at timing d+1 -- not the
    stored ones: nothing applies the stored caches at the day change."""
    recs = {r.day: r for r in _days(run, "replay-405")}
    full = {s["TimeChunkIndex"]: s for s in _full(run, "replay-405")}
    bad, differs = [], 0
    for d in range(4, LAST):
        s, r = full[d + 1], recs[d]
        if not _same([st["Value"] for st in s["Stats"]], [r.stats[st["Key"]][0] for st in s["Stats"]]) \
                or _snapshot_items(s) != [(k, lv, c) for k, lv, c, _ in _items(r)]:
            bad.append((d, "state"))
        dps, defense = _recomputed_dps(ctx, _forecast(r), _items(r), r.raw["weapon_slots"], r.raw["armor_slots"], d + 1)
        if not _pairs_same([(x["WeaponType"], x["Value"]) for x in s["DpsData"]], dps) \
                or not _pairs_same([(x["WeaponType"], x["Value"]) for x in s["DefenseData"]], defense):
            bad.append((d, "dps"))
        differs += not _pairs_same(dps, ov.expected_day(shipped, "replay", d, evolved).snapshot.dps_data)
    assert bad == []
    check("sim_override", {
        "replay405.forced_dps_differs_from_stored": exact(differs, source="design 4.6",
                                                          note="days d whose day d+1 forced-snapshot DpsData (day "
                                                               "change recompute) differs from day d's stored one"),
    })


def test_replay_405_runs_in_under_two_minutes(run):
    t = json.loads(run.file("replay-405", "timings.json"))
    print("replay-405 job wall", t["job_wall_s"], "s; fast ticks", t["fast_tick_ms"])
    assert t["job_wall_s"] < 120.0


# ---------------------------------------------------------------- reward modifiers

def test_override_removes_reward_modifiers(run, shipped, interp, evolved):
    """C3b's grant_flat adds Reward flat modifiers before day 0's forced snapshot; day 0's override removes them
    (base value = snapshot Value), so the Values equal snapshot 0's and day 1's forced snapshot no longer has them."""
    recs = _days(run, "replay-grant")
    o = run.manifest("replay-grant")["override"]
    assert [r.day for r in recs] == [0, 1, 2]
    assert [r.raw["removed_modifiers"] for r in recs] == [len(GRANT), 0, 0]
    assert not any(k.startswith("removed_modifiers_source_") for k in o["counters"])  # Reward only
    totals = _Totals(interp, {s.time_chunk_index for s in shipped.time_chunk_snapshots})
    for r in recs:
        assert _check_state(r, ov.expected_day(shipped, "replay", r.day, evolved), None) == []
        totals.add(r, ov.expected_day(shipped, "replay", r.day, evolved).snapshot)
    assert totals.snapshot_ulps <= 1.0
    granted = {s["TimeChunkIndex"]: _values(s) for s in _full(run, "replay-grant")}
    plain = {s["TimeChunkIndex"]: _values(s) for s in _full(run, "replay-slow")}
    for key, amount in GRANT.items():
        assert granted[0][key] == plain[0][key] + amount and plain[0][key] + amount != recs[0].stats[key][0]
    assert all(granted[0][k] == plain[0][k] for k in plain[0] if k not in GRANT)
    assert _same([granted[1][k] for k in sorted(granted[1])], [recs[0].stats[k][0] for k in sorted(granted[1])])


def test_checkpoint_from_day_after_a_closed_loop_start(run, shipped, evolved, slots):
    """checkpoint from_day 20: days 0-20 run closed loop with the grant in the Values; the first shipped day snapshot
    at or after 20 (21) resets them, removes the grant and sets the items."""
    recs = _days(run, "checkpoint-from-20")
    o = run.manifest("checkpoint-from-20")["override"]
    tcs = [s.time_chunk_index for s in shipped.time_chunk_snapshots if 20 <= s.time_chunk_index <= SHORT]
    assert [r.day for r in recs] == tcs and tcs[0] == 21
    assert (o["from_day"], o["first_day"], o["calls"]) == (20, 21, SHORT + 1)
    assert [r.raw["removed_modifiers"] for r in recs] == [len(GRANT)] + [0] * (len(recs) - 1)
    bad = {}
    for r in recs:
        diff = _check_state(r, ov.expected_day(shipped, "checkpoint", r.day, evolved), slots)
        if diff:
            bad[r.day] = diff
    assert bad == {}
    full = {s["TimeChunkIndex"]: _values(s) for s in _full(run, "checkpoint-from-20")}
    plain = {s["TimeChunkIndex"]: _values(s) for s in _full(run, "replay-slow")}
    for d in range(0, 22):
        assert full[d] == full[0], d  # nothing overrides or rewards before day 21
    assert all(full[0][k] == plain[0][k] + GRANT.get(k, 0.0) for k in plain[0])
    shipped21 = next(s for s in shipped.time_chunk_snapshots if s.time_chunk_index == 21)
    assert _same([full[22][st.key] for st in shipped21.stats], [st.value for st in shipped21.stats])
    check("sim_override", {
        "checkpoint_from20.counters": exact(o["counters"], source="design 7.3 C4"),
    })


# ---------------------------------------------------------------- checkpoint

def test_checkpoint_resets_at_shipped_day_snapshots(run, shipped, interp, ctx, evolved, slots, spawners):
    recs = _days(run, "checkpoint-405")
    m = run.manifest("checkpoint-405")
    tcs = [s.time_chunk_index for s in shipped.time_chunk_snapshots]
    assert [r.day for r in recs] == tcs
    assert m["override"]["applied"] == len(tcs) and m["override"]["calls"] == LAST + 1
    assert not any(k.startswith("total_value_") for k in m["override"]["counters"])  # shipped TotalValue composes exactly
    registered, ident = spawners
    boss = ident[sp.boss_for_location(1, registered).spawner_id]
    totals = _Totals(interp, set(tcs))
    bad = {}
    for r in recs:
        e = ov.expected_day(shipped, "checkpoint", r.day, evolved)
        diff = _check_state(r, e, slots)
        if r.raw["items_time_chunk"] != r.day or r.raw["left"] != -1 or r.raw["current_boss"] != boss:
            diff.append("source")
        if diff:
            bad[r.day] = diff
        totals.add(r, e.snapshot)
    rpm_bad, rpm_equal, rpm_cells = _rpm_check(ctx, recs)
    print(f"checkpoint RPM: {rpm_equal}/{rpm_cells} cells equal to the fresh simcalc value")
    assert bad == {}
    assert totals.snapshot_ulps == 0.0 and totals.compose_ulps <= 1.0
    assert rpm_bad == [] and rpm_equal > 0
    check("sim_override", {
        "checkpoint405.days": exact(len(recs), source="design 7.3 C4", original=46),
        "checkpoint405.state_mismatch_days": exact(len(bad), source="design 7.3 C4", original=0),
        "checkpoint405.total_value_shipped_max_ulp": exact(totals.shipped_ulps, source="design 7.3 C4"),
        "checkpoint405.rpm_cells_equal_fresh": exact(rpm_equal, source="design 4.6"),
    })


def test_checkpoint_continues_closed_loop(run, shipped, ctx, interp, evolved, slots):
    """Between checkpoints the engine runs on, not overridden: every forced snapshot up to the next checkpoint keeps
    the reset Values and items, and its DpsData/DefenseData are recomputed by the recovered CalculateDpsData at the
    day change (RpmFirst: K2/K3(e-1) on the stats, timing e), no longer the stored ones."""
    full = {s["TimeChunkIndex"]: s for s in _full(run, "checkpoint-405")}
    snaps = shipped.time_chunk_snapshots
    bad, recomputed_differs, checked = [], 0, 0
    for s, nxt_tc in zip(snaps, [x.time_chunk_index for x in snaps[1:]]):
        a = s.time_chunk_index
        values = {st.key: st.value for st in s.stats}
        items = ov.expected_day(shipped, "checkpoint", a, evolved).items
        w = slots[str(a)]
        for e in range(a + 1, nxt_tc + 1):
            f = full[e]
            if not _same([st["Value"] for st in f["Stats"]], [values[st["Key"]] for st in f["Stats"]]) \
                    or _snapshot_items(f) != [(k, lv, c) for k, lv, c, _ in items]:
                bad.append((e, "state"))
            k2, k3 = ss.composition_coefficients(interp, e - 1)
            stats = {k: _fast_value(k, v, k2, k3) for k, v in values.items()}
            dps, defense = _recomputed_dps(ctx, stats, items, w["weapon"], w["armor"], e)
            if not _pairs_same([(x["WeaponType"], x["Value"]) for x in f["DpsData"]], dps) \
                    or not _pairs_same([(x["WeaponType"], x["Value"]) for x in f["DefenseData"]], defense):
                bad.append((e, "dps"))
            if e == a + 1:
                recomputed_differs += not _pairs_same(dps, s.dps_data)
            checked += 1
    assert bad == [] and checked == LAST
    assert recomputed_differs > 0
    check("sim_override", {
        "checkpoint405.recomputed_dps_differs_from_stored": exact(recomputed_differs, source="design 4.6",
                                                                   note="shipped days whose closed-loop DpsData (next day) "
                                                                        "differs from the stored one"),
    })


# ---------------------------------------------------------------- options

def test_econ_caches_recomputed(run, shipped, ctx, evolved):
    recs = _days(run, "replay-recomputed")
    assert run.manifest("replay-recomputed")["override"]["econ_caches"] == "recomputed"
    assert [r.day for r in recs] == list(range(4, SHORT + 1))
    bad, differs = [], 0
    for r in recs:
        e = ov.expected_day(shipped, "replay", r.day, evolved)
        dps, defense = _recomputed_dps(ctx, _forecast(r), _items(r), r.raw["weapon_slots"], r.raw["armor_slots"], r.day)
        if not (_pairs_same(r.dps, dps) and _pairs_same(r.dps_cache, dps) and _pairs_same(r.defense, defense)
                and _pairs_same(r.defense_cache, defense)):
            bad.append(r.day)
        if r.raw["stored_caches"] is not False:
            bad.append((r.day, "flag"))
        differs += not _pairs_same(r.dps, e.snapshot.dps_data)
    rpm_bad, rpm_equal, _ = _rpm_check(ctx, recs)
    assert bad == [] and differs > 0
    assert rpm_bad == [] and rpm_equal > 0
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


def test_unlock_snapshot_opens_islands_slots_and_boss(run, db, shipped, evolved, spawners):
    recs = _days(run, "replay-unlock-snapshot")
    m = run.manifest("replay-unlock-snapshot")
    assert m["override"]["unlock"] == "snapshot" and m["override"]["slots"] == "unchanged"
    registered, ident = spawners
    rewards = {k: loc for k, loc in sc.slot_unlock_locations(sp.load_spawners(db)).items()
               if k not in sc.P12_WEAPON_SLOT_LOCATIONS}
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
    # forward gates only: a gate into n on an island below n (P6); the back-gates of islands 11-13 stay closed
    forward = sum(1 for d in registered for g in d.gate_next_locations if 2 <= g <= final and d.location < g)
    back = sum(1 for d in registered for g in d.gate_next_locations if 2 <= g <= final and d.location >= g)
    assert back > 0 and counters["gates_opened"] == forward
    assert counters["slot_rewards_opened"] == sum(1 for n in rewards.values() if n <= final)
    check("sim_override", {
        "unlock_snapshot.final_location": exact(final, source="design 4.6", note="snapshot Location at day 30"),
        "unlock_snapshot.counters": exact(counters, source="design 4.6"),
    })


def test_slots_close_and_unequip(run):
    """slots going down: closed slots lose their items (UnequipItem), slots open non-VIP first in config order, and
    a day without a slots entry keeps the previous counts."""
    recs = _days(run, "replay-slots-down")
    o = run.manifest("replay-slots-down")["override"]
    want = {4: (5, 2), 5: (2, 1), 6: (1, 0), 7: (1, 0)}
    assert [r.day for r in recs] == list(want)
    bad = []
    for r in recs:
        states = r.raw["slot_states"]  # [Key, SlotType, IsVip, IsUnlocked, equipped item Key]
        groups = ([x for x in states if x[1] == ItemSlotType.Weapon],
                  [x for x in states if x[1] in (ItemSlotType.Helmet, ItemSlotType.Shield)])
        assert (len(groups[0]), len(groups[1])) == (5, 2)
        for group, n in zip(groups, want[r.day]):
            ordered = sorted(group, key=lambda x: x[2])  # stable: non-VIP first, then config order
            if [x[3] for x in ordered] != [True] * n + [False] * (len(group) - n):
                bad.append((r.day, "order"))
        if any(x[4] is not None for x in states if not x[3]):
            bad.append((r.day, "item in a closed slot"))
        if (r.raw["weapon_slots"], r.raw["armor_slots"]) != want[r.day]:
            bad.append((r.day, "counts"))
    assert bad == []
    # slots change before items, so an override unequips exactly the slots that held an item and are now closed
    unequipped = sum(1 for prev, r in zip(recs, recs[1:]) for x, y in zip(prev.raw["slot_states"], r.raw["slot_states"])
                     if x[4] is not None and not y[3])
    c = o["counters"]
    assert unequipped > 0 and c["slot_unequips"] == unequipped and c["slots_missing"] == 1
    assert c["slots_closed"] == sum(1 for prev, r in zip(recs, recs[1:])
                                    for x, y in zip(prev.raw["slot_states"], r.raw["slot_states"]) if x[3] and not y[3])
    check("sim_override", {"slots_down.counters": exact(c, source="design 4.6")})


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
    for job in ("replay-405", "replay-405-compose", "checkpoint-405", "replay-recomputed", "replay-items-recompute",
                "replay-unlock-snapshot", "replay-grant", "checkpoint-from-20", "replay-slots-down"):
        m = run.manifest(job)
        given = m["job"]["override"]
        o = m["override"]
        assert (o["mode"], o["from_day"], o["econ_caches"], o["replay_items"], o["unlock"], o["total_value"]) == \
            (m["mode"], given["from_day"] or 0, given["econ_caches"], given["replay_items"], given["unlock"],
             given["total_value"])
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
