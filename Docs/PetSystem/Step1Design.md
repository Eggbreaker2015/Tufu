# 第一步技术设计：武器系统改为宠物系统

> 状态：草案，待用户确认第 13.1 节后实施。分支 `feature/pet-system`。本文由 7 组子系统分析加对照代码的核查和审查整理而成，引用的行号对应提交 `0dac6e5`。

> 路径约定：`CodeBase/` = `/Users/joyfort/MyProjects/Tufu/Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/`，`Assets/` = `/Users/joyfort/MyProjects/Tufu/Recovery/StudyProject/Assets/`。
> 方法体可信度标记：
> - **【原生】**：带 `Native ELF VA` 注释的恢复体。
> - **【重建】**：无 VA 注释、手工重建的方法体，可以运行，但不保证与原版一致。
> - **【空桩】**：方法体为空。

---

## 1. 目标与范围

### 1.1 用户已决定的事项（第一步要做）

| # | 决定 |
|---|---|
| 1 | 去掉原作武器的攻击方式，换成宠物。获得一件武器就等于获得一只宠物。宠物是独立单位，会死亡。 |
| 2 | 宠物外观先复用游戏里现有敌人 prefab 的模型和动画。 |
| 3 | 每只宠物带一种伤害类型，沿用原武器的穿刺、斩击、钝击（Stabbing=0 / Cutting=1 / Buckshot=3）。 |
| 4 | 原来判断武器的能力条件，改成判断宠物。 |
| 5 | 宠物沿用原武器的稀有度、碎片和进化。 |
| 6 | 玩家自己也有攻击。 |
| 7 | 宠物上限等于武器槽：weapon_1~4，外加 weapon_5_vip。 |
| 8 | 怒气的"多打一下"同时作用于宠物和玩家。 |
| 9 | 宠物死后冷却一段时间自动复活，或者回到泉水复活。 |
| 10 | 敌人仇恨：玩家和宠物都可以成为目标。先这样做，用户体验后再调。 |
| 11 | 宠物继承玩家"杀怪获得或直接获得"的属性，不包括装备提供的属性。 |
| 12 | 其他保持与原作一致。 |

### 1.2 第一步不做

- 玩家自有的额外装备系统（第二步）。头盔、盾牌、水晶维持原作逻辑，仍然是玩家的护甲和装备，不变成宠物。
- 离线模拟和收益预测（`SimulationSystem`）不改，接受数值漂移，见第 13 节。
- 新的宠物美术、宠物专属 UI 面板、宠物属性详情页。
- 宠物状态（血量、死亡、复活倒计时）不存档。
- Creeper、FireElemental、Assassin 这类范围攻击本来就是【空桩】，不处理。

### 1.3 设计原则

**宠物的数据仍然是那件武器 `ItemModel`，只换掉"怎么攻击"这一层。** 物品、槽位、稀有度、进化、能力、存档全部不动。新增一个 `PetUnit` 单位作为攻击执行者和受击者。

---

## 2. 现状要点（只列与改动相关、已核实的事实）

### 2.1 攻击管线

- 玩家没有独立于武器的攻击。玩家发出的 `WeaponHitEvent` 只来自 `PlayerAttackSystem.Attack` 里对 `EquippedWeapons` 的 foreach。全局只有三个生产者：`PlayerAttackSystem.cs:182`、`EnemyAttackSystem.cs:346`、`LavaCrabExplosionSystem.cs:116`。见 `CodeBase/Player/PlayerAttackSystem.cs:159-190`【原生 0x1441034】。
- 选目标的规则是取 `EnemiesModel.Enemies` 里第一个"非 IsFar 且在射程内"的敌人，不是最近的。所有武器共用 `PlayerUnit.CurrentTarget`，目标出射程后保留 0.2s。见 `PlayerAttackSystem.cs:97-118, 213-223`。
- 每件武器的冷却状态机存在 `ItemModel` 上，见 `PlayerAttackSystem.cs:119-148, 160-181`：
  - 字段：Cooldown、AttackInProgress、JustStartedAttack、ResetCooldown、WearingTime。
  - 出手条件：`Cooldown <= 0` 时开始挥击，并把 Cooldown 设为 `AttackCooldown`。
  - 命中时机：Cooldown 越过 `castPoint × AttackCooldown` 时命中。
  - 装备不满 1s（`WearingTime < 1`）不攻击。
  - weapon_1 只用基础射程，链槽（weapon_2~5_vip）对弱敌和金像可以用 ×3 射程。
- 出手点会随移动变化：`GetAnimationCastPoint` 读的是静态字段 `animationCastPointSpeedCoefficient`，由玩家摇杆驱动，见 `PlayerAttackSystem.cs:61, 93-95, 206-211`。宠物复用它，会让宠物的出手节奏跟着玩家走路变化。
- 命中时机不依赖动画事件。`WeaponView` 和 `PlayerWeaponView` 只负责表现，见 `WeaponView.cs:436-468`。在 `Pudge.prefab` 上：
  - weapon_1 用角色 Animator 的 `attackR` 和 `handRSpeed`，见 `Assets/GameObject/Pudge.prefab:12644-12651`。
  - weapon_2~5_vip 是触手链，见 `Pudge.prefab:12692-12843`。
- `ItemConfig.Prefab` 的类型是 `WeaponView`，不能直接放敌人 prefab，见 `CodeBase/Player/Inventory/ItemConfig.cs:41-43`。

### 2.2 伤害结算

- `UnitDamageSystem.ApplyDamage` 只按 `Target is EnemyUnit` 分流，其他目标一律当作玩家处理，见 `CodeBase/Unit/Systems/UnitDamageSystem.cs:103-108`【原生 0x15cd754】。玩家分支里有这些逻辑，宠物被打时如果走这个分支，全部会被误触发，死亡时还会抛异常：
  - 格挡和超暴加成：118-124
  - 荆棘：124
  - 闪避触发：125-133
  - `HitsTakenCount`：140-141
  - 免死：145
  - 死亡时 `(PlayerUnit)` 强转：149
  - `OnGetHitLowHealth`：152
- `DamageCalculator` 对敌人的分支会无条件解引用 `data.Weapon`，`Weapon == null` 会抛 NRE，见 `CodeBase/Services/Damage/DamageCalculator.cs:128-169`【原生 0x140a4cc】。`PlayerDamageEventsViewSystem.cs:43-44` 用 `damageViews[data.Weapon.SlotIndex]` 取伤害数字视图。
- 输出伤害公式在 `DamageCalculator.cs:140`：
  `percentStat(W.DamageStatTypePercent) × (Src.Damage + W.Damage + Src.flat(W.DamageStatTypeFlat) + PlayerModel.AbilityDamageFlat + W.AbilityDamageFlat) × (1 − resist[W.DamageType])`
  之后依次乘：塔加成、暴击、超暴、能力倍率、GlobalAttack、岛屿精通、眩晕和感电加成、闪避，见 `DamageCalculator.cs:123-205`。所有攻击方属性都取自 `data.Source`。
- 受击分支（目标不是 EnemyUnit）的问题，见 `DamageCalculator.cs:69-121`：
  - 会加上玩家 `EquippedArmors`（头盔、盾牌）的护甲，见 `:83`。
  - 会套用玩家的怒气减伤，见 `:105-110`。
  - 防御公式：`a·exp(−0.75d/a)·max(0,1−0.25d/a)`，其中 `d=(UniversalDefFlat+armor+typedDefFlat)×typedDefPercent`，见 `:24-29, 87`。
  - `:111` 的 `GetEnemyEventDamageMultiplier(target, attacker)` 要求攻击方是 EnemyUnit。
- 吸血治疗的是 `data.Source`，上限是 Source 的最大生命，见 `UnitDamageSystem.cs:336-366`。以下两处只在 `Source is PlayerUnit` 时生效：
  - 敌人反伤：`UnitDamageSystem.cs:255`，`:276` 处还有强转。
  - 穿刺印记：`CodeBase/Abilities/Systems/PiercingWeaponMarkSystem.cs:33`。

### 2.3 物品、槽位、进化

- 武器共 30 个 ItemConfig：15 个基础武器（5 种稀有度 × 3 种类型）和 15 个 `_div` 遗留品。`_div` 遗留品的 Rarity=5，没有进化数据，`CanEvolve` 会抛异常，无法获得。见 `Assets/MonoBehaviour/Items Provider.asset`、`CodeBase/Balance/CombatBalanceConfig.cs:210-214`。
- 伤害由 `ItemModel.Damage` 计算，不存档，见 `CodeBase/Player/Inventory/ItemModel.cs:404-518`：
  - 进化前：`growth × Copies + FlatDamage`。
  - 进化后：`EvolutionDamage(Level) + CopyValue × Copies + FlatDamage`。
  - 进化等级没有上限，数值在 15 级封顶，见 `CombatBalanceConfig.cs:147-150`。
- 能力解锁：未进化只解锁 `UnlockLevel==0` 的能力；进化后解锁 `UnlockLevel <= Level` 的能力，见 `ItemModel.cs:520-523, 561-573`。knife、pike、paddle 等 8 种基础武器在进化前没有任何激活能力。
- 槽位配置见 `Assets/MonoBehaviour/Player Config.asset:76-106`：
  - weapon_1 默认解锁。
  - weapon_2：岛 1 Boss 掉落（同时掉 pike），见 `Assets/Scenes/Main.unity:3375769-3375801`。
  - weapon_3、weapon_4：任务奖励。
  - weapon_5_vip：VIP 4 级解锁，见 `CodeBase/VIP/Features/ExtraWeaponSlotFeature.cs:22-32`。
  - 起始武器是 knife（cut common）。
- 获得物品：`AddItem` 遇到同 Key 的物品会合并 Copies；新物品自动装进同类型的第一个空槽，见 `CodeBase/Player/Models/PlayerModel.cs:705-747`。
- 存档：`ItemDataUd` 只存 `{key, level, copies, evolved}`，见 `CodeBase/Player/Models/ItemDataUd.cs:19-22`。读档用 `ItemsProvider.Get`，它内部用 `First()`，Key 找不到就抛异常，见 `CodeBase/Player/Inventory/ItemsProvider.cs:13-16`。

### 2.4 属性与修饰来源

- `ModifierSource` 枚举：Enemy=0、Item=1、AbilityPermanent=2、AbilityTemporary=3、Shop=4、Ads=5、Reward=6、LevelPass=7、Crystal=8、CrystalUpgrade=9、GraphUpgrade=10、…、SecretChest=16、Quest=18，见 `CodeBase/Unit/ModifierSource.cs:3-25`。
- 88 个物品配置的 `StatModifiers` 全部为 `[]`。装备给的属性实际只有三种来源：
  - `ItemModel.Damage`
  - 头盔和盾牌的 `ItemModel.Armor`
  - 物品能力里的 `AddStatEffect` / `AddStatPassiveEffect`。这些修饰的 Source 是 1、2 或 3，名字等于 `AbilityDef.Key`，见 `CodeBase/Abilities/Effects/AddStatEffect.cs:31-50`【原生 0x15ec76c】。
