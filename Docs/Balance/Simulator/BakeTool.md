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
- `bake_report.json`：每个刷怪点的 `SpawnerBakeReportData` 全部字段，数值以 IEEE 位的十六进制表示（与 DP5 同一编码），另加 `SnapshotSource`（读到的是哪个存储快照：天制 `t:<逐日列表下标>`、分钟制 `s:<Snapshots 下标>`，插值得到的为 null）；抛异常的刷怪点记 `error`。`bake_report.csv`：主要字段的往返文本，导入后是 `sim.sqlite` 的 `sim_bake_report` 表。
- `manifest.bake`：两个文件的 sha256、字节数、行数，刷怪点数、注册数、出错数、非有限值个数、读到的快照个数、换入说明。烘焙耗时写进 `timings.json` 的 `bake_report_ms`。
- 钩子 `RebuildJobExportBake` 在 C1 的导出与 C3c 的轨迹之后、`manifest.json` 之前调用。

## 4. X4 与 DP5

- `compare_x4(job_dir, con)`：用本次导出的 `simulation_data.json`（`Snapshots`）和 `timechunk_full.json`（逐日列表，按导出器的位摘要核对过）构造 `SimulationData`，经 `load_bake_context(con, simulation_data=…)` 交给 `model.bake`，并把 `config_time_chunk_snapshots` 设为逐日列表（与 C# 的 `GetSnapshotForBake` 相同）。NewHealth/NewDamage 逐位；中间量相对 1e-12，`NewHealthRaw` 超出时必须正好是编辑器算术（`probes.mono_new_health_raw`，见 [DifferentialProbes.md](DifferentialProbes.md) 第 3 节）；`SnapshotSource` 相同；两边都抛异常的单列。
- `compare_dp5(report, dp5)`：`shipped` 报告与同一次运行里编辑模式 DP5 的报告逐字段比较（十六进制文本相等即位相等）。

## 5. 写回

三个入口都由 `bake_report.py` 在运行工作树上另起一个批处理编辑器执行，持 Unity 锁；结束后运行工作树必须干净、受保护文件不变（与 `params import` 相同的核对，含用户编辑器改写真实存档时的归属规则）。

### 5.1 计划（`bake --plan`）

- 在作业运行时的提交上打开 `Main.unity`（`--rev` 只能指向 `Main.unity` 相同的提交），不保存任何东西。每个场景刷怪点一行：`old_health`/`old_damage`（场景里 `Stats` 第一个 Health/Damage 的 `BaseValue`，即烘焙读的那一个）、`new_health`/`new_damage`（报告），数字之外另有 `*_bits`（十六进制；写回只认位，不认文本，避开 Mono 解析 double 的 1 ulp 误差）。
- `action`：`write`、`unchanged`（新旧逐位相同）、`skip`。跳过原因按固定顺序列出：`event`、`vip`、`boss_tower`（对象名以 `BossTower` 开头）、`dummy`、`hand_balance`、`non_finite`、`not_in_report`、`report_error`、`no_stat`、`duplicate_id`（同一身份在场景或报告里出现多次）。
- 计划里没有时间和进程号，同样的输入得到逐字节相同的文件；口令就是文件的 sha256。进程号、耗时、场景前后的 sha256 写在旁边的 `bake_plan_report.json`。
- 输出目录 `Balance/out/sim/bake/plan-<报告 sha256 前 16 位>-<提交前 12 位>/`。
- `check_plan(plan, report, con)`：Python 用抽取的场景（`spawners` 表）和报告独立重算每一行（旧值、新值、动作、原因）与计数。

### 5.2 写回（`bake --apply --token T`）

Python 在启动编辑器之前拒绝（`refused: …`，退出码 2）：

- 环境变量 `CI`、`TUFU_REFRESH`、`GITHUB_ACTIONS` 任一非空，或调用者不是命令行（`apply(caller=…)`；refresh 与校准不得以 `cli` 调用）；
- 口令不是计划文件的 sha256；计划不是 `Main.unity` 的、没有 `code_rev`、或一行都不写；
- 目标场景（默认本工作树的 `Main.unity`）不存在、是 LFS 指针、在 git 里不干净或未跟踪、与计划记录的文件 sha256 不同；目标在运行工作树里。

