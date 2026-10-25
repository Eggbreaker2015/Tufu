using System;
using UnityEngine;

namespace Codebase.Quests
{
	[Serializable]
	public class QuestGiverDef
	{
		public string GiverKey;

		public string SideChainKey;

		public string HubChainKey;

		public Sprite Portrait;

		public Sprite MinimapIcon;

		public bool OverrideMinimapHighlightColor;

		public Color MinimapHighlightColor;
	}
}
