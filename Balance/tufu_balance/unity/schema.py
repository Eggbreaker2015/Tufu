"""Formats of a job's output files (design 1.4.3; unit C1).

Every file a job writes has a schema here; :mod:`.simio` validates a file before it ingests it and an
unknown field (JSON key, CSV column, event type) is an error. Units that add fields or files later
(C3c ``daily``/``events``/``digests``, C8 the bake report, C9 the applied patch, ...) register them with
:func:`register_csv_columns`, :func:`register_event` and :func:`register_manifest_keys` from their own
module; fields are only added, a change of meaning bumps the schema id.

Value types: ``int``, ``float`` (JSON: a float token, CSV: any number text incl. ``NaN``/``Infinity``),
``str``, ``bool01`` (CSV ``0``/``1``), ``bool``, ``sha256`` (64 lower-case hex), ``object``, ``list``,
``any``. A ``?`` suffix allows null (JSON) or an empty cell (CSV).
"""

from __future__ import annotations

import csv
import io
import json
import re
from typing import Any, Iterable, Mapping

EXPORT_SCHEMA = "tufu.sim.export/1"
MANIFEST_SCHEMA = "tufu.sim.manifest/1"


class SchemaError(ValueError):
    """An output file does not match its schema."""


# ---------------------------------------------------------------- SimulationData (isomorphic JSON)

# Serialized fields of CodeBase/Simulation/{SimulationData,SimulationSnapshot,SimulationStatData,
# SimulationItemData,SimulationDpsData}.cs, in declaration order (= the exporter's order).
# tests/test_simio.py checks them against the C# source.
SIMULATION_DATA_LISTS = ("Snapshots", "TimeChunkSnapshots")
SNAPSHOT_FIELDS = (("Playtime", "float32"), ("TotalPlaytime", "float32"), ("Location", "int"), ("TimeChunkIndex", "int"),
                   ("Stats", "SimulationStatData[]"), ("Items", "SimulationItemData[]"),
                   ("DpsData", "SimulationDpsData[]"), ("DefenseData", "SimulationDpsData[]"))
STRUCT_FIELDS = {
    "SimulationSnapshot": SNAPSHOT_FIELDS,
    "SimulationStatData": (("Key", "enum:StatType"), ("Value", "double"), ("StatsFromFeaturesFlat", "double"),
                           ("PercentMultiplier", "double"), ("TotalFlatValue", "double"), ("TotalValue", "double"),
                           ("CurrentGpm", "double")),
    "SimulationItemData": (("Key", "str"), ("Level", "int"), ("Copies", "int")),
    "SimulationDpsData": (("WeaponType", "enum:WeaponType"), ("Value", "double")),
}
FLOAT32_FIELDS = frozenset(f"{owner}.{name}" for owner, fields in STRUCT_FIELDS.items() for name, t in fields
                           if t == "float32")


def validate_simulation_data(data: Any, where: str = "simulation_data.json",
                             lists: Iterable[str] = SIMULATION_DATA_LISTS) -> None:
    """Structure check: only the serialized fields, all present, numbers where numbers belong."""
    allowed = tuple(lists)
    if not isinstance(data, Mapping):
        raise SchemaError(f"{where}: top level must be an object")
    for key in data:
        if key not in allowed:
            raise SchemaError(f"{where}: unknown field {key!r} (allowed {allowed})")
    for key in allowed:
        if key not in data:
            raise SchemaError(f"{where}: missing {key!r}")
        if not isinstance(data[key], list):
            raise SchemaError(f"{where}.{key} must be a list")
        for i, snapshot in enumerate(data[key]):
            _check_struct("SimulationSnapshot", snapshot, f"{where}.{key}[{i}]")


def _check_struct(owner: str, value: Any, where: str) -> None:
    fields = STRUCT_FIELDS[owner]
    if not isinstance(value, Mapping):
        raise SchemaError(f"{where} must be an object")
    names = [n for n, _ in fields]
    for key in value:
        if key not in names:
            raise SchemaError(f"{where}: unknown field {key!r}")
    for name, kind in fields:
        if name not in value:
            raise SchemaError(f"{where}: missing {name!r}")
        item = value[name]
        if kind.endswith("[]"):
            if not isinstance(item, list):
                raise SchemaError(f"{where}.{name} must be a list")
            for j, element in enumerate(item):
                _check_struct(kind[:-2], element, f"{where}.{name}[{j}]")
        elif kind in ("int",) or kind.startswith("enum:"):
            if isinstance(item, bool) or not isinstance(item, int):
                raise SchemaError(f"{where}.{name} must be an integer, got {item!r}")
        elif kind in ("float32", "double"):
            if isinstance(item, bool) or not isinstance(item, (int, float)):
                raise SchemaError(f"{where}.{name} must be a number, got {item!r}")
        elif kind == "str":
            if not isinstance(item, str):
                raise SchemaError(f"{where}.{name} must be a string, got {item!r}")


