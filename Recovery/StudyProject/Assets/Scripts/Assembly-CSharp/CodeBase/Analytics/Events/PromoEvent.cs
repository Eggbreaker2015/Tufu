using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class PromoEvent : UserContextEventBase
	{
		[JsonProperty("code")]
		public string Code { get; }

		[JsonProperty("result")]
		public string Result { get; }

		[JsonIgnore]
		public override string Name => null;

		public PromoEvent(string code, bool success)
		{
		}
	}
}
