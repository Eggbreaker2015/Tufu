using System;
using System.Collections.Generic;
using UnityEngine;

namespace Codebase.UI.Shop
{
	[Serializable]
	public class ShopSectionDef
	{
		public string Title;

		public Color HeaderBackgroundColor;

		public int Columns;

		public List<ShopPassBannerType> PassBanners;

		public bool HasExtraHeader;

		public bool HasExtraInfo;

		public bool HasOfferWallSection;

		public string ExtraTitle;

		public Color ExtraHeaderBackgroundColor;

		public List<ShopLotDef> Lots;
	}
}
