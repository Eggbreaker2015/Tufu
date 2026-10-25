using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class GraphUpgradeEvent : UserContextEventBase
	{
		[JsonExtensionData]
		private readonly Dictionary<string, object> data;

		[JsonIgnore]
		public override string Name => "graph";

		public GraphUpgradeEvent(string upgradeName)
		{
			// Original ELF VA 0x15aeb7c.
			data = new Dictionary<string, object>
			{
				["upgrade"] = new Dictionary<string, object> { ["name"] = upgradeName }
			};
		}
	}
}
