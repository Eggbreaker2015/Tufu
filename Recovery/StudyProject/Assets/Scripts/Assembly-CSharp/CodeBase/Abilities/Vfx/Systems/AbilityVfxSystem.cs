using Codebase.Utilities.ObjectPool;
using UnityEngine;
using Codebase.Abilities.Vfx.Components;
using Codebase.Core;
using Scellecs.Morpeh;

namespace Codebase.Abilities.Vfx.Systems
{
	public class AbilityVfxSystem : SystemBase
	{
		private Filter filter;

		[Inject]
		private Stash<AbilityVfxComponent> vfxStash;

		public override void OnAwake()
		{
			// Native ELF VA 0x15e57d4.
			filter = Filter.With<AbilityVfxComponent>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			// Native ELF VA 0x15e5888; vector register values verified against bounded ARM64.
			foreach (Entity entity in filter)
			{
			    ref AbilityVfxComponent vfx = ref vfxStash.Get(entity);
			    vfx.Elapsed += deltaTime;
			    if (vfx.Target != null)
			        vfx.ParticleSystem.transform.position = vfx.Target.position + Vector3.up * vfx.HeightOffset + vfx.PositionOffset;
			    if (vfx.Elapsed >= vfx.Duration)
			    {
			        ObjectPoolUtility<ParticleSystem>.Despawn(vfx.ParticleSystem);
			        World.RemoveEntity(entity);
			    }
			}
		}
	}
}
