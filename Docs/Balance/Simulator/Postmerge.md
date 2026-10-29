# Postmerge 集成验收

## Round 2：C4 / C7 集成修复

结论：**本轮验收全部通过，没有未完成的 Postmerge 阻塞项**。完整 Unity 最终确认 **287 passed、3 perf skipped、0 failures / errors**，523.88 秒；全仓 Python **969 passed**，refresh L1 **13/13** 步通过。

实现提交 `97c986f`。本轮将上轮 15 个失败逐项归为 **(b) C6 合入后测试 / 夹具假设陈旧**；没有发现需要修改生产策略或覆盖逻辑的错误。C# 只在已有 C7 调试探针开启时追加候选成本事件，默认模拟的行为没有改变。完整验收结果在本节末尾记录；下方 Round 1 是历史归档。

### 判据与独立证据

- 原版 `6af011f` 的 `SimulationSystem.cs:765–786` 只保证 `currentBoss` 的奖励值为 1e6；`FindBestSpawner` 和 `SpawnerData.GetPriority` 都是空壳，没有“无条件优先门禁”的原生证据。设计 4.5 的优先级族是奖励除以时间；从奖励 1e6 推出门禁必胜的后一句不总成立。C6 改变战力和路线后，`slow-bydist` 在第 2 天 P=125.751366 出现门禁优先级 **98462.47958719311**，低于普通 Cut 战斗的 **126445.349354054**。保留既有优先级规则，用全允许集合的 argmax 替换过强推论，属于需 Z1 回填的设计偏离。
- C7 的 `selection` 探针现在在每个合格门禁 Tick 写 `slow_selection`：按原顺序排列的候选、时间成本、失血、跳过标志和实际选择。测试从只读 DB 取奖励、Count、PriorityMultiplier，独立计算门槛和优先级，核对最高者、首个平局者以及三类决策计数。四个变体共 **212** 个决策（61 / 32 / 73 / 46），唯一允许但未选门禁的反例正是上述距离时间变体。没有直接相信 C# 写出的 priority 或 reward。
- 旧岛 3 夹具结束慢阶段时 P≈236，未曾检查 OT=240 的门禁，不能用于致死门槛验收。单改 Cut grant 的 15 作业扫描没有给三个门槛都产生覆盖；随后扫描仅用于夹具的时钟。在 CMove=2/4/8 的扫描中取最小值，使用 `Decomposed / CMove=2`，保留随包敌人、开局岛 2–3 和 Cut +1200：SameAsFast / CurrentHealth 在第 3 天 P=240.17363、loss/H=1.217195 拒绝，None 在 P=240.295959 选中并死亡 / 复活一次，岛 4 不开。逐事件门槛和死亡断言全部保留；随包 TC3 的 loss/H≈4.16 仍由静态 P8 预言机单独检查。没有更改默认 CMove 或任何校准参数。
- 原版 `ItemModel.cs:135–175` 定义有效等级与碎片费用，`EvolveItemCommandRunner.cs:25–44` 明确消费 Copies、首次设 Evolved、后续升 Level。设计 4.5 Inventory 的 Timed 与 4.6 的“覆盖后继续闭环”允许这些转移。C6 在没有正掉落时仍处理到期进化；null_policy 只关闭战斗 / 规划。C4 覆盖后的即时记录继续逐项精确核对请求，跨日则使用 **P4 原版进化转移 + 随包时间 / 碎片表** 预测物品，再独立重算 DPS / DefenseData；没有冻结物品，也没有忽略等级或份数。
- C4 replay 有 **167** 个进化事件、**118** 个即时覆盖之后的跨日区间改变物品；Evolved flip 有 3 个进化事件；checkpoint 有 **20** 个进化事件。跨日 Values 仍须不变，所有物品和缓存仍须精确符合独立预测，且测试强制要求确实覆盖有进化的区间。

原始测量与脚本结果在 `Balance/out/postmerge/round2/`：`c4-evolution-evidence.json`、`c7-selection-evidence.json`、`gate-sweep.json`、`gate-time-sweep.json`。原版 / 设计的依据均已只读核查，没有修改 Design.md。

### 上轮 15 个失败的逐项裁决

