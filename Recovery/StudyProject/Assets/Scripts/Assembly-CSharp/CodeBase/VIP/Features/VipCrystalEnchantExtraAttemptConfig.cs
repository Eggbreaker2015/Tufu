using System;

namespace Codebase.VIP.Features
{
	[Serializable]
	public class VipCrystalEnchantExtraAttemptConfig : VipFeatureConfig
	{
		public int ExtraAttempts;
		public override string Key
		{
			get
			{
				return "crystal_enchant_extra_attempt";
			}
		}

		public override IVipFeature CreateFeature()
		{
			return new VipCrystalEnchantExtraAttemptFeature(UnlockLevel, ExtraAttempts);
		}

		public VipCrystalEnchantExtraAttemptConfig()
		{
			ExtraAttempts = 1;
		}
	}
}
