namespace Codebase.Utilities
{
	public static class LocalizationStrings
	{
		public static string Get(string key)
		{
			return Codebase.Localization.LocalizationService.Get(key);
		}

		public static string Get(string key, string locale)
		{
			return Codebase.Localization.LocalizationService.Get(key, locale);
		}

		public static string GetPlural(string key, int value)
		{
			return Get(key + "_" + ResolvePluralForm(value)).Replace("{value}", value.ToString());
		}

		private static string ResolvePluralForm(int value)
		{
			long absoluteValue = System.Math.Abs((long)value);
			switch (Codebase.Localization.LocalizationService.CurrentLocale)
			{
				case "ru": case "uk": return ResolveSlavicPluralForm(absoluteValue);
				case "pl": return ResolvePolishPluralForm(absoluteValue);
				case "fr": case "pt-BR": return absoluteValue <= 1 ? "ONE" : "OTHER";
				case "en": case "de": case "es": case "it": return absoluteValue == 1 ? "ONE" : "OTHER";
				default: return "OTHER";
			}
		}

		private static string ResolveSlavicPluralForm(long absoluteValue)
		{
			long last = absoluteValue % 10, lastTwo = absoluteValue % 100;
			if (last == 1 && lastTwo != 11) return "ONE";
			if (last >= 2 && last <= 4 && (lastTwo < 12 || lastTwo > 14)) return "FEW";
			return "MANY";
		}

		private static string ResolvePolishPluralForm(long absoluteValue)
		{
			if (absoluteValue == 1) return "ONE";
			long last = absoluteValue % 10, lastTwo = absoluteValue % 100;
			if (last >= 2 && last <= 4 && (lastTwo < 12 || lastTwo > 14)) return "FEW";
			return "MANY";
		}
	}
}
