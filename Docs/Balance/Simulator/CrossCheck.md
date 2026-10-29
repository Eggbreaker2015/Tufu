# 模拟输出的交叉检查（P11）

P11 单元的说明：每次 C# 运行导出以后，用 Python 预言机逐项核对模拟输出（设计稿 3.5 节 X1–X7），以及在 46 个随包天快照状态上核对逐刷怪点经济学（3.4 节 DP8）。导出与导入见 [ExportIngest.md](ExportIngest.md)，状态覆盖见 [Runbook.md](Runbook.md) 第 18 节。

## 1. 怎么跑

```bash
python -m tufu_balance.crosscheck JOB_DIR [JOB_DIR ...] [--db PATH] [--checks X1 X2 ...]   # 不需要 Unity
pytest --unity tests/unity/test_run_crosscheck.py                                            # 一次 Unity 运行，五个作业
```

- `tufu_balance.crosscheck.run_checks(job_dir, con)` 对一个作业目录跑全部检查，返回每项的 `CheckResult`（`status` 为 `pass`、`fail`、`vacuous`，另有 `items`、`mismatches`、`counts`、`vacuous`、`examples`）。
- 每项检查把逐项的比较写成 `<作业>/crosscheck/<检查>.csv`（每行一个比较项，最后一列 `ok`），汇总写 `summary.json`。DP8 写两张表：`DP8.csv`（每个状态、每个刷怪点一行）与 `DP8_rpm.csv`（每个状态、每项属性的 RPM 一行）。
- **vacuous 的含义**：某一部分在这次运行里没有可比较的东西（例如空策略下没有清怪，烘焙报告的模块还没合入），就记进 `vacuous`，写明原因，不会悄悄跳过。整项检查一个比较项都没有时状态为 `vacuous`；有比较项、且全部成立时为 `pass`，其中没比到的部分仍列在 `vacuous` 里。
- 验收用的作业由 `crosscheck.standard_jobs(con, null_policy=True)` 生成：`smoke-11`、`golden-405`（X1–X7），`golden-405-flat`（同上，加平坦的 Cut 伤害与防御各 10，让 K2 的滞后进入 DpsData/DefenseData），`dp8-checkpoint`（C4 的 checkpoint，`unlock = snapshot`，打开 DP8 导出）与 `dp8-reference`（同一作业，不导出）。每个作业都打开 `digest_text`。K3 换成校准版时给 `null_policy=False`。
- 全部检查在 `crosscheck.mono_arithmetic()` 下运行（第 4 节第 1 条）。

## 2. 各项检查

