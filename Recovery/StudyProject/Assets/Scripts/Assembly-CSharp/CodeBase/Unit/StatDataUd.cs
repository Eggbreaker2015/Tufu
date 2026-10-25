using Multicast.UserData;

namespace Codebase.Unit
{
	public class StatDataUd : UdObject
	{
		public UdValue<StatType> Stat { get; }

		public UdValue<double> BaseValue { get; }

		public UdListObject<StatModifierUd> Modifiers { get; }

		public StatDataUd(UdArgs args)
			: base(args)
		{
			// Native ELF VA 0x15ca1e8. Exact Child keys and cache flags come from RELA-resolved literals.
			Stat = new UdValue<StatType>(Child("stat_type", true));
			BaseValue = new UdValue<double>(Child("base_value", true));
			Modifiers = new UdListObject<StatModifierUd>(Child("modifiers", false), child => new StatModifierUd(child));
		}
	}
}
