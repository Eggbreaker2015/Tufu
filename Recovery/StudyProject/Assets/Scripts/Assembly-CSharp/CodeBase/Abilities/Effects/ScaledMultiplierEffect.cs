using System;
using Codebase.Abilities.Core;
using Codebase.Enemy;
using Codebase.Unit;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class ScaledMultiplierEffect : AbilityEffectBase
	{
		public MultiplierTarget Target;

		public AbilityValueScaling ValueScaling;

		public StatType ScalingStat;

		public bool FilterByWeaponType;

		public WeaponType WeaponType;

		private bool HasWeaponTypeFilter => false;

		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
		}
	}
}
