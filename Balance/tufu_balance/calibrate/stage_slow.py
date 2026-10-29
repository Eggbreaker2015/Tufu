"""Stage 2: closed 48-candidate grid; train days 0-1, hold out 2-3.

Scientific acceptance failures remain failures, independently of regression tests.
"""
from __future__ import annotations
import argparse
from collections import Counter
import dataclasses
import itertools
import json
import math
import os
from pathlib import Path
import statistics
import time
from . import driver, killseq, metrics as M, params as P, report, targets as T
from .stage_open import BALANCE, load_results, measure, read_series, write_json
from ..unity import inventory, request, runner, trace
from ..model.f32 import f32

GRID = tuple(dict(zip(('PriorityFamily', 'SmartPath', 'TimeModel', 'SlowLethalityGate', 'DeathPolicy'), v))
    for v in itertools.product(('Dynamic', 'Static'), (True, False),
        ('FarmedTime', 'FarmedTimeMoveByDistance'), ('SameAsFast', 'CurrentHealth', 'None'), ('SkipSpawner', 'Retry')))
SPLIT = {'train': [0, 1], 'holdout': [2, 3]}


def preflight(db, workspace):
    """Run before submitting any grid. Snapshot positions do not identify distance times."""
    intervals, categories, parts = killseq.analyse(db)
    rows = []
    for key, c in categories.items():
        for scene, fid in c.members:
            rows.append(dict(category=key, scene=scene, file_id=fid, kills_in_category=c.kills,
                allocated_clear_seconds=c.clear_seconds, upper_bound=c.upper_bound, farmed_seconds=c.farmed_seconds,
                fight_seconds=c.fight_seconds, move_seconds=c.move_seconds, regen_seconds=c.regen_seconds,
                distance_model_lower_bound=2*c.fight_seconds+c.regen_seconds, ratio=c.ratio,
                note='category allocation, not per-spawner observation; distance/route not observed'))
    workspace.mkdir(parents=True, exist_ok=True)
    (workspace/'killseq_times.csv').write_text(report.csv_text(tuple(rows[0]), rows))
    ivs = [dataclasses.asdict(iv) for iv in intervals]
    for row in ivs:
        for k in ('kills', 'delta', 'item_delta', 'residual'):
            row[k] = json.dumps(row[k], sort_keys=True)
    (workspace/'killseq_intervals.csv').write_text(report.csv_text(tuple(ivs[0]), ivs))
    evidence = dict(intervals=len(intervals), exact=sum(iv.exact for iv in intervals),
        kills=sum(iv.total_kills for iv in intervals), categories=len(categories), spawner_rows=len(rows),
        parts_fit_diagnostic_only=dataclasses.asdict(parts), grid=GRID, split=SPLIT, CMove=1, CRegen=1,
        distance_model='not identifiable from snapshots; zero-move lower bound only',
        selection='T20 filter; mean T16 tolerance-normalized error at TC1/2 only; stable grid order for ties',
        holdout='same T16 normalized error at TC3/4; never used for selection',
        secondary_targets='T03/T04/island arrivals/items reported without reranking',
        db_sha256=report.sha256_file(os.environ['TUFU_BALANCE_DB']))
    write_json(workspace/'preflight.json', evidence)
    return evidence


def make_job(name, changes):
    p = P.resolve(['defaults', 'open'], {'CMove': 1., 'CRegen': 1., **changes})
    return request.make_job(name, day=6, params=p.params, outputs={'inventory': True}, max_wall_seconds=180.)


def initial_jobs():
    return [make_job(f'slow-{i:02d}', c) for i, c in enumerate(GRID)]


def normalized(c):
    if c.sim is None or c.ref is None:
        return math.inf
    scale = .05*abs(c.ref) if c.group == 'main' else c.bound
    return abs(c.sim-c.ref)/scale if scale else (0. if c.sim == c.ref else math.inf)


