# 战斗结算（`combat`）

[返回公式清单总览](../FormulaCatalog.md)

## 概述

Combat runs on a single hit resolver, DamageCalculator.Calculate (native VA 0x140a4cc), which has two branches. Player hits on enemies are multiplicative: (typed%Dmg x (Damage stat + item damage + typed flat + ability flat)) x (1 - ArmorConfig resist for the weapon type), then crit, super-crit, ability, event, GlobalAttack and island-medal multipliers. Enemy hits on the player go through a soft flat-defence curve, f(A, D) = A * exp(-0.75 D/A) * max(0, 1 - 0.25 D/A), so damage reaches zero once D >= 4A. In that curve D = (universal flat + matching armor items + typed flat) x typed defence% stat, which has base 1 and acts as a multiplier on flat defence, not as a percent reduction. After the curve come universal%, dodge and block rolls, rage, event, island-medal and GlobalDefence divisors. Crit, evasion, block, GlobalAttack/Defence and move speed do not use the raw stat. They use diminishing-return 'specific value' power curves; evasion and block are capped at 0.8, and super-crit damage is hard-coded to 2.0 unless post-formula modifiers apply. Pacing comes from item AttackCooldown divided by an ability attack-speed product (x2.5 vs weak or golem targets). The hit lands at an animation cast point that shifts with movement for cut and smash weapons. Enemies hit on their AttackCooldown stat inside AttackRange. The same defence curve is inverted by bisection (SolveAttackDamage) so the simulation and boss tower can calibrate enemy Damage to a target incoming DPS. Several runtime pieces are stubs: ArmorFormulas.Get, HammerRegenSystem, PlayerHitsTakenDecaySystem, CreeperExplosionSystem and UnitAutoTargetSystem.

