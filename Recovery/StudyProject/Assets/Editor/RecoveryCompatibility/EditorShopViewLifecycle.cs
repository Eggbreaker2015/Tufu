#if UNITY_EDITOR
// Editor teardown compatibility: recycle shop tabs before scene roots disappear.
// Clearing them from their Lifetime destruction can otherwise recreate a pool root.
using System;
using System.Reflection;
using Codebase.Core.UI;
using Codebase.UI.Shop;
using UnityEditor;
using UnityEngine;

namespace RecoveryCompatibility
{
    [InitializeOnLoad]
    public static class EditorShopViewLifecycle
    {
        private static readonly FieldInfo Tabs = typeof(ShopView).GetField("tabsContainer", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(ShopView).FullName, "tabsContainer");
        public static int LastClearedShops { get; private set; }
        public static int LastClearedTabs { get; private set; }

        static EditorShopViewLifecycle()
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
            LastClearedShops = 0;
            LastClearedTabs = 0;
            foreach (ShopView view in Resources.FindObjectsOfTypeAll<ShopView>())
            {
                if (view == null || !view.gameObject.scene.IsValid() || !view.gameObject.scene.isLoaded) continue;
                var container = (ViewContainer<ShopTabButtonView, ShopTabDef>)Tabs.GetValue(view);
                if (container == null) continue;
                LastClearedShops++;
                LastClearedTabs += container.Active.Count;
                container.Clear();
            }
        }
    }
}
#endif
