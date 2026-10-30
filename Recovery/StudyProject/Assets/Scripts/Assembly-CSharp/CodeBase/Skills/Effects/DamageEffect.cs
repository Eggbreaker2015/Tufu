using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Unit;

namespace Codebase.Skills.Effects
{
	// Power x the caster's attack (x context power). Raises no OnHit-style triggers.
	[Serializable]
	public class DamageEffect : ISkillEffect
	{
		public TargetSelector Area = new TargetSelector();

		public float Power = 1f;

		// Off: the caster's own damage type (enemy weapon type, pet item type).
		public bool OverrideDamageType;

		public WeaponType DamageType;

		public bool Undodgeable;

		public void Apply(in SkillCastContext context)
		{
			if (!GameContext.Has<SkillService>())
				return;
			SkillService service = GameContext.Get<SkillService>();
			List<UnitCore> targets = SkillTargetQuery.RentList();
			if (global::Codebase.Rpg.RpgModeConfig.Enabled) SkillTargetQuery.RpgHitStatic = true;
			Area.Collect(context, targets);
			if (global::Codebase.Rpg.RpgModeConfig.Enabled) SkillTargetQuery.RpgHitStatic = false;
			float power = Power * context.Power;
			WeaponType? type = OverrideDamageType ? DamageType : (WeaponType?)null;
			foreach (UnitCore target in targets)
				service.DealDamage(context, target, power, type, Undodgeable);
			SkillTargetQuery.ReturnList(targets);
		}
	}
}
