# 刷怪点烘焙复核：Health 与 Damage

用 Python 重写原团队给刷怪点定 Health / Damage 的烘焙管线（`SimulationConfig.BuildSpawnerBakeReport`，原生 VA 0x13db16c），在随包的 Simulation Data 上逐个重跑 717 个刷怪点，与场景值和 `SpawnerBakeInfos` 对比，并逐条检验不匹配的原因。分析对象是原版行为（提交 6af011f，宠物功能未加入）。

下文把随包的 Simulation Data 记为**数据源 A**，把烘焙天制场景值时实际用到的玩家状态记为**数据源 B**，把 `SpawnerBakeInfos` 日志对应的玩家状态记为**数据源 C**。B 和 C 是从结果反推出来的，它们是一次模拟、多次模拟还是改过配置后的同一次模拟，数据都无法判断（见第 6 节）。

## 结论

1. **公式本身已确认（精确复现）。** 分钟制（`IsDays=0`，L1–L3）刷怪点有 79 个 Health 和 Damage 都逐位复现，Damage 108 个中复现 104 个。天制场景值无法逐位复现，但同一烘焙时刻、同一被选护甲类型的刷怪点，其“场景 HP / 模型原始 HP”是同一个系数：127 个多成员组里有 125 组在取整误差内一致。其中真正有检验力的（组内 AverageFightTime、Count、抗性不全相同）是 92 组、406 个刷怪点、151 个独立约束，90 组一致。不一致的 2 组各含一个手调的整万 HP 刷怪点。
2. **天制场景值用的玩家状态（B）不是随包的 Simulation Data（A）。** 609 个天制刷怪点里 HP 逐位复现的只有 1 个，即 `Count=0` 的 BossTower（两边都是 `inf`）；Damage 复现 20 个，全部在烘焙时刻 < 5 天。少参数变换都解释不了：最优全局倍率对 HP 是 1.000（不改善），对 Damage 是 1.0055（1% 内从 176 升到 219，仍有 386/605 在 1% 外）；全局时间平移和时间缩放也不行；用 HP 和 Damage 分别反推的时间偏移相差中位数 0.444 天，而且逐岛相关系数正负都有，所以也不是单纯的时间轴变形。
3. **A 和 B 的分歧从约 2 小时游戏时间开始，先出现在进攻侧。** 分钟制 OT ≤ 120 分钟的刷怪点（不含活动、假人、HandBalance）78 个里 76 个 HP 逐位一致；121–240 分钟只有 3/22。这 22 个里，被选护甲为 Stabbing 的 3 个（OT 133、151、173）HP 和 Damage 都逐位一致；Cutting/Buckshot 的 19 个 HP 全部偏高（比值 1.0093–1.2964），Damage 有 18 个逐位一致。Damage 按 10 取整，逐位一致只说明防御侧需求落在 ±5 的取整带内，在这 18 个点上相当于场景 Damage 的 0.40%–3.33%。所以只能说：防御侧在取整精度内相同，Cut/Buckshot 的 DPS 不同。到天制 4–7 天，Stabbing 也已经不同：L6 的 5 个 `Spawner_vip_6` 在 4.0 天 Damage 逐位一致但 HP 比值 1.1275，L4 的 `Spawner_mid_cut_epic` 为 1.0792。7 天以后 Damage 也普遍不一致。
4. **手调值可以从数据里认出来（例外）。** 2 个 `HandBalance=1`，6 个活动怪（场景值是占位值，运行时被 `ApplyEventPlanToConfig` 覆盖），`Spawner_Dummy`（HP 1e9），以及 2 个整万 HP（`Spawner_1_epic_buck` 200000、`Spawner_elite_regen` 270000，偶然出现的期望个数 0.061）。`HandBalance` 并不覆盖所有手调点。
5. **`SpawnerBakeInfos` 是更早一次烘焙的日志（C），不是场景值的来源。** 它和场景的 Health 只有 7 个相等。它本身也与公式自洽（135/143 组），但晚期岛屿的玩家强度低得多：场景 HP / 日志 HP 的中位数在 L17 为 19.591。11 个 L1–L2 分钟制刷怪点的 OT 在写日志之后被改过，其中 10 个用当前 OT 逐位复现、用日志 OT 一个都不复现，所以场景的最后一次烘焙晚于日志（前提是 OT 没有改过又改回）。

## 复现方法

在 `Balance/` 目录执行：

```bash
.venv/bin/python analysis/bake_check.py
```

约 9 秒。只读 `Balance/out/balance.sqlite`，输出写到 `Balance/out/analysis/bake/`：

| 文件 | 内容 |
|---|---|
| `spawners.csv` | 每个刷怪点一行：key、岛屿、分类、`IsDays`、OptimumTime、重算时间、所用快照、被选护甲、预测与场景的 Health/Damage 及比值、匹配等级、`bake.*`、各种标志、反推时间、解释代码 |
| `groups.csv` | 同一（模式、烘焙时刻、被选护甲类型）的分组：场景 HP / 模型原始 HP 的比值、组内一致性、组内不同输入组合个数 `n_distinct_inputs`、涉及岛屿 |
| `summary.json` | 本文所有数字 |

连跑两次输出逐字节相同。本文数字都来自 `summary.json`，括号里给出键名。

## 1. 烘焙管线（Python 模型）

代码在 `Balance/tufu_balance/model/`，每个函数的文档字符串都写明对应的 C# 文件和行号。C# 中声明为 `float` 的量每步都按 binary32 取整，声明为 `double` 的按 binary64 计算。

