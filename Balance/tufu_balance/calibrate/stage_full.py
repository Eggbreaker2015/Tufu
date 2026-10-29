"""Full-run structural comparison, stage-3 jobs and reproducible calibration archive.

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



def check_t05_explanations(evidence, tolerances):
    """Enforce the first free-run unexplained-interval cap using spawner-day evidence."""
    unexplained = evidence["unexplained"]
    if not isinstance(unexplained, list) or len(set(unexplained)) != len(unexplained):
        raise ValueError("invalid T05 unexplained interval evidence")
    return len(unexplained) <= tolerances["targets"]["T05"]["max_unexplained"]

def t19(db, job):
    """Compare against A on R1's A-defined informative subsets; X4 validates the run oracle."""
    from ..model import bake as B, data as D
    shipped = D.load_bake_context(db)
    run = bake_report.bake_context(job, db)
    armors = D.load_armor_configs(db)
    rows = []
    minute = []
    for row in D.load_spawner_rows(db):
        sp = D.spawner_input_from_row(row, armors)
        triple = (sp, B.bake_spawner(shipped, sp), B.bake_spawner(run, sp))
        if not sp.is_days:
            minute.append(triple)
        elif not (str(row['go_name']).startswith('BossTower') or row['go_name'] == 'Spawner_Dummy'):
            rows.append(triple)
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
    result['minute'] = {'n': len(minute),
        'health_exact_A': sum(s.health_base == a.new_health for s, a, b in minute),
        'health_exact_free': sum(s.health_base == b.new_health for s, a, b in minute),
        'damage_exact_A': sum(s.damage_base == a.new_damage for s, a, b in minute),
        'damage_exact_free': sum(s.damage_base == b.new_damage for s, a, b in minute)}
    return result


def main(workspace=None):
    workspace = Path(workspace or BALANCE / 'out/calibration/full/preflight')
    workspace.mkdir(parents=True, exist_ok=True)
    write_json(workspace / 'plan.json', {'arms': ARMS, 'selection': 'mean T06 relative error TC 4..103; T20 hard filter; baseline first on ties', 'holdout': 'TC >= 104; report only', 'mode': 'free', 'ab': 'A'})
    jobs = []
    for name, changes in ARMS.items():
        p = P.resolve(['defaults', 'open', 'slow'], changes)
        job = request.make_job(name, params=p.params, outputs={'inventory': True, 'bake_report': True}, max_wall_seconds=600)
        jobs.append(job)
    backend = driver.default_backend()
    if backend.name != 'serve':
        raise RuntimeError('start the launcher serve process before the full-stage run')
    drv = driver.Driver(backend, batch_size=12)
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




