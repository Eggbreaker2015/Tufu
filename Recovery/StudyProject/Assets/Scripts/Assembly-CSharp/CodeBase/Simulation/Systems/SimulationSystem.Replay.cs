using Codebase.Simulation.Rebuild;

namespace Codebase.Simulation.Systems
{
	public partial class SimulationSystem
	{
		[SimulationRebuild(Provenance.Null)]
		partial void RebuildApplyDayOverride(int day)
		{
		}

		[SimulationRebuild(Provenance.Null)]
		partial void RebuildApplyReplayCaches()
		{
		}
	}
}
