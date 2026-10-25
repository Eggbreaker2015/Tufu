using Multicast.UserData;

namespace Codebase.Location
{
	public class IslandMasteryUd : UdObject
	{
		public UdDict<IslandMasteryLocationUd> Locations { get; }

		public UdValue<bool> LegacyMigrationComplete { get; }

		public IslandMasteryUd(UdArgs args) : base(args)
{
    Locations = new global::Multicast.UserData.UdDict<global::Codebase.Location.IslandMasteryLocationUd>(Child("locations", true), childArgs => new global::Codebase.Location.IslandMasteryLocationUd(childArgs));
    LegacyMigrationComplete = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("legacy_migration_complete", true), false);
}
	}
}
