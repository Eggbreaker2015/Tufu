using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Codebase.VIP
{
	public class VipConfig : ScriptableObject
	{
		public int MaxLevel;
		[SerializeReference]
		public List<VipFeatureConfig> Features;
		public List<VipFeatureDescription> FeatureDescriptions;
		public int[] RequiredExperiencePerLevel;
		public int GetRequiredExperience(int level)
		{
			if (level < 1 || level > MaxLevel || level > RequiredExperiencePerLevel.Length)
				return 0;
			return RequiredExperiencePerLevel[level - 1];
		}

		public List<VipFeatureConfig> GetFeatureConfigsByKey(string key)
		{
			return Features.Where(feature => feature.Key == key).ToList();
		}

		public List<IVipFeature> GetUnlockedFeatures(int vipLevel)
		{
			var result = new List<IVipFeature>();
			foreach (var feature in Features)
				if (feature.UnlockLevel <= vipLevel)
					result.Add(feature.CreateFeature());
			return result;
		}

		public int GetMaxLevelInFeatures()
		{
			int maximum = 1;
			foreach (var description in FeatureDescriptions)
			{
				if (description == null)
					continue;
				maximum = System.Math.Max(maximum, description.UnlockLevel);
				if (description.UpgradeLevels != null)
					foreach (int level in description.UpgradeLevels)
						maximum = System.Math.Max(maximum, level);
			}

			return maximum;
		}

		public VipConfig()
		{
			MaxLevel = 10;
			Features = new List<VipFeatureConfig>();
			FeatureDescriptions = new List<VipFeatureDescription>();
		}
	}
}
