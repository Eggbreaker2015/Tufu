# 模拟器无头运行手册（C0a 探路版）

> 状态：C0a 首版，2026-10-28。记录无头运行的探路结论、实测耗时和读代码清单，交给 C0b 接手扩写。设计见 [Design.md](Design.md) 第 2 节；本文与设计稿不一致的地方在第 9 节列出，由 Z1 汇总回设计稿。

## 结论（进退判定）

**进：首选方案可行，不需要退到 F1/F2。** 在运行工作树里用 `-batchmode` 跑最小入口 `SimulationBatch.Run`：从 Boot 进入 Play、等到就绪、退出 Play、以退出码退出编辑器。代码最终版（提交 `4559118`）上连续 3 次成功，另有 1 次 `-nographics` 成功；此前各提交上同一入口还有 11 次成功，游戏就绪 B 中位数 5.2 秒（上限 120 秒）。2.9 节第 5、6 项都有明确结论：

- **PlayerSettings 改名可用（Q08 用首选方案）。** 改名后编辑模式与 Play 模式里的 `Application.persistentDataPath` 都换到沙箱名下，PlayerPrefs 也写进沙箱域；改回后 `ProjectSettings.asset` 的 sha256 不变，运行工作树 `git status` 为空，真实存档与真实 plist 的 sha256 在全部运行（15 次 Play、3 次只改名）后都与运行前相同。注意两点：PlayerPrefs 的未保存写入要在改回之前 `PlayerPrefs.Save()`（第 3 节）；编辑器每次退出都会重写真实 plist 的修改时间，内容不变，所以只能比哈希，不能比 mtime。
- **AB 抽组只在一处。** `ABTestModel.SelectVariant` 的 `UnityEngine.Random.Range`（`ABTestModel.cs:38-39`）是全部抽组的唯一来源；实测 `fastRPMParams` 在不同运行里抽到 A 或 B，必须加 C0b 的钩子。

其余：

- 不挑 `1186f67`（Q09）：不挑它也能启动并就绪。
- `-nographics` 也可用（2 次，图形设备 `Null`，结果相同）。默认值是否改成 `-nographics` 留给 C0b 的 `selftest` 决定。
- Play 模式里的游戏代码不会联网：分析与集成全是空桩，插件 DLL 也是空桩（第 4 节）。外发连接只来自 Unity 编辑器自身，不进入 Play 也有。
- 新档就绪时 `AnyoneAlive && IsEnabled` 的刷怪点有 **699 / 716** 个。17 个例外都在机器人范围之外，或者本来就生不出单位。就绪补救在新档上是空操作（第 6 节）。

## 1. 环境与命令

| 项 | 值 |
|---|---|
| Unity | 6000.6.3f1，`-disable-assembly-updater Unity.Mathematics.dll Unity.Burst.dll` |
| 运行工作树 | `/Users/joyfort/MyProjects/Tufu-stage5-run`，游离 HEAD，检出单元分支 `balance/s5-C0a` 的提交 |
| Library 来源 | W0 用 `cp -cR` 从主检出复制（宠物分支）；首次 batchmode 打开 48 秒，0 编译错误 |
| 入口 | `ED/SimulationBatch.cs`（`Codebase.Simulation.EditorTools.SimulationBatch`）：`Run`（完整探路）、`RenameProbe`（只做编辑模式下的改名实验，不进 Play）|
| 沙箱名 | `companyName = TufuSimSandbox`，`productName = C0a-spike` |

```bash
/Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath /Users/joyfort/MyProjects/Tufu-stage5-run/Recovery/StudyProject \
  -disable-assembly-updater Unity.Mathematics.dll Unity.Burst.dll \
  -logFile <out>/unity.log \
  -executeMethod Codebase.Simulation.EditorTools.SimulationBatch.Run \
  -tufuSpikeOut <out>          # 可选 -tufuReadyTimeout <秒>（默认 300）、-nographics
```

入口的流程：

1. 记录进程启动到入口被调用的时间 `E`；核对进入 Play 时域重载与场景重载都开着。
2. 改名进入沙箱：
   - 断言 `persistentDataPath` 已变；
   - 写一个探针键并 `Save()`，经 `defaults read` 确认它在沙箱域、不在真实域；
   - 然后清空沙箱：`PlayerPrefs.DeleteAll()` 并删除沙箱存档目录。
   - 任何一步失败都改回名字，以退出码 2 结束，不进 Play。
