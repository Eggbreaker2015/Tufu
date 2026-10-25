using DG.Tweening;
using Codebase.Core;
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
		private float milestoneSnapBias = 0.3f;

		[SerializeField]
		private float milestoneFlickVelocityThreshold = 800f;

		private ViewContainer<RosterEventMilestoneView, ForecastCrystalMilestonePlan> milestoneChestViewsContainer;

		private ViewContainer<RosterEventMilestoneNavButton, ForecastCrystalMilestonePlan> milestoneNavButtonsContainer;

		private int lastNearestMilestoneIndex = -1;

		private int dragStartMilestoneIndex;

		private bool hasSnappedSelectorOnce;

		private EventModel currentEvent;

		private List<ForecastCrystalMilestonePlan> milestones;

		[SerializeField]
		private bool forceShowAsLocked;

		private bool displayInfo;

		public void Initialize(Lifetime lifetime, Func<IEnumerable<ForecastCrystalMilestonePlan>> milestonesSource, Action<int> onChestClicked, bool displayInfo)
		{
			// Original APK ARM64 ELF VA 0x1666554.
			milestoneChestViewsContainer = new ViewContainer<RosterEventMilestoneView, ForecastCrystalMilestonePlan>(milestoneChestsContent, milestoneChestViewPrefab)
			    .BindTo(lifetime, milestonesSource).WithoutPooling().Setup((view, data, index) =>
			    {
			        Injector.InjectInto(view);
			        view.Setup(index, onChestClicked, displayInfo);
			    });
			milestoneNavButtonsContainer = new ViewContainer<RosterEventMilestoneNavButton, ForecastCrystalMilestonePlan>(milestoneNavButtonsContent, milestoneNavButtonViewPrefab)
			    .BindTo(lifetime, milestonesSource).WithoutPooling().Setup((view, data, index) => view.Setup(index));
			this.displayInfo = displayInfo;
			milestoneChestsScrollRect.onValueChanged.AddListener(HandleMilestoneScrollChanged);
			milestoneChestsScrollEndDragNotifier.Ended += HandleMilestoneScrollDragEnd;
			milestoneChestsScrollEndDragNotifier.Begin += HandleMilestoneScrollDragBegin;
			milestoneNavButtonsBg.SetAsFirstSibling();
		}

		private void HandleMilestoneScrollDragBegin()
		{
			// Original APK ARM64 ELF VA 0x166e1e0.
			dragStartMilestoneIndex = GetNearestMilestoneIndex(milestoneChestViewsContainer.Active);
			foreach (var view in milestoneChestViewsContainer.Active) view.SetTargeted(true);
		}

		public void UpdateState(EventModel currentEvent, List<ForecastCrystalMilestonePlan> milestones)
		{
			// Original APK ARM64 ELF VA 0x1666a4c.
			this.currentEvent = currentEvent;
			this.milestones = milestones;
			if (milestones == null || milestones.Count == 0) { milestoneChestsRoot.SetActive(false); return; }
			milestoneChestsRoot.SetActive(true);
			double collected = currentEvent.GetCollectedCurrency("red_crystal") + currentEvent.GetCollectedCurrency("blue_crystal");
			if (milestoneChestViewsContainer == null) return;
			bool previousReached = true;
			for (int i = 0; i < milestones.Count; i++)
			{
			    int required = milestones[i].RequiredCrystals;
			    bool claimed = i < currentEvent.RosterMilestoneClaimedCount;
			    bool ready = !claimed && i == currentEvent.RosterMilestoneClaimedCount && required <= collected;
			    milestoneChestViewsContainer.Active[i].SetupState(required, i == 0 ? 0 : milestones[i - 1].RequiredCrystals,
			        collected, ready, claimed, i == milestones.Count - 1, i == 0,
			        previousReached ? RosterDividerState.Claimed : RosterDividerState.Empty, forceShowAsLocked);
			    milestoneNavButtonsContainer.Active[i].SetupState(claimed ? RosterDividerState.Claimed :
			        required <= collected ? RosterDividerState.NotClaimed : RosterDividerState.Empty);
			    previousReached = required <= collected;
			}
			if (milestoneNavButtonsBg.GetSiblingIndex() != 0) milestoneNavButtonsBg.SetAsFirstSibling();
			UpdateMilestoneSelector();
		}

		public void UpdateMilestoneSelector()
		{
			// Original APK ARM64 ELF VA 0x166e724.
			if (milestoneChestViewsContainer == null || milestoneNavButtonsContainer == null) return;
			LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)milestoneChestsContent);
			LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)milestoneNavButtonsContent);
			var chests = milestoneChestViewsContainer.Active;
			var navs = milestoneNavButtonsContainer.Active;
			if (chests.Count == 0 || navs.Count == 0 || chests.Count != navs.Count) return;
			int index = GetNearestMilestoneIndex(chests);
			if (index == lastNearestMilestoneIndex && hasSnappedSelectorOnce) return;
			double collected = currentEvent.GetCollectedCurrency("red_crystal") + currentEvent.GetCollectedCurrency("blue_crystal");
			MoveSelectorTo(index, navs[index].RectTransform.position, milestones[index].RequiredCrystals <= collected, false);
		}

		public void ScrollToLastMilestoneInstant()
		{
			// Original APK ARM64 ELF VA 0x166ecf4.
			if (milestoneChestViewsContainer == null || milestoneNavButtonsContainer == null || milestones == null || milestones.Count == 0) return;
			LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)milestoneChestsContent);
			LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)milestoneNavButtonsContent);
			double collected = currentEvent.GetCollectedCurrency("red_crystal") + currentEvent.GetCollectedCurrency("blue_crystal");
			int index = milestones.FindIndex(milestone => collected < milestone.RequiredCrystals);
			for (int i = 0; i < milestones.Count; i++)
			    if (milestones[i].RequiredCrystals <= collected && i >= currentEvent.RosterMilestoneClaimedCount) { index = i; break; }
			if (index < 0) index = milestones.Count - 1;
			var chests = milestoneChestViewsContainer.Active;
			var navs = milestoneNavButtonsContainer.Active;
			if (chests.Count <= index || navs.Count <= index) return;
			CenterMilestoneScroll(chests[index], true);
			MoveSelectorTo(index, navs[index].RectTransform.position, milestones[index].RequiredCrystals <= collected, true);
		}

		public async UniTask ScrollToLastMilestoneInstantForFrames(int frameCount = 2)
		{
			// Original APK ARM64 ELF VA 0x16670d4.
			for (int i = 0; i < frameCount; i++) await UniTask.NextFrame();
			ScrollToLastMilestoneInstant();
		}

		private void MoveSelectorTo(int index, Vector3 targetPos, bool passed, bool instant)
		{
			// Original APK ARM64 ELF VA 0x166eae0.
			lastNearestMilestoneIndex = index;
			DOTween.Kill(milestoneSelector);
			if (!instant && hasSnappedSelectorOnce)
			{
			    DOTween.To(() => milestoneSelector.position, value => milestoneSelector.position = value, targetPos, 0.3f)
			        .SetTarget(milestoneSelector).SetEase((Ease)9);
			    milestoneSelectorAnim.SetVisible(passed, false);
			}
			else
			{
			    milestoneSelector.position = targetPos;
			    milestoneSelectorAnim.SetVisibleImmediate(passed);
			    hasSnappedSelectorOnce = true;
			}
		}

		private int GetNearestMilestoneIndex(IReadOnlyList<RosterEventMilestoneView> chestViews)
		{
			// Original APK ARM64 ELF VA 0x166e4b4.
			var viewport = milestoneChestsScrollRect.viewport != null ? milestoneChestsScrollRect.viewport : (RectTransform)milestoneChestsScrollRect.transform;
			float center = viewport.TransformPoint(viewport.rect.center).x;
			int nearest = 0;
			float distance = float.MaxValue;
			for (int i = 0; i < chestViews.Count; i++)
			{
			    var rect = chestViews[i].RectTransform;
			    float currentDistance = Mathf.Abs(rect.TransformPoint(rect.rect.center).x - center);
			    if (currentDistance < distance) { nearest = i; distance = currentDistance; }
			}
			return nearest;
		}

		private void HandleMilestoneScrollChanged(Vector2 _)
		{
			// Original APK ARM64 ELF VA 0x166f384.
			UpdateMilestoneSelector();
		}

		private void HandleMilestoneScrollDragEnd()
		{
			// Original APK ARM64 ELF VA 0x166f388.
			if (milestoneChestViewsContainer == null || milestoneChestViewsContainer.Active.Count == 0) return;
			var views = milestoneChestViewsContainer.Active;
			CenterMilestoneScroll(views[GetTargetMilestoneIndex(views)]);
		}

		private int GetTargetMilestoneIndex(IReadOnlyList<RosterEventMilestoneView> chestViews)
		{
			// Original APK ARM64 ELF VA 0x166f4dc.
			int start = Mathf.Clamp(dragStartMilestoneIndex, 0, chestViews.Count - 1);
			float velocity = milestoneChestsScrollRect.velocity.x;
			if (Mathf.Abs(velocity) > milestoneFlickVelocityThreshold)
			    return Mathf.Clamp(start + (velocity > 0 ? -1 : 1), 0, chestViews.Count - 1);
			float center = GetNormalizedXForRect(chestViews[start].RectTransform);
			float position = milestoneChestsScrollRect.horizontalNormalizedPosition;
			int neighbor = position <= center ? Math.Max(start - 1, 0) : Math.Min(start + 1, chestViews.Count - 1);
			if (neighbor != start)
			{
			    float neighborCenter = GetNormalizedXForRect(chestViews[neighbor].RectTransform);
			    if (neighborCenter - center != 0 && Mathf.Abs((position - center) / (neighborCenter - center)) >= milestoneSnapBias)
			        return neighbor;
			}
			return start;
		}

		private float GetNormalizedXForRect(RectTransform target)
		{
			// Original APK ARM64 ELF VA 0x166f7f4.
			var content = milestoneChestsScrollRect.content;
			var viewport = milestoneChestsScrollRect.viewport != null ? milestoneChestsScrollRect.viewport : (RectTransform)milestoneChestsScrollRect.transform;
			float center = content.InverseTransformPoint(target.TransformPoint(target.rect.center)).x;
			float width = Math.Max(0.0001f, content.rect.width - viewport.rect.width);
			return Mathf.Clamp01((center - content.rect.xMin - viewport.rect.width * 0.5f) / width);
		}

		private void CenterMilestoneScroll(RosterEventMilestoneView targetView, bool instant = false)
		{
			// Original APK ARM64 ELF VA 0x166f0a8.
			float target = GetNormalizedXForRect(targetView.RectTransform);
			milestoneChestsScrollRect.velocity = Vector2.zero;
			DOTween.Kill(milestoneChestsScrollRect);
			if (instant)
			{
			    milestoneChestsScrollRect.horizontalNormalizedPosition = target;
			    NotifyTargeted(targetView);
			}
			else DOTween.To(() => milestoneChestsScrollRect.horizontalNormalizedPosition,
			        value => milestoneChestsScrollRect.horizontalNormalizedPosition = value, target, 0.3f)
			    .SetTarget(milestoneChestsScrollRect).SetEase((Ease)9).OnComplete(() => NotifyTargeted(targetView));
		}

		private void NotifyTargeted(RosterEventMilestoneView targetView)
		{
			// Original APK ARM64 ELF VA 0x166f99c.
			if (targetView == null) return;
			foreach (var view in milestoneChestViewsContainer.Active) view.SetTargeted(false);
			targetView.SetTargeted(true);
		}

		public void Cleanup()
		{
			// Original APK ARM64 ELF VA 0x166954c.
			milestoneChestViewsContainer.Clear();
			milestoneNavButtonsContainer.Clear();
			DOTween.Kill(milestoneSelector);
			DOTween.Kill(milestoneChestsScrollRect);
			hasSnappedSelectorOnce = false;
			lastNearestMilestoneIndex = -1;
			dragStartMilestoneIndex = 0;
		}
	}
}
