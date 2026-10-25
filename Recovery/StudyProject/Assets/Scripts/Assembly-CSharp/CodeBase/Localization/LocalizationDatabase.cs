using System;
using UnityEngine;

namespace Codebase.Localization
{
	public class LocalizationDatabase : ScriptableObject
	{
		[Serializable]
		public struct Entry
		{
			public string key;

			public string[] values;
		}

		[SerializeField]
		private string[] supportedLocales;

		[SerializeField]
		private Entry[] entries;

		public string[] SupportedLocales => this.supportedLocales;

		public Entry[] Entries => this.entries;

		public void SetData(string[] locales, Entry[] localizationEntries)
		{
		}
	}
}