3. `playModeStartScene = Boot`，`isPlaying = true`。
4. 域重载后由 `[InitializeOnLoad]` 的静态构造接续，状态经 `SessionState` 传递，请求里的进程号与当前不符就忽略。
5. 在 `EditorApplication.update` 里等就绪：`SimulationSystem.Instance != null`，`EnemiesModel.Spawners.Count > 0`，`PendingBootstrap` 与 `PendingEventMobBootstrap` 都为空，且这个状态连续保持 1 秒（`t_ready` 取这 1 秒的起点）。
6. 测量：刷怪点清单、AB 分组、空闲帧、已恢复方法的耗时、空 `ExecuteTick` 循环、Q36 补救探针。
7. 退出 Play，在 `EnteredEditMode` 时依次：写报告、`PlayerPrefs.Save()`（仍在沙箱域）、改回名字（并清掉 PlayerSettings 的脏标记）、`EditorApplication.Exit(code)`。改回与退出在同一个调用里，中间不会有游戏代码运行。

退出码沿用设计稿 2.2：0 成功，1 未捕获异常，2 守卫失败，3 超时（自 Play 请求起 600 秒），4 未就绪（默认 300 秒），5 请求无效。

输出写在 `-tufuSpikeOut` 目录：

| 文件 | 内容 |
|---|---|
| `spike_report.txt` | 时间戳、耗时与各项结论，`键=值` |
| `spawners_ready.csv` | 就绪时每个注册刷怪点一行 |
| `spawners_ready_plus3s.csv` | 就绪 3 秒后再导一次 |
| `spawners_remedy.csv` | 补救探针 |
| `spawners_after_remedy.csv` | 补救之后的刷怪点 |

验收时外部脚本还做了这些检查：

- 启动前确认没有其它 Unity 进程、运行工作树干净；
- 运行中每 3 秒用 `lsof -i` 与 `nettop` 采样；
- 进程退出后比较真实存档目录、真实 plist、`ProjectSettings.asset` 的 sha256，并检查 `git status --porcelain -uall`。

## 2. 实测耗时

来源：最终提交 `4559118` 上的 3 次运行，加之前提交上同一入口的 9 次运行，共 12 次（有图形）；另有 2 次 `-nographics`。Library 已就绪；每次换提交后的第一次启动要编译 Editor 程序集，约 0.4–0.7 秒，计入 `E`。

| 阶段 | 中位数 | 范围 | 设计稿 2.7 的估计 | 说明 |
|---|---|---|---|---|
| 编辑器冷启动 `E`（进程启动 → 入口被调用）| 2.7 秒 | 2.5–4.0 | 30–90 秒 | |
| 进入 Play 的域重载 | 0.57 秒 | 0.56–0.59 | 1.1–1.7 秒 | `t_play_requested` → `[InitializeOnLoad]` 重新执行；日志 `Domain Reload Profiling` 为 560–620 ms |
| 请求 Play → `EnteredPlayMode` | 1.12 秒 | 1.09–1.16 | — | |
| 进入 Play → 游戏就绪 `B` | 5.2 秒 | 5.05–5.69 | 20–60 秒 | 期间 7300–9900 帧 |
| 退出 Play `X` | 0.11 秒 | 0.11–0.13 | — | `isPlaying = false` → `EnteredEditMode` |
| `EditorApplication.Exit` 之后编辑器关闭 | 4.9 秒 | 2.0–5.4 | 5–10 秒（含退出 Play 与资产守卫）| |
| 整个进程（含约 4 秒测量）| 17.2 秒 | 13.8–17.6 | 2–4 分钟（含模拟）| |

`-nographics`：`E` 2.4–2.9 秒，`B` 4.9–5.6 秒，`X` 0.12–0.14 秒。

一个 `reenter_play` 作业的固定开销约为 `B + X ≈ 5.3 秒`；冷启动一次另加 `E + 关闭 ≈ 5–8 秒`。5.4 节原先按 B = 40、X = 8、E = 60 秒估计，K1 之前应按这里的数字重算。

### 帧与 Tick 吞吐

| 项 | 中位数 | 说明 |
|---|---|---|
| batchmode 空闲帧（就绪后，游戏照常运行，1015 个敌人）| 1.9 毫秒/帧 | `-nographics` 为 1.5 |
| 已恢复的 `ExecuteTick` 是空的，用原版 `OnUpdate` 循环跑 `SimulationSpeed = 1 000 000` | 2.2 毫秒/帧 | 一百万次空调用只比空闲帧多 0.3 毫秒，空循环不是瓶颈 |
| `CalculateSpawnerData(true)`（GPM），新档只开岛 1 | 0.13 毫秒 | |
| `CalculateSpawnerData(false)`（机器人过滤），只开岛 1 | 0.06 毫秒 | |
| 同上两次，内存里把 `UnlockedLocationNumber` 临时设为 17 | 3.7 毫秒（2.9–4.0）| GPM 3.66 + 机器人 0.05 |
| `CalculateSimulationData()`（公开方法，含 DPS、防御、缓存、GPM）| 0.18 毫秒 | 只开岛 1 |

所以"每个慢阶段 Tick 两次 `CalculateSpawnerData`"的基础成本：

