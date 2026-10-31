# 英雄小队 RPG 模式设计稿

> 状态：设计稿，2026-10-29。决定见第 1 节和第 17 节，均已确认。RPG 模式是在单独分支上做的实验，和原玩法对照测试，不代表方向已定。所有数值都是参考值，切片试玩后再调。
>
> 路径约定：`CB/` = `Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/`，`Assets/` = `Recovery/StudyProject/Assets/`。

## 1. 已确认的决定（2026-10-29）

| # | 决定 |
|---|---|
| 1 | 长期存档：一个存档一直成长，不按局重置。主线通关后开难度档，反复刷。 |
| 2 | 所有英雄是同一种单位、同一套规则，玩家随时切换操控任意存活英雄，其余英雄由 AI 控制（第 19 节，覆盖原来的"只操控一个英雄"）。每个英雄有一个手动大招，其余技能自动施放。加仇恨系统。 |
| 3 | 先做 5 个英雄，技能专门设计。稀有度分 3 档，稀有度越低技能越少，暂定 1、2、3 个。 |
| 4 | 杀怪不再给永久属性，改为经验和金币。吞噬降成次要系统。 |
| 5 | 刷怪点保留。去掉每日次数，缩短重生冷却。经验不按等级差递减（2026-10-30 关闭）。 |
| 6 | 满级后靠难度档和随机掉落撑起反复刷。 |
| 7 | 新成长层单独写，用开关和原作并存。原作的战斗、技能、刷怪、任务、地图当底座。 |
| 8 | 先做第 1–2 岛的验证切片，再决定是否铺到 17 个岛。 |
| 9 | 做成单独分支（建议 `feature/rpg-mode`，从 `feature/pet-system` 切出，因为要用宠物单位和技能层）。原玩法保留，两种玩法都要测试对照。 |

## 2. 核心循环与节奏

5 人小队在原作的岛屿上打怪，拿经验升级，拿金币和掉落换装备，打精英和 Boss 开门推进主线。主线通关后切更高难度，在同一张地图上反复刷更好的装备。

```
打怪（经验、金币） → 升级、买装备、拿掉落 → 打精英和 Boss（首杀加成、开门）
       ↑                                                        ↓
 难度档（主线通关后） ←──── 主线任务、悬赏 ←──── 进入下一个岛
```

节奏目标（切片验收用）：

- 连续玩 30–60 分钟：升级 3–6 次，换 1–2 件装备，打 1 个精英或 Boss。
- 第 1 岛结束约 5 级，第 2 岛结束约 10 级，合计 60–120 分钟。原作第 2 岛结束在第 110 活跃分钟，可以对照。
- 等重生的时间不超过 10%。

## 3. 原作系统怎么处理

开关为 `RpgModeConfig.Enabled`，关闭时行为与现在一致。

| 系统 | RPG 模式 |
|---|---|
| 移动、摇杆、键盘、相机 | 保留 |
| 战斗结算：伤害、护甲、暴击、闪避、格挡 | 保留 |
| 技能与 Buff 层 | 保留并扩展：嘲讽、拉拽、复活、持续区域、手动施放 |
| 敌人 AI 与敌人技能 | 保留，选目标改成按仇恨 |
| 宠物单位：跟随、寻路、瞬移、复活、攻击 | 改造成 AI 队友 |
| 刷怪点 | 改规则，见第 9 节 |
| 吞噬 | 动画保留，用来表现击杀回收；永久属性只剩首杀加成 |
| 门、岛屿、NavMesh | 保留 |
| 主线任务、剧情、过场 | 保留，奖励换算成经验、金币、装备 |
| 泉水 | 保留。宠物模式下已经能复活和治疗宠物 |
| 地图宝箱（57 个） | 保留发现和开箱，奖励换成金币和装备 |
| 血条 | 改成显示敌人等级；首杀加成未领时显示加成图标 |
| 刷新倒计时 | 保留 |
| 怒气 | 关闭 |
| 武器、头盔、盾牌、水晶，以及副本和进化 | 关闭，换成新装备 |
| 宠物收集、图鉴、羁绊、万能碎片 | 关闭 |
| 护符、灵魂之树 | 关闭 |
| 矿区升级 | 关闭。任务里的矿区剧情另行处理，见第 18 节 |
| Boss 塔 | 关闭，以后可改成终局挑战 |
| 水晶活动及其它活动 | 关闭 |
| VIP、付费商店、首充、广告属性、通行证、返现 | 关闭 |
| 离线模拟与收益预测（`SimulationSystem`） | 关闭 |

## 4. 英雄

### 4.1 名单

稀有度三档：普通 1 个技能，稀有 2 个，史诗 3 个。技能数包含大招，所以普通英雄只有一个大招。稀有度只决定技能数量，不改属性比例。

主角是英雄之一，按史诗档做，不参与稀有度获取。玩家可以切换操控任何英雄，见第 19 节。4 个队友是 2 个普通、2 个稀有。

| 英雄 | 稀有度 | 技能数 | 定位 | 普攻 | 伤害类型 | 模型（沿用现有敌人） | 加入时间 |
|---|---|---|---|---|---|---|---|
| 主角 | 史诗 | 3 | 近战，玩家操控 | 近战 | 劈砍 | 原主角 | 开局 |
| 石魔像 | 普通 | 1 | 坦克 | 近战 | 砸击 | 石魔像 | 第 1 岛 Boss 首杀 |
| 秘教徒 | 稀有 | 2 | 治疗 | 远程 6m | 砸击 | 秘教徒 | 第 2 岛中段任务 |
| 骷髅射手 | 普通 | 1 | 远程输出 | 远程 7m | 穿刺 | 15 号岛远程骷髅，带骨箭 | 第 3 岛（切片后） |
| 法师 | 稀有 | 2 | 范围与控制 | 远程 7m | 法术（`WeaponType.Magic`） | 法师 | 第 4 岛（切片后） |

模型、缩放和隐藏节点沿用现有宠物种类的 `PetDefinition`。伤害类型按英雄重新定，不沿用宠物表。

### 4.2 技能

"攻击"指英雄的 `Damage` 属性。技能伤害按攻击的百分比算，会随等级和装备一起涨，不需要单独的技能等级。职业特性（仇恨倍率、属性比例）是数值，不算技能。

**主角**：横扫 1 级、大招·吞噬 3 级、钩锁 5 级解锁，兼作新手引导。大招放在第 1 岛就解锁，让玩家早点学会按大招。

| 技能 | 方式 | 效果 | 冷却 | 实现 |
|---|---|---|---|---|
| 横扫 | 自动 | 前方 120° 扇形 3m，150% 攻击 | 6s | 现有效果 |
| 钩锁 | 自动 | 把 8m 内最远的一个敌人拉到身前，100% 攻击，眩晕 1s | 12s | 新增"拉拽"效果，表现借用吞噬钩子 |
| 大招·吞噬 | 手动 | 周围 4m 内敌人 400% 攻击，之后 6s 吸血 +20% | 40s | 现有效果（范围伤害 + Buff） |

**石魔像**（坦克）

| 技能 | 方式 | 效果 | 冷却 | 实现 |
|---|---|---|---|---|
| 大招·岩石怒吼 | 手动 | 嘲讽 6m 内敌人 4s；自身受到的伤害 −40%，持续 6s | 30s | 新增"嘲讽"效果 |

**秘教徒**（治疗）

| 技能 | 方式 | 效果 | 冷却 | 实现 |
|---|---|---|---|---|
| 治疗术 | 自动 | 血量最低且低于 70% 的队友，回复其 20% 最大生命 | 5s | 现有效果 |
| 大招·群体复苏 | 手动 | 全体回复 40% 最大生命，复活阵亡队友（30% 血） | 60s | 新增"复活"效果，复用宠物复活逻辑 |

**骷髅射手**（远程输出）

| 技能 | 方式 | 效果 | 冷却 | 实现 |
|---|---|---|---|---|
| 大招·箭雨 | 手动 | 敌人最密集处 5m 圆形，2.5s 内分 5 次共 300% 攻击，命中减速 30% 2s | 35s | 新增"持续区域"效果；不做就改成一次 250% 加减速 |

**法师**（范围与控制）

