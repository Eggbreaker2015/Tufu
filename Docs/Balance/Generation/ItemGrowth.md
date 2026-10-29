# 物品成长规则逆向（武器 / 护甲）

阶段 4，轨道 `growth`。目标：从 APK 里保留下来的输出数据，反推原团队生成武器/护甲数值的规则（生成器方法体在 APK 中是空的）。

**复现方式**

```
cd Balance && .venv/bin/python analysis/item_growth.py
```

脚本只读 `Balance/out/balance.sqlite`，结果写到 `Balance/out/analysis/growth/`：

| 文件 | 内容 |
|---|---|
| `summary.json` | 本文引用的全部数字（下文用 `summary.<路径>` 标注出处） |
| `generator_replay.csv` | 450 行进化值的逐行重放（预测值、存储值、起作用的约束） |
| `item_ladders.csv` | 每个物品在每次进化阈值前后的数值（运行时公式） |
| `dps_defense_recompute.csv` | 用运行时公式重算 Simulation Data 的 DpsData / DefenseData（三种物品状态变体） |
| `shards_capture_check.csv` | 碎片需求的“捕获”假设检验（逐级候选） |
| `weapon_heuristic_eps.csv` | 武器启发值拟合的逐行残差，`envelope_row` 标出样本内行 |

脚本是确定性的：连续两次运行，6 个输出文件的 md5 完全相同。分析对象是原始行为（`6af011f`，宠物功能关闭）。本轨道用到的 `ItemModel`、`EvolveItemCommandRunner`、`CombatBalanceConfig`、`BalanceInterpolationsConfig`、`SimulationSystem.CalculateDpsData/CalculateDefenseData` 都按 `6af011f` 版本读取。

结论分三类，下文逐条标注：

- **【精确复现】**：由代码或数据恢复出的公式，逐行重算后与数据完全一致。
- **【拟合规则】**：能解释数据的模型，同时给出参数个数、残差和例外。
- **【假设】**：尚未证实。

---

## 0. 结论速览

1. **【精确复现】进化值生成器。** 给定 `ReferenceHeuristicValue`（下文记作 H）和碎片表，下面的规则对 Stabbing / Cutting / Buckshot × Common…Legendary 的 450 行全部成立：`Value` 450/450，`CopyValue` 450/450，`ReferenceStatPercent` 450/450（`summary.generator.variants.full_rule`、`summary.generator.ReferenceStatPercent`）。
   ```
   Value[L]     = nice( max( H[L],
                             Value[L-1] + MinEvolutionValueFromShardsMultiplier · CopyValue[L-1] · Shards[L],
                             (armor only) MinArmorEvolutionValueFromPreviousEvolutionMultiplier · Value[L-1] ) )
   CopyValue[L] = max( MinCopyValueGrowthMultiplier · CopyValue[L-1],
                       nice( CopyValueTargetShare · (H[L] − Value[L-1]) / Shards[L] ) )
   Value[0]     = 进化前的 base（武器 DamageBase = 0，护甲 ArmorBaseByRarity[r]）
   CopyValue[0] = 进化前每份拷贝的成长（Damage/ArmorGrowthPerLevelByRarity）
   ReferenceStatPercent = Math.Round(100 · Value / ReferenceStatValue)        （银行家舍入）
   ```
   - `nice(x)`：保留"好看"的数。尾数 m < 4 时步长 0.1，4 ≤ m < 8 时步长 0.5，m ≥ 8 时步长 1。中点按"半数取偶"舍入。
   - 杠杆当前值：Shards 倍数 2，护甲前级倍数 1.3，`CopyValueTargetShare` 0.5，拷贝成长倍数 1。
   - 模拟期之后的等级（"填充行"）使用同一组公式，只是 H 固定为最后一个模拟等级的 `Value`。
   - **Mythical 不属于这个生成器**，它有自己的闭式（第 3.4 节）。
2. **【精确复现】`ReferenceStatValue`。** 它就是玩家在 `ReferenceTimeChunkIndex` 时刻的对应类型平坦属性 `TotalValue`：武器用 `<Type>DamageFlat`，护甲用 `<Type>DefenceFlat`。恰好落在快照日上的 16 行全部一致（16/16）。
3. **【拟合规则】武器的 `ReferenceHeuristicValue`，只描述后期渐近线。** `H ≈ k_t · M_r · AttackCooldown(物品) · ReferenceStatValue`，其中 `M_r = 1, 1.25, 1.5, 1.875, 2.5`，`k_t ≈ 1.0 / 0.8 / 0.573`。这 8 个参数是从 15 个"包络行"读出来的。样本外（其余 80 行）：tc ≥ 200 时残差中位 0.18%、最大 0.43%；全部样本外行中位 1.19%、最大 30.4%。前期偏高是**系统性**的（随 tc 单调衰减，Spearman −0.95），目前没有解释，不能当作噪声。
4. **【拟合规则】护甲的 H。** `H ≈ 0.80 · DefenceFlat`，没有稀有度溢价；同一目标时点的不同稀有度共用一个 H。
5. **【拟合规则】碎片需求。** `Shards[L] ≈ nice(T_L 时刻处于 L−1 级的同稀有度物品的拷贝数)`。预先指定的单一规则"取拷贝最多的那件"：武器 35 级中 28 级误差 ≤ 5%（中位 2.3%，最大 16.1%），护甲 30 级中 23 级（中位 2.9%，最大 22.1%）。平台期就是模拟时间窗的尽头（85/85）。
6. **【拟合规则，仅描述趋势】成长曲线。** 模拟期内的进化值大致是目标时点的幂律，指数中位 2.56，R²(log) 中位 0.995。但单点最大相对误差中位 18%、最大 40%，所以只能算粗略趋势，不是生成规则；真正的规则是第 1 条里"跟着玩家属性走"。模拟期之后，武器等差增长（+2·CopyValue·Shards），护甲按 ×1.3 等比增长。
7. **【假设】`ArmorBaseByRarity` 是由模拟自动写入的**，不是手工数据（代码里有 `TryApplyArmorBaseAutoBalance`、`TrySetArmorBaseValue`、`ResetArmorBaseValues`，方法体都是空的）。数据只给出弱支持（第 6.1 节）。
8. **【假设】DpsData / DefenseData 重算只做到近似一致，原因未定。** "快照 Items 比 DpsData 新"这一解释已被检验否定为唯一原因（第 2 节）。

---

## 1. 时间单位

