using System;
using System.Collections.Generic;
using Codebase.Unit;

namespace Codebase.Rewards
{
	[Serializable]
	public class StatReward : RewardBase
	{
		public bool IsRandom;

		public bool UseGpm;

		public bool CustomModifier;

		public bool DailyRotation;

		public List<StatType> AllowedStats;

		public List<DailyRotationStat> DailyRotationStats;

		public string SaveKey;

		public StatType Type;

		public float Value;

		public string Name;

		public ModifierSource Source;

		public ModifierType ModifierType;

		[NonSerialized]
		public BakedStatReward BakedData;

		public StatType? GetResolvedType()
		{
			return null;
		}
	}
}
