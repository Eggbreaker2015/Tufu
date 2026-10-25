using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class LethalIgnorePassiveEffect : AbilityEffectBase
	{
		public LethalIgnoreMode Mode;

		public override bool HasPassiveTick => true;

		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
            // Original ELF VA 0x15f0ce4. Native B.GT skips the active cooldown only; NaN follows the chance branch.
            context.PlayerModel.LethalIgnoreCooldown = context.State.Cooldown;
            if (context.State.Cooldown > 0f) return;
            context.PlayerModel.LethalIgnoreChance += context.State.GetChance();
            if (Mode == LethalIgnoreMode.PerTarget) context.PlayerModel.HasPerTargetLethalIgnore = true;
        }
	}
}
