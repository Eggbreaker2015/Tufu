using System;

namespace Codebase.Skills.Conditions
{
	// Caster health fraction below Percent (0.5 = 50%).
	[Serializable]
	public class SelfHealthBelowCondition : ISkillCastCondition
	{
		public float Percent = 0.5f;

		public bool Check(in SkillCastContext context)
		{
			return context.Caster != null && SkillTargetQuery.GetHealthFraction(context.Caster) < Percent;
		}
	}
}
