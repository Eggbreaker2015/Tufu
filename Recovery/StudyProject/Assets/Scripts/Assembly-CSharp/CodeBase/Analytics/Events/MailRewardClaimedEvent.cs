using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class MailRewardClaimedEvent : UserContextEventBase
	{
		[JsonProperty("mail_key")]
		public string MailKey { get; }

		[JsonProperty("gems")]
		public int Gems { get; }

		[JsonIgnore]
		public override string Name => null;

		public MailRewardClaimedEvent(string mailKey, int gems)
		{
		}
	}
}
