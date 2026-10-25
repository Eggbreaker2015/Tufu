using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class RewardedEvent : BaseAnalyticsEvent
	{
		[JsonExtensionData]
		private readonly Dictionary<string, object> data;

		[JsonIgnore]
		public override string Name => null;

		public RewardedEvent(string stateName, string key = "")
		{
		}
	}
}
