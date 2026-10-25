using Codebase.Core;
using Codebase.Events.Analytics;
using Codebase.Events.Models;
using Scellecs.Morpeh;

namespace Codebase.Events.Systems
{
	public class EventAnalyticsPlaytimeSystem : SystemBase
	{
		[Inject]
		private EventsRepository eventsRepository;

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
