# 补丁语义用例

Python（`tufu_balance.model.patch`，P9）与 C#（`SimulationConfigPatcher`，C9）共用的补丁用例。补丁格式见[阶段 5 设计](../../../Docs/Balance/Simulator/Design.md) 6.1 节，本文写明两侧都要遵守的细节。

## 文件

| 文件 | 内容 |
|---|---|
| `NN_<名字>.yaml` | 一份补丁，作用在随包配置上 |
| `NN_<名字>.expected.json` | 期望的叶子级 `PatchReport`：`leaves`（净变化）、`ops`（每条覆盖或生成步骤命中的行数与叶子数；`values` 给出该步骤自己的叶子 `old`/`new`，`first` 只给第一个）、`warnings`（警告代码）、`passthrough`（只交给 C# 的节点）|
| `errors/<名字>.yaml` + `.expected.json` | 必须报错的补丁，期望里只有错误代码 `{"error": "<code>"}` |

期望值都由独立的查询和算术得出（SQL 读库、numpy 的 float32），不是把 `patch.py` 的输出抄进来。`16_regenerate_island17` 与 `19_regenerate_exception_kept` 的期望由一个只调用 `model.timeline` 规则和 SQL 的脚本生成；`21_base_regenerate_precedence` 的期望是用例 16 的期望去掉 rel→OT 与岛表尾部两个步骤的叶子。

## 叶子

一个叶子是 `(node, row, index)`：

- `node`：`data_dictionary` 的节点名，例如 `CombatBalanceConfig.IslandTargetDays`、`EnemySpawnerConfig.Stats[Health]`；
- `row`：行标识。单个资产（`CombatBalanceConfig`、`AmuletConfig`、`SimulationConfig`）是 `{}`；列表元素是 `{"idx": 列表下标}`；刷怪点是 `{"scene", "file_id"}`（C# 侧经 C1 的身份映射换成层级路径 + Key）；按资产区分的配置是 `{"asset": "MonoBehaviour/xxx.asset"}`，护甲表再加 `idx`；
- `index`：数组字段的元素下标，标量字段为 `null`。

`old`/`new` 是字段的 C# 类型下的值：`float` 写成 float32 扩成 double 后的数，`double` 原样，`int`/枚举写整数，`bool` 写 true/false，字符串与资产引用写字符串或 null。数组变短或变长时，多出或缺少的元素一侧为 null。`leaves` 只列出净变化（补丁前与补丁后不同的叶子）；`ops` 的叶子数包括被选中但值没变的叶子。

## 语义

- 覆盖按顺序执行，每条作用在前一条的结果上；`base` 补丁的覆盖排在最前面（来源记为 `base:<名字>`）。
- 选行：单个资产不用选择器。其它表必须给 `where`（字段等值；枚举也可写成员名，bool 可写 0/1；`where: {}` 表示全部行），列表表也可用 `index` 选列表下标。命中 0 行是错误。
  - 等值按字段的 C# 类型比较（C9 加，C# 比较的是内存里的字段值）：`float` 字段按 binary32（`f32(单元格) == f32(值)`，库里存的 4.4 与 float32 扩成 double 的 4.400000095367432 都命中 4.4f），`double` 按 double，int/bool/枚举按数值；枚举字段也可写成员名，库里没有 `__name` 列的枚举字段也一样（刷怪点的 `WeaponType`）；字符串值永远不匹配 float/double 字段。用例 `22_where_float32`、`errors/where_string_on_float`。
  - 不在 `config` 声明里的选择列（`Key`、`go_name`、`DamageType` 等）照旧：数字按数值、字符串按 `str()` 相等、有 `__name` 列的按成员名。
