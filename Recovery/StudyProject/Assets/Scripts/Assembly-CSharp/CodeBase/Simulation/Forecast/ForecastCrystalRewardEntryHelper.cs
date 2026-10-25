using System.Collections.Generic;
using Codebase.Player.Inventory;
using Codebase.Rewards;

namespace Codebase.Simulation.Forecast
{
    public static class ForecastCrystalRewardEntryHelper
    {
        // Original ARM64 ELF VA 0x13f69ac and 0x13f6ab4.
        public static List<RewardBase> ToRewards(List<ForecastCrystalRewardEntry> entries, ItemsProvider itemsProvider)
        {
            var result = new List<RewardBase>(entries.Count);
            foreach (var entry in entries) result.Add(ToReward(entry, itemsProvider));
            return result;
        }
        public static RewardBase ToReward(ForecastCrystalRewardEntry entry, ItemsProvider itemsProvider)
        {
            switch (entry.Kind)
            {
                case ForecastCrystalRewardKind.Stat:
                    return new StatReward { Type = entry.StatType, Value = (float)entry.Value, CustomModifier = entry.CustomModifier, Name = entry.Name, Source = entry.Source, ModifierType = entry.ModifierType };
                case ForecastCrystalRewardKind.EventCurrency:
                    return new EventCurrencyReward { Key = entry.Key, Amount = entry.Amount };
                case ForecastCrystalRewardKind.Chest:
                    return new ChestReward { Key = entry.Key, Amount = entry.Amount };
                case ForecastCrystalRewardKind.WeaponTier:
                case ForecastCrystalRewardKind.ShieldTier:
                case ForecastCrystalRewardKind.ArmorTier:
                    bool mythical = entry.Tier - 1 == 6;
                    return new ItemReward
                    {
                        IsRandom = !mythical,
                        Rarity = (Rarity)(entry.Tier - 1),
                        SlotType = entry.Kind == ForecastCrystalRewardKind.WeaponTier ? ItemSlotType.Weapon : entry.Kind == ForecastCrystalRewardKind.ShieldTier ? ItemSlotType.Shield : ItemSlotType.Helmet,
                        Def = mythical ? itemsProvider.Get(entry.Key) : null,
                        Level = entry.Amount,
                        BakedData = mythical ? null : new BakedItemReward { ItemKey = entry.Key }
                    };
                default:
                    return new EventReward { ExtraDurationMinutes = entry.Amount };
            }
        }
    }
}
