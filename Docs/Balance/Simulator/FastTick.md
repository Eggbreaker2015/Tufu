# 快阶段（C5）

本单元填写 `SimulationSystem.FastTick.cs` 的规划、累积、冲刷和活跃分钟；不改 `RebuildAdvanceFastDay`、已恢复的方法或设计稿。默认开环是 `replay` + `stored`，从第 4 天覆盖属性与经济缓存，解锁由门禁推导。Postmerge 在 C6 + Enginefix 合入的代码上实测：岛 2 在慢阶段第 0 天、岛 3 在第 2 天打开；快阶段打开岛 4–17，共 14 个，总计仍为 16 个且无重复。此前只集成 C7 的评审值是第 1 / 4 天，本次 C6 正物品奖励使该旧观测不再成立。随包岛 2 的早期表为 TC0、天表为 TC1，T07 day_ranges {2: [0, 0]}、slack 1；岛 3 随包第 1 天，剩余偏差交 K2，不改策略或默认值。

## 实现与默认值

- 每天重复计算机器人数据；合格条件是原有机器人过滤、`fightTime < 300` 和 `!(L * H < loss)`。合格后在 `SimulateFightWithSpawnerFast` 内调用 `RebuildApplyClear`。门槛标 `Inferred` / `pending:simulation.fast_kill_gate`。
- `KillLossMultiplier = 1`、`KillLossFromAb = false`、`StartOfDay`、同日规划新岛、`FarmedTime`、κ = 0.245。诊断开关关闭，缩放因子为 1。读取 AB 的 L 或运行 B 组时，manifest 标 `policy_extrapolation`。
- 次数为 `(limit - used) / Count` 的整数商；未清过的 DAFC 为一次。充能和冷却立即改变，DAFC 清后为正无穷冷却。
- 累积字段完整保留属性、物品、灵魂、门禁和击杀数。轮间只冲刷门禁；每天结束属性各加一次合并的修饰器，随后处理物品、灵魂和剩余门禁，清空累积字段。C5 首次测量时物品函数尚为空；C6 合入后 401 个快日均有正物品奖励，Postmerge 的参考冲刷及 Inventory 验收验证该路径。
- 编辑器专用 `fast_tick_probe.reference_flush` 换用只读原版累积字段、无 `Rebuild*` 方法调用的参考冲刷。分别在同日与次日规划下比对全部日摘要、完整天快照。

## 首次测量与设计差异

首次运行代码 `b065bfc`，运行键 `8f11bbbe426ab36c96b09d1ff1c00b8795c09fe95dd5fd36dcfb1bb77119382b`（18 个作业，389.42 秒），B 组运行键 `736b2b364563102a44af552b47d70c23bd6f46519b5290dd9eacfe59d48e6822`（15.08 秒）。两者启动器退出码均为 0，资产守卫通过。测量经 `golden accept --seed-original` 记入 `sim_fasttick`，没有从设计稿抄入“实测冻结”的数值。

| 验收项 | 实测 | 结论 |
|---|---|---|
| T05 七个逐日区间、15 项属性 | 7/7；全部 37/41 精确 | 通过；精确总数只报告 |
| 多日残差 | 17→19、37→41、140→154 可由首杀提前一天解释；24→27 无解释（Health −24000、HealthRegen −21）| 未解释数上限冻结为 1 |
| 关同日规划 | 17/41 坏区间 | 实测冻结；调研是 14 |
| 去掉可杀性 | 25/41 精确 | 实测冻结；调研约 26 |
| θ = 0.9 / 1.0 / 1.1 | 14 / 4 / 7 坏区间 | 实测冻结；调研最后一项是 6 |
| DPS = 0.95 / 0.98 / 0.99 / 1 / 1.01 / 1.02 / 1.05 | 7 / 6 / 5 / 4 / 3 / 2 / 3 坏区间，集合也等于夹具 | 通过 |
| T07 岛 5–17 | 12/13 在区间内；岛 9 第 26 天，早一天 | 通过；岛 4 由岛 3 门禁在第 5 天推出 |
| T08 | 各难打刷怪点在区间内或只差一天 | 通过；岛 8 的 elite health 为第 26 天，17_death 为第 202 天 |
| 每日合格集合 X5、DAFC X6 | 与 `model.simcalc.eligible_spawners` 一致；清过的 DAFC 不再出现 | 通过 |
| 门禁与冲刷 | 16 个岛各解锁一次、无重复；参考冲刷的摘要和天快照相同 | 通过 |
| 每日属性修饰器 | 调用数等于当天奖励属性数 | 通过 |
| TC405 Value | 最大相对差 3.0596812e−8 | 通过（上限 1e−5）；逐属性结果入黄金 |
| T04，κ = 0.245，TC ≥ 11 | 34 个区间，中位误差 4.29264%，最大 8.58559% | **未达到设计 2% / 8%**；显式登记放宽，不能报告为原验收通过 |

诊断数差异的原因可以直接对照冻结夹具与运行事件：调研 `forensics/rulefit.py` 无条件设 `unlock[4] = 5`，不对分钟制刷怪点推导首杀日。本实现对岛 3 门禁也用可杀性。θ = 1.1 或关门槛时，岛 4 在第 4 天打开，多出的坏区间正是 4→5。关同日规划时，岛 2 / 3 / 4 分别在第 4 / 5 / 6 天打开，相对夹具多出 4→5、5→6、6→7 三个坏区间。未用硬编码的解锁日掩盖这些差异。

