using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.Flows;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Codebase.UI.Flows;
using Codebase.BossTower.Systems;

namespace Codebase.BossTower.Flows
{
	public class LeaveBossArenaFlowRunner : IFlowRunner<LeaveBossArenaFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CExecute_003Ed__1 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public LeaveBossArenaFlowRunner _003C_003E4__this;
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
		private IAudioPlayer audioPlayer;
		public async UniTask Execute(LeaveBossArenaFlow flow)
		{
			await App.InitiateFlow(new ShowCurtainFlow());
			App.ExecuteCommand(new ExitBossArenaCommand());
			await UniTask.WaitForSeconds(0.5f);
			await App.InitiateFlow(new HideCurtainFlow());
			audioPlayer.FadeMusic(false).Forget();
		}
	}
}
