# 烘焙报告与守卫写回（C8）

C8 单元的说明：作业结束时用本次运行的快照跑原版 `SimulationConfig.BuildSpawnerBakeReport`，由 Python 预言机逐位核对（设计稿 3.5 节 X4）；再由编辑器工具生成写回计划，在守卫下把 Health/Damage 写回场景（设计稿 1.5 节 C# 第 6 条、7.3 节 C8、Q13）。运行方式见 [Runbook.md](Runbook.md) 第 10 节。

## 1. 文件

| 文件 | 内容 |
|---|---|
| `CB/Simulation/Harness/SimulationRunRequest.Bake.cs` | 作业字段 `bake`（`snapshots`：`run` 或 `shipped`）与校验 `ValidateBakeFields` |
| `CB/Simulation/Harness/SimulationBakeExport.cs` | 烘焙导出与 `SimulationHarness` 的钩子 `RebuildJobExportBake` |
| `ED/SimulationBakeTool.cs` | 批处理入口 `Plan`、`Apply`、`SelfTest` |
| `Balance/tufu_balance/unity/bake_report.py` | 请求字段登记、报告读取、X4 与 DP5 比较、计划核对、三个入口的启动器 |
| `Balance/tests/unity/test_bake_tool.py` | Unity 验收；`Balance/tests/test_bake_report.py` 是不需要 Unity 的部分 |
| `Balance/tests/golden/{original,current}/bake_cs.json` | 黄金套件 `bake_cs` |

## 2. 怎么跑

```bash
# 在一个作业里打开烘焙报告（Python）
from tufu_balance.unity import bake_report as BR, request as req
job = BR.bake_outputs(req.make_job("golden-405"))                 # 本次快照
job = BR.bake_outputs(req.empty_job("shipped"), "shipped")        # 随包数据（不跑模拟）

python -m tufu_balance.unity bake JOB_DIR                         # X4：C# 报告对 model.bake（不需要 Unity）
python -m tufu_balance.unity bake JOB_DIR --plan [--rev REV]      # 生成 bake_plan.json，打印口令
python -m tufu_balance.unity bake JOB_DIR --apply --token SHA256 [--plan-file P] [--dest SCENE]
python -m tufu_balance.unity bake --selftest [--rev REV]          # 写回逻辑在未保存的临时场景上自检
pytest --unity tests/unity/test_bake_tool.py
```

## 3. 烘焙报告

- 开关是作业的 `outputs.bake_report`（默认关）。`bake.snapshots`：
  - `run`（默认）：天制刷怪点读内存里的 `config.TimeChunkSnapshots`，也就是本次运行**不裁剪**的逐日列表；分钟制刷怪点读 `SimulationData.GetByPlayTime`，所以临时建一个装着本次 `Snapshots`（和逐日列表）的 `SimulationData` 实例（`HideAndDontSave`），换进 `config.SimulationData`，烘焙完在 `finally` 里换回并 `DestroyImmediate`。`manifest.bake.swap.restored` 记录换回成功。本次的 `Snapshots` 或逐日列表为空时作业失败（退出码 1）：否则 `GetSnapshotForBake` 会不声不响地读随包数据。`empty` 作业不能用 `run`。
  - `shipped`：把 `config.TimeChunkSnapshots` 临时换成空列表（`OnAwake` 本来就把它清空了），读随包 `Simulation Data`，与 DP5 在编辑模式下的输入相同，只是在 Play 里。
- 范围：已加载场景里的全部 `EnemySpawner` 组件，包括未激活的（717 个，与 DP5 相同），按场景与层级顺序；每行记是否在 `EnemiesModel.Spawners` 里注册（716 个）。身份是 C1 的"层级路径 + Key"。
- `bake_report.json`：每个刷怪点的 `SpawnerBakeReportData` 全部字段，数值以 IEEE 位的十六进制表示（与 DP5 同一编码），另加 `SnapshotSource`（读到的是哪个存储快照：天制 `t:<逐日列表下标>`、分钟制 `s:<Snapshots 下标>`，插值得到的为 null）；抛异常的刷怪点记 `error`。`run` 时另有 `inputs`：烘焙读到的 `Snapshots` 与逐日列表的全部字段的十六进制（原因见 6.1）。`bake_report.csv`：主要字段的往返文本，导入后是 `sim.sqlite` 的 `sim_bake_report` 表。
- `manifest.bake`：两个文件的 sha256、字节数、行数，刷怪点数、注册数、出错数、非有限值个数、读到的快照个数、换入说明。烘焙耗时写进 `timings.json` 的 `bake_report_ms`。
- 钩子 `RebuildJobExportBake` 在 C1 的导出与 C3c 的轨迹之后、`manifest.json` 之前调用。

