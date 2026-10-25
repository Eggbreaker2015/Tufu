using Multicast.UserData;

namespace Codebase.LevelPass
{
	public class LevelPassUd : UdObject
	{
		public UdListObject<LevelPassDataUd> Passes { get; }

		public LevelPassUd(UdArgs args) : base(args)
{
    Passes = new global::Multicast.UserData.UdListObject<global::Codebase.LevelPass.LevelPassDataUd>(Child("passes", true), childArgs => new global::Codebase.LevelPass.LevelPassDataUd(childArgs));
}
	}
}
