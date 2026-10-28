# 黄金基线

阶段 5 的回归测试拿测得的值与这里的基线比较。基线分两层：**原版基线**（`original/`）记录随包数据上今天的数字，冻结不动；**当前基线**（`current/`）是测试实际比较的对象，只能用 `golden accept` 命令带理由修改。设计见 [阶段 5 设计](../../../Docs/Balance/Simulator/Design.md) 3.2 节。

## 目录

| 路径 | 内容 | 谁改 |
|---|---|---|
| `original/<套件>.json` | 原版基线 | 新键首次 `accept` 时按测试声明的原版值写入（无声明时须给 `--seed-original`）；之后只有 `rebaseline-original` 能改 |
| `current/<套件>.json` | 当前基线，测试与它严格比较 | 只经 `accept` |
| `current/<套件>/<文件>` | 文件套件的逐字节基线，如 `current/sim_run/*.csv` | 只经 `accept --files` |
| `loosening/<套件>.jsonl` | 放宽登记，一行一条：`{key, from, to, kind, better, reason, date, git_rev}` | `accept --loosen` 追加 |
| `accepted/<套件>.jsonl` | 每次 `accept`、`rebaseline-original` 的旧值、新值、理由、提交；写入原版基线的键及来源（`declared` 或 `measured`）| 命令追加 |
| `registry.json` | 公式实现登记的开关 `pending_allowed` | P1 建（初值 true），Z1 改为 false |
| `fixtures/*.json` | 从调研脚本冻结的夹具 | W0 建，之后不可变 |

套件按领域拆开（`core`、`timeline`、`items`、`evolution_gen`、`spawners`、`bake`、`simcalc`、`pipeline`……），一个套件的文件只归一个单元，避免多个单元同时改同一个 JSON。

## 期望的写法

每个键一条期望：

```json
{"kind": "exact", "value": 84, "source": "G01", "note": "岛 16–99 的 ITD 由尾部闭式精确给出"}
```

| kind | 测试时的判定（对当前基线）| 什么算更宽松（对原版基线）|
|---|---|---|
| `exact` | 用 `==`，变好也算失败 | 任何改变。可选 `"better": "higher"/"lower"`，按这个方向的改变算收紧 |
| `set` | 集合相等，失败时列出增减的元素 | 任何改变。可选 `"better": "superset"/"subset"`，例如精确命中的刷怪点集合取 superset、例外集合取 subset |
| `min` | 测得值 `>=` 基线；明显变好时发 `GoldenTightenWarning`，提示收紧 | 基线变低 |
| `max` | 测得值 `<=` 基线；明显变好时发警告 | 基线变高 |

数值比较时 `1 == 1.0`，`NaN` 等于 `NaN`。集合按元素的规范 JSON 排序存放。改了 `kind` 或 `better`、或删掉一个键，都算放宽。

## 测试里怎么用

```python
from tufu_balance.golden import at_least, at_most, check, exact, same_set

def test_g03_rel_to_ot(db):
    ...
    check("timeline", {
        "G03.rel_to_ot_hits": exact(hits, source="G03", better="higher", original=701),
        "G03.exceptions": same_set(exceptions, source="G03", better="subset"),
        "G03.ablation_round_half_up": exact(ablation_hits, source="G03", original=682),
    })
```

第一个参数是测得值；`original=` 写设计文档 3.3 节表里的数字（原版基线）。`check` 先把测得值和声明的原版值记到 `Balance/out/golden/latest/<套件>.json`（不入库），再与当前基线比较，一次列出全部不符的键。新键在当前基线里不存在时测试失败，提示用 `accept` 接受。声明的原版值与 `original/` 里的不一致时测试也失败。逐字节比较的文件用 `check_file(套件, 文件名, bytes)`。

原版基线不会悄悄取测得值：

- 声明了 `original=` 的键，首次 `accept` 时按声明值写进 `original/`。测得值比声明值宽松（例如设计写 84、测得 83）时拒绝，必须先查原因，确认是阶段 4 旧脚本的错误后再给 `--loosen` 登记。
- 没有声明的键（设计写明"首次运行时测出并冻结"的，例如 R1 有信息量子集、R2b、G13 的 `double_tail`/`double_all` 计数，以及只能从夹具得到的集合）首次 `accept` 必须给 `--seed-original`，测得值写进 `original/`，`accepted/` 里记为 `"from": "measured"`。不要对设计表里有数字的键用它。

