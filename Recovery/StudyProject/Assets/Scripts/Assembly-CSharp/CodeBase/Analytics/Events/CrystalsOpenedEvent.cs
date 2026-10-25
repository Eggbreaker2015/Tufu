using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class CrystalsOpenedEvent : UserContextEventBase
	{
		[JsonExtensionData]
		private readonly Dictionary<string, object> data;

		[JsonIgnore]
		public override string Name => "crystals"; // Native ELF0x15ade74.
        // Native ELF0x15adeb4; original local extension data only.
        public CrystalsOpenedEvent() { data = new Dictionary<string, object> { ["open"] = new Dictionary<string, object>() }; }
	}
}
