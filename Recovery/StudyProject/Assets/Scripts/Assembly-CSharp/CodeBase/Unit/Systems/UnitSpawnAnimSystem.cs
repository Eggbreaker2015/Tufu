using Codebase.Core;
using Codebase.Enemy;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using DG.Tweening;
using UnityEngine;

namespace Codebase.Unit.Systems
{
	public class UnitSpawnAnimSystem : SystemBase
	{
		private const float MinAnimCheckDelay = 0.2f;
		private Filter stateFilter;
		private SystemStateProcessor<UnitSpawnAnimState> spawnProcessor;
		[Inject]
		private Stash<UnitSpawnAnimState> stateStash;
		[Inject]
		private Stash<UnitHasSpawnAnimComponent> animStash;
		[Inject]
		private Stash<UnitComponent> unitStash;
		[Inject]
		private Stash<UnitSpawnActiveMarker> spawnActiveStash;
		public override void OnAwake()
		{
			spawnProcessor = Filter.With<UnitHasSpawnAnimComponent>().With<UnitComponent>().Without<UnitDeadMarker>().Without<PlayerTag>().ToSystemStateProcessor<UnitSpawnAnimState>(OnSpawnBegan, OnSpawnCleaned);
			stateFilter = Filter.With<UnitSpawnAnimState>().With<UnitComponent>().Without<UnitDeadMarker>().Without<PlayerTag>().Build();
		}

		public override void Dispose()
		{
			spawnProcessor.Dispose();
		}

		public override void OnUpdate(float deltaTime)
		{
			spawnProcessor.Process();
			UpdateSpawnTimers(deltaTime);
		}

		private UnitSpawnAnimState OnSpawnBegan(Entity entity)
		{
			EnemyUnit unit = (EnemyUnit)unitStash.Get(entity).Core;
			int trigger = animStash.Get(entity).TriggerHash;
			if (unit.Animator != null)
			{
				unit.SetDeathState(false);
				unit.PlaySpawnAudio();
				unit.Animator.SetTrigger(trigger);
			}

			spawnActiveStash.Set(entity);
			return default;
		}

		private void OnSpawnCleaned(ref UnitSpawnAnimState state)
		{
		}

		private void UpdateSpawnTimers(float deltaTime)
		{
			foreach (Entity entity in stateFilter)
			{
				ref UnitSpawnAnimState state = ref stateStash.Get(entity);
				UnitHasSpawnAnimComponent animation = animStash.Get(entity);
				if (state.Timer < 0f)
					continue;
				state.Timer += deltaTime;
				if (state.Timer < MinAnimCheckDelay)
					continue;
				EnemyUnit unit = (EnemyUnit)unitStash.Get(entity).Core;
				if (animation.finishByTimer ? state.Timer < animation.maxTimer : !IsAnimationComplete(unit))
					continue;
				spawnActiveStash.Remove(entity);
				state.Timer = -1f;
			}
		}

		private static bool IsAnimationComplete(EnemyUnit unit)
		{
			Animator animator = unit.Animator;
			if (animator == null)
				return true;
			return !animator.GetCurrentAnimatorStateInfo(0).IsTag("Spawn") && !animator.IsInTransition(0);
		}
	}
}