| ID | 模块 | 口径 | 空策略下 vacuous 的部分 |
|---|---|---|---|
| X1 | `composition` | 两个列表的每个快照：SFF、PM、TF、TV 由 Value 与 K2/K3(当天) 经 `compose_stat` 重算，经 C2 的 `probes.encode_snapshot` 逐位比较 | 无 |
| X2 | `dps` | 由导出的物品（`items_ext.csv` 的 `Evolved`）、槽数（`snapshots_ext.csv`）与属性重算 DpsData/DefenseData，逐位。天快照与它在早期列表里的副本用 K2/K3(d−1)（换天时算的，`RpmFirst`），其余早期快照用 `slow_tick_basis.csv` 的 Tick 开头预测属性（K2/K3(d)，战斗奖励之前；X2fix）；系数都在第 d 天。属性取 `GetForecastStatValue` 的游戏顺序 `((1·K3)·(1+K2))·Value` | `k2_lag`（没有平坦伤害/防御属性时 K2(d−1) 与 K2(d) 给出相同的缓存，滞后本身没被测到；`golden-405-flat` 测它）、`rewards_in_tick`（没有清怪）；覆盖作业的非强制早期快照记 `override_slow_tick`（C4 用随包缓存替换，不能由物品重算）；`forecast_order` 表示两种乘法顺序在本次数据上相同 |
| X3 | `rpm` | 沿运行路径重算天列表的 CurrentGpm：每天在换天的状态上（K2/K3(d−1) 的预测属性、该快照自己的 DpsData/DefenseData、Location、第 d 天、记录的当前血量）跑 `CalculateRPMValueTimeChunk`（`forGPMBot = true`），预言机自己从全 0 开始取大；相对 1e-12，另计逐位个数 | replay/checkpoint 作业不适用（覆盖之后会重算 RPM，Runbook 18.2），记为 `override`；`running_max`（没有格子保留高于当天新值的旧 RPM）、`missing_health`（比较状态没有缺血）；盔甲平局规则见下文 |
| X4 | `bake` | 用本次运行的快照（`Snapshots` 与不裁剪的天列表作 `config.TimeChunkSnapshots`）跑 `model.bake`，与 C8 的 `bake_report.json`（`spawners[].report.NewHealth/NewDamage` 的 IEEE 十六进制）逐位比较 | `report`：本分支没有 C8 的导出（预言机一侧仍对 717 个刷怪点烘焙）|
| X5 | `eligibility` | `daily.csv` 每天第一次规划的合格集合（个数与签名）对 `simcalc.calculate_spawner_data(forGPMBot = false)`：当天开始时的状态（强制快照的 Values 在 K(d) 下、由物品与槽数在 K(d) 重算的缓存、Location、P、当前血量），充能清空、冷却清零，只有更早的天里清过的 DAFC 是 `+∞`（`first_kill` 事件）。另计 `eligible_spawners`（加快阶段可杀性门槛）的个数给 C5 | 快阶段的天：最小 FastTick 不规划（`eligible_count = −1`）；覆盖作业整项记 `override`（C4 在强制快照后覆盖，快照不代表规划状态）|
| X6 | `invariants` | Value = 基础值 + 测试赠送 + 奖励（没有清怪时逐位）；充能用量不超上限；P、T 不下降（天列表 T 严格上升）；TC = floor((float)(T/1440))；天列表每天恰好一次、导出的天按裁剪规则、`exported_index` 一致；DAFC 至多清一次、之后冷却一直 `+∞`；每个岛只解锁一次、没有 `repeat_unlocks` | `values_rewards`、`charges`、`dafc`、`unlocks`（都没有发生）|
| X7 | `rpm` | 每个天快照的 CurrentGpm 对"从前一天快照的 CurrentGpm 出发、K2/K3(d−1)"的重算（`RpmFirst`），相对 1e-12；同时用 K(d)（`ModifierFirst`）重算，计不同的格子数，为 0 时记 `order` 为 vacuous。DpsData 一半由 X2 覆盖 | `smoke-11` 的 `order`（两种顺序到第 25 天才第一次分开）；`running_max`、`missing_health` 与 X3 相同 |
| DP8 | `rpm` | checkpoint 作业每个覆盖日的 C# `spawnerData`（9 个 double）与 `simcalc.calculate_spawner_data` 在**只由随包快照构造**的状态上比较，两种状态：覆盖后的 K(d)（`gpm`）与 `RpmFirst`（K2/K3(d−1)、当天存储的缓存、K4/K5 在 d，`rpm_first`）；字段相对 1e-12、键集合相同。锚点：C# 在 `rpm_first` 状态上、只在覆盖日之间取大的 RPM，按 G20 的格子判定对随包 CurrentGpm | 非 replay/checkpoint 作业；`running_max`、`missing_health` 与 X3 相同 |

`counts.running_max_cells` 统计旧 RPM 严格高于当天从零计算的 RPM 的比较格子；`health_states`、`missing_health_states` 统计比较状态及其中 `current_health < Health` 的状态。空策略的 X3/X7 与 DP8 两项计数都是 0，因此明确标记未覆盖。X5 在慢阶段有 3 个缺血状态（新一天的 Health 高于记录的当前血量），但机器人过滤集合的成员只依赖充能、冷却、存活、OT、岛和 `fight_time`，不依赖当前血量；不能据此声称该集合验证了回血数值。

