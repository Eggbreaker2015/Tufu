# 永久成长与奖励（`meta`）

[返回公式清单总览](../FormulaCatalog.md)

## 概述

Permanent growth runs through two stat containers. Player combat stats get a Flat StatModifier for each StatRewards entry when an enemy is devoured. Its value is authored Value × stat_drop[location] (MetaStat, base 1, raised by amulet-graph flats) × the zero-cooldown charge burst count × cheat × an optional ability DoubleLoot bonus × an optional VIP roll. StatBase.AddModifier merges modifiers that share (Name, Source, Type), so every devour of a class ("map.normal_mob", "map.elite", "map.tree") adds into one growing modifier. Souls (amulet currency) per devour are (max(1, 1+soul_drop_bonus) + souls_rarity_amount[soulKey]) × devour multiplier, and the Soul Pass doubles them with 25% chance. Item spawners use items_rarity_* meta stats for extra copies per kill, extra charges and a timer divisor; stat spawners subtract map_spawner_timer_reduce[location] from their cooldown. Meta stats are fed almost entirely by UpgradeModel (amulet graph and crystal upgrades). An upgrade applies f(level) as one replacing modifier, not a cumulative one, and charges PrettyRound(f_cost(level)) in a currency held by CurrencyModel. The other reward paths (granted rewards, baked GPM rewards, rewarded ads, chests) scale with the simulation's RPM per stat or with chest_level.

**核验结论**：The catalog is largely accurate. I checked every cited file and native annotation: MetaStat aggregation, param match, the persistence mask 0xEF0A, the calculators, the PlayerRewardSystem multipliers, the soul and item paths, UpgradeModel, PrettyRound, CurrencyModel, cashback and the rewarded-ad math all match the code, and their status labels hold. Both stubs (UpgradeMaxCapacityBonusSystem, PrepareEventRewardsCommandRunner) and the empty MetaStatsConfig.Fill are confirmed empty. Asset facts hold: 14 rewarded pools, PassDoubleSoulChance 0.25, the chest weights and the spawner counts. I reject nothing.

Corrections:
- grant_reward_stat misdescribes the bake fallback. A random or GPM reward with a SaveKey but no stored bake grants the raw Value, with no random pick and no RPM scaling.
- soul_drop_for_kill_count omits the forecast-health spawner filter, the 0.03 floor and the monotonic max.
- daily_rotation_stat does have a consumer: EventForecastService.cs:226.
- crystal_event_mob_stat_reward appends nothing when RPM ≤ 0; there is no floor of 1 in that case.
- The Health top-up condition is "not a percent type", not just "Flat".
- upgrade_total_preview filters by param only when DisplayParam is set.
- The meta stat count is 58, not 56.
- Some line references were wrong: the rewarded-ad value is at 287-297, not 309-318.

Missed formulas:
- Spawner charge top-up when capacity rises (EnemySpawner 569-585).
- Crystal chest legendary chance, 0.035 + 0.005·chest_level.
- free_chest cooldown days in ShopLotModel (native).
- Event currency and progress on devour, which bypass every multiplier.
- A second BakedStatReward writer in EventForecastService: no min-1 clamp and no command multiplier.
- Quest stat reward × stat_drop[location] (native, but tagged "candidate only").
- The per-upgrade simulation settings (SimWeight etc.), which have no consumer left in the reconstruction.

## 公式索引

