namespace Codebase.Quests
{
	public class QuestStepFlowResolver
	{
		private readonly QuestConfig config;
		public QuestStepFlowResolver(QuestConfig config)
		{
			this.config = config;
		}

		public bool IsChainFinalStep(QuestStepDef step)
		{ // Original ELF 0x142ce04; independent candidate only.
			if (step == null)
				return false;
			var chain = config.GetChainOfStep(step.StepKey);
			return chain != null && chain.Steps.Count > 0 && chain.Steps[chain.Steps.Count - 1].StepKey == step.StepKey;
		}

		public bool ShouldPlayFinaleOnChainComplete(QuestStepDef step)
		{ // Original ELF 0x142cec0; independent candidate only.
			var chain = config.GetChainOfStep(step.StepKey);
			return chain != null && !chain.IsPrologue && IsChainFinalStep(step) && (step.HasCutscene || step.HasMonologue || step.HasPostCutsceneMonologue);
		}

		public bool ShouldAutoStartStep(QuestStepDef step)
		{ // Original ELF 0x142cf44; independent candidate only.
			return ShouldPlayFinaleOnChainComplete(step) || (!step.HasMonologue && !step.HasCutscene && !step.HasPostCutsceneMonologue);
		}

		public bool RequiresDialogueFlowOnApproach(QuestStepDef step, QuestChainDef chain)
		{ // Original ELF 0x142cf98; independent candidate only.
			if (ShouldPlayFinaleOnChainComplete(step))
				return false;
			return (QuestStepWorldApplier.TryGetWorld(step.StepKey, out var world) && world.HasScriptedSequence) || step.HasCutscene || (chain.IsPrologue && step.HasMonologue);
		}
	}
}
