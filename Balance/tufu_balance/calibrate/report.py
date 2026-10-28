"""Calibration reports: tables and CSV only, byte-identical for the same inputs (design 5.6; unit P10).

``write(out_root, stage, ...)`` writes ``<out_root>/<stage>/<run_id>/``:

* ``targets.csv``: per mode, target and group: status, cell counts, rule, details, and the median
  relative residual on the training and holdout splits (for the 1.5x rule of design 5.5);
* ``residuals.csv``: every cell (design 5.5: per cell, not only totals);
* ``params.csv``: the resolved parameters and the layer each value came from;
* ``hypotheses.csv``: the ranked hypotheses of a stage (best, runner-up, ...);
* ``sensitivity.csv``, ``evolution_margin.csv``: rows handed in by C5/K1 (header only when absent);
* ``manifest.json``: stage, run id, input digests, statuses and the sha256 of every file above.

No timestamps, no host names, explicit row order, numbers in shortest round-trip form. ``run_id``
defaults to a digest of everything written, so the same inputs land in the same directory.
"""

from __future__ import annotations

import csv
import hashlib
import io
import json
import math
import statistics
from pathlib import Path
from typing import Any, Iterable, Mapping, Sequence

from .metrics import Evaluation
from .params import Resolved

SCHEMA = "tufu.calib.report/1"
DEFAULT_ROOT = Path(__file__).resolve().parents[2] / "out" / "calibration"
RESIDUAL_COLUMNS = ("mode", "target", "group", "key", "stat", "tc", "split", "sim", "ref", "residual", "rel",
                    "bound", "ok", "note")
TARGET_COLUMNS = ("mode", "target", "group", "status", "cells", "cells_ok", "median_rel_train",
                  "median_rel_holdout", "rule", "detail")
HYPOTHESIS_COLUMNS = ("stage", "family", "rank", "hypothesis", "score_train", "score_holdout", "post_hoc",
                      "distinguishing_segment", "note")
SENSITIVITY_COLUMNS = ("probe", "scale", "metric", "value", "note")
EVOLUTION_MARGIN_COLUMNS = ("item", "level", "target_day", "reached_tc", "copies_margin", "shards_margin", "note")


def fmt(value: Any) -> str:
    """Deterministic cell text: shortest round-trip floats, NaN/Infinity spelled out, bools as 0/1."""
    if value is None:
        return ""
    if isinstance(value, bool):
        return "1" if value else "0"
    if isinstance(value, float):
        if math.isnan(value):
            return "NaN"
        if math.isinf(value):
            return "Infinity" if value > 0 else "-Infinity"
        return repr(value)
    if isinstance(value, (list, tuple)):
        return json.dumps([_plain(v) for v in value], separators=(",", ":"))
    if isinstance(value, Mapping):
        return json.dumps({str(k): _plain(v) for k, v in sorted(value.items())}, separators=(",", ":"))
    if hasattr(value, "item"):  # numpy scalar
        return fmt(value.item())
    return str(value)


def _plain(v: Any) -> Any:
    if hasattr(v, "item"):
        v = v.item()
    if isinstance(v, float) and not math.isfinite(v):
        return fmt(v)
    return v


def csv_text(columns: Sequence[str], rows: Iterable[Mapping[str, Any]]) -> str:
    buf = io.StringIO()
    w = csv.writer(buf, lineterminator="\n")
    w.writerow(columns)
    for r in rows:
        unknown = set(r) - set(columns)
        if unknown:
            raise ValueError(f"unknown columns {sorted(unknown)}")
        w.writerow([fmt(r.get(c)) for c in columns])
    return buf.getvalue()


def _median(values: list[float]) -> float | None:
    values = [v for v in values if v is not None and not math.isnan(v)]
    return statistics.median(values) if values else None


def target_rows(evaluations: Sequence[Evaluation]) -> list[dict]:
    rows = []
    for ev in sorted(evaluations, key=lambda e: e.mode):
        cells: dict[tuple[str, str], list] = {}
        for c in ev.cells:
            cells.setdefault((c.target, c.group), []).append(c)
        for g in sorted(ev.groups, key=lambda g: (g.target, g.group)):
            members = cells.get((g.target, g.group), [])
            rows.append({"mode": ev.mode, "target": g.target, "group": g.group, "status": g.status, "cells": g.n,
                         "cells_ok": g.n_ok,
                         "median_rel_train": _median([c.rel for c in members if c.split == "train"]),
                         "median_rel_holdout": _median([c.rel for c in members if c.split == "holdout"]),
                         "rule": g.rule, "detail": g.detail})
        for tid, status in sorted(ev.targets.items()):
            rows.append({"mode": ev.mode, "target": tid, "group": "*", "status": status})
    return rows


def residual_rows(evaluations: Sequence[Evaluation]) -> list[dict]:
    rows = []
    for ev in sorted(evaluations, key=lambda e: e.mode):
        for c in sorted(ev.cells, key=lambda c: (c.target, c.group, c.key)):
            rows.append({"mode": ev.mode, "target": c.target, "group": c.group, "key": c.key, "stat": c.stat,
                         "tc": c.tc, "split": c.split, "sim": c.sim, "ref": c.ref, "residual": c.residual,
                         "rel": c.rel, "bound": c.bound, "ok": c.ok, "note": c.note})
    return rows


def param_rows(params: Resolved | Mapping[str, Any] | None) -> list[dict]:
    if params is None:
        return []
    if isinstance(params, Resolved):
        return [{"param": k, "value": v, "layer": params.source.get(k)} for k, v in sorted(params.params.items())]
    return [{"param": k, "value": v, "layer": None} for k, v in sorted(params.items())]