| # | 上轮测试（省略模块前缀） | 裁决与保留的性质 |
|---|---|---|
| 1 | C7 gate / slow-default | (b) C6 掉落使解锁从 `{2:1}` 变成 `{2:0,3:2}`；保留门槛、独立 argmax、决策计数及解锁诊断 |
| 2 | C7 gate / slow-bydist | (b) 1e6 除以成本后输给普通战斗是允许集合的正确 argmax；原硬断言改为逐 Tick 全集合检查，并首次测到此前被硬失败挡住的 unlock key |
| 3 | C7 gate / slow-nosmart | (b) 同 #1；禁用 SmartPath 的独立轨迹仍检查 |
| 4 | C7 gate / slow-current-health | (b) 同 #1；CurrentHealth 谓词和不死亡性质仍检查 |
| 5 | C7 island3 lethality | (b) 旧夹具没有合格事件；只改夹具时钟恢复三门槛的拒绝 / 选择 / 死亡覆盖，不删 `assert gates` |
| 6 | C7 deaths / None + SkipSpawner | (b) C6 改变闭环战斗，死亡 55→43；逐场死亡、复活、跳过会话、清怪计数仍检查 |
| 7 | C7 deaths / ClearThenRespawn | (b) 同上，死亡 41→11；死亡后清怪奖励与回血、位置仍检查 |
| 8 | C7 death_policy_changes | (b) 汇总中 None 55→43，default=2 / CurrentHealth=0 不变；保留三者严格顺序和 Retry 摘要差异 |
| 9 | C7 smart_path | (b) 新物品改变路线 / 清怪，顺路清怪 175→311；保留路径几何、允许集合、关闭 SmartPath 和计数核对 |
| 10 | C7 counters | (b) 12 变体的闭环轨迹诊断随 C6 移动；每种时间模型、血量、门槛、写入点和计数的硬检查仍在 |
| 11 | C4 replay immediate state | (b) 即时状态与 RPM 硬检查均通过，只有 fresh-RPM 精确相等细胞数 14472→14464；进化改变跨日缓存及 RPM 历史最大值 |
| 12 | C4 replay carry | (b) 取消“不允许进化”的隐含假设；Value 不变、进化后的物品、DPS / DefenseData 均须精确符合独立预言机 |
| 13 | C4 day-change RPM | (b) 所有 CurrentGpm 与 max(既有 RPM, 新重算) 逐位一致，只有 carried 诊断 750→730 |
| 14 | C4 Evolved flip | (b) 请求即时 Evolved 两向覆盖仍精确断言；下一快照按请求允许的 Timed 进化预测，不丢弃份数 / 有效等级检查 |
| 15 | C4 checkpoint continues | (b) 每次重置仍精确符合随包值；两个 checkpoint 之间连续应用随包进化转移，405 个跨日状态及缓存逐项检查 |

### 本轮全部黄金变化

经 P1 `golden accept` 接受 **13 个 CURRENT 键、3 条记录**。exact 诊断值的任何变化都要求工具登记 `--loosen`，本轮已登记；这没有改 ORIGINAL、比较 kind、数值容差或默认参数。没有 seed-original、rebaseline-original 或手写基线。

| 套件 / 键 | CURRENT 旧→新 | accepted 记录（行号） |
|---|---|---|
| sim_override / replay405.rpm_cells_equal_fresh | 14472→14464 | `accepted/sim_override.jsonl:5`，C6 到期进化与跨日缓存 |
| sim_override / replay405.day_change_gpm_carried | 750→730 | 同上 |
| sim_slow / unlock_days.slow-default | `{2:1}`→`{2:0,3:2}` | `accepted/sim_slow.jsonl:4`，C6 闭环轨迹 |
| sim_slow / unlock_days.slow-bydist | `{2:1}`→`{2:0,3:2}` | 同上；上轮硬失败遮住了这个测量 |
| sim_slow / unlock_days.slow-nosmart | `{2:1}`→`{2:0,3:2}` | 同上 |
| sim_slow / unlock_days.slow-current-health | `{2:1}`→`{2:0,3:2}` | 同上 |
| sim_slow / deaths.slow-none | 55→43 | 同上 |
| sim_slow / deaths.slow-clear-then-respawn | 41→11 | 同上 |
| sim_slow / deaths.by_gate | None 55→43，另两项不变 | 同上 |
| sim_slow / smart_path.en_route_clears | 175→311 | 同上 |
| sim_slow / counters | 12 变体诊断；default clears 403→854、ticks 237→557；完整旧 / 新见记录 | 同上 |
| sim_slow / island3_gate.first_loss_per_h | 4.501→1.217 | `accepted/sim_slow.jsonl:5`，C6 后恢复夹具覆盖 |
| sim_slow / island3_gate.first_p | 240.072→240.174 | 同上 |

