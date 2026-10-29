using System;
using Codebase.Core;

namespace Codebase.Skills.Effects
{
	// Enemy casters only; BossMinionService pools these without rewards or OnKill/OnDevour triggers.
	// Despawned with the caster; summons never get summon skills themselves; boss tower arena casters are skipped.
	[Serializable]
	public class SummonEffect : ISkillEffect
	{
		// SkillLibrary.Summons key.
		public string SummonKey;

		public int Count = 1;

		// Spawn points are NavMesh-sampled within this radius of the caster.
		public float Radius = 2.5f;

		// Living summons of this key per caster; 0 = unlimited.
		public int MaxAlive;

		// Seconds; 0 = until killed or the caster dies.
		public float Lifetime;

		// Multiplier on the caster's base Health.
		public float HealthScale = 1f;

		// Multiplier on the caster's base Damage.
		public float DamageScale = 1f;

		// Optional SkillLibrary.Vfx key played on appearance and despawn.
		public string VfxKey;

		public void Apply(in SkillCastContext context)
		{
			if (!GameContext.Has<SkillService>())
				return;
			GameContext.Get<SkillService>().Summon(context, this);
		}
	}
}
