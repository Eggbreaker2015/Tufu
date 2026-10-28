"""Parameters, random source and trace in Unity (design 1.4.3, 2.6, 4.7, 4.8, 7.3 C3c).

* Request params take effect (the early-snapshot cadence and the fast offline step of an override run follow a
  reference clock with the overridden values), ``params_hash`` is P10's ``params_hash`` on the C# side too,
  unknown fields fail the job (exit 5), ``params_source = "asset"`` reads the parameter asset through the
  AssetDatabase (the interactive path's loader).
* ``params import`` writes the asset in a fresh editor that never entered Play; every editor that did enter Play
  logs the importer guard's refusal.
* ``daily.csv``, ``events.jsonl``, ``digests.csv`` validate against C1's schema; the digest text carries every
  item of design 2.6, each equal to the independently exported snapshot value.

Every engine job runs with ``engine_probe.null_policy`` so the checks keep their meaning after C5-C7.
"""

from __future__ import annotations

import csv
import hashlib
import io
import json
import sqlite3
import struct

import numpy as np
import pytest

from tufu_balance.calibrate import params as P
from tufu_balance.model import timeaxis as ta
from tufu_balance.model.f32 import f32
from tufu_balance.unity import engine, launcher, paramsio, request as req, schema, simio, trace

pytestmark = pytest.mark.unity

NULL = engine.engine_probe(null_policy=True)
DAYS = 8
OVERRIDES = {"FastAfkStepMinutes": 60.0, "EarlySnapshotIntervals": [2.0, 5.0], "RollMode": 1, "Seed": 7}
IMPORT_OVERRIDES = {"FastAfkStepMinutes": 90.0, "Seed": 11, "RollMode": 1, "EarlySnapshotIntervalStarts": [0.0, 15.0]}
GUARD_LINE = "[TufuSim] params import guard after Play: "


def _defaults():
    return P.resolve(["defaults"])


def _override():
    return P.resolve(["defaults"], OVERRIDES)


def _jobs():
    d, o = _defaults(), _override()
    return [
        engine.engine_job("trace-defaults", day=DAYS, max_wall_seconds=300.0, probe=NULL, params=d.params,
                          outputs={"digest_text": True}),
        engine.engine_job("params-override", day=DAYS, max_wall_seconds=300.0, probe=NULL, params=o.params),
        {**engine.engine_job("params-asset", day=2, max_wall_seconds=300.0, probe=NULL), "params_source": "asset"},
        engine.engine_job("trace-write-points", max_wall_seconds=300.0, probe=engine.engine_probe(write_points=True),
                          params=d.params),
    ]


def _ok(result):
    assert result.exit_code == 0, result.describe()
    return result


@pytest.fixture(scope="module")
def run(unity_run):
    return _ok(unity_run(req.make_request(_jobs())))


@pytest.fixture(scope="module")
def unknown_run(unity_run):
    """A job whose params carry an unknown field: C# rejects it (exit 5); the request's hash itself is right."""
    params = {**_defaults().params, "Bogus": 1}
    return unity_run(req.make_request([engine.engine_job("params-unknown", day=1, max_wall_seconds=120.0, probe=NULL,
                                                         params=params)]))


@pytest.fixture(scope="module")
def imported(unity_rev, unity_opts, tmp_path_factory):
    dest = tmp_path_factory.mktemp("params_import") / "Simulation Rebuild Params.asset"
    result = paramsio.import_params([], IMPORT_OVERRIDES, rev=unity_rev, dest=dest, opts=unity_opts)
    return result


def _json(result, job, name):
    return json.loads(result.file(job, name))


def _bits_f(x: float) -> str:
    return format(struct.unpack("<I", struct.pack("<f", x))[0], "08x")


def _bits_d(x: float) -> str:
    return format(struct.unpack("<Q", struct.pack("<d", x))[0], "016x")


# ---------------------------------------------------------------- params

def test_request_params_hash_equals_p10(run):
    for job, resolved in (("trace-defaults", _defaults()), ("params-override", _override())):
        m = run.manifest(job)
        p = m["params"]
        assert p["schema"] == "tufu.sim.params/1" and p["source"] == "request" and p["interactive"] is False
        assert p["complete"] is True and p["missing"] == [] and p["error"] is None
        assert p["request_hash"] == p["request_hash_given"] == p["hash"] == resolved.params_hash
        assert p["matches_request"] is True
        assert m["job"]["params_hash"] == resolved.params_hash
        assert m["engine"]["params"] == paramsio.effective(resolved.params)


