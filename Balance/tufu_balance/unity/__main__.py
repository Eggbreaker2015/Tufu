"""Command line for the Unity harness (design 1.4.4). Run from ``Balance/``::

    python -m tufu_balance.unity selftest [--rev REV] [--runs N] [--nographics]
    python -m tufu_balance.unity run (--preset NAME | --request R.json) [--rev REV | --dev] [--force]
    python -m tufu_balance.unity serve start [--rev REV] [--idle-timeout S] | status | stop | submit (--preset|--request)
    python -m tufu_balance.unity open [--rev REV | --dev]
    python -m tufu_balance.unity status
    python -m tufu_balance.unity ingest JOB_DIR [--db PATH]        # outputs -> JOB_DIR/sim.sqlite (C1)
    python -m tufu_balance.unity spawner-map [--out CSV]          # SpawnerId <-> path|Key (C1)
    python -m tufu_balance.unity params build [--layers L ...] [--set K=JSON ...] [--float32] [--out FILE]   # (C3c)
    python -m tufu_balance.unity params import [--layers L ...] [--set K=JSON ...] [--float32] [--rev REV] [--dest PATH]
    python -m tufu_balance.unity bake JOB_DIR                     # X4: the job's C# bake report against model.bake (C8)
    python -m tufu_balance.unity bake JOB_DIR --plan [--rev REV]  # bake_plan.json in a fresh editor; touches no asset
    python -m tufu_balance.unity bake [JOB_DIR] --apply --token SHA256 [--plan-file P] [--dest SCENE]
    python -m tufu_balance.unity bake --selftest [--rev REV]      # write-back logic on an unsaved temporary scene

Exit status is the run's exit code (0 ok, 2 guard, 3 timeout, 4 not ready, 5 bad request, 6 self-check,
7 dev worktree changed, 8 ready check).
"""

from __future__ import annotations

import argparse
import json
import sqlite3
import sys
from pathlib import Path

from . import launcher, request as req, runner, schema


def _options(args) -> launcher.Options:
    opts = launcher.Options()
    if getattr(args, "nographics", False):
        opts.nographics = True
    if getattr(args, "allow_main_checkout", False):
        opts.allow_main_checkout = True
    if getattr(args, "stall_timeout", None):
        opts.stall_timeout_s = args.stall_timeout
    return opts