- 统计：`AddStatEffect` 中 Source1 有 44 个、Source2 有 16 个、Source3 有 12 个；`AddStatPassiveEffect` 中 Source1 有 4 个、Source3 有 25 个。以 coolpike_div 为例，它 OnEquip 给的 +20% StabDamagePercent 用的是 Source3。因此只排除 Source==Item 不足以排除"装备属性"。
- 水晶（Source 8）也会写到玩家属性上，见 `CodeBase/Crystals/CrystalModel.cs:1165-1177`。
- 杀怪属性来自吞噬，不是击杀：`EnemySpawner.HandleDevour → GrantSpawnerStatRewardCommand → PlayerRewardSystem.GrantStatReward`，Source=Enemy，见 `CodeBase/Enemy/EnemySpawner.cs:1359-1384`【重建】、`CodeBase/Player/Systems/PlayerRewardSystem.cs:40-84`【原生 0x1456084】。其中 Enemy 来源的收益已经乘过物品能力 DoubleLoot，事后无法分离，见 `PlayerRewardSystem.cs:56-63`。
- `StatBase.AddModifier` 按 (Name, Source, ModifierType) 原地合并，没有公开的变更事件，见 `CodeBase/Unit/StatBase.cs:63-105`【重建】。`StatModifier` 没有 Clone 方法，见 `CodeBase/Unit/StatModifier.cs:7-35`。
- **共享列表陷阱**：`PlayerConfig.Stats[i].Modifiers`、`LastStats[i].Modifiers`、`Unit.Stats[i].Modifiers` 是同一个 List 实例，见 `PlayerModel.cs:524-526, 690`。宠物从 Player Config 取基础值时，绝不能传原来的 Modifiers 引用。
- `UnitCore.GetStatValue` 遇到缺失的属性返回 0，见 `CodeBase/Unit/UnitCore.cs:85-114`【原生】。`statLookup` 在 `Start` 或 `BuildStatLookup` 之前是 null。Player Config 里各类型伤害百分比和防御百分比的基础值是 1，所以它们相当于乘子。
- `PlayerModel.SaveStats` 不保存 Source 1、3、8、10，见 `PlayerModel.cs:654-675`【原生】。

### 2.5 能力系统

- `ItemModel` 是唯一的 `IAbilityOwner`，见 `ItemModel.cs:16`。
- 触发器分两类，见 `CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs:222-227`：
  - OnEquip、OnHit、OnCrit、OnSuperCrit、OnCooldownReset、OnPreHit 只发给 `data.Item`。
  - 其他触发器（OnPostHit、OnKill、OnDevour 等）发给所有已装备物品。
- 每帧重置 Ability* 字段只覆盖 `EquippedItems`，见 `AbilityExecuteEventSystem.cs:52-87`。`ItemModel` 的倍率字段构造时默认是 0。
- `AbilityContext` 没有攻击者字段，`PlayerUnit` 固定取 `PlayerModel.Unit`，见 `CodeBase/Abilities/Core/AbilityContext.cs:15-36`。有一批条件和效果在拿目标时回退到 `PlayerModel.Unit.CurrentTarget`。
- `GrantRageAccessEffect` 根据 `EquippedItems` 计算怒气上限，只有 chainsaw 会给，见 `CodeBase/Abilities/Effects/GrantRageAccessEffect.cs:23-33`。
- 怒气只有一个来源：`AddRageEffect`，见 `AddRageEffect.cs:30`。

### 2.6 怒气"多打一下"

- `RageExtraHitSystem` 累计怒气到阈值后，给 `item.Damage` 最高的那件武器置 `ResetCooldown`，并触发 OnCooldownReset，见 `CodeBase/Player/Systems/RageExtraHitSystem.cs:26-59`【重建，无 VA】。
  - 阈值来自元属性 `rage_extra_hit`，只由护符升级 `graph_rage_extra_hit_7_21` 给出，分别是 150、120、90，见 `CodeBase/Amulet/Upgrades/AmuletUpgradeDefsBuilder.cs:1698-1703`。
  - 这个系统并不新增一击，只是让该武器提前出手：闲置时 0.15s 后命中，挥击中途则命中后立刻开始下一击，见 `PlayerAttackSystem.cs:142-147, 177-181`。

### 2.7 敌人 AI

- `EnemyAttackSystem` 是场景里的 MonoBehaviour，不是 Morpeh 系统，见 `CodeBase/Enemy/EnemyAttackSystem.cs:16, 52`【重建，无 VA】。
- 目标写死为 `playerModel.Unit`，在 `:31-37` 定义，`:107、149、162、192、224、273、286、310、318、346` 处使用。
- 仇恨规则，见 `:111, 137-139, 277, 283-285`：
  - 玩家在 AggroRange 内时刷新 AggroTimer。
  - 敌人是玩家的 CurrentTarget 时保持仇恨。
  - 受击不会引起仇恨。
- 其他规则：
  - 敌人离玩家超过 30m 会回满血，见 `:108-109, 274-275`。
  - 玩家睡眠、死亡中或 `!CanMove` 时，敌人全部回巡逻点，见 `:110, 276`。
- 没有投射物，远程敌人只是 AttackRange 更大，见 `:140-142, 342-348`。
- 唯一恢复出来的范围攻击是 LavaCrab，它只检测玩家，见 `CodeBase/BossTower/Systems/LavaCrabExplosionSystem.cs:105-128`。

### 2.8 单位与 ECS

- `UnitCore.Start` 注册 `UnitComponent`、`NavMeshAgentComponent`、`NavMeshAgentActiveMarker`、`PositionComponent`、`RotationComponent`。是否打 `EnemyTag` 或 `PlayerTag` 只看 C# 类型，见 `UnitCore.cs:63-83`【原生 0x15cb2f4】。
- 107 个敌人 prefab 的根节点都挂着 NavMeshAgent、UnitMovement、EnemyUnit、EntityProvider。直接实例化会变成未初始化的敌人，`EnemyUnit.Tick` 会 NRE，见 `CodeBase/Enemy/EnemyUnit.cs:142-188`。
- 模型挂在根节点下名为 `Visual` 的子物体上，其中有 Animator（applyRootMotion=1），见 `Assets/GameObject/pier_pier_easy.prefab:33, 4513-4526`。
- 动画参数：`isWalking`、`isAttacking`、`isDead`、`Spawn`，攻击动画速度为 `1/AttackCooldown`，见 `EnemyUnit.cs:17-21, 198-215, 313-316`。龙类用的是另一套参数（`Walk Forward` 和 `Bite Attack`）。
- 以下各处对"非玩家单位"会强转成 EnemyUnit：
  - `UnitDeathAnimSystem.cs:32-33, 49`（需要 `UnitHasDeathAnimMarker`）
  - `UnitSpawnAnimSystem.cs:25, 42`
  - `UnitBuffSystem.cs:70`（眩晕）
  - `HealthBar.RenderDrops`，见 `CodeBase/UI/World/HealthBar.cs:237-243`
- 只认标签的系统：
  - 只认 PlayerTag：`PlayerRegenSystem`、`PlayerReviveSystem`。
  - 只认 EnemyTag：`EnemyRegenSystem`、`EnemyHealthBarSystem` 等。

### 2.9 击杀归属与吞噬

- 击杀奖励与攻击来源无关。`ProcessEnemyDeath → UnitCore.OnDied → EnemySpawner.HandleDeath`，发放掉落、槽位、门、OnKill，并触发静态 `OnDied`，见 `UnitDamageSystem.cs:203-210`、`EnemySpawner.cs:1173-1247`【重建】。
- `PlayerDevour` 订阅静态 `OnDied`，不判断距离，带超时保底，最终调用 `OnDevoured`，见 `CodeBase/Player/PlayerDevour.cs:81, 102-123`【原生】。
- 结论：宠物击杀会自动给玩家吞噬成长，不需要改。

### 2.10 生命周期

- **泉水已经存在**：就是 `FirecampSystem`，场景里只有一个，GameObject 名为 `HealField`，见 `Main.unity:3256730-3256748`，参数 `Radius 3.05`、`Regen 0.12`。它的行为见 `CodeBase/Buildings/FirecampSystem.cs:34-53`【重建】：
  - 玩家在半径内且 4s 没受伤时，每秒回 12% 最大生命。
  - 玩家死亡后在这里复活。
  - 只治疗 `playerModel.Unit`。
- 玩家死亡流程：
  1. `ProcessPlayerDeath` 处理死亡，见 `UnitDamageSystem.cs:164-174`。
  2. `PlayerReviveSystem` 接手，在竞技场内跳过，见 `CodeBase/Player/Systems/PlayerReviveSystem.cs:26-37`。
  3. `DeathSequenceCoroutine` 播放死亡和复活过程，见 `CodeBase/Player/PlayerUnit.cs:443-498`。
  4. `RevivePlayerCommand` 把玩家传到泉水，生命设为 1，重置怒气，所有敌人回满血，有仇恨的敌人 `RetreatTimer=3`，`SetCurrentLocation(1)`，见 `CodeBase/Player/Commands/RevivePlayerCommandHandler.cs:26-43`。
- 正常游玩中没有换岛传送：各岛在同一块 NavMesh 上，开门只是禁用 NavMeshObstacle，见 `CodeBase/Gates/Gate.cs:58-71`。
- 真正的传送：
  - 进入竞技场：`EnterBossArenaCommandHandler.cs:27-77`
  - 离开竞技场：`ExitBossArenaCommandHandler.cs:16-29`、`BossRaidSystem.cs:299-309`
  - 竞技场重试：`StartNextFloorFlowRunner.cs:81-91`
  - 离开 NavMesh 的兜底：`PlayerNavMeshFallbackSystem.cs:25-36`
  - 活动门：`EventBossGateSystem.cs:168-177`
- 共有 11 处流程会设 `CanMove=false`：开门、传送门、任务对话和过场、宝箱、水晶、宝箱队列。`CanMove=false` 实际相当于战斗暂停。

---

## 3. 总体方案

### 3.1 数据层：宠物就是武器 ItemModel（不改）

- 每个已装备的武器槽（`SlotType == Weapon`，含 weapon_5_vip）对应一只宠物，宠物上限等于已解锁的武器槽数，见 `PlayerModel.cs:213-221`。
- 宠物的数据都取自它的物品：伤害类型为 `Config.DamageType`，攻击力为 `ItemModel.Damage`，攻速为 `Config.AttackCooldown`，能力为 `ItemModel.AbilityStates`，稀有度和进化完全沿用。
- **宠物死亡时物品保持装备**。如果死亡时卸下，会丢失怒气权限和被动能力，还会触发 OnEquip 移除，见 `GrantRageAccessEffect.cs:23-33`、`PlayerModel.cs:768-806`。
- 不改 ItemsProvider、进化命令和流程、槽位解锁、存档结构。

### 3.2 外观映射

新建 `PetConfig`（ScriptableObject，`Assets/MonoBehaviour/Pet Config.asset`），其中有映射表 `List<PetVisualEntry { string ItemKey; EnemyUnit Prefab; }>`，另有按 `DamageType` 的兜底项。

- 不改 30 个武器 asset，以后换正式美术只需要改一个文件。
- 用 `EnemyUnit` 作为引用类型，和 `EnemySpawnerConfig.Prefab` 的用法一致，见 `CodeBase/Enemy/EnemySpawnerConfig.cs:19`。
- **只实例化敌人 prefab 里名为 `Visual` 的子物体**，挂到宠物根节点下。不能实例化根节点，原因见 2.8。实例化后：
  - 把 Animator 的 `applyRootMotion` 设为 false，和 `EnemyUnit.cs:179-181` 的做法一致。
  - 映射时避开龙类 prefab，或者为它们单独适配参数名。
  - 第一步避开带 Boss 专用 provider 的 prefab，例如 BossVisualProvider、CreeperMobProvider；也排除树木和静态 prefab（`tree_*`、`ice_pike_blocker` 等）。
  - **体型**：很多敌人的缩放写在根节点上（例如 `buck_pier_medium` 1.3、`buck_buck_boss` 2），只拿 `Visual` 会丢失体型。实例化后把敌人根节点的 `localScale` 复制到 VisualSlot，并把 Visual 的局部位姿归零，与 `EnemyUnit.cs:156-157` 的做法一致。
