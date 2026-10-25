using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class ABTestRemoteEvent : BaseAnalyticsEvent
	{
		[JsonExtensionData]
		private readonly Dictionary<string, object> data;

		[JsonIgnore]
		public override string Name => null;

		public ABTestRemoteEvent(string key, string variantList, string variant)
		{
		}
	}
}
