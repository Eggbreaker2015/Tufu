using Codebase.Unit;
using Multicast.UserData;

namespace Codebase.Meta
{
	public class MetaStatDataUd : UdObject
	{
		public UdValue<string> Key { get; }

		public UdValue<double> BaseValue { get; }

		public UdListObject<StatModifierUd> Modifiers { get; }

		public MetaStatDataUd(UdArgs args) : base(args)
{
    Key = new global::Multicast.UserData.UdValue<global::System.String>(Child("key", true));
    BaseValue = new global::Multicast.UserData.UdValue<global::System.Double>(Child("base_value", true));
    Modifiers = new global::Multicast.UserData.UdListObject<global::Codebase.Unit.StatModifierUd>(Child("modifiers", true), childArgs => new global::Codebase.Unit.StatModifierUd(childArgs));
}
	}
}
