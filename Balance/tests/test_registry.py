"""Formula implementation registry: @implements on the Python side, [SimulationRebuild] on the C# side."""

from __future__ import annotations

import warnings

import pytest

from tufu_balance.model import registry as reg
from tufu_balance.model.registry import (PendingFormulaWarning, Provenance, RebuildAnnotation, Registry,
                                         UnknownFormulaId, parse_rebuild_annotations)

KNOWN = "stats.raw_value_aggregation"
ALIAS = "combat.stat_value_aggregation"  # alias of KNOWN in catalog/formulas.json


def test_registry_json_switch():
    config = reg.load_config()
    assert config["schema"] == "tufu.registry/1"
    assert isinstance(config["pending_allowed"], bool)


def test_unknown_id_raises_and_pending_warns():
    r = Registry()
    with pytest.raises(UnknownFormulaId):
        r.implements("timeline.no_such_formula", provenance=Provenance.RULE)
    with pytest.raises(UnknownFormulaId):
        r.implements("pending:BadName", provenance="rule")
    with pytest.raises(ValueError):
        r.implements(KNOWN, provenance="guess")
    with warnings.catch_warnings():
        warnings.simplefilter("error")
        r.implements(KNOWN, provenance=Provenance.PORT)  # known ids do not warn
    with pytest.warns(PendingFormulaWarning):
        r = Registry(pending_allowed=True)
        r.implements("pending:simulation.fast_kill_gate", provenance=Provenance.INFERRED)
    with pytest.raises(UnknownFormulaId, match="no longer allowed"):
        Registry(pending_allowed=False).implements("pending:simulation.fast_kill_gate", provenance="inferred")


def test_decorator_registers_and_resolves_aliases():
    r = Registry()

    @r.implements(ALIAS, provenance=Provenance.PORT, code_ref="CodeBase/Unit/StatBase.cs:107", va="0x1")
    @r.implements("stats.specific_value_pipeline", provenance="port")
    def f(x):
        return x

    assert f(3) == 3
    assert [i.canonical_id for i in f.__implements__] == ["stats.specific_value_pipeline", KNOWN]
    [impl] = r.implementers(KNOWN)
    assert (impl.formula_id, impl.qualname, impl.code_ref, impl.va) == (ALIAS, f.__qualname__,
                                                                       "CodeBase/Unit/StatBase.cs:107", "0x1")
    assert r.implementers(ALIAS) == [impl]


def test_coverage_counts_by_provenance():
    r = Registry(pending_allowed=True)
    with warnings.catch_warnings():
        warnings.simplefilter("ignore", PendingFormulaWarning)
        for fid, prov in [(KNOWN, "port"), ("stats.specific_value_pipeline", "port"),
                          ("stats.event_percent_bonus", "rule"), ("pending:timeline.tail", "rule")]:
            r.implements(fid, provenance=prov)(lambda: None)
    cs = [RebuildAnnotation("a.cs", 1, "SimulationSystem", "Tick", "Inferred", ALIAS),
          RebuildAnnotation("a.cs", 9, "SimulationSystem", "FastTick", None, None),
          {"member": "Gate", "provenance": "Inferred", "formula_id": "pending:simulation.fast_kill_gate"}]
    cov = r.coverage(csharp=cs, import_models=False)
    assert cov["python"]["by_provenance"] == {"port": 2, "rule": 2}
    assert cov["python"]["formulas"] == 3 and cov["python"]["pending"] == ["pending:timeline.tail"]
    assert cov["csharp"]["by_provenance"] == {"Inferred": 2, "unspecified": 1}
    assert cov["csharp"]["pending"] == ["pending:simulation.fast_kill_gate"]
    assert cov["catalog"]["python"] == 3 and cov["catalog"]["csharp"] == 1 and cov["catalog"]["either"] == 3
    assert cov["catalog"]["formulas"] == len(reg.load_catalog()[0])


CS_SAMPLE = '''
namespace Codebase.Simulation
{
	public partial class SimulationSystem
	{
		// [SimulationRebuild(Provenance.Inferred, "pending:sim.commented_out")]
		/* [SimulationRebuild(Provenance.Inferred, "pending:sim.block_comment")] */
		[SimulationRebuild]
		private void FastTick() { var a = new int[3]; a[1] = 2; }

		[SimulationRebuild(Provenance.Inferred, "pending:simulation.fast_kill_gate")]
		[Obsolete("x ] y")]
		internal bool RebuildIsKillable(float loss, float health) => !(health < loss);

		[Codebase.Simulation.Rebuild.SimulationRebuildAttribute(Rebuild.Provenance.Calibrated,
			"simulation.rpm_time_chunk")]
		public float SimulationSpeed { get; set; }

		[SerializeField, SimulationRebuild(Provenance.Null)] partial void RebuildApplyDayOverride(int day);
	}

	public class SpawnerData
	{
		[method: SimulationRebuild(Provenance.DebugOnly, FormulaId = "pending:spawners.priority")]
		public float GetPriority<T>(T x) { return 0f; }
	}

	public partial class SimulationSystem
	{
		[SimulationRebuild(Provenance.Null, "simulation.rpm_time_chunk")]
		private (double, double, string) GetUnlockTimingLogData(EnemySpawner spawner) { return default; }

		[SimulationRebuild(Provenance.Null)]
		private static (Stat stat, StatModifier modifier)? ModifyFromItem<T>(T item, int n = 0) => default;

		[SimulationRebuild(Provenance.Inferred)]
		public (int Count, float Sum) Totals { get; private set; }

		[SimulationRebuild(Provenance.Inferred)]
		internal Task<(int, Dictionary<int, float>)> LoadAsync() { return null; }
	}
}
'''


def test_parse_rebuild_annotations():
    got = [(a.line, a.type_name, a.member, a.provenance, a.formula_id)
           for a in parse_rebuild_annotations(CS_SAMPLE, "X.cs")]
    assert got == [
        (8, "SimulationSystem", "FastTick", None, None),
        (11, "SimulationSystem", "RebuildIsKillable", "Inferred", "pending:simulation.fast_kill_gate"),
        (15, "SimulationSystem", "SimulationSpeed", "Calibrated", "simulation.rpm_time_chunk"),
        (19, "SimulationSystem", "RebuildApplyDayOverride", "Null", None),
        (24, "SpawnerData", "GetPriority", "DebugOnly", "pending:spawners.priority"),
        (30, "SimulationSystem", "GetUnlockTimingLogData", "Null", "simulation.rpm_time_chunk"),
        (33, "SimulationSystem", "ModifyFromItem", "Null", None),
        (36, "SimulationSystem", "Totals", "Inferred", None),
        (39, "SimulationSystem", "LoadAsync", "Inferred", None),
    ]
    assert parse_rebuild_annotations("class A { [Obsolete] void F() {} }") == []


def test_repository_annotations_and_implementations_are_registered():
    """Every id used on either side is a catalog id or (while allowed) a pending id."""
    pending_allowed = reg.load_config()["pending_allowed"]
    with warnings.catch_warnings():
        warnings.simplefilter("ignore", PendingFormulaWarning)
        reg.import_model_modules()
    ids, aliases = reg.load_catalog()
    for impl in reg.REGISTRY.implementations:
        assert impl.canonical_id in ids or (impl.pending and pending_allowed), impl
    for a in reg.scan_csharp():
        if a.formula_id is None:
            continue
        if a.pending:
            assert pending_allowed, f"{a.file}:{a.line} uses {a.formula_id} but pending ids are no longer allowed"
        else:
            assert a.formula_id in ids or a.formula_id in aliases, f"{a.file}:{a.line} unknown id {a.formula_id}"
