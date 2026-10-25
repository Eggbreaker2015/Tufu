using System;
using System.Collections.Generic;
using Codebase.Meta.Upgrades;
using Codebase.Utilities;

namespace Codebase.Amulet.Upgrades
{
	public class AmuletUpgradeDescriptionContext
	{
		private const string COLOR_TAG = "#FFD500";

		private readonly UpgradeDef def;

		private readonly Dictionary<string, Func<string>> variables;

		public AmuletUpgradeDescriptionContext(string upgradeKey, AmuletUpgradesModel upgradesModel, bool showOnlyCurrent)
		{
			// Original ELF VA 0x15b8aa0, including its two captured value evaluators.
			UpgradeModel upgrades = upgradesModel.Upgrades;
			def = upgrades?.GetDef(upgradeKey);
			int level = upgrades?.GetLevel(upgradeKey) ?? 0;
			bool hasNext = def != null && level < def.MaxLevel;
			bool useStatModifier = def != null && def.Modifiers.Count == 0 && def.StatModifiers.Count > 0;
			double ValueAtLevel(int targetLevel) => upgrades == null ? 0.0 : useStatModifier
				? upgrades.GetStatModifierValueAtLevel(upgradeKey, 0, targetLevel)
				: upgrades.GetModifierValueAtLevel(upgradeKey, 0, targetLevel);
			double current = ValueAtLevel(level);
			double next = hasNext ? ValueAtLevel(level + 1) : current;
			variables = new Dictionary<string, Func<string>>
			{
				["value"] = () => FormatProgression(current, next, hasNext && !showOnlyCurrent)
			};
		}

		private string FormatProgression(double current, double next, bool hasNext)
		{
			string text = FormatValue(current);
			return hasNext ? text + " > " + FormatValue(next) : text;
		}

		public string Resolve(string template)
		{
			string text = ProcessColorTags(template);
			foreach (var variable in variables)
			{
				string token = "{" + variable.Key + "}";
				if (text.Contains(token)) text = text.Replace(token, WrapInColor(variable.Value()));
			}
			return text;
		}

		private string ProcessColorTags(string text)
		{
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
			return "<color=" + COLOR_TAG + ">" + text + "</color>";
		}

		private string FormatValue(double value)
		{
			return def != null ? def.FormatDescriptionValue(value) : value.Format();
		}
	}
}
