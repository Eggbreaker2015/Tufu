using Codebase.Analytics.Systems;
using Codebase.Core;
using UnityEngine;

namespace Codebase.Analytics
{
	public class UserPlaytimeUpdater : MonoBehaviour
	{
		[Inject]
		private UserModel userModel;

		[Inject]
		private IAnalyticsWrapper analyticsWrapper;

		[Inject]
		private AfkPlaytimeSystem afkPlaytimeSystem;

		private float elapsed;

		private int seconds;

		private int activeSeconds;

		private void Start()
		{
			// Original ARM64 ELF VA 0x15ab998 restores only these two cached counters.
			elapsed = userModel.Elapsed.Value;
			seconds = userModel.Seconds.Value;
		}

		private void Update()
		{
			// Original ARM64 ELF VA 0x15aba38: real playtime, capped to one second per frame.
			elapsed += Mathf.Min(Time.unscaledDeltaTime, 1f);
			if (elapsed < 1f) return;
			elapsed -= 1f;
			seconds++;
			if (!afkPlaytimeSystem.IsAfk)
			{
				activeSeconds++;
				if (activeSeconds % 60 == 0)
				{
					userModel.ActiveSeconds.Value = activeSeconds;
					userModel.ActiveMinutes.Value++;
					analyticsWrapper.Send(new Codebase.Analytics.Events.ActivePlaytimeEvent(userModel.ActiveMinutes.Value));
				}
			}
			if (seconds % 60 == 0)
			{
				userModel.Seconds.Value = seconds;
				userModel.Elapsed.Value = elapsed;
				userModel.Minutes.Value++;
				analyticsWrapper.Send(new Codebase.Analytics.Events.PlaytimeEvent(userModel.Minutes.Value));
			}
		}
	}
}
