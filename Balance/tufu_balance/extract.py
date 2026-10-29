"""Export the game's numeric data (configs, scene spawners, simulation output) to SQLite and CSV.

Usage (from the repository root):
    Balance/.venv/bin/python -m tufu_balance.extract            # run from Balance/
"""

import argparse
import csv
import dataclasses
import json
import math
import re
import shutil
import sqlite3
import time
from collections import Counter, defaultdict
from pathlib import Path

from .csharp_enums import EnumDecoder, parse_enums
from .field_types import FieldTypes
from .unity_yaml import (
    CLASS_GAME_OBJECT, CLASS_MONO_BEHAVIOUR, CLASS_RECT_TRANSFORM, CLASS_TRANSFORM,
    GuidIndex, flatten, iter_documents, payload, resolve_refs, to_json,
)

REPO = Path(__file__).resolve().parents[2]
ASSETS = REPO / "Recovery" / "StudyProject" / "Assets"
OUT = REPO / "Balance" / "out"
SCENES = ["Scenes/Main.unity", "Scenes/Boot.unity"]
# The stage 5 simulator rebuild adds these directories; their types are tooling, not game data.
REBUILD_SCRIPT_DIRS = ["Assembly-CSharp/CodeBase/Simulation/Harness", "Assembly-CSharp/CodeBase/Simulation/Rebuild"]
SPAWNER_CLASS = "EnemySpawner"


def is_rebuild_script(script_path):
    """True for a script (path relative to Assets) under one of REBUILD_SCRIPT_DIRS."""
    return bool(script_path) and any(script_path.startswith(f"Scripts/{d}/") for d in REBUILD_SCRIPT_DIRS)


class Tables:
    """Row store: table -> list of dicts, written with the union of keys as columns."""

    def __init__(self):
        self.rows = defaultdict(list)
        self.groups = {}

    def add(self, table, row, group="raw"):
        self.rows[table].append(row)
        self.groups.setdefault(table, group)

    def columns(self, table):
        seen = {}
        for row in self.rows[table]:
            for key in row:
                seen.setdefault(key, None)
        return list(seen)


def cell(value):
    if isinstance(value, (list, dict)):
        return to_json(value)
    if isinstance(value, float) and math.isnan(value):
        return str(value)  # SQLite would store NaN as NULL; infinities stay REAL so numeric columns keep their type
    if isinstance(value, int) and not -2**63 <= value < 2**63:
        return str(value)  # serialize-reference ids and hashes exceed SQLite INTEGER
    return value


def sql_type(values):
    kinds = {type(v) for v in values if v is not None}
    if str in kinds and kinds <= {str, int} and all(isinstance(v, int) or v.lstrip("-").isdigit()
                                                   for v in values if v is not None):
        return "TEXT"  # mixed huge ids: keep as text
    if kinds and kinds <= {int, bool}:
        return "INTEGER"
    if kinds and kinds <= {int, float, bool}:
        return "REAL"
    return "TEXT"


def table_name(*parts):
    return "__".join(re.sub(r"[^\w.]", "_", str(p)) for p in parts)


# ---------------------------------------------------------------- document loading

# UnitySerializedDictionary<int, T> writes its List<int> keys (and int values) as one hex blob of
# little-endian int32s. A blob made only of digits (keys 1..4 -> 01000000020000000300000004000000)
# would resolve as a YAML float, so keyData/valueData scalars are quoted before loading. Float values
# are a YAML sequence and are not affected.
KEY_DATA_SCALAR = re.compile(r"^([ \t-]*(?:keyData|valueData):[ \t]+)([0-9A-Fa-f]+)[ \t]*$", re.M)


def load_document(doc):
    """``Document.load`` with ``keyData``/``valueData`` hex blobs kept as text."""
    if "Data:" in doc.text:
        doc = dataclasses.replace(doc, text=KEY_DATA_SCALAR.sub(r"\1'\2'", doc.text))
    return doc.load()


# ---------------------------------------------------------------- generic records

