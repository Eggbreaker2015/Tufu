using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeBase.Audio;
using Codebase.Camera;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Core.UI;
using Codebase.Player.Models;
using Codebase.Quests.Components;
using Codebase.UI;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Scellecs.Morpeh;
using UnityEngine;
using System;
using Codebase.Analytics;

namespace Codebase.Quests.Flows
{
	public class QuestDialogueFlowRunner : IFlowRunner<QuestDialogueFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CAnimateStoneTransition_003Ed__22 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public QuestStepWorldComponent world;
			private float _003Celapsed_003E5__2;
			private Cysharp.Threading.Tasks.YieldAwaitable.Awaiter _003C_003Eu__1;
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
		private struct _003CExecute_003Ed__14 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public QuestDialogueFlowRunner _003C_003E4__this;
			public QuestDialogueFlow flow;
			private QuestStepDef _003Cdef_003E5__2;
			private Sprite _003Cportrait_003E5__3;
			private QuestStepWorldComponent _003Cworld_003E5__4;
			private bool _003ChasScriptedSequence_003E5__5;
			private bool _003CuseCamera_003E5__6;
			private bool _003CfocusNextAfterPrologue_003E5__7;
			private UniTask.Awaiter _003C_003Eu__1;
			private QuestChainDef _003CprologueChain_003E5__8;
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
		private struct _003CPlayCutsceneSound_003Ed__20 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;
			public QuestCutsceneSound sound;
			public QuestDialogueFlowRunner _003C_003E4__this;
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
		private struct _003CPlayPostDialogueAndToggleWorld_003Ed__18 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public QuestDialogueFlowRunner _003C_003E4__this;
			public string stepKey;
			public QuestStepDef def;
			public Sprite portrait;
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
		private struct _003CPlayScriptedSequence_003Ed__21 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public QuestStepWorldComponent world;
			public QuestDialogueFlowRunner _003C_003E4__this;
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
		private const float ScriptedSequenceDuration = 4f;
		private const float StoneTransitionDuration = 1.5f;
		private const float PrologueFocusNextDelay = 0.5f;
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private UiModel uiModel;
		[Inject]
		private CameraFollowSystem cameraFollowSystem;
		[Inject]
		private UINavigator navigator;
		[Inject]
		private QuestModel questModel;
		[Inject]
		private QuestConfig config;
		[Inject]
		private IAudioPlayer audioPlayer;
		private Filter worldFilter;
		public async UniTask Execute(QuestDialogueFlow flow)
		{ // Original ELF 0x1433a3c; candidate only.
			var def = config.GetStep(flow.StepKey);
			var portrait = config.GetGiverOfStep(flow.StepKey).Portrait;
			bool scripted = TryGetWorldComponent(flow.StepKey, out var world) && world.HasScriptedSequence;
			bool focus = scripted || def.HasMonologue;
			playerModel.CanMove = false;
			uiModel.IsHudVisible = false;
			uiModel.IsWorldUiVisible = false;
			bool completedPrologue = false;
			try
			{
				if (focus)
				{
					if (scripted)
						cameraFollowSystem.FollowExactTransform(world.DialogueCameraTarget, 1.25f);
					else
						cameraFollowSystem.FollowAlongTargetForward(flow.CameraTarget, 1.75f, -26f, 1.25f);
					await UniTask.Delay(TimeSpan.FromSeconds(1.25));
					if (scripted)
						QuestScriptedSequenceAnimator.SetBool(world.ScriptedCharacters, QuestScriptedSequenceAnimatorParams.IsTalking, true);
				}

				await ShowDialogueIfNeeded(portrait, def);
				await PlayPostDialogueAndToggleWorld(flow.StepKey, portrait, def);
				questModel.MarkGiven(flow.StepKey);
				if (questModel.Prologue.IsPrologueStep(flow.StepKey))
				{
					App.ExecuteCommand(new Codebase.Quests.Commands.CompleteQuestStepCommand { StepKey = flow.StepKey });
					completedPrologue = true;
				}

				if (focus)
					cameraFollowSystem.Unfollow();
			}
			finally
			{
				playerModel.CanMove = true;
				uiModel.IsHudVisible = true;
				uiModel.IsWorldUiVisible = true;
			}

			if (completedPrologue)
			{
				var chain = config.GetChainOfStep(flow.StepKey);
				if (chain != null)
				{
					await UniTask.Delay(TimeSpan.FromSeconds(.5));
					App.ExecuteCommand(new Codebase.Quests.Commands.FocusNextQuestGiverCommand { CompletedChainKey = chain.ChainKey });
				}
			}
		}

		private UniTask ShowDialogueIfNeeded(Sprite portrait, QuestStepDef def)
		{ // Original ELF 0x1433b00; candidate only.
			return def.HasMonologue ? ShowDialogue(portrait, def.NpcNameKey, def.Monologue) : UniTask.CompletedTask;
		}

		private UniTask ShowPostCutsceneDialogueIfNeeded(Sprite portrait, QuestStepDef def)
		{ // Original ELF 0x1433d00; candidate only.
			return def.HasPostCutsceneMonologue ? ShowDialogue(portrait, def.NpcNameKey, def.PostCutsceneMonologue) : UniTask.CompletedTask;
		}

