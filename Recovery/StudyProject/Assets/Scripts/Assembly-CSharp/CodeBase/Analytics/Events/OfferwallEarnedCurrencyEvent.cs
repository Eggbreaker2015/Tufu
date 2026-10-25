using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class OfferwallEarnedCurrencyEvent : UserContextEventBase
	{
		[JsonExtensionData]
		private readonly Dictionary<string, object> data;

		[JsonIgnore]
		public override string Name => null;

		public OfferwallEarnedCurrencyEvent(int balance, int earned)
		{
		}
	}
}
