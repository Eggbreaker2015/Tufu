using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Integrations.AppMetrica;
using Codebase.Player.Models;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Multicast;
using Multicast.Payments;
using UnityEngine;

namespace Codebase.Hub
{
	public class HubAuthFlowRunner : IFlowRunner<HubAuthFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CExecute_003Ed__4 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public HubAuthFlowRunner _003C_003E4__this;

			private Disposer _003Cdisposer_003E5__2;

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
		private PlayerModel playerModel;

		[Inject]
		private AppMetricaAnalyticsWrapper analytics;

		[Inject]
		private UserModel userModel;

		[Inject]
		private IPaymentsService paymentsService;

		[AsyncStateMachine(typeof(_003CExecute_003Ed__4))]
		public UniTask Execute(HubAuthFlow flow)
		{
			return default;
		}
	}
}
