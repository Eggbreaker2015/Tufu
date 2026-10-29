using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.UI.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Amulet
{
	public class AmuletUnlockView : PopupView
	{
		[SerializeField]
		private Button openInventoryButton;

		protected override void Awake()
		{
			// Native ELF VA 0x13cb210; bind once for the pooled popup instance.
			base.Awake();
			openInventoryButton.onClick.AddListener(HandleOpenInventory);
			GameContext.Get<IAudioPlayer>().PlaySoundOnce("features_pop_up");
		}

		private void HandleOpenInventory()
		{
			// Native ELF VA 0x13cb388; release the popup background before revealing inventory.
			Close();
			GameContext.Get<InventoryView>().OpenForEmptyAmuletReveal();
		}
	}
}
