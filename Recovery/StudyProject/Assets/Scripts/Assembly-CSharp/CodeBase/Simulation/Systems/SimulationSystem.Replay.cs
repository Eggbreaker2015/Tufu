using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Simulation.Harness;
using Codebase.Simulation.Rebuild;
using Codebase.Unit;
using Newtonsoft.Json.Linq;

namespace Codebase.Simulation.Systems
{
	public partial class SimulationSystem
	{
		private SimulationStateOverride.DayState RebuildReplayRpmDay;
		private int RebuildReplayLocationBefore, RebuildReplayRemoved;

		// Stored replay loads day d before RPM, with K(d-1) still live. The post-snapshot hook
		// restores the requested planning TotalValue at K(d), without feeding it into the RPM maximum.
		[SimulationRebuild(Provenance.DebugOnly, "simulation.state_override")]
		partial void RebuildApplyDayOverrideBeforeRpm(int day)
		{
			SimulationStateOverride state = SimulationStateOverride.For(SimulationHarness.CurrentJob);
			if (state == null || state.Mode != "replay" || !state.StoredCaches || RebuildStopped) return;
			RebuildReplayApplyState(state, day, true);
		}

		[SimulationRebuild(Provenance.DebugOnly, "simulation.state_override")]
		partial void RebuildApplyDayOverride(int day)
		{
			SimulationStateOverride state = SimulationStateOverride.For(SimulationHarness.CurrentJob);
			if (state == null || RebuildStopped) return;
			if (state.Mode == "replay" && state.StoredCaches)
			{
				if (RebuildReplayRpmDay == null) return;
				RebuildReplayFinish(state, RebuildReplayRpmDay, RebuildReplayLocationBefore, RebuildReplayRemoved, false);
				RebuildReplayRpmDay = null;
				return;
			}
			RebuildReplayApplyState(state, day, false);
		}

		private void RebuildReplayApplyState(SimulationStateOverride state, int day, bool beforeRpm)
		{
			try
			{
				if (state.Error != null)
				{
					RebuildFail(SimulationHarness.ExitBadRequest, state.Error);
					return;
				}
				SimulationStateOverride.DayState s = state.Resolve(day);
				if (s == null) return;
				int locationBefore = playerModel.LastUnlockedLocation.Number;
				if (s.UnlockTo.HasValue) RebuildReplayUnlock(state, s.UnlockTo.Value);
				if (s.Slots != null && !RebuildReplaySetSlots(state, s.Slots.Weapon, s.Slots.Armor)) return;
				if (s.SetItems) RebuildReplaySetItems(state, s.Items);
				int removed = RebuildReplaySetStats(state, s.Snapshot.Stats);
				state.Applied(s);
				if (beforeRpm)
				{
					RebuildReplayRpmDay = s;
					RebuildReplayLocationBefore = locationBefore;
					RebuildReplayRemoved = removed;
					return;
				}
				RebuildReplayFinish(state, s, locationBefore, removed, state.RecomputeRpm);
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogException(e);
				RebuildFail(SimulationHarness.ExitException, "RebuildApplyDayOverride(" + day + "): " + e.GetType().Name + ": " + e.Message);
			}
		}

		private void RebuildReplayFinish(SimulationStateOverride state, SimulationStateOverride.DayState s,
			int locationBefore, int removed, bool recomputeRpm)
		{
			if (state.TotalValueFromSnapshot) RebuildReplaySetTotalValues(state, s.Snapshot.Stats);
			playerModel.Unit.CurrentHealth = GetForecastStatValue(StatType.Health);
			CalculateSimulationData();
			RebuildApplyReplayCaches();
			if (recomputeRpm)
			{
				CalculateSoulRPMValues();
				foreach (StatType type in Enum.GetValues(typeof(StatType))) CalculateRPMValueTimeChunk(type);
			}
			RebuildCount("day_overrides");
			JObject record = RebuildReplayRecord(state, s, locationBefore, removed);
			RebuildCrosscheckDay(record);
			state.Record(record);
		}

