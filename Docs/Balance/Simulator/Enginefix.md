# Enginefix：早期快照缓存与逐写入奖励核对

实现提交 `b7f2e30`；对照提交 `a896e20`。仅本单元工作树提交，Unity 只经启动器；Design.md 未修改。设计证据、调用顺序与偏离分别见 Runbook 16.2a/16.6、CrossCheck 第 9 节。

## 逐文件对照

使用同一请求 `Balance/out/enginefix/comparison_request.json`，含 smoke-11、golden-405、全部 15 个慢策略/性能作业和 3 个门禁作业；两个长作业打开 digest_text 和 C8 bake。

```sh
python tools/audit_cache_refresh.py out/sim/runs/27f35e52daaecaf785648c0e86df04fab790cb18b49f3a9a94c74da318e63ea0 out/sim/runs/6c9c40a16ed228dbca835489a87cc35a8a2ef1185f9b5515abbb37436fde257f --out out/enginefix/output_changes.json
```

20 作业、0 非预期变化。启动器前/后分别 136.78 / 192.73 秒（后者含排队；Unity 进程 134.5 / 144.0 秒）。两个运行全部 exit 0，资产守卫通过。完整逐字段旧值/新值及全部未变文件的 SHA256 位于 [output_changes.json](../../../Balance/out/enginefix/output_changes.json)。

| 作业 | 早期 DPS 变化格 / 快照数 | basis 变化格 | bake JSON / CSV 变化字段数 |
|---|---:|---:|---:|
| gate-current-health | 50 / 10 | 55 | 0 / 0 |
| gate-none | 70 / 14 | 55 | 0 / 0 |
| gate-same-as-fast | 50 / 10 | 55 | 0 / 0 |
| golden-405 | 75 / 15 | 76 | 318 / 127 |
| perf-null | 0 / 0 | 0 | 0 / 0 |
| perf-null-all | 0 / 0 | 0 | 0 / 0 |
| perf-policy-all | 75 / 15 | 87 | 0 / 0 |
| slow-bydist | 75 / 15 | 78 | 0 / 0 |
| slow-clear-then-respawn | 45 / 9 | 63 | 0 / 0 |
| slow-current-health | 65 / 13 | 73 | 0 / 0 |
| slow-decomposed | 75 / 15 | 76 | 0 / 0 |
| slow-decomposed-bydist | 75 / 15 | 78 | 0 / 0 |
| slow-default | 75 / 15 | 76 | 0 / 0 |
| slow-factors | 70 / 14 | 76 | 0 / 0 |
| slow-marginal | 65 / 13 | 70 | 0 / 0 |
| slow-none | 70 / 14 | 65 | 0 / 0 |
| slow-none-retry | 10 / 2 | 3 | 0 / 0 |
| slow-nosmart | 35 / 7 | 37 | 0 / 0 |
| slow-static | 75 / 15 | 76 | 0 / 0 |
| smoke-11 | 75 / 15 | 76 | 318 / 127 |

两个预设各有 53 个非强制慢 Tick 快照；其中 15 个快照的 75 个 DPS 值变化。快照全部其它字段（含 Value、物品、槽位、时钟与 Gpm）、全部 day list、fast phase、daily/events/digests、ready_spawners、fasttick_days 及 spawner_days 均逐字节未变。战斗选择、门禁、死亡、充能、RPM 和引擎计数器未变。

变化文件与字段（`[*]` 表示完整 JSON 清单中逐一列出的索引）：

