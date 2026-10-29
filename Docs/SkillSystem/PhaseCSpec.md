# 技能系统 C 期规格：宠物和主角主动施法、宠物增益、种类技能共用

状态：开工规格（2026-10-29）。前置：[SkillSystemDesign.md](SkillSystemDesign.md) 第 1～10 节、[Phase2Spec.md](Phase2Spec.md)。
路径缩写：`CB/` = `Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/`，`A/` = `Recovery/StudyProject/Assets/`。

## 0. 用户已定的规则

- 前摇是技能本身的属性（`SkillDef.CastPoint`），谁放都有；**预警圈只在敌人施放时画**。
- 宠物增益要真正生效；宠物和主角要能按 AI 规则自动施法。
- 敌人用和"自己同模型的宠物"相同的种类技能。
- 遗留问题只修一个：Boss 塔竞技场小兵拿到"远处已隐藏"的对象池实例而看不见。
- 省 token：Codex 写代码和做初审，Claude 只做规划、关键审查和编译。

通用约束同第二期：不启动 Unity；不改 `Main.unity`、`Balance/`、`Docs/Balance/`、`*.mat`；不 commit；新 `.cs` 手写 `.meta`；`[SerializeReference]` 类不改名；没配置时行为不变；宠物模式关闭时没有任何宠物技能。

## 1. 代码（Lane X：`CB/Skills/**`、`CB/Pet/**`、`CB/Player/**`、`CB/BossTower/BossMinionService.cs`、`CB/Services/Damage/**`、`CB/Unit/Systems/UnitDamageSystem.cs`）

### X1 竞技场小兵修复
- 把第二期只放在 `BossMinionService.SpawnSummon` 里的 `ResetFarState` 用到所有 `BossMinionService.Spawn`（Boss 塔竞技场小兵、巫妖骷髅都受益）。

### X2 友方增益（宠物和主角）
- 友方单位身上的 Buff 一律按"读取时乘系数"生效，不加属性修正：
  - `Damage × PercentMultiplicative` → 输出伤害系数（主角和宠物的普攻、技能伤害都乘）。新增。
  - `AttackCooldown`（攻击间隔）、`GlobalDefence`（防御）、`MovementSpeed`（移速）：沿用第二期的系数。
  - 回血用 `OnTick` 的 `HealEffect`。
- 来源区分：友方施加给友方的 Buff 可以是增益也可以是减益；敌人施加给友方的仍只允许眩晕、降防、降攻速、减速（第二期规则不变）。其他属性类型 `WarnOnce` 后忽略。
- 宠物死亡、复活、回收时清 Buff；切换宠物模式时清。

### X3 自动施法
- **宠物**：`PetSpawnSystem` 生成宠物时按 `SkillLibrary.FindLoadout(Pet, itemKey)` 安装 `SkillCasterComponent`；复活、回收、换装备时重置冷却；宠物模式关闭不安装。
  - 施法流程和敌人相同（思考间隔、优先级、Pick、UseWhen、前摇、后摇），但**不画预警圈**。
  - 前摇和后摇期间宠物停止移动和普攻（`PetMovementSystem`、`PetAttackSystem` 加施法守卫）；被眩晕打断。
  - Pick 对宠物的含义：`CurrentTarget` = 宠物当前攻击目标；`NearestHostile` = 离宠物最近的敌人；`LowestHpAlly` = 主角和宠物里血量比例最低的；`Self`；`Hero` = 主角。
  - 前摇开始时播一次宠物的攻击动画（有就播，没有就不播）。
  - 宠物技能伤害沿用 `SkillService.DealDamage` 的宠物公式，再乘 X2 的输出伤害系数。不触发命中类物品技能（原规则）。
- **主角**：按 `FindLoadout(Hero, "hero")` 安装，默认没有技能（配置为空则不装）。主角前摇期间可以移动，但不普攻；被眩晕打断。
- `CastSkillEffect`（物品技能命中触发的瞬发施法）保留不变。

## 2. 内容（Lane Y：`A/Resources/Skills/**`、`A/Resources/localization/LocalizationDatabase.asset`、`A/MonoBehaviour/pet_golem.asset`）

