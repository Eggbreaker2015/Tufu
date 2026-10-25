using Codebase.Core;
using Codebase.Events.Models;
namespace Codebase.Events
{
	public static class EventBalance
	{
		public static double GetCurrencyValue(string currencyKey)
		{
			// Native ELF VA 0x14d8c84; literal keys and 0.02 read from the original ELF.
			EventModel current = GameContext.Get<EventsRepository>().CurrentEvent;
			if (current == null) return 1.0;
			double amount = current.GetCurrency(currencyKey);
			if (current.Key == "crystal_event_roster" || current.Difficulty == 0) amount += amount;
			if (currencyKey == "red_crystal") return amount * 0.02 + 1.0;
			if (currencyKey == "blue_crystal") return 1.0 - GetIncomingDamageMultiplier(amount);
			return 1.0;
		}

		public static double GetPlayerDamageMultiplier()
		{
			// Native ELF VA 0x14d8e14.
			return GetCurrencyValue("red_crystal");
		}

		public static double GetEnemyDamageMultiplier()
		{
			// Native ELF VA 0x14d8e54.
			return 1.0 - GetCurrencyValue("blue_crystal");
		}

		private static double GetIncomingDamageMultiplier(double blueCrystals)
		{
			// Native ELF VA 0x14d8df8.
			return 1.0 / (blueCrystals * 0.02 + 1.0);
		}
	}
}