`counts.forecast_data_order_differences` 统计 X2 中游戏顺序与数据顺序产生不同位的预测属性，本次所有标准作业均为 0。`counts.armor_best_ties` 统计检查运行期间实际遇到的最优盔甲平局，本次为 0；X3/X5/X7/DP8 的 `armor_tie_selection` 在计数为 0 时明确指出本次没有遇到平局选择。非 Unity 测试分别用会产生 1 ulp 差异的乘法输入、两种不同类型/抗性的等 DPS 盔甲验证游戏乘法顺序和首次平局优先。

X 检查的输入一律是 C1 的导出文件，读取时核对 C1 的逐位摘要（第 4 节第 2 条）。

## 3. C# 一侧：DP8 导出

C4 的 `override_days.jsonl` 只记缓存与 RPM，不记 `spawnerData`，DP8 必须从 C# 拿到逐刷怪点的字段，所以本单元加了一个只读导出：

- `Systems/SimulationSystem.Crosscheck.cs`（新 partial 文件，只有方法）：`RebuildCrosscheckDay(JObject record)`。作业的输出开关 `crosscheck_dp8`（默认关，Python 侧 `schema.register_output`）打开时，往当天的覆盖记录里加 `crosscheck_dp8`：
  - `gpm`：覆盖刚结束时的 `spawnerData`（`RebuildApplyReplayCaches` 里的 `CalculateSpawnerData(true)`）；
  - `rpm_first`：把插值修饰器临时换成第 d−1 天（`UpdateInterpolationStatModifier`，`timeChunkIndex` 只在这一次调用里改），`CalculateSimulationData` + `RebuildApplyReplayCaches`（当天存储的缓存），把 `RPMCalculatedValue` 换成本作业的取大链，逐项 `CalculateRPMValueTimeChunk`，记下 `spawnerData`、预测属性、缓存与 RPM；
  - 然后恢复：`RPMCalculatedValue` 逐键写回、修饰器换回第 d 天、再算一遍缓存与 `spawnerData`。
- `Harness/SimulationCrosscheck.cs`：开关名与按作业保存的取大链（partial 文件只能放方法，所以状态放在这个静态类里）。
- 调用点：C4 的 `SimulationSystem.Replay.cs` 在 `RebuildApplyDayOverride` 末尾、写记录之前调用一次（改了一行，越过所有权，见第 5 节）。
- 恢复是否完整由 `dp8-reference` 验证：两个作业的 `digests.csv`、`timechunk_full.json`、`simulation_data.json`、`daily.csv` 逐字节相同，`override_days.jsonl` 去掉新键后相同。

## 4. 发现

1. **编辑器 Mono 的 `(double)(1f - armor.Resist)`**：`SimulationSystem.cs:611`（`GetDpsAgainstSpawner`）与 `:640`（`GetBestArmorAgainstDps`）里的 `1f - armor.Resist` 在 binary64 里算、从不存成 float，所以是 `1.0 − Resist`；`model.simcalc` 按逐步舍入先舍成 float。DP8 在逐步舍入下有 322 个刷怪点行超出 1e-12（另有 43 个 RPM 比较行，共 365 项）（最大相对差 3.7e-8，都是 `playerDPS` 及其下游）；换成编辑器的算法后 36514 行、328626 个字段**全部逐位相等**，C# 的 RPM 与 `simcalc.replay_day_gpm`（同样换算法）1380/1380 逐位相等。`crosscheck.mono_arithmetic()` 只在交叉检查里替换这两个函数，没有改 `model.simcalc`（P8 的文件）。这与 C2 在 DifferentialProbes.md 第 3 节记录的规则相同，交给 Q03/Z1 决定预言机是否改用编辑器的算法；G20 的 1380/1380 在两种算法下都成立。
2. **C1 的文本缺陷在本单元的运行里也出现了**（C8 在 BakeTool.md 6.1 先发现）：`golden-405-flat` 的 `timechunk_full.json` 有 1 个 double 被写成 Mono 会读高 1 ulp 的 15 位文本，`simio.ingest` 因此拒绝导入这个作业。交叉检查自己读文件：摘要不符时先照常解析，比较中凡是"C# 文本差 1 ulp、且预言机值的 15 位文本正好解析成它"的格子记为修复项，用预言机的值代入，全部检查结束后把修复项代回两个文件重算 C1 的逐位摘要；只要任何文件的逐位摘要不符，就始终执行完整摘要确认，即使选中的检查没有修复任何格子。摘要不符的文件全部恢复后才算通过；否则所有选中的检查均判失败（包括没有修复项或原本 vacuous 的检查），并写 `export_text.csv`，不能只看已比较的字段。这次修复 1 处（X2 的一个 DefenseData），摘要相同。`smoke-11`、`golden-405`、`dp8-*` 没有这个问题，正常导入。修法仍在 C1（`FormatDouble` 不信任 Mono 的解析）。
3. `smoke-11` 测不到换天顺序：`RpmFirst` 与 `ModifierFirst` 的 CurrentGpm 到第 25 天才第一次不同（C3b 已记录）；`golden-405` 有 3 个格子不同，X7 在那里有信息量。
4. 平坦伤害/防御属性存在时，405 天里有 317 天的 DpsData/DefenseData 能分出 K2(d−1) 与 K2(d)（K2 在其余的天上不变），X2 在这些天全部逐位符合 K2(d−1)。
5. 空策略下 X5 只有慢阶段的 4 天有集合；快阶段 401 天（`golden-405`）没有规划，等 C5。

