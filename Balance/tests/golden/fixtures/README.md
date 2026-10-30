# 黄金夹具（W0 冻结）

阶段 5 的原版基线夹具，由 W0 从开发工作树的抽取结果冻结，之后不可变，任何单元都只读。设计见 [阶段 5 设计](../../../../Docs/Balance/Simulator/Design.md) 3.3 节“夹具”。

## 文件

| 文件 | 内容 | 来源 | 谁用 |
|---|---|---|---|
| `stage4_summaries.json` | 阶段 4 三个分析脚本的 `summary.json`（`bake`、`growth`、`timeline`），解析后重新序列化 | `analysis/{bake_check,item_growth,island_timeline}.py`，在本库上重跑 | 回归对照 |
| `stage4_slot_schedule.json` | 每个天快照的武器/护甲槽数及对应的 DpsData/DefenseData 最大相对误差。`kind` 为 `"fitted"`：逐快照在 1–8 里取误差最小的槽数（并列取小），不是独立事实 | `item_growth.runtime_section` 的拟合 | R2（P8）；重放请求的 `override.slots`（C4）；不能用来验证 P12 |
| `item_growth_values.json` | 46 个天快照上每件物品的 (Key, Level, Copies)、是否已进化、`calc_damage`/`calc_armor` 的值 | `item_growth.calc_damage`、`calc_armor`、`snapshot_item_states` | P4 |
| `forensics_targets.json` | 校准目标序列：天快照与早期快照的属性值、Gpm、DpsData、DefenseData、物品，逐区间活跃分钟，各岛首次出现的快照区间 | 移植 forensics `load.py`、`targets.py` | P10 |
| `forensics_killdays.json` | θ = 1、开局状态下每个天制刷怪点的首杀日；各岛门禁与解锁日；坏区间及残差；θ 网格（消融）；玩家 DPS 缩放 0.95–1.05 的坏区间曲线；同日/次日规划新岛；可杀性谓词用当天/滞后的 K2、K3；岛 3 门禁与第一个 dragon 逐日的 fightTime、loss/H（`l3_gate_lethality`，`SlowLethalityGate` 的依据）| 移植 forensics `fight.py`、`spawn.py`、`rulefit.py`，architect `l17check.py`，critic_fid `sens.py`，critique-fidelity `nextday.py`、`killlag.py`、`l3gate.py` | P8、C5、P15 |
| `bake_probe_counts.json` | 烘焙精确计数（分钟制精确 HP 的刷怪点集合按 `Key|名字` 冻结，附 sha256）、属性列合成、插值节点、进化时间点、岛表尾部的计数 | 移植 model `golden_probe.py` | P7 |
| `interpolation_rounding.json` | G11：`stepwise`、`fma`、`double_tail`、`double_all` 四种舍入下，109 个快照的 K2/K3 通过数、主属性 981 个单元的 SFF/PM/TF/TV 逐位相等计数、其余 2289 个单元的 0/1/恒等计数、不通过的快照；四种舍入在 0..800 的 60,800 个网格点上两两不同的点数 | 移植 architect `fma_check.py`、critique-fidelity `fma3.py` | P2、C2（DP3）|
| `rpm_replay.json` | G20：天快照 CurrentGpm 重放，K2/K3 与 K4/K5 各取当天或前一天共四种口径的命中数和全部未命中（存储值、重放值）；`RpmFirst` 口径下 46 × 30 个重放值 | 移植 forensics `rpm.py`、critique-fidelity `lag.py` | P8、DP8 |
| `extract_parity.json` | 开发工作树抽取与主检出抽取的差异表，逐行归因 | W0 自写的对照逻辑（不是调研移植）| 记录 |

`generate.py` 是生成脚本，不在设计 7.3 列出的文件里，是有意加的：夹具的 `source` 指向它，`--check` 靠它核对。

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

