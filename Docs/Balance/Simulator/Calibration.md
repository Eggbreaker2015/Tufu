# 阶段5校准记录

K2 先记录本单元结构变更，K3 在此续写完整闭环报告。开环证据与已知未命中见 Runbook §24；K2 的原始证据与最终状态见 §25。

## K2 / Q34：慢阶段分解时间形式

登记于后备拟合之前。先运行两种已恢复的 TimeModel、48个离散组合，必须两者在 TC1/2 训练段的 T16 都无合格者才允许后备。早期 killseq 逐点类别表已在网格前生成；起始区间69.9365秒，常数移动的 farmedTime 合计232.0878秒，末状态下移除移动的 `2×fight+regen` 仍106.4822秒。回血等待与移动形式可能不适用，但这些类别分摊不能独立确定路线和顺路战斗时间。

后备只引入现有 CMove/CRegen，按 Runbook §25.3 的固定5×5网格及有界单纯形；不改原生 RPM `(70,50,2)`、不拟合战斗乘数、不加逐日/逐岛参数。训练第0–1天、持出第2–3天；T20硬筛后训练排名，不能用持出、岛屿时刻、物品数或活跃分钟挑另一个结果。所有原设计阈值继续作科学验收，回归黄金只冻结观测，失败不得改称通过。

所有权偏离：为 T20 修改 C3c 的 daily.csv 与其 schema，逐日报告 ProcessDeath 次数并与事件核对；C7 的 boss 诊断在清怪前取样。没有改变策略行为，未改 Design.md、默认参数或参数资产。

K1 round-2 口径修正：T09 1380/1380 使用 G20 的1e-4单元测试，逐位1190/1380，其余≤3 ulp；非TC最大值0变化是按构造成立，all-days反事实会抬高5336格但G20仍1380。T12每日整数余数带源于模拟器Carry，不是随包统计噪声模型；逐日辨识力弱，阶段限仍约束。mace的C5归因已撤回，K3检查状态A/B及原版 loss>H/2 时 RPS×0.2 的策略线索。

### K2 实测结果（验收前冻结，不追加候选）

初始48组合中30个通过T20；FarmedTime的14个、距离模型的16个T20合格者，**训练TC1/2的T16通过数均为0**，T16+T03联合通过也均为0，故按Q34启用后备。初始最优 slow-06 的训练/持出容差单位平均残差为0.433569664/0.184810093。

后备25粗网格+19个新增单纯形作业；优化器8迭代、22评价、success=true，候选预算不追加。最终首次同分者 simplex-06：Dynamic、SmartPath=true、SameAsFast、SkipSpawner、DecomposedMoveByDistance，**CMove=.953125、CRegen=1**。CMove为本阶段拟合，CRegen保持单位值；其它字段沿用K1/结构默认。第二名simplex-08训练同分，持出也相同，不能把此点值宣称唯一识别。条件自助法仅重采样2个训练日，不再运行Unity；对同分者保留不确定性，区间是已测候选内的描述，不是原生参数的可信区间。

| 目标 | 结果 | 原设计判定 |
|---|---|---|
| T20 | TC6前Location4为0；day3死亡0；day0–5的countRespawn=3/0/0/0/0/0 | PASS；所有候选逐日计数与每次day3死亡另存CSV |
| T16 | 56/60格通过；Health通过，所有小属性在一次清怪以内；TC1/2 HealthRegen=18.5/39.5，对21.5/44.5；MovementSpeed=11/23，对13/25 | FAIL；主属性最大15.3846%，不改5%阈值 |
| 岛2/3 | P=36.4415512 /128.836319；岛3在day2 | 岛2 PASS，岛3 FAIL（原110.2–115.3） |
| 岛4 | day5解锁，TC6首次出现 | PASS |
| TC1–4物品数 | 4/5/7/8，对4/7/8/8 | FAIL |
| T03 | 64行，跨日强制3/3，TC4不进早期列表；cadence 57/60，三处ΔP=7.316623/7.714237/7.123370 | FAIL；不改原2分钟overshoot带 |
| T04 day0–3 | 64.904630/62.228045/64.527005/62.722260分钟；相对2.5714%/1.8486%/4.7394%/0.9198% | FAIL（day2），原3%保留 |
| 留出 | 训练0.400236331、持出0.151476760，比例0.378468291 | PASS，≤1.5；不能用它冲销单项失败 |

本单元科学验收**未通过**：T03/T04/T16/岛3/物品数仍失败；没有用持出、到岛时刻或物品数挑另一个参数，没有更换P06/P12、没有继续增加SmartPath半径/收益权重等候选。K3应以此为未通过的慢阶段输入，完整闭环只验证；需重新拟合时先登记结构假设。原版5%/3%/一次清怪阈值、固定夹具、所有既有ORIGINAL均不改。calib_slow将以明确的放宽登记冻结这些观测作为回归，不能把回归通过改称设计通过。

