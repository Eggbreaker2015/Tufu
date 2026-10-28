# 黄金夹具（W0 冻结）

阶段 5 的原版基线夹具，由 W0 从开发工作树的抽取结果冻结，之后不可变，任何单元都只读。设计见 [阶段 5 设计](../../../../Docs/Balance/Simulator/Design.md) 3.3 节“夹具”。

## 文件

| 文件 | 内容 | 来源 | 谁用 |
|---|---|---|---|
| `stage4_summaries.json` | 阶段 4 三个分析脚本的 `summary.json`（`bake`、`growth`、`timeline`），解析后重新序列化 | `analysis/{bake_check,item_growth,island_timeline}.py`，在本库上重跑 | 回归对照 |
| `stage4_slot_schedule.json` | 每个天快照的武器/护甲槽数及对应的 DpsData/DefenseData 最大相对误差。`kind` 为 `"fitted"`：逐快照在 1–8 里取误差最小的槽数（并列取小），不是独立事实 | `item_growth.runtime_section` 的拟合 | R2（P8）；重放请求的 `override.slots`（C4）；不能用来验证 P12 |
| `item_growth_values.json` | 46 个天快照上每件物品的 (Key, Level, Copies)、是否已进化、`calc_damage`/`calc_armor` 的值 | `item_growth.calc_damage`、`calc_armor`、`snapshot_item_states` | P4 |
| `forensics_targets.json` | 校准目标序列：天快照与早期快照的属性值、Gpm、DpsData、DefenseData、物品，逐区间活跃分钟，各岛首次出现的快照区间 | 移植 forensics `load.py`、`targets.py` | P10 |
| `forensics_killdays.json` | θ = 1、开局状态下每个天制刷怪点的首杀日；各岛门禁与解锁日；坏区间及残差；θ 网格（消融）；玩家 DPS 缩放 0.95–1.05 的坏区间曲线；同日/次日规划新岛；可杀性谓词用当天/滞后的 K2、K3 | 移植 forensics `fight.py`、`spawn.py`、`rulefit.py`，architect `l17check.py`，critic_fid `sens.py`，critique-fidelity `nextday.py`、`killlag.py` | P8、C5 |
| `bake_probe_counts.json` | 烘焙精确计数（分钟制精确 HP 的刷怪点集合按 `Key|名字` 冻结，附 sha256）、属性列合成、插值节点、进化时间点、岛表尾部的计数 | 移植 model `golden_probe.py` | P7 |
| `extract_parity.json` | 开发工作树抽取与主检出抽取的差异表，逐行归因 | 移植 `dbparity.py`、`rowdiff.py` | 记录 |

每个夹具的 `meta` 带：

- `source`：生成脚本路径与 sha256；`ported_from` 是移植时调研脚本的路径与 sha256（脚本在会话 scratchpad 里，可能已被清理）；`imports` 是导入的仓库模块及其 sha256；
- `db_sha256`：所读 `balance.sqlite` 的 sha256；
- `git_rev`、`date`：生成时开发工作树的提交与日期。

## 生成与核对

```bash
cd Balance
.venv/bin/python tests/golden/fixtures/generate.py --compare-survey     # 生成（W0 只做一次）
.venv/bin/python tests/golden/fixtures/generate.py --check              # 核对
```

- 数据库取 `$TUFU_BALANCE_DB`，否则取 `Balance/out/balance.sqlite`，只读。
- `--check` 用已提交夹具里的 `date` 与 `git_rev` 在临时目录重新生成，逐字节比较。`date`、`git_rev` 是传入的参数，不随运行时刻变化；生成脚本自身的 sha256 也写在夹具里，改了脚本，核对就会失败。
- `extract_parity.json` 还要读主检出的数据库（`--main-db`，默认 `/Users/joyfort/MyProjects/Tufu/Balance/out/balance.sqlite`）。它的 sha256 记在 `meta.main_db` 里；主检出重新抽取后哈希变了，`--check` 跳过这个文件并打印说明。
- 夹具里没有耗时。`golden_probe.py` 原来打印的 `hash(tuple(keys))` 随进程变化，改成键列表的 sha256。
- 一次生成约 10 秒（Apple Silicon，Python 3.14）；连续两次生成、以及换 `PYTHONHASHSEED` 生成，7 个文件都逐字节相同。

