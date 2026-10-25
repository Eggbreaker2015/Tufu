using System;
using Codebase.Core;
using Codebase.Events.Analytics;
using Codebase.Services.Time;
using Scellecs.Morpeh;

namespace Codebase.Events.Systems
{
	public class EventAnalyticsHeartbeatSystem : SystemBase
	{
		private const float IntervalSeconds = 30f;

		[Inject]
		private ITimeService timeService;

		[Inject]
		private EventAnalyticsModel eventAnalyticsModel;

		private float elapsed;

		public override void OnAwake()
        {
            // Original ELF VA0x14db0c0: initial heartbeat is after30 seconds, not immediate.
            elapsed = 0f;
        }

		public override void OnUpdate(float deltaTime)
        {
            // Original ELF VA0x14db0c8: subtract one interval and issue one heartbeat.
            elapsed += deltaTime;
            if (elapsed < IntervalSeconds) return;
            elapsed -= IntervalSeconds;
            int now = unchecked((int)new DateTimeOffset(timeService.Now.ToDateTime().ToUniversalTime()).ToUnixTimeSeconds());
            eventAnalyticsModel.HeartbeatUtc(now);
        }
	}
}
