using System.Collections.Generic;
using Codebase.Core;
using TMPro;
using UnityEngine;

namespace Codebase.Localization
{
	public static class LocalizationFontApplier
	{
		private struct FontOrigin
		{
			public TMP_FontAsset Font;

			public Material Material;

			public bool IsClear;
		}

		private const string ClearKeyword = "Clear";

		private static readonly List<GameObject> roots = new List<GameObject>();

		private static readonly Dictionary<TMP_Text, FontOrigin> origins = new Dictionary<TMP_Text, FontOrigin>();

		private static readonly Dictionary<TMP_Text, string> localeOverrides = new Dictionary<TMP_Text, string>();

		private static readonly List<TMP_Text> textBuffer = new List<TMP_Text>();

		private static readonly List<TMP_SubMeshUI> subMeshBuffer = new List<TMP_SubMeshUI>();

		private static LocalizationFontSettings settings;

		private static bool settingsLoaded;

		private static bool subscribed;

		public static void Register(GameObject root)
		{
			EnsureSubscribed();
			if (!roots.Contains(root)) roots.Add(root);
			ApplyToTree(root);
		}

		// For a text created at runtime next to an already-localized one: take over that text's original
		// font and material, so a later locale switch restores both to the same font.
		public static void RegisterLike(TMP_Text text, TMP_Text source)
		{
			if (source != null && origins.TryGetValue(source, out var origin))
			{
				text.font = origin.Font;
				text.fontSharedMaterial = origin.Material;
			}
			var config = GetSettings();
			if (config != null) ApplyToText(text, config);
		}

		public static void Unregister(GameObject root)
		{
			roots.Remove(root);
		}

		public static void SetLocaleOverride(TMP_Text text, string locale)
		{
			localeOverrides[text] = locale;
			var config = GetSettings();
			if (config != null) ApplyToText(text, config);
		}

		public static void ClearLocaleOverride(TMP_Text text)
		{
			localeOverrides.Remove(text);
			var config = GetSettings();
			if (config != null) ApplyToText(text, config);
		}

		private static void EnsureSubscribed()
		{
			if (subscribed) return;
			LocalizationService.OnLocaleChanged += ReapplyAll;
			subscribed = true;
		}

		private static void ReapplyAll()
		{
			for (int i = roots.Count - 1; i >= 0; i--)
				if (roots[i] == null) roots.RemoveAt(i); else ApplyToTree(roots[i]);
		}

		private static void ApplyToTree(GameObject root)
		{
			var config = GetSettings();
			if (config == null) return;
			root.GetComponentsInChildren(true, textBuffer);
			for (int i = 0; i < textBuffer.Count; i++) ApplyToText(textBuffer[i], config);
			textBuffer.Clear();
		}

		private static void ApplyToText(TMP_Text text, LocalizationFontSettings config)
		{
			if (!origins.TryGetValue(text, out var origin))
			{
				origin = new FontOrigin { Font = text.font, Material = text.fontSharedMaterial,
					IsClear = text.fontSharedMaterial != null && text.fontSharedMaterial.name.Contains(ClearKeyword) };
				origins[text] = origin;
				text.OnPreRenderText += _ => ApplyFallbackMaterials(text);
			}
			if (TryGetFontBinding(text, config, out var binding) && binding.FontAsset != null)
			{
				text.font = binding.FontAsset;
				text.fontSharedMaterial = origin.IsClear ? binding.ClearMaterial : binding.FontAsset.material;
			}
			else { text.font = origin.Font; text.fontSharedMaterial = origin.Material; }
		}

		private static void ApplyFallbackMaterials(TMP_Text text)
		{
			if (!origins.TryGetValue(text, out var origin) || !origin.IsClear) return;
			var config = GetSettings();
			if (!TryGetFontBinding(text, config, out var binding) || text.font != binding.FontAsset) return;
			text.GetComponentsInChildren(true, subMeshBuffer);
			for (int i = 0; i < subMeshBuffer.Count; i++)
			{
				var subMesh = subMeshBuffer[i];
				if (subMesh.textComponent == text && config.TryGetFallbackClearMaterial(subMesh.fontAsset, out var material) && subMesh.sharedMaterial != material)
					subMesh.sharedMaterial = material;
			}
			subMeshBuffer.Clear();
		}

		private static bool TryGetFontBinding(TMP_Text text, LocalizationFontSettings config, out LocalizationFontSettings.LocaleFontBinding binding)
		{
			var locale = localeOverrides.TryGetValue(text, out var local) ? local : ResolveFontLocale(LocalizationService.CurrentLocale);
			return config.TryGetLocaleBinding(locale, out binding);
		}

		private static string ResolveFontLocale(string locale)
		{
			return locale == "uk" ? "ru" : locale;
		}

		private static LocalizationFontSettings GetSettings()
		{
			if (!settingsLoaded) { settings = Resources.Load<LocalizationFontSettings>(LocalizationFontSettings.ResourcesPath); settingsLoaded = true; }
			return settings;
		}

		[StaticReset]
		private static void ResetStaticState()
		{
			if (subscribed) { LocalizationService.OnLocaleChanged -= ReapplyAll; subscribed = false; }
			roots.Clear(); origins.Clear(); textBuffer.Clear(); subMeshBuffer.Clear();
			settings = null; settingsLoaded = false;
		}
	}
}
