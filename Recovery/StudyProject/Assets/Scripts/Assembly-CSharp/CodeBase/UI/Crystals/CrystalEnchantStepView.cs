using BrunoMikoski.AnimationSequencer;
using UnityEngine;

namespace Codebase.UI.Crystals
{
	public class CrystalEnchantStepView : MonoBehaviour
	{
		[SerializeField]
		private GameObject completedObject;

		[SerializeField]
		private AnimationSequencerController completedAnimation;

		public void SetCompleted(bool completed)
		{
            // Native ELF0x13b3e9c.
            completedObject.SetActive(completed);
            if (!completed) { completedAnimation.ClearPlayingSequence(); completedAnimation.ResetToInitialState(); }
        }

		public void PlayCompletedAnimation()
		{
            // Native ELF0x13b3f04.
            completedAnimation.ClearPlayingSequence(); completedAnimation.ResetToInitialState(); completedAnimation.Play();
        }
	}
}
