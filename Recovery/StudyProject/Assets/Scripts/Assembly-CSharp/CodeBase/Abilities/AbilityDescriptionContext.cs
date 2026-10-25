using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Codebase.Abilities.Conditions;
using Codebase.Abilities.Effects;
using Codebase.Core;
using Codebase.Meta;
using Codebase.Unit;
using Codebase.Utilities;
using UnityEngine;

namespace Codebase.Abilities
{
	public class AbilityDescriptionContext
	{
		private readonly Dictionary<string, Func<string>> variables;

		private string economyProcBonusSuffix = string.Empty;

		private bool hideNextValue;

		private const string COLOR_TAG = "#FFD500";

		private const string EconomyBonusColorHex = "8BF07A";

		public AbilityDescriptionContext(AbilityState abilityState)
		{
			// Native ELF VA 0x15e1058 and its eight captured delegates.
			AbilityDef def = abilityState.Def;
			bool scalesWithStars = abilityState.HasNextUpgrade;
			WeaponStatStackEffect stack = def.Effects.OfType<WeaponStatStackEffect>().FirstOrDefault();
			AddStatEffect stat = def.Effects.OfType<AddStatEffect>().FirstOrDefault();
			float duration = stack != null ? stack.Duration : stat != null ? stat.Duration : 0f;
			StatType statType = stat != null ? stat.StatType : StatType.Health;
			variables = new Dictionary<string, Func<string>>
			{
				["value"] = () => FormatValue(abilityState.GetBaseValue(), abilityState.GetNextStarValue(), scalesWithStars, false),
				["value_percent"] = () => FormatValue(abilityState.GetBaseValue() * 100f, abilityState.GetNextStarValue() * 100f, scalesWithStars, true),
				["value_percent_minus_one"] = () => FormatValue(abilityState.GetBaseValue() * 100f - 100f, abilityState.GetNextStarValue() * 100f - 100f, scalesWithStars, true),
				["duration"] = () => duration.ToString(CultureInfo.InvariantCulture),
				["stack_limit"] = () => FormatValue(abilityState.GetBaseStackLimit(), abilityState.GetNextStarStackLimit(), scalesWithStars, false),
				["max_marks"] = () => FormatValue(abilityState.GetBaseMaxMarksCount(), abilityState.GetNextStarMaxMarksCount(), scalesWithStars, false),
				["chance"] = () =>
				{
					string result = FormatValue(abilityState.GetBaseChance() * 100f, abilityState.GetNextStarChance() * 100f, scalesWithStars, true);
					economyProcBonusSuffix = string.Empty;
					if (IsEconomyAbility(def))
					{
						float bonus = ComputeEconomyProcBonus() * 100f;
						if (bonus > 0f) economyProcBonusSuffix = " <color=#8BF07A>(+" + FormatNumber(bonus) + "%)</color>";
					}
					return result;
				},
				["stat_type"] = () => LocalizationStrings.Get("STAT_" + statType)
			};
		}

		public string Resolve(string template, bool hideNextValue = false)
		{
			// Native ELF VA 0x15e1768; evaluate only placeholders present in the template.
			economyProcBonusSuffix = string.Empty;
			this.hideNextValue = hideNextValue;
			string text = ProcessColorTags(template);
			foreach (var variable in variables)
			{
				string token = "{" + variable.Key + "}";
				if (!text.Contains(token)) continue;
				string replacement = WrapInColor(variable.Value());
				if (variable.Key == "chance" && economyProcBonusSuffix.Length > 0) replacement += economyProcBonusSuffix;
				text = text.Replace(token, replacement);
			}
			return text;
		}

		private string ProcessColorTags(string text)
		{
			// Native ELF VA 0x15e1c34: paired dollars mark literal highlighted text.
			while (text.Contains("$"))
			{
				int start = text.IndexOf('$');
				int end = text.IndexOf('$', start + 1);
				if (end == -1) return text;
				text = text.Substring(0, start) + WrapInColor(text.Substring(start + 1, end - start - 1)) + text.Substring(end + 1);
			}
			return text;
		}

		private string WrapInColor(string text)
		{
			// Native ELF VA 0x15e1d3c.
			return "<color=#FFD500>" + text + "</color>";
		}

		private string FormatValue(float currentValue, float nextValue, bool scalesWithStars, bool addPercent)
		{
			// Native ELF VA 0x15e1da8; both values use the original preview color.
			string current = FormatNumber(currentValue);
			if (addPercent) current += "%";
			if (scalesWithStars && !hideNextValue && !Mathf.Approximately(currentValue, nextValue))
			{
				string next = FormatNumber(nextValue);
				if (addPercent) next += "%";
				return "<color=#D388FF>" + current + " > " + next + "</color>";
			}
			return current;
		}

		public static bool IsEconomyAbility(AbilityDef def)
		{
			// Native ELF VA 0x15e21a0: chance condition plus a permanent stat effect.
			if (def.Conditions == null) return false;
			bool chance = false;
			foreach (var condition in def.Conditions)
				if (condition is ChanceCondition) { chance = true; break; }
			if (!chance || def.Effects == null) return false;
			foreach (var effect in def.Effects)
				if (effect is AddStatEffect stat && stat.Source == ModifierSource.AbilityPermanent) return true;
			return false;
		}

		private static float ComputeEconomyProcBonus()
		{
			// Native ELF VA 0x15e2338.
			MetaStatsModel model = GameContext.Get<MetaStatsModel>();
			MetaStat stat = model?.Get("economy_item_abilities_chance");
			return stat == null ? 0f : (float)stat.Value;
		}

		private string FormatNumber(float value)
		{
			// Native 0x15e1fec -> eight-byte outline at 0x297b040 -> 0x15e2008.
			// Original .rodata 0x920e58 is float32 0.01, not Mathf.Approximately.
			float rounded = Mathf.Round(value);
			if (Mathf.Abs(value - rounded) < 0.01f) return rounded.ToString("F0", CultureInfo.InvariantCulture);
			if (value >= 1f) return value.ToString(CultureInfo.InvariantCulture);
			return value.ToString("F4", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
		}
	}
}
