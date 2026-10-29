# 物品、装备与进化（C6）

实现位于 `SimulationSystem.Inventory.cs`、`SimulationRebuildParams.Inventory.cs` 和 `CombatBalanceConfig` 的自动平衡被调方法。默认使用随包进化表（Frozen）、Chain、Swapped、Timed、岛 4/8 开武器槽 3/4、StatsThenItems × SlotBeforeRpm。份数结构、交叉偏好和后期平台均为 **post-hoc**，不是外部验证。

## 契约与参数

- 快阶段按剩余充能计算 `Chain(n,K1) × (1+K0/K6+[preferred]K7)`；E1 把 Chain 换成 n。K1=1 时显式返回 n，n≤0 返回 0。
- 偏好取当前已解锁岛：Swapped 的武器读 PreferredArmorType，护甲读 PreferredWeaponType；Direct 对调回来。比较映射只使用 K7>0 的日子；掉落系数的消融只使用 K0>0.1 的日子。
- 慢阶段每次清怪是一次 `Roll(item.Key, E(d))`。快阶段先按 Key 累积 double，再按 Key 排序逐物品取整。Carry 和 Seeded 都调用 C3c 的 SimulationRandom；不使用 Unity 随机数。
- 份数经 `PlayerModel.AddItem(..., applySilently:true)` 合并，不触发奖励 UI。未进化的快照 Level 使用 EffectiveLevel（等于 Copies）。进化直接执行真实命令相同的份数扣除和 Level/Evolved 更新，避免调用分析/UI 的进化流程；随后调用 RebuildTraceEvolve。
- Timed 需达到目标日且碎片足够；Greedy 只看碎片，TimedLag 多等一天。TimedForce 只作诊断。最高到 MaxEvolutionLevel，不重复消费封顶表。
- weapon_3/4 只由 UnlockSimulationWeaponSlot 开启，调用 RebuildTraceSlotUnlock。weapon_2 和护甲槽仍归门禁 SlotReward；C6 没有把第 1 天硬编码成解锁日。
- P18 的两个正交字段为 DayChangeInventoryOrder（StatsThenItems / ItemsThenStats）和 SlotRpmOrder（SlotBeforeRpm / SlotAfterRpm）。后者将快阶段新物品的合并、进化和武器槽延迟到换天 RPM 重算之后、强制快照之前；进化仍使用奖励所属的旧日。当天即使没有正物品奖励，也保留延迟的定时进化处理。

Regenerate 只允许 reenter_play 批处理作业，直接改注入的 CombatBalanceConfig。它在目标日捕获同稀有度、前一级物品的最大份数，以 nice 舍入成碎片；按 P5 的生成递推写 Value/CopyValue。首次护甲按对应平坦防御的启发值设基础值；作业末尾以最后一个已生成等级的 Value 填充该系列尾部。没有生成过等级的系列保持原表。相关方法的出处分别标 Inferred 或 Calibrated。

Regenerate 的 H 不是精确恢复：武器使用 P5 的渐近线 `type × rarity × cooldown × stat`，护甲使用 `0.8 × stat`。系数放入参数字段 WeaponHeuristicTypeMultipliers、WeaponHeuristicRarityMultipliers、ArmorHeuristicMultiplier，默认分别为 `[1,0.8,0.573]`、`[1,1.25,1.5,1.875,2.5]`、`0.8`；本单元没有重新拟合这些先验。G07 的验收单独喂入存储 H 与碎片，不把启发值近似误差混进生成器的精确计数。

作业开始保存 JsonUtility JSON；结束路径用 finally 恢复，并清空非序列化的捕获状态。Frozen 不改表。inventory.json 记录作业前、运行后、恢复后的三个 sha256，以及相等判断；退出 Play 后仍由原有资产守卫核对内存和磁盘。

## 输出与下游接口

`outputs.inventory = true` 写 `inventory.json`（`tufu.sim.inventory/1`），Python 的 `unity.inventory` 注册开关。它包含：

