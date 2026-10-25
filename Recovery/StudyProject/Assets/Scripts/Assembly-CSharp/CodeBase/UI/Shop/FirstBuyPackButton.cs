using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Shop
{
    public class FirstBuyPackButton : View
    {
        [SerializeField] private Button button;
        [SerializeField] private GameObject notification;
        [Inject] private PlayerModel playerModel;
        [Inject] private UserModel userModel;
        [Inject] private UINavigator navigator;

        // Original ARM64 ELF VA 0x1623b58 and click closure 0x1623c30.
        protected override void Start()
        {
            base.Start();
            Initialize();
            button.onClick.AddListener(() => navigator.Open<FirstBuyPackView>("Assets/Prefabs/UI/FirstBuyPack.prefab", false));
        }
        protected override void Render()
        {
            // Local learning adaptation requested by the user: original release 0x1623c04 always hid this entry.
            bool claimed = userModel.FirstBuyPackRewardsGranted.Value;
            bool paid = userModel.InAppsBoughtValueInRewarded.Value > 0f;
            notification.SetActive(paid && !claimed);
            gameObject.SetActive(!claimed);
        }
    }
}
