using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Core.UI;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;

namespace Codebase.UI.Flows
{
	public class ItemSlotUnlockFlowRunner : IFlowRunner<ItemSlotUnlockFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CExecute_003Ed__2 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public ItemSlotUnlockFlowRunner _003C_003E4__this;

			public ItemSlotUnlockFlow flow;

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
		private UiModel uiModel;

		public async UniTask Execute(ItemSlotUnlockFlow flow)
		{
            // Original ELF VA 0x1657a70 / MoveNext 0x1657b50. Do not grant slots or alter player movement here.
            if (uiModel.IsItemSlotUnlockShowing)
                await UniTask.WaitUntil(() => !uiModel.IsItemSlotUnlockShowing, PlayerLoopTiming.Update);
            uiModel.IsItemSlotUnlockShowing = true;
            try
            {
                var view = navigator.Open<global::Codebase.UI.ItemSlotUnlockView>("Assets/Prefabs/UI/ItemSlotUnlockView.prefab", false);
                view.Setup(flow.SlotType, flow.Index, flow.IsVip);
                await view.WaitForContinue();
            }
            finally
            {
                uiModel.IsItemSlotUnlockShowing = false;
            }
		}
	}
}
