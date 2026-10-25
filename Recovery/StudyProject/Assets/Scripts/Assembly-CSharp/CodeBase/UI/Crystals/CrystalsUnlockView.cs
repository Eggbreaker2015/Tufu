using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Crystals
{
	public class CrystalsUnlockView : PopupView
	{
		[SerializeField]
		private Button continueButton;
		[Inject]
		private UINavigator navigator;
		[Inject]
		private IAudioPlayer audioPlayer;
		protected override void Awake()
		{
			base.Awake();
			continueButton.onClick.AddListener(HandleContinue);
		}

		public override void Initialize()
		{
			base.Initialize();
			audioPlayer.PlaySoundOnce("features_pop_up");
		}

		private void HandleContinue()
		{
			Close();
			navigator.Open<AmuletCrystalsView>("Assets/Prefabs/UI/Amulet/Crystals/AmuletCrystalsView.prefab", false);
		}
	}
}
