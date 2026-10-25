using TMPro;
using UnityEngine;

namespace Codebase.UI
{
	[RequireComponent(typeof(TMP_Text))]
	public class LocalizedText : MonoBehaviour
	{
		[SerializeField]
		private string key;

		private TMP_Text textField;

		private void Awake()
		{
// Native ELF VA 0x15f804c: initial localization before event subscription.
            textField = GetComponent<TMP_Text>();
            Localize();
            global::Codebase.Localization.LocalizationService.OnLocaleChanged += Localize;
		}

		private void OnDestroy()
		{
// Native ELF VA 0x15f816c: remove the same instance delegate.
            global::Codebase.Localization.LocalizationService.OnLocaleChanged -= Localize;
		}

		public void SetKey(string key)
		{
// Native ELF VA 0x15f8210 tail-calls Localize after storing the key.
            this.key = key;
            Localize();
		}

		private void Localize()
		{
// Native ELF VA 0x15f8138; no invented empty-key/text guard.
            textField.text = global::Codebase.Utilities.LocalizationStrings.Get(key);
		}
	}
}
