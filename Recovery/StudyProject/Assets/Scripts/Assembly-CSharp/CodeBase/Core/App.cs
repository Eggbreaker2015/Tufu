using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Core.Commands;
using Codebase.Core.Flows;
using Codebase.Core.UserData;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using JetBrains.Annotations;
using UniMob;

namespace Codebase.Core
{
	public static class App
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CInitiateFlow_003Ed__11<TFlow> : IAsyncStateMachine where TFlow : struct, IFlow
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public bool checkSingle;

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

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CInitiateFlow_003Ed__12<TFlow, TResult> : IAsyncStateMachine where TFlow : struct, IFlow<TResult>
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder<TResult> _003C_003Et__builder;

			public bool checkSingle;

			public TResult resultDefault;

			public TFlow flow;

			private UniTask<TResult>.Awaiter _003C_003Eu__1;

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
		private struct _003CUpdate_003Ed__14 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public LifetimeController lc;

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

		private static CommandFactory commandFactory;

		private static GameDataService gameDataService;

		private static FlowFactory handlerFactory;

		private static bool appUpdateFlowRequested;

		[CanBeNull]
		private static IFlow currentActiveFlow;

		private static readonly Stack<IFlowHandlerBase> RunningFlows = new Stack<IFlowHandlerBase>();

        [StaticReset]
        private static void ResetStaticState()
        {
            handlerFactory = null;
            currentActiveFlow = null;
            appUpdateFlowRequested = false;
            RunningFlows.Clear();
            commandFactory = null;
            gameDataService = null;
        }

        public static void ExecuteCommand<TCommand>(TCommand command) where TCommand : struct, ICommand
        {
            if (commandFactory == null) commandFactory = GameContext.Get<CommandFactory>();
            if (gameDataService == null) gameDataService = GameContext.Get<GameDataService>();
            commandFactory.Create<TCommand>().Handler.Execute(command);
        }

        public static TResult ExecuteCommand<TCommand, TResult>(TCommand command) where TCommand : struct, ICommand<TResult>
        {
            if (commandFactory == null) commandFactory = GameContext.Get<CommandFactory>();
            if (gameDataService == null) gameDataService = GameContext.Get<GameDataService>();
            return commandFactory.Create<TCommand, TResult>().Handler.Execute(command);
        }

        public static void Startup(LifetimeController lifetimeController) => Update(lifetimeController).Forget();

        public static void RequestAppUpdateFlow() => appUpdateFlowRequested = true;

        public static async UniTask InitiateFlow<TFlow>(TFlow flow, bool checkSingle = false) where TFlow : struct, IFlow
        {
            if (!AllowFlowInitiate(checkSingle))
            {
                UnityEngine.Debug.LogWarning("Another flow already initiated");
                return;
            }
            if (handlerFactory == null) handlerFactory = GameContext.Get<FlowFactory>();
            var handler = handlerFactory.Create<TFlow>();
            RunningFlows.Push(handler);
            await handler.Execute(flow);
            RunningFlows.Pop();
        }

        public static async UniTask<TResult> InitiateFlow<TFlow, TResult>(TFlow flow, TResult resultDefault, bool checkSingle = false) where TFlow : struct, IFlow<TResult>
        {
            if (!AllowFlowInitiate(checkSingle))
            {
                UnityEngine.Debug.LogWarning("Another flow already initiated");
                return resultDefault;
            }
            if (handlerFactory == null) handlerFactory = GameContext.Get<FlowFactory>();
            var handler = handlerFactory.Create<TFlow, TResult>();
            RunningFlows.Push(handler);
            var result = await handler.Execute(flow);
            RunningFlows.Pop();
            return result;
        }

        private static bool AllowFlowInitiate(bool checkSingle) => RunningFlows.Count < 1 || !checkSingle;

        private static async UniTask Update(LifetimeController lc)
        {
            while (!lc.IsDisposed)
            {
                if (appUpdateFlowRequested)
                {
                    await InitiateFlow(new AppUpdateFlow());
                    appUpdateFlowRequested = false;
                }
                await UniTask.NextFrame();
            }
        }
    }
}
