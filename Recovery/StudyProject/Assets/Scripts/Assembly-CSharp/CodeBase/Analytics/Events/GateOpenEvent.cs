using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class GateOpenEvent : UserContextEventBase
	{
		[JsonExtensionData]
		private readonly Dictionary<string, object> data;

		[JsonProperty("opened_location_number")]
		public string LocationNumber { get; }

		[JsonIgnore]
		public override string Name => null;

		public GateOpenEvent(string locationNumber, string key)
		{
		}
	}
}
