"""Offline checks for the stage-1 experiment and its data partition."""
from dataclasses import replace

import pytest

from tufu_balance.calibrate import stage_open as S, targets as T
from tufu_balance.model import data
from tufu_balance.unity import request


def test_closed_matrix_and_replay_contract(db):
    jobs = S.initial_jobs(db)
    assert len(jobs) == 30
    assert len({j['job_id'] for j in jobs}) == len(jobs)
    assert all(j['mode'] == 'replay' and j.get('reset_mode', 'reenter_play') == 'reenter_play' for j in jobs)
    assert all(j['override']['econ_caches'] == 'stored' for j in jobs)
    assert all(j['params']['KillLossMultiplier'] == 1 for j in jobs if not j['job_id'].startswith('theta-'))
    assert {j['params']['KillState'] for j in jobs if j['job_id'].startswith('kill-')} == {0, 1}
    assert all(j['stop']['day'] == 12 for j in jobs if j['job_id'].startswith('p18-'))
    r = request.make_request(jobs)
    r.update(out_dir=str(S.BALANCE / 'out/calibration/test'), code_rev='a'*40, run_key='b'*64)
    request.validate(r)


def test_nn_path_is_open_and_deterministic():
    # A closed tour would add a return distance of 7; a coordinate sum gives 10.
    assert S.nn_tour([(3, 0), (3, 4), (0, 4)]) == 10
    assert S.nn_tour([]) == 0


def test_discrete_selection_ignores_holdout_and_keeps_runner_up():
    first = S.ranking('x', {'a': (1, 100), 'b': (2, 0)}, post_hoc=False, segment='train')
    second = S.ranking('x', {'a': (1, 0), 'b': (2, 100)}, post_hoc=False, segment='train')
    assert [r['hypothesis'] for r in first] == ['a', 'b'] == [r['hypothesis'] for r in second]


def test_active_coefficients_never_fit_holdout(monkeypatch, db):
    snaps = data.load_simulation_data(db).time_chunk_snapshots
    days = [type('D', (), {'day': d, 'farmed_seconds': 60*(d+10), 'tour': d+3, 'speed': 1})() for d in range(4, 405)]
    monkeypatch.setattr(S, 'fast_days', lambda p: days)
    monkeypatch.setattr(S, 'tour_features', lambda db, p: {d: (d+3, (d+7)**.5) for d in range(4, 405)})
    paths = {'kill-start': None, 'tour': None}
    changes, summary, _ = S.active_fit(db, paths, snaps)
    perturbed = [replace(s, playtime=s.playtime+10000) if s.time_chunk_index > 104 else s for s in snaps]
    changed, other, _ = S.active_fit(db, paths, perturbed)
    for field in ('Kappa', 'TourA', 'TourB'):
        assert changes[field] == changed[field]
    assert summary['models']['FarmedTime']['holdout'] != other['models']['FarmedTime']['holdout']


def test_p12_fit_uses_stored_inputs_not_fitted_slot_fixture(db):
    target = T.build(db, slot_schedule={0: (99, 99)})
    scores, rows = S.p12_scores(db, target)
    assert min(scores, key=lambda n: scores[n][0]) == '4,8'
    assert all(r['tc'] <= 24 for r in rows if r['split'] == 'train')
    assert all(r['tc'] >= 25 for r in rows if r['split'] == 'holdout')