		// After CalculateSimulationData (Tick, FastTick planning rounds, the override): the stored DpsData/DefenseData
		// of the day replace the recomputed ones, then the combat caches and the GPM spawnerData are rebuilt on them.
		[SimulationRebuild(Provenance.DebugOnly, "simulation.state_override")]
		partial void RebuildApplyReplayCaches()
		{
			SimulationStateOverride state = SimulationStateOverride.For(SimulationHarness.CurrentJob);
			if (state == null || !state.TryStoredCaches(timeChunkIndex, out List<SimulationDpsData> dps, out List<SimulationDpsData> defense)) return;
			currentDpsData.Clear();
			currentDpsData.AddRange(dps);
			currentDefenseData.Clear();
			currentDefenseData.AddRange(defense);
			UpdateDpsDataCache();
			RebuildPerFrameCaches();
			CalculateSpawnerData(true);
			state.Count("cache_applications");
		}

		// Islands up to `location`: the forward gates into each new island (on an island already open, so not the
		// back-gates of islands 11-13) open (GatesModel) with their SlotReward slots, then RebuildUnlockLocation (C3b
		// opens slots only when a gate spawner is cleared).
		private void RebuildReplayUnlock(SimulationStateOverride state, int location)
		{
			for (int n = playerModel.LastUnlockedLocation.Number + 1; n <= location; n++)
			{
				foreach (EnemySpawner spawner in enemiesModel.Spawners)
				{
					EnemySpawnerConfig def = spawner.Config;
					if (def.LocationNumber >= n || def.Gates == null || !def.Gates.Any(g => g != null && g.NextLocationNumber == n)) continue;
					foreach (var gate in def.Gates)
						if (gate != null && gate.NextLocationNumber == n && !gatesModel.IsGateUnlocked(gate.Key))
						{
							gatesModel.OpenGate(gate.Key);
							state.Count("gates_opened");
						}
					if (string.IsNullOrEmpty(def.SlotReward)) continue;
					ItemSlotModel slot = playerModel.ItemSlots.FirstOrDefault(x => x.Key == def.SlotReward);
					if (slot == null || slot.IsUnlocked) continue;
					slot.IsUnlocked = true;
					state.Count("slot_rewards_opened");
				}
				RebuildUnlockLocation(n);
				state.Count("unlocks");
			}
		}

		// Exactly `weapons` weapon and `armors` helmet/shield slots unlocked: non-VIP slots first, config order.
		private bool RebuildReplaySetSlots(SimulationStateOverride state, int weapons, int armors)
		{
			var weaponSlots = playerModel.ItemSlots.Where(x => x.SlotType == ItemSlotType.Weapon).OrderBy(x => x.IsVip).ToList();
			var armorSlots = playerModel.ItemSlots.Where(x => x.SlotType == ItemSlotType.Helmet || x.SlotType == ItemSlotType.Shield)
				.OrderBy(x => x.IsVip).ToList();
			if (weapons > weaponSlots.Count || armors > armorSlots.Count)
			{
				RebuildFail(SimulationHarness.ExitBadRequest, "override: slots " + weapons + "/" + armors + " exceed the player's "
					+ weaponSlots.Count + " weapon / " + armorSlots.Count + " armor slots");
				return false;
			}
			RebuildReplaySetSlotsOpen(state, weaponSlots, weapons);
			RebuildReplaySetSlotsOpen(state, armorSlots, armors);
			return true;
		}

		private void RebuildReplaySetSlotsOpen(SimulationStateOverride state, List<ItemSlotModel> slots, int n)
		{
			for (int i = 0; i < slots.Count; i++)
			{
				bool open = i < n;
				if (slots[i].IsUnlocked == open) continue;
				if (!open && slots[i].ItemModel != null)
				{
					playerModel.UnequipItem(slots[i].ItemModel);
					state.Count("slot_unequips");
				}
				slots[i].IsUnlocked = open;
				state.Count(open ? "slots_opened" : "slots_closed");
			}
		}

