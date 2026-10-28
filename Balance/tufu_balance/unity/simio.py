"""Ingest a job's outputs into ``<job_dir>/sim.sqlite`` (design 1.4.3; unit C1).

``simulation_data.json`` (written by ``SimulationExport``, isomorphic to the serialized SimulationData)
becomes the same ``c_SimulationData``/``c_SimulationData__*`` tables the extractor builds from the
shipped asset: the rows go through ``extract.add_record_tables`` with the same enum decoder
(``Key__name``, ``WeaponType__name``) and are written with the extractor's cell and column-type rules.
Values are first given the shape the extractor sees in Unity's YAML, so the tables equal
``balance.sqlite``'s for the shipped data:

* a float32 field (``Playtime``, ``TotalPlaytime``) becomes the double of its shortest round-trip
  decimal text, which is what Unity writes and the YAML loader reads;
* an integral finite number (|x| < 1e15) becomes an integer, as Unity writes ``100`` for 100.0;
* NaN stays NaN (stored as the text ``nan``, as the extractor does).

Before that, every float and double is checked bit for bit against the digest the exporter put in
``manifest.json`` (``export.files.<file>.bits_sha256``), so text formatting can never change a value
unnoticed. ``timechunk_full.json`` becomes ``c_SimulationDataFull*`` tables of the same shape; the side
files become ``sim_*`` tables after validation by :mod:`.schema`.

    from tufu_balance.unity import simio
    db = simio.ingest(job_dir)            # -> job_dir / "sim.sqlite"
    data = load_simulation_data(connect(db))
"""

from __future__ import annotations

import hashlib
import json
import math
import sqlite3
import struct
from collections import Counter
from functools import lru_cache
from pathlib import Path
from typing import Any, Iterable, Mapping

from .. import extract
from ..csharp_enums import EnumDecoder, parse_enums
from ..field_types import FieldTypes
from ..model.f32 import f32
from . import schema

SHIPPED_ASSET = "MonoBehaviour/Simulation Data.asset"
SHIPPED_NAME = "Simulation Data"
SIM_DB = "sim.sqlite"
INTEGRAL_LIMIT = 1e15

# file -> (table prefix, class name, lists): tables "<prefix>_<class>" and "<prefix>_<class>__<list>..."
SNAPSHOT_FILES = {
    "simulation_data.json": ("c", "SimulationData", schema.SIMULATION_DATA_LISTS),
    "timechunk_full.json": ("c", "SimulationDataFull", ("TimeChunkSnapshots",)),
}


class SimioError(RuntimeError):
    """A job's outputs cannot be ingested faithfully."""


# ---------------------------------------------------------------- decoding

@lru_cache(maxsize=4)
def field_types(scripts_root: str | None = None) -> FieldTypes:
    """The extractor's enum decoder and field-type table over the C# sources."""
    root = Path(scripts_root) if scripts_root else extract.ASSETS / "Scripts"
    decoder = EnumDecoder(parse_enums(root, exclude=extract.REBUILD_SCRIPT_DIRS))
    return FieldTypes(root, decoder)


def shortest_float32_text(value: float) -> str:
    """Shortest decimal text that reads back as the same float32 (what Unity writes for a float)."""
    x = f32(value)
    for digits in range(1, 10):
        text = f"{x:.{digits}g}"
        if f32(float(text)) == x:
            return text
    raise AssertionError(value)


def _yaml_number(value: Any, kind: str) -> Any:
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        return value
    v = float(value)
    if math.isnan(v) or math.isinf(v):
        return v
    if kind == "float32":
        v = float(shortest_float32_text(v))
    if v == int(v) and abs(v) < INTEGRAL_LIMIT:
        return int(v)
    return v


def yaml_shape(owner: str, value: Any) -> Any:
    """The value as the extractor would have loaded it from Unity's YAML (see the module doc)."""
    if owner == "SimulationData":
        return {k: [yaml_shape("SimulationSnapshot", s) for s in v] for k, v in value.items()}
    out = {}
    for name, kind in schema.STRUCT_FIELDS[owner]:
        item = value[name]
        if kind.endswith("[]"):
            out[name] = [yaml_shape(kind[:-2], x) for x in item]
        elif kind in ("float32", "double"):
            out[name] = _yaml_number(item, kind)
        else:
            out[name] = item
    return out


