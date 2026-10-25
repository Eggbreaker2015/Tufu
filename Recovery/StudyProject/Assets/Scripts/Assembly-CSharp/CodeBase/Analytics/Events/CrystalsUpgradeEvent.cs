using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class CrystalsUpgradeEvent : UserContextEventBase
	{
		[JsonExtensionData]
		private readonly Dictionary<string, object> data;

		[JsonIgnore]
		public override string Name => "crystals"; // Original ELF 0x15ae100.

		public CrystalsUpgradeEvent(string upgradeName)
		{
			// Original ELF 0x15ae140; local payload construction does not send telemetry.
			data = new Dictionary<string, object>
			{
				["upgrade"] = new Dictionary<string, object> { ["name"] = upgradeName }
			};
		}
	}
}
