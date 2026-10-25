using System;
using Codebase.Abilities.Core;
using Codebase.Unit;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class AddWeaponSuperCritChanceEffect : AbilityEffectBase
	{
		public AbilityValueScaling ValueScaling;

		public StatType ScalingStat;

		public override bool HasPassiveTick => true;

		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
            // Original ELF VA 0x15ed5e4; owner must be the actual ItemModel, with no positivity clamp.
            if (context.Owner is Codebase.Player.Inventory.ItemModel weapon)
                weapon.AbilitySuperCritChanceBonus = ValueScaler.GetScaledValue(context, ValueScaling, ScalingStat) + weapon.AbilitySuperCritChanceBonus;
        }
	}
}
