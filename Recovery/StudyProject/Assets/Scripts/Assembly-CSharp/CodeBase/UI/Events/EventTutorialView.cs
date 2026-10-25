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
		}

		private void OnCloseClick()
		{
		}
	}
}
