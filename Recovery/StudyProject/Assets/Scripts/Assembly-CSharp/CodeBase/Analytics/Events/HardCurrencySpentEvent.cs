using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class HardCurrencySpentEvent : UserContextEventBase
	{
		[JsonIgnore]
		public override string Name => null;

		[JsonProperty("currency")]
		public string Currency { get; }

		[JsonProperty("source")]
		public string Source { get; }

		[JsonProperty("amount")]
		public double Amount { get; }

		[JsonProperty("details")]
		public Dictionary<string, object> Details { get; }

		public HardCurrencySpentEvent(string source, double amount, Dictionary<string, object> details = null)
		{
		}
	}
}
