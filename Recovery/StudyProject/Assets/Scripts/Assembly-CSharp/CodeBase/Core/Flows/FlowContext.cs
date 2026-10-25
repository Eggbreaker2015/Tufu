using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;

namespace Codebase.Core.Flows
{
	public class FlowContext<TFlow>
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CInitiateFlow_003Ed__2 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public FlowContext<TFlow> _003C_003E4__this;

			public TFlow flow;

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

		private readonly Func<TFlow, bool, UniTask> initiateFlowInternal;

        public FlowContext(Func<TFlow, bool, UniTask> initiateFlowInternal)
        {
            this.initiateFlowInternal = initiateFlowInternal;
        }

        public async UniTask InitiateFlow(TFlow flow)
        {
            await initiateFlowInternal(flow, true);
        }
    }
}
