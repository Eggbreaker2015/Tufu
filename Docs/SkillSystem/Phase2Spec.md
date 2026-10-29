# 技能系统第二期规格（A 敌人更有威胁 + B 让玩家看得懂 + 宠物详情改版 + 15 号岛骷髅远程化）

状态：开工规格（2026-10-29）。前置：第一期与远程射击见 [SkillSystemDesign.md](SkillSystemDesign.md) 第 1～9 节。
调研原文（只读、带行号）：
- 敌人 A：`/private/tmp/claude-501/-Users-joyfort-MyProjects-Tufu/1d51cb8e-6fe6-48c0-a868-5d09993f0bb3/scratchpad/recon2_enemyA.md`
- 世界 UI B：`/private/tmp/claude-501/-Users-joyfort-MyProjects-Tufu/1d51cb8e-6fe6-48c0-a868-5d09993f0bb3/scratchpad/recon2_worldui.md`
- 宠物详情：`/private/tmp/claude-501/-Users-joyfort-MyProjects-Tufu/1d51cb8e-6fe6-48c0-a868-5d09993f0bb3/scratchpad/recon2_petui.md`

路径缩写：`CB/` = `Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/`，`A/` = `Recovery/StudyProject/Assets/`。

## 0. 通用约束

- 不启动 Unity；编译只用各自分到的 `check_*.sh`。不改 `A/Scenes/Main.unity`、`Balance/`、`Docs/Balance/`、`*.mat`。不 commit/stash/reset/push。
- 新 `.cs` 手写 `.cs.meta`（新 guid）；`[SerializeReference]` 类写好后不改名不挪命名空间。
- 没配置新功能时，现有敌人、Boss 塔、宠物、主角行为不变。
- 共享契约（已加好）：`SkillDef.IconKey`、`BuffDef.IconKey`（空 = 自动分类）。技能显示名的本地化键：`SKILL_NAME_<SkillDef.Key>`，13 列都要填（zh 中文，其余英文）。

## 1. A：敌人更有威胁（Lane 1，文件：`CB/Skills/**`、`CB/BossTower/BossMinionService.cs`、主角/宠物攻击移动、`DamageCalculator.cs`、`Enemy Skills.asset`、本地化库）

### A1 点目标、扇形、直线（不可跟踪）
- 开始施法时锁定落点 `Point` 和方向 `Direction`（存进 `SkillCastingComponent` 和 `SkillCastContext`）；前摇期间不随目标移动。`TargetType = Point` 一律按锁定处理。
- 开始施法时施法者朝目标转一次。
- `TargetSelectMode` 新增 `Line`：从施法者出发，长度 `Length`、宽度 `Width`，按锁定方向。`Cone` 用锁定方向。`SkillTargetQuery` 加矩形/线段查询。
- `TelegraphDef` 加 `Width`（直线用）。`SkillTelegraphView`：
  - 圆形：现状。
  - 直线：以施法者为起点、按方向旋转的矩形；内层沿长度方向长满。
  - 扇形：运行时生成扇面网格，内层沿半径长满。
  - 跟随时同步旋转；共用 6 个上限不变。
- `bone_volley` 的 `Telegraph.Shape` 改回 0（它是追踪弹，线形预警会误导）。

### A2 可躲的直线飞弹
- 新效果 `LinearMissileEffect{VfxKey, Speed, Count, SpreadAngle, Range, Radius, Pierce, Power, DamageType?, OnImpact}`：沿锁定方向直线飞行，每帧按"单位到本帧线段的水平距离 ≤ Radius + 单位半径"判定命中，`Pierce=false` 时命中一个就停，`true` 时每个单位只命中一次；撞墙（`NavMesh.Raycast`）或超出 `Range` 消失。
- 外观复用 `SkillProjectileView`（运行时贴图弹道）；施法者死亡后飞弹仍飞完（直线弹是"已经放出去的"），但不再触发反伤之类需要施法者的逻辑。
- 打友方统一走 `DealDamage`，并补上主角睡眠/`!CanMove` 不受伤的检查（和远程普攻一致）。
- 默认速度 8～10（主角体感移速约 5～6），保证能躲。

