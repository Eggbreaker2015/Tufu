using System;
using System.Collections.Generic;
using System.Linq;
using Codebase.Balance;
using Codebase.Enemy;
using Codebase.Player.Inventory;
using Codebase.Simulation.Forecast;
using Codebase.Simulation.Harness;
using Codebase.Simulation.Rebuild;
using Codebase.Unit;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Codebase.Simulation.Systems
{
	public partial class SimulationSystem
	{
		// One tick: a whole day from FastTickStartChunkOffset on, else one slow step; the stop guard makes the
		// result independent of SimulationSpeed (design 2.4, 4.4).
		[SimulationRebuild(Provenance.Inferred, "pending:simulation.execute_tick")]
		private void ExecuteTick()
		{
			if (RebuildStopped) return;
			try
			{
				bool fast = config.UseFastTick && timeChunkIndex >= config.FastTickStartChunkOffset;
				RebuildLastTickFast = fast;
				RebuildDayTicks++;
				if (fast)
				{
					wasFastTickActive = true;
					if (RebuildNullPolicy) RebuildAdvanceFastDay(0f);
					else FastTick();
				}
				else Tick();
				RebuildCount(fast ? "fast_ticks" : "slow_ticks");
				CheckSimulationStopConditions();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				RebuildFail(SimulationHarness.ExitException, "ExecuteTick: " + e.GetType().Name + ": " + e.Message);
			}
			RebuildOnTickCompleted();
		}

		// Idempotent (isSimulationPrepared). Resets the engine, position and policy state of design 4.2 to its
		// OnAwake values; PlayerModel, items, slots and GatesModel come from the fresh save of the Play session
		// (reenter_play; in-session jobs need S1). Then day 0: K2/K3(0) modifiers, RPM, the forced snapshot 0 in
		// both lists, the replay/checkpoint override.
		[SimulationRebuild(Provenance.Inferred, "pending:simulation.prepare_run")]
		private void PrepareSimulationRun()
		{
			if (isSimulationPrepared) return;
			isSimulationPrepared = true;
			try
			{
				RebuildLoadRunOptions();
				if (RebuildStopped) return;
				string unsupported = config.AutoGambling ? "AutoGambling" : config.CrystalSimulation ? "CrystalSimulation"
					: config.AutoBalanceAmulet ? "AutoBalanceAmulet" : config.GraphSimulation ? "GraphSimulation" : null;
				if (unsupported != null)
				{
					RebuildFail(SimulationHarness.ExitBadRequest, "not supported: SimulationConfig." + unsupported + " is on (design 4.5, disabled modules)");
					return;
				}
				config.CurrentPlaytime = 0f;
				config.CurrentTotalPlaytime = 0f;
				config.Snapshots = new List<SimulationSnapshot>();
				config.TimeChunkSnapshots = new List<SimulationSnapshot>();
				config.RewardList = new List<RewardEntry>();
				config.Log = string.Empty;
				SimulationExport.ResetRecorder();
				timeChunkIndex = 0;
				lifeCycleIndex = 0;
				lifeCyclePeriodEnd = config.LifeCycleInMinutes[0];
				lastAdsTime = 0f;
				wasFastTickActive = false;
				RebuildLastSnapshotPlaytime = 0f;
				currentSpawner = null;
				currentPosition = Vector3.zero;
				lastRewardStat = null;
				foreach (StatType type in statsBenefitKeys) statsBenefit[type] = 1f;
				itemBenefit.Clear();
				PathSpawners.Clear();
				statBenefitCandidates.Clear();
				totalFightTime = totalMoveTime = 0.0;
				countFight = countNeighbFight = countFightWithStrong = countRespawn = 0;
				spawnerChargesUsage.Clear();
				// Q07: a spawner's death is its cooldown, owned by the bot while IsRunning; a new save has every
				// spawner populated, whatever wall-clock alignment countdown it was given at bootstrap.
				RebuildPrepareCooldownsZeroed = 0;
				foreach (EnemySpawner spawner in enemiesModel.Spawners)
				{
					if (spawner.Config.IsEvent || spawner.Config.IsVip) continue;
					if (spawner.Cooldown != 0f) RebuildPrepareCooldownsZeroed++;
					spawner.Cooldown = 0f;
				}
				fastTickChunkStatRewards.Clear();
				fastTickChunkItemRewards.Clear();
				fastTickChunkSoulRewards.Clear();
				fastTickChunkGateRewardSpawners.Clear();
				fastTickChunkKillCount = 0;
				RebuildPendingItemCopies.Clear();
				foreach (StatType type in RPMCalculatedValue.Keys.ToList()) RPMCalculatedValue[type] = 0.0;
				foreach (string key in SoulRPMCalculatedValue.Keys.ToList()) SoulRPMCalculatedValue[key] = 0.0;
				ResetAmuletAutoBalance();
				RebuildApplyProbeGrants();
				currentBoss = RebuildFindBoss(playerModel.LastUnlockedLocation.Number);
				UpdateInterpolationStatModifier();
				playerModel.Unit.CurrentHealth = GetForecastStatValue(StatType.Health);
				CalculateRPMValueTimeChunk();
				MakeSnapshot(true);
				RebuildApplyDayOverride(0);
				RebuildTraceDayStart();
				RebuildApplyProbeSetup();
				CheckSimulationStopConditions();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				RebuildFail(SimulationHarness.ExitException, "PrepareSimulationRun: " + e.GetType().Name + ": " + e.Message);
			}
		}

		// StopAtDay: the run ends right after the forced snapshot of TargetDay; StopAtLocation: after the unlock.
		[SimulationRebuild(Provenance.Inferred, "pending:simulation.stop")]
		private void CheckSimulationStopConditions()
		{
			if (RebuildStopped) return;
			string reason = null;
			if (config.StopAtDay && timeChunkIndex >= config.TargetDay) reason = "day";
			else if (config.StopAtLocation && playerModel.LastUnlockedLocation.Number >= config.TargetLocationNumber) reason = "location";
			if (reason == null) return;
			RebuildStopped = true;
			RebuildStopReason = reason;
			config.IsRunning = false;
		}

		// Slow phase: one fight or one idle step (design 4.4).
		[SimulationRebuild(Provenance.Inferred, "pending:simulation.slow_tick")]
		private void Tick()
		{
			CalculateSimulationData();
			SimulationExport.SlowTickBasis = playerModel.Unit.Stats.ToDictionary(stat => (int)stat.Type, stat => GetForecastStatValue(stat.Type));
			RebuildApplyReplayCaches();
			CalculateSpawnerData(false);
			RebuildTraceEligible();
			SpawnerData best = null;
			if (!RebuildNullPolicy)
			{
				CalculateStatBenefit();
				best = FindBestSpawner();
			}
			if (best != null) SimulateFightWithSpawner(best);
			else RebuildIdle(RebuildIdleMinutes());
			MakeSnapshot();
			CheckLifeCycle();
			ProcessSimulateAds();
			UpdateSpawnerChargesChunk();
		}

		// Idle until the next early snapshot is due, at most TimeStep: P and T together, regen, cooldowns.
		[SimulationRebuild(Provenance.Inferred, "pending:simulation.idle_step")]
		private void RebuildIdle(float minutes)
		{
			config.CurrentPlaytime = (float)(config.CurrentPlaytime + minutes);
			config.CurrentTotalPlaytime = (float)(config.CurrentTotalPlaytime + minutes);
			ApplyRegen(minutes * 60.0);
			RebuildAdvanceCooldowns((float)(minutes * 60f));
			RebuildCount("idle_steps");
		}

		private float RebuildIdleMinutes()
		{
			float step = config.TimeStep / 60f;
			float untilSnapshot = (float)(RebuildNextSnapshotDue() - config.CurrentPlaytime);
			return untilSnapshot > 0f && untilSnapshot < step ? untilSnapshot : step;
		}

		// The policy-independent part of a fast day (design 4.4): flush, active minutes, 120-minute offline steps
		// until the day index changes, then the day change. The minimal FastTick passes 0 active minutes.
		[SimulationRebuild(Provenance.Inferred, "pending:simulation.fast_day_advance")]
		private void RebuildAdvanceFastDay(float activeMinutes)
		{
			FlushFastTickChunkRewards();
			config.CurrentPlaytime = (float)(config.CurrentPlaytime + activeMinutes);
			config.CurrentTotalPlaytime = (float)(config.CurrentTotalPlaytime + activeMinutes);
			int day = timeChunkIndex;
			int steps = 0;
			while (RebuildDayIndex() == day)
			{
				ProcessAFK(RebuildParams.FastAfkStepMinutes);
				if (++steps > 100000)
				{
					RebuildFail(SimulationHarness.ExitException, "RebuildAdvanceFastDay: T no longer advances at " + config.CurrentTotalPlaytime);
					return;
				}
			}
			UpdateSpawnerChargesChunk();
		}

		// The day index floor(T / SpawnerChargesChunkMinutes) in float, as GetBalanceTimingMinutes reads it.
		private int RebuildDayIndex()
		{
			return (int)Mathf.Floor((float)(config.CurrentTotalPlaytime / config.SpawnerChargesChunkMinutes));
		}

		[SimulationRebuild(Provenance.DebugOnly, "pending:simulation.next_location_timing")]
		public float GetNextLocationInterpolationTiming(int locationNumber)
		{
			int next = locationNumber + 1;
			if (next < 2 || next - 2 >= combatBalanceConfig.IslandTargetDays.Length) return float.NaN;
			return ConvertIslandTimingToDays(next);
		}

		// The body ForecastSnapshots inlines (original 0x157ad44).
		[SimulationRebuild(Provenance.Inferred, "pending:timeline.island_timing_to_days")]
		public float ConvertIslandTimingToDays(float islandTiming)
		{
			return ForecastSnapshots.ConvertIslandTimingToDays(combatBalanceConfig, islandTiming);
		}

		// Spawners other than a cleared DAFC (+inf) respawn at the day change.
		[SimulationRebuild(Provenance.Inferred, "pending:simulation.day_change")]
		private void ResetCooldowns()
		{
			foreach (EnemySpawner spawner in enemiesModel.Spawners)
			{
				if (spawner.Config.IsEvent || spawner.Config.IsVip || float.IsPositiveInfinity(spawner.Cooldown)) continue;
				spawner.Cooldown = 0f;
			}
		}

		// The only place a day changes (design 4.3). Order RpmFirst (G20): RPM and DpsData under the old
		// K2/K3(d-1) modifiers (K4/K5 already read d), then the K(d) modifiers, the forced snapshot, the override.
		[SimulationRebuild(Provenance.Inferred, "pending:simulation.day_change_order")]
		private void UpdateSpawnerChargesChunk()
		{
			int day = RebuildDayIndex();
			if (day == timeChunkIndex) return;
			RebuildTraceDayEnd();
			RebuildDayLog.Add(new JArray(timeChunkIndex, RebuildLastTickFast ? 1 : 0, RebuildDayTicks, RebuildDayAfkSteps, RebuildDayOfflineMinutes,
				(double)config.CurrentPlaytime, (double)config.CurrentTotalPlaytime));
			RebuildDayTicks = RebuildDayAfkSteps = 0;
			RebuildDayOfflineMinutes = 0.0;
			timeChunkIndex = day;
			spawnerChargesUsage.Clear();
			ResetCooldowns();
			if (RebuildParams.DpsCacheProvenance == DpsCacheProvenance.ModifierFirst)
			{
				UpdateInterpolationStatModifier();
				RecalculateChunkRpmValues();
			}
			else
			{
				RecalculateChunkRpmValues();
				UpdateInterpolationStatModifier();
			}
			ProcessAmuletAutoBalanceTargets(day);
			ProcessCrystalSimulationChunk();
			MakeSnapshot(true);
			// After the forced snapshot, as on day 0, so day d's snapshot keeps the closed-loop state (design 4.6;
			// the 4.4 pseudo-code has it before the snapshot).
			RebuildApplyDayOverride(day);
			RebuildTraceDayStart();
			RebuildCount("day_changes");
		}

		// RPM (and with it DpsData/DefenseData) for the new day; StaleTickCache keeps the last tick's caches.
		[SimulationRebuild(Provenance.Inferred, "pending:simulation.day_change_order")]
		private void RecalculateChunkRpmValues()
		{
			if (RebuildParams.DpsCacheProvenance != DpsCacheProvenance.StaleTickCache)
			{
				CalculateRPMValueTimeChunk();
				return;
			}
			RebuildPerFrameCaches();
			CalculateSpawnerData(true);
			CalculateSoulRPMValues();
			foreach (StatType type in Enum.GetValues(typeof(StatType))) CalculateRPMValueTimeChunk(type);
		}

		// Charges count units: ChargesBeforeBigRespawn = 3 * Count.
		[SimulationRebuild(Provenance.Inferred, "pending:simulation.charges_per_clear")]
		private int GetSpawnerChargesPerClear(EnemySpawner spawner)
		{
			return spawner.Config.Count;
		}

		[SimulationRebuild(Provenance.Inferred, "pending:simulation.forecast_unlocked_location")]
		public int GetForecastUnlockedLocationNumber()
		{
			return playerModel.LastUnlockedLocation.Number;
		}

		// Wrapper 0x13ee9d0 only tail-calls the config method (ForecastSnapshots).
		[SimulationRebuild(Provenance.Inferred, "balance.total_coefficient")]
		public float GetSimulationInterpolationCoefficient(float timing, BalanceInterpolationType type, Rarity rarity = Rarity.Rare)
		{
			return combatBalanceConfig.GetInterpolationCoefficient(timing, type, rarity);
		}

		// Replaces the two __simulation* modifiers by K2(d) (PercentAdditive, 9 main stats) and K3(d)
		// (PercentMultiplicative, Health/HealthRegen/LifestealFlat); adding again would merge the values.
		[SimulationRebuild(Provenance.Inferred, "simulation.snapshot_stat_composition")]
		private void UpdateInterpolationStatModifier()
		{
			float timing = timeChunkIndex;
			float k2 = GetSimulationInterpolationCoefficient(timing, BalanceInterpolationType.StatsFromFeaturesFlat);
			float k3 = GetSimulationInterpolationCoefficient(timing, BalanceInterpolationType.StatModifier);
			foreach (Stat stat in playerModel.Unit.Stats)
			{
				foreach (StatModifier old in stat.Modifiers.Where(m => m.Name == SimulationStatsFromFeaturesFlatModifierName || m.Name == SimulationInterpolationStatModifierName).ToList())
					stat.RemoveModifier(old);
				if (UsesInterpolationStats(stat.Type))
					stat.AddModifier(new StatModifier { Name = SimulationStatsFromFeaturesFlatModifierName, StatType = stat.Type, ModifierType = ModifierType.PercentAdditive, Source = ModifierSource.Reward, Value = k2 });
				if (DirectInterpolationStats.Contains(stat.Type))
					stat.AddModifier(new StatModifier { Name = SimulationInterpolationStatModifierName, StatType = stat.Type, ModifierType = ModifierType.PercentMultiplicative, Source = ModifierSource.Reward, Value = k3 });
			}
			RebuildK2 = k2;
			RebuildK3 = k3;
			RebuildModifierDay = timeChunkIndex;
		}

		// StatBase.CalculateValue without the two __simulation* modifiers and without equipment (Item) modifiers:
		// the snapshot's Value (design 4.5, X6). Same order of operations as the original.
		[SimulationRebuild(Provenance.Inferred, "simulation.snapshot_stat_composition")]
		private double GetSimulationSnapshotStatValue(Stat stat)
		{
			double flat = 0.0;
			double additiveMultiplier = 1.0;
			double multiplicativeMultiplier = 1.0;
			double eventMultiplier = 1.0;
			var preAddPostMultBySource = new List<KeyValuePair<ModifierSource, double>>();
			foreach (StatModifier modifier in stat.Modifiers)
			{
				if (modifier.Name == SimulationStatsFromFeaturesFlatModifierName || modifier.Name == SimulationInterpolationStatModifierName
				    || modifier.Source == ModifierSource.Item) continue;
				switch (modifier.ModifierType)
				{
					case ModifierType.Flat: flat += modifier.Value; break;
					case ModifierType.PercentAdditive: additiveMultiplier += modifier.Value; break;
					case ModifierType.PercentMultiplicative: multiplicativeMultiplier *= modifier.Value; break;
					case ModifierType.PreAdditivePostMultiplicative:
						int index = preAddPostMultBySource.FindIndex(pair => pair.Key == modifier.Source);
						if (index < 0) preAddPostMultBySource.Add(new KeyValuePair<ModifierSource, double>(modifier.Source, 0.0 + modifier.Value));
						else preAddPostMultBySource[index] = new KeyValuePair<ModifierSource, double>(modifier.Source, preAddPostMultBySource[index].Value + modifier.Value);
						break;
					case ModifierType.EventPercentModifier:
						eventMultiplier *= 1.0 + StatModifier.GetEventPercentBonusPercent(modifier.Value) * 0.01;
						break;
				}
			}
			double result = eventMultiplier * multiplicativeMultiplier * additiveMultiplier * (stat.BaseValue + flat);
			foreach (var pair in preAddPostMultBySource) result *= 1.0 + pair.Value;
			return result;
		}

		[SimulationRebuild(Provenance.Inferred, "simulation.snapshot_stat_composition")]
		private bool UsesInterpolationStats(StatType statType)
		{
			return MainInterpolationStats.Contains(statType);
		}
	}
}
