using System.Linq;
using Codebase.Events.Flows;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Codebase.Buildings;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Events.Components;
using Codebase.Events.Models;
using Codebase.Pet;
using Codebase.Player.Models;
using JetBrains.Annotations;
using Scellecs.Morpeh;

namespace Codebase.Events.Systems
{
	public class EventBossGateSystem : SystemBase
	{
		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private EnemiesModel enemiesModel;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private FirecampSystem firecampSystem;

		[Inject]
		private PetsModel petsModel;

		[Inject]
		private Stash<EventBossGateComponent> gateStash;

		[Inject]
		private Stash<EventBossGateOpenedTag> openedStash;

		[Inject]
		private Stash<EventBossChestComponent> chestStash;

		private Filter filter;

		private Filter chestFilter;

		private string lastActiveEventKey;

		private EventState lastEventState;

		private int lastRosterProgress = -1;

		private bool isMiniBossFlowRunning;

		private bool isInitialSync = true;

		[PublicAPI]
		public bool IsMiniBossFlowRunning => this.isMiniBossFlowRunning;

		public override void OnAwake()
		{
			filter = Filter.With<EventBossGateComponent>().Build();
			chestFilter = Filter.With<EventBossChestComponent>().Build();
			ProcessChests();
		}

		public override void OnUpdate(float deltaTime)
		{
			var currentEvent = eventsRepository.CurrentEvent ?? eventsRepository.CompletedEvent;
			HandleRosterGates();
			if (currentEvent == null)
			{
				lastActiveEventKey = null;
				lastEventState = EventState.NotStarted;
				ProcessChests();
				return;
			}
			if (isMiniBossFlowRunning) return;
			HandleEventChange(currentEvent);
			HandleGateOpening(currentEvent);
			HandleRosterProgressChange(currentEvent);
		}

		private void HandleRosterProgressChange(EventModel currentEvent)
		{
			if (currentEvent.Key == EventKeys.CrystalRoster && lastRosterProgress != currentEvent.Progress)
				lastRosterProgress = currentEvent.Progress;
		}

		private void HandleRosterGates()
		{
			var model = eventsRepository.ScheduledEvent ?? eventsRepository.CurrentEvent;
			if (model == null || model.Key != EventKeys.CrystalRoster) return;
			foreach (var entity in filter)
			{
				var gate = gateStash.Get(entity).Gate;
				if (gate == null || openedStash.Has(entity)) continue;
				gate.Open();
				openedStash.Set(entity);
			}
		}

		private void HandleEventChange(EventModel currentEvent)
		{
			if (lastActiveEventKey == currentEvent.Key && lastEventState == currentEvent.State) return;
			lastActiveEventKey = currentEvent.Key;
			lastEventState = currentEvent.State;
			OnEventActivated(currentEvent);
		}

		private void OnEventActivated(EventModel eventModel)
		{
			ProcessChests();
			if (isInitialSync && eventModel.State == EventState.Completed)
				TryPlayCompletedChestAnimation(GetChestDifficultyForEvent(eventModel));
			if (lastEventState == EventState.Active)
			{
				if (eventModel.Key == EventKeys.CrystalRoster) TryResumeRosterMiniBoss(eventModel);
				else
				{
					CloseAllGates();
					TeleportPlayerIfNeeded();
				}
			}
			isInitialSync = false;
		}

		private void TryResumeRosterMiniBoss(EventModel eventModel)
		{
			if (isMiniBossFlowRunning || eventModel.Progress >= eventModel.ProgressMax) return;
			var spawner = enemiesModel.EventSpawners.FirstOrDefault(s => s.Config.EventDef != null &&
				s.Config.EventDef.Key == eventModel.Key && s.Config.EventDef.UnitType == EventUnitType.MiniBoss);
			if (spawner != null && spawner.CurrentCount < 1)
				App.InitiateFlow(new RosterSpawnNextMiniBossFlow { EventKey = eventModel.Key }).Forget();
		}

		private EventDifficulty GetChestDifficultyForEvent(EventModel eventModel)
		{
			return eventModel.Key == EventKeys.CrystalRoster
				? GetRosterChestDifficulty(GetRosterStageForChest(eventModel)) : eventModel.Difficulty;
		}

