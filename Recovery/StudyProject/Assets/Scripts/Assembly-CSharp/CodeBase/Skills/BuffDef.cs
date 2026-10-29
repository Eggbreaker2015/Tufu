using System;
using System.Collections.Generic;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Skills
{
	[Serializable]
	public class BuffStatMod
	{
		public StatType StatType;

		public ModifierType ModifierType;

		// Flat/additive values add up per stack, multiplicative ones multiply.
		// MovementSpeed x PercentMultiplicative is the felt speed factor on every unit (see SkillService.GetStatValue).
		public float ValuePerStack;
	}

	[Serializable]
	public class BuffDef
	{
		public string Key;

		public float Duration;

		public bool IsDebuff;

		public BuffStackMode Stack;

		// 0 = unlimited (Stack mode only).
		public int MaxStacks;

		public List<BuffStatMod> StatMods = new List<BuffStatMod>();

		// Enemies only in phase 1.
		public bool Stun;

		// Blocks skill casting (not basic attacks).
		public bool Silence;

		// 0 = no periodic effects.
		public float TickInterval;

		// Run with Caster = the buff holder and Instigator = whoever applied the buff.
		[SerializeReference]
		public List<ISkillEffect> OnTick = new List<ISkillEffect>();
	}
}
