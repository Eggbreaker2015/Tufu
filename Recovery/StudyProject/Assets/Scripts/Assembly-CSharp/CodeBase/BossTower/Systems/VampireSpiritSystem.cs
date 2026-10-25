using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Unit;
using Codebase.Unit.Components;
using Codebase.Unit.Events;
using Codebase.Vfx;
using Scellecs.Morpeh;
using System;
using CodeBase.Audio;
using Codebase.Abilities.Effects;
using Codebase.BossTower.Helpers;
using Codebase.Enemy;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;

namespace Codebase.BossTower.Systems
{
	public class VampireSpiritSystem : SystemBase
	{
		public const string DAMAGE_MODIFIER_NAME = "VampireSpiritBuff";

		public const string DEFENCE_MODIFIER_NAME = "VampireSpiritArmorBuff";

		public const string SPEED_MODIFIER_NAME = "VampireSpiritSpeedBuff";

		private const float SpiritSoulVfxLifetime = 3f;

		private const float VampireBuffVfxLifetime = 2f;

		private const float SpiritSpawnVfxLifetime = 2f;

		private Filter bossFilter;

		private Filter spiritFilter;

		private Filter deadBossFilter;

		private Filter deadSpiritFilter;

		[Inject]
		private Stash<VampireBossComponent> vampireStash;

		[Inject]
		private Stash<VampireSpiritTag> spiritTagStash;

		[Inject]
		private Stash<UnitComponent> unitStash;

		[Inject]
		private Stash<UnitHasDeathAnimMarker> unitHasDeathAnimMarker;

		[Inject]
		private Stash<LightningVulnerabilityComponent> vulnStash;

		[Inject]
		private Stash<ShockComponent> shockStash;

		[Inject]
		private BossMinionService minionService;

		[Inject]
		private BossTowerModel bossTowerModel;

		[Inject]
		private WorldVfxService worldVfxService;

		[Inject]
		private Stash<PositionComponent> positionsStash;

		[Inject]
		private Stash<VampireBossFightVisualComponent> visualStash;

		[Inject]
		private Stash<BoneEffectSlotsComponent> boneEffectStash;

		[Inject]
		private BoneEffectSlotsSystem boneEffectSlotsSystem;

		private const float SpiritBuffScaleStep = 0.15f;

		private bool spiritsWereActive;

		private Filter visualFilter;

		private Event<UnitDiedEvent> unitDiedEvent;

		public override void OnAwake()
		{
			// Original ELF VA 0x1570c2c.
			bossFilter = Filter.With<VampireBossComponent>().With<UnitComponent>().Without<UnitDeadMarker>().Build();
			spiritFilter = Filter.With<VampireSpiritTag>().With<UnitComponent>().With<BossArenaUnitTag>().Without<UnitDeadMarker>().Build();
			deadBossFilter = Filter.With<VampireBossComponent>().With<UnitDeadMarker>().Build();
			deadSpiritFilter = Filter.With<VampireSpiritTag>().With<UnitDeadMarker>().Build();
			visualFilter = Filter.With<VampireBossFightVisualComponent>().Build();
			unitDiedEvent = World.GetEvent<UnitDiedEvent>();
		}

		public override void OnUpdate(float deltaTime)
		{
			// Original ELF VA 0x157104c: active spirits pause the next summon timer.
			if (!bossTowerModel.IsFighting) return;
			UpdateBosses(deltaTime);
			UpdateSpirits();
			if (bossFilter.IsEmpty() && !spiritFilter.IsEmpty())
				foreach (Entity spirit in spiritFilter) minionService.Despawn((EnemyUnit)unitStash.Get(spirit).Core);
		}

		public void OnRaidEnded()
		{
			// Original ELF VA 0x1571834; both live and dead pooled entities are cleaned.
			foreach (Entity entity in bossFilter)
			{
				if (boneEffectStash.Has(entity)) boneEffectSlotsSystem.ClearAllEffects(boneEffectStash.Get(entity).Avatar);
				vampireStash.Remove(entity);
			}
			foreach (Entity entity in deadBossFilter)
			{
				if (boneEffectStash.Has(entity)) boneEffectSlotsSystem.ClearAllEffects(boneEffectStash.Get(entity).Avatar);
				vampireStash.Remove(entity);
			}
			foreach (Entity entity in spiritFilter) spiritTagStash.Remove(entity);
			foreach (Entity entity in deadSpiritFilter) spiritTagStash.Remove(entity);
			spiritsWereActive = false;
			foreach (Entity entity in visualFilter)
			{
				ref VampireBossFightVisualComponent visual = ref visualStash.Get(entity);
				visual.CenterVisual.SetActive(false);
				HideAllSpawnVfx(ref visual);
			}
		}

