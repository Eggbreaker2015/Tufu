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

