# 模拟输出的导出与导入（C1）

C1 单元的说明：C# 导出器、Python 导入、旁路文件的格式、刷怪点身份映射，以及就绪核对接上 P6 的期望表。设计见 [Design.md](Design.md) 1.4.3、1.5 第 5 条、3.4、7.3 C1；无头运行见 [Runbook.md](Runbook.md)。

## 1. 文件

每个作业目录 `<out_dir>/<job_id>/` 里，C1 写下面这些文件。开关在请求的 `job.outputs`（布尔值），默认值以 `SimulationExport.OutputDefaults` 为准；`empty` 作业只写明确打开的：

| 文件 | 开关（默认）| 内容 |
|---|---|---|
| `simulation_data.json` | `simulation_data`（开）| 与 `SimulationData` 同构：只有 `Snapshots`、`TimeChunkSnapshots` 两个字段，字段顺序同 C# 声明。`TimeChunkSnapshots` 按 `GetTrimmedTimeChunkSnapshots` 裁剪；它还返回 `null`（C3b 之前）时不裁剪，manifest 的 `export.time_chunk_trim` 写明 |
| `timechunk_full.json` | `timechunk_full`（开）| 不裁剪的天快照，只有 `TimeChunkSnapshots` 一个字段 |
| `snapshots_ext.csv` | `snapshots_ext`（开，随 `simulation_data`）| 完整列表里每个快照一行：`list,index,exported_index,time_chunk_index,playtime,total_playtime,location,unlocked_weapon_slots,unlocked_armor_slots,current_health`。`exported_index` 是它在 `simulation_data.json` 里的序号（被裁掉为空）；后三列由引擎记录，没有记录时为空 |
| `items_ext.csv` | `items_ext`（开）| 每个快照的每件物品：`list,index,item_index,key,level,copies,evolved,evolution_level`。前几列取自快照的 `Items`，`evolved`、`evolution_level` 来自引擎的记录，没有时为空 |
| `shipped/simulation_data.json` | `shipped_simulation_data`（关）| 随包 `Simulation Data` 资产走同一个导出器，往返测试的参照 |
| `shipped/snapshots_ext.csv`、`shipped/items_ext.csv` | 同上 | 探针：用真正的写出函数，对随包的两个列表写旁路文件。`TimeChunkSnapshots` 按"位置 i % 3 != 1 的保留"裁剪后算 `exported_index`；引擎记录用一套合成规则（`Snapshots[i]` 在 i % 4 != 3 时、`TimeChunkSnapshots[i]` 在 i % 5 != 2 时记录；武器槽 i % 6、护甲槽取 `Location`、血量 `TotalPlaytime / 3.0`、第 k 件物品 `evolved = (i + k) % 2 == 0`、`evolution_level = Level * 10 + Copies`；`Snapshots[1]` 故意少记一件物品，进化列应为空并在探针的 `problems` 里写明）。规则写在 `SimulationExport.WriteShippedProbe`，测试按同一规则从 `balance.sqlite` 独立算出每一行比较。这样在 C3b 产出快照之前，每一列、`ExportedIndices`（含"不是子序列时返回 null"）都已经在真实数据上跑过 |

manifest 里加一个 `export` 对象（只在导出了文件时出现，所以 selftest 的 manifest 不变）：`schema`（`tufu.sim.export/1`）、`files`（每个文件的 `sha256`、`bytes`；快照文件另有 `doubles`、`floats`、`nans`、`bits_sha256`，CSV 有 `rows`）、`time_chunk_trim`、`trim_simulation_data`、`snapshots`、`time_chunk_snapshots_full`、`recorded_snapshot_ext`、`problems`，以及 `shipped_twice_identical`、`shipped_asset`、`shipped_probe`（`trim_rule`、`record_rule`、`trimmed`、`recorded`、`not_subsequence_is_null`、`problems`）。

**给 C3b 的接口**：`SimulationSnapshot` 里没有的状态由 `MakeSnapshot` 在追加快照后调用 `SimulationExport.RecordSnapshot(timeChunkList, index, ext)` 记下（`index` 是在 `config.Snapshots` 或 `config.TimeChunkSnapshots` 里的位置，`ext.Items` 与快照的 `Items` 同序）；开始一次新的运行、新建列表时调用 `SimulationExport.ResetRecorder()`。作为兜底，`SimulationHarness.StartJob` 在每个作业开始时也调用一次，所以上一个作业的记录不会带进下一个（2.6；`in_session` 模式下静态字段不随域重载清空）。记录的物品与快照的 `Items` 对不上时，该快照的进化列留空，`export.problems` 写明。

