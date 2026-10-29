"""Oracle cross-checks of simulation runs (design 3.5 X1-X7, 3.4 DP8; unit P11).

One Unity run (``crosscheck.standard_jobs``, null policy): ``smoke-11`` and ``golden-405`` must hold X1-X7, with the
parts that need modules not built yet (bake report C8, fights C7, fast planning C5, inventory C6) reported as vacuous;
``golden-405-flat`` adds flat Cut damage/defence so the K2 lag of the caches is tested; ``dp8-checkpoint`` carries
the 46 shipped day-snapshot states (C4's checkpoint override, ``unlock = snapshot``) with the C# spawnerData export,
``dp8-reference`` is the same job without it. Each job is ingested (C1) before it is checked; every check writes its
per-item CSV under ``<job>/crosscheck/``.
"""

from __future__ import annotations

import json

import pytest

from tufu_balance import crosscheck as X
from tufu_balance.golden import check, exact, same_set
from tufu_balance.model.data import connect, load_simulation_data
from tufu_balance.unity import request as req, simio

pytestmark = pytest.mark.unity

RUN_JOBS = ("smoke-11", "golden-405")
X_CHECKS = ("X1", "X2", "X3", "X4", "X5", "X6", "X7")
# Checks that must compare something on a null-policy run, and the parts that stay vacuous until later units.
NON_VACUOUS = ("X1", "X2", "X3", "X5", "X6", "X7")
VACUOUS_PARTS = {"X2": {"k2_lag", "rewards_in_tick", "forecast_order"}, "X4": {"report"},
                 "X3": {"running_max", "missing_health", "armor_tie_selection"},
                 "X5": {"fast_days_without_planning", "fast_phase", "armor_tie_selection"},
                 "X6": {"values_rewards", "charges", "dafc", "unlocks"},
                 "X7": {"running_max", "missing_health", "armor_tie_selection"}}
# ModifierFirst first differs from RpmFirst in the CurrentGpm on day 25 (C3b): smoke-11 cannot test the order.
VACUOUS_EXTRA = {"smoke-11": {"X7": {"order"}}}


@pytest.fixture(scope="module")
def xrun(unity_run, db):
    result = unity_run(req.make_request(X.standard_jobs(db)))
    assert result.exit_code == 0, result.describe()
    return result


INGEST_REFUSED = {}  # job -> C1's refusal (its text defect, BakeTool.md 6.1): the checks repair and confirm instead


@pytest.fixture(scope="module")
def results(xrun, db):
    out = {}
    for job in ("smoke-11", "golden-405", "golden-405-flat", "dp8-checkpoint", "dp8-reference"):
        job_dir = xrun.run_dir / job
        try:
            sim = simio.ingest(job_dir)
            with connect(sim) as con:  # the import must read back as Simulation Data
                assert len(load_simulation_data(con).time_chunk_snapshots) > 0
        except simio.SimioError as e:
            assert "bits_sha256" in str(e), e
            INGEST_REFUSED[job] = str(e)
        checks = X.CHECKS if job == "dp8-checkpoint" else X_CHECKS
        out[job] = X.run_checks(job_dir, db, checks=checks)
    return out


def _summary(r):
    return {"status": r.status, "items": r.items, "mismatches": r.mismatches}


def test_x1_to_x7_hold_on_smoke_11_and_golden_405(results):
    print("ingest refused:", sorted(INGEST_REFUSED))
    for job in RUN_JOBS:
        assert job not in INGEST_REFUSED
        rs = results[job]
        print(job, {c: (r.status, r.items, r.mismatches, sorted(r.vacuous)) for c, r in rs.items()})
        for c, r in rs.items():
            assert r.status != "fail", (job, c, r.examples)
        for c in NON_VACUOUS:
            assert rs[c].status == "pass" and rs[c].items > 0, (job, c)
        assert rs["X4"].status == "vacuous"
        for c, parts in VACUOUS_PARTS.items():
            assert set(rs[c].vacuous) == parts | VACUOUS_EXTRA.get(job, {}).get(c, set()), (job, c, rs[c].vacuous)
    assert results["golden-405"]["X7"].counts["modifier_first_cells_off"] > 0  # the order is tested through the RPM
    g = results["golden-405"]
    check("crosscheck", {
        **{f"golden405.{c}": exact(_summary(r), source="X" if c != "X4" else "X4", note="status, items, mismatches")
           for c, r in g.items()},
        **{f"smoke11.{c}": exact(_summary(r), source=c) for c, r in results["smoke-11"].items()},
        "golden405.vacuous_parts": exact({c: sorted(r.vacuous) for c, r in g.items() if r.vacuous}, source="design 7.3 P11",
                                         note="parts with nothing to check on a null policy"),
        "golden405.x7_modifier_first_cells_off": exact(g["X7"].counts["modifier_first_cells_off"], source="X7"),
        "golden405.x5_slow_days": exact(g["X5"].counts.get("days_slow", 0), source="X5"),
        "golden405.x4_oracle_spawners": exact(g["X4"].counts["oracle_spawners"], source="X4"),
    })


