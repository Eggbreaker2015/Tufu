"""Sandbox, asset guard, request round trip, heartbeat and exit codes in real Unity runs (design 2.3,
2.6, 7.3 C0b)."""

from __future__ import annotations

import csv
import io
import json
import re

import pytest

from tufu_balance.unity import launcher, request as req

pytestmark = pytest.mark.unity

EPOCH = "2024-01-01T00:00:00Z"


def _ok(result):
    assert result.exit_code == 0, result.describe()
    return result


# ---------------------------------------------------------------- sandbox

def test_real_save_prefs_and_project_unchanged(selftest_runs):
    for result in selftest_runs:
        assert result.report["guard_problems"] == [], result.describe()
        assert result.report["restored"] == []
        assert result.report["workers_left"] == []
        assert any("unity.TufuSimSandbox." in r for r in result.report["sandbox_removed"])


def test_save_and_prefs_paths_are_sandboxed(selftest_run):
    m = selftest_run.manifest("selftest")["sandbox"]
    assert m["company_name"] == "TufuSimSandbox"
    assert "/Application Support/TufuSimSandbox/" in m["persistent_data_path"]
    assert m["prefs_domain"].startswith("unity.TufuSimSandbox.")
    log = (selftest_run.run_dir / "unity.log").read_text(errors="replace")
    support = re.findall(r"Application Support/([^/\n]+)/", log)
    assert support and set(support) == {"TufuSimSandbox"}, set(support)
    assert "prefs domain unity.TufuSimSandbox." in log
    assert "Multicast Games" not in "".join(l for l in log.splitlines() if "persistentDataPath" in l)
    editor = json.loads((selftest_run.run_dir / "editor_report.json").read_text())
    assert editor["sandbox"]["prefs_probe_in_sandbox_domain"] and not editor["sandbox"]["prefs_probe_in_real_domain"]
    assert editor["restored"]["company"] == "Multicast Games" and not editor["restored"]["player_settings_dirty"]


def test_sandbox_clock(selftest_run):
    m = selftest_run.manifest("selftest")
    assert m["sandbox"]["clock_epoch_utc"] == EPOCH
    assert m["sandbox"]["time_service_now_utc"] == "2024-01-01T00:00:00.0000000Z"
    assert m["sandbox"]["time_service_cheat_offset_s"] == 0
    assert m["ready"]["clock_unix"] == 1704067200


def test_request_round_trip_through_csharp(unity_run):
    """Python writes every core field (dictionaries, nulls, int/float/string prefs); C# parses it with
    Newtonsoft and echoes the parsed object into the manifest; it equals what Python wrote."""
    result = _ok(unity_run(req.roundtrip_example()))
    manifest = result.manifest("roundtrip")
    sent = json.loads((result.run_dir / "request.json").read_text())
    assert manifest["request"] == sent
    assert manifest["job"] == sent["jobs"][0]
    assert manifest["config_overrides_applied"] == {"FastTickStartChunkOffset": 4, "SpawnerChargesChunkMinutes": 7.5,
                                                    "UseFastTick": True}
    assert manifest["sandbox"]["prefs_seed_seen"] == {"ShowSnapshotSelectorOnce": 1, "TimeSkipSeconds": 259200.0,
                                                      "tufu_probe_text": "sandbox-text"}


def test_time_skip_and_shared_prefs_do_not_change_the_state(unity_run, selftest_run):
    """prefs_seed with TimeSkipSeconds = 259200 and other shared keys: ready table and digests unchanged."""
    request = req.preset("selftest")
    request["sandbox"]["prefs_seed"] = {"TimeSkipSeconds": 259200.0, "ShowSnapshotSelectorOnce": 1, "game_loaded_once": 1}
    result = _ok(unity_run(request))
    m, base = result.manifest("selftest"), selftest_run.manifest("selftest")
    assert m["sandbox"]["prefs_time_skip_seconds"] == 259200.0
    assert m["sandbox"]["time_service_cheat_offset_s"] == 0
    assert result.file("selftest", "ready_spawners.csv") == selftest_run.file("selftest", "ready_spawners.csv")
    for key in ("state_sha256_job_start", "state_sha256_job_end"):
        assert m[key] == base[key], key
    assert m["ready"] == base["ready"] and m["ab"] == base["ab"]


