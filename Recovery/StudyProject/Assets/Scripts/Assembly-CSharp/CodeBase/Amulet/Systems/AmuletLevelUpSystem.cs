using Codebase.Analytics.Events;
using Codebase.Analytics;
using Codebase.Core;
using Scellecs.Morpeh;

namespace Codebase.Amulet.Systems
{
	public class AmuletLevelUpSystem : SystemBase
	{
		[Inject]
		private AmuletModel amuletModel;

		[Inject]
		private IAnalyticsWrapper analytics;

		private int lastTrackedLevel;

		public override void OnAwake()
		{
// Native ELF VA 0x15bbe54.
            lastTrackedLevel = amuletModel.Level;
		}

		public override void OnUpdate(float deltaTime)
		{
// Native ELF VA 0x15bbe78 emits one original level event per update, including skipped levels.
            if (amuletModel.Level <= lastTrackedLevel) return;
            lastTrackedLevel++;
            analytics.Send(new AmuletLevelUpEvent(lastTrackedLevel));
		}
	}
}