def bits_digest(data: Mapping[str, Any]) -> dict:
    """Digest of every float32/double in document order, as ``SimulationExport.Bits`` computes it."""
    lines: list[str] = []
    counts = Counter()

    def number(kind: str, v: Any):
        v = float(v)
        tag = "f" if kind == "float32" else "d"
        counts["floats" if tag == "f" else "doubles"] += 1
        if math.isnan(v):
            counts["nans"] += 1
            lines.append(f"{tag}:nan")
        elif tag == "f":  # the text reads back as the nearest float32, as float.Parse does
            lines.append(f"f:{struct.unpack('<I', struct.pack('<f', f32(v)))[0]:08x}")
        else:
            lines.append(f"d:{struct.unpack('<Q', struct.pack('<d', v))[0]:016x}")

    def walk(owner: str, value: Mapping[str, Any]):
        for name, kind in schema.STRUCT_FIELDS[owner]:
            item = value[name]
            if kind.endswith("[]"):
                for x in item:
                    walk(kind[:-2], x)
            elif kind in ("float32", "double"):
                number(kind, item)

    for list_name in data:
        for snapshot in data[list_name]:
            walk("SimulationSnapshot", snapshot)
    text = "".join(line + "\n" for line in lines)
    return {"doubles": counts["doubles"], "floats": counts["floats"], "nans": counts["nans"],
            "bits_sha256": hashlib.sha256(text.encode()).hexdigest()}


def read_snapshot_file(path: Path, lists: Iterable[str] = schema.SIMULATION_DATA_LISTS,
                       expected: Mapping[str, Any] | None = None) -> dict:
    """Parse and validate a snapshot JSON file; with ``expected`` (the manifest's file entry) check its
    sha256 and the bits digest of every number."""
    raw = Path(path).read_bytes()
    data = json.loads(raw.decode("utf-8"))
    schema.validate_simulation_data(data, Path(path).name, lists)
    if expected is not None:
        sha = hashlib.sha256(raw).hexdigest()
        if sha != expected.get("sha256"):
            raise SimioError(f"{path}: sha256 {sha} differs from the manifest's {expected.get('sha256')}")
        digest = bits_digest(data)
        for key in ("doubles", "floats", "nans", "bits_sha256"):
            if key in expected and digest[key] != expected[key]:
                raise SimioError(f"{path}: {key} {digest[key]} differs from the exporter's {expected[key]}: "
                                 "a value did not survive the text round trip")
    return data


# ---------------------------------------------------------------- tables

def snapshot_tables(tables: extract.Tables, data: Mapping[str, Any], *, prefix: str = "c",
                    class_name: str = "SimulationData", asset: str = SHIPPED_ASSET, asset_name: str = SHIPPED_NAME,
                    types: FieldTypes | None = None) -> None:
    """Add the extractor's tables for one SimulationData-shaped object (``extract_assets`` path)."""
    types = types or field_types()
    decoded = types.decode("SimulationData", yaml_shape("SimulationData", data))
    extract.add_record_tables(tables, prefix, class_name, {"asset": asset, "asset_name": asset_name}, decoded, "assets")


def write_tables(tables: extract.Tables, db_path: Path, columns_of: Mapping[str, list[str]] | None = None) -> None:
    """``extract.write_outputs`` without the CSV mirror: same cell values, column types and header rules.
    ``columns_of`` fixes the columns of tables that may have no rows (side files)."""
    db_path = Path(db_path)
    tmp = db_path.with_name(db_path.name + ".tmp")
    tmp.unlink(missing_ok=True)
    con = sqlite3.connect(tmp)
    columns_of = columns_of or {}
    try:
        for table in sorted(set(tables.rows) | set(columns_of)):
            columns = columns_of.get(table) or tables.columns(table)
            rows = [[extract.cell(row.get(c)) for c in columns] for row in tables.rows[table]]
            types = [extract.sql_type([r[i] for r in rows]) for i in range(len(columns))]
            headers, taken = [], Counter()
            for column in columns:
                taken[column.lower()] += 1
                headers.append(column if taken[column.lower()] == 1 else f"{column}#{taken[column.lower()]}")
            quoted = ", ".join(f'"{c}" {t}' for c, t in zip(headers, types))
            con.execute(f'CREATE TABLE "{table}" ({quoted})')
            con.executemany(f'INSERT INTO "{table}" VALUES ({", ".join("?" * len(headers))})', rows)
        con.commit()
    finally:
        con.close()
    tmp.replace(db_path)


