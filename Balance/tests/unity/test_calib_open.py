"""Stage-1 empirical regression. Design failures are explicit report outputs.

A passing regression suite says the experiment and its audit reproduce; it does
not turn a failed scientific acceptance criterion into a pass.
"""
from pathlib import Path
import json

import pytest

from tufu_balance.calibrate import stage_open as S, params as P, report
from tufu_balance.golden import check, exact, at_most
from tufu_balance.unity import runner

pytestmark = pytest.mark.unity
SUITE = 'calib_open'


@pytest.fixture(scope='module')
def calibration(unity_rev):
    assert S.main(['--rev', unity_rev]) == 0
    latest = json.loads((S.BALANCE / 'out/calibration/open-work/latest.json').read_text())
    directory = Path(latest['report'])
    return directory, json.loads((directory / 'evidence.json').read_text())


def test_all_hypotheses_and_guards_are_real(calibration, unity_rev):
    directory, evidence = calibration
    assert len(evidence['jobs']) == 33
    for job, row in evidence['jobs'].items():
        assert row['code_rev'] == unity_rev, job
        source = Path(row['directory'])
        guard = json.loads((source / 'asset_guard.json').read_text())['after_job']
        assert guard['ok'] and not guard['changed_on_disk'], job
        assert 'Combat Balance Config' not in guard['changed_in_memory'], job
        assert row['asset_guard_sha256'] == report.sha256_file(source / 'asset_guard.json')
    manifest = json.loads((directory / 'manifest.json').read_text())
    for name, digest in {**manifest['files'], **manifest['evidence_files']}.items():
        assert report.sha256_file(directory / name) == digest, name


def test_choices_match_the_params_layer_and_splits(calibration):
    directory, evidence = calibration
    changes = json.loads((directory / 'open.json').read_text())
    assert P.resolve(['defaults'], changes).params == P.resolve(['defaults', 'open']).params
    assert changes['KillState'] == 'StartOfDay'
    assert changes['ActiveMinutesModel'] == evidence['active']['aic'][0]['name']
    # The complete runner-up table must survive the archive, with the pre-registered segments.
    import csv
    with (directory / 'hypotheses.csv').open() as f:
        hypotheses = list(csv.DictReader(f))
    for family in ('KillState', 'ActiveMinutesModel', 'EmissionModel', 'PreferredMapping', 'EvolutionTrigger', 'P12', 'P18'):
        rows = [r for r in hypotheses if r['family'] == family]
        assert len(rows) >= 2 and rows[0]['rank'] == '1' and rows[1]['rank'] == '2'
        assert rows[0]['distinguishing_segment']
    check(SUITE, {'choices': exact(changes, source='K1: fixed grid and trace fit'),
                  'holdout': exact(evidence['holdout'], source='5.5; failed ratios remain failed'),
                  'active.aic': exact(evidence['active']['aic'], source='P03 holdout AIC (selection, not external validation)')})


def test_t04_csharp_confirmation_and_t05_intervals(calibration):
    _, e = calibration
    t04, t05 = e['t04_confirmed'], e['t05']
    assert t05['daily_exact'] == 7 and t05['exact'] == 37
    assert len(t05['unexplained']) == e['t05_unexplained_cap']
    assert t05['threshold_equal_rows'] == 0
    check(SUITE, {'t04.median': at_most(t04['median'], source='T04 original open tolerance', original=.02),
                  't04.max': at_most(t04['max'], source='T04 original open tolerance', original=.08),
                  't05.exact': exact(t05['exact'], source='T05 report; all 41 post-hoc intervals', original=37),
                  't05.unexplained': at_most(len(t05['unexplained']), source='T05 first measured cap'),
                  't05.explanations': exact(t05['explanations'], source='T05 first-kill day +/-1'),
                  'sensitivity': exact(e['sensitivity'], source='P01/DPS diagnostic; no tuning')})


def test_t09_distinguishes_export_from_g20(calibration):
    _, e = calibration
    oracle = e['rpm_oracle']['RpmFirst']
    assert oracle['cells'] == oracle['hits'] == 1380 and oracle['misses'] == []
    exported = e['t09_exported']
    assert exported['cells'] == 1380
    if exported['hits'] < 1380:
        assert e['raw_export_status']['T09'] == 'fail'
        assert any(row.startswith('T09.') for row in e['design_failures'])
    check(SUITE, {'t09.g20_hits': exact(oracle['hits'], source='G20 replay on shipped states', original=1380),
                  't09.exported': exact(exported, source='raw exported path; explicitly not G20 acceptance'),
                  'p07': exact(e['p07'], source='P07 both real C# ablations')})


def test_p18_q25_and_every_failed_criterion_remain_visible(calibration):
    _, e = calibration
    assert len(e['p18']) == 4 and e['Q25'] == 'unresolved'
    assert all(r['day5'] > 2e-5 for r in e['p18'].values())
    for family, values in e['holdout'].items():
        if not values['pass']:
            assert f'holdout {family} exceeds 1.5x' in e['design_failures']
    check(SUITE, {'p18': exact(e['p18'], source='P18 TC4-7 fit, TC8-11 holdout; DPS only'),
                  'design_failures': exact(e['design_failures'], source='unaltered design limits; report, not acceptance'),
                  'late_platform': exact(e['late_platform'], source='T12 actual charge units, TC206-404'),
                  'bridge': exact(e['bridge'], source='stored/recomputed cache bridge; P12 slots')})
