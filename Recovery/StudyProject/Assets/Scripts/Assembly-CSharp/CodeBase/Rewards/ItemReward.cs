using System;
using Codebase.Player.Inventory;

namespace Codebase.Rewards
{
	[Serializable]
	public class ItemReward : RewardBase
	{
		public bool IsRandom;
		public ItemConfig Def;
		public Rarity Rarity;
		public ItemSlotType SlotType;
		public string SaveKey;
		public int Level = 1;
		[NonSerialized]
		public BakedItemReward BakedData;
		[NonSerialized]
		public bool ForceShowAsRandom;
		[NonSerialized]
		public int? BakedTotalCopies;
	}
}
