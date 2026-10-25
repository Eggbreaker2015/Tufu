using System;
using System.Collections.Generic;
using Codebase.Player.Inventory;

namespace Codebase.Balance
{
	[Serializable]
	public class BalanceInterpolationsConfig
	{
		public List<BalanceInterpolation> Interpolations = new List<BalanceInterpolation>();

		public float GetTotalCoefficient(float timing, BalanceInterpolationType type, Rarity rarity = Rarity.Rare)
		{
// Original ELF VA 0x1579dac: multiplication for types 1/3, addition for 0/2/4/5/6/7.
            if (rarity == Rarity.Legendary && type == BalanceInterpolationType.ItemEmission)
                type = BalanceInterpolationType.ItemLegendaryEmission;
            bool multiply = !((uint)type < 8u && ((1u << (int)type) & 0xf5u) != 0u);
            float total = multiply ? 1f : 0f;
            for (int i = 0; i < Interpolations.Count; i++)
            {
                var interpolation = Interpolations[i];
                if (interpolation.Type != type || !interpolation.Use) continue;
                float value = interpolation.Evaluate(timing);
                total = multiply ? total * value : total + value;
            }
            return total;
		}

		public string GetDebugInfo(float timing)
		{
			return null;
		}
	}
}
