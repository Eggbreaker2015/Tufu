namespace Codebase.Events
{
	public static class EventKeys
	{
		public const string Crystal = "crystal_event";

		public const string CrystalRoster = "crystal_event_roster";

		public static bool IsCrystal(string key)
		{
			// Original APK ARM64 ELF VA 0x14da984.
			return key == Crystal || key == CrystalRoster;
		}
	}
}
