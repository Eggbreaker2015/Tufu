"""Preliminary full-run structural comparison; K3 acceptance is not complete.

Select on T06 relative errors at TC 4..103; TC >= 104 is held out.
T20 remains a hard filter. The baseline wins exact ties.
"""
from __future__ import annotations

import json
import math
import os
from pathlib import Path
import statistics

from . import driver, metrics as M, params as P, report, targets as T
from .stage_open import BALANCE, measure, read_series, write_json
from .stage_slow import evaluate as slow_evaluate
from ..unity import bake_report, inventory, request

ARMS = {
    'baseline': {},
    'h2': {'HealthModel': 'RegenToFull'},
    'h3': {'CapIdleAtSessionEnd': True, 'BreakEnRouteAtBoundary': True},
    'h2-h3': {'HealthModel': 'RegenToFull', 'CapIdleAtSessionEnd': True, 'BreakEnRouteAtBoundary': True},
}


def t19(db, job):
    """Compare against A on R1's A-defined informative subsets; X4 validates the run oracle."""
    from ..model import bake as B, data as D
    shipped = D.load_bake_context(db)
    run = bake_report.bake_context(job, db)
    armors = D.load_armor_configs(db)
    rows = []
    for row in D.load_spawner_rows(db):
        sp = D.spawner_input_from_row(row, armors)
        if not sp.is_days or str(row['go_name']).startswith('BossTower') or row['go_name'] == 'Spawner_Dummy':
            continue
        rows.append((sp, B.bake_spawner(shipped, sp), B.bake_spawner(run, sp)))
    def within(a, b, tol):
        if math.isinf(a) and math.isinf(b):
            return True
        return math.isfinite(a) and math.isfinite(b) and b != 0 and abs(a / b - 1) <= tol
    result = {}
    for field, subset in (('health', rows), ('damage', [r for r in rows if r[1].defense != 0])):
        scene = 'health_base' if field == 'health' else 'damage_base'
        pred = 'new_health' if field == 'health' else 'new_damage'
        counts = {'n': len(subset)}
        for tol in (.01, .05):
            counts[str(tol)] = {
                'A': sum(within(getattr(s, scene), getattr(a, pred), tol) for s, a, b in subset),
                'free': sum(within(getattr(s, scene), getattr(b, pred), tol) for s, a, b in subset)}
        result[field] = counts
    return result


def main():
    workspace = BALANCE / 'out/calibration/full/preflight'
    workspace.mkdir(parents=True, exist_ok=True)
    write_json(workspace / 'plan.json', {'arms': ARMS, 'selection': 'mean T06 relative error TC 4..103; T20 hard filter; baseline first on ties', 'holdout': 'TC >= 104; report only', 'mode': 'free', 'ab': 'A'})
    jobs = []
    for name, changes in ARMS.items():
        p = P.resolve(['defaults', 'open', 'slow'], changes)
        job = request.make_job(name, params=p.params, outputs={'inventory': True, 'bake_report': True}, max_wall_seconds=600)
        jobs.append(job)
    drv = driver.Driver(driver.default_backend(), batch_size=12)
    paths = measure(jobs, drv, workspace / 'jobs.json')
    with T.connect_ro(os.environ['TUFU_BALANCE_DB']) as db:
        tg = T.build(db, slot_schedule=T.load_slot_schedule())
        scores = {}
        for name, path in paths.items():
            series = read_series(path)
            ev, detail = slow_evaluate(path, tg)
            cells = M.t06(series, tg)
            def score(predicate):
                vals = [c.rel for c in cells if c.rel is not None and predicate(c.tc)]
                return statistics.mean(vals) if vals else None
            scores[name] = {'train': score(lambda tc: 4 <= tc < 104), 'holdout': score(lambda tc: tc >= 104), 't20_ok': detail['t20_ok'], 'slow': detail}
        write_json(workspace / 'scores.json', scores)
        write_json(workspace / 't19.json', t19(db, paths['baseline']))
        ranked = sorted((n for n in ARMS if scores[n]['t20_ok'] and scores[n]['train'] is not None), key=lambda n: scores[n]['train'])
        if not ranked:
            raise RuntimeError('no valid candidate')
        summary = {'ranked': ranked, 'scores': {n: {k:v for k,v in scores[n].items() if k != 'slow'} for n in ranked}}
        write_json(workspace / 'ranking.json', summary)
        print(json.dumps(summary, indent=2))


if __name__ == '__main__':
    main()