每条记录的测量提交为 `97c986f`。上轮接受的 8 个键本轮未改，清单保留在下方归档。

### 最终验收

命令均在本工作树 `Balance/` 下运行，解释器 `/Users/joyfort/MyProjects/Tufu-stage5/Balance/.venv/bin/python`，数据库环境 `TUFU_BALANCE_DB=/Users/joyfort/MyProjects/Tufu-stage5/Balance/out/balance.sqlite`，pytest 的 `TMPDIR=$PWD/out/postmerge/tmp`。Unity 固定 `TUFU_UNITY_REV=97c986f`；refresh 显式 `--rev 97c986f`。首次完整 Unity 是冷运行，最终确认复用同一提交的成功运行；确定性强制重启、自检、失败型作业和参数导入仍实际执行。

| 项 | 结果 | 命令、计数与耗时 |
|---|---|---|
| 上轮 C4 / C7 的 15 个失败 | PASS / DEVIATION | 冷全套中 C4 **18 passed**（18.92 秒），C7 **61 passed、1 perf skip**（9.97 秒）；每项裁决见上表；设计偏离仅为修正门禁必胜推论和测试专用夹具时钟 |
| 完整 Unity 最终确认 | PASS | `python -m pytest -q --unity tests/unity --junitxml=out/postmerge/round2/unity-final.xml --durations=25`：**287 passed、3 perf skipped、0 failures / errors**，**523.88 秒**；所有负向 harness 作业均通过，没有 user-editor skip |
| 首次冷全套 | 环境失败，已重试 | 同一全套命令：**284 passed、3 failed、3 perf skipped、0 errors**，**1940.71 秒**。3 个失败均是用户主编辑器写存档；下面单独列证据，不能将冷运行报告成全绿 |
| 用户编辑器阻塞的负向作业 | PASS（重试） | `pytest -q --unity tests/unity/test_engine.py::test_disabled_modules_fail_the_job tests/unity/test_engine.py::test_heartbeat_follows_ticks_in_the_job_phase tests/unity/test_state_override.py::test_slots_beyond_the_player_fail_the_job`：**5 passed**，**314.16 秒**（含 Unity 锁排队） |
| 全仓 Python | PASS | `python -m pytest -q --junitxml=out/postmerge/round2/pytest-final.xml --durations=15`：**969 passed、296 skipped、24 warnings**，**360.17 秒** |
| refresh L1 | PASS | `python -m tufu_balance.refresh --level 1 --rev 97c986f --out out/postmerge/round2/refresh`：**13/13 步 exit 0**，**302.56 秒**，CURRENT unchanged=true |
| digest-enabled golden-405 | PASS | `python -m tufu_balance.unity run --request out/postmerge/golden405-request.json --rev 97c986f` 等价 API 请求：launcher / Unity **exit 0**，**32.2 秒**；ingest 成功，X1–X7 共 **349103** 项、**0 mismatch**，**16.66 秒**。`digests.csv`、`events.jsonl`、`timechunk_full.json`、`simulation_data.json` 与 Round 1 逐字节一致，证明正常策略未变 |
| C6 inventory-free 慢掉落与 X2 | PASS | 全套 inventory **14 passed**；**127** 次正慢阶段物品发放；X2 **6340** 项，含 **56** 个 slow-tick 快照，**0 mismatch**。X1 / X3 / X5 / X6 / X7 也通过；X4 无 bake report，明确 vacuous |
| W0 fixture | PASS | `python tests/golden/fixtures/generate.py --check`：`identical`；只提示导入模块的 provenance 变化 |
| C# | PASS | `python -m tufu_balance.cscheck --worktree /Users/joyfort/MyProjects/Tufu-s5-Postmerge`：**2.2 秒**；带 / 不带 UNITY_EDITOR 均 **0 errors**，签名 **0 violations**、禁用 API **0 uses**；原有 warnings 3707 / 8，没有增加 |
| 黄金守卫 | PASS | `python -m tufu_balance.golden guard`：**20 suites、0 violations**；13 个 CURRENT 键都有上表记录，ORIGINAL / fixtures / 默认参数未改 |