- rewards：每次取整的 day、key、phase、expectation、copies；totals：每 Key 累计期望与整数份数。
- headroom：目标日首次处理时的 key、level、target、day、current_level、copies、shards、headroom。day 可能迟于 target；这表示目标日尚未获得物品或先前未触发处理，不能称为目标日的精确观测。
- generator：450 行存储输入生成器探针、35 个早期五稀有度 Chain/E1 样本、岛时间换算样本、X6 装备修饰器排除探针。生成器诊断在克隆的 CombatBalanceConfig 上改表，随后销毁克隆，不改注入的实例。

P10 所需槽数已经由 C3b/C1 写入 snapshots_ext.csv，`simio.ingest` 导入 `sim_snapshots_ext.unlocked_weapon_slots / unlocked_armor_slots`；C6 的验收通过实际运行的 sim.sqlite 读取这些列，不再造另一份槽数表。

## 验收方法

`Balance/tests/unity/test_inventory.py` 一次启动运行 13 个 reenter_play 作业：四个份数组合、正物品奖励的参考冲刷、四个 P18 组合、free、checkpoint、Regenerate、Seeded。开环从第 0 天开始 replay_items=recompute（避免首次覆盖后的份数校正混入 TC4–19 增量），经济缓存 stored，槽数使用真实开槽规则。P18 只比较 TC4–11 的 DpsData；P12 用独立 P8 重算，在 `(3,8)/(4,8)/(4,7)` 候选中仅以 TC1–24 的 DPS 选规则，TC≥25 单列。

逐刷怪点实际清怪次数和真实插值配置独立重算快阶段物品期望；逐次调用独立 Python SimulationRandom 核对所有 Carry/Seeded 整数。正物品参考冲刷与默认冲刷逐字节比较两种快照 JSON、两种旁路 CSV 和 digests.csv。G07 先把克隆中所有普通稀有度、非 Universal 的 level≥1 系列的 Value/CopyValue 全部置为 -1，再按等级递增喂入存储输入；因此递推只能消费本次生成的前级。每行导出 before_value/before_copy_value，C# 与 Python 均核对毒值确已生效，再与 P5 和随包表双重比较。离线埋错覆盖未清表的原表 no-op、清表后的 no-op 和只写 Value 三种情况，均须拒绝。

本单元的黄金套件为 sim_inventory。新测量使用 golden accept --seed-original；设计有明确阈值的键声明 original，不把测量值写成设计阈值，不修改既有 ORIGINAL。任何不能满足的阈值另行说明并登记放宽，不能把登记放宽称为原验收通过。

## 所有权偏离

为了实现现有钩子无法表达的行为，修改了 C5 的 FastTick 调用点（E1 选择、按 Key 取整、P18 冲刷和正奖励参考实现）、C3b 的 Run（准备备份、RPM 后生效）和 C0b 的 Harness（所有完成路径恢复、导出）。新增内部成员和 Inventory 自有状态对应更新 C3a 的签名/分部文件断言；原生成员和已恢复的方法体不变。参数资产按合并后的声明顺序补字段；unity/__init__.py 注册新输出。没有修改 Design.md、共享数据库、场景或随包平衡资产。

## C1 文本缺陷的完整摘要确认

首轮 13 作业运行 `868af23b…` 中，默认开环 timechunk_full.json 有一个 CurrentGpm 被写为 `7.0236302039131`，但内存值是 `7.0236302039131004`（差 1 ulp）。逐个保留原始十进制词法文本，交给编辑器同版 Mono 的 double.Parse；只替换这一格后，完整文件的逐位摘要精确等于导出器记录的 `02a4ab5cea7e2542c7822fd1cdb020f5d30afbbf5ce9bb83a9be09ef66e89321`。没有绕过 simio 的检查，没有在测试里代入预言机值凑摘要。

修复 C1 的 SimulationJson.RoundTrips：改用 C3c 已有的精确十进制解析器核对 R/G17 文本，不再信任 Mono 的 double.TryParse。仍保留原来的 R/G17 文本选择，不整体改写为另一种格式。新增离线测试用 .NET 和编辑器同版 Mono 运行真实 SimulationJson，再用 Python 的 IEEE 解析器核对 4,000 个随机 double 与边界、回归样本的位。工具所需的 SimulationJson 源码链接与 export-format 操作属于此次必要的跨单元修复。

