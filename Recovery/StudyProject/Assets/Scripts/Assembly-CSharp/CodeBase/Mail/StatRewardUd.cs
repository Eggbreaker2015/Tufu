using Multicast.UserData;

namespace Codebase.Mail
{
	public class StatRewardUd : UdObject
	{
		public UdValue<int> StatType { get; }

		public UdValue<float> Value { get; }

		public UdValue<int> ModifierType { get; }

		public UdValue<string> Name { get; }

		public UdValue<int> Source { get; }

		public StatRewardUd(UdArgs args) : base(args)
{
    StatType = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("stat_type", true));
    Value = new global::Multicast.UserData.UdValue<global::System.Single>(Child("value", true));
    ModifierType = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("modifier_type", true));
    Name = new global::Multicast.UserData.UdValue<global::System.String>(Child("name", true));
    Source = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("source", true));
}
	}
}
