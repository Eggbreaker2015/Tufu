using System;
using Codebase.Abilities.Core;
using Codebase.Unit;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class IncreaseMaxMarksEffect : AbilityEffectBase
	{
		public bool UseAbilityValue;

		public int Count = 1;

		public AbilityValueScaling Scaling;

		public StatType ScalingStat;

		public int GetCount(AbilityState state)
		{
			// Original ELF VA 0x15ee300. The original ARM64 conversion saturates
			// finite overflow, yields zero for NaN, and special-cases positive infinity.
			if (!UseAbilityValue) return Count;
			float value = state.GetValue();
			if (float.IsNaN(value)) return 0;
			if (float.IsPositiveInfinity(value)) return int.MinValue;
			if (value >= 2147483648f) return int.MaxValue;
			if (value <= -2147483648f) return int.MinValue;
			return (int)value;
		}
	}
}