编辑器（`SimulationBakeTool.Apply`）再查一遍：本进程从未进入 Play（`[InitializeOnLoad]` 记录 `ExitingEditMode`）、不在 Play、是批处理编辑器；调用者与环境；口令；`Main.unity` 在 git 里干净并已跟踪、与计划的 sha256 相同；每个要写的刷怪点在场景里恰好一个、两个属性都在、当前值与计划的旧值逐位相同（否则是过期计划，一个都不写）。然后：

1. 一个 Undo 组里经 `SerializedObject` 写 `config.Stats.Array.data[i].BaseValue`（`ApplyModifiedProperties` 记录 Undo），逐个读回核对；
2. 场景文件先复制到 `<out>/backup/`，核对 sha256，再 `EditorSceneManager.SaveScene`；
3. 写 `bake_apply_manifest.json`：口令、场景前后的 sha256、备份路径、每个写入的旧值与新值、未变与跳过的个数。

Python 随后把写好的场景复制到目标，再把运行工作树的场景恢复到 HEAD。`SpawnerBakeInfos` 不动（Q13 的另一个选项，未做）。

**本单元从未对任何工作树的真实 `Main.unity` 执行写回**：写回逻辑只在临时场景上验证（5.3），`--apply` 的拒绝路径由不需要 Unity 的测试覆盖（`tests/test_bake_report.py`，含"测试会失败如果启动了编辑器"的替身）。

### 5.3 自检（`bake --selftest`）

`SimulationBakeTool.SelfTest` 在新的批处理编辑器里：

- 守卫：新进程允许；设上"进入过 Play"的标记后拒绝；调用者 `cli` 允许、`refresh` 与缺省拒绝；三个环境变量各自拒绝；口令相符允许、不符与缺省拒绝；`Main.unity` 干净且已跟踪允许，`Temp/` 下的未跟踪文件拒绝。
- 用 `EditorSceneManager.NewScene` 建一个**从不保存**的场景，放 16 个刷怪点覆盖每种动作与原因（含嵌套路径、只改 Health、第二个 Health 行、重复身份、报告里多出的行），生成计划并核对每行的动作与原因、计划文本的确定性；过期计划一个都不写；正式写回 3 个、未变 1 个、跳过 12 个，值逐位正确、第二个 Health 行不动；`Undo.PerformUndo` 全部恢复、`PerformRedo` 重新写入；最后 `Main.unity` 的 sha256 不变、临时场景没有路径。

## 6. 验收记录

（见第 7 节之前的表，由测试运行填写。）

## 7. 与设计稿不同的地方

- 改了 C0b 的 `tufu_balance/unity/__init__.py`（导入 `bake_report` 以登记请求字段与 manifest 键）与 `__main__.py`（`bake` 子命令）。`bake` 子命令另加了 `--selftest` 与 `--plan-file`、`--dest`；不带选项时做 X4（不需要 Unity）。
- 设计稿 1.4.4 说 `bake` 需要 Unity；X4 比较不需要，只有计划、写回、自检需要。
- 跳过原因比设计稿多四个：`not_in_report`、`report_error`、`no_stat`、`duplicate_id`。`boss_tower` 按对象名判断（场景里只有一个，`Count = 0`，NewHealth 为 +Inf，同时也是 `non_finite`）。
- 不需要 Unity 的测试放在 `tests/test_bake_report.py`（`tests/unity/` 下的测试没有 `--unity` 时一律跳过）。
- 计划与写回的产物放在 `Balance/out/sim/bake/`，不放进作业目录（作业目录的文件按 `run_key` 缓存、可逐字节比较）。
- 写回的"不是 refresh 或 CI 调用"用环境变量与调用者参数判定；P13 的 refresh 应设 `TUFU_REFRESH=1`，且不得以 `cli` 调用 `apply`。
