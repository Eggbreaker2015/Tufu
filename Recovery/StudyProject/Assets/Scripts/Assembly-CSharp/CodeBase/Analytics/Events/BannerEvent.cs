using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class BannerEvent : BaseAnalyticsEvent
	{
		[JsonExtensionData]
		private readonly Dictionary<string, object> data;

		[JsonIgnore]
		public override string Name => null;

		public BannerEvent(string stateName, string key = "")
		{
		}
	}
}