- 第 0 天约 0.19 毫秒，1 秒的帧能跑约 5000 个 Tick；
- 全部岛屿解锁时约 3.7 毫秒，约 270 个 Tick；
- 都在 2.7 节"空策略 ≤ 5 毫秒"的预算内。

机器人过滤这一遍很便宜，是因为新档的 `CurrentPlaytime = 0`，`IsCapOptimumTime` 把大部分刷怪点筛掉了；运行中后期它会变贵，C3b 需要按天实测。

## 3. 沙箱：PlayerSettings 改名（Q08）

| 检查 | 结果 |
|---|---|
| 编辑模式改名后的 `persistentDataPath` | `~/Library/Application Support/TufuSimSandbox/C0a-spike`（改前是 `…/Multicast Games/Butcher hero_ hook'n'chew RPG`）|
| Play 模式里的 `persistentDataPath` | 同上；游戏的 `game_data.bin` 写在沙箱目录 |
| PlayerPrefs 探针 | 写入沙箱域 `unity.TufuSimSandbox.C0a-spike`，真实域里没有；改回后 `HasKey` 为假 |
| `ProjectSettings.asset` | 全部运行前后 sha256 都是 `d5d9c55f…`；`git status` 为空 |
| 真实存档目录、真实 plist | 全部运行前后 sha256 与基线相同 |

注意事项（C0b 照做）：

1. **plist 文件是延迟落盘的。** 探针只读磁盘上的 plist 时，第二次运行 5 秒内都没等到（文件已存在时 cfprefsd 不立即写盘）。改用 `/usr/bin/defaults read <域> <键>` 经 cfprefsd 读取后，40–300 毫秒内确认。
2. **未保存的 PlayerPrefs 写入留在编辑器内存里，退出时写进当时的域。** `Boot.cs:120` 的 `PlayerPrefs.SetInt("game_loaded_once", 1)` 没有 `Save()`：
   - 就绪时 `PlayerPrefs.GetInt` 已经是 1，但 `defaults read` 在沙箱域里查不到；
   - 退出 Play 后在改回名字之前 `PlayerPrefs.Save()`，它才进入沙箱域。

   不先 `Save()` 的话，这类写入会在编辑器退出、名字已改回时落进真实域。早期几次运行没有这一步，真实 plist 的内容也没变，很可能只是因为真实域里本来就有 `game_loaded_once = 1`。
3. **编辑器每次退出都会重写真实 plist。** 即使不进 Play（`RenameProbe`），退出时也会写，内容不变，只改修改时间。启动器只比较 sha256。
4. **编辑器自身也写 `persistentDataPath`。** Unity 编辑器的分析数据写在 `Unity/<云项目 id>/Editor/Analytics/`。改名期间它们写进沙箱目录，真实目录下的这两个文件一直没变。
5. 改名会把 PlayerSettings 标脏（`IsDirty` 为真）。改回后若原先不脏就 `EditorUtility.ClearDirty`，免得以后的 `SaveAssets` 重写 `ProjectSettings.asset`。实测不清也没有落盘，清掉更保险。
6. Unity 自己的键也会写进沙箱域：`unity.cloud_userid`、`unity.player_session_count`、`unity_connect.*`、`UnityGraphicsQuality`。游戏写的是 `locale`、`ud_migration_complete_v1` 和 `game_loaded_once`。
7. 进入 Play 的设置：`EditorSettings.enterPlayModeOptionsEnabled` 在 6000.6.3f1 读出来是 `False`，但 `EditorSettings.asset` 里写的是 `m_EnterPlayModeOptionsEnabled: 1`；`enterPlayModeOptions` 为 `None`。这两个属性没有标 `Obsolete`。判断式取"开关为假，或选项里没有 `DisableDomainReload`/`DisableSceneReload`"。实测每次进入 Play 都有域重载（`[InitializeOnLoad]` 重新执行），Boot 也是重新加载的。

## 4. 网络与分析（2.9 第 1 项）

### 4.1 读代码清单

下表是启动链路上的分析、集成与后端调用。全部是恢复工程里的空桩：方法体为空或 `return default`，`async` 方法的状态机 `MoveNext` 也是空的。

