using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Unit.Components;
using Codebase.Vfx;
using Scellecs.Morpeh;
using DG.Tweening;
using UnityEngine;

namespace Codebase.Unit.Systems
{
	public class UnitDeathAnimSystem : SystemBase
	{
		private const float MinDeathDuration = 1f;
		private const float PostDeathDelay = 2f;
		private const float SinkDuration = 1.2f;
		private const float SinkDepth = 3f;
		private Filter stateFilter;
		private SystemStateProcessor<UnitDeathAnimState> deathProcessor;
		[Inject]
		private Stash<UnitDeathAnimState> stateStash;
		[Inject]
		private Stash<UnitComponent> unitStash;
		[Inject]
		private Stash<UnitHasDeathAnimMarker> markerStash;
		[Inject]
		private Stash<ScaleComponent> scaleStash;
		[Inject]
		private WorldVfxService worldVfxService;
		public override void OnAwake()
		{
			deathProcessor = Filter.With<UnitDeadMarker>().With<UnitHasDeathAnimMarker>().With<UnitComponent>().Without<PlayerTag>().ToSystemStateProcessor<UnitDeathAnimState>(OnDeathBegan, OnDeathCleaned);
			stateFilter = Filter.With<UnitDeathAnimState>().With<UnitComponent>().Without<PlayerTag>().Build();
		}

		public override void Dispose()
		{
			deathProcessor.Dispose();
		}

		public override void OnUpdate(float deltaTime)
		{
			deathProcessor.Process();
			UpdateDeathTimers(deltaTime);
		}

		private UnitDeathAnimState OnDeathBegan(Entity entity)
		{
			EnemyUnit unit = (EnemyUnit)unitStash.Get(entity).Core;
			unit.SetDeathState(true);
			unit.PlayDeathAudio();
			if (scaleStash.Has(entity))
			{
				ScaleComponent scale = scaleStash.Get(entity);
				if (!scale.keepScaleAfterDeath)
					unit.transform.DOScale(scale.OriginalScale, 0.3f);
			}

			return default;
		}

		private void OnDeathCleaned(ref UnitDeathAnimState state)
		{
		}

		private void UpdateDeathTimers(float deltaTime)
		{
			foreach (Entity entity in stateFilter)
			{
				ref UnitDeathAnimState state = ref stateStash.Get(entity);
				UnitHasDeathAnimMarker marker = markerStash.Get(entity);
				state.Timer += deltaTime;
				if (state.InPostDeathPhase)
				{
					if (!state.InSinkPhase)
					{
						if (state.Timer >= PostDeathDelay)
						{
							state.InSinkPhase = true;
							state.Timer = 0f;
							StartSinkAnimation((EnemyUnit)unitStash.Get(entity).Core);
						}

						continue;
					}

					if (state.Timer < SinkDuration)
						continue;
				}
				else
				{
					EnemyUnit animatedUnit = (EnemyUnit)unitStash.Get(entity).Core;
					if (!IsAnimationComplete(animatedUnit) && state.Timer < MinDeathDuration)
						continue;
					state.InPostDeathPhase = true;
					state.Timer = 0f;
					if (!marker.instantDespawnAfterAnim)
						continue;
				}

				EnemyUnit unit = (EnemyUnit)unitStash.Get(entity).Core;
				ParticleSystem particle = markerStash.Get(entity).deathParticle;
				unit.PlayDisappearAudio();
				if (particle != null)
				{
					Vector3 position = unit.transform.position;
					if (Physics.Raycast(position + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
						position = hit.point + Vector3.up * 0.01f;
					worldVfxService.Spawn(particle, position, Quaternion.identity, 3f);
				}

				if (scaleStash.Has(entity))
				{
					Vector3 originalScale = scaleStash.Get(entity).OriginalScale;
					unit.transform.DOKill();
					unit.transform.localScale = originalScale;
				}

				unit.SetDeathState(false);
				unit.OnDevoured?.Invoke(unit);
				markerStash.Remove(entity);
				return;
			}
		}

		private static void StartSinkAnimation(EnemyUnit unit)
		{
			unit.VisualRoot.DOLocalMoveY(unit.VisualRoot.localPosition.y - SinkDepth, SinkDuration).SetEase(Ease.InQuad);
		}

		private static bool IsAnimationComplete(EnemyUnit unit)
		{
			Animator animator = unit.Animator;
			if (animator == null)
				return true;
			return !animator.IsInTransition(0) && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f;
		}
	}
}
