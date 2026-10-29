# 技能系统设计（推荐方案）：独立 Skill + Buff 层，用适配效果接入现有物品技能

状态：设计稿，待拍板。本次全程只读，没有改任何项目文件，也没有启动 Unity。
路径约定：`CB/` = `/Users/joyfort/MyProjects/Tufu/Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/`，`Assets/` = `/Users/joyfort/MyProjects/Tufu/Recovery/StudyProject/Assets/`。
置信标记：【已核实】表示本次读代码确认过；【未核实】表示没有核实。

## 0. 评审中核实或纠正的关键事实

| 说法 | 结果 | 依据 |
|---|---|---|
| 打敌人的分支无条件解引用 `data.Weapon` | 【已核实】 | `CB/Services/Damage/DamageCalculator.cs:132-142` |
| `SpecialMultiplier` 只在受击（敌人打我方）分支生效 | 【已核实】，**方案 A 的说法有误**。A 让宠物技能打敌人时用 `Weapon = pet.Item + SpecialMultiplier`，这样倍率不会生效 | `DamageCalculator.cs:121`。`SpecialMultiplier` 在整个文件中只出现这一处 |
| `HasFixedDamage` 在打敌人的分支被忽略 | 【已核实】 | `CB/Unit/Systems/UnitDamageSystem.cs:316`，对比 `:121`、`:143` |
| `ApplyDamageToEnemy` 一进入就同步发出 OnPreHit，伤害数字依赖 Weapon | 【已核实】。所以新分支必须放在**函数开头**并提前 return | `UnitDamageSystem.cs:319`、`:349` |
| 眩晕结束时强转 `EnemyUnit` | 【已核实】 | `CB/Unit/Systems/UnitBuffSystem.cs:70` |
| 飞行敌人只判断眩晕，不判断 `IsCasting` | 【已核实】 | `CB/Enemy/EnemyAttackSystem.cs:272` |
| `PetAttackSystem` 没有眩晕或施法守卫 | 【已核实】（grep 结果为空） | `CB/Pet/Systems/PetAttackSystem.cs` |
| 4 条刷怪路径都走同一个 `EnemyUnit.Initialize` | 【已核实】 | 定义在 `CB/Enemy/EnemyUnit.cs:142`。调用方：`EnemySpawner.cs:968`、`QuestMobSpawnerService.cs:118`、`BossMinionService.cs:73`、`BossSpawnService.cs:153` |
| `EnemyCastSystem.IsCasting(entity)` 存在；施法结束后才调用 `OnComplete` | 【已核实】 | `CB/Enemy/Systems/EnemyCastSystem.cs:113-121`、`:209`（`UniTask.Delay(duration)`） |
| `AddModifier` 会把 (Name, Source, Type) 相同的修正合并 | 【已核实】 | `CB/Unit/StatBase.cs:66-89` |
| `IsRuntimeOnlySource` 只放行 PetCollection 和 PetSynergy | 【已核实】，**方案 B 漏了这一点**。B 声称“存档不涉及”，但主角身上的 Buff 修正会被 `SaveStats` 写进存档 | `CB/Player/Models/PlayerModel.cs:697`、`:709-712` |
| `ModifierSource` 最大值是 21，`AbilityTrigger` 最大值是 22 | 【已核实】 | `CB/Unit/ModifierSource.cs`、`CB/Abilities/AbilityTrigger.cs` |
| `IAbilityEffect` 有 5 个成员，`AbilityContext.Target` 的类型是 `EnemyUnit` | 【已核实】。A 方案的每个新效果都要背上这套接口，还要背 `PlayerModel` 语义 | `CB/Abilities/Core/IAbilityEffect.cs:3-15`、`AbilityContext.cs:29` |

## 1. 结论

**采用方案 B 的结构做底座：在 `Codebase.Skills` 下新建一个独立的技能层（SkillDef + BuffDef + Missile），再从方案 A 移植统一 Loadout、AI 优先级与选目标、精确前摇/后摇、光环、`IsRuntimeOnlySource` 修复和安装校验。**

理由：
- 现有物品技能系统（216 个物品技能、36 个宠物招牌技）一行都不用改。
- 新层与单位类型无关，敌人、宠物、主角都可以当施法者。
- 宠物、武器、主角通过两个适配效果（`CastSkillEffect`、`ApplySkillBuffEffect`）复用现有的 OnHit、概率、冷却条件，接入成本很低。

## 2. 核心概念

### 2.1 魔兽争霸 3 对照表

| WC3 概念 | 本设计 | 第几期 |
|---|---|---|
| Ability（技能） | `SkillDef`，放在 `SkillLibrary` 资产里 | 1 |
| 目标类型：无目标 / 单位 / 点 / 被动 | `SkillTargetType { NoTarget, Unit, Point, Passive }` | 1（Point 第 2 期） |
| Targets Allowed（可选目标） | `SkillTargetFilter [Flags]`：Enemy、Ally、Self、Hero、Pet、Boss、Alive，**相对施法者阵营**解析 | 1 |
| Cast Range / Cooldown | `Range`、`Cooldown`、`InitialCooldown` | 1 |
| Cast Point / Backswing（前摇 / 后摇） | `CastPoint`（预警、可被打断）、`Backswing`（仍处于施法状态，不移动也不普攻） | 1 |
| Effect（效果） | `[SerializeReference] List<ISkillEffect> Effects`，在前摇结束那一刻结算 | 1 |
| Buff（状态） | `BuffDef`：持续时间、叠加规则、属性修正、Stun/Silence、周期效果 | 1 |
| Aura（光环） | `AuraEffect`：被动技能每 0.5 秒给半径内的单位刷新一个 1 秒的 Buff | 1（来自 A） |
| Missile（飞弹） | `MissileEffect`，到达时执行 `OnImpact` 子效果列表 | 1（追踪弹），2（可躲避的直线弹） |
| Summon（召唤） | `SummonEffect`，包一层 `BossMinionService.Spawn` | 2 |
| AI 自动施法 | `AutoCast.Priority` + `Pick` + `UseWhen` 条件列表（来自 A） | 1 |
| 技能等级 | `SkillSlot.Level`，按 `LevelScale[]` 系数缩放 Power | 1 |
| 法术不触发攻击特效 | 技能伤害不发 OnHit 等触发器 | 1 |
| 魔法值 | 不做，用冷却控制节奏 | — |

### 2.2 施法流程（单个技能槽）

1. **思考**：每个单位每 0.2 秒思考一次，不同单位错峰。
2. **筛选**：跳过冷却中的技能，跳过 `Pick` 选不出目标、超出射程、`UseWhen` 不满足的技能；在剩下的技能里取 `Priority` 最高的一个。
3. **开始施法**：
   - 设置冷却。
   - 施法者是敌人时，调用 `EnemyCastSystem.StartCast(Duration = CastPoint + Backswing)`。
   - 生成预警圈。
4. **前摇结束**：由 `SkillCastingComponent` 自己计时。到点时，如果施法者还活着、仍在施法、没有被眩晕，就执行 `Effects`；否则视为被打断，技能作废。
5. **后摇**：剩余时间里 `IsCasting` 仍为真，单位不移动、不普攻；后摇结束时 `EnemyCastSystem` 自行清理。
6. **清理**：死亡、被打断或施法结束时回收预警圈。

### 2.3 增益与减益

- `BuffDef.StatMods`：`{StatType, ModifierType, ValuePerStack}`。
- 叠加规则：`Refresh`、`Stack`、`Independent`、`KeepLonger`。
- 实现方式：每个 Buff 实例 new 一个独立的 `StatModifier`，`Name = "skill:{key}:{instanceId}"`，`Source = ModifierSource.Skill (22)`，由 Buff 系统负责移除。
  - 不走 `AbilityTemporary` 的原因：同名修正会被合并（`StatBase.cs:66-89`），光环反复刷新会越叠越高。
- 减速：`MovementSpeed` 的 `PercentMultiplicative` 修正，取值 0.7。敌人（`EnemyAttackSystem.cs:105`）和主角每帧实时读取移动速度，所以第 1 期就能生效。
- 眩晕：写入现有的 `BuffStunComponent`。

### 2.4 光环、投射物、AI 规则

- **光环**：`AuraEffect{Radius, Filter, BuffKey, Interval = 0.5}`，挂在 `TargetType = Passive` 的技能上。单位离开范围后，Buff 在 1 秒内自然过期。
- **投射物**：用 `WorldVfxService.SpawnTracking`（`CB/Vfx/WorldVfxService.cs:62`）做追踪弹。到达时先检查目标的 `IsDead`，因为对象池里的单位不会被销毁，然后执行 `OnImpact`。
- **AI 选目标 `Pick`**：`CurrentTarget`、`NearestHostile`、`LowestHpAlly`、`Self`、`DensestHostile`。
  - 敌人的 `CurrentTarget` 是 `AggroTarget`，为空时取 `playerModel.Unit`，与 `EnemyAttackSystem.cs:353-361` 一致。

## 3. 运行时结构

### 3.1 新增类型（`CB/Skills/`）

| 类型 | 说明 |
|---|---|
| `SkillDef`、`BuffDef`、`TelegraphDef`、`SkillAutoCast`、`SkillEnums` | 数据定义，都是 `[Serializable]` |
| `SkillLibrary : ScriptableObject` | 包含 `Skills`、`Buffs`、`Loadouts`、`Vfx`（Key 到 ParticleSystem 的直接引用）。用 `Resources.LoadAll<SkillLibrary>("Skills")` 加载，支持拆成多个文件 |
| `UnitSkillLoadout` | `{OwnerKind: Enemy/Boss/Pet/Hero, Key, List<SkillSlotDef{SkillKey, Level}>}`，统一 Loadout，来自 A |
| `SkillCastContext`（struct） | `Caster`、`CasterEntity`、`PrimaryTarget`、`Point`、`Skill`、`Level`、`Power`、`SourceItem`（可空） |
| `ISkillEffect` | `void Apply(in SkillCastContext)`，只有一个成员 |
| `ISkillCastCondition` | `bool Check(in SkillCastContext)` |
| `TargetSelector` | `Mode`（Self / Primary / CircleCaster / CirclePoint / Cone）、`Radius`、`Angle`、`Filter`、`MaxTargets` |
| `SkillTargetQuery` | 阵营与半径查询。`IsHostile = (a is EnemyUnit) != (b is EnemyUnit)`。敌方候选来自 `EnemiesModel.Enemies`；友方候选是 `playerModel.Unit` 加上存活的宠物。按 XZ 平方距离比较 |
| `SkillService`（放进 GameContext） | `Install`、`Cast`、`ApplyBuff`、`DealDamage`、`Query`；安装时校验键名并打 warning（来自 A） |

### 3.2 ECS 组件

- `SkillCasterComponent { List<SkillSlot> Slots; float NextThinkTime; }`，其中 `SkillSlot = {Def, Level, Cooldown}`。
- `SkillCastingComponent { Slot, Target, Point, float Elapsed, bool Fired, TelegraphHandle }`：只在施法期间存在。
- `UnitBuffsComponent { List<BuffInstance> Buffs; }`，其中 `BuffInstance = {Def, Remaining, Stacks, TickTimer, Source, List<(Stat, StatModifier)> Applied}`。

