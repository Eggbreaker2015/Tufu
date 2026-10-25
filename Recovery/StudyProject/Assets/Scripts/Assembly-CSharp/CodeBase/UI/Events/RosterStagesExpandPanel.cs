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
		private float expandDuration;

		[SerializeField]
		private float fadeDuration;

		[SerializeField]
		private float rowHeight;

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
		}

		public void SetCurrentStage(int stageIndex, int currentPassedIndex, Action onExpandRequested)
		{
		}

		public void SetExpanded(bool expand, int currentStageIndex)
		{
		}

		private float ComputeCenteredContentY(int rowIndex, float windowHeight)
		{
			return 0f;
		}

		public void Cleanup()
		{
		}
	}
}
