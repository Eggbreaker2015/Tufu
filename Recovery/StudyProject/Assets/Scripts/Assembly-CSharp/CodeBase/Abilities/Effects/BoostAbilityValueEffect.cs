using System;
using Codebase.Abilities.Core;
using Codebase.Unit;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class BoostAbilityValueEffect : AbilityEffectBase, IAbilityBoostEffect, IAbilityEffect
	{
		public int TargetAbilityIndex;

		public AbilityValueScaling ValueScaling;

		public StatType ScalingStat;

		public bool BoostNextProcOnly;

		public bool IsPercent;

		int IAbilityBoostEffect.TargetAbilityIndex => this.TargetAbilityIndex;

		public override bool HasPassiveTick => !BoostNextProcOnly; // Native 0x15eec5c.

		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
			// Native ELF VA 0x15eec6c.
			if (!BoostNextProcOnly) ApplyBonus(context, false);
		}

		public override void Execute(AbilityContext context)
		{
			// Native ELF VA 0x15eee10.
			if (BoostNextProcOnly) ApplyBonus(context, true);
		}

		private void ApplyBonus(AbilityContext context, bool nextProcOnly)
		{
			// Native ELF VA 0x15eec80.
			var states = context.Owner.AbilityStates;
			if (TargetAbilityIndex < 0 || TargetAbilityIndex >= states.Count) return;
			float value = ValueScaler.GetScaledValue(context, ValueScaling, ScalingStat);
			if (IsPercent) value *= states[TargetAbilityIndex].GetBaseValue();
			if (nextProcOnly) states[TargetAbilityIndex].NextProcBonusValue += value;
			else states[TargetAbilityIndex].BonusValue += value;
		}
	}
}
