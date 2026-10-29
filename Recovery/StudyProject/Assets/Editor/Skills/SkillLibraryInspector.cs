using System.Collections.Generic;
using Codebase.Skills;
using UnityEditor;
using UnityEngine;

namespace Codebase.SkillsEditor
{
    [CustomEditor(typeof(SkillLibrary))]
    public sealed class SkillLibraryInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            try
            {
                if (GUILayout.Button("打开技能库编辑器")) SkillLibraryEditorWindow.Open((SkillLibrary)target);
                if (GUILayout.Button("校验"))
                {
                    serializedObject.ApplyModifiedProperties();
                    SkillLibraryValidator.LogValidation();
                }
                var lib = (SkillLibrary)target;
                EditorGUILayout.LabelField("技能 " + (lib.Skills?.Count ?? 0) + "  Buff " + (lib.Buffs?.Count ?? 0) +
                    "  配置 " + (lib.Loadouts?.Count ?? 0) + "  特效 " + (lib.Vfx?.Count ?? 0) + "  召唤 " + (lib.Summons?.Count ?? 0));
                var p = serializedObject.GetIterator();
                bool enter = true;
                var paths = new List<string>();
                while (p.NextVisible(enter))
                {
                    enter = false;
                    if (p.name != "m_Script") paths.Add(p.propertyPath);
                }
                foreach (string path in paths)
                {
                    var property = serializedObject.FindProperty(path);
                    if (property != null) ManagedReferenceGUI.DrawProperty(property, new GUIContent(property.displayName));
                }
            }
            finally { serializedObject.ApplyModifiedProperties(); }
        }
    }
}