- 15 个基础武器分别对应哪个 prefab，需要用户选，见第 13 节。

### 3.3 宠物单位与 ECS

新建 `CodeBase/Pet/PetUnit.cs : UnitCore`，它既不是 EnemyUnit 也不是 PlayerUnit。

- 字段：`ItemModel Item`、`EnemyUnit CurrentTarget`、`Animator Animator`、`Transform VisualRoot`。
- 在 `Start()` 里先调 `base.Start()`，再加 `PetTag`。`PetTag` 参照 `CodeBase/Unit/Components/PlayerTag.cs` 写。
- `UnitCore.HealthBarPoint` 和 `Agent` 是 `private set`（`UnitCore.cs:28, 31`），需要在 `PetRoot.prefab` 上序列化赋值或在 Awake 里获取。
- 受击闪白要覆写 `virtual ApplyHitAnimation`（`UnitCore.cs:134`），否则不会有表现。`SetupHealthBar` 内部已经注入血条（`UnitCore.cs:120-121`），宠物只需额外 `InjectInto` 自己。

宠物根 prefab 新建为 `Assets/Prefabs/Pet/PetRoot.prefab`，结构如下：

```
PetRoot
├─ EntityProvider (Morpeh)        // UnitCore.Awake 需要
├─ NavMeshAgent (AgentType 0, radius 0.5)   // Pudge radius 1，宠物取小值，避免把玩家挤开
├─ PetUnit
├─ HealthBarPoint
└─ VisualSlot                     // 运行时放入敌人 prefab 的 Visual 子物体
```

ECS 组件的约束：

| 是否挂 | 组件 | 说明 |
|---|---|---|
| 挂（UnitCore 自动添加） | UnitComponent、NavMeshAgentComponent、NavMeshAgentActiveMarker、PositionComponent、RotationComponent | |
| 挂（新增） | PetTag | |
| 死亡时挂 | UnitDeadMarker | 已核实：`UnitDeathAnimSystem` 需要同时有 `UnitHasDeathAnimMarker` 才会处理，`PlayerReviveSystem` 需要 PlayerTag，所以宠物挂它是安全的 |
| **禁止** | EnemyTag、PlayerTag、UnitHasDeathAnimMarker、UnitHasSpawnAnimComponent、BuffStunComponent、HasTemporaryModifiersTag | 前五个会进入敌人或玩家的筛选和强转。最后一个会让 `UnitBuffSystem` 重复扣减克隆出来的临时修饰的 Duration |

宠物**不加入 `EnemiesModel`**，否则玩家的选目标逻辑会选中宠物，见 `PlayerAttackSystem.cs:213-221`。

运行时实例化宠物时，`Boot.InjectScene` 已经执行过（只执行一次），所以需要手动调用 `Injector.InjectInto(petUnit)`，见 `CodeBase/Core/Injector.cs:110-113`、`CodeBase/Core/Boot.cs:101`。

### 3.4 运行时模型与生成

- `CodeBase/Pet/PetModel.cs`（每只宠物一个）：`ItemSlotModel Slot`、`ItemModel Item`、`PetUnit Unit`、`PetState State {Alive, Dead}`、`float ReviveAt`。
- `CodeBase/Pet/PetsModel.cs : Model`：持有宠物列表，提供 `Get(ItemModel)`、`AlivePets`、`WarpAll(Vector3)`、`ReviveAll(Vector3)`。在 `Boot` 里用 `RegisterModel` 注册，排在 PlayerModel 之后，见 `Boot.cs:24-123`。
- `PetSpawnSystem` 每帧比对 `playerModel.ItemSlots` 里武器槽的"是否装备"和"物品 Key"，做法参照 `PlayerWeaponView.Update` 的轮询：
  - 新装备：在玩家身边 `Instantiate(PetRoot)`，挂外观，`InjectInto`，同步属性，满血。
  - 卸下或替换：`Dispose` 血条，销毁对象。
  - 前提是 `playerModel.Unit` 已存在，也就是 `PlayerFactory.Start` 之后，见 `CodeBase/Player/PlayerFactory.cs:11-19`。
- 第一只宠物来自起始武器：新存档由 `InitializeUnit` 自动装备 knife，见 `PlayerModel.cs:686-703`。系统会自动为它生成宠物，不需要特殊处理。

### 3.5 跟随、寻路、瞬移

新建 `PetMovementSystem`，只处理存活的宠物：

| 状态 | 行为 |
|---|---|
| 暂停：`!playerModel.CanMove`、玩家 IsDying 或 IsSleeping | 清空 CurrentTarget，回到跟随位，不攻击，和敌人此时回巡逻的行为一致 |
| 跟随：没有目标 | 目的地是玩家位置加按槽位序号排列的环形偏移（半径约 2.5m） |
| 追击：有目标 | 目的地是目标位置，进入射程后停步，朝向目标 |
| 拴绳 | 宠物离玩家超过 `PetConfig.LeashRadius`（默认 12m）时，放弃目标并回到跟随位 |
| 瞬移 | 离玩家超过 `PetConfig.TeleportDistance`（默认 18m），或 2s 内寻路失败时，用 `NavMesh.SamplePosition` 加 `Agent.Warp` 瞬移到玩家身边 |

- 移动速度：每帧设 `Agent.speed = playerModel.Unit.GetStatSpecificValue(MovementSpeed) × PetConfig.FollowSpeedMultiplier`（默认 1.2）。玩家不靠 `Agent.speed` 移动，而是 `agent.Move(speed×input×dt)`，`speed` 来自 MovementSpeed 经曲线 `pow(v,0.115)×4.6` 换算，并随吞噬上涨（`UnitMovement.cs:41-52`、`Stat.cs:65`）。`Pudge.prefab` 上的 `m_Speed 3.5` 不起作用，不能拿来当基准。
- 动画：`isWalking = Agent.velocity.sqrMagnitude > 0.01`。
- 拴绳和瞬移的距离必须小于 30m（敌人回满血的距离）和 60m（EnemyVisibility 冻结的距离），见 `CodeBase/Enemy/Systems/EnemyVisibilitySystem.cs:18`。

---

## 4. 战斗

### 4.1 宠物攻击：新建 `PetAttackSystem`

对每只存活宠物 `p` 及其物品 `it = p.Item`，复用 `PlayerAttackSystem` 的冷却状态机语义，见 `:119-190`：

1. 门控：`!CanMove`、玩家死亡中或睡眠、宠物死亡时都不攻击；`it.WearingTime < 1` 时不攻击。
2. 攻速：`attackSpeed = it 所有 AbilityState.GetWeaponStatMultiplier(AttackSpeed) 之积 × it.AbilityAttackSpeedMultiplier`，打弱敌或金像时再 ×2.5，保持与原作一致。判断弱敌和金像要用**宠物自己的目标**：把 `PlayerAttackSystem` 的 `IsWeakEnemy` 和 `IsEventGolem` 改成可共用（原 `IsAttackingWeakEnemy` 读的是玩家目标，见 `:88, 247-250`）。
3. 每帧先 `it.JustStartedAttack = false`，存活时 `it.WearingTime += dt`。原来这两件事由 `PlayerAttackSystem` 对 EquippedWeapons 的循环完成（`:119-141`），改成只处理玩家攻击后必须由宠物侧接手，否则 `WearingTime` 永远是 0，宠物**永不出手**。然后：有目标且在射程内时 `Cooldown -= attackSpeed × dt`；没有目标时按原逻辑回退。
4. `Cooldown <= 0` 时开始挥击：`AttackInProgress = JustStartedAttack = true`，`Cooldown = it.Config.AttackCooldown`，同时触发外观 Animator 的攻击（`isAttacking=true`，速度 `1/AttackCooldown`）。
5. Cooldown 越过 `castPoint × AttackCooldown` 时命中。`castPoint = it.Config.AnimationCastPoint`，**取固定值**，不读玩家摇杆的静态系数。然后发出：
   ```
   World.Default.GetEvent<WeaponHitEvent>().NextFrame(new WeaponHitEvent{
       Source = p.Unit, Target = p.Unit.CurrentTarget, Weapon = it, DamageType = it.Config.DamageType });
   ```
   并生成 hitVfx。震动只在玩家攻击时触发。
6. ResetCooldown 的两种消费方式与原作 `:142-147, 177-181` 相同，条件照搬：`!AttackInProgress && Cooldown <= AnimationBackPoint × AttackCooldown`。

射程用宠物自身属性 `AttackRange`（继承玩家的基础值 3.4），**从宠物位置测量**。宠物会自己走过去，所以取消链槽的 ×3 射程和 weapon_1 的特例。

宠物的目标选择，按优先级：

1. 玩家的 `CurrentTarget`（集火）。这样大多数读玩家目标的能力条件也能保持一致。
2. 正在以本宠物为仇恨目标的敌人（见第 7 节）。
3. 玩家周围 `PetConfig.EngageRadius`（默认 8m）内最近的、非 IsFar、未死亡的 `EnemiesModel.Enemies`。

以下几项沿用原作逻辑，不需要改：

- 伤害公式：`Source` 是宠物，所以属性从宠物身上读，见第 5 节；伤害类型和抗性来自 `it.Config.DamageType`。
- 物品级触发器：OnPreHit、OnHit、OnCrit 用 `Item = it`，每只宠物的能力天然隔离，见 `UnitDamageSystem.cs:290, 331, 372-379`。
- 伤害数字：仍按 `it.SlotIndex` 显示在 HUD 槽位上，见 `PlayerDamageEventsViewSystem.cs:44`。

### 4.2 玩家自有攻击

做法是新建一个**合成物品**，不进入 Items 和槽位：

- 新 asset：`Assets/MonoBehaviour/player_claw.asset`（ItemConfig）。字段：Key=`player_claw`，Rarity=Common，SlotType=Weapon，DamageType、DamageStatTypeFlat/Percent、FlatDamage、AttackCooldown、AnimationCastPoint/BackPoint 的具体值待定，`AbilityDefs: []`。**不要加入 Items Provider**。
- `PlayerConfig` 新增字段 `ItemConfig PlayerAttackItem`。`PlayerModel.InitializeUnit` 用它构造一个 `ItemModel`（构造方式与 `PlayerModel.cs:692` 构造起始武器相同），挂在新属性 `PlayerModel.PlayerAttackItem` 上。**不调 AddItem，所以不会存档、不会占槽**。

在 `PlayerAttackSystem` 里：

- 把 `:119-148`（冷却）和 `:159-190`（出手）里的 `foreach (EquippedWeapons)` 改成只处理 `PlayerAttackItem`，其他逻辑保留：选目标、0.2s 保持、weapon_1 基础射程规则、门控、`Source = PlayerUnit`。
- 玩家攻击用玩家的完整属性，**包括装备**。排除装备属性的规则只针对宠物。

需要处理的四个坑：