| 模块 | 移植自（6af011f） | 内容 |
|---|---|---|
| `bake.py` | `Simulation/SimulationConfig.cs:103-239` | `GetReCalcOptimumTime`、`GetSnapshotForBake`、`BuildSpawnerBakeReport`、`CalculateBakedSpawnerHealth` |
| `snapshots.py` | `Simulation/SimulationData.cs:13-86` | `GetByPlayTime`（最近快照，稳定排序取先出现者）、`GetByTimeChunkIndex` 与 `InterpolateSnapshot` |
| `interpolation.py` | `Balance/BalanceInterpolation.cs:23-64`、`BalanceInterpolationsConfig.cs:12-27`、`CombatBalanceConfig.cs:135-145` | 分段线性曲线、类型 1/3 连乘、其余相加 |
| `defense.py` | `Services/Damage/DamageCalculator.cs:24-62, 269-280` | `ApplyDamageDefense`、倍增加二分求逆、`GetBattleRegen`、武器类型到防御属性的映射 |
| `stats.py` | `Unit/Stat.cs:51-110` | `CalculateSpecificValue` 各曲线、0.8 上限、`UpdateValue` 管线 |
| `data.py` | — | 从 SQLite 读取上述输入；检查 Simulation Config 自己的快照列表为空，不为空就报错 |

单个刷怪点的计算（`SimulationConfig.cs:113-160`）：

```
t     = GetReCalcOptimumTime(spawner)       # L >= 11 且 Rel < 0.5 时减去 (ITD[L-1]-ITD[L-2])*(0.5-Rel)*1，分钟制再 *60
snap  = IsDays ? GetByTimeChunkIndex(t) : GetByPlayTime(t)
day   = IsDays ? t : t/60
armor = argmax_a DpsData[a.Type] * (1 - a.Resist)       # 稳定降序；用未截断的 Resist
r     = clamp(armor.Resist, -0.9, 0.9)
P     = (1 + K4(day)) * DpsData[armor.Type];  Peff = (1-cc)*P + cd*cc*P     # cc, cd 为 CritChance / CritDamagePercent 的 specific 值
Health = Math.Round(Peff * AverageFightTime / Count * (1 - r))
D     = Difficulty <= 0 ? 1 : Difficulty;   n = min(4, (Count+1)/2)
need  = ((D+1)*HealthRegen + (2D+3)*LifestealFlat + HP*D/AverageFightTime) / n * AttackCooldown/(1-ev) / (1 - bc*ba)
a     = 求最小攻击使 ApplyDamageDefense(a, Def[PreferredWeaponType(L)]) / (1 + K5(day)) >= need   # 先倍增到上界，再二分到间隔 <= 1，返回上界
Damage = Math.Round(max(10, a)/10) * 10
```

数据层面的事实（`data_facts`）：

- Simulation Config 自己的 `Snapshots` 和 `TimeChunkSnapshots` 都是 `[]`（`config_snapshot_lists_empty`，从数据库读出，不是写死的），烘焙回落到 Simulation Data 的 46 个按天快照（下标 0–11, 13, 15, …, 405）和 63 个按游戏时间的快照（Playtime 0–248.13 分钟）。
- 两个列表里所有快照都满足 `TimeChunkIndex = floor(TotalPlaytime / 1440)`（46/46，63/63），与 `SpawnerChargesChunkMinutes = 1440` 一致。所以天制烘焙用的“天”是 1440 分钟总时长（含离线）的序号，不是 `TimeChunkDurationMinutes = 30` 分钟。对应的在线 Playtime：第 1 天 63.28 分钟，第 4 天 248.13，第 10 天 350.08，第 21 天 744.80，第 104 天 4713.02，第 405 天 18699.48。
- 分钟制时 `day = t/60`，只用来查能力曲线。AbilityDPS 和 AbilityEHP 的第一个节点都在 20 天、系数 0，所以 20 天以内两条曲线都是 0（`ability_dps_at_day`：4 天、20 天为 0，90 天 0.4，150 天 1.3）。分钟制刷怪点的这个单位换算对结果没有影响。
- 没有刷怪点的 Difficulty ≤ 0（`difficulty_fallback`），回落到 1 的分支在随包数据里从未触发。
- 关联：`spawners` 有 717 行，日志 `SpawnerBakeInfos` 有 712 行，按 Key 关联上 713 个刷怪点，因为 `Eventspawner_Boss_79e49ca7fb46ec243a029656b3fa8507` 这个 Key 在场景里有 2 个刷怪点（`duplicate_spawner_keys`；日志里没有重复 Key）。

## 2. 精确复现情况

“1% 内”“5% 内”都是累计数（含逐位相同的）。比值 = 场景值 / 预测值。

### 按模式

| 范围 | 个数 | HP 逐位 | HP 1% 内 | HP 5% 内 | Damage 逐位 | Damage 1% 内 | Damage 5% 内 | 两者都逐位 |
|---|---|---|---|---|---|---|---|---|
| 全部 | 717 | 80 | 245 | 558 | 124 | 281 | 506 | 79 |
| 分钟制 | 108 | 79 | 82 | 88 | 104 | 104 | 105 | 79 |
| 天制 | 609 | 1 | 163 | 470 | 20 | 177 | 401 | 0 |

（`overall`、`by_mode`）

### 按岛屿