| 技能 | 方式 | 效果 | 冷却 | 实现 |
|---|---|---|---|---|
| 冰霜新星 | 自动 | 敌人最密集处 4m 圆形，150% 攻击，减速 40% 3s | 8s | 现有效果 |
| 大招·雷暴 | 手动 | 8m 内全部敌人 350% 攻击，眩晕 1.5s | 45s | 现有效果 |

合计 9 个技能，要新增 4 种效果：拉拽、嘲讽、复活、持续区域。

### 4.3 属性与成长

英雄属性 = 基础值 × 定位比例 × 等级成长 + 装备 + 首杀加成。不再继承玩家的杀怪属性；[宠物第一步](../PetSystem/Step1Design.md)第 15 节的比例公式在 RPG 模式下不用。

| 属性 | 主角 1 级基础值 | 每级 |
|---|---|---|
| 生命 | 120 | ×1.08 |
| 攻击 | 10 | ×1.08 |
| 防御（通用） | 2 | ×1.08 |
| 攻击间隔 | 1.0s | 不变 |

60 级时等级成长约为 1 级的 94 倍（1.08^59），装备再提供约 3–5 倍。每级固定比例，是为了让"高几级"在全程都有一样的分量，等级差的经验递减才说得通。

| 英雄 | 生命 | 攻击 | 防御 | 仇恨倍率 |
|---|---|---|---|---|
| 主角 | 1.0 | 1.0 | 1.0 | 1 |
| 石魔像（坦克） | 1.6 | 0.6 | 1.5 | 3 |
| 秘教徒（治疗） | 0.8 | 0.7 | 0.8 | 1 |
| 骷髅射手（远程） | 0.7 | 1.3 | 0.6 | 1 |
| 法师（控制） | 0.7 | 1.2 | 0.6 | 1 |

### 4.4 获得

- 英雄靠主线进度招募，不抽卡，不掉副本。招募后有空格就自动上阵，没有空格就进名单当替补，玩家可以在背包里自己换人。
- 队伍上限是主角加 4 个队友。队友格子随主线开放（2026-10-30，10-31 改为开局给 1 个队友格）：
  - 开局是主角加 1 个队友格。
  - 每个岛只有一只开门怪，都是岛 Boss。第一次打死它、打开通往下一岛的门时，再开一个队友格。第 3 岛 Boss 之后满 5 人。
  - 门槛配在 `RpgModeConfig.CompanionSlotUnlockLocations`，默认 `{1, 2, 3, 4}`，按 `BossProgressLocationNumber` 判断（开局为 1）。
  - 格子状态每次都按进度重新算，不读存档里的值。老存档里超出的队友会退回名单。
  - 游戏中开新格时，自动补上名单里星级最高的英雄，并提示"队伍格子解锁（n/5）"。
  - 锁住的格子上显示"第 N 岛 Boss"。

## 5. 操作与 AI

### 5.1 主角

- 摇杆移动，普攻自动，沿用现有主角攻击。
- 两个普通技能自动施放（`SkillAutoCastSystem`），大招手动。
- 躲敌人的预警技能靠玩家自己走位。

### 5.2 大招按钮

- 屏幕右下：主角大招一个大按钮，旁边每个队友一个小按钮，显示头像、冷却圈和血条。英雄阵亡时按钮变灰。
- 点一下就放。目标按技能自己的选目标规则自动选（最密集的敌群、血最少的队友等），不用瞄准。拖动瞄准以后再考虑。
- 用冷却控制，不做能量。
- 大招不放进自动施放的技能槽。`SkillAutoCastSystem` 里的选目标（`Pick`）和开始施法（`StartCast`）现在是私有的，抽成公共入口，手动大招走同一条路径，前摇、预警、效果执行都复用。

### 5.3 队友 AI

跟随、拴绳 12m、瞬移 18m、交战半径 8m 沿用 `PetConfig`。改的是站位和选目标：

| 定位 | 站位 | 选目标 |
|---|---|---|
| 坦克 | 主角前方约 2m | 优先打正在打其他英雄的敌人（抢仇恨），其次主角的目标 |
| 远程输出、控制 | 主角后方 3–4m，分在两侧 | 现有规则：主角的目标 → 正在打自己的敌人 → 离主角最近的敌人 |
| 治疗 | 主角后方 3–4m | 治疗由自动施法处理（目标血量低于 70%），其余时间同输出 |

- 远程队友和目标保持 6–7m。敌人贴近到 3m 内时后撤，借用敌人远程普攻的后撤参数（`RangedAttackDef.MinRange` 等）。
- 远程普攻现在只有敌人有：`UnitSkillLoadout.RangedAttack` 注明 "Enemies only"，`SkillService.LaunchRangedAttack` 的施法者类型是 `EnemyUnit`。要放宽成 `UnitCore`，开放给英雄。

### 5.4 躲预警

- 数据已经有：敌人施法期间，`SkillCastingComponent` 里存着施法者、技能（预警形状和尺寸、前摇时长）、目标点、朝向和已过时间。只需要加一个查询，列出所有敌人正在施放、带预警的技能区域和剩余时间。没有锚定目标的预警会跟着施法者移动，查询时按施法者当前位置算。
- 队友每 0.2s 检查自己是否在预警区里。在的话，朝离开区域最近的方向走出去，期间不普攻；来不及也照走。
- 切片验收：走得出去的情况下，队友至少 70% 能躲开。

## 6. 仇恨（2026-10-30 简化为魔兽争霸 3 式）

不做仇恨表。敌人按"锁定目标"选人，坦克靠嘲讽技能抢怪。只在 RPG 模式下生效，原作和宠物模式不变。

**锁定目标**（替换 `EnemyAttackSystem.SelectAggroTarget` 的 RPG 分支）：

1. 被嘲讽：身上有嘲讽 Buff 且嘲讽者活着时，只打嘲讽者，期间不脱战、不回巡逻。
2. 已锁定：上一次选中的英雄还活着，且距离不超过 `max(追击范围, 攻击距离) + 4m`，就继续打它，不因为别人更近而换人。
3. 反击：没有锁定目标时，最后一个打它的英雄（3 秒内）成为目标。
4. 都没有：打最近的英雄（即现在的规则）。

目标阵亡、跑远或敌人脱战时清掉锁定。30m 外回满血、主角不可用时全体回巡逻，这两条仍以主角为准。

**嘲讽**：Buff 上的一个开关（同眩晕、沉默）。带嘲讽的 Buff 挂在敌人身上时，强制攻击施放者。精英和 Boss 的嘲讽时长减半。

| 英雄 | 技能 | 方式 | 效果 | 冷却 |
|---|---|---|---|---|
| 重甲矛兵（坦克） | 群体嘲讽 | 自动：6m 内有敌人在打其他英雄时施放 | 嘲讽 6m 内全部敌人 3 秒（精英、Boss 1.5 秒） | 12s |

暂时只有重甲矛兵有嘲讽。单体嘲讽以后需要时再加，Buff 开关和施放条件可以直接复用。嘲讽是 RPG 额外装上的技能，不替换英雄原有的物种技能。第 4.2 节石魔像"岩石怒吼"的嘲讽部分由本节代替。

## 7. 等级与经验

- 队伍共享一个等级和一条经验条。新招募的英雄直接和队伍同级，换人不吃亏。
- 主线等级上限 60。
- 每只怪的经验：`xp(L) = 20 × 1.1^(L−1)`，L 是怪物等级。精英 ×5，岛 Boss ×20。树等静态物和普通怪一样（2026-10-29 从 0 改为 0.5，2026-10-30 改为 1）。
- 升级所需：`need(L) = (1.5 + 0.15L) × xp(L)`。从 L 级升到 L+1 级，大约要打 1.5+0.15L 只同级普通怪。2026-10-29 试玩发现升级太慢，所需经验降为原来的 1/10；2026-10-30 每级增量从 0.3 降到 0.15，曲线变缓（14 级升级需求 394 → 249，1→60 累计约 90 万 → 49 万）。
- 任务和悬赏的经验占总经验约 25%。

等级差修正（队伍等级 − 怪物等级）：

已关闭（2026-10-30）：不论等级差，经验一律 100%。之前先是 5 级差 15%、6 级差以上为 0，后改成最低 50%，最后按用户要求取消递减。`XpDifferenceFactors` 只留一项 1，需要时可在配置里重新加档。