- `simulation_data.json`：仅 `Snapshots[*].DpsData[*].Value`，均为日 0–3 非强制快照；DefenseData 未变。
- `slow_tick_basis.csv`：仅 `value_hex`（捕获点移到战斗后），行数、键与格式未变。
- `bake_report.json`（仅两个预设）：`inputs.Snapshots[*].dps`；44 个分钟制刷怪点的 `report.{Candidates,CritDps,DpsByArmorType,NewHealth,NewHealthRaw,PlayerDps,PlayerDpsBeforeCrit}`。天制报告不变。
- `bake_report.csv`（仅两个预设）：`new_health`、`new_health_raw`、`player_dps`，共 127 格；Damage 未变。
- `asset_guard.json`：18 个非空策略作业仅 `after_job.changed_sha256.Simulation Config` 变化（内存里 config.Snapshots 的合法派生摘要）；恢复结果和全部磁盘资产摘要不变。
- `manifest.json`：`code_rev`、`run_key`、`request.{code_rev,out_dir,run_key}`；`export.files.simulation_data.json.{bits_sha256,sha256,bytes}`（bytes 仅部分变）、`export.files.slow_tick_basis.csv.sha256`；两个预设另有 `bake.files.bake_report.csv.{bytes,sha256}`、`bake.files.bake_report.json.sha256`。具体每作业字段见 JSON。
- `timings.json`、`unity.log` 为易变运行记录；根目录的 `request.json`、`launcher.json`、`editor_report.json`、`sandbox.json` 为代码身份/路径、进程与耗时差异（字段清单亦在 JSON）；就绪表不变。真实存档备份不是模型输出，不参与比较。

审计比较的是 Unity 与启动器原始输出；`sim.sqlite` 和 `crosscheck/` 是随后生成的导入/核对产物，不计入原始导出文件的逐字节比较。导入表 `c_SimulationData__Snapshots__DpsData.Value`、`sim_slow_tick_basis.value_hex` 会继承上述源数据变化。ForecastSnapshots 读取已修正的 config.Snapshots，属于同一缓存的下游；本批没有单独 ForecastSnapshots 导出。原随包 Simulation Data 与场景资产均未写回。

## 核对与验收

共享 DB 只读。以下命令在本工作树 Balance/ 执行，Python 为 `/Users/joyfort/MyProjects/Tufu-stage5/Balance/.venv/bin/python`，配置 `TUFU_BALANCE_DB=/Users/joyfort/MyProjects/Tufu-stage5/Balance/out/balance.sqlite`。

- `pytest -q`：955 passed，282 skipped，211.00 秒（perf/Unity 按 harness 默认跳过）。
- `pytest -q tests/test_crosscheck.py tests/test_diff.py tests/test_unity_launcher.py tests/test_calibration_harness.py`：334 passed，94.48 秒。
- `python -m tufu_balance.cscheck --worktree /Users/joyfort/MyProjects/Tufu-s5-X2fix`：4.8 秒，带/不带 UNITY_EDITOR 均 0 错误、无新增警告，signature 0 violations，bans 0。
- `tests/golden/fixtures/generate.py --check`：identical。
- `python -m tufu_balance.crosscheck <after>/golden-405 --checks X1 X2 X3 X4 X5 X6 X7`：全部 pass；X2 6220、X6 232456 项，0 mismatches；X4 717 项，0 mismatches。
- `python -m tufu_balance.refresh --level 1 --rev b7f2e30 --out out/ef1`：13/13 步退出 0，104.56 秒，CURRENT 未变；其中探针 14 项、smoke-11 的导入与 X1–X7 均通过。
- 原样命令 `python -m tufu_balance.refresh --level 1` 也通过：13/13 步退出 0，74.91 秒，CURRENT 未变（Unity 作业复用上面的成功缓存）；报告 `out/refresh/20261029T072619.348321Z/summary.md`。
- `python -m tufu_balance.golden guard`：19 suites，0 violations。
- `pytest --unity tests/unity/test_engine.py`（短 TMPDIR）：21 passed、1 perf skipped，66.66 秒；包含停用模块和停顿心跳，未使用用户编辑器例外。
- `pytest -q --unity tests/unity/test_{engine,slow_policy,run_crosscheck,x2_cache_basis,determinism,bake_tool,diff_e2e}.py --junitxml=out/enginefix/unity-final.xml`：103 passed、2 perf skipped，348.06 秒，0 fail/error。代码修订用 `TUFU_UNITY_REV=b7f2e30`；最终提交的 Unity 源码与该修订相同。逐模块：engine 21+1 skip，slow_policy 61+1 skip，run_crosscheck 6，X2 basis/植错 2，determinism 4，bake_tool 6，diff_e2e 3。
- 随包日 0 的 25 张快照（125 个 DPS 值）逐位通过；重建默认策略全部 53 张非强制慢 Tick 快照的 DPS/Defense 和 basis 均由自身状态通过。X2 的战斗前缓存、错位 Tick、错误属性植错均失败；X6 的 2^24+1 写入、丢失慢写入与错误值植错均失败。
- 共享 DB 最终 SHA256 `a7a2e3c5d048c57f9a77d3057dd0c35ed30cf799fe6612529961c44a18e3e098`，权限仍为 444。通过启动器 status 核对运行工作树干净；Design.md、ORIGINAL 和冻结夹具未修改。