| 调用点 | 被调方 | 在 `6af011f` 的实现 |
|---|---|---|
| `MainSceneInitializer.cs:180` | `new AppMetricaAnalyticsWrapper()` | `Send<T>` 为空（`Integrations/AppMetrica/AppMetricaAnalyticsWrapper.cs`）|
| `MainSceneInitializer.cs:181-182`；`Boot.cs:118` | `BootTimeReporter.Start/ReportStep/Tick/FlushFinalTick` | 全部为空（`Analytics/BootTimeReporter.cs`）|
| `MainSceneInitializer.cs:197` | `HubBackendBootstrap.GetBackendInfo` | `return default` |
| `MainSceneInitializer.cs:204` | `BackendApi.Init` | `Multicast.Backend.dll`，见 4.2 |
| `MainSceneInitializer.cs:205` | `HubBackendBootstrap.TryLogin` | `return default` |
| `MainSceneInitializer.cs:221` | `MaxUserConsentService.InitializeAndShowUserConsent` | `return default` |
| `MainSceneInitializer.cs:251` | `AppMetricaTrackingUpdater.ApplyConsent` | 为空 |
| `MainSceneInitializer.cs:252-253` | `BootTimeEvent` 经 `AppMetricaAnalyticsWrapper.Send` | `Send` 为空 |
| `MainSceneInitializer.cs:254` | `AdjustInitializer.InitializeAsync` | `return default` |
| `MainSceneInitializer.cs:255` | `FirebaseInitializer.InitializeAsync` | `return default` |
| `MainSceneInitializer.cs:256` | `HubUpdateAttributesFlowRunner.CollectDeviceIdentifiers` | 为空 |
| `Boot.cs:119` | `GameLoadedEvent` 经 `AppMetricaAnalyticsWrapper.Send` | `Send` 为空 |
| `Boot.cs:65` | `BackendRewardInboxService` | `Multicast.Backend.dll` 空桩 |
| `Boot.cs:68-69` | `StudyPaymentsService.Initialize` | 本地沙箱支付（学习工程的改写）|
| `Boot.cs:33`、`:116` | `LocalNotificationService`、`NotificationsModel.InitializeNotifications` | 本地通知；`Unity.Notifications.Android.dll` 为空桩 |
| `AppMetricaActivator.cs:9-11` | `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)] Activate` | 为空 |
| `ConsolePopupView.cs:99-101` | `[RuntimeInitializeOnLoadMethod] RegisterLogListener` | 为空 |
| `ABTestInitializer.cs:39-40` | `UnityServices.InitializeAsync`、`RemoteConfigService.FetchConfigsAsync` | 不会走到：`ABTest Data Config.asset:15` 的 `developCohortsMode: 1`（UseDevelop），在 `ABTestInitializer.cs:37` 就返回了；两个 DLL 也是空桩 |

游戏代码里没有任何网络 API：在 `Assets/Scripts` 与 `Assets/Editor` 里搜 `UnityWebRequest|HttpClient|WebClient|System.Net|TcpClient|Socket|new WWW`，零命中。唯一的 `Application.OpenURL` 在 `UI/RateUs/RateUsView.cs:79`，只在玩家点击时调用。

### 4.2 插件 DLL

`Assets/Plugins` 下的第三方 SDK 都是只有签名的空桩程序集。下表用 `System.Reflection.Metadata` 统计方法体 IL 的长度（≤ 8 字节即 `ret`、`ldnull; ret` 一类）：

| DLL | IL ≤ 8 字节的方法数 / 有方法体的方法数 | 其余方法 |
|---|---|---|
| `Multicast.Backend.dll` | 245 / 251 | 6 个，≤ 30 字节，都是构造函数与取值器 |
| `AppMetrica.dll` | 216 / 225 | 9 个 JSON 解析桩，10 字节 |
| `AdjustSdk.Scripts.dll` | 550 / 554 | 4 个，10 字节 |
| `Firebase.App.dll` | 269 / 277 | 8 个，10 字节；82 个 P/Invoke 声明没有被任何方法体调用 |
| `Firebase.Analytics.dll` | 71 / 74 | 同上，18 个 P/Invoke |
| `MaxSdk.Scripts.dll` | 470 / 481 | 11 个，≤ 15 字节 |
| `Facebook.Unity.dll` | 1157 / 1167 | 10 个，≤ 10 字节 |
| `Unity.Services.Core*.dll`、`Unity.Services.RemoteConfig.dll`、`Unity.Services.Authentication*.dll`、`Tapjoy.Plugin.Runtime.dll`、`Multicast.CloudSave.dll` | 几乎全部 | 其余都是 ≤ 22 字节的构造函数与取值器 |

结论：沙箱下跳过这些调用不是"不外发"的必要条件。C0b 仍可以按设计在 `MainSceneInitializer` 里加跳过作为纵深防御，但不必依赖它。

### 4.3 实测

- **采样。** 运行中对 Unity 进程每 3 秒做一次 `lsof -i` 与 `nettop`：
  - Unity 的 TCP 外连全部指向 `127.0.0.1:7897`，即本机系统代理（Clash Verge，`scutil --proxy` 的 HTTP/HTTPS 代理端口）；
  - 另有回环连接与监听端口：PlayerConnection 的 TCP 34999/55504、UDP 34997/54997 组播发现，`127.0.0.1` 上的调试端口。
