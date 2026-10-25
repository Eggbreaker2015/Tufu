using System;

namespace Codebase.Crystals
{
	[Serializable]
	public class RarityEnchantMaturityDef
	{
		public CrystalRarity Rarity;

		public int MaturityLevel;

		public float MaturityChancePercent;

		public float PostMaturityChanceStepPercent;
	}
}