		private EventDifficulty GetRosterChestDifficulty(int stageIndex)
		{
			if (stageIndex < 7) return EventDifficulty.Easy;
			return stageIndex > 14 ? EventDifficulty.Hard : EventDifficulty.Medium;
		}

		private int GetRosterStageForChest(EventModel eventModel)
		{
			return eventModel.Progress >= eventModel.ProgressMax
				? Mathf.Max(0, eventModel.Progress - 1) : eventModel.Progress;
		}

		[PublicAPI]
		public EventDifficulty GetChestDifficultyForStage(EventModel eventModel, int stageIndex)
		{
			if (eventModel == null) return EventDifficulty.Easy;
			return eventModel.Key == EventKeys.CrystalRoster ? GetRosterChestDifficulty(stageIndex) : eventModel.Difficulty;
		}

		private void CloseAllGates()
		{
			foreach (var entity in filter)
			{
				if (openedStash.Has(entity)) openedStash.Remove(entity);
				var gate = gateStash.Get(entity).Gate;
				if (gate != null) gate.Reset();
			}
		}

		private void TeleportPlayerIfNeeded()
		{
			foreach (var entity in filter)
			{
				var gate = gateStash.Get(entity).Gate;
				if (gate == null) continue;
				if (Vector3.Distance(playerModel.Unit.transform.position, gate.transform.position) < 50f)
				{
					playerModel.Unit.Agent.Warp(firecampSystem.transform.position);
					// Pet mode: keep pets from being shut behind the gate.
					petsModel.WarpAll(firecampSystem.transform.position);
				}
			}
		}

		private void HandleGateOpening(EventModel currentEvent)
		{
			foreach (var entity in filter)
			{
				var gate = gateStash.Get(entity).Gate;
				if (gate == null || !ShouldOpenGate(currentEvent)) continue;
				gate.Open();
				openedStash.Set(entity);
			}
		}

		private bool ShouldOpenGate(EventModel currentEvent)
		{
			return currentEvent.Key == EventKeys.CrystalRoster || currentEvent.IsCompleted ||
				(currentEvent.IsActive && currentEvent.Progress >= currentEvent.ProgressMax - 1);
		}

		public EventBossGate GetGateForEvent(string eventKey)
		{
			// Original native method returns the first scene gate; its eventKey parameter is unused.
			foreach (var entity in filter)
			{
				var gate = gateStash.Get(entity).Gate;
				if (gate != null) return gate;
			}
			return null;
		}

		private void ProcessChests()
		{
			foreach (var entity in chestFilter)
			{
				var chest = chestStash.Get(entity);
				if (chest.Root == null) continue;
				var model = eventsRepository.CurrentEvent ?? eventsRepository.CompletedEvent;
				chest.Root.SetActive(model != null && (model.IsActive || model.IsCompleted) &&
					IsChestActiveForEvent(model, chest.Difficulty));
			}
		}

		private bool IsChestActiveForEvent(EventModel eventModel, EventDifficulty chestDifficulty)
		{
			return GetChestDifficultyForEvent(eventModel) == chestDifficulty;
		}

		[PublicAPI]
		public bool TryGetFirstAvailableChest(EventDifficulty difficulty, out EventBossChestComponent chest)
		{
			chest = default;
			foreach (var entity in chestFilter)
			{
				var candidate = chestStash.Get(entity);
				if (candidate.Root == null || !candidate.Root.activeInHierarchy || candidate.Difficulty != difficulty) continue;
				chest = candidate;
				return true;
			}
			// Preserve the native scene fallback, including Morpeh's empty-filter exception.
			chest = chestStash.Get(chestFilter.First());
			return true;
		}

		private void TryPlayCompletedChestAnimation(EventDifficulty difficulty)
		{
			TryGetFirstAvailableChest(difficulty, out var chest);
			if (chest.Animation != null) chest.Animation.Play();
		}

		[PublicAPI]
		public void SetMiniBossFlowRunning(bool running)
		{
			isMiniBossFlowRunning = running;
			if (!running) ProcessChests();
		}

		[PublicAPI]
		public void PlayChestAnimationForRoster(EventModel eventModel)
		{
			TryPlayCompletedChestAnimation(GetChestDifficultyForEvent(eventModel));
		}
	}
}