1. `ResetPerFrameState` 只重置 `EquippedItems`。合成物品的 `AbilityDamageMultiplier` 和 `AbilityAttackSpeedMultiplier` 构造时默认是 0，会让伤害和攻速都为 0。需要在 `AbilityExecuteEventSystem.cs:52-87` 里同样重置 `playerModel.PlayerAttackItem`。
2. 爪子不经装备流程，`SlotIndex` 保持默认 0（只在装备时赋值，`PlayerModel.cs:605, 790, 819`），不会越界。效果是爪子自动套用 weapon_1 的"只用基础射程"规则（`PlayerAttackSystem.cs:161-162`），伤害数字也和 weapon_1 宠物共用 `damageViews[0]`。第一步接受；要分开显示再另加视图。
3. 动画：原 weapon_1 的手部挥击由 `PlayerWeaponView` 和 `WeaponView` 驱动，这两者会被关掉。新建 `CodeBase/Player/PlayerClawView.cs` 挂在 Pudge 上，引用同一个 Animator（`Pudge.prefab:12644-12651` 中的 animator 95222568209840903）。在 `JustStartedAttack` 时 `SetTrigger("attackR")`，并设置 `handRSpeed`。参照 `WeaponView.cs:462-468`。
4. 链槽 ×3 射程：`GetAttackRangeSqr` 按槽位 key 判断是否装了链武器（`PlayerAttackSystem.cs:238-244`）。宠物占着 weapon_2~5 时玩家会锁定 3 倍距离外的弱敌，而爪子打不到，宠物又优先集火玩家目标，会被引走。开关打开时，玩家选目标不再使用链槽 ×3。

对能力的影响：

- 物品级触发器发给合成物品，因为它没有能力，所以什么都不发生。
- 全局触发器（OnPostHit、OnPostCrit、OnOneshot）照常让头盔、盾牌、宠物的能力生效。
- `PlayerModel.AbilityDamageFlat`、`AbilityDamageMultiplier` 和 NextHit* 等全局量对玩家攻击同样生效。
- "所有武器"类效果（`AllWeaponsDamageMultiplierEffect` 等）只遍历 `EquippedWeapons`，**不会加成玩家攻击**。

基础数值：玩家的 `StatType.Damage` 基础值为 0，吞噬也从不给它，见 `Player Config.asset:22-23`。所以玩家攻击 = 爪子的 `ItemModel.Damage` 加上对应类型的 flat。按原作的 CombatBalance 表，Common 1 份 Copies 的数值是 Cut 12、Pierce 8、Smash 25。具体数值见第 13 节。

附带好处：原作中卸掉全部武器后玩家会完全打不出伤害（`InventoryView.cs:445` 没有拦截）。现在零宠物时玩家仍能攻击，不会卡死。

### 4.3 怒气"多打一下"同时作用于宠物与玩家

修改 `RageExtraHitSystem.cs:42-58`：

- 从**存活**宠物的 `Item` 里取 `Damage` 最高的一个，置 `ResetCooldown = true`，并像原来一样触发 `OnCooldownReset`（`Item` = 它）。
- 同时给 `playerModel.PlayerAttackItem.ResetCooldown = true`。玩家攻击没有能力，所以不触发 OnCooldownReset。
- 宠物侧通过 `PetAttackSystem` 消费这个标志，见 4.1 的第 6 步；玩家侧由现有的 `PlayerAttackSystem` 逻辑消费。

**注意**：怒气只有装备 chainsaw（传说斩击）才存在，"多打一下"还需要护符升级 `graph_rage_extra_hit_7_21`。第 1、2 岛的正常流程里观察不到，验证需要作弊，见第 12 节。

---

## 5. 属性继承

### 5.1 继承规则

"装备"在第一步指玩家的头盔、盾牌、水晶。宠物物品自身能力产生的属性属于宠物，不算装备。原作中武器能力的属性写在玩家身上，等于对所有武器生效，所以继承它们与原作等价。

| 修饰来源 | 是否继承 | 理由 |
|---|---|---|
| Enemy(0)：吞噬 | 继承 | 杀怪获得 |
| Shop(4)、Ads(5)、Reward(6)、LevelPass(7)、CrystalEvent(12)、SecretChest(16)、Quest(18) | 继承 | 直接获得 |
| GraphUpgrade(10)：护符和灵魂图 | 继承（**待确认**） | 属于成长，不是可穿脱的装备 |
| Crystal(8) | **不继承**（**待确认**） | 可装备的水晶，属于装备 |
| Item(1)、AbilityPermanent(2)、AbilityTemporary(3) | 按名字归属判断 | 见下 |

按名字判断的方法：启动时遍历 `ItemsProvider` 的全部 ItemConfig，建立 `AbilityDef.Key → SlotType` 的字典。修饰名有两种格式：`AddStatEffect`、`GrantRandomStatPerDistanceEffect` 用 `Def.Key`；`AddStatPassiveEffect`、`StatStackEffect`、`BonusStatPerHitsTakenEffect` 用 `"{Def.Key}|{StatType}|{ModifierType}|{Source}"`（`AddStatPassiveEffect.cs:86-89` 的 `BuildKey`）。所以先取 `modifier.Name.Split('|')[0]` 再查表：

- 归属武器（宠物）物品：**继承**。例如 knife 的 OnKill +CutDamageFlat（Source 2）、doubleaxe_div 的 +20% CutDamagePercent（Source 1）、coolpike_div（Source 3）。
- 归属头盔或盾牌：**不继承**。例如 stone_disk 的 StatStack、盾牌被动的防御和回血。
- 查不到：不继承，并打 Warning 日志。已知的有 LavaCrab 的回血增益 `LavaCrabShockRegen`（Source 3，`LavaCrabExplosionSystem.cs:102`），直接写进排除表。水晶修饰名是 `crystal_*`（Source 8）。
- 例外：怒气额外生命。`RageExtraHealthSystem` 以 GraphUpgrade 来源往玩家 Health 加百分比修饰（`RageExtraHealthSystem.cs:60-70`）。怒气是玩家状态，这一条按名字排除，与"宠物不吃怒气减伤"保持一致。

两种命名格式都已核实：`AddStatEffect.cs:37, 43, 49`，`AddStatPassiveEffect.cs:86-89`，`StatStackEffect.cs:39`，`BonusStatPerHitsTakenEffect.cs:25`。

另外两项装备属性也不给宠物：

- 头盔和盾牌的 `ItemModel.Armor`：在 `DamageCalculator.cs:83` 对宠物跳过，见第 7 节。
- 玩家的怒气减伤：对宠物跳过。怒气是玩家状态，不属于继承属性。

已知无法剥离的部分：Enemy 来源的吞噬收益中已经乘过物品能力 DoubleLoot，见 `PlayerRewardSystem.cs:56-63`。按原样接受。

### 5.2 宠物属性的构造

新建 `CodeBase/Pet/PetStatInheritance.cs` 和 `PetStatsSyncSystem`。这个系统必须排在 `AbilityExecuteEventSystem` 之后，因为被动属性在那里同步到玩家身上，见 `SystemsInstaller.cs:71`、`AbilityExecuteEventSystem.cs:302-313`。

```
对每种 StatType t（取 PlayerConfig.Stats 中的 30 项）：
  Pet.Stat[t] = new Stat(t, PlayerConfig.Stats[t].BaseValue, new List<StatModifier>())   // 新列表！
  对 m ∈ playerModel.Unit.Stats[t].Modifiers 且 Inherit(m)：
      Pet.Stat[t].AddModifier( 逐字段复制 m )      // StatModifier 无 Clone
  若 t == Health：再加 PostFormulaMultiplicative 修饰
      { Name="pet.health_mult", Source=Enemy?/新建, Value = PetConfig.HealthMultiplier − 1 }
      // Stat.cs:81-90：PostMult = Π(1 + PostFormulaMultiplicative)
pet.Stats = 新列表; pet.BuildStatLookup();
```

- **触发同步的时机**：在 `StatBase` 上新增 `public int Version`，在 `AddModifier`（含原地合并分支）和 `RemoveModifier` 中自增，见 `StatBase.cs:63-105`。`PetStatsSyncSystem` **按 StatType 分别比较** Version，只重建变化的那一项。不能用总和：按生命缩放的被动（`ValueScaler.cs:18-23`）在战斗中每帧都会 Remove 再 Add（`AddStatPassiveEffect.cs:100-106`），总和每帧都变，会导致所有宠物每帧重建 30 项属性。修饰的删除都走 `RemoveModifier`（`UnitBuffSystem.cs:51`、`CrystalModel.cs:1248-1249`、`PlayerModel.cs:813-815`），唯一直接改列表的 `LoadStats`（`PlayerModel.cs:635`）发生在宠物生成之前。
- **当前生命**：重建后，如果最大生命上升，按增量加到当前生命上（与 `GrantStatReward` 的处理一致，见 `PlayerRewardSystem.cs:79`），然后取上限。
- 宠物属性**不存档**：`SaveStats` 只遍历玩家的 `Unit.Stats`。

### 5.3 公式

宠物打敌人时，`Source = 宠物`，`W = 宠物 ItemModel`。与原作公式完全一致，只是 `Src` 从玩家换成了宠物：

```
W.Damage = 进化前：Growth[type][rarity] × Copies + FlatDamage
           进化后：EvolutionValue(type,rarity,min(Level,15)) + CopyValue × Copies + FlatDamage
dmg = Pet.Stat(W.DamageStatTypePercent)
      × ( Pet.Damage + W.Damage + Pet.Stat(W.DamageStatTypeFlat)
          + PlayerModel.AbilityDamageFlat + W.AbilityDamageFlat )
      × (1 − enemy.Resist[W.DamageType])
      × 暴击(Pet.CritChance, Pet.CritDamagePercent, W.AbilityCrit*) × 超暴 × 能力倍率
      × W.AbilityDamageMultiplier × PlayerModel.AbilityDamageMultiplier × GlobalAttack(Pet) × 精通 × …
```

`Pet.Damage` 对应 StatType 3，基础值 0，吞噬不给，所以实际为 0。`Pet.Stat(W.DamageStatTypePercent)` 在 Player Config 里的基础值是 1。

宠物受击时，`Source = 敌人`：

```
d = (Pet.UniversalDefFlat + 0 [不含 EquippedArmors] + Pet.TypedDefFlat) × Pet.TypedDefPercent
incoming = a·exp(−0.75d/a)·max(0,1−0.25d/a) × (1 − Pet.UniversalDefencePercent)
           × 闪避(Pet.Evasion) × 格挡(Pet.BlockChance/BlockAmount)
           × [不应用怒气减伤] × 活动倍率 × 精通 × 1/(1 + Pet.GlobalDefence/100)
```

---

## 6. 能力迁移表

下文的"攻击者目标"指新增的帮助函数 `AbilityTargetResolver.Resolve(ctx)`，按顺序取：`ctx.Target` → `ctx.Attacker`（宠物）的 `CurrentTarget` → `ctx.Owner` 是武器物品且对应宠物存活时，取该宠物的 `CurrentTarget`（被动阶段没有 Attacker，靠这一步拿到所属宠物的目标） → 玩家的 `CurrentTarget`。为此需要给 `AbilityTriggerEvent` 和 `AbilityContext` 新增 `UnitCore Attacker`，并修改以下位置：

- `UnitDamageSystem` 的各个命中触发器生产者用 `data.Source` 填充 `Attacker`。
- `RentContext`（`AbilityExecuteEventSystem.cs:253-281`）复制该字段。
- `Release`（`AbilityContext.cs:44-60`）清空该字段。
- `TriggerAbilityEffect.cs:20-23` 和 `ApplyMarkEffect.cs:81-82` 手工构造上下文的地方也要设置该字段（`ApplyMarkEffect` 还要能从 Owner 找到对应宠物）。