| 岛 | 个数 | HP 逐位 / 1% / 5% | HP 比中位数 | Damage 逐位 / 1% / 5% | Damage 比中位数 |
|---|---|---|---|---|---|
| 1 | 35 | 26 / 26 / 26 | 1.0000 | 33 / 33 / 33 | 1.0000 |
| 2 | 28 | 26 / 27 / 27 | 1.0000 | 26 / 26 / 26 | 1.0000 |
| 3 | 46 | 27 / 29 / 35 | 1.0000 | 45 / 45 / 46 | 1.0000 |
| 4 | 31 | 0 / 1 / 1 | 1.2344 | 15 / 27 / 29 | 1.0000 |
| 5 | 37 | 0 / 0 / 37 | 1.0404 | 0 / 0 / 0 | 1.1341 |
| 6 | 54 | 0 / 8 / 49 | 1.0276 | 5 / 5 / 6 | 1.1684 |
| 7 | 44 | 0 / 34 / 44 | 1.0002 | 0 / 0 / 9 | 1.0803 |
| 8 | 46 | 0 / 5 / 18 | 1.0862 | 0 / 2 / 26 | 1.0409 |
| 9 | 48 | 0 / 11 / 46 | 0.9859 | 0 / 0 / 4 | 1.0687 |
| 10 | 67 | 0 / 25 / 53 | 0.9847 | 0 / 3 / 52 | 1.0172 |
| 11 | 44 | 0 / 13 / 31 | 1.0065 | 0 / 5 / 44 | 1.0135 |
| 12 | 44 | 0 / 1 / 19 | 0.9381 | 0 / 41 / 43 | 1.0028 |
| 13 | 37 | 0 / 8 / 26 | 0.9643 | 0 / 19 / 37 | 1.0012 |
| 14 | 28 | 0 / 13 / 24 | 0.9899 | 0 / 15 / 28 | 0.9944 |
| 15 | 42 | 1 / 9 / 42 | 1.0227 | 0 / 24 / 38 | 0.9915 |
| 16 | 42 | 0 / 14 / 40 | 1.0003 | 0 / 6 / 42 | 0.9857 |
| 17 | 44 | 0 / 21 / 40 | 1.0095 | 0 / 30 / 43 | 0.9961 |

（`by_location`；L1 含 1 个天制的 `second_biome_door`，L15 的 1 个 HP 逐位是 BossTower 的 `inf`。）

### 按分类

分类由名字和标志得出（`category()`）：活动、假人、阻挡物（door/blocker）、BossTower、树、VIP、Boss（含 boss/death）、精英、骑士/龙/hard、普通。

| 分类 | 个数 | HP 逐位 / 1% / 5% | Damage 逐位 / 1% / 5% |
|---|---|---|---|
| tree | 340 | 54 / 121 / 273 | 75 / 137 / 236 |
| regular | 213 | 15 / 74 / 162 | 26 / 78 / 143 |
| knight_dragon_hard | 71 | 0 / 19 / 60 | 0 / 27 / 53 |
| elite | 34 | 3 / 10 / 29 | 3 / 16 / 30 |
| boss | 21 | 2 / 5 / 18 | 3 / 5 / 17 |
| blocker | 20 | 0 / 10 / 10 | 0 / 0 / 9 |
| vip | 10 | 5 / 5 / 5 | 10 / 10 / 10 |
| event | 6 | 0 / 0 / 0 | 6 / 6 / 6 |
| dummy | 1 | 0 / 0 / 0 | 1 / 1 / 1 |
| boss_tower | 1 | 1 / 1 / 1 | 0 / 1 / 1 |

（`by_category`）分类之间的差别主要反映它们所在的时间段：逐位复现全部来自 L1–L3 分钟制，而树和普通怪在那里占多数。本项没有在同一时间段内单独比较各分类。活动怪的 Damage “全对”没有意义，预测和场景都是下限 10（见第 4 节）。

### 天制按烘焙时刻分段

| 烘焙时刻（天） | 个数 | HP 1% / 5% 内 | HP 比 中位数 [p10, p90] | Damage 逐位 / 1% / 5% | Damage 比 中位数 [p10, p90] |
|---|---|---|---|---|---|
| [4, 5) | 26 | 0 / 0 | 1.2154 [1.1275, 1.3008] | 20 / 25 / 26 | 1.0000 [1.0000, 1.0060] |
| [5, 7) | 8 | 0 / 0 | 1.2416 [1.1082, 1.2911] | 0 / 7 / 8 | 1.0070 [1.0042, 1.0163] |
| [7, 20) | 134 | 48 / 133 | 1.0274 [0.9991, 1.0421] | 0 / 0 / 8 | 1.1362 [1.0648, 1.3449] |
| [20, 60) | 223 | 48 / 149 | 0.9875 [0.9310, 1.1076] | 0 / 31 / 149 | 1.0197 [1.0013, 1.0753] |
| [60, 120) | 116 | 24 / 93 | 0.9931 [0.9307, 1.0441] | 0 / 70 / 113 | 0.9977 [0.9814, 1.0109] |
| [120, 181) | 99 | 42 / 93 | 1.0075 [0.9674, 1.0382] | 0 / 44 / 97 | 0.9902 [0.9725, 1.0010] |

（`day_mode_by_bake_band`，只含解释代码为 `day_source_b` 或 `exact` 的 606 个。）

4–7 天的 34 个按被选护甲类型分（`days_4_7_by_armor_type`）：Cutting 28 个，Damage 逐位 15 个，HP 比 1.1207–1.3061（中位数 1.2344）；Stabbing 6 个，Damage 逐位 5 个，HP 比 1.0792–1.1275（5 个 `Spawner_vip_6` 都是 1.1275，L4 `Spawner_mid_cut_epic` 1.0792）。这一段 Damage 的取整带是场景 Damage 的 0.08%–2.0%（`damage_rounding_band.days_4_7_all`）。

