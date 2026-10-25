namespace Codebase.VIP.Features
{
	public class AutoFarmFeature : VipFeatureBase
	{
		public override string Key
		{
			get
			{
				return "auto_farm";
			}
		}

		public AutoFarmFeature(int unlockLevel) : base(unlockLevel)
		{
		}

		public float GetResourceMultiplier(int vipLevel)
		{
			return vipLevel >= 8 ? 2f : 1f;
		}

		public float GetRadius(int vipLevel)
		{
			return vipLevel >= 5 ? float.MaxValue : 50f;
		}

		public float GetMaxDurationMinutes(int vipLevel)
		{
			return vipLevel >= 5 ? 40f : 30f;
		}

		public override void OnLevelUp(int fromLevel, int toLevel)
		{
		}
	}
}
