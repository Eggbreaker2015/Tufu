# 地图隐藏宝箱恢复

更新：2026-10-27。实现位于 `Recovery/StudyProject`，本次实际验证使用 Unity **6000.6.0f1 / Windows Editor / DX12**。宝箱使用原场景、模型、动画、UI、奖励和音效资源，C# 根据 APK 的 ARM64 程序重建。

从 Boot 进入 Play，靠近地图上的宝箱后点击 **打开**，在奖励页点击领取。已领取的宝箱保持开启，并在附近显示全局发现数量；重启后继续保留。

## 原版依据

输入文件为本地 `Recovery/com.multicastgames.butcher.pjb.apk`，SHA-256：

```text
c842d8d2674bc8e90894d5c01cd2094bde8f87c523e2e0bf3ee9221e2fe70b78
```

只读核对了 APK 的 2404 个成员，提取 `lib/arm64-v8a/libil2cpp.so` 和 `assets/bin/Data/Managed/Metadata/global-metadata.dat`。Metadata 版本为 31；方法地址通过 [Il2CppDumper 6.7.46](https://github.com/Perfare/Il2CppDumper/releases/tag/v6.7.46) 恢复，再用 Capstone 反汇编核对。下列地址均为该 APK 的 ARM64 ELF 虚拟地址。

| 系统 | 原版地址 | 恢复的行为 |
|---|---|---|
| 存档模型 | `0x140f480`、`0x1410f54`—`0x1411438` | 未知键查询不创建存档；已开计数读取真实保存条目；场景总数可响应更新。 |
| 初始状态与开箱外观 | `0x1410eec`、`0x14127e4`—`0x1412dd8` | Animator 参数为小写 `open`；读档后恢复开盖、光效和箱内物件状态。 |
| 距离判断 | `0x1412274`—`0x14127dc` | 每 0.15 秒更新，先比较三维直线距离，再采样两端 NavMesh；仅接受完整路径，比较路径总长；离开缓冲 1 米，采样半径 3 米。 |
| 世界提示与池复用 | `0x141184c`—`0x1412274`、`0x143c3b8`—`0x143c994` | 沿原锚点显示 UI，离开时播放隐藏动画，快速返回时取消隐藏并复用视图。 |
| 开启与领奖 | `0x1412de4`—`0x14143b8` | 先占用宝箱防重，隐藏 HUD 和世界 UI，禁止移动；近景镜头参数为 `3.4 / -15 / 1s`；触发 `map_chest_open`，等待 1 秒及后续 0.1 秒后显示原奖励页；领取回调记录开启并发奖。 |
| 提示文字与点击 | `0x1631314`—`0x1631688` | 原“打开”按钮、`SECRET_CHEST_FOUND_COUNT` 本地化文本及 `{value}/{total}` 数量。 |
| 属性图标与百分比 | `0x15cafa8`—`0x15cb074`，跳转表 `0x9226f0` | 恢复属性类型映射，修正所有属性奖励错误显示鞋子图标的问题；沿用原百分比显示规则。 |

属性奖励沿用 `secret_chest` 来源及 `ModifierSource.SecretChest = 16`。装备和灵魂继续通过现有 `GrantRewardCommand` 发放，不另造一套奖励或成长规则。商店宝箱已有独立实现，本次修改针对地图隐藏宝箱及其必要显示依赖。

## 源码入口

- [模型及存档](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/SecretChest/SecretChestModel.cs)
- [生成和读档状态](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/SecretChest/Systems/SecretChestSpawnSystem.cs)
- [导航路径距离](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/SecretChest/Systems/SecretChestProximitySystem.cs)
- [世界提示](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/SecretChest/Systems/SecretChestPromptSystem.cs)及[原按钮视图](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/UI/SecretChest/SecretChestPromptView.cs)
- [开启、领奖和保存](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/SecretChest/Commands/OpenSecretChestCommandHandler.cs)
- [隐藏动画取消](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/ProximityView/ProximityViewHideAnimation.cs)及[属性图标映射](../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/Unit/StatTypeExtensions.cs)

学习工程额外处理了未领取关闭、场景销毁和 Play 中断的清理：恢复镜头、移动和 HUD，未领取的宝箱允许重试；领取标记与奖励在同一次强制保存中写入。原宝箱数据、稀有度、奖励数值和 GUID 均未改动。本次只有脚本改动，无需重建 Addressables。

## 本次验证

| 检查 | 结果与范围 |
|---|---|
| 全场景配置 | 57 个宝箱、57 个唯一键；缺失引用、无法处理的奖励和缺失图标均为 0；57 个位置均可在原采样半径内找到 NavMesh。 |
| 独立规则 | 21 项通过：真实 MessagePack 往返、未知键、计数更新、重复标记、完整/部分路径、隔墙绕路、三维距离、精确距离边界、0.15 秒节拍及 1 米缓冲；属性映射覆盖全部枚举及非法边界。 |
| 正常鼠标操作 | 从 Boot 到 Main，实际点击附近 `30479630` 宝箱的“打开”和“点击领取”；奖励为生命 `+10`、斩击伤害 `+2`、生命 `+1%`，记录来源 16，计数变为 `1/57`。 |
| 重复与读档 | 同一已领取宝箱连续执行 5 次开启命令，属性和计数不增加；停止并重新 Play 后仍为已开状态，内容物关闭、光效停止、原奖励保留。 |
| 装备 | 受控移动到 `7f7ee689`，经原按钮双击开启并领取，`pike` 数量由 1 增至 2，另获原配置的穿刺伤害 `+1%`。 |
| 灵魂 | 受控移动到 `9b98009c`，领取原配置的 1 个神圣灵魂，数量由 0 增至 1。 |
| 中断与池复用 | 另有 7 项通过：取消隐藏、离开后快速返回、重复按钮事件、打开未领不发奖、关闭未领不消耗、重新打开、非法键不锁输入；开箱中停止 Play 后重启，未领取宝箱仍未打开，输入与 HUD 正常。 |
| 显示与编译 | 已查看 Unity 实际属性、装备、灵魂奖励页和发现计数；编译通过，最后正常运行及退出的 Console 错误为 0。 |

测试前备份了主存档和备份存档，测试结束后逐文件恢复并校验 SHA-256；测试用装备、灵魂、属性、位置及宝箱进度不写入最终用户存档。

上述跨岛奖励验证使用受控传送和原 UI 按钮事件；距离测试使用临时合成 NavMesh 和独立 Morpeh World。它们不代表自然解锁全部岛屿，也没有运行原 APK 做逐帧画面或音频对照。本次没有生成或验证 Android 安装包。

## 本地证据与复现

研究文件按现有仓库规则保留在本地，不作为游戏运行依赖：

- `Recovery/Evidence/chest-apk-inventory.json`：输入和 APK 成员校验。
- `Recovery/Evidence/ChestNative/secret-chests-arm64.txt`、`secret-chests-methods.json`：注释反汇编、方法边界和机器码哈希；也包含必要的图标与隐藏动画依赖。
- `Recovery/ChestValidation/config-audit.json`、`rule-checks.json`、`interruption-checks.json`：全配置、21 项规则、7 项中断检查结果。
- `Recovery/ChestValidation/` 下的 `baseline-*`、`claimed-*`、`restarted-*` 和 `Images/`：运行状态及截图。
- `Recovery/ChestValidation/session.json` 与 `UserSaveBackup/`：测试前存档备份和校验。
- `Recovery/RuntimeDiagnostics/Chest*Checks.cs`、`AuditChestConfig.cs`：通过 Unity Pipeline 的 `script run` 执行的本地验证脚本；不放入 Assets，不包含在构建中。

例如进入 Play 并完成 Boot 后，可只读核对全配置：

```powershell
unity command script run --file "E:\project\Tufu\Recovery\RuntimeDiagnostics\AuditChestConfig.cs" --entry AuditChestConfig.Run --json
```

会改变角色位置或领取奖励的检查必须继续使用备份测试流程；普通开发和游玩直接使用原场景按钮即可。