def split_record(record):
    """Scalars and nested dicts become dotted columns; lists of dicts are returned as children;
    other lists stay as JSON values."""
    columns, children = {}, {}

    def walk(value, prefix):
        for key, item in value.items():
            name = f"{prefix}.{key}" if prefix else key
            if isinstance(item, dict):
                walk(item, name)
            elif isinstance(item, list) and item and all(isinstance(x, dict) for x in item):
                children[name] = item
                columns[f"{name}#count"] = len(item)
            else:
                columns[name] = item

    walk(record, "")
    return columns, children


def add_record_tables(tables, prefix, class_name, keys, record, group, depth=2):
    """One row in <prefix>_<Class>, and one row per element of each list-of-dicts field
    in <prefix>_<Class>__<field> (recursively up to depth)."""
    columns, children = split_record(record)
    tables.add(f"{prefix}_{class_name}", {**keys, **columns}, group)
    if depth <= 0:
        return
    for field, items in children.items():
        child_table = table_name(f"{prefix}_{class_name}", field)
        for i, item in enumerate(items):
            child_keys = {**keys, f"{field}#idx": i}
            child_columns, grandchildren = split_record(item)
            tables.add(child_table, {**child_keys, **child_columns}, group)
            for sub, sub_items in grandchildren.items() if depth > 1 else ():
                for j, sub_item in enumerate(sub_items):
                    sub_columns, _ = split_record(sub_item)
                    tables.add(table_name(child_table, sub),
                               {**child_keys, f"{sub}#idx": j, **sub_columns}, group)


def add_serialize_references(tables, owner, data, types):
    references = data.get("references") if isinstance(data, dict) else None
    if not isinstance(references, dict):
        return
    for entry in references.get("RefIds") or []:
        type_info = entry.get("type") or {}
        class_name = type_info.get("class")
        tables.add("serialize_references", {
            "owner": owner,
            "rid": str(entry.get("rid")),
            "class": class_name,
            "namespace": type_info.get("ns"),
            "data": types.decode(class_name, entry.get("data")) if class_name else entry.get("data"),
        })


def add_values(tables, owner, class_name, data):
    for path, value in flatten(data):
        row = {"owner": owner, "class": class_name, "field_path": path,
               "value_num": value if isinstance(value, (int, float)) and not isinstance(value, bool) else None,
               "value_text": None if isinstance(value, (int, float)) else value}
        tables.add("config_values", row)
        match = re.fullmatch(r"(.*)\[(\d+)\]", path)
        if match and row["value_num"] is not None:
            tables.add("config_arrays", {"owner": owner, "class": class_name, "field_path": match.group(1),
                                         "idx": int(match.group(2)), "value": value})


# ---------------------------------------------------------------- ScriptableObject configs

def extract_assets(tables, index, types, unresolved):
    count = 0
    for path in sorted(ASSETS.rglob("*.asset")):
        with open(path, encoding="utf-8", errors="replace") as handle:
            head = handle.read(4096)
        if "--- !u!114 " not in head or "m_Script:" not in head:
            continue
        rel = path.relative_to(ASSETS).as_posix()
        for doc in iter_documents(path):
            if doc.class_id != CLASS_MONO_BEHAVIOUR or not index.is_game_script(doc.script_guid()):
                continue
            if is_rebuild_script(index.path(doc.script_guid())):
                continue
            class_name = index.script_class(doc.script_guid())
            raw = load_document(doc)
            name = raw.get("m_Name")
            data = resolve_refs(payload(raw), index, unresolved)
            decoded = types.decode(class_name, data)
            owner = rel if doc.file_id == 11400000 else f"{rel}#{doc.file_id}"
            tables.add("assets", {"owner": owner, "name": name, "class": class_name, "data": decoded})
            add_values(tables, owner, class_name, data)
            add_serialize_references(tables, owner, decoded, types)
            body = {k: v for k, v in decoded.items() if k != "references"}
            add_record_tables(tables, "c", class_name, {"asset": owner, "asset_name": name}, body, "assets")
            count += 1
    return count


# ---------------------------------------------------------------- scenes and prefabs

def quat_mul(a, b):
    ax, ay, az, aw = a
    bx, by, bz, bw = b
    return (aw * bx + ax * bw + ay * bz - az * by,
            aw * by - ax * bz + ay * bw + az * bx,
            aw * bz + ax * by - ay * bx + az * bw,
            aw * bw - ax * bx - ay * by - az * bz)