		// The player's items become `specs`, in their order (CalculateDpsData breaks ties by list order): existing
		// ItemModels are set in place, new ones go through AddItem, the rest are unequipped and dropped.
		private void RebuildReplaySetItems(SimulationStateOverride state, List<SimulationStateOverride.ItemSpec> specs)
		{
			ItemsProvider provider = GameContext.Get<ItemsProvider>();
			var ordered = new List<ItemModel>();
			foreach (SimulationStateOverride.ItemSpec spec in specs)
			{
				ItemModel item = playerModel.Items.FirstOrDefault(x => x.Config.Key == spec.Key);
				bool added = false;
				if (item == null)
				{
					ItemConfig config = provider.Get(spec.Key);
					if (config == null)
					{
						state.Count("items_missing_config");
						continue;
					}
					item = new ItemModel(playerModel.Lifetime, config);
					added = true;
				}
				item.Evolved = spec.Evolved;
				item.Level = spec.Evolved ? spec.Level : 1;
				item.Copies = spec.Copies;
				if (added)
				{
					playerModel.AddItem(item, false, true);
					state.Count("items_added");
				}
				ordered.Add(item);
			}
			List<ItemModel> current = playerModel.Items;
			foreach (ItemModel item in current.Where(x => !ordered.Contains(x)).ToList())
			{
				playerModel.UnequipItem(item);
				state.Count("items_removed");
			}
			if (current.SequenceEqual(ordered)) return;
			MethodInfo setter = typeof(PlayerModel).GetProperty("Items", BindingFlags.Instance | BindingFlags.Public).GetSetMethod(true);
			setter.Invoke(playerModel, new object[] { ordered });
			state.Count("items_reordered");
		}

		// Base value = snapshot Value; modifiers other than the two __simulation* ones and equipment's are removed
		// (on a bot they are rewards), and so is the previous override's TotalValue correction. Returns the number
		// removed (corrections not counted).
		private int RebuildReplaySetStats(SimulationStateOverride state, List<SimulationStatData> values)
		{
			int removed = 0;
			foreach (Stat stat in playerModel.Unit.Stats)
			{
				foreach (StatModifier modifier in stat.Modifiers.Where(m => m.Name != null && m.Name.StartsWith(SimulationStateOverride.TotalValueModifierName, StringComparison.Ordinal)).ToList())
					stat.RemoveModifier(modifier);
				int index = values.FindIndex(x => x.Key == stat.Type);
				if (index < 0)
				{
					state.Count("stats_missing_in_snapshot");
					continue;
				}
				foreach (StatModifier modifier in stat.Modifiers.Where(m => m.Name != SimulationStatsFromFeaturesFlatModifierName
					         && m.Name != SimulationInterpolationStatModifierName && m.Source != ModifierSource.Item).ToList())
				{
					if (modifier.Source != ModifierSource.Reward) state.Count("removed_modifiers_source_" + (int)modifier.Source);
					stat.RemoveModifier(modifier);
					removed++;
				}
				stat.BaseValue = values[index].Value;
				// StatBase recomputes Value only when a modifier changes.
				var refresh = new StatModifier { Name = "__rebuildReplayRefresh", StatType = stat.Type, ModifierType = ModifierType.Flat, Source = ModifierSource.Reward, Value = 0f };
				stat.AddModifier(refresh);
				stat.RemoveModifier(refresh);
			}
			return removed;
		}

		// total_value = snapshot: Stat.FastValue becomes the snapshot's TotalValue, which on interpolated days is not
		// Value composed with K2/K3(d) (TotalValue is interpolated linearly, K2/K3 are not). The factor rides on two
		// Item-source PreAdditivePostMultiplicative modifiers: Item modifiers stay out of the snapshot Value, and the
		// two float values sum in double, so the factor holds to about an ulp; the second one is tuned over its float
		// neighbours. Equipment changes later in the day (ApplyEquippedItemsToUnit) drop them.
		private void RebuildReplaySetTotalValues(SimulationStateOverride state, List<SimulationStatData> values)
		{
			foreach (Stat stat in playerModel.Unit.Stats)
			{
				int index = values.FindIndex(x => x.Key == stat.Type);
				if (index < 0) continue;
				double want = values[index].TotalValue;
				double have = stat.FastValue;
				if (have == want) continue;
				if (have == 0.0 || want == 0.0 || double.IsNaN(want) || double.IsInfinity(want) || double.IsNaN(have) || double.IsInfinity(have))
				{
					state.Count("total_value_uncorrected");
					continue;
				}
				double factor = want / have - 1.0;
				float coarse = (float)factor;
				float fine = (float)(factor - coarse);
				var first = new StatModifier { Name = SimulationStateOverride.TotalValueModifierName + "1", StatType = stat.Type, ModifierType = ModifierType.PreAdditivePostMultiplicative, Source = ModifierSource.Item, Value = coarse };
				var second = new StatModifier { Name = SimulationStateOverride.TotalValueModifierName + "2", StatType = stat.Type, ModifierType = ModifierType.PreAdditivePostMultiplicative, Source = ModifierSource.Item, Value = fine };
				stat.AddModifier(first);
				stat.AddModifier(second);
				float best = fine;
				double bestError = Math.Abs(stat.FastValue - want);
				for (int direction = -1; direction <= 1 && bestError > 0.0; direction += 2)
				{
					float candidate = fine;
					for (int step = 0; step < 8 && bestError > 0.0; step++)
					{
						candidate = RebuildNextFloat(candidate, direction);
						stat.RemoveModifier(second);
						second.Value = candidate;
						stat.AddModifier(second);
						double error = Math.Abs(stat.FastValue - want);
						if (error < bestError)
						{
							bestError = error;
							best = candidate;
						}
					}
				}
				if (second.Value != best)
				{
					stat.RemoveModifier(second);
					second.Value = best;
					stat.AddModifier(second);
				}
				state.Count("total_value_corrections");
				if (stat.FastValue != want) state.Count("total_value_corrections_inexact");
			}
		}