## 2. 数值格式

- double：先用 `"R"`，再把文本解析回来比较 IEEE 位，不同就用 `"G17"`，仍不同就抛异常。实测编辑器 Mono 的 `"R"` 是 .NET Framework 的做法（先 15 位，不能往返时 17 位），不是最短表示：随包数据的 20650 个有限 double 里有 1396 个写成了 17 位。
- float：`"G9"`，同样解析回来核对。
- `-0` 单独处理；NaN、Infinity 写成裸符号。
- JSON 里有限的整数值 double/float 一律带 `.0`（`SimulationJson.JsonDouble/JsonFloat`），读的一侧不会把它当成整数。CSV 仍用 `FormatDouble/FormatFloat`，不加 `.0`，所以 `ready_spawners.csv` 与 C0b 的逐字节相同。这也修好了 C0b 遗留的问题：manifest 回显请求时 `259200.0` 曾变成 `259200`。
- **逐位核对**：导出时按文档顺序把每个 float（`f:<8 位十六进制>`）和 double（`d:<16 位十六进制>`，NaN 写 `nan`）各写一行，取 sha256 记进 `export.files.<文件>.bits_sha256`。导入时 Python 从解析出的文本重算（float 取最近的 float32，与 `float.Parse` 一致），不同就报错。所以文本格式不可能悄悄改变任何一个值。

## 3. 导入（`tufu_balance.unity.simio`）

```bash
python -m tufu_balance.unity ingest <job_dir> [--db PATH]     # → <job_dir>/sim.sqlite
```

- 先按 `schema.py` 校验 manifest：未知的顶层键、`export` 键、`export.files` 的文件名和键、`export.shipped_probe` 的键都报错；`.json` 条目必须有 `doubles`、`floats`、`nans`、`bits_sha256`，`.csv` 条目必须有 `rows`。**严格只到这一层**：C0b 写的嵌套对象（`ready`、`sandbox`、`ab`、`unity`、`counts`、`selfcheck` 等）只检查是对象，里面的字段不校验，它们的内容由 C0b 的测试负责。
- `export.files` 列出的每个文件都要存在，sha256 与字节数相同，CSV 的数据行数等于 `rows`；目录里有 C1 的文件（`EXPORT_FILES`）而 manifest 没列出，也报错。快照文件再按逐位摘要核对，缺摘要的键不会跳过而是报错。
- `simulation_data.json` 先变成"抽取器从 Unity 的 YAML 里读到的样子"，再走 `extract.add_record_tables` 和同一个枚举解码器（`Key__name`、`WeaponType__name`），用抽取器的 `cell`、`sql_type` 和列名规则写表。"YAML 的样子"指：
  - float32 字段（`Playtime`、`TotalPlaytime`）取它最短往返十进制文本对应的 double，这正是 Unity 写进 YAML、抽取器读出的值；
  - 有限的整数值（|x| < 1e15）变成整数，Unity 把 100.0 写成 `100`；
  - NaN 存成文本 `nan`，与抽取器相同。
- 表：`c_SimulationData`、`c_SimulationData__Snapshots`、`…__Stats`、`…__Items`、`…__DpsData`、`…__DefenseData` 及 `TimeChunkSnapshots` 的同名表，`asset`/`asset_name` 默认取随包资产的（`MonoBehaviour/Simulation Data.asset`、`Simulation Data`），所以按名字选资产的现有工具读模拟结果不用改；`timechunk_full.json` 进 `c_SimulationDataFull*`；旁路文件进 `sim_snapshots_ext`、`sim_items_ext`、`sim_ready_spawners`、`sim_daily`、`sim_digests`、`sim_events`（公共列加 `data` JSON）、`sim_manifest`。
- 与抽取器一样，**空列表**在父表里是一个值为 `[]` 的列。抽取器此时不建子表；导入则把缺的子表按抽取器的列名（`simio.snapshot_table_layout`，由 `STRUCT_FIELDS` 推出）建成空表，所以任一列表为空（`Snapshots`、`TimeChunkSnapshots`，或所有快照的 `Items` 都为空）时 `load_simulation_data` 读出空列表，而不是因为缺表失败。空表的列没有数据，声明类型一律是 `TEXT`。
- `-0.0` 存成整数 `0`：与抽取器一致（Unity 的 YAML 写 `-0`，YAML 读成整数 0）。逐位摘要按 JSON 文本核对，`-0.0` 的符号在那里是保留的；只有落表时丢掉。随包数据里没有 `-0.0`。
- `simio.compare_tables(a, b)` 逐表、逐行（rowid 顺序）比较：列名与声明类型、行数、每个值的存储类型都要相同；REAL 逐位相同，float32 字段按 `f32` 比较（另计逐位相同的个数）；NaN 位置相同。每一处不同是 `differences` 里带 `problem` 的一条。它本身有故障注入测试（`tests/test_simio.py::test_compare_tables_reports_each_planted_fault`）：在随包表的副本里分别埋下差 1 ulp 的 double、差 1 ulp 的 float32、改动的 `Key__name` 与 `asset_name`、挪了位置的 NaN、少一行，以及无类型列里同值不同存储类型，每一种都要恰好报出对应的那一条。

