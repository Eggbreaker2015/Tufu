using System;
using Codebase.Abilities.Core;
using Codebase.Unit;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class BoostAbilityStackLimitEffect : AbilityEffectBase, IAbilityBoostEffect, IAbilityEffect
	{
		public int TargetAbilityIndex;

		public AbilityValueScaling ValueScaling;

		public StatType ScalingStat;

		public bool BoostNextProcOnly;

		int IAbilityBoostEffect.TargetAbilityIndex => this.TargetAbilityIndex;

		public override bool HasPassiveTick => !BoostNextProcOnly; // Native 0x15eeaa4.

		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
			// Native ELF VA 0x15eeab4.
			if (!BoostNextProcOnly) ApplyBonus(context, false);
		}

		public override void Execute(AbilityContext context)
		{
			// Native ELF VA 0x15eec38.
			if (BoostNextProcOnly) ApplyBonus(context, true);
		}

		private void ApplyBonus(AbilityContext context, bool nextProcOnly)
		{
			// Native ELF VA 0x15eeac8.
			var states = context.Owner.AbilityStates;
			if (TargetAbilityIndex < 0 || TargetAbilityIndex >= states.Count) return;
			float value = ValueScaler.GetScaledValue(context, ValueScaling, ScalingStat);
			// Original FCVTZS truncates toward zero.
			int whole = (int)value;
			if (nextProcOnly) states[TargetAbilityIndex].NextProcBonusStackLimit += whole;
			else states[TargetAbilityIndex].BonusStackLimit += whole;
		}
	}
}
