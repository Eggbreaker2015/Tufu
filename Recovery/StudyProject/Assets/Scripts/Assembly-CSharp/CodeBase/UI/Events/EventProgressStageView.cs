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
		}

		public void UpdateState(ProgressStageState state)
		{
		}

		public void SetPrioritizedSortingOrder(bool prioritized)
		{
		}

		public void SetPassedState()
		{
		}

		public void SetNotPassedState()
		{
		}

		public void PlayAnimation()
		{
		}

		public static ProgressStageType GetStageType(int stageIndex, int totalStages)
		{
			return ProgressStageType.Mob;
		}
	}
}
