using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Codebase.Economy
{
	[CreateAssetMenu(menuName = "Devour/Economy/Currency Style Config")]
	public class CurrencyStyleConfig : ScriptableObject
	{
		public List<CurrencyStyle> Styles = new List<CurrencyStyle>(); // Original ctor ELF 0x1516a20.

		public CurrencyStyle GetStyle(string key)
		{
			return Styles.FirstOrDefault(style => style.Key == key); // Original ELF 0x151690c / predicate 0x1516aa8.
		}

		public Sprite GetIcon(string key)
		{
			return GetStyle(key)?.Icon; // Original ELF 0x1516a08.
		}
	}
}
