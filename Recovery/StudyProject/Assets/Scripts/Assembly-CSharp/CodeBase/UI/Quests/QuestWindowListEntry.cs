using Codebase.Quests;
using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Analytics;

namespace Codebase.UI.Quests
{
	public sealed class QuestWindowListEntry
	{
		public QuestStepDef Step { get; private set; }
		public QuestHubTokenSlot HubSlot { get; private set; }

		public bool IsHubToken
		{
			get
			{
				return HubSlot != null;
			}
		}

		public static QuestWindowListEntry FromStep(QuestStepDef step)
		{ // Original ELF 0x16391f0; candidate only.
			return new QuestWindowListEntry
			{
				Step = step
			};
		}

		public static QuestWindowListEntry FromHubSlot(QuestHubTokenSlot slot)
		{ // Original ELF 0x1639178; candidate only.
			return new QuestWindowListEntry
			{
				HubSlot = slot
			};
		}
	}
}
