using System;
using UnityEngine;

namespace Codebase.UI.Settings
{
	[Serializable]
	public sealed class SettingsLanguageOption
	{
		[SerializeField]
		private string locale;

		[SerializeField]
		private Sprite flag;

		public string Locale => this.locale;

		public Sprite Flag => this.flag;

		public SettingsLanguageOption(string locale)
		{
// Original ELF VA 0x162f7ec.
            this.locale = locale;
		}
	}
}
