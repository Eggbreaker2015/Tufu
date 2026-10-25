using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Codebase.Core;
using Codebase.Quests.Flows;

namespace Codebase.Quests
{
	public static class QuestFlowLauncher
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CFocusQuestGiverAfterKillAsync_003Ed__2 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;
			public QuestConfig config;
			public string stepKey;
			private UniTask.Awaiter _003C_003Eu__1;
			private Cysharp.Threading.Tasks.YieldAwaitable.Awaiter _003C_003Eu__2;
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

		private const float FocusQuestGiverDelaySeconds = 1.5f;
		public static void FocusQuestGiverAfterKill(QuestConfig config, string stepKey)
		{ // Original ELF 0x142313c; candidate only.
			FocusQuestGiverAfterKillAsync(config, stepKey).Forget();
		}

		private static async UniTaskVoid FocusQuestGiverAfterKillAsync(QuestConfig config, string stepKey)
		{ // Original ELF 0x1423160; candidate only.
			await UniTask.Delay(System.TimeSpan.FromSeconds(FocusQuestGiverDelaySeconds));
			await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
			var chain = config.GetChainOfStep(stepKey);
			if (chain == null || chain.IsPrologue || chain.Steps.Count == 0)
				return;
			var target = QuestNpcLocator.FindForChain(chain);
			if (target == null)
				return;
			await App.InitiateFlow(new QuestMobSpawnedFlow { TargetTransform = target });
		}
	}
}
