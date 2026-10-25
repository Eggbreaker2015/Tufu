using Codebase.Utilities;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Amulet
{
	public class AmuletGraphLevelUnlockView : PopupView
	{
		[SerializeField]
		private TMP_Text unlockTitleText;

		[SerializeField]
		private TMP_Text levelNumberText;

		[SerializeField]
		private Button continueButton;

		[Inject]
		private IAudioPlayer audioPlayer;

		protected override void Awake()
		{
			// Native ELF VA 0x13c9f90.
			base.Awake(); continueButton.onClick.AddListener(HandleContinue);
		}

		public void Setup(int level)
		{
			// Native ELF VA 0x13ca02c.
			unlockTitleText.text = LocalizationStrings.Get("AMULET_GRAPH_LEVEL_UNLOCKED").Replace("{value}",level.ToString());
			levelNumberText.text = level.ToString();
		}

		public override void Initialize()
		{
			// Native ELF VA 0x13ca110.
			base.Initialize(); audioPlayer.PlaySoundOnce("features_pop_up");
		}

		private void HandleContinue()
		{
			// Native ELF VA 0x13ca1dc.
			Close();
		}
	}
}
