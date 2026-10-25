using Codebase.Player.Inventory;
using System;

namespace Codebase.Meta
{
	public static class ItemDropAmountCalculator
	{
		public static int GetFlatBonus(ItemConfig item, MetaStatsModel metaStatsModel)
		{
			if (item == null)
				return 0;
			return (int)metaStatsModel.Get("items_rarity_drop_amount").GetValue(RarityStatParam.From(item.Rarity));
		}

		public static int CalculatePerKill(ItemConfig item, MetaStatsModel metaStatsModel)
		{
			return item == null ? 0 : GetFlatBonus(item, metaStatsModel) + 1;
		}

		public static int ResolveKillDropCopies(ItemConfig item, MetaStatsModel metaStatsModel, int remainingCharges, out int additionalChargesToConsume)
		{
			int copies = Math.Min(CalculatePerKill(item, metaStatsModel), remainingCharges + 1);
			additionalChargesToConsume = Math.Max(0, copies - 1);
			return copies;
		}

		public static double CalculateForKillCount(ItemConfig item, double killCount, MetaStatsModel metaStatsModel)
		{
			return CalculatePerKill(item, metaStatsModel) * killCount;
		}
	}
}
