#if UNITY_EDITOR
using System;
using System.Reflection;
using Codebase.UI.Quests;
using UnityEditor;
using UnityEngine;

namespace RecoveryCompatibility
{
    // Recycle quest list/reward rows while their scene roots still exist. Let the
    // normal popup and lifetime cleanup run afterwards; never invoke claim callbacks.
    [InitializeOnLoad]
    public static class EditorQuestViewLifecycle
    {
        private static readonly FieldInfo[] Containers =
        {
            FindContainer("questsViewContainer"),
            FindContainer("chainRewardsViewContainer")
        };

        static EditorQuestViewLifecycle()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Application.quitting -= RecycleRows;
            Application.quitting += RecycleRows;
        }

        private static FieldInfo FindContainer(string name) => typeof(QuestsWindowView).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(QuestsWindowView).FullName, name);

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Application.quitting -= RecycleRows;
                Application.quitting += RecycleRows;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode) RecycleRows();
        }

        private static void RecycleRows()
        {
            foreach (var view in Resources.FindObjectsOfTypeAll<QuestsWindowView>())
            {
                if (view == null || !view.gameObject.scene.IsValid() || !view.gameObject.scene.isLoaded) continue;
                foreach (var field in Containers)
                {
                    object container = field.GetValue(view);
                    if (container == null) continue;
                    var clear = field.FieldType.GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null)
                        ?? throw new MissingMethodException(field.FieldType.FullName, "Clear");
                    clear.Invoke(container, null);
                }
            }
        }
    }
}
#endif
