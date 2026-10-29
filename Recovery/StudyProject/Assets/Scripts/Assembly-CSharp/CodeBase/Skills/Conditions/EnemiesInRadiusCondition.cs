using System;

namespace Codebase.Skills.Conditions
{
	// At least Min living hostile units within Radius of the caster.
	[Serializable]
	public class EnemiesInRadiusCondition : ISkillCastCondition
	{
		public float Radius = 3f;

		public int Min = 1;

		public bool Check(in SkillCastContext context)
		{
			if (context.Caster == null)
				return false;
			return SkillTargetQuery.CountInRadius(context.Caster, context.Caster.transform.position, Radius, SkillTargetFilter.Enemy | SkillTargetFilter.Alive) >= Min;
		}
	}
}
