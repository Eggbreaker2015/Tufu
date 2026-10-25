using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using BrunoMikoski.AnimationSequencer;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using UnityEngine;

namespace Codebase.ProximityView
{
	public class ProximityViewHideAnimation : MonoBehaviour
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CHideRoutine_003Ed__5 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;

			public ProximityViewHideAnimation _003C_003E4__this;

			public CancellationToken token;

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

		[SerializeField]
		private AnimationSequencerController hideAnimation;

		private CancellationTokenSource cts;

		private void OnEnable()
		{
		}

		public void Show()
		{
		}

		public void Hide(Action onComplete)
		{
		}

		[AsyncStateMachine(typeof(_003CHideRoutine_003Ed__5))]
		private UniTaskVoid HideRoutine(Action onComplete, CancellationToken token)
		{
			return default;
		}
	}
}
