namespace Codebase.Core
{
	internal static class BootLoadingProgress
	{
		private const float ScenePreloadEnd = 0.75f;

		private const float FinalizingStart = 0.82f;

		private const float ActivationStart = 0.88f;

		internal const float UnityPreloadComplete = 0.9f;

		public static float Finalizing => 0f;

		public static float FromScenePreload(float asyncProgress)
		{
			return 0f;
		}

		public static float FromSceneActivation(float asyncProgress)
		{
			return 0f;
		}
	}
}
