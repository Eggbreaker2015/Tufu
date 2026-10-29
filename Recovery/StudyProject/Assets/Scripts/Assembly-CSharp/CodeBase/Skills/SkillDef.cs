using System;
using System.Collections.Generic;
using UnityEngine;

namespace Codebase.Skills
{
	[Serializable]
	public class SkillDef
	{
		public string Key;

		public SkillTargetType TargetType;

		public float Range;

		// Warning time; effects resolve when it ends. Threatening skills should use at least 0.6 s.
		public float CastPoint;

		// Still casting after the effects: no movement, no basic attacks.
		public float Backswing;

		public float Cooldown;

		public float InitialCooldown;

		public string AnimationBool;

		public string AnimationTrigger;

		public TelegraphDef Telegraph = new TelegraphDef();

		public SkillAutoCast AutoCast = new SkillAutoCast();

		// Power multiplier per level (index 0 = level 1); missing entries use the last one, empty means 1.
		public float[] LevelScale = new float[0];

		[SerializeReference]
		public List<ISkillEffect> Effects = new List<ISkillEffect>();

		public float GetLevelScale(int level)
		{
			if (LevelScale == null || LevelScale.Length == 0)
				return 1f;
			int index = Mathf.Clamp(level - 1, 0, LevelScale.Length - 1);
			return LevelScale[index];
		}
	}
}
