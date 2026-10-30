using System;
using UnityEngine;

namespace Codebase.Rpg
{
	public enum HeroRole { Protagonist, Tank, Bruiser, Guard, MeleeDps, CritDps, ReachDps }

	[Serializable]
	public class HeroDefinition
	{
		public string Key;
		public bool IsProtagonist;
		public HeroRole Role;
		public string PetItemKey;
		public string AttackItemKey;
		public float HealthRatio = 1f;
		public float AttackRatio = 1f;
		public float DefenceFlat;
		public float DefenceRatio = 1f;
		public float AttackRangeOverride;
		public string DisplayName;
		public Sprite Icon;
	}
}
