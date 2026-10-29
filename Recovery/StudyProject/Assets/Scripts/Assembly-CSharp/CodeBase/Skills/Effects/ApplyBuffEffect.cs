using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Unit;

namespace Codebase.Skills.Effects
{
	[Serializable]
	public class ApplyBuffEffect : ISkillEffect
	{
		public TargetSelector Area = new TargetSelector();

		public string BuffKey;

		public void Apply(in SkillCastContext context)
		{
			if (!GameContext.Has<SkillService>())
				return;
			BuffDef def = SkillLibrary.FindBuff(BuffKey);
			if (def == null)
				return;
			SkillService service = GameContext.Get<SkillService>();
			List<UnitCore> targets = SkillTargetQuery.RentList();
			Area.Collect(context, targets);
			foreach (UnitCore target in targets)
				service.ApplyBuff(target, def, context.Source, context.Power);
			SkillTargetQuery.ReturnList(targets);
		}
	}
}
