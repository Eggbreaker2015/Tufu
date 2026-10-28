# 属性系统（`stats`）

[返回公式清单总览](../FormulaCatalog.md)

## 概述

Every unit stat is a Stat(BaseValue, Type) plus a list of StatModifiers. It produces two numbers. Value (raw) = event * percentMult * (1+Σ percentAdd) * (Base+Σflat), then multiplied by Π over sources of (1+Σ preAddPostMult[source]). SpecificValue = clamp(postMult * (f_type(raw) + Σ postFlat)). f_type is a concave, piecewise power curve for MovementSpeed, Evasion, CritChance, CritDamage, GlobalAttack/Defence and BlockChance/Amount, and the identity for everything else. SuperCritDamage is a constant 2.0. Evasion, BlockChance and BlockAmount are hard-capped at 0.8. Combat reads raw Value for Health, Damage, flat/percent damage and defence, Lifesteal, AttackRange and AttackCooldown, but reads SpecificValue for probabilities and multipliers, and also for HealthRegen through a special case in UnitCore.GetStatValue. So item and crystal 'chance' bonuses that use PostFormulaFlat skip the diminishing curve, while raw-stat growth such as devour or quests goes through it. Modifiers with the same (Name, Source, ModifierType) merge into one: multiplicative types multiply, all others add, and float32 accumulation applies. Every devour reward is a single 'Devoured'/Enemy/Flat modifier per stat, so permanent growth is a plain float sum added to BaseValue before any multiplier. The aggregation core (StatBase.CalculateValue, Stat.UpdateValue/CalculateSpecificValue, AddModifier merge) has no native VA comment in this reconstruction. The ordering is corroborated by the natively annotated sibling MetaStat.CalculateValue (0x1485ef4), and the curve constants are too specific to have been invented.

**核验结论**：The catalog is largely accurate. I re-checked every expression against Unit/Stat.cs, StatBase.cs, StatModifier.cs, StatTypeExtensions.cs, UnitCore.cs, Systems/UnitBuffSystem.cs and Systems/UnitDamageSystem.cs. All of these match the code exactly: the raw aggregation order (E*M*A*(Base+F)*PROD(1+P_s)), the post-formula pipeline clamp(PM*(f+PF)), every curve constant and branch comparison (<10000, <700, <=700), the 0.8 cap set, the merge rule, and all four StatTypeExtensions bit masks (decoded independently). The persistence masks 0x50a and 0xef0a and the Player Config base table (30 entries) also check out. Status labels are defensible. Stat.cs and StatBase.cs carry no VA comments, so labelling the aggregation and pipeline 'reconstructed' is right. The curves are labelled recovered_native only on the strength of their non-guessable constants; the natively annotated bake and forecast callers (SimulationConfig.cs:114 at 0x13db16c, ForecastSnapshots.cs:150 at 0x13f7da0) corroborate that a static CalculateSpecificValue exists, but not its constants. No formula is rejected. Main corrections: (1) devour rewards are NOT one 'Devoured' modifier per stat. The name is rewritten to map.tree, map.elite or map.normal_mob, and the value is scaled by meta stat_drop, count, DropMultiplier, (1+DoubleLoot) and VIP before merging. (2) The lethal-ignore description was wrong; the per-target mode has no roll and is cooldown-gated. (3) The MagicDamagePercent concern is moot because no weapon uses type 11. The real hazard is Stats.First throwing for stats missing from Player Config. (4) Save and load drop Param/StringParam and bypass the merge. (5) Small line-ref fixes: EnemyUnit.cs:155, not 153; DamageCalculator.cs:148, not 142. (6) Snapshot TotalValue is raw except HealthRegen, and the runtime SimulationSystem does use the full specific pipeline. Missed formulas added: player movement displacement (UnitMovement, native) and the lethal-ignore rule. UnitAutoTargetSystem is an empty MonoBehaviour that no prefab or scene references (GUID grep), so it was left out as dead code. Balance/CoreConstants.cs is confirmed empty.

## 公式索引

