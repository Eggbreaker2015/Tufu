using Multicast.UserData;

namespace Codebase.Location
{
	public class IslandMasteryLocationUd : UdObject
	{
		public UdValue<int> MedalLevel { get; }

		public UdValue<bool> ClearProcessed { get; }

		public IslandMasteryLocationUd(UdArgs args) : base(args)
{
    MedalLevel = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("medal_level", true), 0);
    ClearProcessed = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("clear_processed", true), false);
}
	}
}