## 5. 验收记录（设计稿 7.3 P11）

Unity 运行：提交 `b9884ae`（C# 与最终提交相同，之后只改 Python、测试和文档），运行工作树，一次启动五个作业，Unity 进程 44.6 秒；作业耗时 `smoke-11` 0.05 秒、`golden-405` 0.30 秒、`golden-405-flat` 0.32 秒、`dp8-checkpoint` 3.7 秒（`dp8-reference` 2.9 秒）。Python 一侧的全部检查：`golden-405` 约 1.4 秒，DP8 约 1.8 秒。

| 项 | 结果 | 证据 |
|---|---|---|
| `smoke-11` 上 X1–X7 成立 | 通过：X1 9600、X2 800、X3 360、X5 4、X6 2572、X7 360 项，0 不符；X4 vacuous（没有烘焙报告）；vacuous 的部分见第 2 节 | `test_x1_to_x7_hold_on_smoke_11_and_golden_405`，黄金 `smoke11.*` |
| `golden-405` 上 X1–X7 成立 | 通过：X1 56880、X2 4740、X3 12180（全部逐位）、X5 4、X6 15574、X7 12180（全部逐位，`ModifierFirst` 3 格不同）项，0 不符；X4 vacuous（预言机烘焙 717 个）| 同上，黄金 `golden405.*` |
| K2 滞后（X2/X7 的另一半）| 通过：`golden-405-flat` 317 天能分辨，全部逐位；X7 12180 项 0 不符 | `test_x2_k2_lag_with_flat_stats`，黄金 `flat405.*` |
| DP8：46 个随包天快照状态上 `spawnerData` 各字段相对差 ≤ 1e-12 | 通过：46 个状态 × 两种状态共 36514 行，328626 个字段全部逐位相等（最大相对差 0），键集合相同，顺序也相同；`location`、`currentBoss` 与随包快照、P6 一致 | `test_dp8_spawner_data_and_rpm_first_anchor`，黄金 `dp8.*` |
| 锚点：C# 输出上的 RPM 按 `RpmFirst` 1380/1380 | 通过：1380/1380（G20 的格子判定），并且与 `simcalc.replay_day_gpm` 1380/1380 逐位相等 | 同上，`dp8.g20_matches` |
| DP8 导出不改变运行 | 通过：与 `dp8-reference` 的四个文件逐字节相同 | `test_dp8_export_leaves_the_run_unchanged` |
| 每项检查写逐项差异 CSV | 通过：九张表（X1–X7、DP8、DP8_rpm）加 `summary.json` | `test_every_check_writes_its_csv` |
| 比较器能抓到错误（不需要 Unity）| 通过：随包 109 个快照上 X1 等于 G11；只用随包数据构造的 DP8 锚点 1380/1380；埋 1 ulp 的 TotalValue、1e-9 的 fightTime、缺一个刷怪点、一格 RPM、`ModifierFirst` 数据、Value 加 1、错的 TC，各自恰好报出 | `tests/test_crosscheck.py` |

