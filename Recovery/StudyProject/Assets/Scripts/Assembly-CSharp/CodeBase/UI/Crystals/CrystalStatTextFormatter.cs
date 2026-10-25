using System;
using System.Linq;
using System.Globalization;
using Codebase.Utilities;
using System.Text;
using Codebase.Crystals;
using Codebase.Player;
using Codebase.Unit;
using TMPro;
using UnityEngine;

namespace Codebase.UI.Crystals
{
	public static class CrystalStatTextFormatter
	{
		public struct CrystalStatsParts
		{
			public string Base;

			public string Special;

			public string Legendary;
		}

		public struct CrystalStatNameValue
		{
			public string Name;

			public string Value;
		}

		public const string HighlightColor = "#FFD500";

		public const string BaseColor = "white";

		public const string SuccessColor = "#B9B9B9";

		public const string NextValueColor = "#90E810";

		public const string PreviewColor = "#C851FF";

		public const string SellSprite = "atlas_98";

		public const string SuccessSprite = "atlas_101";

		public const string TransitionArrow = "atlas_114";

		public static string WrapColor(string text, string color)
		{
            // Native ELF0x13a9268.
            return "<color=" + color + ">" + text + "</color>";
        }

		public static string FormatSellPrice(double price)
		{
            // Native ELF0x13a8b78.
            return "<sprite name=atlas_98> " + price.Format();
        }

		public static string FormatDustCashback(double value)
		{
            // Native ELF0x13ba2a8.
            return "+" + value.Format() + " <sprite name=atlas_98>";
        }

		public static string FormatSuccessChance(float chance)
		{
            // Native ELF0x13a8d30.
            return "<color=#B9B9B9>" + LocalizationStrings.Get("CRYSTALS_SUCCESS_CHANCE") + " -</color> " + FormatChancePercent(chance) + "% <sprite name=atlas_101>";
        }

		public static string FormatGuaranteeEnchantProgress(int remaining)
		{
            // Native ELF0x13a93ac.
            return LocalizationStrings.Get("CRYSTALS_GUARANTEED_ENCHANT_IN").Replace("{value}", WrapColor(remaining.ToString(), HighlightColor));
        }

		public static string FormatChancePercent(float value)
		{
            // Native ELF0x13ba348.
            if (value <= 0f) return "0";
            double logarithm = Math.Floor(Math.Log10(Math.Abs(value)));
            int exponent = double.IsNaN(logarithm) ? 0 : double.IsPositiveInfinity(logarithm) ? int.MinValue : logarithm >= int.MaxValue ? int.MaxValue : logarithm <= int.MinValue ? int.MinValue : (int)logarithm;
            int digits = Math.Max(0, unchecked(1 - exponent));
            return value.ToString("F" + digits.ToString(), CultureInfo.InvariantCulture);
        }

		public static string FormatStatLine(IconStyleConfig iconStyleConfig, StatType statType, double value, bool isPercent, string valueColor = null)
		{
            // Native ELF0x13b0bb8.
            var sb = new StringBuilder(); AppendStatIcon(sb, iconStyleConfig, statType);
            sb.Append(LocalizationStrings.Get(string.Format("STAT_{0}", statType))).Append(' ');
            AppendValue(sb, value, isPercent, valueColor); return sb.ToString();
        }

		public static string FormatStatLineWithPreview(IconStyleConfig iconStyleConfig, StatType statType, double current, double next, bool isPercent, bool showPreview)
		{
            // Native ELF0x13ba798.
            var sb = new StringBuilder(); AppendStatIcon(sb, iconStyleConfig, statType);
            sb.Append(LocalizationStrings.Get(string.Format("STAT_{0}", statType))).Append("  +").Append(current.Format());
            if (isPercent) sb.Append('%');
            if (next > current && showPreview) {
                sb.Append("<color=#C851FF><sprite name=\"atlas_114\" color=#C851FF> +").Append(next.Format());
                if (isPercent) sb.Append('%'); sb.Append("</color>");
            }
            return sb.ToString();
        }

		public static Sprite GetStatIconSprite(IconStyleConfig iconStyleConfig, StatType statType)
		{
            // Native ELF0x13a96f4.
            return iconStyleConfig.Styles.FirstOrDefault(style => style.StatType == statType)?.Sprite;
        }

		public static CrystalStatNameValue FormatStatNameValue(IconStyleConfig iconStyleConfig, StatType statType, double value, bool isPercent, string valueColor = null, bool includeIcon = true)
		{
            // Native ELF0x13a945c.
            var name = new StringBuilder(); if (includeIcon) AppendStatIcon(name, iconStyleConfig, statType);
            name.Append(LocalizationStrings.Get(string.Format("STAT_{0}", statType)));
            var result = new StringBuilder(); AppendValue(result, value, isPercent, valueColor);
            return new CrystalStatNameValue { Name = name.ToString(), Value = result.ToString() };
        }

