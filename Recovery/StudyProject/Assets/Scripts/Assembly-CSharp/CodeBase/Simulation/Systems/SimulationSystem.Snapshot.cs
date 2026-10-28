using Codebase.Simulation.Rebuild;

namespace Codebase.Simulation.Systems
{
	public partial class SimulationSystem
	{
		[SimulationRebuild(Provenance.Null)]
		private void MakeSnapshot(bool force = false)
		{
		}
	}
}
