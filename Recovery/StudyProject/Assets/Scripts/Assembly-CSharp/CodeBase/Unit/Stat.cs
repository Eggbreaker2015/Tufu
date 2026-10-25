using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UniMob;
using UniMob.Core;

namespace Codebase.Unit
{
	[Serializable]
	public class Stat : StatBase
	{
		public StatType Type;

		[CompilerGenerated]
		private double __SpecificValue__BackingField;

		private const double CappedSpecificValueLimit = 0.8;

		private ComputedAtom<double> __SpecificValue;

		public double SpecificValue
		{
			[CompilerGenerated]
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__SpecificValue == null)
					__SpecificValue = CodeGenAtom.CreatePooled(this, null, () => SpecificValue, false);
				return __SpecificValue.DirectEvaluate() ? __SpecificValue__BackingField : __SpecificValue.Value;
			}
			[CompilerGenerated]
			private set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__SpecificValue == null || __SpecificValue.CompareAndInvalidate(value))
					__SpecificValue__BackingField = value;
			}
		}

		public double FastSpecificValue { get; private set; }

		public bool IsSpecificValueCapped => IsSpecificValueCappable(Type) && SpecificValue >= CappedSpecificValueLimit;

		public Stat(Lifetime lifetime, double baseValue, StatType type, List<StatModifier> modifiers = null)
			: base(lifetime, baseValue, modifiers)
		{
			Type = type;
			UpdateValue();
		}

		public static bool IsSpecificValueCappable(StatType type)
		{
			return type == StatType.Evasion || type == StatType.BlockChance || type == StatType.BlockAmount;
		}

		public static double ClampSpecificValue(StatType type, double value)
		{
			return IsSpecificValueCappable(type) ? Math.Min(value, CappedSpecificValueLimit) : value;
		}

		public static double CalculateSpecificValue(StatType type, double value)
		{
			switch (type)
			{
				case StatType.MovementSpeed:
					return value < 10000.0 ? Math.Pow(value, 0.115) * 4.6 : Math.Pow(value, 0.1) * 3.0 + 5.8;
				case StatType.Evasion:
					return value < 700.0 ? Math.Pow(value, 0.12) * 0.06 : Math.Pow(value, 0.065) * 0.05 + 0.06;
				case StatType.CritChance:
					return value <= 700.0 ? Math.Pow(value, 0.15) * 0.08 : Math.Pow(value, 0.1) * 0.07 + 0.082;
				case StatType.CritDamagePercent: return Math.Pow(value, 0.08) * 1.5;
				case StatType.SuperCritDamagePercent: return 2.0;
				case StatType.GlobalAttack:
				case StatType.GlobalDefence: return value <= 100.0 ? value : Math.Pow(value - 100.0, 0.75) + 100.0;
				case StatType.BlockChance: return Math.Pow(value, 0.1) * 0.08;
				case StatType.BlockAmount: return Math.Pow(value, 0.053) * 0.15;
				default: return value;
			}
		}

		protected override void UpdateValue()
		{
			var raw = CalculateValue();
			var postMultiplier = GetPostFormulaMultiplier();
			var converted = CalculateSpecificValue(Type, raw);
			SpecificValue = ClampSpecificValue(Type, postMultiplier * (converted + GetPostFormulaFlat()));
			FastSpecificValue = SpecificValue;
			FastValue = raw;
			Value = raw;
		}

		private double GetPostFormulaMultiplier()
		{
			double additive = 0.0;
			double multiplicative = 1.0;
			foreach (var modifier in Modifiers)
			{
				if (modifier.ModifierType == ModifierType.PostFormulaAdditive) additive += modifier.Value;
				else if (modifier.ModifierType == ModifierType.PostFormulaMultiplicative) multiplicative *= 1.0 + modifier.Value;
			}
			return multiplicative * (additive + 1.0);
		}

		private double GetPostFormulaFlat()
		{
			double result = 0.0;
			foreach (var modifier in Modifiers)
				if (modifier.ModifierType == ModifierType.PostFormulaFlat) result += modifier.Value;
			return result;
		}
	}
}
