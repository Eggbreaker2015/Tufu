using System;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Amulet
{
	public class AmuletTutorialView : PopupView
	{
		[SerializeField]
		private Button closeButton;

		[Inject]
		private PlayerModel playerModel;

		private Action onClosed;

		protected override void Awake()
		{
			// Native ELF VA 0x13cb0b4.
			base.Awake(); closeButton.onClick.AddListener(OnCloseClick);
		}

		public void Setup(Action onClosed)
		{
			// Native ELF VA 0x13cb150.
			this.onClosed = onClosed;
		}

		private void OnCloseClick()
		{
			// Native ELF VA 0x13cb158; capture and clear before Close/pool reuse.
			playerModel.Data.SeenAmuletGraphTutorial.Value = true;
			Action callback = onClosed; onClosed = null; Close(); callback?.Invoke();
		}
	}
}
