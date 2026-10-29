"""P12 designer workflow: locality, repeatability, validation and read-only asset expansion."""

from dataclasses import replace
import csv
import json
from pathlib import Path
import time

import pytest

from tufu_balance.diff import compare, impact, load, patchfile, report, run, tables
from tufu_balance.diff.__main__ import main
from tufu_balance.model import config as C, patch as P

EXAMPLES = Path(__file__).resolve().parents[1] / "patches/examples"


@pytest.fixture(scope="module")
def cfg(db_path):
    return C.load_game_config(db_path)


@pytest.fixture(scope="module")
def island(cfg):
    return run(cfg, load(EXAMPLES / "island10_later.yaml"), skip_sim=True)


def test_empty_patch_has_no_changed_cells(cfg, tmp_path):
    c = run(cfg, patchfile.empty(), skip_sim=True)
    assert not c.changed and not any(c.comparisons.values())
    path = report.write(c, tmp_path)
    assert "无变化" in path.read_text()
    for name in c.comparisons:
        assert tables.read_csv(tmp_path / name) == []
    assert not c.formulas


def test_island10_local_ot_and_windows(island):
    from tufu_balance.model.timeline import relative_window, optimum_time_from_relative
    base = {r["spawner"]: r for r in island.static.base.tables["optimum_time"]}
    moved = {c["key"][0] for c in island.static.diff["optimum_time"] if c["field"] == "stored"}
    # The expected set comes from the shipped window/relative data, independently of patch expansion.
    itd = island.static.config.value("CombatBalanceConfig.IslandTargetDays")
    expected = set()
    for sid, r in base.items():
        if r["location"] in (10, 11) and not r["exception"]:
            lo, hi, days = relative_window(itd, r["location"])
            if float(optimum_time_from_relative(r["relative"], lo, hi, days)) != r["stored"]:
                expected.add(sid)
    assert moved == expected
    assert {base[s]["location"] for s in moved} == {10, 11}
    assert {c["key"][0] for c in island.static.diff["relative_windows"]} == {10, 11}
    for r in island.static.patched.tables["optimum_time"]:
        if base[r["spawner"]]["exception"]:
            assert r["stored"] == base[r["spawner"]]["stored"]
    assert any(w["code"] == "ot_exceptions_preserved" and len(w["spawners"]) == 16 for w in island.warnings)


def test_island10_evolution_interpolation_and_graph(island):
    assert island.static.diff["evolution_target_days"]
    assert island.static.diff["interpolation_points"]
    assert not island.static.diff.get("evolution_values")
    assert {"balance.island_timing_to_days", "simulation.bake_recalc_optimum_time"} <= {r["id"] for r in island.formulas}
    assert {r["scope"] for r in island.formulas} == {"immediate", "progression", "full"}
    assert all(r["previous"] and r["implementers"] for r in island.formulas)


def test_single_spawner_bake_locality(cfg):
    c = run(cfg, load(EXAMPLES / "spawner_fight_time.yaml"), skip_sim=True)
    assert set(c.static.diff) == {"bake"}
    assert {tuple(r["key"]) for r in c.comparisons["bake/spawners.csv"]} == {("Scenes/Main.unity:57651",)}
    assert {r["field"] for r in c.comparisons["bake/spawners.csv"]} == {"new_health", "new_damage"}


def test_unmodelled_reader_warning(cfg):
    c = run(cfg, P.parse_patch({"overrides": [{"node": "SimulationConfig.TimeChunkDurationMinutes", "mul": 2}]}),
            skip_sim=True)
    warning = next(w for w in c.warnings if w["code"] == "not_modeled_static")
    assert warning["detail"]["formulas"]
    assert not any(c.comparisons.values())


def test_unknown_graph_reader_warns(cfg):
    rows, warnings = impact.affected(cfg, ["NoSuchConfig.Nonexistent"])
    assert rows == [] and warnings[0]["code"] == "no_formula_reader"


def test_reports_byte_identical(island, tmp_path):
    a, b = tmp_path / "a", tmp_path / "b"
    report.write(island, a)
    report.write(island, b)
    first = {p.relative_to(a): p.read_bytes() for p in a.rglob("*") if p.is_file()}
    second = {p.relative_to(b): p.read_bytes() for p in b.rglob("*") if p.is_file()}
    assert first == second
    assert "model_floor.csv" in {str(p) for p in first}


