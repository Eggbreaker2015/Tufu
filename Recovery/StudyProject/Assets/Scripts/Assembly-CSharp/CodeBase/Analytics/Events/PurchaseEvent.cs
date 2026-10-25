using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class PurchaseEvent : UserContextEventBase
	{
		[JsonExtensionData]
		private readonly Dictionary<string, object> data;

		[JsonProperty("key")]
		public string Key { get; }

		[JsonIgnore]
		public override string Name => null;

		public PurchaseEvent(string key, string transaction, string productId)
		{
		}
	}
}
