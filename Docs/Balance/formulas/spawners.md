# 刷怪点、岛屿与门（`spawners`）

[返回公式清单总览](../FormulaCatalog.md)

## 概述

Spawners are the game's only renewable source of permanent stats, items and souls. Each of the 717 EnemySpawner configs in Main.unity sets enemy base stats (always the six stats MoveSpeed, Health, HealthRegen, Damage, AttackRange and AttackCooldown), one reward StatModifier (577 spawners; always Flat/Enemy source), and a respawn schedule. The schedule has three parts: a small cooldown between respawns, charges (each spawn uses one), and a big cooldown. In 710 spawners the big cooldown is 86400 s synced to UTC, so charges refill at UTC midnight and charges become a daily kill budget. For ordinary stat spawners (not item, VIP or event, and Cooldown>=10) the scene's Cooldown and ChargesBeforeBigRespawn are overridden at runtime by the spawner_cooldowns_v3 AB data. Non-elite: cooldown = 2(L+1) minutes capped at 30, charges = 3*Count. Elite: cooldown = 60/90/120/150 minutes for L1-4 and 180 after, charges = 2*Count. Meta upgrades cut the cooldown (map_spawner_timer_reduce, a flat subtraction), divide item-spawner cooldowns (items_rarity_timer) and add charges (items_rarity_charges). When the reduced cooldown reaches 0, one kill uses up all remaining charges and multiplies the stat and soul reward. Rewards scale as reward.Value * stat_drop[LocationNumber] * multiplier. OptimumTime, RelativeOptimumTime, AverageFightTime, Difficulty, PriorityMultiplier and HandBalance are not read by live spawning. They feed the editor balance bake (Health and Damage are solved from Difficulty and AverageFightTime) and the simulation/forecast bot. Locations are opened by gate rewards on island-boss spawners (unlockedLocation = max(unlocked, NextLocationNumber)). Island mastery gives +0.1 per medal level, up to 5, each time every eligible spawner on an island has used all its charges.

**核验结论**：The catalog is largely accurate. I checked every formula against the code: expressions, constants, clamps and rounding in EnemySpawner (cooldown/charges/big cooldown/UTC sync/zero-cooldown collapse/item copies), the Meta calculators, the AB tables, the grant handlers, EnemyUnit, EnemyAttackSystem, EnemyRegenSystem, IslandMastery, Gates and the editor tools. Status labels are correct: EnemySpawner only has native VAs on SetupView (0x1503b50) and ApplyEventPlanToConfig (0x150639c); the stubs (TryInitializeEventSpawner, ApplyTimeSkip, the creeper system) and the empty editor tools are confirmed. No formula is rejected.

Corrections are mostly scene-fact and nuance fixes:
- 709 spawners (not 710) have BigCooldown 86400 with UTC sync; the creeper uses a rolling 24 h.
- Elite scene cooldowns are min(180, 30(L+2)) min, not 'mostly 10800'.
- 569 spawners are AB-eligible.
- 22 one-shot spawners get AB charges 3.
- The Health=Infinity spawner is a BossTower item spawner.
- All 10 VIP spawners have RequiredVipLevel 3, so a VIP-level check is likely missing.
- GateLocationTrigger uses a serialized locationNumber at runtime; the NextLocationNumber-isExit relation is editor-only.
- The days converter is a runtime static used by quests.
- 'Event spawning broken' is overstated: external callers spawn event units.
- ApplyTimeSkip, ConsumeAllCharges and DefeatAllUnits have no callers.
- The difficulty_semantics bake expression lacked crit, ability coefficients, the resist clamp and the round-to-10 damage. That bake is native (0x13db16c) and belongs as recovered_native under simulation.
- '../' code_ref prefixes should be dropped.

Added missed items:
- geode timer reduction per kill (KillCount = extra charges + multiplier)
- far-distance culling that disables enemy regen and AI (60/30 units)
- an IsDummy contradiction that stops the dummy spawner from ever spawning
- UTC wake scheduling for full or depleted spawners

Scene checks confirmed: all 717 spawners have stat types 0-5; 577 have one Flat/Enemy reward; 22/717 RelativeOptimumTime values are inconsistent with the island ranges; non-elite cooldown = min(1800,120(L+2)) s for 563/566.

## 公式索引

