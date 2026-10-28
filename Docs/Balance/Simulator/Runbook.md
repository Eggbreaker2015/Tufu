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
- 预设 `smoke-11`、`golden-405`、`slow-5` 要等 C3b 填好引擎才有意义：现在 `free` 作业的 `ExecuteTick` 是空的，只会在 `max_wall_seconds` 停下。
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
- `progress` 只在作业、阶段、Tick 数、帧数（编辑模式下是编辑器的 update 计数）变化时才变。启动器看的是 `progress`：一帧再长，只要 Tick 在走就不算停；Tick 和帧都不动才判超时（退出码 3）。验收：一帧 12 秒、一直调 Tick 钩子的作业，6 秒的停顿超时没有触发；一帧 60 秒不调钩子的作业在 8 秒停顿后被杀，退出码 3。

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
- 就绪时的 `Cooldown`：沙箱时钟 2024-01-01T00:00:00Z 恰好是 UTC 零点，所以满员刷怪点的 `Cooldown` 是一个整周期（到下一个对齐时刻）而不是 0。作业开始、设 `IsRunning` 之前，把非活动、非 VIP 刷怪点的非零 `Cooldown` 清零：698 个（`ready_cooldowns_zeroed`）。`ready_spawners.csv` 记的是清零之前的值。C3b 若要自己管理 `Cooldown`（Q07），可以在请求里关掉 `zero_ready_cooldowns`。
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
