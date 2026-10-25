using System.Collections.Generic;
using UnityEngine;

namespace Codebase.UI.Shop
{
	public readonly struct ChestChancesData
	{
		public string ChestKey { get; }

		public Sprite Icon { get; }

		public string Name { get; }

		public IReadOnlyList<ChestRarityChanceData> Rarities { get; }

		public ChestChancesData(string chestKey, Sprite icon, string name, IReadOnlyList<ChestRarityChanceData> rarities)
		{
			ChestKey = chestKey;
			Icon = icon;
			Name = name;
			Rarities = rarities;
		}
	}
}