完整 golden-405 的 X1–X7 项数为 **55920 / 6340 / 12180 / 717 / 405 / 261361 / 12180**，各 0 mismatch；仍逐项列出 forecast_order、missing_health、running_max、armor_tie_selection 等没有区分样本的分支，未用通过数掩盖未覆盖分支。运行键 `b67cdb88f367…`，报告 `out/postmerge/round2/golden405-crosscheck.json`。Inventory-free 运行键 `b95a151fd33a…`，报告 `inventory-free-evidence.json`。

冷全套的三个环境失败是 `test_disabled_modules_fail_the_job[AutoGambling]`、`[AutoBalanceAmulet]` 和 `test_heartbeat_follows_ticks_in_the_job_phase`。对应原始 launcher 报告的 `unity_exit` 为 **5 / 5 / 3**，`guard_problems` 都只有 `real save file changed: game_data.bin`，`main_checkout_editors=[83799]`，没有工程或资产守卫问题。较早的 C4 invalid-slot 作业也是同一原因；它在 focused 测试中跳过，在上述重试及冷全套中已通过。首次报告副本在 `out/postmerge/round2/guard-blocked-first/`，没有用后来覆盖的成功报告冒充最初结果。

通过 launcher 的只读快照监测 **602.64 秒**，观察到 **80** 次内容变化，最后达到 **90.26 秒**安静；从首次 guard exit 到安静确认是 **724.02 秒**，小于工作单的 15 分钟上限。然后才排队重试；没有触碰、发信号或操作用户编辑器。证据为 `quiet-monitor-timing.json` 与 `guard-retry.xml`。冷全套原始报告保留为 `unity-cold.xml/.log`，最终确认另写 `unity-final.xml/.log`。

最终完整 Unity 按模块计数如下，耗时包括该模块的 pytest setup / call / teardown 及排队；三个 skip 只来自忙机器的 perf 标记。

| 模块 | passed | failed / errors | skipped | 秒 |
|---|---:|---:|---:|---:|
| bake_tool | 6 | 0 | 0 | 1.23 |
| determinism | 4 | 0 | 0 | 30.56 |
| diff_e2e | 3 | 0 | 0 | 70.49 |
| differential | 14 | 0 | 0 | 3.09 |
| engine | 21 | 0 | 1 | 71.33 |
| export_roundtrip | 7 | 0 | 0 | 0.67 |
| fasttick | 18 | 0 | 1 | 18.29 |
| headless_smoke | 6 | 0 | 0 | 66.05 |
| inventory | 14 | 0 | 0 | 30.44 |
| params | 15 | 0 | 0 | 40.90 |
| patcher_parity | 73 | 0 | 0 | 15.56 |
| run_crosscheck | 6 | 0 | 0 | 8.38 |
| sandbox | 19 | 0 | 0 | 115.15 |
| slow_policy | 61 | 0 | 1 | 9.83 |
| state_override | 18 | 0 | 0 | 16.81 |
| x2_cache_basis | 2 | 0 | 0 | 20.41 |

共享 DB 最终 SHA256 仍是 `a7a2e3c5d048c57f9a77d3057dd0c35ed30cf799fe6612529961c44a18e3e098`，mode **444**。launcher 最终 status 为 run worktree **clean=true、lock=null、unity_processes=[]、serve.running=false**。没有改 ORIGINAL、冻结 fixtures、默认参数或真实游戏资产；交付提交的 C# / Unity 工程字节与已验收 `97c986f` 相同。基线和本报告提交后，本单元 `git status` 为空；未 push、切换其它开发分支或删除工作树。

### 交接

