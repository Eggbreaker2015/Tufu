using System;
using System.Collections.Generic;
using UnityEngine;

namespace Codebase.Skills
{
	[Serializable]
	public class SkillDef
	{
		public const float DefaultLevelScaleStep = 0.08f;

		public string Key;

		// Ability icon key (Resources/AbilityIcons/<key>); empty = classified from the effects.
		public string IconKey;

		public SkillTargetType TargetType;

		public float Range;

		// Warning time; effects resolve when it ends. Threatening skills should use at least 0.6 s.
		public float CastPoint;

		// Still casting after the effects: no movement, no basic attacks.
		public float Backswing;

		// Zero preserves the instant effects -> backswing flow.
		public float ChannelDuration;

		// Non-positive: one tick at the end; positive intervals are at least 0.05 s.
		public float ChannelTickInterval;

		[SerializeReference]
		public List<ISkillEffect> OnChannelTick = new List<ISkillEffect>();

		public float Cooldown;

		public float InitialCooldown;

		public string AnimationBool;

		public string AnimationTrigger;

		public TelegraphDef Telegraph = new TelegraphDef();

		public SkillAutoCast AutoCast = new SkillAutoCast();

		// Power multiplier per level (index 0 = level 1); missing entries use the last one, empty means the default +8% per level.
		public float[] LevelScale = new float[0];

		[SerializeReference]
		public List<ISkillEffect> Effects = new List<ISkillEffect>();

		public float GetLevelScale(int level)
		{
			if (LevelScale == null || LevelScale.Length == 0)
				return 1f + DefaultLevelScaleStep * (Mathf.Max(1, level) - 1);
			int index = Mathf.Clamp(level - 1, 0, LevelScale.Length - 1);
			return LevelScale[index];
		}
	}
}
