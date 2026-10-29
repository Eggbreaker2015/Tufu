using System;
using Codebase.Core;

namespace Codebase.Skills.Conditions
{
	// The caster has fewer than Count living summons of SummonKey (SkillLibrary.Summons key).
	[Serializable]
	public class SummonsBelowCondition : ISkillCastCondition
	{
		public string SummonKey;

		public int Count = 1;

		public bool Check(in SkillCastContext context)
		{
			if (context.Caster == null)
				return false;
			if (!GameContext.Has<SkillService>())
				return true;
			return GameContext.Get<SkillService>().CountSummons(context.Caster, SummonKey) < Count;
		}
	}
}