## 黄金与交接

仅接受 `crosscheck` 的 3 个定义变化键：`flat405.x2`、`golden405.X2`（4740 → 6660）和 `smoke11.X2`（800 → 2720）。各空策略作业的 64 张非强制快照额外逐位核对全部 30 项 basis，即增加 1920 个断言；状态仍为 pass，mismatches 仍为 0。接受理由含 `engine cache refresh before early snapshot (shipped-data evidence)`。ORIGINAL 不改，X2 逐位与 X6 的 `VALUE_REL_TOL = 1e-6` 不变，X6 实际比较还收紧到逐位。

P1 对无方向的 exact 对象的任何值变化都要求 `--loosen` 登记，所以此处保留工具生成的 3 条 exact 定义变更登记及同一条 accept 记录；这不是数值容差放宽，也没有接受失败的状态或非零 mismatch。`sim_engine`、`sim_slow`、`bake_cs`、`sim_determinism` 的已测键没有移动，无需重新接受。`sim_fasttick` 没有因本次修改移动的键，亦不接受它的旧失败。

- Z1：把 4.4 Tick 的早期缓存刷新顺序折回设计；7.4 跨所有权修改由本 Enginefix 工作单授权。
- C6：刷新发生在奖励/装备处理后，X2 直接读快照自身物品/槽位；无须再导出 Tick 开头的物品。合并后重跑 inventory 与本次 X2 植错测试。
- P11：X6 使用真实的 float32 奖励修饰器写入与 double BaseValue 分离，慢日也逐位；未放宽 VALUE_REL_TOL。
- P12：Unity 端到端测试也断言没有任何失败检查，所有 X2 fail 致命；就绪表相对且内容寻址，后端显式传配置 DB 到启动器。reconstructed 无登记实现者时仍为未建模，报表措辞与统计一致。

## 额外检查与运行限制

- 上轮额外跑 `pytest --unity tests/unity/test_fasttick.py`：14 passed、1 perf skipped、4 failed（509.09 秒）。三个测试假定所有 16 个解锁都发生在快阶段，但本分支原来已在慢阶段第 1 天解锁岛 2，所以快阶段只有 15 个；另一个是旧 `t07.unlock_days` 的岛 2 期望 4，实际 1。对照 `a896e20`（78.67 秒）和 `b7f2e30` 的 `fast-405`、`fast-405-nextday`：day list、daily、events、digests、fasttick_days、快照/物品旁路及就绪表逐字节相同，全部解锁日相同（见 `Balance/out/enginefix/fast-preexisting.json`）。这是 C5/C7 集成后的已有测试假设，不改快阶段或此黄金。其旧 latest 观测已归档为 `out/enginefix/preexisting-sim_fasttick-latest.json`，供独立检查；L1 不执行该模块。
- 第一轮本次 resume 的临时目录过深：Unity Bee 报 Unix domain socket 路径超出 macOS 的 104 字符限制，得到进程退出码 1，资产守卫无问题。改用工作树内短 TMPDIR `Balance/out/t`，refresh 使用 `--out out/ef1` 后全部 13 步退出 0，104.56 秒，CURRENT 未变。P13 交接：长 `--out` 下的自动 TMPDIR 存在此限制；未改 refresh 工具或系统临时目录。

慢策略测试辅助函数对只读的每作业参数做缓存，避免在每个战斗事件上重复解析相同的 C# 参数元数据；没有删减事件或数值断言。引擎 manifest 测试改用全部已解析默认参数，并将 9 位 JSON float 文本归一到 float32 后比较，修复 C5/C7 合入后旧核心参数对象不完整的问题。
