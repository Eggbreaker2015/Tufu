using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class OfferwallCurrencyInfoEvent : UserContextEventBase
	{
		[JsonExtensionData]
		private readonly Dictionary<string, object> data;

		[JsonIgnore]
		public override string Name => null;

		public OfferwallCurrencyInfoEvent(int balance)
		{
		}
	}
}