| 条件或效果（文件） | 原作语义 | 宠物语义 | 改动 |
|---|---|---|---|
| `WeaponSetCondition`（Conditions/WeaponSetCondition.cs:13-18） | 已装备武器中同类型或异类型的数量 | 已装备宠物（**含死亡**，避免闪烁） | 无（EquippedWeapons 已经就是宠物） |
| `WeaponTypeCondition` | Owner 物品的伤害类型 | 宠物的伤害类型 | 无 |
| `WearingItemCondition` | 是否装备某物品 | 是否拥有该宠物或护甲 | 无 |
| `EmptySlotsCondition`、`ValueScaler.PerEmptySlot`、`DamageForEmptySlotsEffect`、`AttackSpeedForEmptySlotsEffect` | `EmptyItemSlots` 包含头盔和盾牌槽（PlayerModel.cs:130-139） | 按用户决定改成判断宠物：新增 `PlayerModel.EmptyPetSlots` = 已解锁、`SlotType==Weapon`、未装备的槽位；死亡宠物不算空槽 | 改 |
| `TargetCondition.TargetChanged`（:16-18）及 `AbilityExecuteEventSystem.cs:170` 的 `state.CurrentTarget` | 与玩家目标比较 | 与攻击者目标比较 | 改为使用攻击者目标 |
| `TargetShockedCondition`、`TargetStunnedCondition`、`TargetBossArenaCondition`、`HealthThresholdCondition` 的回退分支 | 读玩家目标 | 读攻击者目标 | 改 |
| `CombatCondition`（:16-18） | 玩家的 `max(LastDamageGet, LastDamageDealt)` | 再与所有存活宠物的 `LastDamageDealt` 取最大 | 改 |
| `HitsCountCondition`、`ValueScaler.PerHitsTaken` | 所有武器的出手都计数（`DamageCalculator.cs:167-168`） | 所有宠物加玩家的出手都计数（出手方更多，触发更快，见第 13 节） | 无 |
| `AllWeaponsDamageMultiplierEffect`、`AttackSpeedMultiplierEffect`（PerEquippedWeapon） | 所有武器 | 所有已装备宠物（不含玩家攻击） | 无 |
| `DamageForMissingHealthEffect` | 按玩家缺失生命比例加成对应类型武器 | 仍按**玩家**缺失生命比例加成对应类型宠物（它判断的是玩家状态，不是武器）。如果想改成按宠物自身缺失生命，见 13.1 | 无 |
| `DamageForStatEffect` | 指定槽或全局 | 指定槽目前只出现在 `_div` 遗留品上；全局部分对所有出手生效 | 无 |
| `TemporaryWeaponBuffEffect`、`WeaponStatStackEffect`、`EmpowerHit*`、`GuaranteedCritEffect`、`AddWeaponSuperCrit*` | 作用在 Owner 物品上 | 作用在所属宠物上（`PetAttackSystem` 读同样的字段） | 无。`WeaponStatStack` 的目标切换重置改用攻击者目标；`AddWeaponSuperCrit*` 在 Passive 上下文中按 PerMarksOnTarget 取所属宠物的目标 |
| `ResetCooldownEffect`（Self / HighestDamageWeapon）、`TriggerHighestDamageWeaponEffect` | 按 DPS 取最高的武器 | 只在**存活宠物**中取最高；Self 的情况下若宠物已死则跳过 | 改：加存活过滤 |
| `RageExtraHitSystem` | 最高 Damage 的武器 | 最高 Damage 的存活宠物，加上玩家攻击 | 改，见 4.3 |
| `StunEffect`（:15-20）、`ConsumeShockEffect`（:21-34）、`SpawnLightningEffect` 的回退目标、`StatStackEffect` 的重置、`ValueScaler.ResolveTarget`（:44-49） | 玩家目标 | 攻击者目标 | 改 |
| `PiercingWeaponMarkSystem`（:33） | `Source is PlayerUnit` | `Source is PlayerUnit` 或 `PetUnit`，仍需装备 chainsawpike（原作逻辑） | 改 |
| `UnitDamageSystem.HasEquippedPierceWeapon`（:382-387） | 有穿刺武器 | 有穿刺宠物 | 无 |
| `AddRageEffect`、`GrantRageAccessEffect` | 命中加怒气；装备 chainsaw 才开放怒气 | 宠物命中加怒气；chainsaw 宠物死亡后**仍保留**怒气权限 | 无 |
| `HealFromDamageEffect`、`TriggerLifestealEffect`、`ThornsEffect`、`LethalIgnorePassiveEffect`、`HealPercentEffect` 等玩家侧效果 | 作用于玩家 | 仍作用于玩家 | 无 |
| `AddStatEffect`、`AddStatPassiveEffect`、`StatStackEffect`、`BonusStatPerHitsTakenEffect` | 写在玩家身上 | 仍写在玩家身上，宠物按第 5 节的规则继承 | 无 |
| 被动能力（Passive）在宠物死亡期间 | — | 继续生效（能力跟随物品，不跟随生死） | 无 |
| `GrantStatOnHitEffect`、`ScaledMultiplierEffect`、`TreatAsAllSlotsEmptyEffect` | 【空桩】，没有 asset 使用 | — | 不动 |

---

## 7. 仇恨与受伤

### 7.1 敌人选目标

修改 `CodeBase/Enemy/EnemyUnit.cs`（新增 `public UnitCore AggroTarget`）和 `CodeBase/Enemy/EnemyAttackSystem.cs`（【重建】，可以自由修改）。

每帧对每个非静态敌人：

1. 候选：玩家（若可用），以及所有存活宠物。
2. `AggroTarget` = 距离敌人最近的候选。
3. 刷新仇恨：任意候选在 AggroRange 内时，`AggroTimer = AggroDuration`。原来 `:107` 的 `distanceSquared` 只对玩家计算，要改成对所有候选取最小值，否则宠物在玩家 AggroRange 外开打时，敌人会回巡逻。
4. 保持仇恨：把原条件 `player.CurrentTarget == enemy` 扩展为"玩家或**任一宠物**的 CurrentTarget 是该敌人"。这样宠物攻击玩家附近之外的敌人时，敌人不会走开。
5. 把 `:149、162、192、224、286、310、318` 处的 `PlayerUnit` 换成 `AggroTarget`，`Attack()` 里 `:346` 处改为 `Target = enemy.AggroTarget`。

以下行为**保持以玩家为准**，第 3.5 节的拴绳和瞬移保证宠物离玩家足够近，所以不受影响：

- 30m 回满血（`:108-109, 274-275`）
- 玩家不可用时全体回巡逻（`:110, 276`）。这时宠物也暂停，行为一致。
- 会逃跑的敌人只看玩家（`:171-256`）

### 7.2 伤害结算

在 `UnitDamageSystem.ApplyDamage`（`:103-108`）里，**在玩家兜底分支之前**新增一个分支：

```
else if (data.Target is PetUnit pet) ApplyDamageToPet(data, pet);
```

`ApplyDamageToPet` 的内容：

- 开头加守卫：宠物已死就直接返回。敌人的命中用 `NextFrame` 发出，宠物死后仍会收到在途命中；玩家分支也有同样的 `IsDying` 守卫（`UnitDamageSystem.cs:113`）。
- 用 `DamageCalculator.Calculate` 的受击分支算伤害，修改见下。
- 扣血，更新 `pet.LastDamageGet`。
- `ApplyLifesteal(data.Source)`：敌人吸血，与打玩家时一致。
- 用 `UnitDamageViewSystem.Create(...)` 显示世界坐标伤害数字，见 `CodeBase/UI/World/UnitDamageViewSystem.cs:23`。不要使用玩家 HUD 列表 `EnemyDamageEventsViewSystem`。
- 生命 ≤ 0 时调用 `PetsModel.OnPetDied(pet)`，见第 8 节。
- **不执行**：格挡和闪避的触发器、荆棘、`HitsTakenCount`、免死、`OnGetHitLowHealth`、`ProcessPlayerDeath`、`UnitDiedEvent` 和 `OnDied`。最后两项会被闪电、Hydra、Vampire 等当作敌人死亡处理，所以不能发。

修改 `DamageCalculator` 的受击分支：

- `:83`：目标是 `PetUnit` 时，`armor = 0`。
- `:105-110`：目标是 `PetUnit` 时，跳过怒气减伤。
- 闪避和格挡的判定取目标自身属性，宠物沿用。

### 7.3 范围攻击、爆炸、投射物

- 普通敌人没有投射物，不需要处理。
- `LavaCrabExplosionSystem.Explode`（`:105-128`，Boss 塔）：在原来判断玩家距离的地方，再遍历存活宠物，对半径内的宠物同样发出 `WeaponHitEvent`。可以选做，但建议做，保持"都会被打"的一致性。
- Creeper、FireElemental、Assassin 是【空桩】，不改。
- `MageBossSystem` 只从玩家位置后撤，不改。

### 7.4 宠物血条

- 修改 `HealthBar.RenderDrops`（`HealthBar.cs:240`）：从"`unit is PlayerUnit` 才 return"改为"`!(unit is EnemyUnit)` 就 return"，避免强转异常。
- 新建 `PetHealthBarSystem`（筛选 `PetTag` 且没有 `UnitDeadMarker`）：调用 `SetupHealthBar(playerConfig.HealthBarPrefab)`，死亡或销毁时调用 `Dispose`，参照 `EnemyHealthBarSystem.cs:35-48`。

### 7.5 宠物回血

新建 `PetRegenSystem`（筛选 `PetTag`，存活）：每帧 `CurrentHealth += HealthRegen 的 specific 值 × dt`，上限为最大生命，参照 `PlayerRegenSystem.cs:20-38`。不乘玩家的 `AbilityHealthRegenMultiplier`。

---

## 8. 死亡与复活

### 8.1 死亡

在 `PetsModel.OnPetDied` 中：

- `State = Dead`，`ReviveAt = Time.time + PetConfig.ReviveCooldown`（默认 15s，**待确认**）。
- 添加 `UnitDeadMarker`，停用 Agent 并移除 `NavMeshAgentActiveMarker`，清空 `CurrentTarget`。
- Animator：`isDead = true`。约 2s 后隐藏 VisualSlot 和血条。宠物尸体不留在场上。
- 物品**保持装备**，冷却状态保留（复活后重置）。

### 8.2 冷却自动复活

新建 `PetReviveSystem`：当 `Time.time >= ReviveAt` 时，在玩家身边找跟随位，用 `SamplePosition` 加 `Warp` 放置宠物，然后：

- 生命设为最大值的 20%（`PetConfig.ReviveHealthFraction`，§14 更新）。
- 移除 `UnitDeadMarker`，启用 Agent，并重新 `Set` `NavMeshAgentActiveMarker`。`NavMeshSyncSystem` 靠它更新位置（`NavMeshSyncSystem.cs:19`），漏掉的话宠物复活后位置不再同步。
- `isDead = false`，`SetTrigger("Spawn")`。
- 显示外观，重建血条。
- 把 `Item.WearingTime` 置 0，复用"1s 后才出手"的门控。

### 8.3 回到泉水复活

代码里已经有泉水：`FirecampSystem`，即场景中唯一的 `HealField`，半径 3.05m，位于起始出生点附近。在 `FirecampSystem.Update`（`:45-53`）里扩展：

- **复活**：玩家在 Radius 内时，立即复活全部死亡宠物，放在玩家身边。
- **治疗**：存活宠物在 Radius 内且 4s 没受伤时，按同样的 12%/s 回血。

### 8.4 推荐规则（待用户确认）

