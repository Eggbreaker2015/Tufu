# 护符、怒气与能力（`powerups`）

[返回公式清单总览](../FormulaCatalog.md)

## 概述

Powerups add combat power through three systems: the Amulet upgrade graph, Rage, and item Abilities. Rewarded-ad stat grants are covered too. The Amulet is a permanent skill tree of 298 nodes, defined in code by AmuletUpgradeDefsBuilder with native per-level lambdas. Each level of a node sets an absolute value, not a sum of earlier levels: value(L) = f(L) * StatModifierMultipliers[key]. The cost to reach level L is PrettyRound(c(L) * UpgradeCostMultipliers[key]), paid in souls. Both multiplier dictionaries in Amulet Config.asset are the baked output of an editor auto-balance tool that was not in the APK. Paid soul costs become amulet XP (BaseExperience[soul] * souls). That XP sets amulet level 1-7 through cumulative thresholds, and the level gates which nodes can be bought. At full tree the amulet gives about +13.59M flat Health, +0.6-0.8 PreAdditivePostMultiplicative % per damage/defence type, and post-formula flat +6% crit/evasion/block. It also provides keystone meta stats that feed Rage (extra health, damage reduction, extra hits), marks, lightning and the economy proc chance. Rage is a 0-3 level (4 in the Berserker boss fight) per-fight buff. It fills from AddRageEffect procs on hit against fixed thresholds [30,100,270,270], decays after 10 s without gain, and is unlocked by GrantRageAccessEffect on items. Its strength comes entirely from amulet meta stats and crystal legendaries. Abilities are data-driven (AbilityDef in item assets): value/chance/stacks are tier overrides chosen by item stars plus per-frame boosts, multiplied by a ValueScaler. Each frame they rebuild multiplicative per-weapon and per-player damage, attack-speed, crit and regen factors. They also add one-shot next-hit/empower bonuses, lightning, marks, shock, thorns and heals, and can grant permanent stats as rewards. Rewarded ads grant a permanent flat stat equal to round(2x? * 0.5 * RPM[stat] * 6 * locMult).

**核验结论**：The catalog is largely accurate. I checked all 36 formulas against the code and found no rejections. I re-derived the amulet numbers with a script that parses all 298 AmuletUpgradeDefsBuilder defs and the baked multipliers in Amulet Config.asset. Stat totals, meta lambda values, XP thresholds, and the native/stub status labels all hold up.

Corrections:
- **Amulet cost (main defect):** cost is PrettyRound(PrettyRound(c(L)) * CM), not single-rounded. UpgradeCostDef.Evaluate already rounds before UpgradeDef.EvaluateCosts multiplies. This changes non-integer shapes such as L^1.5 and L^1.2. Corrected totals: elite 84,435 (not ~84.6k), common 12,143,385, XP 41.59M.
- **Two open questions are wrong:** the AbilityHealthRegenMultiplier consumer exists (PlayerRegenSystem.cs:36), and DoubleLoot is consumed in PlayerRewardSystem.cs:55-63.
- **Callers that do not exist:** no code calls AddSouls(isSimulation=true) or AmuletModel.AddLevels. Their used_by claims are unsupported.
- **AddStatEffect:** the daily-proc check comes before the temporary branch too.
- **Ability conditions:** CombatCondition has a Mode inversion and StacksCondition has a NotReachedMax mode; both were missing. The cooldown tick lives in ResetPerFrameState.
- **Lightning:** its base damage is the already-mitigated hit damage.
- **Line numbers:** many code_refs are off (AbilityState, AbilityExecuteEventSystem, AddRageEffect, AbilityContext, builder lambdas at 1958-3214).

Missed formulas added:
- the DoubleLoot multiplier;
- the trigger-routing mask, which makes OnHit/OnCrit procs fire only from the owning weapon while OnPostHit is broadcast;
- TriggerAbilityEffect chaining;
- the item ability unlock gate (Evolved and UnlockLevel).

## 公式索引