- 新建 `A/Resources/Skills/Species Skills.asset`（`SkillLibrary`，`Resources.LoadAll` 会合并），放 36 个种类技能、它们用到的 Buff 和宠物 Loadout（`OwnerKind: Pet`，Key = 物品 Key）。已有敌人装配的 prefab 继续在 `Enemy Skills.asset` 里追加，**同一个 Loadout Key 只能出现在一个文件里**（重复会被警告）。
- 每个种类一个主动技能，Key `sp_<物品Key>`，沿用该种类招牌技的主题；三种已有技能直接共用：骷髅兵 = `bone_volley`，石魔像 = `slam`，其余见下表。删掉 `pet_golem.asset` 里第一期的 `pet_skill_golem_slam`（被自动施法取代；本地化行可留）。
- **敌人侧**：把种类技能追加到该种类模型 prefab 的敌人装配里（没有就新建）。**排除**：`*_Summoned`（巫妖召唤物）和所有 `Boss_*`（Boss 塔 Boss 自带机制）。
- 本地化：每个种类技能 `SKILL_NAME_sp_*` 和 `SKILL_DESC_sp_*`，另补 `SKILL_DESC_slam`、`SKILL_DESC_bone_volley`；13 列都填（zh 中文，其余英文）。

| 物品 Key | 宠物 | 模型 prefab | 技能（Key / 名字 / 内容） |
|---|---|---|---|
| paddle | 胖子 | buck_buck_medium | sp_paddle 暴食：自身回 20% 生命，生命低于 60% 时用，CD 14 |
| mailbox | 棍棒兵 | cut_buck_medium | sp_mailbox 当头一棒：单体 Power 1.5 + 眩晕 0.8 秒，CD 10 |
| hammer | 重锤队长 | buck_buck_boss | sp_hammer 震地：自身周围半径 3，Power 1.8，CD 10 |
| mace | 锤骑士 | cut_buck_knight | sp_mace 骑士之怒：自身伤害 ×1.4，6 秒，CD 16 |
| coolHammer | 战锤领主 | all_buck_knight | sp_coolHammer 领主号令：8 米内友方回 10% 生命 + 伤害 ×1.2 5 秒，CD 20 |
| knife | 斧头兵 | cut_cut_easy | sp_knife 掷斧：直线飞弹 Power 1.4，速度 12，CD 8 |
| minisaw | 剑士 | cut_cut_medium | sp_minisaw 疾风连斩：自身攻击间隔 ×0.75，5 秒，CD 14 |
| katana | 阔剑卫士 | pier_cut_medium upg | sp_katana 卫士意志：自身防御 ×1.5 6 秒 + 回 8% 生命，CD 16 |
| doubleaxe | 巨剑队长 | cut_cut_boss | sp_doubleaxe 巨剑横扫：扇形 120° 半径 4，Power 2.0，CD 10 |
| chainsaw | 巨剑骑士 | cut_cut_knight | sp_chainsaw 骑士冲锋：直线 8×1.5，Power 1.8，CD 12（第三期改成真正的冲锋位移） |
| pike | 长矛兵 | pier_pier_easy | sp_pike 投矛：穿透直线飞弹 Power 1.2，CD 8 |
| fork | 矛卫 | pier_pier_medium | sp_fork 锐目：自身伤害 ×1.3，5 秒，CD 14 |
| pick | 重甲矛兵 | buck_pier_boss | sp_pick 破甲刺：单体 Power 1.6 + 目标防御 ×0.7 6 秒，CD 10 |
| coolpike | 戟兵队长 | pier_pier_boss | sp_coolpike 长戟横扫：直线 6×2，Power 2.0，CD 10 |
| chainsawpike | 枪骑士 | pier_pier_knight | sp_chainsawpike 骑枪突刺：直线 9×1.2，Power 2.4，CD 12 |
| skeleton | 骷髅兵 | Skeleton_Easy | 共用 bone_volley |
| skeleton_guard | 骷髅卫士 | Skeleton_Medium+_Summoned | sp_skeleton_guard 盾击：单体 Power 1.2 + 眩晕 1 秒，CD 12（敌人侧排除） |
| golem | 石魔像 | elite_buck | 共用 slam |
| cultist | 秘教徒 | vip enemy | sp_cultist 禁忌电光：点目标半径 2.5，前摇 1.2，Power 2.0，CD 12 |
| skeleton_king | 骷髅王 | Skeleton_Boss_Summoned | sp_skeleton_king 王者号令：8 米内友方回 12% + 防御 ×1.3 6 秒，CD 20（敌人侧排除） |
| vampire_thrall | 吸血鬼仆从 | Vampire_Easy | sp_vampire_thrall 吸血撕咬：单体 Power 1.3 + 自身回 8%，CD 10 |
| vampire_blade | 吸血鬼剑士 | Vampire_Medium+ | sp_vampire_blade 血刃回旋：自身周围半径 3，Power 1.5 + 自身回 10%，CD 12 |
| bear | 战熊 | elite_pier | sp_bear 熊掌重拍：扇形 90° 半径 3，Power 1.8 + 眩晕 1 秒，CD 12 |
| ghost | 幽灵 | ghost | sp_ghost 幽影突袭：单体 Power 2.5，前摇 0.4，CD 10（第三期加闪现） |
| abyss_reaver | 深渊收割者 | undead_pierce_cut_elite | sp_abyss_reaver 深渊收割：自身周围半径 4，Power 2.2 + 减速，CD 12 |
| zombie | 僵尸 | Zombie_Easy | sp_zombie 不死之躯：生命低于 50% 时自身回 25%，CD 20 |
| zombie_brute | 僵尸暴徒 | Zombie_Medium+ | sp_zombie_brute 蛮力猛击：单体 Power 2.0 + 眩晕 1 秒，CD 12（敌人侧和原 slam 并存） |
| treant | 树人 | elite_cut green | sp_treant 自然馈赠：8 米内友方回 15%，CD 16 |
| mage | 法师 | mage | sp_mage 奥术飞弹：直线飞弹 Power 1.8，速度 11，CD 8 |
| dark_treant | 黑暗树妖 | undead_cut_buck_elite | sp_dark_treant 根须缠绕：点目标半径 3，Power 1.5 + 减速 50% 3 秒，CD 12 |
| boss_crab | 熔岩巨蟹 | Boss_Crab | sp_boss_crab 熔岩爆裂：自身周围半径 4，前摇 1.0，Power 2.5，CD 10（敌人侧排除） |
| boss_vampire_king | 吸血鬼王 | Boss_Vampire_Boss | sp_boss_vampire_king 猩红盛宴：友方回 20% + 伤害 ×1.3 6 秒，CD 20（敌人侧排除） |
| boss_hydra | 九头蛇 | Boss_Hydra_Small | sp_boss_hydra 风暴吐息：3 支直线飞弹，散射 30°，每支 Power 1.5，CD 10（敌人侧排除） |
| boss_lich | 巫妖 | Boss_Lich | sp_boss_lich 死亡新星：点目标半径 3.5，前摇 1.2，Power 3.0，CD 12（敌人侧排除） |
| boss_viking_king | 维京王魂 | Boss_Viking_King_Ghost | sp_boss_viking_king 亡魂横扫：扇形 150° 半径 5，Power 2.5 + 减速，CD 12（敌人侧排除） |
| boss_berserker | 狂暴恶魔 | Boss_Berserker | sp_boss_berserker 恶魔狂暴：生命低于 70% 时自身伤害 ×1.6 + 攻击间隔 ×0.7 8 秒，CD 20（敌人侧排除） |