def quat_rotate(q, v):
    x, y, z, w = q
    vx, vy, vz = v
    tx, ty, tz = 2 * (y * vz - z * vy), 2 * (z * vx - x * vz), 2 * (x * vy - y * vx)
    return (vx + w * tx + (y * tz - z * ty), vy + w * ty + (z * tx - x * tz), vz + w * tz + (x * ty - y * tx))


class Hierarchy:
    def __init__(self):
        self.objects = {}     # GameObject fileID -> {name, active}
        self.transforms = {}  # Transform fileID -> {go, father, pos, rot, scale}
        self.go_transform = {}
        self._world = {}

    def add(self, doc):
        data = doc.load()
        if doc.class_id == CLASS_GAME_OBJECT:
            self.objects[doc.file_id] = {"name": data.get("m_Name"), "active": data.get("m_IsActive")}
        else:
            vec = lambda v, d: tuple(float((v or {}).get(k, d)) for k in "xyz")
            rot = data.get("m_LocalRotation") or {}
            go = (data.get("m_GameObject") or {}).get("fileID")
            self.transforms[doc.file_id] = {
                "go": go,
                "father": (data.get("m_Father") or {}).get("fileID", 0),
                "pos": vec(data.get("m_LocalPosition"), 0),
                "rot": tuple(float(rot.get(k, 1 if k == "w" else 0)) for k in "xyzw"),
                "scale": vec(data.get("m_LocalScale"), 1),
            }
            self.go_transform[go] = doc.file_id

    def world(self, transform_id):
        if transform_id in self._world:
            return self._world[transform_id]
        node = self.transforms.get(transform_id)
        if node is None:
            return (0.0, 0.0, 0.0), (0.0, 0.0, 0.0, 1.0), (1.0, 1.0, 1.0)
        if node["father"] in self.transforms:
            p_pos, p_rot, p_scale = self.world(node["father"])
            scaled = tuple(a * b for a, b in zip(p_scale, node["pos"]))
            pos = tuple(a + b for a, b in zip(p_pos, quat_rotate(p_rot, scaled)))
            result = pos, quat_mul(p_rot, node["rot"]), tuple(a * b for a, b in zip(p_scale, node["scale"]))
        else:
            result = node["pos"], node["rot"], node["scale"]
        self._world[transform_id] = result
        return result

    def path(self, go_id):
        names, transform_id, guard = [], self.go_transform.get(go_id), 0
        while transform_id in self.transforms and guard < 256:
            node = self.transforms[transform_id]
            names.append((self.objects.get(node["go"]) or {}).get("name") or "?")
            transform_id, guard = node["father"], guard + 1
        return "/".join(reversed(names))

    def active_in_hierarchy(self, go_id):
        transform_id, guard = self.go_transform.get(go_id), 0
        while transform_id in self.transforms and guard < 256:
            node = self.transforms[transform_id]
            if (self.objects.get(node["go"]) or {}).get("active") == 0:
                return 0
            transform_id, guard = node["father"], guard + 1
        return 1


def read_hierarchy_file(path, index, wanted):
    """Return (hierarchy, [(doc, class_name)]) for game-script MonoBehaviours in a scene/prefab."""
    hierarchy, behaviours = Hierarchy(), []
    for doc in iter_documents(path):
        if doc.class_id in (CLASS_GAME_OBJECT, CLASS_TRANSFORM, CLASS_RECT_TRANSFORM):
            hierarchy.add(doc)
        elif doc.class_id == CLASS_MONO_BEHAVIOUR:
            guid = doc.script_guid()
            if index.is_game_script(guid) and wanted(doc):
                behaviours.append((doc, index.script_class(guid)))
    return hierarchy, behaviours


def has_payload(doc):
    return "m_EditorClassIdentifier:" in doc.text and not doc.text.rstrip().endswith("m_EditorClassIdentifier:")


