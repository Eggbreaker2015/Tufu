using System;
using System.Collections.Generic;
using Codebase.Rewards;
using Codebase.Utilities;
using UnityEngine;

namespace Codebase.Quests
{
	[Serializable]
	public class QuestChainDef
	{
		public string ChainKey;
		public bool IsPrologue;
		public QuestChainRole Role;
		public string QuestGiverKey;
		public string UnlocksAfterChainKey;
		[LocalizationKey(Prefix = "QUEST_CHAIN_NAME")]
		public string ChainNameKey;
		[LocalizationKey(Prefix = "QUEST_CHAIN_DESC")]
		public string ChainDescriptionKey;
		public Sprite ChainIcon;
		public float MobEvasionPercent;
		public float MobHealthRegenMaxHealthPercent;
		public float MobAttackMultiplier;
		public float MobHealthMultiplier;
		public float MobAttackCooldown;
		public float MobLifestealMaxHealthPercent;
		public float MobReflectionMaxHealthPercent;
		public List<QuestStepDef> Steps;
		public List<QuestHubTokenSlot> HubTokenSlots;
		[LocalizationKey(Prefix = "QUEST_NPC_NAME")]
		public string FinaleNpcNameKey;
		public List<QuestDialogueLine> FinaleMonologue;
		[SerializeReference]
		public List<RewardBase> ChainReward;
		public bool HasFinaleMonologue
		{
			get
			{
				return FinaleMonologue.Count > 0;
			}
		}
	}
}
