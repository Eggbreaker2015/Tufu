// Opt-in restoration diagnostic. Records state; never starts/stops Play or edits gameplay.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RecoveryTools
{
    [InitializeOnLoad]
    public static class RecoveryPlayLifecycleTrace
    {
        [Serializable] private sealed class Row
        {
            public string utc, kind, scene, detail, stack;
            public bool playing, changing, compiling, updating;
            public int frame;
        }
        private static readonly string Root = Path.GetFullPath(Path.Combine(Application.dataPath, "../../RuntimeDiagnostics"));
        private static readonly Queue<string[]> Errors = new Queue<string[]>();
        private static double nextSample;
        private static int writes;
        private static int logWrites;
        private static string budgetOutput;
        static RecoveryPlayLifecycleTrace()
        {
            EditorApplication.playModeStateChanged += state => Write("play-state-" + state, null, new System.Diagnostics.StackTrace(1, true).ToString());
            AssemblyReloadEvents.beforeAssemblyReload += () => Write("before-assembly-reload", null, null);
            Application.logMessageReceivedThreaded += (m, s, t) => {
                if (t != LogType.Error && t != LogType.Exception && t != LogType.Assert) return;
                lock (Errors) if (Errors.Count < 32) Errors.Enqueue(new [] { t.ToString(), m, s });
            };
            EditorApplication.update += Tick;
            Write("diagnostic-domain-init", null, null);
        }
        private static string Output()
        {
            string request = Path.Combine(Root, "play-lifecycle.request");
            if (!File.Exists(request)) return null;
            string id = File.ReadAllText(request).Trim();
            if (id.Length == 0 || id.Length > 80) return null;
            foreach (char c in id) if (!char.IsLetterOrDigit(c) && c != '-' && c != '_') return null;
            return Path.Combine(Root, id + ".jsonl");
        }
        private static void Write(string kind, string detail, string stack)
        {
            string output = Output();
            if (output == null) return;
            if (budgetOutput != output)
            {
                budgetOutput = output;
                writes = 0;
                logWrites = 0;
            }
            // Cap routine sampling/logs, while preserving later Play/reload state events.
            if (kind == "sample" && ++writes > 256) return;
            if (kind.StartsWith("log-", StringComparison.Ordinal) && ++logWrites > 64) return;
            var row = new Row { utc = DateTime.UtcNow.ToString("o"), kind = kind, detail = detail, stack = stack,
                scene = SceneManager.GetActiveScene().path, playing = EditorApplication.isPlaying,
                changing = EditorApplication.isPlayingOrWillChangePlaymode, compiling = EditorApplication.isCompiling,
                updating = EditorApplication.isUpdating, frame = Time.frameCount };
            File.AppendAllText(output, JsonUtility.ToJson(row) + Environment.NewLine);
        }
        private static void Tick()
        {
            if (Output() == null) { lock (Errors) Errors.Clear(); return; }
            while (true) {
                string[] e;
                lock (Errors) { if (Errors.Count == 0) break; e = Errors.Dequeue(); }
                Write("log-" + e[0], e[1], e[2]);
            }
            if (EditorApplication.timeSinceStartup < nextSample) return;
            nextSample = EditorApplication.timeSinceStartup + .25;
            Write("sample", null, null);
        }
    }
}
