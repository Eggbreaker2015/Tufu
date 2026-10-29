using System;
using Codebase.Abilities.Core;
using Codebase.Core;
using Codebase.Skills;
using Codebase.Unit;

namespace Codebase.Abilities.Effects
{
	// Item ability -> skill buff on the resolved enemy target (OnTarget) or on the attacker/hero.
	// The ability Value scales the buff's periodic effects (0 = 1).
	[Serializable]
	public class ApplySkillBuffEffect : AbilityEffectBase
	{
		public string BuffKey;

		public bool OnTarget = true;

		public override void Execute(AbilityContext context)
		{
			if (!GameContext.Has<SkillService>())
				return;
			BuffDef def = SkillLibrary.FindBuff(BuffKey);
			UnitCore caster = context.Attacker != null ? context.Attacker : context.PlayerUnit;
			if (def == null || caster == null)
				return;
			UnitCore target = OnTarget ? AbilityTargetResolver.Resolve(context) : caster;
			if (target == null)
				return;
			float value = context.State != null ? context.State.GetValue() : 0f;
			GameContext.Get<SkillService>().ApplyBuff(target, def, caster, value > 0f ? value : 1f);
		}
	}
}
