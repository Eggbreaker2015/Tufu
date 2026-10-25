using System;

namespace Codebase.Events
{
	[Serializable]
	public class EventProgressBarStyle
	{
		public EventProgressStageStyle Mob;

		public EventProgressStageStyle Boss;

		public EventProgressStageStyle GrandBoss;

		public EventProgressStageStyle GetStyle(ProgressStageType type)
		{
			// Original APK ARM64 ELF VA 0x14daeac.
			return type == ProgressStageType.Boss ? Boss : type == ProgressStageType.GrandBoss ? GrandBoss : Mob;
		}
	}
}
