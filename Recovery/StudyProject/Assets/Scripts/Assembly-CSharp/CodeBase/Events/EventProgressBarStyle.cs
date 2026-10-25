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
			return null;
		}
	}
}
