using System;
using System.Collections.Generic;
using Codebase.Meta.Upgrades;
using Codebase.Unit;

namespace Codebase.Amulet.Upgrades
{
    public static class AmuletUpgradeDefsBuilder
    {
        // Generated from bounded original ARM64 constants/calls and 628 original scalar delegates.
        // Source provenance: Evidence/amulet-original-definition-calls.json and amulet-original-lambda-expressions.json.
        public static List<UpgradeDef> Build()
        {
            var definitions = new List<UpgradeDef>(298);
            // Original constructor call 0x1582d04
            definitions.Add(new UpgradeDef("graph_souls_drop_unlock_common_1_1")
                .WithMaxLevel(1)
                .WithCost("soul.huge", 1.0d)
                .WithModifier("souls_drop_unlock", (ModifierType)0, 1.0d, "common"));
            // Original constructor call 0x1582db4
            definitions.Add(new UpgradeDef("graph_health_flat_1_2")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b63f4)
                .WithStatModifier((StatType)1, (ModifierType)0, Native_15b63fc, 0)
                .WithSimulationSettings(10, 0.30000001192092896f, 17, 0.20000000298023224f));
            // Original constructor call 0x1582f80
            definitions.Add(new UpgradeDef("graph_cut_damage_flat_1_3")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b6404)
                .WithStatModifier((StatType)8, (ModifierType)0, Native_15b640c, 0)
                .WithSimulationSettings(7, 1.0f, 19, 0.30000001192092896f));
            // Original constructor call 0x1583134
            definitions.Add(new UpgradeDef("graph_cut_damage_percent_1_4")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b6414)
                .WithStatModifier((StatType)9, (ModifierType)4, Native_15b641c, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6430)
                .WithSimulationSettings(3, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x158338c
            definitions.Add(new UpgradeDef("graph_movement_speed_percent_1_5")
                .WithMaxLevel(3)
                .WithCost("soul.large", Native_15b6440)
                .WithStatModifier((StatType)0, (ModifierType)5, Native_15b6448, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b645c)
                .WithSimulationSettings(1, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x15835dc
            definitions.Add(new UpgradeDef("graph_chest_level_1_6")
                .WithMaxLevel(1)
                .WithCost("soul.common", Native_15b646c)
                .WithModifier("chest_level", (ModifierType)0, Native_15b6474, 0)
                .WithSimulationSettings(1, 0.10000000149011612f, 16, 0.30000001192092896f));
            // Original constructor call 0x15837a4
            definitions.Add(new UpgradeDef("graph_souls_drop_common_amount_1_7")
                .WithMaxLevel(2)
                .WithCost("soul.common", Native_15b647c)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b64d8, "common")
                .WithSimulationSettings(2, 0.30000001192092896f, 1, 1.0f));
            // Original constructor call 0x1583964
            definitions.Add(new UpgradeDef("graph_souls_drop_common_amount_1_8")
                .WithMaxLevel(1)
                .WithCost("soul.great", 3.0d)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b64e0, "common")
                .WithSimulationSettings(1, 0.30000001192092896f, 1, 1.0f));
            // Original constructor call 0x1583aa8
            definitions.Add(new UpgradeDef("graph_souls_drop_unlock_trees_1_9")
                .WithMaxLevel(1)
                .WithCost("soul.chaotic", 1.0d)
                .WithModifier("feature_unlock", (ModifierType)0, 1.0d, "static_souls_drop"));
            // Original constructor call 0x1583b4c
            definitions.Add(new UpgradeDef("graph_cut_damage_flat_1_10")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b64e8)
                .WithStatModifier((StatType)8, (ModifierType)0, Native_15b64f0, 0)
                .WithSimulationSettings(10, 0.30000001192092896f, 17, 0.25f));
            // Original constructor call 0x1583d00
            definitions.Add(new UpgradeDef("graph_items_drop_common_1_11")
                .WithMaxLevel(1)
                .WithCost("soul.huge", 1.0d)
                .WithModifier("items_rarity_drop_amount", (ModifierType)0, Native_15b64f8, 1)
                .WithSimulationSettings(2, 0.30000001192092896f, 1, 1.0f));
            // Original constructor call 0x1583e50
            definitions.Add(new UpgradeDef("graph_items_timer_common_1_12")
                .WithMaxLevel(2)
                .WithCost("soul.huge", Native_15b6500)
                .WithModifier("items_rarity_timer", (ModifierType)0, Native_15b650c, 1)
                .WithSimulationSettings(2, 0.5f, 1, 1.0f)
                .WithDisplay((UpgradeValueFormat)0, Native_15b651c));
            // Original constructor call 0x15840c0
            definitions.Add(new UpgradeDef("graph_items_charges_common_1_13")
                .WithMaxLevel(4)
                .WithCost("soul.large", Native_15b6528)
                .WithModifier("items_rarity_charges", (ModifierType)0, Native_15b6584, 1)
                .WithSimulationSettings(2, 1.0f, 1, 1.0f));
            // Original constructor call 0x158428c
            definitions.Add(new UpgradeDef("graph_items_drop_common_1_14")
                .WithMaxLevel(1)
                .WithCost("soul.huge", 3.0d)
                .WithModifier("items_rarity_drop_amount", (ModifierType)0, Native_15b658c, 1)
                .WithSimulationSettings(2, 0.30000001192092896f, 1, 1.0f));
            // Original constructor call 0x15843dc
            definitions.Add(new UpgradeDef("graph_health_flat_1_15")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b6594)
                .WithStatModifier((StatType)1, (ModifierType)0, Native_15b659c, 0)
                .WithSimulationSettings(7, 1.0f, 19, 0.30000001192092896f));
            // Original constructor call 0x1584590
            definitions.Add(new UpgradeDef("graph_health_percent_1_16")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b65a4)
                .WithStatModifier((StatType)1, (ModifierType)4, Native_15b65ac, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b65c0)
                .WithSimulationSettings(3, 1.0f, 17, 0.30000001192092896f));
            // Original constructor call 0x15847e0
            definitions.Add(new UpgradeDef("graph_souls_drop_unlock_uncommon_1_17")
                .WithMaxLevel(1)
                .WithCost("soul.huge", 2.0d)
                .WithModifier("souls_drop_unlock", (ModifierType)0, 1.0d, "large")
                .WithSimulationSettings(1, 0.30000001192092896f, 1, 1.0f));
            // Original constructor call 0x1584890
            definitions.Add(new UpgradeDef("graph_souls_drop_uncommon_amount_1_18")
                .WithMaxLevel(2)
                .WithCost("soul.large", Native_15b65d0)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b662c, "large")
                .WithSimulationSettings(1, 1.0f, 1, 1.0f));
            // Original constructor call 0x1584a50
            definitions.Add(new UpgradeDef("graph_souls_drop_common_amount_1_19")
                .WithMaxLevel(2)
                .WithCost("soul.large", Native_15b6634)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b6690, "common")
                .WithSimulationSettings(1, 1.0f, 1, 1.0f));
            // Original constructor call 0x1584c18
            definitions.Add(new UpgradeDef("graph_smash_defence_flat_1_20")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b6698)
                .WithStatModifier((StatType)20, (ModifierType)0, Native_15b66a0, 0)
                .WithSimulationSettings(10, 0.5f, 17, 2.0f));
            // Original constructor call 0x1584dd4
            definitions.Add(new UpgradeDef("graph_smash_defence_percent_1_21")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b66a8)
                .WithStatModifier((StatType)21, (ModifierType)4, Native_15b66b0, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b66c4)
                .WithSimulationSettings(3, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x1585030
            definitions.Add(new UpgradeDef("graph_smash_defence_flat_1_22")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b66d4)
                .WithStatModifier((StatType)20, (ModifierType)0, Native_15b66dc, 0)
                .WithSimulationSettings(7, 0.6000000238418579f, 19, 3.0f));
            // Original constructor call 0x15851f4
            definitions.Add(new UpgradeDef("graph_cut_damage_percent_1_23")
                .WithMaxLevel(10)
                .WithCost("soul.great", 1.0d)
                .WithStatModifier((StatType)9, (ModifierType)4, Native_15b66e4, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b66f8)
                .WithSimulationSettings(5, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x15853d0
            definitions.Add(new UpgradeDef("graph_health_percent_1_24")
                .WithMaxLevel(10)
                .WithCost("soul.great", 1.0d)
                .WithStatModifier((StatType)1, (ModifierType)4, Native_15b6708, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b671c)
                .WithSimulationSettings(5, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x15855ac
            definitions.Add(new UpgradeDef("graph_smash_defence_percent_1_25")
                .WithMaxLevel(10)
                .WithCost("soul.great", 1.0d)
                .WithStatModifier((StatType)21, (ModifierType)4, Native_15b672c, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6740)
                .WithSimulationSettings(5, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x1585788
            definitions.Add(new UpgradeDef("graph_loadout_button_1_26")
                .WithMaxLevel(1)
                .WithCost("soul.huge", 1.0d)
                .WithModifier("loadout_button", (ModifierType)0, 1.0d, 0));
            // Original constructor call 0x1585824
            definitions.Add(new UpgradeDef("graph_items_drop_common_1_27")
                .WithMaxLevel(1)
                .WithCost("soul.huge", 20.0d)
                .WithModifier("items_rarity_drop_amount", (ModifierType)0, Native_15b6750, 1)
                .WithSimulationSettings(2, 0.30000001192092896f, 1, 1.0f));
            // Original constructor call 0x1585978
            definitions.Add(new UpgradeDef("graph_souls_drop_unlock_rare_2_1")
                .WithMaxLevel(1)
                .WithCost("soul.huge", 2.0d)
                .WithModifier("souls_drop_unlock", (ModifierType)0, 1.0d, "elite"));
            // Original constructor call 0x1585a18
            definitions.Add(new UpgradeDef("graph_weak_spawner_extra_range_2_2")
                .WithMaxLevel(1)
                .WithCost("soul.elite", Native_15b6758)
                .WithModifier("weak_spawner_extra_range", (ModifierType)0, Native_15b6760, 1)
                .WithSimulationSettings(1, 0.10000000149011612f, 1, 1.0f));
            // Original constructor call 0x1585be0
            definitions.Add(new UpgradeDef("graph_map_spawner_timer_reduce_2_3")
                .WithMaxLevel(1)
                .WithCost("soul.large", Native_15b6768)
                .WithModifier("map_spawner_timer_reduce", (ModifierType)0, Native_15b6770, 1)
                .WithDisplay((UpgradeValueFormat)3, 1, null)
                .WithSimulationSettings(1, 0.05000000074505806f, 1, 1.0f));
            // Original constructor call 0x1585dc4
            definitions.Add(new UpgradeDef("graph_map_spawner_drop_island1_2_4")
                .WithMaxLevel(5)
                .WithCost("soul.common", Native_15b677c)
                .WithModifier("stat_drop", (ModifierType)0, Native_15b67d8, 1)
                .WithDisplay((UpgradeValueFormat)1, Native_15b67f0)
                .WithSimulationSettings(3, 1.0f, 1, 1.0f));
            // Original constructor call 0x158602c
            definitions.Add(new UpgradeDef("graph_map_spawner_drop_island2_2_5")
                .WithMaxLevel(5)
                .WithCost("soul.common", Native_15b6800)
                .WithModifier("stat_drop", (ModifierType)0, Native_15b685c, 2)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6874)
                .WithSimulationSettings(3, 1.0f, 1, 1.0f));
            // Original constructor call 0x1586294
            definitions.Add(new UpgradeDef("graph_map_spawner_drop_island3_2_6")
                .WithMaxLevel(5)
                .WithCost("soul.common", Native_15b6884)
                .WithModifier("stat_drop", (ModifierType)0, Native_15b68e0, 3)
                .WithDisplay((UpgradeValueFormat)1, Native_15b68f8)
                .WithSimulationSettings(3, 1.0f, 1, 1.0f));
            // Original constructor call 0x15864fc
            definitions.Add(new UpgradeDef("graph_souls_drop_epic_amount_2_7")
                .WithMaxLevel(1)
                .WithCost("soul.chaotic", 1.0d)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b6908, "huge"));
            // Original constructor call 0x1586630
            definitions.Add(new UpgradeDef("graph_souls_drop_uncommon_amount_2_8")
                .WithMaxLevel(1)
                .WithCost("soul.large", Native_15b6910)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b6918, "large")
                .WithSimulationSettings(1, 0.5f, 1, 1.0f));
            // Original constructor call 0x15867f8
            definitions.Add(new UpgradeDef("graph_souls_drop_uncommon_amount_2_9")
                .WithMaxLevel(1)
                .WithCost("soul.great", 10.0d)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b6920, "large")
                .WithSimulationSettings(1, 0.6000000238418579f, 1, 1.0f));
            // Original constructor call 0x1586940
            definitions.Add(new UpgradeDef("graph_cut_defence_flat_2_10")
                .WithMaxLevel(12)
                .WithCost("soul.elite", Native_15b6928)
                .WithStatModifier((StatType)16, (ModifierType)0, Native_15b6930, 0)
                .WithSimulationSettings(8, 0.800000011920929f, 22, 1.2000000476837158f));
            // Original constructor call 0x1586b08
            definitions.Add(new UpgradeDef("graph_cut_defence_percent_2_11")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b6938)
                .WithStatModifier((StatType)17, (ModifierType)4, Native_15b6940, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6954)
                .WithSimulationSettings(4, 1.0f, 17, 0.20000000298023224f));
            // Original constructor call 0x1586d64
            definitions.Add(new UpgradeDef("graph_pierce_damage_flat_2_12")
                .WithMaxLevel(12)
                .WithCost("soul.elite", Native_15b6964)
                .WithStatModifier((StatType)6, (ModifierType)0, Native_15b696c, 0)
                .WithSimulationSettings(8, 0.800000011920929f, 22, 0.6000000238418579f));
            // Original constructor call 0x1586f20
            definitions.Add(new UpgradeDef("graph_pierce_damage_percent_2_13")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b6974)
                .WithStatModifier((StatType)7, (ModifierType)4, Native_15b697c, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6990)
                .WithSimulationSettings(4, 1.0f, 17, 0.20000000298023224f));
            // Original constructor call 0x158717c
            definitions.Add(new UpgradeDef("graph_souls_drop_rare_amount_2_14")
                .WithMaxLevel(1)
                .WithCost("soul.elite", Native_15b69a0)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b69a8, "elite")
                .WithSimulationSettings(1, 0.4000000059604645f, 1, 1.0f));
            // Original constructor call 0x158734c
            definitions.Add(new UpgradeDef("graph_souls_drop_common_amount_2_15")
                .WithMaxLevel(1)
                .WithCost("soul.great", 10.0d)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b69b0, "common")
                .WithSimulationSettings(1, 0.4000000059604645f, 1, 1.0f));
            // Original constructor call 0x1587494
            definitions.Add(new UpgradeDef("graph_souls_drop_rare_amount_2_16")
                .WithMaxLevel(1)
                .WithCost("soul.divine", 1.0d)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b69bc, "elite")
                .WithSimulationSettings(1, 0.4000000059604645f, 1, 1.0f));
            // Original constructor call 0x15875dc
            definitions.Add(new UpgradeDef("graph_lifesteal_flat_2_17")
                .WithMaxLevel(12)
                .WithCost("soul.elite", Native_15b69c4)
                .WithStatModifier((StatType)22, (ModifierType)0, Native_15b69cc, 0)
                .WithSimulationSettings(8, 0.800000011920929f, 22, 1.0f));
            // Original constructor call 0x1587798
            definitions.Add(new UpgradeDef("graph_lifesteal_percent_2_18")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b69d4)
                .WithStatModifier((StatType)22, (ModifierType)4, Native_15b69dc, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b69f0)
                .WithSimulationSettings(4, 1.0f, 17, 0.20000000298023224f));
            // Original constructor call 0x15879f4
            definitions.Add(new UpgradeDef("graph_evasion_percent_2_19")
                .WithMaxLevel(3)
                .WithCost("soul.large", Native_15b6a00)
                .WithStatModifier((StatType)23, (ModifierType)6, Native_15b6a08, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6a1c)
                .WithSimulationSettings(1, 1.0f, 17, 0.20000000298023224f));
            // Original constructor call 0x1587c50
            definitions.Add(new UpgradeDef("graph_items_drop_uncommon_2_20")
                .WithMaxLevel(1)
                .WithCost("soul.elite", Native_15b6a2c)
                .WithModifier("items_rarity_drop_amount", (ModifierType)0, Native_15b6a34, 2)
                .WithSimulationSettings(1, 0.30000001192092896f, 1, 1.0f));
            // Original constructor call 0x1587e24
            definitions.Add(new UpgradeDef("graph_items_charges_uncommon_2_21")
                .WithMaxLevel(3)
                .WithCost("soul.huge", Native_15b6a3c)
                .WithModifier("items_rarity_charges", (ModifierType)0, Native_15b6a98, 2)
                .WithSimulationSettings(1, 0.6000000238418579f, 1, 1.0f));
            // Original constructor call 0x1588000
            definitions.Add(new UpgradeDef("graph_items_timer_uncommon_2_22")
                .WithMaxLevel(2)
                .WithCost("soul.huge", Native_15b6aa0)
                .WithModifier("items_rarity_timer", (ModifierType)0, Native_15b6ab0, 2)
                .WithSimulationSettings(2, 0.5f, 1, 1.0f)
                .WithDisplay((UpgradeValueFormat)0, Native_15b6ac0));
            // Original constructor call 0x158827c
            definitions.Add(new UpgradeDef("graph_chest_level_2_23")
                .WithMaxLevel(1)
                .WithCost("soul.common", Native_15b6acc)
                .WithModifier("chest_level", (ModifierType)0, Native_15b6ad4, 0)
                .WithSimulationSettings(10, 0.5f, 17, 0.20000000298023224f));
            // Original constructor call 0x1588444
            definitions.Add(new UpgradeDef("graph_items_drop_uncommon_2_24")
                .WithMaxLevel(1)
                .WithCost("soul.huge", 40.0d)
                .WithModifier("items_rarity_drop_amount", (ModifierType)0, Native_15b6adc, 2)
                .WithSimulationSettings(1, 0.30000001192092896f, 1, 1.0f));
            // Original constructor call 0x158859c
            definitions.Add(new UpgradeDef("graph_cut_defence_percent_2_25")
                .WithMaxLevel(10)
                .WithCost("soul.great", 1.0d)
                .WithStatModifier((StatType)17, (ModifierType)4, Native_15b6ae4, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6af8)
                .WithSimulationSettings(5, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x1588778
            definitions.Add(new UpgradeDef("graph_pierce_damage_percent_2_26")
                .WithMaxLevel(10)
                .WithCost("soul.great", 1.0d)
                .WithStatModifier((StatType)7, (ModifierType)4, Native_15b6b08, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6b1c)
                .WithSimulationSettings(5, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x1588954
            definitions.Add(new UpgradeDef("graph_lifesteal_percent_2_27")
                .WithMaxLevel(10)
                .WithCost("soul.great", 1.0d)
                .WithStatModifier((StatType)22, (ModifierType)4, Native_15b6b2c, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6b40)
                .WithSimulationSettings(5, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x1588b30
            definitions.Add(new UpgradeDef("graph_map_spawner_timer_reduce_island2_2_28")
                .WithMaxLevel(1)
                .WithCost("soul.large", Native_15b6b50)
                .WithModifier("map_spawner_timer_reduce", (ModifierType)0, Native_15b6b58, 2)
                .WithDisplay((UpgradeValueFormat)3, 2, null)
                .WithSimulationSettings(1, 0.30000001192092896f, 1, 1.0f));
            // Original constructor call 0x1588d10
            definitions.Add(new UpgradeDef("graph_map_spawner_timer_reduce_island3_2_29")
                .WithMaxLevel(1)
                .WithCost("soul.large", Native_15b6b64)
                .WithModifier("map_spawner_timer_reduce", (ModifierType)0, Native_15b6b6c, 3)
                .WithDisplay((UpgradeValueFormat)3, 3, null)
                .WithSimulationSettings(1, 0.3499999940395355f, 1, 1.0f));
            // Original constructor call 0x1588ef8
            definitions.Add(new UpgradeDef("graph_weak_spawner_extra_range_island2_2_30")
                .WithMaxLevel(1)
                .WithCost("soul.elite", Native_15b6b78)
                .WithModifier("weak_spawner_extra_range", (ModifierType)0, Native_15b6b80, 2)
                .WithSimulationSettings(1, 0.20000000298023224f, 1, 1.0f));
            // Original constructor call 0x15890c0
            definitions.Add(new UpgradeDef("graph_weak_spawner_extra_range_island3_2_31")
                .WithMaxLevel(1)
                .WithCost("soul.elite", Native_15b6b88)
                .WithModifier("weak_spawner_extra_range", (ModifierType)0, Native_15b6b90, 3)
                .WithSimulationSettings(1, 0.25f, 1, 1.0f));
            // Original constructor call 0x1589288
            definitions.Add(new UpgradeDef("graph_boss_tower_tickets_3_1")
                .WithMaxLevel(2)
                .WithCost("soul.huge", Native_15b6b98)
                .WithModifier("boss_tower_tickets", (ModifierType)0, Native_15b6ba4, 0));
            // Original constructor call 0x158943c
            definitions.Add(new UpgradeDef("graph_health_regen_flat_3_2")
                .WithMaxLevel(10)
                .WithCost("soul.elite", Native_15b6bac)
                .WithStatModifier((StatType)2, (ModifierType)0, Native_15b6bb4, 0)
                .WithSimulationSettings(10, 1.0f, 28, 0.30000001192092896f));
            // Original constructor call 0x15895f8
            definitions.Add(new UpgradeDef("graph_hammer_regen_3_3")
                .WithMaxLevel(3)
                .WithCost("soul.divine", Native_15b6bbc)
                .WithCost("soul.chaotic", Native_15b6bd0)
                .WithModifier("hammer_regen", (ModifierType)0, Native_15b6be4, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6bfc)
                .WithSimulationSettings(3, 1.0f, 28, 0.20000000298023224f));
            // Original constructor call 0x1589924
            definitions.Add(new UpgradeDef("graph_smash_damage_flat_3_4")
                .WithMaxLevel(15)
                .WithCost("soul.large", Native_15b6c0c)
                .WithStatModifier((StatType)12, (ModifierType)0, Native_15b6c14, 0)
                .WithSimulationSettings(7, 1.0f, 32, 0.5f));
            // Original constructor call 0x1589ae0
            definitions.Add(new UpgradeDef("graph_smash_damage_percent_3_5")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b6c1c)
                .WithStatModifier((StatType)13, (ModifierType)4, Native_15b6c24, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6c38)
                .WithSimulationSettings(4, 1.0f, 28, 0.5f));
            // Original constructor call 0x1589d3c
            definitions.Add(new UpgradeDef("graph_pierce_defence_flat_3_6")
                .WithMaxLevel(15)
                .WithCost("soul.large", Native_15b6c48)
                .WithStatModifier((StatType)14, (ModifierType)0, Native_15b6c50, 0)
                .WithSimulationSettings(7, 1.0f, 32, 0.699999988079071f));
            // Original constructor call 0x1589f00
            definitions.Add(new UpgradeDef("graph_pierce_defence_percent_3_7")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b6c58)
                .WithStatModifier((StatType)15, (ModifierType)4, Native_15b6c60, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6c74)
                .WithSimulationSettings(4, 1.0f, 28, 0.5f));
            // Original constructor call 0x158a15c
            definitions.Add(new UpgradeDef("graph_souls_drop_common_amount_3_8")
                .WithMaxLevel(2)
                .WithCost("soul.elite", Native_15b6c84)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b6c8c, "common")
                .WithSimulationSettings(1, 0.800000011920929f, 28, 0.20000000298023224f));
            // Original constructor call 0x158a324
            definitions.Add(new UpgradeDef("graph_items_drop_rare_3_9")
                .WithMaxLevel(1)
                .WithCost("soul.elite", Native_15b6c94)
                .WithModifier("items_rarity_drop_amount", (ModifierType)0, Native_15b6c9c, 3)
                .WithSimulationSettings(1, 0.4000000059604645f, 1, 1.0f));
            // Original constructor call 0x158a4f8
            definitions.Add(new UpgradeDef("graph_items_timer_rare_3_10")
                .WithMaxLevel(1)
                .WithCost("soul.huge", Native_15b6ca4)
                .WithModifier("items_rarity_timer", (ModifierType)0, Native_15b6cb4, 3)
                .WithSimulationSettings(2, 0.5f, 1, 1.0f)
                .WithDisplay((UpgradeValueFormat)0, Native_15b6cc4));
            // Original constructor call 0x158a774
            definitions.Add(new UpgradeDef("graph_items_charges_rare_3_11")
                .WithMaxLevel(2)
                .WithCost("soul.large", Native_15b6cd0)
                .WithModifier("items_rarity_charges", (ModifierType)0, Native_15b6d2c, 3)
                .WithSimulationSettings(1, 1.0f, 1, 1.0f));
            // Original constructor call 0x158a948
            definitions.Add(new UpgradeDef("graph_chest_level_3_12")
                .WithMaxLevel(1)
                .WithCost("soul.common", Native_15b6d34)
                .WithModifier("chest_level", (ModifierType)0, Native_15b6d3c, 0)
                .WithSimulationSettings(10, 0.30000001192092896f, 17, 0.20000000298023224f));
            // Original constructor call 0x158ab10
            definitions.Add(new UpgradeDef("graph_economy_item_abilities_chance_3_13")
                .WithMaxLevel(2)
                .WithCost("soul.huge", Native_15b6d44)
                .WithModifier("economy_item_abilities_chance", (ModifierType)0, Native_15b6dac, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6dc0)
                .WithSimulationSettings(2, 0.30000001192092896f, 1, 1.0f));
            // Original constructor call 0x158ad80
            definitions.Add(new UpgradeDef("graph_free_chest_weekly_3_14")
                .WithMaxLevel(1)
                .WithCost("soul.great", 10.0d)
                .WithModifier("free_chest", (ModifierType)0, 7.0d, "chest_1")
                .WithDisplay((UpgradeValueFormat)2, null)
                .WithSimulationSettings(10, 0.30000001192092896f, 17, 0.20000000298023224f));
            // Original constructor call 0x158ae54
            definitions.Add(new UpgradeDef("graph_free_chest_daily_3_15")
                .WithMaxLevel(1)
                .WithCost("soul.chaotic", 1.0d)
                .WithModifier("free_chest", (ModifierType)0, 0.0d, "chest_1")
                .WithDisplay((UpgradeValueFormat)2, null)
                .WithSimulationSettings(10, 0.30000001192092896f, 17, 0.20000000298023224f));
            // Original constructor call 0x158af18
            definitions.Add(new UpgradeDef("graph_smash_damage_flat_3_16")
                .WithMaxLevel(10)
                .WithCost("soul.elite", Native_15b6dd0)
                .WithStatModifier((StatType)12, (ModifierType)0, Native_15b6dd8, 0)
                .WithSimulationSettings(10, 1.0f, 28, 0.25f));
            // Original constructor call 0x158b0d4
            definitions.Add(new UpgradeDef("graph_pierce_defence_flat_3_17")
                .WithMaxLevel(10)
                .WithCost("soul.elite", Native_15b6de0)
                .WithStatModifier((StatType)14, (ModifierType)0, Native_15b6de8, 0)
                .WithSimulationSettings(10, 1.0f, 28, 0.3499999940395355f));
            // Original constructor call 0x158b290
            definitions.Add(new UpgradeDef("graph_health_regen_flat_3_18")
                .WithMaxLevel(15)
                .WithCost("soul.large", Native_15b6df0)
                .WithStatModifier((StatType)2, (ModifierType)0, Native_15b6df8, 0)
                .WithSimulationSettings(7, 1.0f, 32, 0.5f));
            // Original constructor call 0x158b44c
            definitions.Add(new UpgradeDef("graph_health_regen_percent_3_19")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b6e00)
                .WithStatModifier((StatType)2, (ModifierType)4, Native_15b6e08, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6e1c)
                .WithSimulationSettings(4, 1.0f, 28, 0.5f));
            // Original constructor call 0x158b6a8
            definitions.Add(new UpgradeDef("graph_souls_drop_uncommon_amount_3_20")
                .WithMaxLevel(1)
                .WithCost("soul.elite", Native_15b6e2c)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b6e34, "large")
                .WithSimulationSettings(1, 1.2999999523162842f, 28, 0.20000000298023224f));
            // Original constructor call 0x158b874
            definitions.Add(new UpgradeDef("graph_boss_tower_damage_reduction_3_21")
                .WithMaxLevel(3)
                .WithCost("soul.huge", Native_15b6e3c)
                .WithModifier("tower_damage_reduction", (ModifierType)0, Native_15b6e4c, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6e60)
                .WithSimulationSettings(3, 1.0f, 28, 0.20000000298023224f));
            // Original constructor call 0x158bae4
            definitions.Add(new UpgradeDef("graph_boss_tower_damage_3_22")
                .WithMaxLevel(3)
                .WithCost("soul.huge", Native_15b6e70)
                .WithModifier("tower_damage", (ModifierType)0, Native_15b6e80, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6e94)
                .WithSimulationSettings(3, 1.0f, 28, 0.20000000298023224f));
            // Original constructor call 0x158bd54
            definitions.Add(new UpgradeDef("graph_crit_chance_3_23")
                .WithMaxLevel(3)
                .WithCost("soul.great", 4.0d)
                .WithStatModifier((StatType)24, (ModifierType)6, Native_15b6ea4, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6eb8)
                .WithSimulationSettings(3, 1.0f, 28, 0.20000000298023224f));
            // Original constructor call 0x158bf30
            definitions.Add(new UpgradeDef("graph_crit_damage_percent_3_24")
                .WithMaxLevel(5)
                .WithCost("soul.great", 2.0d)
                .WithStatModifier((StatType)25, (ModifierType)6, Native_15b6ec8, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6edc)
                .WithSimulationSettings(3, 1.0f, 28, 0.20000000298023224f));
            // Original constructor call 0x158c10c
            definitions.Add(new UpgradeDef("graph_pierce_defence_percent_3_25")
                .WithMaxLevel(10)
                .WithCost("soul.great", 1.0d)
                .WithStatModifier((StatType)15, (ModifierType)4, Native_15b6eec, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6f00)
                .WithSimulationSettings(5, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x158c2e8
            definitions.Add(new UpgradeDef("graph_smash_damage_percent_3_26")
                .WithMaxLevel(10)
                .WithCost("soul.great", 1.0d)
                .WithStatModifier((StatType)13, (ModifierType)4, Native_15b6f10, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6f24)
                .WithSimulationSettings(5, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x158c4c4
            definitions.Add(new UpgradeDef("graph_health_regen_percent_3_27")
                .WithMaxLevel(10)
                .WithCost("soul.great", 1.0d)
                .WithStatModifier((StatType)2, (ModifierType)4, Native_15b6f34, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6f48)
                .WithSimulationSettings(5, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x158c6a0
            definitions.Add(new UpgradeDef("graph_boss_tower_unlock_berserker_4_1")
                .WithMaxLevel(1)
                .WithCost("soul.huge", 3.0d)
                .WithModifier("boss_tower_unlock", (ModifierType)0, 1.0d, "berserker"));
            // Original constructor call 0x158c744
            definitions.Add(new UpgradeDef("graph_cut_damage_flat_4_2")
                .WithMaxLevel(25)
                .WithCost("soul.common", Native_15b6f58)
                .WithStatModifier((StatType)8, (ModifierType)0, Native_15b6f60, 0)
                .WithSimulationSettings(13, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x158c900
            definitions.Add(new UpgradeDef("graph_cut_damage_percent_4_3")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b6f68)
                .WithStatModifier((StatType)9, (ModifierType)4, Native_15b6f70, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6f84)
                .WithSimulationSettings(5, 0.30000001192092896f, 17, 0.20000000298023224f));
            // Original constructor call 0x158cb5c
            definitions.Add(new UpgradeDef("graph_lifesteal_flat_4_4")
                .WithMaxLevel(25)
                .WithCost("soul.common", Native_15b6f94)
                .WithStatModifier((StatType)22, (ModifierType)0, Native_15b6f9c, 0)
                .WithSimulationSettings(13, 1.0f, 50, 0.4000000059604645f));
            // Original constructor call 0x158cd18
            definitions.Add(new UpgradeDef("graph_lifesteal_percent_4_5")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b6fa4)
                .WithStatModifier((StatType)22, (ModifierType)4, Native_15b6fac, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6fc0)
                .WithSimulationSettings(5, 0.30000001192092896f, 17, 0.20000000298023224f));
            // Original constructor call 0x158cf74
            definitions.Add(new UpgradeDef("graph_smash_defence_flat_4_6")
                .WithMaxLevel(25)
                .WithCost("soul.common", Native_15b6fd0)
                .WithStatModifier((StatType)20, (ModifierType)0, Native_15b6fd8, 0)
                .WithSimulationSettings(13, 1.0f, 40, 0.5f));
            // Original constructor call 0x158d130
            definitions.Add(new UpgradeDef("graph_smash_defence_percent_4_7")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b6fe0)
                .WithStatModifier((StatType)21, (ModifierType)4, Native_15b6fe8, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b6ffc)
                .WithSimulationSettings(5, 0.30000001192092896f, 17, 0.20000000298023224f));
            // Original constructor call 0x158d38c
            definitions.Add(new UpgradeDef("graph_crystals_sell_all_unlock_4_8")
                .WithMaxLevel(1)
                .WithCost("soul.huge", 3.0d)
                .WithModifier("feature_unlock", (ModifierType)0, 1.0d, "crystals_sell_all")
                .WithSimulationSettings(1, 0.30000001192092896f, 40, 0.4000000059604645f));
            // Original constructor call 0x158d44c
            definitions.Add(new UpgradeDef("graph_items_charges_common_4_9")
                .WithMaxLevel(4)
                .WithCost("soul.common", Native_15b700c)
                .WithModifier("items_rarity_charges", (ModifierType)0, Native_15b7014, 1)
                .WithSimulationSettings(3, 0.5f, 40, 0.4000000059604645f));
            // Original constructor call 0x158d620
            definitions.Add(new UpgradeDef("graph_items_drop_common_4_10")
                .WithMaxLevel(4)
                .WithCost("soul.large", Native_15b701c)
                .WithModifier("items_rarity_drop_amount", (ModifierType)0, Native_15b7024, 1)
                .WithSimulationSettings(3, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x158d7f4
            definitions.Add(new UpgradeDef("graph_boss_tower_weakness_4_11")
                .WithMaxLevel(1)
                .WithCost("soul.divine", Native_15b702c)
                .WithModifier("tower_boss_weakness", (ModifierType)0, Native_15b7034, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b704c)
                .WithSimulationSettings(1, 0.30000001192092896f, 40, 0.4000000059604645f));
            // Original constructor call 0x158da64
            definitions.Add(new UpgradeDef("graph_map_spawner_timer_reduce_4_12")
                .WithMaxLevel(1)
                .WithCost("soul.elite", Native_15b705c)
                .WithModifier("map_spawner_timer_reduce", (ModifierType)0, Native_15b7064, 4)
                .WithDisplay((UpgradeValueFormat)3, 4, null)
                .WithSimulationSettings(1, 0.25f, 40, 0.4000000059604645f));
            // Original constructor call 0x158dc44
            definitions.Add(new UpgradeDef("graph_map_spawner_drop_island4_4_13")
                .WithMaxLevel(5)
                .WithCost("soul.huge", Native_15b7070)
                .WithModifier("stat_drop", (ModifierType)0, Native_15b7078, 4)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7090)
                .WithSimulationSettings(3, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x158deb4
            definitions.Add(new UpgradeDef("graph_map_spawner_drop_island5_4_14")
                .WithMaxLevel(5)
                .WithCost("soul.huge", Native_15b70a0)
                .WithModifier("stat_drop", (ModifierType)0, Native_15b70a8, 5)
                .WithDisplay((UpgradeValueFormat)1, Native_15b70c0)
                .WithSimulationSettings(3, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x158e124
            definitions.Add(new UpgradeDef("graph_map_spawner_drop_island6_4_15")
                .WithMaxLevel(5)
                .WithCost("soul.huge", Native_15b70d0)
                .WithModifier("stat_drop", (ModifierType)0, Native_15b70d8, 6)
                .WithDisplay((UpgradeValueFormat)1, Native_15b70f0)
                .WithSimulationSettings(3, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x158e394
            definitions.Add(new UpgradeDef("graph_boss_tower_unlock_vampire_4_16")
                .WithMaxLevel(1)
                .WithCost("soul.huge", 3.0d)
                .WithModifier("boss_tower_unlock", (ModifierType)0, 1.0d, "vampire_king"));
            // Original constructor call 0x158e438
            definitions.Add(new UpgradeDef("graph_health_flat_4_17")
                .WithMaxLevel(25)
                .WithCost("soul.common", Native_15b7100)
                .WithStatModifier((StatType)1, (ModifierType)0, Native_15b7108, 0)
                .WithSimulationSettings(13, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x158e5f4
            definitions.Add(new UpgradeDef("graph_health_percent_4_18")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b7110)
                .WithStatModifier((StatType)1, (ModifierType)4, Native_15b7118, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b712c)
                .WithSimulationSettings(5, 0.30000001192092896f, 17, 0.20000000298023224f));
            // Original constructor call 0x158e850
            definitions.Add(new UpgradeDef("graph_chest_level_4_19")
                .WithMaxLevel(1)
                .WithCost("soul.common", Native_15b713c)
                .WithModifier("chest_level", (ModifierType)0, Native_15b7144, 0)
                .WithSimulationSettings(10, 0.5f, 17, 0.20000000298023224f));
            // Original constructor call 0x158ea18
            definitions.Add(new UpgradeDef("graph_block_chance_4_20")
                .WithMaxLevel(3)
                .WithCost("soul.great", 5.0d)
                .WithStatModifier((StatType)33, (ModifierType)6, Native_15b714c, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7160)
                .WithSimulationSettings(2, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x158ebf4
            definitions.Add(new UpgradeDef("graph_rage_extra_health_4_21")
                .WithMaxLevel(3)
                .WithCost("soul.divine", Native_15b7170)
                .WithCost("soul.chaotic", Native_15b7184)
                .WithModifier("rage_extra_health", (ModifierType)0, Native_15b7198, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b71b0)
                .WithSimulationSettings(2, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x158ef20
            definitions.Add(new UpgradeDef("graph_block_amount_4_22")
                .WithMaxLevel(3)
                .WithCost("soul.great", 5.0d)
                .WithStatModifier((StatType)34, (ModifierType)6, Native_15b71c0, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b71d4)
                .WithSimulationSettings(2, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x158f0fc
            definitions.Add(new UpgradeDef("graph_pierce_damage_flat_4_23")
                .WithMaxLevel(25)
                .WithCost("soul.common", Native_15b71e4)
                .WithStatModifier((StatType)6, (ModifierType)0, Native_15b71ec, 0)
                .WithSimulationSettings(13, 1.0f, 50, 0.4000000059604645f));
            // Original constructor call 0x158f2b8
            definitions.Add(new UpgradeDef("graph_pierce_damage_percent_4_24")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b71f4)
                .WithStatModifier((StatType)7, (ModifierType)4, Native_15b71fc, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7210)
                .WithSimulationSettings(5, 0.30000001192092896f, 17, 0.20000000298023224f));
            // Original constructor call 0x158f514
            definitions.Add(new UpgradeDef("graph_boss_tower_unlock_lich_4_25")
                .WithMaxLevel(1)
                .WithCost("soul.huge", 3.0d)
                .WithModifier("boss_tower_unlock", (ModifierType)0, 1.0d, "lich"));
            // Original constructor call 0x158f5b8
            definitions.Add(new UpgradeDef("graph_cut_defence_flat_4_26")
                .WithMaxLevel(25)
                .WithCost("soul.common", Native_15b7220)
                .WithStatModifier((StatType)16, (ModifierType)0, Native_15b7228, 0)
                .WithSimulationSettings(13, 1.0f, 50, 0.5f));
            // Original constructor call 0x158f774
            definitions.Add(new UpgradeDef("graph_cut_defence_percent_4_27")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b7230)
                .WithStatModifier((StatType)17, (ModifierType)4, Native_15b7238, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b724c)
                .WithSimulationSettings(5, 0.30000001192092896f, 17, 0.20000000298023224f));
            // Original constructor call 0x158f9d0
            definitions.Add(new UpgradeDef("graph_souls_drop_common_amount_4_28")
                .WithMaxLevel(2)
                .WithCost("soul.elite", Native_15b725c)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b7264, "common")
                .WithSimulationSettings(2, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x158fb98
            definitions.Add(new UpgradeDef("graph_souls_drop_uncommon_amount_4_29")
                .WithMaxLevel(1)
                .WithCost("soul.common", Native_15b726c)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b7274, "large")
                .WithSimulationSettings(1, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x158fd60
            definitions.Add(new UpgradeDef("graph_souls_drop_rare_amount_4_30")
                .WithMaxLevel(1)
                .WithCost("soul.large", Native_15b727c)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b7284, "elite")
                .WithSimulationSettings(1, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x158ff28
            definitions.Add(new UpgradeDef("graph_items_charges_epic_4_31")
                .WithMaxLevel(2)
                .WithCost("soul.elite", Native_15b728c)
                .WithModifier("items_rarity_charges", (ModifierType)0, Native_15b7294, 4)
                .WithSimulationSettings(1, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x15900fc
            definitions.Add(new UpgradeDef("graph_items_drop_epic_4_32")
                .WithMaxLevel(1)
                .WithCost("soul.huge", 10.0d)
                .WithModifier("items_rarity_drop_amount", (ModifierType)0, Native_15b729c, 4)
                .WithSimulationSettings(1, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x1590250
            definitions.Add(new UpgradeDef("graph_items_timer_epic_4_33")
                .WithMaxLevel(1)
                .WithCost("soul.huge", Native_15b72a4)
                .WithModifier("items_rarity_timer", (ModifierType)0, Native_15b72b4, 4)
                .WithSimulationSettings(1, 1.0f, 40, 0.4000000059604645f)
                .WithDisplay((UpgradeValueFormat)0, Native_15b72c4));
            // Original constructor call 0x15904cc
            definitions.Add(new UpgradeDef("graph_health_percent_4_34")
                .WithMaxLevel(10)
                .WithCost("soul.great", 1.0d)
                .WithStatModifier((StatType)1, (ModifierType)4, Native_15b72d0, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b72e4)
                .WithSimulationSettings(5, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x15906a8
            definitions.Add(new UpgradeDef("graph_cut_damage_percent_4_35")
                .WithMaxLevel(10)
                .WithCost("soul.great", 1.0d)
                .WithStatModifier((StatType)9, (ModifierType)4, Native_15b72f4, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7308)
                .WithSimulationSettings(5, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x1590884
            definitions.Add(new UpgradeDef("graph_smash_defence_percent_4_36")
                .WithMaxLevel(10)
                .WithCost("soul.great", 1.0d)
                .WithStatModifier((StatType)21, (ModifierType)4, Native_15b7318, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b732c)
                .WithSimulationSettings(5, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x1590a60
            definitions.Add(new UpgradeDef("graph_cut_defence_percent_4_37")
                .WithMaxLevel(10)
                .WithCost("soul.great", 1.0d)
                .WithStatModifier((StatType)17, (ModifierType)4, Native_15b733c, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7350)
                .WithSimulationSettings(5, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x1590c3c
            definitions.Add(new UpgradeDef("graph_lifesteal_percent_4_38")
                .WithMaxLevel(10)
                .WithCost("soul.great", 1.0d)
                .WithStatModifier((StatType)22, (ModifierType)4, Native_15b7360, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7374)
                .WithSimulationSettings(5, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x1590e18
            definitions.Add(new UpgradeDef("graph_pierce_damage_percent_4_39")
                .WithMaxLevel(10)
                .WithCost("soul.great", 1.0d)
                .WithStatModifier((StatType)7, (ModifierType)4, Native_15b7384, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7398)
                .WithSimulationSettings(5, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x1590ff4
            definitions.Add(new UpgradeDef("graph_map_spawner_timer_reduce_island5_4_40")
                .WithMaxLevel(1)
                .WithCost("soul.elite", Native_15b73a8)
                .WithModifier("map_spawner_timer_reduce", (ModifierType)0, Native_15b73b0, 5)
                .WithDisplay((UpgradeValueFormat)3, 5, null)
                .WithSimulationSettings(1, 0.30000001192092896f, 1, 1.0f));
            // Original constructor call 0x15911d4
            definitions.Add(new UpgradeDef("graph_map_spawner_timer_reduce_island6_4_41")
                .WithMaxLevel(1)
                .WithCost("soul.elite", Native_15b73bc)
                .WithModifier("map_spawner_timer_reduce", (ModifierType)0, Native_15b73c4, 6)
                .WithDisplay((UpgradeValueFormat)3, 6, null)
                .WithSimulationSettings(1, 0.25f, 1, 1.0f));
            // Original constructor call 0x15913b4
            definitions.Add(new UpgradeDef("graph_super_crit_chance_4_42")
                .WithMaxLevel(1)
                .WithCost("soul.divine", 1.0d)
                .WithStatModifier((StatType)28, (ModifierType)6, Native_15b73d0, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b73e8)
                .WithSimulationSettings(10, 0.30000001192092896f, 17, 0.20000000298023224f));
            // Original constructor call 0x1591590
            definitions.Add(new UpgradeDef("graph_super_crit_damage_percent_4_43")
                .WithMaxLevel(1)
                .WithCost("soul.divine", 1.0d)
                .WithStatModifier((StatType)29, (ModifierType)6, Native_15b73f8, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7410)
                .WithSimulationSettings(10, 0.30000001192092896f, 17, 0.20000000298023224f));
            // Original constructor call 0x159176c
            definitions.Add(new UpgradeDef("graph_weak_spawner_extra_range_island4_4_44")
                .WithMaxLevel(1)
                .WithCost("soul.large", Native_15b7420)
                .WithModifier("weak_spawner_extra_range", (ModifierType)0, Native_15b7428, 4)
                .WithSimulationSettings(1, 0.25f, 1, 1.0f));
            // Original constructor call 0x1591934
            definitions.Add(new UpgradeDef("graph_weak_spawner_extra_range_island5_4_45")
                .WithMaxLevel(1)
                .WithCost("soul.large", Native_15b7430)
                .WithModifier("weak_spawner_extra_range", (ModifierType)0, Native_15b7438, 5)
                .WithSimulationSettings(1, 0.30000001192092896f, 1, 1.0f));
            // Original constructor call 0x1591afc
            definitions.Add(new UpgradeDef("graph_weak_spawner_extra_range_island6_4_46")
                .WithMaxLevel(1)
                .WithCost("soul.large", Native_15b7440)
                .WithModifier("weak_spawner_extra_range", (ModifierType)0, Native_15b7448, 6)
                .WithSimulationSettings(1, 0.30000001192092896f, 1, 1.0f));
            // Original constructor call 0x1591cc4
            definitions.Add(new UpgradeDef("graph_event_golem_extra_range_4_47")
                .WithMaxLevel(1)
                .WithCost("soul.chaotic", 1.0d)
                .WithModifier("event_golem_extra_range", (ModifierType)0, 1.0d, 0)
                .WithModifier("stat_drop", (ModifierType)0, 1.0d, -1)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7450)
                .WithSimulationSettings(1, 0.3499999940395355f, 1, 1.0f));
            // Original constructor call 0x1591e40
            definitions.Add(new UpgradeDef("graph_event_golem_damage_4_48")
                .WithMaxLevel(1)
                .WithCost("soul.elite", Native_15b7460)
                .WithModifier("event_golem_damage", (ModifierType)0, 0.5d, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7468)
                .WithSimulationSettings(1, 0.3499999940395355f, 1, 1.0f));
            // Original constructor call 0x1592018
            definitions.Add(new UpgradeDef("graph_crystal_slot2_unlock_5_1")
                .WithMaxLevel(1)
                .WithCost("soul.huge", 20.0d)
                .WithModifier("feature_unlock", (ModifierType)0, 1.0d, "crystal_slot_3"));
            // Original constructor call 0x15920bc
            definitions.Add(new UpgradeDef("graph_smash_damage_flat_5_2")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b7478)
                .WithStatModifier((StatType)12, (ModifierType)0, Native_15b7480, 0)
                .WithSimulationSettings(8, 1.0f, 58, 0.30000001192092896f));
            // Original constructor call 0x1592278
            definitions.Add(new UpgradeDef("graph_empowered_lightning_5_3")
                .WithMaxLevel(3)
                .WithCost("soul.divine", Native_15b7488)
                .WithCost("soul.chaotic", Native_15b749c)
                .WithModifier("empowered_lightning", (ModifierType)0, Native_15b74b0, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b74c4)
                .WithSimulationSettings(8, 1.0f, 60, 0.20000000298023224f));
            // Original constructor call 0x15925a4
            definitions.Add(new UpgradeDef("graph_pierce_defence_flat_5_4")
                .WithMaxLevel(20)
                .WithCost("soul.elite", Native_15b74d4)
                .WithStatModifier((StatType)14, (ModifierType)0, Native_15b74dc, 0)
                .WithSimulationSettings(12, 1.0f, 65, 0.3499999940395355f));
            // Original constructor call 0x1592760
            definitions.Add(new UpgradeDef("graph_pierce_defence_percent_5_5")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b74e4)
                .WithStatModifier((StatType)15, (ModifierType)4, Native_15b74ec, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7500)
                .WithSimulationSettings(6, 1.0f, 85, 0.3499999940395355f));
            // Original constructor call 0x15929bc
            definitions.Add(new UpgradeDef("graph_health_flat_5_6")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b7510)
                .WithStatModifier((StatType)1, (ModifierType)0, Native_15b7518, 0)
                .WithSimulationSettings(8, 1.0f, 65, 0.30000001192092896f));
            // Original constructor call 0x1592b78
            definitions.Add(new UpgradeDef("graph_rage_damage_reduce_5_7")
                .WithMaxLevel(3)
                .WithCost("soul.divine", Native_15b7520)
                .WithCost("soul.chaotic", Native_15b7534)
                .WithModifier("rage_damage_reduce", (ModifierType)0, Native_15b7548, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b755c)
                .WithSimulationSettings(8, 1.0f, 60, 0.20000000298023224f));
            // Original constructor call 0x1592ea4
            definitions.Add(new UpgradeDef("graph_cut_damage_flat_5_8")
                .WithMaxLevel(20)
                .WithCost("soul.elite", Native_15b756c)
                .WithStatModifier((StatType)8, (ModifierType)0, Native_15b7574, 0)
                .WithSimulationSettings(12, 1.0f, 80, 0.3499999940395355f));
            // Original constructor call 0x1593060
            definitions.Add(new UpgradeDef("graph_cut_damage_percent_5_9")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b757c)
                .WithStatModifier((StatType)9, (ModifierType)4, Native_15b7584, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7598)
                .WithSimulationSettings(6, 1.0f, 85, 0.3499999940395355f));
            // Original constructor call 0x15932bc
            definitions.Add(new UpgradeDef("graph_crystal_blacksmithing_5_10")
                .WithMaxLevel(4)
                .WithCost("soul.common", Native_15b75a8)
                .WithModifier("crystal_blacksmithing", (ModifierType)0, Native_15b7608, 0)
                .WithSimulationSettings(3, 1.0f, 17, 0.20000000298023224f));
            // Original constructor call 0x1593484
            definitions.Add(new UpgradeDef("graph_health_regen_flat_5_11")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b7610)
                .WithStatModifier((StatType)2, (ModifierType)0, Native_15b7618, 0)
                .WithSimulationSettings(8, 1.0f, 58, 0.30000001192092896f));
            // Original constructor call 0x1593640
            definitions.Add(new UpgradeDef("graph_pierce_defence_flat_5_12")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b7620)
                .WithStatModifier((StatType)14, (ModifierType)0, Native_15b7628, 0)
                .WithSimulationSettings(8, 1.0f, 58, 0.30000001192092896f));
            // Original constructor call 0x15937fc
            definitions.Add(new UpgradeDef("graph_mark_lifesteal_5_13")
                .WithMaxLevel(3)
                .WithCost("soul.divine", Native_15b7630)
                .WithCost("soul.chaotic", Native_15b7644)
                .WithModifier("mark_lifesteal", (ModifierType)0, Native_15b7658, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7670)
                .WithSimulationSettings(8, 1.0f, 60, 0.20000000298023224f));
            // Original constructor call 0x1593b28
            definitions.Add(new UpgradeDef("graph_crystals_open_all_unlock_5_14")
                .WithMaxLevel(1)
                .WithCost("soul.large", Native_15b7680)
                .WithModifier("feature_unlock", (ModifierType)0, 1.0d, "crystals_open_all")
                .WithSimulationSettings(1, 0.15000000596046448f, 17, 0.20000000298023224f));
            // Original constructor call 0x1593c70
            definitions.Add(new UpgradeDef("graph_health_regen_flat_5_15")
                .WithMaxLevel(20)
                .WithCost("soul.elite", Native_15b7688)
                .WithStatModifier((StatType)2, (ModifierType)0, Native_15b7690, 0)
                .WithSimulationSettings(12, 1.0f, 65, 0.3499999940395355f));
            // Original constructor call 0x1593e2c
            definitions.Add(new UpgradeDef("graph_health_regen_percent_5_16")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b7698)
                .WithStatModifier((StatType)2, (ModifierType)4, Native_15b76a0, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b76b4)
                .WithSimulationSettings(6, 1.0f, 85, 0.3499999940395355f));
            // Original constructor call 0x1594088
            definitions.Add(new UpgradeDef("graph_crystal_enchantment_5_17")
                .WithMaxLevel(1)
                .WithCost("soul.great", 5.0d)
                .WithModifier("crystal_enchantment", (ModifierType)0, Native_15b76c4, 0)
                .WithSimulationSettings(1, 1.0f, 17, 0.20000000298023224f));
            // Original constructor call 0x15941d0
            definitions.Add(new UpgradeDef("graph_crystal_guarantee_enchant_5_18")
                .WithMaxLevel(1)
                .WithCost("soul.chaotic", 1.0d)
                .WithModifier("crystal_guarantee_enchant", (ModifierType)0, Native_15b76cc, 0)
                .WithSimulationSettings(1, 1.0f, 17, 0.20000000298023224f));
            // Original constructor call 0x1594318
            definitions.Add(new UpgradeDef("graph_crystal_enchantment_stones_5_19")
                .WithMaxLevel(1)
                .WithCost("soul.divine", 1.0d)
                .WithModifier("crystal_max_enchant_stones", (ModifierType)0, Native_15b76d8, 0)
                .WithSimulationSettings(1, 1.0f, 17, 0.20000000298023224f));
            // Original constructor call 0x1594460
            definitions.Add(new UpgradeDef("graph_health_flat_5_20")
                .WithMaxLevel(20)
                .WithCost("soul.elite", Native_15b76e0)
                .WithStatModifier((StatType)1, (ModifierType)0, Native_15b76e8, 0)
                .WithSimulationSettings(12, 1.0f, 80, 0.3499999940395355f));
            // Original constructor call 0x159461c
            definitions.Add(new UpgradeDef("graph_health_percent_5_21")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b76f0)
                .WithStatModifier((StatType)1, (ModifierType)4, Native_15b76f8, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b770c)
                .WithSimulationSettings(6, 1.0f, 85, 0.3499999940395355f));
            // Original constructor call 0x1594878
            definitions.Add(new UpgradeDef("graph_smash_defence_flat_5_22")
                .WithMaxLevel(20)
                .WithCost("soul.elite", Native_15b771c)
                .WithStatModifier((StatType)20, (ModifierType)0, Native_15b7724, 0)
                .WithSimulationSettings(12, 1.0f, 80, 0.3499999940395355f));
            // Original constructor call 0x1594a34
            definitions.Add(new UpgradeDef("graph_smash_defence_percent_5_23")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b772c)
                .WithStatModifier((StatType)21, (ModifierType)4, Native_15b7734, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7748)
                .WithSimulationSettings(6, 1.0f, 85, 0.3499999940395355f));
            // Original constructor call 0x1594c90
            definitions.Add(new UpgradeDef("graph_boss_tower_tickets_5_24")
                .WithMaxLevel(2)
                .WithCost("soul.great", 5.0d)
                .WithModifier("boss_tower_tickets", (ModifierType)0, Native_15b7758, 0)
                .WithSimulationSettings(1, 0.800000011920929f, 17, 0.20000000298023224f));
            // Original constructor call 0x1594dd8
            definitions.Add(new UpgradeDef("graph_boss_tower_items_drop_5_25")
                .WithMaxLevel(2)
                .WithCost("soul.huge", Native_15b7760)
                .WithModifier("boss_tower_items_drop", (ModifierType)0, Native_15b7774, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7788)
                .WithSimulationSettings(1, 0.800000011920929f, 17, 0.20000000298023224f));
            // Original constructor call 0x1595048
            definitions.Add(new UpgradeDef("graph_boss_tower_hydra_5_26")
                .WithMaxLevel(3)
                .WithCost("soul.huge", Native_15b7798)
                .WithModifier("boss_tower_hydra", (ModifierType)0, Native_15b77a8, 0)
                .WithSimulationSettings(1, 0.800000011920929f, 17, 0.20000000298023224f));
            // Original constructor call 0x1595218
            definitions.Add(new UpgradeDef("graph_boss_tower_damage_5_27")
                .WithMaxLevel(3)
                .WithCost("soul.elite", Native_15b77b0)
                .WithModifier("tower_damage", (ModifierType)0, Native_15b77b8, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b77cc)
                .WithSimulationSettings(2, 0.800000011920929f, 17, 0.20000000298023224f));
            // Original constructor call 0x1595480
            definitions.Add(new UpgradeDef("graph_boss_tower_damage_reduction_5_28")
                .WithMaxLevel(3)
                .WithCost("soul.large", Native_15b77dc)
                .WithModifier("tower_damage_reduction", (ModifierType)0, Native_15b77e4, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b77f8)
                .WithSimulationSettings(2, 0.800000011920929f, 17, 0.20000000298023224f));
            // Original constructor call 0x15956e8
            definitions.Add(new UpgradeDef("graph_chest_level_5_29")
                .WithMaxLevel(1)
                .WithCost("soul.common", Native_15b7808)
                .WithModifier("chest_level", (ModifierType)0, Native_15b7810, 0)
                .WithSimulationSettings(10, 0.30000001192092896f, 17, 0.20000000298023224f));
            // Original constructor call 0x15958b0
            definitions.Add(new UpgradeDef("graph_boss_tower_crab_5_30")
                .WithMaxLevel(3)
                .WithCost("soul.huge", Native_15b7818)
                .WithModifier("boss_tower_crab", (ModifierType)0, Native_15b7828, 0)
                .WithSimulationSettings(1, 0.800000011920929f, 17, 0.20000000298023224f));
            // Original constructor call 0x1595a80
            definitions.Add(new UpgradeDef("graph_smash_damage_flat_5_31")
                .WithMaxLevel(20)
                .WithCost("soul.elite", Native_15b7834)
                .WithStatModifier((StatType)12, (ModifierType)0, Native_15b783c, 0)
                .WithSimulationSettings(12, 1.0f, 65, 0.3499999940395355f));
            // Original constructor call 0x1595c3c
            definitions.Add(new UpgradeDef("graph_smash_damage_percent_5_32")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b7844)
                .WithStatModifier((StatType)13, (ModifierType)4, Native_15b784c, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7860)
                .WithSimulationSettings(6, 1.0f, 85, 0.3499999940395355f));
            // Original constructor call 0x1595e98
            definitions.Add(new UpgradeDef("graph_souls_drop_epic_amount_5_33")
                .WithMaxLevel(1)
                .WithCost("soul.divine", 1.0d)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b7870, "huge")
                .WithSimulationSettings(1, 0.30000001192092896f, 1, 1.0f));
            // Original constructor call 0x1595fe8
            definitions.Add(new UpgradeDef("graph_smash_defence_flat_5_34")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b7878)
                .WithStatModifier((StatType)20, (ModifierType)0, Native_15b7880, 0)
                .WithSimulationSettings(8, 1.0f, 65, 0.30000001192092896f));
            // Original constructor call 0x15961a4
            definitions.Add(new UpgradeDef("graph_map_spawner_drop_island9_5_35")
                .WithMaxLevel(5)
                .WithCost("soul.great", 4.0d)
                .WithModifier("stat_drop", (ModifierType)0, Native_15b7888, 9)
                .WithDisplay((UpgradeValueFormat)1, Native_15b78a0)
                .WithSimulationSettings(3, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x159638c
            definitions.Add(new UpgradeDef("graph_map_spawner_drop_island8_5_36")
                .WithMaxLevel(5)
                .WithCost("soul.great", 4.0d)
                .WithModifier("stat_drop", (ModifierType)0, Native_15b78b0, 8)
                .WithDisplay((UpgradeValueFormat)1, Native_15b78c8)
                .WithSimulationSettings(3, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x1596574
            definitions.Add(new UpgradeDef("graph_map_spawner_drop_island7_5_37")
                .WithMaxLevel(5)
                .WithCost("soul.great", 4.0d)
                .WithModifier("stat_drop", (ModifierType)0, Native_15b78d8, 7)
                .WithDisplay((UpgradeValueFormat)1, Native_15b78f0)
                .WithSimulationSettings(3, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x159675c
            definitions.Add(new UpgradeDef("graph_souls_drop_common_amount_5_38")
                .WithMaxLevel(2)
                .WithCost("soul.large", Native_15b7900)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b7908, "common")
                .WithSimulationSettings(2, 0.6000000238418579f, 1, 1.0f));
            // Original constructor call 0x1596924
            definitions.Add(new UpgradeDef("graph_souls_drop_uncommon_amount_5_39")
                .WithMaxLevel(1)
                .WithCost("soul.elite", Native_15b7910)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b7918, "large")
                .WithSimulationSettings(1, 1.0f, 1, 1.0f));
            // Original constructor call 0x1596aec
            definitions.Add(new UpgradeDef("graph_economy_item_abilities_chance_5_40")
                .WithMaxLevel(2)
                .WithCost("soul.huge", Native_15b7920)
                .WithModifier("economy_item_abilities_chance", (ModifierType)0, Native_15b7930, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7944)
                .WithSimulationSettings(2, 1.0f, 60, 0.20000000298023224f));
            // Original constructor call 0x1596d5c
            definitions.Add(new UpgradeDef("graph_cut_damage_flat_5_41")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b7954)
                .WithStatModifier((StatType)8, (ModifierType)0, Native_15b795c, 0)
                .WithSimulationSettings(8, 1.0f, 65, 0.30000001192092896f));
            // Original constructor call 0x1596f18
            definitions.Add(new UpgradeDef("graph_crystal_sell_price_bonus_5_42")
                .WithMaxLevel(3)
                .WithCost("soul.elite", Native_15b7964)
                .WithModifier("crystal_sell_price_bonus", (ModifierType)0, Native_15b79c4, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b79d8)
                .WithSimulationSettings(2, 1.0f, 17, 0.20000000298023224f));
            // Original constructor call 0x1597180
            definitions.Add(new UpgradeDef("graph_map_spawner_timer_reduce_5_43")
                .WithMaxLevel(1)
                .WithCost("soul.elite", Native_15b79e8)
                .WithModifier("map_spawner_timer_reduce", (ModifierType)0, Native_15b79f0, 7)
                .WithDisplay((UpgradeValueFormat)3, 7, null)
                .WithSimulationSettings(1, 0.30000001192092896f, 1, 1.0f));
            // Original constructor call 0x1597360
            definitions.Add(new UpgradeDef("graph_items_drop_uncommon_5_44")
                .WithMaxLevel(3)
                .WithCost("soul.large", Native_15b79fc)
                .WithModifier("items_rarity_drop_amount", (ModifierType)0, Native_15b7a04, 2)
                .WithSimulationSettings(2, 0.6000000238418579f, 1, 1.0f));
            // Original constructor call 0x1597534
            definitions.Add(new UpgradeDef("graph_items_charges_uncommon_5_45")
                .WithMaxLevel(3)
                .WithCost("soul.common", Native_15b7a0c)
                .WithModifier("items_rarity_charges", (ModifierType)0, Native_15b7a14, 2)
                .WithSimulationSettings(2, 0.6000000238418579f, 1, 1.0f));
            // Original constructor call 0x1597708
            definitions.Add(new UpgradeDef("graph_health_regen_percent_5_46")
                .WithMaxLevel(10)
                .WithCost("soul.great", 1.0d)
                .WithStatModifier((StatType)2, (ModifierType)4, Native_15b7a1c, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7a30)
                .WithSimulationSettings(5, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x15978e4
            definitions.Add(new UpgradeDef("graph_smash_damage_percent_5_47")
                .WithMaxLevel(10)
                .WithCost("soul.great", 1.0d)
                .WithStatModifier((StatType)13, (ModifierType)4, Native_15b7a40, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7a54)
                .WithSimulationSettings(5, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x1597ac0
            definitions.Add(new UpgradeDef("graph_pierce_defence_percent_5_48")
                .WithMaxLevel(10)
                .WithCost("soul.great", 1.0d)
                .WithStatModifier((StatType)15, (ModifierType)4, Native_15b7a64, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7a78)
                .WithSimulationSettings(5, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x1597c9c
            definitions.Add(new UpgradeDef("graph_smash_defence_percent_5_49")
                .WithMaxLevel(10)
                .WithCost("soul.great", 1.0d)
                .WithStatModifier((StatType)21, (ModifierType)4, Native_15b7a88, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7a9c)
                .WithSimulationSettings(5, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x1597e78
            definitions.Add(new UpgradeDef("graph_health_percent_5_50")
                .WithMaxLevel(10)
                .WithCost("soul.great", 1.0d)
                .WithStatModifier((StatType)1, (ModifierType)4, Native_15b7aac, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7ac0)
                .WithSimulationSettings(5, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x1598054
            definitions.Add(new UpgradeDef("graph_cut_damage_percent_5_51")
                .WithMaxLevel(10)
                .WithCost("soul.great", 1.0d)
                .WithStatModifier((StatType)9, (ModifierType)4, Native_15b7ad0, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7ae4)
                .WithSimulationSettings(5, 1.0f, 16, 0.30000001192092896f));
            // Original constructor call 0x1598230
            definitions.Add(new UpgradeDef("graph_map_spawner_timer_reduce_island8_5_52")
                .WithMaxLevel(1)
                .WithCost("soul.elite", Native_15b7af4)
                .WithModifier("map_spawner_timer_reduce", (ModifierType)0, Native_15b7afc, 8)
                .WithDisplay((UpgradeValueFormat)3, 8, null)
                .WithSimulationSettings(1, 0.25f, 40, 0.4000000059604645f));
            // Original constructor call 0x1598410
            definitions.Add(new UpgradeDef("graph_map_spawner_timer_reduce_island9_5_53")
                .WithMaxLevel(1)
                .WithCost("soul.elite", Native_15b7b08)
                .WithModifier("map_spawner_timer_reduce", (ModifierType)0, Native_15b7b10, 9)
                .WithDisplay((UpgradeValueFormat)3, 9, null)
                .WithSimulationSettings(1, 0.30000001192092896f, 40, 0.4000000059604645f));
            // Original constructor call 0x15985f0
            definitions.Add(new UpgradeDef("graph_crystal_enchantment_5_54")
                .WithMaxLevel(1)
                .WithCost("soul.huge", 10.0d)
                .WithModifier("crystal_enchant_extra_attempts", (ModifierType)0, Native_15b7b1c, 1)
                .WithDisplay((UpgradeValueFormat)2, 1, null)
                .WithSimulationSettings(1, 1.0f, 60, 0.20000000298023224f));
            // Original constructor call 0x1598750
            definitions.Add(new UpgradeDef("graph_weak_spawner_extra_range_island7_5_55")
                .WithMaxLevel(1)
                .WithCost("soul.large", Native_15b7b24)
                .WithModifier("weak_spawner_extra_range", (ModifierType)0, Native_15b7b2c, 7)
                .WithSimulationSettings(1, 0.30000001192092896f, 1, 1.0f));
            // Original constructor call 0x1598918
            definitions.Add(new UpgradeDef("graph_weak_spawner_extra_range_island8_5_56")
                .WithMaxLevel(1)
                .WithCost("soul.large", Native_15b7b34)
                .WithModifier("weak_spawner_extra_range", (ModifierType)0, Native_15b7b3c, 8)
                .WithSimulationSettings(1, 0.25f, 1, 1.0f));
            // Original constructor call 0x1598ae0
            definitions.Add(new UpgradeDef("graph_weak_spawner_extra_range_island9_5_57")
                .WithMaxLevel(1)
                .WithCost("soul.large", Native_15b7b44)
                .WithModifier("weak_spawner_extra_range", (ModifierType)0, Native_15b7b4c, 9)
                .WithSimulationSettings(1, 0.30000001192092896f, 1, 1.0f));
            // Original constructor call 0x1598ca8
            definitions.Add(new UpgradeDef("graph_boss_tower_legendary_drop_5_58")
                .WithMaxLevel(1)
                .WithCost("soul.huge", Native_15b7b54)
                .WithModifier("tower_drop_legend_per_floor", (ModifierType)0, Native_15b7b64, "hydra")
                .WithSimulationSettings(1, 0.800000011920929f, 17, 0.20000000298023224f));
            // Original constructor call 0x1598e80
            definitions.Add(new UpgradeDef("graph_boss_tower_legendary_drop_5_59")
                .WithMaxLevel(1)
                .WithCost("soul.huge", Native_15b7b6c)
                .WithModifier("tower_drop_legend_per_floor", (ModifierType)0, Native_15b7b7c, "lava_crab")
                .WithSimulationSettings(1, 0.800000011920929f, 17, 0.20000000298023224f));
            // Original constructor call 0x1599058
            definitions.Add(new UpgradeDef("graph_boss_tower_legendary_drop_5_60")
                .WithMaxLevel(1)
                .WithCost("soul.huge", Native_15b7b84)
                .WithModifier("tower_drop_legend_per_floor", (ModifierType)0, Native_15b7b94, "vikings")
                .WithSimulationSettings(1, 0.800000011920929f, 17, 0.20000000298023224f));
            // Original constructor call 0x1599230
            definitions.Add(new UpgradeDef("graph_items_charges_legendary_6_1")
                .WithMaxLevel(1)
                .WithCost("soul.huge", 30.0d)
                .WithModifier("items_rarity_charges", (ModifierType)0, Native_15b7b9c, 5));
            // Original constructor call 0x1599368
            definitions.Add(new UpgradeDef("graph_crystals_filter_unlock_6_2")
                .WithMaxLevel(1)
                .WithCost("soul.huge", 10.0d)
                .WithModifier("feature_unlock", (ModifierType)0, 1.0d, "crystals_filter")
                .WithSimulationSettings(1, 0.30000001192092896f, 1, 1.0f));
            // Original constructor call 0x1599428
            definitions.Add(new UpgradeDef("graph_crystal_blacksmithing_6_3")
                .WithMaxLevel(4)
                .WithCost("soul.large", Native_15b7ba4)
                .WithModifier("crystal_blacksmithing", (ModifierType)0, Native_15b7bac, 0)
                .WithSimulationSettings(3, 1.0f, 17, 0.20000000298023224f));
            // Original constructor call 0x15995f0
            definitions.Add(new UpgradeDef("graph_smash_damage_flat_6_4")
                .WithMaxLevel(10)
                .WithCost("soul.elite", Native_15b7bb4)
                .WithStatModifier((StatType)12, (ModifierType)0, Native_15b7bbc, 0)
                .WithSimulationSettings(4, 0.44999998807907104f, 130, 0.3499999940395355f));
            // Original constructor call 0x15997bc
            definitions.Add(new UpgradeDef("graph_smash_damage_percent_6_5")
                .WithMaxLevel(20)
                .WithCost("soul.huge", Native_15b7bc4)
                .WithStatModifier((StatType)13, (ModifierType)4, Native_15b7bd4, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7be8)
                .WithSimulationSettings(10, 0.30000001192092896f, 17, 0.20000000298023224f));
            // Original constructor call 0x1599a20
            definitions.Add(new UpgradeDef("graph_lightning_damage_6_6")
                .WithMaxLevel(3)
                .WithCost("soul.divine", Native_15b7bf8)
                .WithCost("soul.chaotic", Native_15b7c0c)
                .WithModifier("lightning_damage", (ModifierType)0, Native_15b7c20, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7c2c)
                .WithSimulationSettings(8, 1.0f, 60, 0.20000000298023224f));
            // Original constructor call 0x1599d4c
            definitions.Add(new UpgradeDef("graph_crystal_enchantment_6_7")
                .WithMaxLevel(1)
                .WithCost("soul.common", Native_15b7c3c)
                .WithModifier("crystal_enchantment", (ModifierType)0, Native_15b7c44, 0)
                .WithSimulationSettings(2, 1.0f, 17, 0.20000000298023224f));
            // Original constructor call 0x1599f14
            definitions.Add(new UpgradeDef("graph_crystal_max_geodes_6_8")
                .WithMaxLevel(3)
                .WithCost("soul.elite", Native_15b7c4c)
                .WithModifier("crystal_max_geodes", (ModifierType)0, Native_15b7ca8, 0)
                .WithSimulationSettings(2, 1.0f, 17, 0.20000000298023224f));
            // Original constructor call 0x159a0dc
            definitions.Add(new UpgradeDef("graph_crystal_geode_timer_6_9")
                .WithMaxLevel(4)
                .WithCost("soul.great", 6.0d)
                .WithModifier("crystal_geode_timer", (ModifierType)0, Native_15b7cb0, 0)
                .WithDisplay((UpgradeValueFormat)4, null)
                .WithSimulationSettings(2, 0.800000011920929f, 17, 0.20000000298023224f));
            // Original constructor call 0x159a238
            definitions.Add(new UpgradeDef("graph_pierce_defence_flat_6_10")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b7cb8)
                .WithStatModifier((StatType)14, (ModifierType)0, Native_15b7cc0, 0)
                .WithSimulationSettings(6, 0.4000000059604645f, 105, 0.2800000011920929f));
            // Original constructor call 0x159a400
            definitions.Add(new UpgradeDef("graph_pierce_damage_flat_6_11")
                .WithMaxLevel(10)
                .WithCost("soul.elite", Native_15b7cc8)
                .WithStatModifier((StatType)6, (ModifierType)0, Native_15b7cd0, 0)
                .WithSimulationSettings(4, 0.44999998807907104f, 120, 0.3499999940395355f));
            // Original constructor call 0x159a5bc
            definitions.Add(new UpgradeDef("graph_pierce_damage_percent_6_12")
                .WithMaxLevel(20)
                .WithCost("soul.huge", Native_15b7cd8)
                .WithStatModifier((StatType)7, (ModifierType)4, Native_15b7ce8, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7cfc)
                .WithSimulationSettings(10, 0.30000001192092896f, 17, 0.20000000298023224f));
            // Original constructor call 0x159a820
            definitions.Add(new UpgradeDef("graph_crit_damage_percent_6_13")
                .WithMaxLevel(5)
                .WithCost("soul.great", 2.0d)
                .WithStatModifier((StatType)25, (ModifierType)6, Native_15b7d0c, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7d20)
                .WithSimulationSettings(2, 1.0f, 17, 0.20000000298023224f));
            // Original constructor call 0x159a9fc
            definitions.Add(new UpgradeDef("graph_items_charges_rare_6_14")
                .WithMaxLevel(2)
                .WithCost("soul.common", Native_15b7d30)
                .WithModifier("items_rarity_charges", (ModifierType)0, Native_15b7d8c, 3)
                .WithSimulationSettings(1, 0.3499999940395355f, 1, 1.0f));
            // Original constructor call 0x159abd0
            definitions.Add(new UpgradeDef("graph_chest_level_6_15")
                .WithMaxLevel(1)
                .WithCost("soul.large", Native_15b7d94)
                .WithModifier("chest_level", (ModifierType)0, Native_15b7d9c, 0)
                .WithSimulationSettings(1, 0.20000000298023224f, 17, 0.20000000298023224f));
            // Original constructor call 0x159ad98
            definitions.Add(new UpgradeDef("graph_smash_damage_flat_6_16")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b7da4)
                .WithStatModifier((StatType)12, (ModifierType)0, Native_15b7dac, 0)
                .WithSimulationSettings(6, 0.4000000059604645f, 105, 0.2800000011920929f));
            // Original constructor call 0x159af54
            definitions.Add(new UpgradeDef("graph_boss_tower_weakness_6_17")
                .WithMaxLevel(1)
                .WithCost("soul.divine", Native_15b7db4)
                .WithModifier("tower_boss_weakness", (ModifierType)0, Native_15b7dbc, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7dd4)
                .WithSimulationSettings(1, 0.800000011920929f, 17, 0.20000000298023224f));
            // Original constructor call 0x159b1c4
            definitions.Add(new UpgradeDef("graph_pierce_defence_flat_6_18")
                .WithMaxLevel(10)
                .WithCost("soul.elite", Native_15b7de4)
                .WithStatModifier((StatType)14, (ModifierType)0, Native_15b7dec, 0)
                .WithSimulationSettings(4, 0.44999998807907104f, 130, 0.3499999940395355f));
            // Original constructor call 0x159b380
            definitions.Add(new UpgradeDef("graph_pierce_defence_percent_6_19")
                .WithMaxLevel(20)
                .WithCost("soul.huge", Native_15b7df4)
                .WithStatModifier((StatType)15, (ModifierType)4, Native_15b7e04, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7e18)
                .WithSimulationSettings(10, 0.30000001192092896f, 17, 0.20000000298023224f));
            // Original constructor call 0x159b5e4
            definitions.Add(new UpgradeDef("graph_boss_tower_items_drop_6_20")
                .WithMaxLevel(3)
                .WithCost("soul.huge", Native_15b7e28)
                .WithModifier("boss_tower_items_drop", (ModifierType)0, Native_15b7e38, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7e4c)
                .WithSimulationSettings(1, 0.800000011920929f, 17, 0.20000000298023224f));
            // Original constructor call 0x159b854
            definitions.Add(new UpgradeDef("graph_crystal_chest_ench_level_6_21")
                .WithMaxLevel(1)
                .WithCost("soul.chaotic", 1.0d)
                .WithModifier("crystal_chest_ench_level", (ModifierType)0, Native_15b7e5c, 0)
                .WithSimulationSettings(1, 0.20000000298023224f, 17, 0.20000000298023224f));
            // Original constructor call 0x159b99c
            definitions.Add(new UpgradeDef("graph_health_regen_flat_6_22")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b7e64)
                .WithStatModifier((StatType)2, (ModifierType)0, Native_15b7e6c, 0)
                .WithSimulationSettings(6, 0.4000000059604645f, 105, 0.2800000011920929f));
            // Original constructor call 0x159bb58
            definitions.Add(new UpgradeDef("graph_boss_tower_vampire_6_23")
                .WithMaxLevel(3)
                .WithCost("soul.huge", Native_15b7e74)
                .WithModifier("boss_tower_vampire", (ModifierType)0, Native_15b7e84, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7e98)
                .WithSimulationSettings(1, 0.800000011920929f, 17, 0.20000000298023224f));
            // Original constructor call 0x159bdc8
            definitions.Add(new UpgradeDef("graph_boss_tower_lich_6_24")
                .WithMaxLevel(3)
                .WithCost("soul.huge", Native_15b7ea8)
                .WithModifier("boss_tower_lich", (ModifierType)0, Native_15b7eb8, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7ed0)
                .WithSimulationSettings(1, 0.800000011920929f, 17, 0.20000000298023224f));
            // Original constructor call 0x159c038
            definitions.Add(new UpgradeDef("graph_lifesteal_flat_6_25")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b7ee0)
                .WithStatModifier((StatType)22, (ModifierType)0, Native_15b7ee8, 0)
                .WithSimulationSettings(6, 0.4000000059604645f, 90, 0.2800000011920929f));
            // Original constructor call 0x159c1f4
            definitions.Add(new UpgradeDef("graph_pierce_damage_flat_6_26")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b7ef0)
                .WithStatModifier((StatType)6, (ModifierType)0, Native_15b7ef8, 0)
                .WithSimulationSettings(6, 0.4000000059604645f, 90, 0.2800000011920929f));
            // Original constructor call 0x159c3b0
            definitions.Add(new UpgradeDef("graph_cut_defence_flat_6_27")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b7f00)
                .WithStatModifier((StatType)16, (ModifierType)0, Native_15b7f08, 0)
                .WithSimulationSettings(6, 0.4000000059604645f, 90, 0.2800000011920929f));
            // Original constructor call 0x159c56c
            definitions.Add(new UpgradeDef("graph_crit_chance_6_28")
                .WithMaxLevel(3)
                .WithCost("soul.great", 4.0d)
                .WithStatModifier((StatType)24, (ModifierType)6, Native_15b7f10, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7f24)
                .WithSimulationSettings(2, 1.0f, 17, 0.20000000298023224f));
            // Original constructor call 0x159c74c
            definitions.Add(new UpgradeDef("graph_extra_mark_6_29")
                .WithMaxLevel(3)
                .WithCost("soul.divine", Native_15b7f34)
                .WithCost("soul.chaotic", Native_15b7f48)
                .WithModifier("extra_mark", (ModifierType)0, Native_15b7f5c, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b7f74)
                .WithSimulationSettings(8, 1.0f, 60, 0.20000000298023224f));
            // Original constructor call 0x159ca78
            definitions.Add(new UpgradeDef("graph_souls_drop_common_amount_6_30")
                .WithMaxLevel(2)
                .WithCost("soul.common", Native_15b7f84)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b7fe0, "common")
                .WithSimulationSettings(2, 0.3499999940395355f, 17, 0.20000000298023224f));
            // Original constructor call 0x159cc40
            definitions.Add(new UpgradeDef("graph_souls_drop_rare_amount_6_31")
                .WithMaxLevel(1)
                .WithCost("soul.elite", Native_15b7fe8)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b7ff0, "elite")
                .WithSimulationSettings(1, 1.0f, 17, 0.20000000298023224f));
            // Original constructor call 0x159ce08
            definitions.Add(new UpgradeDef("graph_souls_drop_uncommon_amount_6_32")
                .WithMaxLevel(1)
                .WithCost("soul.large", Native_15b7ff8)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b8000, "large")
                .WithSimulationSettings(1, 0.5f, 17, 0.20000000298023224f));
            // Original constructor call 0x159cfd0
            definitions.Add(new UpgradeDef("graph_health_regen_flat_6_33")
                .WithMaxLevel(10)
                .WithCost("soul.elite", Native_15b8008)
                .WithStatModifier((StatType)2, (ModifierType)0, Native_15b8010, 0)
                .WithSimulationSettings(4, 0.44999998807907104f, 130, 0.3499999940395355f));
            // Original constructor call 0x159d18c
            definitions.Add(new UpgradeDef("graph_health_regen_percent_6_34")
                .WithMaxLevel(20)
                .WithCost("soul.huge", Native_15b8018)
                .WithStatModifier((StatType)2, (ModifierType)4, Native_15b8028, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b803c)
                .WithSimulationSettings(10, 0.30000001192092896f, 17, 0.20000000298023224f));
            // Original constructor call 0x159d3f0
            definitions.Add(new UpgradeDef("graph_block_to_supercrit_6_35")
                .WithMaxLevel(3)
                .WithCost("soul.divine", Native_15b804c)
                .WithCost("soul.chaotic", Native_15b8060)
                .WithModifier("block_to_supercrit", (ModifierType)0, Native_15b8074, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b8088)
                .WithSimulationSettings(8, 1.0f, 60, 0.20000000298023224f));
            // Original constructor call 0x159d71c
            definitions.Add(new UpgradeDef("graph_items_drop_rare_6_36")
                .WithMaxLevel(2)
                .WithCost("soul.large", Native_15b8098)
                .WithModifier("items_rarity_drop_amount", (ModifierType)0, Native_15b80a0, 3)
                .WithSimulationSettings(2, 1.0f, 1, 1.0f));
            // Original constructor call 0x159d8f0
            definitions.Add(new UpgradeDef("graph_lifesteal_flat_6_37")
                .WithMaxLevel(10)
                .WithCost("soul.elite", Native_15b80a8)
                .WithStatModifier((StatType)22, (ModifierType)0, Native_15b80b0, 0)
                .WithSimulationSettings(4, 0.44999998807907104f, 120, 0.3499999940395355f));
            // Original constructor call 0x159daac
            definitions.Add(new UpgradeDef("graph_lifesteal_percent_6_38")
                .WithMaxLevel(20)
                .WithCost("soul.huge", Native_15b80b8)
                .WithStatModifier((StatType)22, (ModifierType)4, Native_15b80c8, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b80dc)
                .WithSimulationSettings(10, 0.30000001192092896f, 17, 0.20000000298023224f));
            // Original constructor call 0x159dd10
            definitions.Add(new UpgradeDef("graph_cut_defence_flat_6_39")
                .WithMaxLevel(10)
                .WithCost("soul.elite", Native_15b80ec)
                .WithStatModifier((StatType)16, (ModifierType)0, Native_15b80f4, 0)
                .WithSimulationSettings(4, 0.44999998807907104f, 120, 0.3499999940395355f));
            // Original constructor call 0x159decc
            definitions.Add(new UpgradeDef("graph_cut_defence_percent_6_40")
                .WithMaxLevel(20)
                .WithCost("soul.huge", Native_15b80fc)
                .WithStatModifier((StatType)17, (ModifierType)4, Native_15b810c, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b8120)
                .WithSimulationSettings(10, 0.30000001192092896f, 17, 0.20000000298023224f));
            // Original constructor call 0x159e138
            definitions.Add(new UpgradeDef("graph_evasion_percent_6_41")
                .WithMaxLevel(3)
                .WithCost("soul.great", 5.0d)
                .WithStatModifier((StatType)23, (ModifierType)6, Native_15b8130, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b8144)
                .WithSimulationSettings(2, 1.0f, 17, 0.20000000298023224f));
            // Original constructor call 0x159e314
            definitions.Add(new UpgradeDef("graph_crystal_sell_price_bonus_6_42")
                .WithMaxLevel(3)
                .WithCost("soul.common", Native_15b8154)
                .WithModifier("crystal_sell_price_bonus", (ModifierType)0, Native_15b81b4, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b81c8)
                .WithSimulationSettings(2, 1.0f, 17, 0.20000000298023224f));
            // Original constructor call 0x159e57c
            definitions.Add(new UpgradeDef("graph_crystal_enchantment_6_43")
                .WithMaxLevel(2)
                .WithCost("soul.huge", Native_15b81d8)
                .WithModifier("crystal_enchant_extra_attempts", (ModifierType)0, Native_15b8244, 2)
                .WithDisplay((UpgradeValueFormat)2, 2, null)
                .WithSimulationSettings(2, 1.0f, 60, 0.20000000298023224f));
            // Original constructor call 0x159e76c
            definitions.Add(new UpgradeDef("graph_crystal_enchantment_stones_6_45")
                .WithMaxLevel(1)
                .WithCost("soul.chaotic", 1.0d)
                .WithModifier("crystal_max_enchant_stones", (ModifierType)0, Native_15b824c, 0)
                .WithSimulationSettings(1, 1.0f, 60, 0.20000000298023224f));
            // Original constructor call 0x159e8ac
            definitions.Add(new UpgradeDef("graph_super_crit_chance_6_46")
                .WithMaxLevel(1)
                .WithCost("soul.divine", 1.0d)
                .WithStatModifier((StatType)28, (ModifierType)6, Native_15b8254, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b826c)
                .WithSimulationSettings(10, 0.30000001192092896f, 17, 0.20000000298023224f));
            // Original constructor call 0x159ea88
            definitions.Add(new UpgradeDef("graph_super_crit_damage_percent_6_47")
                .WithMaxLevel(1)
                .WithCost("soul.divine", 1.0d)
                .WithStatModifier((StatType)29, (ModifierType)6, Native_15b827c, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b8294)
                .WithSimulationSettings(10, 0.30000001192092896f, 17, 0.20000000298023224f));
            // Original constructor call 0x159ec64
            definitions.Add(new UpgradeDef("graph_event_golem_damage_6_48")
                .WithMaxLevel(1)
                .WithCost("soul.elite", Native_15b82a4)
                .WithModifier("event_golem_damage", (ModifierType)0, 0.5d, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b82ac)
                .WithSimulationSettings(1, 0.3499999940395355f, 1, 1.0f));
            // Original constructor call 0x159ee3c
            definitions.Add(new UpgradeDef("graph_boss_tower_tickets_7_1")
                .WithMaxLevel(2)
                .WithCost("soul.huge", Native_15b82bc)
                .WithModifier("boss_tower_tickets", (ModifierType)0, Native_15b8328, 0));
            // Original constructor call 0x159eff0
            definitions.Add(new UpgradeDef("graph_map_spawner_timer_reduce_7_2")
                .WithMaxLevel(1)
                .WithCost("soul.elite", Native_15b8330)
                .WithModifier("map_spawner_timer_reduce", (ModifierType)0, Native_15b8338, 10)
                .WithDisplay((UpgradeValueFormat)3, 10, null)
                .WithSimulationSettings(1, 0.15000000596046448f, 140, 0.20000000298023224f));
            // Original constructor call 0x159f1d0
            definitions.Add(new UpgradeDef("graph_cut_damage_flat_7_3")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b8344)
                .WithStatModifier((StatType)8, (ModifierType)0, Native_15b834c, 0)
                .WithSimulationSettings(7, 0.5f, 130, 0.25f));
            // Original constructor call 0x159f38c
            definitions.Add(new UpgradeDef("graph_smash_defence_flat_7_4")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b8354)
                .WithStatModifier((StatType)20, (ModifierType)0, Native_15b835c, 0)
                .WithSimulationSettings(6, 0.5f, 150, 0.25f));
            // Original constructor call 0x159f548
            definitions.Add(new UpgradeDef("graph_smash_defence_percent_7_5")
                .WithMaxLevel(20)
                .WithCost("soul.huge", Native_15b8364)
                .WithStatModifier((StatType)21, (ModifierType)4, Native_15b8374, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b8388)
                .WithSimulationSettings(6, 0.5f, 180, 0.25f));
            // Original constructor call 0x159f7ac
            definitions.Add(new UpgradeDef("graph_souls_drop_uncommon_amount_7_6")
                .WithMaxLevel(1)
                .WithCost("soul.elite", Native_15b8398)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b83f4, "large")
                .WithSimulationCostShare(0.5f)
                .WithSimulationSettings(1, 0.800000011920929f, 1, 1.0f));
            // Original constructor call 0x159f98c
            definitions.Add(new UpgradeDef("graph_pierce_damage_flat_7_7")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b83fc)
                .WithStatModifier((StatType)6, (ModifierType)0, Native_15b8404, 0)
                .WithSimulationSettings(6, 0.5f, 180, 0.25f));
            // Original constructor call 0x159fb50
            definitions.Add(new UpgradeDef("graph_pierce_damage_percent_7_8")
                .WithMaxLevel(20)
                .WithCost("soul.huge", Native_15b840c)
                .WithStatModifier((StatType)7, (ModifierType)4, Native_15b841c, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b8430)
                .WithSimulationSettings(6, 0.5f, 180, 0.20000000298023224f));
            // Original constructor call 0x159fdc0
            definitions.Add(new UpgradeDef("graph_map_spawner_drop_island10_7_9")
                .WithMaxLevel(5)
                .WithCost("soul.great", 4.0d)
                .WithModifier("stat_drop", (ModifierType)0, Native_15b8440, 10)
                .WithDisplay((UpgradeValueFormat)1, Native_15b8458)
                .WithSimulationSettings(3, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x159ffb0
            definitions.Add(new UpgradeDef("graph_map_spawner_drop_island11_7_10")
                .WithMaxLevel(5)
                .WithCost("soul.great", 5.0d)
                .WithModifier("stat_drop", (ModifierType)0, Native_15b8468, 11)
                .WithDisplay((UpgradeValueFormat)1, Native_15b8480)
                .WithSimulationSettings(3, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x15a01a0
            definitions.Add(new UpgradeDef("graph_souls_drop_rare_amount_7_11")
                .WithMaxLevel(1)
                .WithCost("soul.common", Native_15b8490)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b84ec, "elite")
                .WithSimulationCostShare(0.0f)
                .WithSimulationSettings(1, 0.800000011920929f, 1, 1.0f));
            // Original constructor call 0x15a0380
            definitions.Add(new UpgradeDef("graph_map_spawner_drop_island12_7_12")
                .WithMaxLevel(5)
                .WithCost("soul.great", 5.0d)
                .WithModifier("stat_drop", (ModifierType)0, Native_15b84f4, 12)
                .WithDisplay((UpgradeValueFormat)1, Native_15b850c)
                .WithSimulationSettings(3, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x15a0570
            definitions.Add(new UpgradeDef("graph_map_spawner_drop_island13_7_13")
                .WithMaxLevel(5)
                .WithCost("soul.great", 5.0d)
                .WithModifier("stat_drop", (ModifierType)0, Native_15b851c, 13)
                .WithDisplay((UpgradeValueFormat)1, Native_15b8534)
                .WithSimulationSettings(3, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x15a0760
            definitions.Add(new UpgradeDef("graph_map_spawner_drop_island14_7_14")
                .WithMaxLevel(5)
                .WithCost("soul.great", 5.0d)
                .WithModifier("stat_drop", (ModifierType)0, Native_15b8544, 14)
                .WithDisplay((UpgradeValueFormat)1, Native_15b855c)
                .WithSimulationSettings(3, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x15a0950
            definitions.Add(new UpgradeDef("graph_cut_defence_flat_7_15")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b856c)
                .WithStatModifier((StatType)16, (ModifierType)0, Native_15b8574, 0)
                .WithSimulationSettings(6, 0.5f, 180, 0.20000000298023224f));
            // Original constructor call 0x15a0b14
            definitions.Add(new UpgradeDef("graph_cut_defence_percent_7_16")
                .WithMaxLevel(10)
                .WithCost("soul.huge", Native_15b857c)
                .WithStatModifier((StatType)17, (ModifierType)4, Native_15b858c, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b85a0)
                .WithSimulationSettings(6, 0.5f, 180, 0.20000000298023224f));
            // Original constructor call 0x15a0d84
            definitions.Add(new UpgradeDef("graph_souls_drop_common_amount_7_17")
                .WithMaxLevel(4)
                .WithCost("soul.large", Native_15b85b0)
                .WithModifier("souls_rarity_amount", (ModifierType)0, Native_15b85b8, "common")
                .WithSimulationSettings(2, 0.800000011920929f, 1, 1.0f));
            // Original constructor call 0x15a0f54
            definitions.Add(new UpgradeDef("graph_health_flat_7_18")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b85c0)
                .WithStatModifier((StatType)1, (ModifierType)0, Native_15b85c8, 0)
                .WithSimulationSettings(7, 0.5f, 130, 0.25f));
            // Original constructor call 0x15a1118
            definitions.Add(new UpgradeDef("graph_cut_damage_flat_7_19")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b85d0)
                .WithStatModifier((StatType)8, (ModifierType)0, Native_15b85d8, 0)
                .WithSimulationSettings(6, 0.5f, 150, 0.25f));
            // Original constructor call 0x15a12dc
            definitions.Add(new UpgradeDef("graph_cut_damage_percent_7_20")
                .WithMaxLevel(20)
                .WithCost("soul.huge", Native_15b85e0)
                .WithStatModifier((StatType)9, (ModifierType)4, Native_15b85f0, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b8604)
                .WithSimulationSettings(6, 0.5f, 180, 0.20000000298023224f));
            // Original constructor call 0x15a154c
            definitions.Add(new UpgradeDef("graph_rage_extra_hit_7_21")
                .WithMaxLevel(3)
                .WithCost("soul.divine", Native_15b8614)
                .WithCost("soul.chaotic", Native_15b8628)
                .WithModifier("rage_extra_hit", (ModifierType)0, Native_15b863c, 0)
                .WithSimulationSettings(8, 1.0f, 60, 0.20000000298023224f));
            // Original constructor call 0x15a17e4
            definitions.Add(new UpgradeDef("graph_crystal_enchant_efficiency_7_22")
                .WithMaxLevel(5)
                .WithCost("soul.great", Native_15b8650)
                .WithModifier("crystal_enchant_efficiency", (ModifierType)4, Native_15b8658, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b8670)
                .WithSimulationSettings(2, 0.30000001192092896f, 1, 1.0f));
            // Original constructor call 0x15a1a60
            definitions.Add(new UpgradeDef("graph_smash_defence_flat_7_23")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b8680)
                .WithStatModifier((StatType)20, (ModifierType)0, Native_15b8688, 0)
                .WithSimulationSettings(7, 0.5f, 130, 0.25f));
            // Original constructor call 0x15a1c24
            definitions.Add(new UpgradeDef("graph_health_flat_7_24")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b8690)
                .WithStatModifier((StatType)1, (ModifierType)0, Native_15b8698, 0)
                .WithSimulationSettings(6, 0.5f, 150, 0.25f));
            // Original constructor call 0x15a1de8
            definitions.Add(new UpgradeDef("graph_health_percent_7_25")
                .WithMaxLevel(20)
                .WithCost("soul.huge", Native_15b86a0)
                .WithStatModifier((StatType)1, (ModifierType)4, Native_15b86b0, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b86c4)
                .WithSimulationSettings(6, 0.5f, 180, 0.20000000298023224f));
            // Original constructor call 0x15a2058
            definitions.Add(new UpgradeDef("graph_lifesteal_flat_7_26")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b86d4)
                .WithStatModifier((StatType)22, (ModifierType)0, Native_15b86dc, 0)
                .WithSimulationSettings(7, 0.5f, 150, 0.25f));
            // Original constructor call 0x15a221c
            definitions.Add(new UpgradeDef("graph_evasion_mark_7_27")
                .WithMaxLevel(3)
                .WithCost("soul.divine", Native_15b86e4)
                .WithCost("soul.chaotic", Native_15b86f8)
                .WithModifier("evasion_mark", (ModifierType)0, Native_15b870c, 0)
                .WithSimulationSettings(8, 1.0f, 60, 0.20000000298023224f));
            // Original constructor call 0x15a24b4
            definitions.Add(new UpgradeDef("graph_economy_item_abilities_chance_7_28")
                .WithMaxLevel(2)
                .WithCost("soul.great", 10.0d)
                .WithModifier("economy_item_abilities_chance", (ModifierType)0, Native_15b8714, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b8728)
                .WithSimulationSettings(2, 0.30000001192092896f, 1, 1.0f));
            // Original constructor call 0x15a26a4
            definitions.Add(new UpgradeDef("graph_chest_level_7_29")
                .WithMaxLevel(1)
                .WithCost("soul.common", Native_15b8738)
                .WithModifier("chest_level", (ModifierType)0, Native_15b8740, 0)
                .WithSimulationSettings(10, 0.30000001192092896f, 17, 0.20000000298023224f));
            // Original constructor call 0x15a2874
            definitions.Add(new UpgradeDef("graph_crystal_blacksmithing_7_30")
                .WithMaxLevel(4)
                .WithCost("soul.elite", Native_15b8748)
                .WithModifier("crystal_blacksmithing", (ModifierType)0, Native_15b8750, 0)
                .WithSimulationSettings(3, 0.0f, 1, 1.0f));
            // Original constructor call 0x15a2a44
            definitions.Add(new UpgradeDef("graph_crystal_enchantment_7_31")
                .WithMaxLevel(1)
                .WithCost("soul.large", Native_15b8758)
                .WithModifier("crystal_enchantment", (ModifierType)0, Native_15b8760, 0)
                .WithSimulationSettings(1, 0.8999999761581421f, 1, 1.0f));
            // Original constructor call 0x15a2c18
            definitions.Add(new UpgradeDef("graph_cut_defence_flat_7_32")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b8768)
                .WithStatModifier((StatType)16, (ModifierType)0, Native_15b8770, 0)
                .WithSimulationSettings(7, 0.5f, 150, 0.25f));
            // Original constructor call 0x15a2ddc
            definitions.Add(new UpgradeDef("graph_lifesteal_flat_7_33")
                .WithMaxLevel(10)
                .WithCost("soul.common", Native_15b8778)
                .WithStatModifier((StatType)22, (ModifierType)0, Native_15b8780, 0)
                .WithSimulationSettings(6, 0.5f, 180, 0.25f));
            // Original constructor call 0x15a2fa8
            definitions.Add(new UpgradeDef("graph_lifesteal_percent_7_34")
                .WithMaxLevel(20)
                .WithCost("soul.huge", Native_15b8788)
                .WithStatModifier((StatType)22, (ModifierType)4, Native_15b8798, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b87ac)
                .WithSimulationSettings(6, 0.5f, 180, 0.20000000298023224f));
            // Original constructor call 0x15a3218
            definitions.Add(new UpgradeDef("graph_boss_tower_damage_reduction_7_35")
                .WithMaxLevel(4)
                .WithCost("soul.elite", Native_15b87bc)
                .WithModifier("tower_damage_reduction", (ModifierType)0, Native_15b87c4, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b87d8)
                .WithSimulationSettings(2, 0.800000011920929f, 1, 1.0f));
            // Original constructor call 0x15a348c
            definitions.Add(new UpgradeDef("graph_boss_tower_vikings_7_36")
                .WithMaxLevel(3)
                .WithCost("soul.huge", Native_15b87e8)
                .WithModifier("boss_tower_vikings", (ModifierType)0, Native_15b87f8, 0)
                .WithSimulationSettings(1, 0.800000011920929f, 17, 0.20000000298023224f));
            // Original constructor call 0x15a3664
            definitions.Add(new UpgradeDef("graph_boss_tower_berserker_7_37")
                .WithMaxLevel(3)
                .WithCost("soul.huge", Native_15b880c)
                .WithModifier("boss_tower_berserker", (ModifierType)0, Native_15b881c, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b8830)
                .WithSimulationSettings(1, 0.800000011920929f, 17, 0.20000000298023224f));
            // Original constructor call 0x15a38e0
            definitions.Add(new UpgradeDef("graph_crystal_timer_on_kill_reduce_7_38")
                .WithMaxLevel(1)
                .WithCost("soul.chaotic", 1.0d)
                .WithModifier("crystal_timer_on_kill_reduce", (ModifierType)0, Native_15b8840, 0)
                .WithSimulationSettings(8, 1.0f, 60, 0.20000000298023224f));
            // Original constructor call 0x15a3a2c
            definitions.Add(new UpgradeDef("graph_boss_tower_legendary_drop_7_39")
                .WithMaxLevel(1)
                .WithCost("soul.huge", Native_15b8868)
                .WithModifier("tower_drop_legend_per_floor", (ModifierType)0, Native_15b8878, "berserker")
                .WithSimulationSettings(1, 0.800000011920929f, 17, 0.20000000298023224f));
            // Original constructor call 0x15a3c04
            definitions.Add(new UpgradeDef("graph_boss_tower_damage_7_40")
                .WithMaxLevel(4)
                .WithCost("soul.elite", Native_15b8880)
                .WithModifier("tower_damage", (ModifierType)0, Native_15b8888, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b889c)
                .WithSimulationSettings(2, 0.800000011920929f, 1, 1.0f));
            // Original constructor call 0x15a3e78
            definitions.Add(new UpgradeDef("graph_pierce_damage_flat_7_41")
                .WithMaxLevel(10)
                .WithCost("soul.large", Native_15b88ac)
                .WithStatModifier((StatType)6, (ModifierType)0, Native_15b88b4, 0)
                .WithSimulationSettings(7, 0.5f, 150, 0.25f));
            // Original constructor call 0x15a403c
            definitions.Add(new UpgradeDef("graph_movement_speed_percent_7_42")
                .WithMaxLevel(3)
                .WithCost("soul.elite", Native_15b88bc)
                .WithStatModifier((StatType)0, (ModifierType)5, Native_15b88c4, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b88d8)
                .WithSimulationSettings(2, 0.800000011920929f, 180, 0.20000000298023224f));
            // Original constructor call 0x15a42a4
            definitions.Add(new UpgradeDef("graph_block_chance_7_43")
                .WithMaxLevel(3)
                .WithCost("soul.great", 5.0d)
                .WithStatModifier((StatType)33, (ModifierType)6, Native_15b88e8, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b88fc)
                .WithSimulationSettings(2, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x15a4488
            definitions.Add(new UpgradeDef("graph_block_amount_7_44")
                .WithMaxLevel(3)
                .WithCost("soul.great", 5.0d)
                .WithStatModifier((StatType)34, (ModifierType)6, Native_15b890c, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b8920)
                .WithSimulationSettings(2, 1.0f, 40, 0.4000000059604645f));
            // Original constructor call 0x15a466c
            definitions.Add(new UpgradeDef("graph_crystal_sell_price_bonus_7_45")
                .WithMaxLevel(4)
                .WithCost("soul.elite", Native_15b8930)
                .WithModifier("crystal_sell_price_bonus", (ModifierType)0, Native_15b8938, 0)
                .WithDisplay((UpgradeValueFormat)1, Native_15b894c)
                .WithSimulationSettings(3, 1.0f, 17, 0.20000000298023224f));
            // Original constructor call 0x15a48e0
            definitions.Add(new UpgradeDef("graph_map_spawner_timer_reduce_island11_7_46")
                .WithMaxLevel(1)
                .WithCost("soul.elite", Native_15b895c)
                .WithModifier("map_spawner_timer_reduce", (ModifierType)0, Native_15b8964, 11)
                .WithDisplay((UpgradeValueFormat)3, 11, null)
                .WithSimulationSettings(1, 0.2199999988079071f, 1, 1.0f));
            // Original constructor call 0x15a4acc
            definitions.Add(new UpgradeDef("graph_map_spawner_timer_reduce_island12_7_47")
                .WithMaxLevel(1)
                .WithCost("soul.elite", Native_15b8970)
                .WithModifier("map_spawner_timer_reduce", (ModifierType)0, Native_15b8978, 12)
                .WithDisplay((UpgradeValueFormat)3, 12, null)
                .WithSimulationSettings(1, 0.15000000596046448f, 1, 1.0f));
            // Original constructor call 0x15a4cb4
            definitions.Add(new UpgradeDef("graph_map_spawner_timer_reduce_island13_7_48")
                .WithMaxLevel(1)
                .WithCost("soul.elite", Native_15b8984)
                .WithModifier("map_spawner_timer_reduce", (ModifierType)0, Native_15b898c, 13)
                .WithDisplay((UpgradeValueFormat)3, 13, null)
                .WithSimulationSettings(1, 0.20000000298023224f, 1, 1.0f));
            // Original constructor call 0x15a4e9c
            definitions.Add(new UpgradeDef("graph_map_spawner_timer_reduce_island14_7_49")
                .WithMaxLevel(1)
                .WithCost("soul.elite", Native_15b8998)
                .WithModifier("map_spawner_timer_reduce", (ModifierType)0, Native_15b89a0, 14)
                .WithDisplay((UpgradeValueFormat)3, 14, null)
                .WithSimulationSettings(1, 0.25f, 1, 1.0f));
            // Original constructor call 0x15a5084
            definitions.Add(new UpgradeDef("graph_crystal_enchantment_7_50")
                .WithMaxLevel(3)
                .WithCost("soul.huge", Native_15b89ac)
                .WithModifier("crystal_enchant_extra_attempts", (ModifierType)0, Native_15b8a18, 3)
                .WithDisplay((UpgradeValueFormat)2, 3, null)
                .WithSimulationSettings(2, 1.0f, 60, 0.20000000298023224f));
            // Original constructor call 0x15a5274
            definitions.Add(new UpgradeDef("graph_weak_spawner_extra_range_island10_7_51")
                .WithMaxLevel(1)
                .WithCost("soul.large", Native_15b8a20)
                .WithModifier("weak_spawner_extra_range", (ModifierType)0, Native_15b8a28, 10)
                .WithSimulationSettings(1, 0.20000000298023224f, 1, 1.0f));
            // Original constructor call 0x15a5444
            definitions.Add(new UpgradeDef("graph_weak_spawner_extra_range_island11_7_52")
                .WithMaxLevel(1)
                .WithCost("soul.large", Native_15b8a30)
                .WithModifier("weak_spawner_extra_range", (ModifierType)0, Native_15b8a38, 11)
                .WithSimulationSettings(1, 0.25f, 1, 1.0f));
            // Original constructor call 0x15a5614
            definitions.Add(new UpgradeDef("graph_weak_spawner_extra_range_island12_7_53")
                .WithMaxLevel(1)
                .WithCost("soul.large", Native_15b8a40)
                .WithModifier("weak_spawner_extra_range", (ModifierType)0, Native_15b8a48, 12)
                .WithSimulationSettings(1, 0.20000000298023224f, 1, 1.0f));
            // Original constructor call 0x15a57e4
            definitions.Add(new UpgradeDef("graph_weak_spawner_extra_range_island13_7_54")
                .WithMaxLevel(1)
                .WithCost("soul.large", Native_15b8a50)
                .WithModifier("weak_spawner_extra_range", (ModifierType)0, Native_15b8a58, 13)
                .WithSimulationCostShare(0.0f)
                .WithSimulationSettings(1, 0.25f, 1, 1.0f));
            // Original constructor call 0x15a59c4
            definitions.Add(new UpgradeDef("graph_weak_spawner_extra_range_island14_7_55")
                .WithMaxLevel(1)
                .WithCost("soul.large", Native_15b8a60)
                .WithModifier("weak_spawner_extra_range", (ModifierType)0, Native_15b8a68, 14)
                .WithSimulationCostShare(0.30000001192092896f)
                .WithSimulationSettings(1, 0.30000001192092896f, 1, 1.0f));
            // Original constructor call 0x15a5bb4
            definitions.Add(new UpgradeDef("graph_boss_tower_legendary_drop_7_56")
                .WithMaxLevel(1)
                .WithCost("soul.huge", Native_15b8a70)
                .WithModifier("tower_drop_legend_per_floor", (ModifierType)0, Native_15b8a80, "vampire_king")
                .WithSimulationSettings(1, 0.800000011920929f, 17, 0.20000000298023224f));
            // Original constructor call 0x15a5d84
            definitions.Add(new UpgradeDef("graph_boss_tower_legendary_drop_7_57")
                .WithMaxLevel(1)
                .WithCost("soul.huge", Native_15b8a88)
                .WithModifier("tower_drop_legend_per_floor", (ModifierType)0, Native_15b8a98, "lich")
                .WithSimulationSettings(1, 0.800000011920929f, 17, 0.20000000298023224f));
            // Original constructor call 0x15a5f5c
            definitions.Add(new UpgradeDef("graph_boss_tower_legendary_drop_7_58")
                .WithMaxLevel(1)
                .WithCost("soul.chaotic", 1.0d)
                .WithModifier("tower_drop_legend_per_floor", (ModifierType)0, 1.0d, "hydra")
                .WithModifier("tower_drop_legend_per_floor", (ModifierType)0, 1.0d, "lava_crab")
                .WithModifier("tower_drop_legend_per_floor", (ModifierType)0, 1.0d, "vikings")
                .WithModifier("tower_drop_legend_per_floor", (ModifierType)0, 1.0d, "berserker")
                .WithModifier("tower_drop_legend_per_floor", (ModifierType)0, 1.0d, "vampire_king")
                .WithModifier("tower_drop_legend_per_floor", (ModifierType)0, 1.0d, "lich")
                .WithSimulationSettings(1, 0.800000011920929f, 17, 0.20000000298023224f));
            return definitions;
        }

        private static string SoulCost(string soulKey) => "soul." + soulKey;
        // Preserve the original S-register rounding on Mono as well as ARM64.
        private static float RoundFloat32(float value) => BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(value));

        // Original ELF VA 0x15b63f4; 2 bounded instructions.
        private static double Native_15b63f4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b63fc; 2 bounded instructions.
        private static double Native_15b63fc(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6404; 2 bounded instructions.
        private static double Native_15b6404(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b640c; 2 bounded instructions.
        private static double Native_15b640c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6414; 2 bounded instructions.
        private static double Native_15b6414(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b641c; 5 bounded instructions.
        private static double Native_15b641c(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b6430; 4 bounded instructions.
        private static double Native_15b6430(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6440; 2 bounded instructions.
        private static double Native_15b6440(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6448; 5 bounded instructions.
        private static double Native_15b6448(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.03d);
        // Original ELF VA 0x15b645c; 4 bounded instructions.
        private static double Native_15b645c(double value) => (value * 100.0d);
        // Original ELF VA 0x15b646c; 2 bounded instructions.
        private static double Native_15b646c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6474; 2 bounded instructions.
        private static double Native_15b6474(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b647c; 23 bounded instructions.
        private static double Native_15b647c(int level) => System.Math.Pow(unchecked((double)(unchecked((int)(unchecked((uint)(unchecked((int)(unchecked((uint)(level)))))))))), 5.0d);
        // Original ELF VA 0x15b64d8; 2 bounded instructions.
        private static double Native_15b64d8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b64e0; 2 bounded instructions.
        private static double Native_15b64e0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b64e8; 2 bounded instructions.
        private static double Native_15b64e8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b64f0; 2 bounded instructions.
        private static double Native_15b64f0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b64f8; 2 bounded instructions.
        private static double Native_15b64f8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6500; 3 bounded instructions.
        private static double Native_15b6500(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) + (unchecked((int)(unchecked((uint)(level)))) << 2))))))));
        // Original ELF VA 0x15b650c; 4 bounded instructions.
        private static double Native_15b650c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) << 1))))) - 1)))))));
        // Original ELF VA 0x15b651c; 3 bounded instructions.
        private static double Native_15b651c(double value) => (value + 1.0d);
        // Original ELF VA 0x15b6528; 23 bounded instructions.
        private static double Native_15b6528(int level) => System.Math.Pow(unchecked((double)(unchecked((int)(unchecked((uint)(unchecked((int)(unchecked((uint)(level)))))))))), 3.0d);
        // Original ELF VA 0x15b6584; 2 bounded instructions.
        private static double Native_15b6584(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b658c; 2 bounded instructions.
        private static double Native_15b658c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6594; 2 bounded instructions.
        private static double Native_15b6594(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b659c; 2 bounded instructions.
        private static double Native_15b659c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b65a4; 2 bounded instructions.
        private static double Native_15b65a4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b65ac; 5 bounded instructions.
        private static double Native_15b65ac(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b65c0; 4 bounded instructions.
        private static double Native_15b65c0(double value) => (value * 100.0d);
        // Original ELF VA 0x15b65d0; 23 bounded instructions.
        private static double Native_15b65d0(int level) => System.Math.Pow(unchecked((double)(unchecked((int)(unchecked((uint)(unchecked((int)(unchecked((uint)(level)))))))))), 1.5d);
        // Original ELF VA 0x15b662c; 2 bounded instructions.
        private static double Native_15b662c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6634; 23 bounded instructions.
        private static double Native_15b6634(int level) => System.Math.Pow(unchecked((double)(unchecked((int)(unchecked((uint)(unchecked((int)(unchecked((uint)(level)))))))))), 1.5d);
        // Original ELF VA 0x15b6690; 2 bounded instructions.
        private static double Native_15b6690(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6698; 2 bounded instructions.
        private static double Native_15b6698(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b66a0; 2 bounded instructions.
        private static double Native_15b66a0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b66a8; 2 bounded instructions.
        private static double Native_15b66a8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b66b0; 5 bounded instructions.
        private static double Native_15b66b0(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b66c4; 4 bounded instructions.
        private static double Native_15b66c4(double value) => (value * 100.0d);
        // Original ELF VA 0x15b66d4; 2 bounded instructions.
        private static double Native_15b66d4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b66dc; 2 bounded instructions.
        private static double Native_15b66dc(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b66e4; 5 bounded instructions.
        private static double Native_15b66e4(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b66f8; 4 bounded instructions.
        private static double Native_15b66f8(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6708; 5 bounded instructions.
        private static double Native_15b6708(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b671c; 4 bounded instructions.
        private static double Native_15b671c(double value) => (value * 100.0d);
        // Original ELF VA 0x15b672c; 5 bounded instructions.
        private static double Native_15b672c(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b6740; 4 bounded instructions.
        private static double Native_15b6740(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6750; 2 bounded instructions.
        private static double Native_15b6750(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6758; 2 bounded instructions.
        private static double Native_15b6758(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6760; 2 bounded instructions.
        private static double Native_15b6760(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6768; 2 bounded instructions.
        private static double Native_15b6768(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6770; 3 bounded instructions.
        private static double Native_15b6770(int level) => 7200.0d;
        // Original ELF VA 0x15b677c; 23 bounded instructions.
        private static double Native_15b677c(int level) => System.Math.Pow(unchecked((double)(unchecked((int)(unchecked((uint)(unchecked((int)(unchecked((uint)(level)))))))))), 5.0d);
        // Original ELF VA 0x15b67d8; 6 bounded instructions.
        private static double Native_15b67d8(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.20000000298023224f)))))));
        // Original ELF VA 0x15b67f0; 4 bounded instructions.
        private static double Native_15b67f0(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6800; 23 bounded instructions.
        private static double Native_15b6800(int level) => System.Math.Pow(unchecked((double)(unchecked((int)(unchecked((uint)(unchecked((int)(unchecked((uint)(level)))))))))), 5.0d);
        // Original ELF VA 0x15b685c; 6 bounded instructions.
        private static double Native_15b685c(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.20000000298023224f)))))));
        // Original ELF VA 0x15b6874; 4 bounded instructions.
        private static double Native_15b6874(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6884; 23 bounded instructions.
        private static double Native_15b6884(int level) => System.Math.Pow(unchecked((double)(unchecked((int)(unchecked((uint)(unchecked((int)(unchecked((uint)(level)))))))))), 5.0d);
        // Original ELF VA 0x15b68e0; 6 bounded instructions.
        private static double Native_15b68e0(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.20000000298023224f)))))));
        // Original ELF VA 0x15b68f8; 4 bounded instructions.
        private static double Native_15b68f8(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6908; 2 bounded instructions.
        private static double Native_15b6908(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6910; 2 bounded instructions.
        private static double Native_15b6910(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6918; 2 bounded instructions.
        private static double Native_15b6918(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6920; 2 bounded instructions.
        private static double Native_15b6920(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6928; 2 bounded instructions.
        private static double Native_15b6928(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6930; 2 bounded instructions.
        private static double Native_15b6930(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6938; 2 bounded instructions.
        private static double Native_15b6938(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6940; 5 bounded instructions.
        private static double Native_15b6940(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b6954; 4 bounded instructions.
        private static double Native_15b6954(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6964; 2 bounded instructions.
        private static double Native_15b6964(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b696c; 2 bounded instructions.
        private static double Native_15b696c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6974; 2 bounded instructions.
        private static double Native_15b6974(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b697c; 5 bounded instructions.
        private static double Native_15b697c(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b6990; 4 bounded instructions.
        private static double Native_15b6990(double value) => (value * 100.0d);
        // Original ELF VA 0x15b69a0; 2 bounded instructions.
        private static double Native_15b69a0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b69a8; 2 bounded instructions.
        private static double Native_15b69a8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b69b0; 3 bounded instructions.
        private static double Native_15b69b0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) << 1)))))));
        // Original ELF VA 0x15b69bc; 2 bounded instructions.
        private static double Native_15b69bc(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b69c4; 2 bounded instructions.
        private static double Native_15b69c4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b69cc; 2 bounded instructions.
        private static double Native_15b69cc(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b69d4; 2 bounded instructions.
        private static double Native_15b69d4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b69dc; 5 bounded instructions.
        private static double Native_15b69dc(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.02d);
        // Original ELF VA 0x15b69f0; 4 bounded instructions.
        private static double Native_15b69f0(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6a00; 2 bounded instructions.
        private static double Native_15b6a00(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6a08; 5 bounded instructions.
        private static double Native_15b6a08(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b6a1c; 4 bounded instructions.
        private static double Native_15b6a1c(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6a2c; 2 bounded instructions.
        private static double Native_15b6a2c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6a34; 2 bounded instructions.
        private static double Native_15b6a34(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6a3c; 23 bounded instructions.
        private static double Native_15b6a3c(int level) => System.Math.Pow(unchecked((double)(unchecked((int)(unchecked((uint)(unchecked((int)(unchecked((uint)(level)))))))))), 2.0d);
        // Original ELF VA 0x15b6a98; 2 bounded instructions.
        private static double Native_15b6a98(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6aa0; 4 bounded instructions.
        private static double Native_15b6aa0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) + (unchecked((int)(unchecked((uint)(level)))) << 2)))))) << 1)))))));
        // Original ELF VA 0x15b6ab0; 4 bounded instructions.
        private static double Native_15b6ab0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) << 1))))) - 1)))))));
        // Original ELF VA 0x15b6ac0; 3 bounded instructions.
        private static double Native_15b6ac0(double value) => (value + 1.0d);
        // Original ELF VA 0x15b6acc; 2 bounded instructions.
        private static double Native_15b6acc(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6ad4; 2 bounded instructions.
        private static double Native_15b6ad4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6adc; 2 bounded instructions.
        private static double Native_15b6adc(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6ae4; 5 bounded instructions.
        private static double Native_15b6ae4(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b6af8; 4 bounded instructions.
        private static double Native_15b6af8(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6b08; 5 bounded instructions.
        private static double Native_15b6b08(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b6b1c; 4 bounded instructions.
        private static double Native_15b6b1c(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6b2c; 5 bounded instructions.
        private static double Native_15b6b2c(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.02d);
        // Original ELF VA 0x15b6b40; 4 bounded instructions.
        private static double Native_15b6b40(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6b50; 2 bounded instructions.
        private static double Native_15b6b50(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6b58; 3 bounded instructions.
        private static double Native_15b6b58(int level) => 7200.0d;
        // Original ELF VA 0x15b6b64; 2 bounded instructions.
        private static double Native_15b6b64(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6b6c; 3 bounded instructions.
        private static double Native_15b6b6c(int level) => 7200.0d;
        // Original ELF VA 0x15b6b78; 2 bounded instructions.
        private static double Native_15b6b78(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6b80; 2 bounded instructions.
        private static double Native_15b6b80(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6b88; 2 bounded instructions.
        private static double Native_15b6b88(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6b90; 2 bounded instructions.
        private static double Native_15b6b90(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6b98; 3 bounded instructions.
        private static double Native_15b6b98(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) << 1)))))));
        // Original ELF VA 0x15b6ba4; 2 bounded instructions.
        private static double Native_15b6ba4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6bac; 2 bounded instructions.
        private static double Native_15b6bac(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6bb4; 2 bounded instructions.
        private static double Native_15b6bb4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6bbc; 5 bounded instructions.
        private static double Native_15b6bbc(int level) => (unchecked((int)(unchecked((uint)(level)))) < 3 ? unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) : 0.0d);
        // Original ELF VA 0x15b6bd0; 5 bounded instructions.
        private static double Native_15b6bd0(int level) => (unchecked((int)(unchecked((uint)(level)))) < 3 ? 0.0d : 1.0d);
        // Original ELF VA 0x15b6be4; 6 bounded instructions.
        private static double Native_15b6be4(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.0010000000474974513f)))))));
        // Original ELF VA 0x15b6bfc; 4 bounded instructions.
        private static double Native_15b6bfc(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6c0c; 2 bounded instructions.
        private static double Native_15b6c0c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6c14; 2 bounded instructions.
        private static double Native_15b6c14(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6c1c; 2 bounded instructions.
        private static double Native_15b6c1c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6c24; 5 bounded instructions.
        private static double Native_15b6c24(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b6c38; 4 bounded instructions.
        private static double Native_15b6c38(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6c48; 2 bounded instructions.
        private static double Native_15b6c48(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6c50; 2 bounded instructions.
        private static double Native_15b6c50(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6c58; 2 bounded instructions.
        private static double Native_15b6c58(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6c60; 5 bounded instructions.
        private static double Native_15b6c60(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b6c74; 4 bounded instructions.
        private static double Native_15b6c74(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6c84; 2 bounded instructions.
        private static double Native_15b6c84(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6c8c; 2 bounded instructions.
        private static double Native_15b6c8c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6c94; 2 bounded instructions.
        private static double Native_15b6c94(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6c9c; 2 bounded instructions.
        private static double Native_15b6c9c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6ca4; 4 bounded instructions.
        private static double Native_15b6ca4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) * 50)))))));
        // Original ELF VA 0x15b6cb4; 4 bounded instructions.
        private static double Native_15b6cb4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) << 1))))) - 1)))))));
        // Original ELF VA 0x15b6cc4; 3 bounded instructions.
        private static double Native_15b6cc4(double value) => (value + 1.0d);
        // Original ELF VA 0x15b6cd0; 23 bounded instructions.
        private static double Native_15b6cd0(int level) => System.Math.Pow(unchecked((double)(unchecked((int)(unchecked((uint)(unchecked((int)(unchecked((uint)(level)))))))))), 2.0d);
        // Original ELF VA 0x15b6d2c; 2 bounded instructions.
        private static double Native_15b6d2c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6d34; 2 bounded instructions.
        private static double Native_15b6d34(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6d3c; 2 bounded instructions.
        private static double Native_15b6d3c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6d44; 26 bounded instructions.
        private static double Native_15b6d44(int level) => (System.Math.Pow(unchecked((double)(unchecked((int)(unchecked((uint)(unchecked((int)(unchecked((uint)(level)))))))))), 2.0d) * 10.0d);
        // Original ELF VA 0x15b6dac; 5 bounded instructions.
        private static double Native_15b6dac(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b6dc0; 4 bounded instructions.
        private static double Native_15b6dc0(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6dd0; 2 bounded instructions.
        private static double Native_15b6dd0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6dd8; 2 bounded instructions.
        private static double Native_15b6dd8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6de0; 2 bounded instructions.
        private static double Native_15b6de0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6de8; 2 bounded instructions.
        private static double Native_15b6de8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6df0; 2 bounded instructions.
        private static double Native_15b6df0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6df8; 2 bounded instructions.
        private static double Native_15b6df8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6e00; 2 bounded instructions.
        private static double Native_15b6e00(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6e08; 5 bounded instructions.
        private static double Native_15b6e08(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.02d);
        // Original ELF VA 0x15b6e1c; 4 bounded instructions.
        private static double Native_15b6e1c(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6e2c; 2 bounded instructions.
        private static double Native_15b6e2c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6e34; 2 bounded instructions.
        private static double Native_15b6e34(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6e3c; 4 bounded instructions.
        private static double Native_15b6e3c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) << 4))))) - unchecked((int)(unchecked((uint)(level)))))))))));
        // Original ELF VA 0x15b6e4c; 5 bounded instructions.
        private static double Native_15b6e4c(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.05d);
        // Original ELF VA 0x15b6e60; 4 bounded instructions.
        private static double Native_15b6e60(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6e70; 4 bounded instructions.
        private static double Native_15b6e70(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) + (unchecked((int)(unchecked((uint)(level)))) << 2)))))) << 1)))))));
        // Original ELF VA 0x15b6e80; 5 bounded instructions.
        private static double Native_15b6e80(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.05d);
        // Original ELF VA 0x15b6e94; 4 bounded instructions.
        private static double Native_15b6e94(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6ea4; 5 bounded instructions.
        private static double Native_15b6ea4(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b6eb8; 4 bounded instructions.
        private static double Native_15b6eb8(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6ec8; 5 bounded instructions.
        private static double Native_15b6ec8(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.1d);
        // Original ELF VA 0x15b6edc; 4 bounded instructions.
        private static double Native_15b6edc(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6eec; 5 bounded instructions.
        private static double Native_15b6eec(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b6f00; 4 bounded instructions.
        private static double Native_15b6f00(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6f10; 5 bounded instructions.
        private static double Native_15b6f10(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b6f24; 4 bounded instructions.
        private static double Native_15b6f24(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6f34; 5 bounded instructions.
        private static double Native_15b6f34(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.02d);
        // Original ELF VA 0x15b6f48; 4 bounded instructions.
        private static double Native_15b6f48(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6f58; 2 bounded instructions.
        private static double Native_15b6f58(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6f60; 2 bounded instructions.
        private static double Native_15b6f60(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6f68; 2 bounded instructions.
        private static double Native_15b6f68(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6f70; 5 bounded instructions.
        private static double Native_15b6f70(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b6f84; 4 bounded instructions.
        private static double Native_15b6f84(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6f94; 2 bounded instructions.
        private static double Native_15b6f94(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6f9c; 2 bounded instructions.
        private static double Native_15b6f9c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6fa4; 2 bounded instructions.
        private static double Native_15b6fa4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6fac; 5 bounded instructions.
        private static double Native_15b6fac(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.02d);
        // Original ELF VA 0x15b6fc0; 4 bounded instructions.
        private static double Native_15b6fc0(double value) => (value * 100.0d);
        // Original ELF VA 0x15b6fd0; 2 bounded instructions.
        private static double Native_15b6fd0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6fd8; 2 bounded instructions.
        private static double Native_15b6fd8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6fe0; 2 bounded instructions.
        private static double Native_15b6fe0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b6fe8; 5 bounded instructions.
        private static double Native_15b6fe8(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b6ffc; 4 bounded instructions.
        private static double Native_15b6ffc(double value) => (value * 100.0d);
        // Original ELF VA 0x15b700c; 2 bounded instructions.
        private static double Native_15b700c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7014; 2 bounded instructions.
        private static double Native_15b7014(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b701c; 2 bounded instructions.
        private static double Native_15b701c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7024; 2 bounded instructions.
        private static double Native_15b7024(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b702c; 2 bounded instructions.
        private static double Native_15b702c(int level) => 1.0d;
        // Original ELF VA 0x15b7034; 6 bounded instructions.
        private static double Native_15b7034(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.15000000596046448f)))))));
        // Original ELF VA 0x15b704c; 4 bounded instructions.
        private static double Native_15b704c(double value) => (value * 100.0d);
        // Original ELF VA 0x15b705c; 2 bounded instructions.
        private static double Native_15b705c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7064; 3 bounded instructions.
        private static double Native_15b7064(int level) => 7200.0d;
        // Original ELF VA 0x15b7070; 2 bounded instructions.
        private static double Native_15b7070(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7078; 6 bounded instructions.
        private static double Native_15b7078(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.20000000298023224f)))))));
        // Original ELF VA 0x15b7090; 4 bounded instructions.
        private static double Native_15b7090(double value) => (value * 100.0d);
        // Original ELF VA 0x15b70a0; 2 bounded instructions.
        private static double Native_15b70a0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b70a8; 6 bounded instructions.
        private static double Native_15b70a8(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.20000000298023224f)))))));
        // Original ELF VA 0x15b70c0; 4 bounded instructions.
        private static double Native_15b70c0(double value) => (value * 100.0d);
        // Original ELF VA 0x15b70d0; 2 bounded instructions.
        private static double Native_15b70d0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b70d8; 6 bounded instructions.
        private static double Native_15b70d8(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.20000000298023224f)))))));
        // Original ELF VA 0x15b70f0; 4 bounded instructions.
        private static double Native_15b70f0(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7100; 2 bounded instructions.
        private static double Native_15b7100(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7108; 2 bounded instructions.
        private static double Native_15b7108(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7110; 2 bounded instructions.
        private static double Native_15b7110(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7118; 5 bounded instructions.
        private static double Native_15b7118(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b712c; 4 bounded instructions.
        private static double Native_15b712c(double value) => (value * 100.0d);
        // Original ELF VA 0x15b713c; 2 bounded instructions.
        private static double Native_15b713c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7144; 2 bounded instructions.
        private static double Native_15b7144(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b714c; 5 bounded instructions.
        private static double Native_15b714c(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b7160; 4 bounded instructions.
        private static double Native_15b7160(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7170; 5 bounded instructions.
        private static double Native_15b7170(int level) => (unchecked((int)(unchecked((uint)(level)))) < 3 ? unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) : 0.0d);
        // Original ELF VA 0x15b7184; 5 bounded instructions.
        private static double Native_15b7184(int level) => (unchecked((int)(unchecked((uint)(level)))) < 3 ? 0.0d : 1.0d);
        // Original ELF VA 0x15b7198; 6 bounded instructions.
        private static double Native_15b7198(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.05000000074505806f)))))));
        // Original ELF VA 0x15b71b0; 4 bounded instructions.
        private static double Native_15b71b0(double value) => (value * 100.0d);
        // Original ELF VA 0x15b71c0; 5 bounded instructions.
        private static double Native_15b71c0(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b71d4; 4 bounded instructions.
        private static double Native_15b71d4(double value) => (value * 100.0d);
        // Original ELF VA 0x15b71e4; 2 bounded instructions.
        private static double Native_15b71e4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b71ec; 2 bounded instructions.
        private static double Native_15b71ec(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b71f4; 2 bounded instructions.
        private static double Native_15b71f4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b71fc; 5 bounded instructions.
        private static double Native_15b71fc(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b7210; 4 bounded instructions.
        private static double Native_15b7210(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7220; 2 bounded instructions.
        private static double Native_15b7220(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7228; 2 bounded instructions.
        private static double Native_15b7228(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7230; 2 bounded instructions.
        private static double Native_15b7230(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7238; 5 bounded instructions.
        private static double Native_15b7238(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b724c; 4 bounded instructions.
        private static double Native_15b724c(double value) => (value * 100.0d);
        // Original ELF VA 0x15b725c; 2 bounded instructions.
        private static double Native_15b725c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7264; 2 bounded instructions.
        private static double Native_15b7264(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b726c; 2 bounded instructions.
        private static double Native_15b726c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7274; 2 bounded instructions.
        private static double Native_15b7274(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b727c; 2 bounded instructions.
        private static double Native_15b727c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7284; 2 bounded instructions.
        private static double Native_15b7284(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b728c; 2 bounded instructions.
        private static double Native_15b728c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7294; 2 bounded instructions.
        private static double Native_15b7294(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b729c; 2 bounded instructions.
        private static double Native_15b729c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b72a4; 4 bounded instructions.
        private static double Native_15b72a4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) * 100)))))));
        // Original ELF VA 0x15b72b4; 4 bounded instructions.
        private static double Native_15b72b4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) << 1))))) - 1)))))));
        // Original ELF VA 0x15b72c4; 3 bounded instructions.
        private static double Native_15b72c4(double value) => (value + 1.0d);
        // Original ELF VA 0x15b72d0; 5 bounded instructions.
        private static double Native_15b72d0(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b72e4; 4 bounded instructions.
        private static double Native_15b72e4(double value) => (value * 100.0d);
        // Original ELF VA 0x15b72f4; 5 bounded instructions.
        private static double Native_15b72f4(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b7308; 4 bounded instructions.
        private static double Native_15b7308(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7318; 5 bounded instructions.
        private static double Native_15b7318(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b732c; 4 bounded instructions.
        private static double Native_15b732c(double value) => (value * 100.0d);
        // Original ELF VA 0x15b733c; 5 bounded instructions.
        private static double Native_15b733c(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b7350; 4 bounded instructions.
        private static double Native_15b7350(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7360; 5 bounded instructions.
        private static double Native_15b7360(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.02d);
        // Original ELF VA 0x15b7374; 4 bounded instructions.
        private static double Native_15b7374(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7384; 5 bounded instructions.
        private static double Native_15b7384(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b7398; 4 bounded instructions.
        private static double Native_15b7398(double value) => (value * 100.0d);
        // Original ELF VA 0x15b73a8; 2 bounded instructions.
        private static double Native_15b73a8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b73b0; 3 bounded instructions.
        private static double Native_15b73b0(int level) => 7200.0d;
        // Original ELF VA 0x15b73bc; 2 bounded instructions.
        private static double Native_15b73bc(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b73c4; 3 bounded instructions.
        private static double Native_15b73c4(int level) => 7200.0d;
        // Original ELF VA 0x15b73d0; 6 bounded instructions.
        private static double Native_15b73d0(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.05000000074505806f)))))));
        // Original ELF VA 0x15b73e8; 4 bounded instructions.
        private static double Native_15b73e8(double value) => (value * 100.0d);
        // Original ELF VA 0x15b73f8; 6 bounded instructions.
        private static double Native_15b73f8(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.20000000298023224f)))))));
        // Original ELF VA 0x15b7410; 4 bounded instructions.
        private static double Native_15b7410(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7420; 2 bounded instructions.
        private static double Native_15b7420(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7428; 2 bounded instructions.
        private static double Native_15b7428(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7430; 2 bounded instructions.
        private static double Native_15b7430(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7438; 2 bounded instructions.
        private static double Native_15b7438(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7440; 2 bounded instructions.
        private static double Native_15b7440(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7448; 2 bounded instructions.
        private static double Native_15b7448(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7450; 4 bounded instructions.
        private static double Native_15b7450(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7460; 2 bounded instructions.
        private static double Native_15b7460(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7468; 4 bounded instructions.
        private static double Native_15b7468(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7478; 2 bounded instructions.
        private static double Native_15b7478(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7480; 2 bounded instructions.
        private static double Native_15b7480(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7488; 5 bounded instructions.
        private static double Native_15b7488(int level) => (unchecked((int)(unchecked((uint)(level)))) < 3 ? unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) : 0.0d);
        // Original ELF VA 0x15b749c; 5 bounded instructions.
        private static double Native_15b749c(int level) => (unchecked((int)(unchecked((uint)(level)))) < 3 ? 0.0d : 1.0d);
        // Original ELF VA 0x15b74b0; 5 bounded instructions.
        private static double Native_15b74b0(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.1d);
        // Original ELF VA 0x15b74c4; 4 bounded instructions.
        private static double Native_15b74c4(double value) => (value * 100.0d);
        // Original ELF VA 0x15b74d4; 2 bounded instructions.
        private static double Native_15b74d4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b74dc; 2 bounded instructions.
        private static double Native_15b74dc(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b74e4; 2 bounded instructions.
        private static double Native_15b74e4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b74ec; 5 bounded instructions.
        private static double Native_15b74ec(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b7500; 4 bounded instructions.
        private static double Native_15b7500(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7510; 2 bounded instructions.
        private static double Native_15b7510(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7518; 2 bounded instructions.
        private static double Native_15b7518(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7520; 5 bounded instructions.
        private static double Native_15b7520(int level) => (unchecked((int)(unchecked((uint)(level)))) < 3 ? unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) : 0.0d);
        // Original ELF VA 0x15b7534; 5 bounded instructions.
        private static double Native_15b7534(int level) => (unchecked((int)(unchecked((uint)(level)))) < 3 ? 0.0d : 1.0d);
        // Original ELF VA 0x15b7548; 5 bounded instructions.
        private static double Native_15b7548(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.05d);
        // Original ELF VA 0x15b755c; 4 bounded instructions.
        private static double Native_15b755c(double value) => (value * 100.0d);
        // Original ELF VA 0x15b756c; 2 bounded instructions.
        private static double Native_15b756c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7574; 2 bounded instructions.
        private static double Native_15b7574(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b757c; 2 bounded instructions.
        private static double Native_15b757c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7584; 5 bounded instructions.
        private static double Native_15b7584(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b7598; 4 bounded instructions.
        private static double Native_15b7598(double value) => (value * 100.0d);
        // Original ELF VA 0x15b75a8; 24 bounded instructions.
        private static double Native_15b75a8(int level) => System.Math.Pow(unchecked((double)(unchecked((int)(unchecked((uint)(unchecked((int)(unchecked((uint)(level)))))))))), 1.3d);
        // Original ELF VA 0x15b7608; 2 bounded instructions.
        private static double Native_15b7608(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7610; 2 bounded instructions.
        private static double Native_15b7610(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7618; 2 bounded instructions.
        private static double Native_15b7618(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7620; 2 bounded instructions.
        private static double Native_15b7620(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7628; 2 bounded instructions.
        private static double Native_15b7628(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7630; 5 bounded instructions.
        private static double Native_15b7630(int level) => (unchecked((int)(unchecked((uint)(level)))) < 3 ? unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) : 0.0d);
        // Original ELF VA 0x15b7644; 5 bounded instructions.
        private static double Native_15b7644(int level) => (unchecked((int)(unchecked((uint)(level)))) < 3 ? 0.0d : 1.0d);
        // Original ELF VA 0x15b7658; 6 bounded instructions.
        private static double Native_15b7658(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.05000000074505806f)))))));
        // Original ELF VA 0x15b7670; 4 bounded instructions.
        private static double Native_15b7670(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7680; 2 bounded instructions.
        private static double Native_15b7680(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7688; 2 bounded instructions.
        private static double Native_15b7688(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7690; 2 bounded instructions.
        private static double Native_15b7690(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7698; 2 bounded instructions.
        private static double Native_15b7698(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b76a0; 5 bounded instructions.
        private static double Native_15b76a0(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.02d);
        // Original ELF VA 0x15b76b4; 4 bounded instructions.
        private static double Native_15b76b4(double value) => (value * 100.0d);
        // Original ELF VA 0x15b76c4; 2 bounded instructions.
        private static double Native_15b76c4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b76cc; 3 bounded instructions.
        private static double Native_15b76cc(int level) => 100.0d;
        // Original ELF VA 0x15b76d8; 2 bounded instructions.
        private static double Native_15b76d8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b76e0; 2 bounded instructions.
        private static double Native_15b76e0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b76e8; 2 bounded instructions.
        private static double Native_15b76e8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b76f0; 2 bounded instructions.
        private static double Native_15b76f0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b76f8; 5 bounded instructions.
        private static double Native_15b76f8(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b770c; 4 bounded instructions.
        private static double Native_15b770c(double value) => (value * 100.0d);
        // Original ELF VA 0x15b771c; 2 bounded instructions.
        private static double Native_15b771c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7724; 2 bounded instructions.
        private static double Native_15b7724(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b772c; 2 bounded instructions.
        private static double Native_15b772c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7734; 5 bounded instructions.
        private static double Native_15b7734(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b7748; 4 bounded instructions.
        private static double Native_15b7748(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7758; 2 bounded instructions.
        private static double Native_15b7758(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7760; 5 bounded instructions.
        private static double Native_15b7760(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(((unchecked((int)(unchecked((uint)(level)))) * 90) + -80)))))));
        // Original ELF VA 0x15b7774; 5 bounded instructions.
        private static double Native_15b7774(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.1d);
        // Original ELF VA 0x15b7788; 4 bounded instructions.
        private static double Native_15b7788(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7798; 4 bounded instructions.
        private static double Native_15b7798(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) * 50)))))));
        // Original ELF VA 0x15b77a8; 2 bounded instructions.
        private static double Native_15b77a8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b77b0; 2 bounded instructions.
        private static double Native_15b77b0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b77b8; 5 bounded instructions.
        private static double Native_15b77b8(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.05d);
        // Original ELF VA 0x15b77cc; 4 bounded instructions.
        private static double Native_15b77cc(double value) => (value * 100.0d);
        // Original ELF VA 0x15b77dc; 2 bounded instructions.
        private static double Native_15b77dc(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b77e4; 5 bounded instructions.
        private static double Native_15b77e4(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.05d);
        // Original ELF VA 0x15b77f8; 4 bounded instructions.
        private static double Native_15b77f8(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7808; 2 bounded instructions.
        private static double Native_15b7808(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7810; 2 bounded instructions.
        private static double Native_15b7810(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7818; 4 bounded instructions.
        private static double Native_15b7818(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) * 50)))))));
        // Original ELF VA 0x15b7828; 3 bounded instructions.
        private static double Native_15b7828(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) << 1)))))));
        // Original ELF VA 0x15b7834; 2 bounded instructions.
        private static double Native_15b7834(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b783c; 2 bounded instructions.
        private static double Native_15b783c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7844; 2 bounded instructions.
        private static double Native_15b7844(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b784c; 5 bounded instructions.
        private static double Native_15b784c(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b7860; 4 bounded instructions.
        private static double Native_15b7860(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7870; 2 bounded instructions.
        private static double Native_15b7870(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7878; 2 bounded instructions.
        private static double Native_15b7878(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7880; 2 bounded instructions.
        private static double Native_15b7880(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7888; 6 bounded instructions.
        private static double Native_15b7888(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.20000000298023224f)))))));
        // Original ELF VA 0x15b78a0; 4 bounded instructions.
        private static double Native_15b78a0(double value) => (value * 100.0d);
        // Original ELF VA 0x15b78b0; 6 bounded instructions.
        private static double Native_15b78b0(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.20000000298023224f)))))));
        // Original ELF VA 0x15b78c8; 4 bounded instructions.
        private static double Native_15b78c8(double value) => (value * 100.0d);
        // Original ELF VA 0x15b78d8; 6 bounded instructions.
        private static double Native_15b78d8(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.20000000298023224f)))))));
        // Original ELF VA 0x15b78f0; 4 bounded instructions.
        private static double Native_15b78f0(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7900; 2 bounded instructions.
        private static double Native_15b7900(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7908; 2 bounded instructions.
        private static double Native_15b7908(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7910; 2 bounded instructions.
        private static double Native_15b7910(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7918; 2 bounded instructions.
        private static double Native_15b7918(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7920; 4 bounded instructions.
        private static double Native_15b7920(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) * 100)))))));
        // Original ELF VA 0x15b7930; 5 bounded instructions.
        private static double Native_15b7930(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b7944; 4 bounded instructions.
        private static double Native_15b7944(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7954; 2 bounded instructions.
        private static double Native_15b7954(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b795c; 2 bounded instructions.
        private static double Native_15b795c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7964; 24 bounded instructions.
        private static double Native_15b7964(int level) => System.Math.Pow(unchecked((double)(unchecked((int)(unchecked((uint)(unchecked((int)(unchecked((uint)(level)))))))))), 1.3d);
        // Original ELF VA 0x15b79c4; 5 bounded instructions.
        private static double Native_15b79c4(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.05d);
        // Original ELF VA 0x15b79d8; 4 bounded instructions.
        private static double Native_15b79d8(double value) => (value * 100.0d);
        // Original ELF VA 0x15b79e8; 2 bounded instructions.
        private static double Native_15b79e8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b79f0; 3 bounded instructions.
        private static double Native_15b79f0(int level) => 7200.0d;
        // Original ELF VA 0x15b79fc; 2 bounded instructions.
        private static double Native_15b79fc(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7a04; 2 bounded instructions.
        private static double Native_15b7a04(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7a0c; 2 bounded instructions.
        private static double Native_15b7a0c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7a14; 2 bounded instructions.
        private static double Native_15b7a14(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7a1c; 5 bounded instructions.
        private static double Native_15b7a1c(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.02d);
        // Original ELF VA 0x15b7a30; 4 bounded instructions.
        private static double Native_15b7a30(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7a40; 5 bounded instructions.
        private static double Native_15b7a40(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b7a54; 4 bounded instructions.
        private static double Native_15b7a54(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7a64; 5 bounded instructions.
        private static double Native_15b7a64(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b7a78; 4 bounded instructions.
        private static double Native_15b7a78(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7a88; 5 bounded instructions.
        private static double Native_15b7a88(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b7a9c; 4 bounded instructions.
        private static double Native_15b7a9c(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7aac; 5 bounded instructions.
        private static double Native_15b7aac(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b7ac0; 4 bounded instructions.
        private static double Native_15b7ac0(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7ad0; 5 bounded instructions.
        private static double Native_15b7ad0(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b7ae4; 4 bounded instructions.
        private static double Native_15b7ae4(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7af4; 2 bounded instructions.
        private static double Native_15b7af4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7afc; 3 bounded instructions.
        private static double Native_15b7afc(int level) => 7200.0d;
        // Original ELF VA 0x15b7b08; 2 bounded instructions.
        private static double Native_15b7b08(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7b10; 3 bounded instructions.
        private static double Native_15b7b10(int level) => 7200.0d;
        // Original ELF VA 0x15b7b1c; 2 bounded instructions.
        private static double Native_15b7b1c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7b24; 2 bounded instructions.
        private static double Native_15b7b24(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7b2c; 2 bounded instructions.
        private static double Native_15b7b2c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7b34; 2 bounded instructions.
        private static double Native_15b7b34(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7b3c; 2 bounded instructions.
        private static double Native_15b7b3c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7b44; 2 bounded instructions.
        private static double Native_15b7b44(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7b4c; 2 bounded instructions.
        private static double Native_15b7b4c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7b54; 4 bounded instructions.
        private static double Native_15b7b54(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) * 100)))))));
        // Original ELF VA 0x15b7b64; 2 bounded instructions.
        private static double Native_15b7b64(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7b6c; 4 bounded instructions.
        private static double Native_15b7b6c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) * 100)))))));
        // Original ELF VA 0x15b7b7c; 2 bounded instructions.
        private static double Native_15b7b7c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7b84; 4 bounded instructions.
        private static double Native_15b7b84(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) * 100)))))));
        // Original ELF VA 0x15b7b94; 2 bounded instructions.
        private static double Native_15b7b94(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7b9c; 2 bounded instructions.
        private static double Native_15b7b9c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7ba4; 2 bounded instructions.
        private static double Native_15b7ba4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7bac; 2 bounded instructions.
        private static double Native_15b7bac(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7bb4; 2 bounded instructions.
        private static double Native_15b7bb4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7bbc; 2 bounded instructions.
        private static double Native_15b7bbc(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7bc4; 4 bounded instructions.
        private static double Native_15b7bc4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) + (unchecked((int)(unchecked((uint)(level)))) << 2)))))) + 10)))))));
        // Original ELF VA 0x15b7bd4; 5 bounded instructions.
        private static double Native_15b7bd4(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b7be8; 4 bounded instructions.
        private static double Native_15b7be8(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7bf8; 5 bounded instructions.
        private static double Native_15b7bf8(int level) => (unchecked((int)(unchecked((uint)(level)))) < 3 ? unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) : 0.0d);
        // Original ELF VA 0x15b7c0c; 5 bounded instructions.
        private static double Native_15b7c0c(int level) => (unchecked((int)(unchecked((uint)(level)))) < 3 ? 0.0d : 1.0d);
        // Original ELF VA 0x15b7c20; 3 bounded instructions.
        private static double Native_15b7c20(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) / 2.0d)))))));
        // Original ELF VA 0x15b7c2c; 4 bounded instructions.
        private static double Native_15b7c2c(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7c3c; 2 bounded instructions.
        private static double Native_15b7c3c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7c44; 2 bounded instructions.
        private static double Native_15b7c44(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7c4c; 23 bounded instructions.
        private static double Native_15b7c4c(int level) => System.Math.Pow(unchecked((double)(unchecked((int)(unchecked((uint)(unchecked((int)(unchecked((uint)(level)))))))))), 1.5d);
        // Original ELF VA 0x15b7ca8; 2 bounded instructions.
        private static double Native_15b7ca8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7cb0; 2 bounded instructions.
        private static double Native_15b7cb0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7cb8; 2 bounded instructions.
        private static double Native_15b7cb8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7cc0; 2 bounded instructions.
        private static double Native_15b7cc0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7cc8; 2 bounded instructions.
        private static double Native_15b7cc8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7cd0; 2 bounded instructions.
        private static double Native_15b7cd0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7cd8; 4 bounded instructions.
        private static double Native_15b7cd8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) + (unchecked((int)(unchecked((uint)(level)))) << 2)))))) + 10)))))));
        // Original ELF VA 0x15b7ce8; 5 bounded instructions.
        private static double Native_15b7ce8(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b7cfc; 4 bounded instructions.
        private static double Native_15b7cfc(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7d0c; 5 bounded instructions.
        private static double Native_15b7d0c(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.1d);
        // Original ELF VA 0x15b7d20; 4 bounded instructions.
        private static double Native_15b7d20(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7d30; 23 bounded instructions.
        private static double Native_15b7d30(int level) => System.Math.Pow(unchecked((double)(unchecked((int)(unchecked((uint)(unchecked((int)(unchecked((uint)(level)))))))))), 2.0d);
        // Original ELF VA 0x15b7d8c; 2 bounded instructions.
        private static double Native_15b7d8c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7d94; 2 bounded instructions.
        private static double Native_15b7d94(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7d9c; 2 bounded instructions.
        private static double Native_15b7d9c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7da4; 2 bounded instructions.
        private static double Native_15b7da4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7dac; 2 bounded instructions.
        private static double Native_15b7dac(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7db4; 2 bounded instructions.
        private static double Native_15b7db4(int level) => 1.0d;
        // Original ELF VA 0x15b7dbc; 6 bounded instructions.
        private static double Native_15b7dbc(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.15000000596046448f)))))));
        // Original ELF VA 0x15b7dd4; 4 bounded instructions.
        private static double Native_15b7dd4(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7de4; 2 bounded instructions.
        private static double Native_15b7de4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7dec; 2 bounded instructions.
        private static double Native_15b7dec(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7df4; 4 bounded instructions.
        private static double Native_15b7df4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) + (unchecked((int)(unchecked((uint)(level)))) << 2)))))) + 10)))))));
        // Original ELF VA 0x15b7e04; 5 bounded instructions.
        private static double Native_15b7e04(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b7e18; 4 bounded instructions.
        private static double Native_15b7e18(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7e28; 4 bounded instructions.
        private static double Native_15b7e28(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) * 100)))))));
        // Original ELF VA 0x15b7e38; 5 bounded instructions.
        private static double Native_15b7e38(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.1d);
        // Original ELF VA 0x15b7e4c; 4 bounded instructions.
        private static double Native_15b7e4c(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7e5c; 2 bounded instructions.
        private static double Native_15b7e5c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7e64; 2 bounded instructions.
        private static double Native_15b7e64(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7e6c; 2 bounded instructions.
        private static double Native_15b7e6c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7e74; 4 bounded instructions.
        private static double Native_15b7e74(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) * 100)))))));
        // Original ELF VA 0x15b7e84; 5 bounded instructions.
        private static double Native_15b7e84(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.05d);
        // Original ELF VA 0x15b7e98; 4 bounded instructions.
        private static double Native_15b7e98(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7ea8; 4 bounded instructions.
        private static double Native_15b7ea8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) * 100)))))));
        // Original ELF VA 0x15b7eb8; 6 bounded instructions.
        private static double Native_15b7eb8(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.10000000149011612f)))))));
        // Original ELF VA 0x15b7ed0; 4 bounded instructions.
        private static double Native_15b7ed0(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7ee0; 2 bounded instructions.
        private static double Native_15b7ee0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7ee8; 2 bounded instructions.
        private static double Native_15b7ee8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7ef0; 2 bounded instructions.
        private static double Native_15b7ef0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7ef8; 2 bounded instructions.
        private static double Native_15b7ef8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7f00; 2 bounded instructions.
        private static double Native_15b7f00(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7f08; 2 bounded instructions.
        private static double Native_15b7f08(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7f10; 5 bounded instructions.
        private static double Native_15b7f10(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b7f24; 4 bounded instructions.
        private static double Native_15b7f24(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7f34; 5 bounded instructions.
        private static double Native_15b7f34(int level) => (unchecked((int)(unchecked((uint)(level)))) < 3 ? unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) : 0.0d);
        // Original ELF VA 0x15b7f48; 5 bounded instructions.
        private static double Native_15b7f48(int level) => (unchecked((int)(unchecked((uint)(level)))) < 3 ? 0.0d : 1.0d);
        // Original ELF VA 0x15b7f5c; 6 bounded instructions.
        private static double Native_15b7f5c(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.10000000149011612f)))))));
        // Original ELF VA 0x15b7f74; 4 bounded instructions.
        private static double Native_15b7f74(double value) => (value * 100.0d);
        // Original ELF VA 0x15b7f84; 23 bounded instructions.
        private static double Native_15b7f84(int level) => System.Math.Pow(unchecked((double)(unchecked((int)(unchecked((uint)(unchecked((int)(unchecked((uint)(level)))))))))), 3.0d);
        // Original ELF VA 0x15b7fe0; 2 bounded instructions.
        private static double Native_15b7fe0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7fe8; 2 bounded instructions.
        private static double Native_15b7fe8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7ff0; 2 bounded instructions.
        private static double Native_15b7ff0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b7ff8; 2 bounded instructions.
        private static double Native_15b7ff8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8000; 2 bounded instructions.
        private static double Native_15b8000(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8008; 2 bounded instructions.
        private static double Native_15b8008(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8010; 2 bounded instructions.
        private static double Native_15b8010(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8018; 4 bounded instructions.
        private static double Native_15b8018(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) + (unchecked((int)(unchecked((uint)(level)))) << 2)))))) + 10)))))));
        // Original ELF VA 0x15b8028; 5 bounded instructions.
        private static double Native_15b8028(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.02d);
        // Original ELF VA 0x15b803c; 4 bounded instructions.
        private static double Native_15b803c(double value) => (value * 100.0d);
        // Original ELF VA 0x15b804c; 5 bounded instructions.
        private static double Native_15b804c(int level) => (unchecked((int)(unchecked((uint)(level)))) < 3 ? unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) : 0.0d);
        // Original ELF VA 0x15b8060; 5 bounded instructions.
        private static double Native_15b8060(int level) => (unchecked((int)(unchecked((uint)(level)))) < 3 ? 0.0d : 1.0d);
        // Original ELF VA 0x15b8074; 5 bounded instructions.
        private static double Native_15b8074(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.03d);
        // Original ELF VA 0x15b8088; 4 bounded instructions.
        private static double Native_15b8088(double value) => (value * 100.0d);
        // Original ELF VA 0x15b8098; 2 bounded instructions.
        private static double Native_15b8098(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b80a0; 2 bounded instructions.
        private static double Native_15b80a0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b80a8; 2 bounded instructions.
        private static double Native_15b80a8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b80b0; 2 bounded instructions.
        private static double Native_15b80b0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b80b8; 4 bounded instructions.
        private static double Native_15b80b8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) + (unchecked((int)(unchecked((uint)(level)))) << 2)))))) + 10)))))));
        // Original ELF VA 0x15b80c8; 5 bounded instructions.
        private static double Native_15b80c8(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.02d);
        // Original ELF VA 0x15b80dc; 4 bounded instructions.
        private static double Native_15b80dc(double value) => (value * 100.0d);
        // Original ELF VA 0x15b80ec; 2 bounded instructions.
        private static double Native_15b80ec(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b80f4; 2 bounded instructions.
        private static double Native_15b80f4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b80fc; 4 bounded instructions.
        private static double Native_15b80fc(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) + (unchecked((int)(unchecked((uint)(level)))) << 2)))))) + 10)))))));
        // Original ELF VA 0x15b810c; 5 bounded instructions.
        private static double Native_15b810c(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b8120; 4 bounded instructions.
        private static double Native_15b8120(double value) => (value * 100.0d);
        // Original ELF VA 0x15b8130; 5 bounded instructions.
        private static double Native_15b8130(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b8144; 4 bounded instructions.
        private static double Native_15b8144(double value) => (value * 100.0d);
        // Original ELF VA 0x15b8154; 24 bounded instructions.
        private static double Native_15b8154(int level) => System.Math.Pow(unchecked((double)(unchecked((int)(unchecked((uint)(unchecked((int)(unchecked((uint)(level)))))))))), 1.2d);
        // Original ELF VA 0x15b81b4; 5 bounded instructions.
        private static double Native_15b81b4(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.05d);
        // Original ELF VA 0x15b81c8; 4 bounded instructions.
        private static double Native_15b81c8(double value) => (value * 100.0d);
        // Original ELF VA 0x15b81d8; 27 bounded instructions.
        private static double Native_15b81d8(int level) => (System.Math.Pow(unchecked((double)(unchecked((int)(unchecked((uint)(unchecked((int)(unchecked((uint)(level)))))))))), 2.0d) * 100.0d);
        // Original ELF VA 0x15b8244; 2 bounded instructions.
        private static double Native_15b8244(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b824c; 2 bounded instructions.
        private static double Native_15b824c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8254; 6 bounded instructions.
        private static double Native_15b8254(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.05000000074505806f)))))));
        // Original ELF VA 0x15b826c; 4 bounded instructions.
        private static double Native_15b826c(double value) => (value * 100.0d);
        // Original ELF VA 0x15b827c; 6 bounded instructions.
        private static double Native_15b827c(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.20000000298023224f)))))));
        // Original ELF VA 0x15b8294; 4 bounded instructions.
        private static double Native_15b8294(double value) => (value * 100.0d);
        // Original ELF VA 0x15b82a4; 2 bounded instructions.
        private static double Native_15b82a4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b82ac; 4 bounded instructions.
        private static double Native_15b82ac(double value) => (value * 100.0d);
        // Original ELF VA 0x15b82bc; 27 bounded instructions.
        private static double Native_15b82bc(int level) => (System.Math.Pow(unchecked((double)(unchecked((int)(unchecked((uint)(unchecked((int)(unchecked((uint)(level)))))))))), 2.0d) * 50.0d);
        // Original ELF VA 0x15b8328; 2 bounded instructions.
        private static double Native_15b8328(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8330; 2 bounded instructions.
        private static double Native_15b8330(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8338; 3 bounded instructions.
        private static double Native_15b8338(int level) => 7200.0d;
        // Original ELF VA 0x15b8344; 2 bounded instructions.
        private static double Native_15b8344(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b834c; 2 bounded instructions.
        private static double Native_15b834c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8354; 2 bounded instructions.
        private static double Native_15b8354(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b835c; 2 bounded instructions.
        private static double Native_15b835c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8364; 4 bounded instructions.
        private static double Native_15b8364(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) + (unchecked((int)(unchecked((uint)(level)))) << 2)))))) + 10)))))));
        // Original ELF VA 0x15b8374; 5 bounded instructions.
        private static double Native_15b8374(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b8388; 4 bounded instructions.
        private static double Native_15b8388(double value) => (value * 100.0d);
        // Original ELF VA 0x15b8398; 23 bounded instructions.
        private static double Native_15b8398(int level) => System.Math.Pow(unchecked((double)(unchecked((int)(unchecked((uint)(unchecked((int)(unchecked((uint)(level)))))))))), 1.5d);
        // Original ELF VA 0x15b83f4; 2 bounded instructions.
        private static double Native_15b83f4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b83fc; 2 bounded instructions.
        private static double Native_15b83fc(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8404; 2 bounded instructions.
        private static double Native_15b8404(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b840c; 4 bounded instructions.
        private static double Native_15b840c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) + (unchecked((int)(unchecked((uint)(level)))) << 2)))))) + 10)))))));
        // Original ELF VA 0x15b841c; 5 bounded instructions.
        private static double Native_15b841c(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b8430; 4 bounded instructions.
        private static double Native_15b8430(double value) => (value * 100.0d);
        // Original ELF VA 0x15b8440; 6 bounded instructions.
        private static double Native_15b8440(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.20000000298023224f)))))));
        // Original ELF VA 0x15b8458; 4 bounded instructions.
        private static double Native_15b8458(double value) => (value * 100.0d);
        // Original ELF VA 0x15b8468; 6 bounded instructions.
        private static double Native_15b8468(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.20000000298023224f)))))));
        // Original ELF VA 0x15b8480; 4 bounded instructions.
        private static double Native_15b8480(double value) => (value * 100.0d);
        // Original ELF VA 0x15b8490; 23 bounded instructions.
        private static double Native_15b8490(int level) => System.Math.Pow(unchecked((double)(unchecked((int)(unchecked((uint)(unchecked((int)(unchecked((uint)(level)))))))))), 1.5d);
        // Original ELF VA 0x15b84ec; 2 bounded instructions.
        private static double Native_15b84ec(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b84f4; 6 bounded instructions.
        private static double Native_15b84f4(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.20000000298023224f)))))));
        // Original ELF VA 0x15b850c; 4 bounded instructions.
        private static double Native_15b850c(double value) => (value * 100.0d);
        // Original ELF VA 0x15b851c; 6 bounded instructions.
        private static double Native_15b851c(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.20000000298023224f)))))));
        // Original ELF VA 0x15b8534; 4 bounded instructions.
        private static double Native_15b8534(double value) => (value * 100.0d);
        // Original ELF VA 0x15b8544; 6 bounded instructions.
        private static double Native_15b8544(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.20000000298023224f)))))));
        // Original ELF VA 0x15b855c; 4 bounded instructions.
        private static double Native_15b855c(double value) => (value * 100.0d);
        // Original ELF VA 0x15b856c; 2 bounded instructions.
        private static double Native_15b856c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8574; 2 bounded instructions.
        private static double Native_15b8574(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b857c; 4 bounded instructions.
        private static double Native_15b857c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) + (unchecked((int)(unchecked((uint)(level)))) << 1)))))) << 1)))))));
        // Original ELF VA 0x15b858c; 5 bounded instructions.
        private static double Native_15b858c(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b85a0; 4 bounded instructions.
        private static double Native_15b85a0(double value) => (value * 100.0d);
        // Original ELF VA 0x15b85b0; 2 bounded instructions.
        private static double Native_15b85b0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b85b8; 2 bounded instructions.
        private static double Native_15b85b8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b85c0; 2 bounded instructions.
        private static double Native_15b85c0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b85c8; 2 bounded instructions.
        private static double Native_15b85c8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b85d0; 2 bounded instructions.
        private static double Native_15b85d0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b85d8; 2 bounded instructions.
        private static double Native_15b85d8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b85e0; 4 bounded instructions.
        private static double Native_15b85e0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) + (unchecked((int)(unchecked((uint)(level)))) << 2)))))) + 10)))))));
        // Original ELF VA 0x15b85f0; 5 bounded instructions.
        private static double Native_15b85f0(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b8604; 4 bounded instructions.
        private static double Native_15b8604(double value) => (value * 100.0d);
        // Original ELF VA 0x15b8614; 5 bounded instructions.
        private static double Native_15b8614(int level) => (unchecked((int)(unchecked((uint)(level)))) < 3 ? unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) : 0.0d);
        // Original ELF VA 0x15b8628; 5 bounded instructions.
        private static double Native_15b8628(int level) => (unchecked((int)(unchecked((uint)(level)))) < 3 ? 0.0d : 1.0d);
        // Original ELF VA 0x15b863c; 5 bounded instructions.
        private static double Native_15b863c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(((unchecked((int)(unchecked((uint)(level)))) * -30) + 180)))))));
        // Original ELF VA 0x15b8650; 2 bounded instructions.
        private static double Native_15b8650(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8658; 6 bounded instructions.
        private static double Native_15b8658(int level) => unchecked((double)(RoundFloat32((float)(RoundFloat32((float)((RoundFloat32((float)(RoundFloat32((float)(unchecked((int)(unchecked((uint)(level)))))))) * 0.009999999776482582f)))))));
        // Original ELF VA 0x15b8670; 4 bounded instructions.
        private static double Native_15b8670(double value) => (value * 100.0d);
        // Original ELF VA 0x15b8680; 2 bounded instructions.
        private static double Native_15b8680(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8688; 2 bounded instructions.
        private static double Native_15b8688(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8690; 2 bounded instructions.
        private static double Native_15b8690(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8698; 2 bounded instructions.
        private static double Native_15b8698(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b86a0; 4 bounded instructions.
        private static double Native_15b86a0(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) + (unchecked((int)(unchecked((uint)(level)))) << 2)))))) + 10)))))));
        // Original ELF VA 0x15b86b0; 5 bounded instructions.
        private static double Native_15b86b0(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b86c4; 4 bounded instructions.
        private static double Native_15b86c4(double value) => (value * 100.0d);
        // Original ELF VA 0x15b86d4; 2 bounded instructions.
        private static double Native_15b86d4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b86dc; 2 bounded instructions.
        private static double Native_15b86dc(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b86e4; 5 bounded instructions.
        private static double Native_15b86e4(int level) => (unchecked((int)(unchecked((uint)(level)))) < 3 ? unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) : 0.0d);
        // Original ELF VA 0x15b86f8; 5 bounded instructions.
        private static double Native_15b86f8(int level) => (unchecked((int)(unchecked((uint)(level)))) < 3 ? 0.0d : 1.0d);
        // Original ELF VA 0x15b870c; 2 bounded instructions.
        private static double Native_15b870c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8714; 5 bounded instructions.
        private static double Native_15b8714(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.02d);
        // Original ELF VA 0x15b8728; 4 bounded instructions.
        private static double Native_15b8728(double value) => (value * 100.0d);
        // Original ELF VA 0x15b8738; 2 bounded instructions.
        private static double Native_15b8738(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8740; 2 bounded instructions.
        private static double Native_15b8740(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8748; 2 bounded instructions.
        private static double Native_15b8748(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8750; 2 bounded instructions.
        private static double Native_15b8750(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8758; 2 bounded instructions.
        private static double Native_15b8758(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8760; 2 bounded instructions.
        private static double Native_15b8760(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8768; 2 bounded instructions.
        private static double Native_15b8768(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8770; 2 bounded instructions.
        private static double Native_15b8770(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8778; 2 bounded instructions.
        private static double Native_15b8778(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8780; 2 bounded instructions.
        private static double Native_15b8780(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8788; 4 bounded instructions.
        private static double Native_15b8788(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) + (unchecked((int)(unchecked((uint)(level)))) << 2)))))) + 10)))))));
        // Original ELF VA 0x15b8798; 5 bounded instructions.
        private static double Native_15b8798(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.02d);
        // Original ELF VA 0x15b87ac; 4 bounded instructions.
        private static double Native_15b87ac(double value) => (value * 100.0d);
        // Original ELF VA 0x15b87bc; 2 bounded instructions.
        private static double Native_15b87bc(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b87c4; 5 bounded instructions.
        private static double Native_15b87c4(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.05d);
        // Original ELF VA 0x15b87d8; 4 bounded instructions.
        private static double Native_15b87d8(double value) => (value * 100.0d);
        // Original ELF VA 0x15b87e8; 4 bounded instructions.
        private static double Native_15b87e8(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) * 100)))))));
        // Original ELF VA 0x15b87f8; 5 bounded instructions.
        private static double Native_15b87f8(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.7d);
        // Original ELF VA 0x15b880c; 4 bounded instructions.
        private static double Native_15b880c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) * 100)))))));
        // Original ELF VA 0x15b881c; 5 bounded instructions.
        private static double Native_15b881c(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.05d);
        // Original ELF VA 0x15b8830; 4 bounded instructions.
        private static double Native_15b8830(double value) => (value * 100.0d);
        // Original ELF VA 0x15b8840; 10 bounded instructions.
        private static double Native_15b8840(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(((unchecked((long)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) << 6))))) - (unchecked((int)(unchecked((uint)(level)))) << 2)))))))) * 715827883L) >> 33))) + unchecked((int)((unchecked((ulong)((unchecked((long)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) << 6))))) - (unchecked((int)(unchecked((uint)(level)))) << 2)))))))) * 715827883L))) >> 63))))))))));
        // Original ELF VA 0x15b8868; 4 bounded instructions.
        private static double Native_15b8868(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) * 300)))))));
        // Original ELF VA 0x15b8878; 2 bounded instructions.
        private static double Native_15b8878(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8880; 2 bounded instructions.
        private static double Native_15b8880(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8888; 5 bounded instructions.
        private static double Native_15b8888(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.05d);
        // Original ELF VA 0x15b889c; 4 bounded instructions.
        private static double Native_15b889c(double value) => (value * 100.0d);
        // Original ELF VA 0x15b88ac; 2 bounded instructions.
        private static double Native_15b88ac(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b88b4; 2 bounded instructions.
        private static double Native_15b88b4(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b88bc; 2 bounded instructions.
        private static double Native_15b88bc(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b88c4; 5 bounded instructions.
        private static double Native_15b88c4(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.03d);
        // Original ELF VA 0x15b88d8; 4 bounded instructions.
        private static double Native_15b88d8(double value) => (value * 100.0d);
        // Original ELF VA 0x15b88e8; 5 bounded instructions.
        private static double Native_15b88e8(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b88fc; 4 bounded instructions.
        private static double Native_15b88fc(double value) => (value * 100.0d);
        // Original ELF VA 0x15b890c; 5 bounded instructions.
        private static double Native_15b890c(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.01d);
        // Original ELF VA 0x15b8920; 4 bounded instructions.
        private static double Native_15b8920(double value) => (value * 100.0d);
        // Original ELF VA 0x15b8930; 2 bounded instructions.
        private static double Native_15b8930(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8938; 5 bounded instructions.
        private static double Native_15b8938(int level) => (unchecked((double)(unchecked((int)(unchecked((uint)(level)))))) * 0.05d);
        // Original ELF VA 0x15b894c; 4 bounded instructions.
        private static double Native_15b894c(double value) => (value * 100.0d);
        // Original ELF VA 0x15b895c; 2 bounded instructions.
        private static double Native_15b895c(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8964; 3 bounded instructions.
        private static double Native_15b8964(int level) => 7200.0d;
        // Original ELF VA 0x15b8970; 2 bounded instructions.
        private static double Native_15b8970(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8978; 3 bounded instructions.
        private static double Native_15b8978(int level) => 7200.0d;
        // Original ELF VA 0x15b8984; 2 bounded instructions.
        private static double Native_15b8984(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b898c; 3 bounded instructions.
        private static double Native_15b898c(int level) => 7200.0d;
        // Original ELF VA 0x15b8998; 2 bounded instructions.
        private static double Native_15b8998(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b89a0; 3 bounded instructions.
        private static double Native_15b89a0(int level) => 7200.0d;
        // Original ELF VA 0x15b89ac; 27 bounded instructions.
        private static double Native_15b89ac(int level) => (System.Math.Pow(unchecked((double)(unchecked((int)(unchecked((uint)(unchecked((int)(unchecked((uint)(level)))))))))), 2.0d) * 100.0d);
        // Original ELF VA 0x15b8a18; 2 bounded instructions.
        private static double Native_15b8a18(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8a20; 2 bounded instructions.
        private static double Native_15b8a20(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8a28; 2 bounded instructions.
        private static double Native_15b8a28(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8a30; 2 bounded instructions.
        private static double Native_15b8a30(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8a38; 2 bounded instructions.
        private static double Native_15b8a38(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8a40; 2 bounded instructions.
        private static double Native_15b8a40(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8a48; 2 bounded instructions.
        private static double Native_15b8a48(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8a50; 2 bounded instructions.
        private static double Native_15b8a50(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8a58; 2 bounded instructions.
        private static double Native_15b8a58(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8a60; 2 bounded instructions.
        private static double Native_15b8a60(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8a68; 2 bounded instructions.
        private static double Native_15b8a68(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8a70; 4 bounded instructions.
        private static double Native_15b8a70(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) * 300)))))));
        // Original ELF VA 0x15b8a80; 2 bounded instructions.
        private static double Native_15b8a80(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
        // Original ELF VA 0x15b8a88; 4 bounded instructions.
        private static double Native_15b8a88(int level) => unchecked((double)(unchecked((int)(unchecked((uint)((unchecked((int)(unchecked((uint)(level)))) * 300)))))));
        // Original ELF VA 0x15b8a98; 2 bounded instructions.
        private static double Native_15b8a98(int level) => unchecked((double)(unchecked((int)(unchecked((uint)(level))))));
    }
}