def extract_scene(tables, scene, index, types, unresolved, spawner_rows):
    hierarchy, behaviours = read_hierarchy_file(ASSETS / scene, index, has_payload)
    for doc, class_name in behaviours:
        raw = load_document(doc)
        go = (raw.get("m_GameObject") or {}).get("fileID")
        data = types.decode(class_name, resolve_refs(payload(raw), index, unresolved))
        go_path = hierarchy.path(go)
        owner = f"{scene}#{doc.file_id}"
        keys = {"scene": scene, "file_id": doc.file_id, "go_path": go_path}
        tables.add("scene_components", {**keys, "class": class_name, "enabled": raw.get("m_Enabled"),
                                        "active": hierarchy.active_in_hierarchy(go), "data": data})
        add_serialize_references(tables, owner, data, types)
        add_record_tables(tables, "s", class_name, keys, {k: v for k, v in data.items() if k != "references"},
                          "scene", depth=1)
        if class_name == SPAWNER_CLASS:
            pos, _, _ = hierarchy.world(hierarchy.go_transform.get(go))
            spawner_rows.append({"scene": scene, "file_id": doc.file_id, "go_path": go_path,
                                 "go_name": (hierarchy.objects.get(go) or {}).get("name"),
                                 "enabled": raw.get("m_Enabled"), "active": hierarchy.active_in_hierarchy(go),
                                 "world_x": round(pos[0], 3), "world_y": round(pos[1], 3),
                                 "world_z": round(pos[2], 3), "data": data})
    return len(behaviours)


def extract_prefabs(tables, index, types, unresolved):
    count = 0
    for path in sorted(ASSETS.rglob("*.prefab")):
        rel = path.relative_to(ASSETS).as_posix()
        hierarchy, behaviours = read_hierarchy_file(path, index, has_payload)
        for doc, class_name in behaviours:
            raw = load_document(doc)
            go = (raw.get("m_GameObject") or {}).get("fileID")
            data = types.decode(class_name, resolve_refs(payload(raw), index, unresolved))
            keys = {"prefab": rel, "file_id": doc.file_id, "go_path": hierarchy.path(go)}
            tables.add("prefab_components", {**keys, "class": class_name, "data": data})
            add_serialize_references(tables, f"{rel}#{doc.file_id}", data, types)
            add_record_tables(tables, "p", class_name, keys, {k: v for k, v in data.items() if k != "references"},
                              "prefabs", depth=1)
            count += 1
    return count


# ---------------------------------------------------------------- core normalized tables

def short_asset(value):
    return Path(value.split("#")[0]).stem if isinstance(value, str) and value else value


def build_spawners(tables, spawner_rows, decoder, bake_by_key):
    stat_name = lambda v: decoder.name("StatType", v)
    for row in spawner_rows:
        config = row["data"].get("config") or {}
        key = config.get("Key")
        out = {k: row[k] for k in ("scene", "file_id", "go_name", "go_path", "enabled", "active",
                                   "world_x", "world_y", "world_z")}
        for field, value in config.items():
            if field in ("Stats", "StatRewards", "Gates", "EventDef"):
                continue
            if field in ("Prefab", "ArmorConfig", "ItemReward"):
                value = short_asset(value)
            out[field] = value
        event = config.get("EventDef") or {}
        out["EventDef.Key"] = event.get("Key")
        out["EventDef.UnitType"] = event.get("UnitType")
        out["EventDef.UnitType__name"] = event.get("UnitType__name")
        out["EventDef.Variants#count"] = len(event.get("Variants") or [])
        out["Gates#count"] = len(config.get("Gates") or [])
        for stat in config.get("Stats") or []:
            out[f"stat.{stat_name(stat.get('Type'))}"] = stat.get("BaseValue")
            tables.add("spawner_stats", {"spawner_key": key, "location": config.get("LocationNumber"),
                                         "stat_type": stat.get("Type"), "stat_name": stat_name(stat.get("Type")),
                                         "base_value": stat.get("BaseValue")}, "core")
        rewards = []
        for i, reward in enumerate(config.get("StatRewards") or []):
            rewards.append(f"{stat_name(reward.get('StatType'))}:{reward.get('ModifierType__name')}:{reward.get('Value')}")
            tables.add("spawner_rewards", {"spawner_key": key, "location": config.get("LocationNumber"), "idx": i,
                                           **{k: v for k, v in reward.items()}}, "core")
        out["StatRewards.summary"] = "; ".join(rewards)
        bake = bake_by_key.get(key)
        for field in ("OptimumTime", "Health", "Damage", "PrevHealth", "PrevDamage"):
            out[f"bake.{field}"] = bake.get(field) if bake else None
        tables.add("spawners", out, "core")


