using System;
using Codebase.Abilities.Core;
using Codebase.Unit;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class LightningDamageBonusEffect : AbilityEffectBase
	{
		public AbilityValueScaling ValueScaling;

		public StatType ScalingStat;

		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
			// Original ELF VA 0x15f0d6c.
			context.PlayerModel.LightningDamageBonus += ValueScaler.GetScaledValue(context, ValueScaling, ScalingStat);
		}
	}
}
