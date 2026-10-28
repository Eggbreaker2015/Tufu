using Codebase.UI.World;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
			gateFilter = Filter.With<EventBossGateComponent>().Build();
			LoadPrefab();
		}

		public override void Dispose()
		{
			// Original4-byte method0x1513654 is a tail branch to RemoveProgressBar, not RET.
			RemoveProgressBar();
		}

		public override void OnUpdate(float deltaTime)
		{
			if (!prefabLoaded) return;
			var model = eventsRepository.CurrentEvent;
			if (model == null) { RemoveProgressBar(); return; }
			HandleEventChange(model);
			ProcessGateProgressBar(model);
		}

		private void HandleEventChange(EventModel currentEvent)
		{
			if (lastActiveEventKey == currentEvent.Key) return;
			lastActiveEventKey = currentEvent.Key;
			if (currentProgressView != null) currentProgressView.Show();
		}

		private void ProcessGateProgressBar(EventModel currentEvent)
		{
			var found = FindGateForEvent(currentEvent.Key);
			if (!found.HasValue) { RemoveProgressBar(); return; }
			var gate = found.Value;
			if (!gate.Item3 && (currentGate != gate.Item2 || currentProgressView == null))
				CreateProgressBar(gate.Item2);
		}

		private (Entity, EventBossGate, bool)? FindGateForEvent(string eventKey)
		{
			foreach (var entity in gateFilter)
			{
				var gate = gateStash.Get(entity).Gate;
				if (gate != null && gate.EventKey == eventKey) return (entity, gate, gate.IsOpened);
			}
			return null;
		}

		private void CreateProgressBar(EventBossGate gate)
		{
			RemoveProgressBar();
			var canvas = GameContext.Get<DynamicWorldCanvas>();
			currentProgressView = canvas.CreateNew(prefab);
			Injector.InjectInto(currentProgressView);
			currentProgressView.Initialize();
			currentProgressView.Show();
			canvas.AddTarget(gate.ProgressBarAnchor, currentProgressView.RectTransform);
			currentGate = gate;
		}

		private void RemoveProgressBar()
		{
			if (currentProgressView == null) return;
			var canvas = GameContext.Get<DynamicWorldCanvas>();
			if (currentGate != null) canvas.RemoveTarget(currentGate.ProgressBarAnchor);
			currentProgressView.Cleanup();
			canvas.Despawn(currentProgressView);
			currentProgressView = null;
			currentGate = null;
		}

		private void LoadPrefab()
		{
			global::Codebase.Core.RecoveredAddressables.LoadAsync(PrefabAddress, result =>
			{
				if (result == null) return;
				prefab = result.GetComponent<BossHuntProgressView>();
				if (prefab != null) prefabLoaded = true;
			});
		}

		public BossHuntProgressView GetProgressView()
		{
			return this.currentProgressView;
		}
	}
}
