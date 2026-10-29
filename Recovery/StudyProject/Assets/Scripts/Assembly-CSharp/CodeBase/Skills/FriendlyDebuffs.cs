using Codebase.Core;
using Codebase.Unit;

namespace Codebase.Skills
{
	// Enemy debuffs on the hero and pets, read where they act (movement, attack, defence); neutral while the skill layer is not up.
	public static class FriendlyDebuffs
	{
		public static bool IsStunned(UnitCore unit)
		{
			return unit != null && GameContext.Has<SkillService>() && GameContext.Get<SkillService>().IsStunned(unit);
		}

		public static float AttackIntervalFactor(UnitCore unit)
		{
			return unit != null && GameContext.Has<SkillService>() ? GameContext.Get<SkillService>().GetAttackIntervalFactor(unit) : 1f;
		}

		public static float DefenceFactor(UnitCore unit)
		{
			return unit != null && GameContext.Has<SkillService>() ? GameContext.Get<SkillService>().GetDefenceFactor(unit) : 1f;
		}
	}
}
