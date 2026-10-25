using System.Collections.Generic;
using Codebase.Player.Inventory;
using UnityEngine;

namespace Codebase.UI.Stats
{
	public class StatSourceData
	{
		public StatSourceKind Kind;

		public List<StatSourceRow> Rows;

		public double Total;

		public bool IsPercent;

		public bool IsPostFormula;

		public string ParamLabel;

		public Rarity? CategoryRarity;

		public Sprite CategoryIcon;

		public string RowNameKey;
	}
}