		public static CrystalStatNameValue FormatLegendaryAbilityNameValue(CrystalModel crystalModel, LegendaryAbilityType ability, string valueColor = null, CrystalItemModel crystal = null, int? enchantLevelOverride = null, bool includeIcon = true)
		{
            // Native ELF0x13a9874.
            if (ability == LegendaryAbilityType.None) return default;
            var sb = new StringBuilder();
            if (includeIcon) { var icon = crystalModel.Config.GetLegendaryAbilityIcon(ability); if (icon != null) sb.Append("<sprite name=").Append(icon.name).Append("> "); }
            string template = LocalizationStrings.Get(string.Format("CRYSTALS_LEGENDARY_{0}", ability));
            double value = enchantLevelOverride.HasValue ? CrystalBalance.GetLegendaryAbilityValue(crystalModel.Config, ability, enchantLevelOverride.Value) : crystal != null ? CrystalBalance.GetLegendaryAbilityValue(crystalModel.Config, crystal) : CrystalBalance.GetLegendaryAbilityValue(crystalModel.Config, ability, 0);
            SplitLegendaryTemplate(template, value, out string name, out string valueText); sb.Append(name);
            if (!string.IsNullOrEmpty(valueText) && !string.IsNullOrEmpty(valueColor)) valueText = WrapColor(valueText, valueColor);
            return new CrystalStatNameValue { Name = sb.ToString(), Value = valueText };
        }

		private static void SplitLegendaryTemplate(string template, double value, out string name, out string valueText)
		{
            // Native ELF0x13baa9c.
            int index = template.IndexOf("{value}", StringComparison.Ordinal);
            if (index < 0) { name = template; valueText = string.Empty; return; }
            string before = template.Substring(0, index).TrimEnd(); string after = template.Substring(index + "{value}".Length);
            string sign = "+";
            if (before.Length > 0 && (before[before.Length - 1] == '-' || before[before.Length - 1] == '+')) {
                sign = before[before.Length - 1].ToString(); before = before.Substring(0, before.Length - 1).TrimEnd();
            }
            string percent = string.Empty;
            if (after.StartsWith("%", StringComparison.Ordinal)) { percent = "%"; after = after.Substring(1); }
            after = after.Trim();
            name = before.Length > 0 && after.Length > 0 ? before + " " + after : before.Length == 0 ? after : before;
            valueText = sign + value.Format() + percent;
        }

		public static bool IsLegendarySpecialStat(LegendaryAbilityType ability)
		{
            // Native ELF0x13bad78.
            return (uint)ability < 12u && (0x809u & (1u << (int)ability)) != 0;
        }

		public static CrystalStatsParts BuildCrystalStatsParts(CrystalModel crystalModel, IconStyleConfig iconStyleConfig, CrystalItemModel crystal, bool showPreview = false, bool respectOwnHide = true)
		{
            // Native ELF0x13b4118.
            var parts = new CrystalStatsParts(); bool preview = showPreview && !crystalModel.IsAtMaxEnchantLevel(crystal);
            string baseLine = showPreview ? FormatStatLineWithPreview(iconStyleConfig, crystal.BaseStatType, crystal.BaseStatValue, crystalModel.CalculatePreviewBaseStatValue(crystal), false, preview) : FormatStatLine(iconStyleConfig, crystal.BaseStatType, crystal.BaseStatValue, false);
            parts.Base = WrapColor(baseLine, BaseColor);
            if (crystal.HasSpecialStat) {
                float current = crystalModel.GetSpecialStatValue(crystal, crystal.SpecialStatType, crystal.EnchantLevel);
                string special = showPreview ? FormatStatLineWithPreview(iconStyleConfig, crystal.SpecialStatType, current, crystalModel.GetSpecialStatValue(crystal, crystal.SpecialStatType, unchecked(crystal.EnchantLevel + 1)), true, preview) : FormatStatLine(iconStyleConfig, crystal.SpecialStatType, current, true);
                parts.Special = WrapColor(special, HighlightColor);
            }
            if (crystal.HasSecondaryStat) {
                string secondary = showPreview ? FormatStatLineWithPreview(iconStyleConfig, crystal.SecondaryStatType, crystal.SecondaryStatValue, crystalModel.CalculatePreviewSecondaryStatValue(crystal), false, preview) : FormatStatLine(iconStyleConfig, crystal.SecondaryStatType, crystal.SecondaryStatValue, false);
                parts.Legendary = WrapColor(secondary, BaseColor);
            } else if (crystal.HasLegendaryAbility) {
                var definition = crystalModel.Config.GetLegendaryAbilityDef(crystal.LegendaryAbility);
                if (!(respectOwnHide && definition != null && definition.HideInOwnStats)) {
                    string line = showPreview ? FormatLegendaryAbilityLineWithPreview(crystalModel, crystal.LegendaryAbility, CrystalBalance.GetLegendaryAbilityValue(crystalModel.Config, crystal.LegendaryAbility, crystal.EnchantLevel), CrystalBalance.GetLegendaryAbilityValue(crystalModel.Config, crystal.LegendaryAbility, unchecked(crystal.EnchantLevel + 1)), preview) : FormatLegendaryAbilityLine(crystalModel, crystal.LegendaryAbility, null, crystal);
                    if (!string.IsNullOrEmpty(line)) parts.Legendary = WrapColor(line, IsLegendarySpecialStat(crystal.LegendaryAbility) ? HighlightColor : BaseColor);
                }
            }
            return parts;
        }

