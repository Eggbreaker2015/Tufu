using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Rewards;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Codebase.Analytics;

namespace Codebase.UI.Quests
{
	public class QuestRewardItemView : View
	{
		[SerializeField]
		private Image icon;
		[SerializeField]
		private TMP_Text nameText;
		[SerializeField]
		private TMP_Text amountText;
		[SerializeField]
		private Sprite itemSlotUnlockIcon;
		[SerializeField]
		private Sprite mineUnlockIcon;
		[Inject]
		private RewardIconResolver rewardIconResolver;
		private RewardBase reward;
		public void Setup(RewardBase reward)
		{ // Original ELF 0x1636210; candidate only.
			this.reward = reward;
			Initialize();
		}

		protected override void Render()
		{ // Original ELF 0x1636234; candidate only.
			if (reward == null)
				return;
			nameText.text = rewardIconResolver.FormatName(reward);
			amountText.text = rewardIconResolver.FormatAmount(reward);
			icon.sprite = reward is ItemSlotReward ? itemSlotUnlockIcon : reward is MineUnlockReward ? mineUnlockIcon : rewardIconResolver.Resolve(reward);
		}
	}
}
