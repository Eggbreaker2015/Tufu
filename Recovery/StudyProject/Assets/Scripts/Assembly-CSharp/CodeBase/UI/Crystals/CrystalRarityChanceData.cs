using Codebase.Crystals;

namespace Codebase.UI.Crystals
{
	public readonly struct CrystalRarityChanceData
	{
		public CrystalRarity Rarity { get; }

		public float CurrentChance { get; }

		public float NextChance { get; }

		public CrystalRarityChanceData(CrystalRarity rarity, float currentChance, float nextChance)
		{
            // Native ELF0x13b4840.
            Rarity = rarity; CurrentChance = currentChance; NextChance = nextChance;
        }
	}
}
