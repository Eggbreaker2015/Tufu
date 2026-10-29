using System;

namespace Codebase.Skills.Conditions
{
	// Flat distance from the caster to the picked target within [Min, Max]; Max 0 = no upper bound.
	[Serializable]
	public class TargetDistanceCondition : ISkillCastCondition
	{
		public float Min;

		public float Max;

		public bool Check(in SkillCastContext context)
		{
			if (context.Caster == null || context.PrimaryTarget == null)
				return false;
			float distance = SkillTargetQuery.FlatDistance(context.Caster, context.PrimaryTarget);
			return distance >= Min && (Max <= 0f || distance <= Max);
		}
	}
}