## 4. X4 与 DP5

- `compare_x4(job_dir, con)`：用报告的 `inputs`（烘焙读到的 `Snapshots` 与逐日列表，IEEE 十六进制）构造 `SimulationData`，经 `load_bake_context(con, simulation_data=…)` 交给 `model.bake`，并把 `config_time_chunk_snapshots` 设为逐日列表（与 C# 的 `GetSnapshotForBake` 相同）。NewHealth/NewDamage 逐位；DP5 的中间量相对 1e-12，`NewHealthRaw` 超出时必须正好是编辑器算术（`probes.mono_new_health_raw`，见 [DifferentialProbes.md](DifferentialProbes.md) 第 3 节）；DP5 不带的三个报告字段 `DefenseFromSnapshot`、`PlayerBlockChanceRaw`、`PlayerBlockAmountRaw` 逐位（`report_only_*`，预言机的 `defense` 与烘焙所读快照的两个原始格挡值）；`SnapshotSource` 相同；两边都抛异常的单列。
- 本单元的数据上，天制刷怪点全部读插值快照（`interpolated_day` = 609），所以 `SnapshotSource` 的 `t:<下标>` 分支没有被真实数据走到；分钟制的 `s:<下标>` 有。插值舍入 `double_tail` 与 `double_all` 在这两组数据上给出相同的 X4 结果，X4 分不出二者（C2 已经确认是 `double_all`）。
- `compare_dp5(report, dp5)`：`shipped` 报告与同一次运行里编辑模式 DP5 的报告逐字段比较（十六进制文本相等即位相等）。
- 两个比较器本身由 `tests/test_bake_report.py` 用"预言机写出的报告 + 人为植入的错误"检验：NewHealth/NewDamage 各差 1 ulp、一个中间量、一个整数字段、三个报告独有字段、天制与分钟制的 `SnapshotSource`、单边异常，每一处都必须出现在 `mismatches` 里；DP5 比较同理（改一个字段、改一个整数、单边异常、缺一行）。

## 5. 写回

三个入口都由 `bake_report.py` 在运行工作树上另起一个批处理编辑器执行，持 Unity 锁；结束后运行工作树必须干净、受保护文件不变（与 `params import` 相同的核对，含用户编辑器改写真实存档时的归属规则）。编辑器退出后运行工作树一律恢复到 HEAD（共享的运行工作树要保持可用）：除写回允许改动的 `Main.unity` 外，任何改动或新文件先复制到 `<out>/run_worktree_changes/` 留证，算守卫问题（退出码 2，写回不会复制到目标），然后重置索引、检出改动的跟踪文件、删除未跟踪文件。

### 5.1 计划（`bake --plan`）

- 在作业运行时的提交上打开 `Main.unity`（`--rev` 只能指向 `Main.unity` 相同的提交），不保存任何东西。每个场景刷怪点一行：`old_health`/`old_damage`（场景里 `Stats` 第一个 Health/Damage 的 `BaseValue`，即烘焙读的那一个）、`new_health`/`new_damage`（报告），数字之外另有 `*_bits`（十六进制；写回只认位，不认文本，避开 Mono 解析 double 的 1 ulp 误差）。
- `action`：`write`、`unchanged`（新旧逐位相同）、`skip`。跳过原因按固定顺序列出：`event`、`vip`、`boss_tower`（对象名以 `BossTower` 开头）、`dummy`、`hand_balance`、`non_finite`、`not_in_report`、`report_error`、`no_stat`、`duplicate_id`（同一身份在场景或报告里出现多次）。
- 计划里没有时间和进程号，同样的输入得到逐字节相同的文件；口令就是文件的 sha256。进程号、耗时、场景前后的 sha256 写在旁边的 `bake_plan_report.json`。
- 输出目录 `Balance/out/sim/bake/plan-<报告 sha256 前 16 位>-<提交前 12 位>/`。
- `check_plan(plan, report, con)`：Python 用抽取的场景（`spawners` 表）和报告独立重算每一行（旧值、新值、动作、原因）与计数。