### 3.3 系统（在 `CB/Core/SystemsInstaller.cs` 中注册）

| 系统 | 位置 | 职责 |
|---|---|---|
| `SkillAutoCastSystem` | `EnemyCastSystem`（`:46`）之前 | 扣冷却，执行光环 tick，做思考与施法，推进前摇计时和结算，清理预警圈。以下情况跳过思考：被眩晕、带 Silence、`EnemyCastSystem.IsCasting`（避免与狂战士、熔岩蟹自身的施法冲突）、`UnitSpawnActiveMarker`、`IsFar`、`RetreatTimer > 0` |
| `SkillBuffSystem` | `PetStatsSyncSystem`（`:74`）之后，`UnitDamageSystem`（`:67`）之前的位置**需要再确认** | 扣时间、执行周期效果、到期移除修正、写入眩晕 |

### 3.4 接入点

| 对象 | 接入位置 | 方式 |
|---|---|---|
| 敌人（普通刷怪点、任务怪、Boss 小兵、Boss） | `CB/Enemy/EnemyUnit.cs` 的 `Initialize` 末尾（`:142` 起） | `SkillService.InstallForEnemy(this, config)`。查表顺序：`Boss Key`，然后 `config.Prefab.name`。不用实例名，因为对象池实例名带 `(Clone)`。对象池复用时在这里重置冷却和 Buff |
| 飞行敌人 | `EnemyAttackSystem.cs:272` | 补一行 `IsCasting` 判断 |
| 宠物、武器、护符 | 物品技能新增 `CastSkillEffect` 和 `ApplySkillBuffEffect`（`CB/Abilities/Effects/`） | 施法者取 `ctx.Attacker ?? ctx.PlayerUnit`；目标取 `AbilityTargetResolver.Resolve(ctx)`；Power 取物品技能的 `Value`，可以继续用 `LevelScaling` 按星级成长 |
| 主角 | 同上，挂在 `PlayerAttackItem` 或任意装备上 | 第 2 期再给主角挂 `SkillCasterComponent`，做自动施法栏 |
| 宠物自动施法 | 第 2 期在 `PetSpawnSystem`（`CB/Pet/Systems/PetSpawnSystem.cs:83-99`）中安装 Loadout，同时给 `PetAttackSystem` 加施法和眩晕守卫 | — |

### 3.5 复用的现有管线

- **敌人打我方**：发送 `WeaponHitEvent{Source = caster, Target, DamageType, Undodgeable, HasSpecialMultiplier = true, SpecialMultiplier = Power}`，写法与 `CB/BossTower/Systems/LavaCrabExplosionSystem.cs:115-130` 相同。防御、闪避、格挡、宠物分支全部照常生效。
- **我方打敌人**：在 `UnitDamageSystem.ApplyDamageToEnemy`（`:314`）的**最开头**新增 `if (data.Weapon == null) { ApplySkillDamageToEnemy(data); return; }`。
  - 伤害 = `FixedDamage × (1 − 对应护甲抗性) × (1 − UniversalDefencePercent)`。
  - 扣血后调用 `ProcessEnemyDeath`（`:231`），所以 OnDied 和刷怪点的 OnKill 照常发生。
  - 不发 OnPreHit、OnHit 等触发器，对应 WC3 的规则，也避免了“OnHit → 放技能 → 伤害 → 再次 OnHit”的递归。
  - 现在 `Weapon == null` 走到这里会直接空引用异常，所以新分支不影响任何现有行为。
  - `FixedDamage` 由 `SkillService.DealDamage` 计算：`Power × 施法者 Damage`；宠物施法时再加上 `PetStats.GetOwnAttack(SourceItem.Damage, def)`。
- **限时属性**：`StatBase.AddModifier` / `RemoveModifier`（`CB/Unit/StatBase.cs:66`），配合新增的 `ModifierSource.Skill = 22`。
- **施法表现**：`EnemyCastSystem` 负责 `IsCasting`、Animator 参数、`EnemyCastAbilityVisualProvider`，以及死亡时取消施法。
- **特效**：`WorldVfxService`。`SkillLibrary.Vfx` 直接引用 prefab，绕开不存在的 `Assets/Prefabs/Vfx/Abilities` 寻址路径。

### 3.6 必改的小点

| 文件 | 改动 |
|---|---|
| `CB/Player/Models/PlayerModel.cs:711` | 把 `ModifierSource.Skill` 加入 `IsRuntimeOnlySource`。不加的话，主角身上的 Buff 会被存进存档 |
| `CB/Unit/Systems/UnitBuffSystem.cs:70` | 改成 `(… as EnemyUnit)?.ApplyStunAnimation(false)` |
| `CB/Unit/ModifierSource.cs` | 新增 `Skill = 22` |

## 4. 配置方式

### 4.1 敌人在哪配

- 不碰 `Assets/Scenes/Main.unity`：717 个刷怪点配置都在这个文件里，而它现在是 LFS 冲突状态。
- 新建 `Assets/Resources/Skills/Enemy Skills.asset`（类型 `SkillLibrary`，手写 YAML），配一个 NativeFormatImporter 类型的 `.meta`，`mainObjectFileID: 11400000`。
- 先手写 `CB/Skills/SkillLibrary.cs.meta` 固定一个 guid，资产的 `m_Script` 填这个 guid。
- 多态数据沿用现有写法：`rid` + `references: version: 2 RefIds`，rid 在文件内唯一，RefIds 按 rid 升序排列。
- 按 prefab 名配置：74 种 prefab 各配一次就能覆盖全部刷怪点。按刷怪点单独覆盖留到第 2 期，等场景冲突解决之后。

### 4.2 SkillDef 字段

| 字段 | 类型 | 说明 |
|---|---|---|
| `Key` | string | 唯一键，也是本地化后缀 |
| `TargetType` | enum | NoTarget / Unit / Point / Passive |
| `Range` | float | 施法距离 |
| `CastPoint` / `Backswing` | float | 前摇 / 后摇。对玩家有威胁的技能 `CastPoint` 至少 0.6 秒 |
| `Cooldown` / `InitialCooldown` | float | 冷却 / 开战后多久才能第一次放 |
| `AnimationBool` / `AnimationTrigger` | string | 留空表示不切换动画 |
| `Telegraph` | `{Shape: None/Circle/Cone/Line, Radius, Angle, AnchorOnTarget}` | 预警形状 |
| `AutoCast` | `{Priority, Pick, [SerializeReference] UseWhen}` | AI 规则 |
| `LevelScale` | `float[]` | 按等级缩放 Power |
| `Effects` | `[SerializeReference] List<ISkillEffect>` | 前摇结束时结算 |

第 1 期效果：`DamageEffect{Area, Power, DamageType?, Undodgeable}`、`HealEffect{Area, PercentMaxHp}`、`ApplyBuffEffect{Area, BuffKey}`、`MissileEffect{VfxKey, Speed, Count, Area, OnImpact}`、`AuraEffect`、`SpawnVfxEffect`。

第 1 期条件：`EnemiesInRadius{Radius, Min}`、`SelfHealthBelow{Percent}`、`AlliesHurtInRadius{Radius, Percent, Min}`、`TargetDistance{Min, Max}`、`HasBuff{Key, Invert}`、`SkillChance{Chance}`。

### 4.3 示例（rid 已简写）

**① 砸地 `slam`：带预警圈的 AoE 加减速**

```yaml
- Key: slam
  TargetType: 0
  Range: 3
  CastPoint: 1.2
  Backswing: 0.6
  Cooldown: 8
  InitialCooldown: 3
  Telegraph: {Shape: 1, Radius: 3.5, AnchorOnTarget: 0}
  AutoCast: {Priority: 10, Pick: 0, UseWhen: [{rid: 11}]}   # EnemiesInRadius{Radius: 3.5, Min: 1}
  Effects: [{rid: 12}, {rid: 13}]
  # 12 DamageEffect{Area:{Mode: CircleCaster, Radius: 3.5, Filter: Enemy|Alive}, Power: 2.5, Undodgeable: 1}
  # 13 ApplyBuffEffect{Area: 同上, BuffKey: slow_30}
```

**② 骨箭齐射 `bone_volley`：远程追踪弹**

近身时这个技能不可用，敌人改用普攻，形成“远了射箭、近了砍人”两种状态。

```yaml
- Key: bone_volley
  TargetType: 1
  Range: 9
  CastPoint: 0.6
  Backswing: 0.4
  Cooldown: 6
  Telegraph: {Shape: 3, Radius: 9}
  AutoCast: {Priority: 5, Pick: 0, UseWhen: [{rid: 21}]}    # TargetDistance{Min: 3, Max: 9}
  Effects: [{rid: 22}]
  # 22 MissileEffect{VfxKey: bone_arrow, Speed: 12, Count: 3,
  #      Area:{Mode: CirclePoint, Radius: 6, Filter: Enemy|Alive, MaxTargets: 3}, OnImpact: [{rid: 23}]}
  # 23 DamageEffect{Area:{Mode: Primary}, Power: 0.8}
```

**③ 低血狂暴 `enrage` 与治疗光环 `heal_aura`**

```yaml
- Key: enrage
  TargetType: 0
  CastPoint: 0.8
  Cooldown: 20
  AutoCast: {Priority: 20, Pick: 3, UseWhen: [{rid: 31}, {rid: 32}]}  # SelfHealthBelow{0.5}, HasBuff{enrage, Invert:1}
  Effects: [{rid: 33}]                                                # ApplyBuffEffect{Area:{Mode: Self}, BuffKey: enrage}
- Key: heal_aura
  TargetType: 3
  Effects: [{rid: 41}]                  # AuraEffect{Radius: 5, Filter: Ally|Alive, BuffKey: aura_regen, Interval: 0.5}
Buffs:
- {Key: slow_30, Duration: 2, IsDebuff: 1, Stack: 0,
   StatMods: [{StatType: MovementSpeed, ModifierType: PercentMultiplicative, ValuePerStack: 0.7}]}
- {Key: enrage, Duration: 8, Stack: 0,
   StatMods: [{StatType: Damage, ModifierType: PercentMultiplicative, ValuePerStack: 1.5},
              {StatType: AttackCooldown, ModifierType: PercentMultiplicative, ValuePerStack: 0.6}]}
- {Key: aura_regen, Duration: 1, TickInterval: 1, OnTick: [HealEffect{Area:{Mode: Self}, PercentMaxHp: 0.02}]}
Loadouts:
- {OwnerKind: Enemy, Key: Bear, Skills: [{SkillKey: slam, Level: 1}]}
- {OwnerKind: Enemy, Key: skeleton_king, Skills: [{SkillKey: enrage, Level: 1}, {SkillKey: heal_aura, Level: 1}]}
```

Loadout 里的 prefab 名在 LFS 场景中，【未核实】。

**④ 复用到宠物：石像鬼宠物的砸地**

