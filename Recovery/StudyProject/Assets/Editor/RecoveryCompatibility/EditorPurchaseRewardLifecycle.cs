#if UNITY_EDITOR
// Recycle only the restored first-purchase and VIP daily-reward popup containers
// while scene roots are still present. Do not invoke Close or collection callbacks.
using System;
using System.Reflection;
using Codebase.Core.UI;
using Codebase.Rewards;
using Codebase.UI.BossTower;
using Codebase.UI.Events;
using Codebase.UI.Shop;
using UnityEditor;
using UnityEngine;

namespace RecoveryCompatibility
{
    [InitializeOnLoad]
    public static class EditorPurchaseRewardLifecycle
    {
        private static readonly FieldInfo FirstBuyRewards = typeof(FirstBuyPackView).GetField("rewardsViewContainer", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(FirstBuyPackView).FullName, "rewardsViewContainer");
        private static readonly FieldInfo DailyRewards = typeof(BossRaidRewardView).GetField("rewardsViewContainer", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(BossRaidRewardView).FullName, "rewardsViewContainer");
        public static int LastClearedFirstBuyViews { get; private set; }
        public static int LastClearedDailyViews { get; private set; }
        public static int LastClearedRewardItems { get; private set; }

        static EditorPurchaseRewardLifecycle()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Subscribe();
        }

        private static void Subscribe()
        {
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) Subscribe();
            else if (state == PlayModeStateChange.EnteredEditMode) Application.quitting -= OnQuitting;
        }

        private static void OnQuitting()
        {
            LastClearedFirstBuyViews = 0;
            LastClearedDailyViews = 0;
            LastClearedRewardItems = 0;
            foreach (FirstBuyPackView view in Resources.FindObjectsOfTypeAll<FirstBuyPackView>())
            {
                if (view == null || !view.gameObject.scene.IsValid() || !view.gameObject.scene.isLoaded) continue;
                var container = (ViewContainer<RewardView, RewardBase>)FirstBuyRewards.GetValue(view);
                if (container == null) continue;
                LastClearedFirstBuyViews++;
                LastClearedRewardItems += container.Active.Count;
                container.Clear();
            }
            foreach (BossRaidRewardView view in Resources.FindObjectsOfTypeAll<BossRaidRewardView>())
            {
                if (view == null || !view.gameObject.scene.IsValid() || !view.gameObject.scene.isLoaded) continue;
                var container = (ViewContainer<RewardView, RewardBase>)DailyRewards.GetValue(view);
                if (container == null) continue;
                LastClearedDailyViews++;
                LastClearedRewardItems += container.Active.Count;
                container.Clear();
            }
        }
    }
}
#endif
