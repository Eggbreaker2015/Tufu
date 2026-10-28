using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Codebase.Enemy;
using Codebase.Player.Inventory;
using Codebase.Simulation.Rebuild;
using Codebase.Unit;

namespace Codebase.Simulation.Systems
{
	public partial class SimulationSystem
	{
		// ---- trace (design 1.4.3, 2.6; unit C3c): writes into SimulationTrace.Current, never changes the run ----
		// Engine call sites: day start (PrepareSimulationRun and UpdateSpawnerChargesChunk, after the forced
		// snapshot and the override), day end (UpdateSpawnerChargesChunk, before the day index changes), the
		// eligible set (Tick, after CalculateSpawnerData(false)), first kills (RebuildApplyClear), unlocks and slot
		// unlocks (RebuildUnlockLocation, RebuildApplyGateReward). For the module units: RebuildTraceFight (C5 per
		// fast fight batch, C7 per slow fight), RebuildTraceEligible (C5 after the first planning round's
		// CalculateSpawnerData(false)), RebuildTraceSlotUnlock (C6 UnlockSimulationWeaponSlot), RebuildTraceEvolve
		// (C6), RebuildTraceDeath (C7 ProcessDeath).

		private void RebuildTraceDayStart()
		{
			SimulationTrace t = SimulationTrace.Current;
			if (t == null) return;
			t.DayIndex = timeChunkIndex;
			t.DaySnapshotIndex = config.TimeChunkSnapshots.Count - 1;
			t.DayStartPlaytime = config.CurrentPlaytime;
			RebuildCounters.TryGetValue("kills", out t.DayStartKills);
			RebuildCounters.TryGetValue("clears", out t.DayStartClears);
			t.FightTimeSum = t.FarmedTimeSum = 0.0;
			t.EligibleCount = -1;
			t.EligibleSignature = "";
			t.AddDigest(timeChunkIndex, t.DaySnapshotIndex, config.CurrentPlaytime, config.CurrentTotalPlaytime, RebuildDigestText());
		}

		private void RebuildTraceDayEnd()
		{
			SimulationTrace t = SimulationTrace.Current;
			if (t == null || t.DayIndex < 0) return;
			t.AddDay(RebuildTraceRow(t, true));
		}

		// The harness, when the job ends: the open day as an incomplete row, unless the run stopped right at its
		// start (StopAtDay stops after the target day's forced snapshot).
		internal void RebuildTraceFlush()
		{
			SimulationTrace t = SimulationTrace.Current;
			if (t == null || t.DayIndex < 0) return;
			if (RebuildDayTicks > 0 || RebuildDayAfkSteps > 0) t.AddDay(RebuildTraceRow(t, false));
			t.DayIndex = -1;
		}

		private SimulationTrace.Day RebuildTraceRow(SimulationTrace t, bool complete)
		{
			RebuildCounters.TryGetValue("kills", out long kills);
			RebuildCounters.TryGetValue("clears", out long clears);
			return new SimulationTrace.Day
			{
				DayIndex = t.DayIndex,
				SnapshotIndex = t.DaySnapshotIndex,
				Playtime = config.CurrentPlaytime,
				TotalPlaytime = config.CurrentTotalPlaytime,
				ActiveMinutes = (double)config.CurrentPlaytime - t.DayStartPlaytime,
				OfflineMinutes = RebuildDayOfflineMinutes,
				Kills = kills - t.DayStartKills,
				Clears = clears - t.DayStartClears,
				FightTimeSum = t.FightTimeSum,
				FarmedTimeSum = t.FarmedTimeSum,
				EligibleCount = t.EligibleCount,
				EligibleSignature = t.EligibleSignature,
				Location = currentSpawner != null ? currentSpawner.Config.LocationNumber : currentLocationNumber,
				UnlockedLocation = playerModel.LastUnlockedLocation.Number,
				Ticks = RebuildDayTicks,
				OfflineSteps = RebuildDayAfkSteps,
				Complete = complete,
			};
		}

		// The bot-filter set (spawnerData right after CalculateSpawnerData(false)) of the day's first planning.
		private void RebuildTraceEligible()
		{
			SimulationTrace t = SimulationTrace.Current;
			if (t == null || t.EligibleCount >= 0) return;
			t.EligibleCount = spawnerData.Count;
			t.EligibleSignature = SimulationTrace.SetSignature(spawnerData.Keys.Select(RebuildTraceId));
		}

