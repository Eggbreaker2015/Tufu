namespace Codebase.VIP.Features
{
	public class StatMultiplierFeature : VipFeatureBase
	{
		private readonly StatMultiplierConfig config;
		public override string Key
		{
			get
			{
				return "stat_multiplier";
			}
		}

		public StatMultiplierFeature(int unlockLevel, StatMultiplierConfig config) : base(unlockLevel)
		{
			this.config = config;
		}

		public StatMultiplierLevelConfig GetLevelConfig(int vipLevel)
		{
			return config == null ? null : config.GetActiveLevelConfig(vipLevel);
		}
	}
}