### K2 第二轮：回退与预登记（运行新网格之前）

Q34 后备已尝试并拒绝：六个系数点训练分数并列，CMove 不可识别；与单位系数 slow-06 相比，没有任何目标由失败变成通过。故恢复 slow-06：FarmedTimeMoveByDistance、Dynamic、SmartPath、SameAsFast、SkipSpawner，CMove=CRegen=1。两种恢复的时间模型均失败的主要证据是策略结构：HealthRegen 和 MovementSpeed 的收益估值不足，day 0 的死亡丢失一次 minisaw，而 idle 越过 session 终点；改变时间系数不修复这些格。48 个原始组合中 Static/Dynamic 在 Constant 下完全相同，故只有 24 个不同策略，PriorityFamily 在该条件下不可辨识。后备的首个非同分训练结果是单位系数 slow-06（0.433569664，持出 0.184810093）；六个并列最优系数点的训练为 0.400236331。上述后备排序仅作历史证据，不选择系数。

以下为 5.5 的封闭假设清单，均为 Inferred，登记于任何新拟合运行之前。训练仅用 T16 TC1/2，T20 硬筛；TC3/4 持出及其余目标仅报告，不参与排序；不扩大网格。

1. H1（优先级 1）：Dynamic 的 StatBenefitModel 使用现有 MarginalKillTime，或新增 RPM 归一化 `mean(CurrentGpm)/CurrentGpm[s]`，每 session 计算一次；静态臂因 Constant 下退化而移除。目标：day 0 HealthRegen/MovementSpeed、岛 3 到达 P 和 TC2/3 物品。
2. H2（优先级 2）：新增 HealthModel，默认 RegenRate；RegenToFull 在存活且时间模型计入 avRegenTime 后将 CurrentHealth 设为 forecast Health。与 H1 联合测试，目标为 day 0 死亡、minisaw、TC1 Cutting DPS 和 boss 损血比；T20 不放宽。
3. H3（优先级 3）：新增默认关闭的 idle 终点约束，正剩余时间时将 RebuildIdleMinutes 限于 lifeCyclePeriodEnd − CurrentPlaytime；另可在顺路战斗后到达下一快照或 session 终点时结束链。目标 T04 day 0–3、T03 cadence 和 boss 前的 session 相位。测试 H3 单独以及与 H1/H2 组合的边际效果。

交叉文件所有权偏离：H1 修改 C7 Targeting 与枚举，H2 修改 C7 Fight，H3 修改 C3b Run（及可选 Fight）；仅新开关选择新行为，默认保持原值。每项切换只由 slow.json 显式选用。

### K2 第二轮网格结果

代码 7b3055b，8 个已登记组合均通过 T20，无 day 3 死亡及 TC6 前岛 4；训练最优为 baseline（0.433569664），首个非同分第二名 H3（0.542982956）。持出不参与选择，baseline 持出 0.184810093、比例 0.4263。单位系数 baseline 的 day0–5 死亡 3/0/0/0/0/0；岛2 P=36.480759、岛3 P=127.473541（day2）、岛4 day5/TC6，物品 4/5/7/8；T16 56/60、主属性最大 15.3846%，T03 cadence 57/60，T04 第0–3天误差 2.90%/0.75%/4.66%/0.91%。因此 T03、T04、T16、岛3和物品仍失败，交 K3/Z1，不扩大网格或放宽原判据。

边际效果（均仅作报告，未以持出重排）：H1 Marginal 单独训练 0.900254，岛3 P=136.301，day0 死亡3；RPM 单独 1.467083，岛3 P=129.869，死亡4；H1 Marginal+H2 训练 0.591865、死亡0、T03通过，岛3 P=127.835；RPM+H2 训练 0.788623、死亡0、T03/T04通过，岛3 P=125.322；Marginal+H2+H3 训练 0.558532、死亡0、T03/T04通过，岛3 P=124.939；RPM+H2+H3 训练 0.575967、死亡0、T03/T04通过，岛3 P=125.170；H3 单独训练 0.542983、死亡3、T03通过但 day3 T04 误差3.22%、岛3 P=126.531。各组物品均为 4/5/7/8。八组所有天数 countRespawn、全部 T03/T04 单元及 T16 残差见归档 `out/calibration/slow/e25c2dc1c4c5f2dd/`。RPM 归一化对正且有限的 CurrentGpm 求均值和倒数权重；零或非有限值保留 1，避免除零。5% 主属性阈值在 TC1 对 MovementSpeed/HealthRegen 比一次清怪奖励更严，是设计口径问题，交 Z1。

