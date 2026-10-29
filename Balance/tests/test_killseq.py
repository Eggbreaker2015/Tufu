"""killseq on the shipped early snapshots (design 4.5, 7.3 C7; unit C7): kill counts per reward category, the
decomposition residuals and the per-category clear time against farmedTime, kept in golden suite ``sim_slow``
as a ratchet for K2."""

from __future__ import annotations

import pytest

from tufu_balance.calibrate import killseq as ks
from tufu_balance.golden import at_most, check, exact
from tufu_balance.model import simcalc as sc
from tufu_balance.model.data import load_simulation_data


@pytest.fixture(scope="module")
def analysis(db):
    ctx = sc.load_simcalc_context(db)
    early = load_simulation_data(db).snapshots
    intervals = ks.decompose(ctx, early)
    times = ks.clear_times(ctx, early, intervals)
    return ctx, early, intervals, times


def test_first_interval_is_every_ot0_spawner_once(analysis):
    """The shipped first early snapshot (P = 1.1656, Health +15, Cut +3) is the six OT = 0 spawners of island 1,
    each once, in 69.9 active seconds: far less than their farmedTime (232 s) and than rpm.Item3 * fightTime alone
    (79.8 s), so no time form with the recovered fight factor 2 reaches it (Runbook 19)."""
    ctx, early, intervals, _ = analysis
    first = intervals[0]
    assert first.index == 1 and first.delta == {1: 15.0, 8: 3.0} and first.exact
    assert first.kills == {"1=10": 1, "1=2": 1, "1=3": 1, "8=1": 3}
    members = ks.eligible(ctx, early[0], early[1])
    assert sorted(s.d.optimum_time for s in members) == [0.0] * 6
    assert first.active_seconds == pytest.approx(69.94, abs=0.01)
    assert first.farmed == pytest.approx(232.09, abs=0.01)
    assert 2.0 * first.fight > first.active_seconds > first.fight


def test_categories_merge_identical_rewards(analysis):
    ctx, early, _, _ = analysis
    cats = ks.categories(ctx, ks.eligible(ctx, early[0], early[1]))
    assert {k: len(c.members) for k, c in cats.items()} == {"1=10": 1, "1=2": 1, "1=3": 1, "8=1": 3}


def test_solver_reports_residuals_in_clears():
    cat = ks.Category("1=10", ((1, 10.0),), None, (("s", 1),))
    kills, residual, exact_ = ks._solve({1: 25.0}, {}, [cat], [2])
    assert kills == [2] and residual == {"stat:1": 0.5} and not exact_
    kills, residual, exact_ = ks._solve({1: 20.0}, {}, [cat], [5])
    assert kills == [2] and residual == {} and exact_


def test_decomposition_and_clear_times_ratchet(analysis):
    _, _, intervals, times = analysis
    fit = ks.fit_parts(intervals)
    inexact = [iv.index for iv in intervals if not iv.exact]
    residual = sum(abs(v) for iv in intervals for v in iv.residual.values())
    results = {
        "killseq.intervals": exact(len(intervals), source="design 7.3 C7", note="early pairs of days 0-3 with dP > 0"),
        "killseq.kills": exact(sum(iv.total_kills for iv in intervals), source="design 7.3 C7"),
        "killseq.inexact_intervals": at_most(len(inexact), source="design 7.3 C7"),
        "killseq.residual_clears": at_most(round(residual, 6), source="design 7.3 C7"),
        "killseq.first.active_s": exact(round(intervals[0].active_seconds, 3), source="design 7.3 C7"),
        "killseq.first.farmed_s": exact(round(intervals[0].farmed, 3), source="design 7.3 C7"),
        "killseq.first.fight_s": exact(round(intervals[0].fight, 3), source="design 7.3 C7"),
        "killseq.fit": exact({"fight": round(fit.fight, 4), "move": round(fit.move, 4), "regen": round(fit.regen, 4),
                              "rms_s": round(fit.rms_seconds, 2), "intervals": fit.intervals,
                              "kappa": round(fit.kappa, 4)}, source="design 4.5",
                             note="dP = a*fight + b*move + c*regen over intervals ending on a fight"),
    }
    for key, t in times.items():
        results[f"killseq.clear_time.{key}"] = exact(
            {"kills": t.kills, "intervals": t.intervals, "members": len(t.members), "clear_s": round(t.clear_seconds, 2),
             "farmed_s": round(t.farmed_seconds, 2), "ratio": round(t.ratio, 4), "upper_bound": t.upper_bound},
            source="design 7.3 C7", note="mean active seconds per clear against mean farmedTime")
    check("sim_slow", results)
