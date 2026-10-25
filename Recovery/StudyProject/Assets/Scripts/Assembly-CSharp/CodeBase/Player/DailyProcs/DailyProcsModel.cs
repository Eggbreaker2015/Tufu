using System;
using Codebase.Core;
using Codebase.Services.Time;
using UniMob;

namespace Codebase.Player.DailyProcs
{
	public class DailyProcsModel : Model<DailyProcsUd>
	{
		[Inject]
		private ITimeService timeService;

		public DailyProcsModel(Lifetime lifetime)
			: base(lifetime)
		{
		}

		public bool TryConsumeProc(string key, int dailyLimit)
		{
			// Native ELF VA 0x1467c9c; zero and negative limits allow no procs.
			RefreshDailyState();int count=GetCount(key);if(count>=dailyLimit)return false;
			Data.Counts[key]=count+1;return true;
		}

		public int GetRemainingProcs(string key, int dailyLimit)
		{
			// Native ELF VA 0x1467f7c.
			RefreshDailyState();return Math.Max(0,dailyLimit-GetCount(key));
		}

		public bool HasReachedLimit(string key, int dailyLimit)
		{
			// Native ELF VA 0x1468004.
			RefreshDailyState();return GetCount(key)>=dailyLimit;
		}

		private int GetCount(string key)
		{
			// Native ELF VA 0x1467ef0.
			return Data.Counts.TryGetValue(key,out int value)?value:0;
		}

		private void RefreshDailyState()
		{
			// Native ELF VA 0x1467d4c; only a strictly later date resets. Clock rollback retains usage.
			long today=timeService.Now.ToDateTime().Date.Ticks;
			if(today<=Data.LastResetDayTicks.Value)return;
			Data.LastResetDayTicks.Value=today;Data.Counts.Clear();
		}
	}
}