def build_items(tables):
    for row in tables.rows.get("c_ItemConfig", []):
        tables.add("items", {k: v for k, v in row.items() if not k.startswith("AbilityDefs")}, "core")


KEY_COLUMNS = {"asset", "asset_name", "scene", "file_id", "go_path", "prefab"}
ORIGINS = {"c": "asset", "s": "scene", "p": "prefab"}


def dictionary_node(table, column):
    """Config field id used by the formula catalog and dependency graph, e.g.
    'CombatBalanceConfig.EvolutionDamageValues[].Value' or 'EnemySpawnerConfig.Stats[Health]'."""
    if table == "spawners":
        if column.startswith("stat."):
            return f"EnemySpawnerConfig.Stats[{column[5:]}]"
        if column.startswith("bake.") or column in ("go_name", "enabled", "active", "world_x", "world_y", "world_z"):
            return None
        return f"EnemySpawnerConfig.{column}"
    if table == "spawner_rewards":
        return None if column in ("spawner_key", "location", "idx") else f"EnemySpawnerConfig.StatRewards[].{column}"
    prefix, _, rest = table.partition("_")
    if prefix not in ORIGINS or not rest:
        return None
    class_name, *fields = rest.split("__")
    if class_name == SPAWNER_CLASS and prefix == "s":
        return None  # covered by the normalized spawners table
    path = "".join(f".{f}[]" for f in fields)
    return f"{class_name}{path}.{column}"


def build_dictionary(tables):
    """One row per config field across asset, scene and prefab tables, with value statistics."""
    entries = {}
    for table in sorted(tables.rows):
        rows = tables.rows[table]
        for column in tables.columns(table):
            if column in KEY_COLUMNS or "#" in column or column.endswith("__name"):
                continue
            node = dictionary_node(table, column)
            if node is None:
                continue
            values = [r.get(column) for r in rows if r.get(column) is not None]
            numbers = [v for v in values if isinstance(v, (int, float)) and not isinstance(v, bool)]
            entry = entries.setdefault(node, {"node": node, "class": node.split(".")[0], "tables": [],
                                              "origin": ORIGINS.get(table.split("_")[0], "core"),
                                              "rows": 0, "values": [], "numbers": []})
            entry["tables"].append(table)
            entry["rows"] += len(rows)
            entry["values"] += values
            entry["numbers"] += numbers
    for node in sorted(entries):
        e = entries.pop(node)
        distinct = list(dict.fromkeys(cell(v) for v in e["values"]))
        tables.add("data_dictionary", {
            "node": node, "class": e["class"], "origin": e["origin"], "tables": ", ".join(e["tables"]),
            "rows": e["rows"], "non_null": len(e["values"]), "distinct": len(distinct),
            "min": min(e["numbers"]) if e["numbers"] else None, "max": max(e["numbers"]) if e["numbers"] else None,
            "sample": "; ".join(str(v)[:60] for v in distinct[:4]),
        })


# ---------------------------------------------------------------- output

