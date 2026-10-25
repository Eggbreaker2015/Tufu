using Multicast.UserData;

namespace Codebase.Player.Models
{
	public class ItemDataUd : UdObject
	{
		public UdValue<string> Key { get; }

		public UdValue<int> Level { get; }

		public UdValue<int> Copies { get; }

		public UdValue<bool> Evolved { get; }

		public ItemDataUd(UdArgs args)
			: base(args)
		{
			// Native ELF VA 0x1462a4c. Exact Child keys and cache flags come from RELA-resolved literals.
			Key = new UdValue<string>(Child("key", true));
			Level = new UdValue<int>(Child("level", true));
			Copies = new UdValue<int>(Child("copies", true));
			Evolved = new UdValue<bool>(Child("evolved", true));
		}
	}
}
