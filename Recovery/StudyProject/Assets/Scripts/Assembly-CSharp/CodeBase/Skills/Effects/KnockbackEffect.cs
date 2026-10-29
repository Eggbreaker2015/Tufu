using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Unit;

namespace Codebase.Skills.Effects
{
	[Serializable]
	public class KnockbackEffect : ISkillEffect
	{
		public TargetSelector Area = new TargetSelector();

		public float Distance = 2f;

		public float Duration = 0.25f;

		public bool FromPoint;

		public void Apply(in SkillCastContext context)
		{
			if (!GameContext.Has<SkillService>() || context.Caster == null || Area == null)
				return;
			List<UnitCore> targets = SkillTargetQuery.RentList();
			try
			{
				Area.Collect(context, targets);
				foreach (UnitCore target in targets)
					GameContext.Get<SkillService>().Displace(context, target, Distance, Duration, false, FromPoint);
			}
			finally
			{
				SkillTargetQuery.ReturnList(targets);
			}
		}
	}
}
