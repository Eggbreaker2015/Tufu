"""Calibration partitioning and missing-evidence checks, without Unity."""
import pytest
from tufu_balance.calibrate import stage_slow as S, metrics as M
from tufu_balance.unity import request


def test_free_matrix_cannot_inject_judging_states():
    jobs = S.initial_jobs()
    assert len(jobs) == 48
    for j in jobs:
        assert j['mode'] == 'free' and j['stop']['day'] == 6
        assert j['params']['CMove'] == j['params']['CRegen'] == 1
        assert not any(j.get(k) for k in ('override', 'engine_probe', 'test'))
        assert j['params']['WeaponSlotUnlockLocations'] == [4, 8]
    r = request.make_request(jobs, out_dir=str(S.BALANCE/'out/calibration/test'))
    request.validate(r)


def test_selection_is_independent_of_holdout_and_hard_filter_precedes_rank(monkeypatch):
    paths = {'a': None, 'b': None, 'rejected': None}
    values = {'a': (2, 100, True), 'b': (3, 0, True), 'rejected': (0, 0, False)}
    def evaluate(path, tg):
        a, b, ok = values[tg]
        return None, dict(scores=dict(train=a, holdout=b), t20_ok=ok)
    # Map paths to unique opaque evidence rather than replacing ranking itself.
    monkeypatch.setattr(S, 'evaluate', lambda p, tg: evaluate(None, p))
    paths = {k: k for k in paths}
    candidates = {k: {} for k in paths}
    a = S.choose(paths, None, candidates)
    values['a'] = (2, 0, True)
    values['b'] = (3, 100, True)
    b = S.choose(paths, None, candidates)
    assert a[0] == b[0] == ['a', 'b']
    assert a[1][-1]['family'] == 'T20-rejected'
    values.update({k: (0, 0, False) for k in values})
    with pytest.raises(RuntimeError, match='no candidate'):
        S.choose(paths, None, candidates)


def test_residual_normalization_preserves_zero_bound_failure():
    c = M.Cell('T16', 'small', 'x', 1, 0, 1, bound=0)
    assert S.normalized(c) == float('inf')
    c.sim = 0
    assert S.normalized(c) == 0