- **C4 / C7**：上轮 15 项已逐项处理；生产行为没有强制门禁优先、禁止进化或延后解锁之类的改动。
- **Z1**：更新设计 4.5 / 7.3 的“1e6 因而必胜”推论；记录 C7 门禁夹具的测试专用时钟，及 C4 空战斗策略仍允许 Timed 进化。
- **K2**：岛 3 第 2 天对随包第 1 天仍是校准交接，不能用测试修复消除；T04 / Q25 等既有未解项未变。

## Round 1 历史归档（以下不是本轮最终状态）

结论：**部分通过，完整 Unity 验收仍被 C4 / C7 阻塞**。最终全模块运行是 272 passed、15 failed、3 perf skipped、0 errors，602.44 秒；不能报告成全绿。15 个失败中，10 个属于未接受的诊断黄金变化，5 个是硬断言失败。其余模块全部通过。

基点 `c5a1450`；实现提交 `92e5cb1`、`ec7094e`。Unity 验收固定 `TUFU_UNITY_REV=92e5cb1`，本单元最终版本的 C# 和资产字节与此修订完全相同；最终 Python 与测试使用工作树中的修正。所有 Unity 进程均经过运行工作树的受守卫启动流程。没有修改 Design.md、ORIGINAL、冻结夹具、共享数据库、C# 或随包资产。

## 验收对照

以下命令均在本工作树 `Balance/` 下，解释器为 `/Users/joyfort/MyProjects/Tufu-stage5/Balance/.venv/bin/python`；输入环境 `TUFU_BALANCE_DB=/Users/joyfort/MyProjects/Tufu-stage5/Balance/out/balance.sqlite`，普通测试的 `TMPDIR=$PWD/out/postmerge/tmp`。

| 项 | 结果 | 命令、证据与耗时 |
|---|---|---|
| C5 按慢 / 快阶段分别计解锁，保留总计 16、岛 2–17 各一次 | PASS / DEVIATION | `pytest -q --unity tests/unity` 的 fasttick 模块 18 passed、1 perf skip；冷启动 18 作业运行键 `5ad1c0f0ccaf…`，351.53 秒。C6 合入后慢阶段打开岛 2 / 3，快阶段实际为 14 个；不是工作单引用的旧 15 个 |
| C5 T07 与次日规划重测 | PASS / DEVIATION | 岛 2 / 3 实测 0 / 2 天，工作单要求的旧值是 1 / 4 天；其余解锁日不变。次日规划坏区间 17→15，AfterPass 6→5，三个接受记录分开；默认 37/41 精确、七个逐日区间精确、四个坏区间不变。原始测量见 `out/postmerge/fasttick-measurements.json` |
| X6 pending 奖励 | PASS | 强制天快照要求 `fast.stat.* == 0`，按 C# 的 binary64 编码读取；不再在两侧同时加 pending。六个植错组合覆盖 0、1、1e-300 和有 / 无 clears；`tests/test_crosscheck.py` 与全仓 pytest 通过 |
| P12 supplied driver readiness DB | PASS | `compare._simulate` 将配置 DB 传给已有 BatchBackend / ServeBackend；serve_submit 将其传至旁路表写入。两个后端的无环境变量回归用例通过；diff_e2e 3 passed |
| StaleTickCache 消融说明及 C4 补跑 | 说明 PASS，C4 FAIL | Runbook 16.2a 记录到期 Tick 的战斗后缓存也成为消融模式换天时复用的缓存；C4 模块实际重跑 13 passed、5 failed，见下面阻塞项 |
| C3c 参数集成修正 | PASS | manifest 按声明的 float32 / binary64 位比较，规范 hash 不变；支持 Unity `0400000008000000i` 的小端 int32 数组及原 YAML 列表。fresh import 的 10 项检查全部 true，退出 0、恢复运行资产，6.86 秒；Unity params 15 passed；静态参数组 59 passed / 40.08 秒 |
| P13 refresh | PASS | `python -m tufu_balance.refresh --level 1 --rev 92e5cb1 --out out/postmerge/refresh-final`：13/13 步退出 0，121.87 秒，CURRENT 未变。报告 `out/postmerge/refresh-final/summary.md` |
| 完整 Unity 集成 | FAIL | `TUFU_UNITY_REV=92e5cb1 python -m pytest -q --unity tests/unity --junitxml=out/postmerge/unity-final.xml --durations=25`：272 passed、15 failed、3 skipped、0 errors，602.44 秒；下面逐模块列出 |
| digest-enabled golden-405，实际完整策略与烘焙 | PASS | `python -m tufu_balance.unity run --request out/postmerge/golden405-request.json --rev 92e5cb1`：退出 0，27.8 秒；运行键 `2b34297cffdaf77065b213c278775e9c38ee97f6e0bfa4768fb3f105d8d20b16`。随后 ingest 与 X1–X7 共 349103 项，0 mismatch，14.10 秒；`out/postmerge/golden405-crosscheck.json` |
| C6 inventory-free 与慢 Tick 发物品后的 X2 | PASS | `test_inventory_free_crosscheck_includes_slow_item_grants`：127 次正慢阶段物品发放；X2 6340 项，含 56 个非强制慢快照，0 mismatch。X1 / X3 / X5 / X6 / X7 也通过；X4 没有 bake report，明确 vacuous。该批冷启动 inventory 模块耗时 206.24 秒，最终复用运行与重算耗时见下表 |
| 全仓 Python | PASS | `python -m pytest -q --junitxml=out/postmerge/pytest-delivery.xml --durations=15`：968 passed、297 skipped、24 warnings，231.30 秒；跳过含未启用 Unity、忙机器 perf 和 LFS 条件检查 |
| W0 fixture | PASS | `python tests/golden/fixtures/generate.py --check`：`identical`，`out/postmerge/fixture-final.log` |
| cscheck | PASS | `python -m tufu_balance.cscheck --worktree /Users/joyfort/MyProjects/Tufu-s5-Postmerge`：4.4 秒；两程序集 0 errors，原有 warnings 3707 / 8；无编辑器编译 0 errors / 0 新基线外错误；签名 0 violations、禁用 API 0 uses |
| golden guard | PASS | `python -m tufu_balance.golden guard`：20 suites、0 violations。只接受下面列出的八个键 |

