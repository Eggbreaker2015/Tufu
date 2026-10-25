using System;
using System.Collections.Generic;
using Codebase.Utilities;
using UnityEngine;

namespace Codebase.Quests
{
	[Serializable]
	public class QuestHubTokenSlot
	{
		public string TokenKey;

		public string SourceChainKey;

		public Sprite Icon;

		[LocalizationKey(Prefix = "QUEST_CURRENCY")]
		public string DisplayNameKey;

		[LocalizationKey(Prefix = "QUEST_NPC_NAME")]
		public string DeliveryNpcNameKey;

		public List<QuestDialogueLine> DeliveryMonologue;
	}
}
