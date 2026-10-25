using System;

namespace Codebase.Events
{
	[Serializable]
	public class EventTimeInterval
	{
		public int StartHour;

		public int StartMinute;

		public int EndHour;

		public int EndMinute;

		private string Preview => null;

		public TimeSpan StartTime => new TimeSpan(StartHour, StartMinute, 0);

		public TimeSpan EndTime => new TimeSpan(EndHour, EndMinute, 0);

		public bool IsActiveAt(TimeSpan utcTime)
		{
			// Original APK ARM64 ELF VA 0x14d95c8.
			return StartTime <= EndTime ? utcTime >= StartTime && utcTime < EndTime : utcTime >= StartTime || utcTime < EndTime;
		}
	}
}