T04 按原式 `κ * Σ(clears * farmedTime) / 60` 实现。新增测试独立核对所导出的清怪次数、实际充能用量、每日 farmedTime 总和，并在第 4 / 11 / 30 / 104 / 206 / 404 天对照 Python 的已恢复战斗公式。调研 `critic_fid/farmtime.py` 对不走 AB 的刷怪点使用 `clears = limit`，而本契约一律除以 Count。例如 `Spawner_1 (11)`（岛 4）Count = 20、limit = 60，契约只清 3 次，调研计 60 次。仅在同一运行轨迹中恢复这个错误，误差就变成中位 2.30937%、最大 8.14644%，仍不能证明原阈值成立。另有调研 DAFC 在检查可杀性前加入 done 集合的问题。没有更改已恢复的 `CalculateSpawnerData`，没有调整默认 κ 或放大活跃分钟去凑目标。

同一轨迹重新拟合训练段 κ = 0.2564 时，全段中位 2.30%、最大 7.85%；NnTour 初值为中位 1.02%、最大 5.31%。两者只报告；没有做留出 AIC 选择，因此仍保留 FarmedTime 默认（Q24）。TC4–10 误差逐区间入黄金（约 +10.16% 至 +38.07%），不参与后段阈值。

`t04.median_rel_cap` / `t04.max_rel_cap` 的 ORIGINAL 保留设计 0.02 / 0.08，CURRENT 接受实际测量，并经 `--loosen` 写明偏离原因。此前首次测量的完整字典保留，不重写 ORIGINAL。该放宽只冻结已知失败，不代表 T04 满足设计；Z1/K3 需处理阈值与调研次数错误。

恢复后的完整验收代码为 `3f7f828`，18 个作业运行键 `3367f41746c8fc42c47153bbf3ba2e511e654c1863adc26d5b5cc25ffd6e3f32`，B 组运行键 `648627465d06f6e13f0a2ddca6abb16ee93814a04e7d6540907b7ae85e7d920d`。首次冻结的所有键均复现；新增的充能与 farmedTime 预言机检查也通过。主 replay 导出 238456 行、417 轮、401 天，清过的 DAFC 25 个；实际充能用量合计 1132830，free 为 993734。耗时及命令见 [Runbook 第 19 节](Runbook.md#19-快阶段c5)。

## P10 / C6 / K3 交接

- `fasttick_days.jsonl`（输出开关 `fasttick_days`，默认开）：每一天的规划轮、清怪次数、击杀数、活跃分钟、属性/物品/灵魂累积、两阶段的解锁岛、冲刷修饰器数。浮点用 C2 的 IEEE 十六进制编码。
- `spawner_days.csv.gz`（`outputs.spawner_days = true`）：每一规划轮的机器人数据各一行，包含 `day,round,spawner,location,dafc,killable,fights,fight_time,farmed_time,loss,max_health,count,charge_limit,charges_before,charges_after`。每一天一个 gzip member，一个总表头。`spawner` 为 C1 的层级路径与 Key，映射回 `(scene,file_id)`。
- Python `unity.fasttick.read_spawner_days` 读取新表并兼容恢复前的 11 列表。`kill_days` 求首次实际清怪日；`charge_counts` 保留原 API，返回清怪次数；**`charge_usage` 返回实际充能单位**（写入后的用量减之前的用量），不需要再乘 Count。老输出缺少充能列时该函数明确报错。
- T05 闭环阈值由 `calibrate.sensitivity.closed_loop_exact_min(curve, e)` 推出，例如 DPS 误差 2% 对应 ≥35/41；不要把报告值 37/41 当闭环硬阈值。T07/T05 的结构规则均为事后推断。
- 开环 T05 应用 `sensitivity.interval_results(shipped_days, sensitivity.daily_gains(fast_days))` 比较每天实际冲刷的奖励；replay 的强制快照前后夹着教师覆盖，不能直接用模拟快照差值代替奖励。Postmerge 按 C6 + C7 合并代码重测：t07.unlock_days 的岛 2 / 3 从 4 / 4 变为 0 / 2，其余解锁日不变。偏离工作单要求的 1 / 4，因为该要求引用了 C6 合入前的观测；不写入与当前输出矛盾的黄金值。岛 3 第 2 天相对随包第 1 天仍交 K2。次日规划坏区间 17→15，去掉 4→5、6→7；AfterPass 诊断 6→5，当前集合 11→13、17→19、24→27、37→41、140→154；两项各有单独接受记录，默认 StartOfDay 的 37/41 与 4 个坏区间未变。
- C6 已填写物品函数与 P18；Postmerge 重跑带正物品奖励的普通/参考冲刷，并把 inventory-free 送入 X1–X7，验证慢 Tick 物品奖励、快照后缓存与 X2 的集成。C5 没有改 Inventory 的方法体。
- C5 首次验收时 `refresh --level 1` 尚不存在，因此当时分别执行各项检查；P13 合入后的 Postmerge 实际执行记录见 [Postmerge.md](Postmerge.md)。

## 文件归属偏离

沿用首次实现必要的附加文件 `SimulationFastTickRecorder.cs`、`SimulationRunRequest.FastTick.cs`、Python `unity/fasttick.py`、静态测试和相应 `.meta`。`.Rebuild.cs` 仅增加 fast_tick manifest 一行；参数资产补默认字段；cscheck 原警告数少 6 条，更新其基线；签名/参数静态测试对应更新。恢复后补本说明、Runbook 的实测与充能列，没有修改 Design.md、场景或随包数据。
