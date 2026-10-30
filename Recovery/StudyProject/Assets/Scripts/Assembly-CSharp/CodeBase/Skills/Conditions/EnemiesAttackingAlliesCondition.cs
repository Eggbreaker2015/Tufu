using System;
using Codebase.Enemy;
using Codebase.Unit;

namespace Codebase.Skills.Conditions
{
	[Serializable]
	public class EnemiesAttackingAlliesCondition : ISkillCastCondition
	{
		public float Radius = 6f;
		public int MinCount = 1;

		public bool Check(in SkillCastContext context)
		{
			if (context.Caster == null) return false;
			int count = 0;
			foreach (UnitCore unit in SkillTargetQuery.GetCandidates(context.Caster))
				if (unit is EnemyUnit enemy && SkillTargetQuery.IsAlive(enemy) && enemy.AggroTarget != null && enemy.AggroTarget != context.Caster &&
					SkillTargetQuery.FlatSqrDistance(enemy.transform.position, context.Caster.transform.position) <= Radius * Radius && ++count >= MinCount)
					return true;
			return MinCount <= 0;
		}
	}
}