- **代理日志反查目标主机**（`clash-verge-rev/logs/service/`，按本地端口对应）：
  - Unity 编辑器连接的是 `config.uca.cloud.unity3d.com`、`public-cdn.cloud.unity3d.com`、`cdp.cloud.unity3d.com`，以及许可证的 `license.unity3d.com`；
  - 只做编辑模式的 `RenameProbe` 也有同样 4 条；进入 Play 的运行多一条 `cdp.cloud.unity3d.com`（编辑器分析）；
  - 运行时段内没有 AppMetrica、Adjust、Firebase、AppLovin、Multicast 或任何游戏后端的主机。
- **对 C0b 的影响。** 设计稿 2.3 的"`nettop` 采样断言没有外发连接"直接照做会失败，因为编辑器本身就外连；而且经过代理时 `nettop` 只看得到 `127.0.0.1:7897`。可行的口径有两种：
  - 以只做编辑模式的 `RenameProbe` 为基线，断言 Play 期间没有新的远端（代理日志可用时比主机名）；
  - 或断言游戏代码里没有网络 API（静态检查），加 Play 期间日志里没有 SDK 输出。

## 5. 启动链路（2.9 第 1、2、3 项）

每一步都在 batchmode 下完成，日志里没有游戏代码的错误或异常，`Debug.LogError` 计数为 0：

1. `Boot` 场景：`MainSceneInitializer.Awake/Start` 的完整链路（`MainSceneInitializer.cs:175-276`），包括：
   - 新建并初始化 `GameDataService`（存档在沙箱目录）；
   - `PlayLaunchSoundAsync`：`AppLaunchSoundPlayer.cs:107-118`，等视频首帧有超时上限，不会卡住；
   - AB 初始化、本地化；
   - `PreloadEditorStartupAssetsAsync`：两个 Addressables 预制体，从 Windows 包加载成功（`MainSceneInitializer.cs:279-298`）；
   - 异步加载 `Main`，以及编辑器的 `EditorEnemySpawnTemplates.PrepareAsync`；
   - 激活 `Main`，等近处刷怪点自举。
2. `Main` 场景：`Boot.Start`（`Boot.cs:24-123`，挂在 Main 里，执行顺序 -1000）注册模型和系统；716 个刷怪点在 `Start` 里注册并进入 `PendingBootstrap`；`EnemySpawnerBootstrapSystem` 按每帧 5 毫秒的编辑器预算把它们全部初始化完。
3. 编辑器日志里唯一的异常是 Unity 自己的 QuickSearch 索引（`UnityEditor.Search.SearchDatabase.EnumerateAll` 的 `ArgumentOutOfRangeException`），在编辑器启动时，与游戏无关，每次都有。另有约 80 条 "There are no audio listeners in the scene" 警告：`LoadingScreen.DisableAudioListener` 之后 Main 里没有监听器，无害。
4. **Q09：不需要 `1186f67`。** 本分支没有它也能启动并就绪；它修的是 macOS Metal 下 Windows 包的显示问题（洋红 UI、白方块粒子），不影响启动，也不影响模拟用到的数据。另外，`1186f67` 依赖的 `Recovery/Evidence/addressables-recovered-asset-map.json` 被 `.gitignore`（`/Recovery/Evidence/*`）排除，新工作树里默认没有；运行工作树里有一份未入库的副本。
5. **无图形模式**：`-nographics` 下 `SystemInfo.graphicsDeviceType = Null`，链路、就绪个数、错误集合都与有图形时相同，`B` 相当。

## 6. 刷怪点就绪（2.9 第 4 项，Q36）

### 6.1 就绪时的个数（补救之前）

| 项 | 值 |
|---|---|
| 场景里的 `EnemySpawner` 组件 | 717 |
| 注册到 `EnemiesModel.Spawners` 的 | 716 |
| `IsEnabled` 为真的 | 716 |
| `AnyoneAlive && IsEnabled` | **699**（就绪时与就绪 3 秒后相同，15 次 Play 运行都一样）|
| 敌人总数 | 1015 |
| 注册顺序 | 15 次运行的 key 序列 sha256 相同（`b817c620…`）|

