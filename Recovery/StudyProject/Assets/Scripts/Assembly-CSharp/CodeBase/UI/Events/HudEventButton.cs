using CodeBase.Audio;
using Codebase.BossTower;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events.Models;
using Codebase.Player.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Events
{
	public class HudEventButton : View
	{
		[SerializeField]
		private Button button;

		[Header("States")]
		[SerializeField]
		private GameObject lockedState;

		[SerializeField]
		private GameObject unlockedState;

		[Header("Unlocked State")]
		[SerializeField]
		private GameObject startsInContainer;

		[SerializeField]
		private GameObject timerContainer;

		[SerializeField]
		private GameObject finishedContainer;

		[SerializeField]
		private TMP_Text startsInText;

		[Header("Ready State")]
		[SerializeField]
		private GameObject readyContainer;

		[Header("Progress State")]
		[SerializeField]
		private GameObject progressContainer;

		[Header("Progress State - Completed")]
		[SerializeField]
		private GameObject plainProgressState;

		[SerializeField]
		private TMP_Text progressText;

		[SerializeField]
		private Slider progressSlider;

		[Header("Progress State - Chest Ready")]
		[SerializeField]
		private GameObject chestReadyState;

		[Header("Progress State - Metrics")]
		[SerializeField]
		private GameObject metricsState;

		[SerializeField]
		private Image leagueIconImage;

		[SerializeField]
		private TMP_Text bossProgressText;

		[SerializeField]
		private Color bossProgressAdvancedColor;

		[SerializeField]
		private TMP_Text crystalProgressText;

		[SerializeField]
		private TMP_Text mobCountText;

		[SerializeField]
		private VisibilityAnimator visibilityAnimator;

		[Header("Locked State")]
		[SerializeField]
		private TMP_Text unlocksAtIslandText;

		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private UINavigator navigator;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private IAudioPlayer audioPlayer;

		[Inject]
		private BossTowerModel bossTowerModel;

		protected override void Awake()
		{
		}

		protected override void Render()
		{
		}

		private void RenderTimer(bool hasScheduledEvent)
		{
		}

		private void RenderProgress()
		{
		}

		private void RenderCompletedProgress()
		{
		}

		public override void Cleanup()
		{
		}

		private void OnButtonClick()
		{
		}
	}
}
