using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class BuffNextHitSuperCritEffect : AbilityEffectBase
	{
		public override void Execute(AbilityContext context)
		{
            // Original ELF VA 0x15eef04; the same original value affects both chance and damage.
            float value = context.State.GetValue();
            context.PlayerModel.NextHitSuperCritChanceBonus += value;
            context.PlayerModel.NextHitSuperCritDamageBonus = value + context.PlayerModel.NextHitSuperCritDamageBonus;
        }
	}
}
