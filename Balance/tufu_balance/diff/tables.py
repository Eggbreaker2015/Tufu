"""Stable, changed-cell tables (identical inputs always produce identical bytes)."""

from __future__ import annotations

import csv
import json
import math
from pathlib import Path


def text(value):
    if value is None:
        return ""
    if isinstance(value, (dict, list, tuple)):
        return json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":"))
    return repr(value) if isinstance(value, float) else str(value)


def write(path: Path, rows: list[dict], columns):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as f:
        w = csv.DictWriter(f, fieldnames=list(columns), lineterminator="\n", extrasaction="raise")
        w.writeheader()
        for row in sorted(rows, key=lambda r: tuple(_sort(r.get(c)) for c in columns)):
            w.writerow({c: text(row.get(c)) for c in columns})


def _sort(value):
    if value is None:
        return (0, "")
    if isinstance(value, (list, tuple)):
        return (1, tuple(_sort(x) for x in value))
    if type(value) in (int, float):
        return (2, value if not math.isnan(value) else float("inf"))
    return (3, text(value))


CHANGE_COLUMNS = ("key", "field", "base", "patched", "delta", "ratio")


def change(key, field, a, b):
    numeric = type(a) in (int, float) and type(b) in (int, float)
    return {"key": key, "field": field, "base": a, "patched": b,
            "delta": b - a if numeric else None,
            "ratio": b / a if numeric and a != 0 else None}


def same(a, b):
    return a == b or (isinstance(a, float) and isinstance(b, float) and math.isnan(a) and math.isnan(b))


def compare_rows(base, head, keys):
    def index(rows):
        out = {}
        for row in rows:
            key = tuple(row[k] for k in keys)
            if key in out:
                raise ValueError(f"duplicate comparison key: {key}")
            out[key] = row
        return out
    a, b = index(base), index(head)
    changes = []
    for key in sorted(a.keys() | b.keys(), key=text):
        if key not in a or key not in b:
            changes.append(change(key, "row", a.get(key), b.get(key)))
        else:
            for field in sorted(a[key].keys() | b[key].keys()):
                if field not in keys and not same(a[key].get(field), b[key].get(field)):
                    changes.append(change(key, field, a[key].get(field), b[key].get(field)))
    return changes


def read_csv(path):
    if not path.exists():
        return []
    with path.open(encoding="utf-8-sig", newline="") as f:
        return [{k: _number(v) for k, v in row.items()} for row in csv.DictReader(f)]


def _number(value):
    try:
        return int(value)
    except (ValueError, TypeError):
        try:
            return float(value)
        except (ValueError, TypeError):
            return value
