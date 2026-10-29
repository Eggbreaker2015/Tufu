"""P12 designer workflow: locality, repeatability, validation and read-only asset expansion."""

from pathlib import Path
from dataclasses import replace
import gzip
import json
import shutil
import subprocess
import time
from types import SimpleNamespace

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


@pytest.fixture
def asset_repo(tmp_path):
    repo = tmp_path / "repo"
    assets = repo / compare.ASSET_PREFIX
    source = compare.REPO / compare.ASSET_PREFIX
    shutil.copytree(source / "Scripts", assets / "Scripts")
    asset = assets / "MonoBehaviour/Combat Balance Config.asset"
    asset.parent.mkdir(parents=True)
    for path in ("MonoBehaviour/Combat Balance Config.asset", "MonoBehaviour/Combat Balance Config.asset.meta"):
        shutil.copy2(source / path, assets / path)
    texture = assets / "Texture2D/test.png"
    texture.parent.mkdir()
    texture.write_bytes(b"test")
    texture.with_suffix(".png.meta").write_text("fileFormatVersion: 2\nguid: " + "a" * 32 + "\n")
    for args in (("init", "-q"), ("add", "."),
                 ("-c", "user.name=Diff Test", "-c", "user.email=diff@example.invalid", "commit", "-qm", "Fixture")):
        subprocess.run(["git", "--no-pager", "-C", str(repo), *args], check=True, capture_output=True)
    return repo


def test_asset_version_temporary_edit_restored(cfg, tmp_path, asset_repo):
    asset = asset_repo / compare.ASSET_PREFIX / "MonoBehaviour/Combat Balance Config.asset"
    original = asset.read_bytes()
    try:
        updated = original.decode().replace("  CopyValueTargetShare: 0.5", "  CopyValueTargetShare: 0.6")
        assert updated != original.decode()
        asset.write_text(updated)
        base, patch, metadata = compare.asset_patch(cfg, base="HEAD", worktree=asset_repo, scratch=tmp_path)
        assert [o.node for o in patch.overrides] == ["CombatBalanceConfig.CopyValueTargetShare"]
        assert len(metadata["raw_diff"]) == 1 and not metadata["warnings"]
        c = run(base, patch, skip_sim=True)
        c.comparisons["static/asset_leaves.csv"] = metadata["raw_diff"]
        assert "有变化" in report.write(c, tmp_path / "report").read_text()
    finally:
        asset.write_bytes(original)
    assert asset.read_bytes() == original
    assert compare.git("status", "--porcelain", worktree=asset_repo) == ""


def test_asset_mode_ignores_unrelated_texture_meta_with_same_guid(cfg, tmp_path, asset_repo):
    meta = asset_repo / compare.ASSET_PREFIX / "Texture2D/test.png.meta"
    meta.write_text(meta.read_text() + "\n")
    _, patch, metadata = compare.asset_patch(cfg, worktree=asset_repo, scratch=tmp_path)
    assert not patch.overrides and metadata["ignored_meta"] == [str(meta.relative_to(asset_repo))]


@pytest.mark.parametrize("meta_path,change_guid", [
    ("Texture2D/test.png.meta", True), ("MonoBehaviour/Combat Balance Config.asset.meta", False),
])
def test_asset_mode_refuses_unrelated_identity_or_config_meta(cfg, tmp_path, asset_repo, meta_path, change_guid):
    meta = asset_repo / compare.ASSET_PREFIX / meta_path
    meta.write_text(meta.read_text().replace("a" * 32, "b" * 32) if change_guid else meta.read_text() + "\n")
    with pytest.raises(ValueError, match="scene/prefab/identity"):
        compare.asset_patch(cfg, worktree=asset_repo, scratch=tmp_path)


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


def test_malformed_yaml_and_unowned_output_are_rejected(tmp_path, db_path):
    p = tmp_path / "broken.yaml"
    p.write_text("name: [\n")
    assert main([str(p), "--skip-sim", "--db", str(db_path)]) == 1
    out = tmp_path / "existing"
    out.mkdir()
    keep = out / "keep.txt"
    keep.write_text("preserve")
    assert main(["--skip-sim", "--db", str(db_path), "--out", str(out)]) == 1
    assert keep.read_text() == "preserve"


def test_simulation_disabled_does_not_construct_driver(cfg, monkeypatch):
    monkeypatch.setattr(compare, "_simulate", lambda *a, **kw: pytest.fail("simulation should be skipped"))
    c = run(cfg, P.parse_patch({"simulate": {"enabled": False}}))
    assert c.manifest["skip_sim"] and c.manifest["run_keys"] == []