		private UniTask ShowDialogue(Sprite icon, string nameKey, List<QuestDialogueLine> lines)
		{ // Original ELF 0x1433b94; candidate only.
			var completion = new UniTaskCompletionSource();
			navigator.Open<Codebase.UI.Quests.QuestNpcDialogueView>("Assets/Prefabs/UI/Mines/Dialogue/DialogueWindow.prefab").Setup(icon, nameKey, lines, () => completion.TrySetResult());
			return completion.Task;
		}

		private async UniTask PlayPostDialogueAndToggleWorld(string stepKey, Sprite portrait, QuestStepDef def)
		{ // Original ELF 0x1433da0; candidate only.
			if (!TryGetWorldComponent(stepKey, out var world))
				return;
			if (def.HasCutscene && world.Cutscene != null)
			{
				var cutscenes = new QuestCutsceneService();
				QuestStepWorldApplier.HideDuringCutscene(world);
				if (world.CamerasRoot != null)
					world.CamerasRoot.SetActive(true);
				world.Cutscene.gameObject.SetActive(true);
				PlayCutsceneSounds(world);
				if (def.HasPostCutsceneMonologue)
				{
					await cutscenes.PlayAndHoldLastFrame(world.Cutscene);
					await ShowPostCutsceneDialogueIfNeeded(portrait, def);
				}
				else
					await cutscenes.Play(world.Cutscene);
				QuestCutsceneService.Release(world.Cutscene);
				world.Cutscene.gameObject.SetActive(false);
				if (world.CamerasRoot != null)
					world.CamerasRoot.SetActive(false);
				QuestStepWorldApplier.ShowAfterCutscene(world);
				QuestStepWorldApplier.Apply(world);
				return;
			}

			if (world.HasScriptedSequence)
			{
				PlayCutsceneSounds(world);
				await PlayScriptedSequence(world);
				QuestStepWorldApplier.Apply(world);
				QuestScriptedSequenceAnimator.SetBool(world.ScriptedCharacters, QuestScriptedSequenceAnimatorParams.IsStoned, true);
				return;
			}

			QuestStepWorldApplier.Apply(world);
		}

		private void PlayCutsceneSounds(QuestStepWorldComponent world)
		{ // Original ELF 0x1433e88; candidate only.
			foreach (var sound in world.CutsceneSounds)
				PlayCutsceneSound(sound).Forget();
		}

		private async UniTaskVoid PlayCutsceneSound(QuestCutsceneSound sound)
		{ // Original ELF 0x1433f0c; candidate only.
			if (sound.Delay > 0f)
				await UniTask.Delay(TimeSpan.FromSeconds(sound.Delay));
			audioPlayer.PlayClipOnce(sound.Clip, 1f);
		}

		private async UniTask PlayScriptedSequence(QuestStepWorldComponent world)
		{ // Original ELF 0x1433fc0; candidate only.
			QuestScriptedSequenceAnimator.SetBool(world.ScriptedCharacters, QuestScriptedSequenceAnimatorParams.IsTalking, false);
			QuestScriptedSequenceAnimator.SetBool(world.ScriptedCharacters, QuestScriptedSequenceAnimatorParams.IsStoned, true);
			foreach (var particle in world.ScriptedParticles)
				particle.Play();
			await UniTask.WhenAll(AnimateStoneTransition(world), UniTask.Delay(TimeSpan.FromSeconds(4)));
		}

		private async UniTask AnimateStoneTransition(QuestStepWorldComponent world)
		{ // Original ELF 0x1434084; candidate only.
			float elapsed = 0f;
			QuestScriptedSequenceAnimator.SetFloat(world.StoneTransitionAnimator, QuestScriptedSequenceAnimatorParams.Speed, 1f);
			foreach (var renderer in world.StoneTransitionRenderers)
				QuestScriptedSequenceAnimator.SetStoneTransitionProgress(renderer, .3f);
			while (elapsed < 1.5f)
			{
				elapsed += Time.deltaTime;
				float progress = Mathf.Clamp01(elapsed / 1.5f);
				float stone = Mathf.Lerp(.3f, 1f, progress);
				foreach (var renderer in world.StoneTransitionRenderers)
					QuestScriptedSequenceAnimator.SetStoneTransitionProgress(renderer, stone);
				QuestScriptedSequenceAnimator.SetFloat(world.StoneTransitionAnimator, QuestScriptedSequenceAnimatorParams.Speed, 1f - progress);
				await UniTask.Yield();
			}

			foreach (var renderer in world.StoneTransitionRenderers)
				QuestScriptedSequenceAnimator.SetStoneTransitionProgress(renderer, 1f);
			QuestScriptedSequenceAnimator.SetFloat(world.StoneTransitionAnimator, QuestScriptedSequenceAnimatorParams.Speed, 0f);
		}

		private bool TryGetWorldComponent(string stepKey, out QuestStepWorldComponent world)
		{ // Original ELF 0x143412c; candidate only.
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
