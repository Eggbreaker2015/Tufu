// Local editor diagnostics for this restoration. Not recovered game code.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RecoveryTools
{
    [InitializeOnLoad]
    public static class RecoveryControl
    {
        [Serializable] private class Command
        {
            public string id;
            public string action;
            public string assetPath;
            public string cameraName;
            public int width = 1280;
            public int height = 720;
        }
        [Serializable] private class Reply
        {
            public string id;
            public string action;
            public bool success;
            public string error;
            public string output;
            public State state;
            public List<ScriptInfo> scripts;
        }
        [Serializable] private class ScriptInfo
        {
            public string name;
            public string fullType;
            public string guid;
            public long fileID;
        }
        [Serializable] private class SceneInfo
        {
            public string path;
            public bool dirty;
            public bool loaded;
            public string[] roots;
        }
        [Serializable] private class CameraInfo
        {
            public string name;
            public string hierarchy;
            public bool enabled;
            public bool active;
            public bool orthographic;
            public float size;
            public Vector3 position;
            public Vector3 rotation;
            public float depth;
            public int cullingMask;
        }
        [Serializable] private class State
        {
            public string editorVersion;
            public bool playing;
            public bool paused;
            public bool compiling;
            public bool updating;
            public List<SceneInfo> scenes = new List<SceneInfo>();
            public List<CameraInfo> cameras = new List<CameraInfo>();
        }
        private static readonly string Root;
        private static double nextPoll;
        static RecoveryControl()
        {
            Root = Path.GetFullPath(Path.Combine(Application.dataPath, "../../EditorControl"));
            foreach (string folder in new[] { "inbox", "processed", "results" })
                Directory.CreateDirectory(Path.Combine(Root, folder));
            EditorApplication.update += Poll;
        }
        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            nextPoll = EditorApplication.timeSinceStartup + 0.4;
            string file = Directory.GetFiles(Path.Combine(Root, "inbox"), "*.json").OrderBy(p => p, StringComparer.Ordinal).FirstOrDefault();
            if (file == null) return;
            string id = Path.GetFileNameWithoutExtension(file);
            var reply = new Reply { id = id };
            try
            {
                string json = File.ReadAllText(file);
                File.Move(file, Path.Combine(Root, "processed", Path.GetFileName(file)));
                Command command = JsonUtility.FromJson<Command>(json);
                reply.action = command.action;
                switch (command.action)
                {
                    case "state": reply.state = GetState(); break;
                    case "assembly-scripts":
                        reply.scripts = new List<ScriptInfo>();
                        foreach (MonoScript script in AssetDatabase.LoadAllAssetsAtPath(command.assetPath).OfType<MonoScript>())
                        {
                            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(script, out string guid, out long fileID);
                            reply.scripts.Add(new ScriptInfo { name = script.name, fullType = script.GetClass()?.FullName, guid = guid, fileID = fileID });
                        }
                        break;
                    case "render":
                        reply.output = Render(command, id);
                        break;
                    case "play":
                        EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
                        break;
                    case "stop":
                        EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
                        break;
                    case "validate":
                        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before validation.");
                        for (int i = 0; i < SceneManager.sceneCount; i++)
                            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("An open scene has unsaved changes; validation will not discard them.");
                        SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
                        try
                        {
                            typeof(RecoveryValidation).GetMethod("Validate", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { false });
                        }
                        finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
                        reply.state = GetState();
                        break;
                    default: throw new InvalidOperationException("Unknown diagnostic action: " + command.action);
                }
                reply.success = true;
            }
            catch (Exception exception) { reply.error = exception.ToString(); }
            File.WriteAllText(Path.Combine(Root, "results", id + ".json"), JsonUtility.ToJson(reply, true));
        }
        private static State GetState()
        {
            var state = new State { editorVersion = Application.unityVersion, playing = EditorApplication.isPlaying,
                paused = EditorApplication.isPaused, compiling = EditorApplication.isCompiling, updating = EditorApplication.isUpdating };
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                state.scenes.Add(new SceneInfo { path = scene.path, dirty = scene.isDirty, loaded = scene.isLoaded,
                    roots = scene.isLoaded ? scene.GetRootGameObjects().Select(g => g.name).ToArray() : new string[0] });
            }
            foreach (Camera camera in Resources.FindObjectsOfTypeAll<Camera>().Where(c => c.gameObject.scene.IsValid()))
                state.cameras.Add(new CameraInfo { name = camera.name, hierarchy = PathFor(camera.transform), enabled = camera.enabled,
                    active = camera.gameObject.activeInHierarchy, orthographic = camera.orthographic, size = camera.orthographicSize,
                    position = camera.transform.position, rotation = camera.transform.eulerAngles, depth = camera.depth, cullingMask = camera.cullingMask });
            return state;
        }
        private static string Render(Command command, string id)
        {
            Camera camera = string.IsNullOrEmpty(command.cameraName) ? SceneView.lastActiveSceneView?.camera :
                Resources.FindObjectsOfTypeAll<Camera>().FirstOrDefault(c => c.gameObject.scene.IsValid() && c.name == command.cameraName);
            if (camera == null) throw new InvalidOperationException("Requested camera is unavailable.");
            int width = Mathf.Clamp(command.width, 64, 2048), height = Mathf.Clamp(command.height, 64, 2048);
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousTarget = camera.targetTexture, previousActive = RenderTexture.active;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                string output = Path.Combine(Root, "results", id + ".png");
                File.WriteAllBytes(output, image.EncodeToPNG());
                return output;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }
        private static string PathFor(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null) { transform = transform.parent; path = transform.name + "/" + path; }
            return path;
        }
    }
}
