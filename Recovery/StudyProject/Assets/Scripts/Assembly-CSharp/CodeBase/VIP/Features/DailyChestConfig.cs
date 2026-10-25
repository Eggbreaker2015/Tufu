using System;
using System.Collections.Generic;
using System.Linq;

namespace Codebase.VIP.Features
{
	[Serializable]
	public class DailyChestConfig : VipFeatureConfig
	{
		public List<DailyChestLevelConfig> LevelConfigs;
		public override string Key
		{
			get
			{
				return "daily_chest";
			}
		}

		public DailyChestLevelConfig GetActiveLevelConfig(int vipLevel)
		{
			DailyChestLevelConfig selected = null;
			if (LevelConfigs != null)
				foreach (var item in LevelConfigs)
					if (item != null && item.Level > 0 && item.Level <= vipLevel && (selected == null || selected.Level < item.Level))
						selected = item;
			return selected;
		}

		public override VipFeatureChangeType GetChangeType(int level)
		{
			if (level == UnlockLevel)
				return VipFeatureChangeType.New;
			return LevelConfigs != null && LevelConfigs.Any(item => item != null && item.Level == level) ? VipFeatureChangeType.Up : VipFeatureChangeType.None;
		}

		public override int GetLastChangeLevel(int currentLevel)
		{
			if (currentLevel < UnlockLevel)
				return -1;
			int last = UnlockLevel;
			if (LevelConfigs != null)
				foreach (var item in LevelConfigs)
					if (item != null && item.Level > last && item.Level <= currentLevel)
						last = item.Level;
			return last;
		}

		public override IVipFeature CreateFeature()
		{
			return new DailyChestFeature(UnlockLevel);
		}
	}
}
