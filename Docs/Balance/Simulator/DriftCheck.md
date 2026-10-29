# 代码漂移检查与一键刷新（P13）

## 漂移检查

在本工作树的 `Balance/` 运行：

```bash
python -m tufu_balance.drift --base 6af011f --head HEAD
python -m tufu_balance.drift --head worktree --strict
python -m tufu_balance.drift --repo /Users/joyfort/MyProjects/Tufu-stage5 --head feature/pet-system --out out/drift/pets-tip
```

默认输出 `out/drift/<目标提交前12位>/report.md`、`drift.csv`、`report.json` 和 `signature.json`。工作区输出目录为 `out/drift/worktree/`。相同目标会覆盖报告，不改目录、基线或源代码。`--repo` 只读 git 对象或该仓库的目录文件，不切换分支；历史版本的 C# 输入从 git archive 临时展开在报告目录，调用 P0 的 `cscheck --signature --worktree <临时目录>`。工作区模式直接检查该工作树。

- 文件范围是 `Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/**/*.cs`。工作区模式比较 base 与工作区最终内容，包括已提交、暂存、未暂存、删除，以及未跟踪的新 C# 文件。
- `code_refs` 支持单行、闭区间和逗号分隔的多个区间；有完整仓库前缀或 `CodeBase/` 前缀也能解析。先移去错误的开头 `../`，再规范化并确认文件存在于 base 的 CodeBase 目录；无法解析的单列，不能静默丢掉。
- 行号全部属于 `--base`，不使用目标版本的行号。默认 base 是目录引用对应的 `6af011f`；如另选 base，调用者须保证引用行号也对应它。
- `git diff --no-color -U0 --no-renames` 的旧行段与引用区间相交才是行段命中，不受 `color.ui=always` 影响。两端均存在的已修改文件如果没有可解析行段（包括仅模式变化、二进制差异），抛出 ValueError，CLI 返回 2，避免静默降级为仅文件命中。纯插入位于旧行 n 与 n+1 之间，仅在引用内部计入（`lo ≤ n < hi`）；在首行前或末行后插入只计同文件改动。删除按被删旧行计；重命名按旧路径删除、新路径新增处理，避免漏掉旧引用。
- 无行号的引用标为 `file_unbounded`，只计文件级，不假装精确命中某个公式。一个公式的多个引用合并计一次。
- 报告列出公式状态、子系统、当前 Python registry 的实现、目标版本的 C# `[SimulationRebuild]` 成员；未登记明确写出。DP/X/G 重跑建议按子系统保守展开，不等于逐公式覆盖证明。
- 默认命中只报告；`--strict` 有任意文件级命中返回 1。签名锁失败无论是否 strict 都返回非零；工具或 git 错误返回 2。无法解析引用单独报告，不等同于已证明命中。

## 宠物分支实测（2026-10-28）

通过共享仓库的 git 对象读取，未进入主检出，未切换或修改宠物分支。设计提交 `3dd8b210` 的时间为 `2026-10-28 02:59:06 -0700`；用 `git log feature/pet-system -1 --before=<该时间>` 得到当时的分支提交 `72d5640a7d2516d128a79adc9f111c2e7f8491c3`。当前 tip 为 `ec3058aad01b0b2bf2354d9597595c7ee55bfc7d`。

| 目标 | 改过的 C# 文件 | 文件级公式 | 行段级公式 | 仅文件级公式 | ../ 修复引用 | 无法解析引用 |
|---|---:|---:|---:|---:|---:|---:|
| 设计时的 `72d5640` | 82 | 105 | 41 | 64 | 8 | 0 |
| 当前 `ec3058a` | 164 | 132 | 48 | 84 | 8 | 0 |

两个历史目标都实际调用了 P0 的签名锁，均为 0 项违规。

设计时按状态：`recovered_native 71 / reconstructed 26 / data_only 4 / stub 4`；按子系统：`combat 40 / powerups 29 / stats 14 / spawners 9 / items 7 / simulation 4 / meta 2`。与 6.7 完全相同。当前按状态为 `recovered_native 79 / reconstructed 42 / data_only 6 / stub 4 / editor_only_missing 1`；按子系统为 `combat 40 / powerups 30 / stats 14 / spawners 10 / items 22 / meta 9 / simulation 7`。差额来自宠物分支继续开发，不是放宽 105 的验收。

## 刷新

```bash
python -m tufu_balance.refresh --level 0 --dry-run
python -m tufu_balance.refresh --level 0
python -m tufu_balance.refresh --level 1 --rev HEAD
python -m tufu_balance.refresh --level 2 --rev HEAD
```