- 升级时全队播特效、回满血。
- 吞噬动画保留：怪物死后照样被拉向主角，动画结束时飘出"+经验 +金币"。

## 8. 金币与装备

### 8.1 金币

- 货币 Key 为 `rpg.gold`，登记到 `Currency Style Config.asset`，做法同 `pet.universal_shard`。
- 每只怪：`gold(L) = 5 × 1.1^(L−1)`。精英 ×5，Boss ×20，树等静态物和普通怪一样（2026-10-30 从 50% 改为 100%）。金币不受等级差影响，低级怪本来就给得少。
- 用途：商店装备、强化。卖装备回收买价的 25%。
- 定价：商店第 i 档装备约等于 50 只同级普通怪的金币，目标每 10–15 分钟能换一件。

### 8.2 装备（2026-10-30 讨论后修订，待确认数值）

**已确认**：每个英雄 3 格（武器、护甲、饰品），主角也一样；固定物品为主，Boss 另掉少量稀有物品；来源是泉水商店、精英和 Boss 掉落、地图宝箱、任务奖励；图标复用原作，缺的让 Codex 按原作风格出图。

**物品**：每件有名字、格子、档位（= 所在岛）和固定属性，不随机。切片做第 1、2 档，每档每格 2 件普通物品，外加 3 件稀有物品。

| 档 | 格子 | 名字 | 属性 | 商店价 |
|---|---|---|---|---|
| 1 | 武器 | 铁斧 | 攻击 +3 | 300 |
| 1 | 武器 | 锋利短剑 | 攻击 +2，暴击率 +5% | 300 |
| 1 | 护甲 | 皮甲 | 生命 +30 | 300 |
| 1 | 护甲 | 铁盔 | 生命 +15，防御 +1 | 300 |
| 1 | 饰品 | 回复戒指 | 生命回复 +1/秒 | 300 |
| 1 | 饰品 | 迅捷护符 | 攻速 +8% | 300 |
| 2 | 武器 | 战斧 | 攻击 +5 | 500 |
| 2 | 武器 | 锯齿剑 | 攻击 +4，暴击率 +8% | 500 |
| 2 | 护甲 | 锁子甲 | 生命 +55 | 500 |
| 2 | 护甲 | 钢盔 | 生命 +30，防御 +2 | 500 |
| 2 | 饰品 | 生命护符 | 生命回复 +2/秒 | 500 |
| 2 | 饰品 | 狂战护符 | 攻速 +12% | 500 |
| 稀有 | 武器 | 首领之刃 | 攻击 +8，暴击伤害 +30% | 不卖 |
| 稀有 | 护甲 | 守护者胸甲 | 生命 +60，格挡率 +15% | 不卖 |
| 稀有 | 饰品 | 吸血獠牙 | 吸血 8% | 不卖 |

- 数值依据：第 1 档按 3 级英雄（攻击约 12、生命约 140）算，第 2 档按 8 级（攻击约 17、生命约 205）算，一套同档装备约 +25–30% 战力。
- 价格约等于 50 只同档等级普通怪的金币（第 8.1 节），卖出回收 25%。
- 暴击、攻速、吸血、格挡这些属性要先确认队友单位真的会读；不生效的换成攻击或生命。

**来源**

| 来源 | 内容 |
|---|---|
| 泉水商店 | 卖已开放的岛对应档位的普通物品；同时可以卖出背包里的装备 |
| 精英 | 50% 掉 1 件本岛档位的普通物品，其中 10% 换成稀有 |
| 岛 Boss | 必掉 1 件本岛档位的普通物品，另有 30% 掉 1 件稀有 |
| 地图宝箱 | 本岛档位的 1 件普通物品加金币（原来在 RPG 下被关掉的物品奖励改走这里） |
| 任务奖励 | 原来给物品的任务步骤，改给该步骤所在岛档位的 1 件普通物品 |

**背包和穿戴**

- 装备放在 RPG 独立的装备背包里，不进原作的 `PlayerModel.Items`（那里现在是英雄名单）。背包不限格数。
- 存档：`RpgUd` 末尾追加装备背包（物品 Key 列表）和每个英雄的 3 个已穿戴 Key。
- 属性用修饰器加到英雄身上，来源 `RpgHero`，每格一个名字（`rpg.equip.weapon` 等），先删后加。英雄下阵时装备留在身上，再上阵自动生效。
- 界面：英雄卡下方显示 3 个装备格，点格子弹出背包里同类装备，可穿上或卸下。泉水旁靠近时出现"商店"按钮，商店分"购买""出售"两页。获得装备时飘字"获得 铁斧"。
- 强化（+1 到 +5）以后再做。

**图标**：护甲用原作头盔图标，饰品用原作护符图标；武器优先用没被当成英雄的原作武器图标，避免和英雄名单里的武器图标混淆。缺的由 Codex 按原作风格生成。

## 9. 刷怪点

### 9.1 规则

| 原规则 | RPG 模式 |
|---|---|
| 每日次数：709 个刷怪点 UTC 零点重置 | 关闭，打完按冷却无限重生 |
| 普通怪重生 4–30 分钟 | 45s |
| 精英重生 1–3 小时 | 3 分钟 |
| 岛 Boss | 10 分钟 |
| 树等静态物 | 2 分钟 |
| 冷却减到 0 时一次用光全部次数 | 关闭 |
| 物品刷怪点（掉武器副本） | 当普通刷怪点，掉落走新掉落表 |
| VIP 刷怪点 | 去掉 VIP 门槛，当普通刷怪点 |
| 活动刷怪点 | 关闭 |
| 岛屿精通奖章 | 关闭 |
| 一次性刷怪点（障碍等） | 保持一次性 |
| Boss 开门 | 保留 |

### 9.2 敌人等级

每个刷怪点已经有 `RelativeOptimumTime`（下称 rel），表示它在本岛进度窗口里的位置，直接拿来定等级：

```
level = round(L_start(岛) + rel × (L_end(岛) − L_start(岛)))，限制在 1–60
```

- 岛窗口：第 1 岛 1–5 级，第 2 岛 5–10 级；之后每岛约 +3.3 级，第 17 岛结束 60 级。
- rel 超出 0–1 的刷怪点保留原作的意图：VIP 点偏低，超级 Boss 偏高。例如第 1 岛精英的 rel 是 1.33，得 6 级，比岛内其它怪高一级。

### 9.3 敌人数值

沿用原生恢复的烘焙方法，见[公式清单](../Balance/formulas/simulation.md)的 `simulation.bake_spawner_health` 和 `simulation.bake_spawner_damage`：

- 血量 = 队伍有效 DPS × 期望战斗时长 / 数量 × (1 − 抗性)。
- 攻击按"期望战斗时长内打掉受击单位 Difficulty 倍的生命，再补上它的回血和吸血"反解。

只换输入：

- 原来是"原作机器人第 X 天的属性"，现在是"L 级的标准队伍"：那个等级预计拥有的英雄、商店同档普通装备加 1 件魔法装备、预计首杀加成。
- 受击单位有坦克时取坦克，否则取主角。
- `AverageFightTime`、`Difficulty`、`Count` 和护甲配置沿用场景里的值。

落地方式：

- 结果写进单独的数值表（刷怪点 Key → 等级、血量、攻击、经验、金币），场景配置不动。
- RPG 模式在 `EnemyUnit.Initialize` 之后用表里的数覆盖。4 条刷怪路径都经过它。
- 生成脚本用 Python，放在 `Balance/` 下，读 `balance.sqlite`，和现有工具链一致。

### 9.4 首杀加成（吞噬降级后剩下的部分）

- 每个精英和 Boss 刷怪点，整个存档只有第一次击杀给一次永久加成，作用于全队。难度档不重复给。
- 加成的属性沿用该刷怪点原来的奖励类型（`StatRewards`），保留"这只怪给生命"的辨识度，血条上也沿用原来的图标。
- 数值：精英 +0.5%，Boss +1%。全图精英和 Boss 共约 50 个，合计约 +40%。

### 9.5 第 1–2 岛的现状

- 第 1 岛 35 个刷怪点里，17 个是树，6 个是活动刷怪点。
- 第 2 岛 28 个刷怪点里，18 个是树。
- 两岛各有 1 个精英和 1 个门禁 Boss。

能打的怪偏少。试玩时觉得稀，就在数值表里加数量或缩短冷却。

