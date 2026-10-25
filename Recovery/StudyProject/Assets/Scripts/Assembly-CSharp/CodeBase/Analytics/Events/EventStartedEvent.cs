using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class EventStartedEvent : UserContextEventBase
	{
		[JsonExtensionData]
		private readonly Dictionary<string, object> data;

		private readonly string eventKey;

		[JsonIgnore]
		public override string Name => this.eventKey;

		public EventStartedEvent(string eventKey, string schedule, string difficulty, int currentLeague)
		{
		}
	}
}