| ID | 名称 | 状态 | 类型 |
|---|---|---|---|
| [`meta.metastat_value`](#metametastat_value) | MetaStat parameterised value aggregation | 原生证据恢复 | `runtime_formula` |
| [`meta.metastat_param_match`](#metametastat_param_match) | Meta modifier param filter | 原生证据恢复 | `config_rule` |
| [`meta.metastat_base_values`](#metametastat_base_values) | Meta stat base values (design defaults) | 纯数据 | `lookup_table` |
| [`meta.metastats_fill`](#metametastats_fill) | MetaStatsConfig.Fill editor generator | 编辑器工具缺失 | `editor_generator` |
| [`meta.spawner_stat_drop_multiplier`](#metaspawner_stat_drop_multiplier) | Spawner stat-drop multiplier (stat_drop) | 原生证据恢复 | `runtime_formula` |
| [`meta.devour_stat_reward_scaling`](#metadevour_stat_reward_scaling) | Devour stat reward per StatRewards entry | 重建（无原生标注） | `runtime_formula` |
| [`meta.devour_stat_grant`](#metadevour_stat_grant) | PlayerRewardSystem.GrantStatReward multipliers | 原生证据恢复 | `runtime_formula` |
| [`meta.soul_drop_per_kill`](#metasoul_drop_per_kill) | Souls per devour (calculator) | 原生证据恢复 | `runtime_formula` |
| [`meta.soul_grant_on_devour`](#metasoul_grant_on_devour) | Soul grant on devour (gate, cheat, Soul Pass double) | 重建（无原生标注） | `runtime_formula` |
| [`meta.soul_drop_for_kill_count`](#metasoul_drop_for_kill_count) | Souls for a forecast kill count (simulation soul RPM) | 原生证据恢复 | `simulation` |
| [`meta.item_drop_copies_per_kill`](#metaitem_drop_copies_per_kill) | Item copies per kill (item spawners) | 重建（无原生标注） | `runtime_formula` |
| [`meta.item_drop_for_kill_count`](#metaitem_drop_for_kill_count) | Item copies for a kill count (forecast) | 重建（无原生标注） | `simulation` |
| [`meta.spawner_currency_reward`](#metaspawner_currency_reward) | Spawner flat currency reward | 重建（无原生标注） | `runtime_formula` |
| [`meta.event_plan_stat_reward`](#metaevent_plan_stat_reward) | Event spawner stat reward override from event plan | 原生证据恢复 | `runtime_formula` |
| [`meta.grant_reward_stat`](#metagrant_reward_stat) | Generic StatReward grant (quests/shop/pass/mail/events) | 重建（无原生标注） | `runtime_formula` |
| [`meta.bake_stat_reward`](#metabake_stat_reward) | Baked GPM/random stat reward value | 原生证据恢复 | `runtime_formula` |
| [`meta.pretty_round`](#metapretty_round) | PrettyRound (designer rounding) | 原生证据恢复 | `runtime_formula` |
| [`meta.daily_rotation_stat`](#metadaily_rotation_stat) | Daily rotation stat type | 原生证据恢复 | `runtime_formula` |
| [`meta.random_item_reward_pick`](#metarandom_item_reward_pick) | Random item reward resolution | 原生证据恢复 | `runtime_formula` |
| [`meta.chest_drop_weighted`](#metachest_drop_weighted) | Chest drop (ItemType, Rarity) selection | 重建（无原生标注） | `runtime_formula` |
| [`meta.chest_copies`](#metachest_copies) | Copies per chest | 重建（无原生标注） | `runtime_formula` |
| [`meta.chest_level_vip`](#metachest_level_vip) | VIP chest level bonus | 重建（无原生标注） | `runtime_formula` |
| [`meta.upgrade_cost`](#metaupgrade_cost) | Upgrade cost per level | 重建（无原生标注） | `runtime_formula` |
| [`meta.upgrade_modifier_apply`](#metaupgrade_modifier_apply) | Upgrade → meta/player modifier value | 原生证据恢复 | `runtime_formula` |
| [`meta.upgrade_purchase_rule`](#metaupgrade_purchase_rule) | Upgrade purchase / starting level rule | 原生证据恢复 | `config_rule` |
| [`meta.upgrade_total_preview`](#metaupgrade_total_preview) | Upgrade total-value preview at a target level | 原生证据恢复 | `runtime_formula` |
| [`meta.currency_balance`](#metacurrency_balance) | Currency balance arithmetic | 原生证据恢复 | `runtime_formula` |
| [`meta.upgrade_max_capacity_bonus`](#metaupgrade_max_capacity_bonus) | Max-capacity upgrade top-up (geodes/enchant stones/tickets/free chest) | 空壳 | `runtime_formula` |
| [`meta.prepare_event_rewards`](#metaprepare_event_rewards) | Prepare event success/failed rewards | 空壳 | `runtime_formula` |
| [`meta.rewarded_ad_stat_value`](#metarewarded_ad_stat_value) | Rewarded-ad permanent stat offer value | 原生证据恢复 | `runtime_formula` |
| [`meta.rewarded_pool_pick`](#metarewarded_pool_pick) | Rewarded-ad stat type pool | 原生证据恢复 | `runtime_formula` |
| [`meta.rewarded_dynamic_weights`](#metarewarded_dynamic_weights) | Rewarded-ad dynamic weight adaptation (AB) | 原生证据恢复 | `runtime_formula` |
| [`meta.cashback_earned_stat`](#metacashback_earned_stat) | Cashback earned-stat ledger | 原生证据恢复 | `runtime_formula` |
| [`meta.location_enemy_reward`](#metalocation_enemy_reward) | location_enemy_reward meta stat | 纯数据 | `lookup_table` |
| [`meta.crystal_chest_legendary_chance`](#metacrystal_chest_legendary_chance) | Crystal chest legendary chance | 重建（无原生标注） | `runtime_formula` |
| [`meta.free_chest_cooldown_days`](#metafree_chest_cooldown_days) | Free chest lot cooldown from meta stat | 重建（无原生标注） | `runtime_formula` |
| [`meta.event_currency_devour_reward`](#metaevent_currency_devour_reward) | Event variant pick and event currency / progress on devour | 重建（无原生标注） | `runtime_formula` |
| [`meta.event_forecast_gpm_bake`](#metaevent_forecast_gpm_bake) | Event-forecast GPM stat reward bake (second BakedStatReward writer) | 重建（无原生标注） | `runtime_formula` |
| [`meta.quest_stat_reward_stat_drop`](#metaquest_stat_reward_stat_drop) | Quest step stat reward scaled by stat_drop | 原生证据恢复 | `runtime_formula` |
| [`meta.upgrade_simulation_settings`](#metaupgrade_simulation_settings) | Per-upgrade simulation purchase settings | 纯数据 | `config_rule` |

## 公式

### `meta.metastat_value`

**MetaStat parameterised value aggregation**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x14857f8, 0x148641c, 0x1485ef4`

```text
v(p) = (Π_{m∈PM(p)} m.Value) * (1 + Σ_{m∈PA(p)} m.Value) * (BaseValue + Σ_{m∈Flat(p)} m.Value) * Π_{source s} (1 + Σ_{m∈PAPM(p), m.Source=s} m.Value); X(p) = modifiers of type X that pass MatchesParam(m, p)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `BaseValue` | stat base value | Meta Stats Config.asset Stats[].BaseValue |
| `m.Value` | modifier value (float) | UpgradeModel.ApplyAll, ChestsModel VIP modifier, saved MetaStatDataUd.Modifiers |
| `p` | int param (location number, rarity+1, -1 = event golem) or string param (soul key, feature key, boss key) | caller |
| `PM/PA/Flat/PAPM` | ModifierType 2 / 1 / 0 / 4 | StatModifier.ModifierType |

**输出**：double meta stat value for a given param

**代码**：[Meta/MetaStat.cs:68-80](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStat.cs#L68)；[Meta/MetaStat.cs:152-174](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStat.cs#L152)

**状态依据**：Comments 'Native ELF VA 0x1485ef4. Meta modifiers filter by parameter before combining.'

**配置参数**：`Meta Stats Config.Stats[].BaseValue`

**使用方**：meta.spawner_stat_drop_multiplier；meta.soul_drop_per_kill；meta.soul_drop_for_kill_count；meta.item_drop_copies_per_kill；spawners.effective_charges；spawners.base_cooldown；powerups.amulet_souls_drop_unlock；[Quests/Commands/ClaimQuestStepRewardCommandHandler.cs:120](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/Commands/ClaimQuestStepRewardCommandHandler.cs#L120)；crystal/tower/boss-tower subsystems reading crystal_* / tower_* keys

**备注**：Unlike StatBase.CalculateValue, MetaStat ignores EventPercentModifier, PostFormula* and other types. PercentAdditive starts at 1, so PA values are fractions. Nearly all real meta writers are Flat (ModifierType 0). The exception is crystal_enchant_efficiency, which uses PAPM (type 4) at 0.01*level.

**依赖图节点**：输入 `config:MetaStatsConfig.Stats[].BaseValue`、`config:MetaStatsConfig.Stats[].Key`、`value:player.upgrade_meta_modifier`、`value:player.meta_modifier_applicable`、`state:crystal_equipment`、`config:CrystalConfig.LegendaryTowerDamageBonus`、`config:CrystalConfig.LegendaryTowerDamageBonusPerEnchant`、`config:CrystalConfig.LegendaryTowerDamageReduction`、`config:CrystalConfig.LegendaryTowerDamageReductionPerEnchant`、`config:CrystalConfig.LegendaryEconomyProcChancePerEnchant`、`constant:CrystalModel.objective_kill_blacksmithing_bonus`、`state:vip_level`；输出 `value:player.meta_stat`、`value:player.meta_stat[mark_lifesteal]`、`value:player.meta_stat[rage_damage_reduce]`、`value:player.meta_stat[rage_extra_health]`、`value:player.meta_stat[rage_extra_hit]`、`value:player.meta_stat[tower_damage]`、`value:player.meta_stat[tower_damage_reduction]`、`value:player.meta_stat[island_medal_bonus]`、`value:player.meta_stat[event_golem_damage]`、`value:player.meta_stat[weak_spawner_extra_range]`、`value:player.meta_stat[event_golem_extra_range]`、`value:player.meta_stat[block_to_supercrit]`、`value:player.meta_stat[evasion_mark]`、`value:player.meta_stat[extra_mark]`、`value:player.meta_stat[soul_drop_bonus]`、`value:player.meta_stat[souls_rarity_amount]`、`value:player.meta_stat[souls_drop_unlock]`、`value:player.meta_stat[feature_unlock]`、`value:player.meta_stat[items_rarity_drop_amount]`、`value:player.meta_stat[items_rarity_charges]`、`value:player.meta_stat[items_rarity_timer]`、`value:player.meta_stat[map_spawner_timer_reduce]`、`value:player.meta_stat[crystal_timer_on_kill_reduce]`、`value:player.meta_stat[chest_level]`、`value:player.meta_stat[free_chest]`、`value:player.meta_stat[economy_item_abilities_chance]`、`value:player.meta_stat[empowered_lightning]`、`value:player.meta_stat[lightning_damage]`、`value:player.meta_stat[hammer_regen]`、`value:player.meta_stat[location_enemy_reward]`

### `meta.metastat_param_match`

**Meta modifier param filter**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`config_rule`
- 来源：子系统梳理
- 原生地址：`0x1487008, 0x1487080`

```text
match(m, i, s) = (m.StringParam ≠ ∅) ? (m.StringParam == s) : (s ≠ ∅ ? m.Param == 0 : (m.Param == 0 ∨ m.Param == i))
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `m.Param / m.StringParam` | scoping param on modifier | UpgradeModifierDef.Param / StringParam |
| `i, s` | query int / string param | caller |

**输出**：bool: whether a modifier contributes to v(p)

**代码**：[Meta/MetaStat.cs:176-188](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStat.cs#L176)

**状态依据**：Comment 'Native ELF VA 0x1487008: global integer-zero modifiers also affect string queries.'

**使用方**：meta.metastat_value；[Meta/Upgrades/UpgradeModel.cs:147](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeModel.cs#L147) (display variant)

**备注**：Param 0 means global and applies to every int query and every string query. For example stat_drop with Param=1 boosts island 1 only, and Param=-1 boosts event golems only (SpawnerStatDropAmountCalculator.EventGolemParam).

**依赖图节点**：输入 `value:player.upgrade_meta_modifier`、`state:crystal_equipment`；输出 `value:player.meta_modifier_applicable`

### `meta.metastat_base_values`

**Meta stat base values (design defaults)**

- 状态：**纯数据**（`data_only`）
- 类型：`lookup_table`
- 来源：合并核验
- 原生地址：`0x1487e78 (MetaStatsModel.Populate)`

```text
For each MetaStatDef d in MetaStatsConfig.Stats (skipping empty keys and keys already added, so the first wins): stat = new MetaStat(d.BaseValue, d.Key). The saved base_value is written only when no saved entry exists and is never read back (BindToUserData restores only modifiers), so config BaseValue always wins. MetaStat.GetValue(p) = mult * add * (BaseValue + flat) * Π_source(1 + PAPM)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `BaseValue` | default before any upgrade | MonoBehaviour/Meta Stats Config.asset Stats[].BaseValue |
| `Key` | meta stat identifier (58 entries in the asset) | MonoBehaviour/Meta Stats Config.asset Stats[].Key |

**输出**：base for every meta stat. Non-zero bases: stat_drop=1, items_rarity_timer=1, crystal_enchant_efficiency=1, crystal_max_enchant_stones=1, chest_level=1. The other 53 are 0.

**代码**：[Meta/MetaStatsModel.cs:29-48](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStatsModel.cs#L29)；[Meta/MetaStatDef.cs:13](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStatDef.cs#L13)；[Meta/MetaStat.cs:49-64](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStat.cs#L49)；[Meta/MetaStat.cs:171](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStat.cs#L171)

**状态依据**：Populate is native-annotated (0x1487e78), but the numbers themselves are authored data in Meta Stats Config.asset.

**配置参数**：`Meta Stats Config.Stats[].Key`；`Meta Stats Config.Stats[].BaseValue`；`Meta Stats Config.Stats[].Format`；`Meta Stats Config.Stats[].ShowInCalculator`

**使用方**：meta.metastat_value

**备注**：58 Key/BaseValue pairs in Meta Stats Config.asset, with no duplicate keys. Duplicate config keys would be skipped (first wins). For duplicate saved keys, the last one wins. crystal_slots is in the asset (base 0) but is not a MetaStatKeys constant and no meta-stat reader uses it. The 'crystal_slots' code reads are the VIP feature of the same name (CrystalModel.cs:719). hammer_regen (written by amulet node graph_hammer_regen_3_3) and crystal_enchantment_unlock are MetaStatKeys constants with base 0.

**依赖图节点**：输入 `config:MetaStatsConfig.Stats[].Key`、`config:MetaStatsConfig.Stats[].BaseValue`；输出 `value:player.meta_stat_base`

**核验记录**（已写入上方字段）：

- 采纳 `notes / params Stats[].Key`（来自 `meta.metastat_base_values`）：Re-parsed the asset: 58 pairs, and the non-zero bases are exactly stat_drop, items_rarity_timer, crystal_enchant_efficiency, crystal_max_enchant_stones and chest_level = 1. The output, variables and notes now say 58. One detail was corrected in the process: hammer_regen and crystal_enchantment_unlock ARE MetaStatKeys constants (MetaStatKeys.cs:53,31). Only crystal_slots is absent from MetaStatKeys.

### `meta.metastats_fill`

**MetaStatsConfig.Fill editor generator**

- 状态：**编辑器工具缺失**（`editor_only_missing`）
- 类型：`editor_generator`
- 来源：子系统梳理

```text
(empty) — originally populated Stats list from MetaStatKeys
```

**输出**：Meta Stats Config.asset Stats list

**代码**：[Meta/MetaStatsConfig.cs:11-13](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStatsConfig.cs#L11)

**状态依据**：Private Fill() has an empty body. Only the asset output survives.

**配置参数**：`Meta Stats Config.Stats`

**备注**：UpgradeStyleConfig.Fill (Meta/Upgrades/UpgradeStyleConfig.cs:22) is likewise empty, but it is icon-only.

**依赖图节点**：输入 ；输出 `config:MetaStatsConfig.Stats`、`config:MetaStatsConfig.Stats[].Key`

### `meta.spawner_stat_drop_multiplier`

**Spawner stat-drop multiplier (stat_drop)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x1488e3c (GetMultiplier); 0x1485ef4 (MetaStat.CalculateValue); 0x1487008 (MatchesParam)`

```text
M_stat(spawner) = stat_drop.GetValue(param), param = (IsEvent && EventDef != null && EventDef.UnitType == Mob) ? -1 : LocationNumber. With MetaStat.CalculateValue and modifiers matching (Param == 0 || Param == param): M_stat = Π PercentMult * (1 + Σ PercentAdditive) * (BaseValue + Σ Flat) * Π_source(1 + Σ PAPM). With current writers: M_stat = (1 + Σ_crystal StatDropBonus/100) * (1 + Σ_{amulet Flat at Param == param} v)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `LocationNumber` | spawner island | EnemySpawnerConfig.LocationNumber (Scenes/Main.unity) |
| `BaseValue` | stat_drop base = 1 | Meta Stats Config.Stats[stat_drop].BaseValue |
| `flat` | amulet Flat nodes on stat_drop at Params 1..14 and -1 (no Param 0). Island 1: 'graph_map_spawner_drop_island1_2_4', max level 5, cost soul.common = L^5 (Native_15b677c), value float32(L*0.2f) (Native_15b67d8), so up to +1.0. The event golem node graph_event_golem_extra_range_4_47 gives constant +1.0 at Param -1 | Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:201-218, 637-651, 861, 1093-1107, 1621-1656, 2060-2062 |
| `crystal StatDropBonus` | legendary crystal ability, PercentAdditive at Param 0 (applies to every param), value = LegendaryAbilityValue/100 | Crystals/CrystalModel.cs:1197-1212 |

**输出**：multiplier on all StatRewards of a spawner (and on quest step stat rewards by step location)

**代码**：[Meta/SpawnerStatDropAmountCalculator.cs:9-14](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SpawnerStatDropAmountCalculator.cs#L9)；[Meta/MetaStat.cs:151-174](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStat.cs#L151)；[Meta/MetaStat.cs:182-188](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStat.cs#L182)；[Crystals/CrystalModel.cs:1197-1212](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Crystals/CrystalModel.cs#L1197)；[Quests/Commands/ClaimQuestStepRewardCommandHandler.cs:115-124](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/Commands/ClaimQuestStepRewardCommandHandler.cs#L115)；[UI/Quests/QuestItemView.cs:154](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/Quests/QuestItemView.cs#L154)

**状态依据**：SpawnerStatDropAmountCalculator.GetMultiplier has the comment 'Original bounded ARM64 ELF VA 0x1488e3c.'

**配置参数**：`Meta Stats Config.Stats[stat_drop].BaseValue=1`；`EnemySpawnerConfig.LocationNumber`；`EnemySpawnerConfig.IsEvent`；`EnemySpawnerConfig.EventDef`；`Amulet graph_map_spawner_drop_* node values/costs`；`Crystal legendary StatDropBonus value`

**使用方**：meta.devour_stat_reward_scaling；[Quests/Commands/ClaimQuestStepRewardCommandHandler.cs:120](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/Commands/ClaimQuestStepRewardCommandHandler.cs#L120) (quest stat reward = stat.Value * (float)stat_drop[step.LocationNumber]; method tagged 'candidate only' native 0x143afdc)

**备注**：Island-1 upgrade cost is soul.common = level^5 (Native_15b677c). Event MiniBoss/Boss spawners use their LocationNumber; only event Mob (golem) spawners use Param -1. SimulationSystem RPM uses raw StatRewards[].Value (SimulationSystem.cs:409, 761-765) without visibly applying stat_drop; see open questions.

**依赖图节点**：输入 `config:MetaStatsConfig.Stats[].BaseValue`、`value:player.upgrade_meta_modifier`、`value:player.meta_modifier_applicable`、`config:CrystalConfig.LegendaryStatDropBonus`、`config:CrystalConfig.LegendaryStatDropBonusPerEnchant`、`state:crystal_equipment`、`config:EnemySpawnerConfig.LocationNumber`、`config:EnemySpawnerConfig.IsEvent`、`config:EnemySpawnerConfig.EventDef.UnitType`；输出 `value:spawner.stat_drop_multiplier`

**核验记录**（已写入上方字段）：

- 采纳 `variables.flat.source`（来自 `meta.spawner_stat_drop_multiplier`）：The key name, cost L^5, value float32(L*0.2f) and the Param -1 constant are confirmed at AmuletUpgradeDefsBuilder.cs:201-204, 861 and 2060-2062. The quest consumer is tagged 'candidate only' 0x143afdc. The correction's Param list was incomplete: grep finds stat_drop nodes at Params 1-14 and -1. The variable now says this. A crystal PercentAdditive writer was also added.

### `meta.devour_stat_reward_scaling`

**Devour stat reward per StatRewards entry**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 合并自：`meta.devour_stat_reward_scaling`、`spawners.stat_reward_grant`
- 原生地址：`0x1488e3c (callee SpawnerStatDropAmountCalculator.GetMultiplier); 0x1456084 (callee PlayerRewardSystem.GrantStatReward)`

```text
rewards = (unit != null && unit.StatRewards != null) ? unit.StatRewards : spawner.Config.StatRewards; n = (unit != null) ? unit.SpawnerStatRewardMultiplier : 1; for each r in rewards: copy.Value = r.Value (float); copy.Value *= (float) M_stat(spawner); if n > 1: copy.Value *= n; PlayerRewardSystem.GrantStatReward(copy, displayOverride = -1, displayMultiplier = (n > 1 ? n : 1)). M_stat = stat_drop.GetValue(param), param = (IsEvent && EventDef != null && EventDef.UnitType == Mob) ? -1 : LocationNumber
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `r.Value` | authored stat reward per devour (per-unit copy of config, may be extended at spawn) | EnemySpawnerConfig.StatRewards[].Value (Scenes/Main.unity), copied in EnemyUnit.Initialize (EnemyUnit.cs:147); event plan override; crystal event mobs get an extra entry max(1, RPM*0.4) (EnemySpawner.cs:979-993) |
| `M_stat` | stat_drop multiplier keyed by location (or -1 for event golems) | meta.spawner_stat_drop_multiplier (Meta/SpawnerStatDropAmountCalculator.cs:9-14) |
| `n` | SpawnerStatRewardMultiplier (kill multiplier) | spawners.zero_cooldown_collapse (non-item units) or meta.item_drop_copies_per_kill copies (item units); reset to 1 in EnemyUnit.Initialize |

**输出**：StatModifier (same StatType/ModifierType/Source/StringParam as the authored reward) passed to PlayerRewardSystem.GrantStatReward, which applies the multipliers in meta.devour_stat_grant

**代码**：[Enemy/Commands/GrantSpawnerStatRewardCommandHandler.cs:17-37](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerStatRewardCommandHandler.cs#L17)；[Meta/SpawnerStatDropAmountCalculator.cs:9-14](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SpawnerStatDropAmountCalculator.cs#L9)；[Enemy/EnemySpawner.cs:1359-1383](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1359)；[Enemy/EnemySpawner.cs:1186](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1186)；[Enemy/EnemySpawner.cs:1214-1215](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1214)；[Enemy/EnemySpawner.cs:979-993](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L979)；[Enemy/EnemyUnit.cs:133](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L133)；[Enemy/EnemyUnit.cs:146-147](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L146)；[Player/Systems/PlayerRewardSystem.cs:49](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/PlayerRewardSystem.cs#L49)

**状态依据**：GrantSpawnerStatRewardCommandHandler.Execute has no native annotation. Its callees SpawnerStatDropAmountCalculator (0x1488e3c) and PlayerRewardSystem.GrantStatReward (0x1456084) are native-annotated.

**配置参数**：`EnemySpawnerConfig.StatRewards[] (StatType, ModifierType, Value, Source, Name)`；`Meta Stats Config.stat_drop`；`EnemySpawnerConfig.LocationNumber`；`EnemySpawnerConfig.IsEvent`；`EnemySpawnerConfig.EventDef`

**使用方**：meta.devour_stat_grant；stat aggregation (Unit/Stat)；Simulation RPM (SimulationSystem.GetResourceValue uses StatRewards[0].Value)

**备注**：Fires on devour (HandleDevour, after the OnDevour ability event), not on death. Modifier type and source come from the authored reward. The display shows the per-kill value (Value / n) with an 'xn' suffix. Rewards with Source Enemy and an empty or 'Devoured' name are renamed to map.tree (IsStatic), map.elite (IsEliteBoss) or map.normal_mob. The product is computed in float (copy.Value is float, and M_stat is cast to float). Scene: 577 spawners each have exactly 1 reward, all ModifierType 0 (Flat) and Source 0 (Enemy). Stat types by count: Health(1) 138, HealthRegen(2) 81, BuckDmgFlat(12) 66, CutDmgFlat(8) 54, StabDmgFlat(6) 54, Buck/CutDefFlat(20/16) 34 each, StabDefFlat(14) 28, MoveSpeed 19, Crit 17, CritDmg 16, Evasion 14, Block 8+8, Lifesteal 6. Value range grows with location, from 0.5 to 500 at L1 up to 1e6 at L17. The downstream cheat, DoubleLoot and VIP multipliers belong to meta.devour_stat_grant. EnemyUnit.Initialize does not copy StringParam into the per-unit StatRewards.

**依赖图节点**：输入 `value:enemy.stat_rewards`、`config:EnemySpawnerConfig.StatRewards[].Value`、`config:EnemySpawnerConfig.StatRewards[].StatType`、`config:EnemySpawnerConfig.StatRewards[].ModifierType`、`config:EnemySpawnerConfig.StatRewards[].Source`、`value:spawner.stat_drop_multiplier`、`value:spawner.kill_reward_multiplier`；输出 `value:reward.devour_stat_modifier`

**核验记录**（已写入上方字段）：

- 采纳 `code_refs`（来自 `meta.devour_stat_reward_scaling`）：Confirmed by grep: the property is at EnemyUnit.cs:133 and the reset to 1 at :146. Line 147 is kept because it is the StatRewards per-unit copy. EnemySpawner.cs:1186 (zero-cooldown set) and 1214-1215 (overwrite by item copies) were added.

### `meta.devour_stat_grant`

**PlayerRewardSystem.GrantStatReward multipliers**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x1456084; predicates 0x145704c/0x1457078/0x1457038; 0x15c4bf4 (Roll)`

```text
v' = v * cheat.DropMultiplier; if Source == Enemy: for each equipped item, for each ability state with DoubleLoot == true: DoubleLoot = false; v' *= (1 + ability.GetValue()) (product over all flagged abilities); if Source == Enemy and the VIP 'stat_multiplier' feature exists: level = GetLevelConfig(vipLevel); with Random.Range(0,1) < level.Chance: v' *= level.Multiplier; stat.AddModifier(v'); cashback.AddEarnedStat(v'); if ModifierType ∉ {PercentAdditive, PercentMultiplicative, PreAdditivePostMultiplicative, PostFormulaMultiplicative} && StatType == Health && !Unit.IsDying: CurrentHealth += v'
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `v` | incoming modifier value | meta.devour_stat_reward_scaling, meta.rewarded_ad_stat_value, quests, abilities |
| `cheat.DropMultiplier` | debug int multiplier (1 by default) | Services/Cheats/CheatsModel.cs:28,50 |
| `ability.GetValue()` | double-loot ability bonus | item ability states (abilities subsystem) |
| `level.Chance, level.Multiplier` | VIP stat multiplier roll | VIP StatMultiplierConfig.LevelConfigs[vipLevel] |

**输出**：permanent player stat modifier (merged per meta.modifier_merge_accumulation), the cashback ledger entry, and an immediate heal for non-percent Health rewards

**代码**：[Player/Systems/PlayerRewardSystem.cs:49-84](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/PlayerRewardSystem.cs#L49)；[Utilities/DoubleExtensions.cs:67-71](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Utilities/DoubleExtensions.cs#L67)

**状态依据**：Comment 'Original ELF VA 0x1456084 and three bounded predicate bodies 0x145704c/78/38'.

**配置参数**：`VIP StatMultiplierConfig.LevelConfigs[].Chance`；`VIP StatMultiplierConfig.LevelConfigs[].Multiplier`

**使用方**：meta.devour_stat_reward_scaling；meta.rewarded_ad_stat_value；Quests ClaimQuestStepReward；Abilities AddStatEffect / GrantRandomStatPerDistanceEffect；AdView no_ads grant

**备注**：The VIP and DoubleLoot bonuses apply only to Source==Enemy, so quest, ads and reward grants are excluded. The heal-on-grant covers Flat, PostFormulaAdditive, PostFormulaFlat and EventPercentModifier Health rewards. Percent types are displayed x100 and never heal. The display value defaults to v'/displayMultiplier.

**依赖图节点**：输入 `value:reward.devour_stat_modifier`、`value:ads.rewarded_stat_value`、`value:reward.quest_stat_reward_scaled`、`state:cheats`、`value:ability.double_loot_multiplier`、`state:vip_level`、`config:VipConfig.Features`、`state:rng`、`value:player.current_hp`、`value:unit.is_dead`；输出 `value:reward.granted_stat_modifier`、`value:player.current_hp`

**核验记录**（已写入上方字段）：

- 采纳 `expression`（来自 `meta.devour_stat_grant`）：PlayerRewardSystem.cs:76-79 heals when the type is not in the four percent types (so PostFormulaAdditive/Flat/EventPercent included), StatType==Health and !IsDying. Lines 57-63 loop over every equipped item's DoubleLoot abilities, multiplying (1+GetValue()) and clearing each flag. The expression now states both.

### `meta.soul_drop_per_kill`

**Souls per devour (calculator)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x1488810`

```text
souls = (max(1, 1 + soul_drop_bonus[0]) + souls_rarity_amount[soulKey]) * rewardMultiplier
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `soul_drop_bonus` | global % soul bonus (Format Percent) | MetaStat soul_drop_bonus, base 0 |
| `souls_rarity_amount[soulKey]` | flat extra souls per kill for the soul type (common/large/elite/huge) | amulet graph 'graph_souls_drop_<type>_amount' (Flat, string param) |
| `rewardMultiplier` | CurrentDevourRewardMultiplier (n) | spawners.zero_cooldown_collapse |

**输出**：double souls added to the amulet soul balance

**代码**：[Meta/SoulDropAmountCalculator.cs:7-8](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SoulDropAmountCalculator.cs#L7)；[Meta/SoulDropAmountCalculator.cs:12-13](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SoulDropAmountCalculator.cs#L12)

**状态依据**：Comment 'Original ELF VA 0x1488810; per-kill clamps only the multiplier, not the rarity bonus.'

**配置参数**：`Meta Stats Config.Stats[soul_drop_bonus]`；`Meta Stats Config.Stats[souls_rarity_amount]`；`EnemySpawner.SoulReward`

**使用方**：meta.soul_grant_on_devour

**备注**：The rarity bonus is additive to the multiplier, not multiplied by it. No amulet node was found writing soul_drop_bonus, so it is effectively 0 and the per-kill base is 1.

**依赖图节点**：输入 `value:player.meta_stat[soul_drop_bonus]`、`value:player.meta_stat[souls_rarity_amount]`、`config:EnemySpawnerConfig.SoulReward`、`value:spawner.kill_reward_multiplier`、`constant:SoulDropAmountCalculator.min_drop_multiplier`；输出 `value:reward.souls_per_devour`

### `meta.soul_grant_on_devour`

**Soul grant on devour (gate, cheat, Soul Pass double)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 合并自：`meta.soul_grant_on_devour`、`spawners.soul_reward_grant`
- 原生地址：`0x1488810 (callee SoulDropAmountCalculator.CalculatePerKill); 0x15ba79c (callee IsSoulsDropUnlocked)`

```text
key = spawner.Config.SoulReward; if IsNullOrWhiteSpace(key) or !IsSoulsDropUnlocked(config): no grant. unlocked = SoulAlwaysAvailable || (IsStatic ? feature_unlock.GetValue('static_souls_drop') : souls_drop_unlock.GetValue(key)) > 0. a = souls(key, CurrentDevourRewardMultiplier) * cheat.DropMultiplier, where souls = meta.soul_drop_per_kill = (max(1, 1 + soul_drop_bonus[0]) + souls_rarity_amount[key]) * CurrentDevourRewardMultiplier; doubled = SoulPassPurchased && Random.value < PassDoubleSoulChance; if doubled: a = a + a; AmuletModel.AddSouls(key, a)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `souls` | per-kill souls | meta.soul_drop_per_kill (Meta/SoulDropAmountCalculator.cs:7-8, native 0x1488810) |
| `CurrentDevourRewardMultiplier` | = unit.SpawnerStatRewardMultiplier during HandleDevour (reset to 1 after) | Enemy/EnemySpawner.cs:1362,1380; spawners.zero_cooldown_collapse / meta.item_drop_copies_per_kill |
| `key` | soul rarity key (common/large/elite/huge/great/chaotic) | EnemySpawnerConfig.SoulReward |
| `unlock` | IsStatic ? feature_unlock['static_souls_drop'] : souls_drop_unlock[key], bypassed by SoulAlwaysAvailable | Amulet/Upgrades/AmuletUpgradesModel.cs:142-151 |
| `cheat.DropMultiplier` | debug int multiplier (1 by default) | Services/Cheats/CheatsModel.cs:28,50 |
| `PassDoubleSoulChance` | Soul Pass double chance | Amulet Config.asset PassDoubleSoulChance = 0.25 |

**输出**：soul currency (soul.<key>) gained in AmuletModel

**代码**：[Enemy/Commands/GrantSpawnerSoulRewardCommandHandler.cs:29-40](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerSoulRewardCommandHandler.cs#L29)；[Amulet/Upgrades/AmuletUpgradesModel.cs:137-151](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradesModel.cs#L137)；[Meta/SoulDropAmountCalculator.cs:7-13](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SoulDropAmountCalculator.cs#L7)；[Enemy/EnemySpawner.cs:1362](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1362)；[Enemy/EnemySpawner.cs:1378-1380](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1378)

**状态依据**：GrantSpawnerSoulRewardCommandHandler.Execute has no native annotation. The calculator (0x1488810) and the unlock gate (0x15ba79c) it calls are native-annotated.

**配置参数**：`EnemySpawnerConfig.SoulReward`；`EnemySpawnerConfig.SoulAlwaysAvailable`；`EnemySpawnerConfig.IsStatic`；`Amulet Config.PassDoubleSoulChance`；`Meta Stats Config.soul_drop_bonus`；`Meta Stats Config.souls_rarity_amount`；`Meta Stats Config.souls_drop_unlock`；`Meta Stats Config.feature_unlock[static_souls_drop]`

**使用方**：Amulet upgrade purchases (soul.* costs)；amulet progression

**备注**：The soul key comes from spawner.Config.SoulReward, not EnemyUnit.SoulReward. The unlock result is cached per config and invalidated by the MetaStat ModifierVersion. Doubling uses UnityEngine.Random.value. A first 'huge' soul while the amulet is locked and EmptyAmuletCount > 0 opens the AmuletSoulHarvestedView popup (no numeric effect). Main.unity spawners by SoulReward: common 459, large 183, elite 33, empty 28, huge 12, great 1, chaotic 1. The simulation uses the unclamped CalculateForKillCount (no max(1, ...)), which belongs to meta.soul_drop_per_kill.

**依赖图节点**：输入 `value:reward.souls_per_devour`、`config:EnemySpawnerConfig.SoulReward`、`value:spawner.souls_drop_unlocked`、`state:cheats`、`state:iap_purchases`、`config:AmuletConfig.PassDoubleSoulChance`、`state:rng`；输出 `value:reward.souls_granted`、`value:economy.currency_balance`

### `meta.soul_drop_for_kill_count`

**Souls for a forecast kill count (simulation soul RPM)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`simulation`
- 来源：合并核验
- 原生地址：`0x14889a4 (CalculateForKillCount); simulation context 0x13e6dd0, 0x13e8424, 0x13e86b8, 0x13e83ec`

```text
souls(K) = ((1 + soul_drop_bonus.GetValue(0)) + souls_rarity_amount.GetValue(soulKey)) * K   (no max(1,·) clamp, unlike CalculatePerKill). Simulation call: K = [Σ over spawnerData where SoulReward == soulKey ∧ forecastHealth ≥ -healthDelta of (10 / (fightTime + 10)) * chargesLimit] / 60, with chargesLimit = Config.DisableAfterFirstClear ? Config.Count : ChargesTotal. Then soulRPM = souls(K); if soulRPM > 0: soulRPM = max(0.03, soulRPM); if a previous SoulRPMCalculatedValue[soulKey] exists: soulRPM = max(soulRPM, previous), so the value only grows. Evaluated for every distinct non-blank Config.SoulReward key.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `K` | forecast kills per minute for spawners rewarding soulKey | Simulation/Systems/SimulationSystem.cs:475-485 |
| `soul_drop_bonus` | meta stat, value at param 0; the multiplier is 1 + this | Meta/SoulDropAmountCalculator.cs:12 |
| `souls_rarity_amount[soulKey]` | meta stat flat rarity bonus per soul key | Meta/SoulDropAmountCalculator.cs:13 |
| `fightTime` | forecast fight time for the spawner | SimulationSystem spawnerData.fightTime |
| `healthDelta` | forecast health requirement for the spawner (it is skipped when forecastHealth < -healthDelta) | Simulation/Systems/SimulationSystem.cs:479 |
| `chargesLimit` | DisableAfterFirstClear ? Count : ChargesTotal | Simulation/Systems/SimulationSystem.cs:608-612 |

**输出**：soul RPM per soul key (SimulationSystem.SoulRPMCalculatedValue[soulKey])

**代码**：[Meta/SoulDropAmountCalculator.cs:9-13](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SoulDropAmountCalculator.cs#L9)；[Simulation/Systems/SimulationSystem.cs:463-470](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L463)；[Simulation/Systems/SimulationSystem.cs:472-486](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L472)；[Simulation/Systems/SimulationSystem.cs:488-495](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L488)；[Simulation/Systems/SimulationSystem.cs:608-612](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L608)

**状态依据**：Meta/SoulDropAmountCalculator.cs:9 comment 'Original ELF VA 0x14889a4 deliberately has no per-kill clamp.' on CalculateForKillCount; the calling simulation methods carry 'Original ELF VA 0x13e8424', '0x13e86b8', '0x13e83ec'.

**配置参数**：`Meta Stats Config.Stats[soul_drop_bonus]`；`Meta Stats Config.Stats[souls_rarity_amount]`；`EnemySpawnerConfig.SoulReward`；`EnemySpawnerConfig.DisableAfterFirstClear`；`EnemySpawnerConfig.Count`

**使用方**：simulation.soul_rpm

**备注**：Ignores the Soul Pass double chance and the per-kill n burst. Compare with CalculatePerKill (VA 0x1488810, line 7-8): (max(1, 1 + soul_drop_bonus) + rarity) * rewardMultiplier, which clamps the multiplier only. The 0.03 floor and the monotonic max belong to SetSoulRPMValue (simulation side) and are included here for the full chain.

**依赖图节点**：输入 `value:player.meta_stat[soul_drop_bonus]`、`value:player.meta_stat[souls_rarity_amount]`、`config:EnemySpawnerConfig.SoulReward`、`value:sim.spawner_fight_time`、`value:sim.spawner_health_delta`、`value:player.stat_raw[Health]`、`value:spawner.charges_limit_per_day`、`constant:SimulationSystem.soul_kill_time_pad_seconds`、`constant:SimulationSystem.soul_rpm_floor`；输出 `value:sim.soul_rpm`

**核验记录**（已写入上方字段）：

- 采纳 `expression`（来自 `meta.soul_drop_for_kill_count`）：Verified at SimulationSystem.cs:475-485 (health gate, SoulReward filter, chargesLimit via 608-612, /60) and 488-495 (positive-only max(0.03) floor, monotonic max with the previous value). Folded into the expression and code_refs.

### `meta.item_drop_copies_per_kill`

**Item copies per kill (item spawners)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 合并自：`meta.item_drop_copies_per_kill`、`spawners.item_drop_copies`
- 原生地址：`0x146639c (callee RarityStatParam.From only; ItemDropAmountCalculator and HandleDeath carry no annotation)`

```text
perKill = (item == null) ? 0 : 1 + (int) items_rarity_drop_amount.GetValue(RarityStatParam.From(item.Rarity)) where From(r) = (int)r + 1 and (int) truncates toward zero. item = enemy.ItemReward ?? config.ItemReward. If IsChargesEnabled (= !config.IsEvent && effectiveCharges > 0): copies = min(perKill, RemainingCharges + 1); additionalCharges = max(0, copies - 1); RemainingCharges -= additionalCharges. Else copies = perKill, additionalCharges = 0. If copies > 0: GrantSpawnerItemRewardCommand(Copies = copies) -> PlayerModel.CreateItem(item, copies) (the handler falls back to CalculatePerKill when Copies < 1), and afterwards enemy.SpawnerStatRewardMultiplier = copies. ReduceGeodeTimerOnKill.KillCount = additionalCharges + SpawnerStatRewardMultiplier, evaluated before that overwrite (the pre-overwrite value is 1 for item units)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `item.Rarity` | item reward rarity (0=Common..6=Mythical) | EnemyUnit.ItemReward ?? EnemySpawnerConfig.ItemReward (item ScriptableObject, e.g. 'cut common.asset') |
| `items_rarity_drop_amount[p]` | meta flat bonus copies at rarity param p = rarity+1 (base 0) | Meta Stats Config.asset; amulet nodes graph_items_drop_<rarity>_* (Flat) with Params 1..4 (Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:79,98,180,306,331,435,617,759,1153,1490) |
| `RemainingCharges` | charges left before the big respawn | EnemySpawner.RemainingCharges; total = ChargesBeforeBigRespawn + items_rarity_charges bonus (Meta/SpawnerChargesCalculator.cs:23-41) |
| `IsChargesEnabled` | !config.IsEvent && GetEffectiveChargesBeforeBigRespawn() > 0 | Enemy/EnemySpawner.cs:307-312 |

**输出**：ItemModel copies added via PlayerModel.CreateItem(item, copies); also sets enemy.SpawnerStatRewardMultiplier = copies, which later scales devour stat and soul rewards

**代码**：[Meta/ItemDropAmountCalculator.cs:8-25](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/ItemDropAmountCalculator.cs#L8)；[Enemy/EnemySpawner.cs:1191-1215](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1191)；[Enemy/EnemySpawner.cs:307-312](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L307)；[Enemy/Commands/GrantSpawnerItemRewardCommandHandler.cs:26-30](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerItemRewardCommandHandler.cs#L26)；[Player/Inventory/RarityStatParam.cs:5-9](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/RarityStatParam.cs#L5)；[Meta/SpawnerChargesCalculator.cs:23-41](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SpawnerChargesCalculator.cs#L23)

**状态依据**：ItemDropAmountCalculator.GetFlatBonus/CalculatePerKill/ResolveKillDropCopies and the HandleDeath block have no native annotation. Only the callee RarityStatParam.From is annotated (0x146639c).

**配置参数**：`Meta Stats Config.Stats[items_rarity_drop_amount]`；`Meta Stats Config.Stats[items_rarity_charges]`；`EnemySpawnerConfig.ItemReward`；`EnemySpawnerConfig.ChargesBeforeBigRespawn`

**使用方**：spawners.zero_cooldown_collapse；meta.devour_stat_reward_scaling (via SpawnerStatRewardMultiplier = copies)；meta.soul_grant_on_devour (via CurrentDevourRewardMultiplier)；items/evolution economy (shard/copy supply)

**备注**：Param = RarityStatParam.From(rarity) = rarity+1 (native 0x146639c). Extra copies consume extra spawner charges, so the total copies per big cycle stay at most chargesTotal (+1 for the last kill's base copy). The meta bonus only speeds up collection, unless items_rarity_charges also raises chargesTotal. SpawnerStatRewardMultiplier = copies is set only when copies > 0, and it then also multiplies any StatRewards/SoulReward on that item unit at devour. For item units the zero-cooldown burst (ConsumeZeroCooldownStatRewardMultiplier) always returns 1 because it requires unit.ItemReward == null (EnemySpawner.cs:724). With charges disabled, ReduceGeodeTimerOnKill gets KillCount 1 even when copies > 1. CalculateForKillCount (= perKill * killCount) exists but has no caller in the reconstruction. Edge case (verifier's scene read): the BossTower item spawner has base charges 0, so charges are disabled (unlimited respawn every cooldown) until items_rarity_charges gives a bonus > 0. At that point it becomes charge-limited to just the bonus. Item spawners never use the AB spawner-charges override (CanUseSpawnerCooldownsAb requires ItemReward == null, EnemySpawner.cs:717-719).

**依赖图节点**：输入 `config:EnemySpawnerConfig.ItemReward`、`config:ItemConfig.Rarity`、`value:item.rarity_param`、`value:player.meta_stat[items_rarity_drop_amount]`、`constant:ItemDropAmountCalculator.base_copies_per_kill`、`value:spawner.remaining_charges`、`value:spawner.charges_total`、`config:EnemySpawnerConfig.IsEvent`；输出 `value:reward.item_copies_per_kill`、`value:spawner.extra_charges_consumed`、`value:spawner.remaining_charges`、`value:spawner.kill_reward_multiplier`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `spawners.item_drop_copies`）：Verified against EnemySpawner.cs:1204-1215 (multiplier overwrite only when copies>0), GrantSpawnerItemRewardCommandHandler.cs:29 (fallback when Copies<1), EnemySpawner.cs:307-312 and SpawnerChargesCalculator.cs:40-41. The expression now includes the fallback and the overwrite, and the notes include the stat/soul scaling and the BossTower edge case.

### `meta.item_drop_for_kill_count`

**Item copies for a kill count (forecast)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`simulation`
- 来源：子系统梳理

```text
copies(K) = (1 + (int) items_rarity_drop_amount[rarity+1]) * K
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `K` | kill count | caller |

**输出**：forecast copies

**代码**：[Meta/ItemDropAmountCalculator.cs:27-30](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/ItemDropAmountCalculator.cs#L27)

**状态依据**：No annotation.

**配置参数**：`Meta Stats Config.Stats[items_rarity_drop_amount]`

**使用方**：simulation / UI forecast (no call site found in Simulation)

**备注**：Unlike the runtime path, there is no clamp to remaining charges.

**依赖图节点**：输入 `value:player.meta_stat[items_rarity_drop_amount]`、`value:item.rarity_param`、`constant:ItemDropAmountCalculator.base_copies_per_kill`、`state:sim_bot_state`；输出 `value:sim.item_copies_for_kills`

### `meta.spawner_currency_reward`

**Spawner flat currency reward**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 合并自：`meta.spawner_currency_reward`、`spawners.currency_reward`

```text
key = Config.CurrencyReward; if !IsNullOrWhiteSpace(key) && CurrencyRewardAmount > 0: CurrencyModel.Add(key, CurrencyRewardAmount) (per devour; no multiplier). If key == 'empty_amulet' and the previous EmptyAmuletCount <= 0, the AmuletUnlockView popup opens
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `CurrencyRewardAmount` | flat amount per devour (default 1.0) | EnemySpawnerConfig.CurrencyRewardAmount |
| `CurrencyReward` | currency key | EnemySpawnerConfig.CurrencyReward |

**输出**：currency balance

**代码**：[Enemy/Commands/GrantSpawnerCurrencyRewardCommandHandler.cs:15-28](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerCurrencyRewardCommandHandler.cs#L15)；[Enemy/EnemySpawner.cs:1379](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1379)

**状态依据**：No native annotation in GrantSpawnerCurrencyRewardCommandHandler.

**配置参数**：`EnemySpawnerConfig.CurrencyReward`；`EnemySpawnerConfig.CurrencyRewardAmount`

**使用方**：amulet unlock

**备注**：Only one spawner in Main.unity uses it: the L4 dragon (DisableAfterFirstClear) gives empty_amulet x1, which unlocks the amulet. No meta, cheat, devour (CurrentDevourRewardMultiplier) or charge-burst multiplier applies.

**依赖图节点**：输入 `config:EnemySpawnerConfig.CurrencyReward`、`config:EnemySpawnerConfig.CurrencyRewardAmount`、`state:player_actions`；输出 `value:economy.currency_balance`

### `meta.event_plan_stat_reward`

**Event spawner stat reward override from event plan**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x150639c`

```text
spawner.StatRewards := [{StatType = rewardInfo.RewardStatType, Value = rewardInfo.RewardValue, Flat, Source Enemy, Name 'event.<key>.<unitType>'}]
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `RewardValue` | event forecast reward | EventsRepository.GetEventRewardInfo (events subsystem) |

**输出**：event spawner StatRewards

**代码**：[Enemy/EnemySpawner.cs:1140-1169](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1140)

**状态依据**：Comment 'Original APK ARM64 ELF VA 0x150639c'.

**使用方**：meta.devour_stat_reward_scaling

**备注**：The values come from the events/forecast subsystem.

**依赖图节点**：输入 `value:forecast.event_plan`、`state:current_event`；输出 `value:enemy.stat_rewards`

### `meta.grant_reward_stat`

**Generic StatReward grant (quests/shop/pass/mail/events)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：合并核验

```text
type = reward.Type; v = reward.Value. Branch (a): if CompletedEvent != null ∧ !CompletedEvent.IsProgressComplete: if BakedData == null ∧ (IsRandom ∨ UseGpm) ∧ SaveKey non-empty ∧ BakedRewardStorage.Has(SaveKey) then BakedData = Load(SaveKey); if BakedData != null then (type, v) = (BakedData.Type, BakedData.Value) else resolveDynamic. Branch (b): else if (IsRandom ∨ UseGpm) ∧ SaveKey non-empty: if a stored bake exists (and is non-null) then (type, v) = (baked.Type, baked.Value), ELSE keep the raw (Type, Value), with no random pick and no RPM scaling. Branch (c): else resolveDynamic. Dynamic: if IsRandom then type = U(randomStats) (8 fixed stats, not RPM-filtered, ignores AllowedStats); if UseGpm then v *= (float)RPMCalculatedValue[type]. Then v *= cheats.DropMultiplier; if v ≤ 0 return. Add StatModifier {StatType = type, Value = v, Param = 0, Name = CustomModifier ? reward.Name : sourcePath, ModifierType = CustomModifier ? reward.ModifierType : Flat, Source = CustomModifier ? reward.Source : (sourceOverride ?? Reward)} to the player stat of that type; cashback.AddEarnedStat(modifier). If stat.Type == Health ∧ ModifierType ∉ {PercentAdditive, PercentMultiplicative, PreAdditivePostMultiplicative, PostFormulaMultiplicative, EventPercentModifier} then CurrentHealth += v.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `randomStats` | fixed list (StatType)1,2,8,6,12,16,14,20 = {Health, HealthRegen, CutDmgFlat, StabDmgFlat, BuckshotDmgFlat, CutDefFlat, StabDefFlat, BuckshotDefFlat} | Rewards/Commands/GrantRewardCommandRunner.cs:69-79 |
| `RPM[type]` | simulation reward-per-minute (indexer, so a missing key throws) | SimulationSystem.RPMCalculatedValue |
| `BakedData / BakedRewardStorage[SaveKey]` | pre-baked (type, value). Main writer is BakeRewardsCommandRunner.BakeStatReward (meta.bake_stat_reward): when IsRandom it picks from AllowedStats, or randomStats when AllowedStats is empty, filtered by RPM > 0 when UseGpm. Its value is max(1, (RPM*Value*multiplier).PrettyRound(2,1,4)) when UseGpm, else Value*multiplier, and the bake is removed when no candidate or RPM exists. EventForecastService.ProcessStatReward also writes a bake, for UseGpm only, as max(0, GPM*Value).PrettyRound(2,1,4) with the daily-rotation/AllowedStats type. ClaimQuestStepRewardCommandHandler sets BakedData in memory for display rewards. | Rewards/Commands/BakeRewardsCommandRunner.cs:53-98; Events/Services/EventForecastService.cs:224-241; Quests/Commands/ClaimQuestStepRewardCommandHandler.cs:130 |
| `DropMultiplier` | cheat drop multiplier | CheatsModel.DropMultiplier |
| `sourceOverride` | optional ModifierSource override of the runner | Rewards/Commands/GrantRewardCommandRunner.cs:102 |

**输出**：permanent player stat modifier (plus a current-health top-up for non-percent Health rewards)

**代码**：[Rewards/Commands/GrantRewardCommandRunner.cs:69-79](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/Commands/GrantRewardCommandRunner.cs#L69)；[Rewards/Commands/GrantRewardCommandRunner.cs:190-253](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/Commands/GrantRewardCommandRunner.cs#L190)；[Rewards/StatReward.cs:11-25](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/StatReward.cs#L11)；[Rewards/Commands/BakeRewardsCommandRunner.cs:53-98](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/Commands/BakeRewardsCommandRunner.cs#L53)；[Events/Services/EventForecastService.cs:236-241](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Events/Services/EventForecastService.cs#L236)；[Economy/CurrencyModel.cs:14](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Economy/CurrencyModel.cs#L14)

**状态依据**：GrantRewardCommandRunner has no native annotations (grep for Original/Native/ELF finds none).

**配置参数**：`StatReward.Type`；`StatReward.Value`；`StatReward.UseGpm`；`StatReward.IsRandom`；`StatReward.SaveKey`；`StatReward.CustomModifier`；`StatReward.ModifierType`；`StatReward.Source`；`StatReward.Name`；`CheatsModel.DropMultiplier`

**使用方**：[Rewards/Flows/GrantRewardsFlowRunner.cs:16-31](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/Flows/GrantRewardsFlowRunner.cs#L16)；shop/pass/vip/event reward lists

**备注**：This path bypasses PlayerRewardSystem, so there is no VIP or DoubleLoot multiplier. In branch (b) a missing bake does not fall back to dynamic resolution (the else-if at 208-219 never sets resolveDynamic), so an unbaked UseGpm reward grants the raw Value (in minutes) as a flat stat, and an unbaked IsRandom reward grants the raw Type. The baked value depends on the writer. BakeRewardsCommandRunner (meta.bake_stat_reward) floors at 1, applies the command multiplier and honours AllowedStats. EventForecastService floors at 0 and bakes UseGpm only. Both PrettyRound the GPM value. Dynamic values are not rounded, and the dynamic random pick ignores AllowedStats. Health top-up also fires for Flat, PostFormulaAdditive and PostFormulaFlat. Other handlers: Currency: CurrencyModel.Add ignores amount ≤ 0 (CurrencyModel.cs:14); SoulReward adds souls; SoulPass grants Amulet Config.PassOneTimeDivineSouls (=1) divine souls; ChaoticSoulOffer adds 1 chaotic soul; Gems +=; VipPoints add to InAppsBoughtValueInRewarded.

**依赖图节点**：输入 `config:VipConfig.Features[].rid`、`config:FirstBuyPackRewardsConfig.Rewards[].rid`、`config:QuestConfig.Chains[].ChainReward[].rid`、`config:SecretChestProvider.serializedData.Rewards[].rid`、`config:LevelPassConfig.Passes[].Tiers[].FreeReward.rid`、`config:LevelPassConfig.Passes[].Tiers[].PaidReward.rid`、`value:forecast.crystal_reward`、`value:reward.baked_stat_reward`、`value:sim.stat_rpm`、`constant:GrantRewardCommandRunner.randomStats`、`state:rng`、`state:cheats`、`state:current_event`；输出 `value:reward.granted_stat_modifier`、`value:player.current_hp`

**核验记录**（已写入上方字段）：

- 采纳 `expression`（来自 `meta.grant_reward_stat`）：Verified at GrantRewardCommandRunner.cs:194-228 (three branches; branch b falls back to the raw Type/Value when no bake exists) and 245-250 (Health top-up excludes the four percent types and EventPercentModifier, so it is not limited to Flat). The expression was rewritten accordingly.
- 合并复核修正：The merge added a BakedData variable and a note saying baked values come only from EventForecastService.ProcessStatReward (max(0, GPM*Value).PrettyRound, UseGpm only). The main bake writer is Rewards/Commands/BakeRewardsCommandRunner.BakeStatReward/CreateBakedStatReward (lines 53-98, catalogued as meta.bake_stat_reward). It bakes IsRandom rewards too: it picks from AllowedStats or the 8 default stats, filtered by RPM > 0 when UseGpm. For UseGpm it stores max(1, (RPM*Value*multiplier).PrettyRound(2,1,4)), with a floor of 1 rather than 0; otherwise it stores Value*multiplier. It removes the bake when no candidate exists. So the claims that baked values are written only by EventForecastService and that every baked value is PrettyRounded are wrong. The expression, the pending correction and the status were checked against GrantRewardCommandRunner.cs:190-253 and are correct.

### `meta.bake_stat_reward`

**Baked GPM/random stat reward value**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x141a824, 0x141ae0c, 0x141b170, 0x141b448`

```text
mult = (cmd.Multiplier == 0) ? 1 : cmd.Multiplier; type = IsRandom ? U(choices filtered by RPM>0 if UseGpm) : Type; v = UseGpm ? max(1, PrettyRound(RPM[type] * Value * mult)) : Value * mult; persisted under SaveKey until ForceBake
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Value` | minutes-of-RPM factor when UseGpm, else absolute | StatReward.Value |
| `mult` | e.g. VIP level for VIP daily chest | VIP/Commands/TryClaimVipDailyChestCommandRunner.cs:46 |
| `choices` | AllowedStats or the default 8-stat list | StatReward.AllowedStats |

**输出**：BakedStatReward {Type, Value}

**代码**：[Rewards/Commands/BakeRewardsCommandRunner.cs:30-41](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/Commands/BakeRewardsCommandRunner.cs#L30)；[Rewards/Commands/BakeRewardsCommandRunner.cs:54-99](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/Commands/BakeRewardsCommandRunner.cs#L54)；[Rewards/BakedRewardStorage.cs:13-30](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/BakedRewardStorage.cs#L13)

**状态依据**：Comments 'Original0x141b170 ...' and 'Native double arithmetic and PrettyRound(2,1,4), then minimum1.'

**配置参数**：`StatReward.Value`；`StatReward.UseGpm`；`StatReward.IsRandom`；`StatReward.AllowedStats`；`StatReward.SaveKey`

**使用方**：meta.grant_reward_stat；roster event rewards；VIP daily chest

**备注**：The value is snapshotted at bake time, so the RPM at that moment fixes the reward.

**依赖图节点**：输入 `config:VipConfig.Features[].rid`、`value:sim.stat_rpm`、`state:vip_level`、`constant:BakeRewardsCommandRunner.default_multiplier`、`state:rng`、`constant:BakeRewardsCommandRunner.randomStats`、`constant:BakeRewardsCommandRunner.min_gpm_value`、`constant:BakeRewardsCommandRunner.pretty_round_args`；输出 `value:reward.baked_stat_reward`

### `meta.pretty_round`

**PrettyRound (designer rounding)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15c4c28`

```text
x < 10 → round_half_even(x); else scale = 10^(floor(log10 x) - d + 1), first = floor(x / (10*scale)), step = first ≤ b1 ? scale : first ≤ b2 ? 5*scale : 10*scale; result = (int)(round_half_even(x / step) * step); defaults d=2, b1=1, b2=4
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `x` | raw value | caller |

**输出**：int rounded value

**代码**：[Utilities/DoubleExtensions.cs:73-83](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Utilities/DoubleExtensions.cs#L73)

**状态依据**：Comment 'Native ELF VA 0x15c4c28 uses midpoint-to-even Math.Round.'

**使用方**：meta.bake_stat_reward；meta.upgrade_cost

**备注**：Returns int, so costs above 2^31 would overflow.

**依赖图节点**：输入 `constant:DoubleExtensions.roundingDigits`、`constant:DoubleExtensions.firstDigitBoundary`、`constant:DoubleExtensions.secondDigitBoundary`、`constant:DoubleExtensions.small_value_threshold`；输出 `value:reward.pretty_rounded_value`

### `meta.daily_rotation_stat`

**Daily rotation stat type**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x1419a2c`

```text
if !DailyRotation: return Type; idx = (UtcDate(timeService.Now) - UtcDate(FromUnixTimeSeconds(installTime))).Days % DailyRotationStats.Count; entry = DailyRotationStats[idx]; return entry.Empty ? null : entry.Type
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `installTime` | user install unix time (seconds) | UserModel.installTime |
| `now` | game time service Now, converted to a UTC date | ITimeService.Now |
| `DailyRotationStats` | authored list of {Empty, Type} slots | Rewards/DailyRotationStat.cs:7-12 |

**输出**：StatType? for a StatReward (null = empty rotation slot)

**代码**：[Rewards/StatReward.cs:16-18](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/StatReward.cs#L16)；[Rewards/StatReward.cs:27-36](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/StatReward.cs#L27)；[Rewards/DailyRotationStat.cs:7-12](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/DailyRotationStat.cs#L7)；[Events/Services/EventForecastService.cs:224-241](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Events/Services/EventForecastService.cs#L224)

**状态依据**：StatReward.cs:29 comment 'Original ARM64 ELF VA 0x1419a2c' on GetResolvedType.

**配置参数**：`StatReward.DailyRotation`；`StatReward.DailyRotationStats[]`

**使用方**：[Events/Services/EventForecastService.cs:226](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Events/Services/EventForecastService.cs#L226) (ProcessStatReward for crystal-event stage rewards; returns early when the rotation slot is Empty)

**备注**：GetResolvedType is called only from EventForecastService.ProcessStatReward. GrantRewardCommandRunner.HandleStatReward and BakeRewardsCommandRunner.CreateBakedStatReward use reward.Type directly. The rotated type reaches the actual grant only for UseGpm rewards, because ProcessStatReward stores BakedData/BakedRewardStorage[SaveKey] = {rotated type, max(0, GPM*Value).PrettyRound(2,1,4)} only when UseGpm (lines 236-240), and the grant then reads that bake. For a non-UseGpm DailyRotation reward the rotated type appears only in the forecast entry (ForecastCrystalRewardEntry.StatType), while the grant uses reward.Type, so the display and the grant can disagree. In ProcessStatReward an IsRandom reward with AllowedStats then overrides the resolved type with U(AllowedStats filtered by GPM > 0 when UseGpm), defaulting to Health. C# % keeps the sign, so a device date before install gives a negative index (exception); an empty list divides by zero.

**依赖图节点**：输入 `config:CrystalEventRosterConfig.Milestones[].Rewards[].rid`、`config:CrystalEventRosterConfig.Stages[].Rewards[].rid`、`state:utc_now`、`state:install_time`；输出 `value:reward.daily_rotation_stat_type`

**核验记录**（已写入上方字段）：

- 采纳 `used_by`（来自 `meta.daily_rotation_stat`）：grep confirms the only caller, EventForecastService.cs:226, which returns at line 227 when the result has no value. used_by and notes were updated.
- 合并复核修正：The merged note says the rotation affects the forecast/bake path and 'the baked Type then reaches the grant'. That holds only for UseGpm rewards. EventForecastService.ProcessStatReward (lines 236-240) writes BakedData/BakedRewardStorage only when UseGpm. A non-UseGpm DailyRotation reward is never baked there, so the grant uses reward.Type, or the raw Type / a random pick per GrantRewardCommandRunner's branches, and the rotated type appears only in the forecast entry. The rotation also never reaches BakeRewardsCommandRunner.CreateBakedStatReward, which reads reward.Type directly. The note overstates the link between the rotation and the grant. Everything else, including the applied used_by correction, matches the code.

### `meta.random_item_reward_pick`

**Random item reward resolution**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x141af2c, 0x141b6f0`

```text
def = U({items : SlotType == r.SlotType ∧ Rarity == r.Rarity}) (baked per SaveKey); grant copies = r.Level
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `r.Level` | passed as copies to PlayerModel.CreateItem(config, copies) | ItemReward.Level (default 1) |

**输出**：item + copies

**代码**：[Rewards/Commands/BakeRewardsCommandRunner.cs:101-115](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/Commands/BakeRewardsCommandRunner.cs#L101)；[Rewards/Commands/GrantRewardCommandRunner.cs:268-293](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/Commands/GrantRewardCommandRunner.cs#L268)；[Rewards/BossRewardResolver.cs:10-38](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/BossRewardResolver.cs#L10)；[Player/Models/PlayerModel.cs:738-747](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L738)

**状态依据**：Bake path annotated 'Original0x141af2c and predicate0x141b6f0'. Grant and BossRewardResolver paths are unannotated.

**配置参数**：`ItemReward.Rarity`；`ItemReward.SlotType`；`ItemReward.Level`；`ItemReward.IsRandom`

**使用方**：boss tower rewards；shop/pass rewards

**备注**：ItemReward.Level is semantically a copy count in the grant path.

**依赖图节点**：输入 `config:BossTowerConfig.Bosses[].Rewards[].rid`、`config:FirstBuyPackRewardsConfig.Rewards[].rid`、`config:VipConfig.Features[].rid`、`config:SecretChestProvider.serializedData.Rewards[].rid`、`value:forecast.crystal_reward`、`config:ItemConfig.Rarity`、`config:ItemConfig.SlotType`、`state:rng`；输出 `value:reward.item_copies_granted`

### `meta.chest_drop_weighted`

**Chest drop (ItemType, Rarity) selection**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
rare chest (royal, royal_armor): rarity = U < clamp01(0.095 + 0.005*chest_level) ? Legendary : Epic, ItemType = Drops[0].ItemType; else P(drop_i) = Weight_i / Σ Weight; item = U({items : Rarity = rarity ∧ SlotType ∈ slots(ItemType)}), Weapon→{Weapon}, Armor→{Helmet, Shield}
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Weight_i` | authored drop weight | Chests Config.asset Chests[].Drops[].Weight |
| `chest_level` | (int) MetaStat chest_level.Value | stats.raw_value_aggregation |

**输出**：one item per chest opened

**代码**：[Shop/Flows/ChestOpenFlowRunner.cs:128-197](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/Flows/ChestOpenFlowRunner.cs#L128)；[Shop/ChestsModel.cs:96-108](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L96)；[Shop/ChestsModel.cs:140-165](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L140)；[Rewards/ChestsConfig.cs:11-22](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/ChestsConfig.cs#L11)

**状态依据**：Chest opening code has no native annotation. ChestsConfig.Get/TryGet are native (0x1417f9c, 0x1418098).

**配置参数**：`Chests Config.Chests[].Drops[].ItemType`；`Chests Config.Chests[].Drops[].Rarity`；`Chests Config.Chests[].Drops[].Weight`

**使用方**：shop/chest queue (ChestReward, GrantRewardsFlow)

**备注**：Asset weights: knightly C50/U30/R20 (weapon), armor C60/U30/R10, royal E85/L15, royal_armor E80/L20, general and crystal empty. The royal weights are overridden at runtime by the chest_level legendary formula and are display-only.

**依赖图节点**：输入 `config:ChestsConfig.Chests[].Drops[].Weight`、`config:ChestsConfig.Chests[].Drops[].Rarity`、`config:ChestsConfig.Chests[].Drops[].ItemType`、`config:ChestsConfig.Chests[].Key`、`value:player.meta_stat[chest_level]`、`constant:ChestsModel.FallbackLevel`、`constant:ChestsModel.EquipmentLegendaryBaseChance`、`constant:ChestsModel.EquipmentLegendaryChancePerLevel`、`constant:ChestsModel.rare_chest_keys`、`config:ItemConfig.Rarity`、`config:ItemConfig.SlotType`、`state:rng`、`state:player_actions`；输出 `value:reward.chest_item_pick`

### `meta.chest_copies`

**Copies per chest**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
c = rare ? L + 4 : 2L + 8; if ItemType == Armor: c = ceil(c / 1.5); L = (int) chest_level
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `L` | chest level = 1 + Σ amulet chest_level flats + VIP level | stats.raw_value_aggregation, meta.chest_level_vip |

**输出**：copies granted per chest item

**代码**：[Shop/ChestsModel.cs:150-159](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L150)；[Shop/Flows/ChestOpenFlowRunner.cs:106](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/Flows/ChestOpenFlowRunner.cs#L106)

**状态依据**：No annotation.

**配置参数**：`Meta Stats Config.Stats[chest_level].BaseValue=1`

**使用方**：meta.chest_drop_weighted

**备注**：Defined in the Shop subsystem. Listed because chest_level is a meta stat.

**依赖图节点**：输入 `value:player.meta_stat[chest_level]`、`constant:ChestsModel.FallbackLevel`、`config:ChestsConfig.Chests[].Key`、`config:ChestsConfig.Chests[].Drops[].ItemType`、`constant:ChestsModel.rare_chest_keys`、`constant:ChestsModel.rare_copies_offset`、`constant:ChestsModel.normal_copies_per_level`、`constant:ChestsModel.normal_copies_offset`、`constant:ChestsModel.ArmorCountDivider`；输出 `value:reward.item_copies_granted`

### `meta.chest_level_vip`

**VIP chest level bonus**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：合并核验

```text
on each change of vipModel.Level: stat = meta chest_level (return if missing); if vipModifier != null: stat.RemoveModifier(vipModifier) (by reference), vipModifier = null; if vipLevel > 0: vipModifier = {Name 'vip_chest_level', Type Flat, Source Vip, Value = vipLevel}; stat.AddModifier(vipModifier). ChestsModel.Level = chest_level stat missing ? 1 : (int)stat.Value
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `vipLevel` | VIP level | VipModel.Level |

**输出**：chest_level meta stat (+vipLevel flat)

**代码**：[Shop/ChestsModel.cs:20](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L20)；[Shop/ChestsModel.cs:33-45](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L33)；[Shop/ChestsModel.cs:89-92](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L89)；[Shop/ChestsModel.cs:116-138](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L116)

**状态依据**：No native annotation anywhere in ChestsModel.cs.

**配置参数**：`Meta Stats Config.Stats[chest_level]`；`VIP level thresholds (VipModel)`

**使用方**：meta.chest_copies；meta.chest_drop_weighted；ChestsModel.EquipmentLegendaryChance / CrystalLegendaryChance via Level ([Shop/ChestsModel.cs:47-71](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L47), 94-102)

**备注**：Reaction registered in Populate (line 91). The modifier is added only when vipLevel > 0. The Level getter truncates the stat value with (int) and falls back to 1 when the stat is missing.

**依赖图节点**：输入 `state:vip_level`；输出 `value:player.meta_stat[chest_level]`

**核验记录**（已写入上方字段）：

- 采纳 `code_refs`（来自 `meta.chest_level_vip`）：ChestsModel.cs:91 (reaction) and 116-138 (ApplyVipModifier) verified, including the vipLevel > 0 guard at 127 and remove-by-reference at 121-125. code_refs now carry line numbers and the expression states the guard.

### `meta.upgrade_cost`

**Upgrade cost per level**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
cost_k(L) = PrettyRound( PrettyRound(f_k(L)) * CostMultiplier ) for each currency k, L = currentLevel + 1 (null if L > MaxLevel)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `f_k(L)` | per-def cost lambda (e.g. crystal_sell_price_bonus: 50*1.362*L^2.4 + 32; amulet island stat_drop: L^5 soul.common) | Crystals/CrystalUpgradeDefsBuilder.cs, Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs |
| `CostMultiplier` | def-level multiplier (default 1) | UpgradeDef.WithCostMultiplier |

**输出**：list of (currencyKey, amount)

**代码**：[Meta/Upgrades/UpgradeCostDef.cs:17-20](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeCostDef.cs#L17)；[Meta/Upgrades/UpgradeDef.cs:214-220](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeDef.cs#L214)；[Meta/Upgrades/UpgradeModel.cs:62-66](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeModel.cs#L62)

**状态依据**：UpgradeDef and UpgradeCostDef have no annotations. UpgradeModel is native (0x148a848..0x148d2d4).

**配置参数**：`UpgradeDef.MaxLevel`；`UpgradeDef.CostMultiplier`

**使用方**：amulet graph upgrades (soul.* currencies)；crystal upgrades (crystal.magic_dust)

**备注**：The concrete curves live in the Amulet (298 defs, 628 native lambdas) and Crystals builders and belong to those subsystems.

**依赖图节点**：输入 `value:player.upgrade_level`、`constant:AmuletUpgradeDefsBuilder.cost_curves`、`constant:CrystalUpgradeDefsBuilder.cost_curves`、`config:AmuletConfig.UpgradeCostMultipliers`、`constant:AmuletUpgradeDefsBuilder.max_level`、`constant:CrystalUpgradeDefsBuilder.max_level`；输出 `value:economy.upgrade_cost`

### `meta.upgrade_modifier_apply`

**Upgrade → meta/player modifier value**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x148a848..0x148d2d4`

```text
for def with L = level ≥ 1: meta modifier value = (float) g(L) (one modifier per def+param, replaced on every ApplyAll, not summed over levels); player stat modifier value = (float)(g_s(L) * ValueMultiplier)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `g(L)` | value lambda (e.g. stat_drop island: 0.2*L; items_rarity_timer: 2L-1; crystal_blacksmithing: L) | UpgradeModifierDef.ValueByLevel |
| `ValueMultiplier` | stat modifier scale (default 1) | UpgradeDef.WithStatModifierMultiplier |

**输出**：StatModifier named upgrade_<key>[_<param>] with Source GraphUpgrade (amulet) or CrystalUpgrade

**代码**：[Meta/Upgrades/UpgradeModel.cs:121-146](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeModel.cs#L121)；[Meta/Upgrades/UpgradeModifierDef.cs:23-26](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeModifierDef.cs#L23)；[Meta/Upgrades/UpgradeStatModifierDef.cs:25-28](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeStatModifierDef.cs#L25)；[Meta/Upgrades/UpgradeModel.cs:148-157](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeModel.cs#L148)

**状态依据**：Class header 'Original ARM64 methods 0x148a848..0x148d2d4'.

**使用方**：meta.metastat_value；combat stat aggregation (CollectStatModifiers)

**备注**：Several defs can target the same meta key and param. Their values add as separate Flat modifiers.

**依赖图节点**：输入 `value:player.upgrade_level`、`constant:AmuletUpgradeDefsBuilder.value_curves`、`constant:CrystalUpgradeDefsBuilder.value_curves`、`config:AmuletConfig.StatModifierMultipliers`；输出 `value:player.upgrade_meta_modifier`、`value:player.upgrade_stat_modifier`

### `meta.upgrade_purchase_rule`

**Upgrade purchase / starting level rule**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`config_rule`
- 来源：子系统梳理
- 原生地址：`0x148a848..0x148d2d4`

```text
level := max(level, StartingLevel) at setup; TryUpgrade: requires level < MaxLevel ∧ ∀k: balance_k ≥ cost_k(level+1) (unless free); spend; level += 1; ApplyAll
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `StartingLevel` | free initial level (e.g. crystal_blacksmithing = 1) | UpgradeDef.WithStartingLevel |

**输出**：new upgrade level

**代码**：[Meta/Upgrades/UpgradeModel.cs:47-51](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeModel.cs#L47)；[Meta/Upgrades/UpgradeModel.cs:67-79](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeModel.cs#L67)

**状态依据**：Same native class header.

**配置参数**：`UpgradeDef.MaxLevel`；`UpgradeDef.StartingLevel`

**使用方**：amulet graph；crystal upgrades

**备注**：Amulet-specific prerequisites (graph adjacency) live in AmuletUpgradesModel.IsPurchasable.

**依赖图节点**：输入 `state:save_counters`、`constant:CrystalUpgradeDefsBuilder.starting_level`、`constant:AmuletUpgradeDefsBuilder.max_level`、`constant:CrystalUpgradeDefsBuilder.max_level`、`value:economy.upgrade_cost`、`value:economy.currency_balance`、`value:amulet.node_available`、`state:player_actions`；输出 `value:player.upgrade_level`

### `meta.upgrade_total_preview`

**Upgrade total-value preview at a target level**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x148a848..0x148d2d4`

```text
Total = Σ over modifiers m on metaStats[mod.StatKey] with m.Source == modelSource ∧ m.ModifierType == mod.ModifierType ∧ (def.DisplayParam == null ∨ (StringParam empty ∧ (m.Param == 0 ∨ m.Param == DisplayParam))) of m.Value (0 if the stat is missing). Total(target) = Total - (current > 0 ? g(current) : 0) + (target > 0 ? g(target) : 0), with g = mod.Evaluate. NextTotal = (current+1 > MaxLevel) ? Total : Total(current+1)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `g` | UpgradeModifierDef.Evaluate(level) value lambda | UpgradeModifierDef |
| `modelSource` | UpgradeModel ModifierSource (default CrystalUpgrade) | Meta/Upgrades/UpgradeModel.cs:39-42 |
| `current` | current upgrade level | UpgradeModel.GetLevel |
| `DisplayParam` | optional UpgradeDef param filter | UpgradeDef.DisplayParam |

**输出**：previewed meta stat contribution

**代码**：[Meta/Upgrades/UpgradeModel.cs:90-98](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeModel.cs#L90)；[Meta/Upgrades/UpgradeModel.cs:99-104](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeModel.cs#L99)；[Meta/Upgrades/UpgradeModel.cs:105-110](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeModel.cs#L105)；[Meta/Upgrades/UpgradeModel.cs:121-135](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeModel.cs#L121)；[Meta/Upgrades/UpgradeModel.cs:147](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeModel.cs#L147)

**状态依据**：Class header comment UpgradeModel.cs:12 'Original ARM64 methods 0x148a848..0x148d2d4', which covers all methods of the class.

**配置参数**：`UpgradeDef.Modifiers[].StatKey/ModifierType/Param/StringParam`；`UpgradeDef.DisplayParam`；`UpgradeDef.MaxLevel`

**使用方**：upgrade UI / calculator

**备注**：Preview only. Confirms that per-level values replace each other and do not accumulate (ApplyAll removes and re-adds one modifier per def/mod at Evaluate(level)). With no DisplayParam there is no param filter, so the sum includes every same-source, same-type modifier on that stat, including other defs' modifiers. Applied values are stored as (float)Evaluate while g is a double, so tiny float rounding residues can appear.

**依赖图节点**：输入 `value:player.upgrade_level`、`value:player.upgrade_meta_modifier`、`constant:AmuletUpgradeDefsBuilder.value_curves`、`constant:CrystalUpgradeDefsBuilder.value_curves`、`constant:AmuletUpgradeDefsBuilder.display_param`、`constant:AmuletUpgradeDefsBuilder.max_level`、`constant:CrystalUpgradeDefsBuilder.max_level`；输出 `value:player.upgrade_preview_value`

**核验记录**（已写入上方字段）：

- 采纳 `expression`（来自 `meta.upgrade_total_preview`）：Verified at UpgradeModel.cs:95-96 (Source/ModifierType filter, with the DisplayParam filter only when it has a value), 147 (MatchesDisplayParam) and 109 (the current>0/target>0 guards). The expression was rewritten.

### `meta.currency_balance`

**Currency balance arithmetic**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x1516550, 0x15165a8, 0x1516638, 0x15148a8, 0x15166c8, 0x1516778, 0x15167b0`

```text
Add: if a > 0: B[k] += a; Set: B[k] = max(0, a); TrySpend: if a > 0 ∧ B[k] ≥ a: B[k] -= a (else fail); HasEnough = a ≤ 0 ∨ B[k] ≥ a
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `B[k]` | saved double balance per key (soul.*, crystal.*, boss_tower.tickets, empty_amulet, quest.*) | CurrencyUd.Balances |

**输出**：currency balances

**代码**：[Economy/CurrencyModel.cs:12-20](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Economy/CurrencyModel.cs#L12)；[Economy/CurrencyKeys.cs:5-19](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Economy/CurrencyKeys.cs#L5)

**状态依据**：Header comment lists the original native methods.

**使用方**：meta.upgrade_purchase_rule；meta.spawner_currency_reward；meta.grant_reward_stat

**备注**：Balances are doubles, so fractional souls are possible because of the soul_drop_bonus percentage.

**依赖图节点**：输入 `state:save_counters`、`value:economy.upgrade_cost`、`state:player_actions`、`constant:CurrencyModel.min_balance`；输出 `value:economy.currency_balance`

### `meta.upgrade_max_capacity_bonus`

**Max-capacity upgrade top-up (geodes/enchant stones/tickets/free chest)**

- 状态：**空壳**（`stub`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
(stub) presumably: on increase of crystal_max_geodes / crystal_max_enchant_stones / boss_tower_tickets / free_chest[chest_1], grant Δcapacity to the currency or reset the free-chest cooldown; GetFreeChestCooldownDays() returns 0
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `prevMax*` | previous capacity snapshot | system fields |

**输出**：currency top-ups

**代码**：[Meta/Systems/UpgradeMaxCapacityBonusSystem.cs:33-44](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Systems/UpgradeMaxCapacityBonusSystem.cs#L33)

**状态依据**：OnAwake and OnUpdate are empty and GetFreeChestCooldownDays returns 0, although prevMax* fields and injected models show it had logic.

**配置参数**：`Meta Stats Config.Stats[crystal_max_geodes]`；`Meta Stats Config.Stats[crystal_max_enchant_stones]`；`Meta Stats Config.Stats[boss_tower_tickets]`；`Meta Stats Config.Stats[free_chest]`

**备注**：Exact behavior is unknown.

**依赖图节点**：输入 `value:player.meta_stat`、`value:player.meta_stat[free_chest]`；输出 `value:economy.currency_balance`

### `meta.prepare_event_rewards`

**Prepare event success/failed rewards**

- 状态：**空壳**（`stub`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
(stub) PrepareSuccessRewards / PrepareFailedRewards: likely bake event StatRewards from simulation RPM
```

**输出**：event reward lists

**代码**：[Rewards/Commands/PrepareEventRewardsCommandRunner.cs:16-26](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/Commands/PrepareEventRewardsCommandRunner.cs#L16)

**状态依据**：All three methods are empty, yet the class injects SimulationSystem.

**使用方**：events subsystem

**依赖图节点**：输入 `value:sim.stat_rpm`、`state:current_event`；输出 `value:reward.event_reward_list`

### `meta.rewarded_ad_stat_value`

**Rewarded-ad permanent stat offer value**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 合并自：`meta.rewarded_ad_stat_value`、`powerups.rewarded_ad_stat_reward`
- 原生地址：`0x14c61ec (AdsInitialize.Update), 0x14c56b8 (GrantAdReward), 0x15a9df8 (UserModel.GetRewardedDensity), 0x14c6c08 (GetRewardWithDynamicWeights), 0x15c4bf4 (DoubleExtensions.Roll)`

```text
type = RewardedConfig.RewardChances[userModel.PoolIndex].GetRandomReward().Type (dynamic weights disabled); density = (InAppsBoughtValueInRewarded + RewardedWatchTimes) / (max(1, Minutes - totalDelay) / 60), totalDelay = delayMinutes + delayMinutesPerUsdSpent * InAppsBoughtValueInRewarded; doubled = Minutes >= 30 && Random.Range(0,1) < (1/exp(0.1*density) - 0.5); value = round((doubled ? 2 : 1) * rewardedValueMultiplier * RPM[type] * 6 * rewardedStatValueMultiplier(lastUnlockedLocation)). On grant: Flat StatModifier{Name='ads', Source=Ads, Value=(float)value} -> GrantStatReward, which multiplies by cheat.DropMultiplier. DoubleLoot and VIP do not apply because Source = Ads
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `RPM[type]` | simulation stat reward per minute (the offer is worth 6 minutes of farming) | SimulationSystem.RPMCalculatedValue |
| `type` | stat type picked from the RewardedConfig pool | meta.rewarded_pool_pick (AdsInitialize.cs:327-331) |
| `rewardedValueMultiplier` | AB-test scale | ABTestData.EnemyHpDamageMult.rewardedValueMultiplier (default 0.5) |
| `rewardedStatValueMultiplier` | AB-test per-location scale | ABTestData.CommonRewardMultipliers.rewardedStatValueMultiplier (ValueByLocation) |
| `totalDelay` | delayMinutes + delayMinutesPerUsdSpent*InAppsBoughtValueInRewarded | ABTestData.RewardedDelayFromStart |
| `cheat.DropMultiplier` | debug multiplier applied at grant (1 by default) | Player/Systems/PlayerRewardSystem.cs:54 |

**输出**：Flat permanent stat modifier 'ads' (Source Ads) via PlayerRewardSystem (about 6 minutes of farming income)

**代码**：[Integrations/Ads/AdsInitialize.cs:287-297](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Integrations/Ads/AdsInitialize.cs#L287)；[Integrations/Ads/AdsInitialize.cs:217-227](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Integrations/Ads/AdsInitialize.cs#L217)；[Integrations/Ads/AdsInitialize.cs:327-331](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Integrations/Ads/AdsInitialize.cs#L327)；[Analytics/UserModel.cs:398-403](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Analytics/UserModel.cs#L398)；[ABTests/ABTestService.cs:13](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L13)；[ABTests/ABTestService.cs:16-17](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L16)；[ABTests/ABTestService.cs:20](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L20)；[ABTests/ABTestService.cs:36](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L36)；[Utilities/DoubleExtensions.cs:67-71](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Utilities/DoubleExtensions.cs#L67)；[Player/Systems/PlayerRewardSystem.cs:54-65](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/PlayerRewardSystem.cs#L54)

**状态依据**：Update, which holds the value computation, has the comment 'Native ELF VA 0x14c61ec'. GrantAdReward (0x14c56b8), GetRewardedDensity ('Original ARM64 ELF VA 0x15a9df8'), GetRewardWithDynamicWeights (0x14c6c08) and Roll (0x15c4bf4) are also annotated.

**配置参数**：`ABTestData.EnemyHpDamageMult.rewardedValueMultiplier`；`ABTestData.CommonRewardMultipliers.rewardedStatValueMultiplier`；`ABTestData.RewardedDelayFromStart.delayMinutes/delayMinutesPerUsdSpent`；`RewardedConfig.RewardChances`；`EnemySpawnerConfig.RewardedPoolIndex`；`NoAdsData.adCooldown/rewardedOfferDuration/maxAdsPerDay`

**使用方**：meta.devour_stat_grant；permanent stat growth

**备注**：The double chance is at most 0.5 (at density 0) and falls as ad/IAP density rises. It is 0 once density >= 10*ln2 ≈ 6.93 per hour. The reward is tied to the simulation RPM, so changing devour/stat-reward balance rescales ad rewards automatically. Dynamic weights are disabled (IsDynamicRewardedWeights=false). The value is rounded at offer time and stored in userModel.CurrentRewardValue, then cast to float at grant. ResetReward (AdsInitialize.cs:311-317) has no math.

**依赖图节点**：输入 `value:ads.rewarded_stat_type`、`value:sim.stat_rpm`、`config:ABTestDataConfig.enemyHpDamageMult.variants[].data.rewardedValueMultiplier`、`config:ABTestDataConfig.commonRewardMultipliers.variants[].data.rewardedStatValueMultiplier`、`value:player.unlocked_location`、`config:ABTestDataConfig.rewardedDelayFromStart.variants`、`constant:RewardedDelayFromStartData.delayMinutes`、`constant:RewardedDelayFromStartData.delayMinutesPerUsdSpent`、`state:iap_purchases`、`state:ads_events`、`state:save_counters`、`constant:AdsInitialize.offer_rpm_minutes`、`constant:AdsInitialize.double_min_playtime_minutes`、`constant:AdsInitialize.density_exp_coef`、`constant:AdsInitialize.double_chance_offset`、`constant:AdsInitialize.double_value_multiplier`、`constant:UserModel.density_min_elapsed_minutes`、`state:rng`、`state:ab_cohort`；输出 `value:ads.rewarded_stat_value`

**核验记录**（已写入上方字段）：

- 采纳 `code_refs`（来自 `meta.rewarded_ad_stat_value`）：AdsInitialize.cs:287-297 holds the reward pick, density, exp, the doubling roll, the product and Math.Round. 311-317 is ResetReward. code_refs now cite 287-297.
- 采纳 `expression`（来自 `powerups.rewarded_ad_stat_reward`）：Verified: the pool is RewardChances[userModel.PoolIndex.Value] (AdsInitialize.cs:330). GrantStatReward multiplies by cheatsModel.DropMultiplier (PlayerRewardSystem.cs:54). DoubleLoot and VIP require Source==Enemy (56,65), and the ad modifier has Source=Ads (224). The expression includes all three.

### `meta.rewarded_pool_pick`

**Rewarded-ad stat type pool**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x144a610, 0x144aab4, 0x14c6c08`

```text
pool = RewardChances[PoolIndex]; PoolIndex = max over killed spawners of RewardedPoolIndex; P(type_i) = w_i / Σ_{j: RPM[type_j] ≠ 0} w_j
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `w_i` | pool weight | Rewarded Config.asset RewardChances[].Reward[].Weight |
| `RewardedPoolIndex` | pool tier unlocked by killing a spawner | EnemySpawner.RewardedPoolIndex (701 spawners at 0; one each at 1-12; four at 13) |

**输出**：StatType for the ad offer

**代码**：[Player/RewardPool.cs:13-33](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/RewardPool.cs#L13)；[Enemy/EnemySpawner.cs:1237-1239](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1237)；[Integrations/Ads/AdsInitialize.cs:327-350](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Integrations/Ads/AdsInitialize.cs#L327)

**状态依据**：Comment 'Original APK ARM64 ELF VA 0x144a610'. PoolIndex update in EnemySpawner is unannotated.

**配置参数**：`Rewarded Config.RewardChances[].Reward[].Type`；`Rewarded Config.RewardChances[].Reward[].Weight`；`EnemySpawner.RewardedPoolIndex`

**使用方**：meta.rewarded_ad_stat_value

**备注**：There are 14 pools. Pool 0 is Health 0.45 / CutDmg 0.45 / Regen 0.1. Later pools add StabDmg(6), BuckshotDmg(12), CutDef(16), StabDef(14) and BuckshotDef(20), keep Regen near 0.1, and rotate the dominant weapon type.

**依赖图节点**：输入 `value:player.rewarded_pool_index`、`config:RewardedConfig.RewardChances[].Reward[].Type`、`config:RewardedConfig.RewardChances[].Reward[].Weight`、`value:sim.stat_rpm`、`constant:ABTestService.is_dynamic_rewarded_weights`、`state:rng`；输出 `value:ads.rewarded_stat_type`

### `meta.rewarded_dynamic_weights`

**Rewarded-ad dynamic weight adaptation (AB)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x14c4e84 (watch), 0x14c61ec (Update/skip), 0x14c6c08 (roll; predicate 0x14c7c1c), 0x14c52dc (GetCurrentWeights), 0x14c744c (SaveWeights), 0x14c5630 (UpdateRewardWeight)`

```text
AB IsDynamicRewardedWeights only. On watch completion: w[type] = w.Has(type) ? w[type] + 1 : 2; LastSkippedRewardKey = ''. On offer expiry while shown and still offered (skip): w[type] = max(0.5, (w.Has(type) ? w[type] : 1)^0.9 * 0.9); LastSkippedRewardKey = type. Roll: rewards = RewardChances[PoolIndex].Reward filtered by RPM[type] > 0 (throw if empty); excluding LastSkipped type, total = Σ (w[type] ?? 1); r = U(0, total); return the first non-skipped entry with cumulative ≥ r, else rewards[Count-1] (the last entry of the RPM-filtered list, even if it is the skipped type). AB off → pool.GetRandomReward() (asset weights).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `w` | saved per-stat weight (default 1 when unset; duplicate saved rows keep the last value) | UserModel.RewardedWeights |
| `LastSkippedRewardKey` | stat excluded from the next roll | UserModel.LastSkippedRewardKey |
| `RPM[type]` | simulation RPM; entries with RPM ≤ 0 are excluded | SimulationSystem.RPMCalculatedValue |

**输出**：adapted offer type distribution (replaces the asset weights when the AB flag is on)

**代码**：[Integrations/Ads/AdsInitialize.cs:182-209](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Integrations/Ads/AdsInitialize.cs#L182)；[Integrations/Ads/AdsInitialize.cs:249-256](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Integrations/Ads/AdsInitialize.cs#L249)；[Integrations/Ads/AdsInitialize.cs:287](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Integrations/Ads/AdsInitialize.cs#L287)；[Integrations/Ads/AdsInitialize.cs:327-352](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Integrations/Ads/AdsInitialize.cs#L327)；[Integrations/Ads/AdsInitialize.cs:354-384](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Integrations/Ads/AdsInitialize.cs#L354)

**状态依据**：Each method carries a 'Native ELF VA ...' comment (AdsInitialize.cs:184, 237, 329, 356, 365, 380).

**配置参数**：`AB IsDynamicRewardedWeights`；`RewardedConfig.RewardChances[PoolIndex]`

**使用方**：meta.rewarded_pool_pick

**备注**：The final fallback returns rewards[Count-1] of the RPM-filtered list even if it is the skipped type. It is reachable only by float edge cases or when every filtered entry is the skipped type, with total = 0. Weights grow linearly with watches and decay geometrically toward the 0.5 floor on skips.

**依赖图节点**：输入 `constant:ABTestService.is_dynamic_rewarded_weights`、`state:save_counters`、`state:ads_events`、`value:player.rewarded_pool_index`、`config:RewardedConfig.RewardChances[].Reward[].Type`、`value:sim.stat_rpm`、`constant:AdsInitialize.dynamic_weight_watch_increment`、`constant:AdsInitialize.dynamic_weight_skip_exponent`、`constant:AdsInitialize.dynamic_weight_skip_factor`、`constant:AdsInitialize.dynamic_weight_floor`、`constant:AdsInitialize.dynamic_weight_default`、`state:rng`；输出 `value:ads.rewarded_weights`、`value:ads.rewarded_stat_type`

**核验记录**（已写入上方字段）：

- 采纳 `code_refs`（来自 `meta.rewarded_dynamic_weights`）：Verified the skip decay at 249-256 (line 254 Mathf.Max(0.5f, Mathf.Pow(current,0.9f)*0.9f)), the watch increment at 195-201 and the roll at 327-352 with LastSkippedRewardKey exclusion. code_refs were updated (handler ranges widened).
- 合并复核修正：The expression says the roll falls back to 'the last pool entry'. The code returns rewards[rewards.Count - 1] (AdsInitialize.cs:351), where rewards is the RPM-filtered list from line 332, not the raw pool. If the last authored pool entry has RPM ≤ 0, it is not the fallback. The expression is wrong on this point. Everything else matches the code at lines 195-201, 249-256 and 327-384.

### `meta.cashback_earned_stat`

**Cashback earned-stat ledger**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x154cdb4`

```text
if enabled ∧ v > 0 ∧ Type == Flat ∧ Name ≠ 'vip.cashback' ∧ StatType ∉ {GlobalAttack(30), GlobalDefence(31)}: Earned[StatType] += v
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `v` | granted modifier value | meta.devour_stat_grant / meta.grant_reward_stat |

**输出**：per-stat cumulative earned value used by cashback

**代码**：[Cashback/CashbackModel.cs:38-53](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Cashback/CashbackModel.cs#L38)

**状态依据**：Comment 'Native ELF VA 0x154cdb4'.

**配置参数**：`Cashback config Enabled`

**使用方**：cashback subsystem

**备注**：Defined in Cashback. Listed because every devour gain feeds it.

**依赖图节点**：输入 `value:reward.granted_stat_modifier`、`config:CashBackConfig.Enabled`、`constant:CashbackModel.excluded_stat_types`、`constant:CashbackModel.excluded_modifier_name`；输出 `value:economy.cashback_earned_stat`

### `meta.location_enemy_reward`

**location_enemy_reward meta stat**

- 状态：**纯数据**（`data_only`）
- 类型：`lookup_table`
- 来源：子系统梳理

```text
location_enemy_reward = BaseValue 0; no reader found
```

**输出**：unused

**代码**：[Meta/MetaStatKeys.cs:5](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStatKeys.cs#L5)

**状态依据**：Key exists in the asset and MetaStatKeys, but nothing reads or writes it.

**配置参数**：`Meta Stats Config.Stats[location_enemy_reward]`

**备注**：May be legacy, or read by a stripped or stubbed system.

**依赖图节点**：输入 `config:MetaStatsConfig.Stats[].BaseValue`；输出 `value:player.meta_stat[location_enemy_reward]`

### `meta.crystal_chest_legendary_chance`

**Crystal chest legendary chance**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：核验时补充

```text
p_leg = clamp01(0.035 + 0.005 * L), L = (int) chest_level.Value; RollCrystalRarity = U < p_leg ? Legendary : Epic
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `L` | chest level (1 + amulet chest_level flats + VIP level) | stats.raw_value_aggregation |

**输出**：CrystalRarity for a crystal chest roll

**代码**：[Shop/ChestsModel.cs:23-24](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L23)；[Shop/ChestsModel.cs:60-71](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L60)；[Shop/ChestsModel.cs:99-114](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L99)；[Shop/ChestsModel.cs:167-171](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L167)

**状态依据**：No native annotation in ChestsModel.

**配置参数**：`Meta Stats Config.Stats[chest_level].BaseValue=1`

**使用方**：crystal chest opening (Crystals/Shop)

**备注**：Companion of the equipment rule 0.095 + 0.005L. Constants CrystalBlacksmithBonus = 3 and LegendaryArmorUnlockLocation = 14 also sit here.

**依赖图节点**：输入 `value:player.meta_stat[chest_level]`、`constant:ChestsModel.CrystalLegendaryBaseChance`、`constant:ChestsModel.CrystalLegendaryChancePerLevel`、`constant:ChestsModel.FallbackLevel`、`state:rng`；输出 `value:reward.crystal_chest_legendary_chance`

### `meta.free_chest_cooldown_days`

**Free chest lot cooldown from meta stat**

- 状态：**重建（无原生标注）**（`reconstructed`），核验前为 `recovered_native`
- 类型：`runtime_formula`
- 来源：核验时补充

```text
days = sat_int32(free_chest.GetValue(lot.Def.Key)) (NaN→0, +Inf→int.MinValue); IsFreeChestLot = LotType == Chest ∧ days > 0; cooldown = TimeSpan.FromDays(days)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `free_chest[lotKey]` | days between free chest claims, string param = shop lot key (e.g. chest_1) | MetaStat free_chest (amulet/crystal writers) |

**输出**：free chest cooldown

**代码**：[Shop/ShopLotModel.cs:149-160](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ShopLotModel.cs#L149)

**状态依据**：Comment 'Original ARM64 FCVTZS saturates; its explicit +Infinity branch returns MinValue.'

**配置参数**：`Meta Stats Config.Stats[free_chest].BaseValue=0`

**使用方**：meta.upgrade_max_capacity_bonus (stub GetFreeChestCooldownDays)

**备注**：This is the live consumer of free_chest. The stubbed UpgradeMaxCapacityBonusSystem presumably reacted to changes in this value.

**依赖图节点**：输入 `value:player.meta_stat[free_chest]`、`config:ChestShopSectionsConfig.Sections[].Lots[].Key`、`config:ChestShopSectionsConfig.Sections[].Lots[].LotType`、`config:AmuletShopSectionsConfig.Sections[].Lots[].Key`、`config:AmuletShopSectionsConfig.Sections[].Lots[].LotType`；输出 `value:economy.free_chest_cooldown`

**核验记录**（已写入上方字段）：

- 采纳 `status`（来自 `meta.free_chest_cooldown_days`）：Mechanical audit 2026-10-27: none of the cited files contain a native address comment and native_va is empty; matches the completeness critic's status audit.

### `meta.event_currency_devour_reward`

**Event variant pick and event currency / progress on devour**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 合并自：`meta.event_currency_devour_reward`、`spawners.event_variant_currency`
- 原生地址：`0x14ee9ac (callee EventModel.AddProgress); 0x14ee830 (callee EventModel.AddCurrency)`

```text
At spawn (IsEvent && EventDef != null): variant = (Variants empty) ? null : Variants[Random.Range(0, Variants.Count)]; unit.EventUnitReward = variant. On devour: if CurrentEvent == null or Config.EventDef?.Key != CurrentEvent.Key: nothing. MiniBoss: AddProgress(1) (plus TryAdvanceRosterLeague for 'crystal_event_roster'); Boss: AddProgress(ProgressMax); AddProgress clamps Progress at ProgressMax. Mob: analytics only. Then if unit.EventUnitReward != null && CurrencyAmount > 0: Currencies[CurrencyKey] += CurrencyAmount and CollectedCurrencies[CurrencyKey] += CurrencyAmount
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `CurrencyAmount` | event currency per devour (float) | EventSpawnerVariant.CurrencyAmount via EnemyUnit.EventUnitReward |
| `CurrencyKey` | event currency key | EventSpawnerVariant.CurrencyKey |
| `Variants` | uniformly picked spawn variants (prefab + reward) | EventSpawnerDef.Variants |
| `ProgressMax` | event progress cap | EventModel.ProgressMax |

**输出**：event currency (current and collected) and event progress

**代码**：[Enemy/Commands/GrantSpawnerEventCurrencyRewardCommandHandler.cs:30-61](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerEventCurrencyRewardCommandHandler.cs#L30)；[Enemy/EventSpawnerDef.cs:18-21](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EventSpawnerDef.cs#L18)；[Enemy/EnemySpawner.cs:953-958](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L953)；[Enemy/EnemySpawner.cs:979](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L979)；[Enemy/EnemySpawner.cs:1377](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1377)；[Events/Models/EventModel.cs:367-372](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Events/Models/EventModel.cs#L367)；[Events/Models/EventModel.cs:383-388](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Events/Models/EventModel.cs#L383)

**状态依据**：The handler and GetRandomVariant have no native annotation. Only the EventModel callees AddProgress (0x14ee9ac) and AddCurrency (0x14ee830) are annotated.

**配置参数**：`EventSpawnerDef.Key`；`EventSpawnerDef.UnitType`；`EventSpawnerDef.Variants`；`EventSpawnerVariant.CurrencyKey`；`EventSpawnerVariant.CurrencyAmount`

**使用方**：events subsystem

**备注**：Fires in HandleDevour, but unlike stats and souls it is NOT scaled by CurrentDevourRewardMultiplier, cheat.DropMultiplier, stat_drop or the charge burst. Progress and currency are independent: progress is granted even when the variant has no currency. Because AddProgress clamps, a Boss devour sets Progress = ProgressMax. For crystal events, the picked variant's currency (blue_crystal/red_crystal) also decides an extra StatReward on mobs (EnemySpawner.cs:979-993), which is covered by meta.devour_stat_reward_scaling.

**依赖图节点**：输入 `config:EnemySpawnerConfig.IsEvent`、`config:EnemySpawnerConfig.EventDef.Key`、`config:EnemySpawnerConfig.EventDef.UnitType`、`config:EnemySpawnerConfig.EventDef`、`config:EventConfig.DifficultySettings`、`config:CrystalEventRosterConfig.BossMinutes`、`constant:GrantSpawnerEventCurrencyRewardCommandHandler.miniboss_progress_per_kill`、`state:current_event`、`state:rng`、`state:player_actions`；输出 `value:economy.currency_balance`、`value:economy.event_progress`

### `meta.event_forecast_gpm_bake`

**Event-forecast GPM stat reward bake (second BakedStatReward writer)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：核验时补充

```text
type = GetResolvedType() (DailyRotation aware; Empty → skip); if IsRandom ∧ AllowedStats: type = U(AllowedStats filtered by GPM>0 if UseGpm) else Health; if Value ≤ 0 skip; v = UseGpm ? PrettyRound(max(0, GPM_snap[type] * Value)) : Value; when UseGpm: BakedRewardStorage[SaveKey] = (type, v)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `GPM_snap[type]` | forecast snapshot gain per minute | ForecastSnapshotExtended (events/forecast subsystem) |

**输出**：baked crystal-event stage stat rewards later read by GrantRewardCommandRunner

**代码**：[Events/Services/EventForecastService.cs:224-245](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Events/Services/EventForecastService.cs#L224)

**状态依据**：ProcessStatReward has no native comment.

**配置参数**：`StatReward.Value`；`StatReward.UseGpm`；`StatReward.AllowedStats`；`StatReward.DailyRotation/DailyRotationStats`

**使用方**：meta.grant_reward_stat

**备注**：Differs from BakeRewardsCommandRunner (meta.bake_stat_reward): no min-1 clamp, no command multiplier, uses the forecast snapshot instead of live RPM, and falls back to Health instead of returning null.

**依赖图节点**：输入 `config:CrystalEventRosterConfig.Stages[].Rewards[].rid`、`config:CrystalEventRosterConfig.Milestones[].Rewards[].rid`、`config:CrystalEventRosterConfig.BossMinutes`、`value:forecast.snapshot`、`value:reward.daily_rotation_stat_type`、`state:rng`、`constant:EventForecastService.pretty_round_digits`；输出 `value:reward.baked_stat_reward`、`value:forecast.event_plan`

### `meta.quest_stat_reward_stat_drop`

**Quest step stat reward scaled by stat_drop**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：核验时补充
- 原生地址：`0x143afdc`

```text
v = r.Value * (float) stat_drop.GetValue(step.LocationNumber); modifier {Type = r.ModifierType, Source = Quest(18), Name = 'quest.<stepKey>'}
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `r.Value` | authored quest step stat reward | QuestStepDef.StatRewards |
| `stat_drop[loc]` | island stat-drop multiplier | meta.spawner_stat_drop_multiplier |

**输出**：quest stat modifiers

**代码**：[Quests/Commands/ClaimQuestStepRewardCommandHandler.cs:115-124](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/Commands/ClaimQuestStepRewardCommandHandler.cs#L115)

**状态依据**：Comment 'Original ELF 0x143afdc; candidate only.' The 'candidate only' wording makes this weaker than other native ports.

**配置参数**：`QuestStepDef.StatRewards`；`QuestStepDef.LocationNumber`

**使用方**：quests subsystem

**备注**：This is the second runtime consumer of stat_drop. Amulet island stat_drop nodes therefore boost quest rewards for that island too. Source Quest is not Enemy, so VIP and DoubleLoot do not apply.

**依赖图节点**：输入 `config:QuestConfig.Chains[].Steps[].StatRewards`、`config:QuestConfig.Chains[].Steps[].LocationNumber`、`value:spawner.stat_drop_multiplier`、`state:quest_progress`、`state:player_actions`；输出 `value:reward.quest_stat_reward_scaled`

### `meta.upgrade_simulation_settings`

**Per-upgrade simulation purchase settings**

- 状态：**纯数据**（`data_only`）
- 类型：`config_rule`
- 来源：核验时补充

```text
UpgradeDef.{SimTargetLevel=1, SimWeight=1, SimDay=1, SimStatSnapshotPercent=1, SimCostShare=1} defaults, overridden per def via WithSimulationSettings / WithSimulationCostShare
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `SimWeight, SimTargetLevel, SimDay, SimStatSnapshotPercent, SimCostShare` | inputs for the original balance simulator's upgrade-purchase model | Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs, Crystals/CrystalUpgradeDefsBuilder.cs |

**输出**：none at runtime

**代码**：[Meta/Upgrades/UpgradeDef.cs:45-49](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeDef.cs#L45)；[Meta/Upgrades/UpgradeDef.cs:56-62](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeDef.cs#L56)；[Meta/Upgrades/UpgradeDef.cs:144-157](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeDef.cs#L144)

**状态依据**：Nothing in the reconstruction reads these properties outside UpgradeDef.cs, so the consuming simulator or bake tool is absent.

**配置参数**：`UpgradeDef.SimTargetLevel`；`UpgradeDef.SimWeight`；`UpgradeDef.SimDay`；`UpgradeDef.SimStatSnapshotPercent`；`UpgradeDef.SimCostShare`

**备注**：These are design levers for the missing editor balance simulation (e.g. island1 stat_drop node: target level 3, weight 1.0, day 1). They are useful for rebuilding the design model.

**依赖图节点**：输入 `constant:AmuletUpgradeDefsBuilder.SimTargetLevel`、`constant:AmuletUpgradeDefsBuilder.SimWeight`、`constant:AmuletUpgradeDefsBuilder.SimDay`、`constant:AmuletUpgradeDefsBuilder.SimStatSnapshotPercent`、`constant:AmuletUpgradeDefsBuilder.SimCostShare`、`constant:UpgradeDef.sim_settings_defaults`；输出 `value:sim.upgrade_purchase_hints`

## 配置参数

| 资产 | 字段 | 类型 | 含义 | 用于 |
|---|---|---|---|---|
| Meta Stats Config.asset | `Stats[].BaseValue` | double | Base value of each meta stat. Non-zero: stat_drop 1, items_rarity_timer 1, crystal_enchant_efficiency 1, crystal_max_enchant_stones 1, chest_level 1. | meta.metastat_base_values, meta.metastat_value |
| Meta Stats Config.asset | `Stats[].Key` | string | meta stat identifier (56 entries) | meta.metastat_value |
| Meta Stats Config.asset | `Stats[].Format / ShowInCalculator / NameKey` | enum/bool/string | display only (MetaStatDisplayFormat) |  |
| Chests Config.asset | `Chests[].Drops[].Weight` | float | relative weight of (ItemType, Rarity) in a chest; royal and royal_armor weights are display-only | meta.chest_drop_weighted |
| Chests Config.asset | `Chests[].Drops[].Rarity / ItemType` | enum | drop rarity and item class; Drops[0].ItemType selects the copies formula and the armor divider | meta.chest_drop_weighted, meta.chest_copies |
| Rewarded Config.asset | `RewardChances[].Reward[].Type / Weight` | StatType/float | 14 rewarded-ad stat pools indexed by UserModel.PoolIndex | meta.rewarded_pool_pick |
| Scenes/Main.unity EnemySpawner | `StatRewards[] (StatType, ModifierType, Value, Source, Name)` | List<StatModifier> | permanent stat gained per devour | meta.devour_stat_reward_scaling |
| Scenes/Main.unity EnemySpawner | `LocationNumber` | int | param for stat_drop and map_spawner_timer_reduce | meta.spawner_stat_drop_multiplier, meta.spawner_timer |
| Scenes/Main.unity EnemySpawner | `SoulReward / SoulAlwaysAvailable / IsStatic` | string/bool/bool | soul type dropped and its unlock gate | meta.soul_drop_per_kill, meta.soul_drop_unlock_gate |
| Scenes/Main.unity EnemySpawner | `ItemReward` | ItemConfig | item dropped; its Rarity selects the items_rarity_* params | meta.item_drop_copies_per_kill, meta.spawner_bonus_charges, meta.spawner_timer |
| Scenes/Main.unity EnemySpawner | `Cooldown / BigCooldown / ChargesBeforeBigRespawn` | float/float/int | respawn timing and charges | meta.spawner_timer, meta.spawner_bonus_charges, meta.zero_cooldown_charge_burst |
| Scenes/Main.unity EnemySpawner | `CurrencyReward / CurrencyRewardAmount` | string/double | flat currency per devour | meta.spawner_currency_reward |
| Scenes/Main.unity EnemySpawner | `RewardedPoolIndex` | int | raises the rewarded-ad pool tier on kill | meta.rewarded_pool_pick |
| Amulet Config.asset | `PassDoubleSoulChance` | float | Soul Pass soul double chance (0.25) | meta.soul_grant_on_devour |
| Amulet Config.asset | `PassOneTimeDivineSouls` | int | divine souls granted on Soul Pass purchase (1) | meta.grant_reward_stat |
| StatReward (embedded in reward lists) | `Value / UseGpm / IsRandom / AllowedStats / SaveKey / CustomModifier` | mixed | generic stat reward definition; with UseGpm, Value means minutes of RPM | meta.grant_reward_stat, meta.bake_stat_reward |
| code constants (Shop/ChestsModel.cs) | `EquipmentLegendaryBaseChance=0.095, PerLevel=0.005, ArmorCountDivider=1.5` | float | rare chest legendary chance and armor copy divider | meta.chest_drop_weighted, meta.chest_copies |
| code constants (Integrations/Ads/AdsInitialize.cs) | `6.0 RPM-minutes, 0.1 density factor, 30 min double gate` | double | rewarded-ad value shape | meta.rewarded_ad_stat_value |

## 枚举

| 枚举 | 文件 | 取值 |
|---|---|---|
| `ModifierType` | [Unit/ModifierType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/ModifierType.cs) | 0=Flat,1=PercentAdditive,2=PercentMultiplicative,3=PostFormulaAdditive,4=PreAdditivePostMultiplicative,5=PostFormulaMultiplicative,6=PostFormulaFlat,7=EventPercentModifier |
| `ModifierSource` | [Unit/ModifierSource.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/ModifierSource.cs) | 0=Enemy,1=Item,2=AbilityPermanent,3=AbilityTemporary,4=Shop,5=Ads,6=Reward,7=LevelPass,8=Crystal,9=CrystalUpgrade,10=GraphUpgrade,11=MineUpgrade,12=CrystalEvent,13=CrystalPass,14=SoulPass,15=Vip,16=SecretChest,17=CrystalGuardian,18=Quest,19=Cashback |
| `StatType` | [Unit/StatType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatType.cs) | 0=MovementSpeed,1=Health,2=HealthRegen,3=Damage,4=AttackRange,5=AttackCooldown,6=StabDamageFlat,7=StabDamagePercent,8=CutDamageFlat,9=CutDamagePercent,10=MagicDamageFlat,11=MagicDamagePercent,12=BuckshotDamageFlat,13=BuckshotDamagePercent,14=StabDefenceFlat,15=StabDefencePercent,16=CutDefenceFlat,17=CutDefencePercent,18=MagicDefenceFlat,19=MagicDefencePercent,20=BuckshotDefenceFlat,21=BuckshotDefencePercent,22=LifestealFlat,23=Evasion,24=CritChance,25=CritDamagePercent,26=UniversalDefenceFlat,27=UniversalDefencePercent,28=SuperCritChance,29=SuperCritDamagePercent,30=GlobalAttack,31=GlobalDefence,32=LifestealPercent,33=BlockChance,34=BlockAmount,35=VisualScaleMultiplier |
| `Rarity` | [Player/Inventory/Rarity.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/Rarity.cs) | 0=Common,1=Uncommon,2=Rare,3=Epic,4=Legendary,5=Divine,6=Mythical (meta param = rarity+1) |
| `ItemType` | [Player/Inventory/ItemType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemType.cs) | 0=Weapon,1=Armor |
| `MetaStatDisplayFormat` | [Meta/MetaStatDisplayFormat.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStatDisplayFormat.cs) | 0=Flat,1=Percent,2=Minutes,3=Seconds |
| `UpgradeValueFormat` | [Meta/Upgrades/UpgradeValueFormat.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeValueFormat.cs) | 0=Bonus,1=BonusPercent,2=Absolute,3=Time,4=TimeReduceMinutes |

## 未解决问题

1. SimulationSystem RPM (SimulationSystem.cs:409, 761-765) reads raw StatRewards[].Value. Does the original forecast apply stat_drop[location] and the zero-cooldown burst n? If not, forecast RPM underestimates real devour gains once amulet stat_drop nodes are bought.
2. PlayerRewardSystem applies cheat.DropMultiplier, and GrantSpawnerSoulRewardCommandHandler applies it too, but GrantSpawnerStatRewardCommandHandler does not. That is consistent with the cheat being applied once, but the stat handler itself is unannotated (reconstructed).
3. UpgradeMaxCapacityBonusSystem is a stub. How much currency is granted when crystal_max_geodes, crystal_max_enchant_stones, boss_tower_tickets or free_chest rise, and what is the free-chest cooldown in days?
4. PrepareEventRewardsCommandRunner is a stub. What formula produced event success/failed rewards (probably RPM-based like BakeRewards)?
5. StatReward.GetResolvedType (DailyRotation) has no call site in the reconstruction. Was it used by the grant or display path in the original?
6. ItemReward.Level is passed as the copy count to PlayerModel.CreateItem in GrantRewardCommandRunner.HandleItemReward. Is that original semantics or a reconstruction artifact?
7. location_enemy_reward has no readers. Was it consumed by a stripped system?
8. soul_drop_bonus has no writer in the Amulet or Crystal builders. Which source, if any, modifies it (shop, VIP, event)?
9. GrantRewardCommandRunner.HandleStatReward bypasses PlayerRewardSystem, so there is no VIP stat multiplier or DoubleLoot. Is that faithful? It is unannotated.
10. PrettyRound returns int. Do any late-game upgrade costs (e.g. ForgeCost-based crystal costs with offsets up to about 210000 and power 1.3) approach the int overflow range?
