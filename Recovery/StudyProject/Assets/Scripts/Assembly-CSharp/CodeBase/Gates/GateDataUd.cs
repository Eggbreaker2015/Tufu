using Multicast.UserData;

namespace Codebase.Gates
{
	public class GateDataUd : UdObject
	{
		public UdValue<string> Key { get; }

		public UdValue<bool> IsUnlocked { get; }

		public GateDataUd(UdArgs args) : base(args)
{
    Key = new global::Multicast.UserData.UdValue<global::System.String>(Child("key", true));
    IsUnlocked = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("is_unlocked", true));
}
	}
}
