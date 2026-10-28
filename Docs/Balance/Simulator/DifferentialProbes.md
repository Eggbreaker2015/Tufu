# 差分探针（C2）

C2 单元的说明：用 Unity 里实际运行的游戏代码，在 Python 生成的输入网格上算一遍共享公式，与预言机逐位比较（设计稿 3.4 节 DP1–DP7）。运行方式见 [Runbook.md](Runbook.md) 第 10 节。

## 1. 怎么跑

```bash
python -m tufu_balance.unity.probes            # 核对入库的输入网格能否由种子重放（输出 identical）
python -m tufu_balance.unity.probes --write    # 重新生成（数据库变了以后）
pytest --unity tests/unity/test_differential.py
```

- 输入网格由 `tufu_balance.unity.probes.generate_inputs` 按固定种子（`SEED = 20261028`，每个 DP 一条独立的随机流，只用 `random()`）从数据库生成，入库为 `Balance/tests/data/probes_in.json`。`tests/test_probes.py::test_inputs_replay_from_the_seed` 断言重放结果与入库文件逐字节相同。
- 探针作业是一个 `empty` 作业加 `probes` 字段（`probes.probe_job()`）：`{"input": "Balance/tests/data/probes_in.json", "input_sha256": …, "edit": [DP1…DP5], "runtime": [DP3, DP6, DP7]}`。`input` 相对仓库根目录，C# 在运行工作树（检出的就是被测提交）里读它，并核对 sha256，不符就失败。
- 编辑模式的探针由 `SimulationPlayModeDriver.StartJob` 在该作业进入 Play **之前**调用 `ED/SimulationProbes.cs`（此时资产是磁盘上的状态），结果写 `<作业>/probes_edit.json`；DP5 以 additive 方式打开 Main，算完关掉。Play 模式的探针由 `SimulationHarness.FinishJob` 经新的 partial 钩子 `RebuildJobRunProbes` 调用 `SimulationRuntimeProbes`，结果写 `<作业>/probes_runtime.json`，manifest 加 `probes` 对象（输入与两个输出文件的 sha256）；各探针的耗时写进 `timings.json`，manifest 仍可逐字节比较。
- 数值一律以 IEEE 位的十六进制跨边界（double 16 位、float 8 位、int 8 位，同一网格拼成一个字符串），与数字格式无关。NaN 之间不比较载荷。
- `tests/test_probes.py::test_comparisons_report_each_planted_fault`（`slow`，约 6 秒）用预言机自己生成一份"假 C# 输出"，确认每个比较函数报 0 个差异；再在 DP1、DP2、DP3、DP4、DP7 里各埋一个差 1 位的值（以及一个改动的来源），确认恰好报出那一处。

| 探针 | 模式 | 输入 | 比较 |
|---|---|---|---|
| DP1 | 编辑 | 36 个 `StatType` × 217 个点（对数网格 1e-4..1e8 共 200 点，加 0、−0、−1、1、最小次正规数、NaN、±Inf、100/700/10000 及其相邻 double）| `CalculateSpecificValue` 与 `ClampSpecificValue` 逐位 |
| DP2 | 编辑 | `ApplyDamageDefense` 1023 对；`SolveAttackDamageForIncomingDamage` 404 组（百分比 ≥ 0.05，否则 C# 会空转 2^31 次）；`GetBattleRegen` 302 组；`WeaponTypeToDefence` 类型 −1..6 | 逐位；越界类型两边都抛异常 |
| DP3 | 编辑与 Play | t = 0..800 的整数、1000 个小数点，另加 7 个特殊值（−1、−0、800.5、1e6、NaN、±Inf，单列）| 19 条曲线各自的 `Evaluate`，`GetInterpolationCoefficient` 的类型 0..8 及类型 × 稀有度；对四种舍入各出一张 ulp 直方图 |
| DP4 | 编辑 | `GetByTimeChunkIndex` 816 点（−2..410 的整数、400 个小数、NaN、±Inf）；早期列表 76 点；`GetByPlayTime` 424 点（全部快照的 Playtime、相邻两点的中点即平局、300 个随机点）| 返回快照的全部字段逐位，外加"返回的是哪个存储元素"（引用相同）以及插值快照的 Items 来自哪一侧 |
| DP5 | 编辑（打开 Main）| 场景里全部 717 个 `EnemySpawner`（含未注册的 1 个）| NewHealth/NewDamage 逐位，中间量相对 1e-12；用到的快照是哪个存储元素 |
| DP6 | Play | 运行时注册的 716 个刷怪点；AB 服务的岛 1–20 表 | `ChargesTotal`、`BaseCooldown`（位）、`GetSpawnerChargesLimit`（反射调用私有方法）、`CanUseSpawnerCooldownsAb` |
| DP7 | Play | 1952 个 (物品, Level, Copies, Evolved)：G18 每次跳变的前后两态，加每个物品配置 7 个随机样本 | `Damage`、`Armor`、`DPS`、`LevelsToEvolve`；缺表行时两边都抛异常 |

