# 跨系统（`cross`）

[返回公式清单总览](../FormulaCatalog.md)

## 公式索引

| ID | 名称 | 状态 | 类型 |
|---|---|---|---|
| [`balance.shield_value_sync_by_leader`](#balanceshield_value_sync_by_leader) | Shield armor values synced to a leader item (editor) | 编辑器工具缺失 | `editor_generator` |
| [`balance.evolution_autobalance_thresholds`](#balanceevolution_autobalance_thresholds) | Evolution auto-balance guard multipliers (unconsumed levers) | 编辑器工具缺失 | `editor_generator` |

## 公式

### `balance.shield_value_sync_by_leader`

**Shield armor values synced to a leader item (editor)**

- 状态：**编辑器工具缺失**（`editor_only_missing`）
- 类型：`editor_generator`
- 来源：完整性审查补充

```text
Editor rule, body absent. When SyncShieldValuesByLeader is true, the shield EvolutionArmorValues / ArmorBase rows are presumably copied or scaled from a 'leader' armor row (helmet or same rarity) during auto-balance, giving armor(shield, r, e) = armor(leader, r, e).
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `SyncShieldValuesByLeader` | generator flag | Combat Balance Config.SyncShieldValuesByLeader (1) |

**输出**：Shield armor rows in EvolutionArmorValues

**代码**：[Balance/CombatBalanceConfig.cs:80](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L80)

**状态依据**：The field has no consumer anywhere in CodeBase.

**配置参数**：`Combat Balance Config.SyncShieldValuesByLeader`；`Combat Balance Config.EvolutionArmorValues`

**使用方**：items.armor_evolved

**备注**：Also note that GetArmorBase and GetArmorGrowthPerLevel ignore damageType (they index by rarity only), so pre-evolution helmets and shields of equal rarity have identical armor.

**依赖图节点**：输入 `config:CombatBalanceConfig.SyncShieldValuesByLeader`、`config:CombatBalanceConfig.EvolutionArmorValues[].Value`、`config:CombatBalanceConfig.EvolutionArmorValues[].CopyValue`、`config:CombatBalanceConfig.EvolutionArmorValues[].DamageType`；输出 `config:CombatBalanceConfig.EvolutionArmorValues[].Value`、`config:CombatBalanceConfig.EvolutionArmorValues[].CopyValue`

### `balance.evolution_autobalance_thresholds`

**Evolution auto-balance guard multipliers (unconsumed levers)**

- 状态：**编辑器工具缺失**（`editor_only_missing`）
- 类型：`editor_generator`
- 来源：完整性审查补充

```text
Presumed generator constraints: copyValue_e >= MinCopyValueGrowthMultiplier * copyValue_{e-1}; copyValue share of the evolution step = CopyValueTargetShare; value_e >= MinEvolutionValueFromShardsMultiplier * (value gained from shards before evolution); armorValue_e >= MinArmorEvolutionValueFromPreviousEvolutionMultiplier * armorValue_{e-1}.
```

| 符号 | 含义 | 来源 |
|---|---|---|
| `MinCopyValueGrowthMultiplier` | min growth of copy value between evolutions | Combat Balance Config (1) |
| `CopyValueTargetShare` | target share of copies in evolution value | Combat Balance Config (0.5) |
| `MinEvolutionValueFromShardsMultiplier` | min evolution value vs shards | Combat Balance Config (2) |
| `MinArmorEvolutionValueFromPreviousEvolutionMultiplier` | min armor step ratio | Combat Balance Config (1.3) |

**输出**：EvolutionDamageValues / EvolutionArmorValues (Value, CopyValue)

**代码**：[Balance/CombatBalanceConfig.cs:17-23](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L17)；[Balance/CombatBalanceConfig.cs:217-226](../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Balance/CombatBalanceConfig.cs#L217)

**状态依据**：The four fields have no consumer, and ApplyEvolutionAutoBalance / FillRemainingEvolutionValues / SetEvolutionValue are empty.

**配置参数**：`Combat Balance Config.MinCopyValueGrowthMultiplier`；`Combat Balance Config.CopyValueTargetShare`；`Combat Balance Config.MinEvolutionValueFromShardsMultiplier`；`Combat Balance Config.MinArmorEvolutionValueFromPreviousEvolutionMultiplier`

**使用方**：balance.evolution_value_autobalance；balance.copy_value_rule

**备注**：If balance.evolution_value_autobalance already lists these four config_params, fold this entry into it. They can be validated empirically against the exported EvolutionCombatValue rows (ratio checks).

**依赖图节点**：输入 `config:CombatBalanceConfig.MinCopyValueGrowthMultiplier`、`config:CombatBalanceConfig.CopyValueTargetShare`、`config:CombatBalanceConfig.MinEvolutionValueFromShardsMultiplier`、`config:CombatBalanceConfig.MinArmorEvolutionValueFromPreviousEvolutionMultiplier`、`config:CombatBalanceConfig.EvolutionDamageValues[].Value`、`config:CombatBalanceConfig.EvolutionDamageValues[].CopyValue`、`config:CombatBalanceConfig.EvolutionArmorValues[].Value`、`config:CombatBalanceConfig.EvolutionArmorValues[].CopyValue`、`config:CombatBalanceConfig.EvolutionDamageShardsRequirements[].Value`、`config:CombatBalanceConfig.EvolutionArmorShardsRequirements[].Value`、`config:CombatBalanceConfig.CutDamageBaseByRarity`、`config:CombatBalanceConfig.CutDamageGrowthPerLevelByRarity`、`config:CombatBalanceConfig.PierceDamageBaseByRarity`、`config:CombatBalanceConfig.PierceDamageGrowthPerLevelByRarity`、`config:CombatBalanceConfig.SmashDamageBaseByRarity`、`config:CombatBalanceConfig.SmashDamageGrowthPerLevelByRarity`；输出 `config:CombatBalanceConfig.EvolutionDamageValues[].Value`、`config:CombatBalanceConfig.EvolutionDamageValues[].CopyValue`、`config:CombatBalanceConfig.EvolutionArmorValues[].Value`、`config:CombatBalanceConfig.EvolutionArmorValues[].CopyValue`
