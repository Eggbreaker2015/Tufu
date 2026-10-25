using System;
using Codebase.Player.Inventory;
using UnityEngine;

namespace Codebase.Balance
{
	[Serializable]
	public class EvolutionIslandFillRuleRow
	{
		[HideInInspector]
		public Rarity Rarity;

		public string Name;

		public float StartValue;

		public float StepPerEvolution;

		private Color RowColor => default;
	}
}
