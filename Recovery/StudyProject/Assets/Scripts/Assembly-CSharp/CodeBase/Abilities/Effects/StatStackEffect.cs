using System;
using Codebase.Abilities.Core;
using Codebase.Unit;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class StatStackEffect : AbilityEffectBase
	{
		public StatType StatType;

		public ModifierType ModifierType;

		public ModifierSource Source;

		public bool ResetOnTargetChange;

		[NonSerialized]
		private string cachedKey;

		public override bool HasPassiveTick => true;

		public override void Execute(AbilityContext context)
		{
            // Original ELF VA 0x15f2700. Original limits are not clamped nonnegative.
            if (ShouldResetByTarget(context)) context.State.Stacks = 0;
            context.State.Stacks = System.Math.Min(context.State.GetStackLimit(), context.State.Stacks + 1);
        }

		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
            // Original ELF VA 0x15f28d0; target changes reset stacks and capture the new current target before publishing.
            if (ShouldResetByTarget(context))
            {
                context.State.Stacks = 0;
                context.State.CurrentTarget = context.PlayerModel.Unit.CurrentTarget;
            }
            float value = context.State.GetValue() * context.State.Stacks;
            if (cachedKey == null) cachedKey = AddStatPassiveEffect.BuildKey(context.Def.Key, StatType, ModifierType, Source);
            AddStatPassiveEffect.PublishDesired(cachedKey, StatType, ModifierType, Source, value);
        }

		private bool ShouldResetByTarget(AbilityContext context)
		{
            // Original ELF VA 0x15f27c8; both Unity targets must exist before inequality resets the stack.
            return ResetOnTargetChange && context.State.CurrentTarget != null && context.PlayerModel.Unit.CurrentTarget != null && context.State.CurrentTarget != context.PlayerModel.Unit.CurrentTarget;
        }
	}
}
