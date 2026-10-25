using System;

namespace Codebase.BossTower
{
	[Serializable]
	public class AssassinBossConfig : BossBehaviorConfig
	{
		public float IllusionInterval;

		public int IllusionCount;

		public float DamageMultiplier;

		public float IllusionSpawnRadius;

		public float CorrectIllusionScale;
	}
}