		public static void ApplyStatsParts(CrystalStatsParts parts, TMP_Text baseText, TMP_Text specialText, TMP_Text legendaryText)
		{
            // Native ELF0x13b47dc.
            ApplyPart(baseText, parts.Base); ApplyPart(specialText, parts.Special); ApplyPart(legendaryText, parts.Legendary);
        }

		private static void ApplyPart(TMP_Text target, string text)
		{
            // Native ELF0x13bb318.
            if (target == null) return;
            bool empty = string.IsNullOrEmpty(text); target.gameObject.SetActive(!empty);
            if (!empty) target.text = text;
        }

		public static string BuildEnchantToUnlockText(CrystalStyleConfig styleConfig, CrystalRarity rarity)
		{
            // Native ELF0x13b4588.
            var icon = styleConfig.GetEnchantmentIcon(1);
            string name = icon != null ? icon.name : string.Empty;
            string sprite = "<size=140%><sprite name=\"" + name + "\" color=" + styleConfig.GetRarityColorCode(rarity) + "></size>";
            return "<align=center><color=#EEBA9B>" + LocalizationStrings.Get("CRYSTALS_ENCHANT_TO_UNLOCK").Replace("{sprite}", sprite);
        }

		public static string FormatLegendaryAbilityLineWithPreview(CrystalModel crystalModel, LegendaryAbilityType ability, double currentValue, double nextValue, bool showPreview)
		{
            // Native ELF0x13bad90.
            if (ability == LegendaryAbilityType.None) return string.Empty;
            var sb = new StringBuilder(); var icon = crystalModel.Config.GetLegendaryAbilityIcon(ability);
            if (icon != null) sb.Append("<sprite name=").Append(icon.name).Append("> ");
            string template = LocalizationStrings.Get(string.Format("CRYSTALS_LEGENDARY_{0}", ability));
            int index = template.IndexOf("{value}", StringComparison.Ordinal);
            if (index < 0) return sb.Append(template).ToString();
            string before = template.Substring(0, index), after = template.Substring(index + "{value}".Length), sign = string.Empty;
            string trimmed = before.TrimEnd();
            if (trimmed.Length > 0 && (trimmed[trimmed.Length - 1] == '-' || trimmed[trimmed.Length - 1] == '+')) sign = trimmed[trimmed.Length - 1].ToString();
            string percent = string.Empty;
            if (after.StartsWith("%", StringComparison.Ordinal)) { percent = "%"; after = after.Substring(1); }
            sb.Append(before).Append(currentValue.Format()).Append(percent);
            if (nextValue > currentValue && showPreview) sb.Append("<color=#C851FF><sprite name=\"atlas_114\" color=#C851FF> ").Append(sign).Append(nextValue.Format()).Append(percent).Append("</color>");
            return sb.Append(after).ToString();
        }

		public static string FormatLegendaryAbilityLine(CrystalModel crystalModel, LegendaryAbilityType ability, string valueColor = null, CrystalItemModel crystal = null)
		{
            // Native ELF0x13bb28c.
            if (ability == LegendaryAbilityType.None) return string.Empty;
            double value = crystal != null ? CrystalBalance.GetLegendaryAbilityValue(crystalModel.Config, crystal) : CrystalBalance.GetLegendaryAbilityValue(crystalModel.Config, ability, 0);
            return FormatLegendaryAbilityLine(crystalModel, ability, value, valueColor);
        }

		public static string FormatLegendaryAbilityLine(CrystalModel crystalModel, LegendaryAbilityType ability, double totalValue, string valueColor = null)
		{
            // Native ELF0x13b0954.
            if (ability == LegendaryAbilityType.None) return string.Empty;
            var sb = new StringBuilder(); var icon = crystalModel.Config.GetLegendaryAbilityIcon(ability);
            if (icon != null) sb.Append("<sprite name=").Append(icon.name).Append("> ");
            string value = totalValue.Format(); if (!string.IsNullOrEmpty(valueColor)) value = WrapColor(value, valueColor);
            return sb.Append(LocalizationStrings.Get(string.Format("CRYSTALS_LEGENDARY_{0}", ability)).Replace("{value}", value)).ToString();
        }

		private static void AppendStatIcon(StringBuilder sb, IconStyleConfig iconStyleConfig, StatType statType)
		{
            // Native ELF0x13ba490.
            var icon = iconStyleConfig.Styles.FirstOrDefault(style => style.StatType == statType)?.Sprite;
            if (icon != null) sb.Append("<sprite name=\"").Append(icon.name).Append("\"> ");
        }

		private static void AppendValue(StringBuilder sb, double value, bool isPercent, string valueColor)
		{
            // Native ELF0x13ba640.
            bool color = !string.IsNullOrEmpty(valueColor);
            if (color) sb.Append("<color=").Append(valueColor).Append('>');
            sb.Append('+').Append(value.Format()); if (isPercent) sb.Append('%');
            if (color) sb.Append("</color>");
        }
	}
}
