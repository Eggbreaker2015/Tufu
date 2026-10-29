using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Codebase.SkillsEditor
{
    // The outer draw owns a mutation queue: never invalidate an iterator mid-layout.
    public static class ManagedReferenceGUI
    {
        static readonly Dictionary<Type, Type[]> Candidates = new Dictionary<Type, Type[]>();
        static readonly Dictionary<string, Type> Types = new Dictionary<string, Type>();
        static readonly List<Action> Deferred = new List<Action>();
        static readonly HashSet<long> Ancestors = new HashSet<long>();
        static int drawDepth;

        public static void DrawChildren(SerializedProperty parent)
        {
            DrawScope(() => Children(parent));
        }

        public static void DrawProperty(SerializedProperty p, GUIContent label)
        {
            DrawScope(() => Property(p, label));
        }

        static void DrawScope(Action draw)
        {
            bool outer = drawDepth++ == 0;
            bool finished = false;
            try { draw(); finished = true; }
            finally
            {
                drawDepth--;
                if (outer)
                {
                    var actions = Deferred.ToArray();
                    Deferred.Clear();
                    Ancestors.Clear();
                    if (finished)
                        foreach (var action in actions) action();
                }
            }
        }

        static void Children(SerializedProperty parent)
        {
            var child = parent.Copy();
            var end = parent.GetEndProperty();
            if (!child.Next(true)) return;
            do
            {
                if (SerializedProperty.EqualContents(child, end) || child.depth <= parent.depth) break;
                Property(child.Copy(), new GUIContent(child.displayName));
            } while (child.Next(false));
        }

        static void Property(SerializedProperty p, GUIContent label)
        {
            if (drawDepth > 64) { EditorGUILayout.LabelField("嵌套过深"); return; }
            if (p.propertyType == SerializedPropertyType.ManagedReference)
                Managed(p, label);
            else if (p.isArray && p.propertyType != SerializedPropertyType.String && IsComplexList(p))
                List(p, label);
            else if (p.propertyType == SerializedPropertyType.Generic && !p.isArray && p.hasChildren)
            {
                p.isExpanded = EditorGUILayout.Foldout(p.isExpanded, label, true);
                if (p.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    try { DrawChildren(p); }
                    finally { EditorGUI.indentLevel--; }
                }
            }
            else EditorGUILayout.PropertyField(p, label, true);
        }

        public static bool IsManagedList(SerializedProperty p)
        {
            return p.isArray && p.propertyType != SerializedPropertyType.String &&
                (p.arrayElementType.StartsWith("managedReference<", StringComparison.Ordinal) ||
                 (p.arraySize > 0 && p.GetArrayElementAtIndex(0).propertyType == SerializedPropertyType.ManagedReference));
        }

        static bool IsComplexList(SerializedProperty p)
        {
            if (IsManagedList(p)) return true;
            if (p.arraySize > 0) return p.GetArrayElementAtIndex(0).propertyType == SerializedPropertyType.Generic;
            Type type = FieldType(p);
            return type != null && !type.IsPrimitive && !type.IsEnum && type != typeof(string) &&
                !typeof(UnityEngine.Object).IsAssignableFrom(type) && !type.IsValueType;
        }

        static void List(SerializedProperty p, GUIContent label)
        {
            bool managed = IsManagedList(p);
            using (new EditorGUILayout.HorizontalScope())
            {
                p.isExpanded = EditorGUILayout.Foldout(p.isExpanded, new GUIContent(label.text + " (" + p.arraySize + ")"), true);
                int size = Mathf.Max(0, EditorGUILayout.DelayedIntField(p.arraySize, GUILayout.Width(55)));
                if (size != p.arraySize)
                    Queue(p, list =>
                    {
                        int old = list.arraySize;
                        list.arraySize = size;
                        for (int n = old; n < size; n++)
                        {
                            var item = list.GetArrayElementAtIndex(n);
                            if (managed) item.managedReferenceValue = null;
                            else Initialize(item);
                        }
                    });
            }
            if (!p.isExpanded) return;
            if (managed && DeclaredType(p) == null)
                EditorGUILayout.HelpBox("无法解析声明类型", MessageType.Warning);
            EditorGUI.indentLevel++;
            try
            {
                for (int i = 0; i < p.arraySize; i++)
                {
                    int index = i;
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("元素 " + (i + 1));
                            using (new EditorGUI.DisabledScope(i == 0))
                                if (GUILayout.Button("↑", GUILayout.Width(26))) Queue(p, a => a.MoveArrayElement(index, index - 1));
                            using (new EditorGUI.DisabledScope(i == p.arraySize - 1))
                                if (GUILayout.Button("↓", GUILayout.Width(26))) Queue(p, a => a.MoveArrayElement(index, index + 1));
                            if (GUILayout.Button("×", GUILayout.Width(26))) Queue(p, a => Remove(a, index));
                        }
                        DrawProperty(p.GetArrayElementAtIndex(i), new GUIContent("元素 " + (i + 1)));
                    }
                }
                if (GUILayout.Button(managed ? "＋ 添加…" : "＋ 添加"))
                {
                    if (managed) ShowPicker(p, true);
                    else Queue(p, a => { int n = a.arraySize++; Initialize(a.GetArrayElementAtIndex(n)); });
                }
            }
            finally { EditorGUI.indentLevel--; }
        }

        static void Managed(SerializedProperty p, GUIContent label)
        {
            // A reference whose class is gone reads as null; only then look it up among the missing types.
            object value;
            try { value = p.managedReferenceValue; }
            catch (Exception) { value = null; }
            bool isMissing = false;
            ManagedReferenceMissingType lost = default;
            if (value == null)
                foreach (var m in SerializationUtility.GetManagedReferencesWithMissingTypes(p.serializedObject.targetObject))
                    if (m.referenceId == p.managedReferenceId) { lost = m; isMissing = true; break; }
            using (new EditorGUILayout.HorizontalScope())
            {
                string name = value == null ? "（空）" : ObjectNames.NicifyVariableName(value.GetType().Name);
                p.isExpanded = EditorGUILayout.Foldout(p.isExpanded, new GUIContent(label.text + "  " + name), true);
                if (GUILayout.Button("更换类型", GUILayout.Width(80))) ShowPicker(p, false);
            }
            if (isMissing)
            {
                EditorGUILayout.HelpBox("类型丢失: " + lost.namespaceName + "." + lost.className + " (rid " + lost.referenceId + ")", MessageType.Error);
                if (GUILayout.Button("移除")) Queue(p, x => x.managedReferenceValue = null);
                return;
            }
            if (DeclaredType(p) == null)
                EditorGUILayout.HelpBox("无法解析声明类型", MessageType.Warning);
            if (!p.isExpanded || value == null) return;
            long id = p.managedReferenceId;
            if (!Ancestors.Add(id)) { EditorGUILayout.LabelField("循环引用"); return; }
            EditorGUI.indentLevel++;
            try { DrawChildren(p); }
            finally { EditorGUI.indentLevel--; Ancestors.Remove(id); }
        }

        static void Queue(SerializedProperty p, Action<SerializedProperty> change)
        {
            var so = p.serializedObject;
            string path = p.propertyPath;
            Deferred.Add(() =>
            {
                if (so.targetObject == null) return;
                var fresh = so.FindProperty(path);
                if (fresh != null) change(fresh);
            });
        }

        public static void Remove(SerializedProperty list, int index)
        {
            if (index < 0 || index >= list.arraySize) return;
            int count = list.arraySize;
            var element = list.GetArrayElementAtIndex(index);
            if (element.propertyType == SerializedPropertyType.ManagedReference) element.managedReferenceValue = null;
            list.DeleteArrayElementAtIndex(index);
            if (list.arraySize == count) list.DeleteArrayElementAtIndex(index);
        }

        static void Initialize(SerializedProperty p)
        {
            Type type = FieldType(p);
            if (type == null || type.IsAbstract || type.IsInterface) return;
            // Field initializers (e.g. SkillSlotDef.Level = 1) instead of a copy of the previous element; keep the copy on failure.
            try { p.boxedValue = Activator.CreateInstance(type); }
            catch (Exception ex) { Debug.LogWarning("新元素初始化失败: " + ex.Message, p.serializedObject.targetObject); }
        }

        static void ShowPicker(SerializedProperty p, bool append)
        {
            Type baseType = DeclaredType(p);
            if (baseType == null)
            {
                var unavailable = new GenericMenu();
                unavailable.AddDisabledItem(new GUIContent("无法解析声明类型"));
                unavailable.ShowAsContext();
                return;
            }
            var so = p.serializedObject;
            string path = p.propertyPath;
            Type current = !append ? p.managedReferenceValue?.GetType() : null;
            // Preserve any text edits made before opening the popup.
            so.ApplyModifiedProperties();
            var menu = new GenericMenu();
            if (!append)
                menu.AddItem(new GUIContent("（空）"), current == null, () => Change(so, path, null, false));
            Type[] candidates = GetCandidates(baseType);
            foreach (Type candidate in candidates)
            {
                Type type = candidate;
                string group = (type.Namespace ?? "其他").Split('.').Last();
                menu.AddItem(new GUIContent(group + "/" + ObjectNames.NicifyVariableName(type.Name)), current == type,
                    () => Change(so, path, type, append));
            }
            if (candidates.Length == 0) menu.AddDisabledItem(new GUIContent("没有可用类型"));
            menu.ShowAsContext();
        }

        static void Change(SerializedObject so, string path, Type type, bool append)
        {
            if (so.targetObject == null) return;
            so.Update();
            var p = so.FindProperty(path);
            if (p == null) return;
            try
            {
                object value = type == null ? null : Activator.CreateInstance(type);
                if (append)
                {
                    int index = p.arraySize++;
                    p = p.GetArrayElementAtIndex(index);
                }
                else if (value != null && p.managedReferenceValue != null)
                {
                    try { JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(p.managedReferenceValue), value); }
                    catch (Exception) { /* A different field shape keeps the new type's defaults. */ }
                }
                p.managedReferenceValue = value;
                p.isExpanded = true;
                so.ApplyModifiedProperties();
            }
            catch (Exception ex) { Debug.LogWarning("类型更换失败: " + ex.Message, so.targetObject); so.Update(); }
        }

        public static Type DeclaredType(SerializedProperty p)
        {
            if (p.propertyType == SerializedPropertyType.ManagedReference)
                return ResolveType(p.managedReferenceFieldTypename);
            if (p.arraySize > 0 && p.GetArrayElementAtIndex(0).propertyType == SerializedPropertyType.ManagedReference)
                return DeclaredType(p.GetArrayElementAtIndex(0));
            string element = p.arrayElementType;
            if (element.StartsWith("managedReference<", StringComparison.Ordinal))
            {
                string name = element.Substring(17).TrimEnd('>');
                if (Types.TryGetValue(name, out Type known)) return known;
                var matches = AppDomain.CurrentDomain.GetAssemblies().SelectMany(SafeTypes)
                    .Where(t => (t.IsInterface || t.IsAbstract) && (t.Name == name || t.FullName == name))
                    .OrderByDescending(t => t.Assembly.GetName().Name == "Assembly-CSharp")
                    .ThenByDescending(t => t.Namespace != null && t.Namespace.StartsWith("Codebase.", StringComparison.Ordinal))
                    .ThenBy(t => t.FullName, StringComparer.Ordinal);
                Type found = matches.FirstOrDefault();
                Types[name] = found;
                return found;
            }
            return FieldType(p);
        }

        static Type ResolveType(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (Types.TryGetValue(name, out Type found)) return found;
            int space = name.IndexOf(' ');
            found = space > 0 ? Type.GetType(name.Substring(space + 1) + ", " + name.Substring(0, space), false) : Type.GetType(name, false);
            Types[name] = found;
            return found;
        }

        static IEnumerable<Type> SafeTypes(Assembly assembly)
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null); }
            catch (Exception) { return Array.Empty<Type>(); }
        }

        static Type[] GetCandidates(Type baseType)
        {
            if (!Candidates.TryGetValue(baseType, out Type[] types))
            {
                types = TypeCache.GetTypesDerivedFrom(baseType).Where(t => !t.IsAbstract && !t.IsInterface &&
                    !t.IsGenericType && !t.ContainsGenericParameters && !typeof(UnityEngine.Object).IsAssignableFrom(t) &&
                    t.IsDefined(typeof(SerializableAttribute), false) && t.GetConstructor(Type.EmptyTypes) != null)
                    .OrderBy(t => t.Name, StringComparer.Ordinal).ThenBy(t => t.FullName, StringComparer.Ordinal).ToArray();
                Candidates[baseType] = types;
            }
            return types;
        }

        // Also resolves empty inline-class lists; no skill/effect field names are hard-coded.
        internal static Type FieldType(SerializedProperty p)
        {
            Type type = p.serializedObject.targetObject.GetType();
            string[] parts = p.propertyPath.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == "Array" && i + 1 < parts.Length && parts[i + 1].StartsWith("data[", StringComparison.Ordinal))
                { type = ElementType(type); i++; }
                else
                {
                    FieldInfo field = null;
                    for (Type t = type; t != null && field == null; t = t.BaseType)
                        field = t.GetField(parts[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (field == null) return null;
                    type = field.FieldType;
                }
                // A concrete managed-reference type can introduce fields absent from its interface.
                string prefix = string.Join(".", parts.Take(i + 1));
                var owner = p.serializedObject.FindProperty(prefix);
                if (owner != null && owner.propertyType == SerializedPropertyType.ManagedReference && owner.managedReferenceValue != null)
                    type = owner.managedReferenceValue.GetType();
            }
            return p.isArray && p.propertyType != SerializedPropertyType.String ? ElementType(type) : type;
        }

        static Type ElementType(Type type)
        {
            if (type == null) return null;
            return type.IsArray ? type.GetElementType() : type.IsGenericType ? type.GetGenericArguments()[0] : null;
        }
    }
}
