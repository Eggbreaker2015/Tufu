using System;
using Codebase.Abilities.Core;
using Codebase.Unit;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class BuffNextHitLifestealFlatEffect : AbilityEffectBase
	{
		public AbilityValueScaling ValueScaling;

		public StatType ScalingStat;

		public override void Execute(AbilityContext context)
		{
            // Original ELF VA 0x15eeeb4; FCMP/B.LS rejects nonpositive values but permits unordered NaN.
            float value = ValueScaler.GetScaledValue(context, ValueScaling, ScalingStat);
            if (value <= 0f) return;
            context.PlayerModel.NextHitLifestealFlatBonus = value + context.PlayerModel.NextHitLifestealFlatBonus;
        }
	}
}
