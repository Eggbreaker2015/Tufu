using System;
using UniMob;
using System.Linq;
using Codebase.Utilities;
using Codebase.Rewards.Commands;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events;
using Codebase.Events.Models;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Rewards;
using Codebase.Simulation.Forecast;
using Codebase.UI.IslandNotification;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using TMPro;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Events
{
	public class RosterEventStartView : PopupView
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CBuildPreviewEventPlan_003Ed__38 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;

			public RosterEventStartView _003C_003E4__this;

			private UniTask.Awaiter _003C_003Eu__1;

			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CStartEvent_003Ed__39 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncVoidMethodBuilder _003C_003Et__builder;

			public RosterEventStartView _003C_003E4__this;

			private UniTask.Awaiter _003C_003Eu__1;

			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[SerializeField]
		private RosterLeagueProgressPanel leagueProgressPanel;

		[Header("Buttons")]
		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Button infoButton;

		[SerializeField]
		private Button startEventButton;

		[Header("Texts")]
		[SerializeField]
		private TMP_Text eventTitleText;

		[SerializeField]
		private TMP_Text durationText;

		[SerializeField]
		private TMP_Text bossesCounterTxt;

		[Header("Rewards Section")]
		[SerializeField]
		private Transform rewardsContent;

		[SerializeField]
		private RewardView rewardViewPrefab;

		[SerializeField]
		private IslandNotificationMapInfoView weaknessInfo;

		[SerializeField]
		private IslandNotificationMapInfoView attackInfo;

		[SerializeField]
		private Color weaknessColor;

		[SerializeField]
		private Color attackColor;

		[Inject]
		private UINavigator navigator;

		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private EventStyleConfig eventStyleConfig;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private ItemsProvider itemsProvider;

		[Inject]
		private IAudioPlayer audioPlayer;

		private ViewContainer<RosterStageView, int> stagesViewContainer;

		private ViewContainer<RewardView, RewardBase> rewardsViewContainer;

		private bool isStartingEvent;

		private ComputedAtom<EventModel> __CurrentEvent;

		private EventModel CurrentEvent
        {
            get
            {
                // Original APK ARM64 ELF VA 0x166a604.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__CurrentEvent == null) __CurrentEvent = CodeGenAtom.CreatePooled(this, null, () => CurrentEvent, false);
                return __CurrentEvent.DirectEvaluate() ? eventsRepository.ScheduledEvent : __CurrentEvent.Value;
            }
        }

		protected override void Awake()
		{
			// Original APK ARM64 ELF VA 0x166a730.
			base.Awake();
			closeButton.onClick.AddListener(HandleClose);
			infoButton.onClick.AddListener(OpenTutorial);
			startEventButton.onClick.AddListener(StartEvent);
		}

		protected override void Start()
		{
			// Original APK ARM64 ELF VA 0x166a874.
			base.Start();
			rewardsViewContainer = new ViewContainer<RewardView, RewardBase>(rewardsContent, rewardViewPrefab)
			    .BindTo(Lifetime, GetSummaryRewards).Setup((view, reward) => { view.Setup(reward, RewardViewStyle.Simplified); view.SimplifiedRewardGlow.SetActive(false); });
		}

		public override void Initialize()
		{
			// Original APK ARM64 ELF VA 0x166ab58.
			base.Initialize();
			SetupEventInfo();
			BuildPreviewEventPlan().Forget();
		}

		protected override void Render()
		{
			// Original APK ARM64 ELF VA 0x166aec0.
			var model = CurrentEvent;
			var plan = eventsRepository.EventPlan;
			if (model == null || !plan.HasValue) return;
			leagueProgressPanel.UpdateProgress(model, plan);
			int index = plan.Value.LeagueIndex;
			var leagues = model.Def.CrystalRoster.Leagues;
			var league = leagues[index];
			var next = leagues[Math.Min(leagues.Count - 1, index + 1)];
			eventTitleText.text = "<color=" + league.TitleColor + ">" + LocalizationStrings.Get("EVENT_LEAGUE_TITLE_" + league.Name) + "</color>";
			bossesCounterTxt.transform.parent.gameObject.SetActive(!model.MaxLeagueReached);
			if (!model.MaxLeagueReached)
			    bossesCounterTxt.text = LocalizationStrings.Get("EVENT_BOSSES_LEFT_TO_LEAGUE")
			        .Replace("{color}", next.TitleColor ?? "")
			        .Replace("{league}", LocalizationStrings.Get("EVENT_LEAGUE_NAME_" + next.Name))
			        .Replace("{value}", (league.BossesToAdvance - model.Progress).ToString());
			durationText.text = "<size=90%><sprite name=atlas_56></size> " +
			    TimeLocalizer.GetFormatTime(model.Def.CrystalRoster.ForecastSettings.DurationMinutes * 60f, true);
		}

		private List<ForecastCrystalMilestonePlan> GetMilestones()
		{
			// Original APK ARM64 ELF VA 0x166b580.
			return GetPlanMilestones() ?? new List<ForecastCrystalMilestonePlan>();
		}

		private List<RewardBase> GetMilestoneRewards(int milestoneIndex)
		{
			// Original APK ARM64 ELF VA 0x166b758.
			return ForecastCrystalRewardEntryHelper.ToRewards(GetPlanMilestones()[milestoneIndex].Rewards, itemsProvider);
		}

		private List<RewardBase> GetSummaryRewards()
		{
			// Original APK ARM64 ELF VA 0x166b7d4.
			var all = new List<RewardBase>();
			for (int index = 0; index < GetMilestones().Count; index++) all.AddRange(GetMilestoneRewards(index));
			var result = all.OfType<StatReward>().GroupBy(reward => (reward.Type, reward.ModifierType))
			    .Select(group => (RewardBase)new StatReward
			    {
			        Type = group.First().Type,
			        ModifierType = group.First().ModifierType,
			        CustomModifier = group.First().CustomModifier,
			        Value = group.Sum(reward => reward.Value),
			    }).Take(3).ToList();
			var item = all.OfType<ItemReward>().FirstOrDefault(reward => GetItemDef(reward)?.Rarity == (Rarity)4);
			if (item != null) result.Add(item);
			return result;
		}

		private ItemConfig GetItemDef(ItemReward itemReward)
		{
			// Original APK ARM64 ELF VA 0x166bbc4.
			return itemReward.Def != null ? itemReward.Def : itemsProvider.Get(itemReward.BakedData.ItemKey);
		}

		private List<ForecastCrystalMilestonePlan> GetPlanMilestones()
		{
			// Original APK ARM64 ELF VA 0x166b5fc.
			return eventsRepository.EventPlan?.CrystalMilestones;
		}

		private List<int> GetStageIndices()
		{
			// Original APK ARM64 ELF VA 0x166aa88.
			return CurrentEvent?.Def?.CrystalRoster?.Stages != null ? Enumerable.Range(0, CurrentEvent.Def.CrystalRoster.Stages.Count).ToList() : new List<int>();
		}

		private void SetupEventInfo()
		{
			// Original APK ARM64 ELF VA 0x166ab9c.
			var model = CurrentEvent;
			if (model == null) return;
			var location = playerModel.LastUnlockedLocation;
			if (location != null)
			{
			    model.Data.WeaponType.Value = location.PreferredWeaponType;
			    model.Data.ResistType.Value = location.PreferredArmorType;
			}
			if (weaknessInfo != null)
			{
			    Injector.InjectInto(weaknessInfo);
			    weaknessInfo.Setup(model.Data.ResistType.Value, "ISLAND_WEAKNESS", weaknessColor);
			}
			if (attackInfo != null)
			{
			    Injector.InjectInto(attackInfo);
			    attackInfo.Setup(model.Data.WeaponType.Value, "ISLAND_ATTACK", attackColor);
			}
		}

		private void HandleClose()
		{
			// Original APK ARM64 ELF VA 0x166bc60.
			audioPlayer.PlaySoundOnce("tap");
			Close();
		}

		private void OpenTutorial()
		{
			// Original APK ARM64 ELF VA 0x166bd2c.
			audioPlayer.PlaySoundOnce("tap");
			navigator.Open<EventTutorialView>("Assets/Prefabs/UI/Event/Roster/RosterEventTutorial.prefab");
		}

		private void OpenRewards()
		{
			// Original APK ARM64 ELF VA 0x166be2c.
			audioPlayer.PlaySoundOnce("tap");
			navigator.Open<RosterEventRewardsView>("Assets/Prefabs/UI/Event/Roster/RosterRewardsPopup.prefab");
		}

		private async UniTaskVoid BuildPreviewEventPlan()
		{
			// Original APK ARM64 ELF VA 0x166c390.
			if (CurrentEvent == null) return;
			await eventsRepository.BuildRosterEventPlanAsync(CurrentEvent.Key);
			App.ExecuteCommand(new BakeRewardsCommand { BakeRosterEvent = true });
		}

		private async void StartEvent()
		{
			// Original APK ARM64 ELF VA 0x166c754.
			if (CurrentEvent == null || isStartingEvent) return;
			isStartingEvent = true;
			startEventButton.interactable = false;
			await eventsRepository.StartEventAsync(CurrentEvent.Key);
			isStartingEvent = false;
			startEventButton.interactable = true;
			Close();
		}

		public override void Cleanup()
		{
			// Original APK ARM64 ELF VA 0x166bfd0.
			base.Cleanup();
			rewardsViewContainer?.Clear();
		}
	}
}
