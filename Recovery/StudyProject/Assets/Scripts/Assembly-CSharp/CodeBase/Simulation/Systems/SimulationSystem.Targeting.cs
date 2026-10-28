using Codebase.Enemy;
using Codebase.Player.Inventory;
using Codebase.Simulation.Rebuild;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Simulation.Systems
{
	public partial class SimulationSystem
	{
		[SimulationRebuild(Provenance.DebugOnly)]
		public EnemySpawner GetBestSpawnerForInfo()
		{
			return null;
		}

		[SimulationRebuild(Provenance.Null)]
		private void CalculateSpawnerPaths(EnemySpawner A, EnemySpawner B)
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private double GetFightEfficiency(EnemySpawner spawner)
		{
			return 0.0;
		}

		[SimulationRebuild(Provenance.Null)]
		private double GetTotalDPS()
		{
			return 0.0;
		}

		[SimulationRebuild(Provenance.Null)]
		private double GetTotalArmor()
		{
			return 0.0;
		}

		[SimulationRebuild(Provenance.Null)]
		private double CalculateFarmPotencial(EnemySpawner spawner, double totalPath, double totalDamage)
		{
			return 0.0;
		}

		[SimulationRebuild(Provenance.Null)]
		private void CalculateStatBenefit()
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private (Stat, StatModifier) ModifyFromItem(ItemModel item)
		{
			return default;
		}

		[SimulationRebuild(Provenance.DebugOnly)]
		private void DrawSquare(Vector3 pos, float size, Color color)
		{
		}

		[SimulationRebuild(Provenance.Null)]
		private SpawnerData FindBestSpawner()
		{
			return null;
		}

		[SimulationRebuild(Provenance.DebugOnly)]
		public bool TryGetSpawnerPriority(EnemySpawner spawner, out string priorityInfo)
		{
			priorityInfo = null;
			return false;
		}
	}
}
