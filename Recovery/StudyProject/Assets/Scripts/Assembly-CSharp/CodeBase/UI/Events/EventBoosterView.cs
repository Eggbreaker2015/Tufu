using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events;
using Codebase.Events.Models;
using Codebase.In_apps;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Events
{
	public class EventBoosterView : BaseView
	{
		[Header("Common")]
		[SerializeField]
		private Button buyButton;

		[SerializeField]
		private Image icon;

		[SerializeField]
		private TMP_Text nameText;

		[SerializeField]
		private TMP_Text costText;

		[SerializeField]
		private TMP_Text remainingText;

		[Header("States")]
		[SerializeField]
		private GameObject availableState;

		[SerializeField]
		private GameObject soldOutState;

		[Header("Button Sprites")]
		[SerializeField]
		private Sprite gemButtonSprite;

		[SerializeField]
		private Sprite iapButtonSprite;

		[SerializeField]
		private Sprite disabledButtonSprite;

		[SerializeField]
		private Image firstCurrencyImage;

		[SerializeField]
		private Image secondCurrencyImage;

		[SerializeField]
		private TMP_Text firstCurrencyText;

		[SerializeField]
		private TMP_Text secondCurrencyText;

		[Header("VIP")]
		[SerializeField]
		private GameObject vipPointsContainer;

		[SerializeField]
		private TMP_Text vipPointsAmount;

		[Inject]
		private IAPManager iapManager;

		[Inject]
		private EventStyleConfig eventStyleConfig;

		private EventBoosterModel model;

		protected override void Awake()
		{
		}

		public void Setup(EventBoosterModel boosterModel)
		{
		}

		protected override void Render()
		{
		}

		private void RenderGemBooster()
		{
		}

		private void RenderIAPBooster()
		{
		}

		private void UpdateRemainingText()
		{
		}

		private void UpdateNameText()
		{
		}

		private void OnBuyClicked()
		{
		}

		private void LateUpdate()
		{
		}

		public override void Cleanup()
		{
		}
	}
}
