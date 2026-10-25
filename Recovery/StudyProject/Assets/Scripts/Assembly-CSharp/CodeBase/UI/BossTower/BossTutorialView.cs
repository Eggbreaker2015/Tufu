using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.BossTower
{
	public class BossTutorialView : PopupView
	{
		[SerializeField]
		private Button closeButton;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private UINavigator navigator;

		private bool openTowerAfter;

		protected override void Awake()
		{
			// Native ELF VA 0x13c7ddc.
			base.Awake();
			closeButton.onClick.AddListener(OnCloseClick);
		}

		private void OnCloseClick()
		{
			// Native ELF VA 0x13c7e78; open the hub before closing this popup.
			playerModel.Data.SeenBossTutorial.Value = true;
			if (openTowerAfter) navigator.Open<BossTowerHubView>("Assets/Prefabs/UI/BossTower/BossTowerHub.prefab").Setup(null);
			Close();
		}

		public void Setup(bool openTowerAfter)
		{
			this.openTowerAfter = openTowerAfter;
		}
	}
}