## 10. 任务与悬赏

- 主线任务链、剧情、过场全部保留。
- 奖励按类型换算，不用逐条改任务：
  - 属性奖励 → 经验；
  - 物品 → 同档装备；
  - 钻石、灵魂 → 金币；
  - 炸药、头盔等任务道具照发（`CurrencyKeys.IsQuestCurrency`）。
- 新增悬赏板，放在泉水旁：
  - 同时显示 3 条，例如"击杀第 2 岛的骷髅 ×20""击杀第 1 岛精英"，奖励经验和金币。
  - 完成一条立刻补一条，从已开放的岛里抽，不按真实时间刷新。
  - 切片做 5 条固定轮换。

## 11. 死亡与复活

- 队友阵亡：15s 后以 20% 血复活，或主角到泉水时复活。沿用 `PetConfig.ReviveCooldown` 和 `ReviveHealthFraction`；泉水复活在宠物模式已实现。
- 主角阵亡：沿用原流程，回泉水，敌人回满血，全体队友一起复活。
- 切片不做经验和金币惩罚。

## 12. 难度档（切片之后）

- 击败第 17 岛 Boss 解锁"噩梦"，之后每通关一档解锁下一档。
- 每档：敌人等级 +10，英雄等级上限 +10（经验继续有意义），掉落的物品等级跟着涨，稀有品质概率提高。
- 在泉水切换难度。地图进度（门、任务）不重置。
- Boss 塔的 6 个 Boss 以后可以改成高难度挑战。

## 13. 存档

- RPG 模式用独立存档文件，从序章开新档，不和原作存档混用。原档里的永久属性和物品会和 RPG 成长互相污染。分档参照快速测试的 `StudyTestProfileController`，但要在打包版本里也生效。
- 新增 `RpgUd`，挂在 `GameUserData` 下，内容：
  - 队伍等级、经验；
  - 已招募的英雄、上阵顺序；
  - 每个英雄的装备、背包；
  - 首杀记录（刷怪点 Key）；
  - 当前和已解锁的难度；
  - 悬赏进度。
- 金币存在 `CurrencyUd`。
- 英雄属性每次读档重算，不存修饰器。新增 `ModifierSource.RpgHero = 23`、`RpgItem = 24`、`RpgFirstKill = 25`，加入 `IsRuntimeOnlySource`（`CB/Player/Models/PlayerModel.cs:709`）。

## 14. 和数值模型的关系

- 原玩法还要继续测试，第 5 阶段照常进行，不受 RPG 模式影响。
- RPG 模式不用原作的成长部分（每日次数、属性奖励、护符灵魂、物品产出），只借用战斗结算层（伤害、护甲、暴击、闪避）和第 9.3 节的烘焙方法。
- RPG 数值新增两条输入曲线：英雄成长（第 4.3 节）和岛 → 等级（第 9.2 节）。

## 15. 工程方案

### 15.1 新代码

放在 `CB/Rpg/`：

| 类型 | 作用 |
|---|---|
| `RpgModeConfig` | 开关和全部参数（ScriptableObject，做法同 `PetConfig`） |
| `HeroDefinition`、`HeroConfig` | 英雄名单：定位、稀有度、比例、技能、模型、招募条件 |
| `RpgModel`、`RpgUd` | 运行态与存档 |
| `TeamLevelService` | 经验、等级差修正、升级 |
| `HeroStatsSystem` | 等级、装备、首杀 → 运行时修饰器，作用到主角和队友 |
| `RpgRewardService` | 击杀奖励、首杀、掉落、任务奖励换算 |
| `ThreatSystem` | 仇恨表、嘲讽 |
| `HeroAiSystem` | 定位站位、远程保持距离、躲预警 |
| `UltimateService`、`UltimateHudView` | 手动大招和按钮 |
| `RpgItemModel`、词条、商店和背包界面 | 装备系统；界面在代码里搭，做法同 `PetCollectionView` |
| `BountyService`、`BountyView` | 悬赏 |
| `RpgSpawnerTable` | 生成的刷怪点数值表 |

### 15.2 接入点

| 接入点 | 位置 | 改动 |
|---|---|---|
| 敌人选目标 | `CB/Enemy/EnemyAttackSystem.cs:519` | 查仇恨表 |
| 敌人数值 | `CB/Enemy/EnemyUnit.cs` 的 `Initialize` | 用数值表覆盖等级、血量、攻击 |
| 重生与次数 | `CB/Enemy/EnemySpawner.cs` 的冷却、有效次数、大冷却 | 关闭次数，换冷却表 |
| 击杀奖励 | `EnemySpawner.HandleDeath`（:1177）、`HandleDevour`（:1363） | 跳过属性、灵魂、物品、活动奖励，改发经验、金币、首杀、掉落；任务道具照发 |
| 英雄技能 | `CB/Skills/SkillService.cs` 的 `Install` | 给主角和队友装自动技能。这个方法是通用的，现在只给敌人用 |
| 手动大招 | `CB/Skills/Systems/SkillAutoCastSystem.cs` 的 `Pick`、`StartCast` | 抽成公共入口 |
| 躲预警 | `CB/Skills/Components/SkillCastingComponent.cs` | 加查询，列出敌人正在施放的预警区域 |
| 队友选目标 | `CB/Pet/Systems/PetAttackSystem.cs` 的 `SelectTarget` | 按定位改优先级 |
| 队友移动 | `CB/Pet/Systems/PetMovementSystem.cs` | 站位、保持距离、躲预警 |
| 远程普攻 | `SkillService.LaunchRangedAttack` | 施法者类型从 `EnemyUnit` 放宽，开放给英雄 |
| 修饰器存档 | `CB/Player/Models/PlayerModel.cs:709` | 加 RPG 来源 |
| 存档 | `CB/Core/UserData/GameUserData.cs` | 加 `RpgUd` |
| 任务奖励 | `QuestChainDef.ChainReward` 的发放处 | 按类型换算 |
| 血条 | `CB/UI/World/HealthBar.cs` | 显示等级和首杀图标 |
| 测试工具 | `Assets/Editor/RecoveryTools/StudyQuickTestWindow.cs` | 加经验和等级、金币、装备、招募、难度 |

### 15.3 阶段

每个阶段做完都能单独进 Play 验证。

| 阶段 | 内容 |
|---|---|
| A 基础 | 开关、配置、独立存档、`RpgUd`、金币、修饰器来源 |
| B 英雄 | 英雄定义、属性层、队友单位改造、招募；先做第 18 节第 1 条的技术验证 |
| C 战斗 | 仇恨、嘲讽、手动大招与按钮、定位 AI、远程普攻、躲预警、拉拽和复活效果 |
| D 成长 | 经验与升级、首杀、装备与掉落、商店、背包 |
| E 刷怪与数值 | 刷怪规则、敌人等级、Python 生成数值表、血条 |
| F 内容 | 第 1–2 岛任务奖励换算、悬赏、宝箱奖励换算 |
| G 切片试玩 | 按第 16 节验收，调参 |

## 16. 验证切片：第 1–2 岛

内容：主角、石魔像、秘教徒；第 1–2 岛的全部刷怪点、任务、宝箱；1 个商店，5 条悬赏。

| 指标 | 目标 |
|---|---|
| 从新档打完第 2 岛 Boss | 60–120 分钟 |
| 打第 2 岛 Boss 时的等级 | 约 10 |
| 前 10 级平均升级间隔 | 5–10 分钟 |
| 等重生的时间占比 | < 10% |
| 金币 | 每 10–15 分钟能换一件装备 |
| 精英战里坦克持有仇恨 | 坦克存活时 ≥ 80% 的时间 |
| 队友躲预警 | 走得出去时 ≥ 70% |
| 主观感受 | 连续玩 30–60 分钟，不觉得在等，也不觉得在看 |

另外检查：

- 读档后等级、经验、金币、装备、队伍、首杀记录都在；
- 原作存档字节不变，参照快速测试的校验做法；
- 关闭开关后行为和现在一致；
- Console 没有错误。

对照测试：用原玩法从新档同样打到第 2 岛 Boss，回答同一组问题——会不会觉得在等、在看、在磨，玩完想不想继续。两种玩法的用时和结论记在一起比较。

试玩结论决定下一步：

