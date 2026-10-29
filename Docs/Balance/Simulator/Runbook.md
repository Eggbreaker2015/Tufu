# 模拟器无头运行手册

> 状态：C0b 版，2026-10-28。第 1–8 节是 C0a 的探路记录，编号保持不变（代码注释和其它文档按编号引用）；第 10–15 节是 C0b 实现的无头运行框架：用法、流程、沙箱钩子清单、启动器的检查、就绪与冷却、实测耗时与验收记录。设计见 [Design.md](Design.md) 第 2 节；本文与设计稿不一致的地方在第 9 节列出，由 Z1 汇总回设计稿。

## 结论（进退判定）

**C0b：用 batchmode，不退到 F1/F2。** `unity selftest` 在运行工作树里连续 3 次成功（进入 Main、就绪、就绪补救、一个空作业、退出 Play、资产守卫通过，退出码 0），三次的 `manifest.json`、`ready_spawners.csv`、`asset_guard.json` 逐字节相同。整次进程约 11 秒（换提交后第一次约 14 秒，多出来的是编辑器程序集编译）。默认仍不加 `-nographics`（Q11）；`-nographics` 的输出与有图形时逐字节相同（第 15 节），以后可以改成默认。

以下是 C0a 的结论：

**进：首选方案可行，不需要退到 F1/F2。** 在运行工作树里用 `-batchmode` 跑最小入口 `SimulationBatch.Run`：从 Boot 进入 Play、等到就绪、退出 Play、以退出码退出编辑器。代码最终版（提交 `4559118`）上连续 3 次成功，另有 1 次 `-nographics` 成功；此前各提交上同一入口还有 11 次成功，游戏就绪 B 中位数 5.2 秒（上限 120 秒）。2.9 节第 5、6 项都有明确结论：

- **PlayerSettings 改名可用（Q08 用首选方案）。** 改名后编辑模式与 Play 模式里的 `Application.persistentDataPath` 都换到沙箱名下，PlayerPrefs 也写进沙箱域；改回后 `ProjectSettings.asset` 的 sha256 不变，运行工作树 `git status` 为空，真实存档与真实 plist 的 sha256 在全部运行（15 次 Play、3 次只改名）后都与运行前相同。注意两点：PlayerPrefs 的未保存写入要在改回之前 `PlayerPrefs.Save()`（第 3 节）；编辑器每次退出都会重写真实 plist 的修改时间，内容不变，所以只能比哈希，不能比 mtime。
- **AB 抽组只在一处。** `ABTestModel.SelectVariant` 的 `UnityEngine.Random.Range`（`ABTestModel.cs:38-39`）是全部抽组的唯一来源；实测 `fastRPMParams` 在不同运行里抽到 A 或 B，必须加 C0b 的钩子。

其余：

- 不挑 `1186f67`（Q09）：不挑它也能启动并就绪。
- `-nographics` 也可用（2 次，图形设备 `Null`，结果相同）。默认值是否改成 `-nographics` 留给 C0b 的 `selftest` 决定。
- Play 模式里的游戏代码不会联网：分析与集成全是空桩，插件 DLL 也是空桩（第 4 节）。外发连接只来自 Unity 编辑器自身，不进入 Play 也有。
- 新档就绪时 `AnyoneAlive && IsEnabled` 的刷怪点有 **699 / 716** 个。17 个例外都在机器人范围之外，或者本来就生不出单位。就绪补救在新档上是空操作（第 6 节）。

## 1. 环境与命令

> C0a 的入口参数（`-tufuSpikeOut`、`-tufuReadyTimeout`）和 `RenameProbe` 已被 C0b 的入口取代，现行用法见第 10 节。本节保留探路时的记录。

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

来源：有图形的全部 13 次成功 Play 运行，一次不漏：

- `play2`（提交 `4301a19`，`B` 最慢的一次）；
- `final1–3`、`v2_final1–3`、`v3_final1–3`（中间各提交）；
- `v4_diag`、`v4_final2`、`v4_final3`（最终代码 `4559118`，`B` = 5.33、5.69、5.11 秒）。

另有 2 次 `-nographics`（`nographics1`、`v4_nographics`）单列。`play1` 在进 Play 之前因 plist 探针超时以退出码 2 结束（第 3 节第 1 条），没有 Play 阶段的数据。Library 已就绪；每次换提交后的第一次启动要编译 Editor 程序集，约 0.4–0.7 秒，计入 `E`。

| 阶段 | 中位数 | 范围 | 设计稿 2.7 的估计 | 说明 |
|---|---|---|---|---|
| 编辑器冷启动 `E`（进程启动 → 入口被调用）| 2.7 秒 | 2.5–4.0 | 30–90 秒 | |
| 进入 Play 的域重载 | 0.57 秒 | 0.56–0.59 | 1.1–1.7 秒 | `t_play_requested` → `[InitializeOnLoad]` 重新执行；日志 `Domain Reload Profiling` 为 560–620 ms |
| 请求 Play → `EnteredPlayMode` | 1.12 秒 | 1.09–1.19 | — | |
| 请求 Play → 游戏就绪 `B` | 5.2 秒 | 5.05–6.05 | 20–60 秒 | 期间 7300–9900 帧；起点是 Play 请求，不是 `EnteredPlayMode`，含上一行的约 1.1 秒；终点是"就绪状态保持 1 秒"的起点 |
| 退出 Play `X` | 0.12 秒 | 0.11–0.13 | — | `isPlaying = false` → `EnteredEditMode` |
| `EditorApplication.Exit` 之后编辑器关闭 | 4.8 秒 | 2.0–5.4 | 5–10 秒（含退出 Play 与资产守卫）| 进程墙钟减去 `t_exit − t_process_start` |
| 整个进程（含约 4 秒测量）| 17.2 秒 | 13.8–17.6 | 2–4 分钟（含模拟）| |

`-nographics`（2 次）：`E` 2.4–2.9 秒，`B` 4.9–5.6 秒，`X` 0.12–0.14 秒。复核时评审在 `b0eb43f` 上又跑了 1 次：`E` 2.48、`B` 4.97、`X` 0.12 秒，进程 14.3 秒。

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

机器人过滤这一遍很便宜，是因为新档的 `CurrentPlaytime = 0`，`IsCapOptimumTime` 把大部分刷怪点筛掉了；运行中后期它会变贵。C3b 的实测见第 16.3 节。

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
7. 进入 Play 的设置：`EditorSettings.enterPlayModeOptionsEnabled` 在 6000.6.3f1 读出来是 `False`，但 `EditorSettings.asset` 里写的是 `m_EnterPlayModeOptionsEnabled: 1`；`enterPlayModeOptions` 为 `None`。这两个属性没有标 `Obsolete`。所以 6000.6 上开关的读数不可信，**守卫只看 `enterPlayModeOptions` 里有没有 `DisableDomainReload`/`DisableSceneReload`**，不看开关。`b0eb43f` 之前的判断式是"开关为假，或选项里没有禁用标志"，开关恒读出假，这个守卫永远不会失败；`SimulationBatch.cs` 已改成只看禁用标志，C0b 照此实现。实测每次进入 Play 都有域重载（`[InitializeOnLoad]` 重新执行），Boot 也是重新加载的。

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
4. **Q09：不需要 `1186f67`。** 本分支没有它也能启动并就绪；它修的是 macOS Metal 下 Windows 包的显示问题（洋红 UI、白方块粒子），不影响启动，也不影响模拟用到的数据。`1186f67` 读的 `Recovery/Evidence/addressables-recovered-asset-map.json` 是入库文件（`.gitignore:31-33` 先排除 `/Recovery/Evidence/*`，再用 `!/Recovery/Evidence/addressables-recovered-asset-map.json` 放行），每个工作树里都有。
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

### 7.1a 绕过 `TimeService` 的时钟读取

在 `TimeService` 一处加沙箱时钟钩子**不能**覆盖全部墙钟读取。游戏代码里（`Assets/Scripts`，不含插件与编辑器代码）直接读时钟的地方：

| 文件:行 | 读什么 | 会不会影响模拟输出 |
|---|---|---|
| `Services/Time/TimeService.cs:23` | `DateTime.UtcNow` | 会；这就是要加钩子的地方 |
| `Analytics/UserModel.cs:431` | `DateTimeOffset.UtcNow`，`ResetDailyAdsIfNeeded` 取真实 UTC 零点（注释说明原版故意不用游戏时间）| 会。经 `CanWatchAd`（`:439-445`）进入 `AdsInitialize.cs:279` 的每日广告上限判断，经 `IncrementDailyAds`（`:447-452`）在 `AdsInitialize.cs:189` 记数。只要模拟走激励广告或 `no_ads` 的 `maxAdsPerDay > 0`，每日次数就按真实日期重置，与沙箱时钟脱节 |
| `Analytics/UserModel.cs:464` | `DateTimeOffset.UtcNow`，新档写入 `installTime` | 会。`installTime` 存进存档；`Rewards/StatReward.cs:31-34` 的 `GetResolvedType` 用"沙箱时钟的 UTC 日期 − 安装日期"的天数对 `DailyRotationStats.Count` 取模。`Crystal Event Roster Config.asset` 里有 `DailyRotation: 1` 的奖励（如 `:656`、`:679`），调用方是 `EventForecastService.cs:226` 和 `RewardIconResolver.cs:107`。沙箱时钟早于真实安装日时天数为负，C# 的 `%` 得负下标，会抛 `ArgumentOutOfRangeException`；晚于时，轮换下标随真实日期变化，输出不再与真实日期无关 |
| `Analytics/UserModel.cs:465` | `DateTimeOffset.UtcNow`，算 `daysSinceInstall` | 基本不会。只写进 `UserDataUd.DaysSinceInstall` 和分析事件（`UserContextEventBase`），没有游戏逻辑读它；但它进存档，会让存档摘要随真实日期变化 |
| `Analytics/Systems/AppPauseTimeSkipSystem.cs:24` | `Time.realtimeSinceStartup`，帧间隔超过 3 秒写 `UserModel.SkipSeconds` | 不会。`SkipSeconds` 没有读者，同一次更新里又被清零（`:31-35`）。模拟的长帧可能超过 3 秒，也无害 |
| `Enemy/Systems/EnemySpawnerBootstrapSystem.cs:42`、`:83` | `Stopwatch` 的每帧 5 毫秒自举预算 | 不影响就绪后的状态，只影响哪些刷怪点在哪一帧自举。时钟冻结时各帧的 `now` 相同，所以与分帧无关；不冻结时（原版）各刷怪点拿到的 `now` 不同 |
| `Analytics/BootTimeReporter.cs:11`、`FIMSpace/FDebug.cs:8` | `Stopwatch` | 不会，只用于分析与调试日志 |
| `Crystals/CrystalModel.cs:204`、`:818` 及若干 UI | `Time.unscaledTime` | 不会，只决定横幅与 UI 动画何时显示 |

给 C0b 的建议（设计稿 7.3 的钩子清单里没有 `UserModel`，需要 Z1 或协调者放行所有权）：

- 首选：在 `UserModel` 的两处 `DateTimeOffset.UtcNow`（`:431`、`:464-465`）改读 `ITimeService.Now`，只在沙箱标志打开时生效；钩子在 Runbook 登记文件与行号。
- 不改 `UserModel` 的话，至少：
  - 让启动器在作业开始前把 `installTime` 设为沙箱时钟的时刻，并断言 `DailyAdsResetTime` 不晚于它；
  - 断言机器人从不调用 `CanWatchAd`/`IncrementDailyAds`；
  - C3d 的"不同真实日期跑两次"测试要覆盖跨 UTC 零点的情况。

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

## 8. 给 C0b 的交接（已完成，实现见第 10–14 节）

- `ED/SimulationBatch.cs` 的所有权转给 C0b。保留的骨架：
  - `SessionState` 加进程号的跨域重载接续；
  - 就绪判据；
  - 改名、探针与清空顺序；
  - 退出时先 `Save()`、再改回、再 `Exit`。

  测量部分（`ProbeState`）是探路用的，可以删掉，或移到 `selftest`。
- 沙箱用首选方案（改名），不需要 `GameDataService` 的存档目录钩子。
- **沙箱残留要清理。** 探路入口只在开始时清空沙箱，结束时不清，好让验收脚本检查 `game_data.bin` 落在哪里。所以每次运行后会留下 `~/Library/Preferences/unity.TufuSimSandbox.<沙箱名>.plist` 和 `~/Library/Application Support/TufuSimSandbox/<沙箱名>/`。C0b 的启动器在每次运行（成功或失败）之后：
  - 先取走需要的产物；
  - `/usr/bin/defaults delete unity.TufuSimSandbox.<沙箱名>`，经 cfprefsd 删除；若 plist 文件仍在再删文件（直接删文件而不经 cfprefsd，缓存可能把它写回来）；
  - 删除沙箱数据目录；
  - 删除之前核对路径确实在 `TufuSimSandbox` 之下，绝不碰真实域 `unity.Multicast Games.*` 与真实存档目录。
- 时钟钩子不止 `TimeService` 一处，见 7.1a。
- Play 设置守卫只看 `enterPlayModeOptions` 的禁用标志（第 3 节第 7 条）。
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
| 2.3 | `EditorSettings` 断言域重载与场景重载（属性名待核对）| `enterPlayModeOptionsEnabled` 读出 `False`，与资产里的 1 不符，不能用；只看 `enterPlayModeOptions` 有没有 `DisableDomainReload`/`DisableSceneReload`（第 3 节第 7 条）|
| 2.3、2.6、7.3 C0b | 沙箱时钟只在 `TimeService` 加钩子；"读墙钟的系统由沙箱时钟冻结"，输出与真实日期无关 | `UserModel.cs:431`、`:464-465` 直接读 `DateTimeOffset.UtcNow`，绕过 `TimeService`：每日广告上限按真实 UTC 零点重置，`installTime` 取真实时刻并影响 `StatReward` 的每日轮换（沙箱时钟早于真实安装日时会抛异常）。C0b 的钩子清单需要加上 `UserModel`，或用 7.1a 的替代办法 |
| 1.5 第 6 条、2.3 | 资产守卫用 `EditorJsonUtility.ToJson` 比较内存摘要；不同就 `ImportAsset(ForceUpdate)` | `StatBase.Modifiers` 是自动属性，Unity 不序列化它，`ToJson` 看不到 `PlayerConfig.Stats[i].Modifiers` 的改动（它也不会被 `SaveAssets` 落盘，进入 Play 的域重载本身就会丢掉它）。守卫另加了一遍托管字段的遍历（11.4）。每次 Play 之后 Player Config 与 Simulation Config 的内存状态都变了；`ImportAsset(ForceUpdate)` 一次也没有恢复它们，`Resources.UnloadAsset` 后重新加载才恢复 |
| 2.3 AB | 钩子只在抽组处 | `ABTestModel.TryApply` 对老用户在抽组之前就返回（7.2），所以构造函数里的 `isNewUser` 也加了钩子（`ABTestModel.cs:16`）；否则 `prefs_seed` 里有 `game_loaded_once` 时 `Data.Tests` 为空 |
| 7.3 C0b 文件清单 | 钩子只在 `TimeService`、`ABTestModel`、`MainSceneInitializer`（及后备的 `GameDataService`）| 按 7.1a 另加了 `Analytics/UserModel.cs` 的三处时钟钩子（所有权偏离，需要 Z1 放行）；`GameDataService` 没有改（改名方案成立）|
| 2.3 刷怪点就绪 | 补救后断言个数即可 | 另需在设 `IsRunning` 之前把就绪时的 `Cooldown` 清零（请求 `sandbox.zero_ready_cooldowns`，默认开）：沙箱时钟取 2024-01-01T00:00:00Z（UTC 零点）时有 698 个刷怪点的 `Cooldown` 是一整个周期（第 14 节）|
| 2.3、7.3 C0b | `nettop` 采样断言没有外发连接 | 改为三条口径：`lsof` 采样比较 Play 期间与编辑模式期间的非回环远端；Play 期间日志里没有 SDK 输出；游戏与编辑器代码里没有网络 API（静态测试）。系统代理下所有连接都是 `127.0.0.1:7897`，采样看不到主机名（第 13 节）|
| 2.2 第 5 条 | 进程退出后真实存档目录不变 | 编辑器在改名之前就把自己的分析会话写进真实 `persistentDataPath` 的 `Unity/<云项目 id>/Editor/Analytics/`，正常退出时删掉；被杀掉的编辑器会留下它。启动器把这部分单列为 `editor_analytics_leftovers`，从备份恢复，不判退出码 2；游戏存档与 PlayerPrefs 的任何改动仍判 2 |
| 1.4.1、2.4、2.5 | 心跳与队列在 `Temp/TufuSimulation/`，常驻模式的结果放在队列里 | Unity 退出时删除整个 `Temp/`，常驻模式的 `serve_report.json` 因此写到 `Balance/out/sim/serve/<时间>/`。心跳仍在 `Temp/` |
| 7.4 | 作业结束经 `SimulationHarness` 里声明的 partial 钩子进入 `SimulationBakeExport`、`SimulationConfigPatcher` | C# 的 partial 方法只能有一个实现，C1 与 C8 都要导出、C4 与 C9 都要在开跑前改状态，所以每个单元一个钩子：`RebuildJobApplyPatch`（C9）、`RebuildJobApplyOverride`（C4）、`RebuildJobExportOutputs`（C1）、`RebuildJobExportBake`（C8）；请求的校验同理（`ValidateOverrideFields`/`ValidateBakeFields`/`ValidatePatchFields`）|
| 1.4.2 | 请求的核心字段 | 另加了 `unity.ready_timeout_s`、`unity.heartbeat_interval_s`、`sandbox.expected_alive_enabled`、`sandbox.skip_integrations`、`sandbox.zero_ready_cooldowns`、`stop.max_frames`、作业的 `test`（自检动作），以及模式 `empty`（只走框架、不设 `IsRunning`）。`override`、`patch` 留给 C4、C9 追加。`sandbox.identity` 只作产品名，公司名固定为 `TufuSimSandbox` |
| 1.4.3 | 输出清单 | C0b 写 `manifest.json`、`ready_spawners.csv`、`timings.json`、`asset_guard.json`、`unity.log`，以及运行级的 `request.json`、`editor_report.json`、`launcher.json`。状态摘要暂写在 manifest 里（`state_sha256_job_start/end`），逐日的 `digests.csv` 归 C3c |
| 2.2 第 1 条 | DLL 为已知损坏值时替换后照常运行 | 照做，但替换会让运行工作树不干净，运行后的 `git status` 检查会判 2。入库的 DLL 就是期望值，这条路径没有实际触发过 |

