using System.Diagnostics;
using UnityEngine;

namespace FIMSpace
{
	public static class FDebug
	{
		private static readonly Stopwatch _debugWatch;

		public static long _LastMeasureMilliseconds;

		public static long _LastMeasureTicks;

		public static void Log(string log)
		{
		}

		public static void Log(string log, string category)
		{
		}

		public static void LogRed(string log)
		{
		}

		public static void LogOrange(string log)
		{
		}

		public static void LogYellow(string log)
		{
		}

		public static void StartMeasure()
		{
		}

		public static void PauseMeasure()
		{
		}

		public static void ResumeMeasure()
		{
		}

		public static void EndMeasureAndLog(string v)
		{
		}

		public static long EndMeasureAndGetTicks()
		{
			return 0L;
		}

		public static string MarkerColor(string color)
		{
			return null;
		}

		public static string EndColorMarker()
		{
			return null;
		}

		public static void DrawBounds2D(this Bounds b, Color c, float y = 0f, float scale = 1f, float duration = 1.1f)
		{
		}

		public static void DrawBounds3D(this Bounds b, Color c, float scale = 1f, float time = 1.01f)
		{
		}
	}
}
