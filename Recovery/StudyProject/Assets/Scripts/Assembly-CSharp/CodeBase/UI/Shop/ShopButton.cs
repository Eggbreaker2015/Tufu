using Codebase.Analytics.Events;
using UniMob;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player.Models;
using Codebase.Shop;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Shop
{
	public class ShopButton : View
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private GameObject notification;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private IAnalyticsWrapper analytics;

		[Inject]
		private ShopLotRepository shopLotRepository;

		[Inject]
		private UINavigator navigator;

		protected override void Awake()
		{
            // Native 0x16254c8 and closure 0x1625658; persistent authored button calls remain intact.
            base.Awake();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => analytics.Send(new OpenShopEvent(playerModel.LastUnlockedLocation.Number)));
		}

		protected override void Start()
		{
            // Native 0x162557c.
            base.Start();
            Atom.Reaction(Lifetime, Render);
		}

		protected override void Render()
		{
            // Native 0x1625618.
            notification.SetActive(shopLotRepository.CanClaimSomething);
		}
	}
}
