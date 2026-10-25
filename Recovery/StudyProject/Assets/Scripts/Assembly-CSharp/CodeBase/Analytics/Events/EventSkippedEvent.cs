using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class EventSkippedEvent : UserContextEventBase
	{
		[JsonExtensionData]
		private readonly Dictionary<string, object> data;

		private readonly string eventKey;

		[JsonIgnore]
		public override string Name => this.eventKey;

		public EventSkippedEvent(string eventKey, string schedule, bool online, int currentLeague)
		{
		}
	}
}
