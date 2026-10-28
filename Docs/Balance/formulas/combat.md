# 战斗结算（`combat`）

[返回公式清单总览](../FormulaCatalog.md)

## 概述

Combat runs on a single hit resolver, DamageCalculator.Calculate (native VA 0x140a4cc), which has two branches. Player hits on enemies are multiplicative: (typed%Dmg x (Damage stat + item damage + typed flat + ability flat)) x (1 - ArmorConfig resist for the weapon type), then crit, super-crit, ability, event, GlobalAttack and island-medal multipliers. Enemy hits on the player go through a soft flat-defence curve, f(A, D) = A * exp(-0.75 D/A) * max(0, 1 - 0.25 D/A), so damage reaches zero once D >= 4A. In that curve D = (universal flat + matching armor items + typed flat) x typed defence% stat, which has base 1 and acts as a multiplier on flat defence, not as a percent reduction. After the curve come universal%, dodge and block rolls, rage, event, island-medal and GlobalDefence divisors. Crit, evasion, block, GlobalAttack/Defence and move speed do not use the raw stat. They use diminishing-return 'specific value' power curves; evasion and block are capped at 0.8, and super-crit damage is hard-coded to 2.0 unless post-formula modifiers apply. Pacing comes from item AttackCooldown divided by an ability attack-speed product (x2.5 vs weak or golem targets). The hit lands at an animation cast point that shifts with movement for cut and smash weapons. Enemies hit on their AttackCooldown stat inside AttackRange. The same defence curve is inverted by bisection (SolveAttackDamage) so the simulation and boss tower can calibrate enemy Damage to a target incoming DPS. Several runtime pieces are stubs: ArmorFormulas.Get, HammerRegenSystem, PlayerHitsTakenDecaySystem, CreeperExplosionSystem and UnitAutoTargetSystem.