在 `Assets/MonoBehaviour/pet_golem.asset` 的 `AbilityDefs` **末尾**追加一条。该文件现有的技能依次是 `pet_golem_ability_1`、`pet_golem_ability_2`、`pet_sig_golem`（`:33`、`:76`、`:103`）。必须追加在末尾，因为 Boost 类效果按索引引用技能。

```yaml
- Key: pet_skill_golem_slam
  Trigger: 2                  # OnHit
  Conditions: [{rid: A}, {rid: B}]   # A PetModeCondition{}, B CooldownCondition{Cooldown: 8}
  Effects: [{rid: C}]                # C CastSkillEffect{SkillKey: slam, Level: 1}
  Value: 0.6                         # 作为 Power 系数，可用 LevelScaling 随星级成长
  UnlockLevel: 0
```

- `slam` 的 `Filter: Enemy` 相对宠物所在的友方阵营解析，所以伤害打到敌人身上，走 `Weapon == null` 分支，不会触发招牌技递归。
- 主角复用时，把同样的效果挂到 `PlayerAttackItem` 上即可。
- 需要补两条本地化：`ABILITY_NAME_pet_skill_golem_slam` 和 `ABILITY_DESCRIPTION_pet_skill_golem_slam`。数据库每条必须写满 13 个语言值，否则查询时会越界（`CB/Localization/LocalizationService.cs:88`）。

## 5. 可读性（移动端）

| 项目 | 做法 |
|---|---|
| 地面预警 | `SkillTelegraphView`：运行时创建贴地 quad，材质用现有贴图 `Assets/Texture2D/fullCircleFaded.png` 或 `circle_sharp.png`，不手写 prefab。外圈立即显示完整半径，内圈在 `CastPoint` 时间内从 0 长满，长满的那一刻结算。用 `ObjectPoolUtility` 池化 |
| 颜色与上限 | 敌方技能红色；友方技能默认不画。同屏最多 6 个预警圈，超出的不画（来自 A） |
| 施法动作 | 只有带施法状态的控制器才配动画参数：berserker 的 `StartCast`、Crab 和 Creeper 的 `isCasting`。其他敌人在 `IsCasting` 期间停止播放动画（`CB/Enemy/EnemyUnit.cs:200`），靠预警圈提示 |
| 自带视觉 | 挂了 `EnemyCastAbilityVisualProvider` 的 prefab（Creeper、Boss_Berserker、Boss_Crab）会自动显示 |
| 施法条、Buff 图标、头顶技能名 | 第 2 期。Boss 施法条参考 `CB/UI/BossTower/LavaCrabHudPanel.cs`；Buff 图标接未跟踪的 `CB/UI/AbilityBuffIconsView.cs` |

## 6. 分阶段计划

### 第一阶段（MVP）：敌人自动施法，外加 1 个宠物复用示例

**新增（`CB/Skills/`，每个 `.cs` 配 `.meta`）**

| 文件 | 约行数 |
|---|---|
| `SkillDef.cs`、`BuffDef.cs`、`TelegraphDef.cs`、`SkillAutoCast.cs`、`SkillEnums.cs`、`UnitSkillLoadout.cs` | 200 |
| `SkillLibrary.cs`（含查表） | 90 |
| `Core/SkillCastContext.cs`、`ISkillEffect.cs`、`ISkillCastCondition.cs`、`TargetSelector.cs`、`SkillTargetQuery.cs` | 220 |
| `Components/SkillCasterComponent.cs`、`SkillCastingComponent.cs`、`UnitBuffsComponent.cs` | 60 |
| `SkillService.cs` | 260 |
| `Systems/SkillAutoCastSystem.cs`、`Systems/SkillBuffSystem.cs` | 450 |
| `Effects/`：Damage、Heal、ApplyBuff、Missile、Aura、SpawnVfx | 320 |
| `Conditions/`：6 个 | 130 |
| `Telegraph/SkillTelegraphView.cs` | 120 |
| `CB/Abilities/Effects/CastSkillEffect.cs`、`ApplySkillBuffEffect.cs` | 80 |

**修改（约 45 行）**

| 文件 | 改动 |
|---|---|
| `CB/Core/SystemsInstaller.cs` | 注册 2 个系统 |
| `CB/Core/Boot.cs` | 注册 `SkillService`，参照 `:80` 处 `WorldVfxService` 的写法 |
| `CB/Enemy/EnemyUnit.cs` | `Initialize` 末尾调用安装 |
| `CB/Enemy/EnemyAttackSystem.cs:272` | 补 `IsCasting` 判断 |
| `CB/Unit/Systems/UnitDamageSystem.cs:314` | 在函数开头新增 `Weapon == null` 分支，约 30 行 |
| `CB/Unit/Systems/UnitBuffSystem.cs:70` | 改成 `as EnemyUnit` |
| `CB/Unit/ModifierSource.cs` | `Skill = 22` |
| `CB/Player/Models/PlayerModel.cs:711` | 加入运行时来源 |

**数据**

- 新增 `Assets/Resources/Skills/Enemy Skills.asset` 及其 `.meta`。
- 修改 `Assets/MonoBehaviour/pet_golem.asset`。
- 修改 `Assets/Resources/localization/LocalizationDatabase.asset`。

**工作量**：新增 C# 约 1900 行，改动约 45 行，估计 4 到 6 人日。验证需要在 Unity 里实际跑。

**第一阶段限制**

- 敌人技能对主角和宠物只造成伤害和减速；**对宠物的属性类 Buff 不生效**，也不眩晕友方单位。
- 宠物技能只通过 OnHit 概率或冷却触发，没有自动施法。

### 第二阶段

- 宠物属性 Buff：在 `PetStatsSyncSystem.FillExtras`（`CB/Pet/Systems/PetStatsSyncSystem.cs:171`）里读取 `UnitBuffsComponent`。采用 A 的做法，比 B 的“检测到 Stat 被替换就重挂”更稳，后者可能触发反复重建。
- 眩晕友方单位：给 `PlayerAttackSystem` 和 `PetAttackSystem` 加眩晕与施法守卫。
- 宠物和主角的自动施法栏（`SkillCasterComponent` 挂到 `PetUnit` 和主角上）。
- Point 目标、扇形和线形预警、可躲避的直线弹（自建 `MissileSystem`）、召唤。
- 施法条、Buff 图标、宠物详情页显示技能。
- 按刷怪点覆盖技能（等 `Main.unity` 冲突解决）。

### 第三阶段

- 位移、击退、引导技能。
- 技能等级与宠物星级挂钩。
- 把 `BossSpawnService.ApplyBehaviors`（`CB/BossTower/BossSpawnService.cs:239-262`）的 `is` 链逐步迁到技能层。
- 编辑器里的 SerializeReference 类型选择器（项目目前没有）。
- 把技能期望输出接入数值模型和模拟器。

## 7. 风险和待确认的问题

### 7.1 风险

1. **手写资产**：SerializeReference 按类名、命名空间、程序集匹配，类写好后不能改名或移动（项目没有用 `[MovedFrom]`）。rid 要在文件内唯一、RefIds 要升序。`.cs.meta` 的 guid 要先手写固定。
2. **与 Boss 自身施法冲突**：在已经施法的实体上再次 `castingStash.Set`，不会重新进入 `OnCastStarted`。所以必须先检查 `IsCasting`；不要给狂战士和熔岩蟹配主动技能。
3. **前摇自计时与 `EnemyCastSystem` 的 UniTask 计时并存**：施法者死亡后，离开过滤器的单位要靠 `SkillAutoCastSystem` 每帧检查并回收预警圈，否则会残留。
4. **数值**：敌人技能会改变受到的伤害，现有的数值模型和模拟器不知道这些技能。第 1 期手动调整 Power。
5. **性能**：思考频率 0.2 秒一次并错峰，半径查询是线性的，开销小；Buff 列表注意每帧不分配新对象。
6. **【未核实】**：
   - prefab 名和 `EnemySpawnerConfig.Key` 的实际取值（都在 LFS 场景里）。
   - Boss 塔小兵是否都在 `EnemiesModel.Enemies` 中。
   - Morpeh 系统与 MonoBehaviour 版 `EnemyAttackSystem` 在同一帧里的执行先后。不过 `IsCasting` 守卫无论谁先执行，最多晚一帧生效。

### 7.2 需要你拍板

1. **技能伤害是否触发 OnHit 类物品技能？** 推荐不触发，即 WC3 的“法术不触发攻击特效”。代价是宠物的技能 AoE 不会再触发招牌技。
2. **第 1 期敌人技能能否眩晕或减益主角和宠物？** 推荐第 1 期只造成伤害和减速，眩晕友方单位放到第 2 期。
3. **敌人技能按 prefab 名统一配置，还是等场景冲突解决后按刷怪点配置？** 推荐先按 prefab 名配置。
4. **宠物技能形态**：通过 OnHit 冷却触发（第 1 期就能做），还是做成独立的自动施法栏（第 2 期）？另外，宠物技能是和招牌技并存，还是替换招牌技？

### 7.3 已拍板（2026-10-29）

| 问题 | 决定 |
|---|---|
| 技能伤害是否触发 OnHit 类物品技能 | 不触发（法术不触发攻击特效） |
| 第 1 期敌人技能对主角和宠物的效果 | 只有伤害和减速；眩晕和属性减益放第 2 期 |
| 敌人技能的配置粒度 | 按敌人种类（prefab 名 / Boss Key）在独立技能表里配置，不改 Main.unity |
| 宠物用新技能的方式 | 命中时按冷却触发（`CastSkillEffect`），与招牌技并存 |

## 8. 第一阶段实现记录（2026-10-29）

代码在 `CB/Skills/`，适配效果在 `CB/Abilities/Effects/CastSkillEffect.cs`、`ApplySkillBuffEffect.cs`，敌人技能表在 `Assets/Resources/Skills/Enemy Skills.asset`。编译通过，**没有在 Play 模式里验证过**。

### 8.1 敌人配置

运行时查表顺序：Boss 的 `config.Key` → 实例名去掉 `(Clone)` → `config.Prefab.name`。

| 敌人 prefab | 位置 | 技能 |
|---|---|---|
| `Zombie_Medium+` | 16 号岛，6 个刷怪点 | 砸地 slam |
| `Zombie_Boss` | 16 号岛 Boss | 砸地 + 低血狂暴 |
| `Skeleton_Medium` | 15 号岛，7 个刷怪点 | 骨箭齐射 bone_volley |
| `Skeleton_Boss` | 15 号岛 Boss | 低血狂暴 + 治疗光环 |

宠物示例：`pet_golem.asset` 末尾加了 `pet_skill_golem_slam`（命中时，8 秒冷却，施放 slam，Power 0.6）。

### 8.2 与设计稿不同的地方

