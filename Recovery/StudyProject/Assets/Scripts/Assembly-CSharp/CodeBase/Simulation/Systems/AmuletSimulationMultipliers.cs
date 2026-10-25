using System.Collections.Generic;
using Codebase.Amulet;
using Codebase.Amulet.Upgrades;
using Codebase.Enemy;
using Codebase.Player.Inventory;

namespace Codebase.Simulation.Systems
{
	public class AmuletSimulationMultipliers
	{
		private readonly AmuletConfig amuletConfig;

		private readonly AmuletUpgradeConfig upgradeConfig;

		private readonly EnemiesModel enemiesModel;

		private readonly Dictionary<string, List<(int Day, double Amount)>> soulEmissionPoints;

		private readonly Dictionary<Rarity, List<(int Day, double Bonus)>> spawnerChargeBonusPoints;

		private static readonly string[] CostSoulKeys;

		public AmuletSimulationMultipliers(AmuletConfig amuletConfig, AmuletUpgradeConfig upgradeConfig, EnemiesModel enemiesModel)
		{
		}

		public void Build()
		{
		}

		public double GetSoulEmission(string soulKey, int day)
		{
			return 0.0;
		}

		public double GetSpawnerChargeMultiplier(EnemySpawner spawner, int day)
		{
			return 0.0;
		}

		private double GetSpawnerChargeBonus(Rarity rarity, int day)
		{
			return 0.0;
		}

		private void LogPoints()
		{
		}
	}
}
