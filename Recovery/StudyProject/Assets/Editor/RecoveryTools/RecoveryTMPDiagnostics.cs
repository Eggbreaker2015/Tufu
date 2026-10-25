// Read-only diagnostics for recovering from previously empty TMP constructors.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace RecoveryTools
{
    [InitializeOnLoad]
    public static class RecoveryTMPDiagnostics
    {
        [Serializable] private class Entry { public string name; public string type; public string scene; public bool persistent; public bool active; public int localCorners; public int baseCorners; }
        [Serializable] private class Report { public string unityVersion; public string assemblyMvid; public string jsonAssemblyMvid; public bool packageJsonImporterPresent; public bool packageJsonEditorCompatible; public bool assetJsonImporterPresent; public bool assetJsonAnyCompatible; public bool assetJsonEditorCompatible; public int textCount; public int invalidLocalCorners; public int invalidBaseCorners; public List<Entry> invalid = new List<Entry>(); }
        private static double nextPoll;
        private static readonly string Root = Path.GetFullPath(Path.Combine(Application.dataPath, "../../TMPDiagnostics"));
        static RecoveryTMPDiagnostics() { EditorApplication.update += Poll; }
        private static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + .5;
            var request = Path.Combine(Root, "inspect.request");
            if (!File.Exists(request)) return;
            File.Delete(request);
            Export();
        }
        public static void Export()
        {
            var report = new Report { unityVersion = Application.unityVersion, assemblyMvid = typeof(TMP_Text).Module.ModuleVersionId.ToString() };
            report.jsonAssemblyMvid = typeof(Newtonsoft.Json.JsonConvert).Module.ModuleVersionId.ToString();
            var packageJson = AssetImporter.GetAtPath("Packages/com.unity.nuget.newtonsoft-json/Runtime/Newtonsoft.Json.dll") as PluginImporter;
            report.packageJsonImporterPresent = packageJson != null;
            report.packageJsonEditorCompatible = packageJson != null && packageJson.GetCompatibleWithEditor();
            var assetJson = AssetImporter.GetAtPath("Assets/Plugins/Newtonsoft.Json.dll") as PluginImporter;
            report.assetJsonImporterPresent = assetJson != null;
            report.assetJsonAnyCompatible = assetJson != null && assetJson.GetCompatibleWithAnyPlatform();
            report.assetJsonEditorCompatible = assetJson != null && assetJson.GetCompatibleWithEditor();
            foreach (var text in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                if (text == null) continue;
                report.textCount++;
                var localField = text.GetType().GetField("m_RectTransformCorners", BindingFlags.Instance | BindingFlags.NonPublic);
                var baseField = typeof(TMP_Text).GetField("m_textContainerLocalCorners", BindingFlags.Instance | BindingFlags.NonPublic);
                var local = localField?.GetValue(text) as Vector3[];
                var corners = baseField?.GetValue(text) as Vector3[];
                var localBad = localField != null && (local == null || local.Length < 4);
                var baseBad = corners == null || corners.Length < 4;
                if (localBad) report.invalidLocalCorners++;
                if (baseBad) report.invalidBaseCorners++;
                if ((localBad || baseBad) && report.invalid.Count < 30)
                    report.invalid.Add(new Entry { name = text.name, type = text.GetType().FullName, scene = text.gameObject.scene.path, persistent = EditorUtility.IsPersistent(text), active = text.gameObject.activeInHierarchy, localCorners = local == null ? -1 : local.Length, baseCorners = corners == null ? -1 : corners.Length });
            }
            Directory.CreateDirectory(Root);
            File.WriteAllText(Path.Combine(Root, "tmp-state.json"), JsonUtility.ToJson(report, true));
            Debug.Log("RECOVERY_TMP_DIAGNOSTICS texts=" + report.textCount + " badLocal=" + report.invalidLocalCorners + " badBase=" + report.invalidBaseCorners + " mvid=" + report.assemblyMvid);
        }
    }
}