- **减速统一为"慢 30%"。** 主角移速走幂曲线，原始 0.7 只慢约 4%，所以主角这边做了换算；敌人用线性值；宠物在 `PetMovementSystem` 里应用自己的减速，并扣掉主角身上的技能减速，避免宠物跟着主角一起变慢或被减两次。
- **宠物不继承 Skill 来源的修正**（`PetStatInheritance`）。
- **技能命中不触发穿刺标记**（`PiercingWeaponMarkSystem`），与"法术不触发攻击特效"一致。
- **敌人只在战斗中思考和计冷却**（仇恨计时 > 0 或被主角锁定），进战时重置初始冷却。
- **打断**：施法者死亡、回收、被眩晕或沉默时立即取消施法（新增 `EnemyCastSystem.CancelCast`）。没有施法动画的敌人施法时停止走路/攻击动画。
- **slam 射程 3.5**：地面敌人停在约 3.2～3.3 米，射程 3 几乎不会触发。
- **宠物施放的技能是瞬发**，没有前摇和预警圈。
- **飞弹**：所有飞弹都有超时（`travel×1.5+0.5` 秒），施法者死亡后飞弹作废；`WorldVfxService.SpawnTracking` 新增可选 `lifetime` 参数。（已补：骨箭改用运行时生成的 `bone_bolt` 弹道，见第 9 节。）
- ~~bone_volley 最小距离 4 米~~：已改为 0～9 米，骷髅改成纯远程，见第 9 节。
- **只画圆形预警圈**，扇形和线形在第 2 期，所以骨箭没有预警。

### 8.3 进 Play 要测的

1. 没配技能的敌人、宠物战斗、Boss 塔：行为不变，控制台没有新报错。
2. `Zombie_Medium+`：进战约 3 秒后出现红圈，1.2 秒长满后砸地；站在圈里吃 2.5 倍不可闪避伤害并减速 30% 2 秒，提前走出圈外能躲开。
3. `Zombie_Boss`：血量低于 50% 时狂暴 8 秒，期间不重复施放。
4. `Skeleton_Medium`：见 9.4（纯远程，不再近身普攻）。
5. `Skeleton_Boss`：附近受伤的骷髅每秒回 2% 生命；低血狂暴。
6. 石魔像宠物：约每 8 秒一次砸地，伤害并减速周围敌人，不触发命中连锁，原有招牌技照常。
7. 施法中击杀、眩晕敌人：预警圈消失、施法取消；对象池复用的敌人状态干净；减速期间存档不会存下移速修正。

### 8.4 已知问题

- ~~骨箭需要一个真正的飞行弹道特效。~~ 已在第 9 节解决。
- 同屏超过 6 个预警圈时，多出来的砸地没有预警（实际很难出现）。
- 石魔像现在有 4 个技能，详情页可能被行数上限截掉一个；`CastSkillEffect` 还没有对应的分类图标。
- 数值模型和模拟器还不知道敌人技能。

## 9. 远程普攻（2026-10-29）

需求：骷髅改成**纯远程**，在远处站定射箭，普攻也是飞行的骨箭，不再走到脸上近砍。做成通用能力：任何敌人在技能表的装配（Loadout）里加一段 `RangedAttack` 就能变成远程，没加的敌人行为完全不变。编译通过，**没有在 Play 模式里验证过**。

### 9.1 行为

- **接敌**：有效仇恨范围 = max(刷怪点的 AggroRange, `RangedAttack.AggroRange`)。骷髅原来只有 5 米，现在 10 米，所以在远处就进战、计技能冷却，也不会因为目标离开 5 米就回家。
- **站位**：地面敌人走到 `AttackRange` 以内、并且视线没被挡住时就停下、转向目标并射击；已经站定后允许目标再远 1 米才重新追，避免在边缘走走停停。视线用 `NavMesh.Raycast` 判断（墙会切开导航网格），每 0.25 秒算一次；3 米以内不检查视线，避免两边导航网格不连通时卡在原地不打。
- **后撤（可选）**：目标进到 `MinRange` 以内时，最多每 `KiteCooldown` 秒向远离目标的方向退 `KiteDistance` 米（遇到导航网格边缘、悬崖就停在边缘），后撤途中不射击。`MinRange: 0` 表示不后撤、原地射击。
- **普攻**：攻击间隔仍是原来的 `AttackCooldown` 属性，攻击动画不变。每次普攻发射一支追踪弹道，命中时发出与近战普攻**完全相同**的 `WeaponHitEvent`，所以闪避、格挡、防御、反伤、吸血都照常；`DamageMultiplier ≠ 1` 时才走 `SpecialMultiplier`。没有近战兜底。
- **目标**：开宠物模式时打它正在仇恨的单位（主角或宠物），否则打主角。
- **飞行敌人**：同样生效。射程取 max(属性射程, `AttackRange`)，移动沿用原逻辑（到射程就悬停），不做视线检查。
- **安全性**：弹道都有超时（飞行时间×1.5+0.5 秒）；目标或射手在飞行途中死亡/回收，这一发作废。对象池复用时，远程状态在每次 `EnemyUnit.Initialize` 里按装配重新设置。
- **技能不受影响**：施法（如 bone_volley）期间照常停下；施法结束后继续站桩射击。

### 9.2 配置字段（`UnitSkillLoadout.RangedAttack`）

| 字段 | 骷髅的值 | 说明 |
|---|---|---|
| `Enabled` | 1 | 0 或不写这一段 = 近战（原样） |
| `AttackRange` | 7.5 | 站定射击距离；实际 = max(属性射程, 这个值) |
| `AggroRange` | 10 | 实际仇恨范围 = max(刷怪点配置, 这个值) |
| `MinRange` | 2.5 | 目标近于此距离时后撤；0 = 不后撤 |
| `KiteDistance` | 3 | 每次后撤的距离（米） |
| `KiteCooldown` | 4 | 两次后撤的最短间隔（秒） |
| `ProjectileVfxKey` | `bone_bolt` | 飞行外观，`Vfx` 表里的键；空 = 不显示，按飞行时间结算 |
| `ProjectileSpeed` | 14 | 米/秒，7.5 米约 0.5 秒命中 |
| `ImpactVfxKey` | `bone_arrow` | 命中特效（RoundHitYellow），可空 |
| `ImpactVfxScale` | 0.6 | 命中特效缩放 |
| `DamageMultiplier` | 1 | 1 = 与近战普攻同伤害 |
| `RequireLineOfFire` | 1 | 仅地面敌人：视线被挡时继续靠近 |

**弹道外观**：`Vfx` 表的条目可以不填 `Prefab`，改填 `ProjectileTexture`，运行时会生成一个"沿飞行方向、始终朝向镜头的贴图面片 + 拖尾"，并和其他特效一样走对象池（`CB/Skills/SkillProjectileView.cs`）。可调字段：`ProjectileLength`、`ProjectileWidth`、`ProjectileTint`、`TrailTime`、`TrailWidth`、`TrailColor`，`Scale` 整体缩放。贴图要求横向、箭头朝右、透明背景。`bone_bolt` 用的贴图是 `Assets/Resources/Skills/bone_bolt.png`（Codex 生成的骨箭，512×160）。着色器用 `Sprites/Default`（在 Always Included 列表里）。`MissileEffect.VfxKey` 也能用这种条目，所以 bone_volley 的三支箭现在也能看到。

**示例**（`Enemy Skills.asset` 里 `Skeleton_Medium` 的装配）：

```yaml
  - OwnerKind: 0
    Key: Skeleton_Medium
    Skills:
    - SkillKey: bone_volley
      Level: 1
    RangedAttack:
      Enabled: 1
      AttackRange: 7.5
      AggroRange: 10
      MinRange: 2.5
      KiteDistance: 3
      KiteCooldown: 4
      ProjectileVfxKey: bone_bolt
      ProjectileSpeed: 14
      ImpactVfxKey: bone_arrow
      ImpactVfxScale: 0.6
      DamageMultiplier: 1
      RequireLineOfFire: 1
```

### 9.3 哪些敌人用了

- `Skeleton_Medium`（15 号岛，7 个刷怪点）：远程普攻 + bone_volley。bone_volley 的距离条件从 4～9 米改成 0～9 米，弹道改为 `bone_bolt`。
  - 这 7 个刷怪点是场景里名字带 `pierce` 的持盾/戴盔骷髅：`Spawner_15_medium_common_pierce_helm`、`..._common_pierce_shield`、`..._uncommon_pierce_helm`、`..._uncommon_pierce_shield`、`..._rare_pierce_shield`、`..._epic_pierce_helmet`、`..._epic_pierce_shield`。
- **15 号岛上其他骷髅仍是近战（有意不改）**：装配按 prefab 名精确匹配，下面这些 prefab 没有装配，所以不射箭、也不放 bone_volley。测试时在 15 号岛会遇到不少走过来近砍的骷髅，这是正常的，不是远程失效。
  - `Skeleton_Medium+`（骑士骷髅）：`Spawner_15_knight (1)`～`(6)`，6 个刷怪点。
  - `Skeleton_Easy`：`Spawner_15_easy` ×3。
  - `Skeleton_Elite`：`Spawner_15_elite (1)`～`(3)`。
  - `skeleton dragon`：`Spawner_15_medium_legendary_pierce_helm` / `..._shield`（名字带 pierce，但 prefab 是骨龙，不是 `Skeleton_Medium`）。
  - Boss 塔召唤的 `Skeleton_Medium_Summoned`、`Skeleton_Medium+_Summoned`。
  - 想让它们也射箭，只需在 `Enemy Skills.asset` 里为对应 prefab 名加一段同样的装配（`RangedAttack`，可选 bone_volley）。
- `Skeleton_Boss` **没有**改：它的治疗光环只覆盖身边 5 米，狂暴也是近战加成，远程站桩会让这两个技能失去意义，而且 Boss 伤害高，远程化是较大的平衡改动。要改只需在它的装配里加同样一段 `RangedAttack`。

### 9.4 怎么测

1. 进 Play，按 **F8**（或菜单 **Tools > 学习测试 > 快速测试**），点「从正常进度建立测试副本并进入」；在「地图与任务」页选「第 15 岛 · 诅咒之地」，点「传送并开放沿途入口」；在「战斗」页只开「无敌」（不要用「一键战斗体验」「快速击杀」，它们开一击必杀，敌人来不及出手）。
2. 走近**持盾/戴盔的 `Skeleton_Medium` 骷髅**（9.3 列出的 7 个 pierce 刷怪点；骑士、easy、elite 骷髅和骨龙仍是近战，见 9.3）：约 10 米时进战，它停在约 7.5 米处转身射箭；能看到骨箭从它身前飞向主角并带浅蓝拖尾，命中时有黄色命中闪光并结算伤害。
3. 继续贴近到 2.5 米以内：骷髅每 4 秒最多后退约 3 米一次，其余时间原地射击，不会近砍。
4. 隔着墙或障碍：骷髅会绕到能看到你的位置再射。
5. 关掉无敌，确认伤害和原来近战普攻相当，闪避/格挡/反伤正常。
6. 宠物模式：骷髅打离它最近的单位，射宠物时弹道飞向宠物。
7. 跑出 10 米并等 5 秒，骷髅回刷怪点；再次进战正常。
8. 其他敌人仍是近战，行为不变：僵尸，以及 15 号岛的骑士骷髅（`Skeleton_Medium+`）、easy/elite 骷髅、骨龙和 Boss 塔召唤骷髅（见 9.3）；控制台没有新报错。