`--dry-run` 列出全部命令，不创建目录或启动 Unity。实际输出 `out/refresh/<UTC时间戳>/summary.md`、`summary.json` 和每步的 `.log`；`--out` 可选，但必须是本工作树 `Balance/out` 内尚不存在的新目录。逐步执行，失败立即停止并保留原退出码；后续未执行的步骤也列在汇总中。CURRENT 的内容哈希每步前后核对。

`summary.json` 的总计和每个步骤在 `seconds` 旁记录 `load_start`、`load_end`：包含 1/5/15 分钟的 `load_average` 和 `cpu_count`，用于解释共享机器负载下的耗时；不因负载自动放宽或改变退出码。旧验收报告没有采集负载，不能事后补造。

| 级别 | 累积步骤 |
|---|---|
| L0 | 私有数据库完整抽取；graph build 与目录文档生成；`pytest -m 'not unity'`；完整 cscheck（双编译、签名、禁用 API）；工作区 drift；golden guard；当前基线差异 |
| L1 | L0；C2 差分探针的 Unity pytest 子集（编辑和 Play，严格对 differential 当前基线）；smoke-11；C1 导入；X1–X7；模拟 CSV 当前基线比较 |
| L2 | L1；带 bake_report 的 golden-405；导入与 X1–X7；C8 烘焙报告逐位检查；405 天空补丁双作业对比；模拟 CSV 基线比较；全部 Unity pytest（含棘轮） |

所有 Unity 作业均经现有 launcher，使用提交版本，取共用锁和资产守卫。未提交的 Python 检查运行当前代码，Unity 检查 `--rev`（默认 HEAD）的已提交代码，所以应先提交再验收。刷新不调用 accept、rebaseline-original、bake --apply 或任何资产保存操作。

### 与设计的明确差异及后续交接

1. 6.8 的抽取、graph 和 render_catalog 产物写入刷新私有目录，不覆盖 `out/balance.sqlite`，也不改 Z1 所有的文档。后续检查通过 `TUFU_BALANCE_DB` 使用刚抽取的私有库，共享只读库不打开为可写。LFS 场景先用 `git lfs ls-files --json` 按路径和指针 OID 确认 `downloaded=true`；对象不在本地时明确报错，绝不下载。随后 `git lfs smudge` 仅从本地对象生成到私有 assets 覆盖层，并把 LFS URL 限定为本次输出目录中不存在的 `file://` 端点、关闭远端自动探测，使检查后对象消失也不能回退到网络。其余文件只读链接；不读运行工作树的资产，不依赖运行车道当前检出哪个并行单元。
2. 2.8 的 cscheck 在 6.8 的步骤表中遗漏，已纳入 L0。验收按 7.3 更严格的 60 秒预算记录（2.8 表为 90 秒）。两处预算冲突及负载影响交 Z1 统一，不将实测超时自动改成通过。
3. 当前没有 K3 的 `tests/golden/current/sim_run/*.csv`；刷新明确报告 0 个模拟 CSV 基线，而非声称已比较。已有静态与 differential 套件仍严格比较 CURRENT。K3/Z1 提供 CSV 后自动逐字节检查；支持 `sim_run/<preset>/`，golden-405 也兼容 3.2 的平铺布局（405 天基线不能当作 11 天基线）。
4. `unity probe` 尚无独立 CLI 子命令；使用 C2 已有的 `pytest --unity tests/unity/test_differential.py -m unity`，会通过 launcher 真正运行编辑和 Play 探针并严格比较当前基线，避免重复启动。未新增该命令或跨单元修改 unity/__main__.py。
5. L2 的全部 Unity 测试包含故意失败的守卫测试；若主检出的用户编辑器正在改真实存档，按 Runbook 的归属规则可能失败，P13 不操作该编辑器。P13 只要求实际验收 L1；L2 的计划及失败传播有单元测试，未声称已实际通过 L2。
6. 原样串行运行完整静态 pytest，实测 162 秒，整次 L0 为 178 秒（888 通过、5 个 perf 跳过）。因此 L0 先用 `pytest --collect-only -m 'not unity'` 收集完整节点，再按模块分批交给最多六个独立 pytest 进程；每个节点恰好执行一次，不排除 slow 测试。C# 编译夹具集中在同一进程，等待假编辑器的两个模块、参数加载及静态流水线模块各拆成两批。各进程使用隔离临时目录，共用一个 `TUFU_GOLDEN_RUN`，P1 的文件锁负责同套件结果合并。无需 xdist 或新增环境依赖。该实现是对原设计单进程命令的性能调整，不缩减检查集合；最终仍另跑完整串行 `pytest -q`。
7. 明确偏差：为避免基于不完整的前序产物继续检查，保留 fail-fast，前序失败后不再运行 `baseline-smoke-11` 或 `golden-current-differences` 等后续只读比较。因此失败的 L1 不保证完成 2.8 的当前基线比较；汇总列出未执行步骤并写明「前序步骤失败，未比较」，保留首个失败退出码。未将这些比较改成失败后的 report-only 步骤。

