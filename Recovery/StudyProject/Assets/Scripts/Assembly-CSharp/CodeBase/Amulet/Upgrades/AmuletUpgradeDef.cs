using System;
using System.Collections.Generic;
using Codebase.Meta.Upgrades;
using Codebase.Utilities;
using UnityEngine;

namespace Codebase.Amulet.Upgrades
{
	[Serializable]
	public class AmuletUpgradeDef
	{
		[KeysDropdown(typeof(UpgradeKeys), Prefix = "graph")]
		public string Key;

		[KeysDropdown(typeof(UpgradeKeys), Prefix = "graph")]
		public List<string> RequiredUpgradeKeys;

		public int RequiredAmuletLevel;

		public Sprite Icon;
	}
}
