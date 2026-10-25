using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace SimpleAssets.Common.Animation
{
	public class AnimationStateMachine : StateMachineBehaviour
	{
		public string StateName;

		[CompilerGenerated]
		private Action m_OnExit;

		[CompilerGenerated]
		private Action m_OnReachEnd;

		private bool ReachedEnd { get; set; }

		public event Action OnExit
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

		public event Action OnReachEnd
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

		public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
		}
	}
}
