# cscheck：离线 C# 检查

设计见[阶段 5 设计稿](../../../Docs/Balance/Simulator/Design.md) 1.6 节（单元 P0）。.NET 8 控制台工具，用 Roslyn 离线完成编译检查、签名锁和禁用 API 检查，不需要 Unity，不占 Unity 锁。

## 用法

在工作树的 `Balance/` 下执行：

```bash
.venv/bin/python -m tufu_balance.cscheck                       # 全部四步
.venv/bin/python -m tufu_balance.cscheck --signature --bans    # 只跑选中的步骤
.venv/bin/python -m tufu_balance.cscheck --generate-signature  # 从 6af011f 重新生成 signature_6af011f.json
.venv/bin/python -m tufu_balance.cscheck --update-baseline     # 接受当前的警告数与无宏编译的错误集合
```

| 步骤 | 选项 | 内容 |
|---|---|---|
| 编译 | `--compile` | 用 `.rsp` 编译 `Assembly-CSharp` 和 `Assembly-CSharp-Editor`，要求 0 错误，各警告编号的条数不超过 `baseline.json` |
| 无宏编译 | `--no-editor-define` | 去掉 `UNITY_EDITOR*` 宏再编译 `Assembly-CSharp`，错误集合不得超出 `baseline.json` |
| 签名锁 | `--signature` | 与 `signature_6af011f.json` 比较三个类的成员 |
| 禁用 API | `--bans` | 重建代码里不许用随机、时间和墙钟 |

不带选项时四步都跑。单元工作树（单元工作树没有 Library）用开发工作树的解释器 `/Users/joyfort/MyProjects/Tufu-stage5/Balance/.venv/bin/python`，检查的是自己的源文件。其它选项：`--worktree` 指定被检查的工作树（默认是 `tufu_balance` 所在的这个），`--run-worktree` 或环境变量 `TUFU_RUN_WORKTREE` 指定运行工作树（默认 `/Users/joyfort/MyProjects/Tufu-stage5-run`），`--json` 把完整结果写到文件。退出码：0 通过，1 有违规，2 工具无法运行。

第一次运行时 Python 会用 `dotnet build -c Release -p:RestoreLockedMode=true` 编译工具（按 `packages.lock.json` 还原，Roslyn 固定为 `Microsoft.CodeAnalysis.CSharp 4.10.0`，与 Unity 6000.6.3f1 自带 `DotNetSdk 8.0.318` 的编译器同版本）；之后源文件没变就直接运行 `bin/Release/net8.0/cscheck.dll`。`bin/`、`obj/` 不入库。

也可以直接调用工具：`dotnet bin/Release/net8.0/cscheck.dll check|signature|sources --worktree <工作树> --run-worktree <运行工作树> ...`，`sources` 打印每个源文件归属的程序集。

## 编译用的输入

- **`.rsp`**：取运行工作树 `Recovery/StudyProject/Library/Bee/artifacts/*/Assembly-CSharp.rsp` 与 `Assembly-CSharp-Editor.rsp`（有多个 dag 目录时取最新的）。从中读出引用、宏、`-langversion`、`/unsafe`、`/nullable`、`/nowarn`、`/warn`、`/warnaserror`、`/checked`。相对路径的引用（`Assets/Plugins/*.dll`、`Library/PackageCache/...`、`Packages/com.unity.pipeline/...`、`Library/Bee/artifacts/*/*.ref.dll`）按运行工作树的工程目录解析。读不懂的选项列在结果的 `rsp.ignored_options` 里，当前为空。
- **源文件**：`.rsp` 里的源文件列表是运行工作树上次编译时的状态，路径相对于 Unity 工程。被检查的工作树可能增删了文件，所以不直接用这个列表，而是在被检查工作树的 `Assets/` 下按 Unity 预定义程序集的规则重新归类：
  - 跳过以 `.` 开头、以 `~` 结尾、名为 `cvs`、以 `.tmp` 结尾的文件和目录（Unity 的隐藏资源规则）；
  - 所在目录或上级目录有 `.asmdef`/`.asmref` 的脚本属于那个程序集；
  - `Assets/Plugins`、`Assets/Standard Assets`、`Assets/Pro Standard Assets` 下的脚本属于 `*-firstpass`；
  - 路径中有名为 `Editor` 的目录的脚本属于编辑器程序集；
  - 其余属于 `Assembly-CSharp`。

  这套规则在运行工作树上与 `.rsp` 的列表逐个相同（验证时运行工作树检出 C0a 的 `5c07fed`：`Assembly-CSharp` 1647 个、`Assembly-CSharp-Editor` 10 个；测试 `test_membership_reproduces_rsp_on_run_worktree`）。结果的 `sources` 列出被检查工作树相对 `.rsp` 新增和删除的文件，只作提示。本工程没有 asmdef 和 firstpass 脚本，出现这两类时没有对应的 `.rsp`，检查判失败。源文件按 `.rsp` 的顺序排列，新文件排在最后。
