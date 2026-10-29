# 原版模拟与烘焙管线（`simulation`）

[返回公式清单总览](../FormulaCatalog.md)

## 概述

The simulation subsystem is the original team's offline balancing pipeline. A bot (SimulationSystem) plays the map greedily with a time model of 10 minutes of play followed by 230 minutes AFK, then one chunk per day from day 4. It writes the simulated player's power (stats, DPS per weapon type, defense per damage type, items, and RPM, the rewards per minute for each stat) to Simulation Data as 63 snapshots indexed by play minutes and 46 snapshots indexed by day. Those snapshots were trimmed to roughly 10% geometric spacing. Every spawner's Health and Damage is then baked from the snapshot at its OptimumTime. OptimumTime is set as island start day + RelativeOptimumTime × island duration. Health is chosen so the player's crit- and ability-scaled DPS against the enemy's weakest armor kills the group in AverageFightTime. Damage is chosen so the group takes Difficulty × player HP (plus battle regen) over AverageFightTime, after inverting the exp/linear defense curve, with evasion, block and the ability EHP curve applied. The BalanceInterpolation curves are configured by island and converted to days. They scale everything as time-dependent multipliers: type 2 is an additive flat bonus (+3.0 by day 405), type 3 is a multiplicative bonus (×4.9 by day 405), and types 4/5 are the ability DPS/EHP multipliers. At runtime the same machinery computes RPM, which scales stat, shop and event rewards. The Forecast module re-uses the baked snapshots to auto-balance crystal-event golems and bosses. I re-ran the recovered bake formula against the shipped Simulation Data and it reproduces the shipped scene spawner HP within 5% for 552 of 706 spawners. 79 match exactly, 79 of the 102 minute-mode spawners. The SpawnerBakeInfos log in Simulation Config is an older, stale bake.

**核验结论**：The catalog is accurate overall. I checked every expression against the code: Evaluate/GetTotalCoefficient, the fight model, the old and new RPM, soul RPM, DpsData/DefenseData, fast and slow spawner DPS, BuildSpawnerBakeReport (HP and damage), GetReCalcOptimumTime, snapshot interpolation and nearest lookup, and the forecast BuildCore/BuildAutoBalancedEnemies/ramp/reward/crystal mapping. Constants, clamps, rounding and status labels are correct. The native VA comments are present wherever the catalog says recovered_native, and the stub and editor_only bodies really are empty. Code_ref line numbers are correct, except DamageCalculator 77-78, which should be 78-79.

Data claims also check out: 46 trimmed chunk indices that fit 10% geometric spacing, TimeChunkIndex = floor(Total/1440), K2 = 3.0 and K3 = 4.9075 at day 405, K2 = 0.20397 at day 3, the island→timing conversion of the curves, 604/609 RelativeOptimumTime fits, 5/712 stale SpawnerBakeInfos rows, and HandBalance on 2 spawners. My independent re-bake gives HP exact 79 (all minute-mode), within 1% 246, within 5% 555 of 716; damage exact 124 and within 5% 511. That matches the reported 79/246/552/118/504.

Corrections:
- The attacker factor min(4,(Count+1)/2) uses float division, so Count=2 gives 1.5.
- The bot's cooldown filter applies only while IsRunning, and runtime always uses GPM-bot mode.
- The ChargesTotal derivation was incomplete: it is AB charges×Count, or ChargesBeforeBigRespawn plus the meta items_rarity_charges bonus; DisableAfterFirstClear spawners are never used at runtime.
- Snapshot feature fields are K2/K3 only for MainInterpolationStats.
- Minute snapshots are about 1.1 min apart in the first cycle, not 5 min.
- The open-question claim that 9 of 53 on-chunk day-mode spawners match is wrong. 0 of 54 match HP and 0 of 608 day-mode spawners match exactly. Trimming therefore cannot explain the drift; the day-mode scene values come from a different simulation run.
- The gamma-curve guess for EventBossMinutes contradicts the shipped data.
- The slow spawner-DPS path uses the spawner's own WeaponType.

I added two missed entries: the spawner charges limit (a recovered_native RPM multiplier) and the local forecast fallback snapshot. No formulas rejected.

## 公式索引

