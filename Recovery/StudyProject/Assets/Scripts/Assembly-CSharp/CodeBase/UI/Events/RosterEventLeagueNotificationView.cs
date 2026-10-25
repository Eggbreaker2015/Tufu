using UniMob;
using Codebase.Utilities;
using CodeBase.Audio;
using BrunoMikoski.AnimationSequencer;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events.Models;
using TMPro;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Events
{
	public class RosterEventLeagueNotificationView : PopupView
	{
		[SerializeField]
		private Button continueBtn;

		[SerializeField]
		private AnimationSequencerController sequencerController;

		[SerializeField]
		private Image inProgressLeagueIcon;

		[SerializeField]
		private TMP_Text leagueNameTxt;

		[SerializeField]
		private TMP_Text descriptionTxt;

		[Inject]
		private EventsRepository eventsRepository;

		private ComputedAtom<EventModel> __CurrentEvent;

		private EventModel CurrentEvent
        {
            get
            {
                // Original APK ARM64 ELF VA 0x1664634.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__CurrentEvent == null) __CurrentEvent = CodeGenAtom.CreatePooled(this, null, () => CurrentEvent, false);
                return __CurrentEvent.DirectEvaluate() ? eventsRepository.CurrentEvent ?? eventsRepository.CompletedEvent : __CurrentEvent.Value;
            }
        }

		protected override void Awake()
		{
			// Original APK ARM64 ELF VA 0x1664788.
			base.Awake();
			continueBtn.onClick.AddListener(HandleContinue);
		}

		public override void Initialize()
		{
			// Original APK ARM64 ELF VA 0x1664824.
			base.Initialize();
			Render();
			GameContext.Get<IAudioPlayer>().PlaySoundOnce("features_pop_up");
		}

		private void HandleContinue()
		{
			// Original APK ARM64 ELF VA 0x1664940.
			Close();
		}

		protected override void Render()
		{
			// Original APK ARM64 ELF VA 0x1664948.
			base.Render();
			var model = CurrentEvent;
			if (model == null) return;
			var league = model.Def.CrystalRoster.Leagues[model.RosterLeagueIndex];
			inProgressLeagueIcon.sprite = league.Icon;
			string name = LocalizationStrings.Get("EVENT_LEAGUE_NAME_" + league.Name);
			leagueNameTxt.text = "<color=" + league.TitleColor + ">" + name;
			descriptionTxt.text = LocalizationStrings.Get("EVENT_NEW_LEAGUE_DESCRIPTION")
			    .Replace("{league}", "<color=" + league.TitleColor + ">" + name + "</color>");
		}
	}
}
