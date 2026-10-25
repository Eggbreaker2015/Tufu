using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class BossTowerOpenedEvent : UserContextEventBase
	{
		[JsonExtensionData]
		private readonly Dictionary<string, object> data;

		// Native ELF VA 0x15ad520.
		public BossTowerOpenedEvent() { data = new Dictionary<string,object> { ["opened"] = new Dictionary<string,object>() }; }

		[JsonIgnore]
		public override string Name => "boss_tower"; // Native ELF VA 0x15ad4e0.
	}
}
