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
			return null;
		}

		public EventCurrencyStyle GetCurrencyStyle(string currencyKey)
		{
			return Currencies.First(style => style.Key == currencyKey);
		}

		public EventDifficultyStyle GetDifficultyStyle(EventDifficulty difficulty)
		{
			return null;
		}
	}
}
