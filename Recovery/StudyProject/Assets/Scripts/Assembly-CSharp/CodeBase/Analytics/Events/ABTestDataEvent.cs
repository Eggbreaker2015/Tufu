using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class ABTestDataEvent : BaseAnalyticsEvent
	{
		[JsonExtensionData]
		private readonly Dictionary<string, object> data;

		[JsonIgnore]
		public override string Name => null;

		public ABTestDataEvent(int revision, string abDataJson)
		{
		}

		public ABTestDataEvent(string errorKey)
		{
		}
	}
}
