using System;
using Codebase.Utilities;

namespace Codebase.Amulet
{
	[Serializable]
	public class SoulDef
	{
		[KeysDropdown(typeof(SoulKeys))]
		public string Key;

		public double BaseExperience;
	}
}
