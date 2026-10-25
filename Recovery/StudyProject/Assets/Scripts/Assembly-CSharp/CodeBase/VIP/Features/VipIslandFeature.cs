namespace Codebase.VIP.Features
{
	public class VipIslandFeature : VipFeatureBase
	{
		public override string Key
		{
			get
			{
				return "vip_island";
			}
		}

		public VipIslandFeature(int unlockLevel) : base(unlockLevel)
		{
		}

		public int GetIslandNumber(int vipLevel)
		{
			return vipLevel > 5 ? 6 : vipLevel >= 2 ? 2 : -1;
		}

		public bool IsUnlockedAtLevel(int vipLevel)
		{
			return vipLevel > 1;
		}
	}
}
