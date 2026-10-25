using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Core.UI;
using Codebase.UI.Inventory;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;

namespace Codebase.Player.Flows
{
	public class EvolveItemFlowRunner : IFlowRunner<EvolveItemFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CExecute_003Ed__2 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public EvolveItemFlowRunner _003C_003E4__this;

			public EvolveItemFlow flow;

			private EvolveItemView _003CevolveItemView_003E5__2;

			private SoundHandle _003CsoundHandle_003E5__3;

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
		private UINavigator navigator;

		[Inject]
		private IAudioPlayer audioPlayer;

		public async UniTask Execute(EvolveItemFlow flow)
		{
			// Native state machine 0x14668f8; strings resolved from original ELF RELA.
			audioPlayer.FadeMusic(true);
			EvolveItemView view = navigator.Open<EvolveItemView>("Assets/Prefabs/UI/EvolveItemView.prefab");
			view.SetSourceItem(flow.OldItem);
			view.SetTargetItem(flow.NewItem);
			view.Setup(flow.OldItem, false);
			view.SetActive(true);
			SoundHandle sound = audioPlayer.PlaySoundOnceInterruptible("item_first_evolve");
			await view.WaitForAnimationOrSkip(!flow.IsFirstEvolve);
			if (sound != null) sound.Stop();
			view.ShowDiff();
			await view.WaitForTap();
			audioPlayer.FadeMusic(false);
			view.Close();
		}
	}
}
