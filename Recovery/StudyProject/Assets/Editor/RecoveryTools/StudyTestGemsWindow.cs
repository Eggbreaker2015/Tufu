#if UNITY_EDITOR
// Learning-project test utility, not recovered game code. Editor folder excludes it from players.
using System;
using System.Globalization;
using Codebase.Core;
using Codebase.Core.UserData;
using Codebase.Player.Models;
using UnityEditor;
using UnityEngine;

namespace RecoveryTools
{
    public sealed class StudyTestGemsWindow : EditorWindow
    {
        private const string MenuPath = "Tools/学习测试/钻石工具";
        [SerializeField] private int amount = 10000;
        private string lastResult;
        private MessageType lastResultType;

        [MenuItem(MenuPath, false, 2000)]
        public static void Open()
        {
            var window = GetWindow<StudyTestGemsWindow>("钻石测试");
            window.minSize = new Vector2(360, 230);
            window.Show();
        }

        private void OnInspectorUpdate() => Repaint();

        private void OnGUI()
        {
            EditorGUILayout.LabelField("学习工程 · 钻石测试", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("运行游戏并进入 Main 后使用。增加的钻石会立即写入当前存档。", MessageType.Info);
            bool ready = TryGetServices(out var player, out _, out string reason);
            EditorGUILayout.LabelField("当前钻石", ready ? Format(player.Gems) : "—");
            if (!ready) EditorGUILayout.HelpBox(reason, MessageType.Info);

            using (new EditorGUI.DisabledScope(!ready))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+1,000")) AddFromWindow(1000);
                    if (GUILayout.Button("+10,000")) AddFromWindow(10000);
                }
            }
            amount = EditorGUILayout.IntField("自定义增加数量", amount);
            using (new EditorGUI.DisabledScope(!ready || amount <= 0))
                if (GUILayout.Button("增加钻石并保存")) AddFromWindow(amount);

            if (!string.IsNullOrEmpty(lastResult)) EditorGUILayout.HelpBox(lastResult, lastResultType);
        }

        private void AddFromWindow(int value)
        {
            bool success = TryAddGems(value, out lastResult);
            lastResultType = success ? MessageType.Info : MessageType.Warning;
            Repaint();
        }

        public static bool TryAddGems(int value, out string result)
        {
            if (value <= 0)
            {
                result = "增加数量必须为正整数。";
                return false;
            }
            if (!TryGetServices(out var player, out var save, out result)) return false;
            double before = player.Gems;
            double after = before + value;
            if (double.IsNaN(before) || double.IsInfinity(before) || before < 0 ||
                double.IsNaN(after) || double.IsInfinity(after) || after <= before)
            {
                result = "当前余额或增加后的余额超出可用数值范围，未作修改。";
                return false;
            }

            // Use the same observable property as real gem rewards and shop spending.
            // CurrencyModel.Add("gems", ...) would change a different, unused balance.
            player.Gems = after;
            bool saved = false;
            void OnSaved(GameDataSaveReason reason)
            {
                if (reason == GameDataSaveReason.Forced) saved = true;
            }
            save.OnAfterSave += OnSaved;
            try
            {
                save.ForceSave();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                result = "钻石已增加至 " + Format(after) + "，但保存失败；请查看 Console。";
                return false;
            }
            finally
            {
                save.OnAfterSave -= OnSaved;
            }
            if (!saved)
            {
                // ForceSave reports disk errors through Console instead of throwing.
                result = "钻石已增加至 " + Format(after) + "，但未确认存档完成；请查看 Console。";
                return false;
            }
            result = "已增加 " + Format(value) + " 钻石；当前 " + Format(after) + "，已保存。";
            Debug.Log("[Study Test] " + result);
            return true;
        }

        private static bool TryGetServices(out PlayerModel player, out GameDataService save, out string reason)
        {
            player = null;
            save = null;
            reason = "请先运行游戏，等待 Main 场景加载完成。";
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                !GameContext.Has<PlayerModel>() || !GameContext.Has<GameDataService>()) return false;
            player = GameContext.Get<PlayerModel>();
            save = GameContext.Get<GameDataService>();
            if (player == null || save == null || player.Lifetime.IsDisposed || save.Lifetime.IsDisposed ||
                player.Data == null || player.Unit == null) return false;
            reason = null;
            return true;
        }

        private static string Format(double value) => value.ToString("N0", CultureInfo.CurrentCulture);
    }
}
#endif
