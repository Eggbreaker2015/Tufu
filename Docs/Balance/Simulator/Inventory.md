# 物品、装备与进化（C6）

实现位于 `SimulationSystem.Inventory.cs`、`SimulationRebuildParams.Inventory.cs` 和 `CombatBalanceConfig` 的自动平衡被调方法。默认使用随包进化表（Frozen）、Chain、Swapped、Timed、岛 4/8 开武器槽 3/4、StatsThenItems × SlotBeforeRpm。份数结构、交叉偏好和后期平台均为 **post-hoc**，不是外部验证。

## 契约与参数

- 快阶段按剩余充能计算 `Chain(n,K1) × (1+K0/K6+[preferred]K7)`；E1 把 Chain 换成 n。K1=1 时显式返回 n，n≤0 返回 0。
- 偏好取当前已解锁岛：Swapped 的武器读 PreferredArmorType，护甲读 PreferredWeaponType；Direct 对调回来。比较映射只使用 K7>0 的日子；掉落系数的消融只使用 K0>0.1 的日子。
- 慢阶段每次清怪是一次 `Roll(item.Key, E(d))`。快阶段先按 Key 累积 double，再按 Key 排序逐物品取整。Carry 和 Seeded 都调用 C3c 的 SimulationRandom；不使用 Unity 随机数。
- 份数经 `PlayerModel.AddItem(..., applySilently:true)` 合并，不触发奖励 UI。未进化的快照 Level 使用 EffectiveLevel（等于 Copies）。进化直接执行真实命令相同的份数扣除和 Level/Evolved 更新，避免调用分析/UI 的进化流程；随后调用 RebuildTraceEvolve。
- Timed 需达到目标日且碎片足够；Greedy 只看碎片，TimedLag 多等一天。TimedForce 只作诊断。最高到 MaxEvolutionLevel，不重复消费封顶表。
- weapon_3/4 只由 UnlockSimulationWeaponSlot 开启，调用 RebuildTraceSlotUnlock。weapon_2 和护甲槽仍归门禁 SlotReward；C6 没有把第 1 天硬编码成解锁日。
- P18 的两个正交字段为 DayChangeInventoryOrder（StatsThenItems / ItemsThenStats）和 SlotRpmOrder（SlotBeforeRpm / SlotAfterRpm）。后者将快阶段新物品的合并、进化和武器槽延迟到换天 RPM 重算之后、强制快照之前；进化仍使用奖励所属的旧日。当天即使没有正物品奖励，也保留延迟的定时进化处理。

Regenerate 只允许 reenter_play 批处理作业，直接改注入的 CombatBalanceConfig。它在目标日捕获同稀有度、前一级物品的最大份数，以 nice 舍入成碎片；按 P5 的生成递推写 Value/CopyValue。首次护甲按对应平坦防御的启发值设基础值；作业末尾以最后一个已生成等级的 Value 填充该系列尾部。没有生成过等级的系列保持原表。相关方法的出处分别标 Inferred 或 Calibrated。

Regenerate 的 H 不是精确恢复：武器使用 P5 的渐近线 `type × rarity × cooldown × stat`，护甲使用 `0.8 × stat`。系数放入参数字段 WeaponHeuristicTypeMultipliers、WeaponHeuristicRarityMultipliers、ArmorHeuristicMultiplier，默认分别为 `[1,0.8,0.573]`、`[1,1.25,1.5,1.875,2.5]`、`0.8`；本单元没有重新拟合这些先验。G07 的验收单独喂入存储 H 与碎片，不把启发值近似误差混进生成器的精确计数。

作业开始保存 JsonUtility JSON；结束路径用 finally 恢复，并清空非序列化的捕获状态。Frozen 不改表。inventory.json 记录作业前、运行后、恢复后的三个 sha256，以及相等判断；退出 Play 后仍由原有资产守卫核对内存和磁盘。

## 输出与下游接口

`outputs.inventory = true` 写 `inventory.json`（`tufu.sim.inventory/1`），Python 的 `unity.inventory` 注册开关。它包含：

