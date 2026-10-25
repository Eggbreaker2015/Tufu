using System;
using System.Collections.Generic;
using Codebase.Player.Inventory;

namespace Codebase.VIP
{
	[Serializable]
	public class VipFeatureDescription
	{
		public int UnlockLevel;

		public string LocalizationKey;

		public List<int> UpgradeLevels = new List<int>();

		public ItemConfig PreviewItemConfig;

		public List<VipFeatureDescriptionLevel> LevelLocalizationOverrides = new List<VipFeatureDescriptionLevel>();

		public string GetLocalizationKey(int level)
		{
            // Native 0x14abf5c: exact level override, then authored default (not nearest previous level).
            foreach (VipFeatureDescriptionLevel entry in LevelLocalizationOverrides)
                if (entry.Level == level) return entry.LocalizationKey;
            return LocalizationKey;
		}
	}
}
