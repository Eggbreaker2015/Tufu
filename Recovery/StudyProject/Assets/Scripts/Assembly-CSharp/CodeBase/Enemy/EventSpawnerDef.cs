using System;
using System.Collections.Generic;
using Codebase.Events;
using Codebase.Utilities;

namespace Codebase.Enemy
{
	[Serializable]
	public class EventSpawnerDef
	{
		[KeysDropdown(typeof(EventKeys))]
		public string Key;

		public EventUnitType UnitType;

		public List<EventSpawnerVariant> Variants;

		public EventSpawnerVariant GetRandomVariant()
		{
			return Variants == null || Variants.Count == 0 ? null : Variants[UnityEngine.Random.Range(0, Variants.Count)];
		}
	}
}
