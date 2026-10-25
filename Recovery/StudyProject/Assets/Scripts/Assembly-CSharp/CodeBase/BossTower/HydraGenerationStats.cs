using System;
using System.Collections.Generic;

namespace Codebase.BossTower
{
	[Serializable]
	public class HydraGenerationStats
	{
		public List<MinionStat> Stats;
        // Native ELF VA 0x1550030.
        public HydraGenerationStats() { Stats = new List<MinionStat>(); }

	}
}
