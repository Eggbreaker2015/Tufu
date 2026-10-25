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

		private EventModel CurrentEvent => null;

		protected override void Awake()
		{
		}

		public override void Initialize()
		{
		}

		private void HandleContinue()
		{
		}

		protected override void Render()
		{
		}
	}
}
