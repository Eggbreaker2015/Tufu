using Codebase.Player.Inventory;

namespace Codebase.UI.Shop
{
	public readonly struct ChestRarityChanceData
	{
		public Rarity Rarity { get; }

		public float Chance { get; }

		public int CurrentCopies { get; }

		public int NextCopies { get; }

		public bool ShowCopies { get; }

		public ChestRarityChanceData(Rarity rarity, float chance, int currentCopies, int nextCopies)
		{
			Rarity = rarity;
			Chance = chance;
			CurrentCopies = currentCopies;
			NextCopies = nextCopies;
			ShowCopies = true;
		}

		public ChestRarityChanceData(Rarity rarity, float chance)
		{
			Rarity = rarity;
			Chance = chance;
			CurrentCopies = 0;
			NextCopies = 0;
			ShowCopies = false;
		}
	}
}
