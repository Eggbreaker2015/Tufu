using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class BossTowerRaidEvent : UserContextEventBase
	{
		[JsonExtensionData]
		private readonly Dictionary<string, object> data;
		[JsonIgnore]
		public override string Name
		{
			get
			{
				return "boss_tower";
			}
		}

		public BossTowerRaidEvent(string bossName)
		{
			data = new Dictionary<string, object>
			{
				["raid"] = new Dictionary<string, object>
				{
					["boss"] = bossName
				}
			};
		}
	}
}
