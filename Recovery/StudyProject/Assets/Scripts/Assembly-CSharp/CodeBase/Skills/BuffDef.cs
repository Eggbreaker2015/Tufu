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
		// On the hero and pets enemy debuffs only keep MovementSpeed, GlobalDefence and AttackCooldown (x PercentMultiplicative, read where used).
		public float ValuePerStack;
	}

	[Serializable]
	public class BuffDef
	{
		public string Key;

		// Ability icon key (Resources/AbilityIcons/<key>); empty = classified from the buff's contents.
		public string IconKey;

		public float Duration;

		public bool IsDebuff;

		public BuffStackMode Stack;

		// 0 = unlimited (Stack mode only).
		public int MaxStacks;

		public List<BuffStatMod> StatMods = new List<BuffStatMod>();

		// All units. An enemy stun on the hero lasts at most 1.5 s; death, revive and teleports end friendly stuns.
		public bool Stun;

		// Blocks skill casting (not basic attacks).
		public bool Silence;

		// Forces enemies to target the buff source.
		public bool Taunt;

		// 0 = no periodic effects.
		public float TickInterval;

		// Run with Caster = the buff holder and Instigator = whoever applied the buff.
		[SerializeReference]
		public List<ISkillEffect> OnTick = new List<ISkillEffect>();
	}
}
