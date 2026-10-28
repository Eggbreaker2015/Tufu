using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Codebase.Core;
using Codebase.Pet;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Utilities;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Codebase.UI
{
	// Pet mode: text the item views show for a weapon's pet instead of the weapon's damage.
	public static class PetStatsText
	{
		public const float RefreshInterval = 0.5f;

		private const string Arrow = " <sprite name=atlas_59> ";

		private const string NextColor = "<color=#7CFF7C>";

		private const string NameColor = "<color=#FFD54A>";

		// OS fonts for the pet names' CJK glyphs the bundled static atlases lack (胖, 兵, 矛).
		private static readonly string[,] SystemFontFaces =
		{
			{ "PingFang SC", "Regular" }, { "Hiragino Sans GB", "W3" }, { "Heiti SC", "Medium" }, { "STHeiti", "Medium" },
			{ "Noto Sans CJK SC", "Regular" }, { "Microsoft YaHei", "Regular" }, { "SimHei", "Regular" }
		};

		private static readonly string[] SystemFontFiles = { "PingFang", "Hiragino Sans GB", "STHeiti", "NotoSansCJK", "NotoSansSC", "DroidSansFallback", "msyh", "simhei" };

		private static readonly StringBuilder builder = new StringBuilder();

		private static readonly Dictionary<TMP_FontAsset, TMP_FontAsset> wrappedFonts = new Dictionary<TMP_FontAsset, TMP_FontAsset>();

		private static readonly HashSet<TMP_FontAsset> runtimeFonts = new HashSet<TMP_FontAsset>();

		private static TMP_FontAsset systemFont;

		private static bool systemFontLoaded;

		// Font file loaded for the OS-path fallback; the face-name path has none.
		private static Font systemSourceFont;

		private static bool releaseHooked;

		[StaticReset]
		private static void ResetStaticState()
		{
			ReleaseFonts();
			builder.Clear();
		}

		// The runtime fonts are plain (not DontSave) objects freed here when play mode stops or the app quits,
		// before a domain reload drops the static references to them.
		private static void ReleaseFonts()
		{
			foreach (TMP_FontAsset font in runtimeFonts)
				if (font != null) Object.DestroyImmediate(font);
			if (systemFont != null)
			{
				// The wrapped copies share the source atlases; only the OS font asset owns its atlas and material.
				if (systemFont.atlasTextures != null)
					foreach (Texture2D atlas in systemFont.atlasTextures)
						if (atlas != null) Object.DestroyImmediate(atlas);
				if (systemFont.material != null) Object.DestroyImmediate(systemFont.material);
				Object.DestroyImmediate(systemFont);
			}
			if (systemSourceFont != null) Object.DestroyImmediate(systemSourceFont);
			wrappedFonts.Clear();
			runtimeFonts.Clear();
			systemFont = null;
			systemSourceFont = null;
			systemFontLoaded = false;
		}

		private static void HookRelease()
		{
			if (releaseHooked) return;
			releaseHooked = true;
			Application.quitting += ReleaseFonts;
		}

		// Pet attack (own + player share, without the typed percent stat) at another weapon damage, 'levelsRemoved' pet levels lower.
		public static double AttackAt(PetStatsInfo info, double weaponDamage, int levelsRemoved)
		{
			PetStatRatios ratios = PetStats.GetRatios(info.Definition, info.Item.Rarity, info.Level - levelsRemoved);
			return PetStats.GetOwnAttack(weaponDamage, info.Definition) + info.PlayerAttack * ratios.Attack;
		}

		// Pet attack one copy adds; every copy, evolved or not, is one pet level (PetStats.GetLevel).
		public static double AttackPerCopy(PetStatsInfo info)
		{
			return PetStats.GetOwnAttack(info.Item.StatPerCopy, info.Definition) + info.PlayerAttack * (info.NextRatios.Attack - info.Ratios.Attack);
		}

		// Pet attack the last 'copies' copies added.
		public static double AttackGain(PetStatsInfo info, int copies)
		{
			return info.Attack - AttackAt(info, info.Item.Damage - info.Item.StatPerCopy * copies, copies);
		}

		// Same terms as the original weapon DPS of InventoryStatView, with the pet's attack, its inherited percent stat and its attack rate.
		public static double Dps(PetStatsInfo info, PlayerModel playerModel)
		{
			ItemModel item = info.Item;
			return info.AttackPercent * (info.Attack + (double)item.AbilityDamageFlat + (double)playerModel.AbilityDamageFlat) *
			    (double)item.AbilityDamageMultiplier * (double)playerModel.AbilityDamageMultiplier * (double)info.AttacksPerSecond;
		}

		// The same pet one level up (NextRatios / NextOwnAttack), for a diff.
		public static PetStatsInfo Next(PetStatsInfo info)
		{
			info.Ratios = info.NextRatios;
			info.OwnAttack = info.NextOwnAttack;
			return info;
		}

		public static string Build(PetStatsInfo info, bool includeName)
		{
			return Build(info, info, includeName);
		}

		// Lines of the pet's stats; values that change from 'from' to 'to' get an arrow and the new value.
		public static string Build(PetStatsInfo from, PetStatsInfo to, bool includeName)
		{
			builder.Clear();
			string name = to.Definition != null ? to.Definition.DisplayName : null;
			if (includeName && !string.IsNullOrEmpty(name)) builder.Append(NameColor).Append(name).Append("</color>\n");
			builder.Append("生命 ").Append(Ratio(from.Ratios.Health)).Append(" (").Append(from.Health.Format()).Append(')');
			if (Changed(from.Health, to.Health)) builder.Append(Arrow).Append(NextColor).Append(Ratio(to.Ratios.Health)).Append(" (").Append(to.Health.Format()).Append(")</color>");
			builder.Append("\n攻击 ").Append(from.Attack.Format()).Append(" (自身 ").Append(from.OwnAttack.Format()).Append(" + 玩家 ").Append(from.PlayerAttack.Format())
			    .Append(" × ").Append(from.Ratios.Attack.ToString("0.00", CultureInfo.InvariantCulture)).Append(')');
			if (Changed(from.Attack, to.Attack)) builder.Append(Arrow).Append(NextColor).Append(to.Attack.Format()).Append("</color>");
			builder.Append("\n防御 ").Append(Ratio(from.Ratios.Defence));
			if (Changed(from.Ratios.Defence, to.Ratios.Defence)) builder.Append(Arrow).Append(NextColor).Append(Ratio(to.Ratios.Defence)).Append("</color>");
			builder.Append("\n移速 ").Append(Ratio(from.Ratios.MoveSpeed));
			if (Changed(from.Ratios.MoveSpeed, to.Ratios.MoveSpeed)) builder.Append(Arrow).Append(NextColor).Append(Ratio(to.Ratios.MoveSpeed)).Append("</color>");
			builder.Append(" (战斗中)");
			builder.Append("\n攻速 每秒 ").Append(to.AttacksPerSecond.ToString("0.##", CultureInfo.InvariantCulture)).Append(" 次");
			return builder.ToString();
		}

		private static string Ratio(float ratio)
		{
			// "倍" reads clearer than a bare "×" suffix, which also showed as a missing glyph in the context menu.
			return ratio.ToString("0.00", CultureInfo.InvariantCulture) + "倍";
		}

		private static bool Changed(double from, double to)
		{
			return Math.Abs(to - from) > Math.Max(Math.Abs(from), Math.Abs(to)) * 1e-6;
		}

		// A rich text block cloned from one of the view's labels (keeps its font, material and colour), hidden until used.
		public static TMP_Text CreateBlock(TMP_Text template, Transform parent, string name)
		{
			GameObject clone = Object.Instantiate(template.gameObject, parent, false);
			clone.name = name;
			// Drop LocalizedText and layout helpers: the block's text is set by code only.
			foreach (MonoBehaviour behaviour in clone.GetComponents<MonoBehaviour>())
				if (!(behaviour is TMP_Text)) Object.DestroyImmediate(behaviour);
			// Child objects (sub meshes, icons) belong to the template; TMP recreates its sub meshes as needed.
			for (int i = clone.transform.childCount - 1; i >= 0; i--)
				Object.DestroyImmediate(clone.transform.GetChild(i).gameObject);
			TMP_Text text = clone.GetComponent<TMP_Text>();
			text.text = string.Empty;
			text.richText = true;
			text.raycastTarget = false;
			text.textWrappingMode = TextWrappingModes.NoWrap;
			text.overflowMode = TextOverflowModes.Overflow;
			text.alignment = TextAlignmentOptions.TopLeft;
			text.enableAutoSizing = true;
			text.fontSizeMin = 16f;
			text.lineSpacing = 0f;
			RectTransform rect = text.rectTransform;
			rect.anchorMin = new Vector2(0f, 1f);
			rect.anchorMax = new Vector2(1f, 1f);
			rect.pivot = new Vector2(0.5f, 1f);
			clone.SetActive(false);
			return text;
		}

		// Item title: in pet mode the item is its pet species, so the species name replaces the weapon name.
		public static void SetItemName(TMP_Text text, ItemConfig config)
		{
			PetDefinition definition = PetConfig.Enabled && config != null && config.SlotType != ItemSlotType.Helmet && config.SlotType != ItemSlotType.Shield ? PetConfig.Instance.Find(config) : null;
			if (definition != null && !string.IsNullOrEmpty(definition.DisplayName))
				SetText(text, definition.DisplayName);
			else
				text.text = LocalizationStrings.Get("ITEM_NAME_" + config.Key);
		}

		public static void SetText(TMP_Text text, string value)
		{
			EnsureFont(text);
			if (text.text != value) text.text = value;
		}

		// Swaps the text's font for a runtime copy that also falls back to an OS CJK font; the shared font assets are not modified.
		private static void EnsureFont(TMP_Text text)
		{
			TMP_FontAsset font = text.font;
			if (font == null || runtimeFonts.Contains(font)) return;
			TMP_FontAsset fallback = GetSystemFont();
			if (fallback == null) return;
			if (!wrappedFonts.TryGetValue(font, out TMP_FontAsset wrapped) || wrapped == null)
			{
				wrapped = Object.Instantiate(font);
				wrapped.name = font.name + " (Pet)";
				var fallbacks = font.fallbackFontAssetTable != null ? new List<TMP_FontAsset>(font.fallbackFontAssetTable) : new List<TMP_FontAsset>();
				fallbacks.Add(fallback);
				wrapped.fallbackFontAssetTable = fallbacks;
				wrappedFonts[font] = wrapped;
				runtimeFonts.Add(wrapped);
			}
			// The copy shares the atlas, so the text's current (outline / clear) material still fits it.
			Material material = text.fontSharedMaterial;
			text.font = wrapped;
			if (material != null && text.fontSharedMaterial != material) text.fontSharedMaterial = material;
		}

		private static TMP_FontAsset GetSystemFont()
		{
			if (systemFontLoaded) return systemFont;
			systemFontLoaded = true;
			HookRelease();
			try
			{
				for (int i = 0; i < SystemFontFaces.GetLength(0) && systemFont == null; i++)
					systemFont = TMP_FontAsset.CreateFontAsset(SystemFontFaces[i, 0], SystemFontFaces[i, 1], 90);
				if (systemFont == null)
					foreach (string path in Font.GetPathsToOSFonts())
					{
						if (Array.FindIndex(SystemFontFiles, file => path.IndexOf(file, StringComparison.OrdinalIgnoreCase) >= 0) < 0) continue;
						Font source = new Font(path);
						systemFont = TMP_FontAsset.CreateFontAsset(source);
						if (systemFont != null)
						{
							systemSourceFont = source;
							break;
						}
						Object.DestroyImmediate(source);
					}
			}
			catch (Exception exception)
			{
				Debug.LogWarning("PetStatsText: no OS CJK font fallback (" + exception.Message + ")");
				systemFont = null;
			}
			if (systemFont == null) Debug.LogWarning("PetStatsText: no OS CJK font found; pet names may miss glyphs.");
			return systemFont;
		}
	}
}
