using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Core;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Scellecs.Morpeh;

namespace Codebase.Quests.Systems
{
	public class QuestSpawnerControlSystem : SystemBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CFocusAvailableQuestGiverAsync_003Ed__6 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;
			public QuestSpawnerControlSystem _003C_003E4__this;
			private Cysharp.Threading.Tasks.YieldAwaitable.Awaiter _003C_003Eu__1;
			private UniTask.Awaiter _003C_003Eu__2;
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
		private QuestModel questModel;
		[Inject]
		private QuestConfig questConfig;
		[Inject]
		private QuestMobSpawnerService questMobSpawnerService;
		private bool initialSpawnPassComplete;
		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{ // Original ELF 0x1431b44; independent candidate only.
			bool firstPass = !initialSpawnPassComplete;
			bool focused = false;
			foreach (var chain in questConfig.Chains)
				foreach (var step in chain.Steps)
				{
					if (step.Prefab == null)
						continue;
					bool shouldSpawn = questModel.IsStepGiven(step.StepKey) && !questModel.IsStepComplete(step.StepKey) && questModel.ShouldSpawnQuestMob(step.StepKey);
					bool requestFocus = firstPass && shouldSpawn;
					questMobSpawnerService.SyncStep(step.StepKey, step, shouldSpawn, requestFocus && !focused);
					focused |= requestFocus;
				}

			if (firstPass && !focused)
				FocusAvailableQuestGiverAsync().Forget();
			initialSpawnPassComplete = true;
		}

		private async UniTaskVoid FocusAvailableQuestGiverAsync()
		{ // Original ELF 0x143201c; independent candidate only.
			await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
			var chain = questModel.Chain.GetFirstAvailableUnstartedChain();
			if (chain == null)
				return;
			var target = QuestNpcLocator.FindForChain(chain);
			if (target == null)
				return;
			await App.InitiateFlow(new Codebase.Quests.Flows.QuestMobSpawnedFlow { TargetTransform = target });
		}
	}
}
