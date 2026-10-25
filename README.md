# Tufu Unity 学习工程

从 Android IL2CPP APK 的资源、类型信息和原生代码证据重建的 Unity 工程，用于学习和继续开发。资源由 AssetRipper 2.0.0 导出，游戏方法、Shader 和运行依赖另行恢复；C# 是重建实现，并非原作者源码。

唯一开发工程是 [Recovery/StudyProject](Recovery/StudyProject)，使用 **Unity 6000.6.3f1**。原 APK 构建版本为 6000.2.6f2。

## 打开工程

1. 安装 Git LFS，克隆后在仓库根目录执行：

   ```powershell
   git lfs install
   git lfs pull
   ```

   `Assets/Scenes/Main.unity` 使用 LFS；完成下载后再打开 Unity。

2. 双击 `Recovery/Open-StudyProject.cmd`。Unity 安装在其他位置时，可从仓库根目录指定：

   ```powershell
   .\Recovery\Open-StudyProject.ps1 -UnityEditor "D:\Unity\6000.6.3f1\Editor\Unity.exe"
   ```

   启动脚本为已适配的 Mathematics/Burst DLL 提供 API 迁移保护，Burst 编译保持开启。

3. 等待首次导入、编译完成后按 Play。应从 [Boot](Recovery/StudyProject/Assets/Scenes/Boot.unity) 初始化并进入 [Main](Recovery/StudyProject/Assets/Scenes/Main.unity)。若 Play 起始场景被改变，使用 `Tools > APK Learning Project > Use Boot for Play Mode` 恢复。

保持 **Reload Domain 和 Reload Scene 均开启**。关闭域重载的快速 Play 尚不支持。修改脚本、插件或重建资源前先停止 Play，刷新并等待编译完成后再运行。

## 当前状态

已实际跑通原摇杆、自动战斗、吞噬成长、新武器及第二武器槽、首岛开门、重启后进入第二岛和真实存档。刷新倒计时、血条奖励数值、VIP、首充、广告属性奖励、水晶活动，以及随机闪烁和桌面实时阴影已有恢复与专项验证。商品支付和观看广告均在本地模拟成功，保留原奖励、冷却与存档逻辑，不接入真实平台；邮件不恢复。

运行后先点击 **Game 窗口**，使用 **WASD 或方向键**移动，也可以继续拖动屏幕摇杆。斜向速度保持一致；在输入框中打字、切离游戏或禁用控制器时停止输入。增加测试钻石使用 **Tools → 学习测试 → 钻石工具**。

后期全部岛屿、活动、完整矿区以及 Android 构建仍未完整验证。ParticleImage 当前覆盖恢复的 CPU 分支；jobs 和部分噪声浮点差异仍有限制。具体范围见 [恢复状态](Docs/RecoveryStatus.md)，源码和配置入口见 [学习导航](Docs/Learning.md)。

## 仓库内容

- `Recovery/StudyProject/`：Unity 的 Assets、Packages、ProjectSettings 及必要运行资源。
- `Recovery/Open-StudyProject.*`：启动脚本。
- `Recovery/Rebuild-StudyAddressables.ps1`：需要时重建 Windows 本地 Addressables；所需小型工具随仓库保留。
- `Docs/`：当前使用和恢复说明。

旧导出工程、一次性诊断工具、截图、日志和过程文档已清理。原 APK、原生分析资料和存档备份仍在本地，由 Git ignore 排除。Unity 缓存、临时文件、构建产物和个人编辑器设置也不提交。

修改 Addressables 所用预制体后，停止 Play，从仓库根目录运行：

```powershell
.\Recovery\Rebuild-StudyAddressables.ps1 -UnityEditor "D:\Unity\6000.6.3f1\Editor\Unity.exe"
```

脚本在独立临时工程构建，再更新学习工程的本地资源包。日常玩法验证直接使用 Unity Editor。
