using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class ABTestDefaultEvent : BaseAnalyticsEvent
	{
		[JsonExtensionData]
		private readonly Dictionary<string, object> data;

		[JsonIgnore]
		public override string Name => null;

		public ABTestDefaultEvent(string key, string variantList, string variant)
		{
		}
	}
}
