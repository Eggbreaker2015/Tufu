using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Skills.Effects
{
	// Tracking missiles (WorldVfxService.SpawnTracking); Count missiles go round-robin over the Area targets.
	// OnImpact runs with PrimaryTarget = the hit unit. An empty or unknown VfxKey still resolves after the travel time.
	[Serializable]
	public class MissileEffect : ISkillEffect
	{
		public string VfxKey;

		public float Speed = 12f;

		public int Count = 1;

		// Sideways spacing of the launch points.
		public float Spread = 0.4f;

		public TargetSelector Area = new TargetSelector { Mode = TargetSelectMode.Primary };

		[SerializeReference]
		public List<ISkillEffect> OnImpact = new List<ISkillEffect>();

		public void Apply(in SkillCastContext context)
		{
			if (!GameContext.Has<SkillService>() || context.Caster == null)
				return;
			SkillService service = GameContext.Get<SkillService>();
			List<UnitCore> targets = SkillTargetQuery.RentList();
			Area.Collect(context, targets);
			int count = Mathf.Max(1, Count);
			if (targets.Count > 0)
			{
				Vector3 origin = context.Caster.transform.position;
				for (int i = 0; i < count; i++)
				{
					UnitCore target = targets[i % targets.Count];
					Vector3 direction = target.transform.position - origin;
					direction.y = 0f;
					Vector3 side = direction.sqrMagnitude > 0.0001f ? Vector3.Cross(Vector3.up, direction.normalized) : Vector3.right;
					Vector3 offset = side * ((i - (count - 1) * 0.5f) * Spread);
					service.LaunchMissile(context, target, VfxKey, Speed, offset, OnImpact);
				}
			}
			SkillTargetQuery.ReturnList(targets);
		}
	}
}
