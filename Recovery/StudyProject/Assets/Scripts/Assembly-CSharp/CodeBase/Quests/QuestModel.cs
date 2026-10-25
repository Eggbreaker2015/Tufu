using System.Collections.Generic;
using Codebase.Core;
using Codebase.Economy;
using Codebase.Services.Time;
using UniMob;

namespace Codebase.Quests
{
	public class QuestModel : Model<QuestsUd>
	{
		[Inject]
		private QuestConfig config;
		[Inject]
		private ITimeService timeService;
		[Inject]
		private CurrencyModel currencyModel;
		private readonly HashSet<string> pendingMobSpawnSteps;
		private readonly HashSet<string> roadRevealHoldChains;
		public QuestChainResolver Chain { get; private set; }
		public QuestHubResolver Hub { get; private set; }
		public QuestRoadResolver Road { get; private set; }
		public QuestPopupResolver Popup { get; private set; }
		public QuestPrologueResolver Prologue { get; private set; }
		public QuestStepFlowResolver StepFlow { get; private set; }

		public QuestModel(Lifetime lifetime) : base(lifetime)
		{
			// Original ELF0x1427704.
			pendingMobSpawnSteps = new HashSet<string>();
			roadRevealHoldChains = new HashSet<string>();
		}

		public override void Initialize()
		{
			// Original ELF0x14277d4 initializes genuine saved data before resolver bindings.
			base.Initialize();
			Chain = new QuestChainResolver(this, config);
			Hub = new QuestHubResolver(this, config, currencyModel);
			Road = new QuestRoadResolver(this, config);
			Popup = new QuestPopupResolver(this, config);
			Prologue = new QuestPrologueResolver(this, config);
			StepFlow = new QuestStepFlowResolver(config);
		}

		public void HoldQuestRoadUntilPopup(string chainKey)
		{
			// Original ELF 0x14279c8.
			if (!string.IsNullOrEmpty(chainKey))
				roadRevealHoldChains.Add(chainKey);
		}

		public void ReleaseQuestRoadHold(string chainKey)
		{
			// Original ELF 0x1427a3c.
			if (!string.IsNullOrEmpty(chainKey))
				roadRevealHoldChains.Remove(chainKey);
		}

		public bool IsQuestRoadHeld(string chainKey)
		{
			// Original ELF 0x1427ab0.
			return !string.IsNullOrEmpty(chainKey) && roadRevealHoldChains.Contains(chainKey);
		}

		public bool IsStepGiven(string stepKey)
		{
			return Data.Steps.TryGetValue(stepKey, out var saved) && saved.IsGiven.Value;
		}

		public bool IsStepComplete(string stepKey)
		{
			return Data.Steps.TryGetValue(stepKey, out var saved) && saved.IsComplete.Value;
		}

		public bool IsStepRewardClaimed(string stepKey)
		{
			return Data.Steps.TryGetValue(stepKey, out var saved) && saved.RewardClaimed.Value;
		}

		public void MarkGiven(string stepKey)
		{ // Original ELF 0x1427b28; independent candidate only.
			bool alreadyGiven = IsStepGiven(stepKey);
			Data.Steps.GetOrCreate(stepKey, out _).IsGiven.Value = true;
			if (!alreadyGiven && ShouldDeferMobSpawn(stepKey))
				pendingMobSpawnSteps.Add(stepKey);
		}

		public void MarkComplete(string stepKey)
		{ // Original ELF 0x1427cb4; independent candidate only.
			Data.Steps.GetOrCreate(stepKey, out _).IsComplete.Value = true;
			pendingMobSpawnSteps.Remove(stepKey);
		}

		public void MarkStepRewardClaimed(string stepKey)
		{ // Original ELF 0x1427d90; independent candidate only.
			Data.Steps.GetOrCreate(stepKey, out _).RewardClaimed.Value = true;
		}

		public bool ShouldSpawnQuestMob(string stepKey)
		{ // Original ELF 0x1427e44; independent candidate only.
			return !pendingMobSpawnSteps.Contains(stepKey);
		}

		public void ReleaseQuestMobSpawn(string stepKey)
		{ // Original ELF 0x1410c0c; independent candidate only.
			pendingMobSpawnSteps.Remove(stepKey);
		}

		private bool ShouldDeferMobSpawn(string stepKey)
		{ // Original ELF 0x1427c28; independent candidate only.
			var step = config.GetStep(stepKey);
			return step != null && step.Prefab != null;
		}