def evaluate(directory, tg):
    series, tr = read_series(directory), trace.read(directory)
    cells = M.t03(series, tg) + [c for c in M.t04(series, tg) if c.group == 'day_0_3'] + M.t16(series, tg)
    for c in cells:
        if c.target in ('T16', 'T04'):
            c.split = 'train' if c.tc <= 2 else 'holdout'
    ev = M.evaluate(cells, M.load_tolerances(), 'closed')
    values = [c for c in ev.cells if c.target == 'T16']
    scores = {s: statistics.mean(normalized(c) for c in values if c.split == s) for s in SPLIT}
    early4 = [dict(list=name, index=s.index, tc=s.tc, location=s.location) for name, snaps in
        (('early', series.early), ('tc', series.tc)) for s in snaps if s.tc < 6 and s.location >= 4]
    deaths = [e for e in tr.events if e['type'] == 'death']
    day3 = [e for e in deaths if e['day'] == 3]
    per_day = Counter(e['day'] for e in deaths)
    respawns = [dict(day=d['day'], countRespawn=d['countRespawn'], death_events=per_day[d['day']]) for d in tr.daily]
    assert all(r['countRespawn'] == r['death_events'] for r in respawns)
    arrivals = {str(e['location']): dict(day=e['day'], p=e['playtime']) for e in tr.events if e['type'] == 'unlock'}
    first4 = min((s.tc for s in series.tc+series.early if s.location >= 4), default=None)
    counts = [len(series.day(d).items) for d in range(1, 5)]
    failures = ['T20'] if early4 or day3 else []
    failures += [t for t in ('T03', 'T04', 'T16') if ev.targets[t] != 'pass']
    for loc, lo, hi in ((2, 31.2, 37.4), (3, 110.2, 115.3)):
        if not lo <= arrivals.get(str(loc), {}).get('p', math.inf) <= hi:
            failures.append(f'island{loc}')
    if arrivals.get('4', {}).get('day') != 5 or first4 != 6:
        failures.append('island4')
    if counts != [4, 7, 8, 8]:
        failures.append('items')
    ratio = scores['holdout']/scores['train'] if scores['train'] else (0 if not scores['holdout'] else math.inf)
    if ratio > 1.5:
        failures.append('holdout')
    # These K2-only targets are not computed by P10 from sim.sqlite.
    extra = [M.Cell('T20', 'hard', 'premature_location4', len(early4), 0, len(early4), ok=not early4),
             M.Cell('T20', 'hard', 'day3_deaths', len(day3), 0, len(day3), ok=not day3)]
    for loc, lo, hi in ((2, 31.2, 37.4), (3, 110.2, 115.3)):
        p = arrivals.get(str(loc), {}).get('p')
        extra.append(M.Cell('T07', 'early_arrival', f'island{loc}.P', p, [lo, hi],
            None if p is None else max(lo-p, p-hi, 0.), ok=p is not None and lo <= p <= hi))
    for key, value, ref in (('island4.unlock_day', arrivals.get('4', {}).get('day'), 5), ('island4.first_tc', first4, 6)):
        extra.append(M.Cell('T07', 'early_arrival', key, value, ref,
            None if value is None else value-ref, ok=value==ref))
    extra += [M.Cell('T11', 'early_item_count', f'TC{d}', n, r, n-r, tc=d, ok=n==r)
              for d, (n, r) in enumerate(zip(counts, (4, 7, 8, 8)), 1)]
    ev.cells.extend(extra)
    for target, group in (('T20', 'hard'), ('T07', 'early_arrival'), ('T11', 'early_item_count')):
        members = [c for c in extra if c.target==target]
        ok = sum(c.ok for c in members)
        ev.targets[target] = 'pass' if ok==len(members) else 'fail'
        ev.groups.append(M.GroupResult(target, group, ev.targets[target], len(members), ok,
                                      'K2 original criterion; no loosening'))
    return ev, dict(scores=scores, day_scores={str(d): statistics.mean(normalized(c) for c in values if c.tc==d+1)
        for d in range(4)}, holdout_ratio=ratio, t20_ok=not early4 and not day3,
        premature_location4=early4, day3_deaths=day3, countRespawn=respawns, arrivals=arrivals,
        first_location4_tc=first4, item_counts=counts, early_rows=len(series.early), failures=failures,
        t16=dict(hits=sum(c.ok for c in values), cells=len(values), main_max_rel=max(c.rel for c in values if c.group=='main')),
        t04=[dict(day=c.tc-1, sim=c.sim, ref=c.ref, rel=c.rel, ok=c.ok) for c in ev.cells if c.target=='T04'],
        t03=[dict(group=g.group, status=g.status, ok=g.n_ok, n=g.n) for g in ev.groups if g.target=='T03'])


