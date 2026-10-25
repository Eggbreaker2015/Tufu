using System;
using Codebase.Player.Inventory;

namespace Codebase.BossTower
{
	[Serializable]
	public struct BossDropItemData
	{
		public Rarity rarity;

		public int floorStart;

		public double addItemDrop;

		public double modItemDrop;

		public int startItemDrop;
	}
}
