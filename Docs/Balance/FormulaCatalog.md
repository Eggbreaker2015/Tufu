# 数值公式清单

核心战斗成长相关的全部公式、配置参数及其恢复状态。数据导出见 [Balance/README.md](../../Balance/README.md)。

共 329 条公式，由 7 个子系统梳理 agent 逐一读代码整理，每个子系统再由一个对抗性核验 agent 对照代码检查表达式、状态和行号，最后由完整性审查 agent 补漏。105 条带有核验意见，见各子系统页。

结构化原始数据是 [formulas.json](../../Balance/catalog/formulas.json)，本页及子页由 `Balance/tufu_balance/render_catalog.py` 从它生成；修改请改 JSON 后重新生成。公式细节保留英文原文，以免翻译改动表达式和标识符。

本次范围不含 BossTower、水晶活动、VIP、商店、通行证、矿区和任务奖励。

## 状态说明

| 状态 | 含义 |
|---|---|
| 原生证据恢复（`recovered_native`） | 已实现，且带原生地址注释或明确依据原生代码移植。可信。 |
| 重建（无原生标注）（`reconstructed`） | 已实现，但没有原生标注，行为是推断的。可用，但需实测或原生证据确认。 |
| 空壳（`stub`） | 原版运行时一定有逻辑，这里返回默认值或为空。建模时不能用当前代码，需补证据。 |
| 编辑器工具缺失（`editor_only_missing`） | 原团队的编辑器/烘焙/生成工具，APK 中没有，方法体为空，只剩产出数据。规则需从数据反推。 |
| 纯数据（`data_only`） | 没有公式，是配置里手填或烘焙的数值，本身就是设计杠杆。 |

## 各子系统

| 子系统 | 原生证据恢复 | 重建（无原生标注） | 空壳 | 编辑器工具缺失 | 纯数据 | 合计 |
|---|---|---|---|---|---|---|
| [属性系统](formulas/stats.md) | 16 | 10 | 0 | 0 | 1 | 27 |
| [战斗结算](formulas/combat.md) | 34 | 18 | 5 | 0 | 1 | 58 |
| [装备与进化](formulas/items.md) | 11 | 13 | 2 | 8 | 3 | 37 |
| [永久成长与奖励](formulas/meta.md) | 22 | 21 | 2 | 1 | 3 | 49 |
| [刷怪点、岛屿与门](formulas/spawners.md) | 2 | 36 | 3 | 3 | 6 | 50 |
| [原版模拟与烘焙管线](formulas/simulation.md) | 24 | 8 | 10 | 5 | 1 | 48 |
| [护符、怒气与能力](formulas/powerups.md) | 44 | 9 | 3 | 2 | 0 | 58 |
| [跨系统](formulas/cross.md) | 0 | 0 | 0 | 2 | 0 | 2 |
| **合计** | **153** | **115** | **25** | **21** | **15** | **329** |

## 空壳：建模前必须补证据

