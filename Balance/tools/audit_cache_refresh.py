"""Audit every job output before/after the early snapshot cache refresh (Enginefix)."""
from __future__ import annotations

import argparse
from collections import Counter
import csv
import hashlib
import json
import math
from pathlib import Path

VOLATILE = {"timings.json", "unity.log"}
DERIVED = {"sim.sqlite", "sim.sqlite-shm", "sim.sqlite-wal"}


def differences(a, b, path=""):
    if isinstance(a, dict) and isinstance(b, dict):
        for k in sorted(a.keys() | b.keys()):
            yield from differences(a.get(k), b.get(k), f"{path}.{k}" if path else k)
    elif isinstance(a, list) and isinstance(b, list) and len(a) == len(b):
        for i, (x, y) in enumerate(zip(a, b)):
            yield from differences(x, y, f"{path}[{i}]")
    elif a != b and not (isinstance(a, float) and isinstance(b, float) and math.isnan(a) and math.isnan(b)):
        yield {"field": path, "before": a, "after": b}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def audit(before, after, jobs):
    report = {"before": str(before), "after": str(after), "jobs": {}, "violations": []}
    for job in jobs:
        a, b = before / job, after / job
        files = {p.relative_to(a).as_posix() for p in a.rglob("*") if p.is_file()}
        files |= {p.relative_to(b).as_posix() for p in b.rglob("*") if p.is_file()}
        entries = {}
        for name in sorted(files):
            x, y = a / name, b / name
            if name in DERIVED or name.startswith("crosscheck/"):
                continue
            if not x.exists() or not y.exists():
                report["violations"].append(f"{job}/{name}: file set changed")
                continue
            if x.read_bytes() == y.read_bytes():
                entries[name] = {"status": "identical", "sha256": digest(x)}
                continue
            entry = {"status": "volatile" if name in VOLATILE else "changed",
                     "before_sha256": digest(x), "after_sha256": digest(y)}
            if name.endswith(".json"):
                entry["fields"] = list(differences(json.loads(x.read_text()), json.loads(y.read_text())))
            elif name.endswith(".csv"):
                with x.open() as f, y.open() as g:
                    entry["fields"] = list(differences(list(csv.DictReader(f)), list(csv.DictReader(g))))
            entries[name] = entry
            allowed = {"manifest.json", "simulation_data.json", "slow_tick_basis.csv", "bake_report.json", "bake_report.csv"} | VOLATILE
            if name not in allowed:
                report["violations"].append(f"{job}/{name}: unexpected changed output")
            if name == "simulation_data.json":
                old = json.loads(x.read_text())
                full = json.loads((a / "timechunk_full.json").read_text())
                forced = {(s["Playtime"], s["TotalPlaytime"], s["TimeChunkIndex"]) for s in full["TimeChunkSnapshots"]}
                for row in entry["fields"]:
                    import re
                    m = re.fullmatch(r"Snapshots\[(\d+)\]\.(DpsData|DefenseData)\[\d+\]\.Value", row["field"])
                    if not m:
                        report["violations"].append(f"{job}/{name}: unexpected {row['field']}")
                        continue
                    s = old["Snapshots"][int(m[1])]
                    if s["TimeChunkIndex"] > 3 or (s["Playtime"], s["TotalPlaytime"], s["TimeChunkIndex"]) in forced:
                        report["violations"].append(f"{job}/{name}: changed forced/fast snapshot {m[1]}")
            if name == "manifest.json":
                for row in entry["fields"]:
                    if not row["field"].startswith(("run_key", "code_rev", "export.files.", "export.simulation_data_bits_sha256", "export.snapshot_text", "bake.")):
                        report["violations"].append(f"{job}/manifest.json: unexpected {row['field']}")
        report["jobs"][job] = entries
    return report


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("before", type=Path)
    ap.add_argument("after", type=Path)
    ap.add_argument("--out", type=Path, required=True)
    args = ap.parse_args()
    jobs = sorted(p.name for p in args.before.iterdir() if p.is_dir() and (p / "manifest.json").exists())
    report = audit(args.before, args.after, jobs)
    args.out.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n")
    for job, entries in report["jobs"].items():
        print(job, {name: len(e.get("fields", [])) for name, e in entries.items() if e["status"] == "changed"})
    print(f"{len(jobs)} jobs; {len(report['violations'])} violations; {args.out}")
    for row in report["violations"][:20]:
        print(row)
    return bool(report["violations"])


if __name__ == "__main__":
    raise SystemExit(main())
