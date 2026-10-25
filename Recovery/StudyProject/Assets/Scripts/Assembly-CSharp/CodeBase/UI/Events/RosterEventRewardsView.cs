using System.Linq;
using UniMob;
using Codebase.Rewards.Commands;
using System.Collections.Generic;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events.Models;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Events
{
	public class RosterEventRewardsView : PopupView
	{
		[Header("Buttons")]
		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Button claimButton;

		[Header("Stages Section")]
		[SerializeField]
		private Transform stagesContent;

		[SerializeField]
		private RosterStageView stageViewPrefab;

		[SerializeField]
		private RosterProgressFill progressFill;

		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private IAudioPlayer audioPlayer;

		private ViewContainer<RosterStageView, int> stagesViewContainer;

		private ComputedAtom<EventModel> __CurrentEvent;

		private EventModel CurrentEvent
        {
            get
            {
                // Original APK ARM64 ELF VA 0x1669dc8.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__CurrentEvent == null) __CurrentEvent = CodeGenAtom.CreatePooled(this, null, () => CurrentEvent, false);
                return __CurrentEvent.DirectEvaluate() ? eventsRepository.ScheduledEvent ?? eventsRepository.CurrentEvent ?? eventsRepository.CompletedEvent : __CurrentEvent.Value;
            }
        }

		protected override void Awake()
		{
			// Original APK ARM64 ELF VA 0x1669f30.
			base.Awake();
			closeButton.onClick.AddListener(HandleClose);
			claimButton.onClick.AddListener(HandleClose);
		}

		private void HandleClose()
		{
			// Original APK ARM64 ELF VA 0x166a010.
			audioPlayer.PlaySoundOnce("tap");
			Close();
		}

		protected override void Start()
		{
			// Original APK ARM64 ELF VA 0x166a0dc.
			base.Start();
			int count = GetStageIndices().Count;
			stagesViewContainer = new ViewContainer<RosterStageView, int>(stagesContent, stageViewPrefab)
			    .BindTo(Lifetime, GetStageIndices).Setup((view, data, index) => view.Setup(data, index == count - 1));
		}

		public override void Initialize()
		{
			// Original APK ARM64 ELF VA 0x166a398.
			base.Initialize();
			App.ExecuteCommand(new BakeRewardsCommand { BakeRosterEvent = true });
		}

		protected override void Render()
		{
			// Original APK ARM64 ELF VA 0x166a430.
			if (CurrentEvent != null && progressFill != null) progressFill.Setup(CurrentEvent.Progress, CurrentEvent.ProgressMax);
		}

		private List<int> GetStageIndices()
		{
			// Original APK ARM64 ELF VA 0x166a2c8.
			return CurrentEvent?.Def?.CrystalRoster?.Stages != null ? Enumerable.Range(0, CurrentEvent.Def.CrystalRoster.Stages.Count).ToList() : new List<int>();
		}

		public override void Cleanup()
		{
			// Original APK ARM64 ELF VA 0x166a5b8.
			base.Cleanup();
		}
	}
}
