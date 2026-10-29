using System;
using System.Collections.Generic;
using Codebase.Core;
using UnityEngine;

namespace Codebase.Skills.Effects
{
	[Serializable]
	public class DashEffect : ISkillEffect
	{
		public float Distance = 6f;

		public float Speed = 18f;

		public bool StopAtFirstHit;

		public float Width = 1.5f;

		public float Power = 1f;

		public bool PassThrough = true;

		public SkillTargetFilter Filter = SkillTargetFilter.Enemy | SkillTargetFilter.Alive;

		[SerializeReference]
		public List<ISkillEffect> OnHit = new List<ISkillEffect>();

		public void Apply(in SkillCastContext context)
		{
			if (GameContext.Has<SkillService>())
				GameContext.Get<SkillService>().Dash(context, this);
		}
	}
}
