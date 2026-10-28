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
| [`simulation.interp_evaluate`](#simulationinterp_evaluate) | BalanceInterpolation piecewise-linear curve | 原生证据恢复 | `runtime_formula` |
| [`simulation.interp_total_coefficient`](#simulationinterp_total_coefficient) | Interpolation total coefficient per type | 原生证据恢复 | `runtime_formula` |
| [`simulation.interp_island_timing_points`](#simulationinterp_island_timing_points) | Interpolation island -> day timing conversion (ApplyIslandTimings) | 编辑器工具缺失 | `editor_generator` |
| [`simulation.island_timing_to_days`](#simulationisland_timing_to_days) | Island timing -> days | 原生证据恢复 | `runtime_formula` |
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
| [`simulation.optimum_time_from_relative`](#simulationoptimum_time_from_relative) | Spawner OptimumTime from RelativeOptimumTime | 编辑器工具缺失 | `editor_generator` |
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

## 公式

### `simulation.interp_evaluate`

**BalanceInterpolation piecewise-linear curve**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x1579ed4 (Evaluate), 0x157a65c (ParsePoints)`

```text
T = parse(TimingPoints), C = parse(CoefPoints) (split ';', invariant float). If |T|<2 or |C|<2 or |T|!=|C| -> 0 (error). If x <= T0 -> C0. Else for the first segment i with T[i+1] >= x: p = clamp01((x - T[i])/(T[i+1]-T[i])) (p=0 if T[i]==T[i+1]); value = C[i] + p*(C[i+1]-C[i]). If x > T_last -> C_last (flat extrapolation both ends).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `x` | timing in days (or hours for minute-mode bakes) | caller: GetBalanceTimingMinutes / snapshot day / ConvertIslandTimingToDays |
| `T` | timing breakpoints (days) | Combat Balance Config.Interpolations[].TimingPoints |
| `C` | coefficient breakpoints | Combat Balance Config.Interpolations[].CoefPoints |

**输出**：float coefficient of one curve

**代码**：[Balance/BalanceInterpolation.cs:23-52](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/BalanceInterpolation.cs#L23)；[Balance/BalanceInterpolation.cs:58-64](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/BalanceInterpolation.cs#L58)

**状态依据**：Comments 'Original ELF VA 0x1579ed4' and '0x157a65c' on bodies; data check: snapshot StatsFromFeaturesFlat/PercentMultiplier reproduce exactly from these curves at integer days (e.g. day 3 -> 0.20396976, day 405 -> 3.0 / 4.9075).

**配置参数**：`Combat Balance Config.Interpolations.Interpolations[].TimingPoints`；`Combat Balance Config.Interpolations.Interpolations[].CoefPoints`；`Combat Balance Config.Interpolations.Interpolations[].Use`

**使用方**：simulation.interp_total_coefficient

**备注**：FormatPoint (BalanceInterpolation.cs:66) is an empty editor helper. IslandPoints are not read at runtime; see simulation.interp_island_timing_points.

### `simulation.interp_total_coefficient`

**Interpolation total coefficient per type**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x1579dac, 0x157dd80, 0x157dd98`

```text
if rarity==Legendary and type==ItemEmission(0): type = ItemLegendaryEmission(6). If type in {1 ItemChainProbability, 3 StatModifier}: K(type,x) = PRODUCT over curves with Use && Type==type of Evaluate(x), starting at 1. Else (types 0,2,4,5,6,7): K = SUM of Evaluate(x), starting at 0. CombatBalanceConfig.GetInterpolationCoefficient(x,type[,rarity]) = K (default rarity Rare).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `x` | timing (days) | caller |
| `type` | BalanceInterpolationType | caller constant |
| `0xF5 mask` | bitmask of additive types {0,2,4,5,6,7} | constant in native |

**输出**：float K(type, day)

**代码**：[Balance/BalanceInterpolationsConfig.cs:12-27](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/BalanceInterpolationsConfig.cs#L12)；[Balance/CombatBalanceConfig.cs:135-145](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L135)

**状态依据**：Native VA comments; verified against Simulation Data: day 405 type2 sum = 0.3+0.6+0.7+0.4+0.4+0.2+0.4 = 3.0 and type3 product = 1.6*1.8*1.704 = 4.9075, equal to the stored StatsFromFeaturesFlat / PercentMultiplier.

**配置参数**：`Combat Balance Config.Interpolations`

**使用方**：simulation.dps_data；simulation.defense_data；simulation.ability_multipliers；simulation.snapshot_stat_composition；simulation.bake_spawner_health；simulation.bake_spawner_damage；simulation.forecast_auto_balanced_enemy；simulation.item_emission_in_sim；[BossTower/BossSpawnService.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/BossSpawnService.cs):BakeStatsForFloor

**备注**：Shipped curves (Combat Balance Config lines ~8150-8265): type0 ItemEmission: Tower(26..150: 0..0.2), GraphMapUpgrade(20..150: 0..1), EventItems(7..26: 0..0.3); type7 TowerByType(26..150: 0..1.8); type6 TowerLeg(110..180: 0..0.8), GraphMapUpgrade(110..180: 0..1); type1 FarmItem(20..44: 0.9..1); type2 GraphFlat 0..0.3, AmuletFlat 0..0.6, VIP 0..0.7, InApps 0..0.4, EventsFlat 0..0.4, Ads 0..0.2, ItemsEconomic 0..0.4 (max sum 3.0); type3 GraphMult 1..1.6, AmuletMult 1..1.8, EventsMult 1..2.0 (max product 5.76 at day 775); type4 AbilityDPS (20,90,150,775 -> 0,0.4,1.3,4); type5 AbilityEHP (-> 0,0.2,0.5,1.5).

### `simulation.interp_island_timing_points`

**Interpolation island -> day timing conversion (ApplyIslandTimings)**

- 状态：**编辑器工具缺失**（`editor_only_missing`）
- 类型：`editor_generator`
- 来源：子系统梳理

```text
TimingPoints[i] = IslandStartDay(IslandPoints[i]) where IslandStartDay(n) = 0 if n==1 else IslandTargetDays[n-2] (i.e. ConvertIslandTimingToDays with integer island)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `IslandPoints` | island numbers at which curve breakpoints sit | Combat Balance Config.Interpolations[].IslandPoints |
| `IslandTargetDays` | target day by which island n is finished | Combat Balance Config.IslandTargetDays |

**输出**：TimingPoints string (days)

**代码**：[Balance/BalanceInterpolation.cs:54-56](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/BalanceInterpolation.cs#L54)；[Balance/CombatBalanceConfig.cs:131-133](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L131)

**状态依据**：ApplyIslandTimings and CombatBalanceConfig.ApplyInterpolationIslandsToTimings have empty bodies; all shipped curves satisfy the rule (e.g. islands 9;14;17 -> 26;90;150, 3 -> 1.833333, 19 -> 211.6667, 1 -> 0).

**配置参数**：`Combat Balance Config.Interpolations[].IslandPoints`；`Combat Balance Config.IslandTargetDays`

**使用方**：simulation.interp_evaluate

**备注**：Changing IslandTargetDays does NOT move curves at runtime until this editor step is re-run; TimingPoints are baked.

### `simulation.island_timing_to_days`

**Island timing -> days**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x157ad44`

```text
n = floor(s); prev = (n==1) ? 0 : IslandTargetDays[n-2]; f = s - n; day = (f==0) ? prev : prev + f*(IslandTargetDays[n-1] - prev)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `s` | fractional island timing (island number + progress) | ForecastAutoBalanceInput.ForecastLocationNumber + BossSnapshotTimingStep*i |
| `IslandTargetDays` | cumulative day at end of each island | Combat Balance Config.IslandTargetDays (0.5,1.833,4,7,10,14,20,26,34,44,56,71,90,110,130,150,180,211.67,245,280,...) |

**输出**：day timing

**代码**：[Simulation/Forecast/ForecastSnapshots.cs:69-75](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastSnapshots.cs#L69)；[Simulation/Systems/SimulationSystem.cs:528-531](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L528)

**状态依据**：Comment 'Original 0x157ad44, inlined locally'; the public SimulationSystem.ConvertIslandTimingToDays wrapper (SimulationSystem.cs:528) is still a stub returning 0.

**配置参数**：`Combat Balance Config.IslandTargetDays`

**使用方**：simulation.forecast_build_core；simulation.interp_island_timing_points

**备注**：SimulationSystem.GetNextLocationInterpolationTiming (SimulationSystem.cs:523) is a related stub.

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

### `simulation.snapshot_stat_composition`

**Simulated player stat composition with feature interpolation**

- 状态：**空壳**（`stub`）
- 类型：`simulation`
- 来源：子系统梳理

```text
For s in MainInterpolationStats {Stab/Cut/BuckshotDamageFlat, Stab/Cut/BuckshotDefenceFlat, Health, HealthRegen, LifestealFlat}: TotalFlat_s = Value_s * (1 + K(2,day)); for s in DirectInterpolationStats {Health, HealthRegen, LifestealFlat}: Total_s = TotalFlat_s * K(3,day); for combat flats Total_s = TotalFlat_s (K(3) is applied later in DpsData/DefenseData); all other stats: Total = TotalFlat = Value. StatsFromFeaturesFlat = K(2,day), PercentMultiplier = K(3,day).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Value_s` | stat earned by the bot (base + devour/item rewards) | simulation.player_progression_tick output |
| `K(2,day)` | flat 'features' bonus (graph, amulet, VIP, IAP, events, ads, economy) | simulation.interp_total_coefficient type StatsFromFeaturesFlat |
| `K(3,day)` | multiplicative feature bonus | simulation.interp_total_coefficient type StatModifier |

**输出**：SimulationStatData {Value, StatsFromFeaturesFlat, PercentMultiplier, TotalFlatValue, TotalValue, CurrentGpm}

**代码**：[Simulation/Systems/SimulationSystem.cs:183-191](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L183)；[Simulation/Systems/SimulationSystem.cs:894-906](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L894)；[Simulation/Systems/SimulationSystem.cs:822-824](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L822)；[Simulation/SimulationStatData.cs:7-22](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationStatData.cs#L7)

**状态依据**：UpdateInterpolationStatModifier, GetSimulationSnapshotStatValue, UsesInterpolationStats and MakeSnapshot are empty. The stat sets and modifier names (__simulationStatsFromFeaturesFlatModifier, __simulationInterpolationStatModifier) are kept. I reverse-engineered the math from Simulation Data: tf/v = 1+K2 and tv/tf = K3 exactly for Health, HealthRegen and LifestealFlat, while tv/tf = 1 for the flat damage/defence stats.

**配置参数**：`Combat Balance Config.Interpolations`

**使用方**：simulation.bake_spawner_health；simulation.bake_spawner_damage；simulation.forecast_auto_balanced_enemy

**备注**：This is how non-combat progression systems (map graph, amulet, VIP, IAP, events, ads) enter enemy balance. They are not simulated; they are injected as time curves. CurrentGpm = RPMCalculatedValue[stat] at snapshot time.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression`：Only for MainInterpolationStats (6/8/12/14/16/20/1/2/22) is StatsFromFeaturesFlat = K(2,day) and PercentMultiplier = K(3,day). All other stats store StatsFromFeaturesFlat=0 and PercentMultiplier=1. For the six combat flats PercentMultiplier holds K3, but TotalValue = TotalFlatValue.  
  依据：Simulation Data day 405: key 23 Evasion has StatsFromFeaturesFlat 0 and PercentMultiplier 1; key 14 has 3.0 / 4.9075 with TotalValue == TotalFlatValue; key 1 has TotalValue = TotalFlat*4.9075.

### `simulation.dps_data`

**Simulated DPS per weapon type (DpsData)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x13e4478 (+closures 0x13f4634/465c/514c), UpdateDpsDataCache 0x13e4f24`

```text
For each WeaponType w: equip up to UnlockedWeaponSlots non-armor items sorted by (DamageType==w desc, item.DPS desc); DPS_w = SUM_items [ P_i * (F_i + item.Damage) * K(3,t) / AttackCooldown_i * (DamageType_i==w ? 1 : 1/10) * SimulationDpsWeaponMultiplier_i ], where F_i = stat(DamageStatTypeFlat_i), P_i = stat(DamageStatTypePercent_i), t = balance timing.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `F_i` | player flat damage stat for item's type (incl. 1+K2 feature bonus) | PlayerModel stat value (GetForecastStatValue) |
| `P_i` | player percent damage stat for item's type | PlayerModel stat value |
| `item.Damage` | item damage at its level/evolution | items subsystem (ItemModel.Damage) |
| `AttackCooldown_i` | weapon cooldown | ItemConfig.AttackCooldown |
| `SimulationDpsWeaponMultiplier_i` | per-item sim fudge factor (79x1, 1.3,1.35,3x1.4,2x1.6,2x2) | ItemConfig.SimulationDpsWeaponMultiplier |
| `K(3,t)` | StatModifier multiplier | simulation.interp_total_coefficient |

**输出**：List<SimulationDpsData> currentDpsData (stored in snapshots as DpsData)

**代码**：[Simulation/Systems/SimulationSystem.cs:908-933](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L908)；[Simulation/Systems/SimulationSystem.cs:869-873](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L869)

**状态依据**：Native VA comments. In the data, the Magic and Universal DPS entries are exactly 1/10 of the matching-type DPS at day 0, which confirms the off-type /10 rule.

**配置参数**：`ItemConfig.SimulationDpsWeaponMultiplier`；`ItemConfig.AttackCooldown`；`ItemConfig.DamageType`；`ItemConfig.DamageStatTypeFlat`；`ItemConfig.DamageStatTypePercent`

**使用方**：simulation.dps_against_spawner；simulation.bake_spawner_health；simulation.forecast_auto_balanced_enemy；[BossTower/BossSpawnService.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/BossSpawnService.cs):BakeStatsForFloor

**备注**：Crit, GlobalAttack and ability DPS are NOT in DpsData; they are applied by consumers.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `status_evidence`：At day 0 (one Cut item) Stab, Magic, Buckshot and Universal are all exactly 1/10 of Cut (0.923 vs 9.23). At later days Magic equals Universal (no items of either type), each equal to the sum of off-type DPS/10 over the equipped items.  
  依据：Simulation Data Snapshots[0].DpsData; day-405 DpsData Magic=Universal=1.01e8.

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
| `K(3,t)` | StatModifier multiplier | simulation.interp_total_coefficient |

**输出**：List<SimulationDpsData> currentDefenseData (snapshot DefenseData)

**代码**：[Simulation/Systems/SimulationSystem.cs:935-958](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L935)

**状态依据**：Native VA comment.

**配置参数**：`Armor item configs (Armor, DamageType)`

**使用方**：simulation.spawner_dps；simulation.bake_spawner_damage；simulation.forecast_auto_balanced_enemy

**备注**：Universal defence stats are not included here.

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

### `simulation.spawner_dps`

**Spawner DPS against the player**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x13ef8d4`

```text
If IsStatic: 0. Fast: dmg = ApplyDamageDefense(simMult * BaseDamage * (IsEvent ? 1 : abMult(loc)), Def[PreferredWeaponType(loc)]); DPSenemy = dmg / AttackCooldownBase * (1 - dodge) * blockMult * gDefMult. ApplyDamageDefense(a,d) = a * exp(-0.75 d/a) * max(0, 1 - 0.25 d/a). Slow: (1-dodge)*Calculate(hit).Damage/AttackCooldown * (1 - bc*ba) * simMult (NaN->0).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `BaseDamage` | spawner Damage stat (Stats Type 3) | EnemySpawnerConfig.Stats |
| `AttackCooldownBase` | spawner AttackCooldown stat (Type 5) | EnemySpawnerConfig.Stats |
| `Def[...]` | player defense vs the location's preferred weapon type | simulation.defense_data, LocationsConfig.Locations[].PreferredWeaponType |

**输出**：double enemy DPS per unit

**代码**：[Simulation/Systems/SimulationSystem.cs:960-991](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L960)；[Services/Damage/DamageCalculator.cs:24-29](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L24)

**状态依据**：Native VA comment.

**配置参数**：`EnemySpawnerConfig.Stats`；`EnemySpawnerConfig.IsStatic`；`Locations Config.Locations[].PreferredWeaponType`

**使用方**：simulation.spawner_fight_model

**备注**：It uses the location's preferred weapon type, not the spawner's own WeaponType. The bake does the same.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：This holds for the fast path only. The slow path builds an Undodgeable WeaponHitEvent with DamageType = spawner.Config.WeaponType and the runtime FirstUnit AttackCooldown. It applies (1-dodge), (1-bc*ba) and simMult, but not GlobalDefence and not the AB enemy multiplier (except inside Calculate).  
  依据：SimulationSystem.cs:979-990.

### `simulation.spawner_fight_model`

**Per-spawner fight/farm model (SpawnerData)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x13e6074, 0x13e13dc, 0x13efd54, 0x13e0724`

```text
(m,r,f,n)=(70,50,2,2). moveTime = 70 / S(MovementSpeed). missing = max(0, HPmax - HPcur). playerDPS = DPSvs + 0.1; countF = min((Count+1)/2, 4); enemyDPS = (DPSenemy + 0.1)*countF / AbEhp(t); fightTime = simMult * BaseHP*Count * abMult(loc) / playerDPS; delta = (BattleRegen(Difficulty) - enemyDPS)*fightTime; healthDelta = min(delta, missing); regenTime = 50 * max(0, -healthDelta / max(1,HPmax)); farmedTime = regenTime + moveTime + 2*fightTime; playerRPS = Reward0 / farmedTime, *0.2 if -healthDelta > 0.5*HPmax; keep only if fightTime < 300 s. BattleRegen(D) = (D+1)*HealthRegen + (2D+3)*LifestealFlat.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `BaseHP` | spawner Health stat | EnemySpawnerConfig.Stats Type 1 |
| `Count` | units per spawner | EnemySpawnerConfig.Count |
| `Difficulty` | spawner difficulty (fraction of player HP to lose) | EnemySpawnerConfig.Difficulty |
| `Reward0` | value of first StatReward | simulation.spawner_reward_value (GetResourceValue) |
| `HPmax` | player max health | PlayerModel stat |

**输出**：SpawnerData {playerDPS, enemyDPS, fightTime, healthDelta, farmedTime, avMoveTime, avRegenTime, rewardValue, playerRPS}

**代码**：[Simulation/Systems/SimulationSystem.cs:623-674](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L623)；[Simulation/Systems/SimulationSystem.cs:226-230](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L226)；[Simulation/Systems/SimulationSystem.cs:686-691](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L686)；[Services/Damage/DamageCalculator.cs:59-62](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L59)；[Simulation/Systems/SpawnerData.cs:36-42](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SpawnerData.cs#L36)

**状态依据**：Native VA comments (float32 count factor preserved).

**配置参数**：`EnemySpawnerConfig.Count`；`EnemySpawnerConfig.Difficulty`；`EnemySpawnerConfig.Stats`；`EnemySpawnerConfig.IsEvent`；`EnemySpawnerConfig.IsVip`；`EnemySpawnerConfig.DisableAfterFirstClear`；`EnemySpawnerConfig.OptimumTime`；`EnemySpawnerConfig.IsDays`；`Simulation Config.IsCapOptimumTime`；`Simulation Config.UseSpawnerCharges`

**使用方**：simulation.rpm_time_chunk；simulation.rpm_old；simulation.soul_rpm；simulation.player_progression_tick

**备注**：Filters: skip Event/Vip spawners. Skip spawners on locations not yet unlocked. Outside GPM-bot mode, also skip dead or cooling-down spawners, spawners with charges used up, and spawners not yet at OptimumTime when IsCapOptimumTime (day index for IsDays, Playtime minutes otherwise). In GPM-bot mode (forGPMBot=true) spawners with DisableAfterFirstClear are excluded.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression`：countF = min((float)(Count+1)/2f, 4f) (float division). Non-GPM filters: skip when !AnyoneAlive; skip when Cooldown > 0 ONLY if config.IsRunning; skip when charges are used up (UseSpawnerCharges); skip when IsCapOptimumTime && OptimumTime > (UseSpawnerCharges&&IsDays ? timeChunkIndex : CurrentPlaytime). At runtime CalculateSimulationData always calls CalculateSpawnerData(true), so the game only uses GPM-bot mode: no alive, cooldown, charge or optimum-time filters, and DisableAfterFirstClear spawners are excluded.  
  依据：SimulationSystem.cs:638-654 (`if (!(spawner.Cooldown <= 0f) && config.IsRunning) continue;`), :659, :328 `CalculateSpawnerData(true)`.

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

### `simulation.rpm_time_chunk`

**Stat reward-per-minute (RPM/GPM), current algorithm**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x13e7cf4, 0x13e1468, 0x13e83ec, 0x13e7a00`

```text
(F, L) = FastRPMParams(fightTimeSeconds, maxHealthLossMultiplier) = (10, 1) variant A / (15, 5) variant B. For spawners s with a StatReward of type k, skipping those with loss_s = -healthDelta_s > L*HPmax: c_s = F/(F + fightTime_s) * reward_s(k) * charges_s * locMult_s * hpMult_s, where charges_s = DisableAfterFirstClear ? Count : ChargesTotal; locMult = 2 for non-elite spawners on locations 1-2, else 1; hpMult = sqrt(HPmax/loss_s) if loss_s > HPmax else 1. RPM_k = SUM c_s / 60; if RPM_k > 0 then RPM_k = max(0.5, RPM_k); RPM_k *= AB RpmStatValueMultiplier(k) (attack {6,8,12}, armor {14,16,20}, sustain {1,2,22}); RPM_k = max(RPM_k, previous RPM_k) (monotonic).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `reward_s(k)` | stat reward per kill | EnemySpawnerConfig.StatRewards |
| `ChargesTotal` | kills per big respawn (daily) | EnemySpawner.ChargesTotal (ChargesBeforeBigRespawn / AB spawnerCooldowns.charges * unit count) |
| `fightTime_s, healthDelta_s` | from fight model | simulation.spawner_fight_model |
| `60` | divisor; inferred = designed active minutes per day (6 sessions x 10 min) | constant |

**输出**：RPMCalculatedValue[StatType] (stat gained per minute of play)

**代码**：[Simulation/Systems/SimulationSystem.cs:426-461](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L426)；[Simulation/Systems/SimulationSystem.cs:232-237](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L232)；[Simulation/Systems/SimulationSystem.cs:608-612](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L608)；[Simulation/Systems/SimulationSystem.cs:376-382](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L376)；[ABTests/ABTestService.cs:40-58](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L40)

**状态依据**：Native VA comments ('original location multipliers and FP operation order').

**配置参数**：`ABTest Data Config.fastRPMParams`；`ABTest Data Config.commonRewardMultipliers.attack/armor/sustainStatValueMultiplier`；`EnemySpawnerConfig.StatRewards`；`EnemySpawnerConfig.ChargesBeforeBigRespawn`；`EnemySpawnerConfig.DisableAfterFirstClear`；`EnemySpawnerConfig.IsEliteBoss`；`EnemySpawnerConfig.LocationNumber`

**使用方**：[UI/Events/RewardView.cs:288](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/Events/RewardView.cs#L288)；[Rewards/Commands/GrantRewardCommandRunner.cs:227](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/Commands/GrantRewardCommandRunner.cs#L227)；[Shop/Systems/ShopLotRpmCalculationSystem.cs:77](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/Systems/ShopLotRpmCalculationSystem.cs#L77)；[Rewards/Commands/BakeRewardsCommandRunner.cs:84-93](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/Commands/BakeRewardsCommandRunner.cs#L84)；[Enemy/EnemySpawner.cs:990](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L990) (crystal-event mob reward = max(1, 0.4*RPM))；[Events/Services/EventForecastService.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Events/Services/EventForecastService.cs) (via snapshot CurrentGpm)；simulation.forecast_reward；SimulationSnapshot.Stats[].CurrentGpm

**备注**：This is the main runtime link between spawner configuration and the value of rewarded, shop and event stat rewards: reward = Value × RPM[k]. With the daily big-respawn charges it reads as 'achievable daily devour income ÷ 60 minutes'. The old algorithm is used only when rpm_old_algorithm is enabled; the shipped rule is A, which is disabled.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `variables`：ChargesTotal = IsChargesEnabled ? eff : 0, where eff = AB GetSpawnerCharges(isElite, loc) * Count if CanUseSpawnerCooldownsAb (not Event, not Vip, no ItemReward, Cooldown >= 10), else SpawnerChargesCalculator.GetTotalCharges, which is ChargesBeforeBigRespawn plus the meta 'items_rarity_charges' bonus for item-reward spawners. Spawners with 0 charges contribute 0. Because runtime spawnerData is built with forGPMBot=true, DisableAfterFirstClear spawners are never candidates at runtime; the Count branch applies only inside the bot.  
  依据：[Enemy/EnemySpawner.cs:307-341](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L307) and ComputeEffectiveChargesBeforeBigRespawn; [Meta/SpawnerChargesCalculator.cs:23-40](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SpawnerChargesCalculator.cs#L23); SimulationSystem.cs:328, :654.

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

### `simulation.snapshot_lookup_playtime`

**Snapshot lookup by playtime (minute-mode)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
snap = argmin over Snapshots of |snap.Playtime - t| (no interpolation)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `t` | (re-calculated) optimum time in active-play minutes | EnemySpawnerConfig.OptimumTime (IsDays=0) |

**输出**：SimulationSnapshot

**代码**：[Simulation/SimulationData.cs:13-16](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationData.cs#L13)；[Simulation/SimulationConfig.cs:233-236](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L233)

**状态依据**：No native annotation. Data check: with nearest-snapshot lookup, 79 of 102 minute-mode spawners bake to exactly the shipped scene Health.

**配置参数**：`Simulation Data.Snapshots (63 entries, Playtime 0..248 min, ~5 min apart = SnapshotInterval 300 s)`

**使用方**：simulation.bake_spawner_health；simulation.bake_spawner_damage

**备注**：Covers roughly the first 4 days of the game (Playtime ≤ 248 min). Minute-mode spawners have OptimumTime up to 240.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `config_params`：63 entries, Playtime 0..248.1 min. During the first 10-minute cycle they are about 1.1 min apart (9 snapshots in 0-10.1 min); after that about 5 min apart. Duplicates occur at day boundaries (63.3 twice, 186.0 twice). Nearest-snapshot lookup picks the first of a tie.  
  依据：Parsed Simulation Data.Snapshots Playtime list: 0,1.2,2.2,3.4,4.4,5.6,6.8,8.2,10.1,11.1,...,63.3,63.3,...,186.0,186.0,...,248.1.

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

**配置参数**：`Simulation Config.EarlySpawnerOptimumTimeOffsetStartLocation`；`Simulation Config.EarlySpawnerOptimumTimeOffsetRelativePart`；`Simulation Config.EarlySpawnerOptimumTimeOffsetMultiplier`；`Combat Balance Config.IslandTargetDays`

**使用方**：simulation.bake_spawner_health；simulation.bake_spawner_damage

**备注**：On late islands, spawners in the first half of the island are baked against the player's power from earlier days, so they are easier. With the shipped values the shift reaches up to half an island's duration at Rel=0.

### `simulation.optimum_time_from_relative`

**Spawner OptimumTime from RelativeOptimumTime**

- 状态：**编辑器工具缺失**（`editor_only_missing`）
- 类型：`editor_generator`
- 来源：子系统梳理

```text
OptimumTime(days) = start(L) + RelativeOptimumTime * (IslandTargetDays[L-1] - start(L)), start(L) = (L==1 ? 0 : IslandTargetDays[L-2]), rounded to 0.1 day
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `RelativeOptimumTime` | designer placement within island | EnemySpawnerConfig.RelativeOptimumTime |
| `IslandTargetDays` | island schedule | Combat Balance Config.IslandTargetDays |

**输出**：EnemySpawnerConfig.OptimumTime for IsDays spawners

**代码**：[Enemy/SpawnerRelativeOptimumTimeTool.cs:10-24](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/SpawnerRelativeOptimumTimeTool.cs#L10)

**状态依据**：SpawnerRelativeOptimumTimeTool.FillRelativeOptimumTimes / ApplyOptimumTimesFromRelative have empty bodies. In Main.unity, 604 of 609 IsDays spawners satisfy the formula within ±0.05. The outliers are a superboss, an elite, Spawner_1_epic_buck (rel>1), Spawner_Crystal and second_biome_door.

**配置参数**：`EnemySpawnerConfig.RelativeOptimumTime`；`EnemySpawnerConfig.OptimumTime`；`Combat Balance Config.IslandTargetDays`；`SpawnerRelativeOptimumTimeTool.Islands`

**使用方**：simulation.bake_spawner_health；simulation.bake_spawner_damage；simulation.spawner_fight_model (IsCapOptimumTime)

**备注**：This is the main lever for retiming content. Changing IslandTargetDays and re-running this tool and the bake moves every spawner's power target.

### `simulation.bake_spawner_health`

**Baked spawner Health**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`editor_generator`
- 来源：子系统梳理
- 原生地址：`0x13db16c`

```text
snap = snapshot(bakeTime); day = IsDays ? bakeTime : bakeTime/60; cc = S(CritChance, snap), cd = S(CritDamagePercent, snap); rows = armors sorted desc by Score = DpsData[a.Type]*(1 - a.Resist); a* = rows[0]; r = clamp(a*.Resist, -0.9, 0.9); P0 = (1 + K(4,day)) * DpsData[a*.Type]; Peff = (1-cc)*P0 + cd*cc*P0; NewHealth = round(Peff * AverageFightTime / Count * (1 - r))
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `DpsData` | simulated player DPS by weapon type | Simulation Data snapshot |
| `AverageFightTime` | designed seconds to clear the spawner | EnemySpawnerConfig.AverageFightTime (5 s for 318 spawners; 10, 12, 15, 20, ... up to 500) |
| `Count` | units | EnemySpawnerConfig.Count |
| `Armors` | enemy resists | EnemySpawnerConfig.ArmorConfig |
| `K(4,day)` | ability DPS curve | simulation.ability_multipliers |

**输出**：per-unit Health written to EnemySpawnerConfig.Stats[Health].BaseValue

**代码**：[Simulation/SimulationConfig.cs:113-160](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L113)；[Simulation/SimulationConfig.cs:108-111](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L108)

**状态依据**：Comment 'Original ELF0x13db16c plus all19 actual predicates'. Re-running it on shipped Simulation Data reproduces scene Health exactly for 79 spawners, within 1% for 246 and within 5% for 552 of 706.

**配置参数**：`EnemySpawnerConfig.AverageFightTime`；`EnemySpawnerConfig.Count`；`EnemySpawnerConfig.ArmorConfig`；`EnemySpawnerConfig.OptimumTime`；`EnemySpawnerConfig.IsDays`；`Combat Balance Config.Interpolations (AbilityDPS)`

**使用方**：simulation.bake_apply；Enemy runtime HP (×abMult at spawn)

**备注**：The design intent is that, at its optimum time, the player kills the group in AverageFightTime using their best weapon type against the enemy's weakest resist. GlobalAttack is not included. The report method is runtime-callable, but writing the result into spawners was an editor step (see simulation.bake_apply). CalculateBakedSpawnerHealth(spawner, day) exposes it with an explicit day and no early shift.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `status_evidence`：Independent recomputation: all 79 exact HP matches are minute-mode (79 of 108); 0 of 608 day-mode spawners match HP exactly (median ratio 0.995, 10th-90th percentile 0.915-1.058). Even the 54 day-mode spawners whose re-calc time is exactly a stored chunk index give 0 exact HP and 10 exact damage. At a stored index the interpolation returns the stored snapshot unchanged, so trimming cannot explain the mismatch. Day-mode scene values were baked from a different simulation run or Simulation Data version (Snapshots/minute-mode data is consistent with the scene). Overall within 5%: HP 555/716, damage 511/716; exact damage 124.  
  依据：Re-ran SimulationConfig.cs:113-160 logic (incl. GetReCalcOptimumTime, interpolation, Stat.CalculateSpecificValue, SolveAttackDamage) against Main.unity and Simulation Data.asset.

### `simulation.bake_spawner_damage`

**Baked spawner Damage**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`editor_generator`
- 来源：子系统梳理
- 原生地址：`0x13db16c`

```text
D = Difficulty<=0 ? 1 : Difficulty; n = min(4, (Count+1)/2); BR = (D+1)*HealthRegen + (2D+3)*LifestealFlat (snapshot TotalValues); needDpsUnit = (BR + HP_player*D/AverageFightTime)/n; perHit = needDpsUnit * (AttackCooldown/(1 - ev)) / (1 - bc*ba); find a with ApplyDamageDefense(a, Def[PreferredWeaponType(L)]) / (1 + K(5,day)) = perHit (doubling then bisection to within 1); NewDamage = round(max(10, a)/10)*10
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `HP_player` | simulated player Health TotalValue | Simulation Data snapshot |
| `Difficulty` | fraction of player HP lost per fight (0.23, 0.3, 0.7, 1 most common) | EnemySpawnerConfig.Difficulty |
| `AttackCooldown` | enemy attack cooldown (Stats Type 5) | EnemySpawnerConfig.Stats |
| `ev, bc, ba` | player specific Evasion, BlockChance, BlockAmount | snapshot via Stat.CalculateSpecificValue |
| `Def` | player defense vs location's preferred weapon | snapshot DefenseData, Locations Config |
| `K(5,day)` | ability EHP curve | simulation.ability_multipliers |

**输出**：per-unit Damage written to EnemySpawnerConfig.Stats[Damage].BaseValue

**代码**：[Simulation/SimulationConfig.cs:130-159](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L130)；[Services/Damage/DamageCalculator.cs:24-62](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L24)

**状态依据**：Same native method. Recomputed damage matches the scene exactly for 118 of 706 spawners and within 5% for 504.

**配置参数**：`EnemySpawnerConfig.Difficulty`；`EnemySpawnerConfig.Count`；`EnemySpawnerConfig.Stats[AttackCooldown]`；`EnemySpawnerConfig.AverageFightTime`；`Locations Config.Locations[].PreferredWeaponType`；`Combat Balance Config.Interpolations (AbilityEHP)`

**使用方**：simulation.bake_apply

**备注**：Design intent: over AverageFightTime the group drains Difficulty × player max HP on top of the player's battle regen. At most 4 units attack at once (SimulationConfig.MaxSimultaneousAttackers=4).

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression`：n = min(4, (float)(Count+1)/2) (float division, so Count=2 gives 1.5 and Count=4 gives 2.5; it is not an integer attacker count). Rest unchanged: needDpsUnit = (BR + HP_player*D/AverageFightTime)/n; perHit = needDpsUnit*(AttackCooldown/(1-ev))/(1-bc*ba); solve ApplyDamageDefense(a, Def[PreferredWeaponType(L)])*(1/(1+K5)) >= perHit (doubling from 1, then bisection until upper-lower <= 1, returning the upper bound); NewDamage = Math.Round(max(10,a)/10)*10 (banker's rounding)  
  依据：[Simulation/SimulationConfig.cs:139](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L139) `float enemies = Math.Min(4f, (float)unchecked(c.Count + 1) / 2f);`; DamageCalculator.cs:31-52 returns `upper` once upper-lower<=1.

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

### `simulation.snapshot_growth_curve`

**Simulated player power vs day (baked balance target)**

- 状态：**纯数据**（`data_only`）
- 类型：`lookup_table`
- 来源：子系统梳理

```text
P(day) table: Health TotalValue 100 (d0), 1.6e3 (d1), 9.1e3 (d3), 9.5e4 (d10), 4.2e5 (d21), 1.66e6 (d33), 5.8e6 (d51), 2.4e7 (d77), 9.5e7 (d115), 4.5e8 (d170), 1.24e9 (d250), 2.74e9 (d405). Max DpsData 9.2 -> 345 -> 1.7e3 -> 1.3e4 -> 6.5e4 -> 3.0e5 -> 1.3e6 -> 6.8e6 -> 2.5e7 -> 1.15e8 -> 3.8e8 -> 9.3e8. Daily growth: HP ×1.45/day (d4-6), ×1.12 (d20-33), ×1.05 (d50-80), ×1.03 (d100-155), ×1.01 (d200-250), ×1.004 (d400)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `P(day)` | snapshot stats/DPS/defense/items | Simulation Data.TimeChunkSnapshots |

**输出**：target power curve used by all bakes

**代码**：[Simulation/SimulationData.cs:9-12](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationData.cs#L9)；[Simulation/SimulationSnapshot.cs:7-24](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationSnapshot.cs#L7)

**状态依据**：Produced by the stub bot and stored as data. Stats present: keys 0-9, 12-17, 20-31, 33, 34. Items (Key, Level, Copies) grow from 1 to 45.

**配置参数**：`Simulation Data.Snapshots`；`Simulation Data.TimeChunkSnapshots`

**使用方**：simulation.bake_spawner_health；simulation.bake_spawner_damage；simulation.forecast_snapshot；[BossTower/BossSpawnService.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/BossSpawnService.cs):BakeStatsForFloor

**备注**：This is the most important single lever. Every spawner's HP and damage is linear in this curve at its OptimumTime. A 10% change in simulated DPS at day d changes the HP of every spawner baked at d by 10%.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：Add: the bot reaches Location 17 at day 154 and stays there through day 405. Locations Config defines only 17 locations, so every spawner is on islands 1-17. Items plateau at 45 from day 187. Growth after day 154 therefore comes only from farming and the feature curves (K2/K3/ability), which explains the slowing daily growth.  
  依据：Simulation Data.TimeChunkSnapshots Location column; Locations Config.asset Number 1..17.

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

### `simulation.forecast_damage_ramp_average`

**Average event damage-ramp multiplier**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
if T<=0 or p<=0: 1; d = max(0, delay); if d >= T: 1; else RampAvg = 1 + (T - d)^2 * p / (2T)  (= mean over [0,T] of 1 + p*max(0, t - d))
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `delay` | 5 s | AutoBalanceInput.DamageRampDelaySeconds |
| `p` | 0.04 per s | AutoBalanceInput.DamageRampPercentPerSecond |
| `T` | boss fight seconds | presets |

**输出**：divisor for boss damage

**代码**：[Simulation/Forecast/ForecastSnapshots.cs:191-196](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastSnapshots.cs#L191)；[Services/Damage/DamageCalculator.cs:77-78](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L77)

**状态依据**：No annotation. It is consistent with the runtime event ramp in DamageCalculator: attack × (1 + 0.04 × max(0, timer − 5)).

**配置参数**：`Crystal Event Roster Config.ForecastSettings.AutoBalanceInput.DamageRampDelaySeconds`；`...DamageRampPercentPerSecond`

**使用方**：simulation.forecast_auto_balanced_enemy

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `code_refs`：Services/Damage/DamageCalculator.cs:78-79  
  依据：Line 78 is the IsEvent check and line 79 is `attack *= Math.Max(0f, attacker.EventDamageRampTimer - 5f) * 0.04 + 1.0`.

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

### `simulation.forecast_event_plan`

**Event plan assembly**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`lookup_table`
- 来源：子系统梳理
- 原生地址：`0x13f5ee0`

```text
Bosses[i] = {Minute = EventBossMinutes[i], Enemy = snapshot_i Boss, Reward = snapshot_i reward}; Golem = first snapshot Golem at minute 0; GrandBoss = last snapshot GrandBoss at max(0, EventEndMinute); resists/attack type copied from input
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `EventEndMinute` | 200 | AutoBalanceInput.EventEndMinute |

**输出**：ForecastEventPlan

**代码**：[Simulation/Forecast/ForecastSnapshots.cs:115-147](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastSnapshots.cs#L115)

**状态依据**：Native VA comment.

**配置参数**：`AutoBalanceInput.EventBossMinutes`；`AutoBalanceInput.EventEndMinute`

**使用方**：Events crystal roster

**备注**：EventBossCount, EventBossGamma and EventLastBossMinute are not used here. ForecastHelper.SetEventSettings(grandBossMinute, lastBossMinute, bossCount, bossGamma) is a stub that presumably generated EventBossMinutes, e.g. minute_i = last × (i/count)^gamma.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：That guess contradicts the shipped data. EventBossCount=10, EventBossGamma=1 and EventLastBossMinute=150, yet EventBossMinutes has 20 irregular entries (0,10,25,40,55,60,80,100,120,135,150,165,180,195,210,300,400,500,600,700), so the list was hand-edited or made another way. RandomBossResists is also unused by the recovered BuildCore.  
  依据：Crystal Event Roster Config.asset lines 32-57; ForecastSnapshots.cs:111-112 uses EventBossMinutes.Count only.

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

**使用方**：simulation.rpm_time_chunk；meta.rewarded_ad_stat_value；meta.bake_stat_reward；meta.grant_reward_stat；meta.crystal_event_mob_stat_reward

**备注**：The extractor must export ABTest Data Config.asset (all variants and per-location overrides). Scene and config values alone do not determine live RPM, enemy HP/damage, spawner cooldowns or charges.

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
