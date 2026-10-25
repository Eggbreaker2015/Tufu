using Codebase.Quests.Components;

namespace Codebase.Quests
{
	public class QuestPopupResolver
	{
		private readonly QuestModel model;
		private readonly QuestConfig config;
		public QuestPopupResolver(QuestModel model, QuestConfig config)
		{
			this.model = model;
			this.config = config;
		}

		public QuestChainDef GetActiveChainForGiver(string giverKey)
		{
			var giver = config.GetGiver(giverKey);
			if (giver == null)
				return null;
			if (IsSideChainInProgress(giver))
				return config.GetChain(giver.SideChainKey);
			if (!string.IsNullOrEmpty(giver.HubChainKey))
			{
				var hub = config.GetChain(giver.HubChainKey);
				if (hub != null && model.Chain.IsChainUnlocked(hub) && !model.Chain.IsChainFullyComplete(hub))
					return hub;
			}

			return null;
		}

		public QuestChainDef GetActiveChainForNpc(QuestNpcComponent npc)
		{
			if (model.Prologue.TryGetPrologueApproachChain(npc, out var chain, out var stepKey))
				return chain;
			return GetActiveNonPrologueChainForNpc(npc);
		}

		public bool ShouldShowQuestPopup(QuestChainDef chain)
		{
			if (chain.IsPrologue)
				return false;
			if (chain.Role == QuestChainRole.Hub)
				return model.Chain.IsChainUnlocked(chain) && !IsSideChainInProgress(config.GetGiverByHubChain(chain.ChainKey)) && !model.Chain.IsChainFullyComplete(chain);
			if (!model.Chain.IsChainStarted(chain))
				return false;
			return model.Chain.GetActiveStepKey(chain) != null || model.Chain.HasUncollectedStepReward(chain) || !model.IsChainRewardClaimed(chain.ChainKey);
		}

		public bool TryGetQuestPopupChain(QuestNpcComponent npc, out QuestChainDef chain, out QuestPopupDisplayMode mode)
		{ // Original ELF 0x142aa88; independent candidate only.
			if (model.Prologue.IsZeroQuestNpc(npc))
			{
				chain = GetSideChainForObjectivePlaque();
				mode = QuestPopupDisplayMode.Plaque;
				return chain != null;
			}

			chain = GetActiveNonPrologueChainForNpc(npc);
			mode = QuestPopupDisplayMode.Button;
			if (chain == null || chain.IsPrologue || !ShouldShowQuestPopup(chain))
			{
				chain = null;
				return false;
			}

			return true;
		}

		public string GetObjectivePlaqueStepKey(QuestChainDef chain)
		{ // Original ELF 0x142ae8c; independent candidate only.
			string hunt = model.Chain.GetActiveHuntStepKey(chain);
			if (hunt != null)
				return hunt;
			foreach (var step in chain.Steps)
				if (model.IsStepComplete(step.StepKey) && !model.IsStepRewardClaimed(step.StepKey))
					return step.StepKey;
			return model.Chain.IsChainStarted(chain) ? null : model.Chain.GetActiveStepKey(chain);
		}

		public QuestChainDef GetSideChainForObjectivePlaque()
		{ // Original ELF 0x142ac30; independent candidate only.
			foreach (var chain in config.Chains)
				if (!chain.IsPrologue && chain.Role != QuestChainRole.Hub && model.Chain.IsChainUnlocked(chain) && !model.Chain.IsChainFullyComplete(chain) && GetObjectivePlaqueStepKey(chain) != null)
					return chain;
			return null;
		}

		public bool ShouldShowNpcOnMinimap(QuestChainDef chain)
		{
			if (chain.IsPrologue)
				return true;
			if (chain.Role != QuestChainRole.Hub)
			{
				if (chain.Steps.Count == 0)
					return false;
				string active = model.Chain.GetActiveStepKey(chain);
				if (active != null && !model.IsStepGiven(active))
					return true;
			}

			return ShouldShowQuestPopup(chain);
		}

		public bool ShouldHighlightQuestNpc(QuestChainDef chain)
		{
			if (chain.IsPrologue)
				return false;
			if (chain.Role == QuestChainRole.Hub)
				return model.Hub.ShouldHighlightHub(chain);
			if (chain.Steps.Count == 0)
				return false;
			string active = model.Chain.GetActiveStepKey(chain);
			if (active != null && !model.IsStepGiven(active))
				return true;
			if (model.Chain.HasUncollectedStepReward(chain))
				return true;
			return model.Chain.IsChainComplete(chain) && !model.IsChainRewardClaimed(chain.ChainKey) && !model.IsChainRewardClaimTimerActive(chain.ChainKey);
		}

		private QuestChainDef GetActiveNonPrologueChainForNpc(QuestNpcComponent npc)
		{
			if (!string.IsNullOrEmpty(npc.GiverKey))
				return GetActiveChainForGiver(npc.GiverKey);
			var chain = config.GetChainOfStep(npc.StepKey);
			if (chain == null || chain.IsPrologue || chain.Role == QuestChainRole.Hub)
				return null;
			if (!string.IsNullOrEmpty(chain.QuestGiverKey))
				return GetActiveChainForGiver(chain.QuestGiverKey);
			if (!model.Chain.IsChainUnlocked(chain) || model.Chain.IsChainFullyComplete(chain))
				return null;
			if (ShouldShowQuestPopup(chain))
				return chain;
			string active = model.Chain.GetActiveStepKey(chain);
			return active == null || active == npc.StepKey ? chain : null;
		}

		private bool IsSideChainInProgress(QuestGiverDef giver)
		{
			if (giver == null || string.IsNullOrEmpty(giver.SideChainKey))
				return false;
			var side = config.GetChain(giver.SideChainKey);
			return side != null && model.Chain.IsChainUnlocked(side) && !model.Chain.IsChainFullyComplete(side);
		}
	}
}