**核验结论**：The catalog holds up overall. I checked every expression against the code. The core resolver matches line by line: Calculate at DamageCalculator.cs:64-206 (both branches, operator order, banker's Math.Round, the event ramp, tower, rage, island and event wrappers, and Undodgeable gating). The defence curve and solver, the specific-value curves (Stat.cs:61-79), StatBase aggregation and merge, the player attack cycle and cast point, targeting, enemy regen, revive, the death checks and all four stubs also match. Line numbers are accurate except one EnemyUnit ref. Data claims I re-measured are correct: Player Config base stats, the 20 ArmorConfig assets and their resist tables, the per-asset usage counts over the 717 spawners, spawner WeaponType counts 474/147/96, spawner stats limited to types 0-5, weapon cooldowns and FlatDamage, and no prefab or scene using UnitAutoTargetSystem. Three corrections matter most. (1) SolveAttackDamage has no VA comment, so it should be reconstructed, the same as its sibling functions. (2) Lethal-ignore was misread: per-target mode does not fall back to the chance roll. (3) The claim that SimulationSystem.CalculateDefenseData matches runtime defence is false: it drops universal flat and universal armor and adds an interpolation coefficient. The island-mastery incoming half also needs to be labelled reconstructed. Smaller fixes cover the enemy attack cadence (flying enemies attack while retreating; escaping and vampire enemies never attack), the ResetCooldown and no-target cancel paths in the weapon cycle, and simulation-mode rounding and consumption. The crit-chance-buff open question is mostly answered: it flows through AbilityCritChanceBonus. Five in-scope formulas were missing: temporary modifier expiry (0x15ccda8), stun duration and its effect on enemy DPS (0x15cd31c), the 60 m far/active culling radius, the player movement displacement (0x15cbf80), and the live rescale when the player's A/B test group for the enemy HP/damage multiplier changes. I rejected nothing; every cataloged item exists in code.

## 公式索引

| ID | 名称 | 状态 | 类型 |
|---|---|---|---|
| [`combat.player_lifesteal_on_hit`](#combatplayer_lifesteal_on_hit) | Player lifesteal per weapon hit | 原生证据恢复 | `runtime_formula` |
| [`combat.enemy_lifesteal_on_hit`](#combatenemy_lifesteal_on_hit) | Enemy lifesteal when damaging the player | 原生证据恢复 | `runtime_formula` |
| [`combat.thorns_reflection`](#combatthorns_reflection) | Player thorns reflection on being attacked | 原生证据恢复 | `runtime_formula` |
| [`combat.enemy_reflection`](#combatenemy_reflection) | Enemy reflection damage to the player on being hit | 原生证据恢复 | `runtime_formula` |
| [`combat.first_vulnerability_multiplier`](#combatfirst_vulnerability_multiplier) | First-hit vulnerability multiplier | 原生证据恢复 | `runtime_formula` |
| [`combat.lethal_damage_ignore`](#combatlethal_damage_ignore) | Lethal damage ignore (cheat death) | 原生证据恢复 | `runtime_formula` |
| [`combat.flat_defense_curve`](#combatflat_defense_curve) | Flat defence mitigation curve (ApplyDamageDefense) | 重建（无原生标注） | `runtime_formula` |
| [`combat.solve_attack_for_incoming`](#combatsolve_attack_for_incoming) | Inverse defence: attack needed for target incoming damage | 重建（无原生标注） | `runtime_formula` |
| [`combat.battle_regen`](#combatbattle_regen) | Battle regen budget (difficulty-scaled sustain) | 重建（无原生标注） | `runtime_formula` |
| [`combat.player_flat_defense`](#combatplayer_flat_defense) | Player effective flat defence vs a damage type (live hit) | 原生证据恢复 | `runtime_formula` |
| [`combat.event_enemy_damage_ramp`](#combatevent_enemy_damage_ramp) | Event enemy damage ramp-up (timer + multiplier) | 原生证据恢复 | `runtime_formula` |
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
| [`combat.quest_mob_armor_and_stats`](#combatquest_mob_armor_and_stats) | Quest chain mob armor and combat stats | 重建（无原生标注） | `runtime_formula` |
| [`combat.block_to_supercrit_bonus`](#combatblock_to_supercrit_bonus) | Block grants super-crit chance | 原生证据恢复 | `runtime_formula` |
| [`combat.attack_speed_multiplier`](#combatattack_speed_multiplier) | Weapon attack-speed multiplier | 原生证据恢复 | `runtime_formula` |
| [`combat.player_weapon_attack_cycle`](#combatplayer_weapon_attack_cycle) | Player weapon attack cycle / cooldown | 原生证据恢复 | `runtime_formula` |
| [`combat.animation_cast_point`](#combatanimation_cast_point) | Movement-adjusted animation cast point | 原生证据恢复 | `runtime_formula` |
| [`combat.player_attack_range_and_targeting`](#combatplayer_attack_range_and_targeting) | Player attack range and auto-target | 原生证据恢复 | `runtime_formula` |
| [`combat.enemy_attack_cycle`](#combatenemy_attack_cycle) | Enemy attack cadence | 重建（无原生标注） | `runtime_formula` |
| [`combat.enemy_leash_and_aggro`](#combatenemy_leash_and_aggro) | Enemy leash full-heal, aggro and retreat | 重建（无原生标注） | `runtime_formula` |
| [`combat.enemy_regen`](#combatenemy_regen) | Enemy HP regeneration | 重建（无原生标注） | `runtime_formula` |
| [`combat.player_regen`](#combatplayer_regen) | Player HP regeneration | 原生证据恢复 | `runtime_formula` |
| [`combat.death_threshold`](#combatdeath_threshold) | Death threshold and enemy death processing | 原生证据恢复 | `runtime_formula` |
| [`combat.player_revive`](#combatplayer_revive) | Player revive | 原生证据恢复 | `runtime_formula` |
| [`combat.hits_taken_decay_stub`](#combathits_taken_decay_stub) | Hits-taken counter decay | 空壳 | `runtime_formula` |
| [`combat.unit_auto_target_stub`](#combatunit_auto_target_stub) | UnitAutoTargetSystem (MonoBehaviour) | 空壳 | `runtime_formula` |
| [`combat.ab_enemy_hp_damage_live_rescale`](#combatab_enemy_hp_damage_live_rescale) | Live A/B cohort change rescale of enemy HP/Damage | 重建（无原生标注） | `runtime_formula` |
| [`combat.weapon_equip_warmup_and_primary_range_gate`](#combatweapon_equip_warmup_and_primary_range_gate) | Weapon equip warm-up and weapon_1 range gate | 原生证据恢复 | `runtime_formula` |
| [`combat.cooldown_reset_cycle`](#combatcooldown_reset_cycle) | Ability cooldown reset and highest-DPS weapon re-trigger | 原生证据恢复 | `runtime_formula` |
| [`combat.mark_meta_synergies`](#combatmark_meta_synergies) | Mark procs and mark-scaled lifesteal (meta and passive) | 原生证据恢复 | `runtime_formula` |
| [`combat.devour_reward_delay`](#combatdevour_reward_delay) | Devour animation delay before rewards | 原生证据恢复 | `runtime_formula` |

## 公式

### `combat.player_lifesteal_on_hit`

**Player lifesteal per weapon hit**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 合并自：`combat.player_lifesteal_on_hit`、`stats.lifesteal_player`
- 原生地址：`0x15d00cc (ApplyPlayerLifesteal); caller ApplyDamageToEnemy 0x15cd840; BuffNextHitLifestealFlatEffect.Execute 0x15eeeb4`

```text
flat = Source.GetStatValue(LifestealFlat); pct = Source.GetStatValue(LifestealPercent); if target has MarkComponent with k = Stacks > 0 and b = Meta('mark_lifesteal') > 0: m = 1 + b*k; if flat > 0: flat *= m; if pct > 0: pct *= m; if flat > 0 and target has HydraBossComponent and Mark Stacks k > 0: flat *= 1 + k*0.1; if n = PlayerModel.NextHitLifestealFlatBonus > 0: NextHitLifestealFlatBonus = 0 (consumed even when flat == 0); flat *= 1f + n; if flat > 0 and !Source.IsDead: HP += flat; if pct > 0 and !Source.IsDead: HP += pct * Source.GetStatValue(Health); if flat > 0 or pct > 0: HP = min(HP, Source.GetStatValue(Health))
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `LifestealFlat (stat 22), LifestealPercent (stat 32)` | attacker (player) stat Values via GetStatValue (FastValue, not the specific value) | Player Config.Stats + modifiers (devour, items such as cut_rare_shield, quests) |
| `b` | meta stat mark_lifesteal | Meta Stats Config key mark_lifesteal |
| `k` | MarkComponent.Stacks on the hit target | abilities mark system (runtime) |
| `0.1` | extra flat-lifesteal multiplier per mark stack against the Hydra boss | hard-coded constant, UnitDamageSystem.cs:356 |
| `n` | PlayerModel.NextHitLifestealFlatBonus, one-shot flat multiplier | BuffNextHitLifestealFlatEffect.Execute: += scaled value if > 0 (Abilities/Effects/BuffNextHitLifestealFlatEffect.cs:14-20) |
| `Health` | player max HP = raw Health stat Value | GetStatValue(Health) |

**输出**：player heal per weapon hit (applied even on dodged or zero-damage hits and on dummy targets)

**代码**：[Unit/Systems/UnitDamageSystem.cs:336-366](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L336)；[Unit/Systems/UnitDamageSystem.cs:318](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L318)；[Unit/Systems/UnitDamageSystem.cs:332](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L332)；[Unit/UnitCore.cs:107-114](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitCore.cs#L107)；[Abilities/Effects/BuffNextHitLifestealFlatEffect.cs:14-20](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/BuffNextHitLifestealFlatEffect.cs#L14)

**状态依据**：ApplyPlayerLifesteal carries the comment 'Native ELF VA 0x15d00cc' (UnitDamageSystem.cs:338).

**配置参数**：`Player Config.Stats[Type=22] (LifestealFlat)`；`Player Config.Stats[Type=32] (LifestealPercent)`；`Meta Stats Config: mark_lifesteal`；`BuffNextHitLifestealFlatEffect.ValueScaling / ScalingStat`

**使用方**：combat.battle_regen (DamageCalculator.GetBattleRegen models it as (2d+3)*lifestealFlat)；simulation battle regen (DamageCalculator.GetBattleRegen uses LifestealFlat)；combat

**备注**：Called unconditionally from ApplyDamageToEnemy for every player weapon hit: in the dummy branch (line 318) and in the normal branch (line 332). It therefore runs on dodged hits and on hits whose damage is 0. NextHitLifestealFlatBonus is consumed even when flat == 0. Each heal is skipped if the player IsDead. The min(HP, MaxHP) cap runs only if flat > 0 or pct > 0. MaxHP is the raw Health Value from GetStatValue. The mark multiplier applies to both flat and percent; the Hydra 0.1*k bonus applies to flat only. Unlike enemy lifesteal (combat.enemy_lifesteal_on_hit), this path is capped at MaxHP.

**依赖图节点**：输入 `value:player.stat_raw[LifestealFlat]`、`value:player.stat_raw[LifestealPercent]`、`value:player.stat_raw[Health]`、`value:player.mark_lifesteal_multiplier`、`value:enemy.mark_stacks`、`constant:UnitDamageSystem.hydra_mark_flat_lifesteal_per_stack`、`state:boss_tower_context`、`value:player.next_hit_lifesteal_bonus`、`value:unit.is_dead`；输出 `value:player.lifesteal_heal_per_hit`、`value:player.current_hp`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `stats.lifesteal_player`）：Checked against UnitDamageSystem.cs:318,332,357-365: calls are unconditional (dodged hits and dummies included), the bonus is reset before the flat>0 check, the cap is guarded by flat>0||pct>0, and MaxHP comes from GetStatValue(Health), which is the raw Value. The expression and notes now state all of this.

### `combat.enemy_lifesteal_on_hit`

**Enemy lifesteal when damaging the player**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 合并自：`combat.enemy_lifesteal_on_hit`、`stats.lifesteal_enemy`
- 原生地址：`0x15ce7e8 (ApplyLifesteal); caller ApplyDamageToPlayer 0x15cdf10`

```text
in ApplyDamageToPlayer, only when damage (after FixedDamage override and god-mode zeroing) > 0, after player HP -= damage: a = hit Source; flat = a.GetStatValue(LifestealFlat); pct = a.GetStatValue(LifestealPercent); if flat > 0 and !a.IsDead: a.HP += flat; if pct > 0 and !a.IsDead: a.HP += pct * a.GetStatValue(Health); no MaxHP cap
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `LifestealFlat, LifestealPercent` | attacker (enemy) stats via GetStatValue; in practice only quest mobs set LifestealPercent | QuestMobSpawnerService.ApplyChainMobSettings: LifestealPercent = (double)(MobLifestealMaxHealthPercent * 0.01f) (Quests/QuestMobSpawnerService.cs:283); see combat.quest_mob_armor_and_stats |
| `Health` | enemy max HP (raw Health Value) | EnemySpawnerConfig.Stats[Type=1] |

**输出**：enemy heal per landed (damage > 0) hit on the player

**代码**：[Unit/Systems/UnitDamageSystem.cs:134-144](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L134)；[Unit/Systems/UnitDamageSystem.cs:155-162](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L155)；[Quests/QuestMobSpawnerService.cs:283](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestMobSpawnerService.cs#L283)

**状态依据**：ApplyLifesteal carries 'Native ELF VA 0x15ce7e8; this general helper intentionally does not cap health' (UnitDamageSystem.cs:157).

**配置参数**：`QuestChainDef.MobLifestealMaxHealthPercent`

**使用方**：quests

**备注**：Only applied when damage > 0 after the god-mode check, so dodged hits, fully absorbed hits and god mode give no heal. There is no health cap, so enemies can overheal above MaxHP. It runs before the player's lethal-ignore check (line 145).

**依赖图节点**：输入 `value:enemy.stat_raw[LifestealFlat]`、`value:enemy.stat_raw[LifestealPercent]`、`value:enemy.stat_raw[Health]`、`value:player.incoming_damage_per_hit`、`state:cheats`、`value:unit.is_dead`；输出 `value:enemy.lifesteal_heal_per_hit`、`value:enemy.current_hp`

### `combat.thorns_reflection`

**Player thorns reflection on being attacked**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 合并自：`combat.thorns_reflection`、`stats.thorns_reflection`、`powerups.thorns`
- 原生地址：`0x15ce604 (ApplyThornsReflection), 0x15f2fa0 (ThornsEffect.UpdatePassive), 0x15eef6c (BuffNextThornsDamageEffect.Execute), 0x15e8420 (ResetPerFrameState)`

```text
Upstream, each frame: ThornsDamage = 0 (AbilityExecuteEventSystem.ResetPerFrameState); for each active ThornsEffect passive: ThornsDamage += (float)SpecificValue(DamageSourceStat) * (float)SpecificValue(DamageMultiplierStat) * State.GetValue(). BuffNextThornsDamageEffect.Execute: NextThornsDamageBonus += State.GetValue() (persists until consumed). Application in ApplyDamageToPlayer (skipped if the player IsDying), after block handling and before the dodge / god-mode / damage<=0 checks: if ThornsDamage > 0: d = ThornsDamage; if NextThornsDamageBonus > 0: bonus consumed (set to 0), d *= 1f + bonus; attacker.HP -= d (no armor/defence/resist); attacker.LastDamageGet = now; ProcessEnemyDeath(attacker)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ThornsDamage` | PlayerModel.ThornsDamage, rebuilt every frame as SUM over ThornsEffect of specific(sourceStat) * specific(multStat) * abilityValue | Abilities/Effects/ThornsEffect.cs:14-20; reset in Abilities/Systems/AbilityExecuteEventSystem.cs:79 |
| `DamageSourceStat, DamageMultiplierStat` | player stats whose specific values are multiplied | ThornsEffect fields |
| `NextThornsDamageBonus` | one-shot multiplier bonus, accumulated by BuffNextThornsDamageEffect | Abilities/Effects/BuffNextThornsDamageEffect.cs:9-13 |

**输出**：flat, unmitigated damage to the attacking enemy on every enemy hit attempt

**代码**：[Abilities/Effects/ThornsEffect.cs:14-20](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/ThornsEffect.cs#L14)；[Abilities/Effects/BuffNextThornsDamageEffect.cs:9-13](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/BuffNextThornsDamageEffect.cs#L9)；[Abilities/Systems/AbilityExecuteEventSystem.cs:51-53](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L51)；[Abilities/Systems/AbilityExecuteEventSystem.cs:79](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L79)；[Unit/Systems/UnitDamageSystem.cs:113](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L113)；[Unit/Systems/UnitDamageSystem.cs:124](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L124)；[Unit/Systems/UnitDamageSystem.cs:212-228](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L212)

**状态依据**：ApplyThornsReflection carries 'Native ELF VA 0x15ce604'; ThornsEffect.UpdatePassive 'Native 0x15f2fa0'; BuffNextThornsDamageEffect.Execute 'Original ELF VA 0x15eef6c'.

**配置参数**：`ThornsEffect.DamageSourceStat`；`ThornsEffect.DamageMultiplierStat`；`AbilityDef.Value (State.GetValue) of ThornsEffect / BuffNextThornsDamageEffect`

**使用方**：combat

**备注**：Triggers on every enemy hit attempt, including dodged, blocked, zero-damage and god-mode hits, because line 124 runs before the damage<=0 early return. It does not trigger when the player IsDying (line 113). It is unmitigated by enemy armor, defence or resist. It also refreshes the attacker's LastDamageGet, which keeps resetting the enemy idle-regen timer (see combat.enemy_regen), and it can kill the attacker via ProcessEnemyDeath. ThornsEffect casts each double stat to float before the float product (native order).

**依赖图节点**：输入 `value:player.stat_specific`、`value:ability.effective_value`、`value:unit.is_dead`；输出 `value:player.thorns_damage_per_hit`、`value:enemy.current_hp`

### `combat.enemy_reflection`

**Enemy reflection damage to the player on being hit**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 合并自：`combat.enemy_reflection`、`stats.enemy_reflection`
- 原生地址：`0x15cf560 (ApplyEnemyReflection); caller ApplyDamageToEnemy 0x15cd840`

```text
in ApplyDamageToEnemy, non-dummy branch only, after enemy HP -= damage: if !HasDodged and target.ReflectionDamage > 0 and Source is PlayerUnit and !GodMode and !player.IsDead: player.HP -= ReflectionDamage (flat, no defence/armor, no rounding); player.LastDamageGet = now; HitsTakenCount++; HitsTakenLastUpdate = now; if player.HP <= 0.01 and !IsDying: ProcessPlayerDeath(enemy.name) (no lethal-ignore check)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ReflectionDamage` | enemy field, flat damage per player hit | Quest mobs: Damage stat * (double)MobReflectionMaxHealthPercent * (double)0.01f (Quests/QuestMobSpawnerService.cs:270,284); crystal objectives: CrystalObjectiveUnitStats.ReflectionDamage (Crystals/CrystalObjectiveBalance.cs:36) |

**输出**：player HP loss per own non-dodged hit

**代码**：[Unit/Systems/UnitDamageSystem.cs:252-271](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L252)；[Unit/Systems/UnitDamageSystem.cs:314-321](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L314)；[Unit/Systems/UnitDamageSystem.cs:329](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L329)；[Quests/QuestMobSpawnerService.cs:270](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestMobSpawnerService.cs#L270)；[Quests/QuestMobSpawnerService.cs:284](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestMobSpawnerService.cs#L284)；[Crystals/CrystalObjectiveBalance.cs:36](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Crystals/CrystalObjectiveBalance.cs#L36)

**状态依据**：ApplyEnemyReflection carries 'Native ELF VA 0x15cf560' (UnitDamageSystem.cs:254).

**配置参数**：`QuestChainDef.MobReflectionMaxHealthPercent`；`Crystal objective balance (CrystalObjectiveUnitStats.ReflectionDamage)`

**使用方**：quests；crystals

**备注**：It bypasses lethal-ignore and can kill the player directly. It also increments HitsTakenCount and sets HitsTakenLastUpdate, so it feeds BonusStatPerHitsTakenEffect. It applies to non-dodged hits even when the hit damage is 0, but not to dummies, because the dummy branch returns before it. Despite its name, the quest MobReflectionMaxHealthPercent scales the mob's Damage stat, not its max health.

**依赖图节点**：输入 `value:enemy.reflection_damage`、`config:CrystalConfig.ObjectiveReflectionDamageMultiplier`、`config:CrystalConfig.ObjectiveSnapshotBaseDay`、`config:CrystalConfig.ObjectiveSnapshotDayStep`、`value:sim.snapshot_at_time`、`constant:CrystalObjectiveBalance.reflection_kill_ramp_divisor`、`state:save_counters`、`value:unit.hit_roll_outcome`、`state:cheats`、`value:unit.is_dead`；输出 `value:player.reflection_damage_taken`、`value:player.current_hp`、`value:player.hits_taken_count`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `combat.enemy_reflection`）：Confirmed in UnitDamageSystem.cs:255-260 (HitsTaken increments), 314-321 (dummy branch returns before line 329) and 329 (only a !HasDodged guard, no damage>0 guard). The expression and notes now reflect it.

### `combat.first_vulnerability_multiplier`

**First-hit vulnerability multiplier**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 合并自：`combat.first_vulnerability_multiplier`、`stats.first_hit_vulnerability`
- 原生地址：`0x15cd840 (ApplyDamageToEnemy)`

```text
damage = OneDamageMode ? 1.0 : (OneHitMode ? 0.9 * target.Health : Math.Round(DamageCalculator.Calculate(data).Damage)); if target entity has UnitFirstVulnerabilityComponent: damage *= DamageModifier (float); component removed; result.IsEmpowered = true
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `DamageModifier` | one-shot outgoing damage multiplier (float32) | Set only by VampireSpiritSystem to 2f (BossTower/Systems/VampireSpiritSystem.cs:249) |

**输出**：outgoing player damage on the first hit against the marked unit

**代码**：[Unit/Systems/UnitDamageSystem.cs:291-306](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L291)；[Unit/Components/UnitFirstVulnerabilityComponent.cs:5-8](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Components/UnitFirstVulnerabilityComponent.cs#L5)；[BossTower/Systems/VampireSpiritSystem.cs:229](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/Systems/VampireSpiritSystem.cs#L229)；[BossTower/Systems/VampireSpiritSystem.cs:249](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/Systems/VampireSpiritSystem.cs#L249)

**状态依据**：Inline in ApplyDamageToEnemy, which carries 'Native ELF VA 0x15cd840' (UnitDamageSystem.cs:288).

**使用方**：combat.outgoing_damage_to_enemy；bosstower

**备注**：Applied after DamageCalculator rounding, so the result can be fractional before HP subtraction. It is not part of simulation DPS. It is applied after the cheat overrides (OneHitMode 0.9*MaxHP, OneDamageMode 1), so OneDamageMode plus vulnerability gives 2. The component is consumed even on a dodged hit (dodged damage is 0, so the x2 is wasted) because there is no HasDodged guard. It applies before the dummy branch.

**依赖图节点**：输入 `constant:VampireSpiritSystem.first_vulnerability_damage_modifier`、`state:boss_tower_context`；输出 `value:enemy.first_hit_damage_multiplier`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `stats.first_hit_vulnerability`）：UnitDamageSystem.cs:292-306 sets the cheat overrides before the multiplier and has no HasDodged guard. The outgoing Calculate path zeroes damage on dodge (DamageCalculator.cs:195), and VampireSpiritSystem.cs:249 uses 2f. The expression, variables and notes now reflect this.

### `combat.lethal_damage_ignore`

**Lethal damage ignore (cheat death)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 合并自：`combat.lethal_damage_ignore`、`stats.lethal_ignore`、`powerups.lethal_ignore`
- 原生地址：`0x15ce89c (TryIgnoreLethalDamage), 0x15f0ce4 (LethalIgnorePassiveEffect.UpdatePassive), 0x15cdf10 (ApplyDamageToPlayer), 0x15e8420 (ResetPerFrameState)`

```text
Upstream, each frame: LethalIgnoreChance = 0, HasPerTargetLethalIgnore = false (ResetPerFrameState); for each LethalIgnorePassiveEffect: LethalIgnoreCooldown = State.Cooldown; if State.Cooldown > 0: skip; else LethalIgnoreChance += State.GetChance(); if Mode == PerTarget: HasPerTargetLethalIgnore = true. On a player hit with damage > 0, after HP -= damage and after enemy lifesteal: if player.IsDead (HP <= 0.01): TryIgnoreLethalDamage: if LethalIgnoreChance <= 0 and !HasPerTargetLethalIgnore: fail; if HasPerTargetLethalIgnore: { id = Source.GetEntityId().GetHashCode(); if id in LethalIgnoreTargetIds or LethalIgnoreCooldown > 0: fail (NO fallback to the chance roll); else add id } else if Random.value >= LethalIgnoreChance: fail; success: fire OnLethalDamageIgnore, HasPerTargetLethalIgnore = false, LethalIgnoreChance = 0, HP = 1. If it fails and HP <= 0.01: ProcessPlayerDeath
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `LethalIgnoreChance` | survive probability, rebuilt each frame from passive abilities | LethalIgnorePassiveEffect: += State.GetChance() (AbilityDef.Chance, see powerups.ability_effective_value) |
| `HasPerTargetLethalIgnore` | per-attacker mode flag | LethalIgnorePassiveEffect.Mode == PerTarget |
| `LethalIgnoreCooldown` | ability state cooldown mirrored each frame | LethalIgnorePassiveEffect.cs:16 (State.Cooldown) |
| `LethalIgnoreTargetIds` | set of attacker entity-id hashes already ignored | PlayerModel.LethalIgnoreTargetIds (PlayerModel.cs:495,521) |

**输出**：whether the player survives a lethal hit at 1 HP

**代码**：[Unit/Systems/UnitDamageSystem.cs:140-152](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L140)；[Unit/Systems/UnitDamageSystem.cs:176-193](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L176)；[Abilities/Effects/LethalIgnorePassiveEffect.cs:13-20](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/LethalIgnorePassiveEffect.cs#L13)；[Abilities/Systems/AbilityExecuteEventSystem.cs:70](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L70)；[Abilities/Systems/AbilityExecuteEventSystem.cs:77-78](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L77)；[Player/Models/PlayerModel.cs:495-496](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L495)；[Player/Models/PlayerModel.cs:521](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L521)

**状态依据**：TryIgnoreLethalDamage carries 'Native ELF VA 0x15ce89c'; LethalIgnorePassiveEffect.UpdatePassive carries 'Original ELF VA 0x15f0ce4'.

**配置参数**：`AbilityDef.Chance`；`LethalIgnorePassiveEffect.Mode`；`CooldownCondition.Cooldown`

**使用方**：combat survival / effective HP；combat survival / time-to-kill

**备注**：The check runs only after a damaging hit (damage > 0), once IsDead is true (HP <= 0.01; IsDying was already excluded at line 113), and after enemy lifesteal. In per-target mode, a used attacker or an active cooldown fails outright; the chance roll only happens when per-target mode is off. On success both flags are zeroed, but this lasts only until the next frame's passive tick rebuilds them. The real rate limiter is the ability State.Cooldown, which gates the passive's contribution in both modes. No call clears LethalIgnoreTargetIds outside the PlayerModel constructor in this reconstruction. Enemy reflection deaths are not covered (ApplyEnemyReflection kills directly without this check, UnitDamageSystem.cs:261-265). If several passives exist, LethalIgnoreCooldown holds the last one's State.Cooldown. NaN cooldown follows the chance branch (native B.GT).

**依赖图节点**：输入 `value:ability.effective_chance`、`value:ability.condition_pass`、`state:rng`、`value:player.incoming_damage_per_hit`、`value:unit.is_dead`、`constant:UnitDamageSystem.lethal_ignore_survive_hp`；输出 `value:player.lethal_ignore_chance`、`value:player.current_hp`

**核验记录**（已写入上方字段）：

- 采纳 `id (missed list)`（来自 `stats.lethal_ignore`）：The three duplicates are merged into this single canonical entry. stats.lethal_ignore and powerups.lethal_ignore are listed in merged_from.
- 采纳 `expression`（来自 `combat.lethal_damage_ignore`）：Matches UnitDamageSystem.cs:179-190: early fail at 179, per-target fail at 184 with no fallback, chance roll only in the else-branch at 187, and the reset at 189-190. Lines 140-145 confirm it runs after HP -= damage and ApplyLifesteal. The expression now encodes this exactly.

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

**依赖图节点**：输入 `value:enemy.attack_damage_ramped`、`value:player.boss_tower_incoming_multiplier`、`value:player.flat_defense_effective`、`constant:DamageCalculator.flat_defense_exp_coef`、`constant:DamageCalculator.flat_defense_linear_coef`；输出 `value:player.incoming_damage_after_flat_defense`

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

**依赖图节点**：输入 `value:sim.target_incoming_damage_per_hit`、`value:sim.snapshot_at_time`、`value:sim.ability_ehp_multiplier`、`constant:DamageCalculator.flat_defense_exp_coef`、`constant:DamageCalculator.flat_defense_linear_coef`、`constant:DamageCalculator.solve_attack_tolerance`；输出 `value:enemy.solved_attack_damage`

**核验记录**（已写入上方字段）：

- 采纳 `status`（来自 `combat.solve_attack_for_incoming`）：[Services/Damage/DamageCalculator.cs:31-57](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L31) has no native VA comment. No recovery notes outside the .cs files mention it (I grepped the repo). The int.MaxValue counter and LogError are only shape evidence, the same kind the catalog itself rejected for ApplyDamageDefense (lines 24-29, labelled reconstructed) and GetBattleRegen. One rule should apply to all three, so treat this as unconfirmed.

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

**依赖图节点**：输入 `config:EnemySpawnerConfig.Difficulty`、`config:BossTowerConfig.Bosses[].Difficulty`、`config:BossTowerConfig.Bosses[].EliteDifficulty`、`value:sim.snapshot_at_time`、`value:player.stat_specific[HealthRegen]`、`value:player.stat_raw[LifestealFlat]`、`constant:DamageCalculator.battle_regen_regen_offset`、`constant:DamageCalculator.battle_regen_lifesteal_slope`、`constant:DamageCalculator.battle_regen_lifesteal_offset`；输出 `value:player.battle_regen`

### `combat.player_flat_defense`

**Player effective flat defence vs a damage type (live hit)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x140a4cc (DamageCalculator.Calculate)`

```text
D = s * (UniversalDefenceFlat + sum_{i in EquippedArmors, i.Config.DamageType in {Universal, hitType}} i.Armor + TypedDefenceFlat[hitType]) * TypedDefencePercent[hitType] ; s = fromSimulation ? simulationFlatDefenceMultiplier : 1.0 ; EquippedArmors = equipped items with SlotType Helmet or Shield ; typed stats via WeaponTypeToDefence(hitType): Stab 14/15, Cut 16/17, Magic 18/19, Buckshot 20/21, Universal 26/27 (so for hitType = Universal: D = s * (2*UniversalDefenceFlat + sum Universal armor) * UniversalDefencePercent, because stat 26 is added twice)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `UniversalDefenceFlat` | player stat 26 raw value (base 0) | Player Config.Stats + modifiers |
| `i.Armor` | helmet/shield armor value | items subsystem ItemModel.CalcArmor (Player/Inventory/ItemModel.cs:486-498) |
| `TypedDefenceFlat` | Stab/Cut/Magic/Buckshot DefenceFlat raw (stats 14/16/18/20); stat 26 for Universal hits | Player Config.Stats + devour/shop modifiers |
| `TypedDefencePercent` | Stab/Cut/Magic/Buckshot DefencePercent raw (stats 15/17/19/21, base 1); stat 27 for Universal hits. It acts as a MULTIPLIER on all flat defence | Player Config.Stats + modifiers |
| `hitType` | WeaponHitEvent.DamageType = attacking enemy's DamageType (spawner WeaponType) | EnemySpawnerConfig.WeaponType |
| `s` | simulation flat defence multiplier | Calculate() argument (default 1.0; simulation passes 1.0) |

**输出**：D for combat.flat_defense_curve (ApplyDamageDefense(A, D))

**代码**：[Services/Damage/DamageCalculator.cs:71-72](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L71)；[Services/Damage/DamageCalculator.cs:75](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L75)；[Services/Damage/DamageCalculator.cs:83-87](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L83)；[Services/Damage/DamageCalculator.cs:269-280](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L269)；[Player/Models/PlayerModel.cs:163-172](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L163)；[Player/Inventory/ItemModel.cs:486-498](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L486)

**状态依据**：Inside Calculate, annotated 'Native ELF VA 0x140a4cc (bounded pcode)'. SimulationSystem.CalculateDefenseData (VA 0x13e5104) does NOT corroborate it, because it uses a different formula: D_sim = TypedPercent * (TypedFlat + sum of top-N armor items whose DamageType == t exactly) * interpolationCoefficient(StatModifier, Rare), with N = UnlockedArmorSlots. It leaves out UniversalDefenceFlat and Universal (mythical) armor and adds a balance-interpolation factor. The fast GetSpawnerDps path also looks D up by location.PreferredWeaponType, not spawner.WeaponType.

**配置参数**：`Player Config.Stats[Type=14..21,26,27]`；`ItemConfig.DamageType (armor items)`；`ItemConfig.SlotType (Helmet/Shield)`；`CombatBalanceConfig armor base/growth`

**使用方**：combat.incoming_damage_to_player

**备注**：Player Config has no stats 18/19 (Magic defence), so unless a modifier creates them, TypedDefencePercent = 0 for Magic and D = 0: Magic attacks ignore all flat defence and armor. For a Universal-typed hit, the percent stat is UniversalDefencePercent (27, base 0), so D = 0 unless 27 is raised. That same stat also cuts damage by (1 - stat27). The flat stat is UniversalDefenceFlat (26), which line 87 adds twice (once as universalFlat, once as typedFlat), while Universal armor is counted once. Universal (mythical) armor counts toward every type. The percent stat multiplies all flat sources, universal flat included. The simulation and bake defence (SimulationSystem.CalculateDefenseData, Simulation/Systems/SimulationSystem.cs:935-957; fast path :964-971; BossSpawnService.BakeStatsForFloor reads snapshot.DefenseData by spawner WeaponType) is a separate formula, catalogued under the simulation subsystem, and differs as described in status_evidence. The only simulation route that would use this live formula is the GetSpawnerDps slow path (Calculate(hit, true, 1.0), s = 1, SimulationSystem.cs:982-985), which is unreachable in the reconstruction (only isFast=true is called, :660). Expect a bake-vs-live delta for players holding Universal flat defence or mythical armor, and for spawners whose WeaponType differs from the location's PreferredWeaponType.

**依赖图节点**：输入 `value:player.stat_raw[UniversalDefenceFlat]`、`value:player.stat_raw[DefenceFlatByType]`、`value:player.stat_raw[DefencePercentByType]`、`value:player.stat_raw[UniversalDefencePercent]`、`value:weapon.damage_type_defence_stats`、`value:armor.item_armor`、`value:player.equipped_items`、`config:ItemConfig.DamageType`、`config:ItemConfig.SlotType`、`value:enemy.damage_type`、`constant:DamageCalculator.simulation_flat_defence_multiplier`；输出 `value:player.flat_defense_effective`

**核验记录**（已写入上方字段）：

- 采纳 `status_evidence`（来自 `combat.player_flat_defense`）：Verified at SimulationSystem.cs:935-957 (exact DamageType match, top UnlockedArmorSlots armors, no UniversalDefenceFlat, x Rare StatModifier interpolation coefficient) and :964-971 (fast path keyed on location.PreferredWeaponType). status_evidence says the simulation does not corroborate the runtime formula. The simulation.CalculateDefenseData used_by link is removed because CalculateDefenseData does not call this formula, and the differences are recorded in notes. recovered_native stays because the runtime formula is in the annotated Calculate.
- 合并复核修正：(b) The expression and notes miss a case. For a Universal-typed hit, WeaponTypeToDefence(Universal, false) returns UniversalDefenceFlat (stat 26), so the line-87 sum universalFlat + armor + typedFlat counts stat 26 TWICE: D = s*(2*UniversalDefenceFlat + universal armor)*stat27. The notes describe the Universal-hit case but give only the stat-27 gate. The entry also gives no consumer caveat for the live-formula simulation use: the GetSpawnerDps slow path (Calculate(hit, true, 1.0), s=1) uses this formula but is unreachable in the reconstruction (only isFast=true is called, SimulationSystem.cs:660). The forecast defence therefore always comes from CalculateDefenseData.

### `combat.event_enemy_damage_ramp`

**Event enemy damage ramp-up (timer + multiplier)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 合并自：`combat.event_enemy_damage_ramp`、`spawners.event_damage_ramp_timer`
- 原生地址：`0x140a4cc (DamageCalculator.Calculate)`

```text
Timer (EnemyUnit.Tick, every frame): t = (Config.IsEvent && CurrentHealth < GetStatValue(Health) - 0.01) ? t + dt : 0 (float; t = 0 on Initialize) ; Multiplier (DamageCalculator.Calculate, enemy->player branch, applied to the raw attack BEFORE the tower reduction and the defence curve): if attacker is EnemyUnit && attacker.Config.IsEvent: A = A * ((double)max(0f, t - 5f) * 0.04 + 1.0)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `A` | event enemy Damage stat (GetStatValue(Damage)) | EnemySpawnerConfig.Stats[Type=3] set by the event forecast (EnemySpawner.ApplyEventPlanToConfig) |
| `t` | EventDamageRampTimer: seconds the event enemy has continuously been below full HP | EnemyUnit.Tick runtime state |
| `dt` | frame delta time | Tick(deltaTime) |

**输出**：ramped attack value A fed to the flat-defence curve (combat.flat_defense_curve) in the incoming hit

**代码**：[Services/Damage/DamageCalculator.cs:77-79](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L77)；[Enemy/EnemyUnit.cs:129](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L129)；[Enemy/EnemyUnit.cs:144](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L144)；[Enemy/EnemyUnit.cs:186-188](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L186)

**状态依据**：The multiplier is inside Calculate, annotated 'Native ELF VA 0x140a4cc (bounded pcode)'. The timer update in EnemyUnit.Tick (EnemyUnit.cs:188) has no native annotation, so the timer half is reconstructed.

**配置参数**：`EnemySpawnerConfig.IsEvent`；`constant grace period 5 s`；`constant ramp slope 0.04 per s`

**使用方**：combat.incoming_damage_to_player

**备注**：Event enemies gain +4% raw attack per second after the first 5 s spent below full HP. This is an anti-stall lever. Because the ramp scales attack before the non-linear defence curve exp(-0.75 D/A)*(1-0.25 D/A), the damage actually taken grows faster than 4%/s while D/A is significant. The timer resets to 0 on any frame the enemy is at full HP (within 0.01), for example after a leash or revive full-heal (combat.enemy_leash_and_aggro) or full regen. The ramp checks only Config.IsEvent, not whether the event is current (unlike enemyEventMult). simulation.forecast_damage_ramp_average is a separate averaged quantity and is not merged here. The spawners agent catalogued the timer half as spawners.event_damage_ramp_timer.

**依赖图节点**：输入 `value:enemy.stat_raw[Damage]`、`config:EnemySpawnerConfig.IsEvent`、`state:frame_delta_time`、`value:enemy.current_hp`、`value:enemy.stat_raw[Health]`、`constant:DamageCalculator.event_damage_ramp_delay_seconds`、`constant:DamageCalculator.event_damage_ramp_per_second`；输出 `value:enemy.attack_damage_ramped`

### `combat.incoming_damage_to_player`

**Enemy -> player hit resolution**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x140a4cc (Calculate); 0x15cdf10 (ApplyDamageToPlayer); 0x140c070 (GetEnemyEventDamageMultiplier); 0x140c150 (GetIslandMasteryIncomingMultiplier); 0x15ce604 (ApplyThornsReflection); 0x15ce7e8 (ApplyLifesteal)`

```text
Calculate (target not EnemyUnit): A = Source.Damage ; if attacker.IsEvent: A *= max(0, t-5)*0.04 + 1 ; if attacker.IsBossFightPart: A /= (tower_damage_reduction + 1) ; x = ApplyDamageDefense(A, D) ; x = max((1 - UniversalDefencePercent_raw) * x, 0) ; if roll(Evasion_spec) && !Undodgeable: x = 0, HasDodged ; else { if !Undodgeable && roll(BlockChance_spec): x *= 1 - BlockAmount_spec, HasBlocked ; if x > 0: { if rageLevel > 0 && rage_damage_reduce > 0: x /= rage_damage_reduce * rageLevel + 1 ; x *= enemyEventMult(attacker) ; if attacker != null: x *= islandIncoming ; x *= 1/(1 + 0.01*GlobalDefence_spec) } } ; if HasSpecialMultiplier: x *= SpecialMultiplier ; Damage = Math.Round(x) (banker's) ; Resist = Damage <= 0 ? 0 : 1 - A/Damage. ApplyDamageToPlayer: skip if target IsDying ; if HasFixedDamage: Damage = FixedDamage (overrides everything above, including a dodge) ; applied = GodMode ? 0 : Damage ; on block: BlockSuperCritChanceBonus += block_to_supercrit (if > 0), OnBlock trigger ; if ThornsDamage > 0: thorns reflect ; on dodge: evasion triggers/marks ; if applied <= 0: stop ; else HitsTakenCount++, CurrentHealth -= applied, enemy lifesteal (LifestealFlat + LifestealPercent*enemyMaxHP, uncapped), lethal-ignore check (HP = 1), death at HP <= 0.01
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `A / Damage_enemy` | enemy Damage stat raw | EnemySpawnerConfig.Stats[Type=3] x AB multiplier (spawners.enemy_spawn_stats) |
| `t` | event ramp timer | combat.event_enemy_damage_ramp |
| `D` | flat defence | combat.player_flat_defense |
| `UniversalDefencePercent_raw` | player stat 27 raw (base 0) | Player Config.Stats |
| `Evasion_spec, BlockChance_spec, BlockAmount_spec` | specific values (capped 0.8) | stats.specific_value_pipeline |
| `rageLevel` | active rage level | RageModel.ActiveRageLevel (rage subsystem) |
| `rage_damage_reduce` | meta stat | Meta Stats Config key rage_damage_reduce (amulet upgrades) |
| `tower_damage_reduction` | meta stat for boss-tower attackers | Meta Stats Config key tower_damage_reduction |
| `enemyEventMult` | event enemy damage multiplier (current event only) | combat.event_crystal_damage_multipliers (GetEnemyEventDamageMultiplier -> EventBalance.GetEnemyDamageMultiplier) |
| `islandIncoming` | island mastery reduction | combat.island_mastery_multipliers |
| `GlobalDefence_spec` | global defence specific value | combat.global_defence_multiplier |
| `SpecialMultiplier` | per-hit special multiplier (e.g. LavaCrab explosion = 10, also Undodgeable) | WeaponHitEvent producer |
| `FixedDamage` | per-hit fixed damage override | WeaponHitEvent producer |

**输出**：DamageResult.Damage subtracted from player CurrentHealth. DamageResult.Resist = 1 - A/Damage is display only.

**代码**：[Services/Damage/DamageCalculator.cs:64-121](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L64)；[Services/Damage/DamageCalculator.cs:227-236](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L227)；[Services/Damage/DamageCalculator.cs:243-246](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L243)；[Services/Damage/DamageCalculator.cs:263-267](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L263)；[Unit/Systems/UnitDamageSystem.cs:110-153](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L110)；[Unit/Systems/UnitDamageSystem.cs:155-162](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L155)；[Unit/Systems/UnitDamageSystem.cs:212-228](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L212)；[Enemy/EnemyAttackSystem.cs:342-348](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L342)；[BossTower/Systems/LavaCrabExplosionSystem.cs:116](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/Systems/LavaCrabExplosionSystem.cs#L116)；[Simulation/Systems/SimulationSystem.cs:979-990](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L979)；[Simulation/Systems/SimulationSystem.cs:660](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L660)

**状态依据**：Calculate ('Native ELF VA 0x140a4cc (bounded pcode)') and ApplyDamageToPlayer ('Native ELF VA 0x15cdf10') both carry native annotations. The island incoming helper's tail math is inferred (see combat.island_mastery_multipliers).

**配置参数**：`EnemySpawnerConfig.Stats[Type=3]`；`EnemySpawnerConfig.WeaponType`；`EnemySpawnerConfig.IsBossFightPart`；`EnemySpawnerConfig.IsEvent`；`Player Config.Stats[Type=14..21,23,26,27,31,33,34]`；`Meta Stats Config: tower_damage_reduction, rage_damage_reduce, island_medal_bonus, block_to_supercrit, evasion_mark`

**使用方**：simulation.GetSpawnerDps slow path ([Simulation/Systems/SimulationSystem.cs:979-990](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L979); unreachable in the reconstruction, see notes)；boss_tower

**备注**：Order matters: universal% applies before dodge and block. Rage, event, island and GlobalDefence apply only when x > 0 after block. The special multiplier applies even to dodged (0) hits. Math.Round uses banker's rounding. Undodgeable=true turns off BOTH the dodge roll and the block roll (DamageCalculator.cs:90,97). The simulation slow path relies on this: it sets Undodgeable and applies (1-ev)*(1-bc*ba) analytically (SimulationSystem.cs:979-987). In this reconstruction, though, GetSpawnerDps is only called with isFast=true (SimulationSystem.cs:660), so the slow path and its Calculate(hit, true) call never run. The forecast enemy DPS uses the fast path (ApplyDamageDefense on the cached defence with analytic dodge/block/GlobalDefence), not this function. HasFixedDamage overrides the computed damage, even on a dodged hit, so the dodge still triggers its effects but FixedDamage is taken. Quirk: GetEnemyEventDamageMultiplier(target, attacker) is called even when attacker is null (line 111, inside x > 0), which would throw a null reference if a non-EnemyUnit source ever hit the player. Thorns fire before the god-mode zeroing takes effect and before the damage <= 0 return (UnitDamageSystem.cs:124 vs 135), so they also fire on dodged hits and in god mode. Block bonuses (block_to_supercrit) also accrue in god mode. Each damaging hit increments HitsTakenCount. Enemy lifesteal (flat + percent of the enemy's own max HP, not capped) fires only on damaging hits.

**依赖图节点**：输入 `value:enemy.stat_raw[Damage]`、`value:enemy.attack_damage_ramped`、`value:player.boss_tower_incoming_multiplier`、`value:player.flat_defense_effective`、`value:player.incoming_damage_after_flat_defense`、`value:player.stat_raw[UniversalDefencePercent]`、`value:unit.hit_roll_outcome`、`value:player.stat_specific[Evasion]`、`value:player.stat_specific[BlockChance]`、`value:player.stat_specific[BlockAmount]`、`value:rage.incoming_damage_multiplier`、`value:player.event_crystal_incoming_multiplier`、`value:island.mastery_incoming_multiplier`、`value:player.global_defence_multiplier`、`constant:LavaCrabExplosionSystem.special_multiplier`、`state:boss_tower_context`、`state:cheats`；输出 `value:player.incoming_damage_per_hit`、`value:player.current_hp`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `combat.incoming_damage_to_player`）：Verified: line 97 gates the block roll on !Undodgeable. SimulationSystem.cs:984-987 sets Undodgeable and applies (1-dodge) and (1-block*blockAmount) analytically. Line 111 calls GetEnemyEventDamageMultiplier with a possibly-null attacker, which dereferences enemy.Config. UnitDamageSystem.cs:124 (thorns) runs before the damage <= 0 return at 135, and god mode only zeroes the applied damage at 116. All three points are in notes, and the expression reflects the null-guarded island call and the thorns ordering.
- 合并复核修正：(b/a) The notes cite the wrong line for the damage <= 0 early return. It says 'UnitDamageSystem.cs:124 vs 133', but line 133 is the evasion_mark call; the `if (damage <= 0.0) return` is at line 135 (verified). The resolution reason repeats the same wrong line. The entry also leaves out a consumer caveat: used_by names the simulation GetSpawnerDps slow path (SimulationSystem.cs:979-990) as a consumer, but the reconstruction's only caller (SimulationSystem.cs:660) passes isFast=true. The slow path, and with it Calculate(hit, true) on the incoming branch, is unreachable here (the native comment at :962 does not list other callers).

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
| `p` | probability (specific value) | stats.specific_value_pipeline |

**输出**：boolean outcomes, block reduction factor

**代码**：[Utilities/DoubleExtensions.cs:67-71](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Utilities/DoubleExtensions.cs#L67)；[Services/Damage/DamageCalculator.cs:89-102](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L89)；[Services/Damage/DamageCalculator.cs:192-195](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L192)

**状态依据**：Roll is annotated 'Native ELF VA 0x15c4bf4'. The usage sits inside Calculate (0x140a4cc).

**配置参数**：`Player Config.Stats[Type=23,33,34]`

**使用方**：combat.incoming_damage_to_player；combat.outgoing_damage_to_enemy；combat.crit_and_supercrit

**备注**：Expected incoming multiplier = (1 - ev) * (1 - bc*ba). The simulation uses exactly this (SimulationSystem.cs:986-988). Enemies have stats 0-5 only, so player hits are never dodged except by quest mobs with an Evasion override.

**依赖图节点**：输入 `state:rng`、`value:player.stat_specific[Evasion]`、`value:player.stat_specific[BlockChance]`、`value:player.stat_specific[BlockAmount]`、`value:enemy.stat_specific[Evasion]`、`value:player.crit_chance_total`、`value:player.supercrit_chance_total`、`value:weapon.ability_supercrit_bonus`、`state:boss_tower_context`；输出 `value:unit.hit_roll_outcome`

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

**依赖图节点**：输入 `value:player.stat_raw[DamagePercentByType]`、`value:player.stat_raw[Damage]`、`value:weapon.item_damage`、`value:player.stat_raw[DamageFlatByType]`、`value:player.ability_damage_flat`、`value:weapon.ability_damage_flat`、`config:ItemConfig.DamageStatTypeFlat`、`config:ItemConfig.DamageStatTypePercent`、`config:ItemConfig.DamageType`、`config:ArmorConfig.Armors[].Resist`、`config:ArmorConfig.Armors[].Type`、`config:EnemySpawnerConfig.ArmorConfig`、`config:BossTowerConfig.Bosses[].ArmorConfig`、`value:enemy.resist_by_damage_type`、`value:player.boss_tower_outgoing_multiplier`、`value:unit.hit_roll_outcome`、`value:player.crit_multiplier_total`、`value:player.supercrit_multiplier_total`、`value:weapon.stat_stack_multiplier[Damage]`、`value:weapon.empowered_hit_multiplier`、`value:weapon.ability_damage_multiplier`、`value:player.ability_damage_multiplier`、`value:enemy.stat_raw`、`value:player.next_hit_damage_bonus`、`value:player.event_crystal_outgoing_multiplier`、`value:player.global_attack_multiplier`、`value:island.mastery_outgoing_multiplier`、`value:enemy.stunned_damage_multiplier`、`value:enemy.shock_damage_multiplier`、`value:player.event_golem_damage_multiplier`、`value:enemy.first_hit_damage_multiplier`、`state:cheats`；输出 `value:player.outgoing_damage_per_hit`、`value:enemy.current_hp`

### `combat.outgoing_damage_simulation_mode`

**Expected-value hit used by simulation (fromSimulation=true)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x140a4cc (Calculate); 0x140bef8 (GetEventGolemDamageMultiplier)`

```text
Same pipeline as combat.outgoing_damage_to_enemy (Calculate player->enemy branch) with fromSimulation=true: critMult_exp = (CritDmg_spec - 1) * p_crit + 1 is ALWAYS applied, times AbilityCritMultiplier and prod GetWeaponStatMultiplier(CritDamage), regardless of the rolled IsCrit ; IsCrit and IsSuperCrit are still RNG-rolled, and superMult is applied when IsSuperCrit ; EmpoweredHit / NextHitEmpower are applied AND consumed, HitsCounter is incremented (on owner.EquippedItems), NextHitDamageBonus is applied but NOT consumed ; no dodge roll ; Damage = Math.Round(base * eventGolemMult) (banker's) ; NextHitSuperCrit*, NextHitDamageBonus and BlockSuperCritChanceBonus are not reset
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `p_crit` | crit chance (uncapped, includes enemy SpecialAdditionalCritChancePercent and AbilityCritChanceBonus) | combat.crit_and_supercrit |
| `CritDmg_spec` | crit damage multiplier 1.5*v^0.08 | stats.specific_value_pipeline |
| `eventGolemMult` | 1 + event_golem_damage for event golem units | Meta Stats Config event_golem_damage |

**输出**：per-hit damage for DPS forecasts. It is expectation-based for crit, but super-crit is still random.

**代码**：[Services/Damage/DamageCalculator.cs:136-148](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L136)；[Services/Damage/DamageCalculator.cs:149-174](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L149)；[Services/Damage/DamageCalculator.cs:193-203](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L193)；[Services/Damage/DamageCalculator.cs:219-225](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L219)；[Simulation/Systems/SimulationSystem.cs:826-851](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L826)；[Simulation/Systems/SimulationSystem.cs:658](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L658)；[Simulation/Systems/SimulationSystem.cs:795-797](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L795)；[Simulation/Systems/SimulationSystem.cs:908-958](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L908)

**状态依据**：The same VA-annotated method (Native ELF VA 0x140a4cc), fromSimulation branch.

**配置参数**：`Player Config.Stats[Type=24,25,28,29]`；`ItemConfig.SimulationDpsWeaponMultiplier`；`ItemConfig.AttackCooldown`；`Meta Stats Config: event_golem_damage`

**使用方**：simulation.GetDpsAgainstSpawner slow path ([Simulation/Systems/SimulationSystem.cs:841-850](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L841); unreachable in the reconstruction because the only caller at :658 passes isFast=true)

**备注**：Simulation DPS (slow path) = GetPlayerAbilityDpsMultiplier() * sum over SimulationSystem.equippedItems (non-armor) of Damage_sim * ItemConfig.SimulationDpsWeaponMultiplier / ItemConfig.AttackCooldown (SimulationSystem.cs:841-850). It ignores attack speed and cast timing. The fast path (SimulationSystem.cs:828-834) does not call Calculate. Reachability: the only call to GetDpsAgainstSpawner (SimulationSystem.cs:658) passes isFast=true, so this fromSimulation=true outgoing branch is never reached in the reconstruction. The native comment at :828 ('normal shop uses the fast cached branch') suggests that other native callers may use the slow path. equippedItems is the simulation's scratch list, not PlayerModel.EquippedItems. CalculateDpsData fills it with the top UnlockedWeaponSlots items from playerModel.Items, then CalculateDefenseData clears it and refills it with armor (called in that order at :324-326). ProcessInventory, which runs on a location change, is an empty stub (:795-797). As reconstructed, the slow path would usually iterate no weapons. Potential side effect: if the slow path ran, a simulation call would consume EmpoweredHit and NextHitEmpower on the passed ItemModel (a real inventory item, possibly unequipped) and increment HitsCounter on the player's real equipped items, so forecasting could alter live combat state. NextHitDamageBonus is applied but not reset. With p_crit > 1 the expected crit multiplier exceeds the real one. GuaranteedCrit does not raise critMult_exp. The super-crit RNG in simulation may be a reconstruction artifact or an original quirk.

**依赖图节点**：输入 `value:player.stat_raw[DamagePercentByType]`、`value:player.stat_raw[Damage]`、`value:weapon.item_damage`、`value:player.stat_raw[DamageFlatByType]`、`value:player.ability_damage_flat`、`value:weapon.ability_damage_flat`、`config:ItemConfig.DamageStatTypeFlat`、`config:ItemConfig.DamageStatTypePercent`、`config:ItemConfig.DamageType`、`config:ArmorConfig.Armors[].Resist`、`config:ArmorConfig.Armors[].Type`、`config:EnemySpawnerConfig.ArmorConfig`、`value:enemy.resist_by_damage_type`、`value:player.boss_tower_outgoing_multiplier`、`value:player.stat_specific[CritDamagePercent]`、`value:player.crit_chance_total`、`value:weapon.ability_crit_multiplier`、`value:weapon.stat_stack_multiplier[CritDamage]`、`value:unit.hit_roll_outcome`、`value:player.supercrit_multiplier_total`、`value:weapon.stat_stack_multiplier[Damage]`、`value:weapon.empowered_hit_multiplier`、`value:weapon.ability_damage_multiplier`、`value:player.ability_damage_multiplier`、`value:enemy.stat_raw`、`value:player.next_hit_damage_bonus`、`value:player.event_crystal_outgoing_multiplier`、`value:player.global_attack_multiplier`、`value:island.mastery_outgoing_multiplier`、`value:enemy.stunned_damage_multiplier`、`value:enemy.shock_damage_multiplier`、`value:player.event_golem_damage_multiplier`；输出 `value:player.expected_damage_per_hit`

**核验记录**（已写入上方字段）：

- 采纳 `expression`（来自 `combat.outgoing_damage_simulation_mode`）：Verified: line 203 rounds damage * GetEventGolemDamageMultiplier with Math.Round (banker's). Lines 170-174 apply NextHitDamageBonus, and the resets at 198-201 sit only in the !fromSimulation branch. EmpoweredHit (152-158) and NextHitEmpower (160-166) are consumed unconditionally, and HitsCounter++ (167-168) also runs in simulation. The expression states all of this. used_by is also corrected: Calculate(…, true) is called only at SimulationSystem.cs:847 (GetDpsAgainstSpawner) for outgoing hits, not from UpdateDpsDataCache.
- 合并复核修正：(a/b) Two consumer caveats are wrong or missing. (1) used_by and notes present the GetDpsAgainstSpawner slow path as the live consumer and warn that forecasting alters live combat state. But the only caller in the codebase, SimulationSystem.cs:658, passes isFast=true, so Calculate(hit, true) on the outgoing branch is never reached in the reconstruction. The side effect is only potential. (2) 'sum over equipped non-armor items' is misleading. The loop iterates SimulationSystem's scratch list equippedItems (SimulationSystem.cs:101). CalculateDpsData fills it with the top UnlockedWeaponSlots candidates from playerModel.Items, and CalculateDefenseData then clears it and refills it with armor (SimulationSystem.cs:324-326, 914-922, 941-950). ProcessInventory, which should rebuild it on a location change, is an empty stub (:795-797). As reconstructed, the non-armor filter would therefore usually see no items, and the consumed EmpoweredHit/NextHitEmpower would belong to inventory items, which are not necessarily equipped.

### `combat.crit_and_supercrit`

**Crit / super-crit chance and multipliers**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x140a4cc (Calculate); 0x15f2bd4, 0x15f2ce4 (TemporaryWeaponBuffEffect)`

```text
p_crit = enemy.SpecialAdditionalCritChancePercent + CritChance_spec + weapon.AbilityCritChanceBonus ; isCrit = any(weapon.AbilityStates.GuaranteedCrit) or roll(p_crit) ; p_super = SuperCritChance_spec + weapon.AbilitySuperCritChanceBonus + owner.NextHitSuperCritChanceBonus + owner.BlockSuperCritChanceBonus ; isSuper = isCrit and roll(p_super) ; critMult = CritDamagePercent_spec (= postMult*(1.5*v^0.08 + postFlat)) ; superMult = SuperCritDamagePercent_spec (= postMult*(2.0 + postFlat)) + weapon.AbilitySuperCritDamageBonus + owner.NextHitSuperCritDamageBonus ; if isCrit (or fromSimulation, using critMult_exp): damage *= critMult * weapon.AbilityCritMultiplier * prod_states GetWeaponStatMultiplier(CritDamage) ; then damage *= isSuper ? superMult : 1 ; after a real (non-simulation) hit: if isSuper: BlockSuperCritChanceBonus = 0 ; NextHitSuperCritChanceBonus = NextHitSuperCritDamageBonus = NextHitDamageBonus = 0
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `CritChance_spec` | 0.08*v^0.15 (v<=700) else 0.07*v^0.1+0.082; player base v=1 -> 8% | Player Config.Stats[Type=24] + modifiers |
| `SpecialAdditionalCritChancePercent` | per-enemy bonus (Lich marks*0.01) | BossTower/Systems/LichSkeletonDeathSystem.cs:47 |
| `AbilityCritChanceBonus` | per-weapon crit chance bonus, including the TemporaryWeaponBuffEffect CritChance buff while it lasts | Abilities/Effects/TemporaryWeaponBuffEffect.cs:28,53-57; reset each frame at Abilities/Systems/AbilityExecuteEventSystem.cs:61 |
| `SuperCritChance_spec` | raw passthrough (base 0) | Player Config.Stats[Type=28] |
| `BlockSuperCritChanceBonus` | accumulated block_to_supercrit | combat.block_to_supercrit_bonus |
| `CritDamagePercent_spec` | 1.5*v^0.08; base v=1 -> 1.5x | Player Config.Stats[Type=25] |
| `SuperCritDamagePercent_spec` | constant 2.0 before post-formula mods | Player Config.Stats[Type=29] |

**输出**：crit flags and multipliers

**代码**：[Services/Damage/DamageCalculator.cs:132-148](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L132)；[Services/Damage/DamageCalculator.cs:197-201](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L197)；[Unit/Stat.cs:61-77](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L61)；[Unit/Stat.cs:80-88](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L80)；[Abilities/Effects/TemporaryWeaponBuffEffect.cs:28](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/TemporaryWeaponBuffEffect.cs#L28)；[Abilities/Effects/TemporaryWeaponBuffEffect.cs:53-57](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/TemporaryWeaponBuffEffect.cs#L53)；[Abilities/Systems/AbilityExecuteEventSystem.cs:61](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L61)

**状态依据**：Inside the VA-annotated Calculate (Native ELF VA 0x140a4cc). The specific-value curves it depends on (Stat.CalculateSpecificValue) carry no native annotation and are reconstructed (see combat.stat_specific_value_curves).

**配置参数**：`Player Config.Stats[Type=24,25,28,29]`；`Meta Stats Config: block_to_supercrit`；`AbilityDef TemporaryWeaponBuffEffect (BuffType=CritChance/CritDamage, value scaling, Duration)`

**使用方**：combat.outgoing_damage_to_enemy；combat.outgoing_damage_simulation_mode

**备注**：Crit chance is not capped and can exceed 1. Super-crit damage ignores raw SuperCritDamagePercent modifiers (the curve returns 2.0); only PostFormula* modifiers and ability bonuses change it. After a real hit, the NextHit* bonuses reset, and BlockSuperCritChanceBonus resets after a super-crit. WeaponBuffType.CritChance is handled by TemporaryWeaponBuffEffect. It writes AbilityState.TemporaryCritChanceBonus and, while the duration lasts, adds it into ItemModel.AbilityCritChanceBonus each frame, which Calculate reads at line 133. Only the WeaponStatStack route for CritChance (GetWeaponStatMultiplier) goes unused in Calculate. Similarly, a CritDamage temporary buff multiplies AbilityCritMultiplier by (1 + bonus).

**依赖图节点**：输入 `value:player.stat_specific[CritChance]`、`state:boss_tower_context`、`value:weapon.ability_crit_chance_bonus`、`value:weapon.ability_supercrit_bonus`、`value:player.stat_specific[SuperCritChance]`、`value:player.next_hit_supercrit_bonus`、`value:player.supercrit_bonus_from_block`、`value:player.stat_specific[CritDamagePercent]`、`value:player.stat_specific[SuperCritDamagePercent]`、`value:weapon.ability_crit_multiplier`、`value:weapon.stat_stack_multiplier[CritDamage]`；输出 `value:player.crit_chance_total`、`value:player.crit_multiplier_total`、`value:player.supercrit_chance_total`、`value:player.supercrit_multiplier_total`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `combat.crit_and_supercrit`）：Verified: TemporaryWeaponBuffEffect.cs:28 stores TemporaryCritChanceBonus, :53-57 adds it into ItemModel.AbilityCritChanceBonus while the duration lasts, AbilityExecuteEventSystem.cs:61 resets AbilityCritChanceBonus, and DamageCalculator.cs:133 reads it. The note 'Calculate never reads it' is replaced, and the AbilityCritChanceBonus variable, code_refs and config_params were added.

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

**依赖图节点**：输入 `value:player.stat_specific[GlobalAttack]`、`constant:DamageCalculator.global_attack_percent_scale`；输出 `value:player.global_attack_multiplier`

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

**依赖图节点**：输入 `value:player.stat_specific[GlobalDefence]`、`constant:DamageCalculator.global_defence_percent_scale`；输出 `value:player.global_defence_multiplier`

### `combat.island_mastery_multipliers`

**Island medal (mastery) damage multipliers**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x140c264 (IsIslandMasteryEligible), 0x140be68 (outgoing), 0x140c150 (incoming)`

```text
eligible = !(IsEvent || IsVip || IsSpecial || IsDummy || IsBossFightPart || enemy is CrystalUnit || has QuestMobTag) ; b(loc) = MetaStat island_medal_bonus.GetValue(enemy.Config.LocationNumber) ; outgoing = eligible ? b(loc) + 1 : 1 [native] ; incoming = eligible ? max(0, 1 - b(loc)) : 1 [tail math inferred, not ported] ; incoming is applied only when the attacker is an EnemyUnit and the hit is > 0 after dodge/block
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `b(loc)` | per-island medal bonus (param = location number) | Meta Stats Config key island_medal_bonus; IslandMasteryModel (location subsystem) |
| `loc` | enemy location | EnemySpawnerConfig.LocationNumber |

**输出**：two multipliers: outgoing (player->enemy damage) and incoming (enemy->player damage)

**代码**：[Services/Damage/DamageCalculator.cs:248-267](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L248)；[Services/Damage/DamageCalculator.cs:112](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L112)；[Services/Damage/DamageCalculator.cs:175](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L175)

**状态依据**：Split. The eligibility gate (0x140c264, 'bounded ARM64 includes the tail') and the outgoing 1 + b(loc) (0x140be68) are recovered_native. The incoming helper is VA-annotated (0x140c150), but its comment says 'original tail math is absent in decompiler return types', so max(0, 1 - b(loc)) is reconstructed (inferred), not ported. The single status field keeps recovered_native for the entry, and the incoming term is marked [inferred] in the expression.

**配置参数**：`Meta Stats Config: island_medal_bonus`；`EnemySpawnerConfig.LocationNumber, IsEvent, IsVip, IsSpecial, IsDummy, IsBossFightPart`

**使用方**：combat.outgoing_damage_to_enemy；combat.incoming_damage_to_player

**备注**：The incoming formula is uncertain. It could equally be 1/(1+b) or another form; treat max(0, 1 - b) as a placeholder until the native tail is recovered. Quest mobs (QuestMobTag), crystal units, event, VIP, special, dummy and boss-fight units are excluded.

**依赖图节点**：输入 `value:player.meta_stat[island_medal_bonus]`、`config:EnemySpawnerConfig.LocationNumber`、`config:EnemySpawnerConfig.IsEvent`、`config:EnemySpawnerConfig.IsVip`、`config:EnemySpawnerConfig.IsSpecial`、`config:EnemySpawnerConfig.IsDummy`、`config:EnemySpawnerConfig.IsBossFightPart`、`state:quest_progress`；输出 `value:island.mastery_outgoing_multiplier`、`value:island.mastery_incoming_multiplier`

**核验记录**（已写入上方字段）：

- 采纳 `status`（来自 `combat.island_mastery_multipliers`）：DamageCalculator.cs:265 confirms the incoming tail math is not recovered. The status field holds one value per entry, so the split is applied in status_evidence (gate and outgoing recovered_native, incoming reconstructed or inferred) and by tagging the incoming term [inferred] in the expression. The entry-level status stays recovered_native because the gate and outgoing methods are natively annotated and ported.

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

**依赖图节点**：输入 `value:economy.currency_balance`、`state:current_event`、`config:EnemySpawnerConfig.IsEvent`、`config:EnemySpawnerConfig.EventDef.Key`、`constant:EventBalance.red_crystal_damage_per_unit`、`constant:EventBalance.blue_crystal_defence_per_unit`、`constant:EventBalance.currency_double_factor`；输出 `value:player.event_crystal_outgoing_multiplier`、`value:player.event_crystal_incoming_multiplier`

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

**依赖图节点**：输入 `value:player.meta_stat[event_golem_damage]`、`config:EnemySpawnerConfig.IsEvent`、`config:EnemySpawnerConfig.EventDef.UnitType`；输出 `value:player.event_golem_damage_multiplier`

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

**依赖图节点**：输入 `value:player.meta_stat[tower_damage]`、`value:player.meta_stat[tower_damage_reduction]`、`config:EnemySpawnerConfig.IsBossFightPart`；输出 `value:player.boss_tower_outgoing_multiplier`、`value:player.boss_tower_incoming_multiplier`

### `combat.rage_damage_reduction`

**Rage incoming damage reduction**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 合并自：`combat.rage_damage_reduction`、`powerups.rage_damage_reduce`
- 原生地址：`0x140a4cc (DamageCalculator.Calculate)`

```text
in DamageCalculator.Calculate incoming (target not EnemyUnit) branch, non-dodged path, after block: if incoming > 0 and L = RageModel.ActiveRageLevel > 0 and r = Meta('rage_damage_reduce') > 0: incoming /= r * L + 1.0; then *= event, island-mastery (if attacker != null) and global defence multipliers; then (outside the dodge branch) *= SpecialMultiplier if HasSpecialMultiplier; Math.Round
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `L` | RageModel.ActiveRageLevel (= RageLevel if EffectiveMaxLevel > 0, else 0) | rage subsystem (GameContext RageModel, Rage/RageModel.cs:124-131) |
| `r` | meta stat rage_damage_reduce (base 0) | Meta Stats Config key rage_damage_reduce; raised only by amulet upgrade graph_rage_damage_reduce_5_7 = 0.05*level, MaxLevel 3 (AmuletUpgradeDefsBuilder.cs:910-916, 2524), so r <= 0.15; see powerups.amulet_meta_modifiers |

**输出**：incoming damage to the player

**代码**：[Services/Damage/DamageCalculator.cs:97-117](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L97)；[Services/Damage/DamageCalculator.cs:105-110](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L105)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:910-916](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L910)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:2524](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L2524)；[Simulation/Systems/SimulationSystem.cs:960-990](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L960)

**状态依据**：Inline in DamageCalculator.Calculate, annotated 'Native ELF VA 0x140a4cc' (DamageCalculator.cs:66).

**配置参数**：`Meta Stats Config: rage_damage_reduce`；`Amulet Upgrade Config: graph_rage_damage_reduce_5_7`

**使用方**：combat.incoming_damage_to_player；combat incoming damage；simulation.GetSpawnerDps slow path (non-fast; Calculate(hit, true, 1.0), SimulationSystem.cs:985)

**备注**：Defined inside the combat DamageCalculator but driven by rage and amulet powerups. It is applied after block and before the event, island and global-defence multipliers. It is skipped on dodge. With r = 0.15 (upgrade maxed) and ActiveRageLevel 3, incoming is divided by 1.45. ActiveRageLevel can exceed 3 if EffectiveMaxLevel (MaxAccessibleLevel + BossMaxLevelBonus) allows it. Simulation: only the slow GetSpawnerDps path (isFast=false) goes through Calculate(fromSimulation=true) and so includes this reduction, and it reads the LIVE RageModel.ActiveRageLevel rather than a forecast value. The fast path (isFast=true, SimulationSystem.cs:965-977) uses ApplyDamageDefense / AttackCooldown * (1-dodge) * block * global-defence multipliers and omits rage reduction entirely.

**依赖图节点**：输入 `value:rage.active_level`、`value:player.meta_stat[rage_damage_reduce]`；输出 `value:rage.incoming_damage_multiplier`

**核验记录**（已写入上方字段）：

- 合并复核修正：The notes say the simulation path runs the same code and reads the live RageModel, which implies simulation DPS always includes rage reduction. In SimulationSystem.GetSpawnerDps, only the slow path (isFast=false, lines 978-990) calls DamageCalculator.Calculate(hit, true, 1.0) and reaches the rage division, using the live RageModel.ActiveRageLevel rather than a forecast value. The fast path (lines 965-977) computes ApplyDamageDefense(...)/AttackCooldown * (1-dodge) * blockMult * globalDefenceMult and never applies rage reduction. The simulation consumer is also missing from used_by.

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

**依赖图节点**：输入 `constant:DamageCalculator.weapon_type_to_defence_map`；输出 `value:weapon.damage_type_defence_stats`

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

**依赖图节点**：输入 `config:ArmorConfig.Armors[].Type`、`config:ArmorConfig.Armors[].Resist`、`config:EnemySpawnerConfig.ArmorConfig`、`config:BossTowerConfig.Bosses[].ArmorConfig`；输出 `value:enemy.resist_by_damage_type`

### `combat.quest_mob_armor_and_stats`

**Quest chain mob armor and combat stats**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x1424700, 0x1425184, 0x1424d18, 0x1424c74, 0x1425e08, 0x1426018, 0x142608c, 0x1425c54, 0x142623c (all 'independent candidate only')`

```text
BuildConfig: Count = 1, WeaponType = location.PreferredWeaponType, AggroRange = 10, AggroDuration = 8, Difficulty = 1, IsEvent = false, AverageFightTime = (step is last in chain) ? 25 : 15, SpawnArea = ref.SpawnArea*4 + (10,10) ; Armors: for t in {0,1,2,3}: Resist_t = (t == location.PreferredArmorType) ? -0.9 : +0.9 ; BuildFixedStats: Health = 0, Damage = 0, MovementSpeed = 3.5, AttackRange = 3.3f, AttackCooldown = chain.MobAttackCooldown ; Bake: BossSpawnService.BakeStatsForFloor(config, optimum, isWeakened=false) then Damage.BaseValue *= chain.MobAttackMultiplier, Health.BaseValue *= chain.MobHealthMultiplier (optimum = IsDays ? ConvertDaysToOptimum(OptimumTime) : OptimumTime) ; on spawn: if abMult = GetEnemyHpDamageMultiplier(loc) != 1: Health, Damage = round_half_even(BaseValue * abMult), CurrentHealth = Health ; ApplyChainMobSettings (reads post-A/B Health and Damage): Evasion = base 0 + PostFormulaFlat(MobEvasionPercent*0.01f) -> spec = min(0.8, MobEvasionPercent/100) ; HealthRegen = Health * MobHealthRegenMaxHealthPercent * 0.01f ; LifestealPercent = MobLifestealMaxHealthPercent * 0.01f ; ReflectionDamage = Damage * MobReflectionMaxHealthPercent * 0.01f
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `PreferredArmorType / PreferredWeaponType` | location defaults (weakness / attack type) | Locations Config.asset |
| `Mob*Percent, MobAttackCooldown, MobAttackMultiplier, MobHealthMultiplier` | quest chain tuning | QuestChainDef (quests subsystem config) |
| `OptimumTime, IsDays` | step optimum time used for the bake | QuestStepDef |
| `abMult` | A/B enemy HP/damage multiplier for the location | spawners.enemy_spawn_stats (ABTest Data Config) |

**输出**：quest mob EnemySpawnerConfig, baked Health/Damage and runtime stats

**代码**：[Quests/QuestMobSpawnerService.cs:79-105](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestMobSpawnerService.cs#L79)；[Quests/QuestMobSpawnerService.cs:107-154](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestMobSpawnerService.cs#L107)；[Quests/QuestMobSpawnerService.cs:191-215](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestMobSpawnerService.cs#L191)；[Quests/QuestMobSpawnerService.cs:217-220](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestMobSpawnerService.cs#L217)；[Quests/QuestMobSpawnerService.cs:222-232](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestMobSpawnerService.cs#L222)；[Quests/QuestMobSpawnerService.cs:234-266](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestMobSpawnerService.cs#L234)；[Quests/QuestMobSpawnerService.cs:268-286](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestMobSpawnerService.cs#L268)；[Quests/QuestMobSpawnerService.cs:293-301](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestMobSpawnerService.cs#L293)；[BossTower/BossSpawnService.cs:444-499](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/BossSpawnService.cs#L444)；[Unit/Stat.cs:17](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L17),55-58,67-68

**状态依据**：Every method carries an 'Original ELF 0x... ; independent candidate only' comment, so the match to native is unconfirmed.

**配置参数**：`Locations Config: PreferredArmorType, PreferredWeaponType`；`QuestChainDef: MobEvasionPercent, MobHealthRegenMaxHealthPercent, MobLifestealMaxHealthPercent, MobReflectionMaxHealthPercent, MobAttackCooldown, MobAttackMultiplier, MobHealthMultiplier`；`QuestStepDef: OptimumTime, IsDays, LocationNumber, Prefab`；`ABTest Data Config: EnemyHpDamageMult`；`constants: AggroRange 10, AggroDuration 8, MoveSpeed 3.5, AttackRange 3.3, AverageFightTime 25/15, armor resist +/-0.9`

**使用方**：combat.outgoing_damage_to_enemy；combat.enemy_reflection；combat.enemy_regen；combat.enemy_lifesteal_on_hit；combat.incoming_damage_to_player

**备注**：Defined in the Quests folder and listed here for the ArmorConfig structure. Quest mobs are excluded from island mastery (QuestMobTag). Unlike the asset tables, quest armor resists Magic at +0.9 unless Magic is the weakness. Health and Damage start at 0 in BuildFixedStats and are filled entirely by the bake. The bake solves Damage from the player snapshot with AverageFightTime 25/15, Difficulty 1 and attackers = min(4, (1+1)/2) = 1, then applies the chain multipliers, then the A/B factor with explicit banker's rounding. The 'MaxHealthPercent' names are misleading in two places: reflection is a percent of mob Damage, not HP, and LifestealPercent heals that fraction of the mob's own max HP per damaging hit on the player. The 0.01 factors are float (0.01f).

**依赖图节点**：输入 `config:QuestConfig.Chains[].MobAttackCooldown`、`config:QuestConfig.Chains[].MobAttackMultiplier`、`config:QuestConfig.Chains[].MobHealthMultiplier`、`config:QuestConfig.Chains[].MobEvasionPercent`、`config:QuestConfig.Chains[].MobHealthRegenMaxHealthPercent`、`config:QuestConfig.Chains[].MobLifestealMaxHealthPercent`、`config:QuestConfig.Chains[].MobReflectionMaxHealthPercent`、`config:QuestConfig.Chains[].Steps`、`config:QuestConfig.Chains[].Steps[].OptimumTime`、`config:QuestConfig.Chains[].Steps[].IsDays`、`config:QuestConfig.Chains[].Steps[].LocationNumber`、`config:LocationsConfig.Locations[].PreferredWeaponType`、`config:LocationsConfig.Locations[].PreferredArmorType`、`value:spawner.optimum_time_days`、`config:SimulationConfig.TimeChunkDurationMinutes`、`value:sim.snapshot_at_time`、`value:sim.snapshot_progress`、`value:sim.balance_coefficient[PlayerAbilityDPSMult]`、`value:sim.balance_coefficient[PlayerAblityEHPMult]`、`value:player.battle_regen`、`value:enemy.solved_attack_damage`、`value:enemy.ab_hp_damage_multiplier`、`state:quest_progress`、`constant:QuestMobSpawnerService.fight_time_last_step`、`constant:QuestMobSpawnerService.fight_time_step`、`constant:QuestMobSpawnerService.difficulty`、`constant:QuestMobSpawnerService.count`、`constant:QuestMobSpawnerService.move_speed`、`constant:QuestMobSpawnerService.attack_range`、`constant:QuestMobSpawnerService.weakness_resist`、`constant:QuestMobSpawnerService.other_resist`、`constant:BossSpawnService.max_attackers`、`constant:BossSpawnService.resist_cap`、`constant:BossSpawnService.beyond_snapshot_growth`、`constant:BossSpawnService.damage_round_step`；输出 `value:enemy.override_base_stats`、`value:enemy.damage_type`、`value:enemy.resist_by_damage_type`、`value:enemy.stat_base`、`value:enemy.current_hp`、`value:enemy.reflection_damage`、`value:sim.target_incoming_damage_per_hit`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `combat.quest_mob_armor_and_stats`）：Verified: BuildFixedStats sets Health and Damage to 0 (226-227). The bake runs at line 95 with chain.MobAttackMultiplier/MobHealthMultiplier (BossSpawnService.cs:494-499). The A/B banker's rounding is at 119-151. AggroRange 10, AggroDuration 8 and Difficulty 1 are at 205-206 and 213. The expression, config_params and notes now include these, and the BuildConfig code_ref is corrected to 191-215 (the method spans 191-215, and its initializer ends at 214).

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

**依赖图节点**：输入 `value:player.meta_stat[block_to_supercrit]`、`value:unit.hit_roll_outcome`；输出 `value:player.supercrit_bonus_from_block`

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

**依赖图节点**：输入 `value:weapon.stat_stack_multiplier[AttackSpeed]`、`value:weapon.ability_attack_speed_multiplier`、`value:enemy.weak_or_event_golem_flag`、`constant:PlayerAttackSystem.WeakEnemyAttackSpeedMultiplier`、`constant:PlayerAttackSystem.EventGolemAttackSpeedMultiplier`；输出 `value:weapon.attack_speed_multiplier`

### `combat.player_weapon_attack_cycle`

**Player weapon attack cycle / cooldown**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x1440178 (Update), 0x1441034 (Attack), 0x1440f74 (GetHitCooldownThreshold), 0x1440fa0 (GetAnimationCastPoint), 0x1440a9c (GetAttackRangeSqr), 0x1440de4 / 0x1440eac (weak enemy / event golem checks), 0x14630e8 (ItemConfig.HitFrameAt60Fps)`

```text
Update, per equipped weapon each frame: hasTarget = current target alive and within GetAttackRangeSqr, OR out of range for < 0.2 s (retention), else FindTarget ; as = prod_states GetWeaponStatMultiplier(AttackSpeed) * AbilityAttackSpeedMultiplier * (2.5 if the target is a weak enemy or event golem) ; T = cp*AC (GetHitCooldownThreshold) ; if hasTarget: Cooldown -= as*dt ; else if Cooldown > T: { Cooldown += 3*as*dt ; if Cooldown >= AC: Cooldown = 0, AttackInProgress = false } ; else { AttackInProgress = false (pending hit cancelled) ; Cooldown -= as*dt } ; WearingTime += dt ; ResetCooldown path 1: if ResetCooldown && !AttackInProgress && Cooldown <= AnimationBackPoint*AC: ResetCooldown = false, AttackInProgress = true, Cooldown = T + 0.15 (forced hit about 0.15/as s later). Then Attack() if hasTarget and alive (skips if dying, dead, sleeping or !CanMove), per weapon: skip if slot 'weapon_1' and sqrDist >= baseAttackRange^2 ; skip if WearingTime < 1 ; if Cooldown <= 0: AttackInProgress = true, Cooldown = AC (swing starts, no hit this frame) ; else if AttackInProgress && Cooldown <= T + as*dt: hit fires (AttackInProgress = false ; ResetCooldown path 2: if ResetCooldown: ResetCooldown = false, Cooldown = 0, so the next swing starts on the next Attack()) => steady period ~= AC/as (+ up to 1 frame), hit ~= (1-cp)*AC/as - dt after swing start
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `AC` | weapon attack cooldown (s) | ItemConfig.AttackCooldown (e.g. cut common 1.3, smash legendary 1.7, div smash legendary 3) |
| `as` | weapon attack speed multiplier | combat.attack_speed_multiplier |
| `cp` | effective cast point = 1 - (1-AnimationCastPoint)/(factor*k + 1), factor Cut 0.5 / Buckshot 1 / else 0, k = smoothed joystick*MoveSpeed_spec*0.05 | combat.animation_cast_point |
| `AnimationBackPoint` | reset window as a fraction of AC | ItemConfig.AnimationBackPoint |
| `ResetCooldown` | extra-hit flag | ResetCooldownEffect, TriggerHighestDamageWeaponEffect, RageExtraHitSystem |
| `dt` | Time.deltaTime | runtime |

**输出**：hit timing. Per-weapon DPS ~= hitDamage * as / AC.

**代码**：[Player/PlayerAttackSystem.cs:85-151](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L85)；[Player/PlayerAttackSystem.cs:97-118](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L97)；[Player/PlayerAttackSystem.cs:119-148](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L119)；[Player/PlayerAttackSystem.cs:153-191](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L153)；[Player/PlayerAttackSystem.cs:206-211](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L206)；[Player/PlayerAttackSystem.cs:225-229](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L225)；[Player/PlayerAttackSystem.cs:238-244](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L238)；[Player/Inventory/ItemConfig.cs:25](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemConfig.cs#L25)；[Player/Inventory/ItemConfig.cs:32](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemConfig.cs#L32)；[Player/Inventory/ItemConfig.cs:35](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemConfig.cs#L35)；[Player/Inventory/ItemConfig.cs:49](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemConfig.cs#L49)；[Player/Systems/RageExtraHitSystem.cs:57](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/RageExtraHitSystem.cs#L57)

**状态依据**：VA comments on Update (Native ELF VA 0x1440178), Attack (0x1441034) and GetHitCooldownThreshold (0x1440f74).

**配置参数**：`ItemConfig.AttackCooldown`；`ItemConfig.AnimationCastPoint`；`ItemConfig.AnimationBackPoint`；`constants: wearing delay 1 s, no-target rewind 3x, reset hit offset 0.15 s, weak/golem speed 2.5x, target retention 0.2 s`

**使用方**：simulation DPS (Damage/AttackCooldown, SimulationSystem.cs:848)；items ItemModel.DPS = Damage / AttackCooldown ([Player/Inventory/ItemModel.cs:397-405](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L397))

**备注**：All equipped weapons cycle in parallel, so total DPS is the sum over weapons. The weapon_1 slot also requires the base AttackRange (no chain x3). A target that leaves range is kept for 0.2 s (hasTarget stays true and Cooldown keeps ticking down) before re-targeting. Without a target, a swing still before its hit threshold is rewound at 3x speed back to ready; a swing already past it keeps cooling and its pending hit is cancelled. The +as*dt look-ahead makes the hit fire one frame early. The hit is emitted as a NextFrame WeaponHitEvent, so damage lands one frame after the threshold crossing. Both simulation and ItemModel.DPS ignore attack speed and cast timing. ItemConfig.HitFrameAt60Fps = (1 - AnimationCastPoint)*AC*60 uses the RAW config cast point, not the effective cp, and is display or editor only.

**依赖图节点**：输入 `config:ItemConfig.AttackCooldown`、`config:ItemConfig.AnimationBackPoint`、`value:weapon.attack_speed_multiplier`、`value:weapon.cast_point_effective`、`value:weapon.cooldown_reset_flag`、`value:player.auto_target`、`value:weapon.can_attack`、`value:player.equipped_items`、`value:unit.is_dead`、`state:frame_delta_time`、`constant:PlayerAttackSystem.ResetCooldownPostCastPointDelay`、`constant:PlayerAttackSystem.AttackCancelRewindMultiplier`；输出 `value:weapon.attacks_per_second`

**核验记录**（已写入上方字段）：

- 采纳 `expression`（来自 `combat.player_weapon_attack_cycle`）：Verified at PlayerAttackSystem.cs:127-140 (the no-target branch sets AttackInProgress=false at 133 and 138 and keeps cooling at 139), :142-147 (ResetCooldown path 1, Cooldown = cp*AC + 0.15) and :177-181 (path 2 sets Cooldown = 0 when the hit lands). The expression describes both paths and the AttackInProgress resets.
- 合并复核修正：(b) The notes say 'ItemConfig.HitFrameAt60Fps = (1-cp)*AC*60', where cp is the entry's EFFECTIVE (joystick-dependent) cast point. ItemConfig.cs:49 actually computes (1f - AnimationCastPoint) * AttackCooldown * 60f from the raw config value. It does not use GetAnimationCastPoint, so for Cut/Buckshot weapons on the move it differs from the live hit frame. The entry also leaves out the 0.2 s out-of-range target retention (PlayerAttackSystem.cs:104-111) that defines hasTarget, which affects whether Cooldown keeps ticking down or rewinds.

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
| `MoveSpeed_spec` | player movement speed specific value | stats.specific_value_pipeline |

**输出**：cp used for hit threshold

**代码**：[Player/PlayerAttackSystem.cs:93-95](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L93)；[Player/PlayerAttackSystem.cs:206-211](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L206)；[Player/PlayerAttackSystem.cs:26-32](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L26)

**状态依据**：VA comments with the type factor table [0, .5, 0, 1].

**配置参数**：`ItemConfig.AnimationCastPoint`；`ItemConfig.DamageType`

**使用方**：combat.player_weapon_attack_cycle

**备注**：Moving makes cut and smash hits land earlier within a cycle but does not shorten the period. It mostly affects first-hit latency.

**依赖图节点**：输入 `config:ItemConfig.AnimationCastPoint`、`config:ItemConfig.DamageType`、`value:player.stat_specific[MovementSpeed]`、`state:player_input`、`state:frame_delta_time`、`constant:PlayerAttackSystem.AnimationCastPointSpeedFactor`、`constant:PlayerAttackSystem.cast_point_smoothing_rate`、`constant:PlayerAttackSystem.CuttingCastPointSpeedMultiplier`、`constant:PlayerAttackSystem.BuckshotCastPointSpeedMultiplier`、`constant:PlayerAttackSystem.StabbingCastPointSpeedMultiplier`；输出 `value:weapon.cast_point_effective`

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

**依赖图节点**：输入 `value:player.stat_raw[AttackRange]`、`value:player.has_chain_weapon`、`constant:PlayerAttackSystem.WeakEnemyAttackRangeMultiplier`、`constant:PlayerAttackSystem.EventGolemAttackRangeMultiplier`、`value:player.meta_stat[weak_spawner_extra_range]`、`value:player.meta_stat[event_golem_extra_range]`、`value:player.unlocked_location`、`config:EnemySpawnerConfig.LocationNumber`、`config:EnemySpawnerConfig.IsVip`、`config:EnemySpawnerConfig.IsSpecial`、`config:EnemySpawnerConfig.IsEvent`、`config:EnemySpawnerConfig.EventDef.UnitType`、`state:unit_positions`、`value:enemy.is_far`、`value:unit.is_dead`、`constant:PlayerAttackSystem.TargetOutOfRangeRetainDuration`、`state:frame_delta_time`；输出 `value:player.attack_range_effective`、`value:player.auto_target`、`value:enemy.weak_or_event_golem_flag`

### `combat.enemy_attack_cycle`

**Enemy attack cadence**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 合并自：`combat.enemy_attack_cycle`、`spawners.enemy_attack_cadence`

```text
each frame for non-static enemies that are not IsDead, not IsFar and not in spawn animation. Ground: return if VampireSpiritTag, EnemyCanEscapeMarker, stunned or IsCasting; else if AttackCooldown > 0: AttackCooldown -= dt; if retreat or player unavailable or waiting marker: no attack; else if dist^2 < AttackRange^2: Attack. Flying: return if EnemyCanEscapeMarker or stunned; if AttackCooldown > 0: AttackCooldown -= dt; (move) ; if dist < AttackRange: Attack (regardless of retreat/unavailable/waiting). Attack: if AttackCooldown > 0 return; emit WeaponHitEvent{Source=enemy, Target=player, DamageType=enemy.DamageType}; AttackCooldown = (float)GetStatValue(AttackCooldown). AttackCooldown = 0 at Initialize, so the first hit is instant. Hit rate ~= 1/AttackCooldown_stat; enemy DPS ~= incoming(Damage)/AttackCooldown_stat
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `AttackCooldown_stat` | enemy stat 5 (s) | EnemySpawnerConfig.Stats[Type=5] |
| `AttackRange` | enemy stat 4 | EnemySpawnerConfig.Stats[Type=4] |
| `MovementSpeed` | enemy stat 0; x2 while RetreatTimer > 0 | EnemySpawnerConfig.Stats[Type=0] |
| `AggroRange, AggroDuration` | aggro parameters gating the retreat state | EnemySpawnerConfig.AggroRange / AggroDuration |

**输出**：WeaponHitEvent frequency to the player (damage type = config.WeaponType; per-hit damage from DamageCalculator)

**代码**：[Enemy/EnemyAttackSystem.cs:52-73](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L52)；[Enemy/EnemyAttackSystem.cs:81-100](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L81)；[Enemy/EnemyAttackSystem.cs:102-105](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L102)；[Enemy/EnemyAttackSystem.cs:110-142](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L110)；[Enemy/EnemyAttackSystem.cs:258-270](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L258)；[Enemy/EnemyAttackSystem.cs:276-303](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L276)；[Enemy/EnemyAttackSystem.cs:342-348](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L342)；[Enemy/EnemyUnit.cs:154](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L154)；[Enemy/EnemyUnit.cs:313-316](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L313)；[Player/Commands/RevivePlayerCommandHandler.cs:37](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Commands/RevivePlayerCommandHandler.cs#L37)；[BossTower/Systems/MageBossSystem.cs:93](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/Systems/MageBossSystem.cs#L93)

**状态依据**：EnemyAttackSystem and the relevant EnemyUnit methods have no VA comments.

**配置参数**：`EnemySpawnerConfig.Stats[Type=0,3,4,5]`；`EnemySpawnerConfig.WeaponType`；`EnemySpawnerConfig.AggroRange`；`EnemySpawnerConfig.AggroDuration`；`EnemySpawnerConfig.IsStatic (static enemies never attack)`

**使用方**：combat.incoming_damage_to_player；combat damage model；simulation.GetSpawnerDps (damage / AttackCooldown, [Simulation/Systems/SimulationSystem.cs:960](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L960))

**备注**：Stun freezes AttackCooldown for both ground and flying enemies (both return before the decrement). IsCasting freezes cooldown and movement for ground enemies only. Ground enemies keep decrementing AttackCooldown during retreat, player-unavailable and waiting frames, but can only attack in the non-retreat branch (lines 140-142). Flying enemies call Attack() whenever distance < AttackRange, even while retreating, while the player is unavailable or sleeping, or while a waiting marker is set. Escaping enemies (EnemyCanEscapeMarker, both handlers) never attack. VampireSpirit-tagged enemies never attack in the ground handler; the flying handler has no such check. The ground range test is dist^2 < R^2; the flying test is dist < R. The timer is reset to the full cooldown without carrying the negative remainder, so the real period is quantized up to whole frames. The attack animation speed is 1/AttackCooldown_stat (EnemyUnit.cs:313-316), which is visual only. Aggro and retreat (detailed in combat.enemy_leash_and_aggro): dist < AggroRange sets AggroTimer = AggroDuration; retreat when dist > AggroRange && AggroTimer <= 0 && the player is not targeting the enemy, or RetreatTimer > 0. Move speed = MovementSpeed * (RetreatTimer > 0 ? 2 : 1). RetreatTimer is set to 3 s for aggroed enemies on player revive (with AggroTimer = 0) and to RetreatDuration by MageBossSystem. Scene WeaponType counts: Stabbing(0) 474, Cutting(1) 147, Buckshot(3) 96.

**依赖图节点**：输入 `value:enemy.stat_raw[AttackCooldown]`、`value:enemy.stat_raw[AttackRange]`、`value:enemy.aggro_state`、`config:EnemySpawnerConfig.WeaponType`、`config:BossTowerConfig.Bosses[].WeaponType`、`config:EnemySpawnerConfig.IsStatic`、`value:enemy.stun_duration`、`value:enemy.is_far`、`value:unit.is_dead`、`state:unit_positions`、`state:frame_delta_time`、`state:boss_tower_context`；输出 `value:enemy.attacks_per_second`、`value:enemy.damage_type`

**核验记录**（已写入上方字段）：

- 采纳 `code_refs`（来自 `combat.enemy_attack_cycle`）：EnemyUnit.cs:154 sets AttackCooldown = 0f in Initialize and is now cited for the instant first hit. 313-316 is kept, cited only for the visual animation speed.
- 采纳 `notes`（来自 `combat.enemy_attack_cycle`）：Points (a), (b), (c) and (e) are confirmed at EnemyAttackSystem.cs:91-105, 111-135 and 302-303. Point (d) is applied with a precision fix: the VampireSpiritTag early return exists only in HandleGroundEnemy (line 83); HandleFlyingEnemy has no such check. The expression and notes now reflect this.
- 采纳 `notes`（来自 `spawners.enemy_attack_cadence`）：Confirmed: flying Attack at 302-303 is outside the retreat branch, ground attacks only at 140-142, escaping enemies never attack, RevivePlayerCommandHandler.cs:37 sets RetreatTimer = 3f for aggroed enemies, MageBossSystem.cs:93 sets RetreatDuration, and EnemyUnit.cs:154 gives the immediate first hit. All of this is now in the expression, notes and code_refs.

### `combat.enemy_leash_and_aggro`

**Enemy leash full-heal, aggro and retreat**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 合并自：`combat.enemy_leash_and_aggro`、`spawners.enemy_leash_heal`
- 原生地址：`0x1469b28 (RevivePlayerCommandHandler.Execute only)`

```text
Per Update, for each enemy in NonStaticEnemies that is not dead, not IsFar and not in spawn anim (ground: also skipped if VampireSpirit, escaping, stunned or casting; flying: skipped if escaping or stunned) : RetreatTimer -= dt ; LEASH: if dist(enemy, player) > 30 (ground: 3D sqrMagnitude > 900f; flying: Vector3.Distance > 30f) and !IsDead and !BossArenaUnitTag: CurrentHealth = GetStatValue(Health) ; retreat = (dist > AggroRange [ground compares sqrDist > AggroRange^2] and AggroTimer <= 0 and player.CurrentTarget != enemy) or RetreatTimer > 0 ; if retreat or player unavailable (sleeping, !CanMove, dying) or waiting marker: return to CurrentSpawnerPatrolPoint (AggroTimer not decremented) ; else { AggroTimer -= dt ; if dist < AggroRange: AggroTimer = AggroDuration ; chase / attack } ; move speed = MovementSpeed_raw * (RetreatTimer > 0 ? 2 : 1) (ground: Agent.speed; flying: step per frame); escaping enemies use MovementSpeed_raw x1 ; on player revive: for EVERY enemy in EnemiesModel.Enemies: PreviousHealth = CurrentHealth, CurrentHealth = GetStatValue(Health); and if AggroTimer > 0: AggroTimer = 0, RetreatTimer = 3
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `dist` | 3D world distance enemy-player (ground compares squared distance) | runtime transforms (EnemyAttackSystem.Distance / SqrDistance) |
| `AggroRange, AggroDuration` | aggro radius (m) and aggro hold time (s) | EnemySpawnerConfig.AggroRange / AggroDuration via EnemyUnit.AggroRange/AggroDuration (Enemy/EnemyUnit.cs:90-102); quest mobs use 10 / 8 |
| `AggroTimer, RetreatTimer` | per-enemy runtime timers (s) | EnemyUnit runtime state |
| `MovementSpeed_raw` | enemy stat 0 raw value (GetStatValue, not the specific curve) | EnemySpawnerConfig.Stats[Type=0] |
| `Health` | enemy max HP (stat 1 fast value) | EnemySpawnerConfig.Stats[Type=1] x A/B multiplier |
| `30` | hard-coded leash radius (m) | constant in EnemyAttackSystem |

**输出**：enemy CurrentHealth reset to max HP, pursuit/retreat state, and movement speed

**代码**：[Enemy/EnemyAttackSystem.cs:52-74](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L52)；[Enemy/EnemyAttackSystem.cs:81-169](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L81)；[Enemy/EnemyAttackSystem.cs:107-109](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L107)；[Enemy/EnemyAttackSystem.cs:249](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L249)；[Enemy/EnemyAttackSystem.cs:258-304](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L258)；[Enemy/EnemyAttackSystem.cs:274-275](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L274)；[Enemy/EnemyAttackSystem.cs:350-358](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L350)；[Enemy/EnemyUnit.cs:90-102](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L90)；[Player/Commands/RevivePlayerCommandHandler.cs:26-43](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Commands/RevivePlayerCommandHandler.cs#L26)

**状态依据**：EnemyAttackSystem (Update, HandleGroundEnemy, HandleFlyingEnemy) has no native VA comments, so the leash, aggro and retreat logic is reconstructed. Only the revive handler is annotated (Native ELF VA 0x1469b28).

**配置参数**：`EnemySpawnerConfig.AggroRange`；`EnemySpawnerConfig.AggroDuration`；`EnemySpawnerConfig.Stats[Type=0] (MovementSpeed)`；`EnemySpawnerConfig.Stats[Type=1] (Health)`；`constant leash radius 30 m`；`constant revive RetreatTimer 3 s`；`constant retreat speed multiplier 2`

**使用方**：combat.event_enemy_damage_ramp (a leash or revive full-heal brings HP back to max, which resets the ramp timer to 0)；spawners / boss arena (BossArenaUnitTag units are exempt)

**备注**：The 30 m leash full-heal prevents kiting or chip damage across fights. The radius is hard-coded and not a config lever. Boss-arena units are exempt. Both ground and flying use full 3D distance (ground: sqrMagnitude vs 900). The heal does not run on enemies that are IsFar, stunned, casting (ground) or escaping, because those branches return before the leash check. On revive, ALL enemies are refilled to max HP, not just aggroed ones. Only aggroed ones (AggroTimer > 0) also get the 3 s double-speed retreat. Quirk (flying): the `if (distance < attackRange) Attack(enemy)` call at EnemyAttackSystem.cs:302-303 sits outside the retreat branch, so a flying enemy within AttackRange still attacks while retreating or while the player is unavailable. The spawners agent catalogued the leash-only subset as spawners.enemy_leash_heal.

**依赖图节点**：输入 `config:EnemySpawnerConfig.AggroRange`、`config:EnemySpawnerConfig.AggroDuration`、`config:BossTowerConfig.Bosses[].AggroRange`、`config:BossTowerConfig.Bosses[].AggroDuration`、`constant:QuestMobSpawnerService.aggro_range`、`constant:QuestMobSpawnerService.aggro_duration`、`constant:EnemyAttackSystem.leash_radius`、`constant:EnemyAttackSystem.retreat_speed_multiplier`、`constant:RevivePlayerCommandHandler.revive_retreat_timer`、`value:enemy.stat_raw[MovementSpeed]`、`value:enemy.stat_raw[Health]`、`value:player.auto_target`、`state:unit_positions`、`state:frame_delta_time`、`state:player_actions`、`state:boss_tower_context`、`value:enemy.is_far`、`value:enemy.stun_duration`、`value:unit.is_dead`；输出 `value:enemy.current_hp`、`value:enemy.move_speed_effective`、`value:enemy.aggro_state`

**核验记录**（已写入上方字段）：

- 合并复核修正：(b) Code line references are off. HandleFlyingEnemy runs to line 304, so the code_ref '258-302' cuts off the Attack call. The notes cite the flying Attack call as 'EnemyAttackSystem.cs:301-302', but line 301 is `enemy.LastDestination = target` and the `if (distance < attackRange) Attack(enemy)` is at 302-303. The entry also does not say that the ground leash uses the full 3D sqrMagnitude (SqrDistance, line 355-358), not a planar distance. The 'ground dist^2' wording in the original unit description could be read as planar.

### `combat.enemy_regen`

**Enemy HP regeneration**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 合并自：`combat.enemy_regen`、`spawners.enemy_regen`

```text
each frame for enemies without UnitDeadMarker, FarFromPlayerMarker or UnitWithoutRegenMarker: HP = min(HP + (GetStatValue(HealthRegen) + (LastDamageGet + 5 <= Time.time ? 0.05 * MaxHP : 0)) * dt, MaxHP)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `HealthRegen` | enemy stat 2; GetStatValue returns its specific value for HealthRegen | EnemySpawnerConfig.Stats[Type=2]; quest mobs: Health * MobHealthRegenMaxHealthPercent * 0.01f (Quests/QuestMobSpawnerService.cs:282) |
| `MaxHP` | enemy Health stat Value | EnemySpawnerConfig.Stats[Type=1] |
| `0.05, 5` | 5% MaxHP/s idle regen after 5 s without damage | constants EnemyRegenSystem.HEAL_PER_SECOND / NO_DAMAGE_TIME_TO_HEAL |
| `LastDamageGet` | last time the enemy took damage (also refreshed by player thorns) | UnitDamageSystem |

**输出**：enemy CurrentHealth over time

**代码**：[Enemy/Systems/EnemyRegenSystem.cs:12-13](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/EnemyRegenSystem.cs#L12)；[Enemy/Systems/EnemyRegenSystem.cs:16](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/EnemyRegenSystem.cs#L16)；[Enemy/Systems/EnemyRegenSystem.cs:19-29](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Systems/EnemyRegenSystem.cs#L19)；[Unit/UnitCore.cs:107-114](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitCore.cs#L107)；[Quests/QuestMobSpawnerService.cs:282](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestMobSpawnerService.cs#L282)

**状态依据**：EnemyRegenSystem has no VA comment. HEAL_PER_SECOND=0.05 and NO_DAMAGE_TIME_TO_HEAL=5 are named constants from metadata.

**配置参数**：`EnemySpawnerConfig.Stats[Type=1,2]`；`QuestChainDef.MobHealthRegenMaxHealthPercent`

**使用方**：combat

**备注**：From near-zero HP, a full heal takes 20 s of idle regen, which ends about 25 s after the last damage (5 s delay + 20 s), less if HealthRegen > 0. Regen is skipped for enemies with FarFromPlayerMarker, UnitWithoutRegenMarker (crystal objectives) or UnitDeadMarker. The separate leash full-heal (distance > 30) lives in EnemyAttackSystem; see combat.enemy_leash_and_aggro.

**依赖图节点**：输入 `value:enemy.current_hp`、`value:enemy.stat_specific[HealthRegen]`、`value:enemy.stat_raw[Health]`、`constant:EnemyRegenSystem.HEAL_PER_SECOND`、`constant:EnemyRegenSystem.NO_DAMAGE_TIME_TO_HEAL`、`value:enemy.is_far`、`value:unit.is_dead`、`state:frame_delta_time`；输出 `value:enemy.current_hp`、`value:enemy.hp_regen_per_second`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `combat.enemy_regen`）：EnemyRegenSystem.cs:16 filters the three markers. Line 26 starts idle regen at LastDamageGet+5, and 0.05/s needs 20 s to fill, so a full heal ends about 25 s after the last hit. The notes are replaced accordingly.

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

**依赖图节点**：输入 `value:player.current_hp`、`value:player.stat_specific[HealthRegen]`、`value:player.ability_regen_multiplier`、`value:player.stat_raw[Health]`、`state:frame_delta_time`；输出 `value:player.current_hp`、`value:player.hp_regen_per_second`

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

**依赖图节点**：输入 `value:player.current_hp`、`value:enemy.current_hp`、`value:enemy.stat_raw[Health]`、`value:player.outgoing_damage_per_hit`、`constant:UnitCore.death_hp_threshold`、`constant:UnitDamageSystem.oneshot_full_hp_tolerance`；输出 `value:unit.is_dead`、`value:unit.ability_trigger_events`

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

**依赖图节点**：输入 `state:player_actions`、`value:player.stat_raw[Health]`、`value:enemy.stat_raw[Health]`、`constant:RevivePlayerCommandHandler.revive_hp`、`constant:RevivePlayerCommandHandler.revive_location`、`constant:RevivePlayerCommandHandler.last_damage_get_shift`；输出 `value:player.current_hp`、`value:enemy.current_hp`、`value:rage.level`、`value:player.current_location`

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

**依赖图节点**：输入 `value:player.incoming_damage_per_hit`、`constant:PlayerHitsTakenDecaySystem.OutOfCombatTimeout`、`state:frame_delta_time`；输出 `value:player.hits_taken_count`

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

**依赖图节点**：输入 ；输出 `value:player.auto_target`

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

**使用方**：spawners.enemy_spawn_stats

**备注**：Companion to the spawn-time multiplier. Repeated rounding can drift values. Modifiers on those stats are dropped because a new Stat(base, type) is created.

**依赖图节点**：输入 `state:ab_cohort`、`config:ABTestDataConfig.enemyHpDamageMult.variants[].data.damageMultiplier.defaultValue`、`config:ABTestDataConfig.enemyHpDamageMult.variants[].data.damageMultiplier.overridesByLocation.keyData`、`config:ABTestDataConfig.enemyHpDamageMult.variants[].data.damageMultiplier.overridesByLocation.valueData`、`config:EnemySpawnerConfig.LocationNumber`、`config:EnemySpawnerConfig.IsEvent`、`value:enemy.stat_base`、`value:enemy.stat_raw[Health]`、`value:enemy.current_hp`；输出 `value:enemy.ab_hp_damage_multiplier`、`value:enemy.stat_base`、`value:enemy.current_hp`

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

**依赖图节点**：输入 `state:player_actions`、`state:frame_delta_time`、`constant:PlayerAttackSystem.equip_warmup_seconds`、`value:player.equipped_items`、`config:PlayerConfig.Slots[].Key`、`value:player.stat_raw[AttackRange]`、`value:player.attack_range_effective`、`value:player.auto_target`、`state:unit_positions`；输出 `value:weapon.can_attack`

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

**依赖图节点**：输入 `value:weapon.cooldown_reset_flag`、`value:ability.proc_roll`、`value:weapon.item_dps`、`value:player.equipped_items`、`value:ability.effective_value`、`value:ability.scaled_value`、`config:ItemConfig.AttackCooldown`、`config:ItemConfig.AnimationBackPoint`、`value:weapon.cast_point_effective`、`constant:PlayerAttackSystem.reset_cooldown_offset`；输出 `value:weapon.attacks_per_second`、`value:weapon.empowered_hit_multiplier`

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

**使用方**：combat.player_lifesteal_on_hit；powerups.marks

**备注**：The 10% base piercing mark chance is hard-coded. If stats.lifesteal_player / combat.player_lifesteal_on_hit omit the mark_lifesteal multiplier, those entries are incomplete. Note that the general ApplyLifesteal (enemy on player) does not cap health, while the player path caps at HPmax.

**依赖图节点**：输入 `value:player.meta_stat[mark_lifesteal]`、`value:player.meta_stat[evasion_mark]`、`value:player.meta_stat[extra_mark]`、`value:enemy.mark_stacks`、`value:enemy.max_marks`、`constant:PiercingWeaponMarkSystem.base_piercing_mark_chance`、`value:ability.scaled_value`、`value:item.unlocked_abilities`、`config:ItemConfig.DamageType`、`value:player.equipped_items`、`value:unit.hit_roll_outcome`、`state:rng`；输出 `value:enemy.mark_stacks`、`value:player.mark_lifesteal_multiplier`

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

**使用方**：meta.devour_stat_reward_scaling；simulation.spawner_fight_model

**备注**：This is small, but it is per-kill overhead that the simulation folds into its constant RPM tuple (70, 50, 2, 2). It matters for fast-kill spawners (fight time of 1-2 s).

**依赖图节点**：输入 `config:PlayerDevour.floatingTime`、`config:PlayerDevour.catchingTime`、`constant:PlayerDevour.FinishTimeout`、`state:player_actions`、`state:frame_delta_time`；输出 `value:reward.devour_delay_seconds`

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