**【精确复现】** 在 46/46 个 TimeChunk 快照上，`TimeChunkIndex == floor(TotalPlaytime / SpawnerChargesChunkMinutes)` 都成立，其中 `SpawnerChargesChunkMinutes = 1440`、`UseSpawnerCharges = 1`（`summary.units`）。这和 `SimulationSystem.GetBalanceTimingMinutes()`（`6af011f`，`UseSpawnerCharges` 分支 `Mathf.Floor(CurrentTotalPlaytime / SpawnerChargesChunkMinutes)`）一致。

- 所以 `ReferenceTimeChunkIndex`、`*EvolutionTargetTimings` 的单位是"1440 分钟总时长的一天"。
- `TimeChunkDurationMinutes = 30` 也参与这把时间尺：`GetSpawnerOptimumTimingForCurrentMode` 在 `UseSpawnerCharges && !IsDays` 时把 `OptimumTime`（分钟）除以 30，换算到同一个"天"刻度上。也就是说，设计假设"一天 = 30 分钟活跃游戏时间"。
- 模拟实际的活跃时长是每天 46.17 分钟（最后一个快照的 `Playtime / TimeChunkIndex`，`summary.units.active_minutes_per_chunk_last`），比设计假设的 30 分钟多约一半，值得 timeline 轨道留意。
- 最后一个快照的 `Playtime / TotalPlaytime = 0.032`。"TotalPlaytime 包含离线时间"是由这个比值**推断**的，代码里 `ExecuteTick/Tick` 方法体为空，无法直接确认。
- `*EvolutionTargetTimings` 本身如何生成，见 `IslandTimeline.md`（timeline 轨道），这里不重复。

---

## 2. 运行时闭式公式（原始代码，`6af011f`）

**【精确复现：来自代码】** 来源是 `ItemModel.CalcDamage/CalcArmor/CalcEvolutionDamage/CalcEvolutionArmor`、`CombatBalanceConfig.Get*`，以及 `EvolveItemCommandRunner`（native 0x146851c）。

记号：t = `ItemConfig.DamageType`，r = `ItemConfig.Rarity`（是 Config 上的稀有度，不是显示用的 Divine），c = `Copies`，L = `Level`（进化等级），`Lc = min(L, MaxEvolutionLevel = 15)`。

```
武器（float32 计算）:
  未进化:  Damage = DamageBase[t][r] + DamageGrowth[t][r] · c + FlatDamage          (DamageBase 全为 0)
  已进化:  Damage = EvoDmg[t][r][Lc].Value + EvoDmg[t][r][Lc].CopyValue · c + FlatDamage
  数组选择: Buckshot → Smash*，Cutting → Cut*，其它（Stabbing/Magic/Universal）→ Pierce*
护甲:
  未进化:  Armor = ArmorBase[r] + ArmorGrowth[r] · c                                 （忽略 t，没有 flat 项）
  已进化:  Armor = EvoArm[t][r][Lc].Value + EvoArm[t][r][Lc].CopyValue · c
进化:
  LevelsToEvolve = Shards[isArmor][r][min(Level + (Evolved ? 1 : 0), 15)]
  Evolve: Copies -= LevelsToEvolve;  首次进化令 Evolved = true（Level 保持 1），之后每次 Level++
DPS = Damage / AttackCooldown
```

统一写法：如果把"未进化"看作第 0 级，令 `V[0] = Base`、`CV[0] = Growth`，那么任意状态都是 `stat = V[L] + CV[L]·c (+FlatDamage)`。第 3 节的生成器正是把第 0 级当作递推起点。

**进化瞬间的跳变**（`item_ladders.csv`）：恰好在阈值处进化时，`进化后(c=0) / 进化前(c=Shards)` 的比值在 690 个（物品, 等级）组合上为：最小 1.022，中位 1.235，最大 8.4（`paddle` 第 1 级：2500 → 21000）（`summary.runtime.evolve_jump_ratio_at_threshold`）。

### 2.1 DpsData / DefenseData 重算：【假设】近似一致，原因未定

方法：用上面的公式加 `CalculateDpsData/CalculateDefenseData`（`6af011f`），重算 46 个快照的 DpsData / DefenseData（`summary.runtime.dps_recompute`、`defense_recompute`，逐行在 `dps_defense_recompute.csv`）。

各输入的可信程度：

- **系数 K：精确。** 代码用的是 `GetInterpolationCoefficient(GetBalanceTimingMinutes(), StatModifier, Rare)`。脚本按 `BalanceInterpolationsConfig.GetTotalCoefficient`（StatModifier 为连乘）和 `BalanceInterpolation.Evaluate` 从 Interpolations 表直接算出 K。它与快照 `Health.PercentMultiplier` 在 46 个快照上的最大差值为 0（`summary.runtime.stat_modifier_coefficient_vs_health_percent_multiplier`），所以 K 不是误差来源。
- **平坦 / 百分比属性：代理值。** 代码用的是 `GetForecastStatValue(type) = Unit.GetStatValue(type, true)`，这个值没有被存下来，脚本用快照的 `TotalValue` 代替。
- **槽位数：拟合值，不是观测值。** `UnlockedWeaponSlots / UnlockedArmorSlots` 不在快照里。脚本对每个快照取让误差最小的 N（1–8），得到武器槽 1→2→3→4（第 0/1/6/24 天变化）、护甲槽 1→2（第 15 天变化）。这是自由参数。
- **进化状态：推断值。** 快照只存 `(Level, Copies)`，脚本把"首次出现 Level ≠ Copies 之后"视为已进化。

三种物品状态变体的结果：

| 变体 | DPS：完全一致 / 最大误差（天） / ≥100 天最大 / 最后快照 | Defense：完全一致 / 最大误差（天） / ≥100 天最大 / 最后快照 |
|---|---|---|
| A 当前快照 Items | 1 / 0.228（19） / 0.0121 / 0.00132 | 9 / 0.879（9） / 0.0097 / 0.00115 |
| B 去掉自上个快照以来新增的物品 | 1 / 0.929（1） / 0.0121 / 0.00132 | 9 / 0.748（17） / 0.177 / 0.00115 |
| C 用上一个快照的 Items | 0 / 0.967（1） / 0.156 / 0.0988 | 8 / 0.748（17） / 0.215 / 0.146 |

部分天的 DPS 误差（A / B / C，`summary.runtime.dps_recompute.per_chunk_examples`）：第 19 天 0.228 / 0.042 / 0.154；第 21 天 0.179 / 0.179 / 0.169；第 24 天 0.119 / 0.119 / 0.082；第 6 天 0.046 / 0.080 / 0.136。

结论：

