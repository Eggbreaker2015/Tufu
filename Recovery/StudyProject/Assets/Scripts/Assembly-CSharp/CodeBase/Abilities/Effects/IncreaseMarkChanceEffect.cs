using System;
using Codebase.Abilities.Core;
using Codebase.Unit;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class IncreaseMarkChanceEffect : AbilityEffectBase
	{
		public bool UseAbilityValue;

		public float Amount;

		public AbilityValueScaling ValueScaling;

		public StatType ScalingStat;

		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
			// Original ELF VA 0x15f0c3c.
			float value = UseAbilityValue ? context.State.GetValue() : Amount;
			context.PlayerModel.BonusMarkChance += value * ValueScaler.Resolve(ValueScaling, context, ScalingStat);
		}
	}
}
