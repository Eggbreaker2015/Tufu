using Codebase.Analytics;
using Codebase.Core;
using Codebase.VIP.Components;
using Codebase.VIP.Events;
using Scellecs.Morpeh;

namespace Codebase.VIP.Systems
{
	public class VipLevelUpSystem : SystemBase
	{
		[Inject]
		private VipModel vipModel;

		[Inject]
		private IAnalyticsWrapper analytics;

		private Event<VipFeatureUnlockedEvent> featureUnlockedEvent;

		private int lastTrackedLevel;

		[Inject]
		private Stash<VipLocationComponent> vipLocationStash;

		[Inject]
		private Stash<VipLocationUnlockedComponent> vipLocationUnlockedStash;

		private Filter filter;

		public override void OnAwake()
		{
		}

		private void UnlockMissedFeatures()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private void OnVipLevelUp(int fromLevel, int toLevel)
		{
		}

		private void FireFeatureUnlockedEvent(IVipFeature feature, int level)
		{
		}
	}
}
