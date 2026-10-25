using Multicast.UserData;

namespace Codebase.Meta
{
	public class MetaStatsUd : UdObject
	{
		public UdListObject<MetaStatDataUd> Stats { get; }

		public MetaStatsUd(UdArgs args) : base(args)
{
    Stats = new global::Multicast.UserData.UdListObject<global::Codebase.Meta.MetaStatDataUd>(Child("stats", true), childArgs => new global::Codebase.Meta.MetaStatDataUd(childArgs));
}
	}
}
