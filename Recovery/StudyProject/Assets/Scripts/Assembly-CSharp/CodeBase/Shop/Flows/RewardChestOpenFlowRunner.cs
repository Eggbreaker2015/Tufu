using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Core.UI;
using Codebase.Rewards;
using Codebase.UI.Shop;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;

namespace Codebase.Shop.Flows
{
	public class RewardChestOpenFlowRunner : IFlowRunner<RewardChestOpenFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CExecute_003Ed__3 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public RewardChestOpenFlow flow;
			public RewardChestOpenFlowRunner _003C_003E4__this;
			private int _003CrewardsRemaining_003E5__2;
			private ChestOpenView _003CchestOpenView_003E5__3;
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
		[Inject]
		private UINavigator navigator;
		[Inject]
		private ChestsConfig chestsConfig;
		public async UniTask Execute(RewardChestOpenFlow flow)
		{
			if (flow.Rewards == null || flow.Rewards.Count == 0)
				return;
			if (!chestsConfig.TryGet(flow.ChestKey, out var chest))
				return;
			int remaining = flow.Rewards.Count;
			var view = navigator.Open<ChestOpenView>("Assets/Prefabs/UI/ChestOpenView.prefab", false);
			audioPlayer.PlaySoundOnce("chest_buy");
			view.SetRewards(flow.Rewards);
			view.SetChestSprites(chest.ClosedSprite, chest.OpenedSprite);
			while (remaining > 0)
			{
				view.SetChestCount(remaining);
				view.SetActive(true);
				await view.WaitForTap();
				view.SetReward(flow.Rewards[flow.Rewards.Count - remaining]);
				audioPlayer.PlaySoundOnce("chest_riser");
				await view.WaitForTapOrAnimationEnd();
				await view.WaitForTap();
				remaining--;
				if (remaining > 0)
					view.ResetAnimation();
			}

			view.Close();
		}
	}
}
