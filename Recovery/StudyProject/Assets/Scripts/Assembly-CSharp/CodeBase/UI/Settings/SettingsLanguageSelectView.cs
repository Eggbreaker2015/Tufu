using Codebase.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Settings
{
	public class SettingsLanguageSelectView : PopupView
	{
		private static readonly string[] LanguageOrder = { "en", "de", "es", "fr", "it", "pl", "pt-BR", "ru", "tr", "ja", "ko", "uk", "zh" };

		[SerializeField]
		private Transform languagesRoot;

		[SerializeField]
		private SettingsLanguageOptionView languageOptionPrefab;

		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private SettingsLanguageOption[] languageOptions = { new SettingsLanguageOption("en"), new SettingsLanguageOption("de"), new SettingsLanguageOption("es"), new SettingsLanguageOption("fr"), new SettingsLanguageOption("it"), new SettingsLanguageOption("pl"), new SettingsLanguageOption("pt-BR"), new SettingsLanguageOption("ru"), new SettingsLanguageOption("tr"), new SettingsLanguageOption("ja"), new SettingsLanguageOption("ko"), new SettingsLanguageOption("uk"), new SettingsLanguageOption("zh") };

		private ViewContainer<SettingsLanguageOptionView, string> languagesContainer;

		protected override void Awake()
		{
// Original ELF VA 0x162fa8c.
            base.Awake();
            closeButton.onClick.AddListener(Close);
		}

		private new void Start()
		{
// Original ELF VA 0x162fb28 and generated closures 0x16306d0/0x16308d0/0x1630928.
            languagesContainer = new ViewContainer<SettingsLanguageOptionView, string>(languagesRoot, languageOptionPrefab)
                .BindTo(Lifetime, () => LanguageOrder)
                .Setup((view, locale) =>
                {
                    var option = GetLanguageOption(locale);
                    view.Setup(locale, option.Flag, locale == global::Codebase.Localization.LocalizationService.CurrentLocale,
                        () => SelectLanguage(locale));
                });
		}

		private SettingsLanguageOption GetLanguageOption(string locale)
		{
// Original ELF VA 0x162fd18; exact ordinal equality, missing entry throws.
            foreach (var option in languageOptions)
                if (option.Locale == locale) return option;
            throw new global::System.InvalidOperationException("Language option for locale '" + locale + "' is not configured");
		}

		private void SelectLanguage(string locale)
		{
// Original ELF VA 0x162fe00; SetLocale before user-close and navigator-close callbacks.
            global::Codebase.Localization.LocalizationService.SetLocale(locale);
            Close();
		}
	}
}
