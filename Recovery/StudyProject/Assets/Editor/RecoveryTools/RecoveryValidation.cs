// Added for this learning reconstruction. This is not recovered game code.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RecoveryTools
{
    public static class RecoveryValidation
    {
        [Serializable]
        public class SceneResult
        {
            public string path;
            public int rootObjects;
            public int gameObjects;
            public int missingScripts;
            public int renderers;
            public int missingMaterialSlots;
            public int meshFilters;
            public int missingMeshes;
            public List<string> objectsWithNullMaterials = new List<string>();
            public List<string> objectsWithNullMeshes = new List<string>();
        }

        [Serializable]
        public class ConfigResult
        {
            public string path;
            public string loadedType;
            public List<string> serializedRootFields = new List<string>();
            public int populatedManagedReferenceSlots;
            public int uniqueManagedReferences;
            public int nullManagedReferenceSlots;
            public List<string> managedReferenceTypes = new List<string>();
        }

        [Serializable]
        public class Report
        {
            public string editorVersion;
            public string projectPath;
            public string sourceUnityVersion = "6000.2.6f2";
            public string scope = "Editor import and scene structure only. IL2CPP methods are stubs; gameplay was not tested.";
            public bool completed;
            public int scriptAssets;
            public int prefabAssets;
            public int materialAssets;
            public int textureAssets;
            public List<SceneResult> scenes = new List<SceneResult>();
            public List<ConfigResult> configs = new List<ConfigResult>();
            public List<string> failures = new List<string>();
        }

        [MenuItem("Tools/APK Learning Project/Validate Scene Structure")]
        public static void ValidateMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try { Validate(false); }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }

        public static void ValidateBatch() { Validate(true); }

        private static void Validate(bool batch)
        {
            var report = new Report
            {
                editorVersion = Application.unityVersion,
                projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..")),
                scriptAssets = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" }).Length,
                prefabAssets = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }).Length,
                materialAssets = AssetDatabase.FindAssets("t:Material", new[] { "Assets" }).Length,
                textureAssets = AssetDatabase.FindAssets("t:Texture", new[] { "Assets" }).Length
            };
            try
            {
                report.configs.Add(InspectConfig("Assets/MonoBehaviour/Boss Tower Config.asset"));
                report.configs.Add(InspectConfig("Assets/MonoBehaviour/Player Config.asset"));
                foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
                {
                    if (!entry.enabled) continue;
                    Scene scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
                    var result = new SceneResult { path = entry.path };
                    GameObject[] roots = scene.GetRootGameObjects();
                    result.rootObjects = roots.Length;
                    foreach (GameObject root in roots)
                    {
                        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                        {
                            result.gameObjects++;
                            result.missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject);
                            foreach (Renderer renderer in item.GetComponents<Renderer>())
                            {
                                result.renderers++;
                                foreach (Material material in renderer.sharedMaterials)
                                    if (material == null)
                                    {
                                        result.missingMaterialSlots++;
                                        result.objectsWithNullMaterials.Add(HierarchyPath(item));
                                    }
                            }
                            foreach (MeshFilter mesh in item.GetComponents<MeshFilter>())
                            {
                                result.meshFilters++;
                                if (mesh.sharedMesh == null)
                                {
                                    result.missingMeshes++;
                                    result.objectsWithNullMeshes.Add(HierarchyPath(item));
                                }
                            }
                        }
                    }
                    report.scenes.Add(result);
                }
                report.completed = true;
            }
            catch (Exception exception)
            {
                report.failures.Add(exception.ToString());
                Debug.LogException(exception);
            }
            string evidenceDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Evidence"));
            Directory.CreateDirectory(evidenceDirectory);
            string output = Path.Combine(evidenceDirectory, "unity-validation.json");
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            Debug.Log("RECOVERY_VALIDATION " + output + " completed=" + report.completed);
            if (batch) EditorApplication.Exit(report.completed ? 0 : 2);
        }

        [MenuItem("Tools/APK Learning Project/Open Boot Scene")]
        public static void OpenBoot() { Open("Assets/Scenes/Boot.unity"); }

        [MenuItem("Tools/APK Learning Project/Open Main Scene")]
        public static void OpenMain() { Open("Assets/Scenes/Main.unity"); }

        private static void Open(string path)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }

        private static string HierarchyPath(Transform item)
        {
            string path = item.name;
            for (Transform parent = item.parent; parent != null; parent = parent.parent)
                path = parent.name + "/" + path;
            return path;
        }

        private static ConfigResult InspectConfig(string path)
        {
            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null) throw new InvalidDataException("Config could not be loaded: " + path);
            var result = new ConfigResult { path = path, loadedType = asset.GetType().FullName };
            var serialized = new SerializedObject(asset);
            var iterator = serialized.GetIterator();
            if (iterator.NextVisible(true))
            {
                do { result.serializedRootFields.Add(iterator.name); }
                while (iterator.NextVisible(false));
            }
            var ids = new HashSet<long>();
            var types = new HashSet<string>();
            iterator = serialized.GetIterator();
            while (iterator.Next(true))
            {
                if (iterator.propertyType != SerializedPropertyType.ManagedReference) continue;
                if (string.IsNullOrEmpty(iterator.managedReferenceFullTypename))
                    result.nullManagedReferenceSlots++;
                else
                {
                    result.populatedManagedReferenceSlots++;
                    ids.Add(iterator.managedReferenceId);
                    types.Add(iterator.managedReferenceFullTypename);
                }
            }
            result.uniqueManagedReferences = ids.Count;
            result.managedReferenceTypes.AddRange(types);
            result.managedReferenceTypes.Sort(StringComparer.Ordinal);
            return result;
        }
    }
}
