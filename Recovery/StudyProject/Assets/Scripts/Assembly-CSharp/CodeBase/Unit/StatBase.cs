using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UniMob;
using UniMob.Core;

namespace Codebase.Unit
{
	[Serializable]
	public abstract class StatBase : ILifetimeScope
	{
		public double BaseValue;

		[CompilerGenerated]
		private double __Value__BackingField;

		private readonly Dictionary<ModifierSource, double> preAddPostMultBySource;

		private ComputedAtom<double> __Value;

		public List<StatModifier> Modifiers { get; private set; }

		public double Value
		{
			[CompilerGenerated]
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Value == null)
					__Value = CodeGenAtom.CreatePooled(this, null, () => Value, false);
				return __Value.DirectEvaluate() ? __Value__BackingField : __Value.Value;
			}
			[CompilerGenerated]
			protected set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Value == null || __Value.CompareAndInvalidate(value))
					__Value__BackingField = value;
			}
		}

		public List<StatModifier> TemporaryModifiers { get; private set; }

		public double FastValue { get; protected set; }

		public Lifetime Lifetime { get; set; }

		protected virtual void UpdateValue()
		{
			FastValue = CalculateValue();
			Value = FastValue;
		}

		protected StatBase(Lifetime lifetime, double baseValue, List<StatModifier> modifiers = null)
		{
			TemporaryModifiers = new List<StatModifier>();
			preAddPostMultBySource = new Dictionary<ModifierSource, double>();
			BaseValue = baseValue;
			Modifiers = modifiers ?? new List<StatModifier>();
		}

		public void AddModifier(StatModifier modifier)
		{
			var existing = Modifiers.FirstOrDefault(item => item.Name == modifier.Name &&
				item.Source == modifier.Source && item.ModifierType == modifier.ModifierType);
			if (existing == null)
			{
				Modifiers.Add(modifier);
				TemporaryModifiers = Modifiers.Where(item => item.Source == ModifierSource.AbilityTemporary && item.Duration > 0f).ToList();
				OnModifierAdded(modifier);
			}
			else
			{
				if (existing.ModifierType == ModifierType.PercentMultiplicative ||
					existing.ModifierType == ModifierType.PostFormulaMultiplicative)
					existing.Value *= modifier.Value;
				else
					existing.Value += modifier.Value;
				if (existing.Duration <= modifier.Duration)
					existing.Duration = modifier.Duration;
				OnModifierUpdated(existing);
			}
			UpdateValue();
		}

		public void RemoveModifier(StatModifier modifier)
		{
			Modifiers.Remove(modifier);
			TemporaryModifiers = Modifiers.Where(item => item.Source == ModifierSource.AbilityTemporary && item.Duration > 0f).ToList();
			OnModifierRemoved(modifier);
			UpdateValue();
		}

		protected virtual void OnModifierAdded(StatModifier modifier)
		{
		}

		protected virtual void OnModifierUpdated(StatModifier modifier)
		{
		}

		protected virtual void OnModifierRemoved(StatModifier modifier)
		{
		}

		protected double CalculateValue()
		{
			double flat = 0.0;
			double additiveMultiplier = 1.0;
			double multiplicativeMultiplier = 1.0;
			double eventMultiplier = 1.0;
			preAddPostMultBySource.Clear();
			foreach (var modifier in Modifiers)
			{
				switch (modifier.ModifierType)
				{
					case ModifierType.Flat: flat += modifier.Value; break;
					case ModifierType.PercentAdditive: additiveMultiplier += modifier.Value; break;
					case ModifierType.PercentMultiplicative: multiplicativeMultiplier *= modifier.Value; break;
					case ModifierType.PreAdditivePostMultiplicative:
						preAddPostMultBySource.TryGetValue(modifier.Source, out var sourceValue);
						preAddPostMultBySource[modifier.Source] = sourceValue + modifier.Value;
						break;
					case ModifierType.EventPercentModifier:
						eventMultiplier *= 1.0 + StatModifier.GetEventPercentBonusPercent(modifier.Value) * 0.01;
						break;
				}
			}
			var result = eventMultiplier * multiplicativeMultiplier * additiveMultiplier * (BaseValue + flat);
			foreach (var pair in preAddPostMultBySource)
				result *= 1.0 + pair.Value;
			return result;
		}
	}
}
