using Codebase.Core;
using Codebase.Events;
using Codebase.Events.Components;
using Codebase.Events.Models;
using Codebase.UI.Events;
using Scellecs.Morpeh;

namespace Codebase.Enemy.Systems
{
	public class EventBossProgressBarSystem : SystemBase
	{
		private const string PrefabAddress = "Assets/Prefabs/UI/Event/BossHuntProgress.prefab";

		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private Stash<EventBossGateComponent> gateStash;

		[Inject]
		private Stash<EventBossGateOpenedTag> openedStash;

		private Filter gateFilter;

		private BossHuntProgressView prefab;

		private bool prefabLoaded;

		private BossHuntProgressView currentProgressView;

		private EventBossGate currentGate;

		private string lastActiveEventKey;

		public override void OnAwake()
		{
		}

		public override void Dispose()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private void HandleEventChange(EventModel currentEvent)
		{
		}

		private void ProcessGateProgressBar(EventModel currentEvent)
		{
		}

		private (Entity, EventBossGate, bool)? FindGateForEvent(string eventKey)
		{
			return null;
		}

		private void CreateProgressBar(EventBossGate gate)
		{
		}

		private void RemoveProgressBar()
		{
		}

		private void LoadPrefab()
		{
		}

		public BossHuntProgressView GetProgressView()
		{
			return this.currentProgressView;
		}
	}
}
