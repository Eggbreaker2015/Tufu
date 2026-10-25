using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class RewardedPaidEvent : UserContextEventBase
	{
		[JsonIgnore]
		public override string Name => null;
	}
}
