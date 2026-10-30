using System.Collections.Generic;
using Codebase.Core;
using Codebase.Economy;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Rewards;
using UnityEngine;

namespace Codebase.Pet
{
	// Universal shards: one currency that adds copies to any owned pet or unlocks an unowned species.
	public static class PetEconomy
	{
		public static int Shards
		{
			get
			{
				CurrencyModel currency = GameContext.Get<CurrencyModel>();
				return currency == null ? 0 : (int)currency.Get(CurrencyKeys.PetUniversalShard);
			}
		}

		public static void AddShards(int amount)
		{
			if (global::Codebase.Rpg.RpgModeConfig.Enabled) return;
			if (!PetConfig.Enabled || amount <= 0)
				return;
			GameContext.Get<CurrencyModel>()?.Add(CurrencyKeys.PetUniversalShard, amount);
		}

		public static void OnChestsOpened(int chestCount)
		{
			if (!PetConfig.Enabled || chestCount <= 0)
				return;
			AddShards(PetConfig.Instance.ShardsPerChest * chestCount);
		}

		// Keyed species of a weapon item (fallback entries only borrow visuals and are not collectable).
		public static PetDefinition FindSpecies(ItemConfig config)
		{
			PetConfig petConfig = PetConfig.Instance;
			if (petConfig == null || config == null || IsArmor(config))
				return null;
			foreach (PetDefinition definition in petConfig.Pets)
				if (!definition.IsFallback && definition.ItemKey == config.Key)
					return definition;
			return null;
		}

		public static bool IsOwned(ItemConfig config)
		{
			PlayerModel playerModel = GameContext.Get<PlayerModel>();
			if (playerModel == null || config == null)
				return false;
			foreach (ItemModel item in playerModel.Items)
				if (item.Config != null && item.Config.Key == config.Key)
					return true;
			return false;
		}

		// 0: not unlockable with shards.
		public static int GetUnlockCost(ItemConfig config)
		{
			if (!PetConfig.Enabled || FindSpecies(config) == null)
				return 0;
			int[] costs = PetConfig.Instance.UnlockShardCost;
			int index = (int)config.Rarity;
			return costs == null || index < 0 || index >= costs.Length ? 0 : Mathf.Max(0, costs[index]);
		}

		public static bool CanUnlock(ItemConfig config)
		{
			int cost = GetUnlockCost(config);
			return cost > 0 && !IsOwned(config) && Shards >= cost;
		}

		public static bool IsEvolutionCapped(ItemModel item) => item != null && item.IsEvolutionCapped;

		// Shards the 万能碎片 button spends: what is missing to the next evolution, at least one, at most what the player has.
		public static int GetShardSpendAmount(ItemModel item)
		{
			if (!PetConfig.Enabled || item == null || item.IsArmor || IsEvolutionCapped(item))
				return 0;
			int have = Shards;
			if (have <= 0)
				return 0;
			return Mathf.Min(have, Mathf.Max(1, item.LevelsToEvolve - item.Copies));
		}

		public static PetDefinition FindBossPet(string bossKey)
		{
			PetConfig petConfig = PetConfig.Instance;
			if (petConfig == null || string.IsNullOrEmpty(bossKey))
				return null;
			foreach (PetDefinition definition in petConfig.Pets)
				if (!definition.IsFallback && definition.BossKey == bossKey)
					return definition;
			return null;
		}

		// Boss Tower victory in pet mode: shards and copies of the boss's own pet, granted with the victory rewards.
		public static void AppendBossWinRewards(List<RewardBase> rewards, string bossKey, ItemsProvider itemsProvider)
		{
			if (!PetConfig.Enabled || rewards == null)
				return;
			PetConfig petConfig = PetConfig.Instance;
			PetDefinition definition = FindBossPet(bossKey);
			ItemConfig item = definition == null || itemsProvider == null ? null : FindItem(itemsProvider, definition.ItemKey);
			if (item != null && petConfig.BossPetCopiesPerWin > 0)
				rewards.Add(new ItemReward { Def = item, Level = petConfig.BossPetCopiesPerWin, SlotType = item.SlotType, Rarity = item.Rarity });
			else if (definition == null)
				Debug.LogWarning("[Pet] No boss pet for boss '" + bossKey + "'");
			if (petConfig.ShardsPerBossWin > 0)
			{
				EnsureShardStyle();
				rewards.Add(new CurrencyReward { Key = CurrencyKeys.PetUniversalShard, Amount = petConfig.ShardsPerBossWin });
			}
		}

		// Lets CurrencyReward / CurrencyView find the shard icon without a row in Currency Style Config.
		public static void EnsureShardStyle()
		{
			PetConfig petConfig = PetConfig.Instance;
			CurrencyStyleConfig styles = GameContext.Get<CurrencyStyleConfig>();
			if (petConfig == null || petConfig.ShardSprite == null || styles == null || styles.GetStyle(CurrencyKeys.PetUniversalShard) != null)
				return;
			styles.Styles.Add(new CurrencyStyle { Key = CurrencyKeys.PetUniversalShard, Icon = petConfig.ShardSprite });
		}

		private static ItemConfig FindItem(ItemsProvider itemsProvider, string key)
		{
			foreach (ItemConfig item in itemsProvider.Items)
				if (item != null && item.Key == key)
					return item;
			return null;
		}

		private static bool IsArmor(ItemConfig config) => config.SlotType == ItemSlotType.Helmet || config.SlotType == ItemSlotType.Shield;
	}
}
