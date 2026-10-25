using System;
using Codebase.Utilities;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Quests.Components
{
	[Serializable]
	public struct QuestRoadComponent : IComponent
	{
		[KeysDropdown(typeof(QuestGiverKeys))]
		public string FromGiverKey;

		[KeysDropdown(typeof(QuestGiverKeys))]
		public string ToGiverKey;

		public GameObject Road;
	}
}