def test_x2_k2_lag_with_flat_stats(results, xrun):
    """With flat Cut damage/defence the caches tell K2(d-1) from K2(d): X2 is bit-exact under the lag on every day."""
    rs = results["golden-405-flat"]
    for c, r in rs.items():
        assert r.status != "fail", (c, r.examples)
    x2 = rs["X2"]
    assert x2.status == "pass" and "k2_lag" not in x2.vacuous
    assert x2.counts["k2_lag_discriminating"] > 300  # days on which K2(d-1) != K2(d) changes the caches
    # C1 exact decimal formatting must preserve the complete export digest without repairs.
    summary = json.loads((xrun.run_dir / "golden-405-flat" / X.CSV_DIR / "summary.json").read_text())["export_text"]
    assert "golden-405-flat" not in INGEST_REFUSED
    assert summary["exact"] and all(summary["exact"].values())
    check("crosscheck", {
        "flat405.x2": exact(_summary(x2), source="X2"),
        "flat405.x2_k2_lag_discriminating_days": exact(x2.counts["k2_lag_discriminating"], source="X2"),
        "flat405.x7": exact(_summary(rs["X7"]), source="X7"),
        "flat405.text_repairs": exact(summary.get("repairs", 0), source="C1 text defect", better="lower",
                                      note="values C1 wrote as a 15-digit text Mono misreads; confirmed by the bit digest"),
    })


def test_override_jobs_have_no_false_failures(results):
    for job in ("dp8-checkpoint", "dp8-reference"):
        rs = results[job]
        assert all(r.status != "fail" for r in rs.values()), {c: r.examples for c, r in rs.items()}
        for c in ("X3", "X5", "X7"):
            assert rs[c].status == "vacuous" and "override" in rs[c].vacuous
        assert rs["X1"].status == rs["X2"].status == "pass"
        assert "override_slow_tick" in rs["X2"].vacuous
        assert rs["X2"].counts["override_slow_tick_snapshots"] == 64


def test_dp8_spawner_data_and_rpm_first_anchor(results, xrun):
    r = results["dp8-checkpoint"]["DP8"]
    print("DP8", r.status, r.items, r.mismatches, r.counts)
    assert r.status == "pass", r.examples
    assert r.counts["states"] == 46 and r.counts["shipped_states"] == 46
    assert r.counts["g20_cells"] == 1380 and r.counts["g20_matches"] == 1380
    assert r.counts["fields_max_rel_diff"] <= X.REL_TOL
    assert r.counts["fields"] == 9 * r.counts["spawner_rows"]
    assert not r.counts.get("location_differs") and not r.counts.get("current_boss_differs")
    m = xrun.manifest("dp8-checkpoint")["override"]
    assert m["unlock"] == "snapshot" and m["applied"] == 46
    check("crosscheck", {
        "dp8.states": exact(r.counts["states"], source="DP8", original=46),
        "dp8.g20_matches": exact([r.counts["g20_matches"], r.counts["g20_cells"]], source="G20", original=[1380, 1380],
                                 note="C# RpmFirst RPM over the 46 overridden states, G20 cell test"),
        "dp8.rows": exact(_summary(r), source="DP8"),
        "dp8.spawner_rows": exact(r.counts["spawner_rows"], source="DP8", note="(state, spawner) pairs, both K(d) and RpmFirst"),
        "dp8.fields": exact(r.counts["fields"], source="DP8"),
        "dp8.fields_bit_equal": exact(r.counts["fields_bit_equal"], source="DP8", better="higher"),
        "dp8.fields_max_rel_diff": exact(r.counts["fields_max_rel_diff"], source="DP8", better="lower"),
        "dp8.g20_replay_bit_equal": exact(r.counts.get("g20_replay_bit_equal", 0), source="G20", better="higher",
                                          note="C# RPM bit-equal to simcalc.replay_day_gpm"),
        "dp8.order_differs": exact(r.counts.get("order_differs", 0), source="DP8",
                                   note="spawnerData positions differing from the oracle's (scene, file_id) tie order"),
    })


def test_dp8_export_leaves_the_run_unchanged(xrun):
    """The export overrides K and RPM for a moment and restores them: digests, snapshots and the override records
    (without the added key) are byte-identical to the job without it."""
    for name in ("digests.csv", "timechunk_full.json", "simulation_data.json", "daily.csv"):
        assert xrun.file("dp8-checkpoint", name) == xrun.file("dp8-reference", name), name
    with_dp8 = [json.loads(l) for l in xrun.file("dp8-checkpoint", "override_days.jsonl").decode().splitlines()]
    plain = [json.loads(l) for l in xrun.file("dp8-reference", "override_days.jsonl").decode().splitlines()]
    assert all("crosscheck_dp8" in r for r in with_dp8) and not any("crosscheck_dp8" in r for r in plain)
    assert [{k: v for k, v in r.items() if k != "crosscheck_dp8"} for r in with_dp8] == plain


def test_every_check_writes_its_csv(results, xrun):
    for job, rs in results.items():
        out = xrun.run_dir / job / X.CSV_DIR
        assert (out / "summary.json").exists()
        for r in rs.values():
            assert r.csv and all((out / f).exists() for f in r.csv), (job, r.check)
    check("crosscheck", {"csv_files": same_set(sorted({f for rs in results.values() for r in rs.values() for f in r.csv}),
                                               source="design 7.3 P11")})
