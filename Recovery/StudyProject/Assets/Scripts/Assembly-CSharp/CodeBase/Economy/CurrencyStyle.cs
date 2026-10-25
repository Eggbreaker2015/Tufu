using System;
using Codebase.Utilities;
using UnityEngine;

namespace Codebase.Economy
{
	[Serializable]
	public class CurrencyStyle
	{
		[KeysDropdown(typeof(CurrencyKeys))]
		public string Key;

		public Sprite Icon;
	}
}