完整策略 golden-405 的逐项计数：X1 55920、X2 6340、X3 12180、X4 717、X5 405、X6 261361、X7 12180，全部 0 mismatch。X2 `forecast_order` 没有区分样本；X3 / X7 的 missing_health、running_max、armor_tie_selection 仍明确列为未覆盖，不能将通过数解释为这些分支的验证。

## 剩余阻塞与交接

### C7：可杀门禁不总是被选中

`Balance/tests/unity/test_slow_policy.py:224`，`slow-bydist` 的 `slow_boss_allowed_not_chosen = 1`，预期 0。记录点为 `SimulationSystem.Targeting.cs:226`，选择比较在同文件 `RebuildSelectBest`（169–186 行）。

运行 `247ba38abd4bb5ff03f67502…/slow-bydist/events.jsonl`：第 2 天，P=125.751366，会话 12，岛 2 门禁 loss=4289.373617650462、max_health=4760.248596593738、current_health=4397.545514702797，两个拒绝谓词都是 false，skipped=false，chosen=false。下一目标是岛 1 的 `Spawner_common_cut`，priority=126445.349354054、耗时 0.39542775005506287 秒。数字奖励 1e6 本身不保证除以成本后的门禁优先级最高；C6 新的物品与战斗状态让原来的验收命题不再自动成立。

**交 C7 / K2 决定**：门禁必须强制优先，还是按现有优先级规则比较。此处没有添加新的强制优先规则，也没有接受含该事件的 counters 黄金。

### C7：岛 3 致死门槛场景不再覆盖目标

`Balance/tests/unity/test_slow_policy.py:352` 要求 gate-same-as-fast 的岛 3 门禁在慢阶段通过 bot filter，但实际相关事件为 0，另两种 gate 也没有覆盖。旧场景依赖固定 GATE_GRANT 和 TC3 到达 P≥240；C6 合入后第 3 天事件的最大 P 分别为 236.146759 / 236.146759 / 236.777939；最后一个 Tick 后进入第 4 天，不能据此声称活跃慢 Tick 已检查该门禁。

**交 C7 / K2** 重建有独立状态依据的场景；不能仅放宽 `assert gates` 或调整默认策略凑覆盖。