原始离散网格去除 Constant 下 Static 重复臂后为24项；训练最优 slow-06，首个非同分次优 slow-07，训练/持出 0.609300310/0.282021995。第二轮结构网格报告另列8项。`stage_slow` 入口转交封闭结构网格实现（辅助模块 `stage_slow_round2.py`，相对指定文件清单的组织偏离）。

执行顺序偏离：第二轮网格开始前已恢复单位系数并登记假设，但 ORIGINAL arrivals 的纠错、CURRENT 接受和旧网格去重 CSV 在网格运行后才完成；原因是先生成第二轮实际观测以通过黄金工具记录，非按要求把全部完整性修复置于网格之前。旧归档不改写，24 项去重表保存在第二轮归档 `historical_hypotheses.csv`；新网格的 `hypotheses.csv` 是8个不同结构臂。该流程偏离不改变训练排名，但须供复审。

## K3 初步全程比较（未完成验收）

运行前固定四个组合 baseline、H2、H3、H2+H3：只用 free 的 TC4–103 T06 单元平均相对误差选优，TC≥104 单列持出；T20 为硬筛，训练同分按列表先后。H2 为 RegenToFull；H3 为 CapIdleAtSessionEnd 与 BreakEnRouteAtBoundary。此项是针对 K2 交接的结构选项比较，不能当成独立外部验证。

| 候选 | 训练平均相对误差 | 持出平均相对误差 | T20 |
|---|---:|---:|---|
| baseline | 0.006211365 | 0.000415264 | 通过 |
| H3 | 0.006309266 | 0.000415005 | 通过 |
| H2 | 0.006418353 | 0.000415560 | 通过 |
| H2+H3 | 0.006550658 | 0.000414669 | 通过 |

baseline 排第一，H3 第二，保留 K2 的 baseline；持出未用于重排。四作业 serve 批耗时 62.42 秒；checkpoint（from_day 4）与两个 Seeded 作业另批 47.54 秒，均 launcher 退出 0。运行键分别为 `1e7089f96b8231b95b2d984b0499df9c59af55e858837004567d3490122500fe` 与 `7534116e076458bb329d239e6d686643d4a33e99c9cbc8600a5274856ee965ec`，代码身份 `2d81e541dae47d7ff71d9515920f31d914c70f9f`；每个作业均有资产守卫。free 的模拟作业墙钟 8.70 秒（含烘焙导出），不是冷启动时间。

P10 原判据的初步报告位于 `Balance/out/calibration/full-free/d558f4ffdf8d04fc/` 和 `Balance/out/calibration/full-checkpoint/04b46460ab802222/`。free 的 T03、T04、T06、T09、T12、T14、T16 失败；checkpoint 的 T03、T04、T06、T09、T14、T16、T17 失败。两者 T05、T07 尚 pending，T08、T15、T19、T20 未由该报告计算；T20 的硬筛使用逐事件检查。不能把这些初步报告当成第 3 阶段完整验收或放宽登记。free 的 X4 对 717 个刷怪点新 HP/Damage 全部逐位相等、无 mismatch；这不是 T19 的有信息量子集对 A 的外部验证。

T03 分组目前 `n` 已按实际生成的单元数计算；新测试覆盖模拟早期行多于随包行时 `n_ok ≤ n`。这里 baseline 记录 57/60。先前提及的 61/60 问题在当前 `metrics.evaluate` 实现中没有复现，未擅自改变计算定义。

**未完成**：T05/T07/T15 首次运行棘轮及逐项放宽登记、完整训练/持出报告与 T19、连续参数区间复核、完整黄金回归及 `golden accept`、校准参数层与新进程资产导入、冷运行预算。不得将这次初步比较用于发布参数或声称闭环通过。

T19 初步外部验证按随包 A 的 R1 信息子集固定成员，再对 free 与 A 分别计数；Python 烘焙与本次 C# 输出的 X4 已逐位核对。Health 子集 608 个，1% 带内 free 138 / A 162，5% 带内 383 / 469，均低于 A 的 95%，**不通过**；Damage 子集按 A 的 DefenseData 非零固定为 503 个，1% 为 326 / 144、5% 为 433 / 366，均通过。T19 整体不通过，闭环可信度不能据此成立。分钟制未在此完成报告。计算代码 `stage_full.t19` 和结果 `out/calibration/full/preflight/t19.json`，未改变任何原版黄金阈值。

冷启动独立默认 `golden-405`：`python -m tufu_balance.unity run --preset golden-405 --force`，launcher 21.5 秒、job_wall_s 8.5835 秒、退出 0；运行键 `4fb5da3a064115cf63943fe8f982be199e23b72879615b500caac576f9376aa1`（完整键以 `Balance/out/calibration/full/preflight/cold.log` 为准）。这是默认参数预设的预算测量，尚不能代替发布后的 calibrated 层验收。
