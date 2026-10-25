// Restoration diagnostics only. This is not recovered game logic.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace RecoveryTools
{
    [InitializeOnLoad]
    public static class RecoveryRuntimeDiagnostics
    {
        [Serializable] private class Request { public string id; public bool enterPlay; public bool stopAfter; public float seconds = 12; }
        [Serializable] private class Log { public string type; public string message; public string stack; }
        [Serializable] private class UnitInfo { public string type; public string hierarchy; public bool active; public Vector3 position; public bool onNavMesh; public Vector3 navMeshPoint; }
        [Serializable] private class CameraInfo { public string hierarchy; public bool enabled; public bool active; public Vector3 position; public Vector3 rotation; }
        [Serializable] private class Result
        {
            public string id;
            public string unityVersion;
            public bool playing;
            public bool compiling;
            public string scene;
            public double observedSeconds;
            public int playerCount;
            public int activeEnemies;
            public int totalEnemies;
            public int activeGameObjects;
            public List<UnitInfo> units = new List<UnitInfo>();
            public List<CameraInfo> cameras = new List<CameraInfo>();
            public List<Log> logs = new List<Log>();
        }
        private static readonly string Root = Path.GetFullPath(Path.Combine(Application.dataPath,"../../RuntimeDiagnostics"));
        private static readonly List<Log> Logs = new List<Log>();
        private const string SessionKey="Recovery.RuntimeProbe.Request";
        private static Request request;
        private static double started;
        private static double nextPoll;
        static RecoveryRuntimeDiagnostics()
        {
            Directory.CreateDirectory(Root);
            string saved=SessionState.GetString(SessionKey,"");
            if (!string.IsNullOrEmpty(saved))
            {
                request=JsonUtility.FromJson<Request>(saved);
                started=SessionState.GetFloat(SessionKey+".Started",(float)EditorApplication.timeSinceStartup);
            }
            Application.logMessageReceivedThreaded += OnLog;
            EditorApplication.update += Poll;
            EditorApplication.playModeStateChanged += state =>
            {
                if (request!=null && state==PlayModeStateChange.EnteredPlayMode)
                {
                    started=EditorApplication.timeSinceStartup;
                    SessionState.SetFloat(SessionKey+".Started",(float)started);
                }
            };
        }
        private static void OnLog(string message,string stack,LogType type)
        {
            if (request == null || (type!=LogType.Error && type!=LogType.Exception && type!=LogType.Assert)) return;
            lock (Logs)
                if (Logs.Count < 150) Logs.Add(new Log { type=type.ToString(),message=message,stack=stack });
        }
        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup<nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            nextPoll=EditorApplication.timeSinceStartup+.25;
            if (request == null)
            {
                string path=Path.Combine(Root,"capture.request.json");
                if (!File.Exists(path)) return;
                request=JsonUtility.FromJson<Request>(File.ReadAllText(path));
                File.Move(path,Path.Combine(Root,"request-"+SafeId(request.id)+".json"));
                lock (Logs) Logs.Clear();
                started=EditorApplication.timeSinceStartup;
                SessionState.SetString(SessionKey,JsonUtility.ToJson(request));
                SessionState.SetFloat(SessionKey+".Started",(float)started);
                if (request.enterPlay && !EditorApplication.isPlaying)
                { EditorApplication.delayCall += () => EditorApplication.isPlaying=true; return; }
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode!=EditorApplication.isPlaying) return;
            if (EditorApplication.timeSinceStartup-started<Mathf.Clamp(request.seconds,0,120)) return;
            var result=new Result { id=request.id,unityVersion=Application.unityVersion,
                playing=EditorApplication.isPlaying,compiling=EditorApplication.isCompiling,
                scene=SceneManager.GetActiveScene().path,observedSeconds=EditorApplication.timeSinceStartup-started };
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>().Where(g=>g.scene.IsValid()))
                if (go.activeInHierarchy) result.activeGameObjects++;
            foreach (var component in Resources.FindObjectsOfTypeAll<MonoBehaviour>().Where(c=>c!=null && c.gameObject.scene.IsValid()))
            {
                string name=component.GetType().Name;
                bool player=name=="PlayerUnit",enemy=name=="EnemyUnit";
                if (!player && !enemy) continue;
                if (player) result.playerCount++;
                else { result.totalEnemies++;if (component.gameObject.activeInHierarchy) result.activeEnemies++; }
                if (player || (component.gameObject.activeInHierarchy && result.units.Count<30))
                {
                    var unit=new UnitInfo { type=component.GetType().FullName,hierarchy=PathFor(component.transform),active=component.gameObject.activeInHierarchy,position=component.transform.position };
                    unit.onNavMesh=NavMesh.SamplePosition(unit.position,out NavMeshHit hit,1,NavMesh.AllAreas);
                    if (unit.onNavMesh) unit.navMeshPoint=hit.position;
                    result.units.Add(unit);
                }
            }
            foreach (var camera in Resources.FindObjectsOfTypeAll<Camera>().Where(c=>c.gameObject.scene.IsValid()))
                result.cameras.Add(new CameraInfo { hierarchy=PathFor(camera.transform),enabled=camera.enabled,active=camera.gameObject.activeInHierarchy,position=camera.transform.position,rotation=camera.transform.eulerAngles });
            lock (Logs) result.logs.AddRange(Logs);
            bool stop=request.stopAfter && EditorApplication.isPlaying;
            File.WriteAllText(Path.Combine(Root,SafeId(request.id)+".json"),JsonUtility.ToJson(result,true));
            request=null;
            SessionState.EraseString(SessionKey);
            if (stop) EditorApplication.delayCall += () => EditorApplication.isPlaying=false;
        }
        private static string SafeId(string value) { return string.Concat((value??"capture").Where(c=>char.IsLetterOrDigit(c)||c=='-'||c=='_')); }
        private static string PathFor(Transform transform)
        {
            string path=transform.name;
            while (transform.parent!=null) { transform=transform.parent;path=transform.name+"/"+path; }
            return path;
        }
    }
}