def ingest(job_dir: str | Path, db_path: str | Path | None = None, *, asset: str = SHIPPED_ASSET,
           asset_name: str = SHIPPED_NAME, require_manifest: bool = True, types: FieldTypes | None = None) -> Path:
    """Build ``sim.sqlite`` from a job directory and return its path.

    ``asset``/``asset_name`` fill the extractor's key columns of the ``c_SimulationData*`` tables; the
    defaults are the shipped asset's, so tools that select it by name read a simulation the same way.
    """
    job_dir = Path(job_dir)
    db_path = Path(db_path) if db_path else job_dir / SIM_DB
    manifest_path = job_dir / "manifest.json"
    manifest = None
    if manifest_path.exists():
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        schema.validate_manifest(manifest)
    elif require_manifest:
        raise SimioError(f"{job_dir}: no manifest.json")
    files = ((manifest or {}).get("export") or {}).get("files", {})
    job_id = (manifest or {}).get("job_id", job_dir.name)
    types = types or field_types()
    tables = extract.Tables()
    found = []
    for name, (prefix, class_name, lists) in SNAPSHOT_FILES.items():
        path = job_dir / name
        if not path.exists():
            continue
        if manifest is not None and name not in files:
            raise SimioError(f"{path}: not listed in the manifest's export")
        data = read_snapshot_file(path, lists, files.get(name) if manifest is not None else None)
        snapshot_tables(tables, data, prefix=prefix, class_name=class_name, asset=asset, asset_name=asset_name,
                        types=types)
        found.append(name)
    columns_of = {}
    for file, table in schema.SIDE_TABLES.items():
        path = job_dir / file
        if path.exists():
            for row in schema.read_csv(file, path.read_text(encoding="utf-8")):
                tables.add(table, {"job_id": job_id, **row}, "sim")
            columns_of[table] = ["job_id"] + [name for name, _ in schema.CSV_SCHEMAS[file]]
            found.append(file)
    events = job_dir / "events.jsonl"
    if events.exists():
        for e in schema.read_events(events.read_text(encoding="utf-8")):
            common = {k: e[k] for k in schema.EVENT_COMMON}
            rest = {k: v for k, v in e.items() if k not in schema.EVENT_COMMON}
            tables.add("sim_events", {"job_id": job_id, **common, "data": json.dumps(rest, sort_keys=True)}, "sim")
        found.append("events.jsonl")
    if manifest is not None:
        export = manifest.get("export") or {}
        tables.add("sim_manifest", {
            "job_id": job_id, "run_key": manifest.get("run_key"), "code_rev": manifest.get("code_rev"),
            "mode": manifest.get("mode"), "stop_reason": manifest.get("stop_reason"),
            "exit_code": manifest.get("exit_code"), "time_chunk_trim": export.get("time_chunk_trim"),
            "files": json.dumps(sorted(found)), "manifest": json.dumps(manifest, sort_keys=True, allow_nan=True),
        }, "sim")
    if not found and manifest is None:
        raise SimioError(f"{job_dir}: nothing to ingest")
    write_tables(tables, db_path, columns_of)
    return db_path


def ingest_snapshot_file(path: str | Path, db_path: str | Path, *, expected: Mapping[str, Any] | None = None,
                         asset: str = SHIPPED_ASSET, asset_name: str = SHIPPED_NAME,
                         types: FieldTypes | None = None) -> Path:
    """One SimulationData-shaped JSON file (e.g. ``shipped/simulation_data.json``) -> its own database."""
    data = read_snapshot_file(path, schema.SIMULATION_DATA_LISTS, expected)
    tables = extract.Tables()
    snapshot_tables(tables, data, asset=asset, asset_name=asset_name, types=types)
    write_tables(tables, Path(db_path))
    return Path(db_path)


