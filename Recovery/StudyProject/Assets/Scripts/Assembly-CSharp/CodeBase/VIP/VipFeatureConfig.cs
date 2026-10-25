using System;

namespace Codebase.VIP
{
	[Serializable]
	public abstract class VipFeatureConfig
	{
		public int UnlockLevel;
		public abstract string Key { get; }

		public virtual VipFeatureChangeType GetChangeType(int level)
		{
			return level == UnlockLevel ? VipFeatureChangeType.New : VipFeatureChangeType.None;
		}

		public virtual int GetLastChangeLevel(int currentLevel)
		{
			return currentLevel >= UnlockLevel ? UnlockLevel : -1;
		}

		public abstract IVipFeature CreateFeature();
	}
}
