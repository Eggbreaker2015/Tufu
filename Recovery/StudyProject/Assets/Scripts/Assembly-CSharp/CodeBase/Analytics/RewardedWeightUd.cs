using Multicast.UserData;

namespace Codebase.Analytics
{
	public class RewardedWeightUd : UdObject
	{
		public UdValue<int> Type { get; }

		public UdValue<float> Weight { get; }

		public RewardedWeightUd(UdArgs args) : base(args)
{
    Type = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("type", true));
    Weight = new global::Multicast.UserData.UdValue<global::System.Single>(Child("weight", true));
}
	}
}