## 3. 对不匹配的逐项检验

### H1 公式结构：同组一致性（成立）

同一模式、同一重算烘焙时刻、同一被选护甲类型的刷怪点读的是同一个快照，所以“场景 HP / 模型原始 HP”在组内必须是同一个系数，与 AverageFightTime、Count、抗性无关。HP 是整数，每个成员给出区间 `[(HP-0.5)/raw, (HP+0.5)/raw]`，组内区间有交集即视为一致（`H1_group_consistency`）。

组内所有成员的（AverageFightTime, Count, 所用抗性）都相同时，原始 HP 本来就相同，这一组必然一致，没有检验力。下表把这种平凡组分开计数；“独立约束”= 每个非平凡组的不同输入组合数减 1 之和。

| 数据来源 | 多成员组 | 涉及刷怪点 | 一致 | 非平凡组 | 非平凡组涉及刷怪点 | 独立约束 | 非平凡组一致 | 单成员组 | 组内极差中位数 |
|---|---|---|---|---|---|---|---|---|---|
| 场景，天制 | 127 | 512 | 125 | 92 | 406 | 151 | 90 | 95 | 6.7e-9 |
| 场景，分钟制 | 18 | 84 | 18 | 13 | 68 | 21 | 13 | 16 | 3.9e-4（小整数取整所致） |
| `SpawnerBakeInfos.Health` | 143 | 592 | 135 | 103 | 470 | 173 | 95 | 112 | 3.2e-8 |
| `SpawnerBakeInfos.PrevHealth` | 143 | 592 | 137 | 103 | 470 | 173 | 97 | 112 | 2.4e-8 |

场景天制里不一致的 2 组：9.0 天 / Stabbing 组中的 `Spawner_elite_regen`（L5），10.0 天 / Buckshot 组中的 `Spawner_1_epic_buck`（L4），正是 H10 里的两个整万 HP。

能说明的是：场景天制 HP 与模型的偏差，是同一烘焙时刻、同一被选武器类型的刷怪点共享的一个乘法系数。这个系数可以来自所选类型的 DpsData、暴击，也可以来自时间曲线项（AbilityDPS 等），H1 区分不了。也就是说，AverageFightTime、Count、抗性截断、取整这些逐刷怪点的部分都对。

H1 没有覆盖的部分：

- 95 个单成员组（天制）没有被检验。
- 分组用的是 A 的护甲选择（argmax）。如果 B 的各类型 DPS 比例不同，B 里有的刷怪点可能选了别的护甲，这种翻转 H1 检测不到。

### H1b 跨岛连续性（数据不足，未能判断）

如果各岛是在不同时候、用不同玩家状态分别烘焙的，同一类型、烘焙时刻相近、但属于不同岛的组，系数应当比同岛的组差得更多（`H1b_cross_island_continuity`，比较 |ln(k1/k2)|）：

| 时刻窗口 | 同岛组对 | 同岛差中位数 | 跨岛组对 | 跨岛差中位数 |
|---|---|---|---|---|
| ≤ 0.25 天 | 28 | 0.0018 | 1（L8/L10） | 0.0052 |
| ≤ 0.5 天 | 123 | 0.0040 | 4（L7/L10 1 对，L8/L10 3 对） | 0.0107 |
| ≤ 1 天 | 277 | 0.0068 | 6（L7/L10 2 对，L8/L10 4 对） | 0.0205 |

跨岛组对太少，不能下结论。另外，横跨两个岛的多成员组本身就是跨岛检验：天制有 3 个（`multi_island_groups`）。16.0 天 / Cutting（L6、L7）和 40.0 天 / Buckshot（L9、L10）在取整内一致；10.0 天 / Buckshot（L4、L6）不一致，原因是手调的 `Spawner_1_epic_buck`。分钟制有 1 个（30 分钟 / Cutting，L1、L2），一致。这些现象与“所有岛共用一个 B”相容，但样本只有 2 组，不足以排除逐岛烘焙。

### H2 快照稀疏化不是原因（排除）

54 个天制刷怪点的重算时刻正好落在存档的按天快照上，插值返回的就是存档快照本身。它们 HP 逐位 0 个、1% 内 17 个、5% 内 39 个，HP 比中位数 1.0290，范围 0.9987–1.2333（`H2_stored_chunk`）。所以即使有未稀疏化的逐日数据，也不能把场景值还原出来。

### H2b 分钟制的不匹配点不在任何存档快照里（排除查找错误）

对 21 个不逐位的分钟制刷怪点（18 个 `minute_dps_divergence` 加 3 个 `minute_other`），把 63 个按游戏时间的快照和 46 个按天快照逐个代入烘焙：没有一个快照能复现其中任何一个的 HP（`H11_minute_snapshot_search`）。18 个 DPS 分歧点的 Damage 都能由模型所选快照复现。所以这些 HP 不是查错了快照，而是那个时刻 B 的进攻状态在随包数据里根本不存在。

### H3 全局倍率 / 逐岛倍率 / 时间平移 / 时间缩放（拟合，未采纳）

对象是 605 个普通天制刷怪点（`H3_global_fits_days`）。倍率都在网格上优化（全局 0.95–1.05，逐岛 0.70–1.40，步长 0.0005），目标是 1% 内个数最多。k = 1 在网格上，所以优化结果不会比原样差。