## 6. 与设计稿不同的地方

- **P11 改了 C#**：设计稿 7.1 说 P11 不需要 Unity、只读输出；但 DP8 要的 `spawnerData` 不在任何输出里，所以加了第 3 节的只读导出（新文件 `SimulationSystem.Crosscheck.cs`、`SimulationCrosscheck.cs` 及其 `.meta`），并在 C4 的 `SimulationSystem.Replay.cs` 里加了一行调用（越过所有权，需调度者确认、Z1 补进 7.4 的调用点表）。C3a 的 `tests/test_cs_signature_lock.py` 的 `PARTIAL_FILES` 加了 `Crosscheck`、`.meta` 检查加了新文件。
- 新增输出开关 `crosscheck_dp8`（默认关，在 `override_days.jsonl` 里加键，不新建文件）。
- 新增 `tufu_balance/crosscheck/__main__.py`（命令行）与不需要 Unity 的 `tests/test_crosscheck.py`，设计稿的文件表里没有。
- DP8 的状态由覆盖设好（C4），`RpmFirst` 的状态由 P11 的导出在覆盖日临时构造；取大链只在 46 个覆盖日之间取大（G20 的口径），与运行本身的 `RPMCalculatedValue` 无关。
- 交叉检查在编辑器算法下比较（第 4 节第 1 条），不是 `model.simcalc` 的逐步舍入。
- X3/X7 在 replay/checkpoint 作业上不适用（覆盖后重算 RPM），记为 vacuous；X2 的非强制早期快照与 X5 的规划集合也因 C4 覆盖而不适用，具体原因见第 2 节。
- X5 比较的是机器人过滤集合（`CalculateSpawnerData(false)`，C3c 的轨迹记的就是它），不是加了可杀性门槛的 `eligible_spawners`；后者只计数。
- X6 的"Value = 基础值 + Σ 奖励"在有清怪时按天、相对 1e-6 核对（引擎每次写入点加一个 float 修饰器，这里按天合计），需要 `digest_text`。
- 读 C1 文件时容忍 C1 的文本缺陷并用逐位摘要确认修复（第 4 节第 2 条）；`simio.ingest` 仍然拒绝这样的作业。

## 7. 交给后续单元

- **C1/Z1**：文本缺陷（第 4 节第 2 条）。修好之后 `flat405.text_repairs` 应变成 0（只许变少）。
- **C5**：快阶段的 `RebuildTraceEligible` 接上后 X5 自动比较快阶段的天。若规划在冲刷前一天累积的奖励之后，规划时的状态不是强制快照，X5 需要改用摘要文本里的状态。轨迹记的集合是否加可杀性门槛，与 X5 的口径要一致（现在按不加）。
- **C6/C7**：X2fix 已导出慢阶段 Tick 开头的属性（第 9 节）；C6 若在 Tick 中改变物品或槽数，需同样导出对应的缓存输入，不能用上一张快照代替；X6 的奖励核对需要 `digest_text`（`standard_jobs` 已打开）；DAFC 与解锁的不变量会自动变成非 vacuous。
- **C8**：X4 读 `<作业>/bake_report.json`（`snapshots = "run"`，`spawners[].id/registered/report/error`，与 BakeTool.md 第 3 节相同）；合入后在 `standard_jobs` 的 X 作业里打开 `outputs.bake_report`，X4 就不再 vacuous。
- **K3**：`standard_jobs(con, null_policy=False)` 跑校准后的策略；黄金套件 `crosscheck` 的计数会变，按 3.2 节接受。
- **Z1/Q03**：是否让 `model.simcalc` 在 `1f - Resist` 两处改用编辑器的算法（第 4 节第 1 条）。

