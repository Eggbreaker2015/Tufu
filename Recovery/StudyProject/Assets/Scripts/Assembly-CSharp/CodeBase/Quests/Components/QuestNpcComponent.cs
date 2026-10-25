using System;
using Codebase.Utilities;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Quests.Components
{
	[Serializable]
	public struct QuestNpcComponent : IComponent
	{
		[KeysDropdown(typeof(QuestGiverKeys))]
		public string GiverKey;

		[KeysDropdown(typeof(QuestKeys))]
		public string StepKey;

		public Transform Transform;

		public GameObject ExclamationMarker;

		public float TriggerDistance;
	}
}
