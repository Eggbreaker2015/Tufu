using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class AmuletLevelUpEvent : UserContextEventBase
	{
		[JsonIgnore]
		public override string Name => "amulet_level_up";

		[JsonProperty("level")]
		public int Level { get; }

		public AmuletLevelUpEvent(int level)
		{
// Native ELF VA 0x15ad084 after original base constructor.
            Level = level;
		}
	}
}