## 验收记录

命令均在本单元 `Balance/` 下，用 `/Users/joyfort/MyProjects/Tufu-stage5/Balance/.venv/bin/python`，常规回归的 `TUFU_BALANCE_DB` 指向共享只读库；refresh 自行生成并使用私有库。Unity 验收提交为 `aa29634`，后续仅调整 Python 的测试调度、CSV 基线选择和本说明，C# 及配置资产与此提交相同。

| 验收项 | 结果与证据 |
|---|---|
| 临时 git 历史的文件/行段命中、../ 与无法解析引用 | PASS：`pytest tests/test_drift.py tests/test_refresh.py` 对应 32 个测试全部在完整回归里运行；含插入边界、删除、重命名、暂存/未暂存/未跟踪文件、逗号区间 |
| 宠物分支计数 | PASS：`drift --repo /Users/joyfort/MyProjects/Tufu-stage5 --head 72d5640 --out out/drift/pets-design` 为 105/41；`--head feature/pet-system --out out/drift/pets-tip` 为 132/48；两个签名锁均 0 违规 |
| 签名改动检测 | PASS：临时 C# 副本注入公开成员 `P13SignatureViolation`，实际调用 P0 CLI 返回 1，检出访问级别和命名前缀两项违规；正式工作树 0 违规 |
| L0 dry-run | PASS：`refresh --level 0 --dry-run` 列出 8 步；单元测试确认各级完整计划以及不创建目录 |
| L0 实际 ≤60秒 | PASS：`refresh --level 0 --out out/refresh/accept-l0-final` 退出 0，59.646 秒；抽取 10.21 秒、pytest 45.27 秒（含收集/调度）、cscheck 2.27 秒、drift 1.62 秒；897 个非 Unity 节点全部分配一次，892 通过、5 个 perf 跳过 |
| L1 实际跑通 | FAIL（集成问题）：`refresh --level 1 --rev aa29634 --out out/refresh/accept-l1` 实际执行，137.114 秒后在 X2 返回 1；编辑/Play 差分探针 14 通过（30.40 秒 pytest / 30.84 秒步骤），smoke-11 Unity 退出 0（16.31 秒步骤），导入退出 0 |
| 失败传播、CURRENT 不变 | PASS：进程失败、缺失可执行文件和 CURRENT 意外改动的故障注入均非零；上述实际 L0/L1 的 `summary.json` 均记 `current_unchanged = true`；无 accept/rebaseline/write-back 命令 |
| 完整 cscheck | PASS：双编译 0 错误；警告 3707/8（既有基线）；无 UNITY_EDITOR 编译 0 新错误；签名 0、禁用 API 0，工具耗时 2.2 秒 |
| 黄金守卫 | PASS：`python -m tufu_balance.golden guard`，19 个套件、0 违规 |
| 最终完整串行回归 | PASS：`python -m pytest -q --durations=10 --basetemp out/pytest-final`，891 通过、276 跳过（270 个 Unity、6 个 perf），169.80 秒；无失败 |
| 冻结夹具与共享库 | PASS：`python tests/golden/fixtures/generate.py --check` 输出 `identical`（另有既有的 provenance 提示）；共享库仍为 444，SHA-256 `a7a2e3c5d048c57f9a77d3057dd0c35ed30cf799fe6612529961c44a18e3e098` |

### L1 的明确阻塞及后续交接

真实 smoke-11 作业位于 `out/sim/runs/eb20666bfdbec38df0870df3e95e63898ae2971519efb3f584d404adf0ff0539/smoke-11/`，诊断为其中的 `crosscheck/X2.csv` 与 `crosscheck/summary.json`。X1 8280 项、X3/X7 各 360 项、X5 11 项、X6 152 项，均 0 不符；X4 因此预设未打开 bake_report 而明确 vacuous。X2 690 项里有 75 项不符，全部是 `Snapshots` 的 `slow_tick` 的 `DpsData`，涉及 15 个快照、每个五种伤害类型；没有天快照或 DefenseData 不符，也没有导出文本摘要缺陷。

