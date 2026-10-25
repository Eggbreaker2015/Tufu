using Multicast.UserData;

namespace Codebase.Amulet
{
	public class AmuletUd : UdObject
	{
		public UdValue<bool> IsUnlocked { get; }

		public UdValue<double> TotalExperience { get; }

		public UdDictValue<int> UpgradeLevels { get; }

		public UdListValue<string> SeenUpgradeKeys { get; }

		public UdListValue<string> DiscoveredSoulKeys { get; }

		public AmuletUd(UdArgs args) : base(args)
{
    IsUnlocked = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("is_unlocked", true));
    TotalExperience = new global::Multicast.UserData.UdValue<global::System.Double>(Child("total_experience", true));
    UpgradeLevels = new global::Multicast.UserData.UdDictValue<global::System.Int32>(Child("upgrade_levels", true));
    SeenUpgradeKeys = new global::Multicast.UserData.UdListValue<global::System.String>(Child("seen_upgrade_keys", true));
    DiscoveredSoulKeys = new global::Multicast.UserData.UdListValue<global::System.String>(Child("discovered_soul_keys", true));
}
	}
}