| ID | 名称 | 状态 | 类型 |
|---|---|---|---|
| [`spawners.ab_spawner_override_predicate`](#spawnersab_spawner_override_predicate) | Whether AB cooldown/charges override the scene config | 重建（无原生标注） | `config_rule` |
| [`spawners.ab_cooldown_table`](#spawnersab_cooldown_table) | AB spawner cooldown by location (spawner_cooldowns_v3) | 纯数据 | `lookup_table` |
| [`spawners.ab_charges_table`](#spawnersab_charges_table) | AB spawner charges | 纯数据 | `lookup_table` |
| [`spawners.base_cooldown`](#spawnersbase_cooldown) | Effective small (respawn) cooldown | 重建（无原生标注） | `runtime_formula` |
| [`spawners.effective_charges`](#spawnerseffective_charges) | Effective charges before big respawn | 重建（无原生标注） | `runtime_formula` |
| [`spawners.charge_state_init`](#spawnerscharge_state_init) | Charge state initialization and upgrade top-up | 重建（无原生标注） | `runtime_formula` |
| [`spawners.respawn_tick`](#spawnersrespawn_tick) | Respawn scheduling (small cooldown, catch-up spawns) | 重建（无原生标注） | `runtime_formula` |
| [`spawners.death_timer_reset`](#spawnersdeath_timer_reset) | Cooldown restart on kill | 重建（无原生标注） | `runtime_formula` |
| [`spawners.big_cooldown`](#spawnersbig_cooldown) | Big cooldown start and UTC alignment | 重建（无原生标注） | `runtime_formula` |
| [`spawners.charge_restore`](#spawnerscharge_restore) | Charge refill (UTC period roll-over or big cooldown expiry) | 重建（无原生标注） | `runtime_formula` |
| [`spawners.zero_cooldown_collapse`](#spawnerszero_cooldown_collapse) | Zero-cooldown kill multiplier (charge collapse) | 重建（无原生标注） | `runtime_formula` |
| [`spawners.item_drop_copies`](#spawnersitem_drop_copies) | Item copies per kill (item spawners) | 重建（无原生标注） | `runtime_formula` |
| [`spawners.stat_reward_grant`](#spawnersstat_reward_grant) | Devour stat reward (permanent growth) | 重建（无原生标注） | `runtime_formula` |
| [`spawners.soul_reward_grant`](#spawnerssoul_reward_grant) | Devour soul reward | 重建（无原生标注） | `runtime_formula` |
| [`spawners.currency_reward`](#spawnerscurrency_reward) | Spawner currency reward | 重建（无原生标注） | `runtime_formula` |
| [`spawners.enemy_spawn_stats`](#spawnersenemy_spawn_stats) | Enemy instance stats at spawn (incl. AB HP/damage multiplier) | 重建（无原生标注） | `runtime_formula` |
| [`spawners.enemy_attack_cadence`](#spawnersenemy_attack_cadence) | Enemy attack timing, aggro and movement | 重建（无原生标注） | `runtime_formula` |
| [`spawners.enemy_leash_heal`](#spawnersenemy_leash_heal) | Leash full-heal | 重建（无原生标注） | `runtime_formula` |
| [`spawners.enemy_regen`](#spawnersenemy_regen) | Enemy HP regeneration | 重建（无原生标注） | `runtime_formula` |
| [`spawners.event_damage_ramp_timer`](#spawnersevent_damage_ramp_timer) | Event unit damage-ramp timer | 重建（无原生标注） | `runtime_formula` |
| [`spawners.event_plan_override`](#spawnersevent_plan_override) | Event spawner forecast override | 原生证据恢复 | `runtime_formula` |
| [`spawners.event_crystal_mob_bonus`](#spawnersevent_crystal_mob_bonus) | Crystal event mob extra stat reward | 重建（无原生标注） | `runtime_formula` |
| [`spawners.event_variant_currency`](#spawnersevent_variant_currency) | Event variant pick and currency reward | 重建（无原生标注） | `runtime_formula` |
| [`spawners.event_miniboss_spawn_gate`](#spawnersevent_miniboss_spawn_gate) | Event mini-boss spawn threshold | 重建（无原生标注） | `config_rule` |
| [`spawners.event_spawner_init`](#spawnersevent_spawner_init) | Event spawner initialization | 空壳 | `runtime_formula` |
| [`spawners.time_skip`](#spawnerstime_skip) | Spawner time skip | 空壳 | `runtime_formula` |
| [`spawners.vip_spawner_gate`](#spawnersvip_spawner_gate) | VIP spawner unlock | 重建（无原生标注） | `config_rule` |
| [`spawners.disable_after_first_clear`](#spawnersdisable_after_first_clear) | One-shot spawners | 重建（无原生标注） | `config_rule` |
| [`spawners.gate_location_unlock`](#spawnersgate_location_unlock) | Gate opening and location unlock | 重建（无原生标注） | `runtime_formula` |
| [`spawners.location_trigger`](#spawnerslocation_trigger) | Location number from gate triggers | 重建（无原生标注） | `runtime_formula` |
| [`spawners.portal_gem`](#spawnersportal_gem) | Portal gem collection and portal unlock | 重建（无原生标注） | `config_rule` |
| [`spawners.slot_reward`](#spawnersslot_reward) | Item slot unlock reward | 重建（无原生标注） | `config_rule` |
| [`spawners.progress_markers`](#spawnersprogress_markers) | RewardedPoolIndex and user OptimumTime high-water marks | 重建（无原生标注） | `runtime_formula` |
| [`spawners.island_cleared_predicate`](#spawnersisland_cleared_predicate) | Island fully-cleared predicate (mastery) | 重建（无原生标注） | `config_rule` |
| [`spawners.island_medal_progress`](#spawnersisland_medal_progress) | Island medal level and bonus | 重建（无原生标注） | `runtime_formula` |
| [`spawners.creeper_explosion`](#spawnerscreeper_explosion) | Creeper mob explosion damage | 空壳 | `runtime_formula` |
| [`spawners.optimum_time_days_converter`](#spawnersoptimum_time_days_converter) | OptimumTime minutes <-> days conversion | 原生证据恢复 | `runtime_formula` |
| [`spawners.relative_optimum_time`](#spawnersrelative_optimum_time) | RelativeOptimumTime <-> OptimumTime within island | 编辑器工具缺失 | `editor_generator` |
| [`spawners.difficulty_bulk_setter`](#spawnersdifficulty_bulk_setter) | Bulk Difficulty setter | 编辑器工具缺失 | `editor_generator` |
| [`spawners.debugger_gpm_export`](#spawnersdebugger_gpm_export) | Spawner debugger GPM / CSV export | 编辑器工具缺失 | `editor_generator` |
| [`spawners.difficulty_semantics`](#spawnersdifficulty_semantics) | Difficulty / AverageFightTime meaning (bake inputs) | 纯数据 | `config_rule` |
| [`spawners.optimum_time_semantics`](#spawnersoptimum_time_semantics) | OptimumTime / IsDays meaning | 纯数据 | `config_rule` |
| [`spawners.priority_handbalance_semantics`](#spawnerspriority_handbalance_semantics) | PriorityMultiplier / HandBalance meaning | 纯数据 | `config_rule` |
| [`spawners.daily_yield`](#spawnersdaily_yield) | Derived per-spawner daily yield (model identity) | 重建（无原生标注） | `simulation` |
| [`spawners.geode_kill_count`](#spawnersgeode_kill_count) | Geode timer reduction per spawner kill | 重建（无原生标注） | `runtime_formula` |
| [`spawners.enemy_far_culling`](#spawnersenemy_far_culling) | Enemy far-distance culling (disables regen, AI, visuals) | 重建（无原生标注） | `config_rule` |
| [`spawners.dummy_spawn_block`](#spawnersdummy_spawn_block) | Dummy spawner never spawns (reconstruction inconsistency) | 重建（无原生标注） | `config_rule` |
| [`spawners.full_or_depleted_wake_schedule`](#spawnersfull_or_depleted_wake_schedule) | Wake-up scheduling for full or charge-depleted spawners | 重建（无原生标注） | `runtime_formula` |
| [`spawners.item_copies_reward_multiplier_coupling`](#spawnersitem_copies_reward_multiplier_coupling) | Item-drop copies become the devour stat and soul multiplier | 重建（无原生标注） | `runtime_formula` |
| [`spawners.unconsumed_numeric_fields`](#spawnersunconsumed_numeric_fields) | Numeric config fields with no consumer | 纯数据 | `config_rule` |

## 公式

### `spawners.ab_spawner_override_predicate`

**Whether AB cooldown/charges override the scene config**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`config_rule`
- 来源：子系统梳理

```text
usesAB = !IsEvent && !IsVip && ItemReward == null && Cooldown >= 10
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `IsEvent,IsVip,ItemReward,Cooldown` | spawner config fields | Main.unity EnemySpawner.config |

**输出**：bool: if true, cooldown and charges come from ABTestService instead of config.Cooldown / config.ChargesBeforeBigRespawn

**代码**：[Enemy/EnemySpawner.cs:717-720](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L717)；[Enemy/EnemySpawner.cs:693](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L693)；[Enemy/EnemySpawner.cs:709](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L709)；[Enemy/EnemySpawner.cs:714](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L714)

**状态依据**：No native VA comment on EnemySpawner spawn/charge methods (only SetupView 0x1503b50 and ApplyEventPlanToConfig 0x150639c are annotated).

**配置参数**：`EnemySpawnerConfig.Cooldown`；`EnemySpawnerConfig.IsEvent`；`EnemySpawnerConfig.IsVip`；`EnemySpawnerConfig.ItemReward`

**使用方**：spawners.base_cooldown；spawners.effective_charges

**备注**：Scene data: 127 item spawners, 10 VIP, 6 event and 8 with Cooldown<10 (fast packs, dummy, crystal, event bosses) keep their config values. For about 570 ordinary stat spawners the scene Cooldown/ChargesBeforeBigRespawn are only legacy or baked reference values.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：Exactly 569 spawners meet the AB predicate. The 148 excluded are 127 item + 10 VIP + 6 event + 8 with Cooldown<10, minus 3 overlaps (event spawners with Cooldown 5). DisableAfterFirstClear and IsSpecial spawners are NOT excluded by the predicate.  
  依据：Scene parse with predicate !IsEvent && !IsVip && ItemReward==null && Cooldown>=10 gives 569; EnemySpawner.cs:717-720.

### `spawners.ab_cooldown_table`

**AB spawner cooldown by location (spawner_cooldowns_v3)**

- 状态：**纯数据**（`data_only`）
- 类型：`lookup_table`
- 来源：子系统梳理

```text
abCooldownSec(isElite, L) = 60 * (overrides[L] ?? default). Non-elite: overrides {1:4,2:6,...,13:28} = 2(L+1) minutes, default 30. Elite: overrides {1:60,2:90,3:120,4:150}, default 180 minutes
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `L` | config.LocationNumber | EnemySpawnerConfig.LocationNumber |
| `isElite` | config.IsEliteBoss | EnemySpawnerConfig.IsEliteBoss |

**输出**：seconds (before meta timer reduction)

**代码**：../[ABTests/ABTestService.cs:44](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L44)；../[ABTests/SpawnerCooldownsData.cs:8-10](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/SpawnerCooldownsData.cs#L8)

**状态依据**：Hard-coded default data class. A remote AB variant can replace it.

**配置参数**：`ABTestData.SpawnerCooldowns (remote AB spawner_cooldowns_v3)`

**使用方**：spawners.base_cooldown

**备注**：Defined in ABTests and listed here because it controls spawner timing. Scene values differ: non-elite scene Cooldown = min(1800, 120*(L+2)) s (L1=360 ... L13+=1800), so the AB default is 2 minutes shorter for L1-13. Elite scene Cooldown is mostly 10800 (=180 min).

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：Elite scene Cooldown is L1=5400, L2=7200, L3=9000 and L4+=10800, i.e. min(180, 30(L+2)) minutes. The AB elite default is 30(L+1) min for L1-4, so it is 30 min shorter at L1-4 and equal (180) from L5. Non-elite scene Cooldown = min(1800,120(L+2)) s holds for 563 of the 566 AB-eligible non-elites. The exceptions are one L2 spawner (1800), one L5 spawner (12600) and one L8 spawner (1800).  
  依据：Scene parse: elite cooldowns by location (('1','5400'),('2','7200'),('3','9000'),('4'..'17','10800')); [ABTests/SpawnerCooldownsData.cs:10](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/SpawnerCooldownsData.cs#L10).

### `spawners.ab_charges_table`

**AB spawner charges**

- 状态：**纯数据**（`data_only`）
- 类型：`lookup_table`
- 来源：子系统梳理

```text
abCharges(isElite, L, Count) = (overrides[L] ?? default) * Count. Default 3 (non-elite) or 2 (elite), no overrides
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Count` | max simultaneous units of the spawner | EnemySpawnerConfig.Count |

**输出**：charges per big-cooldown cycle (per UTC day)

**代码**：../[ABTests/ABTestService.cs:45](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L45)；../[ABTests/SpawnerCooldownsData.cs:8-10](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/SpawnerCooldownsData.cs#L8)

**状态依据**：Hard-coded AB default data.

**配置参数**：`ABTestData.SpawnerCooldowns.charges`

**使用方**：spawners.effective_charges

**备注**：Scene ChargesBeforeBigRespawn for multi-unit packs already equals 3*Count (e.g. Count 6 has 18, Count 20 has 60). Elites all have 2. Item, VIP and event charges come from the scene.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：True for packs. However, 22 AB-eligible non-elite spawners have scene charges=1 with Count=1: 19 'ice pike blocker' at L10, the L4 dragon, L5 Spawner_elite_regen and L17 Spawner_17_death. All 22 are DisableAfterFirstClear, and AB raises them to 3. This has no effect, because they disable after the first clear (EnemySpawner.cs:775-783).  
  依据：Scene parse: 22 non-elite AB-eligible spawners where ChargesBeforeBigRespawn != 3*Count, all (Count 1, charges 1) and DisableAfterFirstClear=1.

### `spawners.base_cooldown`

**Effective small (respawn) cooldown**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
raw = usesAB ? abCooldownSec(IsEliteBoss, L) : config.Cooldown. If raw <= 0 then cd = raw. Else if ItemReward != null then cd = raw / max(1, items_rarity_timer[rarityParam(ItemReward.Rarity)]). Else if !IsEvent then cd = max(0, raw - map_spawner_timer_reduce[L]). Else cd = raw. smallCooldownSec = ceil(cd)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `raw` | base respawn seconds | spawners.ab_cooldown_table or EnemySpawnerConfig.Cooldown |
| `items_rarity_timer` | meta stat divisor for item spawners, keyed by rarity | Meta Stats Config.asset / Meta/SpawnerTimerCalculator.cs:21-22 |
| `map_spawner_timer_reduce` | meta stat flat seconds removed from stat-spawner timers, keyed by LocationNumber | Meta/SpawnerTimerCalculator.cs:16-20 |

**输出**：BaseCooldown (float) and GetSmallCooldownSeconds (int, ceil)

**代码**：[Enemy/EnemySpawner.cs:686-695](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L686)；[Enemy/EnemySpawner.cs:171-177](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L171)；../[Meta/SpawnerTimerCalculator.cs:9-24](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SpawnerTimerCalculator.cs#L9)

**状态依据**：EnemySpawner.GetBaseCooldown and SpawnerTimerCalculator have no native VA annotation.

**配置参数**：`EnemySpawnerConfig.Cooldown`；`EnemySpawnerConfig.IsEliteBoss`；`EnemySpawnerConfig.LocationNumber`；`EnemySpawnerConfig.ItemReward`；`Meta Stats Config.items_rarity_timer`；`Meta Stats Config.map_spawner_timer_reduce`

**使用方**：spawners.respawn_tick；spawners.zero_cooldown_collapse；[Simulation/Systems/SimulationSystem.cs:411](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L411) (BaseCooldown)；UI SpawnerView / minimap cooldown display

**备注**：Reduction for stat spawners is a flat subtraction, so a large enough reduction drives cd to 0, which triggers spawners.zero_cooldown_collapse. Item spawners use division instead, so they never reach 0.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `code_refs`：Meta/SpawnerTimerCalculator.cs:9-24. All '../Meta/...', '../ABTests/...', '../Simulation/...', '../Player/...' and '../Services/...' refs are directories directly under CodeBase, so drop the '../' prefix.  
  依据：find shows [CodeBase/Meta/SpawnerTimerCalculator.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/CodeBase/Meta/SpawnerTimerCalculator.cs), [CodeBase/ABTests/SpawnerCooldownsData.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/CodeBase/ABTests/SpawnerCooldownsData.cs), etc.

### `spawners.effective_charges`

**Effective charges before big respawn**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
chargesTotal = usesAB ? abCharges(IsEliteBoss, L, Count) : ChargesBeforeBigRespawn + (ItemReward != null ? floor(items_rarity_charges[rarityParam(ItemReward.Rarity)]) : 0). chargesEnabled = !IsEvent && chargesTotal > 0
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ChargesBeforeBigRespawn` | configured charge budget | EnemySpawnerConfig.ChargesBeforeBigRespawn |
| `items_rarity_charges` | meta bonus charges for item spawners by rarity | Meta/SpawnerChargesCalculator.cs:23-41 |

**输出**：ChargesTotal (int, cached per frame). ChargesBase = the same without the meta bonus

**代码**：[Enemy/EnemySpawner.cs:697-715](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L697)；[Enemy/EnemySpawner.cs:307-337](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L307)；../[Meta/SpawnerChargesCalculator.cs:40-41](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SpawnerChargesCalculator.cs#L40)

**状态依据**：No native annotation on these EnemySpawner/SpawnerChargesCalculator methods.

**配置参数**：`EnemySpawnerConfig.ChargesBeforeBigRespawn`；`EnemySpawnerConfig.Count`；`EnemySpawnerConfig.IsEliteBoss`；`Meta Stats Config.items_rarity_charges`

**使用方**：spawners.respawn_tick；spawners.charge_restore；spawners.island_cleared_predicate；[Simulation/Systems/SimulationSystem.cs:608-612](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L608) GetSpawnerChargesLimit；spawners.daily_yield

**备注**：If charges are disabled (ChargesBeforeBigRespawn=0 on a non-AB spawner, or an event spawner), the spawner respawns every small cooldown with no daily cap. Scene distribution: 3 (438), 2 (54), 6 (48), 8 (39), 1 (34), 4 (29), 9 (20), 15 (14), 18 (12), 0 (7).

### `spawners.charge_state_init`

**Charge state initialization and upgrade top-up**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
On first load (RemainingCharges==-1): Remaining = chargesTotal - SavedCount and KnownMax = chargesTotal. When the AB version or the items_rarity_charges modifier version changes and chargesTotal > KnownMax: Remaining = min(chargesTotal, Remaining + chargesTotal - KnownMax). Then KnownMax = chargesTotal
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `SavedCount` | units alive at save | SpawnerDataUd.saved_count |
| `KnownMax` | last known charge cap | SpawnerDataUd.known_max_charges |

**输出**：RemainingCharges

**代码**：[Enemy/EnemySpawner.cs:561-567](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L561)；[Enemy/EnemySpawner.cs:569-585](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L569)；[Enemy/EnemySpawner.cs:656-665](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L656)；[Enemy/SpawnerDataUd.cs:14-23](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/SpawnerDataUd.cs#L14)

**状态依据**：No native annotation.

**配置参数**：`EnemySpawnerConfig.ChargesBeforeBigRespawn`

**使用方**：spawners.respawn_tick

**备注**：Buying a charge upgrade grants the extra charges at once on the current day. A decrease never removes charges already held.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：The upgrade check runs when the source version changes. The source is SpawnerCooldownsVersion for AB-eligible spawners, items_rarity_charges.ModifierVersion for item spawners, and 0 otherwise (EnemySpawner.cs:660-664). The initial condition is RemainingCharges<0 (UdValue default -1), not ==-1 exactly.  
  依据：EnemySpawner.cs:561-567, 654-665; [Enemy/SpawnerDataUd.cs:18](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/SpawnerDataUd.cs#L18),22.

### `spawners.respawn_tick`

**Respawn scheduling (small cooldown, catch-up spawns)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
if Count - alive > 0: cd = smallCooldownSec; chargesAvail = chargesEnabled ? Remaining : INF. If cd <= 0: spawn min(missing, chargesAvail) units now. Else: n = min(floor((now - LastSpawnTime)/cd), missing, chargesAvail); if n <= 0 then Cooldown = cd - (now - LastSpawnTime); else spawn n units, set LastSpawnTime = now, and Cooldown = cd - (now - LastSpawnTime). nextProcessTime = (NextBigSpawnTime > now) ? NextBigSpawnTime : (alive < Count ? (cd > 0 ? max(now, LastSpawnTime + cd) : 0) : 0)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `now` | unix seconds from ITimeService | runtime |
| `LastSpawnTime` | persisted last spawn time | SpawnerDataUd.last_spawn_time |
| `Count` | max simultaneous alive units | EnemySpawnerConfig.Count |

**输出**：units spawned, Cooldown display, nextProcessTime

**代码**：[Enemy/EnemySpawner.cs:744-858](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L744)；[Enemy/EnemySpawner.cs:1342-1357](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1342)；[Enemy/EnemySpawner.cs:656-680](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L656)；[Enemy/Systems/EnemySpawnSystem.cs:34-49](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/EnemySpawnSystem.cs#L34)

**状态依据**：No native annotation on ProcessSpawnTick/UpdateNextProcessTime.

**配置参数**：`EnemySpawnerConfig.Count`；`EnemySpawnerConfig.Cooldown`

**使用方**：spawners.daily_yield

**备注**：Offline catch-up: on load, all elapsed whole cooldown periods spawn at once (capped by missing units and charges). Restore spawns (SavedCount) do not use charges. Each Normal spawn uses 1 charge (EnemySpawner.cs:1042-1043): charges limit spawns, not kills. Spawning is skipped while SimulationSystem.IsRunning.

### `spawners.death_timer_reset`

**Cooldown restart on kill**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
on death: if (aliveBefore >= Count || aliveAfter == 0) and not event mini-boss then LastSpawnTime = now and Cooldown = smallCooldownSec
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `aliveBefore` | units.Count before removing the dead unit | runtime |

**输出**：LastSpawnTime / Cooldown

**代码**：[Enemy/EnemySpawner.cs:1221-1231](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1221)

**状态依据**：No native annotation on HandleDeath.

**配置参数**：`EnemySpawnerConfig.Count`

**使用方**：spawners.respawn_tick

**备注**：For a pack, the timer starts on the first kill from a full pack, and again when the pack is emptied. Kills in between do not reset it.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：Same as before. For event mini-boss spawners (non-roster), the small-cooldown restart happens on DEVOUR instead (HandleDevour sets LastSpawnTime=now and Cooldown=smallCooldownSec). For Count=1 spawners, countBefore>=Count always holds, so every kill restarts the timer.  
  依据：[Enemy/EnemySpawner.cs:1221-1226](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1221) and 1362-1371.

### `spawners.big_cooldown`

**Big cooldown start and UTC alignment**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
bigPeriod = ceil(BigCooldown > 0 ? BigCooldown : Cooldown). When chargesEnabled && Remaining <= 0 && alive == 0 && NextBigSpawnTime <= 0: NextBigSpawnTime = SyncBigRespawnToUtc ? bigPeriod*floor(now/bigPeriod) + bigPeriod : now + bigPeriod. Cooldown = NextBigSpawnTime - now; LastSpawnTime = 0
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `BigCooldown` | seconds of the big respawn | EnemySpawnerConfig.BigCooldown |
| `SyncBigRespawnToUtc` | align to UTC period boundaries | EnemySpawnerConfig.SyncBigRespawnToUtc |

**输出**：NextBigSpawnTime

**代码**：[Enemy/EnemySpawner.cs:355-361](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L355)；[Enemy/EnemySpawner.cs:1084-1101](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1084)；[Enemy/EnemySpawner.cs:1047-1054](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1047)；[Enemy/EnemySpawner.cs:865-882](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L865)

**状态依据**：No native annotation.

**配置参数**：`EnemySpawnerConfig.BigCooldown`；`EnemySpawnerConfig.SyncBigRespawnToUtc`；`EnemySpawnerConfig.Cooldown`

**使用方**：spawners.charge_restore；UI SpawnerView/minimap (BigCooldownSeconds)

**备注**：Scene: 710/717 spawners have BigCooldown=86400 and Sync=1, giving a daily reset at 00:00 UTC no matter when the charges were used up.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：709/717 have BigCooldown=86400 AND SyncBigRespawnToUtc=1, so they reset at 00:00 UTC. Spawner_Creeper has BigCooldown=86400 with Sync=0, so its 24 h refill starts when its charges run out and is not aligned to UTC. Spawner_Crystal has BigCooldown=5 with Sync=1. The 6 event spawners have 0/0. BigCooldownSeconds falls back to config.Cooldown (the scene value, never the AB value) when BigCooldown<=0 (EnemySpawner.cs:355-361).  
  依据：Scene parse of all 717 EnemySpawner configs: Counter{(86400,1):709,(0,0):6,(5,1):1,(86400,0):1}.

### `spawners.charge_restore`

**Charge refill (UTC period roll-over or big cooldown expiry)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
If SyncBigRespawnToUtc: p = floor(now / bigPeriod); if ChargesSyncPeriod != 0 && ChargesSyncPeriod != p then Remaining = chargesTotal - alive, NextBigSpawnTime = 0, and ChargesSyncPeriod = p. Also, if 0 < NextBigSpawnTime <= now then NextBigSpawnTime = 0 and Remaining = chargesTotal
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ChargesSyncPeriod` | index of the last UTC period seen | SpawnerDataUd.charges_sync_period |

**输出**：RemainingCharges

**代码**：[Enemy/EnemySpawner.cs:1056-1082](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1056)

**状态依据**：No native annotation.

**配置参数**：`EnemySpawnerConfig.BigCooldown`；`EnemySpawnerConfig.SyncBigRespawnToUtc`

**使用方**：spawners.daily_yield；spawners.island_cleared_predicate

**备注**：With UTC sync, unused charges do not carry over. Each new UTC day sets the budget back to chargesTotal minus units already alive.

### `spawners.zero_cooldown_collapse`

**Zero-cooldown kill multiplier (charge collapse)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
if smallCooldownSec == 0 && chargesEnabled && unit.ItemReward == null: mult = max(1, Remaining + 1) and Remaining = 0. Else mult = 1. unit.SpawnerStatRewardMultiplier = mult
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Remaining` | charges left at kill time | SpawnerDataUd.remaining_charges |

**输出**：SpawnerStatRewardMultiplier (multiplies stat reward, soul reward, OnKill ability context, geode timer kill count)

**代码**：[Enemy/EnemySpawner.cs:722-732](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L722)；[Enemy/EnemySpawner.cs:1186](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1186)；[Enemy/EnemySpawner.cs:1213](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1213)；../[Abilities/Core/AbilityContext.cs:33](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Core/AbilityContext.cs#L33)

**状态依据**：No native annotation on ConsumeZeroCooldownStatRewardMultiplier.

**配置参数**：`Meta Stats Config.map_spawner_timer_reduce`；`EnemySpawnerConfig.Cooldown`

**使用方**：spawners.stat_reward_grant；spawners.soul_reward_grant

**备注**：This is a design lever: once the map_spawner_timer_reduce upgrade reaches the location's cooldown, one kill pays the whole day's budget. With AB defaults that is 2(L+1) minutes for non-elites; the remaining charges are minus 1 for the spawn that already used one.

### `spawners.item_drop_copies`

**Item copies per kill (item spawners)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
perKill = 1 + floor(items_rarity_drop_amount[rarityParam(r)]). If chargesEnabled: copies = min(perKill, Remaining + 1); Remaining -= copies - 1. Else copies = perKill. Then unit.SpawnerStatRewardMultiplier = copies
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `items_rarity_drop_amount` | meta flat bonus copies by rarity | Meta/ItemDropAmountCalculator.cs:8-13 |
| `r` | ItemReward.Rarity | item ScriptableObject (e.g. 'cut common.asset') |

**输出**：copies granted via PlayerModel.CreateItem(item, copies)

**代码**：[Enemy/EnemySpawner.cs:1191-1215](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1191)；../[Meta/ItemDropAmountCalculator.cs:15-25](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/ItemDropAmountCalculator.cs#L15)；[Enemy/Commands/GrantSpawnerItemRewardCommandHandler.cs:26-30](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerItemRewardCommandHandler.cs#L26)

**状态依据**：No native annotation on HandleDeath or ItemDropAmountCalculator.

**配置参数**：`EnemySpawnerConfig.ItemReward`；`Meta Stats Config.items_rarity_drop_amount`

**使用方**：items/evolution economy (shard/copy supply)

**备注**：Extra copies use extra charges, so the total copies per day stays at most chargesTotal. The meta bonus only speeds up collection, unless items_rarity_charges also raises chargesTotal.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：Add: SpawnerStatRewardMultiplier=copies is set only when copies>0, and it then also multiplies any StatRewards/SoulReward on that item unit. The grant handler falls back to CalculatePerKill when Copies<1 (GrantSpawnerItemRewardCommandHandler.cs:29). Edge case: the BossTower item spawner has base charges 0, so charges are disabled (unlimited respawn every cooldown) until items_rarity_charges gives a bonus >0. At that point it becomes charge-limited to just the bonus.  
  依据：EnemySpawner.cs:1191-1215; [Meta/SpawnerChargesCalculator.cs:40-41](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SpawnerChargesCalculator.cs#L40); EnemySpawner.cs:307-312 IsChargesEnabled = !IsEvent && effectiveCharges>0.

### `spawners.stat_reward_grant`

**Devour stat reward (permanent growth)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x1488e3c (stat_drop multiplier); 0x1456084 (PlayerRewardSystem.GrantStatReward)`

```text
for each reward in unit.StatRewards: v = reward.Value * stat_drop[param] * (mult > 1 ? mult : 1), where param = (IsEvent && EventDef.UnitType==Mob) ? -1 : LocationNumber. Then PlayerRewardSystem: v *= cheatDrop; if Source==Enemy, v *= (1 + doubleLootAbility) when that ability is primed, and v *= vipLevel.Multiplier with probability vipLevel.Chance. stat.AddModifier(v) (permanent; Flat in all scene data)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `reward.Value` | base stat gain per devour | EnemySpawnerConfig.StatRewards[].Value |
| `stat_drop` | meta multiplier keyed by location (or -1 for event golems) | Meta/SpawnerStatDropAmountCalculator.cs:9-14 |
| `mult` | SpawnerStatRewardMultiplier | spawners.zero_cooldown_collapse / spawners.item_drop_copies |

**输出**：StatModifier added to player stat

**代码**：[Enemy/Commands/GrantSpawnerStatRewardCommandHandler.cs:17-37](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerStatRewardCommandHandler.cs#L17)；[Enemy/EnemySpawner.cs:1359-1383](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1359)；[Enemy/EnemyUnit.cs:147](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L147)；../[Player/Systems/PlayerRewardSystem.cs:49-79](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/PlayerRewardSystem.cs#L49)

**状态依据**：The handler itself has no VA. Its components SpawnerStatDropAmountCalculator (0x1488e3c) and PlayerRewardSystem.GrantStatReward (0x1456084) are native-annotated.

**配置参数**：`EnemySpawnerConfig.StatRewards`；`Meta Stats Config.stat_drop`

**使用方**：stat aggregation (Unit/Stat)；Simulation RPM (SimulationSystem.GetResourceValue uses StatRewards[0].Value)

**备注**：Granted on devour, not on kill. Scene: 577 spawners each have exactly 1 reward, all ModifierType 0 (Flat) and Source 0 (Enemy). Stat types by count: Health(1) 138, HealthRegen(2) 81, BuckDmgFlat(12) 66, CutDmgFlat(8) 54, StabDmgFlat(6) 54, Buck/CutDefFlat(20/16) 34 each, StabDefFlat(14) 28, MoveSpeed 19, Crit 17, CritDmg 16, Evasion 14, Block 8+8, Lifesteal 6. Value range grows with location, from 0.5 to 500 at L1 up to 1e6 at L17. Empty or 'Devoured' names are renamed to map.tree/map.elite/map.normal_mob.

### `spawners.soul_reward_grant`

**Devour soul reward**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x1488810 (SoulDropAmountCalculator.CalculatePerKill); 0x15ba79c (IsSoulsDropUnlocked)`

```text
if SoulReward set and (SoulAlwaysAvailable or unlock[...] > 0): amt = ((max(1, 1 + soul_drop_bonus) + souls_rarity_amount[key]) * CurrentDevourRewardMultiplier) * cheatDrop; with SoulPass, amt *= 2 with probability PassDoubleSoulChance
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `key` | soul rarity key (common/large/elite/huge/great/chaotic) | EnemySpawnerConfig.SoulReward |
| `unlock` | IsStatic ? feature_unlock['static_souls_drop'] : souls_drop_unlock[key] | Amulet/Upgrades/AmuletUpgradesModel.cs:142-151 |
| `CurrentDevourRewardMultiplier` | = unit.SpawnerStatRewardMultiplier during devour | spawners.zero_cooldown_collapse |

**输出**：souls added to AmuletModel

**代码**：[Enemy/Commands/GrantSpawnerSoulRewardCommandHandler.cs:29-40](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerSoulRewardCommandHandler.cs#L29)；../[Meta/SoulDropAmountCalculator.cs:7-13](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SoulDropAmountCalculator.cs#L7)；[Enemy/EnemySpawner.cs:1362](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1362)

**状态依据**：The handler has no VA. The calculators it calls are native-annotated.

**配置参数**：`EnemySpawnerConfig.SoulReward`；`EnemySpawnerConfig.SoulAlwaysAvailable`；`Amulet Config.PassDoubleSoulChance`；`Meta Stats Config.soul_drop_bonus`；`Meta Stats Config.souls_rarity_amount`

**使用方**：amulet progression

**备注**：Scene SoulReward counts: common 459, large 183, elite 33, empty 28, huge 12, great 1, chaotic 1.

### `spawners.currency_reward`

**Spawner currency reward**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
if CurrencyReward set and CurrencyRewardAmount > 0: currency[CurrencyReward] += CurrencyRewardAmount (per devour; not multiplied)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `CurrencyRewardAmount` | amount per devour (default 1.0) | EnemySpawnerConfig.CurrencyRewardAmount |

**输出**：currency

**代码**：[Enemy/Commands/GrantSpawnerCurrencyRewardCommandHandler.cs:15-28](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerCurrencyRewardCommandHandler.cs#L15)

**状态依据**：No native annotation.

**配置参数**：`EnemySpawnerConfig.CurrencyReward`；`EnemySpawnerConfig.CurrencyRewardAmount`

**使用方**：amulet unlock

**备注**：Only one scene spawner uses it: the L4 dragon (DisableAfterFirstClear) gives empty_amulet x1, which unlocks the amulet.

### `spawners.enemy_spawn_stats`

**Enemy instance stats at spawn (incl. AB HP/damage multiplier)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
stat_i = config.Stats[i].BaseValue. If !IsEvent and m = EnemyHpDamageMult[L] != 1: Health = round(Health*m) and Damage = round(Damage*m). CurrentHealth = Health. agent.speed = MovementSpeed
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `m` | AB enemy_hp_damage_mult_v3 damageMultiplier by location (default 1) | ABTests/ABTestService.cs:18-19; ABTests/EnemyHpDamageMultData.cs |

**输出**：EnemyUnit.Stats, CurrentHealth

**代码**：[Enemy/EnemySpawner.cs:963-977](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L963)；[Enemy/EnemyUnit.cs:140-155](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L140)；../[ABTests/Systems/EnemyHpDamageMultSystem.cs:31-73](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/Systems/EnemyHpDamageMultSystem.cs#L31)

**状态依据**：No native annotation in EnemySpawner/EnemyUnit init.

**配置参数**：`EnemySpawnerConfig.Stats`；`ABTestData.EnemyHpDamageMult.damageMultiplier`

**使用方**：combat damage (Services/Damage/DamageCalculator)；Simulation fight time (abMultiplier at SimulationSystem.cs:656-662)

**备注**：All 717 spawners have exactly these stats: MoveSpeed(0), Health(1), HealthRegen(2), Damage(3), AttackRange(4), AttackCooldown(5). When the AB cohort changes at runtime, live enemies are rescaled by next/prev (EnemyHpDamageMultSystem). Health range grows from about 1e1-1e3 at L1-2 to about 4e8-5.8e10 at L17. One L15 spawner has Health = inf (check for bad data).

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：The Health=Infinity spawner is BossTower_c296e152609ae5849bb7d52b03df895a (L15). It has an ItemReward, ChargesBeforeBigRespawn=0 and Cooldown 1800. It is a boss-tower placeholder or prop, not a farmable mob, so leave it out of the HP curve model. Also, 'round' is C# Math.Round (banker's rounding, ToEven).  
  依据：Scene parse: BossTower_c296... L15 stats[1]=Infinity; EnemySpawner.cs:963-977 uses Math.Round(stat.BaseValue*multiplier).

### `spawners.enemy_attack_cadence`

**Enemy attack timing, aggro and movement**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
Attack when dist < AttackRange and AttackCooldownTimer <= 0, then timer = AttackCooldown stat. So enemy hit rate = 1/AttackCooldown per unit. Move speed = MovementSpeed * (RetreatTimer > 0 ? 2 : 1). Aggro: dist < AggroRange sets AggroTimer = AggroDuration. The enemy disengages if dist > AggroRange && AggroTimer <= 0 && the player is not targeting it. Animation speed = 1/AttackCooldown
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `AttackRange, AttackCooldown, MovementSpeed` | enemy stats | EnemySpawnerConfig.Stats types 4,5,0 |
| `AggroRange, AggroDuration` | aggro params | EnemySpawnerConfig.AggroRange/AggroDuration |

**输出**：WeaponHitEvent frequency and damage type = config.WeaponType

**代码**：[Enemy/EnemyAttackSystem.cs:102-142](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L102)；[Enemy/EnemyAttackSystem.cs:268-303](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L268)；[Enemy/EnemyAttackSystem.cs:342-348](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L342)；[Enemy/EnemyUnit.cs:313-316](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L313)

**状态依据**：EnemyAttackSystem has no native annotations.

**配置参数**：`EnemySpawnerConfig.Stats`；`EnemySpawnerConfig.AggroRange`；`EnemySpawnerConfig.AggroDuration`；`EnemySpawnerConfig.WeaponType`

**使用方**：combat damage model；Simulation enemyDPS (GetSpawnerDps)

**备注**：Damage per hit is computed in DamageCalculator (combat subsystem) from the Damage stat and config.WeaponType. Scene WeaponType counts: Stabbing(0) 474, Cutting(1) 147, Buckshot(3) 96.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：Add: flying enemies call Attack whenever distance < AttackRange even while retreating (EnemyAttackSystem.cs:302-303 sit outside the retreat branch). Ground enemies only attack in the non-retreat branch (140-142). Escaping enemies (EnemyCanEscapeMarker) never attack. RetreatTimer (x2 move speed) is set to 3 s for aggroed enemies on player revive (Player/Commands/RevivePlayerCommandHandler.cs:37) and by MageBossSystem. The first hit is immediate because AttackCooldown is 0 at Initialize (EnemyUnit.cs:154).  
  依据：[Enemy/EnemyAttackSystem.cs:102-142](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L102), 268-303; RevivePlayerCommandHandler.cs:37.

### `spawners.enemy_leash_heal`

**Leash full-heal**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
if dist(enemy, player) > 30 (ground uses sqr > 900) && !dead && !bossArena then CurrentHealth = Health
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `dist` | world distance to player | runtime |

**输出**：enemy CurrentHealth

**代码**：[Enemy/EnemyAttackSystem.cs:107-109](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L107)；[Enemy/EnemyAttackSystem.cs:274-275](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L274)

**状态依据**：No native annotation.

**使用方**：combat

**备注**：Hard-coded 30-unit leash.

### `spawners.enemy_regen`

**Enemy HP regeneration**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
HP = min(HP + (HealthRegen + (t - LastDamageGet >= 5 ? 0.05*maxHP : 0)) * dt, maxHP)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `HealthRegen` | enemy regen stat (type 2) | EnemySpawnerConfig.Stats |
| `0.05, 5` | 5% maxHP/s idle regen after 5 s without damage | constants EnemyRegenSystem.HEAL_PER_SECOND / NO_DAMAGE_TIME_TO_HEAL |

**输出**：enemy CurrentHealth

**代码**：[Enemy/Systems/EnemyRegenSystem.cs:12-27](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/EnemyRegenSystem.cs#L12)

**状态依据**：No native annotation.

**配置参数**：`EnemySpawnerConfig.Stats[HealthRegen]`

**使用方**：combat

**备注**：Excluded for UnitWithoutRegenMarker (crystal objectives), dead units and far units.

### `spawners.event_damage_ramp_timer`

**Event unit damage-ramp timer**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
rampT = (IsEvent && HP < maxHP - 0.01) ? rampT + dt : 0
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `rampT` | EventDamageRampTimer | runtime |

**输出**：EventDamageRampTimer (read by the DamageCalculator event multiplier)

**代码**：[Enemy/EnemyUnit.cs:188](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L188)

**状态依据**：No native annotation in EnemyUnit.Tick.

**配置参数**：`EnemySpawnerConfig.IsEvent`

**使用方**：Services/Damage/DamageCalculator.GetEnemyEventDamageMultiplier

**备注**：The ramp curve itself is in the combat/events subsystem.

### `spawners.event_plan_override`

**Event spawner forecast override**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x150639c`

```text
WeaponType = enemyInfo.AttackType. For each armor: Resist = (armor.Type == eventResistType && armor.Type != Magic) ? -0.9 : 0. Health.Base = enemyInfo.Health; Damage.Base = enemyInfo.Damage. StatRewards = [Flat(rewardInfo.RewardStatType, rewardInfo.RewardValue)]
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `enemyInfo, rewardInfo` | EventsRepository forecast for UnitType | Events subsystem forecast |
| `eventResistType` | event weakness type | EventModel.Data.ResistType (from LocationConfig.PreferredArmorType) |

**输出**：mutated EnemySpawnerConfig before EnemyUnit.Initialize

**代码**：[Enemy/EnemySpawner.cs:1141-1171](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1141)

**状态依据**：Comment 'Original APK ARM64 ELF VA 0x150639c; exact forecast override'.

**配置参数**：`EnemySpawnerConfig.ArmorConfig`；`EnemySpawnerConfig.Stats`；`EnemySpawnerConfig.StatRewards`；`LocationsConfig.PreferredArmorType`

**使用方**：events subsystem

**备注**：Resist -0.9 means the event enemy takes 190% damage from its weakness type, assuming the armor formula uses (1-resist).

### `spawners.event_crystal_mob_bonus`

**Crystal event mob extra stat reward**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
blue_crystal variant: rewardType = DefenceFlat of event WeaponType. red_crystal: rewardType = DamageFlat of event ResistType. If RPM[rewardType] > 0, add Flat reward with value = max(1, RPM[rewardType] * 0.4)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `RPM` | SimulationSystem.RPMCalculatedValue (reward per minute) | Simulation subsystem |

**输出**：extra StatModifier on the unit

**代码**：[Enemy/EnemySpawner.cs:980-993](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L980)

**状态依据**：No native annotation.

**配置参数**：`EventSpawnerDef.Variants[].CurrencyKey`

**使用方**：spawners.stat_reward_grant

**备注**：Equal to 0.4 minutes of RPM-equivalent stat per kill.

### `spawners.event_variant_currency`

**Event variant pick and currency reward**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
variant = Variants[uniformInt(0, n)]; on devour, eventCurrency[variant.CurrencyKey] += variant.CurrencyAmount. MiniBoss devour: Progress += 1. Boss devour: Progress += ProgressMax
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `CurrencyAmount` | event currency per devour | EventSpawnerVariant.CurrencyAmount |

**输出**：event currency and progress

**代码**：[Enemy/EventSpawnerDef.cs:18-21](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EventSpawnerDef.cs#L18)；[Enemy/Commands/GrantSpawnerEventCurrencyRewardCommandHandler.cs:30-61](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerEventCurrencyRewardCommandHandler.cs#L30)

**状态依据**：No native annotation.

**配置参数**：`EventSpawnerDef.Variants`；`EventSpawnerVariant.CurrencyAmount`

**使用方**：events subsystem

### `spawners.event_miniboss_spawn_gate`

**Event mini-boss spawn threshold**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`config_rule`
- 来源：子系统梳理

```text
spawn allowed only if Progress < ProgressMax - offset, where offset = 0 for crystal_event_roster and 1 otherwise, and the mini-boss flow is not running
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Progress, ProgressMax` | event progress | EventModel |

**输出**：bool

**代码**：[Enemy/EnemySpawner.cs:934-949](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L934)

**状态依据**：No native annotation.

**配置参数**：`EventSpawnerDef.Key`；`EventSpawnerDef.UnitType`

**使用方**：events subsystem

### `spawners.event_spawner_init`

**Event spawner initialization**

- 状态：**空壳**（`stub`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
TryInitializeEventSpawner(now) returns false, so event spawners never get past ProcessSpawnTick
```

**输出**：bool

**代码**：[Enemy/EnemySpawner.cs:860-863](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L860)；[Enemy/EnemySpawner.cs:746-750](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L746)

**状态依据**：The body is 'return false'. The original must have checked or scheduled the event plan.

**配置参数**：`EnemySpawnerConfig.IsEvent`；`EnemySpawnerConfig.EventDef`

**使用方**：events subsystem

**备注**：Event spawning is broken in the reconstruction.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：Overstated. Only the periodic tick/respawn path is disabled: ProcessSpawnTick returns early for IsEvent. Event units are still spawned by external callers: EventsRepository.cs:571 and EventBoosterModel.cs:225 via TrySpawnIgnoringMaxCount, and RosterSpawnNextMiniBossFlowRunner.cs:26 via TrySpawn. The original TryInitializeEventSpawner probably handled event-period scheduling or initial spawning.  
  依据：grep for TrySpawn/TrySpawnIgnoringMaxCount callers in Events/; EnemySpawner.cs:746-750, 860-863.

### `spawners.time_skip`

**Spawner time skip**

- 状态：**空壳**（`stub`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
ApplyTimeSkip(seconds): empty (presumably LastSpawnTime/NextBigSpawnTime -= seconds)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `seconds` | skipped time | caller (time-skip offers/abilities) |

**输出**：none

**代码**：[Enemy/EnemySpawner.cs:682-684](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L682)

**状态依据**：Empty body on a runtime public method.

**使用方**：time-skip features

**备注**：ConsumeAllCharges and DefeatAllUnits (EnemySpawner.cs:426-433) are also empty, probably cheat or debug only.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `used_by`：none found. There are no callers of ApplyTimeSkip, ConsumeAllCharges or DefeatAllUnits anywhere in the recovered code. The consumer (an offer, cheat or ability) is either missing too or never existed.  
  依据：grep 'ApplyTimeSkip(|ConsumeAllCharges|DefeatAllUnits' outside EnemySpawner.cs returns nothing.

### `spawners.vip_spawner_gate`

**VIP spawner unlock**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`config_rule`
- 来源：子系统梳理

```text
VIP spawner spawns only if LastUnlockedLocation.Number >= 2
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `LastUnlockedLocation` | max unlocked island | PlayerModel |

**输出**：bool

**代码**：[Enemy/EnemySpawner.cs:924-925](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L924)

**状态依据**：No native annotation.

**配置参数**：`EnemySpawnerConfig.IsVip`；`EnemySpawnerConfig.RequiredVipLevel`

**备注**：RequiredVipLevel is never read in runtime code (10 VIP spawners in scene).

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：RequiredVipLevel is never read. All 10 VIP spawners have RequiredVipLevel=3, which strongly suggests the original also checked VIP level >= 3 and that check is missing from the reconstruction.  
  依据：Scene parse: RequiredVipLevel Counter{'3':10} over IsVip=1 spawners; EnemySpawner.cs:924-925.

### `spawners.disable_after_first_clear`

**One-shot spawners**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`config_rule`
- 来源：子系统梳理

```text
if DisableAfterFirstClear && ClearedOnce then the spawner is disabled permanently (crystal objective spawners excepted)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ClearedOnce` | persisted flag set when alive==0 after the first spawn | SpawnerDataUd.cleared_once |

**输出**：spawner enabled flag

**代码**：[Enemy/EnemySpawner.cs:484-489](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L484)；[Enemy/EnemySpawner.cs:775-783](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L775)；[Enemy/EnemySpawner.cs:1328-1333](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1328)

**状态依据**：No native annotation.

**配置参数**：`EnemySpawnerConfig.DisableAfterFirstClear`

**使用方**：Simulation GetSpawnerChargesLimit (uses Count)；spawners.island_cleared_predicate

**备注**：25 spawners in scene.

### `spawners.gate_location_unlock`

**Gate opening and location unlock**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x1460b1c (UnlockLocation), 0x1460c24 (AdvanceBossProgressLocation)`

```text
on kill of a spawner whose config.Gates contains locked gates: for each gate, UnlockedLocationNumber = max(UnlockedLocationNumber, gate.NextLocationNumber) and BossProgressLocationNumber = max(BossProgressLocationNumber, gate.NextLocationNumber)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `NextLocationNumber` | island opened by the gate | Gate component in Main.unity |

**输出**：PlayerModel.UnlockedLocationNumber, BossProgressLocationNumber

**代码**：[Enemy/Commands/GrantSpawnerGateRewardCommandHandler.cs:18-27](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerGateRewardCommandHandler.cs#L18)；[Gates/Gate.cs:47-71](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Gates/Gate.cs#L47)；../[Player/Models/PlayerModel.cs:840-852](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L840)

**状态依据**：Gate.cs and the handler have no VA. The PlayerModel unlock methods they call are native-annotated.

**配置参数**：`EnemySpawnerConfig.Gates`；`Gate.NextLocationNumber`

**使用方**：spawners.location_trigger；spawners.vip_spawner_gate；LastUnlockedLocation consumers (events, AB, PlayerAttackSystem weak enemies, Simulation unlockedLocation filter)

**备注**：17 spawners carry gates, all island bosses whose OptimumTime equals the island's max optimum time (e.g. L4 boss OT 6.2 days, L11 boss OT 56). The gate is opened on kill (HandleDeath), not on devour.

### `spawners.location_trigger`

**Location number from gate triggers**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x1460a24 (SetCurrentLocation)`

```text
triggerLocation = gate.NextLocationNumber - (isExit ? 1 : 0). Entering it sets CurrentLocation = triggerLocation; if UnlockedLocationNumber < triggerLocation then unlock it
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `isExit` | trigger on the old side of the gate | GateLocationTrigger.isExit |

**输出**：PlayerModel.CurrentLocationNumber

**代码**：[Gates/GateLocationTrigger.cs:20-30](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Gates/GateLocationTrigger.cs#L20)；../[Player/Models/PlayerModel.cs:830-838](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L830)

**状态依据**：GateLocationTrigger has no VA. SetCurrentLocation is native-annotated.

**配置参数**：`Gate.NextLocationNumber`

**使用方**：spawners.island_medal_progress

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression`：At runtime OnTriggerEnter calls SetCurrentLocation(locationNumber), where locationNumber is the SERIALIZED field GateLocationTrigger.locationNumber. SetCurrentLocation sets CurrentLocation = n and unlocks if UnlockedLocationNumber < n. The relation locationNumber = gate.NextLocationNumber - (isExit?1:0) only comes from UpdateLocationNumber(), a private editor helper with no runtime caller. The live value is whatever was serialized in Main.unity.  
  依据：[Gates/GateLocationTrigger.cs:20-24](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Gates/GateLocationTrigger.cs#L20) uses the serialized field; :26-30 UpdateLocationNumber is never invoked; [Player/Models/PlayerModel.cs:830-838](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L830).

### `spawners.portal_gem`

**Portal gem collection and portal unlock**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`config_rule`
- 来源：子系统梳理

```text
on kill: if PortalGemReward > PortalGemsCollected then PortalGemsCollected = PortalGemReward. The portal unlocks when PortalGemsCollected >= stones.Length
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `PortalGemReward` | gem index 1..7 on island bosses | EnemySpawnerConfig.PortalGemReward |

**输出**：PortalGemsCollected; BossTower portal unlock

**代码**：[Enemy/Commands/GrantSpawnerPortalGemRewardCommandHandler.cs:17-23](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerPortalGemRewardCommandHandler.cs#L17)；[Gates/Flows/CollectPortalGemFlowRunner.cs:106-107](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Gates/Flows/CollectPortalGemFlowRunner.cs#L106)；[Gates/Portal.cs:39-45](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Gates/Portal.cs#L39)

**状态依据**：No native annotation.

**配置参数**：`EnemySpawnerConfig.PortalGemReward`

**使用方**：BossTower unlock

**备注**：Scene gem bosses: L2=1, L3=2, L4=3, L5=4, L6=5, L7=6, L8=7. The portal opens after the L8 boss.

### `spawners.slot_reward`

**Item slot unlock reward**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`config_rule`
- 来源：子系统梳理

```text
on kill: if SlotReward names a locked slot, unlock it
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `SlotReward` | slot key (weapon_2, helmet_1, shield_1, ...) | EnemySpawnerConfig.SlotReward |

**输出**：ItemSlotModel.IsUnlocked

**代码**：[Enemy/Commands/GrantSpawnerItemSlotRewardCommandHandler.cs:17-30](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerItemSlotRewardCommandHandler.cs#L17)；[Enemy/EnemySpawner.cs:1187-1190](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1187)

**状态依据**：No native annotation.

**配置参数**：`EnemySpawnerConfig.SlotReward`；`Player Config.asset Slots`

**使用方**：equipment capacity

**备注**：Scene: L1 boss unlocks weapon_2, L4 dragon unlocks helmet_1, L5 boss unlocks shield_1. weapon_3 and weapon_4 are not unlocked by any spawner.

### `spawners.progress_markers`

**RewardedPoolIndex and user OptimumTime high-water marks**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
on kill: PoolIndex = max(PoolIndex, config.RewardedPoolIndex). If the enemy is not static: user.OptimumTime = max(user.OptimumTime, config.OptimumTime)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `RewardedPoolIndex` | index into RewardedConfig.RewardChances | EnemySpawnerConfig.RewardedPoolIndex |

**输出**：UserModel.PoolIndex, UserModel.OptimumTime

**代码**：[Enemy/EnemySpawner.cs:1237-1241](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1237)；../[Integrations/Ads/AdsInitialize.cs:330](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Integrations/Ads/AdsInitialize.cs#L330)

**状态依据**：No native annotation in HandleDeath.

**配置参数**：`EnemySpawnerConfig.RewardedPoolIndex`；`EnemySpawnerConfig.OptimumTime`

**使用方**：rewarded-ad reward pool selection；analytics

**备注**：Island bosses carry RewardedPoolIndex 1..13, which moves the player to a richer rewarded-ad pool. user.OptimumTime mixes minute and day units: L1-3 are minutes and L4+ are days.

### `spawners.island_cleared_predicate`

**Island fully-cleared predicate (mastery)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`config_rule`
- 来源：子系统梳理

```text
An eligible spawner is not Event/VIP/Special/Dummy/BossFightPart/DisableAfterFirstClear/Crystal/Creeper and has chargesTotal > 0. An island is cleared when every eligible spawner with LocationNumber=L has bootstrapped, has alive == 0 and ChargesLeft == 0
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ChargesLeft` | remaining charges | spawners.effective_charges |

**输出**：bool IsFullyCleared(L)

**代码**：[Location/IslandMasteryResolver.cs:31-75](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Location/IslandMasteryResolver.cs#L31)

**状态依据**：No native annotation.

**配置参数**：`EnemySpawnerConfig.LocationNumber`；`EnemySpawnerConfig.ChargesBeforeBigRespawn`

**使用方**：spawners.island_medal_progress

**备注**：Clearing an island means using up every eligible spawner's daily charges on it.

### `spawners.island_medal_progress`

**Island medal level and bonus**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
Checked every 1 s for the current location. On the cleared transition (cleared && !ClearProcessed): medal = min(5, medal + 1) and ClearProcessed = true. When !cleared after a refill: ClearProcessed = false. island_medal_bonus gets a Flat modifier 0.1 * medal with Param = L
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `medal` | 0..5 | IslandMasteryLocationUd.medal_level |
| `0.1` | BonusPerMedalLevel | constant IslandMasteryModel.cs:15 |

**输出**：island_medal_bonus[L] in [0, 0.5]

**代码**：[Location/Systems/IslandMasterySystem.cs:29-105](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Location/Systems/IslandMasterySystem.cs#L29)；[Location/IslandMasteryModel.cs:14-15](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Location/IslandMasteryModel.cs#L14)；[Location/IslandMasteryModel.cs:94-101](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Location/IslandMasteryModel.cs#L94)；[Location/IslandMasteryModel.cs:169-178](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Location/IslandMasteryModel.cs#L169)

**状态依据**：IslandMastery classes have no native annotations.

**配置参数**：`Meta Stats Config.island_medal_bonus (BaseValue 0)`

**使用方**：[Services/Damage/DamageCalculator.cs:257-267](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L257) (player damage vs island-L enemies *(1+b); enemy damage to player *max(0, 1-b); native VAs 0x140be68/0x140c150)

**备注**：At most one medal per daily charge reset, so 5 full clears (days) give +50% damage and -50% damage taken on that island. DamageCalculator's eligibility check (0x140c264) differs from the resolver: it excludes quest mobs but does not exclude DisableAfterFirstClear or creepers.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：Add: only the player's CURRENT location is evaluated, once per second or immediately when the location changes. On first run, the legacy migration marks islands that are already cleared as ClearProcessed without giving a medal (IslandMasterySystem.cs:65-76). Evaluation is paused while PendingBootstrap is non-empty.  
  依据：[Location/Systems/IslandMasterySystem.cs:29-76](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Location/Systems/IslandMasterySystem.cs#L29).

### `spawners.creeper_explosion`

**Creeper mob explosion damage**

- 状态：**空壳**（`stub`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
likely explosionDmg = DamageMaxHealthPercent * target.maxHP within ExplosionRadius after ExplosionDelay
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `DamageMaxHealthPercent, ExplosionRadius, ExplosionDelay` | creeper component params | Enemy/Components/CreeperMobComponent.cs:10-14 (prefab data) |

**输出**：damage to player

**代码**：[Enemy/Systems/CreeperExplosionSystem.cs:41-55](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/CreeperExplosionSystem.cs#L41)；[Enemy/Components/CreeperMobComponent.cs:8-26](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Components/CreeperMobComponent.cs#L8)

**状态依据**：OnUpdate/StartExplosion/Explode/FinalizeDeath are empty. The expression is inferred from field names.

**配置参数**：`CreeperMobComponent.DamageMaxHealthPercent`；`CreeperMobComponent.ExplosionRadius`；`CreeperMobComponent.ExplosionDelay`

**使用方**：combat

**备注**：Creeper deaths are also suppressed until State == Finalized (EnemyDeathSystem.cs:34-35), so creepers can never die in the reconstruction.

### `spawners.optimum_time_days_converter`

**OptimumTime minutes <-> days conversion**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`（核验前为 `editor_generator`）
- 来源：子系统梳理
- 原生地址：`0x150ced0, 0x150cf78`

```text
days = roundHalfEven(OT_min / 30 * 10) / 10 and OT_min = days * 30. Apply(): for spawners with L >= locationFrom (scene 4), OT = toDays(OT) and IsDays = true
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `30` | playtime minutes per 'day' (one daily session / time chunk) | constant; matches Simulation Config.TimeChunkDurationMinutes=30 |
| `locationFrom` | first location to convert | Main.unity SpawnerOptimumTimeDaysConverter.locationFrom = 4 |

**输出**：OptimumTime in days, IsDays

**代码**：[Enemy/SpawnerOptimumTimeDaysConverter.cs:13-47](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/SpawnerOptimumTimeDaysConverter.cs#L13)；[Enemy/SpawnerOptimumTimeDaysConverter.cs:9-11](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/SpawnerOptimumTimeDaysConverter.cs#L9)

**状态依据**：Static converters are annotated with ELF VAs (marked 'Candidate only' for rounding). Apply() is empty (editor_only_missing).

**配置参数**：`EnemySpawnerConfig.OptimumTime`；`EnemySpawnerConfig.IsDays`

**使用方**：[Quests/QuestMobSpawnerService.cs:94](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestMobSpawnerService.cs#L94),211；spawners.optimum_time_semantics

**备注**：Scene: L1-3 are in minutes (IsDays=0, 108 spawners). L4-17 are in days (609). Conflict: Combat Balance Config.IslandTargetDays and SimulationConfig.GetReCalcOptimumTime use 60 minutes per day (L1 30 min = 0.5, L3 240 min = 4.0).

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [已应用] `kind`：runtime_formula. The static ConvertOptimumToDays/ConvertDaysToOptimum have native VAs and are called at runtime by quest mob spawning. Only Apply() (empty, lines 9-11) is editor_only_missing. Also: L1 has 1 spawner with IsDays=1, so there are 109 L1-3 spawners, 108 of them in minutes.  
  依据：[Quests/QuestMobSpawnerService.cs:94](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestMobSpawnerService.cs#L94) and :211 call SpawnerOptimumTimeDaysConverter statics; [Enemy/SpawnerOptimumTimeDaysConverter.cs:13-47](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/SpawnerOptimumTimeDaysConverter.cs#L13).

### `spawners.relative_optimum_time`

**RelativeOptimumTime <-> OptimumTime within island**

- 状态：**编辑器工具缺失**（`editor_only_missing`），核验前为 `editor_only_missing`
- 类型：`editor_generator`
- 来源：子系统梳理

```text
rel = (OT - Min_L) / (Max_L - Min_L) and OT = Min_L + rel * (Max_L - Min_L), where Min_L/Max_L come from IslandOptimumTimeRange (from the OTs of MinSpawner/MaxSpawner)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Min_L, Max_L` | island optimum-time span; equals IslandTargetDays[L-2..L-1] | Main.unity SpawnerRelativeOptimumTimeTool.Islands |

**输出**：RelativeOptimumTime or OptimumTime

**代码**：[Enemy/SpawnerRelativeOptimumTimeTool.cs:10-41](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/SpawnerRelativeOptimumTimeTool.cs#L10)；[Enemy/IslandOptimumTimeRange.cs:6-19](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/IslandOptimumTimeRange.cs#L6)

**状态依据**：All tool methods are empty. The expression was checked against scene data: 695 of 717 spawners match within 0.011; the 22 mismatches (mostly L2/L4 rel=0.18/0.15, some >1) look stale.

**配置参数**：`EnemySpawnerConfig.RelativeOptimumTime`；`EnemySpawnerConfig.OptimumTime`；`IslandOptimumTimeRange.MinOptimumTime/MaxOptimumTime`

**使用方**：[Simulation/SimulationConfig.cs:219-231](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L219) GetReCalcOptimumTime (early-spawner OT offset for L>=11 when rel<0.5)

**备注**：Island ranges (minutes for L1-3, days for L4+): L1 0-30, L2 35-110, L3 120-240, L4 4-7, L5 7-10, L6 10-14, L7 14-20, L8 20-26, L9 26-34, L10 34-44, L11 44-56, L12 56-71, L13 71-90, L14 90-110, L15 110-130, L16 130-150, L17 150-180.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [已应用] `status`：editor_only_missing. Every method of SpawnerRelativeOptimumTimeTool is empty (ReadIslandMinMaxSpawners, FetchTimeFromSpawners, FillRelativeOptimumTimes, ApplyOptimumTimesFromRelative). The only live RelativeOptimumTime math is SimulationConfig.GetReCalcOptimumTime.  
  依据：[Enemy/SpawnerRelativeOptimumTimeTool.cs:9-38](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/SpawnerRelativeOptimumTimeTool.cs#L9) are all empty bodies. Live use is at [Simulation/SimulationConfig.cs:219-230](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L219).

### `spawners.difficulty_bulk_setter`

**Bulk Difficulty setter**

- 状态：**编辑器工具缺失**（`editor_only_missing`），核验前为 `editor_only_missing`
- 类型：`editor_generator`
- 来源：子系统梳理

```text
likely: for spawners with locationFrom <= L <= locationTo and Difficulty <= maxSourceDifficulty, set Difficulty = targetDifficulty
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `locationFrom, locationTo, maxSourceDifficulty, targetDifficulty` | tool params | SpawnerDifficultyBulkSetter fields |

**输出**：EnemySpawnerConfig.Difficulty

**代码**：[Enemy/SpawnerDifficultyBulkSetter.cs:5-22](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/SpawnerDifficultyBulkSetter.cs#L5)

**状态依据**：Apply() is empty. The expression is inferred from field names.

**配置参数**：`EnemySpawnerConfig.Difficulty`

**使用方**：spawners.difficulty_semantics

**备注**：No serialized instance of this component was found in Main.unity.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [已应用] `status`：editor_only_missing. Apply() is empty. Only its serialized inputs (locationFrom, locationTo, maxSourceDifficulty, targetDifficulty) survive.  
  依据：[Enemy/SpawnerDifficultyBulkSetter.cs:18](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/SpawnerDifficultyBulkSetter.cs#L18) has an empty Apply().

### `spawners.debugger_gpm_export`

**Spawner debugger GPM / CSV export**

- 状态：**编辑器工具缺失**（`editor_only_missing`），核验前为 `editor_only_missing`
- 类型：`editor_generator`
- 来源：子系统梳理

```text
UpdateGpms(): per-location aggregate of spawner stat gains (probably gain per minute = sum StatRewards * stat_drop / cooldown), output to info[Location].Stats[StatType].Value. ExportToCsv in statsCSVOrder
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `statsCSVOrder` | stat column order | Main.unity EnemySpawnersDebugger.statsCSVOrder = [1,8,6,12,2,0,24,25,23,16,14,20] |

**输出**：EnemySpawnerDebugInfo list

**代码**：[Enemy/EnemySpawnersDebugger.cs:7-41](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawnersDebugger.cs#L7)；[Enemy/EnemySpawnerDebugInfo.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawnerDebugInfo.cs)；[Enemy/EnemySpawnerDebugInfoStat.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawnerDebugInfoStat.cs)

**状态依据**：All methods are empty. Only the serialized info list in the scene (if populated) remains.

**配置参数**：`EnemySpawnersDebugger.considerNonRespawnable`；`EnemySpawnersDebugger.considerStatic`

**备注**：The CSV order shows the designers' main reward stats: Health, CutDmg, StabDmg, BuckDmg, Regen, MoveSpeed, Crit, CritDmg, Evasion, CutDef, StabDef, BuckDef.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [已应用] `status`：editor_only_missing. UpdateGpms, FindDuplicateKeys and ExportToCsv are empty, and FindAllSpawners returns null.  
  依据：[Enemy/EnemySpawnersDebugger.cs:25-38](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawnersDebugger.cs#L25).

### `spawners.difficulty_semantics`

**Difficulty / AverageFightTime meaning (bake inputs)**

- 状态：**纯数据**（`data_only`）
- 类型：`config_rule`
- 来源：子系统梳理
- 原生地址：`0x13db16c`

```text
Bake (SimulationConfig.BuildSpawnerBakeReport, 0x13db16c): neededDps = Difficulty * playerHP / AverageFightTime. Enemy Damage is solved so that Count' = min(4, (Count+1)/2) attackers deal (battleRegen(Difficulty) + neededDps) DPS through the player's evasion/block/defence. Health = playerDPS_vs_bestArmor * AverageFightTime / Count * (1 - resist). Runtime forecast: battleRegen = (D+1)*regen + (2D+3)*lifestealFlat
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Difficulty (D)` | fraction of player max HP lost (net of regen) over one full-pack fight; <=0 treated as 1 | EnemySpawnerConfig.Difficulty |
| `AverageFightTime` | target seconds to kill the whole pack (Count units) at the expected player DPS | EnemySpawnerConfig.AverageFightTime |

**输出**：baked Stats[Health], Stats[Damage]

**代码**：../[Simulation/SimulationConfig.cs:113-160](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L113)；../[Services/Damage/DamageCalculator.cs:59-62](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L59)；../[Simulation/Systems/SimulationSystem.cs:663](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L663)

**状态依据**：Live EnemySpawner code never reads these fields. They feed the Simulation bake/forecast (another subsystem).

**配置参数**：`EnemySpawnerConfig.Difficulty`；`EnemySpawnerConfig.AverageFightTime`；`EnemySpawnerConfig.Count`；`EnemySpawnerConfig.ArmorConfig`；`LocationsConfig.PreferredWeaponType`

**使用方**：Simulation bake；Simulation forecast fight selection；[BossTower/BossSpawnService.cs:469-476](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/BossSpawnService.cs#L469) (same formula for bosses)

**备注**：Scene Difficulty counts: 0.23 (277), 0.3 (171), 0.7 (93), 1 (79), 0.5 (27), 0.6 (20), 0.2 (20), 0.4 (15). AverageFightTime: 5 (318), 10 (127), 15 (99), 12 (80), up to 500. Changing either needs a re-bake: the runtime HP/Damage are the baked Stats, not recomputed.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression`：day = IsDays ? OT' : OT'/60 (OT' = GetReCalcOptimumTime). D = Difficulty<=0 ? 1 : Difficulty. armor = argmax over ArmorConfig.Armors of dps[type]*(1-resist). r = clamp(armor.Resist,-0.9,0.9). n = min(4,(Count+1)/2). battleRegen = (D+1)*regen + (2D+3)*lifestealFlat. perUnitDps = (battleRegen + D*HP/AFT)/n. neededHit = perUnitDps * AttackCooldown/(1-evasion) / (1 - blockChance*blockAmount). rawDmg = SolveAttackDamage(neededHit, defense[Location.PreferredWeaponType], 1/(1+abilityEhp(day))). Damage = round(max(10,rawDmg)/10)*10. pDps = (1+abilityDps(day))*dps[armor]; Health = round(((1-cc)*pDps + cd*cc*pDps) * AFT / Count * (1-r)). All player values come from the snapshot at OT'.  
  依据：[Simulation/SimulationConfig.cs:113-160](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L113) (native 0x13db16c); DamageCalculator.cs:54-62.
- [待人工合入] `status`：The fields themselves are data_only levers. The bake formula that consumes them is recovered_native (SimulationConfig.BuildSpawnerBakeReport has native VA 0x13db16c) and should be listed as recovered_native under the simulation subsystem. Scene Difficulty also has 0.8 (8 spawners) and one outlier 10.  
  依据：[Simulation/SimulationConfig.cs:114](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L114) comment 'Original ELF0x13db16c'; scene Difficulty counter includes ('0.8',8),('10',1).

### `spawners.optimum_time_semantics`

**OptimumTime / IsDays meaning**

- 状态：**纯数据**（`data_only`）
- 类型：`config_rule`
- 来源：子系统梳理
- 原生地址：`0x13ed83c (GetSpawnerOptimumTimingForCurrentMode)`

```text
OptimumTime = the playtime at which the design expects this spawner to be farmed (minutes if !IsDays, days if IsDays). The bake reads the player snapshot at OT (via GetReCalcOptimumTime) to size HP/Damage. Simulation with UseSpawnerCharges uses timing = IsDays ? OT : OT / TimeChunkDurationMinutes(30) and skips spawners with OT > current chunk/playtime when IsCapOptimumTime
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `OT` | design time | EnemySpawnerConfig.OptimumTime |

**输出**：bake snapshot selection; forecast availability

**代码**：../[Simulation/SimulationConfig.cs:103-110](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L103)；../[Simulation/SimulationConfig.cs:219-239](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L219)；../[Simulation/Systems/SimulationSystem.cs:533-539](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L533)；../[Simulation/Systems/SimulationSystem.cs:647-652](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L647)；[Enemy/EnemySpawner.cs:1240-1241](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1240)

**状态依据**：Live gameplay only uses it as a high-water mark (user.OptimumTime). Its main use is in Simulation.

**配置参数**：`EnemySpawnerConfig.OptimumTime`；`EnemySpawnerConfig.IsDays`；`Simulation Config.TimeChunkDurationMinutes`；`Simulation Config.EarlySpawnerOptimumTimeOffset*`

**使用方**：Simulation bake/forecast；spawners.progress_markers

**备注**：Early-spawner offset (L >= 11, rel < 0.5): OT' = OT - (IslandTargetDays[L-1] - IslandTargetDays[L-2]) * (0.5 - rel) * 1 (x60 if minutes).

### `spawners.priority_handbalance_semantics`

**PriorityMultiplier / HandBalance meaning**

- 状态：**纯数据**（`data_only`）
- 类型：`config_rule`
- 来源：子系统梳理

```text
PriorityMultiplier: probable multiplier on the bot's spawner priority (SpawnerData.GetStaticPriority/GetPriority, stubbed). HandBalance: flag to exclude a spawner from automatic bake/apply
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `PriorityMultiplier` | bot target-selection weight (default 1) | EnemySpawnerConfig.PriorityMultiplier |
| `HandBalance` | hand-tuned, skip auto-rebake | EnemySpawnerConfig.HandBalance |

**输出**：none at runtime

**代码**：[Enemy/EnemySpawnerConfig.cs:84-86](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawnerConfig.cs#L84)；../[Simulation/Systems/SpawnerData.cs:49-67](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SpawnerData.cs#L49)

**状态依据**：Neither field is referenced anywhere in the recovered code. The likely consumers (SpawnerData priority methods, SimulationConfig bake/apply) are empty.

**配置参数**：`EnemySpawnerConfig.PriorityMultiplier`；`EnemySpawnerConfig.HandBalance`

**使用方**：Simulation bot (inferred)

**备注**：Scene PriorityMultiplier counts: 1 (449), 3 (70), 10 (67), 5 (45), 7 (38), 20 (28), 4 (10), 8/30 (4 each). HandBalance=1 on only 2 spawners.

### `spawners.daily_yield`

**Derived per-spawner daily yield (model identity)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`simulation`
- 来源：子系统梳理

```text
spawnsPerUTCday <= chargesTotal (+ alive at reset). statPerDay ~= chargesTotal * reward.Value * stat_drop[L] (zero-cooldown collapse does not change the total). Minimum active time to use the day's charges ~= (ceil(chargesTotal/Count) - 1) * smallCooldownSec. itemsPerDay ~= chargesTotal. soulsPerDay ~= chargesTotal * (max(1, 1 + soul_drop_bonus) + souls_rarity_amount[key])
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `chargesTotal` | effective charges | spawners.effective_charges |
| `smallCooldownSec` | effective respawn timer | spawners.base_cooldown |

**输出**：per-day resource supply per spawner

**代码**：[Enemy/EnemySpawner.cs:744-858](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L744)；[Enemy/EnemySpawner.cs:1056-1082](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1056)；../[Simulation/Systems/SimulationSystem.cs:608-612](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L608)

**状态依据**：Derived analytically from reconstructed spawn and charge logic. Not a single code method.

**配置参数**：`EnemySpawnerConfig.ChargesBeforeBigRespawn`；`EnemySpawnerConfig.Count`；`EnemySpawnerConfig.StatRewards`；`Meta Stats Config.stat_drop`

**使用方**：economy model

**备注**：Simulation's GetSpawnerChargesLimit uses Count for DisableAfterFirstClear spawners and ChargesTotal otherwise. Its RPM contribution formula (SimulationSystem.cs:448-458) multiplies reward.Value * charges.

### `spawners.geode_kill_count`

**Geode timer reduction per spawner kill**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：核验时补充

```text
KillCount = additionalCharges + SpawnerStatRewardMultiplier (taken BEFORE item copies overwrite the multiplier). For an item spawner: additionalCharges = copies-1 and mult=1, so KillCount = copies. For a zero-cooldown stat spawner: KillCount = Remaining+1. If KillCount>=1 and crystal_timer_on_kill_reduce>0: geodeTimer -= crystal_timer_on_kill_reduce * KillCount
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `additionalCharges` | extra charges used by multi-copy item drops | spawners.item_drop_copies (ItemDropAmountCalculator.ResolveKillDropCopies) |
| `SpawnerStatRewardMultiplier` | zero-cooldown collapse multiplier | spawners.zero_cooldown_collapse |
| `crystal_timer_on_kill_reduce` | meta stat seconds removed from the geode timer per kill | Meta Stats Config.crystal_timer_on_kill_reduce |

**输出**：CrystalModel geode timer reduction

**代码**：[Enemy/EnemySpawner.cs:1213](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1213)；[Crystals/Commands/ReduceGeodeTimerOnKillCommandHandler.cs:13-21](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Crystals/Commands/ReduceGeodeTimerOnKillCommandHandler.cs#L13)

**状态依据**：Neither HandleDeath nor the handler carries a native VA comment.

**配置参数**：`Meta Stats Config.crystal_timer_on_kill_reduce`；`EnemySpawnerConfig.ItemReward`

**使用方**：crystals/geode subsystem

**备注**：Spawner kills drive geode timing with weight equal to charges used, so charge and copy meta upgrades also speed up geodes. Every spawner kill counts, including trees and elites.

### `spawners.enemy_far_culling`

**Enemy far-distance culling (disables regen, AI, visuals)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`config_rule`
- 来源：核验时补充

```text
d2 = min(|player - enemy|^2, |cameraAttackPos - enemy|^2). far = d2 > 3600 (60 units). If far: IsFar=true, NavMesh agent disabled, visuals hidden, FarFromPlayerMarker set. FarOnScreen = d2 > 900 (30 units). EnemyRegenSystem skips entities with FarFromPlayerMarker, so far enemies do not regenerate.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `3600, 900` | squared distance thresholds (60 and 30 units) | constants EnemyVisibilitySystem.FarDistance / FarOnScreenDistance |

**输出**：FarFromPlayerMarker / FarOnScreenMarker / IsFar

**代码**：[Enemy/Systems/EnemyVisibilitySystem.cs:11-12](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/EnemyVisibilitySystem.cs#L11)；[Enemy/Systems/EnemyVisibilitySystem.cs:49-71](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/EnemyVisibilitySystem.cs#L49)；[Enemy/Systems/EnemyRegenSystem.cs:16](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/EnemyRegenSystem.cs#L16)；[Enemy/Systems/EnemySpawnerCooldownSystem.cs:31-32](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/EnemySpawnerCooldownSystem.cs#L31)

**状态依据**：No native annotation in EnemyVisibilitySystem.

**使用方**：spawners.enemy_regen；EnemyUnit.Tick animator gating

**备注**：Interacts with the 30-unit leash full-heal (spawners.enemy_leash_heal): any enemy farther than 30 units from the player is reset to full HP anyway, so the 60-unit regen cutoff only matters for enemies measured close to the camera attack position.

### `spawners.dummy_spawn_block`

**Dummy spawner never spawns (reconstruction inconsistency)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`config_rule`
- 来源：核验时补充

```text
TrySpawnUnit(mode) returns false if config.IsDummy. Because of that, the later DummyTargetTag assignment (if IsDummy) can never run
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `IsDummy` | training-dummy spawner flag | EnemySpawnerConfig.IsDummy |

**输出**：bool spawn allowed

**代码**：[Enemy/EnemySpawner.cs:922-923](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L922)；[Enemy/EnemySpawner.cs:1028-1029](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1028)

**状态依据**：No native annotation; the two IsDummy branches contradict each other, which suggests a reconstruction error.

**配置参数**：`EnemySpawnerConfig.IsDummy`

**使用方**：DamageCalculator / IslandMasteryResolver exclusions

**备注**：Scene has Spawner_Dummy (Cooldown 1). The DPS dummy cannot appear in the reconstruction. Check against ARM64.

### `spawners.full_or_depleted_wake_schedule`

**Wake-up scheduling for full or charge-depleted spawners**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：核验时补充

```text
If the spawner is full (and charges are disabled, or non-UTC with no big cooldown and Remaining>0), or it has used all its charges while units are still alive: nextProcessTime = SyncBigRespawnToUtc && ceil(BigCooldownSeconds)>0 ? bigPeriod*floor(now/bigPeriod)+bigPeriod : Dormant. Dormant spawners are woken only by HandleDeath (UpdateNextProcessTime) or a charge-upgrade top-up (nextProcessTime=0)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `bigPeriod` | ceil(BigCooldown>0 ? BigCooldown : Cooldown) | EnemySpawnerConfig.BigCooldown/Cooldown |

**输出**：nextProcessTime

**代码**：[Enemy/EnemySpawner.cs:769-773](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L769)；[Enemy/EnemySpawner.cs:884-915](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L884)；[Enemy/EnemySpawner.cs:1098-1101](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1098)

**状态依据**：No native annotation.

**配置参数**：`EnemySpawnerConfig.SyncBigRespawnToUtc`；`EnemySpawnerConfig.BigCooldown`

**使用方**：spawners.charge_restore (UTC roll-over runs at the next tick after midnight)

**备注**：This guarantees the UTC-midnight refill of RemainingCharges = chargesTotal - alive even for spawners whose units were never killed.

### `spawners.item_copies_reward_multiplier_coupling`

**Item-drop copies become the devour stat and soul multiplier**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：完整性审查补充

```text
On kill: m = zeroCooldownMultiplier(unit) (1, or max(1, remainingCharges+1) on collapse). If the spawner has an ItemReward: copies = ResolveKillDropCopies(item, meta, RemainingCharges, out extra) when charges are enabled, else CalculatePerKill(item, meta); RemainingCharges -= extra; if copies > 0 then m = copies. On devour: statGain_i = StatRewards[i].Value * stat_drop_mult(config) * (m > 1 ? m : 1) for every StatRewards entry. souls = SoulDropAmountCalculator.CalculatePerKill(soulKey, m, meta) * cheatDropMultiplier. geodeKillCount = extra + m before the item override (crystal, out of scope).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `m` | per-unit SpawnerStatRewardMultiplier | EnemyUnit.SpawnerStatRewardMultiplier (runtime) |
| `copies` | item copies dropped this kill | formula meta.item_drop_copies_per_kill / spawners.item_drop_copies |
| `extra` | additional charges consumed by the multi-copy drop | ItemDropAmountCalculator.ResolveKillDropCopies out param |
| `StatRewards[i].Value` | authored devour reward | EnemySpawnerConfig.StatRewards (Main.unity) |
| `stat_drop_mult` | meta stat_drop multiplier | formula meta.spawner_stat_drop_multiplier |

**输出**：Permanent stat gain and soul amount per devour for item spawners, which scale with copies dropped

**代码**：[Enemy/EnemySpawner.cs:1186](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1186)；[Enemy/EnemySpawner.cs:1194-1215](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1194)；[Enemy/EnemySpawner.cs:1359-1380](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1359)；[Enemy/Commands/GrantSpawnerStatRewardCommandHandler.cs:22-30](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerStatRewardCommandHandler.cs#L22)；[Enemy/Commands/GrantSpawnerSoulRewardCommandHandler.cs:33](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerSoulRewardCommandHandler.cs#L33)

**状态依据**：EnemySpawner.HandleDeath/HandleDevour and the stat reward handler have no native VA annotation. The logic is implemented and consistent with the handlers.

**配置参数**：`EnemySpawnerConfig.ItemReward`；`EnemySpawnerConfig.StatRewards`；`EnemySpawnerConfig.SoulReward`；`EnemySpawnerConfig.ChargesBeforeBigRespawn`；`Meta Stats Config.items_rarity_drop_amount`；`Meta Stats Config.stat_drop`

**使用方**：meta.devour_stat_reward_scaling；meta.soul_drop_per_kill；spawners.daily_yield；simulation.rpm_time_chunk

**备注**：This is a cross-coupling. On an item spawner, raising item drop amount (items_rarity_drop_amount / chest-like upgrades) also multiplies that spawner's permanent stat and soul income. The simulation RPM (CalculateRPMValueTimeChunk) does not model this. It uses reward.Value * charges only, so live yield exceeds simulated yield whenever copies > 1.

### `spawners.unconsumed_numeric_fields`

**Numeric config fields with no consumer**

- 状态：**纯数据**（`data_only`）
- 类型：`config_rule`
- 来源：完整性审查补充

```text
No formula reads these fields. EnemySpawnerConfig.WanderRadius, EnemySpawnerConfig.WanderDwellTime, EnemySpawnerConfig.NonSelectable, ItemConfig.Number (UI only), SimulationConfig.MaxSimultaneousAttackers (const 4; code uses a literal min((Count+1)/2, 4)), SimulationConfig.SnapshotInterval, SimulationConfig.TimeStep, SimulationConfig.SimulationSpeed, SimulationConfig.UseFastTick and SimulationConfig.FastTickStartChunkOffset (bot stubs), and LocationConfig.PreferredArmorType (outside Quests/Events/UI).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `-` | inert field | listed config fields |

**输出**：None (changing these has no runtime numeric effect in the core loop)

**代码**：[Enemy/EnemySpawnerConfig.cs:100](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawnerConfig.cs#L100)；[Enemy/EnemySpawnerConfig.cs:108-110](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawnerConfig.cs#L108)；[Player/Inventory/ItemConfig.cs:29](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemConfig.cs#L29)；[Simulation/SimulationConfig.cs:21](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L21)；[Simulation/SimulationConfig.cs:61-66](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L61)；[Simulation/Systems/SimulationSystem.cs:653](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L653)；[Simulation/SimulationConfig.cs:143](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L143)

**状态依据**：grep finds no reads outside the declarations (and UI for ItemConfig.Number).

**配置参数**：`EnemySpawnerConfig.WanderRadius`；`EnemySpawnerConfig.WanderDwellTime`；`EnemySpawnerConfig.NonSelectable`；`ItemConfig.Number`；`Simulation Config.SnapshotInterval`；`Simulation Config.TimeStep`；`Simulation Config.SimulationSpeed`；`Locations Config.PreferredArmorType`

**备注**：Export them anyway, flagged inert, so impact analysis can say 'no effect'. Wander fields may have been consumed by an AI system that was not recovered.

## 配置参数

| 资产 | 字段 | 类型 | 含义 | 用于 |
|---|---|---|---|---|
| Main.unity EnemySpawner.config | `Count` | int | max simultaneous alive units; also multiplies AB charges | spawners.respawn_tick, spawners.ab_charges_table, spawners.difficulty_semantics |
| Main.unity EnemySpawner.config | `Stats (list {BaseValue, Type})` | List<Stat> | enemy base stats (always types 0,1,2,3,4,5) | spawners.enemy_spawn_stats, spawners.enemy_attack_cadence, spawners.enemy_regen |
| Main.unity EnemySpawner.config | `WeaponType` | WeaponType enum | enemy damage type | spawners.enemy_attack_cadence, spawners.event_plan_override |
| Main.unity EnemySpawner.config | `ArmorConfig` | ArmorConfig asset (list {Type, Resist}) | per-weapon-type resist of the enemy | spawners.event_plan_override, spawners.difficulty_semantics |
| Main.unity EnemySpawner.config | `Cooldown` | float seconds | small respawn cooldown; ignored for AB-eligible spawners | spawners.base_cooldown, spawners.ab_spawner_override_predicate, spawners.big_cooldown |
| Main.unity EnemySpawner.config | `BigCooldown` | float seconds | charge refill period (86400 on 710 spawners) | spawners.big_cooldown, spawners.charge_restore |
| Main.unity EnemySpawner.config | `ChargesBeforeBigRespawn` | int | spawns per big-cooldown cycle (non-AB spawners) | spawners.effective_charges |
| Main.unity EnemySpawner.config | `SyncBigRespawnToUtc` | bool | align refill to UTC period boundaries | spawners.big_cooldown, spawners.charge_restore |
| Main.unity EnemySpawner.config | `AggroRange / AggroDuration` | float | aggro radius and persistence seconds | spawners.enemy_attack_cadence |
| Main.unity EnemySpawner.config | `IsStatic` | bool | tree/static mob (no movement); uses the feature_unlock static_souls_drop gate; excluded from the user OptimumTime mark | spawners.soul_reward_grant, spawners.progress_markers |
| Main.unity EnemySpawner.config | `DisableAfterFirstClear` | bool | one-shot spawner | spawners.disable_after_first_clear, spawners.island_cleared_predicate |
| Main.unity EnemySpawner.config | `LocationNumber` | int 1..17 | island; key for AB tables, stat_drop, map_spawner_timer_reduce, island_medal_bonus, AB HP multiplier | spawners.base_cooldown, spawners.stat_reward_grant, spawners.enemy_spawn_stats, spawners.island_cleared_predicate |
| Main.unity EnemySpawner.config | `StatRewards` | List<StatModifier> | permanent stat granted on devour | spawners.stat_reward_grant, spawners.daily_yield |
| Main.unity EnemySpawner.config | `ItemReward` | ItemConfig ref | item dropped on kill; switches cooldown/charge rules to item rules | spawners.item_drop_copies, spawners.base_cooldown, spawners.effective_charges |
| Main.unity EnemySpawner.config | `SlotReward` | string key | item slot unlocked on kill | spawners.slot_reward |
| Main.unity EnemySpawner.config | `PortalGemReward` | int | portal gem index granted | spawners.portal_gem |
| Main.unity EnemySpawner.config | `SoulReward / SoulAlwaysAvailable` | string / bool | soul rarity key and unlock bypass | spawners.soul_reward_grant |
| Main.unity EnemySpawner.config | `CurrencyReward / CurrencyRewardAmount` | string / double | currency per devour | spawners.currency_reward |
| Main.unity EnemySpawner.config | `Gates` | List<Gate> | gates opened on kill (location unlock) | spawners.gate_location_unlock |
| Main.unity EnemySpawner.config | `RewardedPoolIndex` | int | rewarded-ad pool high-water mark | spawners.progress_markers |
| Main.unity EnemySpawner.config | `IsDays / OptimumTime` | bool / float | design farm time (minutes or days) | spawners.optimum_time_semantics, spawners.optimum_time_days_converter, spawners.progress_markers |
| Main.unity EnemySpawner.config | `RelativeOptimumTime` | float | normalized position within the island's OT span | spawners.relative_optimum_time |
| Main.unity EnemySpawner.config | `AverageFightTime` | float seconds | target kill time for the whole pack (bake) | spawners.difficulty_semantics |
| Main.unity EnemySpawner.config | `PriorityMultiplier` | float | bot priority weight (consumer stubbed) | spawners.priority_handbalance_semantics |
| Main.unity EnemySpawner.config | `HandBalance` | bool | exclude from auto-balance (inferred) | spawners.priority_handbalance_semantics |
| Main.unity EnemySpawner.config | `Difficulty` | float | net HP fraction lost per fight (bake and forecast) | spawners.difficulty_semantics, spawners.difficulty_bulk_setter |
| Main.unity EnemySpawner.config | `IsEliteBoss` | bool | selects the elite AB cooldown/charges table; renames the reward to map.elite | spawners.ab_cooldown_table, spawners.ab_charges_table |
| Main.unity EnemySpawner.config | `IsVip / RequiredVipLevel` | bool / int | VIP spawner (needs unlocked location >= 2); RequiredVipLevel is unused | spawners.vip_spawner_gate |
| Main.unity EnemySpawner.config | `IsEvent / EventDef` | bool / EventSpawnerDef | event spawner; Key, UnitType, Variants | spawners.event_plan_override, spawners.event_variant_currency, spawners.event_miniboss_spawn_gate, spawners.event_spawner_init |
| Main.unity EventSpawnerVariant | `CurrencyAmount / CurrencyKey / Prefab` | float / string / EnemyUnit | event currency per devour and visual variant | spawners.event_variant_currency, spawners.event_crystal_mob_bonus |
| Main.unity SpawnerRelativeOptimumTimeTool | `Islands[] {LocationNumber, IsDays, MinOptimumTime, MaxOptimumTime, MinSpawner, MaxSpawner}` | List<IslandOptimumTimeRange> | per-island OT span (L1 0-30 min ... L17 150-180 days) | spawners.relative_optimum_time |
| Main.unity SpawnerOptimumTimeDaysConverter | `locationFrom` | int (=4) | first island using day units | spawners.optimum_time_days_converter |
| SpawnerDifficultyBulkSetter (component) | `locationFrom, locationTo, maxSourceDifficulty, targetDifficulty` | int,int,float,float | bulk Difficulty edit params | spawners.difficulty_bulk_setter |
| Main.unity Gate | `NextLocationNumber` | int | location unlocked by the gate | spawners.gate_location_unlock, spawners.location_trigger |
| Locations Config.asset | `Locations[] {Name, Number, PreferredWeaponType, PreferredArmorType}` | List<LocationConfig> | 17 islands; PreferredWeaponType = island's attack type (bake defence pick, events, quests); PreferredArmorType = island weakness/resist (events, quests, UI) | spawners.event_plan_override, spawners.difficulty_semantics |
| Player Config.asset | `Stats` | List<Stat> | player base stats: Health 100, MoveSpeed 1, HealthRegen 0.5, Damage 0, AttackRange 3.4, AttackCooldown 0; flat dmg/def 0 and percent dmg/def 1 for Stab/Cut/Buck; Lifesteal 0, Evasion 0, CritChance 1, CritDamage 1, UniversalDef 0/0, SuperCrit 0/1, GlobalAttack 0, GlobalDefence 0, BlockChance 0, BlockAmount 1. Magic types, LifestealPercent and VisualScale are absent | player stat aggregation (base values that spawner rewards add to) |
| Player Config.asset | `Slots` | List<ItemSlotModel> | weapon_1 (default unlocked), weapon_2..4, weapon_5_vip, helmet_1, shield_1 | spawners.slot_reward |
| Player Config.asset | `StartingWeapon` | ItemConfig ref (guid 734ff42a...) | initial weapon; same item as an L7 item spawner reward | player init |
| ABTests/SpawnerCooldownsData.cs (remote AB spawner_cooldowns_v3) | `nonElite/elite {cooldownMinutes, charges}` | ValueByLocation / IntValueByLocation | runtime cooldown and charges override | spawners.ab_cooldown_table, spawners.ab_charges_table |
| ABTests/EnemyHpDamageMultData.cs (remote AB enemy_hp_damage_mult_v3) | `damageMultiplier (default 1), rewardedValueMultiplier 0.5` | ValueByLocation / double | scales enemy Health and Damage by location | spawners.enemy_spawn_stats |
| Meta Stats Config.asset | `stat_drop, map_spawner_timer_reduce, items_rarity_timer, items_rarity_charges, items_rarity_drop_amount, soul_drop_bonus, souls_rarity_amount, souls_drop_unlock, feature_unlock, island_medal_bonus` | MetaStat | meta-upgrade levers on spawner timing, charges and rewards | spawners.base_cooldown, spawners.effective_charges, spawners.item_drop_copies, spawners.stat_reward_grant, spawners.soul_reward_grant, spawners.island_medal_progress |
| Enemy code constants | `EnemyRegenSystem HEAL_PER_SECOND=0.05, NO_DAMAGE_TIME_TO_HEAL=5; leash 30; retreat speed x2; IslandMasteryModel MaxMedalLevel=5, BonusPerMedalLevel=0.1; crystal event reward 0.4*RPM` | const | hard-coded gameplay numbers | spawners.enemy_regen, spawners.enemy_leash_heal, spawners.enemy_attack_cadence, spawners.island_medal_progress, spawners.event_crystal_mob_bonus |
| Simulation Config.asset | `UseSpawnerCharges=1, SpawnerChargesChunkMinutes=1440, TimeChunkDurationMinutes=30, IsCapOptimumTime=1, EarlySpawnerOptimumTimeOffsetStartLocation=11, ...RelativePart=0.5, ...Multiplier=1` | mixed | how simulation interprets spawner OT and charges | spawners.optimum_time_semantics, spawners.relative_optimum_time |

## 枚举

| 枚举 | 文件 | 取值 |
|---|---|---|
| `StatType` | [Unit/StatType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatType.cs) | 0=MovementSpeed,1=Health,2=HealthRegen,3=Damage,4=AttackRange,5=AttackCooldown,6=StabDamageFlat,7=StabDamagePercent,8=CutDamageFlat,9=CutDamagePercent,10=MagicDamageFlat,11=MagicDamagePercent,12=BuckshotDamageFlat,13=BuckshotDamagePercent,14=StabDefenceFlat,15=StabDefencePercent,16=CutDefenceFlat,17=CutDefencePercent,18=MagicDefenceFlat,19=MagicDefencePercent,20=BuckshotDefenceFlat,21=BuckshotDefencePercent,22=LifestealFlat,23=Evasion,24=CritChance,25=CritDamagePercent,26=UniversalDefenceFlat,27=UniversalDefencePercent,28=SuperCritChance,29=SuperCritDamagePercent,30=GlobalAttack,31=GlobalDefence,32=LifestealPercent,33=BlockChance,34=BlockAmount,35=VisualScaleMultiplier |
| `WeaponType` | [Enemy/WeaponType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/WeaponType.cs) | 0=Stabbing,1=Cutting,2=Magic,3=Buckshot,4=Universal |
| `ModifierType` | [Unit/ModifierType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/ModifierType.cs) | 0=Flat,1=PercentAdditive,2=PercentMultiplicative,3=PostFormulaAdditive,4=PreAdditivePostMultiplicative,5=PostFormulaMultiplicative,6=PostFormulaFlat,7=EventPercentModifier |
| `ModifierSource` | [Unit/ModifierSource.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/ModifierSource.cs) | 0=Enemy,1=Item,2=AbilityPermanent,3=AbilityTemporary,4=Shop,5=Ads,6=Reward,7=LevelPass,8=Crystal,9=CrystalUpgrade,10=GraphUpgrade,11=MineUpgrade,12=CrystalEvent,13=CrystalPass,14=SoulPass,15=Vip,16=SecretChest,17=CrystalGuardian,18=Quest,19=Cashback |
| `EventUnitType` | [Enemy/EventUnitType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EventUnitType.cs) | 0=Mob,1=MiniBoss,2=Boss |
| `Rarity` | [Player/Inventory/Rarity.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/Rarity.cs) | 0=Common,1=Uncommon,2=Rare,3=Epic,4=Legendary,5=Divine,6=Mythical |
| `EnemySpawner.SpawnMode` | [Enemy/EnemySpawner.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs) | 0=Restore (no charge use),1=Normal (uses 1 charge),2=Forced |

## 未解决问题

1. Is the AB spawner_cooldowns_v3 default (non-elite 2(L+1) min, charges 3*Count; elite 60/90/120/150/180 min, 2*Count) what live players got, or does a remote variant override it? Scene Cooldowns are 2 minutes longer for L1-13 (min(1800, 120(L+2)) s), so it matters which one is canonical for the model.
2. Time unit conflict. SpawnerOptimumTimeDaysConverter (native 0x150ced0) and TimeChunkDurationMinutes use 30 playtime minutes per day. Combat Balance Config.IslandTargetDays and SimulationConfig.GetReCalcOptimumTime use 60 minutes per day (L1 30 min = 0.5, L3 240 min = 4.0). Which applies to L1-3 bakes?
3. PriorityMultiplier and HandBalance have no consumer in recovered code. Their likely consumers (Simulation/Systems/SpawnerData.GetPriority/GetStaticPriority, SimulationConfig bake/apply) are empty.
4. EnemySpawner core scheduling (ProcessSpawnTick, charges, big cooldown, HandleDeath, HandleDevour) has no native VA annotations. Exact rounding (ceil of cooldown), the order of charge use (per spawn, not per kill) and the zero-cooldown collapse should be checked against ARM64.
5. TryInitializeEventSpawner is stubbed to false, so event spawners never spawn. ApplyTimeSkip, ConsumeAllCharges and DefeatAllUnits are empty. CreeperExplosionSystem and EnemySoulSystem are empty, so the creeper explosion damage formula (DamageMaxHealthPercent) and soul-indicator logic are unknown.
6. RequiredVipLevel is serialized but never read. The VIP spawner gate in code is only LastUnlockedLocation >= 2. The original VIP-level check may be missing.
7. Island mastery eligibility differs between IslandMasteryResolver (excludes DisableAfterFirstClear, creepers, chargesTotal==0) and DamageCalculator.IsIslandMasteryEligible (native 0x140c264; excludes quest mobs only). Confirm whether the bonus applies to one-shot or creeper enemies.
8. 22 of 717 spawners have RelativeOptimumTime inconsistent with (OT-Min)/(Max-Min). Is OT or rel authoritative for them? Their Health/Damage were baked from OT (via GetReCalcOptimumTime).
9. One L15 spawner has Stats Health = inf in Main.unity. Check whether this is intended (an invulnerable prop) or a data error.
10. The EnemySpawnersDebugger 'GPM' output format is unknown (editor body missing). Check whether the serialized info list in Main.unity holds per-location gain-per-minute aggregates that could validate the yield model.
