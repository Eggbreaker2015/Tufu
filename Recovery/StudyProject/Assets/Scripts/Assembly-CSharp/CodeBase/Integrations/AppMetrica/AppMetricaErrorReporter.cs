using UnityEngine;

namespace Codebase.Integrations.AppMetrica
{
	public class AppMetricaErrorReporter : MonoBehaviour
	{
		private void OnEnable()
		{
		}

		private void OnDisable()
		{
		}

		private void LogMessageReceived(string condition, string stackTrace, LogType type)
		{
		}

		private static bool ShouldIgnoreError(string condition, string stackTrace)
		{
			return false;
		}
	}
}