def _reference_clock(cfg, days, starts, intervals, fast_step):
    """tests/unity/test_engine.py's empty-policy reference clock (C3b), with the cadence and step as inputs."""
    F = np.float32
    P_ = T = F(0.0)
    d, last, end = 0, F(0.0), F(cfg.life_cycle_in_minutes[0])
    early, day = [(P_, T, 0)], [(P_, T, 0)]

    def interval(p):
        return F([v for s, v in zip(starts, intervals) if p >= s][-1])

    def day_index(t):
        return int(np.floor(F(t / F(cfg.spawner_charges_chunk_minutes))))

    while d < days:
        if cfg.use_fast_tick and d >= cfg.fast_tick_start_chunk_offset:
            while day_index(T) == d:
                T = F(T + F(fast_step))
        else:
            step = F(cfg.time_step_seconds / F(60.0))
            until = F(F(last + interval(P_)) - P_)
            if F(0.0) < until < step:
                step = until
            P_, T = F(P_ + step), F(T + step)
            if P_ >= F(last + interval(P_)):
                early.append((P_, T, d))
                last = P_
            if P_ >= end:
                T = F(T + F(cfg.life_cycle_in_minutes[1]))
                end = F(P_ + F(cfg.life_cycle_in_minutes[0]))
        c = day_index(T)
        if c != d:
            d = c
            day.append((P_, T, d))
            if ta.writes_early_snapshot(d, cfg):
                early.append((P_, T, d))
                last = P_
    return early, day


def _ptt(rows):
    return [(f32(r["Playtime"]), f32(r["TotalPlaytime"]), r["TimeChunkIndex"]) for r in rows]


