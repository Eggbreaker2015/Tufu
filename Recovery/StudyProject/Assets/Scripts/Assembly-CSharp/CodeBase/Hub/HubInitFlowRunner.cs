using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Services.Purchases;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Multicast;
using Multicast.Payments;
using Multicast.RewardsInbox;

namespace Codebase.Hub
{
	public class HubInitFlowRunner : IFlowRunner<HubInitFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CExecute_003Ed__3 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public HubInitFlowRunner _003C_003E4__this;

			private Disposer _003Cdisposer_003E5__2;

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
		private IRewardsInboxService rewardsInboxService;

		[Inject]
		private IPaymentsService paymentsService;

		[Inject]
		private PurchaseProcessingService purchaseProcessingService;

		[AsyncStateMachine(typeof(_003CExecute_003Ed__3))]
		public UniTask Execute(HubInitFlow flow)
		{
			return default;
		}
	}
}