		private void RebuildTraceFight(double fightSeconds, double farmedSeconds)
		{
			SimulationTrace t = SimulationTrace.Current;
			if (t == null) return;
			t.FightTimeSum += fightSeconds;
			t.FarmedTimeSum += farmedSeconds;
		}

		private void RebuildTraceClear(EnemySpawner spawner)
		{
			SimulationTrace t = SimulationTrace.Current;
			if (t == null) return;
			string id = RebuildTraceId(spawner);
			if (t.Killed.Add(id)) RebuildTraceEvent("first_kill", ("spawner", id), ("location", spawner.Config.LocationNumber));
		}

		private void RebuildTraceUnlock(int location)
		{
			RebuildTraceEvent("unlock", ("location", location));
		}

		// count: unlocked slots of the slot's type after the unlock.
		private void RebuildTraceSlotUnlock(ItemSlotModel slot)
		{
			int count = playerModel.ItemSlots.Count(s => s.SlotType == slot.SlotType && s.IsUnlocked);
			RebuildTraceEvent("slot_unlock", ("slot", slot.Key), ("count", count));
		}

		private void RebuildTraceEvolve(ItemModel item)
		{
			RebuildTraceEvent("evolve", ("item", item.Key), ("level", item.Level), ("copies", item.Copies));
		}

		private void RebuildTraceDeath(EnemySpawner spawner)
		{
			RebuildTraceEvent("death", ("spawner", RebuildTraceId(spawner)), ("location", spawner != null ? spawner.Config.LocationNumber : 0));
		}

		private void RebuildTraceEvent(string type, params (string, object)[] fields)
		{
			SimulationTrace.Current?.AddEvent(type, timeChunkIndex, config.CurrentPlaytime, config.CurrentTotalPlaytime, fields);
		}

		private string RebuildTraceId(EnemySpawner spawner)
		{
			SimulationTrace t = SimulationTrace.Current;
			if (spawner == null) return "";
			if (t == null) return RebuildSpawnerId(spawner);
			if (!t.Ids.TryGetValue(spawner, out string id)) t.Ids[spawner] = id = RebuildSpawnerId(spawner);
			return id;
		}

