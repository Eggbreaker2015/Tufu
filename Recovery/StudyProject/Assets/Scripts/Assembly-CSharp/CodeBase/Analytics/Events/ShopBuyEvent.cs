using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class ShopBuyEvent : UserContextEventBase
	{
		[JsonExtensionData]
		private readonly Dictionary<string, object> data;

		[JsonIgnore]
		public override string Name => null;

		public ShopBuyEvent(string key, double amount, int playTime)
		{
		}
	}
}
