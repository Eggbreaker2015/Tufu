using System;
using Codebase.Meta.Upgrades;
using Codebase.Utilities;

namespace Codebase.Crystals
{
	[Serializable]
	public class CrystalSlotDef
	{
		[KeysDropdown(typeof(CrystalSlotKeys))]
		public string Key;

		public bool UnlockedByDefault;

		public CrystalSlotUnlockSource UnlockSource;

		[KeysDropdown(typeof(UpgradeKeys))]
		public string UnlockUpgradeKey;
	}
}
