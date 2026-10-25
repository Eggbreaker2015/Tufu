using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Enemy.Systems;
using Codebase.Player.Models;
using Codebase.Services.Cheats;
using Codebase.Unit.Components;
using Codebase.Unit.Events;
using Codebase.Vfx;
using Scellecs.Morpeh;
using System;
using UnityEngine;
using Codebase.BossTower.Helpers;
using Codebase.Enemy.Components;
using Codebase.Enemy;
using Codebase.Unit;
using Codebase.Player.Commands;

namespace Codebase.BossTower.Systems
{
	public class LavaCrabExplosionSystem : SystemBase
	{
		private Filter lavaCrabFilter;
		private Event<WeaponHitEvent> hitEvent;
		[Inject]
		private Stash<LavaCrabBossComponent> lavaCrabStash;
		[Inject]
		private Stash<LavaCrabHitBlinkComponent> hitBlinkStash;
		[Inject]
		private Stash<UnitComponent> unitStash;
		[Inject]
		private Stash<ShockComponent> shockStash;
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private WorldVfxService worldVfxService;
		[Inject]
		private CheatsModel cheatsModel;
		[Inject]
		private EnemyCastSystem enemyCastSystem;
		private const float MAX_FIRE_SOUND_VOLUME = 0.3f;
		public override void OnAwake()
		{
			lavaCrabFilter = Filter.With<LavaCrabBossComponent>().Build();
			hitEvent = World.GetEvent<WeaponHitEvent>();
		}

		public override void OnUpdate(float deltaTime)
		{
			if (!playerModel.InBossTowerArena)
				return;
			ProcessHits();
			ProcessShockConsumed();
		}

		private void ProcessHits()
		{
			foreach (var hit in hitEvent.IterateHitsOn(lavaCrabStash))
			{
				Entity entity = hit.Item2;
				if (enemyCastSystem.IsCasting(entity))
					continue;
				ref LavaCrabBossComponent crab = ref lavaCrabStash.Get(entity);
				crab.CurrentHits++;
				UpdateHitBlink(entity, ref crab);
				if (crab.CurrentHits < crab.HitsToExplode)
					continue;
				StopHitBlink(entity);
				ScaleExplosionZone(entity, GetExplosionRadius(crab.ExplosionCount));
				enemyCastSystem.StartCast(entity, new EnemyCastingState { Duration = crab.ExplosionDelay, AnimationBool = "isCasting", SpecialCastKey = null, OnComplete = () => Explode(entity) });
			}
		}

		private static int GetExplosionRadius(int explosions)
		{
			return unchecked(explosions * 3 + 8);
		}

		private void ProcessShockConsumed()
		{
			foreach (Entity entity in lavaCrabFilter)
			{
				ref LavaCrabBossComponent crab = ref lavaCrabStash.Get(entity);
				ref ShockComponent shock = ref shockStash.Get(entity, out bool exists);
				int consumed = exists ? shock.ConsumedShockCount : 0;
				if (exists)
				{
					int delta = unchecked(consumed - crab.LastKnownShockConsumedCount);
					if (delta > 0)
					{
						crab.CurrentHits = Math.Max(0, unchecked(crab.CurrentHits + ~delta));
						UpdateHitBlink(entity, ref crab);
						AddBuffRegenToPlayer();
					}
				}

				crab.LastKnownShockConsumedCount = consumed;
			}
		}

		private void AddBuffRegenToPlayer()
		{
			App.ExecuteCommand(new AddTemporaryModifierCommand { Unit = playerModel.Unit, Name = "LavaCrabShockRegen", StatType = StatType.HealthRegen, ModifierType = ModifierType.PreAdditivePostMultiplicative, Source = ModifierSource.AbilityTemporary, Value = 5f, Duration = 5f, RemoveDuplicates = true });
		}

		private void Explode(Entity entity)
		{
			ref LavaCrabBossComponent crab = ref lavaCrabStash.Get(entity);
			var core = unitStash.Get(entity).Core;
			core.GetStatValue(StatType.Damage, true);
			EnemyUnit enemy = (EnemyUnit)core;
			int radius = GetExplosionRadius(crab.ExplosionCount);
			Vector3 playerPosition = playerModel.Unit.transform.position;
			Vector3 bossPosition = enemy.transform.position;
			float dx = playerPosition.x - bossPosition.x, dz = playerPosition.z - bossPosition.z;
			if (Mathf.Sqrt(dx * dx + dz * dz) <= radius)
				World.GetEvent<WeaponHitEvent>().NextFrame(new WeaponHitEvent { Source = enemy, Target = playerModel.Unit, DamageType = enemy.DamageType, Undodgeable = true, HasSpecialMultiplier = true, SpecialMultiplier = 10.0 });
			if (crab.HealOnExplosion)
			{
				double maximum = enemy.GetStatValue(StatType.Health, true);
				enemy.CurrentHealth = Math.Min(enemy.CurrentHealth + maximum * crab.HealPercent, maximum);
			}

			ParticleSystem particle = worldVfxService.Spawn(crab.ExplosionVfxPrefab, enemy.transform.position + Vector3.up, Quaternion.identity, crab.ExplosionVfxLifetime);
			crab.CurrentHits = 0;
			crab.ExplosionCount++;
			particle.transform.localScale = Vector3.one * unchecked(radius * 2) * 0.25f;
			ResetHitBlink(entity);
		}

		public void OnRaidEnded()
		{
			foreach (Entity entity in lavaCrabFilter)
			{
				ResetHitBlink(entity);
				lavaCrabStash.Remove(entity);
			}
		}

		private void ScaleExplosionZone(Entity entity, float radius)
		{
			if (!hitBlinkStash.Has(entity))
				return;
			var outer = hitBlinkStash.Get(entity).explosionPrepareParticle;
			var inner = hitBlinkStash.Get(entity).explosionPrepareParticleInner;
			if (outer == null)
				return;
			float size = (radius + radius) / 10f;
			outer.transform.localScale = Vector3.one * size;
			inner.transform.localScale = Vector3.one * size;
		}

		private void UpdateHitBlink(Entity entity, ref LavaCrabBossComponent crab)
		{
			if (!hitBlinkStash.Has(entity))
				return;
			ref LavaCrabHitBlinkComponent blink = ref hitBlinkStash.Get(entity);
			blink.View?.SetHits(crab.CurrentHits, crab.HitsToExplode);
			AudioSource sound = blink.fireSoundSource;
			if (sound != null)
			{
				sound.volume = ((float)crab.CurrentHits / crab.HitsToExplode) * MAX_FIRE_SOUND_VOLUME;
				if (!sound.isPlaying)
					sound.Play();
			}
		}

		private void StopHitBlink(Entity entity)
		{
			if (!hitBlinkStash.Has(entity))
				return;
			ref LavaCrabHitBlinkComponent blink = ref hitBlinkStash.Get(entity);
			blink.View?.StopRendererInteraction();
			blink.fireSoundSource?.Stop();
		}

		private void ResetHitBlink(Entity entity)
		{
			if (!hitBlinkStash.Has(entity))
				return;
			ref LavaCrabHitBlinkComponent blink = ref hitBlinkStash.Get(entity);
			blink.View?.Reset();
			AudioSource sound = blink.fireSoundSource;
			if (sound != null)
			{
				sound.volume = 0f;
				sound.Play();
			}
		}
	}
}
