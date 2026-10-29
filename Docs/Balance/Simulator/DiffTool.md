# 改一个数，看全局影响

这份工具把同一套模型的「基线」和「改动后」放在一起比较。它不会保存 Unity 资产，也不会把模拟结果推广成游戏里的 Simulation Data。当前重建模拟器尚未完成 K1–K3 校准，模拟报告明确标为 **uncalibrated**；静态计算与随包数据的已知残差另列，不能把残差当作这次改动的效果。

## 第一次使用

在自己的开发工作树的 `Balance/` 下运行，数据库只读打开：

```sh
export TUFU_BALANCE_DB=/Users/joyfort/MyProjects/Tufu-stage5/Balance/out/balance.sqlite
PY=/Users/joyfort/MyProjects/Tufu-stage5/Balance/.venv/bin/python
$PY -m tufu_balance.diff patches/examples/island10_later.yaml --skip-sim
```

结果在 `Balance/out/diff/island10_later/report.md`。`--out <目录>` 可以另选位置。`--skip-sim` 只运行 Python，通常不到一秒，烘焙使用随包快照。空补丁的报告写「无变化」，每张对比 CSV 只有表头。跳过模拟时，模拟 CSV 为空表示没有运行。

去掉 `--skip-sim` 就会经启动器在专用运行工作树里执行 Unity。先提交代码，默认运行本工作树的 HEAD；也可以指定 `--rev <提交>`。两侧使用相同代码、参数和轮数。运行工作树受锁、存档沙箱、资产守卫保护。不会启动或操作主检出的编辑器。基线模拟按 P10 的作业键缓存，第二次相同请求无需启动 Unity；守卫失败的结果不进入缓存。

## 怎么写补丁

示例 A `patches/examples/island10_later.yaml` 只把第 10 岛的 ITD 加 2 天：

```yaml
name: island10_later
description: 第 10 岛晚 2 天
overrides:
  - node: CombatBalanceConfig.IslandTargetDays
    index: 9
    add: 2.0
regenerate:
  optimum_time_from_relative: true
  evolution_target_timings: true
  interpolation_timing_points: true
  island_target_days_tail: false
  evolution_tables: frozen
  sim_autobalance: frozen
  bake: true
simulate:
  enabled: true
  params: calibrated
  stop_day: 405
  bake_iterations: 1
report:
  key_days: [5, 10, 20, 46, 104, 206, 405]
  top_n: 10
```

`index` 从 0 开始。每条覆盖只能有 `set`（替换）、`add`（增加）或 `mul`（乘）之一。区间用 `index: "3:8"`，含 3、不含 8。列表或场景对象用 `where` 等值筛选；筛选必须至少命中一行，多行全部修改。多个条件同时成立才命中。

示例 B `spawner_fight_time.yaml` 用稳定身份 `(scene, file_id)` 指定一个刷怪点，把 `AverageFightTime` 乘 1.2：

```yaml
name: spawner_fight_time
overrides:
  - node: EnemySpawnerConfig.AverageFightTime
    where: {scene: Scenes/Main.unity, file_id: 57651}
    mul: 1.2
```

运行 `$PY -m tufu_balance.diff patches/examples/spawner_fight_time.yaml --skip-sim`，只有这个点的重新烘焙 HP 和 Damage 变化。不要把「战斗时间增加 20%」理解成敌人伤害也增加 20%；烘焙同时解算血量和伤害。

### 去哪里找节点和刷怪点

合法 `node` 来自数据库的 `data_dictionary.node`（不带图里的 `config:` 前缀）。在 `Balance/` 下可用只读查询查找名称、样本和值域：

```sh
sqlite3 -readonly "$TUFU_BALANCE_DB" "SELECT node, sample, min, max FROM data_dictionary WHERE node LIKE '%AverageFightTime%' ORDER BY node;"
sqlite3 -readonly "$TUFU_BALANCE_DB" "SELECT scene, file_id, Key, go_name, LocationNumber, IsEliteBoss FROM spawners WHERE Key LIKE '%Spawner_10%' OR go_name LIKE '%boss%' ORDER BY scene, file_id;"
```