- 数组字段：`index` 选元素（整数或 `"a:b"`，左闭右开，`"a:"` 到末尾）；不给 `index` 时 `add`/`mul` 作用于每个元素，`set` 需要一个列表并整体替换（长度可以变）。
- 算术一律先用 double 计算，再存成字段类型：`float` 字段 `(float)((double)old + arg)`、`(float)((double)old * arg)`；`double` 字段直接用 double；`int` 字段只接受整数结果（否则报 `bad_type`；NaN、±inf 与溢出成 inf 的结果也是 `bad_type`，用例 `errors/int_nan`、`errors/int_inf`）。`set` 把值换成字段类型（用例 20：`set: 0.1` 存成 float32 的 0.10000000149011612）。结果在字段类型下不是有限数（`1e39` 存成 float、`mul: .inf`）报 `bad_type`；原本存的 inf 做 `add`/`mul` 仍是 inf。`bool`、字符串、资产引用和枚举只能 `set`；枚举的值是成员名或整数值的数（`3`、`3.0` 可以，`true`、`1.5`、不是成员的数都是 `bad_type`，用例 `errors/enum_bool`、`errors/enum_fraction`）；资产引用必须指向存在的资产；插值的 `IslandPoints`/`TimingPoints`/`CoefPoints` 必须是 `;` 分隔的有限数。
- `base` 与本补丁的 `regenerate` 按键合并，本补丁的键优先（用例 21）。
- AB 的 `overridesByLocation`：`keyData`/`valueData` 是两个数组字段。删掉某个键（两个数组同时去掉该元素）后，游戏里该岛回落到 C# 字段初始化器里的类覆盖值，不是 `defaultValue`（Newtonsoft 把资产合并到 `new T()` 的类默认字典上，见 `model/ab.py`），用例 12、13。**C9 的做法**：把 AB 叶子改在 `ABTestDataConfig` 资产对象上，然后重走一遍 JSON 往返（`ABTestDataConfig.BuildDataJson` → `ABTestDataGroup.ApplyRemote`，即 `ABTestInitializer.ApplyDevelopTests` 的路径），再进入 `PrepareSimulationRun`。不能在往返之后直接改运行时字典：那时删掉的键会落到 `defaultValue`（30/180），而不是类默认值（28/150）。
- 节点在 `data_dictionary` 里、但 Python 模型不持有的，原样交给 C#，并给出 `not_python_patchable` 警告（列出读取它的公式）。Python 仍按字典的来源表（`data_dictionary.tables`）解析它的 `where`：字段不存在报 `bad_where`，命中 0 行报 `no_match`，多行表不给选择器报 `bad_selector`；值按该列已存的单元格检查类型（数值、整数、0/1、字符串、JSON 数组）。这类覆盖在 `PatchReport` 里没有叶子，只在 `passthrough` 里列出。
- 只在 Python 里的字段：场景编辑工具 `SpawnerRelativeOptimumTimeTool.Islands[]` 的 `MinOptimumTime`/`MaxOptimumTime`/`IsDays`。游戏只读它写进刷怪点的 `OptimumTime`，所以这些叶子留在 `leaves` 里，但不进 `expanded`，节点列在 `PatchReport.python_only`。

## 生成步骤

`regenerate` 打开的步骤（岛表尾部、rel→OT 与相对窗口、进化岛位与进化时间点、插值节点、进化表）由 Python 按阶段 3/4 的规则重算，结果展开成显式的 `set`，附在 `PatchReport.expanded` 里发给 C#；C# 不实现生成规则。

- 在基线配置里本来就不符合规则的叶子（例外，例如 G03 的 16 个 OT 例外）保留原值；规则给出的新值变了时警告 `exception_kept`。
- 本补丁直接改过的叶子也保留，规则给出不同的值时警告 `override_kept`（用例 16）。
- 规则在补丁后的输入上没有值（例如进化岛位超出第 99 岛），该行的派生叶子保留原值并警告 `rule_error`。
- 岛表尾部（`island_target_days_tail`）：P3 的尾部是岛序号的闭式，不依赖手填段，随包配置的尾部已与它完全一致（G01 84/84），补丁直接写的尾部单元格又按 `override_kept` 保留，所以这一步不会产生任何 `set`。设计 6.1 里“改了手填段就重算尾部”的说法与此不符，交 Z1 处理。
- `evolution_tables: replay_stored_inputs | fitted_h` 整表替换生成行的 Value、CopyValue、ReferenceStatPercent。`SyncShieldValuesByLeader` 打开时，`fitted_h` 按类型分别生成护甲，三种类型不再相同：`evolution_armor_sync: report`（默认）给出 `armor_sync_broken` 警告并列出不一致的 (稀有度, 等级)；写成 `Stabbing`/`Cutting`/`Buckshot` 时以该类型为准同步 Value 与 CopyValue。

## 错误代码与警告代码

错误：`unknown_node`、`bad_patch`、`bad_op`、`bad_type`、`bad_index`、`bad_where`、`bad_selector`、`no_match`、`missing_value`、`bad_ref`。

