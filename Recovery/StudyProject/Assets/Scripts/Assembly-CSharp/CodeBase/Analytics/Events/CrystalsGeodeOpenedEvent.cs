using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class CrystalsGeodeOpenedEvent : UserContextEventBase
	{
		[JsonExtensionData]
		private readonly Dictionary<string, object> data;

		[JsonIgnore]
		public override string Name => "crystals"; // Native ELF0x15add58.
        // Native ELF0x15add98: original extension-data nesting, no emitted network request.
        public CrystalsGeodeOpenedEvent() { data = new Dictionary<string, object> { ["geode_open"] = new Dictionary<string, object>() }; }
	}
}
