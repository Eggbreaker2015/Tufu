using System;
using System.Collections.Generic;
using Codebase.Unit;

namespace Codebase.Skills.Conditions
{
	// At least Min living allies (the caster too when IncludeSelf) within Radius below Percent health.
	[Serializable]
	public class AlliesHurtInRadiusCondition : ISkillCastCondition
	{
		public float Radius = 5f;

		public float Percent = 0.7f;

		public int Min = 1;

		public bool IncludeSelf = true;

		public bool Check(in SkillCastContext context)
		{
			if (context.Caster == null)
				return false;
			SkillTargetFilter filter = SkillTargetFilter.Ally | SkillTargetFilter.Alive | (IncludeSelf ? SkillTargetFilter.Self : SkillTargetFilter.None);
			List<UnitCore> allies = SkillTargetQuery.RentList();
			SkillTargetQuery.GatherInRadius(context.Caster, context.Caster.transform.position, Radius, filter, allies);
			int count = 0;
			foreach (UnitCore ally in allies)
				if (SkillTargetQuery.GetHealthFraction(ally) < Percent)
					count++;
			SkillTargetQuery.ReturnList(allies);
			return count >= Min;
		}
	}
}