- 指标达标、手感好，对照下也比原玩法好：铺到第 3–17 岛，加骷髅射手和法师。
- 觉得"在看"：先把主角的两个自动技能改成手动，再试。
- 觉得"在等"或"在磨"：先调刷怪数量、冷却和经验曲线，再铺开。

## 17. 补充决定（2026-10-29 已确认）

以下是设计时提出的默认做法，已确认采用。备选一栏只作记录。

| # | 决定 | 没采用的备选 |
|---|---|---|
| 1 | 主角按史诗档（3 个技能）做；4 个队友是 2 普通 + 2 稀有。操控方式已由第 19 节改为可切换 | 主角之外再做 5 个队友，共 6 套技能 |
| 2 | 技能数 1、2、3 包含大招，普通英雄只有大招 | 不含大招，即 1+1、2+1、3+1 |
| 3 | 队伍共享一个等级 | 每个英雄单独等级，新英雄靠追赶机制 |
| 4 | 切片用主角、坦克、治疗；射手和法师放到第 3 岛以后 | 切片就做齐 5 个 |
| 5 | 树等静态物的经验和金币都和普通怪一样（试玩后改，原为只给金币） | 不给经验 |
| 6 | RPG 模式用独立存档，从序章开新档 | 原档直接切换，按岛屿进度推算初始等级 |
| 7 | 怒气先关闭 | 改成大招能量 |
| 8 | 主线等级上限 60，难度档每档 +10 | — |
| 9 | 商业化系统全部关闭 | 以后单独设计 |

## 18. 风险

1. **队友借用宠物单位**：宠物单位绑定一件武器物品和一个武器槽（`PetModel.Item`、`PetModel.Slot`），攻击间隔、动画和伤害都从物品读，打敌人的伤害计算还要求有 `Weapon`。第一版让每个英雄挂一个对应宠物种类的物品配置，只借攻击间隔和动画；物品自带的伤害和技能在 RPG 模式下不生效。阶段 B 先做技术验证，借用太别扭就给宠物单位加一个不依赖物品的数据来源。
2. **AI 躲不好**：预警技能会对队友太狠。备选：队友受预警技能的伤害减半。
3. **主角动作**：主角的动画控制器是原作的，横扫、钩锁、吞噬需要动作。先用现有攻击和吞噬钩子的表现凑，没有合适动作的先只出特效。
4. **任务链依赖已关闭的系统**：有些步骤会解锁护符、矿区、水晶。切片前把第 1–2 岛的任务步骤逐条过一遍，决定跳过还是只走剧情。
5. **重建代码**：`EnemyAttackSystem`、`EnemySpawner.HandleDeath/HandleDevour` 和任务流程是重建的，没有原生依据。改之前先读懂现状。
6. **怪物偏少**：见第 9.5 节。
7. **素材版权**：不变。只在打算发布时才是问题。

## 19. 统一英雄单位与切换操控（2026-10-29）

> 路径相对于 `CB/` = `Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/`。行号已在 `feature/rpg-mode` 上逐条核对。
> 本节改写第 1 节决定 2“玩家只操控一个英雄”：改为玩家可以随时切换操控任意存活英雄，其余英雄由 AI 控制。

### 1. 总体做法

以方案一为骨架，吸收方案三的几处细节（输入角度、切换后的瞬移宽限、AI 主角只在目标进入攻击范围时才写 CurrentTarget），以及方案二的“相机覆盖期间禁止切换”。

1. **RPG 叠在宠物模式上。** `RpgModeConfig.Enabled = asset.enabled && PetConfig.Enabled`。队友直接复用已经验证过的宠物单位链路：移动、攻击、受伤、阵亡、15 秒复活、泉水、血条、回血、敌人仇恨、技能自动施放。理由：
   - `PetModel.Slot` 只在 `Pet/Systems/PetSpawnSystem.cs:59` 和 `Pet/PetsModel.cs:98-104` 被读，所以“不占武器槽的宠物”可以成立。
   - 宠物模式里敌人选目标 (`Enemy/EnemyAttackSystem.cs:593-612`)、`IsTargetedByAllies` (:615-625)、`Attack` (:449)、`RevivePlayerCommandHandler.cs:36` 的 `ReviveAll`、泉水 (`Buildings/FirecampSystem.cs:62-78`)、`SkillTargetQuery` 的候选，都会自动带上队友。
   - 如果像方案二、三那样关掉宠物模式，这些分支都要在 RPG 下重写一遍。
2. **两种身体，一种英雄。** 新增抽象类 `HeroUnit : UnitCore`。`PlayerUnit`（Pudge 预制体）和 `PetUnit`（代码搭建、套敌人模型）都改为继承它。`HeroUnit` 只提升公共部分：
   - `EnemyUnit CurrentTarget`：现在两边各有一份，见 `Player/PlayerUnit.cs:357`、`Pet/PetUnit.cs:52`；
   - `bool IsControlled`；
   - `abstract SetWalking(bool)`。

   `HeroUnit` 不声明 Awake/Start（`UnitCore` 的是 private，见 `Unit/UnitCore.cs:63,70`），也不加序列化字段。原作里只存在 `PlayerUnit` 和 `PetUnit` 两个子类，现有的 `is PlayerUnit` 和 `is PetUnit` 判断一律不动。
3. **“英雄”是一条记录，不是一个类型。** 每个英雄（包括主角）对应一个 `HeroModel`，放在 `HeroRosterModel` 里。规则层面没有主次之分：同一套属性来源、同一套目标规则、同一套跟随 AI，任何英雄都能被操控。只有身体层不同：主角有吞噬、钩子、原作死亡动画，队友有敌人模型的动画外观。这和 WC3 里各英雄模型不同、规则相同是一回事。
4. **`PlayerModel.Unit` 永远指向主角身体，从不改指。** 它是属性存档（`Player/Models/PlayerModel.cs:683-706`）、位置存档（:714-721）、Boss 战实体锚点（`BossTower/Systems/BossRaidSystem.cs:257/322/483`），也是几个“单位换了”检测器的对象（`Amulet/Upgrades/UpgradeStatApplySystem.cs:32` 会回满血，`Abilities/Systems/AbilityExecuteEventSystem.cs:331`，`Skills/Systems/SkillAutoCastSystem.cs:67` 会重装技能）。改指会把这些全部触发。`PlayerTag` 同样只留在主角身上。

### 2. “被操控英雄”在代码里的含义

- `HeroRosterModel`：`List<HeroModel> Heroes`、`HeroModel Controlled`、`event ControlledChanged(old, new)`、`bool TrySwitch(HeroModel)`、`void ForceControl(HeroModel)`。
- `HeroModel`：`Key`、`HeroDefinition`、`HeroUnit Unit`、`PetModel Pet`（主角为 null）、`SlotOrder`、`IsAlive`。主角还带 AI 用的 `PathFailTimer` 和 `AiTarget`。
- 静态辅助类 `RpgControl`，RPG 关闭时严格等于原表达式：
  - `UnitCore ControlledUnit(PlayerModel pm)`：`Enabled && roster.Controlled?.Unit != null ? roster.Controlled.Unit : pm.Unit`。
  - `bool IsControlled(UnitCore u)`：`Enabled ? (u is HeroUnit h && h.IsControlled) : u is PlayerUnit`。
- 不新增 ECS 标签。第一步没有任何过滤器需要它，以后做仇恨表时再加 `HeroTag`。

### 3. 各类玩家引用怎么处理（普查共 181 处）

