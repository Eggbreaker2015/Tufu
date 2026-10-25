using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player.Inventory;
using Codebase.PromoCodes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Settings
{
	public class SettingPromoCodeView : PopupView
	{
		[SerializeField]
		private TMP_InputField inputField;

		[SerializeField]
		private Button pasteButton;

		[SerializeField]
		private Button submitButton;

		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private GameObject invalidPromoMarker;

		[SerializeField]
		private GameObject invalidPromoText;

		[Inject]
		private PromoCodesModel promoCodesModel;

		[Inject]
		private ItemsProvider itemsProvider;

		[Inject]
		private UINavigator uiNavigator;

		[Inject]
		private IAnalyticsWrapper analytics;

		protected override void Awake()
		{
		}

		public override void Initialize()
		{
		}

		private void OnPaste()
		{
		}

		private void OnInputChanged(string value)
		{
		}

		private void OnSubmit()
		{
		}

		private void ShowInvalid()
		{
		}

		private void HideInvalid()
		{
		}
	}
}
