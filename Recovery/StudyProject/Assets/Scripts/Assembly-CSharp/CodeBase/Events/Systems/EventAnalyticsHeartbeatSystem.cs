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
		}

		public override void OnUpdate(float deltaTime)
		{
		}
	}
}
