"""Run the pre-registered K2 structural arms; select using training only."""
import argparse
import json
import os
import time
from pathlib import Path
from . import stage_slow as S, driver, report, targets as T
from .stage_open import load_results, measure, write_json

BASE = dict(PriorityFamily='Dynamic', SmartPath=True, TimeModel='FarmedTimeMoveByDistance',
            SlowLethalityGate='SameAsFast', DeathPolicy='SkipSpawner', CMove=1., CRegen=1.)
H3 = dict(CapIdleAtSessionEnd=True, BreakEnRouteAtBoundary=True)
ARMS = {
    'baseline': {},
    'h1-marginal': {'StatBenefitModel': 'MarginalKillTime'},
    'h1-rpm': {'StatBenefitModel': 'RpmNormalized'},
    'h1-marginal-h2': {'StatBenefitModel': 'MarginalKillTime', 'HealthModel': 'RegenToFull'},
    'h1-rpm-h2': {'StatBenefitModel': 'RpmNormalized', 'HealthModel': 'RegenToFull'},
    'h1-marginal-h2-h3': dict(StatBenefitModel='MarginalKillTime', HealthModel='RegenToFull', **H3),
    'h1-rpm-h2-h3': dict(StatBenefitModel='RpmNormalized', HealthModel='RegenToFull', **H3),
    'h3': H3,
}


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument('--rev', required=True)
    ap.add_argument('--workspace', type=Path, default=S.BALANCE/'out/calibration/slow-round2')
    ap.add_argument('--analyze-only', action='store_true')
    ap.add_argument('--write-layer', action='store_true')
    args = ap.parse_args(argv)
    start = time.monotonic()
    with T.connect_ro(os.environ['TUFU_BALANCE_DB']) as db:
        pre = S.preflight(db, args.workspace)
        pre['registered_arms'] = ARMS
        candidates = {n: dict(BASE, **c) for n,c in ARMS.items()}
        index = args.workspace/'structural.json'
        drv = driver.Driver(driver.default_backend(rev=args.rev), batch_size=12)
        paths = load_results(index) if args.analyze_only else measure([S.make_job(n,c) for n,c in candidates.items()], drv, index)
        ranked, hypotheses, evaluations, details = S.choose(paths, T.build(db), candidates)
        chosen = ranked[0]
        jobs = {}
        for n,p in paths.items():
            m = json.loads((p/'manifest.json').read_text())
            jobs[n] = dict(directory=str(p), code_rev=m['code_rev'], requested_changes=candidates[n],
                params_hash=m['params']['hash'], asset_guard_sha256=report.sha256_file(p/'asset_guard.json'))
        next_distinct = next((n for n in ranked[1:] if details[n]['scores']['train'] != details[chosen]['scores']['train']), None)
        detail = dict(selected=chosen, runner_up=next_distinct, choices=candidates[chosen], split=S.SPLIT,
            preflight=pre, jobs=jobs, candidates=details, design_failures=details[chosen]['failures'],
            holdout=details[chosen]['holdout_ratio'], accepted=not details[chosen]['failures'])
        out = report.write(report.DEFAULT_ROOT, 'slow', evaluations=[evaluations[chosen]],
            params=S.P.resolve(['defaults','open'], candidates[chosen]), hypotheses=hypotheses,
            inputs=dict(jobs=jobs, selection=chosen, split=S.SPLIT, preflight=pre))
        write_json(out/'evidence.json', detail)
        write_json(out/'slow.json', {'_stage': 'K2 round 2; see Calibration.md', **candidates[chosen]})
        rows = [dict(candidate=n, **r) for n,ev in evaluations.items() for r in report.residual_rows([ev])]
        (out/'all_residuals.csv').write_text(report.csv_text(('candidate',)+report.RESIDUAL_COLUMNS, rows))
        daily = [dict(candidate=n, **r) for n,d in details.items() for r in d['countRespawn']]
        (out/'countRespawn.csv').write_text(report.csv_text(tuple(daily[0]), daily))
        deaths = [dict(candidate=n, **r) for n,d in details.items() for r in d['day3_deaths']]
        (out/'day3_deaths.csv').write_text(report.csv_text(('candidate','type','day','playtime','total_playtime','spawner','location'), deaths))
        for p in args.workspace.iterdir():
            if p.name in ('killseq_times.csv','killseq_intervals.csv','preflight.json') or p.name.endswith('.timing.json'):
                (out/p.name).write_bytes(p.read_bytes())
        manifest = json.loads((out/'manifest.json').read_text())
        manifest['evidence_files'] = {p.name: report.sha256_file(p) for p in sorted(out.iterdir()) if p.name != 'manifest.json' and p.name not in manifest['files']}
        write_json(out/'manifest.json', manifest)
        write_json(args.workspace/'latest.json', dict(report=str(out)))
        if args.write_layer:
            (S.P.PARAMS_ROOT/'slow.json').write_bytes((out/'slow.json').read_bytes())
    print(json.dumps(dict(report=str(out), selected=chosen, failures=detail['design_failures'], wall_s=time.monotonic()-start), indent=2))


if __name__ == '__main__':
    main()
