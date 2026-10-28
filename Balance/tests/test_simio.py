"""Ingest of job outputs, side-file schemas and the spawner identity map (design 1.4.3, 1.5 item 5; unit C1).

Without Unity: the shipped ``Simulation Data.asset`` read from its YAML stands in for the exporter's
JSON, so the ingest itself is checked against ``balance.sqlite``. The same comparison on the C#
exporter's output runs in tests/unity/test_export_roundtrip.py.
"""

from __future__ import annotations

import csv
import hashlib
import io
import json
import re
import sqlite3
import struct
from pathlib import Path

import pytest

from tufu_balance import extract
from tufu_balance.golden import check, exact, same_set
from tufu_balance.model.data import connect, load_simulation_data
from tufu_balance.unity import launcher, request as req, schema, simio, spawner_map

SHIPPED_YAML = extract.ASSETS / "MonoBehaviour" / "Simulation Data.asset"


@pytest.fixture(scope="module")
def shipped_json() -> dict:
    return simio.snapshot_json_from_yaml_asset(SHIPPED_YAML)


def _bits(v: float) -> str:
    return f"{struct.unpack('<Q', struct.pack('<d', v))[0]:016x}"


def _manifest(files: dict) -> dict:
    return {"schema": schema.MANIFEST_SCHEMA, "job_id": "j", "exit_code": 0, "run_key": None, "code_rev": None,
            "export": {"schema": schema.EXPORT_SCHEMA, "files": files}}


def _job(tmp_path: Path, data: dict, text: str | None = None, name: str = "job") -> Path:
    job = tmp_path / name
    job.mkdir()
    body = (text if text is not None else json.dumps(data, allow_nan=True)).encode()
    (job / "simulation_data.json").write_bytes(body)
    entry = {"sha256": hashlib.sha256(body).hexdigest(), "bytes": len(body), **simio.bits_digest(data)}
    (job / "manifest.json").write_text(json.dumps(_manifest({"simulation_data.json": entry})))
    return job


def _add_csv(job: Path, name: str, text: str, rows: int) -> None:
    """Write a side CSV and list it in the manifest's export, as SimulationExport does."""
    (job / name).write_text(text)
    manifest = json.loads((job / "manifest.json").read_text())
    body = text.encode()
    manifest["export"]["files"][name] = {"sha256": hashlib.sha256(body).hexdigest(), "bytes": len(body), "rows": rows}
    (job / "manifest.json").write_text(json.dumps(manifest))


def _edit_manifest(job: Path, edit) -> None:
    manifest = json.loads((job / "manifest.json").read_text())
    edit(manifest)
    (job / "manifest.json").write_text(json.dumps(manifest))


# ---------------------------------------------------------------- schema of the isomorphic JSON

def test_struct_fields_match_the_csharp_declarations():
    ft = simio.field_types()
    csharp = {"float": "float32", "double": "double", "int": "int", "string": "str", "StatType": "enum:StatType",
              "WeaponType": "enum:WeaponType"}
    for owner, fields in schema.STRUCT_FIELDS.items():
        declared = ft.fields[owner]
        assert [n for n, _ in fields] == list(declared), owner
        for name, kind in fields:
            t = declared[name]
            assert kind == (f"{t[5:-1]}[]" if t.startswith("List<") else csharp[t]), (owner, name, t)
    assert list(ft.fields["SimulationData"])[:2] == list(schema.SIMULATION_DATA_LISTS)
    assert schema.FLOAT32_FIELDS == {"SimulationSnapshot.Playtime", "SimulationSnapshot.TotalPlaytime"}


def test_yaml_shape_of_numbers():
    assert simio.shortest_float32_text(1.16560864) == "1.1656086"
    assert simio.shortest_float32_text(0.1) == "0.1"
    assert simio._yaml_number(259200.0, "double") == 259200 and isinstance(simio._yaml_number(259200.0, "double"), int)
    assert simio._yaml_number(-0.0, "double") == 0 and isinstance(simio._yaml_number(-0.0, "double"), int)  # as the extractor reads "-0"
    assert simio._yaml_number(1e20, "double") == 1e20 and isinstance(simio._yaml_number(1e20, "double"), float)
    assert simio._yaml_number(float("nan"), "double") != simio._yaml_number(float("nan"), "double")
    assert simio._yaml_number(201660.97, "float32") == 201660.97  # float32 text from Unity's YAML is kept


