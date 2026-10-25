using System.Collections.Generic;
using Codebase.Core;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Abilities.Vfx.Systems
{
	public class StunVfxSystem : SystemBase
	{
		private Filter filter;

		[Inject]
		private Stash<BuffStunComponent> stunStash;

		[Inject]
		private Stash<UnitComponent> unitStash;

		private readonly Dictionary<Entity, ParticleSystem> activeVfx;

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
	}
}