## 8. 评审修复

- 覆盖作业的 X2 保留 4100 项强制快照比较，64 个非强制早期快照记 `override_slow_tick`；X5 整项记 `override`。修复前 `dp8-reference` 的 X2 有 180 项假失败、X5 有 2 项假失败。Unity 测试现在检查两个 DP8 作业的全部 X1–X7 状态，不再只检查 X1/X6；DP8 导出作业也执行全部 X 检查。
- 文本修复的故障测试从合成的真实 JSON 文件和 manifest 摘要进入 `run_checks`：已知 15 位文本缺陷能修复并确认；不是该缺陷的 1 ulp 错误必须失败；修复一处但另有未比较字段损坏必须失败；仅运行没有修复机会的检查也必须失败。失败会写逐文件的 `export_text.csv`，状态和 CSV 同时保留完整性失败证据。
- 运行最大值测试把随包末期强状态放在首日，再放入初期弱状态，分别验证 X3、X7 和 DP8 会保留旧 RPM，并抓到每日重置取大链的错误。此数据只用于比较器故障测试，不作为引擎运行基线。
- 增加 X2 武器类型顺序、X5 已清 DAFC 排除与签名错误、当天 K 与前一天 K 在 `fight_time = 300` 边界两侧的集合、X6 时钟回退/天列表重复/裁剪/导出索引的故障测试。另用超出随包天数的合成边界验证 TC 的 float32 商舍入。
- X4 增加 C8 格式报告的非 Unity 测试：717 行全部通过，植入 1 ulp NewHealth 只报出对应一行。C8 尚未合入，因此标准作业的 X4 仍明确 vacuous；没有提前登记 C8 的 manifest 字段或改其它单元的文件。
- 本轮不改 C#，不新增所有权偏离。原有 DP8 C# 导出及跨所有权调用仍需调度者/Z1 按第 6 节处理。

本轮验收：Unity 在 `74e55df` 上经启动器执行一次五作业运行，`run_key = 41faadab5b30c8e7c277a87436ac4132cb8c0276148178aa3c013feb979458f5`，进程 52.46 秒，Unity/启动器退出码均为 0，守卫问题为空。接受仅涉及 `golden405.vacuous_parts` 和 `csv_files` 的两项披露/产物变更（按 P1 对 exact/set 的规则登记 loosening，原版基线不改），随后 `pytest --unity tests/unity/test_run_crosscheck.py tests/test_crosscheck.py` 28 项通过（复用该运行，10.24 秒）。全仓 `pytest -q` 785 通过、114 跳过（230.83 秒）；夹具检查 identical；cscheck 0 编译错误、0 签名违规、0 禁用 API；黄金守卫 15 套件 0 违规。两个覆盖作业全部检查无失败，DP8 仍为 36514 行/328626 字段逐位相等、G20 1380/1380。

## 9. X2fix：慢阶段缓存的属性输入

`Tick` 先 `CalculateSimulationData`，战斗经 `RebuildApplyClear` 发属性奖励，再 `MakeSnapshot`。因此早期非强制快照的 Stats 是战后状态，DpsData/DefenseData 却仍来自本 Tick 开头。X2 原先拿战后属性重算，完整策略下会假报 75 项 DPS 不符（15 张快照 × 5 类武器）。上一张快照也不够：快照之间可以有多个 Tick。

修复只加旁路记录，不改引擎顺序或缓存值：在 `.Run.cs` 的 `CalculateSimulationData` 后立即读取全部 30 项 `GetForecastStatValue`，由 `.Snapshot.cs` 把该字典交给 C1 recorder（仅非强制快照；每 Tick 新字典，不会被后续 Tick 覆盖）。`SimulationExport` 随 `snapshots_ext` 写 `slow_tick_basis.csv`，列为 `list,index,stat,value_hex`；值是 binary64 的 16 位 IEEE 十六进制，避开 Mono 十进制文本缺陷。manifest 记录 SHA256、字节数及行数，`schema.py` 严格校验，`simio.ingest` 自动落表 `sim_slow_tick_basis`，X2 直接用这些预测属性重算。快照 JSON、请求和已有 CSV 语义不变，新增文件是加法扩展，沿用 `tufu.sim.export/1`，没有新 Job 字段。天快照、强制快照副本及 C4 覆盖的处理均不变；新文件已登记或运行有清怪时，缺失/不完整的慢 Tick 输入直接失败，不能降为 vacuous。

