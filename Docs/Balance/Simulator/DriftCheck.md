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
- `git diff -U0 --no-renames` 的旧行段与引用区间相交才是行段命中。纯插入位于旧行 n 与 n+1 之间，仅在引用内部计入（`lo ≤ n < hi`）；在首行前或末行后插入只计同文件改动。删除按被删旧行计；重命名按旧路径删除、新路径新增处理，避免漏掉旧引用。
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

| 级别 | 累积步骤 |
|---|---|
| L0 | 私有数据库完整抽取；graph build 与目录文档生成；`pytest -m 'not unity'`；完整 cscheck（双编译、签名、禁用 API）；工作区 drift；golden guard；当前基线差异 |
| L1 | L0；C2 差分探针的 Unity pytest 子集（编辑和 Play，严格对 differential 当前基线）；smoke-11；C1 导入；X1–X7；模拟 CSV 当前基线比较 |
| L2 | L1；带 bake_report 的 golden-405；导入与 X1–X7；C8 烘焙报告逐位检查；405 天空补丁双作业对比；模拟 CSV 基线比较；全部 Unity pytest（含棘轮） |

所有 Unity 作业均经现有 launcher，使用提交版本，取共用锁和资产守卫。未提交的 Python 检查运行当前代码，Unity 检查 `--rev`（默认 HEAD）的已提交代码，所以应先提交再验收。刷新不调用 accept、rebaseline-original、bake --apply 或任何资产保存操作。

### 与设计的明确差异及后续交接

1. 6.8 的抽取、graph 和 render_catalog 产物写入刷新私有目录，不覆盖 `out/balance.sqlite`，也不改 Z1 所有的文档。后续检查通过 `TUFU_BALANCE_DB` 使用刚抽取的私有库，共享只读库不打开为可写。LFS 场景通过 `git lfs smudge` 从本工作树的对象生成到私有 assets 覆盖层，其余文件只读链接；不读运行工作树的资产，不依赖运行车道当前检出哪个并行单元。
2. 2.8 的 cscheck 在 6.8 的步骤表中遗漏，已纳入 L0。验收按 7.3 更严格的 60 秒预算记录（2.8 表为 90 秒）。
3. 当前没有 K3 的 `tests/golden/current/sim_run/*.csv`；刷新明确报告 0 个模拟 CSV 基线，而非声称已比较。已有静态与 differential 套件仍严格比较 CURRENT。K3/Z1 提供 CSV 后自动逐字节检查；支持 `sim_run/<preset>/`，也兼容 3.2 的平铺布局。
4. `unity probe` 尚无独立 CLI 子命令；使用 C2 已有的 `pytest --unity tests/unity/test_differential.py -m unity`，会通过 launcher 真正运行编辑和 Play 探针并严格比较当前基线，避免重复启动。未新增该命令或跨单元修改 unity/__main__.py。
5. L2 的全部 Unity 测试包含故意失败的守卫测试；若主检出的用户编辑器正在改真实存档，按 Runbook 的归属规则可能失败，P13 不操作该编辑器。P13 只要求实际验收 L1；L2 的计划及失败传播有单元测试，未声称已实际通过 L2。
6. 原样串行运行完整静态 pytest，实测 162 秒，整次 L0 为 178 秒（888 通过、5 个 perf 跳过）。因此 L0 先用 `pytest --collect-only -m 'not unity'` 收集完整节点，再按模块分批交给最多六个独立 pytest 进程；每个节点恰好执行一次，不排除 slow 测试。耗时的模块夹具集中在同一进程，只有两个等待假编辑器的测试模块拆成两批，以免重复 C# 编译夹具。各进程使用隔离临时目录，共用一个 `TUFU_GOLDEN_RUN`，P1 的文件锁负责同套件结果合并。无需 xdist 或新增环境依赖。该实现是对原设计单进程命令的性能调整，不缩减检查集合；最终仍另跑完整串行 `pytest -q`。
