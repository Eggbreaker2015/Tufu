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
| [`powerups.amulet_souls_drop_unlock`](#powerupsamulet_souls_drop_unlock) | Soul drop unlock per spawner | 原生证据恢复 | `config_rule` |
| [`powerups.amulet_autobalance_bake`](#powerupsamulet_autobalance_bake) | Amulet auto-balance bake (cost/stat multipliers) | 编辑器工具缺失 | `editor_generator` |
| [`powerups.rage_thresholds`](#powerupsrage_thresholds) | Rage level thresholds | 重建（无原生标注） | `lookup_table` |
| [`powerups.rage_gain`](#powerupsrage_gain) | Rage accumulation and level-up | 重建（无原生标注） | `runtime_formula` |
| [`powerups.rage_effective_max`](#powerupsrage_effective_max) | Rage max accessible level | 原生证据恢复 | `runtime_formula` |
| [`powerups.rage_decay`](#powerupsrage_decay) | Rage decay / reset | 重建（无原生标注） | `runtime_formula` |
| [`powerups.add_rage_effect`](#powerupsadd_rage_effect) | AddRageEffect proc | 重建（无原生标注） | `runtime_formula` |
| [`powerups.rage_extra_health`](#powerupsrage_extra_health) | Rage max-health bonus | 重建（无原生标注） | `runtime_formula` |
| [`powerups.rage_damage_reduce`](#powerupsrage_damage_reduce) | Rage incoming damage reduction | 原生证据恢复 | `runtime_formula` |
| [`powerups.rage_extra_hit`](#powerupsrage_extra_hit) | Rage-to-extra-attack conversion | 重建（无原生标注） | `runtime_formula` |
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
| [`powerups.thorns`](#powerupsthorns) | Thorns reflection | 原生证据恢复 | `runtime_formula` |
| [`powerups.lethal_ignore`](#powerupslethal_ignore) | Lethal damage ignore | 原生证据恢复 | `runtime_formula` |
| [`powerups.stun_bonus`](#powerupsstun_bonus) | Stun duration and bonus damage to stunned | 原生证据恢复 | `runtime_formula` |
| [`powerups.random_stat_per_distance`](#powerupsrandom_stat_per_distance) | Permanent random stat per distance travelled | 原生证据恢复 | `runtime_formula` |
| [`powerups.scaled_multiplier_effect`](#powerupsscaled_multiplier_effect) | ScaledMultiplierEffect | 空壳 | `runtime_formula` |
| [`powerups.hammer_regen`](#powerupshammer_regen) | Hammer regen (amulet keystone) | 空壳 | `runtime_formula` |
| [`powerups.rewarded_ad_stat_reward`](#powerupsrewarded_ad_stat_reward) | Rewarded-ad permanent stat amount | 原生证据恢复 | `runtime_formula` |
| [`powerups.double_loot`](#powerupsdouble_loot) | DoubleLoot ability multiplier on enemy stat rewards | 原生证据恢复 | `runtime_formula` |
| [`powerups.ability_trigger_routing`](#powerupsability_trigger_routing) | Ability trigger routing to owners (source-item mask) | 原生证据恢复 | `config_rule` |
| [`powerups.trigger_ability_chain`](#powerupstrigger_ability_chain) | TriggerAbilityEffect (direct chained execution) | 原生证据恢复 | `runtime_formula` |
| [`powerups.ability_unlock_gate`](#powerupsability_unlock_gate) | Item ability unlock by evolution and level | 重建（无原生标注） | `config_rule` |
| [`powerups.missing_health_scaling`](#powerupsmissing_health_scaling) | Damage and regen scaling with missing health | 原生证据恢复 | `runtime_formula` |
| [`powerups.empty_slot_and_stat_scaling`](#powerupsempty_slot_and_stat_scaling) | Empty-slot and stat-proportional damage / attack-speed effects | 原生证据恢复 | `runtime_formula` |
| [`powerups.temporary_weapon_buff`](#powerupstemporary_weapon_buff) | Temporary weapon buff (timed damage / attack-speed / crit) | 原生证据恢复 | `runtime_formula` |
| [`powerups.unused_effect_stubs`](#powerupsunused_effect_stubs) | GrantStatOnHit / TreatAsAllSlotsEmpty / ScaledMultiplier effect stubs | 空壳 | `runtime_formula` |
| [`powerups.amulet_level_target_days`](#powerupsamulet_level_target_days) | Amulet level target days and auto-balance multipliers (unconsumed) | 编辑器工具缺失 | `editor_generator` |

## 公式

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

### `powerups.amulet_fill_with_soul`

**Amulet activation cost**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`config_rule`
- 来源：子系统梳理
- 原生地址：`0x15b4374`

```text
if empty_amulet >= 1 AND soul.huge >= 1: empty_amulet -= 1; IsUnlocked = true (the huge soul is checked but NOT spent)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `empty_amulet` | currency count | CurrencyModel 'empty_amulet' |
| `soul.huge` | Epic soul count | CurrencyModel 'soul.huge' |

**输出**：amulet unlocked

**代码**：[Amulet/AmuletModel.cs:166-173](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L166)

**状态依据**：Comment 'Native ELF VA 0x15b4374: huge soul is checked; only one empty_amulet is spent.'

**使用方**：Amulet UI flow

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression`：if empty_amulet > 0 AND soul.huge >= 1: TrySpend(empty_amulet,1) (return value ignored); Unlock(). The huge soul is checked but not spent.  
  依据：[Amulet/AmuletModel.cs:169](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L169) 'if (EmptyAmuletCount <= 0.0 || !HasEnoughSouls("huge",1.0)) return false;'

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

### `powerups.amulet_level_from_xp`

**Amulet level from total XP**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15b4960`

```text
Level(XP) = 0 if XP<0; else max L in [1,MaxLevel] with XP >= XPcum(L). LevelProgress = IsMax ? 1 : (XP - XPcum(Level)) / req(Level+1)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `XP` | AmuletUd.TotalExperience | runtime save |
| `XPcum` | cumulative thresholds | powerups.amulet_cumulative_xp |

**输出**：amulet Level (gates graph nodes)

**代码**：[Amulet/AmuletModel.cs:219-233](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L219)；[Amulet/AmuletModel.cs:107](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L107)；[Amulet/AmuletModel.cs:117](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L117)；[Amulet/AmuletModel.cs:139](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L139)；[Amulet/AmuletModel.cs:149](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L149)

**状态依据**：GetLevelForExperience has comment 'Native ELF VA 0x15b4960'. The progress getters are unannotated but trivial.

**配置参数**：`Amulet Config.RequiredExperiencePerLevel`；`Amulet Config.MaxLevel`

**使用方**：powerups.amulet_node_availability；AmuletLevelUpSystem analytics

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression`：LevelProgress = IsMax ? 1 : (req(Level+1) > 0 ? (XP - XPcum(Level)) / req(Level+1) : 0)  
  依据：[Amulet/AmuletModel.cs:139](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L139)

### `powerups.amulet_upgrade_xp_gain`

**Amulet XP from paid upgrades**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15b46a4, 0x15bae84, 0x15b2518, 0x15b26e8`

```text
on paid TryUpgrade: XP += BaseExperience[s] * soulCost_s, where soulCost_s = first cost entry with key 'soul.*' and amount>0; only if IsUnlocked; free upgrades give 0. BaseExperience default 1.0 if soul key not found
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `BaseExperience[s]` | XP per soul | Amulet Config.SoulDefs: common=1, large=3, elite=80, huge=500, great=2000, divine=10000, chaotic=10000 |
| `soulCost_s` | evaluated soul cost of the bought level | powerups.amulet_upgrade_cost |

**输出**：TotalExperience increment

**代码**：[Amulet/AmuletModel.cs:189-193](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L189)；[Amulet/Upgrades/AmuletUpgradesModel.cs:194-203](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradesModel.cs#L194)；[Amulet/Upgrades/AmuletUpgradesModel.cs:210-223](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradesModel.cs#L210)；[Amulet/AmuletConfig.cs:57-68](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletConfig.cs#L57)

**状态依据**：Native VA comments on AddUpgradeExperience, TryUpgrade, GetSoulDef, GetBaseExperience

**配置参数**：`Amulet Config.SoulDefs[].BaseExperience`

**使用方**：powerups.amulet_level_from_xp

**备注**：Only the first soul cost entry counts. On dual-cost keystone nodes (divine for L<3, chaotic at L3) the zero-amount entry is skipped. Paying the full tree (see amulet_upgrade_cost) yields about 41.6M XP against 5.43M needed for L7, so most XP comes from early cheap nodes.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：Add: TryUpgrade never checks AmuletModel.IsUnlocked. GetLevelForExperience(0)=1, so the 27 nodes with RequiredAmuletLevel=1 can be bought (and their stats applied) before the amulet is unlocked. Those purchases give 0 XP, because AddUpgradeExperience requires IsUnlocked.  
  依据：[Amulet/Upgrades/AmuletUpgradesModel.cs:174-181](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradesModel.cs#L174),194-203; [Amulet/AmuletModel.cs:192](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L192),219-233; Amulet Upgrade Config.asset has 27 nodes with RequiredAmuletLevel: 1.

### `powerups.amulet_sim_soul_xp`

**Amulet XP from simulated soul income**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`simulation`
- 来源：子系统梳理
- 原生地址：`0x15b4514`

```text
AddSouls(s, a, isSimulation=true): soul.s += a; XP += (BaseExperience[s] ?? 1.0) * a  (non-simulation AddSouls adds no XP)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `a` | souls granted | runtime / simulation |

**输出**：TotalExperience increment in simulation

**代码**：[Amulet/AmuletModel.cs:180-187](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L180)

**状态依据**：Comment 'Original ELF VA 0x15b4514'

**配置参数**：`Amulet Config.SoulDefs`

**使用方**：Simulation/forecast reward path

**备注**：Simulation credits XP on soul gain, but live play credits it only on spend. Forecasts therefore run ahead of real amulet level whenever the player hoards souls.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `used_by`：No caller in the reconstruction. Every AddSouls call site passes isSimulation=false or omits it: GrantRewardCommandRunner.cs:154,172, ShopLotModel.cs:246, GrantSpawnerSoulRewardCommandHandler.cs:37. The caller was probably the stripped editor simulation.  
  依据：grep for AddSouls/isSimulation across Assets/Scripts finds no call that passes true.
- [待人工合入] `notes`：The isSimulation branch credits XP on soul gain, while the live path credits it on spend. No in-build simulation calls this branch, so any divergence comes only from the missing editor-side forecast. The divergence is unconfirmed at runtime.  
  依据：[Amulet/AmuletModel.cs:186](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L186); no callers with true.

### `powerups.amulet_add_levels`

**Grant amulet levels**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15b4884`

```text
if amount>=1 AND IsUnlocked AND !IsMaxLevel: XP = XPcum(min(Level+amount, MaxLevel))
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `amount` | levels to grant | reward/caller |

**输出**：TotalExperience reset to cumulative threshold

**代码**：[Amulet/AmuletModel.cs:210-217](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L210)

**状态依据**：Comment 'Native ELF VA 0x15b4884, including native clamp and cumulative experience reset'

**配置参数**：`Amulet Config.MaxLevel`

**使用方**：rewards

**备注**：Partial progress inside the current level is discarded.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `used_by`：No caller of AmuletModel.AddLevels exists in the reconstruction (dead or cheat path).  
  依据：grep 'AddLevels(' finds only the definition at [Amulet/AmuletModel.cs:210](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/AmuletModel.cs#L210).

### `powerups.amulet_upgrade_cost`

**Amulet graph node cost per level**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`Builder: 0x1582d04.. (628 native lambdas 0x15b63f4..); Populate 0x15b9bd0; PrettyRound 0x15c4c28; UpgradeModel 0x148a848..0x148d2d4`

```text
cost_{key,cur}(L) = PrettyRound( c_{key,cur}(L) * CM[key] ), CM default 1. PrettyRound(n) = round(n) if n<10; else s=10^(floor(log10 n)-1), d=floor(n/(10s)), step = s if d<=1, 5s if d<=4, else 10s; result = round(n/step)*step (banker's rounding). c(L) forms used: L, L^1.2, L^1.3, L^1.5, L^2, L^3, L^5, 2L, 5L, 10L, 15L, 5L+10, 6L, 50L, 100L, 300L, 90L-80, 10L^2, 50L^2, 100L^2, constants (1,2,3,4,5,6,10,20,30,40). Keystones use divine=L for L<3 and chaotic=1 at L=3
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `L` | level being bought (cur+1) | runtime |
| `c(L)` | native per-level cost lambda | AmuletUpgradeDefsBuilder Native_* delegates |
| `CM[key]` | baked cost multiplier | Amulet Config.UpgradeCostMultipliers (164 keys, e.g. graph_health_flat_1_2=29.37, graph_health_flat_7_24=14249.86) |

**输出**：list of (currency 'soul.<key>', amount)

**代码**：[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:12-1950](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L12)；[Amulet/Upgrades/AmuletUpgradesModel.cs:81-94](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradesModel.cs#L81)；[Meta/Upgrades/UpgradeDef.cs:214-220](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeDef.cs#L214)；[Meta/Upgrades/UpgradeModel.cs:62-66](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeModel.cs#L62)；[Utilities/DoubleExtensions.cs:73-83](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Utilities/DoubleExtensions.cs#L73)

**状态依据**：Builder header says it was generated from original ARM64 constants and 628 scalar delegates. Populate and PrettyRound carry native VA comments.

**配置参数**：`Amulet Config.UpgradeCostMultipliers`

**使用方**：powerups.amulet_upgrade_xp_gain；powerups.amulet_node_availability (CanAfford)

**备注**：Full-tree totals with current data: common ~12.14M, large ~3.28M, elite ~84.6k, huge ~22.1k, great 616, divine 39, chaotic 19. The CM values are baked auto-balance output (see powerups.amulet_autobalance_bake); the lambdas are the shape.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression`：cost_{key,cur}(L) = PrettyRound( PrettyRound(c_{key,cur}(L)) * float32(CM[key]) ). The lambda is rounded first, then multiplied, then rounded again. PrettyRound truncates to int, so non-integer shapes become integers before scaling: L^1.5 at L=2 is 2.83, then 3, then 3*CM. Same for L^1.2 and L^1.3.  
  依据：[Meta/Upgrades/UpgradeCostDef.cs:17-20](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeCostDef.cs#L17) 'return AmountByLevel(level).PrettyRound();' and [Meta/Upgrades/UpgradeDef.cs:218](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeDef.cs#L218) '(cost.Evaluate(level) * CostMultiplier).PrettyRound()'.
- [待人工合入] `notes`：Recomputed with the double PrettyRound: common 12,143,385; large 3,284,760; elite 84,435; huge 22,058; great 616; divine 39; chaotic 19. Full-tree XP is 41,593,465. The '84.6k' elite figure (84,635) and common 12,142,485 come from the single-round formula.  
  依据：Script that parses all 298 builder defs plus Amulet Config.asset UpgradeCostMultipliers (164 keys), evaluating the native lambdas with UpgradeCostDef.Evaluate + UpgradeDef.EvaluateCosts.
- [待人工合入] `code_refs`：Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:12-1951 (definitions), Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:1958-3214 (Native_* lambdas), Meta/Upgrades/UpgradeCostDef.cs:17-20  
  依据：The file is 3214 lines. The lambda bodies start at line 1958 ('Native_15b63f4').

### `powerups.amulet_upgrade_stat_value`

**Amulet graph combat-stat modifier value**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`Builder lambdas 0x15b63f4..; UpgradeModel 0x148a848..0x148d2d4`

```text
v_{key}(L) = f_key(L) * SM[key] (SM default 1), applied as StatModifier(StatType, ModifierType, v, Source=GraphUpgrade) at the CURRENT level only (not cumulative over levels). f families: flat Health/Regen/Stab/Cut/Buckshot dmg & def/LifestealFlat = L (type Flat, SM 0.86..608735); %damage/%defence/Health% = 0.01L (type PreAdditivePostMultiplicative=4); HealthRegen% and Lifesteal% = 0.02L (type 4, SM=1); MovementSpeed = 0.03L (PostFormulaMultiplicative=5); Evasion, CritChance, BlockChance, BlockAmount = 0.01L (PostFormulaFlat=6); CritDamage = 0.1L (6); SuperCritChance = 0.05L (6); SuperCritDamage = 0.2L (6)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `L` | node level 1..MaxLevel(1..25) | AmuletUd.UpgradeLevels |
| `SM[key]` | baked stat multiplier | Amulet Config.StatModifierMultipliers (57 keys, flat-stat nodes only) |

**输出**：StatModifier on PlayerUnit stats

**代码**：[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:12-1950](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L12)；[Meta/Upgrades/UpgradeDef.cs:84-89](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeDef.cs#L84)；[Meta/Upgrades/UpgradeStatModifierDef.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeStatModifierDef.cs)；[Meta/Upgrades/UpgradeModel.cs:136-146](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeModel.cs#L136)

**状态依据**：Builder generated from native delegates; UpgradeModel header cites original ARM64 range

**配置参数**：`Amulet Config.StatModifierMultipliers`

**使用方**：powerups.amulet_stat_apply；[Unit/Stat.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs) aggregation；simulation

**备注**：Full-tree totals (sum of f(max)*SM): Health flat 13,590,641; Health% 0.8; HealthRegen flat 81,482 and % 1.2; Stab(pierce) dmg flat 599,434 / % 0.8; Cut dmg flat 331,882 / % 0.8; Buckshot(smash) dmg flat 493,089 / % 0.6; Stab def flat 366,721 / % 0.6; Cut def flat 704,288 / % 0.7; Buckshot def flat 466,991 / % 0.8; Lifesteal flat 79,376 / % 1.6; MoveSpeed post-mult 0.18; Evasion/Crit/Block chance/Block amount +0.06 post-flat each; CritDmg +1.0; SuperCrit chance +0.1; SuperCrit dmg +0.4. Key naming: pierce=Stab(6/7/14/15), smash=Buckshot(12/13/20/21).

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `code_refs`：Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:12-1951, Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:1958-3214, Meta/Upgrades/UpgradeStatModifierDef.cs:25-27 (Evaluate = ValueByLevel(L) * ValueMultiplier)  
  依据：The lambdas live after line 1958. UpgradeStatModifierDef.Evaluate multiplies by ValueMultiplier (default 1f). The totals were verified by script: Health flat 13,590,641; all % and post-flat totals match the catalog.

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

### `powerups.amulet_meta_modifiers`

**Amulet graph meta-stat modifier values**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`lookup_table`
- 来源：子系统梳理
- 原生地址：`Builder lambdas 0x15b63f4..; e.g. crystal_timer_on_kill_reduce lambda is ((L*60)*715827883)>>33 = 5L`

```text
MetaStat[key](param) += f(L) at current level (ModifierType Flat unless noted). Combat keystones (max 3): rage_extra_health=0.05L, rage_damage_reduce=0.05L, rage_extra_hit=180-30L (150/120/90), mark_lifesteal=0.05L, extra_mark=0.1L, empowered_lightning=0.1L, lightning_damage=0.5L, block_to_supercrit=0.03L, evasion_mark=L, hammer_regen=0.001L. Economy/progression: economy_item_abilities_chance=0.01L or 0.02L; souls_rarity_amount[soul]=L (one node 2L); souls_drop_unlock[soul]=1; feature_unlock[...]=1; stat_drop[island]=0.2L (max5 => +1.0); map_spawner_timer_reduce[island]=7200 (s); weak_spawner_extra_range[island]=L; items_rarity_drop_amount[r]=L; items_rarity_charges[r]=L; items_rarity_timer[r]=2L-1; chest_level=L; tower_damage=0.05L; tower_damage_reduction=0.05L; tower_boss_weakness=0.15L; boss_tower_tickets=L; boss_tower_items_drop=0.1L; boss_tower_vikings=0.7L; boss_tower_lich=0.1L; boss_tower_vampire=0.05L; boss_tower_berserker=0.05L; boss_tower_crab=2L; boss_tower_hydra=L; event_golem_damage=0.5; crystal_sell_price_bonus=0.05L; crystal_enchant_efficiency=0.01L (type4); crystal_timer_on_kill_reduce=5L; crystal_guarantee_enchant=100; free_chest=7; other crystal_* = L
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `L` | node level | AmuletUd.UpgradeLevels |

**输出**：MetaStat values read by other subsystems

**代码**：[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:392](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L392)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:686](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L686)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:910](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L910)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:1698](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L1698)；[Meta/Upgrades/UpgradeModel.cs:120-135](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/Upgrades/UpgradeModel.cs#L120)

**状态依据**：Generated from native lambdas/constants

**配置参数**：`Amulet Config.UpgradeCostMultipliers`

**使用方**：powerups.rage_extra_health；powerups.rage_damage_reduce；powerups.rage_extra_hit；powerups.marks；powerups.lightning_damage；powerups.economy_proc_bonus；[Unit/Systems/UnitDamageSystem.cs:120](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L120) (block_to_supercrit)；[Unit/Systems/UnitDamageSystem.cs:130](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L130) (evasion_mark)；[Unit/Systems/UnitDamageSystem.cs:346](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L346) (mark_lifesteal)；[Meta/SoulDropAmountCalculator.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SoulDropAmountCalculator.cs)；[Meta/SpawnerTimerCalculator.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SpawnerTimerCalculator.cs)；[Meta/SpawnerChargesCalculator.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SpawnerChargesCalculator.cs)；[Player/PlayerAttackSystem.cs:81](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L81)；[Shop/ChestsModel.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs)；[Quests/Commands/ClaimQuestStepRewardCommandHandler.cs:120](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/Commands/ClaimQuestStepRewardCommandHandler.cs#L120)；[Services/Damage/DamageCalculator.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs) (tower_damage)

**备注**：Meta keys have no StatModifierMultipliers, so values are exactly the lambdas. Cost multipliers still apply.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression`：... free_chest['chest_1']: weekly node 3_14 = 7, daily node 3_15 = 0 (constant 0.0). Keys the catalog omits: boss_tower_unlock[berserker/vampire_king/lich]=1; tower_drop_legend_per_floor[boss]=1 (node 7_58 sets all six); event_golem_extra_range=1 plus stat_drop[param -1]=1 on node 4_47; loadout_button=1; feature_unlock[crystals_*]=1; crystal_enchant_extra_attempts[param]=L. Both event_golem_damage nodes are the constant 0.5.  
  依据：[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:464-476](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L464) (free_chest weekly 7.0d, daily 0.0d). The full meta-modifier dump was produced by evaluating every WithModifier lambda.

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

### `powerups.amulet_souls_drop_unlock`

**Soul drop unlock per spawner**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`config_rule`
- 来源：子系统梳理
- 原生地址：`0x15ba79c`

```text
soulDropUnlocked(spawner) = SoulAlwaysAvailable OR (IsStatic ? MetaStat('feature_unlock')['static_souls_drop'] : MetaStat('souls_drop_unlock')[SoulReward]) > 0
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `SoulReward` | spawner's soul key | EnemySpawnerConfig.SoulReward |
| `SoulAlwaysAvailable,IsStatic` | spawner flags | EnemySpawnerConfig |

**输出**：bool (whether kills drop souls)

**代码**：[Amulet/Upgrades/AmuletUpgradesModel.cs:137-151](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradesModel.cs#L137)

**状态依据**：Comment 'Original ELF VA 0x15ba79c'

**配置参数**：`EnemySpawner.SoulReward`；`EnemySpawner.SoulAlwaysAvailable`；`EnemySpawner.IsStatic`

**使用方**：enemy soul reward flow

**备注**：Unlock nodes: common (1_1, 1 huge), large (1_17, 2 huge), elite (2_1, 2 huge), static/trees (1_9, 1 chaotic).

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

### `powerups.rage_effective_max`

**Rage max accessible level**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15efa6c, 0x15efb00`

```text
MaxAccessibleLevel = max over equipped items/abilities of GrantRageAccessEffect.Level (0 if none); EffectiveMaxLevel = MaxAccessibleLevel + BossMaxLevelBonus (1 while Berserker boss active, else 0); ActiveRageLevel = EffectiveMaxLevel>0 ? RageLevel : 0; SyncToWeaponAccess clamps RageLevel to EffectiveMax
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `GrantRageAccessEffect.Level` | rage cap granted by item ability | item asset (cut legendary.asset: 1,2,3) |
| `BossMaxLevelBonus` | boss fight bonus | BossTower/Systems/BerserkerBossSystem.cs:73,84 |

**输出**：EffectiveMaxLevel, ActiveRageLevel

**代码**：[Abilities/Effects/GrantRageAccessEffect.cs:13-23](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/GrantRageAccessEffect.cs#L13)；[Rage/RageModel.cs:113-133](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rage/RageModel.cs#L113)；[Rage/RageModel.cs:200-220](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rage/RageModel.cs#L200)

**状态依据**：GrantRageAccessEffect.RecalculateMaxLevel has native VA; RageModel getters are unannotated

**配置参数**：`GrantRageAccessEffect.Level`

**使用方**：powerups.rage_gain；powerups.rage_extra_health；powerups.rage_damage_reduce；powerups.crystal_rage_marks_lightning

**备注**：IndependentTrigger = OnEquip (0), so it is recalculated on equip events.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：Recalculated on OnEquip events for the item being (un)equipped (OnEquip is source-item-only). SyncToWeaponAccess clamps only when EffectiveMax>0. If access drops to 0, RageLevel is kept but ActiveRageLevel reads 0. RageModel.SetMaxAccessibleLevel (max-only) is unused; the recalculation assigns MaxAccessibleLevel directly.  
  依据：[Rage/RageModel.cs:200-214](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rage/RageModel.cs#L200); [Abilities/Effects/GrantRageAccessEffect.cs:23-33](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/GrantRageAccessEffect.cs#L23)

### `powerups.rage_decay`

**Rage decay / reset**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x141fff8 (ResetRage), 0x141fd0c (ClearRage), 0x15f1028`

```text
each frame (if EffectiveMax>0 and (Points>0 or Level>0)): TimeSinceLastRage += dt; if >= 10 s: Points=0, Level=0. ResetRage() (ResetRageOnLethalEffect) zeroes points and level but not the timer. DecreaseLevel(n): Level=max(0,Level-n), Points=0 (Vikings boss debuff)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `DecayTimeout` | seconds without rage gain | constant 10 |

**输出**：rage reset

**代码**：[Rage/RageModel.cs:18](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rage/RageModel.cs#L18)；[Rage/RageModel.cs:141-150](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rage/RageModel.cs#L141)；[Rage/RageModel.cs:185-198](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rage/RageModel.cs#L185)；[Rage/RageModel.cs:222-227](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rage/RageModel.cs#L222)；[Abilities/Effects/ResetRageOnLethalEffect.cs:9](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/ResetRageOnLethalEffect.cs#L9)；[Abilities/Systems/AbilityExecuteEventSystem.cs:49](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L49)

**状态依据**：Tick has no annotation, although ResetRage and ClearRage do

**使用方**：[BossTower/Systems/VikingsRageDebuffSystem.cs:49](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/Systems/VikingsRageDebuffSystem.cs#L49)

**备注**：Decay is all-or-nothing after 10 s idle, not gradual.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression`：DecreaseLevel(n): if Level<=0 return (Points untouched); else Level=max(0,Level-n), Points=0. It is called with n=1 when a Viking dies.  
  依据：[Rage/RageModel.cs:192-198](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rage/RageModel.cs#L192); [BossTower/Systems/VikingsRageDebuffSystem.cs:47-50](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/Systems/VikingsRageDebuffSystem.cs#L47)

### `powerups.add_rage_effect`

**AddRageEffect proc**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
on every player hit (IndependentTrigger=OnHit, bypasses ability conditions): if MaxAccessibleLevel>0: TryAddRage(Amount); on level-up: RageExtraHealthSystem.ForceApply(); fire OnRageLevelUp trigger and RageLevelUpEvent
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Amount` | rage per hit | AddRageEffect.Amount (int, default 1; data 2/4/5/8/12) |

**输出**：rage points

**代码**：[Abilities/Effects/AddRageEffect.cs:6-26](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/AddRageEffect.cs#L6)；[Abilities/Systems/AbilityExecuteEventSystem.cs:182-193](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L182)

**状态依据**：No native annotation in AddRageEffect

**配置参数**：`AddRageEffect.Amount`

**使用方**：powerups.rage_gain

**备注**：Rage per second is about Amount * hits/sec of the owning weapon. Only cut (slashing) item assets carry AddRageEffect (cut uncommon/rare/epic/legendary).

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `code_refs`：Abilities/Effects/AddRageEffect.cs:16-39, Abilities/Systems/AbilityExecuteEventSystem.cs:209-227 (ProcessIndependentEffects + ShouldProcessForOwner)  
  依据：The Execute body is at AddRageEffect.cs:27-39. The independent-trigger dispatch is at AbilityExecuteEventSystem.cs:209-220.
- [待人工合入] `expression`：on every hit by the OWNING weapon only. OnHit (trigger 2) is not in the cross-owner mask 0x3feea, so ShouldProcessForOwner passes only when owner == data.Item. Ability conditions are bypassed. If MaxAccessibleLevel>0 then TryAddRage(Amount); on level-up: ForceApply, OnRageLevelUp, RageLevelUpEvent.  
  依据：[Abilities/Systems/AbilityExecuteEventSystem.cs:160](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L160),222-227; [Unit/Systems/UnitDamageSystem.cs:372](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L372) (OnHit carries Item=data.Weapon).

### `powerups.rage_extra_health`

**Rage max-health bonus**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
Health modifier (PercentAdditive, Source=GraphUpgrade) = rage_extra_health * ActiveRageLevel if both >0, else removed; re-applied when value changes by >= 1e-4
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `rage_extra_health` | amulet meta stat | powerups.amulet_meta_modifiers (0.05L, L<=3) |
| `ActiveRageLevel` | rage level | powerups.rage_effective_max |

**输出**：+% max Health

**代码**：[Player/Systems/RageExtraHealthSystem.cs:35-70](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/RageExtraHealthSystem.cs#L35)

**状态依据**：No native annotation

**配置参数**：`graph_rage_extra_health_4_21`

**使用方**：combat HP

**备注**：Max +0.15*3 = +45% Health PercentAdditive at rage 3 (+60% at 4 in the Berserker fight).

### `powerups.rage_damage_reduce`

**Rage incoming damage reduction**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x140a4cc (DamageCalculator.Calculate)`

```text
if incoming>0 AND ActiveRageLevel>0 AND r>0: incoming = incoming / (r * ActiveRageLevel + 1)  where r = MetaStat('rage_damage_reduce')
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `r` | amulet meta stat | powerups.amulet_meta_modifiers (0.05L) |

**输出**：incoming damage to player

**代码**：[Services/Damage/DamageCalculator.cs:105-110](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L105)

**状态依据**：Inside Calculate, annotated 'Native ELF VA 0x140a4cc'

**配置参数**：`graph_rage_damage_reduce_5_7`

**使用方**：combat incoming damage

**备注**：Defined inside the combat DamageCalculator; listed here because it is a pure rage effect. Applied after block, before event/island/global-defence multipliers. Max /1.45 at rage 3.

### `powerups.rage_extra_hit`

**Rage-to-extra-attack conversion**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
for each RageGainedEvent(a>0) with T = MetaStat('rage_extra_hit') > 0: A += a; while A >= T: A -= T; reset cooldown of the equipped weapon with the highest item.Damage (fires OnCooldownReset)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `T` | rage per free attack | powerups.amulet_meta_modifiers (180-30L = 150/120/90) |
| `a` | rage gained | AddRageEffect.Amount |

**输出**：extra immediate weapon attacks

**代码**：[Player/Systems/RageExtraHitSystem.cs:26-59](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/RageExtraHitSystem.cs#L26)

**状态依据**：No native annotation

**配置参数**：`graph_rage_extra_hit_7_21`

**使用方**：Player/PlayerAttackSystem (ResetCooldown)

**备注**：Picks the weapon by Damage, not DPS. ResetCooldownEffect and TriggerHighestDamageWeaponEffect pick by DPS. Accumulator A is not reset by rage decay.

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

### `powerups.ability_tier_override`

**Ability tier (star) override**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15e306c, 0x15e2440, 0x15e3120, 0x15e2bc0, 0x15e27c0, 0x15e360c`

```text
Stars = ItemModel.IsPreDivine ? 0 : ItemModel.Level. base_X = Affects_X ? X_override of LevelScaling entry with greatest Level <= Stars (fallback Def.X) : Def.X, for X in {Value, Chance, StackLimit}; StackLimit truncated to int. NextStar_X uses smallest Level > Stars; a negative override falls back to the current tier
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Def.Value,Def.Chance,Def.StackLimit` | base ability numbers | item asset AbilityDef |
| `LevelScaling[]` | {Level, ValueOverride, ChanceOverride, StacksOverride} | item asset AbilityDef.LevelScaling (88 assets) |
| `AffectsValue/Chance/Stacks` | whether stars change X | item asset AbilityDef |

**输出**：base Value/Chance/StackLimit

**代码**：[Abilities/AbilityState.cs:36](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L36)；[Abilities/AbilityState.cs:60-64](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L60)；[Abilities/AbilityState.cs:89-113](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L89)；[Abilities/AbilityState.cs:133-160](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L133)

**状态依据**：Native VA comments on every method

**配置参数**：`AbilityDef.Value`；`AbilityDef.Chance`；`AbilityDef.StackLimit`；`AbilityDef.LevelScaling`；`AbilityDef.Affects*`

**使用方**：all ability effects

**备注**：Ability power is a step function of item level (stars). It is not interpolated. Pre-divine items always use tier 0.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `code_refs`：Abilities/AbilityState.cs:71 (Stars), 99-103, 133-137, 151-155, 185-198 (GetCurrentOverride), 200-213 (GetNextOverride)  
  依据：Line numbers taken from the file.

### `powerups.ability_effective_value`

**Effective ability value/chance/stack limit**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15e30fc, 0x15e3368, 0x15e3874, 0x15e3898`

```text
Value = base_Value + BonusValue + NextProcBonusValue; Chance = base_Chance + BonusChance + NextProcBonusChance; StackLimit = base_Stack + BonusStackLimit + NextProcBonusStackLimit. CalculateScaledValue(x) = (Def.Value ~ 0) ? x : (base_Value / Def.Value) * x
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Bonus*` | persistent boosts recomputed per frame/trigger | powerups.ability_boosts |
| `NextProcBonus*` | one-shot boosts, cleared after the ability executes | powerups.ability_boosts |

**输出**：Value, Chance, StackLimit

**代码**：[Abilities/AbilityState.cs:55-70](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L55)；[Abilities/AbilityState.cs:84-103](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L84)；[Abilities/Systems/AbilityExecuteEventSystem.cs:212-221](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L212)

**状态依据**：Native VA comments

**使用方**：all ability effects/conditions

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `code_refs`：Abilities/AbilityState.cs:93-110 (GetValue, CalculateScaledValue), 127-131 (GetChance), 145-149 (GetStackLimit), Abilities/Systems/AbilityExecuteEventSystem.cs:242-251 (NextProc* cleared after ExecuteEffects)  
  依据：Line numbers taken from the file.

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

### `powerups.ability_boosts`

**Ability-to-ability boosts**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15eec80, 0x15ee924, 0x15eeac8, 0x15e8420, 0x15e89a0, 0x15ea284`

```text
BoostAbilityValue: target.BonusValue (or NextProcBonusValue if BoostNextProcOnly) += Scaled * (IsPercent ? target.base_Value : 1); BoostAbilityChance: target.BonusChance += Scaled; BoostAbilityStackLimit: target.BonusStackLimit += (int)Scaled. Per frame: Bonus* reset to 0, boost pass runs before regular passives; on a trigger the target's Bonus* are recomputed from boosters (passive boosters only if their conditions hold)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `TargetAbilityIndex` | index into owner's AbilityStates | effect data |

**输出**：Bonus fields of other AbilityStates

**代码**：[Abilities/Effects/BoostAbilityValueEffect.cs:12-32](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/BoostAbilityValueEffect.cs#L12)；[Abilities/Effects/BoostAbilityChanceEffect.cs:11-30](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/BoostAbilityChanceEffect.cs#L11)；[Abilities/Effects/BoostAbilityStackLimitEffect.cs:11-32](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/BoostAbilityStackLimitEffect.cs#L11)；[Abilities/Systems/AbilityExecuteEventSystem.cs:36-55](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L36)；[Abilities/Systems/AbilityExecuteEventSystem.cs:89-118](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L89)；[Abilities/Systems/AbilityExecuteEventSystem.cs:153-169](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L153)

**状态依据**：Native VA comments

**配置参数**：`BoostAbility*Effect.TargetAbilityIndex`；`IsPercent`；`BoostNextProcOnly`

**使用方**：powerups.ability_effective_value

**备注**：Used by 8 BoostAbilityValue, 3 Chance and 1 StackLimit effects in item data.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `code_refs`：Abilities/Systems/AbilityExecuteEventSystem.cs:52-87 (per-frame reset), 110-140 (boost pass then regular pass), 178-194 (RecomputeBoostsForTrigger)  
  依据：Line numbers taken from the file.

### `powerups.economy_proc_bonus`

**Economy ability proc-chance bonus**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15e997c, 0x15ea664, 0x15e21a0`

```text
if ability IsEconomy (has ChanceCondition AND an AddStatEffect with Source=AbilityPermanent): BonusChance += MetaStat('economy_item_abilities_chance') when its trigger fires
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `economy_item_abilities_chance` | amulet/crystal meta stat | powerups.amulet_meta_modifiers (0.01L / 0.02L), Crystals/CrystalModel.cs:1202 |

**输出**：additive proc chance

**代码**：[Abilities/Systems/AbilityExecuteEventSystem.cs:129](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L129)；[Abilities/Systems/AbilityExecuteEventSystem.cs:142](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L142)；[Abilities/Systems/AbilityExecuteEventSystem.cs:291-301](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L291)；[Abilities/AbilityDescriptionContext.cs:109-128](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityDescriptionContext.cs#L109)

**状态依据**：Native VA comments

**配置参数**：`graph_economy_item_abilities_chance_3_13/_5_40/_7_28`

**使用方**：powerups.chance_condition；permanent stat growth from item procs

**备注**：Increases the rate of permanent stat gain from item procs (AddStatEffect to PlayerRewardSystem).

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `code_refs`：Abilities/Systems/AbilityExecuteEventSystem.cs:153 (compute), 166 (state.BonusChance += economyBonus after RecomputeBoostsForTrigger), 330-341; Abilities/AbilityDescriptionContext.cs:109-120  
  依据：Line numbers taken from the file.

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

### `powerups.ability_gating_conditions`

**Ability cooldown/hit-count/stack/threshold conditions**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`config_rule`
- 来源：子系统梳理
- 原生地址：`0x15f4728, 0x15f49e4, 0x15f4b60, 0x15f47f8, 0x15f43e8, 0x15f4788, 0x15f5130, 0x15e920c`

```text
Cooldown: fires iff state.Cooldown<=0, then Cooldown = ScaleByValue ? Value : CooldownCondition.Cooldown; Cooldown -= dt every frame. HitsCount: fires iff HitsCounter >= (ScaleFromStackLimit ? StackLimit : CalculateScaledValue(RequiredHits)), then HitsCounter=0 (HitsCounter++ for every equipped ability state on every player hit). Stacks(RequiresStacks): Stacks++; fires iff Stacks>=StackLimit (optional reset). HealthThreshold: ratio=HP/maxHP; Below: ratio<=thr; Above: ratio>=thr; thr = Chance | Value | Threshold. Combat: inCombat = time - max(LastDamageGet, LastDamageDealt) < Timeout(5 s). EmptySlots >= Min; WeaponSet count >= RequiredCount; WeaponType/WearingItem/Target*/BossArena: boolean gates. Def.ConditionMode All/Any
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `CooldownCondition.Cooldown` | seconds | item data |
| `RequiredHits` | hit count | item data |
| `Threshold` | HP fraction | item data |
| `Timeout` | combat window | CombatCondition.Timeout default 5 |

**输出**：bool

**代码**：[Abilities/Conditions/CooldownCondition.cs:8-14](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/CooldownCondition.cs#L8)；[Abilities/Conditions/HitsCountCondition.cs:8-15](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/HitsCountCondition.cs#L8)；[Abilities/Conditions/StacksCondition.cs:8-20](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/StacksCondition.cs#L8)；[Abilities/Conditions/HealthThresholdCondition.cs:12-22](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/HealthThresholdCondition.cs#L12)；[Abilities/Conditions/CombatCondition.cs:8-14](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/CombatCondition.cs#L8)；[Abilities/Conditions/EmptySlotsCondition.cs:8-11](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/EmptySlotsCondition.cs#L8)；[Abilities/Conditions/WeaponSetCondition.cs:8-13](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/WeaponSetCondition.cs#L8)；[Abilities/Systems/AbilityExecuteEventSystem.cs:52](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L52)；[Abilities/Systems/AbilityExecuteEventSystem.cs:200-211](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L200)；[Services/Damage/DamageCalculator.cs:168-169](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L168)

**状态依据**：Native VA comments on each Evaluate

**配置参数**：`CooldownCondition.Cooldown`；`HitsCountCondition.RequiredHits`；`HealthThresholdCondition.Threshold`；`CombatCondition.Timeout`；`EmptySlotsCondition.MinEmptySlots`；`WeaponSetCondition.RequiredCount`

**使用方**：ability execution frequency

**备注**：Proc frequency is about min(attack rate * P, 1/cooldown); HitsCount procs every ceil(N) hits of any equipped weapon.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression`：The state.Cooldown -= dt tick runs in ResetPerFrameState for every equipped item state (AbilityExecuteEventSystem.cs:70), not in the condition. Stacks has two modes: NotReachedMax fires iff Stacks < StackLimit, with no increment; RequiresStacks does Stacks++ and fires iff Stacks >= StackLimit, optionally resetting to 0. Combat: result = inCombat XOR (Mode != 0), so Mode 1 fires out of combat. HealthThreshold Subject can be Player or Target (event target, else current target). HitsCounter++ happens on every outgoing player hit for every equipped item's states (DamageCalculator.cs:167-168).  
  依据：[Abilities/Conditions/StacksCondition.cs:16-24](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/StacksCondition.cs#L16); [Abilities/Conditions/CombatCondition.cs:17-18](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/CombatCondition.cs#L17); [Abilities/Conditions/HealthThresholdCondition.cs:34-39](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/HealthThresholdCondition.cs#L34); [Abilities/Systems/AbilityExecuteEventSystem.cs:70](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs#L70)
- [待人工合入] `code_refs`：Abilities/Systems/AbilityExecuteEventSystem.cs:70, Abilities/Systems/AbilityExecuteEventSystem.cs:229-240  
  依据：EvaluateConditions is at 229-240; the cooldown tick is at line 70.

### `powerups.add_stat_effect`

**AddStatEffect (equip / temporary / permanent stat)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15ec76c, 0x15f3ab4`

```text
v = Value * S. Trigger OnEquip: add/remove modifier(StatType, ModType, v, Source) for as long as the item is equipped. Duration>0: temporary modifier v for Duration s (replaces duplicate). Else (permanent reward, unless DisablePermanentStatsGain and daily limit not hit): GrantStatReward(v * KillStatRewardMultiplier), with KillStatRewardMultiplier = (Trigger==OnKill && Target) ? Target.SpawnerStatRewardMultiplier : 1
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `DailyProcLimit` | max procs per day when UseDailyProcLimit | AddStatEffect.DailyProcLimit (default 1) |
| `SpawnerStatRewardMultiplier` | zero-cooldown stacked kill copies | Enemy/EnemySpawner.cs:1186 |

**输出**：stat modifier / permanent stat growth

**代码**：[Abilities/Effects/AddStatEffect.cs:14-33](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/AddStatEffect.cs#L14)；[Abilities/Core/AbilityContext.cs:23](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Core/AbilityContext.cs#L23)

**状态依据**：Native VA comments

**配置参数**：`AddStatEffect.StatType/ModifierType/Source/Duration/ValueScaling/UseDailyProcLimit/DailyProcLimit`；`AbilityDef.Value`

**使用方**：PlayerRewardSystem.GrantStatReward (devour growth)；combat stats

**备注**：This is the most common effect in data (72 instances). On-kill permanent-stat procs are the item-driven part of permanent growth.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression`：OnEquip: add/remove modifier(v, Duration). Otherwise the daily-proc check (UseDailyProcLimit) runs FIRST for both remaining branches. If Duration>0: AddTemporaryModifierCommand(v, Duration, RemoveDuplicates). Else if !DisablePermanentStatsGain: GrantStatReward(v * KillStatRewardMultiplier). Inside GrantStatReward: value *= cheats.DropMultiplier; DoubleLoot and VIP multipliers apply only when Source==Enemy, so AbilityPermanent procs are never doubled.  
  依据：[Abilities/Effects/AddStatEffect.cs:42-49](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/AddStatEffect.cs#L42); [Player/Systems/PlayerRewardSystem.cs:54-73](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/PlayerRewardSystem.cs#L54)
- [待人工合入] `code_refs`：Abilities/Effects/AddStatEffect.cs:31-50, Abilities/Core/AbilityContext.cs:33  
  依据：KillStatRewardMultiplier is at AbilityContext.cs:33.

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

### `powerups.weapon_stat_stack`

**Weapon stat stack multiplier**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15f3880, 0x15f39d0, 0x15e39f8`

```text
on proc: Stacks = min(StackLimit, Stacks+1) (reset if target changed), Duration = D; per frame Duration -= dt, Stacks=0 on expiry. Multiplier(buffType) = (Value_eff)*Stacks + 1, applied in DamageCalculator for Damage and CritDamage
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `D` | stack duration | WeaponStatStackEffect.Duration |

**输出**：per-weapon damage/crit multiplier

**代码**：[Abilities/Effects/WeaponStatStackEffect.cs:10-21](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/WeaponStatStackEffect.cs#L10)；[Abilities/AbilityState.cs:161-168](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L161)；[Services/Damage/DamageCalculator.cs:145-152](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L145)

**状态依据**：Native VA comments

**配置参数**：`WeaponStatStackEffect.BuffType/Duration/ResetOnTargetChange`

**使用方**：combat outgoing damage

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `code_refs`：Abilities/AbilityState.cs:215-222  
  依据：GetWeaponStatMultiplier is at 215-222.

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

### `powerups.lightning_damage`

**Lightning proc damage**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15f16f8, 0x15f1ba4, 0x15f0d6c, 0x15f0bac`

```text
on hit (IndependentTrigger OnHit): p = Chance_eff + BonusLightningChance; if p>0 and rand>=p: no proc (p<=0 means always). dmg = hitDamage * DamageMultiplier * (1 + LightningDamageBonus); empowered with probability e=MetaStat('empowered_lightning'): dmg *= (2 + MetaStat('lightning_damage')); dmg *= target LightningVulnerability.DamageMultiplier; target HP -= dmg; target becomes Shocked for 10 s. LightningDamageBonus += Value*S (LightningDamageBonusEffect); BonusLightningChance += (UseAbilityValue ? Value : Amount) * S (IncreaseLightningChanceEffect) + crystal v/100
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `DamageMultiplier` | lightning coefficient | SpawnLightningEffect.DamageMultiplier (default 1) |
| `hitDamage` | triggering hit damage | AbilityTriggerEvent.Damage |

**输出**：extra damage + shock

**代码**：[Abilities/Effects/SpawnLightningEffect.cs:8-47](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/SpawnLightningEffect.cs#L8)；[Abilities/Effects/LightningDamageBonusEffect.cs:8-12](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/LightningDamageBonusEffect.cs#L8)；[Abilities/Effects/IncreaseLightningChanceEffect.cs:10-15](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/IncreaseLightningChanceEffect.cs#L10)

**状态依据**：Native VA comments

**配置参数**：`SpawnLightningEffect.DamageMultiplier`；`AbilityDef.Chance`；`graph_empowered_lightning_5_3`；`graph_lightning_damage_6_6`

**使用方**：combat DPS；powerups.shock

**备注**：Expected multiplier per proc = DamageMultiplier*(1+LDB)*(1 + e*(1+ld)). Lightning damage is not mitigated by armor in this path.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：No second armor pass is applied, but the base is context.Damage, the already-mitigated and rounded damage of the triggering hit (OnHit event Damage=result.Damage). Armor, crit and the multipliers are therefore inherited. It procs only on hits by the owning weapon (OnHit is source-item-only).  
  依据：[Unit/Systems/UnitDamageSystem.cs:312-313](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L312),372; [Abilities/Effects/SpawnLightningEffect.cs:37](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/SpawnLightningEffect.cs#L37)

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

### `powerups.marks`

**Mark stacks (cap, application, decay)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15ed9b8, 0x15eda38, 0x15edf3c, 0x15ee300, 0x15f0c3c, 0x15ebc08, 0x15eba30`

```text
MaxMarks = 1 + BonusMaxMarks + sum over equipped IncreaseMaxMarksEffect of (int)(S(Scaling) * (UseAbilityValue ? (int)Value : Count)). Apply n: new mark stacks = min(Max, n) (Hydra: uncapped); existing: stacks = min(Max, stacks+n) unless allow-above; DecayTimer=12 s (whole mark removed on expiry). After each application, an extra mark is added with p = MetaStat('extra_mark'). Piercing (Stabbing) hits with an AppliesToAllPiercingWeapons owner: P(mark) = 0.1 + BonusMarkChance (IncreaseMarkChanceEffect adds (Value|Amount)*S). Evasion: dodge applies MetaStat('evasion_mark') marks if a pierce weapon is equipped
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `BonusMaxMarks` | crystal legendary bonus | powerups.crystal_rage_marks_lightning |
| `extra_mark` | amulet meta | 0.1L |

**输出**：mark stacks on enemies

**代码**：[Abilities/Effects/ApplyMarkEffect.cs:6-69](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/ApplyMarkEffect.cs#L6)；[Abilities/Effects/IncreaseMaxMarksEffect.cs:10-21](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/IncreaseMaxMarksEffect.cs#L10)；[Abilities/Effects/IncreaseMarkChanceEffect.cs:10-15](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/IncreaseMarkChanceEffect.cs#L10)；[Abilities/Systems/PiercingWeaponMarkSystem.cs:15-31](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/PiercingWeaponMarkSystem.cs#L15)；[Abilities/Systems/MarkDecaySystem.cs:14-22](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Systems/MarkDecaySystem.cs#L14)；[Abilities/AbilityState.cs:114-132](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/AbilityState.cs#L114)

**状态依据**：Native VA comments

**配置参数**：`IncreaseMaxMarksEffect.Count/UseAbilityValue/Scaling`；`ApplyMarkEffect.AppliesToAllPiercingWeapons`；`graph_extra_mark_6_29`；`graph_evasion_mark_7_27`

**使用方**：powerups.value_scaler PerMarksOnTarget；[Unit/Systems/UnitDamageSystem.cs:335-356](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L335) (mark_lifesteal: lifesteal *= 1 + mark_lifesteal*stacks; Hydra flat *= 1+0.1*stacks)

**备注**：Marks have no direct damage effect. Their value comes from PerMarksOnTarget scaling and mark_lifesteal. IncreaseMaxMarks appears 15 times in data.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `code_refs`：Abilities/AbilityState.cs:163-183 (UI max-mark count), Abilities/Effects/ApplyMarkEffect.cs:73-90 (runtime CalculateMaxMarks)  
  依据：SumMaxMarksCount is at 175-183.

### `powerups.heal_effects`

**Ability heals and regen multiplier**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15f0814, 0x15f06d0, 0x15f3694, 0x15f0ae4, 0x15eeeb4`

```text
HealPercent: HP = min(maxHP, HP + maxHP*Value). HealFromDamage: HP = min(maxHP, HP + k*damage*Value), k = ScaleWithMissingHealth ? (1-HP/maxHP)*100 : 1. TriggerLifesteal: amt = LifestealFlat + maxHP*LifestealPercent; if >0: HP = min(maxHP, HP + amt*Value). HealthRegenForMissingHealth: player.AbilityHealthRegenMultiplier *= 1 + missing%*Value. BuffNextHitLifestealFlat: NextHitLifestealFlatBonus += Value*S (if >0); next lifesteal flat *= (1+bonus)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `maxHP` | Health stat value | runtime |

**输出**：player HP

**代码**：[Abilities/Effects/HealPercentEffect.cs:7-17](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/HealPercentEffect.cs#L7)；[Abilities/Effects/HealFromDamageEffect.cs:7-15](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/HealFromDamageEffect.cs#L7)；[Abilities/Effects/TriggerLifestealEffect.cs:6-17](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/TriggerLifestealEffect.cs#L6)；[Abilities/Effects/HealthRegenForMissingHealthEffect.cs:6-12](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/HealthRegenForMissingHealthEffect.cs#L6)；[Abilities/Effects/BuffNextHitLifestealFlatEffect.cs:8-14](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/BuffNextHitLifestealFlatEffect.cs#L8)；[Unit/Systems/UnitDamageSystem.cs:357-362](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L357)

**状态依据**：Native VA comments

**配置参数**：`AbilityDef.Value`；`HealFromDamageEffect.ScaleWithMissingHealth`

**使用方**：combat sustain

**备注**：AbilityHealthRegenMultiplier is only read by UI/Inventory/InventoryStatView.cs:64 in this reconstruction. Its runtime regen consumer is missing (see open questions).

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：The runtime consumer exists: PlayerRegenSystem.OnUpdate does HP = min(HP + HealthRegen * AbilityHealthRegenMultiplier * dt, maxHP), annotated native 0x1455978. The open question is resolved.  
  依据：[Player/Systems/PlayerRegenSystem.cs:32-36](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/PlayerRegenSystem.cs#L32)

### `powerups.thorns`

**Thorns reflection**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15f2fa0, 0x15eef6c, 0x15ce604`

```text
per frame ThornsDamage += SpecificValue(DamageSourceStat) * SpecificValue(DamageMultiplierStat) * Value; on every enemy hit on the player (including dodge/block): attacker HP -= ThornsDamage * (1 + NextThornsDamageBonus) (bonus consumed; BuffNextThornsDamage adds Value)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `DamageSourceStat,DamageMultiplierStat` | stats combined | ThornsEffect fields |

**输出**：reflected damage

**代码**：[Abilities/Effects/ThornsEffect.cs:8-14](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/ThornsEffect.cs#L8)；[Abilities/Effects/BuffNextThornsDamageEffect.cs:6-10](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/BuffNextThornsDamageEffect.cs#L6)；[Unit/Systems/UnitDamageSystem.cs:124](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L124)；[Unit/Systems/UnitDamageSystem.cs:212-226](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L212)

**状态依据**：Native VA comments

**配置参数**：`ThornsEffect.DamageSourceStat`；`ThornsEffect.DamageMultiplierStat`；`AbilityDef.Value`

**使用方**：combat

**备注**：Unmitigated by enemy armor.

### `powerups.lethal_ignore`

**Lethal damage ignore**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15f0ce4, 0x15ce89c`

```text
per frame (passive): LethalIgnoreCooldown = state.Cooldown; if state.Cooldown<=0: LethalIgnoreChance += Chance_eff; PerTarget mode sets HasPerTargetLethalIgnore. On lethal hit: PerTarget: ignore once per attacker id if cooldown<=0; else ignore with probability LethalIgnoreChance
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Chance_eff` | ability chance | powerups.ability_effective_value |

**输出**：survive lethal hit

**代码**：[Abilities/Effects/LethalIgnorePassiveEffect.cs:8-15](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/LethalIgnorePassiveEffect.cs#L8)；[Unit/Systems/UnitDamageSystem.cs:176-193](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L176)

**状态依据**：Native VA comments

**配置参数**：`AbilityDef.Chance`；`LethalIgnorePassiveEffect.Mode`；`CooldownCondition.Cooldown`

**使用方**：combat survival / time-to-kill

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

### `powerups.hammer_regen`

**Hammer regen (amulet keystone)**

- 状态：**空壳**（`stub`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
expected: player HealthRegen flat modifier = f(hammer_regen, maxHealth); GetFlatBonus returns 0 and OnUpdate is empty
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `hammer_regen` | amulet meta stat | 0.001L (graph_hammer_regen_3_3, max 3) |
| `maxHealth` | player max HP | runtime |

**输出**：none (stub)

**代码**：[Player/Systems/HammerRegenSystem.cs:31-42](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/HammerRegenSystem.cs#L31)；[Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:392](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs#L392)

**状态依据**：OnUpdate/GetFlatBonus/ApplyModifier have empty bodies or return 0; system is registered in Core/SystemsInstaller.cs:82

**配置参数**：`graph_hammer_regen_3_3`

**备注**：The likely form is regen += hammer_regen * maxHealth, probably conditioned on a hammer (buckshot?) weapon being equipped. This is unconfirmed.

### `powerups.rewarded_ad_stat_reward`

**Rewarded-ad permanent stat amount**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x14c61ec (Update), 0x14c56b8 (GrantAdReward), 0x15a9df8 (GetRewardedDensity), 0x14c6c08`

```text
stat ~ RewardedConfig.RewardChances[PoolIndex] weighted pick; doubled = Minutes>=30 AND Roll(1/exp(0.1*density) - 0.5); density = (InAppsBoughtValueInRewarded + RewardedWatchTimes) / (max(1, Minutes - totalDelay)/60); value = round((doubled?2:1) * rewardedValueMultiplier * RPM[stat] * 6 * rewardedStatValueMultiplier(lastUnlockedLocation)); granted as permanent Flat StatModifier(Source=Ads) via GrantStatReward
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `RPM[stat]` | simulation reward-per-minute for that stat | SimulationSystem.RPMCalculatedValue |
| `rewardedValueMultiplier` | AB-test scale | EnemyHpDamageMultData.rewardedValueMultiplier (default 0.5) |
| `rewardedStatValueMultiplier` | AB-test per-location scale | CommonRewardMultipliersData.rewardedStatValueMultiplier (ValueByLocation) |
| `totalDelay` | delayMinutes + delayMinutesPerUsdSpent*InAppsBoughtValueInRewarded | RewardedDelayFromStart AB data |

**输出**：permanent flat stat bonus (= about 6 minutes of farming income)

**代码**：[Integrations/Ads/AdsInitialize.cs:287-297](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Integrations/Ads/AdsInitialize.cs#L287)；[Integrations/Ads/AdsInitialize.cs:217-227](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Integrations/Ads/AdsInitialize.cs#L217)；[Analytics/UserModel.cs:398-403](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Analytics/UserModel.cs#L398)；[ABTests/ABTestService.cs:16-20](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L16)；[ABTests/ABTestService.cs:36](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ABTests/ABTestService.cs#L36)

**状态依据**：Native VA comments on Update, GrantAdReward, GetRewardedDensity, GetRewardWithDynamicWeights

**配置参数**：`ABTestData.EnemyHpDamageMult.rewardedValueMultiplier`；`ABTestData.CommonRewardMultipliers.rewardedStatValueMultiplier`；`RewardedConfig.RewardChances`；`EnemySpawnerConfig.RewardedPoolIndex`；`NoAdsData.adCooldown/rewardedOfferDuration/maxAdsPerDay`

**使用方**：permanent stat growth

**备注**：The reward is tied to the simulation RPM, so changing devour/stat-reward balance rescales ad rewards automatically. Dynamic weights are disabled (IsDynamicRewardedWeights=false). The doubling chance is 50% when density=0 and falls to 0 once density >= ~6.93/h.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `expression`：value = round((doubled?2:1) * rewardedValueMultiplier * RPM[stat] * 6 * rewardedStatValueMultiplier(lastUnlockedLocation)); the pool is RewardChances[userModel.PoolIndex]. At grant time GrantStatReward also multiplies by cheats.DropMultiplier. Source=Ads, so neither DoubleLoot nor VIP doubling applies.  
  依据：[Integrations/Ads/AdsInitialize.cs:287-296](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Integrations/Ads/AdsInitialize.cs#L287),330-331; [Player/Systems/PlayerRewardSystem.cs:54-65](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/PlayerRewardSystem.cs#L54)

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

### `powerups.ability_unlock_gate`

**Item ability unlock by evolution and level**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`config_rule`
- 来源：核验时补充

```text
ability active iff (item.Evolved ? def.UnlockLevel <= item.Level : def.UnlockLevel == 0). AbilityStates are rebuilt (new states: Stacks, Cooldown, GuaranteedCrit and the temp buffs reset) whenever the active set changes on Level, Evolved or Copies changes.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `UnlockLevel` | star level needed | item asset AbilityDef.UnlockLevel |
| `Evolved, Level` | item evolution flag and star level | ItemModel / ItemDataUd |

**输出**：set of active abilities per item

**代码**：[Player/Inventory/ItemModel.cs:520-523](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L520)；[Player/Inventory/ItemModel.cs:561-573](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Inventory/ItemModel.cs#L561)

**状态依据**：IsAbilityUnlocked and RebuildAbilityStates carry no native annotation

**配置参数**：`AbilityDef.UnlockLevel`

**使用方**：powerups.ability_tier_override；all ability effects

**备注**：This is defined in the items code but is the gate for every ability number. Before evolution only UnlockLevel 0 abilities exist. Combined with Stars = IsPreDivine ? 0 : Level, it makes the full power step function of the item.

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

**配置参数**：`Amulet Config.LevelTargetDays`；`Amulet Config.FeatureUnlockDay`；`Amulet Config.AutoBalanceCostMultiplier`；`Amulet Config.AutoBalanceExperienceMultipliers`

**使用方**：powerups.amulet_autobalance_bake；powerups.amulet_required_xp_per_level

**备注**：The runtime unlock gate is UnlockLocationNumber (8), not FeatureUnlockDay. Merge into powerups.amulet_autobalance_bake if it already cites these fields.

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
