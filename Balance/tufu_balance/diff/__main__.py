"""Change one number and inspect the whole modeled impact (design section 6)."""

from __future__ import annotations

import argparse
import os
from pathlib import Path
import sys
import time

from ..model.config import load_game_config
from . import compare, patchfile, report, tables


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("patch", type=Path, nargs="?")
    parser.add_argument("--db", type=Path, default=Path(os.environ.get("TUFU_BALANCE_DB") or compare.BALANCE / "out/balance.sqlite"))
    parser.add_argument("--skip-sim", action="store_true")
    parser.add_argument("--out", type=Path)
    parser.add_argument("--rev", help="Unity code revision (default HEAD)")
    parser.add_argument("--head", choices=("worktree",))
    parser.add_argument("--base", default="cached", help="asset baseline git ref, or cached")
    parser.add_argument("--cache-base", action="store_true", help="record the clean HEAD asset baseline for --base cached")
    args = parser.parse_args(argv)
    t0 = time.perf_counter()
    try:
        cfg = load_game_config(args.db)
        cache = compare.BALANCE / "out/diff/asset_base.json"
        if args.cache_base:
            if compare.git("status", "--porcelain", "--", compare.ASSET_PREFIX).strip():
                raise ValueError("--cache-base requires clean assets")
            report.json_file(cache, {"ref": compare.git("rev-parse", "HEAD").strip(), "db_sha256": compare.sha(args.db)})
            print(cache)
            return 0
        metadata = None
        if args.head:
            if args.patch:
                raise ValueError("choose patch mode or asset-version mode")
            base = args.base
            if base == "cached":
                import json
                if not cache.exists():
                    raise ValueError("no asset baseline cache; run diff --cache-base on the clean baseline first")
                saved = json.loads(cache.read_text())
                if saved["db_sha256"] != compare.sha(args.db):
                    raise ValueError("cached asset baseline uses a different database; refresh --cache-base")
                base = saved["ref"]
            cfg, patch, metadata = compare.asset_patch(cfg, base=base)
        else:
            patch = patchfile.load(args.patch) if args.patch else patchfile.empty()
        if metadata and metadata["warnings"] and not args.skip_sim:
            raise ValueError("asset edits include leaves outside the model; use --skip-sim to inspect raw differences")
        if metadata and metadata["baseline_overlay_leaves"] and not args.skip_sim:
            raise ValueError("asset base differs from the extracted DB; use --skip-sim or extract/cache that baseline first")
        t1 = time.perf_counter()
        comp = compare.run(cfg, patch, skip_sim=args.skip_sim, rev=args.rev)
        comp.manifest["db_sha256"] = compare.sha(args.db)
        if metadata:
            comp.manifest.update({k: v for k, v in metadata.items() if k not in ("warnings", "raw_diff")})
            comp.warnings += metadata["warnings"]
            comp.comparisons["static/asset_leaves.csv"] = metadata["raw_diff"]
        out = args.out or compare.BALANCE / "out/diff" / patch.name
        # Avoid mixing stale files from a previous simulated report with a skip-sim report.
        if out.exists() and any(out.iterdir()):
            import shutil
            for p in list(out.iterdir()):
                if p.name in ("sim", "static", "bake") and p.is_dir():
                    shutil.rmtree(p)
        path = report.write(comp, out, timings={"load_s": t1 - t0, "total_before_write_s": time.perf_counter() - t0})
        print(f"{path}\n{'有变化' if comp.changed or metadata and metadata['raw_diff'] else '无变化'}；"
              f"{time.perf_counter() - t0:.3f} s")
        return 0
    except (ValueError, OSError) as e:
        print(f"diff: {e}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
