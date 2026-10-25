using System;
using Codebase.Abilities.Core;
using Codebase.Unit;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class AddWeaponSuperCritDamageEffect : AbilityEffectBase
	{
		public AbilityValueScaling ValueScaling;

		public StatType ScalingStat;

		public override bool HasPassiveTick => true;

		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
            // Original ELF VA 0x15ed6b8; owner must be the actual ItemModel, with no positivity clamp.
            if (context.Owner is Codebase.Player.Inventory.ItemModel weapon)
                weapon.AbilitySuperCritDamageBonus = ValueScaler.GetScaledValue(context, ValueScaling, ScalingStat) + weapon.AbilitySuperCritDamageBonus;
        }
	}
}
