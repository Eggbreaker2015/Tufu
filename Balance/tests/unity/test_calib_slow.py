"""Reproduce K2 evidence; regression success does not waive failed design criteria."""
import csv
import json
from pathlib import Path
import pytest
from tufu_balance.calibrate import stage_slow as S, params as P, report, runcache
from tufu_balance.golden import at_most, check, exact
from tufu_balance.unity import trace, paramsio, request

pytestmark = pytest.mark.unity
SUITE = 'calib_slow'


@pytest.fixture(scope='module')
def calibration(unity_rev):
    workspace = S.BALANCE/'out/calibration/slow-work'
    assert S.main(['--rev', unity_rev, '--workspace', str(workspace)]) == 0
    directory = Path(json.loads((workspace/'latest.json').read_text())['report'])
    return directory, json.loads((directory/'evidence.json').read_text())


def test_real_modules_and_archived_inputs(calibration, unity_rev):
    directory, e = calibration
    assert len(e['jobs']) >= 48
    for name, row in e['jobs'].items():
        assert row['code_rev'] == unity_rev
        source = Path(row['directory'])
        assert runcache.run_ok(source.parent)
        m = json.loads((source/'manifest.json').read_text())
        j = m['job']
        assert P.resolve(['defaults', 'open'], row['requested_changes']).params == j['params']
        assert j['mode'] == 'free' and j['stop']['day'] == 6
        assert not any(j.get(k) for k in ('override', 'engine_probe', 'test'))
        assert m['engine']['counters']['slow_fights'] > 0
        assert m['engine']['counters']['fast_ticks'] == 2
        assert m['params']['request_hash_given'] == m['params']['request_hash'] == request.params_hash(j['params'])
        assert m['params']['hash'] == paramsio.effective_hash(j['params'])
        assert row['effective_hash_verified']
        g = json.loads((source/'asset_guard.json').read_text())['after_job']
        assert g['ok'] and not g['changed_on_disk']
        assert row['asset_guard_sha256'] == report.sha256_file(source/'asset_guard.json')
    confirmation = e['confirmation']
    assert confirmation['matches_request'] and len(confirmation['byte_identical_files'])==10
    assert runcache.run_ok(Path(confirmation['directory']).parent)
    m = json.loads((directory/'manifest.json').read_text())
    for name, digest in {**m['files'], **m['evidence_files']}.items():
        assert report.sha256_file(directory/name) == digest
    with (directory/'killseq_times.csv').open() as f:
        rows = list(csv.DictReader(f))
    assert len(rows) == e['preflight']['spawner_rows']
    assert all(float(r['farmed_seconds']) > 0 for r in rows)
    assert e['preflight']['exact'] == e['preflight']['intervals'] == 60


def test_hard_t20_and_each_respawn_is_reported(calibration):
    _, e = calibration
    selected = e['candidates'][e['selected']]
    assert selected['t20_ok'] and not selected['premature_location4'] and not selected['day3_deaths']
    assert [d['day'] for d in selected['countRespawn']] == list(range(6))
    for name, c in e['candidates'].items():
        tr = trace.read(e['jobs'][name]['directory'])
        deaths = [d for d in tr.events if d['type'] == 'death']
        assert len(c['day3_deaths']) == sum(d['day']==3 for d in deaths)
        assert sum(d['countRespawn'] for d in c['countRespawn']) == len(deaths)
        # Successful gate fights must keep their pre-clear boss flag.
        for u in (x for x in tr.events if x['type']=='unlock' and x['day']<4):
            fights = [x for x in tr.events if x['type']=='slow_fight' and x['location']==u['location']-1
                      and x['p_after']==u['playtime']]
            assert fights and all(x['boss'] for x in fights)
    check(SUITE, {'t20.countRespawn': exact(selected['countRespawn'], source='daily.csv vs death events'),
                  't20.all_candidates': exact({n: dict(ok=c['t20_ok'], respawns=c['countRespawn'], day3=c['day3_deaths'])
                      for n,c in e['candidates'].items()}, source='each death reported; no-lethality null model')})


def test_training_choice_and_holdout_are_separate(calibration):
    directory, e = calibration
    changes = json.loads((directory/'slow.json').read_text())
    assert P.resolve(['defaults', 'open'], changes).params == P.resolve(['defaults', 'open', 'slow']).params
    with (directory/'hypotheses.csv').open() as f:
        rows = [r for r in csv.DictReader(f) if r['family']=='P13/P15']
    assert rows[0]['hypothesis']==e['selected'] and rows[1]['hypothesis']==e['runner_up']
    assert all(float(a['score_train']) <= float(b['score_train']) for a,b in zip(rows, rows[1:]))
    check(SUITE, {'choices': exact(changes, source='T20 filter then training-only rank'),
                  'p14.CMove': exact(changes['CMove'], original=1., source='P14; Q34 registered fallback'),
                  'p14.CRegen': exact(changes['CRegen'], original=1., source='P14; Q34 registered fallback'),
                  'holdout.ratio': at_most(e['holdout'], original=1.5, source='5.5; train 0-1, holdout 2-3')})


def test_unaltered_scientific_limits_remain_visible(calibration):
    directory, e = calibration
    c = e['candidates'][e['selected']]
    with (directory/'residuals.csv').open() as f:
        cells = list(csv.DictReader(f))
    t16 = [r for r in cells if r['target']=='T16']
    assert len(t16)==60
    bad03 = sum(r['target']=='T03' and r['ok']=='0' for r in cells)
    bad16 = sum(r['ok']=='0' for r in t16)
    check(SUITE, {'t16.failures': at_most(bad16, original=0, source='T16 original 5%/one-clear thresholds'),
                  't16.main_max_rel': at_most(c['t16']['main_max_rel'], original=.05, source='T16 main stats'),
                  't03.failures': at_most(bad03, original=0, source='all original T03 rules'),
                  't04.max_rel': at_most(max(r['rel'] for r in c['t04']), original=.03, source='T04 days 0-3'),
                  'items': exact(c['item_counts'], original=[4,7,8,8], source='TC1-4 item counts'),
                  'arrivals': exact(c['arrivals'], source='unlock events; exact ranges independently reported'),
                  't07.early_interval_miss': at_most(max(max(lo-c['arrivals'].get(str(loc),{}).get('p',1e9),
                      c['arrivals'].get(str(loc),{}).get('p',1e9)-hi, 0.)
                      for loc,lo,hi in ((2,31.2,37.4),(3,110.2,115.3))), original=0., source='T07 original P ranges'),
                  't07.island4_day': exact(c['arrivals'].get('4',{}).get('day'), original=5, source='T07 day5 unlock'),
                  't20.first_location4_tc': exact(c['first_location4_tc'], original=6, source='T20 first visible TC6'),
                  'design_failures': exact(e['design_failures'], source='failures are not design acceptance')})
    assert e['accepted'] == (not e['design_failures'])
