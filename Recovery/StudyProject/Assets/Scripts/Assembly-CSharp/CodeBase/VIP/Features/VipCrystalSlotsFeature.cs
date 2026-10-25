namespace Codebase.VIP.Features
{
	public class VipCrystalSlotsFeature : VipFeatureBase
	{
		public override string Key
		{
			get
			{
				return "crystal_slots";
			}
		}

		public string SlotKey { get; }

		public VipCrystalSlotsFeature(int unlockLevel, string slotKey) : base(unlockLevel)
		{
			SlotKey = slotKey;
		}
	}
}
