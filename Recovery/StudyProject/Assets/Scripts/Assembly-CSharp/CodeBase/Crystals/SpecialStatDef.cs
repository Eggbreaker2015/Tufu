using System;
using System.Collections.Generic;
using Codebase.Unit;

namespace Codebase.Crystals
{
	[Serializable]
	public class SpecialStatDef
	{
		public StatType StatType;
		public ModifierType ModifierType;
		public List<float> ValuesByEnchantLevel;
		public bool LockedByDefault;
		public float GetValueAtEnchantLevel(int enchantLevel)
		{
			return enchantLevel > 0 && ValuesByEnchantLevel.Count != 0 ? ValuesByEnchantLevel[Math.Min(enchantLevel - 1, ValuesByEnchantLevel.Count - 1)] : 0f;
		}
	}
}