# ---------------------------------------------------------------- request outputs switches

# job.outputs names (request); the C# defaults are SimulationExport.OutputDefaults. Values are bools.
OUTPUTS = {
    "simulation_data": "C1", "timechunk_full": "C1", "snapshots_ext": "C1", "items_ext": "C1",
    "shipped_simulation_data": "C1",  # the shipped asset through the exporter (round-trip reference)
    "daily": "C3c", "events": "C3c", "digests": "C3c", "spawner_days": "C5", "bake_report": "C8",
}


def validate_outputs(outputs: Mapping[str, Any], where: str = "outputs") -> None:
    for key, value in outputs.items():
        if key not in OUTPUTS:
            raise SchemaError(f"{where}.{key}: unknown output (known: {', '.join(sorted(OUTPUTS))})")
        if not isinstance(value, bool):
            raise SchemaError(f"{where}.{key} must be true or false")


def register_output(name: str, unit: str) -> None:
    OUTPUTS.setdefault(name, unit)


# ---------------------------------------------------------------- CSV side files

CSV_SCHEMAS: dict[str, list[tuple[str, str]]] = {
    # C1: per snapshot of the full lists; exported_index = position in simulation_data.json (empty when
    # trimmed away); the slot/health columns are empty until the engine records the snapshot.
    "snapshots_ext.csv": [("list", "str"), ("index", "int"), ("exported_index", "int?"), ("time_chunk_index", "int"),
                          ("playtime", "float"), ("total_playtime", "float"), ("location", "int"),
                          ("unlocked_weapon_slots", "int?"), ("unlocked_armor_slots", "int?"),
                          ("current_health", "float?")],
    "items_ext.csv": [("list", "str"), ("index", "int"), ("item_index", "int"), ("key", "str"), ("level", "int"),
                      ("copies", "int"), ("evolved", "bool01?"), ("evolution_level", "int?")],
    # C0b: the readiness table (SimulationHarness.Remedy)
    "ready_spawners.csv": [("index", "int"), ("path", "str"), ("key", "str"), ("location", "int"),
                           ("is_event", "bool01"), ("is_vip", "bool01"), ("is_dummy", "bool01"), ("is_elite", "bool01"),
                           ("item_reward", "bool01"), ("anyone_alive", "bool01"), ("is_enabled", "bool01"),
                           ("count", "int"), ("max_count", "int"), ("bootstrap_complete", "bool01"),
                           ("charges_left", "int"), ("charges_total", "int"), ("base_cooldown", "float"),
                           ("cooldown", "float"), ("remedied", "bool01")],
    # C3c (design 1.4.3): one row per day
    "daily.csv": [("day", "int"), ("time_chunk_index", "int"), ("playtime", "float"), ("total_playtime", "float"),
                  ("active_minutes", "float"), ("offline_minutes", "float"), ("kills", "int"), ("clears", "int"),
                  ("fight_time_sum", "float"), ("farmed_time_sum", "float"), ("eligible_count", "int"),
                  ("eligible_signature", "str"), ("location", "int"), ("unlocked_location", "int")],
    # C3c (design 2.6): the daily state digest
    "digests.csv": [("day", "int"), ("time_chunk_index", "int"), ("playtime", "float"), ("total_playtime", "float"),
                    ("sha256", "sha256")],
}

SIDE_TABLES = {"snapshots_ext.csv": "sim_snapshots_ext", "items_ext.csv": "sim_items_ext",
               "ready_spawners.csv": "sim_ready_spawners", "daily.csv": "sim_daily", "digests.csv": "sim_digests"}


def register_csv_columns(file: str, columns: Iterable[tuple[str, str]], table: str | None = None) -> None:
    """Add a CSV file or append columns to one (columns are only added, at the end)."""
    known = CSV_SCHEMAS.setdefault(file, [])
    names = {n for n, _ in known}
    for name, kind in columns:
        if name in names:
            raise ValueError(f"{file}: column {name} already registered")
        _parser(kind)
        known.append((name, kind))
        names.add(name)
    if table:
        SIDE_TABLES[file] = table


_SHA = re.compile(r"[0-9a-f]{64}")
_NUMBER = re.compile(r"-?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?")


def _parser(kind: str):
    base = kind.rstrip("?")

    def parse(text: str):
        if base == "int":
            if not re.fullmatch(r"-?\d+", text):
                raise ValueError("not an integer")
            return int(text)
        if base == "float":
            if text in ("NaN", "Infinity", "-Infinity") or _NUMBER.fullmatch(text):
                return float(text)
            raise ValueError("not a number")
        if base == "bool01":
            if text not in ("0", "1"):
                raise ValueError("not 0/1")
            return int(text)
        if base == "sha256":
            if not _SHA.fullmatch(text):
                raise ValueError("not a sha256")
            return text
        if base == "str":
            return text
        raise ValueError(f"unknown column type {kind}")

    if base not in ("int", "float", "bool01", "sha256", "str"):
        raise ValueError(f"unknown column type {kind}")
    return parse


