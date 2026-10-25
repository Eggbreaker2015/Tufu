using System;
using System.Collections.Generic;
using Codebase.Enemy;
using Codebase.Unit;
using Codebase.Utilities;

namespace Codebase.Quests
{
	[Serializable]
	public class QuestStepDef
	{
		[KeysDropdown(typeof(QuestKeys))]
		public string StepKey;
		[LocalizationKey(Prefix = "QUEST_NPC_NAME")]
		public string NpcNameKey;
		public List<QuestDialogueLine> Monologue;
		public List<QuestDialogueLine> PostCutsceneMonologue;
		[LocalizationKey(Prefix = "QUEST_STEP_NAME")]
		public string StepNameKey;
		[LocalizationKey(Prefix = "QUEST_STEP_SHORT_DESC")]
		public string StepShortDescKey;
		public bool HasCutscene;
		public EnemyUnit Prefab;
		public int LocationNumber;
		public float OptimumTime;
		public bool IsDays;
		public List<StatModifier> StatRewards;
		public bool HasMonologue
		{
			get
			{
				return Monologue.Count > 0;
			}
		}

		public bool HasPostCutsceneMonologue
		{
			get
			{
				return PostCutsceneMonologue.Count > 0;
			}
		}
	}
}
