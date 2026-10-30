using System;
using Codebase.Enemy;
using Codebase.Simulation.Rebuild;
using Codebase.Unit;
using Newtonsoft.Json.Linq;

namespace Codebase.Simulation.Systems
{
	public partial class SimulationSystem
	{
		// The ads reward stat: the largest RPM weighted by statsBenefit, first in StatType order on ties.
		[SimulationRebuild(Provenance.Inferred, "simulation.ads_reward_stat")]
		public StatType CalculateBestRewardStat()
		{
			StatType best = StatType.MovementSpeed;
			double bestValue = double.NegativeInfinity;
			foreach (StatType type in Enum.GetValues(typeof(StatType)))
			{
				RPMCalculatedValue.TryGetValue(type, out double rpm);
				float benefit = statsBenefit.TryGetValue(type, out float b) ? b : 1f;
				double value = rpm * benefit;
				if (value > bestValue)
				{
					bestValue = value;
					best = type;
				}
			}
			return best;
		}

		// Offline: T only, regen, cooldowns run down; no rewards (design 4.5).
		[SimulationRebuild(Provenance.Inferred, "simulation.lifecycle_time_model")]
		private void ProcessAFK(float minutes)
		{
			config.CurrentTotalPlaytime = (float)(config.CurrentTotalPlaytime + minutes);
			ApplyRegen(minutes * 60.0);
			RebuildAdvanceCooldowns((float)(minutes * 60f));
			RebuildDayAfkSteps++;
			RebuildDayOfflineMinutes += minutes;
			RebuildCount("afk_calls");
		}

		// G22: the next session ends LifeCycleInMinutes[0] active minutes after this offline.
		[SimulationRebuild(Provenance.Inferred, "simulation.lifecycle_time_model")]
		private void NextLifeCycle()
		{
			ProcessAFK(config.LifeCycleInMinutes[1]);
			lifeCyclePeriodEnd = (float)(config.CurrentPlaytime + config.LifeCycleInMinutes[0]);
			lifeCycleIndex++;
			RebuildOfflineLog.Add(new JArray(lifeCycleIndex, (double)config.CurrentPlaytime, (double)config.CurrentTotalPlaytime, (double)lifeCyclePeriodEnd));
		}

		[SimulationRebuild(Provenance.Inferred, "simulation.lifecycle_time_model")]
		private void CheckLifeCycle()
		{
			if (config.CurrentPlaytime >= lifeCyclePeriodEnd) NextLifeCycle();
		}

		// Every AdsIntervalMinutes of active time: AdsRewardMultiplierInMinutes x RPM of the best stat. The shipped
		// multiplier is 0, so it grants nothing.
		[SimulationRebuild(Provenance.Inferred, "simulation.ads")]
		private void ProcessSimulateAds()
		{
			float interval = config.AdsIntervalMinutes;
			if (!(interval > 0f)) return;
			while (config.CurrentPlaytime - lastAdsTime >= interval)
			{
				lastAdsTime = (float)(lastAdsTime + interval);
				RebuildCount("ads");
				if (config.AdsRewardMultiplierInMinutes == 0f) continue;
				StatType stat = CalculateBestRewardStat();
				RPMCalculatedValue.TryGetValue(stat, out double rpm);
				float value = (float)(config.AdsRewardMultiplierInMinutes * rpm);
				if (value > 0f)
					RebuildAddStatReward(new StatModifier { Name = "__simulationAds", StatType = stat, ModifierType = ModifierType.Flat, Source = ModifierSource.Ads }, value);
			}
		}

		[SimulationRebuild(Provenance.Inferred, "simulation.lifecycle_time_model")]
		private void ApplyRegen(double seconds)
		{
			double health = GetForecastStatValue(StatType.Health);
			playerModel.Unit.CurrentHealth = Math.Min(health, playerModel.Unit.CurrentHealth + GetRegen() * seconds);
		}

		// A cleared DAFC spawner keeps +inf (design 1.5 rule 8).
		private void RebuildAdvanceCooldowns(float seconds)
		{
			foreach (EnemySpawner spawner in enemiesModel.Spawners)
			{
				float cooldown = spawner.Cooldown;
				if (!(cooldown > 0f) || float.IsPositiveInfinity(cooldown)) continue;
				spawner.Cooldown = Math.Max(0f, (float)(cooldown - seconds));
			}
		}
	}
}