def read_csv(file: str, text: str) -> list[dict[str, Any]]:
    """Parse and validate a side CSV: the header must be exactly the registered columns, in order."""
    schema = CSV_SCHEMAS.get(file)
    if schema is None:
        raise SchemaError(f"{file}: no schema")
    rows = list(csv.reader(io.StringIO(text)))
    if not rows:
        raise SchemaError(f"{file}: empty (no header)")
    header, expected = rows[0], [n for n, _ in schema]
    if header != expected:
        unknown = [h for h in header if h not in expected]
        missing = [h for h in expected if h not in header]
        raise SchemaError(f"{file}: header differs from the schema" + (f"; unknown {unknown}" if unknown else "")
                          + (f"; missing {missing}" if missing else "") + ("" if unknown or missing else "; order"))
    parsers = [(name, kind, _parser(kind)) for name, kind in schema]
    out = []
    for line, row in enumerate(rows[1:], start=2):
        if len(row) != len(parsers):
            raise SchemaError(f"{file}:{line}: {len(row)} cells, expected {len(parsers)}")
        record = {}
        for (name, kind, parse), text in zip(parsers, row):
            if text == "" and kind.endswith("?"):
                record[name] = None
                continue
            try:
                record[name] = parse(text)
            except ValueError as e:
                raise SchemaError(f"{file}:{line}: {name}={text!r}: {e}") from None
        out.append(record)
    return out


# ---------------------------------------------------------------- events.jsonl

EVENT_COMMON = {"type": "str", "day": "int", "playtime": "float", "total_playtime": "float"}
EVENT_TYPES: dict[str, dict[str, str]] = {
    # spawner = "<hierarchy path>|<Key>" (the C# identity, see spawner_map)
    "first_kill": {"spawner": "str", "location": "int"},
    "unlock": {"location": "int"},
    "slot_unlock": {"slot": "str", "count": "int"},
    "evolve": {"item": "str", "level": "int", "copies": "int"},
    "death": {"spawner": "str", "location": "int"},
}


def register_event(event_type: str, fields: Mapping[str, str]) -> None:
    known = EVENT_TYPES.setdefault(event_type, {})
    for name, kind in fields.items():
        if name in known and known[name] != kind:
            raise ValueError(f"event {event_type}.{name} already registered as {known[name]}")
        known[name] = kind


def _check_value(kind: str, value: Any, where: str) -> None:
    nullable = kind.endswith("?")
    base = kind.rstrip("?")
    if value is None:
        if not nullable:
            raise SchemaError(f"{where} must not be null")
        return
    ok = {
        "int": lambda v: isinstance(v, int) and not isinstance(v, bool),
        "float": lambda v: isinstance(v, float),
        "number": lambda v: isinstance(v, (int, float)) and not isinstance(v, bool),
        "str": lambda v: isinstance(v, str),
        "bool": lambda v: isinstance(v, bool),
        "sha256": lambda v: isinstance(v, str) and bool(_SHA.fullmatch(v)),
        "object": lambda v: isinstance(v, Mapping),
        "list": lambda v: isinstance(v, list),
        "any": lambda v: True,
    }[base](value)
    if not ok:
        raise SchemaError(f"{where} must be {kind}, got {type(value).__name__} {value!r}")


def read_events(text: str, file: str = "events.jsonl") -> list[dict[str, Any]]:
    out = []
    for line_no, line in enumerate(text.splitlines(), start=1):
        if not line.strip():
            continue
        try:
            event = json.loads(line)
        except ValueError as e:
            raise SchemaError(f"{file}:{line_no}: {e}") from None
        if not isinstance(event, dict):
            raise SchemaError(f"{file}:{line_no}: an event must be an object")
        kind = event.get("type")
        if kind not in EVENT_TYPES:
            raise SchemaError(f"{file}:{line_no}: unknown event type {kind!r}")
        fields = {**EVENT_COMMON, **EVENT_TYPES[kind]}
        for key in event:
            if key not in fields:
                raise SchemaError(f"{file}:{line_no}: unknown field {key!r} for {kind}")
        for key, value_kind in fields.items():
            if key not in event:
                raise SchemaError(f"{file}:{line_no}: missing {key!r}")
            _check_value(value_kind, event[key], f"{file}:{line_no}.{key}")
        out.append(event)
    return out


# ---------------------------------------------------------------- manifest.json

