using BrunoMikoski.AnimationSequencer;
using TMPro;
using UnityEngine;

namespace Codebase.UI.Damage
{
	public class DamageEventView : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text damageText;

		[SerializeField]
		private CanvasGroup canvasGroup;

		[SerializeField]
		private AnimationSequencerController animationSequencerController;

		[SerializeField]
		private AnimationSequencerController specialAnimationSequencerController;

		private AnimationSequencerController currentController;

		public bool IsFinished { get; private set; }

		private void Awake()
		{
			// Native ELF VA 0x1671858; both original controllers notify the same Finish handler.
			animationSequencerController.OnFinishedEvent.AddListener(Finish);
			specialAnimationSequencerController.OnFinishedEvent.AddListener(Finish);
		}

		public void Setup(string text, bool isSpecial = false)
		{
			// Native ELF VA 0x167192c; 1.12f scale read from original rodata 0x920e74.
			damageText.text = text;
			canvasGroup.alpha = 1f;
			if (currentController != null)
			{
			    currentController.ClearPlayingSequence();
			    currentController.ResetToInitialState();
			}
			currentController = isSpecial ? specialAnimationSequencerController : animationSequencerController;
			transform.localScale = Vector3.one * (isSpecial ? 1.12f : 1f);
			currentController.Play();
			IsFinished = false;
		}

		public void Hide()
		{
			// Native ELF VA 0x1671ab4.
			canvasGroup.alpha = 0f;
		}

		private void Finish()
		{
			// Native ELF VA 0x1671ad0.
			IsFinished = true;
			canvasGroup.alpha = 0f;
		}
	}
}
