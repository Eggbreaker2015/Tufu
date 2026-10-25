using CodeBase.Audio;
using Codebase.Unit;
using Codebase.UI.Damage;
using Codebase.Abilities.Vfx;
using Codebase.Core;
using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class HealPercentEffect : AbilityEffectBase
	{
		public HealVfxSize VfxSize;

		public override void Execute(AbilityContext context)
		{
            // Native 0x15f0814: clamp only the maximum, then retain original status/VFX/audio order.
            var unit = context.PlayerUnit;
            double maximum = unit.GetStatValue(StatType.Health);
            if (unit.IsDead) return;
            unit.CurrentHealth = Math.Min(maximum, unit.CurrentHealth + maximum * context.State.GetValue());
            GameContext.Get<PlayerDamageEventsViewSystem>().Create(PlayerDamageEventsViewSystem.StatusEffect.Heal);
            GameContext.Get<AbilityVfxService>().Spawn(VfxSize == HealVfxSize.Big ? AbilityVfxConstants.HealBig : AbilityVfxConstants.HealSmall, unit);
            if (VfxSize == HealVfxSize.Big) GameContext.Get<IAudioPlayer>().PlaySoundOnce("heal_big");
        }
	}
}