### 9.1 随包数据给出的不同证据

`tests/test_crosscheck.py::test_shipped_day_zero_dps_uses_the_recorded_postfight_stats` 从只读随包数据库读早期列表，以当前快照的物品（含 Level/Copies）、岛 1 单武器槽、岛 2 双武器槽、K(0) 独立重算。第 0 天 25 张快照的 **125/125 个 DPS 值逐位相同**。保持物品及槽数不变、仅把属性换为上一张快照时，24 个非初始快照中有 **15 张不符**。

最早有信息量的例子是 `Snapshots[1]`：P = T = 1.1656086444854736，只有 `knife`（Level = Copies = 1，未进化），CutDamageFlat 已从 0 变为 3。存储的 Cut DPS 是 `11.538461961689801`，用该快照属性重算完全相同；用初始属性是 `9.23076956935184`，其它四类依次是 `1.15384619616898` 与 `0.9230769569351841`。此处不依赖开槽或进化推断，也没有后期 K 的舍入歧义。

**结论/交接 C3b、C7**：随包开局 DPS 与记录的战后属性相符，支持“拍快照前已经按奖励后的状态刷新缓存”，与当前重建的 Tick 开头缓存、战后直接拍照的顺序不同。原版空方法体无法仅凭这些样本确定具体重算调用点；不能把上一张快照冒充真正的 Tick 开始状态。X2fix 只修运行输出的自洽检查，不擅自改变恢复的引擎行为。第 1–3 天还混有 K、物品及进化的不确定性，不把它们算作本证据的逐位通过数。

### 9.2 范围及验证

本集成修复按任务授权改 C1 的导出/schema、P11 的输入/比较器及 C3b 的 `.Run.cs`/`.Snapshot.cs` 记录点，跨所有权原因就是原导出缺少缓存输入，交 Z1 补入 7.4。不改 Design.md，不新增 SimulationSystem 成员、参数或请求字段。故障测试分别把 basis 换成前一 Tick、战后属性、前一张快照、缺失记录、不完整记录以及单独错误的防御属性，必须失败；另测 IEEE 值（含负零）导入、重复键及非法 hex。原有 P11 测试与黄金基线保持不变。

### 9.3 X2fix 验收记录

运行 C# 提交 `dc7d0024e0a962e8eb925aba4c9ee19322b58159`，之后只改本节和新 Unity 测试的检查范围。命令均在本单元 `Balance/` 下，用集成工作树的 `.venv/bin/python`，`TUFU_BALANCE_DB=/Users/joyfort/MyProjects/Tufu-stage5/Balance/out/balance.sqlite`（只读）。