警告：`out_of_range`（新值超出字典的 min/max）、`not_python_patchable`、`not_modeled_static`（Python 持有但 `derive_static` 在该行不读的字段，只有 C# 模拟会反应：模拟专用字段；`GetTotalCoefficient` 类型 2–5 以外曲线的 `Use`/`CoefPoints`；`evolution_tables: frozen` 时的进化生成杠杆 `MinCopyValueGrowthMultiplier`、`CopyValueTargetShare`、`MinEvolutionValueFromShardsMultiplier`、`MinArmorEvolutionValueFromPreviousEvolutionMultiplier`、`SyncShieldValuesByLeader`；神话进化值，它们只影响 G10 的恒等检查）、`exception_kept`、`override_kept`、`armor_sync_broken`、`no_window`、`rule_error`。

`pipeline.run` 另外给出：`derive_error`（某条规则在补丁后的配置上没有值，单元格为 None）、`ot_exception_input_changed`/`hand_balance_input_changed`/`bake_exception_input_changed`（例外刷怪点自己的叶子变了，或重算出的 OT 规则值、烘焙结果变了，存储值仍保留）、`no_static_effect`（补丁改了叶子，但没有任何派生表或检查变化）。检查项的变化在 `PipelineResult.checks_diff`。

## C9 的核对方式

对每个用例，C# 应用 `PatchReport.expanded`（不跑生成步骤），写出的 `applied_patch.json` 与 `leaves` 去掉 `python_only` 节点后逐叶子相同（`PatchReport.csharp_leaves()`）。`tests/test_config_patch.py::test_expanded_patch_reproduces_the_leaves` 在 Python 一侧做同样的检查。`errors/` 下的用例 C# 也必须拒绝，错误代码相同。

Unity 里的核对是 `tests/unity/test_patcher_parity.py`（`tufu_balance.unity.patcher.compare`：叶子地址相同，旧值、新值的 JSON 类型相同、浮点逐位相同）。C# 一侧的细节（`SimulationConfigPatcher.cs`，运行手册第 18 节）：

- C# 按反射在内存对象上找字段，节点形如 `<根>.<路径>[.<列表>[].<路径>]`；根是 `CombatBalanceConfig`、`AmuletConfig`、`SimulationConfig`、`LocationsConfig`、`ABTestDataConfig`（游戏在用的那一份资产）、`ItemConfig`、`ArmorConfig`（该类型的全部资产，行 `{"asset"}`）、`EnemySpawnerConfig`（场景里全部刷怪点组件，含没注册的，行经 C1 的身份映射写成 `{"scene", "file_id"}`）。`passthrough` 节点只要 C# 字段存在就能改。
- C# 找不到的节点（Python 不认识的、只在 Python 的 `SpawnerRelativeOptimumTimeTool`、刷怪点下的第二层列表 `StatRewards[]` 等）报 `unknown_node`，错误里列出全部这类节点名。C# 在改任何值之前先查完全部节点，所以一份同时有别的错误的补丁，C# 报的可能是 `unknown_node`，Python 报的是排在前面的那条错误。
- `where` 里的名字：字段路径（可带点，如 `data.nonElite.charges.defaultValue`），列表行另有 `idx`，按资产的表另有 `asset`、`asset_name`，刷怪点另有 `scene`、`file_id`、`go_path`、`go_name`、`Stats[<属性>]`/`stat.<属性>`。C# 允许元素类型的任何字段，Python 只允许声明过的字段和选择列；Python 拒绝的补丁不会发给 C#。
- 刷怪点缺某项属性时，`Stats[<属性>]` 的 `set` 在 C# 里也报 `missing_value`（C# 不新建属性；Python 会写出旧值为 null 的叶子）。
- AB 的 `keyData`/`valueData` 改在资产的 `UnitySerializedDictionary` 私有列表上，改完立即按 `OnAfterDeserialize` 重建字典；全部覆盖做完后重走 `BuildDataJson` → `ABTestData.ApplyRemoteAll`，`applied_patch.json` 的 `ab_roundtrip` 记下各组的版本号与生效值（用例 12、13 与 `model.ab` 在补丁后配置上的值逐位相同）。

P12 逐叶子比较时：`passthrough` 节点在 Python 一侧没有叶子，C# 写出的这些节点的叶子单独列出（只核对节点、行数与值类型），不参与逐叶子相等；`python_only` 节点只在 Python 一侧有叶子，同样不参与。
