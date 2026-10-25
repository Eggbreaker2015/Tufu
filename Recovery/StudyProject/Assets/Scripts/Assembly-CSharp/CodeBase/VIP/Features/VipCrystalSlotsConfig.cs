using System;
using Codebase.Crystals;
using Codebase.Utilities;

namespace Codebase.VIP.Features
{
	[Serializable]
	public class VipCrystalSlotsConfig : VipFeatureConfig
	{
		[KeysDropdown(typeof(CrystalSlotKeys))]
		public string SlotKey;
		public override string Key
		{
			get
			{
				return "crystal_slots";
			}
		}

		public override IVipFeature CreateFeature()
		{
			return new VipCrystalSlotsFeature(UnlockLevel, SlotKey);
		}
	}
}
