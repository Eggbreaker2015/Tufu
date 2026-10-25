using System;
using System.Runtime.CompilerServices;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player;
using Codebase.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.RewardedOffer
{
	public class RewardedOfferLotView : View
	{
		[SerializeField]
		private Image icon;

		[SerializeField]
		private TMP_Text amountText;

		[SerializeField]
		private Button buyButton;

		[SerializeField]
		private TMP_Text costText;

		[Inject]
		private IconStyleConfig iconStyle;

		private ShopLotModel model;

		[CompilerGenerated]
		private Action<ShopLotModel> m_OnBuyClicked;

		public event Action<ShopLotModel> OnBuyClicked
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public void Setup(ShopLotModel lot)
		{
		}

		protected override void Render()
		{
		}

		private void HandleBuyClicked()
		{
		}

		public override void Cleanup()
		{
		}
	}
}