- 去掉新物品只修好了第 19 天（0.228 → 0.042），没有把它降到 0；第 21、24 天没有新武器，误差不变；整体上 B、C 都比 A 更差。所以上一版"快照 Items 比 DpsData 新，属于数据问题"的说法**不成立**，至少不是唯一原因。
- 剩下的候选原因，都未验证：槽位数 N 的拟合；`GetStatValue(type, true)` 与 `TotalValue` 的差别；进化状态的推断；DpsData 在两个快照之间的某个时刻计算（物品与属性不同步）。`MakeSnapshot` 方法体为空，无法从代码确认。
- 后期误差小（≥100 天 ≤ 1.2%），公式结构本身与代码一致，这一点不受影响。

### 2.2 表覆盖缺口（不会被触发）

- 39 个 `*_div` 配置（Rarity = 5，武器 15、护甲 24）在进化值表和碎片表里都没有对应行。但它们出现在快照 Items 里 0 次，出现在 `spawners.ItemReward` 里 0 次（`summary.runtime.coverage_gaps_usage`）。代码在运行时用 `Evolved && Rarity != Mythical` 计算 Divine，不查 Rarity = 5 的行。所以这是"未使用的配置"，不是运行时故障风险。
- Magic（t = 2）在两张进化值表里都是 0 行，但也没有任何 `ItemConfig` 的 `DamageType = Magic`（0 个），所以同样不会被查到。
- Universal（t = 4）在 r0–4 的 150 行都是占位值（Value = 1，CopyValue = 0，Reference* = 0），150/150。

---

## 3. 进化值生成器（`EvolutionDamageValues` / `EvolutionArmorValues`）

### 3.1 规则与验证

**【精确复现】** 规则见第 0 节。逐行结果在 `generator_replay.csv`，计数在 `summary.generator`。

| 规则变体 | 武器 Value | 武器 CopyValue | 护甲 Value | 护甲 CopyValue |
|---|---|---|---|---|
| 完整规则 | 225/225 | 225/225 | 225/225 | 225/225 |
| 去掉护甲 ×1.3 约束 | 225/225 | 225/225 | **111/225** | 225/225 |
| nice 中点改为"四舍五入" | **221/225** | 225/225 | 225/225 | 225/225 |
| 只做 nice(H)，不加任何下限约束 | **80/225** | 225/225 | **63/225** | 225/225 |

规则的几个要点：

- **nice 舍入**：尾数 <4 时步长 0.1，[4,8) 时步长 0.5，≥8 时步长 1。所以可取的值有 3.7、3.9（步长 0.1），4.5、7.5（步长 0.5），8、9、10（步长 1）。
  - 有 4 行刚好落在中点：Stabbing Rare E6 = 12M、Buckshot Uncommon E2 = 220000、Buckshot Uncommon E8 = 40M、Buckshot Epic E6 = 80M（`summary.generator.nice_rounding_ties`）。只有"半数取偶"（C# `Math.Round` 的默认行为）能把这 4 行全部复现。
- **`ReferenceStatPercent`**：半数取偶 450/450，四舍五入 439/450。有 18 行的 100·Value/ReferenceStatValue 恰好是 .5，例如 162.5 → 162、312.5 → 312、1062.5 → 1062。
- **`CopyValue` 用的是 H，不是取整后的 `Value`。** 例如 Cutting Uncommon E2：H = 105825.7，但存储的 Value = 150000（被下限约束抬高）。按 H 算出的原始值是 0.5·(105825.7 − 32000)/500 = 73.8，再被 `CopyValue[L-1] = 120` 兜底，得到 120 = 存储值。
- **第 1 级的递推起点是"第 0 级 = 进化前物品"。**
  - 护甲 Rare E1：原始值 0.5·(127922.8 − 27000)/160 → nice 后为 320，再被 `ArmorGrowth[2] = 350` 兜底，得到 350。
  - 护甲 Epic E1：0.5·(967646 − 310000)/190 → 1700。
  - 护甲 Legendary E1：0.5·(4856360 − 2500000)/160 → 7500。
  - 第 1 级 CopyValue 被进化前成长兜底的情况有 9 个护甲行，武器 0 行（`summary.generator.copyvalue_binding_counts`）。
  - 护甲的起点用的是**最终存储的** `ArmorBaseByRarity`。如果 ArmorBase 本身是模拟写入的（第 6.1 节），那么它必须在对应稀有度首次进化之前就已经是这个值，重放 225/225 与此相容。

**哪条约束决定了 `Value`**（`summary.generator.value_binding_counts`，护甲按 3 个类型各算一次）：

| 范围 | 由 H 决定 | 由 Shards 下限决定 | 由护甲 ×1.3 决定 |
|---|---|---|---|
| 武器，模拟期 | 79 | 16 | – |
| 武器，填充期 | 0 | 130 | – |
| 护甲，模拟期 | 48 | 42 | 0 |
| 护甲，填充期 | 0 | 18 | 117 |

设计含义：

- **Shards 下限**：`Value[L] − Value[L-1] ≥ 2 · CopyValue[L-1] · Shards[L]`。进化带来的增量至少是"被消耗掉的拷贝原本价值"的 2 倍，所以进化净增益至少等于消耗掉的拷贝价值，进化永远不会亏。
- **`CopyValueTargetShare = 0.5`**：本级增量的一半，平摊到为达到本级而消耗的每一份拷贝上，作为进化后每份剩余拷贝的价值。
- **`MinCopyValueGrowthMultiplier = 1`**：保证 CopyValue 单调不降。

**对目录（catalog）的修正建议**（本轨道不能改 catalog）：

- `balance.copy_value_rule` 说"舍入规则未知、可能有手改"；`balance.evolution_value_autobalance` 说"护甲 1.3 未严格执行、有手改"。实际上这 450 行全部由同一规则生成。
- 护甲最小步长比 1.23 来自粗网格舍入。例如 1.3 × 65M = 84.5M 落在 [8,10) 段（步长 1），舍入到 80M，比值变成 80/65 = 1.2308。

### 3.2 模拟期与填充期

H == `ReferenceStatValue` 的行是填充行（`FillRemainingEvolutionValues` / `FillRemainingEvolutionsByLastValue`）。

- 共 265 个填充行（武器 130、护甲 135）。265/265 满足"H = 最后一个模拟等级的 Value"；265/265 的 CopyValue 冻结在最后一个模拟值（`summary.generator.filled_rows`）。这是对规则的真实检验：填充行的 Value 完全由 Shards 下限 / ×1.3 决定。
- 每个系列的最后模拟等级，按 Common/Uncommon/Rare/Epic/Legendary 排列（`summary.reference.simulated_levels_horizon`）：