- 冷却和泉水两者都生效，先到先复活。泉水只有一个，在岛 1，所以远处的岛主要靠冷却复活。
- **玩家死亡复活时，全部宠物一起复活。** 血量规则跟玩家一样：普通复活时死亡宠物为 20% 血、存活宠物保持原血量；`restoreHealth` 时全部满血（§14 更新）。 `RevivePlayerCommand` 会让所有敌人回满血，宠物不跟着复活会让下一场战斗明显变难。

### 8.5 竞技场内

竞技场没有泉水，只能靠冷却复活。宠物死亡**不结束**战斗；结束条件仍然只有玩家死亡或超时，见 `BossRaidSystem.cs:282-297`。

---

## 9. 特殊流程

| 流程 | 现状 | 宠物处理 | 修改文件 |
|---|---|---|---|
| `CanMove=false` 的 11 处流程：开门、传送门、任务对话和过场、宝箱、水晶、宝箱队列 | 敌人回巡逻，玩家停止攻击 | 宠物暂停攻击并回跟随位（`PetMovementSystem` 和 `PetAttackSystem` 统一检查 `CanMove`）。第一步过场中**不隐藏**宠物 | 不改流程文件 |
| 玩家死亡动画和沉入泉水（约 3.2s 补间，外加动画等待） | `CanMove=false`，玩家被隐藏 | 宠物因 `IsDying` 暂停 | — |
| `RevivePlayerCommand`（`RevivePlayerCommandHandler.cs:26-43`） | 传送到泉水，生命设为 1，敌人回满血 | `PetsModel.ReviveAll(revivePosition, yaw, restoreHealth)`：全部瞬移、复活、清目标；普通复活 20% 血，`restoreHealth` 时满血 | 改此 handler |
| 离开 NavMesh 的兜底（`PlayerNavMeshFallbackSystem.cs:25-36`） | 玩家瞬移到泉水 | `PetsModel.WarpAll`（瞬移规则本身也会兜住） | 改 |
| 活动门关闭（`EventBossGateSystem.cs:99-121, 168-177`【重建，触发方向存疑】） | 50m 内的玩家瞬移到泉水 | `PetsModel.WarpAll`，防止宠物被困在门后 | 改 |
| 进入竞技场（`EnterBossArenaCommandHandler.cs:27-77`） | 重置物品冷却，玩家满血，瞬移到 `PlayerSpawnPoint` | 全部宠物复活、满血、按偏移瞬移、清目标。物品冷却已由 `ResetItemCooldowns` 处理（`:67-77`），合成爪子需要单独重置 | 改 |
| 离开竞技场（`BossRaidSystem.TeleportPlayerToTowerEntrance` `:299-309`） | 瞬移到 `Portal.playerSpawnPosition` | 宠物一起瞬移。如果玩家已死，走死亡流程，由 `RevivePlayerCommand` 覆盖 | 改 `BossRaidSystem.cs` |
| 竞技场重试（`StartNextFloorFlowRunner.cs:81-91`，`ReviveFirst`） | 调用 `RevivePlayerCommand(restoreHealth=true)` | 已由 Revive 的钩子覆盖 | — |
| 竞技场与主世界的 NavMesh 是否连通 | 未确认 | 一律使用 `Warp` | — |
| Boss 战开始和法师传送（`BossRaidSystem.cs:148-163`，`MageBossSystem.cs:52-65`） | 任何命中都算 | 宠物命中也会开战，法师传送更频繁 | 接受 |
| 宝箱镜头（`OpenSecretChestCommandHandler.cs:58-149`） | 锁 `CanMove`，不移动玩家 | 宠物暂停即可 | — |
| 换岛 | 同一块 NavMesh，`GateLocationTrigger` 只认 PlayerUnit | 宠物正常寻路；`PetUnit` 不是 PlayerUnit，不会误改所在区域 | — |
| 吞噬表现 | 触手从玩家身上伸出，不限距离 | 宠物在远处击杀时触手会拉得很长，第一步接受 | — |
| 命中音效 | `ApplyDamageToEnemy` 对所有来源都播 `PlayPlayerWeaponImpactSound`（`UnitDamageSystem.cs:297`） | 多只宠物同时命中会叠音，第一步只对 `Source is PlayerUnit` 播放，宠物命中暂时静音 | 改 `UnitDamageSystem.cs` |
| 等待中的敌人 | 命中会移除 `UnitWaitingAttackMarker`（`UnitDamageSystem.cs:299-300`） | 宠物先手同样会"唤醒"敌人，与玩家先手一致 | — |
| `Unstack` 和超时死亡 | 走玩家死亡流程 | 由 Revive 钩子覆盖 | — |
| 武器外观 | `Pudge.prefab` 上 5 个 `PlayerWeaponView` 会继续按冷却甩出武器 | 在 `PlayerWeaponView` 的 `Update` 和 `BuildView` 开头加 `if (PetConfig.Enabled) { 隐藏; return; }` | 改 `PlayerWeaponView.cs` |

---

## 10. 存档与兼容旧存档

- **第一步不改存档结构**：
  - `ItemDataUd` 和 `ItemSlotDataUd` 不变。
  - 宠物的生命、死亡、复活倒计时只在运行时存在。读档时全部宠物以存活状态生成在 `LastPosition` 附近。
  - 玩家生命本来也不存档（`PlayerModel.cs:693`）。
- **旧存档自动兼容**：已装备的武器会被 `PetSpawnSystem` 识别并生成宠物；未装备的物品仍然在背包里。
- 合成爪子 `player_claw` **绝不能**进入 `Items`。一旦被存档，读档时 `ItemsProvider.Get` 会因为找不到 Key 而抛异常（`ItemsProvider.cs:13-16`）。
- 宠物属性不进 `SaveStats`：它们挂在 `PetUnit` 上，`SaveStats` 只遍历玩家的 `Unit.Stats`。
- **不能重命名任何 ItemConfig.Key**。
- 后续如果要存宠物冷却，参照 `UnstackCooldownTimestamp`（`PlayerUd.cs:51, 78`），在 `ItemDataUd` 上新增 `Child("pet_respawn_at")`，类型为 `UdValue<long>`，并在 `ItemModel.BindToUserData` 中读写。Ud 库是闭源 DLL，"旧存档缺 Key 时读成默认值"这一点需要实测。
- 存档带 MD5 校验，不能手改存档来测试。

---

## 11. 文件级改动清单（按实施顺序）

### 阶段 A：基础设施

1. **新建** `CodeBase/Pet/PetConfig.cs` 和 `Assets/MonoBehaviour/Pet Config.asset`。内容：`Enabled`、`ReviveCooldown`、`LeashRadius`、`TeleportDistance`、`EngageRadius`、`FollowSpeedMultiplier`、`HealthMultiplier`、`PetRootPrefab`、`Visuals`（ItemKey → EnemyUnit）、`FallbackByDamageType`。
2. **新建** `CodeBase/Unit/Components/PetTag.cs`（参照 `PlayerTag.cs`）、`CodeBase/Pet/PetUnit.cs`、`Assets/Prefabs/Pet/PetRoot.prefab`。
3. **改** `CodeBase/Unit/StatBase.cs`：新增 `Version`。
4. **新建** `CodeBase/Pet/PetModel.cs`、`CodeBase/Pet/PetsModel.cs`。**改** `CodeBase/Core/Boot.cs`：在 PlayerModel 之后 `RegisterModel(PetsModel)`。
5. **新建** `CodeBase/Pet/PetStatInheritance.cs`、`CodeBase/Pet/Systems/PetStatsSyncSystem.cs`。

### 阶段 B：生成与移动

6. **新建** `CodeBase/Pet/Systems/PetSpawnSystem.cs`、`PetMovementSystem.cs`。
7. **改** `CodeBase/Core/SystemsInstaller.cs`：在 `AbilityExecuteEventSystem`（`:71`）之后依次注册 PetSpawn、PetStatsSync、PetMovement、PetAttack、PetRevive、PetRegen、PetHealthBar。
8. **改** `CodeBase/Player/PlayerWeaponView.cs`：加 `Enabled` 开关，关闭原武器外观。

### 阶段 C：攻击

9. **新建** `Assets/MonoBehaviour/player_claw.asset`。**改** `CodeBase/Player/PlayerConfig.cs` 和 `Assets/MonoBehaviour/Player Config.asset`：新增 `PlayerAttackItem`。**改** `CodeBase/Player/Models/PlayerModel.cs`：新增 `PlayerAttackItem` 属性，在 `InitializeUnit` 中构造。
10. **改** `CodeBase/Player/PlayerAttackSystem.cs`：在开关打开时，把对 EquippedWeapons 的循环换成只处理 PlayerAttackItem。
11. **新建** `CodeBase/Player/PlayerClawView.cs`，挂到 `Assets/GameObject/Pudge.prefab` 上。
12. **改** `CodeBase/Abilities/Systems/AbilityExecuteEventSystem.cs`：重置 PlayerAttackItem 的每帧字段；在 `:170` 使用攻击者目标；`RentContext` 复制 `Attacker`。
13. **改** `CodeBase/Player/PlayerAttackSystem.cs`：`IsWeakEnemy`、`IsEventGolem` 改为可共用；开关打开时 `GetAttackRangeSqr` 不再用链槽 ×3。
14. **新建** `CodeBase/Pet/Systems/PetAttackSystem.cs`。
15. **改** `CodeBase/Player/Systems/RageExtraHitSystem.cs`、`Abilities/Effects/ResetCooldownEffect.cs`、`Abilities/Effects/TriggerHighestDamageWeaponEffect.cs`：加存活过滤和玩家攻击。

### 阶段 D：能力目标

16. **改** `Abilities/Events/AbilityTriggerEvent.cs`、`Abilities/Core/AbilityContext.cs`。**新建** `Abilities/Core/AbilityTargetResolver.cs`。
17. **改** 以下文件，改用攻击者目标：`Abilities/Effects/TriggerAbilityEffect.cs`、`Abilities/Effects/ApplyMarkEffect.cs`、`Abilities/Effects/StunEffect.cs`、`Abilities/Effects/ConsumeShockEffect.cs`、`Abilities/Effects/SpawnLightningEffect.cs`、`Abilities/Effects/StatStackEffect.cs`、`Abilities/Effects/WeaponStatStackEffect.cs`、`Abilities/Core/ValueScaler.cs`、`Abilities/Conditions/TargetCondition.cs`、`TargetShockedCondition.cs`、`TargetStunnedCondition.cs`、`TargetBossArenaCondition.cs`、`HealthThresholdCondition.cs`。
18. **改** `Abilities/Conditions/CombatCondition.cs`、`Abilities/Systems/PiercingWeaponMarkSystem.cs`。
18b. **改** `CodeBase/Player/Models/PlayerModel.cs`：新增 `EmptyPetSlots`；**改** `Abilities/Conditions/EmptySlotsCondition.cs`、`Abilities/Core/ValueScaler.cs`（PerEmptySlot）、`Abilities/Effects/DamageForEmptySlotsEffect.cs`、`Abilities/Effects/AttackSpeedForEmptySlotsEffect.cs` 改用它。

### 阶段 E：受伤与仇恨

19. **改** `CodeBase/Unit/Systems/UnitDamageSystem.cs`：新增宠物分支 `ApplyDamageToPet`（含已死守卫）；各命中触发器生产者填充 `Attacker`；命中音效只对玩家播放。
20. **改** `CodeBase/Services/Damage/DamageCalculator.cs`：受击分支对宠物跳过护甲和怒气减伤。
21. **改** `CodeBase/UI/World/HealthBar.cs`（`:240`）。**新建** `PetHealthBarSystem.cs`、`PetRegenSystem.cs`。
22. **改** `CodeBase/Enemy/EnemyUnit.cs`（`AggroTarget`）、`CodeBase/Enemy/EnemyAttackSystem.cs`。
23. **改** `CodeBase/BossTower/Systems/LavaCrabExplosionSystem.cs`（选做）。

