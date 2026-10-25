using Codebase.Notifications.Flows;
using Codebase.Mail.Flows;
using Codebase.Hub;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Events.Analytics;
using Codebase.Events.Models;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;

namespace Codebase.Core.Flows
{
	public class AppStartupFlowRunner : IFlowRunner<AppStartupFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CExecute_003Ed__2 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public AppStartupFlowRunner _003C_003E4__this;

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
		private EventsRepository eventsRepository;

		[Inject]
		private EventAnalyticsModel eventAnalyticsModel;

		
		public async UniTask Execute(AppStartupFlow flow)
		{
			// Native ELF VA 0x154b038; actual async body MoveNext at 0x154b0d8 has no out-of-range pcode.
			await UniTask.NextFrame();
			eventAnalyticsModel.ReportOfflineSkipped(eventsRepository);
			await App.InitiateFlow(new SendMailFlow());
			await App.InitiateFlow(new HubInitFlow());
			await App.InitiateFlow(new NotificationStartupFlow());
		}
	}
}
