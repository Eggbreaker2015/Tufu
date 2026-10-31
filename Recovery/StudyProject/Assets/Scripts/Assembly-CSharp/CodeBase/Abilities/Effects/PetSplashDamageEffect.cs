using System;
using System.Collections.Generic;
using Codebase.Abilities.Core;
using Codebase.Abilities.Vfx;
using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Pet;
using Codebase.Unit.Events;
using Codebase.Unit.Systems;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Abilities.Effects
{
	// Pet mode: deals Value x hit damage to the other living enemies around the hit target.
	// Applied directly like SpawnLightningEffect, so it raises no OnHit of its own.
	[Serializable]
	public class PetSplashDamageEffect : AbilityEffectBase
	{
		public float Radius = 2.5f;

		private static readonly List<EnemyUnit> Victims = new List<EnemyUnit>();

		[StaticReset]
		private static void ResetStaticState()
		{
			Victims.Clear();
		}

		public override void Execute(AbilityContext context)
		{
			double damage = context.Damage * context.State.GetValue();
			if (damage <= 0.0)
				return;
			EnemyUnit center = AbilityTargetResolver.Resolve(context);
			if (center == null)
				return;
			Vector3 origin = center.transform.position;
			float radiusSqr = Radius * Radius;
			Victims.Clear();
			foreach (EnemyUnit enemy in GameContext.Get<EnemiesModel>().Enemies)
			{
				if (enemy == null || enemy == center || enemy.IsDead || enemy.IsFar || enemy.IsStatic)
					continue;
				Vector3 position = enemy.transform.position;
				float x = position.x - origin.x;
				float z = position.z - origin.z;
				if (x * x + z * z <= radiusSqr)
					Victims.Add(enemy);
			}
			if (Victims.Count == 0)
				return;
			World world = context.World;
			Event<UnitDiedEvent> died = world.GetEvent<UnitDiedEvent>();
			Stash<VampireSpiritTag> spirits = world.GetStash<VampireSpiritTag>();
			AbilityVfxService vfx = GameContext.Get<AbilityVfxService>();
			foreach (EnemyUnit enemy in Victims)
			{
				if (enemy.IsDead || spirits.Has(enemy.EntityProvider.Entity))
					continue;
				bool wasDead = enemy.IsDead;
				enemy.CurrentHealth -= damage;
				enemy.LastDamageGet = Time.time;
				UnitDamageSystem.ProcessEnemyDeath(enemy, wasDead, died);
				vfx.Spawn(AbilityVfxConstants.SwordHit, enemy);
			}
			Victims.Clear();
		}
	}
}
