using System;
using System.Collections.Generic;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Skills
{
	[Serializable]
	public class TargetSelector
	{
		public TargetSelectMode Mode;

		public float Radius;

		// Cone only: full angle in degrees, facing the primary target (or the caster's forward).
		public float Angle = 90f;

		public SkillTargetFilter Filter = SkillTargetFilter.Enemy | SkillTargetFilter.Alive;

		// 0 = unlimited; the nearest to the center are kept.
		public int MaxTargets;

		public void Collect(in SkillCastContext context, List<UnitCore> result)
		{
			result.Clear();
			UnitCore caster = context.Caster;
			if (caster == null)
				return;
			switch (Mode)
			{
				case TargetSelectMode.Self:
					result.Add(caster);
					return;
				case TargetSelectMode.Primary:
					if (context.PrimaryTarget != null && SkillTargetQuery.Matches(caster, context.PrimaryTarget, Filter | SkillTargetFilter.Self))
						result.Add(context.PrimaryTarget);
					return;
				case TargetSelectMode.CircleCaster:
					SkillTargetQuery.GatherInRadius(caster, caster.transform.position, Radius, Filter, result);
					break;
				case TargetSelectMode.CirclePoint:
					SkillTargetQuery.GatherInRadius(caster, context.Point, Radius, Filter, result);
					break;
				case TargetSelectMode.Cone:
					GatherCone(context, result);
					break;
			}
			if (MaxTargets > 0 && result.Count > MaxTargets)
			{
				Vector3 center = Mode == TargetSelectMode.CirclePoint ? context.Point : caster.transform.position;
				SkillTargetQuery.SortByDistance(result, center);
				result.RemoveRange(MaxTargets, result.Count - MaxTargets);
			}
		}

		private void GatherCone(in SkillCastContext context, List<UnitCore> result)
		{
			UnitCore caster = context.Caster;
			Vector3 origin = caster.transform.position;
			SkillTargetQuery.GatherInRadius(caster, origin, Radius, Filter, result);
			Vector3 forward = context.PrimaryTarget != null && context.PrimaryTarget != caster ? context.PrimaryTarget.transform.position - origin : caster.transform.forward;
			forward.y = 0f;
			if (forward.sqrMagnitude < 0.0001f)
				return;
			float halfAngle = Angle * 0.5f;
			for (int i = result.Count - 1; i >= 0; i--)
			{
				Vector3 direction = result[i].transform.position - origin;
				direction.y = 0f;
				if (direction.sqrMagnitude > 0.0001f && Vector3.Angle(forward, direction) > halfAngle)
					result.RemoveAt(i);
			}
		}
	}
}