# ---------------------------------------------------------------- shipped data through the ingest

def test_ingest_of_the_shipped_data_equals_the_extracted_tables(shipped_json, db_path, tmp_path):
    job = _job(tmp_path, shipped_json)
    sim = simio.ingest(job)
    a, b = sqlite3.connect(sim), sqlite3.connect(f"file:{db_path}?mode=ro", uri=True)
    report = simio.compare_tables(a, b)
    assert report["equal"], report["differences"][:10]
    assert report["float32_bit_identical"] == report["float32_values"]
    assert repr(load_simulation_data(connect(sim))) == repr(load_simulation_data(connect(db_path)))
    tables = {r[0] for r in a.execute("SELECT name FROM sqlite_master WHERE type = 'table'")}
    assert {"sim_manifest"} <= tables and not any(t.startswith("sim_items") for t in tables)
    check("export", {
        "yaml_roundtrip.equal": exact(report["equal"], source="design 7.3 C1", original=True),
        "yaml_roundtrip.rows": exact(report["rows"], source="design 7.3 C1"),
        "yaml_roundtrip.values": exact(report["values"], source="design 7.3 C1"),
        "yaml_roundtrip.nan_cells": exact(report["nan_cells"], source="G19", original=60),
        "yaml_roundtrip.name_cells": exact(report["name_cells"], source="design 7.3 C1"),
        "shipped.bits": exact(simio.bits_digest(shipped_json), source="design 7.3 C1",
                              note="every float/double of the shipped asset; the C# export must reproduce it"),
    })


def test_ingest_rejects_a_value_changed_in_the_text(shipped_json, tmp_path):
    text = json.dumps(shipped_json, allow_nan=True)
    job = _job(tmp_path, shipped_json, text)
    first = next(st["Value"] for st in shipped_json["TimeChunkSnapshots"][5]["Stats"]
                 if isinstance(st["Value"], float) and st["Value"] != int(st["Value"]))
    after = struct.unpack("<d", struct.pack("<Q", int(_bits(first), 16) + 1))[0]
    needle = f'"Value": {json.dumps(first)}'
    assert text.count(needle) >= 1
    changed = text.replace(needle, f'"Value": {json.dumps(after)}', 1)
    (job / "simulation_data.json").write_text(changed)
    manifest = json.loads((job / "manifest.json").read_text())
    manifest["export"]["files"]["simulation_data.json"].update(sha256=hashlib.sha256(changed.encode()).hexdigest(),
                                                                bytes=len(changed.encode()))
    (job / "manifest.json").write_text(json.dumps(manifest))
    with pytest.raises(simio.SimioError, match="bits_sha256"):
        simio.ingest(job)


def test_ingest_rejects_a_file_not_in_the_manifest(shipped_json, tmp_path):
    job = _job(tmp_path, shipped_json)
    (job / "timechunk_full.json").write_text(json.dumps({"TimeChunkSnapshots": []}))
    with pytest.raises(simio.SimioError, match="not listed"):
        simio.ingest(job)


@pytest.mark.parametrize("mutate, message", [
    (lambda d: d.update(Extra=[]), "unknown field 'Extra'"),
    (lambda d: d["Snapshots"][0].update(Gold=1.0), "unknown field 'Gold'"),
    (lambda d: d["Snapshots"][0]["Stats"][0].update(Note="x"), "unknown field 'Note'"),
    (lambda d: d["TimeChunkSnapshots"][0]["Items"][0].pop("Copies"), "missing 'Copies'"),
    (lambda d: d["TimeChunkSnapshots"][0]["DpsData"][0].update(WeaponType=1.5), "must be an integer"),
])
def test_unknown_or_missing_fields_are_errors(shipped_json, mutate, message):
    data = json.loads(json.dumps(shipped_json, allow_nan=True))
    mutate(data)
    with pytest.raises(schema.SchemaError, match=message):
        schema.validate_simulation_data(data)


# ---------------------------------------------------------------- side files