## 2. 结果（Unity 6000.6.3f1，编辑器 Mono，arm64）

| 项 | 结果 |
|---|---|
| DP1 | 7812/7812 逐位相等 |
| DP2 | 1023、404、302、8 全部逐位相等 |
| DP3 | **编辑器是 `double_all`**：19 条曲线 34219 个值、系数 129672 个值全部逐位相等，特殊值也相等；Play 与编辑模式的输出完全相同。其它三种的直方图见下 |
| DP3 锚点 | 用 C# 的 K2/K3 核对 109 个随包快照：K2 109/109、K3 109/109（写进原版基线）。所以不需要按 Q03 改游戏代码 |
| DP4 | `tc_early` 76/76、`GetByPlayTime` 424/424（含相邻快照的 60 个中点）、`tc` 796/816；20 个不同全是插值快照的 TotalPlaytime（±1 ulp：+1 有 15 个，−1 有 5 个），见第 3 节 |
| DP4 锚点 | C# 在 46 个存储日上查到的 TimeChunkIndex 序列等于 G19 的裁剪规则序列，Location 也相同 |
| DP5 | 717 个刷怪点 NewHealth/NewDamage 全部逐位相等（按 DP3 测出的 `double_all` 算 K4/K5）；中间量 20076 个里 20072 个逐位相等，另 4 个 NewHealthRaw 相对差 3.7e-8（第 3 节）；快照来源 717/717 相同；没有任何一方抛异常 |
| DP5 BossTower | `Scenes/Main.unity:56955`，Count 0：两边的 NewHealthRaw 与 NewHealth 都是 +Inf，单列 |
| DP5 锚点 | C# 报告上分钟制 HP 精确的刷怪点 79/108，集合与 G13 的 79 个相同 |
| DP6 | 716/716 全部相等，没有未知、缺失或重复 |
| DP6 锚点 | 物品刷怪点 127 个，充能按稀有度 C 8（39）+ 0（1）、U 6（30）、R 4（24）、E 2（21）、L 1（12）；非精英冷却岛 1–13 为 4, 6, …, 28 分钟，精英岛 1–4 为 60/90/120/150 分钟，之后默认 30/180；每单位充能 3/2；`fastRPMParams` (10, 1)；每个用 AB 表的刷怪点的 `BaseCooldown / 60` 与 AB 服务的表一致 |
| DP7 | 7808 个值里 7672 个相等（含 618 个两边都抛异常）；136 个不同（Damage 61、DPS 61、Armor 14），全部是第 3 节的原因 |
| DP7 锚点 | 用 C# 的值重算 G18：675 个跳变，最小 1.022、中位 1.235、最大 8.4（paddle 第 1 级 2500 → 21000）|

DP3 的 ulp 直方图（编辑器减预言机，单位是 binary32 的 ulp，1801 个主点）：

| 舍入 | 曲线（19 × 1801）| 系数（类型 0..8，加类型 × 稀有度）|
|---|---|---|
| `stepwise` | 0: 33684；−1: 293；+1: 229；−2: 13 | 0: 126858；−1: 1600；+1: 1119；+2: 55；−2: 40 |
| `fma` | 0: 34097；−1: 106；−2: 13；+1: 3 | 0: 129184；−1: 488 |
| `double_tail` | 0: 34152；−1: 51；−2: 13；+1: 3 | 0: 129624；−1: 48 |
| `double_all` | 0: 34219 | 0: 129672 |

耗时：一次探针运行（一个作业）约 27 秒，其中编辑模式探针约 3–5 秒（DP5 打开 Main 占大头），Play 模式探针不到 1 秒；Python 一侧的比较约 3 秒。

## 3. 编辑器 Mono 的 float 算术

DP3 说明，编辑器的 Mono JIT 把 float 表达式的中间结果按 binary64 计算，只在**存储**时舍入到 binary32：存进编译器保留的局部变量（`/optimize+` 下只用一次的局部变量会被消掉）、字段、float 返回值或 float 参数时。`Evaluate` 里 `raw = (timing - left) / (right - left)` 是保留的局部变量，所以商在 double 里算、存时舍入一次；`return first + factor * (second - first)` 在 double 里算、返回时舍入一次。这正是 `double_all` 的定义。

同一规则解释了其余全部差异，预言机的其它 float 路径按设计（1.5 节 Python 第 3 条）逐步舍入，所以会差：