| 模型 | 参数个数 | HP 1% / 5% 内 | Damage 1% / 5% 内 |
|---|---|---|---|
| 原样 | 0 | 161 / 467 | 176 / 400 |
| 全局倍率，最优（HP k = 1.000，Damage k = 1.0055） | 2 | 161 / 467 | 219 / 411 |
| 参考：全局倍率取比值中位数（HP 1.0050，Damage 1.0133） | 2 | 151 / 460 | 130 / 423 |
| 逐岛倍率，最优 | 2 × 14 | 335 / 509 | 401 / 533 |
| 全局时间平移，-2 到 +2 天每 0.1 天中最优（+0.2 天） | 1 | 159 / 449 | 205 / 441 |
| 时间缩放，0.90 到 1.10 中最优（1.00） | 1 | 161 / 467 | 176 / 400 |

（`global_factor`、`global_factor_median`、`per_island_factor`、`time_shift_grid`、`time_scale_grid`）

- HP 的最优全局倍率就是 1，单一倍率对 HP 没有帮助。
- Damage 乘 1.0055（+0.55%）能多把 43 个刷怪点拉进 1% 内，但仍有 386/605 在 1% 外，不能算规则。
- +0.2 天的平移把 Damage 1% 内从 176 提到 205，但 Damage 逐位从 20 降到 3。
- 逐岛倍率用 28 个参数，HP 也只有 335/605 在 1% 内。逐岛最优倍率见 `per_island_factor.factors`，例如 L4 HP 1.2935、Damage 1.0000，L5 HP 1.037、Damage 1.1385，L12 HP 0.94、Damage 1.0035。
- 没有哪个少参数规则能把天制场景值解释成“A + 简单变换”。

### H4 同一份数据换了时间轴？（排除）

对每个天制刷怪点，在随包数据上分别找出使模型 HP 等于场景 HP 的烘焙时刻 t_H、使模型 Damage 等于场景 Damage 的 t_D（二分，`implied_t_health` / `implied_t_damage`；二分假设原始值随时间单调不减，脚本只在区间两端检查）。如果 B 只是 A 换了节奏，t_H - t 与 t_D - t 应当相同。实际上（`H4_implied_time`，605 个）：

- |t_H - t| 中位数 0.242 天，p90 0.931，最大 2.171；|t_D - t| 中位数 0.278，p90 0.660，最大 1.877；
- |t_H - t_D| 中位数 0.444 天，比任一单侧偏移都大；
- 逐岛中位数的方向不一致，例如 L4 为 t_H +0.62、t_D +0.01，L12 为 t_H -0.89、t_D +0.06，L15 为 t_H +0.57、t_D -0.28；
- 逐岛相关系数正负都有：14 个岛里 |r| > 0.5 的有 7 个，其中 4 个为正（L5 0.81、L8 0.79、L14 0.56、L16 0.65），3 个为负（L4 -0.56、L6 -0.61、L7 -0.54）（`per_island`）。全部放在一起算的 r = -0.033 是正负相抵的结果，不代表两者无关。

所以一个全局或逐岛的时间扭曲解释不了偏差：同一刷怪点的 HP 和 Damage 需要不同的时间偏移。

### H5 `SpawnerBakeInfos` 与场景的关系（另一代烘焙）

日志有 712 行，按 Key 关联上 713 个刷怪点（重复的 Key 见第 1 节）（`H5_bakeinfos`）：

| | 分钟制（108） | 天制（605） |
|---|---|---|
| 日志 OptimumTime = 当前 OT | 97 | 602 |
| 日志 Health = 场景 | 6 | 1 |
| 日志 Damage = 场景 | 42 | 0 |
| 日志 Health = 本模型预测 | 13 | 1 |
| 日志 Damage = 本模型预测 | 43 | 1 |
| 日志 PrevHealth = 场景 | 8 | 1 |
| 日志 PrevHealth = 日志 Health | 53 | 6 |

场景 HP / 日志 HP 的中位数随岛屿递增：L4 1.399，L11 1.855，L13 4.409，L14 6.065，L16 10.646，L17 19.591（最大 29.007）。日志本身与公式自洽（H1），所以它是用另一份、晚期弱得多的玩家状态（C）烘焙的结果，没有写进当前场景。日志里的 PrevHealth 又是再早一代的值。

**先后顺序。** 日志 OT 与当前 OT 不同的刷怪点有 14 个（`ot_changed_since_bakeinfo`、`ot_changed_summary`）。其中 11 个是 L1–L2 分钟制，10 个用当前 OT 时 HP 和 Damage 都逐位复现；把这 14 个改回日志 OT 重算，一个都不复现。所以场景的最后一次烘焙用的是写日志之后才定下的 OT，即顺序为：日志（C）在前，场景烘焙（B，前 120 分钟与 A 相同）在后。这个推断的前提是 OT 没有在两次烘焙之间改过又改回。数据仍然回答不了的只是：C 这次烘焙的结果当时有没有应用到场景上（之后被 B 覆盖），还是只记录了日志。剩下的 1 个 L1–L2 分钟制是 `Spawner_1 (11)`（见第 4 节 `minute_other`），另外 3 个是天制的 `Spawner_1_epic_buck`、`Spawner_elite_regen`、`Spawner_superboss_legendary_pierce`。

### H6 取整规则（确认，但无法区分取整方式）