def test_side_csv_schemas(tmp_path):
    snaps = schema.read_csv("snapshots_ext.csv", "list,index,exported_index,time_chunk_index,playtime,total_playtime,location,"
                            "unlocked_weapon_slots,unlocked_armor_slots,current_health\n"
                            "TimeChunkSnapshots,3,,3,63.28,4320,2,2,1,NaN\nSnapshots,0,0,0,0,0,1,,,\n")
    assert snaps[0]["exported_index"] is None and snaps[0]["current_health"] != snaps[0]["current_health"]
    assert snaps[1]["unlocked_weapon_slots"] is None and snaps[0]["total_playtime"] == 4320.0
    items = schema.read_csv("items_ext.csv", "list,index,item_index,key,level,copies,evolved,evolution_level\n"
                            "Snapshots,0,0,knife,1,1,0,0\nSnapshots,1,0,\"a,b\",2,3,,\n")
    assert items[1]["key"] == "a,b" and items[1]["evolved"] is None
    with pytest.raises(schema.SchemaError, match="unknown \\['bogus'\\]"):
        schema.read_csv("items_ext.csv", "list,index,item_index,key,level,copies,evolved,evolution_level,bogus\n")
    with pytest.raises(schema.SchemaError, match="missing"):
        schema.read_csv("digests.csv", "day,time_chunk_index,playtime,total_playtime\n")
    with pytest.raises(schema.SchemaError, match="evolved='2'"):
        schema.read_csv("items_ext.csv", "list,index,item_index,key,level,copies,evolved,evolution_level\n"
                        "Snapshots,0,0,knife,1,1,2,0\n")
    with pytest.raises(schema.SchemaError, match="playtime='inf'"):
        schema.read_csv("snapshots_ext.csv", "list,index,exported_index,time_chunk_index,playtime,total_playtime,location,"
                        "unlocked_weapon_slots,unlocked_armor_slots,current_health\nSnapshots,0,0,0,inf,0,1,,,\n")


def test_events_schema():
    ok = ('{"type": "unlock", "day": 5, "playtime": 310.5, "total_playtime": 7200.0, "location": 4}\n'
          '{"type": "first_kill", "day": 0, "playtime": 1.0, "total_playtime": 1.0, "spawner": "Main:a/b|k", "location": 1}\n')
    assert [e["type"] for e in schema.read_events(ok)] == ["unlock", "first_kill"]
    for line, message in [
        ('{"type": "teleport", "day": 1, "playtime": 1.0, "total_playtime": 1.0}', "unknown event type"),
        ('{"type": "unlock", "day": 1, "playtime": 1.0, "total_playtime": 1.0, "location": 2, "x": 1}', "unknown field 'x'"),
        ('{"type": "unlock", "day": 1, "playtime": 1, "total_playtime": 1.0, "location": 2}', "must be float"),
        ('{"type": "unlock", "day": 1, "playtime": 1.0, "total_playtime": 1.0}', "missing 'location'"),
    ]:
        with pytest.raises(schema.SchemaError, match=message):
            schema.read_events(line)


def test_manifest_schema():
    m = _manifest({"simulation_data.json": {"sha256": "0" * 64, "bytes": 3, "doubles": 0, "floats": 0, "nans": 0,
                                            "bits_sha256": "1" * 64}})
    schema.validate_manifest(m)
    for mutate, message in [
        (lambda m: m.update(bogus=1), "unknown key 'bogus'"),
        (lambda m: m["export"].update(bogus=1), "unknown key 'bogus'"),
        (lambda m: m["export"]["files"].update({"other.json": {"sha256": "0" * 64, "bytes": 1}}), "unknown file"),
        (lambda m: m["export"]["files"]["simulation_data.json"].pop("sha256"), "missing 'sha256'"),
        (lambda m: m["export"]["files"]["simulation_data.json"].pop("bits_sha256"), "missing 'bits_sha256'"),
        (lambda m: m["export"]["files"].update({"items_ext.csv": {"sha256": "0" * 64, "bytes": 1}}), "missing 'rows'"),
        (lambda m: m["export"].update(shipped_probe={"trim_rule": "x"}), "shipped_probe: missing"),
        (lambda m: m.update(exit_code="0"), "must be int"),
    ]:
        bad = json.loads(json.dumps(m))
        mutate(bad)
        with pytest.raises(schema.SchemaError, match=message):
            schema.validate_manifest(bad)


