using Scellecs.Morpeh;

namespace Codebase.BossTower.Components
{
	public struct BerserkerBossComponent : IComponent
	{
		public float CurrentRage;

		public int RageLevel;

		public int MaxRageLevel;

		public float RagePerSecond;

		public float CastDuration;

		public float RageDecreaseOnPlayerLevelUp;

		public float[] RageThresholds;

		public string BossKey;

		public float BonusSpeedPerLevel;

		public float BonusAttackRangePercentPerLevel;
	}
}