- **编辑器程序集**引用的是这次编译出的 `Assembly-CSharp`（Roslyn 的编译引用），不是 Unity 上次留下的 `Assembly-CSharp.ref.dll`，所以单元新加的运行时类型在编辑器代码里也能用。
- **Packages**：被检查工作树的 `Packages/manifest.json`、`packages-lock.json` 与运行工作树不同，或运行工作树的这两个文件比 Unity 上次编译新（以 `.rsp` 与 `Library/Bee/tundra.log.json` 中较新的修改时间为准），就报工具错误，提示先在运行工作树里让 Unity 编译一次。`.rsp` 引用的 DLL 缺失也报工具错误。
- **不运行分析器和源生成器**：`.rsp` 里的 8 个 `-analyzer` 不加载。它们只产生 `UAC*` 警告（Unity 序列化分析），不影响编译结果；`.rsp` 的 `/additionalfile` 也只给分析器用。

## 与 Unity 的对照

运行工作树在 `5806ec7` 上第一次打开（W0）的日志里有 7448 行 CS 警告，每条警告打印了两次（编译时一次，结束汇总一次）。去重后 `Assembly-CSharp` 3713 条、`Assembly-CSharp-Editor` 8 条。cscheck 在同样的源文件上得到同样的 3713 + 8 条，按（文件、行、列、编号、消息）逐条比较完全相同，0 错误：

| 编号 | Assembly-CSharp | Assembly-CSharp-Editor |
|---|---|---|
| CS0649 | 2339 | 8 |
| CS0169 | 1343 | |
| CS0414 | 10 | |
| CS0618 | 10 | |
| CS4014 | 6 | |
| CS0105 | 4 | |
| CS8632 | 1 | |

这些计数存在 `baseline.json` 的 `warnings` 里。某个编号的条数超过基线就判失败（对应"0 新警告"）；条数减少不报错，可以用 `--update-baseline` 收紧。

## 不带 UNITY_EDITOR 的编译

近似播放器构建：

- 去掉 `UNITY_EDITOR`、`UNITY_EDITOR_64`、`UNITY_EDITOR_OSX` 三个宏；
- 去掉编辑器专用的引用，共 72 个：文件名以 `UnityEditor` 开头的 70 个、`Unity.Pipeline.Editor.ref.dll`，以及 `.meta` 里只对 Editor 平台启用的插件 `Unity.Collections.dll`。

其余宏（包括平台宏）不变。在 `5806ec7`/`3dd8b21` 的源文件上结果是 **0 错误**，所以基线的错误集合 `baseline.json` 的 `no_editor_errors` 为空。错误按"文件 | 编号 | 消息"（不含行号）做多重集比较，新出现的错误判失败，消失的错误列在 `fixed_errors`。

## 签名锁

`signature_6af011f.json` 由工具直接从 git 对象读出 `6af011f` 的源文件生成（`--generate-signature`，不需要检出），用运行工作树 `.rsp` 的引用和宏建出语义模型，连续生成两次逐字节相同。覆盖：

- 三个类型 `Codebase.Simulation.Systems.SimulationSystem`、`Codebase.Simulation.Systems.SpawnerData`、`Codebase.Simulation.SimulationConfig`，以及它们的全部嵌套类型（递归；目前只有私有结构 `SimulationSystem+FastTickChunkStatReward`）；
- 类型本身：种类、可访问性、修饰符、基类、接口、类型参数、特性；
- 每个成员：字段（类型、常量值、`readonly`/`static`/`const`）、属性（类型、各访问器的可访问性）、事件、方法和构造函数（返回类型、ref 种类、类型参数与约束、每个参数的名字、类型、ref 种类、`params`、默认值）、嵌套类型；以及各自的特性（如 `[Inject]`、`[Range(1, 500)]`、`[FormerlySerializedAs]`）。元组带元素名显示，例如 `(Codebase.Enemy.EnemySpawner A, Codebase.Enemy.EnemySpawner B)`；枚举默认值写成成员名，例如 `Codebase.Player.Inventory.Rarity.Rare`；隐式的无参构造函数也在锁内。
- 不锁：`SimulationRebuild` 特性（填空方法要加它）、`partial` 关键字、成员所在的文件（partial 拆分会移动成员）、属性访问器方法、编译器生成的后备字段、隐式静态构造函数（给 `Rebuild*` 静态字段加初值会产生它），以及 lambda 的闭包类型（它们不是源码符号，随方法体变化）。反编译源码里以普通嵌套类型出现的编译器生成类型（`_003C...` 一类名字）会按普通嵌套类型锁住，这三个类里没有这种类型。

检查规则：

