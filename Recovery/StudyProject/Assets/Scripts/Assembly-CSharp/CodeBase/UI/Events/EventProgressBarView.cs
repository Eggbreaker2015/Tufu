using System.Collections.Generic;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events;
using Codebase.Events.Models;
using UniMob.Core;
using UnityEngine;

namespace Codebase.UI.Events
{
	public class EventProgressBarView : View
	{
		[SerializeField]
		private Transform container;

		[SerializeField]
		private EventProgressStageView stagePrefab;

		[SerializeField]
		private bool autoSetup;

		[Inject]
		private EventsRepository eventsRepository;

		private readonly List<EventProgressStageView> spawnedStages;

		private ComputedAtom<EventModel> __CurrentEvent;

		private ComputedAtom<EventModel> __ReadyEvent;

		private EventModel CurrentEvent => null;

		private EventModel ReadyEvent => null;

		private void OnEnable()
		{
		}

		public void Setup()
		{
		}

		protected override void Render()
		{
		}

		private void UpdateStages()
		{
		}

		private static ProgressStageState GetStageState(int stageIndex, int progress, bool isFinished, bool isFailed)
		{
			return ProgressStageState.NotPassed;
		}

		private void ClearStages()
		{
		}

		public void PlayStageAnimation(int stageIndex)
		{
		}

		public void SetNotPassedState(int stageIndex)
		{
		}

		public new void Cleanup()
		{
		}
	}
}