资产守卫会遍历 HashSet 的内部版本号；只做 Clear 仍会改变内存摘要。捕获集合在结束时换成全新的空集合，恢复完整状态，而不只是元素内容。验收还要求每个作业的 asset_guard.after_job.changed_in_memory **不含 Combat Balance Config**，不能只凭 JsonUtility 字符串相同就宣称内存恢复。

## 验收结果（最终代码 b572459）

最终运行键 `b5f85b65cc0c75e5846a913b951c71f849e7dd1b0022f2883243ab7a778cca21`，Unity 和启动器退出码均为 0；13 个作业、185.17 秒，guard_problems 为空，13 个 Play 入口，SDK/error_like 日志均为 0。所有作业的完整浮点摘要经 simio 导入核对；资产守卫均通过，changed_on_disk 为空，changed_in_memory 均不含 Combat Balance Config。

| 验收项 | 判定与证据 |
|---|---|
| 四个份数组合、TC4–11 每日五稀有度 | PASS。35 个独立稀有度/日样本均在要求的 5% 带内；另用逐刷怪点轨迹重算全部实际快奖励。Common 日 4–7 为 5.695327，日 10 为 5.965106；Uncommon 为 4.685590–4.907539；Rare 为 3.439000–3.601900；Epic 为 1.900000–1.990000；Legendary 为 1。E1 的各稀有度残差入黄金；TC4–19 武器 C/U/R/E/L 为 +37.86%/+29.15%/+16.05%/+17.74%/0%，不能把设计的“约 25%”当作本次测量。 |
| T12 武器 ≤5% | DEVIATION。按稀有度/阶段取最大值，TC4–19 Epic 为 68 对 62，即 +9.677419%；其它武器阶段 ≤3.67%。逐物品为 coolpike 24/24、doubleaxe 26/26、mace 18/12，全部多出的 6 份来自 mace。其唯一早期来源为岛 4 Epic 的 Spawner_1_epic_buck（Count=1、2 次充能）：重放第 10 天已合格并清两次，loss/H≈0.53；随包 mace 到 TC13 仍为 0、TC15 为 4，推断约第 13 天首杀。单日约 1.99–2.26 份一致，差异指向 C5 快阶段可杀性/资格（pending:simulation.fast_kill_gate），交 K1/C5。四组合的 mace 均为 18 或 19（E1 为 19），P18 无法改变此刷怪点在第 10 天的合格性。没有调参数消掉 6 份差异。ORIGINAL 保持 0.05，CURRENT 接受 0.0967741935483871，并有单独放宽登记；这不是原阈值通过。 |
| T12 护甲 ≤7%、后期 ≤2% | PASS。护甲阶段最大 3.472222%；TC154–405 全部阶段单元最大 1.012430%。TC206–404 每充能平台为 2.5/4.3000001907，传奇 2.7999999523/4.5999999046，最大相对误差 4.4357e−8。均标 post-hoc。 |
| 映射、掉落系数的辨识范围 | PASS。只在整个区间 K7>0 时比较映射；Chain Swapped 的武器/护甲加权残差 0.3296%/0.1506%，Direct 为 0.3576%/0.7008%。有/无 K0/K6 仅在整个区间 K0>0.1 时比较，逐单元值入黄金。 |
| P18 四组合 | PASS（可选择与打分）。40 个 TC4–11 DPS 单元，SlotBeforeRpm 平均/最大 7.6816%/25.3182%，SlotAfterRpm 为 20.6994%/47.1887%。Frozen 下 StatsThenItems 与 ItemsThenStats 同分；默认保持 StatsThenItems × SlotBeforeRpm。四组合没有解释 TC5，Q25 仍交 K1，不能称为已解决。 |
| P12 | PASS。TC1–24 的 85 个 DPS 单元选中 (4,8)：平均 3.058871%；(4,7) 为 7.734179%，(3,8) 为 9.233272%。TC≥25 单列 140 格：平均 1.449333%、最大 12.484544%，未用于选规则；没有拿槽数夹具验证规则。 |
| T11 | PASS。15 个类型/稀有度中 14 个同快照；Uncommon 武器重放为 TC2，随包为 TC1，恰为下一个快照。 |
| T13 | PASS。free 为 253/254 = 99.606299%；checkpoint 为 253/255 = 99.215686%。free 的 C/U/R 为 196/196、E/L 为 57/58；checkpoint 为 196/197、57/58。两种模式各导出 285 行余量表。超带项目：free 的 doubleaxe L5；checkpoint 的 doubleaxe L5、paddle L9。 |
| T17（只对照） | PASS（free 的设计槽数与 checkpoint 的 TC1）；DEVIATION（replay）。free 与 checkpoint 在 TC1 均为 (2,0)、Location=2。free 符合武器 TC1–5=2、TC6–21=3、TC24 起=4。只有 day-0 override 的派生解锁重放在 TC1 为 (1,0)、Location=1，TC2 才到岛 2；同样解释 T11 的 Uncommon 武器晚一快照。weapon_2 由岛 1 pier boss 的 SlotReward 开启，归 C4/C7 重放初始化交接，不是 C6 的槽 3/4 规则或 K2 一般慢门禁偏迟；原版 UnlockSimulationWeaponSlot 本来是空壳。replay/free 的护甲 TC9 前实际为 0；拟合夹具在 TC0–8 为 1，checkpoint 从第 4 天覆盖后会继承该夹具（TC5 已有 1 个护甲槽），不据此检验自然开槽。TC13 夹具也只有 1 而 replay 实际为 2。夹具不是独立验证。 |
| 慢阶段、Carry/Seeded | PASS。默认重放共 16,429 次取整、45 条物品流；四个组合的最大累计差分别 0.994036/0.984797/0.967610/0.974276，均 <1。慢阶段逐次期望等于 E(d)，两种模式的所有整数逐次匹配独立 Python SimulationRandom。 |
| X6 | PASS。同一快照统计函数在加入 Item 来源 +37 修饰器前后 Value 相同，而活的 FastValue 改变。 |
| Frozen/Regenerate | PASS。三个 JSON sha256 与完整资产守卫共同验证；Regenerate 运行中摘要确实改变、结束后恢复，且退出 Play 的守卫没有看到 Combat Balance Config 改变。请求校验和引擎都限制为 reenter_play 批处理。 |
| G07 | PASS。武器、护甲各 225/225 的 Value 和 CopyValue 与 P5、随包表均逐位相同。生成器诊断在克隆上进行。 |
| C5 正物品冲刷 | PASS。参考冲刷和默认冲刷的两个快照 JSON、两个旁路 CSV、digests.csv 逐字节相同；不再是零物品的空验收。 |

