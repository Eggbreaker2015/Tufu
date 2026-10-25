namespace Codebase.Economy
{
	public static class CurrencyKeys
	{
		public const string BossTowerTickets = "boss_tower.tickets";
		public const string CrystalMagicDust = "crystal.magic_dust";
		public const string CrystalGeodes = "crystal.geodes";
		public const string CrystalEnchantmentStones = "crystal.enchantment_stones";
		public const string SoulPrefix = "soul.";
		public const string EmptyAmulet = "empty_amulet";
		public const string QuestDynamite = "quest.dynamite";
		public const string QuestHelmet = "quest.helmet";
		public const string QuestPickaxe = "quest.pickaxe";
		public const string QuestMineCart = "quest.mine_cart";
		public const string QuestGasMask = "quest.gas_mask";
		public static bool IsQuestCurrency(string key)
		{ // Original ELF0x1516454; five original string equalities, not a prefix test.
			return key == QuestDynamite || key == QuestHelmet || key == QuestPickaxe || key == QuestMineCart || key == QuestGasMask;
		}
	}
}
