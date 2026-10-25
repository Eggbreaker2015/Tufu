using UnityEngine;
using UnityEngine.EventSystems;
using Codebase.Core;

namespace Codebase.Player.Input
{
	public class Joystick : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler, IDragHandler
	{
		[SerializeField]
		private RectTransform pad;

		[SerializeField]
		private RectTransform stick;

		private Canvas canvas;

		private UnityEngine.Camera uiCamera;

		private Vector2 inputVector;

		private float radius;

		private Vector3 defaultPosition;

		private void Awake()
		{
			// Native ELF VA 0x14663ac.
			canvas = GetComponentInParent<Canvas>();
			uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
			radius = pad.sizeDelta.x * 0.5f;
			defaultPosition = pad.anchoredPosition;
			GameContext.Set(this);
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			// Native ELF VA 0x14664d8.
			RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, uiCamera, out Vector2 position);
			pad.anchoredPosition = position;
			stick.anchoredPosition = Vector2.zero;
			pad.gameObject.SetActive(true);
		}

		public void OnDrag(PointerEventData eventData)
		{
			// Native ELF VA 0x1466630.
			RectTransformUtility.ScreenPointToLocalPointInRectangle(pad, eventData.position, uiCamera, out Vector2 position);
			if (position.sqrMagnitude > radius * radius) position = position.normalized * radius;
			stick.anchoredPosition = position;
			inputVector = position / radius;
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			// Native ELF VA 0x1466770: returns pad to its initial anchored position.
			stick.anchoredPosition = Vector2.zero;
			inputVector = Vector2.zero;
			pad.anchoredPosition = defaultPosition;
		}

		public Vector2 GetInput()
		{
			// Native ELF VA 0x1466818.
			return inputVector;
		}
	}
}