`Balance/out/c6/verified/` 保存逐单元 residuals.csv、两模式各 285 行 headroom.csv 和 summary.json；相同测量已经进入版本控制的 sim_inventory 黄金键，忽略目录中的 CSV 不是唯一记录。

### 命令与耗时

以下命令在本工作树 Balance 下，解释器为 `/Users/joyfort/MyProjects/Tufu-stage5/Balance/.venv/bin/python`，环境变量 TUFU_BALANCE_DB 指向只读共享数据库。

- `python -m pytest -q`：861 passed、289 skipped，160.20 秒；跳过包括未启用的 Unity、繁忙机器 perf、未 smudge 场景检查。
- `TUFU_UNITY_REV=b572459 python -m pytest -q --unity tests/unity/test_inventory.py`：接受黄金后 13 passed，12.41 秒，复用已通过的最终运行，不再启动 Unity。
- `python tests/golden/fixtures/generate.py --check`：identical，只有既有 provenance 提示。
- `python -m tufu_balance.cscheck --worktree /Users/joyfort/MyProjects/Tufu-s5-C6`：2.8 秒，带/不带 UNITY_EDITOR 均 0 错误，0 新警告，签名 0、禁用 API 0。总旧警告为运行时 3707、编辑器 8。
- `python -m tufu_balance.golden guard`：20 suites、0 violations。sim_inventory 30 个键首次接受，仅 t12.weapon_phase_max 有放宽登记，ORIGINAL 未被改写。
- `python -m tufu_balance.refresh --level 1`：未运行，当前分支没有 tufu_balance.refresh 模块；交 P13 合并后补跑，不能宣称已通过 refresh。
- `python -m tufu_balance.unity status`：最终运行后 clean=true，锁为空，无运行工作树 Unity 进程。

