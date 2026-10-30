using Multicast.UserData;

namespace Codebase.Rpg.Dungeon
{
	public class RpgDungeonCooldownUd : UdObject
	{
		public UdValue<string> Key { get; }
		public UdValue<long> EndUtcTicks { get; }

		public RpgDungeonCooldownUd(UdArgs args) : base(args)
		{
			Key = new UdValue<string>(Child("key", true));
			EndUtcTicks = new UdValue<long>(Child("end_utc_ticks", true));
		}
	}
}