### 9.5 在哪里能看到 4 个示例技能

| 技能 | 敌人（prefab 名） | 地点 | 表现 |
|---|---|---|---|
| 砸地 slam | `Zombie_Medium+`、`Zombie_Boss` | 16 号岛「夜森」（Nightwood），6 个刷怪点 + 岛上 Boss | 进战约 3 秒后脚下红圈，1.2 秒后砸地，伤害 + 减速 |
| 骨箭齐射 bone_volley | `Skeleton_Medium` | 15 号岛「诅咒之地」（Land of the Damned），7 个 pierce 刷怪点（持盾/戴盔骷髅；同岛的骑士、easy、elite 骷髅和骨龙不放） | 0～9 米同时射 3 支可见骨箭，约 6 秒一次；平时用远程普攻 |
| 狂暴 enrage | `Zombie_Boss`、`Skeleton_Boss` | 16 号岛 Boss、15 号岛 Boss | 血量低于 50% 时触发，持续 8 秒 |
| 治疗光环 heal_aura | `Skeleton_Boss` | 15 号岛 Boss | 身边 5 米内受伤的骷髅每秒回 2% 生命 |

- 游戏里敌人头上不显示名字，本地化表里也没有敌人名，只能按岛屿和外形（骷髅/僵尸）辨认；15 号岛只有持盾/戴盔的 `Skeleton_Medium` 会射箭，靠过来近砍的是其他骷髅 prefab（9.3）；岛名来自 `ISLAND_NAME_15` / `ISLAND_NAME_16`。
- 配置文件：`Recovery/StudyProject/Assets/Resources/Skills/Enemy Skills.asset`（技能、增益、装配、特效表都在这里）。
- 调试入口：游戏内作弊面板已被剥离，用编辑器窗口 **F8 / Tools > 学习测试 > 快速测试**（`Assets/Editor/RecoveryTools/StudyQuickTestWindow.cs`），步骤同 9.4 第 1 条，传送目标选 15 或 16 号岛。


## 10. 第二期实现记录（2026-10-29）

规格见 [Phase2Spec.md](Phase2Spec.md)。两个程序集编译通过（`check_final.sh` 与 `check_final.sh editor` 都是 exit=0），**没有在 Play 模式里验证过**。第 9.3、9.5 节里"15 号岛其他骷髅仍是近战""`Skeleton_Boss` 没有改"已被本期 A6 取代。

### 10.1 做了什么

- **A1 锁定点、扇形、直线**：开始施法时锁定落点 `Point` 和方向 `Direction`，前摇期间不再跟随目标；施法者起手时朝目标转一次。`TargetSelectMode.Line`（矩形，从施法者出发），`Cone` 用锁定方向。预警新增直线矩形和运行时生成的扇面网格，内层沿长度/半径长满，和圆形共用同屏 6 个上限。`bone_volley` 的预警改回无（Shape 0）。
- **A2 可躲直线飞弹**：`LinearMissileEffect` + 运行时 `CB/Skills/SkillLinearMissiles.cs`。每帧按"单位到本帧线段的水平距离 ≤ Radius + 单位半径"判定（有直线预警时不超过预警半宽），可穿透；撞墙（`NavMesh.Raycast`，射线起点每步重新贴回导航网格）或飞满 `Range` 消失。外观复用 `SkillProjectileView`。施法者死后飞弹照常飞完并照常结算伤害和 `OnImpact`；施法者被对象池复用复活时，旧飞弹作废。打主角时睡眠、濒死、`!CanMove` 不受伤。
- **A3 召唤**：`SummonEffect` + `CB/Skills/SkillSummons.cs`，`SkillLibrary.Summons` 表。经 `BossMinionService.SpawnSummon` 刷出（借 Spawn 后去掉竞技场标记，并重置远距离剔除状态），补寻路代理和 `NavMeshAgentActiveMarker`，`AggroTimer = AggroDuration`。记录召唤者：施法者死亡/回收时一起消失，`MaxAlive`、`Lifetime` 生效。召唤物不给奖励、不触发 `OnKill`/`OnDevour`、不进吞噬，自身的召唤类技能被移除；竞技场单位不召唤。新增施放条件 `SummonsBelowCondition`。
- **A4 友方眩晕和减益**：敌人的 buff 对主角和宠物可带眩晕、`GlobalDefence × PercentMultiplicative`（防御系数，在 `DamageCalculator` 相乘）、`AttackCooldown × PercentMultiplicative`（攻击间隔系数，主角/宠物冷却和动画速度都乘），减速和持续伤害照旧，其他属性只警告一次后忽略（`CB/Skills/FriendlyDebuffs.cs`）。眩晕复用 `BuffStunComponent`：主角不移动、不转向、不出手、冷却冻结，单次最长 1.5 秒；宠物停寻路、不出手、期间不传送。死亡、濒死、复活、自身瞬移超过 2 米时清除。主角或宠物被眩晕时头顶显示旋转星星（`stun_stars` → `StunnedCirclingStars.prefab`）。
- **A5 内容 + A6 15 号岛骷髅远程化**：见 10.3。另加 9 条 `SKILL_NAME_*`（13 列，zh 为中文，其余英文）。
- **B 让玩家看得懂**（`CB/UI/World/EnemySkillStatusView.cs`）：敌人血条下方的施法条（技能名 + 进度，`CastPoint ≥ 0.3` 才显示，结算即隐藏），下面一排最多 3 个 buff 图标（36 px，10 Hz 刷新，减益带红圈）。挂在池化血条根节点下，每次建血条重新绑定。`AbilityIcons` 给技能和 buff 自动分类，`IconKey` 非空时优先；物品技能里的 `CastSkillEffect` 按所施放的技能分类（石魔像砸地 → 溅射图标）。
- **宠物详情改版**（方案 B）：宠物模式下详情卡片隐藏原技能列表，碎片行下加"技能摘要"行（最多 6 个图标，未解锁为灰，按钮"技能 N ›"），卡片高度固定约 1314；点击打开可滚动的技能弹窗 `PetAbilitiesView`（每行图标、名字、完整描述、星级行，未解锁灰色带锁和解锁条件，`[专属]`/`[主动]` 标签）。`ItemMiniView` 同样改成摘要行。属性块允许换行。文案逻辑抽成 `ItemAbilityView` 的静态方法，两边一致。非宠物模式不变。

### 10.2 新增配置字段

| 位置 | 字段 | 说明 |
|---|---|---|
| `SkillDef` / `BuffDef` | `IconKey` | 空 = 自动分类；分类键（如 `rage`）或 `Resources/AbilityIcons/<key>` |
| `TelegraphDef` | `Width` | 直线预警全宽（米），`Radius` 是长度；0 = 1 米 |
| `TargetSelector` | `Length`、`Width`；`Mode: 5`（Line） | 直线范围 |
| `SkillAutoCast.Pick` | `5`（`Hero`） | 主角存活且在 `Range` 内时选主角，否则同 `CurrentTarget` |
| `LinearMissileEffect` | `VfxKey, Speed(9), Count(1), SpreadAngle, Range(10), Radius(0.4), Pierce, Power, OverrideDamageType, DamageType, Filter, OnImpact` | 括号内为代码默认值 |
| `SummonEffect` | `SummonKey, Count, Radius(2.5), MaxAlive, Lifetime, HealthScale, DamageScale, VfxKey` | 属性 = 施法者基础值 × 缩放，生命至少 1 |
| `SummonsBelowCondition` | `SummonKey, Count` | 施法者存活的召唤物少于 Count 才施放 |
| `SkillLibrary` | `Summons: [{Key, Prefab}]` | 召唤表，Prefab 需带 `EnemyUnit` |
| 本地化 | `SKILL_NAME_<SkillDef.Key>` | 施法条显示名，缺键时不显示名字也不刷日志 |

### 10.3 敌人与技能

| 敌人 prefab | 地点 | 普攻 | 技能 |
|---|---|---|---|
| `Skeleton_Easy` | 15 号岛 `Spawner_15_easy` ×3 | 远程：射程 6.5，伤害 ×0.8，仇恨 8，近于 2 米后撤 | 无 |
| `Skeleton_Medium` | 15 号岛 7 个 pierce 刷怪点 | 远程（第 9 节，不变） | bone_volley |
| `Skeleton_Medium+`（骑士） | 15 号岛 `Spawner_15_knight (1)～(6)` | 远程：射程 7.5，×1.0 | curse_bolt |
| `Skeleton_Elite` | 15 号岛 `Spawner_15_elite (1)～(3)` | 远程：射程 7.5，×1.0 | bone_spear |
| `Skeleton_Boss` | 15 号岛 Boss（`Spawner_15_boss`） | 远程：射程 8，×1.3，不后撤，箭更粗（`bone_bolt_heavy`） | enrage、heal_aura（半径 7）、bone_rain、raise_dead |
| `Zombie_Medium+` | 16 号岛，6 个刷怪点 | 近战（不变） | slam |
| `Zombie_Boss` | 16 号岛 Boss | 近战（不变） | slam、enrage、cleave、stomp |

| 技能 | 关键数值（起始值） |
|---|---|
| `bone_spear` | 直线预警 10×1.2 米，前摇 0.9 秒，冷却 7；穿透骨矛，速度 9，射程 9.5（从身前 0.5 米出发），Radius 0.2，Power 2 |
| `bone_rain` | 锁定主角起手时的位置（`Pick: 5`），半径 2.5，前摇 1.4 秒，Power 2.2（不可闪避），冷却 8，10 米内 |
| `raise_dead` | 召唤 3 只 `Skeleton_Easy`，生命 3%、伤害 20%（按 Boss 属性），存活 20 秒，同时最多 6 只，冷却 18；存活 ≥ 4 只时不施放 |
| `cleave` | 扇形 120°、半径 4，前摇 1.0 秒，Power 2，附加 `armor_break`（防御 ×0.7，5 秒），冷却 8 |
| `stomp` | 自身圆半径 3，前摇 1.3 秒，Power 1.2，`stomp_stun` 眩晕 1 秒，冷却 14；3 米内有人才施放 |
| `curse_bolt` | 追踪弹，Power 0.6，附加 `weakness`（攻击间隔 ×1.3，4 秒），冷却 10；目标已有 `weakness` 时不施放 |

**有意不改**：`skeleton dragon`（16、17 号岛共用，15 号岛的两个 legendary pierce 刷怪点也是它），远程化会连带改动另外两座岛；Boss 塔巫妖召唤的 `*_Summoned`（最多 30 只）远程化会变成弹幕。巫妖召唤配置的 `Prefab` 是 `Skeleton_Easy`，所以 `FindEnemyLoadout` 对 Boss 战单位不再回退到 `config.Prefab.name`，否则它们会拿到 `Skeleton_Easy` 的远程装配。`raise_dead` 召唤的 `Skeleton_Easy` 按 prefab 名装配，是远程，这是预期。

