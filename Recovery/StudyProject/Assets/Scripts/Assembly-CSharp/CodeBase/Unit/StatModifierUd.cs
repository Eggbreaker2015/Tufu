using Multicast.UserData;

namespace Codebase.Unit
{
	public class StatModifierUd : UdObject
	{
		public UdValue<string> Name { get; }

		public UdValue<float> Value { get; }

		public UdValue<ModifierType> ModifierType { get; }

		public UdValue<ModifierSource> Source { get; }

		public UdValue<int> Param { get; }

		public UdValue<string> StringParam { get; }

		public StatModifierUd(UdArgs args)
			: base(args)
		{
			// Native ELF VA 0x15ca5c8. Exact Child keys and cache flags come from RELA-resolved literals.
			Name = new UdValue<string>(Child("name", true));
			Value = new UdValue<float>(Child("value", true));
			ModifierType = new UdValue<ModifierType>(Child("modifier_type", true), default(ModifierType));
			Source = new UdValue<ModifierSource>(Child("source", true), default(ModifierSource));
			Param = new UdValue<int>(Child("param", true));
			StringParam = new UdValue<string>(Child("string_param", true));
		}
	}
}
