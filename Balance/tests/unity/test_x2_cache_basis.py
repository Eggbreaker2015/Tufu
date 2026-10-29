"""X2fix: full-policy slow caches use their own post-fight state and independently recorded refresh basis."""

import copy
import sqlite3

import pytest

from tufu_balance import crosscheck as X
from tufu_balance.crosscheck import dps
from tufu_balance.unity import request as req, simio

pytestmark = pytest.mark.unity


@pytest.fixture(scope="module")
def full_policy_run(unity_run):
    jobs = [req.make_job(name, day=day, outputs={"digest_text": True, "bake_report": True})
            for name, day in (("smoke-11", 11), ("golden-405", 405))]
    result = unity_run(req.make_request(jobs))
    assert result.exit_code == 0, result.describe()
    return result


@pytest.mark.parametrize("job", ["smoke-11", "golden-405"])
def test_full_policy_x2_cache_basis_and_planted_faults(full_policy_run, db, job):
    job_dir = full_policy_run.run_dir / job
    run = X.load_run(job_dir)
    assert not run.null_policy and run.trace and sum(int(r["clears"]) for r in run.trace.daily) > 0
    rs = X.run_checks(job_dir, db, checks=("X1", "X2", "X3", "X4", "X5", "X6", "X7"))
    assert all(r.status != "fail" for r in rs.values()), {k: r.examples for k, r in rs.items()}
    r = rs["X2"]
    assert r.status == "pass" and r.mismatches == 0
    assert not set(r.vacuous) - {"forecast_order", "k2_lag"}
    forced = dps.forced_keys(run)
    slow = [s for s in run.early if (float(s.snapshot.playtime), float(s.snapshot.total_playtime), s.tc) not in forced]
    assert len(slow) == r.counts["snapshots_slow_tick"] and all(s.slow_tick_basis for s in slow)
    sim = simio.ingest(job_dir)
    with sqlite3.connect(sim) as con:
        assert con.execute("SELECT count(*) FROM sim_slow_tick_basis").fetchone()[0] == sum(len(s.slow_tick_basis) for s in slow)
    env = X.Env.load(db)
    for s in slow:
        assert s.slow_tick_basis == dps.forecast_stats(s.snapshot, env, s.tc)
    # Plant pre-fight stats in the caches while leaving the recorded refresh basis intact.
    bad = copy.deepcopy(run)
    previous = {}
    with X.mono_arithmetic():
        for s in bad.early:
            if s.slow_tick_basis is not None and s.tc in previous:
                calc, defense = dps.recompute(s, env, s.tc, stats=previous[s.tc])
                s.snapshot.dps_data[:], s.snapshot.defense_data[:] = calc, defense
            previous[s.tc] = dps.forecast_stats(s.snapshot, env, s.tc)
        prefight = dps.check_x2(bad, env)
    assert prefight.status == "fail" and prefight.mismatches > 0
    # A wrong stat in the refresh basis must also fail.
    bad = copy.deepcopy(run)
    victim = next(s for s in bad.early if s.slow_tick_basis is not None)
    victim.slow_tick_basis[8] += 1.0
    assert dps.check_x2(bad, env).status == "fail"
    # Shift only within a day so failure cannot be attributed to the day modifier.
    bad = copy.deepcopy(run)
    previous = {}
    shifted = 0
    for s in bad.early:
        if s.slow_tick_basis is not None:
            basis = s.slow_tick_basis
            if s.tc in previous:
                s.slow_tick_basis = previous[s.tc]
                shifted += 1
            previous[s.tc] = basis
    assert shifted > 0
    shifted_result = dps.check_x2(bad, env)
    assert shifted_result.status == "fail" and shifted_result.mismatches > 0
