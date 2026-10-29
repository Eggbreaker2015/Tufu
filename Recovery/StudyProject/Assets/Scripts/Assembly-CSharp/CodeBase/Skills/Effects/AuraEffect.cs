using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Unit;

namespace Codebase.Skills.Effects
{
	// Passive skills: SkillAutoCastSystem pulses this every Interval; give the buff a Duration longer than Interval
	// so it lapses shortly after a unit leaves the radius.
	[Serializable]
	public class AuraEffect : ISkillEffect
	{
		public float Radius = 5f;

		public SkillTargetFilter Filter = SkillTargetFilter.Ally | SkillTargetFilter.Alive;

		public string BuffKey;

		public float Interval = 0.5f;

		public void Apply(in SkillCastContext context)
		{
			if (!GameContext.Has<SkillService>() || context.Caster == null)
				return;
			BuffDef def = SkillLibrary.FindBuff(BuffKey);
			if (def == null)
				return;
			SkillService service = GameContext.Get<SkillService>();
			List<UnitCore> targets = SkillTargetQuery.RentList();
			SkillTargetQuery.GatherInRadius(context.Caster, context.Caster.transform.position, Radius, Filter, targets);
			foreach (UnitCore target in targets)
				service.ApplyBuff(target, def, context.Source, context.Power);
			SkillTargetQuery.ReturnList(targets);
		}
	}
}