## 10. 用法（C0b）

在工作树的 `Balance/` 下执行。单元工作树用开发工作树的解释器 `/Users/joyfort/MyProjects/Tufu-stage5/Balance/.venv/bin/python`，跑的是自己工作树里的 `tufu_balance`。

```bash
python -m tufu_balance.unity selftest [--rev REV] [--runs N] [--nographics]
python -m tufu_balance.unity run (--preset selftest|empty|smoke-11|golden-405|slow-5 | --request R.json) [--rev REV | --dev] [--force]
python -m tufu_balance.unity serve start [--rev REV] [--idle-timeout 秒] | status | submit (--preset P | --request R.json) | stop
python -m tufu_balance.unity open [--rev REV | --dev]       # 交互打开工程，参数与 .ps1 相同
python -m tufu_balance.unity status                         # 锁、运行工作树、Unity 进程、常驻编辑器
pytest --unity tests/unity                                  # 环境变量 TUFU_UNITY_REV、TUFU_UNITY_NOGRAPHICS=1
```

- **前提：抽取好的数据库**。请求的 `sandbox.expected_ready_spawners` 默认是 `"auto"`（C1，见第 14 节），启动器要从 `$TUFU_BALANCE_DB`（没有时 `Balance/out/balance.sqlite`）生成就绪核对表，`selftest` 也一样；数据库不存在时以退出码 5 拒绝。没有数据库又要跑时，在请求里把 `expected_ready_spawners` 显式设为 `null`（不做这项核对）。
- `--rev` 默认是 `tufu_balance` 所在工作树的 HEAD：先提交，再运行。运行工作树检出到该提交（游离 HEAD），检出前后都要求 `git status --porcelain -uall` 为空。
- `--dev` 在当前工作树里跑，要求它有 `Library`（单元工作树没有），结果不进缓存；Unity 退出后工作树的改动状态变了就退出码 7。
- 输出在 `Balance/out/sim/runs/<run_key>/`：`request.json`、`unity.log`、`sandbox.json`（编辑器改名进沙箱、探针通过后立即写：沙箱段与编辑器进程号）、`editor_report.json`（编辑器侧：改名探针、守卫基线、各作业结果，退出时写）、`launcher.json`（启动器侧：命令、耗时、真实存档核对、网络采样、日志扫描），以及每个作业的 `<job_id>/{manifest.json, ready_spawners.csv, asset_guard.json, timings.json, unity.log}`。作业目录里除 `timings.json` 外的文件和 `request.json` 可以逐字节比较；`sandbox.json`、`editor_report.json` 带编辑器进程号，`launcher.json` 带进程号、时间和耗时，`unity.log` 带时间，这几个不能逐字节比较。
- 同一 `run_key` 已有退出码 0 的结果时直接复用，`--force` 重跑。`run_key` 的组成见设计稿 1.4.2；只影响速度的 `simulation_speed`、`frame_budget_ms`、`heartbeat_interval_s`、`ready_timeout_s` 不计入。
- 预设 `smoke-11`、`golden-405`、`slow-5` 自 C3b 起会推进时钟、换天、拍快照；策略模块（C5、C6、C7）填好之前机器人不打怪（第 16 节）。
- Python API：`tufu_balance.unity.launcher.run(request, rev=…)`、`selftest()`、`serve_*()`；请求由 `tufu_balance.unity.request` 生成和校验（`make_request`、`make_job`、`empty_job`、`preset`、`validate`）。其它单元追加请求字段时用 `request.register_fields`，C# 一侧写自己的 `SimulationRunRequest.<单元>.cs`。
- 差分探针（C2）：`tufu_balance.unity.probes.probe_job()` 生成一个带 `probes` 字段的 `empty` 作业，编辑模式探针在进入 Play 之前、Play 模式探针在作业结束时运行；`pytest --unity tests/unity/test_differential.py`。见 [DifferentialProbes.md](DifferentialProbes.md)。

退出码（C# 与启动器共用）：

| 码 | 含义 | 由谁给出 |
|---|---|---|
| 0 | 成功 | |
| 1 | 未捕获的异常 | 运行时或编辑器 |
| 2 | 守卫失败：资产在内存或磁盘上没能恢复、真实存档或 PlayerPrefs 变了、运行工作树不干净、进入 Play 的设置关掉了重载、PlayerPrefs 探针落进真实域、宠物没能关闭 | 编辑器或启动器（启动器的判定覆盖 Unity 的退出码）|
| 3 | 超时：启动后没有心跳（默认 600 秒）、心跳停止前进（默认 120 秒）、超过总时限 | 启动器（杀掉进程组），编辑器的作业硬时限 |
| 4 | 游戏未就绪（`ready_timeout_s`）、运行时一侧没有起来、商店 RPM 一直不空闲 | 运行时 |
| 5 | 请求无效：解析失败（含未知字段）、校验不过、常驻模式下 `code_rev` 或 `identity` 不符 | 编辑器或启动器 |
| 6 | 自检失败：冻结期间玩家血量或 `RPMCalculatedValue` 被其它系统改了 | 运行时 |
| 7 | `--dev` 运行期间工作树被改动 | 启动器 |
| 8 | 就绪核对失败：AB 分组、`expected_ready_spawners` 表、`AnyoneAlive && IsEnabled` 个数 | 运行时 |

## 11. 流程

### 11.1 编辑器一侧（`ED/SimulationPlayModeDriver.cs`）

1. `SimulationBatch.Run/SelfTest/Serve` 读参数，Newtonsoft 解析请求（未知成员报错），校验；失败以 5 退出，`editor_report.json` 写在请求文件旁边。
2. 进入 Play 的设置：只看 `enterPlayModeOptions` 里有没有 `DisableDomainReload`/`DisableSceneReload`（第 3 节第 7 条），有就以 2 退出。
3. 资产守卫取基线（首次进入 Play 之前）。
4. 改名进沙箱：公司名固定为 `TufuSimSandbox`，产品名取 `sandbox.identity`（默认也是 `TufuSimSandbox`）；断言 `persistentDataPath` 已变，写一个探针键经 `defaults read` 确认它在沙箱域、不在真实域，否则以 2 退出。通过后立即把报告里的沙箱段连同本编辑器的进程号写进 `<out_dir>/sandbox.json`（`tufu.sim.sandbox/1`；运行模式的 `out_dir` 是运行目录，常驻模式是 `Balance/out/sim/serve/<时间>/`），启动器在编辑器还开着时就能据此判断沙箱已生效（第 13 节）。
5. 每个作业：清空沙箱存档目录和 PlayerPrefs，写入 `prefs_seed`（整数 → `SetInt`，小数 → `SetFloat`，字符串 → `SetString`）并 `Save()`；写 `Temp/TufuSimulation/active_request.json`（进程号、请求路径、作业序号）；从 Boot 进入 Play；等运行时一侧完成；退出 Play；资产守卫核对与恢复，写 `asset_guard.json`；把编辑器侧的耗时并进 `timings.json`。作业失败就结束整个请求。
6. 结束：`PlayerPrefs.Save()`（仍在沙箱域）、改回名字并清掉 PlayerSettings 的脏标记、删除 `active_request.json`、写 `editor_report.json`、`EditorApplication.Exit(code)`。

状态放在 `SessionState`，进入 Play 的域重载之后由 `[InitializeOnLoad]` 的静态构造接续；存的进程号与当前进程不符时一律不动作。

### 11.2 运行时一侧（`CB/Simulation/Harness/`）

- `SimulationSandbox`：`SubsystemRegistration` 时读 `active_request.json`，进程号相符才打开沙箱标志，记下请求、作业、时钟；`BeforeSceneLoad` 时建一个隐藏的 `SimulationHarness` 对象（`DontDestroyOnLoad`，退出 Play 时随之销毁）。这两个方法只编进编辑器（`#if UNITY_EDITOR`），播放器构建里沙箱标志永远是关的。
- `SimulationHarness` 每帧推进一个阶段：
  1. **WaitReady**：判据同 C0a（`SimulationSystem.Instance`、`EnemiesModel.Spawners` 非空、两个待自举队列为空，保持 1 秒），超过 `ready_timeout_s` 退出码 4。
  2. **就绪**：沙箱报告（存档路径、PlayerPrefs 域、`TimeService.Now`、`CheatOffset`、预置键的读回值、宠物）；AB 核对；就绪补救（第 14 节）；写 `ready_spawners.csv` 的内容；与 `expected_ready_spawners`（有就核对）和 `expected_alive_enabled`（默认 699）比较，不符退出码 8。然后 `Time.timeScale = 0`、`config.DisableUi = true`。
  3. **Settle**：至少 10 帧，并且商店的 RPM 计算空闲（`ShopLotRpmCalculationSystem.isProcessing` 为假）。
  4. **Quiet**：再冻结 30 帧，前后比较玩家血量与 `RPMCalculatedValue`、`SoulRPMCalculatedValue` 的 IEEE 位，变了退出码 6。
  5. **Job**：应用 `config_overrides`（只允许白名单里的运行开关）、停止条件、`SimulationSpeed`；把就绪时的 `Cooldown` 清零（第 14 节）；记下作业开始的状态摘要；依次调 `RebuildJobApplyPatch`（C9）、`RebuildJobApplyOverride`（C4）；`empty` 以外的模式设 `IsRunning = true`；执行 `test` 里的自检动作。停止原因：`simulation_stopped`（模拟自己停了）、`max_ticks`、`max_frames`、`max_wall_seconds`；`empty` 默认 30 帧后停。每帧按上一帧的耗时调整 `SimulationSpeed`，范围 1 到请求值的 16 倍。
  6. **Export**：`IsRunning = false`；`empty` 作业再核对一次血量与 RPM；记下作业结束的状态摘要；依次调 `RebuildJobExportOutputs`（C1）、`RebuildJobExportBake`（C8）；写 `manifest.json`、`ready_spawners.csv`、`timings.json`。
- 给后面单元的接口：
  - `SimulationHarness.OnTickCompleted()`：C3b 的 `RebuildOnTickCompleted` 每个 Tick 调一次；它累加 Tick 数、写心跳，达到 `max_ticks` 时返回真（`StopRequested`），重建的 `ExecuteTick` 应当据此停下。
  - 上面四个 partial 钩子，参数是作业、作业目录和 manifest 的 `JObject`（可以往里加字段）。
- 状态摘要（`state_sha256_job_start/end`）：36 项属性的 `Value` 与 `SpecificValue`、血量、物品 (Key, Level, Copies, Evolved)、当前与已解锁的岛、RPM 字典、每个刷怪点的 (Key, ChargesLeft, Cooldown, 单位数)、AB 分组，全部按位、按键排序后取 sha256。P、T 等引擎状态等 C3b/C3c 补进逐日摘要。

### 11.3 心跳

- 文件 `Recovery/StudyProject/Temp/TufuSimulation/heartbeat.json`：`pid`、`phase`、`job_id`、`progress`、`ticks`、`frames`、`utc`。Unity 退出时会删掉整个 `Temp/`。
- 运行时在 `Update` 和 `OnTickCompleted` 里按 `heartbeat_interval_s`（默认 5 秒）写；编辑器在编辑模式下（作业之间、常驻空闲）写。
- `progress` 只在作业、阶段、Tick 数、帧数（编辑模式下是编辑器的 update 计数）变化时才变；**作业阶段（Job）只看 Tick 数**（C3b 修正：原先帧数也算进去，帧在走而模拟不 Tick 的作业永远不会被判停顿）。启动器看的是 `progress`：一帧再长，只要 Tick 在走就不算停；不动才判超时（退出码 3）。验收：一帧 12 秒、一直调 Tick 钩子的作业，6 秒的停顿超时没有触发；一帧 60 秒不调钩子的作业在 8 秒停顿后被杀，退出码 3。

### 11.4 资产守卫（`ED/SimulationAssetGuard.cs`）

- 受保护资产：Simulation Config、Simulation Data、Combat Balance Config、Player Config、ABTest Data Config、Locations Config、`Assets/Simulation/Simulation Rebuild Params.asset`（C3c 建之前跳过）。
- 内存摘要 = 每个对象的 `EditorJsonUtility.ToJson`，加上一遍托管字段的遍历：基本类型、字符串、列表和 `[Serializable]` 类型的字段（含自动属性的后备字段，如 `StatBase.Modifiers`），跳过 UniMob 的原子与缓存、委托、指针、字典。另记磁盘文件的 sha256。
- 每个作业退出 Play 后与基线比较：不同就 `ImportAsset(ForceUpdate | ForceSynchronousImport)`，仍不同就对其中的对象 `Resources.UnloadAsset` 再重新加载；还不同、或磁盘文件变了，就以 2 结束。结果写进 `asset_guard.json`（变了的资产、恢复方式、变化后的摘要）。下一个作业紧接着进入 Play，中间没有别的代码运行，所以这一步就是"进入 Play 之前的核对"。
- 实测：每次 Play 之后 Simulation Config（`OnAwake` 改写，第 8 节）和 Player Config（`PlayerModel` 构造时把资产里 `Stats[i].Modifiers` 的列表交给活的属性）在内存里都变了；`ImportAsset` 一次也没能恢复，全部靠 `UnloadAsset` 恢复。每个作业约 0.14 秒。
- 自检作业：`test.mutate_player_config` 在 Play 里往 `PlayerConfig.Stats[0].Modifiers` 加一个修饰器，守卫看到的 Player Config 摘要与普通作业不同，恢复后下一个作业的状态摘要与新开的 selftest 相同；`test.skip_reimport` 让守卫不做恢复，结果是退出码 2。
- `StatBase.Modifiers` 不被 Unity 序列化：进入 Play 的域重载本身就会丢掉它，`SaveAssets` 也不会把它落盘。

### 11.5 常驻模式

- `serve start`：取锁、检出、记下真实存档基线和此刻主检出上开着的编辑器进程号（`state.json` 的 `main_editors`）、在 `Temp/TufuSimulation/queue/{inbox,running,done}` 建队列，启动 `SimulationBatch.Serve`，把锁的持有者改成 Unity 的进程号（CLI 返回后锁仍在），等心跳进入 `edit:idle`。
- `serve submit`：按常驻编辑器的提交计算 `run_key`，把请求写进 `inbox/`，等 `done/<名>.result.json`；然后核对一次真实存档（忽略编辑器自己的分析文件），变化的归属规则见第 13 节。
- 编辑器按文件名顺序取请求，每个作业仍是 `reenter_play`，作业后的守卫照常；`code_rev` 或 `identity` 与常驻编辑器不同的请求以 5 拒绝。收到 `stop` 文件、空闲超时（默认 30 分钟）或守卫失败时退出。
- `serve stop`：写 `stop`、等进程退出、做进程退出后的全部核对、清理沙箱、释放锁。编辑器的报告写在 `Balance/out/sim/serve/<时间>/serve_report.json`（`Temp/` 会被删掉）。
- 验收：常驻编辑器里两个相同的 `empty` 作业，状态摘要和 `ready_spawners.csv` 彼此相同，也与冷启动的 selftest 相同。

## 12. 沙箱钩子清单

游戏代码里的钩子都写成"标志关时原样执行"，并由 `tests/test_unity_launcher.py` 检查：每一处都在本表登记了文件与行号，并且都是 `SimulationSandbox.Active ? 沙箱值 : 原表达式` 或 `!Active` 守着的跳过。

| 文件:行 | 标志打开时 | 标志关闭时 |
|---|---|---|
| `Services/Time/TimeService.cs:23` | `Now` 取 `sandbox.clock_epoch_utc`，固定不动 | `DateTime.UtcNow + 偏移` |
| `Services/Time/TimeService.cs:32` | 不读 PlayerPrefs 的 `TimeSkipSeconds`，偏移为 0 | 原样读取 |
| `ABTests/ABTestModel.cs:16` | 一律按新用户处理（不看 `game_loaded_once`）| 原样 |
| `ABTests/ABTestModel.cs:39` | 抽组返回请求的分组（不在可选分组里时取第一个），不调 `UnityEngine.Random` | 原样随机 |
| `Analytics/UserModel.cs:431` | 每日广告重置按沙箱时钟的 UTC 零点 | 真实 UTC 零点 |
| `Analytics/UserModel.cs:464` | 新档的 `installTime` 取沙箱时钟 | 真实时刻 |
| `Analytics/UserModel.cs:465` | `daysSinceInstall` 按沙箱时钟 | 真实时刻 |
| `Core/MainSceneInitializer.cs:205` | `sandbox.skip_integrations` 为真时跳过 `BackendApi.Init`、`HubBackendBootstrap.TryLogin`（`:206-207`）和用户同意（`:223`）| 原样 |
| `Core/MainSceneInitializer.cs:253` | 同上，跳过 `ApplyConsent`、`BootTimeEvent`、Adjust、Firebase、`CollectDeviceIdentifiers`（`:255-260`）| 原样 |

路径相对 `CB` = `Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase`。没有改 `GameDataService`（改名方案成立，不需要存档目录钩子）。`Boot.cs:119` 的 `GameLoadedEvent` 不在 C0b 的文件范围内，没有跳过（`Send` 是空桩，第 4 节）。宠物：本分支没有 `PetConfig`，沙箱报告 `absent`；合并宠物以后，`SimulationSandbox` 用反射把找到的 `PetConfig` 实例的 `Enabled` 设为假并报告 `disabled`（这条路径还没有在真实数据上跑过）。

## 13. 启动器的检查（`tufu_balance/unity/launcher.py`）