def test_ingest_of_side_files(shipped_json, tmp_path):
    job = _job(tmp_path, {"Snapshots": [], "TimeChunkSnapshots": []})
    _add_csv(job, "items_ext.csv", "list,index,item_index,key,level,copies,evolved,evolution_level\n"
                                   "Snapshots,0,0,knife,1,1,0,0\n", rows=1)
    _add_csv(job, "snapshots_ext.csv", "list,index,exported_index,time_chunk_index,playtime,total_playtime,location,"
                                       "unlocked_weapon_slots,unlocked_armor_slots,current_health\n", rows=0)
    (job / "events.jsonl").write_text('{"type": "unlock", "day": 5, "playtime": 310.5, "total_playtime": 7200.0, '
                                      '"location": 4}\n')
    con = sqlite3.connect(simio.ingest(job))
    assert con.execute("SELECT key, level, evolved FROM sim_items_ext").fetchall() == [("knife", 1, 0)]
    assert con.execute("SELECT count(*) FROM sim_snapshots_ext").fetchone() == (0,)
    assert [c[1] for c in con.execute("PRAGMA table_info(sim_snapshots_ext)")][:3] == ["job_id", "list", "index"]
    assert con.execute("SELECT type, day, data FROM sim_events").fetchall() == [("unlock", 5, '{"location": 4}')]
    assert json.loads(con.execute("SELECT files FROM sim_manifest").fetchone()[0]) == [
        "events.jsonl", "items_ext.csv", "simulation_data.json", "snapshots_ext.csv"]
    # an empty list is a JSON column, as the extractor stores it; its tables exist without rows
    assert con.execute("SELECT Snapshots, TimeChunkSnapshots FROM c_SimulationData").fetchone() == ("[]", "[]")
    assert con.execute("SELECT count(*) FROM c_SimulationData__TimeChunkSnapshots__Items").fetchone() == (0,)


# ---------------------------------------------------------------- request outputs

def test_request_outputs_are_validated():
    r = req.make_request([req.make_job("j", outputs={"simulation_data": True, "shipped_simulation_data": False})],
                         out_dir="/x")
    req.validate(r)
    r["jobs"][0]["outputs"]["bogus"] = True
    with pytest.raises(req.RequestError, match="unknown output"):
        req.validate(r)
    r["jobs"][0]["outputs"] = {"daily": 1}
    with pytest.raises(req.RequestError, match="true or false"):
        req.validate(r)


def test_output_names_match_the_csharp_defaults():
    cs = (extract.ASSETS / "Scripts/Assembly-CSharp/CodeBase/Simulation/Harness/SimulationExport.cs").read_text()
    block = cs[cs.index("OutputDefaults"):cs.index("};", cs.index("OutputDefaults"))]
    names = re.findall(r'\["(\w+)"\]', block)
    assert names and set(names) == {k for k, unit in schema.OUTPUTS.items() if unit == "C1"}


# ---------------------------------------------------------------- spawner identity map

def test_spawner_identity_map(db):
    m = spawner_map.load_identity_map(db)
    registered, unregistered = m.registered(), m.unregistered()
    assert len(m.by_runtime_id) == len(m) == len(m.by_spawner_id)
    keys = [e.key for e in m.entries]
    assert len(set(keys)) == len(keys) - 1  # Key alone is not unique; path + Key is
    e = registered[0]
    assert m.resolve(e.path, e.key) == e.spawner_id and e.path == "Main:" + e.go_path
    check("export", {
        "identity.entries": exact(len(m), source="design 3.4", original=717),
        "identity.registered": exact(len(registered), source="design 3.4, Runbook 6.1", original=716),
        "identity.unregistered": same_set([[e.scene, e.file_id, e.runtime_id] for e in unregistered],
                                          source="Runbook 6.1", note="the inactive duplicate Eventspawner_Boss"),
        "identity.csv_sha256": exact(hashlib.sha256(m.to_csv().encode()).hexdigest(), source="design 1.5 item 5"),
    })


