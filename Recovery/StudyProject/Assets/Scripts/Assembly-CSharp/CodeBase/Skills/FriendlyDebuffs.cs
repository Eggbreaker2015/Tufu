using Codebase.Core;
using Codebase.Unit;

namespace Codebase.Skills
{
	// Skill buffs and debuffs on the hero and pets, read where they act; neutral while the skill layer is not up.
	public static class FriendlyDebuffs
	{
		public static bool IsCasting(UnitCore unit)
		{
			return unit != null && GameContext.Has<SkillService>() && GameContext.Get<SkillService>().IsCasting(unit);
		}

		public static bool IsChanneling(UnitCore unit)
		{
			return unit != null && GameContext.Has<SkillService>() && GameContext.Get<SkillService>().IsChanneling(unit);
		}

		public static bool IsDisplaced(UnitCore unit)
		{
			return unit != null && GameContext.Has<SkillService>() && GameContext.Get<SkillService>().IsDisplaced(unit);
		}

		public static bool IsStunned(UnitCore unit)
		{
			return unit != null && GameContext.Has<SkillService>() && GameContext.Get<SkillService>().IsStunned(unit);
		}

		public static float AttackIntervalFactor(UnitCore unit)
		{
			return unit != null && GameContext.Has<SkillService>() ? GameContext.Get<SkillService>().GetAttackIntervalFactor(unit) : 1f;
		}

		public static float OutgoingDamageFactor(UnitCore unit)
		{
			return unit != null && GameContext.Has<SkillService>() ? GameContext.Get<SkillService>().GetOutgoingDamageFactor(unit) : 1f;
		}

		// Damage an enemy takes is divided by this (skill GlobalDefence buffs on enemies).
		public static float EnemyDefenceFactor(UnitCore unit)
		{
			return unit != null && GameContext.Has<SkillService>() ? GameContext.Get<SkillService>().GetEnemyDefenceFactor(unit) : 1f;
		}

		public static float DefenceFactor(UnitCore unit)
		{
			return unit != null && GameContext.Has<SkillService>() ? GameContext.Get<SkillService>().GetDefenceFactor(unit) : 1f;
		}
	}
}
