using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Mail;
using Codebase.Services.Time;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Multicast.RewardsInbox;
using UnityEngine;

namespace Codebase.Hub
{
	public class HubCheckAndClaimRewardsFlowRunner : IFlowRunner<HubCheckAndClaimRewardsFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CExecute_003Ed__3 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public HubCheckAndClaimRewardsFlowRunner _003C_003E4__this;

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
		private struct _003CMarkClaimedRewards_003Ed__5 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public HubCheckAndClaimRewardsFlowRunner _003C_003E4__this;

			private IEnumerator<InboxReward> _003C_003E7__wrap1;

			private InboxReward _003Creward_003E5__3;

			private Awaitable.Awaiter _003C_003Eu__1;

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
		private MailsRepository mailsRepository;

		[Inject]
		private ITimeService timeService;

		[Inject]
		private IRewardsInboxService rewardsInboxService;

		[AsyncStateMachine(typeof(_003CExecute_003Ed__3))]
		public UniTask Execute(HubCheckAndClaimRewardsFlow flow)
		{
			return default;
		}

		private void CreateMailsForRewards()
		{
		}

		[AsyncStateMachine(typeof(_003CMarkClaimedRewards_003Ed__5))]
		private UniTask MarkClaimedRewards()
		{
			return default;
		}
	}
}