- 拒绝主检出 `/Users/joyfort/MyProjects/Tufu/Recovery/StudyProject`（`--allow-main-checkout` 才放行）。
- 锁：`<工作树>/Balance/out/unity.lock`，JSON 里记持有者进程号；持有者已不在就视为过期接管；被占时排队（默认最多等 1 小时），不抢。常驻模式和 `open` 把持有者改成 Unity 的进程号。
- 用不区分大小写的 `pgrep -fil <工程路径>` 再筛 `-projectpath`，发现已有 Unity 打开同一工程就拒绝。
- 受保护 DLL 按 `.ps1` 的规则核对，常数与入库的 `.ps1` 一致（测试）。
- 运行前后核对（Unity 和它的导入进程都退出之后）：真实存档目录下每个文件的 sha256；真实 plist 文件与经 cfprefsd 导出的内容（`defaults export`）；运行工作树里受保护资产、`ProjectSettings.asset`、`EditorSettings.asset`、`TimeManager.asset` 的 sha256；`git status --porcelain -uall`。运行前把真实存档目录和 PlayerPrefs 备份到运行目录的 `_real_backup/`。
  - 有变化时：运行期间没有发现别的 Unity 进程才从备份恢复（另一个编辑器可能正当地写了它们），否则只报告；两种情况都以 2 结束（例外见下面两条）。只在没有变化或已恢复时删除备份，其余情况都保留。
  - 例外是编辑器自己的分析会话 `Unity/<云项目 id>/Editor/Analytics/**`（第 9 节）：单列为 `editor_analytics_leftovers`，照样从备份恢复，不判 2。
  - 用户在主检出的编辑器里游玩时，大约每 5 秒重写一次真实存档（可能还有 PlayerPrefs）。下面几条同时满足时，这些变化记在用户的编辑器名下：列为 `real_changed_by_main_editor`，不恢复（那是用户自己的数据），也不判 2，备份照样保留在 `_real_backup/`。
    - 运行开始前和结束后都看到主检出上的同一个 Unity 编辑器进程（`main_checkout_editors`，导入进程不算）。
    - 本次运行的 Unity 正常退出（退出码 0），运行期间没有别的 Unity 打开运行工作树。
    - 本次运行的 `editor_report.json`（进程号必须等于本次启动的 Unity）证明沙箱生效：`persistentDataPath` 在 `TufuSimSandbox/<名>` 下，不在真实路径之下；它报告的真实路径就是受保护的存档目录；PlayerPrefs 探针只落在沙箱域。
    - 除了真实存档和 PlayerPrefs，没有别的守卫问题：工程文件或运行工作树有变化时，照旧连同真实存档的变化一起判 2。

    任何一条不满足就和以前一样判 2；只要有别的编辑器在，就不会去恢复。这个探针只证明从改名到 `LeaveSandbox` 这段时间的写入进了沙箱；改名之前，编辑器启动时只写分析会话（上一条）。所以把变化归给主编辑器的依据是：本次运行在这段时间外不写存档，而那个编辑器确实开着。
  - 常驻模式（`serve`，P10）用同一组条件，在 `serve submit` 和 `serve stop` 时各判一次：
    - `serve start` 时记下的主检出编辑器（`main_editors`）此刻仍开着（同一进程号）；
    - 干净：`submit` 要求这个请求的退出码与它每个作业的退出码都是 0；`stop` 要求常驻编辑器以 0 退出，并且它服务过的每个请求及其每个作业都是 0；
    - 除常驻编辑器自己（和导入进程）外没有别的 Unity 打开运行工作树；
    - 常驻编辑器的 `serve/<时间>/sandbox.json`（进程号必须等于常驻编辑器，即 `state.json` 的 `pid`）证明沙箱生效，判据同上；`editor_report.json` 要到退出时才写，所以常驻模式读的是进入沙箱后立即写下的 `sandbox.json`；
    - 除了真实存档和 PlayerPrefs 没有别的守卫问题。

    满足时记为 `real_changed_by_main_editor`（`launcher.json` 与 `serve stop` 的输出），不判 2、不恢复，`serve/<时间>/_real_backup/` 保留到人工处理。不满足时照旧判 2；只要主检出上有编辑器，`serve stop` 就不恢复。比较的基线一直是 `serve start` 时的快照，所以用户的编辑器写过一次存档以后，之后每次提交都会重新判一次，结论各自记在该次的 `launcher.json` 里。运行模式的规则不变。
- 网络：运行中每 2 秒 `lsof -nP -a -p <pid> -i` 采样，按心跳的阶段分成编辑模式与 Play；断言 Play 期间没有编辑模式期间之外的非回环远端。另扫描日志里第一次进入 Play 之后的 SDK 输出（AppMetrica、Adjust、Firebase、AppLovin、MaxSdk、Facebook、Tapjoy、BackendApi、UnityServices、RemoteConfigService），并有静态测试断言 `Assets/Scripts` 与 `Assets/Editor` 里没有网络 API。系统代理（`127.0.0.1:7897`）下 `lsof` 只看得到代理端口，看不到主机名；按主机名的核对仍需代理日志（第 4.3 节）。
- 每次运行前后删除沙箱的 PlayerPrefs 域（`defaults delete unity.TufuSimSandbox.<名>`，再删残留的 plist 文件）和沙箱存档目录；删除前核对路径确实在 `TufuSimSandbox` 之下。

## 14. 刷怪点就绪与冷却（C0b 实测）

- 补救按 6.2 节的建议实现：对每个非活动、非 VIP、`!IsBootstrapComplete` 的刷怪点调 `CompleteBootstrap(now)`（`internal`，不用反射），对 `!IsEnabled` 的调 `SetEnabled(true)`。新档上两者都是 0 个，耗时约 0.02 毫秒。
- 补救后 `AnyoneAlive && IsEnabled` = **699**（冻结值，请求默认 `expected_alive_enabled = 699`）；例外 17 个：活动 5、VIP 10、`Spawner_Dummy_b310…`、`BossTower_c296…`。注册 716 个、场景里 717 个组件、敌人 1015 个。三次 selftest、`TimeSkipSeconds = 259200` 的运行和常驻模式里都相同。
- 就绪时的 `Cooldown`：沙箱时钟 2024-01-01T00:00:00Z 恰好是 UTC 零点，所以满员刷怪点的 `Cooldown` 是一个整周期（到下一个对齐时刻）而不是 0。作业开始、设 `IsRunning` 之前，把非活动、非 VIP 刷怪点的非零 `Cooldown` 清零：698 个（`ready_cooldowns_zeroed`）。`ready_spawners.csv` 记的是清零之前的值。Q07 的决定见第 16.4 节：引擎在 `PrepareSimulationRun` 里自己把冷却清零，这一步保留、默认仍打开，结果与关掉它逐字节相同。
- `expected_ready_spawners`（P6 的表）：列 `key`、`charges_total`、`base_cooldown`，可选 `path`（有 `path` 时按"层级路径|Key"对应，能区分重复的 Key）。核对的代码用 selftest 自己的表验证过：全部 716 个相等时通过，改一个 `BaseCooldown` 就以 8 结束。**C1 接上了 P6**：请求里 `sandbox.expected_ready_spawners` 默认是 `"auto"`，启动器在算 `run_key` 之前把它换成 P6 的 `expected_ready_spawners(ab_variant)` 经刷怪点身份映射写成的表 `ready_spawners.expected.<内容哈希>.csv`，放在 `request.json` 旁边；716 个全部相等（黄金套件 `export` 的 `ready_gate.*`），见 [ExportIngest.md](ExportIngest.md)。
- AB：请求 A 时 13 个测试里 11 个被分配，全是 A（`level_pass`、`rewarded_delay_from_start` 没有规则）；`fastRPMParams` 生效值 (10, 1)；`spawner_cooldowns_v3` 的 A 组表（非精英默认 30 分钟、岛 1–13 为 4–28 分钟、3 次充能；精英默认 180 分钟、岛 1–4 为 60–150 分钟、2 次充能）记在 manifest 里。

## 15. 实测耗时与验收记录

运行工作树检出 C0b 的提交，有图形，Library 已就绪：

| 运行 | 进程总耗时 | 说明 |
|---|---|---|
| selftest（3 次）| 10.7–11.4 秒；换提交后的第一次 14.2 秒 | `E` 2.4–2.5 秒（换提交后 5.8 秒，含编辑器程序集编译）、`B` 5.09–5.13 秒、`X` 0.11 秒；守卫约 0.14 秒 |
| 两个作业（`reenter_play`）| 15.8 秒 | 第二个作业多约 5 秒 |
| 常驻模式提交两个作业 | 12.0 秒 | 不含编辑器启动 |
| 一帧 12 秒的心跳作业 | 22.8 秒 | |
| 停顿超时（8 秒）| 17.1 秒 | 退出码 3 |
| 未就绪（`ready_timeout_s = 1`）| 7.0 秒 | 退出码 4 |
| 未知字段 | 4.9 秒 | 退出码 5，进 Play 之前 |

`pytest --unity tests/unity` 全部约 3 分钟。`-nographics` 与有图形时的 selftest 输出逐字节相同（manifest 里不放图形设备，图形设备和 `batchmode` 记在 `timings.json`）。

验收对照（设计稿 7.3 C0b）：

| 项 | 结果 | 测试 |
|---|---|---|
| selftest 连续 3 次成功 | 通过 | `tests/unity/test_headless_smoke.py::test_selftest_three_times_in_a_row` |
| 真实存档、plist、`ProjectSettings.asset`、受保护资产不变，运行工作树干净 | 通过（每次运行都核对）| `test_sandbox.py::test_real_save_prefs_and_project_unchanged` |
| 日志里的存档路径与 PlayerPrefs 域在沙箱名下 | 通过 | `test_save_and_prefs_paths_are_sandboxed` |
| 沙箱时钟；`TimeSkipSeconds = 259200` 时就绪表与摘要不变 | 通过 | `test_sandbox_clock`、`test_time_skip_and_shared_prefs_do_not_change_the_state` |
| AB 分组与生效表 | 通过；P6 的表未到，只导出（核对代码已用合成表验证）| `test_ab_groups_fixed_to_the_request`、`test_expected_ready_spawner_table_gate` |
| 补救后个数等于冻结值 699，例外按 SpawnerId 列出 | 通过 | `test_ready_spawners_after_remedy`、黄金套件 `harness` |
| 分析与集成跳过，没有外发连接 | 通过（按第 13 节的口径）| `test_integrations_skipped_and_no_outbound`、`test_no_network_api_in_game_code` |
| 宠物 `absent` | 通过 | `test_pets_absent` |
| 冻结期间血量与 RPM 不变 | 通过 | `test_health_and_rpm_untouched_while_frozen` |
| 两次启动的注册顺序摘要相同 | 通过（`2c531b36…`）| `test_registration_order_and_ab_stable` |
| 资产守卫检测并恢复 Player Config 的修饰器 | 通过（靠 `UnloadAsset`）| `test_asset_guard_restores_a_mutated_player_config` |
| 请求往返；未知字段报错 | 通过 | `test_request_round_trip_through_csharp`、`test_unknown_field_fails_in_csharp_with_exit_5`、`tests/test_request_roundtrip.py` |
| 心跳：长帧不误判，停顿才超时 | 通过 | `test_long_frame_with_ticks_is_not_a_stall`、`test_stalled_frame_times_out_with_exit_3` |
| 启动器：主检出、脏工作树、`--dev` 退出码 7、`pgrep -fi`、DLL 常数、`run_key` 敏感性、退出码 2/3/4/5/7/8 | 通过（7 与部分 2、3 用假编辑器）| `tests/test_unity_launcher.py`、`test_sandbox.py::test_exit_codes` |
| 常驻模式两个相同作业摘要相同 | 通过 | `test_serve_mode_reenter_play_two_identical_jobs` |

没有做的：可选的 `ED/SimulationRunnerWindow.cs`（交互窗口、单步）。

## 16. 引擎（C3b）

C3b 填了 `SimulationSystem.Run.cs`、`.Lifecycle.cs`、`.Snapshot.cs`、`.Amulet.cs`、`.Rebuild.cs`，`.FastTick.cs` 的最小 `FastTick`、`.Fight.cs` 的调用点骨架，`SimulationConfig` 的 `Tick` 与 `GetTrimmedTimeChunkSnapshots`，以及参数类核心字段 `Rebuild/SimulationRebuildParams.cs`（默认值同 `Balance/sim/params/defaults/engine.json`）。测试在 `Balance/tests/unity/test_engine.py`、`tests/test_engine_static.py`，黄金套件 `sim_engine`。

### 16.1 请求与 manifest

- 作业字段 `engine_probe`（`Harness/SimulationRunRequest.Engine.cs`，Python 侧 `tufu_balance.unity.engine`），只给测试用，正式运行里不出现（为 null 时 C# 回显也省略它，C0b 的请求往返测试不变）：
  - `null_policy`：不调用 `FindBestSpawner`，快阶段固定用最小 FastTick（`RebuildAdvanceFastDay(0)`）。C5、C7 填好策略以后，引擎自身的检查照样有意义；
  - `unlock_through: N`：开跑前经 `RebuildUnlockLocation` 依次解锁 2..N 岛（测全部岛屿解锁时的每 Tick 成本）；
  - `write_points`：`PrepareSimulationRun` 之后跑一遍写入点场景（16.5 列出各步），结果写进 `manifest.engine.probe`，然后停下；
  - `grant_flat: {StatType: Value}`：在第 0 天的 K 修饰器之前给玩家加平坦修饰器（来源 `Reward`）。测试用 `CutDamageFlat`、`CutDefenceFlat` 各 10，让 DpsData/DefenseData 随 K2 变化。
- 参数：作业的 `params` 对象按字段覆盖代码默认值，未知字段报错（退出码 5）。交互运行（没有批处理）用代码默认值。参数资产与完整的请求覆盖归 C3c。
- manifest 新增顶层键 `engine`（`free` 等非 `empty` 作业才有）：生效参数、`null_policy`、`Provenance` 枚举的整数值、停止原因、失败信息、P/T 的 float 位、天序号、插值修饰器对应的天、已解锁岛、`currentBoss`、每个岛的 `RebuildFindBoss` 结果（"层级路径|Key"）、计数器（`slow_ticks`、`fast_ticks`、`idle_steps`、`afk_calls`、`day_changes`、`snapshots_early`、`snapshots_day`、`clears`、`kills`、`accumulate_calls`、`unlocks`、`repeat_unlocks`、`back_gate_noops`、`slot_unlocks`、`dafc_clamped`、`ads` 等，没发生的不写）、逐日记录 `days`（天、是否快阶段、Tick 数、离线步数、离线分钟、换天时的 P 与 T）、慢阶段每次离线 `offline`（序号、离线时的 P、离线后的 T、下一个会话的结束点）、`unlocks`。
- `timings.json` 新增 `slow_tick_ms`、`fast_tick_ms`（样本数、均值、p50、p95、最大值）：同一帧里相邻两次 `OnTickCompleted` 的间隔，每帧第一个 Tick 不计。
- 引擎失败（参数无效、停用模块被打开、`ExecuteTick`/`PrepareSimulationRun` 抛异常）时设 `RebuildFailure`，`SimulationHarness` 在下一帧以对应退出码结束作业：参数与停用模块是 5，异常是 1。

### 16.2 实现要点

