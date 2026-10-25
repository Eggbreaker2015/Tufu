using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Player.Models;
using Codebase.Quests.Components;
using Codebase.UI;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Scellecs.Morpeh;
using System;
using System.Collections.Generic;
using Codebase.Analytics;

namespace Codebase.Quests.Flows
{
	public class QuestCutsceneFlowRunner : IFlowRunner<QuestCutsceneFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CExecute_003Ed__4 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public QuestCutsceneFlowRunner _003C_003E4__this;
			public QuestCutsceneFlow flow;
			private QuestStepWorldComponent _003Cworld_003E5__2;
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
		private struct _003CPlayCutsceneSound_003Ed__6 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;
			public QuestCutsceneSound sound;
			public QuestCutsceneFlowRunner _003C_003E4__this;
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
		private PlayerModel playerModel;
		[Inject]
		private UiModel uiModel;
		[Inject]
		private IAudioPlayer audioPlayer;
		private Filter worldFilter;
		public async UniTask Execute(QuestCutsceneFlow flow)
		{ // Original ELF 0x14329c4; candidate only.
			if (!TryGetWorldComponent(flow.StepKey, out var world) || world.Cutscene == null)
				return;
			playerModel.CanMove = false;
			uiModel.IsHudVisible = false;
			uiModel.IsWorldUiVisible = false;
			try
			{
				var service = new QuestCutsceneService();
				QuestStepWorldApplier.HideDuringCutscene(world);
				if (world.CamerasRoot != null)
					world.CamerasRoot.SetActive(true);
				world.Cutscene.gameObject.SetActive(true);
				PlayCutsceneSounds(world);
				await service.Play(world.Cutscene);
				QuestCutsceneService.Release(world.Cutscene);
				world.Cutscene.gameObject.SetActive(false);
				if (world.CamerasRoot != null)
					world.CamerasRoot.SetActive(false);
				QuestStepWorldApplier.ShowAfterCutscene(world);
				QuestStepWorldApplier.Apply(world);
			}
			finally
			{
				playerModel.CanMove = true;
				uiModel.IsHudVisible = true;
				uiModel.IsWorldUiVisible = true;
			}
		}

		private void PlayCutsceneSounds(QuestStepWorldComponent world)
		{ // Original ELF 0x1432a7c; candidate only.
			foreach (var sound in world.CutsceneSounds)
				PlayCutsceneSound(sound).Forget();
		}

		private async UniTaskVoid PlayCutsceneSound(QuestCutsceneSound sound)
		{ // Original ELF 0x1432b00; candidate only.
			if (sound.Delay > 0f)
				await UniTask.Delay(TimeSpan.FromSeconds(sound.Delay));
			audioPlayer.PlayClipOnce(sound.Clip, 1f);
		}

		private bool TryGetWorldComponent(string stepKey, out QuestStepWorldComponent world)
		{ // Original ELF 0x1432bb4; candidate only.
			if (worldFilter == null)
				worldFilter = World.Default.Filter.With<QuestStepWorldComponent>().Build();
			var stash = World.Default.GetStash<QuestStepWorldComponent>();
			foreach (var entity in worldFilter)
				if (stash.Has(entity))
				{
					var candidate = stash.Get(entity);
					if (candidate.StepKey == stepKey)
					{
						world = candidate;
						return true;
					}
				}

			world = default;
			return false;
		}
	}
}
