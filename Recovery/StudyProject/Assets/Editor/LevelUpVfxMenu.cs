using Codebase.Vfx;
using UnityEditor;
using UnityEngine;

internal static class LevelUpVfxMenu
{
	[MenuItem("Tools/学习测试/播放升级特效")]
	private static void Play()
	{
		LevelUpVfxPlayer player = Object.FindAnyObjectByType<LevelUpVfxPlayer>();
		if (player == null || !player.PlayOnPlayer())
			Debug.LogWarning("升级特效：请在 Play 模式进入已有玩家的游戏场景后重试。");
	}

	[MenuItem("Tools/学习测试/播放升级特效", true)]
	private static bool CanPlay()
	{
		return EditorApplication.isPlaying && !EditorApplication.isCompiling;
	}
}