# Top-level keys and who writes them. C0b's SimulationHarness writes the core; units add theirs.
MANIFEST_KEYS: dict[str, str] = {
    "schema": "str", "run_key": "str?", "code_rev": "str?", "job_id": "str", "job_index": "int", "mode": "str",
    "request": "object", "job": "object", "unity": "object", "sandbox": "object", "ab": "object", "ready": "object",
    "selfcheck": "object", "config_overrides_applied": "object", "ready_cooldowns_zeroed": "int",
    "state_sha256_job_start": "sha256", "state_sha256_job_end": "sha256", "stop_reason": "str", "counts": "object",
    "exit_code": "int", "error": "str?",
    "export": "object",  # C1
}

EXPORT_KEYS = {"schema": "str", "files": "object", "time_chunk_trim": "str", "snapshots": "int",
               "time_chunk_snapshots_full": "int", "trim_simulation_data": "bool", "recorded_snapshot_ext": "int",
               "problems": "list", "shipped_twice_identical": "bool", "shipped_asset": "str", "shipped_probe": "object"}
EXPORT_FILE_KEYS = {"sha256": "sha256", "bytes": "int", "rows": "int", "doubles": "int", "floats": "int", "nans": "int",
                    "bits_sha256": "sha256"}
EXPORT_BITS_KEYS = ("doubles", "floats", "nans", "bits_sha256")  # required for a .json entry; "rows" for a .csv one
EXPORT_FILES = ("simulation_data.json", "timechunk_full.json", "snapshots_ext.csv", "items_ext.csv",
                "shipped/simulation_data.json", "shipped/snapshots_ext.csv", "shipped/items_ext.csv")
# export.shipped_probe (SimulationExport.ShippedProbe): the side-file writers run on the shipped lists with a
# synthetic engine record and a trimmed TimeChunkSnapshots, so their columns are checked before C3b exists.
SHIPPED_PROBE_KEYS = {"trim_rule": "str", "record_rule": "str", "trimmed": "int", "recorded": "int",
                      "not_subsequence_is_null": "bool", "problems": "list"}


def register_manifest_keys(keys: Mapping[str, str]) -> None:
    for name, kind in keys.items():
        if name in MANIFEST_KEYS and MANIFEST_KEYS[name] != kind:
            raise ValueError(f"manifest key {name} already registered as {MANIFEST_KEYS[name]}")
        MANIFEST_KEYS[name] = kind


def validate_manifest(manifest: Any, where: str = "manifest.json") -> None:
    if not isinstance(manifest, Mapping):
        raise SchemaError(f"{where}: top level must be an object")
    for key, value in manifest.items():
        if key not in MANIFEST_KEYS:
            raise SchemaError(f"{where}: unknown key {key!r}")
        _check_value(MANIFEST_KEYS[key], value, f"{where}.{key}")
    for key in ("schema", "job_id", "exit_code"):
        if key not in manifest:
            raise SchemaError(f"{where}: missing {key!r}")
    if manifest["schema"] != MANIFEST_SCHEMA:
        raise SchemaError(f"{where}.schema must be {MANIFEST_SCHEMA}")
    if "export" in manifest:
        validate_export(manifest["export"], f"{where}.export")


def validate_export(export: Mapping[str, Any], where: str = "export") -> None:
    for key, value in export.items():
        if key not in EXPORT_KEYS:
            raise SchemaError(f"{where}: unknown key {key!r}")
        _check_value(EXPORT_KEYS[key], value, f"{where}.{key}")
    if export.get("schema") != EXPORT_SCHEMA:
        raise SchemaError(f"{where}.schema must be {EXPORT_SCHEMA}")
    for name, info in export.get("files", {}).items():
        if name not in EXPORT_FILES:
            raise SchemaError(f"{where}.files: unknown file {name!r}")
        _check_value("object", info, f"{where}.files.{name}")
        for key, value in info.items():
            if key not in EXPORT_FILE_KEYS:
                raise SchemaError(f"{where}.files.{name}: unknown key {key!r}")
            _check_value(EXPORT_FILE_KEYS[key], value, f"{where}.files.{name}.{key}")
        required = ("sha256", "bytes") + (EXPORT_BITS_KEYS if name.endswith(".json") else ("rows",))
        for key in required:
            if key not in info:
                raise SchemaError(f"{where}.files.{name}: missing {key!r}")
    if "shipped_probe" in export:
        probe = export["shipped_probe"]
        for key, value in probe.items():
            if key not in SHIPPED_PROBE_KEYS:
                raise SchemaError(f"{where}.shipped_probe: unknown key {key!r}")
            _check_value(SHIPPED_PROBE_KEYS[key], value, f"{where}.shipped_probe.{key}")
        for key in SHIPPED_PROBE_KEYS:
            if key not in probe:
                raise SchemaError(f"{where}.shipped_probe: missing {key!r}")
