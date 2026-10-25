using System;
using System.Collections.Generic;
using Codebase.Abilities.Core;
using UnityEngine;

namespace Codebase.Abilities
{
	[Serializable]
	public class AbilityDef
	{
		public string Key;

		public AbilityTrigger Trigger;

		[SerializeReference]
		public List<IAbilityCondition> Conditions;

		[SerializeReference]
		public List<IAbilityEffect> Effects;

		public ConditionMode ConditionMode;

		public int UnlockLevel;

		public float Value;

		public float Chance;

		public int StackLimit;

		public bool AffectsValue;

		public bool AffectsChance;

		public bool AffectsStacks;

		public List<AbilityLevelScale> LevelScaling;
	}
}
