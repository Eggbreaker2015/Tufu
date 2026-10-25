using System;
using System.Collections.Generic;

namespace Codebase.Enemy
{
	[Serializable]
	public struct EnemySpawnerDebugInfo
	{
		public int Location;

		public List<EnemySpawnerDebugInfoStat> Stats;
	}
}
