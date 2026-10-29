using System;
using Codebase.Abilities.Core;
using Codebase.Core;
using Codebase.Player.Inventory;
using Codebase.Skills;
using Codebase.Unit;

namespace Codebase.Abilities.Effects
{
	// Item ability -> skill layer: casts SkillKey instantly from the attacker (pet) or the hero at the resolved target.
	// The ability Value is the skill's power multiplier (0 = 1); LevelScaling grows it with stars.
	// Skill damage raises no OnHit, so an OnHit + cooldown ability cannot recurse.
	[Serializable]
	public class CastSkillEffect : AbilityEffectBase
	{
		public string SkillKey;

		public int Level = 1;

		public override void Execute(AbilityContext context)
		{
			if (!GameContext.Has<SkillService>())
				return;
			SkillDef def = SkillLibrary.FindSkill(SkillKey);
			UnitCore caster = context.Attacker != null ? context.Attacker : context.PlayerUnit;
			if (def == null || caster == null)
				return;
			float value = context.State != null ? context.State.GetValue() : 0f;
			ItemModel item = context.SourceItem ?? context.Owner as ItemModel;
			GameContext.Get<SkillService>().CastInstant(caster, def, Level, value > 0f ? value : 1f, AbilityTargetResolver.Resolve(context), item);
		}
	}
}
