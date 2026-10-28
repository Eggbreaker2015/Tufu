using System.Collections.Generic;
using Codebase.Player.Inventory;
using System.Linq;
using UnityEngine;

namespace Codebase.Rewards
{
	public static class BossRewardResolver
	{
		public static List<RewardBase> PreResolveAndBakeDisplay(List<RewardBase> rewards, ItemsProvider itemsProvider)
		{
			var result = new List<RewardBase>(rewards.Count);
			foreach (RewardBase reward in rewards)
			{
				if (!(reward is ItemReward item))
				{
					result.Add(reward);
					continue;
				}

				if (item.IsRandom)
				{
					ItemConfig def = PickRandom(item, itemsProvider);
					if (def != null)
						result.Add(new ItemReward { Def = def, IsRandom = false, Level = item.Level, BakedData = new BakedItemReward { ItemKey = def.Key } });
				}
				else
					result.Add(new ItemReward { Def = item.Def, Level = item.Level, IsRandom = item.IsRandom, SlotType = item.SlotType });
			}

			return result;
		}

		private static ItemConfig PickRandom(ItemReward reward, ItemsProvider itemsProvider)
		{
			var candidates = itemsProvider.Items.Where(item => ItemsProvider.IsAvailable(item) && item.SlotType == reward.SlotType && item.Rarity == reward.Rarity).ToList();
			return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
		}
	}
}