### 阶段 F：死亡复活与流程

24. **新建** `CodeBase/Pet/Systems/PetReviveSystem.cs`。**改** `CodeBase/Buildings/FirecampSystem.cs`：泉水复活和治疗宠物。
25. **改** `Player/Commands/RevivePlayerCommandHandler.cs`、`Player/Systems/PlayerNavMeshFallbackSystem.cs`、`Events/Systems/EventBossGateSystem.cs`、`BossTower/Commands/EnterBossArenaCommandHandler.cs`、`BossTower/Systems/BossRaidSystem.cs`（`TeleportPlayerToTowerEntrance`）。
26. （可选）**改** `CodeBase/UI/ItemSlotView.cs`：宠物死亡时在 `cooldownFill` / `cooldownText` 上显示复活倒计时。

不改的文件：`EnemySpawner.cs`、`PlayerDevour.cs`、`ItemModel.cs`、`ItemConfig.cs`、进化相关、`ItemsProvider`、存档结构、`SimulationSystem.cs`。

---

## 12. 验证计划（Unity Editor，第 1、2 岛）

建议 `PetConfig.Enabled` 可以随时切换，便于和原作对照。

| # | 操作 | 期望观察 |
|---|---|---|
| 1 | 新存档进入游戏 | 玩家身边出现 1 只宠物（knife 对应的外观），有血条；Hierarchy 里有 PetRoot；`Pudge` 身上不再出现飞出的武器；Console 没有 InvalidCast 或 NRE |
| 2 | 走动 | 宠物跟随并播放 `isWalking`，速度跟得上玩家；快速跑远（大于 18m）时宠物瞬移回来 |
| 3 | 靠近敌人 | 宠物走到敌人旁出手（`isAttacking`），敌人掉血，HUD weapon_1 槽位出现伤害数字；玩家自己也播放 `attackR` 并造成伤害 |
| 4 | 打断点或 Inspector 查看宠物属性 | `CutDamageFlat` 等于玩家身上 Enemy 来源和 Reward 来源修饰之和；吞噬后宠物属性同步上涨；不包含头盔、盾牌、水晶的修饰 |
| 5 | 宠物击杀敌人 | 吞噬触手从玩家伸出，玩家属性上涨，掉落和 OnKill 正常 |
| 6 | 站着不动让敌人围攻 | 敌人会分别攻击玩家和宠物；宠物受击时出现世界坐标伤害数字，玩家的格挡、荆棘、受击计数**不**变化 |
| 7 | 让宠物被打死 | 播放 `isDead` 后消失；物品仍装备在槽位上；15s 后在玩家身边复活，生命为最大值的 20% |
| 8 | 宠物死亡后走到 HealField | 立即复活；存活宠物在泉水里回血 |
| 9 | 让玩家死亡 | 玩家死亡动画期间宠物停止攻击；复活后全部宠物瞬移到泉水旁，死亡的宠物生命为 20%，存活的保持原血量 |
| 10 | 击败岛 1 Boss（拿到 weapon_2 和 pike） | 第 2 只宠物（穿刺外观）出现；两只宠物都能攻击 |
| 11 | 在背包里卸下全部宠物 | 宠物消失，玩家仍能攻击 |
| 12 | 开门、任务对话、宝箱 | 镜头离开期间宠物不攻击，结束后恢复 |
| 13 | 进入和离开 Boss 塔竞技场 | 宠物随玩家传入传出并满血；宠物死亡不会结束战斗 |
| 14 | 用作弊让物品进化（knife 进化到 Lv1） | 能力在宠物攻击时触发，例如 knife 的 OnKill 加 CutDamageFlat |
| 15 | 用作弊装备 chainsaw（开放怒气）**并且**给护符升级 `graph_rage_extra_hit_7_21`（设定阈值），两者缺一不可 | 到达怒气阈值时，伤害最高的存活宠物和玩家各自提前出手一次 |
| 16 | 存档、重开 | 物品和槽位不变，宠物全部存活，没有读档异常 |
| 17 | 宠物模式下新存档，查看玩家 Damage 属性 | 玩家 Damage = 10（`PlayerBaseAttack`），爪子按它出伤；存档、重开多次后仍是 10，不叠加；关闭 `PetConfig.Enabled` 重启后为 0 |
| 18 | 打开背包里任一武器的详情 | 出现宠物属性块：名字 + 生命、攻击、防御、移速、攻速 5 行；攻击 = 自身 + 玩家 × 比例，数值与第 15.3 节公式一致；吞噬后绝对值在 0.5s 内刷新；关闭宠物模式后详情与原作一致 |
| 19 | 用作弊给武器加副本、进化 | 详情里的下一级预览（箭头后绿色数值）与升级后的实际值一致；场上对应宠物的最大生命和攻击随之重建，不需要等玩家属性变化 |
| 20 | 宠物追打敌人、然后脱战 | 战斗中移速 = 玩家移速 × 移速比例；脱战后回到玩家移速 × 1.2 跟随，跑远仍会瞬移 |
| 21 | 断点查看宠物打出的一击 | `weaponDamage` = 武器原伤害 × 0.6 × `OwnAttackMultiplier`；宠物 Damage 与类型 Flat 属性带 `pet.ratio` 修饰；最终伤害不再乘旧的 `DamageMultiplier` |

第 14、15 项需要 `CheatsPanel`（`CodeBase/Services/Cheats/CheatsPanel.cs`）里的现有功能。如果没有对应功能，需要临时加。

---

## 13. 风险与待确认问题

### 13.1 需要用户决定

1. **复活规则**：冷却和泉水"两者都生效、先到先复活"，可以吗？冷却时长取多少，默认 15s？玩家死亡复活时是否让全部宠物一起复活？推荐是。
2. **属性继承的边界**（5.1）：
   - 水晶（Source 8）是否算装备？推荐不继承。
   - 护符和灵魂图（GraphUpgrade 10）是否继承？推荐继承。
   - 头盔和盾牌能力给的属性，包括杀怪永久加成（Source 2），是否排除？推荐排除。
   - 宠物自身武器能力给的属性推荐继承，等价于原作。
3. **宠物生命值**：武器没有生命数据。推荐 = 玩家继承后的 Health × `HealthMultiplier`，默认取多少？注意即使取 1.0，宠物也比玩家脆：它不吃头盔和盾牌的护甲（`DamageCalculator.cs:83`），也没有怒气减伤（`:105-110`）；闪避、格挡、全局防御则取宠物自己继承到的属性。
4. **宠物吸血回谁**：Source 是宠物时代码自然回宠物，推荐保持。头盔的 `HealFromDamage` 仍然回玩家。
5. **玩家自有攻击的数值**：伤害类型只能是穿刺、斩击、钝击之一，推荐斩击；FlatDamage 和攻速 AttackCooldown 各取多少？
6. **宠物外观映射**：15 个基础武器各用哪个敌人 prefab（避开龙类和 Boss prefab）？也可以先按伤害类型只选 3 个。
7. **怒气"多打一下"**：宠物和玩家各提前一击，总收益约等于原作的两倍，是否符合预期？
8. **`DamageForMissingHealthEffect`**：按玩家缺失生命（推荐，保持原作）还是按该宠物自身缺失生命？

### 13.2 技术风险（已有对策，或第一步接受）

- 以下代码是**重建体，没有原生标注**，修改时注意它们本身可能和原版有偏差：`EnemyAttackSystem`、`RageExtraHitSystem`、`EnemySpawner.HandleDeath/HandleDevour`、`ItemModel.IsAbilityUnlocked/RebuildAbilityStates`、`FirecampSystem`、`EventBossGateSystem`（触发方向存疑）、任务流程（标注为 candidate only）。
- 出手方变多以后：
  - `HitsCounter` 类能力（sunwheel_crown 的眩晕）触发更快。
  - `NextHit*` 和 `BlockSuperCritChanceBonus` 这些全局一次性加成会被最先出手的宠物或玩家消耗，见 `DamageCalculator.cs:167-201`。
  - 第一步接受，体验后再调。
- `SimulationSystem` 仍按"玩家拿着全部武器"计算 DPS，见 `SimulationSystem.cs:825-849, 982-985`。商店和广告的 RPM 奖励会出现漂移，第一步接受。
- 敌人反伤只作用于玩家，宠物免疫，见 `UnitDamageSystem.cs:255`。保持原样。
- 第 1、2 岛的武器在进化前几乎没有能力，能力迁移需要用作弊验证。
- `InBossTowerArena` 会被存档，而且启动时不会重置，见 `PlayerUd.cs:70`。这是原作就有的问题：中途杀进程后 `PlayerReviveSystem` 会一直跳过。在竞技场测试宠物死亡时要注意。

---

## 14. 已确认的决定（2026-10-27，覆盖前文的"待确认"与"推荐"）

1. **宠物基础属性用倍率表示。**（**已被第 15 节取代**：下面的四个倍率字段已删除，改为比例公式。） 每种宠物在 `PetConfig` 里配置相对主角的倍率，例如"胖子"宠物：生命 1.2、攻击 0.8。
   - 字段：`HealthMultiplier`、`DamageMultiplier`、`DefenceMultiplier`、`AttackSpeedMultiplier`，默认都是 1。
   - 生命：宠物继承后的 Health 再乘 `HealthMultiplier`（PostFormulaMultiplicative 修饰）。
   - 攻击：宠物打出的最终伤害乘 `DamageMultiplier`（在 `DamageCalculator` 里，Source 是宠物时生效）。
   - 防御：宠物受击时，防御值 `d` 乘 `DefenceMultiplier`。
   - 攻速：宠物出手的 `attackSpeed` 再乘 `AttackSpeedMultiplier`。
   - 取代第 13.1 节第 3 条的统一 `HealthMultiplier`。
2. **"按缺失生命加伤害"（`DamageForMissingHealthEffect`）分成两种接收方**，新增字段，默认值（枚举 0）为"给自己"：
   - 给自己：加成到所属宠物自己的攻击；
   - 给玩家：加成到玩家的爪子攻击。
   - 参考血量都是**所属宠物自己的**缺失生命比例。宠物死亡期间不提供加成。
   - 所属物品不是宠物（例如头盔、盾牌）时，保持原作行为：按玩家缺失生命加成对应类型的宠物。
3. 其余按推荐：
   - 冷却复活与泉水复活同时生效，冷却 15s；玩家死亡复活时全部宠物一起复活。
   - **复活血量（更新）**：宠物复活时生命为最大值的 20%（`Pet Config.asset` 的 `ReviveHealthFraction`，可调），不是满血。玩家满血复活的场合（进入竞技场、Boss 塔下一层 `restoreHealth`）宠物也满血。
   - 水晶（Source 8）不继承；灵魂之树和护符（GraphUpgrade 10）继承，但怒气额外生命除外。
   - 头盔和盾牌能力给的属性不继承；宠物自身武器能力的属性继承。
   - 宠物吸血回宠物自己。
   - 玩家爪子：斩击，数值取普通品质斩击武器（knife）的基础值，不带能力。
   - 外观先按伤害类型选 3 只现有敌人，每只配一组倍率；15 把基础武器按伤害类型套用。
   - 怒气"多打一下"同时作用于伤害最高的存活宠物和玩家。
4. 全部改动由 `PetConfig.Enabled` 控制，关闭时行为与原作一致，便于对照。

