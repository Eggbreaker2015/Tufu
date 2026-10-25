using System;
using Codebase.Abilities.Core;
using Codebase.Unit;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class BuffNextHitDamageEffect : AbilityEffectBase
	{
		public AbilityValueScaling ValueScaling;

		public StatType ScalingStat;

		public override void Execute(AbilityContext context)
		{
            // Original ELF VA 0x15eee2c. No positive-value gate or replacement of an existing bonus.
            float value = context.State.GetValue() * ValueScaler.Resolve(ValueScaling, context, ScalingStat);
            context.PlayerModel.NextHitDamageBonus = value + context.PlayerModel.NextHitDamageBonus;
        }
	}
}
