using System.Collections.Generic;
using Codebase.Crystals;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class CrystalEquipEvent : UserContextEventBase
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

		public CrystalEquipEvent(CrystalItemModel crystal, string slotKey)
		{
			data = new Dictionary<string, object>
			{
				["equip"] = new Dictionary<string, object>
				{
					["slot"] = slotKey,
					["rarity"] = crystal.Rarity.ToString(),
					["level"] = crystal.Level,
					["enchant_level"] = crystal.EnchantLevel
				}
			};
		}
	}
}
