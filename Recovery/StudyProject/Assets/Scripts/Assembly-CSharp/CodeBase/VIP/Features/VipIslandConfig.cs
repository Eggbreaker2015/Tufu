using System;

namespace Codebase.VIP.Features
{
	[Serializable]
	public class VipIslandConfig : VipFeatureConfig
	{
		public int Island3UnlockLevel;
		public int Island6UnlockLevel;
		public override string Key
		{
			get
			{
				return "vip_island";
			}
		}

		public override VipFeatureChangeType GetChangeType(int level)
		{
			int first = int.MaxValue;
			if (Island3UnlockLevel > 0)
				first = System.Math.Min(first, Island3UnlockLevel);
			if (Island6UnlockLevel > 0)
				first = System.Math.Min(first, Island6UnlockLevel);
			if (first == int.MaxValue)
				return base.GetChangeType(level);
			if (level != Island3UnlockLevel && level != Island6UnlockLevel)
				return VipFeatureChangeType.None;
			return level == first ? VipFeatureChangeType.New : VipFeatureChangeType.Up;
		}

		public override int GetLastChangeLevel(int currentLevel)
		{
			int first = int.MaxValue;
			if (Island3UnlockLevel > 0)
				first = System.Math.Min(first, Island3UnlockLevel);
			if (Island6UnlockLevel > 0)
				first = System.Math.Min(first, Island6UnlockLevel);
			if (first == int.MaxValue || currentLevel < first)
				return -1;
			if (Island6UnlockLevel > 0 && Island6UnlockLevel <= currentLevel)
				first = System.Math.Max(first, Island6UnlockLevel);
			return first;
		}

		public override IVipFeature CreateFeature()
		{
			return new VipIslandFeature(UnlockLevel);
		}
	}
}