### 5.2 写回（`bake --apply --token T`）

Python 在启动编辑器之前拒绝（`refused: …`，退出码 2）：

- 环境变量 `CI`、`TUFU_REFRESH`、`GITHUB_ACTIONS` 任一非空，或调用者不是命令行：`apply(caller=…)` 没有默认值，不给即拒绝；只有 `__main__` 的 `bake --apply` 传 `cli`，并原样传给编辑器（`-tufuSimBakeCaller`）再查一遍。refresh 与校准不得以 `cli` 调用；
- 口令不是计划文件的 sha256；计划不是 `Main.unity` 的、没有 `code_rev`、或一行都不写；
- 目标场景（默认本工作树的 `Main.unity`）不存在、是 LFS 指针、在运行工作树里、在 git 里不干净（工作区或索引）或未跟踪（含被忽略的文件）、与计划记录的文件 sha256 不同。单元工作树的 `Main.unity` 是 LFS 指针，所以在单元工作树里不带 `--dest` 的 `--apply` 总是被拒绝；真实写回要给一个已 smudge、干净的场景作 `--dest`。

编辑器（`SimulationBakeTool.Apply`）再查一遍：本进程从未进入 Play（类带 `[InitializeOnLoad]`，每次域加载时静态构造函数就订阅 `playModeStateChanged`，`ExitingEditMode` 时记入 `SessionState`；不依赖本类先被调用过）、不在 Play、是批处理编辑器；调用者与环境；口令；`Main.unity` 在 git 里干净并已跟踪、与计划的 sha256 相同；每个要写的刷怪点在场景里恰好一个、两个属性都在、当前值与计划的旧值逐位相同（否则是过期计划，一个都不写）。然后：

1. 一个 Undo 组里经 `SerializedObject` 写 `config.Stats.Array.data[i].BaseValue`（`ApplyModifiedProperties` 记录 Undo），逐个读回核对；
2. 场景文件先复制到 `<out>/backup/`，核对 sha256，再 `EditorSceneManager.SaveScene`；
3. 写 `bake_apply_manifest.json`：口令、场景前后的 sha256、备份路径、每个写入的旧值与新值、未变与跳过的个数。

Python 随后把写好的场景复制到目标，再把运行工作树的场景恢复到 HEAD。`SpawnerBakeInfos` 不动（Q13 的另一个选项，未做）。

**本单元从未对任何工作树的真实 `Main.unity` 执行写回**：写回逻辑只在临时场景上验证（5.3），`--apply` 的拒绝路径由不需要 Unity 的测试覆盖（`tests/test_bake_report.py`，含"测试会失败如果启动了编辑器"的替身，每种拒绝核对理由文字）。Python 一侧的整个写回流程（加锁、检出计划的提交、启动编辑器、核对 manifest、复制到目标、恢复运行工作树、额外改动留证并拒绝）用一个替身编辑器在临时 git 仓库里跑通（`test_apply_flow_*`）。真正的 Unity 一侧的"先备份再 `SaveScene`"从未在真实场景上运行：Unity 不允许把场景存到 `Assets/` 以外，而在运行工作树的 `Assets/` 下存临时场景会碰资产守卫，所以自检不做这一步。第一次真实 `--apply` 应由人看着执行。

### 5.3 自检（`bake --selftest`）

`SimulationBakeTool.SelfTest` 在新的批处理编辑器里：

- 守卫：新进程允许；设上"进入过 Play"的标记后拒绝；类带 `[InitializeOnLoad]`，静态构造函数是从 `ProcessInitializeOnLoadAttributes` 调用的（即在域加载时、而不是第一次使用时订阅了 Play 钩子），直接调用钩子处理 `ExitingEditMode` 后守卫拒绝；调用者 `cli` 允许、`refresh` 与缺省拒绝；三个环境变量各自拒绝；口令相符允许、不符与缺省拒绝；`Main.unity` 干净且已跟踪允许，`Temp/` 下的未跟踪文件拒绝；在 `Temp/` 下临时建的 git 仓库里：干净允许、工作区改动拒绝、改动暂存后工作区还原（字节与提交相同）拒绝、被忽略的未跟踪文件拒绝（"未跟踪"）。
- 用 `EditorSceneManager.NewScene` 建一个**从不保存**的场景，放 16 个刷怪点覆盖每种动作与原因（含嵌套路径、只改 Health、第二个 Health 行、重复身份、报告里多出的行），生成计划并核对每行的动作与原因、计划文本的确定性；过期计划一个都不写；正式写回 3 个、未变 1 个、跳过 12 个，值逐位正确、第二个 Health 行不动；`Undo.PerformUndo` 全部恢复、`PerformRedo` 重新写入；最后 `Main.unity` 的 sha256 不变、临时场景没有路径。

