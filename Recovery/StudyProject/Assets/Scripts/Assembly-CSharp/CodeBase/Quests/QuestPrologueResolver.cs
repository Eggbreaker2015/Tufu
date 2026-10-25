using Codebase.Quests.Components;

namespace Codebase.Quests
{
	public class QuestPrologueResolver
	{
		private readonly QuestModel model;

		private readonly QuestConfig config;

		public QuestPrologueResolver(QuestModel model, QuestConfig config)
		{
			this.model = model; this.config = config;
		}

		public bool IsPrologueChain(QuestChainDef chain)
		{ // Original ELF 0x142b36c; candidate only.
			return chain.IsPrologue;
		}

		public bool IsPrologueStep(string stepKey)
		{ // Original ELF 0x142b380; candidate only.
			var chain = config.GetChainOfStep(stepKey);
			return chain != null && chain.IsPrologue;
		}

		public bool IsZeroQuestNpc(QuestNpcComponent npc)
		{
			// Original relocated literals0x2b1a448="qg_0",0x2b1a440="quest_0".
			if (npc.GiverKey == "qg_0") return true;
			if (string.IsNullOrEmpty(npc.StepKey)) return false;
			var chain = config.GetChainOfStep(npc.StepKey);
			return chain != null && chain.IsPrologue;
		}

		public bool TryGetPrologueApproachChain(QuestNpcComponent npc, out QuestChainDef chain, out string stepKey)
		{
			chain = null; stepKey = null;
			if (!IsZeroQuestNpc(npc)) return false;
			chain = config.GetChain("quest_0"); if (chain == null) return false;
			stepKey = string.IsNullOrEmpty(npc.StepKey) ? model.Chain.GetActiveStepKey(chain) : npc.StepKey;
			return stepKey != null && !model.IsStepGiven(stepKey) && !model.Chain.IsChainFullyComplete(chain);
		}

		public bool CanApproachPrologueNpc(QuestNpcComponent npc)
		{ // Original ELF 0x142b3b0; candidate only.
			return TryGetPrologueApproachChain(npc, out var chain, out var stepKey);
		}

		public void EnsurePrologueWorldState()
		{ // Original ELF 0x142b3e8; candidate only.
			var chain = config.GetChain("quest_0");
			if (chain == null)
				return;
			if (!model.Chain.IsChainFullyComplete(chain) && QuestStepWorldApplier.TryGetWorld("quest_c0_step_1", out var world))
				QuestWorldStateResetter.ApplyInitialState(world);
		}
	}
}
