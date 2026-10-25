using System;
using Codebase.Utilities;
using UnityEngine;

namespace Codebase.Events
{
	[Serializable]
	public class EventCurrencyStyle
	{
		[KeysDropdown(typeof(EventCurrencies))]
		public string Key;

		public Sprite Sprite;

		public Sprite PrefixSprite;

		public Sprite MinimapSprite;

		public Color Color;
	}
}