@pytest.fixture
def cached_sim(cfg, tmp_path, monkeypatch):
    """Synthetic cached protocol outputs: no Unity and no dependency on local out/ runs."""
    from tufu_balance import crosscheck as X
    from tufu_balance.calibrate.driver import JobResult
    from tufu_balance.unity import bake_report, fasttick, patcher, spawner_map, request
    from tufu_balance.model.enums import WeaponType
    from tufu_balance.model.snapshots import SimulationSnapshot, SimulationItem

    snap = X.Snap("Snapshots", 1, SimulationSnapshot(1.0, 1.0, 1, 0,
                  items=[SimulationItem("knife", 1, 1)], dps_data=[(WeaponType.Cutting, 10.0)]), None, [])
    state = SimpleNamespace(failures={}, x2_rows=[], tamper=False, fight=True, empty=False, batches=[], dirs=[])
    monkeypatch.setattr(X, "load_run", lambda _: SimpleNamespace(early=[] if state.empty else [snap],
                                                               full=[] if state.empty else [snap]))
    # Bake parsing/integrity has its own C8 tests; keep this fake to a single ordinary spawner.
    monkeypatch.setattr(bake_report, "read_report", lambda d: json.loads((d / "bake_report.json").read_text()))
    identity = spawner_map.load_identity_map(cfg.source).by_spawner_id[("Scenes/Main.unity", 57651)]

    def checks(directory, con, *, checks, out_dir):
        tables.write(out_dir / "X2.csv", state.x2_rows,
                     ("list", "index", "case", "kind", "weapon_type", "csharp", "oracle", "ok"))
        return {k: X.CheckResult(k, k, "fail" if k in state.failures else "pass", 10,
                                state.failures.get(k, 0)) for k in checks}

    monkeypatch.setattr(X, "run_checks", checks)

    class CachedDriver:
        template = request.make_request([])

        def run(self, jobs):
            state.batches.append(jobs)
            results = []
            for job in jobs:
                key = f"{len(state.batches)}-{job['job_id']}"
                directory = tmp_path / key
                directory.mkdir(exist_ok=True)
                state.dirs.append(directory)
                _, expanded = P.apply(cfg, P.parse_patch({"overrides": job["patch"]}))
                leaves = [x.to_json() for x in expanded.csharp_leaves()]
                if state.tamper and job["job_id"] == "diff-patched":
                    leaves[0]["new"] += 1.0
                (directory / "applied_patch.json").write_text(json.dumps({"schema": patcher.APPLIED_SCHEMA, "leaves": leaves}))
                (directory / "bake_report.json").write_text(json.dumps({"spawners": [{"id": identity.runtime_id,
                    "report": {"NewHealth": "405e000000000000", "NewDamage": "4000000000000000"}}]}))
                event = {"type": "slow_fight", "day": 0, "p_after": 1.0, "t_after": 1.0, "cleared": True}
                (directory / "events.jsonl").write_text(json.dumps(event) + "\n" if state.fight else "")
                (directory / "daily.csv").write_text("day,clears\n0,1\n")
                (directory / "spawner_days.csv.gz").write_bytes(gzip.compress((",".join(fasttick.SPAWNER_DAYS_COLUMNS) + "\n").encode()))
                results.append(JobResult(job["job_id"], key, "cached", directory, 0, "cache"))
            return results

    state.driver = CachedDriver()
    state.row = {"list": "Snapshots", "index": 1, "case": "slow_tick", "kind": "DpsData",
                 "weapon_type": "Cutting", "csharp": 10.0, "oracle": 12.0, "ok": 0}
    state.patch = P.parse_patch({"overrides": [{"node": "EnemySpawnerConfig.AverageFightTime",
                                  "where": {"scene": "Scenes/Main.unity", "file_id": 57651}, "mul": 1.2}],
                                "simulate": {"stop_day": 6}})
    state.run = lambda patch=None: run(cfg, patch or state.patch, driver=state.driver, out_root=tmp_path / "sim")
    return state


def test_simulation_rejects_tampered_csharp_leaves(cached_sim):
    cached_sim.tamper = True
    with pytest.raises(ValueError, match="C# applied_patch differs"):
        cached_sim.run()


@pytest.mark.parametrize("check", ["X1", "X2", "X3", "X4", "X5", "X6", "X7"])
def test_simulation_rejects_other_crosscheck_failures(cached_sim, check):
    cached_sim.failures = {check: 1}
    with pytest.raises(ValueError, match=f"crosscheck failed.*{check}"):
        cached_sim.run()


def test_slow_tick_x2_is_fatal_even_with_same_tick_fight(cached_sim):
    cached_sim.x2_rows = [cached_sim.row]
    cached_sim.failures = {"X2": 1}
    with pytest.raises(ValueError, match="crosscheck failed.*X2"):
        cached_sim.run()


@pytest.mark.parametrize("empty", [True, False])
def test_vacuous_simulation_does_not_claim_no_change(cached_sim, tmp_path, empty):
    cached_sim.empty = empty
    c = cached_sim.run(load(EXAMPLES / "island10_later.yaml"))
    text = report.write(c, tmp_path / "report").read_text()
    for name in ("items", "item_first", "evolution"):
        section = text.split(f"### sim/{name}.csv", 1)[1].split("###", 1)[0]
        assert "两侧均无数据：当前重建未模拟" in section
        assert "无变化" not in section
    assert any(w["code"] == "sim_not_modelled" and w["nodes"] for w in c.warnings)


