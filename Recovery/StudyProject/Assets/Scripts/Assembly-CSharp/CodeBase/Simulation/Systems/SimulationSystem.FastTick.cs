using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Codebase.Balance;
using Codebase.Enemy;
using Codebase.Player.Inventory;
using Codebase.Simulation.Harness;
using Codebase.Simulation.Rebuild;
using Codebase.Unit;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Codebase.Simulation.Systems
{
	public partial class SimulationSystem
	{
		// A whole fast day (design 4.4, 4.5; unit C5): planning rounds on the day's state, each over the bot data
		// (CalculateSpawnerData(false)) with every eligible spawner cleared for all its charges through
		// SimulateFightWithSpawnerFast; a gate cleared in a round opens its island at once (RebuildFlushFastTickGates)
		// and the day plans again, until a round opens nothing. Rewards wait in the five accumulated fields for
		// RebuildAdvanceFastDay's flush; the active minutes (ActiveMinutesModel) are an output only (design 4.3).
		[SimulationRebuild(Provenance.Calibrated, "pending:simulation.fast_tick")]
		private void FastTick()
		{
			SimulationFastTickRecorder recorder = SimulationFastTickRecorder.For(SimulationHarness.CurrentJob);
			string error = RebuildParams.ValidateFastTick() ?? recorder?.Error;
			if (error != null)
			{
				RebuildFail(error.StartsWith("params", StringComparison.Ordinal) ? SimulationHarness.ExitBadRequest : SimulationHarness.ExitException, error);
				return;
			}
			int day = timeChunkIndex;
			int locationStart = playerModel.LastUnlockedLocation.Number;
			float playtime = config.CurrentPlaytime;
			var rounds = new JArray();
			var roundUnlocks = new JArray();
			var toured = new List<EnemySpawner>();
			double fightSeconds = 0.0, farmedSeconds = 0.0;
			long clears = 0, kills = 0, rejected = 0;
			for (int round = 1; ; round++)
			{
				List<StatModifier> afterPass = RebuildParams.KillState == KillState.AfterPass && round > 1 ? RebuildFastAfterPassAdd() : null;
				CalculateSimulationData();
				RebuildApplyReplayCaches();
				CalculateSpawnerData(false);
				if (round == 1) RebuildTraceEligible();
				int location = playerModel.LastUnlockedLocation.Number;
				double health = GetForecastStatValue(StatType.Health);
				var eligible = new List<string>();
				long roundClears = 0, roundKills = 0;
				double roundFight = 0.0, roundFarmed = 0.0;
				foreach (SpawnerData data in spawnerData.Values.ToList())
				{
					EnemySpawner spawner = data.spawner;
					EnemySpawnerConfig def = spawner.Config;
					string id = RebuildTraceId(spawner);
					spawnerChargesUsage.TryGetValue(spawner, out int chargesBefore);
					int fights = RebuildFastFights(spawner);
					bool killed = fights > 0 && SimulateFightWithSpawnerFast(data, fights);
					spawnerChargesUsage.TryGetValue(spawner, out int chargesAfter);
					recorder?.AddSpawnerRow(day, round, id, def.LocationNumber, def.DisableAfterFirstClear, killed || RebuildFastKillable(data),
						killed ? fights : 0, data.fightTime, data.farmedTime, -data.healthDelta, health,
						def.Count, GetSpawnerChargesLimit(spawner), chargesBefore, chargesAfter);
					if (!killed)
					{
						if (fights > 0) rejected++;
						continue;
					}
					eligible.Add(id);
					roundClears += fights;
					roundKills += (long)fights * def.Count;
					roundFight += fights * data.fightTime;
					roundFarmed += fights * data.farmedTime;
					RebuildTraceFight(fights * data.fightTime, fights * data.farmedTime);
					if (!def.DisableAfterFirstClear) toured.Add(spawner);
				}
				if (afterPass != null) RebuildFastAfterPassRemove(afterPass);
				clears += roundClears;
				kills += roundKills;
				fightSeconds += roundFight;
				farmedSeconds += roundFarmed;
				int unlockedBefore = playerModel.LastUnlockedLocation.Number;
				if (RebuildParams.PlanNewIslandsSameDay) RebuildFlushFastTickGates();
				int unlockedAfter = playerModel.LastUnlockedLocation.Number;
				for (int n = unlockedBefore + 1; n <= unlockedAfter; n++) roundUnlocks.Add(n);
				rounds.Add(new JObject
				{
					["location"] = location,
					["bot"] = spawnerData.Count,
					["eligible"] = eligible.Count,
					["signature"] = SimulationTrace.SetSignature(eligible),
					["clears"] = roundClears,
					["kills"] = roundKills,
					["fight_seconds"] = SimulationRuntimeProbes.D(roundFight),
					["farmed_seconds"] = SimulationRuntimeProbes.D(roundFarmed),
					["unlocked"] = unlockedAfter,
				});
				RebuildCount("fast_rounds");
				if (unlockedAfter == unlockedBefore) break;
				if (round >= 64)
				{
					RebuildFail(SimulationHarness.ExitException, "FastTick: day " + day + " still opens islands after " + round + " planning rounds");
					return;
				}
			}
			float activeMinutes = RebuildFastActiveMinutes(farmedSeconds, fightSeconds, toured, out double tour, out double speed);
			JObject record = recorder == null ? null : new JObject
			{
				["day"] = day,
				["playtime"] = SimulationRuntimeProbes.F(playtime),
				["location_start"] = locationStart,
				["location_end"] = playerModel.LastUnlockedLocation.Number,
				["kill_loss"] = SimulationRuntimeProbes.D(RebuildFastKillLoss()),
				["rounds"] = rounds,
				["round_unlocks"] = roundUnlocks,
				["clears"] = clears,
				["kills"] = kills,
				["kill_gate_rejections"] = rejected,
				["fight_seconds"] = SimulationRuntimeProbes.D(fightSeconds),
				["farmed_seconds"] = SimulationRuntimeProbes.D(farmedSeconds),
				["tour"] = SimulationRuntimeProbes.D(tour),
				["speed"] = SimulationRuntimeProbes.D(speed),
				["active_minutes"] = SimulationRuntimeProbes.F(activeMinutes),
				["stats"] = new JObject(fastTickChunkStatRewards.OrderBy(p => (int)p.Key).Select(p =>
					new JProperty(((int)p.Key).ToString(CultureInfo.InvariantCulture), SimulationRuntimeProbes.D(p.Value.Value)))),
				["items"] = new JObject(fastTickChunkItemRewards.OrderBy(p => p.Key.Key, StringComparer.Ordinal).Select(p =>
					new JProperty(p.Key.Key, SimulationRuntimeProbes.D(p.Value)))),
				["souls"] = new JObject(fastTickChunkSoulRewards.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p =>
					new JProperty(p.Key, SimulationRuntimeProbes.D(p.Value)))),
				["kill_count"] = fastTickChunkKillCount,
				["gate_spawners"] = fastTickChunkGateRewardSpawners.Count,
			};
			recorder?.BeginFlush();
			RebuildAdvanceFastDay(activeMinutes);
			if (recorder == null) return;
			record["flush_unlocks"] = new JArray(recorder.FlushUnlocks.Cast<object>().ToArray());
			record["flush_stat_modifiers"] = recorder.FlushStatModifiers;
			recorder.Count("days");
			recorder.Count("rounds", rounds.Count);
			recorder.Count("round_unlocks", roundUnlocks.Count);
			recorder.Count("flush_unlocks", recorder.FlushUnlocks.Count);
			recorder.WriteDay(record);
		}

		// Design 4.5: the bot data's spawner is farmed only if !(L * H < loss), loss = -healthDelta,
		// H = GetForecastStatValue(Health): the text of CalculateRPMValueTimeChunk's survivability test, used as the
		// bot's kill gate (fitted on the data: 37/41 intervals against 26/41 without it). fightTime < 300 is already
		// CalculateSpawnerData's. Not killable: false; else all the fights go through the write point (accumulate).
		[SimulationRebuild(Provenance.Inferred, "pending:simulation.fast_kill_gate")]
		private bool SimulateFightWithSpawnerFast(SpawnerData bestSpawner, int fightsCount)
		{
			if (bestSpawner == null || fightsCount <= 0) return false;
			if (!RebuildFastKillable(bestSpawner))
			{
				RebuildCount("fast_kill_gate_rejections");
				return false;
			}
			RebuildApplyClear(bestSpawner.spawner, fightsCount, true);
			return true;
		}

		// Writes all five accumulated fields of the original (design 4.5): stats (one double per stat), the
		// fractional item copies, souls, the gate spawners, the kill count. Nothing reaches the player before the
		// flush. Currencies have no original field and are recorded at once, as the immediate path records them.
		[SimulationRebuild(Provenance.Inferred, "pending:simulation.fast_accumulate")]
		private void AccumulateFastTickSpawnerRewards(EnemySpawner spawner, int fightsCount)
		{
			if (spawner == null || fightsCount <= 0) return;
			EnemySpawnerConfig def = spawner.Config;
			if (def.StatRewards != null)
				foreach (StatModifier reward in def.StatRewards)
				{
					double value = (double)reward.Value * def.Count * fightsCount;
					if (fastTickChunkStatRewards.TryGetValue(reward.StatType, out FastTickChunkStatReward entry))
					{
						if (entry.Name != reward.Name || entry.ModifierType != reward.ModifierType || entry.Source != reward.Source)
							RebuildCount("fast_stat_reward_mixed");
						entry.Value += value;
					}
					else
						entry = new FastTickChunkStatReward
						{
							Name = reward.Name, StatType = reward.StatType, ModifierType = reward.ModifierType, Source = reward.Source,
							Duration = reward.Duration, Value = value,
						};
					fastTickChunkStatRewards[reward.StatType] = entry;
				}
			if (def.ItemReward != null)
			{
				// Expected copies for the day's charges (design 4.5 Inventory, Chain): GetChainedItemBaseCount(n, K1) times
				// GetItemEmissionCoefficient; both are C6's (zero until filled).
				float timing = GetBalanceTimingMinutes();
				float chain = GetSimulationInterpolationCoefficient(timing, BalanceInterpolationType.ItemChainProbability);
				double copies = GetChainedItemBaseCount(fightsCount * def.Count, chain) * GetItemEmissionCoefficient(timing, def.ItemReward);
				if (copies != 0.0)
				{
					fastTickChunkItemRewards.TryGetValue(def.ItemReward, out double sum);
					fastTickChunkItemRewards[def.ItemReward] = sum + copies;
				}
				else RebuildCount("fast_item_rewards_zero");
			}
			if (!string.IsNullOrWhiteSpace(def.SoulReward))
			{
				fastTickChunkSoulRewards.TryGetValue(def.SoulReward, out double souls);
				fastTickChunkSoulRewards[def.SoulReward] = souls + (double)fightsCount * def.Count;
			}
			if (!string.IsNullOrWhiteSpace(def.CurrencyReward))
			{
				RebuildCurrencyRewards.TryGetValue(def.CurrencyReward, out double amount);
				RebuildCurrencyRewards[def.CurrencyReward] = amount + fightsCount * def.CurrencyRewardAmount;
			}
			if ((def.Gates != null && def.Gates.Count > 0) || !string.IsNullOrEmpty(def.SlotReward)) fastTickChunkGateRewardSpawners.Add(spawner);
			fastTickChunkKillCount += fightsCount * def.Count;
		}

		// Reads the five accumulated fields and empties them (design 4.5): one merged modifier per stat (StatType
		// order), then the items (rolled, ProcessInventory), souls, the gates the planning rounds left. P18's
		// ItemsThenStats is C6's.
		[SimulationRebuild(Provenance.Inferred, "pending:simulation.fast_flush")]
		private void FlushFastTickChunkRewards()
		{
			SimulationFastTickRecorder recorder = SimulationFastTickRecorder.For(SimulationHarness.CurrentJob);
#if UNITY_EDITOR
			if (recorder != null && recorder.ReferenceFlush)
			{
				RebuildReferenceFlush();
				return;
			}
#endif
			foreach (var pair in fastTickChunkStatRewards.OrderBy(p => (int)p.Key))
			{
				FastTickChunkStatReward reward = pair.Value;
				RebuildAddStatReward(new StatModifier { Name = reward.Name, StatType = reward.StatType, ModifierType = reward.ModifierType, Source = reward.Source },
					(float)reward.Value);
				if (recorder != null) recorder.FlushStatModifiers++;
			}
			foreach (var pair in fastTickChunkItemRewards.OrderBy(p => p.Key.Key, StringComparer.Ordinal))
			{
				int copies = RollFractionalCount(pair.Value);
				if (copies <= 0) continue;
				RebuildPendingItemCopies.TryGetValue(pair.Key, out int pending);
				RebuildPendingItemCopies[pair.Key] = pending + copies;
			}
			if (fastTickChunkItemRewards.Count > 0) ProcessInventory();
			foreach (var pair in fastTickChunkSoulRewards)
			{
				RebuildSoulRewards.TryGetValue(pair.Key, out double souls);
				RebuildSoulRewards[pair.Key] = souls + pair.Value;
			}
			int before = playerModel.LastUnlockedLocation.Number;
			RebuildFlushFastTickGates();
			if (recorder != null)
				for (int n = before + 1; n <= playerModel.LastUnlockedLocation.Number; n++) recorder.FlushUnlocks.Add(n);
			if (fastTickChunkKillCount != 0) RebuildCount("fast_kills_flushed", fastTickChunkKillCount);
			fastTickChunkStatRewards.Clear();
			fastTickChunkItemRewards.Clear();
			fastTickChunkSoulRewards.Clear();
			fastTickChunkKillCount = 0;
		}

#if UNITY_EDITOR
		// The native-replacement acceptance (design 4.5): FlushFastTickChunkRewards as an original body would be, with
		// only the accumulated fields and game APIs, no Rebuild* call. A job with fast_tick_probe.reference_flush must
		// give the same state digests as the default flush. Engine counters, logs and events are not part of it.
		private void RebuildReferenceFlush()
		{
			foreach (var pair in fastTickChunkStatRewards.OrderBy(p => (int)p.Key))
			{
				FastTickChunkStatReward reward = pair.Value;
				Stat stat = playerModel.Unit.Stats.FirstOrDefault(s => s.Type == reward.StatType);
				if (stat == null) continue;
				float value = (float)reward.Value;
				stat.AddModifier(new StatModifier { Name = reward.Name, StatType = reward.StatType, ModifierType = reward.ModifierType, Source = reward.Source, Value = value });
				bool percent = reward.ModifierType == ModifierType.PercentAdditive || reward.ModifierType == ModifierType.PercentMultiplicative
					|| reward.ModifierType == ModifierType.PreAdditivePostMultiplicative || reward.ModifierType == ModifierType.PostFormulaMultiplicative;
				if (!percent && reward.StatType == StatType.Health) playerModel.Unit.CurrentHealth += value;
			}
			foreach (var pair in fastTickChunkItemRewards.OrderBy(p => p.Key.Key, StringComparer.Ordinal))
			{
				int copies = RollFractionalCount(pair.Value);
				if (copies <= 0) continue;
				RebuildPendingItemCopies.TryGetValue(pair.Key, out int pending);
				RebuildPendingItemCopies[pair.Key] = pending + copies;
			}
			if (fastTickChunkItemRewards.Count > 0) ProcessInventory();
			foreach (var pair in fastTickChunkSoulRewards)
			{
				RebuildSoulRewards.TryGetValue(pair.Key, out double souls);
				RebuildSoulRewards[pair.Key] = souls + pair.Value;
			}
			List<EnemySpawner> gates = sortedSpawners.Where(fastTickChunkGateRewardSpawners.Contains).ToList();
			fastTickChunkGateRewardSpawners.Clear();
			foreach (EnemySpawner spawner in gates)
			{
				EnemySpawnerConfig def = spawner.Config;
				var reached = new List<int>();
				if (def.Gates != null)
					foreach (var gate in def.Gates)
					{
						if (gate == null || gatesModel.IsGateUnlocked(gate.Key)) continue;
						gatesModel.OpenGate(gate.Key);
						if (!reached.Contains(gate.NextLocationNumber)) reached.Add(gate.NextLocationNumber);
					}
				foreach (int number in reached)
				{
					if (number <= playerModel.LastUnlockedLocation.Number) continue;
					playerModel.UnlockLocation(number);
					playerModel.AdvanceBossProgressLocation(number);
					currentBoss = enemiesModel.Spawners.FirstOrDefault(s => s.Config.LocationNumber == number && s.Config.Gates != null
						&& s.Config.Gates.Any(g => g != null && g.NextLocationNumber == number + 1));
					UnlockSimulationWeaponSlot(number);
				}
				if (string.IsNullOrEmpty(def.SlotReward)) continue;
				ItemSlotModel slot = playerModel.ItemSlots.FirstOrDefault(s => s.Key == def.SlotReward);
				if (slot != null && !slot.IsUnlocked) slot.IsUnlocked = true;
			}
			fastTickChunkStatRewards.Clear();
			fastTickChunkItemRewards.Clear();
			fastTickChunkSoulRewards.Clear();
			fastTickChunkKillCount = 0;
		}
#endif

		// fights = (limit - used) / Count (design 4.5), one for an uncleared DAFC spawner (a cleared one is +inf and
		// never in the bot data).
		private int RebuildFastFights(EnemySpawner spawner)
		{
			if (spawner.Config.DisableAfterFirstClear) return 1;
			int perClear = GetSpawnerChargesPerClear(spawner);
			if (perClear <= 0)
			{
				RebuildCount("fast_no_units");
				return 0;
			}
			spawnerChargesUsage.TryGetValue(spawner, out int used);
			int left = GetSpawnerChargesLimit(spawner) - used;
			if (left % perClear != 0) RebuildCount("fast_charge_remainders");
			return left / perClear;
		}

		// L: KillLossMultiplier, or GetFastRPMConfig().Item2 when KillLossFromAb (policy extrapolation).
		private double RebuildFastKillLoss()
		{
			return RebuildParams.KillLossFromAb ? GetFastRPMConfig().Item2 : (double)RebuildParams.KillLossMultiplier;
		}

		// The kill gate, with the sensitivity probes' factors (fightTime / dps, loss / (dps * defence); 1 by default).
		private bool RebuildFastKillable(SpawnerData data)
		{
			if (RebuildParams.DiagnosticDisableKillPredicate) return true;
			double dps = RebuildParams.DiagnosticPlayerDpsScale;
			if (!(data.fightTime / dps < 300.0)) return false;
			double loss = -data.healthDelta / (dps * RebuildParams.DiagnosticDefenseScale);
			return !(RebuildFastKillLoss() * GetForecastStatValue(StatType.Health) < loss);
		}

		// KillState.AfterPass: the rewards the earlier rounds accumulated, on the player for one round only (a name of
		// their own, so they never merge into the rewards).
		private List<StatModifier> RebuildFastAfterPassAdd()
		{
			var added = new List<StatModifier>();
			foreach (var pair in fastTickChunkStatRewards.OrderBy(p => (int)p.Key))
			{
				Stat stat = playerModel.Unit.Stats.FirstOrDefault(s => s.Type == pair.Key);
				if (stat == null) continue;
				var modifier = new StatModifier { Name = "__rebuildFastAfterPass", StatType = pair.Key, ModifierType = pair.Value.ModifierType, Source = pair.Value.Source, Value = (float)pair.Value.Value };
				stat.AddModifier(modifier);
				added.Add(modifier);
			}
			return added;
		}

		private void RebuildFastAfterPassRemove(List<StatModifier> added)
		{
			foreach (StatModifier modifier in added)
				playerModel.Unit.Stats.First(s => s.Type == modifier.StatType).RemoveModifier(modifier);
		}

		// Design 4.5 P03/P04. FarmedTime: Kappa * sum(clears * farmedTime) / 60. NnTour: TourA * Tours * the nearest-neighbour
		// tour from the origin over the farmed non-DAFC spawners (x, z) / GetForecastStatSpecificValue(MovementSpeed) / 60
		// + TourB * sum(clears * fightTime) / 60.
		private float RebuildFastActiveMinutes(double farmedSeconds, double fightSeconds, List<EnemySpawner> toured, out double tour, out double speed)
		{
			speed = GetForecastStatSpecificValue(StatType.MovementSpeed);
			tour = 0.0;
			if (RebuildParams.ActiveMinutesModel == ActiveMinutesModel.FarmedTime) return (float)(RebuildParams.Kappa * farmedSeconds / 60.0);
			tour = RebuildFastNearestNeighbourTour(toured);
			return (float)(RebuildParams.TourA * RebuildParams.Tours * tour / speed / 60.0 + RebuildParams.TourB * fightSeconds / 60.0);
		}

		private static double RebuildFastNearestNeighbourTour(List<EnemySpawner> spawners)
		{
			var points = spawners.Distinct().Select(s => s.transform.position).ToList();
			var left = new List<int>(Enumerable.Range(0, points.Count));
			double x = 0.0, z = 0.0, length = 0.0;
			while (left.Count > 0)
			{
				int best = -1;
				double bestDistance = double.MaxValue;
				foreach (int i in left)
				{
					double dx = points[i].x - x, dz = points[i].z - z, d = dx * dx + dz * dz;
					if (d < bestDistance)
					{
						bestDistance = d;
						best = i;
					}
				}
				length += Math.Sqrt(bestDistance);
				x = points[best].x;
				z = points[best].z;
				left.Remove(best);
			}
			return length;
		}

		// manifest engine.fast_tick (C5): the kill gate's provenance and L, the policy-extrapolation mark (L from the
		// AB table, or an AB group other than A, which also changes RPM), diagnostics, files, counters.
		private JObject RebuildFastTickManifest()
		{
			SimulationFastTickRecorder recorder = SimulationFastTickRecorder.For(SimulationHarness.CurrentJob);
			MethodInfo gate = typeof(SimulationSystem).GetMethod(nameof(SimulateFightWithSpawnerFast), BindingFlags.NonPublic | BindingFlags.Instance);
			var attribute = (SimulationRebuildAttribute)Attribute.GetCustomAttribute(gate, typeof(SimulationRebuildAttribute));
			string variant = recorder?.AbVariant ?? "A";
			var reasons = new JArray();
			if (RebuildParams.KillLossFromAb) reasons.Add("kill_loss_from_ab");
			if (variant != "A") reasons.Add("ab_variant_" + variant);
			return new JObject
			{
				["schema"] = SimulationFastTickRecorder.Schema,
				["kill_gate"] = new JObject
				{
					["method"] = gate.Name,
					["provenance"] = attribute?.Provenance.ToString(),
					["formula_id"] = attribute?.FormulaId,
					["kill_loss"] = RebuildFastKillLoss(),
					["kill_loss_from_ab"] = RebuildParams.KillLossFromAb,
				},
				["ab_variant"] = variant,
				["policy_extrapolation"] = reasons.Count > 0,
				["policy_extrapolation_reasons"] = reasons,
				["diagnostics_on"] = RebuildParams.FastTickDiagnosticsOn,
				["plan_new_islands_same_day"] = RebuildParams.PlanNewIslandsSameDay,
				["reference_flush"] = recorder != null && recorder.ReferenceFlush,
				["files"] = recorder?.Files(),
				["counters"] = recorder?.CountersJson(),
				["error"] = recorder?.Error,
			};
		}
	}
}
