using Codebase.Abilities.Vfx;
using Codebase.UI.Damage;
using Codebase.Core;
using Codebase.Unit;
using System;
using Codebase.Abilities.Core;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class TriggerLifestealEffect : AbilityEffectBase
	{
		public override void Execute(AbilityContext context)
		{
            // Native 0x15f3694, distinct from UnitDamageSystem's intentionally uncapped general lifesteal helper.
            var unit = context.PlayerModel.Unit;
            if (unit.IsDead) return;
            double maximum = unit.GetStatValue(StatType.Health);
            double amount = unit.GetStatValue(StatType.LifestealFlat) + maximum * unit.GetStatValue(StatType.LifestealPercent);
            if (amount <= 0.0) return;
            unit.CurrentHealth = Math.Min(maximum, unit.CurrentHealth + amount * context.State.GetValue());
            GameContext.Get<PlayerDamageEventsViewSystem>().Create(PlayerDamageEventsViewSystem.StatusEffect.Heal);
            GameContext.Get<AbilityVfxService>().Spawn(AbilityVfxConstants.HealSmall, unit);
        }
	}
}
