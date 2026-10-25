namespace Codebase.VIP.Features
{
	public class VipCrystalEnchantExtraAttemptFeature : VipFeatureBase
	{
		public override string Key
		{
			get
			{
				return "crystal_enchant_extra_attempt";
			}
		}

		public int ExtraAttempts { get; }

		public VipCrystalEnchantExtraAttemptFeature(int unlockLevel, int extraAttempts) : base(unlockLevel)
		{
			ExtraAttempts = extraAttempts;
		}
	}
}