- **716 与 717 的差**：`balance.sqlite` 的 `spawners` 表有 717 行、716 个不同的 key。`Eventspawner_Boss_79e49ca7fb46ec243a029656b3fa8507` 出现两次，其中一个所在的 GameObject 未激活（`active = 0`），所以 `Start` 不执行、不注册。`EnemySpawnersDebugger.duplicateKeys` 也记着它。这一点供 C1 的映射表与 DP5/DP6 使用。
- **17 个例外**，全部是 `AnyoneAlive = false`：

  | 类别 | 个数 | 原因 |
  |---|---|---|
  | 活动（`IsEvent`）| 5 | `TryInitializeEventSpawner` 是返回 `false` 的桩（`EnemySpawner.cs:860-863`），`ProcessSpawnTick` 在 `:746` 直接返回 |
  | VIP | 10 | 新档 `LastUnlockedLocation.Number < 2`，`TrySpawnUnit` 拒绝（`EnemySpawner.cs:924-925`）|
  | `Spawner_Dummy_b310…` | 1 | `IsDummy`，`TrySpawnUnit` 拒绝（`:922-923`）|
  | `BossTower_c296…` | 1 | `config.Count = 0`，永远生不出单位 |

  活动和 VIP 本来就被机器人过滤排除（`SimulationSystem.cs:637`），所以机器人能用的普通刷怪点里，除 BossTower 外全部有单位。冻结值的初值：**`AnyoneAlive && IsEnabled` = 699**，例外即上表 17 个，清单见每次运行的 `spawners_ready.csv`。
- **新档没有被任务禁用的刷怪点。** 716 个全部 `IsEnabled = true`，MonoBehaviour 也都 `enabled`。读代码：
  - `QuestSpawnerControlSystem.OnUpdate` 只为任务步骤生成任务怪（`QuestMobSpawnerService.SyncStep`），不禁用刷怪点；
  - `EnemySpawner.SetEnabled` 在游戏代码里没有调用方；
  - 禁用只发生在 `EnemySpawner.cs:485-489`（`ClearedOnce && DisableAfterFirstClear`，新档不会）与 `:775-783`（清空后 `DisableAfterFirstClear`）。
- **就绪时的 `Cooldown` 不为零，而且取决于墙钟**，C0b/C3b 必须处理：
  - 697 个刷怪点的 `Cooldown` 等于"距下一个 UTC 零点的秒数"（例如就绪于 10:22:44Z 时是 49035）；8 个为 0；10 个 VIP 加 Dummy 为大负数。
  - 来源是满员且 `SyncBigRespawnToUtc` 的刷怪点（710/717 个是 1）：`ScheduleFullSpawnerProcessing` 把下一次处理排到 UTC 对齐时刻，随后 `ProcessSpawn` 每帧写 `Cooldown = nextProcessTime - now`（`EnemySpawner.cs:669-675`）。
  - 已恢复的机器人过滤在 `IsRunning` 时跳过 `Cooldown > 0` 的刷怪点（`SimulationSystem.cs:641`）。照原样设 `IsRunning`，几乎所有刷怪点都会被排除。
  - 沙箱时钟冻结后这个值是确定的，但仍不为零。设 `IsRunning` 之前要把 `Cooldown` 清零，或者由重建的"死亡只用冷却表示"（Q07）自己管理 `Cooldown`。由 C0b 的就绪补救和 C3b 的准备阶段决定。

### 6.2 Q36：`InitializeSpawnerNow` 还是 `ProcessSpawn`

探针在就绪后、固定同一个 `now` 下执行：

1. 对每个非活动、非 VIP 且没有单位的刷怪点（2 个：BossTower 与 Dummy），依次调用：
   - `ProcessSpawn(now)` 两次；
   - 反射 `InitializeSpawnerNow(now)`；
   - 再用 `now + 1 天` 调用 `InitializeSpawnerNow`。

   4 种调用都补不出单位，耗时 0.06 毫秒。原因是结构性的：`Count = 0`、`IsDummy`。
2. 构造 `HandleDeath` 之后的状态：选一个有单位的普通刷怪点 `tree_1 (11)`（小冷却 240 秒，`Count = 1`），清空单位，令 `SavedCount = 0`、`LastSpawnTime = now`，并更新 `nextProcessTime`。然后：
   - 同一个 `now` 下 `ProcessSpawn`：0 个单位；
   - 同一个 `now` 下 `InitializeSpawnerNow`：0 个；
   - `ProcessSpawn(now + 240)`：1 个。

读代码的解释：

- **新档自举在任何冻结时刻都能生满。** 新档的 `LastSpawnTime = 0`，`ProcessSpawnTick` 的 `spawnCount = min((now - LastSpawnTime) / small, …)`（`EnemySpawner.cs:808`）在任何现实的 `now` 下都覆盖全部缺额。就绪时没有需要补的普通刷怪点。
- **两种调用都要求时钟走过一个小冷却。**
  - `ProcessSpawn` 要求 `isInitialized`（`:658`），并在 `nextProcessTime > now` 时直接返回（`:669`）。生成或死亡之后 `nextProcessTime = max(now, LastSpawnTime + small)`（`:1354`），所以冻结时钟下它补不出单位。
  - `InitializeSpawnerNow`（`:594-602`）先 `RestoreSavedUnits`（按 `SavedCount` 补，不看时钟；但 `HandleDeath` 在 `:1226` 已把 `SavedCount` 设为当前个数），再调同一个 `ProcessSpawnTick`。冻结时钟下同样补不出单位。它还会无条件把 `isInitialized` 置真，跳过 `CompleteBootstrap` 的守卫。
