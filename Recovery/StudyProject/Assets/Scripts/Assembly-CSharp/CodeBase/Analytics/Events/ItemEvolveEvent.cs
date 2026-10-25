using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class ItemEvolveEvent : UserContextEventBase
	{
		[JsonProperty("item_key")]
		public string Key { get; }

		[JsonIgnore]
		public override string Name => "evolve_item"; // Native 0x15aef04.

		public ItemEvolveEvent(string key)
		{
			Key = key; // Native 0x15aef44, after original UserContextEventBase construction.
		}
	}
}
