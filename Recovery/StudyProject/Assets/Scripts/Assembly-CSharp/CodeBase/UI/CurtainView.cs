using Codebase.Core;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BrunoMikoski.AnimationSequencer;
using Codebase.Core.UI;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI
{
	public class CurtainView : View
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CPlayHide_003Ed__9 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public CurtainView _003C_003E4__this;

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
		private struct _003CPlayShow_003Ed__8 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public CurtainView _003C_003E4__this;

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
		private AnimationSequencerController showAnimation;

		[SerializeField]
		private AnimationSequencerController hideAnimation;

		[SerializeField]
		private GameObject showAnimationObject;

		[SerializeField]
		private GameObject hideAnimationObject;

		[SerializeField]
		private GameObject darkObject;

		[SerializeField]
		private Image uiBlocker;

		protected override void Awake()
		{
			// Native ELF VA 0x15d4770.
			base.Awake();
			Initialize();
		}

		public override void Initialize()
		{
			// Native ELF VA 0x15d4794.
			base.Initialize();
			GameContext.Set(this);
		}

		
		public async UniTask PlayShow()
		{
			// Native MoveNext ELF VA 0x15d4e34; await the genuine sequencer, no fixed-delay substitute.
			transform.SetAsLastSibling();
			uiBlocker.gameObject.SetActive(true); showAnimationObject.gameObject.SetActive(true);
			showAnimation.ResetToInitialState(); await showAnimation.PlayAsync();
			darkObject.gameObject.SetActive(true); showAnimationObject.gameObject.SetActive(false); hideAnimationObject.gameObject.SetActive(false);
		}

		
		public async UniTask PlayHide()
		{
			// Native MoveNext ELF VA 0x15d4948; await the genuine sequencer, no fixed-delay substitute.
			darkObject.gameObject.SetActive(false); hideAnimationObject.gameObject.SetActive(true);
			hideAnimation.ResetToInitialState(); await hideAnimation.PlayAsync();
			uiBlocker.gameObject.SetActive(false); showAnimationObject.gameObject.SetActive(false); hideAnimationObject.gameObject.SetActive(false);
		}
	}
}