## 4. 旁路文件的格式（`tufu_balance.unity.schema`）

- CSV：表头必须与登记的列完全一致（含顺序）；类型 `int`、`float`（只接受数字文本和 `NaN`/`Infinity`/`-Infinity`）、`str`、`bool01`、`sha256`，带 `?` 的可以为空。已登记：`snapshots_ext.csv`、`items_ext.csv`、`ready_spawners.csv`（C0b 的列）、`daily.csv`、`digests.csv`。
- `events.jsonl`：公共字段 `type,day,playtime,total_playtime`（`playtime` 必须是 JSON 浮点），类型 `first_kill`（`spawner` 为"路径|Key"、`location`）、`unlock`（`location`）、`slot_unlock`（`slot`、`count`）、`evolve`（`item`、`level`、`copies`）、`death`（`spawner`、`location`）。未知类型、未知字段都报错。
- `daily.csv` 的列：`day,time_chunk_index,playtime,total_playtime,active_minutes,offline_minutes,kills,clears,fight_time_sum,farmed_time_sum,eligible_count,eligible_signature,location,unlocked_location`；`digests.csv`：`day,time_chunk_index,playtime,total_playtime,sha256`。这两个文件和 `events.jsonl` 由 C3c 写；需要改动时由 C3c 用 `register_csv_columns`、`register_event` 追加（只增不改）。其它单元往 manifest 加顶层键用 `register_manifest_keys`，加输出开关用 `register_output`。
- 请求的 `job.outputs` 只接受登记过的名字（`request.validate` 调 `schema.validate_outputs`）。C# 一侧没有校验钩子（`Validate` 只给 C4/C8/C9 留了 partial 方法），未知开关在 C# 里会被忽略。

## 5. 刷怪点身份映射（`tufu_balance.unity.spawner_map`）

```bash
python -m tufu_balance.unity spawner-map [--out CSV]     # scene,file_id,path,key,active,enabled,registers
```

- C# 的身份是 `SimulationHarness.HierarchyPath` 加 Key：`<场景名>:<层级路径>|<Key>`。映射由 `spawners` 表生成：场景文件名去掉扩展名，加 `:` 和 `go_path`。
- "路径|Key"在 717 行里唯一（Key 本身有 716 个）。
- 运行时注册在 `EnemySpawner.Start`（`EnemySpawner.cs:513-518`），只有启用的组件、在层级里激活的对象才执行，所以预测的注册集合是 `active && enabled`：716 个。
- 每次运行的 `ready_spawners.csv` 与预测逐个比较：716 个全部对上，没有未知的、没有重复的。差集只有一行：`(Scenes/Main.unity, 57694)`，即 `Main:Events/Spawners (1)/Eventspawner_Boss|Eventspawner_Boss_79e49ca7…`（未激活的重复 Key）。

## 6. 就绪核对接上 P6（C0b 遗留）

- 请求的 `sandbox.expected_ready_spawners` 默认为 `"auto"`。启动器在算 `run_key` 之前把它换成 `ready_spawners.expected.<内容 sha256 前 16 位>.csv`，内容是 P6 的 `expected_ready_spawners(sandbox.ab_variant)` 经映射写成的表（列 `path,key,charges_total,base_cooldown,scene,file_id`），写在 `request.json` 旁边。文件名带内容哈希，所以表一变 `run_key` 就变。
- 数据库取 `$TUFU_BALANCE_DB`，没有时取 `Balance/out/balance.sqlite`；都没有时启动器以退出码 5 拒绝（要关掉核对就显式传 `null`）。
- 实测：A 组 716 个刷怪点的 `ChargesTotal`、`BaseCooldown` 全部相等，`mismatched = 0`；不符时退出码 8（C0b 已有，`test_expected_ready_spawner_table_gate`）。

