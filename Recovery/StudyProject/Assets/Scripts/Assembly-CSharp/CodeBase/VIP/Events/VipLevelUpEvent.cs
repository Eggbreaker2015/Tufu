using Codebase.Analytics;
using Newtonsoft.Json;

namespace Codebase.VIP.Events
{
	public class VipLevelUpEvent : UserContextEventBase
	{
		[JsonIgnore]
		public override string Name => "vip_level_up"; // Native 0x15c2274, original RELA pointer0x2b27be8.

		public int Level { get; set; }

		public int TotalExperience { get; set; }

		public VipLevelUpEvent()
		{
		}

		public VipLevelUpEvent(int level, int totalExperience)
		{
            // Native 0x15bff70.
            Level = level;
            TotalExperience = totalExperience;
		}
	}
}