完整默认重放的 401 个 FastTick：p50/均值/p95/最大 43.40/46.95/67.69/83.40 ms，按中位数 ≤50 ms 的预算通过；free 为 17.32/19.63/43.87/48.93 ms。默认重放慢 Tick 的中位数 1.19 ms，free 为 0.78 ms。测量与 full pytest 同时执行，perf 自动跳过，不声称是空闲机器成绩。

## 后续交接

- K1：四组合和辨识范围、P18 分数、P12 训练/验证分开记录在 sim_inventory。保留 TC5/Q25 未解决；T12 早期 Epic 的 6 份差异保留（mace 18/12；coolpike 24/24、doubleaxe 26/26），与 C5 跟进岛 4 Spawner_1_epic_buck 的首杀资格：重放 day 10，随包约 day 13；不再增加 C6 参数。Direct 的后期拷贝累计误差小也会因碎片阈值导致更大的进化时间偏差。
- C4/C7（K2 知悉）：只有 day-0 override + derived unlock 的 replay 在 TC1 少 weapon_2；free/checkpoint 均为 2 槽、岛 2，不能据此断言一般慢门禁偏迟。weapon_2 是 pier boss 的 SlotReward；不要通过 C6 的 P12 槽 3/4 规则补偿。free 与 checkpoint 的进化超带项目、headroom 可定位阈值耦合。
- P10：直接读取已有 sim_snapshots_ext 槽数；inventory.json 提供逐 Key 整数奖励和期望、碎片余量。checkpoint 的快照含重置，不能把跨重置差值当作奖励；用 rewards 账本。
- C1/P11/Z1：C1 格式缺陷已在 b78aa49 修复，并有全文件摘要确认；后续 text_repairs 应降为 0。新增 Inventory、CBC 的 pending 公式 id 需 Z1 登记；固定 P5 启发值参数、所有权偏离和 T12/T17/Q25 差异需合回设计。
- P13/Z1：合并 refresh 后补跑 level 1。没有修改设计稿、主检出、共享数据库或运行工作树的场景/平衡资产。

## 评审修正与待集成交接

inventory.json 改经 SimulationJson.WriteFile/Write 的 JsonDouble 路径序列化，包括嵌套 JObject/JArray；.NET 与编辑器同版 Mono 的离线回归核对易受影响的 double 位。ConvertIslandTimingToDays 的 199 个样本改按 double_all（binary64 表达式、float 返回时舍入）逐位核对；不再用 stepwise 预言机加 rtol=1e-7，岛 3.5 可区分两者。

- C6 → X2fix/C1/P11：本轮不修改 X2/items 行为，等待引擎决策。C6 的慢阶段清怪会在 Tick 内立即合并份数和自动装备，晚于 Tick 开始的 DPS 缓存；X2fix dc7d002 仅保存属性 basis，按快照末尾物品重算仍可能不一致。评审在旧版 inventory-free 上看到 day 0–3 正奖励数 15/28/40/44，X2 有 78 个 DpsData 未命中、涉及 26 个早期快照；旧版属性/物品混杂，尚不能证明合并后的结果。合并 X2fix 后须跑 smoke-11 与 free inventory 的 X2；若仍失配，由引擎/C1 捕获缓存时的 copies、level、evolved 和 equipped set。此项仅交接，不宣称已修复或已验证合并结果。

### 修正后的实测（C# e02fa37）

