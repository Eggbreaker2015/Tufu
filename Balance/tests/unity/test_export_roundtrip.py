"""C# exporter -> Python ingest round trip, spawner identity at runtime, readiness gate (design 7.3 C1).

One request with two ``empty`` jobs (each its own Play session) that export the shipped
``Simulation Data`` through ``SimulationExport`` besides their own (empty) snapshot lists.
"""

from __future__ import annotations

import csv
import io
import json
import sqlite3
import struct

import pytest

from tufu_balance import extract
from tufu_balance.golden import check, exact, same_set
from tufu_balance.model.data import connect, load_simulation_data
from tufu_balance.model.f32 import f32
from tufu_balance.unity import request as req, schema, simio, spawner_map

pytestmark = pytest.mark.unity

OUTPUTS = {"simulation_data": True, "timechunk_full": True, "snapshots_ext": True, "items_ext": True,
           "shipped_simulation_data": True}
SHIPPED = "shipped/simulation_data.json"
SHIPPED_SIDE = ("shipped/snapshots_ext.csv", "shipped/items_ext.csv")


@pytest.fixture(scope="module")
def export_run(unity_run):
    request = req.make_request([req.empty_job("export-a", outputs=OUTPUTS), req.empty_job("export-b", outputs=OUTPUTS)])
    result = unity_run(request)
    assert result.exit_code == 0, result.describe()
    return result


def test_shipped_data_round_trips_through_the_csharp_exporter(export_run, db_path, tmp_path):
    m = export_run.manifest("export-a")
    entry = m["export"]["files"][SHIPPED]
    sim = simio.ingest_snapshot_file(export_run.job_dir("export-a") / SHIPPED, tmp_path / "shipped.sqlite", expected=entry)
    a, b = sqlite3.connect(sim), sqlite3.connect(f"file:{db_path}?mode=ro", uri=True)
    report = simio.compare_tables(a, b)
    assert report["equal"], report["differences"][:10]
    assert report["float32_bit_identical"] == report["float32_values"]
    assert repr(load_simulation_data(connect(sim))) == repr(load_simulation_data(connect(db_path)))
    # Every float and double the editor holds equals the value in the asset's YAML, bit for bit.
    shipped_yaml = simio.snapshot_json_from_yaml_asset(extract.ASSETS / "MonoBehaviour" / "Simulation Data.asset")
    assert {k: entry[k] for k in ("doubles", "floats", "nans", "bits_sha256")} == simio.bits_digest(shipped_yaml)
    check("export", {
        "csharp_roundtrip.equal": exact(report["equal"], source="design 7.3 C1", original=True),
        "csharp_roundtrip.values": exact(report["values"], source="design 7.3 C1"),
        "csharp_roundtrip.nan_cells": exact(report["nan_cells"], source="G19", original=60),
        "csharp_roundtrip.float32_bit_identical": exact(report["float32_bit_identical"], source="design 7.3 C1"),
        "csharp.shipped_sha256": exact(entry["sha256"], source="design 7.3 C1",
                                       note="the exporter's bytes for the shipped asset"),
    })


def test_export_is_byte_identical_twice(export_run):
    a, b = export_run.manifest("export-a"), export_run.manifest("export-b")
    assert a["export"]["shipped_twice_identical"] and b["export"]["shipped_twice_identical"]
    for name in (SHIPPED, *SHIPPED_SIDE, "simulation_data.json", "timechunk_full.json", "snapshots_ext.csv", "items_ext.csv"):
        assert export_run.file("export-a", name) == export_run.file("export-b", name), name
    assert a["export"] == b["export"]


def test_job_outputs_ingest_and_validate(export_run):
    job = export_run.job_dir("export-a")
    m = export_run.manifest("export-a")
    schema.validate_manifest(m)
    export = m["export"]
    assert set(export["files"]) == {"simulation_data.json", "timechunk_full.json", "snapshots_ext.csv", "items_ext.csv",
                                    SHIPPED, *SHIPPED_SIDE}
    assert export["snapshots"] == 0 and export["time_chunk_snapshots_full"] == 0 and export["problems"] == []
    assert export["trim_simulation_data"] is False
    assert export["time_chunk_trim"].startswith("none")  # C3b fills GetTrimmedTimeChunkSnapshots
    con = sqlite3.connect(simio.ingest(job))
    tables = {r[0] for r in con.execute("SELECT name FROM sqlite_master WHERE type = 'table'")}
    assert {"c_SimulationData", "c_SimulationDataFull", "sim_snapshots_ext", "sim_items_ext", "sim_ready_spawners",
            "sim_manifest"} <= tables
    assert con.execute("SELECT count(*) FROM sim_ready_spawners").fetchone() == (716,)
    assert con.execute("SELECT job_id, exit_code FROM sim_manifest").fetchone() == ("export-a", 0)


