using Codebase.Amulet;
using Codebase.Core;
using Codebase.Economy;
using Codebase.Events;
using Codebase.In_apps;
using Codebase.Player;
using Codebase.Player.Inventory;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Rewards
{
	public class RewardIconResolver
	{
		[Inject]
		private IconStyleConfig iconStyleConfig;

		[Inject]
		private ChestsConfig chestsConfig;

		[Inject]
		private EventStyleConfig eventStyleConfig;

		[Inject]
		private ItemsProvider itemsProvider;

		[Inject]
		private EventsProvider eventsProvider;

		[Inject]
		private IAPManager iapManager;

		[Inject]
		private CurrencyStyleConfig currencyStyleConfig;

		[Inject]
		private SoulStyleConfig soulStyleConfig;

		public Sprite Resolve(RewardBase reward)
		{
			return null;
		}

		public string FormatName(RewardBase reward)
		{
			return null;
		}

		public string FormatAmount(RewardBase reward)
		{
			return null;
		}

		private static StatType GetStatType(StatReward stat)
		{
			return StatType.MovementSpeed;
		}

		private string FormatCurrencyName(string key)
		{
			return null;
		}

		private static string FormatItemSlotAmount(string slotKey)
		{
			return null;
		}

		private Sprite ResolveStat(StatReward stat)
		{
			return null;
		}

		private Sprite ResolveChest(ChestReward chest)
		{
			return null;
		}

		private Sprite ResolveItem(ItemReward item)
		{
			return null;
		}

		private string FormatStatAmount(StatReward stat)
		{
			return null;
		}

		private Sprite ResolveEventCurrency(EventCurrencyReward ec)
		{
			return null;
		}

		private Sprite ResolveBoosterToken(string storeId)
		{
			return null;
		}
	}
}
