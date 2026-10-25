using System;
using Codebase.Utilities;
using UnityEngine;

namespace Codebase.Meta.Upgrades
{
	[Serializable]
	public class UpgradeStyle
	{
		[KeysDropdown(typeof(UpgradeKeys))]
		public string Key;

		public Sprite Icon;
	}
}