| 系列 | 最后模拟等级 |
|---|---|
| 武器 Stabbing | 10 / 8 / 7 / 6 / 4 |
| 武器 Cutting、Buckshot | 9 / 7 / 6 / 5 / 3 |
| 护甲（三种类型相同） | 10 / 8 / 6 / 4 / 2 |

- 填充期在武器上表现为等差阶梯（`summary.growth.evolution_ladders.*.filled_steps`）。例如 Stabbing Common E11–E15：每级 +4M。原始步长是 2·160·11000 = 3.52M，nice 后体现为 +4M。
- 填充期在护甲上表现为 ×1.3 等比阶梯：45 个步长比的中位数 1.304，最小 1.231，最大 1.615（`summary.growth.armor_filled_step_ratio`）。

### 3.3 护甲按类型同步（`SyncShieldValuesByLeader = 1`）

在 75 个护甲 (r, L) 单元中（`summary.generator.armor_sync_across_types`）：

- 三个类型的 Value / CopyValue / H / Shards 完全相同：75/75。
- `ReferenceTimeChunkIndex` 与 `ReferenceStatValue` 相同的只有 48/75。也就是说，各类型的参考字段大多是在各自物品进化时写入的，只有数值被同步。

### 3.4 Mythical：独立的闭式

**【精确复现】**（`summary.generator.mythical`）

- `Value = 50000·L·(L+1)`、`CopiesToEvolve = 10L`、`CopyValue = 5000`，各 15/15。
- 这些行被复制到 t ∈ {0,1,3,4} × {武器, 护甲}：120/120；两张碎片表 30/30。
- 来源推测是 `CombatBalanceConfig.FillMythicalEvolutionValues()` / `CreateMythicalEvolutionValues()`（方法体为空），不是第 3.1 节的生成器。理由：
  - **不在 nice 网格上**：15 个 Value 中只有 11 个在网格上。L = 11…14 的 6.6M / 7.8M / 9.1M / 10.5M，nice 后分别是 6.5M / 8M / 9M / 10M（`values_off_nice_grid`）。
  - **起点对不上**：若从进化前的 Mythical 物品起递推，L = 1 的 Shards 下限是 `0 + 2·CV[0]·10`，按类型分别为 Stabbing 32000、Cutting 48000、Buckshot 70000、护甲 100000，结果会随类型不同；而存储的 V1 在所有类型上都是 100000（`generator_shards_min_at_L1_from_pre_evolution`）。
- 唯一的相容之处：`CopyValue = 0.5·ΔV/Shards`（15/15），即 5000 = 0.5·100000L/10L。这只说明两者共用同一个"一半增量平摊到拷贝"的思路，不说明是同一个生成器。

---

## 4. Reference* 列是什么

### 4.1 `ReferenceStatValue`

**【精确复现】**（`summary.reference.stat_value_*`）

- 定义：武器为 `<Stab|Cut|Buckshot>DamageFlat.TotalValue`，护甲为 `<..>DefenceFlat.TotalValue`，取 `ReferenceTimeChunkIndex` 时刻的值。与 `ApplyEvolutionAutoBalance(isArmor, type, rarity, level, heuristicValue, currentStatValue, timeChunkIndex)` 的签名相容。
- 恰好落在快照日的 16 行：16/16 一致（float32 精度）。
- 同一 (种类, 类型, tc) 下的值唯一：178/178 组。
- 不在快照日的 162 个值：157 个落在相邻两快照之间；另外 5 个只落在其他类型防御属性的区间里，见第 7 节例外 3。

### 4.2 `ReferenceTimeChunkIndex` 与进化目标时点

**【精确复现 + 观察】** tc 就是模拟中该物品实际进化的那一天。

- 在快照可夹住的 183 行中，173 行可以看到"该类型、该稀有度的物品在 tc 两侧的快照之间进化到了 L"。剩下 10 行全是第 7 节例外 3 中被复制的护甲行；另有 2 行超出快照范围，无法检查（`summary.reference.timechunk_vs_snapshot_items`）。
- tc 从不早于目标时点：所有类型中 `tc < ceil(T)` 都是 0 行。

各类型准点进化（tc == ceil(T)）的情况（`summary.reference.timechunk_vs_target_timing`）：

| 系列 | 行数 | tc == ceil(T) | 滞后天数中位 | 滞后天数最大 |
|---|---|---|---|---|
| 武器 Stabbing | 35 | 29 | 0 | 5 |
| 武器 Cutting | 30 | 9 | 8 | 107.8 |
| 武器 Buckshot | 30 | 6 | 5.5 | 106.7 |
| 护甲 Stabbing | 30 | 15 | 0.92 | 94.75 |
| 护甲 Cutting | 30 | 10 | 8.5 | 28 |
| 护甲 Buckshot | 30 | 6 | 9.25 | 101.75 |

- 观察：Stabbing 武器几乎准点进化，其他类型后期滞后 40–108 天。27 行滞后超过 40 天，列在 `summary.reference.timechunk_lag_gt_40_days`。
- **【假设】** "碎片需求以 Stabbing 物品为基准捕获"能解释这种不对称，但检验结果不支持把它当作唯一规则：只用 Stabbing 物品时，武器误差分布与"取拷贝最多的那件"相当，但最大误差更大（第 5 节）。

### 4.3 `ReferenceHeuristicValue`（H）

**武器【拟合规则，只描述后期渐近线】**（`summary.reference.weapon_heuristic_fit`，残差在 `weapon_heuristic_eps.csv`）：

```
H ≈ k_t · M_r · AttackCooldown(该类型该稀有度的物品) · ReferenceStatValue · (1 + ε)
```

参数怎么来的：对每个 (t, r) 系列取 `min_L H/(cd·sv)`，这个最小值总是落在该系列的最后一个模拟等级上（15 个"包络行"，tc 274–405）。再除以同类型 Common 的值，得到经验稀有度倍数：

| 类型 | Common | Uncommon | Rare | Epic | Legendary |
|---|---|---|---|---|---|
| Stabbing | 1 | 1.2492 | 1.4980 | 1.8714 | 2.4946 |
| Cutting | 1 | 1.2495 | 1.4984 | 1.8720 | 2.4971 |
| Buckshot | 1 | 1.2495 | 1.4985 | 1.8723 | 2.4971 |
| 规则 `M_r` | 1 | 1.25 | 1.5 | 1.875 | 2.5 |

