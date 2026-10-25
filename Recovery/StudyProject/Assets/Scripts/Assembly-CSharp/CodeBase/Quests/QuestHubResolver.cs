using Codebase.Economy;

namespace Codebase.Quests
{
	public class QuestHubResolver
	{
		private readonly QuestModel model;
		private readonly QuestConfig config;
		private readonly CurrencyModel currencyModel;
		public QuestHubResolver(QuestModel model, QuestConfig config, CurrencyModel currencyModel)
		{
			this.model = model;
			this.config = config;
			this.currencyModel = currencyModel;
		}

		public bool IsHubAllTokensDelivered(QuestChainDef chain)
		{
			if (chain == null || chain.Role != QuestChainRole.Hub)
				return false;
			foreach (var slot in chain.HubTokenSlots)
				if (!model.IsHubTokenDelivered(slot.TokenKey))
					return false;
			return chain.HubTokenSlots.Count > 0;
		}

		public int GetHubDeliveredCount(QuestChainDef chain)
		{ // Original ELF 0x1423a44; independent candidate only.
			int count = 0;
			foreach (var slot in chain.HubTokenSlots)
				if (model.IsHubTokenDelivered(slot.TokenKey))
					count++;
			return count;
		}

		public int GetHubCollectedCount(QuestChainDef chain)
		{ // Original ELF 0x1423c24; independent candidate only.
			int count = 0;
			foreach (var slot in chain.HubTokenSlots)
				if (IsHubQuestItemCollected(slot.TokenKey))
					count++;
			return count;
		}

		public bool IsHubQuestItemCollected(string tokenKey)
		{ // Original ELF 0x1423df8; independent candidate only.
			return model.IsHubTokenDelivered(tokenKey) || currencyModel.Get(tokenKey) >= 1.0;
		}

		public bool CanDeliverHubToken(QuestChainDef chain, string tokenKey)
		{
			if (chain == null || chain.Role != QuestChainRole.Hub || model.IsHubTokenDelivered(tokenKey))
				return false;
			foreach (var slot in chain.HubTokenSlots)
				if (slot.TokenKey == tokenKey)
					return currencyModel.Get(tokenKey) >= 1.0;
			return false;
		}

		public bool CanDeliverAnyHubToken(QuestChainDef chain)
		{
			foreach (var slot in chain.HubTokenSlots)
				if (CanDeliverHubToken(chain, slot.TokenKey))
					return true;
			return false;
		}

		public QuestHubTokenSlot GetInitialDeliverableSlot(QuestChainDef hubChain)
		{ // Original ELF 0x1424078; independent candidate only.
			if (hubChain == null || hubChain.Role != QuestChainRole.Hub)
				return null;
			var giver = config.GetGiverByHubChain(hubChain.ChainKey);
			if (giver == null || string.IsNullOrEmpty(giver.SideChainKey))
				return null;
			foreach (var slot in hubChain.HubTokenSlots)
				if (slot.SourceChainKey == giver.SideChainKey)
					return CanDeliverHubToken(hubChain, slot.TokenKey) ? slot : null;
			return null;
		}

		public bool IsHubSourceChainComplete(string sourceChainKey)
		{ // Original ELF 0x14242b0; independent candidate only.
			var chain = config.GetChain(sourceChainKey);
			return chain != null && model.Chain.IsChainFullyComplete(chain);
		}

		public bool ShouldShowHubTokenFind(QuestChainDef hubChain, QuestHubTokenSlot slot)
		{ // Original ELF 0x14242f0; independent candidate only.
			if (hubChain == null || slot == null || hubChain.Role != QuestChainRole.Hub || model.IsHubTokenDelivered(slot.TokenKey) || IsHubSourceChainComplete(slot.SourceChainKey))
				return false;
			var side = model.Popup.GetSideChainForObjectivePlaque();
			return side != null && side.ChainKey == slot.SourceChainKey;
		}

		public bool ShouldHighlightHub(QuestChainDef chain)
		{
			if (chain == null || chain.Role != QuestChainRole.Hub)
				return false;
			return CanDeliverAnyHubToken(chain) || (IsHubAllTokensDelivered(chain) && !model.IsChainRewardClaimed(chain.ChainKey));
		}
	}
}
