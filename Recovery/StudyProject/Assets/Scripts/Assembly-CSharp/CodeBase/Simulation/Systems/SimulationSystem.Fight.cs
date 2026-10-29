using System.Collections.Generic;
using System.Globalization;
using Codebase.Enemy;
using Codebase.Simulation.Rebuild;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Simulation.Systems
{
	// Slow-phase fights (design 4.5 "战斗"; unit C7). A fight changes health, the clock and the cooldowns here; its
	// rewards, charges and gates go through RebuildApplyClear, called only in SimulateFightWithSpawner's body.
	public partial class SimulationSystem
	{
		[SimulationRebuild(Provenance.DebugOnly, "pending:simulation.slow_fight")]
		private (double, double, string) GetUnlockTimingLogData(EnemySpawner spawner)
		{
			if (spawner == null) return default;
			return (config.CurrentPlaytime, config.CurrentTotalPlaytime, string.Format(CultureInfo.InvariantCulture,
				"L{0} day {1} {2}", spawner.Config.LocationNumber, timeChunkIndex, spawner.Config.Key));
		}

		// A death (CurrentHealth <= 0 after a fight): countRespawn, full health, back at the origin; DeathPolicy
		// decides whether the spawner is skipped for the rest of the session. currentSpawner is where it happened.
		[SimulationRebuild(Provenance.Calibrated, "pending:simulation.slow_death")]
		private void ProcessDeath()
		{
			EnemySpawner spawner = currentSpawner;
			countRespawn++;
			RebuildCount("respawns");
			RebuildTraceDeath(spawner);
			playerModel.Unit.CurrentHealth = GetForecastStatValue(StatType.Health);
			if (RebuildParams.DeathPolicy == DeathPolicy.SkipSpawner && spawner != null) RebuildSlowSkipped(RebuildSlowState()).Add(spawner);
			currentSpawner = null;
			currentPosition = Vector3.zero;
		}

		// SmartPath's en-route fights (countNeighbFight), then the target. Each fight: TimeModel's duration (P and T
		// += dt / 60 in float32), CurrentHealth += healthDelta, cooldowns run down, regen over dt, then the clear.
		// A death ends the sequence (ProcessDeath); only DeathPolicy.ClearThenRespawn still clears that spawner.
		[SimulationRebuild(Provenance.Calibrated, "pending:simulation.slow_fight")]
		private void SimulateFightWithSpawner(SpawnerData bestSpawner)
		{
			SimulationSlowPolicyState state = RebuildSlowState();
			Vector3 from = currentPosition;
			double priority = bestSpawner.GetPriority(from, lastRewardStat);
			// The move to the target at the speed the tick planned with (en-route rewards do not shorten it).
			double targetMove = bestSpawner.GetMoveTime(from);
			List<SpawnerData> fights = RebuildSlowFightSequence(state, bestSpawner);
			foreach (SpawnerData data in fights)
			{
				bool target = data == bestSpawner;
				if (!target && !RebuildSlowAllowed(state, data)) continue;
				SimulationSlowFight fight = RebuildSlowFight(data, target ? targetMove : 0.0, target);
				currentSpawner = data.spawner;
				bool cleared = !fight.Died || RebuildParams.DeathPolicy == DeathPolicy.ClearThenRespawn;
				if (cleared)
				{
					RebuildApplyClear(data.spawner, 1, false);
					if (!target)
					{
						countNeighbFight++;
						RebuildCount("slow_en_route_clears");
					}
				}
				RebuildSlowFightEvent(fight, data, from, target, cleared, target ? (object)priority : null, fights.Count - 1);
				if (fight.Died)
				{
					ProcessDeath();
					return;
				}
				currentPosition = data.spawner.transform.position;
			}
		}

		// Loss over maximum health of `spawner` in the tick's data (difficulty heat map); NaN when it is not there.
		[SimulationRebuild(Provenance.DebugOnly, "pending:simulation.slow_fight")]
		private double CalcDifficultSpawner(EnemySpawner spawner, float secondsRegen = 0f)
		{
			if (spawner == null || !spawnerData.TryGetValue(spawner, out SpawnerData data)) return double.NaN;
			double health = GetForecastStatValue(StatType.Health);
			return (-data.healthDelta - GetRegen() * (double)secondsRegen) / System.Math.Max(1.0, health);
		}

		[SimulationRebuild(Provenance.DebugOnly, "pending:simulation.slow_fight")]
		private void LogSpawnerClear(EnemySpawner spawner, double healthDelta, double fightTime, double moveTime)
		{
			if (spawner == null) return;
			debugStringBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0:R}\t{1}\t{2:R}\t{3:R}\t{4:R}\n",
				config.CurrentPlaytime, spawner.Config.Key, healthDelta, fightTime, moveTime);
		}

		// ---- helpers (unit C7) ----

		// En-route spawners (SmartPath: eligible, allowed, within SmartPathRadius of the segment from the current
		// position to the target, in passing order), then the target.
		private List<SpawnerData> RebuildSlowFightSequence(SimulationSlowPolicyState state, SpawnerData target)
		{
			var fights = new List<SpawnerData>();
			if (RebuildParams.SmartPath && RebuildParams.SmartPathRadius > 0f)
			{
				var key = (currentSpawner, target.spawner);
				if (!PathSpawners.TryGetValue(key, out List<EnemySpawner> path))
				{
					CalculateSpawnerPaths(currentSpawner, target.spawner);
					path = PathSpawners[key];
				}
				foreach (EnemySpawner spawner in path)
					if (spawnerData.TryGetValue(spawner, out SpawnerData data) && data != target && RebuildSlowAllowed(state, data))
						fights.Add(data);
			}
			fights.Add(target);
			return fights;
		}

		// Seconds of one slow fight (design 4.5, TimeModel). An en-route fight has no move of its own; the target's
		// move starts at `from` (the position before the en-route fights). A death lasts the move plus the part of
		// the fight the health covered, without regen.
		private double RebuildSlowFightSeconds(SpawnerData data, double move, bool target, double fightFraction)
		{
			var rpm = GetRPMConfig();
			bool byDistance = RebuildParams.TimeModel == TimeModel.FarmedTimeMoveByDistance || RebuildParams.TimeModel == TimeModel.DecomposedMoveByDistance;
			bool decomposed = RebuildParams.TimeModel == TimeModel.Decomposed || RebuildParams.TimeModel == TimeModel.DecomposedMoveByDistance;
			double m = !target ? 0.0 : byDistance ? move : data.avMoveTime;
			double cMove = decomposed ? (double)RebuildParams.CMove : 1.0;
			if (fightFraction < 1.0) return cMove * m + rpm.Item3 * (data.fightTime * fightFraction);
			switch (RebuildParams.TimeModel)
			{
				case TimeModel.FarmedTime:
					return target ? data.farmedTime : data.farmedTime - data.avMoveTime;
				case TimeModel.FarmedTimeMoveByDistance:
					return data.farmedTime - data.avMoveTime + m;
				default:
					return (double)RebuildParams.CRegen * data.avRegenTime + (cMove * m + rpm.Item3 * data.fightTime);
			}
		}

		private SimulationSlowFight RebuildSlowFight(SpawnerData data, double distanceSeconds, bool target)
		{
			var r = new SimulationSlowFight
			{
				MaxHealth = GetForecastStatValue(StatType.Health),
				HealthBefore = playerModel.Unit.CurrentHealth,
				Regen = GetRegen(),
				PlaytimeBefore = config.CurrentPlaytime,
				TotalPlaytimeBefore = config.CurrentTotalPlaytime,
				DistanceSeconds = distanceSeconds,
			};
			double loss = -data.healthDelta;
			r.Died = r.HealthBefore + data.healthDelta <= 0.0;
			double fraction = r.Died && loss > 0.0 ? r.HealthBefore / loss : 1.0;
			bool byDistance = RebuildParams.TimeModel == TimeModel.FarmedTimeMoveByDistance || RebuildParams.TimeModel == TimeModel.DecomposedMoveByDistance;
			r.MoveSeconds = !target ? 0.0 : byDistance ? r.DistanceSeconds : data.avMoveTime;
			r.Seconds = RebuildSlowFightSeconds(data, r.DistanceSeconds, target, fraction);
			playerModel.Unit.CurrentHealth = r.HealthBefore + data.healthDelta;
			config.CurrentPlaytime = (float)(config.CurrentPlaytime + r.Seconds / 60.0);
			config.CurrentTotalPlaytime = (float)(config.CurrentTotalPlaytime + r.Seconds / 60.0);
			RebuildAdvanceCooldowns((float)r.Seconds);
			if (!r.Died) ApplyRegen(r.Seconds);
			r.HealthAfterFight = playerModel.Unit.CurrentHealth;
			totalFightTime += data.fightTime * fraction;
			totalMoveTime += r.MoveSeconds;
			countFight++;
			if (r.MaxHealth * 0.5 < loss) countFightWithStrong++;
			RebuildCount(r.Died ? "slow_deaths" : "slow_fights");
			RebuildTraceFight(data.fightTime, data.farmedTime);
			return r;
		}

		private void RebuildSlowFightEvent(SimulationSlowFight r, SpawnerData data, Vector3 from, bool target, bool cleared, object priority, int enRoute)
		{
			if (SimulationTrace.Current == null) return;
			Vector3 to = data.spawner.transform.position;
			RebuildTraceEvent("slow_fight", ("spawner", RebuildTraceId(data.spawner)), ("location", data.spawner.Config.LocationNumber),
				("kind", target ? "target" : "en_route"), ("session", lifeCycleIndex), ("boss", data.spawner == currentBoss),
				("p_before", r.PlaytimeBefore), ("t_before", r.TotalPlaytimeBefore),
				("p_after", config.CurrentPlaytime), ("t_after", config.CurrentTotalPlaytime), ("seconds", r.Seconds),
				("fight_time", data.fightTime), ("farmed_time", data.farmedTime), ("av_move_time", data.avMoveTime),
				("av_regen_time", data.avRegenTime), ("move_time", r.MoveSeconds), ("distance_time", r.DistanceSeconds),
				("health_delta", data.healthDelta), ("max_health", r.MaxHealth), ("health_before", r.HealthBefore),
				("health_after_fight", r.HealthAfterFight), ("health_after", playerModel.Unit.CurrentHealth), ("regen", r.Regen),
				("died", r.Died), ("cleared", cleared), ("priority", priority), ("en_route", enRoute),
				("from_x", from.x), ("from_z", from.z), ("to_x", to.x), ("to_z", to.z));
		}
	}
}
