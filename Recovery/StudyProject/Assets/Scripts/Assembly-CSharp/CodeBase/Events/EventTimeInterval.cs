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

		public TimeSpan StartTime => default;

		public TimeSpan EndTime => default;

		public bool IsActiveAt(TimeSpan utcTime)
		{
			return false;
		}
	}
}
