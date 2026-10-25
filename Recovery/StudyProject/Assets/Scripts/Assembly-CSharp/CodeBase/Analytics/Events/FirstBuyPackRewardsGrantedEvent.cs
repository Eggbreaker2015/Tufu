using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class FirstBuyPackRewardsGrantedEvent : UserContextEventBase
	{
		[JsonIgnore]
		public override string Name => null;
	}
}
