using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Enemy;
using UnityEngine;

namespace Codebase.Skills.Effects
{
	// Straight missiles along the locked Direction, else towards PrimaryTarget, else the caster's forward.
	// Hits use flat distance to this frame's segment <= Radius + unit radius (capped at a Line telegraph's half-width);
	// walls and Range end the flight. OnImpact runs with PrimaryTarget = the hit unit, also after the caster died.
	[Serializable]
	public class LinearMissileEffect : ISkillEffect
	{
		// SkillLibrary.Vfx key; empty or unknown = invisible but still simulated.
		public string VfxKey;

		public float Speed = 9f;

		public int Count = 1;

		// Full fan angle in degrees between the outermost missiles (Count > 1).
		public float SpreadAngle;

		public float Range = 10f;

		public float Radius = 0.4f;

		// Off = stops at the first unit hit; on = flies through, each unit hit at most once.
		public bool Pierce;

		public float Power = 1f;

		// Off: the caster's own damage type (enemy weapon type, pet item type).
		public bool OverrideDamageType;

		public WeaponType DamageType;

		public SkillTargetFilter Filter = SkillTargetFilter.Enemy | SkillTargetFilter.Alive;

		[SerializeReference]
		public List<ISkillEffect> OnImpact = new List<ISkillEffect>();

		public void Apply(in SkillCastContext context)
		{
			if (!GameContext.Has<SkillService>() || context.Caster == null)
				return;
			GameContext.Get<SkillService>().LaunchLinearMissiles(context, this);
		}
	}
}
