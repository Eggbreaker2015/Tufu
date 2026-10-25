using System;
using Codebase.Enemy;
using Codebase.Player.Inventory;

namespace Codebase.Balance
{
	[Serializable]
	public struct EvolutionCombatValue
	{
		public WeaponType DamageType;

		public Rarity Rarity;

		public int EvolutionLevel;

		public float Value;

		public float CopyValue;

		public int ReferenceShardsRequirement;

		public int ReferenceStatPercent;

		public int ReferenceTimeChunkIndex;

		public float ReferenceStatValue;

		public float ReferenceHeuristicValue;
	}
}