- **建议（C0b）：**
  - 只对 `!IsBootstrapComplete` 的刷怪点调用 `CompleteBootstrap(now)`。它是 `internal`，`Assembly-CSharp` 里的 `SimulationHarness` 可以直接调用，不需要反射私有方法。新档上这一步是空操作，就绪条件已经保证队列为空。
  - 真要补一个已清空的刷怪点，用 `ResetForRespawn()`（`:445`，把 `LastSpawnTime` 清零）再 `ProcessSpawn(now)`，或者 `TrySpawn()`（`:640`）。
  - 机器人运行期间 `EnemySpawnSystem` 不处理刷怪点（`EnemySpawnSystem.cs:36-37`）。机器人按 Q07 不真的杀死单位，所以运行中不需要补救。

## 7. 时钟与 AB（2.9 第 6 项）

### 7.1 `ITimeService`

`TimeService.Now = DateTime.UtcNow + TimeSkipSeconds`：

- 偏移来自 PlayerPrefs，在构造时读一次（`Services/Time/TimeService.cs:23`、`:30-32`）；
- 构造在 `Boot.cs:32`；
- 同一帧内缓存（`:20-24`）。

全仓 40 个文件用到它。影响自举与刷怪点的：

| 文件:行 | 用途 |
|---|---|
| `Enemy/Systems/EnemySpawnerBootstrapSystem.cs:44` | 自举时刻 `now`，传给 `CompleteBootstrap(now)` |
| `Enemy/Systems/EnemySpawnSystem.cs:38` | 每帧 `ProcessSpawn(now, …)`（`IsRunning` 时不执行）|
| `Enemy/EnemySpawner.cs:417` | `NowUnixSeconds`：`Start` 里的 `InitializePersistentState`（`:523`），`TrySpawn`/`TrySpawnIgnoringMaxCount`（`:644`、`:652`），`TrySpawnUnit` 的充能恢复与大冷却（`:928-931`），`HandleDeath`（`:1225`）|
| `Events/Services/EventTimeCalculator.cs:33`、`:67`；`Events/Systems/EventTimerSystem.cs:62`、`:89`；`Events/Models/EventsRepository.cs:543` | 活动是否进行，决定活动刷怪点 |
| `Crystals/CrystalModel.cs:438`、`:573`、`:765` | 水晶目标刷怪点的重生 |
| `VIP/VipModel.cs:119` | VIP 状态 |
| `Quests/QuestModel.cs:230` | 任务时间戳 |
| 其余，影响经济、不影响自举 | `Shop/Systems/ShopRepositorySystem.cs:27`、`Player/DailyProcs/DailyProcsModel.cs:46`、`Rewards/StatReward.cs:32`、`BossTower/BossTowerModel.cs:97`、`Analytics/UserModel.cs:490`、`:506` 等 |

全仓只有 `TimeService.cs` 直接读 `DateTime.UtcNow`/`DateTime.Now`。所以 C0b 只在 `TimeService` 一处加沙箱时钟钩子，就能覆盖全部墙钟读取。

### 7.2 AB 抽组

- **唯一的随机抽组**：`ABTestModel.SelectVariant`（`ABTests/ABTestModel.cs:38-39`）。
  - 调用链：`ABTestModel.TryApply`（`:23-37`）← `Apply` ← `ABTestData.ApplyRemoteRules`（`ABTestData.cs:116`）← `ApplyRemoteAll` ← `ABTestInitializer.ApplyDevelopTests`（`ABTestInitializer.cs:81`）← `MainSceneInitializer.cs:216`；
  - 发生在 Main 激活、刷怪点自举之前；
  - 唯一的其它写入是调试界面的 `ABTestDebugView.SetVariant`（`ABTestDebugView.cs:64`），只在手动操作时触发。
- **规则**（`ABTest Data Config.asset`）：
  - 多组随机的有 `fastRPMParams` AB（`:84`）、`soul_pass` AB（`:95`）、`crystal_pass` AB（`:104`）、`offerwall` AB（`:115`）、`enchant_failure_dust_cashback` ABC（`:212`）；
  - `spawner_cooldowns_v3`、`enemy_hp_damage_mult_v3`、`rpm_old_algorithm`、`no_ads`、`patch_compensation_gems`、`common_reward_multipliers` 只有 A；
  - `rewarded_delay_from_start`、`level_pass` 没有规则。