`latest/` 的读写加了文件锁，并按会话 id（`$TUFU_GOLDEN_RUN`，由 `tests/conftest.py` 在会话开始时设置，pytest-xdist 的子进程继承）区分会话，并行跑测试时不会互相覆盖。

`tests/conftest.py` 提供的夹具：`db_path`、`db`（只读连接；数据库取 `$TUFU_BALANCE_DB`，没有时取 `Balance/out/balance.sqlite`，都不存在就跳过）、`fixtures_dir`、`load_fixture(name)`（夹具缺失时失败）。带 `unity` 标记的测试只在 `pytest --unity` 时运行，会话夹具由 `tests/unity/conftest.py`（C0b）提供；`slow` 标记可用 `-m "not slow"` 排除。

## 命令

在 `Balance/` 下执行，先跑一遍 `pytest` 记下最新结果：

```bash
.venv/bin/python -m tufu_balance.golden status [套件 ...] [--all]
.venv/bin/python -m tufu_balance.golden accept <套件> --reason "…" [--keys K ...] [--drop K ...] [--loosen] [--seed-original] [--allow-stale]
.venv/bin/python -m tufu_balance.golden accept sim_run --files [文件 ...] --reason "…"
.venv/bin/python -m tufu_balance.golden rebaseline-original <套件> --reason "…" [--keys K ...]
.venv/bin/python -m tufu_balance.golden guard
```

- `status`：逐键列出当前、最新、原版三个值，状态为 ok、FAIL、tighten（棘轮可收紧）、new、not-measured，守卫状态为 ok、loosened（已登记）、VIOLATION。有 FAIL 或 VIOLATION 时返回 1。
- `accept`：把最新结果写进 `current/`，在 `accepted/` 追加记录。不给 `--keys` 时接受最近一次测试会话为该套件记下的全部键（一个套件在新会话里第一次记录时，先清掉它上一次会话的记录，所以已删或改名的测试留下的旧键不会混进来；文件套件同理）。结果不是在当前 HEAD 上测得的拒绝执行，需重跑测试；确认无误时可给 `--allow-stale`。新键写进 `original/` 的规则见上一节。新值比原版宽松、又没有已有登记覆盖时拒绝执行（返回 2）；给 `--loosen` 才放行，并在 `loosening/` 追加登记。`--drop` 从当前基线删键，一律算放宽。`--files` 接受文件套件的文件（逐字节基线），同样检查提交。
- `rebaseline-original`：只在原版数据本身变了时用，例如修好抽取器后重新抽取。有声明的键取声明值，没有声明的取测得值。`tests/golden` 有未提交改动时拒绝执行，结果要单独提交。改完后当前基线若比新的原版宽松，守卫会失败，需要再 `accept`。
- `guard`：与 `test_golden_guard.py` 相同的检查：当前基线比原版宽松、又没有登记的键一律失败。收紧随时允许。一条登记覆盖所有不比它的 `to` 更宽松的值，所以登记之后再收紧不必重新登记。

移植后的数字与设计文档不同时，先查原因；只有确认是阶段 4 旧脚本的错误，才放宽并登记。

## 公式实现登记

`tufu_balance/model/registry.py`：

- 预言机里每个移植或生成规则用 `@implements("公式 id", provenance=Provenance.PORT, code_ref="CodeBase/<文件>:<行>", va="0x…")` 登记。出处取 `port`（移植已恢复的原生代码）、`rule`（从数据反推、在数据上精确）、`fit`（拟合、近似）、`data`（数据恒等式或事实）、`inferred`（没有原生代码也没有精确核对的重建）。
- id 必须在 `catalog/formulas.json` 的 `formulas` 或 `aliases` 里，别名按规范 id 登记；未知 id 在 import 时报 `UnknownFormulaId`。目录里还没有条目的规则写 `pending:<领域>.<名字>`，`pending_allowed` 为 true 时只发 `PendingFormulaWarning`，为 false 时报错。
- `scan_csharp()` 从 `Assets/Scripts` 与 `Assets/Editor` 的 C# 源码静态解析 `[SimulationRebuild(Provenance.X, "id")]`，跳过注释；没有运行输出（`rebuild_coverage.json`）时用它统计 C# 一侧。
- `coverage()` 按出处统计两侧，并给出目录 281 条公式中被 Python、C# 或任一侧实现的条数。

枚举在 `tufu_balance/model/enums.py`，由 P1 一次加齐，`tests/test_enums.py` 逐个成员与数据库的 `enums` 表核对。C# 里槽位枚举叫 `ItemSlotType`，配置字段叫 `SlotType`，两个名字都可以用。
