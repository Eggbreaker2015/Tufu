using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
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
		}

		public override void Initialize()
		{
		}

		private void HandleOpenInventory()
		{
		}
	}
}