### 10.4 与规格不同的地方

- **`raise_dead` 生命 3%、伤害 20%（规格 30%/50%）。** 属性按施法者缩放，而 15 号岛 Boss 有约 3.56e9 血、3.6e7 伤害：30%/50% 时每只约 1.07e9 血，是普通 `Skeleton_Easy`（0.55～1.5e8）的 7～20 倍，6 只合计约 1.8 个 Boss 的血量。3%/20% 约为 1.07e8 血、7.2e6 伤害，落在普通 Easy 范围内。光环照常给召唤物回血（与其他骷髅一致）。
- **`bone_rain` 用新的 `SkillPick.Hero`。** 原来的 `CurrentTarget` 在宠物模式下是仇恨目标，常常是冲在前面的宠物；现在锁主角，主角不在 10 米内时才退回仇恨目标。
- **直线飞弹的命中半径不超过直线预警的半宽。** 规格写"Radius + 单位半径"，但主角单位半径 1.2（代理半径 1 × 缩放 1.2），骨矛会打中中心离线 1.4 米的主角，而预警半宽只有 0.6 米。现在主角中心在红框外就安全，和其他形状"按中心点判定"一致。
- **施法者死后直线飞弹照常扣血。** 规格只要求不触发反伤这类逻辑；吸血对死亡的来源无效，反伤只会打到尸体上（仍会出反伤数字和特效，见 10.5）。
- **预警"跟随时同步旋转"实现为朝向固定在锁定方向**：位置跟随施法者，判定用同一方向，画面和判定一致。
- **`ClearBuffs` 只清友方的眩晕。** 规格写"ClearBuffs 同时清眩晕组件"；如果对敌人也清，带技能 buff 的敌人被物品眩晕后死亡会立刻播死亡动画，违反"没配新功能时行为不变"。友方的清除另由 `SkillBuffSystem` 在死亡、复活、瞬移时处理。
- **宠物眩晕中只在主角真的传送（一帧位移 > 4 米且距离超过传送距离）时解除并传送**；主角走远不会提前解除，眩晕结束后照常传送。
- **宠物被眩晕时不冻结动画**，只回待机姿态并显示星星（冻结的话死亡时可能来不及恢复动画速度）。
- **施法条名字区域比条高**：TMP 的 Ellipsis 在一行高于文字框时会把整段截成空，22 号字在所有绑定字体里行高都是 26～32，所以名字框上下各伸出 12。
- **宠物技能弹窗和摘要行列出全部未隐藏的技能**（含所有未解锁的，签名技放最后），不沿用原背包列表"只显示第一个未解锁"的规则；摘要图标仍最多 6 个。
- **`Zombie_Boss` 同时保留 slam 和 stomp**（规格没说删 slam），靠初始冷却错开。

### 10.5 已知问题

- 全部没有在 Play 模式里看过：预警、施法条、图标、星星、宠物弹窗的大小和位置，以及所有数值手感。
- 施法者死后骨矛命中带反伤的主角时，会出反伤数字和朝尸体的反伤特效（无实际伤害）。要彻底去掉需要改 `UnitDamageSystem`。
- Boss 塔竞技场仆从（同样走 `BossMinionService.Spawn`，同样共享 `Skeleton_Easy` 对象池）仍可能拿到"远距离已剔除"的实例而隐身，这是原有问题；本期只在 `SpawnSummon` 里重置。
- 主角没有眩晕免疫窗口，连续眩晕可以一直控住（每次最长 1.5 秒）；眩晕只挡普攻和摇杆移动，物品技能照常触发。
- 同屏超过 6 个预警时，多出来的没有预警；导航网格边缘外侧的直线飞弹会在边界处当作撞墙消失。
- `StunnedCirclingStars.prefab` 的 meta 带 assetBundleName，打包时可能有重复资源提示。
- 宠物 UI 的"技能""专属""主动""›"是写死的中文，和其余宠物 UI 一致。
- 数值模型和模拟器还不知道敌人技能。

### 10.6 进 Play 要测的

1. **进入**：进 Play，按 **F8**（**Tools > 学习测试 > 快速测试**），点「从正常进度建立测试副本并进入」；「地图与任务」页选「第 15 岛 · 诅咒之地」或「第 16 岛 · 夜森」（岛名随当前语言），点「传送并开放沿途入口」；「战斗」页只开「无敌」（不要用「一键战斗体验」「快速击杀」）。测宠物：确认 `Resources/Pet Config.asset` 的 `enabled: 1`，在「装备与资源」页的「宠物」区选宠物种类（如石魔像 `pet_golem`），等级 1、勾「发放后出战」，点「发放选中宠物」（这样发的宠物未进化）。
2. **回归**：没配技能的敌人、Boss 塔（巫妖召唤的骷髅仍是近战）、16/17 号岛骨龙不射箭、主角和宠物行为不变，控制台没有 `[Skills]` 警告和新报错。
3. **15 号岛远程**：四种骷髅都站桩射箭（Easy 约 6.5 米，骑士/Elite 约 7.5 米，Boss 约 8 米且不后撤）；Boss 身边 7 米内受伤的骷髅回血并显示回血图标。
4. **bone_spear**（Elite）：出现 10×1.2 米红框，0.9 秒后射出骨矛；侧走出框能躲开，站在框外边缘不掉血；在骨矛飞行中击杀 Elite，骨矛仍会打中并扣血；在坡道或台阶上施放，骨矛不会中途凭空消失，撞墙时消失。
5. **bone_rain**（Boss）：带宠物时红圈落在主角脚下（不是宠物），1.4 秒后结算，提前走出能躲开。
6. **raise_dead**（Boss）：一次召唤 3 只远程 Easy，血量和普通 Easy 相当；20 秒后消失，同时最多 6 只，存活 4 只以上时不再召唤；Boss 死亡时召唤物一起消失；击杀召唤物没有掉落、不进吞噬。击杀一只普通 Easy 后立刻用传送离开再回来，之后召唤出的骷髅应可见、会攻击、有血条。
7. **16 号岛 Zombie_Boss**：cleave 出现 120° 扇形，命中后主角/宠物受到的伤害变高（`armor_break` 防御 ×0.7，5 秒）；靠近 3 米内会 stomp，命中后眩晕 1 秒（主角最长 1.5 秒），头顶有星星、不能动不能打，期间仍会受伤。`Zombie_Medium+` 的 slam 照旧。
8. **curse_bolt**（骑士骷髅）：命中后主角/宠物攻击变慢约 30%（4 秒），攻击动画同步变慢。
9. **宠物眩晕**：宠物在 Zombie_Boss 身边被 stomp 眩晕时，主角往远处走，宠物不会提前解除眩晕或被传送；眩晕结束后才跟上或传送。主角进 Boss 塔等真正传送时，宠物立即解除眩晕并传送过去。
10. **施法条**：slam、bone_spear、bone_rain 等前摇时血条下出现橙色进度条，中间显示技能名（英文和中文都要看，切语言后下一次施法生效）；结算或被打断时立即消失；`CastPoint < 0.3` 的技能不显示。
11. **敌人 buff 图标**：被石魔像减速的敌人血条下有带红圈的速度图标；Boss 狂暴时显示狂暴图标。
12. **敌人眩晕回归**：用带眩晕的锤/盾武器眩晕 Boss 身边的骷髅后击杀，尸体保持定格直到眩晕结束，和光环外的骷髅一样。
13. **宠物详情**：背包里点未进化的石魔像，卡片高度固定、按钮不被快捷栏挡、属性换行；摘要行显示 4 个图标（两个未解锁为灰）和"技能 4 ›"；点开弹窗有 4 行，未解锁的两行灰色带锁并写明解锁条件，签名技带 `[专属]`、砸地带 `[主动]`。花万能碎片或进化后，图标解锁状态更新。
14. **图鉴**：已拥有宠物的格子打开的小卡片同样是摘要行，按钮能打开弹窗。
15. **非宠物模式**：关掉宠物模式后背包、物品信息的技能列表和高度与原来完全一致。

## 11. C 期实现记录（2026-10-29）

规格见 [PhaseCSpec.md](PhaseCSpec.md)。两个程序集编译通过（`check_final.sh` 与 `check_final.sh editor` 都是 exit=0），两个技能库的跨文件 YAML 检查通过，**没有在 Play 模式里验证过**。第 10.5 节"竞技场仆从可能隐身"一条已被 X1 取代。

### 11.1 做了什么

- **X1 竞技场小兵**：`BossMinionService.Spawn` 在 `Register` 之后统一调用 `ResetFarState`（`SpawnSummon` 里原来那次删掉），竞技场小兵、巫妖骷髅、技能召唤物都走这一步。
- **X2 友方增益**：主角和宠物身上的 Buff 一律读取时乘系数，不再加属性修正（主角移速例外，见 11.4）。来源过滤：
  - 敌人施加的：仍按第二期，只认眩晕、`OnTick`、减速、降防、降攻速。
  - 友方或无来源施加的：认 `OnTick`（回血）和 `PercentMultiplicative` 的 `Damage`、`AttackCooldown`、`GlobalDefence`、`MovementSpeed`，增减都可以。
  - 其余 `WarnOnce` 后忽略；过滤后什么都不剩就不加这个 Buff。`BuffInstance` 新增 `FriendlyTarget`。
  - 新增输出伤害系数 `GetOutgoingDamageFactor` / `FriendlyDebuffs.OutgoingDamageFactor`（0～10），每次命中只乘一次：普攻在 `DamageCalculator` 敌人分支（非模拟），技能伤害在 `UnitDamageSystem.ApplySkillDamageToEnemy`。
  - 清理：宠物死亡（`SkillBuffSystem`）、复活（`PetsModel.Revive/ReviveAll`）、回收（`PetSpawnSystem.Despawn`）；宠物模式关闭时每帧 `ClearPetSourceBuffs(hero)`。
- **敌人防御 Buff（审查后补）**：敌人没有防御属性，原来 `armor_break`（宠物 `sp_pick`）和敌人 `sp_katana_guard` 落在敌人身上不起作用。新增 `SkillService.GetEnemyDefenceFactor` / `FriendlyDebuffs.EnemyDefenceFactor`：敌人身上所有 `GlobalDefence × PercentMultiplicative` 按层数相乘（0.1～10），敌人受到的伤害 **÷ 系数**，在 `DamageCalculator` 敌人分支（非模拟）和 `ApplySkillDamageToEnemy` 各乘一次；`ApplyMods` 对敌人跳过这类修正，避免重复。
- **X3 自动施法**：
  - 宠物：`PetSpawnSystem` 生成时按 `FindLoadout(Pet, item.Key)` 装技能，回收前取消施法；复活时冷却重置。宠物死亡、宠物模式关闭、主角暂停（不能移动、倒下、睡眠）时不思考，正在前摇的被取消。施法期间停步、不普攻、普攻冷却暂停、保留目标；前摇播攻击动画（`isAttacking`），后摇停。主角瞬移走远时取消施法。不画预警圈（原来就只给敌人画）。
  - 主角：换主角单位时按 `FindLoadout(Hero, "hero")` 装，没有配置就不装（与宠物模式无关）。倒下取消施法，起身冷却重置；施法期间可以移动、不普攻、不被转向目标。
  - 新接口：`SkillService.IsCasting`、`IsInCastPoint`、`ResetCaster`、`CancelCast(UnitCore)`，`FriendlyDebuffs.IsCasting`。友方的 `Pick: 5`（Hero）= 活着且在射程内的主角；宠物的 `CurrentTarget` = `pet.CurrentTarget`。