		// The day's state digest text (design 2.6), "key=value" lines sorted by ordinal: every stat's Value and
		// TotalValue as the snapshot composes them plus the live value, health, items (Key, Level, Copies,
		// Evolved), slots, locations and position, gates, P, T and the session clock, the K modifiers, the RPM
		// and soul RPM dictionaries, charges used, non-zero cooldowns, pending and fast-accumulated rewards, souls
		// and currencies, the policy state, and the random streams. Floats and doubles as IEEE bits.
		private string RebuildDigestText()
		{
			var lines = new List<string>();
			void Add(string key, string value) => lines.Add(key + "=" + value);
			Add("clock.day", RebuildTraceText(timeChunkIndex));
			Add("clock.playtime", RebuildTraceBits(config.CurrentPlaytime));
			Add("clock.total_playtime", RebuildTraceBits(config.CurrentTotalPlaytime));
			Add("clock.life_cycle", RebuildTraceText(lifeCycleIndex) + "," + RebuildTraceBits(lifeCyclePeriodEnd));
			Add("clock.last_ads", RebuildTraceBits(lastAdsTime));
			Add("clock.last_snapshot_playtime", RebuildTraceBits(RebuildLastSnapshotPlaytime));
			Add("clock.fast_tick_active", wasFastTickActive ? "1" : "0");
			Add("modifiers", RebuildTraceText(RebuildModifierDay) + "," + RebuildTraceBits(RebuildK2) + "," + RebuildTraceBits(RebuildK3));
			foreach (SimulationStatData row in RebuildSnapshotStats())
			{
				Stat stat = playerModel.Unit.Stats.First(s => s.Type == row.Key);
				Add("stat." + RebuildTraceText((int)row.Key), RebuildTraceBits(row.Value) + "," + RebuildTraceBits(row.TotalFlatValue) + "," + RebuildTraceBits(row.TotalValue) + "," + RebuildTraceBits(stat.Value));
			}
			Add("health", RebuildTraceBits(playerModel.Unit.CurrentHealth));
			int index = 0;
			foreach (ItemModel item in playerModel.Items.OrderBy(i => i.Key, StringComparer.Ordinal).ThenBy(i => i.Level).ThenBy(i => i.Copies))
				Add("item." + item.Key + "#" + RebuildTraceText(index++), RebuildTraceText(item.Level) + "," + RebuildTraceText(item.Copies) + "," + (item.Evolved ? "1" : "0"));
			foreach (ItemSlotModel slot in playerModel.ItemSlots)
				if (!string.IsNullOrEmpty(slot.Key)) Add("slot." + slot.Key, slot.IsUnlocked ? "1" : "0");
			Add("location.unlocked", RebuildTraceText(playerModel.LastUnlockedLocation.Number));
			Add("location.current", RebuildTraceText(currentLocationNumber));
			Add("location.position", RebuildTraceBits(currentPosition.x) + "," + RebuildTraceBits(currentPosition.y) + "," + RebuildTraceBits(currentPosition.z));
			Add("location.current_spawner", RebuildTraceId(currentSpawner));
			Add("location.current_boss", RebuildTraceId(currentBoss));
			var gates = new SortedSet<string>(StringComparer.Ordinal);
			foreach (EnemySpawner spawner in enemiesModel.Spawners)
				if (spawner.Config.Gates != null)
					foreach (var gate in spawner.Config.Gates)
						if (gate != null && gatesModel.IsGateUnlocked(gate.Key)) gates.Add(gate.Key);
			foreach (string gate in gates) Add("gate." + gate, "1");
			foreach (var pair in RPMCalculatedValue) Add("rpm." + RebuildTraceText((int)pair.Key), RebuildTraceBits(pair.Value));
			foreach (var pair in SoulRPMCalculatedValue) Add("soul_rpm." + pair.Key, RebuildTraceBits(pair.Value));
			foreach (var pair in spawnerChargesUsage)
				if (pair.Value != 0) Add("charges." + RebuildTraceId(pair.Key), RebuildTraceText(pair.Value));
			foreach (EnemySpawner spawner in enemiesModel.Spawners)
				if (spawner.Cooldown != 0f) Add("cooldown." + RebuildTraceId(spawner), RebuildTraceBits(spawner.Cooldown));
			foreach (var pair in RebuildPendingItemCopies) Add("pending_item." + pair.Key.Key, RebuildTraceText(pair.Value));
			foreach (var pair in fastTickChunkStatRewards)
				Add("fast.stat." + RebuildTraceText((int)pair.Key), pair.Value.Name + "," + RebuildTraceText((int)pair.Value.ModifierType) + "," + RebuildTraceText((int)pair.Value.Source) + "," + RebuildTraceBits(pair.Value.Value));
			foreach (var pair in fastTickChunkItemRewards) Add("fast.item." + pair.Key.Key, RebuildTraceBits(pair.Value));
			foreach (var pair in fastTickChunkSoulRewards) Add("fast.soul." + pair.Key, RebuildTraceBits(pair.Value));
			foreach (EnemySpawner spawner in fastTickChunkGateRewardSpawners) Add("fast.gate." + RebuildTraceId(spawner), "1");
			Add("fast.kills", RebuildTraceText(fastTickChunkKillCount));
			foreach (var pair in RebuildSoulRewards) Add("souls." + pair.Key, RebuildTraceBits(pair.Value));
			foreach (var pair in RebuildCurrencyRewards) Add("currency." + pair.Key, RebuildTraceBits(pair.Value));
			foreach (var pair in statsBenefit) Add("policy.stat_benefit." + RebuildTraceText((int)pair.Key), RebuildTraceBits(pair.Value));
			Add("policy.last_reward_stat", lastRewardStat.HasValue ? RebuildTraceText((int)lastRewardStat.Value) : "");
			int stream = 0;
			foreach (string line in RebuildRandom.StateText().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
				Add("random." + RebuildTraceText(stream++), line);
			lines.Sort(StringComparer.Ordinal);
			return string.Join("\n", lines) + "\n";
		}

		private static string RebuildTraceText(int value) => value.ToString(CultureInfo.InvariantCulture);

		private static string RebuildTraceBits(float value) => BitConverter.SingleToInt32Bits(value).ToString("x8", CultureInfo.InvariantCulture);

		private static string RebuildTraceBits(double value) => BitConverter.DoubleToInt64Bits(value).ToString("x16", CultureInfo.InvariantCulture);
	}
}
