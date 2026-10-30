using Codebase.Utilities.ObjectPool;
using UnityEngine;
using Codebase.Abilities.Vfx.Components;
using Codebase.Core;
using Codebase.Vfx;
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
			    {
			        // Deviation from native: follows the character's current size.
			        float factor = vfx.BaseScale == Vector3.zero ? 1f : CharacterVfxScale.Factor(vfx.Target);
			        vfx.ParticleSystem.transform.position = vfx.Target.position + Vector3.up * (vfx.HeightOffset * factor) + vfx.PositionOffset;
			        if (vfx.BaseScale != Vector3.zero) vfx.ParticleSystem.transform.localScale = vfx.BaseScale * factor;
			    }
			    if (vfx.Elapsed >= vfx.Duration)
			    {
			        ObjectPoolUtility<ParticleSystem>.Despawn(vfx.ParticleSystem);
			        World.RemoveEntity(entity);
			    }
			}
		}
	}
}