- **时钟**：P、T 都是 float32，每一步写成 `(float)(x + m)`。慢阶段没有目标时空闲到下一个早期快照的到期时刻，最多 `TimeStep/60`（5 分钟）；到期规则写成 `P ≥ last + interval(P)`（`interval` 取 P06 的分段，P < 20 时 1 分钟，之后 5 分钟），与"`P − last ≥ interval`"数学上等价，但不会因舍入出现零步长。会话：`P ≥ lifeCyclePeriodEnd` 时离线 230 分钟，`lifeCyclePeriodEnd = P + 10`（G22）。快阶段：`RebuildAdvanceFastDay` 冲刷、加活跃分钟，然后每步 120 分钟离线直到天序号变化。
- **换天**只在 `UpdateSpawnerChargesChunk`：天序号是 `Mathf.Floor((float)(T / 1440))`；用量清零、冷却清零（`+∞` 除外）、`RpmFirst`（`RecalculateChunkRpmValues` → `UpdateInterpolationStatModifier` → 停用模块 → 强制快照 → `RebuildApplyDayOverride`）。覆盖在强制快照之后，与第 0 天相同（见 16.6）；覆盖之后 `RPMCalculatedValue` 不会重算，C4 的覆盖如果要让当天的 RPM 反映覆盖后的状态，要在覆盖里自己调用 `CalculateRPMValueTimeChunk`（每个 Tick 开头的 `CalculateSimulationData` 已经会重建战斗数据）。`ModifierFirst` 先换修饰器；`StaleTickCache` 不重算 DpsData/DefenseData，只用 GPM 模式重建 `spawnerData` 再算 RPM（最小 FastTick 下 DpsData 停在最后一个慢阶段 Tick 的值）。
- **快照**：`Value` 是去掉两个 `__simulation*` 修饰器和装备（`Item`）修饰器后按 `StatBase.CalculateValue` 原顺序重算的值；SFF/PM 是修饰器当时的 K2/K3；`TotalFlatValue = Value·(1+K2)`，直接属性 `TotalValue = TotalFlatValue·K3`（G11 的顺序，见 16.6）。早期列表的 CurrentGpm 是对上一个早期快照的 ΔValue/ΔP（ΔP = 0 为 NaN，第一个为 0），天列表是 `RPMCalculatedValue`。每个快照追加后调用 `SimulationExport.RecordSnapshot`（槽数、当前血量、物品的 `Evolved` 与 `EvolutionLevel`；未进化时 `EvolutionLevel` 记 0），`PrepareSimulationRun` 调用 `ResetRecorder`。导出按 `GetTrimmedTimeChunkSnapshots` 裁剪（第 0 天起 `max(i+1, ceil((float)(1.1f·i)))`）。
- **写入点** `RebuildApplyClear(spawner, clears, accumulate)`：次数先截到剩余充能允许的次数——与机器人过滤一致，只要 `用量 < 上限` 就还能清一次（最后一次可以越过上限），即 `ceil((上限 − 用量)/Count)`，多出来的记 `charge_clamped`，截成 0 时什么也不做；然后用量 `+= Count·clears`（不超过上限），冷却 `= BaseCooldown`，DAFC 次数截成 1、冷却 `+∞`；立即模式发属性（`(float)(Value·Count·clears)`，经 `Stat.AddModifier`，平坦 Health 同时加当前血量）、物品（`GetSpawnerItemRewardCopies` 的份数记进 `RebuildPendingItemCopies`，然后 `ProcessInventory`）、灵魂与货币（只记录）、门禁；累积模式只调用 `AccumulateFastTickSpawnerRewards`。
- **门禁与解锁**：刷怪点的每个 Gate 只开一次（`GatesModel`），开出的岛去重后逐个处理：岛号不大于已解锁岛的（回头门：岛 4→1、11→8、12→7、13→2，或指向已开岛的前进门）记 `back_gate_noops`，不做事；其余调用 `RebuildUnlockLocation`。`SlotReward` 在同一处打开对应槽位。`RebuildUnlockLocation(n)`：`UnlockLocation`、`AdvanceBossProgressLocation`、`currentBoss = RebuildFindBoss(n)`、`UnlockSimulationWeaponSlot(n)`；n 不大于已解锁岛（`UnlockLocation` 取最大值，所以就是"已开放"）时记 `repeat_unlocks`、不做事。门禁不会走到这一支，只有直接调用（C4 的 unlock 覆盖、探针）会。
- **槽位只随门禁刷怪点打开**：直接调用 `RebuildUnlockLocation`（C4 的 unlock 覆盖、探针的 `unlock_through`）不会打开 `weapon_2`、`helmet_1`、`shield_1`；`all-islands` 作业解锁到 17 岛，武器槽仍是 1。C4 的 unlock 覆盖要自己打开对应的 `SlotReward` 槽位，C6 不能假定解锁就有槽位。
- **状态重置**：`PrepareSimulationRun` 把 4.2 里引擎、位置、策略那几行重置为 `OnAwake` 的值（`currentPosition`、`statsBenefit` 全 1、`itemBenefit`、`lastRewardStat`、`PathSpawners`、`statBenefitCandidates`，以及时钟、计数器、充能、冷却、快阶段累积、RPM 字典、`currentBoss`）。`PlayerModel`（属性修饰器、血量、物品、槽位、已解锁岛）和 `GatesModel` **不在这里重置**，靠每个作业一个新的 Play 会话（`reset_mode = reenter_play`）拿到新档；C4 的覆盖在 `StartJob` 里先于 `PrepareSimulationRun` 改这些状态，所以这里也不能重置它们。同一会话里连跑作业要靠 S1：`SimulationHarness.StartJob` 在作业开始的状态摘要之前调用 `RebuildTryResetForJob()`（S1 不在时返回 false）；返回 false 而 `reset_mode` 不是 `reenter_play` 时作业以退出码 5 失败（目前请求校验已经先拒绝这种 `reset_mode`，S1 放开校验时调用点已就位）。
- **`RebuildFindBoss(n)`**：注册顺序里第一个在岛 n、带指向 n+1 的门禁的刷怪点；多个时取第一个并记 `boss_ambiguous`（随包数据里没有）。
- **停止**：`CheckSimulationStopConditions` 在每个 Tick 末尾和 `PrepareSimulationRun` 末尾检查 `StopAtDay`（天序号 ≥ 目标日，即目标日的强制快照之后）与 `StopAtLocation`；停下后 `ExecuteTick` 第一行返回，所以一帧里剩下的 Tick 不再执行。`max_ticks` 只设停止标志，不改 `IsRunning`，停止原因仍是 `max_ticks`。
- **停用模块**：`AutoGambling`、`CrystalSimulation`、`AutoBalanceAmulet`、`GraphSimulation` 任一打开，作业以 "not supported" 失败（退出码 5）。
- **交给后续单元的约定**：`.FastTick.cs` 交给 C5，其中 `SimulateFightWithSpawnerFast` 与 `FlushFastTickChunkRewards` 是按 4.5 契约写的骨架（`Provenance.Null`），由 C5 定稿；`.Fight.cs` 交给 C7，`SimulateFightWithSpawner` 只放了 `RebuildApplyClear(…, 1, false)`；C6 的 `ProcessInventory` 消费并清空 `RebuildPendingItemCopies`（写入点与冲刷都只往里放份数）。

### 16.3 每 Tick 耗时（空策略）与预算

运行工作树、有图形、`SimulationSpeed = 500`。整个第 0–30 天的空策略作业 91 个 Tick 在一帧里跑完，约 0.06 秒。

| 状态 | 慢阶段 Tick（p50 / 均值 / p95）| FastTick（p50 / 均值 / p95）| 说明 |
|---|---|---|---|
| 只开岛 1（`engine-30`，n = 64 / 26）| 0.32 / 0.53 / 0.70 毫秒 | 0.37 / 0.40 / 0.43 毫秒 | 两次 `CalculateSpawnerData` 加快照、离线 |
| 开全部 17 个岛（`all-islands`，第 0–30 天，n = 64 / 26）| 3.6 / 5.1 / 15.6 毫秒 | 3.6 / 4.5 / 15.4 毫秒 | 716 个刷怪点的 GPM 一遍；FastTick 的成本在换天的 `RecalculateChunkRpmValues` |

四次运行（提交 `d214306` 两次、`747086d`、评审修复的 WIP 提交 `c915d58` 各一次）的中位数相差不到 0.5 毫秒，表里是最后一次（`all-islands` 改成跑到第 30 天，FastTick 样本从 4 个增加到 26 个）。测量时机器负载约 7–9（10 核），`perf` 测试因此每次都跳过（门槛是负载 ≤ 核数/4），这些数字是负载下的上界。全部岛屿解锁时均值约 5 毫秒、p95 约 16 毫秒，离 5 毫秒的中位数预算只剩约 1.4 毫秒：C5/C7 验收时应在空闲机器上重测，`all-islands` 也按同一预算判定（测试对两个作业都检查）。p95 与最大值主要是换天的那几个慢阶段 Tick（多一遍 `RecalculateChunkRpmValues` 和 36 项修饰器更新）以及第一次换天时的 JIT 编译（只开岛 1 时各次运行的最大值都是约 11 毫秒），不是稳态成本；预算按中位数判定（均值被少数换天 Tick 和负载尖峰拉高，在忙机器上不稳定；C5/C7 在空闲机器上重测时把均值和 p95 一并记进本表，如果空闲时均值仍超过中位数预算，再改成按均值判定），`tests/unity/test_engine.py::test_per_tick_cost_within_budget`（`perf` 标记，机器忙时跳过）。

定死的预算（2.7）：

| 项 | 预算（中位数）| 依据 |
|---|---|---|
| 慢阶段一个 Tick，空策略 | ≤ 5 毫秒 | 全部岛屿解锁时实测约 3.6 毫秒 |
| 慢阶段一个 Tick，完整策略（C7）| ≤ 同一解锁状态下空策略的 3 倍，且 ≤ 15 毫秒 | 2.7 |
| 一个 FastTick（一整天，C5 完整版）| ≤ 50 毫秒 | 空策略约 3.7 毫秒，留给规划轮 |

### 16.4 Q07：刷怪点冷却（决定，已冻结）

- 刷怪点的"死亡"只用冷却表示。`IsRunning` 期间 `EnemySpawnSystem` 直接返回，冷却完全归引擎：`PrepareSimulationRun` 把所有非活动、非 VIP 刷怪点的 `Cooldown` 设为 0（新档的刷怪点都是满员，就绪时的值只是到下一个 UTC 对齐时刻的倒计时），清怪时设为 `BaseCooldown`（DAFC 为 `+∞`），空闲与离线按秒递减（`+∞` 不变），换天清零（`+∞` 除外）。
- 请求的 `sandbox.zero_ready_cooldowns` **保留、默认仍打开**：它在作业开始的状态摘要之前清零，C0b 冻结的 `harness` 黄金值（`ready.cooldowns_zeroed = 698`、`state.job_start_sha256`）依赖它；关掉它会改动原版基线，而模拟结果不变。`test_stop_at_day_is_exact` 用 `zero_ready_cooldowns = false`、`SimulationSpeed = 3` 重跑第 0–6 天，四个导出文件与 `manifest.engine` 都与默认运行逐字节相同。
- 交互运行（设计师在编辑器里勾 `IsRunning`，没有批处理）同样由引擎清零，不依赖这个开关。

### 16.5 验收记录（设计稿 7.3 C3b）

| 项 | 结果 | 证据 |
|---|---|---|
| 快照 0：属性 Value、knife 1/1、DpsData（Cut 9.2308、其它 0.92308）、DefenseData 全 0、天列表 Health CurrentGpm 2.8507 | 通过，且 DpsData、DefenseData 与天列表全部 CurrentGpm 与随包快照 0 **逐位**相同 | `test_snapshot_zero_equals_the_shipped_snapshot` |
| 空策略第 0–30 天：会话（G22）、TC = floor(T/1440)、早期节奏 1/5 分钟、早期列表门控（G23）、第 4 天起 FastTick（P 不变、每天 12 个 120 分钟离线步、每天一次换天）、裁剪 | 通过；P、T、TC 与独立的 float32 参考时钟逐位相同。注意空策略下 P、T 全是整数（P = 0…19、24、29…244，T 每步 230 或 120），float32 与 double 在这里没有区别，这个检查**不能**验证 float32 舍入；舍入要等 C7 的战斗时长（double 加到 float32 的 P 上）出现非整数时才有信息量 | `test_empty_policy_clock_matches_the_reference`、`test_empty_policy_sessions_days_and_gating` |
| 换天 `RpmFirst`：X1（列合成）、X2（DpsData/DefenseData）、X7（天列表 CurrentGpm）逐位 | 四种舍入下 X2、X7 都逐位（31 天、930 个 Gpm）；X1 只有 `double_all` 逐位，`stepwise`、`fma`、`double_tail` 各有 26/11880 个格子差（K2/K3 的末位）。空策略下平坦伤害属性为 0，DpsData 分不出 K2 的滞后，所以另跑 `flat-stats`（`grant_flat` 加 Cut 伤害与防御各 10）：`double_all` 下 DpsData/DefenseData 与 K2/K3(d−1) 逐位相同、与 K2/K3(d) 30 天全不同，Gpm 重放 RpmFirst 逐位；其它三种舍入在第 23、26 天差末位，X1 各差 34 格 | `test_day_change_order_rpm_first_bitwise`、`test_day_change_k2_lag_with_flat_stats`，黄金 `x1.*`、`flat_stats.*` |
| RPM 单调 | 通过 | 同上 |
| P07 消融可选 | `ModifierFirst` 与预言机的 ModifierFirst 重放逐位相同（与 RpmFirst 只在第 25 天 Health 一个格子不同）；`StaleTickCache` 第 1–3 天的 DpsData 等于 K(d−1) 系数下的值，之后冻结 | `test_p07_ablations_are_selectable` |
| 写入点：立即与累积模式、充能上限、DAFC 清一次后 `+∞`（`ResetCooldowns` 与离线都不改）、同岛重复解锁 | 通过。探针选 Count > 1 的刷怪点（立即模式 Count 6、累积模式 Count 4）：用量 `+= Count·次数`、奖励 `Value·Count·次数`、计数器 `clears`/`kills` 的增量都核对；充能上限步请求 1000 次，用量 8/12 时只做 1 次，`charge_clamped` 999；DAFC 选带 Health 奖励的（岛 10 的 ice pike blocker），请求 3 次只记 1 次、奖励只加一份；直接重复调用记 `repeat_unlocks` 1。每一步的灵魂与货币累计都按数据库 `spawners` 表的 `SoulReward`（`次数·Count`）和 `CurrencyRewardAmount`（`次数·Amount`）核对，累积模式不记 | `test_write_points`、`test_write_points_record_souls_and_currencies` |
| 快阶段同日门禁冲刷 `RebuildFlushFastTickGates` | 通过：探针把当前岛的 `RebuildFindBoss` 放进 `fastTickChunkGateRewardSpawners` 再冲刷：解锁 +1（目标岛取自 P6 的门禁数据）、`currentBoss` 移到新岛、槽位按 P6 的 `SlotReward`、集合清空、属性/血量/充能/冷却和 `clears` 都不变 | `test_fast_tick_gate_flush` |
| 换天清用量与冷却、离线递减冷却与回血 | 通过。探针先离线到当天最后一分钟、清一次（冷却 = BaseCooldown 240 秒）、把血量设为 1，再离线 1 分钟：冷却变成 180 秒，血量 = `min(上限, 1 + Regen·60)`，天序号跨到 1；随后 `UpdateSpawnerChargesChunk`：三个被观察刷怪点用量全为 0，有限冷却为 0，DAFC 仍是 `+∞`。`engine-30` 的天列表上 current_health(d) = 第 d−1 天的 Health TotalValue | `test_offline_minute_and_day_change`、`test_regen_refills_to_the_previous_days_health` |
| 早期列表 CurrentGpm = ΔValue/ΔP | 通过：探针在两次立即清之后空闲 1 分钟写早期快照，每项都与 `(V1 − V0)/(P1 − P0)` 逐位相同（Health 非零），随后第 1 天强制快照的早期副本 ΔP = 0，为 NaN | `test_early_snapshot_gpm_is_dvalue_over_dp` |
| X6：回头门不算重复解锁 | 通过：按 P6 数据，有回头门的刷怪点正好 4 个（岛 4、11、12、13）；探针先直接解锁到 13，再逐个清：回头门记 `back_gate_noops` 共 6 次，岛 13 的前进门解锁 14 岛，`repeat_unlocks` 不增加。运行中不会重复解锁是结构保证的（门只开一次，指向已开岛的门不进 `RebuildUnlockLocation`）；`engine-30`、`all-islands` 不清门禁，运行级的检查要等 C5/C7 的策略清门禁时才有信息量 | `test_back_gates_are_noops_not_repeat_unlocks` |
| Q07：引擎自己清零就绪冷却 | 通过：关掉 `zero_ready_cooldowns` 时 `PrepareSimulationRun` 清零 698 个（打开时 0 个，已被沙箱清过），两种情况下探针开始时非零冷却都是 0 | `test_engine_zeroes_ready_cooldowns_without_the_harness` |
| `RebuildFindBoss` 与 P6 的 `boss_for_location` 一致 | 17/17（岛 17 为 null，岛 4 是 dragon）| `test_find_boss_matches_p6` |
| `StopAtDay` 恰好停在目标日 | 通过：最后一个天快照是第 6 天，P/T 位与之相同，输出是 30 天运行的前缀 | `test_stop_at_day_is_exact` |
| 停用模块打开时作业失败 | Unity 侧通过，启动器层面未通过：在评审修复的 WIP 提交 `c915d58`（C# 与最终提交相同）上三个开关各一个作业（运行 632c0f0a、60e4c9e3、53863ae1），Unity 以退出码 5 结束，`error` 与 `manifest.engine.failure` 都是 "not supported: SimulationConfig.<开关> is on"，`engine.stop_reason = failed`；但用户主检出的编辑器整段时间都在每几秒写真实存档（`game_data.bin`，11:17–11:53 从未安静 90 秒；评审第二轮又等了 12:15–13:00，同样从未安静 90 秒），失败型运行按规则得到启动器退出码 2，测试仍判失败。第二轮只在 `StartJob` 里加了 S1 重置入口（`reenter_play` 下不做事），停用模块与心跳的路径没变；加了入口之后的 17 个正常作业测试（WIP 提交 `73b2fa7`）都通过了 `StartJob`。真实存档安静后重跑 `pytest --unity tests/unity/test_engine.py -k "disabled or heartbeat"` | `test_disabled_modules_fail_the_job` |
| 每 Tick 耗时写进本节并定预算 | 见 16.3 | `test_per_tick_cost_within_budget` |
| 7.4 的跨模块调用点全部就位 | 通过（Roslyn 语法检查），包括 `SimulationHarness.StartJob → RebuildTryResetForJob`（S1 的入口），以及两处 `RebuildApplyDayOverride` 都在强制快照之后 | `tests/test_cs_signature_lock.py::test_cross_module_call_sites_in_place`、`test_day_override_follows_the_forced_snapshot`、`test_harness_calls_the_reset_bridge` |
| 交接：作业阶段的心跳只看 Tick | Unity 侧通过，启动器层面未通过（同上）：`c915d58` 上 `empty` 作业帧在走、不 Tick，启动器在 8 秒停顿后杀掉 Unity（`unity_exit` 3），`progress` 停在 `frames-only:4:0`（不再含帧数）；同一运行（6069b3ea）因真实存档被用户编辑器改写，启动器退出码是 2 而不是 3 | `test_heartbeat_follows_ticks_in_the_job_phase` |
| 交接：`Provenance` 的整数值固定 | 0–4 固定（C# 源码、Python 常量、manifest 三处一致）| `tests/test_engine_static.py::test_provenance_enum_values_are_pinned`、`test_manifest_engine_block` |

### 16.6 与设计稿不同的地方

