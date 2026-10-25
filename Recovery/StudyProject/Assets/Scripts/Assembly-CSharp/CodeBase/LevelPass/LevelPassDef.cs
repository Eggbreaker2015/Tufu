using System;
using System.Collections.Generic;
using Codebase.In_apps;
using Codebase.Utilities;

namespace Codebase.LevelPass
{
	[Serializable]
	public class LevelPassDef
	{
		[KeysDropdown(typeof(LevelPassKeys))]
		public string Key;

		public ProductKey ProductKey;

		public List<LevelPassTier> Tiers;
	}
}
