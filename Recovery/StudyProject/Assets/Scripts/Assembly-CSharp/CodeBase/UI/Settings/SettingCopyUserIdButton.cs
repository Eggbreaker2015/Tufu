using BrunoMikoski.AnimationSequencer;
using Codebase.Analytics;
using Codebase.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Settings
{
	public class SettingCopyUserIdButton : MonoBehaviour
	{
		[Inject]
		private UserModel userModel;

		[SerializeField]
		private Button button;

		[SerializeField]
		private AnimationSequencerController animationController;

		private void Awake()
		{
// Original ELF VA 0x162de74.
            button.onClick.AddListener(CopyUserId);
		}

		private void CopyUserId()
		{
// Original ELF VA 0x162df04; native virtual slots 17,22,8 checked in original metadata.
            GUIUtility.systemCopyBuffer = userModel.UserId.Value;
            animationController.Complete(true);
            animationController.ClearPlayingSequence();
            animationController.ResetToInitialState();
            animationController.Play();
		}
	}
}