| ID | 名称 | 状态 | 类型 |
|---|---|---|---|
| [`simulation.balance_timing`](#simulationbalance_timing) | Simulation balance timing | 原生证据恢复 | `runtime_formula` |
| [`simulation.snapshot_stat_composition`](#simulationsnapshot_stat_composition) | Simulated player stat composition with feature interpolation | 空壳 | `simulation` |
| [`simulation.dps_data`](#simulationdps_data) | Simulated DPS per weapon type (DpsData) | 原生证据恢复 | `runtime_formula` |
| [`simulation.defense_data`](#simulationdefense_data) | Simulated defense per damage type (DefenseData) | 原生证据恢复 | `runtime_formula` |
| [`simulation.combat_multiplier_cache`](#simulationcombat_multiplier_cache) | Player defensive/offensive specific-value multipliers | 原生证据恢复 | `runtime_formula` |
| [`simulation.ability_multipliers`](#simulationability_multipliers) | Player ability DPS / EHP multipliers | 原生证据恢复 | `runtime_formula` |
| [`simulation.enemy_multiplier`](#simulationenemy_multiplier) | Enemy HP/Damage global multipliers in simulation | 原生证据恢复 | `runtime_formula` |
| [`simulation.dps_against_spawner`](#simulationdps_against_spawner) | Player DPS against a spawner | 原生证据恢复 | `runtime_formula` |
| [`simulation.spawner_dps`](#simulationspawner_dps) | Spawner DPS against the player | 原生证据恢复 | `runtime_formula` |
| [`simulation.spawner_fight_model`](#simulationspawner_fight_model) | Per-spawner fight/farm model (SpawnerData) | 原生证据恢复 | `runtime_formula` |
| [`simulation.spawner_reward_value`](#simulationspawner_reward_value) | Spawner reward value for bot priority | 原生证据恢复 | `runtime_formula` |
| [`simulation.rpm_time_chunk`](#simulationrpm_time_chunk) | Stat reward-per-minute (RPM/GPM), current algorithm | 原生证据恢复 | `runtime_formula` |
| [`simulation.rpm_old`](#simulationrpm_old) | Stat RPM, old greedy-1000s algorithm | 原生证据恢复 | `runtime_formula` |
| [`simulation.soul_rpm`](#simulationsoul_rpm) | Soul reward-per-minute | 原生证据恢复 | `runtime_formula` |
| [`simulation.spawner_optimum_timing_mode`](#simulationspawner_optimum_timing_mode) | Spawner optimum timing in current mode | 原生证据恢复 | `runtime_formula` |
| [`simulation.snapshot_lookup_playtime`](#simulationsnapshot_lookup_playtime) | Snapshot lookup by playtime (minute-mode) | 重建（无原生标注） | `runtime_formula` |
| [`simulation.snapshot_interpolation_chunk`](#simulationsnapshot_interpolation_chunk) | Snapshot interpolation by day (time chunk) | 重建（无原生标注） | `runtime_formula` |
| [`simulation.snapshot_trim`](#simulationsnapshot_trim) | Trimming TimeChunkSnapshots (TrimSimulationData) | 编辑器工具缺失 | `editor_generator` |
| [`simulation.bake_recalc_optimum_time`](#simulationbake_recalc_optimum_time) | Early-spawner optimum-time shift for bake | 重建（无原生标注） | `config_rule` |
| [`simulation.bake_spawner_health`](#simulationbake_spawner_health) | Baked spawner Health | 原生证据恢复 | `editor_generator` |
| [`simulation.bake_spawner_damage`](#simulationbake_spawner_damage) | Baked spawner Damage | 原生证据恢复 | `editor_generator` |
| [`simulation.bake_apply`](#simulationbake_apply) | Rebake / apply balance to scene spawners | 编辑器工具缺失 | `editor_generator` |
| [`simulation.player_progression_tick`](#simulationplayer_progression_tick) | Bot progression loop (what generated Simulation Data) | 空壳 | `simulation` |
| [`simulation.lifecycle_time_model`](#simulationlifecycle_time_model) | Session / AFK time model | 空壳 | `simulation` |
| [`simulation.spawner_priority`](#simulationspawner_priority) | Bot spawner priority | 空壳 | `simulation` |
| [`simulation.difficulty_map`](#simulationdifficulty_map) | Spawner difficulty classification | 空壳 | `simulation` |
| [`simulation.amulet_sim_multipliers`](#simulationamulet_sim_multipliers) | Amulet soul emission & spawner-charge multipliers in simulation | 编辑器工具缺失 | `simulation` |
| [`simulation.snapshot_growth_curve`](#simulationsnapshot_growth_curve) | Simulated player power vs day (baked balance target) | 纯数据 | `lookup_table` |
| [`simulation.forecast_build_core`](#simulationforecast_build_core) | Forecast snapshot series & enemy growth | 原生证据恢复 | `runtime_formula` |
| [`simulation.forecast_auto_balanced_enemy`](#simulationforecast_auto_balanced_enemy) | Forecast auto-balanced event golem/boss/grand boss | 原生证据恢复 | `runtime_formula` |
| [`simulation.forecast_damage_ramp_average`](#simulationforecast_damage_ramp_average) | Average event damage-ramp multiplier | 重建（无原生标注） | `runtime_formula` |
| [`simulation.forecast_tune_boss_presets`](#simulationforecast_tune_boss_presets) | Boss fight-time/difficulty ramp across event | 重建（无原生标注） | `runtime_formula` |
| [`simulation.forecast_reward`](#simulationforecast_reward) | Forecast boss stat reward | 重建（无原生标注） | `runtime_formula` |
| [`simulation.forecast_event_plan`](#simulationforecast_event_plan) | Event plan assembly | 原生证据恢复 | `lookup_table` |
| [`simulation.forecast_crystal_reward_mapping`](#simulationforecast_crystal_reward_mapping) | Forecast crystal reward entry -> reward | 原生证据恢复 | `lookup_table` |
| [`simulation.spawner_charges_limit`](#simulationspawner_charges_limit) | Spawner charges per daily big respawn (used by RPM and soul RPM) | 原生证据恢复 | `runtime_formula` |
| [`simulation.forecast_current_player_fallback`](#simulationforecast_current_player_fallback) | Forecast fallback snapshot from current player (local addition) | 重建（无原生标注） | `runtime_formula` |
| [`simulation.ads_reward_model`](#simulationads_reward_model) | Simulated rewarded-ad income in the bot | 空壳 | `simulation` |
| [`simulation.stat_benefit_weights`](#simulationstat_benefit_weights) | Bot stat / item benefit weights | 空壳 | `simulation` |
| [`simulation.item_emission_model`](#simulationitem_emission_model) | Simulated item copy emission and fractional rolls | 空壳 | `simulation` |
| [`simulation.bot_fight_resolution`](#simulationbot_fight_resolution) | Bot fight resolution, regen and death handling | 空壳 | `simulation` |
| [`simulation.spawner_charges_chunk`](#simulationspawner_charges_chunk) | Simulated spawner charge budget per day chunk | 空壳 | `simulation` |
| [`simulation.rpm_stat_class_ab_multiplier`](#simulationrpm_stat_class_ab_multiplier) | AB stat-class RPM multiplier and fast-RPM parameters | 重建（无原生标注） | `lookup_table` |
| [`ab.enemy_hp_damage_multiplier`](#abenemy_hp_damage_multiplier) | ab.enemy hp damage multiplier | 重建（无原生标注） | `rebuild_rule` |
| [`balance.armor_base`](#balancearmor_base) | balance.armor base | 重建（无原生标注） | `rebuild_rule` |
| [`balance.evolution_armor_sync_by_leader`](#balanceevolution_armor_sync_by_leader) | balance.evolution armor sync by leader | 重建（无原生标注） | `rebuild_rule` |
| [`balance.evolution_data_identities`](#balanceevolution_data_identities) | balance.evolution data identities | 重建（无原生标注） | `rebuild_rule` |
| [`balance.evolution_fill_remaining`](#balanceevolution_fill_remaining) | balance.evolution fill remaining | 重建（无原生标注） | `rebuild_rule` |
| [`balance.evolution_fitted_h`](#balanceevolution_fitted_h) | balance.evolution fitted h | 重建（无原生标注） | `rebuild_rule` |
| [`balance.evolution_generator`](#balanceevolution_generator) | balance.evolution generator | 重建（无原生标注） | `rebuild_rule` |
| [`balance.evolution_heuristic_fit`](#balanceevolution_heuristic_fit) | balance.evolution heuristic fit | 重建（无原生标注） | `rebuild_rule` |
| [`balance.evolution_nice_rounding`](#balanceevolution_nice_rounding) | balance.evolution nice rounding | 重建（无原生标注） | `rebuild_rule` |
| [`balance.evolution_reference_stat_percent`](#balanceevolution_reference_stat_percent) | balance.evolution reference stat percent | 重建（无原生标注） | `rebuild_rule` |
| [`balance.evolution_reference_stat_value`](#balanceevolution_reference_stat_value) | balance.evolution reference stat value | 重建（无原生标注） | `rebuild_rule` |
| [`balance.evolution_reference_time_chunk`](#balanceevolution_reference_time_chunk) | balance.evolution reference time chunk | 重建（无原生标注） | `rebuild_rule` |
| [`balance.evolution_replay_stored_inputs`](#balanceevolution_replay_stored_inputs) | balance.evolution replay stored inputs | 重建（无原生标注） | `rebuild_rule` |
| [`balance.evolution_targets`](#balanceevolution_targets) | balance.evolution targets | 重建（无原生标注） | `rebuild_rule` |
| [`balance.evolution_value`](#balanceevolution_value) | balance.evolution value | 重建（无原生标注） | `rebuild_rule` |
| [`balance.island_timing`](#balanceisland_timing) | balance.island timing | 重建（无原生标注） | `rebuild_rule` |
| [`balance.shards_capture`](#balanceshards_capture) | balance.shards capture | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.ads`](#simulationads) | simulation.ads | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.ads_reward_stat`](#simulationads_reward_stat) | simulation.ads reward stat | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.amulet_auto_balance`](#simulationamulet_auto_balance) | simulation.amulet auto balance | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.apply_clear`](#simulationapply_clear) | simulation.apply clear | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.armor_base_balance`](#simulationarmor_base_balance) | simulation.armor base balance | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.bake_day`](#simulationbake_day) | simulation.bake day | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.charges_per_clear`](#simulationcharges_per_clear) | simulation.charges per clear | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.crystal_simulation`](#simulationcrystal_simulation) | simulation.crystal simulation | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.current_boss`](#simulationcurrent_boss) | simulation.current boss | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.day_change`](#simulationday_change) | simulation.day change | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.day_change_order`](#simulationday_change_order) | simulation.day change order | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.eligible_spawners`](#simulationeligible_spawners) | simulation.eligible spawners | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.evolution_balance`](#simulationevolution_balance) | simulation.evolution balance | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.execute_tick`](#simulationexecute_tick) | simulation.execute tick | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.fast_accumulate`](#simulationfast_accumulate) | simulation.fast accumulate | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.fast_day_advance`](#simulationfast_day_advance) | simulation.fast day advance | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.fast_flush`](#simulationfast_flush) | simulation.fast flush | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.fast_kill_gate`](#simulationfast_kill_gate) | simulation.fast kill gate | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.fast_tick`](#simulationfast_tick) | simulation.fast tick | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.forecast_unlocked_location`](#simulationforecast_unlocked_location) | simulation.forecast unlocked location | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.fractional_count`](#simulationfractional_count) | simulation.fractional count | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.hand_tick`](#simulationhand_tick) | simulation.hand tick | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.idle_step`](#simulationidle_step) | simulation.idle step | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.item_chain`](#simulationitem_chain) | simulation.item chain | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.item_emission`](#simulationitem_emission) | simulation.item emission | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.item_preferred`](#simulationitem_preferred) | simulation.item preferred | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.make_snapshot`](#simulationmake_snapshot) | simulation.make snapshot | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.next_location_timing`](#simulationnext_location_timing) | simulation.next location timing | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.optimum_time_cap`](#simulationoptimum_time_cap) | simulation.optimum time cap | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.prepare_run`](#simulationprepare_run) | simulation.prepare run | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.process_inventory`](#simulationprocess_inventory) | simulation.process inventory | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.same_day_gates`](#simulationsame_day_gates) | simulation.same day gates | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.slow_death`](#simulationslow_death) | simulation.slow death | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.slow_fight`](#simulationslow_fight) | simulation.slow fight | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.slow_item_reward`](#simulationslow_item_reward) | simulation.slow item reward | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.slow_move_time`](#simulationslow_move_time) | simulation.slow move time | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.slow_priority`](#simulationslow_priority) | simulation.slow priority | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.slow_tick`](#simulationslow_tick) | simulation.slow tick | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.smart_path`](#simulationsmart_path) | simulation.smart path | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.snapshot_current_gpm`](#simulationsnapshot_current_gpm) | simulation.snapshot current gpm | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.stat_benefit`](#simulationstat_benefit) | simulation.stat benefit | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.state_override`](#simulationstate_override) | simulation.state override | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.stop`](#simulationstop) | simulation.stop | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.tick_heartbeat`](#simulationtick_heartbeat) | simulation.tick heartbeat | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.unlock_location`](#simulationunlock_location) | simulation.unlock location | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.weapon_slot_unlocks`](#simulationweapon_slot_unlocks) | simulation.weapon slot unlocks | 重建（无原生标注） | `rebuild_rule` |
| [`simulation.weapon_slots`](#simulationweapon_slots) | simulation.weapon slots | 重建（无原生标注） | `rebuild_rule` |
| [`timeline.days_to_island_timing`](#timelinedays_to_island_timing) | timeline.days to island timing | 重建（无原生标注） | `rebuild_rule` |
| [`timeline.island_timing_to_days`](#timelineisland_timing_to_days) | timeline.island timing to days | 重建（无原生标注） | `rebuild_rule` |

## 公式

### `simulation.balance_timing`

**Simulation balance timing**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x13ebe60`

```text
t = UseSpawnerCharges ? floor(CurrentTotalPlaytime / SpawnerChargesChunkMinutes) : CurrentPlaytime / 60
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `CurrentTotalPlaytime` | simulated wall-clock minutes incl. AFK | Simulation Config.CurrentTotalPlaytime (runtime state) |
| `SpawnerChargesChunkMinutes` | chunk length = 1440 (one day) | Simulation Config.SpawnerChargesChunkMinutes |
| `CurrentPlaytime` | active play minutes | Simulation Config.CurrentPlaytime |

**输出**：integer day index (or hours) used as interpolation timing

**代码**：[Simulation/Systems/SimulationSystem.cs:515-521](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L515)

**状态依据**：Native comment (FRINTM floor). Data: TimeChunkIndex == floor(TotalPlaytime/1440) for all 46 chunk snapshots.

**配置参数**：`Simulation Config.UseSpawnerCharges`；`Simulation Config.SpawnerChargesChunkMinutes`

**使用方**：simulation.ability_multipliers；simulation.dps_data；simulation.defense_data

**备注**：Despite the name it returns days (or hours when UseSpawnerCharges=0). In the game outside simulation CurrentTotalPlaytime is 0, so the timing is day 0 and every curve gives its first value. That makes the runtime RPM use the day-0 ability multipliers. This is an open question.

**依赖图节点**：输入 `config:SimulationConfig.UseSpawnerCharges`、`config:SimulationConfig.SpawnerChargesChunkMinutes`、`value:sim.total_playtime`、`value:sim.active_playtime`、`constant:SimulationSystem.minutes_per_hour`；输出 `value:sim.balance_timing`

### `simulation.snapshot_stat_composition`

**Simulated player stat composition with feature interpolation**

- 状态：**空壳**（`stub`）
- 类型：`simulation`
- 来源：合并核验

```text
(Stub here; math reconstructed from Simulation Data.) For s in MainInterpolationStats {StabDamageFlat 6, CutDamageFlat 8, BuckshotDamageFlat 12, StabDefenceFlat 14, CutDefenceFlat 16, BuckshotDefenceFlat 20, Health 1, HealthRegen 2, LifestealFlat 22}: StatsFromFeaturesFlat_s = K(2,day); PercentMultiplier_s = K(3,day); TotalFlatValue_s = Value_s * (1 + K(2,day)). For s in DirectInterpolationStats {Health, HealthRegen, LifestealFlat}: TotalValue_s = TotalFlatValue_s * K(3,day). For s in CombatFlatInterpolationStats {6,8,12,14,16,20}: TotalValue_s = TotalFlatValue_s (PercentMultiplier still stores K(3,day), but K(3) is applied later inside DpsData/DefenseData). Every other stat: StatsFromFeaturesFlat = 0, PercentMultiplier = 1, TotalFlatValue = TotalValue = Value. CurrentGpm_s = RPMCalculatedValue[s] at snapshot time. Between stored TimeChunkSnapshots every field (Value, StatsFromFeaturesFlat, PercentMultiplier, TotalFlatValue, TotalValue, CurrentGpm) is linearly interpolated: a + (b - a)*t.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Value_s` | stat earned by the bot (base + devour/item rewards) | simulation.player_progression_tick output |
| `K(2,day)` | flat 'features' bonus (graph, amulet, VIP, IAP, events, ads, economy) | balance.total_coefficient, BalanceInterpolationType.StatsFromFeaturesFlat (=2) |
| `K(3,day)` | multiplicative feature bonus | balance.total_coefficient, BalanceInterpolationType.StatModifier (=3) |
| `RPMCalculatedValue[s]` | current stat reward-per-minute | simulation.rpm_time_chunk |
| `t` | interpolation fraction between neighbouring time-chunk snapshots | SimulationData.GetByTimeChunkIndex |

**输出**：SimulationStatData {Key, Value, StatsFromFeaturesFlat, PercentMultiplier, TotalFlatValue, TotalValue, CurrentGpm} per stat in each snapshot

**代码**：[Simulation/Systems/SimulationSystem.cs:183-191](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L183)；[Simulation/Systems/SimulationSystem.cs:822-824](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L822)；[Simulation/Systems/SimulationSystem.cs:889-906](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L889)；[Simulation/SimulationStatData.cs:7-22](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationStatData.cs#L7)；[Simulation/SimulationData.cs:41-56](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationData.cs#L41)；[Simulation/Forecast/ForecastSnapshots.cs:96-101](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastSnapshots.cs#L96)

**状态依据**：MakeSnapshot (822-824), GetSimulationInterpolationCoefficient (889-892), UpdateInterpolationStatModifier, GetSimulationSnapshotStatValue and UsesInterpolationStats (894-906) are all empty/default with no native annotation. Only the stat sets and modifier names (__simulationStatsFromFeaturesFlatModifier, __simulationInterpolationStatModifier) survive. The math was reverse-engineered from Simulation Data: tf/v = 1+K2 and tv/tf = K3 exactly for Health, HealthRegen and LifestealFlat; tv/tf = 1 for the six combat flats (whose PercentMultiplier still holds K3); day 405: Evasion (23) has StatsFromFeaturesFlat 0 and PercentMultiplier 1, key 14 has 3.0 / 4.9075 with TotalValue == TotalFlatValue, key 1 has TotalValue = TotalFlat*4.9075.

**配置参数**：`Combat Balance Config.Interpolations (StatsFromFeaturesFlat, StatModifier)`；`Simulation Data.Snapshots / TimeChunkSnapshots (stored outputs)`

**使用方**：simulation.bake_spawner_health；simulation.bake_spawner_damage；simulation.forecast_auto_balanced_enemy；simulation.forecast_snapshot；[Events/Services/EventForecastService.cs:292](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Events/Services/EventForecastService.cs#L292) (CurrentGpm)；[Crystals/CrystalObjectiveBalance.cs:28-31](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Crystals/CrystalObjectiveBalance.cs#L28) (Health/LifestealFlat TotalValue)

**备注**：This is how non-combat progression systems (map graph, amulet, VIP, IAP, events, ads) enter enemy balance. They are not simulated; they are injected as time curves. CurrentGpm = RPMCalculatedValue[stat] at snapshot time. The local forecast fallback (ForecastSnapshots.cs:96-101) builds snapshot stats with Value = TotalValue = current GetStatValue and leaves StatsFromFeaturesFlat/PercentMultiplier/TotalFlatValue at 0; that is a local fallback, not the original composition.

**依赖图节点**：输入 `value:sim.snapshot_stat_value`、`value:sim.balance_coefficient[StatsFromFeaturesFlat]`、`value:sim.balance_coefficient[StatModifier]`、`value:sim.balance_timing`、`value:sim.stat_rpm`、`constant:SimulationSystem.main_interpolation_stats`、`constant:SimulationSystem.direct_interpolation_stats`、`constant:SimulationSystem.combat_flat_interpolation_stats`；输出 `value:sim.snapshot_stat_total`、`value:sim.snapshot_stat_gpm`

**核验记录**（已写入上方字段）：

- 采纳 `expression`（来自 `simulation.snapshot_stat_composition`）：Expression now restricts StatsFromFeaturesFlat=K2 / PercentMultiplier=K3 to the nine MainInterpolationStats (keys 6/8/12/14/16/20/1/2/22, matching SimulationSystem.cs:187 and the StatType enum), states 0/1 for all other stats, and notes the combat flats store K3 in PercentMultiplier while TotalValue == TotalFlatValue. The data evidence was copied into status_evidence.

### `simulation.dps_data`

**Simulated DPS per weapon type (DpsData)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x13e4478 (+closures 0x13f4634/0x13f465c/0x13f514c), UpdateDpsDataCache 0x13e4f24, GetBalanceTimingMinutes 0x13ebe60, GetForecastStatValue 0x13e83c4, CalculateSimulationData 0x13e4444`

```text
For each WeaponType w in {Stabbing, Cutting, Magic, Buckshot, Universal}: equipped = first UnlockedWeaponSlots of playerModel.Items.Where(!IsArmor).OrderByDescending(DamageType == w).ThenByDescending(item.DPS); DPS_w = SUM_{i in equipped} [ dps_i * SimulationDpsWeaponMultiplier_i ], dps_i = P_i * (F_i + item.Damage_i) * (double)K(3,t) / AttackCooldown_i, then dps_i /= 10 if DamageType_i != w. F_i = GetStatValue(DamageStatTypeFlat_i), P_i = GetStatValue(DamageStatTypePercent_i), K(3,t) = GetInterpolationCoefficient(t, StatModifier, Rare), t = GetBalanceTimingMinutes() = UseSpawnerCharges ? floor(CurrentTotalPlaytime / SpawnerChargesChunkMinutes) : CurrentPlaytime/60. dpsDataCache[w] = DPS_w (overwritten, never cleared).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `F_i` | player flat damage stat for the item's type (includes the 1+K2 feature bonus in the bot) | GetForecastStatValue = playerModel.Unit.GetStatValue(type, true) |
| `P_i` | player percent damage stat for the item's type | GetForecastStatValue |
| `item.Damage` | item damage at its level/evolution | items subsystem (ItemModel.Damage) |
| `item.DPS` | sort key for item choice | ItemModel.DPS |
| `AttackCooldown_i` | weapon cooldown | ItemConfig.AttackCooldown |
| `SimulationDpsWeaponMultiplier_i` | per-item sim fudge factor (79x1, 1.3,1.35,3x1.4,2x1.6,2x2) | ItemConfig.SimulationDpsWeaponMultiplier |
| `UnlockedWeaponSlots` | number of weapon slots | PlayerModel.UnlockedWeaponSlots |
| `K(3,t)` | StatModifier multiplier (float) | balance.total_coefficient (CombatBalanceConfig.GetInterpolationCoefficient, Rarity.Rare) |
| `t` | balance timing: day chunk index when UseSpawnerCharges, else CurrentPlaytime/60 | SimulationSystem.GetBalanceTimingMinutes |

**输出**：List<SimulationDpsData> currentDpsData (stored in snapshots as DpsData) and dpsDataCache

**代码**：[Simulation/Systems/SimulationSystem.cs:908-933](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L908)；[Simulation/Systems/SimulationSystem.cs:869-873](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L869)；[Simulation/Systems/SimulationSystem.cs:515-520](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L515)；[Simulation/Systems/SimulationSystem.cs:993-997](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L993)；[Simulation/Systems/SimulationSystem.cs:321-329](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L321)

**状态依据**：CalculateDpsData carries 'Original ELF VA 0x13e4478 and closures'. Data check: at day 0 (one Cut item) Stab, Magic, Buckshot and Universal are all exactly 1/10 of Cut (0.923 vs 9.23); at later days Magic equals Universal (no items of either type), each equal to the sum of off-type DPS/10 over the equipped items (day 405: Magic = Universal = 1.01e8). This confirms the off-type /10 rule.

**配置参数**：`ItemConfig.SimulationDpsWeaponMultiplier`；`ItemConfig.AttackCooldown`；`ItemConfig.DamageType`；`ItemConfig.DamageStatTypeFlat`；`ItemConfig.DamageStatTypePercent`；`Combat Balance Config.Interpolations (StatModifier)`；`Simulation Config.UseSpawnerCharges`；`Simulation Config.SpawnerChargesChunkMinutes`

**使用方**：simulation.dps_against_spawner；simulation.spawner_fight_model；simulation.bake_spawner_health；simulation.forecast_auto_balanced_enemy；simulation.forecast_snapshot；[BossTower/BossSpawnService.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/BossSpawnService.cs):BakeStatsForFloor

**备注**：Crit, GlobalAttack and ability DPS are NOT in DpsData; they are applied by consumers (fast GetDpsAgainstSpawner, bake). The off-type /10 is applied before the SimulationDpsWeaponMultiplier. equippedItems is a shared list. CalculateDpsData leaves it holding the Universal weapon selection, but CalculateSimulationData runs CalculateDefenseData next (SimulationSystem.cs:324-326), which clears it and refills it with the Universal armor selection (941-950). The slow GetDpsAgainstSpawner path iterates equippedItems.Where(!IsArmor) (841), and ProcessInventory (795-797) is an empty stub, so after CalculateSimulationData that path finds no weapons and returns 0. Runtime uses only the fast path.

**依赖图节点**：输入 `value:player.stat_raw[DamageFlatByType]`、`value:player.stat_raw[DamagePercentByType]`、`value:weapon.item_damage`、`value:weapon.item_dps`、`value:item.copies`、`value:player.unlocked_slot_counts`、`config:ItemConfig.SlotType`、`config:ItemConfig.DamageType`、`config:ItemConfig.DamageStatTypeFlat`、`config:ItemConfig.DamageStatTypePercent`、`config:ItemConfig.AttackCooldown`、`config:ItemConfig.SimulationDpsWeaponMultiplier`、`value:sim.balance_coefficient[StatModifier]`、`value:sim.balance_timing`、`constant:SimulationSystem.off_type_dps_divisor`；输出 `value:sim.dps_by_damage_type`

**核验记录**（已写入上方字段）：

- 采纳 `status_evidence`（来自 `simulation.dps_data`）：status_evidence rewritten with the corrected data observation (all four off-types are 1/10 of Cut at day 0; Magic = Universal later). Consistent with code: one Cut item gives every other type that item's DPS/10 (SimulationSystem.cs:927).
- 合并复核修正：(b/notes vs code) The merged notes say equippedItems 'is reused and left holding the selection of the last weapon type (Universal)'. That is wrong after CalculateSimulationData. CalculateDefenseData runs right after CalculateDpsData (SimulationSystem.cs:324-326), clears equippedItems and refills it with armor items for the last weapon type (lines 941-950). The slow GetDpsAgainstSpawner path iterates equippedItems.Where(!IsArmor) (line 841), and ProcessInventory (795-797) is an empty stub, so after CalculateSimulationData that path sees no weapons. The notes are corrected; all other fields were verified against lines 908-933 and 515-520.

### `simulation.defense_data`

**Simulated defense per damage type (DefenseData)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x13e5104 (+closures 0x13f4674/468c/5180)`

```text
For each WeaponType w: A_w = SUM of item.Armor over the top UnlockedArmorSlots armor items with DamageType==w; Def_w = P_def(w) * (F_def(w) + A_w) * K(3,t)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `F_def(w)` | flat defence stat for w (WeaponTypeToDefence(w,false)) | PlayerModel stat |
| `P_def(w)` | percent defence stat for w | PlayerModel stat |
| `item.Armor` | armor item value | items subsystem |
| `K(3,t)` | StatModifier multiplier | balance.total_coefficient |

**输出**：List<SimulationDpsData> currentDefenseData (snapshot DefenseData)

**代码**：[Simulation/Systems/SimulationSystem.cs:935-958](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L935)

**状态依据**：Native VA comment.

**配置参数**：`Armor item configs (Armor, DamageType)`

**使用方**：simulation.spawner_dps；simulation.bake_spawner_damage；simulation.forecast_auto_balanced_enemy

**备注**：Universal defence stats are not included here.

**依赖图节点**：输入 `value:player.stat_raw[DefenceFlatByType]`、`value:player.stat_raw[DefencePercentByType]`、`value:player.stat_raw[UniversalDefenceFlat]`、`value:player.stat_raw[UniversalDefencePercent]`、`value:weapon.damage_type_defence_stats`、`value:armor.item_armor`、`value:item.copies`、`value:player.unlocked_slot_counts`、`config:ItemConfig.SlotType`、`config:ItemConfig.DamageType`、`value:sim.balance_coefficient[StatModifier]`、`value:sim.balance_timing`；输出 `value:sim.defense_by_damage_type`

### `simulation.combat_multiplier_cache`

**Player defensive/offensive specific-value multipliers**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x13e5bc0, 0x13e686c, 0x13e83c4`

```text
dodge = S(Evasion); blockMult = 1 - S(BlockChance)*S(BlockAmount); gDefMult = 1/(1 + 0.01*S(GlobalDefence)); critMult = S(CritChance)*(S(CritDamagePercent) - 1) + 1; gAtkMult = 1 + 0.01*S(GlobalAttack). S(x) = Stat.CalculateSpecificValue (e.g. Evasion = 0.06*v^0.12 (v<700) else 0.05*v^0.065+0.06; CritChance = 0.08*v^0.15 (v<=700) else 0.07*v^0.1+0.082; CritDamage = 1.5*v^0.08; BlockChance = 0.08*v^0.1; BlockAmount = 0.15*v^0.053; Global* linear to 100 then (v-100)^0.75+100).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `S(stat)` | player specific (post-curve) stat value | PlayerModel.Unit.GetStatSpecificValue / Unit/Stat.cs:61-79 |

**输出**：cached multipliers

**代码**：[Simulation/Systems/SimulationSystem.cs:331-348](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L331)；[Simulation/Systems/SimulationSystem.cs:993-1003](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L993)

**状态依据**：Native VA comments.

**使用方**：simulation.dps_against_spawner；simulation.spawner_dps

**备注**：The same five factors are recomputed from snapshot TotalValues in the forecast (ForecastSnapshots.cs:152-158). The spawner bake uses only crit, evasion and block, not the Global* stats.

**依赖图节点**：输入 `value:player.stat_specific[Evasion]`、`value:player.stat_specific[BlockChance]`、`value:player.stat_specific[BlockAmount]`、`value:player.stat_specific[GlobalDefence]`、`value:player.stat_specific[CritChance]`、`value:player.stat_specific[CritDamagePercent]`、`value:player.stat_specific[GlobalAttack]`、`constant:SimulationSystem.global_stat_percent_scale`；输出 `value:player.expected_crit_multiplier`、`value:player.block_damage_multiplier`、`value:player.global_attack_multiplier`、`value:player.global_defence_multiplier`

### `simulation.ability_multipliers`

**Player ability DPS / EHP multipliers**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x13f2f98, 0x13efd18`

```text
AbDps(t) = 1 + K(4 PlayerAbilityDPSMult, t); AbEhp(t) = 1 + K(5 PlayerAblityEHPMult, t) (Rare rarity selection)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `t` | balance timing (days) | simulation.balance_timing (runtime sim) or snapshot day (bake/forecast) |

**输出**：multipliers >= 1 (shipped: 1..5 for DPS, 1..2.5 for EHP between day 20 and 775)

**代码**：[Simulation/Systems/SimulationSystem.cs:875-887](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L875)；[Simulation/SimulationConfig.cs:120-121](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L120)；[Simulation/Forecast/ForecastSnapshots.cs:54-55](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastSnapshots.cs#L54)

**状态依据**：Native VA comments.

**配置参数**：`Combat Balance Config.Interpolations (AbilityDPS type4, AbilityEHP type5)`

**使用方**：simulation.dps_against_spawner；simulation.spawner_fight_model；simulation.bake_spawner_health；simulation.bake_spawner_damage；simulation.forecast_auto_balanced_enemy

**备注**：These model player power from active abilities, which are not simulated. They directly inflate baked enemy HP (×AbDps) and damage (the inverse solve uses 1/AbEhp). SimulationSystem.GetSimulationInterpolationCoefficient (SimulationSystem.cs:889) is a stub.

**依赖图节点**：输入 `value:sim.balance_coefficient[PlayerAbilityDPSMult]`、`value:sim.balance_coefficient[PlayerAblityEHPMult]`、`value:sim.balance_timing`；输出 `value:sim.ability_dps_multiplier`、`value:sim.ability_ehp_multiplier`

### `simulation.enemy_multiplier`

**Enemy HP/Damage global multipliers in simulation**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x13e13b0, 0x13e68ac`

```text
simMult = IsRunning ? EnemyHpDamageMultiplier : 1; abMult(loc) = ABTest enemy_hp_damage_mult_v3 overridesByLocation[loc] ?? defaultValue (cached per location)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `EnemyHpDamageMultiplier` | bot-only global enemy scale | Simulation Config.EnemyHpDamageMultiplier (=1) |
| `abMult` | AB enemy HP/damage multiplier (variant A default 1; B 1.5 with loc1..4 = 1.1..1.4) | ABTest Data Config.enemyHpDamageMult |

**输出**：multipliers on spawner HP and damage

**代码**：[Simulation/Systems/SimulationSystem.cs:220-224](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L220)；[Simulation/Systems/SimulationSystem.cs:350-357](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L350)；[ABTests/ABTestService.cs:18-19](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L18)

**状态依据**：Native VA comments.

**配置参数**：`Simulation Config.EnemyHpDamageMultiplier`；`ABTest Data Config.enemyHpDamageMult`

**使用方**：simulation.spawner_fight_model；simulation.spawner_dps

**备注**：The bake does NOT apply these, so baked HP and damage are pre-AB values.

**依赖图节点**：输入 `config:SimulationConfig.EnemyHpDamageMultiplier`、`config:SimulationConfig.IsRunning`、`state:sim_bot_state`、`config:ABTestDataConfig.enemyHpDamageMult.variants[].data.damageMultiplier.defaultValue`、`config:ABTestDataConfig.enemyHpDamageMult.variants[].data.damageMultiplier.overridesByLocation`、`config:ABTestDataConfig.enemyHpDamageMult.rules`、`state:ab_cohort`、`config:EnemySpawnerConfig.LocationNumber`；输出 `value:sim.enemy_hp_damage_multiplier`、`value:enemy.ab_hp_damage_multiplier`

### `simulation.dps_against_spawner`

**Player DPS against a spawner**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x13ef248, 0x13f2cf8`

```text
Fast: a* = argmax over spawner armors a with DpsData[a.Type] of DpsData[a.Type]*(1 - a.Resist) (first wins on ties; fallback first armor); DPSvs = AbDps(t) * gAtkMult * critMult * DpsData[a*.Type] * (1 - a*.Resist). Slow: DPSvs = AbDps(t) * SUM_equipped weapons DamageCalculator.Calculate(hit vs spawner, fromSimulation).Damage * SimulationDpsWeaponMultiplier / AttackCooldown.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `a.Resist` | enemy resist per weapon type (can be negative = weakness) | ArmorConfig.Armors[].Resist (e.g. '05 05 -02': Cut 0.5, Stab 0.5, Buck -0.2, Magic 0) |
| `DpsData` | simulated DPS per type | simulation.dps_data |

**输出**：double player DPS vs spawner

**代码**：[Simulation/Systems/SimulationSystem.cs:826-851](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L826)；[Simulation/Systems/SimulationSystem.cs:853-867](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L853)

**状态依据**：Native VA comments.

**配置参数**：`EnemySpawnerConfig.ArmorConfig`；`ItemConfig.SimulationDpsWeaponMultiplier`

**使用方**：simulation.spawner_fight_model

**备注**：The resist is not clamped here. The bake clamps it to [-0.9, 0.9].

**依赖图节点**：输入 `value:sim.dps_by_damage_type`、`config:EnemySpawnerConfig.ArmorConfig`、`config:ArmorConfig.Armors[].Type`、`config:ArmorConfig.Armors[].Resist`、`value:sim.ability_dps_multiplier`、`value:player.global_attack_multiplier`、`value:player.expected_crit_multiplier`、`value:player.expected_damage_per_hit`、`config:ItemConfig.SimulationDpsWeaponMultiplier`、`config:ItemConfig.AttackCooldown`；输出 `value:sim.player_dps_vs_spawner`

### `simulation.spawner_dps`

**Spawner DPS against the player**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x13ef8d4 (GetSpawnerDps), RebuildPerFrameCaches 0x13e5bc0, GetSimulationEnemyMultiplier 0x13e13b0, GetCachedEnemyHpDamageMultiplier 0x13e68ac, GetForecastStatSpecificValue 0x13e686c`

```text
If IsStatic: 0. Fast path (isFast=true, used by the fight model): abMult = IsEvent ? 1 : GetCachedEnemyHpDamageMultiplier(loc); dmg = ApplyDamageDefense(simMult * (BaseDamage * abMult), Def[PreferredWeaponType(loc)]); DPSenemy = dmg / AttackCooldownBase * (1 - dodge) * (1 - bc*ba) * 1/(1 + 0.01*GlobalDefence_spec). ApplyDamageDefense(a,d) = a <= 0 ? 0 : exp(-0.75*d/a) * a * max(0, 1 - 0.25*d/a). Slow path: hit = WeaponHitEvent{Source = spawner.FirstUnit, Target = player, DamageType = spawner.Config.WeaponType, Undodgeable = true}; total = (1 - dodge) * (Calculate(hit, true, 1).Damage / (float)FirstUnit.GetStatValue(AttackCooldown)) * (1 - bc*ba) * simMult; NaN -> 0. simMult = Simulation Config.IsRunning ? EnemyHpDamageMultiplier : 1; dodge, bc, ba, GlobalDefence = player specific values (Stat.CalculateSpecificValue).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `BaseDamage` | spawner Damage stat BaseValue (Stats Type 3) | EnemySpawnerConfig.Stats |
| `AttackCooldownBase` | spawner AttackCooldown stat BaseValue (Type 5) | EnemySpawnerConfig.Stats |
| `Def[...]` | player defense vs the location's preferred weapon type | simulation.defense_data (defenseDataCache), LocationsConfig.Locations[].PreferredWeaponType |
| `abMult` | AB enemy HP/damage multiplier by location | ABTestService.GetEnemyHpDamageMultiplier (EnemyHpDamageMult.damageMultiplier) |
| `simMult` | simulation-only enemy multiplier | Simulation Config.EnemyHpDamageMultiplier (only while IsRunning) |
| `dodge` | player specific Evasion | cachedDodgeChance (RebuildPerFrameCaches) |
| `bc, ba` | player specific BlockChance, BlockAmount | cachedBlockDamageMultiplier = 1 - bc*ba |
| `GlobalDefence_spec` | player specific GlobalDefence | cachedGlobalDefenceMultiplier = 1/(GlobalDefence*0.01 + 1) |

**输出**：double enemy DPS per unit

**代码**：[Simulation/Systems/SimulationSystem.cs:960-991](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L960)；[Simulation/Systems/SimulationSystem.cs:331-348](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L331)；[Simulation/Systems/SimulationSystem.cs:220-224](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L220)；[Simulation/Systems/SimulationSystem.cs:350-357](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L350)；[Simulation/Systems/SimulationSystem.cs:999-1003](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L999)；[Services/Damage/DamageCalculator.cs:24-29](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L24)；[ABTests/ABTestService.cs:18-19](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L18)

**状态依据**：GetSpawnerDps carries 'Original ELF VA 0x13ef8d4'; the cached multipliers come from RebuildPerFrameCaches (0x13e5bc0).

**配置参数**：`EnemySpawnerConfig.Stats (Damage, AttackCooldown)`；`EnemySpawnerConfig.IsStatic`；`EnemySpawnerConfig.IsEvent`；`EnemySpawnerConfig.WeaponType (slow path only)`；`Locations Config.Locations[].PreferredWeaponType`；`ABTest Data Config.EnemyHpDamageMult.damageMultiplier`；`Simulation Config.EnemyHpDamageMultiplier`

**使用方**：simulation.spawner_fight_model

**备注**：The fast path uses the location's preferred weapon type, not the spawner's own WeaponType; the bake does the same. The slow path is different: it builds an Undodgeable WeaponHitEvent with DamageType = spawner.Config.WeaponType and uses the runtime FirstUnit AttackCooldown stat. It applies (1-dodge), (1-bc*ba) and simMult, but not GlobalDefence and not the AB enemy multiplier explicitly (only whatever DamageCalculator.Calculate itself applies). Only the fast path is called at runtime (CalculateSpawnerData passes isFast=true).

**依赖图节点**：输入 `config:EnemySpawnerConfig.IsStatic`、`config:EnemySpawnerConfig.IsEvent`、`config:EnemySpawnerConfig.Stats[Damage]`、`config:EnemySpawnerConfig.Stats[AttackCooldown]`、`config:EnemySpawnerConfig.LocationNumber`、`config:LocationsConfig.Locations[].PreferredWeaponType`、`value:sim.defense_by_damage_type`、`value:enemy.ab_hp_damage_multiplier`、`value:sim.enemy_hp_damage_multiplier`、`value:player.stat_specific[Evasion]`、`value:player.block_damage_multiplier`、`value:player.global_defence_multiplier`、`constant:DamageCalculator.flat_defense_exp_coef`、`constant:DamageCalculator.flat_defense_linear_coef`、`config:EnemySpawnerConfig.WeaponType`、`value:player.incoming_damage_per_hit`、`value:enemy.stat_raw[AttackCooldown]`；输出 `value:sim.spawner_dps_vs_player`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `simulation.spawner_dps`）：Verified at SimulationSystem.cs:979-990. The slow path uses spawner.Config.WeaponType, Undodgeable=true and the runtime FirstUnit AttackCooldown, and multiplies by (1-dodge), (1-block*blockAmount) and GetSimulationEnemyMultiplier only. Notes and expression now limit the preferred-weapon-type statement to the fast path.

### `simulation.spawner_fight_model`

**Per-spawner fight/farm model (SpawnerData)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x13e6074 (CalculateSpawnerData), 0x13e13dc (GetRPMConfig), 0x13efd54 (GetBattleRegen), 0x13e0724 (SpawnerData ctor), 0x13efd98 (GetResourceValue), 0x13efd18 (GetPlayerAblityEhpMultiplier), 0x13e4444 (CalculateSimulationData), 0x13e83ec (GetSpawnerChargesLimit)`

```text
(m,r,f,n) = GetRPMConfig() = (70,50,2,2). HPmax = GetStatValue(Health); speed = specific MovementSpeed; missing = max(0, HPmax - CurrentHealth); moveTime = 70 / speed. Per kept spawner: playerDPS = DPSvs(fast) + (double)0.1f; countF = min((float)(Count+1)/2f, 4f) (float division: Count=2 gives 1.5, Count=4 gives 2.5); enemyDPS = (DPSenemy(fast) + (double)0.1f) * countF / AbEhp(t), AbEhp = 1 + K(PlayerAblityEHPMult, t, Rare); fightTime = simMult * (BaseHP * Count) * abMult(loc) / playerDPS; delta = (BattleRegen(Difficulty) - enemyDPS) * fightTime, using the raw Difficulty; healthDelta = min(delta, missing); regenTime = 50 * max(0, -healthDelta / max(1, HPmax)); farmedTime = regenTime + (moveTime + 2*fightTime); rewardValue = CalculateSpawnerRewardValue(spawner); playerRPS = StatRewards[0].Value / farmedTime, then *= (double)0.2f if -healthDelta > 0.5*HPmax; keep in spawnerData only if fightTime < 300 s. BattleRegen(D) = (D+1)*HealthRegen + (2D+3)*LifestealFlat (stat values). Filters: skip IsEvent or IsVip; skip LocationNumber > LastUnlockedLocation. forGPMBot=true (the only runtime call, from CalculateSimulationData): also skip DisableAfterFirstClear. forGPMBot=false (bot only): skip !AnyoneAlive; skip Cooldown > 0 only if Simulation Config.IsRunning; skip if UseSpawnerCharges and used charges >= GetSpawnerChargesLimit; skip if IsCapOptimumTime and OptimumTime > (UseSpawnerCharges && IsDays ? timeChunkIndex : CurrentPlaytime).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `BaseHP` | spawner Health stat BaseValue | EnemySpawnerConfig.Stats Type 1 |
| `Count` | units per spawner | EnemySpawnerConfig.Count |
| `Difficulty` | spawner difficulty (fraction of player HP to lose); used raw here, without the <=0 -> 1 validation the bake applies | EnemySpawnerConfig.Difficulty |
| `StatRewards[0].Value` | value of the first StatReward (GetResourceValue) | EnemySpawnerConfig.StatRewards |
| `HPmax` | player Health stat value | GetForecastStatValue(Health) |
| `DPSvs` | player DPS against the spawner, fast path | simulation.dps_against_spawner |
| `DPSenemy` | spawner DPS against the player, fast path | simulation.spawner_dps |
| `abMult(loc)` | AB enemy HP/damage multiplier | ABTestService.GetEnemyHpDamageMultiplier |
| `simMult` | Simulation Config.EnemyHpDamageMultiplier while IsRunning, else 1 | GetSimulationEnemyMultiplier |
| `t` | balance timing | GetBalanceTimingMinutes |

**输出**：SpawnerData {playerDPS, enemyDPS, fightTime, healthDelta, farmedTime, avMoveTime, avRegenTime, rewardValue, playerRPS} in spawnerData[spawner]

**代码**：[Simulation/Systems/SimulationSystem.cs:623-674](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L623)；[Simulation/Systems/SimulationSystem.cs:226-230](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L226)；[Simulation/Systems/SimulationSystem.cs:220-224](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L220)；[Simulation/Systems/SimulationSystem.cs:321-329](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L321)；[Simulation/Systems/SimulationSystem.cs:608-612](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L608)；[Simulation/Systems/SimulationSystem.cs:686-691](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L686)；[Simulation/Systems/SimulationSystem.cs:758-763](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L758)；[Simulation/Systems/SimulationSystem.cs:882-887](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L882)；[Services/Damage/DamageCalculator.cs:59-62](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L59)；[Simulation/Systems/SpawnerData.cs:36-42](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SpawnerData.cs#L36)

**状态依据**：CalculateSpawnerData carries 'Original ELF VA 0x13e6074. Preserve float32 count factor and widened float literals.'

**配置参数**：`EnemySpawnerConfig.Count`；`EnemySpawnerConfig.Difficulty`；`EnemySpawnerConfig.Stats`；`EnemySpawnerConfig.StatRewards`；`EnemySpawnerConfig.IsEvent`；`EnemySpawnerConfig.IsVip`；`EnemySpawnerConfig.LocationNumber`；`EnemySpawnerConfig.DisableAfterFirstClear`；`EnemySpawnerConfig.OptimumTime`；`EnemySpawnerConfig.IsDays`；`Simulation Config.IsCapOptimumTime`；`Simulation Config.UseSpawnerCharges`；`Simulation Config.IsRunning`；`Simulation Config.EnemyHpDamageMultiplier`；`ABTest Data Config.EnemyHpDamageMult.damageMultiplier`；`Combat Balance Config.Interpolations (PlayerAblityEHPMult)`

**使用方**：simulation.rpm_time_chunk；simulation.rpm_old；simulation.soul_rpm；simulation.player_progression_tick

**备注**：At runtime CalculateSimulationData always calls CalculateSpawnerData(true), so the game only uses GPM-bot mode: no alive, cooldown, charge or optimum-time filters, and DisableAfterFirstClear spawners are excluded. The non-GPM filters exist only for the (stubbed) bot. The 0.1f and 0.2f literals are float32 widened to double. playerRPS uses only the first StatReward value (GetResourceValue), while rewardValue uses the benefit-weighted CalculateSpawnerRewardValue. abMult multiplies fightTime regardless of IsEvent (event spawners are skipped anyway).

**依赖图节点**：输入 `value:sim.player_dps_vs_spawner`、`value:sim.spawner_dps_vs_player`、`value:sim.ability_ehp_multiplier`、`value:sim.enemy_hp_damage_multiplier`、`value:enemy.ab_hp_damage_multiplier`、`config:EnemySpawnerConfig.Stats[Health]`、`config:EnemySpawnerConfig.Count`、`value:player.battle_regen`、`config:EnemySpawnerConfig.Difficulty`、`value:player.stat_raw[Health]`、`value:player.current_hp`、`value:player.stat_specific[MovementSpeed]`、`config:EnemySpawnerConfig.StatRewards[].Value`、`config:EnemySpawnerConfig.IsEvent`、`config:EnemySpawnerConfig.IsVip`、`config:EnemySpawnerConfig.LocationNumber`、`value:player.unlocked_location`、`config:EnemySpawnerConfig.DisableAfterFirstClear`、`config:EnemySpawnerConfig.OptimumTime`、`config:EnemySpawnerConfig.IsDays`、`config:SimulationConfig.IsCapOptimumTime`、`config:SimulationConfig.UseSpawnerCharges`、`config:SimulationConfig.IsRunning`、`value:spawner.charges_limit_per_day`、`state:sim_bot_state`、`constant:SimulationSystem.rpm_move_distance`、`constant:SimulationSystem.rpm_regen_time_factor`、`constant:SimulationSystem.rpm_fight_time_multiplier`、`constant:SimulationSystem.dps_epsilon`、`constant:SimulationSystem.count_factor_divisor`、`constant:SimulationSystem.count_factor_cap`、`constant:SimulationSystem.heavy_damage_hp_fraction`、`constant:SimulationSystem.heavy_damage_rps_penalty`、`constant:SimulationSystem.max_fight_time_seconds`；输出 `value:sim.spawner_fight_time`、`value:sim.spawner_health_delta`、`value:sim.spawner_farmed_time`、`value:sim.spawner_player_rps`

**核验记录**（已写入上方字段）：

- 采纳 `expression`（来自 `simulation.spawner_fight_model`）：Verified at SimulationSystem.cs:638-660 and :328. Expression now uses float division for countF, gives the exact non-GPM filters (cooldown only when IsRunning, charges, optimum time against timeChunkIndex vs CurrentPlaytime), and states that runtime always uses forGPMBot=true.

### `simulation.spawner_reward_value`

**Spawner reward value for bot priority**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x13efd98, 0x13e0cb8`

```text
GetResourceValue = StatRewards[0].Value (0 if none). CalculateSpawnerRewardValue(sp, stat?) = if stat given: reward(stat).Value; if sp==currentBoss: 1e6; v = SUM_r statsBenefit[r.StatType]*r.Value; if ItemReward: v = (owned && itemBenefit) ? v + itemBenefit : 10000; return v*Count
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `statsBenefit` | per-stat utility weight (init 1, updated by CalculateStatBenefit stub) | runtime state |
| `StatRewards` | devour stat rewards | EnemySpawnerConfig.StatRewards |

**输出**：reward value

**代码**：[Simulation/Systems/SimulationSystem.cs:758-787](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L758)

**状态依据**：Native VA comments.

**配置参数**：`EnemySpawnerConfig.StatRewards`；`EnemySpawnerConfig.ItemReward`；`EnemySpawnerConfig.Count`

**使用方**：simulation.spawner_fight_model；simulation.spawner_priority

**备注**：CalculateStatBenefit (SimulationSystem.cs:717) is a stub, so statsBenefit stays 1.

**依赖图节点**：输入 `config:EnemySpawnerConfig.StatRewards[].Value`、`config:EnemySpawnerConfig.StatRewards[].StatType`、`config:EnemySpawnerConfig.ItemReward`、`config:EnemySpawnerConfig.Count`、`value:sim.stat_benefit_weights`、`value:item.copies`、`state:sim_bot_state`、`constant:SimulationSystem.current_boss_reward_value`、`constant:SimulationSystem.unowned_item_reward_value`；输出 `value:sim.spawner_reward_value`

### `simulation.rpm_time_chunk`

**Stat reward-per-minute (RPM/GPM), current algorithm**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x13e7cf4 (CalculateRPMValueTimeChunk(StatType)), 0x13e7a00 (CalculateRPMValueTimeChunk()), 0x13e71d4 (CalculateRPMValue(StatType) dispatcher), 0x13e6adc (CalculateRPMValue()), 0x13e1468 (GetFastRPMConfig), 0x13e83ec (GetSpawnerChargesLimit)`

```text
(F, L) = GetFastRPMConfig() = AB FastRPMParams (fightTimeSeconds, maxHealthLossMultiplier): (10, 1) variant A / (15, 5) variant B. HPmax = GetStatValue(Health). Candidates = spawnerData entries whose StatRewards contain type k (none -> RPM_k unchanged). For each, loss_s = -healthDelta_s; skip if L*HPmax < loss_s; c_s = F/(F + fightTime_s) * reward_s(k) * charges_s * locMult_s * hpMult_s, where reward_s(k) = first StatReward of type k, charges_s = GetSpawnerChargesLimit = DisableAfterFirstClear ? Count : ChargesTotal, locMult = 2 for non-elite spawners on locations 1-2 else 1, hpMult = sqrt(HPmax/loss_s) if loss_s > HPmax else 1. RPM_k = SUM c_s / 60; if RPM_k > 0 then RPM_k = max(0.5, RPM_k); RPM_k *= AB RpmStatValueMultiplier(k) (attack for {6,8,12}, armor for {14,16,20}, sustain for {1,2,22}, else 1); RPM_k = max(RPM_k, previous RPM_k) (monotonic). ChargesTotal = IsChargesEnabled ? eff : 0, IsChargesEnabled = !IsEvent && eff > 0; eff = CanUseSpawnerCooldownsAb (not Event, not Vip, no ItemReward, Cooldown >= 10) ? AB spawnerCooldowns(isElite).charges(loc) * Count : ChargesBeforeBigRespawn + meta 'items_rarity_charges'(ItemReward rarity) bonus (bonus only for item-reward spawners).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `reward_s(k)` | stat reward per kill (first StatReward of type k) | EnemySpawnerConfig.StatRewards |
| `ChargesTotal` | kills per big respawn (daily); 0 if charges disabled, so such spawners contribute 0 | EnemySpawner.ChargesTotal / ComputeEffectiveChargesBeforeBigRespawn; ABTestService.GetSpawnerCharges; SpawnerChargesCalculator.GetTotalCharges |
| `fightTime_s, healthDelta_s` | from the fight model | simulation.spawner_fight_model |
| `F, L` | fight-time softening constant and max health-loss multiplier | ABTest Data Config.fastRPMParams |
| `60` | divisor; inferred = designed active minutes per day (6 sessions x 10 min) | constant |
| `HPmax` | player Health stat value | GetForecastStatValue(Health) |

**输出**：RPMCalculatedValue[StatType] (stat gained per minute of play)

**代码**：[Simulation/Systems/SimulationSystem.cs:426-461](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L426)；[Simulation/Systems/SimulationSystem.cs:396-400](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L396)；[Simulation/Systems/SimulationSystem.cs:368-382](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L368)；[Simulation/Systems/SimulationSystem.cs:232-237](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L232)；[Simulation/Systems/SimulationSystem.cs:608-612](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L608)；[ABTests/ABTestService.cs:26-27](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L26)；[ABTests/ABTestService.cs:40-41](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L40)；[ABTests/ABTestService.cs:45](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L45)；[ABTests/ABTestService.cs:49-58](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L49)；[Enemy/EnemySpawner.cs:307-328](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L307)；[Enemy/EnemySpawner.cs:697-720](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L697)；[Meta/SpawnerChargesCalculator.cs:23-40](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SpawnerChargesCalculator.cs#L23)

**状态依据**：CalculateRPMValueTimeChunk(StatType) carries 'Original ELF VA 0x13e7cf4: original location multipliers and FP operation order'.

**配置参数**：`ABTest Data Config.fastRPMParams`；`ABTest Data Config.rpmOldAlgorithm.enabled`；`ABTest Data Config.commonRewardMultipliers.attack/armor/sustainStatValueMultiplier`；`ABTest Data Config.spawnerCooldowns.elite/nonElite.charges`；`EnemySpawnerConfig.StatRewards`；`EnemySpawnerConfig.ChargesBeforeBigRespawn`；`EnemySpawnerConfig.DisableAfterFirstClear`；`EnemySpawnerConfig.IsEliteBoss`；`EnemySpawnerConfig.LocationNumber`；`EnemySpawnerConfig.Count`；`EnemySpawnerConfig.Cooldown`；`EnemySpawnerConfig.ItemReward`；`Meta stat items_rarity_charges`

**使用方**：[UI/Events/RewardView.cs:288](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/Events/RewardView.cs#L288)；[Rewards/Commands/GrantRewardCommandRunner.cs:227](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/Commands/GrantRewardCommandRunner.cs#L227)；[Shop/Systems/ShopLotRpmCalculationSystem.cs:77](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/Systems/ShopLotRpmCalculationSystem.cs#L77)；[Rewards/Commands/BakeRewardsCommandRunner.cs:84-93](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/Commands/BakeRewardsCommandRunner.cs#L84)；[Enemy/EnemySpawner.cs:990](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L990) (crystal-event mob reward = max(1, 0.4*RPM))；[Events/Services/EventForecastService.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Events/Services/EventForecastService.cs) (via snapshot CurrentGpm)；simulation.forecast_reward；simulation.snapshot_stat_composition (CurrentGpm)

**备注**：This is the main runtime link between spawner configuration and the value of rewarded, shop and event stat rewards: reward = Value x RPM[k]. With the daily big-respawn charges it reads as 'achievable daily devour income / 60 minutes'. CalculateRPMValue(StatType) falls through to this method unless UseOldRpmAlgorithm (rpm_old_algorithm) is enabled; the shipped rule is A, which is disabled. Because runtime spawnerData is built with forGPMBot=true, DisableAfterFirstClear spawners are never candidates at runtime, so the Count branch of GetSpawnerChargesLimit applies only inside the bot. Candidates are only spawners that passed the fight-model filters (fightTime < 300 s).

**依赖图节点**：输入 `config:ABTestDataConfig.fastRPMParams.variants[].data.fightTimeSeconds`、`config:ABTestDataConfig.fastRPMParams.variants[].data.maxHealthLossMultiplier`、`config:ABTestDataConfig.rpmOldAlgorithm.variants[].data.enabled`、`state:ab_cohort`、`value:player.stat_raw[Health]`、`value:sim.spawner_health_delta`、`value:sim.spawner_fight_time`、`config:EnemySpawnerConfig.StatRewards[].Value`、`config:EnemySpawnerConfig.StatRewards[].StatType`、`value:spawner.charges_limit_per_day`、`config:EnemySpawnerConfig.IsEliteBoss`、`config:EnemySpawnerConfig.LocationNumber`、`constant:SimulationSystem.locationMultipliers`、`constant:SimulationSystem.rpm_minutes_divisor`、`constant:SimulationSystem.rpm_min_floor`、`config:ABTestDataConfig.commonRewardMultipliers.variants[].data.attackStatValueMultiplier`、`config:ABTestDataConfig.commonRewardMultipliers.variants[].data.armorStatValueMultiplier`、`config:ABTestDataConfig.commonRewardMultipliers.variants[].data.sustainStatValueMultiplier`；输出 `value:sim.stat_rpm`

**核验记录**（已写入上方字段）：

- 采纳 `variables`（来自 `simulation.rpm_time_chunk`）：Verified at EnemySpawner.cs:307-328 and 707-720 (IsChargesEnabled = !IsEvent && eff>0; eff = CanUseSpawnerCooldownsAb ? AB charges*Count : GetTotalCharges), SpawnerChargesCalculator.cs:23-40 (items_rarity_charges bonus only when ItemReward != null), and SimulationSystem.cs:328/659 (forGPMBot=true excludes DisableAfterFirstClear). The ChargesTotal variable, expression and notes are updated.

### `simulation.rpm_old`

**Stat RPM, old greedy-1000s algorithm**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x13e71d4 (closures 0x13f46e4/47dc/47fc/40f4)`

```text
Candidates with reward k and playerRPS>0, sorted by playerRPS desc; budget 1000 s: for j-th candidate: time = farmedTime + 2*j / S(MovementSpeed); fights = min(1000/(BaseCooldown*Count), (1000 - T)/time); T += time*fights; R += fights*reward(k)*Count; stop when T >= 999.9. RPM_k = min(1, 1000/T) * R / max(1000, T) * 60; if >0 then max(0.5, .); monotonic max with previous.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `BaseCooldown` | spawner respawn cooldown | EnemySpawner.BaseCooldown |
| `farmedTime, playerRPS` | from fight model | simulation.spawner_fight_model |

**输出**：RPMCalculatedValue[StatType]

**代码**：[Simulation/Systems/SimulationSystem.cs:396-424](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L396)；[Simulation/Systems/SimulationSystem.cs:368-374](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L368)

**状态依据**：Native VA comments.

**配置参数**：`ABTest Data Config.rpmOldAlgorithm (rules A = disabled)`；`EnemySpawnerConfig.Cooldown`；`EnemySpawnerConfig.Count`

**使用方**：same consumers as simulation.rpm_time_chunk when AB enabled

**备注**：Inactive in the shipped AB config.

**依赖图节点**：输入 `config:ABTestDataConfig.rpmOldAlgorithm.variants[].data.enabled`、`state:ab_cohort`、`value:sim.spawner_player_rps`、`value:sim.spawner_farmed_time`、`value:player.stat_specific[MovementSpeed]`、`constant:SimulationSystem.rpm_move_distance_factor`、`value:spawner.small_cooldown`、`config:EnemySpawnerConfig.Count`、`config:EnemySpawnerConfig.StatRewards[].Value`、`config:EnemySpawnerConfig.StatRewards[].StatType`、`constant:SimulationSystem.rpm_old_time_budget`、`constant:SimulationSystem.rpm_minutes_divisor`、`constant:SimulationSystem.rpm_min_floor`；输出 `value:sim.stat_rpm`

### `simulation.soul_rpm`

**Soul reward-per-minute**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x13e6dd0, 0x13e8424, 0x13e86b8, 0x14889a4`

```text
For each distinct non-blank SoulReward key q: kills = SUM over spawners with SoulReward==q and -healthDelta <= HPmax of 10/(fightTime + 10) * charges_s; SoulRPM_q = (1 + soul_drop_bonus + souls_rarity_amount[q]) * kills/60; if >0 then max(0.03, .); monotonic max with previous.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `charges_s` | GetSpawnerChargesLimit | simulation.rpm_time_chunk |
| `soul_drop_bonus, souls_rarity_amount` | meta stat bonuses | MetaStatsModel (Meta Stats Config) |

**输出**：SoulRPMCalculatedValue[soulKey]

**代码**：[Simulation/Systems/SimulationSystem.cs:463-495](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L463)；[Meta/SoulDropAmountCalculator.cs:10-13](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SoulDropAmountCalculator.cs#L10)

**状态依据**：Native VA comments.

**配置参数**：`EnemySpawnerConfig.SoulReward`；`Meta Stats Config soul_drop_bonus / souls_rarity_amount`

**使用方**：[Shop/Systems/ShopLotRpmCalculationSystem.cs:91](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/Systems/ShopLotRpmCalculationSystem.cs#L91)；simulation.amulet_sim_multipliers

**备注**：F is fixed at 10 here; it is not the AB parameter.

**依赖图节点**：输入 `value:player.stat_raw[Health]`、`value:sim.spawner_health_delta`、`value:sim.spawner_fight_time`、`config:EnemySpawnerConfig.SoulReward`、`value:spawner.charges_limit_per_day`、`constant:SimulationSystem.soul_rpm_fight_time_softening`、`constant:SimulationSystem.rpm_minutes_divisor`、`constant:SimulationSystem.soul_rpm_min_floor`、`value:player.meta_stat[soul_drop_bonus]`、`value:player.meta_stat[souls_rarity_amount]`；输出 `value:sim.soul_rpm`

### `simulation.spawner_optimum_timing_mode`

**Spawner optimum timing in current mode**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x13ed83c`

```text
timing = (UseSpawnerCharges && !IsDays) ? OptimumTime / TimeChunkDurationMinutes : OptimumTime; autoGamblingOptimumTime = max over spawners
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `TimeChunkDurationMinutes` | chunk length for minute-mode spawners (30) | Simulation Config.TimeChunkDurationMinutes |

**输出**：timing in chunk units

**代码**：[Simulation/Systems/SimulationSystem.cs:533-539](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L533)；[Simulation/Systems/SimulationSystem.cs:285](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L285)

**状态依据**：Native VA comment.

**配置参数**：`Simulation Config.TimeChunkDurationMinutes`；`Simulation Config.UseSpawnerCharges`；`EnemySpawnerConfig.OptimumTime`；`EnemySpawnerConfig.IsDays`

**使用方**：simulation.player_progression_tick (auto-gambling stop)

**备注**：The units do not match: snapshot TimeChunkIndex counts days (1440 min), but this divides by 30. See open questions.

**依赖图节点**：输入 `config:SimulationConfig.UseSpawnerCharges`、`config:EnemySpawnerConfig.IsDays`、`config:EnemySpawnerConfig.OptimumTime`、`config:SimulationConfig.TimeChunkDurationMinutes`；输出 `value:spawner.optimum_timing_chunks`

### `simulation.snapshot_lookup_playtime`

**Snapshot lookup by playtime (minute-mode)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：合并核验

```text
snap = Snapshots.OrderBy(|snap.Playtime - t|).FirstOrDefault() (nearest snapshot, float abs; stable sort so ties go to the earlier list entry; no interpolation). Used by GetSnapshotForBake when !IsDays, with t = re-calculated optimum time in minutes.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `t` | (re-calculated) optimum time in active-play minutes | EnemySpawnerConfig.OptimumTime (IsDays=0), via SimulationConfig.GetReCalcOptimumTime |
| `Snapshots` | minute-mode snapshot list | Simulation Data.Snapshots |

**输出**：SimulationSnapshot

**代码**：[Simulation/SimulationData.cs:13-16](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationData.cs#L13)；[Simulation/SimulationConfig.cs:233-239](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L233)

**状态依据**：No native annotation on GetByPlayTime or GetSnapshotForBake. Data check: with nearest-snapshot lookup, 79 minute-mode spawners bake to exactly the shipped scene Health (79 of 102 in the first count over 706 spawners; 79 of 108 in the later independent recomputation over 716 spawners, see simulation.bake_spawner_health). No day-mode spawner matches exactly, so minute-mode data is the part consistent with the scene.

**配置参数**：`Simulation Data.Snapshots (63 entries, Playtime 0..248.1 min; about 1.1 min apart in the first 10-minute cycle (9 snapshots in 0-10.1 min), about 5 min apart after that (SnapshotInterval 300 s); duplicate Playtimes at day boundaries: 63.3 twice, 186.0 twice)`

**使用方**：simulation.bake_spawner_health；simulation.bake_spawner_damage

**备注**：Covers roughly the first 4 days of the game (Playtime <= 248.1 min). Minute-mode spawners have OptimumTime up to 240. For duplicate Playtimes (63.3, 186.0) the stable OrderBy returns the first of the tie. GetByPlayTime has no other caller.

**依赖图节点**：输入 `value:spawner.bake_optimum_time`、`value:sim.snapshot_progress`、`value:sim.snapshot_stat_value`、`value:sim.snapshot_stat_total`、`value:sim.snapshot_stat_gpm`、`value:sim.dps_by_damage_type`、`value:sim.defense_by_damage_type`；输出 `value:sim.snapshot_at_time`

**核验记录**（已写入上方字段）：

- 采纳 `config_params`（来自 `simulation.snapshot_lookup_playtime`）：config_params now give the parsed Playtime distribution and duplicates. The tie-break claim matches the code: LINQ OrderBy is stable, so FirstOrDefault returns the earlier tied entry (SimulationData.cs:15).
- 合并复核修正：The status_evidence count '79 of 102 minute-mode spawners' contradicts the merged simulation.bake_spawner_health evidence: its later independent recomputation counts 108 minute-mode spawners out of 716 (716 - 608 day-mode), still with all 79 exact matches. Left unreconciled, the two merged entries give different denominators for the same data check.

### `simulation.snapshot_interpolation_chunk`

**Snapshot interpolation by day (time chunk)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
Sort by TimeChunkIndex; x <= first -> first; x >= last -> last; else with L,R bracketing x: f = (x - L.tc)/(R.tc - L.tc); every stat field, DpsData, DefenseData and TotalPlaytime = L + (R-L)*f; Location, TimeChunkIndex = (f>=0.5 ? R : L); Items = L.Items; Playtime := x
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `x` | fractional day | bake re-calculated OptimumTime / forecast day / tower chunk |

**输出**：interpolated SimulationSnapshot

**代码**：[Simulation/SimulationData.cs:18-86](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationData.cs#L18)；[Simulation/SimulationConfig.cs:237-238](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L237)

**状态依据**：No native annotation. Behavior matches the data well: recomputed bakes for day-mode spawners land within 5% of the scene for 433/604.

**配置参数**：`Simulation Data.TimeChunkSnapshots (46 entries, days 0..405)`；`Simulation Config.TimeChunkSnapshots (empty; preferred if non-empty)`

**使用方**：simulation.bake_spawner_health；simulation.bake_spawner_damage；simulation.forecast_snapshot；[BossTower/BossSpawnService.cs:446-490](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/BossSpawnService.cs#L446)

**备注**：Linear interpolation in absolute value, while the underlying growth is roughly exponential. Bakes therefore slightly overshoot between trimmed points. GetStatValueAtTimeChunk returns .Value (pre-feature), not TotalValue.

**依赖图节点**：输入 `value:spawner.bake_optimum_time`、`value:time.island_timing_days`、`state:boss_tower_context`、`constant:BossModel.floor_optimum_time_curve`、`config:BossTowerConfig.ArmorBossOptimumTimeOffsetMinutes`、`config:QuestConfig.Chains[].Steps[].OptimumTime`、`config:QuestConfig.Chains[].Steps[].IsDays`、`constant:SpawnerOptimumTimeDaysConverter.minutes_per_day`、`config:SimulationConfig.TimeChunkDurationMinutes`、`config:SimulationConfig.TimeChunkSnapshots`、`value:sim.snapshot_progress`、`value:sim.snapshot_stat_value`、`value:sim.snapshot_stat_total`、`value:sim.snapshot_stat_gpm`、`value:sim.dps_by_damage_type`、`value:sim.defense_by_damage_type`；输出 `value:sim.snapshot_at_time`

### `simulation.snapshot_trim`

**Trimming TimeChunkSnapshots (TrimSimulationData)**

- 状态：**编辑器工具缺失**（`editor_only_missing`）
- 类型：`editor_generator`
- 来源：子系统梳理

```text
Inferred: keep chunk index i_0 = 0 and i_{n+1} = max(i_n + 1, ceil(1.1 * i_n)), plus always the last chunk (=> 0..11, 13,15,17,19,21,24,27,30,33,37,41,46,51,57,63,70,77,85,94,104,115,127,140,154,170,187,206,227,250,275,303,334,368,405)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `i_n` | kept day index | Simulation Data.TimeChunkSnapshots[].TimeChunkIndex |

**输出**：trimmed snapshot list stored in Simulation Data

**代码**：[Simulation/SimulationConfig.cs:98-101](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L98)；[Simulation/SimulationConfig.cs:55](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L55)

**状态依据**：GetTrimmedTimeChunkSnapshots returns null. All 46 stored indices fit the 10% geometric rule exactly.

**配置参数**：`Simulation Config.TrimSimulationData`

**使用方**：simulation.snapshot_interpolation_chunk

**备注**：Trimming adds interpolation error to bakes and the forecast.

**依赖图节点**：输入 `config:SimulationConfig.TrimSimulationData`、`constant:SimulationConfig.trim_geometric_ratio`；输出 `value:sim.snapshot_progress`

### `simulation.bake_recalc_optimum_time`

**Early-spawner optimum-time shift for bake**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`config_rule`
- 来源：子系统梳理

```text
if LocationNumber >= EarlySpawnerOptimumTimeOffsetStartLocation (11) and RelativeOptimumTime < RelPart (0.5): delta = (IslandTargetDays[L-1] - IslandTargetDays[L-2]) * (RelPart - RelativeOptimumTime) * Multiplier(1); if !IsDays delta *= 60; bakeTime = OptimumTime - delta; else bakeTime = OptimumTime
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `L` | spawner island | EnemySpawnerConfig.LocationNumber |
| `RelativeOptimumTime` | 0..1 position inside island | EnemySpawnerConfig.RelativeOptimumTime |

**输出**：ReCalcOptimumTime (snapshot lookup time for the bake)

**代码**：[Simulation/SimulationConfig.cs:219-231](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L219)；[Simulation/SimulationConfig.cs:105](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L105)

**状态依据**：No native annotation on this helper (the caller BuildSpawnerBakeReport is native-annotated). Applying it improves agreement with scene values on islands 11+.

**由数据反推的生成规则**（拟合规则，详见 [Generation/BakeCheck.md](../Generation/BakeCheck.md)）：

```text
Early-spawner offset active when the scene was baked (L >= 11 and Rel < 0.5).
```

吻合情况：Among 182 shifted spawners, HP within 5%: 147 with shift, 13 without; Damage within 5%: 177 with shift, 16 without.

**配置参数**：`Simulation Config.EarlySpawnerOptimumTimeOffsetStartLocation`；`Simulation Config.EarlySpawnerOptimumTimeOffsetRelativePart`；`Simulation Config.EarlySpawnerOptimumTimeOffsetMultiplier`；`Combat Balance Config.IslandTargetDays`

**使用方**：simulation.bake_spawner_health；simulation.bake_spawner_damage

**备注**：On late islands, spawners in the first half of the island are baked against the player's power from earlier days, so they are easier. With the shipped values the shift reaches up to half an island's duration at Rel=0.

**依赖图节点**：输入 `config:EnemySpawnerConfig.LocationNumber`、`config:EnemySpawnerConfig.RelativeOptimumTime`、`config:EnemySpawnerConfig.OptimumTime`、`config:EnemySpawnerConfig.IsDays`、`config:SimulationConfig.EarlySpawnerOptimumTimeOffsetStartLocation`、`config:SimulationConfig.EarlySpawnerOptimumTimeOffsetRelativePart`、`config:SimulationConfig.EarlySpawnerOptimumTimeOffsetMultiplier`、`config:CombatBalanceConfig.IslandTargetDays`、`constant:SimulationConfig.minutes_per_day_minute_mode`；输出 `value:spawner.bake_optimum_time`

### `simulation.bake_spawner_health`

**Baked spawner Health**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`editor_generator`
- 来源：合并核验
- 原生地址：`0x13db16c`

```text
bakeTime = GetReCalcOptimumTime(spawner) (OptimumTime minus the early-spawner shift); snap = IsDays ? GetByTimeChunkIndex(bakeTime, config.TimeChunkSnapshots if non-empty else SimulationData.TimeChunkSnapshots) : GetByPlayTime(bakeTime); day = IsDays ? bakeTime : bakeTime/60; cc = S(CritChance, snap.TotalValue), cd = S(CritDamagePercent, snap.TotalValue); rows = Armors ordered desc (stable) by Score = DpsData[a.Type] * (1 - a.Resist), using the unclamped resist and 0 if the type is missing; a* = armor of rows[0]; r = clamp(a*.Resist, -0.9, 0.9); P0 = (1 + K(4,day)) * DpsData[a*.Type]; Peff = (1 - cc)*P0 + cd*cc*P0; NewHealth = Math.Round(Peff * AverageFightTime / Count * (1 - r)) (banker's rounding). CalculateBakedSpawnerHealth(spawner, d) = same with bakeTime = d, isDays = true and no early shift.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `DpsData` | simulated player DPS by weapon type | Simulation Data snapshot (simulation.dps_data) |
| `AverageFightTime` | designed seconds to clear the spawner | EnemySpawnerConfig.AverageFightTime (5 s for 318 spawners; 10, 12, 15, 20, ... up to 500) |
| `Count` | units | EnemySpawnerConfig.Count |
| `Armors` | enemy resists | EnemySpawnerConfig.ArmorConfig |
| `K(4,day)` | ability DPS curve (PlayerAbilityDPSMult) | simulation.ability_multipliers |
| `S(...)` | specific (diminishing) stat value | Stat.CalculateSpecificValue (Unit/Stat.cs:61) |

**输出**：per-unit Health written to EnemySpawnerConfig.Stats[Health].BaseValue (SpawnerBakeReportData.NewHealth)

**代码**：[Simulation/SimulationConfig.cs:103-111](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L103)；[Simulation/SimulationConfig.cs:113-160](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L113)；[Simulation/SimulationConfig.cs:219-239](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L219)；[Simulation/SimulationData.cs:23-39](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationData.cs#L23)；[Unit/Stat.cs:61](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L61)

**状态依据**：BuildSpawnerBakeReport carries 'Original ELF0x13db16c plus all19 actual predicates/captured armor selectors'. An initial re-run on the shipped Simulation Data reproduced scene Health exactly for 79 spawners, within 1% for 246 and within 5% for 552 of 706. An independent recomputation (GetReCalcOptimumTime, interpolation, Stat.CalculateSpecificValue) against Main.unity and Simulation Data.asset gives these results. All 79 exact HP matches are minute-mode (79 of 108). 0 of 608 day-mode spawners match exactly (median ratio 0.995, 10th-90th percentile 0.915-1.058). Even the 54 day-mode spawners whose re-calc time is exactly a stored chunk index give 0 exact HP; there the interpolation returns the stored snapshot (t=0 or t=1, up to rounding), so trimming cannot explain the gap. Day-mode scene values were baked from a different simulation run or Simulation Data version. Overall within 5%: HP 555/716.

**由数据反推的生成规则**（精确复现，详见 [Generation/BakeCheck.md](../Generation/BakeCheck.md)）：

```text
Python port: tufu_balance.model.bake (float32 where the C# uses float). Day-mode "day" = TimeChunkIndex = floor(TotalPlaytime/1440).
```

吻合情况：Minute mode (108): HP exact 79, within 1% 82, within 5% 88. Day mode (609): HP exact 1 (BossTower inf), but 90 of 92 non-trivial (bake time, chosen armor type) groups share one scene/model factor -> formula structure holds, player state used for the final bake differs from shipped Simulation Data. Stored SpawnerBakeInfos are an older bake log, not the source of scene values.

**配置参数**：`EnemySpawnerConfig.AverageFightTime`；`EnemySpawnerConfig.Count`；`EnemySpawnerConfig.ArmorConfig`；`EnemySpawnerConfig.OptimumTime`；`EnemySpawnerConfig.RelativeOptimumTime`；`EnemySpawnerConfig.IsDays`；`EnemySpawnerConfig.LocationNumber`；`Simulation Config.EarlySpawnerOptimumTimeOffsetStartLocation/RelativePart/Multiplier`；`Simulation Config.TimeChunkSnapshots`；`Combat Balance Config.IslandTargetDays`；`Combat Balance Config.Interpolations (AbilityDPS)`

**使用方**：simulation.bake_apply；Enemy runtime HP (x abMult at spawn)；[Crystals/CrystalObjectiveBalance.cs:25](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Crystals/CrystalObjectiveBalance.cs#L25) (CalculateBakedSpawnerHealth for crystal objective units; replaced by 10^k*1000 when objectiveKillCount > 8, line 27)

**备注**：The design intent is that, at its optimum time, the player kills the group in AverageFightTime using their best weapon type against the enemy's weakest resist. GlobalAttack is not included. The report method is runtime-callable, but writing the result into spawners was an editor step (see simulation.bake_apply). CalculateBakedSpawnerHealth(spawner, day) exposes it with an explicit day and no early shift.

**依赖图节点**：输入 `value:spawner.bake_optimum_time`、`value:sim.snapshot_at_time`、`config:EnemySpawnerConfig.IsDays`、`config:SimulationConfig.TimeChunkSnapshots`、`constant:SimulationConfig.minutes_per_day_minute_mode`、`value:sim.balance_coefficient[PlayerAbilityDPSMult]`、`config:EnemySpawnerConfig.ArmorConfig`、`constant:SimulationConfig.bake_resist_clamp`、`constant:Stat.crit_chance_curve`、`constant:Stat.crit_damage_curve`、`config:EnemySpawnerConfig.AverageFightTime`、`config:EnemySpawnerConfig.Count`；输出 `value:sim.bake_new_health`、`config:EnemySpawnerConfig.Stats[Health]`

**核验记录**（已写入上方字段）：

- 采纳 `status_evidence`（来自 `simulation.bake_spawner_health`）：status_evidence now carries the corrected recomputation statistics and the conclusion that day-mode scene values came from a different run. The code-side claim was checked: SimulationData.cs:26-35 returns ordered[0] at the first index and otherwise interpolates with t=0/1 at stored indices, which reproduces the stored values. The original within-1% count is kept as an earlier-run figure.
- 合并复核修正：(a) The member's within-1% statistic (246 spawners) was dropped when the status_evidence correction was folded in. (b) The used_by consumer reference is off: CalculateBakedSpawnerHealth is called at [Crystals/CrystalObjectiveBalance.cs:25](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Crystals/CrystalObjectiveBalance.cs#L25), not :27. Line 27 is the objectiveKillCount > 8 override, which replaces the baked health with 10^k*1000. All other fields were verified against SimulationConfig.cs:103-160 and 219-239.

### `simulation.bake_spawner_damage`

**Baked spawner Damage**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`editor_generator`
- 来源：合并核验
- 原生地址：`0x13db16c`

```text
snap/day as in the health bake (bakeTime = GetReCalcOptimumTime(spawner); snap = IsDays ? GetByTimeChunkIndex(bakeTime, config.TimeChunkSnapshots if non-empty else SimulationData.TimeChunkSnapshots) : GetByPlayTime(bakeTime); day = IsDays ? bakeTime : bakeTime/60). D = Difficulty <= 0 ? 1 : Difficulty; n = min(4f, (float)(Count+1)/2f) (float division, not an integer attacker count: Count=2 gives 1.5, Count=4 gives 2.5); BR = (D+1)*HealthRegen + (2D+3)*LifestealFlat (snapshot TotalValues); needDpsUnit = (BR + HP_player*D/AverageFightTime) / n; perHit = needDpsUnit * (AttackCooldown/(1 - ev)) / (1 - bc*ba); solve a: ApplyDamageDefense(a, Def[PreferredWeaponType(L)]) * (1/(1 + K(5,day))) >= perHit, starting at upper = 1 and doubling until satisfied, then bisecting (lower=0) until upper - lower <= 1 and returning upper; NewDamage = Math.Round(max(10, a)/10)*10 (banker's rounding). ApplyDamageDefense(a,d) = a <= 0 ? 0 : exp(-0.75*d/a)*a*max(0, 1 - 0.25*d/a).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `HP_player` | simulated player Health TotalValue | Simulation Data snapshot |
| `Difficulty` | fraction of player HP lost per fight (0.23, 0.3, 0.7, 1 most common) | EnemySpawnerConfig.Difficulty |
| `AttackCooldown` | enemy attack cooldown (Stats Type 5 BaseValue) | EnemySpawnerConfig.Stats |
| `AverageFightTime` | designed seconds per fight | EnemySpawnerConfig.AverageFightTime |
| `ev, bc, ba` | player specific Evasion, BlockChance, BlockAmount | snapshot TotalValue via Stat.CalculateSpecificValue |
| `Def` | player defense vs the location's preferred weapon | snapshot DefenseData, Locations Config |
| `K(5,day)` | ability EHP curve (PlayerAblityEHPMult) | simulation.ability_multipliers |
| `snap, day` | bake snapshot and day chosen from the re-calculated optimum time | SimulationConfig.GetReCalcOptimumTime / GetSnapshotForBake (simulation.snapshot_lookup_playtime, SimulationData.GetByTimeChunkIndex) |

**输出**：per-unit Damage written to EnemySpawnerConfig.Stats[Damage].BaseValue (SpawnerBakeReportData.NewDamage)

**代码**：[Simulation/SimulationConfig.cs:113-160](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L113)；[Simulation/SimulationConfig.cs:219-239](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L219)；[Services/Damage/DamageCalculator.cs:24-62](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L24)；[Unit/Stat.cs:61](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L61)

**状态依据**：Same native method BuildSpawnerBakeReport (0x13db16c). Recomputed damage matches the scene exactly for 118 of 706 spawners and within 5% for 504. A later recomputation on 716 spawners gives 124 exact and 511 within 5%; day-mode spawners mostly differ because of a different simulation run.

**由数据反推的生成规则**（精确复现，详见 [Generation/BakeCheck.md](../Generation/BakeCheck.md)）：

```text
Python port: tufu_balance.model.bake / model.defense (inverse of the flat defence curve, round to 10, floor 10).
```

吻合情况：Minute mode Damage exact 104/108; day mode exact 20/609 (all at day 4-5), within 1% 177/609. Which defence type (PreferredWeaponType) is used cannot be tested: defence is 0 in all early snapshots.

**配置参数**：`EnemySpawnerConfig.Difficulty`；`EnemySpawnerConfig.Count`；`EnemySpawnerConfig.Stats[AttackCooldown]`；`EnemySpawnerConfig.AverageFightTime`；`EnemySpawnerConfig.OptimumTime`；`EnemySpawnerConfig.RelativeOptimumTime`；`EnemySpawnerConfig.IsDays`；`EnemySpawnerConfig.LocationNumber`；`Simulation Config.EarlySpawnerOptimumTimeOffsetStartLocation/RelativePart/Multiplier`；`Simulation Config.TimeChunkSnapshots`；`Combat Balance Config.IslandTargetDays`；`Locations Config.Locations[].PreferredWeaponType`；`Combat Balance Config.Interpolations (AbilityEHP)`

**使用方**：simulation.bake_apply

**备注**：Design intent: over AverageFightTime the group drains Difficulty x player max HP on top of the player's battle regen. At most 4 units attack at once (SimulationConfig.MaxSimultaneousAttackers=4), but the attacker factor is the fractional (Count+1)/2. The solver returns the upper bound, so the raw value overshoots the exact solution by up to 1 before rounding to a multiple of 10 (minimum 10).

**依赖图节点**：输入 `value:spawner.bake_optimum_time`、`value:sim.snapshot_at_time`、`config:EnemySpawnerConfig.IsDays`、`config:SimulationConfig.TimeChunkSnapshots`、`constant:SimulationConfig.minutes_per_day_minute_mode`、`config:EnemySpawnerConfig.Difficulty`、`constant:SimulationConfig.bake_default_difficulty`、`config:EnemySpawnerConfig.Count`、`constant:SimulationConfig.MaxSimultaneousAttackers`、`constant:SimulationConfig.attacker_factor_divisor`、`constant:DamageCalculator.battle_regen_coefficients`、`config:EnemySpawnerConfig.AverageFightTime`、`config:EnemySpawnerConfig.Stats[AttackCooldown]`、`constant:Stat.evasion_curve`、`constant:Stat.block_chance_curve`、`constant:Stat.block_amount_curve`、`config:EnemySpawnerConfig.LocationNumber`、`config:LocationsConfig.Locations[].Number`、`config:LocationsConfig.Locations[].PreferredWeaponType`、`value:sim.balance_coefficient[PlayerAblityEHPMult]`、`constant:DamageCalculator.flat_defense_exp_coef`、`constant:DamageCalculator.flat_defense_linear_coef`、`constant:DamageCalculator.solve_attack_tolerance`、`constant:SimulationConfig.bake_damage_rounding_step`；输出 `value:sim.bake_new_damage`、`config:EnemySpawnerConfig.Stats[Damage]`

**核验记录**（已写入上方字段）：

- 采纳 `expression`（来自 `simulation.bake_spawner_damage`）：Verified at SimulationConfig.cs:139 (float division) and DamageCalculator.cs:31-52 (doubling from 1, bisection, returns upper once upper-lower<=1). The expression now states float n, the >= solve condition, the upper-bound return and banker's rounding.
- 合并复核修正：(a) The expression says 'snap/day as in the health bake', so the damage bake depends on the same snapshot-selection levers: GetReCalcOptimumTime and GetSnapshotForBake at SimulationConfig.cs:105, 118-119 and 219-239. The location lookup at line 129 also needs LocationNumber. config_params omits RelativeOptimumTime, LocationNumber, the EarlySpawnerOptimumTimeOffset* params, Simulation Config.TimeChunkSnapshots and IslandTargetDays, so the config-to-formula dependency graph would miss these edges. Variables also omit the snapshot selection. The expression was verified exactly against SimulationConfig.cs:139-159 and DamageCalculator.cs:24-62.

### `simulation.bake_apply`

**Rebake / apply balance to scene spawners**

- 状态：**编辑器工具缺失**（`editor_only_missing`）
- 类型：`editor_generator`
- 来源：子系统梳理

```text
For each non-hand-balanced spawner: info = {Key, OptimumTime, PrevHealth=current HP, PrevDamage=current Damage, Health=NewHealth, Damage=NewDamage}; append to SpawnerBakeInfos; write Stats[Health], Stats[Damage]
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `HandBalance` | presumed opt-out flag (2 spawners) | EnemySpawnerConfig.HandBalance |

**输出**：Main.unity spawner Health/Damage, Simulation Config.SpawnerBakeInfos (712 rows)

**代码**：[Simulation/SimulationConfig.cs:79-80](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L79)；[Simulation/SimulationConfig.cs:75](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L75)；[Simulation/SpawnerBakeInfo.cs:6-19](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SpawnerBakeInfo.cs#L6)；[Simulation/SpawnerBakeReportData.cs:8-115](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SpawnerBakeReportData.cs#L8)

**状态依据**：canRebake/canApplyBalance return false and no rebake method body exists. SpawnerBakeInfos holds 712 PrevHealth→Health rows. All damages are multiples of 10 (min 10) and health is an integer, matching the bake rounding.

**配置参数**：`Simulation Config.SpawnerBakeInfos`；`EnemySpawnerConfig.HandBalance`

**备注**：The stored SpawnerBakeInfos are an OLDER bake: only 5 of 712 scene spawners equal their logged Health/Damage, with a median deviation of 24% for HP and 49% for damage. Late-game scene values are up to 15–30× higher than the log. Use the scene plus Simulation Data as the source of truth.

**依赖图节点**：输入 `value:sim.bake_new_health`、`value:sim.bake_new_damage`、`config:EnemySpawnerConfig.HandBalance`、`config:EnemySpawnerConfig.OptimumTime`；输出 `config:EnemySpawnerConfig.Stats[Health]`、`config:EnemySpawnerConfig.Stats[Damage]`、`config:SimulationConfig.SpawnerBakeInfos[].Health`、`config:SimulationConfig.SpawnerBakeInfos[].Damage`、`config:SimulationConfig.SpawnerBakeInfos[].OptimumTime`

### `simulation.player_progression_tick`

**Bot progression loop (what generated Simulation Data)**

- 状态：**空壳**（`stub`）
- 类型：`simulation`
- 来源：子系统梳理
- 原生地址：`0x13e276c (OnUpdate only)`

```text
Inferred: each Tick (TimeStep=300 s, SimulationSpeed=9 ticks/frame): rebuild SpawnerData; best = argmax priority (simulation.spawner_priority); simulate fight: Playtime += moveTime + fightTime (+regen); Value_k += SUM StatRewards_k * Count (devour growth); roll item drops with emission curves, merge/level items (ProcessInventory, AutoBalanceItems); unlock location when boss/gates cleared; snapshot every SnapshotInterval (300 s) of play. From day FastTickStartChunkOffset (4) on, FastTick processes one day per chunk: per spawner fights = min(charges, ...) and rewards accumulated then flushed; one TimeChunkSnapshot per day.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `TimeStep, SnapshotInterval` | 300 s | Simulation Config |
| `UseFastTick, FastTickStartChunkOffset` | 1, 4 days | Simulation Config |

**输出**：Simulation Data.Snapshots / TimeChunkSnapshots (player power vs time)

**代码**：[Simulation/Systems/SimulationSystem.cs:291-317](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L291)；[Simulation/Systems/SimulationSystem.cs:497-511](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L497)；[Simulation/Systems/SimulationSystem.cs:546-606](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L546)；[Simulation/Systems/SimulationSystem.cs:676-680](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L676)；[Simulation/Systems/SimulationSystem.cs:693-750](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L693)；[Simulation/Systems/SimulationSystem.cs:795-824](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L795)；[Simulation/SimulationConfig.cs:82-96](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L82)

**状态依据**：OnUpdate is recovered, but ExecuteTick, PrepareSimulationRun, Tick, FastTick, FindBestSpawner, SimulateFightWithSpawner(Fast), ProcessInventory, ProcessDeath, ApplyRegen, UpgradeCrystalsGreedy, ProcessCrystalSimulationChunk, RollFractionalCount, GetChainedItemBaseCount, GetSpawnerItemRewardCopies, GetItemEmissionCoefficient, CalcDifficultSpawner, MakeSnapshot and the auto-balance methods are all empty.

**配置参数**：`Simulation Config.TimeStep`；`Simulation Config.SnapshotInterval`；`Simulation Config.SimulationSpeed`；`Simulation Config.UseFastTick`；`Simulation Config.FastTickStartChunkOffset`；`Simulation Config.AutoBalanceItems`；`Simulation Config.IsSmartPath`；`Simulation Config.IsDynamicResourcePriority`；`Simulation Config.StopAtLocation/TargetLocationNumber/StopAtDay/TargetDay`

**使用方**：simulation.snapshot_growth_curve

**备注**：Only its output survives. The bot's item choices and pathing are unknown, so Simulation Data cannot be regenerated for a changed economy. A replacement model would have to re-implement this loop using the recovered fight model and RPM formulas as building blocks.

**依赖图节点**：输入 `config:SimulationConfig.TimeStep`、`config:SimulationConfig.SnapshotInterval`、`config:SimulationConfig.UseFastTick`、`config:SimulationConfig.FastTickStartChunkOffset`、`config:SimulationConfig.AutoBalanceItems`、`config:SimulationConfig.IsSmartPath`、`config:SimulationConfig.IsDynamicResourcePriority`、`config:SimulationConfig.StopAtLocation`、`config:SimulationConfig.TargetLocationNumber`、`config:SimulationConfig.StopAtDay`、`config:SimulationConfig.TargetDay`、`config:SimulationConfig.SpawnerChargesChunkMinutes`、`config:SimulationConfig.UseSpawnerCharges`、`config:SimulationConfig.IsCapOptimumTime`、`config:EnemySpawnerConfig.OptimumTime`、`config:EnemySpawnerConfig.IsDays`、`config:EnemySpawnerConfig.StatRewards[].Value`、`config:EnemySpawnerConfig.Count`、`config:EnemySpawnerConfig.LocationNumber`、`value:sim.spawner_priority`、`value:sim.spawner_fight_time`、`value:sim.spawner_farmed_time`、`value:sim.spawner_health_delta`、`value:sim.spawner_eligible`、`value:spawner.charges_limit_per_day`、`value:spawner.optimum_timing_chunks`、`value:sim.item_copies_emitted`、`value:sim.bot_current_hp`、`value:sim.ads_stat_income`、`value:sim.amulet_multipliers`、`value:sim.upgrade_purchase_hints`、`value:sim.total_playtime`、`value:sim.active_playtime`、`state:sim_bot_state`；输出 `value:sim.snapshot_stat_value`、`value:sim.snapshot_progress`、`value:sim.dps_by_damage_type`、`value:sim.defense_by_damage_type`、`value:sim.active_playtime`

### `simulation.lifecycle_time_model`

**Session / AFK time model**

- 状态：**空壳**（`stub`）
- 类型：`simulation`
- 来源：子系统梳理

```text
Inferred from data: LifeCycleInMinutes=[10,230] -> play 10 active minutes, then 230 AFK minutes (ProcessAFK) per cycle => 6 cycles / 1440 min day = 60 active min/day (days 0-3: Playtime 63, 124, 186, 248 at day boundaries). TotalPlaytime = Playtime + AFK; TimeChunkIndex = floor(TotalPlaytime/1440). Ads every AdsIntervalMinutes(6) grant AdsRewardMultiplierInMinutes(0) minutes of RPM.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `LifeCycleInMinutes` | [active, idle] minutes | Simulation Config.LifeCycleInMinutes |
| `AdsIntervalMinutes, AdsRewardMultiplierInMinutes` | simulated ads (disabled: 0 minutes) | Simulation Config |

**输出**：Playtime/TotalPlaytime progression

**代码**：[Simulation/Systems/SimulationSystem.cs:264-265](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L264)；[Simulation/Systems/SimulationSystem.cs:556-570](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L556)

**状态依据**：ProcessAFK, NextLifeCycle, CheckLifeCycle and ProcessSimulateAds are empty. In the Snapshots list, TotalPlaytime − Playtime jumps by exactly 230 after each ~10-minute block.

**配置参数**：`Simulation Config.LifeCycleInMinutes`；`Simulation Config.AdsIntervalMinutes`；`Simulation Config.AdsRewardMultiplierInMinutes`；`Simulation Config.SpawnerChargesChunkMinutes`

**使用方**：simulation.balance_timing；simulation.rpm_time_chunk (/60 divisor)

**备注**：In fast-tick days (4+), active minutes per day vary between about 11 and 60, averaging 46/day overall (18,699 min over 405 days).

**依赖图节点**：输入 `config:SimulationConfig.LifeCycleInMinutes`、`value:sim.spawner_farmed_time`、`state:sim_bot_state`；输出 `value:sim.active_playtime`、`value:sim.total_playtime`

### `simulation.spawner_priority`

**Bot spawner priority**

- 状态：**空壳**（`stub`）
- 类型：`simulation`
- 来源：子系统梳理

```text
Unknown body; inputs available: playerRPS, rewardValue, moveTime(position), PriorityMultiplier, static vs dynamic (IsDynamicResourcePriority) priority per stat
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `PriorityMultiplier` | designer weight (1 for 449 spawners; 3, 5, 7, 10, 20 for others) | EnemySpawnerConfig.PriorityMultiplier |

**输出**：priority score

**代码**：[Simulation/Systems/SpawnerData.cs:44-67](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SpawnerData.cs#L44)；[Simulation/Systems/SimulationSystem.cs:697-733](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L697)；[Simulation/Systems/SimulationSystem.cs:789-793](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L789)

**状态依据**：GetMoveTime, GetPriority, GetStaticPriority, GetDynamicPriority, GetFightEfficiency, CalculateFarmPotencial and TryGetSpawnerPriority all return 0/false.

**配置参数**：`EnemySpawnerConfig.PriorityMultiplier`；`Simulation Config.IsDynamicResourcePriority`；`Simulation Config.IsSmartPath`

**使用方**：simulation.player_progression_tick

**依赖图节点**：输入 `config:EnemySpawnerConfig.PriorityMultiplier`、`config:SimulationConfig.IsDynamicResourcePriority`、`config:SimulationConfig.IsSmartPath`、`value:sim.spawner_player_rps`、`value:sim.spawner_reward_value`、`value:sim.stat_benefit_weights`、`value:sim.spawner_farmed_time`、`state:sim_bot_state`；输出 `value:sim.spawner_priority`

### `simulation.difficulty_map`

**Spawner difficulty classification**

- 状态：**空壳**（`stub`）
- 类型：`simulation`
- 来源：子系统梳理

```text
Inferred: d = CalcDifficultSpawner(spawner, secondsRegen) (probably health lost / player HP); color = first DifficultyThreshold with d <= Threshold among [0.05, 0.3, 0.95, 1, 4, 10]
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Threshold` | difficulty band edges | Simulation Config.DifficultyThresholds |

**输出**：heatmap color (debug)

**代码**：[Simulation/Systems/SimulationSystem.cs:748-751](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L748)；[Simulation/DifficultyThreshold.cs:7-13](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/DifficultyThreshold.cs#L7)

**状态依据**：CalcDifficultSpawner returns 0. Only the thresholds survive.

**配置参数**：`Simulation Config.DifficultyThresholds`；`Simulation Config.ShowDifficultyMap`

**备注**：This is a visualization only. The bands do suggest the design reads 'fraction of HP lost': below 0.3 is easy, about 1 is the limit, above 1 is impossible.

**依赖图节点**：输入 `config:SimulationConfig.DifficultyThresholds[].Threshold`、`value:sim.spawner_health_delta`、`value:player.stat_raw[Health]`、`value:player.stat_specific[HealthRegen]`；输出 `value:sim.spawner_difficulty_class`

### `simulation.amulet_sim_multipliers`

**Amulet soul emission & spawner-charge multipliers in simulation**

- 状态：**编辑器工具缺失**（`editor_only_missing`）
- 类型：`simulation`
- 来源：子系统梳理

```text
Unknown; structure: soulEmissionPoints[soulKey] = list of (day, amount) -> GetSoulEmission(key, day) (piecewise); spawnerChargeBonusPoints[rarity] = (day, bonus) -> GetSpawnerChargeMultiplier(spawner, day) = 1 + bonus(rarity, day) presumably; amulet auto-balance per level stores Experience and SoulRewards
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `AmuletLevelBalance` | per-level experience/souls from sim | Simulation Config.AmuletLevelBalance (7 entries, all zero/empty) |

**输出**：soul income, charge multipliers, amulet level costs

**代码**：[Simulation/Systems/AmuletSimulationMultipliers.cs:23-44](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/AmuletSimulationMultipliers.cs#L23)；[Simulation/Systems/SimulationSystem.cs:600-606](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L600)；[Simulation/SimulationConfig.cs:90-96](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L90)；[Amulet/AmuletConfig.cs:53-55](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletConfig.cs#L53)

**状态依据**：Every method body is empty (constructor, Build, GetSoulEmission, GetSpawnerChargeMultiplier, ResetAmuletAutoBalance, ProcessAmuletAutoBalanceTargets, Set/ResetAmuletLevelBalance, AmuletConfig.BakeAmuletAutoBalance). The shipped AmuletLevelBalance data is all zeros.

**配置参数**：`Simulation Config.AutoBalanceAmulet`；`Simulation Config.AmuletLevelBalance`；`Amulet Config`；`Amulet Upgrade Config`

**使用方**：Amulet Config (baked RequiredExperiencePerLevel etc.)

**备注**：Auto-balance-cost soul keys are {common, large, elite} (SimulationSystem.cs:203).

**依赖图节点**：输入 `config:SimulationConfig.AutoBalanceAmulet`、`config:AmuletConfig.AutoBalanceCostMultiplier`、`config:AmuletConfig.AutoBalanceExperienceMultipliers`、`config:AmuletConfig.LevelTargetDays`、`config:AmuletConfig.MaxLevel`、`config:AmuletConfig.SoulDefs[].BaseExperience`、`config:AmuletUpgradeConfig.Upgrades[].RequiredAmuletLevel`、`config:ItemConfig.Rarity`、`value:sim.upgrade_purchase_hints`、`value:sim.balance_timing`、`value:sim.snapshot_progress`；输出 `value:sim.amulet_multipliers`、`config:SimulationConfig.AmuletLevelBalance[].Experience`、`config:SimulationConfig.AmuletLevelBalance[].SoulRewards.valueData`、`config:AmuletConfig.RequiredExperiencePerLevel`

### `simulation.snapshot_growth_curve`

**Simulated player power vs day (baked balance target)**

- 状态：**纯数据**（`data_only`）
- 类型：`lookup_table`
- 来源：合并核验

```text
P(day) table: Health TotalValue 100 (d0), 1.6e3 (d1), 9.1e3 (d3), 9.5e4 (d10), 4.2e5 (d21), 1.66e6 (d33), 5.8e6 (d51), 2.4e7 (d77), 9.5e7 (d115), 4.5e8 (d170), 1.24e9 (d250), 2.74e9 (d405). Max DpsData 9.2 -> 345 -> 1.7e3 -> 1.3e4 -> 6.5e4 -> 3.0e5 -> 1.3e6 -> 6.8e6 -> 2.5e7 -> 1.15e8 -> 3.8e8 -> 9.3e8. Daily growth: HP x1.45/day (d4-6), x1.12 (d20-33), x1.05 (d50-80), x1.03 (d100-155), x1.01 (d200-250), x1.004 (d400). Location reached: 17 at day 154 and through day 405; items plateau at 45 from day 187.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `P(day)` | snapshot stats/DPS/defense/items/location | Simulation Data.TimeChunkSnapshots |

**输出**：target power curve used by all bakes

**代码**：[Simulation/SimulationData.cs:9-12](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationData.cs#L9)；[Simulation/SimulationSnapshot.cs:7-24](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationSnapshot.cs#L7)

**状态依据**：Produced by the stubbed bot and stored as data. Stats present: keys 0-9, 12-17, 20-31, 33, 34. Items (Key, Level, Copies) grow from 1 to 45.

**配置参数**：`Simulation Data.Snapshots`；`Simulation Data.TimeChunkSnapshots`

**使用方**：simulation.bake_spawner_health；simulation.bake_spawner_damage；simulation.forecast_snapshot；[BossTower/BossSpawnService.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/BossSpawnService.cs):BakeStatsForFloor；[Crystals/CrystalObjectiveBalance.cs:28-31](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Crystals/CrystalObjectiveBalance.cs#L28)

**备注**：This is the most important single lever. Every spawner's HP and damage is linear in this curve at its OptimumTime. A 10% change in simulated DPS at day d changes the HP of every spawner baked at d by 10%. The bot reaches Location 17 at day 154 and stays there through day 405. Locations Config defines only 17 locations, so every spawner is on islands 1-17. Items plateau at 45 from day 187. Growth after day 154 therefore comes only from farming and the feature curves (K2/K3/ability), which explains the slowing daily growth.

**依赖图节点**：输入 `value:sim.snapshot_stat_value`、`value:sim.snapshot_progress`；输出 `value:sim.snapshot_stat_total`、`value:sim.dps_by_damage_type`、`value:sim.defense_by_damage_type`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `simulation.snapshot_growth_curve`）：The Location-17 plateau, the 17-location limit and the items plateau were added to notes (and summarized in the expression). These are data observations from TimeChunkSnapshots and Locations Config, and the code does not contradict them.

### `simulation.forecast_build_core`

**Forecast snapshot series & enemy growth**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x13f9820, 0x13f6610`

```text
timePoints_i = ForecastLocationNumber + BossSnapshotTimingStep*i for i < |EventBossMinutes|; day_i = ConvertIslandTimingToDays(timePoints_i); progress_i = i/(N-1); snap_i = snapshot(day_i) with Playtime := EventBossMinutes[i]; EnemyStatsMultiplier_i = max(0, 1 + EventBossMinutes[i]*EnemyStatsGrowthPerMinute); AbDps = 1 + K(4, day_i); AbEhp = 1 + K(5, day_i)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ForecastLocationNumber` | player's BossProgressLocation number at runtime | EventForecastService.cs:95 |
| `BossSnapshotTimingStep` | 0 at runtime | EventForecastService.cs:96 |
| `EventBossMinutes` | boss spawn minutes in the event | Crystal Event Roster Config.ForecastSettings.AutoBalanceInput.EventBossMinutes (0,10,25,...,700) |
| `EnemyStatsGrowthPerMinute` | 0.02 (+2%/min linear) | Crystal Event Roster Config.ForecastSettings.AutoBalanceInput |

**输出**：List<ForecastSnapshotExtended>

**代码**：[Simulation/Forecast/ForecastSnapshots.cs:35-66](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastSnapshots.cs#L35)；[Simulation/Forecast/ForecastSnapshots.cs:111-112](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastSnapshots.cs#L111)；[Simulation/Forecast/ForecastApi.cs:13-31](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastApi.cs#L13)；[Events/Services/EventForecastService.cs:85-121](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Events/Services/EventForecastService.cs#L85)

**状态依据**：Native VA comments on BuildCore and ForecastApi. BuildTimePoints/GetBossMinutes (line 111-112) carry no separate annotation.

**配置参数**：`Crystal Event Roster Config.ForecastSettings.AutoBalanceInput`；`Crystal Event Roster Config.ForecastSettings.MinutesAhead/StepMinutes/SnapshotsPerFrame`

**使用方**：simulation.forecast_auto_balanced_enemy；simulation.forecast_reward；Events crystal roster

**备注**：At runtime every boss uses the same snapshot (the start day of the player's island). Growth over the event comes only from EnemyStatsMultiplier. minutesAhead and stepMinutes are ignored by the reconstructed BuildCore. If no table is available, the fallback takes the current player's stats as constant (local addition, not original).

**依赖图节点**：输入 `config:CrystalEventRosterConfig.Leagues[].SnapshotIslandNumber`、`value:player.unlocked_location`、`constant:EventForecastService.crystal_boss_snapshot_timing_step`、`config:CrystalEventRosterConfig.ForecastSettings.AutoBalanceInput.EventBossMinutes`、`config:CrystalEventRosterConfig.BossMinutes`、`config:CrystalEventRosterConfig.ForecastSettings.AutoBalanceInput.EnemyStatsGrowthPerMinute`、`value:time.island_timing_days`、`value:sim.snapshot_at_time`、`value:sim.balance_coefficient[PlayerAbilityDPSMult]`、`value:sim.balance_coefficient[PlayerAblityEHPMult]`、`state:current_event`；输出 `value:forecast.snapshot`、`value:forecast.enemy_stats_multiplier`、`value:sim.ability_dps_multiplier`、`value:sim.ability_ehp_multiplier`

### `simulation.forecast_auto_balanced_enemy`

**Forecast auto-balanced event golem/boss/grand boss**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x13f7da0 (+local functions 0x13f87f0, 0x13f8934)`

```text
crit = S(CC)*(S(CD)-1)+1; atk = 1+0.01*S(GlobalAttack); dodge = S(Evasion); block = 1 - S(BC)*S(BA); gDef = 1/(1+0.01*S(GlobalDefence)); PlayerDps(w) = (1 - clamp(Resist_w,-0.9,0.9)) * DpsData[w] * crit * atk * AbDps for w in {Stab, Cut, Buck}; maxDps = max_w. For kind k with (T, D) and m = (k==Golem ? 1 : EnemyStatsMultiplier): Health = round(m * maxDps * T). target = HP_total*D/max(1e-4,T) * (1 - dodge)/block + (D+1)*HealthRegen + (2D+3)*LifestealFlat; a = Solve(ApplyDamageDefense(a, Def[EnemyAttackType]) * gDef/AbEhp = target); dmg = round(max(10,a)/10)*10; dmg = round(m*dmg/10)*10; if k != Golem: dmg = round(dmg / RampAvg(T)/10)*10; Damage = max(10, dmg)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Resist_w` | event enemy resists (0.5/1/0.5 in roster config; overridden by SetCombatTypes) | ForecastAutoBalanceInput.ResistStabbing/Cutting/Buckshot |
| `T, D` | fight seconds and difficulty per kind (Golem 8/0.2, Boss 20/1 tuned, GrandBoss 40/1) | ForecastEnemyPresets / simulation.forecast_tune_boss_presets |
| `EnemyAttackType` | defense type used | ForecastAutoBalanceInput.EnemyAttackType |

**输出**：ForecastAutoBalancedEnemy {Health, Damage}

**代码**：[Simulation/Forecast/ForecastSnapshots.cs:150-190](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastSnapshots.cs#L150)

**状态依据**：Native VA comment. The NaN/Inf guard at lines 175-177 is a local compatibility addition.

**配置参数**：`Crystal Event Roster Config.ForecastSettings.EnemyPresets`；`Crystal Event Roster Config.ForecastSettings.AutoBalanceInput`

**使用方**：simulation.forecast_event_plan；Events crystal event enemy stats

**备注**：This differs from the spawner bake in four ways: it uses ×(1−dodge) instead of a ÷(1−evasion) hit frequency, has no attack cooldown or unit count, includes GlobalAttack and GlobalDefence, and puts the player's battle regen outside the per-unit split.

**依赖图节点**：输入 `value:forecast.snapshot`、`state:current_event`、`constant:EventForecastService.event_weakness_resist`、`constant:ForecastSnapshots.resist_clamp`、`value:forecast.boss_preset`、`config:CrystalEventRosterConfig.ForecastSettings.EnemyPresets.Golem.FightTimeSeconds`、`config:CrystalEventRosterConfig.ForecastSettings.EnemyPresets.Golem.Difficulty`、`config:CrystalEventRosterConfig.ForecastSettings.EnemyPresets.GrandBoss.FightTimeSeconds`、`config:CrystalEventRosterConfig.ForecastSettings.EnemyPresets.GrandBoss.Difficulty`、`value:forecast.enemy_stats_multiplier`、`value:sim.ability_dps_multiplier`、`value:sim.ability_ehp_multiplier`、`value:forecast.damage_ramp_average`、`constant:ForecastSnapshots.battle_regen_regen_coef`、`constant:ForecastSnapshots.battle_regen_lifesteal_coef`、`constant:DamageCalculator.flat_defense_exp_coef`、`constant:DamageCalculator.flat_defense_linear_coef`、`constant:ForecastSnapshots.damage_round_step`、`constant:ForecastSnapshots.min_damage`、`constant:ForecastSnapshots.min_fight_time`；输出 `value:forecast.enemy_health`、`value:forecast.enemy_damage`

### `simulation.forecast_damage_ramp_average`

**Average event damage-ramp multiplier**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：合并核验

```text
GetAverageDamageRampMultiplier(T, delay, p): if (T <= 0 || p <= 0) return 1.0; d = max(0.0, delay); return d >= T ? 1.0 : 1.0 + (T - d) * (T - d) * p / (T + T)   // = mean over t in [0,T] of (1 + p*max(0, t - d)). Used in BuildEnemy (non-Golem only): damage = round((damage / max(0.0001, RampAvg(settings.FightTimeSeconds, input.DamageRampDelaySeconds, input.DamageRampPercentPerSecond))) / 10) * 10
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `T` | Enemy fight time in seconds (settings.FightTimeSeconds of the Boss/GrandBoss preset; Boss value comes from TunePresetsForBoss, 15..25 s; GrandBoss preset 40 s) | ForecastEnemySettings.FightTimeSeconds (Crystal Event Roster Config.ForecastSettings.EnemyPresets, tuned by TunePresetsForBoss) |
| `delay` | Ramp start delay, 5 s in the shipped config | ForecastAutoBalanceInput.DamageRampDelaySeconds |
| `p` | Ramp slope, 0.04 per second in the shipped config | ForecastAutoBalanceInput.DamageRampPercentPerSecond |
| `RampAvg` | Time-averaged damage multiplier over the fight; divides the forecast boss damage | computed |

**输出**：Divisor (>= 1) applied to the forecast Boss/GrandBoss damage so the ramped runtime damage averages to the balanced target

**代码**：[Simulation/Forecast/ForecastSnapshots.cs:191-196](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastSnapshots.cs#L191)；[Simulation/Forecast/ForecastSnapshots.cs:181-182](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastSnapshots.cs#L181)；[Services/Damage/DamageCalculator.cs:78-79](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L78)；[Enemy/EnemyUnit.cs:188](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L188)

**状态依据**：GetAverageDamageRampMultiplier (ForecastSnapshots.cs:191) has no native-address comment. Only its caller BuildAutoBalancedEnemies is annotated (Original 0x13f7da0 + local functions 0x13f87f0, 0x13f8934), and a caller's annotation does not count. The closed form matches the runtime ramp in DamageCalculator.cs:78-79: attack *= max(0, EventDamageRampTimer - 5) * 0.04 + 1 for IsEvent attackers.

**配置参数**：`Crystal Event Roster Config.ForecastSettings.AutoBalanceInput.DamageRampDelaySeconds (5)`；`Crystal Event Roster Config.ForecastSettings.AutoBalanceInput.DamageRampPercentPerSecond (0.04)`；`Crystal Event Roster Config.ForecastSettings.EnemyPresets.Boss.FightTimeSeconds (20; overridden by BossFightTimeSecondsMin/Max 15..25)`；`Crystal Event Roster Config.ForecastSettings.EnemyPresets.GrandBoss.FightTimeSeconds (40)`

**使用方**：simulation.forecast_auto_balanced_enemy

**备注**：Worked values with the shipped config (d=5, p=0.04): T=15 gives 1.1333; T=20 gives 1.225; T=25 gives 1.32; GrandBoss T=40 gives 1.6125. Golem damage is not divided (kind != EnemyGolem guard at ForecastSnapshots.cs:181). Related runtime entries, which mirror this formula but do not consume it: combat.event_enemy_damage_ramp (DamageCalculator.cs:78-79) and spawners.event_damage_ramp_timer (EnemyUnit.cs:188). The runtime ramp constants 5 s and 0.04 are hard-coded in DamageCalculator and are not read from AutoBalanceInput, so if the config values change, forecast and runtime drift apart. EnemyUnit.cs:188 advances EventDamageRampTimer only while the event enemy is below max health (CurrentHealth < Health - 0.01) and resets it to 0 otherwise, so the real ramp starts at first damage taken, not at spawn. The forecast assumes the ramp runs for the whole fight T.

**依赖图节点**：输入 `value:forecast.boss_preset`、`config:CrystalEventRosterConfig.ForecastSettings.EnemyPresets.GrandBoss.FightTimeSeconds`、`config:CrystalEventRosterConfig.ForecastSettings.AutoBalanceInput.DamageRampDelaySeconds`、`config:CrystalEventRosterConfig.ForecastSettings.AutoBalanceInput.DamageRampPercentPerSecond`；输出 `value:forecast.damage_ramp_average`

**核验记录**（已写入上方字段）：

- 采纳 `code_refs`（来自 `simulation.forecast_damage_ramp_average`）：Checked: DamageCalculator.cs line 78 is the IsEvent check and line 79 is the ramp multiply. code_refs now cite [Services/Damage/DamageCalculator.cs:78-79](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L78).
- 合并复核修正：used_by lists combat.event_enemy_damage_ramp and spawners.event_damage_ramp_timer, but neither consumes this forecast divisor. They are the runtime ramp that the closed form averages: DamageCalculator.cs:78-79 hard-codes 5 s and 0.04, and EnemyUnit.cs:188 drives the timer. Neither reads GetAverageDamageRampMultiplier or its output. In the dependency graph these would become false formula-to-consumer edges. The only real consumer is BuildEnemy in BuildAutoBalancedEnemies (ForecastSnapshots.cs:182), which is simulation.forecast_auto_balanced_enemy. The fix moves the two runtime ids into notes as related, not dependent, entries. I checked the expression, the worked values (1.1333, 1.225, 1.32, 1.6125), the status, the config values (asset lines 30-31, 60-61, 70, 73) and the resolution of the pending code_refs correction against the code. All are correct.

### `simulation.forecast_tune_boss_presets`

**Boss fight-time/difficulty ramp across event**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
T_boss = BossFightTimeSecondsMin + clamp01(progress)*(Max - Min); D_boss = BossDifficultyMin + clamp01(i/4)*(Max - Min); applied only if > 0
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `progress` | i/(N-1) | simulation.forecast_build_core |
| `Min/Max` | 15..25 s, 0.6..1.0 | Crystal Event Roster Config.ForecastSettings.AutoBalanceInput |

**输出**：tuned Boss preset

**代码**：[Simulation/Forecast/ForecastSnapshots.cs:129-136](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastSnapshots.cs#L129)

**状态依据**：No individual native annotation (helper of the native BuildCore).

**配置参数**：`AutoBalanceInput.BossFightTimeSecondsMin/Max`；`AutoBalanceInput.BossDifficultyMin/Max`

**使用方**：simulation.forecast_auto_balanced_enemy

**备注**：Difficulty saturates at the 5th boss.

**依赖图节点**：输入 `value:forecast.snapshot`、`config:CrystalEventRosterConfig.ForecastSettings.AutoBalanceInput.BossFightTimeSecondsMin`、`config:CrystalEventRosterConfig.ForecastSettings.AutoBalanceInput.BossFightTimeSecondsMax`、`config:CrystalEventRosterConfig.ForecastSettings.AutoBalanceInput.BossDifficultyMin`、`config:CrystalEventRosterConfig.ForecastSettings.AutoBalanceInput.BossDifficultyMax`、`constant:ForecastSnapshots.boss_difficulty_ramp_bosses`、`config:CrystalEventRosterConfig.ForecastSettings.EnemyPresets.Boss.FightTimeSeconds`、`config:CrystalEventRosterConfig.ForecastSettings.EnemyPresets.Boss.Difficulty`；输出 `value:forecast.boss_preset`

### `simulation.forecast_reward`

**Forecast boss stat reward**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
minutes = (RewardMinutesMax<=0 && RewardMinutesMin<=0) ? RewardMinutes : Min + clamp01(progress)*(Max - Min); type = random among allowed stats with CurrentGpm>0; value = floor(CurrentGpm[type] * max(0, minutes))
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `CurrentGpm` | snapshot RPM per stat | simulation.rpm_time_chunk via snapshot |
| `RewardMinutesMin/Max` | 5..15 minutes | Crystal Event Roster Config.ForecastSettings.AutoBalanceInput |
| `allowed` | Health, HealthRegen, LifestealFlat, Cut/Stab/Buckshot DamageFlat, Cut/Stab/Buckshot DefenceFlat | ForecastSettings.RewardAllowedStats |

**输出**：(HasReward, RewardStatType, RewardMinutes, RewardValue)

**代码**：[Simulation/Forecast/ForecastSnapshots.cs:197-208](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastSnapshots.cs#L197)；[Events/Services/EventForecastService.cs:99-120](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Events/Services/EventForecastService.cs#L99)

**状态依据**：No individual annotation.

**配置参数**：`AutoBalanceInput.RewardMinutes`；`AutoBalanceInput.RewardMinutesMin`；`AutoBalanceInput.RewardMinutesMax`；`ForecastSettings.RewardAllowedStats`

**使用方**：simulation.forecast_event_plan；EventForecastService baked completion rewards (reward.Value × GPM of last snapshot)

**备注**：Rewards are denominated in 'minutes of farming'.

**依赖图节点**：输入 `value:forecast.snapshot`、`config:CrystalEventRosterConfig.ForecastSettings.AutoBalanceInput.RewardMinutes`、`config:CrystalEventRosterConfig.ForecastSettings.AutoBalanceInput.RewardMinutesMin`、`config:CrystalEventRosterConfig.ForecastSettings.AutoBalanceInput.RewardMinutesMax`、`config:CrystalEventRosterConfig.ForecastSettings.RewardAllowedStats`、`state:rng`；输出 `value:forecast.stat_reward`

### `simulation.forecast_event_plan`

**Event plan assembly**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`lookup_table`
- 来源：合并核验
- 原生地址：`0x13f5ee0`

```text
BuildEventPlan(snapshots, input): for i in 0..input.EventBossMinutes.Count-1: Bosses[i] = { Enemy = ToEnemyInfo(minute = input.EventBossMinutes[i], kind = EnemyBoss, snapshots[i].AutoBalanceInput, FindEnemyOrDefault(snapshots[i], EnemyBoss)), Reward = {HasReward, RewardStatType, RewardMinutes, RewardValue} of snapshots[i] }; Golem = ToEnemyInfo(0, EnemyGolem, snapshots[0].AutoBalanceInput, FindEnemyOrDefault(snapshots[0], EnemyGolem)); GrandBoss = ToEnemyInfo(max(0, input.EventEndMinute), EnemyGrandBoss, snapshots[last].AutoBalanceInput, FindEnemyOrDefault(snapshots[last], EnemyGrandBoss)); ToEnemyInfo copies Health and Damage from the enemy and AttackType = EnemyAttackType, ResistStabbing/Cutting/Buckshot from that snapshot's AutoBalanceInput. FindEnemyOrDefault returns default (Health = 0, Damage = 0) if Enemies is null or has no enemy of that kind. Crystal-event caller (EventForecastService.BuildEventPlanAsync): before the build, input.EventBossMinutes = roster.BossMinutes; EventLastBossMinute = BossMinutes[last]; EventEndMinute = max(EventEndMinute, EventLastBossMinute); BossSnapshotTimingStep = 0.25. After the build, plan.Golem.Health and Damage are replaced by the Golem of the snapshot whose time point (ForecastLocationNumber + 0.25*i) is nearest the player's BossProgressLocation.Number.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `EventBossMinutes` | Per-boss event minute. Its count also sets the number of snapshots (BuildTimePoints). | ForecastAutoBalanceInput.EventBossMinutes; for crystal events overwritten by CrystalEventRosterConfig.BossMinutes |
| `EventEndMinute` | Grand boss minute (asset value 200; for crystal events raised to max(200, last roster boss minute) = 1000) | ForecastAutoBalanceInput.EventEndMinute |
| `snapshots[i]` | ForecastSnapshotExtended i (auto-balanced Golem/Boss/GrandBoss, reward data, AutoBalanceInput copy) | ForecastSnapshots.BuildCore |
| `EnemyAttackType, ResistStabbing/Cutting/Buckshot` | Combat types copied into every enemy info. Set by EventForecastService.SetCombatTypes from the event's WeaponType and ResistType: the resisted weapon type gets -0.9, the others 0. | ForecastAutoBalanceInput |

**输出**：ForecastEventPlan {Golem, Bosses[], GrandBoss} (plus LeagueIndex, CrystalStages and CrystalMilestones for crystal events), serialized to JSON by EventForecastService

**代码**：[Simulation/Forecast/ForecastSnapshots.cs:114-128](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastSnapshots.cs#L114)；[Simulation/Forecast/ForecastSnapshots.cs:137-147](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastSnapshots.cs#L137)；[Simulation/Forecast/ForecastSnapshots.cs:111-112](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastSnapshots.cs#L111)；[Simulation/Forecast/ForecastApi.cs:30](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastApi.cs#L30)；[Events/Services/EventForecastService.cs:154-181](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Events/Services/EventForecastService.cs#L154)；[Simulation/Forecast/ForecastHelper.cs:101-103](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastHelper.cs#L101)

**状态依据**：BuildEventPlan carries the comment "// Original ARM64 ELF VA 0x13f5ee0 and tiny ToEnemyInfo/ToBossInfo bodies." (ForecastSnapshots.cs:114). The crystal-event post-processing in EventForecastService.BuildEventPlanAsync is annotated Original 0x14e6d34; it is listed here as a consumer, not as this formula's address.

**配置参数**：`Crystal Event Roster Config.ForecastSettings.AutoBalanceInput.EventBossMinutes (20 entries: 0,10,25,40,55,60,80,100,120,135,150,165,180,195,210,300,400,500,600,700; overwritten for crystal events)`；`Crystal Event Roster Config.BossMinutes (20 entries: 10,20,30,40,60,60,80,100,120,150,200,250,300,350,450,500,600,700,800,1000)`；`Crystal Event Roster Config.ForecastSettings.AutoBalanceInput.EventEndMinute (200)`；`AutoBalanceInput.EnemyAttackType / ResistStabbing / ResistCutting / ResistBuckshot (overwritten by SetCombatTypes)`

**使用方**：Events crystal roster (EventForecastService.BuildEventPlanAsync -> EventsRepository)；spawners.event_plan_override；meta.event_plan_stat_reward

**备注**：Upstream input: simulation.forecast_build_core produces the snapshots this plan reads (ForecastApi.cs:30); it is not a consumer. EventBossCount (10), EventBossGamma (1) and RandomBossResists (0) are not read by BuildEventPlan or BuildCore; they are only declared in ForecastAutoBalanceInput. EventLastBossMinute is written by EventForecastService but not read here. ForecastHelper.SetEventSettings(grandBossMinute, lastBossMinute, bossCount, bossGamma) is an empty stub (editor/inspector helper). A generator of the form minute_i = last * (i/count)^gamma does not match the shipped data: count=10, gamma=1, last=150, yet AutoBalanceInput.EventBossMinutes has 20 irregular entries. The minute list was therefore hand-edited or produced some other way. For crystal events, that asset list is not used at runtime: EventForecastService replaces it with CrystalEventRosterConfig.BossMinutes (also 20 irregular entries, including a duplicate 60), and that list is also not a gamma curve. Bosses[i] pairs EventBossMinutes[i] with snapshots[i]. This relies on BuildTimePoints producing exactly EventBossMinutes.Count snapshots. The fallback current-player snapshot path is not in the event-plan code.

**依赖图节点**：输入 `value:forecast.snapshot`、`value:forecast.enemy_health`、`value:forecast.enemy_damage`、`value:forecast.stat_reward`、`config:CrystalEventRosterConfig.ForecastSettings.AutoBalanceInput.EventBossMinutes`、`config:CrystalEventRosterConfig.BossMinutes`、`config:CrystalEventRosterConfig.ForecastSettings.AutoBalanceInput.EventEndMinute`、`config:EventConfig.DifficultySettings`、`config:CrystalEventRosterConfig.Leagues[].SnapshotIslandNumber`、`constant:EventForecastService.crystal_boss_snapshot_timing_step`、`constant:EventForecastService.resisted_type_weakness`、`state:current_event`、`value:player.unlocked_location`、`config:CrystalEventRosterConfig.Stages[].Rewards`、`config:CrystalEventRosterConfig.Milestones[].Rewards`、`config:CrystalEventRosterConfig.Milestones[].RequiredCrystals`、`value:reward.baked_stat_reward`、`state:rng`；输出 `value:forecast.event_plan`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `simulation.forecast_event_plan`）：Checked against Crystal Event Roster Config.asset lines 32-57: EventBossCount=10, EventBossGamma=1, EventLastBossMinute=150, a 20-entry irregular EventBossMinutes list, and RandomBossResists=0 with no reader in the code. Notes now replace the gamma-generator guess with this evidence. The notes also add that crystal events overwrite EventBossMinutes with roster.BossMinutes (EventForecastService.cs:157-161), which is also not gamma-shaped.
- 合并复核修正：used_by lists simulation.forecast_build_core, but the dependency runs the other way. BuildCore produces the snapshots that BuildEventPlan consumes: ForecastApi.cs:30 calls BuildEventPlan(snapshots, input) after ForecastSnapshots.BuildAsync. Listing BuildCore as a consumer would create a reversed edge in the dependency graph, from the event plan to BuildCore. The fix removes it from used_by and records it in notes as the upstream input. I checked the rest against ForecastSnapshots.cs:111-147, EventForecastService.cs:122-190 and Crystal Event Roster Config.asset lines 32-57 and 75-95: the expression, the crystal-caller pre/post-processing, the status/native_va 0x13f5ee0 and the resolution of the pending notes correction are accurate (EventLastBossMinute=150, a 20-entry EventBossMinutes list, RandomBossResists=0, and roster BossMinutes with a duplicate 60 ending at 1000).

### `simulation.forecast_crystal_reward_mapping`

**Forecast crystal reward entry -> reward**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`lookup_table`
- 来源：子系统梳理
- 原生地址：`0x13f69ac, 0x13f6ab4`

```text
Stat -> StatReward(Value=(float)Value); EventCurrency/Chest -> Amount; Weapon/Shield/ArmorTier -> ItemReward(Rarity = Tier-1, Level = Amount, IsRandom = Tier != 7, mythical uses explicit Def); other -> EventReward(ExtraDurationMinutes = Amount)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Tier` | 1-based rarity tier | ForecastCrystalRewardEntry.Tier |

**输出**：RewardBase

**代码**：[Simulation/Forecast/ForecastCrystalRewardEntryHelper.cs:9-42](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastCrystalRewardEntryHelper.cs#L9)

**状态依据**：Native VA comment.

**配置参数**：`Crystal Event Roster Config.Stages[].Rewards`

**使用方**：Events crystal roster

**依赖图节点**：输入 `value:forecast.event_plan`、`value:reward.baked_stat_reward`、`config:CrystalEventRosterConfig.Stages[].Rewards`、`config:CrystalEventRosterConfig.Milestones[].Rewards`、`constant:ForecastCrystalRewardEntryHelper.mythical_tier`；输出 `value:forecast.crystal_reward`

### `simulation.spawner_charges_limit`

**Spawner charges per daily big respawn (used by RPM and soul RPM)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：核验时补充
- 原生地址：`0x13e83ec`

```text
chargesLimit = DisableAfterFirstClear ? Count : ChargesTotal; ChargesTotal = IsChargesEnabled ? eff : 0; IsChargesEnabled = !IsEvent && eff > 0; eff = CanUseSpawnerCooldownsAb ? ABcharges(isElite, loc) * Count : ChargesBeforeBigRespawn + (ItemReward ? floor(meta items_rarity_charges[rarity(ItemReward)]) : 0); CanUseSpawnerCooldownsAb = !IsEvent && !IsVip && ItemReward==null && Cooldown >= 10
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ChargesBeforeBigRespawn` | designer charges per big respawn | EnemySpawnerConfig.ChargesBeforeBigRespawn |
| `ABcharges` | AB per-location charges (elite/nonElite group) | ABTest Data Config.spawnerCooldowns.charges |
| `items_rarity_charges` | meta bonus charges for item-reward spawners | Meta Stats Config |

**输出**：int charges counted per day in RPM/soul RPM

**代码**：[Simulation/Systems/SimulationSystem.cs:608-612](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L608)；[Enemy/EnemySpawner.cs:307-341](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L307)；[Meta/SpawnerChargesCalculator.cs:23-40](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SpawnerChargesCalculator.cs#L23)；[ABTests/ABTestService.cs:45](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L45)

**状态依据**：GetSpawnerChargesLimit carries 'Original ELF VA 0x13e83ec'.

**配置参数**：`EnemySpawnerConfig.ChargesBeforeBigRespawn`；`EnemySpawnerConfig.DisableAfterFirstClear`；`EnemySpawnerConfig.Cooldown`；`EnemySpawnerConfig.ItemReward`；`ABTest Data Config.spawnerCooldowns`；`Meta Stats Config items_rarity_charges`

**使用方**：simulation.rpm_time_chunk；simulation.soul_rpm

**备注**：This is a direct multiplier in RPM, so it scales every RPM-denominated reward (shop, rewarded, event). At runtime the DisableAfterFirstClear branch is dead because those spawners are filtered out of spawnerData in GPM-bot mode.

**依赖图节点**：输入 `config:EnemySpawnerConfig.DisableAfterFirstClear`、`config:EnemySpawnerConfig.Count`、`config:EnemySpawnerConfig.ChargesBeforeBigRespawn`、`config:EnemySpawnerConfig.IsEvent`、`config:EnemySpawnerConfig.ItemReward`、`value:spawner.uses_ab_cooldowns`、`value:spawner.ab_charges`、`value:player.meta_stat[items_rarity_charges]`、`value:item.rarity_param`；输出 `value:spawner.charges_total`、`value:spawner.charges_limit_per_day`

### `simulation.forecast_current_player_fallback`

**Forecast fallback snapshot from current player (local addition)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：核验时补充

```text
If Simulation Data.TimeChunkSnapshots is missing or empty: run CalculateSimulationData(); snapshot.Stats[k] = {Value = TotalValue = player.GetStatValue(k), CurrentGpm = RPMCalculatedValue[k]}; DpsData/DefenseData = current runtime sim DPS/defense; Playtime = eventMinute. This snapshot is constant for every boss.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `player stats` | live player stat values | PlayerModel.Unit |
| `RPMCalculatedValue` | runtime RPM | simulation.rpm_time_chunk |

**输出**：SimulationSnapshot for forecast

**代码**：[Simulation/Forecast/ForecastSnapshots.cs:89-109](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastSnapshots.cs#L89)

**状态依据**：Explicitly commented as a 'local learning fallback' and not original. The original path at 0x13f79d0 (lines 78-88) is the table interpolation.

**配置参数**：`Simulation Data.TimeChunkSnapshots`

**使用方**：simulation.forecast_auto_balanced_enemy；simulation.forecast_reward

**备注**：It changes event enemy HP and damage only when the shipped table is missing. The model should ignore it when Simulation Data is present.

**依赖图节点**：输入 `value:sim.snapshot_progress`、`value:player.stat_raw`、`value:player.stat_specific[HealthRegen]`、`value:sim.stat_rpm`、`value:sim.dps_by_damage_type`、`value:sim.defense_by_damage_type`、`value:player.unlocked_location`、`value:item.copies`、`value:item.evolution_level`、`config:CrystalEventRosterConfig.ForecastSettings.AutoBalanceInput.EventBossMinutes`、`config:CrystalEventRosterConfig.BossMinutes`、`config:EventConfig.DifficultySettings`；输出 `value:sim.snapshot_at_time`

### `simulation.ads_reward_model`

**Simulated rewarded-ad income in the bot**

- 状态：**空壳**（`stub`）
- 类型：`simulation`
- 来源：完整性审查补充

```text
Intended: every AdsIntervalMinutes of simulated playtime, grant a stat reward worth AdsRewardMultiplierInMinutes * RPM(stat). Body missing. With the shipped config AdsRewardMultiplierInMinutes = 0 and AdsIntervalMinutes = 6, the baked Simulation Data contains no ad income.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `AdsIntervalMinutes` | interval between simulated ads | Simulation Config.AdsIntervalMinutes (6) |
| `AdsRewardMultiplierInMinutes` | ad value in RPM-minutes | Simulation Config.AdsRewardMultiplierInMinutes (0) |
| `lastAdsTime` | bot state | SimulationSystem.lastAdsTime |

**输出**：Extra stat income in the simulated progression

**代码**：[Simulation/Systems/SimulationSystem.cs:568](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L568)；[Simulation/Systems/SimulationSystem.cs:256](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L256)；[Simulation/SimulationConfig.cs:28-29](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L28)

**状态依据**：ProcessSimulateAds() has an empty body. No other reference to AdsRewardMultiplierInMinutes or AdsIntervalMinutes exists.

**配置参数**：`Simulation Config.AdsRewardMultiplierInMinutes`；`Simulation Config.AdsIntervalMinutes`

**使用方**：simulation.player_progression_tick；simulation.snapshot_growth_curve

**备注**：Live rewarded ads (meta.rewarded_ad_stat_value = RPM*6*...) are therefore not reflected in the baked balance target. Real players who watch ads are ahead of the curve.

**依赖图节点**：输入 `config:SimulationConfig.AdsIntervalMinutes`、`config:SimulationConfig.AdsRewardMultiplierInMinutes`、`value:sim.stat_rpm`、`value:sim.active_playtime`、`state:sim_bot_state`；输出 `value:sim.ads_stat_income`

### `simulation.stat_benefit_weights`

**Bot stat / item benefit weights**

- 状态：**空壳**（`stub`）
- 类型：`simulation`
- 来源：完整性审查补充
- 原生地址：`0x13e0cb8 (consumer only)`

```text
statsBenefit[s] is initialized to 1 for every StatType, and itemBenefit[item] is the item value. Both are meant to be updated by CalculateStatBenefit and ModifyFromItem (marginal power gain per unit of stat or item), whose bodies are missing. Consumer: rewardValue(spawner) = Count * (Σ_r statsBenefit[r.StatType] * r.Value), or for item spawners Count * (value + itemBenefit[item]) with a fallback of 10000 if the item is not owned. currentBoss gets 1e6.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `statsBenefit` | marginal value per unit stat | runtime bot state (init 1.0) |
| `itemBenefit` | marginal value of an item | runtime bot state |
| `r.Value` | authored stat reward | EnemySpawnerConfig.StatRewards |

**输出**：Spawner reward value used for bot target choice

**代码**：[Simulation/Systems/SimulationSystem.cs:270-273](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L270)；[Simulation/Systems/SimulationSystem.cs:717](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L717)；[Simulation/Systems/SimulationSystem.cs:721](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L721)；[Simulation/Systems/SimulationSystem.cs:765-787](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L765)

**状态依据**：CalculateStatBenefit() is empty and ModifyFromItem returns default. The consumer CalculateSpawnerRewardValue is native-annotated.

**配置参数**：`EnemySpawnerConfig.StatRewards`；`EnemySpawnerConfig.ItemReward`；`EnemySpawnerConfig.Count`

**使用方**：simulation.spawner_reward_value；simulation.spawner_priority；simulation.player_progression_tick

**备注**：Without the weights, a rerun bot values all stats equally (1.0), so the farm path and therefore the baked growth curve cannot be regenerated faithfully. Treat Simulation Data snapshots as ground truth.

**依赖图节点**：输入 `constant:SimulationSystem.stats_benefit_init`、`state:sim_bot_state`、`config:EnemySpawnerConfig.StatRewards[].Value`、`config:EnemySpawnerConfig.StatRewards[].StatType`、`config:EnemySpawnerConfig.Count`、`config:EnemySpawnerConfig.ItemReward`、`value:item.copies`、`constant:SimulationSystem.unowned_item_benefit_fallback`、`constant:SimulationSystem.current_boss_reward_value`；输出 `value:sim.stat_benefit_weights`、`value:sim.spawner_reward_value`

### `simulation.item_emission_model`

**Simulated item copy emission and fractional rolls**

- 状态：**空壳**（`stub`）
- 类型：`simulation`
- 来源：完整性审查补充

```text
Intended: copies per simulated clear = RollFractionalCount(baseCount * GetItemEmissionCoefficient(timing, item) * (IsPreferredItemEmissionType(item) ? k : 1)), with chained-item base counts from GetChainedItemBaseCount(baseItemsCount, value). RollFractionalCount(x) is presumably floor(x) + Bernoulli(frac(x)). All bodies return 0 or false.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `timing` | balance timing (minutes or chunk) | formula simulation.balance_timing |
| `item` | ItemConfig reward | EnemySpawnerConfig.ItemReward |

**输出**：Simulated item copies, which drive simulated evolution timing

**代码**：[Simulation/Systems/SimulationSystem.cs:546](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L546)；[Simulation/Systems/SimulationSystem.cs:551](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L551)；[Simulation/Systems/SimulationSystem.cs:807](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L807)；[Simulation/Systems/SimulationSystem.cs:812](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L812)；[Simulation/Systems/SimulationSystem.cs:817](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L817)

**状态依据**：All five methods return default constants.

**配置参数**：`Combat Balance Config.Interpolations (item emission types if present)`；`Locations Config.PreferredWeaponType/PreferredArmorType (likely 'preferred' type)`

**使用方**：balance.evolution_shards_capture；balance.evolution_target_timings_from_islands；simulation.player_progression_tick

**备注**：This is the missing link between spawner item drops and the EvolutionShardsRequirements and EvolutionTargetTimings tables. Those tables are output-only data (editor_only_missing generators).

**依赖图节点**：输入 `value:sim.balance_timing`、`value:sim.balance_coefficient[ItemEmission]`、`config:EnemySpawnerConfig.ItemReward`、`config:EnemySpawnerConfig.Count`、`config:LocationsConfig.Locations[].PreferredWeaponType`、`config:LocationsConfig.Locations[].PreferredArmorType`、`constant:SimulationSystem.preferred_item_emission_multiplier`、`state:rng`；输出 `value:sim.item_copies_emitted`

### `simulation.bot_fight_resolution`

**Bot fight resolution, regen and death handling**

- 状态：**空壳**（`stub`）
- 类型：`simulation`
- 来源：完整性审查补充

```text
Intended per bot step: choose best = FindBestSpawner() by priority; resolve fights via SimulateFightWithSpawner / SimulateFightWithSpawnerFast(fights): time += farmedTime * fights, HP += healthDelta * fights, then ApplyRegen(seconds): HP = min(HPmax, HP + regen * seconds). ProcessDeath on HP <= 0. Efficiency helpers GetFightEfficiency, GetTotalDPS, GetTotalArmor, CalculateFarmPotencial(spawner, path, damage) and CalcDifficultSpawner(spawner, secondsRegen). All bodies are missing.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `farmedTime, healthDelta, fightTime` | per-spawner fight model outputs | formula simulation.spawner_fight_model |
| `regen` | HealthRegen stat | SimulationSystem.GetRegen (native 0x13efe28) |

**输出**：Simulated time and HP evolution, which produce Simulation Data snapshots

**代码**：[Simulation/Systems/SimulationSystem.cs:676](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L676)；[Simulation/Systems/SimulationSystem.cs:693](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L693)；[Simulation/Systems/SimulationSystem.cs:697-713](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L697)；[Simulation/Systems/SimulationSystem.cs:730-741](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L730)；[Simulation/Systems/SimulationSystem.cs:748](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L748)

**状态依据**：Empty or default-return bodies. Only GetRegen/GetBattleRegen/CalculateSpawnerData around them are native.

**配置参数**：`Simulation Config.TimeStep`；`Simulation Config.SimulationSpeed`；`Simulation Config.UseFastTick`；`Simulation Config.FastTickStartChunkOffset`；`Simulation Config.IsSmartPath`

**使用方**：simulation.player_progression_tick；simulation.snapshot_growth_curve

**备注**：Merge into simulation.player_progression_tick if that entry already enumerates these stubs. TimeStep (300), SimulationSpeed, UseFastTick and FastTickStartChunkOffset have no live consumer.

**依赖图节点**：输入 `value:sim.spawner_priority`、`value:sim.spawner_farmed_time`、`value:sim.spawner_health_delta`、`value:sim.spawner_fight_time`、`value:player.stat_specific[HealthRegen]`、`value:player.stat_raw[Health]`、`state:sim_bot_state`、`config:SimulationConfig.TimeStep`、`config:SimulationConfig.SimulationSpeed`、`config:SimulationConfig.UseFastTick`、`config:SimulationConfig.FastTickStartChunkOffset`、`config:SimulationConfig.IsSmartPath`；输出 `value:sim.bot_current_hp`、`value:sim.active_playtime`

### `simulation.spawner_charges_chunk`

**Simulated spawner charge budget per day chunk**

- 状态：**空壳**（`stub`）
- 类型：`simulation`
- 来源：完整性审查补充
- 原生地址：`0x13ebe60, 0x13e83ec (implemented parts)`

```text
chunk = floor(CurrentTotalPlaytime / SpawnerChargesChunkMinutes) with SpawnerChargesChunkMinutes = 1440. In charges mode, a spawner is eligible while used[s] < limit(s), where limit = Count if DisableAfterFirstClear, else ChargesTotal. Intended reset: on chunk rollover, used[s] = 0 (UpdateSpawnerChargesChunk, ResetCooldowns), with each clear consuming GetSpawnerChargesPerClear(s) charges. The reset and per-clear bodies are missing.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `SpawnerChargesChunkMinutes` | charge reset period in simulated minutes | Simulation Config.SpawnerChargesChunkMinutes (1440) |
| `ChargesTotal` | effective charges | formula spawners.effective_charges |
| `used[s]` | charges used this chunk | SimulationSystem.spawnerChargesUsage |

**输出**：Which spawners the bot may farm in the current simulated day

**代码**：[Simulation/Systems/SimulationSystem.cs:515-520](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L515)；[Simulation/Systems/SimulationSystem.cs:580](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L580)；[Simulation/Systems/SimulationSystem.cs:584](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L584)；[Simulation/Systems/SimulationSystem.cs:608-616](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L608)；[Simulation/Systems/SimulationSystem.cs:639-643](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L639)

**状态依据**：UpdateSpawnerChargesChunk and ResetCooldowns are empty, and GetSpawnerChargesPerClear returns 0. The limit and timing functions are native.

**配置参数**：`Simulation Config.UseSpawnerCharges`；`Simulation Config.SpawnerChargesChunkMinutes`；`EnemySpawnerConfig.ChargesBeforeBigRespawn`；`EnemySpawnerConfig.Count`；`EnemySpawnerConfig.DisableAfterFirstClear`

**使用方**：simulation.rpm_time_chunk；simulation.soul_rpm；simulation.spawner_fight_model

**备注**：This complements the already-missed simulation.spawner_charges_limit. Note the unit mismatch: optimum 'days' are 30-minute time chunks (TimeChunkDurationMinutes = 30, ConvertOptimumToDays = minutes/30), while charges reset every 1440 simulated minutes.

**依赖图节点**：输入 `config:SimulationConfig.UseSpawnerCharges`、`config:SimulationConfig.SpawnerChargesChunkMinutes`、`value:sim.total_playtime`、`value:spawner.charges_limit_per_day`、`state:sim_bot_state`；输出 `value:sim.spawner_eligible`

### `simulation.rpm_stat_class_ab_multiplier`

**AB stat-class RPM multiplier and fast-RPM parameters**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`lookup_table`
- 来源：完整性审查补充
- 原生地址：`0x13e1468, 0x13e7cf4 (consumers)`

```text
rpm(stat) *= m(stat), where m = sustainMult for {Health, HealthRegen, LifestealFlat}, attackMult for {StabDamageFlat, CutDamageFlat, BuckshotDamageFlat}, armorMult for {StabDefenceFlat, CutDefenceFlat, BuckshotDefenceFlat}, and 1 otherwise. The fast RPM parameters (fightTimeSeconds T and maxHealthLossMultiplier L) enter the time-chunk RPM as contribution = T / (T + fightTime) * value * charges * locMult * hpMult, where a spawner is skipped if loss > L * HP. The location multiplier locMult is 2 for islands 1-2 (non-elite) and 1 otherwise.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `attackMult, armorMult, sustainMult` | AB class multipliers | ABTest Data Config.asset CommonRewardMultipliers (all 1 in default variant) |
| `T, L` | fast RPM params | ABTest Data Config.asset FastRPMParams (10 / 1 default; variant 15 / 5) |

**输出**：Stat RPM/GPM used by all RPM-valued rewards

**代码**：[ABTests/ABTestService.cs:40-43](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L40)；[ABTests/ABTestService.cs:49-58](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L49)；[Simulation/Systems/SimulationSystem.cs:232-237](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L232)；[Simulation/Systems/SimulationSystem.cs:426-461](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L426)

**状态依据**：ABTestService has no native annotation. The SimulationSystem consumers are native-annotated.

**配置参数**：`ABTest Data Config.CommonRewardMultipliers.attackStatValueMultiplier`；`ABTest Data Config.CommonRewardMultipliers.armorStatValueMultiplier`；`ABTest Data Config.CommonRewardMultipliers.sustainStatValueMultiplier`；`ABTest Data Config.FastRPMParams.fightTimeSeconds`；`ABTest Data Config.FastRPMParams.maxHealthLossMultiplier`

**使用方**：simulation.rpm_time_chunk；meta.rewarded_ad_stat_value；meta.bake_stat_reward；meta.grant_reward_stat；spawners.event_crystal_mob_bonus

**备注**：The extractor must export ABTest Data Config.asset (all variants and per-location overrides). Scene and config values alone do not determine live RPM, enemy HP/damage, spawner cooldowns or charges.

**依赖图节点**：输入 `state:ab_cohort`、`config:ABTestDataConfig.commonRewardMultipliers.variants[].data.attackStatValueMultiplier`、`config:ABTestDataConfig.commonRewardMultipliers.variants[].data.armorStatValueMultiplier`、`config:ABTestDataConfig.commonRewardMultipliers.variants[].data.sustainStatValueMultiplier`；输出 `value:sim.rpm_stat_class_multiplier`

### `ab.enemy_hp_damage_multiplier`

**ab.enemy hp damage multiplier**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
``GetEnemyHpDamageMultiplier``: override for the location, else the default (float32).
```

**输出**：See implementation and tests.

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `balance.armor_base`

**balance.armor base**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for balance.armor_base.
```

**输出**：See implementation and tests.

**代码**：[Balance/CombatBalanceConfig.cs:211](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L211)

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `balance.evolution_armor_sync_by_leader`

**balance.evolution armor sync by leader**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
``SyncShieldValuesByLeader``: the other used types copy the leader's Value and CopyValue (the shards and
H are already synced in the stored tables); ReferenceStatPercent follows each row's own stat value.
```

**输出**：See implementation and tests.

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `balance.evolution_data_identities`

**balance.evolution data identities**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
G09 and G10 counts on the stored tables.
```

**输出**：See implementation and tests.

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `balance.evolution_fill_remaining`

**balance.evolution fill remaining**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Levels 1..n of one (kind, type, rarity) series.

``heuristics[L-1]`` is ``H`` of a simulated level or ``None`` for a level past the simulated horizon,
which uses the last simulated ``Value`` (``FillRemainingEvolutionValues``). The recursion feeds its own
output forward unless ``previous`` gives ``(Value, CopyValue)`` of levels ``0..n`` to feed in instead
(one-step replay of stored rows: level ``L`` starts from ``previous[L-1]``; ``previous[0]`` is level 0).
```

**输出**：See implementation and tests.

**代码**：[Balance/CombatBalanceConfig.cs:129](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L129)

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `balance.evolution_fitted_h`

**balance.evolution fitted h**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Regenerate the 450 rows with the fitted ``H`` (sv = the stored ReferenceStatValue of each simulated
row; filled rows use the last simulated Value). Not exact by construction (R4). ``previous`` as in
:func:`replay_stored_inputs`; armor is generated per type (the sync leader is not known, G09).
```

**输出**：See implementation and tests.

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `balance.evolution_generator`

**balance.evolution generator**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for balance.evolution_generator.
```

**输出**：See implementation and tests.

**代码**：[Balance/CombatBalanceConfig.cs:250](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L250)

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `balance.evolution_heuristic_fit`

**balance.evolution heuristic fit**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Weapons: per series the minimum of ``H / (cd * sv)`` over simulated rows (the envelope row, always the
last simulated level); ``k_t`` is the geometric mean over rarities of ``min / M_r``. Armor: median of
``H / sv`` over on-time rows (R5) with ``T >= armor_min_timing`` (ItemGrowth.md 4.3).
```

**输出**：See implementation and tests.

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `balance.evolution_nice_rounding`

**balance.evolution nice rounding**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Round to the 'nice' grid, computed exactly on the binary value of ``x``.

``tie="even"`` is C# ``Math.Round`` (``MidpointRounding.ToEven``), the rule the data needs (6 midpoint
targets in G07); ``"up"`` (half away from zero) and ``"down"`` (half toward zero) are ablations.
Non-positive inputs give 0.
```

**输出**：See implementation and tests.

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `balance.evolution_reference_stat_percent`

**balance.evolution reference stat percent**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
``Math.Round(100.0 * Value / ReferenceStatValue)`` in double (G08).
```

**输出**：See implementation and tests.

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `balance.evolution_reference_stat_value`

**balance.evolution reference stat value**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
``ReferenceStatValue`` reads the player's ``<Type>DamageFlat`` (weapons) / ``<Type>DefenceFlat`` (armor)
``TotalValue`` at ``ReferenceTimeChunkIndex``.
```

**输出**：See implementation and tests.

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `balance.evolution_reference_time_chunk`

**balance.evolution reference time chunk**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
R5: ``ReferenceTimeChunkIndex == ceil(T)`` (the item evolved on the target day).
```

**输出**：See implementation and tests.

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `balance.evolution_replay_stored_inputs`

**balance.evolution replay stored inputs**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
G07: regenerate the 450 Stabbing/Cutting/Buckshot x Common..Legendary rows from the stored ``H`` and
shards. ``previous="generated"`` chains the recursion; ``"stored"`` feeds each level the stored previous
row (stage 4's one-step replay; the design's ablation counts are one-step).
```

**输出**：See implementation and tests.

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `balance.evolution_targets`

**balance.evolution targets**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for balance.evolution_targets.
```

**输出**：See implementation and tests.

**代码**：[Balance/CombatBalanceConfig.cs:303](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L303)

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `balance.evolution_value`

**balance.evolution value**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for balance.evolution_value.
```

**输出**：See implementation and tests.

**代码**：[Balance/CombatBalanceConfig.cs:235](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L235)

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `balance.island_timing`

**balance.island timing**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for balance.island_timing.
```

**输出**：See implementation and tests.

**代码**：[Balance/CombatBalanceConfig.cs:343](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L343)

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `balance.shards_capture`

**balance.shards capture**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for balance.shards_capture.
```

**输出**：See implementation and tests.

**代码**：[Balance/CombatBalanceConfig.cs:141](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L141)

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.ads`

**simulation.ads**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.ads.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Lifecycle.cs:12

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.ads_reward_stat`

**simulation.ads reward stat**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.ads_reward_stat.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Lifecycle.cs:12

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.amulet_auto_balance`

**simulation.amulet auto balance**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.amulet_auto_balance.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Amulet.cs:23

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.apply_clear`

**simulation.apply clear**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.apply_clear.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Rebuild.cs:153

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.armor_base_balance`

**simulation.armor base balance**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.armor_base_balance.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Inventory.cs:193

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.bake_day`

**simulation.bake day**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
``BuildSpawnerBakeReport``: ``day = isDays ? t : t / 60f``.
```

**输出**：See implementation and tests.

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.charges_per_clear`

**simulation.charges per clear**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.charges_per_clear.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Run.cs:327

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.crystal_simulation`

**simulation.crystal simulation**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.crystal_simulation.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Amulet.cs:11

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.current_boss`

**simulation.current boss**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
``currentBoss`` while ``location`` is the last unlocked island (design P17, B11).

The spawner on island ``location`` with a gate whose ``NextLocationNumber == location + 1``;
None if there is none (island 17). Gates elsewhere can also lead to ``location + 1`` (islands
11, 12, 13 carry back-gates to 8, 7, 2); those are not on the current island and are ignored.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Rebuild.cs:296

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.day_change`

**simulation.day change**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.day_change.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Run.cs:238

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.day_change_order`

**simulation.day change order**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
The player at a day snapshot as ``RecalculateChunkRpmValues`` saw it (design 4.4).

Stats are the stored ``TotalValue`` except Health, HealthRegen and LifestealFlat, which are recomposed
from ``Value`` with K2/K3 of day ``d - offset`` (``order``; offset clamped at day 0). The cached
DpsData/DefenseData are the snapshot's own. ``k4k5_offset`` lags the live K4/K5 too (ablation).
``day`` overrides the snapshot's TimeChunkIndex (an interpolated snapshot carries a neighbour's).
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Run.cs:251

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.eligible_spawners`

**simulation.eligible spawners**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
The fast phase's eligible set (design 4.5): ``CalculateSpawnerData(false)`` (bot filter: alive,
cooldown -- a cleared DAFC spawner's ``+inf`` excludes it --, charges, OT cap, unlocked island),
``fightTime < 300``, and :func:`fast_killable`.
```

**输出**：See implementation and tests.

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.evolution_balance`

**simulation.evolution balance**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.evolution_balance.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Inventory.cs:201

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.execute_tick`

**simulation.execute tick**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.execute_tick.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Run.cs:20

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.fast_accumulate`

**simulation.fast accumulate**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.fast_accumulate.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.FastTick.cs:167

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.fast_day_advance`

**simulation.fast day advance**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.fast_day_advance.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Run.cs:196

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.fast_flush`

**simulation.fast flush**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.fast_flush.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.FastTick.cs:221

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.fast_kill_gate`

**simulation.fast kill gate**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Design 4.5: the fast phase farms a spawner only if :func:`rpm_survivable` with ``L = KillLossMultiplier``
(default 1.0, not the AB value) -- the RPM predicate reused as the bot's kill gate (inferred).
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.FastTick.cs:151

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.fast_tick`

**simulation.fast tick**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.fast_tick.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.FastTick.cs:24

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.forecast_unlocked_location`

**simulation.forecast unlocked location**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.forecast_unlocked_location.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Run.cs:333

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.fractional_count`

**simulation.fractional count**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.fractional_count.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Inventory.cs:72

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.hand_tick`

**simulation.hand tick**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.hand_tick.
```

**输出**：See implementation and tests.

**代码**：[Simulation/SimulationConfig.cs:89](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L89)

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.idle_step`

**simulation.idle step**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.idle_step.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Run.cs:174

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.item_chain`

**simulation.item chain**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.item_chain.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Inventory.cs:86

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.item_emission`

**simulation.item emission**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.item_emission.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Inventory.cs:221

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.item_preferred`

**simulation.item preferred**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.item_preferred.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Inventory.cs:229

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.make_snapshot`

**simulation.make snapshot**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.make_snapshot.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Snapshot.cs:15

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.next_location_timing`

**simulation.next location timing**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.next_location_timing.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Run.cs:222

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.optimum_time_cap`

**simulation.optimum time cap**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
``IsCapOptimumTime`` filter: True = the bot skips the spawner (OT > d for day mode, OT > P otherwise).
```

**输出**：See implementation and tests.

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.prepare_run`

**simulation.prepare run**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.prepare_run.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Run.cs:51

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.process_inventory`

**simulation.process inventory**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.process_inventory.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Inventory.cs:140

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.same_day_gates`

**simulation.same day gates**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.same_day_gates.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Rebuild.cs:285

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.slow_death`

**simulation.slow death**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.slow_death.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Fight.cs:24

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.slow_fight`

**simulation.slow fight**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.slow_fight.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Fight.cs:14

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.slow_item_reward`

**simulation.slow item reward**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.slow_item_reward.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Inventory.cs:214

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.slow_move_time`

**simulation.slow move time**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.slow_move_time.
```

**输出**：See implementation and tests.

**代码**：[Simulation/Systems/SpawnerData.cs:48](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SpawnerData.cs#L48)

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.slow_priority`

**simulation.slow priority**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.slow_priority.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Targeting.cs:20；[Simulation/Systems/SpawnerData.cs:55](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SpawnerData.cs#L55)

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.slow_tick`

**simulation.slow tick**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.slow_tick.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Run.cs:144

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.smart_path`

**simulation.smart path**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.smart_path.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Targeting.cs:30

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.snapshot_current_gpm`

**simulation.snapshot current gpm**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Early-list ``CurrentGpm`` = ΔValue / ΔPlaytime against the previous snapshot; NaN when ΔPlaytime = 0.

Data fact (G19, relative error <= 1e-5): ``MakeSnapshot`` is empty in the recovered code.
```

**输出**：See implementation and tests.

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.stat_benefit`

**simulation.stat benefit**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.stat_benefit.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Targeting.cs:77

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.state_override`

**simulation.state override**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.state_override.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Replay.cs:23

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.stop`

**simulation.stop**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.stop.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Run.cs:130

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.tick_heartbeat`

**simulation.tick heartbeat**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.tick_heartbeat.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Rebuild.cs:105

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.unlock_location`

**simulation.unlock location**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.unlock_location.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Rebuild.cs:265

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.weapon_slot_unlocks`

**simulation.weapon slot unlocks**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Weapon and armor slots unlocked at ``location`` under the closed-form rule (design P12): the default
slot, slot rewards when their gate's next island is reached, weapon_3 at island 4, weapon_4 at island 8.
```

**输出**：See implementation and tests.

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `simulation.weapon_slots`

**simulation.weapon slots**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for simulation.weapon_slots.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Inventory.cs:99

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `timeline.days_to_island_timing`

**timeline.days to island timing**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Inverse of :func:`convert_island_timing_to_days` (double): island ``k`` covers
``[start(k), ITD[k-1])``; days ``<= 0`` map to 1, days past the table to ``len(itd) + 1``.
```

**输出**：See implementation and tests.

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

### `timeline.island_timing_to_days`

**timeline.island timing to days**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`rebuild_rule`
- 来源：合并核验

```text
Reconstructed implementation in the cited C# member for timeline.island_timing_to_days.
```

**输出**：See implementation and tests.

**代码**：Simulation/Systems/SimulationSystem.Run.cs:231

**状态依据**：Stage 5 implementation; inferred or calibrated unless its implementation states otherwise.

**备注**：Stage 5 rule; see implementation and calibration evidence. Dependency edges have not been inferred.

## 配置参数

| 资产 | 字段 | 类型 | 含义 | 用于 |
|---|---|---|---|---|
| Simulation Config.asset | `LifeCycleInMinutes` | float[] | [10, 230]: 10 active minutes then 230 AFK per cycle (6 cycles = 60 active min per day) | simulation.lifecycle_time_model |
| Simulation Config.asset | `UseSpawnerCharges` | bool | 1: timing measured in day chunks; charge-based spawner usage | simulation.balance_timing, simulation.spawner_fight_model, simulation.spawner_optimum_timing_mode |
| Simulation Config.asset | `SpawnerChargesChunkMinutes` | float | 1440: chunk length; TimeChunkIndex = floor(TotalPlaytime/1440) = day | simulation.balance_timing |
| Simulation Config.asset | `TimeChunkDurationMinutes` | float | 30: divisor for minute-mode spawner timing and tower floor chunk; inconsistent with day-sized TimeChunkIndex | simulation.spawner_optimum_timing_mode, BossTower/BossSpawnService.cs:449 |
| Simulation Config.asset | `EnemyHpDamageMultiplier` | float | 1: global enemy HP/damage scale while bot runs | simulation.enemy_multiplier |
| Simulation Config.asset | `DifficultyThresholds` | List<{Threshold,Color}> | 0.05, 0.3, 0.95, 1, 4, 10 difficulty bands for heatmap | simulation.difficulty_map |
| Simulation Config.asset | `EarlySpawnerOptimumTimeOffsetStartLocation / RelativePart / Multiplier` | int/float/float | 11 / 0.5 / 1: shift bake time earlier for first-half spawners on islands >= 11 | simulation.bake_recalc_optimum_time |
| Simulation Config.asset | `IsCapOptimumTime` | bool | 1: bot may not fight spawners before their OptimumTime | simulation.spawner_fight_model |
| Simulation Config.asset | `TimeStep / SnapshotInterval / SimulationSpeed` | int | 300 s / 300 s / 9 ticks per frame | simulation.player_progression_tick |
| Simulation Config.asset | `UseFastTick / FastTickStartChunkOffset` | bool/int | 1 / 4: per-day fast simulation from day 4 | simulation.player_progression_tick |
| Simulation Config.asset | `AdsIntervalMinutes / AdsRewardMultiplierInMinutes` | float | 6 / 0: simulated ad rewards (disabled) | simulation.lifecycle_time_model |
| Simulation Config.asset | `AutoBalanceItems / AutoBalanceAmulet / AutoGambling / CrystalSimulation` | bool | 1/0/0/0: bot side-features | simulation.player_progression_tick, simulation.amulet_sim_multipliers |
| Simulation Config.asset | `TrimSimulationData` | bool | 0 now; data shows trimmed TimeChunkSnapshots (10% geometric) | simulation.snapshot_trim |
| Simulation Config.asset | `SpawnerBakeInfos` | List<SpawnerBakeInfo> | 712-row log of an older bake (Key, OptimumTime, PrevHealth, PrevDamage, Health, Damage); BossTower row has Infinity HP | simulation.bake_apply |
| Simulation Config.asset | `AmuletLevelBalance` | List<AmuletLevelBalanceData> | 7 levels, all Experience 0 / empty souls (no surviving output) | simulation.amulet_sim_multipliers |
| Simulation Config.asset | `curveBossDifficult` | AnimationCurve | empty; cleared on OnAwake; editor visualization of boss difficulty | simulation.difficulty_map |
| Simulation Config.asset | `TimeChunkSnapshots / Snapshots` | List<SimulationSnapshot> | empty; if non-empty, TimeChunkSnapshots overrides Simulation Data's for bakes | simulation.snapshot_interpolation_chunk |
| Simulation Data.asset | `Snapshots` | List<SimulationSnapshot> | 63 snapshots by active Playtime 0..248 min (days 0-3): Stats, Items, DpsData, DefenseData | simulation.snapshot_lookup_playtime, simulation.snapshot_growth_curve |
| Simulation Data.asset | `TimeChunkSnapshots` | List<SimulationSnapshot> | 46 snapshots at days 0..405 (trimmed), HP 100 -> 2.74e9 | simulation.snapshot_interpolation_chunk, simulation.snapshot_growth_curve, simulation.forecast_build_core |
| Combat Balance Config.asset | `IslandTargetDays` | float[] | cumulative end day per island (0.5, 1.83, 4, 7, 10, 14, 20, 26, 34, 44, 56, 71, 90, 110, 130, 150, 180, 211.67, 245, 280, ...) | simulation.island_timing_to_days, simulation.interp_island_timing_points, simulation.optimum_time_from_relative, simulation.bake_recalc_optimum_time |
| Combat Balance Config.asset | `Interpolations.Interpolations[] {Use, Name, Type, IslandPoints, TimingPoints, CoefPoints}` | List<BalanceInterpolation> | time curves: emission (0, 6, 7), chain probability (1), feature flat (2), feature multiplier (3), ability DPS (4), ability EHP (5) | simulation.interp_evaluate, simulation.interp_total_coefficient |
| Main.unity EnemySpawner.config | `OptimumTime / IsDays / RelativeOptimumTime` | float/bool/float | bake target time (days if IsDays, else active minutes) and position within island | simulation.optimum_time_from_relative, simulation.bake_spawner_health, simulation.bake_spawner_damage, simulation.bake_recalc_optimum_time |
| Main.unity EnemySpawner.config | `AverageFightTime` | float | designed seconds to kill the group (HP target) and to deal Difficulty × HP (damage target) | simulation.bake_spawner_health, simulation.bake_spawner_damage |
| Main.unity EnemySpawner.config | `Difficulty` | float | fraction of player max HP the fight should drain (<=0 treated as 1) | simulation.bake_spawner_damage, simulation.spawner_fight_model |
| Main.unity EnemySpawner.config | `Count` | int | units; HP divided by Count; attackers = min(4, (Count+1)/2) | simulation.bake_spawner_health, simulation.bake_spawner_damage, simulation.spawner_fight_model |
| Main.unity EnemySpawner.config | `Stats[AttackCooldown=5], Stats[Health=1], Stats[Damage=3]` | List<Stat> | cooldown input to bake; Health/Damage are bake outputs | simulation.bake_spawner_damage, simulation.spawner_dps, simulation.spawner_fight_model |
| Main.unity EnemySpawner.config | `ArmorConfig` | ArmorConfig ref | per weapon-type resists; picks the player's best type, and the resist scales HP | simulation.bake_spawner_health, simulation.dps_against_spawner |
| Main.unity EnemySpawner.config | `StatRewards / SoulReward / ChargesBeforeBigRespawn / DisableAfterFirstClear / IsEliteBoss` | various | inputs to RPM and soul RPM | simulation.rpm_time_chunk, simulation.soul_rpm, simulation.rpm_old |
| Main.unity EnemySpawner.config | `PriorityMultiplier / HandBalance` | float/bool | bot priority weight; presumed opt-out from rebake | simulation.spawner_priority, simulation.bake_apply |
| Locations Config.asset | `Locations[].PreferredWeaponType` | WeaponType | which player defense type enemies on that island are balanced against | simulation.bake_spawner_damage, simulation.spawner_dps |
| Item configs (weapon/armor .asset) | `SimulationDpsWeaponMultiplier` | float | per-item DPS fudge in simulation (1 for 79 items; 1.3–2 for 9) | simulation.dps_data, simulation.dps_against_spawner |
| ABTest Data Config.asset | `fastRPMParams.{fightTimeSeconds, maxHealthLossMultiplier}` | float | A: 10 / 1, B: 15 / 5 (rules AB) | simulation.rpm_time_chunk |
| ABTest Data Config.asset | `rpmOldAlgorithm.enabled` | bool | rules A -> 0 (new algorithm) | simulation.rpm_old |
| ABTest Data Config.asset | `enemyHpDamageMult.damageMultiplier` | ValueByLocation | A: 1; B: 1.5 default, loc1..4 = 1.1..1.4 | simulation.enemy_multiplier |
| ABTest Data Config.asset | `commonRewardMultipliers.attack/armor/sustainStatValueMultiplier` | float | RPM multiplier per stat group (all 1) | simulation.rpm_time_chunk |
| Crystal Event Roster Config.asset | `ForecastSettings.AutoBalanceInput` | ForecastAutoBalanceInput | EnemyStatsGrowthPerMinute 0.02, ramp 5 s / 0.04, boss T 15..25, D 0.6..1, reward minutes 5..15, 20 EventBossMinutes, EventEndMinute 200 | simulation.forecast_build_core, simulation.forecast_auto_balanced_enemy, simulation.forecast_tune_boss_presets, simulation.forecast_reward, simulation.forecast_damage_ramp_average |
| Crystal Event Roster Config.asset | `ForecastSettings.EnemyPresets` | ForecastEnemyPresets | Golem 8 s / 0.2, Boss 20 s / 1, GrandBoss 40 s / 1 | simulation.forecast_auto_balanced_enemy |
| Meta Stats Config.asset | `soul_drop_bonus, souls_rarity_amount` | meta stat | soul drop multiplier and per-rarity flat bonus | simulation.soul_rpm |

## 枚举

| 枚举 | 文件 | 取值 |
|---|---|---|
| `StatType` | [Unit/StatType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatType.cs) | 0=MovementSpeed,1=Health,2=HealthRegen,3=Damage,4=AttackRange,5=AttackCooldown,6=StabDamageFlat,7=StabDamagePercent,8=CutDamageFlat,9=CutDamagePercent,10=MagicDamageFlat,11=MagicDamagePercent,12=BuckshotDamageFlat,13=BuckshotDamagePercent,14=StabDefenceFlat,15=StabDefencePercent,16=CutDefenceFlat,17=CutDefencePercent,18=MagicDefenceFlat,19=MagicDefencePercent,20=BuckshotDefenceFlat,21=BuckshotDefencePercent,22=LifestealFlat,23=Evasion,24=CritChance,25=CritDamagePercent,26=UniversalDefenceFlat,27=UniversalDefencePercent,28=SuperCritChance,29=SuperCritDamagePercent,30=GlobalAttack,31=GlobalDefence,32=LifestealPercent,33=BlockChance,34=BlockAmount,35=VisualScaleMultiplier |
| `WeaponType` | [Enemy/WeaponType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/WeaponType.cs) | 0=Stabbing,1=Cutting,2=Magic,3=Buckshot,4=Universal |
| `Rarity` | [Player/Inventory/Rarity.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/Rarity.cs) | 0=Common,1=Uncommon,2=Rare,3=Epic,4=Legendary,5=Divine,6=Mythical |
| `BalanceInterpolationType` | [Balance/BalanceInterpolationType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/BalanceInterpolationType.cs) | 0=ItemEmission(sum),1=ItemChainProbability(product),2=StatsFromFeaturesFlat(sum),3=StatModifier(product),4=PlayerAbilityDPSMult(sum),5=PlayerAblityEHPMult(sum),6=ItemLegendaryEmission(sum),7=ItemEmissionByType(sum) |
| `ForecastEnemyKind` | [Simulation/Forecast/ForecastEnemyKind.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastEnemyKind.cs) | 0=EnemyGolem,1=EnemyBoss,2=EnemyGrandBoss |
| `ForecastCrystalRewardKind` | [Simulation/Forecast/ForecastCrystalRewardKind.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastCrystalRewardKind.cs) | 0=Stat,1=EventCurrency,2=Chest,3=WeaponTier,4=ShieldTier,5=ArmorTier,6=CustomKey |

## 未解决问题

1. Recomputing the recovered bake on the shipped Simulation Data matches scene Health exactly for 79 of 706 spawners, within 5% for 552, and damage within 5% for 504. Only 9 of 53 day-mode spawners that sit exactly on a stored chunk index match exactly. So the scene may have been baked from untrimmed Simulation Config.TimeChunkSnapshots (now empty) or from a slightly different sim run, or hand-tweaked afterwards. Which source is authoritative for the model?
2. SpawnerBakeInfos in Simulation Config is clearly stale: only 5 rows equal the scene and late-game values are up to 30× lower. Should it be ignored entirely, or kept as history of an earlier balance pass?
3. TimeChunkDurationMinutes=30 is used to convert minute-based optimum times (SimulationSystem.cs:537, BossSpawnService.cs:449) into chunk indices, but stored TimeChunkIndex values count days (1440 min = SpawnerChargesChunkMinutes). Is this a legacy unit bug, or did tower floors intentionally use 30-minute units?
4. For minute-mode (IsDays=0) bakes, the timing passed to the ability curves is minutes/60, which is hours, not days. This is harmless only because the type 4/5 curves are 0 before day 20. Is that intended?
5. In the game outside simulation, GetBalanceTimingMinutes reads Simulation Config.CurrentTotalPlaytime/CurrentPlaytime, which are 0. Runtime RPM, DpsData and the ability multipliers are therefore evaluated at day 0, not the player's real day. Did the original runtime set these fields from the real playtime somewhere?
6. The bot loop (Tick/FastTick/FindBestSpawner/ProcessInventory/priority) is entirely missing, so Simulation Data cannot be regenerated after economy changes. A replacement progression model must be re-implemented, using the recovered fight model, RPM and bake as building blocks.
7. The forecast damage target uses ×(1−dodge) with no attack cooldown or attacker count. The spawner bake uses cooldown/(1−evasion) and divides by attackers. Is the difference intentional for event bosses?
8. The bake picks player defense by the location's PreferredWeaponType, but BossTower.BakeStatsForFloor uses the spawner's WeaponType and the lowest-resist armor rather than the best player score. Which convention should the model treat as canonical?
9. RPM divides daily obtainable reward (reward × charges) by 60. That fits the 60 active minutes/day lifecycle in the early days, but fast-tick days average about 46 active minutes. Was 60 a deliberate design constant?
10. HandBalance=1 on two spawners (Spawner_1_8dc2006722e6c994593946eef6f4f007, second_biome_door). Was this the rebake opt-out flag? The rebake code is missing.
11. ForecastHelper.SetEventSettings and the EventBossCount/EventBossGamma/EventLastBossMinute inputs are unused in the recovered code. The original probably generated EventBossMinutes from a gamma curve, but the exact formula is unknown.
12. Amulet simulation (AmuletSimulationMultipliers, AmuletLevelBalance, AmuletConfig.BakeAmuletAutoBalance) left no data in Simulation Config. Any baked amulet economy must be recovered from Amulet Config directly.
