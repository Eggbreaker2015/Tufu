using Codebase.Events.Components;
using Scellecs.Morpeh.Providers;

namespace Codebase.Events
{
	public class EventBossGateProvider : MonoProvider<EventBossGateComponent>
	{
		private void Start()
		{
			// Original APK ARM64 0x14d90c4: bind the scene gate before the opening system uses it.
			GetData().Gate = GetComponent<EventBossGate>();
		}
	}
}