def _probe_expectation(db):
    """The rows SimulationExport.WriteShippedProbe must write, from balance.sqlite and the probe rules."""
    snaps, items = [], []
    for name, period, gap in (("Snapshots", 4, 3), ("TimeChunkSnapshots", 5, 2)):
        base = f"c_SimulationData__{name}"
        inv = {}
        for r in db.execute(f'SELECT "{name}#idx", "Items#idx", Key, Level, Copies FROM "{base}__Items" ORDER BY 1, 2'):
            inv.setdefault(r[0], []).append(tuple(r[2:]))
        kept = 0
        for i, tci, playtime, total, location in db.execute(
                f'SELECT "{name}#idx", TimeChunkIndex, Playtime, TotalPlaytime, Location FROM "{base}" ORDER BY 1').fetchall():
            if name == "Snapshots":
                exported = i
            elif i % 3 != 1:
                exported, kept = kept, kept + 1
            else:
                exported = None
            recorded = i % period != gap
            snaps.append({"list": name, "index": i, "exported_index": exported, "time_chunk_index": tci,
                          "playtime": f32(playtime), "total_playtime": f32(total), "location": location,
                          "unlocked_weapon_slots": i % 6 if recorded else None,
                          "unlocked_armor_slots": location if recorded else None,
                          "current_health": float(f32(total)) / 3.0 if recorded else None})
            full = recorded and not (name == "Snapshots" and i == 1)
            for k, (key, level, copies) in enumerate(inv.get(i, [])):
                items.append({"list": name, "index": i, "item_index": k, "key": key, "level": level, "copies": copies,
                              "evolved": int((i + k) % 2 == 0) if full else None,
                              "evolution_level": level * 10 + copies if full else None})
    return snaps, items


def test_shipped_side_files_follow_the_probe(export_run, db):
    """The side-file writers and ExportedIndices on the shipped lists, with a trimmed TimeChunkSnapshots and a
    synthetic engine record (rules in SimulationExport.WriteShippedProbe), compared row by row."""
    job = export_run.job_dir("export-a")
    export = export_run.manifest("export-a")["export"]
    probe = export["shipped_probe"]
    snaps = schema.read_csv("snapshots_ext.csv", (job / SHIPPED_SIDE[0]).read_text(encoding="utf-8"))
    items = schema.read_csv("items_ext.csv", (job / SHIPPED_SIDE[1]).read_text(encoding="utf-8"))
    want_snaps, want_items = _probe_expectation(db)
    assert len(snaps) == len(want_snaps) == export["files"][SHIPPED_SIDE[0]]["rows"]
    assert len(items) == len(want_items) == export["files"][SHIPPED_SIDE[1]]["rows"]
    for got, want in zip(snaps, want_snaps):
        got = {**got, "playtime": f32(got["playtime"]), "total_playtime": f32(got["total_playtime"])}
        health = got["current_health"]
        assert {**got, "current_health": None} == {**want, "current_health": None}, want
        assert (health is None) == (want["current_health"] is None), want
        if health is not None:
            assert struct.pack("<d", health) == struct.pack("<d", want["current_health"]), want
    for got, want in zip(items, want_items):
        assert got == want
    assert probe["trimmed"] == sum(1 for s in want_snaps if s["list"] == "TimeChunkSnapshots" and s["exported_index"] is not None)
    assert probe["recorded"] == sum(1 for s in want_snaps if s["unlocked_weapon_slots"] is not None)
    assert probe["not_subsequence_is_null"] is True
    assert probe["problems"] == ["Snapshots[1]: recorded items do not match the snapshot's Items"]
    check("export", {
        "csharp.shipped_probe_rows": exact([len(snaps), len(items)], source="design 7.3 C1",
                                           note="snapshots_ext and items_ext rows of the shipped lists"),
        "csharp.shipped_probe_sha256": exact([export["files"][f]["sha256"] for f in SHIPPED_SIDE], source="design 7.3 C1",
                                             note="the side-file writers' bytes on the shipped lists"),
    })


def test_runtime_spawners_match_the_identity_map(export_run, db):
    rows = list(csv.DictReader(io.StringIO(export_run.file("export-a", "ready_spawners.csv").decode())))
    identity = spawner_map.load_identity_map(db)
    cmp = identity.compare_runtime((r["path"], r["key"]) for r in rows)
    assert cmp["runtime"] == len(rows) == len(identity.registered())
    assert cmp["unknown"] == cmp["runtime_duplicates"] == cmp["registered_but_predicted_not"] == []
    assert cmp["predicted_but_not_registered"] == []
    check("export", {
        "identity.runtime_registered": exact(cmp["runtime"], source="Runbook 6.1", original=716),
        "identity.runtime_difference": same_set([[e.scene, e.file_id, e.runtime_id] for e in identity.unregistered()],
                                                source="Runbook 6.1", note="table rows that never register"),
    })


def test_readiness_gate_uses_p6s_table(export_run):
    """sandbox.expected_ready_spawners = "auto": P6's expected_ready_spawners('A') by path|Key, all 716 equal."""
    for job in ("export-a", "export-b"):
        table = export_run.manifest(job)["ready"]["expected_table"]
        assert table["checked"] and table["mismatched"] == 0 and table["details"] == []
    name = export_run.request["sandbox"]["expected_ready_spawners"]
    assert name.startswith("ready_spawners.expected.") and (export_run.run_dir / name).exists()
    assert table["path"] == name  # as the request names it: manifests stay comparable across run directories
    check("export", {
        "ready_gate.compared": exact(table["compared"], source="DP6, Runbook 6.1", original=716),
        "ready_gate.mismatched": exact(table["mismatched"], source="DP6", original=0),
    })


def test_unknown_output_name_is_refused_before_launch():
    request = req.make_request([req.empty_job("x", outputs={"bogus": True})], out_dir="/x")
    with pytest.raises(req.RequestError, match="unknown output"):
        req.validate(request)
