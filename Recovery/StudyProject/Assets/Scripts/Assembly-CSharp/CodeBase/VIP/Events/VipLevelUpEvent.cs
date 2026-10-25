using Codebase.Analytics;
using Newtonsoft.Json;

namespace Codebase.VIP.Events
{
	public class VipLevelUpEvent : UserContextEventBase
	{
		[JsonIgnore]
		public override string Name => null;

		public int Level { get; set; }

		public int TotalExperience { get; set; }

		public VipLevelUpEvent()
		{
		}

		public VipLevelUpEvent(int level, int totalExperience)
		{
		}
	}
}
