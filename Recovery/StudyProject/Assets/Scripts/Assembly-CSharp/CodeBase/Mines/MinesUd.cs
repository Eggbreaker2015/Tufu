using Multicast.UserData;

namespace Codebase.Mines
{
	public class MinesUd : UdObject
	{
		public UdValue<bool> IsUnlocked { get; }

		public MinesUd(UdArgs args) : base(args)
{
    IsUnlocked = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("is_unlocked", true));
}
	}
}