def test_ab_groups_fixed_to_the_request(selftest_run):
    ab = selftest_run.manifest("selftest")["ab"]
    assert ab["requested"] == "A"
    assigned = {k: v for k, v in ab["tests"].items() if v is not None}
    assert assigned and set(assigned.values()) == {"A"}
    assert {k for k, v in ab["variant_lists"].items() if v and len(v) > 1} == \
        {"crystal_pass", "enchant_failure_dust_cashback", "fastRPMParams", "offerwall", "soul_pass"}
    assert ab["effective"]["fastRPMParams"] == {"fightTimeSeconds": 10, "maxHealthLossMultiplier": 1}
    cooldowns = ab["effective"]["spawnerCooldowns"]
    assert cooldowns["nonElite"]["cooldownMinutes"]["defaultValue"] == 30
    assert cooldowns["nonElite"]["cooldownMinutes"]["overridesByLocation"]["1"] == 4
    assert cooldowns["elite"]["cooldownMinutes"]["defaultValue"] == 180
    m = selftest_run.manifest("selftest")
    assert m["ready"]["expected_table"] == {"checked": False}  # P6's table is not there yet: export only


def test_expected_ready_spawner_table_gate(unity_run, selftest_run, tmp_path):
    """The DP6 gate with a table in P6's format (path, key, charges_total, base_cooldown): taken from a
    selftest it passes for all 716 spawners; one changed BaseCooldown fails the ready check (exit 8).
    P6's real table replaces this one when it lands."""
    rows = list(csv.DictReader(io.StringIO(selftest_run.file("selftest", "ready_spawners.csv").decode())))
    good, bad = tmp_path / "expected.csv", tmp_path / "expected_bad.csv"
    for path, tweak in ((good, None), (bad, rows[3]["key"])):
        with open(path, "w", newline="") as f:
            w = csv.writer(f, lineterminator="\n")
            w.writerow(["path", "key", "charges_total", "base_cooldown"])
            for r in rows:
                cooldown = r["base_cooldown"] if r["key"] != tweak else repr(float(r["base_cooldown"]) + 1)
                w.writerow([r["path"], r["key"], r["charges_total"], cooldown])
    request = req.preset("selftest")
    request["sandbox"]["expected_ready_spawners"] = str(good)
    result = _ok(unity_run(request))
    table = result.manifest("selftest")["ready"]["expected_table"]
    assert table["checked"] and table["compared"] == 716 and table["mismatched"] == 0
    request["sandbox"]["expected_ready_spawners"] = str(bad)
    result = unity_run(request)
    assert result.exit_code == 8, result.describe()
    table = result.manifest("selftest")["ready"]["expected_table"]
    assert table["mismatched"] == 1 and rows[3]["key"] in table["details"][0]


def test_ready_spawners_after_remedy(selftest_run):
    ready = selftest_run.manifest("selftest")["ready"]
    assert ready["alive_enabled"] == ready["expected_alive_enabled"] == 699
    assert len(ready["exceptions"]) == 716 - 699
    assert ready["remedy_complete_bootstrap"] == 0 and ready["remedy_enabled"] == 0
    rows = selftest_run.file("selftest", "ready_spawners.csv").decode().splitlines()
    assert len(rows) == 1 + 716


def test_integrations_skipped_and_no_outbound(selftest_runs):
    for result in selftest_runs:
        m = result.manifest("selftest")
        assert m["sandbox"]["integrations_skipped"] is True
        net = result.report["net"]
        assert net["samples"] > 0
        assert net["new_non_loopback_in_play"] == [], net
        assert result.report["log_scan"]["sdk_lines"] == [], result.report["log_scan"]


def test_pets_absent(selftest_run):
    assert selftest_run.manifest("selftest")["sandbox"]["pets"] == "absent"


def test_health_and_rpm_untouched_while_frozen(selftest_run):
    check = selftest_run.manifest("selftest")["selfcheck"]
    assert check == {"quiet_frames": 30, "health_and_rpm_unchanged_quiet": True, "health_and_rpm_unchanged_job": True}


