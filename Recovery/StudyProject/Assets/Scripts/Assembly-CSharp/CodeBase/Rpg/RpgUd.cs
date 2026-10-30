using Multicast.UserData;

namespace Codebase.Rpg
{
	public class RpgUd : UdObject
	{
		public UdValue<int> TeamLevel { get; }
		public UdValue<double> Xp { get; }
		public RpgUd(UdArgs args) : base(args)
		{
			TeamLevel = new UdValue<int>(Child("team_level", true), 1);
			Xp = new UdValue<double>(Child("xp", true));
		}
	}
}