- 类型系数 `k_t`（min 比值 / M_r 的几何平均）：Stabbing 1.0025，Cutting 0.8020，Buckshot 0.5727。
- 参数个数：3 个 `k_t` + 5 个 `M_r`（再取整到 1/1.25/1.5/1.875/2.5），全部从 15 个包络行读出。三种类型的经验 M_r 彼此相差不超过 0.003，这一点是对"类型 × 稀有度 × 冷却"可分解结构的真实证据，即使它是样本内的。
- 所用冷却：pike 1.0、fork 0.92、pick 0.84、coolpike 1.3、chainsawpike 0.75、knife 1.3、minisaw 1.2、katana 1.5、doubleaxe 1.0、chainsaw 0.9、paddle 2.0、mailbox 1.9、hammer 1.8、mace 2.3、coolHammer 1.7。

残差 ε，区分样本内与样本外：

| 范围 | 行数 | 中位 | 最大 | 最小 |
|---|---|---|---|---|
| 包络行（样本内，定义参数用） | 15 | −0.02% | 0.12% | −0.10% |
| 样本外，全部 | 80 | 1.19% | 30.4% | 0.005% |
| 样本外，tc ≥ 200 | 17 | 0.18% | 0.43% | 0.005% |
| 样本外，tc < 50 | 18 | 8.3% | 30.4% | 4.9% |
| 样本外，滞后 > 40 天的行 | 7 | 0.14% | 0.25% | 0.06% |

- 上一版报告的"tc ≥ 200 中位 0.07%"统计的 32 行里包含了全部 15 个包络行（`eps_stats_tc_ge_200_incl_envelope`，中位 0.07%），不能当作拟合质量的证据。
- **ε 是系统性的，不是噪声**（`out_of_sample_eps_trend`）：
  - 随 tc 单调衰减：样本外 Spearman(ε, tc) = −0.949；log ε 对 log tc 的斜率 −1.90（R² 0.84，n = 80）。
  - 同一时期稀有度越高 ε 越小：tc 44–46 的 Common E3 为 6.5–12.1%，Uncommon E2 为 5.1–5.9%；tc 50–54 的 Epic E1 为 2.3–4.6%。
  - 最大的几行都是前期首次进化：Cutting Common E1（第 17 天）+30.4%，Stabbing Common E1（第 17 天）+19.2%，Buckshot Common E1（第 19 天）+16.8%，Stabbing Uncommon E1（第 26 天）+15.4%。
  - "加一个常数平坦项"的替代模型也不成立：`A = H − k·M·cd·sv` 在 pike 系列上从 559 增长到 31354 再回落到 17090，不是常数（`additive_alternative_A=hv-k*M*cd*sv(Common series)`）。
- 结论：这个模型只描述 `H/(cd·sv)` 的后期渐近值；前期的超额是一个未解释的系统项。
- 设计解读（只对后期成立）：进化后武器的"自身 DPS"`Value/cd ≈ k_t·M_r·玩家平坦攻击`，稀有度每升一级溢价 ×1.25 / ×1.2 / ×1.25 / ×1.333。这也解释了为什么 `ReferenceStatPercent ≈ 100·k_t·M_r·cd`：例如 coolpike 为 1.0·1.875·1.3 ≈ 2.44，模拟行的存储 pct 在 237–254 之间。
- 模拟行 `ReferenceStatPercent` 的中位数（`summary.growth.stat_percent_by_rarity(simulated rows)`）：武器 C/U/R/E/L 为 110.5 / 127 / 159 / 239.5 / 203.5；护甲为 80 / 75 / 69.5 / 77 / 73.5。
- H 跟随的是**实际进化时刻 tc** 的属性：即使是滞后 40 天以上的 Cutting/Buckshot 行，样本外 ε 也只有中位 0.14%、最大 0.25%。

**护甲【拟合规则】**（`summary.reference.armor_*`）：

- 同一目标时点 T 的不同稀有度共用同一个 H。在有多行共享 T 的 5 个时点中，3 个完全相等（T = 50、165、316.67），另外 2 个是例外（第 7 节例外 2）。所以护甲 H 依赖时间，没有稀有度溢价。
- 在"准点进化"（tc == ceil(T)）的 31 行上，H/sv 最小 0.763、中位 0.805、最大 0.913；其中 T ≥ 150 的 20 行为 0.8029–0.8089（中位 0.8036）。即 `H ≈ 0.80 · DefenceFlat`。这同样只是后期渐近值，前期偏高。

**【假设】**

- 武器 ε 与护甲 0.80 之外的前期正偏差，来源未知。一个常数平坦项已被排除；"H 取自比 sv 稍晚的时刻"尚未检验。
- `k_Buckshot ≈ 0.573` 是否就是 4/7 = 0.5714，未证实。
- 护甲 H 看起来依赖目标时点 T（不同稀有度在不同 tc 进化，却共用 H），武器 H 依赖 tc。
- `SimulationSystem.ApplyAutoBalanceForEvolution(ItemModel)` 的方法体是空的，以上都无法从代码确认。

### 4.4 `ReferenceShardsRequirement`

**【精确复现】** 570 个非占位行（含 Mythical）的 `ReferenceShardsRequirement` 都等于对应的 `Evolution*ShardsRequirements`（同一 rarity、同一 level），570/570。Universal r0–4 的 150 行为 0（`summary.runtime.reference_shards_vs_table`）。捕获规则见第 5 节。

---

## 5. 碎片需求曲线

`summary.shards.tables`，以下按 E1…到平台期列出（之后保持不变）：

| 系列 | Shards | 平台期起点 | 平台期前 log-log 拟合（对 T）：指数 / R² |
|---|---|---|---|
| 武器 Common | 100 390 800 1600 3000 3700 6000 10000 11000 | E9 | 1.58 / 0.982 |
| 武器 Uncommon | 130 500 1200 2200 2700 6000 7000 7500 | E8 | 1.53 / 0.966 |
| 武器 Rare | 130 500 1300 1800 4500 5500 6000 | E7 | 1.61 / 0.968 |
| 武器 Epic | 110 400 750 1700 2200 2500 | E6 | 1.54 / 0.965 |
| 武器 Legendary | 70 180 750 1200 | E4 | 1.74 / 0.978 |
| 护甲 Common | 70 130 600 1200 1600 3400 5000 8000 9000 | E9 | 1.82 / 0.980 |
| 护甲 Uncommon | 100 170 600 1200 2300 4000 5500 6000 | E8 | 1.76 / 0.984 |
| 护甲 Rare | 160 270 900 1800 2600 2900 | E6 | 1.68 / 0.961 |
| 护甲 Epic | 190 550 1000 1600 | E4 | 1.88 / 0.978 |
| 护甲 Legendary | 160 500 | E2 | – |
| Mythical | 10·L | – | – |