def test_rebake_two_iterations_and_cached_baseline(cached_sim):
    c = cached_sim.run(replace(cached_sim.patch, simulate={"stop_day": 6, "bake_iterations": 2}))
    assert len(cached_sim.batches) == 2
    for job in cached_sim.batches[1]:
        sets = {o["node"]: o["set"] for o in job["patch"] if o["node"].startswith("EnemySpawnerConfig.Stats[")}
        assert sets == {"EnemySpawnerConfig.Stats[Health]": 120.0, "EnemySpawnerConfig.Stats[Damage]": 2.0}
    assert c.manifest["bake_iterations"] == 2 and len(c.simulation["jobs"]) == 4
    assert all(j["status"] == "cached" and j["attempts"] == 0 for j in c.simulation["jobs"])


def test_simulated_cached_reports_are_byte_identical(cached_sim, tmp_path):
    first = cached_sim.run()
    cached_sim.batches.clear()
    second = cached_sim.run()
    a, b = tmp_path / "first", tmp_path / "second"
    report.write(first, a)
    report.write(second, b)
    assert {p.relative_to(a): p.read_bytes() for p in a.rglob("*") if p.is_file()} == {
        p.relative_to(b): p.read_bytes() for p in b.rglob("*") if p.is_file()}


def test_explicit_db_simulation_without_environment(cached_sim, monkeypatch, cfg):
    from tufu_balance.unity import launcher
    from tufu_balance.unity.spawner_map import expected_ready_csv, expected_ready_file_name
    monkeypatch.delenv("TUFU_BALANCE_DB", raising=False)
    cached_sim.run()
    table = Path(cached_sim.driver.template["sandbox"]["expected_ready_spawners"])
    text = expected_ready_csv(cfg.source, "A")
    assert not table.is_absolute() and table.name == expected_ready_file_name(text)
    out = cached_sim.dirs[0] / "side-inputs"
    out.mkdir()
    launcher.write_side_inputs(cached_sim.driver.template, out, db=C.source_path(cfg))
    assert (out / table).read_text() == text


def test_db_environment_conflict_fails_early(cfg, monkeypatch, tmp_path):
    monkeypatch.setenv("TUFU_BALANCE_DB", str(tmp_path / "other.sqlite"))
    with pytest.raises(ValueError, match="source and TUFU_BALANCE_DB differ"):
        run(cfg, patchfile.empty())


def test_arrival_extrapolation_uses_island_start_day():
    assert compare._late_arrivals([{"island": 3, "day": 6}], [0.5, 1.83, 4.0]) == [{"island": 3, "day": 6}]
    assert compare._late_arrivals([{"island": 3, "day": 6}], [1.0, 4.0, 9.0]) == []


def test_native_coverage_is_distinct_from_unmodelled(island):
    by_id = {r["id"]: r for r in island.formulas}
    for fid in ("stats.raw_value_aggregation", "combat.outgoing_damage_simulation_mode"):
        assert "C# 原生" in by_id[fid]["coverage"]
        assert by_id[fid]["implementers"] != ["未建模"]


def test_editor_only_leaves_never_claim_csharp_pass(island, tmp_path):
    from tufu_balance.unity import patcher
    c = replace(island, consistency=patcher.compare(island.static.report, {"leaves": [
        x.to_json() for x in island.static.report.csharp_leaves()]}))
    text = report.write(c, tmp_path).read_text()
    rows = tables.read_csv(tmp_path / "overrides.csv")
    editor = [r for r in rows if r["node"].startswith("SpawnerRelativeOptimumTimeTool.")]
    assert len(editor) == 2 and all(r["csharp"] == "仅 Python（编辑器工具，C# 不应用）" for r in editor)
    assert "117/119" in text


def test_readiness_request_is_independent_of_output_location(cached_sim, cfg, tmp_path):
    cached_sim.run()
    first = dict(cached_sim.driver.template["sandbox"])
    run(cfg, cached_sim.patch, driver=cached_sim.driver, out_root=tmp_path / "different-checkout" / "sim")
    assert cached_sim.driver.template["sandbox"] == first


@pytest.mark.parametrize("kind", ["batch", "serve"])
def test_supplied_driver_forwards_config_readiness_db(cached_sim, cfg, monkeypatch, kind):
    from tufu_balance.calibrate import driver as D
    from tufu_balance.unity import launcher

    calls = []
    monkeypatch.delenv("TUFU_BALANCE_DB", raising=False)
    monkeypatch.setattr(launcher, "run", lambda request, **kw: calls.append(kw))
    monkeypatch.setattr(launcher, "serve_submit", lambda request, **kw: calls.append(kw))
    monkeypatch.setattr(launcher, "serve_status", lambda **kw: {"running": True, "code_rev": "test"})
    backend = D.BatchBackend() if kind == "batch" else D.ServeBackend()
    cached_sim.driver.backend = backend
    cached_sim.run()
    assert backend.readiness_db == C.source_path(cfg)
    backend.submit(cached_sim.driver.template)
    assert calls[0]["readiness_db"] == C.source_path(cfg)