		public bool TryGetSpiritTimerData(out float normalizedProgress)
		{
			// Original ELF VA 0x1571da4; the original progress is deliberately unclamped.
			normalizedProgress = 0f;
			if (!bossTowerModel.IsFighting || !spiritFilter.IsEmpty()) return false;
			foreach (Entity entity in bossFilter)
			{
				ref VampireBossComponent vampire = ref vampireStash.Get(entity);
				if (!(vampire.SpiritInterval > 0f)) continue;
				normalizedProgress = 1f - vampire.SpiritTimer / vampire.SpiritInterval;
				return true;
			}
			return false;
		}

		public void ApplyChainLightning(double damage, bool isEmpowered)
		{
			// Original ELF VA 0x1571f08: every live arena spirit receives the same strike.
			foreach (Entity entity in spiritFilter)
				SpawnLightningEffect.ApplyLightningToUnit((EnemyUnit)unitStash.Get(entity).Core, damage, unitDiedEvent, vulnStash, shockStash, isEmpowered);
		}

		private void UpdateBosses(float deltaTime)
		{
			// Original ELF VA 0x157120c.
			if (!spiritFilter.IsEmpty()) { spiritsWereActive = true; return; }
			if (spiritsWereActive)
			{
				spiritsWereActive = false;
				foreach (Entity entity in visualFilter)
				{
					ref VampireBossFightVisualComponent visual = ref visualStash.Get(entity);
					HideAllSpawnVfx(ref visual);
				}
			}
			foreach (Entity entity in bossFilter)
			{
				ref VampireBossComponent vampire = ref vampireStash.Get(entity);
				if (!vampire.SpawnPointsPreselected) PreselectSpawnPoints(ref vampire);
				vampire.SpiritTimer -= deltaTime;
				if (vampire.SpiritTimer <= 0f)
				{
					SpawnSpirits(entity, ref vampire);
					vampire.SpawnPointsPreselected = false;
					vampire.SpiritTimer = vampire.SpiritInterval;
					vampire.PreselectedSpawnPointIndices = null;
				}
			}
		}

		private void PreselectSpawnPoints(ref VampireBossComponent vampire)
		{
			// Original ELF VA 0x1572098.
			vampire.SpawnPointsPreselected = true;
			foreach (Entity entity in visualFilter)
			{
				ref VampireBossFightVisualComponent visual = ref visualStash.Get(entity);
				vampire.PreselectedSpawnPointIndices = GetRandomIndices(visual.SpawnPoints.Length, Math.Min(vampire.SpiritCount, visual.SpawnPoints.Length));
				foreach (int index in vampire.PreselectedSpawnPointIndices)
				{
					VampireSpawnPointEntry point = visual.SpawnPoints[index];
					point.RotationEffect?.Play(vampire.SpiritTimer);
					point.Vfx.SetActive(true);
				}
			}
		}

		private int[] GetRandomIndices(int total, int count)
		{
			// Original ELF VA 0x1572934: partial Fisher-Yates, preserving Unity random draws.
			int[] indices = new int[total];
			for (int i = 0; i < total; i++) indices[i] = i;
			int[] selected = new int[count];
			for (int i = 0; i < count; i++)
			{
				int other = UnityEngine.Random.Range(i, total);
				int value = indices[i]; indices[i] = indices[other]; indices[other] = value;
				selected[i] = indices[i];
			}
			return selected;
		}

		private void HideAllSpawnVfx(ref VampireBossFightVisualComponent visual)
		{
			// Original ELF VA 0x1571d18.
			foreach (VampireSpawnPointEntry point in visual.SpawnPoints)
			{
				point.RotationEffect?.Stop();
				point.Vfx.SetActive(false);
			}
		}

		private void SpawnSpirits(Entity vampireEntity, ref VampireBossComponent vampire)
		{
			// Original ELF VA 0x157228c.
			if (vampire.SpiritPrefab == null || vampire.SpiritConfig == null) return;
			Stash<UnitFirstVulnerabilityComponent> firstVulnerability = Scellecs.Morpeh.World.Default.GetStash<UnitFirstVulnerabilityComponent>();
			GameContext.Get<IAudioPlayer>().PlaySoundOnce("vampire_summon_spawn");
			foreach (Entity entity in visualFilter)
			{
				ref VampireBossFightVisualComponent visual = ref visualStash.Get(entity);
				for (int i = 0; i < vampire.SpiritCount && i < vampire.PreselectedSpawnPointIndices.Length; i++)
				{
					Vector3 position = visual.SpawnPoints[vampire.PreselectedSpawnPointIndices[i]].Point.position;
					if (NavMesh.SamplePosition(position, out NavMeshHit hit, .5f, NavMesh.AllAreas)) position = hit.position;
					EnemyUnit spirit = minionService.Spawn(string.Format("vampire_minion_{0}", i), vampire.SpiritPrefab, position, vampire.SpiritConfig, true, vampire.BakedSpiritStats, true);
					Entity spiritEntity = spirit.GetComponent<Scellecs.Morpeh.Providers.EntityProvider>().Entity;
					spirit.transform.DOScale(spirit.OriginalScale, .3f).From(Vector3.one * .1f).SetEase(Ease.InOutQuad);
					if (!spirit.Agent.isOnNavMesh)
					{
						spirit.Agent.enabled = false;
						spirit.Agent.enabled = true;
						spirit.Agent.Warp(position);
					}
					unitHasDeathAnimMarker.Set(spiritEntity, default);
					spiritTagStash.Set(spiritEntity, new VampireSpiritTag { OwnerVampire = vampireEntity });
					firstVulnerability.Set(spiritEntity, new UnitFirstVulnerabilityComponent { DamageModifier = 2f });
					worldVfxService.Spawn(vampire.SpiritSpawnVfx, position, Quaternion.identity, SpiritSpawnVfxLifetime);
				}
				HideAllSpawnVfx(ref visual);
			}
		}