# ---------------------------------------------------------------- asset guard

def test_asset_guard_restores_a_mutated_player_config(unity_run, selftest_run):
    """Job 1 adds a modifier to PlayerConfig.Stats[0].Modifiers in Play. Every Play leaves Player Config
    changed in memory (PlayerModel shares the asset's modifier lists with live stats), so the guard
    reloads it after every job; the mutation shows as a different in-memory digest, and job 2 starts from
    the same state as a fresh selftest."""
    request = req.make_request([req.empty_job("mutate", test={"mutate_player_config": True}), req.empty_job("after")])
    result = _ok(unity_run(request))
    guard = json.loads(result.file("mutate", "asset_guard.json"))["after_job"]
    plain = json.loads(selftest_run.file("selftest", "asset_guard.json"))["after_job"]
    after = json.loads(result.file("after", "asset_guard.json"))["after_job"]
    assert guard["ok"] and "Player Config" in guard["restored_by_reimport"] + guard["restored_by_unload"]
    assert guard["changed_sha256"]["Player Config"] != plain["changed_sha256"]["Player Config"]
    assert after["changed_sha256"] == plain["changed_sha256"]
    for key in ("state_sha256_job_start", "state_sha256_job_end"):
        assert result.manifest("after")[key] == selftest_run.manifest("selftest")[key], key


# ---------------------------------------------------------------- heartbeat and exit codes

def test_long_frame_with_ticks_is_not_a_stall(unity_run):
    """One 12 s frame that keeps calling the tick hook: the heartbeat moves, the 6 s stall timeout does not fire."""
    request = req.make_request([req.empty_job("busy", test={"busy_seconds": 12.0, "busy_ticks": True})],
                               unity={"heartbeat_interval_s": 1.0})
    result = _ok(unity_run(request, opts=launcher.Options(stall_timeout_s=6.0)))
    assert result.manifest("busy")["counts"]["ticks"] > 1000


def test_stalled_frame_times_out_with_exit_3(unity_run):
    request = req.make_request([req.empty_job("stall", test={"busy_seconds": 60.0, "busy_ticks": False})],
                               unity={"heartbeat_interval_s": 1.0})
    result = unity_run(request, opts=launcher.Options(stall_timeout_s=8.0))
    assert result.exit_code == 3, result.describe()
    assert "stalled" in result.report["reason"]
    assert result.report["guard_problems"] == [] and result.report["workers_left"] == []


@pytest.mark.parametrize("name, mutate, code, error", [
    ("guard", lambda r: r["jobs"][0].update(test=dict(r["jobs"][0]["test"] or {}, mutate_player_config=True,
                                                      skip_reimport=True)), 2, None),
    ("not-ready", lambda r: r["unity"].update(ready_timeout_s=1.0), 4, "not ready"),
    ("ready-check", lambda r: r["sandbox"].update(expected_alive_enabled=700), 8, "AnyoneAlive && IsEnabled = 699"),
    ("exception", lambda r: r["jobs"][0].update(test={**req.make_job("x", test={})["test"], "throw_exception": True}), 1,
     "harness test exception"),
])
def test_exit_codes(unity_run, name, mutate, code, error):
    request = req.preset("selftest")
    request["jobs"][0]["test"] = req.make_job("x", test={})["test"]
    mutate(request)
    result = unity_run(request)
    assert result.exit_code == code, result.describe()
    m = result.manifest("selftest")
    if error:
        assert error in (m["error"] or ""), m["error"]
    if code == 2:
        guard = json.loads(result.file("selftest", "asset_guard.json"))["after_job"]
        assert not guard["ok"] and "Player Config" in guard["not_restored"]
    assert result.report["guard_problems"] == []  # the real save, plist and project stay untouched


def test_unknown_field_fails_in_csharp_with_exit_5(unity_run):
    request = req.preset("selftest")
    request["jobs"][0]["bogus_field"] = 1
    result = unity_run(request, validate=False)
    assert result.exit_code == 5, result.describe()
    report = json.loads((result.run_dir / "editor_report.json").read_text())
    assert "bogus_field" in report["request_error"]
