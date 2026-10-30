"""Golden baseline management.

    python -m tufu_balance.golden status [SUITE ...] [--all]
    python -m tufu_balance.golden accept SUITE --reason "..." [--keys K ...] [--drop K ...] [--loosen]
                                         [--seed-original] [--allow-stale]
    python -m tufu_balance.golden accept SUITE --files [NAME ...] --reason "..."
    python -m tufu_balance.golden rebaseline-original SUITE --reason "..." [--keys K ...]
    python -m tufu_balance.golden guard

``accept`` and ``rebaseline-original`` take the values the last test session recorded under
``Balance/out/golden/latest/`` (without ``--keys``: every key it recorded for the suite), and
refuse values recorded at another commit than HEAD: run ``pytest`` first.
"""

from __future__ import annotations

import argparse
import subprocess
import sys

from .store import BALANCE, AcceptRefused, GoldenError, GoldenStore, canonical


def _cell(value, width=48):
    text = "-" if value is None else canonical(value)
    return text if len(text) <= width else text[:width - 3] + "..."


def cmd_status(store: GoldenStore, args) -> int:
    suites = args.suites or store.suites()
    if not suites:
        print("no golden suites yet")
        return 0
    bad = 0
    for suite in suites:
        st = store.status(suite)
        rows = st["rows"] if args.all else [r for r in st["rows"] if (r["state"], r["guard"]) not in
                                            (("ok", "ok"), ("ok", "-"))]
        counts = {}
        for r in st["rows"]:
            counts[r["state"]] = counts.get(r["state"], 0) + 1
        bad += sum(r["state"] == "FAIL" or r["guard"] == "VIOLATION" for r in st["rows"])
        summary = ", ".join(f"{k} {v}" for k, v in sorted(counts.items()))
        print(f"{suite}: {len(st['rows'])} key(s) ({summary}); loosening records {st['loosening']}, "
              f"accept records {st['accepted']}")
        for r in rows:
            print(f"  {r['state']:<12} guard={r['guard']:<9} {r['kind']:<5} {r['key']}: current {_cell(r['current'])}"
                  f" latest {_cell(r['latest'])} original {_cell(r['original'])}")
    return 1 if bad else 0


def cmd_accept(store: GoldenStore, args) -> int:
    try:
        if args.files is not None:
            changed = store.accept_files(args.suite, args.reason, args.files or None, allow_stale=args.allow_stale)
            print(f"{args.suite}: accepted {len(changed)} file(s): {', '.join(changed) or '-'}")
            return 0
        res = store.accept(args.suite, args.reason, keys=args.keys, loosen=args.loosen, drop=args.drop or (),
                           seed_original=args.seed_original, allow_stale=args.allow_stale)
    except AcceptRefused as e:
        print(e, file=sys.stderr)
        return 2
    print(f"{args.suite}: accepted {len(res.changed)} key(s), dropped {len(res.dropped)}, "
          f"seeded original {len(res.seeded_original)}, registered loosening {len(res.loosened)}")
    for k in sorted(set(res.changed + res.dropped + res.seeded_original)):
        print(f"  {k}")
    return 0


def cmd_rebaseline(store: GoldenStore, args) -> int:
    dirty = subprocess.run(["git", "status", "--porcelain", "--", str(store.root)], cwd=BALANCE,
                           capture_output=True, text=True).stdout.strip()
    if dirty and not args.allow_dirty:
        print("refusing: tests/golden has uncommitted changes; rebaseline-original must be its own commit\n" + dirty,
              file=sys.stderr)
        return 2
    try:
        changed = store.rebaseline_original(args.suite, args.reason, keys=args.keys, allow_stale=args.allow_stale)
    except AcceptRefused as e:
        print(e, file=sys.stderr)
        return 2
    print(f"{args.suite}: original rebaselined for {len(changed)} key(s); commit this change on its own")
    for k in changed:
        print(f"  {k}")
    violations = store.guard([args.suite])
    if violations:
        print("current is now looser than the new original for:\n  " + "\n  ".join(map(str, violations)))
    return 0


def cmd_guard(store: GoldenStore, args) -> int:
    violations = store.guard()
    for v in violations:
        print(v)
    print(f"{len(store.suites())} suite(s), {len(violations)} violation(s)")
    return 1 if violations else 0


def main(argv=None, store: GoldenStore | None = None) -> int:
    ap = argparse.ArgumentParser(prog="python -m tufu_balance.golden", description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    p = sub.add_parser("status", help="compare recorded results with current and original")
    p.add_argument("suites", nargs="*")
    p.add_argument("--all", action="store_true", help="list every key, not only the ones that need attention")
    p = sub.add_parser("accept", help="write recorded results into current/")
    p.add_argument("suite")
    p.add_argument("--reason", required=True)
    p.add_argument("--keys", nargs="+")
    p.add_argument("--drop", nargs="+", help="remove keys from current (counts as loosening)")
    p.add_argument("--loosen", action="store_true", help="allow and register values looser than original")
    p.add_argument("--files", nargs="*", help="accept recorded files of a file suite (all when no names)")
    p.add_argument("--seed-original", action="store_true",
                   help="freeze the measurement as original for new keys whose tests declare no original "
                        "(only for numbers the design measures on first run)")
    p.add_argument("--allow-stale", action="store_true", help="accept results recorded at another commit")
    p = sub.add_parser("rebaseline-original", help="write recorded results into original/ (data changed)")
    p.add_argument("suite")
    p.add_argument("--reason", required=True)
    p.add_argument("--keys", nargs="+")
    p.add_argument("--allow-dirty", action="store_true", help=argparse.SUPPRESS)
    p.add_argument("--allow-stale", action="store_true", help="use results recorded at another commit")
    sub.add_parser("guard", help="fail if current is looser than original without a registered loosening")
    args = ap.parse_args(argv)
    store = store or GoldenStore()
    try:
        return {"status": cmd_status, "accept": cmd_accept, "rebaseline-original": cmd_rebaseline,
                "guard": cmd_guard}[args.cmd](store, args)
    except GoldenError as e:
        print(f"error: {e}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.exit(main())
