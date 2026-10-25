using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Codebase.Events
{
	public class EventStyleConfig : ScriptableObject
	{
		public List<EventStyle> Styles;

		public List<EventCurrencyStyle> Currencies;

		public List<EventDifficultyStyle> DifficultyStyles;

		public EventProgressBarStyle ProgressBarStyle;

		public Sprite TimeSprite;

		public Sprite GiftSprite;

		public Sprite FailSprite;

		public EventStyle GetStyle(string eventKey)
		{
			// Original APK ARM64 ELF VA 0x14dab34.
			return Styles.First(item => item.Key == eventKey);
		}

		public EventCurrencyStyle GetCurrencyStyle(string currencyKey)
		{
			return Currencies.First(style => style.Key == currencyKey);
		}

		public EventDifficultyStyle GetDifficultyStyle(EventDifficulty difficulty)
		{
			// Original APK ARM64 ELF VA 0x14dad2c.
			return DifficultyStyles.First(item => item.Difficulty == difficulty);
		}
	}
}
