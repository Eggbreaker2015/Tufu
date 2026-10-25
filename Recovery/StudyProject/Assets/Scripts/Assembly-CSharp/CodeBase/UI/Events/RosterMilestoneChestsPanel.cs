using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Core.UI;
using Codebase.Events.Models;
using Codebase.Simulation.Forecast;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using UniMob;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Events
{
	public class RosterMilestoneChestsPanel : MonoBehaviour
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CScrollToLastMilestoneInstantForFrames_003Ed__26 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public RosterMilestoneChestsPanel _003C_003E4__this;

			public int frameCount;

			private int _003Ci_003E5__2;

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
		private ScrollRect milestoneChestsScrollRect;

		[SerializeField]
		private ScrollRectEndDragNotifier milestoneChestsScrollEndDragNotifier;

		[SerializeField]
		private GameObject milestoneChestsRoot;

		[SerializeField]
		private Transform milestoneChestsContent;

		[SerializeField]
		private Transform milestoneNavButtonsContent;

		[SerializeField]
		private RosterEventMilestoneView milestoneChestViewPrefab;

		[SerializeField]
		private RosterEventMilestoneNavButton milestoneNavButtonViewPrefab;

		[SerializeField]
		private RectTransform milestoneNavButtonsBg;

		[SerializeField]
		private RectTransform milestoneSelector;

		[SerializeField]
		private VisibilityAnimator milestoneSelectorAnim;

		[SerializeField]
		private float milestoneSnapBias;

		[SerializeField]
		private float milestoneFlickVelocityThreshold;

		private ViewContainer<RosterEventMilestoneView, ForecastCrystalMilestonePlan> milestoneChestViewsContainer;

		private ViewContainer<RosterEventMilestoneNavButton, ForecastCrystalMilestonePlan> milestoneNavButtonsContainer;

		private int lastNearestMilestoneIndex;

		private int dragStartMilestoneIndex;

		private bool hasSnappedSelectorOnce;

		private EventModel currentEvent;

		private List<ForecastCrystalMilestonePlan> milestones;

		[SerializeField]
		private bool forceShowAsLocked;

		private bool displayInfo;

		public void Initialize(Lifetime lifetime, Func<IEnumerable<ForecastCrystalMilestonePlan>> milestonesSource, Action<int> onChestClicked, bool displayInfo)
		{
		}

		private void HandleMilestoneScrollDragBegin()
		{
		}

		public void UpdateState(EventModel currentEvent, List<ForecastCrystalMilestonePlan> milestones)
		{
		}

		public void UpdateMilestoneSelector()
		{
		}

		public void ScrollToLastMilestoneInstant()
		{
		}

		[AsyncStateMachine(typeof(_003CScrollToLastMilestoneInstantForFrames_003Ed__26))]
		public UniTask ScrollToLastMilestoneInstantForFrames(int frameCount = 2)
		{
			return default;
		}

		private void MoveSelectorTo(int index, Vector3 targetPos, bool passed, bool instant)
		{
		}

		private int GetNearestMilestoneIndex(IReadOnlyList<RosterEventMilestoneView> chestViews)
		{
			return 0;
		}

		private void HandleMilestoneScrollChanged(Vector2 _)
		{
		}

		private void HandleMilestoneScrollDragEnd()
		{
		}

		private int GetTargetMilestoneIndex(IReadOnlyList<RosterEventMilestoneView> chestViews)
		{
			return 0;
		}

		private float GetNormalizedXForRect(RectTransform target)
		{
			return 0f;
		}

		private void CenterMilestoneScroll(RosterEventMilestoneView targetView, bool instant = false)
		{
		}

		private void NotifyTargeted(RosterEventMilestoneView targetView)
		{
		}

		public void Cleanup()
		{
		}
	}
}