只重新启动一次 13 作业批处理，运行键 `44f495f2f04dfa78c4027032a581c876b9158d8db7de632c743c3e2ddea4e8a3`，Unity/启动器均退出 0，192.935 秒；13/13 资产守卫通过，guard_problems 为空，13 个 Play 入口、SDK/error_like 均为 0。每行生成前的两个字段均为 -1；清空整系列后，G07 仍为武器和护甲各 225/225，Value/CopyValue 同时匹配 P5 与随包表。没有重新调整生成器参数。重放的 mace 首次清怪日为 10，2 次清怪，loss/H=0.5258240516092347，Count=1、充能上限 2；TC19 为 mace/coolpike/doubleaxe=18/24/26，与上述归因相符。

`SimulationJson` 的 float G9 输出使 free/checkpoint 各 18 行 headroom.target 的文本从 `298.3333`/`335.8333` 变为 `298.333313`/`335.833313`。逐行确认新旧文本还原后的 binary32 位完全相同，其余字段完全相同。经 `golden accept sim_inventory --keys g07.rows inventory-free.headroom inventory-checkpoint.headroom --loosen --reason ...` 重新接受；记录理由明确写明整系列清空和 no-op 埋错。G07 数值不变，工具只将两个发生文本变化的 CURRENT 键列入 changes，并为这两个精确表新增序列化放宽登记；所有 ORIGINAL 与设计阈值均保持不变。T12 的既有登记理由已补入逐物品归因与 K1/C5 交接。

以下在本工作树 Balance 中使用共享解释器和只读 `TUFU_BALANCE_DB`：

- `TUFU_UNITY_REV=e02fa37 python -m pytest -q --unity tests/unity/test_inventory.py`：13 passed，12.29 秒（重用上述唯一的新 Unity 批处理）；包含 free 的完整 T17 槽数对照、checkpoint 的 TC1=(2,0)、199/199 岛时间 binary32 位比较。checkpoint 从第 4 天起主动覆盖拟合槽数，不能用它检验自然护甲开槽。
- `python -m pytest -q tests/test_inventory_probe.py tests/test_export_double_roundtrip.py`：5 passed，2.64 秒；包括三种 G07 埋错和 .NET/Mono 嵌套 JObject 的精确 double 输出。
- `python -m pytest -q`（本轮修正与重新接受后）：863 passed、290 skipped、24 warnings，156.03 秒；此前同轮为 864 passed、289 skipped、171.21 秒，差别为负载触发的 perf 跳过。
- `python -m tufu_balance.cscheck --worktree /Users/joyfort/MyProjects/Tufu-s5-C6`：OK，2.4 秒；带/不带 UNITY_EDITOR 均 0 错误、0 新警告，签名 0、禁用 API 0。
- `python tests/golden/fixtures/generate.py --check`：identical（既有 provenance 提示不影响内容）。
- `python -m tufu_balance.golden guard`：20 suites、0 violations；本轮没有改动 ORIGINAL 文件。
- `python -m tufu_balance.unity status`：运行工作树 clean=true，锁为空，无运行工作树 Unity 进程。

新批处理默认重放 FastTick 的 p50/均值/p95/最大为 44.3902/49.0619/71.4368/81.8719 ms（401 次，中位数 ≤50 ms）；free 为 17.6695/19.7178/42.3140/49.6111 ms。慢 Tick 中位数分别为 1.2359/0.7847 ms。仍是与全仓测试并行的繁忙机器测量。


## K1 round 2 更正（mace 交接）

早期 Epic 的 +6 份不是可由 K1 KillState / KillLossMultiplier 校准解决的选择。K1 复查原版 6af011f 过滤、只读场景数据、就绪表、实际充能及 model.simcalc.eligible_spawners：岛4 Spawner_1_epic_buck（file_id 57815，OT=10天，Count=1，limit=2）在 day10–12 均 eligible；day10 loss/H=0.525824，禁用 kill predicate 也 day10 首杀。岛4 day5 已由门禁打开，derived override 不注入解锁。随包从 day13 才开始产生 mace，相差三天×2=6，正是 mace18/12、Epic68/62。没有 C5 过滤移植差异的证据；交 K3 的闭环成长/配置状态 A/B 诊断，撤回“用 K1 kill gate 参数解决”的建议。T12 日整数5%不可行与 pre-roll 噪声 allowance 见 Runbook 24.7；阶段检查继续保留失败。
