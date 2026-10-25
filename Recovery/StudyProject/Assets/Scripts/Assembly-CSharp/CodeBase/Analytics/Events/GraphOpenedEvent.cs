using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class GraphOpenedEvent : UserContextEventBase
	{
		[JsonExtensionData]
		private readonly Dictionary<string, object> data;

		[JsonIgnore]
		public override string Name => "graph";

		public GraphOpenedEvent()
		{
			// Original ELF VA 0x15aea60: the empty nested payload is retained.
			data = new Dictionary<string, object> { ["open"] = new Dictionary<string, object>() };
		}
	}
}