def test_request_overrides_take_effect(run, db):
    """The overridden cadence (2 minutes below P = 20) and fast offline step (60 minutes) drive the clock; the
    default run follows the default reference, and the two differ."""
    cfg = ta.load_time_axis_config(db)
    for job, params in (("trace-defaults", _defaults().params), ("params-override", _override().params)):
        early_ref, day_ref = _reference_clock(cfg, DAYS, params["EarlySnapshotIntervalStarts"],
                                              params["EarlySnapshotIntervals"], params["FastAfkStepMinutes"])
        assert _ptt(_json(run, job, "timechunk_full.json")["TimeChunkSnapshots"]) == day_ref, job
        assert _ptt(_json(run, job, "simulation_data.json")["Snapshots"]) == early_ref, job
    early_p = {job: {float(s["Playtime"]) for s in _json(run, job, "simulation_data.json")["Snapshots"]
                     if s["Playtime"] < 20} for job in ("trace-defaults", "params-override")}
    assert early_p["trace-defaults"] == {float(p) for p in range(20)}
    assert early_p["params-override"] == {float(p) for p in range(0, 20, 2)}
    fast = [r for r in trace.read(run.job_dir("params-override")).daily if r["day"] >= cfg.fast_tick_start_chunk_offset]
    assert fast and all(r["offline_steps"] == 1440 // 60 and r["offline_minutes"] == 1440.0 for r in fast)
    t = run.manifest("params-override")["trace"]["random"]
    assert (t["mode"], t["seed"], t["streams"]) == (1, 7, 0)


def test_params_asset_is_read_through_the_asset_database(run):
    m = run.manifest("params-asset")
    p = m["params"]
    assert p["source"] == "asset" and p["asset_found"] is True and p["asset_path"] == paramsio.ASSET_REL
    meta = paramsio.asset_path().with_name(paramsio.asset_path().name + ".meta").read_text()
    assert f"guid: {p['asset_guid']}" in meta
    values = paramsio.read_asset()
    assert p["hash"] == paramsio.effective_hash(values) and m["engine"]["params"] == values
    assert "request_hash" not in p and m["job"]["params_source"] == "asset"


def test_unknown_param_field_fails_the_job(unknown_run):
    m = unknown_run.manifest("params-unknown")
    assert m["exit_code"] == launcher.EXIT_BAD_REQUEST
    assert "Bogus" in m["error"] and m["error"].startswith("params")
    if unknown_run.exit_code != launcher.EXIT_BAD_REQUEST:
        # A failed run while the user's editor rewrites the real save is reported as a guard failure (Runbook 13).
        problems = unknown_run.report.get("guard_problems") or []
        assert unknown_run.exit_code == launcher.EXIT_GUARD and unknown_run.report.get("main_checkout_editors")
        assert problems and all(p.startswith("real ") for p in problems), unknown_run.describe()


def test_editors_that_entered_play_refuse_the_import(run):
    log = (run.run_dir / "unity.log").read_text(errors="replace")
    verdicts = [line.split(GUARD_LINE, 1)[1].strip() for line in log.splitlines() if GUARD_LINE in line]
    assert len(verdicts) == len(run.request["jobs"])
    assert set(verdicts) == {"refused: this editor process has entered Play mode"}


def test_params_import_in_a_fresh_editor(imported):
    assert imported.exit_code == 0, json.dumps(imported.report, indent=1, default=str)[-4000:]
    report = imported.report
    assert all(report["checks"].values()) and report["importer"]["entered_play"] is False
    assert report["importer"]["read_back"]["asset_found"] is True
    resolved = P.resolve(["defaults"], IMPORT_OVERRIDES)
    assert paramsio.read_asset(imported.dest) == paramsio.effective(resolved.params)
    assert imported.dest.read_text(encoding="utf-8") == paramsio.asset_text(paramsio.effective(resolved.params))
    assert report["restored_run_worktree_asset"] and report["guard_problems"] == []
    assert not launcher.runner.porcelain(launcher.runner.run_worktree()).strip()


# ---------------------------------------------------------------- trace

@pytest.fixture(scope="module")
def traced(run):
    return {job["job_id"]: trace.read(run.job_dir(job["job_id"])) for job in run.request["jobs"]}


def test_trace_files_validate_against_the_schema(run, traced):
    for job, t in traced.items():
        assert set(t.manifest["files"]) == {trace.DAILY, trace.EVENTS, trace.DIGESTS} | (
            {trace.DIGEST_TEXT} if job == "trace-defaults" else set())
        header = run.file(job, trace.DAILY).decode().splitlines()[0].split(",")
        assert header == [n for n, _ in schema.CSV_SCHEMAS[trace.DAILY]]
    # Through C1's ingest as well.
    db = simio.ingest(run.job_dir("trace-defaults"))
    con = sqlite3.connect(db)
    assert con.execute("select count(*) from sim_daily").fetchone()[0] == DAYS
    assert con.execute("select count(*) from sim_digests").fetchone()[0] == DAYS + 1
    con.close()


def test_daily_rows_follow_the_snapshots(run, traced, db):
    cfg = ta.load_time_axis_config(db)
    full = _json(run, "trace-defaults", "timechunk_full.json")["TimeChunkSnapshots"]
    rows = traced["trace-defaults"].daily
    days = run.manifest("trace-defaults")["engine"]["days"]
    assert [r["day"] for r in rows] == list(range(DAYS)) and all(r["complete"] == 1 for r in rows)
    assert any(r["eligible_count"] > 0 for r in rows)
    for r, nxt, cur, log in zip(rows, full[1:], full, days):
        assert r["time_chunk_index"] == r["day"] == cur["TimeChunkIndex"]
        assert (r["playtime"], r["total_playtime"]) == (nxt["Playtime"], nxt["TotalPlaytime"])
        assert r["active_minutes"] == float(f32(nxt["Playtime"])) - float(f32(cur["Playtime"]))
        assert r["offline_minutes"] + r["active_minutes"] == pytest.approx(nxt["TotalPlaytime"] - cur["TotalPlaytime"], abs=1e-3)
        assert [log[0], log[2], log[3]] == [r["day"], r["ticks"], r["offline_steps"]] and log[4] == r["offline_minutes"]
        assert r["kills"] == r["clears"] == 0 and r["fight_time_sum"] == r["farmed_time_sum"] == 0.0
        assert r["unlocked_location"] == 1 and r["location"] == 1
        if r["day"] < cfg.fast_tick_start_chunk_offset:
            assert r["offline_steps"] >= 1 and r["offline_minutes"] == r["offline_steps"] * cfg.life_cycle_in_minutes[1]
            assert r["eligible_count"] >= 0 and (r["eligible_count"] == 0) == (r["eligible_signature"] == "")
            assert len(r["eligible_signature"]) in (0, 64)
        else:
            assert (r["ticks"], r["offline_steps"], r["offline_minutes"]) == (1, 12, 1440.0)
            assert (r["eligible_count"], r["eligible_signature"]) == (-1, "")


def test_events_follow_the_write_points(run, traced):
    m = run.manifest("trace-write-points")
    steps = m["engine"]["probe"]["steps"]
    events = traced["trace-write-points"].events
    cleared = []
    previous = {}
    for step in steps:
        if step["counters"].get("clears", 0) > previous.get("clears", 0) and step["spawner"] not in cleared:
            cleared.append(step["spawner"])
        previous = step["counters"]
    assert [e["spawner"] for e in events if e["type"] == "first_kill"] == cleared
    assert [e["location"] for e in events if e["type"] == "unlock"] == [u[0] for u in m["engine"]["unlocks"]]
    opened = {k for k, v in m["engine"]["probe"]["slots"].items() if v}
    slot_events = [e for e in events if e["type"] == "slot_unlock"]
    assert {e["slot"] for e in slot_events} <= opened and len(slot_events) == m["engine"]["counters"].get("slot_unlocks", 0)
    assert all(e["count"] >= 1 for e in slot_events)
    assert {e["type"] for e in events} <= set(schema.EVENT_TYPES)
    assert traced["trace-write-points"].manifest["events"] == {
        t: sum(e["type"] == t for e in events) for t in sorted({e["type"] for e in events})}
    # The probe's day change closes day 0 with the clears made before it.
    day0 = traced["trace-write-points"].daily[0]
    before = next(s for s in steps if s["name"] == "clear_before_day_change")["counters"]
    assert (day0["day"], day0["complete"], day0["kills"], day0["clears"]) == (0, 1, before["kills"], before["clears"])


def test_digest_covers_design_2_6(run, traced):
    """Every item of the 2.6 list in the day's digest text, equal to the exported day snapshot: stat Value and
    TotalValue, items (Key, Level, Copies, Evolved), Location, P and T, the RPM dictionary, charges used (none
    under the empty policy); plus health, K modifiers and the random streams."""
    t = traced["trace-defaults"]
    full = _json(run, "trace-defaults", "timechunk_full.json")["TimeChunkSnapshots"]
    items = list(csv.DictReader(io.StringIO(run.file("trace-defaults", "items_ext.csv").decode())))
    ext = list(csv.DictReader(io.StringIO(run.file("trace-defaults", "snapshots_ext.csv").decode())))
    ready = csv.DictReader(io.StringIO(run.file("trace-defaults", "ready_spawners.csv").decode()))
    outside = {f"{r['path']}|{r['key']}" for r in ready if r["is_event"] == "1" or r["is_vip"] == "1"}
    assert [r["day"] for r in t.digests] == list(range(DAYS + 1)) and set(t.texts) == set(range(DAYS + 1))
    for row, snap in zip(t.digests, full):
        assert (row["playtime"], row["total_playtime"], row["time_chunk_index"]) == (snap["Playtime"], snap["TotalPlaytime"], row["day"])
        f = trace.digest_fields(t.texts[row["day"]][1])
        assert {k.split(".")[0] for k in f} >= {"clock", "modifiers", "stat", "health", "item", "slot", "location",
                                                 "rpm", "random", "policy", "fast"}
        assert f["clock.day"] == str(row["day"])
        assert f["clock.playtime"] == _bits_f(f32(snap["Playtime"])) and f["clock.total_playtime"] == _bits_f(f32(snap["TotalPlaytime"]))
        for st in snap["Stats"]:
            value, _, total, _ = f[f"stat.{st['Key']}"].split(",")
            assert (value, total) == (_bits_d(st["Value"]), _bits_d(st["TotalValue"]))
            assert f[f"rpm.{st['Key']}"] == _bits_d(st["CurrentGpm"])
        day_items = [r for r in items if r["list"] == "TimeChunkSnapshots" and int(r["index"]) == row["day"]]
        digest_items = sorted(v for k, v in f.items() if k.startswith("item."))
        assert len(digest_items) == len(snap["Items"]) == len(day_items)
        assert digest_items == sorted(f"{it['level']},{it['copies']},{it['evolved']}" for it in day_items)
        assert {k.split("#")[0][5:] for k in f if k.startswith("item.")} == {it["Key"] for it in snap["Items"]}
        assert f["location.unlocked"] == str(snap["Location"])
        health = next(r for r in ext if r["list"] == "TimeChunkSnapshots" and int(r["index"]) == row["day"])["current_health"]
        assert f["health"] == _bits_d(float(health))
        k2 = next(st for st in snap["Stats"] if st["Key"] == 1)
        _, b2, b3 = f["modifiers"].split(",")
        assert (b2, b3) == (_bits_f(f32(k2["StatsFromFeaturesFlat"])), _bits_f(f32(k2["PercentMultiplier"])))
        # Empty policy: nothing cleared, so no charges used, and only event/VIP spawners (outside the bot,
        # never zeroed) keep a cooldown.
        assert not [k for k in f if k.startswith("charges.")]
        assert {k[len("cooldown."):] for k in f if k.startswith("cooldown.")} <= outside
        assert f["random.0"] == "mode=0,seed=1"


def test_digests_differ_only_where_the_state_differs(traced):
    """Day 0 of the default and the override run: same world, different random source (RollMode 1, Seed 7)."""
    a = trace.digest_fields(traced["trace-defaults"].texts[0][1])
    b_digest = traced["params-override"].digests[0]["sha256"]
    assert b_digest != traced["trace-defaults"].digests[0]["sha256"]
    b = dict(a, **{"random.0": "mode=1,seed=7"})
    text = "".join(sorted(f"{k}={v}\n" for k, v in b.items()))
    assert hashlib.sha256(text.encode()).hexdigest() == b_digest