- 快照的属性是 `Unit.Stats` 的 30 项，不是 36 项（36 是 `UnitCore` 查找表的大小）。
- `TotalValue` 按数据的顺序 `(Value·(1+K2))·K3` 计算，没有直接取 `GetStatValue`：`StatBase.CalculateValue` 的顺序是 `(K3·(1+K2))·Value`，末位可能不同，而 G11 的 981/981 是按前一种顺序成立的。这个选择**只**以 G11（随包数据）为依据：评审第二轮测得引擎的四个作业里 TotalValue ≠ TotalFlatValue 的格子每个作业只有 52 个，换成游戏顺序一个也不变，测试分不出两种顺序。
- 换天时 `RebuildApplyDayOverride(d)` 放在强制快照**之后**：设计稿 4.6 说覆盖"在第 d 天的强制快照之后、当天第一次规划之前生效"，第 0 天的伪代码也是先快照后覆盖，但 4.4 `UpdateSpawnerChargesChunk` 的伪代码把它放在快照之前，两处矛盾。按 4.6：先覆盖的话 checkpoint/replay 第 d 天的快照就是随包值本身，看不到区间误差（4.6 里 checkpoint 存在的意义）。最初的实现照抄了 4.4，评审第二轮改正；`test_day_override_follows_the_forced_snapshot` 锁定顺序。C4 不能改 `.Run.cs`，如果要别的语义需回到设计稿。
- 写入点按剩余充能截断次数（`charge_clamped`）：设计稿 4.5 只说用量不超过上限；不截断时计数器和发给 `AccumulateFastTickSpawnerRewards` 的次数会超过实际能清的次数。C5 算出的 `fights` 超了也不会多发奖励。
- `SimulationHarness.StartJob` 调用 S1 的 `RebuildTryResetForJob()`（7.4 的 `SimulationHarness → RebuildResetForJob` 一行）；`PrepareSimulationRun` 不重置 `PlayerModel`/`GatesModel`（见 16.2）。
- 新成员名一律以大写的 `Rebuild` 开头（`RebuildStopped`、`RebuildParams` 等）：设计稿 4.2 的 `rebuildStopped` 这类小写名会被 cscheck 的签名锁拒绝。
- `SlotReward` 在清掉门禁刷怪点时打开（`RebuildApplyGateReward`），不在 `RebuildUnlockLocation(n)` 里：槽位属于刷怪点，而 `RebuildUnlockLocation` 只知道岛号。随包的三个槽位门禁都只开自己的下一个岛，结果相同。
- 门禁按 `GatesModel` 只开一次：岛 1 的门禁刷怪点可以反复清，反复清不会再触发解锁；指向已开岛的门（回头门）在 `RebuildApplyGateReward` 里记 `back_gate_noops`，不进 `RebuildUnlockLocation`（评审修复：最初的实现先查"已解锁集合"，岛 11→8、12→7、13→2 的回头门会被误记成 `repeat_unlocks`）。
- 快照的 `Value` 另外去掉装备（`Item`）修饰器（X6）；随包物品都没有 `StatModifiers`，目前没有影响。
- 停用模块包括 `GraphSimulation`；失败用退出码 5。
- 引擎从 `SimulationHarness.CurrentJob` 读作业（不引用 `SimulationSandbox`，沙箱钩子清单不变）。
- 改了 C0b 的 `SimulationHarness.cs`（心跳、引擎失败、manifest `engine`、每 Tick 耗时、`CurrentJob`、S1 重置入口）、`tufu_balance/unity/__init__.py`（导入 `engine` 以登记请求字段和 manifest 键）、C1 的 `tests/unity/test_export_roundtrip.py`（空作业的 `time_chunk_trim` 现在是 `GetTrimmedTimeChunkSnapshots`）、C3a 的 `tests/test_cs_signature_lock.py`（改成 C3b 之后的状态：只有填过的方法体变了，出处按 `FILLED`，调用点按 7.4）。

## 17. 参数、随机与轨迹（C3c）

C3c 加了参数类的资产包装与请求覆盖（`Rebuild/SimulationRebuildParams.cs`、`Rebuild/SimulationRebuildParamsAsset.cs`）、参数资产 `Assets/Simulation/Simulation Rebuild Params.asset`、导入入口 `ED/SimulationParamsImporter.cs`、`Rebuild/SimulationRandom.cs`、`Rebuild/SimulationTrace.cs` 与 `Systems/SimulationSystem.Trace.cs`。Python 一侧是 `tufu_balance/unity/{paramsio,trace,simrandom,rebuildcheck}.py` 和 `Balance/tools/rebuildcheck/`。测试：`tests/test_random.py`、`tests/test_params_hash.py`、`tests/test_params_import.py`（不需要 Unity）、`tests/unity/test_params.py`。

### 17.1 参数从哪里来

| 运行 | 来源 | manifest `params.source` |
|---|---|---|
| 批处理，`job.params_source` 缺省或为 `"request"`，`job.params` 不为 null | 请求的参数对象按字段覆盖代码默认值；未知字段报错（退出码 5）；漏掉的字段取代码默认值并列在 `missing` | `request` |
| 批处理，`job.params` 为 null | 代码默认值 | `code_defaults` |
| 批处理，`job.params_source = "asset"`（`job.params` 必须为 null）| 参数资产，经 `AssetDatabase.LoadAssetAtPath` 读，与交互运行同一个函数（`SimulationRebuildParamsLoader.LoadAsset`）| `asset` |
| 交互运行（设计师按 Play 并勾 `IsRunning`，没有批处理作业）| 编辑器里读参数资产，资产不存在时用代码默认值；播放器构建里没有资产，用代码默认值 | `asset` / `code_defaults` |

- 引擎在 `PrepareSimulationRun` 第一步（`RebuildLoadRunOptions`）调用 `SimulationRebuildParamsLoader.Load`，随后按 `RollMode`、`Seed` 建随机源，并开始轨迹记录。参数无效、哈希不符或 `params_source` 不认识时作业以退出码 5 失败，`error` 写明原因。
- 请求参数的检查比 Newtonsoft 本身严：字段名区分大小写（`seed`、`SEED` 是未知字段，报错里提示正确的写法；Newtonsoft 默认忽略大小写，会让 `"seed"` 悄悄覆盖 `Seed`），JSON 类型必须与字段相符（整数与枚举字段只收 JSON 整数，不收字符串、`7.0`、布尔或 null；float/double 收整数与小数；数组不能是 null，元素逐个检查）。`Validate` 另外拒绝非有限的浮点（NaN、±Infinity，以及 `1e39` 这类读成 float 后溢出的值），对所有 float/double 字段和数组生效，后续单元加的字段也一样。
- Python 一侧：`request.validate` 对引擎会加载的作业（非 `empty`）做同样的检查（`trace.check_job_params`，经 C0b 新增的 `request.register_job_check` 登记）：`params_source`、`params_hash` 是否等于 `params` 的规范哈希、`params_hash` 不能没有 `params`、字段名与 JSON 类型对照本工作树的 C# 类（允许部分对象，与 C# 相同）。这样大小写写错、类型不符或哈希不符的请求在启动 Unity 之前就以 5 被拒。`empty` 作业的 `params` 不检查（C# 也不加载它们；C0b 的往返示例带的是任意对象）。专门验证 C# 拒绝路径的 Unity 测试以 `validate=False` 绕过这一步。
- 运行期间只用副本，资产对象本身不被改动（资产守卫核对它的内存摘要）。
- **哈希**：`params_hash` 是规范化 JSON（键按码位排序、无空格、`ensure_ascii=False`、浮点按 Python `repr`）的 sha256，即 `tufu_balance.calibrate.params.params_hash`。C# 的 `SimulationCanonicalJson` 用整数运算求最短往返十进制，不依赖运行时的数字格式化。
  - **读数也不依赖运行时**：Newtonsoft 用运行时的 double 解析器读数字，编辑器的 Mono 会把一些普通的字面量读偏 1 ulp（例如 `181.0483539212821`、`1.534053662242575`，约万分之一到千分之一），C# 的 `request_hash` 就与 Python 不同、作业以 5 失败。所以 `SimulationRunRequest.Load` 在 Newtonsoft 读完请求后，再用 `SimulationCanonicalJson.Parse`（自带的严格 JSON 读取器，十进制转 double 用 BigInteger 正确舍入，语义同 Python 的 `json.loads`：接受 `NaN`/`Infinity`，重复键取最后一个，保留 `-0.0`）从请求原文重读每个作业的 `params`（`ReadExactParams`）；读不了时只让带 `params` 的作业以 5 失败（`Job.ParamsReadError`）。`FromJson` 按解析出的 token 用反射直接设字段，不再经过 `PopulateObject` 的文本往返。
  - 核对：`tools/rebuildcheck` 同时用 .NET 8 和编辑器自带的 Mono（`MonoBleedingEdge` 的 csc 与 mono）构建运行，规范 JSON、参数与随机数的测试在两个运行时下都跑：约 2 万种数字写法与 `json.loads` 相同（含上面两个字面量），读取器的拒绝情形，加载函数的哈希检查（`SimulationRebuildParams.FromRequest`：哈希不符、有哈希没参数都报错），以及"Mono 自己的解析确实不精确"这一前提。Unity 里 `params-mono-literals` 作业带这两个字面量和 `params_hash` 照常运行，`request_hash` 等于 P10 的哈希。
  - `request_hash`：C# 对 `job.params` 原文求的哈希。请求带了 `params_hash` 时两者必须相等，否则退出码 5。
  - `hash`：生效值（`SimulationRebuildParams.EffectiveCanonicalJson`：float 字段写成它的 float32 的最短十进制）的哈希。完整请求里每个浮点都是 float32 值时，它就等于 `params_hash`；Python 用 `paramsio.effective_hash` 预测同一个值。`matches_request` 记两者是否相等。
  - `-0.0`：精确读取器保留符号，`-0.0` 的请求哈希等于 Python 的，生效值也是 `-0.0`（以前 Newtonsoft 读成 `0.0`、作业因哈希不符被拒）。
- manifest `params`（`tufu.sim.params/1`）：`source`、`interactive`、`complete`、`missing`、`request_hash`、`request_hash_given`、`hash`、`matches_request`、`roll_mode`、`seed`、`asset_path`、`asset_guid`、`asset_found`、`error`。`manifest.engine.params` 仍是生效参数本身。失败的作业只要加载函数已经运行，也带 `params`（`SimulationHarness.Complete` 以 `jobDir = null` 调用钩子 `RebuildJobExportTrace`，只写这一项；轨迹记录按作业对象核对，不会写上一个作业的）。

### 17.2 参数资产与 `params import`

```bash
python -m tufu_balance.unity params build [--layers calibrated ...] [--set FIELD=JSON ...] [--float32] [--out FILE]
python -m tufu_balance.unity params import [--layers calibrated ...] [--set FIELD=JSON ...] [--float32] [--rev REV] [--dest PATH]
```

- 资产在 `Assets/Simulation/`，不在 `Resources/` 下；初值等于 `Balance/sim/params/defaults/*.json`（测试断言资产文本就是 `paramsio.asset_text(默认值)`）。ScriptableObject 类 `SimulationRebuildParamsAsset` 单独放在同名文件里：Unity 要求脚本文件名与类名一致，资产才能找到它的脚本。
- `--float32` 把每个 float 字段舍入成它的 float32 值并重算 `params_hash`（`paramsio.round_float32`）。资产只能存 float32：`import` 在启动 Unity 之前就检查，某个 float 字段不是 float32 值时以 5 结束并列出字段（`paramsio.not_float32`），不必等编辑器启动后由导入入口拒绝。
- `build` 合并参数层（默认值总在最前），输出 `{"schema": "tufu.sim.params/1", "params": …, "params_hash": …, "layers": …}`。
- `import` 由 `paramsio.import_params` 执行：取运行工作树的锁、要求干净、检出 `--rev`（默认本工作树的 HEAD）、核对受保护 DLL、记下真实存档与工程文件的哈希，然后启动一个**新的**批处理编辑器 `-executeMethod Codebase.Simulation.EditorTools.SimulationParamsImporter.Import -tufuSimParams <params.json> -tufuSimParamsReport <import_report.json>`（不进入 Play、没有心跳，超时默认 900 秒）。
- 导入入口：
  - `[InitializeOnLoad]` 在每个编辑器进程里监听 `playModeStateChanged`，进入 Play 前（`ExitingEditMode`）在 `SessionState` 记下 `TufuSimParams.entered_play`。导入时这个标志为真、编辑器正在或将要进入 Play、或者不是批处理模式，都拒绝（退出码 2）。批处理编辑器每次退出 Play 时在日志里写一行 `[TufuSim] params import guard after Play: …`，测试用它证明进过 Play 的进程确实被拒。
  - 输入必须是完整对象（缺字段、未知字段、无效值都是 5），`params_hash` 必须等于 C# 算出的规范哈希，生效值的哈希也必须等于它（即每个浮点都是 float32 值），否则 5。
  - 只保存参数资产（`SetDirty` + `SaveAssetIfDirty`，资产不存在时 `CreateAsset`），强制重新导入，再读回三次，哈希都必须相同：`LoadAsset`（`read_back`）、运行时的加载函数 `SimulationRebuildParamsLoader.Load` 带一个 `params_source = "asset"` 的作业（`read_back_job`），以及不带作业的交互分支（`read_back_interactive`）。写入的是非默认值时，这就证明运行的加载路径用的确实是资产里的值，而不是代码默认值。报告写 `import_report.json`。
- 启动器一侧：编辑器退出后，唯一允许变化的工程文件是参数资产（`git status` 里只能有它一行 ` M`），其它变化一律退出码 2。独立核对：导入报告的各个哈希（含两次经加载函数的读回：`source = asset`、`hash`、`result_hash`）、`entered_play = false`、从写出的 YAML 读出的值等于预期的生效值、文件 sha256 与报告一致。全部通过后把资产复制到 `--dest`（默认本工作树的资产路径，之后由人提交），**再把运行工作树里的资产 `git checkout` 回去**，运行工作树保持干净。`--dest` 不能在运行工作树里。
- 真实存档与 PlayerPrefs：导入不进 Play，不写游戏存档。导入期间真实存档变了、而主检出上同一个编辑器从头到尾都开着、导入正常结束且报告 `entered_play = false` 时，变化记为 `real_changed_by_main_editor`，不判 2（与第 13 节的规则对应，沙箱证明换成"没有进过 Play"）；否则照常判 2。
- 资产守卫的放行只有这一个入口：`tests/test_params_hash.py::test_only_the_importer_writes_assets` 断言 `CodeBase/Simulation` 与 `Assets/Editor/Simulation` 里调用写资产 API（`SetDirty`、`SaveAssets`、`SaveAssetIfDirty`、`CreateAsset` 等）的只有 `SimulationParamsImporter.cs`（C8 的 `SimulationBakeTool.cs` 预先列入白名单）。批处理运行的资产守卫与启动器检查不变。
- 交给 K3：`params import --layers calibrated` 写入发布值，结果复制回开发工作树后提交。拟合出的 double 一般不是 float32 值：先把 `calibrated.json` 的 float 字段舍入成 float32（或加 `--float32`），否则导入在启动 Unity 前以 5 结束；请求运行时同样建议先舍入，否则 manifest 的 `hash` 与 `params_hash` 不同（`matches_request = false`）。

### 17.3 `SimulationRandom`

```csharp
var random = new SimulationRandom(RollMode mode, int seed);   // 引擎里是 RebuildRandom，按参数建
int n = random.Roll(string stream, double x);                 // 期望份数 x 的整数结果
double u = random.NextDouble(string stream);                  // [0, 1) 均匀数（两种模式都可用）
double c = random.Carried(string stream);                     // Carry 的累加余数
string s = random.StateText();                                // 进每天的状态摘要
```

- `Carry`（默认）：`n = floor(x) + floor(carry + frac(x))`，余数留在该流里。对任意序列每个流都有 `0 ≤ Σx − Σn < 1`（仅差 double 舍入）。
- `Seeded`：每次 `Roll` 抽一个 `u`（与 x 无关，抽取次数只取决于调用次数），`n = floor(x) + (u < frac(x))`。每个流一个 SplitMix64，起始状态 `Mix(FNV-1a64(流名的 UTF-8) + (uint)Seed · 0x9E3779B97F4A7C15)`。
- 流按名字互相独立：改一个流的调用（数值、次数、`NextDouble`）不改变其它流的任何结果。流名按设计 4.7 用物品 Key；C6 的 `RollFractionalCount` 与快阶段冲刷用 `RebuildRandom.Roll(item.Key, x)`。
- 不用 `UnityEngine.Random`、墙钟；Python 移植 `tufu_balance.unity.simrandom` 与 C#（在 .NET 下）逐位一致（`tests/test_random.py`）。

### 17.4 轨迹文件

非 `empty` 作业结束时由 `SimulationHarness` 的新钩子 `RebuildJobExportTrace`（在 C1 的导出之后）写出；开关在 `job.outputs`：`daily`、`events`、`digests` 默认开，`digest_text` 默认关。manifest 新增 `trace`（`tufu.sim.trace/1`）：每个文件的 `sha256`、`bytes`、`rows`，`days`、`complete_days`、`digests`、`last_digest`、各事件类型的个数，以及 `random`（`mode`、`seed`、`streams`、`state_sha256`）。`tufu_balance.unity.trace.read(job_dir)` 按 manifest 与 `schema.py` 校验全部文件；`simio.ingest` 照常把它们导入 `sim_daily`、`sim_digests`、`sim_events`。

- `daily.csv`：C1 的列，加上追加的 `ticks`、`offline_steps`、`complete`。完整的天在换天时写一行；作业在某天中途停下时，这一天以 `complete = 0` 写一行；`StopAtDay` 停在目标日的强制快照之后，目标日没有行。各列：`time_chunk_index` 是这一天的强制快照在完整天列表里的位置；`playtime`、`total_playtime` 是天末的 P、T（等于下一天强制快照的值）；`active_minutes` 是当天 P 的增量；`offline_minutes`、`offline_steps` 是当天开始的离线步；`kills`、`clears` 来自写入点的计数；`fight_time_sum`、`farmed_time_sum` 由战斗模块上报（C5/C7 调 `RebuildTraceFight`，之前为 0）；`eligible_count`、`eligible_signature` 是当天第一次规划时机器人过滤的集合（`CalculateSpawnerData(false)` 之后的 `spawnerData`；签名是排序后的"层级路径|Key"逐行的 sha256，空集为空串；当天没有规划时为 `-1` 和空串，例如最小 FastTick）；`location` 是最后一次战斗的刷怪点所在岛（还没打过时取引擎的当前岛）；`unlocked_location` 是已解锁的最高岛。
- `events.jsonl`：C1 定的类型与字段，按发生顺序。`first_kill` 每个刷怪点每次运行一次（写入点）；`unlock`（`RebuildUnlockLocation`）；`slot_unlock` 的 `count` 是解锁后同类槽位（武器、头盔、盾）已解锁的个数；`evolve`、`death` 由 C6、C7 调用。
- `digests.csv`：每天一行，在这一天的强制快照和覆盖（`RebuildApplyDayOverride`）之后，记状态文本的 sha256；`digests.txt`（`digest_text`）是这些文本本身，每段以 `# day <d> <sha256>` 开头。

给后续单元的调用（都是 `SimulationSystem` 的 private 方法，在 `.Trace.cs`）：

| 方法 | 谁调用、在哪里 |
|---|---|
| `RebuildTraceEligible()` | C5：`FastTick` 第一轮规划的 `CalculateSpawnerData(false)` 之后（慢阶段的 `Tick` 已经调用；每天只记第一次）|
| `RebuildTraceFight(double fightSeconds, double farmedSeconds)` | C7：每场慢阶段战斗；C5：每批快阶段清怪 |
| `RebuildTraceSlotUnlock(ItemSlotModel slot)` | C6：`UnlockSimulationWeaponSlot` 开槽时（门禁的 `SlotReward` 已由写入点调用）|
| `RebuildTraceEvolve(ItemModel item)` | C6：物品进化时 |
| `RebuildTraceDeath(EnemySpawner spawner)` | C7：`ProcessDeath` |

