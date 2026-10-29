using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Codebase.Enemy;
using Codebase.Player.Inventory;
using Codebase.Simulation.Harness;
using Codebase.Simulation.Rebuild;
using Codebase.Unit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Codebase.Simulation.Systems
{
	// Slow-phase target selection (design 4.5; unit C7): on the tick's bot-filter spawnerData, the allowed spawner
	// (lethality gate, DeathPolicy's skipped spawners) with the highest SpawnerData.GetPriority, the first on ties.
	public partial class SimulationSystem
	{
		[SimulationRebuild(Provenance.DebugOnly, "simulation.slow_priority")]
		public EnemySpawner GetBestSpawnerForInfo()
		{
			if (spawnerData == null || spawnerData.Count == 0) return null;
			return RebuildFindBest(RebuildSlowState())?.spawner;
		}

		// PathSpawners[(A, B)]: the registered spawners within SmartPathRadius of the segment from A (the origin
		// when null) to B, in the order the player passes them. Pure geometry, cached per pair; eligibility is
		// applied when the path is walked (equivalent to caching per eligible set, design 4.5).
		[SimulationRebuild(Provenance.Calibrated, "simulation.smart_path")]
		private void CalculateSpawnerPaths(EnemySpawner A, EnemySpawner B)
		{
			Vector3 from = A != null ? A.transform.position : Vector3.zero;
			Vector3 segment = B.transform.position - from;
			float lengthSq = segment.sqrMagnitude;
			float radius = RebuildParams.SmartPathRadius;
			var found = new List<(float T, EnemySpawner Spawner)>();
			foreach (EnemySpawner spawner in sortedSpawners)
			{
				if (spawner == A || spawner == B || spawner.Config.IsEvent || spawner.Config.IsVip) continue;
				Vector3 p = spawner.transform.position;
				float t = lengthSq > 0f ? Mathf.Clamp01(Vector3.Dot(p - from, segment) / lengthSq) : 0f;
				if (Vector3.Distance(p, from + segment * t) <= radius) found.Add((t, spawner));
			}
			PathSpawners[(A, B)] = found.OrderBy(x => x.T).Select(x => x.Spawner).ToList();
			RebuildCount("smart_path_scans");
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

		// statsBenefit, once per session or day (design 4.5, 4.10). Constant: the OnAwake value 1.0 for every stat.
		// MarginalKillTime: per reward stat, the relative drop of the summed farmedTime of the next
		// StatBenefitCandidates spawners (OptimumTime order) the bot cannot farm yet when one typical reward of that
		// stat is added, normalised to mean 1 over the reward stats (at least 0.01).
		[SimulationRebuild(Provenance.Calibrated, "simulation.stat_benefit")]
		private void CalculateStatBenefit()
		{
			SimulationSlowPolicyState state = RebuildSlowState();
			if (state.BenefitSession == lifeCycleIndex && state.BenefitDay == timeChunkIndex) return;
			state.BenefitSession = lifeCycleIndex;
			state.BenefitDay = timeChunkIndex;
			RebuildCount("stat_benefit_evals");
			foreach (StatType type in statsBenefitKeys) statsBenefit[type] = 1f;
			int candidates = RebuildParams.StatBenefitModel == StatBenefitModel.MarginalKillTime ? RebuildMarginalStatBenefit(state) : 0;
            if (RebuildParams.StatBenefitModel == StatBenefitModel.RpmNormalized)
            {
                var positive = statsBenefitKeys.Where(k => RPMCalculatedValue.TryGetValue(k, out double v) && v > 0.0 && !double.IsInfinity(v) && !double.IsNaN(v)).ToList();
                if (positive.Count > 0)
                {
                    double mean = positive.Average(k => RPMCalculatedValue[k]);
                    foreach (StatType type in positive) statsBenefit[type] = (float)(mean / RPMCalculatedValue[type]);
                }
            }
			var text = new StringBuilder();
			foreach (StatType type in statsBenefitKeys)
				if (statsBenefit[type] != 1f)
					text.Append((int)type).Append('=').Append(SimulationJson.FormatFloat(statsBenefit[type])).Append(';');
			RebuildTraceEvent("stat_benefit", ("session", lifeCycleIndex), ("model", (int)RebuildParams.StatBenefitModel),
				("candidates", candidates), ("benefits", text.ToString()));
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

		// Tick calls it after CalculateSpawnerData(false) and CalculateStatBenefit; null makes the tick idle.
		[SimulationRebuild(Provenance.Calibrated, "simulation.slow_priority")]
		private SpawnerData FindBestSpawner()
		{
			SimulationSlowPolicyState state = RebuildSlowState();
			RebuildRunSlowPolicyProbe(state);
			if (spawnerData.Count == 0)
			{
				RebuildCount("slow_pick_none_eligible");
				return null;
			}
			SpawnerData best = RebuildFindBest(state);
			if (best == null) RebuildCount("slow_pick_none_allowed");
			RebuildRecordGate(state, best);
			return best;
		}

		[SimulationRebuild(Provenance.DebugOnly, "simulation.slow_priority")]
		public bool TryGetSpawnerPriority(EnemySpawner spawner, out string priorityInfo)
		{
			priorityInfo = null;
			if (spawner == null || spawnerData == null || !spawnerData.TryGetValue(spawner, out SpawnerData data)) return false;
			priorityInfo = data.GetPriorityInfo(currentPosition);
			return true;
		}

		// ---- helpers (unit C7) ----

		private SimulationSlowPolicyState RebuildSlowState()
		{
			return SimulationSlowPolicyState.For(this, RebuildParams);
		}

		// For SpawnerData.GetPriority.
		internal PriorityFamily RebuildPriorityFamily()
		{
			return RebuildParams.PriorityFamily;
		}

		// For SpawnerData.GetDynamicPriority.
		internal bool RebuildIsCurrentBoss(EnemySpawner spawner)
		{
			return spawner != null && spawner == currentBoss;
		}

		// CalculateSpawnerRewardValue(spawner) with every benefit at 1 (the boss 1e6, an item 1e4, else the stat
		// rewards), times Count: SpawnerData.GetStaticPriority.
		internal double RebuildStaticRewardValue(EnemySpawner spawner)
		{
			if (spawner == currentBoss) return 1000000.0;
			EnemySpawnerConfig def = spawner.Config;
			double value = 0.0;
			if (def.StatRewards != null)
				foreach (StatModifier reward in def.StatRewards) value += (double)reward.Value;
			if (def.ItemReward != null) value = 10000.0;
			return value * (double)def.Count;
		}

		private SpawnerData RebuildFindBest(SimulationSlowPolicyState state)
		{
			return RebuildSelectBest(spawnerData.Values, data => data.GetPriority(currentPosition, lastRewardStat),
				data => RebuildSlowAllowed(state, data));
		}

		// The highest priority among the allowed, in `ordered` order: a later equal priority does not replace the
		// first (spawnerData is filled in sortedSpawners order). NaN never wins.
		private SpawnerData RebuildSelectBest(IEnumerable<SpawnerData> ordered, Func<SpawnerData, double> priority, Func<SpawnerData, bool> allowed)
		{
			SpawnerData best = null;
			double bestValue = 0.0;
			foreach (SpawnerData data in ordered)
			{
				if (!allowed(data)) continue;
				double value = priority(data);
				if (double.IsNaN(value)) continue;
				if (best == null || value > bestValue)
				{
					best = data;
					bestValue = value;
				}
			}
			return best;
		}

		// Not refused by the lethality gate and not lost to this session (DeathPolicy.SkipSpawner).
		private bool RebuildSlowAllowed(SimulationSlowPolicyState state, SpawnerData data)
		{
			if (RebuildSlowSkipped(state).Contains(data.spawner)) return false;
			return !RebuildSlowRefused(data, RebuildParams.SlowLethalityGate);
		}

		private HashSet<EnemySpawner> RebuildSlowSkipped(SimulationSlowPolicyState state)
		{
			if (state.SkippedSession != lifeCycleIndex)
			{
				state.Skipped.Clear();
				state.SkippedSession = lifeCycleIndex;
			}
			return state.Skipped;
		}

		// SlowLethalityGate (design 4.5): loss = -healthDelta of the tick's spawner data.
		private bool RebuildSlowRefused(SpawnerData data, SlowLethalityGate gate)
		{
			double loss = -data.healthDelta;
			switch (gate)
			{
				case SlowLethalityGate.SameAsFast:
					return SimulationSlowPolicyState.KillLossMultiplier(RebuildParams) * GetForecastStatValue(StatType.Health) < loss;
				case SlowLethalityGate.CurrentHealth:
					return loss >= playerModel.Unit.CurrentHealth;
				default:
					return false;
			}
		}

		// Counts every tick's boss decision; the first of each session goes to event slow_gate with the predicates of
		// all three gates.
		private void RebuildRecordGate(SimulationSlowPolicyState state, SpawnerData best)
		{
			if (currentBoss == null || !spawnerData.TryGetValue(currentBoss, out SpawnerData boss)) return;
			bool allowed = RebuildSlowAllowed(state, boss);
			RebuildCount(!allowed ? "slow_boss_refused" : best == boss ? "slow_boss_chosen" : "slow_boss_allowed_not_chosen");
			if (SimulationHarness.CurrentJob?.SlowPolicy?.Selection == true)
			{
				var candidates = new JArray();
				foreach (SpawnerData data in spawnerData.Values)
					candidates.Add(new JObject { ["spawner"] = RebuildTraceId(data.spawner),
						["farmed_time"] = data.farmedTime, ["av_move_time"] = data.avMoveTime,
						["distance_time"] = data.GetMoveTime(currentPosition), ["loss"] = -data.healthDelta,
						["skipped"] = RebuildSlowSkipped(state).Contains(data.spawner) });
				RebuildTraceEvent("slow_selection", ("boss", RebuildTraceId(currentBoss)), ("best", RebuildTraceId(best?.spawner)),
					("max_health", GetForecastStatValue(StatType.Health)), ("current_health", playerModel.Unit.CurrentHealth),
					("kill_loss_multiplier", SimulationSlowPolicyState.KillLossMultiplier(RebuildParams)),
					("candidates", candidates.ToString(Formatting.None)));
			}
			if (state.GateEventSession == lifeCycleIndex && state.GateEventBoss == currentBoss) return;
			state.GateEventSession = lifeCycleIndex;
			state.GateEventBoss = currentBoss;
			RebuildTraceEvent("slow_gate", ("spawner", RebuildTraceId(currentBoss)), ("location", currentBoss.Config.LocationNumber),
				("session", lifeCycleIndex), ("loss", -boss.healthDelta), ("max_health", GetForecastStatValue(StatType.Health)),
				("current_health", playerModel.Unit.CurrentHealth), ("fight_time", boss.fightTime),
				("kill_loss_multiplier", SimulationSlowPolicyState.KillLossMultiplier(RebuildParams)),
				("refused_same_as_fast", RebuildSlowRefused(boss, SlowLethalityGate.SameAsFast)),
				("refused_current_health", RebuildSlowRefused(boss, SlowLethalityGate.CurrentHealth)),
				("skipped", RebuildSlowSkipped(state).Contains(currentBoss)), ("chosen", best == boss));
		}

		// MarginalKillTime (see CalculateStatBenefit). Restores the tick's caches and bot spawnerData afterwards.
		private int RebuildMarginalStatBenefit(SimulationSlowPolicyState state)
		{
			int unlocked = playerModel.LastUnlockedLocation.Number;
			var farmable = new HashSet<EnemySpawner>(spawnerData.Values.Where(data => RebuildSlowAllowed(state, data)).Select(data => data.spawner));
			var open = sortedSpawners.Where(s => !s.Config.IsEvent && !s.Config.IsVip && s.Config.LocationNumber <= unlocked).ToList();
			statBenefitCandidates.Clear();
			statBenefitCandidates.AddRange(open.Where(s => !s.Config.DisableAfterFirstClear && !farmable.Contains(s))
				.OrderBy(s => s.Config.OptimumTime).Take(Math.Max(0, RebuildParams.StatBenefitCandidates)));
			if (statBenefitCandidates.Count == 0) return 0;
			var amounts = new SortedDictionary<StatType, List<double>>();
			var kinds = new Dictionary<StatType, ModifierType>();
			foreach (EnemySpawner s in open)
			{
				if (s.Config.StatRewards == null) continue;
				foreach (StatModifier reward in s.Config.StatRewards)
				{
					if (!amounts.TryGetValue(reward.StatType, out List<double> list)) amounts[reward.StatType] = list = new List<double>();
					list.Add((double)reward.Value * s.Config.Count);
					if (!kinds.ContainsKey(reward.StatType)) kinds[reward.StatType] = reward.ModifierType;
				}
			}
			const string probeName = "__simulationStatBenefitProbe";
			double baseCost = RebuildCandidateCost();
			var gains = new Dictionary<StatType, double>();
			foreach (var pair in amounts)
			{
				Stat stat = playerModel.Unit.Stats.FirstOrDefault(x => x.Type == pair.Key);
				if (stat == null) continue;
				pair.Value.Sort();
				float amount = (float)pair.Value[(pair.Value.Count - 1) / 2];
				stat.AddModifier(new StatModifier { Name = probeName, StatType = pair.Key, ModifierType = kinds[pair.Key], Source = ModifierSource.Reward, Value = amount });
				double cost = RebuildCandidateCost();
				foreach (StatModifier m in stat.Modifiers.Where(m => m.Name == probeName).ToList()) stat.RemoveModifier(m);
				gains[pair.Key] = baseCost > 0.0 ? Math.Max(0.0, (baseCost - cost) / baseCost) : 0.0;
			}
			double mean = gains.Count > 0 ? gains.Values.Average() : 0.0;
			if (mean > 0.0)
				foreach (var pair in gains) statsBenefit[pair.Key] = (float)Math.Max(0.01, pair.Value / mean);
			CalculateSimulationData();
			RebuildApplyReplayCaches();
			CalculateSpawnerData(false);
			return statBenefitCandidates.Count;
		}

		// Summed farmedTime of statBenefitCandidates in the current state; a candidate the GPM data drops
		// (fightTime >= 300) costs the bound rpm.Item3 * 300 + avMoveTime + rpm.Item2.
		private double RebuildCandidateCost()
		{
			CalculateSimulationData();
			RebuildApplyReplayCaches();
			var rpm = GetRPMConfig();
			double cap = rpm.Item3 * 300.0 + rpm.Item1 / GetForecastStatSpecificValue(StatType.MovementSpeed) + rpm.Item2;
			double total = 0.0;
			foreach (EnemySpawner s in statBenefitCandidates)
				total += spawnerData.TryGetValue(s, out SpawnerData data) ? data.farmedTime : cap;
			return total;
		}

		// job.slow_policy_probe.selection (tests only), at the first FindBestSpawner of the run, on the live
		// spawnerData: every priority equal (the first must win), the first and the last tied above the rest,
		// nothing allowed, nothing eligible (FindBestSpawner's own selection on an empty spawnerData). Event
		// slow_policy_probe; the run continues unchanged.
		private void RebuildRunSlowPolicyProbe(SimulationSlowPolicyState state)
		{
			if (state.ProbeDone) return;
			state.ProbeDone = true;
			SimulationRunRequest.SlowPolicyProbe probe = SimulationHarness.CurrentJob?.SlowPolicy;
			if (probe == null || !probe.Selection) return;
			List<SpawnerData> ordered = spawnerData.Values.ToList();
			SpawnerData first = ordered.FirstOrDefault(), last = ordered.LastOrDefault();
			SpawnerData tie = RebuildSelectBest(ordered, data => 1.0, data => true);
			SpawnerData tieEnds = RebuildSelectBest(ordered, data => data == first || data == last ? 2.0 : 1.0, data => true);
			SpawnerData none = RebuildSelectBest(ordered, data => 1.0, data => false);
			var saved = ordered.Select(data => data.spawner).ToList();
			spawnerData.Clear();
			SpawnerData cleared = RebuildFindBest(state);
			foreach (SpawnerData data in ordered) spawnerData[data.spawner] = data;
			var registered = sortedSpawners.Where(saved.Contains).Select(RebuildTraceId);
			// The gate's priority with a reward stat (lastRewardStat) keeps the gate's 1e6: `first` posing as the gate.
			EnemySpawner boss = currentBoss;
			currentBoss = first?.spawner;
			bool bossStat = first != null && first.GetPriority(currentPosition, StatType.Health) == first.GetPriority(currentPosition);
			currentBoss = boss;
			RebuildTraceEvent("slow_policy_probe", ("eligible", string.Join("\n", saved.Select(RebuildTraceId))),
				("sorted_eligible", string.Join("\n", registered)), ("tie", RebuildTraceId(tie?.spawner)),
				("tie_first_last", RebuildTraceId(tieEnds?.spawner)), ("none_allowed_is_null", none == null),
				("empty_is_null", cleared == null), ("restored", spawnerData.Keys.SequenceEqual(saved)),
				("best", RebuildTraceId(RebuildFindBest(state)?.spawner)), ("boss_stat_keeps_gate", bossStat));
		}
	}
}