		private static float RebuildNextFloat(float value, int direction)
		{
			if (value == 0f) return direction > 0 ? float.Epsilon : -float.Epsilon;
			int bits = BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
			bits += (value > 0f) == (direction > 0) ? 1 : -1;
			return BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
		}

		private JObject RebuildReplayRecord(SimulationStateOverride state, SimulationStateOverride.DayState s, int locationBefore, int removed)
		{
			var stats = new JArray();
			foreach (Stat stat in playerModel.Unit.Stats)
				stats.Add(new JArray((int)stat.Type, SimulationRuntimeProbes.D(GetSimulationSnapshotStatValue(stat)), SimulationRuntimeProbes.D(stat.FastValue),
					SimulationRuntimeProbes.D(GetForecastStatValue(stat.Type))));
			var items = new JArray();
			foreach (ItemModel item in playerModel.Items)
				items.Add(new JArray(item.Config.Key, item.Level, item.Copies, item.Evolved, item.EffectiveLevel));
			return new JObject
			{
				["day"] = s.Day,
				["time_chunk_index"] = timeChunkIndex,
				["kind"] = state.Mode,
				["snapshot_time_chunk"] = s.Snapshot.TimeChunkIndex,
				["snapshot_location"] = s.Snapshot.Location,
				["left"] = s.LeftTimeChunk,
				["right"] = s.RightTimeChunk,
				["t"] = float.IsNaN(s.T) ? null : SimulationRuntimeProbes.F(s.T),
				["items_time_chunk"] = s.ItemsTimeChunk,
				["items_set"] = s.SetItems,
				["slots_set"] = s.Slots != null,
				["stored_caches"] = s.StoredCaches,
				["location_before"] = locationBefore,
				["location"] = playerModel.LastUnlockedLocation.Number,
				["current_boss"] = RebuildSpawnerId(currentBoss),
				["weapon_slots"] = playerModel.UnlockedWeaponSlots,
				["armor_slots"] = playerModel.UnlockedArmorSlots,
				["slot_states"] = new JArray(playerModel.ItemSlots.Select(x => (object)new JArray(x.Key, (int)x.SlotType, x.IsVip, x.IsUnlocked,
					x.ItemModel != null ? x.ItemModel.Config.Key : null)).ToArray()),
				["removed_modifiers"] = removed,
				["k2"] = SimulationRuntimeProbes.F(RebuildK2),
				["k3"] = SimulationRuntimeProbes.F(RebuildK3),
				["modifier_day"] = RebuildModifierDay,
				["current_health"] = SimulationRuntimeProbes.D(playerModel.Unit.CurrentHealth),
				["stats"] = stats,
				["items"] = items,
				["dps"] = RebuildReplayPairs(currentDpsData),
				["dps_cache"] = RebuildReplayPairs(dpsDataCache.OrderBy(x => x.Key).Select(x => x.Value)),
				["defense"] = RebuildReplayPairs(currentDefenseData),
				["defense_cache"] = RebuildReplayPairs(defenseDataCache.OrderBy(x => x.Key).Select(x => x.Value)),
				["rpm"] = new JArray(RPMCalculatedValue.OrderBy(x => x.Key).Select(x => (object)new JArray((int)x.Key, SimulationRuntimeProbes.D(x.Value))).ToArray()),
			};
		}

		private static JArray RebuildReplayPairs(IEnumerable<SimulationDpsData> data) =>
			new JArray(data.Select(x => (object)new JArray((int)x.WeaponType, SimulationRuntimeProbes.D(x.Value))).ToArray());
	}
}
