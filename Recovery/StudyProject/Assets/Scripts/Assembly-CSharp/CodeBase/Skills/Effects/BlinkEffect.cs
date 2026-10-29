using System;
using Codebase.Core;

namespace Codebase.Skills.Effects
{
	[Serializable]
	public class BlinkEffect : ISkillEffect
	{
		public BlinkMode Mode = BlinkMode.BehindTarget;

		public float Distance = 2f;

		public void Apply(in SkillCastContext context)
		{
			if (GameContext.Has<SkillService>())
				GameContext.Get<SkillService>().Blink(context, this);
		}
	}
}
