using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Codebase.UI.Map
{
	public class MiniMapDragHandler : MonoBehaviour, IBeginDragHandler, IEventSystemHandler, IDragHandler, IEndDragHandler
	{
		[SerializeField]
		private Button buttonToBlock;


		private Canvas canvas;

		private UnityEngine.Camera uiCamera;

		private Vector2 dragStartPosition;

		private bool wasDragging;

		public bool IsDragEnabled { get; set; }

		public event Action<Vector2> OnDragDelta;

		private void Awake()
		{
			canvas = GetComponentInParent<Canvas>();
			uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (!IsDragEnabled) return;
			wasDragging = true;
			if (buttonToBlock != null) buttonToBlock.interactable = false;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, eventData.position, uiCamera, out dragStartPosition);
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (!IsDragEnabled || !wasDragging) return;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, eventData.position, uiCamera, out Vector2 position);
			Vector2 delta = position - dragStartPosition;
			dragStartPosition = position;
			OnDragDelta?.Invoke(delta);
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			if (!wasDragging) return;
			wasDragging = false;
			Invoke(nameof(ReenableButton), .1f);
		}

		private void ReenableButton()
		{
			if (buttonToBlock != null) buttonToBlock.interactable = true;
		}
	}
}
