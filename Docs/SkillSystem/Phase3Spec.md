# 技能系统第三期规格：位移、引导、技能等级、编辑器工具

状态：开工规格（2026-10-29）。前置：[SkillSystemDesign.md](SkillSystemDesign.md) 第 1～11 节、[Phase2Spec.md](Phase2Spec.md)、[PhaseCSpec.md](PhaseCSpec.md)。
路径缩写：`CB/` = `Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/`，`A/` = `Recovery/StudyProject/Assets/`。

## 0. 范围

本期做：
1. 位移类效果：击退、冲锋、闪现（以及拉近）。
2. 引导技能。
3. 技能等级随宠物等级和进化成长。
4. 编辑器里的技能编辑工具。
5. 用新能力更新几个种类技能（巨剑骑士真冲锋、幽灵闪现、引导技能示例）。

本期**不做**（写进文档的原因）：
- 把 Boss 塔原有机制（狂战士、熔岩蟹、巫妖等的 `BossSpawnService.ApplyBehaviors`）迁到技能层：这些战斗已经调好，迁移收益小、回归风险大。改为：Boss 可以额外通过 `Boss` 类型的 Loadout 加技能（第一期已支持），原机制不动。
- 数值模型和模拟器接入技能：在 `Balance/` 里，是另一条正在进行的工作，按规定不碰。

通用约束同前几期：不启动 Unity；不改 `Main.unity`、`Balance/`、`Docs/Balance/`、`*.mat`、`ProjectSettings/`；不 commit；新 `.cs` 手写 `.meta`；`[SerializeReference]` 类不改名；没配置时行为不变；宠物模式关闭时没有宠物技能。省 token：Codex 实现并自查，Claude 只看关键部分、编译、修。

## 1. 位移与引导（Lane P：`CB/Skills/**` 代码、`CB/Pet/Systems/**`、`CB/Player/**`、`CB/Unit/UnitMovement.cs`、`CB/Enemy/EnemyUnit.cs`、`CB/Enemy/EnemyAttackSystem.cs`、`CB/UI/World/EnemySkillStatusView.cs`）

### P1 位移效果（新 `ISkillEffect`）
- `KnockbackEffect{Area, Distance, Duration, FromPoint}`：把命中的单位从施法者（或落点）推开。
- `DashEffect{Distance, Speed, StopAtFirstHit, Width, Power, OnHit, PassThrough}`：施法者沿锁定方向冲出，路径上的单位受伤害（每个只一次），可选撞到第一个就停。
- `BlinkEffect{Mode: BehindTarget/ToPoint/AwayFromTarget, Distance}`：施法者瞬移。
- `PullEffect{Area, Distance, Duration}`：把单位拉向施法者。
- 共同要求：
  - 一个统一的"强制位移"组件/系统：每帧推进位移，结束后恢复原来的寻路/移动；同一单位新位移覆盖旧位移。
  - 不穿墙、不出导航网格（`NavMesh.Raycast`/`SamplePosition`，失败就停在边缘）。
  - 敌人：位移期间暂停寻路代理和 AI 移动；飞行敌人只做水平位移；`IsStatic`/Boss 塔 Boss/体型很大的单位免疫击退和拉近（加 `ImmuneToDisplacement` 判断，Boss 一律免疫）。
  - 主角：位移期间不接受摇杆输入；位移由敌人施加时视为控制效果，和眩晕共用"敌人对友方允许的效果"名单；对主角的单次位移最远 3 米。
  - 宠物：暂停寻路，位移结束后恢复跟随/追击。
  - 施法者死亡、回收、被眩晕时中止自己的冲锋；单位死亡时中止加在它身上的位移。
  - 冲锋和闪现期间施法者处于施法状态（不普攻）。

### P2 引导技能
- `SkillDef` 新增 `ChannelDuration`、`ChannelTickInterval`、`[SerializeReference] List<ISkillEffect> OnChannelTick`。
- 流程：前摇结束 → `Effects` 结算一次（可以为空）→ 进入引导，每隔 `ChannelTickInterval` 执行 `OnChannelTick`，持续 `ChannelDuration` → 后摇。
- 引导期间施法者不移动、不普攻；被眩晕、沉默、死亡打断；敌人引导时预警圈保持显示（锁定位置/方向）。
- 施法条：引导阶段显示"反向"进度（满 → 空），技能名不变。

## 2. 技能等级（Lane P 第二步，同一批文件 + `CB/UI/Pet/**`）
- 宠物种类技能的等级 = 由宠物物品等级和进化决定：`Level = 1 + floor((item.Level - 1) / 3)`，进化后再 +2（上限 10）。等级变化（升级、进化、用碎片）时刷新已装的技能槽。
- 技能用 `SkillDef.LevelScale` 缩放 Power；`LevelScale` 为空的技能用默认曲线：每级 +8%。
- 敌人技能等级保持 1（以后再按岛屿/难度）。
- 宠物技能弹窗的主动技能行显示"Lv N"。

## 3. 编辑器工具（Lane E：`A/Editor/Skills/**`，新建）
- `SkillLibrary` 的自定义 Inspector / 编辑窗口（菜单 `Tools/技能/技能库编辑器`）：
  - 列出所有 `Resources/Skills` 下的库文件，按技能、Buff、Loadout、Vfx、Summons 分页，可搜索。
  - `[SerializeReference]` 列表（`ISkillEffect`、`ISkillCastCondition`）提供"添加效果/条件"的类型下拉（反射找实现类），可删除、上下移动，嵌套列表（如 `OnImpact`、`OnTick`、`OnChannelTick`）同样支持。
  - "校验"按钮：未知 Key、跨文件重复 Key、Loadout 指向不存在的 prefab 名、缺本地化 `SKILL_NAME_`/`SKILL_DESC_`、`[SerializeReference]` 丢失类型（missing reference）。结果列表可点击定位。
- 只在编辑器里用，不影响运行时；`Assembly-CSharp-Editor` 要编译通过。

## 4. 内容（Lane D：`A/Resources/Skills/**`、本地化库；在 Lane P 完成后进行）
- `sp_chainsaw` 骑士冲锋：改为真正冲锋（DashEffect 8 米，路径伤害 Power 1.8，撞到敌人时击退 1.5 米）。
- `sp_ghost` 幽影突袭：先闪现到目标身后再打（BlinkEffect + 单体伤害）。
- `sp_hammer` 震地：附加击退 2 米。
- `sp_mage` 奥术飞弹：改为引导 2 秒，每 0.4 秒发一支直线飞弹（Power 0.6）。
- 敌人侧示例：`Zombie_Boss` 新增"冲撞"（DashEffect，直线预警 7×1.5，前摇 1.0，Power 2.0，击退主角 2 米）。
- 对应的 `SKILL_DESC_` 文案更新（13 列）。
- 用 `tools/check_skills_multi.rb`（或更新版）校验两个库文件。

## 5. 流程
- Lane P（P1+P2 → 技能等级）和 Lane E 并行；Lane D 在 Lane P 之后。
- 汇总编译 → 1 个 Claude 审查（运行时/回归）+ 1 路 Codex 审查 → 只验证中高优先级 → 修复 → `SkillSystemDesign.md` 加第 12 节。
