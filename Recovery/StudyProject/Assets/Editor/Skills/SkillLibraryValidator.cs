using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Codebase.BossTower;
using Codebase.Enemy;
using Codebase.Localization;
using Codebase.Pet;
using Codebase.Skills;
using UnityEditor;
using UnityEngine;

namespace Codebase.SkillsEditor
{
    public enum IssueSeverity { Error, Warning, Info }

    public sealed class SkillIssue
    {
        public IssueSeverity Severity;
        public string Message;
        public SkillLibrary Library;
        public int Tab = -1;
        public int Index = -1;
        public string PropertyPath;
    }

    public static class SkillLibraryValidator
    {
        internal static readonly string[] Roots = { "Skills", "Buffs", "Loadouts", "Vfx", "Summons" };

        public static List<SkillLibrary> FindLibraries()
        {
            return AssetDatabase.FindAssets("t:SkillLibrary", new[] { "Assets/Resources/Skills" })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<SkillLibrary>).Where(l => l != null).ToList();
        }

        internal static LocalizationDatabase FindLocalization()
        {
            return AssetDatabase.FindAssets("t:LocalizationDatabase").Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p, StringComparer.Ordinal).Select(AssetDatabase.LoadAssetAtPath<LocalizationDatabase>)
                .FirstOrDefault(l => l != null);
        }

        public static List<SkillIssue> Run()
        {
            var issues = new List<SkillIssue>();
            try { RunAll(issues); }
            catch (Exception ex) { Add(issues, null, IssueSeverity.Error, "校验失败: " + ex.Message); }
            return issues;
        }

        static void RunAll(List<SkillIssue> issues)
        {
            var libraries = FindLibraries();
            var keys = Enumerable.Range(0, 5).Select(_ => new Dictionary<string, SkillIssue>(StringComparer.Ordinal)).ToArray();
            // First pass collects every key, including keys in libraries with later validation errors.
            foreach (var lib in libraries)
            {
                try
                {
                    using (var so = new SerializedObject(lib))
                    {
                        for (int tab = 0; tab < Roots.Length; tab++)
                        {
                            var list = so.FindProperty(Roots[tab]);
                            if (list == null) continue;
                            for (int i = 0; i < list.arraySize; i++)
                            {
                                var entry = list.GetArrayElementAtIndex(i);
                                var keyProperty = entry.FindPropertyRelative("Key");
                                string key = keyProperty?.stringValue;
                                string path = keyProperty?.propertyPath ?? entry.propertyPath;
                                if (string.IsNullOrWhiteSpace(key))
                                { Add(issues, lib, IssueSeverity.Error, "键为空", path); continue; }
                                string identity = tab == 2 ? OwnerName(entry) + "/" + key : key;
                                var location = Location(lib, path);
                                if (keys[tab].TryGetValue(identity, out SkillIssue first))
                                {
                                    string message = "重复键 " + identity + ": " + AssetDatabase.GetAssetPath(first.Library) +
                                        "（条目 " + (first.Index + 1) + "） 与 " + AssetDatabase.GetAssetPath(lib) +
                                        "（条目 " + (i + 1) + "）；运行时按加载顺序保留首项";
                                    Add(issues, lib, IssueSeverity.Error, message, path);
                                }
                                else keys[tab].Add(identity, location);
                            }
                        }
                    }
                }
                catch (Exception ex) { Add(issues, lib, IssueSeverity.Error, "键校验失败: " + ex.Message); }
            }

            var petKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (string guid in AssetDatabase.FindAssets("t:PetConfig"))
            {
                var config = AssetDatabase.LoadAssetAtPath<PetConfig>(AssetDatabase.GUIDToAssetPath(guid));
                if (config?.Pets != null)
                    foreach (var pet in config.Pets) if (pet != null && pet.ItemKey != null) petKeys.Add(pet.ItemKey);
            }
            var bossKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (string guid in AssetDatabase.FindAssets("t:BossTowerConfig"))
            {
                var config = AssetDatabase.LoadAssetAtPath<BossTowerConfig>(AssetDatabase.GUIDToAssetPath(guid));
                if (config?.Bosses != null)
                    foreach (var boss in config.Bosses) if (boss != null && boss.Key != null) bossKeys.Add(boss.Key);
            }
            // Prefabs are inspected once per validation run; a matching valid prefab wins over a namesake.
            var enemies = new Dictionary<string, bool>(StringComparer.Ordinal);
            // A prefab root is named after its file, so only namesakes of Enemy loadout keys are loaded.
            var enemyKeys = new HashSet<string>(libraries.Where(l => l.Loadouts != null).SelectMany(l => l.Loadouts)
                .Where(l => l != null && l.OwnerKind == SkillOwnerKind.Enemy && !string.IsNullOrEmpty(l.Key)).Select(l => l.Key), StringComparer.Ordinal);
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!enemyKeys.Contains(Path.GetFileNameWithoutExtension(assetPath))) continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab == null) continue;
                bool valid = prefab.GetComponent<EnemyUnit>() != null;
                enemies[prefab.name] = valid || (enemies.TryGetValue(prefab.name, out bool previous) && previous);
            }
            var usedByPlayer = new HashSet<string>(StringComparer.Ordinal);
            foreach (var lib in libraries)
            {
                try
                {
                    if (lib.Loadouts == null) continue;
                    foreach (var loadout in lib.Loadouts)
                        if (loadout != null && (loadout.OwnerKind == SkillOwnerKind.Pet || loadout.OwnerKind == SkillOwnerKind.Hero) && loadout.Skills != null)
                            foreach (var slot in loadout.Skills) if (slot?.SkillKey != null) usedByPlayer.Add(slot.SkillKey);
                }
                catch (Exception ex) { Add(issues, lib, IssueSeverity.Error, "配置校验失败: " + ex.Message); }
            }
            var database = FindLocalization();
            var text = new Dictionary<string, string[]>(StringComparer.Ordinal);
            if (database == null) Add(issues, null, IssueSeverity.Warning, "未找到本地化数据库");
            else if (database.Entries != null)
                foreach (var entry in database.Entries)
                    if (entry.key != null && !text.ContainsKey(entry.key)) text.Add(entry.key, entry.values);

            foreach (var lib in libraries)
            {
                try
                {
                    using (var so = new SerializedObject(lib))
                    {
                        var missing = SerializationUtility.GetManagedReferencesWithMissingTypes(lib);
                        var ridPaths = new Dictionary<long, string>();
                        foreach (string root in Roots)
                        {
                            var list = so.FindProperty(root);
                            if (list == null) continue;
                            Walk(list, p =>
                            {
                                if (p.propertyType != SerializedPropertyType.ManagedReference) return;
                                if (!ridPaths.ContainsKey(p.managedReferenceId)) ridPaths.Add(p.managedReferenceId, p.propertyPath);
                                bool lost = missing.Any(m => m.referenceId == p.managedReferenceId);
                                if (!lost && p.managedReferenceValue == null && IsListElement(p))
                                    Add(issues, lib, IssueSeverity.Warning, "空效果/条件", p.propertyPath);
                            });
                            // Walk from visible list roots so paths remain usable by the window.
                            var owners = new List<SerializedProperty>();
                            Walk(list, p =>
                            {
                                while (owners.Count > 0 && !p.propertyPath.StartsWith(owners[owners.Count - 1].propertyPath + ".", StringComparison.Ordinal))
                                    owners.RemoveAt(owners.Count - 1);
                                if (p.propertyType == SerializedPropertyType.ManagedReference)
                                {
                                    if (!missing.Any(m => m.referenceId == p.managedReferenceId) && p.managedReferenceValue != null)
                                        owners.Add(p.Copy());
                                    return;
                                }
                                if (p.propertyType != SerializedPropertyType.String || owners.Count == 0) return;
                                var owner = owners[owners.Count - 1];
                                int keyTab = ReferenceTab(p.name, owner.managedReferenceFullTypename);
                                if (keyTab >= 0) CheckReference(issues, lib, p, keyTab, keys);
                            });
                        }
                        foreach (var lost in missing)
                        {
                            ridPaths.TryGetValue(lost.referenceId, out string path);
                            Add(issues, lib, IssueSeverity.Error, "类型丢失 " + lost.namespaceName + "." + lost.className + " (rid " + lost.referenceId + ")", path);
                        }
                        var loadouts = so.FindProperty("Loadouts");
                        if (loadouts != null)
                            for (int i = 0; i < loadouts.arraySize; i++)
                            {
                                var entry = loadouts.GetArrayElementAtIndex(i);
                                string key = entry.FindPropertyRelative("Key")?.stringValue ?? "";
                                string owner = OwnerName(entry);
                                string path = entry.propertyPath + ".Key";
                                switch (owner)
                                {
                                    case "Enemy":
                                        if (!enemies.TryGetValue(key, out bool valid)) Add(issues, lib, IssueSeverity.Error, "未找到敌人预制体: " + key, path);
                                        else if (!valid) Add(issues, lib, IssueSeverity.Warning, "预制体缺少 EnemyUnit: " + key, path);
                                        break;
                                    case "Boss":
                                        if (!bossKeys.Contains(key)) Add(issues, lib, IssueSeverity.Warning, "Boss 配置未包含键: " + key, path);
                                        break;
                                    case "Pet":
                                        if (!petKeys.Contains(key)) Add(issues, lib, IssueSeverity.Error, "宠物配置未包含键: " + key, path);
                                        break;
                                    case "Hero":
                                        if (key != "hero") Add(issues, lib, IssueSeverity.Warning, "主角键应为 hero: " + key, path);
                                        break;
                                }
                                var slots = entry.FindPropertyRelative("Skills");
                                if (slots != null)
                                    for (int n = 0; n < slots.arraySize; n++)
                                    {
                                        var skill = slots.GetArrayElementAtIndex(n).FindPropertyRelative("SkillKey");
                                        if (skill != null) CheckReference(issues, lib, skill, 0, keys);
                                    }
                                var ranged = entry.FindPropertyRelative("RangedAttack");
                                if (ranged != null)
                                    foreach (string field in new[] { "ProjectileVfxKey", "ImpactVfxKey" })
                                    {
                                        var vfx = ranged.FindPropertyRelative(field);
                                        if (vfx != null) CheckReference(issues, lib, vfx, 3, keys);
                                    }
                            }
                        if (database != null)
                        {
                            var skills = so.FindProperty("Skills");
                            if (skills != null)
                                for (int i = 0; i < skills.arraySize; i++)
                                {
                                    var entry = skills.GetArrayElementAtIndex(i);
                                    string key = entry.FindPropertyRelative("Key")?.stringValue ?? "";
                                    CheckText(issues, lib, text, "SKILL_NAME_" + key, IssueSeverity.Warning, entry.propertyPath);
                                    CheckText(issues, lib, text, "SKILL_DESC_" + key,
                                        usedByPlayer.Contains(key) ? IssueSeverity.Warning : IssueSeverity.Info, entry.propertyPath);
                                }
                        }
                    }
                }
                catch (Exception ex) { Add(issues, lib, IssueSeverity.Error, "库校验失败: " + ex.Message); }
            }
        }

        internal static void Walk(SerializedProperty root, Action<SerializedProperty> visit)
        {
            var p = root.Copy();
            var end = root.GetEndProperty();
            var seen = new HashSet<long>();
            bool enter = true;
            while (p.Next(enter) && !SerializedProperty.EqualContents(p, end) && p.depth > root.depth)
            {
                visit(p);
                // Next(true) expands collapsed references too; skip repeated identities to terminate cyclic graphs.
                enter = p.propertyType != SerializedPropertyType.ManagedReference || seen.Add(p.managedReferenceId);
            }
        }

        static bool IsListElement(SerializedProperty p) => p.propertyPath.EndsWith("]", StringComparison.Ordinal) && p.propertyPath.Contains(".Array.data[");

        static string OwnerName(SerializedProperty entry)
        {
            var owner = entry.FindPropertyRelative("OwnerKind");
            return owner != null && owner.enumValueIndex >= 0 && owner.enumValueIndex < owner.enumNames.Length
                ? owner.enumNames[owner.enumValueIndex] : "未知";
        }

        static int ReferenceTab(string name, string owner)
        {
            if (name.EndsWith("BuffKey", StringComparison.Ordinal)) return 1;
            if (name.EndsWith("VfxKey", StringComparison.Ordinal)) return 3;
            if (name.EndsWith("SummonKey", StringComparison.Ordinal)) return 4;
            if (name.EndsWith("SkillKey", StringComparison.Ordinal)) return 0;
            if (name == "Key" && owner.EndsWith(" " + typeof(Codebase.Skills.Conditions.HasBuffCondition).FullName, StringComparison.Ordinal)) return 1;
            return -1;
        }

        static void CheckReference(List<SkillIssue> issues, SkillLibrary lib, SerializedProperty p, int tab, Dictionary<string, SkillIssue>[] keys)
        {
            string key = p.stringValue;
            if (tab == 3 && string.IsNullOrEmpty(key)) return;
            if (string.IsNullOrWhiteSpace(key) || !keys[tab].ContainsKey(key))
                Add(issues, lib, IssueSeverity.Error, "未知引用 " + p.name + ": " + (string.IsNullOrWhiteSpace(key) ? "（空）" : key), p.propertyPath);
        }

        static void CheckText(List<SkillIssue> issues, SkillLibrary lib, Dictionary<string, string[]> text, string key, IssueSeverity severity, string path)
        {
            if (!text.TryGetValue(key, out string[] values)) Add(issues, lib, severity, "缺少本地化: " + key, path);
            else if (values == null || values.Length == 0 || string.IsNullOrWhiteSpace(values[0]))
                Add(issues, lib, IssueSeverity.Warning, "英文为空: " + key, path);
        }

        static SkillIssue Location(SkillLibrary lib, string path)
        {
            var issue = new SkillIssue { Library = lib, PropertyPath = path };
            if (path == null) return issue;
            for (int tab = 0; tab < Roots.Length; tab++)
            {
                string prefix = Roots[tab] + ".Array.data[";
                if (!path.StartsWith(prefix, StringComparison.Ordinal)) continue;
                int close = path.IndexOf(']', prefix.Length);
                if (close >= 0 && int.TryParse(path.Substring(prefix.Length, close - prefix.Length), out int index))
                { issue.Tab = tab; issue.Index = index; }
                break;
            }
            return issue;
        }

        static void Add(List<SkillIssue> issues, SkillLibrary lib, IssueSeverity severity, string message, string path = null)
        {
            var issue = Location(lib, path);
            issue.Severity = severity;
            issue.Message = message;
            issues.Add(issue);
        }

        [MenuItem("Tools/技能/校验技能库")]
        public static void LogValidation()
        {
            var issues = Run();
            foreach (var issue in issues)
            {
                string message = (issue.Library != null ? Path.GetFileName(AssetDatabase.GetAssetPath(issue.Library)) + ": " : "") + issue.Message;
                if (issue.Severity == IssueSeverity.Error) Debug.LogError(message, issue.Library);
                else if (issue.Severity == IssueSeverity.Warning) Debug.LogWarning(message, issue.Library);
                else Debug.Log(message, issue.Library);
            }
            Debug.Log("技能库校验完成：错误 " + issues.Count(i => i.Severity == IssueSeverity.Error) +
                "，警告 " + issues.Count(i => i.Severity == IssueSeverity.Warning) + "，提示 " + issues.Count(i => i.Severity == IssueSeverity.Info));
        }
    }
}
