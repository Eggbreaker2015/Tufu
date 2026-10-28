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
| [`meta.metastat_value_unfiltered`](#metametastat_value_unfiltered) | MetaStat .Value (StatBase aggregation, param-blind) | 重建（无原生标注） | `runtime_formula` |
| [`meta.modifier_merge_accumulation`](#metamodifier_merge_accumulation) | Permanent stat accumulation by modifier identity merge | 重建（无原生标注） | `runtime_formula` |
| [`meta.metastat_persistence_filter`](#metametastat_persistence_filter) | Temporary (non-persisted) modifier sources | 原生证据恢复 | `config_rule` |
| [`meta.metastat_base_values`](#metametastat_base_values) | Meta stat base values (design defaults) | 纯数据 | `lookup_table` |
| [`meta.metastats_fill`](#metametastats_fill) | MetaStatsConfig.Fill editor generator | 编辑器工具缺失 | `editor_generator` |
| [`meta.spawner_stat_drop_multiplier`](#metaspawner_stat_drop_multiplier) | Spawner stat-drop multiplier (stat_drop) | 原生证据恢复 | `runtime_formula` |
| [`meta.zero_cooldown_charge_burst`](#metazero_cooldown_charge_burst) | Zero-cooldown charge burst and kill multiplier | 重建（无原生标注） | `runtime_formula` |
| [`meta.devour_stat_reward_scaling`](#metadevour_stat_reward_scaling) | Devour stat reward per StatRewards entry | 重建（无原生标注） | `runtime_formula` |
| [`meta.devour_stat_grant`](#metadevour_stat_grant) | PlayerRewardSystem.GrantStatReward multipliers | 原生证据恢复 | `runtime_formula` |
| [`meta.soul_drop_per_kill`](#metasoul_drop_per_kill) | Souls per devour (calculator) | 原生证据恢复 | `runtime_formula` |
| [`meta.soul_grant_on_devour`](#metasoul_grant_on_devour) | Soul grant on devour (gate, cheat, Soul Pass double) | 重建（无原生标注） | `runtime_formula` |
| [`meta.soul_drop_unlock_gate`](#metasoul_drop_unlock_gate) | Soul drop unlock gate | 原生证据恢复 | `config_rule` |
| [`meta.soul_drop_for_kill_count`](#metasoul_drop_for_kill_count) | Souls for a forecast kill count (simulation) | 原生证据恢复 | `simulation` |
| [`meta.item_drop_copies_per_kill`](#metaitem_drop_copies_per_kill) | Item copies per kill | 重建（无原生标注） | `runtime_formula` |
| [`meta.item_drop_for_kill_count`](#metaitem_drop_for_kill_count) | Item copies for a kill count (forecast) | 重建（无原生标注） | `simulation` |
| [`meta.spawner_bonus_charges`](#metaspawner_bonus_charges) | Spawner charges before big respawn | 重建（无原生标注） | `runtime_formula` |
| [`meta.spawner_timer`](#metaspawner_timer) | Spawner small-cooldown meta reduction | 重建（无原生标注） | `runtime_formula` |
| [`meta.spawner_currency_reward`](#metaspawner_currency_reward) | Spawner flat currency reward | 重建（无原生标注） | `runtime_formula` |
| [`meta.crystal_event_mob_stat_reward`](#metacrystal_event_mob_stat_reward) | Crystal-event mob devour stat reward | 重建（无原生标注） | `runtime_formula` |
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
| [`meta.rewarded_ad_stat_value`](#metarewarded_ad_stat_value) | Rewarded-ad stat offer value | 原生证据恢复 | `runtime_formula` |
| [`meta.rewarded_pool_pick`](#metarewarded_pool_pick) | Rewarded-ad stat type pool | 原生证据恢复 | `runtime_formula` |
| [`meta.rewarded_dynamic_weights`](#metarewarded_dynamic_weights) | Rewarded-ad dynamic weight adaptation (AB) | 原生证据恢复 | `runtime_formula` |
| [`meta.cashback_earned_stat`](#metacashback_earned_stat) | Cashback earned-stat ledger | 原生证据恢复 | `runtime_formula` |
| [`meta.location_enemy_reward`](#metalocation_enemy_reward) | location_enemy_reward meta stat | 纯数据 | `lookup_table` |
| [`meta.spawner_charge_upgrade_topup`](#metaspawner_charge_upgrade_topup) | Spawner charge top-up when capacity rises | 重建（无原生标注） | `runtime_formula` |
| [`meta.crystal_chest_legendary_chance`](#metacrystal_chest_legendary_chance) | Crystal chest legendary chance | 重建（无原生标注） | `runtime_formula` |
| [`meta.free_chest_cooldown_days`](#metafree_chest_cooldown_days) | Free chest lot cooldown from meta stat | 重建（无原生标注） | `runtime_formula` |
| [`meta.event_currency_devour_reward`](#metaevent_currency_devour_reward) | Event currency / progress on devour | 重建（无原生标注） | `runtime_formula` |
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

**使用方**：meta.spawner_stat_drop_multiplier；meta.soul_drop_per_kill；meta.soul_drop_for_kill_count；meta.item_drop_copies_per_kill；meta.spawner_bonus_charges；meta.spawner_timer；meta.soul_drop_unlock_gate；[Quests/Commands/ClaimQuestStepRewardCommandHandler.cs:120](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/Commands/ClaimQuestStepRewardCommandHandler.cs#L120)；crystal/tower/boss-tower subsystems reading crystal_* / tower_* keys

**备注**：Unlike StatBase.CalculateValue, MetaStat ignores EventPercentModifier, PostFormula* and other types. PercentAdditive starts at 1, so PA values are fractions. Nearly all real meta writers are Flat (ModifierType 0). The exception is crystal_enchant_efficiency, which uses PAPM (type 4) at 0.01*level.

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

### `meta.metastat_value_unfiltered`

**MetaStat .Value (StatBase aggregation, param-blind)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
V = Π_event(1 + EventPct(m)*0.01) * Π_PM m * (1 + Σ_PA m) * (Base + Σ_Flat m) * Π_s(1 + Σ_PAPM,s m), over ALL modifiers regardless of Param
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Base` | MetaStatDef.BaseValue | Meta Stats Config.asset |

**输出**：double, used where code reads stat.Value instead of GetValue(param)

**代码**：[Unit/StatBase.cs:107-134](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatBase.cs#L107)；[Shop/ChestsModel.cs:42-43](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L42)；[Shop/ChestsModel.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs) (CrystalChestEnchantLevel)

**状态依据**：StatBase.CalculateValue has no native annotation; MetaStat ctor calls UpdateValue() which uses it (Meta/MetaStat.cs:46).

**配置参数**：`Meta Stats Config.Stats[chest_level].BaseValue=1`；`Meta Stats Config.Stats[crystal_chest_ench_level].BaseValue=0`

**使用方**：meta.chest_copies；meta.chest_rare_rarity_roll

**备注**：chest_level = (int)(1 + Σ amulet flat chest_level + VIP level).

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `code_refs`：Shop/ChestsModel.cs:76-83  
  依据：CrystalChestEnchantLevel => (int)metaStatsModel.Get("crystal_chest_ench_level").Value

### `meta.modifier_merge_accumulation`

**Permanent stat accumulation by modifier identity merge**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`MetaStat.OnModifierUpdated 0x148681c`

```text
on AddModifier(n): if ∃ e with (e.Name, e.Source, e.Type) == (n.Name, n.Source, n.Type): e.Value = (Type∈{PM, PostFormulaMult}) ? e.Value * n.Value : e.Value + n.Value; e.Duration = max(e.Duration, n.Duration); else append n
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `n.Name` | reward modifier name, e.g. 'map.normal_mob', 'map.elite', 'map.tree', 'ads', sourcePath, 'quest.<step>', 'event.<key>.<type>' | reward handlers |

**输出**：one cumulative modifier per (name, source, type); the player's permanent stat grows by the sum of all devour gains

**代码**：[Unit/StatBase.cs:63-85](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatBase.cs#L63)；[Meta/MetaStat.cs:105-117](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStat.cs#L105)；[Enemy/Commands/GrantSpawnerStatRewardCommandHandler.cs:26](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerStatRewardCommandHandler.cs#L26)；[Enemy/Commands/GrantSpawnerStatRewardCommandHandler.cs:34-37](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerStatRewardCommandHandler.cs#L34)

**状态依据**：StatBase.AddModifier has no annotation. The MetaStat save hook (0x148681c) is annotated and is consistent with merge-by-identity.

**使用方**：meta.devour_stat_grant；meta.grant_reward_stat；meta.rewarded_ad_stat_value

**备注**：Enemy-source devour rewards with an empty name or 'Devoured' are renamed by spawner class: IsStatic→map.tree, IsEliteBoss→map.elite, else map.normal_mob. So total stat = base + Σ over all devours.

### `meta.metastat_persistence_filter`

**Temporary (non-persisted) modifier sources**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`config_rule`
- 来源：子系统梳理
- 原生地址：`0x1485ed4, 0x1485bf4`

```text
temporary(src) = src < 16 ∧ (0xEF0A >> src) & 1 → src ∈ {Item(1), AbilityTemporary(3), Crystal(8), CrystalUpgrade(9), GraphUpgrade(10), MineUpgrade(11), CrystalPass(13), SoulPass(14), Vip(15)}
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `src` | ModifierSource | StatModifier.Source |

**输出**：whether a meta modifier is written to and kept in save data

**代码**：[Meta/MetaStat.cs:135-139](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStat.cs#L135)；[Meta/MetaStat.cs:49-66](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStat.cs#L49)

**状态依据**：Comment 'Native ELF VA 0x1485ed4, unsigned range and exact original bit mask.'

**使用方**：meta.upgrade_modifier_apply

**备注**：Upgrade-derived meta modifiers (GraphUpgrade, CrystalUpgrade) and the VIP modifier are re-derived each session from upgrade levels. Only sources such as Enemy, Reward, Shop, Ads, LevelPass and CrystalEvent persist on meta stats.

### `meta.metastat_base_values`

**Meta stat base values (design defaults)**

- 状态：**纯数据**（`data_only`）
- 类型：`lookup_table`
- 来源：子系统梳理
- 原生地址：`0x1487e78`

```text
MetaStat(key).BaseValue = MetaStatsConfig.Stats[key].BaseValue (config always wins; saved base_value is written only for new entries and never read back)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `BaseValue` | default before any upgrade | Meta Stats Config.asset |

**输出**：base for every meta stat. Non-zero bases: stat_drop=1, items_rarity_timer=1, crystal_enchant_efficiency=1, crystal_max_enchant_stones=1, chest_level=1. All others are 0.

**代码**：[Meta/MetaStatsModel.cs:29-48](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStatsModel.cs#L29)；[Meta/MetaStatDef.cs:13](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStatDef.cs#L13)

**状态依据**：Populate is native-annotated, but the numbers themselves are authored data in the asset.

**配置参数**：`Meta Stats Config.Stats[].Key`；`Meta Stats Config.Stats[].BaseValue`；`Meta Stats Config.Stats[].Format`

**使用方**：meta.metastat_value

**备注**：Duplicate config keys are skipped (first wins). For duplicate saved keys, the last one wins.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes / params Stats[].Key`：58 entries in Meta Stats Config.asset. Keys beyond MetaStatKeys usage include crystal_slots, hammer_regen and crystal_enchantment_unlock, all with base 0. The non-zero bases are confirmed: stat_drop, items_rarity_timer, crystal_enchant_efficiency, crystal_max_enchant_stones and chest_level are 1.  
  依据：Parsed '- Key:/BaseValue:' pairs in MonoBehaviour/Meta Stats Config.asset: 58 pairs

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

### `meta.spawner_stat_drop_multiplier`

**Spawner stat-drop multiplier (stat_drop)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x1488e3c`

```text
M_stat(spawner) = stat_drop.GetValue(param), param = (IsEvent ∧ EventDef.UnitType == Mob) ? -1 : LocationNumber; with base 1 and Flat writers: M_stat = 1 + Σ_{Param∈{0,param}} flat
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `LocationNumber` | spawner island | EnemySpawner.LocationNumber (Scenes/Main.unity) |
| `flat` | amulet graph 'graph_map_spawner_drop_islandN' value, e.g. Native_15b67d8: 0.2*level (max level 5, so up to +1.0); event golem node +1.0 at Param -1 | Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:201-218, 633-651, 861, 1093-1107, 1621 |

**输出**：multiplier on all StatRewards of a spawner

**代码**：[Meta/SpawnerStatDropAmountCalculator.cs:9-14](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SpawnerStatDropAmountCalculator.cs#L9)

**状态依据**：Comment 'Original bounded ARM64 ELF VA 0x1488e3c.'

**配置参数**：`Meta Stats Config.Stats[stat_drop].BaseValue=1`；`EnemySpawner.LocationNumber`；`EnemySpawner.IsEvent`；`EnemySpawner.EventDef`

**使用方**：meta.devour_stat_reward_scaling；[Quests/Commands/ClaimQuestStepRewardCommandHandler.cs:120](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/Commands/ClaimQuestStepRewardCommandHandler.cs#L120) (quest stat reward = stat.Value * stat_drop[step.LocationNumber])

**备注**：Island-1 upgrade cost is soul.common = level^5 (Native_15b677c). SimulationSystem RPM uses raw StatRewards[].Value (SimulationSystem.cs:409, 761-765) without visibly applying stat_drop; see open questions.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `variables.flat.source`：Upgrade keys look like 'graph_map_spawner_drop_island1_2_4' (max level 5, cost soul.common = L^5 via Native_15b677c, value float32(L*0.2f) via Native_15b67d8). Further stat_drop nodes have Params 2, 3, 4, 5, 6, 8 and 9, and one node has constant 1.0 at Param -1. The quest consumer is tagged 'candidate only' native 0x143afdc.  
  依据：[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:201-206](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L201), 211, 218, 637-651, 861, 1093-1100, 2060-2062

### `meta.zero_cooldown_charge_burst`

**Zero-cooldown charge burst and kill multiplier**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
n = (ceil(cooldown') == 0 ∧ ChargesEnabled ∧ unit.ItemReward == null) ? max(1, RemainingCharges + 1) [RemainingCharges := 0] : 1; if an item was granted: n := itemCopies; CurrentDevourRewardMultiplier = n during devour
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `RemainingCharges` | charges left before the big respawn | EnemySpawner runtime, max = meta.spawner_bonus_charges |
| `cooldown'` | effective small cooldown | meta.spawner_timer |
| `itemCopies` | copies granted on this kill | meta.item_drop_copies_per_kill |

**输出**：EnemyUnit.SpawnerStatRewardMultiplier, which multiplies stat rewards, souls and OnKill AddStat abilities

**代码**：[Enemy/EnemySpawner.cs:722-732](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L722)；[Enemy/EnemySpawner.cs:1186](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1186)；[Enemy/EnemySpawner.cs:1213-1215](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1213)；[Enemy/EnemySpawner.cs:1360-1381](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1360)

**状态依据**：No native annotation on ConsumeZeroCooldownStatRewardMultiplier or HandleDeath/HandleDevour.

**配置参数**：`EnemySpawner.Cooldown`；`EnemySpawner.ChargesBeforeBigRespawn`

**使用方**：meta.devour_stat_reward_scaling；meta.soul_grant_on_devour；[Abilities/Core/AbilityContext.cs:33](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Core/AbilityContext.cs#L33)；[Abilities/Effects/AddStatEffect.cs:48-49](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/AddStatEffect.cs#L48)；Crystals ReduceGeodeTimerOnKillCommand (KillCount = additionalCharges + n)

**备注**：When map_spawner_timer_reduce drives a spawner cooldown to 0, a single kill consumes all remaining charges and pays them at once. This is a strong discontinuity for the model.

### `meta.devour_stat_reward_scaling`

**Devour stat reward per StatRewards entry**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
for r in (unit.StatRewards ?? spawner.StatRewards): v_r = r.Value * M_stat(spawner) * (n > 1 ? n : 1); then PlayerRewardSystem.GrantStatReward(v_r, displayMult = n)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `r.Value` | authored stat reward per devour | EnemySpawner.StatRewards[].Value (Scenes/Main.unity), or event plan override |
| `M_stat` | stat_drop multiplier | meta.spawner_stat_drop_multiplier |
| `n` | kill multiplier | meta.zero_cooldown_charge_burst |

**输出**：StatModifier passed to the player grant

**代码**：[Enemy/Commands/GrantSpawnerStatRewardCommandHandler.cs:17-31](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerStatRewardCommandHandler.cs#L17)；[Enemy/EnemyUnit.cs:147](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L147)；[Enemy/EnemySpawner.cs:1358-1381](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1358)

**状态依据**：Handler has no native annotation.

**配置参数**：`EnemySpawner.StatRewards[] (StatType, ModifierType, Value, Source, Name)`

**使用方**：meta.devour_stat_grant

**备注**：Fires on devour (HandleDevour), not on death. Modifier type and source come from the authored reward, normally Flat and Enemy.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `code_refs`：Enemy/EnemyUnit.cs:133,146 (property and reset to 1); multiplier overwritten by item copies at Enemy/EnemySpawner.cs:1214-1215  
  依据：grep SpawnerStatRewardMultiplier

### `meta.devour_stat_grant`

**PlayerRewardSystem.GrantStatReward multipliers**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x1456084`

```text
v' = v * cheat.DropMultiplier; if Source==Enemy: for each equipped ability with DoubleLoot flag: v' *= (1 + ability.GetValue()) and the flag is consumed; if Source==Enemy ∧ VIP stat_multiplier: with prob level.Chance: v' *= level.Multiplier; stat.AddModifier(v'); if Flat Health ∧ alive: CurrentHealth += v'
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `cheat.DropMultiplier` | debug multiplier (1 by default) | Services/Cheats/CheatsModel.cs:50 |
| `ability.GetValue()` | double-loot ability bonus | item ability states (abilities subsystem) |
| `level.Chance, level.Multiplier` | VIP stat multiplier roll | VIP StatMultiplierConfig.LevelConfigs[vipLevel] |

**输出**：permanent player stat modifier (merged per meta.modifier_merge_accumulation) and the cashback ledger entry

**代码**：[Player/Systems/PlayerRewardSystem.cs:49-84](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/PlayerRewardSystem.cs#L49)

**状态依据**：Comment 'Original ELF VA 0x1456084 and three bounded predicate bodies'.

**配置参数**：`VIP StatMultiplierConfig.LevelConfigs[].Chance`；`VIP StatMultiplierConfig.LevelConfigs[].Multiplier`

**使用方**：meta.devour_stat_reward_scaling；meta.rewarded_ad_stat_value；Quests ClaimQuestStepReward；Abilities AddStatEffect / GrantRandomStatPerDistanceEffect；AdView no_ads grant

**备注**：The VIP and DoubleLoot bonuses apply only to Source==Enemy, so quest, ads and reward grants are excluded.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression`：... if ModifierType ∉ {PA, PM, PAPM, PostFormulaMult} ∧ StatType == Health ∧ !Unit.IsDying: CurrentHealth += v'. This also covers PostFormulaAdditive, PostFormulaFlat and EventPercent. Each equipped ability with the DoubleLoot flag multiplies in turn (product of (1 + GetValue())) and clears its flag.  
  依据：[Player/Systems/PlayerRewardSystem.cs:57-63](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/PlayerRewardSystem.cs#L57), 76-79

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
| `rewardMultiplier` | CurrentDevourRewardMultiplier (n) | meta.zero_cooldown_charge_burst |

**输出**：double souls added to the amulet soul balance

**代码**：[Meta/SoulDropAmountCalculator.cs:7-8](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SoulDropAmountCalculator.cs#L7)；[Meta/SoulDropAmountCalculator.cs:12-13](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SoulDropAmountCalculator.cs#L12)

**状态依据**：Comment 'Original ELF VA 0x1488810; per-kill clamps only the multiplier, not the rarity bonus.'

**配置参数**：`Meta Stats Config.Stats[soul_drop_bonus]`；`Meta Stats Config.Stats[souls_rarity_amount]`；`EnemySpawner.SoulReward`

**使用方**：meta.soul_grant_on_devour

**备注**：The rarity bonus is additive to the multiplier, not multiplied by it. No amulet node was found writing soul_drop_bonus, so it is effectively 0 and the per-kill base is 1.

### `meta.soul_grant_on_devour`

**Soul grant on devour (gate, cheat, Soul Pass double)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
if SoulReward ≠ ∅ ∧ soulsDropUnlocked: a = souls * cheat.DropMultiplier; if SoulPassPurchased ∧ U(0,1) < PassDoubleSoulChance: a = 2a; AmuletModel.AddSouls(SoulReward, a)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `souls` | per-kill souls | meta.soul_drop_per_kill |
| `PassDoubleSoulChance` | Soul Pass double chance | Amulet Config.asset PassDoubleSoulChance = 0.25 |

**输出**：soul currency (soul.<key>) gained

**代码**：[Enemy/Commands/GrantSpawnerSoulRewardCommandHandler.cs:29-40](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerSoulRewardCommandHandler.cs#L29)

**状态依据**：Handler has no native annotation. Calculator and gate are native.

**配置参数**：`Amulet Config.PassDoubleSoulChance`；`EnemySpawner.SoulReward`

**使用方**：Amulet upgrade purchases (soul.* costs)

**备注**：Main.unity spawners by SoulReward: common 459, large 183, elite 33, huge 12, great 1, chaotic 1.

### `meta.soul_drop_unlock_gate`

**Soul drop unlock gate**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`config_rule`
- 来源：子系统梳理
- 原生地址：`0x15ba79c`

```text
unlocked = SoulAlwaysAvailable ∨ (IsStatic ? feature_unlock['static_souls_drop'] : souls_drop_unlock[SoulReward]) > 0
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `souls_drop_unlock[key]` | set to 1 by amulet nodes graph_souls_drop_unlock_<type> | Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:15-18 etc. |

**输出**：whether a spawner drops souls at all

**代码**：[Amulet/Upgrades/AmuletUpgradesModel.cs:142-151](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradesModel.cs#L142)

**状态依据**：Comment 'Original ELF VA 0x15ba79c'.

**配置参数**：`EnemySpawner.SoulAlwaysAvailable`；`EnemySpawner.IsStatic`；`EnemySpawner.SoulReward`

**使用方**：meta.soul_grant_on_devour

**备注**：Defined in the Amulet subsystem and listed here because it gates the meta soul reward.

### `meta.soul_drop_for_kill_count`

**Souls for a forecast kill count (simulation)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`simulation`
- 来源：子系统梳理
- 原生地址：`0x14889a4`

```text
souls(K) = (1 + soul_drop_bonus[0] + souls_rarity_amount[soulKey]) * K   (no max(1,·) clamp); simulation calls it with K = Σ_spawners(soulKey) (10/(fightTime+10)) * charges / 60
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `K` | forecast kills per minute | Simulation/Systems/SimulationSystem.cs:472-485 |

**输出**：soul RPM per soul key

**代码**：[Meta/SoulDropAmountCalculator.cs:10-11](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SoulDropAmountCalculator.cs#L10)；[Simulation/Systems/SimulationSystem.cs:485](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L485)

**状态依据**：Comment 'Original ELF VA 0x14889a4 deliberately has no per-kill clamp.'

**配置参数**：`Meta Stats Config.Stats[soul_drop_bonus]`；`Meta Stats Config.Stats[souls_rarity_amount]`

**使用方**：simulation.soul_rpm

**备注**：Ignores the Soul Pass double chance and the per-kill n burst.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression`：K = Σ over spawners with SoulReward == soulKey ∧ forecastHealth ≥ -healthDelta of (10/(fightTime+10)) * chargesLimit, then / 60, where chargesLimit = DisableAfterFirstClear ? Count : ChargesTotal. soulRPM = souls(K); if soulRPM > 0: soulRPM = max(0.03, soulRPM); soulRPM = max(soulRPM, previous SoulRPM[soulKey]), so the value only ever grows.  
  依据：[Simulation/Systems/SimulationSystem.cs:475-485](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L475), 488-495, 608-612

### `meta.item_drop_copies_per_kill`

**Item copies per kill**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
c = 1 + (int) items_rarity_drop_amount[rarity+1]; with charges enabled: copies = min(c, RemainingCharges + 1), extraChargesConsumed = max(0, copies - 1)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `rarity` | item reward rarity (0=Common..6=Mythical) | EnemySpawner.ItemReward.Rarity |
| `items_rarity_drop_amount[r]` | Flat extra copies per rarity param (amulet graph 'graph_items_drop_<rarity>' with Param 1..3) | Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:80 |

**输出**：ItemModel copies added via PlayerModel.CreateItem(item, copies)

**代码**：[Meta/ItemDropAmountCalculator.cs:8-25](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/ItemDropAmountCalculator.cs#L8)；[Enemy/EnemySpawner.cs:1192-1210](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1192)；[Enemy/Commands/GrantSpawnerItemRewardCommandHandler.cs:26-30](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerItemRewardCommandHandler.cs#L26)

**状态依据**：No native annotation in ItemDropAmountCalculator.

**配置参数**：`Meta Stats Config.Stats[items_rarity_drop_amount]`；`EnemySpawner.ItemReward`

**使用方**：meta.zero_cooldown_charge_burst；item evolution / shard economy (copies)

**备注**：Param = RarityStatParam.From(rarity) = rarity+1 (Player/Inventory/RarityStatParam.cs:5-9, native 0x146639c). Extra copies consume spawner charges, so total copies per big cycle stay bounded by charges+1.

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

### `meta.spawner_bonus_charges`

**Spawner charges before big respawn**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
charges = AB_override ? ABTest.GetSpawnerCharges(IsEliteBoss, LocationNumber, Count) : ChargesBeforeBigRespawn + (ItemReward ? (int) items_rarity_charges[rarity+1] : 0)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ChargesBeforeBigRespawn` | authored charges | EnemySpawner.ChargesBeforeBigRespawn |
| `items_rarity_charges[r]` | Flat extra charges per rarity (amulet graph Params 1..3) | MetaStat items_rarity_charges |
| `AB_override` | !IsEvent ∧ !IsVip ∧ ItemReward==null ∧ Cooldown ≥ 10 | Enemy/EnemySpawner.cs:717-720 |

**输出**：kills available before the BigCooldown

**代码**：[Meta/SpawnerChargesCalculator.cs:23-41](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SpawnerChargesCalculator.cs#L23)；[Enemy/EnemySpawner.cs:697-715](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L697)

**状态依据**：No native annotation (per-frame cache keyed by ModifierVersion).

**配置参数**：`EnemySpawner.ChargesBeforeBigRespawn`；`EnemySpawner.BigCooldown`；`Meta Stats Config.Stats[items_rarity_charges]`

**使用方**：meta.zero_cooldown_charge_burst；meta.item_drop_copies_per_kill；simulation.soul_rpm (GetSpawnerChargesLimit)

### `meta.spawner_timer`

**Spawner small-cooldown meta reduction**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
t0 = AB_override ? ABTest.GetSpawnerCooldownSeconds(IsEliteBoss, LocationNumber) : Cooldown; t' = (t0 ≤ 0) ? t0 : ItemReward ? t0 / max(1, items_rarity_timer[rarity+1]) : !IsEvent ? max(0, t0 - map_spawner_timer_reduce[LocationNumber]) : t0; smallCooldown = ceil(t')
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Cooldown` | authored small cooldown (s) | EnemySpawner.Cooldown |
| `items_rarity_timer[r]` | timer divisor for item spawners (base 1; amulet nodes add 2*level-1 flat, e.g. Native_15b650c) | MetaStat items_rarity_timer |
| `map_spawner_timer_reduce[loc]` | seconds subtracted for stat spawners of an island | amulet graph, Params 1..14 |

**输出**：respawn cooldown seconds

**代码**：[Meta/SpawnerTimerCalculator.cs:9-24](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SpawnerTimerCalculator.cs#L9)；[Enemy/EnemySpawner.cs:687-695](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L687)

**状态依据**：No native annotation.

**配置参数**：`EnemySpawner.Cooldown`；`EnemySpawner.ItemReward`；`EnemySpawner.IsEvent`；`EnemySpawner.LocationNumber`；`Meta Stats Config.Stats[items_rarity_timer].BaseValue=1`；`Meta Stats Config.Stats[map_spawner_timer_reduce]`

**使用方**：meta.zero_cooldown_charge_burst；simulation (spawner.BaseCooldown)

**备注**：The divisor applies to item spawners and the subtraction applies to stat spawners. BigCooldown is not touched by the meta stats.

### `meta.spawner_currency_reward`

**Spawner flat currency reward**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
if CurrencyReward ≠ ∅ ∧ CurrencyRewardAmount > 0: balance[CurrencyReward] += CurrencyRewardAmount
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `CurrencyRewardAmount` | flat amount per devour (default 1) | EnemySpawner.CurrencyRewardAmount |

**输出**：currency balance

**代码**：[Enemy/Commands/GrantSpawnerCurrencyRewardCommandHandler.cs:15-28](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerCurrencyRewardCommandHandler.cs#L15)

**状态依据**：No annotation.

**配置参数**：`EnemySpawner.CurrencyReward`；`EnemySpawner.CurrencyRewardAmount`

**备注**：Only one spawner in Main.unity uses it (empty_amulet). No meta, cheat or devour multiplier applies.

### `meta.crystal_event_mob_stat_reward`

**Crystal-event mob devour stat reward**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
type = blue_crystal ? DefenceFlat(eventWeaponType) : DamageFlat(eventResistType); v = max(1, RPM[type] * 0.4) appended to unit.StatRewards (Flat, Source Enemy)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `RPM[type]` | simulation reward-per-minute for the stat | SimulationSystem.RPMCalculatedValue |

**输出**：extra unit stat reward, then scaled by meta.devour_stat_reward_scaling (stat_drop Param -1)

**代码**：[Enemy/EnemySpawner.cs:979-992](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L979)

**状态依据**：No annotation at this block.

**使用方**：meta.devour_stat_reward_scaling

**备注**：Defined in EnemySpawner (enemy subsystem). Listed because it is a devour reward source.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression`：Applies only if the current event and spawner EventDef are crystal events, UnitType == Mob, the variant currency is blue_crystal or red_crystal, and RPM[type] > 0. Then v = max(1, RPM[type]*0.4) is appended. If RPM is missing or ≤ 0, nothing is appended (there is no floor of 1).  
  依据：[Enemy/EnemySpawner.cs:981-991](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L981) (line 990 guard rpm > 0.0)

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

### `meta.grant_reward_stat`

**Generic StatReward grant (quests/shop/pass/mail/events)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
(type, v) = baked ? (baked.Type, baked.Value) : (IsRandom ? U(randomStats) : Type, Value * (UseGpm ? RPM[type] : 1)); v *= cheat.DropMultiplier; if v ≤ 0 skip; add modifier {Type = CustomModifier ? ModifierType : Flat, Source = CustomModifier ? Source : (override ?? Reward), Name = CustomModifier ? Name : sourcePath}; if Flat Health: CurrentHealth += v
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `randomStats` | {Health, HealthRegen, CutDmgFlat, StabDmgFlat, BuckshotDmgFlat, CutDefFlat, StabDefFlat, BuckshotDefFlat} | Rewards/Commands/GrantRewardCommandRunner.cs:69-79 |
| `RPM[type]` | simulation reward-per-minute | SimulationSystem.RPMCalculatedValue |

**输出**：permanent player stat modifier

**代码**：[Rewards/Commands/GrantRewardCommandRunner.cs:190-253](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/Commands/GrantRewardCommandRunner.cs#L190)

**状态依据**：GrantRewardCommandRunner has no native annotations.

**配置参数**：`StatReward.Value`；`StatReward.UseGpm`；`StatReward.IsRandom`；`StatReward.SaveKey`

**使用方**：[Rewards/Flows/GrantRewardsFlowRunner.cs:16-31](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/Flows/GrantRewardsFlowRunner.cs#L16)；shop/pass/vip/event reward lists

**备注**：This path bypasses PlayerRewardSystem, so there is no VIP or DoubleLoot multiplier. Baked data is preferred when SaveKey is set. Other handlers: Currency += Amount (only if >0), SoulReward adds souls, SoulPass grants Amulet Config.PassOneTimeDivineSouls (=1) divine souls, ChaoticSoulOffer adds 1 chaotic soul, Gems +=, VipPoints add to InAppsBoughtValueInRewarded.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression`：Three branches. (a) A completed event exists with !IsProgressComplete: use reward.BakedData, or the BakedRewardStorage[SaveKey] entry when (IsRandom ∨ UseGpm) ∧ SaveKey is set; if neither exists, resolve dynamically. (b) Otherwise, when (IsRandom ∨ UseGpm) ∧ SaveKey is set: use the stored bake if one exists, ELSE the raw (Type, Value) with no random pick and no RPM scaling. (c) Otherwise resolve dynamically. Dynamic resolution: type = IsRandom ? U(8 default stats, not RPM-filtered) : Type; v = Value * (UseGpm ? RPM[type] : 1). Then v *= cheat.DropMultiplier; skip if v ≤ 0. CurrentHealth += v whenever the stat is Health and the type is not a percent type (PA/PM/PAPM/PostFormulaMult) or EventPercent.  
  依据：[Rewards/Commands/GrantRewardCommandRunner.cs:194-228](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/Commands/GrantRewardCommandRunner.cs#L194) (the else-if branch at 208-219 never sets resolveDynamic, so a missing bake falls back to the raw Value); line 249 sets the Health condition.

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

### `meta.daily_rotation_stat`

**Daily rotation stat type**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x1419a2c`

```text
idx = (date_utc(now) - date_utc(installTime)).Days mod |DailyRotationStats|; type = entry.Empty ? null : entry.Type
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `installTime` | user install unix time | UserModel.installTime |

**输出**：StatType? for a DailyRotation StatReward

**代码**：[Rewards/StatReward.cs:27-36](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/StatReward.cs#L27)；[Rewards/DailyRotationStat.cs:7-12](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/DailyRotationStat.cs#L7)

**状态依据**：Comment 'Original ARM64 ELF VA 0x1419a2c'.

**配置参数**：`StatReward.DailyRotation`；`StatReward.DailyRotationStats[]`

**备注**：No call site was found. HandleStatReward uses reward.Type directly.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `used_by`：["Events/Services/EventForecastService.cs:226 (ProcessStatReward for crystal-event stage rewards; returns early when the rotation slot is Empty)"]  
  依据：grep for GetResolvedType finds [Events/Services/EventForecastService.cs:226](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Events/Services/EventForecastService.cs#L226)

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
| `chest_level` | (int) MetaStat chest_level.Value | meta.metastat_value_unfiltered |

**输出**：one item per chest opened

**代码**：[Shop/Flows/ChestOpenFlowRunner.cs:128-197](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/Flows/ChestOpenFlowRunner.cs#L128)；[Shop/ChestsModel.cs:96-108](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L96)；[Shop/ChestsModel.cs:140-165](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L140)；[Rewards/ChestsConfig.cs:11-22](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/ChestsConfig.cs#L11)

**状态依据**：Chest opening code has no native annotation. ChestsConfig.Get/TryGet are native (0x1417f9c, 0x1418098).

**配置参数**：`Chests Config.Chests[].Drops[].ItemType`；`Chests Config.Chests[].Drops[].Rarity`；`Chests Config.Chests[].Drops[].Weight`

**使用方**：shop/chest queue (ChestReward, GrantRewardsFlow)

**备注**：Asset weights: knightly C50/U30/R20 (weapon), armor C60/U30/R10, royal E85/L15, royal_armor E80/L20, general and crystal empty. The royal weights are overridden at runtime by the chest_level legendary formula and are display-only.

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
| `L` | chest level = 1 + Σ amulet chest_level flats + VIP level | meta.metastat_value_unfiltered, meta.chest_level_vip |

**输出**：copies granted per chest item

**代码**：[Shop/ChestsModel.cs:150-159](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L150)；[Shop/Flows/ChestOpenFlowRunner.cs:106](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/Flows/ChestOpenFlowRunner.cs#L106)

**状态依据**：No annotation.

**配置参数**：`Meta Stats Config.Stats[chest_level].BaseValue=1`

**使用方**：meta.chest_drop_weighted

**备注**：Defined in the Shop subsystem. Listed because chest_level is a meta stat.

### `meta.chest_level_vip`

**VIP chest level bonus**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
chest_level += vipLevel (Flat modifier 'vip_chest_level', Source Vip, replaced whenever the VIP level changes)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `vipLevel` | VIP level | VipModel.Level |

**输出**：chest_level meta stat

**代码**：[Shop/ChestsModel.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs) (ApplyVipModifier)

**状态依据**：No annotation.

**使用方**：meta.chest_copies；meta.chest_drop_weighted

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `code_refs`：Shop/ChestsModel.cs:91, 116-138. The modifier is added only when vipLevel > 0 and is removed by reference before being re-added.  
  依据：ChestsModel.cs:127-137

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

### `meta.upgrade_total_preview`

**Upgrade total-value preview at a target level**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x148a848..0x148d2d4`

```text
Total(target) = Σ_{applied m on stat, Source = src, Type = def.Type, param-matched} m.Value - g(current) + g(target)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `g` | modifier value lambda | UpgradeModifierDef |

**输出**：previewed meta stat contribution

**代码**：[Meta/Upgrades/UpgradeModel.cs:90-110](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeModel.cs#L90)

**状态依据**：Native class header.

**使用方**：upgrade UI / calculator

**备注**：Preview only. Confirms that per-level values replace each other and do not accumulate.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression`：Total = Σ over all modifiers on the target meta stat with Source == model source ∧ ModifierType == mod.ModifierType ∧ (DisplayParam == null ∨ (StringParam empty ∧ (Param == 0 ∨ Param == DisplayParam))). With no DisplayParam there is no param filter, and the sum includes other defs' modifiers. Total(target) = Total - g(current) + g(target).  
  依据：[Meta/Upgrades/UpgradeModel.cs:95-96](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeModel.cs#L95), 109, 147

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

### `meta.rewarded_ad_stat_value`

**Rewarded-ad stat offer value**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x14c61ec, 0x14c56b8, 0x15a9df8`

```text
value = round( d * abRewardedValueMult_EnemyHpTest * RPM[type] * 6 * abRewardedStatValueMult(lastUnlockedLocation) ); d = 2 if Minutes ≥ 30 ∧ U < (1/exp(0.1*density) - 0.5), else 1; density = (InAppsBoughtValueInRewarded + RewardedWatchTimes) / (max(1, Minutes - totalRewardedDelay) / 60)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `RPM[type]` | simulation stat reward per minute (the offer is worth 6 minutes of farming) | SimulationSystem.RPMCalculatedValue |
| `type` | picked from the RewardedConfig pool | meta.rewarded_pool_pick |

**输出**：Flat permanent stat modifier 'ads' (Source Ads) via PlayerRewardSystem

**代码**：[Integrations/Ads/AdsInitialize.cs:309-318](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Integrations/Ads/AdsInitialize.cs#L309)；[Integrations/Ads/AdsInitialize.cs:216-226](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Integrations/Ads/AdsInitialize.cs#L216)；[Analytics/UserModel.cs:398-403](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Analytics/UserModel.cs#L398)

**状态依据**：Comments 'Native ELF VA 0x14c61ec' and 'Original ARM64 ELF VA 0x15a9df8'.

**配置参数**：`AB CommonRewardMultipliers.rewardedStatValueMultiplier`；`AB EnemyHpDamageMult.rewardedValueMultiplier`

**使用方**：meta.devour_stat_grant

**备注**：The double chance is ≤ 0.5 and falls as ad/IAP density rises. It is 0 once density ≥ 10*ln2 ≈ 6.93 per hour.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `code_refs`：Integrations/Ads/AdsInitialize.cs:287-297 (value computation in Update); 311-317 is ResetReward and has no math  
  依据：AdsInitialize.cs:288-296 holds density, exp, the doubled roll, the value product and Math.Round

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

### `meta.rewarded_dynamic_weights`

**Rewarded-ad dynamic weight adaptation (AB)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x14c4e84, 0x14c6c08, 0x14c52dc`

```text
on watch: w[type] = (w[type] ?? 1) + 1 (2 if unset); on skip: w[type] = max(0.5, 0.9 * (w[type] ?? 1)^0.9) and type is excluded next roll; P(type) ∝ w (default 1) over pool entries with RPM > 0
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `w` | saved per-stat weight | UserModel.RewardedWeights |

**输出**：adapted offer type distribution (replaces the asset weights when the AB flag is on)

**代码**：[Integrations/Ads/AdsInitialize.cs:195-201](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Integrations/Ads/AdsInitialize.cs#L195)；[Integrations/Ads/AdsInitialize.cs:251-259](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Integrations/Ads/AdsInitialize.cs#L251)；[Integrations/Ads/AdsInitialize.cs:327-350](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Integrations/Ads/AdsInitialize.cs#L327)

**状态依据**：Native VA comments on each method.

**配置参数**：`AB IsDynamicRewardedWeights`

**使用方**：meta.rewarded_pool_pick

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `code_refs`：Integrations/Ads/AdsInitialize.cs:249-256 (skip decay), 195-201 (watch increment), 327-352 (weighted roll excluding LastSkippedRewardKey)  
  依据：AdsInitialize.cs:254 Mathf.Max(0.5f, Mathf.Pow(current,0.9f)*0.9f)

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

### `meta.spawner_charge_upgrade_topup`

**Spawner charge top-up when capacity rises**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：核验时补充

```text
init: if RemainingCharges < 0: RemainingCharges = effCharges - SavedCount; if KnownMax < 0: KnownMax = effCharges. On meta change: total = effCharges; if total > 0 ∧ total > KnownMax: RemainingCharges = min(total, RemainingCharges + (total - KnownMax)), nextProcessTime = 0; if total ≠ KnownMax: KnownMax = total
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `effCharges` | effective charges before the big respawn | meta.spawner_bonus_charges (AB override or ChargesBeforeBigRespawn + items_rarity_charges[rarity+1]) |
| `KnownMax` | persisted last known max charges | spawner save data |

**输出**：remaining charges after an items_rarity_charges upgrade

**代码**：[Enemy/EnemySpawner.cs:561-567](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L561)；[Enemy/EnemySpawner.cs:569-585](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L569)；[Enemy/EnemySpawner.cs:587-592](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L587)

**状态依据**：No native comment on InitializePersistentState or ApplyChargeUpgradeIfIncreased.

**配置参数**：`EnemySpawner.ChargesBeforeBigRespawn`；`Meta Stats Config.Stats[items_rarity_charges]`

**使用方**：meta.item_drop_copies_per_kill；meta.zero_cooldown_charge_burst

**备注**：Buying a charges upgrade immediately grants the extra charges. A drop in capacity does not take charges back. Defined in EnemySpawner but driven by a meta stat.

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
| `L` | chest level (1 + amulet chest_level flats + VIP level) | meta.metastat_value_unfiltered |

**输出**：CrystalRarity for a crystal chest roll

**代码**：[Shop/ChestsModel.cs:23-24](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L23)；[Shop/ChestsModel.cs:60-71](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L60)；[Shop/ChestsModel.cs:99-114](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L99)；[Shop/ChestsModel.cs:167-171](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L167)

**状态依据**：No native annotation in ChestsModel.

**配置参数**：`Meta Stats Config.Stats[chest_level].BaseValue=1`

**使用方**：crystal chest opening (Crystals/Shop)

**备注**：Companion of the equipment rule 0.095 + 0.005L. Constants CrystalBlacksmithBonus = 3 and LegendaryArmorUnlockLocation = 14 also sit here.

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

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [已应用] `status`：reconstructed  
  依据：Mechanical audit 2026-10-27: none of the cited files contain a native address comment and native_va is empty; matches the completeness critic's status audit.

### `meta.event_currency_devour_reward`

**Event currency / progress on devour**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：核验时补充

```text
if CurrentEvent.Key == spawner.EventDef.Key: MiniBoss → Progress += 1; Boss → Progress += ProgressMax; if unit.EventUnitReward.CurrencyAmount > 0: eventCurrency[CurrencyKey] += CurrencyAmount
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `CurrencyAmount` | per-unit event currency | EnemyUnit.EventUnitReward (event variant) |

**输出**：event currency and event progress

**代码**：[Enemy/Commands/GrantSpawnerEventCurrencyRewardCommandHandler.cs:30-61](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerEventCurrencyRewardCommandHandler.cs#L30)；[Enemy/EnemySpawner.cs:1377](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1377)

**状态依据**：No native annotation in the handler.

**配置参数**：`event variant CurrencyKey/CurrencyAmount`

**使用方**：events subsystem

**备注**：Fires in HandleDevour, but unlike stats and souls it is NOT scaled by CurrentDevourRewardMultiplier, cheat.DropMultiplier, stat_drop or the charge burst.

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