def _request(args) -> dict:
    if getattr(args, "request", None):
        return json.loads(Path(args.request).read_text())
    return req.preset(getattr(args, "preset", None) or "selftest")


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(prog="python -m tufu_balance.unity")
    sub = ap.add_subparsers(dest="cmd", required=True)

    def common(p, dev=True):
        p.add_argument("--rev", help="commit to check out in the run worktree (default: this worktree's HEAD)")
        if dev:
            p.add_argument("--dev", action="store_true", help="run in this worktree (needs a Library; never cached)")
        p.add_argument("--nographics", action="store_true")
        p.add_argument("--allow-main-checkout", action="store_true")
        p.add_argument("--stall-timeout", type=float)

    p = sub.add_parser("selftest", help="headless start, readiness, remedy, one empty job, exit")
    common(p)
    p.add_argument("--runs", type=int, default=1)
    p = sub.add_parser("run", help="run a batch request")
    common(p)
    p.add_argument("--preset", choices=sorted(req.PRESETS))
    p.add_argument("--request")
    p.add_argument("--force", action="store_true", help="ignore a cached result with the same run_key")
    p = sub.add_parser("serve", help="resident editor")
    p.add_argument("action", choices=("start", "status", "stop", "submit"))
    common(p, dev=False)
    p.add_argument("--idle-timeout", type=float, default=1800.0)
    p.add_argument("--preset", choices=sorted(req.PRESETS))
    p.add_argument("--request")
    p = sub.add_parser("open", help="open the project interactively with the right arguments")
    common(p)
    sub.add_parser("status", help="lock, run worktree and Unity processes")
    p = sub.add_parser("ingest", help="a job's outputs -> sim.sqlite (no Unity)")
    p.add_argument("job_dir")
    p.add_argument("--db", help="output database (default JOB_DIR/sim.sqlite)")
    p = sub.add_parser("spawner-map", help="SpawnerId <-> runtime path|Key from the extracted database (no Unity)")
    p.add_argument("--out", help="CSV file (default stdout)")
    p = sub.add_parser("params", help="merge parameter layers (build) or write them into the parameter asset (import)")
    p.add_argument("action", choices=("build", "import"))
    p.add_argument("--layers", nargs="*", default=[], help="layers after the defaults, e.g. calibrated (sim/params/<name>.json)")
    p.add_argument("--set", action="append", default=[], metavar="FIELD=JSON", help="per-run override")
    p.add_argument("--out", help="build: write the params document here (default stdout)")
    p.add_argument("--rev", help="import: commit to check out in the run worktree (default: this worktree's HEAD)")
    p.add_argument("--dest", help="import: where the written asset goes (default: this worktree's asset)")
    p.add_argument("--float32", action="store_true", help="round float fields to float32 values (fitted doubles)")
    p.add_argument("--nographics", action="store_true")
    p = sub.add_parser("bake", help="bake report check (no Unity), plan, guarded write-back, self-test")
    p.add_argument("job_dir", nargs="?")
    mode = p.add_mutually_exclusive_group()
    mode.add_argument("--plan", action="store_true", help="write bake_plan.json (old -> new, skip reasons); no asset changes")
    mode.add_argument("--apply", action="store_true", help="write the plan into Main.unity (needs --token)")
    mode.add_argument("--selftest", action="store_true", help="write-back logic on an unsaved temporary scene")
    p.add_argument("--token", help="apply: sha256 of the plan file")
    p.add_argument("--plan-file", help="apply: the plan (default: the one plan made from JOB_DIR's report)")
    p.add_argument("--dest", help="apply: scene file to update (default: this worktree's Main.unity)")
    p.add_argument("--rev", help="plan/selftest: commit to check out in the run worktree")
    p.add_argument("--nographics", action="store_true")
    args = ap.parse_args(argv)

    if args.cmd == "bake":
        return _bake(args)

    if args.cmd == "ingest":
        from . import simio
        try:
            print(simio.ingest(args.job_dir, args.db))
        except (simio.SimioError, schema.SchemaError) as e:
            print(f"error: {e}", file=sys.stderr)
            return 1
        return 0
    if args.cmd == "spawner-map":
        from .spawner_map import load_identity_map
        con = sqlite3.connect(f"file:{launcher.balance_db()}?mode=ro", uri=True)
        text = load_identity_map(con).to_csv()
        if args.out:
            Path(args.out).write_text(text, encoding="utf-8")
        else:
            sys.stdout.write(text)
        return 0

    if args.cmd == "params":
        from ..calibrate.params import ParamsError
        from . import paramsio
        try:
            overrides = {}
            for item in args.set:
                key, _, value = item.partition("=")
                overrides[key] = json.loads(value)
            if args.action == "build":
                resolved = paramsio.resolve_layers(args.layers, overrides)
                if args.float32:
                    resolved = paramsio.round_float32(resolved)
                text = json.dumps(paramsio.input_document(resolved), indent=1, ensure_ascii=False) + "\n"
                if args.out:
                    Path(args.out).write_text(text, encoding="utf-8")
                else:
                    sys.stdout.write(text)
                return 0
            result = paramsio.import_params(args.layers, overrides, rev=args.rev, dest=args.dest, opts=_options(args),
                                            float32=args.float32)
            print(result.dest or result.out_dir)
            return result.exit_code
        except (ParamsError, ValueError) as e:
            print(f"error: {e}", file=sys.stderr)
            return launcher.EXIT_BAD_REQUEST
        except launcher.LauncherError as e:
            print(f"error: {e}", file=sys.stderr)
            return e.code
        except runner.WorktreeError as e:
            print(f"error: {e}", file=sys.stderr)
            return launcher.EXIT_GUARD

    try:
        if args.cmd == "selftest":
            code = 0
            for _ in range(args.runs):
                result = launcher.selftest(rev=args.rev, dev=args.dev, force=True, archive_previous=True, opts=_options(args))
                print(result.run_dir)
                code = code or result.exit_code
            return code
        if args.cmd == "run":
            result = launcher.run(_request(args), rev=args.rev, dev=args.dev, force=args.force, opts=_options(args))
            print(result.run_dir)
            return result.exit_code
        if args.cmd == "serve":
            if args.action == "start":
                state = launcher.serve_start(rev=args.rev, idle_timeout_s=args.idle_timeout, opts=_options(args))
                print(json.dumps({k: state[k] for k in ("pid", "code_rev", "queue_dir", "serve_dir")}, indent=1))
                return 0
            if args.action == "status":
                print(json.dumps(launcher.serve_status(), indent=1))
                return 0
            if args.action == "stop":
                out = launcher.serve_stop(opts=_options(args))
                print(json.dumps(out, indent=1, default=str))
                return launcher.EXIT_GUARD if out.get("guard_problems") else 0
            result = launcher.serve_submit(_request(args), opts=_options(args))
            print(result.run_dir)
            return result.exit_code
        if args.cmd == "open":
            print(launcher.open_editor(rev=args.rev, dev=args.dev, opts=_options(args)))
            return 0
        run_tree = runner.run_worktree()
        project = runner.project_dir(run_tree)
        print(json.dumps({
            "run_worktree": str(run_tree),
            "head": runner.head(run_tree),
            "clean": not runner.porcelain(run_tree).strip(),
            "lock": launcher.UnityLock(run_tree).read(),
            "unity_processes": launcher.unity_processes(project),
            "serve": launcher.serve_status(run_tree=run_tree),
        }, indent=1))
        return 0
    except (launcher.LauncherError, req.RequestError) as e:
        print(f"error: {e}", file=sys.stderr)
        return getattr(e, "code", launcher.EXIT_BAD_REQUEST)
    except runner.WorktreeError as e:
        print(f"error: {e}", file=sys.stderr)
        return launcher.EXIT_GUARD


