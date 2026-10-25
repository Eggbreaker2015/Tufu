using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class MailViewedEvent : UserContextEventBase
	{
		[JsonProperty("mail_key")]
		public string MailKey { get; }

		[JsonIgnore]
		public override string Name => null;

		public MailViewedEvent(string mailKey)
		{
		}
	}
}