- 前摇默认：伤害类 0.6～0.9 秒，大范围 1.0～1.4 秒，自身增益和治疗 0.3～0.5 秒；后摇 0.2～0.4 秒。射程按技能形状给（近战单体 3，直线和飞弹 8～10，点目标 8）。数值都是起始值。
- AutoCast：伤害技能 `EnemiesInRadius`/`TargetDistance` 判断；治疗 `SelfHealthBelow` 或 `AlliesHurtInRadius`；自身增益在有敌人时用（`EnemiesInRadius` 半径 6）且 `HasBuff` 取反防重复。
- 所有 Buff 和 Vfx 引用的 Key 都要存在；能复用第二期的 Buff（`slow_30`、`armor_break`、`weakness`）就复用。直线飞弹外观用 `bone_bolt`（或另配现有特效）；范围技能用 `slam_smash` 或现有命中特效。

## 3. 宠物界面（Lane Z：`CB/UI/Pet/**`、`CB/UI/Inventory/ItemMiniView.cs`）

- 技能弹窗第一行显示宠物的种类主动技能：分类图标、`SKILL_NAME_*`、`SKILL_DESC_*`、"主动 · 冷却 N 秒"；摘要行把它算进图标和数量里。没有种类技能或宠物模式关闭时不显示。
- 缺本地化键时不刷日志（用一次性检查）。

## 4. 流程

- Lane X（X1+X2 → X3 依次）、Lane Y、Lane Z 并行，文件不重叠。Lane Y 只按本规格写数据；Lane X 不改资产。
- 每条 Lane：Codex 实现并自查，Claude 看 diff 的关键部分、编译、修。
- 汇总后：整体编译 → 2 个 Claude 审查（运行时/回归、规格/数据）+ 1 路 Codex 审查 → 只验证中高优先级问题 → 修复 → 在 `SkillSystemDesign.md` 加第 11 节。