- 716 个有限场景 HP 全是整数；BossTower 的 HP 是 `inf`（`Count=0`，模型同样得到 `inf`）。
- 717 个场景 Damage 中 716 个是 10 的倍数，例外是 `HandBalance` 的 `Spawner_1`（4）。
- 预测原始 Damage < 10 的 25 个中，24 个场景值为 10（下限 10 的规则）。
- 能区分 `Math.Round` 银行家舍入和四舍五入的平局（原始 Damage/10 恰为偶数 + 0.5）有 31 个，全部落在本来就不匹配的刷怪点上（场景值两种取法都不等），HP 没有 .5 平局（`H6_rounding`）。所以舍入方式按代码记为银行家舍入，数据上未能独立验证。

### H7 早期刷怪点时间偏移（代码确认，数据相符）

`GetReCalcOptimumTime` 的偏移来自代码（第 1 类：代码依据）；这些天制刷怪点都不逐位，下面的数字只说明开着偏移时数据吻合得好得多，不是逐位复现。L ≥ 11 且 `RelativeOptimumTime < 0.5` 的 182 个刷怪点，偏移最大 15 天（`H7_early_offset`）：

| | HP 1% / 5% 内 | Damage 1% / 5% 内 | 中位数 \|ln 比\| HP / Damage |
|---|---|---|---|
| 用偏移（代码） | 52 / 147 | 99 / 177 | 0.0244 / 0.0076 |
| 不用偏移 | 9 / 13 | 1 / 16 | 0.2973 / 0.2045 |

场景值是在 `GetReCalcOptimumTime` 生效的情况下烘焙的。

### H8 Damage 用的是哪种防御（数据无法区分）

Damage 的防御取 `LocationsConfig` 中该岛的 `PreferredWeaponType`。63 个按游戏时间的快照防御全为 0，按天快照从第 9 天才出现非零防御（`H8_defense_type`）。在能逐位匹配的范围（分钟制 100 个、天制 < 5 天 26 个）里，换成刷怪点自己的 `WeaponType` 或任一固定类型，逐位个数都一样（97/100、20/26）。这一项只能以代码为准。

### H9 数值精度（不影响结论）

把所有 `float` 改成 `double` 计算，HP 预测变化 247 个、Damage 变化 21 个（多为大数 HP 的末位），但逐位匹配数仍是 80 和 124（`H9_precision`）。

### H10 整数值是手调痕迹

（`H10_round_values`）HP ≥ 1e4 的 609 个场景值中，是 1e4 整倍数的有 3 个；按均匀分布偶然出现的期望个数是 0.061：

| 刷怪点 | 岛 | 场景 HP | 预测 HP | 场景 Damage | 预测 Damage |
|---|---|---|---|---|---|
| `Spawner_Dummy` | 1 | 1000000000 | 180 | 10 | 10 |
| `Spawner_1_epic_buck` | 4 | 200000 | 200087 | 10000 | 11420 |
| `Spawner_elite_regen` | 5 | 270000 | 262131 | 10800 | 9100 |

Damage ≥ 1e4 的 520 个中，1e4 整倍数的有 3 个（期望 0.52）：`Spawner_1_epic_buck` 10000、`Spawner_1_uncommon_pierce_helmet (1)`（L6）20000（预测 17360）、`Spawner_1 (16)`（L8）80000（预测 75870）。后两个只比期望略多，算作疑似。`Spawner_1_epic_buck` 和 `Spawner_elite_regen` 也正是 H1 里破坏组内一致性的两个，并且日志 OT（12.5、9.4）与当前 OT（10、9）不同。

### H12 三个 `minute_other` 能否用改了一个烘焙输入来解释（不能）

HP 取决于 OT、AverageFightTime、Count 和护甲；Damage 取决于 OT、AverageFightTime、Count、Difficulty 和 AttackCooldown。只有 OT、AverageFightTime、Count 同时影响两者，所以只有它们有可能用一处改动同时解释 HP 和 Damage。脚本在当前模型上逐个扫描单个输入（`H12_single_input_scan`；AverageFightTime 1–200 步长 0.5，Count 1–11，Difficulty 0.1–5 步长 0.05，OT 0–250 分钟步长 0.5，AttackCooldown 0.1–5 步长 0.05）：

| 刷怪点 | 当前 AFT / Count / Difficulty | 能复现 HP 的单项改动 | 能复现 Damage 的单项改动 | 两者都复现 | 由 HP 反推的 AFT |
|---|---|---|---|---|---|
| `Spawner_1_pierc_rare`（L2，OT 69） | 20 / 1 / 0.7 | 无 | AFT 23.5–26.0，Difficulty 0.6，AttackCooldown 0.85–0.9 | 无 | 19.83 |
| `Spawner_1 (11)`（L2，OT 72） | 10 / 2 / 0.3 | 无 | AFT 10.5–11.5，AttackCooldown 0.9–0.95 | 无 | 8.63 |
| `Spawner_mid_smash_rare`（L3，OT 200） | 15 / 1 / 0.7 | 无 | AFT 14.5 | 无 | 17.82 |

3 个都不能由单个输入解释，HP 在任何单项扫描中都没有逐位命中。由 HP 反推的 AverageFightTime 也不是整数（19.83、8.63、17.82），所以 HP 不像是“把 AFT 改成另一个整数后重烘焙”的结果。结论是：至少改了两个输入，或者数值是手填的。它们保留为具名例外。`Spawner_mid_smash_rare` 在 120 分钟后的分歧段里，HP 偏高本身可以用 B 解释，但 Damage 差一档（1080 / 1060）仍无法解释。

## 4. 每个刷怪点的最佳解释

解释代码见 `spawners.csv` 的 `explanation` 列（`explanations`）：

