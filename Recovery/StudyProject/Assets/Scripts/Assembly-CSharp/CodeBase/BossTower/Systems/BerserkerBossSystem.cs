using System;
using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Enemy.Systems;
using Codebase.Meta;
using Codebase.Rage;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using Codebase.BossTower.Helpers;
using Codebase.Enemy.Components;
using Codebase.Unit;
using DG.Tweening;
using UnityEngine;

namespace Codebase.BossTower.Systems
{
	public class BerserkerBossSystem : SystemBase
	{
		[Serializable]
		private struct BerserkerRaidState : ISystemStateComponent, IComponent
		{
			public Entity entity;
		}

		private static readonly float[] ScaleMultipliers = new float[]
		{
			2f,
			3.2f,
			4.8f,
			6.6f
		};
		private const string RAGE_LEVEL_UP_TRIGGER = "StartCast";
		private const float SCALE_TWEEN_TIME = 0.5f;
		public const string MODIFIER_NAME = "BerserkerRage";
		private Filter aliveBossFilter;
		private Event<RageLevelUpEvent> rageLevelUpEvent;
		private SystemStateProcessor<BerserkerRaidState> raidStateProcessor;
		[Inject]
		private Stash<BerserkerBossComponent> berserkerStash;
		[Inject]
		private Stash<UnitComponent> unitStash;
		[Inject]
		private Stash<ScaleComponent> scaleStash;
		[Inject]
		private Stash<BerserkerRageVisualComponent> rageVisualStash;
		[Inject]
		private Stash<UnitDeadMarker> deadFilter;
		[Inject]
		private EnemyCastSystem enemyCastSystem;
		[Inject]
		private RageModel rageModel;
		[Inject]
		private BossTowerModel bossTowerModel;
		[Inject]
		private MetaStatsModel metaStatsModel;
		public bool IsDisposing { get; set; }

		public override void OnAwake()
		{
			aliveBossFilter = Filter.With<BerserkerBossComponent>().With<UnitComponent>().Without<UnitDeadMarker>().Build();
			raidStateProcessor = Filter.With<BerserkerBossComponent>().ToSystemStateProcessor<BerserkerRaidState>(OnBossEnter, OnBossExit);
			rageLevelUpEvent = World.GetEvent<RageLevelUpEvent>();
		}

		public override void Dispose()
		{
			IsDisposing = true;
			raidStateProcessor.Dispose();
		}

		private BerserkerRaidState OnBossEnter(Entity entity)
		{
			rageModel.SetBossMaxLevelBonus(1);
			return new BerserkerRaidState
			{
				entity = entity
			};
		}

		private void OnBossExit(ref BerserkerRaidState state)
		{
			if (World.IsNullOrDisposed() || World.IsDisposed(state.entity) || IsDisposing)
				return;
			rageModel.SetBossMaxLevelBonus(0);
		}

		public override void OnUpdate(float deltaTime)
		{
			raidStateProcessor.Process();
			foreach (RageLevelUpEvent ignored in rageLevelUpEvent.BatchedChanges)
				OnPlayerRageLevelUp();
			foreach (Entity entity in aliveBossFilter)
				AccumulateRage(entity, ref berserkerStash.Get(entity), deltaTime);
		}

		private void OnPlayerRageLevelUp()
		{
			foreach (Entity entity in aliveBossFilter)
			{
				ref BerserkerBossComponent berserker = ref berserkerStash.Get(entity);
				int oldLevel = berserker.RageLevel;
				berserker.CurrentRage *= 1f - berserker.RageDecreaseOnPlayerLevelUp;
				int newLevel = oldLevel;
				while (newLevel > 0 && berserker.CurrentRage < berserker.RageThresholds[newLevel - 1])
					newLevel--;
				if (newLevel == oldLevel)
					continue;
				berserker.RageLevel = newLevel;
				ApplyScale(entity, newLevel, 0f);
				ApplyRageLevelEffects(entity, newLevel, berserker);
			}
		}

		private void AccumulateRage(Entity entity, ref BerserkerBossComponent berserker, float deltaTime)
		{
			if (berserker.RageLevel >= berserker.MaxRageLevel || !bossTowerModel.IsFighting)
				return;
			berserker.CurrentRage += berserker.RagePerSecond * (1f - (float)metaStatsModel.Get("boss_tower_berserker").Value) * deltaTime;
			int newLevel = berserker.RageLevel;
			while (newLevel < berserker.MaxRageLevel && berserker.CurrentRage >= berserker.RageThresholds[newLevel])
				newLevel++;
			if (newLevel != berserker.RageLevel)
				StartRageLevelUpCast(entity, ref berserker, newLevel);
		}

		private void StartRageLevelUpCast(Entity entity, ref BerserkerBossComponent berserker, int newLevel)
		{
			berserker.RageLevel = newLevel;
			BerserkerBossComponent captured = berserker;
			ApplyScale(entity, newLevel, 0.5f);
			enemyCastSystem.StartCast(entity, new EnemyCastingState { Duration = berserker.CastDuration, AnimationTrigger = RAGE_LEVEL_UP_TRIGGER, OnComplete = () => ApplyRageLevelEffects(entity, newLevel, captured) });
		}

		private void ApplyScale(Entity entity, int level, float delay)
		{
			if (!scaleStash.Has(entity))
				return;
			UnitCore unit = unitStash.Get(entity).Core;
			ScaleComponent scale = scaleStash.Get(entity);
			float multiplier = level < 1 ? 1f : ScaleMultipliers[level - 1];
			unit.SetModifier(StatType.VisualScaleMultiplier, ModifierType.Flat, MODIFIER_NAME, multiplier - 1f);
			unit.transform.DOKill(false);
			unit.transform.DOScale(scale.OriginalScale * multiplier, SCALE_TWEEN_TIME).SetDelay(delay).SetEase((Ease)30);
		}

		private void ApplyRageLevelEffects(Entity entity, int level, BerserkerBossComponent berserker)
		{
			if (World.IsDisposed(entity))
				return;
			bool dead = deadFilter.Has(entity);
			if (level < 1 || dead)
				return;
			UnitCore unit = unitStash.Get(entity).Core;
			float rageLevel = level;
			unit.SetPercentModifier(StatType.Damage, MODIFIER_NAME, (((rageLevel * 5f + 20f) * rageLevel + 20f) * rageLevel) / 100f);
			unit.SetPercentModifier(StatType.AttackRange, MODIFIER_NAME, berserker.BonusAttackRangePercentPerLevel * rageLevel);
			unit.SetModifier(StatType.MovementSpeed, ModifierType.Flat, MODIFIER_NAME, berserker.BonusSpeedPerLevel * rageLevel);
			if (!rageVisualStash.Has(entity))
				return;
			BerserkerRageVisualComponent visual = rageVisualStash.Get(entity);
			for (int i = 0; i < visual.RageLevelObjects.Length; i++)
				if (visual.RageLevelObjects[i] != null)
					visual.RageLevelObjects[i].SetActive(i == level - 1);
			if (visual.BlinkEffect != null)
			{
				if (level < 4)
					visual.BlinkEffect.Disable();
				else
					visual.BlinkEffect.Enable();
			}
		}

		public void OnRaidEnded()
		{
			foreach (Entity entity in raidStateProcessor.Entities)
				berserkerStash.Remove(entity);
		}
	}
}
