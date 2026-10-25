using Codebase.Utilities;
using BrunoMikoski.AnimationSequencer;
using TMPro;
using UnityEngine;

namespace Codebase.UI.Crystals
{
	public class CrystalEnchantLifeItemView : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text romanText;

		[SerializeField]
		private AnimationSequencerController consumedAnimation;

		public void Setup(int enchantNumber)
		{
            // Native ELF0x13b3d78.
            romanText.text = RomanNumerals.ToRoman(enchantNumber); ResetVisuals();
        }

		public void ResetVisuals()
		{
            // Native ELF0x13b3e08.
            consumedAnimation.ClearPlayingSequence(); consumedAnimation.ResetToInitialState();
        }

		public void PlayConsumedAnimation()
		{
            // Native ELF0x13b3e40.
            consumedAnimation.ClearPlayingSequence(); consumedAnimation.ResetToInitialState(); consumedAnimation.Play();
        }
	}
}
