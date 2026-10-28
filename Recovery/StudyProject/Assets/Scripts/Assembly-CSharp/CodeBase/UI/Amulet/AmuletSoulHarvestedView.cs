using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.UI.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Amulet
{
	public class AmuletSoulHarvestedView : PopupView
	{
		[SerializeField]
		private Button openInventoryButton;

		[Inject]
		private IAudioPlayer audioPlayer;

		protected override void Awake()
		{
			// Native ELF VA 0x13caec0.
			base.Awake();
			openInventoryButton.onClick.AddListener(HandleOpenInventory);
		}

		public override void Initialize()
		{
			// Native ELF VA 0x13caf5c; play again when the popup is reused.
			base.Initialize();
			audioPlayer.PlaySoundOnce("features_pop_up");
		}

		private void HandleOpenInventory()
		{
			// Native ELF VA 0x13cb028.
			Close();
			GameContext.Get<InventoryView>().OpenForAmuletReveal();
		}
	}
}
