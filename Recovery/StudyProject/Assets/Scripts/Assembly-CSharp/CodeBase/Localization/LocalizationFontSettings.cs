using System;
using TMPro;
using UnityEngine;

namespace Codebase.Localization
{
	[CreateAssetMenu(fileName = "LocalizationFontSettings", menuName = "Localization/Font Settings")]
	public class LocalizationFontSettings : ScriptableObject
	{
		[Serializable]
		public struct LocaleFontBinding
		{
			public string Locale;

			public TMP_FontAsset FontAsset;

			public Material ClearMaterial;
		}

		[Serializable]
		public struct FallbackFontMaterialBinding
		{
			public TMP_FontAsset FontAsset;

			public Material ClearMaterial;
		}

		public const string ResourcesPath = "Localization/Localization Font Settings";

		[SerializeField]
		private TMP_FontAsset[] baseFontAssets;

		[SerializeField]
		private TMP_FontAsset latinFallbackFont;

		[SerializeField]
		private LocaleFontBinding[] localeFontBindings;

		[SerializeField]
		private FallbackFontMaterialBinding[] fallbackFontMaterialBindings;

		public TMP_FontAsset[] BaseFontAssets => this.baseFontAssets;

		public TMP_FontAsset LatinFallbackFont => this.latinFallbackFont;

		public LocaleFontBinding[] LocaleFontBindings => this.localeFontBindings;

		public TMP_FontAsset GetLocaleFont(string locale)
		{
			if (localeFontBindings != null)
				foreach (var binding in localeFontBindings)
					if (binding.Locale == locale) return binding.FontAsset;
			return null;
		}

		public bool TryGetLocaleBinding(string locale, out LocaleFontBinding binding)
		{
			if (localeFontBindings != null)
				foreach (var candidate in localeFontBindings)
					if (candidate.Locale == locale) { binding = candidate; return true; }
			binding = default;
			return false;
		}

		public bool TryGetFallbackClearMaterial(TMP_FontAsset fontAsset, out Material material)
		{
			foreach (var binding in fallbackFontMaterialBindings)
				if (binding.FontAsset == fontAsset) { material = binding.ClearMaterial; return true; }
			material = null;
			return false;
		}
	}
}