## 冻结时核对过的数字

- `forensics_killdays.json`，θ = 1、开局状态：
  - 坏区间 4/41：17→19（CutDamageFlat +500）、24→27（Health −24000、HealthRegen −21）、37→41（StabDamageFlat +600）、140→154；
  - 解锁日 {4: 5, 5: 8, 6: 11, 7: 15, 8: 21, 9: 26, 10: 35, 11: 44, 12: 57, 13: 71, 14: 90, 15: 110, 16: 131, 17: 151}；
  - 玩家 DPS 缩放 0.95/0.98/0.99/1.00/1.01/1.02/1.05 → 坏区间 7/6/5/4/3/2/3；
  - 同日规划新岛 4/41，次日 14/41；
  - 可杀性用当天 K2/K3 4/41，用前一天的 6/41（多出 41→46、94→104）。
- `forensics_targets.json` 去掉 `meta` 后与调研的 `golden_targets.json` 完全相等。
- `bake_probe_counts.json` 与在主检出数据库上运行原版 `golden_probe.py` 的输出逐项相同：分钟制 HP 精确 79/108、Damage 104/108、两者 79；`SpawnerBakeInfos` Health 13、Damage 43；天制 HP 1、Damage 20；节点 49 个，float32 精确 45，误差 ≤ 5e-5 的 49；进化时间点 150/150；岛表尾部 84/84。
- `stage4_summaries.json` 的 `timeline` 与主检出 `out/analysis` 及调研备份相同；`bake`、`growth` 各差一处，都由主检出多出的宠物物品 `player_claw` 引起：
  - `bake.data_facts.n_items`：88（本库）对 89；
  - `growth.runtime.evolve_jump_ratio_at_threshold`：`n` 675 对 690，`mean` 1.41853 对 1.424207；最小、中位、最大不变。设计文档 G18 写的“690 个组合”是在含宠物的数据上数的，原版数据上是 675。

## 与设计的出入

- **抽取对照**：设计预期只差 `c_PetConfig*` 与 `Player Config` 的 `PlayerAttackItem`。实际差异更广，但全部与宠物有关（`unexplained = 0`）：
  - 只有主检出有的表：`c_PetConfig`、`c_PetConfig__Pets`；
  - 宠物物品 `player_claw`：`items`、`c_ItemConfig` 各多一行；它的 `AbilityDefs` 是空列表，抽取成标量列，所以 `c_ItemConfig` 多出 `AbilityDefs` 列（只有这一行非空）；
  - `Pet Config` 资产：`assets` 一行、`config_values` 27 行；`player_claw` 另有 `assets` 一行、`config_values` 18 行；
  - `Player Config`：`assets.data` 只差 `PlayerAttackItem`，`c_PlayerConfig` 多一列，`config_values` 多一行；
  - 枚举 `PetState`、`MissingHealthBonusReceiver`（宠物分支 `7972f29` 新增）；
  - `data_dictionary`：15 个宠物节点（13 个 `PetConfig.*`、`ItemConfig.AbilityDefs`、`PlayerConfig.PlayerAttackItem`），另 17 个 `ItemConfig.*` 节点的统计量因多一行而变化。
- **G18 的组合数**：见上，原版数据上是 675，不是 690。
- **`golden_probe.py` 的属性列合成计数**是用 float64、容差 1e-9 粗算的：天快照 TF 405/414、TV 393/414（合计 TF 972、TV 960）。G11 的 981/981 由 P2 按 float32 和舍入方式精确重算得到，不以这里的计数为准。
- **刷怪点 `Key` 不唯一**：717 个刷怪点只有 716 个不同的 `Key`，重复的是两个 `Eventspawner_Boss`（活动刷怪点，不进可杀性规则）。`forensics_killdays.json` 的 `day_spawners` 同时给出 `Key` 和 `file_id`。