**核验结论**：The catalog holds up overall. I checked every expression against the code. The core resolver matches line by line: Calculate at DamageCalculator.cs:64-206 (both branches, operator order, banker's Math.Round, the event ramp, tower, rage, island and event wrappers, and Undodgeable gating). The defence curve and solver, the specific-value curves (Stat.cs:61-79), StatBase aggregation and merge, the player attack cycle and cast point, targeting, enemy regen, revive, the death checks and all four stubs also match. Line numbers are accurate except one EnemyUnit ref. Data claims I re-measured are correct: Player Config base stats, the 20 ArmorConfig assets and their resist tables, the per-asset usage counts over the 717 spawners, spawner WeaponType counts 474/147/96, spawner stats limited to types 0-5, weapon cooldowns and FlatDamage, and no prefab or scene using UnitAutoTargetSystem. Three corrections matter most. (1) SolveAttackDamage has no VA comment, so it should be reconstructed, the same as its sibling functions. (2) Lethal-ignore was misread: per-target mode does not fall back to the chance roll. (3) The claim that SimulationSystem.CalculateDefenseData matches runtime defence is false: it drops universal flat and universal armor and adds an interpolation coefficient. The island-mastery incoming half also needs to be labelled reconstructed. Smaller fixes cover the enemy attack cadence (flying enemies attack while retreating; escaping and vampire enemies never attack), the ResetCooldown and no-target cancel paths in the weapon cycle, and simulation-mode rounding and consumption. The crit-chance-buff open question is mostly answered: it flows through AbilityCritChanceBonus. Five in-scope formulas were missing: temporary modifier expiry (0x15ccda8), stun duration and its effect on enemy DPS (0x15cd31c), the 60 m far/active culling radius, the player movement displacement (0x15cbf80), and the live rescale when the player's A/B test group for the enemy HP/damage multiplier changes. I rejected nothing; every cataloged item exists in code.

## 公式索引

| ID | 名称 | 状态 | 类型 |
|---|---|---|---|
| [`combat.flat_defense_curve`](#combatflat_defense_curve) | Flat defence mitigation curve (ApplyDamageDefense) | 重建（无原生标注） | `runtime_formula` |
| [`combat.solve_attack_for_incoming`](#combatsolve_attack_for_incoming) | Inverse defence: attack needed for target incoming damage | 重建（无原生标注） | `runtime_formula` |
| [`combat.battle_regen`](#combatbattle_regen) | Battle regen budget (difficulty-scaled sustain) | 重建（无原生标注） | `runtime_formula` |
| [`combat.player_flat_defense`](#combatplayer_flat_defense) | Player effective flat defence vs a damage type | 原生证据恢复 | `runtime_formula` |
| [`combat.event_enemy_damage_ramp`](#combatevent_enemy_damage_ramp) | Event enemy damage ramp-up | 原生证据恢复 | `runtime_formula` |
| [`combat.incoming_damage_to_player`](#combatincoming_damage_to_player) | Enemy -> player hit resolution | 原生证据恢复 | `runtime_formula` |
| [`combat.dodge_block_rolls`](#combatdodge_block_rolls) | Chance rolls (dodge / block / crit) | 原生证据恢复 | `runtime_formula` |
| [`combat.outgoing_damage_to_enemy`](#combatoutgoing_damage_to_enemy) | Player weapon -> enemy hit resolution | 原生证据恢复 | `runtime_formula` |
| [`combat.outgoing_damage_simulation_mode`](#combatoutgoing_damage_simulation_mode) | Expected-value hit used by simulation (fromSimulation=true) | 原生证据恢复 | `runtime_formula` |
| [`combat.crit_and_supercrit`](#combatcrit_and_supercrit) | Crit / super-crit chance and multipliers | 原生证据恢复 | `runtime_formula` |
| [`combat.global_attack_multiplier`](#combatglobal_attack_multiplier) | GlobalAttack multiplier | 重建（无原生标注） | `runtime_formula` |
| [`combat.global_defence_multiplier`](#combatglobal_defence_multiplier) | GlobalDefence multiplier | 重建（无原生标注） | `runtime_formula` |
| [`combat.island_mastery_multipliers`](#combatisland_mastery_multipliers) | Island medal (mastery) damage multipliers | 原生证据恢复 | `runtime_formula` |
| [`combat.event_crystal_damage_multipliers`](#combatevent_crystal_damage_multipliers) | Crystal event damage multipliers (wrappers) | 原生证据恢复 | `runtime_formula` |
| [`combat.event_golem_damage_multiplier`](#combatevent_golem_damage_multiplier) | Event golem damage bonus | 原生证据恢复 | `runtime_formula` |
| [`combat.boss_tower_damage_multipliers`](#combatboss_tower_damage_multipliers) | Boss tower damage dealt / taken | 原生证据恢复 | `runtime_formula` |
| [`combat.rage_damage_reduction`](#combatrage_damage_reduction) | Rage incoming damage reduction | 原生证据恢复 | `runtime_formula` |
| [`combat.weapon_type_defence_mapping`](#combatweapon_type_defence_mapping) | Damage type -> defence stat mapping | 重建（无原生标注） | `lookup_table` |
| [`combat.enemy_armor_resist_table`](#combatenemy_armor_resist_table) | Enemy ArmorConfig resist table | 纯数据 | `lookup_table` |
| [`combat.event_enemy_armor_override`](#combatevent_enemy_armor_override) | Event enemy resist/stat override at spawn | 原生证据恢复 | `runtime_formula` |
| [`combat.quest_mob_armor_and_stats`](#combatquest_mob_armor_and_stats) | Quest chain mob armor and combat stats | 重建（无原生标注） | `runtime_formula` |
| [`combat.stat_value_aggregation`](#combatstat_value_aggregation) | Stat raw value aggregation (StatBase.CalculateValue) | 重建（无原生标注） | `runtime_formula` |
| [`combat.stat_modifier_merge`](#combatstat_modifier_merge) | Modifier stacking on AddModifier | 重建（无原生标注） | `runtime_formula` |
| [`combat.stat_specific_value_curves`](#combatstat_specific_value_curves) | Specific-value (diminishing-return) conversion per stat | 重建（无原生标注） | `runtime_formula` |
| [`combat.player_lifesteal_on_hit`](#combatplayer_lifesteal_on_hit) | Player lifesteal per weapon hit | 原生证据恢复 | `runtime_formula` |
| [`combat.enemy_lifesteal_on_hit`](#combatenemy_lifesteal_on_hit) | Enemy lifesteal when damaging player | 原生证据恢复 | `runtime_formula` |
| [`combat.thorns_reflection`](#combatthorns_reflection) | Player thorns on being attacked | 原生证据恢复 | `runtime_formula` |
| [`combat.enemy_reflection`](#combatenemy_reflection) | Enemy reflection on being hit | 原生证据恢复 | `runtime_formula` |
| [`combat.lethal_damage_ignore`](#combatlethal_damage_ignore) | Lethal damage ignore (cheat death) | 原生证据恢复 | `runtime_formula` |
| [`combat.block_to_supercrit_bonus`](#combatblock_to_supercrit_bonus) | Block grants super-crit chance | 原生证据恢复 | `runtime_formula` |
| [`combat.first_vulnerability_multiplier`](#combatfirst_vulnerability_multiplier) | First-hit vulnerability multiplier | 原生证据恢复 | `runtime_formula` |
| [`combat.attack_speed_multiplier`](#combatattack_speed_multiplier) | Weapon attack-speed multiplier | 原生证据恢复 | `runtime_formula` |
| [`combat.player_weapon_attack_cycle`](#combatplayer_weapon_attack_cycle) | Player weapon attack cycle / cooldown | 原生证据恢复 | `runtime_formula` |
| [`combat.animation_cast_point`](#combatanimation_cast_point) | Movement-adjusted animation cast point | 原生证据恢复 | `runtime_formula` |
| [`combat.player_attack_range_and_targeting`](#combatplayer_attack_range_and_targeting) | Player attack range and auto-target | 原生证据恢复 | `runtime_formula` |
| [`combat.enemy_attack_cycle`](#combatenemy_attack_cycle) | Enemy attack cadence | 重建（无原生标注） | `runtime_formula` |
| [`combat.enemy_leash_and_aggro`](#combatenemy_leash_and_aggro) | Enemy leash heal, aggro and retreat | 重建（无原生标注） | `runtime_formula` |
| [`combat.enemy_regen`](#combatenemy_regen) | Enemy HP regeneration | 重建（无原生标注） | `runtime_formula` |
| [`combat.player_regen`](#combatplayer_regen) | Player HP regeneration | 原生证据恢复 | `runtime_formula` |
| [`combat.death_threshold`](#combatdeath_threshold) | Death threshold and enemy death processing | 原生证据恢复 | `runtime_formula` |
| [`combat.player_revive`](#combatplayer_revive) | Player revive | 原生证据恢复 | `runtime_formula` |
| [`combat.rage_extra_health`](#combatrage_extra_health) | Rage max-HP bonus | 重建（无原生标注） | `runtime_formula` |
| [`combat.rage_extra_hit`](#combatrage_extra_hit) | Rage-gained extra hit | 重建（无原生标注） | `runtime_formula` |
| [`combat.enemy_spawn_hp_damage_ab_multiplier`](#combatenemy_spawn_hp_damage_ab_multiplier) | Enemy HP/Damage A/B multiplier at spawn | 重建（无原生标注） | `runtime_formula` |
| [`combat.hammer_regen_stub`](#combathammer_regen_stub) | Hammer regen bonus (graph upgrade) | 空壳 | `runtime_formula` |
| [`combat.hits_taken_decay_stub`](#combathits_taken_decay_stub) | Hits-taken counter decay | 空壳 | `runtime_formula` |
| [`combat.creeper_explosion_stub`](#combatcreeper_explosion_stub) | Creeper mob explosion damage | 空壳 | `runtime_formula` |
| [`combat.unit_auto_target_stub`](#combatunit_auto_target_stub) | UnitAutoTargetSystem (MonoBehaviour) | 空壳 | `runtime_formula` |
| [`combat.armor_formulas_stub`](#combatarmor_formulas_stub) | ArmorFormulas.Get(level) | 空壳 | `runtime_formula` |
| [`combat.temporary_modifier_expiry`](#combattemporary_modifier_expiry) | Temporary (ability) stat modifier expiry | 原生证据恢复 | `runtime_formula` |
| [`combat.stun_duration`](#combatstun_duration) | Stun duration countdown and effects | 原生证据恢复 | `runtime_formula` |
| [`combat.enemy_far_activation`](#combatenemy_far_activation) | Enemy far/active culling radius | 重建（无原生标注） | `runtime_formula` |
| [`combat.player_movement`](#combatplayer_movement) | Player movement displacement | 原生证据恢复 | `runtime_formula` |
| [`combat.ab_enemy_hp_damage_live_rescale`](#combatab_enemy_hp_damage_live_rescale) | Live A/B cohort change rescale of enemy HP/Damage | 重建（无原生标注） | `runtime_formula` |
| [`combat.weapon_equip_warmup_and_primary_range_gate`](#combatweapon_equip_warmup_and_primary_range_gate) | Weapon equip warm-up and weapon_1 range gate | 原生证据恢复 | `runtime_formula` |
| [`combat.cooldown_reset_cycle`](#combatcooldown_reset_cycle) | Ability cooldown reset and highest-DPS weapon re-trigger | 原生证据恢复 | `runtime_formula` |
| [`combat.mark_meta_synergies`](#combatmark_meta_synergies) | Mark procs and mark-scaled lifesteal (meta and passive) | 原生证据恢复 | `runtime_formula` |
| [`combat.devour_reward_delay`](#combatdevour_reward_delay) | Devour animation delay before rewards | 原生证据恢复 | `runtime_formula` |

## 公式

### `combat.flat_defense_curve`

**Flat defence mitigation curve (ApplyDamageDefense)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
r = D / A ; incoming(A, D) = A * exp(-0.75 * r) * max(0, 1 - 0.25 * r)   (returns 0 if A <= 0). Damage is 0 when D >= 4A; at D = A it is 0.354 A; at D = 0.5A it is 0.601 A
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `A` | attacker raw attack damage (after event ramp / tower reduction) | combat.incoming_damage_to_player (enemy Damage stat) |
| `D` | effective flat defence of target | combat.player_flat_defense |

**输出**：pre-percent incoming damage to player

**代码**：[Services/Damage/DamageCalculator.cs:24-29](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L24)

**状态依据**：No VA comment on the method. Its operand order (ratio*-0.75, ratio*-0.25+1.0) looks like decompiler FMA output. Native-annotated callers (SimulationSystem.GetSpawnerDps, VA 0x13ef8d4) use the same function, and SimulationSystem.CalculateDefenseData builds D the same way.

**配置参数**：`Enemy spawner Stats[Type=3 Damage]`；`Player Config.Stats[14..21,26]`；`item armor values (CombatBalanceConfig armor base/growth)`

**使用方**：combat.incoming_damage_to_player；combat.solve_attack_for_incoming；simulation.GetSpawnerDps ([Simulation/Systems/SimulationSystem.cs:970](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L970))

**备注**：Constants 0.75 and 0.25 are the main defence tuning levers and have no config source. f is monotonic in A, which is what makes the bisection inverse well defined.

### `combat.solve_attack_for_incoming`

**Inverse defence: attack needed for target incoming damage**

- 状态：**重建（无原生标注）**（`reconstructed`），核验前为 `recovered_native`
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
find minimal A (absolute tolerance 1.0) s.t. forward(A) >= T; forward(A) = incoming(A, D) * p. Algorithm: upper = 1; while forward(upper) < T: upper *= 2; then bisect [0, upper] until upper - lower <= 1; return upper
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `T` | target incoming damage per hit | caller (simulation bake / forecast / boss tower) |
| `D` | player flat defence | combat.player_flat_defense (forecasted) |
| `p` | percent multiplier applied after the curve | caller (e.g. 1/(1+defenseCoefficient)) |

**输出**：enemy attack Damage stat value that yields T after defence

**代码**：[Services/Damage/DamageCalculator.cs:31-52](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L31)；[Services/Damage/DamageCalculator.cs:54-57](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L54)

**状态依据**：No VA comment, but the body contains decompiler artifacts: an int.MaxValue loop counter and Debug.LogError(upper) on exhaustion. It is clearly a port of native control flow.

**配置参数**：`Spawner AverageFightTime`；`Spawner Difficulty`；`Simulation Config`

**使用方**：simulation.SimulationConfig bake ([Simulation/SimulationConfig.cs:154](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L154))；simulation.ForecastSnapshots ([Simulation/Forecast/ForecastSnapshots.cs:178](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastSnapshots.cs#L178))；boss_tower.BossSpawnService enemy damage calibration ([BossTower/BossSpawnService.cs:473](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/BossSpawnService.cs#L473))

**备注**：This formula ties authored enemy Damage values to the designer's intended incoming DPS. If combat.flat_defense_curve changes, every baked enemy Damage becomes stale.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [已应用] `status`：reconstructed  
  依据：[Services/Damage/DamageCalculator.cs:31-57](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L31) has no native VA comment. No recovery notes outside the .cs files mention it (I grepped the repo). The int.MaxValue counter and LogError are only shape evidence, the same kind the catalog itself rejected for ApplyDamageDefense (lines 24-29, labelled reconstructed) and GetBattleRegen. One rule should apply to all three, so treat this as unconfirmed.

### `combat.battle_regen`

**Battle regen budget (difficulty-scaled sustain)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
battleRegen = (d + 1) * healthRegen + (2d + 3) * lifestealFlat
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `d` | spawner difficulty | EnemySpawnerConfig.Difficulty / boss difficulty |
| `healthRegen` | player HP regen per second (forecast) | player stat HealthRegen (specific value) |
| `lifestealFlat` | player flat lifesteal per hit (forecast) | player stat LifestealFlat |

**输出**：HP/s the enemy must out-damage in balance models

**代码**：[Services/Damage/DamageCalculator.cs:59-62](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L59)

**状态依据**：No VA comment. The decompiler-style 'difficulty + difficulty + 3.0' suggests a direct port.

**配置参数**：`EnemySpawnerConfig.Difficulty`；`Player Config.Stats[Type=2,22]`

**使用方**：simulation.SimulationConfig bake ([Simulation/SimulationConfig.cs:142](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L142))；simulation.SimulationSystem ([Simulation/Systems/SimulationSystem.cs:663](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L663),686-689)；boss_tower.BossSpawnService ([BossTower/BossSpawnService.cs:470](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/BossSpawnService.cs#L470))

**备注**：The model only uses it; the runtime fight does not. In BossSpawnService the target per-hit damage is ((regen + d*playerHP/AverageFightTime)/attackers) * (cooldown/(1-evasion)) / (1 - blockChance*blockAmount).

### `combat.player_flat_defense`

**Player effective flat defence vs a damage type**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x140a4cc`

```text
D = s * (UniversalDefenceFlat + sum_{armor items i equipped, i.DamageType in {Universal, hitType}} i.Armor + TypedDefenceFlat[hitType]) * TypedDefencePercent[hitType] ; s = simulationFlatDefenceMultiplier if fromSimulation else 1
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `UniversalDefenceFlat` | player stat 26 raw value | Player Config.Stats + modifiers |
| `i.Armor` | helmet/shield armor value | items subsystem ItemModel.CalcArmor (Player/Inventory/ItemModel.cs:489-497) |
| `TypedDefenceFlat` | Stab/Cut/Magic/Buckshot DefenceFlat raw (14/16/18/20) | Player Config.Stats + devour/shop modifiers |
| `TypedDefencePercent` | Stab/Cut/Magic/Buckshot DefencePercent raw (15/17/19/21); base 1, acts as a MULTIPLIER on flat defence | Player Config.Stats (base 1) + modifiers |
| `s` | simulation flat defence multiplier | Calculate() argument (default 1.0) |

**输出**：D for combat.flat_defense_curve

**代码**：[Services/Damage/DamageCalculator.cs:71-87](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L71)；[Services/Damage/DamageCalculator.cs:269-280](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L269)

**状态依据**：Inside Calculate, annotated 'Native ELF VA 0x140a4cc (bounded pcode)'. SimulationSystem.CalculateDefenseData (VA 0x13e5104) has the same percent*(flat+armor) form.

**配置参数**：`Player Config.Stats[Type=14..21,26]`；`ItemConfig.DamageType (armor items)`；`CombatBalanceConfig armor base/growth`

**使用方**：combat.incoming_damage_to_player；simulation.CalculateDefenseData

**备注**：Player Config has no stats 18/19 (Magic defence), so TypedDefencePercent = 0 for Magic and D = 0: Magic attacks ignore all flat defence and armor. Universal (mythical) armor counts toward every type. The percent stat multiplies all flat sources, universal flat included.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `status_evidence`：CalculateDefenseData is NOT the same as runtime. It computes D_sim = TypedPercent * (TypedFlat + sum of the top-N armor items whose DamageType == t exactly) * interpolationCoefficient(StatModifier, Rare), with N = UnlockedArmorSlots. Compared with runtime it leaves out UniversalDefenceFlat, leaves out Universal (mythical) armor items, and adds a balance-interpolation factor. The fast GetSpawnerDps path also looks D up by location.PreferredWeaponType, not by spawner.WeaponType. The runtime formula is still inside VA-annotated Calculate 0x140a4cc, so recovered_native stands, but the simulation's D does not corroborate it.  
  依据：[Simulation/Systems/SimulationSystem.cs:935-957](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L935) (CalculateDefenseData) and :964-970 (fast path uses location.PreferredWeaponType). Runtime is at [Services/Damage/DamageCalculator.cs:75](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L75),83-87.

### `combat.event_enemy_damage_ramp`

**Event enemy damage ramp-up**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x140a4cc`

```text
A_event = A * (1 + 0.04 * max(0, t - 5)) ; t (EventDamageRampTimer) increases by dt each tick while event enemy HP < maxHP - 0.01, else resets to 0
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `A` | event enemy Damage stat | event forecast (EnemySpawner.ApplyEventPlanToConfig) |
| `t` | seconds the event enemy has been below full HP | EnemyUnit.Tick runtime state |

**输出**：attack value fed to the defence curve

**代码**：[Services/Damage/DamageCalculator.cs:78-79](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L78)；[Enemy/EnemyUnit.cs:188](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L188)

**状态依据**：The multiplier sits in the VA-annotated Calculate. The timer update in EnemyUnit.Tick has no annotation, so the timer itself is reconstructed.

**配置参数**：`EnemySpawnerConfig.IsEvent`

**使用方**：combat.incoming_damage_to_player

**备注**：Event enemies gain +4% damage per second after the first 5 s of a fight. This is an anti-stall lever.

### `combat.incoming_damage_to_player`

**Enemy -> player hit resolution**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x140a4cc; ApplyDamageToPlayer 0x15cdf10`

```text
A = Damage_enemy * ramp ; if attacker.IsBossFightPart: A /= (1 + tower_damage_reduction) ; x = incoming(A, D) ; x = max((1 - UniversalDefencePercent_raw) * x, 0) ; if roll(Evasion_spec) and !Undodgeable: x = 0 (dodged) ; else { if !Undodgeable and roll(BlockChance_spec): x *= 1 - BlockAmount_spec ; if x > 0: x /= (1 + rage_damage_reduce * rageLevel) [if rage>0 and reduce>0]; x *= enemyEventMult * islandIncoming * 1/(1 + 0.01*GlobalDefence_spec) } ; if HasSpecialMultiplier: x *= SpecialMultiplier ; Damage = round_half_even(x) ; then if HasFixedDamage: Damage = FixedDamage ; godMode -> 0
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Damage_enemy` | enemy Damage stat raw | EnemySpawnerConfig.Stats[Type=3] x AB multiplier (combat.enemy_spawn_hp_damage_ab_multiplier) |
| `ramp` | event ramp | combat.event_enemy_damage_ramp |
| `D` | flat defence | combat.player_flat_defense |
| `UniversalDefencePercent_raw` | player stat 27 raw (base 0) | Player Config.Stats |
| `Evasion_spec, BlockChance_spec, BlockAmount_spec` | specific values (capped 0.8) | combat.stat_specific_value_curves |
| `rageLevel` | active rage level | RageModel.ActiveRageLevel (rage subsystem) |
| `rage_damage_reduce` | meta stat | Meta Stats Config key rage_damage_reduce (amulet upgrades) |
| `enemyEventMult` | event crystal defence multiplier | combat.event_crystal_damage_multipliers |
| `islandIncoming` | island mastery reduction | combat.island_mastery_multipliers |
| `GlobalDefence_spec` | global defence specific value | combat.global_defence_multiplier |
| `SpecialMultiplier` | per-hit special multiplier (e.g. LavaCrab explosion = 10, Undodgeable) | WeaponHitEvent producer |

**输出**：DamageResult.Damage subtracted from player CurrentHealth; DamageResult.Resist = 1 - A/Damage (display only)

**代码**：[Services/Damage/DamageCalculator.cs:69-121](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L69)；[Unit/Systems/UnitDamageSystem.cs:110-153](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L110)；[Enemy/EnemyAttackSystem.cs:342-348](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L342)；[BossTower/Systems/LavaCrabExplosionSystem.cs:116](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/Systems/LavaCrabExplosionSystem.cs#L116)

**状态依据**：Both methods carry Native ELF VA comments (bounded pcode / ARM64).

**配置参数**：`EnemySpawnerConfig.Stats`；`EnemySpawnerConfig.WeaponType`；`EnemySpawnerConfig.IsBossFightPart`；`EnemySpawnerConfig.IsEvent`；`Player Config.Stats`；`Meta Stats Config: tower_damage_reduction, rage_damage_reduce, island_medal_bonus`

**使用方**：simulation.GetSpawnerDps slow path ([Simulation/Systems/SimulationSystem.cs:984-985](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L984))；boss_tower

**备注**：Order matters: universal% applies before dodge and block. Rage, event, island and GlobalDefence apply only when x > 0 after block. The special multiplier applies even to dodged (0) hits. Math.Round uses banker's rounding. Each damaging hit increments HitsTakenCount. Enemy lifesteal and player thorns trigger from this path.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：Add: (a) Undodgeable=true turns off BOTH the dodge roll and the block roll (line 97). The simulation slow path relies on this: it sets Undodgeable and applies (1-ev)*(1-bc*ba) analytically. (b) GetEnemyEventDamageMultiplier(target, attacker) is called even when attacker is null, which would null-ref if a non-EnemyUnit source ever hits the player. (c) Thorns fire before the god-mode and damage<=0 checks, so they also fire on dodged hits and in god mode.  
  依据：[Services/Damage/DamageCalculator.cs:90-113](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L90); [Simulation/Systems/SimulationSystem.cs:981-988](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L981); [Unit/Systems/UnitDamageSystem.cs:116](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L116),124,135

### `combat.dodge_block_rolls`

**Chance rolls (dodge / block / crit)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15c4bf4`

```text
roll(p) = UnityEngine.Random.Range(0,1) < p ; dodge: roll(Evasion_spec) ; block: roll(BlockChance_spec) then dmg *= (1 - BlockAmount_spec)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `p` | probability (specific value) | combat.stat_specific_value_curves |

**输出**：boolean outcomes, block reduction factor

**代码**：[Utilities/DoubleExtensions.cs:67-71](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Utilities/DoubleExtensions.cs#L67)；[Services/Damage/DamageCalculator.cs:89-102](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L89)；[Services/Damage/DamageCalculator.cs:192-195](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L192)

**状态依据**：Roll is annotated 'Native ELF VA 0x15c4bf4'. The usage sits inside Calculate (0x140a4cc).

**配置参数**：`Player Config.Stats[Type=23,33,34]`

**使用方**：combat.incoming_damage_to_player；combat.outgoing_damage_to_enemy；combat.crit_and_supercrit

**备注**：Expected incoming multiplier = (1 - ev) * (1 - bc*ba). The simulation uses exactly this (SimulationSystem.cs:986-988). Enemies have stats 0-5 only, so player hits are never dodged except by quest mobs with an Evasion override.

### `combat.outgoing_damage_to_enemy`

**Player weapon -> enemy hit resolution**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x140a4cc; ApplyDamageToEnemy 0x15cd840`

```text
base = Pct_t * (Damage_p + W + Flat_t + AbilFlat_owner + AbilFlat_weapon) * (1 - resist_t) ; if enemy.IsBossFightPart: base *= 1 + tower_damage ; if crit: base *= CritDmg_spec * weapon.AbilityCritMultiplier * prod_states(CritDamageStackMult) ; if superCrit: base *= SuperDmg ; for each ability state: base *= state.DamageMultiplier * DamageStackMult ; if state.EmpoweredHit>0: base *= 1+EmpoweredHit (consumed) ; if weapon.NextHitEmpower>0: base *= 1+NextHitEmpower (consumed) ; base *= weapon.AbilityDamageMultiplier * owner.AbilityDamageMultiplier * max(0, 1 - UniversalDefencePercent_enemy) ; if owner.NextHitDamageBonus>0: base *= 1+NextHitDamageBonus ; base *= eventMult_player * (1 + 0.01*GlobalAttack_spec) * islandOutgoing ; if target stunned: base *= 1 + BonusDamageToStunned ; if shock consumed n>0: base *= 1 + DamagePerConsumedShockOnTarget*n ; if roll(Evasion_enemy_spec) and !Undodgeable: base = 0 ; base *= eventGolemMult ; Damage = round_half_even(base) ; then UnitDamageSystem: if firstVulnerability: Damage *= DamageModifier
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Pct_t` | typed DamagePercent raw (7/9/11/13) of weapon's DamageStatTypePercent; base 1 | ItemConfig.DamageStatTypePercent, Player Config.Stats |
| `Damage_p` | player Damage stat (3) raw; base 0 | Player Config.Stats + modifiers |
| `W` | weapon item damage | items subsystem ItemModel.Damage = CalcDamage(level, copies) (Player/Inventory/ItemModel.cs:475-484) |
| `Flat_t` | typed DamageFlat raw (6/8/10/12) via ItemConfig.DamageStatTypeFlat; devour rewards add here | Player stats + spawner StatRewards |
| `AbilFlat_owner, AbilFlat_weapon` | ability flat damage | PlayerModel.AbilityDamageFlat, ItemModel.AbilityDamageFlat (abilities subsystem) |
| `resist_t` | enemy resist for weapon damage type | combat.enemy_armor_resist_table (or WeaponHitEvent.Armor override) |
| `tower_damage` | boss tower damage bonus | Meta Stats Config key tower_damage |
| `CritDmg_spec, SuperDmg` | crit multipliers | combat.crit_and_supercrit |
| `DamageStackMult` | (base+bonus+nextProc)*stacks+1 for WeaponStatStack effects | abilities AbilityState.GetWeaponStatMultiplier (Abilities/AbilityState.cs:215-222) |
| `eventMult_player` | red crystal multiplier | combat.event_crystal_damage_multipliers |
| `GlobalAttack_spec` | global attack specific | combat.global_attack_multiplier |
| `islandOutgoing` | 1 + island_medal_bonus(location) | combat.island_mastery_multipliers |
| `eventGolemMult` | 1 + event_golem_damage for event golems | combat.event_golem_damage_multiplier |

**输出**：damage subtracted from enemy CurrentHealth

**代码**：[Services/Damage/DamageCalculator.cs:123-205](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L123)；[Unit/Systems/UnitDamageSystem.cs:286-334](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L286)；[Player/PlayerAttackSystem.cs:182-185](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L182)

**状态依据**：Annotated 'Native ELF VA 0x140a4cc' and 'Native ELF VA 0x15cd840'.

**配置参数**：`ItemConfig.DamageType`；`ItemConfig.DamageStatTypeFlat`；`ItemConfig.DamageStatTypePercent`；`ItemConfig.FlatDamage (via ItemModel)`；`ArmorConfig.Armors[].Resist`；`EnemySpawnerConfig.IsBossFightPart`；`EnemySpawnerConfig.LocationNumber`；`Meta Stats Config: tower_damage, island_medal_bonus, event_golem_damage`；`CombatBalanceConfig damage base/growth (items subsystem)`

**使用方**：simulation.GetDpsAgainstSpawner ([Simulation/Systems/SimulationSystem.cs:847](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L847))；simulation.UpdateDpsDataCache ([Simulation/Systems/SimulationSystem.cs:985](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L985))

**备注**：Core growth loop: devour rewards add Flat_t (e.g. 'Devoured' CutDamageFlat +1), which is additive with weapon damage W and then scaled by Pct_t and (1 - resist). Resist ranges from -0.9 (x1.9) to 1.0 (immune). Player weapons exist only for types 0, 1 and 3, so enemies using magic.asset (resist 1 vs 0/1/3) take 0 from weapons. Ability side effects run even in simulation mode: EmpoweredHit is consumed and HitsCounter is incremented.

### `combat.outgoing_damage_simulation_mode`

**Expected-value hit used by simulation (fromSimulation=true)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x140a4cc`

```text
same as combat.outgoing_damage_to_enemy but: critMult_exp = (CritDmg_spec - 1) * p_crit + 1 is ALWAYS applied (times AbilityCritMultiplier and CritDamage stack mults); no dodge roll; event golem mult applied; Damage = round(base). Super-crit is still RNG-rolled (IsSuperCrit = IsCrit && roll), and IsCrit is still rolled
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `p_crit` | crit chance | combat.crit_and_supercrit |
| `CritDmg_spec` | crit damage multiplier | combat.stat_specific_value_curves |

**输出**：deterministic-ish per-hit damage for DPS forecasts

**代码**：[Services/Damage/DamageCalculator.cs:136-148](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L136)；[Services/Damage/DamageCalculator.cs:193-203](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L193)

**状态依据**：The same VA-annotated method, fromSimulation branch.

**配置参数**：`Player Config.Stats[Type=24,25,28,29]`

**使用方**：simulation.GetDpsAgainstSpawner；simulation.UpdateDpsDataCache

**备注**：Simulation DPS per weapon = Damage_sim * ItemConfig.SimulationDpsWeaponMultiplier / ItemConfig.AttackCooldown (SimulationSystem.cs:848). The super-crit RNG in simulation may be a reconstruction artifact or an original quirk.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression`：Damage = round_half_even(base * eventGolemMult). Also, in simulation mode NextHitDamageBonus is still applied but not consumed, and NextHitSuperCrit*/BlockSuperCritChanceBonus are not reset. EmpoweredHit/NextHitEmpower ARE consumed and HitsCounter IS incremented.  
  依据：[Services/Damage/DamageCalculator.cs:152-168](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L152),170-174,193-203

### `combat.crit_and_supercrit`

**Crit / super-crit chance and multipliers**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x140a4cc`

```text
p_crit = enemy.SpecialAdditionalCritChancePercent + CritChance_spec + weapon.AbilityCritChanceBonus ; isCrit = any(state.GuaranteedCrit) or roll(p_crit) ; p_super = SuperCritChance_spec + weapon.AbilitySuperCritChanceBonus + owner.NextHitSuperCritChanceBonus + owner.BlockSuperCritChanceBonus ; isSuper = isCrit and roll(p_super) ; critMult = CritDamagePercent_spec (= 1.5 * v^0.08) ; superMult = SuperCritDamagePercent_spec (= 2.0 constant before post-formula mods) + weapon.AbilitySuperCritDamageBonus + owner.NextHitSuperCritDamageBonus ; crit hit multiplier = critMult * AbilityCritMultiplier * prod CritDamageStack ; super-crit multiplies further by superMult
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `CritChance_spec` | 0.08*v^0.15 (v<=700) else 0.07*v^0.1+0.082; player base v=1 -> 8% | Player Config.Stats[Type=24] + modifiers |
| `SpecialAdditionalCritChancePercent` | per-enemy bonus (Lich marks*0.01) | BossTower/Systems/LichSkeletonDeathSystem.cs:47 |
| `SuperCritChance_spec` | raw passthrough (base 0) | Player Config.Stats[Type=28] |
| `BlockSuperCritChanceBonus` | accumulated block_to_supercrit | combat.block_to_supercrit_bonus |

**输出**：crit flags and multipliers

**代码**：[Services/Damage/DamageCalculator.cs:132-148](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L132)；[Services/Damage/DamageCalculator.cs:197-201](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L197)；[Unit/Stat.cs:69-72](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L69)

**状态依据**：Inside the VA-annotated Calculate. The specific-value curves it depends on are reconstructed (see combat.stat_specific_value_curves).

**配置参数**：`Player Config.Stats[Type=24,25,28,29]`；`Meta Stats Config: block_to_supercrit`

**使用方**：combat.outgoing_damage_to_enemy

**备注**：Crit chance is not capped and can exceed 1. Super-crit damage ignores raw SuperCritDamagePercent modifiers (it returns 2.0); only PostFormula* modifiers and ability bonuses change it. After a real hit, NextHit* bonuses reset, and BlockSuperCritChanceBonus resets after a super-crit. WeaponBuffType.CritChance (3) exists but Calculate never reads it.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：WeaponBuffType.CritChance is handled by TemporaryWeaponBuffEffect. It writes AbilityState.TemporaryCritChanceBonus and, while the duration lasts, adds it into ItemModel.AbilityCritChanceBonus, which Calculate reads at line 133. Only the WeaponStatStack (GetWeaponStatMultiplier) route for CritChance goes unused, so the open question is mostly answered.  
  依据：[Abilities/Effects/TemporaryWeaponBuffEffect.cs:28](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/TemporaryWeaponBuffEffect.cs#L28),56-57; [Abilities/Systems/AbilityExecuteEventSystem.cs:61](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L61) (reset); [Services/Damage/DamageCalculator.cs:133](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L133)

### `combat.global_attack_multiplier`

**GlobalAttack multiplier**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
mult = 1 + 0.01 * GA_spec ; GA_spec = v if v <= 100 else (v - 100)^0.75 + 100 (v = raw GlobalAttack, post-formula mods applied)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `v` | GlobalAttack stat raw (30) | Player Config.Stats (base 0) + modifiers |

**输出**：outgoing damage multiplier

**代码**：[Services/Damage/DamageCalculator.cs:238-241](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L238)；[Unit/Stat.cs:73-74](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L73)

**状态依据**：No VA comment on GetGlobalAttackMultiplier or Stat.CalculateSpecificValue. It is called from the VA-annotated Calculate.

**配置参数**：`Player Config.Stats[Type=30]`

**使用方**：combat.outgoing_damage_to_enemy；simulation (cachedGlobalAttackMultiplier, SimulationSystem.cs:833)

**备注**：Diminishing returns above +100%.

### `combat.global_defence_multiplier`

**GlobalDefence multiplier**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
mult = 1 / (1 + 0.01 * GD_spec) ; GD_spec = v if v <= 100 else (v - 100)^0.75 + 100
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `v` | GlobalDefence stat raw (31) | Player Config.Stats (base 0) + modifiers |

**输出**：incoming damage multiplier

**代码**：[Services/Damage/DamageCalculator.cs:243-246](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L243)；[Unit/Stat.cs:73-74](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L73)

**状态依据**：No VA comment. Called from the VA-annotated Calculate.

**配置参数**：`Player Config.Stats[Type=31]`

**使用方**：combat.incoming_damage_to_player；simulation (cachedGlobalDefenceMultiplier)

**备注**：Hyperbolic: +100 gives x0.5 incoming.

### `combat.island_mastery_multipliers`

**Island medal (mastery) damage multipliers**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x140c264, 0x140be68, 0x140c150`

```text
eligible = !(IsEvent or IsVip or IsSpecial or IsDummy or IsBossFightPart or CrystalUnit or QuestMob) ; outgoing = eligible ? 1 + b(loc) : 1 ; incoming = eligible ? max(0, 1 - b(loc)) : 1 ; b(loc) = MetaStat island_medal_bonus.GetValue(LocationNumber)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `b(loc)` | per-island medal bonus (param = location number) | Meta Stats Config key island_medal_bonus; IslandMasteryModel (location subsystem) |
| `loc` | enemy location | EnemySpawnerConfig.LocationNumber |

**输出**：two multipliers

**代码**：[Services/Damage/DamageCalculator.cs:248-267](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L248)

**状态依据**：VA comments on all three helpers. The incoming helper notes the 'original tail math is absent in decompiler return types', so the max(0, 1-b) form is partly inferred.

**配置参数**：`Meta Stats Config: island_medal_bonus`；`EnemySpawnerConfig.LocationNumber, IsEvent, IsVip, IsSpecial, IsDummy, IsBossFightPart`

**使用方**：combat.outgoing_damage_to_enemy；combat.incoming_damage_to_player

**备注**：The incoming formula is uncertain (see open questions).

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `status`：split: outgoing 1 + b(loc) and the eligibility gate stay recovered_native (0x140be68, 0x140c264). The incoming max(0, 1 - b(loc)) should be reconstructed.  
  依据：[Services/Damage/DamageCalculator.cs:265](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L265) says outright 'original tail math is absent in decompiler return types'. The incoming expression was inferred, not ported, so a single recovered_native label claims too much.

### `combat.event_crystal_damage_multipliers`

**Crystal event damage multipliers (wrappers)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x140bd50, 0x140c070, EventBalance 0x14d8c84/0x14d8e14/0x14d8e54/0x14d8df8`

```text
applies only if enemy.IsEvent and enemy.EventDef.Key == currentEvent.Key: player outgoing x (1 + 0.02 * red') ; player incoming x 1/(1 + 0.02 * blue') ; red'/blue' = event currency count, doubled if event key == 'crystal_event_roster' or event Difficulty == 0
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `red', blue'` | red/blue crystal currency held in the current event | EventModel.GetCurrency (events subsystem) |

**输出**：event damage multipliers

**代码**：[Services/Damage/DamageCalculator.cs:208-236](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L208)；[Events/EventBalance.cs:7-35](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Events/EventBalance.cs#L7)

**状态依据**：All wrappers and EventBalance carry VA comments.

**配置参数**：`event currencies red_crystal / blue_crystal`

**使用方**：combat.outgoing_damage_to_enemy；combat.incoming_damage_to_player

**备注**：EventBalance itself belongs to the events subsystem. It is listed here because the combat wrappers gate it.

### `combat.event_golem_damage_multiplier`

**Event golem damage bonus**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x140bef8`

```text
if enemy.IsEvent and EventDef.UnitType == Mob(0): mult = 1 + event_golem_damage else 1
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `event_golem_damage` | meta stat (amulet upgrade +0.5 per node) | Meta Stats Config key event_golem_damage |

**输出**：outgoing multiplier (applied after dodge)

**代码**：[Services/Damage/DamageCalculator.cs:219-225](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L219)

**状态依据**：VA comment.

**配置参数**：`Meta Stats Config: event_golem_damage`；`EventSpawnerDef.UnitType`

**使用方**：combat.outgoing_damage_to_enemy

**备注**：'Golem' means EventUnitType 0 (Mob).

### `combat.boss_tower_damage_multipliers`

**Boss tower damage dealt / taken**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x140a4cc`

```text
outgoing vs IsBossFightPart: x (1 + tower_damage) ; incoming from IsBossFightPart attacker: A /= (1 + tower_damage_reduction) (applied to attack BEFORE the defence curve)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `tower_damage` | meta stat | Meta Stats Config key tower_damage |
| `tower_damage_reduction` | meta stat | Meta Stats Config key tower_damage_reduction |

**输出**：multipliers

**代码**：[Services/Damage/DamageCalculator.cs:80-81](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L80)；[Services/Damage/DamageCalculator.cs:141](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L141)

**状态依据**：Inside the VA-annotated Calculate.

**配置参数**：`Meta Stats Config: tower_damage, tower_damage_reduction`；`EnemySpawnerConfig.IsBossFightPart`

**使用方**：combat.outgoing_damage_to_enemy；combat.incoming_damage_to_player

**备注**：Reducing attack before the nonlinear curve means tower_damage_reduction is worth more than a post-curve multiplier when defence is high.

### `combat.rage_damage_reduction`

**Rage incoming damage reduction**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x140a4cc`

```text
if rageLevel > 0 and rage_damage_reduce > 0: incoming /= (1 + rage_damage_reduce * rageLevel)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `rageLevel` | RageModel.ActiveRageLevel | rage subsystem |
| `rage_damage_reduce` | meta stat (base 0) | Meta Stats Config key rage_damage_reduce |

**输出**：incoming multiplier

**代码**：[Services/Damage/DamageCalculator.cs:105-110](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L105)

**状态依据**：Inside the VA-annotated Calculate.

**配置参数**：`Meta Stats Config: rage_damage_reduce`

**使用方**：combat.incoming_damage_to_player

### `combat.weapon_type_defence_mapping`

**Damage type -> defence stat mapping**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`lookup_table`
- 来源：子系统梳理

```text
Stabbing(0)->StabDefenceFlat(14)/StabDefencePercent(15); Cutting(1)->16/17; Magic(2)->18/19; Buckshot(3)->20/21; Universal(4)->26/27
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `type` | WeaponType of hit | EnemySpawnerConfig.WeaponType / ItemConfig.DamageType |

**输出**：StatType pair

**代码**：[Services/Damage/DamageCalculator.cs:269-280](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L269)

**状态依据**：No VA comment. Matches item assets, whose armor DamageStatTypeFlat/Percent use the same pairs (e.g. buck_*_helmet 20/21, universal_mythical 26/27).

**配置参数**：`ItemConfig.DamageStatTypeFlat`；`ItemConfig.DamageStatTypePercent`

**使用方**：combat.player_flat_defense；simulation.CalculateDefenseData (SimulationSystem.cs:944-945)

**备注**：Weapon damage stat pairs are authored per ItemConfig: stab 6/7, cut 8/9, buckshot(smash) 12/13. The 'pierce' asset name means Stabbing and 'smash' means Buckshot.

### `combat.enemy_armor_resist_table`

**Enemy ArmorConfig resist table**

- 状态：**纯数据**（`data_only`）
- 类型：`lookup_table`
- 来源：子系统梳理

```text
resist_t = ArmorConfig.Armors.First(a => a.Type == t).Resist ; damage factor = (1 - resist_t)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ArmorConfig.Armors[]` | list of {Type: WeaponType, Resist: float} | ArmorConfig ScriptableObject assets (20 in MonoBehaviour/) |

**输出**：per-type resist

**代码**：[Enemy/ArmorConfig.cs:6-9](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/ArmorConfig.cs#L6)；[Enemy/Armor.cs:6-11](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Armor.cs#L6)；[Services/Damage/DamageCalculator.cs:125-126](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L125)；[Enemy/EnemyUnit.cs:66-72](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L66)

**状态依据**：Values are hand-authored in assets. The lookup itself is in the VA-annotated Calculate.

**配置参数**：`ArmorConfig.Armors[].Type`；`ArmorConfig.Armors[].Resist`；`EnemySpawnerConfig.ArmorConfig`

**使用方**：combat.outgoing_damage_to_enemy；simulation.GetBestArmorAgainstDps (SimulationSystem.cs:855-866)；simulation.SimulationConfig bake (clamps resist to [-0.9,0.9], SimulationConfig.cs:126-130)；UI HealthBar resist icons (|r|>0.6/0.3/0.1 thresholds)

**备注**：Asset name convention is 'Cut Stab Buckshot' resist x10, e.g. '05 -09 0' = Cut 0.5, Stab -0.9, Buckshot 0, Magic 0. '1' means 0.98. 'magic.asset' is 1/1/1 with Magic -0.9. 'Event Armor Dynamic Config' is all 0 and overwritten at runtime. Spawner usage over the 717 spawners: 09 -09 09 (150), -09 09 09 (127), 09 09 -09 (122), 0 0 0 (107), 05 05 -05 (53), 05 -05 05 (47), 05 09 -09 (31), -05 05 05 (22), 05 -09 0 (15), others fewer. No asset contains a Universal(4) entry, so a Universal-type weapon would throw in First().

### `combat.event_enemy_armor_override`

**Event enemy resist/stat override at spawn**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x150639c`

```text
for each armor a in config.ArmorConfig.Armors: a.Resist = (a.Type == eventResistType and a.Type != Magic) ? -0.9 : 0 ; config.WeaponType = enemyInfo.AttackType ; Health.BaseValue = enemyInfo.Health ; Damage.BaseValue = enemyInfo.Damage
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `eventResistType` | current event's weak type | EventModel.Data.ResistType (events subsystem) |
| `enemyInfo` | forecasted event enemy stats | EventsRepository.GetEventEnemyInfo (events/simulation subsystem) |

**输出**：mutated EnemySpawnerConfig before EnemyUnit.Initialize

**代码**：[Enemy/EnemySpawner.cs:1141-1158](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1141)

**状态依据**：Annotated 'Original APK ARM64 ELF VA 0x150639c'.

**配置参数**：`Event Armor Dynamic Config.asset`；`EventSpawnerDef`

**使用方**：combat.outgoing_damage_to_enemy

**备注**：Mutates the shared ScriptableObject in place.

### `combat.quest_mob_armor_and_stats`

**Quest chain mob armor and combat stats**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x1424d18, 0x1426018, 0x142608c, 0x1425c54`

```text
Armors: for t in {0,1,2,3}: Resist_t = (t == location.PreferredArmorType) ? -0.9 : +0.9 ; WeaponType = location.PreferredWeaponType ; Evasion: base 0 + PostFormulaFlat(MobEvasionPercent*0.01) -> spec = min(0.8, MobEvasionPercent/100) ; HealthRegen = maxHP * MobHealthRegenMaxHealthPercent * 0.01 ; LifestealPercent = MobLifestealMaxHealthPercent*0.01 ; ReflectionDamage = Damage * MobReflectionMaxHealthPercent * 0.01 ; base stats: MoveSpeed 3.5, AttackRange 3.3, AttackCooldown = chain.MobAttackCooldown ; AverageFightTime = 25 (last step) else 15
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `PreferredArmorType / PreferredWeaponType` | location defaults | Locations Config.asset |
| `Mob*Percent` | quest chain tuning | QuestChainDef (quests subsystem config) |

**输出**：quest mob EnemySpawnerConfig and stats

**代码**：[Quests/QuestMobSpawnerService.cs:191-212](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestMobSpawnerService.cs#L191)；[Quests/QuestMobSpawnerService.cs:214-217](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestMobSpawnerService.cs#L214)；[Quests/QuestMobSpawnerService.cs:234-265](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestMobSpawnerService.cs#L234)；[Quests/QuestMobSpawnerService.cs:268-283](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestMobSpawnerService.cs#L268)

**状态依据**：Has VA comments, but each one says 'independent candidate only', so the match to native is unconfirmed.

**配置参数**：`Locations Config: PreferredArmorType, PreferredWeaponType`；`QuestChainDef: MobEvasionPercent, MobHealthRegenMaxHealthPercent, MobLifestealMaxHealthPercent, MobReflectionMaxHealthPercent, MobAttackCooldown`

**使用方**：combat.outgoing_damage_to_enemy；combat.enemy_reflection；combat.enemy_regen

**备注**：Defined in the Quests folder and listed here for the ArmorConfig structure. Quest mobs are excluded from island mastery. Unlike the asset tables, quest armor resists Magic at +0.9 unless Magic is the weakness.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：Add: Health and Damage start at 0 in BuildFixedStats. They are then baked by BossSpawnService.BakeStatsForFloor(spawnerConfig, optimum, false, chain.MobAttackMultiplier, chain.MobHealthMultiplier) and scaled by the A/B EnemyHpDamageMult factor with explicit banker's rounding. Spawn config also sets AggroRange 10, AggroDuration 8 and Difficulty 1. The BuildConfig code_ref should be 191-214, not 191-212.  
  依据：[Quests/QuestMobSpawnerService.cs:95](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestMobSpawnerService.cs#L95),118-146,191-214,222-232

### `combat.stat_value_aggregation`

**Stat raw value aggregation (StatBase.CalculateValue)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
value = prod_event(1 + 0.01 * sqrt(e) * sqrt(min(e, 20))) * prod(PercentMultiplicative v) * (1 + sum PercentAdditive v) * (BaseValue + sum Flat v) * prod_{source s}(1 + sum_{PreAdditivePostMultiplicative, source s} v)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `BaseValue` | authored base | Player Config.Stats / EnemySpawnerConfig.Stats / MetaStat BaseValue |
| `Flat, PercentAdditive, PercentMultiplicative, PreAdditivePostMultiplicative, EventPercentModifier` | modifier lists by ModifierType | StatModifier from items, devour rewards, shop, upgrades, abilities |

**输出**：Stat.Value / FastValue (what GetStatValue returns, except HealthRegen)

**代码**：[Unit/StatBase.cs:107-134](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatBase.cs#L107)；[Unit/StatModifier.cs:25-33](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatModifier.cs#L25)；[Unit/UnitCore.cs:107-114](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitCore.cs#L107)

**状态依据**：No VA comment in StatBase. StatModifier.ToString is annotated (0x15ca9f4) but CalculateValue is not.

**配置参数**：`Player Config.Stats[].BaseValue`；`EnemySpawnerConfig.StatRewards (devour modifiers)`

**使用方**：combat.outgoing_damage_to_enemy；combat.incoming_damage_to_player；meta stats (MetaStat derives from StatBase)；progression/devour

**备注**：PercentMultiplicative uses v directly as a factor, not (1+v). Enemies drop config modifiers at spawn (EnemyUnit.Initialize creates Stat(base, type) only). Probably shared with a stats/progression subsystem.

### `combat.stat_modifier_merge`

**Modifier stacking on AddModifier**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
if a modifier with same (Name, Source, ModifierType) exists: if type in {PercentMultiplicative, PostFormulaMultiplicative}: existing.Value *= new.Value else existing.Value += new.Value ; existing.Duration = max(existing.Duration, new.Duration)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Value` | modifier value | StatModifier |

**输出**：merged modifier

**代码**：[Unit/StatBase.cs:63-85](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatBase.cs#L63)

**状态依据**：No VA comment.

**配置参数**：`StatModifier.Name`；`StatModifier.Source`；`StatModifier.ModifierType`

**使用方**：combat.stat_value_aggregation；devour growth (repeated 'Devoured' rewards add into one Flat modifier)

**备注**：PostFormulaMultiplicative is applied as (1+v), but merged values multiply v directly, which is an inconsistent quirk (see open questions).

### `combat.stat_specific_value_curves`

**Specific-value (diminishing-return) conversion per stat**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
spec = clamp_cap( postMult * (f_type(raw) + postFlat) ) ; postMult = prod(1 + PostFormulaMultiplicative v) * (1 + sum PostFormulaAdditive v) ; postFlat = sum PostFormulaFlat v ; f: MovementSpeed: raw<10000 ? 4.6*raw^0.115 : 3*raw^0.1 + 5.8 ; Evasion: raw<700 ? 0.06*raw^0.12 : 0.05*raw^0.065 + 0.06 ; CritChance: raw<=700 ? 0.08*raw^0.15 : 0.07*raw^0.1 + 0.082 ; CritDamagePercent: 1.5*raw^0.08 ; SuperCritDamagePercent: 2.0 ; GlobalAttack/GlobalDefence: raw<=100 ? raw : (raw-100)^0.75 + 100 ; BlockChance: 0.08*raw^0.1 ; BlockAmount: 0.15*raw^0.053 ; others: raw ; cap: Evasion, BlockChance, BlockAmount -> min(spec, 0.8)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `raw` | combat.stat_value_aggregation output | Stat.Value |
| `PostFormula* modifiers` | modifier types 3,5,6 | StatModifier lists |

**输出**：Stat.SpecificValue / FastSpecificValue (GetStatSpecificValue; GetStatValue for HealthRegen)

**代码**：[Unit/Stat.cs:17](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L17)；[Unit/Stat.cs:51-59](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L51)；[Unit/Stat.cs:61-79](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L61)；[Unit/Stat.cs:81-110](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L81)；[Unit/UnitCore.cs:100-114](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitCore.cs#L100)

**状态依据**：No VA comment in Stat.cs. The constant and branch layout looks ported (piecewise curves are roughly continuous at the breakpoints), but this is unconfirmed.

**配置参数**：`Player Config.Stats[Type=0,23,24,25,29,30,31,33,34]`

**使用方**：combat.crit_and_supercrit；combat.dodge_block_rolls；combat.global_attack_multiplier；combat.global_defence_multiplier；combat.animation_cast_point (MovementSpeed)；combat.player_regen (HealthRegen via GetStatValue)；simulation forecast

**备注**：Player defaults: crit 8%, crit damage x1.5, super-crit x2.0, block amount 15%, block chance 0, evasion 0, move speed 4.6. The 0.8 cap applies to Evasion and Block only, not to CritChance. Enemy MovementSpeed uses the raw value (EnemyAttackSystem), not this curve.

### `combat.player_lifesteal_on_hit`

**Player lifesteal per weapon hit**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15d00cc`

```text
flat = LifestealFlat_raw ; pct = LifestealPercent_raw ; if target marked (stacks n>0) and mark_lifesteal>0: flat *= 1 + mark_lifesteal*n, pct *= same ; if flat>0 and target is Hydra and marked: flat *= 1 + 0.1*n ; if NextHitLifestealFlatBonus b>0: flat *= 1+b (consumed) ; HP += flat + pct * MaxHP ; HP = min(HP, MaxHP)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `LifestealFlat_raw, LifestealPercent_raw` | player stats 22, 32 | Player Config.Stats + modifiers |
| `mark_lifesteal` | meta stat | Meta Stats Config key mark_lifesteal |
| `n` | MarkComponent.Stacks on target | abilities mark system |

**输出**：player heal per hit

**代码**：[Unit/Systems/UnitDamageSystem.cs:336-366](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L336)

**状态依据**：VA comment.

**配置参数**：`Player Config.Stats[Type=22]`；`Meta Stats Config: mark_lifesteal`

**使用方**：combat.battle_regen (modeled as (2d+3)*lifestealFlat)

**备注**：Applied on every player hit, including dodged hits and hits on dummies.

### `combat.enemy_lifesteal_on_hit`

**Enemy lifesteal when damaging player**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15ce7e8`

```text
if damage > 0: enemy.HP += LifestealFlat + LifestealPercent * enemy.MaxHP (no cap)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `LifestealFlat, LifestealPercent` | enemy stats (only quest mobs set LifestealPercent) | combat.quest_mob_armor_and_stats |

**输出**：enemy heal

**代码**：[Unit/Systems/UnitDamageSystem.cs:144](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L144)；[Unit/Systems/UnitDamageSystem.cs:155-162](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L155)

**状态依据**：VA comment that explicitly notes no health cap.

**配置参数**：`QuestChainDef.MobLifestealMaxHealthPercent`

**备注**：Can overheal enemies above MaxHP.

### `combat.thorns_reflection`

**Player thorns on being attacked**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15ce604`

```text
if ThornsDamage > 0: dmg = ThornsDamage * (1 + NextThornsDamageBonus) (bonus consumed) ; attacker.HP -= dmg (no defence/resist)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ThornsDamage, NextThornsDamageBonus` | ability-granted values | PlayerModel (abilities subsystem) |

**输出**：flat damage to attacker

**代码**：[Unit/Systems/UnitDamageSystem.cs:124](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L124)；[Unit/Systems/UnitDamageSystem.cs:212-228](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L212)

**状态依据**：VA comment.

**备注**：Triggers on every enemy hit attempt, including dodged ones, because it runs before the damage<=0 early return.

### `combat.enemy_reflection`

**Enemy reflection on being hit**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15cf560`

```text
if !dodged and enemy.ReflectionDamage > 0 and !godMode: player.HP -= ReflectionDamage (flat, no defence)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ReflectionDamage` | quest mob: Damage * MobReflectionMaxHealthPercent * 0.01 | combat.quest_mob_armor_and_stats |

**输出**：damage to player

**代码**：[Unit/Systems/UnitDamageSystem.cs:252-271](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L252)；[Unit/Systems/UnitDamageSystem.cs:329](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L329)

**状态依据**：VA comment.

**配置参数**：`QuestChainDef.MobReflectionMaxHealthPercent`

**备注**：Bypasses lethal-ignore and can kill the player directly.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：It bypasses lethal-ignore and can kill the player directly. It also increments HitsTakenCount and sets HitsTakenLastUpdate, so it feeds BonusStatPerHitsTakenEffect. It applies to non-dodged hits even when the hit damage is 0, but not to dummies, because the dummy branch returns before it.  
  依据：[Unit/Systems/UnitDamageSystem.cs:255-260](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L255),314-321,329

### `combat.lethal_damage_ignore`

**Lethal damage ignore (cheat death)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15ce89c`

```text
if HP <= 0.01 after hit: if HasPerTargetLethalIgnore and sourceId not used and LethalIgnoreCooldown <= 0 -> HP = 1 ; else if Random.value < LethalIgnoreChance -> HP = 1 ; on success both flags reset to 0/false
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `LethalIgnoreChance, HasPerTargetLethalIgnore, LethalIgnoreCooldown` | ability-granted | PlayerModel (abilities subsystem) |

**输出**：survive at 1 HP

**代码**：[Unit/Systems/UnitDamageSystem.cs:145](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L145)；[Unit/Systems/UnitDamageSystem.cs:176-193](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L176)

**状态依据**：VA comment.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression`：if LethalIgnoreChance <= 0 and !HasPerTargetLethalIgnore: fail ; if HasPerTargetLethalIgnore: { id = hash(source entity) ; if id in LethalIgnoreTargetIds or LethalIgnoreCooldown > 0: fail (NO fallback to the chance roll) ; else add id } else if Random.value >= LethalIgnoreChance: fail ; success -> HP = 1, HasPerTargetLethalIgnore = false, LethalIgnoreChance = 0. The check runs only after a damaging hit, once IsDead is true (HP <= 0.01) and after enemy lifesteal has been applied.  
  依据：[Unit/Systems/UnitDamageSystem.cs:179-190](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L179). When per-target mode is on, line 184 returns false. Line 187 is the else-branch of 'if (HasPerTargetLethalIgnore)', so the chance roll happens only when per-target mode is off.

### `combat.block_to_supercrit_bonus`

**Block grants super-crit chance**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15cdf10`

```text
on player block: BlockSuperCritChanceBonus += block_to_supercrit ; reset to 0 when a super-crit lands
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `block_to_supercrit` | meta stat | Meta Stats Config key block_to_supercrit |

**输出**：additive super-crit chance

**代码**：[Unit/Systems/UnitDamageSystem.cs:118-123](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L118)；[Services/Damage/DamageCalculator.cs:134](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L134)；[Services/Damage/DamageCalculator.cs:198](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L198)

**状态依据**：Inside the VA-annotated ApplyDamageToPlayer.

**配置参数**：`Meta Stats Config: block_to_supercrit`

**使用方**：combat.crit_and_supercrit

**备注**：Evasion similarly applies evasion_mark marks to the attacker when a Stabbing weapon is equipped (UnitDamageSystem.cs:130-132).

### `combat.first_vulnerability_multiplier`

**First-hit vulnerability multiplier**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15cd840`

```text
if target has UnitFirstVulnerabilityComponent: damage *= DamageModifier (component removed)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `DamageModifier` | component field | UnitFirstVulnerabilityComponent (abilities/boss systems) |

**输出**：damage multiplier

**代码**：[Unit/Systems/UnitDamageSystem.cs:301-306](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L301)

**状态依据**：Inside the VA-annotated ApplyDamageToEnemy.

**使用方**：combat.outgoing_damage_to_enemy

**备注**：Applied after DamageCalculator, so it is not included in simulation DPS.

### `combat.attack_speed_multiplier`

**Weapon attack-speed multiplier**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x1440178, 0x1441034`

```text
as = prod_states(AttackSpeedStackMult) * weapon.AbilityAttackSpeedMultiplier * (2.5 if current target is weak-enemy or event-golem else 1) ; AttackSpeedStackMult = (base + bonus + nextProc) * stacks + 1
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `AbilityAttackSpeedMultiplier` | ability-granted | ItemModel (abilities subsystem) |
| `weak enemy` | see combat.player_attack_range_and_targeting | Meta Stats Config weak_spawner_extra_range |

**输出**：cooldown tick rate

**代码**：[Player/PlayerAttackSystem.cs:122-125](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L122)；[Player/PlayerAttackSystem.cs:171-174](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L171)；[Player/PlayerAttackSystem.cs:34-40](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L34)

**状态依据**：VA comments on Update and Attack.

**配置参数**：`Meta Stats Config: weak_spawner_extra_range, event_golem_extra_range`

**使用方**：combat.player_weapon_attack_cycle

**备注**：The player AttackCooldown stat (5, base 0) is not used for player weapons. No stat-based attack speed exists, only abilities.

### `combat.player_weapon_attack_cycle`

**Player weapon attack cycle / cooldown**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x1440178, 0x1441034, 0x1440f74`

```text
per weapon: on start Cooldown = AC ; with target: Cooldown -= as*dt ; hit fires when AttackInProgress and Cooldown <= cp*AC + as*dt ; new attack starts when Cooldown <= 0 ; => period ~= AC/as (+1 frame), hit at ~(1-cp)*AC/as after start ; without target: if Cooldown > cp*AC: Cooldown += 3*as*dt until >= AC then Cooldown = 0 (ready) ; else keep cooling ; weapon cannot attack until WearingTime >= 1 s ; ResetCooldown (rage extra hit): when Cooldown <= AnimationBackPoint*AC -> Cooldown = cp*AC + 0.15 (forces a quick extra hit)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `AC` | weapon attack cooldown (s) | ItemConfig.AttackCooldown (e.g. cut common 1.3, smash legendary 1.7, div smash legendary 3) |
| `as` | attack speed multiplier | combat.attack_speed_multiplier |
| `cp` | effective cast point | combat.animation_cast_point |
| `AnimationBackPoint` | reset window | ItemConfig.AnimationBackPoint |

**输出**：hit timing; per-weapon DPS ~= hitDamage * as / AC

**代码**：[Player/PlayerAttackSystem.cs:119-148](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L119)；[Player/PlayerAttackSystem.cs:153-191](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L153)；[Player/PlayerAttackSystem.cs:225-229](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L225)；[Player/Inventory/ItemConfig.cs:25](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemConfig.cs#L25)；[Player/Inventory/ItemConfig.cs:49](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemConfig.cs#L49)

**状态依据**：VA comments on Update, Attack and GetHitCooldownThreshold.

**配置参数**：`ItemConfig.AttackCooldown`；`ItemConfig.AnimationCastPoint`；`ItemConfig.AnimationBackPoint`

**使用方**：simulation DPS (Damage/AttackCooldown, SimulationSystem.cs:848)；items ItemModel.DPS = Damage / AttackCooldown ([Player/Inventory/ItemModel.cs:397-405](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L397))

**备注**：All equipped weapons cycle in parallel; total DPS is the sum over weapons. The weapon_1 slot additionally requires base AttackRange (no chain x3). ItemConfig.HitFrameAt60Fps = (1-cp)*AC*60 is display or editor only.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression`：without target: if Cooldown > cp*AC: Cooldown += 3*as*dt; once >= AC: Cooldown = 0 and AttackInProgress = false ; else AttackInProgress = false (pending hit cancelled) and Cooldown -= as*dt. ResetCooldown has two paths. (1) In Update, if ResetCooldown and !AttackInProgress and Cooldown <= AnimationBackPoint*AC: ResetCooldown = false, AttackInProgress = true, Cooldown = cp*AC + 0.15, which forces a hit about 0.15/as s later. (2) If ResetCooldown is set while a swing is in progress, the hit sets Cooldown = 0 when it lands, so the next swing starts on the next Attack() call.  
  依据：[Player/PlayerAttackSystem.cs:127-140](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L127) (no-target branch incl. AttackInProgress=false at 133 and 138), :142-147 (path 1), :177-181 (path 2)

### `combat.animation_cast_point`

**Movement-adjusted animation cast point**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x1440fa0, 0x1440178`

```text
k <- k + (target - k) * clamp01(1 - exp(-4 dt)), target = |joystick| * MoveSpeed_spec * 0.05 ; factor = {Cutting: 0.5, Buckshot: 1, Stabbing/Magic: 0} ; cp = 1 - (1 - AnimationCastPoint) / (factor * k + 1)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `AnimationCastPoint` | authored fraction of cycle | ItemConfig.AnimationCastPoint (0.4-0.86) |
| `MoveSpeed_spec` | player movement speed specific value | combat.stat_specific_value_curves |

**输出**：cp used for hit threshold

**代码**：[Player/PlayerAttackSystem.cs:93-95](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L93)；[Player/PlayerAttackSystem.cs:206-211](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L206)；[Player/PlayerAttackSystem.cs:26-32](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L26)

**状态依据**：VA comments with the type factor table [0, .5, 0, 1].

**配置参数**：`ItemConfig.AnimationCastPoint`；`ItemConfig.DamageType`

**使用方**：combat.player_weapon_attack_cycle

**备注**：Moving makes cut and smash hits land earlier within a cycle but does not shorten the period. It mostly affects first-hit latency.

### `combat.player_attack_range_and_targeting`

**Player attack range and auto-target**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x1440178, 0x1440b54, 0x1440a9c, 0x14419bc, 0x1441acc`

```text
R = AttackRange_raw (player base 3.4) ; R_eff = R * 3 if chain weapon equipped and target is weak enemy or event golem ; FindTarget = first enemy in EnemiesModel.Enemies (list order, not nearest) with !IsFar and dist^2 <= R_eff^2 ; keep current target while alive and dist^2 < R_eff^2, or for 0.2 s grace after leaving range ; weak(enemy) = !IsVip and !IsSpecial and !IsEvent and LocationNumber < lastUnlockedLocation and weak_spawner_extra_range has modifier with Param == LocationNumber ; eventGolem = IsEvent and UnitType == Mob and event_golem_extra_range.GetValue(0) > 0
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `AttackRange_raw` | player stat 4 | Player Config.Stats[Type=4]=3.4 |
| `weak_spawner_extra_range, event_golem_extra_range` | meta stats | Meta Stats Config |

**输出**：current target and range check

**代码**：[Player/PlayerAttackSystem.cs:88-118](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L88)；[Player/PlayerAttackSystem.cs:213-223](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L213)；[Player/PlayerAttackSystem.cs:238-275](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L238)

**状态依据**：VA comments on every method.

**配置参数**：`Player Config.Stats[Type=4]`；`Meta Stats Config: weak_spawner_extra_range, event_golem_extra_range`；`EnemySpawnerConfig.LocationNumber, IsVip, IsSpecial, IsEvent`

**使用方**：combat.attack_speed_multiplier

**备注**：Unit/UnitAutoTargetSystem.cs is not the real targeting; see combat.unit_auto_target_stub.

### `combat.enemy_attack_cycle`

**Enemy attack cadence**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
each frame (not stunned/casting/in spawn anim): AttackCooldown -= dt (if > 0) ; if dist < AttackRange (ground: dist^2 < R^2) and AttackCooldown <= 0: emit hit, AttackCooldown = AttackCooldown_stat ; enemy DPS ~= incoming(Damage) / AttackCooldown_stat ; first hit is instant (AttackCooldown = 0 at spawn)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `AttackCooldown_stat` | enemy stat 5 (s) | EnemySpawnerConfig.Stats[Type=5] |
| `AttackRange` | enemy stat 4 | EnemySpawnerConfig.Stats[Type=4] |

**输出**：hit events to player

**代码**：[Enemy/EnemyAttackSystem.cs:102-106](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L102)；[Enemy/EnemyAttackSystem.cs:140-142](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L140)；[Enemy/EnemyAttackSystem.cs:268-303](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L268)；[Enemy/EnemyAttackSystem.cs:342-348](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L342)；[Enemy/EnemyUnit.cs:313-316](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L313)

**状态依据**：EnemyAttackSystem and EnemyUnit have no VA comments.

**配置参数**：`EnemySpawnerConfig.Stats[Type=3,4,5]`；`EnemySpawnerConfig.WeaponType`；`EnemySpawnerConfig.IsStatic (static enemies never attack)`

**使用方**：combat.incoming_damage_to_player；simulation.GetSpawnerDps (damage / AttackCooldown)

**备注**：The attack animation speed is 1/AttackCooldown, which is visual only. Stun freezes the cooldown for ground enemies. Flying enemies return early on stun before the decrement.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `code_refs`：Enemy/EnemyUnit.cs:154 (AttackCooldown = 0f in Initialize). Lines 313-316 are GetAttackAnimationSpeed = 1/AttackCooldown_stat, which is visual only.  
  依据：[Enemy/EnemyUnit.cs:154](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L154) and :313-316
- [待人工合入] `notes`：Corrections and additions. (a) Flying enemies call Attack() whenever distance < AttackRange, even while retreating, while the player is unavailable or sleeping, or while a waiting marker is set. Ground enemies can only attack in the non-retreat branch. (b) Ground enemies keep decrementing AttackCooldown during retreat or player-unavailable frames. (c) IsCasting freezes cooldown and movement for ground enemies only. (d) Enemies with EnemyCanEscapeMarker (both escape handlers) and VampireSpirit-tagged enemies never call Attack. (e) The ground range test is dist^2 < R^2; the flying test is dist < R.  
  依据：[Enemy/EnemyAttackSystem.cs:83-100](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L83) (vampire/escape/stun/cast early returns), :104-105 (decrement before the retreat check), :112-135 (retreat returns before Attack), :268-303 (flying: Attack at 302-303 outside the retreat condition)

### `combat.enemy_leash_and_aggro`

**Enemy leash heal, aggro and retreat**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
if dist > 30 (ground uses dist^2 > 900) and not boss arena: HP = MaxHP ; retreat = (dist > AggroRange and AggroTimer <= 0 and player.target != enemy) or RetreatTimer > 0 ; if dist < AggroRange: AggroTimer = AggroDuration ; speed = MovementSpeed_raw * (2 if RetreatTimer > 0 else 1) ; on player revive: aggroed enemies get RetreatTimer = 3 and full HP
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `AggroRange, AggroDuration` | spawner config | EnemySpawnerConfig.AggroRange/AggroDuration |
| `MovementSpeed_raw` | enemy stat 0 raw | EnemySpawnerConfig.Stats[Type=0] |

**输出**：enemy HP reset and pursuit

**代码**：[Enemy/EnemyAttackSystem.cs:102-139](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L102)；[Enemy/EnemyAttackSystem.cs:274-287](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L274)；[Player/Commands/RevivePlayerCommandHandler.cs:33-37](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Commands/RevivePlayerCommandHandler.cs#L33)

**状态依据**：No VA comments in EnemyAttackSystem. The revive handler is annotated (0x1469b28).

**配置参数**：`EnemySpawnerConfig.AggroRange`；`EnemySpawnerConfig.AggroDuration`；`EnemySpawnerConfig.Stats[Type=0]`

**备注**：The 30 m leash full-heal prevents kiting or chip-damage across fights.

### `combat.enemy_regen`

**Enemy HP regeneration**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
HP = min(HP + (HealthRegen + (time >= LastDamageGet + 5 ? 0.05 * MaxHP : 0)) * dt, MaxHP)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `HealthRegen` | enemy stat 2 (via GetStatValue -> specific value = raw for plain stats) | EnemySpawnerConfig.Stats[Type=2] |
| `MaxHP` | enemy Health stat | EnemySpawnerConfig.Stats[Type=1] |

**输出**：enemy HP over time

**代码**：[Enemy/Systems/EnemyRegenSystem.cs:12-13](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/EnemyRegenSystem.cs#L12)；[Enemy/Systems/EnemyRegenSystem.cs:19-29](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/EnemyRegenSystem.cs#L19)

**状态依据**：No VA comment. The constants HEAL_PER_SECOND=0.05 and NO_DAMAGE_TIME_TO_HEAL=5 are named fields from metadata.

**配置参数**：`EnemySpawnerConfig.Stats[Type=1,2]`

**备注**：After 5 s without damage, an enemy heals 5% of max HP per second, so it fully heals 20 s after combat.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：From near-zero HP, a full heal takes 20 s of idle regen, which ends about 25 s after the last damage (5 s delay + 20 s). Regen is skipped for enemies with FarFromPlayerMarker, UnitWithoutRegenMarker or UnitDeadMarker.  
  依据：[Enemy/Systems/EnemyRegenSystem.cs:12-13](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/EnemyRegenSystem.cs#L12),16,26-27

### `combat.player_regen`

**Player HP regeneration**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x1455978`

```text
HP = min(HP + HealthRegen_spec * AbilityHealthRegenMultiplier * dt, MaxHP)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `HealthRegen_spec` | player stat 2 specific value (base 0.5/s) | Player Config.Stats[Type=2] |
| `AbilityHealthRegenMultiplier` | ability scalar | PlayerModel (abilities subsystem) |

**输出**：player HP over time

**代码**：[Player/Systems/PlayerRegenSystem.cs:30-38](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/PlayerRegenSystem.cs#L30)；[Unit/UnitCore.cs:107-114](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitCore.cs#L107)

**状态依据**：VA comment.

**配置参数**：`Player Config.Stats[Type=2]`

**使用方**：combat.battle_regen (model)

**备注**：No out-of-combat bonus for the player in the recovered code.

### `combat.death_threshold`

**Death threshold and enemy death processing**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15ceb94, 0x15ceed8`

```text
IsDead = CurrentHealth <= 0.01 or IsDying ; player death when HP <= 0.01 and !IsDying -> HP = max(HP, 0) ; enemy death event when IsDead and !wasDeadBefore (creeper may delay) ; OnOneshot trigger if enemy died from HP >= MaxHP - 0.0001
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `CurrentHealth` | unit HP | runtime |

**输出**：death events

**代码**：[Unit/UnitCore.cs:53](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitCore.cs#L53)；[Unit/Systems/UnitDamageSystem.cs:147-151](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L147)；[Unit/Systems/UnitDamageSystem.cs:164-174](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L164)；[Unit/Systems/UnitDamageSystem.cs:203-210](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L203)；[Unit/Systems/UnitDamageSystem.cs:330-331](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L330)；[Enemy/Systems/EnemyDeathSystem.cs:25-38](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/EnemyDeathSystem.cs#L25)

**状态依据**：VA comments on ProcessPlayerDeath and ProcessEnemyDeath. EnemyDeathSystem has none.

**使用方**：devour/rewards (UnitDiedEvent)

### `combat.player_revive`

**Player revive**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x1469b28`

```text
HP = restoreHealth ? MaxHP : 1 ; rage reset ; all enemies HP = MaxHP ; current location = 1 ; LastDamageGet -= 2 ; DeathsCount++
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `restoreHealth` | revive mode | RevivePlayerCommand |

**输出**：post-death state

**代码**：[Player/Commands/RevivePlayerCommandHandler.cs:26-42](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Commands/RevivePlayerCommandHandler.cs#L26)

**状态依据**：VA comment.

**备注**：A firecamp revive returns the player with 1 HP, and regen does the rest.

### `combat.rage_extra_health`

**Rage max-HP bonus**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
bonus = rageLevel > 0 and rage_extra_health > 0 ? rage_extra_health * rageLevel : 0 ; Health stat gets PercentAdditive modifier 'graph_rage_extra_health' = bonus (source GraphUpgrade)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `rage_extra_health` | meta stat | Meta Stats Config key rage_extra_health |
| `rageLevel` | active rage level | RageModel |

**输出**：player MaxHP multiplier (1 + other PA + bonus)

**代码**：[Player/Systems/RageExtraHealthSystem.cs:35-70](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/RageExtraHealthSystem.cs#L35)

**状态依据**：No VA comment.

**配置参数**：`Meta Stats Config: rage_extra_health`

**使用方**：combat.stat_value_aggregation

**备注**：CurrentHealth is not rescaled when MaxHP changes.

### `combat.rage_extra_hit`

**Rage-gained extra hit**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
acc += rageGained ; while acc >= T: acc -= T ; weapon with max ItemModel.Damage gets ResetCooldown = true (extra quick hit) ; T = rage_extra_hit
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `T` | rage per extra hit | Meta Stats Config key rage_extra_hit |

**输出**：extra attacks

**代码**：[Player/Systems/RageExtraHitSystem.cs:26-59](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/RageExtraHitSystem.cs#L26)；[Player/PlayerAttackSystem.cs:142-147](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L142)

**状态依据**：RageExtraHitSystem has no VA comment. The ResetCooldown handling in PlayerAttackSystem is native-annotated.

**配置参数**：`Meta Stats Config: rage_extra_hit`

**使用方**：combat.player_weapon_attack_cycle

### `combat.enemy_spawn_hp_damage_ab_multiplier`

**Enemy HP/Damage A/B multiplier at spawn**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
if !IsEvent and m != 1: Health.Base = round_half_even(Health.Base * m) ; Damage.Base = round_half_even(Damage.Base * m) ; CurrentHealth = MaxHP ; m = ABTest EnemyHpDamageMult.damageMultiplier.overridesByLocation[loc] or defaultValue
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `m` | A/B test multiplier per location | ABTestData.EnemyHpDamageMult (remote config) |

**输出**：enemy Health and Damage

**代码**：[Enemy/EnemySpawner.cs:963-976](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L963)；[ABTests/ABTestService.cs:18-19](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L18)；[Enemy/EnemyUnit.cs:140-155](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L140)

**状态依据**：TrySpawnUnit and EnemyUnit.Initialize have no VA comments.

**配置参数**：`EnemySpawnerConfig.Stats[Type=1,3]`；`ABTest EnemyHpDamageMult`

**使用方**：combat.incoming_damage_to_player；combat.outgoing_damage_to_enemy；simulation GetCachedEnemyHpDamageMultiplier

**备注**：Probably shared with the spawner subsystem. It scales both enemy HP and Damage by the same factor.

### `combat.hammer_regen_stub`

**Hammer regen bonus (graph upgrade)**

- 状态：**空壳**（`stub`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
UNKNOWN: GetFlatBonus(maxHealth) -> 0.0 ; intended: HealthRegen modifier 'graph_hammer_regen' = f(hammer_regen meta, maxHealth)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `hammer_regen` | meta stat (amulet upgrade graph_hammer_regen_3_3) | Meta Stats Config key hammer_regen |

**输出**：player regen bonus (missing)

**代码**：[Player/Systems/HammerRegenSystem.cs:27-42](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/HammerRegenSystem.cs#L27)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:392-396](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L392)

**状态依据**：All methods are empty or return 0.0, yet the system is registered (Core/SystemsInstaller.cs:82) and an upgrade grants hammer_regen.

**配置参数**：`Meta Stats Config: hammer_regen`

**使用方**：combat.player_regen

**备注**：The signature suggests regen scales with max health.

### `combat.hits_taken_decay_stub`

**Hits-taken counter decay**

- 状态：**空壳**（`stub`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
UNKNOWN: presumably HitsTakenCount = 0 when time - HitsTakenLastUpdate > OutOfCombatTimeout (5 s)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `HitsTakenCount` | incremented per damaging hit | PlayerModel |

**输出**：counter used by BonusStatPerHitsTakenEffect

**代码**：[Player/Systems/PlayerHitsTakenDecaySystem.cs:9](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/PlayerHitsTakenDecaySystem.cs#L9)；[Player/Systems/PlayerHitsTakenDecaySystem.cs:14-20](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/PlayerHitsTakenDecaySystem.cs#L14)；[Unit/Systems/UnitDamageSystem.cs:140-141](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L140)

**状态依据**：OnAwake and OnUpdate are empty; the system is registered (Core/SystemsInstaller.cs:84).

**使用方**：abilities.BonusStatPerHitsTakenEffect ([Abilities/Effects/BonusStatPerHitsTakenEffect.cs:22](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/BonusStatPerHitsTakenEffect.cs#L22))

**备注**：Without decay the counter grows forever in this build, which inflates hit-scaling abilities.

### `combat.creeper_explosion_stub`

**Creeper mob explosion damage**

- 状态：**空壳**（`stub`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
UNKNOWN: after ExplosionDelay, damage within ExplosionRadius likely = DamageMaxHealthPercent * (player or creeper) MaxHP via UnitDamageSystem
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ExplosionDelay, ExplosionRadius, DamageMaxHealthPercent` | creeper tuning | CreeperMobComponent (prefab-authored) |

**输出**：explosion damage (missing)

**代码**：[Enemy/Systems/CreeperExplosionSystem.cs:37-55](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/CreeperExplosionSystem.cs#L37)；[Enemy/Components/CreeperMobComponent.cs:10-14](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Components/CreeperMobComponent.cs#L10)

**状态依据**：All bodies are empty; the system is registered (Core/SystemsInstaller.cs:68). EnemyDeathSystem suppresses death until creeper State == Finalized.

**配置参数**：`CreeperMobComponent.DamageMaxHealthPercent`；`CreeperMobComponent.ExplosionRadius`；`CreeperMobComponent.ExplosionDelay`

**使用方**：combat.death_threshold

**备注**：Creepers may never finalize death in this build.

### `combat.unit_auto_target_stub`

**UnitAutoTargetSystem (MonoBehaviour)**

- 状态：**空壳**（`stub`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
UNKNOWN (empty Update/UpdateEnemy/UpdatePlayer)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `unit` | serialized UnitCore | component field |

**输出**：none

**代码**：[Unit/UnitAutoTargetSystem.cs:5-21](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitAutoTargetSystem.cs#L5)

**状态依据**：Empty bodies. No prefab or scene references its script GUID c017ac77278cf4efb1e0bd48ef8d79c7, so it is likely dead code in the original too.

**备注**：Real targeting is PlayerAttackSystem.FindTarget (combat.player_attack_range_and_targeting) plus EnemyAttackSystem aggro.

### `combat.armor_formulas_stub`

**ArmorFormulas.Get(level)**

- 状态：**空壳**（`stub`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
UNKNOWN: Get(level) returns 0.0
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `level` | item or armor level | caller |

**输出**：armor value by level (missing)

**代码**：[Balance/ArmorFormulas.cs:5-8](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/ArmorFormulas.cs#L5)

**状态依据**：Returns the constant 0.0, and no callers exist in the reconstructed code.

**备注**：Live armor values come from ItemModel.CalcArmor (CombatBalanceConfig armor base + growth*level, or evolution tables), so this may be a dead or legacy helper.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `used_by / notes`：Keep one entry (balance.armor_formulas_get). Set used_by to empty: ArmorFormulas.Get(level) has no callers anywhere in CodeBase. Every armor value comes from ItemModel.CalcArmor/CalcEvolutionArmor through CombatBalanceConfig.ArmorBase/ArmorGrowthPerLevelByRarity and EvolutionArmorValues. Treat it as dead code with zero model impact, not as a missing runtime formula.  
  依据：grep 'ArmorFormulas' returns only [Balance/ArmorFormulas.cs:3](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/ArmorFormulas.cs#L3). Item armor comes from [Player/Inventory/ItemModel.cs:491-517](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L491) via [Balance/CombatBalanceConfig.cs:180-203](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L180).

### `combat.temporary_modifier_expiry`

**Temporary (ability) stat modifier expiry**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：核验时补充
- 原生地址：`0x15ccda8`

```text
each frame, for each unit tagged HasTemporaryModifiers, for each stat, for each m in TemporaryModifiers (reverse): m.Duration -= dt ; if m.Duration <= 0: RemoveModifier(m) (stat value recomputed) ; VisualScaleMultiplier tweens to OriginalScale*value over 0.35 s ; TemporaryModifiers = {m : m.Source == AbilityTemporary and m.Duration > 0}. Re-adding the same (Name, Source, Type) merges values and sets Duration = max(old, new), so it refreshes rather than stacks the timer.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `m.Duration` | remaining seconds of a temporary modifier | StatModifier.Duration (set by ability effects) |
| `dt` | frame delta | runtime |

**输出**：lifetime of temporary buffs and debuffs on any stat

**代码**：[Unit/Systems/UnitBuffSystem.cs:43-65](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitBuffSystem.cs#L43)；[Unit/StatBase.cs:70](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatBase.cs#L70)；[Unit/StatBase.cs:80-81](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatBase.cs#L80)；[Unit/StatBase.cs:90](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatBase.cs#L90)

**状态依据**：UpdateTemporaryModifiers has the comment 'Native ELF VA 0x15ccda8; reverse modifier iteration...'.

**配置参数**：`Ability defs: temporary modifier durations`

**使用方**：combat.stat_value_aggregation；combat.stat_specific_value_curves

**备注**：Only Source == AbilityTemporary modifiers with Duration > 0 expire. Every other source is permanent.

### `combat.stun_duration`

**Stun duration countdown and effects**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：核验时补充
- 原生地址：`0x15cd31c`

```text
BuffStunComponent.Duration = ability value (StunEffect: context.State.GetValue()) ; each frame Duration -= dt ; remove at <= 0 and resume enemy animation. While stunned: ground and flying enemy AI returns early, so no movement, no AttackCooldown decrement and no attacks ; animator speed 0 ; player damage x (1 + BonusDamageToStunned)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Duration` | stun seconds | Abilities/Effects/StunEffect.cs:18 (ability state value); BossTower VikingsRageDebuffSystem |
| `BonusDamageToStunned` | player ability bonus | PlayerModel (abilities) |

**输出**：time an enemy is unable to act; damage-bonus window

**代码**：[Unit/Systems/UnitBuffSystem.cs:67-71](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitBuffSystem.cs#L67)；[Abilities/Effects/StunEffect.cs:18](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/StunEffect.cs#L18)；[Enemy/EnemyAttackSystem.cs:91-100](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L91)；[Enemy/EnemyAttackSystem.cs:266-267](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L266)；[Services/Damage/DamageCalculator.cs:176-181](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L176)

**状态依据**：UpdateStuns has the comment 'Native ELF VA 0x15cd31c'. The stun gating in EnemyAttackSystem is not annotated.

**配置参数**：`Ability defs (stun value)`

**使用方**：combat.enemy_attack_cycle；combat.outgoing_damage_to_enemy

**备注**：Stun cuts enemy DPS directly by pausing its attack cooldown, so the effective enemy attack period becomes AttackCooldown + stunned time.

### `combat.enemy_far_activation`

**Enemy far/active culling radius**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：核验时补充

```text
IsFar = min(|p_player - p_enemy|^2, |p_attackPos - p_enemy|^2) > 3600 (i.e. > 60 m) ; far enemies: skipped by EnemyAttackSystem (no attack, no leash heal), skipped by FindTarget (not targetable), NavMesh agent disabled
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `FarDistance` | squared activation radius | constant 3600 (EnemyVisibilitySystem) |
| `p_attackPos` | camera follow attack position | CameraFollowSystem.PositionOfAttack |

**输出**：which enemies participate in combat

**代码**：[Enemy/Systems/EnemyVisibilitySystem.cs:18](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/EnemyVisibilitySystem.cs#L18)；[Enemy/Systems/EnemyVisibilitySystem.cs:56-61](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/EnemyVisibilitySystem.cs#L56)；[Enemy/EnemyAttackSystem.cs:58](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L58)；[Player/PlayerAttackSystem.cs:218](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L218)

**状态依据**：EnemyVisibilitySystem has no VA comment.

**使用方**：combat.player_attack_range_and_targeting；combat.enemy_attack_cycle；combat.enemy_leash_and_aggro

**备注**：Interacts with the 30 m leash. The leash full heal runs only for enemies that are not far, so it applies between 30 m and 60 m.

### `combat.player_movement`

**Player movement displacement**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：核验时补充
- 原生地址：`0x15cbf80`

```text
v = CanMove ? MoveSpeed_spec : 0 ; displacement = Rotate((v*input.x*dt, v*input.y*dt), atan2(cam.z - p.z, cam.x - p.x)*Rad2Deg + 90) ; rotation rate 460 deg/s
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `MoveSpeed_spec` | movement speed specific value (4.6 at raw 1) | combat.stat_specific_value_curves (Player Config.Stats[Type=0]) |
| `input` | joystick vector (\|input\| <= 1) | Joystick.GetInput |

**输出**：player position change per frame

**代码**：[Unit/UnitMovement.cs:45-51](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitMovement.cs#L45)；[Unit/UnitMovement.cs:60-64](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitMovement.cs#L60)

**状态依据**：The comment reads 'Native ELF VA 0x15cbf80; ARM64 confirms the vector return values...'.

**配置参数**：`Player Config.Stats[Type=0]`

**使用方**：combat.animation_cast_point (same MoveSpeed_spec * |input|)

**备注**：This is the only gameplay use of the move-speed curve besides the cast-point shift. It controls kiting and engagement speed.

### `combat.ab_enemy_hp_damage_live_rescale`

**Live A/B cohort change rescale of enemy HP/Damage**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：核验时补充

```text
on cohort change, for each live non-event enemy: r = m_new(loc) / m_old(loc) ; Health.Base = round_half_even(Health.Base * r) ; Damage.Base = round_half_even(Damage.Base * r) ; if enemy was at full HP (or maxHP <= 0): CurrentHealth = new MaxHP (otherwise CurrentHealth is unchanged)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `m(loc)` | EnemyHpDamageMult.damageMultiplier override by location, else default | ABTestData.EnemyHpDamageMult |

**输出**：rescaled live enemy Health/Damage

**代码**：[ABTests/Systems/EnemyHpDamageMultSystem.cs:31-45](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/Systems/EnemyHpDamageMultSystem.cs#L31)；[ABTests/Systems/EnemyHpDamageMultSystem.cs:47-66](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/Systems/EnemyHpDamageMultSystem.cs#L47)

**状态依据**：No VA comment in EnemyHpDamageMultSystem.

**配置参数**：`ABTest EnemyHpDamageMult.damageMultiplier (defaultValue, overridesByLocation)`

**使用方**：combat.enemy_spawn_hp_damage_ab_multiplier

**备注**：Companion to the spawn-time multiplier. Repeated rounding can drift values. Modifiers on those stats are dropped because a new Stat(base, type) is created.

### `combat.weapon_equip_warmup_and_primary_range_gate`

**Weapon equip warm-up and weapon_1 range gate**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：完整性审查补充
- 原生地址：`0x1441034 (PlayerAttackSystem.Attack), 0x1440178 (Update)`

```text
A weapon may start an attack only if WearingTime >= 1.0 s, where WearingTime resets to 0 on every equip or re-apply and grows by deltaTime each frame. The weapon in slot 'weapon_1' fires only if dist^2(player, target) < AttackRange^2 using the base AttackRange stat, with no chain x3 extension. Chain slots (weapon_2..4, weapon_5_vip) may fire within the extended targeting range.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `WearingTime` | seconds since the item was equipped | ItemModel.WearingTime (runtime; reset in PlayerModel.TryEquipItemToSlot / ApplyEquippedItemsToUnit) |
| `AttackRange` | player base attack range | Player Config.asset Stats[AttackRange] |

**输出**：Whether a given weapon can attack this frame

**代码**：[Player/PlayerAttackSystem.cs:141](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L141)；[Player/PlayerAttackSystem.cs:158-163](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L158)；[Player/Models/PlayerModel.cs:792](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L792)；[Player/Models/PlayerModel.cs:818](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L818)

**状态依据**：PlayerAttackSystem.Attack is annotated 'Native ELF VA 0x1441034'.

**配置参数**：`Player Config.Stats[AttackRange]`；`Player Config.Slots[].Key`

**使用方**：combat.player_weapon_attack_cycle；simulation.dps_data

**备注**：Each loadout swap costs 1 s of lost DPS per weapon. The simulation ignores this, which is negligible for long fights. The weapon_1 restriction means the x3 weak-enemy/golem range bonus only benefits chain weapons.

### `combat.cooldown_reset_cycle`

**Ability cooldown reset and highest-DPS weapon re-trigger**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：完整性审查补充
- 原生地址：`0x15f0db8, 0x15f0f40, 0x15f3550, 0x15f35ac`

```text
The ResetCooldownEffect (Mode=Self or HighestDamageWeapon) and TriggerHighestDamageWeaponEffect set weapon.ResetCooldown = true. For HighestDamageWeapon, weapon = argmax_w DPS_w with the first weapon winning ties, and it also sets NextHitEmpower = value. Then in PlayerAttackSystem.Update: if ResetCooldown && !AttackInProgress && Cooldown <= AnimationBackPoint * AttackCooldown, then Cooldown = castPoint(w) * AttackCooldown + 0.15 and AttackInProgress = true, where castPoint(w) = 1 - (1 - AnimationCastPoint) / (factor_type * speedCoef + 1). If the reset fires mid-swing at the hit, Cooldown = 0. The empowered hit multiplies damage by (1 + NextHitEmpower), consumed once.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `DPS_w` | ItemModel.DPS = Damage / AttackCooldown | formula items.weapon_dps |
| `value` | ability state value (scaled) | formula powerups.ability_effective_value / powerups.value_scaler |
| `AnimationBackPoint, AnimationCastPoint, AttackCooldown` | weapon timing fields | ItemConfig.AnimationBackPoint / AnimationCastPoint / AttackCooldown |

**输出**：Extra attacks per trigger (effective attack-speed gain) plus a one-shot damage multiplier

**代码**：[Abilities/Effects/ResetCooldownEffect.cs:13-48](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/ResetCooldownEffect.cs#L13)；[Abilities/Effects/TriggerHighestDamageWeaponEffect.cs:13-33](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/TriggerHighestDamageWeaponEffect.cs#L13)；[Player/PlayerAttackSystem.cs:142-147](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L142)；[Player/PlayerAttackSystem.cs:177-181](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L177)；[Services/Damage/DamageCalculator.cs:160-166](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L160)

**状态依据**：Effects carry 'Original ELF VA' annotations. PlayerAttackSystem.Update is native 0x1440178.

**配置参数**：`ItemConfig.AttackCooldown`；`ItemConfig.AnimationCastPoint`；`ItemConfig.AnimationBackPoint`；`AbilityDef effects ResetCooldownEffect (4 uses in assets) / TriggerHighestDamageWeaponEffect (1 use)`

**使用方**：combat.player_weapon_attack_cycle；powerups.empower_hit

**备注**：Check that powerups.empower_hit and combat.player_weapon_attack_cycle actually include the cadence part (cooldown set to castPoint*cd + 0.15). If they do, merge this entry into them. The simulation's EV DPS (simulation.dps_data) does not include reset procs. They are only approximated by the PlayerAbilityDPSMult interpolation.

### `combat.mark_meta_synergies`

**Mark procs and mark-scaled lifesteal (meta and passive)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：完整性审查补充
- 原生地址：`0x15ebc08, 0x15ebe40, 0x15cdf10, 0x15d00cc`

```text
(1) Piercing auto-mark: on each player hit with DamageType = Stabbing on a live enemy, if any equipped ApplyMarkEffect has AppliesToAllPiercingWeapons, then add a mark when U(0,1) < 0.1 + BonusMarkChance. (2) Evasion mark: on a dodge, if any Stabbing weapon is equipped, apply floor(evasion_mark) marks to the attacker. (3) Mark lifesteal: on a player hit against a target with s > 0 mark stacks, lsFlat *= 1 + mark_lifesteal * s and lsPct *= 1 + mark_lifesteal * s. (Hydra target: an extra lsFlat *= 1 + 0.1 * s, BossTower, out of scope.) Then heal = lsFlat * (1 + NextHitLifestealFlatBonus) + lsPct * HPmax, capped at HPmax.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `BonusMarkChance` | per-frame accumulated extra mark chance | IncreaseMarkChanceEffect (runtime, reset each frame) |
| `evasion_mark` | marks applied on dodge | Meta Stats Config / amulet graph 'evasion_mark' |
| `mark_lifesteal` | lifesteal bonus per mark stack | Meta Stats Config / amulet graph 'mark_lifesteal' |
| `s` | mark stacks on target | MarkComponent.Stacks (formula powerups.marks) |

**输出**：Mark application rate and lifesteal multiplier

**代码**：[Abilities/Systems/PiercingWeaponMarkSystem.cs:30-35](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/PiercingWeaponMarkSystem.cs#L30)；[Unit/Systems/UnitDamageSystem.cs:125-133](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L125)；[Unit/Systems/UnitDamageSystem.cs:336-366](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L336)；[Abilities/Effects/IncreaseMarkChanceEffect.cs:22](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/IncreaseMarkChanceEffect.cs#L22)

**状态依据**：All three sites carry 'Native' ELF VA comments.

**配置参数**：`Meta Stats Config.evasion_mark`；`Meta Stats Config.mark_lifesteal`；`AbilityDef ApplyMarkEffect.AppliesToAllPiercingWeapons`；`AbilityDef IncreaseMaxMarksEffect / IncreaseMarkChanceEffect`

**使用方**：stats.lifesteal_player；combat.player_lifesteal_on_hit；powerups.marks

**备注**：The 10% base piercing mark chance is hard-coded. If stats.lifesteal_player / combat.player_lifesteal_on_hit omit the mark_lifesteal multiplier, those entries are incomplete. Note that the general ApplyLifesteal (enemy on player) does not cap health, while the player path caps at HPmax.

### `combat.devour_reward_delay`

**Devour animation delay before rewards**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：完整性审查补充
- 原生地址：`0x1443484`

```text
t_reward = floatingTime + catchingTime when the animation completes, or at most floatingTime + catchingTime + 0.5 s (FinishTimeout). The prefab values are floatingTime = 0.4 and catchingTime = 0.4, so t_reward is about 0.8 s, max 1.3 s. Stat, soul and currency rewards are granted in HandleDevour, not at death.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `floatingTime` | float phase duration | PlayerDevour serialized field (GameObject/Pudge.prefab: 0.4) |
| `catchingTime` | hook/catch phase duration | PlayerDevour serialized field (GameObject/Pudge.prefab: 0.4) |

**输出**：Latency between kill and stat or soul grant

**代码**：[Player/PlayerDevour.cs:74](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerDevour.cs#L74)；[Player/PlayerDevour.cs:111-135](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerDevour.cs#L111)；[Enemy/EnemySpawner.cs:1359-1382](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemySpawner.cs#L1359)

**状态依据**：PlayerDevour update loop annotated 'Native ELF VA 0x1443484'.

**配置参数**：`Pudge.prefab PlayerDevour.floatingTime`；`Pudge.prefab PlayerDevour.catchingTime`

**使用方**：spawners.stat_reward_grant；simulation.spawner_fight_model

**备注**：This is small, but it is per-kill overhead that the simulation folds into its constant RPM tuple (70, 50, 2, 2). It matters for fast-kill spawners (fight time of 1-2 s).

## 配置参数

| 资产 | 字段 | 类型 | 含义 | 用于 |
|---|---|---|---|---|
| ArmorConfig (*.asset, 20 assets e.g. '09 -09 09.asset', 'magic.asset', 'Event Armor Dynamic Config.asset') | `Armors[].Type` | WeaponType (int) | damage type the entry applies to | combat.enemy_armor_resist_table, combat.outgoing_damage_to_enemy |
| ArmorConfig | `Armors[].Resist` | float | damage factor = 1 - Resist (-0.9 weak .. 1.0 immune) | combat.enemy_armor_resist_table, combat.outgoing_damage_to_enemy, combat.event_enemy_armor_override |
| Scenes/Main.unity EnemySpawner.config | `Stats[{BaseValue,Type}] (types 0 MoveSpeed,1 Health,2 HealthRegen,3 Damage,4 AttackRange,5 AttackCooldown on all 717)` | List<Stat> | enemy combat stats | combat.incoming_damage_to_player, combat.enemy_attack_cycle, combat.enemy_regen, combat.enemy_leash_and_aggro, combat.enemy_spawn_hp_damage_ab_multiplier |
| Scenes/Main.unity EnemySpawner.config | `WeaponType` | WeaponType | enemy hit damage type (0:474, 1:147, 3:96 spawners) | combat.player_flat_defense, combat.incoming_damage_to_player |
| Scenes/Main.unity EnemySpawner.config | `ArmorConfig` | ArmorConfig ref | enemy resist table | combat.enemy_armor_resist_table |
| Scenes/Main.unity EnemySpawner.config | `AggroRange / AggroDuration` | float | pursuit radius and memory | combat.enemy_leash_and_aggro |
| Scenes/Main.unity EnemySpawner.config | `IsEvent / IsBossFightPart / IsVip / IsSpecial / IsDummy / IsStatic / IsFlying` | bool | gate multipliers (event ramp, tower, island mastery, weak enemy) and attack behaviour | combat.incoming_damage_to_player, combat.outgoing_damage_to_enemy, combat.island_mastery_multipliers, combat.player_attack_range_and_targeting, combat.enemy_attack_cycle |
| Scenes/Main.unity EnemySpawner.config | `LocationNumber` | int | island index for medal bonus and weak-enemy test | combat.island_mastery_multipliers, combat.player_attack_range_and_targeting, combat.enemy_spawn_hp_damage_ab_multiplier |
| Scenes/Main.unity EnemySpawner.config | `Difficulty / AverageFightTime` | float | balance-model inputs for regen budget and damage calibration | combat.battle_regen, combat.solve_attack_for_incoming |
| Player Config.asset | `Stats[{BaseValue,Type}]` | List<Stat> | player base stats: Health 100, MoveSpeed 1, HealthRegen 0.5, Damage 0, AttackRange 3.4, typed Flat 0 / Percent 1, CritChance 1, CritDamage 1, SuperCritDamage 1, BlockAmount 1, others 0; Magic(10,11,18,19) and LifestealPercent(32) absent | combat.player_flat_defense, combat.outgoing_damage_to_enemy, combat.stat_specific_value_curves, combat.player_regen, combat.player_attack_range_and_targeting |
| Item assets (e.g. 'cut common.asset', 'div cut epic.asset', 'buck_rare_helmet.asset') | `DamageType` | WeaponType | weapon hit type / armor coverage type (4 = universal mythical armor) | combat.outgoing_damage_to_enemy, combat.player_flat_defense, combat.animation_cast_point |
| Item assets | `DamageStatTypeFlat / DamageStatTypePercent` | StatType | which player stats add flat and percent damage (weapons 6/7, 8/9, 12/13; armor 14/15, 16/17, 20/21, 26/27) | combat.outgoing_damage_to_enemy, combat.weapon_type_defence_mapping |
| Item assets | `AttackCooldown` | float (s) | weapon cycle period (0.6-3.0 s for weapons; armor 1, unused) | combat.player_weapon_attack_cycle |
| Item assets | `AnimationCastPoint / AnimationBackPoint` | float [0,1] | fraction of cycle when the hit lands / window for cooldown reset | combat.animation_cast_point, combat.player_weapon_attack_cycle |
| Item assets | `SimulationDpsWeaponMultiplier` | float | simulation-only DPS weight (1-2) | combat.outgoing_damage_simulation_mode |
| Item assets | `FlatDamage` | float | flat damage added to item damage (legendary weapons 5000/15000/30000; smash epic 400) | combat.outgoing_damage_to_enemy |
| Meta Stats Config.asset | `tower_damage, tower_damage_reduction` | MetaStat | boss tower damage dealt / taken | combat.boss_tower_damage_multipliers |
| Meta Stats Config.asset | `rage_damage_reduce, rage_extra_health, rage_extra_hit` | MetaStat | rage combat effects | combat.rage_damage_reduction, combat.rage_extra_health, combat.rage_extra_hit |
| Meta Stats Config.asset | `island_medal_bonus` | MetaStat (param = location) | per-island damage dealt bonus / damage taken reduction | combat.island_mastery_multipliers |
| Meta Stats Config.asset | `event_golem_damage, event_golem_extra_range, weak_spawner_extra_range` | MetaStat | event golem damage, x3 range and x2.5 attack speed gates | combat.event_golem_damage_multiplier, combat.player_attack_range_and_targeting, combat.attack_speed_multiplier |
| Meta Stats Config.asset | `block_to_supercrit, evasion_mark, mark_lifesteal` | MetaStat | defensive procs that feed offence or sustain | combat.block_to_supercrit_bonus, combat.player_lifesteal_on_hit |
| Meta Stats Config.asset | `hammer_regen` | MetaStat | regen bonus (consumer stubbed) | combat.hammer_regen_stub |
| Enemy prefab CreeperMobComponent | `ExplosionDelay, ExplosionRadius, DamageMaxHealthPercent` | float | creeper explosion tuning | combat.creeper_explosion_stub |
| QuestChainDef (quest config) | `MobEvasionPercent, MobHealthRegenMaxHealthPercent, MobLifestealMaxHealthPercent, MobReflectionMaxHealthPercent, MobAttackCooldown` | float | quest mob combat stats | combat.quest_mob_armor_and_stats |
| Locations Config.asset | `PreferredArmorType / PreferredWeaponType` | WeaponType | quest mob weakness and attack type | combat.quest_mob_armor_and_stats |
| ABTestData (remote) | `EnemyHpDamageMult.damageMultiplier (defaultValue, overridesByLocation)` | float | global enemy HP and Damage scale | combat.enemy_spawn_hp_damage_ab_multiplier |

## 枚举

| 枚举 | 文件 | 取值 |
|---|---|---|
| `WeaponType` | [Enemy/WeaponType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/WeaponType.cs) | 0=Stabbing(pierce),1=Cutting(cut),2=Magic,3=Buckshot(smash/buck),4=Universal |
| `StatType` | [Unit/StatType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatType.cs) | 0=MovementSpeed,1=Health,2=HealthRegen,3=Damage,4=AttackRange,5=AttackCooldown,6=StabDamageFlat,7=StabDamagePercent,8=CutDamageFlat,9=CutDamagePercent,10=MagicDamageFlat,11=MagicDamagePercent,12=BuckshotDamageFlat,13=BuckshotDamagePercent,14=StabDefenceFlat,15=StabDefencePercent,16=CutDefenceFlat,17=CutDefencePercent,18=MagicDefenceFlat,19=MagicDefencePercent,20=BuckshotDefenceFlat,21=BuckshotDefencePercent,22=LifestealFlat,23=Evasion,24=CritChance,25=CritDamagePercent,26=UniversalDefenceFlat,27=UniversalDefencePercent,28=SuperCritChance,29=SuperCritDamagePercent,30=GlobalAttack,31=GlobalDefence,32=LifestealPercent,33=BlockChance,34=BlockAmount,35=VisualScaleMultiplier |
| `ModifierType` | [Unit/ModifierType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/ModifierType.cs) | 0=Flat,1=PercentAdditive,2=PercentMultiplicative,3=PostFormulaAdditive,4=PreAdditivePostMultiplicative,5=PostFormulaMultiplicative,6=PostFormulaFlat,7=EventPercentModifier |
| `ModifierSource` | [Unit/ModifierSource.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/ModifierSource.cs) | 0=Enemy,1=Item,2=AbilityPermanent,3=AbilityTemporary,4=Shop,5=Ads,6=Reward,7=LevelPass,8=Crystal,9=CrystalUpgrade,10=GraphUpgrade,11=MineUpgrade,12=CrystalEvent,13=CrystalPass,14=SoulPass,15=Vip,16=SecretChest,17=CrystalGuardian,18=Quest,19=Cashback |
| `Rarity` | [Player/Inventory/Rarity.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/Rarity.cs) | 0=Common,1=Uncommon,2=Rare,3=Epic,4=Legendary,5=Divine,6=Mythical |
| `ItemSlotType` | [Player/Inventory/ItemSlotType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemSlotType.cs) | 0=Weapon,1=Helmet,2=Shield |
| `WeaponBuffType` | [Abilities/Effects/WeaponBuffType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/WeaponBuffType.cs) | 0=Damage,1=AttackSpeed,2=CritDamage,3=CritChance |
| `EventUnitType` | [Enemy/EventUnitType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EventUnitType.cs) | 0=Mob(golem),1=MiniBoss,2=Boss |

## 未解决问题

1. Island mastery incoming multiplier: the code uses max(0, 1 - island_medal_bonus), but its comment says the 'original tail math is absent in decompiler return types'. It could instead be 1/(1+b) or a different curve. This needs ARM64 verification at 0x140c150.
2. Typed DefencePercent stats (15/17/19/21, base 1) multiply the whole flat defence sum, universal flat included, instead of reducing damage by a percent. Both the Calculate incoming branch and SimulationSystem.CalculateDefenseData agree. Confirm this is native and not a shared reconstruction assumption.
3. Simulation mode (fromSimulation=true) still rolls IsCrit and IsSuperCrit, consumes EmpoweredHit and NextHitEmpower, and increments ability HitsCounter. This makes forecasts stochastic and mutates state. Is that an original quirk or a reconstruction artifact?
4. Stat.CalculateSpecificValue curves (crit, evasion, block, move speed, global atk/def) and StatBase.CalculateValue have no VA annotations. Their constants look ported, but they need native confirmation because almost every combat number depends on them.
5. ApplyDamageDefense, GetBattleRegen and the GlobalAttack/GlobalDefence helpers lack VA comments. Confirm the constants 0.75, 0.25, 0.01 and (d+1)/(2d+3) against native.
6. EnemyAttackSystem, EnemyUnit (event ramp timer, initialize) and EnemyRegenSystem have no native annotations. Need to confirm the enemy leash at 30 m with full heal, the retreat speed x2, the 5 s / 5% per second idle regen, and that the attack cooldown only ticks while not stunned.
7. SuperCritDamagePercent specific value is hard-coded 2.0, so raw modifiers to stat 29 have no effect and only PostFormula* modifiers do. Is that intended, or is the curve truncated?
8. WeaponBuffType.CritChance (3) exists, but Calculate never multiplies crit chance by GetWeaponStatMultiplier(CritChance). It may be missing from the reconstruction.
9. PostFormulaMultiplicative modifiers are applied as (1+v), but StatBase.AddModifier merges duplicates by multiplying v directly, which gives (1+v1*v2) rather than (1+v1)(1+v2). Is this an original bug?
10. Stubs with live consumers are HammerRegenSystem (hammer_regen meta stat is granted by amulet upgrades), PlayerHitsTakenDecaySystem (HitsTakenCount never decays), and CreeperExplosionSystem (explosion damage and death finalization). Their formulas must be recovered from native.
11. ArmorFormulas.Get(level) is a stub with no callers. Determine whether the original had callers (UI or editor) or is dead code.
12. No ArmorConfig asset contains a Universal (4) entry, and Player Config lacks the Magic stats (10/11/18/19). Confirm that Magic hits ignore all player defence and that magic.asset enemies are immune to every player weapon type (resist 1.0).
