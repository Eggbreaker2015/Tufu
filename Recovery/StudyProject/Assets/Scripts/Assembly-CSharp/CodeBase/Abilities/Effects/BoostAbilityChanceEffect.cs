using System;
using Codebase.Abilities.Core;
using Codebase.Unit;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class BoostAbilityChanceEffect : AbilityEffectBase, IAbilityBoostEffect, IAbilityEffect
	{
		public int TargetAbilityIndex;

		public AbilityValueScaling ValueScaling;

		public StatType ScalingStat;

		public bool BoostNextProcOnly;

		int IAbilityBoostEffect.TargetAbilityIndex => this.TargetAbilityIndex;

		public override bool HasPassiveTick => !BoostNextProcOnly; // Native 0x15ee900.

		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
			// Native ELF VA 0x15ee910.
			if (!BoostNextProcOnly) ApplyBonus(context, false);
		}

		public override void Execute(AbilityContext context)
		{
			// Native ELF VA 0x15eea80.
			if (BoostNextProcOnly) ApplyBonus(context, true);
		}

		private void ApplyBonus(AbilityContext context, bool nextProcOnly)
		{
			// Native ELF VA 0x15ee924.
			var states = context.Owner.AbilityStates;
			if (TargetAbilityIndex < 0 || TargetAbilityIndex >= states.Count) return;
			float value = ValueScaler.GetScaledValue(context, ValueScaling, ScalingStat);

			if (nextProcOnly) states[TargetAbilityIndex].NextProcBonusChance += value;
			else states[TargetAbilityIndex].BonusChance += value;
		}
	}
}
