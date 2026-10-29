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
    return ev, dict(scores=scores, holdout_ratio=ratio, t20_ok=not early4 and not day3,
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


def archive(workspace, paths, candidates, tg, out_root, pre):
    ranked, hypotheses, evaluations, details = choose(paths, tg, candidates)
    chosen = ranked[0]
    changes = {'CMove': 1., 'CRegen': 1., **candidates[chosen]}
    jobs = {}
    for name, directory in paths.items():
        m = json.loads((directory/'manifest.json').read_text())
        jobs[name] = dict(directory=str(directory), code_rev=m['code_rev'],
            asset_guard_sha256=report.sha256_file(directory/'asset_guard.json'), params_hash=m['params']['hash'])
    detail = dict(selected=chosen, runner_up=ranked[1], choices=changes, split=SPLIT, preflight=pre,
        candidates=details, jobs=jobs, design_failures=details[chosen]['failures'],
        holdout=details[chosen]['holdout_ratio'], accepted=not details[chosen]['failures'])
    out = report.write(out_root, 'slow', evaluations=[evaluations[chosen]],
        params=P.resolve(['defaults', 'open'], changes), hypotheses=hypotheses,
        inputs=dict(jobs=jobs, selection=chosen, split=SPLIT, preflight=pre))
    write_json(out/'evidence.json', detail)
    write_json(out/'slow.json', {'_stage': 'K2; empirical candidate, acceptance status in Runbook section 25', **changes})
    rows = [dict(candidate=n, **r) for n, ev in evaluations.items() for r in report.residual_rows([ev])]
    (out/'all_residuals.csv').write_text(report.csv_text(('candidate',)+report.RESIDUAL_COLUMNS, rows))
    for p in workspace.iterdir():
        if p.name in ('killseq_times.csv', 'killseq_intervals.csv', 'preflight.json') or p.name.endswith('.timing.json'):
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
        out, detail = archive(args.workspace, paths, candidates, T.build(db), args.out, pre)
        if args.write_layer:
            (P.PARAMS_ROOT/'slow.json').write_bytes((out/'slow.json').read_bytes())
    print(json.dumps(dict(report=str(out), selected=detail['selected'], failures=detail['design_failures'],
                         holdout_ratio=detail['holdout'], wall_s=time.monotonic()-started), indent=2))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