- 所有值都在 nice 网格上。
- 平台期：在所有类型中最后一个模拟等级之后的 85 个 (种类, r, L)，85/85 等于最后捕获值（`summary.shards.plateau_check`）。平台期就是模拟时间窗的尽头，不是设计上限。
- 达到平台期所需的累计拷贝：武器 C/U/R/E/L 为 36590 / 27230 / 19730 / 7660 / 2200；护甲为 29000 / 19870 / 8630 / 3340 / 660。

**【拟合规则】捕获假设：`Shards[L] = nice(拷贝数(T_L))`。** 拷贝数取自目标时点 T_L 时处于第 L−1 级的同稀有度物品（`TryCaptureShardsRequirement(list, rarity, level, items, isArmor, playtime, targetTiming)` 的签名正好是这些参数）。

检验方法（`shards_capture_check.csv`）：取 T 两侧的快照；对在区间内保持 L−1 级、或在区间内进化到 L 级（把消耗掉的 Shards 加回获得量）的每个候选物品，线性插值它在 T 的拷贝数。

**预先指定的单一规则**（每级只用一个值，不挑最优；`summary.shards.capture_single_reference_tests`）：

| 规则 | 种类 | 有候选的等级 | 误差 ≤ 5% | 误差 ≤ 15% | \|误差\| 中位 | \|误差\| 最大 |
|---|---|---|---|---|---|---|
| 任意类型中拷贝最多的那件 | 武器 | 35/35 | 28 | 34 | 2.3% | 16.1% |
| 只用 Stabbing 物品 | 武器 | 35/35 | 28 | 34 | 2.5% | 24.6% |
| 只用 Cutting 物品 | 武器 | 30/35 | 4 | 14 | 19.7% | 77.9% |
| 只用 Buckshot 物品 | 武器 | 30/35 | 10 | 15 | 14.7% | 76.2% |
| 任意类型中拷贝最多的那件 | 护甲 | 30/30 | 23 | 29 | 2.9% | 22.1% |
| 只用 Cutting 物品（取拷贝多的那件） | 护甲 | 30/30 | 16 | 18 | 4.2% | 63.3% |
| 只用 Stabbing 物品（取拷贝多的那件） | 护甲 | 26/30 | 9 | 15 | 7.3% | 66.6% |
| 任意类型中拷贝最少的那件 | 护甲 | 30/30 | 3 | 4 | 44.8% | 77.7% |

- 最可信的单一规则是"取拷贝最多的那件"：武器和护甲都只需一条规则。
  - 武器误差 > 5% 的 7 级都偏高、都在前期：Common E1 +16.1%、E2 +9.7%、E3 +10.4%，Uncommon E1 +5.7%、E2 +7.3%，Rare E1 +6.0%，Epic E2 +7.7%。
  - 护甲误差 > 5% 的 7 级：Common E1 +8.9%、E2 +22.1%、E3 +5.9%、E5 +11.5%，Uncommon E1 +5.5%、E2 +5.0%，Rare E2 +14.8%。
  - 前期一致偏高，可能说明捕获时刻略早于 T，或者拷贝增长不是匀速的；未验证。
- "只用 Stabbing 物品"对武器同样好（28/35），但最大误差更大：Common E2 −24.6%、Uncommon E5 −10.4%、Common E3 −9.2%、Epic E2 +7.7%。所以"以 Stabbing 物品为基准"只是**假设**，数据无法把它和"拷贝最多"区分开（前期拷贝最多的往往就是 Stabbing 物品）。
- 作为参考，"最多 6 个候选里取最优"的乐观上界（`summary.shards.capture_hypothesis`）：65 级中 59 级 ≤ 5%、64 级 ≤ 15%，52 级满足 `nice(插值) == Shards`，中位 1.38%，最大 17.7%。最优候选：武器 Stabbing* 22、Buckshot* 6、Cutting* 4、Stabbing 2、Buckshot 1；护甲 Cutting* 17、Stabbing* 11、Stabbing 1、Buckshot* 1（* 表示该物品在区间内进化）。

---

## 6. 成长比例

### 6.1 进化前的数组（`summary.growth.pre_evolution_arrays`、`summary.armor_base`）

| 数组（C..L） | 相邻比 | 几何拟合：每级倍率 / R²(log) | C→L | 来源分类 |
|---|---|---|---|---|
| Pierce 8 35 100 400 1600 | 4.375 2.857 4.0 4.0 | 3.68 / 0.998 | ×200 | 候选手工数据 |
| Cut 12 50 140 500 2400 | 4.167 2.8 3.571 4.8 | 3.63 / 0.996 | ×200 | 候选手工数据 |
| Smash 25 80 200 700 3500 | 3.2 2.5 3.5 5.0 | 3.34 / 0.990 | ×140 | 候选手工数据 |
| ArmorGrowth 100 200 350 500 2000 | 2.0 1.75 1.43 4.0 | 2.00 / 0.948 | ×20 | 候选手工数据 |
| ArmorBase 600 4500 27000 310000 2500000 | 7.5 6.0 11.48 8.06 | 8.09 / 0.998 | ×4167 | **模拟自动写入（生成器未知）** |

每份拷贝的类型比：Cut/Pierce = 1.5 / 1.43 / 1.4 / 1.25 / 1.5；Smash/Pierce = 3.125 / 2.29 / 2.0 / 1.75 / 2.19。

**`ArmorBaseByRarity`：【假设】由模拟自动写入。** 依据是 `6af011f` 的代码：

- `CombatBalanceConfig.TrySetArmorBaseValue(Rarity, float)`、`CombatBalanceConfig.ResetArmorBaseValues()`、`SimulationSystem.TryApplyArmorBaseAutoBalance(ItemModel)`，方法体都为空。
- 数组为 `[600, 4500, 27000, 310000, 2500000, 0, 0]`。r5/r6 为 0 更像被重置后未被写入，而 `ArmorGrowthPerLevelByRarity` 的 r5/r6 是 5000 / 5000。
- 5 个值都在 nice 网格上（5/5）。

检验假设"`ArmorBase[r] ≈ nice(k · DefenceFlat)`，取首次获得 r 稀有度护甲时的值，k ≈ 1"：快照只能把首次获得时刻夹在两个快照之间（`summary.armor_base.rows`）。

