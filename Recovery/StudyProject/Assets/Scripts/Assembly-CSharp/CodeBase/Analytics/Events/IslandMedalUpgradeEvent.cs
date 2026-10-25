using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class IslandMedalUpgradeEvent : UserContextEventBase
	{
		[JsonIgnore]
		public override string Name => null;

		[JsonProperty("location_number")]
		public int LocationNumber { get; }

		[JsonProperty("previous_level")]
		public int PreviousLevel { get; }

		[JsonProperty("level")]
		public int Level { get; }

		public IslandMedalUpgradeEvent(int locationNumber, int previousLevel, int level)
		{
		}
	}
}
