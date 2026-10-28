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
| [`stats.specific_cap`](#statsspecific_cap) | Specific value cap (Evasion / BlockChance / BlockAmount <= 0.8) | 重建（无原生标注） | `config_rule` |
| [`stats.specific_identity`](#statsspecific_identity) | Identity conversion for non-curved stats | 重建（无原生标注） | `runtime_formula` |
| [`stats.modifier_merge`](#statsmodifier_merge) | Modifier stacking / merge rule (StatBase.AddModifier) | 重建（无原生标注） | `runtime_formula` |
| [`stats.modifier_source_persistence`](#statsmodifier_source_persistence) | Which modifier sources persist (save filters) | 原生证据恢复 | `lookup_table` |
| [`stats.temporary_modifier_decay`](#statstemporary_modifier_decay) | Temporary modifier duration decay and expiry (UnitBuffSystem) | 原生证据恢复 | `runtime_formula` |
| [`stats.stun_decay`](#statsstun_decay) | Stun duration countdown and effects (BuffStunComponent) | 原生证据恢复 | `runtime_formula` |
| [`stats.stat_accessors`](#statsstat_accessors) | Stat lookup / accessor semantics (UnitCore) | 原生证据恢复 | `config_rule` |
| [`stats.player_base_stats`](#statsplayer_base_stats) | Player base stat table | 纯数据 | `lookup_table` |
| [`stats.percent_stat_classification`](#statspercent_stat_classification) | StatType classification masks | 原生证据恢复 | `lookup_table` |
| [`stats.damage_application`](#statsdamage_application) | HP application and cheat overrides | 原生证据恢复 | `runtime_formula` |
| [`stats.player_movement_displacement`](#statsplayer_movement_displacement) | Player per-frame movement displacement (UnitMovement.Update) | 原生证据恢复 | `runtime_formula` |

## 公式

### `stats.raw_value_aggregation`

**Raw stat value aggregation (StatBase.CalculateValue)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 合并自：`stats.raw_value_aggregation`、`combat.stat_value_aggregation`、`meta.metastat_value_unfiltered`

```text
Value = E * M * A * (Base + F) * PROD_s (1 + P_s), in double, over ALL entries of Modifiers (no Param/StringParam filter). F = SUM v[Flat(0)]; A = 1 + SUM v[PercentAdditive(1)]; M = PROD v[PercentMultiplicative(2)] (raw factor, NOT 1+v); P_s = SUM v[PreAdditivePostMultiplicative(4)] grouped by ModifierSource s; E = PROD over EventPercentModifier(7) of (1 + 0.01 * sqrt(v) * sqrt(min(v, 20))). PostFormula types 3/5/6 are ignored here. Code order: result = E*M*A*(Base+F), then result *= (1 + P_s) for each source in Dictionary order. No rounding, no clamps.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Base` | StatBase.BaseValue (Stat or MetaStat) | Player Config.asset Stats[].BaseValue; EnemySpawner(Config).Stats[].BaseValue (Main.unity); boss/quest/crystal overrides; for MetaStat, Meta Stats Config.asset Stats[].BaseValue (e.g. chest_level=1, crystal_chest_ench_level=0) |
| `v[Flat]` | Flat modifier values (ModifierType 0) | item StatModifiers (Source 1). Devour StatRewards (Source 0 Enemy, Flat): authored as 'Devoured', renamed at grant to map.tree/map.elite/map.normal_mob, value scaled by the meta stat_drop multiplier, kill count, cheats DropMultiplier, DoubleLoot (1+ability value) and VIP chance/multiplier (GrantSpawnerStatRewardCommandHandler.cs:26-29, PlayerRewardSystem.cs:54-74). Shop/reward/levelpass/quest/vip etc. On MetaStat island_medal_bonus: the IslandMastery Flat/Reward(6) modifier with Value = level*BonusPerMedalLevel, Param = locationNumber (Location/IslandMasteryModel.cs:177). On MetaStat chest_level: amulet GraphUpgrade flats and the 'vip_chest_level' Flat/Vip(15) modifier with Value = vipLevel (Shop/ChestsModel.cs:116-137) |
| `v[PercentAdditive]` | ModifierType 1 values (fractions, 0.1 = +10%) | item/ability modifiers, RageExtraHealthSystem (GraphUpgrade), BossStatExtensions.SetPercentModifier |
| `v[PercentMultiplicative]` | ModifierType 2 raw factors | rare in data (1 asset: helm cut common div) |
| `P_s` | PreAdditivePostMultiplicative (type 4): additive inside one ModifierSource, multiplicative across sources | Level Pass Config (Source 7), Crystal Config, item/ability modifiers (Source 1/3), SecretChest rewards in Main.unity (Source 16), boss debuffs (VikingsRage, LavaCrab) |
| `E` | Event percent multiplier, one factor per type-7 modifier: 1 + 0.01*sqrt(v)*sqrt(min(v,20)) | StatModifier.GetEventPercentBonusPercent (StatModifier.cs:25-28); Crystal Event Roster Config (Source 12, type 7); see stats.event_percent_bonus |

**输出**：Stat.Value / Stat.FastValue (double), which UnitCore.GetStatValue returns for every stat except HealthRegen. Combat uses it for Health (max HP), Damage, *DamageFlat, *DamagePercent, *DefenceFlat, *DefencePercent, Universal*, LifestealFlat/Percent, AttackRange and AttackCooldown. Also MetaStat.Value (param-blind), which code reads directly in ChestsModel.Level ((int)chest_level.Value) and ChestsModel.CrystalChestEnchantLevel ((int)crystal_chest_ench_level.Value).

**代码**：[Unit/StatBase.cs:107-134](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatBase.cs#L107)；[Unit/StatBase.cs:49-53](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatBase.cs#L49)；[Unit/Stat.cs:81-90](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L81)；[Unit/StatModifier.cs:25-33](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatModifier.cs#L25)；[Unit/UnitCore.cs:107-114](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitCore.cs#L107)；[Meta/MetaStat.cs:40-47](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStat.cs#L40)；[Meta/MetaStat.cs:152-174](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStat.cs#L152)；[Shop/ChestsModel.cs:42-43](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L42)；[Shop/ChestsModel.cs:76-83](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L76)；[Shop/ChestsModel.cs:89-92](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L89)；[Shop/ChestsModel.cs:116-137](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L116)

**状态依据**：StatBase.CalculateValue (StatBase.cs:107) and StatModifier.GetEventPercentBonusPercent carry no native VA comment. The same ordering (M * A * (Base+F), then a per-source (1+sum) product) appears in the natively annotated sibling MetaStat.CalculateValue(int,string) (ELF 0x1485ef4, MetaStat.cs:152), which adds a Param filter and has no EventPercentModifier branch. StatModifier.ToString (0x15ca9f4) calls GetEventPercentBonusPercent. Sibling annotations do not count for this method.

**配置参数**：`Player Config.Stats[].BaseValue`；`EnemySpawner(Config).Stats (Main.unity)`；`ItemConfig.StatModifiers`；`EnemySpawner(Config).StatRewards`；`Level Pass Config modifiers`；`Crystal Config modifiers`；`Crystal Event Roster Config modifiers`；`Meta Stats Config.Stats[chest_level].BaseValue=1`；`Meta Stats Config.Stats[crystal_chest_ench_level].BaseValue=0`；`IslandMastery BonusPerMedalLevel`；`VIP level (vip_chest_level modifier on chest_level)`

**使用方**：combat (DamageCalculator.cs:83-140)；combat.outgoing_damage_to_enemy；combat.incoming_damage_to_player；player (PlayerModel stats, PlayerRegenSystem max HP)；enemy (EnemyUnit, EnemyAttackSystem AttackRange/AttackCooldown/MovementSpeed, EnemyRegenSystem)；stats.specific_value_pipeline (raw input to f_type)；simulation/forecast (raw snapshot stats)；bosstower；quests；progression/devour；meta.chest_copies；meta.chest_rare_rarity_roll

**备注**：PercentMultiplicative uses v directly as a factor, not (1+v). The iteration order of preAddPostMultBySource (a Dictionary) does not matter because the step is a product. MetaStat.Value goes through this StatBase method (the MetaStat ctor and BindToUserData call UpdateValue, MetaStat.cs:46,64), so it ignores Param and DOES include event modifiers. MetaStat.GetValue(param) instead uses the param-filtered MetaStat.CalculateValue, which has no event branch. ChestsModel reads the unfiltered .Value: chest_level = (int)(1 + SUM of Flat chest_level modifiers), i.e. 1 + amulet graph_chest_level_* flats + vipLevel. The vipLevel term comes from the 'vip_chest_level' Flat/Vip modifier that ApplyVipModifier re-applies whenever vipModel.Level changes; Vip is a temporary meta source, so it is not saved. When a Stat object is rebuilt with a new BaseValue (EnemySpawner.cs:972 AB multiplier, EnemyHpDamageMultSystem.cs:61, QuestMobSpawnerService.cs:146 banker's rounding, BossSpawnService), its modifiers are DROPPED because `new Stat(lifetime, round(base*mult), type)` passes no modifiers. EnemyUnit.Initialize likewise builds Stat(base, type) only. Player Stat objects in LastStats and Unit.Stats share the same Modifiers List reference (PlayerModel.cs:526, 690).

**依赖图节点**：输入 `config:PlayerConfig.Stats[].BaseValue`、`value:player.stat_base`、`value:enemy.stat_base`、`config:MetaStatsConfig.Stats[].BaseValue`、`value:player.meta_stat_base`、`value:player.stat_modifiers`、`value:enemy.stat_modifiers`、`value:player.event_percent_factor`、`value:player.upgrade_meta_modifier`、`state:vip_level`；输出 `value:player.stat_raw`、`value:player.stat_raw[Health]`、`value:player.stat_raw[Damage]`、`value:player.stat_raw[DamageFlatByType]`、`value:player.stat_raw[DamagePercentByType]`、`value:player.stat_raw[DefenceFlatByType]`、`value:player.stat_raw[DefencePercentByType]`、`value:player.stat_raw[UniversalDefenceFlat]`、`value:player.stat_raw[UniversalDefencePercent]`、`value:player.stat_raw[LifestealFlat]`、`value:player.stat_raw[LifestealPercent]`、`value:player.stat_raw[AttackRange]`、`value:player.stat_raw[MovementSpeed]`、`value:player.stat_raw[Evasion]`、`value:player.stat_raw[CritChance]`、`value:player.stat_raw[CritDamagePercent]`、`value:player.stat_raw[SuperCritDamagePercent]`、`value:player.stat_raw[GlobalAttack]`、`value:player.stat_raw[GlobalDefence]`、`value:player.stat_raw[BlockChance]`、`value:player.stat_raw[BlockAmount]`、`value:enemy.stat_raw`、`value:enemy.stat_raw[Health]`、`value:enemy.stat_raw[Damage]`、`value:enemy.stat_raw[AttackCooldown]`、`value:enemy.stat_raw[AttackRange]`、`value:enemy.stat_raw[MovementSpeed]`、`value:enemy.stat_raw[LifestealFlat]`、`value:enemy.stat_raw[LifestealPercent]`、`value:player.meta_stat`

**核验记录**（已写入上方字段）：

- 采纳 `variables[v[Flat]].source`（来自 `stats.raw_value_aggregation`）：Verified at GrantSpawnerStatRewardCommandHandler.cs:26-29,34-37 (rename to map.tree/map.elite/map.normal_mob, stat_drop multiplier, count) and PlayerRewardSystem.cs:54-74 (DropMultiplier, DoubleLoot 1+GetValue, VIP chance roll). IslandMasteryModel.cs:177 adds a Flat/Reward modifier with Value=level*BonusPerMedalLevel, Param=locationNumber, on MetaStat island_medal_bonus. The v[Flat] source now states this.
- 采纳 `code_refs`（来自 `meta.metastat_value_unfiltered`）：CrystalChestEnchantLevel is at [Shop/ChestsModel.cs:76-83](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L76) ((int)crystal_chest_ench_level.Value); code_refs now cite that range.
- 合并复核修正：(a) The usage note from meta.metastat_value_unfiltered gives chest_level = (int)(1 + SUM amulet flat + VIP level). The merged note keeps only 'the vip_chest_level modifier from ApplyVipModifier' and drops its magnitude. ChestsModel.ApplyVipModifier ([Shop/ChestsModel.cs:116-137](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ChestsModel.cs#L116), run by an Atom.Reaction on vipModel.Level at :91) removes the old modifier and adds a Flat, Source Vip(15) modifier with Value = vipLevel. That code is not cited.

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

**依赖图节点**：输入 `value:reward.granted_stat_modifier`、`constant:StatModifier.event_percent_knee`、`constant:StatModifier.event_percent_scale`；输出 `value:player.event_percent_factor`

### `stats.specific_value_pipeline`

**Specific (effective) value pipeline (Stat.UpdateValue)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 合并自：`stats.specific_value_pipeline`、`combat.stat_specific_value_curves`

```text
raw = CalculateValue(); SpecificValue = FastSpecificValue = ClampSpecificValue(Type, PM * (f_Type(raw) + PF)); Value = FastValue = raw. PM = PROD(1 + v[PostFormulaMultiplicative(5)]) * (SUM v[PostFormulaAdditive(3)] + 1); PF = SUM v[PostFormulaFlat(6)]. ClampSpecificValue = min(x, 0.8) only for Evasion, BlockChance, BlockAmount (no lower clamp); identity otherwise.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `raw` | Raw aggregated value | stats.raw_value_aggregation |
| `f_Type` | Per-StatType conversion curve (Stat.CalculateSpecificValue): MovementSpeed, Evasion, CritChance, CritDamagePercent, SuperCritDamagePercent (constant 2.0), GlobalAttack/GlobalDefence, BlockChance, BlockAmount; identity for all other stats | stats.specific_curve.* (Stat.cs:61-79) |
| `PF` | Post-formula flat, added in final units (probabilities for chance stats) | ModifierType 6 on items (Evasion/Crit/CritDmg/SuperCrit/Block), Crystal Config, Quest Config, QuestMobSpawnerService evasion |
| `PM` | Post-formula multiplier | ModifierType 3 (items: MovementSpeed, Evasion, CritChance) and 5 (one SecretChest reward in Main.unity) |
| `0.8` | CappedSpecificValueLimit | Stat.cs:17 constant; see stats.specific_cap |

**输出**：Stat.SpecificValue / FastSpecificValue, read by UnitCore.GetStatSpecificValue for MovementSpeed, Evasion, CritChance, CritDamage, SuperCrit*, Block*, GlobalAttack/Defence and ability %-health ratios, and by UnitCore.GetStatValue for HealthRegen only

**代码**：[Unit/Stat.cs:17](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L17)；[Unit/Stat.cs:42-59](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L42)；[Unit/Stat.cs:61-79](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L61)；[Unit/Stat.cs:81-110](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L81)；[Unit/UnitCore.cs:100-114](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitCore.cs#L100)

**状态依据**：No VA comment on Stat.UpdateValue, GetPostFormulaMultiplier or GetPostFormulaFlat. The accessors UnitCore.GetStatSpecificValue (0x15cbb94) and GetStatValue (0x15cb14c, HealthRegen special case) are annotated but are separate methods. The UI stubs StatDetailsPopupView.CalculatePostFormulaMultiplier, GetPostFormulaFlatTotal, CalculateMultCoefficient and BuildPostFormulaFlatGroup (UI/Stats/StatDetailsPopupView.cs:263, 283-297) show that the post-formula stage exists natively; the exact ordering is inferred. The curve constants look ported (piecewise curves are roughly continuous at the breakpoints) but are unconfirmed.

**配置参数**：`ItemConfig.StatModifiers (types 3/5/6)`；`Crystal Config`；`Quest Config`；`Player Config.Stats[Type=0,23,24,25,29,30,31,33,34]`

**使用方**：combat DamageCalculator (Evasion/Block/Crit/SuperCrit/GlobalAttack/GlobalDefence)；combat.crit_and_supercrit；combat.dodge_block_rolls；combat.global_attack_multiplier；combat.global_defence_multiplier；combat.player_regen (HealthRegen via GetStatValue)；stats.player_movement_displacement (UnitMovement.cs:45)；combat.animation_cast_point (PlayerAttackSystem.cs:93)；CameraFollowSystem.cs:314；simulation SimulationSystem.cs:339-347,410,631,979-981

**备注**：PF bypasses the diminishing curve but not the 0.8 cap, so item chance bonuses add linearly in probability. The 0.8 cap applies to Evasion and Block only, not CritChance. Health SpecificValue (used by abilities for %HP) can diverge from max HP (raw Value) when PostFormula modifiers sit on Health. Player defaults at raw 1: crit 8%, crit damage x1.5, super-crit x2.0, block amount 15%, block chance 0, evasion 0, move speed 4.6. Enemy MovementSpeed uses the raw value (EnemyUnit.cs:155 BaseValue at init, EnemyAttackSystem.cs:102 GetStatValue), not this curve. Per-stat curve formulas are catalogued in stats.specific_curve.* and the cap in stats.specific_cap; combat.stat_specific_value_curves' curve list was distributed there.

**依赖图节点**：输入 `value:player.stat_raw`、`value:player.stat_raw[Health]`、`value:player.stat_raw[MovementSpeed]`、`value:player.stat_raw[Evasion]`、`value:player.stat_raw[CritChance]`、`value:player.stat_raw[CritDamagePercent]`、`value:player.stat_raw[GlobalAttack]`、`value:player.stat_raw[GlobalDefence]`、`value:player.stat_raw[BlockChance]`、`value:player.stat_raw[BlockAmount]`、`value:enemy.stat_raw`、`value:player.stat_modifiers`、`value:enemy.stat_modifiers`、`constant:Stat.CappedSpecificValueLimit`；输出 `value:player.stat_specific`、`value:player.stat_specific[MovementSpeed]`、`value:player.stat_specific[Evasion]`、`value:player.stat_specific[CritChance]`、`value:player.stat_specific[CritDamagePercent]`、`value:player.stat_specific[SuperCritChance]`、`value:player.stat_specific[SuperCritDamagePercent]`、`value:player.stat_specific[GlobalAttack]`、`value:player.stat_specific[GlobalDefence]`、`value:player.stat_specific[BlockChance]`、`value:player.stat_specific[BlockAmount]`、`value:player.stat_specific[HealthRegen]`、`value:player.stat_specific[Health]`、`value:enemy.stat_specific`、`value:enemy.stat_specific[HealthRegen]`、`value:enemy.stat_specific[Evasion]`、`value:enemy.stat_specific[BlockChance]`、`value:enemy.stat_specific[BlockAmount]`

### `stats.specific_curve.movement_speed`

**Movement speed curve**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：合并核验

```text
f(x) = pow(x, 0.115) * 4.6  if x < 10000;  pow(x, 0.1) * 3.0 + 5.8  otherwise (no cap; then effective = PM*(f(x)+PF))
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `x` | raw MovementSpeed Value (StatType 0) | Player Config base 1 (gives 4.6), devour/shop flats |

**输出**：Player move speed in units/s (also camera height bonus = speed/5, attack anim speed target = |input|*speed*0.05)

**代码**：[Unit/Stat.cs:61-66](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L61)

**状态依据**：No VA comment in Stat.cs, but the piecewise constants cannot have been guessed and must come from the binary. f(9999.99)=13.27 vs f(10000)=13.34, a small discontinuity.

**配置参数**：`Player Config.Stats[Type=0].BaseValue`

**使用方**：[Unit/UnitMovement.cs:45](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitMovement.cs#L45)；[Camera/CameraFollowSystem.cs:314](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Camera/CameraFollowSystem.cs#L314)；[Player/PlayerAttackSystem.cs:93](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerAttackSystem.cs#L93)；[Simulation/Systems/SimulationSystem.cs:410](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L410),631

**备注**：Enemies do not use this curve. EnemyUnit.cs:155 sets Agent.speed = (float)Stats.First(MovementSpeed).BaseValue at init, so modifiers and the curve are both ignored there; EnemyAttackSystem.cs:102 later sets Agent.speed = GetStatValue(MovementSpeed) (raw Value, including modifiers) * (RetreatTimer > 0 ? 2 : 1) while chasing.

**依赖图节点**：输入 `value:player.stat_raw[MovementSpeed]`、`value:player.stat_modifiers`、`constant:Stat.move_speed_breakpoint`、`constant:Stat.move_speed_low_coef`、`constant:Stat.move_speed_low_exp`、`constant:Stat.move_speed_high_coef`、`constant:Stat.move_speed_high_exp`、`constant:Stat.move_speed_high_offset`；输出 `value:player.stat_specific[MovementSpeed]`

**核验记录**（已写入上方字段）：

- 采纳 `notes (EnemyUnit line)`（来自 `stats.specific_curve.movement_speed`）：Verified: EnemyUnit.cs:155 is Agent.speed = BaseValue; line 153 is AggroTimer. Notes now cite :155 and add that EnemyAttackSystem.cs:102 later overwrites speed with the raw Value (modifiers included), so 'modifiers ignored' holds only for the init assignment.

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

**依赖图节点**：输入 `value:player.stat_raw[Evasion]`、`value:enemy.stat_raw`、`value:player.stat_modifiers`、`value:enemy.stat_modifiers`、`constant:Stat.evasion_breakpoint`、`constant:Stat.evasion_low_coef`、`constant:Stat.evasion_low_exp`、`constant:Stat.evasion_high_coef`、`constant:Stat.evasion_high_exp`、`constant:Stat.evasion_high_offset`、`constant:Stat.CappedSpecificValueLimit`；输出 `value:player.stat_specific[Evasion]`、`value:enemy.stat_specific[Evasion]`

**核验记录**（已写入上方字段）：

- 采纳 `status`（来自 `stats.specific_curve.evasion`）：Mechanical audit 2026-10-27: none of the cited files contain a native address comment and native_va is empty; matches the completeness critic's status audit.

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

**依赖图节点**：输入 `value:player.stat_raw[CritChance]`、`value:player.stat_modifiers`、`constant:Stat.crit_chance_breakpoint`、`constant:Stat.crit_chance_low_coef`、`constant:Stat.crit_chance_low_exp`、`constant:Stat.crit_chance_high_coef`、`constant:Stat.crit_chance_high_exp`、`constant:Stat.crit_chance_high_offset`；输出 `value:player.stat_specific[CritChance]`

**核验记录**（已写入上方字段）：

- 采纳 `status`（来自 `stats.specific_curve.crit_chance`）：Mechanical audit 2026-10-27: none of the cited files contain a native address comment and native_va is empty; matches the completeness critic's status audit.

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

**依赖图节点**：输入 `value:player.stat_raw[CritDamagePercent]`、`value:player.stat_modifiers`、`constant:Stat.crit_damage_coef`、`constant:Stat.crit_damage_exp`；输出 `value:player.stat_specific[CritDamagePercent]`

**核验记录**（已写入上方字段）：

- 采纳 `status`（来自 `stats.specific_curve.crit_damage`）：Mechanical audit 2026-10-27: none of the cited files contain a native address comment and native_va is empty; matches the completeness critic's status audit.

### `stats.specific_curve.super_crit_damage`

**Super-crit damage constant**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 来源：合并核验

```text
f(x) = 2.0 (raw value ignored); Specific(29) = PM*(2.0 + PF), not capped. In DamageCalculator: superDamage = Specific(SuperCritDamagePercent) + weapon.AbilitySuperCritDamageBonus + owner.NextHitSuperCritDamageBonus; damage *= IsSuperCrit ? superDamage : 1.0, where IsSuperCrit = IsCrit && roll(superChance)
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `x` | raw SuperCritDamagePercent (ignored) | Player Config base 1 |
| `PF` | PostFormulaFlat on StatType 29 | item modifiers type 6 (e.g. buck_epic_shield, stab_epic_helmet) |

**输出**：super-crit damage multiplier (applied on top of the crit multiplier)

**代码**：[Unit/Stat.cs:72](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L72)；[Services/Damage/DamageCalculator.cs:135](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L135)；[Services/Damage/DamageCalculator.cs:137](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L137)；[Services/Damage/DamageCalculator.cs:148](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L148)

**状态依据**：Constant switch arm in Stat.CalculateSpecificValue with no native annotation; only PostFormula modifiers change it.

**配置参数**：`ItemConfig PostFormulaFlat on StatType 29`

**使用方**：[Services/Damage/DamageCalculator.cs:135](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L135) (superDamage = Specific(29) + weapon AbilitySuperCritDamageBonus + NextHitSuperCritDamageBonus)；[Services/Damage/DamageCalculator.cs:148](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L148) (damage *= superDamage if IsSuperCrit)

**备注**：SuperCritChance (28) has no curve (identity): raw + PF. Line 142 of DamageCalculator is the crit branch, not the super-crit application.

**依赖图节点**：输入 `constant:Stat.super_crit_damage_base`、`value:player.stat_modifiers`；输出 `value:player.stat_specific[SuperCritDamagePercent]`

**核验记录**（已写入上方字段）：

- 采纳 `used_by`（来自 `stats.specific_curve.super_crit_damage`）：Verified: DamageCalculator.cs:135 computes superDamage, :142 is 'if (result.IsCrit || fromSimulation)', :148 is 'damage *= result.IsSuperCrit ? superDamage : 1.0'. used_by and code_refs now cite 135 and 148.

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

**依赖图节点**：输入 `value:player.stat_raw[GlobalAttack]`、`value:player.stat_raw[GlobalDefence]`、`constant:Stat.global_soft_cap_knee`、`constant:Stat.global_soft_cap_exp`；输出 `value:player.stat_specific[GlobalAttack]`、`value:player.stat_specific[GlobalDefence]`

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

**依赖图节点**：输入 `value:player.stat_raw[BlockChance]`、`value:player.stat_raw[BlockAmount]`、`value:enemy.stat_raw`、`value:player.stat_modifiers`、`value:enemy.stat_modifiers`、`constant:Stat.block_chance_coef`、`constant:Stat.block_chance_exp`、`constant:Stat.block_amount_coef`、`constant:Stat.block_amount_exp`、`constant:Stat.CappedSpecificValueLimit`；输出 `value:player.stat_specific[BlockChance]`、`value:player.stat_specific[BlockAmount]`、`value:enemy.stat_specific[BlockChance]`、`value:enemy.stat_specific[BlockAmount]`

### `stats.specific_cap`

**Specific value cap (Evasion / BlockChance / BlockAmount <= 0.8)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`config_rule`
- 来源：合并核验

```text
Stat.UpdateValue: SpecificValue = ClampSpecificValue(Type, PM * (CalculateSpecificValue(Type, raw) + PF)), where ClampSpecificValue(t, v) = IsSpecificValueCappable(t) ? Math.Min(v, 0.8) : v and IsSpecificValueCappable(t) = (t == Evasion || t == BlockChance || t == BlockAmount). IsSpecificValueCapped = IsSpecificValueCappable(Type) && SpecificValue >= 0.8. FastSpecificValue = SpecificValue (clamped).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `0.8` | CappedSpecificValueLimit (private const double) | constant Unit/Stat.cs:17 |
| `Type` | StatType of the stat; only Evasion(23), BlockChance(33), BlockAmount(34) are cappable | Stat.Type |
| `raw` | CalculateValue() result (base + modifiers) | StatBase.CalculateValue |
| `PM` | post-formula multiplier = prod(1+PostFormulaMultiplicative) * (1 + sum PostFormulaAdditive) | Stat.GetPostFormulaMultiplier (Stat.cs:92-102) |
| `PF` | sum of PostFormulaFlat modifier values | Stat.GetPostFormulaFlat (Stat.cs:104-110) |

**输出**：capped SpecificValue / FastSpecificValue; IsSpecificValueCapped flag

**代码**：[Unit/Stat.cs:17](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Stat.cs#L17),42,51-59,81-90；[Simulation/Systems/SimulationSystem.cs:339-347](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/SimulationSystem.cs#L339),999-1003；[Simulation/Forecast/ForecastSnapshots.cs:100](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Forecast/ForecastSnapshots.cs#L100),150-158；[Simulation/SimulationConfig.cs:114-138](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs#L114)；[BossTower/BossSpawnService.cs:83](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/BossSpawnService.cs#L83),444-467,494-496；[Quests/QuestMobSpawnerService.cs:95](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestMobSpawnerService.cs#L95)；[UI/BossTower/BossTowerBossDetailsView.cs:243](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/BossTower/BossTowerBossDetailsView.cs#L243)；[UI/Stats/StatDetailsPopupView.cs:308-311](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/Stats/StatDetailsPopupView.cs#L308)

**状态依据**：Stat.cs carries no native VA annotation on IsSpecificValueCappable, ClampSpecificValue, IsSpecificValueCapped or UpdateValue. The 0.8 const is a metadata-declared field (reliable); the cappable type set is inferred and consistent with the UI ColorizeIfCapped stub (UI/Stats/StatDetailsPopupView.cs:308, empty body).

**使用方**：stats.specific_value_pipeline；combat (dodge/block via GetStatSpecificValue)；simulation runtime SimulationSystem cached dodge/block multipliers；UI stat details (ColorizeIfCapped stub)

**备注**：The clamp is applied AFTER the post-formula stage (PM/PF), so post-formula modifiers cannot push Evasion/BlockChance/BlockAmount above 0.8. Crit chance, super-crit and lifesteal are uncapped. IsSpecificValueCapped has no caller in the reconstruction. Paths that read a snapshot's TotalValue and call the static Stat.CalculateSpecificValue directly SKIP both the clamp and the PM/PF stage: editor bake SimulationConfig.BuildSpawnerBakeReport (annotated 'Original ELF0x13db16c', SimulationConfig.cs:114; CalculateSpecificValue at :124-138), Forecast BuildAutoBalancedEnemies ('Original 0x13f7da0', ForecastSnapshots.cs:150-153), and the runtime boss-tower/quest BossSpawnService.BakeStatsForFloor (BossSpawnService.cs:455-467, called from BossSpawnService.cs:83, BossTowerBossDetailsView.cs:243, and QuestMobSpawnerService.cs:95 via the 5-arg overload BossSpawnService.cs:494-496; reads stored SimulationData snapshots, not the live player). Snapshot TotalValue is captured via UnitCore.GetStatValue(type, true) (ForecastSnapshots.cs:100; lerped between chunks in SimulationData.cs:53), so it is the raw Value for every stat EXCEPT HealthRegen, which is already the (post-formula) specific value. The runtime SimulationSystem itself reads the live clamped SpecificValue through GetForecastStatSpecificValue (SimulationSystem.cs:339-347, :999, VA 0x13e686c -> UnitCore.GetStatSpecificValue(type, true)). The annotated native bake/forecast methods corroborate that the original calls a static CalculateSpecificValue(type, raw) there, i.e. the cap skip is original behaviour, not a reconstruction artefact.

**依赖图节点**：输入 `constant:Stat.CappedSpecificValueLimit`、`value:player.stat_raw[Evasion]`、`value:player.stat_raw[BlockChance]`、`value:player.stat_raw[BlockAmount]`、`value:enemy.stat_raw`、`value:player.stat_modifiers`、`value:enemy.stat_modifiers`；输出 `value:player.stat_specific`、`value:player.stat_specific[Evasion]`、`value:player.stat_specific[BlockChance]`、`value:player.stat_specific[BlockAmount]`、`value:enemy.stat_specific`、`value:enemy.stat_specific[Evasion]`、`value:enemy.stat_specific[BlockChance]`、`value:enemy.stat_specific[BlockAmount]`

**核验记录**（已写入上方字段）：

- 采纳 `notes (bake/forecast inputs)`（来自 `stats.specific_cap`）：Verified: ForecastSnapshots.cs:100 TotalValue = GetStatValue(type,true) (raw except HealthRegen); SimulationSystem.cs:339-347 and :999-1003 (VA 0x13e686c) read GetStatSpecificValue; annotations at SimulationConfig.cs:114 and ForecastSnapshots.cs:150 exist. Folded into notes and code_refs, with one refinement: BossSpawnService.BakeStatsForFloor is a runtime (boss tower / quest) path, not editor-only, so the skip is not limited to editor bake and forecast.
- 合并复核修正：Wrong code_ref path: [UI/StatDetailsPopupView.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/StatDetailsPopupView.cs) does not exist; the file is [UI/Stats/StatDetailsPopupView.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/Stats/StatDetailsPopupView.cs) (ColorizeIfCapped at :308). Also, QuestMobSpawnerService.cs:95 reaches BakeStatsForFloor through the 5-arg overload BossSpawnService.cs:494-496, which is missing from code_refs. Expression, status and correction resolution are otherwise verified against Stat.cs:17-110, SimulationSystem.cs:339-347/999-1003, ForecastSnapshots.cs:100/150-158, SimulationConfig.cs:114-138 and BossSpawnService.cs:444-467.

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

**依赖图节点**：输入 `value:player.stat_raw`、`value:player.stat_raw[Health]`、`value:player.stat_modifiers`、`value:enemy.stat_raw`、`value:enemy.stat_modifiers`；输出 `value:player.stat_specific`、`value:player.stat_specific[HealthRegen]`、`value:player.stat_specific[Health]`、`value:player.stat_specific[SuperCritChance]`、`value:enemy.stat_specific`、`value:enemy.stat_specific[HealthRegen]`

### `stats.modifier_merge`

**Modifier stacking / merge rule (StatBase.AddModifier)**

- 状态：**重建（无原生标注）**（`reconstructed`）
- 类型：`runtime_formula`
- 合并自：`stats.modifier_merge`、`combat.stat_modifier_merge`、`meta.modifier_merge_accumulation`

```text
AddModifier(n): e = first m in Modifiers with m.Name == n.Name && m.Source == n.Source && m.ModifierType == n.ModifierType. If none: Modifiers.Add(n); TemporaryModifiers := {m : m.Source == AbilityTemporary && m.Duration > 0}; OnModifierAdded(n). Else: if e.ModifierType in {PercentMultiplicative(2), PostFormulaMultiplicative(5)}: e.Value := e.Value * n.Value else e.Value := e.Value + n.Value (float32); if e.Duration <= n.Duration then e.Duration := n.Duration (= max); OnModifierUpdated(e); TemporaryModifiers is NOT rebuilt on this path. Finally UpdateValue() (Value, and SpecificValue for Stat). Param, StringParam and StatType are not part of the key; the existing entry keeps its own.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `v` | StatModifier.Value (float32) | incoming modifier n and existing entry e |
| `Name, Source, ModifierType` | merge key | StatModifier fields; reward handlers set names such as 'map.normal_mob', 'map.elite', 'map.tree', 'ads', sourcePath, 'quest.<step>', 'event.<key>.<type>', ability Def.Key |
| `Duration` | remaining seconds; merge keeps the max | incoming modifier (AddTemporaryModifierCommand, AddStatEffect) |

**输出**：Merged modifier list: one cumulative modifier per (Name, Source, ModifierType) on a stat, then recomputed Value / SpecificValue

**代码**：[Unit/StatBase.cs:63-85](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatBase.cs#L63)；[Meta/MetaStat.cs:97-117](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStat.cs#L97)；[Enemy/Commands/GrantSpawnerStatRewardCommandHandler.cs:17-37](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/Commands/GrantSpawnerStatRewardCommandHandler.cs#L17)；[Player/Systems/PlayerRewardSystem.cs:49-74](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/PlayerRewardSystem.cs#L49)；[Meta/SpawnerStatDropAmountCalculator.cs:11](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/SpawnerStatDropAmountCalculator.cs#L11)

**状态依据**：StatBase.AddModifier has no native VA comment. The annotated MetaStat hooks OnModifierAdded (0x1486634) and OnModifierUpdated (0x148681c, which updates only the saved Value of the entry matching Name/Source/Type) confirm the identity-merge contract. The main caller PlayerRewardSystem.GrantStatReward is annotated (0x1456084), but that is a different method.

**配置参数**：`EnemySpawner(Config).StatRewards[] (577 authored as Name 'Devoured' in Main.unity, renamed at grant)`；`StatModifier.Name / Source / ModifierType`；`Meta stat 'stat_drop' (SpawnerStatDropAmountCalculator)`；`VIP feature stat_multiplier (Chance, Multiplier)`；`Cheats DropMultiplier`；`DoubleLoot ability value`

**使用方**：stats.raw_value_aggregation；devour growth (PlayerRewardSystem.cs:74)；meta.devour_stat_grant；meta.grant_reward_stat；meta.rewarded_ad_stat_value；GrantRewardCommandRunner.cs:243；ShopLotModel.cs:286；items (PlayerModel.cs:795,822)；AddTemporaryModifierCommandHandler

**备注**：Devour accumulation: Source==Enemy rewards with an empty name or 'Devoured' are renamed before the grant to 'map.tree' (IsStatic), 'map.elite' (IsEliteBoss) or 'map.normal_mob' (GrantSpawnerStatRewardCommandHandler.cs:26,34-37), so devour growth lands in up to 3 Source-0 float32 modifiers per stat (Flat in the authored data), not one. Per-kill value v = reward * statDropMult(meta 'stat_drop', SpawnerStatDropAmountCalculator, VA 0x1488e3c) * count (SpawnerStatRewardMultiplier, if >1) * cheats.DropMultiplier * PROD over DoubleLoot abilities (1 + ability.GetValue()) * (vip.Multiplier with probability vip.Chance) (GrantSpawnerStatRewardCommandHandler.cs:27-29; PlayerRewardSystem.cs:54-72). Because the buckets are Flat, total stat = Base + SUM over all devours, and float32 accumulation loses precision at large totals. Suspected inconsistency: PostFormulaMultiplicative is merged by multiplying v but evaluated as (1+v), so two 0.1 grants give 1+0.01 instead of 1.21. Param is not in the key, so on a MetaStat two modifiers that differ only by Param would merge (IslandMastery avoids this with per-location names). Item modifiers are the shared ItemConfig objects (PlayerModel.cs:795), so a merge into them would mutate the asset at runtime. All AddTemporaryModifierCommand callers pass RemoveDuplicates=true, which removes the (Name, Source) match first, so the merge path is normally bypassed for temporary buffs.

**依赖图节点**：输入 `value:item.equip_stat_modifiers`、`value:ability.stat_modifier`、`value:player.upgrade_stat_modifier`、`value:rage.extra_health_modifier`、`value:reward.granted_stat_modifier`、`value:player.event_percent_factor`、`state:crystal_equipment`、`value:player.persisted_stat_modifiers`、`config:EnemySpawnerConfig.StatRewards[].ModifierType`、`state:boss_tower_context`；输出 `value:player.stat_modifiers`、`value:enemy.stat_modifiers`

**核验记录**（已写入上方字段）：

- 采纳 `notes / config_params (devour bucket)`（来自 `stats.modifier_merge`）：Verified at GrantSpawnerStatRewardCommandHandler.cs:19-37 and PlayerRewardSystem.cs:49-74: rename to 3 buckets, stat_drop multiplier, count, DropMultiplier, DoubleLoot (1+GetValue), VIP roll, then stat.AddModifier at :74. Notes and config_params now state this; 'all Flat' is qualified as true of the authored data because the copy keeps reward.ModifierType.

### `stats.modifier_source_persistence`

**Which modifier sources persist (save filters)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`lookup_table`
- 合并自：`stats.modifier_source_persistence`、`meta.metastat_persistence_filter`
- 原生地址：`0x145d45c (SaveStats), 0x145c268 (LoadStats), 0x1485ed4 (IsTemporarySource), 0x1485bf4 (BindToUserData), 0x1486634 (OnModifierAdded), 0x148681c (OnModifierUpdated), 0x1486c28 (OnModifierRemoved), 0x14866dc (WriteModifierToUd)`

```text
Player stats (PlayerModel.SaveStats): skip modifier if (uint)Source <= 10 && (0x50A >> Source) & 1, i.e. Source in {1 Item, 3 AbilityTemporary, 8 Crystal, 10 GraphUpgrade}; every other source is saved as (Name, Value, ModifierType, Source) plus the stat's BaseValue. LoadStats appends saved modifiers with stat.Modifiers.Add (no merge), keeps BaseValue from PlayerConfig, and restores no Param/StringParam/Duration. Meta stats (MetaStat.IsTemporarySource): temporary(src) = (uint)src < 16 && (0xEF0A >> src) & 1, i.e. src in {1 Item, 3 AbilityTemporary, 8 Crystal, 9 CrystalUpgrade, 10 GraphUpgrade, 11 MineUpgrade, 13 CrystalPass, 14 SoulPass, 15 Vip}; temporary modifiers are never written on add/update/remove and are stripped from saved data in BindToUserData; persisted ones are saved with Param and StringParam.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Source` | ModifierSource enum (0 Enemy, 1 Item, 2 AbilityPermanent, 3 AbilityTemporary, 4 Shop, 5 Ads, 6 Reward, 7 LevelPass, 8 Crystal, 9 CrystalUpgrade, 10 GraphUpgrade, 11 MineUpgrade, 12 CrystalEvent, 13 CrystalPass, 14 SoulPass, 15 Vip, 16 SecretChest, 17 CrystalGuardian, 18 Quest, 19 Cashback) | StatModifier.Source (Unit/ModifierSource.cs) |

**输出**：which stat bonuses are saved versus re-derived on load, i.e. which part of the aggregate is permanent

**代码**：[Player/Models/PlayerModel.cs:654-675](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L654)；[Player/Models/PlayerModel.cs:624-637](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L624)；[Meta/MetaStat.cs:135-139](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStat.cs#L135)；[Meta/MetaStat.cs:49-66](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStat.cs#L49)；[Meta/MetaStat.cs:97-150](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Meta/MetaStat.cs#L97)；[Unit/ModifierSource.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/ModifierSource.cs)

**状态依据**：SaveStats cites 'original native source-selection and unsigned exclusion mask 0x50a'; IsTemporarySource cites 'Native ELF VA 0x1485ed4, unsigned range and exact original bit mask'; LoadStats, BindToUserData and the MetaStat save hooks are annotated.

**使用方**：player persistence；meta stats persistence；meta.upgrade_modifier_apply

**备注**：The two masks differ: on player stats CrystalUpgrade(9), MineUpgrade(11), CrystalPass(13), SoulPass(14) and Vip(15) modifiers ARE saved, while on meta stats they are treated as temporary and re-derived each session from upgrade levels / VIP. Permanent player sources include Enemy devour, AbilityPermanent, Shop, Ads, Reward, LevelPass, CrystalEvent, SecretChest, CrystalGuardian, Quest, Cashback. LoadStats appends saved modifiers with stat.Modifiers.Add, bypassing the AddModifier merge, so saved duplicates stay separate but sum identically; Duration is 0. Param and StringParam are lost for player stats: SaveStats never writes them (PlayerModel.cs:669-672) and LoadStats never reads them (:635), so any Param-keyed player modifier is reloaded without its Param. (IslandMastery's Param=locationNumber modifier lives on a MetaStat, whose save path does keep Param.) MetaStat.OnModifierUpdated rewrites only the saved Value of the matching (Name, Source, Type) entry.

**依赖图节点**：输入 `value:player.stat_modifiers`、`constant:PlayerModel.save_exclude_source_mask`、`constant:MetaStat.temporary_source_mask`、`state:save_counters`；输出 `value:player.persisted_stat_modifiers`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `stats.modifier_source_persistence`）：Verified at PlayerModel.cs:624-637 (Modifiers.Add with Name/Value/ModifierType/Source only) and 654-675 (no Param/StringParam written). Expression and notes now state it, with line numbers corrected to 669-672 and 635. The IslandMastery example was qualified: that modifier is on MetaStat island_medal_bonus (IslandMasteryModel.cs:170-177), whose save path writes Param, so the loss applies only to Param-keyed modifiers on player Stats.

### `stats.temporary_modifier_decay`

**Temporary modifier duration decay and expiry (UnitBuffSystem)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 合并自：`stats.temporary_modifier_decay`、`combat.temporary_modifier_expiry`
- 原生地址：`0x15ccda8, 0x15ccd80, 0x14681d4`

```text
Each frame, for each entity with UnitComponent + HasTemporaryModifiersTag, for each stat, for i from TemporaryModifiers.Count-1 down to 0: m.Duration -= dt; if m.Duration <= 0: stat.RemoveModifier(m) (Value/SpecificValue recomputed, TemporaryModifiers rebuilt), m returned to StatModifierPool; if stat.Type == VisualScaleMultiplier and the entity has ScaleComponent: transform.DOKill(); DOScale(ScaleComponent.OriginalScale * (float)stat.Value, 0.35 s) with Ease 30. If no stat has temporary modifiers left, remove the tag. TemporaryModifiers = {m : m.Source == AbilityTemporary && m.Duration > 0}. Applying (AddTemporaryModifierCommand): if RemoveDuplicates, remove the first modifier matching (Name, Source), ignoring ModifierType; then AddModifier(new pooled modifier) and set HasTemporaryModifiersTag.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `dt` | frame delta | runtime |
| `Duration` | remaining seconds of the modifier | AddTemporaryModifierCommand.Duration from AddStatEffect.Duration (when > 0), VikingsRage DebuffDuration, LavaCrab 5 s |
| `RemoveDuplicates` | replace instead of merge | true at every call site (AddStatEffect.cs:44, VikingsRageDebuffSystem.cs:71,76, LavaCrabExplosionSystem.cs:102) |

**输出**：lifetime of temporary buffs and debuffs on any stat (player or enemy)

**代码**：[Unit/Systems/UnitBuffSystem.cs:36-41](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitBuffSystem.cs#L36)；[Unit/Systems/UnitBuffSystem.cs:43-65](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitBuffSystem.cs#L43)；[Unit/StatBase.cs:63-93](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatBase.cs#L63)；[Player/Commands/AddTemporaryModifierCommandHandler.cs:14-22](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Commands/AddTemporaryModifierCommandHandler.cs#L14)；[Abilities/Effects/AddStatEffect.cs:43-46](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/AddStatEffect.cs#L43)；[BossTower/Systems/VikingsRageDebuffSystem.cs:67-79](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/Systems/VikingsRageDebuffSystem.cs#L67)；[BossTower/Systems/LavaCrabExplosionSystem.cs:102](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/Systems/LavaCrabExplosionSystem.cs#L102)

**状态依据**：UnitBuffSystem.UpdateTemporaryModifiers has 'Native ELF VA 0x15ccda8; reverse modifier iteration, return expired modifiers, clear exhausted tags', OnUpdate 0x15ccd80, the tween constant cites rodata 0x920ed8 = .35f, and AddTemporaryModifierCommandHandler.Execute is annotated 0x14681d4 (duplicate predicate 0x14684cc).

**配置参数**：`AddStatEffect.Duration / Source / ModifierType (ability defs)`；`VikingsBossComponent.DebuffDuration, DebuffPerRageLevel, ScaleStep`；`LavaCrab shock regen: HealthRegen PreAdditivePostMultiplicative +5 (x6) for 5 s`

**使用方**：abilities (AddStatEffect with Duration > 0)；bosstower debuffs (VikingsRage, LavaCrab)；stats.raw_value_aggregation；stats.specific_value_pipeline；[UI/ItemSlotView.cs:329](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/ItemSlotView.cs#L329) (remaining duration display)

**备注**：Only Source == AbilityTemporary modifiers with Duration > 0 expire; every other source is permanent until removed explicitly. AddStatEffect passes its configured Source, so a Duration>0 effect authored with another Source would be added through the command but never expire. With RemoveDuplicates=true (every current caller) the old (Name, Source) modifier is removed regardless of ModifierType and a fresh one is added, so the value and timer are replaced, not stacked. With RemoveDuplicates=false and the same (Name, Source, Type), AddModifier would merge: values ADD (stack) and Duration = max; the TemporaryModifiers cache is not rebuilt on a merge (StatBase.cs:73-83), only on add/remove. The expiry tween uses ScaleComponent.OriginalScale (not UnitCore.OriginalScale) with target = OriginalScale * stat.Value after removal. Ease 30 is OutBounce in DOTween's enum order.

**依赖图节点**：输入 `state:frame_delta_time`、`value:player.stat_modifiers`、`value:enemy.stat_modifiers`、`value:ability.stat_modifier`、`state:boss_tower_context`、`value:rage.level`、`constant:VikingsRageDebuffSystem.max_scale_reduction`、`constant:LavaCrabExplosionSystem.shock_regen_value`、`constant:LavaCrabExplosionSystem.shock_regen_duration`；输出 `value:player.stat_modifiers`、`value:enemy.stat_modifiers`

**核验记录**（已写入上方字段）：

- 采纳 `notes`（来自 `stats.temporary_modifier_decay`）：Verified at AddTemporaryModifierCommandHandler.cs:18-20 (removal by Name+Source only), StatBase.cs:65-84 (merge adds, Duration max, no TemporaryModifiers rebuild) and UnitBuffSystem.cs:53-57 (ScaleComponent.OriginalScale * stat.Value). The expression and notes now state this, and the combat member's claim that re-adding 'refreshes rather than stacks' via merge was replaced.

### `stats.stun_decay`

**Stun duration countdown and effects (BuffStunComponent)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 合并自：`stats.stun_decay`、`combat.stun_duration`
- 原生地址：`0x15cd31c, 0x15f2a50`

```text
Apply: stash.Set(entity, BuffStunComponent{Duration = d}) overwrites any existing stun (no max, no stacking), then ApplyStunAnimation(true) sets animator.speed = 0. Sources: (1) StunEffect.Execute: target = PlayerModel.Unit.CurrentTarget (return if null, independent of context.Target), d = context.State.GetValue(). (2) VikingsRageDebuffSystem.ApplyVikingsStun, run once per RageLevelUpEvent (after ApplyRageDebuff): d = (float)meta 'boss_tower_vikings'.Value; if d <= 0 return; else every alive entity with VikingsBossComponent + UnitComponent and no UnitDeadMarker (the Viking boss units only, not all enemies) is stunned. Each frame (UnitBuffSystem.UpdateStuns): Duration -= dt; if Duration <= 0: remove the component and call ((EnemyUnit)core).ApplyStunAnimation(false) (animator.speed = 1). While stunned, every enemy AI branch (ground, escaping ground, flying, escaping flying) returns early: no movement (the ground agent gets isStopped = true), no RetreatTimer or AttackCooldown decrement, no attacks. Player hit damage *= (1 + BonusDamageToStunned) when BonusDamageToStunned > 0 and the target has BuffStunComponent.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `Duration` | remaining stun seconds (BuffStunComponent.Duration, float) | Abilities/Effects/StunEffect.cs:18 (ability state value); BossTower/Systems/VikingsRageDebuffSystem.cs:54-60 (meta stat boss_tower_vikings, applied to alive Viking bosses on each rage level-up) |
| `dt` | frame delta | runtime |
| `BonusDamageToStunned` | player ability bonus while the target is stunned | PlayerModel (abilities); see powerups.stun_bonus |

**输出**：time an enemy cannot act; damage-bonus window

**代码**：[Unit/Systems/UnitBuffSystem.cs:67-71](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitBuffSystem.cs#L67)；[Unit/Components/BuffStunComponent.cs:5-8](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Components/BuffStunComponent.cs#L5)；[Abilities/Effects/StunEffect.cs:13-21](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Effects/StunEffect.cs#L13)；[BossTower/Systems/VikingsRageDebuffSystem.cs:34](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/Systems/VikingsRageDebuffSystem.cs#L34)；[BossTower/Systems/VikingsRageDebuffSystem.cs:38-45](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/Systems/VikingsRageDebuffSystem.cs#L38)；[BossTower/Systems/VikingsRageDebuffSystem.cs:52-63](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower/Systems/VikingsRageDebuffSystem.cs#L52)；[Enemy/EnemyUnit.cs:243-249](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L243)；[Enemy/EnemyUnit.cs:203](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyUnit.cs#L203)；[Enemy/EnemyAttackSystem.cs:91-100](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L91)；[Enemy/EnemyAttackSystem.cs:173](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L173)；[Enemy/EnemyAttackSystem.cs:266-267](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L266)；[Enemy/EnemyAttackSystem.cs:308](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy/EnemyAttackSystem.cs#L308)；[Services/Damage/DamageCalculator.cs:176-181](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Damage/DamageCalculator.cs#L176)

**状态依据**：UnitBuffSystem.UpdateStuns has the comment 'Native ELF VA 0x15cd31c; original enemy-only cast retained'. StunEffect.Execute is annotated 'Native 0x15f2a50'. The stun gating in EnemyAttackSystem, the VikingsRage stun and the DamageCalculator bonus are not annotated.

**配置参数**：`Ability defs (stun value / scaling)`；`Meta stat boss_tower_vikings`

**使用方**：combat.enemy_attack_cycle；combat.outgoing_damage_to_enemy (DamageCalculator.cs:176-181)；powerups.stun_bonus；[Abilities/Conditions/TargetStunnedCondition.cs:13](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Conditions/TargetStunnedCondition.cs#L13)；[Abilities/Vfx/Systems/StunVfxSystem.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities/Vfx/Systems/StunVfxSystem.cs)

**备注**：BuffType enum has only Stun=0. Stun cuts enemy DPS directly by pausing the attack cooldown, so the effective enemy attack period becomes AttackCooldown + time spent stunned. The ground branch also freezes while enemy.IsCasting. Re-stunning resets the timer to the new value even when it is shorter. The expiry casts to EnemyUnit, so a stun on a non-enemy unit would throw. EnemyUnit's per-frame animator update (EnemyUnit.cs:203) also forces speed 0 while isStunned. The boss_tower_vikings stun hits only the Viking boss units (VikingsBossComponent filter), on every rage level-up.

**依赖图节点**：输入 `state:frame_delta_time`、`value:ability.effective_value`、`value:player.auto_target`、`value:player.meta_stat`、`value:rage.level`、`state:boss_tower_context`；输出 `value:enemy.stun_duration`、`value:enemy.stunned_damage_multiplier`

**核验记录**（已写入上方字段）：

- 合并复核修正：(b)/(a) The merged expression and variable source say VikingsRage stuns 'all alive enemies'. VikingsRageDebuffSystem.aliveFilter (VikingsRageDebuffSystem.cs:34) is With<VikingsBossComponent>().With<UnitComponent>().Without<UnitDeadMarker>(), so only the alive Viking boss units are stunned. The stun is applied once per RageLevelUpEvent (OnUpdate :38-45) and is skipped entirely when boss_tower_vikings.Value <= 0 (:55-56). The merged entry leaves out the trigger and the <= 0 guard.

### `stats.stat_accessors`

**Stat lookup / accessor semantics (UnitCore)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`config_rule`
- 来源：合并核验
- 原生地址：`0x15cb8d4, 0x15cbb48, 0x15cbb94, 0x15cb14c, 0x15cbe30`

```text
BuildStatLookup: statLookup = new Stat[36]; for stat in Stats: if statLookup[(int)stat.Type] == null then statLookup[(int)stat.Type] = stat (first duplicate wins). TryGetStat(t) = statLookup[(int)t] != null. HasStat(t) = statLookup[(int)t] != null. GetStatSpecificValue(t, isFast=true) = stat == null ? 0.0 : (isFast ? stat.FastSpecificValue : stat.SpecificValue). GetStatValue(t, isFast=true) = stat == null ? 0.0 : (t == HealthRegen ? (isFast ? FastSpecificValue : SpecificValue) : (isFast ? FastValue : Value)). Damage = statLookup == null ? 0.0 : GetStatValue(Damage). IsDead = CurrentHealth <= 0.01 || IsDying.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `t` | StatType (0..35) | caller |
| `isFast` | read the cached Fast* fields (default true) instead of the reactive atoms | caller |
| `36` | fixed lookup size (StatType count) | constant UnitCore.cs:88 |
| `0.01` | death HP threshold | constant UnitCore.cs:53 |

**输出**：stat values seen by every system; IsDead flag

**代码**：[Unit/UnitCore.cs:43](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitCore.cs#L43),53,85-114,138-142；[Unit/StatType.cs:3-41](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatType.cs#L3)；[Player/Models/PlayerModel.cs:693](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs#L693),776,795,822；[Player/Systems/PlayerRewardSystem.cs:52](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/PlayerRewardSystem.cs#L52)；[Rewards/Commands/GrantRewardCommandRunner.cs:242](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/Commands/GrantRewardCommandRunner.cs#L242)；[Shop/ShopLotModel.cs:286](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ShopLotModel.cs#L286)；[UI/GemsHudOffer/GemsHudOfferPopupView.cs:105](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/GemsHudOffer/GemsHudOfferPopupView.cs#L105)

**状态依据**：Native ELF VA comments on BuildStatLookup (0x15cb8d4), TryGetStat (0x15cbb48), GetStatSpecificValue (0x15cbb94, tail calls verified), GetStatValue (0x15cb14c, 'regen deliberately uses its specific value'), HasStat (0x15cbe30). IsDead/Damage properties have no annotation.

**配置参数**：`Player Config: Stats list (which StatTypes exist on the player)`

**使用方**：all damage/regen/ability systems；stats.specific_cap；stats.damage_application；simulation (GetForecastStatSpecificValue, ForecastSnapshots TotalValue)

**备注**：GetStatValue returns the specific (post-formula, converted) value only for HealthRegen; every other stat returns the raw Value, so callers wanting converted values must use GetStatSpecificValue. Missing stats read as 0 via both accessors. Player Config contains StatTypes 0-9, 12-17, 20-31, 33, 34. It lacks MagicDamageFlat/Percent (10,11), MagicDefenceFlat/Percent (18,19), LifestealPercent (32) and VisualScaleMultiplier (35), so these read as 0 on the player. The earlier concern that MagicDamagePercent=0 would zero magic weapon damage is moot for current data: no asset uses DamageStatTypePercent=11. Weapons use 7/9/13 (x10 each); armor configs reuse the field with 15/17/21 (x18 each) and 27 (x4). The real hazard: any item, reward or shop StatModifier targeting a stat missing from Player Config (10,11,18,19,32,35) would throw InvalidOperationException, because these sites bypass statLookup/TryGetStat and use Unit.Stats.First(...): PlayerModel.cs:776 (RemoveModifier), :795 and :822 (AddModifier), PlayerRewardSystem.GrantStatReward (PlayerRewardSystem.cs:52, VA 0x1456084), GrantRewardCommandRunner.cs:242, ShopLotModel.cs:286 and GemsHudOfferPopupView.cs:105.

**依赖图节点**：输入 `config:PlayerConfig.Stats[].Type`、`constant:UnitCore.stat_lookup_size`、`value:player.stat_raw`、`value:player.stat_specific`、`value:player.stat_specific[HealthRegen]`、`value:enemy.stat_raw`、`value:enemy.stat_specific`、`value:player.current_hp`、`value:enemy.current_hp`、`constant:UnitCore.dead_hp_threshold`；输出 `value:unit.is_dead`

**核验记录**（已写入上方字段）：

- 采纳 `notes (Magic stat concern)`（来自 `stats.stat_accessors`）：Verified: DamageStatTypePercent counts over assets are 7:10, 9:10, 13:10, 15:18, 17:18, 21:18, 27:4 (no 11). Player Config lacks 10,11,18,19,32,35. PlayerModel.cs:795/822 (also :776 RemoveModifier) and PlayerRewardSystem.cs:52 (the correction said :51) use Stats.First. Notes rewritten, code_refs added with corrected file paths, and the other Stats.First call sites on the player listed.
- 合并复核修正：The code_refs added from the correction use wrong paths: [Player/PlayerModel.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerModel.cs) and [Player/PlayerRewardSystem.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerRewardSystem.cs) do not exist. The files are [Player/Models/PlayerModel.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Models/PlayerModel.cs) and [Player/Systems/PlayerRewardSystem.cs](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Systems/PlayerRewardSystem.cs). Line numbers verified: PlayerModel :693/:776/:795/:822 and PlayerRewardSystem :52 (VA 0x1456084). The Stats.First hazard list is also incomplete. The same unguarded First lookup on player stats appears in [Rewards/Commands/GrantRewardCommandRunner.cs:242](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards/Commands/GrantRewardCommandRunner.cs#L242) (generic reward grant), [Shop/ShopLotModel.cs:286](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Shop/ShopLotModel.cs#L286) and [UI/GemsHudOffer/GemsHudOfferPopupView.cs:105](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/GemsHudOffer/GemsHudOfferPopupView.cs#L105). Player Config stat types (0-9,12-17,20-31,33,34) and the DamageStatTypePercent counts were re-verified.

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

**依赖图节点**：输入 `config:PlayerConfig.Stats[].BaseValue`、`config:PlayerConfig.Stats[].Type`；输出 `value:player.stat_base`

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

**依赖图节点**：输入 `constant:StatTypeExtensions.percent_stat_mask`、`constant:StatTypeExtensions.specific_percent_stat_mask`、`constant:StatTypeExtensions.specific_stat_mask`；输出 `value:unit.stat_display_class`

### `stats.damage_application`

**HP application and cheat overrides**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 来源：合并核验
- 原生地址：`0x15cd810, 0x15cd754, 0x15cdf10, 0x15ce7e8, 0x15ceb94, 0x15ce89c, 0x15cd840`

```text
ApplyDamage: target is EnemyUnit ? ApplyDamageToEnemy : ApplyDamageToPlayer.
Enemy: OnPreHit trigger; result = DamageCalculator.Calculate(data) (HasFixedDamage NOT applied); if OneHitMode: result.Damage = GetStatValue(Health) * 0.9; dmg = OneDamageMode ? 1.0 : result.Damage; if FirstVulnerability present: dmg *= DamageModifier, component removed, IsEmpowered = true; if dummy target: RecordHit(dmg), CurrentHealth = GetStatValue(Health), player lifesteal, return; else maxHP = GetStatValue(Health); oldHP = CurrentHealth; wasDead = IsDead; CurrentHealth -= dmg; ProcessEnemyDeath; if !HasDodged: ApplyEnemyReflection; OnOneshot trigger if IsDead && !wasDead && oldHP >= maxHP - 0.0001; ApplyPlayerLifesteal.
Player: if target.IsDying return; result = DamageCalculator.Calculate(data); if HasFixedDamage: result.Damage = FixedDamage; dmg = GodMode ? 0.0 : result.Damage; if HasBlocked: bonus = (float)meta block_to_supercrit; if bonus > 0: BlockSuperCritChanceBonus += bonus; OnBlock trigger; if ThornsDamage > 0: ApplyThornsReflection (every hit, including dodged/blocked); if HasDodged: OnEvasion/OnEvasionProc triggers; marks = (int)meta evasion_mark; if marks > 0 && source is enemy && pierce weapon equipped: ApplyMark(marks); if dmg <= 0: show event, return; HitsTakenCount++; CurrentHealth -= dmg; ApplyLifesteal(source enemy) [enemy HP += LifestealFlat; += LifestealPercent * enemy Health, uncapped]; if IsDead && TryIgnoreLethalDamage: CurrentHealth = 1.0 (see combat.lethal_damage_ignore); if CurrentHealth <= 0.01 && !IsDying: ProcessPlayerDeath (CurrentHealth = max(CurrentHealth, 0), IsDying = true); else OnGetHitLowHealth trigger with Damage = dmg.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `block_to_supercrit` | meta bonus super-crit chance added per block (reset when a super-crit lands) | Meta Stats Config |
| `evasion_mark` | mark stacks applied to the attacker on dodge (requires equipped pierce weapon) | Meta Stats Config |
| `0.9` | OneHitMode fraction of max HP | constant UnitDamageSystem.cs:292 |
| `0.0001` | full-HP tolerance for the Oneshot trigger | constant UnitDamageSystem.cs:330 |
| `0.01` | player death HP threshold | constant UnitDamageSystem.cs:147 |
| `FixedDamage` | fixed incoming damage override (player path only) | WeaponHitEvent |
| `DamageModifier` | first-hit vulnerability multiplier | FirstVulnerability component (combat.first_vulnerability_multiplier) |
| `OneHitMode / OneDamageMode / GodMode` | debug cheat overrides | CheatsModel |

**输出**：CurrentHealth changes; super-crit bonus accumulation; on-hit triggers (OnBlock, OnEvasion, OnOneshot, OnGetHitLowHealth)

**代码**：[Unit/Systems/UnitDamageSystem.cs:97-153](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/Systems/UnitDamageSystem.cs#L97),155-193,286-334

**状态依据**：Native ELF VA comments on ProcessHitImmediate (0x15cd810), ApplyDamage (0x15cd754), ApplyDamageToPlayer (0x15cdf10), ApplyLifesteal (0x15ce7e8), ProcessPlayerDeath (0x15ceb94), TryIgnoreLethalDamage (0x15ce89c), ApplyDamageToEnemy (0x15cd840, 'original outgoing path does not apply HasFixedDamage').

**配置参数**：`Meta Stats Config: block_to_supercrit, evasion_mark`

**使用方**：combat；combat.incoming_damage_to_player；combat.outgoing_damage_to_enemy；combat.lethal_damage_ignore；combat.first_vulnerability_multiplier；combat.thorns_reflection；combat.enemy_reflection；combat.enemy_lifesteal_on_hit；combat.player_lifesteal_on_hit

**备注**：Lethal ignore (TryIgnoreLethalDamage, 0x15ce89c; full rule in stats.lethal_ignore): returns false immediately if LethalIgnoreChance <= 0 and !HasPerTargetLethalIgnore. The two modes are exclusive: if HasPerTargetLethalIgnore there is NO roll; success requires the source's id not in LethalIgnoreTargetIds and LethalIgnoreCooldown <= 0, and the id is then added. Otherwise success requires Random.value < LethalIgnoreChance. On success HP := 1.0 and BOTH HasPerTargetLethalIgnore := false and LethalIgnoreChance := 0 (and no damage number is shown for that hit). The attacking enemy's lifesteal is applied before the lethal check (line 144). Death clamps HP to max(HP, 0) (ProcessPlayerDeath, line 167). On the enemy path OneDamageMode overrides OneHitMode and is applied before the first-vulnerability multiplier; dummy targets are reset to full HP after recording the hit.

**依赖图节点**：输入 `value:player.outgoing_damage_per_hit`、`value:player.incoming_damage_per_hit`、`value:unit.hit_roll_outcome`、`state:cheats`、`constant:UnitDamageSystem.one_hit_mode_hp_fraction`、`value:enemy.stat_raw[Health]`、`value:enemy.first_hit_damage_multiplier`、`value:enemy.current_hp`、`constant:UnitDamageSystem.oneshot_full_hp_tolerance`、`value:player.current_hp`、`value:player.meta_stat[block_to_supercrit]`、`value:player.supercrit_bonus_from_block`、`value:player.meta_stat[evasion_mark]`、`value:player.equipped_items`、`value:enemy.max_marks`、`value:player.meta_stat[extra_mark]`、`state:rng`、`value:enemy.lifesteal_heal_per_hit`、`value:player.lethal_ignore_chance`、`constant:UnitDamageSystem.lethal_ignore_hp`、`constant:UnitDamageSystem.player_death_hp_threshold`、`value:player.hits_taken_count`；输出 `value:enemy.current_hp`、`value:player.current_hp`、`value:player.supercrit_bonus_from_block`、`value:enemy.mark_stacks`、`value:unit.is_dead`、`value:unit.ability_trigger_events`、`value:player.hits_taken_count`

**核验记录**（已写入上方字段）：

- 采纳 `notes (lethal ignore)`（来自 `stats.damage_application`）：Verified against UnitDamageSystem.cs:142-151, 167, 176-193: per-target mode has no roll and checks id set + cooldown, else Random.value < chance; both flags reset on success; lifesteal precedes the lethal check; ProcessPlayerDeath clamps HP to >= 0. Notes rewritten, expression and code_refs updated.

### `stats.player_movement_displacement`

**Player per-frame movement displacement (UnitMovement.Update)**

- 状态：**原生证据恢复**（`recovered_native`）
- 类型：`runtime_formula`
- 合并自：`stats.player_movement_displacement`、`combat.player_movement`
- 原生地址：`0x15cbf80, 0x15cc4fc, 0x15cc4a8`

```text
if !isPlayer or player.Unit.IsDying: return. speed = CanMove ? (float)GetStatSpecificValue(MovementSpeed) : 0; theta = atan2(cam.z - p.z, cam.x - p.x) * Rad2Deg + 90; d = RotateVector((speed*(input.x*dt), speed*(input.y*dt)), theta), where RotateVector(v, a) = (v.x*cos a - v.y*sin a, v.x*sin a + v.y*cos a); agent.Move((d.x, 0, d.y)). Facing: UpdateVisualTarget(); if visualTarget != null and alive: dir = target.pos - pos with dir.y = 0; if dir.sqrMagnitude <= 0 return (no rotation this frame); else rotate toward LookRotation(dir) at 460 deg/s (RotateTowards, 460*dt). Otherwise (no alive visual target), if |input| > 0, rotate toward LookRotation(movement) at 460 deg/s. Visual target = Unit.CurrentTarget if non-null and alive; otherwise the nearest alive enemy (3D squared distance), taken if dist^2 < 16 when none was held, or dist^2 <= 36 when one was held; else null.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `speed` | effective move speed (units/s); 4.6 at raw 1 | stats.specific_curve.movement_speed via UnitCore.GetStatSpecificValue (Player Config.Stats[Type=0]) |
| `input` | joystick vector, magnitude <= 1 | runtime Joystick.GetInput |
| `dt` | Time.deltaTime | runtime |
| `theta` | camera yaw relative to the player + 90 deg | Camera.main position vs player position |

**输出**：player displacement per frame (distance/time travelled, which feeds fight/move time, kiting and engagement speed)

**代码**：[Unit/UnitMovement.cs:38-66](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitMovement.cs#L38)；[Unit/UnitMovement.cs:68-93](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitMovement.cs#L68)；[Unit/UnitMovement.cs:95-102](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitMovement.cs#L95)；[Unit/UnitMovement.cs:13-15](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitMovement.cs#L13)；[Unit/UnitCore.cs:100-105](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/UnitCore.cs#L100)

**状态依据**：Update carries 'Native ELF VA 0x15cbf80; ARM64 confirms the vector return values ... and a rotation rate of 460 degrees/sec'. UpdateVisualTarget (0x15cc4fc) and RotateVector (0x15cc4a8) are also annotated.

**配置参数**：`Player Config.Stats[Type=0].BaseValue`

**使用方**：player movement；combat.animation_cast_point (same specific speed * |input| * 0.05, PlayerAttackSystem.cs:93)；simulation move-time assumptions (SimulationSystem.cs:410,631 use the same specific speed)

**备注**：Displacement is linear in specific speed, so the travel-time impact of a MovementSpeed change comes entirely from the concave curve. Radii 4/6 (VisualTargetEnterRadius/ExitRadius) are compared squared (16 strict, 36 inclusive), which gives hysteresis. When the visual target is exactly on the player horizontally, Update returns before any facing change (no fallback to movement facing). Apart from the cast-point shift and the camera height bonus (speed/5), this is the only gameplay use of the move-speed curve.

**依赖图节点**：输入 `value:player.stat_specific[MovementSpeed]`、`state:player_input`、`state:frame_delta_time`、`state:unit_positions`、`value:unit.is_dead`、`value:player.auto_target`、`constant:UnitMovement.rotation_speed_deg_per_sec`、`constant:UnitMovement.VisualTargetEnterRadius`、`constant:UnitMovement.VisualTargetExitRadius`；输出 `value:player.move_distance`

**核验记录**（已写入上方字段）：

- 合并复核修正：(b) The facing logic in the merged expression is wrong. The code (UnitMovement.cs:55-65) is: if the visual target is alive, compute the horizontal direction; if direction.sqrMagnitude <= 0, RETURN (no rotation at all); else rotate toward it. The movement-direction rotation happens only when there is no alive visual target. The merged text 'if visualTarget alive and sqrMagnitude > 0 ... else if |input| > 0 rotate toward movement' wrongly implies a fallback to movement facing when the target sits exactly on the player.

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