| ID | 名称 | 状态 | 类型 |
|---|---|---|---|
| [`stats.raw_value_aggregation`](#statsraw_value_aggregation) | Raw stat value aggregation (StatBase.CalculateValue) | 重建（无原生标注） | `runtime_formula` |
| [`stats.event_percent_bonus`](#statsevent_percent_bonus) | EventPercentModifier diminishing bonus | 原生证据恢复 | `runtime_formula` |
| [`stats.specific_value_pipeline`](#statsspecific_value_pipeline) | Specific (effective) value pipeline (Stat.UpdateValue) | 重建（无原生标注） | `runtime_formula` |
| [`stats.specific_curve.movement_speed`](#statsspecific_curvemovement_speed) | Movement speed curve | 重建（无原生标注） | `runtime_formula` |
| [`stats.specific_curve.evasion`](#statsspecific_curveevasion) | Evasion (dodge chance) curve | 重建（无原生标注） | `runtime_formula` |
| [`stats.specific_curve.crit_chance`](#statsspecific_curvecrit_chance) | Crit chance curve | 重建（无原生标注） | `runtime_formula` |
| [`stats.specific_curve.crit_damage`](#statsspecific_curvecrit_damage) | Crit damage multiplier curve | 重建（无原生标注） | `runtime_formula` |
| [`stats.specific_curve.super_crit_damage`](#statsspecific_curvesuper_crit_damage) | Super-crit damage constant | 重建（无原生标注） | `runtime_formula` |
| [`stats.specific_curve.global_attack_defence`](#statsspecific_curveglobal_attack_defence) | GlobalAttack / GlobalDefence soft cap | 原生证据恢复 | `runtime_formula` |
| [`stats.specific_curve.block`](#statsspecific_curveblock) | Block chance and block amount curves | 原生证据恢复 | `runtime_formula` |
| [`stats.specific_cap`](#statsspecific_cap) | Specific value cap | 重建（无原生标注） | `config_rule` |
| [`stats.specific_identity`](#statsspecific_identity) | Identity conversion for non-curved stats | 重建（无原生标注） | `runtime_formula` |
| [`stats.modifier_merge`](#statsmodifier_merge) | Modifier stacking / merge rule (AddModifier) | 重建（无原生标注） | `runtime_formula` |
| [`stats.modifier_source_persistence`](#statsmodifier_source_persistence) | Which modifier sources persist (save filters) | 原生证据恢复 | `lookup_table` |
| [`stats.temporary_modifier_decay`](#statstemporary_modifier_decay) | Temporary modifier duration decay | 原生证据恢复 | `runtime_formula` |
| [`stats.stun_decay`](#statsstun_decay) | Stun duration decay | 原生证据恢复 | `runtime_formula` |
| [`stats.stat_accessors`](#statsstat_accessors) | Stat lookup / accessor semantics | 原生证据恢复 | `config_rule` |
| [`stats.player_base_stats`](#statsplayer_base_stats) | Player base stat table | 纯数据 | `lookup_table` |
| [`stats.percent_stat_classification`](#statspercent_stat_classification) | StatType classification masks | 原生证据恢复 | `lookup_table` |
| [`stats.lifesteal_player`](#statslifesteal_player) | Player on-hit lifesteal | 原生证据恢复 | `runtime_formula` |
| [`stats.lifesteal_enemy`](#statslifesteal_enemy) | Enemy lifesteal on hitting the player | 原生证据恢复 | `runtime_formula` |
| [`stats.thorns_reflection`](#statsthorns_reflection) | Player thorns reflection | 原生证据恢复 | `runtime_formula` |
| [`stats.enemy_reflection`](#statsenemy_reflection) | Enemy reflection damage to the player | 原生证据恢复 | `runtime_formula` |
| [`stats.first_hit_vulnerability`](#statsfirst_hit_vulnerability) | First-hit vulnerability multiplier | 原生证据恢复 | `runtime_formula` |
| [`stats.damage_application`](#statsdamage_application) | HP application and cheat overrides | 原生证据恢复 | `runtime_formula` |
| [`stats.player_movement_displacement`](#statsplayer_movement_displacement) | Player per-frame movement displacement | 原生证据恢复 | `runtime_formula` |
| [`stats.lethal_ignore`](#statslethal_ignore) | Lethal damage ignore (cheat-death) rule | 原生证据恢复 | `runtime_formula` |

## 公式

### `stats.raw_value_aggregation`

**Raw stat value aggregation (StatBase.CalculateValue)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
Value = E * M * A * (Base + F) * PROD_s (1 + P_s); where F = SUM v[Flat], A = 1 + SUM v[PercentAdditive], M = PROD v[PercentMultiplicative] (raw factors, NOT 1+v), E = PROD (1 + eventBonus%(v[EventPercent])/100), P_s = SUM v[PreAdditivePostMultiplicative] grouped by ModifierSource s. PostFormula* types are ignored here. No rounding, no caps.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Base` | Stat.BaseValue | Player Config.asset Stats[].BaseValue; EnemySpawner.Stats[].BaseValue (Main.unity); boss/quest/crystal overrides |
| `v[Flat]` | Flat modifier values (ModifierType 0) | item StatModifiers (Source 1), devour StatRewards 'Devoured' (Source 0), shop/reward/levelpass/quest/vip etc. |
| `v[PercentAdditive]` | ModifierType 1 values (fractions, e.g. 0.1 = +10%) | item/ability modifiers, RageExtraHealthSystem (GraphUpgrade), BossStatExtensions.SetPercentModifier |
| `v[PercentMultiplicative]` | ModifierType 2 raw factors | rare in data (1 asset: helm cut common div) |
| `P_s` | PreAdditivePostMultiplicative (type 4): additive inside one ModifierSource, multiplicative across sources | Level Pass Config (Source 7), Crystal Config, item/ability modifiers (Source 1/3), SecretChest rewards in Main.unity (Source 16), boss debuffs (VikingsRage, LavaCrab) |
| `E` | Event percent multiplier, one factor per distinct modifier | stats.event_percent_bonus; Crystal Event Roster Config (Source 12, type 7) |

**输出**：Stat.Value / Stat.FastValue (double). This is the number combat uses for Health (max HP), Damage, *DamageFlat, *DamagePercent, *DefenceFlat, *DefencePercent, Universal*, LifestealFlat/Percent, AttackRange, AttackCooldown

**代码**：[Unit/StatBase.cs:107-134](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatBase.cs#L107)；[Unit/Stat.cs:81-90](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L81)；[Meta/MetaStat.cs:155-176](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStat.cs#L155)

**状态依据**：StatBase.cs has no native VA comment. The identical ordering (multiplicative * additive * (base+flat), then a per-source (1+sum) product) appears in the natively annotated MetaStat.CalculateValue (ELF 0x1485ef4, Meta/MetaStat.cs:155), which lacks only the EventPercentModifier branch.

**配置参数**：`Player Config.Stats`；`EnemySpawner.Stats (Main.unity)`；`ItemConfig.StatModifiers`；`EnemySpawner.StatRewards`；`Level Pass Config modifiers`；`Crystal Config modifiers`；`Crystal Event Roster Config modifiers`

**使用方**：combat (DamageCalculator.cs:83-140)；player (PlayerModel stats, PlayerRegenSystem max HP)；enemy (EnemyUnit, EnemyAttackSystem AttackRange/AttackCooldown, EnemyRegenSystem)；simulation/forecast (raw snapshot stats)；bosstower；quests

**备注**：Iteration order of preAddPostMultBySource (Dictionary) does not affect the result because the operation is a product. When a Stat object is rebuilt with new BaseValue (EnemySpawner.cs:972 AB multiplier, EnemyHpDamageMultSystem.cs:61, QuestMobSpawnerService.cs:146 banker's rounding, BossSpawnService), its modifiers are DROPPED because `new Stat(lifetime, round(base*mult), type)` passes no modifiers. Player Stat objects for LastStats and Unit.Stats share the same Modifiers List reference (PlayerModel.cs:526, 690).

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `variables[v[Flat]].source`：devour StatRewards, renamed to map.tree/map.elite/map.normal_mob (Source 0, Flat), scaled by stat_drop meta, count, DropMultiplier, DoubleLoot and VIP. Also the IslandMastery Flat/Reward modifier with Value=level*BonusPerMedalLevel and Param=location (Location/IslandMasteryModel.cs:177).  
  依据：GrantSpawnerStatRewardCommandHandler.cs:26-29; IslandMasteryModel.cs:177

### `stats.event_percent_bonus`

**EventPercentModifier diminishing bonus**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15ca9f4 (ToString caller)`

```text
bonus%(v) = sqrt(v) * sqrt(min(v, 20))  (= v for v<=20; = sqrt(20*v) for v>20); multiplier(v) = 1 + bonus%(v)/100
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `v` | EventPercentModifier value in percent units (after same-name merges, which ADD) | Crystal Event Roster Config.asset (ModifierType 7, Source 12 CrystalEvent); chest/reward CustomModifier |

**输出**：Factor E in stats.raw_value_aggregation; display text '(+X%)'

**代码**：[Unit/StatModifier.cs:25-33](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatModifier.cs#L25)；[Unit/StatBase.cs:125-127](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatBase.cs#L125)；[Unit/StatModifier.cs:40](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatModifier.cs#L40)

**状态依据**：The helper has no VA of its own. It is called by the native-ported StatModifier.ToString (ELF 0x15ca9f4), and the non-guessable knee at 20 points to binary evidence. Its use inside StatBase.CalculateValue is reconstructed.

**配置参数**：`Crystal Event Roster Config.asset modifiers (type 7)`

**使用方**：stats.raw_value_aggregation；UI ChestOpenView/RewardView

**备注**：Because merging the same Name/Source/Type ADDS v, repeated event grants are diminished as a whole (sqrt of the sum). Separately named event modifiers multiply.

### `stats.specific_value_pipeline`

**Specific (effective) value pipeline (Stat.UpdateValue)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
Specific = clamp_type( PM * ( f_type(Value) + PF ) ); PM = PROD(1 + v[PostFormulaMultiplicative]) * (1 + SUM v[PostFormulaAdditive]); PF = SUM v[PostFormulaFlat]
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Value` | Raw aggregated value | stats.raw_value_aggregation |
| `f_type` | Per-StatType conversion curve | stats.specific_curve.* |
| `PF` | Post-formula flat, added in final units (probabilities for chance stats) | ModifierType 6 on items (Evasion/Crit/CritDmg/SuperCrit/Block), Crystal Config, Quest Config, QuestMobSpawnerService evasion |
| `PM` | Post-formula multiplier | ModifierType 3 (items: MovementSpeed, Evasion, CritChance) and 5 (one SecretChest reward in Main.unity) |

**输出**：Stat.SpecificValue / FastSpecificValue. Read by UnitCore.GetStatSpecificValue for MovementSpeed, Evasion, CritChance, CritDamage, SuperCrit*, Block*, GlobalAttack/Defence, ability %-health ratios, and HealthRegen through GetStatValue

**代码**：[Unit/Stat.cs:81-110](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L81)

**状态依据**：No VA comment. The original UI method stubs StatDetailsPopupView.CalculatePostFormulaMultiplier, GetPostFormulaFlatTotal, CalculateMultCoefficient and BuildPostFormulaFlatGroup (UI/Stats/StatDetailsPopupView.cs:283-297, 263) show that the post-formula stage exists natively. Exact ordering is inferred.

**配置参数**：`ItemConfig.StatModifiers (types 3/5/6)`；`Crystal Config`；`Quest Config`

**使用方**：combat DamageCalculator (Evasion/Block/Crit/SuperCrit/GlobalAttack/GlobalDefence)；player movement UnitMovement.cs:45；CameraFollowSystem.cs:314；PlayerAttackSystem.cs:93；PlayerRegenSystem；simulation SimulationSystem.cs:339-347

**备注**：PF bypasses the diminishing curve but not the 0.8 cap, so item chance bonuses add linearly in probability. Health SpecificValue (used by abilities for %HP) can diverge from max HP (raw Value) when PostFormula modifiers sit on Health.

### `stats.specific_curve.movement_speed`

**Movement speed curve**

- 状态：**重建（无原生标注）**（`reconstructed`），核验前为 `recovered_native`
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
f(x) = 4.6 * x^0.115  if x < 10000;  3.0 * x^0.1 + 5.8  otherwise
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `x` | raw MovementSpeed Value | Player Config base 1 (gives 4.6), devour/shop flats |

**输出**：Player move speed in units/s (also camera follow bonus = speed/5, attack anim speed factor speed*0.05)

**代码**：[Unit/Stat.cs:65-66](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L65)

**状态依据**：No VA comment, but the piecewise constants cannot have been guessed and must come from the binary. f(9999.99)=13.27 vs f(10000)=13.34, a small discontinuity.

**配置参数**：`Player Config.Stats[Type=0].BaseValue`

**使用方**：[Unit/UnitMovement.cs:45](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitMovement.cs#L45)；[Camera/CameraFollowSystem.cs:314](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Camera/CameraFollowSystem.cs#L314)；[Player/PlayerAttackSystem.cs:93](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L93)；[Simulation/Systems/SimulationSystem.cs:410](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L410),631

**备注**：Enemies set Agent.speed from the raw MovementSpeed BaseValue (EnemyUnit.cs:153), not from the curve.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes (EnemyUnit line)`：Enemy/EnemyUnit.cs:155: Agent.speed = (float)Stats.First(MovementSpeed).BaseValue. This is BaseValue, so modifiers are ignored as well as the curve.  
  依据：grep shows Agent.speed at EnemyUnit.cs:155. Line 153 is AggroTimer = 0f.
- [已应用] `status`：reconstructed  
  依据：Mechanical audit 2026-10-27: none of the cited files contain a native address comment and native_va is empty; matches the completeness critic's status audit.

### `stats.specific_curve.evasion`

**Evasion (dodge chance) curve**

- 状态：**重建（无原生标注）**（`reconstructed`），核验前为 `recovered_native`
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
f(x) = 0.06 * x^0.12  if x < 700;  0.05 * x^0.065 + 0.06  otherwise; then +PF, *PM, min(.,0.8)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `x` | raw Evasion Value | Player Config base 0; enemy spawner Stats |

**输出**：dodge probability per hit

**代码**：[Unit/Stat.cs:67-68](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L67)

**状态依据**：Non-guessable constants. f(100)=0.104 and f(699)=0.132 vs f(700)=0.137 (jump). The 0.8 cap cannot be reached through the curve alone.

**配置参数**：`Player Config.Stats[Type=23]`；`ItemConfig PostFormulaFlat Evasion`

**使用方**：[Services/Damage/DamageCalculator.cs:89](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L89),192；SimulationConfig.cs:134；BossSpawnService.cs:465；SimulationSystem.cs:339,979

**备注**：With x=0 the result is 0, so only PostFormulaFlat or PostFormulaAdditive add dodge.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [已应用] `status`：reconstructed  
  依据：Mechanical audit 2026-10-27: none of the cited files contain a native address comment and native_va is empty; matches the completeness critic's status audit.

### `stats.specific_curve.crit_chance`

**Crit chance curve**

- 状态：**重建（无原生标注）**（`reconstructed`），核验前为 `recovered_native`
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
f(x) = 0.08 * x^0.15  if x <= 700;  0.07 * x^0.1 + 0.082  otherwise (no cap)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `x` | raw CritChance Value | Player Config base 1 (gives 8%) |

**输出**：crit probability (DamageCalculator adds enemy SpecialAdditionalCritChancePercent and weapon AbilityCritChanceBonus)

**代码**：[Unit/Stat.cs:69-70](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L69)

**状态依据**：Non-guessable constants. f(700)=0.2137 vs just above it 0.2168.

**配置参数**：`Player Config.Stats[Type=24]`

**使用方**：DamageCalculator.cs:133；SimulationConfig.cs:124；BossSpawnService.cs:455；SimulationSystem.cs:344

**备注**：Not cappable. Values above 1 always crit.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [已应用] `status`：reconstructed  
  依据：Mechanical audit 2026-10-27: none of the cited files contain a native address comment and native_va is empty; matches the completeness critic's status audit.

### `stats.specific_curve.crit_damage`

**Crit damage multiplier curve**

- 状态：**重建（无原生标注）**（`reconstructed`），核验前为 `recovered_native`
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
f(x) = 1.5 * x^0.08
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `x` | raw CritDamagePercent Value | Player Config base 1 (gives x1.5) |

**输出**：crit damage multiplier (applied as damage *= f; simulation uses 1 + (f-1)*critChance)

**代码**：[Unit/Stat.cs:71](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L71)

**状态依据**：Non-guessable constants. f(10)=1.80, f(100)=2.17, f(1000)=2.61.

**配置参数**：`Player Config.Stats[Type=25]`

**使用方**：DamageCalculator.cs:132,139；SimulationConfig.cs:125；BossSpawnService.cs:456；SimulationSystem.cs:345

**备注**：x=0 would give a multiplier of 0, so base 1 is required.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [已应用] `status`：reconstructed  
  依据：Mechanical audit 2026-10-27: none of the cited files contain a native address comment and native_va is empty; matches the completeness critic's status audit.

### `stats.specific_curve.super_crit_damage`

**Super-crit damage constant**

- 状态：**重建（无原生标注）**（`reconstructed`），核验前为 `recovered_native`
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
f(x) = 2.0 (raw value ignored); effective = PM*(2.0 + PF)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `x` | raw SuperCritDamagePercent (ignored) | Player Config base 1 |

**输出**：super-crit multiplier (DamageCalculator adds weapon/next-hit bonuses)

**代码**：[Unit/Stat.cs:72](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L72)

**状态依据**：Constant switch arm; only PostFormula modifiers (type 6 on items, e.g. buck_epic_shield, stab_epic_helmet) change it.

**配置参数**：`ItemConfig PostFormulaFlat on StatType 29`

**使用方**：DamageCalculator.cs:135,142

**备注**：SuperCritChance (28) has no curve (identity): raw + PF.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `used_by`：Services/Damage/DamageCalculator.cs:135 (superDamage = Specific(29) + weapon AbilitySuperCritDamageBonus + NextHitSuperCritDamageBonus) and :148 (damage *= superDamage if IsSuperCrit). Line 142 is the crit branch.  
  依据：DamageCalculator.cs:142 is 'if (result.IsCrit || fromSimulation)'. Line 148 is 'damage *= result.IsSuperCrit ? superDamage : 1.0'.
- [已应用] `status`：reconstructed  
  依据：Mechanical audit 2026-10-27: none of the cited files contain a native address comment and native_va is empty; matches the completeness critic's status audit.

### `stats.specific_curve.global_attack_defence`

**GlobalAttack / GlobalDefence soft cap**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
f(x) = x if x <= 100; (x-100)^0.75 + 100 otherwise. Consumers: atkMult = 1 + f(GA)/100; defMult = 1 / (1 + f(GD)/100)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `x` | raw GlobalAttack/GlobalDefence Value (percent points) | Player Config base 0; meta/upgrade modifiers |

**输出**：percent bonus. f(1000)=264.3

**代码**：[Unit/Stat.cs:73-74](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L73)；[Services/Damage/DamageCalculator.cs:238-246](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L238)

**状态依据**：Curve has no VA. Consumers in DamageCalculator are unannotated too. Constants are specific.

**配置参数**：`Player Config.Stats[Type=30,31]`

**使用方**：DamageCalculator.cs:112,177；SimulationSystem.cs:343,347

**备注**：Defence is hyperbolic: 100 points halves incoming damage.

### `stats.specific_curve.block`

**Block chance and block amount curves**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
blockChance = min(0.8, PM*(0.08 * x^0.1 + PF)); blockAmount = min(0.8, PM*(0.15 * y^0.053 + PF)); incoming *= (1 - blockAmount) on a block roll
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `x` | raw BlockChance | Player Config base 0 |
| `y` | raw BlockAmount | Player Config base 1 (gives 15% reduction) |

**输出**：block proc probability and damage reduction fraction

**代码**：[Unit/Stat.cs:75-76](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L75)；[Services/Damage/DamageCalculator.cs:97-101](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L97)

**状态依据**：Non-guessable exponents 0.1 and 0.053. The curve alone reaches 0.8 only at x=1e10 or y~5e13, so the cap matters only with PF (buck_legendary items, Crystal Config type 6).

**配置参数**：`Player Config.Stats[Type=33,34]`；`ItemConfig PostFormulaFlat on 33/34`

**使用方**：DamageCalculator.cs:97-101；SimulationConfig.cs:137-138；BossSpawnService.cs:466-467；SimulationSystem.cs:340-341

**备注**：Simulation uses the expected value: incoming *= 1 - blockChance*blockAmount.

### `stats.specific_cap`

**Specific value cap**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`config_rule`
- 来源：子系统梳理

```text
if Type in {Evasion, BlockChance, BlockAmount}: Specific = min(Specific, 0.8); IsSpecificValueCapped = Specific >= 0.8
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `0.8` | CappedSpecificValueLimit const | constant Stat.cs:17 |

**输出**：capped SpecificValue

**代码**：[Unit/Stat.cs:17](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L17),42,51-59,86

**状态依据**：The const 0.8 is a metadata-declared field, so it is reliable. The set of cappable types is inferred (consistent with UI ColorizeIfCapped stub at StatDetailsPopupView.cs:308). No VA.

**使用方**：stats.specific_value_pipeline；UI stat details

**备注**：Crit chance, super-crit and lifesteal are uncapped. Simulation and bake code call CalculateSpecificValue directly and SKIP both the clamp and the post-formula stage (SimulationConfig.cs:124-138, BossSpawnService.cs:455-467, ForecastSnapshots.cs:153).

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes (bake/forecast inputs)`：Correct, with an addition. Snapshot TotalValue is captured through UnitCore.GetStatValue (Simulation/Forecast/ForecastSnapshots.cs:100), so it is the raw Value for every stat EXCEPT HealthRegen, which is already the specific value. The runtime SimulationSystem itself (SimulationSystem.cs:339-347, GetForecastStatSpecificValue at :999, VA 0x13e686c) reads the real SpecificValue, including PF/PM and the cap. Only the editor bake and forecast paths skip them. BuildSpawnerBakeReport carries a native annotation ('Original ELF0x13db16c', SimulationConfig.cs:114) and ForecastSnapshots does too ('Original 0x13f7da0', :150). This corroborates that the native code calls a static CalculateSpecificValue(type, raw).  
  依据：ForecastSnapshots.cs:100 TotalValue = player.Unit.GetStatValue(stat.Type, true); SimulationSystem.cs:999-1003.

### `stats.specific_identity`

**Identity conversion for non-curved stats**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
f(x) = x for all StatTypes not listed (Health, HealthRegen, Damage, flat/percent dmg/def, Lifesteal*, SuperCritChance, AttackRange, AttackCooldown, VisualScaleMultiplier)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `x` | raw Value | stats.raw_value_aggregation |

**输出**：SpecificValue = PM*(x+PF)

**代码**：[Unit/Stat.cs:77](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L77)

**状态依据**：Default branch of the switch.

**使用方**：PlayerRegenSystem/EnemyRegenSystem via GetStatValue(HealthRegen)；ability %HP effects

**备注**：IsSpecificStat mask (MovementSpeed, Evasion, Crit*, SuperCrit*, Global*, Block*) includes SuperCritChance even though it has no curve.

### `stats.modifier_merge`

**Modifier stacking / merge rule (AddModifier)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：子系统梳理

```text
key=(Name,Source,ModifierType). If an existing modifier matches: for type in {PercentMultiplicative, PostFormulaMultiplicative}: v_old := v_old * v_new; else v_old := v_old + v_new; Duration := max(Duration_old, Duration_new). Otherwise append. Then recompute Value and Specific.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `v` | StatModifier.Value (float32) | incoming modifier |

**输出**：Merged modifier list

**代码**：[Unit/StatBase.cs:63-85](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatBase.cs#L63)

**状态依据**：No VA. Persistence callbacks in MetaStat.OnModifierUpdated (0x148681c) confirm the 'update existing by Name/Source/Type' contract.

**配置参数**：`EnemySpawner.StatRewards[].Name ('Devoured' for all 577)`

**使用方**：devour growth (PlayerRewardSystem.cs:74)；GrantRewardCommandRunner.cs:243；ShopLotModel.cs:286；items (PlayerModel.cs:795,822)；AddTemporaryModifierCommandHandler

**备注**：Devour growth: permanent_flat[stat] = SUM over kills of reward*DropMultiplier*(abilityDouble)*(vipDouble), accumulated in ONE float32 modifier, so precision loss grows at large totals. Suspected inconsistency: PostFormulaMultiplicative is merged by multiplying v but evaluated as (1+v), so two 0.1 grants give 1.01 instead of 1.21. Item modifiers are the shared config objects (PlayerModel.cs:795), so a merge would mutate the ItemConfig asset at runtime.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes / config_params (devour bucket)`：The 577 'Devoured' names in Main.unity are renamed before the grant. If Source==Enemy and the name is empty or 'Devoured', the name becomes 'map.tree' (IsStatic), 'map.elite' (IsEliteBoss) or 'map.normal_mob' (Enemy/Commands/GrantSpawnerStatRewardCommandHandler.cs:26,34-37). So devour growth lands in up to 3 Flat/Source-0 float32 modifiers per stat, not one. The per-kill value is v = reward * statDropMult(meta 'stat_drop', SpawnerStatDropAmountCalculator, VA 0x1488e3c) * count(SpawnerStatRewardMultiplier, if >1) * cheats.DropMultiplier * PROD over DoubleLoot abilities (1 + ability.GetValue()) * (vip.Multiplier with probability vip.Chance) (GrantSpawnerStatRewardCommandHandler.cs:27-29; Player/Systems/PlayerRewardSystem.cs:52-72). abilityDouble is (1+GetValue), not a literal x2. The sum is still a plain add to Base+F, because all buckets are Flat.  
  依据：GrantSpawnerStatRewardCommandHandler.cs:19-31 builds a copy with the resolved name and multipliers, then calls PlayerRewardSystem.GrantStatReward, which multiplies by DropMultiplier, doubleLoot and VIP before stat.AddModifier at PlayerRewardSystem.cs:74.

### `stats.modifier_source_persistence`

**Which modifier sources persist (save filters)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`lookup_table`
- 来源：子系统梳理
- 原生地址：`0x145d45c (SaveStats), 0x1485ed4 (IsTemporarySource), 0x145c268 (LoadStats)`

```text
PlayerModel.SaveStats skips Source in {1 Item, 3 AbilityTemporary, 8 Crystal, 10 GraphUpgrade} (mask 0x50a, source<=10); all others are saved. MetaStat.IsTemporarySource = {1,3,8,9,10,11,13,14,15} (mask 0xef0a).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Source` | ModifierSource enum | StatModifier.Source |

**输出**：which stat bonuses are saved vs re-derived on load

**代码**：[Player/Models/PlayerModel.cs:654-674](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L654)；[Meta/MetaStat.cs:135-139](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStat.cs#L135)；[Player/Models/PlayerModel.cs:624-636](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L624)

**状态依据**：Masks are quoted from the ARM64 code in the comments.

**使用方**：player persistence；meta stats

**备注**：Defined in Player/Meta and listed here because it decides which part of the aggregate is permanent (Enemy devour, Shop, Ads, Reward, LevelPass, CrystalUpgrade, MineUpgrade, SecretChest, Quest, Cashback...). LoadStats keeps BaseValue from PlayerConfig, not from the save.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：LoadStats keeps BaseValue from PlayerConfig, not from the save. It also appends saved modifiers with stat.Modifiers.Add, which bypasses the AddModifier merge, so saved duplicates stay separate but sum identically. Duration is 0. Param and StringParam are lost: SaveStats never writes them (PlayerModel.cs:666-670) and LoadStats never reads them (:634). Any Param-keyed player modifier, such as IslandMastery's Param=locationNumber, is reloaded without its Param.  
  依据：[Player/Models/PlayerModel.cs:624-636](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L624), 654-674

### `stats.temporary_modifier_decay`

**Temporary modifier duration decay**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15ccda8, 0x14681d4`

```text
each frame for modifiers with Source=AbilityTemporary and Duration>0: Duration -= dt; if Duration<=0 remove (and recompute stat). On VisualScaleMultiplier expiry: localScale tweens to OriginalScale*Value over 0.35 s
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `dt` | frame delta | runtime |
| `Duration` | modifier duration (merge keeps the max) | AddTemporaryModifierCommand / AddStatEffect.Duration |

**输出**：active temporary buffs

**代码**：[Unit/Systems/UnitBuffSystem.cs:43-65](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitBuffSystem.cs#L43)；[Unit/StatBase.cs:70](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatBase.cs#L70),90；[Player/Commands/AddTemporaryModifierCommandHandler.cs:14-22](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Commands/AddTemporaryModifierCommandHandler.cs#L14)

**状态依据**：VA comments on UnitBuffSystem.UpdateTemporaryModifiers and the command handler.

**配置参数**：`ability configs Duration`；`VikingsRage DebuffDuration`；`LavaCrab 5s/+500% regen`

**使用方**：abilities；bosstower debuffs

**备注**：RemoveDuplicates=true replaces by (Name,Source) rather than merging, so the value refreshes and does not stack.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：With RemoveDuplicates=true, the old modifier matching (Name, Source) is removed, ignoring ModifierType, and a new one is added. With RemoveDuplicates=false and the same (Name, Source, Type), AddModifier merges: values ADD (they stack) and Duration = max. The TemporaryModifiers cache is not refreshed on a merge (StatBase.cs:73-83), only on add/remove. The expiry tween uses ScaleComponent.OriginalScale, not UnitCore.OriginalScale, with target scale = OriginalScale * stat.Value after removal.  
  依据：AddTemporaryModifierCommandHandler.cs:18-20; StatBase.cs:65-84; UnitBuffSystem.cs:53-57

### `stats.stun_decay`

**Stun duration decay**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15cd31c`

```text
stun.Duration -= dt; remove when <= 0
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Duration` | BuffStunComponent.Duration | ability stun effects |

**输出**：stun state (DamageCalculator BonusDamageToStunned multiplier 1+b while stunned)

**代码**：[Unit/Systems/UnitBuffSystem.cs:67-71](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitBuffSystem.cs#L67)；[Unit/Components/BuffStunComponent.cs:7](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Components/BuffStunComponent.cs#L7)

**状态依据**：VA comment.

**使用方**：combat DamageCalculator.cs:178-183

**备注**：BuffType enum has only Stun=0.

### `stats.stat_accessors`

**Stat lookup / accessor semantics**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`config_rule`
- 来源：子系统梳理
- 原生地址：`0x15cb8d4, 0x15cbb94, 0x15cb14c, 0x15cbe30`

```text
lookup[36] from Unit.Stats, first duplicate wins; GetStatValue(t) = 0 if missing; = Specific if t==HealthRegen; else Value. GetStatSpecificValue(t) = 0 if missing else Specific. IsDead = CurrentHealth <= 0.01 or IsDying
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `t` | StatType | caller |

**输出**：stat values seen by every system

**代码**：[Unit/UnitCore.cs:53](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitCore.cs#L53),85-114,138-142

**状态依据**：VA comments; regen special case noted as deliberate.

**使用方**：all damage/regen/ability systems

**备注**：The player has no Magic damage/defence stats (10,11,18,19), LifestealPercent (32) or VisualScaleMultiplier in Player Config, so these read as 0. Note that MagicDamagePercent=0 would zero magic weapon damage if it were ever used as a percent multiplier.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes (Magic stat concern)`：No asset uses DamageStatTypePercent=11. Weapons use 7/9/13 and armor configs reuse the field with 15/17/21/27, so the concern is moot for the current data. The real hazard is different: any item or reward StatModifier targeting a stat missing from Player Config (10,11,18,19,32,35) would throw. PlayerModel.cs:795/822 and PlayerRewardSystem.cs:51 use Stats.First(...), not TryGetStat.  
  依据：grep 'DamageStatTypePercent:' over MonoBehaviour/*.asset: counts 7:10, 9:10, 13:10, 15:18, 17:18, 21:18, 27:4. StatType 32 appears only in Icon Style Config.

### `stats.player_base_stats`

**Player base stat table**

- 状态：**纯数据**（`data_only`）
- 类型：`lookup_table`
- 来源：子系统梳理

```text
Health=100, MovementSpeed=1, HealthRegen=0.5, Damage=0, AttackRange=3.4, AttackCooldown=0, all *Flat dmg/def=0, all *Percent dmg/def (Stab/Cut/Buckshot)=1, LifestealFlat=0, Evasion=0, CritChance=1, CritDamagePercent=1, UniversalDefenceFlat=0, UniversalDefencePercent=0, SuperCritChance=0, SuperCritDamagePercent=1, GlobalAttack=0, GlobalDefence=0, BlockChance=0, BlockAmount=1
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `BaseValue` | per StatType base | Player Config.asset Stats |

**输出**：starting raw values. Effective: speed 4.6, crit 8%, crit x1.5, block amount 15%

**代码**：[Player/Models/PlayerModel.cs:524-526](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L524)

**状态依据**：Values read from Assets/MonoBehaviour/Player Config.asset lines 15-75.

**配置参数**：`Player Config.Stats`

**使用方**：stats.raw_value_aggregation

**备注**：Percent stats use base 1 and are applied as direct multipliers (damage = percent * (...)). They are not 1+x.

### `stats.percent_stat_classification`

**StatType classification masks**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`lookup_table`
- 来源：子系统梳理
- 原生地址：`0x15cafa8, 0x15cafc8, 0x15cafe4, 0x15cb004, 0x15cb054`

```text
IsPercentStat = {7,9,11,13,15,17,19,21,27,32}; IsSpecificPercentStat = {23,24,25,28,29,33,34}; IsPercentDisplay = union of both; IsSpecificStat = {0,23,24,25,28,29,30,31,33,34}; GetIconStatType maps X%->XFlat for Stab/Cut/Buckshot dmg & def
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `type` | StatType int | enum |

**输出**：display and percent handling flags

**代码**：[Unit/StatTypeExtensions.cs:5-42](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatTypeExtensions.cs#L5)

**状态依据**：Bit masks quoted from ARM64.

**使用方**：UI QuestItemView, RewardView, ChestOpenView

**备注**：Display-oriented, but it documents which stats are fractions (x100 for display) vs raw points.

### `stats.lifesteal_player`

**Player on-hit lifesteal**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15d00cc`

```text
f = LifestealFlat; p = LifestealPercent; if target marked (stacks k>0) and meta mark_lifesteal=b>0: f*=1+b*k, p*=1+b*k; if f>0 and target is Hydra with marks: f*=1+0.1*k; if NextHitLifestealFlatBonus=n>0: f*=1+n (consumed); HP += f; HP += p*MaxHP; HP = min(HP, MaxHP)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `LifestealFlat/Percent` | raw stat Values of attacker | devour, items (cut_rare_shield...), quests |
| `b` | meta stat mark_lifesteal | Meta Stats Config |
| `k` | MarkComponent.Stacks on target | runtime |

**输出**：player heal per hit (applied even on dodge or zero damage)

**代码**：[Unit/Systems/UnitDamageSystem.cs:336-366](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L336)

**状态依据**：VA comment.

**配置参数**：`Meta Stats Config: mark_lifesteal`

**使用方**：combat；simulation battle regen (DamageCalculator.GetBattleRegen uses LifestealFlat)

**备注**：Called on every enemy hit, including dummy targets.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：Called on every enemy hit, including dodged hits and dummy targets. NextHitLifestealFlatBonus is consumed even when flat == 0. The min(HP, MaxHP) cap is applied only if flat>0 or percent>0. MaxHP is the raw Health Value.  
  依据：UnitDamageSystem.cs:318, 332 (unconditional calls), 357-365

### `stats.lifesteal_enemy`

**Enemy lifesteal on hitting the player**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15ce7e8`

```text
HP_enemy += LifestealFlat; HP_enemy += LifestealPercent * MaxHP_enemy (no cap)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `LifestealPercent` | enemy stat | QuestMobSpawnerService: MobLifestealMaxHealthPercent*0.01 |

**输出**：enemy heal per landed hit

**代码**：[Unit/Systems/UnitDamageSystem.cs:155-162](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L155)

**状态依据**：VA comment says the health cap is intentionally absent.

**配置参数**：`QuestChainDef.MobLifestealMaxHealthPercent`

**使用方**：quests

**备注**：Only applied when damage > 0 after the god-mode check.

### `stats.thorns_reflection`

**Player thorns reflection**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15ce604`

```text
reflect = ThornsDamage * (1 + NextThornsDamageBonus) (bonus consumed); attackerHP -= reflect; applied on every hit received if ThornsDamage>0
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ThornsDamage` | PlayerModel.ThornsDamage = SUM sourceStatSpecific * multStatSpecific * abilityValue | Abilities/Effects/ThornsEffect.cs:17-19 |

**输出**：damage to attacking enemy (unaffected by armor)

**代码**：[Unit/Systems/UnitDamageSystem.cs:124](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L124),212-228

**状态依据**：VA comment.

**配置参数**：`ability ThornsEffect DamageSourceStat/DamageMultiplierStat`

**使用方**：combat

**备注**：Fires even when the hit was dodged.

### `stats.enemy_reflection`

**Enemy reflection damage to the player**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15cf560`

```text
if not dodged: playerHP -= enemy.ReflectionDamage (flat, unmitigated, skipped in god mode)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `ReflectionDamage` | enemy field | Quest: damage*MobReflectionMaxHealthPercent*0.01; CrystalObjectiveUnitStats.ReflectionDamage |

**输出**：player HP loss per own hit

**代码**：[Unit/Systems/UnitDamageSystem.cs:252-271](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L252),329

**状态依据**：VA comment.

**配置参数**：`QuestChainDef.MobReflectionMaxHealthPercent`；`Crystal objective balance`

**使用方**：quests；crystals

### `stats.first_hit_vulnerability`

**First-hit vulnerability multiplier**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15cd840`

```text
damage *= UnitFirstVulnerabilityComponent.DamageModifier (once, then component removed; marks hit empowered)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `DamageModifier` | one-shot damage multiplier | VampireSpiritSystem sets 2.0 |

**输出**：outgoing damage on the first hit

**代码**：[Unit/Systems/UnitDamageSystem.cs:301-306](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L301)；[Unit/Components/UnitFirstVulnerabilityComponent.cs:7](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Components/UnitFirstVulnerabilityComponent.cs#L7)

**状态依据**：Part of the annotated ApplyDamageToEnemy.

**使用方**：bosstower

**备注**：Applied after DamageCalculator rounding, so the result can be fractional before HP subtraction.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes`：Applied after the cheat overrides (OneHitMode 0.9*MaxHP, OneDamageMode 1), so OneDamageMode plus vulnerability gives 2. The component is consumed even on a dodged hit (dodged damage is 0, so the x2 is wasted). DamageModifier is float32 (2f from VampireSpiritSystem.cs:249).  
  依据：UnitDamageSystem.cs:292-306: there is no HasDodged guard around the firstVulnerability branch. VampireSpiritSystem.cs:249 sets DamageModifier = 2f.

### `stats.damage_application`

**HP application and cheat overrides**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：子系统梳理
- 原生地址：`0x15cdf10, 0x15cd840`

```text
Enemy: dmg = DamageCalculator result; OneHitMode -> 0.9*MaxHP; OneDamageMode -> 1; HP -= dmg; dummy targets reset HP to max; Oneshot trigger if killed and oldHP >= MaxHP - 1e-4. Player: dmg = FixedDamage if HasFixedDamage; GodMode -> 0; HP -= dmg; death if HP <= 0.01; on block: BlockSuperCritChanceBonus += meta block_to_supercrit; on dodge with a pierce weapon equipped: apply evasion_mark marks
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `block_to_supercrit` | meta bonus super-crit chance per block (reset when a super-crit lands) | Meta Stats Config |
| `evasion_mark` | mark stacks applied on dodge | Meta Stats Config |

**输出**：CurrentHealth changes; super-crit bonus accumulation

**代码**：[Unit/Systems/UnitDamageSystem.cs:110-153](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L110),286-334

**状态依据**：VA comments.

**配置参数**：`Meta Stats Config: block_to_supercrit, evasion_mark`

**使用方**：combat

**备注**：Lethal ignore (0x15ce89c): if LethalIgnoreChance roll succeeds or a per-target ignore is available, HP is set to 1 and the chance is reset to 0.

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `notes (lethal ignore)`：The two modes are exclusive. If HasPerTargetLethalIgnore, there is NO roll. Success requires id(source) not in LethalIgnoreTargetIds and LethalIgnoreCooldown <= 0, and the id is then added. Otherwise success requires Random.value < LethalIgnoreChance. On success HP := 1.0 and BOTH HasPerTargetLethalIgnore := false and LethalIgnoreChance := 0. Death clamps HP to max(HP, 0) (ProcessPlayerDeath, UnitDamageSystem.cs:167). Lifesteal of the attacking enemy is applied before the lethal check (line 144). See missed formula stats.lethal_ignore.  
  依据：[Unit/Systems/UnitDamageSystem.cs:176-193](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L176), 142-151

### `stats.player_movement_displacement`

**Player per-frame movement displacement**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：核验时补充
- 原生地址：`0x15cbf80, 0x15cc4fc, 0x15cc4a8`

```text
speed = CanMove ? Specific(MovementSpeed) : 0; d = R(theta) * (speed*input.x*dt, speed*input.y*dt), theta = atan2(cam.z - p.z, cam.x - p.x)*180/pi + 90 deg; agent.Move((d.x, 0, d.y)); facing turns at 460 deg/s toward the visual target (acquired when the nearest enemy is within 4 units, kept within 6) or else toward the movement direction
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `speed` | effective move speed (units/s) | stats.specific_curve.movement_speed via UnitCore.GetStatSpecificValue |
| `input` | joystick vector, magnitude <= 1 | runtime Joystick.GetInput |
| `dt` | Time.deltaTime | runtime |

**输出**：player displacement per frame (distance/time travelled, which feeds fight/move time in play)

**代码**：[Unit/UnitMovement.cs:38-66](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitMovement.cs#L38)；[Unit/UnitMovement.cs:68-93](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitMovement.cs#L68)；[Unit/UnitMovement.cs:95-102](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitMovement.cs#L95)

**状态依据**：VA comments on Update, UpdateVisualTarget and RotateVector. The 460 deg/s rate is noted as confirmed in ARM64.

**配置参数**：`Player Config.Stats[Type=0].BaseValue`

**使用方**：player movement；simulation move-time assumptions (SimulationSystem.cs:410,631 use the same specific speed)

**备注**：Linear in specific speed, so the travel-time impact of a MovementSpeed change is entirely the concave curve. Radii 4/6 are compared squared (16/36).

### `stats.lethal_ignore`

**Lethal damage ignore (cheat-death) rule**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：核验时补充
- 原生地址：`0x15ce89c`

```text
on player HP <= 0.01 after a hit: if HasPerTargetLethalIgnore: success iff srcId not in LethalIgnoreTargetIds and LethalIgnoreCooldown <= 0 (then add srcId); else success iff LethalIgnoreChance > 0 and U(0,1) < LethalIgnoreChance. On success: HP := 1, LethalIgnoreChance := 0, HasPerTargetLethalIgnore := false, fire OnLethalDamageIgnore
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `LethalIgnoreChance` | one-shot survive probability | PlayerModel.LethalIgnoreChance (ability effects) |
| `LethalIgnoreCooldown` | cooldown gating per-target mode | Abilities/Effects/LethalIgnorePassiveEffect.cs:16 (State.Cooldown) |

**输出**：whether the player survives a lethal hit at 1 HP

**代码**：[Unit/Systems/UnitDamageSystem.cs:145](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L145)；[Unit/Systems/UnitDamageSystem.cs:176-193](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L176)

**状态依据**：VA comment on TryIgnoreLethalDamage.

**配置参数**：`ability configs granting LethalIgnore (LethalIgnorePassiveEffect)`

**使用方**：combat survival / effective HP

**备注**：Not covered by enemy reflection deaths (ApplyEnemyReflection kills directly without the check, UnitDamageSystem.cs:261-265).

**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：

- [待人工合入] `id (missed list)`：Duplicate of combat.lethal_damage_ignore and powerups.lethal_ignore. Merge into one entry (defined in Unit/Systems/UnitDamageSystem.cs:176-193 TryIgnoreLethalDamage, native 0x15ce89c) and list the other two as aliases.  
  依据：[Unit/Systems/UnitDamageSystem.cs:145](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L145) and :176-193 is the only implementation.

## 配置参数

| 资产 | 字段 | 类型 | 含义 | 用于 |
|---|---|---|---|---|
| Player Config.asset | `Stats[].BaseValue / Type` | List<Stat{double BaseValue, StatType Type}> | Player base raw stats (30 entries; no Magic*, LifestealPercent or VisualScale) | stats.player_base_stats, stats.raw_value_aggregation |
| Scenes/Main.unity EnemySpawner | `Stats[].BaseValue / Type` | List<Stat> | Enemy base raw stats (no modifiers; Agent.speed uses raw MovementSpeed) | stats.raw_value_aggregation |
| Scenes/Main.unity EnemySpawner | `StatRewards[] {Name='Devoured', StatType, ModifierType=0, Source=0, Value}` | List<StatModifier> | Permanent devour growth per kill, merged into one Flat modifier per stat | stats.modifier_merge, stats.raw_value_aggregation |
| Item assets (e.g. cut common.asset, buck_rare_helmet.asset, *div*.asset) | `StatModifiers[] / ability AddStatEffect data {StatType, ModifierType, Source, Value, Duration}` | StatModifier | Equipment and ability stat bonuses. Chance stats use ModifierType 6 PostFormulaFlat; %dmg/%def use type 4 or 1 | stats.raw_value_aggregation, stats.specific_value_pipeline, stats.temporary_modifier_decay |
| Level Pass Config.asset | `rewards modifiers (ModifierType 4, Source 7)` | StatModifier | Per-source multiplicative bonuses | stats.raw_value_aggregation |
| Crystal Config.asset | `modifiers (types 4 and 6)` | StatModifier | Crystal stat bonuses | stats.raw_value_aggregation, stats.specific_value_pipeline |
| Crystal Event Roster Config.asset | `modifiers (ModifierType 7, Source 12)` | StatModifier | Event percent modifiers with diminishing bonus | stats.event_percent_bonus |
| Quest Config.asset | `rewards / mob stats (Flat Source 0; PostFormulaFlat on 23/24/25)` | StatModifier | Quest stat rewards | stats.raw_value_aggregation, stats.specific_value_pipeline |
| Meta Stats Config.asset | `mark_lifesteal, block_to_supercrit, evasion_mark` | MetaStat key | Meta upgrades read by UnitDamageSystem | stats.lifesteal_player, stats.damage_application |
| (code constant) | `Stat.CappedSpecificValueLimit = 0.8` | double | Cap for Evasion/BlockChance/BlockAmount | stats.specific_cap |
| (code constants) | `curve coefficients in Stat.CalculateSpecificValue` | double | Diminishing-returns curves | stats.specific_curve.movement_speed, stats.specific_curve.evasion, stats.specific_curve.crit_chance, stats.specific_curve.crit_damage, stats.specific_curve.global_attack_defence, stats.specific_curve.block |

## 枚举

| 枚举 | 文件 | 取值 |
|---|---|---|
| `StatType` | [Unit/StatType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatType.cs) | 0=MovementSpeed,1=Health,2=HealthRegen,3=Damage,4=AttackRange,5=AttackCooldown,6=StabDamageFlat,7=StabDamagePercent,8=CutDamageFlat,9=CutDamagePercent,10=MagicDamageFlat,11=MagicDamagePercent,12=BuckshotDamageFlat,13=BuckshotDamagePercent,14=StabDefenceFlat,15=StabDefencePercent,16=CutDefenceFlat,17=CutDefencePercent,18=MagicDefenceFlat,19=MagicDefencePercent,20=BuckshotDefenceFlat,21=BuckshotDefencePercent,22=LifestealFlat,23=Evasion,24=CritChance,25=CritDamagePercent,26=UniversalDefenceFlat,27=UniversalDefencePercent,28=SuperCritChance,29=SuperCritDamagePercent,30=GlobalAttack,31=GlobalDefence,32=LifestealPercent,33=BlockChance,34=BlockAmount,35=VisualScaleMultiplier |
| `ModifierType` | [Unit/ModifierType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/ModifierType.cs) | 0=Flat(+base),1=PercentAdditive(sum, *(1+S)),2=PercentMultiplicative(*v raw factor; merges by *),3=PostFormulaAdditive(specific *(1+S)),4=PreAdditivePostMultiplicative(sum per Source, *(1+S_src) across sources),5=PostFormulaMultiplicative(specific *(1+v); merges by *),6=PostFormulaFlat(added after curve),7=EventPercentModifier(*(1+sqrt(v)*sqrt(min(v,20))/100)) |
| `ModifierSource` | [Unit/ModifierSource.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/ModifierSource.cs) | 0=Enemy,1=Item,2=AbilityPermanent,3=AbilityTemporary,4=Shop,5=Ads,6=Reward,7=LevelPass,8=Crystal,9=CrystalUpgrade,10=GraphUpgrade,11=MineUpgrade,12=CrystalEvent,13=CrystalPass,14=SoulPass,15=Vip,16=SecretChest,17=CrystalGuardian,18=Quest,19=Cashback. Player save skips {1,3,8,10}; MetaStat temporary {1,3,8,9,10,11,13,14,15}. TemporaryModifiers list = Source 3 with Duration>0 |
| `BuffType` | [Unit/BuffType.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/BuffType.cs) | 0=Stun |

## 未解决问题

1. Stat.cs and StatBase.cs (CalculateValue, UpdateValue, GetPostFormulaMultiplier/Flat, CalculateSpecificValue, AddModifier merge) have no native VA comments. Their ordering should be checked against the IL2CPP binary (MetaStat 0x1485ef4 corroborates the raw stage only). In particular: is the clamp applied after PM*(f+PF), and is EventPercent part of the raw stage?
2. AddModifier merges PostFormulaMultiplicative by multiplying Values while evaluation uses (1+v). Is this an original bug or a reconstruction error? It affects stacking of any type-5 grant (one exists in Main.unity SecretChest rewards).
3. Simulation, bake and forecast code (SimulationConfig.cs:124-138, BossSpawnService.cs:455-467, ForecastSnapshots.cs:153) calls CalculateSpecificValue on raw snapshot totals, skipping PostFormula modifiers and the 0.8 cap. Does the original forecast likewise ignore item PostFormulaFlat chance bonuses?
4. Discontinuities at the MovementSpeed (10000), Evasion (700) and CritChance (700) breakpoints are small but real. Are they intentional?
5. StatModifier.Value is float32 and devour rewards merge into one 'Devoured' modifier per stat. Large totals (e.g. Health > 1.6e7) lose integer precision. Is this a design concern?
6. Item stat modifiers are added by reference from ItemConfig.StatModifiers (PlayerModel.cs:795). A merge with an identical (Name,Source,Type) key would mutate the config asset at runtime. Do the original assets guarantee unique names?
7. Balance/CoreConstants.cs nested classes Balance/Strings/Placements are empty. Any const values there were probably inlined and are not recoverable from this file.
8. UI StatDetailsPopupView formula helpers (CalculateMultCoefficient, AggregateValue, IsRawPercentModifier) are stubs. Recovering them would confirm the native per-ModifierType aggregation rule.
9. MagicDamage/MagicDefence stats are absent from Player Config, so GetStatValue returns 0. If any weapon uses DamageStatTypePercent=MagicDamagePercent, its damage would be 0. Check the weapon configs.