例如 `Snapshots[1]` 的 Cutting DPS：C# `9.23076956935184`，由同一快照属性重算为 `10.000000366797828`。现有 `Tick` 的顺序是 `CalculateSimulationData` → 战斗/奖励 → `MakeSnapshot`，且此集成点的 `ProcessInventory` 仍为空；这个模式与战前缓存、战后属性一起入快照的解释一致，但应由 C3b/C6/K3/P11 核对缓存的预期语义，不能仅凭此断言 C6 一定修复。P13 未修改引擎或交叉检查，未把完整策略换成 null_policy，未放宽 X2，也未接受新基线。

启动器的两个成功运行都通过沙箱/资产守卫。smoke 作业 `after_job.ok = true`、`not_restored = []`、`changed_on_disk = []`；通过 `unity status` 确认运行工作树 `clean = true`，锁为空，无 Unity 进程或常驻服务。本轮失败是 Python X2 检查，不是用户主编辑器活动导致的真实存档守卫失败。

Z1 应把上述性能实现差异、宠物分支移动及 L1 阻塞折回设计；K3 提供按预设分目录的模拟 CSV 基线（或平铺 golden-405 基线）后，再运行 L1/L2。L2 仅做了计划/失败传播测试，本单元没有实际验收 L2。

## 复核修正记录

本轮仅修改 P13 的五个所属文件，不改变引擎、C#、基线、运行预设或刷新顺序。

- 退出码保护：通过 `main` / `run_step` 测试静态 shard 的首个非零码、pytest worker、launcher、crosscheck、`--step` 的 ValueError 和总入口的 execute 结果；对复核指出的六处退出码变异逐一注入，6/6 均由对应断言杀死。脚本与日志在 `out/fix-round/check_mutations.py`、`mutations.json`、`mutation-*.log`。
- 漂移：增加 `color.ui=always` 和两端均存在却无行段的临时历史测试，保留空文件新增/删除的合法情况。强制颜色的真实 `--head 72d5640` 复跑仍为 105/41；当前 `feature/pet-system`（仍为 `ec3058a`）为 132/48；均 8 个规范化引用、0 无法解析、签名 0 违规。记录在 `out/fix-round/drift-design-color/` 与 `drift-tip/`。
- LFS：测试本地对象缺失时在 smudge 前失败、对象存在时正常抽取、离线 smudge 失败时停止；路径与 OID 必须匹配库存。未下载、删除或修改共享 LFS 对象。
- 负载与偏差：汇总的总耗时和每步耗时均带开始/结束负载及 CPU 数；保留上述第 7 条 fail-fast 偏差，60/90 秒预算冲突交 Z1。
- `pytest -q tests/test_drift.py tests/test_refresh.py --basetemp out/fix-round/pytest-p13`：46 通过，3.60 秒。完整 `pytest -q --durations=10 --basetemp out/fix-round/pytest-full`：905 通过、276 跳过（270 Unity、6 perf），158.16 秒，日志 `out/fix-round/pytest-full.log`。
- `tests/golden/fixtures/generate.py --check`：identical（保留既有 provenance 提示）；`golden guard`：19 个套件、0 违规。`refresh --level 0 --dry-run` 仍列出 8 步。
- 实际 `refresh --level 0 --out out/refresh/fix-round-l0`：8 步全部退出 0，80.219 秒，CURRENT 未变。功能检查 PASS，本轮 7.3 的 ≤60 秒预算 FAIL（仍在 2.8 的 ≤90 秒内，不替换原预算）；10 CPU，总计开始负载 1/5/15 分钟为 `6.65/8.08/10.27`，结束为 `40.63/17.72/13.69`。`summary.json` 保存原始精度和每步负载；抽取 9.784 秒、静态测试 62.019 秒。911 个节点分到 39 个 shard、恰好各一次，906 通过、5 perf 跳过、0 失败/错误。未为追求 60 秒重复运行或缩减检查。
- 此次 L0 完整 cscheck：双编译 0 错误、既有警告 3707/8；无 UNITY_EDITOR 编译 0 错误、0 新错误；签名 0 违规、禁用 API 0，步骤耗时 4.527 秒。前后 `tests/golden/current/` 内容哈希完全一致，共享库仍为 444、SHA-256 `a7a2e3c5d048c57f9a77d3057dd0c35ed30cf799fe6612529961c44a18e3e098`；记录在 `out/fix-round/integrity-{before,after}.json`。

本轮未再次启动 Unity；原 L1 的 X2 阻塞及 L2 未实际运行的状态不变，仍按上节交接处理。
