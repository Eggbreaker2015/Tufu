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

		private EventModel CurrentEvent => null;

		protected override void Awake()
		{
		}

		private void HandleClose()
		{
		}

		protected override void Start()
		{
		}

		public override void Initialize()
		{
		}

		protected override void Render()
		{
		}

		private List<int> GetStageIndices()
		{
			return null;
		}

		public override void Cleanup()
		{
		}
	}
}
