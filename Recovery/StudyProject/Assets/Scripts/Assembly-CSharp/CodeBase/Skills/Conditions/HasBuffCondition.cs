using System;
using Codebase.Core;

namespace Codebase.Skills.Conditions
{
	// The caster (or the picked target with OnTarget) has buff Key; Invert = does not have it.
	[Serializable]
	public class HasBuffCondition : ISkillCastCondition
	{
		public string Key;

		public bool Invert;

		public bool OnTarget;

		public bool Check(in SkillCastContext context)
		{
			if (!GameContext.Has<SkillService>())
				return Invert;
			bool has = GameContext.Get<SkillService>().HasBuff(OnTarget ? context.PrimaryTarget : context.Caster, Key);
			return has != Invert;
		}
	}
}
