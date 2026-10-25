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
| 玩家与战斗 | [Player](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Player)、[Unit](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit)、[Enemy](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Enemy) | 摇杆、导航、武器、受伤、死亡与吞噬 |
| 成长与通行 | [Rewards](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Rewards)、[Gates](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Gates)、[Location](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Location) | 奖励如何改变属性、装备槽、门和岛屿状态 |
| 任务与界面 | [QuestModel](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests/QuestModel.cs)、[Quests](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Quests)、[UI](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI)、[Core/UI](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Core/UI) | NPC、剧情、领奖、任务道路与视图生命周期 |
| 存档 | [GameUserData](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Core/UserData/GameUserData.cs)、[GameDataService](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Core/UserData/GameDataService.cs) | 运行态模型如何写入 MessagePack 并在重启后恢复 |
| 扩展系统 | [Abilities](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Abilities)、[BossTower](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/BossTower)、[Crystals](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Crystals)、[Amulet](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Amulet) | 配置、系统、消费者与 UI 的分层 |

## 在编辑器里对照

从一次真实交互开始，例如摇杆接近敌人：观察自动攻击、目标死亡、吞噬、永久属性变化，再读对应系统和存档字段。不要只凭方法名推断行为；遇到空方法时，还需检查是否为未使用的导出状态机或尚未恢复的入口。

Main 场景较大，适合选中一个角色、道路或 UI 子树逐层查看。修改资源时保留 `.meta`，避免破坏场景、Prefab 和配置的 GUID 引用。

编辑器兼容层分别位于 [RecoveryCompatibility](../Recovery/StudyProject/Assets/RecoveryCompatibility) 和 [Editor/RecoveryCompatibility](../Recovery/StudyProject/Assets/Editor/RecoveryCompatibility)。它们处理恢复出的 Player 形式依赖在 Unity Editor 中缺少的生命周期行为。日常保持正常域和场景重载，停止 Play 后再修改、刷新、等待编译。

当前已验证路径、受控测试边界和剩余限制见[恢复状态](RecoveryStatus.md)。本地仍保留原生地址、机器码哈希及历史测试证据，这些体积较大的材料未纳入精简源码仓库。