| 规则 | 违规 |
|---|---|
| `member_removed` | 锁里的成员不见了（含改了参数类型，那会同时变成"删除 + 新增"）|
| `member_changed` | 同一成员的任何一项变了，如返回类型、默认值、可访问性、特性 |
| `type_changed`、`type_removed` | 锁住的类型本身变了或不见了 |
| `new_member_accessibility` | 新成员不是 `private` 或 `internal` |
| `new_member_name` | 新成员的名字不以 `Rebuild` 开头 |

新增的 `Rebuild*` 嵌套类型内部的成员不再检查。

## 禁用 API

"重建代码"的范围：

- **整个文件**：`CodeBase/Simulation/Rebuild/` 下的全部文件；`CodeBase/Simulation/Systems/` 下以 `SimulationSystem` 开头的全部 `.cs`（包括原文件 `SimulationSystem.cs` 和各 partial 模块文件）；以及其它任何声明了 `SimulationSystem` 一部分的文件。原文件里已恢复的方法体也在范围内，它们本来就不用这些 API。
- **单个成员**：整个 `Assembly-CSharp` 里带 `[SimulationRebuild]` 的成员（例如 `SpawnerData.cs` 和 `SimulationConfig.cs` 里填过的方法）；`SpawnerData`、`SimulationConfig` 里名字以 `Rebuild` 开头的成员。成员内部嵌套的内容一并检查。其它类型里名字以 `Rebuild` 开头的方法（UI 里有 15 个，如 `ViewContainer.Rebuild`）不算重建代码。
- `Harness/` 不在范围内，心跳和帧预算的 `Stopwatch` 放在 `SimulationHarness` 里（设计稿 1.5 C# 第 12 条）。

禁用的符号（按语义模型解析，别名 `using R = UnityEngine.Random;`、`using static` 都能识别）：

- `UnityEngine.Random`、`UnityEngine.Time`、`System.Diagnostics.Stopwatch` 的任何使用；
- `System.DateTime.Now`/`UtcNow`/`Today`、`System.DateTimeOffset.Now`/`UtcNow`、`System.Environment.TickCount`/`TickCount64`。

`DateTime` 本身不禁：沙箱时钟要用 `new DateTime(...)` 和日期运算，禁的是读墙钟。同一行同一个禁用类型只报一次（如 `Stopwatch.StartNew().ElapsedTicks`）。

## 耗时

本机（Apple Silicon），四步并行：

| 情形 | 耗时 |
|---|---|
| 一次完整检查（工具已编译）| 约 1.8 秒（结果内 `total_ms` 约 1700：载入 70 ms、解析 150 ms、编译 Assembly-CSharp 约 1.5 s，其余步骤与之并行）|
| 删掉 `bin/`、`obj/` 后的第一次（含 `dotnet build`，NuGet 缓存已有）| 约 3 秒 |
| `tests/test_cscheck.py` 全部 37 个测试 | 约 8 秒 |

## 测试

`tests/test_cscheck.py`，在 `Balance/` 下用 `.venv/bin/python -m pytest tests/test_cscheck.py`。所有改动都在临时目录里的源文件副本上做，不碰任何真实工作树：

- 当前工作树：四步通过，警告与基线相同，耗时 ≤ 30 秒；
- 签名锁连续生成两次逐字节相同，并与入库文件相同；覆盖默认值、元组、嵌套类型、特性、常量；
- 构造的改动逐个被抓到：改返回类型、改默认值、新增 public 成员（即使以 `Rebuild` 开头）、新增不以 `Rebuild` 开头的成员、删除成员；在新的 `Rebuild/` 文件、partial 文件 `SimulationSystem.Fight.cs`、`SpawnerData` 的 `Rebuild*` 成员和带 `[SimulationRebuild]` 的方法里用 `UnityEngine.Random`、`UnityEngine.Time`、`DateTime.UtcNow`/`Now`、`DateTimeOffset.Now`、`Stopwatch`、`Environment.TickCount`；
- 不误报：加 `partial`、把方法移到 partial 文件、新增 private/internal 的 `Rebuild*` 成员、`SimulationRebuild` 特性、`Harness/` 里的 `Stopwatch`、UI 类型里的 `Rebuild*` 方法、`new DateTime(...)`；
- 编译：新文件被编进对应程序集；编译错误、新增警告、在 `#if UNITY_EDITOR` 外用 `UnityEditor` 导致的播放器编译错误都被抓到；编辑器程序集能用新加的运行时类型；
- 程序集归属规则（隐藏目录、`~`、嵌套 `Editor`、firstpass、asmdef、删除文件）；运行工作树上与 `.rsp` 列表一致；Packages 不同时报工具错误。

需要 `dotnet` 和运行工作树 Library 里的 `.rsp`，缺少时整个文件跳过。
