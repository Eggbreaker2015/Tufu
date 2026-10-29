using Codebase.Player.Commands;
using UnityEngine.AI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Enemy;
using Codebase.Core;
using Codebase.Player.Input;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using Codebase.Player.Models;
using Codebase.Unit;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using DG.Tweening;
using UnityEngine;

namespace Codebase.Player
{
	public class PlayerUnit : HeroUnit
	{
		[CompilerGenerated]
		private sealed class _003C_003Ec__DisplayClass53_0
		{
			public bool jumpOutPlayed;

			public PlayerUnit _003C_003E4__this;

			public Vector3 firecampPosition;

			public ParticleSystem jumpOutEffect;

			internal void _003CDeathSequenceCoroutine_003Eb__0()
			{
			}
		}

		[CompilerGenerated]
		private sealed class _003CDeathSequenceCoroutine_003Ed__53 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public PlayerUnit _003C_003E4__this;

			public Vector3 firecampPosition;

			public ParticleSystem jumpOutEffect;

			public PlayerModel playerModel;

			public string killer;

			private _003C_003Ec__DisplayClass53_0 _003C_003E8__1;

			public Vector3 landPosition;

			public ParticleSystem underFirecampEffect;

			private Vector3 _003CunderPos_003E5__2;

			private Vector3 _003ClandPos_003E5__3;

			private float _003ClandingWait_003E5__4;

			object IEnumerator<object>.Current
			{
				[DebuggerHidden]
				get
				{
					return this._003C_003E2__current;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return this._003C_003E2__current;
				}
			}

			[DebuggerHidden]
			public _003CDeathSequenceCoroutine_003Ed__53(int _003C_003E1__state)
			{
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
			}

			private bool MoveNext()
			{
				return false;
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
			}
		}

		[CompilerGenerated]
		private sealed class _003CPlayDeathAnimation_003Ed__52 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public PlayerUnit _003C_003E4__this;

			public PlayerModel playerModel;

			object IEnumerator<object>.Current
			{
				[DebuggerHidden]
				get
				{
					return this._003C_003E2__current;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return this._003C_003E2__current;
				}
			}

			[DebuggerHidden]
			public _003CPlayDeathAnimation_003Ed__52(int _003C_003E1__state)
			{
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
			}

