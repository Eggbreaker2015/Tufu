using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class EventCompletedEvent : UserContextEventBase
	{
		[JsonExtensionData]
		private readonly Dictionary<string, object> data;

		private readonly string eventKey;

		[JsonIgnore]
		public override string Name => this.eventKey;

		public EventCompletedEvent(string eventKey, string schedule, string difficulty, string result, int miniBossesDefeated, int eventMobsKilled, int playtimeMinutes, int adsWatched, int currentLeague)
		{
		}
	}
}
