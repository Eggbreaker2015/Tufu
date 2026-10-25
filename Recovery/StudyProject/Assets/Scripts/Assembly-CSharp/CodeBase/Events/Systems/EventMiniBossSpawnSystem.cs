using System;
using Codebase.Core;
using Codebase.Events.Models;
using Codebase.Unit.Components;
using Scellecs.Morpeh;

namespace Codebase.Events.Systems
{
	public class EventMiniBossSpawnSystem : SystemBase
	{
		[Serializable]
		private struct EventMiniBossSpawnState : ISystemStateComponent, IComponent
		{
			public Entity Entity;
		}

		[Inject]
		private Stash<UnitComponent> unitStash;

		[Inject]
		private EventsRepository eventsRepository;

		private SystemStateProcessor<EventMiniBossSpawnState> processor;

		private bool isFirstRosterMiniBossSpawned;

		private string lastRosterEventKey;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private void ResetRosterFlagIfEventChanged()
		{
		}

		private EventMiniBossSpawnState OnMiniBossSpawned(Entity entity)
		{
			return default;
		}

		private bool ShouldPlayCameraFlight()
		{
			return false;
		}

		private void OnMiniBossRemoved(ref EventMiniBossSpawnState state)
		{
		}

		public override void Dispose()
		{
		}
	}
}