| 类别 | 数量 | 处理 |
|---|---|---|
| controlled_hero | 29 | **第一步改指向被操控英雄的 13 处**：相机 `Camera/CameraFollowSystem.cs:272,313`；移动 `Unit/UnitMovement.cs:42`，Pudge 不被操控时直接返回；`Player/PlayerUnit.cs:398` 的走路动画；`Player/PlayerAttackSystem.cs:104` 的摇杆系数；`Skills/Systems/SkillAutoCastSystem.cs:243,404` 的转身判断；`Quests/Systems/QuestNpcProximitySystem.cs:131`；`SecretChest/Systems/SecretChestProximitySystem.cs:36`；`UI/Map/MiniMapView.cs:160`；泉水复活触发 `Buildings/FirecampSystem.cs:66`；`Pet/Systems/PetReviveSystem.cs:31` 的复活位置；`Unit/Systems/UnitDamageSystem.cs:333` 的打击音效。**其余 16 处暂时留在主角身上**：各流程的镜头锚点和宝石生成位置、刷怪特效判断、行走距离统计、`Gates/GateLocationTrigger.cs:22`、`Map/Systems/StaticMiniMapPoiSystem.cs:221`、位置存档、`Player/Systems/UnstackSystem.cs:34`、背包属性面板。AI 主角始终被拴在 12 m 内、18 m 外瞬移，所以这些位置最多偏差十来米。 |
| all_heroes | 68 | 大部分对 PetUnit 已经成立：受伤分支、友方 buff 和眩晕、目标候选、敌人仇恨、泉水回血、复活。约 20 处是物品技能上下文 `AbilityContext.PlayerUnit`（`Abilities/Core/AbilityContext.cs:21`），会继续落在主角身上。队友的攻击物品清空技能，所以这些只会由主角的物品触发，留到装备阶段再统一。水晶、护符、Boss 塔这几处不动。 |
| pet_only | 32 | 保留，作为队友的实现。RPG 下要关掉或绕开 6 处：`PetSpawnSystem` 按武器槽生成（:58-73）、`PetStatsSyncSystem`（:49）、`PetCollectionBonusSystem`（:42）、`Services/Damage/DamageCalculator.cs:91` 的防御比例、`:138` 的 GetOwnAttack、`Skills/SkillService.cs:315-320` 的宠物攻击加成。`PetMovementSystem`、`PetAttackSystem`、`PetReviveSystem` 改为以被操控英雄为锚。 |
| any_hero_lookup | 15 | 敌人选目标在宠物模式下已经覆盖队友。以主角为准的距离判断不变：30 m 回满血（`Enemy/EnemyAttackSystem.cs:127-131`）、逃跑（:259,:291,:412,:420）、刷怪点 Far 标记。前提是主角被拴在附近，这一点列为风险。 |
| player_only_feature | 9 | 全部留在主角身体上：吞噬、原作死亡和泉水流程、武器视图、爪子视图、免死、怒气特效（怒气按第 17 节第 7 条以后关闭）。 |
| unclear | 28 | `PlayerModel.Unit` 仍是主角，存档、奖励、Boss 塔、界面第一步都不改。奖励的永久属性只会写进 RPG 独立存档，第二步再换算成经验。 |

### 4. 切换流程

- **入口**：电脑 `Tab` 依次切到下一个存活英雄，`1`–`3` 直接选；手机点屏幕右侧的英雄头像（显示血条，阵亡时变灰）。
- **以下情况拒绝切换**：
  - `!playerModel.CanMove`（对话、过场、开门、宝箱队列）；
  - 相机正处于覆盖状态，即 `followingTransform != null || isOverridingTarget`（`CameraFollowSystem.cs:85,91`），需要新增一个只读 getter `IsOverriding`；
  - 主角 `IsDying` 或 `IsReviving`；
  - 目标英雄已阵亡、还没生成，或已经是当前操控对象；
  - 距上次切换不到 0.3 秒。
- **生效**：每帧在 `HeroControlSystem` 里统一执行一次。
  1. 旧英雄 `IsControlled = false`，下一帧由 AI 接管。主角第一次交给 AI 时，把 agent 的 `acceleration` 设为 20、`angularSpeed` 设为 720。预制体上是 8 和 120（`Pudge.prefab:3385,3387`），`agent.Move` 从来不用这两个值，但 `SetDestination` 会用。
  2. 新英雄 `IsControlled = true`，执行 `ResetPath()`、`velocity = Vector3.zero`、`SetWalking(false)`。
  3. 开始 1 秒宽限：这段时间 AI 英雄不因“离主人超过 18 m”而瞬移。
  4. 触发 `ControlledChanged`。
- **相机**：不调用 `Unfollow()`。默认跟随分支改读 `RpgControl.ControlledUnit`，用惯性 Slerp（`CameraFollowSystem.cs:291`）平滑移过去。
- **输入**：
  - 操控主角时仍由原生的 `UnitMovement` 驱动，手感不变。
  - 操控队友时由新的 `HeroControlSystem` 驱动。它是 `UnitMovement.Update/UpdateVisualTarget`（`Unit/UnitMovement.cs:42-95`）的移植，规则相同：`agent.Move`、460°/s 转身、4 m/6 m 滞回的朝向目标、眩晕/位移/引导时不动。不同的地方有三处：
    - 速度取队友自己的 `MovementSpeed` 乘以 `SkillService.GetMoveSpeedFactor`，因为宠物身上没有真实的速度修饰器（`Skills/SkillService.cs:540`）；
    - 输入角度改用相机前方向量的水平分量，避免切换后相机还在平移时方向偏斜；
    - 走路动画看输入大小。
- **AI 接管**：
  - 队友由 `PetMovementSystem` 和 `PetAttackSystem` 驱动，锚点改为被操控英雄，并跳过被操控的那个单位。被操控的队友只打攻击范围内的敌人，不自动追击，和原作主角一样。
  - 主角不被操控时，由新的 `ProtagonistAiSystem` 驱动。它复用宠物的跟随环位、12 m 拴绳、18 m 瞬移、追击和选目标逻辑。目标存在 `HeroModel.AiTarget` 里，进入攻击范围后才写入 `Pudge.CurrentTarget`，这样 `PlayerAttackSystem` 的 0.2 秒保留逻辑（:108-128）不会把目标扔掉。普攻仍由原生的 `PlayerAttackSystem` 打出。走路动画改为看速度（`PlayerUnit.cs:398` 加一个 RPG 分支）。
- **读档**：总是从操控主角开始，第一步不记住上次操控的是谁。

### 5. 被操控英雄阵亡

- **被操控的队友阵亡**：走宠物流程 `PetsModel.OnPetDied`（`Pet/PetsModel.cs:143-147`），15 秒后以 20% 血复活，或在泉水复活。`HeroControlSystem` 发现当前操控对象 `!IsAlive` 后，自动切到主角；主角不可用时切到第一个存活英雄。
- **主角阵亡（第一步）**：沿用设计稿第 11 节和原作流程。`ProcessPlayerDeath` 先把 `CanMove` 设为 false，再走泉水流程，最后由 `RevivePlayerCommand` 复活全队。`HeroControlSystem` 在主角 `IsDying` 的第一帧 `ForceControl(主角)`，让相机跟着死亡和跳出动画，复活后继续操控主角。
- 为什么第一步不做“主角也单独阵亡”：`PlayDeathAnimation` 会写全局 `CanMove = false`（`PlayerUnit.cs:428`），`HandleWalkEnabled` 会解引用可能为空的 `currentPlayerModel`（:514），而敌人、宠物和技能有十几处读主角的 `IsDying`（`EnemyAttackSystem.cs:132,356`；`SkillAutoCastSystem.cs:148-161`；`PetAttackSystem.cs:64`）。要做这件事，得一次改完再统一验证，所以放到第三步，并作为待确认问题。

### 6. 属性来源

- **所有英雄都从同一个基底出发**：`PlayerConfig.Stats` 的基础值，加上宠物模式的 `PlayerBaseAttack`（`PlayerModel.cs:546-552`），再乘 `HeroDefinition` 里的定位比例。
- **队友**：生成时建一份稳定的 `Stat` 列表，覆盖 `PlayerConfig.Stats` 的全部类型，然后加 `ModifierSource.RpgHero = 23` 的修饰器（Health、Damage 用 PercentMultiplicative，值是比例；防御用 Flat；攻击距离可选覆盖）。RPG 下 `PetStatsSyncSystem` 和 `PetCollectionBonusSystem` 不运行，所以队友属性不再继承主角。
- **主角**：保留现有的 `Stat` 对象，不替换列表，这样 `SkillService` 等处持有的引用不会失效。第一步主角的比例是 1，不加修饰器。
- **伤害**：普攻只按 `Damage` 属性计算。RPG 下英雄来源的 `weaponDamage = 0`（`DamageCalculator.cs:135-138`），队友不乘物品防御比例（:91），`SkillService.DealDamage` 不加物品伤害（:315-323）。
- **存档**：`RpgHero/RpgItem/RpgFirstKill = 23/24/25` 加入 `IsRuntimeOnlySource`（`PlayerModel.cs:709-712`），任何一个都不会写进存档。
- **攻击物品**：队友攻击物品是 `new ItemModel(lifetime, itemsProvider.Get(def.AttackItemKey))`，用法和 `PlayerModel.cs:733-737` 构建爪子一样。它只提供攻击间隔、出手点和伤害类型。建好后清空 `AbilityStates`，因为物品自带的触发会按 `data.Item` 执行（`AbilityExecuteEventSystem.cs:251`）。它不在任何槽位里，`SlotIndex` 为 0，伤害数字视图（`UI/Damage/PlayerDamageEventsViewSystem.cs:44`）不会越界，爪子物品已经是这样用的。
- **等级成长**（`1.08^(L−1)`）、升级回满、经验和金币放在第二步。