def choose(paths, tg, candidates):
    evaluations, details = {}, {}
    for n, p in paths.items():
        evaluations[n], details[n] = evaluate(p, tg)
    eligible = [n for n, d in details.items() if d['t20_ok']]
    if not eligible:
        raise RuntimeError('no candidate passes hard T20; do not publish a layer')
    ranked = sorted(eligible, key=lambda n: (details[n]['scores']['train'], list(paths).index(n)))
    hypotheses = []
    for name in ranked + [n for n in paths if n not in eligible]:
        d = details[name]
        hypotheses.append(dict(stage='slow', family='P13/P15' if name in eligible else 'T20-rejected',
            rank=ranked.index(name)+1 if name in eligible else 0, hypothesis=name,
            score_train=d['scores']['train'], score_holdout=d['scores']['holdout'], post_hoc=False,
            distinguishing_segment='T16 TC1/2 train; TC3/4 holdout; T20 days 3-6 hard filter',
            note=json.dumps(candidates[name], sort_keys=True)))
    return ranked, hypotheses, evaluations, details


# Q34 fallback, fixed before its first run. The endpoint fit suggested subunit
# movement and near-zero regen; no day/point coefficients or fight multiplier.
COEFFICIENT_GRID = (0., .25, .5, .75, 1.)


def fallback(workspace, initial, candidates, tg, drv, *, analyze_only=False):
    ranked, _, evs, details = choose(initial, tg, candidates)
    by_model = {}
    for model in ('FarmedTime', 'FarmedTimeMoveByDistance'):
        names = [n for n in initial if candidates[n]['TimeModel']==model and details[n]['t20_ok']]
        by_model[model] = dict(candidates=len(names),
            training_t16_pass=sum(all(c.ok for c in evs[n].cells if c.target=='T16' and c.split=='train') for n in names),
            joint_t16_t03_pass=sum(evs[n].targets['T16']=='pass' and evs[n].targets['T03']=='pass' for n in names))
    # Trigger with training failures, not holdout errors. T03 is also reported.
    if any(x['training_t16_pass'] for x in by_model.values()):
        return initial, candidates, dict(used=False, recovered_models=by_model)
    base = dict(candidates[ranked[0]])
    base['TimeModel'] = 'DecomposedMoveByDistance' if 'Distance' in base['TimeModel'] else 'Decomposed'
    protocol = dict(used=True, recovered_models=by_model, base_from_training=ranked[0], base=base,
        grid=COEFFICIENT_GRID, bounds=[0., 1.], simplex_maxiter=12, simplex_maxfev=24,
        objective='T20 hard filter then T16 TC1/2 normalized mean; no holdout feedback',
        registration='Calibration.md Q34; no modification of defaults or original tolerances')
    if analyze_only:
        recorded = json.loads((workspace/'fallback_protocol.json').read_text())
        if 'optimizer' not in recorded:
            raise RuntimeError('fallback protocol is missing its recorded optimizer result')
        assert {k:v for k,v in recorded.items() if k != 'optimizer'} == protocol
        protocol = recorded
    else:
        write_json(workspace/'fallback_protocol.json', protocol)
    coarse = {f'coef-{i:02d}': dict(base, CMove=m, CRegen=r) for i,(m,r) in
              enumerate(itertools.product(COEFFICIENT_GRID, repeat=2))}
    coarse_path = workspace/'coarse.json'
    coarse_paths = load_results(coarse_path) if analyze_only else measure(
        [make_job(n,c) for n,c in coarse.items()], drv, coarse_path)
    paths = {**initial, **coarse_paths}
    candidates = {**candidates, **coarse}
    ranked, _, _, _ = choose(paths, tg, candidates)
    seed = candidates[ranked[0]]
    if seed['TimeModel'] not in ('Decomposed', 'DecomposedMoveByDistance'):
        seed = dict(base, CMove=1., CRegen=1.)
    if analyze_only:
        fine = json.loads((workspace/'simplex_candidates.json').read_text())
        fine_paths = load_results(workspace/'simplex.json')
    else:
        import numpy as np
        from scipy.optimize import minimize
        fine, fine_paths, memo = {}, {}, {}
        def point(x):
            return tuple(float(f32(min(1., max(0., v)))) for v in x)
        for n,p in paths.items():
            c = candidates[n]
            if c['TimeModel']==base['TimeModel']:
                _, d = evaluate(p,tg)
                memo[(c['CMove'], c['CRegen'])] = d['scores']['train'] if d['t20_ok'] else 1e6+d['scores']['train']
        def batch(points):
            fresh = [x for x in dict.fromkeys(map(point,points)) if x not in memo]
            additions = {}
            for x in fresh:
                n = f'simplex-{len(fine):02d}'
                fine[n] = dict(base, CMove=x[0], CRegen=x[1])
                additions[n] = fine[n]
            if additions:
                index = len(list(workspace.glob('simplex-step-*.json')))
                result = measure([make_job(n,c) for n,c in additions.items()], drv,
                                 workspace/f'simplex-step-{index:02d}.json')
                fine_paths.update(result)
                for n,p in result.items():
                    _, d = evaluate(p,tg)
                    c = additions[n]
                    memo[(c['CMove'],c['CRegen'])] = d['scores']['train'] if d['t20_ok'] else 1e6+d['scores']['train']
            return [memo[point(x)] for x in points]
        x0 = np.array([seed.get('CMove',1.), seed.get('CRegen',1.)])
        simplex = [x0, x0+[-.125 if x0[0]>=.875 else .125,0], x0+[0,-.125 if x0[1]>=.875 else .125]]
        batch(simplex)
        result = minimize(lambda x: batch([x])[0], x0, method='Nelder-Mead', bounds=((0,1),(0,1)),
            options=dict(initial_simplex=np.array(simplex), maxiter=12, maxfev=24, xatol=.015625, fatol=.001))
        protocol['optimizer'] = dict(x=list(map(float,result.x)), fun=float(result.fun),
            nit=result.nit, nfev=result.nfev, success=bool(result.success), message=str(result.message))
        write_json(workspace/'simplex_candidates.json', fine)
        write_json(workspace/'simplex.json', [dict(job_id=n,job_dir=str(p)) for n,p in fine_paths.items()])
        write_json(workspace/'fallback_protocol.json', protocol)
    return {**paths, **fine_paths}, {**candidates, **fine}, protocol


