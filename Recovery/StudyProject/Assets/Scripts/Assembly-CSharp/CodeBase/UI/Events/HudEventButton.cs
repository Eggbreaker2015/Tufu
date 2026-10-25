using System;
using System.Linq;
using Codebase.Utilities;
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
			// Original APK ARM64 ELF VA 0x1660a04.
			base.Awake();
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(OnButtonClick);
			Initialize();
		}

		protected override void Render()
		{
			// Original APK ARM64 ELF VA 0x1660ac8.
			bool unlocked = eventsRepository.HasAnyUnlockedEvent;
			lockedState.SetActive(!unlocked);
			unlockedState.SetActive(unlocked);
			visibilityAnimator.SetVisible(!bossTowerModel.IsFighting);
			if (!unlocked)
			{
			    unlocksAtIslandText.text = LocalizationStrings.Get("EVENT_UNLOCKS_AT_ISLAND").Replace("{value}", eventsRepository.Events.Min(model => model.RequiredLocationNumber).ToString());
			    return;
			}
			bool completed = eventsRepository.HasCompletedEvent, active = eventsRepository.HasActiveEvent;
			bool ready = eventsRepository.HasReadyEvent, scheduled = eventsRepository.HasScheduledEvent;
			startsInContainer.SetActive(!scheduled && !completed);
			readyContainer.SetActive(ready && !active && !completed);
			progressContainer.SetActive(active || completed);
			timerContainer.SetActive(!completed);
			finishedContainer.SetActive(completed);
			RenderTimer(scheduled);
			if (active) RenderProgress();
			else if (completed) RenderCompletedProgress();
		}

		private void RenderTimer(bool hasScheduledEvent)
		{
			// Original APK ARM64 ELF VA 0x1660e0c.
			startsInText.text = TimeLocalizer.GetFormatTime((float)(hasScheduledEvent ? eventsRepository.TimeUntilScheduledEventEnds : eventsRepository.TimeUntilNextEvent).TotalSeconds, true);
		}

		private void RenderProgress()
		{
			// Original APK ARM64 ELF VA 0x1660ed8.
			plainProgressState.SetActive(false);
			var model = eventsRepository.CurrentEvent;
			if (model == null) return;
			bool chestReady = eventsRepository.IsRosterMilestoneChestReady(model);
			chestReadyState.SetActive(chestReady);
			metricsState.SetActive(!chestReady);
			if (chestReady) return;
			int planLeague = eventsRepository.EventPlan?.LeagueIndex ?? 0;
			var leagues = model.Def.CrystalRoster.Leagues;
			var currentLeague = leagues[model.RosterLeagueIndex];
			var nextLeague = leagues[Math.Clamp(planLeague + 1, 0, leagues.Count - 1)];
			bool advanced = model.Progress >= currentLeague.BossesToAdvance;
			int target = advanced ? model.Def.CrystalRoster.StagesCount : currentLeague.BossesToAdvance;
			bossProgressText.color = advanced ? bossProgressAdvancedColor : Color.white;
			bossProgressText.text = string.Format("<sprite name=League_Atlas_6>{0}/{1}<size=110%><sprite name={2}>", model.Progress, target, nextLeague.TextIcon);
			double collected = model.GetCollectedCurrency("red_crystal") + model.GetCollectedCurrency("blue_crystal");
			if (eventsRepository.HasClaimedAllRosterMilestones(model)) crystalProgressText.text = string.Format("<sprite name=Event_Atlas_0>{0}", (int)collected);
			else
			{
			    var milestone = eventsRepository.GetNextRosterMilestone(model);
			    if (milestone.HasValue) crystalProgressText.text = string.Format("<sprite name=Event_Atlas_0>{0}/{1}",
			        (int)Math.Min((float)collected, milestone.Value.RequiredCrystals), milestone.Value.RequiredCrystals);
			}
			var mobs = eventsRepository.GetEventMobCounts(model);
			mobCountText.text = string.Format("<sprite name=League_Atlas_5>{0}/{1}", mobs.Item1, mobs.Item2);
		}

		private void RenderCompletedProgress()
		{
			// Original APK ARM64 ELF VA 0x1661558.
			chestReadyState.SetActive(false);
			metricsState.SetActive(false);
			var model = eventsRepository.CompletedEvent;
			if (model == null) return;
			plainProgressState.SetActive(true);
			float normalized = model.ProgressNormalized;
			progressText.text = string.Format("{0}%", (int)Math.Round(normalized * 100f));
			progressSlider.value = normalized;
		}

		public override void Cleanup()
		{
			// Original APK ARM64 ELF VA 0x1661748.
			base.Cleanup();
		}

		private void OnButtonClick()
		{
			// Original APK ARM64 ELF VA 0x1661750.
			if (!eventsRepository.HasReadyEvent && !eventsRepository.HasActiveEvent) return;
			audioPlayer.PlaySoundOnce("tap");
			if (eventsRepository.HasActiveEvent)
			{
			    navigator.Open(eventsRepository.CurrentEvent?.Def.EventProgressViewKey ?? "Assets/Prefabs/UI/Event/EventProgress.prefab");
			    return;
			}
			var model = eventsRepository.ReadyEvent;
			string startKey = model?.Def.EventStartViewKey ?? "Assets/Prefabs/UI/Event/EventStart.prefab";
			string tutorialKey = model?.Def.EventTutorialViewKey ?? "Assets/Prefabs/UI/Event/EventTutorial.prefab";
			if (!playerModel.Data.SeenEventTutorial.Value)
			    navigator.Open<EventTutorialView>(tutorialKey).SetUserCloseCallback(() => navigator.Open(startKey));
			else navigator.Open(startKey);
		}
	}
}
