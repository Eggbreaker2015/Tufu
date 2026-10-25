using BrunoMikoski.AnimationSequencer;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events.Models;
using TMPro;
using UnityEngine;

namespace Codebase.UI.Events
{
	public class BossHuntProgressView : View
	{
		[SerializeField]
		private TMP_Text progressText;

		[SerializeField]
		private CanvasGroup canvasGroup;

		[SerializeField]
		private AnimationSequencerController hideAnimation;

		[SerializeField]
		private EventProgressBarView eventBarView;

		[Inject]
		private EventsRepository eventsRepository;

		private EventModel CurrentEvent => eventsRepository.CurrentEvent;

		public override void Initialize()
		{
			base.Initialize();
			UpdateProgress();
		}

		protected override void Render()
		{
			// Original4-byte0x1658fa4 tail-calls UpdateProgress.
			UpdateProgress();
		}

		private void UpdateProgress()
		{
			var model = CurrentEvent;
			if (model != null) progressText.text = string.Format("{0:F0}%", model.ProgressNormalized * 100f);
		}

		public void Show()
		{
			if (canvasGroup != null) canvasGroup.alpha = 1f;
			gameObject.SetActive(true);
		}

		public void PlayStageAnimation(int stageIndex)
		{
			eventBarView.PlayStageAnimation(stageIndex);
		}

		public void PrepareStageAnimation(int stageIndex)
		{
			eventBarView.SetNotPassedState(stageIndex);
		}

		public void PlayHideAnimation()
		{
			if (hideAnimation != null) hideAnimation.Play();
			else if (canvasGroup != null) canvasGroup.alpha = 0f;
		}
	}
}
