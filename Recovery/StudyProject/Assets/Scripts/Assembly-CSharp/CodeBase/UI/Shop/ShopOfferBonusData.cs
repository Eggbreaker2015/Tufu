using UnityEngine;

namespace Codebase.UI.Shop
{
	public readonly struct ShopOfferBonusData
	{
		public ShopOfferBonusType Type { get; }

		public Sprite Icon { get; }

		public string Text { get; }

		public bool ShowSoulsShowcase { get; }

		public ShopOfferBonusData(Sprite icon, string text, bool showSoulsShowcase = false)
		{
            // Native 0x1629ae0.
            Type = ShopOfferBonusType.Reward;
            Icon = icon; Text = text; ShowSoulsShowcase = showSoulsShowcase;
		}

		private ShopOfferBonusData(string text)
		{
            // Native 0x1629b20.
            Type = ShopOfferBonusType.Description;
            Icon = null; Text = text; ShowSoulsShowcase = false;
		}

		public static ShopOfferBonusData Description(string text)
		{
            // Native 0x1629b64.
            return new ShopOfferBonusData(text);
		}
	}
}
