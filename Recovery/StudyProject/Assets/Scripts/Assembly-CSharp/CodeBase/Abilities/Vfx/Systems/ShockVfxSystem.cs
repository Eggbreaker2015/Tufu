using System.Collections.Generic;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Abilities.Vfx.Systems
{
	public class ShockVfxSystem : SystemBase
	{
		private struct ShockVfxInstance
		{
			public ParticleSystem ParticleSystem;

			public Transform RendererTransform;
		}

		private Filter filter;

		[Inject]
		private Stash<ShockComponent> shockStash;

		[Inject]
		private Stash<UnitComponent> unitStash;

		private readonly Dictionary<Entity, List<ShockVfxInstance>> activeVfx;

		private readonly List<Entity> toRemove;

		public override void OnAwake()
		{
		}

		public override void Dispose()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private void SpawnVfxForEnemy(Entity entity, EnemyUnit enemy)
		{
		}

		private void ReleaseVfx(ParticleSystem ps)
		{
		}
	}
}