| ID | 名称 | 状态 | 类型 |
|---|---|---|---|
| [`powerups.rage_extra_health`](#powerupsrage_extra_health) | Rage max-health bonus | 重建（无原生标注） | `runtime_formula` |
| [`powerups.rage_extra_hit`](#powerupsrage_extra_hit) | Rage-to-extra-attack conversion | 重建（无原生标注） | `runtime_formula` |
| [`powerups.hammer_regen`](#powerupshammer_regen) | Hammer regen (amulet keystone) | 空壳 | `runtime_formula` |
| [`powerups.amulet_souls_drop_unlock`](#powerupsamulet_souls_drop_unlock) | Soul drop unlock per spawner | 原生证据恢复 | `config_rule` |
| [`powerups.amulet_unlock_condition`](#powerupsamulet_unlock_condition) | Amulet unlock gate | 重建（无原生标注） | `config_rule` |
| [`powerups.amulet_fill_with_soul`](#powerupsamulet_fill_with_soul) | Amulet activation cost | 原生证据恢复 | `config_rule` |
| [`powerups.amulet_required_xp_per_level`](#powerupsamulet_required_xp_per_level) | Amulet XP required for level L | 原生证据恢复 | `lookup_table` |
| [`powerups.amulet_cumulative_xp`](#powerupsamulet_cumulative_xp) | Cumulative XP for amulet level | 原生证据恢复 | `runtime_formula` |
| [`powerups.amulet_level_from_xp`](#powerupsamulet_level_from_xp) | Amulet level from total XP | 原生证据恢复 | `runtime_formula` |
| [`powerups.amulet_upgrade_xp_gain`](#powerupsamulet_upgrade_xp_gain) | Amulet XP from paid upgrades | 原生证据恢复 | `runtime_formula` |
| [`powerups.amulet_sim_soul_xp`](#powerupsamulet_sim_soul_xp) | Amulet XP from simulated soul income | 原生证据恢复 | `simulation` |
| [`powerups.amulet_add_levels`](#powerupsamulet_add_levels) | Grant amulet levels | 原生证据恢复 | `runtime_formula` |
| [`powerups.amulet_upgrade_cost`](#powerupsamulet_upgrade_cost) | Amulet graph node cost per level | 原生证据恢复 | `runtime_formula` |
| [`powerups.amulet_upgrade_stat_value`](#powerupsamulet_upgrade_stat_value) | Amulet graph combat-stat modifier value | 原生证据恢复 | `runtime_formula` |
| [`powerups.amulet_stat_apply`](#powerupsamulet_stat_apply) | Apply amulet stat modifiers to player | 原生证据恢复 | `runtime_formula` |
| [`powerups.amulet_meta_modifiers`](#powerupsamulet_meta_modifiers) | Amulet graph meta-stat modifier values | 原生证据恢复 | `lookup_table` |
| [`powerups.amulet_node_availability`](#powerupsamulet_node_availability) | Amulet graph node availability and purchasability | 重建（无原生标注） | `config_rule` |
| [`powerups.amulet_autobalance_bake`](#powerupsamulet_autobalance_bake) | Amulet auto-balance bake (cost/stat multipliers) | 编辑器工具缺失 | `editor_generator` |
| [`powerups.rage_thresholds`](#powerupsrage_thresholds) | Rage level thresholds | 重建（无原生标注） | `lookup_table` |
| [`powerups.rage_gain`](#powerupsrage_gain) | Rage accumulation and level-up | 重建（无原生标注） | `runtime_formula` |
| [`powerups.rage_effective_max`](#powerupsrage_effective_max) | Rage max accessible level | 原生证据恢复 | `runtime_formula` |
| [`powerups.rage_decay`](#powerupsrage_decay) | Rage decay / reset | 重建（无原生标注） | `runtime_formula` |
| [`powerups.add_rage_effect`](#powerupsadd_rage_effect) | AddRageEffect proc | 重建（无原生标注） | `runtime_formula` |
| [`powerups.crystal_rage_marks_lightning`](#powerupscrystal_rage_marks_lightning) | Crystal legendary ability effects applied per frame | 原生证据恢复 | `runtime_formula` |
| [`powerups.ability_tier_override`](#powerupsability_tier_override) | Ability tier (star) override | 原生证据恢复 | `runtime_formula` |
| [`powerups.ability_effective_value`](#powerupsability_effective_value) | Effective ability value/chance/stack limit | 原生证据恢复 | `runtime_formula` |
| [`powerups.value_scaler`](#powerupsvalue_scaler) | Ability value scaling factor | 原生证据恢复 | `runtime_formula` |
| [`powerups.ability_boosts`](#powerupsability_boosts) | Ability-to-ability boosts | 原生证据恢复 | `runtime_formula` |
| [`powerups.economy_proc_bonus`](#powerupseconomy_proc_bonus) | Economy ability proc-chance bonus | 原生证据恢复 | `runtime_formula` |
| [`powerups.chance_condition`](#powerupschance_condition) | Ability proc chance | 原生证据恢复 | `runtime_formula` |
| [`powerups.ability_gating_conditions`](#powerupsability_gating_conditions) | Ability cooldown/hit-count/stack/threshold conditions | 原生证据恢复 | `config_rule` |
| [`powerups.add_stat_effect`](#powerupsadd_stat_effect) | AddStatEffect (equip / temporary / permanent stat) | 原生证据恢复 | `runtime_formula` |
| [`powerups.add_stat_passive`](#powerupsadd_stat_passive) | Passive stat modifiers (AddStatPassive / StatStack / BonusStatPerHitsTaken) | 原生证据恢复 | `runtime_formula` |
| [`powerups.weapon_stat_stack`](#powerupsweapon_stat_stack) | Weapon stat stack multiplier | 原生证据恢复 | `runtime_formula` |
| [`powerups.weapon_damage_multipliers`](#powerupsweapon_damage_multipliers) | Per-frame ability damage multipliers | 原生证据恢复 | `runtime_formula` |
| [`powerups.attack_speed_multipliers`](#powerupsattack_speed_multipliers) | Ability attack-speed multipliers | 原生证据恢复 | `runtime_formula` |
| [`powerups.crit_buffs`](#powerupscrit_buffs) | Ability crit / super-crit modifiers | 原生证据恢复 | `runtime_formula` |
| [`powerups.empower_hit`](#powerupsempower_hit) | Empowered / next-hit damage bonuses | 原生证据恢复 | `runtime_formula` |
| [`powerups.lightning_damage`](#powerupslightning_damage) | Lightning proc damage | 原生证据恢复 | `runtime_formula` |
| [`powerups.shock`](#powerupsshock) | Shock consumption damage bonus | 原生证据恢复 | `runtime_formula` |
| [`powerups.marks`](#powerupsmarks) | Mark stacks (cap, application, decay) | 原生证据恢复 | `runtime_formula` |
| [`powerups.heal_effects`](#powerupsheal_effects) | Ability heals and regen multiplier | 原生证据恢复 | `runtime_formula` |
| [`powerups.stun_bonus`](#powerupsstun_bonus) | Stun duration and bonus damage to stunned | 原生证据恢复 | `runtime_formula` |
| [`powerups.random_stat_per_distance`](#powerupsrandom_stat_per_distance) | Permanent random stat per distance travelled | 原生证据恢复 | `runtime_formula` |
| [`powerups.scaled_multiplier_effect`](#powerupsscaled_multiplier_effect) | ScaledMultiplierEffect | 空壳 | `runtime_formula` |
| [`powerups.double_loot`](#powerupsdouble_loot) | DoubleLoot ability multiplier on enemy stat rewards | 原生证据恢复 | `runtime_formula` |
| [`powerups.ability_trigger_routing`](#powerupsability_trigger_routing) | Ability trigger routing to owners (source-item mask) | 原生证据恢复 | `config_rule` |
| [`powerups.trigger_ability_chain`](#powerupstrigger_ability_chain) | TriggerAbilityEffect (direct chained execution) | 原生证据恢复 | `runtime_formula` |
| [`powerups.missing_health_scaling`](#powerupsmissing_health_scaling) | Damage and regen scaling with missing health | 原生证据恢复 | `runtime_formula` |
| [`powerups.empty_slot_and_stat_scaling`](#powerupsempty_slot_and_stat_scaling) | Empty-slot and stat-proportional damage / attack-speed effects | 原生证据恢复 | `runtime_formula` |
| [`powerups.temporary_weapon_buff`](#powerupstemporary_weapon_buff) | Temporary weapon buff (timed damage / attack-speed / crit) | 原生证据恢复 | `runtime_formula` |
| [`powerups.unused_effect_stubs`](#powerupsunused_effect_stubs) | GrantStatOnHit / TreatAsAllSlotsEmpty / ScaledMultiplier effect stubs | 空壳 | `runtime_formula` |
| [`powerups.amulet_level_target_days`](#powerupsamulet_level_target_days) | Amulet level target days and auto-balance multipliers (unconsumed) | 编辑器工具缺失 | `editor_generator` |

## 公式

### `powerups.rage_extra_health`

**Rage max-health bonus**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 合并自：`powerups.rage_extra_health`、`combat.rage_extra_health`

```text
Every frame (OnUpdate -> ForceApply), if playerModel.Unit != null, for each live player entity: L = RageModel.ActiveRageLevel (= EffectiveMaxLevel > 0 ? RageLevel : 0); v = MetaStat('rage_extra_health').Value; bonus = (L <= 0) ? 0 : (v > 0 ? v * L : 0). If |bonus - lastAppliedValue| >= 1e-4 (lastAppliedValue starts at -1): remove the existing Health modifier named 'graph_rage_extra_health'; if bonus > 0, add StatModifier{Name='graph_rage_extra_health', StatType=Health, ModifierType=PercentAdditive, Source=GraphUpgrade, Value=(float)bonus}; lastAppliedValue = bonus. The meta stat comes from amulet node graph_rage_extra_health_4_21: v(Lnode) = float32(Lnode * 0.05f), max node level 3.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `rage_extra_health (v)` | amulet meta stat: Health percent-additive bonus per rage level | Meta Stats Config key rage_extra_health, set by amulet node graph_rage_extra_health_4_21: Native_15b7198 = float32(0.05*Lnode), Lnode <= 3 (see powerups.amulet_meta_modifiers) |
| `L (ActiveRageLevel)` | active rage level | RageModel.ActiveRageLevel (Rage/RageModel.cs:124-131); see powerups.rage_effective_max |
| `lastAppliedValue` | last bonus written to the Health stat (change detector) | RageExtraHealthSystem field, initial -1.0 |

**输出**：Health (max HP) PercentAdditive modifier = rage_extra_health * ActiveRageLevel, i.e. the max-HP multiplier becomes (1 + other PercentAdditive + bonus)

**代码**：[Player/Systems/RageExtraHealthSystem.cs:30-70](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/RageExtraHealthSystem.cs#L30)；[Rage/RageModel.cs:124-131](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rage/RageModel.cs#L124)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:686-692](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L686)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:2384](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L2384)；[Core/SystemsInstaller.cs:80](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Core/SystemsInstaller.cs#L80)

**状态依据**：RageExtraHealthSystem (ForceApply, GetPercentBonus, ApplyModifier) has no native VA annotation. The only native annotations nearby are on the upgrade value lambda Native_15b7198, which feeds the meta stat and is not this method. The system is registered in Core/SystemsInstaller.cs:80.

**配置参数**：`Meta Stats Config: rage_extra_health`；`Amulet upgrade node graph_rage_extra_health_4_21 (MaxLevel 3, value 0.05*L)`

**使用方**：stats.raw_value_aggregation (Health stat)；player max HP

**备注**：At node level 3 the bonus is +0.15 per rage level: +45% Health PercentAdditive at rage 3, and +60% at rage 4 in the Berserker fight. The modifier value is cast to float. lastAppliedValue is one field shared across all entities the filter matches (in practice only the player). CurrentHealth is not rescaled when MaxHP changes (combat catalog note). When rage drops to 0 the modifier is removed and not re-added.

**依赖图节点**：输入 `value:player.meta_stat[rage_extra_health]`、`value:rage.active_level`；输出 `value:rage.extra_health_modifier`

### `powerups.rage_extra_hit`

**Rage-to-extra-attack conversion**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 合并自：`powerups.rage_extra_hit`、`combat.rage_extra_hit`
- 原生地址：`0x1440178 (PlayerAttackSystem.Update, ResetCooldown consumer 1), 0x1441034 (PlayerAttackSystem.Attack, ResetCooldown consumer 2); RageExtraHitSystem itself is unannotated`

```text
For each RageGainedEvent e in the batch (published via NextFrame by RageModel.TryAddRage): T = MetaStat('rage_extra_hit').Value (re-read per event); if !(T > 0 && e.Amount > 0) skip; A += e.Amount (A is a float accumulator); while A >= T: { A -= (float)T; best = equipped weapon with strictly greatest ItemModel.Damage (ties go to the first in EquippedWeapons); if best != null: best.ResetCooldown = true; fire AbilityTrigger.OnCooldownReset for best immediately (AbilityExecuteEventSystem.ProcessEventImmediate) }. The flag is consumed in PlayerAttackSystem by one of two paths. (1) Idle or recovering weapon, in Update: if weapon.ResetCooldown && !weapon.AttackInProgress && weapon.Cooldown <= Config.AnimationBackPoint * Config.AttackCooldown: ResetCooldown = false; AttackInProgress = true; Cooldown = GetAnimationCastPoint(weapon) * Config.AttackCooldown + 0.15. This starts a swing whose hit lands once Cooldown falls to the cast-point threshold. (2) Mid-swing weapon, in Attack() at the hit frame: when AttackInProgress && Cooldown <= GetAnimationCastPoint(weapon)*AttackCooldown + attackSpeed*dt, the hit fires; if ResetCooldown is set then: ResetCooldown = false; Cooldown = 0. On the next Attack() call (Cooldown <= 0) a new swing starts at once, skipping the post-hit recovery. T comes from node graph_rage_extra_hit_7_21: T(Lnode) = 180 - 30*Lnode (150/120/90).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `T` | rage points per free attack | Meta Stats Config key rage_extra_hit; node graph_rage_extra_hit_7_21 Native_15b863c = 180 - 30*L (L <= 3); see powerups.amulet_meta_modifiers |
| `a (e.Amount)` | rage gained in one event | RageGainedEvent emitted by RageModel.TryAddRage (Rage/RageModel.cs:157-162), e.g. from AddRageEffect.Amount |
| `A` | accumulated rage toward the next free attack | RageExtraHitSystem.accumulatedRage (float, never reset) |
| `ItemModel.Damage` | weapon damage used to pick the target weapon | PlayerModel.EquippedWeapons |
| `AnimationBackPoint, AttackCooldown, AnimationCastPoint` | weapon config timings that gate and shape the reset swing | ItemModel.Config; GetAnimationCastPoint (PlayerAttackSystem.cs:206-211, Native 0x1440fa0) |

**输出**：Extra weapon attacks on the highest-Damage weapon: either an immediate new swing (idle weapon) or no post-hit recovery (mid-swing weapon), plus OnCooldownReset ability triggers

**代码**：[Player/Systems/RageExtraHitSystem.cs:26-59](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/RageExtraHitSystem.cs#L26)；[Player/PlayerAttackSystem.cs:142-147](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L142)；[Player/PlayerAttackSystem.cs:175-181](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L175)；[Player/PlayerAttackSystem.cs:206-211](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L206)；[Player/PlayerAttackSystem.cs:224-228](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L224)；[Rage/RageModel.cs:157-162](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rage/RageModel.cs#L157)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:1698-1703](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L1698)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:3052](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L3052)；[Core/SystemsInstaller.cs:81](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Core/SystemsInstaller.cs#L81)

**状态依据**：RageExtraHitSystem.OnUpdate and ResetHighestDamageWeaponCooldown have no native VA annotation, so the accumulator and threshold loop are reconstructed. The ResetCooldown consumers are native-annotated: PlayerAttackSystem.Update 'Native ELF VA 0x1440178' and PlayerAttackSystem.Attack 'Native ELF VA 0x1441034'. Those annotations do not cover the accumulator.

**配置参数**：`Meta Stats Config: rage_extra_hit`；`Amulet upgrade node graph_rage_extra_hit_7_21 (MaxLevel 3, T = 180 - 30L)`；`Weapon config AnimationBackPoint / AttackCooldown / AnimationCastPoint`

**使用方**：combat.player_weapon_attack_cycle (PlayerAttackSystem ResetCooldown, Update and Attack paths)；Abilities: AbilityTrigger.OnCooldownReset

**备注**：The weapon is picked by Damage, not DPS. ResetCooldownEffect and TriggerHighestDamageWeaponEffect pick by DPS. The accumulator is not reset by rage decay. ResetCooldown is a bool, so several thresholds crossed before the weapon consumes the flag collapse into one extra attack, though each still fires OnCooldownReset. RageModel.TryAddRage emits RageGainedEvent whenever EffectiveMaxLevel > 0, even at max rage level where RagePoints are zeroed, so free attacks keep coming at max rage. The event is published with NextFrame, so the conversion lags the rage gain by one frame. The Update path adds a fixed 0.15 s (ResetCooldownPostCastPointDelay) before the hit. The Attack path sets Cooldown = 0 at the hit frame, which removes the remaining recovery portion of the cycle.

**依赖图节点**：输入 `value:player.meta_stat[rage_extra_hit]`、`value:rage.points_gained`、`value:rage.effective_max_level`、`value:player.equipped_items`、`value:weapon.item_damage`、`config:ItemConfig.AnimationBackPoint`、`config:ItemConfig.AttackCooldown`、`value:weapon.cast_point_effective`、`constant:PlayerAttackSystem.ResetCooldownPostCastPointDelay`、`value:weapon.attack_speed_multiplier`、`state:frame_delta_time`；输出 `value:weapon.cooldown_reset_flag`、`value:unit.ability_trigger_events`、`value:weapon.attacks_per_second`

**核验记录**（已写入上方字段）：

- 合并复核修正：The expression and code_refs cover only one of the two places where PlayerAttackSystem consumes ResetCooldown, the Update branch at PlayerAttackSystem.cs:142-147. They miss the second one in PlayerAttackSystem.Attack() at lines 177-181 (Native ELF VA 0x1441034). If the flag is set while the weapon is mid-swing (AttackInProgress = true), the Update branch cannot fire because it requires !AttackInProgress. Instead, at the hit frame Attack() sets ResetCooldown = false and Cooldown = 0, so the next swing starts on the next Attack() call. Rage gained during a swing is handled by exactly this path, and the merged entry drops it. The entry is also internally inconsistent: native_va names only a consumer method while status is reconstructed. I kept the consumer VAs but labelled both consumers explicitly. Everything else checked out against the code: the accumulator, the strict > tie-break, the float cast, the threshold node 180-30L at Native_15b863c, and SystemsInstaller:81.

### `powerups.hammer_regen`

**Hammer regen (amulet keystone)**

- 状态：**空壳**（`stub`）
- 类型：`runtime_formula`
- 合并自：`powerups.hammer_regen`、`combat.hammer_regen_stub`

```text
UNKNOWN (stub): OnAwake, OnUpdate and ApplyModifier have empty bodies, and GetFlatBonus(maxHealth) returns 0.0, so there is no runtime effect. Intended (inferred from the fields and signatures): player regen modifier 'graph_hammer_regen' = f(hammer_regen, maxHealth), likely flat HealthRegen = hammer_regen * maxHealth. The meta stat comes from node graph_hammer_regen_3_3: hammer_regen(L) = float32(L * 0.001f), L <= 3, display value*100 (%).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `hammer_regen` | amulet meta stat (keystone) | Meta Stats Config key hammer_regen; node graph_hammer_regen_3_3 Native_15b6be4 = float32(0.001*L), max level 3 |
| `maxHealth` | player max HP (GetFlatBonus argument) | runtime |

**输出**：none in the reconstruction (stub); intended: player HP regen bonus

**代码**：[Player/Systems/HammerRegenSystem.cs:10-43](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/HammerRegenSystem.cs#L10)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:392-397](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L392)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:2186-2192](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L2186)；[Core/SystemsInstaller.cs:82](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Core/SystemsInstaller.cs#L82)

**状态依据**：All HammerRegenSystem methods are empty or return 0.0 and carry no native annotation, yet the system is registered (Core/SystemsInstaller.cs:82) and amulet node graph_hammer_regen_3_3 grants hammer_regen. Fields (ModifierName 'graph_hammer_regen', unitStash, playerModel, metaStatsModel, lastAppliedValue) show the original applied a stat modifier.

**配置参数**：`Meta Stats Config: hammer_regen`；`Amulet upgrade node graph_hammer_regen_3_3 (MaxLevel 3; cost soul.divine = L for L<3, soul.chaotic = 1 at L>=3)`

**使用方**：combat.player_regen (intended)

**备注**：The GetFlatBonus(maxHealth) signature suggests regen scales with max health. The likely form is regen += hammer_regen * maxHealth (0.1%/0.2%/0.3% of max HP), possibly conditioned on a hammer (buckshot?) weapon being equipped. This is unconfirmed. Combat (regen) is a defensible alternative owner. The entry sits with the other amulet keystone systems.

**依赖图节点**：输入 `value:player.meta_stat[hammer_regen]`、`value:player.stat_raw[Health]`、`constant:HammerRegenSystem.flat_bonus_stub`；输出 `value:player.hammer_regen_bonus`

### `powerups.amulet_souls_drop_unlock`

**Soul drop unlock per spawner**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`config_rule`
- 合并自：`powerups.amulet_souls_drop_unlock`、`meta.soul_drop_unlock_gate`
- 原生地址：`0x15ba79c`

```text
IsSoulsDropUnlocked(cfg) = cfg.SoulAlwaysAvailable OR (cfg.IsStatic ? MetaStat('feature_unlock').GetValue('static_souls_drop') : MetaStat('souls_drop_unlock').GetValue(cfg.SoulReward)) > 0. The result is cached per EnemySpawnerConfig and invalidated when the MetaStat's ModifierVersion changes. The EnemySpawner overload forwards spawner.Config.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `SoulAlwaysAvailable` | spawner flag that bypasses the gate | EnemySpawnerConfig |
| `IsStatic` | static spawner (trees) flag choosing the feature_unlock stat | EnemySpawnerConfig |
| `SoulReward` | spawner's soul key, used as the souls_drop_unlock parameter | EnemySpawnerConfig.SoulReward |
| `souls_drop_unlock[key]` | param-filtered meta stat, set to 1 by amulet nodes graph_souls_drop_unlock_<type> | Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:16-19,114-117,183-186 |
| `feature_unlock['static_souls_drop']` | param-filtered meta stat, set to 1 by graph_souls_drop_unlock_trees_1_9 | Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:65-68 |

**输出**：bool: whether kills or devours of this spawner drop souls at all

**代码**：[Amulet/Upgrades/AmuletUpgradesModel.cs:137-151](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradesModel.cs#L137)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:16-19](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L16)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:65-68](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L65)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:114-117](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L114)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:183-186](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L183)；[Enemy/Commands/GrantSpawnerSoulRewardCommandHandler.cs:32](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerSoulRewardCommandHandler.cs#L32)

**状态依据**：IsSoulsDropUnlocked(EnemySpawnerConfig) carries 'Original ELF VA 0x15ba79c: cache is invalidated by the actual MetaStat modifier version.'

**配置参数**：`EnemySpawnerConfig.SoulAlwaysAvailable`；`EnemySpawnerConfig.IsStatic`；`EnemySpawnerConfig.SoulReward`；`Meta Stats Config: souls_drop_unlock (param = soul key)`；`Meta Stats Config: feature_unlock (param static_souls_drop)`；`Amulet nodes graph_souls_drop_unlock_common_1_1 / _uncommon_1_17 / _rare_2_1 / _trees_1_9`

**使用方**：meta.soul_grant_on_devour

**备注**：Unlock nodes: common (1_1, cost 1 huge, param 'common'), large (graph_souls_drop_unlock_uncommon_1_17, cost 2 huge, param 'large'), elite (graph_souls_drop_unlock_rare_2_1, cost 2 huge, param 'elite'), static/trees (1_9, cost 1 chaotic, feature_unlock 'static_souls_drop'). The code lives in Amulet/. It is also listed under meta because it gates the meta soul reward: GrantSpawnerSoulRewardCommandHandler returns early when the gate is false. Per-key values come from MetaStat.GetValue(param) (see meta.metastat_value).

**依赖图节点**：输入 `config:EnemySpawnerConfig.SoulAlwaysAvailable`、`config:EnemySpawnerConfig.IsStatic`、`config:EnemySpawnerConfig.SoulReward`、`value:player.meta_stat[souls_drop_unlock]`、`value:player.meta_stat[feature_unlock]`；输出 `value:spawner.souls_drop_unlocked`

### `powerups.amulet_unlock_condition`

**Amulet unlock gate**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`config_rule`
- 来源：子系统梳理
- 原生地址：`0x15b434c (Unlock)`

```text
CanUnlock = !IsUnlocked AND LastUnlockedLocation.Number >= UnlockLocationNumber; Unlock() just sets IsUnlocked=true (no cost)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `UnlockLocationNumber` | location number required | Amulet Config.UnlockLocationNumber (=8) |
| `LastUnlockedLocation.Number` | player progress | runtime PlayerModel |

**输出**：bool CanUnlock / IsUnlockOnboardingActive

**代码**：[Amulet/AmuletModel.cs:65](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L65)；[Amulet/AmuletModel.cs:75](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L75)；[Amulet/AmuletModel.cs:160-164](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L160)

**状态依据**：Unlock has native VA comment, but the CanUnlock getter expression has no annotation

**配置参数**：`Amulet Config.UnlockLocationNumber`；`Amulet Config.FeatureUnlockDay`

**使用方**：UI onboarding；AmuletModel.TryFillWithSoul

**备注**：FeatureUnlockDay=16 has no runtime consumer. It looks like a simulation/auto-balance input.

**依赖图节点**：输入 `config:AmuletConfig.UnlockLocationNumber`、`value:player.unlocked_location`、`value:amulet.is_unlocked`；输出 `value:amulet.can_unlock`

### `powerups.amulet_fill_with_soul`

**Amulet activation cost**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`config_rule`
- 来源：合并核验
- 原生地址：`0x15b4374, 0x15b434c`

```text
TryFillWithSoul(): if EmptyAmuletCount <= 0 (currency 'empty_amulet') OR !HasEnough('soul.huge', 1) return false; currencyModel.TrySpend('empty_amulet', 1) (return value ignored); Unlock() (IsUnlocked = true if not already); return true. The huge soul is checked but NOT spent.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `empty_amulet` | empty amulet currency count | CurrencyModel 'empty_amulet' (AmuletModel.EmptyAmuletCount, AmuletModel.cs:79) |
| `soul.huge` | huge (epic) soul count; must be >= 1 but is not consumed | CurrencyModel 'soul.huge' |

**输出**：amulet IsUnlocked = true; one empty_amulet spent (if TrySpend succeeds)

**代码**：[Amulet/AmuletModel.cs:160-173](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L160)；[Amulet/AmuletModel.cs:79](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L79)；[Amulet/AmuletModel.cs:200-203](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L200)；[UI/Inventory/InventoryView.cs:293](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/Inventory/InventoryView.cs#L293)

**状态依据**：TryFillWithSoul carries 'Native ELF VA 0x15b4374: huge soul is checked; only one empty_amulet is spent.' Unlock carries 'Native ELF VA 0x15b434c'.

**使用方**：Amulet UI flow (InventoryView.cs:293)；powerups.amulet_upgrade_xp_gain (IsUnlocked gate)；powerups.amulet_add_levels (IsUnlocked gate)

**备注**：The gate is EmptyAmuletCount > 0, not >= 1. With a fractional count in (0,1), TrySpend(1) would presumably fail, but its result is ignored, so the amulet still unlocks. In practice the count is integral.

**依赖图节点**：输入 `value:economy.currency_balance`、`constant:AmuletModel.fill_required_huge_souls`、`constant:AmuletModel.fill_empty_amulet_cost`、`state:player_actions`；输出 `value:amulet.is_unlocked`、`value:economy.currency_balance`

**核验记录**（已写入上方字段）：

- 采纳 `expression`（来自 `powerups.amulet_fill_with_soul`）：AmuletModel.cs:169-171 gates on EmptyAmuletCount <= 0.0 || !HasEnoughSouls("huge",1.0), then calls TrySpend("empty_amulet",1.0) without checking its result, then Unlock(). The expression now uses > 0, the ignored TrySpend return and Unlock().

### `powerups.amulet_required_xp_per_level`

**Amulet XP required for level L**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`lookup_table`
- 来源：子系统梳理
- 原生地址：`0x15b1318`

```text
req(L) = RequiredExperiencePerLevel[L-2] if 2 <= L <= MaxLevel else 0
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `L` | amulet level | runtime |
| `RequiredExperiencePerLevel` | per-level XP table | Amulet Config.RequiredExperiencePerLevel = [17000,65000,150000,700000,1700000,2800000,5000000] |
| `MaxLevel` | max amulet level | Amulet Config.MaxLevel (=7) |

**输出**：XP needed to go from L-1 to L

**代码**：[Amulet/AmuletConfig.cs:47-51](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletConfig.cs#L47)

**状态依据**：Comment 'Native ELF VA 0x15b1318 uses level - 2 and returns zero outside [2,MaxLevel]'

**配置参数**：`Amulet Config.RequiredExperiencePerLevel`；`Amulet Config.MaxLevel`

**使用方**：powerups.amulet_cumulative_xp；powerups.amulet_level_from_xp；AmuletModel.NextLevelExperience

**备注**：With MaxLevel=7 only indices 0..5 are read. The 7th entry (5,000,000) is dead data unless MaxLevel is raised.

**依赖图节点**：输入 `config:AmuletConfig.RequiredExperiencePerLevel`、`config:AmuletConfig.MaxLevel`；输出 `value:amulet.required_xp_per_level`

### `powerups.amulet_cumulative_xp`

**Cumulative XP for amulet level**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15b3c30`

```text
XPcum(L) = sum_{k=1..L} req(k)  => L2=17000, L3=82000, L4=232000, L5=932000, L6=2632000, L7=5432000
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `req(k)` | per-level requirement | powerups.amulet_required_xp_per_level |

**输出**：total XP at which level L starts

**代码**：[Amulet/AmuletModel.cs:245-251](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L245)

**状态依据**：Comment 'Native ELF VA 0x15b3c30; no additional clamp'

**配置参数**：`Amulet Config.RequiredExperiencePerLevel`

**使用方**：powerups.amulet_level_from_xp；AmuletModel.AddLevels；AmuletModel.CurrentLevelExperience

**依赖图节点**：输入 `value:amulet.required_xp_per_level`；输出 `value:amulet.cumulative_xp_threshold`

### `powerups.amulet_level_from_xp`

**Amulet level from total XP**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x15b4960, 0x15b3c30, 0x15b1318`

```text
req(l) = (1 < l <= MaxLevel) ? RequiredExperiencePerLevel[l-2] : 0; XPcum(L) = sum_{l=1..L} req(l). Level = GetLevelForExperience(XP): 0 if !(XP >= 0) (negative or NaN); 1 if MaxLevel < 1; else walk l = 1..MaxLevel adding req(l) and return the previous level at the first l with XP < XPcum(l); MaxLevel if none (= max L in [1,MaxLevel] with XP >= XPcum(L) for non-negative req). CurrentLevelExperience = XP - XPcum(Level); NextLevelExperience = req(Level+1); IsMaxLevel = Level >= MaxLevel; LevelProgress = IsMaxLevel ? 1 : (req(Level+1) > 0 ? (float)((XP - XPcum(Level)) / req(Level+1)) : 0).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `XP` | AmuletUd.TotalExperience | runtime save |
| `req(l)` | XP required to go from level l-1 to l (0 for l <= 1 or l > MaxLevel) | Amulet Config.RequiredExperiencePerLevel via AmuletConfig.GetRequiredExperience; see powerups.amulet_required_xp_per_level |
| `XPcum(L)` | cumulative XP threshold for level L | AmuletModel.GetExperienceForLevel; see powerups.amulet_cumulative_xp |
| `MaxLevel` | amulet max level | Amulet Config.MaxLevel |

**输出**：amulet Level (gates graph nodes via RequiredAmuletLevel), CurrentLevelExperience, NextLevelExperience, LevelProgress, IsMaxLevel

**代码**：[Amulet/AmuletModel.cs:219-233](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L219)；[Amulet/AmuletModel.cs:240-251](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L240)；[Amulet/AmuletModel.cs:101-119](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L101)；[Amulet/AmuletModel.cs:133-151](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L133)；[Amulet/AmuletConfig.cs:47-51](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletConfig.cs#L47)

**状态依据**：GetLevelForExperience carries 'Native ELF VA 0x15b4960'. GetExperienceForLevel carries 'Native ELF VA 0x15b3c30'. AmuletConfig.GetRequiredExperience carries 'Native ELF VA 0x15b1318'. The progress getters (CurrentLevelExperience, NextLevelExperience, LevelProgress, IsMaxLevel) are unannotated but trivial.

**配置参数**：`Amulet Config.RequiredExperiencePerLevel`；`Amulet Config.MaxLevel`

**使用方**：powerups.amulet_node_availability；powerups.amulet_add_levels；AmuletLevelUpSystem analytics；Amulet UI progress bar

**备注**：Because req(1) = 0, XP = 0 gives Level 1, not 0. Level 0 occurs only for negative or NaN XP. LevelProgress is returned as float.

**依赖图节点**：输入 `value:amulet.total_xp`、`value:amulet.cumulative_xp_threshold`、`value:amulet.required_xp_per_level`、`config:AmuletConfig.MaxLevel`；输出 `value:amulet.level`

**核验记录**（已写入上方字段）：

- 采纳 `expression`（来自 `powerups.amulet_level_from_xp`）：AmuletModel.cs:139: IsMaxLevel ? 1f : NextLevelExperience > 0.0 ? (float)(CurrentLevelExperience / NextLevelExperience) : 0f. The expression now includes the req(Level+1) > 0 guard returning 0.

### `powerups.amulet_upgrade_xp_gain`

**Amulet XP from paid upgrades**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x15bae84, 0x15b46a4, 0x15b2518, 0x15b26e8`

```text
TryUpgrade(key, forFree): if !IsAvailable(key) return false; soulCost = forFree ? null : GetNextSoulCost(key), the first entry of the pre-upgrade GetNextCost(key) with non-empty CurrencyKey starting with 'soul.' and Amount > 0; if Upgrades == null or !Upgrades.TryUpgrade(key, forFree) return false; if soulCost: AddUpgradeExperience(s = CurrencyKey minus 'soul.', amt) does: if IsUnlocked && amt > 0: XP += GetBaseExperience(s) * amt, where GetBaseExperience(s) = (first SoulDef with Key == s)?.BaseExperience ?? 1.0. Free upgrades give 0 XP. IsAvailable does not check AmuletModel.IsUnlocked; only the XP credit does. Preview: GetNextUpgradeExperience(key) = soulCost ? GetBaseExperience(s) * amt : 0.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `BaseExperience[s]` | XP per soul of type s | Amulet Config.SoulDefs: common=1, large=3, elite=80, huge=500, great=2000, divine=10000, chaotic=10000 (1.0 if key missing) |
| `soulCost_s (amt)` | evaluated soul cost of the level being bought (first soul.* entry with amount > 0) | powerups.amulet_upgrade_cost |
| `IsUnlocked` | amulet activated (filled with soul) | powerups.amulet_fill_with_soul |

**输出**：TotalExperience increment

**代码**：[Amulet/Upgrades/AmuletUpgradesModel.cs:194-203](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradesModel.cs#L194)；[Amulet/Upgrades/AmuletUpgradesModel.cs:210-223](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradesModel.cs#L210)；[Amulet/Upgrades/AmuletUpgradesModel.cs:174-181](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradesModel.cs#L174)；[Amulet/AmuletModel.cs:189-193](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L189)；[Amulet/AmuletConfig.cs:57-68](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletConfig.cs#L57)

**状态依据**：Native VA comments on TryUpgrade (0x15bae84), AddUpgradeExperience (0x15b46a4), GetSoulDef (0x15b2518) and GetBaseExperience (0x15b26e8). GetNextSoulCost and GetNextUpgradeExperience are unannotated.

**配置参数**：`Amulet Config.SoulDefs[].BaseExperience`；`Amulet Upgrade Config.Upgrades[].RequiredAmuletLevel`

**使用方**：powerups.amulet_level_from_xp

**备注**：Only the first soul cost entry counts. On dual-cost keystone nodes (divine for L<3, chaotic at L3) the zero-amount entry is skipped. Paying the full tree (see amulet_upgrade_cost) yields about 41.6M XP against 5.43M needed for L7, so most XP comes from early cheap nodes. TryUpgrade never checks AmuletModel.IsUnlocked. Since GetLevelForExperience(0) = 1, the 27 nodes with RequiredAmuletLevel = 1 in Amulet Upgrade Config.asset can be bought, and their stats applied, before the amulet is unlocked. Those purchases give 0 XP because AddUpgradeExperience requires IsUnlocked.

**依赖图节点**：输入 `config:AmuletConfig.SoulDefs[].BaseExperience`、`constant:AmuletConfig.default_base_experience`、`value:economy.upgrade_cost`、`value:amulet.is_unlocked`、`value:amulet.node_available`、`value:economy.currency_balance`、`state:player_actions`；输出 `value:amulet.total_xp`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `powerups.amulet_upgrade_xp_gain`）：Verified: IsAvailable (AmuletUpgradesModel.cs:174-181) and TryUpgrade (194-203) never test IsUnlocked; AddUpgradeExperience (AmuletModel.cs:192) does. GetLevelForExperience(0) returns 1 because req(1) = 0. Amulet Upgrade Config.asset has exactly 27 'RequiredAmuletLevel: 1' nodes. Added to notes and expression.

### `powerups.amulet_sim_soul_xp`

**Amulet XP from simulated soul income**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`simulation`
- 来源：合并核验
- 原生地址：`0x15b4514`

```text
AddSouls(s, a, isSimulation = false): if a <= 0 return; currency 'soul.'+s += a; add s to DiscoveredSoulKeys if new; if isSimulation: XP += (GetSoulDef(s)?.BaseExperience ?? 1.0) * a. The simulation branch has no IsUnlocked check. Non-simulation AddSouls adds no XP.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `a` | souls granted | caller |
| `BaseExperience[s]` | XP per soul of type s | Amulet Config.SoulDefs (1.0 if missing) |
| `isSimulation` | credit XP on gain instead of on spend | caller flag (no in-build caller passes true) |

**输出**：TotalExperience increment in simulation (soul currency increment always)

**代码**：[Amulet/AmuletModel.cs:180-187](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L180)；[Amulet/AmuletConfig.cs:57-62](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletConfig.cs#L57)

**状态依据**：AddSouls carries 'Original ELF VA 0x15b4514'.

**配置参数**：`Amulet Config.SoulDefs[].BaseExperience`

**使用方**：No in-build caller of the isSimulation=true branch; probably the stripped editor simulation/forecast. Live callers (isSimulation false/omitted): [Rewards/Commands/GrantRewardCommandRunner.cs:154](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/Commands/GrantRewardCommandRunner.cs#L154),172, [Shop/ShopLotModel.cs:246](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ShopLotModel.cs#L246), [Enemy/Commands/GrantSpawnerSoulRewardCommandHandler.cs:37](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerSoulRewardCommandHandler.cs#L37)

**备注**：The isSimulation branch credits XP on soul gain, while the live path credits it on spend (powerups.amulet_upgrade_xp_gain). No in-build simulation calls this branch, so any divergence comes only from the missing editor-side forecast. The divergence is unconfirmed at runtime.

**依赖图节点**：输入 `value:reward.souls_granted`、`config:AmuletConfig.SoulDefs[].BaseExperience`、`constant:AmuletModel.default_base_experience`、`state:sim_bot_state`；输出 `value:amulet.total_xp`、`value:economy.currency_balance`

**核验记录**（已写入上方字段）：

- 采纳 `used_by`（来自 `powerups.amulet_sim_soul_xp`）：grep for AddSouls finds callers only at GrantRewardCommandRunner.cs:154,172 (explicit false), ShopLotModel.cs:246 and GrantSpawnerSoulRewardCommandHandler.cs:37 (default false). None pass true. used_by now says so.
- 采纳 `notes`（来自 `powerups.amulet_sim_soul_xp`）：With no in-build caller of the true branch, the claim that forecasts run ahead of real amulet level is unsupported. Notes reworded to say the divergence is limited to the missing editor forecast and is unconfirmed.

### `powerups.amulet_add_levels`

**Grant amulet levels**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x15b4884`

```text
AddLevels(n): if n < 1 OR !IsUnlocked OR IsMaxLevel return; next = min(Level + n, MaxLevel); if next <= Level return; XP = XPcum(next) (GetExperienceForLevel(next)).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `n (amount)` | levels to grant | caller (none in the reconstruction) |
| `XPcum` | cumulative XP threshold | powerups.amulet_cumulative_xp / powerups.amulet_level_from_xp |

**输出**：TotalExperience set to the cumulative threshold of the new level

**代码**：[Amulet/AmuletModel.cs:210-217](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L210)；[Amulet/AmuletModel.cs:245-251](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L245)

**状态依据**：AddLevels carries 'Native ELF VA 0x15b4884, including native clamp and cumulative experience reset'.

**配置参数**：`Amulet Config.MaxLevel`；`Amulet Config.RequiredExperiencePerLevel`

**使用方**：No caller in the reconstruction (dead or cheat/debug path)

**备注**：Partial progress inside the current level is discarded.

**依赖图节点**：输入 `value:amulet.level`、`value:amulet.is_unlocked`、`config:AmuletConfig.MaxLevel`、`value:amulet.cumulative_xp_threshold`；输出 `value:amulet.total_xp`

**核验记录**（已写入上方字段）：

- 采纳 `used_by`（来自 `powerups.amulet_add_levels`）：grep 'AddLevels(' over Assets/Scripts finds only the definition at [Amulet/AmuletModel.cs:210](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L210). used_by now records no caller.

### `powerups.amulet_upgrade_cost`

**Amulet graph node cost per level**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`Builder constructor calls 0x1582d04..; 628 native lambdas 0x15b63f4..; Populate 0x15b9bd0; PrettyRound 0x15c4c28; UpgradeModel class range 0x148a848..0x148d2d4`

```text
For node key, currency cur and level being bought L = GetLevel(key)+1 (no cost when L > MaxLevel): cost_{key,cur}(L) = PrettyRound( (double)PrettyRound(c_{key,cur}(L)) * (double)float32(CM[key]) ), where CM[key] = Amulet Config.UpgradeCostMultipliers[key] (default 1f). The lambda is rounded first, then scaled, then rounded again. PrettyRound(n) (roundingDigits=2, boundaries 1/4): if n < 10 return (int)Math.Round(n) (banker's rounding); else s = 10^((int)log10(n) - 1), d = (int)(n / (10s)), step = 10s, step = 5s if d <= 4, step = s if d <= 1; return (int)(Math.Round(n/step)*step) (banker's). Because the inner PrettyRound returns an int, non-integer shapes become integers before scaling (L^1.5 at L=2: 2.83 -> 3 -> 3*CM). c(L) forms used: L, L^1.2, L^1.3, L^1.5, L^2, L^3, L^5, 2L, 5L, 10L, 15L, 5L+10, 6L, 50L, 100L, 300L, 90L-80, 10L^2, 50L^2, 100L^2, and constants (1,2,3,4,5,6,10,20,30,40). Keystones (MaxLevel 3) charge divine = (L < 3 ? L : 0) and chaotic = (L < 3 ? 0 : 1). CanAfford requires HasEnough for every cost with a non-empty currency; TryUpgrade spends only costs with Amount > 0.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `L` | level being bought (current level + 1) | runtime, UpgradeModel.GetNextCost |
| `c(L)` | native per-level cost lambda | AmuletUpgradeDefsBuilder Native_* delegates (WithCost) |
| `CM[key]` | baked cost multiplier (float32) | Amulet Config.UpgradeCostMultipliers (164 keys, e.g. graph_health_flat_1_2=29.37, graph_health_flat_7_24=14249.86) |
| `PrettyRound` | 2-significant-digit rounding to steps of s, 5s or 10s | Utilities/DoubleExtensions.cs PrettyRound |

**输出**：list of UpgradeCostInstance (currency 'soul.<key>', int amount) for the next level

**代码**：[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:12-1951](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L12)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:1958-3214](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L1958)；[Amulet/Upgrades/AmuletUpgradesModel.cs:81-94](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradesModel.cs#L81)；[Meta/Upgrades/UpgradeCostDef.cs:17-20](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeCostDef.cs#L17)；[Meta/Upgrades/UpgradeDef.cs:78-82](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeDef.cs#L78)；[Meta/Upgrades/UpgradeDef.cs:159-168](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeDef.cs#L159)；[Meta/Upgrades/UpgradeDef.cs:214-220](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeDef.cs#L214)；[Meta/Upgrades/UpgradeModel.cs:62-78](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeModel.cs#L62)；[Utilities/DoubleExtensions.cs:73-83](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Utilities/DoubleExtensions.cs#L73)

**状态依据**：The builder header says it was generated from original ARM64 constants and 628 scalar delegates, and each lambda carries an 'Original ELF VA' comment. Populate (0x15b9bd0) and PrettyRound (0x15c4c28) carry native VA comments. UpgradeModel has a class header citing the original ARM64 range. UpgradeCostDef.Evaluate and UpgradeDef.EvaluateCosts have no per-method annotation.

**配置参数**：`Amulet Config.UpgradeCostMultipliers`

**使用方**：powerups.amulet_upgrade_xp_gain；powerups.amulet_node_availability (CanAfford)；Meta/Upgrades/UpgradeModel.TryUpgrade

**备注**：Full-tree totals with current data, recomputed with the double PrettyRound: common 12,143,385; large 3,284,760; elite 84,435; huge 22,058; great 616; divine 39; chaotic 19. Full-tree XP is 41,593,465. The single-round formula would give common 12,142,485, elite 84,635 and XP 41,608,565, so only common and elite differ. The CM values are baked auto-balance output (see powerups.amulet_autobalance_bake); the lambdas are the shape. CM is stored as float, so the multiplication uses the float32 value widened to double.

**依赖图节点**：输入 `value:player.upgrade_level`、`config:AmuletConfig.UpgradeCostMultipliers.valueData`、`constant:AmuletUpgradeDefsBuilder.cost_lambdas`、`constant:AmuletUpgradeDefsBuilder.node_max_level`、`constant:DoubleExtensions.pretty_round_digits`；输出 `value:economy.upgrade_cost`

**核验记录**（已写入上方字段）：

- 采纳 `expression`（来自 `powerups.amulet_upgrade_cost`）：Confirmed in code: UpgradeCostDef.Evaluate (line 19) returns AmountByLevel(level).PrettyRound() as int, and UpgradeDef.EvaluateCosts (line 218) computes (cost.Evaluate(level) * CostMultiplier).PrettyRound() with a float CostMultiplier. The expression now has the double PrettyRound and float32(CM).
- 采纳 `notes`（来自 `powerups.amulet_upgrade_cost`）：Re-ran the evaluation script against the builder and Amulet Config.asset. Double-round totals: common 12,143,385, large 3,284,760, elite 84,435, huge 22,058, great 616, divine 39, chaotic 19, XP 41,593,465. Single-round totals: common 12,142,485, elite 84,635. The notes now use the double-round numbers.
- 采纳 `code_refs`（来自 `powerups.amulet_upgrade_cost`）：Build() returns at line 1950 and closes at 1951. Native_15b63f4 starts at line 1958 and the file has 3214 lines. UpgradeCostDef.cs:17-20 is Evaluate.

### `powerups.amulet_upgrade_stat_value`

**Amulet graph combat-stat modifier value**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`Builder lambdas 0x15b63f4..; Populate 0x15b9bd0; UpgradeModel class range 0x148a848..0x148d2d4`

```text
v_{key}(L) = (float)( f_key(L) * (double)float32(SM[key]) ), where SM[key] = Amulet Config.StatModifierMultipliers[key] (ValueMultiplier, default 1f), set on every stat modifier of the node by WithStatModifierMultiplier. It is emitted by CollectStatModifiers as StatModifier(StatType, ModifierType, Value=v, Param, StringParam, Source=GraphUpgrade) for every node with level L >= 1, at the CURRENT level only (not cumulative over levels). f families: flat Health/HealthRegen/Stab/Cut/Buckshot damage and defence/LifestealFlat = L (ModifierType Flat=0). These are the only nodes with SM, and the 57 SM values range from 74.1 to 608735.6. %damage/%defence/Health% = 0.01L (PreAdditivePostMultiplicative=4); HealthRegen% and Lifesteal% = 0.02L (type 4, SM=1); MovementSpeed = 0.03L (PostFormulaMultiplicative=5); Evasion, CritChance, BlockChance, BlockAmount = 0.01L (PostFormulaFlat=6); CritDamage = 0.1L (6); SuperCritChance = 0.05L (6); SuperCritDamage = 0.2L (6).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `L` | node level 1..MaxLevel (1..25) | AmuletUd.UpgradeLevels |
| `f_key(L)` | native per-level stat lambda | AmuletUpgradeDefsBuilder Native_* delegates (WithStatModifier) |
| `SM[key]` | baked stat multiplier (float32) | Amulet Config.StatModifierMultipliers (57 keys, flat-stat nodes only, values 74.1..608735.6) |

**输出**：StatModifier on PlayerUnit stats (Source=GraphUpgrade)

**代码**：[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:12-1951](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L12)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:1958-3214](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L1958)；[Amulet/Upgrades/AmuletUpgradesModel.cs:81-94](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradesModel.cs#L81)；[Meta/Upgrades/UpgradeDef.cs:84-89](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeDef.cs#L84)；[Meta/Upgrades/UpgradeDef.cs:181-190](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeDef.cs#L181)；[Meta/Upgrades/UpgradeStatModifierDef.cs:15-28](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeStatModifierDef.cs#L15)；[Meta/Upgrades/UpgradeModel.cs:136-146](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeModel.cs#L136)

**状态依据**：The builder was generated from native delegates, with a per-lambda 'Original ELF VA' comment. Populate carries native VA 0x15b9bd0. UpgradeModel has a class header citing the original ARM64 range. UpgradeStatModifierDef.Evaluate has no per-method annotation.

**配置参数**：`Amulet Config.StatModifierMultipliers`

**使用方**：powerups.amulet_stat_apply；[Unit/Stat.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs) aggregation；simulation

**备注**：Full-tree totals (sum of f(max)*SM, verified by script): Health flat 13,590,641; Health% 0.8; HealthRegen flat 81,482 and % 1.2; Stab (pierce) damage flat 599,434 / % 0.8; Cut damage flat 331,882 / % 0.8; Buckshot (smash) damage flat 493,089 / % 0.6; Stab defence flat 366,721 / % 0.6; Cut defence flat 704,288 / % 0.7; Buckshot defence flat 466,991 / % 0.8; Lifesteal flat 79,376 / % 1.6; MoveSpeed post-mult 0.18; Evasion/Crit/Block chance/Block amount +0.06 post-flat each; CritDmg +1.0; SuperCrit chance +0.1; SuperCrit dmg +0.4. Key naming: pierce=Stab (StatType 6/7/14/15), smash=Buckshot (12/13/20/21). Every StatModifierMultipliers key belongs to a node whose stat modifiers are all Flat, and every node with a Flat stat modifier has an SM key. The smallest SM is 74.1. The 0.86 values are UpgradeCostMultipliers entries, not SM.

**依赖图节点**：输入 `value:player.upgrade_level`、`config:AmuletConfig.StatModifierMultipliers.valueData`、`constant:AmuletUpgradeDefsBuilder.stat_lambdas`；输出 `value:player.upgrade_stat_modifier`

**核验记录**（已写入上方字段）：

- 采纳 `code_refs`（来自 `powerups.amulet_upgrade_stat_value`）：The definitions are at builder lines 12-1951 and the lambdas at 1958-3214. UpgradeStatModifierDef.Evaluate (lines 25-28) returns ValueByLevel(level) * ValueMultiplier, with ValueMultiplier defaulting to 1f (line 17). Re-ran the totals script and they match. code_refs and expression are updated.
- 合并复核修正：The expression gives the StatModifierMultipliers range as 0.86..608735, carried over from the member, and that is wrong. I parsed Amulet Config.asset: the 57 StatModifierMultipliers values run from 74.1 to 608735.6. The 0.86250633 values are in UpgradeCostMultipliers (asset lines 222-225), not StatModifierMultipliers. I re-ran the script on the rest. All 57 SM keys belong to nodes whose stat modifiers are all Flat, and every node with a Flat stat modifier has an SM key. All full-tree totals match.

### `powerups.amulet_stat_apply`

**Apply amulet stat modifiers to player**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15bb7d4, 0x15bbb28, 0x15bb904`

```text
whenever upgradesModel.Version or the PlayerUnit changes: remove previous modifiers; for each bought node add every v_{key}(L) to the matching stat; if the unit changed: CurrentHealth = Health(max)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Version` | UpgradeModel change counter | runtime |

**输出**：player Stat modifier set

**代码**：[Amulet/Upgrades/UpgradeStatApplySystem.cs:29-66](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/UpgradeStatApplySystem.cs#L29)

**状态依据**：Native VA comments on OnUpdate, ApplyAll, ClearApplied

**使用方**：combat damage/HP ([Unit/Stat.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs))

**备注**：Meta-key (non-StatType) modifiers are applied instead to MetaStats by UpgradeModel.ApplyAll (Meta/Upgrades/UpgradeModel.cs:120-135).

**依赖图节点**：输入 `value:player.upgrade_stat_modifier`、`value:player.stat_raw[Health]`；输出 `value:player.stat_modifiers`、`value:player.current_hp`

### `powerups.amulet_meta_modifiers`

**Amulet graph meta-stat modifier values**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`lookup_table`
- 来源：合并核验
- 原生地址：`Builder lambdas 0x15b63f4.., e.g. crystal_timer_on_kill_reduce Native_15b8840 is ((L*64 - L*4)*715827883)>>33 = 5L; UpgradeModel class range 0x148a848..0x148d2d4`

```text
For every node with level L >= 1, UpgradeModel.ApplyAll adds to MetaStat[key] a StatModifier(ModifierType, Source=GraphUpgrade, Value=(float)f(L), Param/StringParam) at the CURRENT level; all modifiers are removed and re-added on each upgrade. Meta modifiers have no value multiplier (UpgradeModifierDef.Evaluate = ValueByLevel(L)). ModifierType is Flat (0) unless noted. f32(c*L) below means the lambda computes in float32. Combat keystones (MaxLevel 3): rage_extra_health = f32(0.05*L); rage_damage_reduce = 0.05L; rage_extra_hit = 180-30L (150/120/90); mark_lifesteal = f32(0.05*L); extra_mark = f32(0.1*L); empowered_lightning = 0.1L; lightning_damage = f32(L/2); block_to_supercrit = 0.03L; evasion_mark = L; hammer_regen = f32(0.001*L). Economy and progression: economy_item_abilities_chance = 0.01L (nodes 3_13, 5_40) or 0.02L (7_28), max 2; souls_rarity_amount[soul] = L (node 2_15 common = 2L); souls_drop_unlock[common/large/elite] = 1; feature_unlock[static_souls_drop, crystals_sell_all, crystal_slot_3, crystals_open_all, crystals_filter] = 1; loadout_button = 1; stat_drop[island 1..14] = f32(0.2*L) (max 5, so +1.0); node 4_47 sets event_golem_extra_range = 1 and stat_drop[param -1] = 1; map_spawner_timer_reduce[island] = 7200 (s); weak_spawner_extra_range[island] = L (max 1); items_rarity_drop_amount[r] = L; items_rarity_charges[r] = L; items_rarity_timer[r] = 2L-1; chest_level = L (7 nodes, max 1); free_chest['chest_1']: weekly node 3_14 = 7, daily node 3_15 = 0 (constant 0.0); tower_damage = 0.05L; tower_damage_reduction = 0.05L; tower_boss_weakness = f32(0.15*L) (max 1); boss_tower_tickets = L; boss_tower_unlock[berserker/vampire_king/lich] = 1; tower_drop_legend_per_floor[boss] = L (max 1; nodes 5_58 hydra, 5_59 lava_crab, 5_60 vikings, 7_39 berserker, 7_56 vampire_king, 7_57 lich), and node 7_58 sets all six to 1; boss_tower_items_drop = 0.1L; boss_tower_vikings = 0.7L; boss_tower_lich = f32(0.1*L); boss_tower_vampire = 0.05L; boss_tower_berserker = 0.05L; boss_tower_crab = 2L; boss_tower_hydra = L; event_golem_damage = 0.5 constant (nodes 4_48 and 6_48). Crystals: crystal_sell_price_bonus = 0.05L; crystal_enchant_efficiency = f32(0.01*L) (ModifierType 4, max 5); crystal_timer_on_kill_reduce = 5L (max 1); crystal_guarantee_enchant = 100; crystal_enchant_extra_attempts[1/2/3] = L; crystal_blacksmithing, crystal_enchantment, crystal_max_enchant_stones, crystal_max_geodes, crystal_geode_timer, crystal_chest_ench_level = L.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `L` | node level | AmuletUd.UpgradeLevels |
| `f(L)` | native per-level meta lambda or constant | AmuletUpgradeDefsBuilder WithModifier arguments / Native_* delegates |

**输出**：MetaStat values read by other subsystems

**代码**：[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:12-1951](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L12)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:396](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L396)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:464-476](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L464)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:690](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L690)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:860-861](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L860)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:914](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L914)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:1702](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L1702)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:1811](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L1811)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:1940-1948](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L1940)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:1958-3214](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L1958)；[Meta/Upgrades/UpgradeDef.cs:170-201](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeDef.cs#L170)；[Meta/Upgrades/UpgradeModifierDef.cs:24-27](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeModifierDef.cs#L24)；[Meta/Upgrades/UpgradeModel.cs:121-135](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeModel.cs#L121)

**状态依据**：Generated from native lambdas and constants, each lambda annotated 'Original ELF VA'. UpgradeModel.ApplyAll falls inside the class-level native range.

**配置参数**：`Amulet Config.UpgradeCostMultipliers (node cost only; does not scale meta values)`

**使用方**：powerups.rage_extra_health；combat.rage_damage_reduction；powerups.rage_extra_hit；powerups.marks；powerups.lightning_damage；powerups.economy_proc_bonus；[Abilities/Systems/AbilityExecuteEventSystem.cs:333](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L333) (economy_item_abilities_chance)；[Unit/Systems/UnitDamageSystem.cs:120](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L120) (block_to_supercrit)；[Unit/Systems/UnitDamageSystem.cs:130](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L130) (evasion_mark)；[Unit/Systems/UnitDamageSystem.cs:346](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L346) (mark_lifesteal)；[Meta/SoulDropAmountCalculator.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SoulDropAmountCalculator.cs)；[Meta/SpawnerTimerCalculator.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SpawnerTimerCalculator.cs)；[Meta/SpawnerChargesCalculator.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SpawnerChargesCalculator.cs)；[Player/PlayerAttackSystem.cs:81](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L81)；[Shop/ChestsModel.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs)；[Quests/Commands/ClaimQuestStepRewardCommandHandler.cs:120](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/Commands/ClaimQuestStepRewardCommandHandler.cs#L120)；[Services/Damage/DamageCalculator.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs) (tower_damage)

**备注**：Meta keys have no StatModifierMultipliers (UpgradeModifierDef has no multiplier), so values are exactly the lambdas, cast to float in ApplyAll. Cost multipliers still apply to node cost. Several lambdas compute in float32 (RoundFloat32), so for example 0.05f*L carries float32 error before being widened. The daily free_chest node 3_15 sets chest_1 to the constant 0.0.

**依赖图节点**：输入 `value:player.upgrade_level`、`constant:AmuletUpgradeDefsBuilder.meta_lambdas`、`constant:AmuletUpgradeDefsBuilder.rage_extra_health_per_level`、`constant:AmuletUpgradeDefsBuilder.rage_extra_hit_threshold`、`constant:AmuletUpgradeDefsBuilder.rage_damage_reduce_per_level`、`constant:AmuletUpgradeDefsBuilder.stat_drop_per_level`、`constant:AmuletUpgradeDefsBuilder.map_spawner_timer_reduce`、`constant:AmuletUpgradeDefsBuilder.items_rarity_values`、`constant:AmuletUpgradeDefsBuilder.tower_damage_per_level`、`constant:AmuletUpgradeDefsBuilder.event_golem_damage`、`constant:AmuletUpgradeDefsBuilder.economy_item_abilities_chance_per_level`、`constant:AmuletUpgradeDefsBuilder.crystal_timer_on_kill_reduce_per_level`、`constant:AmuletUpgradeDefsBuilder.mark_lightning_values`；输出 `value:player.upgrade_meta_modifier`

**核验记录**（已写入上方字段）：

- 采纳 `expression`（来自 `powerups.amulet_meta_modifiers`）：Checked by dumping every WithModifier call. free_chest weekly 3_14 = 7.0d and daily 3_15 = 0.0d (lines 467, 474). boss_tower_unlock is set for berserker/vampire_king/lich (561, 658, 717). Node 7_58 sets tower_drop_legend_per_floor for all six bosses (1943-1948). Node 4_47 sets event_golem_extra_range = 1 and stat_drop param -1 = 1 (860-861). loadout_button = 1 (175). feature_unlock has five keys including crystals_*. crystal_enchant_extra_attempts params 1/2/3 = L. Both event_golem_damage nodes are 0.5d. All of this is now in the expression.

### `powerups.amulet_node_availability`

**Amulet graph node availability and purchasability**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`config_rule`
- 来源：子系统梳理

```text
IsAvailable(k) = def exists AND level(k) < MaxLevel(k) AND AmuletLevel >= RequiredAmuletLevel(k) AND for all p in RequiredUpgradeKeys(k): level(p) >= 1. IsPurchasable = IsAvailable AND CanAfford(next cost) AND (level(k)>0 OR k is root OR any parent bought)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `RequiredAmuletLevel` | amulet level gate | Amulet Upgrade Config.Upgrades[].RequiredAmuletLevel |
| `RequiredUpgradeKeys` | parent nodes | Amulet Upgrade Config.Upgrades[].RequiredUpgradeKeys |

**输出**：bool

**代码**：[Amulet/Upgrades/AmuletUpgradesModel.cs:153-187](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradesModel.cs#L153)；[Amulet/Upgrades/AmuletUpgradeConfig.cs:46-64](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeConfig.cs#L46)；[Meta/Upgrades/UpgradeModel.cs:67-72](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeModel.cs#L67)

**状态依据**：No native annotations on IsAvailable/IsPurchasable/IsUnknown

**配置参数**：`Amulet Upgrade Config.Upgrades`

**使用方**：TryUpgrade；UI PurchasableNodeCount

**备注**：IsAvailable requires ALL parents, but IsUnknown is satisfied by ANY parent. Since IsAvailable is checked first, ALL wins.

**依赖图节点**：输入 `config:AmuletUpgradeConfig.Upgrades[].RequiredAmuletLevel`、`config:AmuletUpgradeConfig.Upgrades[].RequiredUpgradeKeys`、`value:amulet.level`、`value:player.upgrade_level`、`constant:AmuletUpgradeDefsBuilder.node_max_level`、`value:economy.upgrade_cost`、`value:economy.currency_balance`；输出 `value:amulet.node_available`

### `powerups.amulet_autobalance_bake`

**Amulet auto-balance bake (cost/stat multipliers)**

- 状态：**编辑器工具缺失**（`editor_only_missing`）
- 类型：`editor_generator`
- 来源：子系统梳理

```text
UNKNOWN. Inputs: per-level target days, cost and XP multipliers, and per-node simulation settings. Outputs: UpgradeCostMultipliers[key] and StatModifierMultipliers[key] (and possibly RequiredExperiencePerLevel)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `LevelTargetDays` | target day for amulet level 1..7 | Amulet Config.LevelTargetDays = [20,26,34,56,90,130,180] (hex int32 array) |
| `AutoBalanceCostMultiplier` | global cost scale | Amulet Config (=1.25) |
| `AutoBalanceExperienceMultipliers` | per-level XP scale | Amulet Config = [0.8,0.8,0.85,0.9,0.95,1,1] |
| `SimTargetLevel,SimWeight,SimDay,SimStatSnapshotPercent,SimCostShare` | per-node simulation hints | UpgradeDef.WithSimulationSettings(...) in AmuletUpgradeDefsBuilder |
| `AutoBalanceStatTypes` | static stat-type set to balance | AmuletConfig static field (contents not recovered) |

**输出**：baked dictionaries in Amulet Config.asset

**代码**：[Amulet/AmuletConfig.cs:11](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletConfig.cs#L11)；[Amulet/AmuletConfig.cs:53-55](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletConfig.cs#L53)；[Meta/Upgrades/UpgradeDef.cs:144-157](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeDef.cs#L144)

**状态依据**：BakeAmuletAutoBalance body empty; Sim* fields have no runtime reader

**配置参数**：`Amulet Config.LevelTargetDays`；`Amulet Config.AutoBalanceCostMultiplier`；`Amulet Config.AutoBalanceExperienceMultipliers`；`Amulet Config.UpgradeCostMultipliers`；`Amulet Config.StatModifierMultipliers`

**使用方**：powerups.amulet_upgrade_cost；powerups.amulet_upgrade_stat_value

**备注**：Takes List<AmuletLevelBalanceData> and List<SimulationSnapshot>, so it was driven by the simulation forecast. Re-tuning amulet numbers means reproducing it or editing the dictionaries directly.

**依赖图节点**：输入 `config:AmuletConfig.LevelTargetDays`、`config:AmuletConfig.AutoBalanceCostMultiplier`、`config:AmuletConfig.AutoBalanceExperienceMultipliers`、`value:sim.upgrade_purchase_hints`、`value:sim.snapshot_at_time`、`constant:AmuletConfig.AutoBalanceStatTypes`、`constant:AmuletUpgradeDefsBuilder.cost_lambdas`、`constant:AmuletUpgradeDefsBuilder.stat_lambdas`；输出 `config:AmuletConfig.UpgradeCostMultipliers.valueData`、`config:AmuletConfig.StatModifierMultipliers.valueData`、`config:AmuletConfig.RequiredExperiencePerLevel`

### `powerups.rage_thresholds`

**Rage level thresholds**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`lookup_table`
- 来源：子系统梳理

```text
thr(level) = [30, 100, 270, 270][level] for level in 0..3, else +inf. Cumulative rage points needed from 0: lvl1=30, lvl2=130, lvl3=400, lvl4=670
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `level` | current RageLevel | runtime |

**输出**：rage points to next level

**代码**：[Rage/RageModel.cs:11-17](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rage/RageModel.cs#L11)；[Rage/RageModel.cs:152-155](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rage/RageModel.cs#L152)

**状态依据**：Hard-coded static array without native annotation

**使用方**：powerups.rage_gain

**备注**：Not data-driven; changing the rage curve means changing code.

**依赖图节点**：输入 `constant:RageModel.Thresholds`、`value:rage.level`；输出 `value:rage.level_threshold`

### `powerups.rage_gain`

**Rage accumulation and level-up**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
TryAddRage(a): if EffectiveMax<=0 return false; TimeSinceLastRage=0; emit RageGainedEvent(a); if RageLevel>=EffectiveMax {Points=0; return false}; Points += a; while Points>=thr(RageLevel) AND RageLevel<EffectiveMax {Points -= thr; RageLevel++}; if RageLevel>=EffectiveMax: Points=0
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `a` | rage added per proc | AddRageEffect.Amount (item data: 2,4,5,8,12) |
| `EffectiveMax` | max rage level | powerups.rage_effective_max |

**输出**：RageLevel 0..EffectiveMax

**代码**：[Rage/RageModel.cs:157-183](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rage/RageModel.cs#L157)

**状态依据**：No native annotation on TryAddRage

**配置参数**：`AddRageEffect.Amount (cut *.asset)`

**使用方**：powerups.add_rage_effect；powerups.rage_extra_hit；powerups.value_scaler (PerRageLevel)

**备注**：RageGainedEvent fires even at max level, so rage_extra_hit keeps accumulating while capped.

**依赖图节点**：输入 `value:rage.points_gained`、`value:rage.level_threshold`、`value:rage.effective_max_level`；输出 `value:rage.level`

### `powerups.rage_effective_max`

**Rage max accessible level**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x15efa6c (GrantRageAccessEffect.Execute), 0x15efb00 (RecalculateMaxLevel), 0x15efa10 (IndependentTrigger), 0x15f0184 (ctor), 0x1460790 (PlayerModel.SyncRageAccessFromEquipment)`

```text
RecalculateMaxLevel: MaxAccessibleLevel = max over equipped items, their AbilityStates and each state's Def.Effects of GrantRageAccessEffect.Level (0 if none). It is assigned directly, not via the max-only SetMaxAccessibleLevel, then SyncToWeaponAccess runs. EffectiveMaxLevel = MaxAccessibleLevel + BossMaxLevelBonus (1 while the Berserker boss is active, else 0). ActiveRageLevel = EffectiveMaxLevel > 0 ? RageLevel : 0. SyncToWeaponAccess: if EffectiveMaxLevel <= 0 return; if RageLevel > EffectiveMaxLevel then RageLevel = EffectiveMaxLevel and RagePoints = 0. SetBossMaxLevelBonus(b) sets the bonus and then runs SyncToWeaponAccess.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `GrantRageAccessEffect.Level` | rage cap granted by an item ability (default 1) | item asset (cut legendary.asset: 1,2,3) |
| `BossMaxLevelBonus` | boss fight bonus | BossTower/Systems/BerserkerBossSystem.cs:73 (1 on enter), 84 (0 on exit) |

**输出**：MaxAccessibleLevel, EffectiveMaxLevel, ActiveRageLevel

**代码**：[Abilities/Effects/GrantRageAccessEffect.cs:13-33](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/GrantRageAccessEffect.cs#L13)；[Rage/RageModel.cs:113-133](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rage/RageModel.cs#L113)；[Rage/RageModel.cs:200-220](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rage/RageModel.cs#L200)；[Player/Models/PlayerModel.cs:778](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L778)；[Player/Models/PlayerModel.cs:797](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L797)；[Player/Models/PlayerModel.cs:802-806](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L802)；[Player/Models/PlayerModel.cs:826](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L826)；[Player/Commands/EvolveItemCommandRunner.cs:41](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Commands/EvolveItemCommandRunner.cs#L41)；[BossTower/Systems/BerserkerBossSystem.cs:71-86](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/Systems/BerserkerBossSystem.cs#L71)；[Abilities/Systems/AbilityExecuteEventSystem.cs:222-227](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L222)

**状态依据**：GrantRageAccessEffect.RecalculateMaxLevel and Execute have native VA comments. The RageModel getters and SyncToWeaponAccess are unannotated.

**配置参数**：`GrantRageAccessEffect.Level`

**使用方**：powerups.rage_gain；powerups.rage_extra_health；combat.rage_damage_reduction；powerups.crystal_rage_marks_lightning；powerups.add_rage_effect

**备注**：Recalculated on OnEquip (IndependentTrigger 0) events for the item being (un)equipped only, because OnEquip is not in the cross-owner mask 0x3feea. It is also recalculated directly by PlayerModel.SyncRageAccessFromEquipment after equip/unequip changes and by EvolveItemCommandRunner after evolving. SyncToWeaponAccess clamps only when EffectiveMax > 0. If access drops to 0, RageLevel is kept but ActiveRageLevel reads 0. RageModel.SetMaxAccessibleLevel (max-only) is unused. AddRageEffect gates on MaxAccessibleLevel, not EffectiveMaxLevel, so the Berserker +1 never grants rage access on its own.

**依赖图节点**：输入 `value:player.equipped_items`、`value:item.unlocked_abilities`、`config:ItemConfig.AbilityDefs[].Effects`、`state:boss_tower_context`、`constant:BerserkerBossSystem.boss_max_level_bonus`、`value:rage.level`；输出 `value:rage.effective_max_level`、`value:rage.active_level`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `powerups.rage_effective_max`）：Confirmed: ShouldProcessForOwner mask 0x3feea has bit 0 clear, so OnEquip is source-item-only. SyncToWeaponAccess returns early when EffectiveMaxLevel <= 0 (RageModel.cs:207). RecalculateMaxLevel assigns MaxAccessibleLevel directly (GrantRageAccessEffect.cs:31), and SetMaxAccessibleLevel has no callers. The notes and expression reflect this.

### `powerups.rage_decay`

**Rage decay / reset**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x141fff8 (ResetRage), 0x141fd0c (ClearRage), 0x15f1028 (ResetRageOnLethalEffect.Execute), 0x1469b28 (RevivePlayerCommandHandler.Execute), 0x15e8368 (AbilityExecuteEventSystem.OnUpdate)`

```text
Tick(dt) runs every frame at the end of AbilityExecuteEventSystem.OnUpdate: if EffectiveMaxLevel <= 0 return; if not (RagePoints > 0 or RageLevel > 0) return; TimeSinceLastRage += dt; if TimeSinceLastRage >= 10 s then ClearRage() (RagePoints = 0, RageLevel = 0; timer not reset). TimeSinceLastRage is reset to 0 only by TryAddRage, including when already at the cap. ResetRage() (ResetRageOnLethalEffect, RevivePlayerCommandHandler) zeroes points and level but not the timer. DecreaseLevel(n): if RageLevel <= 0 return (Points untouched); else RageLevel = max(0, RageLevel - n), RagePoints = 0. It is called with n = 1 when a Viking dies.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `DecayTimeout` | seconds without rage gain before a full reset | constant 10 (RageModel.DecayTimeout) |
| `dt` | frame delta time | AbilityExecuteEventSystem.OnUpdate |

**输出**：rage reset (RagePoints, RageLevel)

**代码**：[Rage/RageModel.cs:18](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rage/RageModel.cs#L18)；[Rage/RageModel.cs:141-150](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rage/RageModel.cs#L141)；[Rage/RageModel.cs:157-161](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rage/RageModel.cs#L157)；[Rage/RageModel.cs:185-198](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rage/RageModel.cs#L185)；[Rage/RageModel.cs:222-227](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rage/RageModel.cs#L222)；[Abilities/Effects/ResetRageOnLethalEffect.cs:9-13](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/ResetRageOnLethalEffect.cs#L9)；[Abilities/Systems/AbilityExecuteEventSystem.cs:49](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L49)；[BossTower/Systems/VikingsRageDebuffSystem.cs:47-50](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/Systems/VikingsRageDebuffSystem.cs#L47)；[Player/Commands/RevivePlayerCommandHandler.cs:32](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Commands/RevivePlayerCommandHandler.cs#L32)

**状态依据**：RageModel.Tick and DecreaseLevel have no annotation. ResetRage, ClearRage and the callers (ResetRageOnLethalEffect, RevivePlayerCommandHandler, OnUpdate) do.

**使用方**：[BossTower/Systems/VikingsRageDebuffSystem.cs:49](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/Systems/VikingsRageDebuffSystem.cs#L49)；[Player/Commands/RevivePlayerCommandHandler.cs:32](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Commands/RevivePlayerCommandHandler.cs#L32)；powerups.rage_gain

**备注**：Decay is all-or-nothing after 10 s without a TryAddRage call, not gradual. Hitting while at the cap still resets the timer, so a capped rage level is held as long as rage-granting hits continue. The timer is not reset by ClearRage or ResetRage, but Tick only advances while points or level are above 0, and the next TryAddRage zeroes it.

**依赖图节点**：输入 `state:frame_delta_time`、`constant:RageModel.DecayTimeout`、`value:rage.effective_max_level`、`state:boss_tower_context`、`constant:VikingsRageDebuffSystem.level_decrease_per_viking_death`、`state:player_actions`、`value:unit.ability_trigger_events`；输出 `value:rage.level`

**核验记录**（已写入上方字段）：

- 采纳 `expression`（来自 `powerups.rage_decay`）：RageModel.cs:194-195 returns early when RageLevel <= 0 without touching RagePoints. VikingsRageDebuffSystem.OnVikingDied calls DecreaseLevel(1) (line 49). The expression has been updated.

### `powerups.add_rage_effect`

**AddRageEffect proc**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`dispatch only: 0x15e9d54 (ProcessIndependentEffects), 0x15e9d1c (ShouldProcessForOwner), 0x15cfc64 (FireHitTriggerEvents)`

```text
On each OnHit trigger event (fired NextFrame by UnitDamageSystem.FireHitTriggerEvents with Item = data.Weapon), only the OWNING weapon processes it. OnHit (trigger 2) is not in the cross-owner mask 0x3feea, so ShouldProcessForOwner passes only when owner == data.Item. Its independent effects then run without ability conditions (ProcessIndependentEffects). AddRageEffect.Execute: if MaxAccessibleLevel <= 0 or !TryAddRage(Amount) return. On a level-up (TryAddRage returned true): RageExtraHealthSystem.ForceApply(), spawn the RageLevelUps vfx[clamp(level-1)], play 'rage_level_up', ProcessEventImmediate(OnRageLevelUp), then RageLevelUpEvent{RageLevel} NextFrame.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Amount` | rage points per hit | AddRageEffect.Amount (int, default 1; data 2/4/5/8/12) |

**输出**：rage points / level-up events

**代码**：[Abilities/Effects/AddRageEffect.cs:16-39](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/AddRageEffect.cs#L16)；[Abilities/Systems/AbilityExecuteEventSystem.cs:157-160](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L157)；[Abilities/Systems/AbilityExecuteEventSystem.cs:209-227](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L209)；[Unit/Systems/UnitDamageSystem.cs:372](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L372)；[Rage/RageModel.cs:157-183](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rage/RageModel.cs#L157)

**状态依据**：AddRageEffect has no native annotation. Only the dispatch methods in AbilityExecuteEventSystem and UnitDamageSystem carry VAs.

**配置参数**：`AddRageEffect.Amount`

**使用方**：powerups.rage_gain；powerups.rage_decay (timer reset via TryAddRage)

**备注**：Rage per second is about Amount * hits/sec of the owning weapon only. Other equipped items do not add rage from that weapon's hits. Only cut (slashing) item assets carry AddRageEffect (cut uncommon/rare/epic/legendary). The gate is MaxAccessibleLevel, while TryAddRage itself also requires EffectiveMaxLevel > 0 and resets the decay timer.

**依赖图节点**：输入 `config:ItemConfig.AbilityDefs[].Effects`、`value:rage.effective_max_level`、`value:unit.ability_trigger_events`、`value:weapon.attacks_per_second`；输出 `value:rage.points_gained`、`value:unit.ability_trigger_events`

**核验记录**（已写入上方字段）：

- 采纳 `code_refs`（来自 `powerups.add_rage_effect`）：The class spans AddRageEffect.cs:16-39 and Execute is 27-39. ProcessIndependentEffects and ShouldProcessForOwner are AbilityExecuteEventSystem.cs:209-227. code_refs are updated, plus the caller at 157-160.
- 采纳 `expression`（来自 `powerups.add_rage_effect`）：ShouldProcessForOwner (line 226) passes when owner == data.Item, trigger >= 20, or mask 0x3feea bit set. Bit 2 (OnHit) is clear. UnitDamageSystem.cs:372 fires OnHit with Item = data.Weapon. The expression now states owning-weapon-only.

### `powerups.crystal_rage_marks_lightning`

**Crystal legendary ability effects applied per frame**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15e863c`

```text
for each equipped crystal with a legendary ability, v = CrystalBalance.GetLegendaryAbilityValue: MaxMarksPerEnchant: BonusMaxMarks += (int)v; DamagePerRageLevel: D += v; HammerLightningChancePerEnchant: BonusLightningChance += v/100. Then if ActiveRageLevel>0 and D>0: PlayerAbilityDamageMultiplier *= (D * ActiveRageLevel / 100 + 1)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `v` | crystal legendary value | Crystals/CrystalBalance (crystal subsystem) |
| `ActiveRageLevel` | rage | powerups.rage_effective_max |

**输出**：player damage multiplier, mark cap, lightning chance

**代码**：[Abilities/Systems/AbilityExecuteEventSystem.cs:89-108](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L89)

**状态依据**：Comment 'Native ELF VA 0x15e863c'

**配置参数**：`Crystal config (legendary abilities)`

**使用方**：powerups.marks；powerups.lightning_damage；combat outgoing damage (owner.AbilityDamageMultiplier)

**备注**：Runs after the per-frame reset, before passive abilities.

**依赖图节点**：输入 `state:crystal_equipment`、`config:CrystalConfig.LegendaryMaxMarksPerEnchant`、`config:CrystalConfig.LegendaryDamagePerRageLevelBase`、`config:CrystalConfig.LegendaryDamagePerRageLevelPerEnchant`、`config:CrystalConfig.LegendaryHammerLightningChancePerEnchant`、`value:rage.active_level`；输出 `value:player.ability_damage_multiplier`、`value:player.bonus_max_marks`、`value:player.bonus_lightning_chance`

### `powerups.ability_tier_override`

**Ability tier (star) override**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x15e306c (Stars), 0x15e2440 (GetBaseValue), 0x15e3120 (GetCurrentOverride), 0x15e2bc0 (GetBaseChance), 0x15e27c0 (GetBaseStackLimit), 0x15e360c (GetNextOverride), 0x15e2534, 0x15e2cb4, 0x15e28d4 (NextStar getters), 0x15e342c (GetNextStarLevel), 0x15e1590 (HasNextUpgrade)`

```text
Stars = ItemModel.IsPreDivine ? 0 : ItemModel.Level. For X in {Value, Chance, StackLimit}: base_X = !Affects_X ? Def.X : the X_override of the LevelScaling entry with the greatest Level such that 0 <= Level <= Stars. The scan starts at bestLevel = -1, so negative-Level entries are never selected. The comparison is strict, so the first entry wins ties, and the override is used even if negative. If no entry qualifies, base_X = Def.X. GetBaseStackLimit truncates toward zero ((int)). NextStar_X: !Affects_X ? Def.X : the override of the entry with the smallest Level > Stars; if there is none or the override < 0, it falls back to the current tier (GetCurrentOverride). GetNextStarLevel = the smallest Level > Stars, else Stars. HasNextUpgrade = any LevelScaling.Level > Stars, independent of the Affects flags.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Def.Value, Def.Chance, Def.StackLimit` | base ability numbers | item asset AbilityDef |
| `LevelScaling[]` | {Level, ValueOverride, ChanceOverride, StacksOverride} | item asset AbilityDef.LevelScaling (88 assets) |
| `AffectsValue/AffectsChance/AffectsStacks` | whether stars change X | item asset AbilityDef |
| `Stars` | item level once divine, else 0 | ItemModel.IsPreDivine, ItemModel.Level |

**输出**：base Value/Chance/StackLimit and the next-star previews

**代码**：[Abilities/AbilityState.cs:71](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L71)；[Abilities/AbilityState.cs:73-83](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L73)；[Abilities/AbilityState.cs:99-103](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L99)；[Abilities/AbilityState.cs:112-125](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L112)；[Abilities/AbilityState.cs:133-143](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L133)；[Abilities/AbilityState.cs:151-161](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L151)；[Abilities/AbilityState.cs:185-198](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L185)；[Abilities/AbilityState.cs:200-213](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L200)

**状态依据**：Every method involved carries a 'Native ELF VA' comment.

**配置参数**：`AbilityDef.Value`；`AbilityDef.Chance`；`AbilityDef.StackLimit`；`AbilityDef.LevelScaling`；`AbilityDef.Affects*`

**使用方**：all ability effects；powerups.ability_effective_value；powerups.ability_boosts (IsPercent uses target base value)

**备注**：Ability power is a step function of item level (stars). It is not interpolated. Pre-divine items always use tier 0 (Stars = 0 whatever their Level). That is Def.X unless a LevelScaling entry has Level == 0; negative-Level entries are ignored by GetCurrentOverride.

**依赖图节点**：输入 `config:ItemConfig.AbilityDefs[].Value`、`config:ItemConfig.AbilityDefs[].Chance`、`config:ItemConfig.AbilityDefs[].StackLimit`、`config:ItemConfig.AbilityDefs[].AffectsValue`、`config:ItemConfig.AbilityDefs[].AffectsChance`、`config:ItemConfig.AbilityDefs[].AffectsStacks`、`config:ItemConfig.AbilityDefs[].LevelScaling[].Level`、`config:ItemConfig.AbilityDefs[].LevelScaling[].ValueOverride`、`config:ItemConfig.AbilityDefs[].LevelScaling[].ChanceOverride`、`config:ItemConfig.AbilityDefs[].LevelScaling[].StacksOverride`、`value:item.evolution_level`；输出 `value:ability.base_value`

**核验记录**（已写入上方字段）：

- 采纳 `code_refs`（来自 `powerups.ability_tier_override`）：Checked against the file: Stars at 71, GetBaseValue 99-103, GetBaseChance 133-137, GetBaseStackLimit 151-155, GetCurrentOverride 185-198, GetNextOverride 200-213. The refs were also extended to the NextStar getters (121-125, 139-143, 157-161), GetNextStarLevel (112-119) and HasNextUpgrade (73-83).
- 合并复核修正：The expression and notes do not match GetCurrentOverride (AbilityState.cs:185-198). The method starts with bestLevel = -1 and selects an entry only when scale.Level <= Stars && bestLevel < scale.Level, so an entry with Level < 0 can never be chosen. The current tier is therefore the greatest Level with 0 <= Level <= Stars, not just the greatest Level <= Stars. The notes say pre-divine items use Def.X 'unless a LevelScaling entry has Level <= 0', but only a Level == 0 entry can override tier 0. The entry also omits that IsPreDivine items get Stars = 0 whatever their Level.

### `powerups.ability_effective_value`

**Effective ability value/chance/stack limit**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x15e30fc (GetValue), 0x15e3368 (CalculateScaledValue), 0x15e3874 (GetChance), 0x15e3898 (GetStackLimit), 0x15ea7fc (ExecuteEffects), 0x15e8420 (ResetPerFrameState), 0x15e9450 (ProcessTriggerEvent)`

```text
Value = base_Value + BonusValue + NextProcBonusValue; Chance = base_Chance + BonusChance + NextProcBonusChance; StackLimit = base_StackLimit + BonusStackLimit + NextProcBonusStackLimit (float32 / int). CalculateScaledValue(x) = Mathf.Approximately(Def.Value, 0) ? x : (base_Value / Def.Value) * x, in float32 using the raw Def.Value. Bonus* are zeroed every frame in ResetPerFrameState, refilled by the passive boost pass, and zeroed and recomputed for the target state on each trigger (RecomputeBoostsForTrigger). After that, if the economy proc bonus is > 0 and the ability is an economy ability, BonusChance += economy_item_abilities_chance. NextProcBonus* are zeroed after ExecuteEffects, i.e. only when the ability's conditions passed and it executed.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `base_X` | tier-selected base value | powerups.ability_tier_override |
| `Bonus*` | persistent boosts recomputed per frame and per trigger | powerups.ability_boosts |
| `NextProcBonus*` | one-shot boosts, cleared after the ability executes | powerups.ability_boosts |
| `economyBonus` | MetaStat economy_item_abilities_chance added to BonusChance of economy abilities on trigger | powerups.economy_proc_bonus |

**输出**：Value, Chance, StackLimit

**代码**：[Abilities/AbilityState.cs:93-110](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L93)；[Abilities/AbilityState.cs:127-131](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L127)；[Abilities/AbilityState.cs:145-149](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L145)；[Abilities/Systems/AbilityExecuteEventSystem.cs:52-72](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L52)；[Abilities/Systems/AbilityExecuteEventSystem.cs:161-171](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L161)；[Abilities/Systems/AbilityExecuteEventSystem.cs:242-251](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L242)

**状态依据**：GetValue, CalculateScaledValue, GetChance, GetStackLimit and ExecuteEffects carry native VA comments.

**使用方**：all ability effects/conditions；powerups.ability_boosts (booster Scaled uses its own GetValue)

**备注**：NextProc bonuses persist across frames until the target ability actually executes (conditions pass). Per-frame Bonus* do not.

**依赖图节点**：输入 `value:ability.base_value`、`value:ability.boost_bonus`、`value:ability.economy_proc_chance_bonus`、`config:ItemConfig.AbilityDefs[].Value`；输出 `value:ability.effective_value`、`value:ability.effective_chance`、`value:ability.effective_stack_limit`

**核验记录**（已写入上方字段）：

- 采纳 `code_refs`（来自 `powerups.ability_effective_value`）：Checked against the file: GetValue/GetBaseValue/CalculateScaledValue 93-110, GetChance 127-131, GetStackLimit 145-149, ExecuteEffects with the NextProc clear at AbilityExecuteEventSystem.cs:242-251. Refs for the per-frame reset (52-72) and the trigger path (161-171) were added.

### `powerups.value_scaler`

**Ability value scaling factor**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15f3ca8, 0x15f2c8c, 0x15f3ed0, 0x15f4014, 0x15f4140, 0x15f41bc`

```text
Scaled = Value * S(mode); S: None=1; PerMissingHealthPercent=(1 - HP/maxHP)*100; PerHealthAbovePercent=max(0,(HP/maxHP - thr)*100) where thr = first fixed Player-Above HealthThresholdCondition threshold else Def.Chance; PerMarksOnTarget=mark stacks; PerRageLevel=ActiveRageLevel; PerEmptySlot = TreatAsAllSlotsEmpty ? ItemSlots.Count : EmptyItemSlots.Count; PerConsumedShockOnTarget=ConsumedShockCount; PerHitsTaken=min(HitsCounter, StackLimit); PerStat=player SpecificValue(ScalingStat)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Value` | effective ability value | powerups.ability_effective_value |

**输出**：scaled ability magnitude

**代码**：[Abilities/Core/ValueScaler.cs:5-69](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Core/ValueScaler.cs#L5)；[Abilities/Core/AbilityValueScaling.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Core/AbilityValueScaling.cs)

**状态依据**：Native VA comments

**配置参数**：`<Effect>.ValueScaling`；`<Effect>.ScalingStat`

**使用方**：most ability effects；powerups.chance_condition

**备注**：Missing-health percent is expressed in points from 0 to 100, so Value is a per-percent coefficient.

**依赖图节点**：输入 `value:ability.effective_value`、`config:ItemConfig.AbilityDefs[].Effects`、`value:player.current_hp`、`value:player.stat_specific[Health]`、`config:ItemConfig.AbilityDefs[].Conditions`、`config:ItemConfig.AbilityDefs[].Chance`、`value:enemy.mark_stacks`、`value:rage.active_level`、`value:player.empty_slot_count`、`value:enemy.consumed_shock_count`、`value:ability.hits_counter`、`value:ability.effective_stack_limit`、`value:player.stat_specific`、`value:player.auto_target`；输出 `value:ability.scaled_value`

### `powerups.ability_boosts`

**Ability-to-ability boosts**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x15eec80, 0x15eec6c, 0x15eee10, 0x15eec5c (Value); 0x15ee924, 0x15ee910, 0x15eea80, 0x15ee900 (Chance); 0x15eeac8, 0x15eeab4, 0x15eec38, 0x15eeaa4 (StackLimit); 0x15f2c8c (GetScaledValue); 0x15e8420 (ResetPerFrameState); 0x15e89a0 (UpdatePassiveAbilities); 0x15e8cc8 (UpdatePassiveEffectsForOwner); 0x15ea284 (RecomputeBoostsForTrigger); 0x15ea7fc (ExecuteEffects)`

```text
Common: Scaled = booster.GetValue() * ValueScaler.Resolve(ValueScaling, ctx, ScalingStat). The target is owner.AbilityStates[TargetAbilityIndex] of the same item; an out-of-range index is a no-op. BoostAbilityValue: v = Scaled * (IsPercent ? target.GetBaseValue() : 1). BoostAbilityChance: v = Scaled. BoostAbilityStackLimit: v = (int)Scaled (truncation toward zero). If BoostNextProcOnly, NextProcBonus{Value,Chance,StackLimit} += v; this is applied in Execute, i.e. when the booster's own ability fires via ExecuteEffects. Otherwise Bonus{Value,Chance,StackLimit} += v, applied in UpdatePassive (HasPassiveTick = !BoostNextProcOnly). Per frame: ResetPerFrameState zeroes Bonus*. Then, for each equipped item, the boost pass runs before the regular passive pass. A boost effect is applied if (its ability is Passive and its conditions hold) or (its ability is not Passive and HasPassiveTick). On a trigger, RecomputeBoostsForTrigger zeroes the target state's Bonus* and re-applies every non-independent boost effect of the same owner whose TargetAbilityIndex equals the target's index. Passive boosters apply only if their conditions hold; non-passive ones apply unconditionally, with dt = 0.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `TargetAbilityIndex` | index into the owner's AbilityStates | effect data |
| `ValueScaling, ScalingStat` | scaling mode and stat for the booster's value | effect data, resolved by powerups.value_scaler |
| `IsPercent` | Value boost multiplies by the target's base value | BoostAbilityValueEffect.IsPercent |
| `BoostNextProcOnly` | one-shot boost consumed on the target's next execution | BoostAbility*Effect.BoostNextProcOnly |

**输出**：Bonus* / NextProcBonus* fields of other AbilityStates

**代码**：[Abilities/Effects/BoostAbilityValueEffect.cs:22-45](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/BoostAbilityValueEffect.cs#L22)；[Abilities/Effects/BoostAbilityChanceEffect.cs:20-43](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/BoostAbilityChanceEffect.cs#L20)；[Abilities/Effects/BoostAbilityStackLimitEffect.cs:20-44](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/BoostAbilityStackLimitEffect.cs#L20)；[Abilities/Core/ValueScaler.cs:66-70](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Core/ValueScaler.cs#L66)；[Abilities/Systems/AbilityExecuteEventSystem.cs:52-87](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L52)；[Abilities/Systems/AbilityExecuteEventSystem.cs:110-140](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L110)；[Abilities/Systems/AbilityExecuteEventSystem.cs:161-171](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L161)；[Abilities/Systems/AbilityExecuteEventSystem.cs:178-194](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L178)；[Abilities/Systems/AbilityExecuteEventSystem.cs:242-251](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L242)

**状态依据**：Native VA comments on every ApplyBonus/UpdatePassive/Execute/HasPassiveTick member and on the system methods involved.

**配置参数**：`BoostAbility*Effect.TargetAbilityIndex`；`BoostAbility*Effect.ValueScaling`；`BoostAbility*Effect.ScalingStat`；`BoostAbilityValueEffect.IsPercent`；`BoostAbility*Effect.BoostNextProcOnly`

**使用方**：powerups.ability_effective_value

**备注**：Used by 8 BoostAbilityValue, 3 Chance and 1 StackLimit effects in item data. The Chance and StackLimit boosts also support BoostNextProcOnly. Only the Value boost has IsPercent, and it scales by the target's base (tier) value, not its effective value. A booster's Scaled uses the booster's effective GetValue, so boosts on the booster itself carry through.

**依赖图节点**：输入 `value:ability.scaled_value`、`value:ability.base_value`、`config:ItemConfig.AbilityDefs[].Effects`、`value:ability.condition_pass`、`value:unit.ability_trigger_events`；输出 `value:ability.boost_bonus`

**核验记录**（已写入上方字段）：

- 采纳 `code_refs`（来自 `powerups.ability_boosts`）：Checked against the file: ResetPerFrameState 52-87, UpdatePassiveAbilities/UpdatePassiveEffectsForOwner 110-140 (boost pass then regular pass), RecomputeBoostsForTrigger 178-194. The effect-file ranges were also corrected and ValueScaler and ExecuteEffects refs added.

### `powerups.economy_proc_bonus`

**Economy ability proc-chance bonus**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x15e997c (ComputeEconomyProcBonus), 0x15ea664 (IsEconomyAbility wrapper), 0x15e21a0 (AbilityDescriptionContext.IsEconomyAbility), 0x15e2338 (description ComputeEconomyProcBonus), 0x15e9450 (ProcessTriggerEvent), 0x15ea284 (RecomputeBoostsForTrigger), 0x15e3874 (GetChance), 0x15f437c (ChanceCondition.Evaluate)`

```text
Per AbilityTriggerEvent (ProcessTriggerEvent): economyBonus = MetaStat('economy_item_abilities_chance').Value (0 if the stat is missing), read once per event. For each owner/state with Def.Trigger == event.Trigger that passes ShouldProcessForOwner: RecomputeBoostsForTrigger first resets state.BonusChance = 0 and re-adds boost effects. Then, if economyBonus > 0 AND IsEconomyAbility(Def): state.BonusChance += economyBonus. IsEconomyAbility means the top-level Def.Conditions contains a ChanceCondition AND Def.Effects contains an AddStatEffect with Source == AbilityPermanent. The ChanceCondition then fires iff Random.value < (GetBaseChance() + BonusChance + NextProcBonusChance) * ValueScaler.Resolve(ChanceCondition.Scaling). UI: when the template contains {chance}, IsEconomyAbility(def) holds and economyBonus*100 > 0, the chance text gets the green suffix ' (+economyBonus*100%)'.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `economy_item_abilities_chance` | additive proc chance (fraction) for economy abilities | MetaStatsModel. Amulet graph upgrades (see powerups.amulet_meta_modifiers): 3_13 = 0.01*L max 2, 5_40 = 0.01*L max 2, 7_28 = 0.02*L max 2. Crystal legendary EconomyProcChancePerEnchant = LegendaryAbilityValue/100 (Crystals/CrystalModel.cs:1201-1213). |
| `GetBaseChance` | ability Def.Chance, or the star-level ChanceOverride when AffectsChance | Abilities/AbilityState.cs:133-137 |
| `NextProcBonusChance` | one-shot chance bonus cleared after ExecuteEffects | Abilities/AbilityState.cs:43 |
| `Resolve(Scaling)` | ChanceCondition value scaler (1 for no scaling) | Abilities/Core/ValueScaler.cs:12-33 |

**输出**：additive proc chance on economy (permanent-stat) item abilities, i.e. the rate of permanent stat gain from item procs

**代码**：[Abilities/Systems/AbilityExecuteEventSystem.cs:153](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L153)；[Abilities/Systems/AbilityExecuteEventSystem.cs:165-166](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L165)；[Abilities/Systems/AbilityExecuteEventSystem.cs:178-194](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L178)；[Abilities/Systems/AbilityExecuteEventSystem.cs:330-341](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L330)；[Abilities/AbilityDescriptionContext.cs:45-54](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityDescriptionContext.cs#L45)；[Abilities/AbilityDescriptionContext.cs:109-120](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityDescriptionContext.cs#L109)；[Abilities/AbilityDescriptionContext.cs:122-128](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityDescriptionContext.cs#L122)；[Abilities/AbilityState.cs:127-131](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L127)；[Abilities/Conditions/ChanceCondition.cs:11-15](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/ChanceCondition.cs#L11)；[Crystals/CrystalModel.cs:1201-1213](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Crystals/CrystalModel.cs#L1201)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:457-462](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L457)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:1123-1126](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L1123)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:1744-1749](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L1744)；[UI/Inventory/ItemAbilityView.cs:74](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/Inventory/ItemAbilityView.cs#L74)

**状态依据**：ComputeEconomyProcBonus (AbilityExecuteEventSystem.cs:332), IsEconomyAbility (:339, AbilityDescriptionContext.cs:111) and the applying ProcessTriggerEvent (:150) all carry Native ELF VA comments.

**配置参数**：`graph_economy_item_abilities_chance_3_13`；`graph_economy_item_abilities_chance_5_40`；`graph_economy_item_abilities_chance_7_28`；`crystal LegendaryAbilityType.EconomyProcChancePerEnchant`；`ChanceCondition.Scaling`；`AbilityDef.Chance`；`AddStatEffect.Source (AbilityPermanent)`

**使用方**：powerups.chance_condition；powerups.add_stat_effect (permanent stat growth from item procs)；UI ability description {chance} placeholder (AbilityDescriptionContext)；[UI/Inventory/ItemAbilityView.cs:74](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/Inventory/ItemAbilityView.cs#L74) (AppendAccumulatedStat skips the accumulated permanent-stat suffix for economy abilities)

**备注**：Increases the rate of permanent stat gain from item procs (AddStatEffect to PlayerRewardSystem). The bonus is applied only on the triggered path (ProcessTriggerEvent, which includes ProcessEventImmediate calls). It is not applied to passive abilities or to IndependentTrigger effects. Because it is added to BonusChance before ChanceCondition multiplies by its Scaling resolver, a scaled ChanceCondition also scales the economy bonus. BonusChance is reset each frame in ResetPerFrameState and again in RecomputeBoostsForTrigger, so the bonus does not accumulate across events. Only top-level conditions are checked: a ChanceCondition nested in a Composite or Invert condition does not make an ability 'economy'.

**依赖图节点**：输入 `value:player.meta_stat[economy_item_abilities_chance]`、`config:CrystalConfig.LegendaryEconomyProcChancePerEnchant`、`state:crystal_equipment`、`config:ItemConfig.AbilityDefs[].Conditions`、`config:ItemConfig.AbilityDefs[].Effects`、`config:ItemConfig.AbilityDefs[].Trigger`、`value:unit.ability_trigger_events`；输出 `value:ability.economy_proc_chance_bonus`

**核验记录**（已写入上方字段）：

- 采纳 `code_refs`（来自 `powerups.economy_proc_bonus`）：Verified: compute at AbilityExecuteEventSystem.cs:153, BonusChance += economyBonus at :166 after RecomputeBoostsForTrigger at :165, helpers at 330-341, IsEconomyAbility at AbilityDescriptionContext.cs:109-120. Also kept the description ComputeEconomyProcBonus at 122-128.
- 合并复核修正：used_by mislabels ItemAbilityView.cs:74. That code is AppendAccumulatedStat, which returns early for economy abilities, so it does not render an economy description. The variable source drops 'max 2' for graph_economy_item_abilities_chance_5_40 (AmuletUpgradeDefsBuilder.cs:1124 WithMaxLevel(2)). The expression says the UI suffix is added for every economy ability, but AbilityDescriptionContext.cs:50-51 adds it only when bonus > 0 and only through the {chance} placeholder. The member's cross-ref to powerups.amulet_meta_modifiers was dropped. Also missing: IsEconomyAbility checks only top-level Def.Conditions, so a ChanceCondition nested in a Composite or Invert condition does not qualify.

### `powerups.chance_condition`

**Ability proc chance**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15f437c`

```text
proc iff Random.value < Chance_eff * S(Scaling)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Chance_eff` | effective chance | powerups.ability_effective_value |

**输出**：bool

**代码**：[Abilities/Conditions/ChanceCondition.cs:7-11](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/ChanceCondition.cs#L7)

**状态依据**：Native VA comment

**配置参数**：`AbilityDef.Chance`；`ChanceCondition.Scaling`

**使用方**：ability execution

**备注**：Other gating conditions are configured by data; formulas are in the notes of powerups.ability_gating_conditions.

**依赖图节点**：输入 `value:ability.effective_chance`、`config:ItemConfig.AbilityDefs[].Conditions`、`value:rage.active_level`、`value:player.empty_slot_count`、`state:rng`；输出 `value:ability.proc_roll`

### `powerups.ability_gating_conditions`

**Ability cooldown/hit-count/stack/threshold conditions**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`config_rule`
- 来源：合并核验
- 原生地址：`0x15f4728 (Cooldown), 0x15f49e4 (HitsCount), 0x15f4b60 (Stacks), 0x15f47f8 (HealthThreshold), 0x15f4924 (HealthThreshold.ResolveUnit), 0x15f43e8 (Combat), 0x15f44a4 (Combat ctor), 0x15f4788 (EmptySlots), 0x15f5130 (WeaponSet), 0x15f5190 (WeaponType), 0x15f5238 (WearingItem), 0x15f4bf8 (TargetBossArena), 0x15f4d80 (Target), 0x15f4edc (TargetShocked), 0x15f5014 (TargetStunned), 0x15f4b28 (SourceMatch), 0x15f4a68 (Invert), 0x15f44b4 (Composite), 0x15e920c (EvaluateConditions), 0x15e8420 (ResetPerFrameState cooldown tick), 0x15e3368 (CalculateScaledValue)`

```text
EvaluateConditions: no conditions -> true; ConditionMode All(0) = short-circuit AND, Any = short-circuit OR, so a stateful condition after a failing one is not evaluated. Cooldown: prev = state.Cooldown; if prev <= 0: state.Cooldown = ScaleByValue ? state.GetValue() : Cooldown; fires iff prev <= 0. The tick state.Cooldown -= dt runs every frame for every equipped item state in ResetPerFrameState, not in the condition. HitsCount: required = ScaleFromStackLimit ? GetStackLimit() : CalculateScaledValue(RequiredHits), where CalculateScaledValue(x) = Def.Value ~= 0 ? x : (GetBaseValue()/Def.Value)*x; fires iff HitsCounter >= required, then HitsCounter = 0. HitsCounter++ happens for every ability state of every equipped item on every outgoing player hit (DamageCalculator). Stacks: limit = GetStackLimit(); NotReachedMax(0): fires iff Stacks < limit, no increment; RequiresStacks(1): Stacks++, fires iff Stacks >= limit, then Stacks = 0 if ResetOnTrigger. HealthThreshold: unit = Subject==Target ? (event Target ?? player.CurrentTarget) : player; false if unit null or maxHP(GetStatSpecificValue(Health)) <= 0; thr = UseAbilityChance ? GetChance() : UseAbilityValue ? GetValue() : Threshold; ratio = (float)(HP/maxHP); Below(0): ratio <= thr; Above(1): ratio >= thr. Combat: inCombat = Time.time - max(LastDamageGet, LastDamageDealt) < Timeout (default 5 s); result = inCombat XOR (Mode != 0), so OutOfCombat(1) fires out of combat. EmptySlots: EmptyItemSlots.Count >= MinEmptySlots. WeaponSet: (Mode==DifferentWeaponTypes ? EquippedDifferentWeaponTypesCount : EquippedSameWeaponTypeMaxCount) >= RequiredCount. WeaponType: owner item Config.DamageType == RequiredType. WearingItem: any equipped item Config == RequiredItem. TargetBossArena: ExcludeBossArenaUnits(default true) ? !isBossArenaUnit && !InBossTowerArena : isBossArenaUnit. TargetShocked/TargetStunned: current target has shock/stun. Target: IsStationary/IsNotStationary/TargetChanged (state.CurrentTarget != player.CurrentTarget). SourceMatch OwnerOnly: SourceItem == Owner. Invert: Condition null -> true, else !Condition. Composite: nested All/Any.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `CooldownCondition.Cooldown` | seconds between procs (or GetValue() when ScaleByValue) | item ability data |
| `RequiredHits` | hit count, scaled by star level via CalculateScaledValue | item ability data |
| `StackLimit` | GetBaseStackLimit() + BonusStackLimit + NextProcBonusStackLimit | Abilities/AbilityState.cs:145-155 |
| `Threshold` | HP fraction [0,1] | item ability data |
| `Timeout` | combat window | CombatCondition.Timeout default 5 |
| `MinEmptySlots` | required empty slots | item ability data |
| `RequiredCount` | weapon set count | item ability data |

**输出**：bool (whether the ability's effects execute)

**代码**：[Abilities/Conditions/CooldownCondition.cs:13-19](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/CooldownCondition.cs#L13)；[Abilities/Conditions/HitsCountCondition.cs:13-20](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/HitsCountCondition.cs#L13)；[Abilities/Conditions/StacksCondition.cs:13-25](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/StacksCondition.cs#L13)；[Abilities/Conditions/HealthThresholdCondition.cs:22-40](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/HealthThresholdCondition.cs#L22)；[Abilities/Conditions/CombatCondition.cs:11-19](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/CombatCondition.cs#L11)；[Abilities/Conditions/EmptySlotsCondition.cs:11-15](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/EmptySlotsCondition.cs#L11)；[Abilities/Conditions/WeaponSetCondition.cs:13-18](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/WeaponSetCondition.cs#L13)；[Abilities/Conditions/WeaponTypeCondition.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/WeaponTypeCondition.cs)；[Abilities/Conditions/WearingItemCondition.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/WearingItemCondition.cs)；[Abilities/Conditions/TargetBossArenaCondition.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/TargetBossArenaCondition.cs)；[Abilities/Conditions/TargetCondition.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/TargetCondition.cs)；[Abilities/Conditions/TargetShockedCondition.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/TargetShockedCondition.cs)；[Abilities/Conditions/TargetStunnedCondition.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/TargetStunnedCondition.cs)；[Abilities/Conditions/SourceMatchCondition.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/SourceMatchCondition.cs)；[Abilities/Conditions/InvertCondition.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/InvertCondition.cs)；[Abilities/Conditions/CompositeCondition.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/CompositeCondition.cs)；[Abilities/Systems/AbilityExecuteEventSystem.cs:70](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L70)；[Abilities/Systems/AbilityExecuteEventSystem.cs:229-240](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L229)；[Abilities/AbilityState.cs:105-110](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L105)；[Services/Damage/DamageCalculator.cs:167-168](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L167)

**状态依据**：Each condition's Evaluate carries a Native ELF VA comment, as do EvaluateConditions and ResetPerFrameState.

**配置参数**：`CooldownCondition.Cooldown`；`CooldownCondition.ScaleByValue`；`HitsCountCondition.RequiredHits`；`HitsCountCondition.ScaleFromStackLimit`；`StacksCondition.Mode`；`StacksCondition.ResetOnTrigger`；`HealthThresholdCondition.Threshold/Subject/Mode/UseAbilityChance/UseAbilityValue`；`CombatCondition.Timeout`；`CombatCondition.Mode`；`EmptySlotsCondition.MinEmptySlots`；`WeaponSetCondition.RequiredCount`；`WeaponSetCondition.Mode`；`AbilityDef.ConditionMode`；`AbilityDef.StackLimit`

**使用方**：ability execution frequency；powerups.economy_proc_bonus；powerups.weapon_stat_stack

**备注**：Proc frequency is about min(attack rate * P, 1/cooldown). Because every equipped item's state counts every outgoing player hit, HitsCount procs every ceil(required) hits of any equipped weapon, not only the owning weapon. HealthThreshold's threshold can come from GetChance()/GetValue(), so chance/value boosts move it. Under All mode, Cooldown/Stacks/HitsCount side effects only run if every earlier condition passed.

**依赖图节点**：输入 `config:ItemConfig.AbilityDefs[].ConditionMode`、`config:ItemConfig.AbilityDefs[].Conditions`、`config:ItemConfig.AbilityDefs[].Value`、`value:ability.base_value`、`value:ability.effective_value`、`value:ability.effective_chance`、`value:ability.effective_stack_limit`、`value:ability.proc_roll`、`value:ability.hits_counter`、`value:ability.stacks`、`state:frame_delta_time`、`value:weapon.attacks_per_second`、`value:player.current_hp`、`value:player.stat_specific[Health]`、`value:enemy.current_hp`、`value:enemy.stat_raw[Health]`、`value:player.auto_target`、`value:unit.ability_trigger_events`、`state:utc_now`、`value:player.empty_slot_count`、`value:player.weapon_synergy_counts`、`value:player.equipped_items`、`config:ItemConfig.DamageType`、`value:enemy.stun_duration`、`state:boss_tower_context`；输出 `value:ability.condition_pass`、`value:ability.hits_counter`、`value:ability.stacks`

**核验记录**（已写入上方字段）：

- 采纳 `expression`（来自 `powerups.ability_gating_conditions`）：Verified against the code: the cooldown tick is in ResetPerFrameState (line 70); Stacks has NotReachedMax (no increment) and RequiresStacks modes (StacksCondition.cs:17-23); Combat returns inCombat != (Mode != 0) (CombatCondition.cs:18); HealthThreshold Subject resolves event target then current target (:34-40); HitsCounter++ for all equipped items' states at DamageCalculator.cs:167-168. All are now in the expression.
- 采纳 `code_refs`（来自 `powerups.ability_gating_conditions`）：EvaluateConditions is at AbilityExecuteEventSystem.cs:229-240 and the cooldown tick at :70. code_refs updated.

### `powerups.add_stat_effect`

**AddStatEffect (equip / temporary / permanent stat)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x15ec76c (AddStatEffect.Execute), 0x15ecd04 (ctor), 0x15ecb38 (FindStat), 0x15ecc00 (FindModifier), 0x15f3ab4 (KillStatRewardMultiplier), 0x15f2c8c (GetScaledValue), 0x1456084 (GrantStatReward), 0x1467c9c (TryConsumeProc)`

```text
v = ValueScaler.GetScaledValue = state.GetValue() * Resolve(ValueScaling, ScalingStat), where GetValue = base(star override) + BonusValue + NextProcBonusValue. If context.Trigger == OnEquip: on equip, stat.AddModifier(StatType, ModifierType, Name=Def.Key, Source, Value=v, Duration); on unequip, remove the modifier matching (Def.Key, Source). No daily-limit check. Otherwise: if UseDailyProcLimit and !DailyProcs.TryConsumeProc(Def.Key, DailyProcLimit) (count >= limit) -> no effect (this check runs first for both remaining branches). If Duration > 0: AddTemporaryModifierCommand(v, Duration, RemoveDuplicates=true). Else if !DisablePermanentStatsGain: m = KillStatRewardMultiplier = (Trigger==OnKill && Target != null) ? Target.SpawnerStatRewardMultiplier : 1; GrantStatReward(modifier Value = v*m). Inside GrantStatReward: Value *= cheats.DropMultiplier; DoubleLoot (Value *= 1+ability.GetValue()) and VIP stat_multiplier (on Chance roll, Value *= Multiplier) apply only when Source == Enemy, so AbilityPermanent procs are never doubled; non-percent Health rewards also raise CurrentHealth by Value.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `v` | scaled ability value | Abilities/Core/ValueScaler.cs:66-70 |
| `DailyProcLimit` | max procs per day when UseDailyProcLimit; <= 0 allows none | AddStatEffect.DailyProcLimit (default 1); Player/DailyProcs/DailyProcsModel.cs:18-23 |
| `SpawnerStatRewardMultiplier` | zero-cooldown stacked kill copies | Enemy/EnemySpawner.cs:1186 |
| `DropMultiplier` | cheat drop multiplier (1 normally) | CheatsModel |
| `DisablePermanentStatsGain` | player flag disabling permanent growth | PlayerModel |

**输出**：stat modifier (equip / temporary) or permanent stat growth

**代码**：[Abilities/Effects/AddStatEffect.cs:31-50](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/AddStatEffect.cs#L31)；[Abilities/Core/AbilityContext.cs:33](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Core/AbilityContext.cs#L33)；[Abilities/Core/ValueScaler.cs:66-70](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Core/ValueScaler.cs#L66)；[Player/Systems/PlayerRewardSystem.cs:49-84](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/PlayerRewardSystem.cs#L49)；[Player/DailyProcs/DailyProcsModel.cs:18-23](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/DailyProcs/DailyProcsModel.cs#L18)

**状态依据**：AddStatEffect.Execute carries a Native ELF VA comment (0x15ec76c). KillStatRewardMultiplier (0x15f3ab4) and GrantStatReward (0x1456084) are annotated too.

**配置参数**：`AddStatEffect.StatType`；`AddStatEffect.ModifierType`；`AddStatEffect.Source`；`AddStatEffect.Duration`；`AddStatEffect.ValueScaling`；`AddStatEffect.ScalingStat`；`AddStatEffect.UseDailyProcLimit`；`AddStatEffect.DailyProcLimit`；`AbilityDef.Value`

**使用方**：PlayerRewardSystem.GrantStatReward (devour growth)；combat stats；powerups.economy_proc_bonus (IsEconomyAbility keys on AddStatEffect with Source=AbilityPermanent)

**备注**：This is the most common effect in data (72 instances). On-kill permanent-stat procs are the item-driven part of permanent growth. The UI display divides by the kill multiplier and appends 'xN' when N > 1.

**依赖图节点**：输入 `value:ability.scaled_value`、`config:ItemConfig.AbilityDefs[].Effects`、`config:ItemConfig.AbilityDefs[].Key`、`config:ItemConfig.AbilityDefs[].Trigger`、`value:ability.condition_pass`、`value:ability.proc_roll`、`value:unit.ability_trigger_events`、`state:player_actions`、`state:save_counters`、`state:utc_now`、`value:spawner.kill_reward_multiplier`、`state:boss_tower_context`、`state:cheats`；输出 `value:ability.stat_modifier`、`value:reward.granted_stat_modifier`、`value:player.current_hp`

**核验记录**（已写入上方字段）：

- 采纳 `expression`（来自 `powerups.add_stat_effect`）：Verified: the daily-proc check (line 42) precedes both the Duration>0 branch (43-46) and the permanent branch (47-49). OnEquip passes Duration to the modifier. GrantStatReward applies DropMultiplier, and DoubleLoot/VIP only for Source==Enemy (PlayerRewardSystem.cs:54-73). Expression rewritten accordingly.
- 采纳 `code_refs`（来自 `powerups.add_stat_effect`）：Execute spans AddStatEffect.cs:31-50 and KillStatRewardMultiplier is at AbilityContext.cs:33.

### `powerups.add_stat_passive`

**Passive stat modifiers (AddStatPassive / StatStack / BonusStatPerHitsTaken)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15ecd14, 0x15ecfb8, 0x15eab18, 0x15f2700, 0x15f28d0, 0x15ee77c`

```text
AddStatPassive: v = Value*S; if NormalizeAboveThreshold AND scaling=PerHealthAbovePercent: v /= (1 - thr). StatStack: v = Value * Stacks, Stacks = min(StackLimit, Stacks+1) per proc, reset on target change. BonusStatPerHitsTaken: v = Value * (StackLimit>0 ? min(HitsTakenCount, StackLimit) : HitsTakenCount). All publish desired[key=ability|stat|modType|source] = max(v over duplicates, v>0 only), then sync once per frame as modifier(Value=v)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Stacks` | ability state stacks | runtime |
| `HitsTakenCount` | player hits taken | PlayerModel |

**输出**：live stat modifiers

**代码**：[Abilities/Effects/AddStatPassiveEffect.cs:41-79](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/AddStatPassiveEffect.cs#L41)；[Abilities/Effects/StatStackEffect.cs:13-35](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/StatStackEffect.cs#L13)；[Abilities/Effects/BonusStatPerHitsTakenEffect.cs:10-19](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/BonusStatPerHitsTakenEffect.cs#L10)；[Abilities/Systems/AbilityExecuteEventSystem.cs:266-277](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L266)

**状态依据**：Native VA comments

**配置参数**：`AddStatPassiveEffect.*`；`StatStackEffect.*`；`BonusStatPerHitsTakenEffect.*`

**使用方**：combat stats

**备注**：Duplicate identical abilities (same key) do not stack: the maximum value wins.

**依赖图节点**：输入 `value:ability.scaled_value`、`value:ability.effective_value`、`value:ability.effective_stack_limit`、`value:ability.stacks`、`value:player.hits_taken_count`、`config:ItemConfig.AbilityDefs[].Effects`、`config:ItemConfig.AbilityDefs[].Conditions`、`config:ItemConfig.AbilityDefs[].Chance`、`config:ItemConfig.AbilityDefs[].Key`、`value:ability.condition_pass`、`value:player.auto_target`；输出 `value:ability.stat_modifier`、`value:ability.stacks`

### `powerups.weapon_stat_stack`

**Weapon stat stack multiplier**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x15f3880 (Execute), 0x15f39d0 (UpdatePassive), 0x15e39f8 (GetWeaponStatMultiplier)`

```text
On proc (Execute): if ResetOnTargetChange && state.CurrentTarget != null && state.CurrentTarget != player.CurrentTarget: Stacks = 0; Stacks = min(GetStackLimit(), Stacks + 1); state.Duration = Duration. Per frame (UpdatePassive, HasPassiveTick): if state.Duration > 0 { Duration -= dt; if Duration <= 0: Stacks = 0 }. Multiplier(buffType) = (GetBaseValue() + BonusValue + NextProcBonusValue) * Stacks + 1 for the first WeaponStatStackEffect on the state whose BuffType matches, else 1. Consumers: Damage -> damage *= product over weapon states (DamageCalculator); CritDamage -> crit damage *= product (only when IsCrit or simulation); AttackSpeed -> weapon cooldown tick rate *= product (PlayerAttackSystem). CritChance has no runtime consumer.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `D` | stack duration | WeaponStatStackEffect.Duration |
| `StackLimit` | GetBaseStackLimit + BonusStackLimit + NextProcBonusStackLimit | Abilities/AbilityState.cs:145-155 |
| `Value_eff` | per-stack bonus = GetBaseValue + BonusValue + NextProcBonusValue | Abilities/AbilityState.cs:215-222 |

**输出**：per-weapon damage / crit damage / attack speed multiplier

**代码**：[Abilities/Effects/WeaponStatStackEffect.cs:9-29](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/WeaponStatStackEffect.cs#L9)；[Abilities/AbilityState.cs:215-222](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L215)；[Services/Damage/DamageCalculator.cs:142-151](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L142)；[Player/PlayerAttackSystem.cs:122-123](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L122)；[Player/PlayerAttackSystem.cs:171-172](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L171)；[Player/PlayerWeaponView.cs:353-354](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerWeaponView.cs#L353)

**状态依据**：Execute, UpdatePassive and GetWeaponStatMultiplier carry Native ELF VA comments.

**配置参数**：`WeaponStatStackEffect.BuffType`；`WeaponStatStackEffect.Duration`；`WeaponStatStackEffect.ResetOnTargetChange`；`AbilityDef.Value`；`AbilityDef.StackLimit`

**使用方**：combat outgoing damage；player weapon attack cadence；weapon hand animation speed (PlayerWeaponView)

**备注**：When the remaining Duration goes negative on expiry it is kept as-is (not clamped). Weapon stacks are tracked in the ability state, so different items keep separate stack counts.

**依赖图节点**：输入 `value:ability.effective_value`、`value:ability.effective_stack_limit`、`config:ItemConfig.AbilityDefs[].Effects[].rid`、`state:frame_delta_time`、`value:player.auto_target`；输出 `value:weapon.stat_stack_multiplier[Damage]`、`value:weapon.stat_stack_multiplier[CritDamage]`、`value:weapon.stat_stack_multiplier[AttackSpeed]`

**核验记录**（已写入上方字段）：

- 采纳 `code_refs`（来自 `powerups.weapon_stat_stack`）：GetWeaponStatMultiplier is at AbilityState.cs:215-222.

### `powerups.weapon_damage_multipliers`

**Per-frame ability damage multipliers**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15ed784, 0x15ef2c4, 0x15ef3cc, 0x15ef680, 0x15ef860, 0x15f2bd4, 0x15f2ce4, 0x15e8420`

```text
reset per frame: item.AbilityDamageMultiplier=1, item.AbilityDamageFlat=0, player.AbilityDamageMultiplier=1, player.AbilityDamageFlat=0. AllWeaponsDamageMultiplier: every weapon *= (1+Value). DamageForEmptySlots: owner *= (1 + Value*emptySlots). DamageForMissingHealth: each weapon (optionally filtered by WeaponType) *= (1 + missing%*Value), missing% = (1-HP/maxHP)*100. DamageForStat: b = Value*SpecificValue(SourceStat); slot item or player: Flat => AbilityDamageFlat += b else *= (1+b). TemporaryWeaponBuff(Damage): owner *= (1+bonus) while timer>0
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Value` | effective ability value | powerups.ability_effective_value |

**输出**：item/player AbilityDamageMultiplier and AbilityDamageFlat

**代码**：[Abilities/Effects/AllWeaponsDamageMultiplierEffect.cs:6-11](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/AllWeaponsDamageMultiplierEffect.cs#L6)；[Abilities/Effects/DamageForEmptySlotsEffect.cs:6-12](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/DamageForEmptySlotsEffect.cs#L6)；[Abilities/Effects/DamageForMissingHealthEffect.cs:8-15](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/DamageForMissingHealthEffect.cs#L8)；[Abilities/Effects/DamageForStatEffect.cs:11-26](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/DamageForStatEffect.cs#L11)；[Abilities/Effects/TemporaryWeaponBuffEffect.cs:11-30](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/TemporaryWeaponBuffEffect.cs#L11)；[Abilities/Systems/AbilityExecuteEventSystem.cs:36-68](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L36)

**状态依据**：Native VA comments

**配置参数**：`AbilityDef.Value`；`TemporaryWeaponBuffEffect.Duration (default 7)`；`DamageForStatEffect.SourceStat/ModifierType/SlotKey`

**使用方**：[Services/Damage/DamageCalculator.cs:140](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L140) (flat)；[Services/Damage/DamageCalculator.cs:169](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L169) (item * player multiplier)

**备注**：All factors multiply together. In DamageCalculator the flat terms add to base attack before the % stat and resist, and the multipliers apply after crit/super-crit.

**依赖图节点**：输入 `value:ability.effective_value`、`value:ability.scaled_value`、`value:player.empty_slot_count`、`value:player.current_hp`、`value:player.stat_specific[Health]`、`value:player.stat_specific`、`value:player.stat_specific[MovementSpeed]`、`constant:DamageForMissingHealthEffect.missing_health_percent_scale`、`value:player.equipped_items`、`config:ItemConfig.DamageType`、`config:ItemConfig.AbilityDefs[].Effects[].rid`、`state:frame_delta_time`；输出 `value:weapon.ability_damage_multiplier`、`value:weapon.ability_damage_flat`、`value:player.ability_damage_multiplier`、`value:player.ability_damage_flat`

### `powerups.attack_speed_multipliers`

**Ability attack-speed multipliers**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15ee470, 0x15ee360, 0x15f2ce4`

```text
item.AbilityAttackSpeedMultiplier reset to 1 per frame; AttackSpeedMultiplier: if Scaled>0: every weapon *= 1 + Scaled*(PerEquippedWeapon ? nEquippedWeapons : 1); AttackSpeedForEmptySlots: owner *= 1 + Value*emptySlots; TemporaryWeaponBuff(AttackSpeed): owner *= 1+bonus while active
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `nEquippedWeapons` | count | PlayerModel.EquippedWeapons |

**输出**：weapon attack-speed factor

**代码**：[Abilities/Effects/AttackSpeedMultiplierEffect.cs:10-18](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/AttackSpeedMultiplierEffect.cs#L10)；[Abilities/Effects/AttackSpeedForEmptySlotsEffect.cs:6-13](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/AttackSpeedForEmptySlotsEffect.cs#L6)；[Abilities/Effects/TemporaryWeaponBuffEffect.cs:32-37](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/TemporaryWeaponBuffEffect.cs#L32)

**状态依据**：Native VA comments

**配置参数**：`AbilityDef.Value`

**使用方**：[Player/PlayerAttackSystem.cs:124](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L124)；[Player/PlayerAttackSystem.cs:173](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L173)

**备注**：Attack speed is multiplied, so effective cooldown = AttackCooldown / product.

**依赖图节点**：输入 `value:ability.scaled_value`、`value:ability.effective_value`、`value:player.empty_slot_count`、`value:player.equipped_weapon_count`、`value:player.equipped_items`、`config:ItemConfig.AbilityDefs[].Effects[].rid`、`state:frame_delta_time`；输出 `value:weapon.ability_attack_speed_multiplier`

### `powerups.crit_buffs`

**Ability crit / super-crit modifiers**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15f2ce4, 0x15ed5e4, 0x15ed6b8, 0x15eef04, 0x15f06a4`

```text
TemporaryWeaponBuff(CritDamage): owner.AbilityCritMultiplier *= 1+bonus; TemporaryWeaponBuff(CritChance): owner.AbilityCritChanceBonus += bonus; AddWeaponSuperCritChance/Damage: owner.AbilitySuperCrit{Chance,Damage}Bonus += Value*S; BuffNextHitSuperCrit: player.NextHitSuperCrit{Chance,Damage}Bonus += Value (cleared after the next hit); GuaranteedCrit: state.GuaranteedCrit=true makes every hit of that weapon crit (flag persists until the state is rebuilt)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `bonus` | Value*S at proc time | TemporaryWeaponBuffEffect |

**输出**：crit chance/damage terms consumed by DamageCalculator

**代码**：[Abilities/Effects/TemporaryWeaponBuffEffect.cs:38-49](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/TemporaryWeaponBuffEffect.cs#L38)；[Abilities/Effects/AddWeaponSuperCritChanceEffect.cs:9-14](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/AddWeaponSuperCritChanceEffect.cs#L9)；[Abilities/Effects/AddWeaponSuperCritDamageEffect.cs:9-14](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/AddWeaponSuperCritDamageEffect.cs#L9)；[Abilities/Effects/BuffNextHitSuperCritEffect.cs:6-12](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/BuffNextHitSuperCritEffect.cs#L6)；[Abilities/Effects/GuaranteedCritEffect.cs:6-10](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/GuaranteedCritEffect.cs#L6)；[Services/Damage/DamageCalculator.cs:133-142](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L133)

**状态依据**：Native VA comments

**配置参数**：`AbilityDef.Value`

**使用方**：combat crit formula

**备注**：These are added to the post-curve SpecificValue crit chance, so they bypass the diminishing-returns curve in Unit/Stat.cs.

**依赖图节点**：输入 `value:ability.scaled_value`、`value:ability.effective_value`、`config:ItemConfig.AbilityDefs[].Effects[].rid`、`state:frame_delta_time`；输出 `value:weapon.ability_crit_multiplier`、`value:weapon.ability_crit_chance_bonus`、`value:weapon.ability_supercrit_bonus`、`value:player.next_hit_supercrit_bonus`

### `powerups.empower_hit`

**Empowered / next-hit damage bonuses**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15ef9c0, 0x15ef968, 0x15f0db8, 0x15f0f40, 0x15f3550, 0x15eee2c`

```text
EmpowerHit: state.EmpoweredHit += Value*S; EmpowerHitAgainstStatic (OnPreHit): += Amount if target IsStatic; consumed as damage *= (1+EmpoweredHit) once. ResetCooldown(HighestDamageWeapon): highest-DPS weapon ResetCooldown=true, NextHitEmpower = Value; ResetCooldown(Self): owner ResetCooldown=true; TriggerHighestDamageWeapon: highest-DPS weapon reset, NextHitEmpower = Value*S; consumed as damage *= (1+NextHitEmpower). BuffNextHitDamage: player.NextHitDamageBonus += Value*S, consumed as damage *= (1+bonus)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Amount` | flat empower vs static targets | EmpowerHitAgainstStaticEffect.Amount |

**输出**：one-shot damage multipliers and extra attacks

**代码**：[Abilities/Effects/EmpowerHitEffect.cs:7-11](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/EmpowerHitEffect.cs#L7)；[Abilities/Effects/EmpowerHitAgainstStaticEffect.cs:7-12](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/EmpowerHitAgainstStaticEffect.cs#L7)；[Abilities/Effects/ResetCooldownEffect.cs:7-43](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/ResetCooldownEffect.cs#L7)；[Abilities/Effects/TriggerHighestDamageWeaponEffect.cs:7-26](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/TriggerHighestDamageWeaponEffect.cs#L7)；[Abilities/Effects/BuffNextHitDamageEffect.cs:8-13](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/BuffNextHitDamageEffect.cs#L8)；[Services/Damage/DamageCalculator.cs:146-161](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L146)；[Services/Damage/DamageCalculator.cs:171-175](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L171)

**状态依据**：Native VA comments

**配置参数**：`AbilityDef.Value`；`EmpowerHitAgainstStaticEffect.Amount`；`ResetCooldownEffect.Mode`

**使用方**：combat outgoing damage；PlayerAttackSystem cooldown reset

**备注**：ResetCooldown(HighestDamageWeapon) overwrites NextHitEmpower; it does not add.

**依赖图节点**：输入 `value:ability.scaled_value`、`value:ability.effective_value`、`config:ItemConfig.AbilityDefs[].Effects[].rid`、`config:EnemySpawnerConfig.IsStatic`、`value:weapon.item_dps`、`value:player.equipped_items`；输出 `value:weapon.empowered_hit_multiplier`、`value:weapon.cooldown_reset_flag`、`value:player.next_hit_damage_bonus`、`value:unit.ability_trigger_events`

### `powerups.lightning_damage`

**Lightning proc damage**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x15f16f8 (Execute), 0x15f1ba4 (ApplyLightningToUnit), 0x15f1698 (IndependentTrigger), 0x15f1f48 (ctor), 0x15f0d6c (LightningDamageBonusEffect), 0x15f0bac (IncreaseLightningChanceEffect), 0x1571f08 (ApplyChainLightning), 0x15e9d1c (ShouldProcessForOwner)`

```text
SpawnLightningEffect (IndependentTrigger OnHit; bypasses Def conditions; source item only). Chance: p = state.GetChance() + player.BonusLightningChance; if p > 0 and Random.value >= p, no proc (p <= 0 means always). Target: target = event Target ?? player.CurrentTarget; skip if null or dead. Damage: dmg = context.Damage * DamageMultiplier * (1 + LightningDamageBonus). Empower: e = MetaStat('empowered_lightning'); empowered iff e > 0 and Random.value < e; if empowered, dmg *= 2 + MetaStat('lightning_damage'). Strike: if the target has VampireSpiritTag, ApplyChainLightning calls ApplyLightningToUnit(dmg) on every live arena spirit; otherwise ApplyLightningToUnit(target, dmg). ApplyLightningToUnit(u, dmg): dmg *= u.LightningVulnerability.DamageMultiplier if present; if dmg > 0, u.HP -= dmg, LastDamageGet = now, then ProcessEnemyDeath; then shock (IsShocked = true, ShockedTimer = 10, ConsumedDecayTimer = 10). OnLightningHit then fires immediately with the original target, Damage = pre-vulnerability dmg and Item = owner. Per-frame contributors (both reset to 0 each frame): LightningDamageBonus += GetValue() * Resolve(ValueScaling) from LightningDamageBonusEffect; BonusLightningChance += (UseAbilityValue ? GetValue() : Amount) * Resolve(ValueScaling) from IncreaseLightningChanceEffect, plus crystal HammerLightningChancePerEnchant value/100.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `DamageMultiplier` | lightning coefficient | SpawnLightningEffect.DamageMultiplier (default 1) |
| `context.Damage` | triggering hit damage: final result.Damage of the owning weapon's hit (after armor, crit, multipliers, rounding and the first-hit vulnerability) | AbilityTriggerEvent.Damage from UnitDamageSystem.FireHitTriggerEvents |
| `empowered_lightning` | empower chance | MetaStat (graph_empowered_lightning_5_3) |
| `lightning_damage` | extra empowered multiplier | MetaStat (graph_lightning_damage_6_6) |
| `LightningVulnerability.DamageMultiplier` | target lightning vulnerability (1 if absent) | BossTower/BossSpawnService.cs:311: 1.25, set only on the Lava Crab boss-tower boss (ApplyLavaCrabBehavior) |

**输出**：extra damage + 10 s shock

**代码**：[Abilities/Effects/SpawnLightningEffect.cs:26-70](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/SpawnLightningEffect.cs#L26)；[Abilities/Effects/LightningDamageBonusEffect.cs:14-18](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/LightningDamageBonusEffect.cs#L14)；[Abilities/Effects/IncreaseLightningChanceEffect.cs:18-23](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/IncreaseLightningChanceEffect.cs#L18)；[Abilities/Systems/AbilityExecuteEventSystem.cs:82-83](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L82)；[Abilities/Systems/AbilityExecuteEventSystem.cs:103](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L103)；[Abilities/Systems/AbilityExecuteEventSystem.cs:209-227](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L209)；[Unit/Systems/UnitDamageSystem.cs:301-313](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L301)；[Unit/Systems/UnitDamageSystem.cs:368-372](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L368)；[BossTower/Systems/VampireSpiritSystem.cs:148-153](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/Systems/VampireSpiritSystem.cs#L148)；[BossTower/BossSpawnService.cs:305-312](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/BossSpawnService.cs#L305)

**状态依据**：Native VA comments on SpawnLightningEffect.Execute/ApplyLightningToUnit and on both bonus effects' UpdatePassive.

**配置参数**：`SpawnLightningEffect.DamageMultiplier`；`AbilityDef.Chance`；`IncreaseLightningChanceEffect.UseAbilityValue/Amount/ValueScaling`；`LightningDamageBonusEffect.ValueScaling`；`graph_empowered_lightning_5_3`；`graph_lightning_damage_6_6`；`crystal LegendaryAbilityType.HammerLightningChancePerEnchant`；`LightningVulnerabilityComponent.DamageMultiplier (Lava Crab 1.25)`

**使用方**：combat DPS；powerups.shock；OnLightningHit-triggered abilities

**备注**：Expected multiplier per proc = DamageMultiplier*(1+LDB)*(1 + e*(1+ld)), times the target's vulnerability. No second armor pass is applied, but the base is context.Damage, the final damage of the triggering hit (OnHit event Damage = result.Damage, rounded in DamageCalculator and then multiplied by any first-hit vulnerability). Armor, crit and the multipliers are therefore inherited. It procs only on hits by the owning weapon: OnHit (2) is in the source-local trigger mask 0xC0115, so ShouldProcessForOwner requires owner == event Item. Against vampire spirits, one proc strikes every live spirit at full damage, and each spirit gets its own vulnerability, death check and shock. OnLightningHit still fires only once.

**依赖图节点**：输入 `value:player.outgoing_damage_per_hit`、`value:ability.effective_chance`、`value:player.bonus_lightning_chance`、`value:ability.scaled_value`、`config:ItemConfig.AbilityDefs[].Effects[].rid`、`value:player.meta_stat[empowered_lightning]`、`value:player.meta_stat[lightning_damage]`、`constant:SpawnLightningEffect.empowered_base_multiplier`、`state:rng`、`value:player.auto_target`、`constant:BossSpawnService.lava_crab_lightning_vulnerability`、`state:boss_tower_context`、`value:enemy.current_hp`、`constant:SpawnLightningEffect.shocked_timer_seconds`；输出 `value:enemy.lightning_damage`、`value:enemy.current_hp`、`value:unit.is_dead`、`value:enemy.shock_damage_multiplier`、`value:unit.ability_trigger_events`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `powerups.lightning_damage`）：Verified: FireHitTriggerEvents passes Damage = result.Damage (UnitDamageSystem.cs:312-313, 372), and OnHit is source-local per ShouldProcessForOwner's mask (AbilityExecuteEventSystem.cs:222-227). The note and the context.Damage variable now say this. Also noted that the first-hit vulnerability multiplier is applied after rounding.
- 合并复核修正：The expression contrasts the vampire chain with ApplyLightningToUnit ('otherwise ApplyLightningToUnit: vulnerability ... shock'), implying chained spirits skip vulnerability and shock. In fact VampireSpiritSystem.ApplyChainLightning (VampireSpiritSystem.cs:151-152) calls SpawnLightningEffect.ApplyLightningToUnit for every spirit, so each one gets the vulnerability multiplier, HP loss, death processing and the 10 s shock. The 1.25 LightningVulnerability is also described as a general 'boss' value, but the only setter is ApplyLavaCrabBehavior (BossSpawnService.cs:311), so it applies only to the Lava Crab boss-tower boss. ApplyLightningToUnit also sets LastDamageGet and runs ProcessEnemyDeath, which the entry omits.

### `powerups.shock`

**Shock consumption damage bonus**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15ef044, 0x15ee6bc, 0x15eb784`

```text
ConsumeShock: if current target is Shocked: IsShocked=false, ConsumedShockCount += 1, ConsumedDecayTimer=10 s, fire OnShockConsume. Decay: ShockedTimer counts down 10 s; ConsumedShockCount resets to 0 when ConsumedDecayTimer (paused while shocked) hits 0. Damage bonus: damage *= (1 + DamagePerConsumedShockOnTarget * ConsumedShockCount), DamagePerConsumedShockOnTarget += Value per frame (BonusDamagePerConsumedShockOnTargetEffect)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ConsumedShockCount` | per-target counter | ShockComponent |

**输出**：damage multiplier vs target

**代码**：[Abilities/Effects/ConsumeShockEffect.cs:9-23](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/ConsumeShockEffect.cs#L9)；[Abilities/Effects/BonusDamagePerConsumedShockOnTargetEffect.cs:6-10](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/BonusDamagePerConsumedShockOnTargetEffect.cs#L6)；[Abilities/Systems/ConsumedShockDecaySystem.cs:15-30](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/ConsumedShockDecaySystem.cs#L15)；[Services/Damage/DamageCalculator.cs:182-191](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L182)

**状态依据**：Native VA comments

**配置参数**：`AbilityDef.Value`

**使用方**：combat outgoing damage；powerups.value_scaler PerConsumedShockOnTarget

**依赖图节点**：输入 `value:ability.effective_value`、`value:player.auto_target`、`constant:ConsumeShockEffect.consumed_decay_seconds`、`state:frame_delta_time`、`config:ItemConfig.AbilityDefs[].Effects[].rid`；输出 `value:enemy.shock_damage_multiplier`、`value:unit.ability_trigger_events`、`value:enemy.consumed_shock_count`

### `powerups.marks`

**Mark stacks (cap, application, decay)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x15ed9b8 (Execute), 0x15ec3d4 (ApplyMark), 0x15eda38 (ApplyMarkInternal), 0x15ee214 (ProcessMarkAppliedEvents), 0x15edf3c (CalculateMaxMarks), 0x15ee300 (GetCount), 0x15f0c3c (IncreaseMarkChanceEffect), 0x15ebc08 (PiercingWeaponMarkSystem.OnUpdate), 0x15ebe40 (HasPiercingMarkAbility), 0x15eb8f0/0x15eba30 (MarkDecaySystem), 0x15e2a5c/0x15e2a74/0x15e38bc (UI max-mark count), 0x15cdf10 (ApplyDamageToPlayer evasion), 0x15ce71c (HasEquippedPierceWeapon)`

```text
Runtime cap: MaxMarks = 1 + BonusMaxMarks + sum, over every equipped item's ability states with an IncreaseMaxMarksEffect (no condition/trigger gate), of (int)(Resolve(Scaling, ScalingStat; context.Target = the mark target) * GetCount(state)). GetCount = UseAbilityValue ? (int)state.GetValue() : Count; the float-to-int conversion saturates and NaN gives 0. ApplyMarkInternal(n, allowToStayAboveMax, roll): skip if target is null or n <= 0. allowAbove = target is Hydra || allowToStayAboveMax. New mark: Stacks = allowAbove ? n : min(Max, n), MaxStacks = Max, DecayTimer = 12, AllowToStayAboveMax = allowAbove. Existing mark: MaxStacks = Max, DecayTimer = 12; if allowAbove: Stacks += n and the flag is set; else if !AllowToStayAboveMax: Stacks = min(Max, Stacks+n); else Stacks += n only if Stacks+n <= Max (otherwise unchanged). OnMarkApplied fires once per newly gained stack (newStacks - previous). Extra mark: if roll, then with p = MetaStat('extra_mark') one more mark is applied via ApplyMarkInternal(1, allowAbove, roll=false). roll is true for every external caller (ApplyMarkEffect.Execute and the ApplyMark wrapper used by the piercing and evasion paths) and false only for the extra mark itself. Decay: DecayTimer -= dt; the whole mark is removed at <= 0 (MarkPermanentMarker excluded); AllowToStayAboveMax is cleared once Stacks <= MaxStacks. Sources: (1) ApplyMarkEffect.Execute: 1 mark to the event target (nothing if the target is null), unless AppliesToAllPiercingWeapons. (2) Piercing: for each player hit with DamageType Stabbing on a live enemy, if any equipped ApplyMarkEffect has AppliesToAllPiercingWeapons, apply 1 mark with P = 0.1 + BonusMarkChance; IncreaseMarkChanceEffect adds (UseAbilityValue ? GetValue() : Amount) * Resolve(ValueScaling) per frame. (3) Evasion: on a dodge, n = (int)MetaStat('evasion_mark') marks on the attacking enemy if n > 0, the attacker is non-null and a Stabbing weapon is equipped.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `BonusMaxMarks` | crystal legendary MaxMarksPerEnchant bonus, reset each frame | Abilities/Systems/AbilityExecuteEventSystem.cs:85,101 (see powerups.crystal_rage_marks_lightning) |
| `extra_mark` | amulet meta extra-mark chance | graph_extra_mark_6_29: 0.1*L, max 3 |
| `evasion_mark` | marks applied on dodge | graph_evasion_mark_7_27: L, max 3 |
| `BonusMarkChance` | extra piercing mark chance | IncreaseMarkChanceEffect, reset each frame |

**输出**：mark stacks on enemies

**代码**：[Abilities/Effects/ApplyMarkEffect.cs:21-90](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/ApplyMarkEffect.cs#L21)；[Abilities/Effects/IncreaseMaxMarksEffect.cs:10-29](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/IncreaseMaxMarksEffect.cs#L10)；[Abilities/Effects/IncreaseMarkChanceEffect.cs:18-23](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/IncreaseMarkChanceEffect.cs#L18)；[Abilities/Systems/PiercingWeaponMarkSystem.cs:29-46](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/PiercingWeaponMarkSystem.cs#L29)；[Abilities/Systems/MarkDecaySystem.cs:8-30](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/MarkDecaySystem.cs#L8)；[Abilities/AbilityState.cs:163-183](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L163)；[Unit/Systems/UnitDamageSystem.cs:125-133](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L125)；[Unit/Systems/UnitDamageSystem.cs:382-387](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L382)

**状态依据**：Native VA comments on ApplyMarkInternal, CalculateMaxMarks, GetCount, PiercingWeaponMarkSystem.OnUpdate and MarkDecaySystem.OnUpdate.

**配置参数**：`IncreaseMaxMarksEffect.Count/UseAbilityValue/Scaling/ScalingStat`；`IncreaseMarkChanceEffect.UseAbilityValue/Amount/ValueScaling`；`ApplyMarkEffect.AppliesToAllPiercingWeapons`；`ApplyMarkEffect.PiercingWeaponHitChance (0.1)`；`MarkDecaySystem.DecayDuration (12)`；`graph_extra_mark_6_29`；`graph_evasion_mark_7_27`；`crystal LegendaryAbilityType.MaxMarksPerEnchant`

**使用方**：powerups.value_scaler PerMarksOnTarget；[Unit/Systems/UnitDamageSystem.cs:341-356](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L341) (mark_lifesteal: flat and percent lifesteal *= 1 + mark_lifesteal*stacks; Hydra flat *= 1+0.1*stacks)；OnMarkApplied-triggered abilities

**备注**：Marks have no direct damage effect. Their value comes from PerMarksOnTarget scaling, mark_lifesteal and OnMarkApplied triggers. IncreaseMaxMarks appears 15 times in data. The UI max-mark count (AbilityState.SumMaxMarksCount) differs from the runtime cap: it sums UseAbilityValue ? (int)value : Count without the Scaling resolver and without the base 1 or crystal bonus. The extra_mark roll applies to every mark source, including piercing and evasion, but never chains.

**依赖图节点**：输入 `value:player.bonus_max_marks`、`constant:ApplyMarkEffect.base_max_marks`、`value:ability.effective_value`、`value:ability.scaled_value`、`config:ItemConfig.AbilityDefs[].Effects[].rid`、`constant:ApplyMarkEffect.PiercingWeaponHitChance`、`constant:ApplyMarkEffect.mark_decay_seconds`、`value:player.meta_stat[extra_mark]`、`value:player.meta_stat[evasion_mark]`、`state:rng`、`value:unit.hit_roll_outcome`、`value:weapon.attacks_per_second`、`config:ItemConfig.DamageType`、`value:player.equipped_items`、`value:player.auto_target`、`state:boss_tower_context`、`state:frame_delta_time`；输出 `value:enemy.mark_stacks`、`value:enemy.max_marks`、`value:unit.ability_trigger_events`

**核验记录**（已写入上方字段）：

- 采纳 `code_refs`（来自 `powerups.marks`）：AbilityState.cs:163-183 holds the UI max-mark helpers (SumMaxMarksCount at 175-183), and the runtime CalculateMaxMarks is at ApplyMarkEffect.cs:73-90, which is covered by the 21-90 range.
- 合并复核修正：The expression says 'Then, if rolling, one extra mark is applied' without saying when rolling happens. In code, every external caller rolls: ApplyMarkEffect.Execute passes rollExtraMarkChance=true, and so does the ApplyMark wrapper used by the piercing and evasion paths (ApplyMarkEffect.cs:29,35). Only the recursive extra mark does not, and it carries allowAbove into the recursion (:61). So evasion and piercing marks can also gain the extra mark. The member's cross-ref to powerups.crystal_rage_marks_lightning for BonusMaxMarks was dropped. Missing caveat: CalculateMaxMarks builds its context with the mark target, so a target-dependent Scaling (for example PerMarksOnTarget) is evaluated against that target.

### `powerups.heal_effects`

**Ability heals and regen multiplier**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x15f0814 (HealPercent), 0x15f06d0 (HealFromDamage), 0x15f3694 (TriggerLifesteal), 0x15f0ae4 (HealthRegenForMissingHealth), 0x15eeeb4 (BuffNextHitLifestealFlat), 0x1455978 (PlayerRegenSystem.OnUpdate), 0x15d00cc (ApplyPlayerLifesteal)`

```text
maxHP = GetStatValue(Health); skip if dead. HealPercent: HP = min(maxHP, HP + maxHP * GetValue()). HealFromDamage: k = ScaleWithMissingHealth ? (1 - (float)(HP/GetStatSpecificValue(Health))) * 100 : 1; HP = min(maxHP, HP + k * (context.Damage * GetValue())). TriggerLifesteal: amt = LifestealFlat + maxHP * LifestealPercent; if amt > 0: HP = min(maxHP, HP + amt * GetValue()). HealthRegenForMissingHealth (per frame while active): AbilityHealthRegenMultiplier *= missing% * GetValue() + 1, missing% = (1 - HP/maxHP_specific) * 100; the multiplier is reset to 1 each frame. PlayerRegenSystem: HP = min(HP + HealthRegen * AbilityHealthRegenMultiplier * dt, maxHP). BuffNextHitLifestealFlat: v = GetValue() * Resolve(ValueScaling); if v > 0: NextHitLifestealFlatBonus += v; on the next player hit, flat lifesteal *= 1 + bonus and the bonus is reset to 0.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `maxHP` | Health stat value | runtime |
| `GetValue` | ability value incl. star override and bonuses | Abilities/AbilityState.cs:93-97 |
| `context.Damage` | triggering event damage | AbilityTriggerEvent.Damage |
| `HealthRegen` | regen per second | StatType.HealthRegen |

**输出**：player HP

**代码**：[Abilities/Effects/HealPercentEffect.cs:16-26](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/HealPercentEffect.cs#L16)；[Abilities/Effects/HealFromDamageEffect.cs:12-20](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/HealFromDamageEffect.cs#L12)；[Abilities/Effects/TriggerLifestealEffect.cs:13-24](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/TriggerLifestealEffect.cs#L13)；[Abilities/Effects/HealthRegenForMissingHealthEffect.cs:10-16](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/HealthRegenForMissingHealthEffect.cs#L10)；[Abilities/Effects/BuffNextHitLifestealFlatEffect.cs:14-20](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/BuffNextHitLifestealFlatEffect.cs#L14)；[Abilities/Systems/AbilityExecuteEventSystem.cs:74](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L74)；[Player/Systems/PlayerRegenSystem.cs:30-38](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/PlayerRegenSystem.cs#L30)；[Unit/Systems/UnitDamageSystem.cs:357-365](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L357)

**状态依据**：Each effect's Execute/UpdatePassive carries a Native VA comment. PlayerRegenSystem.OnUpdate is annotated 0x1455978.

**配置参数**：`AbilityDef.Value`；`HealFromDamageEffect.ScaleWithMissingHealth`；`BuffNextHitLifestealFlatEffect.ValueScaling/ScalingStat`；`HealPercentEffect.VfxSize`

**使用方**：combat sustain；[Player/Systems/PlayerRegenSystem.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/PlayerRegenSystem.cs) (regen tick)；[UI/Inventory/InventoryStatView.cs:64](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/Inventory/InventoryStatView.cs#L64)

**备注**：AbilityHealthRegenMultiplier has a runtime consumer: PlayerRegenSystem.OnUpdate (native 0x1455978) multiplies HealthRegen by it each frame, and InventoryStatView also displays it. The earlier open question about a missing consumer is resolved. HealFromDamage's missing-health factor is in percent units (0..100), not a fraction.

**依赖图节点**：输入 `value:player.stat_raw[Health]`、`value:player.stat_specific[Health]`、`value:player.current_hp`、`value:ability.effective_value`、`value:ability.scaled_value`、`value:player.outgoing_damage_per_hit`、`value:player.stat_raw[LifestealFlat]`、`value:player.stat_raw[LifestealPercent]`、`value:player.stat_specific[HealthRegen]`、`constant:HealFromDamageEffect.missing_health_percent_scale`、`config:ItemConfig.AbilityDefs[].Effects[].rid`、`state:frame_delta_time`、`value:unit.is_dead`；输出 `value:player.current_hp`、`value:player.ability_regen_multiplier`、`value:player.next_hit_lifesteal_bonus`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `powerups.heal_effects`）：Verified PlayerRegenSystem.cs:30-38 (annotated native 0x1455978): HP = min(HP + HealthRegen * AbilityHealthRegenMultiplier * dt, maxHP). Notes, expression and code_refs updated.

### `powerups.stun_bonus`

**Stun duration and bonus damage to stunned**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15f2a50, 0x15ee71c`

```text
Stun: current target gets BuffStun(Duration = Value). BonusDamageToStunned += Value per frame; damage *= (1 + BonusDamageToStunned) vs stunned targets
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Value` | stun seconds / bonus fraction | AbilityDef.Value |

**输出**：stun time, damage multiplier

**代码**：[Abilities/Effects/StunEffect.cs:6-14](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/StunEffect.cs#L6)；[Abilities/Effects/BonusDamageToStunnedEffect.cs:6-10](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/BonusDamageToStunnedEffect.cs#L6)；[Services/Damage/DamageCalculator.cs:176-181](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L176)

**状态依据**：Native VA comments

**配置参数**：`AbilityDef.Value`

**使用方**：combat

**依赖图节点**：输入 `value:ability.effective_value`、`value:player.auto_target`；输出 `value:enemy.stun_duration`、`value:enemy.stunned_damage_multiplier`

### `powerups.random_stat_per_distance`

**Permanent random stat per distance travelled**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15f019c, 0x15f02f4`

```text
MovedDistance += frameDelta; while MovedDistance >= Distance: MovedDistance -= Distance; if daily procs < DailyLimit: GrantStatReward(random stat in StatPool, Value) else MovedDistance=0
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Distance` | units per grant | GrantRandomStatPerDistanceEffect.Distance (default 400) |
| `DailyLimit` | grants per day | default 100 |

**输出**：permanent stat growth

**代码**：[Abilities/Effects/GrantRandomStatPerDistanceEffect.cs:12-33](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/GrantRandomStatPerDistanceEffect.cs#L12)

**状态依据**：Native VA comments

**配置参数**：`GrantRandomStatPerDistanceEffect.Distance/DailyLimit/StatPool/ModifierType/Source`；`AbilityDef.Value`

**使用方**：PlayerRewardSystem

**备注**：Max daily growth = DailyLimit * Value spread across the pool.

**依赖图节点**：输入 `value:player.move_distance`、`value:ability.effective_value`、`config:ItemConfig.AbilityDefs[].Effects[].rid`、`state:save_counters`、`state:utc_now`、`state:rng`；输出 `value:reward.granted_stat_modifier`

### `powerups.scaled_multiplier_effect`

**ScaledMultiplierEffect**

- 状态：**空壳**（`stub`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
expected: target(MultiplierTarget) *= 1 + Value*S, optionally only for weapons of WeaponType. Current body is empty
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Target` | OwnerDamage/AllWeaponsDamage/PlayerDamage/PlayerHealthRegen/OwnerAttackSpeed/AllWeaponsAttackSpeed/OwnerCritDamage | effect data |

**输出**：none (stub)

**代码**：[Abilities/Effects/ScaledMultiplierEffect.cs:12-14](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/ScaledMultiplierEffect.cs#L12)；[Abilities/Effects/MultiplierTarget.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/MultiplierTarget.cs)

**状态依据**：UpdatePassive empty; HasWeaponTypeFilter returns false

**备注**：No item asset references this effect, so there is no current gameplay impact. GrantStatOnHitEffect.Execute and TreatAsAllSlotsEmptyEffect.Execute are also empty stubs with no data references. TreatAsAllSlotsEmpty is never set true, so PerEmptySlot always uses actual empty slots.

**依赖图节点**：输入 `config:ItemConfig.AbilityDefs[].Effects`、`value:ability.effective_value`、`value:ability.scaled_value`；输出 `value:ability.stub_effect_output`

### `powerups.double_loot`

**DoubleLoot ability multiplier on enemy stat rewards**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：核验时补充
- 原生地址：`0x15ef8dc (DoubleLootEffect.Execute), 0x1456084 (GrantStatReward)`

```text
DoubleLootEffect.Execute: state.DoubleLoot = true. In GrantStatReward, if modifier.Source == Enemy: for each equipped item state with DoubleLoot: DoubleLoot=false; value *= (1 + state.GetValue()). Each flagged state is consumed once and the factors compound if several are flagged.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `state.GetValue()` | effective ability value (base tier + bonuses) | powerups.ability_effective_value |
| `Source` | stat reward source | StatModifier.Source (only ModifierSource.Enemy is eligible) |

**输出**：multiplied permanent stat reward from devour/kill

**代码**：[Abilities/Effects/DoubleLootEffect.cs:9-12](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/DoubleLootEffect.cs#L9)；[Player/Systems/PlayerRewardSystem.cs:55-63](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/PlayerRewardSystem.cs#L55)

**状态依据**：Both methods carry native ELF VA comments

**配置参数**：`AbilityDef.Value / LevelScaling of the 3 item abilities using DoubleLootEffect`

**使用方**：permanent stat growth (devour)

**备注**：Resolves the catalog's open question: the consumer exists. Ability-permanent and Ads rewards are not doubled. The VIP stat_multiplier roll is applied after this, also for Enemy source only.

**依赖图节点**：输入 `value:ability.effective_value`、`value:ability.proc_roll`、`value:ability.condition_pass`、`config:ItemConfig.AbilityDefs[].Effects`、`value:player.equipped_items`；输出 `value:ability.double_loot_multiplier`

### `powerups.ability_trigger_routing`

**Ability trigger routing to owners (source-item mask)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`config_rule`
- 来源：核验时补充
- 原生地址：`0x15e9d1c, 0x15e9450`

```text
Trigger t is processed for owner o iff o == event.Item OR t >= 20 OR bit t of 0x3feea is set. Source-item-only triggers are OnEquip(0), OnHit(2), OnCrit(4), OnSuperCrit(8), OnCooldownReset(18), OnPreHit(19). Triggers broadcast to all equipped items are OnKill, OnMove, OnTargetChange, Passive, OnPostHit, OnDevour, OnEvasion, OnPostCrit, OnGetHitLowHealth, OnBlock, OnRageLevelUp, OnEvasionProc, OnShockConsume, OnLethalDamageIgnore, plus everything >=20 (OnOneshot, OnLightningHit, OnMarkApplied).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `t` | AbilityTrigger of the event | AbilityTriggerEvent.Trigger |
| `event.Item` | weapon/item that caused the event | UnitDamageSystem.FireHitTriggerEvents (Item=data.Weapon) |

**输出**：whether an item's abilities and independent effects see the event

**代码**：[Abilities/Systems/AbilityExecuteEventSystem.cs:148-176](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L148)；[Abilities/Systems/AbilityExecuteEventSystem.cs:222-227](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L222)

**状态依据**：Comment 'Native ELF VA 0x15e9d1c; source-local trigger mask is 0xC0115.'

**配置参数**：`AbilityDef.Trigger`

**使用方**：all proc-frequency estimates (OnHit/OnCrit abilities proc only from their own weapon's hits; OnPostHit abilities proc from any weapon's hits)

**备注**：Key for the DPS model: the proc rate of an OnHit/OnCrit ability equals the owning weapon's attack rate times P, not the total attack rate.

**依赖图节点**：输入 `config:ItemConfig.AbilityDefs[].Trigger`、`config:ItemConfig.AbilityDefs[].Effects`、`value:unit.ability_trigger_events`、`value:player.equipped_items`、`value:item.unlocked_abilities`、`constant:AbilityExecuteEventSystem.broadcast_trigger_mask`、`constant:AbilityExecuteEventSystem.broadcast_trigger_min`；输出 `value:ability.trigger_routing`

### `powerups.trigger_ability_chain`

**TriggerAbilityEffect (direct chained execution)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：核验时补充
- 原生地址：`0x15f3098`

```text
On execute: target = owner.AbilityStates[TargetAbilityIndex]; for each effect e of target.Def.Effects: e.Execute(context with State=target). The target's conditions (chance, cooldown, ...) are bypassed, and NextProc bonuses of the target are NOT cleared.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `TargetAbilityIndex` | index into the owner's ACTIVE ability-state list | TriggerAbilityEffect.TargetAbilityIndex (item data, 2 instances) |

**输出**：extra execution of another ability's effects

**代码**：[Abilities/Effects/TriggerAbilityEffect.cs:9-27](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/TriggerAbilityEffect.cs#L9)

**状态依据**：Comment 'Original ELF VA 0x15f3098. Direct execution bypasses definition conditions'

**配置参数**：`TriggerAbilityEffect.TargetAbilityIndex`

**使用方**：ability proc frequency

**备注**：Indices refer to the active (unlocked) state list, so they shift when abilities unlock (see powerups.ability_unlock_gate).

**依赖图节点**：输入 `config:ItemConfig.AbilityDefs[].Effects`、`value:item.unlocked_abilities`、`value:ability.proc_roll`、`value:ability.condition_pass`；输出 `value:ability.chained_execution`

### `powerups.missing_health_scaling`

**Damage and regen scaling with missing health**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：完整性审查补充
- 原生地址：`0x15ef3cc, 0x15f0ae4`

```text
missingPct = (1 - HP / specific(Health)) * 100. DamageForMissingHealthEffect: for each equipped weapon (optionally only WeaponType X), AbilityDamageMultiplier *= 1 + missingPct * v. HealthRegenForMissingHealthEffect: AbilityHealthRegenMultiplier *= 1 + missingPct * v. Neither has a clamp. Player regen per second = HealthRegen * AbilityHealthRegenMultiplier.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `HP` | current health | PlayerUnit.CurrentHealth |
| `v` | ability value | formula powerups.ability_effective_value |

**输出**：Per-frame weapon damage and regen multipliers

**代码**：[Abilities/Effects/DamageForMissingHealthEffect.cs:14-21](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/DamageForMissingHealthEffect.cs#L14)；[Abilities/Effects/HealthRegenForMissingHealthEffect.cs:10-16](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/HealthRegenForMissingHealthEffect.cs#L10)；[Player/Systems/PlayerRegenSystem.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/PlayerRegenSystem.cs)

**状态依据**：Annotated 'Native 0x15ef3cc' / 'Native 0x15f0ae4'.

**配置参数**：`AbilityDef DamageForMissingHealthEffect (3 uses)`；`AbilityDef HealthRegenForMissingHealthEffect (1 use)`

**使用方**：powerups.weapon_damage_multipliers；powerups.heal_effects；combat.player_regen

**备注**：Merge into powerups.weapon_damage_multipliers / heal_effects if they already enumerate these effects. The expected-value models (simulation, bake) cannot represent HP-state-dependent multipliers, which is a structural gap in the model.

**依赖图节点**：输入 `value:player.current_hp`、`value:player.stat_specific[Health]`、`constant:DamageForMissingHealthEffect.missing_health_percent_scale`、`value:ability.effective_value`、`config:ItemConfig.AbilityDefs[].Effects`、`config:ItemConfig.DamageType`、`value:player.equipped_items`；输出 `value:weapon.ability_damage_multiplier`、`value:player.ability_regen_multiplier`

### `powerups.empty_slot_and_stat_scaling`

**Empty-slot and stat-proportional damage / attack-speed effects**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：完整性审查补充
- 原生地址：`0x15ef2c4, 0x15ee360, 0x15ef680, 0x15ef860`

```text
E = |{slots: unlocked and not equipped}|. DamageForEmptySlots: ownerItem.AbilityDamageMultiplier *= 1 + v * E. AttackSpeedForEmptySlots: ownerItem.AbilityAttackSpeedMultiplier *= 1 + v * E. ValueScaler 'empty slots' source: E, or the total slot count if TreatAsAllSlotsEmpty. DamageForStat: b = v * specific(SourceStat). If AffectSpecificSlot: the item in SlotKey gets AbilityDamageFlat += b (ModifierType Flat) or AbilityDamageMultiplier *= 1 + b. Otherwise the player gets AbilityDamageFlat += b or AbilityDamageMultiplier *= 1 + b.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `E` | unlocked empty item slots | PlayerModel.EmptyItemSlots |
| `v` | ability value | formula powerups.ability_effective_value |
| `SourceStat` | stat used for scaling | DamageForStatEffect.SourceStat (asset) |

**输出**：Per-frame weapon damage / attack-speed multipliers and flat ability damage

**代码**：[Abilities/Effects/DamageForEmptySlotsEffect.cs:10-16](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/DamageForEmptySlotsEffect.cs#L10)；[Abilities/Effects/AttackSpeedForEmptySlotsEffect.cs:9-16](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/AttackSpeedForEmptySlotsEffect.cs#L9)；[Abilities/Effects/DamageForStatEffect.cs:21-37](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/DamageForStatEffect.cs#L21)；[Abilities/Core/ValueScaler.cs:26](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Core/ValueScaler.cs#L26)；[Player/Models/PlayerModel.cs:130-139](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L130)

**状态依据**：Annotated with native/original ELF VAs.

**配置参数**：`AbilityDef DamageForEmptySlotsEffect (4 uses)`；`AbilityDef AttackSpeedForEmptySlotsEffect (2 uses)`；`AbilityDef DamageForStatEffect (3 uses)`；`Player Config.Slots`

**使用方**：powerups.weapon_damage_multipliers；powerups.attack_speed_multipliers；combat.outgoing_damage_to_enemy；combat.attack_speed_multiplier

**备注**：This creates a design coupling: unlocking a slot raises E (more damage) until an item is equipped there. Merge into the aggregate powerups.* entries if they already include it.

**依赖图节点**：输入 `value:player.empty_slot_count`、`value:ability.effective_value`、`value:player.stat_specific[MovementSpeed]`、`value:player.stat_specific[Health]`、`config:ItemConfig.AbilityDefs[].Effects`、`value:player.equipped_items`；输出 `value:weapon.ability_damage_multiplier`、`value:weapon.ability_attack_speed_multiplier`、`value:weapon.ability_damage_flat`、`value:player.ability_damage_multiplier`、`value:player.ability_damage_flat`

### `powerups.temporary_weapon_buff`

**Temporary weapon buff (timed damage / attack-speed / crit)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：完整性审查补充
- 原生地址：`0x15f2bd4, 0x15f2ce4, ctor 0x15f2f90`

```text
On trigger: bonus_T = ValueScaler(ValueScaling), with MovementSpeed as the default scaling stat, and timer_T = Duration (default 7 s). The new bonus replaces the old one; it does not stack. Each frame, for every T with timer_T > 0: timer_T -= dt. When it expires, bonus_T = 0. Otherwise: Damage gives owner.AbilityDamageMultiplier *= 1 + bonus. AttackSpeed gives owner.AbilityAttackSpeedMultiplier *= 1 + bonus. CritDamage gives owner.AbilityCritMultiplier *= 1 + bonus. CritChance gives owner.AbilityCritChanceBonus += bonus.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Duration` | buff duration | TemporaryWeaponBuffEffect.Duration (asset, default 7) |
| `ValueScaling` | value scaling spec | formula powerups.value_scaler |

**输出**：Timed per-weapon multipliers

**代码**：[Abilities/Effects/TemporaryWeaponBuffEffect.cs:11](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/TemporaryWeaponBuffEffect.cs#L11)；[Abilities/Effects/TemporaryWeaponBuffEffect.cs:19-55](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/TemporaryWeaponBuffEffect.cs#L19)

**状态依据**：Original ELF VA annotations on Execute/UpdatePassive/ctor.

**配置参数**：`AbilityDef TemporaryWeaponBuffEffect (3 uses): BuffType, Duration, ValueScaling`

**使用方**：powerups.weapon_damage_multipliers；powerups.attack_speed_multipliers；powerups.crit_buffs

**备注**：Uptime = min(1, Duration * procRate). Steady-state models must convert proc rate into uptime. Merge if already inside the aggregate ids.

**依赖图节点**：输入 `value:ability.scaled_value`、`config:ItemConfig.AbilityDefs[].Effects`、`value:ability.proc_roll`、`state:frame_delta_time`；输出 `value:weapon.ability_damage_multiplier`、`value:weapon.ability_attack_speed_multiplier`、`value:weapon.ability_crit_multiplier`、`value:weapon.ability_crit_chance_bonus`

### `powerups.unused_effect_stubs`

**GrantStatOnHit / TreatAsAllSlotsEmpty / ScaledMultiplier effect stubs**

- 状态：**空壳**（`stub`）
- 类型：`runtime_formula`
- 来源：完整性审查补充

```text
GrantStatOnHitEffect.Execute: intended to add a StatModifier(StatType, ModifierType, Source, value) on hit; body empty. TreatAsAllSlotsEmptyEffect.Execute: intended to set PlayerModel.TreatAsAllSlotsEmpty = true; body empty. The flag is reset every frame in AbilityExecuteEventSystem and read in ValueScaler (empty-slot count = all slots).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `TreatAsAllSlotsEmpty` | per-frame flag | PlayerModel.TreatAsAllSlotsEmpty |

**输出**：None in the current data

**代码**：[Abilities/Effects/GrantStatOnHitEffect.cs:16](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/GrantStatOnHitEffect.cs#L16)；[Abilities/Effects/TreatAsAllSlotsEmptyEffect.cs:9](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/TreatAsAllSlotsEmptyEffect.cs#L9)；[Abilities/Systems/AbilityExecuteEventSystem.cs:81](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L81)；[Abilities/Core/ValueScaler.cs:26](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Core/ValueScaler.cs#L26)；[Abilities/Effects/ScaledMultiplierEffect.cs:22](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/ScaledMultiplierEffect.cs#L22)

**状态依据**：Empty Execute/UpdatePassive bodies.

**使用方**：powerups.empty_slot_and_stat_scaling

**备注**：An asset scan (grep 'class: <Type>' over *.asset) finds 0 uses of GrantStatOnHitEffect, TreatAsAllSlotsEmptyEffect, ScaledMultiplierEffect and EmptySlotsCondition, so these stubs have zero impact on current numbers. Track them only as a risk for future content.

**依赖图节点**：输入 `config:ItemConfig.AbilityDefs[].Effects`、`value:ability.effective_value`；输出 `value:ability.stub_effect_output`

### `powerups.amulet_level_target_days`

**Amulet level target days and auto-balance multipliers (unconsumed)**

- 状态：**编辑器工具缺失**（`editor_only_missing`）
- 类型：`editor_generator`
- 来源：完整性审查补充

```text
Bake target: amulet level L should be reached at day LevelTargetDays[L-1] = [20, 26, 34, 56, 90, 130, 180]. The feature is available from FeatureUnlockDay = 16. RequiredExperiencePerLevel and the graph cost multipliers are solved so that the cumulative simulated soul XP at LevelTargetDays matches, scaled by AutoBalanceCostMultiplier (1.25) and AutoBalanceExperienceMultipliers[L] (0.8, ...).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `LevelTargetDays` | target day per amulet level | Amulet Config.LevelTargetDays |
| `FeatureUnlockDay` | design unlock day | Amulet Config.FeatureUnlockDay (16) |
| `AutoBalanceCostMultiplier` | graph cost scale | Amulet Config (1.25) |
| `AutoBalanceExperienceMultipliers` | per-level XP scale | Amulet Config |

**输出**：Amulet Config.RequiredExperiencePerLevel and UpgradeCostMultipliers

**代码**：[Amulet/AmuletConfig.cs:17-31](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletConfig.cs#L17)；[Amulet/AmuletConfig.cs:53](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletConfig.cs#L53)

**状态依据**：FeatureUnlockDay, LevelTargetDays, AutoBalanceCostMultiplier and AutoBalanceExperienceMultipliers have no consumers, and BakeAmuletAutoBalance is empty.

**由数据反推的生成规则**（拟合规则，详见 [Generation/IslandTimeline.md](../Generation/IslandTimeline.md)）：

```text
LevelTargetDays are the start days of islands 8, 9, 10, 12, 14, 16, 18 (data identity; no rule for choosing those islands found).
```

吻合情况：All values equal IslandTargetDays at those islands.

**配置参数**：`Amulet Config.LevelTargetDays`；`Amulet Config.FeatureUnlockDay`；`Amulet Config.AutoBalanceCostMultiplier`；`Amulet Config.AutoBalanceExperienceMultipliers`

**使用方**：powerups.amulet_autobalance_bake；powerups.amulet_required_xp_per_level

**备注**：The runtime unlock gate is UnlockLocationNumber (8), not FeatureUnlockDay. Merge into powerups.amulet_autobalance_bake if it already cites these fields.

**依赖图节点**：输入 `config:AmuletConfig.LevelTargetDays`、`config:AmuletConfig.FeatureUnlockDay`、`config:AmuletConfig.AutoBalanceCostMultiplier`、`config:AmuletConfig.AutoBalanceExperienceMultipliers`、`config:AmuletConfig.MaxLevel`、`config:AmuletConfig.SoulDefs[].BaseExperience`、`config:SimulationConfig.AmuletLevelBalance[].Experience`、`config:SimulationConfig.AmuletLevelBalance[].SoulRewards`、`value:sim.snapshot_at_time`；输出 `config:AmuletConfig.RequiredExperiencePerLevel`、`config:AmuletConfig.UpgradeCostMultipliers`

## 配置参数

| 资产 | 字段 | 类型 | 含义 | 用于 |
|---|---|---|---|---|
| Amulet Config.asset | `UnlockLocationNumber` | int (8) | location required to unlock the amulet | powerups.amulet_unlock_condition |
| Amulet Config.asset | `MaxLevel` | int (7) | amulet level cap | powerups.amulet_required_xp_per_level, powerups.amulet_level_from_xp, powerups.amulet_add_levels |
| Amulet Config.asset | `RequiredExperiencePerLevel` | List<double> [17000,65000,150000,700000,1700000,2800000,5000000] | XP per amulet level (index = level-2); last entry unused at MaxLevel 7 | powerups.amulet_required_xp_per_level, powerups.amulet_cumulative_xp |
| Amulet Config.asset | `SoulDefs[].BaseExperience` | double (common1, large3, elite80, huge500, great2000, divine10000, chaotic10000) | amulet XP per soul spent | powerups.amulet_upgrade_xp_gain, powerups.amulet_sim_soul_xp |
| Amulet Config.asset | `UpgradeCostMultipliers` | dict<string,float> (164 keys) | baked per-node cost scale applied before PrettyRound | powerups.amulet_upgrade_cost |
| Amulet Config.asset | `StatModifierMultipliers` | dict<string,float> (57 keys, flat stat nodes) | baked per-node stat value scale | powerups.amulet_upgrade_stat_value |
| Amulet Config.asset | `LevelTargetDays` | List<int> [20,26,34,56,90,130,180] | target play day per amulet level (auto-balance input) | powerups.amulet_autobalance_bake |
| Amulet Config.asset | `AutoBalanceCostMultiplier` | double (1.25) | auto-balance cost scale | powerups.amulet_autobalance_bake |
| Amulet Config.asset | `AutoBalanceExperienceMultipliers` | List<double> [0.8,0.8,0.85,0.9,0.95,1,1] | auto-balance XP scale per level | powerups.amulet_autobalance_bake |
| Amulet Config.asset | `FeatureUnlockDay` | int (16) | design day of amulet unlock (no runtime reader) | powerups.amulet_autobalance_bake |
| Amulet Config.asset | `PassDoubleSoulChance` | float (0.25) | Soul Pass chance to double spawner soul drops | Enemy/Commands/GrantSpawnerSoulRewardCommandHandler.cs:34 |
| Amulet Config.asset | `PassDailyBossTickets / PassDailyLegendarySouls / PassOneTimeDivineSouls / PassDurationDays` | int (3 / 1 / 1 / 30) | Soul Pass rewards that feed amulet souls | SoulPass/Commands/SendSoulPassDailyMailCommandHandler.cs, Rewards/Commands/GrantRewardCommandRunner.cs:130 |
| Amulet Upgrade Config.asset | `Upgrades[].RequiredAmuletLevel / RequiredUpgradeKeys` | int / List<string> | graph topology and amulet-level gates for 298 nodes | powerups.amulet_node_availability |
| Amulet Upgrade Config.asset | `Seed, TargetEdgeLength, MinimumNodeDistance, LayoutIterations, InitialLayoutJitter, PathThickness, PathTileScale` | layout | UI graph layout only (no gameplay impact) |  |
| AmuletUpgradeDefsBuilder (code) | `WithMaxLevel / WithCost lambdas / WithStatModifier/WithModifier lambdas / WithSimulationSettings` | code constants | per-node max level, cost curve shape, value curve shape, simulation hints | powerups.amulet_upgrade_cost, powerups.amulet_upgrade_stat_value, powerups.amulet_meta_modifiers, powerups.amulet_autobalance_bake |
| item assets (e.g. cut legendary.asset, buck_*_shield.asset) | `AbilityDef.Value / Chance / StackLimit / Trigger / ConditionMode / UnlockLevel` | float/int/enum | base ability numbers | powerups.ability_tier_override, powerups.ability_effective_value |
| item assets | `AbilityDef.LevelScaling[] {Level, ValueOverride, ChanceOverride, StacksOverride} + AffectsValue/Chance/Stacks` | list | per-star ability tier overrides (88 assets) | powerups.ability_tier_override |
| item assets | `AddRageEffect.Amount` | int (2,4,5,8,12) | rage points per hit | powerups.add_rage_effect, powerups.rage_gain |
| cut legendary.asset | `GrantRageAccessEffect.Level` | int (1,2,3) | rage level cap granted | powerups.rage_effective_max |
| item assets | `SpawnLightningEffect.DamageMultiplier` | float (default 1) | lightning damage coefficient | powerups.lightning_damage |
| item assets | `TemporaryWeaponBuffEffect.Duration / BuffType` | float (default 7) / enum | temporary weapon buff | powerups.weapon_damage_multipliers, powerups.attack_speed_multipliers, powerups.crit_buffs |
| item assets | `WeaponStatStackEffect.Duration / BuffType / ResetOnTargetChange` | float/enum/bool | stacking weapon buff | powerups.weapon_stat_stack |
| item assets | `CooldownCondition.Cooldown / ScaleByValue; HitsCountCondition.RequiredHits; HealthThresholdCondition.Threshold; CombatCondition.Timeout (5)` | float/int | ability proc gating | powerups.ability_gating_conditions |
| item assets | `AddStatEffect.StatType/ModifierType/Source/Duration/ValueScaling/UseDailyProcLimit/DailyProcLimit` | various | stat grant parameters | powerups.add_stat_effect |
| item assets | `IncreaseMaxMarksEffect.Count/UseAbilityValue/Scaling` | int/bool/enum | mark cap | powerups.marks |
| item assets | `GrantRandomStatPerDistanceEffect.Distance (400) / DailyLimit (100) / StatPool` | float/int/list | distance-based permanent stats | powerups.random_stat_per_distance |
| code constants | `RageModel.Thresholds [30,100,270,270], DecayTimeout 10` | float[]/float | rage curve and idle decay | powerups.rage_thresholds, powerups.rage_decay |
| code constants | `MarkDecaySystem.DecayDuration 12; ApplyMarkEffect.PiercingWeaponHitChance 0.1; ConsumedShockDecaySystem 10/10` | float | mark/shock timers and base chance | powerups.marks, powerups.shock |
| ABTestData (EnemyHpDamageMult, CommonRewardMultipliers, RewardedDelayFromStart, NoAds) | `rewardedValueMultiplier (0.5), rewardedStatValueMultiplier, delayMinutes, delayMinutesPerUsdSpent, adCooldown, maxAdsPerDay` | double/ValueByLocation/float | rewarded-ad stat reward scaling and pacing | powerups.rewarded_ad_stat_reward |

## 枚举

| 枚举 | 文件 | 取值 |
|---|---|---|
| `StatType` | [Unit/StatType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatType.cs) | 0=MovementSpeed,1=Health,2=HealthRegen,3=Damage,4=AttackRange,5=AttackCooldown,6=StabDamageFlat,7=StabDamagePercent,8=CutDamageFlat,9=CutDamagePercent,10=MagicDamageFlat,11=MagicDamagePercent,12=BuckshotDamageFlat,13=BuckshotDamagePercent,14=StabDefenceFlat,15=StabDefencePercent,16=CutDefenceFlat,17=CutDefencePercent,18=MagicDefenceFlat,19=MagicDefencePercent,20=BuckshotDefenceFlat,21=BuckshotDefencePercent,22=LifestealFlat,23=Evasion,24=CritChance,25=CritDamagePercent,26=UniversalDefenceFlat,27=UniversalDefencePercent,28=SuperCritChance,29=SuperCritDamagePercent,30=GlobalAttack,31=GlobalDefence,32=LifestealPercent,33=BlockChance,34=BlockAmount,35=VisualScaleMultiplier |
| `ModifierType` | [Unit/ModifierType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/ModifierType.cs) | 0=Flat,1=PercentAdditive,2=PercentMultiplicative,3=PostFormulaAdditive,4=PreAdditivePostMultiplicative,5=PostFormulaMultiplicative,6=PostFormulaFlat,7=EventPercentModifier |
| `SoulKeys` | [Amulet/SoulKeys.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/SoulKeys.cs) | common=Common,large=Uncommon,elite=Rare,huge=Epic,great=Legendary,divine=Divine,chaotic=Chaotic (currency 'soul.<key>') |
| `AbilityTrigger` | [Abilities/AbilityTrigger.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityTrigger.cs) | 0=OnEquip,1=OnKill,2=OnHit,3=OnMove,4=OnCrit,5=OnTargetChange,6=Passive,7=OnPostHit,8=OnSuperCrit,9=OnDevour,10=OnEvasion,11=OnPostCrit,12=OnGetHitLowHealth,13=OnBlock,14=OnRageLevelUp,15=OnEvasionProc,16=OnShockConsume,17=OnLethalDamageIgnore,18=OnCooldownReset,19=OnPreHit,20=OnOneshot,21=OnLightningHit,22=OnMarkApplied |
| `AbilityValueScaling` | [Abilities/Core/AbilityValueScaling.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Core/AbilityValueScaling.cs) | 0=None,1=PerMissingHealthPercent,2=PerHealthAbovePercent,3=PerMarksOnTarget,4=PerRageLevel,5=PerEmptySlot,6=PerConsumedShockOnTarget,7=PerHitsTaken,8=PerStat |
| `WeaponBuffType` | [Abilities/Effects/WeaponBuffType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/WeaponBuffType.cs) | 0=Damage,1=AttackSpeed,2=CritDamage,3=CritChance |
| `MultiplierTarget` | [Abilities/Effects/MultiplierTarget.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/MultiplierTarget.cs) | 0=OwnerDamage,1=AllWeaponsDamage,2=PlayerDamage,3=PlayerHealthRegen,4=OwnerAttackSpeed,5=AllWeaponsAttackSpeed,6=OwnerCritDamage |
| `ResetCooldownMode` | [Abilities/Effects/ResetCooldownMode.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/ResetCooldownMode.cs) | 0=Self,1=HighestDamageWeapon |
| `LethalIgnoreMode` | [Abilities/Effects/LethalIgnoreMode.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/LethalIgnoreMode.cs) | 0=Chance,1=PerTarget |
| `ConditionMode` | [Abilities/Core/ConditionMode.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Core/ConditionMode.cs) | 0=All,1=Any |

## 未解决问题

1. DoubleLootEffect sets AbilityState.DoubleLoot=true (3 data instances), but nothing in the reconstruction reads it; the comment says PlayerRewardSystem consumes it. Is the doubling of stat rewards missing from PlayerRewardSystem?
2. PlayerModel.AbilityHealthRegenMultiplier (HealthRegenForMissingHealthEffect) is only read by UI/Inventory/InventoryStatView.cs:64. The runtime player regen tick that should multiply by it is not found.
3. HammerRegenSystem is a full stub (hammer_regen meta = 0.001L from graph_hammer_regen_3_3). What are the original formula and its weapon condition?
4. ScaledMultiplierEffect, GrantStatOnHitEffect and TreatAsAllSlotsEmptyEffect are stubs. No current asset uses them, but the original bodies are unknown.
5. AmuletConfig.BakeAmuletAutoBalance and the static AutoBalanceStatTypes set are missing. The algorithm that maps LevelTargetDays, AutoBalance* and per-node SimulationSettings to UpgradeCostMultipliers/StatModifierMultipliers needs reconstructing before amulet numbers can be re-tuned consistently.
6. RageModel (thresholds 30/100/270/270, 10 s decay, TryAddRage), RageExtraHealthSystem, RageExtraHitSystem and AddRageEffect have no native-address annotations and should be verified against the binary.
7. RequiredExperiencePerLevel has 7 entries but only 6 are reachable at MaxLevel=7. Was a level 8 planned, or is the index offset (level-2) intentional?
8. AmuletModel.TryFillWithSoul checks for 1 huge soul but does not spend it. The comment says this is native behavior; confirm it is intended design.
9. RageExtraHitSystem picks the weapon by item.Damage, while ResetCooldown and TriggerHighestDamageWeapon pick by DPS. Confirm this difference against native code.
