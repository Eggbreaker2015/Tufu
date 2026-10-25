using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Crystals;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Crystals
{
	public class HudCrystalsButton : View
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private GameObject content;

		[SerializeField]
		private GameObject notification;

		[Inject]
		private CrystalModel crystalModel;

		[Inject]
		private UINavigator navigator;

		[Inject]
		private IAudioPlayer audioPlayer;

		protected override void Awake()
		{
            // Native ELF0x13bbb08.
            base.Awake(); button.onClick.AddListener(OnButtonClick); Initialize();
        }

		protected override void Render()
		{
            // Native ELF0x13bbbb4.
            content.SetActive(crystalModel.IsUnlocked); notification.SetActive(crystalModel.IsUnlocked && crystalModel.GeodesCount > 0.0);
        }

		private void OnButtonClick()
		{
            // Native ELF0x13bbc3c.
            audioPlayer.PlaySoundOnce("tap"); navigator.Open<AmuletCrystalsView>("Assets/Prefabs/UI/Amulet/Crystals/AmuletCrystalsView.prefab");
        }
	}
}