### C4 / C6：空战斗策略不能隔离定时进化

`SimulationSystem.Inventory.cs:137` 即使没有正奖励也调用 ProcessInventory；同文件 168–181 行执行到期进化。C4 的 null_policy 只禁用战斗 / 快阶段规划，没有冻结物品模块。

可复核实例：运行 `86dd0354f8f28c0694116a302662925d7869d26bf33dc9b1d2f6c980bd43e98a/replay-405` 的第 18 天 override 把 pike 设为 `(raw Level=1, Copies=101, Evolved=false, effective Level=101)`；同日有明确 `evolve` 事件，花掉 100 份；第 19 天天快照是 `Level=1, Copies=1`。这与 C6 的无新奖励也检查到期进化一致，却违反旧 C4 测试的“不变物品”假设。

受阻硬断言：`test_state_override.py:409`、`:490`、`:614`（replay 下一强制快照、Evolved flip 下一快照、checkpoint 区间保持物品不变）。另外 `:304` / `:425` 的既有状态和 RPM 硬检查已通过，但两个诊断黄金仍不相等：`replay405.rpm_cells_equal_fresh` 14472→14464，`replay405.day_change_gpm_carried` 750→730。

**交 C4 / C6**：明确隔离物品模块的探针，或用独立进化转移预言机改写空策略测试；保留真正 checkpoint 的闭环行为。此次没有改变运行期进化或接受 sim_override 黄金。另一个明确陈旧的 P12 槽位断言已修复，`test_unlock_snapshot_opens_islands_slots_and_boss` 通过。

### 未接受的 C7 诊断漂移

另有八个 sim_slow 黄金键变动：三个 `unlock_days.slow-default / slow-nosmart / slow-current-health` 从 `{2:1}` 变为 `{2:0,3:2}`；`deaths.slow-none` 55→43；`deaths.slow-clear-then-respawn` 41→11；`deaths.by_gate` 中 slow-none 55→43；`smart_path.en_route_clears` 175→311；`counters` 多个字段变化。因同套件仍有上述硬失败，全部保留当前基线，交 C7 / K2 审查。

未接受测量已完整保存在 `out/postmerge/unaccepted-sim_slow-latest.json`、`unaccepted-sim_override-latest.json`；15 个最终失败的完整文本在 `out/postmerge/blockers.json`。这些是本单元未完成的验收，不是“用户编辑器在用”例外。

## Unity 逐模块结果

最终运行的耗时含 Python 校验与可复用的成功运行；determinism 的强制重启、selftest、失败型 harness 和必要的慢策略重跑仍实际执行。初次完整运行是 208 passed、19 failed、4 skipped、59 errors，1946.14 秒。那 59 个 setup errors 来自慢策略单次 launcher 守卫拒绝，最终重跑已全部消除。

| 模块 | passed | failed | skipped | 秒 |
|---|---:|---:|---:|---:|
| test_bake_tool | 6 | 0 | 0 | 1.00 |
| test_determinism | 4 | 0 | 0 | 57.35 |
| test_diff_e2e | 3 | 0 | 0 | 64.59 |
| test_differential | 14 | 0 | 0 | 2.47 |
| test_engine | 21 | 0 | 1 | 58.97 |
| test_export_roundtrip | 7 | 0 | 0 | 0.51 |
| test_fasttick | 18 | 0 | 1 | 14.51 |
| test_headless_smoke | 6 | 0 | 0 | 65.60 |
| test_inventory | 14 | 0 | 0 | 39.61 |
| test_params | 15 | 0 | 0 | 49.97 |
| test_patcher_parity | 73 | 0 | 0 | 12.63 |
| test_run_crosscheck | 6 | 0 | 0 | 6.91 |
| test_sandbox | 19 | 0 | 0 | 100.49 |
| test_slow_policy | 51 | 10 | 1 | 99.00 |
| test_state_override | 13 | 5 | 0 | 12.77 |
| test_x2_cache_basis | 2 | 0 | 0 | 15.01 |

