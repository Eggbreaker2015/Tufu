using System;
using UnityEngine;

namespace Codebase.Rpg
{
	[Serializable]
	public class HeroDefinition
	{
		public string Key;
		public bool IsProtagonist;
		public string PetItemKey;
		public string AttackItemKey;
		public float HealthRatio = 1f;
		public float AttackRatio = 1f;
		public float DefenceFlat;
		public float AttackRangeOverride;
		public string DisplayName;
		public Sprite Icon;
	}
}
