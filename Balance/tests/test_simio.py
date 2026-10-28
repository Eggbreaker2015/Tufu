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


def _job(tmp_path: Path, data: dict, text: str | None = None) -> Path:
    job = tmp_path / "job"
    job.mkdir()
    body = (text if text is not None else json.dumps(data, allow_nan=True)).encode()
    (job / "simulation_data.json").write_bytes(body)
    entry = {"sha256": hashlib.sha256(body).hexdigest(), "bytes": len(body), **simio.bits_digest(data)}
    (job / "manifest.json").write_text(json.dumps(_manifest({"simulation_data.json": entry})))
    return job


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
    assert simio._yaml_number(-0.0, "double") == 0
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
    manifest["export"]["files"]["simulation_data.json"]["sha256"] = hashlib.sha256(changed.encode()).hexdigest()
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
        (lambda m: m.update(exit_code="0"), "must be int"),
    ]:
        bad = json.loads(json.dumps(m))
        mutate(bad)
        with pytest.raises(schema.SchemaError, match=message):
            schema.validate_manifest(bad)


def test_ingest_of_side_files(shipped_json, tmp_path):
    job = _job(tmp_path, {"Snapshots": [], "TimeChunkSnapshots": []})
    (job / "items_ext.csv").write_text("list,index,item_index,key,level,copies,evolved,evolution_level\n"
                                       "Snapshots,0,0,knife,1,1,0,0\n")
    (job / "snapshots_ext.csv").write_text("list,index,exported_index,time_chunk_index,playtime,total_playtime,location,"
                                           "unlocked_weapon_slots,unlocked_armor_slots,current_health\n")
    (job / "events.jsonl").write_text('{"type": "unlock", "day": 5, "playtime": 310.5, "total_playtime": 7200.0, '
                                      '"location": 4}\n')
    con = sqlite3.connect(simio.ingest(job))
    assert con.execute("SELECT key, level, evolved FROM sim_items_ext").fetchall() == [("knife", 1, 0)]
    assert con.execute("SELECT count(*) FROM sim_snapshots_ext").fetchone() == (0,)
    assert [c[1] for c in con.execute("PRAGMA table_info(sim_snapshots_ext)")][:3] == ["job_id", "list", "index"]
    assert con.execute("SELECT type, day, data FROM sim_events").fetchall() == [("unlock", 5, '{"location": 4}')]
    assert json.loads(con.execute("SELECT files FROM sim_manifest").fetchone()[0]) == [
        "events.jsonl", "items_ext.csv", "simulation_data.json", "snapshots_ext.csv"]
    # an empty list is a JSON column, as the extractor stores it (no child tables)
    assert con.execute("SELECT Snapshots, TimeChunkSnapshots FROM c_SimulationData").fetchone() == ("[]", "[]")


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