def test_expected_ready_table_from_p6(db, db_path, monkeypatch):
    from tufu_balance.model.spawners import expected_ready_spawners

    text = spawner_map.expected_ready_csv(db, "A")
    rows = list(csv.DictReader(io.StringIO(text)))
    ready = expected_ready_spawners("A", con=db)
    m = spawner_map.load_identity_map(db)
    assert len(rows) == len(ready) == len(m.registered())
    for r in rows:
        sid = m.resolve(r["path"], r["key"])
        assert sid == (r["scene"], int(r["file_id"]))
        assert int(r["charges_total"]) == ready[sid].charges_total
        assert float(r["base_cooldown"]) == ready[sid].base_cooldown
        assert struct.pack("<f", float(r["base_cooldown"])) == struct.pack("<f", ready[sid].base_cooldown)
    monkeypatch.setenv("TUFU_BALANCE_DB", str(db_path))
    name, again = launcher.expected_ready_table("A")
    assert again == text and name == spawner_map.expected_ready_file_name(text)
    assert name.startswith("ready_spawners.expected.") and name.endswith(".csv")


def test_auto_readiness_table_needs_the_database(tmp_path, monkeypatch):
    monkeypatch.setenv("TUFU_BALANCE_DB", str(tmp_path / "missing.sqlite"))
    with pytest.raises(launcher.LauncherError, match="needs the extracted database") as e:
        launcher.expected_ready_table("A")
    assert e.value.code == launcher.EXIT_BAD_REQUEST
    assert req.default_sandbox()["expected_ready_spawners"] == req.EXPECTED_READY_AUTO


# ---------------------------------------------------------------- the comparer (the round-trip oracle)

def _shipped_copy(db_path: Path) -> sqlite3.Connection:
    """The shipped c_SimulationData* tables, same declarations and row order, in a private database."""
    con = sqlite3.connect("file::memory:", uri=True)
    con.execute("ATTACH DATABASE ? AS src", (f"file:{db_path}?mode=ro",))
    for name, sql in con.execute("SELECT name, sql FROM src.sqlite_master WHERE type = 'table' AND "
                                 "(name = 'c_SimulationData' OR name LIKE 'c\\_SimulationData\\_\\_%' ESCAPE '\\')").fetchall():
        con.execute(sql)
        con.execute(f'INSERT INTO main."{name}" SELECT * FROM src."{name}" ORDER BY rowid')
    con.commit()
    con.execute("DETACH DATABASE src")
    return con


def _next_double(v: float) -> float:
    return struct.unpack("<d", struct.pack("<Q", struct.unpack("<Q", struct.pack("<d", v))[0] + 1))[0]


def _next_float32(v: float) -> float:
    return struct.unpack("<f", struct.pack("<I", struct.unpack("<I", struct.pack("<f", v))[0] + 1))[0]


def _faults(con: sqlite3.Connection) -> dict:
    """Each fault: (plant(con), the differences compare_tables must report as (table, row, column, problem))."""
    stats, snaps = "c_SimulationData__TimeChunkSnapshots__Stats", "c_SimulationData__Snapshots"
    one = lambda sql, *a: con.execute(sql, a).fetchone()
    rid_d, v_d = one(f'SELECT rowid, Value FROM "{stats}" WHERE typeof(Value) = \'real\' AND Value != round(Value) '
                     'ORDER BY rowid LIMIT 1')
    # a small playtime, where one float32 ulp is far below 1e-3
    rid_f, v_f = one(f'SELECT rowid, Playtime FROM "{snaps}" WHERE Playtime BETWEEN 1 AND 100 ORDER BY rowid LIMIT 1')
    gpm = "c_SimulationData__Snapshots__Stats"
    rid_nan = one(f'SELECT rowid FROM "{gpm}" WHERE CurrentGpm = \'nan\' ORDER BY rowid LIMIT 1')[0]
    rid_num, v_num = one(f'SELECT rowid, CurrentGpm FROM "{gpm}" WHERE CurrentGpm != \'nan\' ORDER BY rowid LIMIT 1')
    dps = "c_SimulationData__TimeChunkSnapshots__DpsData"
    n_dps = one(f'SELECT count(*) FROM "{dps}"')[0]
    return {
        "double_one_ulp": (lambda c: c.execute(f'UPDATE "{stats}" SET Value = ? WHERE rowid = ?', (_next_double(v_d), rid_d)),
                           [(stats, rid_d - 1, "Value", "double bits differ")]),
        "float32_one_ulp": (lambda c: c.execute(f'UPDATE "{snaps}" SET Playtime = ? WHERE rowid = ?',
                                                (_next_float32(v_f), rid_f)),
                            [(snaps, rid_f - 1, "Playtime", "float32 differs")]),
        "key_name": (lambda c: c.execute(f'UPDATE "{stats}" SET Key__name = \'Bogus\' WHERE rowid = 3'),
                     [(stats, 2, "Key__name", "value differs")]),
        "asset_name": (lambda c: c.execute(f'UPDATE "{snaps}" SET asset_name = \'Other\' WHERE rowid = 4'),
                       [(snaps, 3, "asset_name", "value differs")]),
        "nan_moved": (lambda c: c.executemany(f'UPDATE "{gpm}" SET CurrentGpm = ? WHERE rowid = ?',
                                              [(v_num, rid_nan), ("nan", rid_num)]),
                      sorted([(gpm, rid_nan - 1, "CurrentGpm", "value differs"),
                              (gpm, rid_num - 1, "CurrentGpm", "value differs")], key=lambda d: d[1])),
        "row_dropped": (lambda c: c.execute(f'DELETE FROM "{dps}" WHERE rowid = 7'),
                        [(dps, None, None, "row count differs")]),
    }


