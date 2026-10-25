using TMPro;
using UnityEngine;

namespace Codebase.Localization
{
	public static class LocalizationFontService
	{
		private static readonly string[] CjkLocales = { "ja", "ko", "zh" };

		public static void ApplyForLocale(string locale)
		{
			var settings = Resources.Load<LocalizationFontSettings>(LocalizationFontSettings.ResourcesPath);
			if (settings == null)
			{
				Debug.LogWarning("LocalizationFontSettings not found at Resources/Localization/Localization Font Settings");
				return;
			}
			var fallback = ResolveFallbackFont(locale, settings);
			var fonts = settings.BaseFontAssets;
			if (fonts == null || fonts.Length == 0) return;
			foreach (var font in fonts)
			{
				if (font == null) continue;
				font.fallbackFontAssetTable.Clear();
				if (fallback != null) font.fallbackFontAssetTable.Add(fallback);
			}
			TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, null);
		}

		private static TMP_FontAsset ResolveFallbackFont(string locale, LocalizationFontSettings settings)
		{
			if (locale == "uk") return settings.GetLocaleFont("ru");
			return IsCjkLocale(locale) ? settings.GetLocaleFont(locale) : settings.LatinFallbackFont;
		}

		private static bool IsCjkLocale(string locale)
		{
			foreach (var candidate in CjkLocales) if (candidate == locale) return true;
			return false;
		}
	}
}