## 6. 验收记录（设计稿 7.3 C8）

提交 `6d8b171`，运行工作树，有图形。`pytest --unity tests/unity/test_bake_tool.py` 6 项通过；黄金套件 `bake_cs` 首次测量以 `--seed-original` 写入。

| 项 | 结果 | 证据 |
|---|---|---|
| 作业结束时在 Play 里用本次快照跑原版 `BuildSpawnerBakeReport`；天制读 `config.TimeChunkSnapshots`，分钟制换入瞬时 `SimulationData` 再换回 | 通过。`bake-run`：空策略（`null_policy`）加 8 个平坦攻防修饰器各 10（让 DefenseData 不为 0），跑到第 405 天；读到 68 个早期快照、406 个天快照；`swap.restored = true`；717 个刷怪点（注册 716）、0 个出错、1 个非有限值（BossTower）；716 个刷怪点的 NewHealth 与随包数据上的报告不同（确实读的是本次快照） | `test_run_report_is_the_original_bake_on_this_runs_snapshots` |
| X4：报告与 `model.bake` 在同一快照上逐位相等 | 通过。NewHealth/NewDamage 717/717 逐位；中间量 20076/20076 **逐位**（本次的抗性都让 `(double)(1f - usedResist)` 恰好可表示，所以连 DP5 的 4 个 `NewHealthRaw` 差异也没有出现）；读到的快照 717/717 相同；没有未知或缺失的刷怪点 | 同上，黄金 `run.x4.*` |
| 用随包数据得到的报告与 DP5 一致 | 通过。同一次运行里编辑模式 DP5 与 Play 模式 `shipped` 导出：717/717 个刷怪点、35850 个字段的十六进制全部相同；X4 在随包数据上 717/717；分钟制 HP 精确的集合仍是 G13 的 79 个 | `test_shipped_report_equals_dp5_and_the_oracle`，黄金 `shipped.*` |
| `bake --plan` 生成计划、不碰资产 | 通过。717 行：写 697、未变 0、跳过 20（活动 6、VIP 10、BossTower 1（同时非有限）、Dummy 1、`HandBalance` 2）；编辑器报告 `entered_play = false`、`scene_dirty = false`，`Main.unity` 前后 sha256 相同，运行工作树干净、没有守卫问题；口令等于计划文件的 sha256；Python 用抽取的场景独立重算每一行（旧值、新值、动作、原因、计数）全部相同 | `test_plan_touches_nothing_and_matches_the_scene`，黄金 `plan.*` |
| `bake --apply` 的守卫 | 通过（拒绝路径）。Python 侧：CI/`TUFU_REFRESH`/`GITHUB_ACTIONS`、非 `cli` 调用者、口令错或缺、场景脏、未跟踪、LFS 指针、文件 sha256 不符、非 Main 计划、一行都不写，全部在启动编辑器之前拒绝（替身保证没有启动编辑器）；编辑器侧的同一组检查在自检里逐个验证 | `tests/test_bake_report.py`、`test_tool_self_test_on_an_unsaved_scene` |
| 写回用 `SerializedObject` 加 Undo，保存前备份，写 manifest；在 `NewScene` 临时场景上验证、不保存 | 通过。自检 29 项全部成立（见 5.3）；临时场景始终没有路径，`Main.unity` 不变 | 同上，黄金 `selftest.*` |
| 对真实 `Main.unity` 写回 | **按指示没有执行**（任何工作树都没有）。备份与保存两步只在真实写回里运行，未经实测 | — |

耗时：两个作业的运行 26.4 秒（其中烘焙导出 0.06 秒，`bake_report.json` 3.3 MB，含精确输入）；`bake --plan` 9.6 秒（打开 Main 1.9 秒）；自检 8.0 秒；Python 的 X4 比较 0.25 秒。

### 6.1 发现：C1 文本导出有 1 ulp 的偏差