轨迹只读状态、不改状态；`RebuildTraceFlush()`（internal）由钩子在导出前调用，补写未完成的那一天。

交给 C4（或 C3d）：C3c 的作业都没有当天覆盖，摘要取在 `RebuildApplyDayOverride` 之后这一点目前只有静态检查（`tests/test_params_hash.py::test_digest_is_taken_after_the_day_override`）。C4 填覆盖后加一个带当天覆盖的作业，断言当天的摘要反映了覆盖后的状态。

交给 C5：参数类目前没有 bool 字段；`paramsio.read_asset` 对 bool 只收 Unity 写的 `0`/`1`（`test_asset_reader_parses_bool_fields_as_unity_writes_them` 用一个合成的 bool 字段核对）。C5 加 `KillLossFromAb` 之类的 bool 字段时，`check_against_csharp` 与 C# 的 `CheckTokens` 都只收 JSON 的 `true`/`false`。

### 17.5 每天的状态摘要

文本是按序（ordinal）排好的 `键=值` 行，浮点与 double 写 IEEE 位的十六进制。覆盖设计 2.6 的全部内容，另加第二天会用到的引擎状态：

| 前缀 | 内容 |
|---|---|
| `stat.<StatType>` | 快照合成的 `Value`、`TotalFlatValue`、`TotalValue`，以及活的 `stat.Value`（30 项，即 `Unit.Stats`）|
| `item.<Key>#<序号>` | `Level`、`Copies`、`Evolved` |
| `location.*` | 已解锁岛、当前岛、位置、当前刷怪点、`currentBoss` |
| `clock.*` | 天序号、P、T、会话序号与会话终点、上次广告、上次早期快照的 P、是否已进入快阶段 |
| `rpm.<StatType>`、`soul_rpm.<名>` | `RPMCalculatedValue`、`SoulRPMCalculatedValue` |
| `charges.<刷怪点>`、`cooldown.<刷怪点>` | 当天到此为止非零的充能用量、非零的冷却 |
| `charges_closed.day`、`charges_closed.<刷怪点>` | 上一天结束时的充能用量（非零的）。换天先清空 `spawnerChargesUsage` 再取摘要，所以 `charges.*` 在闭环运行里总是空的；`RebuildTraceDayEnd` 在清空之前把用量记下，下一天的摘要带上。第 0 天没有这一组；作业停下时还开着的那一天的用量不进任何摘要 |
| `health`、`slot.*`、`gate.*`、`modifiers` | 当前血量、槽位、已开门禁、插值修饰器的天与 K2/K3 |
| `pending_item.*`、`fast.*`、`souls.*`、`currency.*`、`policy.*` | 待处理份数、快阶段累积、灵魂与货币记录、策略状态（`statsBenefit`、`lastRewardStat`）|
| `random.*` | `SimulationRandom.StateText()` 的每一行 |

`daily.csv` 的 `eligible_count`/`eligible_signature` 由 `test_daily_eligible_set_is_p8s_bot_filter_set` 独立核对：在每个慢阶段天的强制快照上跑 P8 的 `simcalc.calculate_spawner_data(forGPMBot = false)`（空策略：没用过充能、就绪冷却为 0、全员存活），用 C1 的身份表转成"层级路径|Key"，个数与签名都相同（`trace-defaults` 第 0 天 6 个、之后 26 个；引擎运行 `all-islands` 第 1 天起 46 个，用一次性脚本核对也相同）。

`tests/unity/test_params.py::test_digest_covers_design_2_6` 逐天核对：每项属性的 Value/TotalValue、天列表的 CurrentGpm（即 RPM）、P、T、Location、物品、血量、K2/K3 都与独立导出的快照文件逐位相同。`test_digest_carries_the_charges_of_the_closed_day` 在写入点探针（第 0 天清怪后换天）上核对第 1 天摘要的 `charges_closed.*` 等于探针自己记录的换天前各刷怪点用量。

### 17.6 验收记录（设计稿 7.3 C3c）

在提交 `a2c6080` 上，运行工作树里 `pytest --unity tests/unity/test_params.py tests/unity/test_engine.py tests/unity/test_export_roundtrip.py -k "not disabled and not heartbeat"`：35 通过、1 跳过（`perf`，机器负载 8.5/10 核）。评审修正后在 `ba93b57` 上重跑同一命令：36 通过、1 跳过（`perf`，负载 7.9/10 核），用时 156 秒；新增的 `test_digest_carries_the_charges_of_the_closed_day` 在第 1 天摘要里看到 4 个 `charges_closed` 刷怪点（用量 18、12、2、1），与探针记录相同；导入（约 7 秒）的两次加载函数读回都是非默认值的哈希。C3b 的失败型测试 `disabled`/`heartbeat` 没有跑：用户主检出的编辑器一直开着（pid 19703），失败型运行会被启动器判 2（第 13 节），记为"未运行：用户编辑器在用"。不需要 Unity 的测试：`tests/test_random.py`、`tests/test_params_hash.py`、`tests/test_params_import.py` 与 P10 的 `tests/test_calibration_harness.py` 全部通过。

第二轮评审修正（精确读取请求参数，C# 最后改动在 `b9216d2`）后，在 `b9216d2` 上重跑同一命令：39 通过、1 跳过（`perf`，负载 8.5/10 核），用时约 50 秒（引擎与导出运行命中缓存；导入与两个失败型运行重新执行，导入约 7 秒）。新增：`params-mono-literals` 带 Mono 会读偏的两个字面量，`request_hash = request_hash_given =` P10 的哈希、生效值等于 `paramsio.effective` 的 float32 值；`params-wrong-hash` 带错误的 `params_hash`，Unity 退出码 5、`error` 原文与哈希都核对（启动器因用户编辑器写真实存档得到 2，同上）；`test_daily_eligible_set_is_p8s_bot_filter_set`。之后的提交只改 Python、测试与文档。之前的 .NET 逐字节结论只覆盖 .NET；Mono 下的结论来自本轮的双运行时 `rebuildcheck`。

| 项 | 结果 | 证据 |
|---|---|---|
| 请求覆盖生效 | 通过：`params-override`（`FastAfkStepMinutes` 60、`EarlySnapshotIntervals` [2, 5]、`RollMode` 1、`Seed` 7）的早期列表与天列表逐位等于用这些值的参考时钟，P < 20 的早期快照是 0, 2, …, 18（默认运行是 0…19），快阶段每天 24 个 60 分钟离线步；`manifest.trace.random` 为 (1, 7) | `test_request_overrides_take_effect` |
| `params_hash` 稳定、与 P10 相同 | 通过：Unity 里 `request_hash = request_hash_given = hash = P10 params_hash`（默认值与覆盖两组），Mono 会读偏的字面量也一样（`params-mono-literals`）；错误的 `params_hash` 以 5 失败（`params-wrong-hash`）；.NET 与编辑器的 Mono 下 C# 规范 JSON 与 Python 逐字节相同（约 8000 个 double、6000 个 float32、300 个随机 JSON，另有约 2 万种数字写法与 `json.loads` 比较），成员顺序和数字写法（`1.2e2`、`20.000`）不改变哈希 | `test_request_params_hash_equals_p10`、`test_literals_the_editor_parser_rounds_keep_the_p10_hash`、`test_wrong_params_hash_fails_the_job`、`tests/test_params_hash.py` |
| P10 的 `resolve()`/`check_against_csharp` 对真实类仍通过 | 通过 | `tests/test_calibration_harness.py`（含 `test_csharp_parameter_class_of_this_worktree`）|
| 未知字段报错 | 通过：Unity 里作业以 5 失败，`error` 写明 `Bogus`，失败的 manifest 也带 `params`（`request_hash` 等于 Python 的哈希）；大小写不同的字段名、类型不符的值在 .NET 下同一段代码里报错（`test_field_names_and_json_types_are_strict`）；启动器层面因用户主检出的编辑器一直在写真实存档，失败型运行得到 2（第 13 节的规则），测试按这一情形核对 | `test_unknown_param_field_fails_the_job`、`test_unknown_missing_and_invalid_fields` |
| 交互运行经 `AssetDatabase` 读资产 | 通过（经批处理的 `params_source = "asset"` 走同一个加载函数）：`source = asset`、`asset_found`、GUID 与 `.meta` 相同，哈希等于从资产 YAML 独立读出的值。导入写入非默认值后，`Load` 的作业分支与交互分支读回的哈希都等于新值（不等于默认值）| `test_params_asset_is_read_through_the_asset_database`、`test_params_import_in_a_fresh_editor` |
| `params import` 只在没进过 Play 的新编辑器进程里执行、能写资产 | 通过：新批处理编辑器约 6.4 秒写入（`entered_play = false`），读回哈希、文件 sha256、独立解析的值全部一致，Unity 写出的文本与 `paramsio.asset_text` 逐字节相同；运行工作树恢复干净，资产复制到目标路径。每个进过 Play 的批处理编辑器在退出 Play 时都记下 "refused: this editor process has entered Play mode"（4 个作业 4 行）。导入期间真实存档的变化按 17.2 归给用户的编辑器 | `test_params_import_in_a_fresh_editor`、`test_editors_that_entered_play_refuse_the_import`、`tests/test_params_import.py` |
| 资产守卫只对这个入口放行 | 通过：启动器只允许导入时参数资产一个文件变化，其它变化退出码 2、不复制；写资产的编辑器 API 只出现在导入入口 | `tests/test_params_import.py`、`test_only_the_importer_writes_assets` |
| `Carry` 下 Σn 与 Σx 之差 < 1 | 通过：每个流、每个前缀都满足 `0 ≤ Σx − Σn < 1`（精确有理数求和，2 万次调用含近 1 与极小的小数）| `test_carry_keeps_every_stream_within_one` |
| `Seeded` 的流按名字分开，改一个流不影响其它流 | 通过（两种模式都检查）；C# 与 Python 移植在 .NET 下逐位一致 | `test_streams_are_independent`、`test_csharp_matches_the_port_bit_for_bit`、`test_csharp_streams_are_independent` |
| 轨迹按 C1 的 `schema.py` 校验 | 通过：三个文件（加 `digests.txt`）按 manifest 的 sha256/字节数/行数与 schema 校验，并经 `simio.ingest` 导入 | `test_trace_files_validate_against_the_schema`、`test_daily_rows_follow_the_snapshots`、`test_events_follow_the_write_points` |
| 摘要覆盖 2.6 | 通过：第 0–8 天每项属性的 Value/TotalValue、RPM、P、T、Location、物品（Key/Level/Copies/Evolved）、血量、K2/K3 与导出快照逐位相同；空策略下没有充能用量（第 d ≥ 1 天只有 `charges_closed.day`），冷却只有活动/VIP 刷怪点；写入点探针第 1 天的 `charges_closed` 等于换天前的用量；两次运行第 0 天只差随机源一行 | `test_digest_covers_design_2_6`、`test_digests_differ_only_where_the_state_differs` |
| 不改变 C3b/C1 的结果 | 通过：C3b 的引擎测试（除去失败型运行的两个）和 C1 的往返测试照常通过；空策略每 Tick 耗时（`engine-30` 慢阶段 p50 0.33 毫秒，`all-islands` 3.58 毫秒；FastTick p50 0.54 / 3.91 毫秒）与 16.3 相同量级 | `tests/unity/test_engine.py`、`tests/unity/test_export_roundtrip.py` |

### 17.7 与设计稿不同的地方

- 资产包装类 `SimulationRebuildParamsAsset` 不在 `SimulationRebuildParams.cs`，而在同名文件 `SimulationRebuildParamsAsset.cs` 里（Unity 的 ScriptableObject 资产按文件名找脚本）；加载函数 `SimulationRebuildParamsLoader` 也在那里，`SimulationRebuildParams.cs` 因此不依赖 UnityEngine，可以在 .NET 下测试。抽取器会把资产导出成 `c_SimulationRebuildParamsAsset` 一类的表名（按脚本类名），不是设计稿写的 `c_SimulationRebuildParams`。
- 请求覆盖仍按字段覆盖**代码默认值**（C3b 的行为），不要求完整对象；完整与否记在 manifest `params.complete`/`missing`。设计稿 1.4.2 说请求带完整对象，正式运行（P10 的驱动）也确实带完整对象；只有导入入口强制完整。
- 新增作业字段 `params_source`（`SimulationRunRequest.Params.cs`）：批处理也能走交互运行读资产的那条路径，用来验收"经 AssetDatabase 读资产"。提交的资产等于默认值，这个作业本身分不出"读了资产"与"退回代码默认值"；所以导入入口写入非默认值后，再经 `SimulationRebuildParamsLoader.Load`（作业分支与无作业的交互分支）读回核对（17.2），`Load` 因此改为 public。交互运行在 Play 里的那一刻（设计师按 Play）仍只有代码审查。
- `params_hash` 之外另报生效值的哈希 `hash`：C# 的字段是 float32，非 float32 的请求值读进来就变了，只有生效值的哈希能说明实际用了什么。
- 新增 partial 文件 `Systems/SimulationSystem.Trace.cs`（只有方法），以及 C3b 文件里的一行调用：`.Run.cs` 的 `PrepareSimulationRun`、`Tick`、`UpdateSpawnerChargesChunk`，`.Rebuild.cs` 的 `RebuildApplyClear`、`RebuildApplyGateReward`、`RebuildUnlockLocation`，以及 `RebuildLoadRunOptions` 的方法体和字段 `RebuildRandom`。C0b 的 `SimulationHarness.cs` 加了钩子 `RebuildJobExportTrace` 的声明与两处调用（`FinishJob` 导出；`Complete` 失败时只写 `params`）。新增 internal 成员 `RebuildTraceFlush()`。这些调用点与钩子不在设计稿 7.4 的表里，需要 Z1 补进去。
- `FromJson`、`Validate` 改为 public：编辑器程序集（导入入口）看不到 `Assembly-CSharp` 的 internal 成员。
- 精确读取请求参数（第二轮评审）：C0b 的 `SimulationRunRequest.Load` 多一行调用 `ReadExactParams`；新增 public 的 `Job.ParamsReadError`（不序列化）、`SimulationRebuildParams.FromRequest`（加载函数的请求分支挪到这里，不依赖 UnityEngine，`rebuildcheck` 能测）、`SimulationCanonicalJson.ParseToken`、`JobParams`、`DecimalToDouble`。`rebuildcheck` 输出 `NextDouble` 的结果改用 `FormatDouble`（Mono 的 `"R"` 不能往返）。
- C0b 的 `tufu_balance/unity/request.py` 新增 `register_job_check`（`validate` 末尾调用登记的作业检查），C3c 用它登记 `trace.check_job_params`。P10 的 `tests/test_calibration_harness.py` 里驱动测试的占位参数 `{"Kappa": …}`（类里还没有这个字段）改成 `{"FastAfkStepMinutes": …}`，否则会被新检查拒绝；生产路径上 `resolve()` 本来就拒绝未知字段。
- 请求的部分参数对象仍按字段覆盖代码默认值（见上），没有改成拒绝：C3b 的引擎测试（`modifier-first`、`stale-tick-cache`）就是靠部分对象只改一个字段；正式运行带完整对象，`manifest.params.complete` 可查。是否在批处理里强制完整对象留给 Z1 决定。
- 设计稿 2.6 写 36 项属性；引擎的快照与摘要都是 `Unit.Stats` 的 30 项（同 16.6）。
- 改了别的单元的测试：C3a/C3b 的 `tests/test_cs_signature_lock.py`（`PARTIAL_FILES` 加 `Trace`，`NEW_MEMBERS` 加 `RebuildTraceFlush`，新文件的 `.meta` 检查加上本单元的文件）、C3b 的 `tests/test_engine_static.py`（`FromJson` 可以是 public）；C0b 的 `tufu_balance/unity/__init__.py`（导入 `trace` 以登记字段）、`__main__.py`（`params build|import`）。

## 18. 确定性（C3d）

测试在 `Balance/tests/unity/test_determinism.py`，黄金套件 `sim_determinism`。作业都是第 0–6 天（`stop.day = 6`）、开 `digest_text`；默认策略（不带 `engine_probe`），所以 C5–C7 填好策略后重跑本文件就是对它们的确定性检查；另有一个 `null_policy` 作业 `det-null`，它第 0–6 天的摘要链记进黄金套件，作为跨会话、跨提交的锚点（与 C3c 的 `stop-6` 相同）。

### 18.1 运行

| 启动 | 请求 | 用途 |
|---|---|---|
| 基准 | 速度 500，三个作业 `det`、`det-again`（与 `det` 相同）、`det-null` | 同进程两作业；复制到临时目录后与下一次比较 |
| 重复 | 与基准完全相同，`force` 重跑（真实时刻更晚）| 两次启动逐字节相同 |
| 预置 | 基准加 `prefs_seed = {TimeSkipSeconds: 259200.0}` | 预置 `TimeSkipSeconds`、不同真实时刻 |
| 速度 | 各一个作业：`speed-1`（`frame_budget_ms = 1e-6`）、`speed-9`、`speed-500`、`speed-paced`（速度 8、预算 2 毫秒）| 速度无关、自动调速 |

`simulation_speed`、`frame_budget_ms` 不进 `run_key`（设计稿 1.4.2），所以每个速度变体用自己的作业 id，各是一次独立的运行，不会命中速度 500 的缓存。

逐字节比较的是运行目录里的全部文件，只除去第 10 节列出的带时间或进程号的文件：运行级的 `launcher.json`、`editor_report.json`、`sandbox.json`、`unity.log`，作业级的 `timings.json`、`unity.log`，以及测试自己用 `simio.ingest` 生成的 `sim.sqlite` 和启动器的 `_real_backup/`。一次三作业的启动比较 35 个文件（每个作业 11 个，加 `request.json` 和就绪核对表）。`simio.ingest` 从两次启动的作业目录建出的 `sim.sqlite` 也逐字节相同。

### 18.2 验收记录（设计稿 7.3 C3d）

在提交 `e0d2fe4` 上，`pytest --unity tests/unity/test_determinism.py`：4 通过；首次全部新跑 126 秒（7 次启动：三作业的约 23–24 秒，单作业的 12–13 秒），之后只有 `force` 的重复启动要跑，约 26 秒。运行期间主检出没有开着的编辑器（`main_checkout_editors` 为空）。

