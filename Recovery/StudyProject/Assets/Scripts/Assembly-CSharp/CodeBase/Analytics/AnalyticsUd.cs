using Multicast.UserData;

namespace Codebase.Analytics
{
	public class AnalyticsUd : UdObject
	{
		public UdDictValue<string> BakedRewards { get; }

		public AnalyticsUd(UdArgs args) : base(args)
{
    BakedRewards = new global::Multicast.UserData.UdDictValue<global::System.String>(Child("bakedRewards", true));
}
	}
}
