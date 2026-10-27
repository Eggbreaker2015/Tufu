# 学习导航

先按[仓库说明](../README.md)打开 `Recovery/StudyProject`，通过 Boot 进入 Main。以下链接均指向当前工程；原始导出和逆向工具不作为运行依赖。

## 先看配置，再追踪代码

选择 [Player Config.asset](<../Recovery/StudyProject/Assets/MonoBehaviour/Player Config.asset>)，对照 [PlayerConfig.cs](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/PlayerConfig.cs)，从字段追踪原预制体、武器和属性。通过 `.meta` 中的 GUID 可以继续定位资源引用。

再看 [Boss Tower Config.asset](<../Recovery/StudyProject/Assets/MonoBehaviour/Boss Tower Config.asset>) 中的 `references / RefIds`，理解 `[SerializeReference]` 的多态奖励和行为数据。AssetRipper 2.0.0 改善了这类字段的读取；字段成功导出不等于所有玩法都已验证。

## 源码入口

游戏代码集中在 [Assets/Scripts/Assembly-CSharp/CodeBase](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase)。建议按以下顺序阅读：

| 方向 | 入口 | 观察内容 |
|---|---|---|
| 启动与依赖 | [Boot](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Core/Boot.cs)、[MainSceneInitializer](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Core/MainSceneInitializer.cs)、[GameContext](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Core/GameContext.cs)、[SystemsInstaller](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Core/SystemsInstaller.cs) | 配置、模型、服务和 ECS 系统如何接起来 |
| 玩家与战斗 | [Player](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player)、[Unit](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit)、[Enemy](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy) | 摇杆/键盘输入、导航、武器、受伤、死亡与吞噬 |
| 成长与通行 | [Rewards](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards)、[Gates](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Gates)、[Location](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Location) | 奖励如何改变属性、装备槽、门和岛屿状态 |
| 任务与界面 | [QuestModel](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestModel.cs)、[Quests](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests)、[UI](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI)、[Core/UI](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Core/UI) | NPC、剧情、领奖、任务道路与视图生命周期 |
| 存档 | [GameUserData](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Core/UserData/GameUserData.cs)、[GameDataService](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Core/UserData/GameDataService.cs) | 运行态模型如何写入 MessagePack 并在重启后恢复 |
| 地图宝箱 | [SecretChest](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/SecretChest)、[SecretChestPromptView](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/SecretChest/SecretChestPromptView.cs) | NavMesh 路径距离、原开箱 UI、领取奖励和存档防重；[恢复依据与验证](ChestRecovery.md) |
| 扩展系统 | [Abilities](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities)、[BossTower](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower)、[Crystals](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Crystals)、[Amulet](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet) | 配置、系统、消费者与 UI 的分层 |

## 在编辑器里对照

先点击 Game 窗口，再用 WASD 或方向键移动；触屏摇杆保留。两者共用 [Joystick.GetInput](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player/Input/Joystick.cs) 入口，移动系统继续处理相机方向、NavMesh 和剧情锁定。键盘方向归一化，避免斜走加速；编辑输入框及失焦时不移动。

从一次真实交互开始，例如摇杆接近敌人：观察自动攻击、目标死亡、吞噬、永久属性变化，再读对应系统和存档字段。不要只凭方法名推断行为；遇到空方法时，还需检查是否为未使用的导出状态机或尚未恢复的入口。

Main 场景较大，适合选中一个角色、道路或 UI 子树逐层查看。修改资源时保留 `.meta`，避免破坏场景、Prefab 和配置的 GUID 引用。

编辑器兼容层分别位于 [RecoveryCompatibility](../Recovery/StudyProject/Assets/RecoveryCompatibility) 和 [Editor/RecoveryCompatibility](../Recovery/StudyProject/Assets/Editor/RecoveryCompatibility)。它们处理恢复出的 Player 形式依赖在 Unity Editor 中缺少的生命周期行为。日常保持正常域和场景重载，停止 Play 后再修改、刷新、等待编译。

## 增加测试钻石

运行游戏进入 Main 后，在 Unity 菜单打开 **Tools → 学习测试 → 钻石工具**。可点击 `+1,000`、`+10,000`，或输入正整数后点击“增加钻石并保存”。窗口显示当前余额及保存结果；停止 Play 或玩家尚未加载时按钮禁用。

这个工具是学习工程新增的 Editor 功能，源码为 [StudyTestGemsWindow.cs](../Recovery/StudyProject/Assets/Editor/RecoveryTools/StudyTestGemsWindow.cs)。它增加真实玩家钻石并立即保存，重启后保留，不模拟购买或增加 VIP 经验；Editor 目录下的工具不会包含在游戏构建中。

## 本地模拟广告奖励

游戏左侧的场记板图标是原广告属性奖励入口，显示这次可领取的属性和数值。点击后本地立即完成，走原属性奖励、次数记录和存档链路；不会播放广告或调用广告 SDK。该入口奖励生命、伤害、防御等永久属性，不是钻石购买入口。

学习工程默认进入游戏约 3 秒后即可测试首次奖励，后续展示时间、冷却和每日次数按原配置。若要保留原版累计 60 分钟解锁及首次 60 秒等待，在 Main 的 `AdsInitialize` 组件取消 `Quick Start For Study`。临时跳过等待不会伪造存档的解锁标记，原总游戏时长计时也已恢复。

当前已验证路径、受控测试边界和剩余限制见[恢复状态](RecoveryStatus.md)。本地仍保留原生地址、机器码哈希及历史测试证据，这些体积较大的材料未纳入精简源码仓库。

## 本地模拟支付与礼包

商店付费按钮显示“模拟支付”。点击后会短暂显示处理中，再默认支付成功，沿用商品处理、VIP 经验、奖励展示及存档逻辑；没有真实扣款或支付 SDK 请求。购买钻石会增加 VIP 经验，Editor 钻石工具只增加钻石，两者用途不同。

首充入口在未领取时显示。完成一次模拟付费后，回到礼包页领取原配置的装备和恢复属性；不会只因打开支付结果页就重复发奖。VIP 页显示真实等级、各级权益和每日奖励，领取后按 UTC 次日重置。订单按本地交易号去重，重启保留购买与领取状态。

相关入口为 [StudyPaymentsService](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Purchases/StudyPaymentsService.cs)、[PurchaseProcessingService](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Services/Purchases/PurchaseProcessingService.cs)、[FirstBuyPackView](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/Shop/FirstBuyPackView.cs) 和 [VIP](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/VIP)。邮件按学习范围没有恢复；灵魂通行证每日奖励和活动结束补领改为直接发放。

## 活动和头顶奖励预览

骷髅入口对应水晶活动，仍要求解锁第 4 岛，并遵守原启动时段、持续时间和奖励门槛。当前用户档未达到条件时，入口保持锁定。开始后可查看阶段、里程碑和倒计时；结束页可补领已达标但未领取的里程碑奖励。原活动配置与实际计划生成见 [Events](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Events)。

血条旁的属性图标现在显示单位实际配置的基础奖励数值；灵魂、活动货币和抗性按真实条件显示，相关代码为 [HealthBar](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/World/HealthBar.cs)。怒气条是状态显示，随可用等级和当前怒气更新；工具图标沿用原隐藏规则。