---

## 15. 宠物属性比例（方案 A）

2026-10-28 用户确认。宠物属性改为"玩家属性的比例"，同时缩小宠物的直接增伤（武器自带伤害）。本节覆盖：

- 第 14 节第 1 条：`HealthMultiplier`、`DamageMultiplier`、`DefenceMultiplier`、`AttackSpeedMultiplier` 四个字段全部删除，最终伤害不再乘 `DamageMultiplier`，攻速不再乘种族系数。
- 第 5.2 节的 `pet.health_mult` 修饰：改为 `pet.ratio`，见 15.3。
- 第 5.3 节"`Pet.Damage` 实际为 0"：宠物模式下玩家 Damage 基础值为 10，宠物继承它，见 15.4。`W.Damage` 在宠物出手时换成自身攻击，见 15.3。
- 第 13.1 节第 3 条的宠物生命值。

"玩家属性"仍指宠物已经在用的继承集合（杀怪获得 + 直接获得，不含装备，规则见 5.1）。例外是移速，见 15.3。

### 15.1 比例公式

```
ratio(stat, item) = 种族比例[stat]
                  × RarityRatioFactor[item.Rarity]
                  × (1 + LevelRatioGrowth × (max(1, 宠物等级) − 1))

宠物等级 = 进化前：Copies
         进化后：已进化 1..Level 消耗的副本数之和 + Copies + Level
```

- `item.Rarity` 在进化后返回 Divine。
- 宠物等级不用 `item.EffectiveLevel`：原作进化会扣掉副本，`EffectiveLevel` 从副本数（≥ 进化需求，如普通 100、优秀 130）跌回 `Level` = 1，比例会下降 40%~60%。宠物等级改为累计收集的副本数，进化时不变，于是首次进化只多了 Divine 系数和 +1 级（神话保持 1.7 系数，只 +1 级）；之后每次进化 +1 级，进化后的每个副本也 +1 级。消耗数用 `CombatBalanceConfig.GetEvolutionShardsRequirement`，与 `ItemModel.LevelsToEvolve` 相同。
- 公式只写在一处：`CodeBase/Pet/PetStats.cs` 的 `PetStats.GetRatios(def, rarity, level)`，另有 `GetLevel(ItemModel)`、`GetRatios(ItemModel, def)`、`GetNextRatios`、`GetOwnAttack`、`GetAttackSpeedMultiplier`、`FindDefinition`。属性同步、`DamageCalculator`、`PetMovementSystem` 和 UI 都调用它。
- 生命、攻击、防御三个比例按这个公式随稀有度和等级增长。**战斗移速不成长**，只用种类比例（`MoveSpeedRatio`），已确认。

### 15.2 配置值

`PetConfig` 全局字段（`Assets/Resources/Pet Config.asset`）：

| 字段 | 值 | 说明 |
|---|---|---|
| `PlayerBaseAttack` | 10 | 宠物模式下玩家 Damage 属性的初始值 |
| `OwnAttackScale` | 0.6 | 自身攻击 = 武器原伤害 × 0.6 × 种族 `OwnAttackMultiplier` |
| `LevelRatioGrowth` | 0.02 | 每级比例增长 2% |
| `RarityRatioFactor` | [1.0, 1.1, 1.2, 1.35, 1.5, 1.7, 1.7] | 按 `Rarity` 枚举 Common、Uncommon、Rare、Epic、Legendary、Divine、Mythical |
| `FollowSpeedMultiplier` | 1.2 | 不变，脱战跟随用 |

`PetDefinition` 种族字段：`HealthRatio`、`AttackRatio`、`DefenceRatio`、`MoveSpeedRatio`、`OwnAttackMultiplier`、`DisplayName`。C# 默认值都是 1，字段缺失时仍能运行。

| DamageType | 外观 prefab | 名字 | 生命 | 攻击 | 防御 | 移速 | 自身攻击 |
|---|---|---|---|---|---|---|---|
| 3 钝击 Buckshot | `buck_buck_medium` | 胖子 | 1.2 | 0.6 | 1.2 | 0.85 | 1.0 |
| 1 斩击 Cutting | `cut_cut_easy` | 斧头兵 | 0.8 | 0.8 | 1.0 | 1.0 | 1.0 |
| 0 穿刺 Stabbing | `pier_pier_easy` | 长矛兵 | 0.7 | 1.0 | 0.8 | 1.1 | 1.0 |

### 15.3 各属性组

| 属性组 | 规则 | 实现位置 |
|---|---|---|
| 生命 | 宠物最大生命 = 玩家生命 × 生命比例 | `PetStatsSyncSystem.Build`：Health 加 PercentMultiplicative 修饰 `pet.ratio`（最大生命读原始值，所以用 PercentMultiplicative） |
| 攻击 | 宠物攻击 = 自身攻击 + 玩家攻击 × 攻击比例 | 见下 |
| 防御 | 宠物受击时防御值 `d` 乘防御比例 | `DamageCalculator` 原有的宠物目标分支，`DefenceMultiplier` 换成 `GetRatios(...).Defence` |
| 移速 | 战斗中（有 `CurrentTarget`，追击或出手）= 玩家实际移速（`GetStatSpecificValue`，m/s）× 移速比例；脱战跟随 = 玩家移速 × 1.2，跑远照旧瞬移 | `PetMovementSystem` |
| 攻速 | 不用比例，就是武器自己的 `AttackCooldown`（再乘能力倍率，与原来一致） | `PetAttackSystem`，种族攻速系数已删除 |
| 暴击、闪避、吸血、回血等 | 1:1 继承，不乘比例，UI 不显示 | 不变 |

攻击拆成两部分：

```
自身攻击 = item.Damage × OwnAttackScale × OwnAttackMultiplier        // "宠物的直接增伤需要缩小"
玩家攻击 = 玩家 Damage(StatType 3) + 玩家 Stat(W.DamageStatTypeFlat)
宠物 dmg = Pet.Stat(W.DamageStatTypePercent)                          // 类型百分比不乘比例，照旧乘整体
           × ( 自身攻击 + 玩家攻击 × 攻击比例 + AbilityDamageFlat … )
           × (1 − 抗性) × 暴击 × …                                    // 其余同 5.3
```

- `PetStatsSyncSystem.Build` 给宠物的 Damage 和对应类型的 `*DamageFlat` 加 PercentMultiplicative 修饰 `pet.ratio`，值为攻击比例。
- `DamageCalculator` 在 Source 是 `PetUnit` 时，用 `PetStats.GetOwnAttack(data.Weapon.Damage, def)` 代替 `data.Weapon.Damage`。
- `SimulationSystem` 和 `SolveAttackDamage*` 的 Source 是玩家或敌人，不经过宠物分支，行为不变；只是宠物模式下玩家 Damage 多了 10。
- 宠物属性的 BaseValue 改取玩家**运行时**的基础值，不再取 `PlayerConfig`，这样宠物能看到 +10。

### 15.4 玩家基础攻击 10

- 只在宠物模式下：`PlayerModel` 构造时创建 `LastStats` 后，给 Damage 的 BaseValue 加 `PlayerBaseAttack`（10）。爪子攻击按它出伤，宠物通过继承把它算进玩家攻击。
- 不会存档叠加：`SaveStats` 会写出 BaseValue，但 `LoadStats` 不读回（保留 `PlayerConfig` 的值），其他地方也不读存档里的 `base_value`。
- 关闭宠物模式时不加。运行中途关掉 `PetConfig.Enabled` 时，这 10 点要重启才消失。

### 15.5 升级规则

- 武器加副本（进化前后都算）和进化都会提高宠物等级或 `Rarity`，于是比例按 15.1 上涨；自身攻击随原作武器伤害表（`CalcDamage`、进化表）上涨。
- `PetModel.SyncedRatios` 记录上次同步的比例。比例变化时重建该宠物的 Health、Damage 和类型 Flat 属性，不必等玩家属性的 Version 变化。
- 下一级预览：`GetNextRatios` 用宠物等级 + 1 和当前 `item.Rarity`（进化前是多一个副本，进化后是下一次进化，两者都是 +1 级），下一级自身攻击用 `item.NextLevelDamage`。首次进化（未进化 → Divine Lv1）没有"下一级"预览；进化界面直接比较新旧两个 ItemModel。

### 15.6 UI

只在宠物模式下生效，关闭时各界面走原作代码。凡是显示武器伤害或 DPS 的地方改为显示宠物：

```
长矛兵
生命 0.84倍 (11.1K)
攻击 965 (自身 720 + 玩家 167 × 1.46)
防御 1.02倍
移速 1.10倍 (战斗中)
攻速 每秒 1.19 次
```

- 生命同时显示比例和绝对值；攻击显示总值和拆分；防御和移速只显示比例；攻速 = 能力攻速倍率之积 × `AbilityAttackSpeedMultiplier` / `AttackCooldown`（`PetStats.GetAttackSpeedMultiplier`，与 `PetAttackSystem` 相同，不含随目标变化的弱敌倍率），宠物 DPS 用同一攻速。爪子 DPS 含玩家 Damage 属性（基础攻击 10 + 吞噬所得）。
- 运行时字体不设 `DontSave`，在 `Application.quitting`（编辑器退出播放模式时也会触发）里销毁，避免每次播放泄漏一份系统字体和图集。
- 绝对值用玩家当前属性，每 0.5s 不受时间缩放刷新一次（`PetStatsText.RefreshInterval`）。原因是 `PetStats` 的继承缓存不登记 UniMob 依赖。数字用 `.Format()`，所以 11053 显示为 11.1K；比例用 `0.00`。
- 有"下一级"的界面在变化的数值后追加 `<sprite name=atlas_59>` 箭头和绿色新值。
- 涉及的界面：背包物品详情 `InventoryItemContextMenu`（含下一级）、`ItemMiniView`（只有当前值）、`EvolveItemView`（旧 → 新，替换伤害行）、`InventoryStatView`（Damage 显示全部宠物 DPS + 爪子 DPS）、宝箱和武器奖励弹窗（旧 → 新的伤害数字改为宠物攻击）。
- 实现：`CodeBase/UI/PetStatsText.cs`。属性块在运行时克隆界面上已有的 TMP 文本，没有改 prefab 和场景 YAML。原有行保留本地化标签（DAMAGE 等），只把数值换成宠物的。中文缺字（胖、兵、矛、宠）由运行时创建的系统 CJK 字体副本补到 fallback，不修改共享字体资源。
- 背包排序和"一键装备最佳"仍按 `item.DPS`，不变。

### 15.7 算例

第 3 岛拿到的稀有（Rare）pick，Lv12，穿刺类，对应长矛兵。玩家继承的穿刺类型 Flat 为 157。

| | 原作 | 方案 A |
|---|---|---|
| 武器伤害部分 | 1200 | 自身攻击 = 1200 × 0.6 × 1.0 = 720 |
| 玩家部分 | 157（Damage 0 + Flat 157） | 玩家攻击 = 10 + 157 = 167 |
| 攻击比例 | — | 1.0 × 1.2 × (1 + 0.02 × 11) = 1.464 |
| 每击（乘类型百分比之前） | 1200 + 157 = **1357** | 720 + 167 × 1.46 ≈ **965** |

同一只宠物的其余比例：生命 0.7 × 1.464 = 1.02×，防御 0.8 × 1.464 = 1.17×，战斗移速 1.1×（不随稀有度和等级变化）。

单击伤害约为原作的 71%。武器本身伤害的权重下降，玩家吞噬成长通过攻击比例放大后占比上升。
