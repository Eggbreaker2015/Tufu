namespace Codebase.UI.Shop
{
	public readonly struct ShopTabDef
	{
		public ShopTab Tab { get; }

		public string LocalizationKey { get; }

		public ShopTabDef(ShopTab tab, string localizationKey)
		{
            // Native 0x1629c90.
            Tab = tab;
            LocalizationKey = localizationKey;
		}
	}
}
