using System.Linq;
using Codebase.Amulet;
using Codebase.Core;
using Codebase.Economy;
using Codebase.Events;
using Codebase.In_apps;
using Codebase.Player;
using Codebase.Player.Inventory;
using Codebase.Unit;
using Codebase.Utilities;
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
			// Original APK ARM64 0x1418170: use the authored style for each reward type.
			switch (reward)
			{
				case StatReward stat: return ResolveStat(stat);
				case ChestReward chest: return ResolveChest(chest);
				case ItemReward item: return ResolveItem(item);
				case GemsReward _: return iconStyleConfig.GemsSprite;
				case EventCurrencyReward currency: return ResolveEventCurrency(currency);
				case EventReward _: return eventStyleConfig.TimeSprite;
				case VipPointsReward _: return iconStyleConfig.VipPointsSprite;
				case NoAdsReward _: return iconStyleConfig.NoAdsSprite;
				case LevelPassReward _: return iconStyleConfig.LevelPassSprite;
				case SoulPassReward _: return iconStyleConfig.SoulPassSprite;
				case CrystalPassReward _: return iconStyleConfig.CrystalPassSprite;
				case ChaoticSoulOfferReward _: return iconStyleConfig.ChaoticSoulOfferSprite;
				case FreeBoosterTokenReward booster: return ResolveBoosterToken(booster.productId);
				case CurrencyReward currency: return currencyStyleConfig.GetIcon(currency.Key);
				case SoulReward soul: return soulStyleConfig.GetIcon(soul.SoulKey);
				case ItemSlotReward _: return iconStyleConfig.ItemSlotUnlockSprite;
				case MineUnlockReward _: return iconStyleConfig.MineUnlockSprite;
				default: return null;
			}
		}

		public string FormatName(RewardBase reward)
		{
			// Original APK ARM64 0x1418d28.
			switch (reward)
			{
				case ItemSlotReward _: return LocalizationStrings.Get("QUEST_REWARD_UNLOCKS");
				case MineUnlockReward _: return LocalizationStrings.Get("QUEST_REWARD_FEATURE_UNLOCK");
				case StatReward stat: return LocalizationStrings.Get("STAT_" + GetStatType(stat));
				case GemsReward _: return LocalizationStrings.Get("CURRENCY_Gems");
				case CurrencyReward currency: return FormatCurrencyName(currency.Key);
				case SoulReward soul: return LocalizationStrings.Get("SOUL_NAME_" + soul.SoulKey);
				case EventCurrencyReward currency: return LocalizationStrings.Get("EVENT_CURRENCY_NAME_" + currency.Key);
				case ChestReward chest: return LocalizationStrings.Get("CHEST_" + chest.Key);
				default: return string.Empty;
			}
		}

		public string FormatAmount(RewardBase reward)
		{
			// Original APK ARM64 0x14192f0. Unlock labels occupy the amount line.
			switch (reward)
			{
				case ItemSlotReward slot: return FormatItemSlotAmount(slot.SlotKey);
				case MineUnlockReward _: return LocalizationStrings.Get("QUEST_REWARD_DEEP_MINES");
				case StatReward stat: return FormatStatAmount(stat);
				case ChestReward chest: return "x" + chest.Amount;
				case ItemReward item: return "Lv." + item.Level;
				case GemsReward gems: return "+" + gems.Gems.Format();
				case EventCurrencyReward currency: return "+" + currency.Amount.Format();
				case EventReward duration: return "+" + duration.ExtraDurationMinutes + "m";
				case VipPointsReward vip: return "+" + vip.Amount;
				case CurrencyReward currency: return "+" + currency.Amount.Format();
				case SoulReward soul: return "x" + soul.Amount;
				default: return string.Empty;
			}
		}

		private static StatType GetStatType(StatReward stat)
		{
			// Original APK ARM64 0x1419058: baked and daily-rotation types take precedence.
			return stat.BakedData != null ? stat.BakedData.Type : stat.GetResolvedType() ?? stat.Type;
		}

		private string FormatCurrencyName(string key)
		{
			// Original APK ARM64 0x1419194.
			if (key.StartsWith("quest."))
				return LocalizationStrings.Get("QUEST_CURRENCY_" + key.Substring("quest.".Length).ToUpperInvariant());
			return LocalizationStrings.Get("CURRENCY_" + char.ToUpperInvariant(key[0]) + key.Substring(1));
		}

		private static string FormatItemSlotAmount(string slotKey)
		{
			// Original APK ARM64 0x1419820 retains a separate label for the third slot.
			return LocalizationStrings.Get(slotKey == "weapon_3"
				? "QUEST_REWARD_WEAPON_SLOT_3"
				: "QUEST_REWARD_ITEM_SLOT_" + slotKey.ToUpperInvariant());
		}

		private Sprite ResolveStat(StatReward stat)
		{
			// Original APK ARM64 0x14186a8 / predicate 0x1419d40.
			StatType type = GetStatType(stat).GetIconStatType();
			return iconStyleConfig.Styles.FirstOrDefault(style => style.StatType == type)?.Sprite;
		}

		private Sprite ResolveChest(ChestReward chest)
		{
			// Original APK ARM64 0x14187a8.
			return chestsConfig.Get(chest.Key)?.ClosedSprite;
		}

		private Sprite ResolveItem(ItemReward item)
		{
			// Original APK ARM64 0x14187f0.
			ItemConfig config = item.BakedData != null ? itemsProvider.Get(item.BakedData.ItemKey) : item.Def;
			return config != null ? config.Icon : null;
		}

		private string FormatStatAmount(StatReward stat)
		{
			// Original APK ARM64 0x14198d0: only explicit percentage modifiers add a percent sign.
			double value = stat.Value;
			if (stat.CustomModifier)
			{
				int modifier = (int)stat.ModifierType;
				if ((uint)modifier <= 7 && ((1 << modifier) & 0x36) != 0)
					return "+" + (value * 100.0).Format() + "%";
				if (stat.ModifierType == ModifierType.EventPercentModifier)
					return "+" + value.Format() + "%";
			}
			return "+" + value.Format();
		}

		private Sprite ResolveEventCurrency(EventCurrencyReward ec)
		{
			// Original APK ARM64 0x1418834.
			return eventStyleConfig.GetCurrencyStyle(ec.Key)?.Sprite;
		}

		private Sprite ResolveBoosterToken(string storeId)
		{
			// Original APK ARM64 0x1418864.
			if (string.IsNullOrEmpty(storeId)) return null;
			foreach (EventConfig config in eventsProvider.Events)
			{
				if (config.Boosters == null) continue;
				foreach (EventBoosterDef booster in config.Boosters)
					if (booster.Type == EventBoosterType.IAP && booster.ProductKey != 0
						&& iapManager.GetStoreId(booster.ProductKey) == storeId)
						return booster.TokenSprite;
			}
			return null;
		}
	}
}