def archive(db, paths, workspace, out_root, run_id='calibrated-round4'):
    """Rebuild the standard report from guarded job directories and preregistered grid results."""
    from .stage_open import trace_cells, inventory_data, t05_evidence, stat_intervals, events
    from . import sensitivity as S
    from ..model import data
    workspace = Path(workspace)
    paths = {k: Path(v) for k, v in paths.items()}
    tg = T.build(db, slot_schedule=T.load_slot_schedule())
    tol = M.load_tolerances()
    shipped = data.load_simulation_data(db).time_chunk_snapshots
    evaluations, margins, evidence, bakes, x4 = [], [], {}, {}, {}
    for mode in ('free', 'checkpoint', 'seed-1', 'seed-2'):
        path = paths[mode]
        guard = json.loads((path / 'asset_guard.json').read_text())
        if not guard['after_job']['ok'] or guard['after_job']['changed_on_disk']:
            raise ValueError(f'invalid asset guard: {path}')
        cells = M.compute(read_series(path), tg)
        cells = [c for c in cells if c.target not in ('T04', 'T05') or c.target == 'T04' and c.group in ('day_0_3', 'p_final')] + trace_cells(path, tg)
        ev = M.evaluate(cells, tol, 'closed')
        ev.mode = mode
        if (path / 'spawner_days.csv.gz').exists():
            evidence[mode] = t05_evidence(db, path, shipped)
            e = evidence[mode]
            ok = check_t05_explanations(e, tol)
            ev.groups.append(M.GroupResult('T05', 'unexplained', 'pass' if ok else 'fail', len(e['bad_spans']), len(e['bad_spans'])-len(e['unexplained']), 'max_unexplained=3', str(e['unexplained'])))
            if not ok:
                ev.targets['T05'] = 'fail'
        bake = bakes[mode] = t19(db, path)
        external_ok = all(bake[f][q]['free'] >= math.ceil(.95 * bake[f][q]['A']) for f in ('health', 'damage') for q in ('0.01', '0.05'))
        ev.targets['T19'] = 'pass' if external_ok else 'fail'
        _, slow = slow_evaluate(path, tg)
        ev.targets['T20'] = 'pass' if slow['t20_ok'] else 'fail'
        x4[mode] = bake_report.compare_x4(path, db)
        evaluations.append(ev)
        for row in inventory_data(path)['headroom']:
            margins.append({'item': row['key'], 'level': row['level'], 'target_day': row['target'], 'reached_tc': row['day'], 'copies_margin': row['headroom'], 'shards_margin': row['copies']-row['shards'], 'note': mode+' first processing on/after target'})
    grid_paths = {r['job_id']: Path(r['job_dir']) for r in json.loads((workspace/'sensitivity_jobs.json').read_text())}
    base = S.t07(S.unlock_days(events(grid_paths['extended-1.0'])))
    grid = {}
    for name, path in grid_paths.items():
        unlock = S.t07(S.unlock_days(events(path)))
        grid[name.removeprefix('extended-')] = {'bad_intervals': len(S.bad_intervals(stat_intervals(path, shipped))), 'flips': sum((base[k]['distance']==0) != (unlock[k]['distance']==0) for k in base if k >= 5)}
    free_unlock = S.t07(S.unlock_days(events(paths['free'])))
    flipped = [k for k in base if k >= 5 and (base[k]['distance']==0) != (free_unlock[k]['distance']==0)]
    flips = {'flip_count':len(flipped), 'flipped_islands':flipped, 'open_in_range':sum(base[k]['distance']==0 for k in base if k >= 5), 'free_in_range':sum(free_unlock[k]['distance']==0 for k in free_unlock if k >= 5)}
    scores = json.loads((workspace/'scores.json').read_text())
    arms = sorted(scores, key=lambda n: (not scores[n]['t20_ok'], scores[n]['train']))
    hypotheses = [{'stage':'full', 'family':'slow structural options', 'rank':i, 'hypothesis':n, 'score_train':scores[n]['train'], 'score_holdout':scores[n]['holdout'], 'post_hoc':False, 'distinguishing_segment':'T06 TC4-103; TC>=104 holdout; T20 hard filter', 'note':'A'} for i,n in enumerate(arms, 1)]
    sensitivity = [{'probe':'dps', 'scale':k, 'metric':m, 'value':v, 'note':'discrete grid only; interpolation unverified'} for k, vals in grid.items() for m,v in vals.items()]
    inputs = {'database_sha256': report.sha256_file(os.environ['TUFU_BALANCE_DB']), 'tolerances_sha256': report.sha256_file(M.TOLERANCES), 'job_manifests': {k: report.sha256_file(v/'manifest.json') for k,v in paths.items()}, 'job_guards': {k: report.sha256_file(v/'asset_guard.json') for k,v in paths.items()}}
    out = report.write(out_root, 'full', run_id=run_id, evaluations=evaluations, params=P.resolve(['defaults','calibrated']), hypotheses=hypotheses, sensitivity=sensitivity, evolution_margin=margins, inputs=inputs)
    for name, value in [('t19.json', bakes), ('x4.json', x4), ('t05-evidence.json', evidence), ('expanded_sensitivity.json', grid), ('t07-flips.json', flips)]:
        write_json(out/name, value)
    t15rows = [r for r in report.residual_rows(evaluations) if r['target'] == 'T15']
    (out/'t15-observations.csv').write_text(report.csv_text(report.RESIDUAL_COLUMNS, t15rows))
    for name in ('plan.json','scores.json','jobs.json','full_jobs.json','sensitivity_jobs.json'):
        (out/name).write_bytes((workspace/name).read_bytes())
    manifest = json.loads((out/'manifest.json').read_text())
    manifest['files'] = {f.name:report.sha256_file(f) for f in sorted(out.iterdir()) if f.is_file() and f.name != 'manifest.json'}
    write_json(out/'manifest.json', manifest)
    return out


def run_stage(workspace, out_root):
    """Submit the declared full-run and sensitivity jobs through the cached driver."""
    from .stage_open import make_job
    from ..unity import override
    workspace = Path(workspace)
    workspace.mkdir(parents=True, exist_ok=True)
    backend = driver.default_backend()
    if backend.name != 'serve':
        raise RuntimeError('start the launcher serve process before the full-stage run')
    drv = driver.Driver(backend, batch_size=12)
    with T.connect_ro(os.environ['TUFU_BALANCE_DB']) as db:
        jobs = []
        for name, mode, changes in [('free','free',{}), ('checkpoint','checkpoint',{}), ('seed-1','free',{'RollMode':'Seeded','Seed':1}), ('seed-2','free',{'RollMode':'Seeded','Seed':2})]:
            p = P.resolve(['defaults','calibrated'], changes)
            job = request.make_job(name, mode, params=p.params, outputs={'inventory':True,'bake_report':True,'spawner_days':True}, max_wall_seconds=600)
            if mode == 'checkpoint':
                job['override'] = override.default_override(db, from_day=4)
            jobs.append(job)
        paths = measure(jobs, drv, workspace/'full_jobs.json')
        scales = (.74,.75,.8,.85,.9,1.,1.1,1.15,1.2,1.25,1.26)
        measure([make_job(db, f'extended-{v}', {'DiagnosticPlayerDpsScale':v}) for v in scales], drv, workspace/'sensitivity_jobs.json')
        return archive(db, paths, workspace, out_root)


if __name__ == '__main__':
    import argparse
    ap = argparse.ArgumentParser()
    ap.add_argument('--workspace', type=Path, default=BALANCE/'out/calibration/full/preflight')
    ap.add_argument('--out', type=Path, default=BALANCE/'out/calibration')
    ap.add_argument('--cached', action='store_true', help='rebuild from recorded job paths')
    args = ap.parse_args()
    if args.cached:
        from .stage_open import load_results
        paths = load_results(args.workspace/'full_jobs.json')
        with T.connect_ro(os.environ['TUFU_BALANCE_DB']) as db:
            print(archive(db, paths, args.workspace, args.out))
    else:
        main(args.workspace)
        print(run_stage(args.workspace, args.out))