- **实测**：15 次 Play 运行里 `fastRPMParams` 抽到 A 10 次、B 5 次；`crystal_pass`、`offerwall`、`enchant_failure_dust_cashback` 也在变。刷怪点的充能与冷却只看 `spawner_cooldowns_v3`，它恒为 A，所以 699 不受影响；但 `fastRPMParams` 进入 `GetFastRPMConfig`（`SimulationSystem.cs:232-237`），必须固定。
- **与 PlayerPrefs 的耦合**（设计稿没有提到）：
  - `ABTestModel` 构造时用 `PlayerPrefs.GetInt("game_loaded_once")` 判断新用户（`ABTestModel.cs:16`）；
  - 没有前缀的规则只对新用户生效（`:29`）。
  - 所以沙箱必须从空的 PlayerPrefs 开始，或者 `prefs_seed` 里不能有 `game_loaded_once`。真实 plist 里是 1，共用真实 PlayerPrefs 的话，新档的 `Data.Tests` 会是空的。
  - 钩子放在 `SelectVariant`，对新老用户都成立。

## 8. 给 C0b 的交接

- `ED/SimulationBatch.cs` 的所有权转给 C0b。保留的骨架：
  - `SessionState` 加进程号的跨域重载接续；
  - 就绪判据；
  - 改名、探针与清空顺序；
  - 退出时先 `Save()`、再改回、再 `Exit`。

  测量部分（`ProbeState`）是探路用的，可以删掉，或移到 `selftest`。
- 沙箱用首选方案（改名），不需要 `GameDataService` 的存档目录钩子。
- 就绪补救按 6.2 实现。冻结值的初值是 699（例外 17 个）。另外处理就绪时 `Cooldown` 非零的问题（6.1）。
- 不外发的断言口径见 4.3。
- **资产守卫要预先知道**：原版 `SimulationSystem.OnAwake` 每次进入 Play 都会在内存里改写 Simulation Config（`SimulationSystem.cs:256-268`）：
  - `IsRunning = AutoGambling`；
  - 新建 `Snapshots`、`TimeChunkSnapshots`、`RewardList`；
  - 清空 `Log`；
  - 把 `curveBossDifficult.keys` 设为空数组。

  这些改动不落盘（`git status` 一直为空），但退出 Play 后内存摘要与磁盘不同。按设计，每个作业进 Play 前都会触发一次强制重新导入，基线应取"打开工程后、首次进入 Play 之前"。
- 可以考虑默认加 `-nographics`（第 5 节第 5 条）；`B` 只有约 5 秒，`reenter_play` 很便宜，5.4 节的预算应大幅下调。

## 9. 与设计稿不一致的事实

| 设计稿位置 | 设计稿的说法 | 实测或读代码的结果 |
|---|---|---|
| 2.7 | `E` 30–90 秒，域重载 1.1–1.7 秒，`B` 20–60 秒，退出 5–10 秒 | `E` 2.7 秒，域重载 0.57 秒，`B` 5.2 秒，`X` 0.11 秒，关闭 2–5 秒 |
| 2.3、2.9 第 4 项、8.1 | 新档时 `QuestSpawnerControlSystem` 等禁用了一些刷怪点 | 它不禁用刷怪点；新档 716 个全部 `IsEnabled`。没有单位的 17 个是活动、VIP、Dummy、BossTower |
| 2.3、2.9 第 1 项 | `BackendApi.Init`、`BootTimeEvent`、Adjust/Firebase、`CollectDeviceIdentifiers` 每次启动都会执行副作用 | 都会被调用，但全是空桩，连同插件 DLL 都没有网络能力 |
| 2.3 | 用 `nettop` 断言 Unity 进程没有外发连接 | 编辑器自身会经系统代理连 Unity 云服务，不进 Play 也有；断言需要基线或换口径（4.3）|
| 2.3 | 就绪补救用 `InitializeSpawnerNow`，不行再用 `ProcessSpawn` | 新档不需要补救；冻结时钟下两者都补不出已清空的刷怪点；建议 `CompleteBootstrap` 或 `ResetForRespawn` 加 `ProcessSpawn`（6.2）|
| 2.3 | 没有提到 | 就绪时 697 个刷怪点的 `Cooldown` 是距 UTC 零点的秒数，会让已恢复的机器人过滤排除它们（6.1）|
| 2.3、Q08 | 改名后存档和偏好一起换到沙箱 | 成立；但 PlayerPrefs 的未保存写入要在改回前 `Save()`，真实 plist 的 mtime 每次退出都变（第 3 节）|
| 2.3 | AB 只与抽组的随机有关 | 还取决于 PlayerPrefs 的 `game_loaded_once`（7.2）|
| 2.3 | `EditorSettings` 断言域重载与场景重载（属性名待核对）| `enterPlayModeOptionsEnabled` 读出 `False`，与资产里的 1 不符；以 `enterPlayModeOptions` 无禁用标志为准（第 3 节第 7 条）|
| 2.10 | `Recovery/Evidence` 的资产映射表随 `1186f67` 可用 | 该目录被 `.gitignore` 排除，新工作树默认没有 |
