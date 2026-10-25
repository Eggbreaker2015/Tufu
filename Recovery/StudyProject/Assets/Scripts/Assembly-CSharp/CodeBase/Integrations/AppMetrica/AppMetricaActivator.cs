using UnityEngine;

namespace Codebase.Integrations.AppMetrica
{
	public static class AppMetricaActivator
	{
		private const string FIRST_START = "first_start";

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Activate()
		{
		}

		private static bool IsFirstLaunch()
		{
			return false;
		}
	}
}
