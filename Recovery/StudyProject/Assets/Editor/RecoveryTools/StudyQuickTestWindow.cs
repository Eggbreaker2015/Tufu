#if UNITY_EDITOR
using System;
using System.Linq;
using Codebase.Amulet;
using Codebase.BossTower;
using Codebase.Core;
using Codebase.Location;
using Codebase.Player.Models;
using Codebase.Quests;
using Codebase.Services.Cheats;
using Codebase.Services.Time;
using Codebase.UI;
using Codebase.Utilities;
using Cysharp.Threading.Tasks;
using RecoveryCompatibility;
using UnityEditor;
using UnityEngine;

namespace RecoveryTools
{
    public sealed class StudyQuickTestWindow : EditorWindow
    {
        [SerializeField] private int tab;
        [SerializeField] private int islandIndex;
        [SerializeField] private int chainIndex;
        [SerializeField] private int bossIndex;
        [SerializeField] private int bossFloor = 1;
        [SerializeField] private int previewIndex;
        [SerializeField] private int gearLevel = 10;
        [SerializeField] private int gearCopies = 100;
        [SerializeField] private bool evolveGear;
        [SerializeField] private Vector2 scroll;
        private bool busy;
        private string result;
        private MessageType resultType = MessageType.Info;
        private static readonly string[] Tabs = { "战斗", "地图与任务", "装备与资源", "功能与活动", "剧情重播" };

        [MenuItem("Tools/学习测试/快速测试 _F8", false, 1990)]
        public static void Open()
        {
            var window = GetWindow<StudyQuickTestWindow>("快速测试");
            window.minSize = new Vector2(490, 620);
            window.Show();
        }

        private void OnInspectorUpdate() => Repaint();

