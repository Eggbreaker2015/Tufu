using System.Collections.Generic;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events.Models;
using TMPro;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Events
{
	public class EventTimeIsOverView : PopupView
	{
		[Header("Buttons")]
		[SerializeField]
		private Button closeButton;

		[Header("Texts")]
		[SerializeField]
		private TMP_Text eventTitleText;

		[SerializeField]
		private TMP_Text progressText;

		[SerializeField]
		private TMP_Text defeatBossText;

		[Header("Boosters")]
		[SerializeField]
		private Transform boostersContainer;

		[SerializeField]
		private EventBoosterView boosterViewPrefab;

		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private UINavigator navigator;

		private readonly List<EventBoosterView> spawnedBoosterViews;

		private ComputedAtom<EventModel> __CurrentEvent;

		private EventModel CurrentEvent => null;

		protected override void Awake()
		{
		}

		public override void Initialize()
		{
		}

		protected override void Render()
		{
		}

		private void SetupBoosters()
		{
		}

		private bool HasExtraTimeReward(EventBoosterModel boosterModel)
		{
			return false;
		}

		private void ClearBoosters()
		{
		}

		public override void Cleanup()
		{
		}
	}
}
