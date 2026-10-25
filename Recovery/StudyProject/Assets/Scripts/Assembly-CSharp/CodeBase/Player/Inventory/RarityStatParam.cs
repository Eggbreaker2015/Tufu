namespace Codebase.Player.Inventory
{
	public static class RarityStatParam
	{
		public static int From(Rarity rarity)
		{
// Original two-instruction body ELF VA 0x146639c.
            return (int)rarity + 1;
		}

		public static Rarity ToRarity(int param)
		{
// Original two-instruction body ELF VA 0x14663a4.
            return (Rarity)(param - 1);
		}
	}
}
