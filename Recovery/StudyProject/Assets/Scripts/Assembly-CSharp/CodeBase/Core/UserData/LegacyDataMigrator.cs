using System;
using System.Collections.Generic;
using Codebase.ABTests;
using Codebase.Analytics;
using Codebase.Cashback;
using Codebase.Events.Analytics;
using Codebase.Events.Models;
using Codebase.Gates;
using Codebase.Mail;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Rewards;
using Codebase.Shop;
using Codebase.Unit;
using Codebase.VIP;

namespace Codebase.Core.UserData
{
	public class LegacyDataMigrator
	{
		[Serializable]
		private class BoughtProductsWrapper
		{
			public List<PurchaseDataLegacy> Data;
		}

		[Serializable]
		private class PurchaseDataLegacy
		{
			public string productId;

			public string receipt;

			public string purchaseToken;

			public string transactionId;
		}

		[Serializable]
		private class GatesListWrapper
		{
			public List<GateDataLegacy> Data;
		}

		[Serializable]
		private class GateDataLegacy
		{
			public string Key;

			public bool Unlocked;
		}

		[Serializable]
		private class CashbackStatsWrapper
		{
			public List<CashbackStatEntryLegacy> Data;
		}

		[Serializable]
		private class CashbackStatEntryLegacy
		{
			public StatType Type;

			public double Value;
		}

		[Serializable]
		private class ItemLegacy
		{
			public string Key;

			public int Level;
		}

		[Serializable]
		private class ItemSlotLegacy
		{
			public string Key;

			public string SlotKey;

			public ItemSlotType SlotType;

			public bool IsUnlocked;

			public bool IsPinned;
		}

		[Serializable]
		private class StatLegacy
		{
			public double BaseValue;

			public StatType StatType;

			public List<StatModifier> Modifiers;
		}

		[Serializable]
		private class ABTestSaveData
		{
			public List<ABTestSaveEntry> tests;
		}

		[Serializable]
		private class ABTestSaveEntry
		{
			public string name;

			public string variant;
		}

		public void Migrate(GameUserData data)
		{
		}

		private void MigratePlayer(PlayerUd player)
		{
		}

		private void MigratePlayerItems(PlayerUd player)
		{
		}

		private void MigratePlayerItemSlots(PlayerUd player)
		{
		}

		private void MigratePlayerStats(PlayerUd player)
		{
		}

		private void MigratePlayerPosition(PlayerUd player)
		{
		}

		private void MigrateUserSettings(UserDataUd settings)
		{
		}

		private void MigrateBoughtProducts(UserDataUd data)
		{
		}

		private void MigrateVip(VipUd vip)
		{
		}

		private void MigrateEvents(EventsUd events)
		{
		}

		private void MigrateEventCurrencies(EventsUd events)
		{
		}

		private void MigrateMails(MailRepoUd mailRepo)
		{
		}

		private void MigrateShop(ShopUd shop)
		{
		}

		private void MigrateGates(GatesUd gates)
		{
		}

		private void MigrateCashback(CashbackUd cashback)
		{
		}

		private void MigrateEventAnalytics(EventAnalyticsUd eventAnalytics)
		{
		}

		private static int GetInt(string key, int defaultValue)
		{
			return 0;
		}

		private static float GetFloat(string key, float defaultValue)
		{
			return 0f;
		}

		private static string GetString(string key, string defaultValue)
		{
			return null;
		}

		private static bool GetBool(string key, bool defaultValue)
		{
			return false;
		}

		private static double GetDouble(string key, double defaultValue)
		{
			return 0.0;
		}

		private void MigrateABTests(ABTestUd abTests)
		{
		}

		private void MigrateBakedRewards(BakedRewardsUd bakedRewards)
		{
		}
	}
}
