using Codebase.ABTests;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.In_apps;
using Codebase.Services.Purchases;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Shop
{
	public class NoAdsBannerView : View
	{
		[SerializeField]
		private ProductKey productKey;

		[SerializeField]
		private GameObject content;

		[SerializeField]
		private GameObject purchasedLabel;

		[SerializeField]
		private Button button;

		[SerializeField]
		private TMP_Text vipPoints;

		[SerializeField]
		private TMP_Text priceText;

		[Inject]
		private ABTestService abTestService;

		[Inject]
		private UserModel userModel;

		[Inject]
		private PurchaseProcessingService purchaseService;

		[Inject]
		private IAPManager iapManager;

		private bool purchaseStarted;

		protected override void Start()
		{
		}

		private void OnEnable()
		{
		}

		protected override void Render()
		{
		}

		private void Update()
		{
		}

		private void OnButtonClicked()
		{
		}

		private void OnPurchaseFinished()
		{
		}

		public override void Cleanup()
		{
		}
	}
}