- **Y 内容**：新建 `A/Resources/Skills/Species Skills.asset`（34 个 `sp_*` 技能、10 个新 Buff、36 个宠物 Loadout、26 个敌人 Loadout）；`Enemy Skills.asset` 只追加两处（见 11.3）；删掉 `pet_golem.asset` 的 `pet_skill_golem_slam`；本地化加 70 行。
- **Z 宠物界面**：`PetAbilitiesView.GetSpeciesSkills(ItemModel)`（宠物模式关闭、没有 Loadout 时返回空，按物品 Key 缓存，`[StaticReset]` 清）。技能弹窗第一行是种类技能（图标、名字、描述、"主动 · 冷却 N 秒"，总是解锁样式），摘要行把它算进图标和数量。本地化键先收成集合再查，缺键不刷日志。`ItemMiniView` 没改，自动生效。

### 11.2 配置约定

| 位置 | 约定 |
|---|---|
| `SkillLibrary.Loadouts` | `OwnerKind: 2`（Pet）+ `Key` = 物品 Key；`OwnerKind: 3`（Hero）+ `Key: hero`，默认没有；敌人仍是 `OwnerKind: 0` + prefab 名 |
| `Species Skills.asset` | 和 `Enemy Skills.asset` 一起被 `Resources.LoadAll` 合并；技能、Buff、Vfx、Loadout 的 Key 只能在一个文件里出现（重复会警告），引用可以跨文件 |
| 技能 Key | 种类技能 `sp_<物品Key>`；骷髅兵、石魔像直接共用 `bone_volley`、`slam` |
| 本地化 | `SKILL_NAME_<技能Key>`（施法条和宠物弹窗）、`SKILL_DESC_<技能Key>`（宠物弹窗，缺键时描述为空），13 列，zh 中文、其余英文 |
| 友方 Buff | 落在主角或宠物身上只认 11.1 的允许列表；想让宠物给友方加别的属性需要先扩列表 |
| 敌人 Buff | 防御只认 `GlobalDefence × PercentMultiplicative`（受伤 ÷ 系数）；其他属性仍按真实属性修正（敌人有这个属性才生效） |

### 11.3 种类技能和敌人侧

数值见 PhaseCSpec 第 2 节表格（11.4 列出偏差）。宠物侧 36 个种类全部有技能；敌人侧装配如下：

| 物品 Key → 敌人 prefab（技能） |
|---|
| paddle → `buck_buck_medium`；mailbox → `cut_buck_medium`；hammer → `buck_buck_boss`；mace → `cut_buck_knight`；coolHammer → `all_buck_knight` |
| knife → `cut_cut_easy`；minisaw → `cut_cut_medium`；katana → `pier_cut_medium upg`；doubleaxe → `cut_cut_boss`；chainsaw → `cut_cut_knight` |
| pike → `pier_pier_easy`；fork → `pier_pier_medium`；pick → `buck_pier_boss`；coolpike → `pier_pier_boss`；chainsawpike → `pier_pier_knight` |
| golem → `elite_buck`（slam）；cultist → `vip enemy`；bear → `elite_pier`；treant → `elite_cut green`；mage → `mage`；ghost → `ghost` |
| vampire_thrall → `Vampire_Easy`；vampire_blade → `Vampire_Medium+`；abyss_reaver → `undead_pierce_cut_elite`；dark_treant → `undead_cut_buck_elite`；zombie → `Zombie_Easy` |
| 在 `Enemy Skills.asset` 追加：skeleton → `Skeleton_Easy`（bone_volley，远程普攻不变）；zombie_brute → `Zombie_Medium+`（slam 之后加 sp_zombie_brute） |

**排除（只有宠物侧）**：skeleton_guard（`Skeleton_Medium+_Summoned`）、skeleton_king（`Skeleton_Boss_Summoned`）、boss_crab、boss_vampire_king、boss_hydra、boss_lich、boss_viking_king、boss_berserker（`Boss_*`）。

新 Buff：`stun_08`、`slow_50`、`sp_mace_fury`、`sp_fork_focus`、`sp_minisaw_haste`、`sp_katana_guard`、`sp_coolHammer_command`、`sp_skeleton_king_command`、`sp_boss_vampire_king_feast`、`sp_boss_berserker_frenzy`；复用 `slow_30`、`armor_break`、`stomp_stun`。除 `stun_08` 外都只用 `Damage`、`AttackCooldown`、`GlobalDefence`、`MovementSpeed` 的 `PercentMultiplicative`，落在友方身上不会被丢掉。

### 11.4 与规格不同的地方

- **主角移速仍是真实属性修正**（规格"一律乘系数"）：第二期主角移速就是修正，宠物跟随速度依赖它。
- **同一个 Buff 在主角或宠物身上，敌人施加的和友方施加的各算一份**，不合并叠层，免得两套允许列表混用。友方施加的眩晕被忽略（规格没写）。
- **宠物模式关闭时每帧清主角身上来自宠物的 Buff**；`DespawnAll` 仍只在 `Pets.Count > 0` 时执行，免得每帧清掉待复活记录。
- **主角整个施法（前摇 + 后摇）都不普攻**（规格只写前摇），和宠物一致；后摇只有 0.2～0.4 秒。
- **宠物前摇期间持续播攻击动画**（只用 `isAttacking`，不读技能自己配的动画参数），规格是"前摇开始时播一次"。
- **敌人防御 Buff 的生效方式是"受伤 ÷ 系数"**：`armor_break` ×0.7 ≈ 受伤 ×1.43，`sp_katana_guard` ×1.5 ≈ 受伤 ×0.67。主角和宠物仍是"防御值 × 系数"后进伤害公式，两边手感不同。
- **`sp_pick` 复用 `armor_break`，持续 5 秒**（规格 6 秒，规格同时要求复用）。
- **近战单体技能射程 3.5**（规格 3）：自动施法要求目标在 `Range` 内，3.5 和 `TargetDistance` 条件一致，手长的敌人也能放。近战命中特效高度 0.2（不是 1.2）。
- **共用技能不用 `Pick: 5`**，宠物不会把技能对准主角。
- **一个宠物 Loadout 有多个技能时弹窗每个一行**（规格按每种一个写）。

### 11.5 已知问题

- 全部没有在 Play 模式里看过：宠物施法动作、增益数字、弹窗布局、数值手感。
- 26 种战役普通敌人现在会放技能（治疗、增益、眩晕），战役难度需要重新调。
- `Skeleton_Easy` 也是 `raise_dead` 的召唤 prefab，所以 15 号岛 Boss 召唤的骷髅也会放 `bone_volley`。
- 主角施法时普攻冷却暂停，但武器挥动动画还在播，可能卡在半途或不同步。
- 走同一条命中流程的物品技能伤害也会乘输出伤害系数和敌人防御系数。
- 宠物弹窗的冷却是 `SkillDef.Cooldown` 原值，不带等级缩放；运行中改技能资产要等域重载或静态重置才显示。
- 数值模型和模拟器（`fromSimulation`）不读宠物技能、友方增益和敌人防御系数。
- `Main.unity` 当前没有改动，之前的冲突已经不在。

### 11.6 进 Play 要测的

1. **进入**：同 10.6 第 1 步（F8 → 快速测试 → 建测试副本 → 传送；「战斗」页只开「无敌」）。确认 `Resources/Pet Config.asset` 的 `enabled: 1`，在「装备与资源」页「宠物」区选种类、等级 1、勾「发放后出战」，点「发放选中宠物」。建议依次发：石魔像 `pet_golem`、锤骑士 `mace`、重甲矛兵 `pick`、战锤领主 `coolHammer`、阔剑卫士 `katana`。
2. **宠物施法**：遇敌后按冷却放技能，没有预警圈；前摇时停下播攻击动画、不普攻，后摇结束恢复追击和普攻。石魔像通过自动施法砸地，旧的 `pet_skill_golem_slam` 不再出现。
3. **友方增益**：`mace` 放技能后 6 秒内宠物普攻和技能伤害数字约 ×1.4，到期恢复；`minisaw` 攻击变快、动画同步；`katana` 期间宠物受伤变少；`coolHammer` 给 8 米内的主角和宠物回血并加伤害。主角属性面板（除移速外）不出现技能修正。
4. **破甲**：`pick` 的 `sp_pick` 命中后，敌人血条下出现减益图标，5 秒内主角和宠物打它的伤害约 ×1.43，之后恢复。
5. **敌人侧**：`buck_buck_medium`、`cut_cut_easy`、`pier_pier_easy` 等普通敌人带预警圈放各自的种类技能；`pier_cut_medium upg` 放 `sp_katana` 后 6 秒内受伤约 ×0.67 并回血；`Zombie_Medium+` 同时有 slam 和 sp_zombie_brute。
6. **打断与清理**：宠物前摇时被眩晕，技能取消且冷却已扣；宠物死亡后不施法，复活后冷却重置；卸下再装上重新装技能；主角对话、睡眠、倒下时前摇被取消。
7. **Boss 塔**：第二次进塔或波次复用时，竞技场小兵都看得见。
8. **宠物界面**：弹窗第一行是种类技能（图标、名字、描述、"主动 · 冷却 N 秒"），摘要行图标多一个、数量加一；背包小卡和图鉴同样；控制台没有缺键日志。
9. **关闭宠物模式**：没有任何宠物技能和种类技能行；主角身上来自宠物的 Buff 立即消失；主角技能配置为空时主角行为和以前一样。
10. **回归**：敌人施法、预警圈、施法条和第二期一样；加载时没有 `[Skills] Duplicate`/`Unknown` 警告。

## 12. 第三期实现记录（2026-10-29）

规格见 [Phase3Spec.md](Phase3Spec.md)。两个程序集编译通过（`check_final.sh` 与 `check_final.sh editor` 都是 exit=0），两个技能库的跨文件 YAML 检查（`SKILL_LIBS="Species Skills.asset|Enemy Skills.asset"`）和本地化检查通过，**没有在 Play 模式和 Unity 编辑器里验证过**。

### 12.1 做了什么

