using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Codebase.Core;
using UnityEngine;

namespace Codebase.Localization
{
	public static class LocalizationService
	{
		private static readonly string[] Locales = { "en", "de", "es", "fr", "it", "ja", "ko", "pl", "pt-BR", "ru", "tr", "uk", "zh" };

		private const string DefaultLocale = "en";

		private const string LocalePrefsKey = "locale";

		private const string DatabasePath = "Localization/LocalizationDatabase";

		private static Dictionary<string, string[]> entries;

		private static int currentLocaleIndex;

		private static string currentLocale;

		[CompilerGenerated]
		private static Action m_OnLocaleChanged;

		public static string CurrentLocale => currentLocale;

		public static string[] SupportedLocales => Locales;

		public static event Action OnLocaleChanged
		{
			[CompilerGenerated]
			add
			{
				Action previous, combined;
				do { previous = m_OnLocaleChanged; combined = (Action)Delegate.Combine(previous, value); }
				while (Interlocked.CompareExchange(ref m_OnLocaleChanged, combined, previous) != previous);
			}
			[CompilerGenerated]
			remove
			{
				Action previous, removed;
				do { previous = m_OnLocaleChanged; removed = (Action)Delegate.Remove(previous, value); }
				while (Interlocked.CompareExchange(ref m_OnLocaleChanged, removed, previous) != previous);
			}
		}

		public static void Initialize()
		{
			var database = Resources.Load<LocalizationDatabase>(DatabasePath);
			if (database == null)
			{
				Debug.LogError("LocalizationDatabase not found at Resources/Localization/LocalizationDatabase");
				currentLocale = DefaultLocale;
				currentLocaleIndex = 0;
				entries = new Dictionary<string, string[]>();
				return;
			}
			entries = new Dictionary<string, string[]>(database.Entries.Length);
			foreach (var entry in database.Entries) entries[entry.key] = entry.values;
			var locale = PlayerPrefs.GetString(LocalePrefsKey, string.Empty);
			if (string.IsNullOrEmpty(locale)) locale = GetDefaultLocale();
			SetLocaleInternal(locale, false);
		}

		public static string Get(string key)
		{
			if (entries == null) Initialize();
			return Get(key, currentLocaleIndex);
		}

		public static string Get(string key, string locale)
		{
			if (entries == null) Initialize();
			return Get(key, Math.Max(0, Array.IndexOf(Locales, locale)));
		}

		private static string Get(string key, int localeIndex)
		{
			if (!entries.TryGetValue(key, out var values))
			{
				Debug.LogWarning("Localization key '" + key + "' not found");
				return key;
			}
			var value = values[localeIndex];
			if (!string.IsNullOrEmpty(value)) return value;
			return string.IsNullOrEmpty(values[0]) ? key : values[0];
		}

		public static void SetLocale(string locale)
		{
			SetLocaleInternal(locale, true);
		}

		public static List<string> GetKeysWithPrefix(string prefix)
		{
			if (entries == null) Initialize();
			var result = new List<string>();
			foreach (var key in entries.Keys)
				if (key.StartsWith(prefix, StringComparison.Ordinal)) result.Add(key);
			return result;
		}

		private static void SetLocaleInternal(string locale, bool notify)
		{
			int index = Array.IndexOf(Locales, locale);
			currentLocale = index < 0 ? DefaultLocale : locale;
			currentLocaleIndex = Math.Max(index, 0);
			PlayerPrefs.SetString(LocalePrefsKey, currentLocale);
			LocalizationFontService.ApplyForLocale(currentLocale);
			if (notify) m_OnLocaleChanged?.Invoke();
		}

		private static string GetDefaultLocale()
		{
			switch (Application.systemLanguage)
			{
				case SystemLanguage.German: return "de";
				case SystemLanguage.Spanish: return "es";
				case SystemLanguage.French: return "fr";
				case SystemLanguage.Italian: return "it";
				case SystemLanguage.Japanese: return "ja";
				case SystemLanguage.Korean: return "ko";
				case SystemLanguage.Polish: return "pl";
				case SystemLanguage.Portuguese: return "pt-BR";
				case SystemLanguage.Russian: return "ru";
				case SystemLanguage.Turkish: return "tr";
				case SystemLanguage.Ukrainian: return "uk";
				case SystemLanguage.Chinese:
				case SystemLanguage.ChineseSimplified:
				case SystemLanguage.ChineseTraditional: return "zh";
				default: return DefaultLocale;
			}
		}

		[StaticReset]
		private static void ResetStaticState()
		{
			m_OnLocaleChanged = null;
			entries = null;
			currentLocale = null;
			currentLocaleIndex = 0;
		}
	}
}
