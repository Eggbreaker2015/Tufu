using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Unit;

namespace Codebase.Skills.Effects
{
	// Heals PercentMaxHp of each target's own max health (x context power).
	[Serializable]
	public class HealEffect : ISkillEffect
	{
		public TargetSelector Area = new TargetSelector { Mode = TargetSelectMode.Self, Filter = SkillTargetFilter.Self | SkillTargetFilter.Alive };

		public float PercentMaxHp;

		public void Apply(in SkillCastContext context)
		{
			if (!GameContext.Has<SkillService>() || PercentMaxHp <= 0f)
				return;
			SkillService service = GameContext.Get<SkillService>();
			List<UnitCore> targets = SkillTargetQuery.RentList();
			Area.Collect(context, targets);
			foreach (UnitCore target in targets)
				if (target.HasStat(StatType.Health))
					service.Heal(target, target.GetStatValue(StatType.Health) * PercentMaxHp * context.Power);
			SkillTargetQuery.ReturnList(targets);
		}
	}
}
