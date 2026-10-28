using System;

namespace Codebase.Simulation.Rebuild
{
	// Where the body of a rebuilt (originally empty) simulation method comes from (design 1.5 C# rule 3, 4.1).
	public enum Provenance
	{
		// Behaviour fixed by the shipped data.
		Inferred = 0,
		// Behaviour fitted to the shipped data.
		Calibrated = 1,
		// Disabled under the shipped config; still an empty placeholder.
		Null = 2,
		// Debug or logging only; the bot does not depend on it.
		DebugOnly = 3,
		// Body recovered from native code (design 4.9).
		Native = 4
	}

	// [SimulationRebuild(Provenance.Inferred, "simulation.balance_timing")]: the formula id is a catalog id or
	// "pending:<domain>.<name>"; placeholders may leave it out. Read by tufu_balance.model.registry.
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
	public sealed class SimulationRebuildAttribute : Attribute
	{
		public SimulationRebuildAttribute(Provenance provenance, string formulaId = null)
		{
			Provenance = provenance;
			FormulaId = formulaId;
		}

		public Provenance Provenance { get; }

		public string FormulaId { get; }
	}
}
