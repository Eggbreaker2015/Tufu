using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Unit;
using Codebase.Unit.Components;
using Codebase.Vfx;
using Scellecs.Morpeh;
using UnityEngine;
using Codebase.BossTower.Helpers;
using DG.Tweening;

namespace Codebase.BossTower.Systems
{
	public class VikingsBuffSystem : SystemBase
	{
		private const float BuffVfxLifetime = 3f;
		private const float ScaleStep = 0.2f;
		private Filter aliveFilter;
		[Inject]
		private Stash<VikingsBossComponent> vikingsStash;
		[Inject]
		private Stash<UnitComponent> unitStash;
		[Inject]
		private Stash<UnitDeadMarker> deadMarkerStash;
		[Inject]
		private Stash<ScaleComponent> scaleStash;
		[Inject]
		private Stash<BoneEffectSlotsComponent> boneEffectStash;
		[Inject]
		private BoneEffectSlotsSystem boneEffectSlotsSystem;
		[Inject]
		private WorldVfxService worldVfxService;
		public override void OnAwake()
		{
			aliveFilter = Filter.With<VikingsBossComponent>().With<UnitComponent>().Without<UnitDeadMarker>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		public void OnVikingDied(int groupId, VikingsBossComponent vikingData, int deadAlliesCount, Transform diedVikingTransform, ParticleSystem buffVfxPrefab, ParticleSystemForceField buffForceFieldPrefab)
		{
			BuffSurvivingVikings(groupId, vikingData, deadAlliesCount, diedVikingTransform, buffVfxPrefab, buffForceFieldPrefab);
		}

		private void BuffSurvivingVikings(int groupId, VikingsBossComponent vikingData, int deadAlliesCount, Transform diedVikingTransform, ParticleSystem buffVfxPrefab, ParticleSystemForceField buffForceFieldPrefab)
		{
			foreach (Entity entity in aliveFilter)
			{
				if (vikingsStash.Get(entity).GroupId != groupId)
					continue;
				UnitCore unit = unitStash.Get(entity).Core;
				worldVfxService.SpawnWithForceField(buffVfxPrefab, buffForceFieldPrefab, diedVikingTransform, unit.transform, 3f, Vector3.up * .5f);
				ApplyBuffToViking(unit, entity, vikingData, deadAlliesCount);
			}
		}

		public void ApplyBuffToViking(UnitCore unit, Entity entity, VikingsBossComponent vikingData, int deadAlliesCount, bool keepScale = false)
		{
			if (World.IsDisposed(entity) || !vikingsStash.Has(entity) || deadMarkerStash.Has(entity))
				return;
			ApplyBuffToViking(vikingData, deadAlliesCount, unit);
			if (scaleStash.Has(entity) && !keepScale)
			{
				ScaleComponent scale = scaleStash.Get(entity);
				float multiplier = unit.TryGetStat(StatType.VisualScaleMultiplier, out Stat stat) ? (float)stat.Value + .2f : 1.2f;
				unit.SetModifier(StatType.VisualScaleMultiplier, ModifierType.Flat, "VikingBuff", multiplier - 1f);
				unit.transform.DOKill(false);
				unit.transform.DOScale(scale.OriginalScale * multiplier, .6f).SetEase((Ease)30);
			}

			if (boneEffectStash.Has(entity))
				boneEffectSlotsSystem.ApplyEffect(boneEffectStash.Get(entity).Avatar, BoneGroupType.Hands, vikingData.HandEffectPrefab, deadAlliesCount, .2f);
		}

		public static void ApplyBuffToViking(VikingsBossComponent vikingData, int deadAlliesCount, UnitCore unit)
		{
			unit.SetPercentModifier(StatType.Damage, "VikingBuff", vikingData.BuffPerDeadAlly * deadAlliesCount);
		}
	}
}
