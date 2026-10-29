"""P12 full designer workflow on the unit commit through the guarded launcher."""

from dataclasses import replace
from pathlib import Path

import pytest

from tufu_balance.calibrate.driver import BatchBackend, Driver
from tufu_balance.calibrate.runcache import RunCache
from tufu_balance.diff import load, patchfile, report, run
from tufu_balance.model.config import load_game_config

pytestmark = pytest.mark.unity
EXAMPLES = Path(__file__).resolve().parents[2] / "patches/examples"


@pytest.fixture(scope="module")
def comparison(db_path, unity_rev, unity_opts):
    cfg = load_game_config(db_path)
    driver = Driver(BatchBackend(rev=unity_rev, opts=unity_opts), cache=RunCache())
    patch = load(EXAMPLES / "island10_later.yaml")
    first = run(cfg, patch, driver=driver, rev=unity_rev)
    submitted = len(driver.submitted)
    second = run(cfg, patch, driver=driver, rev=unity_rev)
    return first, second, driver, submitted


def test_patch_a_csharp_leaves_and_crosschecks(comparison):
    first, _, _, _ = comparison
    assert first.consistency["identical"]
    assert first.consistency["python"] == first.consistency["csharp"] > 0
    assert first.manifest["simulation_label"] == "uncalibrated"
    for record in first.simulation["crosschecks"]:
        assert record["checks"]["X4"]["items"] > 0
        assert record["checks"]["X4"]["status"] == "pass"
        assert record["checks"]["X1"]["status"] == "pass"
        bad = [k for k, c in record["checks"].items() if c["status"] == "fail"]
        assert not bad, (record["side"], bad)


def test_baseline_cache_and_identical_reports(comparison, tmp_path):
    first, second, driver, submitted = comparison
    assert len(driver.submitted) == submitted
    assert all(j["status"] == "cached" and j["attempts"] == 0 for j in second.simulation["jobs"])
    assert first.manifest["run_keys"] == second.manifest["run_keys"]
    for name, comp in (("first", first), ("second", second)):
        report.write(comp, tmp_path / name)
    a, b = tmp_path / "first", tmp_path / "second"
    assert {p.relative_to(a): p.read_bytes() for p in a.rglob("*") if p.is_file()} == {
        p.relative_to(b): p.read_bytes() for p in b.rglob("*") if p.is_file()}
    assert "uncalibrated" in (a / "report.md").read_text()


def test_empty_patch_simulation_has_no_changed_rows(db_path, unity_rev, unity_opts):
    cfg = load_game_config(db_path)
    driver = Driver(BatchBackend(rev=unity_rev, opts=unity_opts), cache=RunCache())
    c = run(cfg, replace(patchfile.empty(), simulate={"stop_day": 6}), driver=driver, rev=unity_rev)
    assert c.consistency["identical"] and not c.changed
    assert not any(c.comparisons.values())