| 项 | 结果 | 证据 |
|---|---|---|
| 速度 1、9、500 与自动调速，第 0–6 天摘要相同 | 通过：四个变体 7 天的摘要全部相同，也与基准启动的 `det` 相同；作业目录的文件（除 `timings.json`、`unity.log`）逐字节相同，`manifest.json` 除去请求回显（`run_key`、`request`、`job`、`job_id`、`job_index`）后相同。实际节奏：`speed-1` 67 帧、每帧 1 个 Tick（速度始终 1）；`speed-9` 4 帧、速度 9→36；`speed-500` 1 帧；`speed-paced` 52 帧、速度在 1 到 8 之间随帧耗时变化。全部 67 个 Tick | `test_digests_do_not_depend_on_the_simulation_speed`；黄金 `speeds.*` |
| 两次启动的全部输出逐字节相同 | 通过：同一请求在不同真实时刻（相隔数十秒，测试断言后一次的启动时刻更晚）的两次启动，35 个文件全部逐字节相同 | `test_two_launches_are_byte_identical`；黄金 `launches.differing_files` |
| 其中一次在不同真实时刻、预置 `TimeSkipSeconds` 非零 | 通过：`prefs_time_skip_seconds = 259200`、`prefs_seed_seen` 读回预置值，`time_service_cheat_offset_s = 0`、`TimeService.Now` 仍是沙箱时刻；除 `request.json` 与三个 `manifest.json` 外的文件全部逐字节相同，这两类文件只在请求回显（`prefs_seed`、`run_key`、`out_dir`）和沙箱报告的两个预置读回字段上不同 | `test_time_skip_preset_and_real_time_change_nothing`；黄金 `time_skip.differing_files` |
| 同一编辑器进程里连跑两个相同作业（`reenter_play`），摘要相同 | 通过：`det` 与 `det-again` 7 天摘要相同、作业开始的状态摘要相同，文件逐字节相同（`asset_guard.json` 去掉 `job_id` 后），`manifest.json` 除作业 id 与序号外相同 | `test_two_identical_jobs_in_one_editor_process`；黄金 `same_process.*` |

### 18.3 找到的不确定性与修正

- **`asset_guard.json` 依赖速度与帧耗时**（已修正，偏离：改了 C0b 的 `Harness/SimulationHarness.cs`）。资产守卫在作业后把 Simulation Config 的内存摘要写进 `asset_guard.json` 的 `changed_sha256`；`SimulationSpeed` 是这个资产对象的序列化字段，而作业里它先被设成请求的速度、再由帧预算按墙钟帧耗时调整。所以这个文件随请求的速度变（C3c 的运行：速度 3 的 `stop-6` 是 `a3cef66d…`，速度 500 是 `e4bfe673…`），作业跨多帧、调速生效时还随机器负载变。修正：`StartJob` 记下作业前的 `SimulationSpeed`，`Complete` 时恢复（成功与失败都经过 `Complete`）。修正后四个速度变体的 `asset_guard.json` 除 `job_id` 外逐字节相同。守卫之后照样把资产卸载重载，不影响模拟结果。
- 没有发现别的不确定来源：引擎、导出、轨迹、就绪表、manifest 在上面的所有比较里都相同。

### 18.4 与设计稿不同的地方

- 设计稿把"速度 1、9、500"和"帧预算自动调速"分开写，但驾驭器总在调速（第 11.2 节），`frame_budget_ms` 必须大于 0，没有固定速度的开关。`speed-1` 用 `frame_budget_ms = 1e-6` 把速度压在 1（每帧 1 个 Tick）；`speed-9` 用默认预算，前几帧 9 个 Tick，之后翻倍到 36；`speed-500` 一帧跑完。真正按帧耗时上下调的是 `speed-paced`，测试要求它的速度确实变过（最小值 < 最大值）。
- "两次启动的全部输出逐字节相同，其中一次预置 `TimeSkipSeconds`"：预置值在请求里，`request.json` 和 manifest 的请求回显必然不同，所以拆成两项：同一请求的两次启动逐字节相同；预置的那次除请求回显与预置读回外相同。多跑了一次启动。
- 比较"全部文件"时除去第 10 节本来就说明不能逐字节比较的文件（时间、进程号、日志），见 18.1。
- 设计稿 2.6 写 36 项属性；摘要是 `Unit.Stats` 的 30 项（同 16.6、17.7）。

## 18. 状态覆盖：replay 与 checkpoint（C4）

C4 填了 `Systems/SimulationSystem.Replay.cs` 的两个钩子 `RebuildApplyDayOverride(int)`、`RebuildApplyReplayCaches()`，新建 `Harness/SimulationStateOverride.cs`（读随包快照、构造每天的状态、记录；也实现 `SimulationHarness` 的钩子 `RebuildJobApplyOverride`）和 `Harness/SimulationRunRequest.Override.cs`（请求字段）。Python 一侧是 `tufu_balance/unity/override.py`。测试在 `Balance/tests/unity/test_state_override.py`、`tests/test_state_override_static.py`，黄金套件 `sim_override`。

### 18.1 请求字段 `job.override`

`mode` 为 `replay` 或 `checkpoint` 的作业读它（为 null 时全取默认值）；`free`、`empty` 作业忽略它，manifest 只记 `{"schema", "mode", "active": false}`。

| 字段 | 取值（默认）| 含义 |
|---|---|---|
| `from_day` | 整数 ≥ 0 或 null（0）| 从这一天起覆盖 |
| `econ_caches` | `stored`（默认）、`recomputed` | `stored`：当天的 DpsData/DefenseData 取（插值）快照的值；`recomputed`：由已恢复的 `CalculateDpsData`/`CalculateDefenseData` 从物品和槽位重算 |
| `replay_items` | `snapshot`（默认）、`recompute` | `snapshot`：每次覆盖都设物品；`recompute`：只在第一次覆盖时设，之后由物品模块（C6）按击杀累积 |
| `unlock` | `derived`（默认）、`snapshot` | `derived`：岛只由门禁的可杀性解锁（策略）；`snapshot`：解锁到快照的 `Location`（诊断用）|
| `total_value` | `snapshot`、`compose` 或 null（`replay` 取 `snapshot`，`checkpoint` 取 `compose`）| `snapshot`：属性的 TotalValue（`Stat.FastValue`）等于快照自己的 TotalValue（插值日是插值值）；`compose`：由 Value 与当天 K2/K3 修饰器合成（与强制快照的写法相同）。`checkpoint` 给 `snapshot` 时请求被拒：校正修饰器会留到闭环的后几天（K2/K3 已经变了，C9 的 K 补丁也会被拉回随包值）|
| `rpm_after_override` | `recompute`（默认）、`skip` | `recompute`：覆盖之后在新状态上再算一遍当天的 RPM（18.2 第 6 步）；`skip`：不算，`RPMCalculatedValue` 只有换天时算的值 |
| `items_evolved` | `{"<TC>": {"<Key>": true/false/null}}` 或 null | 物品是否已进化，按随包天快照的 TimeChunkIndex 给。缺这一天、缺这件物品或值为 null 时按 `Level != Copies` 判定，并计数（`items_evolved_fallback` 等）|
| `slots` | `{"<TC>": {"weapon": n, "armor": m}}` 或 null | 武器槽、护甲（头盔+盾）槽的个数；null 表示不动槽位。超出玩家已有槽位数（武器 5、护甲 2）时作业以 5 失败 |

- 键都是**随包天快照**的 TimeChunkIndex（46 个）。某一天用"提供物品的那一侧快照"的键（见 18.2），物品、进化标志和槽数因此总是同一个快照的。
- Python：`override.default_override(con, **选项)` 填好 P4 的 `items_evolved`（`items_evolved_default`，未定的记为未进化）和夹具 `stage4_slot_schedule.json` 的 `slots`（`slots_fixture`，拟合值）；`slots_p12(con)` 给闭式 P12 规则下各快照 Location 的槽数（设计稿 1.4.2 的 `slots_rule = "p12"` 就用它代替，C# 不另设字段）。`override_job(job_id, mode, override, probe=…, **make_job 参数)` 生成作业；`request.validate` 经 `register_job_check` 做与 C# 相同的检查（未知字段、取值、键的写法、槽数非负）。
- 典型请求（第 4–405 天开环重放，空策略）：

```python
from tufu_balance.unity import engine, override as ov, request as req
o = ov.default_override(con, from_day=4)
job = ov.override_job("replay-405", "replay", o, probe=engine.engine_probe(null_policy=True), day=405)
request = req.make_request([job])
```

### 18.2 每天做什么

覆盖在第 d 天的强制快照**之后**生效（C3b 的顺序，16.6），第 0 天在快照 0 之后。哪些天：

- `replay`：每个 d ≥ `from_day`。状态取 `config.SimulationData.GetByTimeChunkIndex(d)`（已恢复代码，随包资产的 46 个天快照）。
- `checkpoint`：只在随包天快照所在的 d（≥ `from_day`），状态就是那个快照本身；两次之间闭环照跑。

同一天只覆盖一次（再调用记 `repeat_day_calls`，不做事）。每次覆盖依次：

1. `unlock = snapshot`：对每个新岛 n，打开通向 n 的**前向**门禁（刷怪点所在岛 < n；11、12、13 岛通回 8、7、2 岛的回门不开，它们仍只在 C3b 清掉该刷怪点时打开），打开这些门禁刷怪点的 `SlotReward` 槽位，再 `RebuildUnlockLocation(n)`（C3b 的解锁不开槽，16.2）。
2. 槽位：按 `slots` 把武器槽、护甲槽的解锁个数设成给定值（非 VIP 优先、按配置顺序；关槽前先卸下装备）。计数 `slots_opened`、`slots_closed`、`slot_unequips`；记录里的 `slot_states` 给出每个槽位。
3. 物品：玩家物品变成快照的物品，**顺序也相同**（`CalculateDpsData` 的平局按列表顺序）：已有的 ItemModel 原地改，新的经 `AddItem(…, applySilently: true)` 加入，多余的卸下后去掉（经反射设 `PlayerModel.Items`，`RemoveItem` 是空方法）。已进化：`Evolved = true`、`Level = 快照 Level`；未进化：`Level = 1`、`Copies = 快照 Copies`（快照的 Level 是有效等级）。
   - **提供物品的那一侧**：`GetByTimeChunkIndex` 不插值物品，返回 `left.Items`。所以插值日取左侧快照的物品；恰好落在随包天快照 k（不是第一个或最后一个）时，t = 1，属性是 k 的，物品却是前一个快照的；d ≥ 405 取最后一个。记录里的 `items_time_chunk` 就是这个快照。
4. 属性：每项属性去掉两个 `__simulation*` 修饰器和装备（`Item`）修饰器以外的全部修饰器（以及上一次覆盖的 TotalValue 校正），基础值设为快照的 `Value`，于是快照 Value 逐位等于它；当天的 K2/K3 修饰器（换天时已换成 K(d)）保留，TotalValue 先由 Value 与 K(d) 合成。
   - `total_value = snapshot`（默认）：再把 `Stat.FastValue` 校正到快照自己的 TotalValue。插值日的 TotalValue 是线性插值的，K2/K3 不是，合成值与它最多差 1.98%（Health；风险 18 说可杀性边界 ±3% 内有 30 多个刷怪点/天）。校正用两个 `Item` 来源的 `PreAdditivePostMultiplicative` 修饰器（名字 `__rebuildReplayTotalValue1/2`）：`Item` 修饰器不进快照 Value，Value 仍逐位；`StatModifier.Value` 是 float，两个 float 在 double 里相加，第二个在相邻 float 里挑误差最小的，结果最多差 1 ulp。计数 `total_value_corrections`、`total_value_corrections_inexact`（差 1 ulp 的格子）。当天之后装备一变（`ApplyEquippedItemsToUnit` 去掉全部 `Item` 修饰器）校正就没了。
5. 血量回满（`CurrentHealth = Health`）。
6. 缓存与 RPM：`CalculateSimulationData()` → `RebuildApplyReplayCaches()`；`rpm_after_override = recompute`（默认）时再 `CalculateSoulRPMValues()` 与每项属性的 `CalculateRPMValueTimeChunk`。换天时的 RPM 是覆盖之前、按上一天状态算的（C3b 的换天不在覆盖之后重算），所以覆盖自己再算一遍，当天的 `RPMCalculatedValue` 对应覆盖后的状态（K(d)，`stored` 时在存储的缓存上算）。已恢复的 `CalculateRPMValueTimeChunk` 保留最大值（`Math.Max(value, RPMCalculatedValue[t])`），所以这个值会**带到下一天**：第 d+1 天换天算出的 RPM 与它取大，第 d+1 天强制快照的 `CurrentGpm` = max(第 d 天覆盖后的 RPM, 换天自己的重算值)。`replay-405` 里 1604 个非零格子有 750 个严格取了覆盖的值；`checkpoint` 检查点的下一天 179 个格子里 0 个（`skip` 时 0 个）。`checkpoint` 的闭环因此以检查点那天 K(d) 下的 RPM 为下限，直到更大的值出现（属于本单元的偏离，18.5）。

`RebuildApplyReplayCaches`（`econ_caches = stored` 且当天有存储的缓存时）：用快照的 DpsData/DefenseData 替换 `currentDpsData`/`currentDefenseData`，再 `UpdateDpsDataCache`、`RebuildPerFrameCaches`（防御缓存与战斗乘数）、`CalculateSpawnerData(true)`。它在 `Tick` 与（C5 的）FastTick 规划轮每次 `CalculateSimulationData` 之后被调用；`replay` 每天都有存储的缓存，`checkpoint` 只在重置的那一天有。

注意：

- 第 d 天的强制快照在覆盖之前拍，记录的是上一天覆盖之后的状态（空策略下就是第 d−1 天的插值状态）。它的 DpsData/DefenseData **总是**换天时重算的值：C3b 的换天（`RecalculateChunkRpmValues` → `CalculateSimulationData`）不调用 `RebuildApplyReplayCaches`，而且那时天已经换了，存储的缓存只对覆盖的那一天有效。重算用的是第 d−1 天覆盖后的属性（RpmFirst：K2/K3(d−1) 加校正，即 `override_days.jsonl` 里那天的 `GetForecastStatValue`）、物品、槽位，时刻 d；402 天里 401 天与第 d−1 天的存储值不同（黄金 `replay405.forced_dps_differs_from_stored`）。换天的 RPM 也是在这些重算的缓存上算的，但它与前一天覆盖后的 RPM 取大（第 6 步），所以 `CurrentGpm` 不是单纯的换天重算值；`rpm_after_override = skip` 时才只剩游戏自己的取大。要看覆盖后的状态读 `override_days.jsonl`。
- 交互运行（没有批处理作业）与 `free` 作业里两个钩子直接返回。

### 18.3 输出

- manifest `override`（`tufu.sim.override/1`）：`mode`、`active`、`from_day`、`econ_caches`、`replay_items`、`unlock`、`total_value`（解析后的值）、`rpm_after_override`、`items_evolved`（`request` 或 `level_ne_copies`）、`slots`（`request` 或 `unchanged`）、`shipped_asset`、`shipped_time_chunks`、`file`、`calls`（钩子被调用的天数）、`applied`（实际覆盖的天数）、`first_day`、`last_day`、`error`、`counters`（`cache_applications`、`items_added`、`items_removed`、`items_reordered`、`unlocks`、`gates_opened`、`slot_rewards_opened`、`slots_missing`、`slots_opened`、`slots_closed`、`slot_unequips`、`total_value_corrections`、`total_value_corrections_inexact`、`total_value_uncorrected`、`items_evolved_*`、`unevolved_level_ne_copies`、`removed_modifiers_source_<n>`、`stored_caches_other_day`（在设缓存那天以外的天调用 `RebuildApplyReplayCaches`，没有用存储的缓存）等）。引擎计数器另有 `day_overrides`。
- `override_days.jsonl`（输出开关 `override_days`，replay/checkpoint 默认开）：每次覆盖一行，覆盖完成后引擎的状态。double/float 写成十六进制位（`SimulationRuntimeProbes.D/F`），`override.read_days` 解码。字段：`day`、`time_chunk_index`、`kind`、`snapshot_time_chunk`/`snapshot_location`（`GetByTimeChunkIndex` 返回的）、`left`/`right`/`t`（插值区间，端点外为 −1/null）、`items_time_chunk`、`items_set`、`slots_set`、`stored_caches`、`location_before`、`location`、`current_boss`、`weapon_slots`、`armor_slots`、`slot_states`（每个槽位 `[Key, SlotType, IsVip, IsUnlocked, 装备的物品 Key 或 null]`，配置顺序）、`removed_modifiers`、`k2`/`k3`/`modifier_day`、`current_health`、`stats`（每项 `[Key, 快照 Value, Stat.FastValue, GetForecastStatValue]`）、`items`（`[Key, Level, Copies, Evolved, EffectiveLevel]`）、`dps`/`dps_cache`/`defense`/`defense_cache`、`rpm`。文件在作业运行中逐行追加。
- 预言机 `override.expected_day(shipped, mode, day, items_evolved)`：用 `model.snapshots` 独立给出某天应设的快照、物品来源和进化标志。

### 18.4 验收记录（设计稿 7.3 C4）

最初的验收在 `74bf236` 上（`accepted/sim_override.jsonl` 第一条记录的 `git_rev` 是 `79bcf84`：那是修订前的提交，C# 与 `74bf236` 相同，后来被 amend 掉，已不在任何分支上）。第一轮评审修正之后在 `7a9ae47` 上重跑（11 个作业）。第二轮评审修正之后在 `e4bf8ec` 上重跑 `pytest --unity tests/unity/test_state_override.py`：一次 Unity 运行 13 个作业（全部空策略），另有一次失败型运行，18 个测试全部通过；新键在同一提交上接受（`--seed-original`，设计稿没有对应的数）。

