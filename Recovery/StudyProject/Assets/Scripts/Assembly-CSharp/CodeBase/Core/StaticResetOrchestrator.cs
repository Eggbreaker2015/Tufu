using UnityEngine;

namespace Codebase.Core
{
	public static class StaticResetOrchestrator
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticState()
		{
		}
	}
}
