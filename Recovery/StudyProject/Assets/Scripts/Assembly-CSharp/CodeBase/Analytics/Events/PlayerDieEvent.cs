using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class PlayerDieEvent : BaseAnalyticsEvent
	{
		[JsonProperty("enemy")]
		public string Enemy { get; }

		[JsonProperty("location_number")]
		public int LocationNumber { get; }

		[JsonIgnore]
		public override string Name => "player_die";

		public PlayerDieEvent(string enemy, int locationNumber)
		{
// Native ELF VA 0x15b036c; event name resolves from original metadata storage 0x2b27370.
            Enemy = enemy;
            LocationNumber = locationNumber;
		}
	}
}