# ---------------------------------------------------------------- comparison with another database

def _cols(con: sqlite3.Connection, table: str) -> list[tuple[str, str]]:
    return [(r[1], r[2]) for r in con.execute(f'PRAGMA table_info("{table}")')]


def _bits(v: float) -> int:
    return struct.unpack("<Q", struct.pack("<d", v))[0]


def compare_tables(a: sqlite3.Connection, b: sqlite3.Connection, prefix: str = "c_SimulationData",
                   ignore_columns: Iterable[str] = ()) -> dict:
    """Compare every table named ``prefix`` or ``prefix__*`` in two databases, row by row in rowid order.

    Columns, declared types, row counts and each value's storage class must match. REAL values must be
    bit-identical, except the float32 fields (``Playtime``, ``TotalPlaytime``), which are compared through
    ``f32`` (their bit-identity is counted separately). NaN is the text ``nan`` in both (same positions).
    """
    ignore = set(ignore_columns)
    float32_columns = {name for _, fields in schema.STRUCT_FIELDS.items() for name, t in fields if t == "float32"}
    names = lambda con: sorted(r[0] for r in con.execute(
        "SELECT name FROM sqlite_master WHERE type = 'table' AND (name = ? OR name LIKE ? ESCAPE '\\')",
        (prefix, prefix.replace("_", "\\_") + "\\_\\_%")))
    ta, tb = names(a), names(b)
    report = {"tables": ta, "missing": sorted(set(ta) ^ set(tb)), "rows": {}, "differences": [],
              "values": 0, "float32_values": 0, "float32_bit_identical": 0, "nan_cells": 0, "name_cells": 0}
    for table in sorted(set(ta) & set(tb)):
        ca, cb = _cols(a, table), _cols(b, table)
        if ca != cb:
            report["differences"].append({"table": table, "columns": [ca, cb]})
            continue
        columns = [c for c, _ in ca if c not in ignore]
        select = ", ".join(f'"{c}", typeof("{c}")' for c in columns)
        ra = a.execute(f'SELECT {select} FROM "{table}" ORDER BY rowid').fetchall()
        rb = b.execute(f'SELECT {select} FROM "{table}" ORDER BY rowid').fetchall()
        report["rows"][table] = len(ra)
        if len(ra) != len(rb):
            report["differences"].append({"table": table, "row_count": [len(ra), len(rb)]})
            continue
        for i, (x, y) in enumerate(zip(ra, rb)):
            for j, column in enumerate(columns):
                va, ka, vb, kb = x[2 * j], x[2 * j + 1], y[2 * j], y[2 * j + 1]
                report["values"] += 1
                if column.endswith("__name"):
                    report["name_cells"] += 1
                if va == "nan" or vb == "nan":
                    report["nan_cells"] += 1
                problem = None
                if ka != kb:
                    problem = f"storage {ka} vs {kb}"
                elif ka == "real" and column in float32_columns:
                    report["float32_values"] += 1
                    if f32(va) != f32(vb):
                        problem = "float32 differs"
                    elif _bits(va) == _bits(vb):
                        report["float32_bit_identical"] += 1
                elif ka == "real":
                    if _bits(va) != _bits(vb):
                        problem = "double bits differ"
                elif va != vb:
                    problem = "value differs"
                if problem:
                    report["differences"].append({"table": table, "row": i, "column": column, "a": va, "b": vb,
                                                  "problem": problem})
    report["equal"] = not report["missing"] and not report["differences"]
    return report


def snapshot_json_from_yaml_asset(path: str | Path) -> dict:
    """The shipped ``Simulation Data.asset`` as SimulationData-shaped JSON (numbers as the YAML has
    them): a Unity-free input for the ingest tests."""
    from ..unity_yaml import iter_documents, payload

    for doc in iter_documents(Path(path)):
        data = payload(extract.load_document(doc))
        return {k: data[k] for k in schema.SIMULATION_DATA_LISTS}
    raise SimioError(f"{path}: no document")
