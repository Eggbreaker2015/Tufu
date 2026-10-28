using Codebase.Simulation.Rebuild;
using Codebase.Unit;

namespace Codebase.Simulation.Systems
{
	public partial class SimulationSystem
	{
		[SimulationRebuild(Provenance.Null)]
		public StatType CalculateBestRewardStat()
		{
			return StatType.MovementSpeed;
		}

		[SimulationRebuild(Provenance.Null)]
		private void ProcessAFK(float minutes)
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private void NextLifeCycle()
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private void CheckLifeCycle()
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private void ProcessSimulateAds()
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private void ApplyRegen(double seconds)
		{
		}
	}
}