### A3 召唤
- 新效果 `SummonEffect{SummonKey, Count, Radius, MaxAlive, Lifetime, HealthScale, DamageScale}`；`SkillLibrary` 新增 `Summons` 表：`{Key, Prefab(GameObject)}`。
- 用 `BossMinionService.Spawn`，属性按施法者 config 缩放；补寻路代理和 `NavMeshAgentActiveMarker`、`AggroTimer = AggroDuration`；按巫妖写法处理死亡动画与回收（`hasDeathAnim=false` + 死亡动画标记）。
- 记录"谁召唤了谁"：施法者死亡/回收时召唤物一并 Despawn；`MaxAlive` 限制同时存在数；`Lifetime > 0` 到时消失。
- 召唤物：不给奖励、**不触发 `OnKill` 物品技能**、不进吞噬流程；召唤物自身不再安装召唤类技能（防递归）。
- 施法者是 Boss 塔竞技场单位时不召唤（打一次 warning）。

### A4 友方眩晕和减益
- 敌人技能对主角和宠物可以：眩晕、降防御、降攻速（另加原有减速）。
- 眩晕：复用 `BuffStunComponent` + `UnitBuffSystem` 倒计时；**不用 `PlayerModel.CanMove`**。主角：移动、转向、走路动画、攻击都停；攻击冷却冻结。宠物：停寻路、`SetWalking(false)`、不攻击；被眩晕时不传送。单次对主角的眩晕最长 1.5 秒。死亡/复活/传送时清掉。补一个简单的眩晕表现（头顶旋转星星，可用现有粒子或运行时小图标）。
- 减益按"读取时乘系数"实现（和宠物减速一样），不给主角/宠物加属性修正：
  - `GlobalDefence × PercentMultiplicative` → 防御系数，在 `DamageCalculator` 防御值处相乘（主角和宠物都生效）。
  - `AttackCooldown × PercentMultiplicative` → 攻击间隔系数，主角 `PlayerAttackSystem`、宠物 `PetAttackSystem` 冷却与动画速度都要乘。
  - 其他属性类型对友方忽略（`WarnOnce`）。
- `BuffDef.Stun` 注释改为"所有单位"。`ClearBuffs` 同时清眩晕组件。

### A5 内容（`A/Resources/Skills/Enemy Skills.asset`）
| 技能 | 敌人 | 内容 |
|---|---|---|
| `bone_spear` | `Skeleton_Elite` | 直线预警 10×1.2 米，前摇 0.9 秒，射出一支穿透骨矛（速度 9，Power 2.0），可躲 |
| `bone_rain` | `Skeleton_Boss` | 点目标：锁定主角施法开始时的位置，圆半径 2.5，前摇 1.4 秒，Power 2.2 |
| `raise_dead` | `Skeleton_Boss` | 召唤 3 个 `Skeleton_Easy`，生命 30%、伤害 50%，存活 20 秒，同时最多 6 个，冷却 18 秒 |
| `cleave` | `Zombie_Boss` | 扇形 120°、半径 4，前摇 1.0 秒，Power 2.0，附加 `armor_break`（防御 ×0.7，5 秒） |
| `stomp` | `Zombie_Boss` | 圆半径 3，前摇 1.3 秒，Power 1.2，眩晕 1.0 秒 |
| `curse_bolt` | `Skeleton_Medium+` | 追踪飞弹，Power 0.6，附加 `weakness`（攻击间隔 ×1.3，4 秒），冷却 10 秒 |

数值都是起始值，记录在实现报告里。每个会施法的技能补 `SKILL_NAME_<key>`。

> 实现时的调整（原因见 [SkillSystemDesign.md](SkillSystemDesign.md) 10.4）：`raise_dead` 改为生命 3%、伤害 20%（按 Boss 属性的 30%/50% 是普通 Skeleton_Easy 的 7～20 倍血）；`bone_rain` 用新选目标方式 `SkillPick.Hero`；直线飞弹的命中半径不超过直线预警的半宽。

### A6 15 号岛骷髅全部远程
- 给 `Skeleton_Easy`、`Skeleton_Medium+`、`Skeleton_Elite`、`Skeleton_Boss` 加 `RangedAttack`（`Skeleton_Medium` 已有）。
  - Easy 数量多（32 只）：伤害倍率 0.8、射程 6.5；Boss：射程 8、单发更重（倍率 1.3）、不后撤。
  - 治疗光环半径改 7 以覆盖远程站位。
