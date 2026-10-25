using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player.Models;
using Codebase.Quests.Components;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Quests.Systems
{
	public class QuestNpcProximitySystem : SystemBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CAutoTakeQuest_003Ed__15 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;

			public QuestNpcProximitySystem _003C_003E4__this;

			public string chainKey;

			public string stepKey;

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
		private struct _003CTriggerDialogue_003Ed__16 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;

			public QuestNpcProximitySystem _003C_003E4__this;

			public string stepKey;

			public Transform cameraTarget;

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

		private const float UpdateInterval = 0.15f;

		[Inject]
		private Stash<QuestNpcComponent> npcStash;

		[Inject]
		private QuestModel questModel;

		[Inject]
		private QuestConfig config;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private UINavigator navigator;

		private Filter filter;

		private float updateTimer;

		private bool dialogueInProgress;

		public override void OnAwake()
		{
			// Original ELF 0x142f334.
			filter = Filter.With<QuestNpcComponent>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			// Original ELF 0x142f3e8.
			if (dialogueInProgress) return;
			updateTimer += deltaTime;
			if (updateTimer < UpdateInterval) return;
			updateTimer = 0f;
			var player = playerModel.Unit;
			if (player == null) return;
			var playerPosition = player.transform.position;
			foreach (var entity in filter) {
			    var npc = npcStash.Get(entity);
			    if (!TryResolveProximityTarget(npc, out var target)) continue;
			    if (!questModel.Prologue.TryGetPrologueApproachChain(npc, out var chain, out var activeStepKey)) {
			        if (navigator.HasActivePopups) continue;
			        chain = questModel.Popup.GetActiveChainForNpc(npc);
			        if (chain == null || chain.Role == QuestChainRole.Hub || chain.Steps.Count == 0) continue;
			        activeStepKey = questModel.Chain.GetActiveStepKey(chain);
			        if (activeStepKey == null || questModel.IsStepGiven(activeStepKey)) continue;
			    }
			    if (TryHandleApproach(target, playerPosition, npc, chain, activeStepKey)) return;
			}
		}

		private bool TryHandleApproach(Transform proximityTarget, Vector3 playerPosition, QuestNpcComponent npc, QuestChainDef chain, string activeStepKey)
		{
			// Original ELF 0x142f874.
			var stepDef = config.GetStep(activeStepKey);
			if (stepDef == null) return false;
			// Native FMUL compares a rounded Single; Mono may retain a wider evaluation stack.
			float triggerDistanceSq = System.BitConverter.Int32BitsToSingle(System.BitConverter.SingleToInt32Bits(npc.TriggerDistance * npc.TriggerDistance));
			if (GetHorizontalDistanceSq(playerPosition, proximityTarget.position) > triggerDistanceSq) return false;
			var firstStep = chain.Steps[0];
			if (activeStepKey == firstStep.StepKey && !questModel.Chain.IsChainStarted(chain) && ShouldAutoTakeOnApproach(stepDef, chain)) {
			    AutoTakeQuest(activeStepKey, chain.ChainKey).Forget();
			    return true;
			}
			if (!questModel.StepFlow.RequiresDialogueFlowOnApproach(stepDef, chain)) return false;
			if (activeStepKey == firstStep.StepKey && !chain.IsPrologue && !questModel.Chain.IsChainStarted(chain))
			    questModel.HoldQuestRoadUntilPopup(chain.ChainKey);
			TriggerDialogue(proximityTarget, activeStepKey).Forget();
			return true;
		}

		private bool TryResolveProximityTarget(QuestNpcComponent npc, out Transform target)
		{
			// Original ELF 0x142f6f8.
			target = npc.Transform;
			if (target == null) return false;
			if (target.gameObject.activeInHierarchy) return true;
			if (questModel.Prologue.TryGetPrologueApproachChain(npc, out var chain, out var stepKey)) return true;
			if (questModel.Prologue.IsZeroQuestNpc(npc)) {
			    target = QuestNpcLocator.FindTransform("quest_c0_step_1");
			    if (target != null) return true;
			}
			return false;
		}

		private bool ShouldAutoTakeOnApproach(QuestStepDef stepDef, QuestChainDef chain)
		{
			// Original ELF 0x142fa78.
			if (questModel.StepFlow.ShouldPlayFinaleOnChainComplete(stepDef)) return false;
			if (HasScriptedSequence(stepDef.StepKey) || stepDef.HasCutscene) return false;
			return !chain.IsPrologue || !stepDef.HasMonologue;
		}

		private bool HasScriptedSequence(string stepKey)
		{
			// Original ELF 0x142fca0.
			return QuestStepWorldApplier.TryGetWorld(stepKey, out var world) && world.HasScriptedSequence;
		}
		private async UniTaskVoid AutoTakeQuest(string stepKey, string chainKey)
		{
			// Original ELF 0x142fb1c, 0x142fce8, 0x143005c.
			dialogueInProgress = true;
			questModel.HoldQuestRoadUntilPopup(chainKey);
			questModel.MarkGiven(stepKey);
			QuestStepWorldApplier.Apply(stepKey);
			dialogueInProgress = false;
			await UniTask.CompletedTask;
		}
		private async UniTaskVoid TriggerDialogue(Transform cameraTarget, string stepKey)
		{
			// Original ELF 0x142fbe0, 0x1430068, 0x14304fc.
			dialogueInProgress = true;
			await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
			await App.InitiateFlow(new Codebase.Quests.Flows.QuestDialogueFlow { StepKey = stepKey, CameraTarget = cameraTarget });
			dialogueInProgress = false;
		}

		private static float GetHorizontalDistanceSq(Vector3 a, Vector3 b)
		{
			// Original ELF 0x142fa60.
			// Preserve each original FSUB/FMUL/FADD Single rounding on Editor Mono too.
			float x = System.BitConverter.Int32BitsToSingle(System.BitConverter.SingleToInt32Bits(a.x - b.x));
			float z = System.BitConverter.Int32BitsToSingle(System.BitConverter.SingleToInt32Bits(a.z - b.z));
			float xx = System.BitConverter.Int32BitsToSingle(System.BitConverter.SingleToInt32Bits(x * x));
			float zz = System.BitConverter.Int32BitsToSingle(System.BitConverter.SingleToInt32Bits(z * z));
			return System.BitConverter.Int32BitsToSingle(System.BitConverter.SingleToInt32Bits(xx + zz));
		}
	}
}