### 7. 保证原作和宠物模式不变

- **RPG 关闭**（资产缺失、`enabled = 0`，或宠物模式关闭）时：
  - 所有新系统第一行就返回；
  - `RpgControl` 返回原表达式；
  - 对原有文件的改动只能是以下三种之一：
    1. 用 `if (RpgModeConfig.Enabled)` 包住的插入；
    2. `HeroUnit` 基类替换，以及把 `CurrentTarget` 提升到基类；
    3. 换成 `RpgControl` 的等价写法。

  原有代码行不做别的改写。
- **宠物模式额外改动两处**：
  - `PetSpawnSystem.Spawn` 的单位搭建代码原样抽到 `PetUnitFactory`；
  - `PetMovementSystem.lastOwner` 的类型放宽为 `UnitCore`。

  这两处都不改变行为，验收要跑一遍宠物模式。
- **独立存档**：RPG 下存档目录追加 `Rpg` 子目录（`Core/UserData/GameDataService.cs:156-161`，放在编辑器 StudyTest 覆盖之后），并跳过旧 PlayerPrefs 迁移（:169-174）。第一步不加 `RpgUd`，因此原作存档的结构完全不变。

### 8. 文件改动清单（第一步）

| 文件 | 改动 |
|---|---|
| `Rpg/RpgModeConfig.cs` + `.meta`（新），`Assets/Resources/Rpg Mode Config.asset`（新） | 开关和参数，写法同 `PetConfig`（:120-141）。`Enabled` 依赖 `PetConfig.Enabled`。包含 `Heroes`、`StartingHeroKeys`、切换冷却 0.3 秒、宽限 1 秒。 |
| `Rpg/HeroDefinition.cs`（新） | 字段：Key、IsProtagonist、PetItemKey（外观，通过 `PetConfig` 找到 `PetDefinition`）、AttackItemKey、HealthRatio、AttackRatio、DefenceFlat、AttackRangeOverride、DisplayName、Icon。 |
| `Rpg/HeroModel.cs`、`Rpg/HeroRosterModel.cs`、`Rpg/RpgControl.cs`（新） | 见第 2 节。 |
| `Unit/HeroUnit.cs`（新） | 抽象基类。 |
| `Player/PlayerUnit.cs` | 基类改为 `HeroUnit`，删掉 :357，实现 `SetWalking`。`Update` 加 RPG 分支：不被操控时按速度驱动走路动画。 |
| `Pet/PetUnit.cs` | 基类改为 `HeroUnit`，删掉 :52，`SetWalking`（:196）改为 override。 |
| `Pet/PetUnitFactory.cs`（新），`Pet/Systems/PetSpawnSystem.cs` | 原样抽出单位搭建代码。RPG 下 `OnUpdate` 在第一行返回。 |
| `Pet/Systems/PetStatsSyncSystem.cs`（:49），`Pet/Systems/PetCollectionBonusSystem.cs` | RPG 下提前返回。 |
| `Rpg/Systems/RpgHeroSpawnSystem.cs`（新） | 先把主角登记为 0 号英雄并设为操控对象，再生成 golem 和 cultist：建无槽位的 `PetModel`，放进 `PetsModel.Pets`，设 `PetTag`，安装宠物技能配置，建好稳定属性。 |
| `Rpg/HeroStats.cs`（新） | 构建 v0 属性（基底 × 比例）。 |
| `Unit/ModifierSource.cs`，`Player/Models/PlayerModel.cs:709` | 新增 23、24、25，并标为仅运行时。 |
| `Services/Damage/DamageCalculator.cs:91,138`，`Skills/SkillService.cs:315-323` | 加 RPG 门控。 |
| `Rpg/Systems/HeroControlSystem.cs`（新） | 处理切换输入、拒绝条件、生效步骤、队友的摇杆移动、自动切换、主角阵亡时强制切回。 |
| `Rpg/Systems/ProtagonistAiSystem.cs`（新），`Pet/HeroAi.cs`（新，静态辅助） | AI 主角。从宠物代码里抽出 `MoveTo`、`Chase`、`FlatSqrDistance`、`SelectFollowerTarget`、`FindAggressor`、`FindNearest`，参数改为 `UnitCore` 加领队。 |
| `Pet/Systems/PetMovementSystem.cs`，`Pet/Systems/PetAttackSystem.cs` | 锚点改为 `RpgControl.ControlledUnit`，跳过被操控单位，RPG 下用自己的速度，被操控时选目标只看攻击范围内。 |
| `Unit/UnitMovement.cs:42`，`Player/PlayerAttackSystem.cs:104`，`Skills/Systems/SkillAutoCastSystem.cs:243,404`，`Skills/SkillForcedMovement.cs:95,116`，`Unit/Systems/UnitDamageSystem.cs:333` | 加门控，或改用 `RpgControl` 的等价写法。 |
| `Camera/CameraFollowSystem.cs:272,313` | 改读被操控英雄，并新增 `IsOverriding`。 |
| `Quests/Systems/QuestNpcProximitySystem.cs:131`，`SecretChest/Systems/SecretChestProximitySystem.cs:36`，`UI/Map/MiniMapView.cs:160`，`Buildings/FirecampSystem.cs:66`，`Pet/Systems/PetReviveSystem.cs:31` | 改用被操控英雄的位置。 |
| `Rpg/UI/HeroSwitchHudView.cs`（新） | 头像按钮，代码搭建，写法同 `UI/Pet/PetUiBuilder.cs`。 |
| `Core/UserData/GameDataService.cs` | 独立存档目录，跳过旧数据迁移。 |
| `Core/Boot.cs:40`，`Core/SystemsInstaller.cs:74-81` | 注册 `HeroRosterModel` 和各个新系统。 |

### 9. 分步计划

1. **第一步 A：基础。** 开关、独立存档、`HeroUnit`、只含主角的名单、`RpgControl`、三个宠物系统在 RPG 下提前返回、修饰器来源。验证：RPG 开启时主角照常移动和战斗，不生成武器槽宠物，存档写进 `Rpg/`。
2. **第一步 B：队友上阵。** `PetUnitFactory`、golem 和 cultist、v0 属性、伤害门控。验证：三人小队一起战斗，队友能阵亡、复活，泉水和主角的泉水流程会把全队救回来。
3. **第一步 C：切换操控。** 第 4、5 节的全部内容，加上相机、各处位置判断、头像按钮。验证：可以随意切换，各英雄自动战斗。
4. **第二步：成长循环。** `RpgUd`（等级、经验）、`rpg.gold`、经验表和等级差、`HandleDeath/HandleDevour` 的奖励替换、取消每日次数、刷怪冷却表、`RpgHero` 等级修饰器作用到所有英雄。
5. **第三步：真正的英雄平等（需确认）。** 主角也单独阵亡，15 秒复活，只有全队阵亡才走泉水流程；`PartyUnavailable` 代替所有读主角 `IsDying` 的地方；门触发改为看被操控英雄；保存当前操控的英雄。
6. **之后**：大招按钮（可以和头像合并）、仇恨表、定位 AI、远程普攻、装备、招募。

### 10. 第一步范围与验收

**范围**：上面的 1A、1B、1C，分三次交给 Codex，每次做完我审查一次再合并。开局就上阵主角、石魔像、秘教徒，暂不按剧情招募。不做等级、经验、金币、仇恨、大招。

**验收**：