def write_outputs(tables, out_dir):
    if out_dir.exists():
        shutil.rmtree(out_dir)
    (out_dir / "csv").mkdir(parents=True)
    database = sqlite3.connect(out_dir / "balance.sqlite")
    for table in sorted(tables.rows):
        columns = tables.columns(table)
        rows = [[cell(row.get(c)) for c in columns] for row in tables.rows[table]]
        types = [sql_type([r[i] for r in rows]) for i in range(len(columns))]
        headers, taken = [], Counter()
        for column in columns:  # SQLite column names are case-insensitive
            taken[column.lower()] += 1
            headers.append(column if taken[column.lower()] == 1 else f"{column}#{taken[column.lower()]}")
        columns = headers
        quoted = ", ".join(f'"{c}" {t}' for c, t in zip(columns, types))
        database.execute(f'CREATE TABLE "{table}" ({quoted})')
        database.executemany(f'INSERT INTO "{table}" VALUES ({", ".join("?" * len(columns))})', rows)
        group_dir = out_dir / "csv" / tables.groups[table]
        group_dir.mkdir(exist_ok=True)
        with open(group_dir / f"{table}.csv", "w", newline="", encoding="utf-8-sig") as handle:
            writer = csv.writer(handle)
            writer.writerow(columns)
            writer.writerows(rows)
    database.commit()
    database.close()


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--out", type=Path, default=OUT)
    parser.add_argument("--skip-prefabs", action="store_true")
    args = parser.parse_args()

    started = time.monotonic()
    index = GuidIndex(ASSETS)
    enum_rows = parse_enums(ASSETS / "Scripts", exclude=REBUILD_SCRIPT_DIRS)
    decoder = EnumDecoder(enum_rows)
    types = FieldTypes(ASSETS / "Scripts", decoder)
    tables, unresolved = Tables(), set()

    for enum, namespace, member, value, source in enum_rows:
        tables.add("enums", {"enum": enum, "namespace": namespace, "member": member, "value": value, "file": source})

    asset_count = extract_assets(tables, index, types, unresolved)
    print(f"assets: {asset_count} ScriptableObjects")

    spawner_rows, scene_counts = [], {}
    for scene in SCENES:
        scene_counts[scene] = extract_scene(tables, scene, index, types, unresolved, spawner_rows)
        print(f"{scene}: {scene_counts[scene]} components")

    prefab_count = 0 if args.skip_prefabs else extract_prefabs(tables, index, types, unresolved)
    print(f"prefabs: {prefab_count} components")

    bake_rows = tables.rows.get("c_SimulationConfig__SpawnerBakeInfos", [])
    bake_by_key = {row["Key"]: row for row in bake_rows}
    build_spawners(tables, spawner_rows, decoder, bake_by_key)
    build_items(tables)
    build_dictionary(tables)

    report = make_report(tables, unresolved, bake_by_key, asset_count, scene_counts, prefab_count)
    write_outputs(tables, args.out)
    (args.out / "extract_report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(report["checks"], ensure_ascii=False, indent=2))
    print(f"wrote {len(tables.rows)} tables to {args.out} in {time.monotonic() - started:.1f}s")


def make_report(tables, unresolved, bake_by_key, asset_count, scene_counts, prefab_count):
    spawners = tables.rows.get("spawners", [])
    keys = Counter(row.get("Key") for row in spawners)
    spawner_keys = set(keys)
    unknown_stats = sorted({r["stat_name"] for r in tables.rows.get("spawner_stats", []) if str(r["stat_name"]).startswith("?")})
    health_match = sum(1 for r in spawners if r.get("bake.Health") and r.get("stat.Health") is not None
                       and abs(r["stat.Health"] - r["bake.Health"]) <= 0.01 * abs(r["bake.Health"]))
    with_bake = sum(1 for r in spawners if r.get("bake.Health") is not None)
    return {
        "inputs": {"assets_root": str(ASSETS), "scriptable_objects": asset_count,
                   "scene_components": scene_counts, "prefab_components": prefab_count},
        "tables": {name: len(rows) for name, rows in sorted(tables.rows.items())},
        "checks": {
            "spawners": len(spawners),
            "spawners_by_location": dict(sorted(Counter(r.get("LocationNumber") for r in spawners).items(),
                                                key=lambda kv: (kv[0] is None, kv[0] or 0))),
            "duplicate_spawner_keys": sorted(k for k, n in keys.items() if n > 1),
            "bake_infos": len(bake_by_key),
            "spawners_with_bake": with_bake,
            "spawners_with_bake_where_health_within_1pct_of_bake": health_match,
            "bake_keys_without_spawner": sorted(set(bake_by_key) - spawner_keys),
            "unknown_stat_types_in_spawners": unknown_stats,
            "unresolved_guid_count": len(unresolved),
            "unresolved_guid_sample": sorted(unresolved)[:10],
        },
    }


if __name__ == "__main__":
    main()
