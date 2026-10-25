using System;

namespace Codebase.VIP.Features
{
	[Serializable]
	public class ExtraWeaponSlotConfig : VipFeatureConfig
	{
		public override string Key
		{
			get
			{
				return "extra_weapon_slot";
			}
		}

		public override IVipFeature CreateFeature()
		{
			return new ExtraWeaponSlotFeature(UnlockLevel);
		}
	}
}
