using System;
using Codebase.Utilities;
using UnityEngine;

namespace Codebase.Quests
{
	[Serializable]
	public class QuestDialogueLine
	{
		[LocalizationKey(Prefix = "QUEST_STEP")]
		public string TextKey;

		public AudioClip Clip;
	}
}
