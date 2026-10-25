using UniMob;
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

		private readonly List<EventProgressStageView> spawnedStages = new List<EventProgressStageView>();

		private ComputedAtom<EventModel> __CurrentEvent;

		private ComputedAtom<EventModel> __ReadyEvent;

		private EventModel CurrentEvent
        {
            get
            {
                // Original APK ARM64 ELF VA 0x165b430.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__CurrentEvent == null) __CurrentEvent = CodeGenAtom.CreatePooled(this, null, () => CurrentEvent, false);
                return __CurrentEvent.DirectEvaluate() ? eventsRepository.CurrentEvent : __CurrentEvent.Value;
            }
        }

		private EventModel ReadyEvent
        {
            get
            {
                // Original APK ARM64 ELF VA 0x165b56c.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__ReadyEvent == null) __ReadyEvent = CodeGenAtom.CreatePooled(this, null, () => ReadyEvent, false);
                return __ReadyEvent.DirectEvaluate() ? eventsRepository.ReadyEvent : __ReadyEvent.Value;
            }
        }

		private void OnEnable()
		{
			// Original APK ARM64 ELF VA 0x165b6a8.
			if (!autoSetup) return;
			Injector.InjectInto(this);
			Initialize();
			Setup();
		}

		public void Setup()
		{
			// Original APK ARM64 ELF VA 0x165aed8.
			ClearStages();
			var model = CurrentEvent ?? ReadyEvent ?? eventsRepository.CompletedEvent;
			if (model == null) return;
			int count = model.ProgressMax;
			for (int i = 0; i < count; i++)
			{
			    var view = Instantiate(stagePrefab, container);
			    Injector.InjectInto(view);
			    view.Setup(i, count);
			    spawnedStages.Add(view);
			}
			UpdateStages();
		}

		protected override void Render()
		{
			// Original APK ARM64 ELF VA 0x165bb48.
			UpdateStages();
		}

		private void UpdateStages()
		{
			// Original APK ARM64 ELF VA 0x165ba0c.
			var model = CurrentEvent ?? ReadyEvent ?? eventsRepository.CompletedEvent;
			if (model == null) return;
			int progress = model.Progress;
			bool failed = model.State == EventState.Completed && !model.IsProgressComplete;
			bool finished = model.IsCompleted;
			for (int i = 0; i < spawnedStages.Count; i++)
			    spawnedStages[i].UpdateState(GetStageState(i, progress, finished, failed));
		}

		private static ProgressStageState GetStageState(int stageIndex, int progress, bool isFinished, bool isFailed)
		{
			// Original APK ARM64 ELF VA 0x165bb4c.
			return stageIndex < progress ? ProgressStageState.Passed : isFinished && isFailed ? ProgressStageState.Failed : ProgressStageState.NotPassed;
		}

		private void ClearStages()
		{
			// Original APK ARM64 ELF VA 0x165b72c.
			foreach (var view in spawnedStages) if (view != null) Destroy(view.gameObject);
			spawnedStages.Clear();
		}

		public void PlayStageAnimation(int stageIndex)
		{
			// Original APK ARM64 ELF VA 0x1659054.
			spawnedStages[stageIndex].PlayAnimation();
		}

		public void SetNotPassedState(int stageIndex)
		{
			// Original APK ARM64 ELF VA 0x16590ec.
			spawnedStages[stageIndex].SetNotPassedState();
		}

		public new void Cleanup()
		{
			// Original APK ARM64 ELF VA 0x165b314.
			ClearStages();
		}
	}
}
