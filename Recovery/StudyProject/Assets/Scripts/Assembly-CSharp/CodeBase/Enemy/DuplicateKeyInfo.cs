using System;
using System.Collections.Generic;

namespace Codebase.Enemy
{
	[Serializable]
	public class DuplicateKeyInfo
	{
		public string Key;

		public List<EnemySpawner> Spawners;
	}
}
