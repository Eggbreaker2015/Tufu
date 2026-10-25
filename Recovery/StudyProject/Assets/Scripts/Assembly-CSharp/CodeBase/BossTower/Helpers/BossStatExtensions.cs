using Codebase.Unit;
using System.Linq;
using UniMob;
using UnityEngine;

namespace Codebase.BossTower.Helpers
{
	public static class BossStatExtensions
	{
		public static void AddModifier(this UnitCore unit, StatType statType, ModifierType modifierType, string modifierName, float value)
		{
			// Original ELF VA 0x1575c20 and predicate 0x157609c.
			Stat stat = unit.Stats.FirstOrDefault(candidate => candidate.Type == statType);
			if (stat == null) { Debug.LogError("No stat found for " + statType); return; }
			stat.AddModifier(new StatModifier { Name = modifierName, StatType = statType, ModifierType = modifierType, Value = value });
		}

		public static void SetModifier(this UnitCore unit, StatType statType, ModifierType modifierType, string modifierName, float value)
		{
			// Original ELF VA 0x15730a0: replace only the first same-name modifier, even for zero.
			Stat stat = unit.Stats.FirstOrDefault(candidate => candidate.Type == statType);
			if (stat == null) { Debug.LogError("No stat found for " + statType); return; }
			StatModifier previous = stat.Modifiers.FirstOrDefault(modifier => modifier.Name == modifierName);
			if (previous != null) stat.RemoveModifier(previous);
			stat.AddModifier(new StatModifier { Name = modifierName, StatType = statType, ModifierType = modifierType, Value = value });
		}

		public static void RemoveModifier(this UnitCore unit, StatType statType, string modifierName)
		{
			// Original ELF VA 0x1575e2c.
			Stat stat = unit.Stats.FirstOrDefault(candidate => candidate.Type == statType);
			StatModifier previous = stat.Modifiers.FirstOrDefault(modifier => modifier.Name == modifierName);
			if (previous != null) stat.RemoveModifier(previous);
		}

		public static void EnsureScaleStat(this UnitCore unit)
		{
			// Original ELF VA 0x1575fbc.
			if (unit.TryGetStat(StatType.VisualScaleMultiplier, out _)) return;
			unit.Stats.Add(new Stat(Lifetime.Eternal, 1.0, StatType.VisualScaleMultiplier));
			unit.BuildStatLookup();
		}

		public static void SetPercentModifier(this UnitCore unit, StatType statType, string modifierName, float value)
		{
			// Original ELF VA 0x1573a6c: unlike SetModifier, zero removes without re-adding.
			Stat stat = unit.Stats.FirstOrDefault(candidate => candidate.Type == statType);
			if (stat == null) return;
			StatModifier previous = stat.Modifiers.FirstOrDefault(modifier => modifier.Name == modifierName);
			if (previous != null) stat.RemoveModifier(previous);
			if (value != 0f) stat.AddModifier(new StatModifier { Name = modifierName, StatType = statType, ModifierType = ModifierType.PercentAdditive, Value = value });
		}
	}
}