			private bool MoveNext()
			{
				return false;
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
			}
		}

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CStartDeathAnimation_003Ed__51 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public PlayerUnit _003C_003E4__this;

			public PlayerModel playerModel;

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

		[CompilerGenerated]
		private sealed class _003CWaitForAnimatorStateEnd_003Ed__57 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public PlayerUnit _003C_003E4__this;

			public string stateName;

			private float _003Celapsed_003E5__2;

			object IEnumerator<object>.Current
			{
				[DebuggerHidden]
				get
				{
					return this._003C_003E2__current;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return this._003C_003E2__current;
				}
			}

			[DebuggerHidden]
			public _003CWaitForAnimatorStateEnd_003Ed__57(int _003C_003E1__state)
			{
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
			}

			private bool MoveNext()
			{
				return false;
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
			}
		}

		private const float AnimatorWaitTimeout = 3f;

		private static readonly int IsWalking = Animator.StringToHash("isWalking");

		private static readonly int DieTrigger = Animator.StringToHash("die");

		private static readonly int LandingTrigger = Animator.StringToHash("landing");

		private static readonly int BattlecryTrigger = Animator.StringToHash("battlecry");

		private static readonly int BattlecryState = Animator.StringToHash("Battlecry");

		[SerializeField]
		private Animator animator;

		[SerializeField]
		private AnimationEventReceiver animationEvents;

		[SerializeField]
		private AudioSource audioSource;

		[SerializeField]
		private ParticleSystem sleepParticles;

		[SerializeField]
		private ParticleSystem aliveParticles;

		[SerializeField]
		private Transform rig;

		[SerializeField]
		public GameObject questIndicator;

		[Header("Death sequence")]
		[SerializeField]
		private float underFirecampDepth = 3f;

		[SerializeField]
		private float jumpHeight = 4f;

		[SerializeField]
		private float jumpUpDuration = 0.4f;

		[SerializeField]
		private float jumpDownDuration = 0.4f;

		[SerializeField]
		private Ease jumpUpEase = (Ease)6;

		[SerializeField]
		private Ease jumpDownEase = (Ease)5;

		[Header("Warp animation")]
		[SerializeField]
		[Range(0f, 2f)]
		private float warpYDescentDuration = 0.25f;

		[SerializeField]
		[Range(0f, 2f)]
		private float warpXzMoveDuration = 0.25f;

		[SerializeField]
		[Range(0f, 2f)]
		private float warpPauseDuration = 0.1f;

		[Header("Death sequence effects")]
		[SerializeField]
		private ParticleSystem deathEffect;

		[SerializeField]
		private ParticleSystem respawnEffect;

		[SerializeField]
		private ParticleSystem landingEffect;

		[Header("Death phase timing")]
		[SerializeField]
		[Range(0.05f, 3f)]
		private float hideDuration = 0.3f;

		[SerializeField]
		[Range(0.05f, 3f)]
		private float restoreDuration = 0.3f;

		[SerializeField]
		[Range(0.01f, 1f)]
		private float vanishDuration = 0.15f;

		public Action<DeathPhase, float> OnDeathPhaseEnter = delegate { };

		private PlayerModel currentPlayerModel;

		public override void SetWalking(bool v) => animator.SetBool(IsWalking, v);

		public bool IsSleeping { get; private set; }

		public DeathPhase CurrentPhase { get; private set; }

		public string KillerName { get; set; }

		private void OnEnable()
		{
			// Native ELF VA 0x14450c0; bounded assembly contains two event subscriptions.
			animationEvents.OnWeaponAppear += HandleWeaponAppear;
			animationEvents.OnWalkEnabled += HandleWalkEnabled;
		}

		private void OnDisable()
		{
			// Native ELF VA 0x1445198.
			animationEvents.OnWeaponAppear -= HandleWeaponAppear;
			animationEvents.OnWalkEnabled -= HandleWalkEnabled;
		}

		public void SetSleeping(bool sleeping)
		{
			// Native ELF VA 0x1445270.
			if (IsSleeping == sleeping) return;
			IsSleeping = sleeping;
			if (sleepParticles)
			{
				ParticleSystem.MainModule main = sleepParticles.main;
				main.simulationSpeed = sleeping ? 1f : 3f;
				if (sleeping) sleepParticles.Play();
				else sleepParticles.Stop();
			}
		}

		private void Update()
		{
			// Native ELF VA 0x1445358. Walking reflects input, with the original 0.15 audio scale.
			if (global::Codebase.Rpg.RpgModeConfig.Enabled && !IsControlled && !IsDying)
			{
				float velocity = Agent.enabled ? Agent.velocity.magnitude : 0f;
				animator.SetBool(IsWalking, Agent.enabled && Agent.velocity.sqrMagnitude > 0.01f);
				animator.speed = 1f;
				audioSource.volume = 0.15f * Mathf.Clamp01(velocity / Mathf.Max(Agent.speed, 0.01f));
				rig.localScale = Vector3.one * 1.35f;
				return;
			}
			if (!IsDying)
			{
				float input = GameContext.Get<PlayerModel>().CanMove && !global::Codebase.Skills.FriendlyDebuffs.IsStunned(this) ? GameContext.Get<Joystick>().GetInput().magnitude : 0f;
				animator.SetBool(IsWalking, input > 0f);
				animator.speed = 1f;
				audioSource.volume = input * 0.15f;
			}
			else audioSource.volume = 0f;
			rig.localScale = Vector3.one * 1.35f;
		}

		public void StartDeathSequence(string killer, Vector3 firecampPosition, Vector3 landPosition, ParticleSystem jumpOutEffect, ParticleSystem underFirecampEffect, PlayerModel playerModel)
		{
// Native ELF VA 0x144555c.
            currentPlayerModel = playerModel;
            StartCoroutine(DeathSequenceCoroutine(killer, firecampPosition, landPosition, jumpOutEffect, underFirecampEffect, playerModel));
		}

		
		public async UniTask StartDeathAnimation(PlayerModel playerModel)
		{
// Native async state body ELF VA 0x1446d64 uses the MonoBehaviour coroutine runner.
            currentPlayerModel = playerModel;
            await PlayDeathAnimation(playerModel).ToUniTask(this);
		}

		
		private IEnumerator PlayDeathAnimation(PlayerModel playerModel)
		{
// Native state body ELF VA 0x1446af8.
            IsDying = true;
            IsDeathAnimationActive = true;
            playerModel.CanMove = false;
            CurrentTarget = null;
            Agent.isStopped = true;
            Agent.ResetPath();
            Agent.enabled = false;
            animator.SetBool(IsWalking, false);
            animator.SetTrigger(DieTrigger);
            if (deathEffect) deathEffect.Play();
            SetPhase(DeathPhase.Hiding, hideDuration);
            yield return WaitForAnimatorStateEnd("Dying");
            yield return new WaitForSeconds(1f);
            IsDeathAnimationActive = false;
		}

		
		private IEnumerator DeathSequenceCoroutine(string killer, Vector3 firecampPosition, Vector3 landPosition, ParticleSystem jumpOutEffect, ParticleSystem underFirecampEffect, PlayerModel playerModel)
		{
// Original coroutine state body ELF VA 0x1446070; phase/tween/event order is preserved.
            IsDying = true;
            IsReviving = true;
            playerModel.CanMove = false;
            CurrentTarget = null;
            // Unity 6000.6 rejects Stop/ResetPath after PlayDeathAnimation has disabled this same agent.
            // Preserve the original stop/reset effect when active; an already disabled agent is already stopped.
            if (Agent.enabled && Agent.isOnNavMesh) { Agent.isStopped = true; Agent.ResetPath(); }
            Agent.enabled = false;
            World.Default.GetStash<NavMeshAgentActiveMarker>().Remove(EntityProvider.Entity);
            yield return WaitForAnimatorStateEnd("Dying");
            App.ExecuteCommand(new RevivePlayerCommand { KillerName = killer, restoreHealth = false, revivePosition = firecampPosition });
            SetPhase(DeathPhase.Vanishing, vanishDuration);
            if (aliveParticles) aliveParticles.Stop();
            Vector3 underPosition = firecampPosition + Vector3.down * underFirecampDepth;
            Vector3 landingPosition = landPosition;
            if (warpYDescentDuration > 0f)
                yield return transform.DOMoveY(underPosition.y, warpYDescentDuration).SetEase(Ease.Linear).WaitForCompletion();
            else transform.position = new Vector3(transform.position.x, underPosition.y, transform.position.z);
            if (warpXzMoveDuration > 0f)
                yield return transform.DOMove(underPosition, warpXzMoveDuration).SetEase(Ease.Linear).WaitForCompletion();
            else transform.position = underPosition;
            transform.rotation = Quaternion.LookRotation(new Vector3(landingPosition.x - firecampPosition.x, 0f, landingPosition.z - firecampPosition.z));
            SetPhase(DeathPhase.Warped, 0f);
            if (underFirecampEffect) underFirecampEffect.Play();
            if (warpPauseDuration > 0f) yield return new WaitForSeconds(warpPauseDuration);
            animator.SetTrigger(LandingTrigger);
            float landingWait = 0f;
            while (animator.IsInTransition(0) || !animator.GetCurrentAnimatorStateInfo(0).IsName("Landing"))
            {
                landingWait += Time.deltaTime;
                if (landingWait >= AnimatorWaitTimeout) break;
                yield return null;
            }
            animator.speed = 0f;
            bool jumpOutPlayed = false;
            Vector3 midpoint = new Vector3((underPosition.x + landingPosition.x) * 0.5f, Mathf.Max(firecampPosition.y, landingPosition.y) + jumpHeight, (underPosition.z + landingPosition.z) * 0.5f);
            yield return transform.DOMove(midpoint, jumpUpDuration).SetEase(jumpUpEase).OnUpdate(() =>
            {
                if (jumpOutPlayed || transform.position.y < firecampPosition.y) return;
                jumpOutPlayed = true;
                if (jumpOutEffect) jumpOutEffect.Play();
                if (respawnEffect) respawnEffect.Play();
            }).WaitForCompletion();
            animator.speed = 1f;
            if (NavMesh.SamplePosition(landingPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas)) landingPosition = hit.position;
            yield return transform.DOMove(landingPosition, jumpDownDuration).SetEase(jumpDownEase).WaitForCompletion();
            if (landingEffect) landingEffect.Play();
            if (IsDying && CurrentPhase != DeathPhase.Reviving) SetPhase(DeathPhase.Reviving, restoreDuration);
            animator.SetTrigger(BattlecryTrigger);
            yield return new WaitForSeconds(0.5f);
            HandleWalkEnabled();
            currentPlayerModel.SavePositionOverride = null;
		}

		private void HandleWeaponAppear()
		{
			// Native ELF VA 0x14458a4.
			if (IsDying && CurrentPhase != DeathPhase.Reviving) SetPhase(DeathPhase.Reviving, restoreDuration);
		}

		private void HandleWalkEnabled()
		{
			// Native ELF VA 0x1445904; original ECS markers and agent are reused.
			if (!IsDying) return;
			Agent.enabled = true;
			World.Default.GetStash<NavMeshAgentActiveMarker>().Set(EntityProvider.Entity);
			Agent.Warp(transform.position);
			Agent.isStopped = false;
			currentPlayerModel.CanMove = true;
			IsDying = false;
			SetPhase(DeathPhase.Alive, 0f);
			aliveParticles.Play();
			IsReviving = false;
			World.Default.GetStash<UnitDeadMarker>().Remove(EntityProvider.Entity);
		}

		private void SetPhase(DeathPhase phase, float duration)
		{
			// Native ELF VA 0x14458c8.
			CurrentPhase = phase;
			OnDeathPhaseEnter(phase, duration);
		}

		
		private IEnumerator WaitForAnimatorStateEnd(string stateName)
		{
// Native ELF VA 0x1447144: one cumulative three-second timeout covers entering and playing the state.
            float elapsed = 0f;
            while (animator.IsInTransition(0) || !animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
            {
                elapsed += Time.deltaTime;
                if (elapsed >= AnimatorWaitTimeout) yield break;
                yield return null;
            }
            while (!animator.IsInTransition(0))
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (!state.IsName(stateName) || (state.normalizedTime >= 1f && animator.speed > 0f)) yield break;
                elapsed += Time.deltaTime;
                if (elapsed >= AnimatorWaitTimeout) yield break;
                yield return null;
            }
		}

		public void SetAliveState()
		{
			// Native ELF VA 0x1445bd4.
			animator.speed = 1f;
			animator.Play(BattlecryState, 0, 0f);
			SetPhase(DeathPhase.Warped, 0f);
			SetPhase(DeathPhase.Reviving, 0f);
			HandleWalkEnabled();
		}
	}
}