def _bake(args) -> int:
    from . import bake_report as BR
    try:
        if args.selftest:
            result = BR.selftest(rev=args.rev, opts=_options(args))
            print(result.out_dir)
            return result.exit_code
        if args.apply:
            plan_file = args.plan_file or (BR.find_plan(args.job_dir) if args.job_dir else None)
            if plan_file is None:
                raise BR.BakeToolError("--apply needs --plan-file or JOB_DIR")
            result = BR.apply(plan_file, args.token, dest=args.dest, caller=BR.CALLER_CLI, opts=_options(args))
            print(json.dumps({"exit_code": result.exit_code, "out_dir": str(result.out_dir),
                              "after": result.report.get("after")}, indent=1))
            return result.exit_code
        if not args.job_dir:
            raise BR.BakeToolError("JOB_DIR is required")
        if args.plan:
            result = BR.plan(args.job_dir, rev=args.rev, opts=_options(args))
            print(json.dumps({"exit_code": result.exit_code, "plan": str(result.plan) if result.plan else None,
                              "token": result.token, "counts": (result.tool or {}).get("counts")}, indent=1))
            return result.exit_code
        con = sqlite3.connect(f"file:{launcher.balance_db()}?mode=ro", uri=True)
        con.row_factory = sqlite3.Row
        res = BR.compare_x4(args.job_dir, con)
        res["mismatches"] = res["mismatches"][:20]
        print(json.dumps(res, indent=1))
        return 0 if res["new_bits_equal"] == res["spawners"] - len(res["errors_both"]) and not res["mismatches"] else 1
    except (BR.BakeToolError, schema.SchemaError, OSError) as e:
        print(f"error: {e}", file=sys.stderr)
        return launcher.EXIT_GUARD if str(e).startswith("refused") else launcher.EXIT_BAD_REQUEST
    except launcher.LauncherError as e:
        print(f"error: {e}", file=sys.stderr)
        return e.code
    except runner.WorktreeError as e:
        print(f"error: {e}", file=sys.stderr)
        return launcher.EXIT_GUARD


if __name__ == "__main__":
    sys.exit(main())
