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
| [`spawners.event_plan_override`](#spawnersevent_plan_override) | Event spawner forecast override (enemy stats, weapon, resist, stat reward) | 原生证据恢复 | `runtime_formula` |
| [`spawners.enemy_spawn_stats`](#spawnersenemy_spawn_stats) | Enemy instance stats at spawn (incl. AB HP/Damage multiplier) | 重建（无原生标注） | `runtime_formula` |
| [`spawners.creeper_explosion`](#spawnerscreeper_explosion) | Creeper mob explosion damage (stub) | 空壳 | `runtime_formula` |
| [`spawners.enemy_far_culling`](#spawnersenemy_far_culling) | Enemy far-distance culling (IsFar / FarFromPlayer / FarOnScreen) | 重建（无原生标注） | `config_rule` |
| [`spawners.zero_cooldown_collapse`](#spawnerszero_cooldown_collapse) | Zero-cooldown charge collapse (kill reward multiplier) | 重建（无原生标注） | `runtime_formula` |
| [`spawners.effective_charges`](#spawnerseffective_charges) | Effective charges before big respawn | 重建（无原生标注） | `runtime_formula` |
| [`spawners.base_cooldown`](#spawnersbase_cooldown) | Effective small (respawn) cooldown | 重建（无原生标注） | `runtime_formula` |
| [`spawners.event_crystal_mob_bonus`](#spawnersevent_crystal_mob_bonus) | Crystal-event mob extra stat reward | 重建（无原生标注） | `runtime_formula` |
| [`spawners.charge_state_init`](#spawnerscharge_state_init) | Charge state initialization and capacity-rise top-up | 重建（无原生标注） | `runtime_formula` |
| [`spawners.ab_spawner_override_predicate`](#spawnersab_spawner_override_predicate) | Whether AB cooldown/charges override the scene config | 重建（无原生标注） | `config_rule` |
| [`spawners.ab_cooldown_table`](#spawnersab_cooldown_table) | AB spawner cooldown by location (spawner_cooldowns_v3) | 纯数据 | `lookup_table` |
| [`spawners.ab_charges_table`](#spawnersab_charges_table) | AB spawner charges | 纯数据 | `lookup_table` |
| [`spawners.respawn_tick`](#spawnersrespawn_tick) | Respawn scheduling (small cooldown, catch-up spawns) | 重建（无原生标注） | `runtime_formula` |
| [`spawners.death_timer_reset`](#spawnersdeath_timer_reset) | Small-cooldown restart on kill (and on devour for event mini-bosses) | 重建（无原生标注） | `runtime_formula` |
| [`spawners.big_cooldown`](#spawnersbig_cooldown) | Big cooldown start and UTC alignment | 重建（无原生标注） | `runtime_formula` |
| [`spawners.charge_restore`](#spawnerscharge_restore) | Charge refill (UTC period roll-over or big cooldown expiry) | 重建（无原生标注） | `runtime_formula` |
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
| [`spawners.optimum_time_days_converter`](#spawnersoptimum_time_days_converter) | OptimumTime minutes <-> days conversion | 原生证据恢复 | `runtime_formula` |
| [`spawners.relative_optimum_time`](#spawnersrelative_optimum_time) | RelativeOptimumTime <-> OptimumTime within island (SpawnerRelativeOptimumTimeTool) | 编辑器工具缺失 | `editor_generator` |
| [`spawners.difficulty_bulk_setter`](#spawnersdifficulty_bulk_setter) | Bulk Difficulty setter | 编辑器工具缺失 | `editor_generator` |
| [`spawners.debugger_gpm_export`](#spawnersdebugger_gpm_export) | Spawner debugger GPM / CSV export | 编辑器工具缺失 | `editor_generator` |
| [`spawners.difficulty_semantics`](#spawnersdifficulty_semantics) | Difficulty / AverageFightTime meaning (bake inputs) | 纯数据 | `config_rule` |
| [`spawners.optimum_time_semantics`](#spawnersoptimum_time_semantics) | OptimumTime / IsDays meaning | 纯数据 | `config_rule` |
| [`spawners.priority_handbalance_semantics`](#spawnerspriority_handbalance_semantics) | PriorityMultiplier / HandBalance meaning | 纯数据 | `config_rule` |
| [`spawners.daily_yield`](#spawnersdaily_yield) | Derived per-spawner daily yield (model identity) | 重建（无原生标注） | `simulation` |
| [`spawners.geode_kill_count`](#spawnersgeode_kill_count) | Geode timer reduction per spawner kill | 重建（无原生标注） | `runtime_formula` |
| [`spawners.dummy_spawn_block`](#spawnersdummy_spawn_block) | Dummy spawner never spawns (reconstruction inconsistency) | 重建（无原生标注） | `config_rule` |
| [`spawners.full_or_depleted_wake_schedule`](#spawnersfull_or_depleted_wake_schedule) | Wake-up scheduling for full or charge-depleted spawners | 重建（无原生标注） | `runtime_formula` |
| [`spawners.item_copies_reward_multiplier_coupling`](#spawnersitem_copies_reward_multiplier_coupling) | Item-drop copies become the devour stat and soul multiplier | 重建（无原生标注） | `runtime_formula` |
| [`spawners.unconsumed_numeric_fields`](#spawnersunconsumed_numeric_fields) | Numeric config fields with no consumer | 纯数据 | `config_rule` |

## 公式

### `spawners.event_plan_override`

**Event spawner forecast override (enemy stats, weapon, resist, stat reward)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 合并自：`spawners.event_plan_override`、`combat.event_enemy_armor_override`
- 原生地址：`0x150639c`

```text
Called in TrySpawnUnit before EnemyUnit.Initialize when config.IsEvent && EventDef != null. If CurrentEvent == null or CurrentEvent.Key != EventDef.Key, return. enemyInfo = GetEventEnemyInfo(UnitType); if missing, return. config.WeaponType = enemyInfo.AttackType; for each armor in config.ArmorConfig.Armors: armor.Resist = (armor.Type == eventResistType && armor.Type != Magic) ? -0.9 : 0; if a Health stat exists: Health.BaseValue = enemyInfo.Health; if a Damage stat exists: Damage.BaseValue = enemyInfo.Damage. Then rewardInfo = GetEventRewardInfo(UnitType); if missing or !HasReward, return; else config.StatRewards.Clear() and add one Flat modifier {StatType = rewardInfo.RewardStatType, Value = (float)rewardInfo.RewardValue, Source Enemy, Name 'event.<key>.<unittype lowercase>'} (detailed as meta.event_plan_stat_reward).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `enemyInfo` | forecast event enemy stats for UnitType (AttackType, Health, Damage) | EventsRepository.GetEventEnemyInfo (events/simulation forecast) |
| `rewardInfo` | forecast event stat reward for UnitType (HasReward, RewardStatType, RewardValue) | EventsRepository.GetEventRewardInfo (events/simulation forecast) |
| `eventResistType` | current event weakness type | EventModel.Data.ResistType (from LocationConfig.PreferredArmorType) |

**输出**：mutated EnemySpawnerConfig (WeaponType, Stats Health/Damage, StatRewards) and its shared ArmorConfig asset, before EnemyUnit.Initialize

**代码**：[Enemy/EnemySpawner.cs:1141-1171](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1141)；[Enemy/EnemySpawner.cs:961-964](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L961)；[Services/Damage/DamageCalculator.cs:125-139](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L125)

**状态依据**：ApplyEventPlanToConfig carries 'Original APK ARM64 ELF VA 0x150639c; exact forecast override before EnemyUnit.Initialize' (EnemySpawner.cs:1143).

**配置参数**：`EnemySpawnerConfig.ArmorConfig (Event Armor Dynamic Config.asset)`；`EnemySpawnerConfig.Stats`；`EnemySpawnerConfig.StatRewards`；`EnemySpawnerConfig.WeaponType`；`EventSpawnerDef`；`LocationsConfig.PreferredArmorType`

**使用方**：combat.outgoing_damage_to_enemy；combat.incoming_damage_to_player；meta.event_plan_stat_reward；events subsystem

**备注**：Resist -0.9 makes the event enemy take 190% damage from its weakness type. This is verified: DamageCalculator multiplies damage by (1 - resist) (DamageCalculator.cs:139). Magic is never weakened. The ArmorConfig is a ScriptableObject (Enemy/ArmorConfig.cs). Event spawners reference the shared 'Event Armor Dynamic Config.asset' (6 references in Main.unity), so the resist mutation is shared in place across those spawners. Stats, WeaponType and StatRewards mutate the per-spawner config, and EnemyUnit.Initialize then copies them. Event spawners skip the AB HP/Damage multiplier (spawners.enemy_spawn_stats). The StatRewards replacement (cleared, then one Flat reward of RewardValue) runs only if the reward info exists and HasReward; it is catalogued as its own quantity in meta.event_plan_stat_reward. Crystal-event mobs then get an extra reward appended per unit (spawners.event_crystal_mob_bonus).

**依赖图节点**：输入 `value:forecast.event_plan`、`state:current_event`、`config:LocationsConfig.Locations[].PreferredArmorType`、`config:EnemySpawnerConfig.IsEvent`、`config:EnemySpawnerConfig.EventDef.Key`、`config:EnemySpawnerConfig.EventDef.UnitType`、`config:EnemySpawnerConfig.ArmorConfig`、`config:ArmorConfig.Armors[].Type`、`constant:EnemySpawner.event_weakness_resist`；输出 `value:enemy.override_base_stats`、`value:enemy.damage_type`、`value:enemy.resist_by_damage_type`、`value:enemy.stat_rewards`

**核验记录**（已写入上方字段）：

- 合并复核修正：(a) The merge dropped the spawners member's StatRewards sub-formula from the expression, along with its 'rewardInfo' variable and the 'EnemySpawnerConfig.StatRewards' config param. The planned home for that detail, meta.event_plan_stat_reward, does not hold it fully: it lacks the HasReward guard, the Clear() of the shared config list and the per-spawner config mutation. The result is that the native method 0x150639c loses its EnemySpawnerConfig.StatRewards edge in the dependency graph. The Health and Damage writes also run only when that stat exists in config.Stats (FirstOrDefault != null). (b, minor) The notes hedge on '(1 - resist)', but DamageCalculator.cs:139 confirms damage *= (1 - resist), so the 190% weakness multiplier is verified.

### `spawners.enemy_spawn_stats`

**Enemy instance stats at spawn (incl. AB HP/Damage multiplier)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 合并自：`spawners.enemy_spawn_stats`、`combat.enemy_spawn_hp_damage_ab_multiplier`

```text
Initialize: Stats[i] = new Stat(config.Stats[i].BaseValue); CurrentHealth = Health.Value; Agent.speed = MovementSpeed.BaseValue; SpawnerStatRewardMultiplier = 1; StatRewards = copy of config.StatRewards. Then m = ABTest EnemyHpDamageMult.damageMultiplier.overridesByLocation[L] ?? defaultValue. If !IsEvent && m != 1: Health.Base = Math.Round(Health.Base * m) and Damage.Base = Math.Round(Damage.Base * m) (banker's rounding, MidpointRounding.ToEven); rebuild the stat lookup; CurrentHealth = GetStatValue(Health).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `m` | AB enemy_hp_damage_mult_v3 per-location multiplier (default 1) | ABTests/ABTestService.cs:18-19; ABTests/EnemyHpDamageMultData.cs (damageMultiplier defaultValue 1); ABTests/ABKeys.cs:7 (key 'enemy_hp_damage_mult_v3', remote config) |
| `config.Stats` | authored base stats: MovementSpeed(0), Health(1), HealthRegen(2), Damage(3), AttackRange(4), AttackCooldown(5) | EnemySpawnerConfig.Stats (scene data) |

**输出**：EnemyUnit.Stats, CurrentHealth, Agent.speed

**代码**：[Enemy/EnemySpawner.cs:963-977](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L963)；[Enemy/EnemyUnit.cs:140-155](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L140)；[ABTests/ABTestService.cs:18-19](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L18)；[ABTests/EnemyHpDamageMultData.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/EnemyHpDamageMultData.cs)；[ABTests/ABKeys.cs:7](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABKeys.cs#L7)；[ABTests/Systems/EnemyHpDamageMultSystem.cs:31-66](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/Systems/EnemyHpDamageMultSystem.cs#L31)

**状态依据**：TrySpawnUnit and EnemyUnit.Initialize have no native annotation.

**配置参数**：`EnemySpawnerConfig.Stats[Type=1,3]`；`ABTestData.EnemyHpDamageMult.damageMultiplier (AB key enemy_hp_damage_mult_v3)`

**使用方**：combat.incoming_damage_to_player；combat.outgoing_damage_to_enemy；Services/Damage/DamageCalculator；Simulation fight time (GetCachedEnemyHpDamageMultiplier abMultiplier at [Simulation/Systems/SimulationSystem.cs:656-662](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L656))

**备注**：HP and Damage are scaled by the same factor. The simulation applies m as an unrounded multiplier on fight time, while the runtime rounds. When the AB cohort changes at runtime, live non-event enemies are rescaled by next/prev; that is a separate entry, combat.ab_enemy_hp_damage_live_rescale (ABTests/Systems/EnemyHpDamageMultSystem.cs:31-66). The same AB data also carries rewardedValueMultiplier (default 0.5, ABTestService.cs:20), which is a separate lever. All 717 spawners carry exactly the six stat types listed, though the list order varies per spawner. Health grows from about 1e1-1e3 at L1-2 to about 4e8-5.8e10 at L17. The one Health=Infinity spawner is BossTower_c296e152609ae5849bb7d52b03df895a (L15). It has an ItemReward, Count 0, ChargesBeforeBigRespawn=0 and Cooldown 1800, so it is a boss-tower placeholder or prop, not a farmable mob. Leave it out of the HP curve model.

**依赖图节点**：输入 `config:EnemySpawnerConfig.Stats[Health]`、`config:EnemySpawnerConfig.Stats[Damage]`、`config:EnemySpawnerConfig.Stats[MovementSpeed]`、`config:EnemySpawnerConfig.Stats[HealthRegen]`、`config:EnemySpawnerConfig.Stats[AttackRange]`、`config:EnemySpawnerConfig.Stats[AttackCooldown]`、`value:enemy.override_base_stats`、`config:EnemySpawnerConfig.StatRewards[].Value`、`config:EnemySpawnerConfig.StatRewards[].StatType`、`config:EnemySpawnerConfig.IsEvent`、`config:EnemySpawnerConfig.LocationNumber`、`config:ABTestDataConfig.enemyHpDamageMult.variants[].data.damageMultiplier.defaultValue`、`config:ABTestDataConfig.enemyHpDamageMult.variants[].data.damageMultiplier.overridesByLocation.keyData`、`config:ABTestDataConfig.enemyHpDamageMult.variants[].data.damageMultiplier.overridesByLocation.valueData`、`state:ab_cohort`；输出 `value:enemy.ab_hp_damage_multiplier`、`value:enemy.stat_base`、`value:enemy.current_hp`、`value:enemy.move_speed_effective`、`value:enemy.stat_rewards`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `spawners.enemy_spawn_stats`）：Verified in Scenes/Main.unity (Key BossTower_c296..., Stats Type 1 BaseValue Infinity, Cooldown 1800, ChargesBeforeBigRespawn 0, LocationNumber 15, ItemReward set, Count 0). EnemySpawner.cs:972 uses Math.Round(double), which rounds half to even by default. The notes and expression are updated.
- 合并复核修正：(a) The merge lost member details. The spawners member's variable source named the AB key ('enemy_hp_damage_mult_v3', [ABTests/ABKeys.cs:7](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABKeys.cs#L7)) and [ABTests/EnemyHpDamageMultData.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/EnemyHpDamageMultData.cs) (damageMultiplier default 1); the merged entry keeps neither. Its code_ref to [ABTests/Systems/EnemyHpDamageMultSystem.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/Systems/EnemyHpDamageMultSystem.cs) was also dropped and survives only as text in the notes. The original range was 31-73, but the file has 68 lines, so 31-66 is correct. Without these, the dependency graph loses the AB config-param node identity.

### `spawners.creeper_explosion`

**Creeper mob explosion damage (stub)**

- 状态：**空壳**（`stub`）
- 类型：`runtime_formula`
- 合并自：`spawners.creeper_explosion`、`combat.creeper_explosion_stub`

```text
UNKNOWN (stub). Intended behavior inferred from field names and the CreeperMobState enum (Alive=0, Charging=1, Exploded=2, Finalized=3): after ExplosionDelay seconds of Charging, explode and deal damage ~ DamageMaxHealthPercent * target MaxHP (likely the player's) to units within ExplosionRadius via UnitDamageSystem, then set State = Finalized so EnemyDeathSystem lets the creeper die. The shipped prefab values are ExplosionDelay=4, ExplosionRadius=20, DamageMaxHealthPercent=0.95.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ExplosionDelay` | charge time before explosion (s); Creeper.prefab = 4 | CreeperMobComponent.ExplosionDelay (GameObject/Creeper.prefab) |
| `ExplosionRadius` | explosion radius (units); Creeper.prefab = 20 | CreeperMobComponent.ExplosionRadius |
| `DamageMaxHealthPercent` | damage as a fraction of max health; Creeper.prefab = 0.95 (a fraction, not a percent) | CreeperMobComponent.DamageMaxHealthPercent |
| `State` | creeper lifecycle state; reset to Alive in CreeperMobProvider.OnEnable | CreeperMobComponent.State (NonSerialized), Enemy/Components/CreeperMobState.cs |

**输出**：explosion damage to the player (missing in this build)

**代码**：[Enemy/Systems/CreeperExplosionSystem.cs:37-55](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/CreeperExplosionSystem.cs#L37)；[Enemy/Components/CreeperMobComponent.cs:8-26](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Components/CreeperMobComponent.cs#L8)；[Enemy/Components/CreeperMobState.cs:3-9](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Components/CreeperMobState.cs#L3)；[Enemy/CreeperMobProvider.cs:8-20](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/CreeperMobProvider.cs#L8)；[Enemy/Systems/EnemyDeathSystem.cs:34-35](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/EnemyDeathSystem.cs#L34)；[Core/SystemsInstaller.cs:68](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Core/SystemsInstaller.cs#L68)

**状态依据**：CreeperExplosionSystem.OnAwake/OnUpdate/StartExplosion/Explode/FinalizeDeath all have empty bodies and no native annotation, yet the system is registered (Core/SystemsInstaller.cs:68) and injects UnitDamageSystem, EnemyCastSystem, PlayerModel and WorldVfxService, so the original had runtime logic. Creeper.prefab carries real tuning data (lines 476-482) and Scenes/Main.unity references the prefab once.

**配置参数**：`CreeperMobComponent.DamageMaxHealthPercent`；`CreeperMobComponent.ExplosionRadius`；`CreeperMobComponent.ExplosionDelay`；`CreeperMobComponent.ExplosionVfxLifetime`

**使用方**：combat.death_threshold；combat (incoming damage to player)

**备注**：The expression is inferred from field names only. EnemyDeathSystem suppresses UnitDeadMarker while a creeper's State != Finalized (EnemyDeathSystem.cs:34-35; CreeperMobProvider.ShouldDelayDeath says the same). Because nothing ever advances State past Alive, creepers can never finalize death in this reconstruction. Event despawn requests bypass the suppression. With DamageMaxHealthPercent=0.95 the intended hit is close to lethal if it targets player MaxHP, so a balance model should treat creepers as near one-shot hazards with a 4 s telegraph and a 20-unit radius.

**依赖图节点**：输入 `config:CreeperMobProvider.serializedData.DamageMaxHealthPercent`、`config:CreeperMobProvider.serializedData.ExplosionRadius`、`config:CreeperMobProvider.serializedData.ExplosionDelay`、`value:player.stat_raw[Health]`、`state:unit_positions`、`state:frame_delta_time`；输出 `value:player.creeper_explosion_damage`

### `spawners.enemy_far_culling`

**Enemy far-distance culling (IsFar / FarFromPlayer / FarOnScreen)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`config_rule`
- 合并自：`spawners.enemy_far_culling`、`combat.enemy_far_activation`

```text
For each enemy without IgnoreFarMarker: d2 = min(|playerPos - enemyPos|^2, |cameraFollow.PositionOfAttack - enemyPos|^2). far = d2 > 3600 (60 units): unit.IsFar = far; FarFromPlayerMarker set iff far; if !DisableAgent: NavMesh agent enabled iff (!far && !IsDead), VisualRoot active iff !far. FarOnScreenMarker set iff d2 > 900 (30 units).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `FarDistance = 3600` | squared activation radius (60 units) | constant EnemyVisibilitySystem.FarDistance |
| `FarOnScreenDistance = 900` | squared on-screen radius (30 units) | constant EnemyVisibilitySystem.FarOnScreenDistance |
| `PositionOfAttack` | camera follow attack position | CameraFollowSystem.PositionOfAttack |

**输出**：UnitCore.IsFar, FarFromPlayerMarker, FarOnScreenMarker, agent and visual enable state

**代码**：[Enemy/Systems/EnemyVisibilitySystem.cs:18-19](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/EnemyVisibilitySystem.cs#L18)；[Enemy/Systems/EnemyVisibilitySystem.cs:36](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/EnemyVisibilitySystem.cs#L36)；[Enemy/Systems/EnemyVisibilitySystem.cs:53-82](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/EnemyVisibilitySystem.cs#L53)；[Enemy/Systems/EnemyRegenSystem.cs:16](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/EnemyRegenSystem.cs#L16)；[Enemy/EnemyAttackSystem.cs:58](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L58)；[Enemy/EnemyAttackSystem.cs:108-109](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L108)；[Enemy/EnemyAttackSystem.cs:274-275](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L274)；[Player/PlayerAttackSystem.cs:218](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L218)；[Enemy/EnemyUnit.cs:191-198](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L191)；[Enemy/Systems/EnemyHealthBarSystem.cs:22](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/EnemyHealthBarSystem.cs#L22)；[Enemy/Systems/EnemySpawnerCooldownSystem.cs:25](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/EnemySpawnerCooldownSystem.cs#L25)

**状态依据**：EnemyVisibilitySystem has no native annotation. PlayerAttackSystem.FindTarget is annotated 'Native ELF VA 0x1440b54', but it is a consumer.

**使用方**：combat.enemy_regen (EnemyRegenSystem excludes FarFromPlayerMarker)；combat.enemy_leash_and_aggro；combat.enemy_attack_cycle (EnemyAttackSystem skips IsFar)；combat.player_attack_range_and_targeting (FindTarget skips IsFar)；EnemyUnit.Tick animator gating；EnemyHealthBarSystem (no health bar with FarOnScreenMarker)

**备注**：Far enemies do not regenerate, attack, get targeted, path or animate, and they keep their current HP. The leash full-heal in EnemyAttackSystem resets CurrentHealth to max HP each frame when the enemy is too far from the player. Ground enemies use player distance^2 > 900 (EnemyAttackSystem.cs:108-109); flyers use distance > 30 (EnemyAttackSystem.cs:274-275). The heal is measured against the player only, not the camera attack position. It runs only for enemies that are non-static (NonStaticEnemies), not dead and not far. It is also skipped for enemies in the spawn animation, enemies with the escape marker or stun marker, and BossArena units. Ground enemies additionally skip it when they are a vampire spirit or casting. So, outside those exclusions, an enemy more than 30 units from the player but within 60 units of the player or the camera attack position snaps back to full HP. Far enemies (more than 60 from both) skip both regen and the leash heal. EnemySpawnerCooldownSystem keeps its own separate 900 constant (FarOnScreenDistance) for spawner markers.

**依赖图节点**：输入 `state:unit_positions`、`constant:EnemyVisibilitySystem.FarDistance`、`constant:EnemyVisibilitySystem.FarOnScreenDistance`、`value:unit.is_dead`；输出 `value:enemy.is_far`

**核验记录**（已写入上方字段）：

- 合并复核修正：(a) and (b) The merged notes state as fact that an enemy more than 30 units from the player but not far 'is reset to full HP every frame'. That is too broad. In EnemyAttackSystem, the leash heal runs only for non-static, non-dead, non-far enemies that are not in the spawn animation and do not have the escape marker, the stun marker or a BossArena unit component. Ground enemies also skip it when they are a vampire spirit or casting. The ground check uses squared player distance > 900, but far-ness uses min(player, camera attack position), so an enemy far from the player but near the attack position also gets the heal. The merge also dropped the spawners member's consumer id spawners.enemy_leash_heal and its code_ref [Enemy/Systems/EnemySpawnerCooldownSystem.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/EnemySpawnerCooldownSystem.cs) (the separate 900 constant).

### `spawners.zero_cooldown_collapse`

**Zero-cooldown charge collapse (kill reward multiplier)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 合并自：`spawners.zero_cooldown_collapse`、`meta.zero_cooldown_charge_burst`

```text
In HandleDeath: if (ceil(GetBaseCooldown()) == 0 && IsChargesEnabled && unit.ItemReward == null) { r = RemainingCharges; RemainingCharges = 0; mult = max(1, r + 1) } else mult = 1. unit.SpawnerStatRewardMultiplier = mult. Then ReduceGeodeTimerOnKillCommand.KillCount = additionalCharges + mult. If an item was actually granted (copies > 0), SpawnerStatRewardMultiplier is overwritten with copies (see spawners.item_copies_reward_multiplier_coupling). On devour, CurrentDevourRewardMultiplier = unit.SpawnerStatRewardMultiplier for the reward commands, then reset to 1.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `RemainingCharges (r)` | charges left at kill time; already decremented by 1 when this unit spawned (EnemySpawner.cs:1042-1043), hence the +1 | SpawnerDataUd.remaining_charges |
| `GetBaseCooldown()` | effective small cooldown in seconds before ceil | spawners.base_cooldown |
| `IsChargesEnabled` | !IsEvent && effective charges > 0 | spawners.effective_charges |
| `additionalCharges` | extra charges consumed by the item-drop copies resolver (0 for stat spawners) | ItemDropAmountCalculator.ResolveKillDropCopies (EnemySpawner.cs:1199-1200) |

**输出**：EnemyUnit.SpawnerStatRewardMultiplier (int). It multiplies the stat reward (GrantSpawnerStatRewardCommandHandler), soul reward (via CurrentDevourRewardMultiplier), the OnKill ability context (AbilityContext.KillStatRewardMultiplier, e.g. AddStatEffect) and the geode-timer kill count.

**代码**：[Enemy/EnemySpawner.cs:722-732](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L722)；[Enemy/EnemySpawner.cs:686-689](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L686)；[Enemy/EnemySpawner.cs:1186](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1186)；[Enemy/EnemySpawner.cs:1213-1215](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1213)；[Enemy/EnemySpawner.cs:1359-1381](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1359)；[Enemy/EnemySpawner.cs:1042-1043](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1042)；[Enemy/EnemyUnit.cs:133](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L133)；[Enemy/EnemyUnit.cs:146](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L146)；[Enemy/Commands/GrantSpawnerStatRewardCommandHandler.cs:22](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerStatRewardCommandHandler.cs#L22)；[Enemy/Commands/GrantSpawnerSoulRewardCommandHandler.cs:33](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerSoulRewardCommandHandler.cs#L33)；[Abilities/Core/AbilityContext.cs:33](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Core/AbilityContext.cs#L33)；[Abilities/Effects/AddStatEffect.cs:48-49](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/AddStatEffect.cs#L48)

**状态依据**：No native annotation on ConsumeZeroCooldownStatRewardMultiplier, HandleDeath or HandleDevour. AbilityContext.KillStatRewardMultiplier carries 'Native 0x15f3ab4', but that is a consumer, not this method.

**配置参数**：`EnemySpawnerConfig.Cooldown`；`EnemySpawnerConfig.ChargesBeforeBigRespawn`；`ABTest SpawnerCooldowns (cooldownMinutes, charges)`；`Meta Stats Config.Stats[map_spawner_timer_reduce]`

**使用方**：meta.devour_stat_reward_scaling；meta.soul_grant_on_devour；[Abilities/Core/AbilityContext.cs:33](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Core/AbilityContext.cs#L33) (OnKill AddStat abilities)；Crystals ReduceGeodeTimerOnKillCommand (KillCount = additionalCharges + mult)

**备注**：Design lever and a strong discontinuity for the model: once map_spawner_timer_reduce[L] reaches a stat spawner's raw cooldown, ceil(cd) = 0 and one kill pays the whole remaining charge budget at once. Item spawners cannot hit this path (they use a divisor, so cd never reaches 0, and unit.ItemReward != null is excluded anyway). With the AB SpawnerCooldowns defaults (ABTests/SpawnerCooldownsData.cs), the non-elite raw cooldown is 2(L+1) minutes for L1-13 and 30 minutes otherwise; elites are 60/90/120/150 minutes for L1-4 and 180 minutes otherwise. A raw config Cooldown <= 0 bypasses the meta reduction and gives ceil(raw) <= 0, which is == 0 only when raw is in (-1, 0].

**依赖图节点**：输入 `value:spawner.small_cooldown`、`value:spawner.charges_total`、`value:spawner.remaining_charges`、`config:EnemySpawnerConfig.ItemReward`；输出 `value:spawner.kill_reward_multiplier`、`value:spawner.remaining_charges`

### `spawners.effective_charges`

**Effective charges before big respawn**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 合并自：`spawners.effective_charges`、`meta.spawner_bonus_charges`

```text
usesAB = !IsEvent && !IsVip && ItemReward == null && Cooldown >= 10. effCharges = usesAB ? ABcharges[isElite](L) * Count : ChargesBeforeBigRespawn + (ItemReward != null ? (int)items_rarity_charges.GetValue(Rarity + 1) : 0). IsChargesEnabled = !IsEvent && effCharges > 0. ChargesTotal = IsChargesEnabled ? effCharges : 0. ChargesBase = IsChargesEnabled ? (usesAB ? ABcharges*Count : ChargesBeforeBigRespawn) : 0. ChargesLeft = IsChargesEnabled ? RemainingCharges : 0.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ChargesBeforeBigRespawn` | authored charge budget | EnemySpawnerConfig.ChargesBeforeBigRespawn |
| `ABcharges[isElite](L)` | AB per-unit charges by location; defaults nonElite 3, elite 2 (no location overrides) | ABTestService.GetSpawnerCharges (ABTests/ABTestService.cs:45), ABTests/SpawnerCooldownsData.cs |
| `Count` | units per spawner | EnemySpawnerConfig.Count |
| `items_rarity_charges[Rarity+1]` | flat extra charges for item spawners, by item rarity param (RarityStatParam.From = rarity + 1; amulet graph Params 1..3) | Meta Stats Config items_rarity_charges, Meta/SpawnerChargesCalculator.cs:23-41 |

**输出**：ChargesTotal (int, cached once per frame; the meta bonus is also cached per frame keyed by ModifierVersion). ChargesBase is the same without the meta bonus.

**代码**：[Enemy/EnemySpawner.cs:697-720](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L697)；[Enemy/EnemySpawner.cs:307-337](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L307)；[Meta/SpawnerChargesCalculator.cs:23-41](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SpawnerChargesCalculator.cs#L23)；[ABTests/ABTestService.cs:45-47](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L45)；[ABTests/ABTestService.cs:60](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L60)；[ABTests/SpawnerCooldownsData.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/SpawnerCooldownsData.cs)；[Player/Inventory/RarityStatParam.cs:5-9](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/RarityStatParam.cs#L5)

**状态依据**：No native annotation on GetEffectiveChargesBeforeBigRespawn, ComputeEffectiveChargesBeforeBigRespawn, GetBaseChargesBeforeBigRespawn, CanUseSpawnerCooldownsAb or SpawnerChargesCalculator. RarityStatParam.From (0x146639c) and SimulationSystem.GetSpawnerChargesLimit (0x13e83ec) are annotated, but they are a helper and a consumer.

**配置参数**：`EnemySpawnerConfig.ChargesBeforeBigRespawn`；`EnemySpawnerConfig.Count`；`EnemySpawnerConfig.IsEliteBoss`；`EnemySpawnerConfig.IsVip`；`EnemySpawnerConfig.IsEvent`；`EnemySpawnerConfig.Cooldown`；`EnemySpawnerConfig.ItemReward`；`EnemySpawnerConfig.BigCooldown`；`ABTest SpawnerCooldowns.charges`；`Meta Stats Config.Stats[items_rarity_charges]`

**使用方**：spawners.respawn_tick；spawners.charge_restore；spawners.charge_state_init；spawners.zero_cooldown_collapse；spawners.island_cleared_predicate；spawners.daily_yield；meta.item_drop_copies_per_kill；simulation.spawner_charges_limit (SimulationSystem.GetSpawnerChargesLimit, [Simulation/Systems/SimulationSystem.cs:608-612](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L608))

**备注**：The meta bonus is truncated with an (int) cast (equal to floor for non-negative values). The AB override applies only to stat spawners with a raw Cooldown >= 10 s that are not event or VIP spawners; for them the authored ChargesBeforeBigRespawn is ignored. If charges are disabled (non-AB spawner with ChargesBeforeBigRespawn + bonus = 0, or any event spawner), the spawner respawns every small cooldown with no daily cap. Scene distribution of authored ChargesBeforeBigRespawn: 3 (438), 2 (54), 6 (48), 8 (39), 1 (34), 4 (29), 9 (20), 15 (14), 18 (12), 0 (7).

**依赖图节点**：输入 `value:spawner.uses_ab_cooldowns`、`value:spawner.ab_charges`、`config:EnemySpawnerConfig.ChargesBeforeBigRespawn`、`config:EnemySpawnerConfig.ItemReward`、`config:EnemySpawnerConfig.IsEvent`、`value:item.rarity_param`、`value:player.meta_stat[items_rarity_charges]`；输出 `value:spawner.charges_total`

### `spawners.base_cooldown`

**Effective small (respawn) cooldown**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 合并自：`spawners.base_cooldown`、`meta.spawner_timer`

```text
raw = usesAB ? ABcooldownMinutes[isElite](L) * 60 : config.Cooldown (usesAB = !IsEvent && !IsVip && ItemReward == null && Cooldown >= 10). If raw <= 0 or metaStatsModel == null: cd = raw. Else if ItemReward != null: cd = (float)(raw / max(1.0, items_rarity_timer.GetValue(Rarity + 1))). Else if !IsEvent: cd = max(0, raw - (float)map_spawner_timer_reduce.GetValue(L)). Else cd = raw. BaseCooldown = cd; smallCooldownSec = (int)ceil(cd).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `raw` | base respawn seconds | spawners.ab_cooldown_table (ABTestService.GetSpawnerCooldownSeconds) or EnemySpawnerConfig.Cooldown |
| `items_rarity_timer[Rarity+1]` | timer divisor for item spawners (BaseValue 1; amulet nodes add 2*level-1 flat, e.g. Native_15b650c) | Meta Stats Config items_rarity_timer, Meta/SpawnerTimerCalculator.cs:21-22 |
| `map_spawner_timer_reduce[L]` | flat seconds removed from stat-spawner timers of island L (amulet graph Params 1..14) | Meta Stats Config map_spawner_timer_reduce, Meta/SpawnerTimerCalculator.cs:16-20,23-24 |

**输出**：BaseCooldown (float seconds) and GetSmallCooldownSeconds (int, ceil)

**代码**：[Enemy/EnemySpawner.cs:686-695](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L686)；[Enemy/EnemySpawner.cs:171-177](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L171)；[Enemy/EnemySpawner.cs:717-720](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L717)；[Meta/SpawnerTimerCalculator.cs:9-24](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SpawnerTimerCalculator.cs#L9)；[ABTests/ABTestService.cs:44](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L44)；[ABTests/SpawnerCooldownsData.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/SpawnerCooldownsData.cs)；[Player/Inventory/RarityStatParam.cs:5-9](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/RarityStatParam.cs#L5)

**状态依据**：EnemySpawner.GetBaseCooldown/GetSmallCooldownSeconds and SpawnerTimerCalculator have no native annotation.

**配置参数**：`EnemySpawnerConfig.Cooldown`；`EnemySpawnerConfig.IsEliteBoss`；`EnemySpawnerConfig.IsVip`；`EnemySpawnerConfig.IsEvent`；`EnemySpawnerConfig.LocationNumber`；`EnemySpawnerConfig.ItemReward`；`ABTest SpawnerCooldowns.cooldownMinutes`；`Meta Stats Config.Stats[items_rarity_timer].BaseValue=1`；`Meta Stats Config.Stats[map_spawner_timer_reduce]`

**使用方**：spawners.respawn_tick；spawners.zero_cooldown_collapse；[Simulation/Systems/SimulationSystem.cs:411](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L411) (spawner.BaseCooldown in the RPM fight loop)；[UI/SpawnerView.cs:99](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/SpawnerView.cs#L99) and [Map/Systems/StaticMiniMapPoiSystem.cs:135](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Map/Systems/StaticMiniMapPoiSystem.cs#L135),162 (cooldown display)

**备注**：Item spawners use a divisor (with a floor of 1), so their cooldown never reaches 0. Stat spawners use a flat subtraction, so a large enough map_spawner_timer_reduce drives cd to 0, which triggers spawners.zero_cooldown_collapse. Event spawners are not modified. BigCooldown is not touched by these meta stats. AB defaults: non-elite 2(L+1) min for L1-13 and 30 min otherwise; elite 60/90/120/150 min for L1-4 and 180 min otherwise.

**依赖图节点**：输入 `value:spawner.uses_ab_cooldowns`、`value:spawner.ab_cooldown_seconds`、`config:EnemySpawnerConfig.Cooldown`、`config:EnemySpawnerConfig.ItemReward`、`config:EnemySpawnerConfig.IsEvent`、`config:EnemySpawnerConfig.LocationNumber`、`value:item.rarity_param`、`value:player.meta_stat[items_rarity_timer]`、`value:player.meta_stat[map_spawner_timer_reduce]`、`constant:SpawnerTimerCalculator.min_item_timer_divisor`、`constant:SpawnerTimerCalculator.min_stat_cooldown`；输出 `value:spawner.small_cooldown`

**核验记录**（已写入上方字段）：

- 采纳 `code_refs`（来自 `spawners.base_cooldown`）：Verified that [CodeBase/Meta/SpawnerTimerCalculator.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/CodeBase/Meta/SpawnerTimerCalculator.cs) exists directly under CodeBase. code_refs now use [Meta/SpawnerTimerCalculator.cs:9-24](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SpawnerTimerCalculator.cs#L9) and ABTests/... without the '../' prefix.
- 合并复核修正：(a) used_by still lists 'meta.zero_cooldown_charge_burst'. That id was merged into spawners.zero_cooldown_collapse in this same step, so it is a dangling node in the dependency graph and duplicates the spawners.zero_cooldown_collapse edge already in the list. It also drops the meta member's consumer note 'simulation (spawner.BaseCooldown)', which is now covered only by the SimulationSystem.cs:411 ref; that part is fine. The expression, status and correction_resolutions are verified against EnemySpawner.cs:686-695 and SpawnerTimerCalculator.cs:9-24.

### `spawners.event_crystal_mob_bonus`

**Crystal-event mob extra stat reward**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 合并自：`spawners.event_crystal_mob_bonus`、`meta.crystal_event_mob_stat_reward`

```text
Applies at spawn only if CurrentEvent != null && IsCrystal(CurrentEvent.Key) && IsCrystal(config.EventDef.Key) && EventDef.UnitType == Mob && variant.CurrencyKey in {blue_crystal, red_crystal}. blue: w = event WeaponType, rewardType = (w==Stabbing ? StabDefenceFlat : w==Cutting ? CutDefenceFlat : BuckshotDefenceFlat). red: w = event ResistType, rewardType = (w==Stabbing ? StabDamageFlat : w==Cutting ? CutDamageFlat : BuckshotDamageFlat). If SimulationSystem.Instance != null && RPM[rewardType] exists && RPM[rewardType] > 0: unit.StatRewards += Flat modifier {StatType = rewardType, Value = (float)max(1, RPM[rewardType] * 0.4), Source Enemy, Name 'event.<key>.mob'}. Otherwise nothing is added (there is no floor of 1).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `RPM[type]` | simulation reward per minute for the stat | SimulationSystem.RPMCalculatedValue |
| `variant` | randomly chosen event spawner variant | EventSpawnerDef.GetRandomVariant (EnemySpawner.cs:955-958) |
| `WeaponType / ResistType` | current event attack type and weakness type | EventModel.Data.WeaponType / ResistType |

**输出**：extra Flat StatModifier on unit.StatRewards, later scaled by meta.devour_stat_reward_scaling (stat_drop Param -1)

**代码**：[Enemy/EnemySpawner.cs:980-993](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L980)；[Enemy/EnemySpawner.cs:953-958](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L953)；[Enemy/WeaponType.cs:3-10](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/WeaponType.cs#L3)

**状态依据**：No native annotation in TrySpawnUnit at this block.

**配置参数**：`EventSpawnerDef.Variants[].CurrencyKey`；`EventSpawnerDef.UnitType`；`EventSpawnerDef.Key`

**使用方**：meta.devour_stat_reward_scaling

**备注**：The bonus is worth 0.4 minutes of RPM-equivalent stat per kill. Magic and Universal weapon types fall through to the Buckshot stat. The bonus is added on top of the event-plan StatRewards (meta.event_plan_stat_reward), because Initialize copies config.StatRewards first. It is a devour reward source.

**依赖图节点**：输入 `state:current_event`、`config:LocationsConfig.Locations[].PreferredWeaponType`、`config:LocationsConfig.Locations[].PreferredArmorType`、`config:EnemySpawnerConfig.EventDef.Key`、`config:EnemySpawnerConfig.EventDef.UnitType`、`state:rng`、`value:sim.stat_rpm`、`constant:EnemySpawner.crystal_mob_rpm_minutes`、`constant:EnemySpawner.crystal_mob_min_reward`；输出 `value:enemy.stat_rewards`

**核验记录**（已写入上方字段）：

- 采纳 `expression`（来自 `meta.crystal_event_mob_stat_reward`）：Verified at EnemySpawner.cs:980-990: the crystal event, Mob and currency guards and the rpm > 0.0 guard. The expression now includes all guards and states that nothing is appended when RPM is missing or <= 0.

### `spawners.charge_state_init`

**Charge state initialization and capacity-rise top-up**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 合并自：`spawners.charge_state_init`、`meta.spawner_charge_upgrade_topup`

```text
InitializePersistentState: if IsChargesEnabled && RemainingCharges < 0 (UdValue default -1): RemainingCharges = effCharges - SavedCount; if IsChargesEnabled && KnownMax < 0: KnownMax = effCharges. ProcessSpawn (non-event only): sourceVersion = usesAB ? SpawnerCooldownsVersion : (ItemReward != null ? items_rarity_charges.ModifierVersion : 0); if sourceVersion != lastSourceVersion (initially -1): ApplyChargeUpgradeIfIncreased -> total = effCharges (cache invalidated); if total <= 0 return; if total > KnownMax: RemainingCharges = min(total, RemainingCharges + total - KnownMax) and nextProcessTime = 0; if total != KnownMax: KnownMax = total.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `effCharges / total` | effective charges before the big respawn | spawners.effective_charges |
| `SavedCount` | units alive at the last save | SpawnerDataUd.saved_count |
| `KnownMax` | persisted last known charge cap (default -1) | SpawnerDataUd.known_max_charges |
| `RemainingCharges` | persisted charges left (default -1); the setter writes only while IsChargesEnabled | SpawnerDataUd.remaining_charges, EnemySpawner.cs:280-292 |
| `sourceVersion` | change-detection key for the charge source | EnemySpawner.cs:660-664, 587-592 |

**输出**：RemainingCharges and KnownMaxCharges

**代码**：[Enemy/EnemySpawner.cs:561-567](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L561)；[Enemy/EnemySpawner.cs:569-585](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L569)；[Enemy/EnemySpawner.cs:587-592](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L587)；[Enemy/EnemySpawner.cs:656-665](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L656)；[Enemy/EnemySpawner.cs:254-292](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L254)；[Enemy/EnemySpawner.cs:517-523](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L517)；[Enemy/SpawnerDataUd.cs:14-23](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/SpawnerDataUd.cs#L14)

**状态依据**：No native annotation on InitializePersistentState, ApplyChargeUpgradeIfIncreased, InitializeChargeUpgradeSource or ProcessSpawn.

**配置参数**：`EnemySpawnerConfig.ChargesBeforeBigRespawn`；`Meta Stats Config.Stats[items_rarity_charges]`；`ABTest SpawnerCooldowns.charges`

**使用方**：spawners.respawn_tick；spawners.zero_cooldown_collapse；meta.item_drop_copies_per_kill

**备注**：Buying an items_rarity_charges upgrade (item spawners) or an AB SpawnerCooldowns cohort change (AB-eligible stat spawners) grants the added capacity immediately, capped at the new total, and wakes the spawner (nextProcessTime = 0). Spawners whose source is neither get version 0 and run the check once, on the first ProcessSpawn, since the version starts at -1. A capacity decrease never removes charges already held, but KnownMax is lowered to the new total, so a later rise is measured from the lower value. Event spawners skip the check.

**依赖图节点**：输入 `value:spawner.charges_total`、`value:spawner.remaining_charges`、`value:spawner.uses_ab_cooldowns`、`state:save_counters`、`state:ab_cohort`；输出 `value:spawner.remaining_charges`、`value:spawner.next_process_time`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `spawners.charge_state_init`）：Verified at EnemySpawner.cs:563 (RemainingCharges.Value < 0), 587-592 and 660-664 (version source selection), and SpawnerDataUd.cs:18,22 (default -1). The expression and notes now state the < 0 condition and the three version sources.

### `spawners.ab_spawner_override_predicate`

**Whether AB cooldown/charges override the scene config**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`config_rule`
- 来源：合并核验

```text
CanUseSpawnerCooldownsAb = !config.IsEvent && !config.IsVip && config.ItemReward == null && config.Cooldown >= 10f. If true: base cooldown = ABTestService.GetSpawnerCooldownSeconds(IsEliteBoss, LocationNumber), and effective/base charges = ABTestService.GetSpawnerCharges(IsEliteBoss, LocationNumber, Count). Otherwise: config.Cooldown, and SpawnerChargesCalculator.GetTotalCharges(config, meta) (effective) or config.ChargesBeforeBigRespawn (base).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `IsEvent, IsVip, ItemReward, Cooldown` | spawner config fields (scene values) | Main.unity EnemySpawner.config |
| `IsEliteBoss, LocationNumber, Count` | keys into the AB tables | EnemySpawnerConfig |

**输出**：bool: if true, cooldown and charges come from ABTestService instead of config.Cooldown / config.ChargesBeforeBigRespawn

**代码**：[Enemy/EnemySpawner.cs:717-720](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L717)；[Enemy/EnemySpawner.cs:691-695](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L691)；[Enemy/EnemySpawner.cs:707-710](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L707)；[Enemy/EnemySpawner.cs:712-715](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L712)；[Enemy/EnemySpawner.cs:589](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L589)

**状态依据**：No native VA comment on CanUseSpawnerCooldownsAb or its callers. In EnemySpawner.cs only SetupView (0x1503b50, line 606) and ApplyEventPlanToConfig (0x150639c, line 1143) are annotated.

**配置参数**：`EnemySpawnerConfig.Cooldown`；`EnemySpawnerConfig.IsEvent`；`EnemySpawnerConfig.IsVip`；`EnemySpawnerConfig.ItemReward`；`ABTestData.SpawnerCooldowns (spawner_cooldowns_v3)`

**使用方**：spawners.base_cooldown；spawners.effective_charges；spawners.ab_cooldown_table；spawners.ab_charges_table

**备注**：Scene parse: exactly 569 of 717 spawners meet the predicate. The 148 excluded are 127 item + 10 VIP + 6 event + 8 with Cooldown<10 (fast packs, dummy, crystal, event bosses), minus 3 overlaps (event spawners with Cooldown 5). DisableAfterFirstClear and IsSpecial spawners are NOT excluded by the predicate. For the 569 AB spawners, the scene Cooldown/ChargesBeforeBigRespawn are only legacy or baked reference values. The Cooldown>=10 test reads the scene value, so setting a scene Cooldown below 10 opts a spawner out of AB timing. The flag is also cached at init (usesSpawnerCooldownsAb, line 589).

**依赖图节点**：输入 `config:EnemySpawnerConfig.IsEvent`、`config:EnemySpawnerConfig.IsVip`、`config:EnemySpawnerConfig.ItemReward`、`config:EnemySpawnerConfig.Cooldown`、`constant:EnemySpawner.ab_min_cooldown_seconds`；输出 `value:spawner.uses_ab_cooldowns`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `spawners.ab_spawner_override_predicate`）：A scene re-parse with the code predicate (EnemySpawner.cs:719) gives exactly 569, and 717-569=148 matches the breakdown. The code has no DisableAfterFirstClear/IsSpecial term. The notes now use the exact counts.

### `spawners.ab_cooldown_table`

**AB spawner cooldown by location (spawner_cooldowns_v3)**

- 状态：**纯数据**（`data_only`）
- 类型：`lookup_table`
- 来源：合并核验

```text
abCooldownSec(isElite, L) = 60 * (group.cooldownMinutes.overridesByLocation[L] ?? group.cooldownMinutes.defaultValue), with group = isElite ? elite : nonElite. Default data: nonElite overrides {1:4,2:6,3:8,4:10,5:12,6:14,7:16,8:18,9:20,10:22,11:24,12:26,13:28} = 2(L+1) min, default 30 min. Elite overrides {1:60,2:90,3:120,4:150} = 30(L+1) min, default 180 min.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `L` | config.LocationNumber | EnemySpawnerConfig.LocationNumber |
| `isElite` | config.IsEliteBoss | EnemySpawnerConfig.IsEliteBoss |

**输出**：seconds (before the meta timer reduction in SpawnerTimerCalculator.Apply)

**代码**：[ABTests/ABTestService.cs:44](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L44)；[ABTests/ABTestService.cs:46-47](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L46)；[ABTests/ABTestService.cs:59](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L59)；[ABTests/SpawnerCooldownsData.cs:8-10](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/SpawnerCooldownsData.cs#L8)；[ABTests/ABKeys.cs:27](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABKeys.cs#L27)；[ABTests/ABTestData.cs:25](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestData.cs#L25)

**状态依据**：A hard-coded default data class (SpawnerCooldownsData), which the remote AB group spawner_cooldowns_v3 can replace. The lookup is a trivial dictionary-or-default.

**配置参数**：`ABTestData.SpawnerCooldowns (remote AB spawner_cooldowns_v3).nonElite/elite.cooldownMinutes`

**使用方**：spawners.base_cooldown

**备注**：Defined in ABTests and listed here because it controls spawner timing. Applies only when spawners.ab_spawner_override_predicate holds. Scene values differ. Non-elite scene Cooldown = min(1800, 120*(L+2)) s holds for 533 of the 536 AB-eligible non-elites. The exceptions are one L2 spawner (1800), one L5 spawner (12600, Spawner_elite_regen) and one L8 spawner (1800). So the AB default is 2 minutes shorter for L1-13. Elite scene Cooldown (33 AB-eligible elites) is L1=5400, L2=7200, L3=9000 and L4+=10800, i.e. min(180, 30(L+2)) min. The AB elite default 30(L+1) min is therefore 30 min shorter at L1-4 and equal (180 min) from L5.

**依赖图节点**：输入 `config:ABTestDataConfig.spawnerCooldowns.variants[].data.nonElite.cooldownMinutes.defaultValue`、`config:ABTestDataConfig.spawnerCooldowns.variants[].data.nonElite.cooldownMinutes.overridesByLocation.keyData`、`config:ABTestDataConfig.spawnerCooldowns.variants[].data.nonElite.cooldownMinutes.overridesByLocation.valueData`、`config:ABTestDataConfig.spawnerCooldowns.variants[].data.elite.cooldownMinutes.defaultValue`、`config:ABTestDataConfig.spawnerCooldowns.variants[].data.elite.cooldownMinutes.overridesByLocation.keyData`、`config:ABTestDataConfig.spawnerCooldowns.variants[].data.elite.cooldownMinutes.overridesByLocation.valueData`、`config:EnemySpawnerConfig.IsEliteBoss`、`config:EnemySpawnerConfig.LocationNumber`、`state:ab_cohort`；输出 `value:spawner.ab_cooldown_seconds`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `spawners.ab_cooldown_table`）：The elite per-location cooldowns (5400/7200/9000/10800) and the three non-elite exceptions (L2 1800, L5 12600, L8 1800) were confirmed by a scene re-parse and checked against SpawnerCooldownsData.cs:10. The count was corrected: there are 33 AB-eligible elites, so 536 non-elites, of which 533 (not 563/566) match the formula.
- 合并复核修正：(a) Broken code_refs, inherited from the member. Paths are relative to CodeBase, and the AB files are at CodeBase/ABTests/, so '../ABTests/...' resolves to a directory that does not exist. Verified: [CodeBase/ABTests/ABTestService.cs:44](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/CodeBase/ABTests/ABTestService.cs#L44),46-47,59, SpawnerCooldownsData.cs:8-10, ABKeys.cs:27 and ABTestData.cs:25. The other fields check out: the scene re-parse reproduces 33 elites, 536 non-elites and 533 matches, plus the 3 exceptions.

### `spawners.ab_charges_table`

**AB spawner charges**

- 状态：**纯数据**（`data_only`）
- 类型：`lookup_table`
- 来源：合并核验

```text
abCharges(isElite, L, Count) = (group.charges.overridesByLocation[L] ?? group.charges.defaultValue) * Count, with group = isElite ? elite : nonElite. Default data: 3 (non-elite) or 2 (elite), no overrides.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Count` | max simultaneous units of the spawner | EnemySpawnerConfig.Count |
| `isElite, L` | IsEliteBoss, LocationNumber | EnemySpawnerConfig |

**输出**：charges per big-cooldown cycle (per UTC day for Sync spawners)

**代码**：[ABTests/ABTestService.cs:45](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L45)；[ABTests/ABTestService.cs:46-47](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L46)；[ABTests/ABTestService.cs:60](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L60)；[ABTests/SpawnerCooldownsData.cs:8-10](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/SpawnerCooldownsData.cs#L8)；[Enemy/EnemySpawner.cs:707-715](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L707)

**状态依据**：Hard-coded AB default data (SpawnerCooldownsData), which the remote AB can replace.

**配置参数**：`ABTestData.SpawnerCooldowns.nonElite/elite.charges`

**使用方**：spawners.effective_charges

**备注**：Applies only when spawners.ab_spawner_override_predicate holds. In this case the AB value is used directly as effective charges, and SpawnerChargesCalculator meta bonuses are skipped (EnemySpawner.cs:709). Scene ChargesBeforeBigRespawn for multi-unit packs already equals 3*Count (e.g. Count 6 has 18, Count 20 has 60), and elites all have 2. However, 22 AB-eligible non-elite spawners have scene charges=1 with Count=1: 19 'ice pike blocker' at L10, the L4 dragon, L5 Spawner_elite_regen and L17 Spawner_17_death. All 22 are DisableAfterFirstClear. AB raises them to 3, but this has no effect because they disable after the first clear (EnemySpawner.cs:775-783). Item, VIP and event charges come from the scene.

**依赖图节点**：输入 `config:ABTestDataConfig.spawnerCooldowns.variants[].data.nonElite.charges.defaultValue`、`config:ABTestDataConfig.spawnerCooldowns.variants[].data.nonElite.charges.overridesByLocation.valueData`、`config:ABTestDataConfig.spawnerCooldowns.variants[].data.elite.charges.defaultValue`、`config:ABTestDataConfig.spawnerCooldowns.variants[].data.elite.charges.overridesByLocation.valueData`、`constant:SpawnerCooldownsData.nonElite_charges_default`、`constant:SpawnerCooldownsData.elite_charges_default`、`state:ab_cohort`、`config:EnemySpawnerConfig.Count`、`config:EnemySpawnerConfig.IsEliteBoss`、`config:EnemySpawnerConfig.LocationNumber`；输出 `value:spawner.ab_charges`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `spawners.ab_charges_table`）：A scene re-parse gives exactly 22 non-elite AB-eligible spawners with charges != 3*Count: (L10,Count1,ch1)x19, L4, L5, L17, all DisableAfterFirstClear=1. EnemySpawner.cs:775-783 disables the spawner once it is cleared after the first spawn.
- 合并复核修正：(a) Broken code_refs, inherited from the member. '../ABTests/...' does not resolve relative to CodeBase; the files are at CodeBase/ABTests/. The rest checks out: the scene re-parse reproduces the 22 (Count 1, charges 1, DisableAfterFirstClear) non-elites and all 33 elites at 2/1.

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

**依赖图节点**：输入 `config:EnemySpawnerConfig.Count`、`value:spawner.small_cooldown`、`value:spawner.remaining_charges`、`value:spawner.charges_total`、`value:spawner.spawned_units`、`value:spawner.last_spawn_time`、`value:spawner.next_big_spawn_time`、`value:spawner.spawn_allowed`、`state:utc_now`、`state:sim_bot_state`；输出 `value:spawner.spawned_units`、`value:spawner.last_spawn_time`、`value:spawner.next_process_time`、`value:spawner.remaining_charges`

### `spawners.death_timer_reset`

**Small-cooldown restart on kill (and on devour for event mini-bosses)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：合并核验

```text
HandleDeath (non-crystal): countBefore = units.Count (before removal); units.Remove(enemy). if ((countBefore >= config.Count || units.Count == 0) && !IsEventMiniBossSpawner) { LastSpawnTime = now; Cooldown = GetSmallCooldownSeconds() = (int)ceil(SpawnerTimerCalculator.Apply(baseCooldown, config, meta)) }. Then if (units.Count == 0 && IsChargesEnabled && RemainingCharges <= 0) TryStartBigCooldown(now). HandleDevour: if IsEventMiniBossSpawner { awaitingMiniBossDevour = false; if !IsRosterMiniBossSpawner { LastSpawnTime = now; Cooldown = GetSmallCooldownSeconds() } }
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `countBefore` | units.Count before removing the dead unit | runtime |
| `config.Count` | max simultaneous units | EnemySpawnerConfig.Count |
| `IsEventMiniBossSpawner` | IsEvent && EventDef != null && EventDef.UnitType == MiniBoss | EnemySpawner.cs:373-379 |
| `IsRosterMiniBossSpawner` | event mini-boss with EventDef.Key == 'crystal_event_roster' | EnemySpawner.cs:381-387 |

**输出**：LastSpawnTime / Cooldown (small cooldown), and possibly NextBigSpawnTime

**代码**：[Enemy/EnemySpawner.cs:1173-1232](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1173)；[Enemy/EnemySpawner.cs:1184](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1184)；[Enemy/EnemySpawner.cs:1219-1230](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1219)；[Enemy/EnemySpawner.cs:1359-1372](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1359)；[Enemy/EnemySpawner.cs:686-695](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L686)；[Enemy/EnemySpawner.cs:373-387](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L373)

**状态依据**：No native annotation on HandleDeath, HandleDevour or GetSmallCooldownSeconds.

**配置参数**：`EnemySpawnerConfig.Count`；`EnemySpawnerConfig.Cooldown`；`EnemySpawnerConfig.EventDef`；`ABTestData.SpawnerCooldowns`

**使用方**：spawners.respawn_tick；spawners.big_cooldown

**备注**：For a pack, the timer starts on the first kill from a full pack, and again when the pack is emptied. Kills in between do not reset it. For Count=1 spawners, countBefore >= Count always holds, so every kill restarts the timer. For event mini-boss spawners the restart is skipped on death (awaitingMiniBossDevour is set instead). For non-roster event mini-bosses it happens on DEVOUR (HandleDevour). Roster mini-bosses get no small-cooldown restart there; they are respawned by RosterSpawnNextMiniBossFlowRunner.

**依赖图节点**：输入 `value:unit.is_dead`、`state:player_actions`、`config:EnemySpawnerConfig.Count`、`value:spawner.spawned_units`、`config:EnemySpawnerConfig.IsEvent`、`config:EnemySpawnerConfig.EventDef.UnitType`、`config:EnemySpawnerConfig.EventDef.Key`、`value:spawner.small_cooldown`、`state:utc_now`、`value:spawner.remaining_charges`、`value:spawner.charges_total`、`config:EnemySpawnerConfig.BigCooldown`、`config:EnemySpawnerConfig.SyncBigRespawnToUtc`、`config:EnemySpawnerConfig.Cooldown`；输出 `value:spawner.last_spawn_time`、`value:spawner.next_big_spawn_time`、`value:spawner.next_process_time`、`value:spawner.spawned_units`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `spawners.death_timer_reset`）：Confirmed at EnemySpawner.cs:1223-1227 (death branch excludes IsEventMiniBossSpawner) and 1363-1371 (devour restart for non-roster mini-bosses). The devour path is now in the expression and the Count=1 case is in the notes.

### `spawners.big_cooldown`

**Big cooldown start and UTC alignment**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：合并核验

```text
bigPeriod = (int)ceil(BigCooldownSeconds), BigCooldownSeconds = config.BigCooldown > 0 ? config.BigCooldown : config.Cooldown. TryStartBigCooldown(now): if IsChargesEnabled && RemainingCharges <= 0 && units.Count == 0 && NextBigSpawnTime <= 0 && bigPeriod > 0: NextBigSpawnTime = SyncBigRespawnToUtc ? bigPeriod*floor(now/bigPeriod) + bigPeriod : now + bigPeriod; Cooldown = NextBigSpawnTime - now; LastSpawnTime = 0; nextProcessTime = NextBigSpawnTime. IsInBigCooldown(now): IsChargesEnabled && NextBigSpawnTime > 0 && (Cooldown = NextBigSpawnTime - now) > 0 (Cooldown is assigned only when the first two hold). IsChargesEnabled = !IsEvent && effectiveCharges > 0.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `BigCooldown` | seconds of the big respawn | EnemySpawnerConfig.BigCooldown |
| `Cooldown` | fallback period when BigCooldown<=0. It is the scene config value, never the AB value | EnemySpawnerConfig.Cooldown |
| `SyncBigRespawnToUtc` | align to UTC period boundaries | EnemySpawnerConfig.SyncBigRespawnToUtc |
| `now` | Unix seconds | NowUnixSeconds |

**输出**：NextBigSpawnTime (and Cooldown for UI)

**代码**：[Enemy/EnemySpawner.cs:355-361](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L355)；[Enemy/EnemySpawner.cs:307-313](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L307)；[Enemy/EnemySpawner.cs:1084-1096](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1084)；[Enemy/EnemySpawner.cs:1098-1101](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1098)；[Enemy/EnemySpawner.cs:1047-1054](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1047)；[Enemy/EnemySpawner.cs:1056-1082](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1056)；[Enemy/EnemySpawner.cs:865-882](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L865)；[Enemy/EnemySpawner.cs:1229-1230](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1229)

**状态依据**：No native annotation on TryStartBigCooldown, GetNextUtcAlignedTime, IsInBigCooldown or BigCooldownSeconds.

**配置参数**：`EnemySpawnerConfig.BigCooldown`；`EnemySpawnerConfig.SyncBigRespawnToUtc`；`EnemySpawnerConfig.Cooldown`；`EnemySpawnerConfig.IsEvent`

**使用方**：spawners.charge_restore；spawners.death_timer_reset；UI SpawnerView/minimap (BigCooldownSeconds)

**备注**：Scene parse of all 717 configs gives (BigCooldown, Sync) = {(86400,1):709, (0,0):6, (5,1):1, (86400,0):1}. The 709 Sync spawners reset at 00:00 UTC no matter when the charges ran out. Spawner_Creeper has BigCooldown=86400 with Sync=0, so its 24 h refill starts when its charges run out and is not UTC-aligned. Spawner_Crystal has BigCooldown=5 with Sync=1. The 6 event spawners have 0/0 and IsChargesEnabled is false for them anyway. When BigCooldown<=0, BigCooldownSeconds falls back to config.Cooldown (the scene value, never the AB value). For Sync spawners, RestoreChargesIfReady also refills on a UTC period rollover (RemainingCharges = effectiveCharges - units.Count when floor(now/period) changes), even if no big cooldown was started; see spawners.charge_restore.

**依赖图节点**：输入 `config:EnemySpawnerConfig.BigCooldown`、`config:EnemySpawnerConfig.Cooldown`、`config:EnemySpawnerConfig.SyncBigRespawnToUtc`、`config:EnemySpawnerConfig.IsEvent`、`value:spawner.charges_total`、`value:spawner.remaining_charges`、`value:spawner.spawned_units`、`state:utc_now`；输出 `value:spawner.next_big_spawn_time`、`value:spawner.last_spawn_time`、`value:spawner.next_process_time`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `spawners.big_cooldown`）：A scene re-parse reproduces Counter{(86400,1):709,(0,0):6,(5,1):1,(86400,0):1}. The BigCooldownSeconds fallback to config.Cooldown is confirmed at EnemySpawner.cs:355-361. The variables and notes now reflect it.
- 合并复核修正：(b) The IsInBigCooldown expression leaves out the IsChargesEnabled guard. EnemySpawner.cs:1049 returns false when !IsChargesEnabled || NextBigSpawnTime <= 0, and only then assigns Cooldown. The merged text 'NextBigSpawnTime > 0 && (Cooldown = NextBigSpawnTime - now) > 0' would report big cooldown for spawners with charges disabled (events, zero charges). The rest checks out: the scene Counter matches and the BigCooldownSeconds fallback is confirmed.

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

**依赖图节点**：输入 `config:EnemySpawnerConfig.SyncBigRespawnToUtc`、`config:EnemySpawnerConfig.BigCooldown`、`config:EnemySpawnerConfig.Cooldown`、`value:spawner.charges_total`、`value:spawner.spawned_units`、`value:spawner.next_big_spawn_time`、`state:utc_now`、`state:save_counters`；输出 `value:spawner.remaining_charges`、`value:spawner.next_big_spawn_time`

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

**依赖图节点**：输入 `value:economy.event_progress`、`config:EventConfig.DifficultySettings`、`config:CrystalEventRosterConfig.BossMinutes`、`constant:EnemySpawner.miniboss_progress_offset`、`config:EnemySpawnerConfig.EventDef.Key`、`config:EnemySpawnerConfig.EventDef.UnitType`、`state:current_event`；输出 `value:spawner.spawn_allowed`

### `spawners.event_spawner_init`

**Event spawner initialization**

- 状态：**空壳**（`stub`）
- 类型：`runtime_formula`
- 来源：合并核验

```text
TryInitializeEventSpawner(now) returns false. So in ProcessSpawnTick, for config.IsEvent: Cooldown = 0 and return; the periodic tick/respawn path never runs for event spawners.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `now` | Unix seconds | ProcessSpawnTick |

**输出**：bool

**代码**：[Enemy/EnemySpawner.cs:860-863](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L860)；[Enemy/EnemySpawner.cs:744-750](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L744)；[Enemy/EnemySpawner.cs:640-655](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L640)；[Events/Models/EventsRepository.cs:571](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Events/Models/EventsRepository.cs#L571)；[Events/Models/EventBoosterModel.cs:225](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Events/Models/EventBoosterModel.cs#L225)；[Events/Flows/RosterSpawnNextMiniBossFlowRunner.cs:26](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Events/Flows/RosterSpawnNextMiniBossFlowRunner.cs#L26)

**状态依据**：The body is 'return false' (EnemySpawner.cs:860-863) and has no native annotation. The original must have checked or scheduled the event plan.

**配置参数**：`EnemySpawnerConfig.IsEvent`；`EnemySpawnerConfig.EventDef`

**使用方**：events subsystem

**备注**：Only the periodic tick/respawn path is disabled: ProcessSpawnTick returns early for IsEvent. Event units are still spawned by external callers: EventsRepository.cs:571 and EventBoosterModel.cs:225 via TrySpawnIgnoringMaxCount, and RosterSpawnNextMiniBossFlowRunner.cs:26 via TrySpawn. The original TryInitializeEventSpawner probably handled event-period scheduling or initial spawning.

**依赖图节点**：输入 `config:EnemySpawnerConfig.IsEvent`；输出 `value:spawner.spawn_allowed`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `spawners.event_spawner_init`）：Confirmed by grep: TrySpawnIgnoringMaxCount callers at EventsRepository.cs:571 and EventBoosterModel.cs:225, and TrySpawn at RosterSpawnNextMiniBossFlowRunner.cs:26. The overstated 'event spawning is broken' note is replaced and the code_refs extended.

### `spawners.time_skip`

**Spawner time skip**

- 状态：**空壳**（`stub`）
- 类型：`runtime_formula`
- 来源：合并核验

```text
ApplyTimeSkip(seconds): empty body (presumably LastSpawnTime/NextBigSpawnTime -= seconds in the original)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `seconds` | skipped time | caller (none found in the recovered code) |

**输出**：none

**代码**：[Enemy/EnemySpawner.cs:682-684](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L682)；[Enemy/EnemySpawner.cs:426-432](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L426)

**状态依据**：Empty body on a runtime public method, with no native annotation.

**备注**：No callers of ApplyTimeSkip, ConsumeAllCharges or DefeatAllUnits exist anywhere in the recovered code. The consumer (a time-skip offer, cheat or ability) is either missing too or never existed. ConsumeAllCharges and DefeatAllUnits (EnemySpawner.cs:426-432) also have empty bodies and are probably cheat or debug only.

**依赖图节点**：输入 `state:player_actions`；输出 `value:spawner.last_spawn_time`、`value:spawner.next_big_spawn_time`

**核验记录**（已写入上方字段）：

- 采纳 `used_by`（来自 `spawners.time_skip`）：A grep for ApplyTimeSkip/ConsumeAllCharges/DefeatAllUnits outside EnemySpawner.cs returns nothing. used_by is now empty, and the variable source and notes are updated.

### `spawners.vip_spawner_gate`

**VIP spawner unlock**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`config_rule`
- 来源：合并核验

```text
TrySpawnUnit: if (config.IsVip && playerModel.LastUnlockedLocation.Number < 2) return false. A VIP spawner spawns only once island 2 is unlocked. The reconstruction has no VIP-level check.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `LastUnlockedLocation.Number` | max unlocked island | PlayerModel |
| `RequiredVipLevel` | declared VIP level requirement (3 on all VIP spawners), never read | EnemySpawnerConfig.RequiredVipLevel (EnemySpawnerConfig.cs:94) |

**输出**：bool (spawn allowed)

**代码**：[Enemy/EnemySpawner.cs:924-925](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L924)；[Enemy/EnemySpawnerConfig.cs:94](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawnerConfig.cs#L94)

**状态依据**：No native annotation on TrySpawnUnit.

**配置参数**：`EnemySpawnerConfig.IsVip`；`EnemySpawnerConfig.RequiredVipLevel`

**备注**：RequiredVipLevel is declared but never read in runtime code. All 10 VIP spawners in the scene have RequiredVipLevel=3, which strongly suggests the original also gated on player VIP level >= RequiredVipLevel. That check is probably missing from the reconstruction (unverified against native). VIP spawners are also excluded from AB cooldown/charges (spawners.ab_spawner_override_predicate).

**依赖图节点**：输入 `config:EnemySpawnerConfig.IsVip`、`value:player.unlocked_location`、`constant:EnemySpawner.vip_min_unlocked_location`、`config:EnemySpawnerConfig.RequiredVipLevel`、`state:vip_level`；输出 `value:spawner.spawn_allowed`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `spawners.vip_spawner_gate`）：Scene re-parse gives RequiredVipLevel Counter{'3':10} over IsVip spawners. A grep shows RequiredVipLevel only at its declaration (EnemySpawnerConfig.cs:94). The likely missing check is recorded in the notes and the variables. The expression keeps the code as written.

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

**依赖图节点**：输入 `config:EnemySpawnerConfig.DisableAfterFirstClear`、`value:spawner.spawned_units`、`state:save_counters`、`config:EnemySpawnerConfig.Prefab`、`config:EnemySpawnerConfig.IsEvent`；输出 `value:spawner.spawn_allowed`

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

**依赖图节点**：输入 `value:unit.is_dead`、`config:Gate.NextLocationNumber`；输出 `value:player.unlocked_location`

### `spawners.location_trigger`

**Location number from gate triggers**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x1460a24 (PlayerModel.SetCurrentLocation), 0x1460b1c (PlayerModel.UnlockLocation)`

```text
OnTriggerEnter(other) with a PlayerUnit -> PlayerModel.SetCurrentLocation(n), n = serialized GateLocationTrigger.locationNumber. SetCurrentLocation(n): CurrentLocationNumber = n; if UnlockedLocationNumber <= n then VisitedLastUnlockedLocation = true; if UnlockedLocationNumber < n then UnlockLocation(n) (UnlockedLocationNumber = max(UnlockedLocationNumber, n); if CurrentLocation.Number < n then VisitedLastUnlockedLocation = false). Design intent only, from the editor helper UpdateLocationNumber() that has no runtime caller: n = gate.NextLocationNumber - (isExit ? 1 : 0).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `n` | location number written when the player enters the trigger; serialized per trigger in the scene | GateLocationTrigger.locationNumber (Scenes/Main.unity) |
| `isExit` | trigger on the old side of the gate; used only by the editor helper UpdateLocationNumber | GateLocationTrigger.isExit |
| `gate.NextLocationNumber` | location the gate leads to; used only by the editor helper to derive n | Gate.NextLocationNumber |

**输出**：PlayerModel.CurrentLocationNumber (and UnlockedLocationNumber, VisitedLastUnlockedLocation)

**代码**：[Gates/GateLocationTrigger.cs:17](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Gates/GateLocationTrigger.cs#L17)；[Gates/GateLocationTrigger.cs:20-24](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Gates/GateLocationTrigger.cs#L20)；[Gates/GateLocationTrigger.cs:26-30](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Gates/GateLocationTrigger.cs#L26)；[Player/Models/PlayerModel.cs:830-838](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L830)；[Player/Models/PlayerModel.cs:840-845](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L840)

**状态依据**：GateLocationTrigger.OnTriggerEnter has no native annotation. The callee SetCurrentLocation carries 'Native ELF VA 0x1460a24' and UnlockLocation carries 'Native ELF VA 0x1460b1c'.

**配置参数**：`GateLocationTrigger.locationNumber (serialized per scene trigger)`；`Gate.NextLocationNumber (editor derivation only)`；`GateLocationTrigger.isExit (editor derivation only)`

**使用方**：spawners.island_medal_progress；PlayerModel.CurrentLocation consumers (location-scoped systems)

**备注**：The live value is whatever was serialized in Main.unity. UpdateLocationNumber() is private and has no caller anywhere in the code, so the gate-minus-isExit relation only describes how the serialized values were originally authored. Main.unity has 70 serialized 'locationNumber' fields with values 1..17. RevivePlayerCommandHandler also calls SetCurrentLocation(1). Gate opening calls UnlockLocation(NextLocationNumber) directly (Gates/Gate.cs:65), separately from these triggers.

**依赖图节点**：输入 `config:GateLocationTrigger.locationNumber`、`state:unit_positions`、`value:player.unlocked_location`；输出 `value:player.current_location`、`value:player.unlocked_location`

**核验记录**（已写入上方字段）：

- 采纳 `expression`（来自 `spawners.location_trigger`）：Confirmed in code: OnTriggerEnter (GateLocationTrigger.cs:20-24) passes the serialized locationNumber, and UpdateLocationNumber (26-30) has no caller (grep finds no reference). The expression now uses the serialized field and keeps the gate relation only as editor design intent. It also adds the VisitedLastUnlockedLocation side effects from SetCurrentLocation.

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

**依赖图节点**：输入 `config:EnemySpawnerConfig.PortalGemReward`、`value:unit.is_dead`、`value:player.portal_gems_collected`、`constant:PlayerModel.portal_unlock_gem_threshold`、`config:Portal.stones`；输出 `value:player.portal_gems_collected`、`value:player.boss_tower_portal_unlocked`

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

**依赖图节点**：输入 `config:EnemySpawnerConfig.SlotReward`、`config:PlayerConfig.Slots[].Key`、`config:PlayerConfig.Slots[].UnlockedByDefault`、`value:player.slot_unlocked`、`value:unit.is_dead`；输出 `value:player.slot_unlocked`

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

**依赖图节点**：输入 `config:EnemySpawnerConfig.RewardedPoolIndex`、`config:EnemySpawnerConfig.OptimumTime`、`config:EnemySpawnerConfig.IsStatic`、`value:unit.is_dead`、`value:player.rewarded_pool_index`、`value:player.optimum_time_mark`；输出 `value:player.rewarded_pool_index`、`value:player.optimum_time_mark`

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

**依赖图节点**：输入 `config:EnemySpawnerConfig.LocationNumber`、`config:EnemySpawnerConfig.IsEvent`、`config:EnemySpawnerConfig.IsVip`、`config:EnemySpawnerConfig.IsSpecial`、`config:EnemySpawnerConfig.IsDummy`、`config:EnemySpawnerConfig.IsBossFightPart`、`config:EnemySpawnerConfig.DisableAfterFirstClear`、`config:EnemySpawnerConfig.Prefab`、`value:spawner.charges_total`、`value:spawner.remaining_charges`、`value:spawner.spawned_units`；输出 `value:island.fully_cleared`

### `spawners.island_medal_progress`

**Island medal level and bonus**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：合并核验

```text
Each update: if EnemiesModel.PendingBootstrap.Count > 0, set awaitingBootstrap and return (when it resumes: clear the flag and BumpStateVersion). On the first update of each session (non-persisted flag), SyncMetaStatModifiers for all tracked locations. If the persisted legacy migration has not run: for each tracked L with IsFullyCleared(L), set ClearProcessed[L] = true and grant no medal; mark the migration complete and return. Otherwise evaluate only L = player CurrentLocation.Number, when it changed, when 1 s has passed (CheckInterval), or right after the bootstrap resumes. ProcessLocation(L): if !ClearProcessed && cleared then (if medal < 5: medal = medal + 1, queue a presentation and send analytics; else NotifyMaxLevelClear) and ClearProcessed = true; else if ClearProcessed && !cleared then ClearProcessed = false. SetMedalLevel clamps to [0,5] and replaces the island_medal_bonus modifier 'island_medal_{L}' (Flat, Source=Reward, Param=L, Value = (float)(level * 0.1)); there is no modifier when level = 0. GetMedalLevel = clamp(MedalLevel, 0, 5).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `medal` | island medal level 0..5 (MaxMedalLevel) | IslandMasteryLocationUd.MedalLevel |
| `0.1` | BonusPerMedalLevel | constant IslandMasteryModel.cs:15 |
| `cleared` | IslandMasteryResolver.IsFullyCleared(L) | IslandMasteryResolver over EnemiesModel |
| `ClearProcessed` | latch so that one clear grants at most one medal; reset once the island is no longer fully cleared (after a refill) | IslandMasteryLocationUd.ClearProcessed |
| `L` | location number (only the player's current location is evaluated) | PlayerModel.CurrentLocation.Number |

**输出**：island_medal_bonus[L] in [0, 0.5]

**代码**：[Location/Systems/IslandMasterySystem.cs:12](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Location/Systems/IslandMasterySystem.cs#L12)；[Location/Systems/IslandMasterySystem.cs:29-63](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Location/Systems/IslandMasterySystem.cs#L29)；[Location/Systems/IslandMasterySystem.cs:65-76](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Location/Systems/IslandMasterySystem.cs#L65)；[Location/Systems/IslandMasterySystem.cs:78-105](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Location/Systems/IslandMasterySystem.cs#L78)；[Location/IslandMasteryModel.cs:14-15](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Location/IslandMasteryModel.cs#L14)；[Location/IslandMasteryModel.cs:94-97](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Location/IslandMasteryModel.cs#L94)；[Location/IslandMasteryModel.cs:99-102](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Location/IslandMasteryModel.cs#L99)；[Location/IslandMasteryModel.cs:122-128](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Location/IslandMasteryModel.cs#L122)；[Location/IslandMasteryModel.cs:130-137](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Location/IslandMasteryModel.cs#L130)；[Location/IslandMasteryModel.cs:169-177](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Location/IslandMasteryModel.cs#L169)；[Services/Damage/DamageCalculator.cs:248-267](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L248)

**状态依据**：IslandMasterySystem and IslandMasteryModel have no native annotations. Only the DamageCalculator consumers are native (0x140be68, 0x140c150, eligibility 0x140c264).

**配置参数**：`Meta Stats Config.island_medal_bonus (BaseValue 0)`；`IslandMasteryModel.MaxMedalLevel = 5`；`IslandMasteryModel.BonusPerMedalLevel = 0.1`；`IslandMasterySystem.CheckInterval = 1 s`

**使用方**：[Services/Damage/DamageCalculator.cs:257-267](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L257) (player damage vs island-L enemies *(1+b); enemy damage to player *max(0, 1-b); native VAs 0x140be68/0x140c150)；IslandMasteryModel.GetMedalBonus (IslandMasteryModel.cs:99-102; no caller in the reconstructed code)

**备注**：At most one medal per daily charge reset, so 5 full clears (days) give +50% damage and -50% damage taken on that island. Only the player's CURRENT location is evaluated, once per second or immediately when the location changes (or right after the bootstrap resumes). The one-time persisted legacy migration (IslandMasterySystem.cs:65-76) marks islands that are FULLY cleared at that moment as ClearProcessed without granting a medal, so each of those needs a refill and another full clear before its first medal. Partly cleared islands are not latched and earn a medal on their next full clear. Evaluation pauses while EnemiesModel.PendingBootstrap is non-empty. At max level a clear still sets ClearProcessed and fires NotifyMaxLevelClear. DamageCalculator's eligibility check (0x140c264) differs from the resolver: it excludes Event/Vip/Special/Dummy/BossFightPart, CrystalUnit and quest mobs, but not DisableAfterFirstClear spawners or creepers.

**依赖图节点**：输入 `value:island.fully_cleared`、`value:player.current_location`、`value:island.medal_level`、`constant:IslandMasteryModel.BonusPerMedalLevel`、`constant:IslandMasteryModel.MaxMedalLevel`、`constant:IslandMasterySystem.CheckInterval`、`state:frame_delta_time`、`state:save_counters`；输出 `value:island.medal_level`、`value:player.meta_stat[island_medal_bonus]`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `spawners.island_medal_progress`）：Verified in IslandMasterySystem.cs:29-76: the PendingBootstrap early return (31-35), the legacy migration that sets ClearProcessed without a medal for fully-cleared islands (65-76), and the current-location-only check gated by CheckInterval or a location change (56-62). Added to both notes and expression.
- 合并复核修正：(1) The new migration caveat says 'a save that is mid-clear at migration needs a refill and another clear'. RunLegacyMigration (IslandMasterySystem.cs:65-76) sets ClearProcessed only for islands that are already FULLY cleared. An island that is only partly cleared is not latched and earns its medal normally on its first full clear. The caveat applies to fully-cleared islands, so the wording is wrong. (2) used_by lists 'IslandMasteryModel.GetMedalBonus (UI)', but GetMedalBonus has no caller anywhere in the reconstructed code. Its code_ref (IslandMasteryModel.cs:99-102, inside the member's 94-101 range) was also dropped when the range was narrowed to 94-97. (3) 'On the first run, SyncMetaStatModifiers' uses the same wording as the persisted one-time legacy migration, but the sync runs once per system lifetime (each session, via the non-persisted metaStatSynchronized flag), while the migration runs once per save (Data.LegacyMigrationComplete).

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

**依赖图节点**：输入 `config:QuestConfig.Chains[].Steps[].OptimumTime`、`config:QuestConfig.Chains[].Steps[].IsDays`、`config:EnemySpawnerConfig.OptimumTime`、`config:EnemySpawnerConfig.LocationNumber`、`config:SpawnerOptimumTimeDaysConverter.locationFrom`、`constant:SpawnerOptimumTimeDaysConverter.minutes_per_day`、`constant:SpawnerOptimumTimeDaysConverter.round_scale`；输出 `value:spawner.optimum_time_days`、`config:EnemySpawnerConfig.OptimumTime`、`config:EnemySpawnerConfig.IsDays`

**核验记录**（已写入上方字段）：

- 采纳 `kind`（来自 `spawners.optimum_time_days_converter`）：[Quests/QuestMobSpawnerService.cs:94](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestMobSpawnerService.cs#L94) and :211 call SpawnerOptimumTimeDaysConverter statics; [Enemy/SpawnerOptimumTimeDaysConverter.cs:13-47](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/SpawnerOptimumTimeDaysConverter.cs#L13).

### `spawners.relative_optimum_time`

**RelativeOptimumTime <-> OptimumTime within island (SpawnerRelativeOptimumTimeTool)**

- 状态：**编辑器工具缺失**（`editor_only_missing`）
- 类型：`editor_generator`
- 合并自：`spawners.relative_optimum_time`、`simulation.optimum_time_from_relative`

```text
Per island L with range [Min_L, Max_L] = (IslandOptimumTimeRange[L].MinOptimumTime, MaxOptimumTime), in minutes for L1-3 (IsDays=0) and days for L4-17 (IsDays=1). FillRelativeOptimumTimes: RelativeOptimumTime = round((OptimumTime - Min_L) / (Max_L - Min_L), 2). ApplyOptimumTimesFromRelative: OptimumTime = Min_L + RelativeOptimumTime * (Max_L - Min_L), rounded to whole minutes (L1-3) or 0.1 day (L4+). For L4-17, Min_L = IslandTargetDays[L-2] and Max_L = IslandTargetDays[L-1] exactly, so this equals the simulation form start(L) + rel*(IslandTargetDays[L-1] - start(L)). For L1-3 the ranges are in minutes: Max_L = 60*IslandTargetDays[L-1] (30, 110, 240), and Min_L = 0, 35, 120 (not 60*IslandTargetDays[L-2] = 30 and 110, which leaves gaps 30->35 and 110->120). The tool bodies are empty, so the formula is fitted from scene data.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `L` | spawner island | EnemySpawnerConfig.LocationNumber |
| `Min_L, Max_L` | island optimum-time span. Set by ReadIslandMinMaxSpawners from the OTs of MinSpawner/MaxSpawner, and stored in the scene | Main.unity SpawnerRelativeOptimumTimeTool.Islands (IslandOptimumTimeRange.MinOptimumTime/MaxOptimumTime) |
| `IslandTargetDays` | island schedule in days (99 entries). Entry [i] is the end of island i+1, so [L-2]..[L-1] = Min_L..Max_L for L>=4, and 60*[L-1] = Max_L (minutes) for L1-3 | Combat Balance Config.IslandTargetDays |
| `RelativeOptimumTime` | designer placement within the island (0..1, with some stale values >1 or <0) | EnemySpawnerConfig.RelativeOptimumTime |
| `OptimumTime` | target player time for the spawner (minutes if !IsDays, days if IsDays) | EnemySpawnerConfig.OptimumTime / IsDays |

**输出**：EnemySpawnerConfig.RelativeOptimumTime (from OT) or EnemySpawnerConfig.OptimumTime (from rel)

**代码**：[Enemy/SpawnerRelativeOptimumTimeTool.cs:8-41](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/SpawnerRelativeOptimumTimeTool.cs#L8)；[Enemy/IslandOptimumTimeRange.cs:6-19](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/IslandOptimumTimeRange.cs#L6)；[Simulation/SimulationConfig.cs:219-231](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L219)；[Simulation/SimulationConfig.cs:56-58](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L56)；[Enemy/SpawnerOptimumTimeDaysConverter.cs:13-47](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/SpawnerOptimumTimeDaysConverter.cs#L13)；[Simulation/Systems/SimulationSystem.cs:533-538](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L533)

**状态依据**：Every method of SpawnerRelativeOptimumTimeTool is an empty body (ReadIslandMinMaxSpawners, FetchTimeFromSpawners, FillRelativeOptimumTimes, ApplyOptimumTimesFromRelative, FetchTimeFromSpawnersInternal, MarkSpawnerDirty, MarkSelfDirty), and GetSpawners returns null (SpawnerRelativeOptimumTimeTool.cs:10-41). None has a native annotation. Re-checked against Main.unity: 696 of 717 spawners satisfy rel == round((OT-Min_L)/(Max_L-Min_L), 2) within 0.01. The 21 misses are 7 L2 spawners with rel 0.18 or 0 (OT 60/35/72/40/30), 5 L4 trees with OT=4.4 and rel=0.15 (0.13 expected), L4 Spawner_1_epic_buck (rel 2.85), L6 Spawner_superboss_legendary (rel 1.8), L5 Spawner_elite_regen (OT 9, rel 0.8), L1 Spawner_elite_HP (OT 65, rel 1.33), L9 Spawner_Crystal (OT 40, rel 0.54) and L1 IsDays second_biome_door (OT 120). For IsDays spawners, 604 of 609 satisfy |start(L) + rel*(IslandTargetDays[L-1]-start(L)) - OT| <= 0.05 after 0.1-day rounding. Two L12 spawners (OT 66.6 vs 66.65 and 69.4 vs 69.35) are float rounding ties, not stale. The 5 real outliers are the superboss, Spawner_elite_regen, Spawner_1_epic_buck, Spawner_Crystal and second_biome_door.

**由数据反推的生成规则**（拟合规则，详见 [Generation/IslandTimeline.md](../Generation/IslandTimeline.md)）：

```text
Apply direction (rel is the input): x = Min + RelativeOptimumTime*(Max-Min); OT = Round(x*k)/k with k=10 for IsDays, 1 for minutes; ties to even. Window: islands 4-17 [IslandTargetDays[L-2], IslandTargetDays[L-1]]; islands 1-3 from SpawnerRelativeOptimumTimeTool (Max = 60*IslandTargetDays[L-1], Min 0/35/120).
```

吻合情况：701/717 spawners exact; reverse Fill direction (rel = round((OT-Min)/W,2)) only 656/717; 16 spawners fit neither (named in the report); 19/19 discriminating ties round to even.

**配置参数**：`EnemySpawnerConfig.RelativeOptimumTime`；`EnemySpawnerConfig.OptimumTime`；`EnemySpawnerConfig.IsDays`；`EnemySpawnerConfig.LocationNumber`；`IslandOptimumTimeRange.MinOptimumTime/MaxOptimumTime`；`IslandOptimumTimeRange.MinSpawner/MaxSpawner`；`SpawnerRelativeOptimumTimeTool.Islands`；`Combat Balance Config.IslandTargetDays`；`SimulationConfig.EarlySpawnerOptimumTimeOffsetStartLocation (11)`；`SimulationConfig.EarlySpawnerOptimumTimeOffsetRelativePart (0.5)`；`SimulationConfig.EarlySpawnerOptimumTimeOffsetMultiplier (1)`

**使用方**：simulation.bake_spawner_health；simulation.bake_spawner_damage；simulation.spawner_fight_model (IsCapOptimumTime)；[Simulation/SimulationConfig.cs:219-231](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L219) GetReCalcOptimumTime (bake report at :105): for L>=11 and rel<0.5, OT' = OT - (IslandTargetDays[L-1]-IslandTargetDays[L-2])*(0.5-rel)*1, times 60 if !IsDays

**备注**：Reconciling the two members: spawners.relative_optimum_time used the tool's IslandOptimumTimeRange bounds, while simulation.optimum_time_from_relative used IslandTargetDays with start(1)=0. Scene tool ranges are L1 0-30, L2 35-110, L3 120-240 (minutes) and L4 4-7, L5 7-10, L6 10-14, L7 14-20, L8 20-26, L9 26-34, L10 34-44, L11 44-56, L12 56-71, L13 71-90, L14 90-110, L15 110-130, L16 130-150, L17 150-180 (days). IslandTargetDays has 99 entries, beginning 0.5, 1.8333, 4, 7, 10, 14, 20, 26, 34, 44, 56, 71, 90, 110, 130, 150, 180, 211.67, 245, 280, ... and ending at 8311.667. The tool has entries only for L1-17, the highest LocationNumber in the scene. For L4-17 the two definitions are identical. For L1-3 the simulation member's day form fits no data: only one IsDays spawner sits at L1-3 (second_biome_door), and it is an outlier. The tool's minute ranges fit, with Max_L = 60*IslandTargetDays[L-1]. Unit conflict: the tool ranges (L3 ends at 240 min, L4 starts at 4 days) and GetReCalcOptimumTime (delta*60 for !IsDays) both treat one IslandTargetDays day as 60 minutes. SpawnerOptimumTimeDaysConverter.ConvertOptimumToDays (min/30, native 0x150ced0) and SimulationSystem.GetSpawnerOptimumTimingForCurrentMode (OT/TimeChunkDurationMinutes=30, native 0x13ed83c) treat it as 30 minutes. Keep units explicit when chaining this into the bake. The only live code that uses RelativeOptimumTime is SimulationConfig.GetReCalcOptimumTime, and that uses IslandTargetDays deltas. This is the main lever for retiming content: changing IslandTargetDays or island ranges, then re-running this tool and the bake, moves every spawner's power target.

**依赖图节点**：输入 `config:EnemySpawnerConfig.LocationNumber`、`config:EnemySpawnerConfig.OptimumTime`、`config:EnemySpawnerConfig.RelativeOptimumTime`、`config:EnemySpawnerConfig.IsDays`、`config:SpawnerRelativeOptimumTimeTool.Islands[].MinOptimumTime`、`config:SpawnerRelativeOptimumTimeTool.Islands[].MaxOptimumTime`、`config:SpawnerRelativeOptimumTimeTool.Islands[].MinSpawner`、`config:SpawnerRelativeOptimumTimeTool.Islands[].MaxSpawner`、`config:SpawnerRelativeOptimumTimeTool.Islands[].LocationNumber`、`config:SpawnerRelativeOptimumTimeTool.Islands[].IsDays`、`config:CombatBalanceConfig.IslandTargetDays`、`constant:SpawnerRelativeOptimumTimeTool.rel_round_digits`、`constant:SpawnerRelativeOptimumTimeTool.ot_rounding`、`constant:SpawnerRelativeOptimumTimeTool.minutes_per_target_day`；输出 `config:EnemySpawnerConfig.RelativeOptimumTime`、`config:EnemySpawnerConfig.OptimumTime`

**核验记录**（已写入上方字段）：

- 已包含 `status`（来自 `spawners.relative_optimum_time`）：The critic correction (editor_only_missing; all tool methods empty; live use only in GetReCalcOptimumTime) was already applied=true and is reflected in status, status_evidence and used_by.
- 合并复核修正：(a/b) The notes say IslandTargetDays = [0.5 ... 280] and that it 'covers L18-20'. In fact Combat Balance Config.IslandTargetDays has 99 entries (up to 8311.667), so both statements are wrong. (b) The expression says the L1-3 minute ranges are 'not IslandTargetDays'. But the tool maxes 30/110/240 are exactly 60*IslandTargetDays[L-1] (0.5, 1.8333, 4), and L3 max 240 min equals L4 min 4 days. So the tool and GetReCalcOptimumTime (delta*60 for !IsDays) both treat one IslandTargetDays unit as 60 minutes. The merged notes repeat the critic's '30-minute chunk' unit (ConvertOptimumToDays = min/30, TimeChunkDurationMinutes=30) without flagging this conflict. (a) status_evidence changed the simulation member's verified 604/609 to 602/609. Re-running gives 602 at a strict 0.05 tolerance, but 2 of the 7 misses are float ties at L12 (OT 66.6 vs 66.65 and 69.4 vs 69.35), which the stated 0.1-day rounding produces. So 604 is correct, and those two are not stale. The member's named outliers were also dropped.

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

**依赖图节点**：输入 `config:EnemySpawnerConfig.LocationNumber`、`config:EnemySpawnerConfig.Difficulty`；输出 `config:EnemySpawnerConfig.Difficulty`

**核验记录**（已写入上方字段）：

- 采纳 `status`（来自 `spawners.difficulty_bulk_setter`）：[Enemy/SpawnerDifficultyBulkSetter.cs:18](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/SpawnerDifficultyBulkSetter.cs#L18) has an empty Apply().

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

**依赖图节点**：输入 `config:EnemySpawnerConfig.StatRewards[].Value`、`config:EnemySpawnerConfig.StatRewards[].StatType`、`config:EnemySpawnerConfig.Cooldown`、`config:EnemySpawnerConfig.LocationNumber`、`config:EnemySpawnerConfig.IsStatic`、`config:EnemySpawnersDebugger.considerNonRespawnable`、`config:EnemySpawnersDebugger.considerStatic`、`config:EnemySpawnersDebugger.statsCSVOrder`、`value:spawner.stat_drop_multiplier`；输出 `value:spawner.debug_gpm_report`

**核验记录**（已写入上方字段）：

- 采纳 `status`（来自 `spawners.debugger_gpm_export`）：[Enemy/EnemySpawnersDebugger.cs:25-38](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawnersDebugger.cs#L25).

### `spawners.difficulty_semantics`

**Difficulty / AverageFightTime meaning (bake inputs)**

- 状态：**纯数据**（`data_only`）
- 类型：`config_rule`
- 来源：合并核验
- 原生地址：`0x13db16c (BuildSpawnerBakeReport, consumer); 0x13efd54 (SimulationSystem.GetBattleRegen, consumer)`

```text
Bake (SimulationConfig.BuildSpawnerBakeReport, 0x13db16c). Main entry point: OT' = GetReCalcOptimumTime(spawner) and isDays = Config.IsDays. CalculateBakedSpawnerHealth(spawner, optimumDay) instead passes OT' = optimumDay with isDays = true and no recalculation. day = isDays ? OT' : OT'/60; all player values come from GetSnapshotForBake(OT', isDays). D = Difficulty <= 0 ? 1 : Difficulty. armor = argmax over ArmorConfig.Armors of dps[type] * (1 - Resist) (score uses the unclamped Resist); r = clamp(armor.Resist, -0.9, 0.9). n = min(4, (Count + 1) / 2) (float division). battleRegen = (D + 1) * HealthRegen + (2D + 3) * LifestealFlat. perUnitDps = (battleRegen + D * HP / AFT) / n. neededHit = perUnitDps * (AttackCooldown.BaseValue / (1 - evasion)) / (1 - blockChance * blockAmount). rawDmg = SolveAttackDamageForIncomingDamage(neededHit, defense[Location.PreferredWeaponType], 1 / (1 + abilityEhp(day))). Damage = Math.Round(max(10, rawDmg) / 10) * 10. pDps = (1 + abilityDps(day)) * dps[armor]; Health = Math.Round(((1 - cc) * pDps + cd * cc * pDps) * AFT / Count * (1 - r)). evasion, block chance/amount, cc and cd pass through Stat.CalculateSpecificValue. abilityDps and abilityEhp are CombatBalanceConfig interpolation types 4 and 5. Runtime forecast (SimulationSystem): healthDelta uses GetBattleRegen(raw def.Difficulty) - enemyDPS, with no <= 0 fallback. Boss/quest-mob variant (BossSpawnService.BakeStatsForFloor, run at runtime) uses the same D, n, battleRegen, neededHit and Health structure, but with these differences: armor = the lowest Resist; defense[spawnerConfig.WeaponType]; interpolation by chunk = optimumTime / TimeChunkDurationMinutes; both values *1.05^beyond past the last snapshot chunk; Damage = Round(damage/10)*10 with no floor of 10; both values divided by (1 + tower_boss_weakness) when weakened.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Difficulty (D)` | fraction of player max HP lost (net of regen) over one full-pack fight; <= 0 is treated as 1 in the bake and the boss bake (not in the forecast) | EnemySpawnerConfig.Difficulty (default 1) |
| `AverageFightTime (AFT)` | target seconds to kill the whole pack (Count units) at the expected player DPS | EnemySpawnerConfig.AverageFightTime |
| `Count` | pack size; n = min(4, (Count+1)/2) simultaneous attackers | EnemySpawnerConfig.Count |
| `OT'` | recalculated optimum time (early-spawner offset applied); equals optimumDay on the CalculateBakedSpawnerHealth path | SimulationConfig.GetReCalcOptimumTime / CalculateBakedSpawnerHealth argument |
| `armor.Resist` | resist of the armor the player is best against (bake), or the lowest-resist armor (boss bake) | EnemySpawnerConfig.ArmorConfig.Armors |
| `HP, HealthRegen, LifestealFlat, evasion, blockChance, blockAmount, cc, cd, dps[type], defense[type]` | player snapshot values at OT' | SimulationConfig.GetSnapshotForBake |

**输出**：baked Stats[Health], Stats[Damage]

**代码**：[Simulation/SimulationConfig.cs:104-111](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L104)；[Simulation/SimulationConfig.cs:113-160](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L113)；[Simulation/SimulationConfig.cs:219-231](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L219)；[Services/Damage/DamageCalculator.cs:54-62](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L54)；[Simulation/Systems/SimulationSystem.cs:659-663](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L659)；[Simulation/Systems/SimulationSystem.cs:686-691](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L686)；[Enemy/EnemySpawnerConfig.cs:82](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawnerConfig.cs#L82)；[Enemy/EnemySpawnerConfig.cs:88](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawnerConfig.cs#L88)；[BossTower/BossSpawnService.cs:444-492](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/BossSpawnService.cs#L444)；[Crystals/CrystalObjectiveBalance.cs:25](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Crystals/CrystalObjectiveBalance.cs#L25)

**状态依据**：The Difficulty and AverageFightTime fields are config levers that live EnemySpawner code never reads. The bake formula that consumes them (SimulationConfig.BuildSpawnerBakeReport, comment 'Original ELF0x13db16c') is recovered_native and belongs canonically to the simulation subsystem. BossSpawnService.BakeStatsForFloor reads them at runtime for boss and quest-mob spawns, but that method has no native annotation.

**配置参数**：`EnemySpawnerConfig.Difficulty`；`EnemySpawnerConfig.AverageFightTime`；`EnemySpawnerConfig.Count`；`EnemySpawnerConfig.ArmorConfig`；`EnemySpawnerConfig.Stats[AttackCooldown]`；`LocationsConfig.PreferredWeaponType`；`EnemySpawnerConfig.WeaponType (boss bake defense type)`

**使用方**：Simulation bake；Simulation forecast fight selection；[Crystals/CrystalObjectiveBalance.cs:25](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Crystals/CrystalObjectiveBalance.cs#L25) (via SimulationConfig.CalculateBakedSpawnerHealth, OT' = day with no recalculation)；[BossTower/BossSpawnService.cs:444-492](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/BossSpawnService.cs#L444) BakeStatsForFloor (runtime variant: lowest-Resist armor, defense by spawner WeaponType, chunk interpolation, 1.05^beyond scaling, no damage floor of 10, tower_boss_weakness divisor; resist multiplier 1.9 when Resist < -0.9, i.e. still a ±0.9 clamp)；[Quests/QuestMobSpawnerService.cs:95](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestMobSpawnerService.cs#L95) (BakeStatsForFloor, then * chain.MobAttackMultiplier / MobHealthMultiplier)

**备注**：Scene Difficulty counts (Main.unity): 0.23 (277), 0.3 (171), 0.7 (93), 1 (80), 0.5 (27), 0.6 (20), 0.2 (20), 0.4 (15), 0.8 (8), plus singletons 2, 10, 0.35, 0.26, 0.25, 0.15, 0.12, 0.1 and 0 (0 bakes as 1, but the forecast uses it raw). AverageFightTime: 5 (318), 10 (127), 15 (99), 12 (80), up to 500. For ordinary scene spawners, changing either needs a re-bake: the runtime HP/Damage are the baked Stats, not recomputed. Bosses and quest mobs are re-baked at runtime by BossSpawnService.BakeStatsForFloor, so for them the values take effect live. Math.Round is .NET banker's rounding (ToEven) for both Damage (per 10) and Health. The spawner bake has a damage floor of 10; the boss variant does not.

**依赖图节点**：输入 `config:EnemySpawnerConfig.Difficulty`、`config:EnemySpawnerConfig.AverageFightTime`、`config:EnemySpawnerConfig.Count`、`config:EnemySpawnerConfig.ArmorConfig`、`config:ArmorConfig.Armors[].Resist`、`config:ArmorConfig.Armors[].Type`、`config:EnemySpawnerConfig.Stats[AttackCooldown]`、`config:LocationsConfig.Locations[].PreferredWeaponType`、`config:EnemySpawnerConfig.WeaponType`、`config:SimulationConfig.TimeChunkDurationMinutes`、`value:spawner.bake_optimum_time`、`config:EnemySpawnerConfig.IsDays`、`value:spawner.optimum_time_days`、`config:BossTowerConfig.ArmorBossOptimumTimeOffsetMinutes`、`value:sim.snapshot_at_time`、`value:sim.ability_dps_multiplier`、`value:sim.ability_ehp_multiplier`、`value:enemy.solved_attack_damage`、`constant:SimulationConfig.difficulty_zero_fallback`、`constant:SimulationConfig.max_simultaneous_attackers`、`constant:DamageCalculator.battle_regen_regen_coef`、`constant:DamageCalculator.battle_regen_lifesteal_coef`、`constant:SimulationConfig.resist_clamp`、`constant:SimulationConfig.damage_round_step`、`constant:SimulationConfig.damage_floor`、`constant:SimulationConfig.minutes_per_day`、`constant:BossSpawnService.beyond_snapshot_growth`、`value:player.meta_stat`、`state:boss_tower_context`；输出 `value:sim.bake_new_health`、`value:sim.bake_new_damage`、`config:EnemySpawnerConfig.Stats[Health]`、`config:EnemySpawnerConfig.Stats[Damage]`、`value:enemy.override_base_stats`、`value:sim.target_incoming_damage_per_hit`

**核验记录**（已写入上方字段）：

- 采纳 `expression`（来自 `spawners.difficulty_semantics`）：Matches SimulationConfig.cs:113-160 line by line: day from OT', the D fallback, armor argmax by dps*(1-Resist), r clamped to ±0.9, the float n, battleRegen via DamageCalculator.GetBattleRegen, neededHit, the incoming multiplier 1/(1+abilityEhp), rounding to a multiple of 10 with a floor of 10, and Health including crit. The expression now uses this form.
- 采纳 `status`（来自 `spawners.difficulty_semantics`）：Status kept as data_only for the lever entry. status_evidence and native_va now say the consuming bake formula is recovered_native (0x13db16c) and canonical under simulation. Verified the scene counts 0.8 (8) and 10 (1) and added them to notes.
- 合并复核修正：(1) Every code_ref uses a '../' prefix (e.g. '../[Simulation/SimulationConfig.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs)'), but code paths are relative to CodeBase and these files are at CodeBase/Simulation, CodeBase/Services, CodeBase/Enemy and CodeBase/BossTower, so the refs point outside the tree. (2) used_by says BossSpawnService uses 'the same formula for bosses', which the code contradicts. BossSpawnService.BakeStatsForFloor (444-492) picks the armor with the LOWEST Resist (OrderBy(Resist).First, not argmax dps*(1-Resist)). It takes defense from spawnerConfig.WeaponType, not Location.PreferredWeaponType. It interpolates by time chunk (optimumTime / TimeChunkDurationMinutes), not by day. It has no damage floor of 10. It scales by 1.05^beyond past the last snapshot, and divides by (1 + tower_boss_weakness) when weakened. It also runs at RUNTIME (BossSpawnService.cs:83, BossTowerBossDetailsView, and QuestMobSpawnerService.cs:95 with chain multipliers), so for bosses and quest mobs Difficulty and AverageFightTime are read live, and the note's 'changing either needs a re-bake' does not hold for them. (3) The second entry point into BuildSpawnerBakeReport is missing: CalculateBakedSpawnerHealth(spawner, optimumDay) (SimulationConfig.cs:108-111) passes OT' = optimumDay with isDays = true and no early-spawner recalculation. CrystalObjectiveBalance.cs:25 uses it, so that consumer is also missing from used_by.

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

**依赖图节点**：输入 `config:EnemySpawnerConfig.OptimumTime`、`config:EnemySpawnerConfig.IsDays`、`config:SimulationConfig.TimeChunkDurationMinutes`、`config:SimulationConfig.UseSpawnerCharges`、`config:SimulationConfig.IsCapOptimumTime`、`config:SimulationConfig.EarlySpawnerOptimumTimeOffsetStartLocation`、`config:SimulationConfig.EarlySpawnerOptimumTimeOffsetRelativePart`、`config:SimulationConfig.EarlySpawnerOptimumTimeOffsetMultiplier`、`config:EnemySpawnerConfig.RelativeOptimumTime`、`config:EnemySpawnerConfig.LocationNumber`、`config:CombatBalanceConfig.IslandTargetDays`、`constant:SimulationConfig.early_offset_days_to_minutes_factor`、`value:sim.active_playtime`、`state:sim_bot_state`；输出 `value:spawner.bake_optimum_time`、`value:spawner.optimum_timing_chunks`、`value:sim.spawner_eligible`

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

**依赖图节点**：输入 `config:EnemySpawnerConfig.PriorityMultiplier`、`config:EnemySpawnerConfig.HandBalance`；输出 `value:sim.spawner_priority`

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

**依赖图节点**：输入 `value:spawner.charges_total`、`value:spawner.small_cooldown`、`config:EnemySpawnerConfig.Count`、`config:EnemySpawnerConfig.StatRewards[].Value`、`value:spawner.stat_drop_multiplier`、`config:EnemySpawnerConfig.SoulReward`、`value:player.meta_stat[soul_drop_bonus]`、`value:player.meta_stat[souls_rarity_amount]`、`value:spawner.souls_drop_unlocked`、`config:EnemySpawnerConfig.ItemReward`、`value:spawner.spawned_units`；输出 `value:spawner.daily_stat_yield`、`value:spawner.daily_soul_yield`、`value:spawner.daily_item_yield`、`value:spawner.daily_min_active_time`

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
| `additionalCharges` | extra charges used by multi-copy item drops | meta.item_drop_copies_per_kill (ItemDropAmountCalculator.ResolveKillDropCopies) |
| `SpawnerStatRewardMultiplier` | zero-cooldown collapse multiplier | spawners.zero_cooldown_collapse |
| `crystal_timer_on_kill_reduce` | meta stat seconds removed from the geode timer per kill | Meta Stats Config.crystal_timer_on_kill_reduce |

**输出**：CrystalModel geode timer reduction

**代码**：[Enemy/EnemySpawner.cs:1213](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1213)；[Crystals/Commands/ReduceGeodeTimerOnKillCommandHandler.cs:13-21](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Crystals/Commands/ReduceGeodeTimerOnKillCommandHandler.cs#L13)

**状态依据**：Neither HandleDeath nor the handler carries a native VA comment.

**配置参数**：`Meta Stats Config.crystal_timer_on_kill_reduce`；`EnemySpawnerConfig.ItemReward`

**使用方**：crystals/geode subsystem

**备注**：Spawner kills drive geode timing with weight equal to charges used, so charge and copy meta upgrades also speed up geodes. Every spawner kill counts, including trees and elites.

**依赖图节点**：输入 `value:spawner.extra_charges_consumed`、`value:spawner.kill_reward_multiplier`、`value:player.meta_stat[crystal_timer_on_kill_reduce]`、`config:EnemySpawnerConfig.ItemReward`；输出 `value:economy.geode_timer_reduction`

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

**依赖图节点**：输入 `config:EnemySpawnerConfig.IsDummy`；输出 `value:spawner.spawn_allowed`

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

**依赖图节点**：输入 `config:EnemySpawnerConfig.SyncBigRespawnToUtc`、`config:EnemySpawnerConfig.BigCooldown`、`config:EnemySpawnerConfig.Cooldown`、`config:EnemySpawnerConfig.Count`、`config:EnemySpawnerConfig.IsEvent`、`value:spawner.charges_total`、`value:spawner.remaining_charges`、`value:spawner.next_big_spawn_time`、`value:spawner.spawned_units`、`state:utc_now`、`constant:EnemySpawner.DormantProcessTime`；输出 `value:spawner.next_process_time`

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
| `copies` | item copies dropped this kill | formula meta.item_drop_copies_per_kill / meta.item_drop_copies_per_kill |
| `extra` | additional charges consumed by the multi-copy drop | ItemDropAmountCalculator.ResolveKillDropCopies out param |
| `StatRewards[i].Value` | authored devour reward | EnemySpawnerConfig.StatRewards (Main.unity) |
| `stat_drop_mult` | meta stat_drop multiplier | formula meta.spawner_stat_drop_multiplier |

**输出**：Permanent stat gain and soul amount per devour for item spawners, which scale with copies dropped

**代码**：[Enemy/EnemySpawner.cs:1186](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1186)；[Enemy/EnemySpawner.cs:1194-1215](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1194)；[Enemy/EnemySpawner.cs:1359-1380](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1359)；[Enemy/Commands/GrantSpawnerStatRewardCommandHandler.cs:22-30](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerStatRewardCommandHandler.cs#L22)；[Enemy/Commands/GrantSpawnerSoulRewardCommandHandler.cs:33](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerSoulRewardCommandHandler.cs#L33)

**状态依据**：EnemySpawner.HandleDeath/HandleDevour and the stat reward handler have no native VA annotation. The logic is implemented and consistent with the handlers.

**配置参数**：`EnemySpawnerConfig.ItemReward`；`EnemySpawnerConfig.StatRewards`；`EnemySpawnerConfig.SoulReward`；`EnemySpawnerConfig.ChargesBeforeBigRespawn`；`Meta Stats Config.items_rarity_drop_amount`；`Meta Stats Config.stat_drop`

**使用方**：meta.devour_stat_reward_scaling；meta.soul_drop_per_kill；spawners.daily_yield；simulation.rpm_time_chunk

**备注**：This is a cross-coupling. On an item spawner, raising item drop amount (items_rarity_drop_amount / chest-like upgrades) also multiplies that spawner's permanent stat and soul income. The simulation RPM (CalculateRPMValueTimeChunk) does not model this. It uses reward.Value * charges only, so live yield exceeds simulated yield whenever copies > 1.

**依赖图节点**：输入 `value:spawner.kill_reward_multiplier`、`config:EnemySpawnerConfig.ItemReward`、`value:spawner.charges_total`、`value:reward.item_copies_per_kill`、`value:spawner.extra_charges_consumed`、`value:spawner.remaining_charges`；输出 `value:spawner.kill_reward_multiplier`、`value:spawner.extra_charges_consumed`、`value:spawner.remaining_charges`

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

**依赖图节点**：输入 `config:EnemySpawnerConfig.WanderRadius`、`config:EnemySpawnerConfig.WanderDwellTime`、`config:EnemySpawnerConfig.NonSelectable`、`config:ItemConfig.Number`、`config:SimulationConfig.SnapshotInterval`、`config:SimulationConfig.TimeStep`、`config:SimulationConfig.SimulationSpeed`、`config:SimulationConfig.UseFastTick`、`config:SimulationConfig.FastTickStartChunkOffset`、`config:LocationsConfig.Locations[].PreferredArmorType`；输出 `value:spawner.unconsumed_config`

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
