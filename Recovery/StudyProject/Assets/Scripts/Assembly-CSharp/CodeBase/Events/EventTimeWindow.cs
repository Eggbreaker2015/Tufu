using System;
using UnityEngine;

namespace Codebase.Events
{
	[Serializable]
	public class EventTimeWindow
	{
		[Tooltip("Start hour in UTC (0-23)")]
		[Range(0f, 23f)]
		public int StartHour;

		[Tooltip("Start minute in UTC (0-59)")]
		[Range(0f, 59f)]
		public int StartMinute;

		[Tooltip("End hour in UTC (0-23)")]
		[Range(0f, 23f)]
		public int EndHour;

		[Tooltip("End minute in UTC (0-59)")]
		[Range(0f, 59f)]
		public int EndMinute;

		public TimeSpan StartTime => default;

		public TimeSpan EndTime => default;

		public bool IsActiveAt(DateTime utcTime)
		{
			return false;
		}
	}
}
