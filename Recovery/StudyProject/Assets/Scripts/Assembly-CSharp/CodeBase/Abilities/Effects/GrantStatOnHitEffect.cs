using System;
using Codebase.Abilities.Core;
using Codebase.Unit;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class GrantStatOnHitEffect : AbilityEffectBase
	{
		public StatType StatType;

		public ModifierType ModifierType;

		public ModifierSource Source;

		public override void Execute(AbilityContext context)
		{
		}
	}
}
