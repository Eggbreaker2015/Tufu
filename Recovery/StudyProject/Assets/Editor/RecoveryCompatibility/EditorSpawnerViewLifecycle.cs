#if UNITY_EDITOR
// Editor teardown compatibility for the recovered UI pools. Lifetime destruction
// clears charge views; doing that after scene roots are destroyed recreates a
// pool container during scene closing. Clear while the real quit notification
// still has a loaded scene. The subsequent Lifetime clear is then empty.
using System;
using System.Reflection;
using Codebase.Core.UI;
using Codebase.UI;
using UnityEditor;
using UnityEngine;

namespace RecoveryCompatibility
{
    [InitializeOnLoad]
    public static class EditorSpawnerViewLifecycle
    {
        private static readonly FieldInfo Charges = typeof(SpawnerView).GetField("chargesViewContainer", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(SpawnerView).FullName, "chargesViewContainer");
        public static int LastClearedContainers { get; private set; }
        public static int LastClearedCharges { get; private set; }
        public static string LastQuitUtc { get; private set; }

        static EditorSpawnerViewLifecycle()
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
            LastClearedContainers = 0;
            LastClearedCharges = 0;
            LastQuitUtc = DateTime.UtcNow.ToString("o");
            foreach (SpawnerView view in Resources.FindObjectsOfTypeAll<SpawnerView>())
            {
                if (view == null || !view.gameObject.scene.IsValid() || !view.gameObject.scene.isLoaded) continue;
                var container = (ViewContainer<ChargeView, Color>)Charges.GetValue(view);
                if (container == null) continue;
                LastClearedContainers++;
                LastClearedCharges += container.Active.Count;
                container.Clear();
            }
        }
    }
}
#endif
