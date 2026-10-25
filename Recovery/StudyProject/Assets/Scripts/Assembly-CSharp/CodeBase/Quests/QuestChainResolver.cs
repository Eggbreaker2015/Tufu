namespace Codebase.Quests
{
	public class QuestChainResolver
	{
		private readonly QuestModel model;
		private readonly QuestConfig config;
		public QuestChainResolver(QuestModel model, QuestConfig config)
		{
			this.model = model;
			this.config = config;
		}

		public string GetActiveStepKey(QuestChainDef chain)
		{
			foreach (var step in chain.Steps)
				if (!model.IsStepComplete(step.StepKey))
					return step.StepKey;
			return null;
		}

		public bool IsChainComplete(QuestChainDef chain)
		{
			foreach (var step in chain.Steps)
				if (!model.IsStepComplete(step.StepKey))
					return false;
			return true;
		}

		public bool IsStepActive(string stepKey)
		{ // Original ELF 0x142069c; independent candidate only.
			var chain = config.GetChainOfStep(stepKey);
			return chain != null && chain.Role != QuestChainRole.Hub && GetActiveStepKey(chain) == stepKey && IsChainUnlocked(chain);
		}

		public bool IsChainUnlocked(QuestChainDef chain)
		{
			if (chain == null)
				return false;
			if (chain.IsPrologue)
				return true;
			QuestChainDef previous;
			if (chain.Role == QuestChainRole.Hub)
			{
				var giver = config.GetGiverByHubChain(chain.ChainKey);
				if (giver == null || string.IsNullOrEmpty(giver.SideChainKey))
					return false;
				previous = config.GetChain(giver.SideChainKey);
			}
			else if (!string.IsNullOrEmpty(chain.UnlocksAfterChainKey))
				previous = config.GetChain(chain.UnlocksAfterChainKey);
			else
			{
				previous = config.GetPreviousChain(chain);
				if (previous == null)
					return true;
			}

			return previous != null && IsChainFullyComplete(previous);
		}

		public bool IsChainFullyComplete(QuestChainDef chain)
		{
			if (chain == null)
				return false;
			bool complete = chain.Role == QuestChainRole.Hub ? model.Hub.IsHubAllTokensDelivered(chain) : IsChainComplete(chain);
			return complete && model.IsChainRewardClaimed(chain.ChainKey);
		}

		public bool IsChainStarted(QuestChainDef chain)
		{
			foreach (var step in chain.Steps)
				if (model.IsStepGiven(step.StepKey))
					return true;
			return false;
		}

		public bool HasUncollectedStepReward(QuestChainDef chain)
		{
			foreach (var step in chain.Steps)
				if (model.IsStepComplete(step.StepKey) && !model.IsStepRewardClaimed(step.StepKey))
					return true;
			return false;
		}

		public bool HasUnclaimedReward(QuestChainDef chain)
		{ // Original ELF 0x1421140; independent candidate only.
			if (chain == null)
				return false;
			if (HasUncollectedStepReward(chain))
				return true;
			if (model.IsChainRewardClaimed(chain.ChainKey))
				return false;
			if (chain.Role == QuestChainRole.Hub)
				return model.Hub.CanDeliverAnyHubToken(chain) || model.Hub.IsHubAllTokensDelivered(chain);
			return IsChainComplete(chain) && model.GetChainRewardClaimRemainingSeconds(chain.ChainKey) <= 0;
		}

		public int GetCompletedStepsCount(QuestChainDef chain)
		{ // Original ELF 0x14213f0; independent candidate only.
			int count = 0;
			foreach (var step in chain.Steps)
				if (model.IsStepComplete(step.StepKey))
					count++;
			return count;
		}

		public int GetClaimedStepsCount(QuestChainDef chain)
		{ // Original ELF 0x14215d0; independent candidate only.
			int count = 0;
			foreach (var step in chain.Steps)
				if (model.IsStepRewardClaimed(step.StepKey))
					count++;
			return count;
		}

		public string GetActiveHuntStepKey(QuestChainDef chain)
		{ // Original ELF 0x1410a04; independent candidate only.
			foreach (var step in chain.Steps)
				if (model.IsStepGiven(step.StepKey) && !model.IsStepComplete(step.StepKey))
					return step.StepKey;
			return null;
		}

		public QuestChainDef GetFirstAvailableUnstartedChain()
		{ // Original ELF 0x14217b0; independent candidate only.
			foreach (var chain in config.Chains)
				if (!chain.IsPrologue && chain.Role != QuestChainRole.Hub && IsChainUnlocked(chain) && !IsChainStarted(chain) && !IsChainFullyComplete(chain))
					return chain;
			return null;
		}

		public QuestChainDef GetNextAvailableChain(QuestChainDef completedChain)
		{ // Original ELF 0x14219cc; independent candidate only.
			foreach (var chain in config.Chains)
				if (!chain.IsPrologue && chain.ChainKey != completedChain.ChainKey && IsChainUnlocked(chain) && !IsChainStarted(chain) && !IsChainFullyComplete(chain))
					return chain;
			return null;
		}
	}
}
