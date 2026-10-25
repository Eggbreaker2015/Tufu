using System.Collections.Generic;
using Codebase.Crystals;
using Newtonsoft.Json;

namespace Codebase.Analytics.Events
{
	public class CrystalEnchantAttemptEvent : UserContextEventBase
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

		public CrystalEnchantAttemptEvent(CrystalItemModel crystal, EnchantAttempt attempt, bool guaranteed)
		{
			data = new Dictionary<string, object>
			{
				["enchant_attempt"] = new Dictionary<string, object>
				{
					["result"] = attempt.IsSuccess ? "success" : "failure",
					["rarity"] = crystal.Rarity.ToString(),
					["level"] = crystal.Level,
					["prev_enchant_level"] = attempt.PrevLevel,
					["new_enchant_level"] = attempt.NewLevel,
					["success_chance"] = attempt.SuccessChance,
					["guaranteed"] = guaranteed,
					["dust_cashback"] = attempt.DustCashback
				}
			};
		}
	}
}
