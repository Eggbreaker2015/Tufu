using System;

namespace Codebase.BossTower
{
	[Serializable]
	public class BerserkerBossConfig : BossBehaviorConfig
	{
		public float RagePerSecond = 10f;
		public float CastDuration = 1.5f;
		public float RageDecreaseOnPlayerLevelUp = 0.2f;
		public float[] RageThresholds = new float[]
		{
			40f,
			120f,
			250f,
			400f
		};
		public float BonusSpeedPerLevel;
		public float BonusAttackRangePercentPerLevel;
	}
}
