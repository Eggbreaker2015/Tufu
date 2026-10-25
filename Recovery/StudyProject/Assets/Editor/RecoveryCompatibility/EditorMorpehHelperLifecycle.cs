#if UNITY_EDITOR
// The recovered player-form Morpeh DLL excludes its official Editor helper
// destruction callback. Restore that callback only. This project uses normal
// Domain + Scene reload; this bridge does not implement fast Play static resets.
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RecoveryCompatibility
{
    [InitializeOnLoad]
    public static class EditorMorpehHelperLifecycle
    {
        private static readonly Type HelperType = typeof(Scellecs.Morpeh.World).Assembly.GetType("Scellecs.Morpeh.UnityRuntimeHelper", true);
        private static readonly bool MissingCallback = HelperType.GetMethod("OnEditorApplicationOnplayModeStateChanged", BindingFlags.Instance | BindingFlags.NonPublic) == null;
        public static int LastCleanedCount { get; private set; }
        public static string LastCleanupUtc { get; private set; }

        static EditorMorpehHelperLifecycle()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode || !MissingCallback) return;
            LastCleanedCount = 0;
            // One callback per original helper; enumerate the precise DLL type
            // so historical HideAndDontSave instances are covered as well.
            foreach (UnityEngine.Object item in Resources.FindObjectsOfTypeAll(HelperType))
            {
                var helper = (Component)item;
                // Same Unity fake-null guard as the official Editor callback.
                if (helper != null && helper.gameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(helper.gameObject);
                    LastCleanedCount++;
                }
            }
            LastCleanupUtc = DateTime.UtcNow.ToString("o");
        }
    }
}
#endif
