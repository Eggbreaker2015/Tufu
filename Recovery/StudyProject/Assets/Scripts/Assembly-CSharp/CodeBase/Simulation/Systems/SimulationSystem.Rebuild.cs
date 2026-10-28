using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
		// ---- cross-module partial hooks (design 4.4, 4.6, 7.4) ----
		// Declared here, implemented by the owning unit's file; a hook without an implementation compiles away.
		// Not to be confused with SimulationHarness's RebuildJob* hooks.

		// Replay/checkpoint state for day `day`, right after its forced snapshot (C4, .Replay.cs).
		partial void RebuildApplyDayOverride(int day);

		// Stored DpsData/DefenseData caches after CalculateSimulationData (C4, .Replay.cs).
		partial void RebuildApplyReplayCaches();

		// Tick progress and heartbeat after every ExecuteTick; forwards to SimulationHarness.OnTickCompleted (C3b).
		partial void RebuildOnTickCompleted();

		// In-session reset between replay/checkpoint jobs (S1, .Reset.cs); sets `reset` when implemented.
		partial void RebuildResetForJob(ref bool reset);

		// Entry point for SimulationHarness, which cannot call a private partial method. False while S1 is absent.
		internal bool RebuildTryResetForJob()
		{
			bool reset = false;
			RebuildResetForJob(ref reset);
			return reset;
		}

		// ---- engine state (design 4.2, the rows marked new) ----

		private SimulationRebuildParams RebuildParams = new SimulationRebuildParams();

		private SimulationRunRequest.EngineProbe RebuildProbe;

		// Set by the stop conditions, max_ticks and failures; ExecuteTick then returns at once (design 2.4).
		private bool RebuildStopped;

		private string RebuildStopReason;

		private string RebuildFailureText;

		private int RebuildFailureCode;

		// Early-list cadence (design 4.5): P of the last snapshot written to config.Snapshots.
		private float RebuildLastSnapshotPlaytime;

		// K2/K3 carried by the two __simulation* modifiers, and the day they were evaluated at.
		private float RebuildK2;

		private float RebuildK3;

		private int RebuildModifierDay = -1;

		private bool RebuildLastTickFast;

		// Item copies granted by the write points and the fast flush; ProcessInventory (C6) merges and empties it.
		private readonly Dictionary<ItemConfig, int> RebuildPendingItemCopies = new Dictionary<ItemConfig, int>();

		// Souls and currencies are recorded only (design 4.5).
		private readonly SortedDictionary<string, double> RebuildSoulRewards = new SortedDictionary<string, double>(StringComparer.Ordinal);

		private readonly SortedDictionary<string, double> RebuildCurrencyRewards = new SortedDictionary<string, double>(StringComparer.Ordinal);

		private readonly HashSet<int> RebuildUnlockedLocations = new HashSet<int>();

		private readonly SortedDictionary<string, long> RebuildCounters = new SortedDictionary<string, long>(StringComparer.Ordinal);

		// Per day: [day, fast (0/1), ticks, offline steps, offline minutes, P and T at the day change].
		private readonly List<JArray> RebuildDayLog = new List<JArray>();

		// Slow-phase offlines: [k, P at the offline, T after it, lifeCyclePeriodEnd after it].
		private readonly List<JArray> RebuildOfflineLog = new List<JArray>();

		// [island, day, P, T] per unlock.
		private readonly List<JArray> RebuildUnlockLog = new List<JArray>();

		private int RebuildDayTicks, RebuildDayAfkSteps;

		private double RebuildDayOfflineMinutes;

		private JObject RebuildProbeResult;

		// ---- harness interface ----

		internal string RebuildFailure => RebuildFailureText;

		internal int RebuildFailureExitCode => RebuildFailureCode;

		internal bool RebuildLastTickWasFast => RebuildLastTickFast;

		[SimulationRebuild(Provenance.Inferred, "pending:simulation.tick_heartbeat")]
		partial void RebuildOnTickCompleted()
		{
			if (SimulationHarness.OnTickCompleted() && !RebuildStopped)
			{
				RebuildStopped = true;
				RebuildStopReason = "max_ticks";
			}
		}

		private void RebuildFail(int exitCode, string message)
		{
			if (RebuildFailureText == null)
			{
				RebuildFailureText = message;
				RebuildFailureCode = exitCode;
				Debug.LogError("[TufuSim] engine: " + message);
			}
			RebuildStopped = true;
			RebuildStopReason = "failed";
			config.IsRunning = false;
		}

		private void RebuildCount(string key, long n = 1)
		{
			RebuildCounters.TryGetValue(key, out long value);
			RebuildCounters[key] = value + n;
		}

		// Batch runs: params and engine switches of the job; interactive runs: code defaults (C3c adds the asset).
		private void RebuildLoadRunOptions()
		{
			SimulationRunRequest.Job job = SimulationHarness.CurrentJob;
			RebuildProbe = job?.Engine;
			try
			{
				RebuildParams = SimulationRebuildParams.FromJson(job?.Params);
			}
			catch (Exception e)
			{
				RebuildParams = new SimulationRebuildParams();
				RebuildFail(SimulationHarness.ExitBadRequest, "params: " + e.Message);
				return;
			}
			string error = RebuildParams.Validate();
			if (error != null) RebuildFail(SimulationHarness.ExitBadRequest, error);
		}

		private bool RebuildNullPolicy => RebuildProbe != null && RebuildProbe.NullPolicy;

		// ---- write points (design 4.5) ----

		// The one place a clear changes the world. Called only from the bodies of SimulateFightWithSpawner (slow,
		// immediate) and SimulateFightWithSpawnerFast (fast, accumulate); charges and cooldown change at once in both.
		[SimulationRebuild(Provenance.Inferred, "pending:simulation.apply_clear")]
		private void RebuildApplyClear(EnemySpawner spawner, int clears, bool accumulate)
		{
			if (spawner == null || clears <= 0) return;
			EnemySpawnerConfig def = spawner.Config;
			if (def.DisableAfterFirstClear && clears > 1)
			{
				RebuildCount("dafc_clamped");
				clears = 1;
			}
			spawnerChargesUsage.TryGetValue(spawner, out int used);
			spawnerChargesUsage[spawner] = Math.Min(GetSpawnerChargesLimit(spawner), used + GetSpawnerChargesPerClear(spawner) * clears);
			spawner.Cooldown = def.DisableAfterFirstClear ? float.PositiveInfinity : spawner.BaseCooldown;
			RebuildCount("clears", clears);
			RebuildCount("kills", (long)clears * def.Count);
			if (accumulate)
			{
				RebuildCount("accumulate_calls");
				AccumulateFastTickSpawnerRewards(spawner, clears);
				return;
			}
			if (def.StatRewards != null)
				foreach (StatModifier reward in def.StatRewards)
					RebuildAddStatReward(reward, (float)((double)reward.Value * def.Count * clears));
			if (def.ItemReward != null)
			{
				int copies = 0;
				for (int i = 0; i < clears; i++) copies += GetSpawnerItemRewardCopies(spawner);
				if (copies > 0)
				{
					RebuildPendingItemCopies.TryGetValue(def.ItemReward, out int pending);
					RebuildPendingItemCopies[def.ItemReward] = pending + copies;
				}
				ProcessInventory();
			}
			if (!string.IsNullOrWhiteSpace(def.SoulReward))
			{
				RebuildSoulRewards.TryGetValue(def.SoulReward, out double souls);
				RebuildSoulRewards[def.SoulReward] = souls + (double)clears * def.Count;
			}
			if (!string.IsNullOrWhiteSpace(def.CurrencyReward))
			{
				RebuildCurrencyRewards.TryGetValue(def.CurrencyReward, out double amount);
				RebuildCurrencyRewards[def.CurrencyReward] = amount + clears * def.CurrencyRewardAmount;
			}
			RebuildApplyGateReward(spawner);
		}

		// Stat reward without GrantStatReward (design 1.5 rule 9, Q06): the reward's name, source and type, so
		// StatBase.AddModifier merges as in the game; a flat Health reward also heals.
		private void RebuildAddStatReward(StatModifier reward, float value)
		{
			Stat stat = playerModel.Unit.Stats.FirstOrDefault(s => s.Type == reward.StatType);
			if (stat == null)
			{
				RebuildCount("stat_reward_missing_stat");
				return;
			}
			stat.AddModifier(new StatModifier { Name = reward.Name, StatType = reward.StatType, ModifierType = reward.ModifierType, Source = reward.Source, Value = value });
			bool percent = reward.ModifierType == ModifierType.PercentAdditive || reward.ModifierType == ModifierType.PercentMultiplicative
				|| reward.ModifierType == ModifierType.PreAdditivePostMultiplicative || reward.ModifierType == ModifierType.PostFormulaMultiplicative;
			if (!percent && reward.StatType == StatType.Health) playerModel.Unit.CurrentHealth += value;
		}

		// A cleared spawner's gates open once; each island they reach is unlocked once; its slot reward opens.
		private void RebuildApplyGateReward(EnemySpawner spawner)
		{
			EnemySpawnerConfig def = spawner.Config;
			if (def.Gates != null)
			{
				var reached = new List<int>();
				foreach (var gate in def.Gates)
				{
					if (gate == null || gatesModel.IsGateUnlocked(gate.Key)) continue;
					gatesModel.OpenGate(gate.Key);
					if (!reached.Contains(gate.NextLocationNumber)) reached.Add(gate.NextLocationNumber);
				}
				foreach (int number in reached) RebuildUnlockLocation(number);
			}
			if (!string.IsNullOrEmpty(def.SlotReward))
			{
				ItemSlotModel slot = playerModel.ItemSlots.FirstOrDefault(s => s.Key == def.SlotReward);
				if (slot == null) RebuildCount("slot_reward_missing");
				else if (!slot.IsUnlocked)
				{
					slot.IsUnlocked = true;
					RebuildCount("slot_unlocks");
				}
			}
		}

		[SimulationRebuild(Provenance.Inferred, "pending:simulation.unlock_location")]
		private void RebuildUnlockLocation(int number)
		{
			if (RebuildUnlockedLocations.Contains(number))
			{
				RebuildCount("repeat_unlocks");
				return;
			}
			if (number <= playerModel.LastUnlockedLocation.Number)
			{
				// A back-gate to an island that is already open (islands 4, 11, 12, 13).
				RebuildCount("back_gate_noops");
				return;
			}
			RebuildUnlockedLocations.Add(number);
			playerModel.UnlockLocation(number);
			playerModel.AdvanceBossProgressLocation(number);
			currentBoss = RebuildFindBoss(number);
			RebuildCount("unlocks");
			RebuildUnlockLog.Add(new JArray(number, timeChunkIndex, (double)config.CurrentPlaytime, (double)config.CurrentTotalPlaytime));
			UnlockSimulationWeaponSlot(number);
		}

		// Consumes only the gate set of the fast accumulation, between two planning rounds (design 4.5).
		[SimulationRebuild(Provenance.Inferred, "pending:simulation.same_day_gates")]
		private void RebuildFlushFastTickGates()
		{
			if (fastTickChunkGateRewardSpawners.Count == 0) return;
			var gates = sortedSpawners.Where(fastTickChunkGateRewardSpawners.Contains).ToList();
			fastTickChunkGateRewardSpawners.Clear();
			foreach (EnemySpawner spawner in gates) RebuildApplyGateReward(spawner);
		}

		// currentBoss: the registered spawner on island `location` with a gate to location + 1, null if none
		// (island 17); never chosen by largest OT (design 4.5). Several candidates: the first registered, counted.
		[SimulationRebuild(Provenance.Inferred, "pending:simulation.current_boss")]
		private EnemySpawner RebuildFindBoss(int location)
		{
			EnemySpawner found = null;
			foreach (EnemySpawner spawner in enemiesModel.Spawners)
			{
				EnemySpawnerConfig def = spawner.Config;
				if (def.LocationNumber != location || def.Gates == null) continue;
				if (!def.Gates.Any(gate => gate != null && gate.NextLocationNumber == location + 1)) continue;
				if (found != null)
				{
					RebuildCount("boss_ambiguous");
					continue;
				}
				found = spawner;
			}
			return found;
		}

		// ---- manifest "engine" ----

		private static string RebuildSpawnerId(EnemySpawner spawner) =>
			spawner == null ? null : SimulationHarness.HierarchyPath(spawner.transform) + "|" + spawner.Config.Key;

		internal JObject RebuildManifest()
		{
			var bosses = new JObject();
			int maxLocation = locationsConfig.Locations.Max(location => location.Number);
			for (int n = 1; n <= maxLocation; n++) bosses[n.ToString(CultureInfo.InvariantCulture)] = RebuildSpawnerId(RebuildFindBoss(n));
			var provenance = new JObject();
			foreach (Provenance p in Enum.GetValues(typeof(Provenance))) provenance[p.ToString()] = (int)p;
			return new JObject
			{
				["params"] = SimulationJson.ToJObject(RebuildParams),
				["null_policy"] = RebuildNullPolicy,
				["provenance_enum"] = provenance,
				["stopped"] = RebuildStopped,
				["stop_reason"] = RebuildStopReason,
				["failure"] = RebuildFailureText,
				["time_chunk_index"] = timeChunkIndex,
				["modifier_day"] = RebuildModifierDay,
				["playtime_bits"] = BitConverter.SingleToInt32Bits(config.CurrentPlaytime),
				["total_playtime_bits"] = BitConverter.SingleToInt32Bits(config.CurrentTotalPlaytime),
				["life_cycle_index"] = lifeCycleIndex,
				["unlocked_location"] = playerModel.LastUnlockedLocation.Number,
				["current_boss"] = RebuildSpawnerId(currentBoss),
				["bosses"] = bosses,
				["counters"] = new JObject(RebuildCounters.Select(kv => new JProperty(kv.Key, kv.Value))),
				["souls"] = new JObject(RebuildSoulRewards.Select(kv => new JProperty(kv.Key, kv.Value))),
				["currencies"] = new JObject(RebuildCurrencyRewards.Select(kv => new JProperty(kv.Key, kv.Value))),
				["days"] = new JArray(RebuildDayLog.Cast<object>().ToArray()),
				["offline"] = new JArray(RebuildOfflineLog.Cast<object>().ToArray()),
				["unlocks"] = new JArray(RebuildUnlockLog.Cast<object>().ToArray()),
				["probe"] = RebuildProbeResult,
			};
		}

		// ---- engine probes (request job.engine_probe; tests only) ----

		private void RebuildApplyProbeSetup()
		{
			if (RebuildProbe == null) return;
			for (int n = 2; n <= RebuildProbe.UnlockThrough; n++) RebuildUnlockLocation(n);
			if (RebuildProbe.WritePoints)
			{
				RebuildProbeResult = RebuildRunWritePointProbe();
				RebuildStopped = true;
				RebuildStopReason = "write_point_probe";
				config.IsRunning = false;
			}
		}

		private JObject RebuildProbeState(EnemySpawner spawner)
		{
			spawnerChargesUsage.TryGetValue(spawner, out int used);
			var stats = new JObject();
			foreach (Stat stat in playerModel.Unit.Stats)
				stats[((int)stat.Type).ToString(CultureInfo.InvariantCulture)] = GetSimulationSnapshotStatValue(stat);
			return new JObject
			{
				["usage"] = used,
				["limit"] = GetSpawnerChargesLimit(spawner),
				["cooldown"] = SimulationJson.FormatFloat(spawner.Cooldown),
				["base_cooldown"] = SimulationJson.FormatFloat(spawner.BaseCooldown),
				["stats"] = stats,
				["current_health"] = playerModel.Unit.CurrentHealth,
				["unlocked_location"] = playerModel.LastUnlockedLocation.Number,
				["current_boss"] = RebuildSpawnerId(currentBoss),
				["total_playtime"] = (double)config.CurrentTotalPlaytime,
			};
		}

		// The write-point scenario of design 7.3 C3b on the fresh run state: immediate and accumulate clears, the
		// charge cap, a DAFC clear and its +inf cooldown through ResetCooldowns and offline time, a gate cleared
		// twice, and a direct repeat unlock.
		private JObject RebuildRunWritePointProbe()
		{
			EnemySpawner Pick(Func<EnemySpawnerConfig, bool> predicate) =>
				sortedSpawners.FirstOrDefault(s => !s.Config.IsEvent && !s.Config.IsVip && predicate(s.Config));
			bool Plain(EnemySpawnerConfig c) => !c.DisableAfterFirstClear && c.ItemReward == null && (c.Gates == null || c.Gates.Count == 0)
				&& c.StatRewards != null && c.StatRewards.Count > 0;

			EnemySpawner health = Pick(c => c.LocationNumber == 1 && Plain(c) && c.StatRewards.Any(r => r.StatType == StatType.Health && r.ModifierType == ModifierType.Flat));
			EnemySpawner other = Pick(c => c.LocationNumber == 1 && Plain(c) && !c.StatRewards.Any(r => r.StatType == StatType.Health));
			EnemySpawner dafc = Pick(c => c.DisableAfterFirstClear && (c.Gates == null || c.Gates.Count == 0));
			EnemySpawner gate = RebuildFindBoss(1);
			var steps = new JArray();

			void Step(string name, EnemySpawner spawner, Action action)
			{
				JObject before = RebuildProbeState(spawner);
				action();
				steps.Add(new JObject
				{
					["name"] = name,
					["spawner"] = RebuildSpawnerId(spawner),
					["count"] = spawner.Config.Count,
					["rewards"] = new JArray(spawner.Config.StatRewards == null ? new object[0] : spawner.Config.StatRewards
						.Select(r => (object)new JObject { ["stat"] = (int)r.StatType, ["type"] = (int)r.ModifierType, ["value"] = (double)r.Value }).ToArray()),
					["before"] = before,
					["after"] = RebuildProbeState(spawner),
					["counters"] = new JObject(RebuildCounters.Select(kv => new JProperty(kv.Key, kv.Value))),
					["fast_tick_chunk_stat_rewards"] = fastTickChunkStatRewards.Count,
				});
			}

			Step("immediate", health, () => RebuildApplyClear(health, 1, false));
			Step("immediate_again", health, () => RebuildApplyClear(health, 1, false));
			Step("accumulate", other, () => RebuildApplyClear(other, 2, true));
			Step("charge_cap", other, () => RebuildApplyClear(other, 1000, true));
			Step("dafc_clear", dafc, () => RebuildApplyClear(dafc, 3, false));
			Step("reset_cooldowns", dafc, ResetCooldowns);
			Step("offline", dafc, () => ProcessAFK(config.LifeCycleInMinutes[1]));
			Step("reset_cooldowns_plain", health, ResetCooldowns);
			Step("gate_clear", gate, () => RebuildApplyClear(gate, 1, false));
			Step("gate_clear_again", gate, () => RebuildApplyClear(gate, 1, false));
			Step("unlock_again", gate, () => RebuildUnlockLocation(2));
			var slots = new JObject();
			foreach (ItemSlotModel slot in playerModel.ItemSlots)
				if (!string.IsNullOrEmpty(slot.Key)) slots[slot.Key] = slot.IsUnlocked;
			return new JObject { ["steps"] = steps, ["slots"] = slots };
		}
	}
}
