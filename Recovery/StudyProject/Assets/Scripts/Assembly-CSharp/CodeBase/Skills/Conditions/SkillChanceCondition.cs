using System;

namespace Codebase.Skills.Conditions
{
	// Rolled on every think (every SkillService.ThinkInterval seconds while the skill is otherwise ready).
	[Serializable]
	public class SkillChanceCondition : ISkillCastCondition
	{
		public float Chance = 1f;

		public bool Check(in SkillCastContext context)
		{
			return UnityEngine.Random.value < Chance;
		}
	}
}