def test_compare_tables_reports_each_planted_fault(db_path):
    base = _shipped_copy(db_path)
    original = sqlite3.connect(f"file:{db_path}?mode=ro", uri=True)
    assert simio.compare_tables(base, original)["equal"]
    for name, (plant, expected) in _faults(base).items():
        planted = sqlite3.connect(":memory:")
        base.backup(planted)
        plant(planted)
        planted.commit()
        report = simio.compare_tables(planted, original)
        got = [(d["table"], d.get("row"), d.get("column"), d["problem"]) for d in report["differences"]]
        assert not report["equal"] and got == expected, name


def test_compare_tables_checks_the_storage_class(tmp_path):
    """Untyped columns keep what was stored: an INTEGER 1 is not the REAL 1.0 even though they compare equal."""
    a, b = sqlite3.connect(":memory:"), sqlite3.connect(":memory:")
    for con, value in ((a, 1), (b, 1.0)):
        con.execute('CREATE TABLE "c_SimulationData" (x)')
        con.execute('INSERT INTO "c_SimulationData" VALUES (?)', (value,))
    report = simio.compare_tables(a, b)
    assert [d["problem"] for d in report["differences"]] == ["storage integer vs real"]


# ---------------------------------------------------------------- float32 text as the C# exporter writes it

def test_ingest_reads_float32_written_as_g9(shipped_json, db_path, tmp_path):
    """SimulationJson.JsonFloat writes G9 (63.2799988), Unity's YAML the shortest text (63.28)."""
    assert simio._yaml_number(63.2799988, "float32") == 63.28
    data = json.loads(json.dumps(shipped_json, allow_nan=True))
    longer = 0
    for snapshots in data.values():
        for s in snapshots:
            for name in ("Playtime", "TotalPlaytime"):
                g9 = float(f"{simio.f32(s[name]):.9g}")
                longer += g9 != s[name]
                s[name] = g9
    assert longer > 0
    sim = simio.ingest(_job(tmp_path, data))
    report = simio.compare_tables(sqlite3.connect(sim), sqlite3.connect(f"file:{db_path}?mode=ro", uri=True))
    assert report["equal"], report["differences"][:5]
    assert report["float32_values"] > 0 and report["float32_bit_identical"] == report["float32_values"]


# ---------------------------------------------------------------- empty lists

@pytest.mark.parametrize("empty", ["Snapshots", "TimeChunkSnapshots", "both", "every_items"])
def test_empty_lists_load_as_empty(shipped_json, db, db_path, tmp_path, empty):
    data = json.loads(json.dumps(shipped_json, allow_nan=True))
    for name in ("Snapshots", "TimeChunkSnapshots"):
        if empty in (name, "both"):
            data[name] = []
        if empty == "every_items":
            for s in data[name]:
                s["Items"] = []
    sim = simio.ingest(_job(tmp_path, data))
    got = load_simulation_data(connect(sim))
    want = load_simulation_data(db)
    for name, attr in (("Snapshots", "snapshots"), ("TimeChunkSnapshots", "time_chunk_snapshots")):
        if empty in (name, "both"):
            assert getattr(got, attr) == []
        elif empty == "every_items":
            assert all(s.items == [] for s in getattr(got, attr))
            assert [repr(s.stats) for s in getattr(got, attr)] == [repr(s.stats) for s in getattr(want, attr)]
        else:
            assert repr(getattr(got, attr)) == repr(getattr(want, attr))
    # the tables made for empty lists have the extractor's columns
    con = sqlite3.connect(sim)
    made = [t for t in simio.snapshot_table_layout() if con.execute(f'SELECT count(*) FROM "{t}"').fetchone() == (0,)]
    assert made
    for table in made:
        cols = lambda c: [r[1] for r in c.execute(f'PRAGMA table_info("{table}")')]
        assert cols(con) == cols(db), table