| 稀有度 | ArmorBase | 首个物品（类型） | 夹住的天数 | 落在哪些类型的 DefenceFlat 区间内（原值 / nice 后） | ArmorBase / 自身类型区间上端 |
|---|---|---|---|---|---|
| Common | 600 | iron_block（Cutting） | 8–9 | 无 / 无 | 1.26（Cutting 0–478） |
| Uncommon | 4500 | scarlet_edge（Stabbing） | 11–13 | 无 / Cutting | 2.12（Stabbing 0–2118） |
| Rare | 27000 | eagle_crest（Cutting） | 24–27 | Cutting / Cutting | 0.89 |
| Epic | 310000 | graveeye_visor（Stabbing） | 63–70 | Cutting、Buckshot / Cutting、Buckshot | 1.06 |
| Legendary | 2500000 | runekeeper_bastion（Stabbing） | 127–140 | Stabbing / 三种都在 | 0.93 |

- 落在某个类型原值区间内的有 3/5，nice 后有 4/5，落在首个物品自身类型原值区间内的只有 2/5（`summary.armor_base.counts`）。Common 的 600 高于任何类型在第 9 天的防御（最高 478）。
- 结论：量级与"首次获得时的玩家平坦防御"相符（比值 0.89–1.26，Uncommon 例外为 2.12），但用哪种类型、取哪个时刻、系数是多少都定不下来，所以只能算弱支持。

### 6.2 进化值阶梯（`summary.growth.evolution_ladders`、`fit_summary`、`fit_table`）

对模拟期内至少有 3 个等级的 19 个系列（15 个武器 + 4 个护甲）做拟合，同时给出 R²(log) 和单点最大相对误差：

| 模型 | R²(log) 中位 / 最小 | 最大相对误差 中位 / 最大 | 参数 |
|---|---|---|---|
| 按等级几何级数 `V ∝ q^E` | 0.977 / 0.939 | 0.40 / 0.98 | 每系列 2 个 |
| 按目标时点幂律 `V ∝ T^p` | 0.995 / 0.986 | 0.18 / 0.40 | 每系列 2 个；p 中位 2.56（2.41–2.75） |
| 按实际进化日幂律 `V ∝ tc^p` | 0.992 / 0.984 | 0.26 / 0.44 | 每系列 2 个 |

逐系列（几何 R² / 最大误差；幂律 T 的指数；幂律 T 的 R² / 最大误差；幂律 tc 的 R² / 最大误差）：

| 系列 | 几何(E) | p(T) | 幂律(T) | 幂律(tc) |
|---|---|---|---|---|
| 武器 Stabbing Common | 0.949 / 0.98 | 2.72 | 0.992 / 0.34 | 0.993 / 0.41 |
| 武器 Stabbing Uncommon | 0.945 / 0.90 | 2.75 | 0.988 / 0.40 | 0.990 / 0.37 |
| 武器 Stabbing Rare | 0.939 / 0.67 | 2.61 | 0.986 / 0.32 | 0.986 / 0.32 |
| 武器 Stabbing Epic | 0.953 / 0.51 | 2.47 | 0.991 / 0.22 | 0.991 / 0.22 |
| 武器 Stabbing Legendary | 0.970 / 0.40 | 2.50 | 0.995 / 0.18 | 0.995 / 0.18 |
| 武器 Cutting Common | 0.989 / 0.40 | 2.70 | 0.998 / 0.24 | 0.996 / 0.30 |
| 武器 Cutting Uncommon | 0.989 / 0.31 | 2.61 | 0.997 / 0.16 | 0.997 / 0.16 |
| 武器 Cutting Rare | 0.983 / 0.30 | 2.62 | 0.996 / 0.18 | 0.993 / 0.21 |
| 武器 Cutting Epic | 0.974 / 0.39 | 2.74 | 0.995 / 0.19 | 0.992 / 0.20 |
| 武器 Cutting Legendary | 1.000 / 0.01 | 2.49 | 0.995 / 0.12 | 0.997 / 0.11 |
| 武器 Buckshot Common | 0.982 / 0.47 | 2.46 | 0.996 / 0.29 | 0.992 / 0.32 |
| 武器 Buckshot Uncommon | 0.983 / 0.44 | 2.72 | 0.999 / 0.09 | 0.993 / 0.26 |
| 武器 Buckshot Rare | 0.977 / 0.49 | 2.69 | 0.997 / 0.14 | 0.989 / 0.27 |
| 武器 Buckshot Epic | 0.981 / 0.26 | 2.56 | 0.997 / 0.16 | 0.986 / 0.22 |
| 武器 Buckshot Legendary | 1.000 / 0.01 | 2.51 | 0.995 / 0.12 | 0.997 / 0.11 |
| 护甲 Common | 0.968 / 0.61 | 2.48 | 0.997 / 0.21 | 0.995 / 0.29 |
| 护甲 Uncommon | 0.971 / 0.59 | 2.49 | 0.994 / 0.25 | 0.990 / 0.44 |
| 护甲 Rare | 0.968 / 0.40 | 2.55 | 0.994 / 0.18 | 0.984 / 0.29 |
| 护甲 Epic | 0.978 / 0.19 | 2.41 | 0.990 / 0.15 | 0.986 / 0.22 |

- 幂律只是**粗略的描述性趋势**：单点误差常达 20–40%，不能用来生成数值。真正的生成规则是第 3 节的"跟随玩家属性 + nice + 下限"。
- Cutting / Buckshot Legendary 的模拟期只有 3 级，两个步长比分别是 4.92 / 5.0 和 5.0 / 5.08，所以几何拟合 R² ≈ 1。这是样本太少导致的退化情况。
- 幂律的来源：H ∝ 玩家平坦属性，而属性本身大致是时间的幂律（`summary.growth.player_flat_stat_curve`，第 17 天起），但同样有很大的单点误差：

| 属性 | 指数 | R²(log) | 最大相对误差 |
|---|---|---|---|
| StabDamageFlat | 2.86 | 0.988 | 0.51 |
| CutDamageFlat | 2.56 | 0.996 | 0.33 |
| BuckshotDamageFlat | 2.53 | 0.994 | 0.40 |
| StabDefenceFlat | 2.64 | 0.993 | 0.42 |

