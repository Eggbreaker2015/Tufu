namespace Codebase.VIP.Features
{
	public class DailyChestFeature : VipFeatureBase
	{
		public override string Key
		{
			get
			{
				return "daily_chest";
			}
		}

		public DailyChestFeature(int unlockLevel) : base(unlockLevel)
		{
		}

		public float GetStatValue(int vipLevel, float gpm)
		{
			return unchecked(vipLevel * 5) * gpm;
		}
	}
}
