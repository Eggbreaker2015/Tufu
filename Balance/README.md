# 数值模型

把学习工程里的全部数值数据导出成 SQLite 和 CSV，作为数值建模的唯一数据源。改了配置或场景后重跑即可，同样的输入得到逐字节相同的输出。

公式清单和恢复状态见 [数值公式清单](../Docs/Balance/FormulaCatalog.md)，依赖关系见 [数值依赖图](../Docs/Balance/DependencyGraph.md)。

## 目录

| 路径 | 内容 |
|---|---|
| `tufu_balance/extract.py` | 数据抽取 |
| `tufu_balance/graph.py` | 依赖图构建与改动影响查询 |
| `tufu_balance/render_catalog.py` | 从 `catalog/formulas.json` 生成公式清单文档 |
| `tufu_balance/model/` | 可执行模型的核心：属性曲线、防御曲线及其反解、插值、模拟快照查询、烘焙 Health/Damage，均注明移植自哪段 C# |
| `analysis/` | 阶段 4 的反推脚本：`bake_check.py`、`island_timeline.py`、`item_growth.py`，结果写到 `out/analysis/`，说明见[生成规则总览](../Docs/Balance/Generation/README.md) |
| `catalog/formulas.json` | 281 条公式：表达式、变量、代码位置、恢复状态、核验记录、依赖图用的 `inputs`/`outputs`（每个输出带 `depends_on`），以及从数据反推的 `generation_rule` |
| `catalog/values.json` | 派生量和运行时状态词表，含所在层（static/progress/combat/simulation） |

## 运行

在仓库根目录执行（只需第一次建环境）：

```bash
python3 -m venv Balance/.venv && Balance/.venv/bin/pip install -r Balance/requirements.txt
```

```bash
cd Balance && .venv/bin/python -m tufu_balance.extract
```

约 7 秒。之后可以构建依赖图并查询改动影响（需先抽取，校验配置节点要用数据字典）：

```bash
cd Balance && .venv/bin/python -m tufu_balance.graph build
```

```bash
cd Balance && .venv/bin/python -m tufu_balance.graph impact CutDamageGrowthPerLevelByRarity --runtime-only --stop-at combat,progress
```

查询命令和分层说明见[数值依赖图](../Docs/Balance/DependencyGraph.md)。

抽取输出写到 `Balance/out/`（不入库）：`balance.sqlite`、`csv/<分组>/<表>.csv`（UTF-8 BOM，可直接用 Excel 打开）和 `extract_report.json`。`--skip-prefabs` 可跳过预制体组件。

## 数据来源

| 来源 | 内容 |
|---|---|
| `Assets/**/*.asset` | 所有以游戏脚本为类型的 ScriptableObject 配置（151 个） |
| `Scenes/Main.unity`、`Boot.unity` | 场景中带数据的游戏组件，含 717 个 `EnemySpawner`，按层级算出世界坐标 |
| `Assets/**/*.prefab` | 预制体中带数据的游戏组件 |
| `Assets/Scripts/**/*.cs` | 枚举定义和字段类型，用来把整数解码成名字 |

对象引用转成资产路径（`MonoBehaviour/cut common.asset`），场景内引用写成 `local:<fileID>`。枚举字段旁边会多一列 `<字段>__name`，例如 `Type` 与 `Type__name = Health`。

## 表

**core/**：建模直接用的规范化表。

| 表 | 一行是 |
|---|---|
| `spawners` | 一个刷怪点：原 `EnemySpawnerConfig` 全部字段（保持原字段名）、`stat.<StatType>` 各属性基础值、奖励摘要、世界坐标、是否启用，以及按 Key 关联的模拟烘焙结果 `bake.*` |
| `spawner_stats` | 刷怪点的一条属性（长表） |
| `spawner_rewards` | 刷怪点击杀/吞噬的一条属性奖励 |
| `items` | 一件武器或护甲的 `ItemConfig` |

**assets/**：每个配置类一张 `c_<类名>` 表，一行一个资产；列表字段展开成子表 `c_<类名>__<字段>`（最多两层），如 `c_CombatBalanceConfig__EvolutionDamageValues`、`c_SimulationData__TimeChunkSnapshots__Stats`。

**scene/**、**prefabs/**：同样规则的 `s_<类名>`、`p_<类名>`，键为场景/预制体、fileID 和层级路径。

**raw/**：

| 表 | 内容 |
|---|---|
| `config_values` | 所有配置的每个叶子值（`owner`、`field_path`、数值或文本），适合做改动前后对比 |
| `config_arrays` | 数值数组按下标展开，如 `IslandTargetDays` |
| `assets`、`scene_components`、`prefab_components` | 每个对象的完整 JSON |
| `serialize_references` | `[SerializeReference]` 多态数据（奖励、能力效果等） |
| `enums` | 所有 C# 枚举成员 |
| `data_dictionary` | 每个配置字段一行，带值域统计。`node` 列就是依赖图里 `config:` 节点的名字，如 `CombatBalanceConfig.EvolutionDamageValues[].Value`、`EnemySpawnerConfig.Stats[Health]` |

## 抽取报告的已知事实

`extract_report.json` 的 `checks` 会列出这些核对结果，当前数据是：

- 717 个刷怪点，分布在 1–17 岛；`Eventspawner_Boss_79e49ca7…` 这个 Key 在场景中出现两次。
- `Simulation Config` 有 712 条烘焙记录，全部能对上刷怪点（713 个刷怪点因重复 Key 共享一条）。
- `Simulation Config` 里存的烘焙记录 `bake.Health` 与刷怪点实际 `stat.Health` 只有 8 个在 1% 以内，`OptimumTime` 则大多一致。这份记录不是场景数值的直接来源，不能假设相等。
- 用已恢复的原生烘焙公式（[`simulation.bake_spawner_health`](../Docs/Balance/formulas/simulation.md)）基于 `Simulation Data` 重算，按分钟计时的刷怪点有 79 个血量完全一致；按"天"计时的 608 个没有一个完全一致（中位比 0.995）。说明后期刷怪点是用另一版模拟数据烘焙的。
- `Simulation Data` 有 63 个早期快照（前 3 岛，约 248 分钟）和 46 个时间块快照（到第 17 岛，约 18,700 分钟）。代码中的"天"指 30 分钟游玩时间块（`TimeChunkDurationMinutes = 30`），不是自然日。
- 运行时敌人血量、伤害、刷新冷却和充能会被 `ABTest Data Config` 的分组覆盖（见 `c_ABTestDataConfig__*`），场景值不一定是玩家实际遇到的值。
