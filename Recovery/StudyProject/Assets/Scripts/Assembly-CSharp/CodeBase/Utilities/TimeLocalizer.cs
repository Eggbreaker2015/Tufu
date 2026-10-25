using Codebase.Services.Time;
using System;

namespace Codebase.Utilities
{
	public class TimeLocalizer
	{
		private const string timeformat_D_H_M_S = "{3}d {2}h {1}m {0}s";
		private const string timeformat_H_M_S = "{2}h {1}m {0}s";
		private const string timeformat_M_S = "{1}m {0}s";
		private const string timeformat_S = "{0}s";
		private const string timeformat_D_H = "{1}d {0}h";
		private const string timeformat_H_M = "{1}h {0}m";
		private const string timeformat_D = "{0}d";
		private const string timeformat_H = "{0}h";
		private const string timeformat_M = "{0}m";
		public static string GetFormatTime(float seconds, bool shortFormat = false)
		{
			if (seconds <= 0f)
				return string.Format(timeformat_S, 0);
			TimeSpan time = TimeSpan.FromSeconds(seconds);
			int days = time.Days;
			int hours = time.Hours;
			int minutes = time.Minutes;
			float remainingSeconds = time.Seconds;
			if (time.TotalDays >= 1.0)
				return shortFormat ? string.Format(timeformat_D_H, hours, days) : string.Format(timeformat_D_H_M_S, remainingSeconds, minutes, hours, days);
			if (time.TotalHours >= 1.0)
				return shortFormat ? string.Format(timeformat_H_M, minutes, hours) : string.Format(timeformat_H_M_S, remainingSeconds, minutes, hours);
			if (time.TotalMinutes >= 1.0)
				return string.Format(timeformat_M_S, (int)remainingSeconds, minutes);
			if (shortFormat)
				return string.Format(timeformat_S, (int)Math.Round(time.TotalSeconds));
			return string.Format(timeformat_S, (float)Math.Round(time.TotalSeconds, 2));
		}

		public static string GetFormatTime(GameTime pastTime, GameTime now)
		{
			return null;
		}

		public static string GetFormatDate(GameTime gameTime)
		{
			return null;
		}
	}
}