def test_cli_repeated_bytes_and_timing(db_path, tmp_path):
    outputs = []
    for i in range(2):
        out = tmp_path / str(i)
        start = time.perf_counter()
        assert main([str(EXAMPLES / "island10_later.yaml"), "--skip-sim", "--db", str(db_path), "--out", str(out)]) == 0
        assert time.perf_counter() - start < 5
        outputs.append({p.relative_to(out): p.read_bytes() for p in out.rglob("*") if p.is_file() and p.name != "timings.json"})
    assert outputs[0] == outputs[1]


@pytest.mark.parametrize("data", [
    {"name": "../escape"}, {"simulate": {"enabled": "yes"}}, {"simulate": {"stop_day": True}},
    {"simulate": {"bake_iterations": 0}}, {"simulate": {"unknown": 1}}, {"simulate": {"params": 5}},
    {"report": {"key_days": [1.2]}}, {"report": {"top_n": 0}}, {"report": {"unknown": 3}},
])
def test_diff_option_validation(data):
    with pytest.raises(P.PatchError):
        patchfile.validate(P.parse_patch(data))


def test_patch_inheritance(tmp_path):
    (tmp_path / "base.yaml").write_text("name: base\nsimulate: {enabled: false}\noverrides: []\n")
    child = tmp_path / "child.yaml"
    child.write_text("name: child\nbase: base.yaml\nreport: {key_days: [5]}\n")
    patch = load(child)
    assert patch.bases == ("base",) and patch.simulate == {"enabled": False}


def test_changed_cells_add_remove_numeric_and_sorted(tmp_path):
    rows = tables.compare_rows([{"id": 1, "n": 0}, {"id": 2, "n": 3}],
                               [{"id": 1, "n": 2}, {"id": 3, "n": 4}], ("id",))
    assert len(rows) == 3 and rows[0]["ratio"] is None and rows[0]["delta"] == 2
    tables.write(tmp_path / "t.csv", list(reversed(rows)), tables.CHANGE_COLUMNS)
    assert tables.read_csv(tmp_path / "t.csv")[0]["delta"] == 2
    with pytest.raises(ValueError, match="duplicate"):
        tables.compare_rows([{"id": 1}, {"id": 1}], [], ("id",))


def test_asset_version_temporary_edit_restored(cfg, tmp_path):
    asset = compare.REPO / compare.ASSET_PREFIX / "MonoBehaviour/Combat Balance Config.asset"
    original = asset.read_bytes()
    try:
        updated = original.decode().replace("  CopyValueTargetShare: 0.5", "  CopyValueTargetShare: 0.6")
        assert updated != original.decode()
        asset.write_text(updated)
        base, patch, metadata = compare.asset_patch(cfg, base="HEAD", scratch=tmp_path)
        assert [o.node for o in patch.overrides] == ["CombatBalanceConfig.CopyValueTargetShare"]
        assert len(metadata["raw_diff"]) == 1 and not metadata["warnings"]
        c = run(base, patch, skip_sim=True)
        c.comparisons["static/asset_leaves.csv"] = metadata["raw_diff"]
        assert "有变化" in report.write(c, tmp_path / "report").read_text()
    finally:
        asset.write_bytes(original)
    assert asset.read_bytes() == original


def test_asset_mode_refuses_scene_and_identity_edits(cfg, monkeypatch, tmp_path):
    def git(*args, **kw):
        return "x" if args[0] == "rev-parse" else compare.ASSET_PREFIX + "Scenes/Main.unity\n"
    monkeypatch.setattr(compare, "git", git)
    with pytest.raises(ValueError, match="scene/prefab/identity"):
        compare.asset_patch(cfg, scratch=tmp_path)


def test_cli_failure_is_nonzero(tmp_path, db_path):
    p = tmp_path / "bad.yaml"
    p.write_text("name: fail\noverrides: [{node: NoSuchConfig.Bad, set: 3}]\n")
    assert main([str(p), "--skip-sim", "--db", str(db_path), "--out", str(tmp_path / "report")]) == 1


def test_simulation_disabled_does_not_construct_driver(cfg, monkeypatch):
    monkeypatch.setattr(compare, "_simulate", lambda *a, **kw: pytest.fail("simulation should be skipped"))
    c = run(cfg, P.parse_patch({"simulate": {"enabled": False}}))
    assert c.manifest["skip_sim"] and c.manifest["run_keys"] == []
