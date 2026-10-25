using Multicast.UserData;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Crystals;
using Codebase.Player.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Crystals
{
	public class CrystalsTutorialView : PopupView
	{
		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private AmuletCrystalItemView itemView;

		[SerializeField]
		private AmuletCrystalItemView secondItemView;

		[Inject]
		private PlayerModel playerModel;

		protected override void Awake()
		{
            // Native ELF0x13bb430.
            base.Awake(); closeButton.onClick.AddListener(OnCloseClick);
        }

		public override void Initialize()
		{
            // Native ELF0x13bb4cc.
            base.Initialize(); Injector.InjectInto(itemView); itemView.SetInteractable(false); itemView.Setup(CreatePreviewCrystal());
            Injector.InjectInto(secondItemView); secondItemView.SetInteractable(false); secondItemView.Setup(CreatePreviewCrystal());
        }

		private CrystalItemModel CreatePreviewCrystal()
		{
            // Native ELF0x13bb58c.
            var data = UdRoot.Create(args => new CrystalItemUd(args)).Value;
            data.Level.Value = 1; data.Rarity.Value = 1; data.BaseStatType.Value = 3; data.Variant.Value = 0; data.EnchantLevel.Value = 2;
            return new CrystalItemModel(Lifetime, data);
        }

		private void OnCloseClick()
		{
            // Native ELF0x13bb7ac.
            playerModel.Data.SeenCrystalsTutorial.Value = true; Close();
        }
	}
}
