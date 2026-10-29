using System.Globalization;
using Codebase.Enemy;
using Codebase.Player.Models;
using Codebase.Simulation.Rebuild;
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

		// Seconds from `position` to the spawner at the player's MovementSpeed (avMoveTime is the same with the
		// recovered average distance rpm.Item1 = 70).
		[SimulationRebuild(Provenance.Calibrated, "simulation.slow_move_time")]
		public double GetMoveTime(Vector3 position)
		{
			double distance = Vector3.Distance(position, spawner.transform.position);
			return distance / playerModel.Unit.GetStatSpecificValue(StatType.MovementSpeed, true);
		}

		[SimulationRebuild(Provenance.Calibrated, "simulation.slow_priority")]
		public double GetPriority(Vector3 position, StatType? statType = null)
		{
			return system.RebuildPriorityFamily() == PriorityFamily.Static ? GetStaticPriority(position) : GetDynamicPriority(position, statType);
		}

		[SimulationRebuild(Provenance.DebugOnly, "simulation.slow_priority")]
		public string GetPriorityInfo(Vector3 position)
		{
			return string.Format(CultureInfo.InvariantCulture,
				"{0} L{1} priority {2:R} static {3:R} dynamic {4:R} reward {5:R} x{6:R} clear {7:R} s (farmed {8:R}, move {9:R}/{10:R}, fight {11:R}, regen {12:R}) loss {13:R}",
				spawner.Config.Key, spawner.Config.LocationNumber, GetPriority(position), GetStaticPriority(position), GetDynamicPriority(position),
				rewardValue, (double)spawner.Config.PriorityMultiplier, (farmedTime - avMoveTime + GetMoveTime(position)), farmedTime, GetMoveTime(position), avMoveTime,
				fightTime, avRegenTime, -healthDelta);
		}

		// Priorities divide by the clear time from `position`: farmedTime with the average move replaced by the move
		// from there (design 4.5).
		// The spawner's own reward (every statsBenefit 1; the boss 1e6, an item 1e4) over that clear time.
		[SimulationRebuild(Provenance.Calibrated, "simulation.slow_priority")]
		public double GetStaticPriority(Vector3 position)
		{
			return system.RebuildStaticRewardValue(spawner) * (double)spawner.Config.PriorityMultiplier / (farmedTime - avMoveTime + GetMoveTime(position));
		}

		// The recovered reward value (weighted by statsBenefit, evaluated now), or the stat's own reward for a stat;
		// the gate keeps its 1e6 either way (the rebuilt code never sets lastRewardStat).
		[SimulationRebuild(Provenance.Calibrated, "simulation.slow_priority")]
		public double GetDynamicPriority(Vector3 position, StatType? statType = null)
		{
			double reward = statType.HasValue && !system.RebuildIsCurrentBoss(spawner)
				? system.CalculateSpawnerRewardValue(spawner, statType) * (double)spawner.Config.Count
				: system.CalculateSpawnerRewardValue(spawner);
			return reward * (double)spawner.Config.PriorityMultiplier / (farmedTime - avMoveTime + GetMoveTime(position));
		}
	}
}