		public void TryAdvanceToNextStep(string claimedStepKey)
		{ // Original ELF 0x1427ea8; independent candidate only.
			var next = config.GetNextStep(claimedStepKey);
			if (next == null || !StepFlow.ShouldAutoStartStep(next) || IsStepGiven(next.StepKey))
				return;
			MarkGiven(next.StepKey);
			if (StepFlow.ShouldPlayFinaleOnChainComplete(next))
				QuestStepWorldApplier.ApplyEnable(next.StepKey);
			else
				QuestStepWorldApplier.Apply(next.StepKey);
		}

		public bool IsChainRewardClaimed(string chainKey)
		{
			return Data.Chains.TryGetValue(chainKey, out var saved) && saved.RewardClaimed.Value;
		}

		public void MarkChainRewardClaimed(string chainKey)
		{ // Original ELF 0x1427f48; independent candidate only.
			Data.Chains.GetOrCreate(chainKey, out _).RewardClaimed.Value = true;
		}

		public int GetChainRewardClaimRemainingSeconds(string chainKey)
		{
			// Original ELF0x1427ffc clears an expired stored timer on read.
			if (!Data.Chains.TryGetValue(chainKey, out var saved))
				return 0;
			int end = saved.RewardClaimAtUnixSeconds.Value;
			if (end <= 0)
				return 0;
			int remaining = unchecked(end - GetNowUnixSeconds());
			if (remaining > 0)
				return remaining;
			saved.RewardClaimAtUnixSeconds.Value = 0;
			return 0;
		}

		public bool IsChainRewardClaimTimerActive(string chainKey)
		{
			return GetChainRewardClaimRemainingSeconds(chainKey) > 0;
		}

		public void StartChainRewardClaimTimer(string chainKey)
		{ // Original ELF 0x1428204; independent candidate only.
			Data.Chains.GetOrCreate(chainKey, out _).RewardClaimAtUnixSeconds.Value = unchecked(GetNowUnixSeconds() + 7200);
		}

		public void ClearChainRewardClaimTimer(string chainKey)
		{ // Original ELF 0x14282c8; independent candidate only.
			Data.Chains.GetOrCreate(chainKey, out _).RewardClaimAtUnixSeconds.Value = 0;
		}

		public int GetSkipChainRewardClaimGemCost(string chainKey)
		{ // Original ELF 0x1428378; independent candidate only.
			int remaining = GetChainRewardClaimRemainingSeconds(chainKey);
			if (remaining <= 0)
				return 0;
			float scaled = ((float)remaining / 7200f) * 250f;
			return System.Math.Max(1, (int)System.Math.Ceiling(scaled));
		}

		public bool IsHubTokenDelivered(string tokenKey)
		{
			return Data.DeliveredTokens.TryGetValue(tokenKey, out bool delivered) && delivered;
		}

		public void MarkHubTokenDelivered(string tokenKey)
		{ // Original ELF 0x1428414; independent candidate only.
			Data.DeliveredTokens[tokenKey] = true;
		}

		public void DeliverInitialHubTokenIfNeeded(QuestChainDef hubChain)
		{ // Original ELF 0x1428498; independent candidate only.
			if (hubChain == null)
				return;
			DeliverInitialHubTokenIfNeeded(config.GetGiverByHubChain(hubChain.ChainKey), hubChain);
		}

		public void DeliverInitialHubTokenIfNeeded(QuestGiverDef giver, QuestChainDef hubChain)
		{ // Original ELF 0x14284e4; independent candidate only.
			if (giver == null || hubChain == null || hubChain.Role != QuestChainRole.Hub || string.IsNullOrEmpty(giver.SideChainKey))
				return;
			foreach (var slot in hubChain.HubTokenSlots)
			{
				if (slot.SourceChainKey != giver.SideChainKey)
					continue;
				if (Hub.CanDeliverHubToken(hubChain, slot.TokenKey))
				{
					currencyModel.TrySpend(slot.TokenKey, 1.0);
					MarkHubTokenDelivered(slot.TokenKey);
				}

				return;
			}
		}

		public void ResetAllProgress()
		{
		}

		public void CompleteAllSideChains()
		{
		}

		public void SkipCurrentQuestToLastStep()
		{
		}

		private QuestChainDef GetCurrentSkippableSideChain()
		{
			return null;
		}

		private int GetNowUnixSeconds()
		{
			return unchecked((int)new System.DateTimeOffset(timeService.Now.ToDateTime()).ToUnixTimeSeconds());
		}
	}
}
