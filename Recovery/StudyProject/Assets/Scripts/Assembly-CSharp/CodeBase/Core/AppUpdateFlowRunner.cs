using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Core.Flows;
using Codebase.Mail;
using Codebase.Player.Models;
using Codebase.Shop;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Codebase.Shop.Flows;
using Codebase.Gates;
using Codebase.Gates.Flows;

namespace Codebase.Core
{
	public struct AppUpdateFlowRunner : IFlowRunner<AppUpdateFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CCheckPortalUnlock_003Ed__4 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public AppUpdateFlowRunner _003C_003E4__this;
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
		private struct _003CExecute_003Ed__3 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public AppUpdateFlowRunner _003C_003E4__this;
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
		private struct _003CProcessChestQueue_003Ed__5 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public AppUpdateFlowRunner _003C_003E4__this;
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
		private ChestQueueModel chestQueue;
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private MailsRepository mailsRepository;
		public async UniTask Execute(AppUpdateFlow flow)
		{
			await ProcessChestQueue();
			await CheckPortalUnlock();
		}

		private async UniTask CheckPortalUnlock()
		{
			if (!mailsRepository.Data.PassedUpdate_2_0_0.Value || playerModel.IsUnlockAnimationShown || playerModel.IsPortalUnlockRunning || playerModel.PortalGemsCollected <= 0)
				return;
			if (playerModel.PortalGemsCollected < GameContext.Get<Portal>().GemsTotal)
				return;
			await App.InitiateFlow(new PortalUnlockFlow { FocusFullTime = true });
		}

		private async UniTask ProcessChestQueue()
		{
			if (!chestQueue.HasPendingChests || chestQueue.IsProcessing)
				return;
			chestQueue.SetProcessing(true);
			while (chestQueue.TryDequeue(out var entry))
			{
				if (entry.Type == ChestQueueEntryType.Chest)
				{
					if (entry.ChestDef.Key == "crystal")
						await App.InitiateFlow(new CrystalChestOpenFlow { ChestCount = entry.Count });
					else
						await App.InitiateFlow(new ChestOpenFlow { ChestCount = entry.Count, ChestDef = entry.ChestDef });
				}
				else if (entry.Type == ChestQueueEntryType.Reward)
					await App.InitiateFlow(new RewardChestOpenFlow { Rewards = entry.Rewards, ChestKey = entry.ChestKey });
			}

			chestQueue.SetProcessing(false);
			chestQueue.OnChestProcessingComplete();
		}
	}
}
