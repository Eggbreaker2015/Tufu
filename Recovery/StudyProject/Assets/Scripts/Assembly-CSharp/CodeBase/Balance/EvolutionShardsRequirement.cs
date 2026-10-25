using System;
using Codebase.Player.Inventory;

namespace Codebase.Balance
{
	[Serializable]
	public struct EvolutionShardsRequirement
	{
		public Rarity Rarity;

		public int EvolutionLevel;

		public int Value;
	}
}