| 位置 | C# | 预言机 | 编辑器的算法 | 解释的差异 |
|---|---|---|---|---|
| `SimulationData.InterpolateSnapshot` 的 TotalPlaytime（`SimulationData.cs:76`）| `a + (b − a) * t` 在 double 里算，存进 float 字段时舍入一次 | 每步舍入 | `probes.mono_total_playtime` | DP4 的 20/20 |
| `BuildSpawnerBakeReport` 的 `newHealthRaw`（`SimulationConfig.cs:158`）| `(double)(1f - usedResist)` 从未舍入成 float | `float(f32(1 − resist))` | `probes.mono_new_health_raw` | DP5 的 4/4（取整后的 NewHealth 不受影响）|
| `ItemModel.CalcDamage/CalcArmor`（`ItemModel.cs:475-518`）| 整个和在 double 里算，以 double 返回，从不舍入 | 每步舍入成 float | `probes.mono_item_values` | DP7 的 136/136（偏差 −0.67..+0.87 个 binary32 ulp）|

这些函数只用来给差异分类，不是预言机；测试断言"每个差异都能这样解释"，并把差异个数（只许减少）和直方图冻结进黄金套件。是否让预言机的其它 float 路径也改用编辑器的算法，按设计交给 Q03（Z1 汇总）：随包 `Simulation Data` 是在编辑器里跑出来的，DP7 说明武器伤害一旦超过 2^24 就会出现这类差别。

## 4. 黄金套件 `differential`

`tests/golden/current/differential.json`：每个 DP 的计数与不符个数、DP3 的实测舍入（`DP3.editor_rounding = "double_all"`）与四张直方图、K2/K3 锚点（原版基线 109/109）、DP4/DP5/DP7 的差异个数与直方图、各锚点的数值与集合。测量值（实测舍入、直方图、计数）首次接受时以 `--seed-original` 写进原版基线。

## 5. 与设计稿不同的地方

- **DP4、DP7 不是"全部相等"**：差异全部来自第 3 节的编辑器算术，已逐个解释并冻结，按设计交给 Q03，没有放宽成容差。
- **DP5 的中间量**：4 个 NewHealthRaw 超出相对 1e-12（3.7e-8），原因同上；NewHealth/NewDamage 717/717 逐位相等。
- **G19 锚点的口径**：在存储日 d（不是第一个）上，`GetByTimeChunkIndex(d)` 以 t = 1 插值，返回新建的快照而不是存储元素；所以锚点比较 TimeChunkIndex 与 Location 的序列，不要求是同一个元素（预言机行为相同，DP4 的来源比较也一致）。
- **G18 计数**：设计稿 3.4 写 690，这是宠物数据上的数；无宠物数据上是 675，与 G18 一致。
- **改了 C0b 的两个文件**（Z1 记进设计稿）：`SimulationHarness.cs` 加 partial 钩子 `RebuildJobRunProbes`（在 `RebuildJobExportOutputs` 之前调用）；`SimulationPlayModeDriver.StartJob` 在作业带 `probes` 时先调 `SimulationProbes.RunEdit`。启动器只调 `SimulationBatch` 的入口，所以编辑模式的探针挂在作业开始处，而不是单独的入口。
- 新增 `SimulationRunRequest.Probes.cs`（`job.probes` 字段），Python 一侧经 `request.register_fields` 登记，manifest 键 `probes` 经 `schema.register_manifest_keys` 登记。
- 不需要 Unity 的测试放在 `tests/test_probes.py`：`tests/unity/` 下的测试都带 `unity` 关键字（目录名），没有 `--unity` 时一律跳过。
- 本单元没有改 `BalanceInterpolation.Evaluate`（Q03）：编辑器已经达到 109/109。
- `model/interpolation_default.json` 改成 `double_all`，单独提交；见第 6 节。

## 6. 预言机默认舍入的切换

DP3 之后把 `model/interpolation_default.json` 的 `rounding` 从 `fma` 改成 `double_all`（P2 → C2 的交接，Q32），重跑 G11、G13：

- G11：默认舍入下 K2、K3 仍是 109/109，SFF/PM/TF/TV 仍是 981/981，`G11.default.*` 不变；
- G13：默认舍入下的计数与集合不变（79/108、104/108、79；76/78；13、43）；
- G20：A 组 1380/1380 不变；
- 只有两个按默认舍入取指纹的键变了：`bake` 套件的 `oracle.raw_bits.default`，`simcalc` 套件的 `G20.variant_b`（B 组重放，没有随包参照，第 115、127、140 天等的和在 1e-8 量级上变化）。两者都带写明舍入切换的接受记录重新接受；`exact` 键的任何变化对原版基线都算放宽，所以同时在 `loosening/bake.jsonl`、`loosening/simcalc.jsonl` 登记。其它基线都没有变。
