"""Calibrated free-run regression; passing does not imply scientific acceptance."""
import json

import pytest

from tufu_balance.calibrate import params
from tufu_balance.golden import check, check_file, exact
from tufu_balance.unity import request

pytestmark = pytest.mark.unity


@pytest.fixture(scope="module")
def calibrated_run(unity_run):
    p = params.resolve(['defaults', 'calibrated'])
    job = request.make_job('calibrated-405', params=p.params, outputs={'inventory': True, 'bake_report': True})
    run = unity_run(request.make_request([job]))
    assert run.exit_code == 0, run.describe()
    root = run.job_dir('calibrated-405')
    guard = json.loads((root / 'asset_guard.json').read_text())['after_job']
    assert guard['ok'] and not guard['changed_on_disk']
    timing = json.loads((root / 'timings.json').read_text())
    assert timing['job_wall_s'] <= 60
    return root, p


def test_parameter_hash(calibrated_run):
    _, p = calibrated_run
    check('sim_regression', {'params_hash': exact(p.params_hash, source='calibrated free run')})


@pytest.mark.parametrize('name', ('daily.csv', 'digests.csv', 'snapshots_ext.csv', 'items_ext.csv'))
def test_output_file(calibrated_run, name):
    root, _ = calibrated_run
    check_file('sim_run', name, (root / name).read_bytes())


def test_scientific_failures_are_recorded(calibrated_run, db):
    from tufu_balance.calibrate import metrics, targets
    from tufu_balance.calibrate.stage_open import read_series
    from tufu_balance.golden import at_most

    root, _ = calibrated_run
    tg = targets.build(db, slot_schedule=targets.load_slot_schedule())
    evaluation = metrics.evaluate(metrics.compute(read_series(root), tg), metrics.load_tolerances(), 'closed')
    groups = {(g.target, g.group): g for g in evaluation.groups}
    checks = {}
    for target, group in [('T03', 'cadence'), ('T04', 'day_0_3'), ('T04', 'tc_ge_11'),
                          ('T06', 'tc_ge_24'), ('T09', 'nonzero'), ('T12', 'phase_4_19'),
                          ('T14', 'level'), ('T16', 'main')]:
        g = groups[target, group]
        # Counts freeze observations, while the original count is the design's minimum permitted count.
        allowed = {'T06': int(g.n * .05), 'T09': int(g.n * .10), 'T14': int(g.n * .10)}.get(target, 0)
        checks[f'{target}.{group}.excess_failures'] = at_most(max(0, g.n - g.n_ok - allowed), original=0,
            source='5.1 closed-loop cell-count tolerance; Calibration.md K3')
    check('sim_regression', checks)


def test_external_and_slow_failures_are_recorded(calibrated_run, db):
    import math
    from tufu_balance.calibrate import targets
    from tufu_balance.calibrate.stage_full import t19
    from tufu_balance.calibrate.stage_slow import evaluate
    from tufu_balance.golden import at_least, at_most

    root, _ = calibrated_run
    bake = t19(db, root)
    _, slow = evaluate(root, targets.build(db, slot_schedule=targets.load_slot_schedule()))
    checks = {}
    for threshold in ('0.01', '0.05'):
        cell = bake['health'][threshold]
        checks[f'T19.health.{threshold}'] = at_least(cell['free'], original=math.ceil(.95 * cell['A']),
            source='T19 external day-mode Health; Calibration.md K3')
    p = slow['arrivals']['3']['p']
    checks['island3.minutes_outside'] = at_most(max(0, 110.2 - p, p - 115.3), original=0,
        source='K2 island 3 arrival range; Calibration.md K3')
    for tc, (actual, expected) in enumerate(zip(slow['item_counts'], (4, 7, 8, 8)), 1):
        checks[f'items.TC{tc}.error'] = at_most(abs(actual - expected), original=0,
            source='K2 item counts; Calibration.md K3')
    check('sim_regression', checks)
