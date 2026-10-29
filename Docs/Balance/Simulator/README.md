# 阶段 5 数值模拟器

模拟器可用于同一代码与参数下的配置对比，也可用于已验证的静态重算、差分探针与烘焙一致性检查；它不能作为真实玩家闭环轨迹的可靠预测。K3 的外部验证 T19 Health 未通过：Carry 在 1%/5% 内为 138/383，随包 A 为 162/469；seed-1 为 171/386，seed-2 为 120/386。Damage 通过；T20、岛 2 和岛 4 通过。T03、T04、T06、T09、T12、T14、T16、岛 3 和早期物品有失败。详细判据与种子结果见 [Calibration.md](Calibration.md)。标为 **calibrated** 的结果是针对确定性 Carry 运行校准，不代表随机种子或闭环可信度已验证；T05/T07/T15 的已冻结棘轮也只适用于 Carry。

在独立开发工作树中提交代码；Unity 只通过启动器在专用运行工作树运行并串行持锁。环境、沙箱、退出码和用法见 [Runbook](Runbook.md)。从 `Balance/` 使用共享数据库的只读路径（需要写入时复制到自己的 `out/`）：

```sh
export TUFU_BALANCE_DB=/Users/joyfort/MyProjects/Tufu-stage5/Balance/out/balance.sqlite
PY=/Users/joyfort/MyProjects/Tufu-stage5/Balance/.venv/bin/python
$PY -m tufu_balance.refresh --level 2 --rev HEAD
$PY -m tufu_balance.diff patches/examples/island10_later.yaml --skip-sim
```

后者只做静态 what-if；去掉 `--skip-sim` 才会运行 Unity。阅读 `out/diff/.../report.md` 及 CSV：补丁前后差值属于同一模型的变化，`model_floor.csv` 是基线与场景的固有残差，不是补丁效果。细节见 [DiffTool.md](DiffTool.md)。

## 覆盖范围

`registry.coverage()` 静态扫描：目录 350 项，Python 登记 98 个实现、覆盖 75 个公式（port 59、rule 16、data 9、inferred 9、fit 5）；C# 87 个标注、54 个公式（Inferred=45、Calibrated=19、Null=12、DebugOnly=11、Native=0）。两侧合计覆盖 121 个公式，pending 和未知 ID 均为零。数字是声明覆盖，不代表所有分支或科学目标通过。目录里新提升的规则未建立依赖图边，影响分析不会自动包含这些关系。

Provenance 的整数为 0 Inferred、1 Calibrated、2 Null、3 DebugOnly、4 Native。C# 标注涵盖重建方法；已恢复的 Native 和无原生标注的 Reconstructed 原有方法并不因未标注而自动计入。静态扫描不等于运行时清单：C1 没有生成 `rebuild_coverage.json`，因此这里没有运行时文件汇总；需补充该导出并与实际作业清单核对。未覆盖的领域包括真实玩家行为、状态 B、宠物和未模拟的系统；空检与模型限制见 [CrossCheck.md](CrossCheck.md)。
