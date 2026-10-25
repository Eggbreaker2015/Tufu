using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Codebase.Unit;
using UniMob;
using UniMob.Core;

namespace Codebase.Meta
{
	[Serializable]
	public sealed class MetaStat : StatBase
	{
		public string Type;

		[CompilerGenerated]
		private int __ModifierVersion__BackingField;

		private MetaStatDataUd udData;

		private readonly Dictionary<ModifierSource, double> preAddPostMultBySource;

		private ComputedAtom<int> __ModifierVersion;

		public int ModifierVersion
		{
			get
			{
			    LifetimeScopeExtension.ThrowIfDisposed(this);
			    if (__ModifierVersion == null) __ModifierVersion = CodeGenAtom.CreatePooled(this, null, () => ModifierVersion, false);
			    return __ModifierVersion.DirectEvaluate() ? __ModifierVersion__BackingField : __ModifierVersion.Value;
			}
			private set
			{
			    LifetimeScopeExtension.ThrowIfDisposed(this);
			    if (__ModifierVersion != null && !__ModifierVersion.CompareAndInvalidate(value)) return;
			    __ModifierVersion__BackingField = value;
			}
		}

		public MetaStat(Lifetime lifetime, double baseValue, string type)
			: base(lifetime, baseValue)
		{
			// Native ELF VA 0x1485b24.
			preAddPostMultBySource = new Dictionary<ModifierSource, double>();
			Type = type;
			UpdateValue();
		}

		public void BindToUserData(MetaStatDataUd ud)
		{
			// Native ELF VA 0x1485bf4 preserves reverse saved-modifier order.
			udData = ud;
			for (int index = ud.Modifiers.Count - 1; index >= 0; index--)
			{
			    StatModifierUd saved = ud.Modifiers[index];
			    if (IsTemporarySource(saved.Source.Value)) { ud.Modifiers.RemoveAt(index); continue; }
			    Modifiers.Add(new StatModifier
			    {
			        Name = saved.Name.Value, Value = saved.Value.Value,
			        ModifierType = saved.ModifierType.Value, Source = saved.Source.Value,
			        Param = saved.Param.Value, StringParam = saved.StringParam.Value
			    });
			}
			UpdateValue();
			ModifierVersion++;
		}

		public double GetValue(int param = 0)
		{
			// Native ELF VA 0x14857f8 / 0x148641c.
			TrackModifiers();
			return CalculateValue(param, null);
		}

		public double GetValue(string stringParam)
		{
			// Native ELF VA 0x14857f8 / 0x148641c.
			TrackModifiers();
			return CalculateValue(0, stringParam);
		}

		public bool HasModifierForParam(int param)
		{
			// Native ELF VA 0x148644c.
			TrackModifiers();
			foreach (StatModifier modifier in Modifiers)
			    if (modifier.Param == param && string.IsNullOrEmpty(modifier.StringParam)) return true;
			return false;
		}

		private void TrackModifiers()
		{
			// Native ELF VA 0x1485eec tracks the base Value atom.
			_ = Value;
		}

		protected override void OnModifierAdded(StatModifier modifier)
		{
			// Native ELF VA 0x1486634.
			ModifierVersion++;
			if (udData == null || IsTemporarySource(modifier.Source)) return;
			WriteModifierToUd(udData.Modifiers.Create(), modifier);
		}

		protected override void OnModifierUpdated(StatModifier modifier)
		{
			// Native ELF VA 0x148681c; existing save updates only Value.
			ModifierVersion++;
			if (udData == null || IsTemporarySource(modifier.Source)) return;
			foreach (StatModifierUd saved in udData.Modifiers)
			    if (saved.Name.Value == modifier.Name && saved.Source.Value == modifier.Source && saved.ModifierType.Value == modifier.ModifierType)
			    {
			        saved.Value.Value = modifier.Value;
			        return;
			    }
			WriteModifierToUd(udData.Modifiers.Create(), modifier);
		}

		protected override void OnModifierRemoved(StatModifier modifier)
		{
			// Native ELF VA 0x1486c28; remove the first saved matching identity.
			ModifierVersion++;
			if (udData == null || IsTemporarySource(modifier.Source)) return;
			for (int index = 0; index < udData.Modifiers.Count; index++)
			{
			    StatModifierUd saved = udData.Modifiers[index];
			    if (saved.Name.Value == modifier.Name && saved.Source.Value == modifier.Source && saved.ModifierType.Value == modifier.ModifierType)
			    {
			        udData.Modifiers.RemoveAt(index);
			        return;
			    }
			}
		}

		private static bool IsTemporarySource(ModifierSource source)
		{
			// Native ELF VA 0x1485ed4, unsigned range and exact original bit mask.
			return (uint)source < 16U && (0xef0aU & (1U << (int)source)) != 0;
		}

		private static void WriteModifierToUd(StatModifierUd modUd, StatModifier modifier)
		{
			// Native ELF VA 0x14866dc.
			modUd.Name.Value = modifier.Name;
			modUd.Value.Value = modifier.Value;
			modUd.ModifierType.Value = modifier.ModifierType;
			modUd.Source.Value = modifier.Source;
			modUd.Param.Value = modifier.Param;
			modUd.StringParam.Value = modifier.StringParam;
		}

		private double CalculateValue(int intParam, string stringParam)
		{
			// Native ELF VA 0x1485ef4. Meta modifiers filter by parameter before combining.
			double flat = 0.0, additive = 1.0, multiplicative = 1.0;
			preAddPostMultBySource.Clear();
			foreach (StatModifier modifier in Modifiers)
			{
			    if (!MatchesParam(modifier, intParam, stringParam)) continue;
			    switch (modifier.ModifierType)
			    {
			        case ModifierType.Flat: flat += modifier.Value; break;
			        case ModifierType.PercentAdditive: additive += modifier.Value; break;
			        case ModifierType.PercentMultiplicative: multiplicative *= modifier.Value; break;
			        case ModifierType.PreAdditivePostMultiplicative:
			            preAddPostMultBySource.TryGetValue(modifier.Source, out double current);
			            preAddPostMultBySource[modifier.Source] = current + modifier.Value;
			            break;
			    }
			}
			double result = multiplicative * additive * (BaseValue + flat);
			foreach (var pair in preAddPostMultBySource) result *= pair.Value + 1.0;
			return result;
		}

		public static bool ModifierMatchesParam(StatModifier modifier, int intParam, string stringParam)
		{
			// Native ELF VA 0x1487080 duplicates MatchesParam.
			return MatchesParam(modifier, intParam, stringParam);
		}

		private static bool MatchesParam(StatModifier modifier, int intParam, string stringParam)
		{
			// Native ELF VA 0x1487008: global integer-zero modifiers also affect string queries.
			if (!string.IsNullOrEmpty(modifier.StringParam)) return modifier.StringParam == stringParam;
			if (!string.IsNullOrEmpty(stringParam)) return modifier.Param == 0;
			return modifier.Param == 0 || modifier.Param == intParam;
		}
	}
}