| 项 | 结果 | 证据 |
|---|---|---|
| `replay` + `stored`：每天覆盖后 DpsData/DefenseData 缓存（列表与字典）与插值快照逐位相等 | 通过：第 4–405 天 402 天，Values、四个缓存、物品、槽位、`currentBoss`（P6 `boss_for_location(1)`）全部逐位，0 天不符 | `test_replay_once_per_day_with_the_interpolated_state` |
| 属性 TotalValue 与插值快照一致（逐位，最差 1 ulp）| 通过（`total_value = snapshot`，默认）：12060 个格子最差 1 ulp，633 个差 1 ulp，其余逐位；随包天快照所在的天 0 ulp；插值日最大相对差 2.2e-16。**偏离**：设计稿的写法（基础值 = 快照值、清掉奖励修饰器）做不到，要靠 18.2 第 4 步的校正修饰器。`compose` 作业保留原来的口径：对 `(Value·(1+K2))·K3` 最差 1 ulp（251 格），对插值 TotalValue 最大相对差 1.98%；`Stat.FastValue` 与游戏顺序 `((1·K3)·(1+K2))·Value` 12060 格逐位 | 同上；`test_replay_compose_total_value`，黄金 `replay405.total_value_*` |
| 物品取提供物品的那一侧，`Evolved` 取请求值 | 通过：物品（Key、有效等级、Copies、Evolved）与预言机的左侧快照逐天相同，ItemModel 的 Level 未进化时为 1；P4 的标志覆盖全部物品（没有回退计数）。P4 的标志在 46 个快照上与 `Level != Copies` 处处相同，所以另有作业 `replay-evolved-flip`（第 17–21 天）把标志两头反过来：TC 17 的 knife（117/117）记为已进化，TC 19 的 knife（1/86）记为未进化。第 18、19 天 knife 为（Level 117、Copies 117、已进化、有效等级 117），第 20、21 天为（1、86、未进化、86），下一天强制快照的物品等级是有效等级；`unevolved_level_ne_copies` = 2，没有回退计数 | 同上；`test_evolved_flags_come_from_the_request`，黄金 `evolved_flip.*` |
| 覆盖后的 RPM | 通过：每天的 `RPMCalculatedValue` 不低于 `model.simcalc.calculate_rpm_value_time_chunk` 在记录的状态（预测属性、缓存、岛、天）上的值且不下降；replay 14472/14472、checkpoint 1656/1656 个格子逐位等于它 | 同上、`test_checkpoint_resets_at_shipped_day_snapshots` |
| 换天的 `CurrentGpm` | 通过：第 d+1 天强制快照的 `CurrentGpm` 逐位等于 `simcalc` 从第 d 天记录的 RPM 出发、在第 d 天记录的状态与该快照的 DpsData/DefenseData 上、第 d+1 天算出的值（即两者取大）。`replay-405` 1604 个非零格子 0 处不符，750 个严格取了覆盖的值；`rpm_after_override = skip`（第 4–30 天）的记录 RPM 等于当天强制快照的 `CurrentGpm`（覆盖不动 RPM），0 个格子取了覆盖的值；`checkpoint-405` 检查点下一天 179 个格子 0 处不符 | `test_day_change_rpm_keeps_the_override_rpm`，黄金 `*.day_change_gpm_*` |
| 每天恰好一次覆盖 | 通过：记录的天正好是 4..405 各一次；`calls` = 406，`applied` = `day_overrides` = 402，没有 `repeat_day_calls` | 同上 |
| 第 4–405 天空策略重放 < 2 分钟 | 通过：作业 0.90 秒（`job_wall_s`；FastTick p50 1.6 毫秒，含每天的覆盖、校正与 RPM 重算）| `test_replay_405_runs_in_under_two_minutes` |
| 覆盖的状态带到下一天 | 通过：第 d+1 天强制快照的 Values 与物品等于第 d 天的记录；它的 DpsData/DefenseData 逐位等于按第 d 天记录的预测属性、物品、槽位、时刻 d+1 重算的值（18.2 的注意）| `test_replay_state_carries_to_the_next_forced_snapshot` |
| 去掉奖励修饰器 | 通过：C3b 的 `grant_flat`（Health +500、StabDamageFlat +3，`Reward` 来源）在第 0 天强制快照里可见；第 0 天的覆盖去掉 2 个，Values 等于快照 0，第 1 天强制快照里不再有 | `test_override_removes_reward_modifiers` |
| `checkpoint`：随包天快照处 Value 与物品等于随包值，之后闭环 | 通过：46 个天快照各覆盖一次，Values、物品、缓存、槽位逐位，TotalValue 0 ulp（`total_value` 解析为 `compose`，没有校正修饰器）；之后的**每个**强制快照（1..405 共 405 个）Values/物品仍是上次检查点的，DpsData/DefenseData 逐位等于 `model.simcalc` 按物品、槽位、K2/K3(e−1)、时刻 e 重算的值（44 个检查点的下一天与存储值不同）| `test_checkpoint_resets_at_shipped_day_snapshots`、`test_checkpoint_continues_closed_loop` |
| `checkpoint` + `from_day` | 通过：`from_day = 20` 时第 0–21 天的强制快照保持初始值 + grant，第一次覆盖在 21（第一个 ≥ 20 的天快照），去掉 2 个修饰器，第 22 天强制快照等于随包 21 | `test_checkpoint_from_day_after_a_closed_loop_start` |
| `econ_caches` 可切换 | `recomputed`：第 4–30 天四个缓存都等于 `simcalc` 按记录的预测属性重算的值（逐位），27 天里 27 天与存储值不同；RPM 检查同样通过；没有 `cache_applications` | `test_econ_caches_recomputed` |
| `replay_items` 可切换 | `recompute`：只有第一次覆盖设物品，之后物品不变（空策略不掉落），属性仍每天覆盖 | `test_replay_items_recompute_sets_items_once` |
| `unlock` 可切换 | `snapshot`（不给 `slots`）：每天 Location、`currentBoss`、槽数按 P6；解锁 8 次，开门 13 个（等于 P6 里刷怪点所在岛 < 目标岛的门数；回门 3 个不开），开槽 3 个 | `test_unlock_snapshot_opens_islands_slots_and_boss` |
| 槽位减少 | 通过：槽数 5/2 → 2/1 → 1/0 → 不变（缺这一天的键，`slots_missing` = 1）；开着的槽是按（VIP、配置顺序）排的前 n 个，关掉的槽没有物品，`slot_unequips` = 4 等于前一天有物品、这一天关掉的槽数 | `test_slots_close_and_unequip` |
| 三个选项与 `total_value`（解析后）、`rpm_after_override` 记进 manifest | 通过（13 个作业里的 11 个覆盖作业）| `test_options_are_echoed_in_the_manifest` |
| `checkpoint` 拒绝 `total_value = snapshot` | 通过（Python 与 C# 的校验相同；C# 一侧只核对源码）| `tests/test_state_override_static.py::test_checkpoint_rejects_the_total_value_correction` |
| 慢阶段每个 Tick 都用存储的缓存 | 通过 | `test_stored_caches_hold_through_slow_ticks` |
| `free` 作业忽略 `override` | 通过 | `test_free_job_ignores_the_override` |
| 槽数超出时作业失败 | 通过：退出码 5，`error` 含 "exceed" | `test_slots_beyond_the_player_fail_the_job` |

现有测试走不到的路径：`items_removed`、`items_reordered`（随包 46 个快照的物品只增不减、顺序不变，空策略也不得物品；13 个作业里这两个计数都没出现）；`TryStoredCaches` 的"只在设缓存的那天生效"（第 4 天以后只有覆盖本身调用 `RebuildApplyReplayCaches`，慢阶段的天在 `checkpoint` 下全是检查点；现在别的天的调用记 `stored_caches_other_day`，13 个作业里都是 0）；非 `Reward` 来源修饰器的 `removed_modifiers_source_<n>`（没有探针能加）；`checkpoint` 加 K 补丁（本分支没有 C9 的补丁钩子）。见 18.6。

### 18.5 与设计稿不同的地方

- 快照 `Items` 的 Level 改为 `EffectiveLevel`（越过所有权改了 C3b 的文件，需调度者确认、Z1 合回；C3b 的 `.Snapshot.cs` 写的是 `item.Level`，未进化时恒为 1，与设计稿 4.5"有效等级"和随包数据 `Level == Copies` 不符；空策略下只有 knife 1/1，看不出来）。`items_ext.csv` 的 `level` 同样改为有效等级，`evolution_level` 仍是 `Level`（已进化时）。改的是 C3b 的文件。
- 新字段 `total_value`（null 时 `replay` 取 `snapshot`、`checkpoint` 取 `compose`；`checkpoint` 不许 `snapshot`）：TotalValue 靠两个 `Item` 来源的校正修饰器等于快照的 TotalValue（18.2 第 4 步）。设计稿 4.6 只写"基础值 = 快照值、清掉奖励修饰器"，那样插值日的 TotalValue 只能是 K(d) 合成值，最多差 1.98%。
- 新字段 `rpm_after_override`（默认 `recompute`）。因为 RPM 取大，覆盖的重算会带到下一天的 `CurrentGpm`，`checkpoint` 的闭环也以检查点那天 K(d) 下的 RPM 为下限（18.2 第 6 步）；`skip` 去掉这一点。设计稿没有这一项，Z1 决定默认值。
- `unlock = snapshot` 只开前向门禁（回门不开）。
- 覆盖之后重算当天的 RPM（设计稿 4.6 没有说；`rpm_after_override = skip` 可关）；换天时的 DpsData 仍按覆盖之前的状态（C3b 的顺序不变），换天的 RPM 与覆盖的 RPM 取大。
- 越过所有权改了别的单元的文件：C3b 的 `SimulationSystem.Snapshot.cs`（见上面第一条）、C3a 的 `tests/test_cs_signature_lock.py`（期望 Replay 的两个钩子已填）、C0b 的 `tufu_balance/unity/__init__.py`（导入 `override` 以注册 C4 的请求字段）。需调度者确认、Z1 合回设计稿与各单元的记录。
- 4.6 表里"清掉奖励修饰器"实现为：去掉两个 `__simulation*` 和装备以外的**全部**修饰器（非奖励来源的按来源计数 `removed_modifiers_source_<n>`；新档上一个也没有）。
- `slots` 的键是随包天快照的 TimeChunkIndex，某天取提供物品的那个快照的槽数；`slots_rule = "p12"` 不进请求，由 Python 的 `slots_p12` 展开成 `slots`。
- `items_evolved` 缺失时回退到 `Level != Copies`，并计数。
- 请求里的 `override` 对 `free` 作业不报错，只是不生效（设计稿 1.4.2 的示例就是 `free` 作业带 `override`）。
- `checkpoint` 的 `econ_caches = stored` 只在重置的那一天用存储的缓存；`from_day` 对 `replay` 同样生效。
- 两个钩子的出处标为 `Provenance.DebugOnly`、公式 id `pending:simulation.state_override`：它们是校准用的教师强制，不是游戏行为。

### 18.6 交给后续单元

- P11（DP8）：46 个随包天快照状态用 `checkpoint` 作业设好（`override_days.jsonl` 的记录是覆盖之后、RPM 重算之后的状态）。**必须**给 `unlock: "snapshot"`：默认的 `derived` 不动 Location，空策略下玩家一直在岛 1，`spawnerData` 与 RPM 就不是随包状态的；槽数用夹具（`default_override` 的默认）或 `slots_p12`。注意 DP8 按 `RpmFirst` 口径要 K2/K3(d−1) 下的 RPM，而覆盖后重算的 RPM 用的是 K(d)，且与之前的 RPM 取大；需要滞后口径时给 `rpm_after_override: "skip"`，在 P11 自己的探针里调用 `CalculateRPMValueTimeChunk`。
- P11、K 单元（T09"重放精确 1380/1380"）：`replay` 作业导出的强制快照的 DpsData/DefenseData 是按物品**重算**的（18.2 的注意），拿不到 stored 口径；换天 RPM 与 `CurrentGpm` 在重算的缓存上算，再与前一天覆盖后的 RPM（K(d)、stored 缓存）取大，`replay-405` 的 1604 个非零格子里 750 个是后者。要 stored 口径：要么由 Z1/C3b 在换天的 `CalculateSimulationData` 之后也接一个用前一天存储缓存的钩子（`TryStoredCaches` 现在按当天匹配，那时天已经换了），要么直接用 `override_days.jsonl`（覆盖后、stored 缓存上的状态与 RPM）或自己的探针。
- C5：FastTick 每个规划轮在 `CalculateSimulationData()` 之后调用 `RebuildApplyReplayCaches()`（设计稿 4.4），`replay` + `stored` 才会在存储的缓存上规划。换天的 `RecalculateChunkRpmValues` 不调用它。接上之后请补一个测试：`checkpoint` 下非检查点那天的规划轮不得用到存储的缓存（`TryStoredCaches` 的按天判断现在测不到）：断言 `stored_caches_other_day` > 0，且 `cache_applications` 只出现在检查点那天。
- C6：`replay_items = recompute` 时物品只在第一次覆盖时设，之后靠 `ProcessInventory`；覆盖用 `AddItem(…, applySilently: true)` 加新物品，会自动装备到空槽。快照 `Items` 的 Level 现在是有效等级。装备变化（`ApplyEquippedItemsToUnit`）会去掉 TotalValue 校正，当天剩下的时间回到 K(d) 合成值。`items_removed`（玩家有、快照没有的物品被去掉）与 `items_reordered` 要等 C6 让玩家得到快照外的物品后才测得到。注意：去掉物品是卸下后经反射重设 `PlayerModel.Items`，`AddItem` 建的存档条目 `Data.Items` 不删、顺序也不改；若之后从存档重建（`LoadItems`），去掉的物品会回来、顺序会变回去。C6 测去掉与重排时要连同存档条目一起测，或在去掉时删掉绑定的条目。
- C9：`checkpoint` 加 K 补丁时 TotalValue 应是补丁后的 K 合成值（`total_value` 解析为 `compose`）；请在 C9 的补丁钩子接上后补这个 Unity 测试。
- C7：覆盖后 `CurrentHealth` 回满；`unlock = snapshot` 会打开门禁（`GatesModel`），被打开的门禁刷怪点不会再触发解锁。

## 19. 快阶段（C5）

实现、黄金结果、与调研脚本的差异和 P10 的充能数据接口见 [FastTick.md](FastTick.md)。验收代码提交为 `3f7f8282aa5391a355608014dea7cdb1285d74f3`；此后的提交只接受黄金结果并补文档。

### 19.1 第 4–405 天的耗时

运行键 `3367f41746c8fc42c47153bbf3ba2e511e654c1863adc26d5b5cc25ffd6e3f32`：18 个作业，一次启动器运行 308.51 秒。B 组单独运行键 `648627465d06f6e13f0a2ddca6abb16ee93814a04e7d6540907b7ae85e7d920d`，15.29 秒。两次 Unity 与启动器都以 0 退出，`guard_problems = []`，日志扫描 `error_like_lines = 0`，启动器核对运行工作树干净；没有写回场景或随包资产。

两个 405 天作业都实跑第 4…404 天的 401 个 FastTick，第 405 天拍强制快照后停止。表来自各作业 `timings.json.fast_tick_ms`，含规划、冲刷、离线、换天、快照与输出。

| 作业 | FastTick 样本 | p50 / 均值 / p95 / 最大值（毫秒）| 中位数预算 |
|---|---|---|---|
| `fast-405`，replay + stored、κ = 0.245，逐刷怪点导出开启 | 401 | 33.94 / 37.16 / 49.30 / 60.75 | 33.94 ≤ 50，通过 |
| `free-405`，当前 C6/C7 尚为空，逐刷怪点导出开启 | 401 | 16.26 / 16.37 / 20.66 / 40.37 | 16.26 ≤ 50，通过 |

机器有 10 核，测量期间 load 约 4–8；不是空闲测量。`perf` 测试因 load 超过 2.5 自动跳过，但上述原始中位数另行逐项检查均在 16.3 节的 50 毫秒预算内。均值与 p95 一并记录，不把最大值当中位数预算。C6/C7 集成后应在空闲机器重测，尤其不能把当前 free 作业的较低耗时当作完整闭环策略的成本。

### 19.2 验收命令与结果

在本单元 `Balance/` 下，解释器为开发工作树的 `.venv/bin/python`，`TUFU_BALANCE_DB` 指向只读共享数据库：

```bash
TUFU_UNITY_REV=3f7f8282aa5391a355608014dea7cdb1285d74f3 python -m pytest -q --unity tests/unity/test_fasttick.py
python -m pytest -q
python tests/golden/fixtures/generate.py --check
python -m tufu_balance.cscheck --worktree /Users/joyfort/MyProjects/Tufu-s5-C5
python -m tufu_balance.golden guard
```

- 新 C5 Unity 验收在首次记录两个 T04 上限键之前，仅 `test_t04_active_minutes` 因这两个新键没有 CURRENT 而失败，其余 17 个通过，1 个 perf 跳过；全部实际 Unity 作业均成功。经 `golden accept sim_fasttick --keys t04.median_rel_cap t04.max_rel_cap --loosen --reason ...` 显式登记偏离，保留 ORIGINAL 的 0.02 / 0.08。
- 接受后重跑 C5 Unity 验收：18 通过、1 个 perf 跳过，120.39 秒；复用上述成功运行，包含等待共享运行锁的时间，没有再启动 Unity。静态 C5 测试 10 通过。
- 全仓 pytest：774 通过、130 跳过，154.90 秒；跳过包括未带 `--unity` 的 Unity 测试、繁忙机器的 perf 和 LFS 场景检查。
- fixture：`identical`（来源模块的 provenance 变化单列）；cscheck：编辑器与非编辑器都编译成功，0 新警告、签名 0 违规、禁用 API 0；golden guard：16 套件、0 违规。
- C4 的交接补测通过：checkpoint 非检查点规划轮的 `stored_caches_other_day > 0`，只有检查点当天应用存储缓存。参考冲刷两种门禁规划模式都与默认冲刷摘要、完整天快照逐字节一致。
- `refresh --level 1` 不能运行：本分支还没有 P13 的 `tufu_balance.refresh` 模块（命令报 `No module named tufu_balance.refresh`）。以上分项检查不等同于声称运行了 refresh。

**T04 原阈值仍未通过**：实际中位误差 4.292634%、最大 8.585590%，登记黄金放宽仅冻结已知缺口。诊断数值与设计稿不同的原因、C6 的非零物品冲刷尚未覆盖的路径见 FastTick.md，交 Z1/K3/C6 后续处理。