第二条查询的 `scene`、`file_id` 可直接填入示例 B 的 `where`。`Key` 和 `go_name` 供搜索、辨认，稳定身份仍是 `(scene, file_id)`；名称可能重复。想按岛或精英筛选时，可用同表的 `LocationNumber`、`IsEliteBoss`，例如 `where: {LocationNumber: 10, IsEliteBoss: 1}`；先查询确认命中范围。其它列表的等值键可参考 [补丁用例说明](../../../Balance/tests/patch_cases/README.md)。[公式目录](../FormulaCatalog.md) 和 [依赖图](../DependencyGraph.md) 说明节点由哪些公式读取；工具产生的 `touched_formulas.csv` 则列出本次改动的传播路径。

`base: another.yaml` 可继承另一份补丁，先执行继承的覆盖，再执行当前覆盖。生成开关遵循 P9 的语义：未开启的步骤不改配置，例外点和显式覆盖优先。开关展开为明确的 `set` 发给 C#，C# 不再自行推导生成规则。`regenerate.bake` 不写资产；报告始终计算两侧烘焙供比较。`bake_iterations: 2` 会将第一轮报告里的可写普通刷怪点 HP/Damage 在内存中用于第二轮模拟；活动、VIP、BossTower、Dummy 和 HandBalance 不进入下一轮写入。

`simulate.params` 接受 defaults、参数层名、JSON 路径或参数映射。`calibrated.json` 不存在时，`params: calibrated` 明确警告并回退 defaults；其它不存在的参数层报错。自定义参数映射仍按 uncalibrated 报告。`sim_autobalance` 支持 frozen/regenerate，两侧一致。`stop_day` 超过 405 给出外推警告。

## 怎么读报告

1. **补丁与校验**：看命中行数和展开叶子数。类型、节点、选择器错误会停止。超出随包 min/max 只警告。模拟开启时，C9 的 `applied_patch.json` 必须逐叶子与 Python PatchReport 一致，否则停止，不能继续解释效果。
2. **公式**：immediate 是即时影响，progression 加成长影响，full 加重新生成、模拟和烘焙。公式表列出深度、上一跳、状态、Python 函数、C# 重建成员和出处。覆盖分别统计 Python、C# 重建、C# 原生、未建模；类别可重叠。原生按目录 recovered_native（加属性原始值合成）登记，不表示本次运行的分支覆盖；没有登记实现者的 reconstructed 仍标「未建模」。
3. **静态表**：每行是一个变化单元格，含 key、字段、基线、补丁、差值和比值。ITD +2 的例子只改变岛 10、11 的 OT 窗口，进化时间点与插值节点同步移动，进化数值保持 frozen。16 个原始 OT 例外保留存储值并在警告里列出；这些例外目前都不在岛 10、11。
4. **烘焙**：`bake/spawners.csv` 只含变化单元格。`model_floor.csv` 是基线模型对随包场景的背景残差，包含未变化的点；它不是这次补丁的变化表。
5. **模拟**：逐日 Value、TotalValue、Gpm、DPS/防御、到岛日、物品首次出现与进化、活跃分钟、首杀见 `sim/`。`rpm.csv` 使用快照里的 CurrentGpm，等同导出所用的 RPM 缓存口径，不代表重新拟合。`baseline/` 与 `patched/` 保存两侧完整表。到岛日超过该岛起始日（第 L 岛用 ITD[L−2]，第 1 岛为 0）两天、loss/H 在 1 的 ±3% 附近发生变化会警告。两侧都没有事件，或只有初始化的刀而没有物品成长时，对应表标「两侧均无数据：当前重建未模拟…」，不作「无变化」结论；触及物品/进化输入会同时给出 `sim_not_modelled` 警告。
6. **警告**：未建模字段会列出直接读取它的公式；没有公式的字段明确写「目录里没有公式读取这个字段」。静态表没变化不等于游戏没有影响，例如 `SimulationConfig.TimeChunkDurationMinutes` 不影响机器人模拟，但其它运行逻辑会读取它。