- 数据库取 `$TUFU_BALANCE_DB`，否则取 `Balance/out/balance.sqlite`，只读。主检出的数据库取 `--main-db`，否则 `$TUFU_MAIN_BALANCE_DB`，否则默认路径；`--compare-survey` 的调研目录可用 `$TUFU_S5_SURVEY_DIR` 改。这些只影响读哪里，不进夹具内容。
- `--check` 用已提交夹具里的 `date` 与 `git_rev` 在临时目录重新生成后比较。`date`、`git_rev` 是传入的参数，不随运行时刻变化。生成脚本和被引用的 `model` 模块的 sha256 只是出处记录：之后的单元会继续改这些模块，所以只有这些哈希不同时，`--check` 列出变化的文件但不判失败；其余内容（全部数值和元数据）必须一致。
- 不带 `--out` 直接运行会拒绝覆盖这里的已冻结夹具（退出码 2）；确需重新冻结时加 `--force`。
- `extract_parity.json` 还要读主检出的数据库（默认 `/Users/joyfort/MyProjects/Tufu/Balance/out/balance.sqlite`）。它的 sha256 记在 `meta.main_db` 里；主检出重新抽取后哈希变了或文件不在，`--check` 不比较这个文件，打印 `NOT CHECKED` 并以退出码 2 结束；加 `--skip-parity` 表示接受，退出码 0。其余文件有差异时退出码 1。
- 夹具里没有耗时。`golden_probe.py` 原来打印的 `hash(tuple(keys))` 随进程变化，改成键列表的 sha256。
- 一次生成约 14 秒（Apple Silicon，Python 3.14）；连续两次生成、以及换 `PYTHONHASHSEED` 生成，9 个文件都逐字节相同。

## 冻结时核对过的数字

- `forensics_killdays.json`，θ = 1、开局状态：
  - 坏区间 4/41：17→19（CutDamageFlat +500）、24→27（Health −24000、HealthRegen −21）、37→41（StabDamageFlat +600）、140→154；
  - 解锁日 {4: 5, 5: 8, 6: 11, 7: 15, 8: 21, 9: 26, 10: 35, 11: 44, 12: 57, 13: 71, 14: 90, 15: 110, 16: 131, 17: 151}；
  - 玩家 DPS 缩放 0.95/0.98/0.99/1.00/1.01/1.02/1.05 → 坏区间 7/6/5/4/3/2/3；
  - 同日规划新岛 4/41，次日 14/41；
  - 可杀性用当天 K2/K3 4/41，用前一天的 6/41（多出 41→46、94→104）；
  - 岛 3 门禁 `Spawner_boss_cut_rare_388c…`（OT 240）：第 3 天 fightTime 41.9 秒、loss/H 4.163，第 4 天 1.066，第 5 天 0.212，第 6 天 0；第一个 dragon（岛 4，OT 7）：第 6、7、8 天 loss/H 3.698、1.312、0.354。
- `interpolation_rounding.json`（G11）：
  - `fma`、`double_tail`、`double_all`：K2、K3 各 109/109，SFF、PM、TF、TV 各 981/981，其余 2289/2289；
  - `stepwise`（消融）：K2 108/109、K3 105/109，SFF 972、PM 945、TF 972、TV 960；
  - 网格差异：`fma`–`double_tail` 123、`fma`–`double_all` 263、`double_tail`–`double_all` 140，`stepwise` 对三者 842、875、1015（共 60,800 点）。
