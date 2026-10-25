using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimpleAssets.Common.Animation;
using UnityEngine;

namespace SimpleAssets.Common.Character
{
	[RequireComponent(typeof(Animator))]
	public class CharacterAnimation : MonoBehaviour
	{
		private static readonly int MoveSpeedHash;

		private static readonly int RunSpeedHash;

		private static readonly int AttackSpeedHash;

		private static readonly int WalkHash;

		private static readonly int RunHash;

		private static readonly int IdleHash;

		private static readonly int AttackHash;

		[CompilerGenerated]
		private Action m_OnAttackReachEnd;

		[CompilerGenerated]
		private Action m_OnAttackExit;

		[CompilerGenerated]
		private Action m_OnDamageTrigger;

		private Animator Animator { get; set; }

		private Dictionary<string, AnimationStateMachine> States { get; set; }

		public event Action OnAttackReachEnd
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public event Action OnAttackExit
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public event Action OnDamageTrigger
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		private void Awake()
		{
		}

		private void OnDestroy()
		{
		}

		private void Initialize()
		{
		}

		private void InitializeEvents()
		{
		}

		public void PlayWalk()
		{
		}

		public void PlayRun()
		{
		}

		public void PlayIdle()
		{
		}

		public void PlayAttack()
		{
		}

		public void SetWalkSpeed(float speed)
		{
		}

		public void SetRunSpeed(float speed)
		{
		}

		public void SetAttackSpeed(float speed)
		{
		}

		public void ResetIdleTrigger()
		{
		}

		public void ResetAttackTrigger()
		{
		}

		public void ResetRunTrigger()
		{
		}

		public void ResetWalkTrigger()
		{
		}

		private void AttackAnimationExit()
		{
		}

		private void AttackAnimationReachEnd()
		{
		}

		private void DamageTrigger()
		{
		}
	}
}
