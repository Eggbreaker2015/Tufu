using Multicast.UserData;

namespace Codebase.Crystals
{
	public class CrystalItemUd : UdObject
	{
		public UdValue<int> Id { get; }

		public UdValue<int> Level { get; }

		public UdValue<int> Rarity { get; }

		public UdValue<int> BaseStatType { get; }

		public UdValue<double> BaseStatValue { get; }

		public UdValue<int> EnchantLevel { get; }

		public UdValue<int> SpecialStatType { get; }

		public UdValue<double> SpecialStatValue { get; }

		public UdValue<int> LegendaryAbility { get; }

		public UdValue<bool> IsProtected { get; }

		public UdValue<string> EquippedSlotKey { get; }

		public UdValue<double> SecondaryStatValue { get; }

		public UdValue<int> SecondaryStatType { get; }

		public UdValue<int> Variant { get; }

		public UdValue<bool> IsNew { get; }

		public CrystalItemUd(UdArgs args) : base(args)
{
    Id = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("id", true));
    Level = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("level", true));
    Rarity = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("rarity", true));
    BaseStatType = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("base_stat_type", true));
    BaseStatValue = new global::Multicast.UserData.UdValue<global::System.Double>(Child("base_stat_value", true));
    EnchantLevel = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("enchant_level", true));
    SpecialStatType = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("special_stat_type", true), -1);
    SpecialStatValue = new global::Multicast.UserData.UdValue<global::System.Double>(Child("special_stat_value", true));
    LegendaryAbility = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("legendary_ability", true), -1);
    IsProtected = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("is_protected", true));
    EquippedSlotKey = new global::Multicast.UserData.UdValue<global::System.String>(Child("equipped_slot_key", true), string.Empty);
    SecondaryStatValue = new global::Multicast.UserData.UdValue<global::System.Double>(Child("secondary_stat_value", true));
    SecondaryStatType = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("secondary_stat_type", true), -1);
    Variant = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("variant", true));
    IsNew = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("is_new", true));
}
	}
}
