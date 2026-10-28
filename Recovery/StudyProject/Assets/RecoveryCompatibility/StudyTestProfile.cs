#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RecoveryCompatibility
{
    // Editor-only save routing. Player builds keep the original save and clock keys.
    public static class StudyTestProfile
    {
        public const string EnabledKey = "Tufu.StudyTest.SeparateSave";
        public static bool IsEnabled => SessionState.GetBool(EnabledKey, false);
        public static string NormalDirectory => Application.persistentDataPath;
        public static string TestDirectory => Path.Combine(NormalDirectory, "StudyTests", Hash128.Compute(Application.dataPath).ToString());
        public static string SaveDirectory => IsEnabled ? TestDirectory : NormalDirectory;
        public static string TimeOffsetKey => IsEnabled ? "StudyTest.TimeSkipSeconds." + Hash128.Compute(Application.dataPath) : "TimeSkipSeconds";
    }
}
#endif
