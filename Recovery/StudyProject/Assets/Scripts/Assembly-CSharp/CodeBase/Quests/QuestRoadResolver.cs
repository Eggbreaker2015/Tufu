using Codebase.Quests.Components;

namespace Codebase.Quests
{
	public class QuestRoadResolver
	{
		private readonly QuestModel model;

		private readonly QuestConfig config;

		public QuestRoadResolver(QuestModel model, QuestConfig config)
		{
			this.model = model; this.config = config;
		}

		public bool ShouldShowQuestRoad(QuestRoadComponent road)
		{
			// Original ELF 0x142baf4.
			return ShouldShowRoadConnection(road.FromGiverKey, road.ToGiverKey)
			    || ShouldShowRoadConnection(road.ToGiverKey, road.FromGiverKey);
		}

		private bool ShouldShowRoadConnection(string fromGiverKey, string toGiverKey)
		{
			// Original ELF 0x142bb38.
			if (string.IsNullOrEmpty(fromGiverKey) || string.IsNullOrEmpty(toGiverKey)) return false;
			var sourceGiver = config.GetGiver(fromGiverKey);
			var targetGiver = config.GetGiver(toGiverKey);
			if (sourceGiver == null || targetGiver == null) return false;
			if (!string.IsNullOrEmpty(sourceGiver.SideChainKey) && !string.IsNullOrEmpty(targetGiver.HubChainKey)) {
			    var sourceChain = config.GetChain(sourceGiver.SideChainKey);
			    var hubChain = config.GetChain(targetGiver.HubChainKey);
			    if (sourceChain != null && hubChain != null && ShouldShowHubDeliveryRoad(hubChain, sourceChain.ChainKey)) return true;
			}
			if (string.IsNullOrEmpty(targetGiver.SideChainKey)) return false;
			var sideChain = config.GetChain(targetGiver.SideChainKey);
			return sideChain != null && (ShouldShowSideUnlockRoad(sourceGiver, sideChain) || ShouldShowHubToSideRoad(sourceGiver, sideChain));
		}

		public bool ShouldShowSideUnlockRoad(QuestGiverDef sourceGiver, QuestChainDef sideChain)
		{
			// Original ELF 0x142bf60.
			if (sideChain == null || sideChain.Role == QuestChainRole.Hub || sideChain.IsPrologue) return false;
			if (!IsSideUnlockSource(sourceGiver, sideChain) || !model.Chain.IsChainUnlocked(sideChain)) return false;
			if (model.Chain.IsChainStarted(sideChain) && !model.IsQuestRoadHeld(sideChain.ChainKey)) return false;
			return !RequiresHubDeliveryBeforeSideStart(sideChain);
		}

		private bool IsSideUnlockSource(QuestGiverDef sourceGiver, QuestChainDef sideChain)
		{
			// Original ELF 0x142c2b8.
			if (sourceGiver == null || string.IsNullOrEmpty(sourceGiver.SideChainKey)) return false;
			string sourceKey = sideChain.UnlocksAfterChainKey;
			if (string.IsNullOrEmpty(sourceKey)) {
			    var previous = config.GetPreviousChain(sideChain);
			    if (previous == null) return false;
			    sourceKey = previous.ChainKey;
			}
			return sourceKey == sourceGiver.SideChainKey;
		}

		public bool ShouldShowHubToSideRoad(QuestGiverDef sourceGiver, QuestChainDef sideChain)
		{
			// Original ELF 0x142c018.
			if (sideChain == null || sideChain.Role == QuestChainRole.Hub || !model.Chain.IsChainUnlocked(sideChain)) return false;
			if (model.Chain.IsChainStarted(sideChain) && !model.IsQuestRoadHeld(sideChain.ChainKey)) return false;
			if (!RequiresHubDeliveryBeforeSideStart(sideChain)) return false;
			var sourceChain = config.GetUnlockSourceChain(sideChain);
			if (sourceChain == null) return false;
			var hubChain = config.GetHubChainForTokenSource(sourceChain.ChainKey);
			if (sourceGiver == null || hubChain == null || sourceGiver.HubChainKey != hubChain.ChainKey) return false;
			foreach (var slot in hubChain.HubTokenSlots) {
			    if (slot.SourceChainKey == sourceChain.ChainKey) return model.IsHubTokenDelivered(slot.TokenKey);
			}
			return false;
		}

		public bool ShouldShowHubDeliveryRoad(QuestChainDef hubChain, string sourceChainKey)
		{
			// Original ELF 0x142bc98.
			if (hubChain == null || hubChain.Role != QuestChainRole.Hub || !model.Chain.IsChainUnlocked(hubChain) || model.Chain.IsChainFullyComplete(hubChain)) return false;
			if (string.IsNullOrEmpty(sourceChainKey)) return model.Hub.CanDeliverAnyHubToken(hubChain);
			var sourceChain = config.GetChain(sourceChainKey);
			if (sourceChain == null || !model.Chain.IsChainFullyComplete(sourceChain)) return false;
			foreach (var slot in hubChain.HubTokenSlots) {
			    if (slot.SourceChainKey == sourceChainKey) return model.Hub.CanDeliverHubToken(hubChain, slot.TokenKey);
			}
			return false;
		}

		private bool RequiresHubDeliveryBeforeSideStart(QuestChainDef sideChain)
		{
			// Original ELF 0x142c34c.
			var sourceChain = config.GetUnlockSourceChain(sideChain);
			if (sourceChain == null) return false;
			var hubChain = config.GetHubChainForTokenSource(sourceChain.ChainKey);
			if (hubChain == null || sourceChain.QuestGiverKey == hubChain.QuestGiverKey) return false;
			foreach (var slot in hubChain.HubTokenSlots) if (slot.SourceChainKey == sourceChain.ChainKey) return true;
			return false;
		}
	}
}