        private void OnGUI()
        {
            EditorGUILayout.LabelField("学习工程 · 快速测试", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("当前存档", StudyTestProfile.IsEnabled ? "独立测试档" : "正常存档");
            DrawProfiles();
            bool ready = StudyQuickTestActions.TryGetReady(out string reason);
            if (!ready) EditorGUILayout.HelpBox(reason, MessageType.Info);
            else
            {
                var player = GameContext.Get<PlayerModel>();
                EditorGUILayout.LabelField("当前进度", "第 " + player.CurrentLocation.Number + " 岛 / 已开放 " + player.LastUnlockedLocation.Number + " 岛 · 钻石 " + player.Gems.ToString("N0"));
            }
            if (!StudyTestProfile.IsEnabled)
                EditorGUILayout.HelpBox("先从上方建立测试副本。以下工具写入独立测试档；正常进度保留。", MessageType.Info);
            if (StudyQuickTestActions.IsPreviewRunning && GUILayout.Button("停止过场预览并恢复画面")) StudyQuickTestActions.StopPreview();
            tab = GUILayout.Toolbar(tab, Tabs);
            using (var area = new EditorGUILayout.ScrollViewScope(scroll))
            {
                scroll = area.scrollPosition;
                using (new EditorGUI.DisabledScope(!ready || !StudyTestProfile.IsEnabled || busy || StudyTestProfileController.IsSwitching))
                {
                    if (ready)
                    {
                        switch (tab)
                        {
                            case 0: DrawCombat(); break;
                            case 1: DrawWorld(); break;
                            case 2: DrawEquipment(); break;
                            case 3: DrawFeatures(); break;
                            case 4: DrawPreviews(); break;
                        }
                    }
                }
            }
            if (busy) EditorGUILayout.HelpBox("正在执行测试操作…", MessageType.Info);
            if (!string.IsNullOrEmpty(result)) EditorGUILayout.HelpBox(result, resultType);
            else if (!string.IsNullOrEmpty(StudyTestProfileController.LastStatus)) EditorGUILayout.HelpBox(StudyTestProfileController.LastStatus, MessageType.Info);
        }

        private void DrawProfiles()
        {
            bool disabled = busy || StudyQuickTestActions.IsPreviewRunning || StudyTestProfileController.IsSwitching || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying);
            using (new EditorGUI.DisabledScope(disabled))
            {
                if (GUILayout.Button("从正常进度建立测试副本并进入", GUILayout.Height(26))) Switch(StudyTestProfileController.Request.CloneNormal);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("继续测试档")) Switch(StudyTestProfileController.Request.ResumeTest);
                    if (GUILayout.Button("全新测试档（从序章）")) Switch(StudyTestProfileController.Request.FreshTest);
                    if (GUILayout.Button("切回正常档")) Switch(StudyTestProfileController.Request.Normal);
                }
                EditorGUILayout.LabelField("建立/重建前自动备份，切换会重新启动 Play。", EditorStyles.miniLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("备份当前档")) Run(() => "存档备份：" + StudyTestProfileController.BackupCurrent());
                    if (GUILayout.Button("打开存档目录")) EditorUtility.RevealInFinder(StudyTestProfile.SaveDirectory);
                }
            }
        }

        private void DrawCombat()
        {
            var cheats = GameContext.Get<CheatsModel>();
            EditorGUILayout.LabelField("本次 Play 的测试开关", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            bool god = EditorGUILayout.Toggle("无敌", cheats.GodMode);
            bool quick = EditorGUILayout.Toggle("快速击杀（原测试开关）", cheats.OneHitMode);
            bool one = EditorGUILayout.Toggle("对敌伤害固定为 1", cheats.OneDamageMode);
            bool timer = EditorGUILayout.Toggle("暂停 Boss 战倒计时", cheats.StopBossTowerTimer);
            int drop = EditorGUILayout.IntSlider("掉落倍率", cheats.DropMultiplier, 1, 100);
            float speed = EditorGUILayout.Slider("战斗倍速", UnityEngine.Time.timeScale, .25f, 5f);
            if (EditorGUI.EndChangeCheck()) Run(() => StudyQuickTestActions.SetCombatOptions(god, quick, one, timer, drop, speed));
            EditorGUILayout.HelpBox("快速击杀沿用原版测试逻辑。战斗倍速不会直接跳过 UTC 活动或刷新时钟。", MessageType.None);
            using (new EditorGUILayout.HorizontalScope())
            {
                Button("一键战斗体验", StudyQuickTestActions.ApplyCombatPreset);
                Button("恢复默认开关", StudyQuickTestActions.ResetCombatOptions);
                Button("回满生命", StudyQuickTestActions.Heal);
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("测试档游戏时钟", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("当前偏移", GameContext.Get<ITimeService>().CheatOffset.ToString());
            using (new EditorGUILayout.HorizontalScope())
            {
                Button("前进 1 小时", () => StudyQuickTestActions.SkipTime(3600));
                Button("前进 1 天", () => StudyQuickTestActions.SkipTime(86400));
                Button("恢复实际时间", StudyQuickTestActions.ResetTimeOffset);
            }
        }

        private void DrawWorld()
        {
            var islands = GameContext.Get<LocationsConfig>().Locations.OrderBy(l => l.Number).ToArray();
            islandIndex = Mathf.Clamp(islandIndex, 0, islands.Length - 1);
            islandIndex = EditorGUILayout.Popup("目标岛屿", islandIndex, islands.Select(l => "第 " + l.Number + " 岛 · " + LocalizationStrings.Get("ISLAND_NAME_" + l.Number)).ToArray());
            using (new EditorGUILayout.HorizontalScope())
            {
                Button("传送并开放沿途入口", () => StudyQuickTestActions.TeleportToIsland(islands[islandIndex].Number));
                Button("地图全部开放", StudyQuickTestActions.UnlockAllGates);
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("任务定位与终章", EditorStyles.boldLabel);
            var chains = GameContext.Get<QuestConfig>().Chains.Where(c => !c.IsPrologue && c.Role != QuestChainRole.Hub).ToArray();
            if (chains.Length == 0) return;
            chainIndex = Mathf.Clamp(chainIndex, 0, chains.Length - 1);
            chainIndex = EditorGUILayout.Popup("任务链", chainIndex, chains.Select(c => LocalizationStrings.Get(c.ChainNameKey) + " · " + c.ChainKey).ToArray());
            var chain = chains[chainIndex];
            var model = GameContext.Get<QuestModel>();
            EditorGUILayout.LabelField("当前步骤", model.Chain.GetActiveStepKey(chain) ?? "步骤已完成，等待领奖");
            using (new EditorGUILayout.HorizontalScope())
            {
                Button("传送到目标 / NPC", () => StudyQuickTestActions.TeleportToTask(chain.ChainKey));
                Button("完成当前步骤", () => StudyQuickTestActions.CompleteTaskStep(chain.ChainKey));
            }
            Button("解锁前置并打开这条任务的终章入口", () => StudyQuickTestActions.JumpToFinale(chain.ChainKey));
            EditorGUILayout.HelpBox("终章入口会改写测试档的前置进度，不补发跳过的步骤奖励。在弹出的原任务窗口点击完成，继续领奖与剧情；仅查看过场可使用“剧情重播”。", MessageType.None);
        }

        private void DrawEquipment()
        {
            EditorGUILayout.LabelField("资源", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                Button("+10,000 钻石", () => StudyQuickTestActions.AddGems(10000));
                Button("+100,000 钻石", () => StudyQuickTestActions.AddGems(100000));
                Button("各类灵魂 +1,000", () => StudyQuickTestActions.GiveSouls(1000));
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("装备", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("已获得", GameContext.Get<PlayerModel>().Items.Count + " 件");
            using (new EditorGUILayout.HorizontalScope())
            {
                Button("解锁所有装备槽", StudyQuickTestActions.UnlockEquipmentSlots);
                Button("补齐全部基础装备", StudyQuickTestActions.GiveAllEquipment);
            }
            gearLevel = EditorGUILayout.IntField("设置所有装备等级", gearLevel);
            gearCopies = EditorGUILayout.IntField("设置所有装备副本数", gearCopies);
            evolveGear = EditorGUILayout.Toggle("同时进化", evolveGear);
            EditorGUILayout.LabelField("神圣装备由基础装备进化获得，不单独发放外观模板。", EditorStyles.miniLabel);
            Button("应用上述装备数值", () => StudyQuickTestActions.UpgradeEquipment(gearLevel, gearCopies, evolveGear));
            using (new EditorGUILayout.HorizontalScope())
            {
                Button("按伤害/护甲自动装备", StudyQuickTestActions.EquipStrongest);
                Button("打开背包", StudyQuickTestActions.OpenInventory);
            }
        }

        private void DrawFeatures()
        {
            EditorGUILayout.LabelField("护符与灵魂之树", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                Button("直接解锁护符", StudyQuickTestActions.UnlockAmulet);
                Button("护符 +10 级", () => StudyQuickTestActions.AddAmuletLevels(10));
                Button("打开灵魂之树", () => StudyQuickTestActions.OpenFeature(UIConstants.AmuletUpgradeGraphView));
            }
            AsyncButton("灵魂之树全部升满", StudyQuickTestActions.MaxAmuletTree);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("水晶与矿区", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                Button("解锁水晶及所有槽", StudyQuickTestActions.UnlockCrystals);
                Button("水晶材料 +100", () => StudyQuickTestActions.AddCrystalMaterials(100));
                Button("打开水晶页面", () => StudyQuickTestActions.OpenFeature(UIConstants.AmuletCrystalsView));
            }
            Button("解锁矿区功能", StudyQuickTestActions.UnlockMines);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Boss 塔", EditorStyles.boldLabel);
            Button("开放传送门、所有 Boss 并补充入场券", StudyQuickTestActions.UnlockBossTower);
            var bosses = GameContext.Get<BossTowerModel>().Bosses;
            if (bosses.Count > 0)
            {
                bossIndex = Mathf.Clamp(bossIndex, 0, bosses.Count - 1);
                bossIndex = EditorGUILayout.Popup("Boss", bossIndex, bosses.Select(b => LocalizationStrings.Get(b.Def.DisplayName) + " · " + b.Key).ToArray());
                bossFloor = EditorGUILayout.IntField("目标层数", bossFloor);
                using (new EditorGUILayout.HorizontalScope())
                {
                    Button("应用层数", () => StudyQuickTestActions.SetBossFloor(bosses[bossIndex].Key, bossFloor));
                    Button("打开 Boss 选择页", () => StudyQuickTestActions.OpenFeature(UIConstants.BossTowerHubView));
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("活动", EditorStyles.boldLabel);
            AsyncButton("重开水晶活动（使用原活动计划）", StudyQuickTestActions.StartCrystalEvent);
            using (new EditorGUILayout.HorizontalScope())
            {
                Button("活动红/蓝水晶各 +2,000", () => StudyQuickTestActions.AddEventCurrency(2000));
                Button("结束当前活动", StudyQuickTestActions.CompleteEvent);
            }
            Button("打开 VIP 页面", () => StudyQuickTestActions.OpenFeature(UIConstants.VipView));
        }

        private void DrawPreviews()
        {
            var keys = StudyQuickTestActions.GetPreviewStepKeys();
            if (keys.Length == 0) { EditorGUILayout.HelpBox("当前场景没有可重播的剧情导演。", MessageType.Info); return; }
            previewIndex = Mathf.Clamp(previewIndex, 0, keys.Length - 1);
            previewIndex = EditorGUILayout.Popup("过场", previewIndex, keys.Select(PreviewName).ToArray());
            EditorGUILayout.HelpBox("按正常速度重播原场景的镜头、角色动画和音效，结束后恢复画面；不推进任务或领取奖励。要体验任务对话和领奖，请使用任务终章跳转。", MessageType.Info);
            AsyncButton("重播选中过场", () => StudyQuickTestActions.PreviewCutscene(keys[previewIndex]));
        }

        private static string PreviewName(string key)
        {
            if (key == "quest_c1_step_5") return "老矿工 · 矿洞爆破";
            if (key == "quest_hub_finale") return "矿洞入口 · 后续剧情";
            if (key == "quest_c0_step_1") return "序章 · 石化剧情";
            return key;
        }

        private void Button(string text, Func<string> action) { if (GUILayout.Button(text)) Run(action); }
        private void AsyncButton(string text, Func<UniTask<string>> action) { if (GUILayout.Button(text)) RunAsync(action); }

        private void Switch(StudyTestProfileController.Request request)
        {
            Run(() => { StudyTestProfileController.SwitchAndRestart(request); return "正在保存并切换存档，请等待重新进入游戏。"; });
        }

        private void Run(Func<string> action)
        {
            try { result = action(); resultType = MessageType.Info; }
            catch (Exception exception) { result = exception.Message; resultType = MessageType.Warning; }
            Repaint();
        }

        private async void RunAsync(Func<UniTask<string>> action)
        {
            busy = true;
            try { result = await action(); resultType = MessageType.Info; }
            catch (OperationCanceledException) { result = "测试操作已停止。"; resultType = MessageType.Info; }
            catch (Exception exception) { result = exception.Message; resultType = MessageType.Warning; Debug.LogException(exception); }
            finally { busy = false; if (this != null) Repaint(); }
        }
    }
}
#endif
