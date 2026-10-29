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
