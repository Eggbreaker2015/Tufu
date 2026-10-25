using Codebase.VIP.Features;
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
            // Native 0x15bf3c4.
            featureUnlockedEvent = World.GetEvent<VipFeatureUnlockedEvent>();
            lastTrackedLevel = vipModel.Level;
            filter = Filter.With<VipLocationComponent>().Without<VipLocationUnlockedComponent>().Build();
            UnlockMissedFeatures();
		}

		private void UnlockMissedFeatures()
		{
            // Native 0x15bf520 restores persistent feature unlock effects on boot.
            int level = vipModel.Level;
            foreach (IVipFeature feature in vipModel.GetAllUnlockedFeatures())
                if (feature.UnlockLevel <= level) feature.OnUnlocked(feature.UnlockLevel);
		}

		public override void OnUpdate(float deltaTime)
		{
            // Native 0x15bf840 advances one VIP level per frame, then removes eligible island doors.
            int level = vipModel.Level;
            if (level > lastTrackedLevel)
            {
                OnVipLevelUp(lastTrackedLevel, lastTrackedLevel + 1);
                lastTrackedLevel++;
            }
            VipIslandFeature feature = vipModel.GetFeature<VipIslandFeature>(VipFeatureKeys.VipIsland);
            if (feature == null) return;
            int island = feature.GetIslandNumber(vipModel.Level);
            foreach (Entity entity in filter)
            {
                ref VipLocationComponent location = ref vipLocationStash.Get(entity);
                if (location.LocationNumber > island) continue;
                location.Door.gameObject.SetActive(false);
                vipLocationUnlockedStash.Set(entity);
            }
		}

		private void OnVipLevelUp(int fromLevel, int toLevel)
		{
            // Native 0x15bfa4c.
            foreach (IVipFeature feature in vipModel.GetAllUnlockedFeatures())
            {
                if (feature.UnlockLevel == toLevel)
                {
                    feature.OnUnlocked(toLevel);
                    FireFeatureUnlockedEvent(feature, toLevel);
                }
                if (feature.IsUnlocked(toLevel)) feature.OnLevelUp(fromLevel, toLevel);
            }
            analytics.Send(new VipLevelUpEvent(toLevel, vipModel.Experience));
		}

		private void FireFeatureUnlockedEvent(IVipFeature feature, int level)
		{
            // Native 0x15bfee0.
            featureUnlockedEvent.NextFrame(new VipFeatureUnlockedEvent { Feature = feature, Level = level });
		}
	}
}