def _sorted_rows(rows: Iterable[Mapping[str, Any]] | None, columns: Sequence[str]) -> list[dict]:
    rows = [dict(r) for r in rows or ()]
    return sorted(rows, key=lambda r: tuple(fmt(r.get(c)) for c in columns))


def build_files(*, stage: str, evaluations: Sequence[Evaluation], params: Resolved | Mapping | None = None,
                hypotheses: Iterable[Mapping[str, Any]] | None = None,
                sensitivity: Iterable[Mapping[str, Any]] | None = None,
                evolution_margin: Iterable[Mapping[str, Any]] | None = None,
                inputs: Mapping[str, Any] | None = None) -> dict[str, bytes]:
    """The report files as bytes (``manifest.json`` included), without writing anything."""
    hyp = sorted((dict(r) for r in hypotheses or ()),
                 key=lambda r: (fmt(r.get("stage")), fmt(r.get("family")), int(r.get("rank") or 0), fmt(r.get("hypothesis"))))
    files = {
        "targets.csv": csv_text(TARGET_COLUMNS, target_rows(evaluations)),
        "residuals.csv": csv_text(RESIDUAL_COLUMNS, residual_rows(evaluations)),
        "params.csv": csv_text(("param", "value", "layer"), param_rows(params)),
        "hypotheses.csv": csv_text(HYPOTHESIS_COLUMNS, hyp),
        "sensitivity.csv": csv_text(SENSITIVITY_COLUMNS, _sorted_rows(sensitivity, SENSITIVITY_COLUMNS)),
        "evolution_margin.csv": csv_text(EVOLUTION_MARGIN_COLUMNS,
                                         _sorted_rows(evolution_margin, EVOLUTION_MARGIN_COLUMNS)),
    }
    out = {name: text.encode("utf-8") for name, text in files.items()}
    manifest = {
        "schema": SCHEMA,
        "stage": stage,
        "modes": sorted(e.mode for e in evaluations),
        "status": {e.mode: dict(sorted(e.targets.items())) for e in sorted(evaluations, key=lambda e: e.mode)},
        "params_hash": params.params_hash if isinstance(params, Resolved) else None,
        "inputs": {str(k): _plain(v) for k, v in sorted((inputs or {}).items())},
        "files": {name: hashlib.sha256(data).hexdigest() for name, data in sorted(out.items())},
    }
    manifest["run_id"] = hashlib.sha256(json.dumps(manifest, sort_keys=True).encode()).hexdigest()[:16]
    out["manifest.json"] = (json.dumps(manifest, indent=1, sort_keys=True, ensure_ascii=False) + "\n").encode("utf-8")
    return out


def write(out_root: str | Path = DEFAULT_ROOT, stage: str = "harness", *, run_id: str | None = None,
          **kw) -> Path:
    """Write the report to ``<out_root>/<stage>/<run_id>/`` and return that directory."""
    files = build_files(stage=stage, **kw)
    rid = run_id or json.loads(files["manifest.json"])["run_id"]
    out = Path(out_root) / stage / rid
    out.mkdir(parents=True, exist_ok=True)
    for name, data in sorted(files.items()):
        tmp = out / f".{name}.tmp"
        tmp.write_bytes(data)
        tmp.replace(out / name)
    return out


def sha256_file(path: str | Path) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def main(argv=None) -> int:
    """``python -m tufu_balance.calibrate.report (JOB_DIR | SIM_DB | --shipped) [--stage S] [--out DIR]``:
    evaluate one run against the targets in both modes and write the report (prints its directory)."""
    import argparse
    import os
    import tempfile
    import warnings

    warnings.simplefilter("ignore")
    from . import metrics as M, targets as T
    from ..unity import simio

    ap = argparse.ArgumentParser(prog="python -m tufu_balance.calibrate.report")
    ap.add_argument("sim", nargs="?", help="a job directory (ingested when it has no sim.sqlite) or a sim.sqlite")
    ap.add_argument("--shipped", action="store_true", help="the shipped Simulation Data as the run (reference)")
    ap.add_argument("--stage", default="harness")
    ap.add_argument("--out", default=str(DEFAULT_ROOT))
    ap.add_argument("--modes", default="open,closed")
    args = ap.parse_args(argv)
    con = T.connect_ro(os.environ.get("TUFU_BALANCE_DB") or BALANCE_DB)
    tg = T.build(con, slot_schedule=T.load_slot_schedule())
    with tempfile.TemporaryDirectory() as tmp:
        if args.shipped:
            db = simio.ingest(M.write_shipped_job(Path(tmp) / "shipped"))
        elif args.sim and Path(args.sim).is_dir():
            db = Path(args.sim) / simio.SIM_DB
            if not db.exists():
                db = simio.ingest(args.sim)
        elif args.sim:
            db = Path(args.sim)
        else:
            ap.error("give a job directory, a sim.sqlite or --shipped")
        tol = M.load_tolerances()
        evs = [M.evaluate_run(db, tg, mode, tol) for mode in args.modes.split(",")]
        out = write(args.out, args.stage, evaluations=evs,
                    inputs={"sim_db_sha256": sha256_file(db), "tolerances_sha256": sha256_file(M.TOLERANCES)})
    print(out)
    return 0


BALANCE_DB = Path(__file__).resolve().parents[2] / "out" / "balance.sqlite"

if __name__ == "__main__":
    raise SystemExit(main())
