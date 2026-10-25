using System;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Economy;
using Codebase.Meta;
using Codebase.Player;
using Codebase.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Codebase.Analytics;

namespace Codebase.UI.Quests
{
	public class QuestItemView : View
	{
		[SerializeField]
		private TMP_Text questName;
		[SerializeField]
		private TMP_Text questDesc;
		[SerializeField]
		private Image statIcon;
		[SerializeField]
		private TMP_Text rewardAmount;
		[SerializeField]
		private Button findButton;
		[SerializeField]
		private Button collectButton;
		[SerializeField]
		private TMP_Text collectButtonLabel;
		[SerializeField]
		private GameObject lockOverlay;
		[SerializeField]
		private GameObject completedOverlay;
		[SerializeField]
		private TMP_Text completedOverlayLabel;
		[Inject]
		private QuestModel questModel;
		[Inject]
		private MetaStatsModel metaStatsModel;
		[Inject]
		private IconStyleConfig iconStyleConfig;
		[Inject]
		private CurrencyStyleConfig currencyStyleConfig;
		[Inject]
		private IAudioPlayer audioPlayer;
		private QuestStepDef stepDef;
		private QuestChainDef hubChain;
		private QuestHubTokenSlot hubSlot;
		private Action onFind;
		private Action onCollect;
		private bool hubMode;
		protected override void Awake()
		{ // Original ELF 0x163442c; candidate only.
			base.Awake();
			findButton.onClick.AddListener(HandleFind);
			collectButton.onClick.AddListener(HandleCollect);
		}

		public void Setup(QuestStepDef stepDef, Action onFind, Action onCollect)
		{ // Original ELF 0x1634520; candidate only.
			hubMode = false;
			this.stepDef = stepDef;
			hubChain = null;
			hubSlot = null;
			this.onFind = onFind;
			this.onCollect = onCollect;
			Initialize();
		}

		public void SetupHubToken(QuestChainDef chain, QuestHubTokenSlot slot, Action onDeliver, Action onFind)
		{ // Original ELF 0x1634598; candidate only.
			hubMode = true;
			hubChain = chain;
			hubSlot = slot;
			stepDef = null;
			this.onFind = onFind;
			onCollect = onDeliver;
			Initialize();
		}

		protected override void Render()
		{ // Original ELF 0x1634620; candidate only.
			if (hubMode)
			{
				RenderHubToken();
				return;
			}

			if (stepDef == null)
				return;
			questName.text = Codebase.Utilities.LocalizationStrings.Get(stepDef.StepNameKey);
			questDesc.text = Codebase.Utilities.LocalizationStrings.Get(stepDef.StepShortDescKey);
			RenderReward();
			bool locked = !questModel.IsStepGiven(stepDef.StepKey);
			bool claimed = false, find = false, collect = false;
			if (!locked)
			{
				claimed = questModel.IsStepRewardClaimed(stepDef.StepKey);
				find = !questModel.IsStepComplete(stepDef.StepKey);
				collect = questModel.IsStepComplete(stepDef.StepKey) && !questModel.IsStepRewardClaimed(stepDef.StepKey);
			}

			lockOverlay.SetActive(locked);
			completedOverlay.SetActive(claimed);
			findButton.gameObject.SetActive(find);
			collectButton.gameObject.SetActive(collect);
			completedOverlayLabel.text = Codebase.Utilities.LocalizationStrings.Get("QUEST_COMPLETED");
			if (collect)
				collectButtonLabel.text = Codebase.Utilities.LocalizationStrings.Get("QUEST_CLAIM");
		}

		private void RenderHubToken()
		{ // Original ELF 0x1634894; candidate only.
			if (hubChain == null || hubSlot == null)
				return;
			questName.text = Codebase.Utilities.LocalizationStrings.Get(hubSlot.DisplayNameKey);
			rewardAmount.text = string.Empty;
			statIcon.sprite = hubSlot.Icon != null ? hubSlot.Icon : currencyStyleConfig.GetIcon(hubSlot.TokenKey);
			bool delivered = questModel.IsHubTokenDelivered(hubSlot.TokenKey);
			bool sourceComplete = questModel.Hub.IsHubSourceChainComplete(hubSlot.SourceChainKey);
			bool canDeliver = questModel.Hub.CanDeliverHubToken(hubChain, hubSlot.TokenKey);
			bool find = questModel.Hub.ShouldShowHubTokenFind(hubChain, hubSlot);
			lockOverlay.SetActive(!(delivered || sourceComplete || find));
			completedOverlay.SetActive(delivered);
			collectButton.gameObject.SetActive(canDeliver);
			findButton.gameObject.SetActive(find);
			completedOverlayLabel.text = delivered ? Codebase.Utilities.LocalizationStrings.Get("QUEST_HUB_TOKEN_DELIVERED") : string.Empty;
			if (delivered)
				questDesc.text = Codebase.Utilities.LocalizationStrings.Get("QUEST_HUB_TOKEN_DELIVERED");
			else if (!sourceComplete)
				questDesc.text = Codebase.Utilities.LocalizationStrings.Get("QUEST_HUB_TOKEN_LOCKED");
			else if (!canDeliver)
				questDesc.text = Codebase.Utilities.LocalizationStrings.Get("QUEST_HUB_TOKEN_MISSING");
			else
			{
				questDesc.text = Codebase.Utilities.LocalizationStrings.Get("QUEST_HUB_TOKEN_READY");
				collectButtonLabel.text = Codebase.Utilities.LocalizationStrings.Get("QUEST_HUB_DELIVER");
			}
		}

		private void RenderReward()
		{ // Original ELF 0x1634c1c; candidate only.
			var stat = System.Linq.Enumerable.FirstOrDefault(stepDef.StatRewards);
			if (stat == null)
			{
				rewardAmount.text = string.Empty;
				return;
			}

			statIcon.sprite = System.Linq.Enumerable.First(iconStyleConfig.Styles, style => style.StatType == stat.StatType).Sprite;
			double metaValue = metaStatsModel.Get("stat_drop").GetValue(stepDef.LocationNumber);
			float value = stat.Value * (float)metaValue;
			if (Codebase.Unit.StatTypeExtensions.IsPercentDisplay(stat.StatType))
				rewardAmount.text = "+" + Codebase.Utilities.DoubleExtensions.Format((double)value * 100.0) + "%";
			else
				rewardAmount.text = string.Format("+{0}", new Codebase.Unit.StatModifier { Value = value, StatType = stat.StatType, ModifierType = stat.ModifierType, Source = stat.Source });
		}

		private void HandleFind()
		{ // Original ELF 0x1634f24; candidate only.
			onFind?.Invoke();
		}

		private void HandleCollect()
		{ // Original ELF 0x1634f40; candidate only.
			if (hubMode)
			{
				audioPlayer.PlaySoundOnce("upgrade_weapon");
				App.ExecuteCommand(new Codebase.Quests.Commands.DeliverQuestHubTokenCommand { ChainKey = hubChain.ChainKey, TokenKey = hubSlot.TokenKey });
				onCollect?.Invoke();
			}
			else
			{
				audioPlayer.PlaySoundOnce("mail_claim");
				App.ExecuteCommand(new Codebase.Quests.Commands.ClaimQuestStepRewardCommand { StepKey = stepDef.StepKey, OnComplete = onCollect });
			}
		}
	}
}
