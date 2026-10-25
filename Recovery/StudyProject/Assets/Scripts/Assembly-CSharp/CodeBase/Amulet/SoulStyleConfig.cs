using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Codebase.Amulet
{
	public class SoulStyleConfig : ScriptableObject
	{
		public List<SoulStyle> Styles;

		public SoulStyle GetStyle(string soulKey)
		{
			// Native ELF VA 0x15b52f0.
			return Styles.FirstOrDefault(style => style.Key == soulKey);
		}

		public Sprite GetIcon(string soulKey)
		{
			// Native ELF VA 0x15b53ec; original absent style returns null.
			return GetStyle(soulKey)?.Icon;
		}

		public IReadOnlyList<string> GetAllKeys()
		{
			// Native ELF VA 0x15b5404; bounded tail calls Select then ToList.
			return Styles.Select(style => style.Key).ToList();
		}
	}
}
