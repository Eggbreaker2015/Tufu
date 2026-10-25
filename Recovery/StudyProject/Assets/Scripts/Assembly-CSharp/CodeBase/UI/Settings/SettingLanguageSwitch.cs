using Codebase.Core;
using Codebase.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Settings
{
	public class SettingLanguageSwitch : MonoBehaviour
	{
		[SerializeField]
		private Button button;

		[Inject]
		private UINavigator navigator;

		private void Awake()
		{
// Original ELF VA 0x162dffc.
            button.onClick.AddListener(OpenLanguageSelect);
		}

		private void OpenLanguageSelect()
		{
// Original ELF VA 0x162e08c; exact original Addressable key, default background=false.
            navigator.Open<SettingsLanguageSelectView>("Assets/Prefabs/UI/Settings/SettingsLanguageSelect.prefab");
		}
	}
}
