# 装备与进化（`items`）

[返回公式清单总览](../FormulaCatalog.md)

## 概述

Each item's stat is a two-phase piecewise-linear function of copies. Before the first evolution the item's level is its copy count: weapon damage = DamageBase[type][r] + Growth[type][r]*copies + FlatDamage, and armor = ArmorBase[r] + ArmorGrowth[r]*copies. All DamageBase arrays are 0, so a weapon's damage is just growth*copies + FlatDamage. Evolving uses up Shards[isArmor][r][L] copies and moves the item onto a hand-baked table. The stat then becomes EvolutionValue[type][r][L] + CopyValue[type][r][L]*leftoverCopies (+FlatDamage for weapons), with L capped at MaxEvolutionLevel=15. The runtime lookups are simple and reconstructed, but none of them carries a native address. The design intelligence lives in the original team's editor generators, which are all empty here. Their outputs can be reverse-fitted from the asset. The island-progress curve IslandTargetDays drives everything through a few steps. Fill rules give E_k = Start + (k-1)*Step island numbers. A native island-to-day lerp turns those into the *EvolutionTargetTimings and the interpolation TimingPoints. Evolution Values are the simulation heuristic rounded to a nice number, and CopyValue ≈ 0.5*(V[L]-V[L-1])/Shards[L]; this matches CopyValueTargetShare=0.5. Mythical rows follow V=50000*L*(L+1), copies=10L and CopyValue=5000. Balance interpolations (native) are piecewise-linear curves over that timing axis. They are summed for the additive types and multiplied for types 1 and 3. They feed simulation and boss scaling, not the item stats directly.

**核验结论**：I checked every cited body in Player/Inventory, Balance and the call sites, and parsed Combat Balance Config.asset and all 88 ItemConfig assets. The items catalog is substantially correct.

**Core formulas:**
- **Code:** All of these match the code exactly, including float32 evaluation order, Cut/Pierce/Smash array selection, the min(L, 15) clamp, LINQ First() lookups and the NextEvolutionLevel = Level + (Evolved ? 1 : 0) shard indexing:
  - pre-evolution weapon damage: base + growth*copies + FlatDamage
  - pre-evolution armor: base + growth*copies
  - evolved damage/armor: Value[Lc] + CopyValue[Lc]*leftover (+Flat for weapons)
  - shard cost and the evolve transition
- **Status labels:** All status labels are correct. Native comments exist only on EvolveItemCommandRunner, TryEvolve, AddItem/CreateItem/SetInitialCopies, RarityStatParam, ItemSlotKeys, HitFrameAt60Fps, BalanceInterpolation.Evaluate/ParsePoints, GetTotalCoefficient (0xf5 additive mask confirmed) and the ForecastSnapshots copy of ConvertIslandTimingToDays. The ItemModel stat math is un-annotated, so 'reconstructed' is right. Every editor generator body is empty, as claimed.

**Data claims I confirmed on the asset:**
- The base/growth arrays.
- 360/360 evolution rows over types {0,1,3,4} x rarities {0-4,6}. Magic and Divine are absent, and the Universal non-mythical rows are 1/0 placeholders.
- 90/90 shard rows with the quoted values.
- Armor rows are identical across types 0/1/3.
- Mythical rows: Value = 50000*L*(L+1), 10L copies, CopyValue 5000.
- The fill rule E_k = Start + (k-1)*Step is exact for all 10 rows.
- The island-to-day lerp reproduces every target timing (max error 1.5e-4) and the interpolation TimingPoints.
- The IslandTargetDays closed form is exact for i = 16..99.
- ReferenceShardsRequirement equals the shards table, and ReferenceStatPercent = round(100*Value/ReferenceStatValue) for all 450 rows.
- Weapon V1/pre-evolution ratio is 2.54-8.4, armor is 1.69-2.47, and the minimum armor step is 1.23.
- CopyValue is monotone in all series.
- All 19 interpolation curves.

**Corrections (minor, no rejections):**
- There are 39 *_div items, not 41.
- SimulationDpsWeaponMultiplier on armor assets is dead data.
- HitFrameAt60Fps has no consumer.
- NextLevel preview has an additional consumer and needs clamp detail.
- The pre-evolution '83000 max' is not a cap, because evolution is manual.
- The CopyValue rule is only approximate: knife and armor common deviate.
- The DropMultiplier applies only when merging into an existing item.
- The simulation DPS divides by 10 for off-type weapons.
- SimulationSystem.ConvertIslandTimingToDays is also a stub.

**Missed formulas (slot system):** weapon-type synergy counts (native), unlocked slot caps / EquippedArmors, and equip-time item StatModifier application (native, currently inert because all StatModifiers are empty).

**Out of scope but relevant to copy pacing:** Shop/ChestsModel.cs:158 gives armor chest copies = ceil(copies / ArmorCountDivider). This belongs to the shop subsystem.

## 公式索引

