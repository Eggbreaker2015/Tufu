using Codebase.Core;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BrunoMikoski.AnimationSequencer;
using Codebase.Core.UI;
using Codebase.Player;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.UI
{
	public class PlayerDeathScreenView : View
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CPlayAsync_003Ed__6 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public PlayerDeathScreenView _003C_003E4__this;

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
		private AnimationSequencerController deathAnimation;

		[SerializeField]
		private CanvasGroup canvasGroup;

		private Event<PlayerDeathEvent> playerDeathEvent;

		protected override void Awake()
		{
base.Awake();
            Initialize();
		}

		public override void Initialize()
		{
// Native ELF VA 0x15f84ec.
            base.Initialize();
            GameContext.Set(this);
            playerDeathEvent = Scellecs.Morpeh.World.Default.GetEvent<PlayerDeathEvent>();
		}

		private void Update()
		{
// Native ELF VA 0x15f85fc: only one animation is started for a nonempty event batch.
            foreach (PlayerDeathEvent entry in playerDeathEvent.BatchedChanges) { PlayAsync().Forget(); break; }
		}

		
		private async UniTask PlayAsync()
		{
// Native ELF VA 0x15f8768; vtable slot 22 is ResetToInitialState.
            canvasGroup.alpha = 1f;
            deathAnimation.ResetToInitialState();
            await deathAnimation.PlayAsync();
		}
	}
}
