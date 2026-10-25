using BrunoMikoski.AnimationSequencer;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Events
{
	public class EventProgressStageView : View
	{
		[SerializeField]
		private Image icon;

		[SerializeField]
		private Image icon2;

		[SerializeField]
		private Image icon3;

		[SerializeField]
		private Canvas canvas;

		[SerializeField]
		private AnimationSequencerController animationSequencerController;

		[Inject]
		private EventStyleConfig eventStyle;

		private int stageIndex;

		private ProgressStageType stageType;

		public void Setup(int stageIndex, int totalStages)
		{
			// Original APK ARM64 ELF VA 0x165b9b8.
			this.stageIndex = stageIndex;
			stageType = GetStageType(stageIndex, totalStages);
		}

		public void UpdateState(ProgressStageState state)
		{
			// Original APK ARM64 ELF VA 0x165bb68.
			icon.sprite = eventStyle.ProgressBarStyle.GetStyle(stageType).GetSprite(state);
		}

		public void SetPrioritizedSortingOrder(bool prioritized)
		{
			// Original APK ARM64 ELF VA 0x165bd54.
			canvas.overrideSorting = prioritized;
		}

		public void SetPassedState()
		{
			// Original APK ARM64 ELF VA 0x165bd70.
			icon3.enabled = false;
		}

		public void SetNotPassedState()
		{
			// Original APK ARM64 ELF VA 0x165bbe4.
			var style = eventStyle.ProgressBarStyle.GetStyle(stageType);
			icon2.sprite = style.GetSprite(ProgressStageState.Passed);
			icon3.sprite = style.GetSprite(ProgressStageState.NotPassed);
			icon3.enabled = true;
		}

		public void PlayAnimation()
		{
			// Original APK ARM64 ELF VA 0x165bbc8.
			animationSequencerController.Play();
		}

		public static ProgressStageType GetStageType(int stageIndex, int totalStages)
		{
			// Original APK ARM64 ELF VA 0x165bd08.
			if (stageIndex == totalStages - 1) return ProgressStageType.GrandBoss;
			if (stageIndex == totalStages - 2) return ProgressStageType.Mob;
			return (stageIndex + 1) % 3 == 0 ? ProgressStageType.Boss : ProgressStageType.Mob;
		}
	}
}
