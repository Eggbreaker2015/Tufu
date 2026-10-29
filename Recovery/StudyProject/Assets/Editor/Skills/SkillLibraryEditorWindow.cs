using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Codebase.Skills;
using UnityEditor;
using UnityEngine;

namespace Codebase.SkillsEditor
{
    public sealed class SkillLibraryEditorWindow : EditorWindow
    {
        [SerializeField] SkillLibrary library;
        [SerializeField] string libraryPath;
        [SerializeField] int tab;
        [SerializeField] int selected = -1;
        [SerializeField] string search = "";
        List<SkillLibrary> libraries = new List<SkillLibrary>();
        SerializedObject serializedLibrary;
        readonly Dictionary<string, string[]> localized = new Dictionary<string, string[]>(StringComparer.Ordinal);
        readonly bool[] severityVisible = { true, true, true };
        int chineseIndex = -1;
        List<SkillIssue> issues = new List<SkillIssue>();
        bool showIssues = true;
        bool revealSelection;
        Vector2 leftScroll, rightScroll, issueScroll;
        static readonly string[] TabNames = { "技能", "Buff", "配置", "特效", "召唤" };
        static readonly string[] SeverityNames = { "错误", "警告", "提示" };
        static readonly string[] Icons = { "console.erroricon.sml", "console.warnicon.sml", "console.infoicon.sml" };

        [MenuItem("Tools/技能/技能库编辑器")]
        static void ShowWindow() => Open(null);

        public static void Open(SkillLibrary lib, int tab = 0, int index = -1)
        {
            var window = GetWindow<SkillLibraryEditorWindow>();
            window.titleContent = new GUIContent("技能库编辑器");
            window.minSize = new Vector2(900, 500);
            window.RefreshLibraries();
            if (lib != null) window.SelectLibrary(lib);
            window.tab = Mathf.Clamp(tab, 0, 4);
            window.selected = index;
            window.search = "";
            window.revealSelection = true;
            window.Show();
        }

        void OnEnable()
        {
            titleContent = new GUIContent("技能库编辑器");
            minSize = new Vector2(900, 500);
            Undo.undoRedoPerformed += OnUndoRedo;
            RefreshLibraries();
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            serializedLibrary?.Dispose();
            serializedLibrary = null;
        }

        void OnUndoRedo() { revealSelection = true; Repaint(); }
        void OnProjectChange() { RefreshLibraries(); Repaint(); }

        void RefreshLibraries()
        {
            libraries = SkillLibraryValidator.FindLibraries();
            if (library == null && !string.IsNullOrEmpty(libraryPath))
                library = AssetDatabase.LoadAssetAtPath<SkillLibrary>(libraryPath);
            if (library == null || !libraries.Contains(library)) library = libraries.FirstOrDefault();
            RecreateSerializedObject();
            localized.Clear();
            chineseIndex = -1;
            var database = SkillLibraryValidator.FindLocalization();
            if (database == null) return;
            if (database.SupportedLocales != null)
                chineseIndex = Array.FindIndex(database.SupportedLocales, l => string.Equals(l, "zh", StringComparison.OrdinalIgnoreCase));
            if (database.Entries != null)
                foreach (var entry in database.Entries)
                    if (entry.key != null && !localized.ContainsKey(entry.key)) localized.Add(entry.key, entry.values);
        }

        void RecreateSerializedObject()
        {
            serializedLibrary?.Dispose();
            serializedLibrary = library != null ? new SerializedObject(library) : null;
            if (library != null) libraryPath = AssetDatabase.GetAssetPath(library);
        }

        void SelectLibrary(SkillLibrary lib)
        {
            if (library == lib && serializedLibrary != null) return;
            if (serializedLibrary != null && serializedLibrary.targetObject != null) serializedLibrary.ApplyModifiedProperties();
            library = lib;
            RecreateSerializedObject();
            selected = -1;
            leftScroll = rightScroll = Vector2.zero;
        }

