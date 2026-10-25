using System;

namespace Codebase.Crystals
{
	[Serializable]
	public class RarityWeightDef
	{
		public CrystalRarity Rarity;

		public int MaturityLevel;

		public float PreMaturityDecayCoef;

		public float BaseWeight;

		public float BaseGrowValue;
	}
}