def bootstrap_coefficients(chosen, candidates, details):
    """Conditional two-training-day block bootstrap over already measured candidates."""
    import numpy as np
    base = candidates[chosen]
    fields = ('PriorityFamily', 'SmartPath', 'TimeModel', 'SlowLethalityGate', 'DeathPolicy')
    names = [n for n,c in candidates.items() if details[n]['t20_ok'] and
             all(c[k]==base[k] for k in fields)]
    selected = []
    tie_rng = np.random.default_rng(20261030)
    for days in np.random.default_rng(20261029).integers(0,2,(1000,2)):
        losses = {n: sum(details[n]['day_scores'][str(d)] for d in days) for n in names}
        tied = [n for n in names if abs(losses[n]-min(losses.values())) <= 1e-12]
        selected.append(tied[int(tie_rng.integers(len(tied)))])
    tied = [n for n in names if abs(details[n]['scores']['train']-details[chosen]['scores']['train'])<=1e-12]
    return dict(blocks=2, samples=1000, level=.9, conditional_on_discrete_policy=True,
        training_ties=tied, identified=len(tied)==1,
        parameters={k: dict(lo=float(np.quantile([candidates[n].get(k,1.) for n in selected],.05)),
                            hi=float(np.quantile([candidates[n].get(k,1.) for n in selected],.95)))
                    for k in ('CMove','CRegen')},
        selected_counts=dict(sorted(Counter(selected).items())),
        crosses_behaviors=len({json.dumps((details[n]['item_counts'], details[n]['countRespawn']), sort_keys=True)
                               for n in selected}) > 1,
        note='tie-aware bootstrap over only two training blocks; nonsmooth objective; not parameter identification')