- rewards：每次取整的 day、key、phase、expectation、copies；totals：每 Key 累计期望与整数份数。
- headroom：目标日首次处理时的 key、level、target、day、current_level、copies、shards、headroom。day 可能迟于 target；这表示目标日尚未获得物品或先前未触发处理，不能称为目标日的精确观测。
- generator：450 行存储输入生成器探针、35 个早期五稀有度 Chain/E1 样本、岛时间换算样本、X6 装备修饰器排除探针。生成器诊断在克隆的 CombatBalanceConfig 上改表，随后销毁克隆，不改注入的实例。

P10 所需槽数已经由 C3b/C1 写入 snapshots_ext.csv，`simio.ingest` 导入 `sim_snapshots_ext.unlocked_weapon_slots / unlocked_armor_slots`；C6 的验收通过实际运行的 sim.sqlite 读取这些列，不再造另一份槽数表。

## 验收方法

`Balance/tests/unity/test_inventory.py` 一次启动运行 13 个 reenter_play 作业：四个份数组合、正物品奖励的参考冲刷、四个 P18 组合、free、checkpoint、Regenerate、Seeded。开环从第 0 天开始 replay_items=recompute（避免首次覆盖后的份数校正混入 TC4–19 增量），经济缓存 stored，槽数使用真实开槽规则。P18 只比较 TC4–11 的 DpsData；P12 用独立 P8 重算，在 `(3,8)/(4,8)/(4,7)` 候选中仅以 TC1–24 的 DPS 选规则，TC≥25 单列。

逐刷怪点实际清怪次数和真实插值配置独立重算快阶段物品期望；逐次调用独立 Python SimulationRandom 核对所有 Carry/Seeded 整数。正物品参考冲刷与默认冲刷逐字节比较两种快照 JSON、两种旁路 CSV 和 digests.csv。G07 与 P5 和随包表双重比较。

本单元的黄金套件为 sim_inventory。新测量使用 golden accept --seed-original；设计有明确阈值的键声明 original，不把测量值写成设计阈值，不修改既有 ORIGINAL。任何不能满足的阈值另行说明并登记放宽，不能把登记放宽称为原验收通过。

## 所有权偏离

为了实现现有钩子无法表达的行为，修改了 C5 的 FastTick 调用点（E1 选择、按 Key 取整、P18 冲刷和正奖励参考实现）、C3b 的 Run（准备备份、RPM 后生效）和 C0b 的 Harness（所有完成路径恢复、导出）。新增内部成员和 Inventory 自有状态对应更新 C3a 的签名/分部文件断言；原生成员和已恢复的方法体不变。参数资产按合并后的声明顺序补字段；unity/__init__.py 注册新输出。没有修改 Design.md、共享数据库、场景或随包平衡资产。

## C1 文本缺陷的完整摘要确认

首轮 13 作业运行 `868af23b…` 中，默认开环 timechunk_full.json 有一个 CurrentGpm 被写为 `7.0236302039131`，但内存值是 `7.0236302039131004`（差 1 ulp）。逐个保留原始十进制词法文本，交给编辑器同版 Mono 的 double.Parse；只替换这一格后，完整文件的逐位摘要精确等于导出器记录的 `02a4ab5cea7e2542c7822fd1cdb020f5d30afbbf5ce9bb83a9be09ef66e89321`。没有绕过 simio 的检查，没有在测试里代入预言机值凑摘要。

修复 C1 的 SimulationJson.RoundTrips：改用 C3c 已有的精确十进制解析器核对 R/G17 文本，不再信任 Mono 的 double.TryParse。仍保留原来的 R/G17 文本选择，不整体改写为另一种格式。新增离线测试用 .NET 和编辑器同版 Mono 运行真实 SimulationJson，再用 Python 的 IEEE 解析器核对 4,000 个随机 double 与边界、回归样本的位。工具所需的 SimulationJson 源码链接与 export-format 操作属于此次必要的跨单元修复。

资产守卫会遍历 HashSet 的内部版本号；只做 Clear 仍会改变内存摘要。捕获集合在结束时换成全新的空集合，恢复完整状态，而不只是元素内容。验收还要求每个作业的 asset_guard.after_job.changed_in_memory **不含 Combat Balance Config**，不能只凭 JsonUtility 字符串相同就宣称内存恢复。