		private void UpdateSpirits()
		{
			// Original ELF VA 0x1571478: all spirits seek the final enumerated visual center.
			Vector3 center = Vector3.zero;
			foreach (Entity entity in visualFilter) center = visualStash.Get(entity).CenterPoint.position;
			foreach (Entity entity in spiritFilter)
			{
				Entity owner = spiritTagStash.Get(entity).OwnerVampire;
				if (!vampireStash.Has(owner)) continue;
				ref VampireBossComponent vampire = ref vampireStash.Get(owner);
				UnitCore spirit = unitStash.Get(entity).Core;
				if (Vector3.Distance(spirit.transform.position, center) < vampire.SpiritReachDistance)
					OnSpiritReachedVampire(entity, owner);
				else if (spirit.Agent.isOnNavMesh)
				{
					spirit.Agent.isStopped = false;
					spirit.Agent.updateRotation = true;
					spirit.Agent.SetDestination(center);
				}
			}
		}

		private void OnSpiritReachedVampire(Entity spiritEntity, Entity vampireEntity)
		{
			// Original ELF VA 0x1572a60.
			ref VampireBossComponent vampire = ref vampireStash.Get(vampireEntity);
			UnitCore vampireUnit = unitStash.Get(vampireEntity).Core;
			Vector3 position = positionsStash.Get(spiritEntity).Position;
			UnitCore spirit = unitStash.Get(spiritEntity).Core;
			if (!vampireUnit.IsDead)
			{
				HealVampire(vampireUnit, ref vampire);
				worldVfxService.Spawn(vampire.VampireBuffVfx, vampireUnit.transform, Vector3.up * 1.3f + Vector3.forward * .5f, VampireBuffVfxLifetime);
				if (boneEffectStash.Has(vampireEntity))
					boneEffectSlotsSystem.ApplyEffect(boneEffectStash.Get(vampireEntity).Avatar, BoneGroupType.Hands, vampire.HandEffectPrefab, vampire.consumedSpirits, SpiritBuffScaleStep);
			}
			GameContext.Get<IAudioPlayer>().PlayAtPosition("vampire_summon_death", position);
			worldVfxService.Spawn(vampire.SpiritVfx, position, Quaternion.identity, vampire.SpiritVfxLifetime);
			worldVfxService.SpawnWithForceField(vampire.SpiritSoulVfx, vampire.SpiritSoulForceField, spirit.transform, vampireUnit.transform, SpiritSoulVfxLifetime, Vector3.up);
			spirit.CurrentHealth = 0;
			spirit.OnDied?.Invoke(spirit);
		}

		private static void HealVampire(UnitCore vampireUnit, ref VampireBossComponent vampire)
		{
			// Original ELF VA 0x1572efc; modifier replacement preserves original float32 products.
			double maximum = vampireUnit.GetStatValue(StatType.Health);
			vampire.consumedSpirits++;
			vampireUnit.CurrentHealth = Math.Min(maximum * vampire.HealPercent + vampireUnit.CurrentHealth, maximum);
			vampireUnit.SetModifier(StatType.Damage, ModifierType.PercentAdditive, DAMAGE_MODIFIER_NAME, vampire.DamageBoostPercent * vampire.consumedSpirits);
			vampireUnit.SetModifier(StatType.MovementSpeed, ModifierType.Flat, SPEED_MODIFIER_NAME, vampire.SpeedBoostPerSpirit * vampire.consumedSpirits);
			double armor = vampireUnit.GetStatValue(StatType.UniversalDefencePercent);
			if (vampire.ArmorBoostOnSpiritReach && armor < (double).9f)
				vampireUnit.SetModifier(StatType.UniversalDefencePercent, ModifierType.Flat, DEFENCE_MODIFIER_NAME, vampire.ArmorBoostPercent * vampire.consumedSpirits);
		}
	}
}
