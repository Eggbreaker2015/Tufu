using System.Diagnostics;

namespace Codebase.Analytics
{
	public sealed class BootTimeReporter
	{
		private const int SECONDS_LIMIT = 100;

		private readonly IAnalyticsWrapper analytics;

		private Stopwatch stopwatch;

		private int sentSeconds;

		public void Start()
		{
		}

		public void ReportStep(string step)
		{
		}

		public void Tick()
		{
		}

		public void FlushFinalTick()
		{
		}
	}
}
