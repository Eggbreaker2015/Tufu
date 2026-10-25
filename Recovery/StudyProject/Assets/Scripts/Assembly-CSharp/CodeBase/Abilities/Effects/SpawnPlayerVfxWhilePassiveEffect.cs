using System;
using System.Collections.Generic;
using Codebase.Abilities.Core;
using Codebase.Core;
using UnityEngine;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class SpawnPlayerVfxWhilePassiveEffect : AbilityEffectBase
	{
		public string AddressableKey;

		public float HeightOffset;

		public Vector3 Rotation = new Vector3(-90f, 0f, 0f); // Native ctor literal ELF 0x8df878.

		private static readonly Dictionary<AbilityState, ParticleSystem> ActiveVfx = new Dictionary<AbilityState, ParticleSystem>();

		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
            // Original ELF VA 0x15f1f58; persistent instances are keyed by original AbilityState, not effect instance.
            if (string.IsNullOrEmpty(AddressableKey) || context.PlayerUnit == null) return;
            if (ActiveVfx.TryGetValue(context.State, out var existing) && existing != null) return;
            var data = new Codebase.Abilities.Vfx.AbilityVfxData(AddressableKey, 0f, HeightOffset, Rotation.x, Rotation.y, Rotation.z);
            var particle = GameContext.Get<Codebase.Abilities.Vfx.AbilityVfxService>().SpawnPersistent(data, context.PlayerUnit, true);
            if (particle != null) ActiveVfx[context.State] = particle;
        }

		public override void OnPassiveInactive(AbilityContext context)
		{
            // Original ELF VA 0x15f2184 is a bounded tail-call wrapper.
            DespawnFor(context.State);
        }

		private void DespawnFor(AbilityState state)
		{
            // Original ELF VA 0x15f2198; return the real persistent instance to its original pool, then remove its state key.
            if (state == null || !ActiveVfx.TryGetValue(state, out var particle)) return;
            GameContext.Get<Codebase.Abilities.Vfx.AbilityVfxService>().DespawnPersistent(particle);
            ActiveVfx.Remove(state);
        }

		[StaticReset]
		private static void ResetStaticState()
		{
            // Original ELF VA 0x15f22b8 destroys remaining GameObjects directly, rather than returning them to the pool.
            foreach (var entry in ActiveVfx) if (entry.Value != null) UnityEngine.Object.Destroy(entry.Value.gameObject);
            ActiveVfx.Clear();
        }
	}
}
