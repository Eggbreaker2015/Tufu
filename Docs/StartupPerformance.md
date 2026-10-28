# 首次进入的性能恢复

更新：2026-10-27。验证环境为本机 Unity **6000.6.0f1 / Windows DX12 Editor**，从 Boot 正常进入 Main，保持 Domain 和 Scene Reload 开启。

## 原版采用的方案

依据本地 APK `com.multicastgames.butcher.pjb.apk` 的 ARM64 指令及导出的资源核对，APK SHA-256 为 `c842d8d2674bc8e90894d5c01cd2094bde8f87c523e2e0bf3ee9221e2fe70b78`。

| 原版路径 | 证据与行为 | 本项目处理 |
|---|---|---|
| 主场景异步预载 | `MainSceneInitializer`：异步加载到 0.9，先禁止激活，与 consent 并行，最后激活及淡出加载界面。原生状态机入口 `0x153a5e8`、`0x1539ef4`。 | 沿用原有恢复实现。 |
| 出生点分批初始化 | `EnemySpawnerBootstrapSystem.ProcessPendingSpawners`，`0x1511a34`：优先处理玩家周围 65 单位内的出生点，其余每帧最多 6 个；事件出生点另有队列。 | 保留队列、范围和远处每帧 6 个上限；Editor 另加约 5 ms 的软预算，附近队列未完成时继续优先处理。 |
| 距离休眠 | `EnemyVisibilitySystem.OnUpdate`，`0x1513038`：距离平方 3600/900 的两级标记，远处关闭导航代理和视觉根。 | 沿用原实现，仍生成全图单位，不减少出生点、敌人数或奖励。 |
| 共享对象池 | `ObjectPool<T>.Spawn`，原恢复地址 `0x1a4490c`：按类型和原 prefab 共用池，回收后复用。 | 原来源键与回收路径保持一致，只允许新实例使用预处理模板。 |
| 图形管线预热 | `ShaderPrewarmer` 的 `Awake` 在 `0x154a65c`，预热入口状态机在 `0x154a800`。**这份 Android APK 明确跳过 iOS 专用 `.graphicsstate` 预热**；另一个函数包含每批 PSO 预热及逐帧让出。 | 没有把平台不匹配的 PSO 资源当作 Windows 卡顿修复。 |

上述方案多数已经恢复。原版没有通过把全部模型和系统初始化改成异步来解决问题，`Boot.Start`（`0x1532b64`）仍是同步入口。本次没有运行 Android 原 APK 做帧率基准，原版依据属于静态指令和资源证据。

## 本次定位和实现

CPU 采样发现，昂贵部分集中在大量模块化角色的 `Instantiate/Instantiate.Awake`，而非导航寻路。受控创建中，`tree 1` 不到 1 ms，`all_buck_knight` 约 43 ms；该角色带有 287 个节点，其中大量是未启用的换装渲染器。单纯延后启用 NavMeshAgent 对后者没有实质改善。

新增 [EditorEnemySpawnTemplates](../Recovery/StudyProject/Assets/RecoveryCompatibility/EditorEnemySpawnTemplates.cs)，在加载界面内逐帧准备临时副本：

- 仅处理普通 `EnemyUnit` 且只有已知基础脚本的预制体；含专用组件的单位直接使用原资源。
- 只移除自身未启用、没有子节点的纯网格叶节点；保留脚本序列化引用、蒙皮骨骼、动画曲线目标、碰撞体和其它组件。
- 源 prefab 不写入修改；模板放在未激活的临时容器中，准备时不会注册额外 ECS 单位。
- 实际出生仍走原对象池，并按原 prefab 记录来源，兼容后续回收、复用和其它普通 Spawn 调用。

受控副本从 287 个节点降到 75 个，复制约 2.3 ms。实际加载的 61 个源模板合计移除 6,599 个不参与运行的网格叶节点。**这项结构精简属于恢复工程的 Editor 适配，没有证据表明原 APK 使用了同一精简算法。**

[MainSceneInitializer](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Core/MainSceneInitializer.cs) 还在主场景预载开始前异步预载原宝箱和任务提示 prefab，避免进入后的首次提示同步初始化 Addressables、本地资源包与 Shader。资源预载放在 `allowSceneActivation=false` 之前，避免等待被主场景阻塞的 Unity 异步操作队列；本轮持有的 handle 在销毁时释放。附近出生队列准备好后才退出加载界面，加载中停止 Play 会取消新增的异步工作。

上述新增预处理、提示资源预载和时间预算均只用于 Editor。Player 继续使用原版资源与既有流程，Android 性能不在本次验证范围。

## 验证口径

使用相同真实存档和位置启动，不注入移动或战斗输入；测试前备份存档，测试后按 SHA-256 恢复。随机状态未固定，因此移动、巡逻和活跃代理数量不要求逐帧相同。

CPU Profiler 普通采样（未启用 Deep Profile）的出生阶段结果：

| ECS 更新指标 | 优化前 | 最终版本 |
|---|---:|---:|
| 采样内累计耗时 | 12,061.79 ms | 3,139.40 ms |
| 单次更新 P95 | 249.81 ms | 23.23 ms |
| 最慢一次 ECS 更新 | 738.18 ms | 211.75 ms |

累计 ECS 时间下降约 74%，P95 下降约 91%。最终记录的游戏更新中没有出现 `AssetBundle.asset/allAssets [Forced assets loading]` 样本；此前首次提示的同步资源加载尖峰已移到加载阶段异步完成。主场景冷加载/激活仍可有约 3.6 秒的 Editor 整帧等待，上表不包含这部分收益承诺。

- 61 个源模板逐一比较可见渲染器、网格、材质、骨骼和脚本引用，并验证原来源池键、回收复用及激活位置。
- 反例覆盖：活动网格、序列化引用、骨骼、碰撞体、动画激活目标和专用脚本不能被裁掉；原 prefab 层级保持完整。
- 实际 Main 保留 716 个出生点、1,035 个游戏单位，普通和事件待初始化队列最终均为 0。
- 正常 Boot → Main → Stop 与重复进入验证；确认编译和 Console 错误为 0，源角色 prefab 未修改。

本地 CPU 数据、审计和检查脚本保存在 `Recovery/RuntimeDiagnostics/startup-*`，原指令记录在 `Recovery/Evidence/ChestNative/startup-arm64.txt`，按仓库既有规则不提交大型过程证据。

性能数字只代表本机 Editor 的采样范围，ECS 时间不等于整帧时间或整段启动时长。Unity 首次 JIT、域重载、主场景反序列化和编辑器集成仍有独立成本；本次不宣称消除这些冷启动等待，也不关闭正常域重载。

关于图形管线预热的平台和调用限制，可参阅 [Unity 官方 PSO 预热说明](https://docs.unity.com/en-us/engine/6000.0/manual/materials-and-shaders/shaders/shader-troubleshooting/shader-reduce-stalling/shader-prevent-stutter/shader-pso-trace-warming/shader-prewarm)。
