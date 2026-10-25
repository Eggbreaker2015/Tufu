using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class SaveMigrationEvent : UserContextEventBase
	{
		[JsonProperty("from")]
		public string Version { get; }

		[JsonIgnore]
		public override string Name => null;

		public SaveMigrationEvent(string version)
		{
		}
	}
}
