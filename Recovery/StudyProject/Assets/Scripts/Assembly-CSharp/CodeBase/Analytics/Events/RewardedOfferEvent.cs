using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class RewardedOfferEvent : UserContextEventBase
	{
		[JsonExtensionData]
		private readonly Dictionary<string, object> data;

		[JsonIgnore]
		public override string Name => null;

		public RewardedOfferEvent(string action, string statType, int lotIndex = -1, double gemCost = 0.0, double value = 0.0)
		{
		}
	}
}