- `rpm_replay.json`（G20）：K2/K3 取前一天、K4/K5 取当天（`RpmFirst`）1380/1380；K2/K3 取当天 1376/1380，未命中恰为 TC1 Health、TC30 CritDamagePercent、TC94 Health、TC94 StabDamageFlat，都是存储值低于重放值；两者都取前一天 1047/1380；只有 K4/K5 取前一天 1046/1380。
- 以上三项与在开发工作树数据库的副本上运行原版 `l3gate.py`、`fma_check.py`、`fma3.py`、`lag.py` 的输出逐项相同（`lag.py` 的四组未命中列表全部相同）。
- `forensics_targets.json` 去掉 `meta` 后与调研的 `golden_targets.json` 完全相等。
- `bake_probe_counts.json` 与在主检出数据库上运行原版 `golden_probe.py` 的输出逐项相同。其中 `snapshot_composition` 用的是模型当前的 `Evaluate`（即 `stepwise` 舍入）加容差，天快照 TF 405/414、TV 393/414，合计 TF 972、TV 960，正好是 G11 的 `stepwise` 消融值；G11 的 981/981 见 `interpolation_rounding.json`。其余：分钟制 HP 精确 79/108、Damage 104/108、两者 79；`SpawnerBakeInfos` Health 13、Damage 43；天制 HP 1、Damage 20；节点 49 个，float32 精确 45，误差 ≤ 5e-5 的 49；进化时间点 150/150；岛表尾部 84/84。
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
- **刷怪点 `Key` 不唯一**：717 个刷怪点只有 716 个不同的 `Key`，重复的是两个 `Eventspawner_Boss`（活动刷怪点，不进可杀性规则）。`forensics_killdays.json` 的 `day_spawners` 同时给出 `Key` 和 `file_id`。

## W0 环境记录

供 C0a/C0b 抄进 Runbook。

- **运行工作树**：`/Users/joyfort/MyProjects/Tufu-stage5-run`，游离 HEAD，首次打开时在 `5806ec7`。
- **Library 来源**：从主检出 `/Users/joyfort/MyProjects/Tufu/Recovery/StudyProject/Library`（`feature/pet-system`）复制，约 594 MB。复制由调度者在本单元之前完成；目录建立于 2026-10-28 02:58:35（本地时间），子目录保留了主检出的创建时间（如 `ArtifactDB` 为 10-27 05:54），与 APFS 克隆（`cp -c`）一致。复制前主检出编辑器是否关闭、复制本身的耗时，本单元没有记录。
- **首次打开**：`Unity -batchmode -quit -projectPath …/Tufu-stage5-run/Recovery/StudyProject -disable-assembly-updater Unity.Mathematics.dll Unity.Burst.dll`，Unity 6000.6.3f1，开始于 2026-10-28T09:58:50Z，约 48 秒，退出码 0（`Exiting batchmode successfully now!`）：
  - `Asset Pipeline Refresh … Total: 38.958 seconds`，其中脚本编译 34.27 秒（宠物分支与 `5806ec7` 不同的脚本重新编译）；
  - `error CS` 0 个；`warning CS` 7448 个：CS0649 4700、CS0169 2686、CS0618 20、CS0414 20、CS4014 12、CS0105 8、CS8632 2（按行计数；同一条警告在日志里出现两遍，去重后 3721 行）；
  - 日志：会话 scratchpad 的 `stage5/w0/first_open.log`（7926 行，sha256 `ba37a5a561a56ce51af80cbe1e4e1f6b232c2a6d177294a4c829a001b96fecb7`），可能被清理。
- **受保护 DLL**（按 `Recovery/Open-StudyProject.ps1` 的期望值核对，大小写不计；运行工作树 `Assets/Plugins/` 里的文件与 `5806ec7` 里的版本哈希相同）：
  - `Unity.Mathematics.dll`：`3a1be62e9bc5b692432d47ca9af877db407157dcf520c131b499e5f5f45e7dd5`，等于期望值；
  - `Unity.Burst.dll`：`bb77bc0cf4e7f5850602d5922d5946d30a6b443b554a565399f3e041e990377d`，等于期望值。
- **`.rsp`**：`Recovery/StudyProject/Library/Bee/artifacts/200b0aE.dag/Assembly-CSharp.rsp` 与 `Assembly-CSharp-Editor.rsp` 存在，供 P0 的 `cscheck` 使用。