初次慢策略运行的 15 个 Unity 作业全部 exit 0，但 real save / PlayerPrefs 有变化，且 main editor 的前后交集为空，所以 launcher 正确以 2 拒绝；报告 `out/postmerge/first-slow-guard.json`，原备份保留为 `first-slow-real-backup/`。最终同批重跑 launcher / Unity 都是 0，guard_problems=[]。没有绕过守卫、没有操作用户编辑器，没有使用“not run: user editor active”豁免。

## 每个已接受黄金键

全部经 P1 `golden accept`，未改 ORIGINAL；exact / set 的定义变化按工具要求登记 `--loosen`，不是放宽数值容差。

| 套件 / 键 | CURRENT 旧→新 | 接受记录 |
|---|---|---|
| sim_fasttick / t07.unlock_days | 岛 2：4→0，岛 3：4→2，其余不变 | accepted/sim_fasttick.jsonl 第 3 行；理由明确记录 C7 慢阶段、随包 TC0、T07 slack 1，以及 C6 使工作单旧值 1/4 失效 |
| sim_fasttick / t05.next_day_bad | 17→15 | 同文件第 4 行，独立的次日规划重测记录 |
| sim_fasttick / t05.next_day_bad_spans | 删除 4→5、6→7，其余 15 个区间不变 | 同文件第 4 行 |
| sim_fasttick / t05.afterpass_bad | 6→5 | 同文件第 5 行，独立 AfterPass 诊断记录 |
| bake_cs / run.export_text.differ | 3→0 | accepted/bake_cs.jsonl 第 4 行，C1 exact-decimal formatter `b78aa49` 消除文本缺陷 |
| bake_cs / run.export_text.ulps | {"-1":3}→{} | 同文件第 4 行 |
| crosscheck / flat405.text_repairs | 1→0 | accepted/crosscheck.jsonl 第 4 行；完整双文件位摘要直接通过 |
| crosscheck / csv_files | 去掉不再需要的 export_text.csv，保留九张 X / DP8 CSV | 同文件第 4 行 |

记录文件均在 `Balance/tests/golden/` 下；每条新记录的 git_rev 为测量时 HEAD `92e5cb1`。完整旧 / 新 expectation 与记录位于 `out/postmerge/golden-changes.json`。

最终 T07 字典为 `{2:0,3:2,4:5,5:8,6:11,7:15,8:21,9:26,10:35,11:44,12:57,13:71,14:90,15:110,16:131,17:151}`。偏离工作单指定的 1 / 4，是为了忠实记录已合入 C6 的实际运行，不是改策略。岛 3 相对随包第 1 天的剩余误差继续交 K2。T04 的旧设计失败 4.29264% / 8.58559% 及既有放宽登记均未改变。

## 附加修正、所有权与守卫

- C3d：C6 使 day-6 场景需要 559 ticks，speed 500 为两帧；断言改成 ceil(ticks/speed)，保留跨速度全部输出与摘要逐字节一致。
- C1：空作业导出文件集合包含新增 slow_tick_basis.csv，并要求该文件 0 行。
- C5 / C6：累积测试要求正物品奖励；401 个快日均存在正物品累积。普通 / 参考冲刷继续相同。
- C3c / C6：支持 packed int[]，manifest 按声明宽度比较，fresh import 保留实际保存文本逐字节检查；默认 asset_text API 仍输出仓库原有可读形式。
- P13：首次 refresh（78.76 秒）在 smoke-11 因 80 字符的 TMPDIR 加 Bee IPC 后缀超过 macOS 104 字符上限而失败；报错来源是 Bee 套接字初始化，并非 C# 源码诊断。改用本工作树 Balance/out 下短的唯一 rt-* 临时目录，保留私有 DB 和输出目录；静态 refresh 组 27 passed / 3.57 秒，实际 L1 已重跑通过。
- 上述跨 C5/C6/C7、C3c/C3d、C1/C8、P10/P11/P12/P13 的所有权修正由 Postmerge 工作单的“小且明确的集成修正”授权；未修改 Design.md，交 Z1 汇总。

共享 DB 最终 SHA256 `a7a2e3c5d048c57f9a77d3057dd0c35ed30cf799fe6612529961c44a18e3e098`，mode 444。启动器最终 status：运行工作树 clean=true、lock=null、unity_processes=[]、serve.running=false。工作单元最终提交后 git status 为空；没有 push、切换其它开发分支或删除工作树。
