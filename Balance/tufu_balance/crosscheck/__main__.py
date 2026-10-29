"""``python -m tufu_balance.crosscheck JOB_DIR [JOB_DIR ...] [--db PATH] [--checks X1 X2 ...]``: run the oracle
cross-checks on job directories, write ``<job>/crosscheck/*.csv`` and ``summary.json``, print one line per check.
Exit code 1 when any check fails."""

from __future__ import annotations

import argparse
import os
import sys

from ..model.data import connect
from . import CHECKS, run_checks


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(prog="python -m tufu_balance.crosscheck")
    ap.add_argument("jobs", nargs="+")
    ap.add_argument("--db", default=os.environ.get("TUFU_BALANCE_DB"))
    ap.add_argument("--checks", nargs="+", default=list(CHECKS), choices=CHECKS)
    args = ap.parse_args(argv)
    con = connect(args.db)
    failed = False
    for job in args.jobs:
        for name, r in run_checks(job, con, checks=args.checks).items():
            failed |= r.status == "fail"
            vac = f"; vacuous: {', '.join(r.vacuous)}" if r.vacuous else ""
            print(f"{job}: {name} {r.status} ({r.items} items, {r.mismatches} mismatches{vac})")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
