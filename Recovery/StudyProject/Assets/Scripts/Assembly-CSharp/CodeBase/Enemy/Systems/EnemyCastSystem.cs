using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Codebase.Core;
using Codebase.Enemy.Components;
using Codebase.Unit.Components;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Enemy.Systems
{
	public class EnemyCastSystem : SystemBase
	{
		[Serializable]
		private struct InternalState : ISystemStateComponent, IComponent
		{
			public CancellationTokenSource Cts;
		}

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CDefaultCastAsync_003Ed__14 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public float duration;
			public CancellationToken ct;
			public Action onComplete;
			private UniTask.Awaiter _003C_003Eu__1;
			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CRunCastAsync_003Ed__13 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;
			public EnemyCastSystem _003C_003E4__this;
			public float duration;
			public Action onComplete;
			public CancellationToken ct;
			public Entity entity;
			public EnemyUnit enemy;
			private UniTask.Awaiter _003C_003Eu__1;
			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[Inject]
		private Stash<EnemyCastingState> castingStash;
		[Inject]
		private Stash<UnitComponent> unitStash;
		[Inject]
		private Stash<EnemyCastAbilityVisualComponent> visualStash;
		private SystemStateProcessor<InternalState> processor;
		public override void OnAwake()
		{
			processor = Filter.With<EnemyCastingState>().With<UnitComponent>().Without<Codebase.Unit.Components.UnitDeadMarker>().ToSystemStateProcessor<InternalState>(OnCastStarted, OnCastEnded);
		}

		public override void Dispose()
		{
			processor.Dispose();
		}

		public override void OnUpdate(float deltaTime)
		{
			processor.Process();
		}

		public void StartCast(Entity entity, EnemyCastingState data)
		{
			castingStash.Set(entity, data);
		}

		public bool IsCasting(Entity entity)
		{
			return castingStash.Has(entity);
		}

		public void OnRaidEnded()
		{
			processor.DestroyAllStates();
		}

		private InternalState OnCastStarted(Entity entity)
		{
			ref EnemyCastingState cast = ref castingStash.Get(entity);
			cast.StartTime = Time.time;
			string animationBool = cast.AnimationBool, animationTrigger = cast.AnimationTrigger;
			float duration = cast.Duration;
			EnemyCastAbilityVisualComponent visual = visualStash.Has(entity) ? visualStash.Get(entity) : default;
			EnemyUnit enemy = unitStash.Get(entity).Core as EnemyUnit;
			if (enemy is not null)
			{
				enemy.IsCasting = true;
				if (!string.IsNullOrEmpty(animationBool) && enemy.Animator != null)
					enemy.Animator.SetBool(animationBool, true);
				if (!string.IsNullOrEmpty(animationTrigger) && enemy.Animator != null)
					enemy.Animator.SetTrigger(animationTrigger);
			}

			if (visual.VisualObject != null)
				visual.VisualObject.SetActive(true);
			if (visual.Sequencer != null)
				visual.Sequencer.PlayForward(true, null);
			var cts = new CancellationTokenSource();
			RunCastAsync(entity, enemy, duration, cast.SpecialCastKey, cast.OnComplete, cts.Token).Forget();
			return new InternalState
			{
				Cts = cts
			};
		}

		private void OnCastEnded(ref InternalState state)
		{
			state.Cts?.Cancel();
			state.Cts?.Dispose();
			state.Cts = null;
		}

		private async UniTaskVoid RunCastAsync(Entity entity, EnemyUnit enemy, float duration, string specialCastKey, Action onComplete, CancellationToken ct)
		{
			try
			{
				try
				{
					await DefaultCastAsync(duration, onComplete, ct);
				}
				catch (OperationCanceledException)
				{
				}
			}
			finally
			{
				if (!World.IsDisposed(entity) && visualStash.Has(entity))
				{
					EnemyCastAbilityVisualComponent visual = visualStash.Get(entity);
					if (visual.VisualObject != null)
						visual.VisualObject.SetActive(false);
					if (visual.Sequencer != null)
					{
						visual.Sequencer.ClearPlayingSequence();
						visual.Sequencer.ResetToInitialState();
						visual.Sequencer.SetProgress(0f, false);
					}

					EnemyCastingState cast = castingStash.Get(entity);
					if (enemy != null)
					{
						if (!string.IsNullOrEmpty(cast.AnimationBool) && enemy.Animator != null)
							enemy.Animator.SetBool(cast.AnimationBool, false);
						if (!string.IsNullOrEmpty(cast.AnimationTrigger) && enemy.Animator != null)
							enemy.Animator.ResetTrigger(cast.AnimationTrigger);
					}
				}

				if (enemy != null)
					enemy.IsCasting = false;
				if (castingStash.Has(entity))
					castingStash.Remove(entity);
			}
		}

		private async UniTask DefaultCastAsync(float duration, Action onComplete, CancellationToken ct)
		{
			await UniTask.Delay(TimeSpan.FromSeconds(duration), false, (PlayerLoopTiming)8, ct, false);
			onComplete?.Invoke();
		}
	}
}
