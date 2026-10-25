using System;
using Codebase.Core;
using Codebase.Enemy.Components;
using Codebase.UI;
using Codebase.Unit.Components;
using Scellecs.Morpeh;

namespace Codebase.Enemy.Systems
{
	public class EnemySpawnerCooldownBarSystem : SystemBase
	{
		[Serializable]
		private struct SpawnerCooldownSystemStateComponent : ISystemStateComponent, IComponent
		{
			public SpawnerView SpawnerView;
		}

		private SystemStateProcessor<SpawnerCooldownSystemStateComponent> processor;

		[Inject]
		private Stash<EnemySpawnerComponent> enemySpawnerStash;

		public override void OnAwake()
		{
			// Original ARM64 ELF VA 0x1511e98, body length 0x22c.
			processor = Filter.With<EnemySpawnerComponent>()
				.With<EnemySpawnerOnCooldownComponent>()
				.Without<FarOnScreenMarker>()
				.Without<EventSpawnerInactiveTag>()
				.ToSystemStateProcessor<SpawnerCooldownSystemStateComponent>(Create, Remove);
		}

		public override void Dispose()
		{
			// Original ARM64 ELF VA 0x15120c4, body length 0x48.
			processor.Dispose();
		}

		public override void OnUpdate(float deltaTime)
		{
			// Original ARM64 ELF VA 0x151210c, body length 0x48.
			processor.Process();
		}

		private SpawnerCooldownSystemStateComponent Create(Entity entity)
		{
			// Original ARM64 ELF VA 0x1512154, body length 0x88.
			return new SpawnerCooldownSystemStateComponent
			{
				SpawnerView = enemySpawnerStash.Get(entity).Spawner.SetupView()
			};
		}

		private void Remove(ref SpawnerCooldownSystemStateComponent state)
		{
			// Original ARM64 ELF VA 0x15121dc, body length 0x14.
			// CBZ tests the managed reference; it does not call Unity's fake-null operator.
			state.SpawnerView?.Dispose();
		}
	}
}