def archive(workspace, paths, candidates, tg, out_root, pre):
    ranked, hypotheses, evaluations, details = choose(paths, tg, candidates)
    chosen = ranked[0]
    changes = {'CMove': 1., 'CRegen': 1., **candidates[chosen]}
    jobs = {}
    for name, directory in paths.items():
        m = json.loads((directory/'manifest.json').read_text())
        jobs[name] = dict(directory=str(directory), code_rev=m['code_rev'],
            asset_guard_sha256=report.sha256_file(directory/'asset_guard.json'), params_hash=m['params']['hash'],
            requested_changes=candidates[name])
    detail = dict(selected=chosen, runner_up=ranked[1], choices=changes, split=SPLIT, preflight=pre,
        candidates=details, jobs=jobs, design_failures=details[chosen]['failures'],
        holdout=details[chosen]['holdout_ratio'], accepted=not details[chosen]['failures'],
        bootstrap=bootstrap_coefficients(chosen, candidates, details))
    out = report.write(out_root, 'slow', evaluations=[evaluations[chosen]],
        params=P.resolve(['defaults', 'open'], changes), hypotheses=hypotheses,
        inputs=dict(jobs=jobs, selection=chosen, split=SPLIT, preflight=pre))
    write_json(out/'evidence.json', detail)
    write_json(out/'slow.json', {'_stage': 'K2; empirical candidate, acceptance status in Runbook section 25', **changes})
    rows = [dict(candidate=n, **r) for n, ev in evaluations.items() for r in report.residual_rows([ev])]
    (out/'all_residuals.csv').write_text(report.csv_text(('candidate',)+report.RESIDUAL_COLUMNS, rows))
    daily = [dict(candidate=n, **r) for n,d in details.items() for r in d['countRespawn']]
    (out/'countRespawn.csv').write_text(report.csv_text(tuple(daily[0]), daily))
    deaths = [dict(candidate=n, **r) for n,d in details.items() for r in d['day3_deaths']]
    (out/'day3_deaths.csv').write_text(report.csv_text(
        ('candidate', 'type', 'day', 'playtime', 'total_playtime', 'spawner', 'location'), deaths))
    for p in workspace.iterdir():
        if p.name in ('killseq_times.csv', 'killseq_intervals.csv', 'preflight.json', 'fallback_protocol.json', 'simplex_candidates.json') or p.name.endswith('.timing.json'):
            (out/p.name).write_bytes(p.read_bytes())
    manifest = json.loads((out/'manifest.json').read_text())
    manifest['evidence_files'] = {p.name: report.sha256_file(p) for p in sorted(out.iterdir())
                                if p.name != 'manifest.json' and p.name not in manifest['files']}
    write_json(out/'manifest.json', manifest)
    write_json(workspace/'latest.json', dict(report=str(out)))
    return out, detail


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--rev')
    ap.add_argument('--workspace', type=Path, default=BALANCE/'out/calibration/slow-work')
    ap.add_argument('--out', type=Path, default=report.DEFAULT_ROOT)
    for flag in ('preflight-only', 'analyze-only', 'write-layer'):
        ap.add_argument('--'+flag, action='store_true')
    args = ap.parse_args(argv)
    started = time.monotonic()
    with T.connect_ro(os.environ['TUFU_BALANCE_DB']) as db:
        pre = preflight(db, args.workspace)
        if args.preflight_only:
            print(json.dumps(pre, indent=2)); return 0
        drv = driver.Driver(driver.default_backend(rev=args.rev), batch_size=12)
        if args.rev and drv.backend.identity().code_rev != runner.resolve(runner.WORKTREE, args.rev):
            raise RuntimeError('resident editor differs from --rev')
        path = args.workspace/'initial.json'
        paths = load_results(path) if args.analyze_only else measure(initial_jobs(), drv, path)
        candidates = {f'slow-{i:02d}': c for i, c in enumerate(GRID)}
        tg = T.build(db)
        paths, candidates, protocol = fallback(args.workspace, paths, candidates, tg, drv, analyze_only=args.analyze_only)
        pre['fallback'] = protocol
        out, detail = archive(args.workspace, paths, candidates, tg, args.out, pre)
        if args.write_layer:
            (P.PARAMS_ROOT/'slow.json').write_bytes((out/'slow.json').read_bytes())
    print(json.dumps(dict(report=str(out), selected=detail['selected'], failures=detail['design_failures'],
                         holdout_ratio=detail['holdout'], wall_s=time.monotonic()-started), indent=2))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