# ---------------------------------------------------------------- integrity of a job directory

def test_ingest_checks_every_listed_file(shipped_json, tmp_path):
    items = "list,index,item_index,key,level,copies,evolved,evolution_level\nSnapshots,0,0,knife,1,1,0,0\n"
    cases = {
        "missing": (lambda job: _edit_manifest(job, lambda m: m["export"]["files"].update(
            {"timechunk_full.json": {"sha256": "0" * 64, "bytes": 1, "doubles": 0, "floats": 0, "nans": 0,
                                     "bits_sha256": "0" * 64}})), "listed in the manifest's export but missing"),
        "snapshot_sha": (lambda job: _edit_manifest(job, lambda m: m["export"]["files"]["simulation_data.json"].update(
            sha256="0" * 64)), "sha256"),
        "csv_rows": (lambda job: _add_csv(job, "items_ext.csv", items, rows=2), "1 rows, the manifest says 2"),
        "csv_sha": (lambda job: (_add_csv(job, "items_ext.csv", items, rows=1),
                                 (job / "items_ext.csv").write_text(items.replace("knife", "knifE"))), "sha256"),
        "csv_unlisted": (lambda job: (job / "snapshots_ext.csv").write_text("x\n"), "not listed"),
    }
    for name, (spoil, message) in cases.items():
        job = _job(tmp_path, shipped_json, name=name)
        spoil(job)
        with pytest.raises(simio.SimioError, match=message):
            simio.ingest(job)


def test_bits_check_cannot_be_skipped(shipped_json, tmp_path):
    job = _job(tmp_path, shipped_json)
    entry = json.loads((job / "manifest.json").read_text())["export"]["files"]["simulation_data.json"]
    with pytest.raises(simio.SimioError, match="sha256"):
        simio.read_snapshot_file(job / "simulation_data.json", expected={**entry, "sha256": "0" * 64})
    entry.pop("bits_sha256")
    with pytest.raises(simio.SimioError, match="the bits check cannot run"):
        simio.read_snapshot_file(job / "simulation_data.json", expected=entry)


def test_csv_cells_and_header_order():
    header = "list,index,item_index,key,level,copies,evolved,evolution_level\n"
    with pytest.raises(schema.SchemaError, match="level=''"):
        schema.read_csv("items_ext.csv", header + "Snapshots,0,0,knife,,1,,\n")
    swapped = ("list,index,exported_index,time_chunk_index,total_playtime,playtime,location,"
               "unlocked_weapon_slots,unlocked_armor_slots,current_health\n")
    with pytest.raises(schema.SchemaError, match="order"):
        schema.read_csv("snapshots_ext.csv", swapped)


def test_launcher_refuses_a_stale_readiness_table(db_path, tmp_path, monkeypatch):
    monkeypatch.setenv("TUFU_BALANCE_DB", str(db_path))
    r = {"sandbox": {"expected_ready_spawners": "ready_spawners.expected.0000000000000000.csv", "ab_variant": "A"}}
    with pytest.raises(launcher.LauncherError, match="changed since the request was prepared"):
        launcher.write_side_inputs(r, tmp_path)
    assert not (tmp_path / r["sandbox"]["expected_ready_spawners"]).exists()


def test_expected_table_refuses_a_spawner_that_never_registers(db):
    from tufu_balance.model.spawners import expected_ready_spawners

    m = spawner_map.load_identity_map(db)
    ready = dict(expected_ready_spawners("A", con=db))
    ready[m.unregistered()[0].spawner_id] = next(iter(ready.values()))
    with pytest.raises(ValueError, match="does not register at runtime"):
        spawner_map.expected_ready_csv(db, "A", ready=ready, identity=m)