| 代码 | 个数 | 解释 | 证据级别 |
|---|---|---|---|
| `exact` | 79 | Health 和 Damage 都逐位复现（全是 L1–L3 分钟制） | 精确复现 |
| `day_source_b` | 606 | 天制，场景值按同一公式、但用不同于 A 的玩家状态 B 烘焙（含 BossTower：HP 两边都是 `inf`，Damage 场景 36234910 / 预测 36096200） | H1、H2、H3、H4 支持；B 不在包里，无法逐位复现 |
| `minute_dps_divergence` | 18 | L3 分钟制，OT 133–240，Damage 逐位，HP 偏高：B 在这段的 Cut/Buckshot 进攻系数更高 | 见下表 |
| `minute_other` | 3 | 分钟制，HP 和 Damage 都不对，单项改动也解释不了（H12） | 未解释，具名例外 |
| `event_runtime_override` | 6 | 活动怪：场景 HP 50，预测与日志都是 96（Boss/MiniBoss）或 2（Golem）。生成时 `EnemySpawner.ApplyEventPlanToConfig`（6af011f，`Enemy/EnemySpawner.cs:1145-1160`，原生 VA 0x150639c，在 `EnemyUnit` 初始化前调用，:961-962）按 `eventsRepository.GetEventEnemyInfo` 覆盖 Health/Damage 的 BaseValue、`WeaponType` 和各护甲的 Resist。场景里的值只是占位，不在烘焙范围内。它们的 Damage“相等”只是预测和场景都取了下限 10 | 运行时覆盖 |
| `hand_balance` | 2 | `Spawner_1`（L1，HP 80 / 预测 96，Damage 4 / 预测 10）、`second_biome_door`（L1 天制，HP 200 / 预测 237522627，没有日志记录） | `HandBalance=1`，手调 |
| `day_round_hand_tuned` | 2 | `Spawner_1_epic_buck`、`Spawner_elite_regen`：整万 HP | H1、H10 |
| `dummy_hand_set` | 1 | `Spawner_Dummy`：HP 1e9 | `IsDummy` |

分钟制 OT > 120 的 22 个按被选护甲类型分（`minute_after_120_by_armor_type`）：Stabbing 3 个（OT 133、151、173），HP 和 Damage 都逐位；Cutting 11 个，HP 0 个逐位、Damage 11 个逐位；Buckshot 8 个，HP 0 个、Damage 7 个（不逐位的是 `Spawner_mid_smash_rare`）。

分钟制 OT ≥ 120 的分组（`minute_groups_power_ratio`，比值 = 场景 HP / 模型原始 HP，组内一致；120 分钟组是逐位复现的参照，200 分钟组是 `minute_other` 的 `Spawner_mid_smash_rare`，其余是 `minute_dps_divergence` 和 3 个 Stabbing 逐位点）：

| OT（分钟） | 被选护甲类型 | 个数 | 比值 |
|---|---|---|---|
| 120 | Cutting | 20 | 1.0002（逐位） |
| 133 | Cutting | 2 | 1.0093 |
| 133 | Stabbing | 1 | 1.0000 |
| 146 | Cutting | 6 | 1.0447 |
| 151 | Stabbing | 1 | 1.0000 |
| 160 | Cutting | 3 | 1.0992 |
| 160 | Buckshot | 2 | 1.1240 |
| 173 | Stabbing | 1 | 1.0000 |
| 173 | Buckshot | 1 | 1.1019 |
| 187 | Buckshot | 3 | 1.2964 |
| 200 | Buckshot | 1 | 1.1881（`Spawner_mid_smash_rare`） |
| 240 | Buckshot | 1 | 1.1066 |

同一时刻 Stabbing 吻合（只有 3 个点），Cutting/Buckshot 偏高。18 个分歧点的 Damage 全部逐位吻合，但这只说明防御侧需求在 ±5 的取整带内相同：取整带是场景 Damage 的 0.40%–3.33%，中位数 1.07%（`damage_rounding_band.minute_dps_divergence`）。

`minute_other` 的 3 个（`named_exceptions`）：

| 刷怪点 | 岛 | OT | 场景 HP / 预测 | 场景 Damage / 预测 | 备注 |
|---|---|---|---|---|---|
| `Spawner_1_pierc_rare` | 2 | 69 | 4403 / 4440 | 130 / 150 | 109 个存档快照中没有一个能复现 HP 或 Damage；Difficulty 0.6（现 0.7）能复现 Damage，但没有单项改动能复现 HP |
| `Spawner_1 (11)` | 2 | 72 | 2489 / 2884 | 90 / 100 | 日志 OT 为 48，OT 改过；同样没有快照或单项改动能复现 HP |
| `Spawner_mid_smash_rare` | 3 | 200 | 22663 / 19075 | 1080 / 1060 | 处于 DPS 分歧段，Damage 也差 1 档；没有快照或单项改动能复现 |

两个 L2 的例外出现在 76/78 都逐位复现的时间段里，是烘焙后被单独改过的设计信息（手填，或同时改了至少两个烘焙输入）。

各标志的情况（`flags`）：`HandBalance` 2 个，都不匹配；`IsEvent` 6 个，Damage 都等于下限 10、HP 全不对；`IsVip` 10 个，Damage 全对，HP 对 5 个（L3 的 5 个分钟制 `Spawner_vip_3`；L6 的 5 个 `Spawner_vip_6` 是天制，OT 4.0）；`IsEliteBoss` 33 个，HP 和 Damage 各对 3 个；`IsSpecial` 2 个、`IsDummy` 1 个；`IsBossFightPart` 0 个。`HandBalance` 是排除自动烘焙的标志（两个都偏离，其中 `second_biome_door` 没有日志记录），但它没有覆盖所有手调点：假人和两个整万 HP 怪都没有打这个标志，活动怪则由运行时覆盖。

