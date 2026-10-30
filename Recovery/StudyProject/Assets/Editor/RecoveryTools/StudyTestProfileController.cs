#if UNITY_EDITOR
using System;
using System.IO;
using Codebase.Core;
using Codebase.Core.UserData;
using RecoveryCompatibility;
using UnityEditor;
using UnityEngine;

namespace RecoveryTools
{
    [InitializeOnLoad]
    public static class StudyTestProfileController
    {
        public enum Request { None, CloneNormal, FreshTest, ResumeTest, Normal }
        private const string PendingKey = "Tufu.StudyTest.PendingSwitch";
        private const string StatusKey = "Tufu.StudyTest.ProfileStatus";
        private static readonly string[] SaveFiles = { "game_data.bin", "game_data_temp.bin", "game_data_backup.bin" };
        public static bool IsSwitching => SessionState.GetInt(PendingKey, 0) != 0;
        public static string LastStatus => SessionState.GetString(StatusKey, "");
        public static string ActiveSaveDirectory => SaveFolder(StudyTestProfile.SaveDirectory);

        static StudyTestProfileController()
        {
            EditorApplication.update -= ProcessPending;
            EditorApplication.update += ProcessPending;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (StudyTestProfile.IsEnabled && (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode))
                UnityEngine.Time.timeScale = 1f;
        }

        public static void SwitchAndRestart(Request request)
        {
            if (request == Request.None || IsSwitching || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying))
                throw new InvalidOperationException("请等待当前编译、导入或存档切换完成。");
            if (EditorApplication.isPlaying && GameContext.Has<GameDataService>()) SaveCurrent();
            SessionState.SetInt(PendingKey, (int)request);
            SessionState.SetString(StatusKey, "正在保存并切换存档…");
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        }

        public static void SaveCurrent()
        {
            if (!EditorApplication.isPlaying || !GameContext.Has<GameDataService>()) return;
            var service = GameContext.Get<GameDataService>();
            bool saved = false;
            void OnSaved(GameDataSaveReason reason) { if (reason == GameDataSaveReason.Forced) saved = true; }
            service.OnAfterSave += OnSaved;
            try { service.ForceSave(); }
            finally { service.OnAfterSave -= OnSaved; }
            if (!saved) throw new IOException("未确认存档保存成功，请查看 Console。");
        }

        public static string BackupCurrent()
        {
            SaveCurrent();
            return BackupDirectory(ActiveSaveDirectory, StudyTestProfile.IsEnabled ? "test" : "normal");
        }

        private static void ProcessPending()
        {
            if (!IsSwitching || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            var request = (Request)SessionState.GetInt(PendingKey, 0);
            SessionState.SetInt(PendingKey, 0);
            try
            {
                string testDirectory = SaveFolder(CheckedTestDirectory());
                bool useTest = request != Request.Normal;
                if (request == Request.CloneNormal)
                {
                    string source = Path.Combine(SaveFolder(StudyTestProfile.NormalDirectory), SaveFiles[0]);
                    if (!File.Exists(source) || new FileInfo(source).Length == 0)
                        throw new IOException("正常存档尚未生成，请先正常运行一次游戏。");
                    BackupDirectory(SaveFolder(StudyTestProfile.NormalDirectory), "normal-before-test");
                    BackupDirectory(testDirectory, "test-before-clone");
                    Directory.CreateDirectory(testDirectory);
                    File.Copy(source, Path.Combine(testDirectory, SaveFiles[0]), true);
                    File.Copy(source, Path.Combine(testDirectory, SaveFiles[2]), true);
                    DeleteTestFile(testDirectory, SaveFiles[1]);
                    PlayerPrefs.SetFloat(TestClockKey(), PlayerPrefs.GetFloat("TimeSkipSeconds", 0f));
                }
                else if (request == Request.FreshTest)
                {
                    BackupDirectory(testDirectory, "test-before-reset");
                    Directory.CreateDirectory(testDirectory);
                    foreach (string file in SaveFiles) DeleteTestFile(testDirectory, file);
                    PlayerPrefs.SetFloat(TestClockKey(), 0f);
                }
                else if (request == Request.ResumeTest && !File.Exists(Path.Combine(testDirectory, SaveFiles[0])))
                    throw new IOException("还没有测试存档，请先建立当前进度副本或新测试档。");
                PlayerPrefs.Save();
                SessionState.SetBool(StudyTestProfile.EnabledKey, useTest);
                SessionState.SetString(StatusKey, useTest ? "已切换到独立测试存档；正常存档保留。" : "已切回正常存档；测试进度仍保存在独立目录。");
                // ProcessPending already runs in an idle edit-mode update. Starting here avoids
                // delayCall waiting for a GUI redraw when the editor is unfocused or locked.
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                SessionState.SetString(StatusKey, "存档切换失败：" + exception.Message);
                Debug.LogException(exception);
            }
        }

        private static string TestClockKey()
        {
            return "StudyTest.TimeSkipSeconds." + Hash128.Compute(Application.dataPath);
        }

        // RPG mode keeps its saves in an Rpg subfolder of each profile, matching GameDataService.
        private static string SaveFolder(string profileDirectory)
        {
            return Codebase.Rpg.RpgModeConfig.Enabled ? Path.Combine(profileDirectory, "Rpg") : profileDirectory;
        }

        private static string CheckedTestDirectory()
        {
            string normal = Path.GetFullPath(StudyTestProfile.NormalDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string test = Path.GetFullPath(StudyTestProfile.TestDirectory);
            if (!test.StartsWith(normal + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("测试存档目录必须位于游戏存档目录内。");
            return test;
        }

        private static void DeleteTestFile(string directory, string file)
        {
            if (!string.Equals(Path.GetFullPath(directory), SaveFolder(CheckedTestDirectory()), StringComparison.OrdinalIgnoreCase) || Array.IndexOf(SaveFiles, file) < 0)
                throw new InvalidOperationException("拒绝修改测试存档以外的文件。");
            string path = Path.Combine(directory, file);
            if (File.Exists(path)) File.Delete(path);
        }

        private static string BackupDirectory(string source, string label)
        {
            string destination = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Backups/QuickTest", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") + "-" + label));
            bool copied = false;
            foreach (string file in SaveFiles)
            {
                string path = Path.Combine(source, file);
                if (!File.Exists(path)) continue;
                Directory.CreateDirectory(destination);
                File.Copy(path, Path.Combine(destination, file));
                copied = true;
            }
            return copied ? destination : "当前目录尚无存档文件。";
        }
    }
}
#endif
