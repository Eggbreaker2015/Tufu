using System;
using Codebase.Amulet;
using Codebase.Economy;
using Codebase.In_apps;
using Codebase.Rewards;
using Codebase.Unit;
using Codebase.Utilities;
using UnityEngine;

namespace Codebase.UI.Shop
{
	[Serializable]
	public class ShopLotDef
	{
		public string Key;

		public ShopLotType LotType;

		public bool IsFree;

		public bool CanClaimOnFirstDay;

		public StatType StatType;

		[KeysDropdown(typeof(ChestKeys))]
		public string ChestKey;

		[KeysDropdown(typeof(CurrencyKeys))]
		public string CurrencyKey;

		[KeysDropdown(typeof(SoulKeys))]
		public string SoulKey;

		public double FreeValue;

		public double Value;

		public int RequiredLocationNumber;

		public double Cost;

		public ProductKey ProductKey;

		public Sprite Sprite;

		public ShopLotIconType IconType;
	}
}