## 5. 拟合规则

本项没有采用任何拟合规则。烘焙公式本身已逐位复现（第 2 节）。天制偏差试过的少参数规则（H3）都不够：

- 最优全局 Damage 倍率 1.0055（1 个参数）：1% 内 219/605，最大的单项改善，但 386 个仍在 1% 外；HP 的最优全局倍率是 1。
- 逐岛倍率（28 个参数）：HP 1% 内 335/605，Damage 401/605。参数多、解释力仍不足，按“不要过拟合”的要求不采用。

## 6. 尚未证实的假设

1. **B 和 A 的差别来自什么。** 从 H1 和第 3 节可知：差别是按（烘焙时刻、被选武器类型）共享的乘法系数；前 120 分钟 A 和 B 相同；之后先是 Cut/Buckshot 的进攻系数不同，防御侧在 Damage 取整精度内相同；4 天时 Stabbing 也不同；7 天以后 Damage 也不同。模拟本身没有随机源（6af011f 的 `Simulation/` 下只有 `Forecast/ForecastSnapshots.cs:203` 用到 `UnityEngine.Random`，`SimulationSystem.cs` 里没有），所以“两次随机运行”不是默认解释。候选：
   - (a) `AutoBalanceItems`（Simulation Config 中为 1）在模拟中改写武器进化数值，导致两次运行的 DpsData 不同；
   - (b) 烘焙之后改了物品配置：`SimulationDpsWeaponMultiplier`（89 个物品中有 9 个不等于 1：Buckshot 1.3、1.4、1.4、1.6、1.6，Cutting 1.35，Stabbing 1.4、2.0、2.0，`items_dps_multiplier_not_1_by_damage_type`），或物品 Damage / AttackCooldown；
   - (c) 改了 `StatModifier` 插值曲线（`CalculateDpsData` 对每件物品乘这个系数）；
   - (d) 各岛在不同时间、用不同玩家状态分别烘焙（H1b 的样本太少，未能排除）。

   (b)、(c) 都会产生“同一时刻、同一类型共享系数”的现象，和 H1 的结果一样。可做的检验：
   - ① 移植 `SimulationSystem.CalculateDpsData`（`Simulation/Systems/SimulationSystem.cs:908-933`，原生 VA 0x13e4478），用快照里的 `Items` 和当前 ItemConfig / 曲线重算 DpsData。如果等于存档 DpsData，说明 A 与当前配置自洽，B 对应的是另一套配置或自动平衡结果；
   - ② 在岛屿时间重叠处（L6 的 4–16 天对 L4/L5，L10 的 13.6–44 天对 L7–L9）比较同类型的隐含系数是否连续。现有数据中 1 天窗口内只有 6 个跨岛组对（H1b）。
   
   本项没有移植 ①，它更适合由物品成长曲线的分析或阶段 5 来做。
2. **C 有没有应用过。** 顺序（C 在 B 之前）已由 OT 推出（H5）；C 的结果当时有没有写进场景、后来被 B 覆盖，数据无法判断。
3. Damage 用的防御类型（`PreferredWeaponType`）和舍入方式（银行家舍入）只有代码依据，数据无法区分（H6、H8）。

## 7. 局限与评审意见的处理

- 评审指出的问题都已核实并采纳：
  - H3 的全局倍率原来取的是比值中位数而不是最优值，现改为网格优化，中位数版本保留作参考；
  - “另一次随机运行”改为“数据源 B”，并在第 6 节列出其他可能；
  - “防御侧完全相同”改为“在取整精度内相同”，并补充天制 4 天时 Stabbing 已分歧；
  - H1 补充平凡组、单成员组和护甲翻转的说明；
  - H4 不再引用合并后的 r；
  - 先后顺序给出推断；
  - H7 改为“代码确认，数据相符”；
  - 活动怪引用 `ApplyEventPlanToConfig`；
  - 补充重复 Key，Simulation Config 快照列表为空改为从数据库读出并检查；
  - 新增 H12 单项输入扫描。
- H4 的二分假设原始 HP / Damage 随烘焙时刻单调不减，脚本只检查区间两端，没有检查整条曲线。
- H12 的网格有步长，连续取值（例如 AFT 19.83）可能落在格点之间。所以“无单项改动能复现 HP”严格说是“在所给网格上没有”；反推的 AFT 已单独列出。
- H1b 的跨岛检验样本太少（1 天窗口内 6 对），不作为证据使用。

## 8. 对阶段 5 的意义

- 用 `tufu_balance.model.bake.bake_spawner` 加随包 Simulation Data，可以作为原团队烘焙的可执行替代：分钟制 79 个刷怪点两项都逐位一致；天制 HP 5% 内的有 470/609，Damage 5% 内 401/609。
- 想要逐位复刻天制场景值，需要数据源 B，包里没有。反过来，这也说明场景数值不是从当前 Simulation Data 精确烘焙出来的：如果在阶段 5 用当前数据重新烘焙，L4 的 HP 会变成场景值的 1/1.2344（比值中位数），L5、L6 的 Damage 会变成场景值的 1/1.1341、1/1.1684。
- 手调点（第 4 节表中的 `hand_balance`、`event_runtime_override`、`dummy_hand_set`、`day_round_hand_tuned`、`minute_other`）在重烘焙时应保留原值，不能只靠 `HandBalance` 标志来识别；活动怪的值由运行时覆盖，不需要烘焙。
