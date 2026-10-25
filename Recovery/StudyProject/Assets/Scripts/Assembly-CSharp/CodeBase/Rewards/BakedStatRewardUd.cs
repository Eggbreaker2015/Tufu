using Codebase.Unit;
using Multicast.UserData;

namespace Codebase.Rewards
{
	public class BakedStatRewardUd : UdObject
	{
		public UdValue<StatType> StatType { get; }

		public UdValue<float> Value { get; }

		public BakedStatRewardUd(UdArgs args) : base(args)
{
    StatType = new global::Multicast.UserData.UdValue<global::Codebase.Unit.StatType>(Child("stat_type", true), (global::Codebase.Unit.StatType)1);
    Value = new global::Multicast.UserData.UdValue<global::System.Single>(Child("value", true));
}
	}
}
