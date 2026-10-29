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
| [`balance.armor_formulas_get`](#balancearmor_formulas_get) | ArmorFormulas.Get(level) (dead helper) | 空壳 | `runtime_formula` |
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
| [`items.ability_unlock_gate`](#itemsability_unlock_gate) | Item ability unlock by evolution and level | 重建（无原生标注） | `config_rule` |
| [`items.chain_weapon_rules`](#itemschain_weapon_rules) | Equipped weapon count / chain-weapon flag | 原生证据恢复 | `runtime_formula` |
| [`items.hit_frame`](#itemshit_frame) | Hit frame timing | 原生证据恢复 | `runtime_formula` |
| [`items.display_rarity`](#itemsdisplay_rarity) | Divine display rarity | 重建（无原生标注） | `config_rule` |
| [`balance.interpolation_evaluate`](#balanceinterpolation_evaluate) | BalanceInterpolation piecewise-linear curve (Evaluate) | 原生证据恢复 | `runtime_formula` |
| [`balance.total_coefficient`](#balancetotal_coefficient) | Combined interpolation coefficient by type (GetTotalCoefficient / GetInterpolationCoefficient) | 原生证据恢复 | `runtime_formula` |
| [`balance.island_timing_to_days`](#balanceisland_timing_to_days) | Island timing to days conversion (ConvertIslandTimingToDays) | 原生证据恢复 | `runtime_formula` |
| [`balance.island_target_days_curve`](#balanceisland_target_days_curve) | Island target timing curve | 纯数据 | `lookup_table` |
| [`balance.evolution_island_fill_rule`](#balanceevolution_island_fill_rule) | Evolution island schedule fill rule | 编辑器工具缺失 | `editor_generator` |
| [`balance.evolution_target_timings_from_islands`](#balanceevolution_target_timings_from_islands) | Evolution target timings from island schedule | 编辑器工具缺失 | `editor_generator` |
| [`balance.interpolation_points_from_islands`](#balanceinterpolation_points_from_islands) | Interpolation timing points from island points (ApplyIslandTimings) | 编辑器工具缺失 | `editor_generator` |
| [`balance.evolution_shards_capture`](#balanceevolution_shards_capture) | Evolution shard requirement capture | 编辑器工具缺失 | `simulation` |
| [`balance.evolution_value_autobalance`](#balanceevolution_value_autobalance) | Evolution stat value auto-balance | 编辑器工具缺失 | `editor_generator` |
| [`balance.copy_value_rule`](#balancecopy_value_rule) | Per-copy value after evolution | 编辑器工具缺失 | `editor_generator` |
| [`balance.mythical_evolution_curve`](#balancemythical_evolution_curve) | Mythical evolution curve | 编辑器工具缺失 | `editor_generator` |
| [`balance.armor_base_reset`](#balancearmor_base_reset) | Armor base / item cost reset tools | 编辑器工具缺失 | `editor_generator` |
| [`items.base_growth_tables`](#itemsbase_growth_tables) | Rarity base/growth tables (key levers) | 纯数据 | `lookup_table` |
| [`items.item_config_levers`](#itemsitem_config_levers) | Per-item config levers | 纯数据 | `lookup_table` |
| [`items.weapon_set_synergy_counts`](#itemsweapon_set_synergy_counts) | Equipped weapon-type synergy counts | 原生证据恢复 | `runtime_formula` |
| [`items.unlocked_slot_caps`](#itemsunlocked_slot_caps) | Unlocked slot counts / equipped armor set | 重建（无原生标注） | `runtime_formula` |
| [`items.equip_stat_modifiers`](#itemsequip_stat_modifiers) | Item stat modifiers applied on equip | 原生证据恢复 | `runtime_formula` |
| [`items.auto_equip_and_starting_weapon`](#itemsauto_equip_and_starting_weapon) | Starting weapon and auto-equip policy | 原生证据恢复 | `runtime_formula` |
| [`items.weapon_attack_timing_stub`](#itemsweapon_attack_timing_stub) | WeaponAttackTiming helper (windup/travel/hit ratios) | 空壳 | `runtime_formula` |

## 公式

### `balance.armor_formulas_get`

**ArmorFormulas.Get(level) (dead helper)**

- 状态：**空壳**（`stub`）
- 类型：`runtime_formula`
- 合并自：`balance.armor_formulas_get`、`combat.armor_formulas_stub`

```text
Get(level) = 0.0 (body empty in the reconstruction; original unknown)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `level` | int level | caller (none exist) |

**输出**：double (unused)

**代码**：[Balance/ArmorFormulas.cs:5-8](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/ArmorFormulas.cs#L5)

**状态依据**：The body is 'return 0.0' with no native annotation. grep 'ArmorFormulas' matches only its own declaration (Balance/ArmorFormulas.cs:3), so there are no callers. It is labelled stub only because the body is empty; it is dead code with zero model impact, not a missing runtime formula.

**备注**：Treat this as dead or legacy code. Every live armor value comes from ItemModel.CalcArmor/CalcEvolutionArmor (Player/Inventory/ItemModel.cs:491-499, 511-518), using CombatBalanceConfig.ArmorBaseByRarity / ArmorGrowthPerLevelByRarity and EvolutionArmorValues (Balance/CombatBalanceConfig.cs:180-203); see items.armor_pre_evolution. It may be a legacy armor-by-level curve from before the table-driven armor; only native recovery could confirm that.

**依赖图节点**：输入 ；输出 `value:armor.legacy_formula_value`

**核验记录**（已写入上方字段）：

- 采纳 `used_by / notes`（来自 `combat.armor_formulas_stub`）：Verified by grep that there are no callers. The two entries are merged into balance.armor_formulas_get, used_by is empty, and notes and status_evidence state dead code with zero model impact and point to ItemModel.CalcArmor/CalcEvolutionArmor as the live source. Status stays 'stub' (the empty body is the only defined category that fits), qualified as dead code in status_evidence.

### `items.weapon_damage_pre_evolution`

**Weapon damage before first evolution**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：合并核验

```text
Damage = CalcDamage(EffectiveLevel, Copies); when !Evolved: EffectiveLevel = Copies and dmg = (double)( (DamageBase[t][r] + DamageGrowth[t][r] * (float)Copies) + FlatDamage ) evaluated in float32. Arrays: t == Buckshot -> Smash*, t == Cutting -> Cut*, else (Stabbing/Magic/Universal) -> Pierce*. In the shipped data DamageBase = 0, so dmg = Growth[t][r]*Copies + FlatDamage, e.g. knife (cut common) = 12*Copies.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `t` | weapon damage type | ItemConfig.DamageType |
| `r` | base rarity index (Config.Rarity, not the display Divine rarity) | ItemConfig.Rarity |
| `DamageBase[t][r]` | base damage by rarity | Combat Balance Config.CutDamageBaseByRarity / PierceDamageBaseByRarity / SmashDamageBaseByRarity (all 0) |
| `DamageGrowth[t][r]` | damage added per copy | Combat Balance Config.Cut/Pierce/SmashDamageGrowthPerLevelByRarity (cut 12,50,140,500,2400,2400,2400; pierce 8,35,100,400,1600x3; smash 25,80,200,700,3500x3) |
| `EffectiveLevel` | Copies while not evolved (Level once evolved) | ItemModel.EffectiveLevel (ItemModel.cs:135-143), items.effective_level |
| `FlatDamage` | per-item flat bonus (mace 400, chainsawpike 5000, coolHammer 15000, chainsaw 30000, all others 0) | ItemConfig.FlatDamage |

**输出**：ItemModel.Damage (double) for non-evolved weapons

**代码**：[Player/Inventory/ItemModel.cs:408-416](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L408)；[Player/Inventory/ItemModel.cs:475-484](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L475)；[Player/Inventory/ItemModel.cs:135-143](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L135)；[Balance/CombatBalanceConfig.cs:156-166](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L156)

**状态依据**：Implemented but with no native-VA comment on Damage, CalcDamage or GetDamageBase/GetDamageGrowthPerLevel. The float32 evaluation order looks deliberately ported, but there is no recovered native address.

**配置参数**：`Combat Balance Config.CutDamageBaseByRarity`；`Combat Balance Config.PierceDamageBaseByRarity`；`Combat Balance Config.SmashDamageBaseByRarity`；`Combat Balance Config.CutDamageGrowthPerLevelByRarity`；`Combat Balance Config.PierceDamageGrowthPerLevelByRarity`；`Combat Balance Config.SmashDamageGrowthPerLevelByRarity`；`ItemConfig.FlatDamage`；`ItemConfig.DamageType`；`ItemConfig.Rarity`

**使用方**：combat: [Services/Damage/DamageCalculator.cs:128](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L128),140 (outgoing dmg = percentStat*(DamageStat + weapon.Damage + flatStat + abilityFlat)*(1-resist)*...)；simulation: [Simulation/Systems/SimulationSystem.cs:923-929](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L923) (dps = percent*(flat+item.Damage)*StatModifierCoef/AttackCooldown; dps /= 10 when item.DamageType != evaluated weaponType; then * SimulationDpsWeaponMultiplier)；simulation: [Simulation/Systems/SimulationSystem.cs:841-849](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L841) (non-fast spawner DPS: sum of DamageCalculator.Calculate(hit, fromSimulation=true).Damage * SimulationDpsWeaponMultiplier / AttackCooldown, times ability DPS multiplier)；abilities: [Player/Systems/RageExtraHitSystem.cs:48-50](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/RageExtraHitSystem.cs#L48) (picks the highest-Damage weapon)；ItemModel.DPS = Damage / AttackCooldown (ItemModel.cs:398-406)；UI previews (InventoryStatView, ChestOpenView, WeaponRewardView, ItemMiniView)

**备注**：CalcDamage(level, copies) ignores the copies argument when not evolved, because the level argument already equals the copy count. The base arrays have 7 entries indexed by Rarity (0..6). Index 5 (Divine) is only reached by the legacy *_div ItemConfigs, and index 6 only by Mythical items (no mythical weapons exist). Magic (2) uses the Pierce arrays. There is no clamp on Copies: evolution is manual, so pre-evolution damage keeps growing linearly with copies until the player evolves.

**依赖图节点**：输入 `value:item.effective_level`、`value:item.evolution_level`、`config:CombatBalanceConfig.CutDamageBaseByRarity`、`config:CombatBalanceConfig.PierceDamageBaseByRarity`、`config:CombatBalanceConfig.SmashDamageBaseByRarity`、`config:CombatBalanceConfig.CutDamageGrowthPerLevelByRarity`、`config:CombatBalanceConfig.PierceDamageGrowthPerLevelByRarity`、`config:CombatBalanceConfig.SmashDamageGrowthPerLevelByRarity`、`config:ItemConfig.FlatDamage`、`config:ItemConfig.DamageType`、`config:ItemConfig.Rarity`；输出 `value:weapon.item_damage`

**核验记录**（已写入上方字段）：

- 采纳 `used_by`（来自 `items.weapon_damage_pre_evolution`）：Verified SimulationSystem.cs:928 'if (item.Config.DamageType != weaponType) dps /= 10.0;' and the second consumer at :847-848 (DamageCalculator.Calculate(hit, true, 1.0).Damage * SimulationDpsWeaponMultiplier / AttackCooldown). Both are now in used_by.

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

**依赖图节点**：输入 `value:item.evolution_row_index`、`value:item.copies`、`value:item.evolution_level`、`config:CombatBalanceConfig.EvolutionDamageValues[].Value`、`config:CombatBalanceConfig.EvolutionDamageValues[].CopyValue`、`config:CombatBalanceConfig.EvolutionDamageValues[].DamageType`、`config:CombatBalanceConfig.EvolutionDamageValues[].Rarity`、`config:CombatBalanceConfig.EvolutionDamageValues[].EvolutionLevel`、`config:ItemConfig.FlatDamage`、`config:ItemConfig.DamageType`、`config:ItemConfig.Rarity`；输出 `value:weapon.item_damage`

### `items.armor_pre_evolution`

**Armor value before first evolution**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：合并核验

```text
Armor = CalcArmor(EffectiveLevel, Copies); when !Evolved: armor = (double)(ArmorBase[r] + ArmorGrowth[r] * (float)Copies) in float32. No flat term; damageType is passed but ignored by GetArmorBase/GetArmorGrowthPerLevel; no clamp on Copies.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `r` | base rarity index | ItemConfig.Rarity |
| `ArmorBase[r]` | armor base by rarity | Combat Balance Config.ArmorBaseByRarity = [600, 4500, 27000, 310000, 2500000, 0, 0] |
| `ArmorGrowth[r]` | armor per copy | Combat Balance Config.ArmorGrowthPerLevelByRarity = [100, 200, 350, 500, 2000, 5000, 5000] |
| `EffectiveLevel` | Copies while not evolved | ItemModel.EffectiveLevel (ItemModel.cs:135-143), items.effective_level |

**输出**：ItemModel.Armor (double) for non-evolved helmets/shields

**代码**：[Player/Inventory/ItemModel.cs:418-426](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L418)；[Player/Inventory/ItemModel.cs:491-499](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L491)；[Player/Inventory/ItemModel.cs:135-143](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L135)；[Balance/CombatBalanceConfig.cs:180-192](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L180)

**状态依据**：No native annotation on Armor, CalcArmor, GetArmorBase or GetArmorGrowthPerLevel. GetArmorBase and GetArmorGrowthPerLevel take a damageType argument but ignore it.

**配置参数**：`Combat Balance Config.ArmorBaseByRarity`；`Combat Balance Config.ArmorGrowthPerLevelByRarity`；`ItemConfig.Rarity`

**使用方**：combat: [Services/Damage/DamageCalculator.cs:83-87](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L83) (sum of equipped armors with DamageType==Universal or ==hit type, added to flat defence: ApplyDamageDefense(attack, (univFlat + armorSum + typedFlat)*typedPercent))；simulation: [Simulation/Systems/SimulationSystem.cs:935-956](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L935) (defense = percent*(flat+sum armor)*StatModifierCoef)；UI

**备注**：Example: buck_rare_helmet (golden_spikes) = 27000 + 350*copies. That is 83000 at the first evolution threshold of 160 copies (Armor Shards[r2][1] = 160), but it is not a cap. Evolution is manual (only UI/Inventory/InventoryItemContextMenu.cs:126 calls TryEvolve), so pre-evolution copies and armor keep growing linearly until the player evolves. The same caveat applies to the 'jump' examples in items.evolve_transition, which assume the player evolves exactly at the threshold. A mythical armor before evolution = 0 + 5000*copies.

**依赖图节点**：输入 `value:item.effective_level`、`value:item.evolution_level`、`config:CombatBalanceConfig.ArmorBaseByRarity`、`config:CombatBalanceConfig.ArmorGrowthPerLevelByRarity`、`config:ItemConfig.Rarity`；输出 `value:armor.item_armor`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `items.armor_pre_evolution`）：Verified that CalcArmor (ItemModel.cs:491-499) has no clamp and that TryEvolve's only caller is [UI/Inventory/InventoryItemContextMenu.cs:126](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/Inventory/InventoryItemContextMenu.cs#L126). The notes now say 160 copies is the first evolution threshold, not a cap, and the expression states there is no clamp.

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

**依赖图节点**：输入 `value:item.evolution_row_index`、`value:item.copies`、`value:item.evolution_level`、`config:CombatBalanceConfig.EvolutionArmorValues[].Value`、`config:CombatBalanceConfig.EvolutionArmorValues[].CopyValue`、`config:CombatBalanceConfig.EvolutionArmorValues[].DamageType`、`config:CombatBalanceConfig.EvolutionArmorValues[].Rarity`、`config:CombatBalanceConfig.EvolutionArmorValues[].EvolutionLevel`、`config:ItemConfig.DamageType`、`config:ItemConfig.Rarity`；输出 `value:armor.item_armor`

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

**依赖图节点**：输入 `value:item.evolution_level`、`value:item.copies`；输出 `value:item.effective_level`

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

**依赖图节点**：输入 `value:item.evolution_level`、`config:CombatBalanceConfig.MaxEvolutionLevel`；输出 `value:item.evolution_row_index`

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

**依赖图节点**：输入 `value:item.evolution_level`、`value:item.copies`、`config:CombatBalanceConfig.MaxEvolutionLevel`、`config:CombatBalanceConfig.EvolutionDamageShardsRequirements[].Value`、`config:CombatBalanceConfig.EvolutionDamageShardsRequirements[].Rarity`、`config:CombatBalanceConfig.EvolutionDamageShardsRequirements[].EvolutionLevel`、`config:CombatBalanceConfig.EvolutionArmorShardsRequirements[].Value`、`config:CombatBalanceConfig.EvolutionArmorShardsRequirements[].Rarity`、`config:CombatBalanceConfig.EvolutionArmorShardsRequirements[].EvolutionLevel`、`config:ItemConfig.SlotType`、`config:ItemConfig.Rarity`；输出 `value:item.copies_to_evolve`

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

**依赖图节点**：输入 `value:item.copies`、`value:item.copies_to_evolve`、`value:item.evolution_level`、`state:player_actions`；输出 `value:item.copies`、`value:item.evolution_level`

### `items.next_level_preview`

**Next-level damage/armor preview**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：合并核验

```text
Let E = min(L+1, MaxEvolutionLevel=15) (the clamp is applied by GetExistingEvolutionLevel inside every table lookup). If Evolved: NextLevelDamage = EvoDmg[t][r][E] + EvoDmgCopy[t][r][E] * max(0, Copies - Shards[isArmor][r][E]) + FlatDamage; NextLevelArmor = EvoArm[t][r][E] + EvoArmCopy[t][r][E] * max(0, Copies - Shards[isArmor][r][E]). If !Evolved: NextLevelDamage = CalcDamage(Copies+1) = DamageBase[t][r] + DamageGrowth[t][r]*(Copies+1) + FlatDamage; NextLevelArmor = ArmorBase[r] + ArmorGrowth[r]*(Copies+1). The non-evolved branch previews +1 copy, not the first evolution. All in float32, then cast to double.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `L` | current evolution level | ItemModel.Level |
| `E` | NextEvolutionLevel = Level + (Evolved ? 1 : 0), clamped to MaxEvolutionLevel in lookups | ItemModel.cs:448, CombatBalanceConfig.GetExistingEvolutionLevel |
| `Shards[isArmor][r][E]` | LevelsToEvolve = copies required for evolution E | Combat Balance Config.EvolutionDamageShardsRequirements / EvolutionArmorShardsRequirements |
| `MaxEvolutionLevel` | evolution table cap (15) | Combat Balance Config.MaxEvolutionLevel |
| `Copies` | current copy count | ItemModel.Copies |

**输出**：ItemModel.NextLevelDamage / NextLevelArmor (double)

**代码**：[Player/Inventory/ItemModel.cs:428-450](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L428)；[Player/Inventory/ItemModel.cs:155-163](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L155)；[Player/Inventory/ItemModel.cs:165](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L165)；[Player/Inventory/ItemModel.cs:475-484](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L475)；[Player/Inventory/ItemModel.cs:491-499](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L491)；[Player/Inventory/ItemModel.cs:501-518](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L501)；[Balance/CombatBalanceConfig.cs:147-150](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L147)；[Balance/CombatBalanceConfig.cs:210-215](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L210)

**状态依据**：No native annotation on NextLevelDamage/NextLevelArmor, NextEvolutionLevel, CopiesAfterEvolution or the CalcEvolution* helpers.

**配置参数**：`Combat Balance Config.EvolutionDamageValues`；`Combat Balance Config.EvolutionArmorValues`；`Combat Balance Config.EvolutionDamageShardsRequirements`；`Combat Balance Config.EvolutionArmorShardsRequirements`；`Combat Balance Config.MaxEvolutionLevel`；`Combat Balance Config.Cut/Pierce/SmashDamageBaseByRarity`；`Combat Balance Config.Cut/Pierce/SmashDamageGrowthPerLevelByRarity`；`Combat Balance Config.ArmorBaseByRarity`；`Combat Balance Config.ArmorGrowthPerLevelByRarity`；`ItemConfig.FlatDamage`

**使用方**：[UI/Inventory/ItemMiniView.cs:98-100](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/Inventory/ItemMiniView.cs#L98)；[UI/Inventory/InventoryItemContextMenu.cs:185](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/Inventory/InventoryItemContextMenu.cs#L185),190

**备注**：Preview only; not used in combat or simulation. At max evolution (L >= 15) E clamps to 15, so the evolved preview reuses the level-15 tables with Copies minus the level-15 shard requirement, rather than showing 'no next level'.

**依赖图节点**：输入 `value:item.evolution_level`、`value:item.copies`、`value:item.effective_level`、`value:item.copies_to_evolve`、`config:CombatBalanceConfig.MaxEvolutionLevel`、`config:CombatBalanceConfig.EvolutionDamageValues[].Value`、`config:CombatBalanceConfig.EvolutionDamageValues[].CopyValue`、`config:CombatBalanceConfig.EvolutionDamageValues[].DamageType`、`config:CombatBalanceConfig.EvolutionDamageValues[].Rarity`、`config:CombatBalanceConfig.EvolutionDamageValues[].EvolutionLevel`、`config:CombatBalanceConfig.EvolutionArmorValues[].Value`、`config:CombatBalanceConfig.EvolutionArmorValues[].CopyValue`、`config:CombatBalanceConfig.EvolutionArmorValues[].DamageType`、`config:CombatBalanceConfig.EvolutionArmorValues[].Rarity`、`config:CombatBalanceConfig.EvolutionArmorValues[].EvolutionLevel`、`config:CombatBalanceConfig.CutDamageBaseByRarity`、`config:CombatBalanceConfig.PierceDamageBaseByRarity`、`config:CombatBalanceConfig.SmashDamageBaseByRarity`、`config:CombatBalanceConfig.CutDamageGrowthPerLevelByRarity`、`config:CombatBalanceConfig.PierceDamageGrowthPerLevelByRarity`、`config:CombatBalanceConfig.SmashDamageGrowthPerLevelByRarity`、`config:CombatBalanceConfig.ArmorBaseByRarity`、`config:CombatBalanceConfig.ArmorGrowthPerLevelByRarity`、`config:ItemConfig.FlatDamage`、`config:ItemConfig.DamageType`、`config:ItemConfig.Rarity`；输出 `value:item.next_level_stat_preview`

**核验记录**（已写入上方字段）：

- 采纳 `used_by`（来自 `items.next_level_preview`）：grep confirms NextLevelDamage/NextLevelArmor are read at [UI/Inventory/InventoryItemContextMenu.cs:185](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/Inventory/InventoryItemContextMenu.cs#L185) and :190 in addition to ItemMiniView.cs:98-100.
- 采纳 `expression`（来自 `items.next_level_preview`）：Verified at ItemModel.cs:434,444,448,450,165 and CombatBalanceConfig.cs:147-150,210-215: the evolved branch uses NextEvolutionLevel = L+1 clamped to MaxEvolutionLevel (15 in the asset) for both the value tables and the shard requirement, and the non-evolved branch is CalcDamage(Copies+1) = Base + Growth*(Copies+1) + FlatDamage (+1-copy preview). The expression has been rewritten to match.

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

**依赖图节点**：输入 `value:item.evolution_level`、`value:item.evolution_row_index`、`config:CombatBalanceConfig.EvolutionDamageValues[].CopyValue`、`config:CombatBalanceConfig.EvolutionDamageValues[].DamageType`、`config:CombatBalanceConfig.EvolutionDamageValues[].Rarity`、`config:CombatBalanceConfig.EvolutionDamageValues[].EvolutionLevel`、`config:CombatBalanceConfig.EvolutionArmorValues[].CopyValue`、`config:CombatBalanceConfig.EvolutionArmorValues[].DamageType`、`config:CombatBalanceConfig.EvolutionArmorValues[].Rarity`、`config:CombatBalanceConfig.EvolutionArmorValues[].EvolutionLevel`、`config:CombatBalanceConfig.CutDamageGrowthPerLevelByRarity`、`config:CombatBalanceConfig.PierceDamageGrowthPerLevelByRarity`、`config:CombatBalanceConfig.SmashDamageGrowthPerLevelByRarity`、`config:CombatBalanceConfig.ArmorGrowthPerLevelByRarity`、`config:ItemConfig.SlotType`、`config:ItemConfig.DamageType`、`config:ItemConfig.Rarity`；输出 `value:item.stat_per_copy`

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

**依赖图节点**：输入 `value:weapon.item_damage`、`config:ItemConfig.AttackCooldown`；输出 `value:weapon.item_dps`

### `items.copy_merge`

**Copy acquisition / duplicate merge**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x145de6c (AddItem), 0x145f44c (CreateItem), 0x145f4e4 (SetInitialCopies / inlined Copies setter), 0x145e26c (EquipItem, predicate 0x146194c)`

```text
incoming = spawner drop: (command.Copies >= 1 ? command.Copies : ItemDropAmountCalculator.CalculatePerKill(item)), where EnemySpawner sets Copies = (IsChargesEnabled ? min(CalculatePerKill, RemainingCharges + 1) : CalculatePerKill) and CalculatePerKill = 1 + (int)items_rarity_drop_amount[RarityStatParam.From(item.Rarity)]; GrantRewardCommandRunner: incoming = reward.Level; chest: incoming = dropCopies. Merge (PlayerModel.AddItem): existing = first Items entry with Config.Key == item.Config.Key; if existing != null: existing.Copies += cheats.DropMultiplier * incoming (no clamp); else: the new ItemModel (Level = 1, Copies = incoming, Evolved = false) is appended to Items without DropMultiplier, written to user data, and EquipItem puts it into the first slot with !IsEquipped && IsUnlocked && SlotType == item.Config.SlotType
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `incoming` | copies granted: spawner drop = command.Copies or CalculatePerKill fallback; reward = itemReward.Level; chest = dropCopies | GrantSpawnerItemRewardCommand.Copies / EnemySpawner kill drop / GrantRewardCommandRunner reward.Level / ChestOpenFlowRunner dropCopies |
| `items_rarity_drop_amount` | meta-stat flat bonus copies per kill, by rarity | MetaStatsModel 'items_rarity_drop_amount' (meta subsystem) |
| `RemainingCharges` | spawner charges cap the per-kill copies at RemainingCharges+1 when charges are enabled | EnemySpawner.RemainingCharges |
| `DropMultiplier` | cheat multiplier (1 in normal play); applied only when merging into an existing item | CheatsModel.DropMultiplier |
| `Config.Key` | duplicate identity of an item | ItemConfig.Key |

**输出**：ItemModel.Copies (new or existing item); pre-evolution EffectiveLevel = Copies

**代码**：[Player/Models/PlayerModel.cs:705-736](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L705)；[Player/Models/PlayerModel.cs:738-747](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L738)；[Player/Models/PlayerModel.cs:753-758](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L753)；[Player/Inventory/ItemModel.cs:525-534](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L525)；[Player/Inventory/ItemModel.cs:544-548](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L544)；[Player/Inventory/ItemModel.cs:202-217](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L202)；[Player/Inventory/ItemModel.cs:141](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L141)；[Enemy/Commands/GrantSpawnerItemRewardCommandHandler.cs:24-32](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerItemRewardCommandHandler.cs#L24)；[Enemy/EnemySpawner.cs:1191-1205](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1191)；[Meta/ItemDropAmountCalculator.cs:15-25](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/ItemDropAmountCalculator.cs#L15)；[Rewards/Commands/GrantRewardCommandRunner.cs:292](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/Commands/GrantRewardCommandRunner.cs#L292)；[Shop/Flows/ChestOpenFlowRunner.cs:136-152](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/Flows/ChestOpenFlowRunner.cs#L136)

**状态依据**：PlayerModel.AddItem carries 'Native ELF VA 0x145de6c', CreateItem 'Native ELF VA 0x145f44c', ItemModel.SetInitialCopies 'Native ELF VA 0x145f4e4', EquipItem 'Native ELF VA 0x145e26c'.

**配置参数**：`Meta Stats Config.items_rarity_drop_amount (meta subsystem)`；`CheatsModel.DropMultiplier`；`EnemySpawner charges (spawners subsystem)`

**使用方**：all reward/drop flows: spawner kills (GrantSpawnerItemRewardCommandHandler), rewards (GrantRewardCommandRunner), chests (ChestOpenFlowRunner.cs:136-152), events, shop；items.weapon_damage_pre_evolution / items.armor_pre_evolution (EffectiveLevel = Copies before evolution)；items.stat_per_copy / evolution (CanEvolve = Copies >= LevelsToEvolve)

**备注**：Identity is Config.Key, so copies of one item stack forever. Copies are the only progression currency for items; there is no separate shard item. Drop-amount math belongs to the meta subsystem. DropMultiplier is applied only in the merge branch, so the first drop of a new item is never multiplied. The Copies setter has no clamp. ChestOpenFlowRunner builds a separate display ItemModel (copies = dropCopies + existing.Copies, copying Evolved/Level) and grants a fresh ItemModel with dropCopies via AddItem(granted, false, applySilently=true).

**依赖图节点**：输入 `value:reward.item_copies_per_kill`、`value:reward.item_copies_granted`、`state:cheats`、`state:player_actions`、`value:player.unlocked_slot_counts`；输出 `value:item.copies`、`value:player.equipped_items`

**核验记录**（已写入上方字段）：

- 采纳 `expression / variables`（来自 `items.copy_merge`）：Verified: GrantSpawnerItemRewardCommandHandler.cs:28-29 uses command.Copies with CalculatePerKill fallback when < 1; GrantRewardCommandRunner.cs:292 passes reward.Level; PlayerModel.cs:712 multiplies only in the existing branch, while the new-item branch (718-735) saves item.Copies unmultiplied. Expression and variables rewritten accordingly.

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

**依赖图节点**：输入 `config:ItemConfig.Rarity`；输出 `value:item.rarity_param`

### `items.ability_unlock_gate`

**Item ability unlock by evolution and level**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`config_rule`
- 合并自：`items.ability_unlock_gate`、`powerups.ability_unlock_gate`
- 原生地址：`Downstream only: IsPreDivine 0x1463664, AbilityState.Stars 0x15e306c, AbilityState.GetCurrentOverride 0x15e3120`

```text
unlocked(def) = Evolved ? (def.UnlockLevel <= Level) : (def.UnlockLevel == 0). activeAbilityDefs = [def in Config.AbilityDefs (config order) where unlocked(def)]. AbilityStates are rebuilt as new AbilityState(def, item) only if the active list differs (count or element) from the current states. Rebuild triggers: constructor, Level setter, Evolved setter, and the Copies setter only while !Evolved. Downstream: Stars = IsPreDivine(!Evolved) ? 0 : Level; ability value = the LevelScaling entry with the greatest Level <= Stars, else def.Value.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `UnlockLevel` | evolution level needed (0 = available before evolution) | ItemConfig.AbilityDefs[].UnlockLevel |
| `Evolved, Level` | item evolution flag and evolution level | ItemModel / ItemDataUd |
| `LevelScaling` | per-evolution value overrides (e.g. knife ability 7,11,15,...,120 for E1..E15) | ItemConfig.AbilityDefs[].LevelScaling |

**输出**：active ability list and ability magnitudes per item

**代码**：[Player/Inventory/ItemModel.cs:520-523](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L520)；[Player/Inventory/ItemModel.cs:561-573](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L561)；[Player/Inventory/ItemModel.cs:191-198](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L191)；[Player/Inventory/ItemModel.cs:210-217](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L210)；[Player/Inventory/ItemModel.cs:229-236](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L229)；[Player/Inventory/ItemModel.cs:133](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L133)；[Abilities/AbilityState.cs:71](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L71)；[Abilities/AbilityState.cs:185-198](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L185)

**状态依据**：IsAbilityUnlocked and RebuildAbilityStates have no native annotation. The downstream IsPreDivine and AbilityState Stars / GetCurrentOverride lookups are native-annotated.

**配置参数**：`ItemConfig.AbilityDefs[].UnlockLevel`；`ItemConfig.AbilityDefs[].LevelScaling`

**使用方**：powerups.ability_tier_override；abilities subsystem (all ability effects)；DamageCalculator (ability multipliers)

**备注**：Before evolution only UnlockLevel-0 abilities exist and Stars = 0, so only LevelScaling entries with Level <= 0 apply. Evolution level is a second axis of item power, through ability tiers, on top of the stat tables. A rebuild resets per-ability state (Stacks, Cooldown, GuaranteedCrit, temp buffs), because new AbilityState objects are created.

**依赖图节点**：输入 `config:ItemConfig.AbilityDefs[].UnlockLevel`、`value:item.evolution_level`、`config:ItemConfig.AbilityDefs[].LevelScaling[].Level`、`config:ItemConfig.AbilityDefs[].LevelScaling[].ValueOverride`、`config:ItemConfig.AbilityDefs[].LevelScaling[].ChanceOverride`、`config:ItemConfig.AbilityDefs[].LevelScaling[].StacksOverride`、`config:ItemConfig.AbilityDefs[].AffectsValue`、`config:ItemConfig.AbilityDefs[].AffectsChance`、`config:ItemConfig.AbilityDefs[].AffectsStacks`、`config:ItemConfig.AbilityDefs[].Value`、`config:ItemConfig.AbilityDefs[].Chance`、`config:ItemConfig.AbilityDefs[].StackLimit`；输出 `value:item.unlocked_abilities`

### `items.chain_weapon_rules`

**Equipped weapon count / chain-weapon flag**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x1465964 (HasChainWeaponEquipped), 0x1465d48 (its predicate), 0x1465a78 (EquippedWeaponCount), 0x1465df0 (its predicate); consumer GetAttackRangeSqr 0x1440a9c`

```text
EquippedWeaponCount = count(slot in player.ItemSlots: slot.IsEquipped && slot.SlotType == Weapon); HasChainWeaponEquipped = any(slot in player.ItemSlots: slot.IsEquipped && slot.SlotType == Weapon && slot.Key in ChainWeaponSlotKeys = {weapon_2, weapon_3, weapon_4, weapon_5_vip}), where slot.Key is the SLOT key (not the item key), so the flag is true iff any weapon slot other than weapon_1 is filled
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ItemSlots` | player item slots | PlayerModel.ItemSlots |
| `slot.Key` | slot identifier (weapon_1..weapon_4, weapon_5_vip, helmet_1, shield_1) | ItemSlotKeys constants |
| `ChainWeaponSlotKeys` | {weapon_5_vip, weapon_3, weapon_2, weapon_4} | ItemSlotKeys.ChainWeaponSlotKeys |

**输出**：int EquippedWeaponCount / bool HasChainWeaponEquipped

**代码**：[Player/Inventory/ItemSlotKeys.cs:9-23](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemSlotKeys.cs#L9)；[Player/Inventory/ItemSlotKeys.cs:25-29](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemSlotKeys.cs#L25)；[Player/Inventory/ItemSlotKeys.cs:31-35](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemSlotKeys.cs#L31)；[Player/PlayerAttackSystem.cs:238-244](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L238)；[Player/WeaponView.cs:462-468](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/WeaponView.cs#L462)

**状态依据**：Inline 'Native ELF VA 0x1465964, predicate 0x1465d48' on HasChainWeaponEquipped and 'Native ELF VA 0x1465a78, predicate 0x1465df0' on EquippedWeaponCount.

**使用方**：[Player/PlayerAttackSystem.cs:242](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L242) GetAttackRangeSqr: range *= 3 vs weak enemies / event golems when HasChainWeaponEquipped (returns range^2)；[Player/WeaponView.cs:465](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/WeaponView.cs#L465) TriggerAttackAnimation: a Cut weapon uses animator AttackType 1 (Smash) instead of its configured type when EquippedWeaponCount >= 2 and HasChainWeaponEquipped

**备注**：Slot count limits how many items contribute damage and armor (simulation uses UnlockedWeaponSlots/UnlockedArmorSlots).

**依赖图节点**：输入 `value:player.equipped_items`、`constant:ItemSlotKeys.ChainWeaponSlotKeys`；输出 `value:player.equipped_weapon_count`、`value:player.has_chain_weapon`

**核验记录**（已写入上方字段）：

- 采纳 `expression`（来自 `items.chain_weapon_rules`）：ItemSlotKeys.cs:28 tests ChainWeaponSlotKeys.Contains(slot.Key) on the slot, not the item key; expression now says slot key and the 'any non-first weapon slot filled' meaning; WeaponView.cs:465 consumer (EquippedWeaponCount >= 2 && chain flag overrides Cut animation type to 1) added to used_by.

### `items.hit_frame`

**Hit frame timing**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x14630e8`

```text
HitFrameAt60Fps = (1f - AnimationCastPoint) * AttackCooldown * 60f
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `AnimationCastPoint` | normalized cast point, [Range(0,1)] | ItemConfig.AnimationCastPoint |
| `AttackCooldown` | attack period in seconds | ItemConfig.AttackCooldown |

**输出**：frame index (at 60 fps) of the hit within the attack cycle, computed from the raw, unadjusted cast point

**代码**：[Player/Inventory/ItemConfig.cs:49](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemConfig.cs#L49)；[Player/Inventory/ItemConfig.cs:25](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemConfig.cs#L25)；[Player/Inventory/ItemConfig.cs:31-32](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemConfig.cs#L31)

**状态依据**：Inline 'Native 0x14630e8' comment on the HitFrameAt60Fps property.

**配置参数**：`ItemConfig.AnimationCastPoint`；`ItemConfig.AttackCooldown`

**备注**：Mostly presentation. No consumer of HitFrameAt60Fps exists in the reconstruction: a grep matches only its definition. It is likely an editor/inspector helper, or its consumer was not recovered. The runtime hit timing is computed separately and is catalogued in combat.animation_cast_point and combat.player_weapon_attack_cycle. It uses the speed-adjusted PlayerAttackSystem.GetAnimationCastPoint (native 0x1440fa0, PlayerAttackSystem.cs:206-211): castPoint' = 1 - (1 - AnimationCastPoint)/(factor*animationCastPointSpeedCoefficient + 1), with factor 0.5 for Cutting, 1 for Buckshot and 0 otherwise. The consumers are WeaponView.cs:545 hitTime = (1 - castPoint') * attackCooldown and PlayerAttackSystem.cs:146/228. So HitFrameAt60Fps matches the runtime hit only where factor = 0 (Stabbing/Magic/Universal), and it overstates the hit delay for Cutting and Buckshot weapons.

**依赖图节点**：输入 `config:ItemConfig.AnimationCastPoint`、`config:ItemConfig.AttackCooldown`、`constant:ItemConfig.frames_per_second`；输出 `value:weapon.hit_frame`

**核验记录**（已写入上方字段）：

- 采纳 `used_by`（来自 `items.hit_frame`）：A grep for HitFrameAt60Fps across CodeBase matches only ItemConfig.cs:49, so used_by is set to [].
- 合并复核修正：The notes say the property is listed because it 'times the hit inside the cooldown', and that its consumer is unknown. The runtime hit timing does not use this property or the raw AnimationCastPoint. It uses PlayerAttackSystem.GetAnimationCastPoint (native 0x1440fa0), which is speed-adjusted per DamageType: castPoint' = 1 - (1 - AnimationCastPoint)/(factor*coef + 1), with factor 0.5 for Cutting, 1 for Buckshot and 0 otherwise. Its consumers are WeaponView.cs:545 hitTime = (1 - castPoint') * attackCooldown and PlayerAttackSystem.cs:146/228. HitFrameAt60Fps therefore matches the runtime hit only for Stabbing/Magic/Universal weapons. That caveat and the cross-reference to combat.animation_cast_point are missing. The pending used_by correction is applied correctly.

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

**依赖图节点**：输入 `config:ItemConfig.Rarity`、`value:item.evolution_level`；输出 `value:item.display_rarity`

### `balance.interpolation_evaluate`

**BalanceInterpolation piecewise-linear curve (Evaluate)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 合并自：`balance.interpolation_evaluate`、`simulation.interp_evaluate`
- 原生地址：`0x1579ed4 (Evaluate), 0x157a65c (ParsePoints)`

```text
T = ParsePoints(TimingPoints), C = ParsePoints(CoefPoints) (split ';', Trim, skip empty, float.Parse InvariantCulture; null/whitespace string -> empty list). If |T| < 2 or |C| < 2 or |T| != |C|: LogError and return 0. If T[0] >= x: return C[0]. For i = 0 .. |T|-2: if T[i+1] < x continue; p = (T[i] == T[i+1]) ? 0 : clamp01((x - T[i]) / (T[i+1] - T[i])); return C[i] + p * (C[i+1] - C[i]). After the loop (x > T_last): return C_last. Flat extrapolation at both ends.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `x` | balance timing. Days in the shipped setup: runtime simulation uses GetBalanceTimingMinutes() = UseSpawnerCharges ? Floor(CurrentTotalPlaytime / SpawnerChargesChunkMinutes) : CurrentPlaytime / 60, and the shipped values UseSpawnerCharges=1, SpawnerChargesChunkMinutes=1440 make it whole days. With charges off it would be playtime hours. Forecast snapshots use the island-derived day, and other callers pass a snapshot day or time chunk. | caller: SimulationSystem.GetBalanceTimingMinutes (SimulationSystem.cs:515-520), snapshot day, ForecastSnapshots.ConvertIslandTimingToDays, BossSpawnService chunk |
| `T_i` | timing breakpoints (days), semicolon-separated string | Combat Balance Config.Interpolations.Interpolations[].TimingPoints |
| `C_i` | coefficient breakpoints, semicolon-separated string | Combat Balance Config.Interpolations.Interpolations[].CoefPoints |
| `p` | segment progress clamped to [0,1]; 0 for a zero-width segment | computed |

**输出**：float coefficient of one curve

**代码**：[Balance/BalanceInterpolation.cs:23-52](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/BalanceInterpolation.cs#L23)；[Balance/BalanceInterpolation.cs:58-64](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/BalanceInterpolation.cs#L58)；[Simulation/Systems/SimulationSystem.cs:515-520](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L515)

**状态依据**：Evaluate carries 'Original ELF VA 0x1579ed4. Ordered comparisons deliberately preserve NaN behavior.' and ParsePoints carries 'Original ELF VA 0x157a65c'. Data check: snapshot StatsFromFeaturesFlat / PercentMultiplier reproduce exactly from these curves at integer days (e.g. day 3 -> 0.20396976, day 405 -> 3.0 / 4.9075).

**配置参数**：`Combat Balance Config.Interpolations.Interpolations[].TimingPoints`；`Combat Balance Config.Interpolations.Interpolations[].CoefPoints`；`Combat Balance Config.Interpolations.Interpolations[].Use`；`Simulation Config.UseSpawnerCharges`；`Simulation Config.SpawnerChargesChunkMinutes`

**使用方**：balance.total_coefficient

**备注**：The points are re-parsed on every call (no caching). Because the comparisons are ordered, a NaN timing fails both T[0] >= x and T[i+1] < x, enters segment 0, and returns NaN. The clamp is written as two steps (cap at 1, then floor at 0) plus a redundant progress<0 check. Use is not checked here; the caller (GetTotalCoefficient) filters on it. FormatPoint (BalanceInterpolation.cs:66-69) is an empty editor helper. IslandPoints are not read at runtime; see balance.interpolation_points_from_islands. The input-unit caveat comes from simulation.interp_evaluate: the curves are authored in days, which matches runtime timing only in spawner-charges mode with 1440-minute chunks (the shipped config). In non-charges mode GetBalanceTimingMinutes returns CurrentPlaytime/60, i.e. hours.

**依赖图节点**：输入 `value:sim.balance_timing`、`value:time.island_timing_days`、`config:CombatBalanceConfig.Interpolations.Interpolations[].TimingPoints`、`config:CombatBalanceConfig.Interpolations.Interpolations[].CoefPoints`；输出 `value:sim.interpolation_curve_value`

**核验记录**（已写入上方字段）：

- 合并复核修正：(a) Lost caveat. The simulation.interp_evaluate member described x as 'timing in days (or hours for minute-mode bakes)', and the merged variable x drops the hours case. The code confirms the caveat: SimulationSystem.GetBalanceTimingMinutes (SimulationSystem.cs:515-520, native 0x13ebe60) returns UseSpawnerCharges ? Floor(CurrentTotalPlaytime / SpawnerChargesChunkMinutes) : CurrentPlaytime / 60. The shipped Simulation Config sets UseSpawnerCharges=1 and SpawnerChargesChunkMinutes=1440, so x is in days. With charges off, x becomes playtime/60 (hours), while the curves stay keyed in days. Every other field, including the expression, the NaN note and native VAs 0x1579ed4 / 0x157a65c, matches BalanceInterpolation.cs:23-64.

### `balance.total_coefficient`

**Combined interpolation coefficient by type (GetTotalCoefficient / GetInterpolationCoefficient)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 合并自：`balance.total_coefficient`、`simulation.interp_total_coefficient`
- 原生地址：`0x1579dac (GetTotalCoefficient), 0x157dd80 (GetInterpolationCoefficient 2-arg), 0x157dd98 (GetInterpolationCoefficient with rarity)`

```text
type' = (rarity == Legendary && type == ItemEmission(0)) ? ItemLegendaryEmission(6) : type; multiply = !(type' < 8 && ((1 << type') & 0xF5) != 0), i.e. multiply for types 1 (ItemChainProbability) and 3 (StatModifier) [and any type >= 8], add for types 0,2,4,5,6,7. K = multiply ? PRODUCT_j f_j(x) (start 1) : SUM_j f_j(x) (start 0), over curves j in config order with Use == true and Type == type'. CombatBalanceConfig.GetInterpolationCoefficient(x, type) = K(x, type, Rare); GetInterpolationCoefficient(x, type, rarity) = K(x, type, rarity).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `x` | timing (days) | caller |
| `type` | BalanceInterpolationType | caller constant |
| `rarity` | item rarity; only Legendary changes the result (ItemEmission remap); defaults to Rare | caller |
| `f_j` | Evaluate(x) of each enabled curve of the type | balance.interpolation_evaluate |
| `0xF5` | bitmask of additive types {0,2,4,5,6,7} | native constant |

**输出**：float K(type, x)

**代码**：[Balance/BalanceInterpolationsConfig.cs:12-27](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/BalanceInterpolationsConfig.cs#L12)；[Balance/CombatBalanceConfig.cs:135-145](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L135)

**状态依据**：Native VA comments on all three methods ('Original ELF VA 0x1579dac: multiplication for types 1/3, addition for 0/2/4/5/6/7', 0x157dd80, 0x157dd98). Verified against Simulation Data: day 405 type2 sum = 0.3+0.6+0.7+0.4+0.4+0.2+0.4 = 3.0 and type3 product = 1.6*1.8*1.704 = 4.9075, equal to the stored StatsFromFeaturesFlat / PercentMultiplier.

**配置参数**：`Combat Balance Config.Interpolations.Interpolations[].Use`；`Combat Balance Config.Interpolations.Interpolations[].Type`；`Combat Balance Config.Interpolations.Interpolations[].TimingPoints`；`Combat Balance Config.Interpolations.Interpolations[].CoefPoints`

**使用方**：simulation: [Simulation/Systems/SimulationSystem.cs:875-887](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L875) (GetPlayerAbilityDpsMultiplier / GetPlayerAblityEhpMultiplier = K(type 4/5) + 1)；simulation: [Simulation/Systems/SimulationSystem.cs:925](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L925) (StatModifier coefficient on DPS) and :953 (StatModifier coefficient on defense)；simulation: [Simulation/SimulationConfig.cs:120-121](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L120) (spawner bake, types 4/5)；simulation: [Simulation/Forecast/ForecastSnapshots.cs:54-55](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastSnapshots.cs#L54) (ability DPS/EHP mult + 1)；boss: [BossTower/BossSpawnService.cs:457-458](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/BossSpawnService.cs#L457) (BakeStatsForFloor, types 4/5)；simulation.dps_data；simulation.defense_data；simulation.ability_multipliers；simulation.snapshot_stat_composition；simulation.bake_spawner_health；simulation.bake_spawner_damage；simulation.forecast_auto_balanced_enemy；simulation.item_emission_in_sim

**备注**：An empty type yields 1 (multiplicative) or 0 (additive). Shipped curves (Combat Balance Config lines ~8150-8265): type0 ItemEmission = Tower(26..150: 0..0.2) + GraphMapUpgrade(20..150: 0..1) + EventItems(7..26: 0..0.3); type6 ItemLegendaryEmission = TowerLeg(110..180: 0..0.8) + GraphMapUpgrade(110..180: 0..1); type7 ItemEmissionByType = TowerByType(26..150: 0..1.8); type1 ItemChainProbability = FarmItem(20..44: 0.9..1); type2 StatsFromFeaturesFlat = GraphFlat 0..0.3 + AmuletFlat 0..0.6 + VIP 0..0.7 + InApps 0..0.4 + EventsFlat 0..0.4 + Ads 0..0.2 + ItemsEconomic 0..0.4 (max sum 3.0); type3 StatModifier = GraphMult 1..1.6 * AmuletMult 1..1.2..1.8 * EventsMult 1..1.1..1.5..2.0 (max product 5.76 at day 775); type4 PlayerAbilityDPSMult = AbilityDPS (20,90,150,775 -> 0,0.4,1.3,4); type5 PlayerAblityEHPMult = AbilityEHP (-> 0,0.2,0.5,1.5). Types 0,1,2,6,7 have no runtime consumer in the reconstruction (SimulationSystem.GetSimulationInterpolationCoefficient / UpdateInterpolationStatModifier are stubs). GetDebugInfo (BalanceInterpolationsConfig.cs:29-32) is an empty debug helper.

**依赖图节点**：输入 `value:sim.interpolation_curve_value`、`config:CombatBalanceConfig.Interpolations.Interpolations[].Use`、`config:CombatBalanceConfig.Interpolations.Interpolations[].Type`、`constant:BalanceInterpolationsConfig.additive_type_mask`；输出 `value:sim.balance_coefficient`、`value:sim.balance_coefficient[ItemEmission]`、`value:sim.balance_coefficient[StatsFromFeaturesFlat]`、`value:sim.balance_coefficient[StatModifier]`、`value:sim.balance_coefficient[PlayerAbilityDPSMult]`、`value:sim.balance_coefficient[PlayerAblityEHPMult]`

### `balance.island_timing_to_days`

**Island timing to days conversion (ConvertIslandTimingToDays)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 合并自：`balance.island_timing_to_days`、`simulation.island_timing_to_days`
- 原生地址：`0x157ad44`

```text
n = FloorToInt(s); prev = (n == 1) ? 0 : IslandTargetDays[n-2]; f = s - n; days = (f == 0) ? prev : prev + f * (IslandTargetDays[n-1] - prev)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `s` | fractional island timing (island number + progress; 7.5 = halfway through island 7) | forecast time points (ForecastAutoBalanceInput.ForecastLocationNumber + BossSnapshotTimingStep*i), IslandPoints, evolution fill rows |
| `IslandTargetDays[k]` | cumulative target day by which island k+1 is finished (0.5, 1.833, 4, 7, 10, 14, 20, 26, 34, 44, 56, 71, 90, 110, 130, 150, 180, 211.67, 245, 280, ...; 99 entries) | Combat Balance Config.IslandTargetDays |

**输出**：timing on the balance axis (days)

**代码**：[Simulation/Forecast/ForecastSnapshots.cs:68-75](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastSnapshots.cs#L68)；[Simulation/Forecast/ForecastSnapshots.cs:47](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastSnapshots.cs#L47)；[Balance/CombatBalanceConfig.cs:252-255](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L252)；[Simulation/Systems/SimulationSystem.cs:528-531](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L528)

**状态依据**：The only functional body is ForecastSnapshots.ConvertIslandTimingToDays, annotated 'Original 0x157ad44, inlined locally because the public SimulationSystem/config wrappers remain empty.' Both public wrappers, CombatBalanceConfig.ConvertIslandTimingToDays (CombatBalanceConfig.cs:252-255) and SimulationSystem.ConvertIslandTimingToDays (SimulationSystem.cs:528-531), are still stubs returning 0f.

**配置参数**：`Combat Balance Config.IslandTargetDays`

**使用方**：simulation.forecast_build_core (ForecastSnapshots.cs:47, day timing per forecast time point)；balance.evolution_target_timings_from_islands；balance.interpolation_points_from_islands

**备注**：The base of island n is the completion of the previous island: s = 9 gives IslandTargetDays[7] = 26, the end of island 8, and s = 1 gives 0. Values of s < 1 would index out of range (no guard). Checked against the asset: every *EvolutionTargetTimings value and every interpolation TimingPoints value equals this function applied to the stored island numbers (max error 1.5e-4). Related stubs: SimulationSystem.GetNextLocationInterpolationTiming (SimulationSystem.cs:523-526) and CombatBalanceConfig.ConvertIslandRowToDayTimings (CombatBalanceConfig.cs:247-250).

**依赖图节点**：输入 `config:CombatBalanceConfig.IslandTargetDays`、`config:CrystalEventRosterConfig.ForecastSettings.AutoBalanceInput.ForecastLocationNumber`、`config:CrystalEventRosterConfig.ForecastSettings.AutoBalanceInput.BossSnapshotTimingStep`；输出 `value:time.island_timing_days`

**核验记录**（已写入上方字段）：

- 采纳 `status_evidence`（来自 `balance.island_timing_to_days`）：Confirmed that SimulationSystem.cs:528-531 is 'return 0f;'. status_evidence now names both stub wrappers and the single functional copy. SimulationSystem.cs:528-531 is added to code_refs, and the x=9 -> IslandTargetDays[7]=26 (end of island 8) indexing is stated in the notes.

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

**由数据反推的生成规则**（拟合规则，详见 [Generation/IslandTimeline.md](../Generation/IslandTimeline.md)）：

```text
Islands 1-15 hand-set; from island 16: IslandTargetDays(i) = (5/6)(i+1)(i+2) - 105 days (i = island number), i.e. increment (5/3)(i+1) days. Island 15 by the formula would be 121.67, stored 130.
```

吻合情况：Islands 16-99: 84/84 float32-identical (max decimal diff 0.000333). Gate spawner OT == IslandTargetDays for islands 1-16 (16/16).

**配置参数**：`Combat Balance Config.IslandTargetDays`

**使用方**：balance.island_timing_to_days；simulation: SimulationConfig.cs:224 (early spawner optimum-time offset)；simulation forecast

**备注**：This is the master pacing lever. Changing it shifts every evolution target timing and every interpolation curve, but only after re-running the missing editor apply steps, because the derived arrays are baked.

**依赖图节点**：输入 `config:CombatBalanceConfig.IslandTargetDays`；输出 `value:island.target_days`

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

**由数据反推的生成规则**（精确复现，详见 [Generation/IslandTimeline.md](../Generation/IslandTimeline.md)）：

```text
E_k = StartValue + (k-1)*StepPerEvolution (k=1..15). Step = 1.75 + 0.25*r for Common..Epic (r=0..3), Legendary 3.5 (hand-set; linear would be 2.75). Weapon starts 7.5/9/10/11.5/12.5; armor starts later by 0.5/0.5/1.75/3.5/6 islands.
```

吻合情况：150/150 cells, max error 0.

**配置参数**：`Combat Balance Config.EvolutionIslandFillRuleRows`；`Combat Balance Config.EvolutionIslandTimingRows`

**使用方**：balance.evolution_target_timings_from_islands

**备注**：Design intent: the item of rarity r reaches evolution k when the player is on island E_k.

**依赖图节点**：输入 `config:CombatBalanceConfig.EvolutionIslandFillRuleRows[].StartValue`、`config:CombatBalanceConfig.EvolutionIslandFillRuleRows[].StepPerEvolution`、`config:CombatBalanceConfig.EvolutionIslandFillRuleRows[].Rarity`、`constant:CombatBalanceConfig.EvolutionTimingLevelsCount`；输出 `config:CombatBalanceConfig.EvolutionIslandTimingRows`

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

**由数据反推的生成规则**（精确复现，详见 [Generation/IslandTimeline.md](../Generation/IslandTimeline.md)）：

```text
<Rarity>[Armor]EvolutionTargetTimings[k-1] = ConvertIslandTimingToDays(E_k): n = floor(s); prev = (n == 1 ? 0 : ITD[n-2]); prev + (s-n)*(ITD[n-1]-prev).
```

吻合情况：150/150, max error 4.8e-5 days (float32).

**配置参数**：`Combat Balance Config.CommonEvolutionTargetTimings`；`Combat Balance Config.UncommonEvolutionTargetTimings`；`Combat Balance Config.RareEvolutionTargetTimings`；`Combat Balance Config.EpicEvolutionTargetTimings`；`Combat Balance Config.LegendaryEvolutionTargetTimings`；`Combat Balance Config.CommonArmorEvolutionTargetTimings`；`Combat Balance Config.UncommonArmorEvolutionTargetTimings`；`Combat Balance Config.RareArmorEvolutionTargetTimings`；`Combat Balance Config.EpicArmorEvolutionTargetTimings`；`Combat Balance Config.LegendaryArmorEvolutionTargetTimings`

**使用方**：balance.evolution_shards_capture；balance.evolution_value_autobalance

**备注**：No runtime code reads these arrays; they only drive the editor balancing pipeline.

**依赖图节点**：输入 `config:CombatBalanceConfig.EvolutionIslandTimingRows`、`config:CombatBalanceConfig.IslandTargetDays`、`value:time.island_timing_days`；输出 `config:CombatBalanceConfig.CommonEvolutionTargetTimings`、`config:CombatBalanceConfig.UncommonEvolutionTargetTimings`、`config:CombatBalanceConfig.RareEvolutionTargetTimings`、`config:CombatBalanceConfig.EpicEvolutionTargetTimings`、`config:CombatBalanceConfig.LegendaryEvolutionTargetTimings`、`config:CombatBalanceConfig.CommonArmorEvolutionTargetTimings`、`config:CombatBalanceConfig.UncommonArmorEvolutionTargetTimings`、`config:CombatBalanceConfig.RareArmorEvolutionTargetTimings`、`config:CombatBalanceConfig.EpicArmorEvolutionTargetTimings`、`config:CombatBalanceConfig.LegendaryArmorEvolutionTargetTimings`

### `balance.interpolation_points_from_islands`

**Interpolation timing points from island points (ApplyIslandTimings)**

- 状态：**编辑器工具缺失**（`editor_only_missing`）
- 类型：`editor_generator`
- 合并自：`balance.interpolation_points_from_islands`、`simulation.interp_island_timing_points`

```text
For each curve and each knot j: TimingPoints_j = ConvertIslandTimingToDays(IslandPoints_j); for an integer island n this is 0 if n == 1 else IslandTargetDays[n-2]. The result is written back as the TimingPoints string (format via FormatPoint, unknown).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `IslandPoints_j` | island-number knots of a curve | Combat Balance Config.Interpolations.Interpolations[].IslandPoints |
| `IslandTargetDays` | cumulative target day by which each island is finished | Combat Balance Config.IslandTargetDays |

**输出**：Interpolations[].TimingPoints (string, days)

**代码**：[Balance/BalanceInterpolation.cs:54-56](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/BalanceInterpolation.cs#L54)；[Balance/BalanceInterpolation.cs:66-69](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/BalanceInterpolation.cs#L66)；[Balance/CombatBalanceConfig.cs:131-133](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L131)

**状态依据**：BalanceInterpolation.ApplyIslandTimings and CombatBalanceConfig.ApplyInterpolationIslandsToTimings have empty bodies, and FormatPoint returns null. All shipped curves satisfy the rule (e.g. islands 9;14;17 -> 26;90;150, 3 -> 1.833333, 19 -> 211.6667, 22 -> 316.667, 32 -> 775, 1 -> 0).

**由数据反推的生成规则**（精确复现，详见 [Generation/IslandTimeline.md](../Generation/IslandTimeline.md)）：

```text
TimingPoints = ConvertIslandTimingToDays(IslandPoints).
```

吻合情况：49/49 knots, max error 4.4e-5 days.

**配置参数**：`Combat Balance Config.Interpolations.Interpolations[].IslandPoints`；`Combat Balance Config.IslandTargetDays`

**使用方**：balance.interpolation_evaluate

**备注**：Designers author curves in island numbers, but the runtime reads only TimingPoints. Changing IslandTargetDays does not move curves at runtime until this editor step is re-run, because TimingPoints are baked. The conversion is balance.island_timing_to_days.

**依赖图节点**：输入 `config:CombatBalanceConfig.Interpolations.Interpolations[].IslandPoints`、`config:CombatBalanceConfig.IslandTargetDays`、`value:time.island_timing_days`；输出 `config:CombatBalanceConfig.Interpolations.Interpolations[].TimingPoints`

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

**由数据反推的生成规则**（拟合规则，详见 [Generation/ItemGrowth.md](../Generation/ItemGrowth.md)）：

```text
Shards requirement at level L ~ copies of the most-copied item of that rarity at level L-1, captured at the level target time (single-reference test). Whether the reference is "max copies" or "the Stabbing item" cannot be separated from snapshot data.
```

吻合情况：Weapon 28/35 within 5%, 34/35 within 15%, median 2.3%, max 16.1%; armor 23/30 and 29/30, median 2.9%, max 22.1%. Early levels over-predicted by 5-22%.

**配置参数**：`Combat Balance Config.EvolutionDamageShardsRequirements`；`Combat Balance Config.EvolutionArmorShardsRequirements`

**使用方**：items.evolution_cost

**备注**：The tables plateau (e.g. common weapon at 11000 from E9), which matches the end of the simulated horizon. Mythical shards = 10*k come from the mythical generator instead.

**依赖图节点**：输入 `value:sim.snapshot_progress`、`value:sim.active_playtime`、`config:ItemConfig.Rarity`、`config:ItemConfig.SlotType`、`config:CombatBalanceConfig.CommonEvolutionTargetTimings`、`config:CombatBalanceConfig.UncommonEvolutionTargetTimings`、`config:CombatBalanceConfig.RareEvolutionTargetTimings`、`config:CombatBalanceConfig.EpicEvolutionTargetTimings`、`config:CombatBalanceConfig.LegendaryEvolutionTargetTimings`、`config:CombatBalanceConfig.CommonArmorEvolutionTargetTimings`、`config:CombatBalanceConfig.UncommonArmorEvolutionTargetTimings`、`config:CombatBalanceConfig.RareArmorEvolutionTargetTimings`、`config:CombatBalanceConfig.EpicArmorEvolutionTargetTimings`、`config:CombatBalanceConfig.LegendaryArmorEvolutionTargetTimings`、`constant:CombatBalanceConfig.InitialShardsRequirement`；输出 `config:CombatBalanceConfig.EvolutionDamageShardsRequirements[].Value`、`config:CombatBalanceConfig.EvolutionArmorShardsRequirements[].Value`、`config:CombatBalanceConfig.EvolutionDamageValues[].ReferenceShardsRequirement`、`config:CombatBalanceConfig.EvolutionArmorValues[].ReferenceShardsRequirement`

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

**由数据反推的生成规则**（精确复现，详见 [Generation/ItemGrowth.md](../Generation/ItemGrowth.md)）：

```text
Value[L] = nice(max(H, Value[L-1] + 2*CopyValue[L-1]*Shards[L], armor only: 1.3*Value[L-1])), level 0 = pre-evolution item (armor uses stored ArmorBaseByRarity). H = ReferenceHeuristicValue. nice(): mantissa step 0.1 below 4, 0.5 in [4,8), 1 from 8; midpoint ties to even (C# Math.Round). ReferenceStatValue = simulated player flat stat TotalValue at ReferenceTimeChunkIndex; ReferenceStatPercent = Math.Round(100*Value/ReferenceStatValue) (ties to even). H itself: late-game asymptote weapon ~ k_t*M_r*attackCooldown*statValue, armor ~ 0.80*DefenceFlat; early-game excess unexplained.
```

吻合情况：Weapon Value 225/225, armor Value 225/225; ReferenceStatValue 16/16 vs snapshots; ReferenceStatPercent 450/450 (half-up 439/450). Ablations: without armor x1.3 armor 111/225; half-up nice weapon 221/225.

**配置参数**：`Combat Balance Config.EvolutionDamageValues`；`Combat Balance Config.EvolutionArmorValues`；`Combat Balance Config.MinEvolutionValueFromShardsMultiplier`；`Combat Balance Config.MinArmorEvolutionValueFromPreviousEvolutionMultiplier`；`Combat Balance Config.SyncShieldValuesByLeader`

**使用方**：items.weapon_damage_evolved；items.armor_evolved

**备注**：Observed: weapon V1/preEvoMax is 2.5-8.4. Armor V1/preEvoMax is 1.69-2.47 and the armor min step ratio is 1.23, so the 2.0/1.3 minimums are not strictly enforced; there were hand edits. With SyncShieldValuesByLeader=1, armor rows are copied from a leader type to all of types 0/1/3.

**依赖图节点**：输入 `value:sim.dps_by_damage_type`、`value:sim.defense_by_damage_type`、`value:sim.snapshot_stat_total`、`value:sim.snapshot_progress`、`config:CombatBalanceConfig.MinEvolutionValueFromShardsMultiplier`、`config:CombatBalanceConfig.MinArmorEvolutionValueFromPreviousEvolutionMultiplier`、`config:CombatBalanceConfig.SyncShieldValuesByLeader`、`config:CombatBalanceConfig.EvolutionDamageShardsRequirements[].Value`、`config:CombatBalanceConfig.EvolutionArmorShardsRequirements[].Value`、`config:CombatBalanceConfig.CutDamageBaseByRarity`、`config:CombatBalanceConfig.PierceDamageBaseByRarity`、`config:CombatBalanceConfig.SmashDamageBaseByRarity`、`config:CombatBalanceConfig.CutDamageGrowthPerLevelByRarity`、`config:CombatBalanceConfig.PierceDamageGrowthPerLevelByRarity`、`config:CombatBalanceConfig.SmashDamageGrowthPerLevelByRarity`、`config:CombatBalanceConfig.ArmorBaseByRarity`、`config:CombatBalanceConfig.ArmorGrowthPerLevelByRarity`、`config:CombatBalanceConfig.CommonEvolutionTargetTimings`、`config:CombatBalanceConfig.UncommonEvolutionTargetTimings`、`config:CombatBalanceConfig.RareEvolutionTargetTimings`、`config:CombatBalanceConfig.EpicEvolutionTargetTimings`、`config:CombatBalanceConfig.LegendaryEvolutionTargetTimings`、`config:CombatBalanceConfig.CommonArmorEvolutionTargetTimings`、`config:CombatBalanceConfig.UncommonArmorEvolutionTargetTimings`、`config:CombatBalanceConfig.RareArmorEvolutionTargetTimings`、`config:CombatBalanceConfig.EpicArmorEvolutionTargetTimings`、`config:CombatBalanceConfig.LegendaryArmorEvolutionTargetTimings`；输出 `config:CombatBalanceConfig.EvolutionDamageValues[].Value`、`config:CombatBalanceConfig.EvolutionArmorValues[].Value`、`config:CombatBalanceConfig.EvolutionDamageValues[].ReferenceStatPercent`、`config:CombatBalanceConfig.EvolutionArmorValues[].ReferenceStatPercent`、`config:CombatBalanceConfig.EvolutionDamageValues[].ReferenceHeuristicValue`、`config:CombatBalanceConfig.EvolutionArmorValues[].ReferenceHeuristicValue`、`config:CombatBalanceConfig.EvolutionDamageValues[].ReferenceStatValue`、`config:CombatBalanceConfig.EvolutionArmorValues[].ReferenceStatValue`、`config:CombatBalanceConfig.EvolutionDamageValues[].ReferenceTimeChunkIndex`、`config:CombatBalanceConfig.EvolutionArmorValues[].ReferenceTimeChunkIndex`、`config:CombatBalanceConfig.EvolutionDamageValues[].ReferenceShardsRequirement`、`config:CombatBalanceConfig.EvolutionArmorValues[].ReferenceShardsRequirement`

### `balance.copy_value_rule`

**Per-copy value after evolution**

- 状态：**编辑器工具缺失**（`editor_only_missing`）
- 类型：`editor_generator`
- 来源：合并核验

```text
Approximate (reverse-engineered) rule: CopyValue[L] ≈ max(CopyValue[L-1] * MinCopyValueGrowthMultiplier, round_?(CopyValueTargetShare * (Value[L] - Value[L-1]) / Shards[L])), with Value[0] = 0; frozen at the last value beyond the simulated horizon. Runtime use (ItemModel.CalcEvolutionDamage/Armor): evolved weapon damage = EvolutionDamageValues[dt,r,min(L,MaxEvolutionLevel)].Value + CopyValue[dt,r,min(L,MaxEvolutionLevel)] * Copies + Config.FlatDamage; evolved armor = EvolutionArmorValues[..].Value + CopyValue[..] * Copies (no flat term). MaxEvolutionLevel = 15.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `CopyValueTargetShare` | 0.5: the grind to evolution L is worth half the stat jump | Combat Balance Config.CopyValueTargetShare |
| `MinCopyValueGrowthMultiplier` | 1: CopyValue never decreases | Combat Balance Config.MinCopyValueGrowthMultiplier |
| `Shards[L]` | copies paid for evolution L | EvolutionDamageShardsRequirements / EvolutionArmorShardsRequirements (balance.evolution_shards_capture) |
| `Value[L]` | evolution base stat at level L | EvolutionDamageValues[].Value / EvolutionArmorValues[].Value |
| `MaxEvolutionLevel` | 15; runtime lookups clamp the evolution level to it (GetExistingEvolutionLevel) | Combat Balance Config.MaxEvolutionLevel |
| `FlatDamage` | per-item flat damage added to evolved weapon damage | ItemConfig.FlatDamage |

**输出**：EvolutionDamageValues[].CopyValue / EvolutionArmorValues[].CopyValue

**代码**：[Balance/CombatBalanceConfig.cs:17-19](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L17)；[Balance/CombatBalanceConfig.cs:25](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L25)；[Balance/CombatBalanceConfig.cs:70-76](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L70)；[Balance/CombatBalanceConfig.cs:147-150](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L147)；[Balance/CombatBalanceConfig.cs:174-178](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L174)；[Balance/CombatBalanceConfig.cs:200-204](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L200)；[Balance/CombatBalanceConfig.cs:206-208](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L206)；[Balance/CombatBalanceConfig.cs:217-219](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L217)；[Balance/CombatBalanceConfig.cs:221-223](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L221)；[Balance/CombatBalanceConfig.cs:225-227](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L225)；[Player/Inventory/ItemModel.cs:501-509](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L501)；[Player/Inventory/ItemModel.cs:511-518](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L511)

**状态依据**：The generator bodies (SetEvolutionValue x2, ApplyEvolutionAutoBalance, FillRemainingEvolutionValues) are empty. The fit is close for pike common (predicted 17.5/22.4/33.8/70.3 vs stored 17/22/33/70). It is exact for Mythical: shards are 10/20/30/..., so 0.5*100000/10 = 5000 = stored. Elsewhere it deviates in both directions. Knife (cut common): predicted 35/34.6/35 vs stored 34/34/37. Paddle (smash common): predicted 105/114.1/112.5/112.5 vs a stored flat 110. Armor common E1: predicted 107 vs stored 100. The MinCopyValueGrowth floor explains E2 (96 -> 100) but not E1. CopyValue is monotone non-decreasing in all 48 series. The rounding rule is unknown and probably includes hand edits, so treat the rule as approximate, not as an exact generator.

**由数据反推的生成规则**（精确复现，详见 [Generation/ItemGrowth.md](../Generation/ItemGrowth.md)）：

```text
CopyValue[L] = max(CopyValue[L-1], nice(0.5*(H - Value[L-1]) / Shards[L])); uses H, not the rounded Value; level 0 CopyValue = pre-evolution growth per level (binds for 9 armor rows).
```

吻合情况：Weapon CopyValue 225/225, armor CopyValue 225/225.

**配置参数**：`Combat Balance Config.CopyValueTargetShare`；`Combat Balance Config.MinCopyValueGrowthMultiplier`；`Combat Balance Config.MaxEvolutionLevel`；`Combat Balance Config.EvolutionDamageValues`；`Combat Balance Config.EvolutionArmorValues`；`Combat Balance Config.EvolutionDamageShardsRequirements`；`Combat Balance Config.EvolutionArmorShardsRequirements`；`ItemConfig.FlatDamage`

**使用方**：items.weapon_damage_evolved；items.armor_evolved；items.stat_per_copy

**备注**：The rounding rule is unknown, and the fit drifts after the reference time chunk saturates. Shipped values in the asset: CopyValueTargetShare 0.5, MinCopyValueGrowthMultiplier 1, MaxEvolutionLevel 15. Common shards are weapon 100/390/800/1600/3000 and armor 70/130/600/1200/1600. Mythical shards are 10*L. The series cover DamageType 0/1/3/4 (Stabbing/Cutting/Buckshot/Universal) x Rarity 0-4 and 6, with 15 levels each: 24 damage + 24 armor = 48 series. Divine (Rarity 5) has no evolution value rows or shard rows. Universal (DamageType 4) rows store Value 1 and CopyValue 0 for Rarity 0-4, but for Mythical they store the same ladder as the other types (100000/300000/600000/1000000/..., CopyValue 5000). Armor common CopyValue is 100 at E1-E4 (190 at E5) for DamageType 0/1/3, and 0 for Universal.

**依赖图节点**：输入 `config:CombatBalanceConfig.CopyValueTargetShare`、`config:CombatBalanceConfig.MinCopyValueGrowthMultiplier`、`config:CombatBalanceConfig.EvolutionDamageValues[].Value`、`config:CombatBalanceConfig.EvolutionArmorValues[].Value`、`config:CombatBalanceConfig.EvolutionDamageShardsRequirements[].Value`、`config:CombatBalanceConfig.EvolutionArmorShardsRequirements[].Value`、`config:CombatBalanceConfig.EvolutionDamageValues[].ReferenceTimeChunkIndex`、`config:CombatBalanceConfig.EvolutionArmorValues[].ReferenceTimeChunkIndex`；输出 `config:CombatBalanceConfig.EvolutionDamageValues[].CopyValue`、`config:CombatBalanceConfig.EvolutionArmorValues[].CopyValue`

**核验记录**（已写入上方字段）：

- 采纳 `status_evidence`（来自 `balance.copy_value_rule`）：Verified against Combat Balance Config.asset. Cut common (7000,34),(34000,34),(90000,37) with weapon shards 100/390/800 gives 35/34.6/35. Armor common E1 (15000,100) with shards 70 gives 107. status_evidence and expression now mark the rule as approximate.
- 合并复核修正：Several statements are factually wrong or incomplete when checked against Combat Balance Config.asset and the code. (a) The note 'Universal (DamageType 4) rows store Value 1, CopyValue 0' is wrong for Mythical (Rarity 6). Those Universal rows store the real ladder (E1 100000, E2 300000, E3 600000, E4 1000000, ...) with CopyValue 5000, identical to the other damage types. (b) The note 'Armor common CopyValue is 100 at E1-E4 for all damage types' is false for Universal, whose CopyValue is 0. It holds for DamageType 0/1/3 only. (c) The runtime-use formula leaves out the weapon FlatDamage term (ItemModel.CalcEvolutionDamage adds Config.FlatDamage) and the clamp of evolutionLevel to MaxEvolutionLevel=15 (GetExistingEvolutionLevel), which is the code-level reason values freeze. (d) Divine (Rarity 5) has no rows in EvolutionDamageValues, EvolutionArmorValues or the shard requirement lists: the rarities present are 0-4 and 6. The 48 series are 24 damage + 24 armor = 4 DamageTypes (0,1,3,4) x 6 rarities. The applied pending correction (knife, armor E1, approximate rule) is correctly reflected. The paddle numbers (105/114.1/112.5 vs a flat 110) and the Mythical 5000 = 0.5*100000/10 were verified.

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

**由数据反推的生成规则**（精确复现，详见 [Generation/ItemGrowth.md](../Generation/ItemGrowth.md)）：

```text
Mythical rows are a separate closed form, not the evolution generator: Value = 50000*L*(L+1), CopiesToEvolve = 10*L, CopyValue = 5000.
```

吻合情况：15/15 for each check; 120/120 copied rows equal; Value is on the nice grid only 11/15.

**配置参数**：`Combat Balance Config.MythicalEvolutionValues`

**使用方**：items.armor_evolved (mushroommancer_cap / mushroommancer_shield, Universal mythical armor granted via Vip Config)

**备注**：Pre-evolution mythical armor = 0 + 5000*copies (ArmorBase[6]=0, ArmorGrowth[6]=5000), with first evolution at 10 copies.

**依赖图节点**：输入 `constant:CombatBalanceConfig.mythical_value_coef`、`constant:CombatBalanceConfig.mythical_copies_per_level`、`constant:CombatBalanceConfig.mythical_copy_value`、`constant:CombatBalanceConfig.EvolutionTimingLevelsCount`；输出 `config:CombatBalanceConfig.MythicalEvolutionValues[].Value`、`config:CombatBalanceConfig.MythicalEvolutionValues[].CopiesToEvolve`、`config:CombatBalanceConfig.MythicalEvolutionValues[].CopyValue`、`config:CombatBalanceConfig.EvolutionDamageValues[].Value`、`config:CombatBalanceConfig.EvolutionDamageValues[].CopyValue`、`config:CombatBalanceConfig.EvolutionArmorValues[].Value`、`config:CombatBalanceConfig.EvolutionArmorValues[].CopyValue`、`config:CombatBalanceConfig.EvolutionDamageShardsRequirements[].Value`、`config:CombatBalanceConfig.EvolutionArmorShardsRequirements[].Value`

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

**由数据反推的生成规则**（假设，详见 [Generation/ItemGrowth.md](../Generation/ItemGrowth.md)）：

```text
ArmorBaseByRarity [600, 4500, 27000, 310000, 2500000] is written by the simulation (TryApplyArmorBaseAutoBalance / TrySetArmorBaseValue are empty at 6af011f), not hand data. Magnitude matches player DefenceFlat when that rarity is first acquired; type, moment and coefficient undetermined.
```

吻合情况：Ratio to own-type defence bracket 1.26, 2.12, 0.89, 1.06, 0.93; all 5 values on the nice grid.

**配置参数**：`Combat Balance Config.ArmorBaseByRarity`

**使用方**：items.armor_pre_evolution

**备注**：ArmorBase grows ~7.5x, 6x, 11.5x, 8x per rarity step (600 -> 2.5e6), unlike the weapon bases, which are all 0.

**依赖图节点**：输入 ；输出 `config:CombatBalanceConfig.ArmorBaseByRarity`、`config:CombatBalanceConfig.EvolutionDamageValues`、`config:CombatBalanceConfig.EvolutionArmorValues`、`config:CombatBalanceConfig.EvolutionDamageShardsRequirements`、`config:CombatBalanceConfig.EvolutionArmorShardsRequirements`

### `items.base_growth_tables`

**Rarity base/growth tables (key levers)**

- 状态：**纯数据**（`data_only`）
- 类型：`lookup_table`
- 来源：合并核验

```text
Indexed by (int)rarity C/U/R/E/L/Divine/Mythical. Per-copy weapon growth: Cut 12/50/140/500/2400/2400/2400, Pierce 8/35/100/400/1600/1600/1600, Smash 25/80/200/700/3500/3500/3500 (Buckshot -> Smash, Cutting -> Cut, everything else -> Pierce). Weapon base (Cut/Pierce/SmashDamageBaseByRarity) = 0 for all rarities. Armor base 600/4500/27000/310000/2500000/0/0, armor growth 100/200/350/500/2000/5000/5000 (same for all damage types). Pre-evolution: damage = base + growth * level + FlatDamage; armor = base + growth * level. Rarity step ratio of weapon growth is about x2.5-4.4 per step and x4-5 to Legendary. Smash/Pierce per-copy ratio C/U/R/E/L = 3.125 / 2.29 / 2.0 / 1.75 / 2.19 (range 1.75-3.1x), with a longer cooldown (paddle 2.0 s vs pike 1.0 s at common).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `r` | rarity index (0..6) | ItemConfig.Rarity |
| `DamageType` | weapon type selecting the Cut/Pierce/Smash table | ItemConfig.DamageType |

**输出**：inputs to items.weapon_damage_pre_evolution / items.armor_pre_evolution

**代码**：[Balance/CombatBalanceConfig.cs:54-68](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L54)；[Balance/CombatBalanceConfig.cs:156-166](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L156)；[Balance/CombatBalanceConfig.cs:180-192](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L180)；[Player/Inventory/ItemModel.cs:475-484](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L475)；[Player/Inventory/ItemModel.cs:491-498](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L491)

**状态依据**：Hand-authored serialized arrays in Combat Balance Config.asset (lines 280-343); no generator exists even as a stub (TrySetArmorBaseValue is an empty stub).

**配置参数**：`Combat Balance Config.CutDamageBaseByRarity`；`Combat Balance Config.PierceDamageBaseByRarity`；`Combat Balance Config.SmashDamageBaseByRarity`；`Combat Balance Config.CutDamageGrowthPerLevelByRarity`；`Combat Balance Config.PierceDamageGrowthPerLevelByRarity`；`Combat Balance Config.SmashDamageGrowthPerLevelByRarity`；`Combat Balance Config.ArmorBaseByRarity`；`Combat Balance Config.ArmorGrowthPerLevelByRarity`

**使用方**：items.weapon_damage_pre_evolution；items.armor_pre_evolution；items.stat_per_copy

**备注**：DPS per copy at common: pike 8/1.0 = 8, knife 12/1.3 = 9.2, paddle 25/2.0 = 12.5. Weapon base arrays are all zero, so pre-evolution weapon damage is purely growth * copies + FlatDamage. Armor base for Divine/Mythical is 0.

**依赖图节点**：输入 `config:CombatBalanceConfig.CutDamageBaseByRarity`、`config:CombatBalanceConfig.PierceDamageBaseByRarity`、`config:CombatBalanceConfig.SmashDamageBaseByRarity`、`config:CombatBalanceConfig.CutDamageGrowthPerLevelByRarity`、`config:CombatBalanceConfig.PierceDamageGrowthPerLevelByRarity`、`config:CombatBalanceConfig.SmashDamageGrowthPerLevelByRarity`、`config:CombatBalanceConfig.ArmorBaseByRarity`、`config:CombatBalanceConfig.ArmorGrowthPerLevelByRarity`、`config:ItemConfig.Rarity`、`config:ItemConfig.DamageType`、`config:ItemConfig.SlotType`；输出 `value:item.rarity_base_growth`

**核验记录**（已写入上方字段）：

- 采纳 `expression`（来自 `items.base_growth_tables`）：Asset values Pierce 8,35,100,400,1600 and Smash 25,80,200,700,3500 give ratios 3.125/2.29/2.0/1.75/2.19; the expression now states them instead of '2-3x'.

### `items.item_config_levers`

**Per-item config levers**

- 状态：**纯数据**（`data_only`）
- 类型：`lookup_table`
- 来源：合并核验
- 原生地址：`0x146310c (OnValidate stamps StatModifiers with Source=Item); equip/apply consumers 0x145f590 (UnequipItem), 0x145fa20 (TryEquipItemToSlot), 0x145e394 (ApplyEquippedItemsToUnit)`

```text
Per ItemConfig: AttackCooldown is the runtime attack period of the weapon. PlayerAttackSystem sets weapon.Cooldown = AttackCooldown when an attack starts, and the hit/reset thresholds are GetAnimationCastPoint(weapon)*AttackCooldown and AnimationBackPoint*AttackCooldown. It is also the DPS divisor in ItemModel.DPS = Damage/AttackCooldown, in simulation DPS and in the inventory DPS display. AnimationCastPoint and AnimationBackPoint are normalized points inside the cooldown that time the hit and the reset (speed-adjusted per DamageType in combat.animation_cast_point). FlatDamage is added to weapon damage at every level, pre- and post-evolution; it is nonzero only for mace 400, chainsawpike 5000, coolHammer 15000 and chainsaw 30000. SimulationDpsWeaponMultiplier is a simulation-only DPS scale, read only for weapons: coolHammer 1.3, katana_div 1.35, fork_div 1.4, mailbox_div 1.4, coolHammer_div 1.6, pick_div 2, chainsawpike_div 2. The non-1 values on the armor assets prismcore_drillmask 1.4 and prismcore_drillmask_div 1.6 are dead data. DamageStatTypeFlat/Percent select which player stats scale the weapon in DamageCalculator: damage uses Source.GetStatValue(DamageStatTypeFlat) and (DamageStatTypePercent), e.g. cut -> CutDamageFlat 8 / CutDamagePercent 9. For armor, DamageStatTypeFlat (*DefenceFlat) selects icons and inventory stat grouping, and the InventoryStatView Damage total also iterates armor items with their stat types. DamageType selects the growth/evolution table and the enemy armor resist. StatModifiers are empty in all shipped items; OnValidate stamps them with Source=Item and Name=Key, and they are applied to the unit when the item is equipped. AbilityDefs are item abilities, unlocked by evolution level.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ItemConfig` | 88 assets = 45 base items (15 weapons + 15 helmets + 15 shields, rarities 0-4) + 39 legacy *_div Rarity-5 items (15 weapons + 12 helmets + 12 shields) + 4 mythical armor files (mushroommancer_cap and mushroommancer_shield, Rarity 6, each in 2 variants: *_0.asset and plain) | Assets/MonoBehaviour/*.asset with ItemConfig script guid 005880fd0f080d7d4797cd3b8a5eb23b |

**输出**：item-specific multipliers, attack timing and stat routing

**代码**：[Player/Inventory/ItemConfig.cs:11-47](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemConfig.cs#L11)；[Player/Inventory/ItemConfig.cs:51-59](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemConfig.cs#L51)；[Player/Inventory/ItemModel.cs:404](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L404)；[Player/Inventory/ItemModel.cs:482](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L482)；[Player/Inventory/ItemModel.cs:507](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L507)；[Services/Damage/DamageCalculator.cs:129-130](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L129)；[Simulation/Systems/SimulationSystem.cs:841-848](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L841)；[Simulation/Systems/SimulationSystem.cs:915-929](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L915)；[Player/Models/PlayerModel.cs:768-828](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L768)；[Player/PlayerAttackSystem.cs:130-146](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L130)；[Player/PlayerAttackSystem.cs:168](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L168)；[Player/PlayerAttackSystem.cs:206-229](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L206)；[UI/Inventory/InventoryStatView.cs:54-65](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/Inventory/InventoryStatView.cs#L54)

**状态依据**：Serialized config values. The only code on ItemConfig is the OnValidate stamping (native 0x146310c) and HitFrameAt60Fps (catalogued in items.hit_frame).

**配置参数**：`ItemConfig.AttackCooldown`；`ItemConfig.AnimationCastPoint`；`ItemConfig.AnimationBackPoint`；`ItemConfig.FlatDamage`；`ItemConfig.SimulationDpsWeaponMultiplier`；`ItemConfig.DamageStatTypeFlat`；`ItemConfig.DamageStatTypePercent`；`ItemConfig.StatModifiers`；`ItemConfig.AbilityDefs`；`ItemConfig.Number`；`ItemConfig.DamageType`；`ItemConfig.Rarity`；`ItemConfig.SlotType`

**使用方**：items.weapon_dps；items.weapon_damage_pre_evolution；items.weapon_damage_evolved；combat DamageCalculator；combat.player_weapon_attack_cycle；combat.animation_cast_point；simulation SimulationSystem (weapon DPS for spawner bake and forecast)；UI InventoryStatView

**备注**：Equipping adds item StatModifiers to unit stats (PlayerModel.cs TryEquipItemToSlot / ApplyEquippedItemsToUnit, native 0x145fa20/0x145e394). They are empty in all 88 shipped items, so item power comes entirely from Damage/Armor plus abilities. Common cooldowns: knife 1.3, pike 1.0, paddle 2.0; common armor items use 1. InventoryStatView's Damage total loops over EquippedItems, not EquippedWeapons. So equipped armor adds GetStatValue(its DamageStatTypePercent) * (GetStatValue(its DamageStatTypeFlat) + armor.Damage + ...) / AttackCooldown to that display number, and armor.Damage evaluates the weapon growth table for its DamageType. This affects the display only, not combat.

**依赖图节点**：输入 `config:ItemConfig.AttackCooldown`、`config:ItemConfig.AnimationCastPoint`、`config:ItemConfig.AnimationBackPoint`、`config:ItemConfig.FlatDamage`、`config:ItemConfig.SimulationDpsWeaponMultiplier`、`config:ItemConfig.DamageStatTypeFlat`、`config:ItemConfig.DamageStatTypePercent`、`config:ItemConfig.StatModifiers`、`config:ItemConfig.AbilityDefs`、`config:ItemConfig.Number`、`config:ItemConfig.DamageType`、`config:ItemConfig.Rarity`、`config:ItemConfig.SlotType`；输出 `value:item.config_levers`

**核验记录**（已写入上方字段）：

- 采纳 `variables[ItemConfig].source / summary counts`（来自 `items.item_config_levers`）：Re-scanned the 88 ItemConfig assets: Rarity 0-4 x 3 slot types x 3 = 45, Rarity 5 = 15 weapons + 12 + 12 = 39, Rarity 6 = 4 files (cap/shield x 2 variants). Variable meaning updated.
- 采纳 `expression (SimulationDpsWeaponMultiplier examples)`（来自 `items.item_config_levers`）：SimulationSystem.cs:841-848 and :915-929 read the multiplier only for non-armor items. The asset scan confirms the listed weapon values and the dead armor values: buck_epic_helmet.asset (key prismcore_drillmask) 1.4 and 'helm buck epic div.asset' (key prismcore_drillmask_div) 1.6. Expression updated.
- 合并复核修正：Three problems against the code. (a) The expression describes AttackCooldown only as a 'DPS divisor in simulation/inventory DPS'. It is also the real runtime attack period: PlayerAttackSystem.cs:168 sets weapon.Cooldown = AttackCooldown at attack start, :146 sets the reset cooldown to GetAnimationCastPoint*AttackCooldown+0.15, :142 tests the AnimationBackPoint*AttackCooldown threshold, and :228 sets the hit threshold to castPoint*AttackCooldown. ItemModel.DPS = Damage/AttackCooldown (ItemModel.cs:404). (b) The ItemConfig levers AnimationCastPoint and AnimationBackPoint, which shape the runtime attack cycle, are left out of the expression and config_params. (c) The claim that armor's DamageStatTypeFlat is 'used only for icons and inventory stat grouping' misses InventoryStatView.cs:54-58. The Damage total there iterates playerModel.EquippedItems (weapons AND armor) and uses each item's DamageStatTypePercent, DamageStatTypeFlat, Damage and AttackCooldown, so armor items also feed that displayed number. All asset values were re-verified: FlatDamage, the SimulationDpsWeaponMultiplier list, the 88/45/39/4 counts, and the empty StatModifiers.

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

**依赖图节点**：输入 `value:player.equipped_items`、`config:ItemConfig.SlotType`、`config:ItemConfig.DamageType`、`config:ItemConfig.AbilityDefs[].Conditions`；输出 `value:player.weapon_synergy_counts`、`value:ability.condition_pass`

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

**依赖图节点**：输入 `config:PlayerConfig.Slots[].UnlockedByDefault`、`config:PlayerConfig.Slots[].SlotType`、`config:PlayerConfig.Slots[].Key`、`config:PlayerConfig.Slots[].IsVip`、`value:player.slot_unlocked`、`config:QuestConfig.Chains[].ChainReward`、`state:quest_progress`、`state:vip_level`、`config:VipConfig.Features`、`state:save_counters`、`state:player_actions`、`config:ItemConfig.SlotType`、`value:ability.stub_effect_output`；输出 `value:player.unlocked_slot_counts`、`value:player.equipped_items`、`value:player.empty_slot_count`

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

**依赖图节点**：输入 `config:ItemConfig.StatModifiers`、`value:player.equipped_items`、`state:player_actions`；输出 `value:item.equip_stat_modifiers`

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

**依赖图节点**：输入 `config:PlayerConfig.StartingWeapon`、`config:PlayerConfig.Slots[].UnlockedByDefault`、`config:PlayerConfig.Slots[].SlotType`、`config:ItemConfig.SlotType`、`value:player.slot_unlocked`、`value:player.unlocked_slot_counts`、`state:player_actions`、`state:save_counters`；输出 `value:player.equipped_items`

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

**依赖图节点**：输入 `config:ItemConfig.AttackCooldown`、`config:WeaponAttackAnimationSettings.<AnimationDurationCap>k__BackingField`、`config:WeaponAttackAnimationSettings.<WindupRatio>k__BackingField`、`config:WeaponAttackAnimationSettings.<TravelRatio>k__BackingField`、`config:WeaponAttackAnimationSettings.<ReturnRatio>k__BackingField`、`config:WeaponAttackAnimationSettings.<OvertravelRatio>k__BackingField`、`constant:WeaponAttackTiming.WindupDurationRatio`、`constant:WeaponAttackTiming.TravelDurationRatio`、`constant:WeaponAttackTiming.ReturnDurationRatio`、`constant:WeaponAttackTiming.HitElapsedRatio`、`constant:WeaponAttackTiming.HitCooldownRatio`；输出 `value:weapon.attack_animation_timing`

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
