using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Player.Models;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;

namespace Codebase.Hub
{
	public class HubUpdateAttributesFlowRunner : IFlowRunner<HubUpdateAttributesFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CExecute_003Ed__3 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public HubUpdateAttributesFlowRunner _003C_003E4__this;

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
		private UserModel _userModel;

		[Inject]
		private PlayerModel _playerModel;

		public static void CollectDeviceIdentifiers()
		{
		}

		[AsyncStateMachine(typeof(_003CExecute_003Ed__3))]
		public UniTask Execute(HubUpdateAttributesFlow flow)
		{
			return default;
		}
	}
}
