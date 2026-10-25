using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class SnapshotStatsEvent : UserContextEventBase
	{
		[JsonExtensionData]
		private readonly Dictionary<string, object> data;

		[JsonIgnore]
		public override string Name => null;

		public SnapshotStatsEvent(Dictionary<string, object> snapshot)
		{
		}
	}
}
