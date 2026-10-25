using System;
using System.Collections.Generic;
using System.Text;
using Codebase.Abilities.Core;
using Codebase.Core;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class AddStatPassiveEffect : AbilityEffectBase
	{
		private readonly struct DesiredModifier
		{
			public readonly StatType StatType;

			public readonly ModifierType ModifierType;

			public readonly ModifierSource Source;

			public readonly float Value;

			public DesiredModifier(StatType statType, ModifierType modifierType, ModifierSource source, float value)
			{
			// Native ELF VA 0x15ed11c.
			StatType=statType; ModifierType=modifierType; Source=source; Value=value;
		}
		}

		private readonly struct AppliedModifier
		{
			public readonly StatType StatType;

			public readonly StatModifier Modifier;

			public AppliedModifier(StatType statType, StatModifier modifier)
			{
			// Native ELF VA 0x15ed310.
			StatType=statType; Modifier=modifier;
		}
		}

		public StatType StatType;

		public ModifierType ModifierType;

		public ModifierSource Source;

		public AbilityValueScaling ValueScaling;

		[Tooltip("If true, the scale is normalized so that it reaches 100% at full HP and 0% at the threshold. Otherwise, raw (healthPercent - threshold) * 100 is used.")]
		public bool NormalizeAboveThreshold;

		public StatType ScalingStat;

		[NonSerialized]
		private string cachedKey;

		private static readonly Dictionary<string, DesiredModifier> DesiredModifiers = new Dictionary<string, DesiredModifier>(); // Native 0x15ed454.

		private static readonly Dictionary<string, AppliedModifier> AppliedModifiers = new Dictionary<string, AppliedModifier>();

		private static readonly List<string> KeysToRemove = new List<string>();

		private static readonly StringBuilder KeyBuilder = new StringBuilder(64);

		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
			// Native ELF VA 0x15ecd14; normalize the original scaled value by the above-threshold fraction.
			float value=ValueScaler.GetScaledValue(context,ValueScaling,ScalingStat);
			if(NormalizeAboveThreshold && ValueScaling==AbilityValueScaling.PerHealthAbovePercent) {float remaining=1f-ValueScaler.ResolveHealthAboveThreshold(context);if(remaining>0f)value/=remaining;}
			if(cachedKey==null) cachedKey=BuildKey(context.Def.Key,StatType,ModifierType,Source);
			PublishDesired(cachedKey,StatType,ModifierType,Source,value);
		}

		public static void PublishDesired(string key, StatType statType, ModifierType modifierType, ModifierSource source, float value)
		{
			// Native ELF VA 0x15ecfb8; duplicate keys keep the largest positive value, rather than sum.
			if(value<=0f)return;
			if(DesiredModifiers.TryGetValue(key,out DesiredModifier existing)&&value<=existing.Value)return;
			DesiredModifiers[key]=new DesiredModifier(statType,modifierType,source,value);
		}

		public static string BuildKey(string abilityKey, StatType statType, ModifierType modifierType, ModifierSource source)
		{
			// Native ELF VA 0x15ece54; raw numeric enum fields, original vertical-bar separator.
			KeyBuilder.Clear(); KeyBuilder.Append(abilityKey);KeyBuilder.Append('|');KeyBuilder.Append((int)statType);KeyBuilder.Append('|');KeyBuilder.Append((int)modifierType);KeyBuilder.Append('|');KeyBuilder.Append((int)source);
			return KeyBuilder.ToString();
		}

		public static void SyncModifiers(UnitCore unit)
		{
			// Native ELF VA 0x15eab18; remove disappeared keys, retain approximately-equal values, then clear this frame's desired set.
			if(unit==null)return;
			KeysToRemove.Clear();
			foreach(var applied in AppliedModifiers)if(!DesiredModifiers.ContainsKey(applied.Key))KeysToRemove.Add(applied.Key);
			foreach(string key in KeysToRemove)RemoveAppliedModifier(unit,key);
			foreach(var desired in DesiredModifiers) {
			    if(AppliedModifiers.TryGetValue(desired.Key,out AppliedModifier applied)) {
			        if(applied.StatType==desired.Value.StatType&&Mathf.Approximately(applied.Modifier.Value,desired.Value.Value))continue;
			        RemoveAppliedModifier(unit,desired.Key);
			    }
			    Stat stat=FindStat(unit,desired.Value.StatType);if(stat==null)continue;
			    var modifier=new StatModifier {Name=desired.Key,StatType=desired.Value.StatType,ModifierType=desired.Value.ModifierType,Source=desired.Value.Source,Value=desired.Value.Value,Duration=0f};
			    stat.AddModifier(modifier);AppliedModifiers[desired.Key]=new AppliedModifier(desired.Value.StatType,modifier);
			}
			DesiredModifiers.Clear();
		}

		private static Stat FindStat(UnitCore unit, StatType statType)
		{
			// Native ELF VA 0x15ed240.
			foreach(Stat stat in unit.Stats)if(stat.Type==statType)return stat;
			return null;
		}

		private static void RemoveAppliedModifier(UnitCore unit, string key)
		{
			// Native ELF VA 0x15ed12c.
			if(!AppliedModifiers.TryGetValue(key,out AppliedModifier applied))return;
			Stat stat=FindStat(unit,applied.StatType);if(stat!=null)stat.RemoveModifier(applied.Modifier);
			AppliedModifiers.Remove(key);
		}

		public static void ClearOnUnitChange()
		{
			// Native ELF VA 0x15eaaa0; original only clears bookkeeping for the prior unit.
			AppliedModifiers.Clear();
		}

		[StaticReset]
		private static void ResetStaticState()
		{
			// Native ELF VA 0x15ed320.
			DesiredModifiers.Clear();AppliedModifiers.Clear();KeysToRemove.Clear();KeyBuilder.Clear();
		}
	}
}
