using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.Flows;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;

namespace Codebase.UI.Flows
{
	public class HideCurtainFlowRunner : IFlowRunner<HideCurtainFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CExecute_003Ed__2 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public HideCurtainFlow flow;

			public HideCurtainFlowRunner _003C_003E4__this;

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
		private CurtainView curtainView;

		[Inject]
		private IAudioPlayer audioPlayer;

		
		public async UniTask Execute(HideCurtainFlow flow)
		{
			// Native MoveNext ELF VA 0x1657370.
			if (!flow.disableSound) audioPlayer.PlaySoundOnce("transition_close");
			await curtainView.PlayHide();
		}
	}
}