        void OnGUI()
        {
            if (library == null && serializedLibrary != null) RefreshLibraries();
            if (library != null && (serializedLibrary == null || serializedLibrary.targetObject != library)) RecreateSerializedObject();
            serializedLibrary?.Update();
            Action afterDraw = null;
            try
            {
                Toolbar(ref afterDraw);
                if (serializedLibrary == null)
                    EditorGUILayout.HelpBox("未找到技能库（Resources/Skills）", MessageType.Info);
                else
                {
                    var labels = SkillLibraryValidator.Roots.Select((r, i) => TabNames[i] + " (" + (serializedLibrary.FindProperty(r)?.arraySize ?? 0) + ")").ToArray();
                    int nextTab = GUILayout.Toolbar(tab, labels);
                    if (nextTab != tab) { tab = nextTab; selected = -1; leftScroll = rightScroll = Vector2.zero; }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("搜索", GUILayout.Width(32));
                        search = EditorGUILayout.TextField(search ?? "");
                        if (GUILayout.Button("清除", GUILayout.Width(45))) search = "";
                    }
                    using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(true)))
                    {
                        DrawList(ref afterDraw);
                        using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                        {
                            rightScroll = EditorGUILayout.BeginScrollView(rightScroll);
                            try
                            {
                                var list = serializedLibrary.FindProperty(SkillLibraryValidator.Roots[tab]);
                                if (list != null && selected >= 0 && selected < list.arraySize)
                                    ManagedReferenceGUI.DrawChildren(list.GetArrayElementAtIndex(selected));
                                else EditorGUILayout.HelpBox("请选择条目", MessageType.Info);
                            }
                            finally { EditorGUILayout.EndScrollView(); }
                        }
                    }
                }
                DrawIssues(ref afterDraw);
            }
            finally
            {
                if (serializedLibrary != null && serializedLibrary.targetObject != null) serializedLibrary.ApplyModifiedProperties();
            }
            // Whole-window structural actions run after every GUILayout scope has closed.
            afterDraw?.Invoke();
        }

        void Toolbar(ref Action afterDraw)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                int current = libraries.IndexOf(library);
                string[] names = libraries.Count == 0 ? new[] { "无技能库" } : libraries.Select(l => Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(l))).ToArray();
                using (new EditorGUI.DisabledScope(libraries.Count == 0))
                {
                    int next = EditorGUILayout.Popup(Mathf.Max(0, current), names, EditorStyles.toolbarPopup, GUILayout.MinWidth(160));
                    if (next != current && next >= 0 && next < libraries.Count)
                    {
                        var chosen = libraries[next];
                        afterDraw = () => { SelectLibrary(chosen); Repaint(); };
                    }
                }
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(45))) afterDraw = RefreshLibraries;
                using (new EditorGUI.DisabledScope(library == null))
                {
                    if (GUILayout.Button("定位文件", EditorStyles.toolbarButton, GUILayout.Width(65))) EditorGUIUtility.PingObject(library);
                    if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(45)))
                        afterDraw = () => { if (library != null) AssetDatabase.SaveAssetIfDirty(library); };
                }
                if (GUILayout.Button("校验", EditorStyles.toolbarButton, GUILayout.Width(45)))
                    afterDraw = () => { issues = SkillLibraryValidator.Run(); showIssues = true; Repaint(); };
            }
        }

        void DrawList(ref Action afterDraw)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(260)))
            {
                var list = serializedLibrary.FindProperty(SkillLibraryValidator.Roots[tab]);
                leftScroll = EditorGUILayout.BeginScrollView(leftScroll);
                try
                {
                    if (list != null)
                        for (int i = 0; i < list.arraySize; i++)
                        {
                            var entry = list.GetArrayElementAtIndex(i);
                            if (!Matches(entry)) continue;
                            var rect = GUILayoutUtility.GetRect(new GUIContent(EntryLabel(entry)), EditorStyles.miniButton, GUILayout.Height(24), GUILayout.ExpandWidth(true));
                            if (GUI.Toggle(rect, selected == i, EntryLabel(entry), EditorStyles.miniButton) && selected != i)
                            { selected = i; rightScroll = Vector2.zero; }
                            if (revealSelection && selected == i && Event.current.type == EventType.Repaint)
                            { leftScroll.y = Mathf.Max(0, rect.y - 30); revealSelection = false; Repaint(); }
                        }
                }
                finally { EditorGUILayout.EndScrollView(); }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("新建")) afterDraw = () => EditEntry(false);
                    using (new EditorGUI.DisabledScope(list == null || selected < 0 || selected >= list.arraySize))
                    {
                        if (GUILayout.Button("复制")) afterDraw = () => EditEntry(true);
                        if (GUILayout.Button("删除")) afterDraw = DeleteEntry;
                    }
                }
            }
        }

        string EntryLabel(SerializedProperty entry)
        {
            string key = entry.FindPropertyRelative("Key")?.stringValue ?? "";
            if (string.IsNullOrEmpty(key)) key = "（空键）";
            if (tab == 2)
            {
                var owner = entry.FindPropertyRelative("OwnerKind");
                string kind = owner != null && owner.enumValueIndex >= 0 && owner.enumValueIndex < owner.enumNames.Length ? owner.enumNames[owner.enumValueIndex] : "未知";
                return "[" + kind + "] " + key;
            }
            if (tab == 0)
            {
                string name = LocalText("SKILL_NAME_" + key, chineseIndex);
                if (!string.IsNullOrEmpty(name)) return key + "  " + name;
            }
            return key;
        }

        string LocalText(string key, int index)
        {
            return index >= 0 && localized.TryGetValue(key, out string[] values) && values != null && index < values.Length ? values[index] ?? "" : "";
        }

        bool Matches(SerializedProperty entry)
        {
            if (string.IsNullOrEmpty(search)) return true;
            if (Contains(EntryLabel(entry), search)) return true;
            if (tab == 2)
            {
                var owner = entry.FindPropertyRelative("OwnerKind");
                string kind = owner != null && owner.enumValueIndex >= 0 && owner.enumValueIndex < owner.enumNames.Length
                    ? owner.enumNames[owner.enumValueIndex] : "未知";
                return Contains(kind + "/" + entry.FindPropertyRelative("Key")?.stringValue, search);
            }
            if (tab != 0) return false;
            string key = entry.FindPropertyRelative("Key")?.stringValue;
            if (Contains(LocalText("SKILL_NAME_" + key, 0), search) || Contains(LocalText("SKILL_NAME_" + key, chineseIndex), search)) return true;
            bool matches = false;
            SkillLibraryValidator.Walk(entry, p =>
            {
                if (p.propertyType == SerializedPropertyType.ManagedReference &&
                    (Contains(p.managedReferenceFullTypename, search) || Contains(ObjectNames.NicifyVariableName(p.managedReferenceFullTypename), search))) matches = true;
            });
            return matches;
        }

        static bool Contains(string value, string query) => !string.IsNullOrEmpty(value) && value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

        void EditEntry(bool duplicate)
        {
            if (serializedLibrary == null || library == null) return;
            serializedLibrary.ApplyModifiedProperties();
            serializedLibrary.Update();
            var property = serializedLibrary.FindProperty(SkillLibraryValidator.Roots[tab]);
            // Edit the C# list directly: the reflective clone gives every managed reference a fresh instance (new rid on save),
            // without relying on SerializedProperty.boxedValue round-tripping [SerializeReference] graphs.
            var field = typeof(SkillLibrary).GetField(SkillLibraryValidator.Roots[tab]);
            if (property == null || field == null || (duplicate && (selected < 0 || selected >= property.arraySize))) return;
            try
            {
                var items = field.GetValue(library) as IList;
                if (items == null) { items = (IList)Activator.CreateInstance(field.FieldType); field.SetValue(library, items); }
                if (duplicate && selected >= items.Count) return;
                object entry;
                if (duplicate)
                {
                    var source = property.GetArrayElementAtIndex(selected);
                    var missing = SerializationUtility.GetManagedReferencesWithMissingTypes(library);
                    bool hasMissing = false;
                    SkillLibraryValidator.Walk(source, p =>
                    {
                        if (p.propertyType == SerializedPropertyType.ManagedReference && missing.Any(m => m.referenceId == p.managedReferenceId)) hasMissing = true;
                    });
                    if (hasMissing) { EditorUtility.DisplayDialog("无法复制", "请先修复条目中的丢失类型。", "确定"); return; }
                    entry = Clone(items[selected], new Dictionary<object, object>(ReferenceComparer.Instance));
                }
                else entry = Activator.CreateInstance(field.FieldType.GetGenericArguments()[0]);
                var keyField = entry?.GetType().GetField("Key");
                if (keyField != null)
                {
                    string basis = duplicate ? (keyField.GetValue(entry) as string) + "_copy" : "new_" + SkillLibraryValidator.Roots[tab].ToLowerInvariant();
                    keyField.SetValue(entry, UniqueKey(items, keyField, basis));
                }
                Undo.RecordObject(library, duplicate ? "复制条目" : "新建条目");
                items.Add(entry);
                EditorUtility.SetDirty(library);
                serializedLibrary.Update();
                selected = items.Count - 1;
                search = "";
                revealSelection = true;
                rightScroll = Vector2.zero;
            }
            catch (Exception ex)
            {
                serializedLibrary.Update();
                EditorUtility.DisplayDialog("操作失败", ex.Message, "确定");
            }
            Repaint();
        }

        static string UniqueKey(IList items, FieldInfo keyField, string basis)
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (object item in items) if (item != null) used.Add(keyField.GetValue(item) as string ?? "");
            string key = basis;
            int suffix = 1;
            while (used.Contains(key)) key = basis + "_" + suffix++;
            return key;
        }

        void DeleteEntry()
        {
            if (serializedLibrary == null || library == null) return;
            serializedLibrary.Update();
            var list = serializedLibrary.FindProperty(SkillLibraryValidator.Roots[tab]);
            if (list == null || selected < 0 || selected >= list.arraySize) return;
            if (!EditorUtility.DisplayDialog("删除条目", "确定删除 " + EntryLabel(list.GetArrayElementAtIndex(selected)) + "？", "删除", "取消")) return;
            ManagedReferenceGUI.Remove(list, selected);
            selected = Mathf.Min(selected, list.arraySize - 1);
            serializedLibrary.ApplyModifiedProperties();
            Repaint();
        }

        // Recreate the complete serialized graph, retaining Unity asset references only.
        // Identity tracking preserves cycles/shared nodes inside the copy, never with the source.
        static object Clone(object source, Dictionary<object, object> copies)
        {
            if (source == null || source is UnityEngine.Object) return source;
            Type type = source.GetType();
            if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)) return source;
            if (copies.TryGetValue(source, out object existing)) return existing;
            if (source is Array array)
            {
                var result = Array.CreateInstance(type.GetElementType(), array.Length);
                copies.Add(source, result);
                for (int i = 0; i < array.Length; i++) result.SetValue(Clone(array.GetValue(i), copies), i);
                return result;
            }
            if (source is IList list)
            {
                var result = (IList)Activator.CreateInstance(type);
                copies.Add(source, result);
                foreach (object item in list) result.Add(Clone(item, copies));
                return result;
            }
            object copy = Activator.CreateInstance(type);
            copies.Add(source, copy);
            for (Type t = type; t != null && t != typeof(object); t = t.BaseType)
                foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (!field.IsStatic && !field.IsInitOnly && !field.IsNotSerialized &&
                        (field.IsPublic || field.IsDefined(typeof(SerializeField), true) || field.IsDefined(typeof(SerializeReference), true)))
                        field.SetValue(copy, Clone(field.GetValue(source), copies));
            return copy;
        }

        sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();
            public new bool Equals(object a, object b) => ReferenceEquals(a, b);
            public int GetHashCode(object value) => RuntimeHelpers.GetHashCode(value);
        }

        void DrawIssues(ref Action afterDraw)
        {
            showIssues = EditorGUILayout.Foldout(showIssues, "校验结果 (" + issues.Count + ")", true);
            if (!showIssues) return;
            using (new EditorGUILayout.HorizontalScope())
                for (int i = 0; i < 3; i++)
                {
                    int severity = i;
                    severityVisible[i] = GUILayout.Toggle(severityVisible[i], SeverityNames[i] + " (" + issues.Count(x => (int)x.Severity == severity) + ")", EditorStyles.miniButton);
                }
            issueScroll = EditorGUILayout.BeginScrollView(issueScroll, GUILayout.Height(Mathf.Clamp(position.height * .24f, 90, 220)));
            try
            {
                foreach (var issue in issues)
                {
                    if (!severityVisible[(int)issue.Severity]) continue;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label(EditorGUIUtility.IconContent(Icons[(int)issue.Severity]), GUILayout.Width(20));
                        string label = (issue.Library != null ? issue.Library.name + ": " : "") + issue.Message;
                        if (GUILayout.Button(new GUIContent(label, label), EditorStyles.label))
                        {
                            var targetIssue = issue;
                            afterDraw = () => JumpTo(targetIssue);
                        }
                    }
                }
            }
            finally { EditorGUILayout.EndScrollView(); }
        }

        void JumpTo(SkillIssue issue)
        {
            if (issue.Library == null) return;
            EditorGUIUtility.PingObject(issue.Library);
            if (issue.Tab < 0) return;
            SelectLibrary(issue.Library);
            tab = Mathf.Clamp(issue.Tab, 0, 4);
            selected = issue.Index;
            serializedLibrary.Update();
            var list = serializedLibrary.FindProperty(SkillLibraryValidator.Roots[tab]);
            if (list != null && selected >= 0 && selected < list.arraySize && !Matches(list.GetArrayElementAtIndex(selected))) search = "";
            string path = issue.PropertyPath;
            while (!string.IsNullOrEmpty(path))
            {
                if (!path.EndsWith(".Array", StringComparison.Ordinal))
                {
                    var p = serializedLibrary.FindProperty(path);
                    if (p != null) p.isExpanded = true;
                }
                int dot = path.LastIndexOf('.');
                path = dot >= 0 ? path.Substring(0, dot) : null;
            }
            revealSelection = true;
            rightScroll = Vector2.zero;
            Repaint();
        }
    }
}
