using System;
using System.Runtime.InteropServices;
using Codebase.Core;
using Codebase.Quests.Components;
using Scellecs.Morpeh;

namespace Codebase.Quests.Systems
{
	public class QuestStateRestoreSystem : SystemBase
	{
		[Serializable]
		[StructLayout((LayoutKind)0, Size = 1)]
		private struct QuestWorldRestoreState : ISystemStateComponent, IComponent
		{
		}

		private SystemStateProcessor<QuestWorldRestoreState> processor;
		[Inject]
		private Stash<QuestStepWorldComponent> worldStash;
		[Inject]
		private QuestModel questModel;
		[Inject]
		private QuestConfig config;
		public override void OnAwake()
		{ // Original ELF 0x143258c; candidate only.
			processor = Filter.With<QuestStepWorldComponent>().ToSystemStateProcessor<QuestWorldRestoreState>(Create, Remove);
		}

		public override void Dispose()
		{ // Original ELF 0x1432718; candidate only.
			processor.Dispose();
		}

		public override void OnUpdate(float deltaTime)
		{ // Original ELF 0x1432760; candidate only.
			processor.Process();
		}

		private QuestWorldRestoreState Create(Entity entity)
		{ // Original ELF 0x14327a8; candidate only.
			var world = worldStash.Get(entity);
			if (questModel.IsStepGiven(world.StepKey))
			{
				foreach (var item in world.ObjectsToEnable)
					item.SetActive(true);
				if (ShouldDisableWorldObjects(world.StepKey))
					foreach (var item in world.ObjectsToDisable)
						item.SetActive(false);
				if (world.HasScriptedSequence)
					QuestScriptedSequenceAnimator.ApplyStonedState(world.ScriptedCharacters, world.StoneTransitionRenderers, world.StoneTransitionAnimator);
			}

			return default;
		}

		private bool ShouldDisableWorldObjects(string stepKey)
		{ // Original ELF 0x143292c; candidate only.
			var step = config.GetStep(stepKey);
			if (step != null && questModel.StepFlow.ShouldPlayFinaleOnChainComplete(step))
			{
				var chain = config.GetChainOfStep(stepKey);
				return chain != null && questModel.IsChainRewardClaimed(chain.ChainKey);
			}

			return true;
		}

		private void Remove(ref QuestWorldRestoreState state)
		{
		}
	}
}
