using System;
using Codebase.Core.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Settings
{
	public class SettingsLanguageOptionView : View
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private Image flagImage;

		[SerializeField]
		private TMP_Text languageNameText;

		private string locale;

		private Sprite flag;

		private bool isSelected;

		private Action onClick;

		protected override void Awake()
		{
// Original ELF VA 0x162f81c.
            base.Awake();
            button.onClick.AddListener(HandleClick);
		}

		public void Setup(string locale, Sprite flag, bool isSelected, Action onClick)
		{
// Original ELF VA 0x162f8b8; field writes precede Initialize.
            this.locale = locale;
            this.flag = flag;
            this.isSelected = isSelected;
            this.onClick = onClick;
            Initialize();
		}

		protected override void Render()
		{
// Original ELF VA 0x162f91c; each language names itself and uses its own font.
            flagImage.sprite = flag;
            languageNameText.text = global::Codebase.Utilities.LocalizationStrings.Get("SETTING_CurrentLanguage", locale);
            global::Codebase.Localization.LocalizationFontApplier.SetLocaleOverride(languageNameText, locale);
            button.interactable = !isSelected;
		}

		public override void Cleanup()
		{
// Original ELF VA 0x162f9f0.
            base.Cleanup();
            global::Codebase.Localization.LocalizationFontApplier.ClearLocaleOverride(languageNameText);
            onClick = null;
		}

		private void HandleClick()
		{
// Original ELF VA 0x162fa68.
            onClick?.Invoke();
		}
	}
}