| ID | 名称 | 代码 |
|---|---|---|
| [`combat.hammer_regen_stub`](formulas/combat.md) | Hammer regen bonus (graph upgrade) | [Player/Systems/HammerRegenSystem.cs:27-42](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/HammerRegenSystem.cs#L27)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:392-396](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L392) |
| [`combat.hits_taken_decay_stub`](formulas/combat.md) | Hits-taken counter decay | [Player/Systems/PlayerHitsTakenDecaySystem.cs:9](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/PlayerHitsTakenDecaySystem.cs#L9)；[Player/Systems/PlayerHitsTakenDecaySystem.cs:14-20](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/PlayerHitsTakenDecaySystem.cs#L14) |
| [`combat.creeper_explosion_stub`](formulas/combat.md) | Creeper mob explosion damage | [Enemy/Systems/CreeperExplosionSystem.cs:37-55](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/CreeperExplosionSystem.cs#L37)；[Enemy/Components/CreeperMobComponent.cs:10-14](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Components/CreeperMobComponent.cs#L10) |
| [`combat.unit_auto_target_stub`](formulas/combat.md) | UnitAutoTargetSystem (MonoBehaviour) | [Unit/UnitAutoTargetSystem.cs:5-21](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitAutoTargetSystem.cs#L5) |
| [`combat.armor_formulas_stub`](formulas/combat.md) | ArmorFormulas.Get(level) | [Balance/ArmorFormulas.cs:5-8](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/ArmorFormulas.cs#L5) |
| [`balance.armor_formulas_get`](formulas/items.md) | ArmorFormulas.Get(level) | [Balance/ArmorFormulas.cs:5-8](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/ArmorFormulas.cs#L5) |
| [`meta.upgrade_max_capacity_bonus`](formulas/meta.md) | Max-capacity upgrade top-up (geodes/enchant stones/tickets/free chest) | [Meta/Systems/UpgradeMaxCapacityBonusSystem.cs:33-44](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Systems/UpgradeMaxCapacityBonusSystem.cs#L33) |
| [`meta.prepare_event_rewards`](formulas/meta.md) | Prepare event success/failed rewards | [Rewards/Commands/PrepareEventRewardsCommandRunner.cs:16-26](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/Commands/PrepareEventRewardsCommandRunner.cs#L16) |
| [`spawners.event_spawner_init`](formulas/spawners.md) | Event spawner initialization | [Enemy/EnemySpawner.cs:860-863](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L860)；[Enemy/EnemySpawner.cs:746-750](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L746) |
| [`spawners.time_skip`](formulas/spawners.md) | Spawner time skip | [Enemy/EnemySpawner.cs:682-684](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L682) |
| [`spawners.creeper_explosion`](formulas/spawners.md) | Creeper mob explosion damage | [Enemy/Systems/CreeperExplosionSystem.cs:41-55](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/CreeperExplosionSystem.cs#L41)；[Enemy/Components/CreeperMobComponent.cs:8-26](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Components/CreeperMobComponent.cs#L8) |
| [`simulation.snapshot_stat_composition`](formulas/simulation.md) | Simulated player stat composition with feature interpolation | [Simulation/Systems/SimulationSystem.cs:183-191](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L183)；[Simulation/Systems/SimulationSystem.cs:894-906](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L894) |
| [`simulation.player_progression_tick`](formulas/simulation.md) | Bot progression loop (what generated Simulation Data) | [Simulation/Systems/SimulationSystem.cs:291-317](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L291)；[Simulation/Systems/SimulationSystem.cs:497-511](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L497) |
| [`simulation.lifecycle_time_model`](formulas/simulation.md) | Session / AFK time model | [Simulation/Systems/SimulationSystem.cs:264-265](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L264)；[Simulation/Systems/SimulationSystem.cs:556-570](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L556) |
| [`simulation.spawner_priority`](formulas/simulation.md) | Bot spawner priority | [Simulation/Systems/SpawnerData.cs:44-67](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SpawnerData.cs#L44)；[Simulation/Systems/SimulationSystem.cs:697-733](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L697) |
| [`simulation.difficulty_map`](formulas/simulation.md) | Spawner difficulty classification | [Simulation/Systems/SimulationSystem.cs:748-751](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L748)；[Simulation/DifficultyThreshold.cs:7-13](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/DifficultyThreshold.cs#L7) |
| [`powerups.scaled_multiplier_effect`](formulas/powerups.md) | ScaledMultiplierEffect | [Abilities/Effects/ScaledMultiplierEffect.cs:12-14](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/ScaledMultiplierEffect.cs#L12)；[Abilities/Effects/MultiplierTarget.cs](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/MultiplierTarget.cs) |
| [`powerups.hammer_regen`](formulas/powerups.md) | Hammer regen (amulet keystone) | [Player/Systems/HammerRegenSystem.cs:31-42](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/HammerRegenSystem.cs#L31)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:392](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L392) |
| [`simulation.ads_reward_model`](formulas/simulation.md) | Simulated rewarded-ad income in the bot | [Simulation/Systems/SimulationSystem.cs:568](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L568)；[Simulation/Systems/SimulationSystem.cs:256](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L256) |
| [`simulation.stat_benefit_weights`](formulas/simulation.md) | Bot stat / item benefit weights | [Simulation/Systems/SimulationSystem.cs:270-273](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L270)；[Simulation/Systems/SimulationSystem.cs:717](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L717) |
| [`simulation.item_emission_model`](formulas/simulation.md) | Simulated item copy emission and fractional rolls | [Simulation/Systems/SimulationSystem.cs:546](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L546)；[Simulation/Systems/SimulationSystem.cs:551](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L551) |
| [`simulation.bot_fight_resolution`](formulas/simulation.md) | Bot fight resolution, regen and death handling | [Simulation/Systems/SimulationSystem.cs:676](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L676)；[Simulation/Systems/SimulationSystem.cs:693](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L693) |
| [`simulation.spawner_charges_chunk`](formulas/simulation.md) | Simulated spawner charge budget per day chunk | [Simulation/Systems/SimulationSystem.cs:515-520](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L515)；[Simulation/Systems/SimulationSystem.cs:580](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L580) |
| [`powerups.unused_effect_stubs`](formulas/powerups.md) | GrantStatOnHit / TreatAsAllSlotsEmpty / ScaledMultiplier effect stubs | [Abilities/Effects/GrantStatOnHitEffect.cs:16](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/GrantStatOnHitEffect.cs#L16)；[Abilities/Effects/TreatAsAllSlotsEmptyEffect.cs:9](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/TreatAsAllSlotsEmptyEffect.cs#L9) |
| [`items.weapon_attack_timing_stub`](formulas/items.md) | WeaponAttackTiming helper (windup/travel/hit ratios) | [Player/WeaponAttackTiming.cs:5-106](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/WeaponAttackTiming.cs#L5) |

## 编辑器工具缺失：只能从数据反推

| ID | 名称 | 代码 |
|---|---|---|
| [`balance.evolution_island_fill_rule`](formulas/items.md) | Evolution island schedule fill rule | [Balance/CombatBalanceConfig.cs:95-97](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L95)；[Balance/EvolutionIslandFillRuleRow.cs:8-20](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/EvolutionIslandFillRuleRow.cs#L8) |
| [`balance.evolution_target_timings_from_islands`](formulas/items.md) | Evolution target timings from island schedule | [Balance/CombatBalanceConfig.cs:99-101](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L99)；[Balance/CombatBalanceConfig.cs:152-154](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L152) |
| [`balance.interpolation_points_from_islands`](formulas/items.md) | Interpolation timing points from island points | [Balance/BalanceInterpolation.cs:54-56](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/BalanceInterpolation.cs#L54)；[Balance/BalanceInterpolation.cs:66-69](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/BalanceInterpolation.cs#L66) |
| [`balance.evolution_shards_capture`](formulas/items.md) | Evolution shard requirement capture | [Balance/CombatBalanceConfig.cs:13](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L13)；[Balance/CombatBalanceConfig.cs:123-125](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L123) |
| [`balance.evolution_value_autobalance`](formulas/items.md) | Evolution stat value auto-balance | [Balance/CombatBalanceConfig.cs:206-208](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L206)；[Balance/CombatBalanceConfig.cs:217-227](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L217) |
| [`balance.copy_value_rule`](formulas/items.md) | Per-copy value after evolution | [Balance/CombatBalanceConfig.cs:17-19](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L17)；[Balance/CombatBalanceConfig.cs:225-227](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L225) |
| [`balance.mythical_evolution_curve`](formulas/items.md) | Mythical evolution curve | [Balance/CombatBalanceConfig.cs:111-113](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L111)；[Balance/CombatBalanceConfig.cs:262-265](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L262) |
| [`balance.armor_base_reset`](formulas/items.md) | Armor base / item cost reset tools | [Balance/CombatBalanceConfig.cs:107-109](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L107)；[Balance/CombatBalanceConfig.cs:185-187](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L185) |
| [`meta.metastats_fill`](formulas/meta.md) | MetaStatsConfig.Fill editor generator | [Meta/MetaStatsConfig.cs:11-13](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStatsConfig.cs#L11) |
| [`spawners.relative_optimum_time`](formulas/spawners.md) | RelativeOptimumTime <-> OptimumTime within island | [Enemy/SpawnerRelativeOptimumTimeTool.cs:10-41](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/SpawnerRelativeOptimumTimeTool.cs#L10)；[Enemy/IslandOptimumTimeRange.cs:6-19](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/IslandOptimumTimeRange.cs#L6) |
| [`spawners.difficulty_bulk_setter`](formulas/spawners.md) | Bulk Difficulty setter | [Enemy/SpawnerDifficultyBulkSetter.cs:5-22](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/SpawnerDifficultyBulkSetter.cs#L5) |
| [`spawners.debugger_gpm_export`](formulas/spawners.md) | Spawner debugger GPM / CSV export | [Enemy/EnemySpawnersDebugger.cs:7-41](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawnersDebugger.cs#L7)；[Enemy/EnemySpawnerDebugInfo.cs](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawnerDebugInfo.cs) |
| [`simulation.interp_island_timing_points`](formulas/simulation.md) | Interpolation island -> day timing conversion (ApplyIslandTimings) | [Balance/BalanceInterpolation.cs:54-56](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/BalanceInterpolation.cs#L54)；[Balance/CombatBalanceConfig.cs:131-133](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L131) |
| [`simulation.snapshot_trim`](formulas/simulation.md) | Trimming TimeChunkSnapshots (TrimSimulationData) | [Simulation/SimulationConfig.cs:98-101](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L98)；[Simulation/SimulationConfig.cs:55](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L55) |
| [`simulation.optimum_time_from_relative`](formulas/simulation.md) | Spawner OptimumTime from RelativeOptimumTime | [Enemy/SpawnerRelativeOptimumTimeTool.cs:10-24](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/SpawnerRelativeOptimumTimeTool.cs#L10) |
| [`simulation.bake_apply`](formulas/simulation.md) | Rebake / apply balance to scene spawners | [Simulation/SimulationConfig.cs:79-80](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L79)；[Simulation/SimulationConfig.cs:75](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L75) |
| [`simulation.amulet_sim_multipliers`](formulas/simulation.md) | Amulet soul emission & spawner-charge multipliers in simulation | [Simulation/Systems/AmuletSimulationMultipliers.cs:23-44](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/AmuletSimulationMultipliers.cs#L23)；[Simulation/Systems/SimulationSystem.cs:600-606](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L600) |
| [`powerups.amulet_autobalance_bake`](formulas/powerups.md) | Amulet auto-balance bake (cost/stat multipliers) | [Amulet/AmuletConfig.cs:11](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletConfig.cs#L11)；[Amulet/AmuletConfig.cs:53-55](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletConfig.cs#L53) |
| [`balance.shield_value_sync_by_leader`](formulas/cross.md) | Shield armor values synced to a leader item (editor) | [Balance/CombatBalanceConfig.cs:80](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L80) |
| [`balance.evolution_autobalance_thresholds`](formulas/cross.md) | Evolution auto-balance guard multipliers (unconsumed levers) | [Balance/CombatBalanceConfig.cs:17-23](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L17)；[Balance/CombatBalanceConfig.cs:217-226](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L217) |
| [`powerups.amulet_level_target_days`](formulas/powerups.md) | Amulet level target days and auto-balance multipliers (unconsumed) | [Amulet/AmuletConfig.cs:17-31](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletConfig.cs#L17)；[Amulet/AmuletConfig.cs:53](../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletConfig.cs#L53) |

## 重叠条目

各子系统并行梳理，同一段代码可能在多个子系统下各有一条。建依赖图前需要合并成一个节点。下表按首个代码引用的文件分组，只列跨子系统的文件。

| 文件 | 条目 |
|---|---|
| `Unit/StatBase.cs` | `stats.raw_value_aggregation`、`stats.modifier_merge`、`combat.stat_value_aggregation`、`combat.stat_modifier_merge`、`meta.metastat_value_unfiltered`、`meta.modifier_merge_accumulation` |
| `Unit/Stat.cs` | `stats.specific_value_pipeline`、`stats.specific_curve.movement_speed`、`stats.specific_curve.evasion`、`stats.specific_curve.crit_chance`、`stats.specific_curve.crit_damage`、`stats.specific_curve.super_crit_damage`、`stats.specific_curve.global_attack_defence`、`stats.specific_curve.block`、`stats.specific_cap`、`stats.specific_identity`、`combat.stat_specific_value_curves` |
| `Player/Models/PlayerModel.cs` | `stats.modifier_source_persistence`、`stats.player_base_stats`、`items.copy_merge`、`items.weapon_set_synergy_counts`、`items.unlocked_slot_caps`、`items.equip_stat_modifiers`、`items.auto_equip_and_starting_weapon` |
| `Unit/Systems/UnitBuffSystem.cs` | `stats.temporary_modifier_decay`、`stats.stun_decay`、`combat.temporary_modifier_expiry`、`combat.stun_duration` |
| `Unit/UnitCore.cs` | `stats.stat_accessors`、`combat.death_threshold` |
| `Unit/Systems/UnitDamageSystem.cs` | `stats.lifesteal_player`、`stats.lifesteal_enemy`、`stats.thorns_reflection`、`stats.enemy_reflection`、`stats.first_hit_vulnerability`、`stats.damage_application`、`stats.lethal_ignore`、`combat.player_lifesteal_on_hit`、`combat.enemy_lifesteal_on_hit`、`combat.thorns_reflection`、`combat.enemy_reflection`、`combat.lethal_damage_ignore`、`combat.block_to_supercrit_bonus`、`combat.first_vulnerability_multiplier` |
| `Unit/UnitMovement.cs` | `stats.player_movement_displacement`、`combat.player_movement` |
| `Services/Damage/DamageCalculator.cs` | `combat.flat_defense_curve`、`combat.solve_attack_for_incoming`、`combat.battle_regen`、`combat.player_flat_defense`、`combat.event_enemy_damage_ramp`、`combat.incoming_damage_to_player`、`combat.outgoing_damage_to_enemy`、`combat.outgoing_damage_simulation_mode`、`combat.crit_and_supercrit`、`combat.global_attack_multiplier`、`combat.global_defence_multiplier`、`combat.island_mastery_multipliers`、`combat.event_crystal_damage_multipliers`、`combat.event_golem_damage_multiplier`、`combat.boss_tower_damage_multipliers`、`combat.rage_damage_reduction`、`combat.weapon_type_defence_mapping`、`powerups.rage_damage_reduce` |
| `Utilities/DoubleExtensions.cs` | `combat.dodge_block_rolls`、`meta.pretty_round` |
| `Enemy/EnemySpawner.cs` | `combat.event_enemy_armor_override`、`combat.enemy_spawn_hp_damage_ab_multiplier`、`meta.zero_cooldown_charge_burst`、`meta.crystal_event_mob_stat_reward`、`meta.event_plan_stat_reward`、`meta.spawner_charge_upgrade_topup`、`spawners.ab_spawner_override_predicate`、`spawners.base_cooldown`、`spawners.effective_charges`、`spawners.charge_state_init`、`spawners.respawn_tick`、`spawners.death_timer_reset`、`spawners.big_cooldown`、`spawners.charge_restore`、`spawners.zero_cooldown_collapse`、`spawners.item_drop_copies`、`spawners.enemy_spawn_stats`、`spawners.event_plan_override`、`spawners.event_crystal_mob_bonus`、`spawners.event_miniboss_spawn_gate`、`spawners.event_spawner_init`、`spawners.time_skip`、`spawners.vip_spawner_gate`、`spawners.disable_after_first_clear`、`spawners.progress_markers`、`spawners.daily_yield`、`spawners.geode_kill_count`、`spawners.dummy_spawn_block`、`spawners.full_or_depleted_wake_schedule`、`spawners.item_copies_reward_multiplier_coupling` |
| `Enemy/EnemyAttackSystem.cs` | `combat.enemy_attack_cycle`、`combat.enemy_leash_and_aggro`、`spawners.enemy_attack_cadence`、`spawners.enemy_leash_heal` |
| `Enemy/Systems/EnemyRegenSystem.cs` | `combat.enemy_regen`、`spawners.enemy_regen` |
| `Player/Systems/RageExtraHealthSystem.cs` | `combat.rage_extra_health`、`powerups.rage_extra_health` |
| `Player/Systems/RageExtraHitSystem.cs` | `combat.rage_extra_hit`、`powerups.rage_extra_hit` |
| `Player/Systems/HammerRegenSystem.cs` | `combat.hammer_regen_stub`、`powerups.hammer_regen` |
| `Enemy/Systems/CreeperExplosionSystem.cs` | `combat.creeper_explosion_stub`、`spawners.creeper_explosion` |
| `Balance/ArmorFormulas.cs` | `combat.armor_formulas_stub`、`balance.armor_formulas_get` |
| `Enemy/Systems/EnemyVisibilitySystem.cs` | `combat.enemy_far_activation`、`spawners.enemy_far_culling` |
| `Player/Inventory/ItemModel.cs` | `items.weapon_damage_pre_evolution`、`items.weapon_damage_evolved`、`items.armor_pre_evolution`、`items.armor_evolved`、`items.effective_level`、`items.evolution_cost`、`items.next_level_preview`、`items.stat_per_copy`、`items.weapon_dps`、`items.ability_unlock_gate`、`items.display_rarity`、`powerups.ability_unlock_gate` |
| `Balance/CombatBalanceConfig.cs` | `items.evolution_level_clamp`、`balance.island_target_days_curve`、`balance.evolution_island_fill_rule`、`balance.evolution_target_timings_from_islands`、`balance.evolution_shards_capture`、`balance.evolution_value_autobalance`、`balance.copy_value_rule`、`balance.mythical_evolution_curve`、`balance.armor_base_reset`、`items.base_growth_tables`、`balance.shield_value_sync_by_leader`、`balance.evolution_autobalance_thresholds` |
| `Balance/BalanceInterpolation.cs` | `balance.interpolation_evaluate`、`balance.interpolation_points_from_islands`、`simulation.interp_evaluate`、`simulation.interp_island_timing_points` |
| `Balance/BalanceInterpolationsConfig.cs` | `balance.total_coefficient`、`simulation.interp_total_coefficient` |
| `Simulation/Forecast/ForecastSnapshots.cs` | `balance.island_timing_to_days`、`simulation.island_timing_to_days`、`simulation.forecast_build_core`、`simulation.forecast_auto_balanced_enemy`、`simulation.forecast_damage_ramp_average`、`simulation.forecast_tune_boss_presets`、`simulation.forecast_reward`、`simulation.forecast_event_plan`、`simulation.forecast_current_player_fallback` |
| `Enemy/Commands/GrantSpawnerStatRewardCommandHandler.cs` | `meta.devour_stat_reward_scaling`、`spawners.stat_reward_grant` |
| `Enemy/Commands/GrantSpawnerSoulRewardCommandHandler.cs` | `meta.soul_grant_on_devour`、`spawners.soul_reward_grant` |
| `Amulet/Upgrades/AmuletUpgradesModel.cs` | `meta.soul_drop_unlock_gate`、`powerups.amulet_node_availability`、`powerups.amulet_souls_drop_unlock` |
| `Enemy/Commands/GrantSpawnerCurrencyRewardCommandHandler.cs` | `meta.spawner_currency_reward`、`spawners.currency_reward` |
| `Integrations/Ads/AdsInitialize.cs` | `meta.rewarded_ad_stat_value`、`meta.rewarded_dynamic_weights`、`powerups.rewarded_ad_stat_reward` |
| `ABTests/ABTestService.cs` | `spawners.ab_cooldown_table`、`spawners.ab_charges_table`、`simulation.rpm_stat_class_ab_multiplier` |
| `Enemy/SpawnerRelativeOptimumTimeTool.cs` | `spawners.relative_optimum_time`、`simulation.optimum_time_from_relative` |
| `Simulation/SimulationConfig.cs` | `spawners.difficulty_semantics`、`spawners.optimum_time_semantics`、`simulation.snapshot_trim`、`simulation.bake_recalc_optimum_time`、`simulation.bake_spawner_health`、`simulation.bake_spawner_damage`、`simulation.bake_apply` |

## 完整性审查结论

The catalog covers the core combat and growth loop well. The main gaps are cross-couplings and simulation and editor stubs, not missing combat math.

**Key missed couplings**
1. Item-spawner copies overwrite the per-kill multiplier. Item drop amount therefore also scales devour stat and soul income (EnemySpawner.cs:1215, GrantSpawnerStatRewardCommandHandler.cs:29, GrantSpawnerSoulRewardCommandHandler.cs:33). The simulated RPM does not model this.
2. Weapon cadence modifiers sit outside the attack-cycle entries: the 1 s equip warm-up, the weapon_1 base-range gate, and the ability cooldown reset (cooldown = castPoint*cd + 0.15).
3. Meta mark synergies (evasion_mark, mark_lifesteal, the hard-coded 10% piercing mark chance) and state-dependent effects (missing-health, empty-slot and stat-proportional damage, temporary buffs) cannot be represented in the expected-value simulation or bake. They enter the design only through the PlayerAbilityDPSMult and EHPMult interpolation curves.

**Status audit (correct labels before export)**
These files carry no native VA comments, so their formulas should be `reconstructed`, not `recovered_native`, unless other evidence exists:
- Unit/Stat.cs (all specific-value curves, cap 0.8) and Unit/StatBase.cs (raw aggregation).
- Runtime EnemySpawner logic (only SetupView is annotated).
- Meta/SpawnerChargesCalculator.cs, SpawnerTimerCalculator.cs, ItemDropAmountCalculator.cs.
- CombatBalanceConfig getters (GetDamageBase, GetEvolution*, GetEvolutionShardsRequirement).
- ABTestService, EnemyAttackSystem, EnemyRegenSystem.
- The DamageCalculator helpers ApplyDamageDefense, SolveAttackDamage, GetBattleRegen and GetGlobalAttack/DefenceMultiplier. Only Calculate and the event and mastery multipliers are annotated.

Other status issues:
- The SpawnerRelativeOptimumTimeTool, SpawnerDifficultyBulkSetter and EnemySpawnersDebugger bodies are empty, so they are `editor_only_missing`.
- ArmorFormulas.Get has no callers, so it has zero model impact.

**Duplicates to merge (same code under several ids)**
- stats.raw_value_aggregation / combat.stat_value_aggregation
- stats.modifier_merge / combat.stat_modifier_merge
- stats.specific_curve.* / combat.stat_specific_value_curves
- Lifesteal, thorns, enemy reflection and first-vulnerability (stats.* vs combat.*); thorns appears a third time as powerups.thorns
- Lethal ignore (three ids)
- Rage reduction, health and extra-hit (combat.* vs powerups.*)
- Hammer regen, creeper explosion and enemy regen (combat vs spawners)
- Interpolation evaluate, total coefficient and island-timing-to-days (balance.* vs simulation.*)
- Zero-cooldown collapse, item copies, stat, soul and currency grant (spawners.* vs meta.*)
- Enemy cadence, leash and damage ramp (spawners vs combat)
- Rewarded-ad stat (meta vs powerups)

**Modeling concerns for the extractor**
1. Export ABTest Data Config.asset with all variants and location overrides. Live enemy HP and damage, spawner cooldowns, charges (x unit Count) and the RPM stat-class multipliers come from it, overriding scene values whenever Cooldown >= 10 and the spawner is not item, VIP or event.
2. "Days" in OptimumTime, IsDays and snapshots are 30-minute playtime chunks (TimeChunkDurationMinutes = 30, ConvertOptimumToDays = min/30). Simulated charge resets use 1440 minutes and live charge refill is UTC-aligned BigCooldown. Keep the units explicit.
3. Bake and simulation use the location's PreferredWeaponType for incoming-damage defence and Rare-rarity interpolation, but live combat uses each spawner's WeaponType. Report the bake-vs-live delta per spawner.
4. In the incoming hit, typed percent defence multiplies flat defence, while universal percent reduces damage directly.
5. GetDamageBase and GetDamageGrowth map Magic and Universal to the Pierce arrays. Armor base and growth ignore damage type. All DamageBaseByRarity arrays are 0, so pre-evolution damage = growth*copies + FlatDamage.
6. Simulation DPS divides off-type weapons by 10 and fills only UnlockedWeaponSlots/UnlockedArmorSlots. Typed defence excludes Universal armor, while live incoming damage includes it.
7. RPM values that feed rewards are recomputed at runtime only from Shop and Forecast code. EnemySpawner.rpmCalculatedOnLoad is never set, so the core-scope trigger is missing. Model RPM as a pure function of the current state.
8. The bot's core (stat benefit weights, item emission, fight resolution, ads, charge-chunk reset, interpolation stat modifier) is stubbed. Treat Simulation Data and TimeChunkSnapshots as ground-truth inputs rather than something to regenerate.
9. An asset scan shows GrantStatOnHit, TreatAsAllSlotsEmpty, ScaledMultiplier and EmptySlotsCondition are unused, so their stubs are harmless today.
10. EnemySoulSystem.OnUpdate is empty (1 s check interval, amulet refs). It is probably a soul-indicator visual, but verify it does not gate soul drops.

All paths are relative to /Users/joyfort/MyProjects/Tufu/Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase.