## 7. 验收记录

| 项（设计稿 7.3 C1）| 结果 | 证据 |
|---|---|---|
| 随包资产经 C# 导出、`simio` 导入后 `c_SimulationData__*` 与 `balance.sqlite` 相同 | 通过：11 张表、59210 个值；double 逐位；218 个 float32 值按 `f32` 相同，并且全部逐位相同；60 个 NaN 位置相同；4360 个 `__name` 相同 | `tests/unity/test_export_roundtrip.py::test_shipped_data_round_trips_through_the_csharp_exporter` |
| 编辑器内存里的每个 float/double 与资产 YAML 的值逐位相同 | 通过：20710 个 double、218 个 float、60 个 NaN，摘要相同 | 同上 |
| `load_simulation_data(connect(sim.sqlite))` 与从 `balance.sqlite` 读出的相等 | 通过（`repr` 逐字相同，NaN 也算）| 同上；不经 Unity 的同一检查：`tests/test_simio.py` |
| 旁路文件按 `schema.py` 校验，未知字段报错 | 通过；C# 写出的旁路文件在随包数据上逐行核对（探针）| `tests/test_simio.py` 的 schema 测试、`test_job_outputs_ingest_and_validate`、`test_shipped_side_files_follow_the_probe` |
| 同一数据导出两次逐字节相同 | 通过：同一 Play 里两次相同，两个作业（两次 Play）的全部导出文件相同 | `test_export_is_byte_identical_twice` |
| 刷怪点映射覆盖全部运行时注册的刷怪点，差集单列 | 通过：716/716，差集 1 个 | `test_runtime_spawners_match_the_identity_map`、`tests/test_simio.py::test_spawner_identity_map` |
| C0b 遗留：就绪核对用 P6 的表 | 通过：716 个比较、0 个不符 | `test_readiness_gate_uses_p6s_table`、`test_sandbox.py::test_ab_groups_fixed_to_the_request` |
| C0b 遗留：manifest 回显保持 int/float | 通过，按类型严格比较 | `test_sandbox.py::test_request_round_trip_through_csharp` |

黄金套件 `export`（`tests/golden/current/export.json`）冻结上面的计数、随包数据的逐位摘要与导出文件的 sha256、映射与差集、就绪核对的计数。

## 8. 与设计稿不同的地方

- **没有建 `ED/SimulationExportRoundTrip.cs`**。往返改在 Play 里经 `SimulationHarness` 执行（输出开关 `shipped_simulation_data`），这样它走启动器的锁、沙箱、资产守卫和心跳，用的也正是模拟输出所用的导出器；编辑模式下单独的入口启动器调不了（入口类固定为 `SimulationBatch`）。
- **没有建 `SimulationRunRequest.C1.cs`**：`job.outputs` 本来就是字典，不需要新字段。
- 新增文件 `tufu_balance/unity/spawner_map.py`（映射与期望表），设计稿没有列出映射放在哪里。
- 1.4.3 说 double 用 `"R"`：Mono 的 `"R"` 不保证往返，也不是最短，所以加了解析核对与 `G17` 后备，并在 manifest 里加了逐位摘要。
- 1.4.3 的 `snapshots_ext.csv`/`items_ext.csv` 只列了内容；这里定了列名，并加了 `exported_index`、`time_chunk_index`、`playtime` 等连接用的列。`daily.csv`、`events.jsonl`、`digests.csv` 的列由本单元先定，C3c 可以追加。
- `rebuild_coverage.json` 不在本单元范围内（没有写）。
- 改了 C0b 的文件（7.4 的交接，由 Z1 记进设计稿）：`launcher.py`、`request.py`（`"auto"` 就绪表，C0b 遗留 a）、`SimulationJson.cs`（JSON 数值带 `.0`，遗留 b）、`SimulationHarness.cs`（manifest 的 `expected_table.path` 按请求写的名字记录；`StartJob` 里调 `SimulationExport.ResetRecorder()`）、`__main__.py`（`ingest`、`spawner-map` 两个命令）、`tests/test_unity_launcher.py`（假运行不受主检出上打开的编辑器影响）、`tests/unity/test_sandbox.py`（`test_ab_groups_fixed_to_the_request` 现在期望 `expected_table` 为 checked、716 个比较、0 个不符；`test_request_round_trip_through_csharp` 按类型严格比较）、Runbook 第 10、14 节。
- `selftest` 现在也需要抽取好的数据库（见 Runbook 第 10 节）。