1. RPG 关、宠物模式关：原作照常，QuickTest 原存档字节比对通过。
2. RPG 关、宠物模式开：宠物从武器槽生成，跟随、攻击、阵亡、15 秒复活、泉水复活都正常，即 `Docs/PetSystem/Step1Design.md` 的试玩清单通过。
3. RPG 开：从序章开新档，存档在 `<存档目录>/Rpg/`。玩一局后原 `game_data.bin` 的字节不变。
4. 石魔像和秘教徒在主角身边生成，用敌人模型；会跟随、攻击、挨打，阵亡后 15 秒以 20% 血复活，泉水也能复活。
5. `Tab` 和 `1`–`3` 能切换操控，点头像也能。相机平滑移到新英雄，不跳。摇杆和 WASD 只移动被操控的英雄，它的走路动画正常。
6. 主角不被操控时会跟随、追击、普攻，走路动画跟着速度变化，吞噬钩子照常。
7. 以下情况切换被拒绝：对话、开门镜头、宝箱队列期间；主角阵亡或复活过程中；目标英雄已阵亡。
8. 被操控的队友阵亡后，操控自动回到主角。主角阵亡时强制切回主角，播放泉水流程，全队复活，之后 `CanMove` 为 true。
9. 操控队友时，走到 NPC 旁边能开始对话，宝箱提示会出现，进泉水能复活阵亡的英雄。
10. 在第 1 岛频繁切换、战斗 10 分钟，控制台没有空引用、类型转换失败或 MissingReference 错误。

### 11. 风险

- **主角阵亡会重置全队。** 正在操控队友时也一样，这和“不分主次”还有差距，留到第三步。
- **敌人那边的距离判断仍以主角为准。** 30 m 回满血、玩家不可用、逃跑这些判断，都依赖 AI 主角被拴在 12 m、18 m 之内。如果主角卡住或离开导航网格，远处的战斗会被回满血。
- **门触发只认主角的碰撞体**（`Gates/GateLocationTrigger.cs:22`）。地点和音乐会比被操控的队友晚一点切换。AI 主角追怪退回出口时，地点可能来回变。
- **基类替换涉及预制体脚本。** `PlayerUnit` 的基类换了，必须确认 Pudge 预制体的引用全部还在。`HeroUnit` 不能有序列化字段，也不能有 Awake/Start。
- **`AbilityStates` 是 private set**（`ItemModel.cs:489`）。只能清空列表，还要确认 `RebuildAbilityStates` 不会因为等级或进化被再次触发。
- **`Player/WeaponView.cs:455` 会读 `UnitMovement.VisualTarget`。** 主角不被操控时这个值会过期。宠物模式下武器视图是隐藏的，预计无影响，需要验证。
- **只分开了存档文件。** `DataVar` 和少数 PlayerPrefs 键仍与原作共用（`Core/DataVar.cs:42-51`、`Services/Time/TimeService.cs:14`）。
- **宠物模式的界面在 RPG 下仍会出现**（宠物图鉴、物品槽显示宠物）。初始武器会自动装备，给主角加 Item 修饰器（`PlayerModel.cs:739-743`）。第一步先接受，第二步屏蔽。
- **第一步没有敌人等级。** 所有英雄的属性只按基底乘比例，第 1 岛的强度需要试玩后调整。
- **切换后可能触发瞬移。** 离新领队超过 18 m 的 AI 英雄会被瞬移，1 秒宽限只能缓解切换那一刻。

### 12. 已确认（2026-10-29）

- 随时可以切换，战斗中也行；被操控的英雄阵亡时，控制权自动交给活着的英雄。
- 代码层面规则统一、共用 `HeroUnit` 基类，身体分两种（主角预制体、敌人模型搭的队友），不新写一套单位类。
- 主角阵亡和其他英雄一样单独倒下、15 秒复活、全队倒下才回泉水，这一条放到第三步。第一步主角一死仍全队回泉水。
- 原作主角是英雄之一。第一步主角一直在队伍里，不能下场；现在 5 个英雄正好全部上阵，所以没有影响。
- 第一步开局就让主角、石魔像、秘教徒上阵，不按主线招募，方便测试。
- 被操控的英雄普攻只打攻击范围内的敌人，不自己追，手感和原作主角一样。

## 20. 副本（2026-10-30）

> 本节是新增玩法，规则已和用户确认。实现说明见 `Step5DungeonBrief.md`。

### 20.1 已确认的规则

| # | 规则 |
|---|---|
| 1 | 部分岛上有副本传送门。进入后是隔离区域：做法同 Boss 塔，在 `Main.unity` 的隔离区瞬移进去，不另开 Unity 场景。 |
| 2 | 副本里的怪死了不刷新。离开副本、阵亡被踢出后，下次进入全部怪物重新生成。 |
| 3 | 副本里杀怪和野外一样立即给经验和金币（2026-10-30 改，原为通关时一次性发放）。击杀 Boss（通关）时另给通关奖励，并弹出本次汇总的结算窗口。 |
| 4 | 主角阵亡（RPG 模式下即全队阵亡）时踢出副本，副本重置。主动离开也重置，离开前要确认。已经拿到的经验和金币保留，只是没有通关奖励。 |
| 4a | 副本里阵亡的队友不会按时间复活（2026-10-30），离开副本时才复活。以后有复活技能再单独处理。 |
| 5 | 通关后副本进入冷却，从击杀 Boss 那一刻起滚动 24 小时。冷却中传送门显示倒计时，不能进入。 |
| 6 | 第一个副本放在第 1 岛，地形借用一个 Boss 塔竞技场（RPG 模式下 Boss 塔已关闭，竞技场闲置）。 |

### 20.2 默认做法（可在配置里调）

- 怪物等级：小怪取第 1 岛上限 5 级，Boss 6 级；队伍成员和 Boss 塔一样随主角进入，复活并回满血。
- 通关奖励 = 本次副本击杀所得经验和金币的 50%（`ClearBonusFraction`），击杀 Boss 时发放。装备系统做好后，Boss 再加必掉装备。
- 副本里的怪不掉招募英雄，不给首杀加成。
- 副本中途存档退出，读档后回到传送门外，进度作废。
- 冷却结束时间存在 `RpgUd` 里，按副本 Key 记录。
- 冷却中走进传送门会弹窗显示剩余时间。开发版（含编辑器，`Debug.isDebugBuild`）多一个“重置冷却(调试)”按钮，点了清空冷却并直接进入；正式包没有这个按钮。编辑器菜单 `Tools/RPG/Dungeon/Reset Dungeon Cooldowns` 也能重置。
- 第一个副本“幽暗地穴”：传送门在第 1 岛 (5, 0, -62)，副本用 Boss 塔的 atlantic 竞技场（入口广场 → 走廊 → Boss 大厅，整条连通）。6 组共 16 只第 1 岛小怪（5 级），Boss 用第 1 岛门禁 Boss 的配置（6 级）。第 1 岛精英原数值远强于 Boss（血 6327、攻 250，Boss 为 1796、44），所以没放进副本。
- 实测（队伍 6 级，改规则前）：一次通关共得 1722 经验、438 金币（含通关奖励），6 级直接升到 14 级，远超“第 2 岛结束约 10 级”的节奏。主要来自 Boss 的 ×20 经验和 50% 通关奖励，需要试玩后下调。

### 20.3 结算窗口

标题“副本通关”，列出：副本名、用时、击杀数、本次经验和金币（击杀所得 + 通关奖励）、进副本以来的升级情况（如“3 级 → 5 级”），以及“下次可挑战：24 小时后”。点“领取”关闭窗口，并把队伍传回传送门外。

## 20. 本分支只做 RPG，英雄统一重构（2026-10-30）

- 用户决定：`feature/rpg-mode` 删掉原作玩法和宠物模式，只保留 RPG。不再要求"原作文件只能加开关插入"。对照原玩法用 `main` / `feature/pet-system`。
- 主角和队友是同一种英雄，跑同一套系统（属性、技能、普攻、移动和 AI、受伤、回血、血条、阵亡复活）。主角只是身体（Pudge 模型）和英雄定义不同。
- 每个英雄单独阵亡，15 秒后复活；全队阵亡才回泉水。副本里沿用"不按时间复活"，失败条件改为全队阵亡。
- 去掉主角的原作独有机制：武器能力、怒气、打弱怪加速、各种能力加成。新增所有英雄都读的攻速属性。
- 分 H1–H5 五个阶段实施，详见 [HeroRefactorBrief.md](HeroRefactorBrief.md)。护符、VIP、水晶、商城、模拟器等大块原作系统放到之后单独清理。