- **P1 位移效果**（`CB/Skills/Effects/`）：
  - `KnockbackEffect{Area, Distance, Duration, FromPoint}`、`PullEffect{Area, Distance, Duration}`、`BlinkEffect{Mode, Distance}`（新枚举 `BlinkMode`：BehindTarget/ToPoint/AwayFromTarget）、`DashEffect{Distance, Speed, StopAtFirstHit, Width, Power, PassThrough, Filter, [SerializeReference] OnHit}`。
  - 统一由 Morpeh 组件 `SkillForcedMovementComponent` + `SkillForcedMovement` 推进，`SkillAutoCastSystem.OnUpdate` 里调用 `SkillService.TickForcedMovement`，`SystemsInstaller` 没改。新位移覆盖旧位移；每步 `NavMesh.Raycast`/`SamplePosition`，失败停在边缘；地面单位用 `agent.Move`，飞行单位只做水平位移；敌人和宠物位移期间停寻路，结束后恢复 AI。
  - 免疫：`EnemyUnit.ImmuneToDisplacement` = `IsStatic`、`IsDummy`、`IsEliteBoss`、`IsBossFightPart`（小兵除外）、竞技场 Boss、寻路半径 × 缩放 > 1.2 米。主角和宠物只接受敌人施加的位移，和眩晕共用 `IsHostileSourceOnFriendlyTarget`；友方来源 `WarnOnce` 后忽略。对主角的单次位移最远 3 米。
  - 冲锋：施法者沿锁定方向移动，路径上每个单位只伤害一次并执行 `OnHit`。`PassThrough=false` 时路上任何活着的身体都会挡住；`StopAtFirstHit` 撞到第一个命中目标就停。冲锋期间 `IsCasting` 为真（不普攻、不开新技能）；施法者死亡、回收（`OnDisable`、`InstallForEnemy`、`CancelCast`/`ResetCaster`）或被眩晕时中止；目标死亡时中止加在它身上的位移；宠物死亡两者都清。被推着的单位不能开新技能，击退和拉近不打断正在进行的施法或引导。
- **P2 引导**：`SkillDef.ChannelDuration`、`ChannelTickInterval`、`[SerializeReference] OnChannelTick`。顺序：前摇 → `Effects` 一次 → 第 k×间隔 秒执行 `OnChannelTick` → 后摇。间隔 ≤ 0 时只在结束时跳一次，最小间隔 0.05 秒。眩晕、沉默、死亡和主角/宠物"能否行动"检查会打断。敌人引导时预警圈满进度并锁定（`SkillTelegraphView.LockPosition`）；主角和敌人引导时不动；`EnemySkillStatusView` 引导阶段进度条由满到空。
- **技能等级**：`PetStats.GetSkillLevel(item)` = `1 + floor((item.Level - 1) / 3)`，进化后 +2，限制在 1～10（`MaxSkillLevel`）。`item.Level` 是进化等级（未进化为 1），所以未进化宠物是 Lv 1，进化 1～3/4～6/7～9/10～12/13～15 级分别是 Lv 3/4/5/6/7。`SkillDef.GetLevelScale` 在 `LevelScale` 为空时用默认曲线 `1 + 0.08 × (等级 - 1)`；`SkillService.Install(..., levelOverride)` 给宠物所有槽位统一等级，`SetSkillLevel(unit, level)` 只改等级、不重装、不重置冷却。`PetModel.SkillLevel` 记录已应用的等级，`PetSpawnSystem` 每帧比较，变了就刷新。宠物弹窗主动技能行显示"主动 · Lv N · 冷却 X 秒"。敌人、Boss、主角仍用 `slotDef.Level`（资产里全是 1）。
- **编辑器**（`A/Editor/Skills/`，全部新建）：
  - `SkillLibraryEditorWindow`（`Tools/技能/技能库编辑器`）：选库、刷新、定位、保存、校验；技能/Buff/配置/特效/召唤五页，可搜索（技能页也按中英文名和效果类型名）；新建、复制（新 rid）、删除（确认）；底部校验结果可筛选，点击跳到对应库、页、条目和字段。
  - `ManagedReferenceGUI`：`[SerializeReference]` 字段和列表的类型下拉（`TypeCache`，按命名空间分组），增删、上下移、换类型（同名字段带过去），嵌套列表自动支持；丢失类型显示红框和"移除"。普通类列表也能增删移动。
  - `SkillLibraryValidator`（窗口按钮和 `Tools/技能/校验技能库`）：空 Key、文件内和跨文件重复、未知 Buff/Vfx/Summon/Skill 引用、Loadout 对不上 prefab（需有 `EnemyUnit`）/`BossTowerConfig`（警告）/`PetConfig.ItemKey`/`hero`、缺 `SKILL_NAME_`（警告）和 `SKILL_DESC_`（宠物或主角用的技能警告，其余提示）、丢失类型和空元素。
  - `SkillLibraryInspector`：打开编辑器、校验按钮和各页条数，下面用同一套绘制器。
- **其他**：`AbilityIcons` 也看 `OnChannelTick`，`sp_mage` 仍是伤害图标。

### 12.2 内容更新

| 技能 | 改动 |
|---|---|
| `sp_chainsaw` | rid 2102 改为 `DashEffect`：8 米、速度 18、宽 1.5、Power 1.8、PassThrough、Filter 65；`OnHit` 击退 1.5 米 / 0.2 秒。预警线 8×1.5 不变 |
| `sp_ghost` | 先 `BlinkEffect`（目标身后 0.5 米）再单体伤害 Power 2.5；Range 和距离条件 3.5 → 5 |
| `sp_hammer` | 伤害之后 3 米内击退 2 米 / 0.25 秒 |
| `sp_mage` | `Effects: []`，引导 2 秒、间隔 0.4 秒，`OnChannelTick` 每次一支直线飞弹 Power 0.6（共 5 支，合计 3.0，原来一支 1.8） |
| `charge`（新，`Zombie_Boss`） | Range 7、前摇 1.0、后摇 0.5、冷却 12 / 初始 6、直线预警 7×1.5、Priority 13；目标 3～7 米才放；`DashEffect` 7 米、速度 16、撞到第一个就停、Power 2.0；`OnHit` 击退 2 米 |

本地化：四个种类技能的 `SKILL_DESC_` 更新，新增 `SKILL_NAME_charge`（Charge / 冲撞），13 列。

### 12.3 有意没做的

- **Boss 塔原有机制不迁到技能层**（`BossSpawnService.ApplyBehaviors` 的狂战士、熔岩蟹、巫妖等）：这些战斗已调好，迁移收益小、回归风险大。需要时用 `Boss` 类型 Loadout 额外加技能，原机制不动。
- **数值模型和模拟器不接技能**：在 `Balance/`，是另一条进行中的工作，按规定不碰。模拟器不读位移、引导和技能等级。

### 12.4 与规格不同的地方

- **技能等级**：先按宠物等级（`PetStats.GetLevel`，按收集份数算）实现，审查发现约 28 份就封顶、进化 +2 永远不生效，已改回规格的 `item.Level`（进化等级）。代价是升级（加份数）不改变技能等级，只有进化会；实际最高 Lv 7，上限 10 只是保险。
- **冲锋命中宽度**：有直线预警时，命中半径不超过预警半宽（与 `SkillLinearMissiles` 同规则），身体接触只有在这个宽度内才算命中，宽度外只挡路。冲锋不命中 `IsStatic`（树）和 `IsFar` 的敌人，它们只参与挡路。
- **友方引导每跳重新瞄准**：主角和宠物引导时每跳朝活着的目标重新算方向和落点，目标死了就按自动施法规则在射程内另找一个（宠物同时转身）；敌人引导仍锁定方向，和预警一致（规格只要求敌人锁定）。
- **`SkillService.CancelCast` 也取消 `EnemyCastSystem` 状态**（只对激活的敌人）。被击退的主角和宠物仍可普攻，只是不接受摇杆、不开新技能。
- **`sp_ghost` 射程放大到 5**，`charge` 的速度、冷却、3 米最小距离、Priority 都是自定的；敌人技能没有 `SKILL_DESC_`，所以没加 `SKILL_DESC_charge`。
- 编辑器的可选检查（技能没有效果、Buff 设了 `TickInterval` 但没有 `OnTick`）没做。

### 12.5 已知问题

- 全部没有在 Play 模式和编辑器里看过：位移手感、撞墙和台阶边缘、引导节奏、窗口布局。
- `Species Skills.asset` 里装了这四个技能的敌人也会用新版本：`cut_cut_knight` 冲锋、`ghost` 闪到主角身后、`buck_buck_boss` 把主角击退 2 米、`mage` 引导飞弹。战役难度需要复查。
- 冲锋每帧扫一遍全部敌人（`Contains` 是 O(n²)）并分配一个小列表，有性能问题再优化。
- 高空飞行敌人闪现时 1.5 米的导航网格采样可能失败，闪现不生效。
- 编辑器"保存"会让 Unity 重新序列化 YAML，rid 和格式可能变化；有人手改 YAML 时不要在编辑器里保存。增删、换类型推迟到绘制结束执行，偶尔可能有一次布局警告。
- `AbilityIcons` 不单独识别位移效果（这几个技能都带伤害，所以仍是伤害图标）；`PetAbilitySummaryRow` 不显示等级；物品能力里放的技能仍是 1 级。
- 宠物弹窗冷却仍显示 `SkillDef.Cooldown` 原值。

### 12.6 要测的

**Play 模式**（进入方式同 11.6 第 1 步）：
1. **冲锋**：`chainsaw` 宠物沿直线冲 8 米，每个敌人只挨一次并被推 1.5 米；Boss、静态、假人、大体型不被推；穿过树林时树不掉血。
2. **闪现**：`ghost` 宠物闪到目标身后再打；靠墙或网格边缘时不穿出去。
3. **震地**：`hammer` 宠物把 3 米内敌人推开 2 米。
4. **引导**：`mage` 宠物 2 秒发 5 支飞弹，目标横向移动时后面的飞弹跟着改方向，目标死了转打射程内另一个；被眩晕时停止。
5. **Zombie_Boss 冲撞**：1 秒直线预警，冲出、撞到主角就停，主角被推 ≤ 3 米；站在预警框外（哪怕只差一点）不挨打；冲撞途中眩晕或打死它，冲锋立即停。
6. **位移恢复**：宠物被推后恢复跟随和追击；敌人冲锋途中被回收，再出场没有残留状态；宠物施加的位移落在主角身上无效。
7. **技能等级**：1 级、未进化宠物伤害和改动前一样；升级不变，第一次进化后弹窗显示 Lv 3、伤害 ×1.16、冷却没被重置；进化到 4 级显示 Lv 4。敌人技能伤害不变。
8. **敌人引导**：带引导的敌人技能预警圈保持显示，施法条由满到空。
9. **关闭宠物模式**：没有宠物技能；主角技能配置为空时和以前一样。

**编辑器**：
1. 打开 `Tools/技能/技能库编辑器`，两个库都能列出，切页、搜索正常。
2. 在 `sp_mage` 上"添加…"效果（能看到新位移效果），换类型、上下移、删除、Ctrl+Z；在 `MissileEffect.OnImpact` 里嵌套添加。
3. 复制一个技能并保存，YAML 里复制品是新 rid。
4. 点"校验"无错误；故意改错一个 `BuffKey` 会报错，点击结果能跳转。
5. 选中库资源，Inspector 有按钮和类型下拉。