`bake-run` 的 `timechunk_full.json` 过不了 `simio` 的位摘要核对（`ingest` 因此拒绝这个作业）。原因：`SimulationJson.FormatDouble` 先用 Mono 的 `"R"` 输出，再用 **Mono 自己的** `double.Parse` 验证能否读回；Mono 的解析器会把某些 15 位文本读高 1 ulp（C3c 也遇到过），于是一个错的 15 位文本被当作可往返。例：第 147 天的 DefenseData `113.36911604847501` 被写成 `113.369116048475`，正确解析是 `113.369116048475`（低 1 ulp）。这次 90060 个数里有 3 个（都是 DefenseData，−1 ulp）。

所以烘焙报告另带 `inputs`：烘焙实际读到的两个快照列表的 IEEE 十六进制（DP4 的编码），X4 用它构造预言机的快照，不依赖文本往返；`export_text_check` 把 C1 的文本与它逐值比较，作为诊断记进黄金（`run.export_text.*`，只许变少）。修法（C1 的文件，不在本单元范围）：`FormatDouble` 不再信任 Mono 的解析，例如 15 位文本一律改用 G17，或用正确舍入的解析器验证。交给 Z1/C1。

### 6.2 评审后的补充（提交 `859356d`、`b4b78ab`）

| 项 | 结果 | 证据 |
|---|---|---|
| 编辑器侧"从未进入 Play"是独立的第二层 | 通过。加了 `[InitializeOnLoad]`；自检 38 项全部成立，其中 `initialize_on_load`、`play_hook_registered_at_load`、`play_hook_marks_entered_play` 为新增 | `bake --selftest`，8.1 秒 |
| 比较器能发现错误 | 通过。植入错误的测试；对评审列出的 17 个变异体（X4 `new_ok` 恒真、只看 `new_ok`、DP5 不比字段或只比 NewHealth、计划不核新值、重复判定改成 `> 2`、原因排序、去掉 porcelain 或 ls-files 检查、口令只比前 8 位、去掉运行工作树检查、去掉 `read_report` 的 sha 检查、去掉快照个数检查、报告独有字段恒真、`caller` 默认 `cli`、不恢复运行工作树、`SnapshotSource` 恒真）逐个运行 `tests/test_bake_report.py`，全部被杀 | `tests/test_bake_report.py` 23 项 |
| 报告独有的三个字段 | 通过。`bake-run` 与 `bake-shipped` 都是 2151/2151 逐位（717 × 3） | 黄金 `run.x4.report_only_bits_equal`、`shipped.x4.report_only_bits_equal` |

## 7. 与设计稿不同的地方

- X4 的预言机输入取报告里的精确快照（`inputs`），不取 C1 导出的 `simulation_data.json`/`timechunk_full.json`：后者的文本有 1 ulp 的偏差（6.1）。
- 改了 C0b 的 `Runbook.md`（第 10 节加一行指向本文）、`tufu_balance/unity/__init__.py`（导入 `bake_report` 以登记请求字段与 manifest 键）与 `__main__.py`（`bake` 子命令）。`bake` 子命令另加了 `--selftest` 与 `--plan-file`、`--dest`；不带选项时做 X4（不需要 Unity）。
- 设计稿 1.4.4 说 `bake` 需要 Unity；X4 比较不需要，只有计划、写回、自检需要。
- 跳过原因比设计稿多四个：`not_in_report`、`report_error`、`no_stat`、`duplicate_id`。`boss_tower` 按对象名判断（场景里只有一个，`Count = 0`，NewHealth 为 +Inf，同时也是 `non_finite`）。
- 不需要 Unity 的测试放在 `tests/test_bake_report.py`（`tests/unity/` 下的测试没有 `--unity` 时一律跳过）。
- 计划与写回的产物放在 `Balance/out/sim/bake/`，不放进作业目录（作业目录的文件按 `run_key` 缓存、可逐字节比较）。
- 写回的"不是 refresh 或 CI 调用"用环境变量与调用者参数判定；`apply` 的调用者没有默认值，P13 的 refresh 应设 `TUFU_REFRESH=1`，且不得以 `cli` 调用 `apply`。
- 每次编辑器调用之后都把运行工作树恢复到 HEAD（意外改动先留证、仍算守卫问题），设计稿只要求核对干净。
- 自检不覆盖"先备份再 `SaveScene`"（理由见 5.2）。
