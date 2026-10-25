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

		private EventModel CurrentEvent => null;

		public override void Initialize()
		{
		}

		protected override void Render()
		{
		}

		private void UpdateProgress()
		{
		}

		public void Show()
		{
		}

		public void PlayStageAnimation(int stageIndex)
		{
		}

		public void PrepareStageAnimation(int stageIndex)
		{
		}

		public void PlayHideAnimation()
		{
		}
	}
}