| ID | 名称 | 状态 | 类型 |
|---|---|---|---|
| [`items.weapon_damage_pre_evolution`](#itemsweapon_damage_pre_evolution) | Weapon damage before first evolution | 重建（无原生标注） | `runtime_formula` |
| [`items.weapon_damage_evolved`](#itemsweapon_damage_evolved) | Weapon damage after evolution | 重建（无原生标注） | `runtime_formula` |
| [`items.armor_pre_evolution`](#itemsarmor_pre_evolution) | Armor value before first evolution | 重建（无原生标注） | `runtime_formula` |
| [`items.armor_evolved`](#itemsarmor_evolved) | Armor value after evolution | 重建（无原生标注） | `runtime_formula` |
| [`items.effective_level`](#itemseffective_level) | Effective / display / next level | 重建（无原生标注） | `runtime_formula` |
| [`items.evolution_level_clamp`](#itemsevolution_level_clamp) | Evolution table level clamp | 重建（无原生标注） | `runtime_formula` |
| [`items.evolution_cost`](#itemsevolution_cost) | Copies required to evolve (shards) | 重建（无原生标注） | `runtime_formula` |
| [`items.evolve_transition`](#itemsevolve_transition) | Evolve state transition | 原生证据恢复 | `runtime_formula` |
| [`items.next_level_preview`](#itemsnext_level_preview) | Next-level damage/armor preview | 重建（无原生标注） | `runtime_formula` |
| [`items.stat_per_copy`](#itemsstat_per_copy) | Stat gained per extra copy | 重建（无原生标注） | `runtime_formula` |
| [`items.weapon_dps`](#itemsweapon_dps) | Item raw DPS | 重建（无原生标注） | `runtime_formula` |
| [`items.copy_merge`](#itemscopy_merge) | Copy acquisition / duplicate merge | 原生证据恢复 | `runtime_formula` |
| [`items.rarity_stat_param`](#itemsrarity_stat_param) | Rarity to meta-stat parameter index | 原生证据恢复 | `runtime_formula` |
| [`items.ability_unlock_gate`](#itemsability_unlock_gate) | Item ability unlock by evolution | 重建（无原生标注） | `config_rule` |
| [`items.chain_weapon_rules`](#itemschain_weapon_rules) | Equipped weapon count / chain-weapon flag | 原生证据恢复 | `runtime_formula` |
| [`items.hit_frame`](#itemshit_frame) | Hit frame timing | 原生证据恢复 | `runtime_formula` |
| [`items.display_rarity`](#itemsdisplay_rarity) | Divine display rarity | 重建（无原生标注） | `config_rule` |
| [`balance.interpolation_evaluate`](#balanceinterpolation_evaluate) | Balance interpolation curve | 原生证据恢复 | `runtime_formula` |
| [`balance.total_coefficient`](#balancetotal_coefficient) | Combined interpolation coefficient by type | 原生证据恢复 | `runtime_formula` |
| [`balance.island_timing_to_days`](#balanceisland_timing_to_days) | Island number to timing (days) conversion | 原生证据恢复 | `runtime_formula` |
| [`balance.island_target_days_curve`](#balanceisland_target_days_curve) | Island target timing curve | 纯数据 | `lookup_table` |
| [`balance.evolution_island_fill_rule`](#balanceevolution_island_fill_rule) | Evolution island schedule fill rule | 编辑器工具缺失 | `editor_generator` |
| [`balance.evolution_target_timings_from_islands`](#balanceevolution_target_timings_from_islands) | Evolution target timings from island schedule | 编辑器工具缺失 | `editor_generator` |
| [`balance.interpolation_points_from_islands`](#balanceinterpolation_points_from_islands) | Interpolation timing points from island points | 编辑器工具缺失 | `editor_generator` |
| [`balance.evolution_shards_capture`](#balanceevolution_shards_capture) | Evolution shard requirement capture | 编辑器工具缺失 | `simulation` |
| [`balance.evolution_value_autobalance`](#balanceevolution_value_autobalance) | Evolution stat value auto-balance | 编辑器工具缺失 | `editor_generator` |
| [`balance.copy_value_rule`](#balancecopy_value_rule) | Per-copy value after evolution | 编辑器工具缺失 | `editor_generator` |
| [`balance.mythical_evolution_curve`](#balancemythical_evolution_curve) | Mythical evolution curve | 编辑器工具缺失 | `editor_generator` |
| [`balance.armor_base_reset`](#balancearmor_base_reset) | Armor base / item cost reset tools | 编辑器工具缺失 | `editor_generator` |
| [`balance.armor_formulas_get`](#balancearmor_formulas_get) | ArmorFormulas.Get(level) | 空壳 | `runtime_formula` |
| [`items.base_growth_tables`](#itemsbase_growth_tables) | Rarity base/growth tables (key levers) | 纯数据 | `lookup_table` |
| [`items.item_config_levers`](#itemsitem_config_levers) | Per-item config levers | 纯数据 | `lookup_table` |
| [`items.weapon_set_synergy_counts`](#itemsweapon_set_synergy_counts) | Equipped weapon-type synergy counts | 原生证据恢复 | `runtime_formula` |
| [`items.unlocked_slot_caps`](#itemsunlocked_slot_caps) | Unlocked slot counts / equipped armor set | 重建（无原生标注） | `runtime_formula` |
| [`items.equip_stat_modifiers`](#itemsequip_stat_modifiers) | Item stat modifiers applied on equip | 原生证据恢复 | `runtime_formula` |
| [`items.auto_equip_and_starting_weapon`](#itemsauto_equip_and_starting_weapon) | Starting weapon and auto-equip policy | 原生证据恢复 | `runtime_formula` |
| [`items.weapon_attack_timing_stub`](#itemsweapon_attack_timing_stub) | WeaponAttackTiming helper (windup/travel/hit ratios) | 空壳 | `runtime_formula` |

## 公式

### `items.weapon_damage_pre_evolution`

**Weapon damage before first evolution**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
dmg = DamageBase[t][r] + DamageGrowth[t][r] * EffectiveLevel + FlatDamage, with EffectiveLevel = Copies (not evolved). Float32 math, then cast to double. Arrays used: t=Buckshot -> Smash*, t=Cutting -> Cut*, anything else (Stabbing/Magic/Universal) -> Pierce*. In the shipped data DamageBase = 0, so dmg = Growth[t][r]*copies + FlatDamage, e.g. knife (cut common) = 12*copies.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `t` | weapon damage type | ItemConfig.DamageType |
| `r` | base rarity index (Config.Rarity, not the display Divine rarity) | ItemConfig.Rarity |
| `DamageBase[t][r]` | base damage by rarity | Combat Balance Config.CutDamageBaseByRarity / PierceDamageBaseByRarity / SmashDamageBaseByRarity (all 0) |
| `DamageGrowth[t][r]` | damage added per copy | Combat Balance Config.Cut/Pierce/SmashDamageGrowthPerLevelByRarity (cut 12,50,140,500,2400,2400,2400; pierce 8,35,100,400,1600x3; smash 25,80,200,700,3500x3) |
| `EffectiveLevel` | Copies while not evolved | items.effective_level |
| `FlatDamage` | per-item flat bonus (mace 400, chainsawpike 5000, coolHammer 15000, chainsaw 30000, all others 0) | ItemConfig.FlatDamage |

**输出**：ItemModel.Damage (double) for non-evolved weapons

**代码**：[Player/Inventory/ItemModel.cs:408-416](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L408)；[Player/Inventory/ItemModel.cs:475-484](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L475)；[Balance/CombatBalanceConfig.cs:156-166](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L156)

**状态依据**：Implemented but carries no native-VA comment. The float32 evaluation order looks deliberately ported, but there is no recovered native address.

**配置参数**：`Combat Balance Config.CutDamageBaseByRarity`；`Combat Balance Config.PierceDamageBaseByRarity`；`Combat Balance Config.SmashDamageBaseByRarity`；`Combat Balance Config.CutDamageGrowthPerLevelByRarity`；`Combat Balance Config.PierceDamageGrowthPerLevelByRarity`；`Combat Balance Config.SmashDamageGrowthPerLevelByRarity`；`ItemConfig.FlatDamage`；`ItemConfig.DamageType`；`ItemConfig.Rarity`

**使用方**：combat: [Services/Damage/DamageCalculator.cs:128](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L128),140 (outgoing dmg = percentStat*(DamageStat + weapon.Damage + flatStat + abilityFlat)*(1-resist)*...)；simulation: [Simulation/Systems/SimulationSystem.cs:923-929](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L923) (dps = percent*(flat+item.Damage)*StatModifierCoef/AttackCooldown*SimulationDpsWeaponMultiplier)；abilities: [Player/Systems/RageExtraHitSystem.cs:48](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/RageExtraHitSystem.cs#L48)；UI previews (InventoryStatView, ChestOpenView, WeaponRewardView, ItemMiniView)

**备注**：CalcDamage(level, copies) ignores the copies argument when not evolved, because the level argument already equals the copy count. Base arrays have 7 entries indexed by Rarity (0..6). Index 5 (Divine) is only reached by the legacy *_div ItemConfigs, and index 6 by Mythical items (no mythical weapons exist). Magic (2) uses the Pierce arrays.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `used_by`：simulation: SimulationSystem.cs:923-929: dps = percent*(flat+item.Damage)*StatModifierCoef/AttackCooldown, divided by 10 when item.DamageType != the evaluated weaponType, then * SimulationDpsWeaponMultiplier. Also SimulationSystem.cs:841-848 (non-fast spawner DPS through DamageCalculator.Calculate(fromSimulation) * SimulationDpsWeaponMultiplier / AttackCooldown).  
  依据：[Simulation/Systems/SimulationSystem.cs:927-929](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L927) contains 'if (item.Config.DamageType != weaponType) dps /= 10.0;', and lines 847-848 are a second Damage consumer.

### `items.weapon_damage_evolved`

**Weapon damage after evolution**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
dmg = EvoDmg[t][r][Lc] + EvoDmgCopy[t][r][Lc] * Copies + FlatDamage, with L = Level (evolution level, >=1) and Lc = min(L, MaxEvolutionLevel)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `L` | evolution level (Level field; 1 after the first evolution) | ItemModel.Level / save ItemDataUd.Level |
| `Copies` | copies left over after the last evolution's cost was paid | ItemModel.Copies |
| `EvoDmg[t][r][L]` | evolved base damage | Combat Balance Config.EvolutionDamageValues[].Value (24 (type,rarity) series x 15 levels) |
| `EvoDmgCopy[t][r][L]` | damage per leftover copy at this evolution | Combat Balance Config.EvolutionDamageValues[].CopyValue |
| `FlatDamage` | per-item flat bonus | ItemConfig.FlatDamage |

**输出**：ItemModel.Damage for evolved weapons

**代码**：[Player/Inventory/ItemModel.cs:477](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L477)；[Player/Inventory/ItemModel.cs:501-509](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L501)；[Balance/CombatBalanceConfig.cs:168-178](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L168)

**状态依据**：No native annotation on CalcEvolutionDamage or GetEvolutionDamage*. Lookups use LINQ First() over (DamageType, Rarity, EvolutionLevel).

**配置参数**：`Combat Balance Config.EvolutionDamageValues`；`Combat Balance Config.MaxEvolutionLevel`；`ItemConfig.FlatDamage`

**使用方**：combat DamageCalculator；simulation SimulationSystem.CalculateDpsData；items.next_level_preview；UI

**备注**：Example: pike (stabbing common) E1 = 3500 + 17*copies, E2 = 21000 + 22*copies, and so on. Rows exist for DamageType 0,1,3,4 and Rarity 0-4 and 6 only. Magic (2) and Divine (5) are missing, so First() would throw. Universal (4) non-mythical rows are placeholders (Value=1, CopyValue=0).

### `items.armor_pre_evolution`

**Armor value before first evolution**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
armor = ArmorBase[r] + ArmorGrowth[r] * EffectiveLevel, with EffectiveLevel = Copies. There is no flat term, and damage type is ignored.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ArmorBase[r]` | armor base by rarity | Combat Balance Config.ArmorBaseByRarity = [600, 4500, 27000, 310000, 2500000, 0, 0] |
| `ArmorGrowth[r]` | armor per copy | Combat Balance Config.ArmorGrowthPerLevelByRarity = [100, 200, 350, 500, 2000, 5000, 5000] |
| `EffectiveLevel` | Copies while not evolved | items.effective_level |

**输出**：ItemModel.Armor for non-evolved helmets/shields

**代码**：[Player/Inventory/ItemModel.cs:418-426](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L418)；[Player/Inventory/ItemModel.cs:491-499](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L491)；[Balance/CombatBalanceConfig.cs:180-192](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L180)

**状态依据**：No native annotation. GetArmorBase and GetArmorGrowthPerLevel take a damageType argument but ignore it.

**配置参数**：`Combat Balance Config.ArmorBaseByRarity`；`Combat Balance Config.ArmorGrowthPerLevelByRarity`

**使用方**：combat: [Services/Damage/DamageCalculator.cs:83-87](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L83) (sum of equipped armors with DamageType==Universal or ==hit type, added to flat defence: ApplyDamageDefense(attack, (univFlat + armorSum + typedFlat)*typedPercent))；simulation: [Simulation/Systems/SimulationSystem.cs:935-956](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L935) (defense = percent*(flat+sum armor)*StatModifierCoef)；UI

**备注**：Example: buck_rare_helmet (golden_spikes) = 27000 + 350*copies until 160 copies (Armor Shards[r2][1]=160), i.e. a max of 83000 pre-evolution. A mythical armor before evolution = 0 + 5000*copies.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：buck_rare_helmet (golden_spikes) = 27000 + 350*copies. That is 83000 at the first evolution threshold of 160 copies, but it is not a cap. Evolution is manual (only InventoryItemContextMenu.cs:126 calls TryEvolve), so pre-evolution copies and stat keep growing linearly until the player evolves. The same applies to the 'jump' examples in items.evolve_transition, which assume the player evolves exactly at the threshold.  
  依据：[Player/Inventory/ItemModel.cs:491-499](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L491) has no clamp. A grep for TryEvolve/EvolveItemCommand shows only the UI button caller ([UI/Inventory/InventoryItemContextMenu.cs:126](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/Inventory/InventoryItemContextMenu.cs#L126)).

### `items.armor_evolved`

**Armor value after evolution**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
armor = EvoArm[t][r][Lc] + EvoArmCopy[t][r][Lc] * Copies, with Lc = min(Level, MaxEvolutionLevel)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `EvoArm` | evolved armor base | Combat Balance Config.EvolutionArmorValues[].Value |
| `EvoArmCopy` | armor per leftover copy | Combat Balance Config.EvolutionArmorValues[].CopyValue |
| `t` | armor's damage type (which hit type it defends against) | ItemConfig.DamageType |

**输出**：ItemModel.Armor for evolved armor

**代码**：[Player/Inventory/ItemModel.cs:493](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L493)；[Player/Inventory/ItemModel.cs:511-518](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L511)；[Balance/CombatBalanceConfig.cs:194-204](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L194)

**状态依据**：No native annotation.

**配置参数**：`Combat Balance Config.EvolutionArmorValues`；`Combat Balance Config.MaxEvolutionLevel`

**使用方**：combat DamageCalculator incoming branch；simulation CalculateDefenseData；UI

**备注**：Armor evolution values are identical across DamageType 0, 1 and 3 for each rarity, which is the effect of SyncShieldValuesByLeader=1. Example: common E1 15000 + 100*c, rare E1 140000 + 350*c, legendary E1 5e6 + 7500*c, E15 5e8 + 8000*c. Universal (4) rows are placeholders (1/0) except Mythical: mushroommancer_cap/shield use the r=6 rows 100000*... (see items.mythical_evolution_curve).

### `items.effective_level`

**Effective / display / next level**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
EffectiveLevel = Evolved ? Level : Copies; DisplayLevel = EffectiveLevel; NextEffectiveLevel = EffectiveLevel + 1
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Evolved` | item has evolved at least once | ItemDataUd.Evolved |
| `Level` | evolution level | ItemDataUd.Level |
| `Copies` | copy count | ItemDataUd.Copies |

**输出**：level index fed to CalcDamage/CalcArmor

**代码**：[Player/Inventory/ItemModel.cs:135-163](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L135)

**状态依据**：No native-VA comment on these computed atoms.

**使用方**：items.weapon_damage_pre_evolution；items.armor_pre_evolution；sorting in InventoryView/ChestOpenFlowRunner/PlayerModel.UnequippedItems

**备注**：Before evolution the 'level' is just the copy count, and the Level field stays at 1 (constructor sets level=1, copies=1: ItemModel.cs:531-532).

### `items.evolution_level_clamp`

**Evolution table level clamp**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
Lc = min(L, MaxEvolutionLevel)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `L` | requested evolution level | ItemModel.Level or NextEvolutionLevel |
| `MaxEvolutionLevel` | last authored evolution row | Combat Balance Config.MaxEvolutionLevel = 15 |

**输出**：row index into EvolutionDamage/ArmorValues and shard tables

**代码**：[Balance/CombatBalanceConfig.cs:147-150](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L147)

**状态依据**：Simple implemented body, no native annotation.

**配置参数**：`Combat Balance Config.MaxEvolutionLevel`

**使用方**：items.weapon_damage_evolved；items.armor_evolved；items.evolution_cost

**备注**：Beyond 15 the stat stops growing, but CanEvolve/EvolveItemCommand have no cap. Level can keep incrementing while still consuming Shards[15].

### `items.evolution_cost`

**Copies required to evolve (shards)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
NextEvolutionLevel = Level + (Evolved ? 1 : 0); LevelsToEvolve = Shards[isArmor][r][min(NextEvolutionLevel, 15)]; CanEvolve = Copies >= LevelsToEvolve; total copies to reach evolution E = sum_{k=1..E} Shards[isArmor][r][k] (+ leftovers)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `isArmor` | SlotType is Helmet or Shield | ItemConfig.SlotType |
| `Shards[0][r][k]` | weapon copies cost of evolution k | Combat Balance Config.EvolutionDamageShardsRequirements (common 100,390,800,1600,3000,3700,6000,10000,11000...; uncommon 130,500,1200,2200,2700,6000,7000,7500...; rare 130,500,1300,1800,4500,5500,6000...; epic 110,400,750,1700,2200,2500...; legendary 70,180,750,1200...; mythical 10k) |
| `Shards[1][r][k]` | armor copies cost | Combat Balance Config.EvolutionArmorShardsRequirements (common 70,130,600,1200,1600,3400,5000,8000,9000...; uncommon 100,170,600,1200,2300,4000,5500,6000...; rare 160,270,900,1800,2600,2900...; epic 190,550,1000,1600...; legendary 160,500,500...; mythical 10k) |

**输出**：ItemModel.LevelsToEvolve, ItemModel.CanEvolve

**代码**：[Player/Inventory/ItemModel.cs:165](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L165)；[Player/Inventory/ItemModel.cs:167-175](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L167)；[Player/Inventory/ItemModel.cs:448](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L448)；[Balance/CombatBalanceConfig.cs:210-215](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L210)

**状态依据**：No native annotation on LevelsToEvolve, CanEvolve or GetEvolutionShardsRequirement.

**配置参数**：`Combat Balance Config.EvolutionDamageShardsRequirements`；`Combat Balance Config.EvolutionArmorShardsRequirements`；`Combat Balance Config.MaxEvolutionLevel`

**使用方**：items.evolve_transition；items.next_level_preview；UI (evolve button, notifications, ability unlock text)

**备注**：Shards rows exist only for Rarity 0-4 and 6. A Rarity-5 (*_div) config would throw. The first evolution reads row k=Level=1 while not evolved; afterwards row k=Level+1. Each EvolutionCombatValue.ReferenceShardsRequirement equals the matching shards value.

### `items.evolve_transition`

**Evolve state transition**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x146851c (EvolveItemCommandRunner.Execute), 0x14658b4 (ItemModel.TryEvolve)`

```text
if CanEvolve: Copies' = Copies - LevelsToEvolve; if Evolved: Level' = Level + 1 else Evolved' = true (Level stays 1)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `LevelsToEvolve` | current shards requirement | items.evolution_cost |

**输出**：new (Level, Copies, Evolved); ability set rebuilt; rage max level recalculated

**代码**：[Player/Commands/EvolveItemCommandRunner.cs:25-44](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Commands/EvolveItemCommandRunner.cs#L25)；[Player/Inventory/ItemModel.cs:536-542](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L536)

**状态依据**：Comments 'Native 0x146851c' and 'Native 0x14658b4'.

**配置参数**：`Combat Balance Config.EvolutionDamageShardsRequirements`；`Combat Balance Config.EvolutionArmorShardsRequirements`

**使用方**：abilities: GrantRageAccessEffect.RecalculateMaxLevel；EvolveItemFlow (UI)

**备注**：Leftover copies carry over and are immediately valued at the new CopyValue. The jump at the first evolution is from Growth*Shards[1] (+flat) to EvoValue[1] + CopyValue[1]*leftover. Example: knife goes from 1200 to 7000, about 5.8x.

### `items.next_level_preview`

**Next-level damage/armor preview**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
NextLevelDamage = Evolved ? EvoDmg[L+1] + EvoDmgCopy[L+1]*max(0, Copies - LevelsToEvolve) + FlatDamage : CalcDamage(Copies + 1); the same for armor with the armor tables (no flat term)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `L` | current evolution level | ItemModel.Level |

**输出**：ItemModel.NextLevelDamage / NextLevelArmor

**代码**：[Player/Inventory/ItemModel.cs:428-450](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L428)

**状态依据**：No native annotation.

**配置参数**：`Combat Balance Config.EvolutionDamageValues`；`Combat Balance Config.EvolutionArmorValues`

**使用方**：[UI/Inventory/ItemMiniView.cs:98-100](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/Inventory/ItemMiniView.cs#L98)

**备注**：Preview only; not used in combat.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `used_by`：UI/Inventory/ItemMiniView.cs:98-100, UI/Inventory/InventoryItemContextMenu.cs:185,190  
  依据：grep for NextLevelDamage/NextLevelArmor
- [待人工合入] `expression`：NextLevelDamage = Evolved ? EvoDmg[min(L+1,15)] + EvoDmgCopy[min(L+1,15)]*max(0, Copies - Shards[isArmor][r][min(L+1,15)]) + FlatDamage : DamageGrowth*(Copies+1) + DamageBase + FlatDamage. The non-evolved branch previews +1 copy, not the first evolution. Armor is the same without FlatDamage.  
  依据：[Player/Inventory/ItemModel.cs:434](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L434),444,448,450 and GetExistingEvolutionLevel clamp at [Balance/CombatBalanceConfig.cs:147-150](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L147).

### `items.stat_per_copy`

**Stat gained per extra copy**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
StatPerCopy = Evolved ? CopyValue[isArmor][t][r][min(Level,15)] : (isArmor ? ArmorGrowth[r] : DamageGrowth[t][r])
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `CopyValue` | per-copy stat at the current evolution | EvolutionDamageValues/EvolutionArmorValues.CopyValue |
| `Growth` | pre-evolution per-copy stat | *GrowthPerLevelByRarity |

**输出**：ItemModel.StatPerCopy / CopyStatGrowth (marginal value of one copy)

**代码**：[Player/Inventory/ItemModel.cs:452-462](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L452)

**状态依据**：No native annotation.

**配置参数**：`Combat Balance Config.*GrowthPerLevelByRarity`；`Combat Balance Config.EvolutionDamageValues`；`Combat Balance Config.EvolutionArmorValues`

**使用方**：[UI/World/WeaponRewardView.cs:86](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/World/WeaponRewardView.cs#L86)；[UI/Inventory/InventoryItemContextMenu.cs:198](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/Inventory/InventoryItemContextMenu.cs#L198)

**备注**：This is the derivative d(stat)/d(copies) for the model.

### `items.weapon_dps`

**Item raw DPS**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
DPS = Damage / AttackCooldown
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Damage` | item damage | items.weapon_damage_pre_evolution / items.weapon_damage_evolved |
| `AttackCooldown` | seconds per attack (e.g. pike 1, knife 1.3, paddle 2, chainsawpike 0.75) | ItemConfig.AttackCooldown |

**输出**：ItemModel.DPS (ignores player stats)

**代码**：[Player/Inventory/ItemModel.cs:398-406](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L398)

**状态依据**：No native annotation.

**配置参数**：`ItemConfig.AttackCooldown`

**使用方**：simulation: SimulationSystem.cs:917 (weapon auto-pick order)；abilities: TriggerHighestDamageWeaponEffect.cs:30, ResetCooldownEffect.cs:43；UI: InventoryEquipAllButton, InventoryView sort

**备注**：The real fight DPS also applies stat percent/flat terms and SimulationDpsWeaponMultiplier (simulation only).

### `items.copy_merge`

**Copy acquisition / duplicate merge**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x145de6c (AddItem), 0x145f44c (CreateItem), 0x145f4e4 (SetInitialCopies)`

```text
if an item with the same Key exists: existing.Copies += DropMultiplier * incoming.Copies; else new item (Level=1, Copies=incoming copies, Evolved=false) is added and auto-equipped into a free unlocked slot of its SlotType
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `incoming.Copies` | copies granted (spawner drop = ItemDropAmountCalculator.CalculatePerKill = 1 + items_rarity_drop_amount[r+1]; rewards use itemReward.Level as copies) | GrantSpawnerItemRewardCommand.Copies / reward configs |
| `DropMultiplier` | cheat multiplier (1 in normal play) | CheatsModel.DropMultiplier |

**输出**：ItemModel.Copies

**代码**：[Player/Models/PlayerModel.cs:705-731](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L705)；[Player/Models/PlayerModel.cs:738-747](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L738)；[Enemy/Commands/GrantSpawnerItemRewardCommandHandler.cs:24-32](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerItemRewardCommandHandler.cs#L24)；[Player/Inventory/ItemModel.cs:525-534](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L525)

**状态依据**：Native VA comments on AddItem/CreateItem/SetInitialCopies.

**配置参数**：`Meta Stats Config.items_rarity_drop_amount (meta subsystem)`

**使用方**：all reward/drop flows: spawners, chests (ChestOpenFlowRunner.cs:140-150), events, shop

**备注**：Identity is Config.Key, so copies of one item stack forever. Copies are the only progression currency for items; there is no separate shard item. Drop-amount math belongs to the meta subsystem.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression / variables`：incoming = (command.Copies >= 1 ? command.Copies : CalculatePerKill) for spawner drops, and reward.Level for GrantRewardCommandRunner. If an item with the same Key exists: existing.Copies += DropMultiplier * incoming. Otherwise the new item is added with Copies = incoming and no DropMultiplier, so the first drop is not multiplied.  
  依据：[Enemy/Commands/GrantSpawnerItemRewardCommandHandler.cs:28-29](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerItemRewardCommandHandler.cs#L28); [Rewards/Commands/GrantRewardCommandRunner.cs:292](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/Commands/GrantRewardCommandRunner.cs#L292); [Player/Models/PlayerModel.cs:708-713](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L708) (multiplier only in the existing branch) vs 724-735.

### `items.rarity_stat_param`

**Rarity to meta-stat parameter index**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x146639c, 0x14663a4`

```text
param = rarity + 1; rarity = param - 1
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `rarity` | Rarity enum int | ItemConfig.Rarity |

**输出**：int param key for rarity-indexed meta stats

**代码**：[Player/Inventory/RarityStatParam.cs:5-15](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/RarityStatParam.cs#L5)

**状态依据**：Comments cite the two-instruction native bodies.

**使用方**：meta: [Meta/ItemDropAmountCalculator.cs:12](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/ItemDropAmountCalculator.cs#L12) (items_rarity_drop_amount)；meta: [Meta/SpawnerTimerCalculator.cs:22](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SpawnerTimerCalculator.cs#L22) (items_rarity_timer)；meta: [Meta/SpawnerChargesCalculator.cs:33](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SpawnerChargesCalculator.cs#L33)

**备注**：Rarity is a 1-based parameter in Meta Stats Config tables.

### `items.ability_unlock_gate`

**Item ability unlock by evolution**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`config_rule`
- 来源：子系统梳理
- 原生地址：`AbilityState: 0x15e306c (Stars), 0x15e3120 (GetCurrentOverride)`

```text
unlocked(def) = Evolved ? (def.UnlockLevel <= Level) : (def.UnlockLevel == 0); ability stars = IsPreDivine ? 0 : Level (AbilityState.Stars); value = the LevelScaling override with the greatest Level <= stars, else def.Value
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `UnlockLevel` | evolution level needed | ItemConfig.AbilityDefs[].UnlockLevel |
| `LevelScaling` | per-evolution value overrides (e.g. knife ability 7,11,15,...,120 for E1..E15) | ItemConfig.AbilityDefs[].LevelScaling |

**输出**：active ability list and ability magnitudes per item

**代码**：[Player/Inventory/ItemModel.cs:520-523](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L520)；[Player/Inventory/ItemModel.cs:561-573](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L561)；[Abilities/AbilityState.cs:70](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L70)；[Abilities/AbilityState.cs:186-198](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L186)

**状态依据**：IsAbilityUnlocked/RebuildAbilityStates have no native annotation. The downstream AbilityState lookups are native-annotated (abilities subsystem).

**配置参数**：`ItemConfig.AbilityDefs`

**使用方**：abilities subsystem；DamageCalculator (ability multipliers)

**备注**：Evolution level is a second axis of item power, through ability tiers, on top of the stat tables.

### `items.chain_weapon_rules`

**Equipped weapon count / chain-weapon flag**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x1465964, 0x1465d48, 0x1465a78, 0x1465df0`

```text
EquippedWeaponCount = count(slots: equipped and SlotType==Weapon); HasChainWeapon = any(equipped weapon slot with key in {weapon_2, weapon_3, weapon_4, weapon_5_vip})
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ItemSlots` | player slots | PlayerModel.ItemSlots |

**输出**：int / bool

**代码**：[Player/Inventory/ItemSlotKeys.cs:23-35](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemSlotKeys.cs#L23)

**状态依据**：Native VA comments.

**使用方**：[Player/PlayerAttackSystem.cs:242](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L242) (attack range *3 vs weak enemies/event golems when a chain weapon is equipped)；[Player/WeaponView.cs:465](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/WeaponView.cs#L465)

**备注**：Slot count limits how many items contribute damage and armor (simulation uses UnlockedWeaponSlots/UnlockedArmorSlots).

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression`：HasChainWeapon = any(slot: slot.IsEquipped and slot.SlotType == Weapon and slot.Key (the SLOT key, not the item key) in {weapon_2, weapon_3, weapon_4, weapon_5_vip}). In effect this means 'more than the first weapon slot is filled'. Consumers: range *= 3 vs weak enemies/event golems (PlayerAttackSystem.cs:242); WeaponView.cs:465 uses EquippedWeaponCount >= 2 and the chain flag to override the Cut attack animation type.  
  依据：[Player/Inventory/ItemSlotKeys.cs:23-35](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemSlotKeys.cs#L23) ChainWeaponSlotKeys and slot.Key; [Player/WeaponView.cs:465](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/WeaponView.cs#L465)

### `items.hit_frame`

**Hit frame timing**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x14630e8`

```text
HitFrameAt60Fps = (1 - AnimationCastPoint) * AttackCooldown * 60
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `AnimationCastPoint` | normalized cast point | ItemConfig.AnimationCastPoint |
| `AttackCooldown` | attack period | ItemConfig.AttackCooldown |

**输出**：frame index of the hit

**代码**：[Player/Inventory/ItemConfig.cs:49](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemConfig.cs#L49)

**状态依据**：Inline 'Native 0x14630e8' comment.

**配置参数**：`ItemConfig.AnimationCastPoint`；`ItemConfig.AttackCooldown`

**使用方**：weapon animation/attack timing

**备注**：Mostly presentation; listed because it times the hit inside the cooldown.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `used_by`：[] (no consumer of ItemConfig.HitFrameAt60Fps in the reconstruction)  
  依据：A grep for HitFrameAt60Fps across CodeBase matches only its definition at [Player/Inventory/ItemConfig.cs:49](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemConfig.cs#L49).

### `items.display_rarity`

**Divine display rarity**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`config_rule`
- 来源：子系统梳理

```text
IsDivine = Evolved and Config.Rarity != Mythical; ModelRarity = IsDivine ? Divine(5) : Config.Rarity
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Evolved` | evolved flag | ItemDataUd.Evolved |

**输出**：ItemModel.Rarity (used for sorting/visuals only)

**代码**：[Player/Inventory/ItemModel.cs:107](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L107)；[Player/Inventory/ItemModel.cs:130](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L130)

**状态依据**：No native annotation on IsDivine/Rarity (CurrentIcon/CurrentPrefab nearby are native).

**使用方**：InventoryView/ChestOpenFlowRunner sort；UI visuals

**备注**：All stat formulas use Config.Rarity, so 'Divine' does not change the table index.

### `balance.interpolation_evaluate`

**Balance interpolation curve**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x1579ed4 (Evaluate), 0x157a65c (ParsePoints)`

```text
Piecewise-linear over (T_i, C_i): f(x) = C_0 if x <= T_0; for the first i with T_{i+1} >= x: f = C_i + clamp01((x - T_i)/(T_{i+1} - T_i)) * (C_{i+1} - C_i); f = C_last if x > T_last. Invalid if fewer than 2 points or counts differ (returns 0).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `T_i` | timing points (semicolon list) | Combat Balance Config.Interpolations.Interpolations[].TimingPoints |
| `C_i` | coefficient points | Combat Balance Config.Interpolations.Interpolations[].CoefPoints |
| `x` | balance timing (day / time chunk) | SimulationSystem.GetBalanceTimingMinutes or caller |

**输出**：float coefficient

**代码**：[Balance/BalanceInterpolation.cs:23-52](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/BalanceInterpolation.cs#L23)；[Balance/BalanceInterpolation.cs:58-64](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/BalanceInterpolation.cs#L58)

**状态依据**：Native VA comments, with NaN ordering preserved.

**配置参数**：`Combat Balance Config.Interpolations`

**使用方**：balance.total_coefficient

**备注**：Parsing uses InvariantCulture floats and skips empty items.

### `balance.total_coefficient`

**Combined interpolation coefficient by type**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x1579dac, 0x157dd80, 0x157dd98`

```text
type' = (rarity == Legendary and type == ItemEmission) ? ItemLegendaryEmission : type; for multiplicative types {1 ItemChainProbability, 3 StatModifier}: K = product of f_j(x) over enabled curves of type'; for additive types {0,2,4,5,6,7}: K = sum of f_j(x)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `f_j` | each enabled (Use=1) curve of the type | balance.interpolation_evaluate |
| `rarity` | defaults to Rare | caller |

**输出**：CombatBalanceConfig.GetInterpolationCoefficient

**代码**：[Balance/BalanceInterpolationsConfig.cs:12-27](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/BalanceInterpolationsConfig.cs#L12)；[Balance/CombatBalanceConfig.cs:135-145](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L135)

**状态依据**：Native VA comments; bitmask 0xf5 selects addition.

**配置参数**：`Combat Balance Config.Interpolations`

**使用方**：simulation: SimulationSystem.cs:877-887 (PlayerAbilityDPSMult+1, PlayerAblityEHPMult+1), :925 & :953 (StatModifier multiplier on DPS/defense)；simulation: SimulationConfig.cs:120-121 (spawner bake)；simulation: [Forecast/ForecastSnapshots.cs:54-55](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Forecast/ForecastSnapshots.cs#L54)；boss: [BossTower/BossSpawnService.cs:457-458](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/BossSpawnService.cs#L457)

**备注**：Shipped curves: StatModifier = GraphMult(1->1.6) * AmuletMult(1->1.2->1.8) * EventsMult(1->1.1->1.5->2.0). PlayerAbilityDPSMult = AbilityDPS 0->0.4->1.3->4 at timing 20/90/150/775. PlayerAblityEHPMult 0->0.2->0.5->1.5. StatsFromFeaturesFlat sums GraphFlat, AmuletFlat, VIP, InApps, EventsFlat, Ads and ItemsEconomic. ItemEmission sums Tower, GraphMapUpgrade and EventItems. ItemLegendaryEmission sums TowerLeg and GraphMapUpgrade. ItemEmissionByType = TowerByType. ItemChainProbability = FarmItem 0.9->1. Types 0,1,2,6,7 have no runtime consumer in the reconstruction (SimulationSystem.GetSimulationInterpolationCoefficient / UpdateInterpolationStatModifier are stubs).

### `balance.island_timing_to_days`

**Island number to timing (days) conversion**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x157ad44`

```text
i = floor(x), f = x - i; prev = (i == 1) ? 0 : IslandTargetDays[i-2]; days = (f == 0) ? prev : prev + f * (IslandTargetDays[i-1] - prev)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `x` | fractional island number (7.5 = halfway through island 7) | fill rows / IslandPoints / caller |
| `IslandTargetDays[k]` | target timing when island k+1 is finished | Combat Balance Config.IslandTargetDays (99 entries) |

**输出**：timing on the balance axis

**代码**：[Simulation/Forecast/ForecastSnapshots.cs:68-75](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastSnapshots.cs#L68)；[Balance/CombatBalanceConfig.cs:252-255](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L252)

**状态依据**：Native body inlined in ForecastSnapshots ('Original 0x157ad44'). The CombatBalanceConfig.ConvertIslandTimingToDays wrapper itself is still a stub returning 0.

**配置参数**：`Combat Balance Config.IslandTargetDays`

**使用方**：balance.evolution_target_timings_from_islands；balance.interpolation_points_from_islands；simulation forecast

**备注**：Checked against the asset: every *EvolutionTargetTimings value and every interpolation TimingPoints value equals this function applied to the stored island numbers (max error 1.5e-4).

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `status_evidence`：The same, and SimulationSystem.ConvertIslandTimingToDays (Simulation/Systems/SimulationSystem.cs:528-531) is also a stub returning 0f. Only the ForecastSnapshots copy (Simulation/Forecast/ForecastSnapshots.cs:68-75) is functional. Note that the index is the previous island's completion: x=9 gives IslandTargetDays[7]=26, the end of island 8.  
  依据：SimulationSystem.cs:528-531 body 'return 0f;'

### `balance.island_target_days_curve`

**Island target timing curve**

- 状态：**纯数据**（`data_only`）
- 类型：`lookup_table`
- 来源：子系统梳理

```text
D(i) = IslandTargetDays[i-1]: hand values for i<=16 (0.5, 1.833, 4, 7, 10, 14, 20, 26, 34, 44, 56, 71, 90, 110, 130, 150). For i>=16: D(i) = 150 + 30*(i-16) + (5/6)*(i-16)*(i-17), i.e. increments 30, 31.67, 33.33, ... (+5/3 each).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `i` | island number 1..99 | Locations |

**输出**：island completion timing

**代码**：[Balance/CombatBalanceConfig.cs:31-32](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L31)；[Balance/CombatBalanceConfig.cs:103-105](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L103)

**状态依据**：Stored array (formerly IslandTargetTimings). The generator FillIslandTargetDaysByGateSpawners is an empty editor method. The closed form was fitted from data and is exact to float precision up to D(99)=8311.667.

**配置参数**：`Combat Balance Config.IslandTargetDays`

**使用方**：balance.island_timing_to_days；simulation: SimulationConfig.cs:224 (early spawner optimum-time offset)；simulation forecast

**备注**：This is the master pacing lever. Changing it shifts every evolution target timing and every interpolation curve, but only after re-running the missing editor apply steps, because the derived arrays are baked.

### `balance.evolution_island_fill_rule`

**Evolution island schedule fill rule**

- 状态：**编辑器工具缺失**（`editor_only_missing`）
- 类型：`editor_generator`
- 来源：子系统梳理

```text
E_k(r, kind) = StartValue(r, kind) + (k - 1) * StepPerEvolution(r, kind), k = 1..15
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `StartValue` | island of first evolution (weapon 7.5/9/10/11.5/12.5; armor 8/9.5/11.75/15/18.5 for C/U/R/E/L) | Combat Balance Config.EvolutionIslandFillRuleRows[].StartValue |
| `StepPerEvolution` | islands between evolutions (1.75/2/2.25/2.5/3.5, the same for weapon and armor) | Combat Balance Config.EvolutionIslandFillRuleRows[].StepPerEvolution |

**输出**：EvolutionIslandTimingRows[].E1..E15

**代码**：[Balance/CombatBalanceConfig.cs:95-97](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L95)；[Balance/EvolutionIslandFillRuleRow.cs:8-20](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/EvolutionIslandFillRuleRow.cs#L8)；[Balance/EvolutionIslandTimingRow.cs:51-58](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/EvolutionIslandTimingRow.cs#L51)

**状态依据**：FillIslandTimingsByStartStep is empty, and EvolutionIslandTimingRow.GetEvolutionIsland/SetEvolutionIsland are stubs. The output rows match this linear rule exactly for all 10 rows.

**配置参数**：`Combat Balance Config.EvolutionIslandFillRuleRows`；`Combat Balance Config.EvolutionIslandTimingRows`

**使用方**：balance.evolution_target_timings_from_islands

**备注**：Design intent: the item of rarity r reaches evolution k when the player is on island E_k.

### `balance.evolution_target_timings_from_islands`

**Evolution target timings from island schedule**

- 状态：**编辑器工具缺失**（`editor_only_missing`）
- 类型：`editor_generator`
- 来源：子系统梳理

```text
<Rarity>[Armor]EvolutionTargetTimings[k-1] = ConvertIslandTimingToDays(EvolutionIslandTimingRows[rarity, kind].E_k)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `E_k` | island of evolution k | balance.evolution_island_fill_rule |

**输出**：Common..Legendary(Armor)EvolutionTargetTimings (15 each)

**代码**：[Balance/CombatBalanceConfig.cs:99-101](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L99)；[Balance/CombatBalanceConfig.cs:152-154](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L152)；[Balance/CombatBalanceConfig.cs:229-237](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L229)；[Balance/CombatBalanceConfig.cs:247-250](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L247)

**状态依据**：ApplyIslandTimingsToDayTimings, ConvertIslandRowToDayTimings, FillEvolutionTargetTimingsByFormula and Get(Armor)EvolutionTargetTimings are empty or return null. The relation was verified on the asset (max error 1.5e-4).

**配置参数**：`Combat Balance Config.CommonEvolutionTargetTimings`；`Combat Balance Config.UncommonEvolutionTargetTimings`；`Combat Balance Config.RareEvolutionTargetTimings`；`Combat Balance Config.EpicEvolutionTargetTimings`；`Combat Balance Config.LegendaryEvolutionTargetTimings`；`Combat Balance Config.CommonArmorEvolutionTargetTimings`；`Combat Balance Config.UncommonArmorEvolutionTargetTimings`；`Combat Balance Config.RareArmorEvolutionTargetTimings`；`Combat Balance Config.EpicArmorEvolutionTargetTimings`；`Combat Balance Config.LegendaryArmorEvolutionTargetTimings`

**使用方**：balance.evolution_shards_capture；balance.evolution_value_autobalance

**备注**：No runtime code reads these arrays; they only drive the editor balancing pipeline.

### `balance.interpolation_points_from_islands`

**Interpolation timing points from island points**

- 状态：**编辑器工具缺失**（`editor_only_missing`）
- 类型：`editor_generator`
- 来源：子系统梳理

```text
TimingPoints_j = ConvertIslandTimingToDays(IslandPoints_j)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `IslandPoints` | island-number knots of a curve | Combat Balance Config.Interpolations.Interpolations[].IslandPoints |

**输出**：Interpolations[].TimingPoints (string)

**代码**：[Balance/BalanceInterpolation.cs:54-56](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/BalanceInterpolation.cs#L54)；[Balance/BalanceInterpolation.cs:66-69](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/BalanceInterpolation.cs#L66)；[Balance/CombatBalanceConfig.cs:131-133](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L131)

**状态依据**：ApplyIslandTimings/ApplyInterpolationIslandsToTimings are empty and FormatPoint returns null. Verified on every curve (e.g. island 9 -> 26, 17 -> 150, 22 -> 316.667, 32 -> 775).

**配置参数**：`Combat Balance Config.Interpolations`

**使用方**：balance.interpolation_evaluate

**备注**：Designers author curves in island numbers; runtime only reads TimingPoints.

### `balance.evolution_shards_capture`

**Evolution shard requirement capture**

- 状态：**编辑器工具缺失**（`editor_only_missing`）
- 类型：`simulation`
- 来源：子系统梳理

```text
Shards[isArmor][r][k] = round_nice(copies of a reference item of rarity r the simulated player owns at playtime ~= TargetTimings[r][k-1]); stored also as EvolutionCombatValue.ReferenceShardsRequirement. Rows beyond the simulated horizon are held at the last value. The initial sentinel is InitialShardsRequirement = 1e9.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `items` | simulated inventory at playtime | Simulation Data snapshots |
| `TargetTimings` | evolution target timing | balance.evolution_target_timings_from_islands |

**输出**：EvolutionDamageShardsRequirements / EvolutionArmorShardsRequirements

**代码**：[Balance/CombatBalanceConfig.cs:13](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L13)；[Balance/CombatBalanceConfig.cs:123-125](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L123)；[Balance/CombatBalanceConfig.cs:239-246](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L239)

**状态依据**：UpdateEvolutionShardsRequirements(playtime, items), TryCaptureShardsRequirement and SetShardsValue are empty. The exact capture/rounding rule is inferred from their signatures; only the output tables survive.

**配置参数**：`Combat Balance Config.EvolutionDamageShardsRequirements`；`Combat Balance Config.EvolutionArmorShardsRequirements`

**使用方**：items.evolution_cost

**备注**：The tables plateau (e.g. common weapon at 11000 from E9), which matches the end of the simulated horizon. Mythical shards = 10*k come from the mythical generator instead.

### `balance.evolution_value_autobalance`

**Evolution stat value auto-balance**

- 状态：**编辑器工具缺失**（`editor_only_missing`）
- 类型：`editor_generator`
- 来源：子系统梳理

```text
Value[t][r][L] ≈ round_nice(ReferenceHeuristicValue); ReferenceStatPercent = round(100 * Value / ReferenceStatValue); constraints: Value[1] >= MinEvolutionValueFromShardsMultiplier * preEvoStat(Shards[1]) (weapons) and ArmorValue[L] >= MinArmorEvolutionValueFromPreviousEvolutionMultiplier * ArmorValue[L-1] (soft). After the simulation horizon (ReferenceTimeChunkIndex saturates) the remaining levels are filled by roughly constant ΔValue steps (FillRemainingEvolutionsByLastValue).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ReferenceHeuristicValue` | target stat from the simulation heuristic at that time chunk | EvolutionCombatValue.ReferenceHeuristicValue |
| `ReferenceStatValue` | player's simulated current stat at that chunk | EvolutionCombatValue.ReferenceStatValue |
| `ReferenceTimeChunkIndex` | simulation chunk used | EvolutionCombatValue.ReferenceTimeChunkIndex |
| `MinEvolutionValueFromShardsMultiplier` | 2 | Combat Balance Config |
| `MinArmorEvolutionValueFromPreviousEvolutionMultiplier` | 1.3 | Combat Balance Config |

**输出**：EvolutionDamageValues[].Value / EvolutionArmorValues[].Value

**代码**：[Balance/CombatBalanceConfig.cs:206-208](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L206)；[Balance/CombatBalanceConfig.cs:217-227](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L217)；[Balance/CombatBalanceConfig.cs:115-121](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L115)；[Balance/EvolutionCombatValue.cs:8-29](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/EvolutionCombatValue.cs#L8)

**状态依据**：ApplyEvolutionAutoBalance, SetEvolutionValue (both overloads), FillRemainingEvolutionValues and FillRemainingEvolutionsByLastValue are empty. The relations were fitted from data: ReferenceStatPercent matches exactly, and Value is a nice rounding of the heuristic (3461.8->3500, 20675->21000, 304372->300000).

**配置参数**：`Combat Balance Config.EvolutionDamageValues`；`Combat Balance Config.EvolutionArmorValues`；`Combat Balance Config.MinEvolutionValueFromShardsMultiplier`；`Combat Balance Config.MinArmorEvolutionValueFromPreviousEvolutionMultiplier`；`Combat Balance Config.SyncShieldValuesByLeader`

**使用方**：items.weapon_damage_evolved；items.armor_evolved

**备注**：Observed: weapon V1/preEvoMax is 2.5-8.4. Armor V1/preEvoMax is 1.69-2.47 and the armor min step ratio is 1.23, so the 2.0/1.3 minimums are not strictly enforced; there were hand edits. With SyncShieldValuesByLeader=1, armor rows are copied from a leader type to all of types 0/1/3.

### `balance.copy_value_rule`

**Per-copy value after evolution**

- 状态：**编辑器工具缺失**（`editor_only_missing`）
- 类型：`editor_generator`
- 来源：子系统梳理

```text
CopyValue[L] ≈ max(CopyValue[L-1] * MinCopyValueGrowthMultiplier, round_nice(CopyValueTargetShare * (Value[L] - Value[L-1]) / Shards[L])), with Value[0] = 0; frozen at the last value beyond the simulated horizon
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `CopyValueTargetShare` | 0.5: the grind to evolution L is worth half the stat jump | Combat Balance Config.CopyValueTargetShare |
| `MinCopyValueGrowthMultiplier` | 1: CopyValue never decreases | Combat Balance Config.MinCopyValueGrowthMultiplier |
| `Shards[L]` | copies paid for evolution L | balance.evolution_shards_capture |

**输出**：EvolutionDamageValues[].CopyValue / EvolutionArmorValues[].CopyValue

**代码**：[Balance/CombatBalanceConfig.cs:17-19](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L17)；[Balance/CombatBalanceConfig.cs:225-227](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L225)

**状态依据**：Generator bodies are empty. The fit is close in the simulated range (pike common 17.5/22.4/33.8/70.3 vs stored 17/22/33/70), exact for Mythical (5000), and CopyValue is monotone non-decreasing in all 48 series.

**配置参数**：`Combat Balance Config.CopyValueTargetShare`；`Combat Balance Config.MinCopyValueGrowthMultiplier`

**使用方**：items.weapon_damage_evolved；items.armor_evolved；items.stat_per_copy

**备注**：The rounding rule is unknown; the fit drifts after the reference time chunk saturates.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `status_evidence`：The fit is close for pike common but deviates in both directions in other series. Knife (cut common): predicted 35/34.6/35 vs stored 34/34/37. Armor common E1: predicted 107 vs stored 100. The MinCopyValueGrowth floor explains E2 (96 -> 100) but not E1. The rounding rule is unknown and probably includes hand edits. Treat the rule as approximate, not as an exact generator.  
  依据：Combat Balance Config.asset parsed: EvolutionDamageValues (1,0) = (7000,34),(34000,34),(90000,37); weapon shards common = 100, 390, 800; EvolutionArmorValues (0,0) E1 = (15000,100) with armor shards common[1] = 70.

### `balance.mythical_evolution_curve`

**Mythical evolution curve**

- 状态：**编辑器工具缺失**（`editor_only_missing`）
- 类型：`editor_generator`
- 来源：子系统梳理

```text
Value(L) = 50000 * L * (L + 1); CopiesToEvolve(L) = 10 * L; CopyValue = 5000 (constant); copied into EvolutionDamageValues/EvolutionArmorValues rows (rarity 6, all damage types 0/1/3/4) and both shards tables (rarity 6)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `L` | evolution level 1..15 | MythicalEvolutionValues[].EvolutionLevel |

**输出**：MythicalEvolutionValues and the rarity-6 rows of the evolution/shard tables

**代码**：[Balance/CombatBalanceConfig.cs:111-113](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L111)；[Balance/CombatBalanceConfig.cs:262-265](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L262)；[Balance/CombatBalanceConfig.cs:271-274](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L271)；[Balance/MythicalEvolutionValue.cs:6-15](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/MythicalEvolutionValue.cs#L6)

**状态依据**：FillMythicalEvolutionValues, CreateMythicalEvolutionValues and GetMythicalEvolutionValue are empty/default. The closed form fits all 15 stored rows exactly and also satisfies the CopyValue rule (0.5*100000L/(10L) = 5000).

**配置参数**：`Combat Balance Config.MythicalEvolutionValues`

**使用方**：items.armor_evolved (mushroommancer_cap / mushroommancer_shield, Universal mythical armor granted via Vip Config)

**备注**：Pre-evolution mythical armor = 0 + 5000*copies (ArmorBase[6]=0, ArmorGrowth[6]=5000), with first evolution at 10 copies.

### `balance.armor_base_reset`

**Armor base / item cost reset tools**

- 状态：**编辑器工具缺失**（`editor_only_missing`）
- 类型：`editor_generator`
- 来源：子系统梳理

```text
unknown; sets ArmorBaseByRarity[r] (TrySetArmorBaseValue) and resets costs/values (ResetItemCostAndValues, ResetArmorBaseValues)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ArmorBaseByRarity` | armor base | Combat Balance Config.ArmorBaseByRarity |

**输出**：ArmorBaseByRarity and evolution tables

**代码**：[Balance/CombatBalanceConfig.cs:107-109](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L107)；[Balance/CombatBalanceConfig.cs:185-187](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L185)；[Balance/CombatBalanceConfig.cs:267-269](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L267)

**状态依据**：Empty bodies.

**配置参数**：`Combat Balance Config.ArmorBaseByRarity`

**使用方**：items.armor_pre_evolution

**备注**：ArmorBase grows ~7.5x, 6x, 11.5x, 8x per rarity step (600 -> 2.5e6), unlike the weapon bases, which are all 0.

### `balance.armor_formulas_get`

**ArmorFormulas.Get(level)**

- 状态：**空壳**（`stub`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
returns 0 (original unknown; f(level) -> double)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `level` | int level | caller (none found) |

**输出**：double

**代码**：[Balance/ArmorFormulas.cs:5-8](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/ArmorFormulas.cs#L5)

**状态依据**：The body is 'return 0.0' and there are no callers in the reconstruction.

**备注**：Possibly a legacy armor-by-level curve from before the table-driven armor. It needs native recovery to confirm.

### `items.base_growth_tables`

**Rarity base/growth tables (key levers)**

- 状态：**纯数据**（`data_only`）
- 类型：`lookup_table`
- 来源：子系统梳理

```text
Per-copy weapon growth by rarity C/U/R/E/L: Cut 12/50/140/500/2400, Pierce 8/35/100/400/1600, Smash 25/80/200/700/3500 (Divine/Mythical = Legendary). Armor base 600/4500/27000/310000/2500000 and growth 100/200/350/500/2000 (Divine/Mythical growth 5000). Rarity ratio of growth ≈ x3-4 per step, x4.8 to Legendary. Smash ≈ 2-3x Pierce per copy but with a longer cooldown (2.0 vs 1.0 s at common).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `r` | rarity | ItemConfig.Rarity |

**输出**：inputs to items.weapon_damage_pre_evolution / items.armor_pre_evolution

**代码**：[Balance/CombatBalanceConfig.cs:54-68](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L54)

**状态依据**：Hand-authored arrays; no generator exists even as a stub.

**配置参数**：`Combat Balance Config.CutDamageGrowthPerLevelByRarity`；`Combat Balance Config.PierceDamageGrowthPerLevelByRarity`；`Combat Balance Config.SmashDamageGrowthPerLevelByRarity`；`Combat Balance Config.ArmorBaseByRarity`；`Combat Balance Config.ArmorGrowthPerLevelByRarity`

**使用方**：items.weapon_damage_pre_evolution；items.armor_pre_evolution；items.stat_per_copy

**备注**：DPS per copy at common: pike 8/1.0 = 8, knife 12/1.3 = 9.2, paddle 25/2.0 = 12.5.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression`：Smash/Pierce per-copy ratio C/U/R/E/L = 3.125 / 2.29 / 2.0 / 1.75 / 2.19 (range 1.75-3.1x)  
  依据：Combat Balance Config.asset lines 312-327: Pierce 8,35,100,400,1600 and Smash 25,80,200,700,3500.

### `items.item_config_levers`

**Per-item config levers**

- 状态：**纯数据**（`data_only`）
- 类型：`lookup_table`
- 来源：子系统梳理
- 原生地址：`0x146310c (OnValidate stamps StatModifiers with Source=Item)`

```text
Per ItemConfig: AttackCooldown (DPS divisor), FlatDamage (added to weapon damage at every level), SimulationDpsWeaponMultiplier (simulation-only DPS scale: buck_epic_helmet 1.4, coolHammer 1.3, several *_div 1.35-2), DamageStatTypeFlat/Percent (which player stats scale the weapon, e.g. cut -> CutDamageFlat 8 / CutDamagePercent 9; armor -> *DefenceFlat used only for icons), StatModifiers (all empty in shipped items), AbilityDefs
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ItemConfig` | 88 assets (45 base items, 41 legacy *_div Rarity-5 items, 2 mythical armors x2 variants) | Assets/MonoBehaviour/*.asset with ItemConfig script guid 005880fd0f080d7d4797cd3b8a5eb23b |

**输出**：item-specific multipliers

**代码**：[Player/Inventory/ItemConfig.cs:11-47](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemConfig.cs#L11)；[Player/Inventory/ItemConfig.cs:51-59](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemConfig.cs#L51)

**状态依据**：Serialized config values.

**配置参数**：`ItemConfig.AttackCooldown`；`ItemConfig.FlatDamage`；`ItemConfig.SimulationDpsWeaponMultiplier`；`ItemConfig.DamageStatTypeFlat`；`ItemConfig.DamageStatTypePercent`；`ItemConfig.StatModifiers`；`ItemConfig.AbilityDefs`；`ItemConfig.Number`

**使用方**：items.weapon_dps；combat DamageCalculator；simulation SimulationSystem

**备注**：Equipping adds item StatModifiers to unit stats (PlayerModel.cs TryEquipItemToSlot / ApplyEquippedItemsToUnit, native 0x145fa20/0x145e394). They are empty in all shipped items, so item power is entirely Damage/Armor plus abilities.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `variables[ItemConfig].source / summary counts`：88 assets = 45 base items (15 weapons + 30 helmets/shields) + 39 legacy *_div Rarity-5 items (15 weapons + 12 helmets + 12 shields) + 4 mythical armor files (mushroommancer_cap and mushroommancer_shield, each in 2 variants: *_0.asset and plain)  
  依据：I grepped every MonoBehaviour/*.asset that uses ItemConfig guid 005880fd... and found 88 files. Of these, 39 have Rarity 5: div cut x5, div piercing x5, div smash x5, helm * div x12, shield * div x12. The '41' in open_questions is wrong for the same reason.
- [待人工合入] `expression (SimulationDpsWeaponMultiplier examples)`：SimulationDpsWeaponMultiplier is read only for weapons: coolHammer 1.3; katana_div 1.35, fork_div 1.4, mailbox_div 1.4, coolHammer_div 1.6, pick_div 2, chainsawpike_div 2. The non-1 values on armor assets are dead data: buck_epic_helmet (prismcore_drillmask) 1.4 and helm buck epic div 1.6.  
  依据：[Simulation/Systems/SimulationSystem.cs:841-848](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L841) (equippedItems.Where(!IsArmor)) and :915-929 (playerModel.Items.Where(!IsArmor)) are the only reads of SimulationDpsWeaponMultiplier.

### `items.weapon_set_synergy_counts`

**Equipped weapon-type synergy counts**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：核验时补充
- 原生地址：`0x14589f0 (+selectors 0x1461414/0x1461434), 0x1458cd8 (+selector 0x146147c), WeaponSetCondition 0x15f5130`

```text
SameTypeMax = max over damage types t of count(equipped weapons with DamageType == t) (0 if none); DifferentTypes = |distinct DamageType among equipped weapons|; WeaponSetCondition passes iff (Mode==DifferentWeaponTypes ? DifferentTypes : SameTypeMax) >= RequiredCount
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `EquippedWeapons` | items in equipped slots with SlotType == Weapon | PlayerModel.EquippedWeapons (ItemSlots) |
| `RequiredCount` | synergy threshold | AbilityDef condition WeaponSetCondition.RequiredCount / Mode |

**输出**：PlayerModel.EquippedSameWeaponTypeMaxCount, PlayerModel.EquippedDifferentWeaponTypesCount -> ability condition bool

**代码**：[Player/Models/PlayerModel.cs:177-185](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L177)；[Player/Models/PlayerModel.cs:189-197](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L189)；[Abilities/Conditions/WeaponSetCondition.cs:13-18](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/WeaponSetCondition.cs#L13)

**状态依据**：Inline 'Native ELF VA' comments on both atoms and on WeaponSetCondition.Evaluate.

**配置参数**：`ItemConfig.DamageType`；`ItemConfig.AbilityDefs[].Conditions (WeaponSetCondition.Mode/RequiredCount)`

**使用方**：abilities: WeaponSetCondition gates synergy abilities

**备注**：Slot-system rule. How weapon types are mixed in the slots decides whether synergy abilities activate, which adds a power axis beyond Damage/Armor.

### `items.unlocked_slot_caps`

**Unlocked slot counts / equipped armor set**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：核验时补充
- 原生地址：`EquipItem 0x145e26c (predicate 0x146194c); the count atoms have none`

```text
UnlockedWeaponSlots = count(slots: IsUnlocked and SlotType==Weapon); UnlockedArmorSlots = count(slots: IsUnlocked and SlotType in {Helmet, Shield}); EquippedArmors = equipped items with SlotType in {Helmet, Shield}; auto-equip of a new item goes into the first slot that is unlocked, empty and has SlotType == item.SlotType
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ItemSlots` | player slot models (weapon_1..4, weapon_5_vip, helmet_1, shield_1) | PlayerModel.ItemSlots / ItemSlotModel.IsUnlocked, UnlockedByDefault, IsVip |

**输出**：int caps used by simulation; list of armors summed in combat

**代码**：[Player/Models/PlayerModel.cs:163-170](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L163)；[Player/Models/PlayerModel.cs:213-231](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L213)；[Player/Models/PlayerModel.cs:753-758](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L753)

**状态依据**：UnlockedWeaponSlots/UnlockedArmorSlots/EquippedArmors carry no native comment. EquipItem is native-annotated.

**配置参数**：`ItemSlotModel.UnlockedByDefault`；`ItemSlotModel.IsVip`

**使用方**：simulation: SimulationSystem.cs:921 (top-N weapons by DPS), :949 (top-N armors of the matching type)；combat: DamageCalculator.cs:83 (sum Armor over EquippedArmors with DamageType==Universal or ==hit type)

**备注**：The number of items contributing Damage/Armor is bounded by these counts. Simulation picks the best-N items, while runtime uses whatever is equipped.

### `items.equip_stat_modifiers`

**Item stat modifiers applied on equip**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：核验时补充
- 原生地址：`0x145fa20 (TryEquipItemToSlot), 0x145e394 (ApplyEquippedItemsToUnit), 0x146310c (OnValidate)`

```text
on equip: for each m in item.Config.StatModifiers: Unit.Stats[m.StatType].AddModifier(m); on rebuild: remove all modifiers with Source==Item from every stat, then re-add the modifiers of every equipped slot's item
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `StatModifiers` | authored item modifiers (Source stamped as Item by OnValidate) | ItemConfig.StatModifiers |

**输出**：Unit stat modifiers (aggregated by the stats subsystem)

**代码**：[Player/Models/PlayerModel.cs:782-800](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L782)；[Player/Models/PlayerModel.cs:808-826](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L808)；[Player/Inventory/ItemConfig.cs:51-59](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemConfig.cs#L51)

**状态依据**：Native ELF VA comments on both methods.

**配置参数**：`ItemConfig.StatModifiers`

**使用方**：stats aggregation (Unit/Stat)

**备注**：StatModifiers is [] in all 88 shipped ItemConfigs, so this path is currently inert. It is still a design lever: adding a modifier to an item gives it a stat bonus beyond Damage/Armor.

### `items.auto_equip_and_starting_weapon`

**Starting weapon and auto-equip policy**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：完整性审查补充
- 原生地址：`0x145dbbc, 0x145de6c, 0x145e26c, 0x145fa20`

```text
On first unit init with no items: grant and equip PlayerConfig.StartingWeapon. AddItem(item): if an item with the same Config.Key exists, existing.Copies += DropMultiplier * item.Copies and nothing is equipped. Otherwise the item is appended and EquipItem picks the first slot with !IsEquipped && IsUnlocked && SlotType == item.SlotType. A new item is equipped only if a free unlocked slot exists. It never replaces an equipped item. Equip triggers WearingTime = 0, adds Config.StatModifiers and recalculates rage access.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `StartingWeapon` | initial weapon | Player Config.StartingWeapon |
| `Slots` | slot list with UnlockedByDefault/IsVip | Player Config.Slots |

**输出**：Which items contribute to live DPS/armor

**代码**：[Player/Models/PlayerModel.cs:686-703](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L686)；[Player/Models/PlayerModel.cs:705-736](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L705)；[Player/Models/PlayerModel.cs:753-758](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L753)；[Player/Models/PlayerModel.cs:782-800](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L782)

**状态依据**：Methods annotated with native ELF VAs.

**配置参数**：`Player Config.StartingWeapon`；`Player Config.Slots[].UnlockedByDefault`；`Player Config.Slots[].SlotType`

**使用方**：items.chain_weapon_rules；simulation.dps_data；items.equip_stat_modifiers

**备注**：The live game depends on player loadout choices. The simulation (CalculateDpsData) instead picks the top UnlockedWeaponSlots weapons by (type match, DPS) and divides off-type DPS by 10. The model should treat the loadout as optimal per the simulation rule, not this auto-equip rule.

### `items.weapon_attack_timing_stub`

**WeaponAttackTiming helper (windup/travel/hit ratios)**

- 状态：**空壳**（`stub`）
- 类型：`runtime_formula`
- 来源：完整性审查补充

```text
Constants: windup = travel = return = 1/3, hitElapsedRatio = 2/3, hitCooldownRatio = 0.3333333. All Get*Duration and GetHitCooldownThreshold(attackCooldown, animationDurationCap, ...) methods return 0. The intended form is presumably hitElapsed = min(attackCooldown, cap) * normalized(windup + travel * progress).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `attackCooldown` | weapon cooldown | ItemConfig.AttackCooldown |
| `animationDurationCap` | visual duration cap | WeaponAttackAnimationSettings (prefab) |

**输出**：Visual or hit timing (unreferenced)

**代码**：[Player/WeaponAttackTiming.cs:5-106](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/WeaponAttackTiming.cs#L5)

**状态依据**：Every method returns 0f, and grep finds no callers of WeaponAttackTiming.*.

**配置参数**：`ItemConfig.AttackCooldown`

**备注**：The live hit threshold is the native PlayerAttackSystem.GetHitCooldownThreshold = castPoint * AttackCooldown (0x1440f74), so this stub does not change gameplay numbers. It is listed so nobody wires it in by mistake.

## 配置参数

| 资产 | 字段 | 类型 | 含义 | 用于 |
|---|---|---|---|---|
| Combat Balance Config.asset | `MinCopyValueGrowthMultiplier` | double (=1) | CopyValue never decreases across evolutions | balance.copy_value_rule |
| Combat Balance Config.asset | `CopyValueTargetShare` | double (=0.5) | share of the evolution stat jump that copy grinding supplies | balance.copy_value_rule |
| Combat Balance Config.asset | `MinEvolutionValueFromShardsMultiplier` | double (=2) | minimum E1 value vs pre-evolution stat at the shard requirement | balance.evolution_value_autobalance |
| Combat Balance Config.asset | `MinArmorEvolutionValueFromPreviousEvolutionMultiplier` | double (=1.3) | minimum armor value growth between evolutions | balance.evolution_value_autobalance |
| Combat Balance Config.asset | `MaxEvolutionLevel` | int (=15) | clamp for evolution table lookups | items.evolution_level_clamp, items.weapon_damage_evolved, items.armor_evolved, items.evolution_cost |
| Combat Balance Config.asset | `IslandTargetDays` | float[99] | timing when island i is complete (master pacing curve) | balance.island_timing_to_days, balance.island_target_days_curve |
| Combat Balance Config.asset | `Common/Uncommon/Rare/Epic/LegendaryEvolutionTargetTimings` | float[15] x5 | weapon evolution k target timing (baked from the island rows) | balance.evolution_target_timings_from_islands, balance.evolution_shards_capture |
| Combat Balance Config.asset | `Common/Uncommon/Rare/Epic/LegendaryArmorEvolutionTargetTimings` | float[15] x5 | armor evolution k target timing | balance.evolution_target_timings_from_islands, balance.evolution_shards_capture |
| Combat Balance Config.asset | `Cut/Pierce/SmashDamageBaseByRarity` | float[7] | weapon base damage by rarity (all 0) | items.weapon_damage_pre_evolution |
| Combat Balance Config.asset | `Cut/Pierce/SmashDamageGrowthPerLevelByRarity` | float[7] | weapon damage per copy before evolution | items.weapon_damage_pre_evolution, items.stat_per_copy, items.base_growth_tables |
| Combat Balance Config.asset | `ArmorBaseByRarity` | float[7] | armor base before evolution | items.armor_pre_evolution |
| Combat Balance Config.asset | `ArmorGrowthPerLevelByRarity` | float[7] | armor per copy before evolution | items.armor_pre_evolution, items.stat_per_copy |
| Combat Balance Config.asset | `EvolutionDamageValues[] {DamageType, Rarity, EvolutionLevel, Value, CopyValue, ReferenceShardsRequirement, ReferenceStatPercent, ReferenceTimeChunkIndex, ReferenceStatValue, ReferenceHeuristicValue}` | List<EvolutionCombatValue> (360 rows = types 0,1,3,4 x rarities 0-4,6 x L1-15) | evolved weapon damage base and per-copy value; Reference* fields are editor provenance only | items.weapon_damage_evolved, items.stat_per_copy, balance.evolution_value_autobalance, balance.copy_value_rule |
| Combat Balance Config.asset | `EvolutionArmorValues[]` | List<EvolutionCombatValue> (360 rows) | evolved armor base and per-copy value (identical across types 0/1/3) | items.armor_evolved, items.stat_per_copy |
| Combat Balance Config.asset | `EvolutionDamageShardsRequirements[] {Rarity, EvolutionLevel, Value}` | List<EvolutionShardsRequirement> (90 rows) | weapon copies consumed by evolution L | items.evolution_cost, items.evolve_transition |
| Combat Balance Config.asset | `EvolutionArmorShardsRequirements[]` | List<EvolutionShardsRequirement> (90 rows) | armor copies consumed by evolution L | items.evolution_cost, items.evolve_transition |
| Combat Balance Config.asset | `MythicalEvolutionValues[] {EvolutionLevel, CopiesToEvolve, CopyValue, Value}` | List<MythicalEvolutionValue> (15) | source rows for the mythical (rarity 6) evolution entries | balance.mythical_evolution_curve |
| Combat Balance Config.asset | `SyncShieldValuesByLeader` | bool (=1) | editor: copy armor evolution values across damage types | balance.evolution_value_autobalance |
| Combat Balance Config.asset | `Interpolations.Interpolations[] {Use, Name, Type, IslandPoints, TimingPoints, CoefPoints}` | List<BalanceInterpolation> (19 curves) | time-based balance multipliers/additives | balance.interpolation_evaluate, balance.total_coefficient, balance.interpolation_points_from_islands |
| Combat Balance Config.asset | `EvolutionIslandTimingRows[] {Rarity, Name, E1..E15}` | List<EvolutionIslandTimingRow> (10) | island number at which evolution k is targeted, per rarity and weapon/armor | balance.evolution_island_fill_rule, balance.evolution_target_timings_from_islands |
| Combat Balance Config.asset | `EvolutionIslandFillRuleRows[] {Rarity, Name, StartValue, StepPerEvolution}` | List<EvolutionIslandFillRuleRow> (10) | linear generator for the island timing rows | balance.evolution_island_fill_rule |
| CombatBalanceConfig (code const) | `InitialShardsRequirement` | const int 1000000000 | sentinel 'unreachable' shard requirement used by the editor capture | balance.evolution_shards_capture |
| CombatBalanceConfig (code const) | `EvolutionTimingLevelsCount` | const int 15 | number of evolution timing columns | balance.evolution_island_fill_rule |
| ItemConfig (*.asset, e.g. cut common.asset) | `Key` | string | identity used for copy stacking | items.copy_merge |
| ItemConfig | `Rarity` | Rarity | table row index | items.weapon_damage_pre_evolution, items.weapon_damage_evolved, items.armor_pre_evolution, items.armor_evolved, items.evolution_cost |
| ItemConfig | `SlotType` | ItemSlotType | Weapon vs Helmet/Shield; selects the armor or damage tables | items.evolution_cost, items.copy_merge |
| ItemConfig | `DamageType` | WeaponType | selects the Cut/Pierce/Smash arrays and evolution rows; for armor, which hit type it defends | items.weapon_damage_pre_evolution, items.weapon_damage_evolved, items.armor_evolved |
| ItemConfig | `FlatDamage` | float | flat damage added to the weapon at every level | items.weapon_damage_pre_evolution, items.weapon_damage_evolved |
| ItemConfig | `AttackCooldown` | float | seconds per attack | items.weapon_dps, items.hit_frame |
| ItemConfig | `SimulationDpsWeaponMultiplier` | float | simulation-only DPS multiplier | items.item_config_levers |
| ItemConfig | `DamageStatTypeFlat / DamageStatTypePercent` | StatType | player stats that scale this weapon in DamageCalculator | items.item_config_levers |
| ItemConfig | `AbilityDefs[].UnlockLevel / LevelScaling` | AbilityDef | ability unlock by evolution and per-evolution value overrides | items.ability_unlock_gate |
| ItemConfig | `StatModifiers` | List<StatModifier> | stat modifiers applied on equip (empty in shipped data) | items.item_config_levers |

## 枚举

| 枚举 | 文件 | 取值 |
|---|---|---|
| `Rarity` | [Player/Inventory/Rarity.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/Rarity.cs) | 0=Common,1=Uncommon,2=Rare,3=Epic,4=Legendary,5=Divine,6=Mythical |
| `ItemSlotType` | [Player/Inventory/ItemSlotType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemSlotType.cs) | 0=Weapon,1=Helmet,2=Shield |
| `ItemType` | [Player/Inventory/ItemType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemType.cs) | 0=Weapon,1=Armor |
| `WeaponType` | [Enemy/WeaponType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/WeaponType.cs) | 0=Stabbing(Pierce arrays),1=Cutting(Cut arrays),2=Magic(Pierce arrays; no evolution rows),3=Buckshot(Smash arrays),4=Universal |
| `BalanceInterpolationType` | [Balance/BalanceInterpolationType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/BalanceInterpolationType.cs) | 0=ItemEmission(+),1=ItemChainProbability(x),2=StatsFromFeaturesFlat(+),3=StatModifier(x),4=PlayerAbilityDPSMult(+),5=PlayerAblityEHPMult(+),6=ItemLegendaryEmission(+),7=ItemEmissionByType(+) |
| `StatType (item-relevant subset)` | [Unit/StatType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatType.cs) | 3=Damage,6=StabDamageFlat,7=StabDamagePercent,8=CutDamageFlat,9=CutDamagePercent,12=BuckshotDamageFlat,13=BuckshotDamagePercent,14=StabDefenceFlat,15=StabDefencePercent,16=CutDefenceFlat,17=CutDefencePercent,20=BuckshotDefenceFlat,21=BuckshotDefencePercent,26=UniversalDefenceFlat,27=UniversalDefencePercent |
| `RarityStatParam` | [Player/Inventory/RarityStatParam.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/RarityStatParam.cs) | param = rarity+1 (Common=1 ... Mythical=7) |

## 未解决问题

1. 41 legacy '*_div' ItemConfigs with Rarity=5 (Divine) are listed in Items Provider.asset, but no evolution or shard rows exist for rarity 5. If one is ever instanced, LevelsToEvolve/CanEvolve (First()) throws. Are they unreachable, or did the original lookups use FirstOrDefault or map Divine to another row?
2. CanEvolve and EvolveItemCommandRunner have no MaxEvolutionLevel cap. Past E15, Level keeps increasing and consumes Shards[15] copies with no stat gain. Did the original block evolution at 15 (in UI or in native code not yet recovered)?
3. CalcDamage/CalcArmor/CalcEvolution*/GetEvolution*/GetEvolutionShardsRequirement/DPS/StatPerCopy have no native-VA annotations. Their float32 arithmetic and the exact lookup failure behavior should be confirmed against the native ItemModel/CombatBalanceConfig methods.
4. ArmorFormulas.Get(int level) is a stub with no callers. What did it compute (legacy armor curve, or armor-to-reduction)?
5. CoreConstants.Balance/Strings/Placements are empty classes. Balance constants may have been lost (inlined constants would not survive as fields).
6. Timing axis units: IslandTargetDays and the target timings are called 'days' but reach 8311. SimulationSystem.GetBalanceTimingMinutes uses floor(CurrentTotalPlaytime / SpawnerChargesChunkMinutes=1440) or CurrentPlaytime/60. Confirm whether one unit is a real day, a 1440-minute chunk of total playtime, or a simulation time chunk (ReferenceTimeChunkIndex aligns with the target timings).
7. The simulation defense calc (SimulationSystem.cs:943) only counts armor with DamageType == weaponType. Runtime DamageCalculator.cs:83 also counts Universal armor. Is this divergence original? It matters for Mythical (Universal) armor.
8. Universal (4) evolution rows for non-mythical rarities hold placeholders (Value=1, CopyValue=0). No non-mythical Universal items exist today, but adding one would silently give it near-zero evolved stats.
9. The exact rounding ('nice number') rule, and the role of MinEvolutionValueFromShardsMultiplier and MinArmorEvolutionValueFromPreviousEvolutionMultiplier in ApplyEvolutionAutoBalance, are unknown. Shipped armor rows violate the 1.3 step minimum (min ratio 1.23), so values were hand-edited after generation.
10. How TryCaptureShardsRequirement maps a target timing to a captured copy count (which simulated item, which rounding, and why ReferenceTimeChunkIndex 31 vs a target timing of 28 for pike E2) is not recoverable from code.
11. Interpolation types ItemEmission, ItemChainProbability, StatsFromFeaturesFlat, ItemLegendaryEmission and ItemEmissionByType have no runtime consumer in the reconstruction (SimulationSystem.GetSimulationInterpolationCoefficient and UpdateInterpolationStatModifier are stubs). Their original consumers, probably the simulation's item emission and stat injection, are missing.
