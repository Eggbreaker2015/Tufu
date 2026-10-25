using System;
using DG.Tweening;
using UnityEngine;

namespace Codebase.UI.Events
{
	public class RosterStagesExpandPanel : MonoBehaviour
	{
		[Header("Layout")]
		[SerializeField]
		private RectTransform panelRect;

		[SerializeField]
		private RectTransform contentRect;

		[SerializeField]
		private RectTransform viewportRect;

		[SerializeField]
		private RectTransform collapsedReference;

		[Header("Fading")]
		[SerializeField]
		private CanvasGroup listCanvasGroup;

		[SerializeField]
		private CanvasGroup dimOverlayGroup;

		[SerializeField]
		private CanvasGroup collapsedPreviewGroup;

		[Header("Collapsed Preview")]
		[SerializeField]
		public RosterStageView collapsedPreviewView;

		[SerializeField]
		private GameObject trailFromPrevious;

		[SerializeField]
		private GameObject trailFromPreviousPassed;

		[Header("Tuning")]
		[SerializeField]
		private float expandDuration = 0.35f;

		[SerializeField]
		private float fadeDuration = 0.1f;

		[SerializeField]
		private float rowHeight = 225.5f;

		private Vector2 expandedSize;

		private Vector2 expandedPos;

		private Vector2 collapsedSize;

		private Vector2 collapsedPos;

		private float viewportInset;

		private int stagesCount;

		private Sequence sequence;

		private Action onExpandRequested;

		public bool IsExpanded { get; private set; }

		public void Initialize(int stagesCount)
		{
			this.stagesCount = stagesCount;
			expandedSize = panelRect.sizeDelta;
			expandedPos = panelRect.anchoredPosition;
			viewportInset = expandedSize.y - viewportRect.rect.height;
			collapsedSize = collapsedReference.sizeDelta;
			collapsedPos = collapsedReference.anchoredPosition;
			panelRect.sizeDelta = collapsedSize;
			panelRect.anchoredPosition = collapsedPos;
			listCanvasGroup.alpha = 0f;
			listCanvasGroup.blocksRaycasts = false;
			collapsedPreviewGroup.alpha = 1f;
			collapsedPreviewGroup.blocksRaycasts = true;
			dimOverlayGroup.alpha = 0f;
			dimOverlayGroup.blocksRaycasts = false;
			dimOverlayGroup.interactable = false;
		}

		public void SetCurrentStage(int stageIndex, int currentPassedIndex, Action onExpandRequested)
		{
			collapsedPreviewView.Setup(stageIndex, stagesCount - 1 == stageIndex, onExpandRequested, null, false, true);
			trailFromPrevious.gameObject.SetActive(stageIndex != 0);
			trailFromPreviousPassed.gameObject.SetActive(stageIndex != 0 && stageIndex < currentPassedIndex);
		}

		public void SetExpanded(bool expand, int currentStageIndex)
		{
			IsExpanded = expand;
			sequence?.Kill();
			Vector2 size = expand ? expandedSize : collapsedSize;
			Vector2 position = expand ? expandedPos : collapsedPos;
			float contentY = ComputeCenteredContentY(currentStageIndex, size.y);
			dimOverlayGroup.blocksRaycasts = true;
			listCanvasGroup.blocksRaycasts = true;
			Ease ease = expand ? Ease.OutQuad : Ease.InQuad;
			sequence = DOTween.Sequence().SetTarget(this)
				.Join(panelRect.DOSizeDelta(size, expandDuration).SetEase(ease))
				.Join(panelRect.DOAnchorPos(position, expandDuration).SetEase(ease))
				.Join(contentRect.DOAnchorPosY(contentY, expandDuration).SetEase(ease))
				.Join(dimOverlayGroup.DOFade(expand ? 1f : 0f, fadeDuration))
				.Join(listCanvasGroup.DOFade(expand ? 1f : 0f, fadeDuration))
				.Join(collapsedPreviewGroup.DOFade(expand ? 0f : 1f, fadeDuration))
				.OnComplete(() =>
				{
					dimOverlayGroup.blocksRaycasts = expand;
					dimOverlayGroup.interactable = expand;
					listCanvasGroup.blocksRaycasts = expand;
					collapsedPreviewGroup.blocksRaycasts = !expand;
				});
		}

		private float ComputeCenteredContentY(int rowIndex, float windowHeight)
		{
			float viewportHeight = windowHeight - viewportInset;
			float max = Mathf.Max(0f, rowHeight * stagesCount - viewportHeight);
			return Mathf.Clamp((rowIndex + 0.5f) * rowHeight - viewportHeight * 0.5f, 0f, max);
		}

		public void Cleanup()
		{
			sequence?.Kill();
			sequence = null;
		}
	}
}
