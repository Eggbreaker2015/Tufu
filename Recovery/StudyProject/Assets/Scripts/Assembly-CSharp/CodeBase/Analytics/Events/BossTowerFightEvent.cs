using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class BossTowerFightEvent : UserContextEventBase
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

		public BossTowerFightEvent(string result, string bossName)
		{
			data = new Dictionary<string, object>
			{
				["fight"] = new[]
				{
					new Dictionary<string, object>
					{
						["result"] = result,
						["boss"] = bossName
					}
				}
			};
		}
	}
}