| 项 | 结果与命令 |
|---|---|
| refresh level 1 的等价链路 | `python -m tufu_balance.unity run --preset smoke-11` 后跑 `python -m tufu_balance.crosscheck JOB_DIR`，两者退出 0。运行 key `909e17a2321df94b5b58f261d528d357840350c57c26b53ed208ff36ea6dc40c`，Unity 38.40 秒；X2 690 项、0 不符 |
| 当前完整策略 405 天 | `python -m tufu_balance.unity run --preset golden-405`，key `376877171e977642dabc4603f8e33517f026485d0d02b9c911f27bb2748e940c`，Unity 22.40 秒；两个预设目录一起跑 crosscheck CLI 退出 0，X2 4630 项、0 不符。X2 都仅有原来的 `forecast_order` vacuous |
| 引擎行为及其它检查不变 | 启动器在基线 `6dbd753` 跑两个相同默认作业，key `ac1139c114721f74140ab4b4f171828224b9e4043f4338c1c165346c40456ff0`，33.36 秒。每个作业的 `simulation_data.json`、`timechunk_full.json`、`snapshots_ext.csv`、`items_ext.csv`、`daily.csv`、`events.jsonl`、`digests.csv`、`ready_spawners.csv` **8 个旧文件逐字节相同**；X1/X3–X7/DP8 的完整检查摘要相同。原版 X2 75 不符 → 新 X2 0，vacuous 完全相同。复核脚本及 JSON 在 `out/x2fix/compare_before.py`、`before_after.json` |
| basis 导出、导入及埋错 | `TUFU_UNITY_REV=dc7d002 pytest --unity -q tests/unity/test_x2_cache_basis.py`：2 通过（缓存复用，总 24.56 秒）；每个完整策略作业有 53 张慢 Tick 快照、1590 行 basis，`sim_slow_tick_basis` 行数相同。替换为战后属性报 75 项，移到同日上一张慢 Tick 记录报 170 项；前一 Tick/缺失/不完整/防御独立埋错另由非 Unity 测试覆盖。运行 key `3108d7999d26957c997f345c22aba9fe451c6cb131022d8c1f16dc5b1c82190f`，Unity 27.15 秒，两个作业 0.230/6.605 秒 |
| P11 原有 Unity 测试及黄金 | `pytest --unity -q tests/unity/test_run_crosscheck.py`：6 通过，70.05 秒（Unity 49.48 秒）；key `79d3ab396bd7f3b53fa1de1ccca3cc96dfd463472f17ce6fb05632cd9decbcda`。空策略 smoke/golden X2 800/4740 项；DP8 仍 328626 字段逐位相同。所有黄金文件未改、未重新接受 |
| 全仓 | `pytest -q`：862 通过、278 跳过，216.53 秒（Unity 默认跳过，perf 可因负载跳过）；`pytest -q tests/test_crosscheck.py tests/test_simio.py --disable-warnings`：56 通过，5.43 秒；另有 request round-trip 的聚焦回归通过 |
| C#、夹具、守卫 | `python -m tufu_balance.cscheck --worktree /Users/joyfort/MyProjects/Tufu-s5-X2fix`：两种编译 0 错误、无新 no-editor 警告、签名 0、禁用 API 0，2.3 秒；`python tests/golden/fixtures/generate.py --check`：identical；`python -m tufu_balance.golden guard`：19 套件、0 违规 |

五次 Unity 启动均退出 0，`guard_problems = []`，各作业都有 `asset_guard.json`；没有改真实场景/资产，没有新 Unity 编译错误。运行工作树的干净状态由启动器结束检查确认。

### 9.4 额外发现：X6 的摘要奖励总量

为更广地检查，用 `digest_text = true, bake_report = true` 跑同样的完整策略（上表 `3108d799…`）。X1–X5、X7 都通过；X4 为 717/717；X2 仍 690/4630 项全同。**golden-405 的 X6 有 47 个晚期 Value 奖励累加项超出其相对 1e-6 容差**，最早是 day 359 Health：记录值 `77020244.0`，按摘要累加的预言机值 `77020322.0`，相对差约 1.01e-6。这次完整检查 CLI 因 X6 退出 1，不能称为整套通过。

此问题独立于 X2fix：额外输出开关不改变 `simulation_data.json`、`timechunk_full.json`、`daily.csv`、`digests.csv`（与默认预设逐字节相同），这些文件又与 `6dbd753` 基线相同；X6 代码未改。默认预设没有摘要文本，仍按原规则把 `values/charges` 等部分记为 vacuous，因此其完整 crosscheck CLI 退出 0；这些 vacuous 与修复前完全相同，X2fix 没有增加任何跳过。新增 Unity 测试专门检查 X2，不把这个独立 X6 缺陷误当成本单元回归。交 **P11/Z1** 调查奖励累加的实际 float 存储路径；本单元没有放宽 X6 容差或改任何黄金值。带更多输出的 smoke-11 全套仍通过。
