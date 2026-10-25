using System;
using Codebase.Player.Inventory;
using UnityEngine;

namespace Codebase.Balance
{
	[Serializable]
	public class EvolutionIslandTimingRow
	{
		private static readonly Func<EvolutionIslandTimingRow, float>[] EvolutionGetters;

		private static readonly Action<EvolutionIslandTimingRow, float>[] EvolutionSetters;

		[HideInInspector]
		public Rarity Rarity;

		public string Name;

		public float E1;

		public float E2;

		public float E3;

		public float E4;

		public float E5;

		public float E6;

		public float E7;

		public float E8;

		public float E9;

		public float E10;

		public float E11;

		public float E12;

		public float E13;

		public float E14;

		public float E15;

		private Color RowColor => default;

		public float GetEvolutionIsland(int evolutionLevel)
		{
			return 0f;
		}

		public void SetEvolutionIsland(int evolutionLevel, float value)
		{
		}
	}
}