- **为什么对滞后类型来说 T 比 tc 拟合得更好**（例如 Buckshot Uncommon：对 T 的 R² 0.9991，对 tc 0.9931）？检验结果（`summary.growth.T_vs_tc_R2_comparison`）：
  - Value 偏好 T 的系列 12 个，偏好 tc 的 7 个；H 偏好 T 的 13 个，偏好 tc 的 6 个。
  - 存储的 sv（`ReferenceStatValue`）本身偏好 T 的 11 个，偏好 tc 的 8 个；H 与 sv 偏好同一时钟的有 17/19 个系列（15 个武器系列全部相同）。
  - 而且 H 精确跟随 sv(tc)（滞后行的样本外 ε 最大 0.25%，第 4.3 节）。
  - 所以"T 拟合更好"是被采样的玩家属性曲线本身的形状造成的（滞后物品在属性曲线变平的区段进化），不说明 H 依赖 T。这与"武器 H ∝ sv(tc)"不矛盾。
- 模拟期相邻等级比由大到小。例如 Stabbing Common 为 6.0, 3.57, 4.0, 2.67, 2.38, 1.95, 1.89, 1.57, 1.36。它们是"属性曲线在进化时点上的采样"，不是常数倍率。
- 稀有度之间：后期同一时点下，比例 ≈ `M_r·cd_r` 之比（见第 4.3 节）。
- 模拟期之后：武器等差（例如 Stabbing Common 每级 +4M，Cutting Common +7M/+5M），护甲 ×1.3 等比（中位 1.304）。

---

## 7. 例外（具名）

1. **H 被下限约束改写的模拟行**（这是规则本身的作用，不是手改；`summary.exceptions.simulated_rows_where_min_rule_changes_value`）。共 15 个武器行和 9 个护甲单元，下面写作 存储 Value（nice(H)），全部由 Shards 下限决定：
   - 武器 Stabbing：Common E9 11M（10M）、E10 15M（14M）；Epic E6 39M（38M）；Legendary E3 14M（12M）、E4 31M（25M）。
   - 武器 Cutting：Uncommon E2 150000（110000）、E3 450000（340000）。
   - 武器 Buckshot：Common E2 110000（60000）、E3 290000（190000）、E4 650000（550000）、E8 18M（17M）、E9 28M（26M）；Uncommon E2 220000（200000）、E7 29M（28M）；Rare E6 36M（34M）。
   - 护甲：Common E1–E4 为 15000 / 40000 / 160000 / 400000（H 只有 11000–340000）；Uncommon E1 45000、E2 110000；Rare E1 140000、E3 1.9M；Epic E4 15M。
   - 含义：前期进化前的拷贝价值（每份成长 × 碎片数）高于"属性比例"启发值，生成器优先保证进化不亏。
2. **护甲 H 不共享**（`summary.exceptions.armor_hv_not_shared_at_same_T`）：
   - T = 110：Epic E1 = 967646.2，Common E5 = 1013015.56。
   - T = 195.83：Legendary E1 = 4856360，Common E7 / Rare E4 = 4782581.5。
   - 两处都是 Epic / Legendary 护甲的首次进化，原因未知。【假设】首次进化取 H 的时刻不同；也可能与 ArmorBase 自动写入发生在同一时刻有关（第 6.1 节）。
3. **护甲参考字段跨类型共享**（`summary.exceptions.armor_reference_fields_shared_by_types`）。在 5 个单元里，Stabbing / Buckshot 行（有时连同 Cutting）共用同一组 (tc, sv)，而这个 sv 只落在 Cutting 或 Buckshot 防御属性的快照区间里，不在 Stabbing 的区间里：

   | 单元 | 共用 (tc, sv) 的类型 | 自有值的类型 | sv 落在哪些类型的区间 | 来源类型 | 是否该稀有度最后模拟级 |
   |---|---|---|---|---|---|
   | Common E9 | Stab、Cut、Buck（tc 317） | – | Cutting、Buckshot | 不确定（Cut 或 Buck） | 否（最后是 E10） |
   | Common E10 | Stab、Buck（tc 385，sv 22002254） | Cutting（tc 386，sv 22086082） | Cutting、Buckshot | Buckshot | 是 |
   | Uncommon E8 | Stab、Cut、Buck（tc 375） | – | Cutting、Buckshot | 不确定 | 是 |
   | Rare E6 | Stab、Buck（tc 355，sv 19487414） | Cutting（tc 371，sv 20828662） | Cutting、Buckshot | Buckshot | 是 |
   | Epic E4 | Stab、Cut、Buck（tc 336） | – | Cutting、Buckshot | 不确定 | 是 |

   - 当 Cutting 有自己的值时，共用值的来源只能是 Buckshot（Common E10、Rare E6）；其他 3 个单元无法区分 Cutting 和 Buckshot。
   - 快照显示对应的 Stabbing / Buckshot 物品在 tc 时并未达到该级（例如 `wooden_horns` / `oak_round` 在第 405 天仍是 8 级）。这 10 行正是第 4.2 节中 10 个不匹配的行。也就是说，这些参考字段是由某一次进化事件写给了多个类型。
4. **类型滞后**：武器 Cutting / Buckshot 后期滞后目标时点 40–108 天，最大是 Cutting Rare E6（T = 289.17 → tc 397，滞后 107.8 天）。目标时点只在 Stabbing 武器上基本准点。
5. **未使用的配置**：39 个 `*_div` 配置没有进化值行和碎片行，Magic 没有进化值行，Universal r0–4 的 150 行是占位值。由于没有物品或奖励会用到它们，这些都不会在运行时被查到（第 2.2 节）。
6. **DpsData / DefenseData 只近似一致**，原因未定（第 2.1 节）。

---

## 8. 局限与未解决的问题

- H 的精确公式：武器前期 ε（系统性，最多 +30%）的来源；`k_t`（1.0 / 0.8 / 0.573）的出处；护甲 0.80 的出处，以及护甲 H 取的是哪一种类型的属性。
- 碎片捕获：数据支持"取拷贝最多的那件"，但无法与"以 Stabbing 物品为基准"区分；捕获时刻是 T 还是首个 ≥ T 的 tick；前期一致偏高 5–22% 的原因。
- `ArmorBaseByRarity` 的生成器：量级与首次获得时的玩家防御相符，但类型、时刻、系数都未确定。
- Epic / Legendary 护甲首次进化 H 的偏差（第 7 节例外 2）。
- DpsData 重算误差的原因：槽位数、`GetStatValue(type, true)` 与 `TotalValue` 的差别、进化状态推断、计算时刻。
- 与评审意见的关系：本版接受了全部意见。具体改动：Mythical 改为独立闭式；ArmorBase 改为自动写入的假设；启发值残差改为样本外口径；DPS 重算的原因改为未定；例外 3 改为来源类型不确定；拟合表补上最大误差；碎片捕获加了单一规则检验；单位一节改写；覆盖缺口降级为未使用的配置。没有发现需要反驳的点。
