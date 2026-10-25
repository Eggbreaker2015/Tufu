using UnityEditor;
using UnityEditor.SceneManagement;

// Editor convenience for the reconstructed project: the original player starts
// in Boot. Preserve a developer's explicitly selected Play Mode start scene.
public static class RecoveredPlayModeStartScene
{
    [InitializeOnLoadMethod]
    private static void SetDefault()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorSceneManager.playModeStartScene != null) return;
        UseBoot();
    }

    [MenuItem("Tools/APK Learning Project/Use Boot for Play Mode")]
    public static void UseBoot()
    {
        var boot = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Boot.unity");
        if (boot != null) EditorSceneManager.playModeStartScene = boot;
    }
}
