using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class CrystalsTormentorVictoryEvent : UserContextEventBase
	{
		[JsonExtensionData]
		private readonly Dictionary<string, object> data;
		[JsonIgnore]
		public override string Name
		{
			get
			{
				return "crystals";
			}
		}

		public CrystalsTormentorVictoryEvent(int tormentorNumber)
		{
			data = new System.Collections.Generic.Dictionary<string, object>
			{
				["tormentor_victory"] = new System.Collections.Generic.Dictionary<string, object>
				{
					["number"] = tormentorNumber
				}
			};
		}
	}
}
