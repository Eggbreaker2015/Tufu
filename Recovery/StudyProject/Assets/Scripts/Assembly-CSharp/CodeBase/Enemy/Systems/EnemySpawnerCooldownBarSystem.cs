using System;
using Codebase.Core;
using Codebase.Enemy.Components;
using Codebase.UI;
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
		}

		public override void Dispose()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private SpawnerCooldownSystemStateComponent Create(Entity entity)
		{
			return default;
		}

		private void Remove(ref SpawnerCooldownSystemStateComponent state)
		{
		}
	}
}