- **不改**：`skeleton dragon`（16、17 号岛共用）、巫妖召唤的 `*_Summoned`（最多 30 只，会变成弹幕）。在文档里写明原因。
- `raise_dead` 召唤出来的 `Skeleton_Easy` 因为按 prefab 名装配，也会是远程，这是预期。

## 2. B：让玩家看得懂（Lane 2，文件：`CB/UI/World/**`、`CB/Enemy/Systems/EnemyHealthBarSystem.cs`、`CB/UI/AbilityBuffIconsView.cs`、`CB/Abilities/AbilityIcons.cs`）

- **施法条**：敌人血条下方，运行时建在 HealthBar 根节点下（不额外 AddTarget）。显示技能名（`SKILL_NAME_<key>`，缺键时不显示名字且不刷日志）+ 进度（`Elapsed / CastPoint`，结算后隐藏）。只对 `CastPoint ≥ 0.3` 的技能显示。名字在开始施法时取一次并缓存，切语言时清缓存。字体用 `RegisterLike(血条文字)`，中文走 `PetStatsText.ApplyCjkFont`。
- **敌人 Buff 图标**：施法条下方一排最多 3 个图标，复用 `AbilityBuffIconsView`（加 `Add(Sprite, remaining01, stacks, tint)` 重载和可配图标尺寸）；减益用红色描边/着色。旋转扫描降到约 10 Hz 或只在最后 2 秒闪烁。
- **池化安全**：血条是池化的，`UnitCore.Dispose` 不清 `HealthBar` 引用。状态视图要绑定到当前单位并在每次 `CreateHealthBarForUnit` 时重置；只对敌人启用。
- **图标分类**：
  - `IconKey` 非空时直接用。
  - Buff 自动：`Stun` → 眩晕；StatMods → 复用 `TryFromStat`；OnTick 治疗 → 回血；OnTick 伤害 → 伤害。`enrage` 设 `IconKey: rage`。
  - Skill 自动：取效果里优先级最高的类别，`ApplyBuffEffect`/`AuraEffect` 递归到 Buff。
  - 物品技能里的 `CastSkillEffect` 按 `SkillLibrary.FindSkill(SkillKey)` 分类（石魔像砸地不再是通用图标）。

## 3. 宠物详情改版（Lane 3，文件：`CB/UI/Inventory/InventoryItemContextMenu.cs`、`ItemAbilityView.cs`、`ItemAbilitiesView.cs`、`ItemMiniView.cs`、`CB/UI/Pet/**`）

采用调研的方案 B：卡片只放摘要，完整技能放独立弹窗。场景和 prefab 不改。
- 宠物模式下详情卡片：隐藏原技能列表，在碎片行下面加一行"技能摘要"：最多 6 个分类小图标（未解锁的灰）+ "技能 N ›" 按钮。卡片高度变为常量（约 1314），四周都在安全区内、不被快捷栏挡。
- 技能弹窗 `PetAbilitiesView`（`UINavigator.OpenTemplate`，`PetUiBuilder` 构建，可滚动）：
  - 标题：宠物名 + "技能"。
  - 每行：图标 + 名字 + 完整描述（换行、关 autosize、固定 36～40 号字、行高随内容）；未解锁行灰色并显示解锁条件；专属技能和"技能"类（`CastSkillEffect`）行带小标签"专属"/"主动"。
  - 文案逻辑从 `ItemAbilityView` 抽成静态方法复用，两边输出一致。
- `ItemMiniView`（图鉴、物品信息里用）在宠物模式下也改成同样的摘要行 + 按钮，去掉"最多 3 条把技能吃掉"的问题。
- 属性块：允许换行或分两列，避免最长一行把 6 行字号都压小（选简单的做法即可）。
- 非宠物模式：原界面完全不变。

## 4. 分工与流程

| Lane | 内容 | 执行 |
|---|---|---|
| 1 | A1–A3 → A4 → A5+A6（依次） | Codex 实现，Claude 审查、编译、补修 |
| 2 | B | 同上 |
| 3 | 宠物详情改版 | 同上 |

- 三条 lane 改的文件互不重叠；编译报错若在别的 lane 的文件里，先等对方完成，不要去改。
- 全部完成后：整体编译 → 多角度审查（运行时、回归、UI、数据与规格符合度，外加 Codex review 一路）→ 逐条验证 → 修复。
- 最后更新 `SkillSystemDesign.md` 第 10 节（第二期实现记录、测试步骤）。
