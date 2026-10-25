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

		public List<int> UpgradeLevels;

		public ItemConfig PreviewItemConfig;

		public List<VipFeatureDescriptionLevel> LevelLocalizationOverrides;

		public string GetLocalizationKey(int level)
		{
			return null;
		}
	}
}
