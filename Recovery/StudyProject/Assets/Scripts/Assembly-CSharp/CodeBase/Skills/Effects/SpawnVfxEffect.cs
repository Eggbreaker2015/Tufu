using System;
using Codebase.Core;
using UnityEngine;

namespace Codebase.Skills.Effects
{
	public enum SkillVfxAnchor
	{
		Caster = 0,
		PrimaryTarget = 1,
		Point = 2
	}

	[Serializable]
	public class SpawnVfxEffect : ISkillEffect
	{
		public string VfxKey;

		public SkillVfxAnchor Anchor;

		public float Scale = 1f;

		public float HeightOffset;

		public void Apply(in SkillCastContext context)
		{
			if (!GameContext.Has<SkillService>())
				return;
			Vector3 position;
			switch (Anchor)
			{
				case SkillVfxAnchor.PrimaryTarget:
					if (context.PrimaryTarget == null)
						return;
					position = context.PrimaryTarget.transform.position;
					break;
				case SkillVfxAnchor.Point:
					position = context.Point;
					break;
				default:
					if (context.Caster == null)
						return;
					position = context.Caster.transform.position;
					break;
			}
			GameContext.Get<SkillService>().SpawnVfx(VfxKey, position + Vector3.up * HeightOffset, Quaternion.identity, Scale);
		}
	}
}
