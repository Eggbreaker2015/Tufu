using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.UI.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Events
{
	public class EventTutorialView : PopupView
	{
		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private InventoryItemView itemView;

		[SerializeField]
		private ItemConfig itemConfig;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private IAudioPlayer audioPlayer;

		protected override void Awake()
		{
			// Original APK ARM64 ELF VA 0x16607b8.
			base.Awake();
			closeButton.onClick.AddListener(OnCloseClick);
			var item = new ItemModel(Lifetime, itemConfig) { Level = 1 };
			itemView.Initialize(item, false);
		}

		private void OnCloseClick()
		{
			// Original APK ARM64 ELF VA 0x16608d8.
			audioPlayer.PlaySoundOnce("tap");
			playerModel.Data.SeenEventTutorial.Value = true;
			Close();
		}
	}
}
