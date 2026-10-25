using System;

namespace Codebase.BossTower
{
	[Serializable]
	public class MageBossConfig : BossBehaviorConfig
	{
		public int HitsToTeleport;

		public float RetreatDistance;

		public float RetreatDuration;

		public MageBossConfig()
		{
			// Original ELF VA 0x1550598.
			HitsToTeleport = 8;
			RetreatDistance = 5f;
			RetreatDuration = .3f;
		}
	}
}
