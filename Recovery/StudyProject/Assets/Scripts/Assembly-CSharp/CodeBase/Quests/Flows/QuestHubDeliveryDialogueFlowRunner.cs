using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Camera;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Core.UI;
using Codebase.Player.Models;
using Codebase.UI;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using UnityEngine;
using System;
using Codebase.Analytics;

namespace Codebase.Quests.Flows
{
	public class QuestHubDeliveryDialogueFlowRunner : IFlowRunner<QuestHubDeliveryDialogueFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CExecute_003Ed__8 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public QuestHubDeliveryDialogueFlow flow;
			public QuestHubDeliveryDialogueFlowRunner _003C_003E4__this;
			private QuestHubTokenSlot _003Cslot_003E5__2;
			private QuestGiverDef _003Cgiver_003E5__3;
			private Transform _003Ctarget_003E5__4;
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

		private const float CameraDistance = 1.75f;
		private const float CameraHeight = -26f;
		private const float CameraFocusDelay = 1.25f;
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private UiModel uiModel;
		[Inject]
		private CameraFollowSystem cameraFollowSystem;
		[Inject]
		private UINavigator navigator;
		[Inject]
		private QuestConfig config;
		public async UniTask Execute(QuestHubDeliveryDialogueFlow flow)
		{ // Original ELF 0x1437220; candidate only.
			var chain = config.GetChain(flow.ChainKey);
			if (chain == null)
				return;
			QuestHubTokenSlot slot = null;
			foreach (var candidate in chain.HubTokenSlots)
				if (candidate.TokenKey == flow.TokenKey)
				{
					slot = candidate;
					break;
				}

			if (slot == null || slot.DeliveryMonologue == null || slot.DeliveryMonologue.Count == 0)
				return;
			var giver = config.GetGiverByHubChain(flow.ChainKey);
			var target = QuestNpcLocator.FindForChain(chain);
			playerModel.CanMove = false;
			uiModel.IsHudVisible = false;
			uiModel.IsWorldUiVisible = false;
			try
			{
				if (target != null)
				{
					cameraFollowSystem.FollowAlongTargetForward(target, 1.75f, -26f, 1.25f);
					await UniTask.Delay(TimeSpan.FromSeconds(1.25));
				}

				await ShowDialogue(giver.Portrait, slot.DeliveryNpcNameKey, slot.DeliveryMonologue);
				if (target != null)
					cameraFollowSystem.Unfollow();
			}
			finally
			{
				playerModel.CanMove = true;
				uiModel.IsHudVisible = true;
				uiModel.IsWorldUiVisible = true;
			}
		}

		private UniTask ShowDialogue(Sprite icon, string nameKey, List<QuestDialogueLine> lines)
		{ // Original ELF 0x14372e0; candidate only.
			var completion = new UniTaskCompletionSource();
			navigator.Open<Codebase.UI.Quests.QuestNpcDialogueView>("Assets/Prefabs/UI/Mines/Dialogue/DialogueWindow.prefab").Setup(icon, nameKey, lines, () => completion.TrySetResult());
			return completion.Task;
		}
	}
}
