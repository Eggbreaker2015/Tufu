using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Crystals
{
	public class AmuletCrystalsEnchantmentTutorialView : PopupView
	{
		[SerializeField]
		private Button closeButton;

		[Inject]
		private PlayerModel playerModel;

		protected override void Awake()
		{
            // Native ELF0x13a7d98.
            base.Awake(); closeButton.onClick.AddListener(OnCloseClick);
        }

		private void OnCloseClick()
		{
            // Native ELF0x13a7e34.
            playerModel.Data.SeenAmuletCrystalsEnchantment.Value = true; Close();
        }
	}
}
