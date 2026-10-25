using Codebase.Buildings;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Events.Components;
using Codebase.Events.Models;
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
		private Stash<EventBossGateComponent> gateStash;

		[Inject]
		private Stash<EventBossGateOpenedTag> openedStash;

		[Inject]
		private Stash<EventBossChestComponent> chestStash;

		private Filter filter;

		private Filter chestFilter;

		private string lastActiveEventKey;

		private EventState lastEventState;

		private int lastRosterProgress;

		private bool isMiniBossFlowRunning;

		private bool isInitialSync;

		[PublicAPI]
		public bool IsMiniBossFlowRunning => this.isMiniBossFlowRunning;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private void HandleRosterProgressChange(EventModel currentEvent)
		{
		}

		private void HandleRosterGates()
		{
		}

		private void HandleEventChange(EventModel currentEvent)
		{
		}

		private void OnEventActivated(EventModel eventModel)
		{
		}

		private void TryResumeRosterMiniBoss(EventModel eventModel)
		{
		}

		private EventDifficulty GetChestDifficultyForEvent(EventModel eventModel)
		{
			return EventDifficulty.Easy;
		}

		private EventDifficulty GetRosterChestDifficulty(int stageIndex)
		{
			return EventDifficulty.Easy;
		}

		private int GetRosterStageForChest(EventModel eventModel)
		{
			return 0;
		}

		[PublicAPI]
		public EventDifficulty GetChestDifficultyForStage(EventModel eventModel, int stageIndex)
		{
			return EventDifficulty.Easy;
		}

		private void CloseAllGates()
		{
		}

		private void TeleportPlayerIfNeeded()
		{
		}

		private void HandleGateOpening(EventModel currentEvent)
		{
		}

		private bool ShouldOpenGate(EventModel currentEvent)
		{
			return false;
		}

		public EventBossGate GetGateForEvent(string eventKey)
		{
			return null;
		}

		private void ProcessChests()
		{
		}

		private bool IsChestActiveForEvent(EventModel eventModel, EventDifficulty chestDifficulty)
		{
			return false;
		}

		[PublicAPI]
		public bool TryGetFirstAvailableChest(EventDifficulty difficulty, out EventBossChestComponent chest)
		{
			chest = default;
			return false;
		}

		private void TryPlayCompletedChestAnimation(EventDifficulty difficulty)
		{
		}

		[PublicAPI]
		public void SetMiniBossFlowRunning(bool running)
		{
		}

		[PublicAPI]
		public void PlayChestAnimationForRoster(EventModel eventModel)
		{
		}
	}
}
