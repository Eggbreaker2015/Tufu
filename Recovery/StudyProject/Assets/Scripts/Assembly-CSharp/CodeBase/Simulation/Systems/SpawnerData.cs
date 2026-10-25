using Codebase.Enemy;
using Codebase.Player.Models;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Simulation.Systems
{
	public class SpawnerData
	{
		public PlayerModel playerModel;

		public SimulationConfig config;

		public SimulationSystem system;

		public EnemySpawner spawner;

		public double healthDelta;

		public double fightTime;

		public double playerDPS;

		public double enemyDPS;

		public double rewardValue;

		public double playerRPS;

		public double farmedTime;

		public double avMoveTime;

		public double avRegenTime;

		public SpawnerData(PlayerModel playerModel, SimulationConfig config, SimulationSystem system)
		{
// Original ELF VA 0x13e0724, all original ownership references retained.
            this.playerModel = playerModel;
            this.config = config;
            this.system = system;
		}

		public double GetMoveTime(Vector3 position)
		{
			return 0.0;
		}

		public double GetPriority(Vector3 position, StatType? statType = null)
		{
			return 0.0;
		}

		public string GetPriorityInfo(Vector3 position)
		{
			return null;
		}

		public double GetStaticPriority(Vector3 position)
		{
			return 0.0;
		}

		public double GetDynamicPriority(Vector3 position, StatType? statType = null)
		{
			return 0.0;
		}
	}
}