旁路文件 `patch_report.json` 保留展开操作；`operations.csv` 是逐条操作的命中统计，`overrides.csv` 是逐叶子的旧值/新值与 C# 校验状态。编辑器工具叶子标「仅 Python（编辑器工具，C# 不应用）」，示例 A 为 117/119 叶子参与 C# 比较。`validation.json` 记录 X1–X7 检查结果及 vacuous 部分。检查失败会停止，X2 的 slow_tick 失败也始终致命（包括 uncalibrated）。

Enginefix 根据随包数据把非强制早期快照的缓存刷新移到战斗/空闲之后，X2 使用快照自身属性、物品和槽位重算，并额外核对刷新点的 basis；原 Tick 起始状态临时豁免已删除。三项豁免子条件测试随之不再适用，测试直接锁定 X1–X7 任一失败必须停止。

就绪表使用 `ready_spawners.expected.<内容哈希>.csv` 相对名；启动器从当前配置的只读 DB 写旁路输入。请求与缓存键不含工作树或 out_root 的绝对路径。

传入 `--db` 时，就绪表也从同一数据库生成，无需额外设置环境变量；若已设的 `TUFU_BALANCE_DB` 指向另一份数据库，模拟前会报错，须统一两者。

C9 在初始化后应用补丁：改 ItemReward、刷怪点冷却/充能或 AB 冷却来源时，场景自举已经算出的 ItemReward/HasItemReward、ChargesTotal/BaseCooldown 可能还是旧值。工具给出初始化缓存警告；两侧补丁叶子一致不等于缓存已经更新。这是 C9 的现有交接限制。

## 直接修改资产后比较

在自己的工作树里、资产还未改动时记录基线：

```sh
$PY -m tufu_balance.diff --cache-base
```

之后改一个配置资产，运行：

```sh
$PY -m tufu_balance.diff --head worktree --base cached --skip-sim
# 或显式指定同一数据库所对应的 git 版本：
$PY -m tufu_balance.diff --head worktree --base HEAD --skip-sim
```

工具从 git 对象读取 base，从当前工作树读取 head，使用原抽取器的 Unity YAML、字段类型和引用解析，在内存中比较并展开为补丁；`static/asset_leaves.csv` 来自 config_values 的原始叶子差异。资产文件不会被工具修改。支持现有 ScriptableObject 的数值、数组、已有列表元素；新增/删除行、场景、Prefab、配置或身份相关的 meta 变化明确拒绝，避免漏掉无法安全映射到 C9 的改动。普通纹理等非配置资产的 meta 仅在 GUID 不变且对应资产仍存在时忽略，并列在 manifest 的 `ignored_meta`；脚本、DLL、ScriptableObject、场景、Prefab 和目录的 meta 不享有此例外。未建模叶子只报告原始差异和公式读取方；这类资产报告必须加 `--skip-sim`。base 相对数据库存在覆盖时，也要求先静态比较或重新抽取对应数据库再模拟。

这里对 6.6 有两处有意收窄，等待 Z1 整合：资产版本模式先提供安全的已有配置叶子比较，不启动带未提交场景改动的 Unity；基线索引由 `diff --cache-base` 创建，P13 的 refresh 可调用相同入口接入。模拟开启时支持的 head 数值改动通过 C9 内存补丁送入干净的运行工作树。

## 可重复性与耗时

所有对比 CSV 按键排序，浮点用 repr。相同数据库、补丁、代码和参数生成逐字节相同的 report、CSV、manifest 和校验 JSON。`manifest.json` 保存数据库/补丁/参数哈希、代码提交、两个作业键与轮数。墙钟耗时、缓存是否命中和重试次数单独放在 `timings.json`；它不参加逐字节比较。这是对 6.4「manifest 内含耗时」的必要调整，否则同一请求无法字节一致。

验收命令：`pytest -q tests/test_diff.py`；实际 Unity 接口、两侧叶子一致、X1–X7、缓存和重复报告：`pytest -q --unity tests/unity/test_diff_e2e.py`。资产测试在 pytest 的临时目录里建立独立 git 仓库、复制 Scripts 和所需资产后修改并恢复；不写运行 pytest 的仓库。离线模拟测试用合成缓存目录覆盖补丁不一致、交叉检查失败（包括 slow_tick X2）、空数据语义与两轮烘焙，无需 Unity。
