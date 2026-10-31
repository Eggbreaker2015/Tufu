using System.Collections.Generic;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Player.Models;
using Scellecs.Morpeh;

namespace Codebase.Location.Systems
{
	public class IslandMasterySystem : SystemBase
	{
		private const float CheckInterval = 1f;
		[Inject]
		private IslandMasteryModel islandMasteryModel;
		[Inject]
		private EnemiesModel enemiesModel;
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private IAnalyticsWrapper analytics;
		private bool metaStatSynchronized;
		private bool awaitingBootstrapCompletion;
		private float timeSinceLastCheck;
		private int lastCheckedLocationNumber;
		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
			return;
		}

		private void RunLegacyMigration(IReadOnlyList<int> trackedLocationNumbers)
		{
			for (int i = 0; i < trackedLocationNumbers.Count; i++)
			{
				int locationNumber = trackedLocationNumbers[i];
				if (islandMasteryModel.Resolver.IsFullyCleared(locationNumber))
					islandMasteryModel.GetOrCreateLocation(locationNumber).ClearProcessed.Value = true;
			}

			islandMasteryModel.MarkLegacyMigrationComplete();
			islandMasteryModel.BumpStateVersion();
		}

		private void ProcessLocation(int locationNumber)
		{
			var data = islandMasteryModel.GetOrCreateLocation(locationNumber);
			bool cleared = islandMasteryModel.Resolver.IsFullyCleared(locationNumber);
			bool processed = data.ClearProcessed.Value;
			if (!processed && cleared)
			{
				int previousLevel = islandMasteryModel.GetMedalLevel(locationNumber);
				if (previousLevel < IslandMasteryModel.MaxMedalLevel)
				{
					int currentLevel = previousLevel + 1;
					islandMasteryModel.SetMedalLevel(locationNumber, currentLevel);
					islandMasteryModel.QueuePresentation(new IslandMasteryPresentationResult { LocationNumber = locationNumber, PreviousLevel = previousLevel, CurrentLevel = currentLevel });
					analytics.Send(new global::Codebase.Analytics.Events.IslandMedalUpgradeEvent(locationNumber, previousLevel, currentLevel));
				}
				else
					islandMasteryModel.NotifyMaxLevelClear(locationNumber);
				data.ClearProcessed.Value = true;
			}
			else
			{
				if (!processed || cleared)
					return;
				data.ClearProcessed.Value = false;
			}

			islandMasteryModel.BumpStateVersion();
		}
	}
}
