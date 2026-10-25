using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events;
using Codebase.UI.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI
{
	public class PurchaseRewardView : PopupView
	{
		[SerializeField]
		private Image icon;

		[SerializeField]
		private TMP_Text amountText;

		[SerializeField]
		private Button closeButton;

		[Inject]
		private ShopSectionsConfig shopConfig;

		[Inject]
		private EventsProvider eventsProvider;

		protected override void Awake()
		{
		}

		public void Setup(Sprite sprite, string amount)
		{
		}

		public void Setup(string rewardType, string amount)
		{
		}

		private Sprite ResolveIcon(string rewardType)
		{
			return null;
		}
	}
}
