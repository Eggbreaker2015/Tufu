using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Codebase.Rewards
{
	public class ChestsConfig : ScriptableObject
	{
		public List<ChestDef> Chests = new List<ChestDef>();

		public ChestDef Get(string key)
		{
			// Native ELF VA 0x1417f9c.
			return Chests.FirstOrDefault(chest => chest.Key == key);
		}

		public bool TryGet(string key, out ChestDef chest)
		{
			// Native ELF VA 0x1418098.
			chest = Get(key);
			return chest != null;
		}
	}
}
